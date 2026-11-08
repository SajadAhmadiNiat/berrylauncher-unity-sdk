using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BerryShooter
{
    public static class Ui
    {
        public static readonly Color Bg = new Color(0.055f, 0.067f, 0.094f, 0.94f);
        public static readonly Color Card = new Color(0.09f, 0.105f, 0.14f, 0.96f);
        public static readonly Color Line = new Color(1, 1, 1, 0.08f);
        public static readonly Color Accent = new Color(0.88f, 0.12f, 0.32f, 1f);
        public static readonly Color Ink = new Color(0.95f, 0.96f, 0.98f, 1f);
        public static readonly Color Muted = new Color(0.62f, 0.66f, 0.74f, 1f);
        public static readonly Color Good = new Color(0.3f, 0.85f, 0.55f, 1f);
        public static readonly Color Warn = new Color(0.98f, 0.75f, 0.2f, 1f);
        public static readonly Color Bad = new Color(0.95f, 0.35f, 0.35f, 1f);
        public static readonly Color RowBg = new Color(1, 1, 1, 0.06f);
        public static readonly Color RowOn = new Color(0.88f, 0.12f, 0.32f, 0.35f);

        public static Canvas MakeCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = 0.5f;
            return c;
        }

        public static void EnsureEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;
#else
            if (Object.FindObjectOfType<EventSystem>() != null) return;
#endif
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        public static RectTransform Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = color.a > 0.01f;
            return rt;
        }

        public static RectTransform Fill(Transform parent, string name, Color color)
        {
            return Panel(parent, name, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, color);
        }

        public static RectTransform Column(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Color color, int pad = 18, float gap = 10f)
        {
            var rt = Panel(parent, name, aMin, aMax, oMin, oMax, color);
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.spacing = gap;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return rt;
        }

        public static RectTransform Row(Transform parent, float height, float gap = 8f)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var h = go.GetComponent<HorizontalLayoutGroup>();
            h.spacing = gap;
            h.childControlWidth = true; h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            return rt;
        }

        public static Text Label(Transform parent, string text, int size = 26, Color? color = null, TextAnchor anchor = TextAnchor.MiddleLeft, float height = -1f, bool bold = false)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Fonts.Get();
            t.fontSize = size;
            t.color = color ?? Ink;
            t.alignment = anchor;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            FaText.Attach(t);
            Set(t, text);
            if (height > 0f)
            {
                var le = go.AddComponent<LayoutElement>();
                le.preferredHeight = height; le.minHeight = height;
            }
            return t;
        }

        public static void Set(Text t, string text)
        {
            if (t == null) return;
            var f = t.GetComponent<FaText>();
            if (f != null) f.Set(text);
            else t.text = text;
        }

        public static Text Stretch(Transform parent, string text, int size, Color color, TextAnchor anchor)
        {
            var t = Label(parent, text, size, color, anchor);
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(12, 0); rt.offsetMax = new Vector2(-12, 0);
            return t;
        }

        public static Button Btn(Transform parent, string label, UnityAction onClick, float height = 64f, bool primary = false, int size = 26)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            var img = go.GetComponent<Image>();
            img.color = primary ? Accent : new Color(1, 1, 1, 0.09f);
            var b = go.GetComponent<Button>();
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.disabledColor = new Color(1, 1, 1, 0.35f);
            b.colors = cb;
            b.targetGraphic = img;
            b.onClick.AddListener(() => Sfx.Play(Sfx.Kind.Click, 0.6f));
            if (onClick != null) b.onClick.AddListener(onClick);
            Stretch(go.transform, label, size, Ink, TextAnchor.MiddleCenter);
            return b;
        }

        public static void SetLabel(Button b, string text)
        {
            if (b == null) return;
            Set(b.GetComponentInChildren<Text>(), text);
        }

        public static InputField Field(Transform parent, string placeholder, string initial, float height = 60f, InputField.ContentType type = InputField.ContentType.Standard)
        {
            var go = new GameObject("Input", typeof(RectTransform), typeof(Image), typeof(InputField), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0.45f);
            var text = Stretch(go.transform, "", 26, Ink, TextAnchor.MiddleLeft);
            DestroyFa(text);
            text.supportRichText = false;
            text.font = Fonts.Persian();
            var ph = Stretch(go.transform, placeholder, 24, Muted, TextAnchor.MiddleLeft);
            ph.fontStyle = FontStyle.Italic;
            var f = go.GetComponent<InputField>();
            f.textComponent = text;
            f.placeholder = ph;
            f.contentType = type;
            f.text = initial ?? "";
            f.targetGraphic = go.GetComponent<Image>();
            return f;
        }

        private static void DestroyFa(Text t)
        {
            var f = t.GetComponent<FaText>();
            if (f != null) Object.DestroyImmediate(f);
        }

        public static Slider Sld(Transform parent, float min, float max, float value, UnityAction<float> onChange, float height = 40f)
        {
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            Panel(go.transform, "Background", new Vector2(0, 0.35f), new Vector2(1, 0.65f), Vector2.zero, Vector2.zero, new Color(1, 1, 1, 0.12f));
            var area = Panel(go.transform, "Fill Area", new Vector2(0, 0.35f), new Vector2(1, 0.65f), new Vector2(0, 0), new Vector2(-18, 0), Color.clear);
            var fill = Panel(area, "Fill", Vector2.zero, Vector2.one, Vector2.zero, new Vector2(18, 0), Accent);
            var handleArea = Panel(go.transform, "Handle Slide Area", Vector2.zero, Vector2.one, new Vector2(10, 0), new Vector2(-10, 0), Color.clear);
            var handle = Panel(handleArea, "Handle", new Vector2(0, 0), new Vector2(0, 1), new Vector2(-10, -4), new Vector2(10, 4), Color.white);
            var s = go.GetComponent<Slider>();
            s.fillRect = fill;
            s.handleRect = handle;
            s.targetGraphic = handle.GetComponent<Image>();
            s.direction = Slider.Direction.LeftToRight;
            s.minValue = min; s.maxValue = max; s.value = value;
            if (onChange != null) s.onValueChanged.AddListener(onChange);
            return s;
        }

        public static RectTransform Scroll(Transform parent, float height)
        {
            var go = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            if (height < 0f) { le.preferredHeight = -1; le.minHeight = 0; le.flexibleHeight = 1; }
            var view = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            var vrt = (RectTransform)view.transform;
            vrt.SetParent(rt, false);
            vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one; vrt.offsetMin = Vector2.zero; vrt.offsetMax = Vector2.zero;
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            var crt = (RectTransform)content.transform;
            crt.SetParent(vrt, false);
            crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1); crt.pivot = new Vector2(0.5f, 1f);
            crt.offsetMin = Vector2.zero; crt.offsetMax = Vector2.zero;
            var v = content.GetComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(8, 8, 8, 8);
            v.spacing = 6;
            v.childControlWidth = true; v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var sr = go.GetComponent<ScrollRect>();
            sr.viewport = vrt;
            sr.content = crt;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 40f;
            return crt;
        }

        public static Button ListItem(Transform parent, string left, string right, bool selected, UnityAction onClick, float height = 64f)
        {
            var go = new GameObject("Item", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var le = go.GetComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            var img = go.GetComponent<Image>();
            img.color = selected ? RowOn : RowBg;
            var b = go.GetComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            b.colors = cb;
            if (onClick != null)
            {
                b.onClick.AddListener(() => Sfx.Play(Sfx.Kind.Click, 0.5f));
                b.onClick.AddListener(onClick);
            }
            var l = Stretch(go.transform, left, 24, Ink, TextAnchor.MiddleLeft);
            var r = Stretch(go.transform, right, 22, Muted, TextAnchor.MiddleRight);
            l.rectTransform.offsetMax = new Vector2(-260, 0);
            r.rectTransform.anchorMin = new Vector2(1, 0);
            r.rectTransform.anchorMax = new Vector2(1, 1);
            r.rectTransform.pivot = new Vector2(1, 0.5f);
            r.rectTransform.sizeDelta = new Vector2(250, 0);
            r.rectTransform.anchoredPosition = new Vector2(-12, 0);
            return b;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.Destroy(t.GetChild(i).gameObject);
        }

        public static Color PingColor(int ms)
        {
            switch (BerrySdk.BerryUtil.PingQuality(ms))
            {
                case BerrySdk.BerryPingQuality.Excellent: return Good;
                case BerrySdk.BerryPingQuality.Good: return new Color(0.6f, 0.9f, 0.4f);
                case BerrySdk.BerryPingQuality.Fair: return Warn;
                case BerrySdk.BerryPingQuality.Poor: return Bad;
                default: return Muted;
            }
        }

        public static string ColorHex(Color c) { return "#" + ColorUtility.ToHtmlStringRGB(c); }
    }
}
