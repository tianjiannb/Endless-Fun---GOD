using System.Collections.Generic;
using UnityEngine;

namespace Numen.Chie
{
    /// <summary>
    /// 未修好的地方用虚线呼吸圈出来：周期 1.2s，alpha 0.26..0.60。
    /// 顶点一律在「本物体自身」的本地空间生成：先按画布本地像素算六处矩形，再经画布变换映射回本物体本地空间，
    /// 所以父级位移/缩放、姿态整幅偏移（shift +30px、up +40px、stand +90px）、画布 PPU 变化都不会让虚线跑偏。
    /// </summary>
    public class ChieMarkerOverlay : MonoBehaviour
    {
        private const float DashPx = 14f;        // 一截虚线的长（画布像素）
        private const float GapPx = 10f;         // 断口长（画布像素）
        private const float ThickPx = 2.5f;      // 线宽（画布像素）
        private const float LocalZ = -0.05f;     // 比画布更靠近相机，同 z 也不打架
        private const float MinGeometryEps = 0.0001f;

        /// <summary>顶点 UV：材质贴的是 1×1 白图，取中心点最稳（Clamp 下不会采到边界）。</summary>
        private static readonly Vector3 QuadUv = new Vector3(0.5f, 0.5f, 0f);

        private ChieConfig _config;
        private FlawJudge _judge;
        private WaveUnlock _wave;
        private Transform _canvas;
        private Mesh _mesh;
        private Material _mat;
        private bool _visible = true;
        private bool _built;
        private float _t;
        private byte _alphaByte = 255;
        private int _signature = int.MinValue;
        private Color32 _tint = new Color32(255, 255, 255, 255);

        private readonly List<Vector3> _verts = new List<Vector3>(1024);
        private readonly List<Vector3> _uvs = new List<Vector3>(1024);
        private readonly List<int> _tris = new List<int>(1536);
        private readonly List<Color32> _cols = new List<Color32>(1024);

        /// <summary>当前是否显示（流程用它开关虚线）。</summary>
        public bool Visible { get { return _visible; } }

        /// <summary>建出网格与材质（Boot 阶段）。</summary>
        public void Init(ChieConfig config, FlawJudge judge, WaveUnlock wave)
        {
            _config = config;
            _judge = judge;
            _wave = wave;

            var mf = gameObject.GetComponent<MeshFilter>();
            if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
            var mr = gameObject.GetComponent<MeshRenderer>();
            if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();

            _mesh = new Mesh { name = "ChieMarkers" };
            _mesh.MarkDynamic();
            mf.sharedMesh = _mesh;

            _mat = MakeMaterial();
            mr.sharedMaterial = _mat;
            mr.sortingOrder = 10;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            _canvas = FindCanvas();
            transform.localPosition = new Vector3(0f, 0f, LocalZ);
            _signature = int.MinValue;
        }

        /// <summary>显示 / 隐藏全部虚线。</summary>
        public void SetVisible(bool visible)
        {
            if (_visible == visible) return;
            _visible = visible;
            _signature = int.MinValue;
            if (!visible) Clear();
        }

        private void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
            if (_mat != null) Destroy(_mat);
        }

        private void LateUpdate()
        {
            if (!_visible || _config == null || _judge == null || _mesh == null)
            {
                Clear();
                return;
            }

            _t += Time.deltaTime;
            float period = Mathf.Max(0.01f, _config.BreathPeriod);
            float k = Mathf.Sin(_t / period * Mathf.PI * 2f) * 0.5f + 0.5f;
            SetAlphaByte(_config.BreathMinAlpha, _config.BreathMaxAlpha, k);

            int sig = Signature();
            if (sig == _signature && _built) return;
            _signature = sig;
            Build();
        }

        /// <summary>呼吸：把 alpha 量化成 0..255，只有跨过一档才重写顶点色。</summary>
        private void SetAlphaByte(float minAlpha, float maxAlpha, float k)
        {
            float a = Mathf.Lerp(minAlpha, maxAlpha, k);
            byte b = (byte)Mathf.Clamp(Mathf.RoundToInt(a * 255f), 0, 255);
            if (b == _alphaByte) return;
            _alphaByte = b;
            _tint = new Color32(255, 255, 255, b);
            if (_cols.Count == 0) return;                 // 还没建网格，Build 会用新 alpha
            for (int i = 0; i < _cols.Count; i++) _cols[i] = _tint;
            if (_mesh != null && _mesh.vertexCount > 0) _mesh.SetColors(_cols);
        }

        /// <summary>姿态、已修状态、波次任一变就重建几何。</summary>
        private int Signature()
        {
            int s = ((int)_judge.Pose + 1) * 17;
            if (_wave != null) s = s * 31 + _wave.Wave;
            for (int i = 0; i < FlawInfo.Count; i++)
                if (_judge.IsRestored((FlawId)i)) s |= 1 << (8 + i);
            return s;
        }

        private void Clear()
        {
            if (_mesh != null && _mesh.vertexCount > 0) _mesh.Clear();
            _built = false;
            _signature = int.MinValue;
        }

        private void Build()
        {
            _verts.Clear();
            _uvs.Clear();
            _tris.Clear();
            _cols.Clear();
            _tint = new Color32(255, 255, 255, _alphaByte);

            PoseId pose = _judge.Pose;
            for (int i = 0; i < FlawInfo.Count; i++)
            {
                var id = (FlawId)i;
                if (_judge.IsRestored(id)) continue;                 // 修好的不再圈
                if (_wave != null && !_wave.IsRevealed(id)) continue; // 还没露出来的不圈
                Vector4 r;
                if (!_config.TryGetZone(id, pose, out r)) continue;
                AddRect(r);
            }

            if (_verts.Count == 0) { Clear(); return; }

            _mesh.Clear();
            _mesh.SetVertices(_verts);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetColors(_cols);
            _mesh.SetTriangles(_tris, 0);
            _mesh.RecalculateBounds();
            _built = true;
        }

        /// <summary>沿一个归一化矩形的四条边铺虚线（归一化坐标：左下原点）。</summary>
        private void AddRect(Vector4 r)
        {
            float w = Mathf.Max(0.0001f, _config.WorldWidth);
            float h = Mathf.Max(0.0001f, _config.WorldHeight);
            float x0 = (r.x - 0.5f) * w;
            float y0 = (r.y - 0.5f) * h;
            float x1 = (r.x + r.z - 0.5f) * w;
            float y1 = (r.y + r.w - 0.5f) * h;

            float ppu = Mathf.Max(1f, _config.PixelsPerUnit);
            float dash = DashPx / ppu;
            float gap = GapPx / ppu;
            float thick = ThickPx / ppu;

            DashLine(new Vector2(x0, y0), new Vector2(x1, y0), dash, gap, thick);
            DashLine(new Vector2(x1, y0), new Vector2(x1, y1), dash, gap, thick);
            DashLine(new Vector2(x1, y1), new Vector2(x0, y1), dash, gap, thick);
            DashLine(new Vector2(x0, y1), new Vector2(x0, y0), dash, gap, thick);
        }

        private void DashLine(Vector2 a, Vector2 b, float dash, float gap, float thick)
        {
            Vector2 dir = b - a;
            float len = dir.magnitude;
            if (len <= MinGeometryEps) return;
            dir /= len;
            Vector2 n = new Vector2(-dir.y, dir.x) * (thick * 0.5f);
            float step = Mathf.Max(0.0001f, dash + gap);

            for (float s = 0f; s < len; s += step)
            {
                float e = Mathf.Min(s + dash, len);
                Vector2 p0 = a + dir * s;
                Vector2 p1 = a + dir * e;
                AddQuad(p0 - n, p0 + n, p1 + n, p1 - n);
            }
        }

        private void AddQuad(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            int i = _verts.Count;
            _verts.Add(ToLocal(a));
            _verts.Add(ToLocal(b));
            _verts.Add(ToLocal(c));
            _verts.Add(ToLocal(d));
            _uvs.Add(QuadUv); _uvs.Add(QuadUv); _uvs.Add(QuadUv); _uvs.Add(QuadUv);
            _cols.Add(_tint); _cols.Add(_tint); _cols.Add(_tint); _cols.Add(_tint);
            _tris.Add(i); _tris.Add(i + 1); _tris.Add(i + 2);
            _tris.Add(i); _tris.Add(i + 2); _tris.Add(i + 3);
        }

        /// <summary>画布本地坐标 → 本物体的本地坐标（画布与本物体是兄弟，父级可能带位移/缩放）。</summary>
        private Vector3 ToLocal(Vector2 canvasLocal)
        {
            if (_canvas == null) return new Vector3(canvasLocal.x, canvasLocal.y, 0f);
            Vector3 world = _canvas.TransformPoint(new Vector3(canvasLocal.x, canvasLocal.y, 0f));
            return transform.InverseTransformPoint(world);
        }

        /// <summary>找到画布（PaintingQuad）的变换；找不到就用父级空间 + 配置尺寸兜底。</summary>
        private Transform FindCanvas()
        {
            Transform p = transform.parent;
            if (p == null) return null;
            for (int i = 0; i < p.childCount; i++)
            {
                Transform c = p.GetChild(i);
                if (c == null || c == transform) continue;
                if (c.GetComponent<SpriteRenderer>() == null) continue;
                if (c.GetComponent<PaintingView>() == null) continue;
                return c;
            }
            return null;
        }

        /// <summary>虚线材质：优先 Sprite 默认 shader（URP 下按 SRPDefaultUnlit 通道绘制），逐级兜底。</summary>
        private static Material MakeMaterial()
        {
            Shader s = Shader.Find("Sprites/Default");
            if (s == null) s = Shader.Find("Unlit/Transparent");
            if (s == null) s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("UI/Default");
            if (s == null)
            {
                Debug.LogError("[Chie] 虚线材质找不到可用 shader，标记将不可见");
                return null;
            }

            var m = new Material(s) { name = "ChieMarkers" };
            m.mainTexture = Texture2D.whiteTexture;
            m.color = Color.white;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", Texture2D.whiteTexture);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            return m;
        }
    }
}
