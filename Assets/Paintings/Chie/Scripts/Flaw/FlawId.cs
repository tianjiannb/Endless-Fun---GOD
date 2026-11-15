namespace Numen.Chie
{
    /// <summary>六处破绽。顺序与 mask 六色编码一致（id 1..6 → 0..5）。</summary>
    public enum FlawId
    {
        Mouth = 0,
        Tip = 1,
        Window = 2,
        Face = 3,
        Dust = 4,
        Shadow = 5,
    }

    /// <summary>四个姿态。</summary>
    public enum PoseId
    {
        Sit = 0,
        Shift = 1,
        Up = 2,
        Stand = 3,
    }

    /// <summary>工具栏四件东西。</summary>
    public enum ToolId
    {
        Brush = 0,
        Eraser = 1,
        Eyedropper = 2,
        PutDown = 3,
    }

    /// <summary>流程状态。</summary>
    public enum FlowState
    {
        Boot = 0,
        Opening = 1,
        Edit = 2,
        Hold = 3,
        Ending = 4,
        Finale = 5,
        Exit = 6,
    }

    /// <summary>破绽常量。</summary>
    public static class FlawInfo
    {
        /// <summary>破绽总数。</summary>
        public const int Count = 6;

        /// <summary>该处是否必须先吸色才能计入覆盖。</summary>
        public static bool NeedDip(FlawId id) { return id == FlawId.Mouth; }

        /// <summary>该处对应需要的工具。</summary>
        public static ToolId ToolOf(FlawId id)
        {
            if (id == FlawId.Window || id == FlawId.Dust) return ToolId.Eraser;
            return ToolId.Brush;
        }

        /// <summary>中文名，用于调试与提示。</summary>
        public static string NameOf(FlawId id)
        {
            switch (id)
            {
                case FlawId.Mouth: return "嘴角";
                case FlawId.Tip: return "笔尖断了";
                case FlawId.Window: return "窗外是白";
                case FlawId.Face: return "五官太淡";
                case FlawId.Dust: return "窗台灰";
                default: return "影子";
            }
        }
    }
}
