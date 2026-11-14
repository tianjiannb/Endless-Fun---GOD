using UnityEngine;

namespace Numen.Chie.EditorTools
{
    /// <summary>程序化占位美术：同一套画法出「好画」与「坏画」，两张只差六处区域。</summary>
    public static class ChiePlaceholderArt
    {
        public const int W = 1920;
        public const int H = 1080;

        private static readonly Color Skin = new Color(0.93f, 0.82f, 0.74f);
        private static readonly Color HairC = new Color(0.15f, 0.12f, 0.13f);
        private static readonly Color Dress = new Color(0.42f, 0.46f, 0.55f);
        private static readonly Color Ink = new Color(0.24f, 0.20f, 0.18f);
        private static readonly Color Frame = new Color(0.30f, 0.23f, 0.18f);
        private static readonly Color Shelf = new Color(0.36f, 0.27f, 0.20f);
        private static readonly Color Lip = new Color(0.72f, 0.32f, 0.30f);
        private static readonly Color WallLo = new Color(0.09f, 0.08f, 0.09f);
        private static readonly Color WallHi = new Color(0.18f, 0.17f, 0.19f);

        /// <summary>墙色：按高度渐变，抠掉影子时用它盖回去。</summary>
        public static Color WallAt(int y)
        {
            float t = Mathf.Clamp01(y / (float)(H - 1));
            return Color.Lerp(WallLo, WallHi, t);
        }

        /// <summary>好画。</summary>
        public static Texture2D BuildFixed(PoseId pose) { return ToTex(DrawCv(pose, true), "fixed_" + pose); }

        /// <summary>底色图：好画，但六处按区域换成坏画内容（区域外逐像素一致）。</summary>
        public static Texture2D BuildBase(PoseId pose)
        {
            Color[] good = DrawCv(pose, true);
            Color[] bad = DrawCv(pose, false);
            for (int i = 0; i < ChieZonesP.PaintOrder.Length; i++)
            {
                RectInt r = ChieZonesP.PixelRect(ChieZonesP.PaintOrder[i], pose, W, H);
                for (int y = r.yMin; y < r.yMax; y++)
                    for (int x = r.xMin; x < r.xMax; x++)
                        if (x >= 0 && y >= 0 && x < W && y < H) good[y * W + x] = bad[y * W + x];
            }
            return ToTex(good, "base_" + pose);
        }

        /// <summary>六色 mask，按 PaintOrder 铺，后铺的盖前面的。</summary>
        public static Texture2D BuildMask(PoseId pose)
        {
            var px = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(0, 0, 0, 255);
            for (int i = 0; i < ChieZonesP.PaintOrder.Length; i++)
            {
                FlawId id = ChieZonesP.PaintOrder[i];
                RectInt r = ChieZonesP.PixelRect(id, pose, W, H);
                Color32 c = ChieZonesP.MaskColor(id);
                for (int y = r.yMin; y < r.yMax; y++)
                {
                    if (y < 0 || y >= H) continue;
                    for (int x = r.xMin; x < r.xMax; x++)
                    {
                        if (x < 0 || x >= W) continue;
                        px[y * W + x] = c;
                    }
                }
            }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>软边圆形笔刷 128×128。</summary>
        public static Texture2D BuildBrush()
        {
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / c;
                    float a = 1f - Mathf.SmoothStep(0.55f, 1f, d);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>底部深、上部透的暗底 4×256。</summary>
        public static Texture2D BuildDim()
        {
            var tex = new Texture2D(4, 256, TextureFormat.RGBA32, false);
            var px = new Color32[4 * 256];
            for (int y = 0; y < 256; y++)
            {
                float t = 1f - y / 255f;                  // 0 顶 1 底
                float a = Mathf.Lerp(0f, 0.78f, Mathf.Pow(t, 1.4f));
                var c = new Color32(0x0A, 0x08, 0x06, (byte)Mathf.RoundToInt(a * 255f));
                for (int x = 0; x < 4; x++) px[y * 4 + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>工具图标：白线稿 96×96。</summary>
        public static Texture2D BuildIcon(ToolId tool)
        {
            const int n = 96;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            var clear = new Color32(255, 255, 255, 0);
            var white = new Color32(0xF2, 0xE8, 0xD8, 0xFF);
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            System.Action<int, int> dot = (x, y) =>
            {
                if (x < 0 || y < 0 || x >= n || y >= n) return;
                px[y * n + x] = white;
            };
            System.Action<int, int, int, int> seg = (x0, y0, x1, y1) =>
            {
                int dx = Mathf.Abs(x1 - x0), dy = Mathf.Abs(y1 - y0);
                int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx - dy;
                int x = x0, y = y0, guard = 0;
                while (guard++ < 4096)
                {
                    dot(x, y); dot(x + 1, y); dot(x, y + 1);
                    if (x == x1 && y == y1) break;
                    int e2 = err * 2;
                    if (e2 > -dy) { err -= dy; x += sx; }
                    if (e2 < dx) { err += dx; y += sy; }
                }
            };

            switch (tool)
            {
                case ToolId.Brush:
                    seg(24, 20, 60, 56);
                    seg(26, 18, 62, 54);
                    seg(60, 56, 76, 72);
                    seg(62, 54, 78, 70);
                    break;
                case ToolId.Eraser:
                    seg(26, 34, 60, 68);
                    seg(34, 26, 68, 60);
                    seg(26, 34, 34, 26);
                    seg(60, 68, 68, 60);
                    seg(66, 62, 74, 70);
                    break;
                case ToolId.Eyedropper:
                    seg(26, 70, 56, 40);
                    seg(30, 74, 60, 44);
                    seg(56, 40, 70, 26);
                    seg(60, 44, 74, 30);
                    dot(74, 22); dot(75, 23); dot(73, 21);
                    break;
                default:
                    seg(24, 34, 58, 68);
                    seg(58, 68, 74, 68);
                    seg(30, 26, 26, 62);
                    seg(26, 62, 62, 66);
                    break;
            }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // ── 画法 ────────────────────────────────────────────────
        private static Texture2D ToTex(Color[] px, string name)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.name = name;
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static Color[] DrawCv(PoseId pose, bool good)
        {
            var cv = new ChiePlaceholderCanvas();
            Vector2 off = ChieZonesP.PoseOffset(pose);
            int ox = Mathf.RoundToInt(off.x), oy = Mathf.RoundToInt(off.y);

            for (int y = 0; y < H; y++)
            {
                Color w = WallAt(y);
                for (int x = 0; x < W; x++) cv.Set(x, y, w);
            }

            RectInt win = ChieZonesP.PixelRect(FlawId.Window, pose, W, H);
            cv.Rect(win.xMin - 18, win.yMin - 18, win.width + 36, win.height + 36, Frame);
            if (good)
            {
                for (int y = win.yMin; y < win.yMax; y++)
                {
                    float t = Mathf.Clamp01((y - win.yMin) / (float)Mathf.Max(1, win.height));
                    Color sky = Color.Lerp(new Color(0.88f, 0.62f, 0.44f), new Color(0.26f, 0.38f, 0.58f), t);
                    for (int x = win.xMin; x < win.xMax; x++) cv.Set(x, y, sky);
                }
                int hillTop = win.yMin + Mathf.RoundToInt(win.height * 0.30f);
                for (int x = win.xMin; x < win.xMax; x++)
                {
                    float u = (x - win.xMin) / (float)Mathf.Max(1, win.width);
                    int top = hillTop + Mathf.RoundToInt(Mathf.Sin(u * 3.1f) * win.height * 0.07f);
                    for (int y = win.yMin; y < top; y++) cv.Set(x, y, new Color(0.13f, 0.16f, 0.20f));
                }
                cv.Line(new Vector2(win.xMin, win.yMin + win.height * 0.55f),
                        new Vector2(win.xMax, win.yMin + win.height * 0.58f), 2f, new Color(0.95f, 0.85f, 0.62f));
            }
            else
            {
                for (int y = win.yMin; y < win.yMax; y++)
                    for (int x = win.xMin; x < win.xMax; x++) cv.Set(x, y, new Color(0.95f, 0.95f, 0.94f));
            }

            RectInt dust = ChieZonesP.PixelRect(FlawId.Dust, pose, W, H);
            cv.Rect(dust.xMin, dust.yMin, dust.width, dust.height, Shelf);
            cv.Rect(dust.xMin, dust.yMax - 6, dust.width, 6, new Color(0.46f, 0.35f, 0.26f));
            if (!good)
            {
                for (int y = dust.yMin; y < dust.yMax; y++)
                    for (int x = dust.xMin; x < dust.xMax; x++)
                        cv.Blend(x, y, new Color(0.62f, 0.61f, 0.58f), 0.55f);
            }

            if (good) cv.SoftEllipse(624 + ox, 850 + oy, 190f, 74f, new Color(0.04f, 0.035f, 0.04f), 0.55f);

            cv.SoftEllipse(787 + ox, 578 + oy, 152f, 152f, HairC, 1f);
            cv.Rect(768 + ox, 470 + oy, 40, 60, Skin);
            for (int y = 380; y < 500; y++)
            {
                float t = (y - 380) / 120f;
                int half = Mathf.RoundToInt(Mathf.Lerp(150f, 88f, t));
                cv.Rect(787 + ox - half, y + oy, half * 2, 1, Dress);
            }
            cv.Rect(712 + ox, 470 + oy, 150, 78, Dress);
            cv.SoftEllipse(787 + ox, 470 + oy, 128f, 128f, Skin, 1f);

            float fa = good ? 1f : 0.14f;
            cv.SoftEllipse(750 + ox, 496 + oy, 15f, 9f, Ink, fa);
            cv.SoftEllipse(826 + ox, 496 + oy, 15f, 9f, Ink, fa);
            cv.Line(new Vector2(734 + ox, 520 + oy), new Vector2(766 + ox, 524 + oy), 3f, Ink, fa);
            cv.Line(new Vector2(810 + ox, 524 + oy), new Vector2(842 + ox, 520 + oy), 3f, Ink, fa);
            // 唇色必须两种画法都有：StrokeGate.Dip 只从当前姿态的 base 取色，
            // 坏画若完全没唇色，①的 needDip 永远满足不了。坏画的嘴画成歪斜过厚的一撇加一坨红印，
            // 形状明显是错的（仍要玩家重画），但颜色就是 Lip，吸管一吸即中。
            if (good) cv.SoftEllipse(787 + ox, 430 + oy, 26f, 11f, Lip, 1f);
            else
            {
                cv.Line(new Vector2(748 + ox, 440 + oy), new Vector2(788 + ox, 418 + oy), 11f, Lip);
                cv.Line(new Vector2(788 + ox, 418 + oy), new Vector2(828 + ox, 446 + oy), 11f, Lip);
                cv.SoftEllipse(816 + ox, 436 + oy, 13f, 8f, Lip, 1f);
            }

            cv.Line(new Vector2(838 + ox, 486 + oy), new Vector2(930 + ox, 690 + oy), 26f, Skin, 1f);
            cv.SoftEllipse(936 + ox, 700 + oy, 26f, 26f, Skin, 1f);
            cv.Line(new Vector2(936 + ox, 694 + oy), new Vector2(994 + ox, 768 + oy), 9f, Ink, 1f);
            if (good) cv.SoftEllipse(1008 + ox, 780 + oy, 20f, 30f, Ink, 1f);

            return cv.Px;
        }
    }
}
