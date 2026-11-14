using UnityEngine;

namespace Numen.Chie
{
    /// <summary>笔迹闸门：只让"该露的、该工具的、颜色对了的"笔迹记进覆盖。</summary>
    public class StrokeGate
    {
        private readonly ChieConfig _config;
        private readonly FlawJudge _judge;
        private readonly WaveUnlock _wave;

        private Color _dipped;
        private bool _hasDip;

        /// <summary>注入依赖。</summary>
        public StrokeGate(ChieConfig config, FlawJudge judge, WaveUnlock wave)
        {
            _config = config;
            _judge = judge;
            _wave = wave;
        }

        /// <summary>是否已经吸过色。</summary>
        public bool HasDip { get { return _hasDip; } }

        /// <summary>吸到（或上次吸到）的颜色。</summary>
        public Color Dipped { get { return _dipped; } }

        /// <summary>清空吸色（切姿态 / 换工具时用）。</summary>
        public void Reset() { _hasDip = false; _dipped = Color.clear; }

        private static Color TargetOf(ChieConfig cfg, FlawId id)
        {
            return id == FlawId.Shadow ? cfg.DressTargetColor : cfg.LipTargetColor;
        }

        /// <summary>吸管取样：从当前姿态底图取一点。返回是否吸到可用颜色。</summary>
        public bool Dip(Vector2 px)
        {
            if (_config == null || _judge == null) return false;
            PoseArt art = _config.GetPose(_judge.Pose);
            Texture2D tex = art != null ? art.baseTex : null;
            if (tex == null) return false;

            Color c;
            try
            {
                int x = Mathf.Clamp(Mathf.FloorToInt(px.x), 0, tex.width - 1);
                int y = Mathf.Clamp(Mathf.FloorToInt(px.y), 0, tex.height - 1);
                c = tex.GetPixel(x, y);
            }
            catch (UnityException)
            {
                Debug.LogWarning("[Chie] 底图没有开 Read/Write，吸管取不到颜色");
                return false;
            }
            _dipped = c;
            _hasDip = true;
            return ColorOk(FlawId.Mouth) || ColorOk(FlawId.Shadow);
        }

        /// <summary>吸到的颜色是否落在该处的目标色容差内。</summary>
        public bool ColorOk(FlawId id)
        {
            if (!_hasDip || _config == null) return false;
            Color t = TargetOf(_config, id);
            float tol = _config.ColorTolerance;
            return Mathf.Abs(_dipped.r - t.r) <= tol
                && Mathf.Abs(_dipped.g - t.g) <= tol
                && Mathf.Abs(_dipped.b - t.b) <= tol;
        }

        /// <summary>该处此刻是否允许被这一笔画到。</summary>
        public bool Allow(FlawId id, ToolId tool)
        {
            if (_wave == null || !_wave.IsRevealed(id)) return false;
            if (FlawInfo.ToolOf(id) != tool) return false;
            if (FlawInfo.NeedDip(id) && !ColorOk(id)) return false;
            return true;
        }
    }
}
