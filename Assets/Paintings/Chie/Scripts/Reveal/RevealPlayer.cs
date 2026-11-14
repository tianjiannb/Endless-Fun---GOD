using System.Collections;
using UnityEngine;

namespace Numen.Chie
{
    /// <summary>逐处回放：UV 推近 3 倍 → 停 0.4s → 一句台词 → 退回。</summary>
    public class RevealPlayer : MonoBehaviour
    {
        private ChieConfig _config;
        private PaintingView _view;
        private FlawJudge _judge;
        private DialogueRunner _dialogue;
        private bool _alive = true;

        // ②④⑤③①⑥ 的顺序
        private static readonly FlawId[] Order =
        {
            FlawId.Tip, FlawId.Face, FlawId.Dust, FlawId.Window, FlawId.Mouth, FlawId.Shadow
        };

        /// <summary>注入依赖。</summary>
        public void Init(ChieConfig config, PaintingView view, FlawJudge judge, DialogueRunner dialogue)
        {
            _config = config;
            _view = view;
            _judge = judge;
            _dialogue = dialogue;
        }

        private void OnDisable()
        {
            _alive = false;
            StopAllCoroutines();
        }

        /// <summary>播完整段回放，结束回调。</summary>
        public void Play(System.Action onDone)
        {
            _alive = true;
            StartCoroutine(CoPlay(onDone));
        }

        private IEnumerator CoPlay(System.Action onDone)
        {
            for (int i = 0; i < Order.Length && _alive; i++)
            {
                FlawId id = Order[i];
                if (_judge == null || !_judge.IsRestored(id)) continue;

                Vector2 c = ZoneCenter(id);
                _view.SetZoom(c, 3f);
                yield return WaitSeconds(0.4f);
                if (!_alive) yield break;

                bool done = false;
                _dialogue.FlawAfter(id, () => done = true);
                while (!done && _alive) yield return null;
                if (!_alive) yield break;

                _view.SetZoom(new Vector2(0.5f, 0.5f), 1f);
                yield return WaitSeconds(0.3f);
            }

            if (_view != null) _view.SetZoom(new Vector2(0.5f, 0.5f), 1f);
            if (_alive && onDone != null) onDone();
        }

        private static IEnumerator WaitSeconds(float d)
        {
            float t = 0f;
            while (t < d)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        private Vector2 ZoneCenter(FlawId id)
        {
            Vector4 r;
            PoseId pose = _judge != null ? _judge.Pose : PoseId.Stand;
            if (_config != null && _config.TryGetZone(id, pose, out r))
            {
                return new Vector2(r.x + r.z * 0.5f, r.y + r.w * 0.5f);
            }
            return new Vector2(0.5f, 0.5f);
        }
    }
}
