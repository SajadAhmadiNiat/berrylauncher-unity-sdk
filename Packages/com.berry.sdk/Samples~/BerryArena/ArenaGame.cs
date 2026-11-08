using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace BerrySdk.Samples
{
    public sealed class ArenaGame : MonoBehaviour
    {
        private const int Port = 7777;
        private UdpClient udp;
        private IPEndPoint hostEndpoint;
        private readonly List<IPEndPoint> guests = new List<IPEndPoint>();
        private readonly Dictionary<string, Vector2> others = new Dictionary<string, Vector2>();
        private Vector2 me = new Vector2(5, 5);
        private Vector2 dot = new Vector2(3, 3);
        private int score;
        private bool bigWin;
        private string status = "Starting...";
        private string tokenInfo = "";
        private float sendTimer;
        private string lanInfo = "";

        private void OnEnable()
        {
            Berry.Ready += OnReady;
            Berry.ContextChanged += OnContext;
            Berry.Pushed += OnPushed;
            Berry.Disconnected += OnDisconnected;
        }

        private void OnDisable()
        {
            Berry.Ready -= OnReady;
            Berry.ContextChanged -= OnContext;
            Berry.Pushed -= OnPushed;
            Berry.Disconnected -= OnDisconnected;
        }

        private void OnDestroy() { if (udp != null) udp.Close(); }

        private void OnReady()
        {
            status = Berry.IsMock ? "Running without the launcher (fake data)" : "Connected to Berry";
            StartNetwork();
            Berry.SetPresence("In the arena");
        }

        private void OnContext(BerryContext c) { status = "Room updated: " + c.Players.Count + " player(s)"; }

        private void OnPushed(string name, object data)
        {
            if (name == "player.joined") status = "A player joined the room";
        }

        private void OnDisconnected(string reason) { status = "Launcher connection closed (" + reason + "); the game goes on"; }

        private void StartNetwork()
        {
            try
            {
                var room = Berry.Room;
                if (room == null) return;
                if (room.Role == BerryRole.Host)
                {
                    udp = new UdpClient(new IPEndPoint(IPAddress.Any, Port));
                }
                else if (room.Role == BerryRole.Guest && !string.IsNullOrEmpty(room.HostAddress))
                {
                    udp = new UdpClient(0);
                    hostEndpoint = new IPEndPoint(IPAddress.Parse(room.HostAddress), Port);
                }
                if (udp != null) udp.Client.Blocking = false;
            }
            catch (Exception e) { status = "Network: " + e.Message; }
        }

        private void Update()
        {
            Vector2 move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            me += move * 4f * Time.deltaTime;
            me.x = Mathf.Clamp(me.x, 0, 10);
            me.y = Mathf.Clamp(me.y, 0, 10);
            if ((me - dot).sqrMagnitude < 0.25f) CollectDot();

            sendTimer += Time.deltaTime;
            if (udp != null && sendTimer > 0.05f) { sendTimer = 0; SendState(); }
            ReceiveState();
        }

        private void CollectDot()
        {
            score++;
            dot = new Vector2(UnityEngine.Random.Range(0.5f, 9.5f), UnityEngine.Random.Range(0.5f, 9.5f));
            Berry.SetPresence("Score " + score);
            Berry.SubmitScore("arena", score);
            if (score >= 10 && !bigWin) { bigWin = true; Berry.UnlockAchievement("ten_dots", "Ten dots!"); }
        }

        private string MyName { get { return Berry.User != null ? Berry.User.Username : "player"; } }

        private void SendState()
        {
            byte[] data = Encoding.UTF8.GetBytes(MyName + "|" + me.x.ToString("F2") + "|" + me.y.ToString("F2"));
            try
            {
                if (hostEndpoint != null) udp.Send(data, data.Length, hostEndpoint);
                else foreach (var g in guests) udp.Send(data, data.Length, g);
            }
            catch (SocketException) { }
        }

        private void ReceiveState()
        {
            if (udp == null) return;
            try
            {
                while (udp.Available > 0)
                {
                    IPEndPoint from = new IPEndPoint(IPAddress.Any, 0);
                    string[] p = Encoding.UTF8.GetString(udp.Receive(ref from)).Split('|');
                    if (p.Length != 3 || p[0] == MyName) continue;
                    float x, y;
                    if (!float.TryParse(p[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x)) continue;
                    if (!float.TryParse(p[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y)) continue;
                    others[p[0]] = new Vector2(Mathf.Clamp(x, 0, 10), Mathf.Clamp(y, 0, 10));
                    if (hostEndpoint == null && !guests.Exists(g => g.Equals(from)) && guests.Count < 16) guests.Add(from);
                }
            }
            catch (SocketException) { }
        }

        private async void AskForToken()
        {
            try
            {
                BerryIdentityToken t = await Berry.GetIdentityTokenAsync("berry-arena-server");
                tokenInfo = "Token OK (" + t.Token.Length + " chars), valid until " + t.ExpiresUtc.ToLocalTime().ToShortTimeString();
            }
            catch (BerryException e) { tokenInfo = "Token refused: " + e.Code; }
        }

        private async void HostLan()
        {
            try
            {
                var r = await Berry.HostLanAsync("Arena " + Berry.User.DisplayName, 4);
                lanInfo = "LAN room code: " + r.Code;
            }
            catch (BerryException e) { lanInfo = e.Code == "denied" ? "The player said no" : "LAN: " + e.Code; }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 420, 520), GUI.skin.box);
            GUILayout.Label("<b>Berry Arena</b>  -  " + status);
            if (Berry.IsReady)
            {
                GUILayout.Label("Player: " + (Berry.User != null ? Berry.User.DisplayName + " (" + Berry.User.Tier + ")" : "(no 'identity' permission)"));
                if (Berry.Room != null)
                    GUILayout.Label("Room: " + Berry.Room.Name + "  role: " + Berry.Room.Role + "  mode: " + Berry.Room.Mode + (Berry.Room.HostAddress.Length > 0 ? "  host: " + Berry.Room.HostAddress : ""));
                foreach (var p in Berry.Players) GUILayout.Label("  - " + p.DisplayName + (p.IsHost ? " (host)" : "") + (p.Address.Length > 0 ? "  " + p.Address : ""));
            }
            GUILayout.Label("Score: " + score + "   others: " + others.Count);
            if (GUILayout.Button("Ask Berry for an identity token")) AskForToken();
            if (tokenInfo.Length > 0) GUILayout.Label(tokenInfo);
            if (GUILayout.Button("Leave room")) Berry.LeaveRoom();
            if (Berry.Context.Has("network") && Berry.Network != null) GUILayout.Label("Virtual LAN: " + Berry.Network.State + " " + Berry.Network.Address + "  peers: " + Berry.Network.Peers.Count);
            if (Berry.Context.Has("profile") && Berry.Profile != null) GUILayout.Label("Badge: " + Berry.Profile.Badge + (Berry.Profile.Club.Length > 0 ? "  club: " + Berry.Profile.Club : ""));
            if (Berry.Context.Has("lan"))
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Host a LAN room")) HostLan();
                if (GUILayout.Button("Leave LAN room")) { var _ = Berry.LeaveLanAsync(); }
                GUILayout.EndHorizontal();
            }
            if (lanInfo.Length > 0) GUILayout.Label(lanInfo);
            GUILayout.EndArea();

            DrawDot(dot, Color.yellow);
            foreach (var kv in others) DrawDot(kv.Value, Color.cyan);
            DrawDot(me, Color.green);
        }

        private static void DrawDot(Vector2 p, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(460 + p.x * 40, 40 + (10 - p.y) * 40, 16, 16), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
