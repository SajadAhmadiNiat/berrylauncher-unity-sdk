using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace BerryShooter
{
    public static class Persian
    {
        private struct Glyph
        {
            public char Iso, Fin, Ini, Med;
            public bool JoinsNext;
        }

        private sealed class Item
        {
            public char Ch;
            public string[] Stack;
        }

        private static readonly Dictionary<char, Glyph> map = Build();
        private static readonly string[] noStack = new string[0];

        private static void Add(Dictionary<char, Glyph> d, int code, int iso, int fin, int ini, int med)
        {
            d[(char)code] = new Glyph { Iso = (char)iso, Fin = (char)fin, Ini = ini == 0 ? (char)iso : (char)ini, Med = med == 0 ? (char)fin : (char)med, JoinsNext = ini != 0 };
        }

        private static Dictionary<char, Glyph> Build()
        {
            var d = new Dictionary<char, Glyph>();
            Add(d, 0x0621, 0xFE80, 0xFE80, 0, 0);
            Add(d, 0x0622, 0xFE81, 0xFE82, 0, 0);
            Add(d, 0x0623, 0xFE83, 0xFE84, 0, 0);
            Add(d, 0x0624, 0xFE85, 0xFE86, 0, 0);
            Add(d, 0x0625, 0xFE87, 0xFE88, 0, 0);
            Add(d, 0x0626, 0xFE89, 0xFE8A, 0xFE8B, 0xFE8C);
            Add(d, 0x0627, 0xFE8D, 0xFE8E, 0, 0);
            Add(d, 0x0628, 0xFE8F, 0xFE90, 0xFE91, 0xFE92);
            Add(d, 0x0629, 0xFE93, 0xFE94, 0, 0);
            Add(d, 0x062A, 0xFE95, 0xFE96, 0xFE97, 0xFE98);
            Add(d, 0x062B, 0xFE99, 0xFE9A, 0xFE9B, 0xFE9C);
            Add(d, 0x062C, 0xFE9D, 0xFE9E, 0xFE9F, 0xFEA0);
            Add(d, 0x062D, 0xFEA1, 0xFEA2, 0xFEA3, 0xFEA4);
            Add(d, 0x062E, 0xFEA5, 0xFEA6, 0xFEA7, 0xFEA8);
            Add(d, 0x062F, 0xFEA9, 0xFEAA, 0, 0);
            Add(d, 0x0630, 0xFEAB, 0xFEAC, 0, 0);
            Add(d, 0x0631, 0xFEAD, 0xFEAE, 0, 0);
            Add(d, 0x0632, 0xFEAF, 0xFEB0, 0, 0);
            Add(d, 0x0633, 0xFEB1, 0xFEB2, 0xFEB3, 0xFEB4);
            Add(d, 0x0634, 0xFEB5, 0xFEB6, 0xFEB7, 0xFEB8);
            Add(d, 0x0635, 0xFEB9, 0xFEBA, 0xFEBB, 0xFEBC);
            Add(d, 0x0636, 0xFEBD, 0xFEBE, 0xFEBF, 0xFEC0);
            Add(d, 0x0637, 0xFEC1, 0xFEC2, 0xFEC3, 0xFEC4);
            Add(d, 0x0638, 0xFEC5, 0xFEC6, 0xFEC7, 0xFEC8);
            Add(d, 0x0639, 0xFEC9, 0xFECA, 0xFECB, 0xFECC);
            Add(d, 0x063A, 0xFECD, 0xFECE, 0xFECF, 0xFED0);
            Add(d, 0x0641, 0xFED1, 0xFED2, 0xFED3, 0xFED4);
            Add(d, 0x0642, 0xFED5, 0xFED6, 0xFED7, 0xFED8);
            Add(d, 0x0643, 0xFB8E, 0xFB8F, 0xFB90, 0xFB91);
            Add(d, 0x06A9, 0xFB8E, 0xFB8F, 0xFB90, 0xFB91);
            Add(d, 0x0644, 0xFEDD, 0xFEDE, 0xFEDF, 0xFEE0);
            Add(d, 0x0645, 0xFEE1, 0xFEE2, 0xFEE3, 0xFEE4);
            Add(d, 0x0646, 0xFEE5, 0xFEE6, 0xFEE7, 0xFEE8);
            Add(d, 0x0647, 0xFEE9, 0xFEEA, 0xFEEB, 0xFEEC);
            Add(d, 0x0648, 0xFEED, 0xFEEE, 0, 0);
            Add(d, 0x0649, 0xFBFC, 0xFBFD, 0xFBFE, 0xFBFF);
            Add(d, 0x064A, 0xFBFC, 0xFBFD, 0xFBFE, 0xFBFF);
            Add(d, 0x06CC, 0xFBFC, 0xFBFD, 0xFBFE, 0xFBFF);
            Add(d, 0x067E, 0xFB56, 0xFB57, 0xFB58, 0xFB59);
            Add(d, 0x0686, 0xFB7A, 0xFB7B, 0xFB7C, 0xFB7D);
            Add(d, 0x0698, 0xFB8A, 0xFB8B, 0, 0);
            Add(d, 0x06AF, 0xFB92, 0xFB93, 0xFB94, 0xFB95);
            return d;
        }

        private static readonly Dictionary<char, char[]> fallback = new Dictionary<char, char[]>
        {
            { 'ﭖ', new[] { 'ﺏ', 'ﺐ', 'ﺑ', 'ﺒ' } },
            { 'ﭺ', new[] { 'ﺝ', 'ﺞ', 'ﺟ', 'ﺠ' } },
            { 'ﮊ', new[] { 'ﺯ', 'ﺰ', 'ﺯ', 'ﺰ' } },
            { 'ﮒ', new[] { 'ﻙ', 'ﻚ', 'ﻛ', 'ﻜ' } },
            { 'ﮎ', new[] { 'ﻙ', 'ﻚ', 'ﻛ', 'ﻜ' } },
            { 'ﯼ', new[] { 'ﻱ', 'ﻲ', 'ﻳ', 'ﻴ' } },
        };

        public static bool IsRtl(char c)
        {
            return (c >= 0x0590 && c <= 0x08FF) || (c >= 0xFB1D && c <= 0xFDFF) || (c >= 0xFE70 && c <= 0xFEFF);
        }

        public static bool IsNumber(char c)
        {
            return (c >= '0' && c <= '9') || (c >= 0x06F0 && c <= 0x06F9) || (c >= 0x0660 && c <= 0x0669);
        }

        public static bool HasRtl(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (char c in s) if (IsRtl(c) && !IsNumber(c)) return true;
            return false;
        }

        private static bool IsLtr(char c)
        {
            if (IsNumber(c)) return true;
            return !IsRtl(c) && char.IsLetter(c);
        }

        private static char Mirror(char c)
        {
            switch (c)
            {
                case '(': return ')';
                case ')': return '(';
                case '[': return ']';
                case ']': return '[';
                case '{': return '}';
                case '}': return '{';
                case '<': return '>';
                case '>': return '<';
                case '«': return '»';
                case '»': return '«';
                default: return c;
            }
        }

        private static char Pick(Glyph g, bool prev, bool next, Font font)
        {
            char c = prev && next ? g.Med : prev ? g.Fin : next ? g.Ini : g.Iso;
            if (font != null && fallback.ContainsKey(g.Iso) && !font.HasCharacter(c))
            {
                char[] f = fallback[g.Iso];
                c = prev && next ? f[3] : prev ? f[1] : next ? f[2] : f[0];
            }
            return c;
        }

        private static bool Connects(char c)
        {
            return map.ContainsKey(c) && c != 'ء';
        }

        private static List<Item> Shape(List<Item> src, Font font)
        {
            var outList = new List<Item>(src.Count);
            for (int i = 0; i < src.Count; i++)
            {
                char c = src[i].Ch;
                Glyph g;
                if (c == '‌' || c == '‍') { outList.Add(new Item { Ch = '‌', Stack = src[i].Stack }); continue; }
                if (!map.TryGetValue(c, out g)) { outList.Add(src[i]); continue; }
                bool prev = false;
                if (outList.Count > 0)
                {
                    char pc = outList[outList.Count - 1].Ch;
                    prev = PrevJoins(src, i);
                    if (pc == '‌') prev = false;
                }
                if (c == 'ل' && i + 1 < src.Count)
                {
                    char n = src[i + 1].Ch;
                    char lig = '\0';
                    if (n == 'ا') lig = prev ? 'ﻼ' : 'ﻻ';
                    else if (n == 'آ') lig = prev ? 'ﻶ' : 'ﻵ';
                    else if (n == 'أ') lig = prev ? 'ﻸ' : 'ﻷ';
                    else if (n == 'إ') lig = prev ? 'ﻺ' : 'ﻹ';
                    if (lig != '\0') { outList.Add(new Item { Ch = lig, Stack = src[i].Stack }); i++; continue; }
                }
                bool next = g.JoinsNext && i + 1 < src.Count && Connects(src[i + 1].Ch);
                outList.Add(new Item { Ch = Pick(g, prev, next, font), Stack = src[i].Stack });
            }
            return outList;
        }

        private static bool PrevJoins(List<Item> src, int i)
        {
            if (i == 0) return false;
            char p = src[i - 1].Ch;
            Glyph pg;
            return map.TryGetValue(p, out pg) && pg.JoinsNext && p != 'ء';
        }

        private static List<Item> Parse(string rich)
        {
            var items = new List<Item>();
            var stack = new List<string>();
            var names = new List<string>();
            string[] current = noStack;
            for (int i = 0; i < rich.Length; i++)
            {
                char c = rich[i];
                if (c == '<')
                {
                    int end = rich.IndexOf('>', i);
                    if (end > i)
                    {
                        string tag = rich.Substring(i, end - i + 1);
                        bool closing = tag.StartsWith("</");
                        string name = TagName(tag);
                        if (closing)
                        {
                            int idx = names.LastIndexOf(name);
                            if (idx >= 0) { stack.RemoveRange(idx, stack.Count - idx); names.RemoveRange(idx, names.Count - idx); }
                        }
                        else { stack.Add(tag); names.Add(name); }
                        current = stack.Count == 0 ? noStack : stack.ToArray();
                        i = end;
                        continue;
                    }
                }
                items.Add(new Item { Ch = c, Stack = current });
            }
            return items;
        }

        private static string TagName(string tag)
        {
            int s = tag.StartsWith("</") ? 2 : 1;
            int e = s;
            while (e < tag.Length && tag[e] != '>' && tag[e] != '=' && tag[e] != ' ') e++;
            return tag.Substring(s, e - s).ToLowerInvariant();
        }

        private static List<Item> Reorder(List<Item> items)
        {
            if (items.Count == 0) return items;
            var types = new int[items.Count];
            for (int i = 0; i < items.Count; i++) types[i] = IsLtr(items[i].Ch) ? 1 : (IsRtl(items[i].Ch) ? 2 : 0);
            var runs = new List<int[]>();
            int start = 0;
            for (int i = 1; i <= items.Count; i++)
            {
                if (i == items.Count || types[i] != types[start]) { runs.Add(new[] { start, i - 1, types[start] }); start = i; }
            }
            for (int r = 0; r < runs.Count; r++)
            {
                if (runs[r][2] != 0) continue;
                bool l = r > 0 && runs[r - 1][2] == 1 && r < runs.Count - 1 && runs[r + 1][2] == 1;
                runs[r][2] = l ? 1 : 2;
            }
            var merged = new List<int[]>();
            foreach (int[] run in runs)
            {
                if (merged.Count > 0 && merged[merged.Count - 1][2] == run[2]) merged[merged.Count - 1][1] = run[1];
                else merged.Add(new[] { run[0], run[1], run[2] });
            }
            var result = new List<Item>(items.Count);
            for (int r = merged.Count - 1; r >= 0; r--)
            {
                int a = merged[r][0], b = merged[r][1];
                if (merged[r][2] == 1) { for (int i = a; i <= b; i++) result.Add(items[i]); }
                else for (int i = b; i >= a; i--) result.Add(new Item { Ch = Mirror(items[i].Ch), Stack = items[i].Stack });
            }
            return result;
        }

        private static string Emit(List<Item> items)
        {
            var sb = new StringBuilder(items.Count + 16);
            string[] cur = noStack;
            foreach (Item it in items)
            {
                if (it.Ch == '‌') continue;
                if (!ReferenceEquals(it.Stack, cur))
                {
                    int common = 0;
                    while (common < cur.Length && common < it.Stack.Length && cur[common] == it.Stack[common]) common++;
                    for (int k = cur.Length - 1; k >= common; k--) sb.Append("</").Append(TagName(cur[k])).Append('>');
                    for (int k = common; k < it.Stack.Length; k++) sb.Append(it.Stack[k]);
                    cur = it.Stack;
                }
                sb.Append(it.Ch);
            }
            for (int k = cur.Length - 1; k >= 0; k--) sb.Append("</").Append(TagName(cur[k])).Append('>');
            return sb.ToString();
        }

        public static string Visual(string rich, Font font = null)
        {
            if (string.IsNullOrEmpty(rich)) return "";
            if (font == null) font = Fonts.Persian();
            List<Item> all = Parse(rich);
            var sb = new StringBuilder();
            var line = new List<Item>();
            bool first = true;
            for (int i = 0; i <= all.Count; i++)
            {
                if (i < all.Count && all[i].Ch != '\n') { line.Add(all[i]); continue; }
                if (!first) sb.Append('\n');
                first = false;
                sb.Append(Emit(Reorder(Shape(line, font))));
                line.Clear();
            }
            return sb.ToString();
        }

        public static float Measure(string rich, Font font, int size, FontStyle style)
        {
            string plain = TextUtil.Plain(rich);
            var shaped = Shape(Parse(plain), font);
            var sb = new StringBuilder();
            foreach (Item it in shaped) if (it.Ch != '‌') sb.Append(it.Ch);
            string text = sb.ToString();
            if (text.Length == 0) return 0f;
            font.RequestCharactersInTexture(text, size, style);
            float w = 0f;
            CharacterInfo info;
            foreach (char c in text) if (font.GetCharacterInfo(c, out info, size, style)) w += info.advance;
            return w;
        }

        public static string Layout(string rich, Font font, int size, FontStyle style, float width)
        {
            if (string.IsNullOrEmpty(rich)) return "";
            string[] paragraphs = rich.Split('\n');
            var lines = new List<string>();
            foreach (string para in paragraphs)
            {
                if (width <= 20f || Measure(para, font, size, style) <= width) { lines.Add(para); continue; }
                string[] words = para.Split(' ');
                string line = "";
                foreach (string w in words)
                {
                    string trial = line.Length == 0 ? w : line + " " + w;
                    if (line.Length > 0 && Measure(trial, font, size, style) > width) { lines.Add(line); line = w; }
                    else line = trial;
                }
                if (line.Length > 0) lines.Add(line);
            }
            return Visual(string.Join("\n", lines.ToArray()), font);
        }
    }

    public sealed class FaText : MonoBehaviour
    {
        private Text target;
        private string logical = "";
        private TextAnchor baseAnchor;
        private float lastWidth = -1f;
        private int lastVersion = -1;
        private bool dirty = true;

        public static FaText Attach(Text t)
        {
            var f = t.gameObject.AddComponent<FaText>();
            f.target = t;
            f.baseAnchor = t.alignment;
            return f;
        }

        public string Logical { get { return logical; } }

        public void Set(string rich)
        {
            rich = rich ?? "";
            if (rich == logical && !dirty) return;
            logical = rich;
            dirty = true;
            Apply();
        }

        public void SetAnchor(TextAnchor a) { baseAnchor = a; dirty = true; Apply(); }

        private static TextAnchor Flip(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAnchor.UpperRight;
                case TextAnchor.UpperRight: return TextAnchor.UpperLeft;
                case TextAnchor.MiddleLeft: return TextAnchor.MiddleRight;
                case TextAnchor.MiddleRight: return TextAnchor.MiddleLeft;
                case TextAnchor.LowerLeft: return TextAnchor.LowerRight;
                case TextAnchor.LowerRight: return TextAnchor.LowerLeft;
                default: return a;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float w = target.rectTransform.rect.width;
            if (dirty || lastVersion != Tr.Version || (Tr.IsFa && Mathf.Abs(w - lastWidth) > 2f)) Apply();
        }

        private void Apply()
        {
            if (target == null) return;
            dirty = false;
            lastVersion = Tr.Version;
            lastWidth = target.rectTransform.rect.width;
            if (!Tr.IsFa && !Persian.HasRtl(logical))
            {
                target.font = Fonts.Get();
                target.alignment = baseAnchor;
                target.horizontalOverflow = HorizontalWrapMode.Wrap;
                target.text = logical;
                return;
            }
            Font f = Fonts.Persian();
            target.font = f;
            target.alignment = Tr.IsFa ? Flip(baseAnchor) : baseAnchor;
            target.horizontalOverflow = HorizontalWrapMode.Overflow;
            float width = lastWidth - 4f;
            target.text = Persian.Layout(logical, f, target.fontSize, target.fontStyle, width);
        }
    }
}
