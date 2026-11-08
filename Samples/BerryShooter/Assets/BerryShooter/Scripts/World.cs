using System.Collections.Generic;
using UnityEngine;

namespace BerryShooter
{
    public enum GameState { Lobby = 0, Playing = 1, Over = 2 }

    public sealed class PlayerState
    {
        public int Id;
        public string Name = "Player", Tier = "free", Badge = "none", Club = "", Country = "", Role = "member", Uid = "";
        public Vector3 Pos;
        public float Yaw, Pitch, Vy;
        public int Hp = 100, Kills, Deaths, PingMs = -1;
        public bool Alive = true;
        public float RespawnAt;
        public float Mx, Mz;
        public bool Fire, Jump, Sprint;
        public float NextFire, LastSeen;
        public int LastInputSeq = -1;
        public string Peer;
        public bool IsBerryPlayer;
    }

    public sealed class EnemyState
    {
        public int Id, Type, Hp, MaxHp;
        public Vector3 Pos;
        public float Yaw, NextAttack;

        public static int MaxHpFor(int type) { return type == 1 ? 120 : type == 2 ? 40 : 30; }
        public static float SpeedFor(int type) { return type == 1 ? 2.0f : type == 2 ? 2.6f : 3.6f; }
        public static float RadiusFor(int type) { return type == 1 ? 1.0f : 0.6f; }
        public static float HeightFor(int type) { return type == 1 ? 2.0f : type == 2 ? 1.8f : 1.2f; }
    }

    public sealed class TracerEvent { public int Shooter; public Vector3 A, B; public bool Hit; }

    public sealed class World
    {
        public const float PlayerSpeed = 6f, SprintSpeed = 9f, JumpSpeed = 7f, Gravity = -22f, EyeHeight = 1.6f;
        public const float FireInterval = 0.16f, Damage = 25f;

        public readonly Dictionary<int, PlayerState> Players = new Dictionary<int, PlayerState>();
        public List<EnemyState> Enemies = new List<EnemyState>();
        public GameState State = GameState.Lobby;
        public int Wave, TeamKills, Remaining;
        public float Time;

        public readonly List<TracerEvent> Tracers = new List<TracerEvent>();
        public readonly List<string> Messages = new List<string>();

        private readonly System.Random rng;
        private int nextEnemyId = 1;
        private int toSpawn;
        private float nextSpawn, nextWaveAt;

        public World(int seed) { rng = new System.Random(seed); }

        public PlayerState AddPlayer(int id, string name, string tier, string badge, string club, string country = "", string role = "member", string uid = "")
        {
            var p = new PlayerState { Id = id, Name = UniqueName(TextUtil.CleanName(name), id), Tier = tier ?? "free", Badge = badge ?? "none", Club = club ?? "", Country = country ?? "", Role = role ?? "member", Uid = uid ?? "" };
            p.Pos = ArenaLayout.SpawnPoint(id);
            p.LastSeen = Time;
            Players[id] = p;
            return p;
        }

        private string UniqueName(string name, int id)
        {
            string candidate = name;
            int n = 2;
            while (NameTaken(candidate, id)) candidate = TextUtil.CleanName(name.Length > 14 ? name.Substring(0, 14) : name) + " " + n++;
            return candidate;
        }

        private bool NameTaken(string name, int id)
        {
            foreach (PlayerState p in Players.Values) if (p.Id != id && p.Name == name) return true;
            return false;
        }

        public void Start()
        {
            Enemies.Clear(); Tracers.Clear();
            Wave = 0; TeamKills = 0; Remaining = 0; toSpawn = 0; nextWaveAt = Time + 2.5f;
            State = GameState.Playing;
            foreach (PlayerState p in Players.Values) { Respawn(p); p.Kills = 0; p.Deaths = 0; }
            Messages.Add("ready");
        }

        private void Respawn(PlayerState p)
        {
            p.Hp = 100; p.Alive = true; p.Vy = 0; p.Pos = ArenaLayout.SpawnPoint(p.Id + rng.Next(8));
        }

        public static void Move(ref Vector3 pos, ref float vy, float yaw, float mx, float mz, bool sprint, bool jump, float dt)
        {
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * new Vector3(mx, 0f, mz);
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            pos += dir * (sprint ? SprintSpeed : PlayerSpeed) * dt;
            if (jump && pos.y <= 0.001f) vy = JumpSpeed;
            vy += Gravity * dt;
            pos.y += vy * dt;
            if (pos.y <= 0f) { pos.y = 0f; vy = 0f; }
            Vector3 r = ArenaLayout.Resolve(pos, ArenaLayout.PlayerRadius);
            pos.x = r.x; pos.z = r.z;
        }

        public void PredictStep(ref Vector3 pos, ref float vy, float yaw, float mx, float mz, bool sprint, bool jump, float dt) { Move(ref pos, ref vy, yaw, mx, mz, sprint, jump, dt); }

        public void Step(float dt)
        {
            Time += dt;
            if (State != GameState.Playing) return;
            int alive = 0;
            foreach (PlayerState p in Players.Values)
            {
                if (!p.Alive) continue;
                alive++;
                float vy = p.Vy; Vector3 pos = p.Pos;
                Move(ref pos, ref vy, p.Yaw, p.Mx, p.Mz, p.Sprint, p.Jump, dt);
                p.Pos = pos; p.Vy = vy;
                if (p.Fire && Time >= p.NextFire) { p.NextFire = Time + FireInterval; Shoot(p); }
            }
            StepWaves();
            StepEnemies(dt);
            foreach (PlayerState p in Players.Values)
                if (!p.Alive && Time >= p.RespawnAt && AnyAlive()) { Respawn(p); Messages.Add("back;" + p.Name); }
            if (!AnyAlive() && Players.Count > 0) { State = GameState.Over; Messages.Add("over;" + Wave); }
            Remaining = toSpawn + Enemies.Count;
        }

        private bool AnyAlive() { foreach (PlayerState p in Players.Values) if (p.Alive) return true; return false; }

        private void StepWaves()
        {
            if (toSpawn > 0 && Time >= nextSpawn)
            {
                nextSpawn = Time + 0.9f;
                toSpawn--;
                SpawnEnemy();
            }
            else if (toSpawn == 0 && Enemies.Count == 0 && Time >= nextWaveAt)
            {
                Wave++;
                toSpawn = 5 + 4 * Wave;
                nextSpawn = Time + 1f;
                nextWaveAt = Time + 4f;
                Messages.Add("wave;" + Wave);
                foreach (PlayerState p in Players.Values) { if (p.Alive) p.Hp = Mathf.Min(100, p.Hp + 30); else p.RespawnAt = Time; }
            }
        }

        private void SpawnEnemy()
        {
            int type = 0;
            double r = rng.NextDouble();
            if (Wave >= 3 && r < 0.15) type = 1; else if (Wave >= 2 && r < 0.4) type = 2;
            float a = (float)(rng.NextDouble() * Mathf.PI * 2.0), rad = ArenaLayout.Half - 1.5f;
            var pos = ArenaLayout.Resolve(new Vector3(Mathf.Cos(a) * rad, 0, Mathf.Sin(a) * rad), 0.8f);
            int hp = EnemyState.MaxHpFor(type) + Wave * 3;
            Enemies.Add(new EnemyState { Id = nextEnemyId++, Type = type, Pos = pos, Hp = hp, MaxHp = hp, NextAttack = Time + 1f });
        }

        private PlayerState NearestAlive(Vector3 from, out float dist)
        {
            PlayerState best = null; dist = float.MaxValue;
            foreach (PlayerState p in Players.Values)
            {
                if (!p.Alive) continue;
                float d = (p.Pos - from).magnitude;
                if (d < dist) { dist = d; best = p; }
            }
            return best;
        }

        private void StepEnemies(float dt)
        {
            foreach (EnemyState e in Enemies)
            {
                float dist;
                PlayerState target = NearestAlive(e.Pos, out dist);
                if (target == null) continue;
                Vector3 to = target.Pos - e.Pos; to.y = 0f;
                if (to.sqrMagnitude > 0.0001f) e.Yaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
                float stop = e.Type == 2 ? 11f : 1.1f;
                if (dist > stop)
                {
                    Vector3 np = e.Pos + to.normalized * EnemyState.SpeedFor(e.Type) * dt;
                    e.Pos = ArenaLayout.Resolve(np, EnemyState.RadiusFor(e.Type));
                }
                if (Time < e.NextAttack) continue;
                if (e.Type == 2)
                {
                    if (dist < 18f)
                    {
                        e.NextAttack = Time + 1.3f;
                        Vector3 o = e.Pos + Vector3.up * 1.4f, tgt = target.Pos + Vector3.up * 1.2f, d = (tgt - o).normalized;
                        float t = ArenaLayout.RayToCover(o, d, (tgt - o).magnitude);
                        bool clear = t >= (tgt - o).magnitude - 0.01f;
                        bool hit = clear && rng.NextDouble() < 0.6;
                        Tracers.Add(new TracerEvent { Shooter = -1, A = o, B = hit ? tgt : o + d * Mathf.Min(t, 30f), Hit = hit });
                        if (hit) Hurt(target, 8);
                    }
                }
                else if (dist < (e.Type == 1 ? 2.1f : 1.7f))
                {
                    e.NextAttack = Time + (e.Type == 1 ? 1.1f : 0.8f);
                    Hurt(target, e.Type == 1 ? 22 : 10);
                }
            }
        }

        private void Hurt(PlayerState p, int dmg)
        {
            if (!p.Alive) return;
            p.Hp -= dmg;
            if (p.Hp <= 0) { p.Hp = 0; p.Alive = false; p.Deaths++; p.RespawnAt = Time + 6f; Messages.Add("down;" + p.Name); }
        }

        private void Shoot(PlayerState p)
        {
            Vector3 o = p.Pos + new Vector3(0f, EyeHeight, 0f);
            Vector3 d = Quaternion.Euler(p.Pitch, p.Yaw, 0f) * Vector3.forward;
            float maxT = ArenaLayout.RayToCover(o, d, 70f);
            EnemyState hit = null; float hitT = maxT;
            foreach (EnemyState e in Enemies)
            {
                Vector3 c = e.Pos + new Vector3(0f, EnemyState.HeightFor(e.Type) * 0.5f, 0f);
                float r = EnemyState.RadiusFor(e.Type) + 0.15f, t = Vector3.Dot(c - o, d);
                if (t < 0f || t > hitT) continue;
                if ((o + d * t - c).sqrMagnitude <= r * r) { hit = e; hitT = t; }
            }
            if (hit != null)
            {
                hit.Hp -= (int)Damage;
                if (hit.Hp <= 0)
                {
                    Enemies.Remove(hit);
                    p.Kills++; TeamKills++;
                    Messages.Add("kill;" + p.Name + ";" + hit.Type);
                }
            }
            Tracers.Add(new TracerEvent { Shooter = p.Id, A = o + d * 0.6f + Vector3.down * 0.2f, B = o + d * hitT, Hit = hit != null });
        }
    }
}
