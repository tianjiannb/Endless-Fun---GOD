using UnityEngine;

namespace Numen.Chie
{
    /// <summary>一个姿态的三张图。</summary>
    [System.Serializable]
    public class PoseArt
    {
        public PoseId pose;
        public Texture2D baseTex;
        public Texture2D fixedTex;
        public Texture2D maskTex;
    }
}
