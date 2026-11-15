using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 结算场景：黑场淡出后，两句关于画的话先后浮现，再浮出「重新游戏 / 退出游戏」。
/// 场景里只需要挂这一个组件，UI 全部在代码里搭建。
/// </summary>
public class EndingSequence : MonoBehaviour
{
    [Header("文案")]
    [TextArea] public string line1 = "画是静止的时间，看画的人替它走了一程。";
    [TextArea] public string line2 = "我们描摹世界，不过是想在世界上留下一笔目光。";

    [Header("节奏（秒）")]
    public float blackFade = 1.0f;
    public float firstLineDelay = 0.35f;
    public float lineFade = 1.2f;
    public float lineInterval = 1.4f;
    public float buttonDelay = 1.0f;

    CanvasGroup _black;
    Image _blackImage;
    CanvasGroup _lineA;
    CanvasGroup _lineB;
    CanvasGroup _buttons;
    bool _leaving;

    void Start()
    {
        BuildUi();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMusic(AudioId.MusicSnow);
        StartCoroutine(Play());
    }

    void BuildUi()
    {
        EnsureEventSystem();

        GameObject canvasGo = new GameObject("EndingCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform root = canvasGo.GetComponent<RectTransform>();

        RectTransform textRoot = MakeFullScreen(root, "TextRoot");
        _lineA = MakeLine(textRoot, line1, 64f);
        _lineB = MakeLine(textRoot, line2, -64f);

        RectTransform buttonRoot = MakeFullScreen(root, "ButtonsRoot");
        _buttons = buttonRoot.gameObject.AddComponent<CanvasGroup>();
        _buttons.alpha = 0f;
        _buttons.interactable = false;
        _buttons.blocksRaycasts = false;
        BuildButton(buttonRoot, "重新游戏", new Vector2(-150f, -300f), RestartGame);
        BuildButton(buttonRoot, "退出游戏", new Vector2(150f, -300f), QuitGame);

        RectTransform black = MakeFullScreen(root, "Black");
        _blackImage = black.gameObject.AddComponent<Image>();
        _blackImage.color = new Color(0.02f, 0.02f, 0.025f, 1f);
        _blackImage.raycastTarget = false;
        _black = black.gameObject.AddComponent<CanvasGroup>();
        _black.alpha = 1f;
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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

    CanvasGroup MakeLine(RectTransform parent, string text, float y)
    {
        GameObject go = new GameObject("Line", typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(1400f, 140f);
        rt.anchoredPosition = new Vector2(0f, y);

        Text t = go.AddComponent<Text>();
        t.font = EndlessFun.UI.GameUiFont.Load();
        t.fontSize = 46;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.98f, 0.94f, 0.86f, 1f);
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.text = text;

        CanvasGroup group = go.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        return group;
    }

    void BuildButton(RectTransform parent, string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject("Btn_" + label, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(240f, 72f);
        rt.anchoredPosition = position;

        Image image = go.GetComponent<Image>();
        image.color = new Color(1f, 0.95f, 0.82f, 0.06f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 0.95f, 0.82f, 0.06f);
        colors.highlightedColor = new Color(1f, 0.95f, 0.82f, 0.22f);
        colors.pressedColor = new Color(1f, 0.95f, 0.82f, 0.34f);
        colors.selectedColor = colors.normalColor;
        colors.fadeDuration = 0.12f;
        button.colors = colors;
        button.onClick.AddListener(onClick);

        GameObject textGo = new GameObject("Label", typeof(RectTransform));
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.SetParent(rt, false);
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Text t = textGo.AddComponent<Text>();
        t.font = EndlessFun.UI.GameUiFont.Load();
        t.fontSize = 30;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = new Color(0.97f, 0.93f, 0.84f, 1f);
        t.raycastTarget = false;
        t.text = label;
    }

    IEnumerator Play()
    {
        yield return Fade(_black, 0f, blackFade);
        yield return new WaitForSecondsRealtime(firstLineDelay);
        yield return Fade(_lineA, 1f, lineFade);
        yield return new WaitForSecondsRealtime(lineInterval);
        yield return Fade(_lineB, 1f, lineFade);
        yield return new WaitForSecondsRealtime(buttonDelay);
        yield return Fade(_buttons, 1f, 0.8f);
        _buttons.interactable = true;
        _buttons.blocksRaycasts = true;
    }

    public void RestartGame()
    {
        if (_leaving) return;
        _leaving = true;
        _buttons.interactable = false;
        GameSession.Ensure().Clear();
        StartCoroutine(Leave("Gallery"));
    }

    public void QuitGame()
    {
        if (_leaving) return;
        _leaving = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator Leave(string sceneName)
    {
        _blackImage.raycastTarget = true;
        yield return Fade(_black, 1f, 0.7f);
        SceneManager.LoadScene(sceneName);
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
