using System.Collections;
using UnityEngine;

public class PaintingSceneRoot : MonoBehaviour
{
    [Header("流程")]
    public string paintingId = "p1";
    [Tooltip("paintingId 留空时从 GameSession 读取")]
    public bool useSessionPaintingId = true;
    public bool fadeOnExit = true;

    [Header("输入")]
    public bool allowEscapeExit = true;
    public KeyCode exitKey = KeyCode.Escape;

    bool _exiting;

    public bool IsExiting { get { return _exiting; } }

    void Start()
    {
        GameSession gs = GameSession.Instance;
        if (useSessionPaintingId && gs != null && !string.IsNullOrEmpty(gs.CurrentPaintingId)) paintingId = gs.CurrentPaintingId;
        if (ScreenFader.Instance != null) StartCoroutine(ScreenFader.Instance.FadeFromBlack());
    }

    void Update()
    {
        if (allowEscapeExit && !_exiting && Input.GetKeyDown(exitKey)) ExitWith("abandon");
    }

    public void ExitWith(string choiceId)
    {
        ExitWith(choiceId, choiceId);
    }

    public void ExitWith(string choiceId, string outcome)
    {
        if (_exiting) return;
        _exiting = true;
        PaintingEntry.Exit(new ChoiceResult(paintingId, choiceId, outcome), fadeOnExit);
    }

    public void ExitAfter(float delay, string choiceId)
    {
        ExitAfter(delay, choiceId, choiceId);
    }

    public void ExitAfter(float delay, string choiceId, string outcome)
    {
        if (_exiting) return;
        StartCoroutine(ExitRoutine(delay, choiceId, outcome));
    }

    IEnumerator ExitRoutine(float delay, string choiceId, string outcome)
    {
        yield return new WaitForSecondsRealtime(delay);
        ExitWith(choiceId, outcome);
    }

    public static PaintingSceneRoot Find()
    {
        return FindObjectOfType<PaintingSceneRoot>();
    }
}
