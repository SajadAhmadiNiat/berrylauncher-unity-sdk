using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace BerrySdk
{
    [Serializable]
    public sealed class BerryOptions
    {
        public string GameId = "";
        public string GameCode = "";
        public long GameNumber;
        public bool MockWhenStandalone = true;
        public BerryContext MockContext;
        public string MockName = "";
        public bool MockIsHost = true;
        public bool ProbeLauncher = true;
    }

    public static class Berry
    {
        private static BerryClient client;
        private static BerryContext context = new BerryContext();
        private static bool mock;
        private static bool ready;
        private static BerryOptions lastOptions;
        private static BerryLauncher probed = new BerryLauncher();

        public static bool IsReady { get { return ready; } }
        public static bool IsMock { get { return mock; } }
        public static bool IsConnected { get { return mock || (client != null && client.IsConnected); } }
        public static BerryContext Context { get { return context; } }
        public static BerryUser User { get { return context.User; } }
        public static BerryRoom Room { get { return context.Room; } }
        public static IReadOnlyList<BerryPlayer> Players { get { return context.Players; } }
        public static BerryProfile Profile { get { return context.Profile; } }
        public static BerryNetwork Network { get { return context.Network; } }
        public static BerryGame Game { get { return context.Game; } }
        public static bool IsLoggedIn { get { return context.IsLoggedIn; } }
        public static bool IsInRoom { get { return context.IsInRoom; } }
        public static bool IsFreeConnection { get { return context.IsFreeConnection; } }
        public static bool IsLauncherAdmin { get { return context.Profile != null && context.Profile.Staff; } }
        public static bool HasProfile { get { return context.Profile != null && context.Profile.HasProfile; } }
        public static bool IsRoomOpen { get { return context.Room != null && context.Room.IsOpen; } }
        public static bool IsVoiceActive { get { return context.Room != null && context.Room.IsVoiceActive; } }
        public static bool IsInLauncherVoice { get { return context.Launcher != null && context.Launcher.VoiceJoined; } }
        public static int RoomMemberCount { get { return context.Room == null ? 0 : context.Room.MemberCount; } }
        public static string InstalledVersion { get { return context.Game == null ? "" : context.Game.InstalledVersion; } }
        public static bool IsDownloadedFromBerry { get { return context.Game != null && context.Game.DownloadedFromBerry; } }
        public static BerryLauncher Launcher { get { return context.Launcher != null && context.Launcher.Open ? context.Launcher : probed; } }
        public static BerryLinkState LinkState
        {
            get
            {
                if (!mock && client != null && client.IsConnected) return BerryLinkState.Connected;
                if (probed.Open) return BerryLinkState.LauncherOpenNoSession;
                return Application.isEditor ? BerryLinkState.Mock : BerryLinkState.LauncherClosed;
            }
        }
        public static bool IsHost { get { return context.Room != null && context.Room.Role == BerryRole.Host; } }

        public static event Action Ready;
        public static event Action<BerryContext> ContextChanged;
        public static event Action<string, object> Pushed;
        public static event Action<string> Disconnected;

        public static async Task<bool> RetryAsync()
        {
            if (client != null) { client.Dispose(); client = null; }
            ready = false;
            mock = false;
            context = new BerryContext();
            return await InitAsync(lastOptions);
        }

        public static async Task<List<BerryRoomInfo>> ListRoomsAsync(bool allGames = false)
        {
            var list = new List<BerryRoomInfo>();
            if (mock) { list.Add(new BerryRoomInfo { Id = "mock-room", Name = "Mock room", Host = "mock_friend", Players = 1, MaxPlayers = 4, Status = "waiting", PingMs = 30 }); return list; }
            var data = new Dictionary<string, object> { { "all", allGames } };
            var l = BerryJson.List(await CallAsync("rooms.list", data), "rooms");
            if (l != null) foreach (object o in l) { if (list.Count >= 100) break; list.Add(BerryRoomInfo.FromJson(o)); }
            return list;
        }

        public static async Task<BerryRoomState> JoinRoomAsync(string roomId, string password = null)
        {
            if (mock) return MockRoomState(false);
            var data = new Dictionary<string, object> { { "roomId", roomId ?? "" } };
            if (!string.IsNullOrEmpty(password)) data["password"] = password;
            return BerryRoomState.FromJson(await CallAsync("lan.join", data));
        }

        public static async Task<bool> InitAsync(BerryOptions options = null)
        {
            if (ready) return true;
            options = options ?? new BerryOptions();
            lastOptions = options;
            BerrySession session = BerrySession.Discover(options.GameId);
            if (session != null) { session.GameCode = options.GameCode ?? ""; session.GameNumber = options.GameNumber; }
            if (session == null)
            {
                probed = options.ProbeLauncher ? await BerryProbe.RunAsync(options.GameId) : new BerryLauncher();
                if (!options.MockWhenStandalone) return false;
                mock = true;
                context = options.MockContext ?? DefaultMock(options.GameId, options.MockName, options.MockIsHost);
                context.Launcher = probed;
                ready = true;
                BerryMainThread.Post(() => { var h = Ready; if (h != null) h(); });
                return true;
            }
            client = new BerryClient();
            client.ContextChanged += j => BerryMainThread.Post(() => { context = BerryContext.FromJson(j); var h = ContextChanged; if (h != null) h(context); });
            client.Pushed += (n, d) => BerryMainThread.Post(() => { var h = Pushed; if (h != null) h(n, d); });
            client.Closed += r => BerryMainThread.Post(() => { var h = Disconnected; if (h != null) h(r); });
            try
            {
                var hello = await client.ConnectAsync(session);
                context = BerryContext.FromJson(BerryJson.Child(hello, "context"));
                ready = true;
                BerryMainThread.Post(() => { var h = Ready; if (h != null) h(); });
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Berry] could not connect to the launcher: " + e.Message);
                client.Dispose();
                client = null;
                return false;
            }
        }

        public static void SetPresence(string text) { Emit("presence.set", new Dictionary<string, object> { { "text", text ?? "" } }); }

        public static void UnlockAchievement(string id, string title = "") { Emit("achievement.unlock", new Dictionary<string, object> { { "id", id }, { "title", title ?? "" } }); }

        public static void SubmitScore(string board, double value) { Emit("score.submit", new Dictionary<string, object> { { "board", board }, { "value", value } }); }

        public static void LeaveRoom() { Emit("room.leave", new Dictionary<string, object>()); }

        public static void SendCustom(string name, Dictionary<string, object> data) { Emit(name, data ?? new Dictionary<string, object>()); }

        public static async Task<BerryIdentityToken> GetIdentityTokenAsync(string audience)
        {
            if (mock) return new BerryIdentityToken { Token = "mock." + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(audience ?? "")), ExpiresUtc = DateTime.UtcNow.AddMinutes(5) };
            if (client == null || !client.IsConnected) throw new BerryException("not_connected");
            var reply = await client.RequestAsync("req", new Dictionary<string, object> { { "name", "token" }, { "data", new Dictionary<string, object> { { "audience", audience ?? "" } } } });
            string type = BerryJson.Str(reply, "type");
            var payload = BerryJson.Child(reply, "payload");
            if (type == "error") throw new BerryException(BerryJson.Str(payload, "code", "failed"));
            if (!BerryJson.Bool(payload, "ok")) throw new BerryException(BerryJson.Str(payload, "error", "failed"));
            var data = BerryJson.Child(payload, "data");
            double expires = BerryJson.Num(data, "expiresAt");
            return new BerryIdentityToken { Token = BerryJson.Str(data, "token"), ExpiresUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(expires) };
        }

        public static async Task<BerryRoomState> GetLanStatusAsync()
        {
            if (mock) return MockRoomState(true);
            return BerryRoomState.FromJson(await CallAsync("lan.status", null));
        }

        public static async Task<BerryRoomState> HostLanAsync(string name, int maxPlayers = 8, string password = null, bool publicRoom = false)
        {
            if (mock) return MockRoomState(true);
            var data = new Dictionary<string, object> { { "name", name ?? "" }, { "maxPlayers", maxPlayers }, { "visibility", publicRoom ? "public" : "private" } };
            if (!string.IsNullOrEmpty(password)) data["password"] = password;
            return BerryRoomState.FromJson(await CallAsync("lan.host", data));
        }

        public static async Task<BerryRoomState> JoinLanAsync(string code, string password = null)
        {
            if (mock) return MockRoomState(false);
            var data = new Dictionary<string, object> { { "code", code ?? "" } };
            if (!string.IsNullOrEmpty(password)) data["password"] = password;
            return BerryRoomState.FromJson(await CallAsync("lan.join", data));
        }

        public static async Task LeaveLanAsync()
        {
            if (mock) return;
            await CallAsync("lan.leave", null);
        }

        public static async Task<BerryNetwork> GetNetworkAsync()
        {
            if (mock) return new BerryNetwork { State = "running", Address = "10.77.0.2" };
            return BerryNetwork.FromJson(await CallAsync("network.get", null));
        }

        public static async Task<List<BerryFriend>> GetFriendsAsync()
        {
            var list = new List<BerryFriend>();
            if (mock) { list.Add(new BerryFriend { Username = "mock_friend", DisplayName = "Mock Friend", Online = true }); return list; }
            var l = BerryJson.List(await CallAsync("friends.list", null), "friends");
            if (l != null)
                foreach (object o in l)
                    list.Add(new BerryFriend { Username = BerryJson.Str(o, "username"), DisplayName = BerryJson.Str(o, "displayName"), Online = BerryJson.Bool(o, "online"), Playing = BerryJson.Bool(o, "playing") });
            return list;
        }

        public static async Task InviteFriendAsync(string username)
        {
            if (mock) return;
            await CallAsync("friends.invite", new Dictionary<string, object> { { "username", username ?? "" } });
        }

        public static async Task<BerryProfileInfo> GetProfileAsync(string username = null)
        {
            if (mock) return new BerryProfileInfo { Username = username ?? context.User.Username, DisplayName = username ?? context.User.DisplayName, Bio = "mock", HasProfile = true, Online = true };
            var d = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(username)) d["username"] = username;
            return BerryProfileInfo.FromJson(await CallAsync("profile.get", d));
        }

        public static async Task<BerryFollowResult> CheckFollowAsync(string follower, string target)
        {
            if (mock) return new BerryFollowResult { A = follower ?? "", B = target ?? "", Follows = true };
            var d = new Dictionary<string, object> { { "b", target ?? "" } };
            if (!string.IsNullOrEmpty(follower)) d["a"] = follower;
            return BerryFollowResult.FromJson(await CallAsync("follow.check", d));
        }

        public static async Task<bool> DoesFollowAsync(string follower, string target)
        {
            return (await CheckFollowAsync(follower, target)).Follows;
        }

        public static async Task<bool> HasAchievementAsync(string id, string username = null)
        {
            if (mock) return false;
            var d = new Dictionary<string, object> { { "id", id ?? "" } };
            if (!string.IsNullOrEmpty(username)) d["username"] = username;
            return BerryJson.Bool(await CallAsync("achievements.has", d), "has");
        }

        public static async Task<List<BerryAchievement>> ListAchievementsAsync()
        {
            var list = new List<BerryAchievement>();
            if (mock) return list;
            var l = BerryJson.List(await CallAsync("achievements.list", null), "achievements");
            if (l != null) foreach (object o in l) { if (list.Count >= 200) break; list.Add(BerryAchievement.FromJson(o)); }
            return list;
        }

        public static async Task<BerryClubInfo> GetClubAsync(string handle = null)
        {
            if (mock) return new BerryClubInfo { InClub = true, Name = "Mock club", Tag = "BRY", MemberCount = 1, MaxMembers = 50 };
            var d = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(handle)) d["handle"] = handle;
            return BerryClubInfo.FromJson(await CallAsync("club.get", d));
        }

        public static async Task<List<BerryTournament>> ListTournamentsAsync()
        {
            var list = new List<BerryTournament>();
            if (mock) return list;
            var l = BerryJson.List(await CallAsync("tournament.list", null), "tournaments");
            if (l != null) foreach (object o in l) { if (list.Count >= 50) break; list.Add(BerryTournament.FromJson(o)); }
            return list;
        }

        public static async Task<BerryTournament> GetTournamentAsync(int id)
        {
            if (mock) return new BerryTournament { Id = id, Name = "Mock tournament" };
            return BerryTournament.FromJson(BerryJson.Child(await CallAsync("tournament.get", new Dictionary<string, object> { { "id", id } }), "tournament"));
        }

        public static async Task<BerryAccountInfo> GetAccountAsync()
        {
            if (mock) return new BerryAccountInfo { Username = context.User.Username, DisplayName = context.User.DisplayName, Coins = 100 };
            return BerryAccountInfo.FromJson(await CallAsync("account.get", null));
        }

        public static async Task<BerrySettingsInfo> GetSettingsAsync()
        {
            if (mock) return new BerrySettingsInfo();
            return BerrySettingsInfo.FromJson(await CallAsync("settings.get", null));
        }

        public static Task KickAsync(string username) { return Admin("room.kick", new Dictionary<string, object> { { "username", username ?? "" } }); }

        public static Task MuteAsync(string username, bool muted = true) { return Admin("room.mute", new Dictionary<string, object> { { "username", username ?? "" }, { "muted", muted } }); }

        public static Task SetRoomAdminAsync(string username, int level) { return Admin("room.setAdmin", new Dictionary<string, object> { { "username", username ?? "" }, { "level", level } }); }

        public static Task SetRoomPasswordAsync(string password) { return Admin("room.setPassword", new Dictionary<string, object> { { "password", password ?? "" } }); }

        public static Task SetRoomStatusAsync(bool inGame) { return Admin("room.setStatus", new Dictionary<string, object> { { "status", inGame ? "in_game" : "waiting" } }); }

        public static Task SetMaxPingAsync(int ms) { return Admin("room.setMaxPing", new Dictionary<string, object> { { "ms", ms } }); }

        public static Task SetRequireReadyAsync(bool on) { return Admin("room.setRequireReady", new Dictionary<string, object> { { "on", on } }); }

        public static Task SetLaunchModeAsync(bool auto) { return Admin("room.setLaunchMode", new Dictionary<string, object> { { "mode", auto ? "auto" : "menu" } }); }

        public static Task CloseRoomAsync() { return Admin("room.close", null); }

        public static Task ShowLauncherAsync() { return Admin("launcher.show", null); }

        public static Task OpenLauncherPageAsync(string page) { return Admin("launcher.open", new Dictionary<string, object> { { "page", page ?? "" } }); }

        public static Task OpenLoginAsync() { return Admin("account.openLogin", null); }

        public static Task OpenForgotPasswordAsync() { return Admin("account.openForgot", null); }

        public static Task LogoutAsync() { return Admin("account.logout", null); }

        private static async Task Admin(string name, Dictionary<string, object> data)
        {
            if (mock) { Debug.Log("[Berry mock] " + name + " " + BerryJson.Serialize(data ?? new Dictionary<string, object>())); return; }
            await CallAsync(name, data);
        }

        private static async Task<Dictionary<string, object>> CallAsync(string name, Dictionary<string, object> data)
        {
            if (client == null || !client.IsConnected) throw new BerryException("not_connected");
            var reply = await client.RequestAsync("req", new Dictionary<string, object> { { "name", name }, { "data", data ?? new Dictionary<string, object>() } }, 60000);
            var payload = BerryJson.Child(reply, "payload");
            if (BerryJson.Str(reply, "type") == "error") throw new BerryException(BerryJson.Str(payload, "code", "failed"));
            if (!BerryJson.Bool(payload, "ok")) throw new BerryException(BerryJson.Str(payload, "error", "failed"));
            return BerryJson.Child(payload, "data");
        }

        private static BerryRoomState MockRoomState(bool host)
        {
            return new BerryRoomState { InRoom = true, Id = "mock-room", Code = "MOCK00", Name = "Mock room", MaxPlayers = 4, IsHost = host, Network = new BerryNetwork { State = "running", Address = "10.77.0.2" } };
        }

        public static void Shutdown()
        {
            if (client != null) client.Dispose();
            client = null;
            ready = false;
            mock = false;
            context = new BerryContext();
        }

        private static void Emit(string name, Dictionary<string, object> data)
        {
            if (mock) { Debug.Log("[Berry mock] " + name + " " + BerryJson.Serialize(data)); return; }
            if (client == null || !client.IsConnected) return;
            client.RequestAsync("event", new Dictionary<string, object> { { "name", name }, { "data", data } }).ContinueWith(t =>
            {
                if (t.IsFaulted) return;
                var reply = t.Result;
                if (BerryJson.Str(reply, "type") == "error")
                    BerryMainThread.Post(() => Debug.LogWarning("[Berry] " + name + " refused: " + BerryJson.Str(BerryJson.Child(reply, "payload"), "code")));
            });
        }

        private static BerryContext DefaultMock(string gameId, string name, bool host)
        {
            string who = string.IsNullOrEmpty(name) ? "mock_player" : name;
            var c = new BerryContext { GameId = gameId, GameName = Application.productName, Language = "en", LauncherVersion = "mock" };
            c.Permissions.AddRange(new[] { "identity", "room", "players", "presence", "achievements", "scores", "token", "network", "lan", "friends", "profile" });
            c.Game = new BerryGame { Id = gameId, Name = Application.productName, Defined = true, Source = "manual", Protocol = "udp" };
            c.Profile = new BerryProfile { HasProfile = true, Badge = Application.isEditor ? "purple" : "none", Club = Application.isEditor ? "BRY" : "", Online = true, Country = Application.isEditor ? "IR" : "" };
            c.Network = new BerryNetwork { State = "running", Address = "10.77.0.2" };
            c.SdkVersion = BerryClient.SdkVersion;
            c.User = new BerryUser { Username = who, DisplayName = who, Tier = "free" };
            c.Room = new BerryRoom { Id = "mock-room", Name = "Mock room", Code = "MOCK00", MaxPlayers = 4, Role = host ? BerryRole.Host : BerryRole.Guest, HostAddress = host ? "" : "127.0.0.1", Mode = BerryMode.Lan, HostName = who, Owner = who, Link = "https://berry.example/join/MOCK00" };
            c.Players.Add(new BerryPlayer { Username = who, DisplayName = who, IsHost = true, Role = "owner" });
            c.Players.Add(new BerryPlayer { Username = "mock_friend", DisplayName = "Mock Friend", Address = "10.77.0.3" });
            return c;
        }
    }

    public static class BerryMainThread
    {
        private static readonly Queue<Action> queue = new Queue<Action>();
        public static void Post(Action a) { lock (queue) queue.Enqueue(a); }
        public static void Pump()
        {
            while (true)
            {
                Action a;
                lock (queue) { if (queue.Count == 0) return; a = queue.Dequeue(); }
                try { a(); } catch (Exception e) { Debug.LogException(e); }
            }
        }
    }
}
