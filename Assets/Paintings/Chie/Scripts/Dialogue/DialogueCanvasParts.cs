using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Numen.Chie
{
    /// <summary>画内对话搭好之后的引用集合（从 InCanvasDialogueView 拆出来）。</summary>
    public class DialogueCanvasParts
    {
        /// <summary>整块画布的透明度。</summary>
        public CanvasGroup group;
        /// <summary>说话人。</summary>
        public TextMeshProUGUI speaker;
        /// <summary>正文。</summary>
        public TextMeshProUGUI body;
        /// <summary>底部渐晕。</summary>
        public Image dim;
        /// <summary>三行选项。</summary>
        public ChieOptionLine[] options;
        /// <summary>选项根节点。</summary>
        public RectTransform choiceRoot;
    }
}
