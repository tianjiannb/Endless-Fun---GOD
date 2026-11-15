using System.Collections;
using UnityEngine;

namespace Numen.Chie
{
    /// <summary>底图 / 修复态切换、姿态交叉淡入、UV 放大。</summary>
    public class PaintingView : MonoBehaviour
    {
        private ChieConfig _config;
        private Material _mat;
        private FlawJudge _judge;
        private SpriteRenderer _sr;
        private bool _alive = true;

        // 画室底噪强度。ChieConfig 暂无该字段（另一路在改 asset），先在本类写死；
        // 与 PaintingCanvas.shader 里 _Grain 的 Properties 默认值保持一致。
        private const float GrainAmount = 0.06f;

        private PoseId _currentPose = PoseId.Sit;

        /// <summary>当前姿态。</summary>
        public PoseId CurrentPose { get { return _currentPose; } }

        /// <summary>姿态淡入是否结束。</summary>
        public bool IsBlending { get; private set; }

        /// <summary>注入依赖（Boot 阶段）。</summary>
        public void Init(ChieConfig config, Material mat, FlawJudge judge)
        {
            _config = config;
            _mat = mat;
            _judge = judge;
            _sr = GetComponent<SpriteRenderer>();
            _alive = true;

            PoseArt sit = config.GetPose(PoseId.Sit);
            if (sit == null || sit.baseTex == null)
            {
                Debug.LogError("[Chie] 缺少 sit 底图，用洋红占位继续跑");
            }
            ApplyPose(PoseId.Sit, true);
            PushRestored();
            PushGrain();
        }

        private void OnDisable()
        {
            _alive = false;
            StopAllCoroutines();
        }

        /// <summary>把底噪强度推进材质（常量，无需每帧刷新）。</summary>
        private void PushGrain()
        {
            if (_mat != null && _mat.HasProperty("_Grain")) _mat.SetFloat("_Grain", GrainAmount);
        }

        /// <summary>把笔迹 RT 接进材质。</summary>
        public void SetBrushRT(RenderTexture rt)
        {
            if (_mat != null) _mat.SetTexture("_BrushTex", rt);
        }

        private Texture2D SafeTex(Texture2D t)
        {
            return t != null ? t : ChieFallbackTex.Magenta;
        }

        private void ApplyPose(PoseId pose, bool immediate)
        {
            PoseArt art = _config != null ? _config.GetPose(pose) : null;
            if (art == null)
            {
                Debug.LogError("[Chie] 缺少姿态图组：" + pose);
                return;
            }
            if (_mat == null) return;

            _mat.SetTexture("_BaseTex", SafeTex(art.baseTex));
            _mat.SetTexture("_FixedTex", SafeTex(art.fixedTex));
            _mat.SetTexture("_MaskTex", art.maskTex != null ? (Texture)art.maskTex : Texture2D.blackTexture);
            if (_mat.HasProperty("_BaseTexB")) _mat.SetTexture("_BaseTexB", SafeTex(art.baseTex));
            if (_mat.HasProperty("_FixedTexB")) _mat.SetTexture("_FixedTexB", SafeTex(art.fixedTex));
            _mat.SetFloat("_Blend", 1f);
            _currentPose = pose;
            if (_judge != null) _judge.SwitchPose(pose);
            if (immediate) IsBlending = false;
        }

        /// <summary>切到新姿态，交叉淡入。第二次调用会先停掉旧的。</summary>
        public void SwitchPose(PoseId next, float dur)
        {
            if (_mat == null || _config == null) return;
            if (next == _currentPose) return;
            StopAllCoroutines();
            StartCoroutine(CoSwitchPose(next, dur));
        }

        private IEnumerator CoSwitchPose(PoseId next, float dur)
        {
            _alive = true;
            IsBlending = true;

            PoseArt cur = _config.GetPose(_currentPose);
            PoseArt nxt = _config.GetPose(next);
            if (nxt == null)
            {
                Debug.LogError("[Chie] 缺少姿态图组：" + next);
                IsBlending = false;
                yield break;
            }

            if (cur != null)
            {
                if (_mat.HasProperty("_BaseTexB")) _mat.SetTexture("_BaseTexB", SafeTex(cur.baseTex));
                if (_mat.HasProperty("_FixedTexB")) _mat.SetTexture("_FixedTexB", SafeTex(cur.fixedTex));
            }
            _mat.SetTexture("_BaseTex", SafeTex(nxt.baseTex));
            _mat.SetTexture("_FixedTex", SafeTex(nxt.fixedTex));
            _mat.SetTexture("_MaskTex", nxt.maskTex != null ? (Texture)nxt.maskTex : Texture2D.blackTexture);
            _mat.SetFloat("_Blend", 0f);

            float t = 0f;
            float d = Mathf.Max(0.01f, dur);
            while (t < d && _alive)
            {
                t += Time.deltaTime;
                _mat.SetFloat("_Blend", Mathf.Clamp01(t / d));
                yield return null;
            }
            _mat.SetFloat("_Blend", 1f);
            _currentPose = next;
            if (_judge != null) _judge.SwitchPose(next);
            IsBlending = false;
        }

        /// <summary>把六处修复状态推进材质（只在变化时调）。</summary>
        public void PushRestored()
        {
            if (_mat == null || _judge == null) return;
            float R(FlawId id) { return _judge.IsRestored(id) ? 1f : 0f; }
            _mat.SetVector("_RestoredA", new Vector4(R(FlawId.Mouth), R(FlawId.Tip), R(FlawId.Window), R(FlawId.Face)));
            _mat.SetVector("_RestoredB", new Vector4(R(FlawId.Dust), R(FlawId.Shadow), 0f, 0f));
        }

        /// <summary>UV 放大到某处（1 = 全景）。</summary>
        public void SetZoom(Vector2 centerUv, float zoom)
        {
            if (_mat == null) return;
            float z = Mathf.Max(1f, zoom);
            var st = new Vector4(1f / z, 1f / z, 0f, 0f);
            st.z = centerUv.x - st.x * 0.5f;
            st.w = centerUv.y - st.y * 0.5f;
            _mat.SetVector("_BaseTex_ST", st);
            // 笔迹 RT 与底图共用同一套 UV 缩放，放大时笔迹要跟着放大
            if (_mat.HasProperty("_BrushTex_ST")) _mat.SetVector("_BrushTex_ST", st);
        }
    }
}
