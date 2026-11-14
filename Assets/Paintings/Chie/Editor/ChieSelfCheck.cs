using System.Text;
using Numen.Chie;
using UnityEditor;
using UnityEngine;

namespace Numen.Chie.EditorTools
{
    /// <summary>Tools/Chie/自检：校验占位资源、判据网格、对话数据。</summary>
    public static class ChieSelfCheck
    {
        private const string ConfigPath = "Assets/Paintings/Chie/Resources/Chie/ChieConfig.asset";

        /// <summary>菜单入口。</summary>
        [MenuItem("Tools/Chie/自检资源与数据")]
        public static void RunMenu() { RunBatch(); }

        /// <summary>批处理入口。</summary>
        public static void RunBatch()
        {
            var sb = new StringBuilder();
            int fail = 0;

            var cfg = AssetDatabase.LoadAssetAtPath<ChieConfig>(ConfigPath);
            if (cfg == null) { Debug.LogError("[Chie][自检] 找不到 ChieConfig"); return; }

            sb.AppendLine("[Chie][自检] 画布 " + cfg.CanvasWidth + "x" + cfg.CanvasHeight
                + " 网格 " + cfg.GridW + "x" + cfg.GridH + " 阈值 " + cfg.CoverThreshold);

            foreach (PoseId pose in new[] { PoseId.Sit, PoseId.Shift, PoseId.Up, PoseId.Stand })
            {
                PoseArt art = cfg.GetPose(pose);
                if (art == null) { sb.AppendLine("  姿态 " + pose + " 缺图组"); fail++; continue; }
                if (art.baseTex == null || art.fixedTex == null || art.maskTex == null)
                { sb.AppendLine("  姿态 " + pose + " 贴图缺失"); fail++; continue; }
                if (art.baseTex.width != cfg.CanvasWidth || art.baseTex.height != cfg.CanvasHeight)
                { sb.AppendLine("  姿态 " + pose + " base 尺寸不对"); fail++; }
                if (art.maskTex.width != cfg.CanvasWidth || art.maskTex.height != cfg.CanvasHeight)
                { sb.AppendLine("  姿态 " + pose + " mask 尺寸不对"); fail++; }

                FlawMaskGrid grid = FlawMaskGrid.Build(art.maskTex, cfg.CellPixels);
                var line = new StringBuilder("  姿态 " + pose + " 格数:");
                for (int i = 0; i < FlawInfo.Count; i++)
                {
                    FlawId id = (FlawId)i;
                    int a = grid.Area(id);
                    line.Append(" " + id + "=" + a);
                }
                sb.AppendLine(line.ToString());

                int total = 0;
                for (int i = 0; i < FlawInfo.Count; i++) total += grid.Area((FlawId)i);
                if (total == 0) { sb.AppendLine("    该姿态 mask 全空"); fail++; }

                // base 与 fixed 只在破绽区域内不同
                int diffOutside = CountDiffOutside(art, cfg);
                sb.AppendLine("    base/fixed 区域外像素差: " + diffOutside);
                if (diffOutside > 0) fail++;
            }

            // 对话数据
            DialogueData data = DialogueData.Load(cfg.DialogueResourcePath);
            if (data == null) { sb.AppendLine("  对话数据加载失败"); fail++; }
            else
            {
                sb.AppendLine("  对话节点 " + (data.nodes != null ? data.nodes.Length : 0)
                    + " 破绽台词 " + (data.flaws != null ? data.flaws.Length : 0));
                if (data.nodes != null)
                    for (int i = 0; i < data.nodes.Length; i++)
                    {
                        DialogueNode n = data.nodes[i];
                        if (n.options == null) continue;
                        for (int j = 0; j < n.options.Length; j++)
                        {
                            DialogueOption o = n.options[j];
                            if (o == null || o.reply == null || o.reply.Length == 0)
                            { sb.AppendLine("    选项回应为空: " + n.id + "#" + j); fail++; }
                            else
                            {
                                for (int k = 0; k < o.reply.Length; k++)
                                    if (o.reply[k] == null || string.IsNullOrEmpty(o.reply[k].t))
                                    { sb.AppendLine("    空台词: " + n.id + "#" + j + "." + k); fail++; }
                            }
                        }
                    }
                string[] need = { "ask_1", "after_ask_1", "ask_2", "after_ask_2",
                                  "wave_2_move", "wave_3_move", "ask_3", "ask_end" };
                for (int i = 0; i < need.Length; i++)
                {
                    if (FindNode(data, need[i]) == null) { sb.AppendLine("    缺节点: " + need[i]); fail++; }
                }
                if (data.opening == null || data.opening.Length == 0) { sb.AppendLine("    缺开场台词"); fail++; }
                if (data.standAll == null || data.standAll.Length == 0) { sb.AppendLine("    缺 standAll"); fail++; }
                if (data.standPart == null || data.standPart.Length == 0) { sb.AppendLine("    缺 standPart"); fail++; }
                if (data.finale == null || data.finale.Length == 0) { sb.AppendLine("    缺 finale"); fail++; }
            }

            if (cfg.Font == null) { sb.AppendLine("  ChieConfig 没有接字体"); fail++; }
            else sb.AppendLine("  字体: " + cfg.Font.name + " 图集 " + cfg.Font.atlasTextures.Length);

            sb.AppendLine(fail == 0 ? "[Chie][自检] 全部通过" : "[Chie][自检] " + fail + " 项不通过");
            Debug.Log(sb.ToString());
        }

        private static DialogueNode FindNode(DialogueData data, string id)
        {
            if (data == null || data.nodes == null) return null;
            for (int i = 0; i < data.nodes.Length; i++)
                if (data.nodes[i] != null && data.nodes[i].id == id) return data.nodes[i];
            return null;
        }

        private static int CountDiffOutside(PoseArt art, ChieConfig cfg)
        {
            Texture2D b = art.baseTex;
            Texture2D f = art.fixedTex;
            if (b.width != f.width || b.height != f.height) return -1;
            Color32[] pa, pb;
            try { pa = b.GetPixels32(); pb = f.GetPixels32(); }
            catch (UnityException) { return -1; }

            int w = b.width, h = b.height;
            int diff = 0;
            for (int y = 0; y < h; y += 2)
            {
                for (int x = 0; x < w; x += 2)
                {
                    if (InsideAnyZone(cfg, art.pose, x, y, w, h)) continue;
                    int i = y * w + x;
                    if (pa[i].r != pb[i].r || pa[i].g != pb[i].g || pa[i].b != pb[i].b) diff++;
                }
            }
            return diff;
        }

        private static bool InsideAnyZone(ChieConfig cfg, PoseId pose, int x, int y, int w, int h)
        {
            for (int i = 0; i < FlawInfo.Count; i++)
            {
                Vector4 r;
                if (!cfg.TryGetZone((FlawId)i, pose, out r)) continue;
                int x0 = Mathf.FloorToInt(r.x * w) - 2;
                int y0 = Mathf.FloorToInt(r.y * h) - 2;
                int x1 = Mathf.CeilToInt((r.x + r.z) * w) + 2;
                int y1 = Mathf.CeilToInt((r.y + r.w) * h) + 2;
                if (x >= x0 && x <= x1 && y >= y0 && y <= y1) return true;
            }
            return false;
        }
    }
}
