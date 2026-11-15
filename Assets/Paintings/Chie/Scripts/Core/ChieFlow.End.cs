using System.Collections;
using Numen.Core;
using UnityEngine;

namespace Numen.Chie
{
    public partial class ChieFlow
    {
        private string _pickedEnding;
        private ChieExtractView _extract;
        private ChieSfx _sfx;

        /// <summary>装配期注入「抽离」过场（可空）。</summary>
        public void AttachExtract(ChieExtractView view) { _extract = view; }

        /// <summary>装配期注入音频总线（可空）。</summary>
        public void AttachSfx(ChieSfx sfx) { _sfx = sfx; }

        /// <summary>站起 → 她说话 → 逐处回看 → 静 0.4s。</summary>
        private IEnumerator CoHold()
        {
            State = FlowState.Hold;
            _busy = true;
            SetPaint(false);
            if (_toolbar != null) _toolbar.SetVisible(false);
            if (_markers != null) _markers.SetVisible(false);

            yield return CoSwitchPose(PoseId.Stand, _config != null ? _config.PoseBlendStand : 0.4f);

            int restored = _judge != null ? _judge.RestoredCount : 0;
            int all = _config != null ? _config.AllOpenAt : 6;
            yield return CoLines(restored >= all ? Data.standAll : Data.standPart);

            if (_reveal != null)
            {
                bool done = false;
                _reveal.Play(() => done = true);
                while (!done && _alive) yield return null;
            }

            yield return Wait(_config != null ? _config.HoldSilence : 0.4f);
            _busy = false;
        }

        /// <summary>结局三选项按修复数开放；选完走回应与终幕。</summary>
        private IEnumerator CoEnding()
        {
            State = FlowState.Ending;
            _busy = true;
            _pickedEnding = null;

            if (_ending != null)
            {
                _ending.Show(_judge != null ? _judge.RestoredCount : 0,
                    id => _pickedEnding = id);
                while (string.IsNullOrEmpty(_pickedEnding) && _alive) yield return null;
            }
            else
            {
                _pickedEnding = "come_again";
            }

            State = FlowState.Finale;
            yield return CoReply("ask_end", _pickedEnding);
            yield return CoLines(Data != null ? Data.finale : null);

            // 终幕台词落定，先响一声碎裂，再让她那一块整片剥离
            if (_sfx != null) _sfx.Break();

            if (_extract != null)
            {
                bool fxDone = false;
                PoseArt art = _config != null ? _config.GetPose(PoseId.Sit) : null;
                Texture src = art != null ? art.baseTex : null;
                _extract.Play(src, () => fxDone = true);
                while (!fxDone && _alive) yield return null;
            }

            float f = _config != null ? _config.FinaleFade : 0.4f;
            if (_ending != null) _ending.FadeOut(f);
            if (_dlg != null) _dlg.FadeOut(f);
            yield return Wait(f);
            _busy = false;
        }

        /// <summary>回传画廊结果 → 黑场 → 换场景；没有画廊就留在这里。</summary>
        private IEnumerator CoExit()
        {
            State = FlowState.Exit;
            if (_sfx != null) _sfx.StopBgm();
            int restored = _judge != null ? _judge.RestoredCount : 0;

            float dur = _config != null ? _config.ExitFade : 0.6f;
            if (_fade != null)
            {
                float t = 0f;
                while (t < dur && _alive)
                {
                    t += Time.deltaTime;
                    _fade.SetAlpha(t / dur);
                    yield return null;
                }
                _fade.SetAlpha(1f);
            }

            // 出口交给画廊实现；没有实现时由 GalleryBridge 打印缺场景告警并留在黑场
            GalleryBridge.Exit(_config != null ? _config.PaintingId : "chie", _pickedEnding, "restored" + restored);
        }
    }
}
