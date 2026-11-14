using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Numen.Chie.EditorTools
{
    /// <summary>打一个 Windows 开发版播放器，用于跑一遍冒烟测试。</summary>
    public static class ChiePlayerBuilder
    {
        private const string ScenePath = "Assets/Scenes/Gallery/Scene_Chie.unity";
        private const string OutPath = "Build/Chie/Chie.exe";

        /// <summary>批处理入口。</summary>
        public static void BuildBatch()
        {
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport rep = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[Chie] 构建结果：" + rep.summary.result
                + " 大小 " + rep.summary.totalSize
                + " 错误 " + rep.summary.totalErrors
                + " → " + OutPath);
            if (rep.summary.result != BuildResult.Succeeded) Debug.LogError("[Chie] 构建失败");
        }
    }
}
