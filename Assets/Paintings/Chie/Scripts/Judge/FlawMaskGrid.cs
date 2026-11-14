using UnityEngine;

namespace Numen.Chie
{
    /// <summary>把六色 mask 降采样成网格，并给出每处的总格数。</summary>
    public class FlawMaskGrid
    {
        private readonly int _gridW;
        private readonly int _gridH;
        private readonly int _cell;

        private readonly sbyte[] _grid;   // 扁平化；-1 = 不属于任何破绽
        private readonly int[] _area = new int[FlawInfo.Count];

        /// <summary>网格宽（格数）。</summary>
        public int GridW { get { return _gridW; } }
        /// <summary>网格高（格数）。</summary>
        public int GridH { get { return _gridH; } }

        private FlawMaskGrid(int gridW, int gridH, int cell)
        {
            _gridW = gridW;
            _gridH = gridH;
            _cell = cell;
            _grid = new sbyte[gridW * gridH];
            for (int i = 0; i < _grid.Length; i++) _grid[i] = -1;
        }

        /// <summary>从一张 mask 贴图建网格；贴图不可读或为空时返回全 -1 的空网格。</summary>
        public static FlawMaskGrid Build(Texture2D mask, int cell)
        {
            int gw = mask != null ? mask.width / cell : 1;
            int gh = mask != null ? mask.height / cell : 1;
            var g = new FlawMaskGrid(Mathf.Max(1, gw), Mathf.Max(1, gh), cell);
            if (mask == null) return g;

            Color32[] px;
            try { px = mask.GetPixels32(); }
            catch (UnityException)
            {
                Debug.LogError("[Chie] mask 不可读，请勾 Read/Write Enabled：" + mask.name);
                return g;
            }
            if (px == null || px.Length == 0) return g;

            int mw = mask.width;
            for (int y = 0; y < g._gridH; y++)
            {
                int py = y * cell + cell / 2;
                if (py >= mask.height) py = mask.height - 1;
                for (int x = 0; x < g._gridW; x++)
                {
                    int pxx = x * cell + cell / 2;
                    if (pxx >= mw) pxx = mw - 1;
                    Color32 c = px[py * mw + pxx];
                    int id = (c.r >= 128 ? 1 : 0) + (c.g >= 128 ? 2 : 0) + (c.b >= 128 ? 4 : 0);
                    int idx = FlawOfColorId(id);
                    if (idx < 0 || idx >= FlawInfo.Count) continue;
                    g._grid[y * g._gridW + x] = (sbyte)idx;
                    g._area[idx]++;
                }
            }
            return g;
        }

        /// <summary>六色 id（r+2g+4b）→ 破绽下标，按规范 §7.3 的颜色表。</summary>
        public static int FlawOfColorId(int colorId)
        {
            switch (colorId)
            {
                case 1: return (int)FlawId.Mouth;    // 红
                case 2: return (int)FlawId.Tip;      // 绿
                case 4: return (int)FlawId.Window;   // 蓝
                case 3: return (int)FlawId.Face;     // 黄
                case 5: return (int)FlawId.Dust;     // 品红
                case 6: return (int)FlawId.Shadow;   // 青
                default: return -1;
            }
        }

        /// <summary>该格属于哪处破绽；-1 表示无。</summary>
        public int FlawAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _gridW || y >= _gridH) return -1;
            return _grid[y * _gridW + x];
        }

        /// <summary>该处的总格数。</summary>
        public int Area(FlawId id)
        {
            int i = (int)id;
            if (i < 0 || i >= FlawInfo.Count) return 0;
            return _area[i];
        }
    }
}
