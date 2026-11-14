using TMPro;
using UnityEngine;

namespace Numen.Chie.UI
{
    /// <summary>文字代替人物与场景：一层 World Space 文字，压在画布上、对话下。</summary>
    public class ChieTextStage : MonoBehaviour
    {
        private const int W = 1920;
        private const int H = 1080;

        private FlawJudge _judge;
        private PoseId _pose = (PoseId)(-1);
        private TMP_Text _poseLine;

        private static readonly Color ColScene = new Color32(0xC9, 0xB7, 0x9A, 0xC0);
        private static readonly Color ColChar = new Color32(0xE8, 0xD9, 0xC4, 0xB4);
        private static readonly Color ColPose = new Color32(0xC9, 0x96, 0x3F, 0xE6);
        private static readonly Color ColHint = new Color32(0x8B, 0x7D, 0x6D, 0x9A);

        /// <summary>搭出文字层。font 可空，空则用 TMP 默认。</summary>
        public void Build(ChieConfig config, FlawJudge judge, TMP_FontAsset font, Camera cam)
        {
            _judge = judge;

            var go = new GameObject("TextStageCanvas", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.pivot = Vector2.zero;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.sizeDelta = new Vector2(W, H);
            rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            rt.localPosition = new Vector3(-W * 0.005f, -H * 0.005f, -1f);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            canvas.sortingOrder = 12;

            // 场景：小屋窗边
            MakeText(rt, "SceneTitle", font, "场景：小 屋 · 窗 边",
                new Vector2(64f, 972f), new Vector2(1000f, 56f), 40f, TextAlignmentOptions.Left, ColScene);
            MakeText(rt, "SceneWindow", font, "窗 外",
                new Vector2(1362f, 792f), new Vector2(300f, 60f), 42f, TextAlignmentOptions.Center, ColScene);
            MakeText(rt, "SceneShelf", font, "窗 台",
                new Vector2(1352f, 604f), new Vector2(300f, 48f), 32f, TextAlignmentOptions.Center, ColScene);

            // 人物：少女（竖排两字）
            MakeText(rt, "CharName", font, "少\n女",
                new Vector2(686f, 286f), new Vector2(200f, 400f), 104f, TextAlignmentOptions.Top, ColChar);

            _poseLine = MakeText(rt, "CharPose", font, "",
                new Vector2(560f, 236f), new Vector2(460f, 56f), 36f, TextAlignmentOptions.Center, ColPose);

            MakeText(rt, "StandInHint", font, "（文字占位：人物＝少女，场景＝小屋窗边，画布仍是可画的图）",
                new Vector2(64f, 32f), new Vector2(1300f, 44f), 26f, TextAlignmentOptions.Left, ColHint);

            Refresh(true);
        }

        private void Update()
        {
            Refresh(false);
        }

        private void Refresh(bool force)
        {
            if (_poseLine == null) return;
            PoseId cur = _judge != null ? _judge.Pose : PoseId.Sit;
            if (!force && cur == _pose) return;
            _pose = cur;
            _poseLine.text = "〔" + PoseWord(cur) + "〕";
        }

        /// <summary>姿态中文名。</summary>
        public static string PoseWord(PoseId p)
        {
            switch (p)
            {
                case PoseId.Sit: return "坐";
                case PoseId.Shift: return "侧身";
                case PoseId.Up: return "起身";
                default: return "立";
            }
        }

        private static TMP_Text MakeText(Transform parent, string name, TMP_FontAsset font, string text,
            Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.pivot = Vector2.zero;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color;
            t.enableWordWrapping = false;
            t.raycastTarget = false;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }
    }
}
