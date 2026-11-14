using Numen.Chie.UI;
using UnityEngine;

namespace Numen.Chie
{
    /// <summary>QA 专用内部访问器：不动流程，只把依赖与真实入口露出来。</summary>
    public partial class ChieFlow
    {
        /// <summary>配置。</summary>
        internal ChieConfig QaConfig { get { return _config; } }
        /// <summary>画面。</summary>
        internal PaintingView QaView { get { return _view; } }
        /// <summary>笔迹绘制。</summary>
        internal BrushPainter QaPainter { get { return _painter; } }
        /// <summary>指针。</summary>
        internal CanvasPointer QaPointer { get { return _pointer; } }
        /// <summary>判定。</summary>
        internal FlawJudge QaJudge { get { return _judge; } }
        /// <summary>波次。</summary>
        internal WaveUnlock QaWave { get { return _wave; } }
        /// <summary>对话运行器。</summary>
        internal DialogueRunner QaRunner { get { return _runner; } }
        /// <summary>对话视图。</summary>
        internal InCanvasDialogueView QaDialogue { get { return _dlg; } }
        /// <summary>工具栏。</summary>
        internal ToolBarView QaToolbar { get { return _toolbar; } }
        /// <summary>结局。</summary>
        internal EndingView QaEnding { get { return _ending; } }
        /// <summary>回看。</summary>
        internal RevealPlayer QaReveal { get { return _reveal; } }
        /// <summary>黑场。</summary>
        internal ChieFade QaFade { get { return _fade; } }
        /// <summary>笔迹闸门。</summary>
        internal StrokeGate QaGate { get { return _gate; } }
        /// <summary>是否已请求放下笔。</summary>
        internal bool QaHoldRequested { get { return _holdRequested; } }

        /// <summary>依赖是否齐了。</summary>
        internal bool QaReady
        {
            get
            {
                return _gate != null && _judge != null && _runner != null && _runner.Data != null
                    && _dlg != null && _painter != null && _config != null && _toolbar != null;
            }
        }

        /// <summary>真实收笔入口（内部会 Evaluate）。</summary>
        internal void QaEndStroke(Vector2 px)
        {
            EndStroke(px);
        }

        /// <summary>只取色不刷 UI（扫描候选点时用，避免每点都改提示文本）。</summary>
        internal bool QaProbeDip(Vector2 px)
        {
            return _gate != null && _gate.Dip(px);
        }

        /// <summary>真实吸管动作：取色 → 更新颜色有效标记 → 更新提示。</summary>
        internal bool QaDipAt(Vector2 px)
        {
            if (_gate == null) return false;
            bool ok = _gate.Dip(px);
            if (_painter != null) _painter.ColorValid = ok;
            if (_toolbar != null)
            {
                _toolbar.SetHint(ok ? "颜色对了，换回笔去改。" : "这个颜色不对，再取一次。");
            }
            return ok;
        }

        /// <summary>真实工具切换入口（工具栏点击的同一处理）。</summary>
        internal void QaPickTool(ToolId tool)
        {
            if (_toolbar != null)
            {
                _toolbar.SetTool(tool);
                _toolbar.Refresh(_wave);
            }
            HandleToolPicked(tool);
        }

        /// <summary>真实放下笔入口（工具栏按钮的同一处理）。</summary>
        internal void QaPutDown()
        {
            HandlePutDown();
        }

        /// <summary>改一次姿态（QA 兜底用；正常流程由 CoWave 自己切）。</summary>
        internal void QaSwitchPose(PoseId pose, float dur)
        {
            if (_view != null) _view.SwitchPose(pose, dur);
            if (_judge != null) _judge.SwitchPose(pose);
        }
    }
}
