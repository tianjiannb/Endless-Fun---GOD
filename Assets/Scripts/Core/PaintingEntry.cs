using UnityEngine;
using UnityEngine.SceneManagement;

public static class PaintingEntry
{
    public enum FailReason
    {
        None,
        NullData,
        EmptySceneName,
        NotInBuild,
        Locked,
    }

    public static event System.Action<string> Entered;
    public static event System.Action<ChoiceResult> Exited;

    public static bool CanEnter(PaintingData data, out FailReason reason)
    {
        reason = FailReason.None;
        if (data == null) { reason = FailReason.NullData; return false; }
        if (!data.interactable) { reason = FailReason.Locked; return false; }
        if (string.IsNullOrEmpty(data.sceneName)) { reason = FailReason.EmptySceneName; return false; }
        if (!Application.CanStreamedLevelBeLoaded(data.sceneName)) { reason = FailReason.NotInBuild; return false; }
        return true;
    }

    public static bool Enter(PaintingData data, bool fade = true)
    {
        FailReason reason;
        return Enter(data, fade, out reason);
    }

    public static bool Enter(PaintingData data, bool fade, out FailReason reason)
    {
        reason = FailReason.None;
        if (data == null)
        {
            reason = FailReason.NullData;
            Debug.LogWarning("[画廊] 进入画作失败：PaintingData 为空");
            return false;
        }
        if (!data.interactable)
        {
            reason = FailReason.Locked;
            Debug.LogWarning("[画廊] 画作当前不可进入（interactable=false）: " + data.paintingId);
            return false;
        }
        return Enter(data.paintingId, data.sceneName, fade, out reason);
    }

    public static bool Enter(string paintingId, string sceneName, bool fade = true)
    {
        FailReason reason;
        return Enter(paintingId, sceneName, fade, out reason);
    }

    public static bool Enter(string paintingId, string sceneName, bool fade, out FailReason reason)
    {
        reason = FailReason.None;

        if (string.IsNullOrEmpty(sceneName))
        {
            reason = FailReason.EmptySceneName;
            Debug.LogWarning("[画廊] 进入画作失败：sceneName 为空（paintingId=" + paintingId + "）");
            return false;
        }
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            reason = FailReason.NotInBuild;
            Debug.LogWarning("[画廊] 进入画作失败：场景不在 Build Settings 里 → " + sceneName);
            return false;
        }

        GameSession gs = GameSession.Ensure();

        if (fade && ScreenFader.Instance != null)
        {
            gs.BeginEnter(paintingId);
            ScreenFader.Instance.FadeAndLoad(sceneName);
        }
        else
        {
            gs.Enter(paintingId, sceneName);
        }

        if (Entered != null) Entered(paintingId);
        return true;
    }

    public static void Exit(ChoiceResult result, bool fade = true)
    {
        GameSession gs = GameSession.Ensure();

        if (fade && ScreenFader.Instance != null)
        {
            gs.RecordResult(result);
            ScreenFader.Instance.FadeAndLoad(gs.ReturnScene);
        }
        else
        {
            gs.Exit(result);
        }

        if (Exited != null) Exited(result);
    }

    public static void ExitToGallery(bool fade = true)
    {
        GameSession gs = GameSession.Ensure();
        if (fade && ScreenFader.Instance != null) ScreenFader.Instance.FadeAndLoad(gs.ReturnScene);
        else SceneManager.LoadScene(gs.ReturnScene);
    }
}
