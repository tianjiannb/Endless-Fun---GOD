using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Numen.Chie.UI
{
    /// <summary>结局三选项：按 restored 数量开放；同一套画内文字样式。</summary>
    public class EndingView : MonoBehaviour
    {
        private ChieConfig _config;
        private CanvasGroup _group;
        private ChieOptionLine[] _lines;
        private TextMeshProUGUI[] _labels;
        private Action<string> _onPick;
        private string[] _ids;

        /// <summary>建出三个结局选项（默认隐藏）。</summary>
        public void Build(ChieConfig config, Camera cam)
        {
            _config = config;

            var canvasGo = new GameObject("EndingCanvas", typeof(RectTransform), typeof(Canvas),
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
            canvas.sortingOrder = 40;

            _group = canvasGo.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            _lines = new ChieOptionLine[3];
            _labels = new TextMeshProUGUI[3];
            _ids = new string[3];

            for (int i = 0; i < 3; i++)
            {
                var go = new GameObject("EndingOption_" + i, typeof(RectTransform),
                    typeof(TextMeshProUGUI), typeof(CanvasGroup), typeof(ChieOptionLine));
                go.transform.SetParent(canvasGo.transform, false);
                var ort = (RectTransform)go.transform;
                ort.anchorMin = Vector2.zero;
                ort.anchorMax = Vector2.zero;
                ort.pivot = Vector2.zero;
                ort.anchoredPosition = new Vector2(154f, 470f - i * 74f);
                ort.sizeDelta = new Vector2(1500f, 62f);

                var t = go.GetComponent<TextMeshProUGUI>();
                if (config != null && config.Font != null) t.font = config.Font;
                t.fontSize = 30f;
                t.enableWordWrapping = false;
                t.alignment = TextAlignmentOptions.Left;
                t.raycastTarget = true;

                _labels[i] = t;
                int idx = i;
                _lines[i] = go.GetComponent<ChieOptionLine>();
                _lines[i].Bind(t, go.GetComponent<CanvasGroup>(), ort, idx,
                    config != null ? config.OptionHoverIndent : 12f,
                    config != null ? config.OptionHoverSeconds : 0.12f, HandlePick);
                go.SetActive(false);
            }
        }

        /// <summary>按 restored 数量显示开放选项，选中回调 choiceId。</summary>
        public void Show(int restored, Action<string> onPick)
        {
            _onPick = onPick;

            List<string> ids = new List<string>(3);
            List<string> texts = new List<string>(3);
            int all = _config != null ? _config.AllOpenAt : 6;
            int part = _config != null ? _config.PartOpenAt : 3;

            if (restored >= all)
            {
                ids.Add("go_together"); texts.Add("一起去。");
                ids.Add("you_go"); texts.Add("你自己去。");
                ids.Add("come_again"); texts.Add("我还会再来。");
            }
            else if (restored >= part)
            {
                ids.Add("you_go"); texts.Add("你自己去。");
                ids.Add("come_again"); texts.Add("我还会再来。");
            }
            else
            {
                ids.Add("come_again"); texts.Add("我还会再来。");
            }

            for (int i = 0; i < 3; i++)
            {
                bool on = i < ids.Count;
                _lines[i].gameObject.SetActive(on);
                if (on)
                {
                    _ids[i] = ids[i];
                    _labels[i].text = texts[i];
                    _lines[i].SetText(texts[i]);
                }
                else _ids[i] = null;
            }

            _group.alpha = 1f;
            _group.blocksRaycasts = true;
        }

        /// <summary>淡入到可见（Hold 之后用）。</summary>
        public void FadeIn(float dur)
        {
            StopAllCoroutines();
            StartCoroutine(CoFade(1f, dur));
        }

        /// <summary>淡出。</summary>
        public void FadeOut(float dur)
        {
            _group.blocksRaycasts = false;
            StopAllCoroutines();
            StartCoroutine(CoFade(0f, dur));
        }

        private System.Collections.IEnumerator CoFade(float target, float dur)
        {
            float from = _group != null ? _group.alpha : 0f;
            float t = 0f;
            float d = Mathf.Max(0.01f, dur);
            while (t < d)
            {
                t += Time.deltaTime;
                if (_group != null) _group.alpha = Mathf.Lerp(from, target, t / d);
                yield return null;
            }
            if (_group != null) _group.alpha = target;
        }

        private void HandlePick(int index)
        {
            if (index < 0 || index >= 3) return;
            string id = _ids[index];
            if (string.IsNullOrEmpty(id)) return;

            for (int i = 0; i < 3; i++)
            {
                if (i != index && _lines[i] != null && _lines[i].gameObject.activeSelf) _lines[i].FadeAway(0.2f);
            }

            Action<string> cb = _onPick;
            _onPick = null;
            if (cb != null) cb(id);
        }

        /// <summary>立刻隐藏。</summary>
        public void Hide()
        {
            StopAllCoroutines();
            if (_group != null)
            {
                _group.alpha = 0f;
                _group.blocksRaycasts = false;
            }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }
    }
}
