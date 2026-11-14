using Numen.Core;
using UnityEngine;

namespace Numen.Chie.Gallery
{
    /// <summary>画廊侧胶水：把画2 的进出接到主工程的 GameSession / PaintingEntry。</summary>
    /// <remarks>单独打开画2 工程时本文件不存在，GalleryBridge 无实现 → 走缺画廊告警分支。</remarks>
    public class ChieGalleryLink : IChieGallery
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Hook()
        {
            GalleryBridge.Register(new ChieGalleryLink());
        }

        /// <summary>进画由 PaintingEntry.Enter 切场景完成，这里只做记录。</summary>
        public void Enter(string paintingId)
        {
            if (GameSession.Instance != null) GameSession.Instance.BeginEnter(paintingId);
        }

        /// <summary>画2 已自己黑场，回画廊不再叠一层淡出。</summary>
        public void Exit(string paintingId, string choiceId, string outcome)
        {
            PaintingEntry.Exit(new ChoiceResult(paintingId, choiceId, outcome), false);
        }
    }
}
