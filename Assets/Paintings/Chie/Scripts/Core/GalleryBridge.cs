using UnityEngine;

namespace Numen.Core
{
    /// <summary>画廊侧要实现的两个动作；画作只认这个接口，不引用画廊类型。</summary>
    public interface IChieGallery
    {
        /// <summary>进入画作（可记录状态，不必做场景切换）。</summary>
        void Enter(string paintingId);

        /// <summary>画作结束，回画廊并带回一个选择结果。</summary>
        void Exit(string paintingId, string choiceId, string outcome);
    }

    /// <summary>依赖倒置出口：单独跑画2 时无实现，合并进主工程后由画廊注册。</summary>
    public static class GalleryBridge
    {
        private static IChieGallery _gallery;

        /// <summary>画廊启动时注册。</summary>
        public static void Register(IChieGallery gallery) { _gallery = gallery; }

        /// <summary>测试用：清掉注册。</summary>
        public static void Reset() { _gallery = null; }

        /// <summary>是否已接上画廊。</summary>
        public static bool HasGallery { get { return _gallery != null; } }

        public static void Enter(string paintingId)
        {
            if (_gallery != null) _gallery.Enter(paintingId);
        }

        /// <summary>回画廊；没有实现时留在本场景并告警。</summary>
        public static void Exit(string paintingId, string choiceId, string outcome)
        {
            if (_gallery != null)
            {
                _gallery.Exit(paintingId, choiceId, outcome);
                return;
            }
            Debug.LogWarning("[Chie] 未找到 Scene_Gallery，留在本场景。请检查 Build Settings。");
        }
    }
}
