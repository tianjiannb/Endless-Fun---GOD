using UnityEngine;

namespace Numen.Chie.QA
{
    /// <summary>命令行含 -chieqa 时创建自动验收驱动器；其他情况完全不介入。</summary>
    public static class ChieQaBootstrap
    {
        /// <summary>场景加载后挂驱动器。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string mode;
            if (!ChieQaArgs.TryRead(out mode)) return;
            if (Object.FindObjectOfType<ChieQaDriver>() != null) return;
            // 只在画2 自己的场景里接管；画廊场景（加法加载或切场景）不介入
            if (Object.FindObjectOfType<ChieBootstrap>() == null) return;

            var go = new GameObject("ChieQaDriver");
            go.hideFlags = HideFlags.DontSave;
            Object.DontDestroyOnLoad(go);
            ChieQaDriver driver = go.AddComponent<ChieQaDriver>();
            Debug.Log("[Chie][QA] 启用自动验收 mode=" + mode + " args=" + string.Join(" ", System.Environment.GetCommandLineArgs()));
            driver.Begin(mode);
        }
    }
}
