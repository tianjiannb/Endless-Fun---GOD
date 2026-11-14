using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Numen.Chie.UI
{
    /// <summary>工具栏一个按钮：图标 + 悬停变金 + 选中描边。无面板框。</summary>
    public class ChieToolButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image _icon;
        [SerializeField] private CanvasGroup _group;

        private ToolId _tool;
        private Action<ToolId> _onClick;
        private float _hoverT;
        private bool _hover;
        private bool _selected;
        private float _appearT;
        private bool _appearing;

        private static readonly Color ColIdle = new Color32(0x8B, 0x7D, 0x6D, 0xFF);
        private static readonly Color ColHover = new Color32(0xC9, 0x96, 0x3F, 0xFF);
        private static readonly Color ColOn = new Color32(0xF0, 0xE2, 0xC8, 0xFF);

        /// <summary>工具 id。</summary>
        public ToolId Tool { get { return _tool; } }

        /// <summary>注入引用。</summary>
        public void Bind(Image icon, CanvasGroup group, ToolId tool, Action<ToolId> onClick)
        {
            _icon = icon;
            _group = group;
            _tool = tool;
            _onClick = onClick;
            if (_icon != null) _icon.color = ColIdle;
        }

        /// <summary>0.15s 淡入出现（解锁时用）。</summary>
        public void Appear()
        {
            gameObject.SetActive(true);
            _appearing = true;
            _appearT = 0f;
            if (_group != null) _group.alpha = 0f;
        }

        /// <summary>立刻显示。</summary>
        public void ShowNow()
        {
            gameObject.SetActive(true);
            _appearing = false;
            _appearT = 1f;
            if (_group != null) _group.alpha = 1f;
        }

        /// <summary>隐藏（未解锁）。</summary>
        public void HideNow()
        {
            _appearing = false;
            if (_group != null) _group.alpha = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>设置选中态。</summary>
        public void SetSelected(bool on)
        {
            _selected = on;
            Paint();
        }

        private void Paint()
        {
            if (_icon == null) return;
            if (_selected) _icon.color = ColOn;
            else _icon.color = Color.Lerp(ColIdle, ColHover, _hoverT);
        }

        private void Update()
        {
            if (_appearing)
            {
                _appearT = Mathf.MoveTowards(_appearT, 1f, Time.deltaTime / 0.15f);
                if (_group != null) _group.alpha = _appearT;
                if (_appearT >= 1f) _appearing = false;
            }

            float target = (_hover && !_selected) ? 1f : 0f;
            if (!Mathf.Approximately(_hoverT, target))
            {
                _hoverT = Mathf.MoveTowards(_hoverT, target, Time.deltaTime / 0.12f);
                Paint();
            }
        }

        /// <summary>进入。</summary>
        public void OnPointerEnter(PointerEventData eventData) { _hover = true; }

        /// <summary>离开。</summary>
        public void OnPointerExit(PointerEventData eventData) { _hover = false; }

        /// <summary>点击。</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_onClick != null) _onClick(_tool);
        }
    }
}
