using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 进入画廊前的开场：黑场中居中浮现 logo，停留后淡出，再揭开花廊。
/// 同一次运行只播一次；期间锁住角色移动。UI 全部在代码里搭建。
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

    static bool _played;

    CanvasGroup _black;
    CanvasGroup _logo;

    void Start()
    {
        if (_played) return;
        _played = true;
        BuildUi();
        StartCoroutine(Play());
    }

    void BuildUi()
    {
        GameObject canvasGo = new GameObject("IntroCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform root = canvasGo.GetComponent<RectTransform>();

        RectTransform black = MakeFullScreen(root, "Black");
        Image blackImage = black.gameObject.AddComponent<Image>();
        blackImage.color = Color.black;
        blackImage.raycastTarget = false;
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
        PlayerMove2D player = PlayerMove2D.Instance;
        if (player != null) player.enabled = false;

        yield return Fade(_logo, 1f, logoFadeIn);
        yield return new WaitForSecondsRealtime(hold);
        yield return Fade(_logo, 0f, logoFadeOut);
        yield return new WaitForSecondsRealtime(0.15f);
        yield return Fade(_black, 0f, blackOut);

        if (player != null) player.enabled = true;
        Destroy(_black.transform.parent.gameObject);
    }

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
