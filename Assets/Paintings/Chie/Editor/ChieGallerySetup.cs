using Numen.Chie;
using Numen.Chie.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Numen.Chie.Gallery.EditorTools
{
    /// <summary>把画2 正式接进白模画廊：画作数据 → 画作场景 → 画廊场景 → Build Settings。</summary>
    public static class ChieGallerySetup
    {
        private const string DataDir = "Assets/Resources/Paintings";
        private const string DataPath = DataDir + "/chie.asset";
        private const string ThumbPath = "Assets/Paintings/Chie/Art/base_sit.png";

        [MenuItem("Tools/Chie/接入画廊（画作数据 + 场景 + Build Settings）")]
        public static void Run()
        {
            ChieSceneInstaller.BuildScene();
            EnsurePaintingData();
            GallerySceneBootstrap.BuildGallery();
            GallerySceneBootstrap.SetupBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (!Application.isBatchMode) EditorSceneManager.OpenScene(GallerySceneBootstrap.GalleryPath, OpenSceneMode.Single);
            Debug.Log("[Chie] 画廊接入完成");
        }

        /// <summary>批处理入口。</summary>
        public static void SetupBatch() { Run(); }

        private static void EnsurePaintingData()
        {
            if (!AssetDatabase.IsValidFolder(DataDir))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
                AssetDatabase.CreateFolder("Assets/Resources", "Paintings");
            }

            PaintingData d = AssetDatabase.LoadAssetAtPath<PaintingData>(DataPath);
            if (d == null)
            {
                d = ScriptableObject.CreateInstance<PaintingData>();
                AssetDatabase.CreateAsset(d, DataPath);
            }

            d.paintingId = "chie";
            d.displayName = "画中女人";
            d.teaser = "她在画里等了很多年，只记得自己是被人一笔一笔放弃的。";
            d.sortOrder = 2;
            d.sceneName = "Scene_Chie";
            d.interactable = true;

            Sprite sp = LoadThumbSprite();
            d.thumbnail = sp;
            d.placeholderSprite = sp;

            EditorUtility.SetDirty(d);
        }

        /// <summary>占位图按 Default 导入（画作内部按 Texture2D 用），这里只把它补成 Sprite 供画廊缩略图。</summary>
        private static Sprite LoadThumbSprite()
        {
            Sprite sp = AssetDatabase.LoadAssetAtPath<Sprite>(ThumbPath);
            if (sp != null) return sp;

            TextureImporter ti = AssetImporter.GetAtPath(ThumbPath) as TextureImporter;
            if (ti == null) { Debug.LogWarning("[Chie] 缩略图没找到：" + ThumbPath); return null; }
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100f;
            ti.alphaIsTransparency = true;
            ti.SaveAndReimport();

            sp = AssetDatabase.LoadAssetAtPath<Sprite>(ThumbPath);
            if (sp == null) Debug.LogWarning("[Chie] 缩略图仍不可用：" + ThumbPath);
            return sp;
        }
    }
}
