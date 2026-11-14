using System;
using UnityEngine;

namespace EndlessFun.UI
{
    /// <summary>A bundled Chinese font keeps UI readable on machines without CJK system fonts.</summary>
    public static class GameUiFont
    {
        private static Font chinese;

        public static Font Load()
        {
            if (chinese != null) return chinese;
            chinese = Resources.Load<Font>("Fonts/NotoSansCJKsc-Regular");
            if (chinese == null) throw new InvalidOperationException("缺少项目中文字体，请检查 Fonts/NotoSansCJKsc-Regular。");
            return chinese;
        }
    }
}
