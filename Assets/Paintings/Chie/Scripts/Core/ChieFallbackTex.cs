using UnityEngine;

namespace Numen.Chie
{
    /// <summary>缺资源时的替代纹理（Unity 没有内置洋红，这里自己造一张）。</summary>
    public static class ChieFallbackTex
    {
        private static Texture2D _magenta;

        public static Texture2D Magenta
        {
            get
            {
                if (_magenta == null)
                {
                    _magenta = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    _magenta.name = "chie_magenta";
                    _magenta.SetPixels(new[] { Color.magenta, Color.magenta, Color.magenta, Color.magenta });
                    _magenta.Apply();
                }
                return _magenta;
            }
        }
    }
}
