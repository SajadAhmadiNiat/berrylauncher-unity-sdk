using System;
using System.Text;
using UnityEngine;

namespace BerryShooter
{
    public static class Settings
    {
        public static float Sensitivity { get { return PlayerPrefs.GetFloat("bs.sens", 2.2f); } set { PlayerPrefs.SetFloat("bs.sens", Mathf.Clamp(value, 0.3f, 8f)); } }
        public static float Volume { get { return PlayerPrefs.GetFloat("bs.vol", 0.7f); } set { PlayerPrefs.SetFloat("bs.vol", Mathf.Clamp01(value)); AudioListener.volume = Mathf.Clamp01(value); } }
        public static float Fov { get { return PlayerPrefs.GetFloat("bs.fov", 75f); } set { PlayerPrefs.SetFloat("bs.fov", Mathf.Clamp(value, 60f, 110f)); } }
        public static int Port { get { return PlayerPrefs.GetInt("bs.port", 7777); } set { PlayerPrefs.SetInt("bs.port", Mathf.Clamp(value, 1024, 65000)); } }
        public static string LastIp { get { return PlayerPrefs.GetString("bs.ip", "127.0.0.1"); } set { PlayerPrefs.SetString("bs.ip", value ?? ""); } }
        public static string PlayerName { get { return PlayerPrefs.GetString("bs.name", ""); } set { PlayerPrefs.SetString("bs.name", value ?? ""); } }
        public static string RoomName { get { return PlayerPrefs.GetString("bs.room", ""); } set { PlayerPrefs.SetString("bs.room", value ?? ""); } }
        public static string Language { get { return PlayerPrefs.GetString("bs.lang", ""); } set { PlayerPrefs.SetString("bs.lang", value == "fa" ? "fa" : "en"); } }
        public static int Transport { get { return Mathf.Clamp(PlayerPrefs.GetInt("bs.proto", 0), 0, 2); } set { PlayerPrefs.SetInt("bs.proto", Mathf.Clamp(value, 0, 2)); } }
        public static string MockId
        {
            get
            {
                string id = PlayerPrefs.GetString("bs.mockid", "");
                if (id.Length == 0) { id = UnityEngine.Random.Range(1000, 9999).ToString(); PlayerPrefs.SetString("bs.mockid", id); }
                return id;
            }
        }
        public static void Save() { PlayerPrefs.Save(); }
    }

    public static class Mats
    {
        private static Shader flat;

        private static Shader Flat()
        {
            if (flat != null) return flat;
            flat = Resources.Load<Shader>("BSFlat");
            if (flat == null) flat = Shader.Find("BerryShooter/Flat");
            if (flat == null) flat = Shader.Find("Unlit/Color");
            if (flat == null) flat = Shader.Find("Sprites/Default");
            return flat;
        }

        private static readonly System.Collections.Generic.Dictionary<string, Material> cache = new System.Collections.Generic.Dictionary<string, Material>();

        private static Material Make(Color c, float emission, bool twoSided)
        {
            string key = ColorUtility.ToHtmlStringRGBA(c) + emission + twoSided;
            Material cached;
            if (cache.TryGetValue(key, out cached) && cached != null) return cached;
            var m = new Material(Flat());
            cache[key] = m;
            m.color = c;
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Emission")) m.SetFloat("_Emission", emission);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", twoSided ? 0f : 2f);
            return m;
        }

        public static Material Solid(Color c, float smooth = 0.25f) { return Make(c, 0f, false); }

        public static Material Glow(Color c) { return Make(c, 1f, true); }

        public static void Tint(Renderer r, Color c)
        {
            if (r == null) return;
            var m = r.material;
            m.color = c;
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        }
    }

    public static class Fonts
    {
        private static Font latin, persian;

        public static Font Get()
        {
            if (latin != null) return latin;
#if UNITY_2022_2_OR_NEWER
            latin = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            latin = Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
            if (latin == null) latin = Font.CreateDynamicFontFromOSFont("Arial", 16);
            return latin;
        }

        public static Font Persian()
        {
            if (persian != null) return persian;
            persian = Resources.Load<Font>("fa-font");
            if (persian == null) persian = Font.CreateDynamicFontFromOSFont(new[] { "Vazirmatn", "Segoe UI", "Tahoma", "Arial" }, 16);
            if (persian == null) persian = Get();
            return persian;
        }

        public static Font ForLanguage() { return Tr.IsFa ? Persian() : Get(); }
    }

    public static class TextUtil
    {
        public static string CleanName(string s, string fallback = "Player")
        {
            var sb = new StringBuilder();
            foreach (char c in s ?? "")
            {
                if (sb.Length >= 18) break;
                if (c == '|' || c == ',' || c == ';' || c == '<' || c == '>' || char.IsControl(c)) continue;
                sb.Append(c);
            }
            string r = sb.ToString().Trim();
            return r.Length == 0 ? fallback : r;
        }

        public static string Tag(string s, int max)
        {
            var sb = new StringBuilder();
            foreach (char c in s ?? "") { if (sb.Length >= max) break; if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c); }
            return sb.ToString();
        }

        public static string Hex(string hex, string text) { return "<color=" + hex + ">" + text + "</color>"; }

        public static string Plain(string s)
        {
            var sb = new StringBuilder();
            bool tag = false;
            foreach (char c in s ?? "")
            {
                if (c == '<') { tag = true; continue; }
                if (c == '>' && tag) { tag = false; continue; }
                if (!tag) sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
