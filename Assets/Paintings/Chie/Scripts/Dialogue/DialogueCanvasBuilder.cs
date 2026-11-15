using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Numen.Chie
{
    /// <summary>把画内对话的层级建出来。只搭壳，不做播放。</summary>
    public static class DialogueCanvasBuilder
    {
        public const float CanvasW = 1920f;
        public const float CanvasH = 1080f;
        public const float BodyFontSize = 34f;
        public const float SpeakerFontSize = 22f;
        public const float OptionFontSize = 32f;

        /// <summary>搭出整块对话层，返回引用集合。</summary>
        public static DialogueCanvasParts Build(ChieConfig config, TMP_FontAsset font, Camera cam,
            Transform parent, Action<int> onOptionClicked)
        {
            var parts = new DialogueCanvasParts();

            var go = new GameObject("DialogueCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasGroup), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);

            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(CanvasW, CanvasH);
            rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            rt.localPosition = new Vector3(0f, 0f, -1f);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            canvas.sortingOrder = 20;
            parts.group = go.GetComponent<CanvasGroup>();

            RectTransform content = MakeNode(go.transform, "Content", CanvasW, CanvasH,
                Vector2.zero, Vector2.zero);

            parts.dim = BuildDim(content, config);
            parts.speaker = MakeText(content, "Speaker", font, SpeakerFontSize,
                new Vector2(154f, 300f), new Vector2(1500f, 40f));
            parts.body = MakeText(content, "Body", font, BodyFontSize,
                new Vector2(154f, 96f), new Vector2(1500f, 180f));
            parts.body.alignment = TextAlignmentOptions.TopLeft;
            parts.body.lineSpacing = config != null ? config.BodyLineSpacing : 0f;
            ApplyOutline(parts.body);

            // 三行 × 68 = 204，容器留 220，避免末行溢出父 rect 后射线被裁掉
            parts.choiceRoot = MakeNode(content, "ChoiceGroup", 1500f, 220f,
                new Vector2(154f, 360f), Vector2.zero);

            parts.options = new ChieOptionLine[3];
            for (int i = 0; i < 3; i++)
            {
                parts.options[i] = MakeOption(parts.choiceRoot, font, config, i, onOptionClicked);
            }
            return parts;
        }

        private static RectTransform MakeNode(Transform parent, string name,
            float w, float h, Vector2 pos, Vector2 sizeOverride)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeOverride == Vector2.zero ? new Vector2(w, h) : sizeOverride;
            return rt;
        }

        private static Image BuildDim(RectTransform parent, ChieConfig config)
        {
            var go = new GameObject("DimGradient", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(CanvasW, 420f);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.type = Image.Type.Simple;
            if (config != null && config.DimTex != null)
            {
                Texture2D t = config.DimTex;
                img.sprite = Sprite.Create(t, new Rect(0f, 0f, t.width, t.height),
                    new Vector2(0.5f, 0.5f), 100f);
            }
            img.color = Color.white;
            return img;
        }

        private static TextMeshProUGUI MakeText(RectTransform parent, string name, TMP_FontAsset font,
            float size, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;

            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = size;
            t.enableWordWrapping = true;
            t.raycastTarget = false;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        private static ChieOptionLine MakeOption(RectTransform parent, TMP_FontAsset font,
            ChieConfig config, int i, Action<int> onOptionClicked)
        {
            // 容器只承载位置与透明度；文字与透明命中框都是子物体。
            var go = new GameObject("OptionLine_" + i, typeof(RectTransform), typeof(CanvasGroup),
                typeof(ChieOptionLine));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(0f, -i * 68f);
            rt.sizeDelta = new Vector2(1500f, 64f);

            // 全行透明命中框：文字没生成字形时 TMP 自身射线会塌成 0 面积，这层兜住整行。
            var hitGo = new GameObject("Hit", typeof(RectTransform), typeof(Image));
            hitGo.transform.SetParent(go.transform, false);
            var hitRt = (RectTransform)hitGo.transform;
            hitRt.anchorMin = Vector2.zero;
            hitRt.anchorMax = Vector2.one;
            hitRt.offsetMin = Vector2.zero;
            hitRt.offsetMax = Vector2.zero;
            var hit = hitGo.GetComponent<Image>();
            hit.raycastTarget = true;
            hit.color = new Color(1f, 1f, 1f, 0f);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(go.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            var t = textGo.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.fontSize = OptionFontSize;
            t.enableWordWrapping = false;
            t.overflowMode = TextOverflowModes.Overflow;
            t.alignment = TextAlignmentOptions.Left;
            t.raycastTarget = false;
            t.text = "";

            var line = go.GetComponent<ChieOptionLine>();
            line.Bind(hit, t, go.GetComponent<CanvasGroup>(), rt, i,
                config != null ? config.OptionHoverIndent : 12f,
                config != null ? config.OptionHoverSeconds : 0.12f, onOptionClicked);
            return line;
        }

        /// <summary>给正文加深色描边，窗外全白那一帧也读得清。</summary>
        public static void ApplyOutline(TextMeshProUGUI t)
        {
            if (t == null || t.font == null) return;
            Material m = new Material(t.font.material);
            m.EnableKeyword(ShaderUtilities.Keyword_Outline);
            m.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.12f);
            m.SetColor(ShaderUtilities.ID_OutlineColor, new Color32(0x0A, 0x08, 0x06, 0xFF));
            m.DisableKeyword(ShaderUtilities.Keyword_Underlay);
            t.fontMaterial = m;
        }
    }
}
