using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Numen.Chie
{
    /// <summary>雪夜画通关 MV。只负责播放；结局结果及返回画廊仍由 ChieFlow 处理。</summary>
    [DisallowMultipleComponent]
    public sealed class ChieEndingMovie : MonoBehaviour
    {
        [Header("通关 MV")]
        public VideoClip clip;
        [Min(1f)] public float prepareTimeout = 10f;
        [Min(0f)] public float fadeDuration = 0.25f;

        public bool IsPlaying { get; private set; }
        public bool CompletedSuccessfully { get; private set; }

        readonly List<AudioSource> _pausedAudio = new List<AudioSource>();
        GameObject _overlay;
        CanvasGroup _group;
        CanvasGroup _picture;
        VideoPlayer _video;
        AudioSource _audio;
        RenderTexture _texture;
        bool _firstFrame;
        bool _finished;
        bool _cancelled;
        string _error;

        /// <summary>由原通关协程等待；无素材或播放失败时正常结束，不截断结果回传。</summary>
        public IEnumerator Play()
        {
            if (IsPlaying || _overlay != null || !isActiveAndEnabled) yield break;
            CompletedSuccessfully = false;
            if (clip == null)
            {
                Debug.LogWarning("[雪夜画] 未配置通关 MV，继续原返回画廊流程。", this);
                yield break;
            }

            IsPlaying = true;
            _cancelled = false;
            _firstFrame = false;
            _finished = false;
            _error = null;
            try
            {
                PauseSceneAudio();
                BuildPlayer();
                _video.Prepare();
                float deadline = Time.realtimeSinceStartup + prepareTimeout;
                while (!_cancelled && _video != null && !_video.isPrepared && _error == null && Time.realtimeSinceStartup < deadline)
                    yield return null;
                if (_cancelled) yield break;
                if (!_video.isPrepared || _error != null)
                {
                    yield return ShowFailure();
                    yield break;
                }

                _video.Play();
                deadline = Time.realtimeSinceStartup + prepareTimeout;
                while (!_firstFrame && _error == null && !_cancelled && Time.realtimeSinceStartup < deadline)
                    yield return null;
                if (_cancelled) yield break;
                if (!_firstFrame || _error != null)
                {
                    yield return ShowFailure();
                    yield break;
                }

                yield return Fade(_group, 1f);
                if (_cancelled) yield break;
                long lastFrame = _video.frame;
                deadline = Time.realtimeSinceStartup + prepareTimeout;
                while (!_finished && _error == null && !_cancelled)
                {
                    if (_video.frame != lastFrame)
                    {
                        lastFrame = _video.frame;
                        deadline = Time.realtimeSinceStartup + prepareTimeout;
                    }
                    if (Time.realtimeSinceStartup > deadline)
                    {
                        _error = "视频播放长时间没有推进。";
                        break;
                    }
                    yield return null;
                }
                if (_cancelled) yield break;
                if (_error != null)
                {
                    yield return ShowFailure();
                    yield break;
                }

                yield return Fade(_picture, 0f);
                if (_cancelled) yield break;
                CompletedSuccessfully = true;
                ReleaseVideo();
                // 留下黑色底板，直到原 ChieFlow 黑场、记录结局并换场景，避免闪回游戏画面。
            }
            finally
            {
                IsPlaying = false;
                if (!CompletedSuccessfully) Release();
            }
        }

        void PauseSceneAudio()
        {
            foreach (AudioSource source in FindObjectsOfType<AudioSource>())
            {
                if (!source.isPlaying) continue;
                _pausedAudio.Add(source);
                source.Pause();
            }
        }

        void BuildPlayer()
        {
            _overlay = new GameObject("雪夜通关MV", typeof(RectTransform), typeof(Canvas),
                typeof(GraphicRaycaster), typeof(CanvasGroup));
            Canvas canvas = _overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600;
            _group = _overlay.GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = true;

            MakeFullScreen("黑色底板", _overlay.transform).AddComponent<Image>().color = Color.black;
            GameObject picture = MakeFullScreen("MV画面", _overlay.transform);
            _picture = picture.AddComponent<CanvasGroup>();
            RawImage image = picture.AddComponent<RawImage>();
            image.raycastTarget = false;
            AspectRatioFitter fitter = picture.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = (float)clip.width / clip.height;

            _texture = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32);
            _texture.name = "雪夜通关MV纹理";
            _texture.Create();
            image.texture = _texture;

            _audio = _overlay.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _video = _overlay.AddComponent<VideoPlayer>();
            _video.playOnAwake = false;
            _video.isLooping = false;
            _video.waitForFirstFrame = true;
            _video.skipOnDrop = true;
            _video.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
            _video.source = VideoSource.VideoClip;
            _video.clip = clip;
            _video.renderMode = VideoRenderMode.RenderTexture;
            _video.targetTexture = _texture;
            _video.audioOutputMode = VideoAudioOutputMode.AudioSource;
            if (clip.audioTrackCount > 0)
            {
                _video.controlledAudioTrackCount = 1;
                _video.EnableAudioTrack(0, true);
                _video.SetTargetAudioSource(0, _audio);
            }
            _video.sendFrameReadyEvents = true;
            _video.frameReady += OnFrameReady;
            _video.loopPointReached += OnFinished;
            _video.errorReceived += OnVideoError;
        }

        IEnumerator ShowFailure()
        {
            Debug.LogWarning("[雪夜画] 通关 MV 无法播放，继续返回画廊。" + _error, this);
            ReleaseVideo();
            if (_overlay == null) yield break;
            _group.alpha = 1f;
            _picture.alpha = 0f;
            Text text = MakeFullScreen("播放提示", _overlay.transform).AddComponent<Text>();
            text.font = EndlessFun.UI.GameUiFont.Load();
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = "通关动画暂时无法播放，即将返回画廊…";
            yield return new WaitForSecondsRealtime(1.2f);
        }

        static GameObject MakeFullScreen(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        IEnumerator Fade(CanvasGroup group, float target)
        {
            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration && !_cancelled && group != null)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration));
                yield return null;
            }
            if (group != null) group.alpha = target;
        }

        void OnFrameReady(VideoPlayer source, long frame) { _firstFrame = true; }
        void OnFinished(VideoPlayer source) { _finished = true; }
        void OnVideoError(VideoPlayer source, string message) { _error = message; }

        void ReleaseVideo()
        {
            if (_video != null)
            {
                _video.frameReady -= OnFrameReady;
                _video.loopPointReached -= OnFinished;
                _video.errorReceived -= OnVideoError;
                _video.Stop();
                _video.targetTexture = null;
                Destroy(_video);
                _video = null;
            }
            if (_audio != null) _audio.Stop();
            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
                _texture = null;
            }
        }

        void Release()
        {
            ReleaseVideo();
            if (_overlay != null) Destroy(_overlay);
            _overlay = null;
            foreach (AudioSource source in _pausedAudio)
                if (source != null) source.UnPause();
            _pausedAudio.Clear();
        }

        void OnDisable()
        {
            _cancelled = true;
            StopAllCoroutines();
            IsPlaying = false;
            Release();
        }
    }
}
