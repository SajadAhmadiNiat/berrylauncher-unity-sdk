using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BerrySdk
{
    public enum BerryTransport { Auto, Udp, Tcp }

    public enum BerryPingQuality { Unknown, Excellent, Good, Fair, Poor }

    public static class BerryLimits
    {
        public const int RoomNameMin = 3, RoomNameMax = 60;
        public const int PlayersMin = 2, PlayersMax = 64;
        public const int PasswordMin = 4, PasswordMax = 64;
    }

    public static class BerryUtil
    {

        public static int TierRank(string tier)
        {
            switch ((tier ?? "").ToLowerInvariant())
            {
                case "silver": return 1;
                case "gold": return 2;
                case "amethyst": return 3;
                case "diamond": return 4;
                default: return 0;
            }
        }

        public static bool IsSubscriber(string tier) { return TierRank(tier) > 0; }

        public static string TierColorHex(string tier)
        {
            switch (TierRank(tier))
            {
                case 1: return "#C0C8D4";
                case 2: return "#FBBF24";
                case 3: return "#A78BFA";
                case 4: return "#38BDF8";
                default: return "#F3F4F6";
            }
        }

        public static string BadgeColorHex(string badge)
        {
            switch ((badge ?? "").ToLowerInvariant())
            {
                case "gray": return "#9CA3AF";
                case "purple": return "#A855F7";
                case "gold": return "#FACC15";
                default: return "#FFFFFF";
            }
        }

        public static string NameWithClub(string displayName, BerryProfile profile)
        {
            string n = string.IsNullOrEmpty(displayName) ? "Player" : displayName;
            return profile != null && !string.IsNullOrEmpty(profile.Club) ? "[" + profile.Club + "] " + n : n;
        }

        public static BerryPingQuality PingQuality(int ms)
        {
            if (ms < 0) return BerryPingQuality.Unknown;
            if (ms <= 40) return BerryPingQuality.Excellent;
            if (ms <= 90) return BerryPingQuality.Good;
            if (ms <= 160) return BerryPingQuality.Fair;
            return BerryPingQuality.Poor;
        }

        public static string PingText(int ms) { return ms < 0 ? "--" : ms + " ms"; }

        public static BerryPeer FindPeer(BerryNetwork network, string username)
        {
            if (network == null || network.Peers == null) return null;
            foreach (BerryPeer p in network.Peers) if (string.Equals(p.Username, username, StringComparison.OrdinalIgnoreCase)) return p;
            return null;
        }

        public static int PeerPing(BerryNetwork network, string username)
        {
            BerryPeer p = FindPeer(network, username);
            return p == null ? -1 : p.PingMs;
        }

        public static int WorstPing(BerryNetwork network)
        {
            int worst = -1;
            if (network != null && network.Peers != null) foreach (BerryPeer p in network.Peers) if (p.PingMs > worst) worst = p.PingMs;
            return worst;
        }

        public static string PhaseText(string phase)
        {
            switch (phase)
            {
                case "direct": return "Direct";
                case "relay": return "Relay";
                case "punching": return "Connecting (NAT)";
                case "connecting": return "Connecting";
                case "waiting": return "Waiting";
                default: return string.IsNullOrEmpty(phase) ? "--" : phase;
            }
        }

        public static bool IsPrivateAddress(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            string[] p = ip.Split('.');
            if (p.Length != 4) return false;
            int a, b, c, d;
            if (!int.TryParse(p[0], out a) || !int.TryParse(p[1], out b) || !int.TryParse(p[2], out c) || !int.TryParse(p[3], out d)) return false;
            if (a < 0 || a > 255 || b < 0 || b > 255 || c < 0 || c > 255 || d < 0 || d > 255) return false;
            return a == 10 || a == 127 || (a == 172 && b >= 16 && b <= 31) || (a == 192 && b == 168) || (a == 169 && b == 254);
        }

        public static string HostAddressToConnect(BerryContext context)
        {
            if (context == null || context.Room == null || context.Room.Role != BerryRole.Guest) return null;
            return IsPrivateAddress(context.Room.HostAddress) ? context.Room.HostAddress : null;
        }

        public static bool IsInRoom(BerryContext context) { return context != null && context.Room != null && context.Room.Role != BerryRole.None; }

        public static string ValidateRoom(string name, int maxPlayers, string password)
        {
            string n = (name ?? "").Trim();
            if (n.Length < BerryLimits.RoomNameMin || n.Length > BerryLimits.RoomNameMax) return "bad_request";
            if (maxPlayers < BerryLimits.PlayersMin || maxPlayers > BerryLimits.PlayersMax) return "bad_request";
            if (!string.IsNullOrEmpty(password) && (password.Length < BerryLimits.PasswordMin || password.Length > BerryLimits.PasswordMax)) return "bad_request";
            return null;
        }

        public static string ErrorText(string code, string lang = "en")
        {
            bool fa = lang == "fa";
            switch (code)
            {
                case "denied": return fa ? "بازیکن اجازه نداد." : "The player said no.";
                case "already_in_room": return fa ? "بازیکن همین حالا در یک اتاق است." : "The player is already in a room.";
                case "not_in_room": return fa ? "بازیکن در هیچ اتاقی نیست." : "The player is not in a room.";
                case "bad_request": return fa ? "مقدارها درست نیست (نام اتاق، تعداد یا رمز)." : "Some value is not valid (room name, size or password).";
                case "rate_limited": return fa ? "خیلی سریع؛ چند ثانیه صبر کنید." : "Too fast - wait a few seconds.";
                case "forbidden": return fa ? "پروفایل بازی این اجازه را ندارد." : "This game's profile does not have that permission.";
                case "not_logged_in": return fa ? "در لانچر وارد حساب نشده‌اید." : "Not signed in to the launcher.";
                case "not_found": return fa ? "اتاق پیدا نشد یا دیگر باز نیست." : "The room was not found or is closed.";
                case "unknown_game": return fa ? "این بازی در لانچر ثبت نشده است." : "This game is not registered in the launcher.";
                case "not_started_by_berry": return fa ? "بازی از طریق لانچر اجرا نشده است." : "The game was not started by the launcher.";
                case "timeout": return fa ? "لانچر پاسخ نداد." : "The launcher did not answer.";
                case "not_connected": return fa ? "به لانچر وصل نیست." : "Not connected to the launcher.";
                case "unsupported": case "unknown_name": return fa ? "این لانچر قدیمی است و این قابلیت را ندارد." : "This launcher is too old for that feature.";
                default: return (fa ? "خطا: " : "Error: ") + (code ?? "failed");
            }
        }

        public static bool LauncherSupportsLan() { return Berry.IsMock || (Berry.Context != null && Berry.Context.SdkVersion.StartsWith("1.") && CompareMinor(Berry.Context.SdkVersion, 2) >= 0); }

        private static int CompareMinor(string version, int wantMinor)
        {
            string[] p = (version ?? "").Split('.');
            int minor;
            if (p.Length < 2 || !int.TryParse(p[1], out minor)) return -1;
            return minor - wantMinor;
        }

        public static async Task<BerryNetwork> WaitForNetworkAsync(int timeoutMs = 15000, int pollMs = 500)
        {
            DateTime end = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            BerryNetwork last = null;
            while (DateTime.UtcNow < end)
            {
                try { last = await Berry.GetNetworkAsync(); }
                catch (BerryException) { return last; }
                if (last != null && last.IsRunning) return last;
                await Task.Delay(pollMs);
            }
            return last;
        }

        public static async Task<string> TryAsync(Func<Task> call, string lang = "en")
        {
            try { await call(); return null; }
            catch (BerryException e) { return ErrorText(e.Code, lang); }
        }

        public static BerryTransport ChooseTransport(BerryTransport preferred, BerryContext context, bool supportsUdp = true, bool supportsTcp = true)
        {
            if (preferred == BerryTransport.Udp && supportsUdp) return BerryTransport.Udp;
            if (preferred == BerryTransport.Tcp && supportsTcp) return BerryTransport.Tcp;
            if (!supportsTcp) return BerryTransport.Udp;
            if (!supportsUdp) return BerryTransport.Tcp;
            if (context != null && context.Network != null)
                foreach (BerryPeer p in context.Network.Peers) if (p.Phase == "relay") return BerryTransport.Tcp;
            return BerryTransport.Udp;
        }

        public static string TransportName(BerryTransport t) { return t == BerryTransport.Tcp ? "TCP" : t == BerryTransport.Udp ? "UDP" : "Auto"; }

        public static string ExpiryText(DateTime utc, string lang = "en")
        {
            bool fa = lang == "fa";
            if (utc == DateTime.MinValue) return fa ? "ندارد" : "none";
            double days = (utc - DateTime.UtcNow).TotalDays;
            if (days <= 0) return fa ? "منقضی شده" : "expired";
            if (days >= 1) return (fa ? Math.Ceiling(days) + " روز" : Math.Ceiling(days) + " days");
            return fa ? "کمتر از یک روز" : "less than a day";
        }

        public static string SubscriptionText(BerryProfile profile, string lang = "en")
        {
            if (profile == null) return "--";
            string tier = profile.Plan ?? "free";
            if (profile.Staff) return lang == "fa" ? "تیم بری" : "Berry team";
            return tier + " (" + ExpiryText(profile.SubscriptionEndUtc, lang) + ")";
        }

        public static string RoleText(string role, int adminLevel, string lang = "en")
        {
            bool fa = lang == "fa";
            switch (role)
            {
                case "owner": return fa ? "صاحب اتاق" : "Owner";
                case "host": return fa ? "میزبان" : "Host";
                case "admin": return (fa ? "ادمین " : "Admin ") + (adminLevel > 0 ? adminLevel.ToString() : "");
                default: return fa ? "عضو" : "Member";
            }
        }

        public static string RoomTypeText(BerryRoom room, string lang = "en")
        {
            if (room == null) return "--";
            bool fa = lang == "fa";
            if (room.Free) return fa ? "آزاد" : "Free";
            if (room.IsNetwork) return fa ? "شبکهٔ خصوصی" : "Private network";
            string v = room.IsPublic ? (fa ? "عمومی" : "Public") : (fa ? "خصوصی" : "Private");
            return room.Locked ? v + (fa ? " + رمز" : " + password") : v;
        }

        public static string LinkStateText(BerryLinkState state, string lang = "en")
        {
            bool fa = lang == "fa";
            switch (state)
            {
                case BerryLinkState.Connected: return fa ? "وصل به لانچر" : "Connected to the launcher";
                case BerryLinkState.LauncherOpenNoSession: return fa ? "لانچر باز است ولی این بازی در لانچر ثبت نشده (بدون نشست SDK)" : "Launcher is open, but this game has no SDK session (add the game in Berry)";
                case BerryLinkState.LauncherClosed: return fa ? "لانچر باز نیست" : "Launcher is not running";
                default: return fa ? "حالت آزمایشی (داده‌ی ساختگی)" : "Test mode (fake data)";
            }
        }
    }
}
