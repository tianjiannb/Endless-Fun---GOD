using UnityEngine;

namespace Numen.Chie.EditorTools
{
    /// <summary>占位美术用的极小画布：直接写 Color 像素，越界自动丢弃，免得到处 clamp。</summary>
    internal sealed class ChiePlaceholderCanvas
    {
        private const int W = ChiePlaceholderArt.W;
        private const int H = ChiePlaceholderArt.H;

        public readonly Color[] Px = new Color[W * H];

        public void Set(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Px[y * W + x] = c;
        }

        public void Blend(int x, int y, Color c, float a)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Px[y * W + x] = Color.Lerp(Px[y * W + x], c, Mathf.Clamp01(a));
        }

        public void Rect(int x0, int y0, int w, int h, Color c)
        {
            for (int y = y0; y < y0 + h; y++)
                for (int x = x0; x < x0 + w; x++) Set(x, y, c);
        }

        public void SoftEllipse(float cx, float cy, float rx, float ry, Color c, float a)
        {
            int x0 = Mathf.FloorToInt(cx - rx), x1 = Mathf.CeilToInt(cx + rx);
            int y0 = Mathf.FloorToInt(cy - ry), y1 = Mathf.CeilToInt(cy + ry);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float dx = (x - cx) / Mathf.Max(1f, rx), dy = (y - cy) / Mathf.Max(1f, ry);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    if (d > 1.05f) continue;
                    float k = 1f - Mathf.SmoothStep(0.92f, 1.05f, d);
                    Blend(x, y, c, k * a);
                }
            }
        }

        public void Line(Vector2 a, Vector2 b, float thick, Color c) { Line(a, b, thick, c, 1f); }

        public void Line(Vector2 a, Vector2 b, float thick, Color c, float alpha)
        {
            float r = thick * 0.5f;
            float len = Vector2.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(len));
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, (float)i / steps);
                SoftEllipse(p.x, p.y, r, r, c, alpha);
            }
        }
    }
}
