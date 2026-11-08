using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace BerryShooter
{
    public enum Role { None, Solo, Host, Client }

    public sealed class PlayerProfile
    {
        public string Name = "Player", Tier = "free", Badge = "none", Club = "", Country = "", Role = "member", Uid = "";
        public bool FromBerry;
    }

    public struct LocalInput
    {
        public float Mx, Mz, Yaw, Pitch;
        public bool Fire, Jump, Sprint;
    }

    public sealed class GameSession
    {
        public const float Step = 1f / 30f, SnapEvery = 0.05f;

        public Role Role { get; private set; }
        public World World { get; private set; }
        public int LocalId { get; private set; }
        public int MaxPlayers { get; private set; }
        public bool Welcomed { get; private set; }
        public string Problem { get; private set; }
        public int PingMs { get; private set; } = -1;
        public int Port { get; private set; }
        public string HostName { get; private set; } = "";
        public string Transport { get { return link == null ? "-" : link.Kind; } }
        public PlayerState LocalPlayer { get { PlayerState p; return World.Players.TryGetValue(LocalId, out p) ? p : null; } }
        public long BytesIn { get { return link != null ? link.BytesIn : 0; } }
        public long BytesOut { get { return link != null ? link.BytesOut : 0; } }
        public bool IsAuthority { get { return Role == Role.Host || Role == Role.Solo; } }

        public Vector3 LocalPos
        {
            get
            {
                PlayerState me = LocalPlayer;
                if (Role == Role.Client && predReady) return predPos;
                if (me != null) return me.Pos;
                return ArenaLayout.SpawnPoint(LocalId);
            }
        }

        public event Action<string> Message;
        public event Action<int, Vector3, Vector3, bool> Tracer;
        public event Action<GameState> StateChanged;

        private ILink link;
        private ClientLink clientLink;
        private readonly PlayerProfile profile;
        private readonly Dictionary<string, int> idByPeer = new Dictionary<string, int>();
        private int nextId = 2, tick, seq;
        private float accum, snapTimer, joinTimer, pingTimer, inputTimer, lastHeard, joinStarted;
        private Vector3 predPos;
        private float predVy;
        private bool predReady;
        private GameState lastState = GameState.Lobby;

        private GameSession(PlayerProfile p)
        {
            profile = p;
            World = new World(Environment.TickCount);
        }

        public static GameSession Solo(PlayerProfile p)
        {
            var s = new GameSession(p) { Role = Role.Solo, MaxPlayers = 1, LocalId = 1, Welcomed = true };
            s.AddLocal();
            s.World.Start();
            return s;
        }

        public static GameSession Host(PlayerProfile p, int port, int maxPlayers, int transport)
        {
            var s = new GameSession(p) { Role = Role.Host, MaxPlayers = Mathf.Clamp(maxPlayers, 2, Proto.MaxPlayers), LocalId = 1, Welcomed = true };
            UdpLink udp = null;
            TcpHostLink tcp = null;
            Exception first = null;
            if (transport != 2)
            {
                try { udp = UdpLink.Bind(port); }
                catch (Exception e) { first = e; }
            }
            if (transport != 1)
            {
                try { tcp = new TcpHostLink(port, Proto.MaxPlayers + 4); }
                catch (Exception e) { if (first == null) first = e; }
            }
            if (udp == null && tcp == null) throw first ?? new InvalidOperationException("no transport");
            var multi = new MultiLink(udp, tcp);
            s.link = multi;
            s.Port = udp != null ? udp.Port : port;
            s.AddLocal();
            return s;
        }

        public static GameSession Client(PlayerProfile p, string ip, int port, int transport, bool preferTcp)
        {
            var s = new GameSession(p) { Role = Role.Client };
            IPAddress addr;
            if (!IPAddress.TryParse(ip, out addr)) throw new ArgumentException("bad address");
            s.clientLink = new ClientLink(addr, port, transport, preferTcp);
            s.link = s.clientLink;
            s.Port = port;
            s.joinStarted = Time.realtimeSinceStartup;
            s.lastHeard = s.joinStarted;
            return s;
        }

        private void AddLocal()
        {
            PlayerState me = World.AddPlayer(LocalId, profile.Name, profile.Tier, profile.Badge, profile.Club, profile.Country, "host", profile.Uid);
            me.Alive = true;
            HostName = me.Name;
        }

        public void StartMatch()
        {
            if (Role == Role.Host && World.State == GameState.Lobby) World.Start();
        }

        public void Restart()
        {
            if (Role == Role.Host || Role == Role.Solo) World.Start();
        }

        public void Update(float dt, LocalInput input)
        {
            float now = Time.realtimeSinceStartup;
            if (Role == Role.Client) UpdateClient(dt, now, input);
            else UpdateAuthority(dt, input);
            if (World.State != lastState)
            {
                lastState = World.State;
                if (StateChanged != null) StateChanged(lastState);
            }
        }

        private void UpdateAuthority(float dt, LocalInput input)
        {
            PlayerState me = LocalPlayer;
            if (me != null)
            {
                me.Mx = input.Mx; me.Mz = input.Mz; me.Yaw = input.Yaw; me.Pitch = input.Pitch;
                me.Fire = input.Fire; me.Jump = input.Jump; me.Sprint = input.Sprint;
                me.LastSeen = World.Time;
            }
            if (link != null) Receive();
            accum += Mathf.Min(dt, 0.1f);
            while (accum >= Step)
            {
                accum -= Step;
                World.Step(Step);
                tick++;
            }
            DrainEvents();
            if (link == null) return;
            snapTimer += dt;
            if (snapTimer >= SnapEvery)
            {
                snapTimer = 0f;
                string s = Proto.Snapshot(World, tick), e = Proto.Enemies(World, tick);
                foreach (PlayerState p in World.Players.Values)
                    if (p.Peer != null) { link.Send(s, p.Peer); link.Send(e, p.Peer); }
            }
            var gone = new List<int>();
            foreach (PlayerState p in World.Players.Values)
                if (p.Peer != null && World.Time - p.LastSeen > 6f) gone.Add(p.Id);
            foreach (int id in gone) RemoveClient(id);
        }

        private void Broadcast(string text)
        {
            if (link == null) return;
            foreach (PlayerState p in World.Players.Values) if (p.Peer != null) link.Send(text, p.Peer);
        }

        private void DrainEvents()
        {
            foreach (TracerEvent t in World.Tracers)
            {
                if (Tracer != null) Tracer(t.Shooter, t.A, t.B, t.Hit);
                Broadcast(Proto.Tracer(t.Shooter, t.A, t.B, t.Hit));
            }
            World.Tracers.Clear();
            foreach (string text in World.Messages)
            {
                if (Message != null) Message(text);
                Broadcast(Proto.Message(text));
            }
            World.Messages.Clear();
        }

        private void RemoveClient(int id)
        {
            PlayerState p;
            if (!World.Players.TryGetValue(id, out p)) return;
            if (p.Peer != null) idByPeer.Remove(p.Peer);
            World.Players.Remove(id);
            World.Messages.Add("left;" + p.Name);
        }

        private void Receive()
        {
            string text, peer;
            int budget = 400;
            while (budget-- > 0 && link.TryReceive(out text, out peer))
            {
                string[] p = text.Split('|');
                if (p.Length == 0 || peer == null) continue;
                int id;
                if (p[0] == "J") { HandleJoin(p, peer); continue; }
                if (!idByPeer.TryGetValue(peer, out id)) continue;
                PlayerState pl;
                if (!World.Players.TryGetValue(id, out pl)) continue;
                pl.LastSeen = World.Time;
                if (p[0] == "I")
                {
                    Proto.InputMsg m;
                    if (!Proto.ParseInput(p, out m) || m.Seq <= pl.LastInputSeq) continue;
                    pl.LastInputSeq = m.Seq; pl.Mx = m.Mx; pl.Mz = m.Mz; pl.Yaw = m.Yaw; pl.Pitch = m.Pitch;
                    pl.Fire = m.Fire; pl.Jump = m.Jump; pl.Sprint = m.Sprint;
                }
                else if (p[0] == "P" && p.Length >= 2)
                {
                    link.Send("Q|" + p[1], peer);
                    int rtt;
                    if (p.Length >= 3 && Proto.TryI(p[2], out rtt)) pl.PingMs = Mathf.Clamp(rtt, -1, 9999);
                }
                else if (p[0] == "L") RemoveClient(id);
            }
        }

        private void HandleJoin(string[] p, string peer)
        {
            Proto.JoinMsg j;
            if (!Proto.ParseJoin(p, out j)) return;
            int id;
            if (idByPeer.TryGetValue(peer, out id)) { link.Send(Proto.Welcome(id, MaxPlayers, HostName), peer); return; }
            if (j.Uid.Length > 0)
            {
                foreach (PlayerState old in World.Players.Values)
                {
                    if (old.Peer == null || old.Uid != j.Uid || IpOf(old.Peer) != IpOf(peer)) continue;
                    idByPeer.Remove(old.Peer);
                    old.Peer = peer;
                    old.LastSeen = World.Time;
                    idByPeer[peer] = old.Id;
                    link.Send(Proto.Welcome(old.Id, MaxPlayers, HostName), peer);
                    return;
                }
            }
            if (j.Proto != Proto.Version) { link.Send("R|version", peer); return; }
            if (World.Players.Count >= MaxPlayers) { link.Send("R|full", peer); return; }
            id = nextId++;
            PlayerState pl = World.AddPlayer(id, j.Name, j.Tier, j.Badge, j.Club, j.Country, "member", j.Uid);
            pl.Peer = peer;
            pl.LastSeen = World.Time;
            if (World.State != GameState.Playing) pl.Alive = true;
            else { pl.Alive = false; pl.Hp = 0; pl.RespawnAt = World.Time + 1f; }
            idByPeer[peer] = id;
            link.Send(Proto.Welcome(id, MaxPlayers, HostName), peer);
            World.Messages.Add("joined;" + pl.Name);
        }

        private static string IpOf(string peer)
        {
            int a = peer.IndexOf(':'), b = peer.LastIndexOf(':');
            return a >= 0 && b > a ? peer.Substring(a + 1, b - a - 1) : peer;
        }

        private void UpdateClient(float dt, float now, LocalInput input)
        {
            if (Problem != null) return;
            if (clientLink != null) clientLink.Update(now);
            string text, peer;
            int budget = 400;
            while (budget-- > 0 && link.TryReceive(out text, out peer))
            {
                lastHeard = now;
                string[] p = text.Split('|');
                switch (p[0])
                {
                    case "W":
                        int id, ver;
                        if (p.Length >= 4 && Proto.TryI(p[1], out id) && Proto.TryI(p[2], out ver) && ver == Proto.Version)
                        {
                            LocalId = id;
                            Welcomed = true;
                            int mp;
                            if (Proto.TryI(p[3], out mp)) MaxPlayers = mp;
                            if (p.Length >= 5) HostName = TextUtil.CleanName(p[4]);
                            if (clientLink != null) clientLink.Lock();
                        }
                        break;
                    case "R":
                        Problem = p.Length > 1 && p[1] == "full" ? "The room is full." : p.Length > 1 && p[1] == "version" ? "The host runs a different version of the game." : "The host refused you.";
                        break;
                    case "S":
                        int t;
                        if (Proto.ApplySnapshot(p, World, out t)) OnSnapshot();
                        break;
                    case "E":
                        Proto.ApplyEnemies(p, World);
                        break;
                    case "B":
                        int sh;
                        Vector3 a, b;
                        bool hit;
                        if (Proto.ParseTracer(p, out sh, out a, out b, out hit) && Tracer != null) Tracer(sh, a, b, hit);
                        break;
                    case "M":
                        if (p.Length > 1 && Message != null) Message(p[1]);
                        break;
                    case "Q":
                        float sent;
                        if (p.Length > 1 && Proto.TryF(p[1], out sent)) PingMs = Mathf.Clamp(Mathf.RoundToInt((now - sent) * 1000f), 0, 9999);
                        break;
                }
            }
            if (!Welcomed)
            {
                joinTimer -= dt;
                if (joinTimer <= 0f)
                {
                    joinTimer = 0.5f;
                    link.Send(Proto.Join(profile.Name, profile.Tier, profile.Badge, profile.Club, profile.Country, profile.Uid), "host");
                }
                if (now - joinStarted > 12f) Problem = "The host did not answer. Is the room open and the address right?";
                return;
            }
            if (now - lastHeard > 6f) { Problem = "Lost the connection to the host."; return; }

            PlayerState me = LocalPlayer;
            if (predReady && me != null && me.Alive && World.State == GameState.Playing)
                World.PredictStep(ref predPos, ref predVy, input.Yaw, input.Mx, input.Mz, input.Sprint, input.Jump, dt);
            inputTimer += dt;
            if (inputTimer >= Step)
            {
                inputTimer = 0f;
                link.Send(Proto.Input(++seq, input.Mx, input.Mz, input.Yaw, input.Pitch, input.Fire, input.Jump, input.Sprint), "host");
            }
            pingTimer -= dt;
            if (pingTimer <= 0f)
            {
                pingTimer = 1f;
                link.Send("P|" + Proto.F(now) + "|" + PingMs, "host");
            }
        }

        private void OnSnapshot()
        {
            PlayerState me = LocalPlayer;
            if (me == null) return;
            bool bad = float.IsNaN(predPos.x) || float.IsNaN(predPos.y) || float.IsNaN(predPos.z);
            if (!predReady || bad || !me.Alive || (me.Pos - predPos).sqrMagnitude > 2.25f)
            {
                predPos = me.Pos;
                predVy = 0f;
                predReady = true;
            }
            else predPos = Vector3.Lerp(predPos, me.Pos, 0.12f);
        }

        public void Close()
        {
            if (link != null)
            {
                if (Role == Role.Client && Welcomed) link.Send("L", "host");
                link.Dispose();
                link = null;
                clientLink = null;
            }
        }
    }
}
