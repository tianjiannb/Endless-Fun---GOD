using System;
using System.IO;
using UnityEngine;

namespace Numen.Chie.QA
{
    /// <summary>-chieqa 命令行开关：全流程验收 / 只走"放下笔"分支。</summary>
    public static class ChieQaArgs
    {
        /// <summary>开关本体。</summary>
        public const string Flag = "-chieqa";
        /// <summary>全流程（修满六处）。</summary>
        public const string ModeFull = "full";
        /// <summary>只走放下笔进 Hold。</summary>
        public const string ModeDrop = "drop";

        /// <summary>读命令行；返回 false 表示本次进程不是 QA 运行。</summary>
        public static bool TryRead(out string mode)
        {
            mode = null;
            string[] args = Environment.GetCommandLineArgs();
            if (args == null) return false;
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                if (string.IsNullOrEmpty(a)) continue;
                if (string.Equals(a, Flag, StringComparison.OrdinalIgnoreCase))
                {
                    mode = ModeFull;
                    return true;
                }
                if (a.StartsWith(Flag + "=", StringComparison.OrdinalIgnoreCase))
                {
                    string v = a.Substring(Flag.Length + 1).Trim().Trim('"');
                    mode = string.Equals(v, ModeDrop, StringComparison.OrdinalIgnoreCase) ? ModeDrop : ModeFull;
                    return true;
                }
            }
            return false;
        }

        /// <summary>报告路径：&lt;项目&gt;/Logs/qa_report.txt。</summary>
        public static string ReportPath()
        {
            string root = Application.dataPath;
            string dir = Path.GetFullPath(Path.Combine(root, "..", "Logs"));
            return Path.Combine(dir, "qa_report.txt");
        }
    }
}
