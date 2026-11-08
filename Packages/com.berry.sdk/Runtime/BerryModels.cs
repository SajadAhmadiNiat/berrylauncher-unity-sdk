using System;
using System.Collections.Generic;

namespace BerrySdk
{
    public enum BerryMode { Lan, VirtualLan, WireGuard, DirectIp, Direct, Dedicated }

    public enum BerryRole { None, Host, Guest }

    public enum BerryLinkState { Connected, Mock, LauncherOpenNoSession, LauncherClosed }

    public static class BerryText
    {
        public static string Clean(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new System.Text.StringBuilder(Math.Min(s.Length, max));
            foreach (char c in s)
            {
                if (sb.Length >= max) break;
                if (c < ' ' || c == '<' || c == '>' || c == '\u007f') continue;
                sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        public static string Str(object o, string key, int max, string def = "")
        {
            return Clean(BerryJson.Str(o, key, def), max);
        }

        public static DateTime FromMs(double ms)
        {
            if (ms <= 0 || ms > 4102444800000d) return DateTime.MinValue;
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ms);
        }
    }

    [Serializable]
    public sealed class BerryUser
    {
        public string Username = "";
        public string DisplayName = "";
        public string Tier = "free";
        public string BerryCode = "";
        public long UserId;
    }

    [Serializable]
    public sealed class BerryPlayerStats
    {
        public int NowMs = -1;
        public int AvgMs = -1;
        public int MinMs = -1;
        public int MaxMs = -1;
        public int JitterMs = -1;
        public double LossPercent = -1;
        public bool? Stable;

        public static BerryPlayerStats FromJson(Dictionary<string, object> j)
        {
            var s = new BerryPlayerStats();
            if (j == null) return s;
            s.NowMs = (int)BerryJson.Num(j, "nowMs", -1);
            s.AvgMs = (int)BerryJson.Num(j, "avgMs", -1);
            s.MinMs = (int)BerryJson.Num(j, "minMs", -1);
            s.MaxMs = (int)BerryJson.Num(j, "maxMs", -1);
            s.JitterMs = (int)BerryJson.Num(j, "jitterMs", -1);
            s.LossPercent = BerryJson.Num(j, "lossPct", -1);
            object st;
            if (j.TryGetValue("stable", out st) && st is bool) s.Stable = (bool)st;
            return s;
        }
    }

    [Serializable]
    public sealed class BerryConnection
    {
        public string Method = "";
        public string Phase = "";
        public bool Relay;
        public int RttMs = -1;
        public bool IsDirect { get { return Method == "direct" || Method == "udp"; } }
        public bool IsRelay { get { return Relay; } }
        public bool IsSelf { get { return Method == "self"; } }

        public static BerryConnection FromJson(Dictionary<string, object> j)
        {
            var c = new BerryConnection();
            if (j == null) return c;
            c.Method = BerryText.Str(j, "method", 12);
            c.Phase = BerryText.Str(j, "phase", 12);
            c.Relay = BerryJson.Bool(j, "relay");
            c.RttMs = (int)BerryJson.Num(j, "rttMs", -1);
            return c;
        }
    }

    [Serializable]
    public sealed class BerryPlayer
    {
        public string Username = "";
        public string DisplayName = "";
        public bool IsHost;
        public string Tier = "free";
        public string Address = "";
        public string Role = "member";
        public int AdminLevel;
        public List<string> AdminPerms = new List<string>();
        public bool Ready;
        public bool Muted;
        public bool Laggy;
        public bool InVoice;
        public string Country = "";
        public string BerryCode = "";
        public long UserId;
        public string Variant = "";
        public DateTime JoinedUtc = DateTime.MinValue;
        public int ServerPingMs = -1;
        public BerryPlayerStats Stats = new BerryPlayerStats();
        public bool IsSelf;
        public BerryConnection Connection = new BerryConnection();
        public DateTime ConnectedUtc = DateTime.MinValue;
        public bool IsReady { get { return Ready; } }
        public bool IsMuted { get { return Muted; } }
        public bool IsLaggy { get { return Laggy; } }
        public bool IsInVoice { get { return InVoice; } }
        public bool IsRelayed { get { return Connection != null && Connection.Relay; } }
        public bool IsOwner { get { return Role == "owner"; } }
        public bool IsAdmin { get { return Role == "admin" || AdminLevel > 0; } }

        public static BerryPlayer FromJson(object o)
        {
            var p = new BerryPlayer
            {
                Username = BerryText.Str(o, "username", 32),
                DisplayName = BerryText.Str(o, "displayName", 40),
                IsHost = BerryJson.Bool(o, "host"),
                Tier = BerryText.Str(o, "tier", 12, "free"),
                Address = BerryText.Str(o, "address", 40),
                Role = BerryText.Str(o, "role", 10, "member"),
                AdminLevel = (int)BerryJson.Num(o, "adminLevel"),
                Ready = BerryJson.Bool(o, "ready"),
                Muted = BerryJson.Bool(o, "muted"),
                Laggy = BerryJson.Bool(o, "laggy"),
                InVoice = BerryJson.Bool(o, "inVoice"),
                Country = BerryText.Str(o, "country", 2),
                BerryCode = BerryText.Str(o, "berryCode", 16),
                UserId = (long)BerryJson.Num(o, "userId"),
                Variant = BerryText.Str(o, "variant", 20),
                JoinedUtc = BerryText.FromMs(BerryJson.Num(o, "joinedAt")),
                ServerPingMs = (int)BerryJson.Num(o, "serverPingMs", -1),
                Stats = BerryPlayerStats.FromJson(BerryJson.Child(o, "stats")),
                IsSelf = BerryJson.Bool(o, "isSelf"),
                Connection = BerryConnection.FromJson(BerryJson.Child(o, "connection")),
                ConnectedUtc = BerryText.FromMs(BerryJson.Num(o, "connectedAt")),
            };
            var perms = BerryJson.List(o, "adminPerms");
            if (perms != null) foreach (object x in perms) { var s = x as string; if (s != null && p.AdminPerms.Count < 8) p.AdminPerms.Add(BerryText.Clean(s, 12)); }
            if (p.DisplayName.Length == 0) p.DisplayName = p.Username;
            return p;
        }
    }

    [Serializable]
    public sealed class BerryRoom
    {
        public string Id = "";
        public string Name = "";
        public string Code = "";
        public int MaxPlayers;
        public BerryRole Role = BerryRole.None;
        public BerryMode Mode = BerryMode.Lan;
        public string HostAddress = "";
        public string Visibility = "private";
        public bool Locked;
        public bool Free;
        public bool AdminOnly;
        public string Status = "";
        public string Kind = "";
        public string GameId = "";
        public string GameName = "";
        public string Owner = "";
        public string HostName = "";
        public int PlayerCount;
        public string Description = "";
        public List<string> Tags = new List<string>();
        public int MaxPing;
        public bool RequireReady = true;
        public string LaunchMode = "auto";
        public DateTime CreatedUtc = DateTime.MinValue;
        public DateTime OpenedUtc = DateTime.MinValue;
        public bool IsNetwork;
        public string Link = "";
        public string MyRole = "member";
        public int MyAdminLevel;
        public bool IsOpen;
        public bool InGame;
        public int MemberCount;
        public BerryRoomVoice Voice = new BerryRoomVoice();
        public bool Relay;
        public string RecommendedTransport = "udp";
        public bool IsPublic { get { return Visibility == "public"; } }
        public bool IsPrivate { get { return Visibility != "public"; } }
        public bool IsLocked { get { return Locked; } }
        public bool IsFree { get { return Free; } }
        public bool IsAdminOnly { get { return AdminOnly; } }
        public bool IsInGame { get { return InGame; } }
        public bool IsRelayed { get { return Relay; } }
        public bool IsHost { get { return Role == BerryRole.Host; } }
        public bool IsFull { get { return MaxPlayers > 0 && MemberCount >= MaxPlayers; } }
        public bool RequiresReady { get { return RequireReady; } }
        public bool IsVoiceActive { get { return Voice != null && Voice.Active; } }
    }

    [Serializable]
    public sealed class BerryRoomVoice
    {
        public bool Active;
        public bool Open;
        public int Count;
        public string Mode = "";
        public string Transport = "";
        public bool MeIn;
        public bool IsActive { get { return Active; } }
        public bool IsOpen { get { return Open; } }
        public bool IsMeIn { get { return MeIn; } }

        public static BerryRoomVoice FromJson(Dictionary<string, object> j)
        {
            var v = new BerryRoomVoice();
            if (j == null) return v;
            v.Active = BerryJson.Bool(j, "active");
            v.Open = BerryJson.Bool(j, "open");
            v.Count = (int)BerryJson.Num(j, "count");
            v.Mode = BerryText.Str(j, "mode", 12);
            v.Transport = BerryText.Str(j, "transport", 12);
            v.MeIn = BerryJson.Bool(j, "meIn");
            return v;
        }
    }

    [Serializable]
    public sealed class BerryGame
    {
        public string Id = "";
        public string Name = "";
        public string Path = "";
        public string InstalledVersion = "";
        public string Source = "unknown";
        public bool DownloadedFromBerry;
        public bool Defined;
        public bool Free;
        public int Port;
        public DateTime StartedUtc = DateTime.MinValue;
        public string Protocol = "";
        public bool IsDefined { get { return Defined; } }
        public bool IsFree { get { return Free; } }
        public bool IsDownloadedFromBerry { get { return DownloadedFromBerry; } }
        public bool UsesUdp { get { return Protocol == "udp" || Protocol == "both"; } }
        public bool UsesTcp { get { return Protocol == "tcp" || Protocol == "both"; } }
        public double RunningSeconds { get { return StartedUtc == DateTime.MinValue ? 0 : Math.Max(0, (DateTime.UtcNow - StartedUtc).TotalSeconds); } }

        public static BerryGame FromJson(Dictionary<string, object> j)
        {
            var g = new BerryGame();
            if (j == null) return g;
            g.Id = BerryText.Str(j, "id", 64);
            g.Name = BerryText.Str(j, "name", 80);
            g.Path = BerryText.Str(j, "path", 260);
            g.InstalledVersion = BerryText.Str(j, "installedVersion", 40);
            g.Source = BerryText.Str(j, "source", 12, "unknown");
            g.DownloadedFromBerry = BerryJson.Bool(j, "downloadedFromBerry");
            g.Defined = BerryJson.Bool(j, "defined");
            g.Free = BerryJson.Bool(j, "free");
            g.Port = (int)BerryJson.Num(j, "port");
            g.StartedUtc = BerryText.FromMs(BerryJson.Num(j, "startedAt"));
            g.Protocol = BerryText.Str(j, "protocol", 6);
            return g;
        }
    }

    [Serializable]
    public sealed class BerryProfile
    {
        public string Badge = "none";
        public string Club = "";
        public bool Online = true;
        public string Country = "";
        public string Plan = "free";
        public DateTime PlanUntilUtc = DateTime.MinValue;
        public DateTime PurpleUntilUtc = DateTime.MinValue;
        public DateTime GrayUntilUtc = DateTime.MinValue;
        public bool Staff;
        public string Gender = "";
        public string Bio = "";
        public string Language = "fa";
        public bool HasProfile;
        public int Followers;
        public DateTime SinceUtc = DateTime.MinValue;
        public bool IsStaff { get { return Staff; } }
        public bool IsAdmin { get { return Staff; } }
        public bool IsOnline { get { return Online; } }
        public bool IsPremium { get { return Badge != "none" && Badge != ""; } }

        public DateTime SubscriptionEndUtc
        {
            get
            {
                DateTime best = PlanUntilUtc;
                if (PurpleUntilUtc > best) best = PurpleUntilUtc;
                if (GrayUntilUtc > best) best = GrayUntilUtc;
                return best;
            }
        }

        public double DaysLeft
        {
            get
            {
                DateTime end = SubscriptionEndUtc;
                if (end == DateTime.MinValue) return 0;
                double d = (end - DateTime.UtcNow).TotalDays;
                return d < 0 ? 0 : d;
            }
        }
    }

    [Serializable]
    public sealed class BerryLauncher
    {
        public bool Open;
        public string Version = "";
        public string Build = "";
        public string Language = "fa";
        public bool LoggedIn;
        public bool Connected;
        public bool Official;
        public DateTime StartedUtc = DateTime.MinValue;
        public int UptimeSec;
        public bool HasSession;
        public int ActiveSessions;
        public string CoreVersion = "";
        public string Theme = "dark";
        public bool VoiceJoined;
        public string VoiceChannel = "";
        public bool IsOpen { get { return Open; } }
        public bool IsLoggedIn { get { return LoggedIn; } }
        public bool IsConnected { get { return Connected; } }
        public bool IsOfficial { get { return Official; } }
        public bool IsDark { get { return Theme != "light"; } }
        public bool IsInVoice { get { return VoiceJoined; } }

        public static BerryLauncher FromJson(Dictionary<string, object> j)
        {
            var l = new BerryLauncher();
            if (j == null) return l;
            l.Open = BerryJson.Bool(j, "open");
            l.Version = BerryText.Str(j, "version", 20);
            l.Build = BerryText.Str(j, "build", 12);
            l.Language = BerryJson.Str(j, "lang", "fa") == "en" ? "en" : "fa";
            l.LoggedIn = BerryJson.Bool(j, "loggedIn");
            l.Connected = BerryJson.Bool(j, "connected");
            l.Official = BerryJson.Bool(j, "official");
            l.StartedUtc = BerryText.FromMs(BerryJson.Num(j, "startedAt"));
            l.UptimeSec = (int)BerryJson.Num(j, "uptimeSec");
            l.HasSession = BerryJson.Bool(j, "hasSession");
            l.ActiveSessions = (int)BerryJson.Num(j, "activeSessions");
            l.CoreVersion = BerryText.Str(j, "core", 20);
            l.Theme = BerryJson.Str(j, "theme", "dark") == "light" ? "light" : "dark";
            var v = BerryJson.Child(j, "voice");
            l.VoiceJoined = BerryJson.Bool(v, "joined");
            l.VoiceChannel = BerryText.Str(v, "channel", 8);
            return l;
        }
    }

    [Serializable]
    public sealed class BerryPeer
    {
        public string Username = "";
        public string Address = "";
        public string Phase = "";
        public int PingMs = -1;
    }

    [Serializable]
    public sealed class BerryNetwork
    {
        public string State = "off";
        public string Address = "";
        public string Nat = "";
        public List<BerryPeer> Peers = new List<BerryPeer>();
        public bool IsRunning { get { return State == "running"; } }

        public static BerryNetwork FromJson(Dictionary<string, object> j)
        {
            if (j == null) return null;
            var n = new BerryNetwork { State = BerryText.Str(j, "state", 10, "off"), Address = BerryText.Str(j, "address", 40), Nat = BerryText.Str(j, "nat", 24) };
            var peers = BerryJson.List(j, "peers");
            if (peers != null)
                foreach (object o in peers)
                {
                    if (n.Peers.Count >= 64) break;
                    n.Peers.Add(new BerryPeer { Username = BerryText.Str(o, "username", 32), Address = BerryText.Str(o, "address", 40), Phase = BerryText.Str(o, "phase", 12), PingMs = (int)BerryJson.Num(o, "pingMs", -1) });
                }
            return n;
        }
    }

    public sealed class BerryRoomState
    {
        public bool InRoom;
        public string Id = "";
        public string Code = "";
        public string Name = "";
        public int MaxPlayers;
        public bool IsHost;
        public bool Requested;
        public BerryNetwork Network;

        public static BerryRoomState FromJson(Dictionary<string, object> j)
        {
            var r = new BerryRoomState();
            if (j == null) return r;
            r.InRoom = BerryJson.Bool(j, "inRoom"); r.Id = BerryText.Str(j, "id", 64); r.Code = BerryText.Str(j, "code", 12); r.Name = BerryText.Str(j, "name", 60);
            r.MaxPlayers = (int)BerryJson.Num(j, "maxPlayers"); r.IsHost = BerryJson.Bool(j, "host"); r.Requested = BerryJson.Bool(j, "requested");
            r.Network = BerryNetwork.FromJson(BerryJson.Child(j, "network"));
            return r;
        }
    }

    public sealed class BerryRoomInfo
    {
        public string Id = "";
        public string Name = "";
        public string Host = "";
        public int Players;
        public int MaxPlayers;
        public bool Locked;
        public bool Free;
        public string Status = "";
        public int PingMs = -1;
        public string GameId = "";
        public string GameName = "";
        public bool IsFull { get { return MaxPlayers > 0 && Players >= MaxPlayers; } }

        public static BerryRoomInfo FromJson(object o)
        {
            return new BerryRoomInfo
            {
                Id = BerryText.Str(o, "id", 64), Name = BerryText.Str(o, "name", 60), Host = BerryText.Str(o, "host", 32),
                Players = (int)BerryJson.Num(o, "players"), MaxPlayers = (int)BerryJson.Num(o, "maxPlayers"), Locked = BerryJson.Bool(o, "locked"),
                Free = BerryJson.Bool(o, "free"), Status = BerryText.Str(o, "status", 12), PingMs = (int)BerryJson.Num(o, "pingMs", -1),
                GameId = BerryText.Str(o, "gameId", 64), GameName = BerryText.Str(o, "gameName", 80),
            };
        }
    }

    public sealed class BerryFriend
    {
        public string Username = "";
        public string DisplayName = "";
        public bool Online;
        public bool Playing;
    }

    public sealed class BerryContext
    {
        public string GameId = "";
        public string GameName = "";
        public string Language = "fa";
        public string LauncherVersion = "";
        public BerryUser User;
        public BerryRoom Room;
        public List<BerryPlayer> Players = new List<BerryPlayer>();
        public List<string> Permissions = new List<string>();
        public BerryProfile Profile;
        public BerryNetwork Network;
        public BerryLauncher Launcher = new BerryLauncher();
        public string SdkVersion = "";
        public BerryGame Game = new BerryGame();
        public bool IsLoggedIn { get { return User != null && User.Username.Length > 0; } }
        public bool IsInRoom { get { return Room != null && Room.Role != BerryRole.None; } }
        public bool IsFreeConnection { get { return Game != null && Game.Free; } }

        public bool Has(string permission) { return Permissions.Contains(permission); }

        public BerryPlayer FindPlayer(string username)
        {
            foreach (var p in Players) if (p.Username == username) return p;
            return null;
        }

        public BerryPlayer Me
        {
            get { return User == null ? null : FindPlayer(User.Username); }
        }

        public static BerryContext FromJson(Dictionary<string, object> j)
        {
            var c = new BerryContext();
            if (j == null) return c;
            var g = BerryJson.Child(j, "game");
            c.GameId = BerryText.Str(g, "id", 64);
            c.GameName = BerryText.Str(g, "name", 80);
            c.Game = BerryGame.FromJson(g);
            var l = BerryJson.Child(j, "launcher");
            c.Language = BerryJson.Str(l, "lang", "fa") == "en" ? "en" : "fa";
            c.LauncherVersion = BerryText.Str(l, "version", 20);
            c.Launcher = BerryLauncher.FromJson(l);
            c.Launcher.HasSession = true;
            c.SdkVersion = BerryText.Str(BerryJson.Child(j, "sdk"), "version", 12);
            var pr = BerryJson.Child(j, "profile");
            if (pr != null)
                c.Profile = new BerryProfile
                {
                    Badge = BerryText.Str(pr, "badge", 8, "none"), Club = BerryText.Str(pr, "club", 6), Online = BerryJson.Bool(pr, "online", true),
                    Country = BerryText.Str(pr, "country", 2), Plan = BerryText.Str(pr, "plan", 12, "free"),
                    PlanUntilUtc = BerryText.FromMs(BerryJson.Num(pr, "planUntil")), PurpleUntilUtc = BerryText.FromMs(BerryJson.Num(pr, "purpleUntil")), GrayUntilUtc = BerryText.FromMs(BerryJson.Num(pr, "grayUntil")),
                    HasProfile = BerryJson.Bool(pr, "hasProfile"), Followers = (int)BerryJson.Num(pr, "followers"), SinceUtc = BerryText.FromMs(BerryJson.Num(pr, "since")),
                    Staff = BerryJson.Bool(pr, "staff"), Gender = BerryText.Str(pr, "gender", 8), Bio = BerryText.Str(pr, "bio", 200), Language = BerryJson.Str(pr, "lang", "fa") == "en" ? "en" : "fa",
                };
            c.Network = BerryNetwork.FromJson(BerryJson.Child(j, "network"));
            var perms = BerryJson.List(j, "permissions");
            if (perms != null) foreach (object p in perms) { var ps = p as string; if (ps != null && c.Permissions.Count < 16) c.Permissions.Add(BerryText.Clean(ps, 20)); }
            var id = BerryJson.Child(j, "identity");
            if (id != null)
                c.User = new BerryUser
                {
                    Username = BerryText.Str(id, "username", 32), DisplayName = BerryText.Str(id, "displayName", 40), Tier = BerryText.Str(id, "tier", 12, "free"),
                    BerryCode = BerryText.Str(id, "berryCode", 16), UserId = (long)BerryJson.Num(id, "userId"),
                };
            var r = BerryJson.Child(j, "room");
            if (r != null)
            {
                string role = BerryJson.Str(r, "role");
                string mode = BerryJson.Str(r, "mode");
                var room = new BerryRoom
                {
                    Id = BerryText.Str(r, "id", 64), Name = BerryText.Str(r, "name", 60), Code = BerryText.Str(r, "code", 12),
                    MaxPlayers = (int)BerryJson.Num(r, "maxPlayers"),
                    Role = role == "host" ? BerryRole.Host : role == "guest" ? BerryRole.Guest : BerryRole.None,
                    Mode = mode == "vlan" ? BerryMode.VirtualLan : mode == "wireguard" ? BerryMode.WireGuard : mode == "ip" ? BerryMode.DirectIp
                         : mode == "direct" ? BerryMode.Direct : mode == "dedicated" ? BerryMode.Dedicated : BerryMode.Lan,
                    HostAddress = BerryText.Str(r, "hostAddress", 40),
                    Visibility = BerryJson.Str(r, "visibility", "private") == "public" ? "public" : "private",
                    Locked = BerryJson.Bool(r, "locked"), Free = BerryJson.Bool(r, "free"), AdminOnly = BerryJson.Bool(r, "adminOnly"),
                    Status = BerryText.Str(r, "status", 12), Kind = BerryText.Str(r, "kind", 12), GameId = BerryText.Str(r, "gameId", 64), GameName = BerryText.Str(r, "gameName", 80),
                    Owner = BerryText.Str(r, "owner", 32), HostName = BerryText.Str(r, "hostName", 32), PlayerCount = (int)BerryJson.Num(r, "playerCount"),
                    Description = BerryText.Str(r, "description", 200), MaxPing = (int)BerryJson.Num(r, "maxPing"), RequireReady = BerryJson.Bool(r, "requireReady", true),
                    LaunchMode = BerryJson.Str(r, "launchMode", "auto") == "menu" ? "menu" : "auto",
                    CreatedUtc = BerryText.FromMs(BerryJson.Num(r, "createdAt")), OpenedUtc = BerryText.FromMs(BerryJson.Num(r, "openedAt")),
                    IsNetwork = BerryJson.Bool(r, "network"), Link = BerryText.Str(r, "link", 120), MyRole = BerryText.Str(r, "myRole", 10, "member"), MyAdminLevel = (int)BerryJson.Num(r, "myAdminLevel"),
                    IsOpen = BerryJson.Bool(r, "isOpen"), InGame = BerryJson.Bool(r, "inGame"), MemberCount = (int)BerryJson.Num(r, "memberCount"),
                    Voice = BerryRoomVoice.FromJson(BerryJson.Child(r, "voice")), Relay = BerryJson.Bool(r, "relay"),
                    RecommendedTransport = BerryJson.Str(r, "recommendedTransport", "udp") == "tcp" ? "tcp" : "udp",
                };
                var tags = BerryJson.List(r, "tags");
                if (tags != null) foreach (object t in tags) { var ts = t as string; if (ts != null && room.Tags.Count < 8) room.Tags.Add(BerryText.Clean(ts, 20)); }
                c.Room = room;
            }
            var pl = BerryJson.List(j, "players");
            if (pl != null)
                foreach (object o in pl)
                {
                    if (c.Players.Count >= 64) break;
                    c.Players.Add(BerryPlayer.FromJson(o));
                }
            return c;
        }
    }

    public sealed class BerryProfileInfo
    {
        public string Username = "";
        public string DisplayName = "";
        public string BerryCode = "";
        public string Tier = "free";
        public string Bio = "";
        public DateTime SinceUtc = DateTime.MinValue;
        public bool Online;
        public bool HasProfile;
        public int Followers;
        public bool FollowedByMe;
        public int AchievementCount;
        public int Points = -1;
        public string Club = "";
        public bool IsOnline { get { return Online; } }
        public bool IsFollowedByMe { get { return FollowedByMe; } }
        public bool InClub { get { return Club.Length > 0; } }

        public static BerryProfileInfo FromJson(Dictionary<string, object> j)
        {
            var p = new BerryProfileInfo();
            if (j == null) return p;
            p.Username = BerryText.Str(j, "username", 32); p.DisplayName = BerryText.Str(j, "displayName", 40); p.BerryCode = BerryText.Str(j, "berryCode", 16);
            p.Tier = BerryText.Str(j, "tier", 12, "free"); p.Bio = BerryText.Str(j, "bio", 200); p.SinceUtc = BerryText.FromMs(BerryJson.Num(j, "since"));
            p.Online = BerryJson.Bool(j, "online"); p.HasProfile = BerryJson.Bool(j, "hasProfile"); p.Followers = (int)BerryJson.Num(j, "followers");
            p.FollowedByMe = BerryJson.Bool(j, "followedByMe"); p.AchievementCount = (int)BerryJson.Num(j, "achievementCount");
            p.Points = (int)BerryJson.Num(j, "points", -1); p.Club = BerryText.Str(j, "club", 6);
            return p;
        }
    }

    public sealed class BerryFollowResult
    {
        public string A = "";
        public string B = "";
        public bool Follows;
        public bool FollowedBy;
        public bool IsFollowing { get { return Follows; } }
        public bool IsFollowedBack { get { return FollowedBy; } }
        public bool IsMutual { get { return Follows && FollowedBy; } }

        public static BerryFollowResult FromJson(Dictionary<string, object> j)
        {
            var r = new BerryFollowResult();
            if (j == null) return r;
            r.A = BerryText.Str(j, "a", 32); r.B = BerryText.Str(j, "b", 32);
            r.Follows = BerryJson.Bool(j, "follows"); r.FollowedBy = BerryJson.Bool(j, "followedBy");
            return r;
        }
    }

    public sealed class BerryAchievement
    {
        public string Id = "";
        public string Title = "";
        public DateTime AtUtc = DateTime.MinValue;
        public int Progress;
        public int Goal;
        public bool Unlocked;
        public bool IsUnlocked { get { return Unlocked; } }
        public bool Has { get { return Unlocked; } }

        public static BerryAchievement FromJson(object o)
        {
            return new BerryAchievement
            {
                Id = BerryText.Str(o, "id", 40), Title = BerryText.Str(o, "title", 60), AtUtc = BerryText.FromMs(BerryJson.Num(o, "at")),
                Progress = (int)BerryJson.Num(o, "progress"), Goal = (int)BerryJson.Num(o, "goal"), Unlocked = BerryJson.Bool(o, "unlocked"),
            };
        }
    }

    public sealed class BerryClubMember
    {
        public string Username = "";
        public string DisplayName = "";
        public string Role = "";
        public bool Online;
        public bool IsOnline { get { return Online; } }
        public bool IsLeader { get { return Role == "owner" || Role == "leader"; } }
    }

    public sealed class BerryClubInfo
    {
        public bool InClub;
        public int Id;
        public string Name = "";
        public string Tag = "";
        public string Handle = "";
        public string Slogan = "";
        public string Description = "";
        public string Visibility = "public";
        public bool JoinClosed;
        public bool HasPassword;
        public int MemberCount;
        public int MaxMembers;
        public string MyRole = "";
        public List<BerryClubMember> Members = new List<BerryClubMember>();
        public bool IsInClub { get { return InClub; } }
        public bool IsPublic { get { return Visibility == "public"; } }
        public bool IsJoinClosed { get { return JoinClosed; } }
        public bool IsFull { get { return MaxMembers > 0 && MemberCount >= MaxMembers; } }

        public static BerryClubInfo FromJson(Dictionary<string, object> j)
        {
            var c = new BerryClubInfo();
            if (j == null) return c;
            c.InClub = BerryJson.Bool(j, "inClub");
            if (!c.InClub) return c;
            c.Id = (int)BerryJson.Num(j, "id"); c.Name = BerryText.Str(j, "name", 24); c.Tag = BerryText.Str(j, "tag", 6); c.Handle = BerryText.Str(j, "handle", 24);
            c.Slogan = BerryText.Str(j, "slogan", 80); c.Description = BerryText.Str(j, "description", 200);
            c.Visibility = BerryJson.Str(j, "visibility", "public") == "private" ? "private" : "public";
            c.JoinClosed = BerryJson.Bool(j, "joinClosed"); c.HasPassword = BerryJson.Bool(j, "hasPassword");
            c.MemberCount = (int)BerryJson.Num(j, "memberCount"); c.MaxMembers = (int)BerryJson.Num(j, "maxMembers"); c.MyRole = BerryText.Str(j, "myRole", 10);
            var l = BerryJson.List(j, "members");
            if (l != null)
                foreach (object o in l)
                {
                    if (c.Members.Count >= 50) break;
                    c.Members.Add(new BerryClubMember { Username = BerryText.Str(o, "username", 32), DisplayName = BerryText.Str(o, "displayName", 40), Role = BerryText.Str(o, "role", 10), Online = BerryJson.Bool(o, "online") });
                }
            return c;
        }
    }

    public sealed class BerryTournament
    {
        public int Id;
        public string Name = "";
        public string Mode = "solo";
        public string GameId = "";
        public string GameName = "";
        public string Organizer = "";
        public string Status = "";
        public string Format = "";
        public int Size;
        public int PlayerCount;
        public DateTime StartsUtc = DateTime.MinValue;
        public DateTime EndedUtc = DateTime.MinValue;
        public string Visibility = "public";
        public string Winner = "";
        public int BestOf = 1;
        public string Description = "";
        public bool IsPublic { get { return Visibility == "public"; } }
        public bool IsGroup { get { return Mode == "group"; } }
        public bool IsFinished { get { return EndedUtc != DateTime.MinValue || Status == "ended" || Status == "finished"; } }
        public bool IsFull { get { return Size > 0 && PlayerCount >= Size; } }

        public static BerryTournament FromJson(object o)
        {
            return new BerryTournament
            {
                Id = (int)BerryJson.Num(o, "id"), Name = BerryText.Str(o, "name", 60), Mode = BerryJson.Str(o, "mode", "solo") == "group" ? "group" : "solo",
                GameId = BerryText.Str(o, "gameId", 64), GameName = BerryText.Str(o, "gameName", 80), Organizer = BerryText.Str(o, "organizer", 32),
                Status = BerryText.Str(o, "status", 12), Format = BerryText.Str(o, "format", 12), Size = (int)BerryJson.Num(o, "size"), PlayerCount = (int)BerryJson.Num(o, "playerCount"),
                StartsUtc = BerryText.FromMs(BerryJson.Num(o, "startsAt")), EndedUtc = BerryText.FromMs(BerryJson.Num(o, "endedAt")),
                Visibility = BerryJson.Str(o, "visibility", "public") == "private" ? "private" : "public", Winner = BerryText.Str(o, "winner", 40),
                BestOf = (int)BerryJson.Num(o, "bestOf", 1), Description = BerryText.Str(o, "description", 200),
            };
        }
    }

    public sealed class BerryAccountInfo
    {
        public string Username = "";
        public string DisplayName = "";
        public string BerryCode = "";
        public int Coins = -1;
        public string Plan = "free";
        public DateTime PlanUntilUtc = DateTime.MinValue;
        public DateTime PurpleUntilUtc = DateTime.MinValue;
        public DateTime GrayUntilUtc = DateTime.MinValue;
        public bool Staff;
        public string Country = "";
        public string Gender = "";
        public bool HasProfile;
        public int Followers;
        public DateTime SinceUtc = DateTime.MinValue;
        public bool IsStaff { get { return Staff; } }
        public bool IsAdmin { get { return Staff; } }
        public bool IsPaid { get { return Plan != "free"; } }

        public static BerryAccountInfo FromJson(Dictionary<string, object> j)
        {
            var a = new BerryAccountInfo();
            if (j == null) return a;
            a.Username = BerryText.Str(j, "username", 32); a.DisplayName = BerryText.Str(j, "displayName", 40); a.BerryCode = BerryText.Str(j, "berryCode", 16);
            a.Coins = (int)BerryJson.Num(j, "coins", -1); a.Plan = BerryText.Str(j, "plan", 12, "free");
            a.PlanUntilUtc = BerryText.FromMs(BerryJson.Num(j, "planUntil")); a.PurpleUntilUtc = BerryText.FromMs(BerryJson.Num(j, "purpleUntil")); a.GrayUntilUtc = BerryText.FromMs(BerryJson.Num(j, "grayUntil"));
            a.Staff = BerryJson.Bool(j, "staff"); a.Country = BerryText.Str(j, "country", 2); a.Gender = BerryText.Str(j, "gender", 8);
            a.HasProfile = BerryJson.Bool(j, "hasProfile"); a.Followers = (int)BerryJson.Num(j, "followers"); a.SinceUtc = BerryText.FromMs(BerryJson.Num(j, "since"));
            return a;
        }
    }

    public sealed class BerrySettingsInfo
    {
        public string Language = "fa";
        public string Theme = "dark";
        public bool Notifications = true;
        public bool NotificationSound = true;
        public bool UiSounds = true;
        public bool AutoJoin = true;
        public bool BetaUpdates;
        public bool IsDark { get { return Theme != "light"; } }
        public bool AreNotificationsOn { get { return Notifications; } }
        public bool IsAutoJoinOn { get { return AutoJoin; } }
        public bool IsBetaUpdatesOn { get { return BetaUpdates; } }

        public static BerrySettingsInfo FromJson(Dictionary<string, object> j)
        {
            var s = new BerrySettingsInfo();
            if (j == null) return s;
            s.Language = BerryJson.Str(j, "language", "fa") == "en" ? "en" : "fa";
            s.Theme = BerryJson.Str(j, "theme", "dark") == "light" ? "light" : "dark";
            s.Notifications = BerryJson.Bool(j, "notifications", true); s.NotificationSound = BerryJson.Bool(j, "notificationSound", true);
            s.UiSounds = BerryJson.Bool(j, "uiSounds", true); s.AutoJoin = BerryJson.Bool(j, "autoJoin", true); s.BetaUpdates = BerryJson.Bool(j, "betaUpdates");
            return s;
        }
    }

    public sealed class BerryIdentityToken
    {
        public string Token = "";
        public DateTime ExpiresUtc;
    }

    public sealed class BerryException : Exception
    {
        public readonly string Code;
        public BerryException(string code) : base(code) { Code = code; }
    }
}
