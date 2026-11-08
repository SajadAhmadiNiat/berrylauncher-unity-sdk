using System.Collections.Generic;

namespace BerryShooter
{
    public static partial class Tr
    {
        public static int Version { get; private set; }
        public static bool IsFa { get; private set; }
        public static string Lang { get { return IsFa ? "fa" : "en"; } }

        public static void Init(string launcherLanguage)
        {
            string saved = Settings.Language;
            string lang = saved.Length > 0 ? saved : (launcherLanguage == "en" ? "en" : "fa");
            Apply(lang);
        }

        public static void Set(string lang)
        {
            Settings.Language = lang;
            Apply(lang);
        }

        private static void Apply(string lang)
        {
            bool fa = lang == "fa";
            if (Version > 0 && fa == IsFa) return;
            IsFa = fa;
            Version++;
        }

        public static string T(string key)
        {
            if (!IsFa || key == null) return key;
            string v;
            return Fa.TryGetValue(key, out v) ? v : key;
        }

        public static string F(string key, params object[] args)
        {
            try { return string.Format(T(key), args); }
            catch (System.FormatException) { return T(key); }
        }

        public static string Feed(string coded)
        {
            string[] p = (coded ?? "").Split(';');
            string a = p.Length > 1 ? p[1] : "", b = p.Length > 2 ? p[2] : "";
            switch (p[0])
            {
                case "ready": return T("Get ready!");
                case "wave": return F("Wave {0}", a);
                case "down": return F("{0} went down", a);
                case "back": return F("{0} is back", a);
                case "over": return F("Game over - wave {0}", a);
                case "joined": return F("{0} joined", a);
                case "left": return F("{0} left", a);
                case "kill": return F("{0} destroyed a {1}", a, T(b == "1" ? "tank" : b == "2" ? "shooter" : "cube"));
                default: return coded ?? "";
            }
        }
    }
}
