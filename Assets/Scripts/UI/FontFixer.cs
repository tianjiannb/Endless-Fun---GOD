using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 为初始场景和后续切换的场景统一应用项目内中文字体。
/// </summary>
public static class FontFixer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void PatchOnLoad()
    {
        SceneManager.sceneLoaded -= PatchScene;
        SceneManager.sceneLoaded += PatchScene;
        Patch();
    }

    static void PatchScene(Scene scene, LoadSceneMode mode) { Patch(); }

    public static void Patch()
    {
        Font f = EndlessFun.UI.GameUiFont.Load();
        Text[] all = Object.FindObjectsOfType<Text>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].font != f) all[i].font = f;
        }
    }
}
