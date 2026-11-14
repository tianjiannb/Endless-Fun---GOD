using UnityEngine;

namespace Numen.Chie
{
    /// <summary>全屏黑场：结局退出前把画面压掉，再换场景。</summary>
    public class ChieFade : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Color _color = new Color(0f, 0f, 0f, 0f);

        /// <summary>按配置建出覆盖整个相机的黑场。</summary>
        public void Init(ChieConfig config)
        {
            _sr = gameObject.GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

            float w = config != null ? config.WorldWidth : 19.2f;
            float h = config != null ? config.WorldHeight : 10.8f;
            Texture2D tex = Texture2D.whiteTexture;
            _sr.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 1f);
            _sr.sortingOrder = 100;
            _sr.color = _color;

            transform.localPosition = new Vector3(0f, 0f, -0.2f);
            transform.localScale = new Vector3(w * 1.2f, h * 1.2f, 1f);
        }

        /// <summary>直接设透明度 0..1。</summary>
        public void SetAlpha(float a)
        {
            _color.a = Mathf.Clamp01(a);
            if (_sr != null) _sr.color = _color;
        }

        /// <summary>当前透明度。</summary>
        public float Alpha { get { return _color.a; } }
    }
}
