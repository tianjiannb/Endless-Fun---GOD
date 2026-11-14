using UnityEngine;

namespace Numen.Chie
{
    /// <summary>笔迹 RenderTexture 绘制；不认识 FlawId。</summary>
    public class BrushPainter : MonoBehaviour
    {
        private ChieConfig _config;
        private RenderTexture _brushRT;
        private RenderTexture _tempRT;
        private Material _matBrush;
        private Material _matErase;
        private int _blitsThisFrame;

        private Vector2 _strokeFrom;
        private Vector2 _strokeTo;
        private bool _hasStroke;
        private Vector2 _lastPx;
        private bool _drawing;

        /// <summary>一段笔迹（参数：起止像素、半径）。</summary>
        public event System.Action<Vector2, Vector2, float> OnStrokeSegment;

        /// <summary>笔迹 RT。</summary>
        public RenderTexture BrushRT { get { return _brushRT; } }

        /// <summary>当前工具。</summary>
        public ToolId Tool { get; set; }

        /// <summary>当前颜色是否有效（吸管取错色时为 false）。</summary>
        public bool ColorValid { get; set; } = true;

        /// <summary>是否正在画。</summary>
        public bool IsDrawing { get { return _drawing; } }

        /// <summary>建 RT 与两个笔刷材质（Boot 阶段）。</summary>
        public void Init(ChieConfig config, Material matBrush, Material matErase)
        {
            _config = config;
            _matBrush = matBrush;
            _matErase = matErase;
            Tool = ToolId.Brush;

            int w = config != null ? config.CanvasWidth : 1920;
            int h = config != null ? config.CanvasHeight : 1080;
            _brushRT = NewRT(w, h, false);
            _tempRT = NewRT(w, h, true);
            ClearBrush();
        }

        private static RenderTexture NewRT(int w, int h, bool noDepth)
        {
            var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Bilinear;
            rt.wrapMode = TextureWrapMode.Clamp;
            rt.useMipMap = false;
            rt.Create();
            return rt;
        }

        /// <summary>清空笔迹。</summary>
        public void ClearBrush()
        {
            if (_brushRT == null) return;
            var prev = RenderTexture.active;
            RenderTexture.active = _brushRT;
            GL.Clear(false, true, new Color(0f, 0f, 0f, 0f));
            RenderTexture.active = prev;
        }

        private void OnDestroy()
        {
            Release(ref _brushRT);
            Release(ref _tempRT);
        }

        private static void Release(ref RenderTexture rt)
        {
            if (rt == null) return;
            rt.Release();
            Destroy(rt);
            rt = null;
        }

        private void LateUpdate()
        {
            _blitsThisFrame = 0;
        }

        /// <summary>本帧的一笔：按下 / 拖动 / 抬起。</summary>
        public void Stroke(Vector2 px, bool down, bool held, bool up)
        {
            if (_brushRT == null || _config == null) return;

            if (down)
            {
                _drawing = true;
                _lastPx = px;
                Stamp(px, px);
                _hasStroke = true;
                _strokeFrom = px;
                _strokeTo = px;
            }
            else if (held && _drawing)
            {
                float dist = Vector2.Distance(_lastPx, px);
                float step = Mathf.Max(1f, _config.BrushStepPx);
                if (dist >= step)
                {
                    int seg = Mathf.Max(1, Mathf.Min(_config.MaxBlitsPerFrame, Mathf.FloorToInt(dist / step)));
                    for (int i = 0; i < seg; i++)
                    {
                        Vector2 a = Vector2.Lerp(_lastPx, px, (float)i / seg);
                        Vector2 b = Vector2.Lerp(_lastPx, px, (float)(i + 1) / seg);
                        Stamp(a, b);
                        _strokeFrom = a;
                        _strokeTo = b;
                        if (OnStrokeSegment != null) OnStrokeSegment(a, b, _config.BrushRadiusPx);
                    }
                    _lastPx = px;
                }
            }

            if (up && _drawing)
            {
                _drawing = false;
                if (_hasStroke)
                {
                    if (OnStrokeSegment != null) OnStrokeSegment(_strokeFrom, _strokeTo, _config.BrushRadiusPx);
                }
                _hasStroke = false;
            }
        }

        private void Stamp(Vector2 fromPx, Vector2 toPx)
        {
            if (_blitsThisFrame >= _config.MaxBlitsPerFrame) return;

            Material m = Tool == ToolId.Eraser ? _matErase : _matBrush;
            if (m == null) return;

            Vector2 uv = new Vector2(toPx.x / _config.CanvasWidth, toPx.y / _config.CanvasHeight);
            float rUv = _config.BrushRadiusPx / _config.CanvasWidth;
            float aspect = (float)_config.CanvasWidth / _config.CanvasHeight;
            m.SetVector("_Center", new Vector4(uv.x, uv.y, 0f, 0f));
            m.SetFloat("_Radius", Mathf.Max(0.0005f, rUv));
            m.SetFloat("_Aspect", aspect);
            m.SetFloat("_Value", 1f);

            Graphics.Blit(_brushRT, _tempRT, m);
            Graphics.Blit(_tempRT, _brushRT);
            _blitsThisFrame++;
        }
    }
}
