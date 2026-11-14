using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 穿画过渡：CanvasGroup 淡入淡出，全程协程，绝不阻塞主线程。
/// 挂在全屏黑色 Image（带 CanvasGroup）上，DontDestroyOnLoad。
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    public float fadeOutDuration = 0.35f;
    public float fadeInDuration = 0.45f;

    CanvasGroup _group;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        _group.blocksRaycasts = false;
        _group.interactable = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SetAlphaImmediate(float a)
    {
        if (_group != null) _group.alpha = Mathf.Clamp01(a);
    }

    public IEnumerator FadeAndLoad(string sceneName)
    {
        yield return FadeTo(1f, fadeOutDuration);
        if (!string.IsNullOrEmpty(sceneName)) SceneManager.LoadScene(sceneName);
    }

    public IEnumerator FadeFromBlack()
    {
        SetAlphaImmediate(1f);
        yield return FadeTo(0f, fadeInDuration);
    }

    public IEnumerator FadeTo(float target, float duration)
    {
        if (_group == null) yield break;
        float start = _group.alpha;
        float t = 0f;
        if (duration <= 0f) { _group.alpha = target; yield break; }
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            _group.alpha = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }
        _group.alpha = target;
    }
}
