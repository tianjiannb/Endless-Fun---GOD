using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 画廊 HUD：屏幕下方浮出的「画名 + 按 E 进入」提示，以及左下角操作说明。
/// 全部代码搭 UI，不依赖预制体。
/// </summary>
public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }

    [Header("样式")]
    public int promptFontSize = 34;
    public int helpFontSize = 20;

    Text _promptText;
    RectTransform _promptRoot;
    Text _helpText;
    string _currentId;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        BuildUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void BuildUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();

        _promptRoot = MakePanel(canvas.transform, "PromptRoot",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 200f), new Vector2(0f, 360f));
        _promptText = MakeText(_promptRoot, "PromptText", promptFontSize, TextAnchor.LowerCenter);
        _promptText.text = "";
        _promptText.color = new Color(0.99f, 0.94f, 0.80f, 1f);
        UnityEngine.UI.Shadow promptShadow = _promptText.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        promptShadow.effectColor = new Color(0.05f, 0.12f, 0.26f, 0.7f);
        promptShadow.effectDistance = new Vector2(2f, -2f);
        _promptRoot.gameObject.SetActive(false);

        RectTransform helpRoot = MakePanel(canvas.transform, "HelpRoot",
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0f, 40f), new Vector2(0f, 80f));
        _helpText = MakeText(helpRoot, "HelpText", helpFontSize, TextAnchor.LowerLeft);
        _helpText.text = "A / D  左右走动        E  进入画作";
        _helpText.color = new Color(0.93f, 0.89f, 0.76f, 0.95f);
        UnityEngine.UI.Shadow helpShadow = _helpText.gameObject.AddComponent<UnityEngine.UI.Shadow>();
        helpShadow.effectColor = new Color(0.05f, 0.12f, 0.26f, 0.7f);
        helpShadow.effectDistance = new Vector2(2f, -2f);
    }

    RectTransform MakePanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = offMin;
        rt.offsetMax = offMax;
        return rt;
    }

    Text MakeText(RectTransform parent, string name, int size, TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(80f, 0f);
        rt.offsetMax = new Vector2(-80f, 0f);
        Text t = go.AddComponent<Text>();
        t.font = EndlessFun.UI.GameUiFont.Load();
        t.fontSize = size;
        t.alignment = anchor;
        t.color = new Color(0.10f, 0.10f, 0.12f, 1f);
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public void Show(string text, string id)
    {
        if (_promptText == null) return;
        _currentId = id;
        _promptText.text = text;
        _promptRoot.gameObject.SetActive(true);
    }

    public void Hide(string id)
    {
        if (_promptRoot == null) return;
        if (id != null && _currentId != id) return;
        _currentId = null;
        _promptRoot.gameObject.SetActive(false);
    }

    public void SetHelpText(string text)
    {
        if (_helpText != null) _helpText.text = text;
    }

    public static void ShowPaintingPrompt(PaintingData data)
    {
        if (Instance == null) return;
        string title = (data != null && !string.IsNullOrEmpty(data.displayName)) ? data.displayName : "画作";
        string id = (data != null) ? data.paintingId : null;
        Instance.Show(title + "\n按 E 进入", id);
    }

    public static void HidePaintingPrompt(PaintingData data)
    {
        if (Instance == null || data == null) return;
        Instance.Hide(data.paintingId);
    }

    public static void ShowRawPrompt(string text, string id)
    {
        if (Instance == null) return;
        Instance.Show(text, id);
    }

    public static void HideRawPrompt(string id)
    {
        if (Instance == null) return;
        Instance.Hide(id);
    }
}
