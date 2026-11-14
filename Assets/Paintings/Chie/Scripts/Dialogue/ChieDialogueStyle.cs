using TMPro;
using UnityEngine;

namespace Numen.Chie
{
    /// <summary>说话人配色与字形样式，集中一处便于对齐规范 §10.5。</summary>
    public static class ChieDialogueStyle
    {
        /// <summary>说话人样式。</summary>
        public struct Style
        {
            /// <summary>显示名（空 = 不显示）。</summary>
            public string name;
            /// <summary>名字颜色。</summary>
            public Color nameColor;
            /// <summary>正文字色。</summary>
            public Color bodyColor;
            /// <summary>是否斜体。</summary>
            public bool italic;
            /// <summary>正文字号（0 = 用默认 32）。</summary>
            public float size;
        }

        private static readonly Style ChieStyle = new Style
        {
            name = "千绘",
            nameColor = new Color32(0xC9, 0x96, 0x3F, 0xFF),
            bodyColor = new Color32(0xF0, 0xE2, 0xC8, 0xFF),
            italic = false,
            size = 0f
        };

        private static readonly Style NaronStyle = new Style
        {
            name = "乃隆",
            nameColor = new Color32(0x7F, 0xA8, 0xBF, 0xFF),
            bodyColor = new Color32(0xC3, 0xD4, 0xDD, 0xFF),
            italic = false,
            size = 0f
        };

        private static readonly Style SysStyle = new Style
        {
            name = "",
            nameColor = new Color32(0x9A, 0x8F, 0x80, 0xFF),
            bodyColor = new Color32(0x9A, 0x8F, 0x80, 0xFF),
            italic = true,
            size = 30f
        };

        private static readonly Style ShadowStyle = new Style
        {
            name = "影子",
            nameColor = new Color32(0x9A, 0x86, 0xC9, 0xFF),
            bodyColor = new Color32(0xBC, 0xAD, 0xDB, 0xFF),
            italic = false,
            size = 0f
        };

        private static readonly Style FxStyle = new Style
        {
            name = "",
            nameColor = new Color32(0xC9, 0x96, 0x3F, 0xFF),
            bodyColor = new Color32(0xD8, 0xC8, 0xAB, 0xFF),
            italic = true,
            size = 0f
        };

        private static readonly Style MoveStyle = new Style
        {
            name = "",
            nameColor = new Color32(0x8A, 0xAD, 0x72, 0xFF),
            bodyColor = new Color32(0xC2, 0xD3, 0xB4, 0xFF),
            italic = true,
            size = 0f
        };

        /// <summary>按 who 取样式；未知走旁白样式。</summary>
        public static Style Get(string who)
        {
            if (string.IsNullOrEmpty(who)) return SysStyle;
            switch (who)
            {
                case "chie": return ChieStyle;
                case "naron": return NaronStyle;
                case "shadow": return ShadowStyle;
                case "fx": return FxStyle;
                case "move": return MoveStyle;
                default: return SysStyle;
            }
        }

        /// <summary>把斜体应用到 TMP 文本。</summary>
        public static void ApplyItalic(TextMeshProUGUI t, bool italic)
        {
            if (t == null) return;
            t.fontStyle = italic ? FontStyles.Italic : FontStyles.Normal;
        }
    }
}
