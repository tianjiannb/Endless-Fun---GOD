using System;
using UnityEngine;

namespace Numen.Chie
{
    /// <summary>三波推进：由她动了才露，不是计数解锁。</summary>
    public class WaveUnlock : MonoBehaviour
    {
        private FlawJudge _judge;
        private ChieConfig _config;

        /// <summary>当前波（1..3）。</summary>
        public int Wave { get; private set; }

        /// <summary>波次变化（参数：新波）。</summary>
        public event Action<int> OnWaveChanged;

        /// <summary>六处全修好。</summary>
        public event Action OnAllRestored;

        private bool _allFired;

        /// <summary>注入依赖。</summary>
        public void Init(ChieConfig config, FlawJudge judge)
        {
            _config = config;
            _judge = judge;
            Wave = 1;
        }

        /// <summary>某处刚被修好后查一次阈值（只在首次跨过时触发）。</summary>
        public void Evaluate(int restoredCount)
        {
            if (_config == null) return;

            if (Wave < 2 && restoredCount >= 1)
            {
                Wave = 2;
                if (OnWaveChanged != null) OnWaveChanged(Wave);
            }
            if (Wave < 3 && restoredCount >= 2)
            {
                Wave = 3;
                if (OnWaveChanged != null) OnWaveChanged(Wave);
            }
            if (!_allFired && restoredCount >= _config.AllOpenAt)
            {
                _allFired = true;
                if (OnAllRestored != null) OnAllRestored();
            }
        }

        /// <summary>该处当前是否已露出来（可供涂抹与判定）。</summary>
        public bool IsRevealed(FlawId id)
        {
            switch (id)
            {
                case FlawId.Tip:
                case FlawId.Face:
                    return Wave >= 1;
                case FlawId.Dust:
                case FlawId.Window:
                    return Wave >= 2;
                default:
                    return Wave >= 3;
            }
        }

        /// <summary>当前工具是否已解锁。</summary>
        public bool IsToolUnlocked(ToolId tool)
        {
            if (tool == ToolId.Brush || tool == ToolId.PutDown) return true;
            if (tool == ToolId.Eraser) return Wave >= 2;
            if (tool == ToolId.Eyedropper) return Wave >= 3;
            return false;
        }
    }
}
