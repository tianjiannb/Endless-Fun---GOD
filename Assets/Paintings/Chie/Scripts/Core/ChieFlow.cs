using System;
using System.Collections;
using Numen.Chie.UI;
using Numen.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Numen.Chie
{
    /// <summary>流程总控：开场 → 两问 → 修画三波 → 放下笔/六处 → 站起 → 揭示 → 结局 → 退出。</summary>
    public partial class ChieFlow : MonoBehaviour
    {
        private ChieConfig _config;
        private PaintingView _view;
        private BrushPainter _painter;
        private CanvasPointer _pointer;
        private FlawJudge _judge;
        private WaveUnlock _wave;
        private DialogueRunner _runner;
        private InCanvasDialogueView _dlg;
        private ToolBarView _toolbar;
        private EndingView _ending;
        private RevealPlayer _reveal;
        private ChieMarkerOverlay _markers;
        private ChieFade _fade;
        private StrokeGate _gate;

        private bool _alive = true;
        private bool _paintAllowed;
        private bool _holdRequested;
        private bool _busy;
        private bool _waitFlag;
        private string _choiceId;
        private int _playedWave = 1;
        private Predicate<FlawId> _allowPred;

        /// <summary>当前流程状态。</summary>
        public FlowState State { get; private set; }

        /// <summary>对话或演出是否正在占用输入。</summary>
        public bool Busy { get { return _busy; } }

        /// <summary>涂抹是否被允许。</summary>
        public bool PaintAllowed { get { return _paintAllowed; } }

        /// <summary>注入全部依赖并起流程。</summary>
        public void Bind(ChieConfig config, PaintingView view, BrushPainter painter, CanvasPointer pointer,
            FlawJudge judge, WaveUnlock wave, DialogueRunner runner, InCanvasDialogueView dialogueView,
            ToolBarView toolbar, EndingView ending, RevealPlayer reveal, ChieMarkerOverlay markers, ChieFade fade)
        {
            _config = config;
            _view = view;
            _painter = painter;
            _pointer = pointer;
            _judge = judge;
            _wave = wave;
            _runner = runner;
            _dlg = dialogueView;
            _toolbar = toolbar;
            _ending = ending;
            _reveal = reveal;
            _markers = markers;
            _fade = fade;

            _gate = new StrokeGate(config, judge, wave);
            _allowPred = AllowFlaw;

            if (_judge != null) _judge.OnFlawRestored += HandleRestored;
            if (_toolbar != null)
            {
                _toolbar.OnToolPicked += HandleToolPicked;
                _toolbar.OnPutDown += HandlePutDown;
            }
            if (_painter != null) _painter.OnStrokeSegment += HandleSegment;

            State = FlowState.Boot;
            StartCoroutine(CoMain());
        }

        private void OnDisable()
        {
            _alive = false;
            StopAllCoroutines();
            if (_judge != null) _judge.OnFlawRestored -= HandleRestored;
            if (_toolbar != null)
            {
                _toolbar.OnToolPicked -= HandleToolPicked;
                _toolbar.OnPutDown -= HandlePutDown;
            }
            if (_painter != null) _painter.OnStrokeSegment -= HandleSegment;
        }

        // ---------- 主脚本 ----------

        private IEnumerator CoMain()
        {
            yield return null;                       // 等一帧，让相机与画布就位
            State = FlowState.Opening;
            SetPaint(false);
            if (_markers != null) _markers.SetVisible(false);
            if (_toolbar != null) _toolbar.SetVisible(false);
            GalleryBridge.Enter(_config != null ? _config.PaintingId : "chie");

            yield return Wait(0.4f);
            yield return CoLines(Data != null ? Data.opening : null);
            yield return CoChoice("ask_1");
            yield return CoNode("after_ask_1");
            yield return CoChoice("ask_2");
            yield return CoNode("after_ask_2");

            yield return CoEdit();
            yield return CoHold();
            yield return CoEnding();
            yield return CoExit();
        }

        private DialogueData Data { get { return _runner != null ? _runner.Data : null; } }

        // ---------- 对话管线 ----------

        private IEnumerator CoLines(DialogueLine[] lines)
        {
            if (lines == null || lines.Length == 0) yield break;
            _busy = true;
            _waitFlag = false;
            _runner.Lines(lines, () => _waitFlag = true);
            while (!_waitFlag && _alive) yield return null;
            _busy = false;
        }

        private IEnumerator CoChoice(string nodeId)
        {
            _busy = true;
            _waitFlag = false;
            _choiceId = null;
            _runner.Choice(nodeId, id => { _choiceId = id; _waitFlag = true; });
            while (!_waitFlag && _alive) yield return null;
            _busy = false;
        }

        private IEnumerator CoNode(string nodeId)
        {
            DialogueNode n = Data != null ? Data.GetNode(nodeId) : null;
            if (n == null) yield break;
            if (n.kind == "choice") yield return CoChoice(nodeId);
            else yield return CoLines(n.lines);
        }

        private IEnumerator CoReply(string nodeId, string choiceId)
        {
            DialogueNode n = Data != null ? Data.GetNode(nodeId) : null;
            if (n == null || n.options == null) yield break;
            for (int i = 0; i < n.options.Length; i++)
            {
                if (n.options[i] == null || n.options[i].id != choiceId) continue;
                yield return CoLines(n.options[i].reply);
                yield break;
            }
        }

        // ---------- 小工具 ----------

        private IEnumerator CoSwitchPose(PoseId next, float dur)
        {
            if (_view == null) yield break;
            _view.SwitchPose(next, dur);
            float t = 0f;
            float d = Mathf.Max(0.01f, dur);
            while (t < d + 0.05f && _alive)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        private static IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>允许涂抹的意图；实际是否落笔由 Update 折算。</summary>
        private void SetPaint(bool on)
        {
            _paintAllowed = on;
            if (!on && _pointer != null) _pointer.CanPaint = false;
        }
    }
}
