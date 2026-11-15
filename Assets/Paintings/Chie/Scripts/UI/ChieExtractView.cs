using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Numen.Chie
{
    /// <summary>
    /// 结局过场：程序化「抽离」。画布先从中间裂开露出一片海，
    /// 然后她那一块像颜料膜一样被剥离、上浮、溶解，最后归于黑。
    /// 不使用任何视频资源（原 turn.mp4 通道已废弃）。
    /// </summary>
    public class ChieExtractView : MonoBehaviour
    {
        // 四段时间轴（秒）
        private const float TRip = 0.8f;
        private const float TSplit = 1.2f;
        private const float TLift = 0.8f;
        private const float TBlack = 0.4f;

        private CanvasGroup _group;
        private Material _mat;
        private bool _playing;

        /// <summary>本过场恒可用（纯程序化，无外部依赖）。</summary>
        public bool HasEffect { get { return _mat != null; } }

        /// <summary>建出全屏特效层。</summary>
        public static ChieExtractView Build(ChieConfig config, Camera cam, Transform parent)
        {
            var go = new GameObject("ExtractStage");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<ChieExtractView>();
            v.Init(config, cam);
            return v;
        }

        private void Init(ChieConfig config, Camera cam)
        {
            Shader sh = Shader.Find("Numen/ChieExtract");
            if (sh == null)
            {
                Debug.LogError("[Chie] 找不到 shader：Numen/ChieExtract");
                return;
            }
            _mat = new Material(sh);
            _mat.SetFloat("_Split", 0f);
            _mat.SetFloat("_Lift", 0f);
            _mat.SetFloat("_Dissolve", 0f);
            _mat.SetFloat("_Rip", 0f);

            PoseArt sit = config != null ? config.GetPose(PoseId.Sit) : null;
            if (sit != null && sit.baseTex != null) _mat.SetTexture("_MainTex", sit.baseTex);

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasGroup), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvasGo.layer = 5;
            var crt = (RectTransform)canvasGo.transform;
            crt.sizeDelta = new Vector2(1920f, 1080f);
            crt.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            crt.localPosition = new Vector3(0f, 0f, -1f);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            canvas.sortingOrder = 35;

            _group = canvasGo.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var imgGo = new GameObject("Screen", typeof(RectTransform), typeof(Image));
            imgGo.transform.SetParent(canvasGo.transform, false);
            var irt = (RectTransform)imgGo.transform;
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = Vector2.zero;
            irt.offsetMax = Vector2.zero;

            var img = imgGo.GetComponent<Image>();
            img.raycastTarget = false;
            img.material = _mat;
            img.color = Color.white;
        }

        /// <summary>换一张要抽离的画面（默认用坐姿底图）。</summary>
        public void SetSource(Texture tex)
        {
            if (_mat != null && tex != null) _mat.SetTexture("_MainTex", tex);
        }

        /// <summary>按当前姿势的画面播一遍。</summary>
        public void Play(Texture tex, Action onDone)
        {
            SetSource(tex);
            Play(onDone);
        }

        /// <summary>播一遍：裂 → 剥 → 浮 → 黑，然后回调。</summary>
        public void Play(Action onDone)
        {
            if (_mat == null || _playing)
            {
                if (onDone != null) onDone();
                return;
            }
            StartCoroutine(CoPlay(onDone));
        }

        private IEnumerator CoPlay(Action onDone)
        {
            _playing = true;
            _group.alpha = 1f;
            _mat.SetFloat("_Rip", 0f);
            _mat.SetFloat("_Split", 0f);
            _mat.SetFloat("_Lift", 0f);
            _mat.SetFloat("_Dissolve", 0f);

            yield return CoRamp("_Rip", 0f, 1f, TRip);
            yield return CoRamp("_Split", 0f, 1f, TSplit);
            yield return CoRamp("_Lift", 0f, 1f, TLift);
            yield return CoRamp("_Dissolve", 0f, 1f, TBlack);

            _group.alpha = 0f;
            _playing = false;
            if (onDone != null) onDone();
        }

        private IEnumerator CoRamp(string prop, float from, float to, float dur)
        {
            float t = 0f;
            float d = Mathf.Max(0.01f, dur);
            while (t < d)
            {
                t += Time.deltaTime;
                _mat.SetFloat(prop, Mathf.Lerp(from, to, Mathf.Clamp01(t / d)));
                yield return null;
            }
            _mat.SetFloat(prop, to);
        }

        /// <summary>中途收起。</summary>
        public void Hide()
        {
            StopAllCoroutines();
            _playing = false;
            if (_group != null) _group.alpha = 0f;
            if (_mat != null)
            {
                _mat.SetFloat("_Rip", 0f);
                _mat.SetFloat("_Split", 0f);
                _mat.SetFloat("_Lift", 0f);
                _mat.SetFloat("_Dissolve", 0f);
            }
        }

        private void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }
    }
}
