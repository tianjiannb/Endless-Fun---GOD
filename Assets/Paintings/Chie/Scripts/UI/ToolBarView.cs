using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Numen.Chie.UI
{
    /// <summary>画面内工具栏（World Space，order 30）：只画图标，不画底板。</summary>
    public class ToolBarView : MonoBehaviour
    {
        private ChieConfig _config;
        private ChieToolButton[] _buttons;
        private TextMeshProUGUI _hint;
        private CanvasGroup _group;
        private bool[] _unlocked;

        private const float ColumnX = 40f;    // 左边缘内缩；列占 40..136，距影子区左缘 150 留 14px
        private const float ColumnY = 700f;   // 从画布底部往上 700，避开左下影子区
        private const float ButtonSize = 96f;
        private const float ButtonGap = 24f;

        private static readonly ToolId[] Order =
        {
            ToolId.Brush, ToolId.Eraser, ToolId.Eyedropper, ToolId.PutDown
        };

        /// <summary>选了某个工具（放笔单独走 OnPutDown）。</summary>
        public event Action<ToolId> OnToolPicked;

        /// <summary>点了放下笔。</summary>
        public event Action OnPutDown;

        /// <summary>当前工具。</summary>
        public ToolId Current { get; private set; }

        /// <summary>建出工具栏。</summary>
        public void Build(ChieConfig config, Camera cam)
        {
            _config = config;
            _unlocked = new bool[4];

            var canvasGo = new GameObject("ToolBarCanvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasGroup), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.layer = 5;
            var rt = (RectTransform)canvasGo.transform;
            rt.sizeDelta = new Vector2(1920f, 1080f);
            rt.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            rt.localPosition = new Vector3(0f, 0f, -1f);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            canvas.sortingOrder = 30;
            _group = canvasGo.GetComponent<CanvasGroup>();
            _group.alpha = 0f;

            // 竖排贴左边缘：原来横排压在左下角的「影子」破绽上，看不清要改的地方。
            // 左列占 x 40..136、y 700..1012，六处破绽都不在这块。
            var root = new GameObject("ToolColumn", typeof(RectTransform));
            root.transform.SetParent(canvasGo.transform, false);
            var rrt = (RectTransform)root.transform;
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.zero;
            rrt.pivot = Vector2.zero;
            rrt.anchoredPosition = new Vector2(ColumnX, ColumnY);
            rrt.sizeDelta = new Vector2(96f, 96f * Order.Length + 24f * (Order.Length - 1));

            _buttons = new ChieToolButton[Order.Length];
            for (int i = 0; i < Order.Length; i++)
            {
                _buttons[i] = MakeButton(rrt, Order[i], i);
            }

            _hint = MakeHint(canvasGo.transform);
            Refresh();
        }

        private ChieToolButton MakeButton(RectTransform parent, ToolId tool, int index)
        {
            // 容器只负责位置与整体透明度，点击交给子物体上的透明命中框。
            var go = new GameObject("Tool_" + tool, typeof(RectTransform), typeof(CanvasGroup),
                typeof(ChieToolButton));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            // 竖排：index 0 在最上；每格 96 + 24 间距 = 120
            rt.anchoredPosition = new Vector2(0f, -index * (ButtonSize + ButtonGap));
            rt.sizeDelta = new Vector2(ButtonSize, ButtonSize);

            // 命中框铺满整格：比图标本身大，图标再小也点得到。
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

            // 图标层不参与射线：避免 sprite 透明处命中不稳定。
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = Vector2.zero;
            iconRt.offsetMax = Vector2.zero;
            var img = iconGo.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;

            Sprite s = IconOf(tool);
            if (s != null)
            {
                img.sprite = s;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(0.7f, 0.6f, 0.5f, 0.9f);
            }

            var cg = go.GetComponent<CanvasGroup>();
            var btn = go.GetComponent<ChieToolButton>();
            btn.Bind(hit, img, cg, tool, HandleClick);
            return btn;
        }

        private Sprite IconOf(ToolId tool)
        {
            if (_config == null) return null;
            Texture2D t = null;
            switch (tool)
            {
                case ToolId.Brush: t = _config.IconBrush; break;
                case ToolId.Eraser: t = _config.IconEraser; break;
                case ToolId.Eyedropper: t = _config.IconEyedropper; break;
                case ToolId.PutDown: t = _config.IconPutDown; break;
            }
            if (t == null) return null;
            return Sprite.Create(t, new Rect(0f, 0f, t.width, t.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private TextMeshProUGUI MakeHint(Transform parent)
        {
            var go = new GameObject("Hint", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            // 提示放竖排工具栏正下方，左对齐，不压画面中心
            rt.anchoredPosition = new Vector2(ColumnX, ColumnY - 56f);
            rt.sizeDelta = new Vector2(520f, 40f);

            var t = go.GetComponent<TextMeshProUGUI>();
            if (_config != null && _config.Font != null) t.font = _config.Font;
            t.fontSize = 22f;
            t.alignment = TextAlignmentOptions.TopLeft;
            t.color = new Color32(0x9A, 0x8F, 0x80, 0xFF);
            t.raycastTarget = false;
            t.text = "";
            return t;
        }

        /// <summary>修画阶段显示 / 隐藏整个工具栏。</summary>
        public void SetVisible(bool visible)
        {
            if (_group == null) return;
            _group.alpha = visible ? 1f : 0f;
            _group.blocksRaycasts = visible;
        }

        /// <summary>刷新解锁状态与选中态。</summary>
        public void Refresh()
        {
            Refresh(null);
        }

        /// <summary>按 WaveUnlock 刷新解锁，并选中当前工具。</summary>
        public void Refresh(WaveUnlock wave)
        {
            if (_buttons == null) return;
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] == null) continue;
                ToolId tool = _buttons[i].Tool;
                bool unlocked = wave == null || wave.IsToolUnlocked(tool);
                bool was = _unlocked[i];
                _unlocked[i] = unlocked;

                if (unlocked && !was && tool != ToolId.Brush && tool != ToolId.PutDown) _buttons[i].Appear();
                else if (unlocked) _buttons[i].ShowNow();
                else _buttons[i].HideNow();

                _buttons[i].SetSelected(tool == Current);
            }
        }

        /// <summary>外部切工具。</summary>
        public void SetTool(ToolId tool)
        {
            Current = tool;
            if (_buttons == null) return;
            for (int i = 0; i < _buttons.Length; i++)
            {
                if (_buttons[i] != null) _buttons[i].SetSelected(_buttons[i].Tool == tool);
            }
        }

        /// <summary>底部提示一行（例如"这一处得先吸她身上的颜色"）。</summary>
        public void SetHint(string text)
        {
            if (_hint != null) _hint.text = text != null ? text : "";
        }

        private void HandleClick(ToolId tool)
        {
            if (tool == ToolId.PutDown)
            {
                if (OnPutDown != null) OnPutDown();
                return;
            }
            SetTool(tool);
            if (OnToolPicked != null) OnToolPicked(tool);
        }
    }
}
