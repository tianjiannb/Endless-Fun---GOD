using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>画作的可选入画视频。播放结束后仍通过 PaintingEntry 进入原来绑定的关卡。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(PaintingInteract))]
public sealed class PaintingVideoIntro : MonoBehaviour
{
    [Header("入画视频")]
    public VideoClip clip;
    [Min(1f)] public float prepareTimeout = 10f;
    [Min(0f)] public float fadeDuration = 0.25f;

    [Header("鼠标点击")]
    public Camera galleryCamera;
    public bool allowClick = true;
    [Tooltip("相对于 Sprite 整张画布的归一化区域，左下角为原点。只覆盖实际莲花画框，不包含透明留白。")]
    public Rect clickArea = new Rect(0.032f, 0.441f, 0.181f, 0.472f);

    static PaintingVideoIntro _active;
    public static bool IsPlaying { get { return _active != null; } }
    public bool IsBusy { get { return _active == this; } }

    PaintingInteract _interaction;
    readonly List<Behaviour> _suspended = new List<Behaviour>();
    readonly List<AudioSource> _pausedAudio = new List<AudioSource>();
    GameObject _overlay;
    CanvasGroup _overlayGroup;
    CanvasGroup _pictureGroup;
    VideoPlayer _video;
    RenderTexture _texture;
    bool _firstFrame;
    bool _finished;
    string _error;

    void Awake() { _interaction = GetComponent<PaintingInteract>(); }

    void Start()
    {
        if (HUD.Instance != null)
            HUD.Instance.SetHelpText("A / D  左右走动        E  进入画作        鼠标左键  点击莲花播放入画动画");
        // 从画作返回时，恢复完整画廊及其操作，不在画廊保留上一次的黑场。
        if (!GalleryIntro.IsPlaying && ScreenFader.Instance != null)
            ScreenFader.Instance.StartCoroutine(ScreenFader.Instance.FadeFromBlack());
    }

    void Update()
    {
        if (GalleryIntro.IsPlaying || !allowClick || IsPlaying || !Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (ContainsScreenPoint(Input.mousePosition)) _interaction.TryEnter();
    }

    public bool ContainsScreenPoint(Vector2 screenPoint)
    {
        Camera camera = galleryCamera != null ? galleryCamera : Camera.main;
        SpriteRenderer renderer = _interaction != null ? _interaction.canvasRenderer : null;
        if (camera == null || renderer == null || !renderer.enabled || renderer.sprite == null) return false;
        if (!camera.pixelRect.Contains(screenPoint)) return false;

        Ray ray = camera.ScreenPointToRay(screenPoint);
        Plane plane = new Plane(renderer.transform.forward, renderer.transform.position);
        float distance;
        if (!plane.Raycast(ray, out distance)) return false;
        Vector3 local = renderer.transform.InverseTransformPoint(ray.GetPoint(distance));
        Bounds bounds = renderer.sprite.bounds;
        Vector2 uv = new Vector2(
            Mathf.InverseLerp(bounds.min.x, bounds.max.x, local.x),
            Mathf.InverseLerp(bounds.min.y, bounds.max.y, local.y));
        if (!bounds.Contains(new Vector3(local.x, local.y, bounds.center.z))) return false;
        if (renderer.flipX) uv.x = 1f - uv.x;
        if (renderer.flipY) uv.y = 1f - uv.y;
        return clickArea.Contains(uv);
    }

    public bool Play()
    {
        if (GalleryIntro.IsPlaying || IsPlaying || !isActiveAndEnabled) return false;
        PaintingEntry.FailReason reason;
        if (!PaintingEntry.CanEnter(_interaction.data, out reason))
        {
            ShowFailure("暂时无法进入这幅画，请检查关卡是否已解锁并加入构建列表。");
            return false;
        }
        if (clip == null)
        {
            ShowFailure("入画视频尚未配置，请在莲花画节点上指定视频后重试。");
            return false;
        }

        _active = this;
        _firstFrame = false;
        _finished = false;
        _error = null;
        SuspendGallery();
        StartCoroutine(PlayAndEnter());
        return true;
    }

    void SuspendGallery()
    {
        foreach (PaintingInteract interaction in FindObjectsOfType<PaintingInteract>()) Suspend(interaction);
        if (PlayerMove2D.Instance != null) Suspend(PlayerMove2D.Instance);
        if (AudioManager.Instance != null)
        {
            foreach (AudioSource source in AudioManager.Instance.GetComponents<AudioSource>())
            {
                if (!source.isPlaying) continue;
                _pausedAudio.Add(source);
                source.Pause();
            }
        }
        if (HUD.Instance != null) HUD.Instance.Show("正在准备入画动画…", "painting_intro");
    }

    void Suspend(Behaviour behaviour)
    {
        if (!behaviour.enabled) return;
        _suspended.Add(behaviour);
        behaviour.enabled = false;
    }

    IEnumerator PlayAndEnter()
    {
        BuildPlayer();
        _video.Prepare();
        float deadline = Time.realtimeSinceStartup + prepareTimeout;
        while (!_video.isPrepared && _error == null && Time.realtimeSinceStartup < deadline)
            yield return null;
        if (!_video.isPrepared || _error != null)
        {
            Recover("入画视频准备失败，请稍后重试。");
            yield break;
        }

        _video.Play();
        deadline = Time.realtimeSinceStartup + prepareTimeout;
        while (!_firstFrame && _error == null && Time.realtimeSinceStartup < deadline)
            yield return null;
        if (!_firstFrame || _error != null)
        {
            Recover("入画视频未能开始播放，请稍后重试。");
            yield break;
        }

        if (HUD.Instance != null) HUD.Instance.Hide("painting_intro");
        yield return Fade(_overlayGroup, 1f);
        long lastFrame = _video.frame;
        deadline = Time.realtimeSinceStartup + prepareTimeout;
        while (!_finished && _error == null)
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
        if (_error != null)
        {
            Recover("入画视频播放中断，请重试。");
            yield break;
        }

        // 最后一帧淡到黑色底板，避免 Stop() 清空纹理造成闪屏。
        yield return Fade(_pictureGroup, 0f);
        PaintingEntry.FailReason reason;
        if (!PaintingEntry.CanEnter(_interaction.data, out reason))
        {
            Recover("目标关卡暂时无法进入，已返回画廊。");
            yield break;
        }
        if (ScreenFader.Instance != null) ScreenFader.Instance.SetAlphaImmediate(1f);
        if (!PaintingEntry.Enter(_interaction.data, false, out reason))
        {
            if (ScreenFader.Instance != null) ScreenFader.Instance.SetAlphaImmediate(0f);
            Recover("进入关卡失败，已返回画廊，请重试。");
        }
    }

    void BuildPlayer()
    {
        _overlay = new GameObject("莲花入画视频", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = _overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450; // 位于 HUD 上方、现有 ScreenFader（500）下方。
        _overlayGroup = _overlay.GetComponent<CanvasGroup>();
        _overlayGroup.alpha = 0f;
        _overlayGroup.blocksRaycasts = true;

        GameObject black = MakeFullScreen("黑色底板", _overlay.transform);
        black.AddComponent<Image>().color = Color.black;
        GameObject picture = MakeFullScreen("视频画面", _overlay.transform);
        _pictureGroup = picture.AddComponent<CanvasGroup>();
        RawImage image = picture.AddComponent<RawImage>();
        image.raycastTarget = false;
        AspectRatioFitter fitter = picture.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = (float)clip.width / clip.height;

        _texture = new RenderTexture((int)clip.width, (int)clip.height, 0, RenderTextureFormat.ARGB32);
        _texture.name = "莲花入画视频纹理";
        _texture.Create();
        image.texture = _texture;

        AudioSource audio = _overlay.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;
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
            _video.SetTargetAudioSource(0, audio);
        }
        _video.sendFrameReadyEvents = true;
        _video.frameReady += OnFrameReady;
        _video.loopPointReached += OnFinished;
        _video.errorReceived += OnVideoError;
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
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration));
            yield return null;
        }
        group.alpha = target;
    }

    void OnFrameReady(VideoPlayer source, long frame) { _firstFrame = true; }
    void OnFinished(VideoPlayer source) { _finished = true; }
    void OnVideoError(VideoPlayer source, string message) { _error = message; }

    void Recover(string message)
    {
        if (!string.IsNullOrEmpty(_error)) Debug.LogWarning("[画廊] 入画视频：" + _error, this);
        Release();
        ShowFailure(message);
    }

    void ShowFailure(string message)
    {
        Debug.LogWarning("[画廊] " + message, this);
        if (HUD.Instance != null) HUD.Instance.Show(message + "\n点击莲花画或按 E 重试", "painting_intro");
    }

    void OnDisable()
    {
        StopAllCoroutines();
        Release();
    }

    void Release()
    {
        if (HUD.Instance != null) HUD.Instance.Hide("painting_intro");
        if (_video != null)
        {
            _video.frameReady -= OnFrameReady;
            _video.loopPointReached -= OnFinished;
            _video.errorReceived -= OnVideoError;
            _video.Stop();
            _video.targetTexture = null;
        }
        if (_overlay != null) Destroy(_overlay);
        if (_texture != null)
        {
            _texture.Release();
            Destroy(_texture);
        }
        foreach (Behaviour behaviour in _suspended)
            if (behaviour != null) behaviour.enabled = true;
        foreach (AudioSource source in _pausedAudio)
            if (source != null) source.UnPause();
        _suspended.Clear();
        _pausedAudio.Clear();
        _video = null;
        _overlay = null;
        _texture = null;
        if (_active == this) _active = null;
    }
}
