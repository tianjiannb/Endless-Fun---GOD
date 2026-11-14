using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace Numen.Chie
{
    /// <summary>画在画面里的对话：World Space、无面板、打字机、三行选项。</summary>
    public class InCanvasDialogueView : MonoBehaviour
    {
        private ChieConfig _config;
        private DialogueCanvasParts _parts;

        private DialogueLine[] _queue;
        private int _cursor;
        private Action _onQueueDone;

        private string _full;
        private float _typed;
        private bool _typing;

        private DialogueOption[] _pendingOptions;
        private Action<int> _onPick;

        /// <summary>是否正在播放台词。</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>是否正在等选。</summary>
        public bool HasChoices { get; private set; }

        /// <summary>是否有任何对话内容在显示。</summary>
        public bool Visible { get { return IsPlaying || HasChoices; } }

        /// <summary>建出全部节点（Boot 阶段）。</summary>
        public void Build(ChieConfig config, TMP_FontAsset font, Camera cam)
        {
            _config = config;
            _parts = DialogueCanvasBuilder.Build(config, font, cam, transform, OnOptionClicked);
            HideAll();
        }

        private CanvasGroup Group { get { return _parts != null ? _parts.group : null; } }

        private void HideAll()
        {
            if (_parts == null) return;
            if (_parts.speaker != null) _parts.speaker.text = "";
            if (_parts.body != null) _parts.body.text = "";
            if (_parts.dim != null) _parts.dim.enabled = false;
            if (_parts.options == null) return;
            for (int i = 0; i < _parts.options.Length; i++)
            {
                if (_parts.options[i] != null) _parts.options[i].gameObject.SetActive(false);
            }
        }

        /// <summary>播放一串台词，播完回调。</summary>
        public void PlayLines(DialogueLine[] lines, Action onDone)
        {
            if (lines == null || lines.Length == 0)
            {
                if (onDone != null) onDone();
                return;
            }
            HasChoices = false;
            _queue = lines;
            _cursor = 0;
            _onQueueDone = onDone;
            IsPlaying = true;
            SetAlpha(1f);
            ShowLine(_queue[0]);
        }

        private void ShowLine(DialogueLine line)
        {
            if (line == null) line = new DialogueLine { who = "sys", t = "" };
            if (_parts == null) return;
            ChieDialogueStyle.Style st = ChieDialogueStyle.Get(line.who);

            if (_parts.speaker != null)
            {
                _parts.speaker.text = st.name;
                _parts.speaker.color = st.nameColor;
            }
            if (_parts.body != null)
            {
                _parts.body.color = st.bodyColor;
                _parts.body.fontSize = st.size > 0f ? st.size : DialogueCanvasBuilder.BodyFontSize;
                ChieDialogueStyle.ApplyItalic(_parts.body, st.italic);
            }
            if (_parts.dim != null) _parts.dim.enabled = true;

            _full = line.t != null ? line.t : "";
            _typed = 0f;
            _typing = true;
            if (_parts.body != null)
            {
                _parts.body.text = _full;
                _parts.body.maxVisibleCharacters = 0;
            }
        }

        private void Update()
        {
            if (!_typing || _parts == null || _parts.body == null) return;
            _typed += Time.deltaTime;
            int shown = Mathf.FloorToInt(_typed / Mathf.Max(0.001f, _config.DialogueCharSeconds));
            _parts.body.maxVisibleCharacters = shown;
            if (shown >= _full.Length)
            {
                _parts.body.maxVisibleCharacters = 9999;
                _typing = false;
            }
        }

        /// <summary>整句立刻显示完。</summary>
        public void CompleteTyping()
        {
            if (!_typing || _parts == null || _parts.body == null) return;
            _typing = false;
            _typed = _full.Length * _config.DialogueCharSeconds;
            _parts.body.maxVisibleCharacters = 9999;
        }

        /// <summary>点击推进：补全 → 下一句 → 播完回调。</summary>
        public void Advance()
        {
            if (HasChoices || !IsPlaying || _parts == null) return;

            if (_typing)
            {
                CompleteTyping();
                return;
            }

            _cursor++;
            if (_queue == null || _cursor >= _queue.Length)
            {
                IsPlaying = false;
                Action cb = _onQueueDone;
                _onQueueDone = null;
                if (cb != null) cb();
                return;
            }
            ShowLine(_queue[_cursor]);
        }

        /// <summary>显示三行选项，选中回调索引。</summary>
        public void PlayChoice(DialogueOption[] options, Action<int> onPick)
        {
            if (_parts == null) return;
            _pendingOptions = options;
            _onPick = onPick;
            HasChoices = true;
            IsPlaying = false;
            _typing = false;
            SetAlpha(1f);
            if (_parts.body != null) _parts.body.text = "";
            if (_parts.speaker != null) _parts.speaker.text = "";
            if (_parts.dim != null) _parts.dim.enabled = true;

            for (int i = 0; i < _parts.options.Length; i++)
            {
                bool on = options != null && i < options.Length && options[i] != null;
                _parts.options[i].gameObject.SetActive(on);
                if (on)
                {
                    _parts.options[i].gameObject.SetActive(true);
                    _parts.options[i].SetText(options[i].text);
                }
            }
        }

        private void OnOptionClicked(int index)
        {
            if (!HasChoices) return;
            if (_pendingOptions == null || index >= _pendingOptions.Length) return;
            HasChoices = false;

            // 选中即收：选项不再停留，也不留任何可点区域
            for (int i = 0; i < _parts.options.Length; i++)
            {
                if (_parts.options[i] != null) _parts.options[i].HideNow();
            }
            _pendingOptions = null;

            Action<int> cb = _onPick;
            _onPick = null;
            if (cb != null) cb(index);
        }

        /// <summary>整体透明度 0..1（终幕字先走）。</summary>
        public void SetAlpha(float a)
        {
            CanvasGroup g = Group;
            if (g != null) g.alpha = Mathf.Clamp01(a);
        }

        /// <summary>淡出后清空，协程会被 OnDisable 停掉。</summary>
        public void FadeOut(float dur)
        {
            StopAllCoroutines();
            StartCoroutine(CoFadeOut(dur));
        }

        private IEnumerator CoFadeOut(float dur)
        {
            CanvasGroup g = Group;
            float t = 0f;
            float d = Mathf.Max(0.01f, dur);
            float from = g != null ? g.alpha : 1f;
            while (t < d)
            {
                t += Time.deltaTime;
                if (g != null) g.alpha = Mathf.Lerp(from, 0f, t / d);
                yield return null;
            }
            if (g != null) g.alpha = 0f;
            HideAll();
            IsPlaying = false;
            HasChoices = false;
        }

        /// <summary>立刻清空并隐藏。</summary>
        public void Clear()
        {
            StopAllCoroutines();
            _queue = null;
            _onQueueDone = null;
            _onPick = null;
            _pendingOptions = null;
            IsPlaying = false;
            HasChoices = false;
            _typing = false;
            HideAll();
            SetAlpha(1f);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
        }
    }
}
