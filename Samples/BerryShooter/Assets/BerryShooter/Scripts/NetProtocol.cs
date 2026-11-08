using System.Globalization;
using System.Text;
using UnityEngine;

namespace BerryShooter
{
    public static class Proto
    {
        public const int Version = 2;
        public const int MaxPacket = 1200;
        public const int MaxPlayers = 8;
        public const int MaxEnemiesSent = 26;
        private static readonly CultureInfo C = CultureInfo.InvariantCulture;

        public static string F(float v) { return v.ToString("0.##", C); }
        public static bool TryF(string s, out float v) { return float.TryParse(s, NumberStyles.Float, C, out v) && !float.IsNaN(v) && !float.IsInfinity(v); }
        public static bool TryI(string s, out int v) { return int.TryParse(s, NumberStyles.Integer, C, out v); }

        public static string Join(string name, string tier, string badge, string club, string country, string uid) { return "J|" + TextUtil.CleanName(name) + "|" + TextUtil.Tag(tier, 10) + "|" + TextUtil.Tag(badge, 10) + "|" + TextUtil.Tag(club, 6) + "|" + Version + "|" + TextUtil.Tag(country, 2) + "|" + TextUtil.Tag(uid, 24); }

        public struct JoinMsg { public string Name, Tier, Badge, Club, Country, Uid; public int Proto; }
        public static bool ParseJoin(string[] p, out JoinMsg j)
        {
            j = new JoinMsg();
            if (p.Length < 8 || p[0] != "J") return false;
            int proto;
            if (!TryI(p[5], out proto)) return false;
            j.Name = TextUtil.CleanName(p[1]); j.Tier = TextUtil.Tag(p[2], 10); j.Badge = TextUtil.Tag(p[3], 10); j.Club = TextUtil.Tag(p[4], 6); j.Proto = proto; j.Country = TextUtil.Tag(p[6], 2); j.Uid = TextUtil.Tag(p[7], 24);
            return true;
        }

        public struct InputMsg { public int Seq; public float Mx, Mz, Yaw, Pitch; public bool Fire, Jump, Sprint; }
        public static string Input(int seq, float mx, float mz, float yaw, float pitch, bool fire, bool jump, bool sprint)
        {
            int flags = (fire ? 1 : 0) | (jump ? 2 : 0) | (sprint ? 4 : 0);
            return "I|" + seq + "|" + F(mx) + "|" + F(mz) + "|" + F(yaw) + "|" + F(pitch) + "|" + flags;
        }
        public static bool ParseInput(string[] p, out InputMsg m)
        {
            m = new InputMsg();
            int seq, flags; float mx, mz, yaw, pitch;
            if (p.Length < 7 || p[0] != "I") return false;
            if (!TryI(p[1], out seq) || !TryF(p[2], out mx) || !TryF(p[3], out mz) || !TryF(p[4], out yaw) || !TryF(p[5], out pitch) || !TryI(p[6], out flags)) return false;
            m.Seq = seq; m.Mx = Mathf.Clamp(mx, -1f, 1f); m.Mz = Mathf.Clamp(mz, -1f, 1f); m.Yaw = Mathf.Repeat(yaw, 360f); m.Pitch = Mathf.Clamp(pitch, -85f, 85f);
            m.Fire = (flags & 1) != 0; m.Jump = (flags & 2) != 0; m.Sprint = (flags & 4) != 0;
            return true;
        }

        public static string PlayerRecord(PlayerState p)
        {
            return p.Id + "," + p.Name + "," + F(p.Pos.x) + "," + F(p.Pos.y) + "," + F(p.Pos.z) + "," + F(p.Yaw) + "," + p.Hp + "," + p.Kills + "," + p.Deaths + "," + p.PingMs + "," + p.Tier + "," + p.Badge + "," + p.Club + "," + p.Country + "," + p.Role;
        }

        public static bool ParsePlayerRecord(string rec, out PlayerState p)
        {
            p = null;
            string[] f = rec.Split(',');
            int id, hp, kills, deaths, ping; float x, y, z, yaw;
            if (f.Length < 15) return false;
            if (!TryI(f[0], out id) || !TryF(f[2], out x) || !TryF(f[3], out y) || !TryF(f[4], out z) || !TryF(f[5], out yaw) || !TryI(f[6], out hp) || !TryI(f[7], out kills) || !TryI(f[8], out deaths) || !TryI(f[9], out ping)) return false;
            p = new PlayerState { Id = id, Name = TextUtil.CleanName(f[1]), Pos = new Vector3(x, y, z), Yaw = yaw, Hp = hp, Kills = kills, Deaths = deaths, PingMs = ping, Tier = TextUtil.Tag(f[10], 10), Badge = TextUtil.Tag(f[11], 10), Club = TextUtil.Tag(f[12], 6), Country = TextUtil.Tag(f[13], 2), Role = TextUtil.Tag(f[14], 8), Alive = hp > 0 };
            return true;
        }

        public static string Snapshot(World w, int tick)
        {
            var sb = new StringBuilder(512);
            sb.Append("S|").Append(tick).Append('|').Append((int)w.State).Append('|').Append(w.Wave).Append('|').Append(w.TeamKills).Append('|').Append(w.Remaining).Append('|');
            bool first = true;
            foreach (PlayerState p in w.Players.Values) { if (!first) sb.Append(';'); first = false; sb.Append(PlayerRecord(p)); }
            return sb.ToString();
        }

        public static bool ApplySnapshot(string[] p, World w, out int tick)
        {
            tick = 0;
            int state, wave, kills, left;
            if (p.Length < 7 || p[0] != "S") return false;
            if (!TryI(p[1], out tick) || !TryI(p[2], out state) || !TryI(p[3], out wave) || !TryI(p[4], out kills) || !TryI(p[5], out left)) return false;
            if (state < 0 || state > 2) return false;
            w.State = (GameState)state; w.Wave = wave; w.TeamKills = kills; w.Remaining = left;
            var seen = new System.Collections.Generic.HashSet<int>();
            foreach (string rec in p[6].Split(';'))
            {
                PlayerState np;
                if (rec.Length == 0 || !ParsePlayerRecord(rec, out np)) continue;
                seen.Add(np.Id);
                PlayerState old;
                if (w.Players.TryGetValue(np.Id, out old))
                {
                    old.Name = np.Name; old.Pos = np.Pos; old.Yaw = np.Yaw; old.Hp = np.Hp; old.Kills = np.Kills; old.Deaths = np.Deaths; old.PingMs = np.PingMs; old.Tier = np.Tier; old.Badge = np.Badge; old.Club = np.Club; old.Country = np.Country; old.Role = np.Role; old.Alive = np.Alive;
                }
                else w.Players[np.Id] = np;
            }
            var gone = new System.Collections.Generic.List<int>();
            foreach (int id in w.Players.Keys) if (!seen.Contains(id)) gone.Add(id);
            foreach (int id in gone) w.Players.Remove(id);
            return true;
        }

        public static string Enemies(World w, int tick)
        {
            var sb = new StringBuilder(512);
            sb.Append("E|").Append(tick).Append('|');
            int n = 0;
            foreach (EnemyState e in w.Enemies)
            {
                if (n >= MaxEnemiesSent) break;
                if (n > 0) sb.Append(';');
                sb.Append(e.Id).Append(',').Append(e.Type).Append(',').Append(F(e.Pos.x)).Append(',').Append(F(e.Pos.z)).Append(',').Append(e.Hp).Append(',').Append(F(e.Yaw));
                n++;
            }
            return sb.ToString();
        }

        public static bool ApplyEnemies(string[] p, World w)
        {
            if (p.Length < 3 || p[0] != "E") return false;
            var list = new System.Collections.Generic.List<EnemyState>();
            foreach (string rec in p[2].Split(';'))
            {
                string[] f = rec.Split(',');
                int id, type, hp; float x, z, yaw;
                if (f.Length < 6 || !TryI(f[0], out id) || !TryI(f[1], out type) || !TryF(f[2], out x) || !TryF(f[3], out z) || !TryI(f[4], out hp) || !TryF(f[5], out yaw)) continue;
                if (type < 0 || type > 2) continue;
                list.Add(new EnemyState { Id = id, Type = type, Pos = new Vector3(x, 0, z), Hp = hp, MaxHp = EnemyState.MaxHpFor(type), Yaw = yaw });
            }
            w.Enemies = list;
            return true;
        }

        public static string Tracer(int shooter, Vector3 a, Vector3 b, bool hit)
        {
            return "B|" + shooter + "|" + F(a.x) + "," + F(a.y) + "," + F(a.z) + "|" + F(b.x) + "," + F(b.y) + "," + F(b.z) + "|" + (hit ? 1 : 0);
        }

        public static bool ParseTracer(string[] p, out int shooter, out Vector3 a, out Vector3 b, out bool hit)
        {
            shooter = 0; a = b = Vector3.zero; hit = false;
            if (p.Length < 5 || p[0] != "B" || !TryI(p[1], out shooter) || !Vec(p[2], out a) || !Vec(p[3], out b)) return false;
            hit = p[4] == "1";
            return true;
        }

        private static bool Vec(string s, out Vector3 v)
        {
            v = Vector3.zero;
            string[] f = s.Split(',');
            float x, y, z;
            if (f.Length != 3 || !TryF(f[0], out x) || !TryF(f[1], out y) || !TryF(f[2], out z)) return false;
            v = new Vector3(x, y, z);
            return true;
        }

        public static string Message(string coded) { return "M|" + (coded ?? "").Replace("|", " "); }

        public static string Welcome(int id, int max, string hostName) { return "W|" + id + "|" + Version + "|" + max + "|" + TextUtil.CleanName(hostName); }
    }
}
