using System.Collections;
using UnityEngine;

namespace Numen.Chie
{
    public partial class ChieFlow
    {
        private IEnumerator CoEdit()
        {
            State = FlowState.Edit;
            _busy = false;
            _holdRequested = false;
            _playedWave = _wave != null ? _wave.Wave : 1;

            yield return CoWaveIntros(_playedWave);
            EnterEditInput();

            while (!_holdRequested && _alive)
            {
                if (_wave != null && _wave.Wave > _playedWave)
                {
                    int next = _playedWave + 1;
                    yield return CoWave(next);
                    _playedWave = next;
                }
                yield return null;
            }

            SetPaint(false);
            if (_toolbar != null) _toolbar.SetVisible(false);
            if (_markers != null) _markers.SetVisible(false);
        }

        /// <summary>一波推进：她动 → 换姿态 → 新露出两处开口 → 第三波追加一问。</summary>
        private IEnumerator CoWave(int waveIndex)
        {
            _busy = true;
            SetPaint(false);
            if (_toolbar != null) _toolbar.SetVisible(false);

            yield return CoNode(waveIndex <= 2 ? "wave_2_move" : "wave_3_move");
            yield return CoSwitchPose(waveIndex <= 2 ? PoseId.Shift : PoseId.Up,
                _config != null ? _config.PoseBlendShort : 0.25f);

            if (_toolbar != null) _toolbar.Refresh(_wave);
            if (_gate != null) _gate.Reset();
            if (_painter != null) _painter.Tool = ToolId.Brush;
            if (_toolbar != null) _toolbar.SetTool(ToolId.Brush);

            yield return CoWaveIntros(waveIndex);

            if (waveIndex >= 3) yield return CoChoice("ask_3");

            _busy = false;
            if (_holdRequested) yield break;
            State = FlowState.Edit;
            EnterEditInput();
        }

        /// <summary>新露出来的两处，各说一句"这里不对"。</summary>
        private IEnumerator CoWaveIntros(int waveIndex)
        {
            FlawId[] ids = FlawsOfWave(waveIndex);
            for (int i = 0; i < ids.Length && _alive; i++)
            {
                if (_judge != null && _judge.IsRestored(ids[i])) continue;
                _busy = true;
                _waitFlag = false;
                _runner.FlawIntro(ids[i], () => _waitFlag = true);
                while (!_waitFlag && _alive) yield return null;
            }
            _busy = false;
        }

        private static FlawId[] FlawsOfWave(int waveIndex)
        {
            if (waveIndex <= 1) return Wave1;
            if (waveIndex == 2) return Wave2;
            return Wave3;
        }

        private static readonly FlawId[] Wave1 = { FlawId.Tip, FlawId.Face };
        private static readonly FlawId[] Wave2 = { FlawId.Dust, FlawId.Window };
        private static readonly FlawId[] Wave3 = { FlawId.Mouth, FlawId.Shadow };

        private void EnterEditInput()
        {
            SetPaint(true);
            if (_toolbar != null)
            {
                _toolbar.SetVisible(true);
                _toolbar.Refresh(_wave);
                _toolbar.SetHint("在虚线圈住的地方改。改够了就松手。");
            }
            if (_markers != null) _markers.SetVisible(true);
        }

        // ---------- 事件 ----------

        private void HandleRestored(FlawId id)
        {
            if (_view != null) _view.PushRestored();
            if (_gate != null) _gate.Reset();
            if (_toolbar != null) _toolbar.SetHint("改好了：" + FlawInfo.NameOf(id) + "。");
            if (_wave != null) _wave.Evaluate(_judge != null ? _judge.RestoredCount : 1);
            if (_toolbar != null) _toolbar.Refresh(_wave);
            if (_judge != null && _config != null && _judge.RestoredCount >= _config.AllOpenAt) _holdRequested = true;
        }

        private void HandleToolPicked(ToolId tool)
        {
            if (_painter != null) _painter.Tool = tool;
            if (_toolbar == null) return;
            switch (tool)
            {
                case ToolId.Eyedropper:
                    _toolbar.SetHint("在她身上点一下，取到颜色再回来。");
                    break;
                case ToolId.Eraser:
                    _toolbar.SetHint("擦掉该擦的地方。");
                    break;
                default:
                    _toolbar.SetHint("在虚线圈住的地方改。改够了就松手。");
                    break;
            }
        }

        private void HandlePutDown()
        {
            if (State == FlowState.Edit) _holdRequested = true;
        }

        private void HandleSegment(Vector2 fromPx, Vector2 toPx, float radiusPx)
        {
            if (State != FlowState.Edit || _busy) return;
            if (_judge == null) return;
            _judge.MarkCoverage(fromPx, toPx, radiusPx, _allowPred);
        }

        private bool AllowFlaw(FlawId id)
        {
            if (_gate == null || _painter == null) return false;
            return _gate.Allow(id, _painter.Tool);
        }
    }
}
