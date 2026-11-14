using System.Collections.Generic;
using UnityEngine;

namespace Numen.Chie.QA
{
    /// <summary>mask 格扫描：找目标格、找可吸色点、排横扫路线。</summary>
    public static class ChieQaMask
    {
        /// <summary>mask 六色编码（r+2g+4b）到破绽的映射，与 FlawMaskGrid 完全一致。</summary>
        public static int ColorId(FlawId id)
        {
            switch (id)
            {
                case FlawId.Mouth: return 1;
                case FlawId.Tip: return 2;
                case FlawId.Window: return 4;
                case FlawId.Face: return 3;
                case FlawId.Dust: return 5;
                case FlawId.Shadow: return 6;
                default: return 0;
            }
        }

        /// <summary>某姿态、某破绽的全部格中心（像素坐标，左下原点）。</summary>
        public static List<Vector2> Cells(ChieConfig cfg, PoseId pose, FlawId id)
        {
            var list = new List<Vector2>(256);
            if (cfg == null) return list;
            PoseArt art = cfg.GetPose(pose);
            Texture2D mask = art != null ? art.maskTex : null;
            if (mask == null) return list;
            Color32[] px;
            try { px = mask.GetPixels32(); }
            catch (UnityException) { return list; }
            if (px == null || px.Length == 0) return list;

            int cell = Mathf.Max(1, cfg.CellPixels);
            int mw = mask.width;
            int gw = mw / cell;
            int gh = mask.height / cell;
            int want = ColorId(id);
            for (int y = 0; y < gh; y++)
            {
                int py = y * cell + cell / 2;
                if (py >= mask.height) py = mask.height - 1;
                for (int x = 0; x < gw; x++)
                {
                    int pxx = x * cell + cell / 2;
                    if (pxx >= mw) pxx = mw - 1;
                    Color32 c = px[py * mw + pxx];
                    int cid = (c.r >= 128 ? 1 : 0) + (c.g >= 128 ? 2 : 0) + (c.b >= 128 ? 4 : 0);
                    if (cid != want) continue;
                    list.Add(new Vector2(pxx, py));
                }
            }
            return list;
        }

        /// <summary>把格中心按行归并成横线（每行一条，返回 [左端, 右端]）。</summary>
        public static List<Vector2[]> Rows(List<Vector2> cells, float rowH)
        {
            var rows = new List<Vector2[]>(32);
            if (cells == null || cells.Count == 0) return rows;
            if (rowH < 1f) rowH = 1f;

            var sorted = new List<Vector2>(cells);
            sorted.Sort(delegate (Vector2 a, Vector2 b)
            {
                if (a.y != b.y) return a.y.CompareTo(b.y);
                return a.x.CompareTo(b.x);
            });

            int start = 0;
            while (start < sorted.Count)
            {
                float y0 = sorted[start].y;
                float minX = sorted[start].x;
                float maxX = sorted[start].x;
                float ySum = 0f;
                int n = 0;
                int i = start;
                while (i < sorted.Count && sorted[i].y - y0 < rowH)
                {
                    if (sorted[i].x < minX) minX = sorted[i].x;
                    if (sorted[i].x > maxX) maxX = sorted[i].x;
                    ySum += sorted[i].y;
                    n++;
                    i++;
                }
                rows.Add(new[] { new Vector2(minX, ySum / Mathf.Max(1, n)), new Vector2(maxX, ySum / Mathf.Max(1, n)) });
                start = i;
            }
            return rows;
        }

        /// <summary>找一处能吸到目标色的点：先试本处的 mask 格，再按区域扫，最后全图稀疏扫。</summary>
        public static bool TryDip(ChieFlow flow, FlawId id, out Vector2 px, out string detail)
        {
            px = Vector2.zero;
            detail = "";
            if (flow == null) { detail = "flow 为空"; return false; }
            ChieConfig cfg = flow.QaConfig;
            FlawJudge judge = flow.QaJudge;
            StrokeGate gate = flow.QaGate;
            if (cfg == null || judge == null || gate == null) { detail = "依赖为空"; return false; }

            List<Vector2> cells = Cells(cfg, judge.Pose, id);
            Color target = id == FlawId.Shadow ? cfg.DressTargetColor : cfg.LipTargetColor;
            for (int i = 0; i < cells.Count; i++)
            {
                if (TryAt(flow, id, cells[i], out px)) { detail = "吸自 mask 格 " + i + "/" + cells.Count + " " + Summary(gate, target, cfg); return true; }
            }

            Vector4 zone;
            if (cfg.TryGetZone(id, judge.Pose, out zone))
            {
                float x0 = zone.x * cfg.CanvasWidth;
                float y0 = zone.y * cfg.CanvasHeight;
                float x1 = (zone.x + zone.z) * cfg.CanvasWidth;
                float y1 = (zone.y + zone.w) * cfg.CanvasHeight;
                for (float y = y0; y <= y1; y += 6f)
                {
                    for (float x = x0; x <= x1; x += 6f)
                    {
                        if (TryAt(flow, id, new Vector2(x, y), out px)) { detail = "吸自区域内 (" + x.ToString("0") + "," + y.ToString("0") + ") " + Summary(gate, target, cfg); return true; }
                    }
                }
            }

            for (float y = 8f; y < cfg.CanvasHeight; y += 12f)
            {
                for (float x = 8f; x < cfg.CanvasWidth; x += 12f)
                {
                    if (TryAt(flow, id, new Vector2(x, y), out px)) { detail = "吸自全图 (" + x.ToString("0") + "," + y.ToString("0") + ") " + Summary(gate, target, cfg); return true; }
                }
            }

            detail = "全图找不到目标色；最后一次取色=" + gate.Dipped + " 目标=" + target + " 容差=" + cfg.ColorTolerance;
            return false;
        }

        private static bool TryAt(ChieFlow flow, FlawId id, Vector2 p, out Vector2 px)
        {
            px = p;
            if (!flow.QaProbeDip(p)) return false;
            return flow.QaGate.ColorOk(id);
        }

        private static string Summary(StrokeGate gate, Color target, ChieConfig cfg)
        {
            return "取色=" + gate.Dipped + " 目标=" + target + " 容差=" + cfg.ColorTolerance;
        }

        /// <summary>格中心的包围盒（像素），用于报告。</summary>
        public static string Bounds(List<Vector2> cells)
        {
            if (cells == null || cells.Count == 0) return "无格";
            float minX = cells[0].x, maxX = cells[0].x, minY = cells[0].y, maxY = cells[0].y;
            for (int i = 1; i < cells.Count; i++)
            {
                Vector2 p = cells[i];
                if (p.x < minX) minX = p.x;
                if (p.x > maxX) maxX = p.x;
                if (p.y < minY) minY = p.y;
                if (p.y > maxY) maxY = p.y;
            }
            return "n=" + cells.Count + " x[" + minX.ToString("0") + "," + maxX.ToString("0") + "] y[" + minY.ToString("0") + "," + maxY.ToString("0") + "]";
        }
    }
}
