using UnityEngine;

namespace Numen.Chie
{
    /// <summary>鼠标 → 画布像素坐标的唯一入口。</summary>
    public class CanvasPointer : MonoBehaviour
    {
        private Camera _cam;
        private ChieConfig _config;

        /// <summary>是否允许涂抹（由 ChieFlow 与对话互斥控制）。</summary>
        public bool CanPaint { get; set; }

        /// <summary>当前指针是否在画布内。</summary>
        public bool InCanvas { get; private set; }

        /// <summary>当前画布像素坐标（出界时钳在边缘）。</summary>
        public Vector2 Pixel { get; private set; }

        /// <summary>本帧是否按下。</summary>
        public bool Down { get; private set; }

        /// <summary>本帧是否按住。</summary>
        public bool Held { get; private set; }

        /// <summary>本帧是否抬起。</summary>
        public bool Up { get; private set; }

        /// <summary>注入配置与相机（Boot 阶段，不再自己去抢 Camera.main）。</summary>
        public void Init(ChieConfig config, Camera cam)
        {
            _config = config;
            _cam = cam;
        }

        private void Update()
        {
            Down = Held = Up = false;
            InCanvas = false;
            if (!CanPaint || _cam == null || _config == null) return;

            // 按键状态与是否在画布内分开算：拖出画布也要能收到抬起，避免笔迹卡住
            Vector3 sp = Input.mousePosition;
            float depth = -_cam.transform.position.z;
            Vector3 wp = _cam.ScreenToWorldPoint(new Vector3(sp.x, sp.y, depth));

            float u = wp.x / _config.WorldWidth + 0.5f;
            float v = wp.y / _config.WorldHeight + 0.5f;
            InCanvas = u >= 0f && u <= 1f && v >= 0f && v <= 1f;

            float cu = Mathf.Clamp01(u);
            float cv = Mathf.Clamp01(v);
            Pixel = new Vector2(cu * _config.CanvasWidth, cv * _config.CanvasHeight);
            Down = Input.GetMouseButtonDown(0);
            Held = Input.GetMouseButton(0);
            Up = Input.GetMouseButtonUp(0);
        }

        /// <summary>把画布像素坐标转成 UV。</summary>
        public Vector2 PixelToUv(Vector2 px)
        {
            if (_config == null) return Vector2.zero;
            return new Vector2(px.x / _config.CanvasWidth, px.y / _config.CanvasHeight);
        }
    }
}
