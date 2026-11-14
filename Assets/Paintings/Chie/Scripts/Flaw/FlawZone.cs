using UnityEngine;

namespace Numen.Chie
{
    /// <summary>一处破绽在画面里的归一化区域（左下原点）。</summary>
    [System.Serializable]
    public class FlawZone
    {
        public FlawId id;
        public Vector4 rect;
        public PoseId pose;
    }
}
