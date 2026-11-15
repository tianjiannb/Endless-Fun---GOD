using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Logo 后接画廊透视运镜；同次启动只播一次，全程归还原有组件状态。
/// </summary>
public class GalleryIntro : MonoBehaviour
{
    [Header("素材")]
    public string logoResource = "UI/logo";
    public float logoSize = 400f;

    [Header("节奏（秒）")]
    public float logoFadeIn = 0.9f;
    public float hold = 1.6f;
    public float logoFadeOut = 0.9f;
    public float blackOut = 0.8f;

    [Header("Logo 后的画廊运镜")]
    public GalleryOpeningTour tour;

    static bool _played;
    static GalleryIntro _active;
    public static bool IsPlaying { get { return _active != null; } }
    readonly List<Behaviour> _suspended = new List<Behaviour>();
    GameObject _canvas;
    GameObject _skip;
    bool _ownsIntro;

    CanvasGroup _black;
    CanvasGroup _logo;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetPlayback() { _played = false; _active = null; }

    void Awake()
    {
        if (!enabled || _played) return;
        _played = true;
        _active = this;
        _ownsIntro = true;
        BuildUi();
    }

    void Start()
    {
        if (!_ownsIntro) return;
        Suspend(PlayerMove2D.Instance);
        foreach (PaintingInteract item in FindObjectsOfType<PaintingInteract>()) Suspend(item);
        foreach (GalleryEndArrow item in FindObjectsOfType<GalleryEndArrow>()) Suspend(item);
        if (HUD.Instance != null) Suspend(HUD.Instance.GetComponentInParent<Canvas>());
        if (tour != null) tour.Prepare();
        StartCoroutine(Play());
    }

    void Update()
    {
        if (_ownsIntro && tour != null && tour.IsPlaying && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space)))
            tour.RequestSkip();
    }

    void Suspend(Behaviour behaviour)
    {
        if (behaviour == null || !behaviour.enabled) return;
        _suspended.Add(behaviour);
        behaviour.enabled = false;
    }

    void BuildUi()
    {
        GameObject canvasGo = new GameObject("IntroCanvas", typeof(Canvas), typeof(CanvasScaler));
        _canvas = canvasGo;
        canvasGo.transform.SetParent(transform, false);
        canvasGo.AddComponent<GraphicRaycaster>();
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform root = canvasGo.GetComponent<RectTransform>();

        RectTransform black = MakeFullScreen(root, "Black");
        Image blackImage = black.gameObject.AddComponent<Image>();
        blackImage.color = Color.black;
        blackImage.raycastTarget = true;
        _black = black.gameObject.AddComponent<CanvasGroup>();
        _black.alpha = 1f;

        GameObject logoGo = new GameObject("Logo", typeof(RectTransform));
        RectTransform logoRect = logoGo.GetComponent<RectTransform>();
        logoRect.SetParent(root, false);
        logoRect.anchorMin = new Vector2(0.5f, 0.5f);
        logoRect.anchorMax = new Vector2(0.5f, 0.5f);
        logoRect.sizeDelta = new Vector2(logoSize, logoSize);
        logoRect.anchoredPosition = Vector2.zero;

        Image logo = logoGo.AddComponent<Image>();
        logo.sprite = Resources.Load<Sprite>(logoResource);
        logo.raycastTarget = false;
        logo.preserveAspect = true;
        _logo = logoGo.AddComponent<CanvasGroup>();
        _logo.alpha = 0f;

        _skip = new GameObject("跳过开场", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform skipRect = _skip.GetComponent<RectTransform>();
        skipRect.SetParent(root,false);
        skipRect.anchorMin = skipRect.anchorMax = new Vector2(1f,0f);
        skipRect.pivot = new Vector2(1f,0f);
        skipRect.anchoredPosition = new Vector2(-36f,32f);
        skipRect.sizeDelta = new Vector2(285f,48f);
        _skip.GetComponent<Image>().color = new Color(.02f,.04f,.09f,.65f);
        _skip.GetComponent<Button>().onClick.AddListener(() => { if (tour != null) tour.RequestSkip(); });
        RectTransform textRect = MakeFullScreen(skipRect,"跳过提示");
        Text text = textRect.gameObject.AddComponent<Text>();
        text.font = EndlessFun.UI.GameUiFont.Load();
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleCenter;
        text.text = "跳过开场  ·  空格 / Esc";
        text.color = new Color(.91f,.94f,1f,.9f);
        text.raycastTarget = false;
        _skip.SetActive(false);
    }

    RectTransform MakeFullScreen(RectTransform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    IEnumerator Play()
    {
        yield return Fade(_logo, 1f, logoFadeIn);
        yield return new WaitForSecondsRealtime(hold);
        yield return Fade(_logo, 0f, logoFadeOut);
        yield return new WaitForSecondsRealtime(0.15f);
        yield return Fade(_black, 0f, blackOut);
        if (tour != null)
        {
            _skip.SetActive(true);
            yield return tour.Play();
        }
        Release();
    }

    void Release()
    {
        if (!_ownsIntro) return;
        if (tour != null) tour.Restore();
        foreach (Behaviour behaviour in _suspended)
            if (behaviour != null) behaviour.enabled = true;
        _suspended.Clear();
        if (_canvas != null) Destroy(_canvas);
        _canvas = null;
        _ownsIntro = false;
        if (_active == this) _active = null;
    }

    void OnDisable() { StopAllCoroutines(); Release(); }

    static IEnumerator Fade(CanvasGroup group, float target, float duration)
    {
        float from = group.alpha;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, target, Mathf.Clamp01(t / duration));
            yield return null;
        }
        group.alpha = target;
    }
}
