using UnityEngine;

namespace Numen.Chie.EditorTools
{
    /// <summary>六处区域：sit 归一化矩形来自规范 §7.4，其它姿态按整幅平移。</summary>
    public static class ChieZonesP
    {
        /// <summary>按 mask 颜色表顺序的六处。</summary>
        public static readonly FlawId[] Order =
        {
            FlawId.Mouth, FlawId.Tip, FlawId.Window, FlawId.Face, FlawId.Dust, FlawId.Shadow
        };

        /// <summary>mask 里六处的纯色（r+2g+4b = id）。</summary>
        public static Color32 MaskColor(FlawId id)
        {
            switch (id)
            {
                case FlawId.Mouth: return new Color32(255, 0, 0, 255);     // 1 红
                case FlawId.Tip: return new Color32(0, 255, 0, 255);       // 2 绿
                case FlawId.Face: return new Color32(255, 255, 0, 255);    // 3 黄
                case FlawId.Window: return new Color32(0, 0, 255, 255);    // 4 蓝
                case FlawId.Dust: return new Color32(255, 0, 255, 255);    // 5 品红
                default: return new Color32(0, 255, 255, 255);             // 6 青
            }
        }

        /// <summary>sit 姿态的归一化区域（左下原点）。</summary>
        public static Vector4 SitRect(FlawId id)
        {
            switch (id)
            {
                case FlawId.Mouth: return new Vector4(0.366f, 0.455f, 0.078f, 0.060f);
                case FlawId.Tip: return new Vector4(0.430f, 0.640f, 0.165f, 0.130f);
                case FlawId.Window: return new Vector4(0.700f, 0.190f, 0.240f, 0.320f);
                case FlawId.Face: return new Vector4(0.345f, 0.335f, 0.125f, 0.225f);
                case FlawId.Dust: return new Vector4(0.693f, 0.596f, 0.255f, 0.080f);
                default: return new Vector4(0.230f, 0.730f, 0.190f, 0.115f);
            }
        }

        /// <summary>相对 sit 的整幅位移（像素）。</summary>
        public static Vector2 PoseOffset(PoseId pose)
        {
            if (pose == PoseId.Shift) return new Vector2(30f, 0f);
            if (pose == PoseId.Up) return new Vector2(30f, 40f);
            if (pose == PoseId.Stand) return new Vector2(30f, 90f);
            return Vector2.zero;
        }

        /// <summary>某姿态下某处的像素矩形。</summary>
        public static RectInt PixelRect(FlawId id, PoseId pose, int w, int h)
        {
            Vector4 r = SitRect(id);
            Vector2 off = PoseOffset(pose);
            int x = Mathf.RoundToInt(r.x * w + off.x);
            int y = Mathf.RoundToInt(r.y * h + off.y);
            int rw = Mathf.RoundToInt(r.z * w);
            int rh = Mathf.RoundToInt(r.w * h);
            if (x < 0) { rw += x; x = 0; }
            if (y < 0) { rh += y; y = 0; }
            if (x + rw > w) rw = w - x;
            if (y + rh > h) rh = h - y;
            return new RectInt(x, y, Mathf.Max(1, rw), Mathf.Max(1, rh));
        }

        /// <summary>某姿态下某处的归一化矩形（用于写进 ChieConfig）。</summary>
        public static Vector4 NormRect(FlawId id, PoseId pose, int w, int h)
        {
            Vector4 r = SitRect(id);
            Vector2 off = PoseOffset(pose);
            return new Vector4(r.x + off.x / w, r.y + off.y / h, r.z, r.w);
        }

        /// <summary>mask 上色顺序（④ 先、① 后，避免嘴角被五官盖掉）。</summary>
        public static readonly FlawId[] PaintOrder =
        {
            FlawId.Window, FlawId.Dust, FlawId.Face, FlawId.Shadow, FlawId.Tip, FlawId.Mouth
        };
    }
}
