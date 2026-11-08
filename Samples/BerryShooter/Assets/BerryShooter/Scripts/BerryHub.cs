using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BerrySdk;
using UnityEngine;

namespace BerryShooter
{
    public sealed class BerryHub
    {
        public const string GameId = "berry-shooter";
        public const string ServerAudience = "berry-shooter-server";

        public BerryNetwork Net;
        public List<BerryFriend> Friends = new List<BerryFriend>();
        public List<BerryRoomInfo> Rooms = new List<BerryRoomInfo>();
        public BerryRoomState LastRoom;
        public string LastToken = "";
        public string LastTokenInfo = "";
        public string RoomsError = "";
        public bool RoomsLoading;
        public bool RoomsLoaded;
        public bool Busy;

        public event Action<string> Toast;
        private float netTimer;
        private bool netBusy;

        private void Say(string text) { if (Toast != null) Toast(text); }

        public static string Yes(bool v) { return Tr.T(v ? "yes" : "no"); }

        public static string Line(string label, string value) { return "<b>" + Tr.T(label) + "</b>  " + value + "\n"; }

        public static string When(DateTime utc)
        {
            return utc == DateTime.MinValue ? "-" : utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }

        public static string Dash(string s) { return string.IsNullOrEmpty(s) ? "-" : s; }

        public string ErrorText(string code) { return BerryUtil.ErrorText(code, Tr.Lang); }

        public PlayerProfile MyProfile()
        {
            var p = new PlayerProfile();
            if (Berry.IsReady && Berry.User != null)
            {
                string shown = Berry.User.DisplayName.Length > 0 ? Berry.User.DisplayName : Berry.User.Username;
                p.Name = TextUtil.CleanName(shown);
                p.Tier = Berry.User.Tier;
                p.FromBerry = !Berry.IsMock;
                p.Uid = TextUtil.Tag(Berry.User.BerryCode.Length > 0 ? Berry.User.BerryCode : Berry.User.Username, 24);
            }
            else
            {
                p.Name = TextUtil.CleanName(Settings.PlayerName.Length > 0 ? Settings.PlayerName : "Player-" + Settings.MockId);
                p.Uid = "pc-" + Settings.MockId;
            }
            if (Berry.Profile != null)
            {
                p.Badge = Berry.Profile.Badge;
                p.Club = TextUtil.Tag(Berry.Profile.Club, 6);
                p.Country = TextUtil.Tag(Berry.Profile.Country, 2);
            }
            if (Berry.Room != null && Berry.Room.Role != BerryRole.None)
                p.Role = Berry.Room.MyRole == "owner" || Berry.Room.Role == BerryRole.Host ? "host" : Berry.Room.MyRole == "admin" ? "admin" : "member";
            return p;
        }

        public string LauncherInfo()
        {
            var sb = new StringBuilder();
            BerryLauncher l = Berry.Launcher;
            sb.Append(Line("Link", BerryUtil.LinkStateText(Berry.LinkState, Tr.Lang)));
            sb.Append(Line("Launcher open", Yes(l.Open || Berry.LinkState == BerryLinkState.Connected)));
            sb.Append(Line("Signed in", l.Open || Berry.LinkState == BerryLinkState.Connected ? Yes(l.LoggedIn) : "-"));
            sb.Append(Line("Connected to Berry server", l.Open ? Yes(l.Connected) : "-"));
            sb.Append(Line("Official launcher", l.Open ? Yes(l.Official) : "-"));
            sb.Append(Line("Launcher version", l.Open ? Dash(l.Version) + (l.Build.Length > 0 ? " (build " + l.Build + ")" : "") : "-"));
            sb.Append(Line("Core version", Dash(l.CoreVersion)));
            sb.Append(Line("Open since", l.Open ? When(l.StartedUtc) + "  (" + Uptime(l.UptimeSec) + ")" : "-"));
            sb.Append(Line("Launcher language", l.Open ? l.Language : "-"));
            sb.Append(Line("Game sessions running", l.Open ? l.ActiveSessions.ToString() : "-"));
            sb.Append(Line("This game has an SDK session", Yes(Berry.LinkState == BerryLinkState.Connected)));
            sb.Append(Line("SDK package", BerryClient.SdkVersion + " / " + Tr.T("launcher level") + " " + Dash(Berry.IsReady ? Berry.Context.SdkVersion : "")));
            sb.Append(Line("Permissions", Berry.IsReady && Berry.Context.Permissions.Count > 0 ? string.Join(", ", Berry.Context.Permissions.ToArray()) : "-"));
            return sb.ToString();
        }

        private static string Uptime(int sec)
        {
            if (sec <= 0) return "-";
            int h = sec / 3600, m = sec % 3600 / 60;
            return h > 0 ? h + "h " + m + "m" : m + "m";
        }

        public string AccountInfo()
        {
            var sb = new StringBuilder();
            if (!Berry.IsReady || Berry.User == null) return Tr.T("No account data (identity permission, or no launcher session).");
            BerryUser u = Berry.User;
            string tc = BerryUtil.TierColorHex(u.Tier);
            sb.Append(Line("Name", TextUtil.Hex(tc, u.DisplayName)));
            sb.Append(Line("User name", "@" + u.Username));
            sb.Append(Line("Unique user id", u.UserId > 0 ? u.UserId.ToString() : "-"));
            sb.Append(Line("Berry code", Dash(u.BerryCode)));
            sb.Append(Line("Subscription tier", TextUtil.Hex(tc, u.Tier)));
            BerryProfile p = Berry.Profile;
            if (p == null) { sb.Append(Line("Profile", Tr.T("(no 'profile' permission)"))); return sb.ToString(); }
            sb.Append(Line("Subscription", BerryUtil.SubscriptionText(p, Tr.Lang)));
            sb.Append(Line("Subscription ends", When(p.SubscriptionEndUtc)));
            sb.Append(Line("Verified tick", TextUtil.Hex(BerryUtil.BadgeColorHex(p.Badge), p.Badge)));
            sb.Append(Line("Club", Dash(p.Club)));
            sb.Append(Line("Nation", Dash(p.Country)));
            sb.Append(Line("Gender", Dash(p.Gender)));
            sb.Append(Line("Bio", Dash(p.Bio)));
            sb.Append(Line("Berry staff", Yes(p.Staff)));
            sb.Append(Line("Online", Yes(p.Online)));
            sb.Append(Line("Profile language", p.Language));
            return sb.ToString();
        }

        public string RoomInfo()
        {
            if (!Berry.IsReady) return Tr.T("Connecting to Berry...");
            BerryRoom r = Berry.Room;
            if (r == null || r.Role == BerryRole.None) return Tr.T("You are not in a Berry room.") + (Berry.IsMock ? "\n" + Tr.T("Test mode: rooms are fake.") : "");
            var sb = new StringBuilder();
            sb.Append(Line("Room", r.Name));
            sb.Append(Line("Room id", Dash(r.Id)));
            sb.Append(Line("Room code", Dash(r.Code)));
            sb.Append(Line("Room link", Dash(r.Link)));
            sb.Append(Line("Game", Dash(r.GameName) + "  (" + Dash(r.GameId) + ")"));
            sb.Append(Line("Room type", BerryUtil.RoomTypeText(r, Tr.Lang)));
            sb.Append(Line("Password set", Yes(r.Locked)));
            sb.Append(Line("Status", Dash(r.Status) + (r.Kind.Length > 0 ? " / " + r.Kind : "")));
            sb.Append(Line("Connection", r.Mode + (r.HostAddress.Length > 0 ? "  " + r.HostAddress : "")));
            sb.Append(Line("Host", Dash(r.HostName)));
            sb.Append(Line("Owner", Dash(r.Owner)));
            sb.Append(Line("My role", BerryUtil.RoleText(r.MyRole, r.MyAdminLevel, Tr.Lang) + "  (" + r.Role + ")"));
            sb.Append(Line("Players", Math.Max(r.PlayerCount, Berry.Players.Count) + " / " + r.MaxPlayers));
            sb.Append(Line("Highest ping allowed", r.MaxPing > 0 ? r.MaxPing + " ms" : "-"));
            sb.Append(Line("Players must be ready", Yes(r.RequireReady)));
            sb.Append(Line("Start mode", r.LaunchMode));
            sb.Append(Line("Created", When(r.CreatedUtc)));
            sb.Append(Line("Opened", When(r.OpenedUtc)));
            sb.Append(Line("Tags", r.Tags.Count > 0 ? string.Join(", ", r.Tags.ToArray()) : "-"));
            sb.Append(Line("Description", Dash(r.Description)));
            return sb.ToString();
        }

        public string NetworkInfo()
        {
            BerryNetwork n = Net ?? Berry.Network;
            if (n == null) return Tr.T("(no 'network' permission)");
            var sb = new StringBuilder();
            sb.Append(Line("Virtual LAN", n.State));
            sb.Append(Line("My address", Dash(n.Address)));
            sb.Append(Line("NAT", Dash(n.Nat)));
            sb.Append(Line("Worst ping", BerryUtil.PingText(BerryUtil.WorstPing(n))));
            foreach (BerryPeer p in n.Peers)
                sb.AppendLine(TextUtil.Hex(Ui.ColorHex(Ui.PingColor(p.PingMs)), "• " + p.Username + "   " + p.Address + "   " + BerryUtil.PhaseText(p.Phase) + "   " + BerryUtil.PingText(p.PingMs)));
            return sb.ToString();
        }

        public string PlayerInfo(BerryPlayer p)
        {
            if (p == null) return Tr.T("Select a player to see the details.");
            var sb = new StringBuilder();
            sb.Append(Line("Name", TextUtil.Hex(BerryUtil.TierColorHex(p.Tier), p.DisplayName)));
            sb.Append(Line("User name", "@" + p.Username));
            sb.Append(Line("Unique user id", p.UserId > 0 ? p.UserId.ToString() : "-"));
            sb.Append(Line("Berry code", Dash(p.BerryCode)));
            sb.Append(Line("Role", BerryUtil.RoleText(p.IsHost && p.Role == "member" ? "host" : p.Role, p.AdminLevel, Tr.Lang)));
            sb.Append(Line("Admin permissions", p.AdminPerms.Count > 0 ? string.Join(", ", p.AdminPerms.ToArray()) : "-"));
            sb.Append(Line("Subscription tier", p.Tier));
            sb.Append(Line("Nation", Dash(p.Country)));
            sb.Append(Line("Address", Dash(p.Address)));
            sb.Append(Line("Ready", Yes(p.Ready)));
            sb.Append(Line("Muted", Yes(p.Muted)));
            sb.Append(Line("In voice", Yes(p.InVoice)));
            sb.Append(Line("Lagging", Yes(p.Laggy)));
            sb.Append(Line("Joined", When(p.JoinedUtc)));
            sb.Append(Line("Server ping", BerryUtil.PingText(p.ServerPingMs)));
            BerryPlayerStats s = p.Stats;
            sb.Append(Line("Latency", BerryUtil.PingText(s.NowMs) + "  avg " + BerryUtil.PingText(s.AvgMs) + "  min " + BerryUtil.PingText(s.MinMs) + "  max " + BerryUtil.PingText(s.MaxMs)));
            sb.Append(Line("Jitter / loss", BerryUtil.PingText(s.JitterMs) + " / " + (s.LossPercent < 0 ? "--" : s.LossPercent.ToString("0.#") + "%")));
            sb.Append(Line("Stable connection", s.Stable.HasValue ? Yes(s.Stable.Value) : "-"));
            return sb.ToString();
        }

        public void Tick(float dt)
        {
            if (!Berry.IsReady || !Berry.Context.Has("network")) return;
            netTimer -= dt;
            if (netTimer > 0f || netBusy) return;
            netTimer = 2f;
            RefreshNetwork();
        }

        public async void RefreshNetwork()
        {
            if (!Berry.IsReady || !Berry.Context.Has("network")) return;
            netBusy = true;
            try { Net = await Berry.GetNetworkAsync(); }
            catch (BerryException) { }
            netBusy = false;
        }

        public int BerryPing(string username) { return BerryUtil.PeerPing(Net ?? Berry.Network, username); }

        public BerryPlayer FindByDisplay(string display)
        {
            foreach (BerryPlayer p in Berry.Players)
                if (TextUtil.CleanName(p.DisplayName.Length > 0 ? p.DisplayName : p.Username) == display) return p;
            return null;
        }

        public int PingByDisplayName(string display)
        {
            BerryPlayer p = FindByDisplay(display);
            return p == null ? -1 : BerryPing(p.Username);
        }

        public async void RefreshRooms(bool all)
        {
            if (RoomsLoading) return;
            RoomsLoading = true;
            RoomsError = "";
            try
            {
                if (!Berry.IsReady) { RoomsError = Tr.T("Connecting to Berry..."); }
                else if (!Berry.IsMock && !Berry.Context.Has("lan")) { RoomsError = Tr.T("This game's profile has no 'lan' permission."); }
                else
                {
                    Rooms = await Berry.ListRoomsAsync(all);
                    RoomsLoaded = true;
                }
            }
            catch (BerryException e) { RoomsError = ErrorText(e.Code); }
            RoomsLoading = false;
        }

        public async Task<BerryRoomState> HostRoom(string name, int max, string password, bool isPublic)
        {
            string bad = BerryUtil.ValidateRoom(name, max, password);
            if (bad != null) { Say(ErrorText(bad)); return null; }
            if (!Berry.Context.Has("lan")) { Say(Tr.T("This game's profile has no 'lan' permission.")); return null; }
            Busy = true;
            Say(Tr.T("Waiting for you to allow it in the Berry launcher..."));
            try
            {
                LastRoom = await Berry.HostLanAsync(name.Trim(), max, string.IsNullOrEmpty(password) ? null : password, isPublic);
                Say(Tr.F("Room created. Code: {0}", LastRoom.Code));
                return LastRoom;
            }
            catch (BerryException e) { Say(ErrorText(e.Code)); return null; }
            finally { Busy = false; }
        }

        public async Task<BerryRoomState> JoinRoom(string code, string password)
        {
            code = (code ?? "").Trim().ToUpperInvariant();
            if (code.Length < 3) { Say(Tr.T("Type the room code first.")); return null; }
            if (!Berry.Context.Has("lan")) { Say(Tr.T("This game's profile has no 'lan' permission.")); return null; }
            Busy = true;
            Say(Tr.T("Waiting for you to allow it in the Berry launcher..."));
            try
            {
                LastRoom = await Berry.JoinLanAsync(code, string.IsNullOrEmpty(password) ? null : password);
                Say(LastRoom.Requested ? Tr.T("Request sent. Waiting for the host to let you in...") : Tr.F("Joined room {0}", LastRoom.Name));
                return LastRoom;
            }
            catch (BerryException e) { Say(ErrorText(e.Code)); return null; }
            finally { Busy = false; }
        }

        public async Task<BerryRoomState> JoinRoomById(string roomId, string password)
        {
            if (!Berry.Context.Has("lan")) { Say(Tr.T("This game's profile has no 'lan' permission.")); return null; }
            Busy = true;
            Say(Tr.T("Waiting for you to allow it in the Berry launcher..."));
            try
            {
                LastRoom = await Berry.JoinRoomAsync(roomId, string.IsNullOrEmpty(password) ? null : password);
                Say(LastRoom.Requested ? Tr.T("Request sent. Waiting for the host to let you in...") : Tr.F("Joined room {0}", LastRoom.Name));
                return LastRoom;
            }
            catch (BerryException e) { Say(ErrorText(e.Code)); return null; }
            finally { Busy = false; }
        }

        public async void LeaveRoom()
        {
            if (!Berry.Context.Has("lan")) { Berry.LeaveRoom(); return; }
            string err = await BerryUtil.TryAsync(() => Berry.LeaveLanAsync(), Tr.Lang);
            if (err != null) Say(err);
        }

        public async void LoadFriends()
        {
            if (!Berry.IsReady || !Berry.Context.Has("friends")) { Say(Tr.T("No 'friends' permission.")); return; }
            Busy = true;
            try
            {
                Friends = await Berry.GetFriendsAsync();
                Say(Tr.F("{0} friend(s)", Friends.Count));
            }
            catch (BerryException e) { Say(ErrorText(e.Code)); }
            finally { Busy = false; }
        }

        public async void Invite(string username)
        {
            string err = await BerryUtil.TryAsync(() => Berry.InviteFriendAsync(username), Tr.Lang);
            Say(err ?? Tr.F("Invitation sent to {0}", username));
        }

        public void Presence(string text) { if (Berry.IsReady) Berry.SetPresence(text); }

        public void Achievement(string id, string title) { if (Berry.IsReady) Berry.UnlockAchievement(id, title); }

        public void Score(string board, double value) { if (Berry.IsReady) Berry.SubmitScore(board, value); }

        public void Custom(string name, string key, string value)
        {
            if (!Berry.IsReady) return;
            Berry.SendCustom(name, new Dictionary<string, object> { { key, value } });
        }

        public async void GetToken()
        {
            if (!Berry.IsReady || !Berry.Context.Has("token")) { Say(Tr.T("No 'token' permission.")); return; }
            try
            {
                BerryIdentityToken t = await Berry.GetIdentityTokenAsync(ServerAudience);
                LastToken = t.Token;
                LastTokenInfo = Tr.F("Token OK: {0} characters, valid until {1}", t.Token.Length, t.ExpiresUtc.ToLocalTime().ToString("HH:mm:ss"));
                Say(LastTokenInfo);
            }
            catch (BerryException e) { LastTokenInfo = ErrorText(e.Code); Say(LastTokenInfo); }
        }

        public void CopyToken()
        {
            if (LastToken.Length == 0) return;
            GUIUtility.systemCopyBuffer = LastToken;
            Say(Tr.T("Token copied"));
        }

        public void LeaveLauncherRoom() { Berry.LeaveRoom(); }

        public BerryTransport PreferredTransport()
        {
            int t = Settings.Transport;
            return t == 1 ? BerryTransport.Udp : t == 2 ? BerryTransport.Tcp : BerryTransport.Auto;
        }

        public bool ShouldPreferTcp()
        {
            return BerryUtil.ChooseTransport(BerryTransport.Auto, Berry.IsReady ? Berry.Context : null) == BerryTransport.Tcp;
        }
    }
}
