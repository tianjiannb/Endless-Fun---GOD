using System.Collections.Generic;
using System.IO;
using Numen.Chie;
using UnityEditor;
using UnityEngine;

namespace Numen.Chie.EditorTools
{
    /// <summary>Tools/Chie/生成占位资源：出全部占位图 + 材质 + ChieConfig。</summary>
    public static class ChiePlaceholderBuilder
    {
        private const string ArtDir = "Assets/Paintings/Chie/Art";
        private const string SourceBg = ArtDir + "/Source/bg.jpg";
        private const string SourceSit = ArtDir + "/Source/char_sit.png";
        private const string SourceStand = ArtDir + "/Source/char_stand.png";
        private const string ResDir = "Assets/Paintings/Chie/Resources/Chie";
        private const string MatDir = ResDir + "/Materials";
        private const string ConfigPath = ResDir + "/ChieConfig.asset";

        private static readonly PoseId[] Poses = { PoseId.Sit, PoseId.Shift, PoseId.Up, PoseId.Stand };

        /// <summary>菜单入口。</summary>
        [MenuItem("Tools/Chie/生成占位资源")]
        public static void BuildAll() { BuildAllBatch(); }

        /// <summary>批处理入口。</summary>
        public static void BuildAllBatch()
        {
            EnsureDir(ArtDir);
            EnsureDir(MatDir);

            // 分层美术源图（Art/Source/bg.jpg + char_sit.png + char_stand.png）齐了，四姿态三张图
            // 由 tools/make_art.py 先合成好：背景不动、只位移人物层，stand 换成站立立绘。
            // 这里只做导入设置；缺源图才退回程序化占位绘制。
            bool useSource = File.Exists(SourceBg) && File.Exists(SourceSit) && File.Exists(SourceStand);
            var poseArts = new List<PoseArt>();
            for (int i = 0; i < Poses.Length; i++)
            {
                PoseId pose = Poses[i];
                string tag = pose.ToString().ToLowerInvariant();
                var art = new PoseArt { pose = pose };
                if (useSource)
                {
                    art.baseTex = Import("base_" + tag, false, true, false);
                    art.fixedTex = Import("fixed_" + tag, false, true, false);
                    art.maskTex = Import("mask_" + tag, false, true, true);
                    if (art.baseTex == null || art.fixedTex == null || art.maskTex == null)
                    {
                        Debug.LogError("[Chie] 正式美术缺图：Art/base|fixed|mask_" + tag + ".png（源图需 Art/Source/bg.jpg + char_sit.png + char_stand.png，先跑 tools/make_art.py）");
                        return;
                    }
                }
                else
                {
                    art.baseTex = Write("base_" + tag, ChiePlaceholderArt.BuildBase(pose), false, true, false);
                    art.fixedTex = Write("fixed_" + tag, ChiePlaceholderArt.BuildFixed(pose), false, true, false);
                    art.maskTex = Write("mask_" + tag, ChiePlaceholderArt.BuildMask(pose), false, true, true);
                }
                poseArts.Add(art);
            }

            Texture2D brush = Write("brush", ChiePlaceholderArt.BuildBrush(), true, true, false);
            Texture2D dim = Write("chie_dim", ChiePlaceholderArt.BuildDim(), true, true, false);
            Texture2D icoBrush = Write("icon_brush", ChiePlaceholderArt.BuildIcon(ToolId.Brush), true, true, false);
            Texture2D icoEraser = Write("icon_eraser", ChiePlaceholderArt.BuildIcon(ToolId.Eraser), true, true, false);
            Texture2D icoDrop = Write("icon_eyedropper", ChiePlaceholderArt.BuildIcon(ToolId.Eyedropper), true, true, false);
            Texture2D icoDown = Write("icon_putdown", ChiePlaceholderArt.BuildIcon(ToolId.PutDown), true, true, false);

            Material paint = MakeMat("ChiePainting", "Numen/ChiePainting", null);
            Material brushMat = MakeMat("ChieBrush", "Numen/BrushStamp", brush);
            MakeMat("ChieErase", "Numen/BrushErase", brush);

            var cfg = AssetDatabase.LoadAssetAtPath<ChieConfig>(ConfigPath);
            bool created = false;
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<ChieConfig>();
                AssetDatabase.CreateAsset(cfg, ConfigPath);
                created = true;
            }

            var so = new SerializedObject(cfg);
            WritePoses(so, poseArts);
            WriteZones(so, BuildZones());
            so.FindProperty("_brushTex").objectReferenceValue = brush;
            so.FindProperty("_dimTex").objectReferenceValue = dim;
            so.FindProperty("_iconBrush").objectReferenceValue = icoBrush;
            so.FindProperty("_iconEraser").objectReferenceValue = icoEraser;
            so.FindProperty("_iconEyedropper").objectReferenceValue = icoDrop;
            so.FindProperty("_iconPutDown").objectReferenceValue = icoDown;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cfg);

            if (paint != null) Selection.activeObject = paint;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log((created ? "[Chie] 占位资源已生成：" : "[Chie] 占位资源已更新：") + ArtDir);
        }

        private static FlawZone[] BuildZones()
        {
            var list = new List<FlawZone>();
            for (int p = 0; p < Poses.Length; p++)
            {
                for (int i = 0; i < ChieZonesP.Order.Length; i++)
                {
                    FlawId id = ChieZonesP.Order[i];
                    list.Add(new FlawZone
                    {
                        id = id,
                        pose = Poses[p],
                        rect = ChieZonesP.NormRect(id, Poses[p],
                            ChiePlaceholderArt.W, ChiePlaceholderArt.H),
                    });
                }
            }
            return list.ToArray();
        }

        private static void WritePoses(SerializedObject so, List<PoseArt> poses)
        {
            SerializedProperty arr = so.FindProperty("_poses");
            arr.arraySize = poses.Count;
            for (int i = 0; i < poses.Count; i++)
            {
                SerializedProperty e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("pose").enumValueIndex = (int)poses[i].pose;
                e.FindPropertyRelative("baseTex").objectReferenceValue = poses[i].baseTex;
                e.FindPropertyRelative("fixedTex").objectReferenceValue = poses[i].fixedTex;
                e.FindPropertyRelative("maskTex").objectReferenceValue = poses[i].maskTex;
            }
        }

        private static void WriteZones(SerializedObject so, FlawZone[] zones)
        {
            SerializedProperty arr = so.FindProperty("_zones");
            arr.arraySize = zones.Length;
            for (int i = 0; i < zones.Length; i++)
            {
                SerializedProperty e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").enumValueIndex = (int)zones[i].id;
                e.FindPropertyRelative("pose").enumValueIndex = (int)zones[i].pose;
                e.FindPropertyRelative("rect").vector4Value = zones[i].rect;
            }
        }

        private static Material MakeMat(string name, string shaderName, Texture2D shape)
        {
            string path = MatDir + "/" + name + ".mat";
            Shader s = Shader.Find(shaderName);
            if (s == null)
            {
                Debug.LogError("[Chie] 找不到 shader：" + shaderName);
                return null;
            }
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(s);
                AssetDatabase.CreateAsset(m, path);
            }
            else
            {
                m.shader = s;
            }
            if (shape != null) m.SetTexture("_Shape", shape);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Texture2D Write(string name, Texture2D tex, bool alpha, bool readable, bool point)
        {
            string path = ArtDir + "/" + name + ".png";
            byte[] png = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            File.WriteAllBytes(path, png);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = !point;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = alpha;
                ti.isReadable = readable;
                ti.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.maxTextureSize = 2048;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>导入已存在的 PNG（正式美术走这条路），只设导入参数不重绘。</summary>
        private static Texture2D Import(string name, bool alpha, bool readable, bool point)
        {
            string path = ArtDir + "/" + name + ".png";
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Default;
                ti.sRGBTexture = !point;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = alpha;
                ti.isReadable = readable;
                ti.filterMode = point ? FilterMode.Point : FilterMode.Bilinear;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.maxTextureSize = 2048;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void EnsureDir(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureDir(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
