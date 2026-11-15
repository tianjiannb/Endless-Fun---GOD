using UnityEngine;

namespace Numen.Chie.EditorTools
{
    /// <summary>六处区域：分层美术下人物随身、背景固定，按姿态各自成表（左上原点像素）。</summary>
    public static class ChieZonesP
    {
        /// <summary>参考分辨率，所有像素矩形都以它为准。</summary>
        public const int RefW = 1920;
        public const int RefH = 1080;

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

        /// <summary>
        /// 某姿态下某处的像素矩形（左上原点）。Mouth/Face 跟着人物层走，其余四处属于背景、四姿态相同。
        /// 与 tools/make_art.py 的 HEAD / FACE / BG 表逐项对应，改一处必须同步改另一处。
        /// </summary>
        public static RectInt RawRect(FlawId id, PoseId pose)
        {
            switch (id)
            {
                case FlawId.Mouth:
                    if (pose == PoseId.Shift) return new RectInt(766, 478, 26, 20);
                    if (pose == PoseId.Up) return new RectInt(758, 448, 26, 20);
                    if (pose == PoseId.Stand) return new RectInt(836, 240, 32, 18);
                    return new RectInt(744, 478, 26, 20);
                case FlawId.Face:
                    if (pose == PoseId.Shift) return new RectInt(710, 392, 94, 134);
                    if (pose == PoseId.Up) return new RectInt(702, 362, 94, 134);
                    if (pose == PoseId.Stand) return new RectInt(760, 170, 100, 100);
                    return new RectInt(688, 392, 94, 134);
                case FlawId.Tip: return new RectInt(450, 700, 160, 120);
                case FlawId.Window: return new RectInt(748, 0, 540, 718);
                case FlawId.Dust: return new RectInt(766, 718, 520, 58);
                default: return new RectInt(150, 790, 300, 290);
            }
        }

        /// <summary>人物层位移（像素，Unity 左下原点）。分层后只有人物动，背景不动。</summary>
        public static Vector2 PoseOffset(PoseId pose)
        {
            switch (pose)
            {
                case PoseId.Shift: return new Vector2(22f, 0f);
                case PoseId.Up: return new Vector2(14f, 30f);
                default: return Vector2.zero;
            }
        }

        /// <summary>某姿态下某处的像素矩形（按目标贴图尺寸等比缩放，并裁到贴图内）。</summary>
        public static RectInt PixelRect(FlawId id, PoseId pose, int w, int h)
        {
            RectInt r = RawRect(id, pose);
            float sx = w / (float)RefW;
            float sy = h / (float)RefH;
            int x = Mathf.RoundToInt(r.x * sx);
            int y = Mathf.RoundToInt(r.y * sy);
            int rw = Mathf.RoundToInt(r.width * sx);
            int rh = Mathf.RoundToInt(r.height * sy);
            if (x < 0) { rw += x; x = 0; }
            if (y < 0) { rh += y; y = 0; }
            if (x + rw > w) rw = w - x;
            if (y + rh > h) rh = h - y;
            return new RectInt(x, y, Mathf.Max(1, rw), Mathf.Max(1, rh));
        }

        /// <summary>某姿态下某处的归一化矩形（左下原点，写进 ChieConfig）。</summary>
        public static Vector4 NormRect(FlawId id, PoseId pose, int w, int h)
        {
            RectInt p = PixelRect(id, pose, w, h);
            float x = p.x / (float)w;
            float y = (h - (p.y + p.height)) / (float)h;
            return new Vector4(x, y, p.width / (float)w, p.height / (float)h);
        }

        /// <summary>mask 上色顺序（① 最后，避免嘴角被五官盖掉）。</summary>
        public static readonly FlawId[] PaintOrder =
        {
            FlawId.Window, FlawId.Dust, FlawId.Face, FlawId.Tip, FlawId.Mouth, FlawId.Shadow
        };
    }
}
