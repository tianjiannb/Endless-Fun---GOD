using System;
using UnityEngine;

namespace Numen.Chie
{
    /// <summary>覆盖率判定：只吃覆盖格点，不认识 RenderTexture。</summary>
    public class FlawJudge : MonoBehaviour
    {
        private ChieConfig _config;
        private FlawMaskGrid[] _masks;      // 按 PoseId 索引
        private bool[] _cover;              // 跨姿态保留
        private readonly bool[] _restored = new bool[FlawInfo.Count];

        /// <summary>某处刚被修好（参数：该处）。</summary>
        public event Action<FlawId> OnFlawRestored;

        /// <summary>已修好数量。</summary>
        public int RestoredCount { get; private set; }

        /// <summary>当前姿态网格。</summary>
        public PoseId Pose { get; private set; }

        /// <summary>四姿态 mask 全部建好。Boot 阶段调用一次。</summary>
        public void Build(ChieConfig config)
        {
            _config = config;
            int poseCount = 4;
            _masks = new FlawMaskGrid[poseCount];
            for (int i = 0; i < poseCount; i++)
            {
                var art = config != null ? config.GetPose((PoseId)i) : null;
                Texture2D mask = art != null ? art.maskTex : null;
                if (mask == null) Debug.LogError("[Chie] 缺少 mask，判定空转：pose " + i);
                _masks[i] = FlawMaskGrid.Build(mask, config != null ? config.CellPixels : 10);
            }
            int cells = _masks[0].GridW * _masks[0].GridH;
            _cover = new bool[cells];
            RestoredCount = 0;
            for (int i = 0; i < _restored.Length; i++) _restored[i] = false;
            Pose = PoseId.Sit;
        }

        /// <summary>切姿态：换网格，已累积覆盖不清空。</summary>
        public void SwitchPose(PoseId pose)
        {
            Pose = pose;
        }

        /// <summary>该处是否已修好（永久锁定）。</summary>
        public bool IsRestored(FlawId id)
        {
            int i = (int)id;
            return i >= 0 && i < _restored.Length && _restored[i];
        }

        /// <summary>返回该处覆盖率 0..1。</summary>
        public float Coverage(FlawId id)
        {
            if (_masks == null) return 0f;
            FlawMaskGrid g = _masks[(int)Pose];
            if (g == null) return 0f;
            int total = g.Area(id);
            if (total == 0) return 0f;
            int hit = 0;
            for (int y = 0; y < g.GridH; y++)
            {
                for (int x = 0; x < g.GridW; x++)
                {
                    if (!_cover[y * g.GridW + x]) continue;
                    if (g.FlawAt(x, y) == (int)id) hit++;
                }
            }
            return (float)hit / total;
        }

        /// <summary>把一段笔迹覆盖到的格标记为已覆盖（不过滤，任何格都算）。</summary>
        public void MarkCoverage(Vector2 fromPx, Vector2 toPx, float radiusPx)
        {
            MarkCoverage(fromPx, toPx, radiusPx, null);
        }

        /// <summary>只把 allow 放行的破绽格标记为已覆盖；allow 为 null 时不过滤。</summary>
        public void MarkCoverage(Vector2 fromPx, Vector2 toPx, float radiusPx, Predicate<FlawId> allow)
        {
            if (_masks == null || _cover == null || _config == null) return;
            FlawMaskGrid g = _masks[(int)Pose];
            if (g == null) return;

            float cell = _config.CellPixels;
            float r = radiusPx;
            float len = Vector2.Distance(fromPx, toPx);
            int steps = Mathf.Max(1, Mathf.CeilToInt(len / Mathf.Max(1f, cell * 0.5f)));
            for (int s = 0; s <= steps; s++)
            {
                float t = steps == 0 ? 0f : (float)s / steps;
                Vector2 p = Vector2.Lerp(fromPx, toPx, t);
                int x0 = Mathf.FloorToInt((p.x - r) / cell);
                int x1 = Mathf.FloorToInt((p.x + r) / cell);
                int y0 = Mathf.FloorToInt((p.y - r) / cell);
                int y1 = Mathf.FloorToInt((p.y + r) / cell);
                for (int y = y0; y <= y1; y++)
                {
                    if (y < 0 || y >= g.GridH) continue;
                    for (int x = x0; x <= x1; x++)
                    {
                        if (x < 0 || x >= g.GridW) continue;
                        if (allow != null)
                        {
                            int flaw = g.FlawAt(x, y);
                            if (flaw < 0) continue;
                            if (!allow((FlawId)flaw)) continue;
                        }
                        _cover[y * g.GridW + x] = true;
                    }
                }
            }
        }

        /// <summary>松笔时结算，返回本次新修好的处数。</summary>
        public int Evaluate()
        {
            if (_config == null) return 0;
            int newly = 0;
            for (int i = 0; i < FlawInfo.Count; i++)
            {
                if (_restored[i]) continue;
                var id = (FlawId)i;
                if (Coverage(id) < _config.CoverThreshold) continue;
                _restored[i] = true;
                RestoredCount++;
                newly++;
                if (OnFlawRestored != null) OnFlawRestored(id);
            }
            return newly;
        }

        /// <summary>调试用：某处当前覆盖率。</summary>
        public float CoverageRatio(FlawId id) { return Coverage(id); }
    }
}
