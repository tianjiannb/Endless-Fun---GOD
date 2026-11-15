using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Numen.Chie
{
    /// <summary>一行选项：透明命中框收点击，文字子物体只负责显示。没有按钮框。</summary>
    public class ChieOptionLine : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private Image _hitbox;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private CanvasGroup _group;

        private RectTransform _rect;
        private Vector2 _basePos;
        private float _indent;
        private float _hoverTime;
        private float _hoverT;
        private float _whiteT;
        private int _index;
        private Action<int> _onPick;
        private bool _picked;
        private bool _hover;

        private static readonly Color ColNormal = new Color32(0x8B, 0x7D, 0x6D, 0xFF);
        private static readonly Color ColHover = new Color32(0xC9, 0x96, 0x3F, 0xFF);
        private static readonly Color ColPicked = Color.white;

        /// <summary>显示用文字组件（命中不依赖它）。</summary>
        public TextMeshProUGUI Label { get { return _label; } }

        /// <summary>命中框。</summary>
        public Image Hitbox { get { return _hitbox; } }

        /// <summary>建好后注入引用。</summary>
        public void Bind(Image hitbox, TextMeshProUGUI label, CanvasGroup group, RectTransform rect,
            int index, float indent, float hoverTime, Action<int> onPick)
        {
            _hitbox = hitbox;
            _label = label;
            _group = group;
            _rect = rect;
            _index = index;
            _indent = indent;
            _hoverTime = Mathf.Max(0.01f, hoverTime);
            _onPick = onPick;
            if (_rect != null) _basePos = _rect.anchoredPosition;

            if (_hitbox != null)
            {
                _hitbox.raycastTarget = true;
                Color c = _hitbox.color;
                c.a = 0f;
                _hitbox.color = c;
            }
            if (_label != null) _label.raycastTarget = false;
            SetColor(ColNormal);
        }

        /// <summary>设置文本与初始位置。</summary>
        public void SetText(string text)
        {
            StopAllCoroutines();
            if (_label != null) _label.text = text;
            _picked = false;
            _hover = false;
            _hoverT = 0f;
            _whiteT = 0f;
            if (_rect != null) _rect.anchoredPosition = _basePos;
            SetColor(ColNormal);
            if (_hitbox != null) _hitbox.enabled = true;
            if (_group != null)
            {
                _group.alpha = 1f;
                _group.blocksRaycasts = true;
                _group.interactable = true;
            }
        }

        private void SetColor(Color c)
        {
            if (_label != null) _label.color = c;
        }

        private void Update()
        {
            if (_rect == null) return;

            float target = (_hover && !_picked) ? 1f : 0f;
            _hoverT = Mathf.MoveTowards(_hoverT, target, Time.deltaTime / _hoverTime);
            _rect.anchoredPosition = _basePos + new Vector2(_indent * _hoverT, 0f);
            SetColor(Color.Lerp(ColNormal, ColHover, _hoverT));

            if (_picked)
            {
                _whiteT = Mathf.MoveTowards(_whiteT, 1f, Time.deltaTime / 0.15f);
                SetColor(Color.Lerp(ColHover, ColPicked, _whiteT));
            }
        }

        /// <summary>鼠标进入。</summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!_picked) _hover = true;
        }

        /// <summary>鼠标离开。</summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            _hover = false;
        }

        /// <summary>点击：本行变白，其余行淡出，然后回调。</summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_picked || _onPick == null) return;
            _picked = true;
            _hover = false;
            if (_group != null) _group.blocksRaycasts = false;
            if (_hitbox != null) _hitbox.enabled = false;
            if (_onPick != null) _onPick(_index);
        }

        /// <summary>立刻收干净：清字、断射线、隐藏。</summary>
        public void HideNow()
        {
            StopAllCoroutines();
            _picked = true;
            _hover = false;
            _hoverT = 0f;
            _whiteT = 0f;
            if (_label != null) _label.text = "";
            if (_rect != null) _rect.anchoredPosition = _basePos;
            if (_hitbox != null) _hitbox.enabled = false;
            if (_group != null)
            {
                _group.alpha = 0f;
                _group.blocksRaycasts = false;
                _group.interactable = false;
            }
            gameObject.SetActive(false);
        }

        /// <summary>淡出（未被选中的行）。</summary>
        public void FadeAway(float dur)
        {
            if (_group == null) return;
            _group.blocksRaycasts = false;
            if (_hitbox != null) _hitbox.enabled = false;
            StartCoroutine(CoFade(dur));
        }

        private System.Collections.IEnumerator CoFade(float dur)
        {
            float t = 0f;
            float d = Mathf.Max(0.01f, dur);
            float from = _group != null ? _group.alpha : 1f;
            while (t < d)
            {
                t += Time.deltaTime;
                if (_group != null) _group.alpha = Mathf.Lerp(from, 0f, t / d);
                yield return null;
            }
            if (_group != null) _group.alpha = 0f;
        }
    }
}
