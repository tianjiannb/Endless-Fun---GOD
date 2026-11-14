using System.Collections.Generic;
using Numen.Chie;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Numen.Chie.EditorTools
{
    /// <summary>Tools/Chie/注册场景到 Build Settings（幂等）+ 建场景。</summary>
    public static class ChieSceneInstaller
    {
        private const string ScenePath = "Assets/Scenes/Gallery/Scene_Chie.unity";
        private const string ConfigPath = "Assets/Paintings/Chie/Resources/Chie/ChieConfig.asset";
        private const string MatDir = "Assets/Paintings/Chie/Resources/Chie/Materials";

        /// <summary>只注册 Build Settings。</summary>
        [MenuItem("Tools/Chie/注册场景到 Build Settings")]
        public static void Register() { RegisterBatch(); }

        /// <summary>建场景并注册（批处理入口）。</summary>
        [MenuItem("Tools/Chie/生成 Scene_Chie 并注册")]
        public static void Rebuild()
        {
            BuildScene();
            RegisterBatch();
        }

        /// <summary>批处理：建场景 + 注册，一步到位。</summary>
        public static void BuildAllBatch()
        {
            BuildScene();
            RegisterBatch();
        }

        /// <summary>建出 Scene_Chie：相机 + Bootstrap，材质从 Resources 里取。</summary>
        public static void BuildScene()
        {
            EnsureDir("Assets/Scenes/Gallery");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(0x0A, 0x0A, 0x0A, 0xFF);
            camGo.transform.position = new Vector3(0f, 0f, -10f);

            var bootGo = new GameObject("ChieBootstrap", typeof(ChieBootstrap));
            var cfg = AssetDatabase.LoadAssetAtPath<ChieConfig>(ConfigPath);
            if (cfg == null) Debug.LogWarning("[Chie] 还没有 ChieConfig，先跑「生成占位资源」");
            var so = new SerializedObject(bootGo.GetComponent<ChieBootstrap>());
            so.FindProperty("_config").objectReferenceValue = cfg;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[Chie] 场景已生成：" + ScenePath);
        }

        private static void RegisterBatch()
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            string selfGuid = AssetDatabase.AssetPathToGUID(ScenePath);

            if (string.IsNullOrEmpty(selfGuid))
            {
                Debug.LogWarning("[Chie] 场景不存在，先跑「生成 Scene_Chie 并注册」：" + ScenePath);
                return;
            }

            bool found = false;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].path != ScenePath) continue;
                list[i] = new EditorBuildSettingsScene(ScenePath, true);
                found = true;
                break;
            }
            if (!found) list.Insert(0, new EditorBuildSettingsScene(ScenePath, true));

            EditorBuildSettings.scenes = list.ToArray();

            bool gallery = false;
            for (int i = 0; i < list.Count; i++)
            {
                string p = list[i].path;
                if (p.EndsWith("/Scene_Gallery.unity") || p.EndsWith("/Gallery.unity"))
                {
                    gallery = true;
                    break;
                }
            }
            if (!gallery)
                Debug.LogWarning("[Chie] Build Settings 里没有 Scene_Gallery，退出时会 Warn 并留在本场景");

            Debug.Log("[Chie] Build Settings 已注册：" + ScenePath + "（当前共 " + list.Count + " 个场景）");
        }

        private static void EnsureDir(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
