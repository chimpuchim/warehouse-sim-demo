using System;
using UnityEngine;
using UnityEngine.UI;

namespace Warehouse
{
    /// <summary>uGUI built in code with the built-in font, so the project needs no UI assets.</summary>
    public static class UI
    {
        public static readonly Color Panel = new Color(0.06f, 0.08f, 0.11f, 0.88f);
        public static readonly Color PanelLight = new Color(0.14f, 0.17f, 0.22f, 0.95f);
        public static readonly Color Accent = new Color(0.98f, 0.76f, 0.18f);
        public static readonly Color Text = new Color(0.95f, 0.96f, 0.97f);
        public static readonly Color Muted = new Color(0.62f, 0.66f, 0.72f);
        public static readonly Color Good = new Color(0.25f, 0.85f, 0.55f);
        public static readonly Color Bad = new Color(0.92f, 0.32f, 0.32f);

        static Font _font;
        public static Font Font => _font != null ? _font : _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static Canvas Canvas(string name, int order)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        /// <summary>Rect anchored at a point with a fixed size; pivot = anchor.</summary>
        public static RectTransform Fixed(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = Rect(name, parent, anchor, anchor, Vector2.zero, Vector2.zero);
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Image(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(RectTransform rt, string text, int size, Color color, TextAnchor anchor = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.fontSize = size;
            t.color = color;
            t.text = text;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Text Label(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string text, int fontSize, Color color, TextAnchor align = TextAnchor.UpperLeft, FontStyle style = FontStyle.Normal)
            => Label(Fixed(name, parent, anchor, pos, size), text, fontSize, color, align, style);

        public static Button Button(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string label, Action onClick, Color? bg = null, int fontSize = 20)
        {
            var rt = Fixed("Button " + label, parent, anchor, pos, size);
            var img = Image(rt, bg ?? PanelLight);
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());
            var text = Label(Rect("Label", rt, Vector2.zero, Vector2.one, new Vector2(8, 4), new Vector2(-8, -4)), label, fontSize, Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return btn;
        }

        public static Slider Slider(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var rt = Fixed("Slider", parent, anchor, pos, size);
            var slider = rt.gameObject.AddComponent<Slider>();
            var bg = Rect("Background", rt, new Vector2(0, 0.35f), new Vector2(1, 0.65f), Vector2.zero, Vector2.zero);
            Image(bg, new Color(1, 1, 1, 0.15f));
            var fillArea = Rect("Fill Area", rt, new Vector2(0, 0.35f), new Vector2(1, 0.65f), Vector2.zero, Vector2.zero);
            var fill = Rect("Fill", fillArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image(fill, Accent);
            var handleArea = Rect("Handle Slide Area", rt, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var handle = Rect("Handle", handleArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            handle.sizeDelta = new Vector2(14, 0);
            Image(handle, Text);
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            return slider;
        }

        public static InputField Input(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string value, int fontSize = 20)
        {
            var rt = Fixed("Input", parent, anchor, pos, size);
            var img = Image(rt, new Color(1, 1, 1, 0.08f));
            var field = rt.gameObject.AddComponent<InputField>();
            field.targetGraphic = img;
            var textRt = Rect("Text", rt, Vector2.zero, Vector2.one, new Vector2(10, 4), new Vector2(-10, -4));
            var text = Label(textRt, value, fontSize, Text, TextAnchor.MiddleLeft);
            text.supportRichText = false;
            field.textComponent = text;
            field.text = value;
            field.characterLimit = 16;
            return field;
        }

        public static string Time(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60:0}:{s % 60:00}";
        }
    }
}
