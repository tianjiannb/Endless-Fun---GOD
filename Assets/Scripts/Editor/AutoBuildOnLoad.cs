using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class AutoBuildOnLoad
{
    const string SessionKey = "EndlessFun.AutoBuild.Done";
    const string MarkerFile = "_REBUILD_GALLERY";

    static AutoBuildOnLoad()
    {
        if (Application.isBatchMode) return;
        EditorApplication.delayCall += Run;
    }

    static string MarkerPath
    {
        get { return Path.Combine(Directory.GetCurrentDirectory(), MarkerFile); }
    }

    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Run;
            return;
        }
        if (EditorUtility.scriptCompilationFailed) return;

        bool forced = File.Exists(MarkerPath);
        if (!forced)
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(GallerySceneBootstrap.GalleryPath) != null)
            {
                SessionState.SetBool(SessionKey, true);
                return;
            }
        }

        SessionState.SetBool(SessionKey, true);
        if (forced) File.Delete(MarkerPath);

        Debug.Log("[画廊] 开始生成白模素材与场景…");
        try
        {
            GallerySceneBootstrap.BuildAll();
            Debug.Log("[画廊] 生成完成，已打开 Gallery 场景，按 Play 即可。");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[画廊] 生成失败，用菜单 Tools/游趣无限/0 手动重跑: " + e);
        }
    }
}
