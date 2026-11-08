using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace BerrySdk
{
    public static class BerryJson
    {
        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException("json");
            var p = new Parser(json);
            object v = p.ReadValue();
            p.SkipWhite();
            if (!p.AtEnd) throw new FormatException("Unexpected data after the JSON value");
            return v;
        }

        public static bool TryParse(string json, out object value)
        {
            try { value = Parse(json); return true; }
            catch (Exception) { value = null; return false; }
        }

        public static string Serialize(object value)
        {
            var sb = new StringBuilder();
            Write(sb, value, 0);
            return sb.ToString();
        }

        public static Dictionary<string, object> Obj(object o) { return o as Dictionary<string, object>; }
        public static string Str(object o, string key, string def = "")
        {
            var d = o as Dictionary<string, object>;
            object v;
            return d != null && d.TryGetValue(key, out v) && v is string ? (string)v : def;
        }
        public static double Num(object o, string key, double def = 0)
        {
            var d = o as Dictionary<string, object>;
            object v;
            return d != null && d.TryGetValue(key, out v) && v is double ? (double)v : def;
        }
        public static bool Bool(object o, string key, bool def = false)
        {
            var d = o as Dictionary<string, object>;
            object v;
            return d != null && d.TryGetValue(key, out v) && v is bool ? (bool)v : def;
        }
        public static Dictionary<string, object> Child(object o, string key)
        {
            var d = o as Dictionary<string, object>;
            object v;
            return d != null && d.TryGetValue(key, out v) ? v as Dictionary<string, object> : null;
        }
        public static List<object> List(object o, string key)
        {
            var d = o as Dictionary<string, object>;
            object v;
            return d != null && d.TryGetValue(key, out v) ? v as List<object> : null;
        }

        private static void Write(StringBuilder sb, object v, int depth)
        {
            if (depth > 32) throw new InvalidOperationException("JSON is nested too deep");
            if (v == null) { sb.Append("null"); return; }
            if (v is string) { WriteString(sb, (string)v); return; }
            if (v is bool) { sb.Append((bool)v ? "true" : "false"); return; }
            if (v is int || v is long || v is short || v is byte) { sb.Append(Convert.ToInt64(v).ToString(CultureInfo.InvariantCulture)); return; }
            if (v is float || v is double)
            {
                double d = Convert.ToDouble(v);
                if (double.IsNaN(d) || double.IsInfinity(d)) { sb.Append("null"); return; }
                sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
                return;
            }
            var dict = v as IDictionary;
            if (dict != null)
            {
                sb.Append('{');
                bool first = true;
                foreach (DictionaryEntry e in dict)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    WriteString(sb, Convert.ToString(e.Key, CultureInfo.InvariantCulture));
                    sb.Append(':');
                    Write(sb, e.Value, depth + 1);
                }
                sb.Append('}');
                return;
            }
            var list = v as IEnumerable;
            if (list != null)
            {
                sb.Append('[');
                bool first = true;
                foreach (object item in list)
                {
                    if (!first) sb.Append(',');
                    first = false;
                    Write(sb, item, depth + 1);
                }
                sb.Append(']');
                return;
            }
            WriteString(sb, Convert.ToString(v, CultureInfo.InvariantCulture));
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private sealed class Parser
        {
            private readonly string s;
            private int i;
            private int depth;
            public Parser(string text) { s = text; }
            public bool AtEnd { get { return i >= s.Length; } }

            public void SkipWhite()
            {
                while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
            }

            public object ReadValue()
            {
                SkipWhite();
                if (i >= s.Length) throw new FormatException("Unexpected end of JSON");
                char c = s[i];
                if (c == '{') return ReadObject();
                if (c == '[') return ReadArray();
                if (c == '"') return ReadString();
                if (c == 't') { Expect("true"); return true; }
                if (c == 'f') { Expect("false"); return false; }
                if (c == 'n') { Expect("null"); return null; }
                return ReadNumber();
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0) throw new FormatException("Bad JSON token at " + i);
                i += word.Length;
            }

            private Dictionary<string, object> ReadObject()
            {
                if (++depth > 32) throw new FormatException("JSON is nested too deep");
                var d = new Dictionary<string, object>();
                i++;
                SkipWhite();
                if (i < s.Length && s[i] == '}') { i++; depth--; return d; }
                while (true)
                {
                    SkipWhite();
                    if (i >= s.Length || s[i] != '"') throw new FormatException("Object key expected at " + i);
                    string key = ReadString();
                    SkipWhite();
                    if (i >= s.Length || s[i] != ':') throw new FormatException("':' expected at " + i);
                    i++;
                    d[key] = ReadValue();
                    SkipWhite();
                    if (i >= s.Length) throw new FormatException("Unexpected end of JSON");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == '}') { i++; break; }
                    throw new FormatException("',' or '}' expected at " + i);
                }
                depth--;
                return d;
            }

            private List<object> ReadArray()
            {
                if (++depth > 32) throw new FormatException("JSON is nested too deep");
                var l = new List<object>();
                i++;
                SkipWhite();
                if (i < s.Length && s[i] == ']') { i++; depth--; return l; }
                while (true)
                {
                    l.Add(ReadValue());
                    SkipWhite();
                    if (i >= s.Length) throw new FormatException("Unexpected end of JSON");
                    if (s[i] == ',') { i++; continue; }
                    if (s[i] == ']') { i++; break; }
                    throw new FormatException("',' or ']' expected at " + i);
                }
                depth--;
                return l;
            }

            private string ReadString()
            {
                var sb = new StringBuilder();
                i++;
                while (true)
                {
                    if (i >= s.Length) throw new FormatException("Unterminated string");
                    char c = s[i++];
                    if (c == '"') break;
                    if (c != '\\') { sb.Append(c); continue; }
                    if (i >= s.Length) throw new FormatException("Unterminated escape");
                    char e = s[i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (i + 4 > s.Length) throw new FormatException("Bad \\u escape");
                            sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                            i += 4;
                            break;
                        default: throw new FormatException("Bad escape \\" + e);
                    }
                }
                return sb.ToString();
            }

            private object ReadNumber()
            {
                int start = i;
                while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '-' || s[i] == '+' || s[i] == '.' || s[i] == 'e' || s[i] == 'E')) i++;
                double d;
                if (i == start || !double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                    throw new FormatException("Bad number at " + start);
                return d;
            }
        }
    }
}
