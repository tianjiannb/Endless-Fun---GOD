using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 编辑器/运行时兜底：把 LegacyRuntime.ttf 塞给场景里所有 Text，
/// 避免脚本建 UI 时字体为空导致文字不可见。
/// </summary>
public static class FontFixer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void PatchOnLoad() { Patch(); }

    public static void Patch()
    {
        Font f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f == null) f = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (f == null) return;
        Text[] all = Object.FindObjectsOfType<Text>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].font == null) all[i].font = f;
        }
    }
}
