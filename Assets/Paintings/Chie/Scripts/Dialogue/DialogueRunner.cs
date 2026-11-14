using System;
using UnityEngine;

namespace Numen.Chie
{
    /// <summary>把 JSON 节点推给画内视图，并按硬编码顺序串起来。</summary>
    public class DialogueRunner : MonoBehaviour
    {
        private DialogueData _data;
        private InCanvasDialogueView _view;
        private Action _pendingDone;
        private Action<string> _pendingChoice;
        private bool _busy;

        /// <summary>是否正在播（含等选项）。</summary>
        public bool IsBusy { get { return _busy; } }

        /// <summary>数据引用。</summary>
        public DialogueData Data { get { return _data; } }

        /// <summary>注入数据与视图。</summary>
        public void Init(DialogueData data, InCanvasDialogueView view)
        {
            _data = data;
            _view = view;
        }

        /// <summary>播一段台词，播完回调。</summary>
        public void Lines(DialogueLine[] lines, Action onDone)
        {
            if (_view == null)
            {
                if (onDone != null) onDone();
                return;
            }
            _busy = true;
            _pendingDone = onDone;
            _view.PlayLines(lines, HandleLinesDone);
        }

        private void HandleLinesDone()
        {
            _busy = false;
            Action cb = _pendingDone;
            _pendingDone = null;
            if (cb != null) cb();
        }

        /// <summary>播一个 choice 节点：选项 → 回应 → 回调（参数：选项 id）。</summary>
        public void Choice(string nodeId, Action<string> onPicked)
        {
            DialogueNode n = _data != null ? _data.GetNode(nodeId) : null;
            if (n == null || n.options == null || n.options.Length == 0)
            {
                Debug.LogWarning("[Chie] 对话节点缺失或没有选项：" + nodeId);
                if (onPicked != null) onPicked(string.Empty);
                return;
            }
            _busy = true;
            _choiceNodeId = nodeId;
            _pendingChoice = onPicked;
            _view.PlayChoice(n.options, HandleChoicePicked);
        }

        private void HandleChoicePicked(int index)
        {
            DialogueNode n = null;
            if (_data != null) n = _data.GetNode(CurrentNodeId);
            DialogueOption opt = (n != null && n.options != null && index < n.options.Length) ? n.options[index] : null;

            string id = opt != null ? opt.id : string.Empty;
            DialogueLine[] reply = opt != null ? opt.reply : null;
            Action<string> cb = _pendingChoice;
            _pendingChoice = null;
            _currentChoiceId = id;

            if (cb == null)
            {
                _busy = false;
                return;
            }
            if (reply == null || reply.Length == 0)
            {
                _busy = false;
                cb(id);
                return;
            }
            _view.PlayLines(reply, () =>
            {
                _busy = false;
                cb(_currentChoiceId);
            });
        }

        private string _currentChoiceId;

        // 记录本次 Choice 的节点 id，供回应取值
        private string CurrentNodeId { get { return _choiceNodeId; } }
        private string _choiceNodeId;

        /// <summary>播某节点的台词（lines 节点）。</summary>
        public void NodeLines(string nodeId, Action onDone)
        {
            DialogueNode n = _data != null ? _data.GetNode(nodeId) : null;
            if (n == null || n.lines == null || n.lines.Length == 0)
            {
                if (onDone != null) onDone();
                return;
            }
            Lines(n.lines, onDone);
        }

        /// <summary>播放六处之一的两段台词。</summary>
        public void FlawIntro(FlawId id, Action onDone)
        {
            DialogueFlaw f = _data != null ? _data.GetFlaw(id) : null;
            if (f == null || f.intro == null || f.intro.Length == 0)
            {
                if (onDone != null) onDone();
                return;
            }
            Lines(f.intro, onDone);
        }

        /// <summary>播放某处修好后的台词。</summary>
        public void FlawAfter(FlawId id, Action onDone)
        {
            DialogueFlaw f = _data != null ? _data.GetFlaw(id) : null;
            if (f == null || f.after == null || f.after.Length == 0)
            {
                if (onDone != null) onDone();
                return;
            }
            Lines(f.after, onDone);
        }

        /// <summary>点击推进（由 ChieFlow 统一转发）。</summary>
        public void Advance()
        {
            if (_view != null) _view.Advance();
        }

        /// <summary>立刻清空。</summary>
        public void Clear()
        {
            _busy = false;
            _pendingDone = null;
            _pendingChoice = null;
            if (_view != null) _view.Clear();
        }
    }
}
