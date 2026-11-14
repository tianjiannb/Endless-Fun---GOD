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

            var root = new GameObject("ToolRow", typeof(RectTransform));
            root.transform.SetParent(canvasGo.transform, false);
            var rrt = (RectTransform)root.transform;
            rrt.anchorMin = Vector2.zero;
            rrt.anchorMax = Vector2.zero;
            rrt.pivot = Vector2.zero;
            rrt.anchoredPosition = new Vector2(120f, 60f);
            rrt.sizeDelta = new Vector2(600f, 96f);

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
            var go = new GameObject("Tool_" + tool, typeof(RectTransform), typeof(Image),
                typeof(CanvasGroup), typeof(ChieToolButton));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(index * 104f, 0f);
            rt.sizeDelta = new Vector2(96f, 96f);

            var img = go.GetComponent<Image>();
            img.raycastTarget = true;
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
            btn.Bind(img, cg, tool, HandleClick);
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
            rt.anchoredPosition = new Vector2(120f, 172f);
            rt.sizeDelta = new Vector2(900f, 40f);

            var t = go.GetComponent<TextMeshProUGUI>();
            if (_config != null && _config.Font != null) t.font = _config.Font;
            t.fontSize = 22f;
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
