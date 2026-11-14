using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Numen.Chie.QA
{
    /// <summary>验收报告：逐条结论 + 玩家日志镜像 + 落盘。</summary>
    public class ChieQaReport
    {
        private class Item
        {
            public string id;
            public string title;
            public string detail;
            public bool pass;
            public float at;
        }

        private readonly List<Item> _items = new List<Item>(64);
        private readonly List<string> _logs = new List<string>(128);
        private readonly List<string> _notes = new List<string>(32);

        private string _mode = "full";
        private string _path = "";
        private float _t0;

        /// <summary>通过数。</summary>
        public int PassCount { get; private set; }
        /// <summary>失败数。</summary>
        public int FailCount { get; private set; }

        /// <summary>开始一次验收。</summary>
        public void Begin(string mode, string path)
        {
            _mode = mode;
            _path = path;
            _t0 = Time.realtimeSinceStartup;
            Application.logMessageReceived += HandleLog;
            Debug.Log("[Chie][QA] 启动 mode=" + mode + " report=" + path);
        }

        /// <summary>记一条通过。</summary>
        public void Pass(string id, string title, string detail)
        {
            Add(id, title, detail, true);
        }

        /// <summary>记一条失败。</summary>
        public void Fail(string id, string title, string detail)
        {
            Add(id, title, detail, false);
        }

        /// <summary>记一条附加信息（不计分）。</summary>
        public void Note(string text)
        {
            if (text == null) text = "";
            _notes.Add(text);
            Debug.Log("[Chie][QA] NOTE " + text);
        }

        private void Add(string id, string title, string detail, bool pass)
        {
            var it = new Item
            {
                id = id,
                title = title,
                detail = detail != null ? detail : "",
                pass = pass,
                at = Time.realtimeSinceStartup - _t0
            };
            _items.Add(it);
            if (pass) PassCount++;
            else FailCount++;
            Debug.Log("[Chie][QA] " + (pass ? "PASS " : "FAIL ") + id + " " + title
                + (it.detail.Length > 0 ? " | " + it.detail : ""));
        }

        /// <summary>收玩家日志（只留警告与错误）。</summary>
        public void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Warning && type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            if (_logs.Count >= 200) return;
            _logs.Add("[" + type + "] " + condition);
        }

        /// <summary>日志里是否出现过某个片段。</summary>
        public bool SawLog(string needle)
        {
            if (string.IsNullOrEmpty(needle)) return false;
            for (int i = 0; i < _logs.Count; i++)
            {
                string s = _logs[i];
                if (s != null && s.IndexOf(needle, StringComparison.Ordinal) >= 0) return true;
            }
            return false;
        }

        /// <summary>取最后一条包含片段的日志。</summary>
        public string LastLogOf(string needle)
        {
            for (int i = _logs.Count - 1; i >= 0; i--)
            {
                string s = _logs[i];
                if (s != null && s.IndexOf(needle, StringComparison.Ordinal) >= 0) return s;
            }
            return "";
        }

        /// <summary>报告正文。</summary>
        public string BuildText()
        {
            var sb = new StringBuilder(4096);
            sb.AppendLine("Chie QA Report");
            sb.AppendLine("mode   : " + _mode);
            sb.AppendLine("report : " + _path);
            sb.AppendLine("unity  : " + Application.unityVersion + "  editor=" + Application.isEditor);
            sb.AppendLine("time   : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("result : " + (FailCount == 0 ? "PASS" : "FAIL") + "  pass=" + PassCount + " fail=" + FailCount);
            sb.AppendLine();
            sb.AppendLine("== 检查项 ==");
            for (int i = 0; i < _items.Count; i++)
            {
                Item it = _items[i];
                sb.Append("[").Append(it.pass ? "PASS" : "FAIL").Append("] ")
                  .Append(it.id).Append(' ').Append(it.title)
                  .Append("  @").Append(it.at.ToString("0.00")).Append("s");
                if (it.detail.Length > 0) sb.Append("  -- ").Append(it.detail);
                sb.AppendLine();
            }
            AppendSpec(sb);
            if (_notes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("== 附注 ==");
                for (int i = 0; i < _notes.Count; i++) sb.AppendLine(_notes[i]);
            }
            if (_logs.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("== 玩家日志（警告/错误） ==");
                for (int i = 0; i < _logs.Count; i++) sb.AppendLine(_logs[i]);
            }
            return sb.ToString();
        }

        /// <summary>规范 §15 里可运行时判定的项，按检查项 id 归组汇总。</summary>
        private void AppendSpec(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("== 规范 §15 运行时判定汇总 ==");
            Spec(sb, "三波随修复数解锁", "W");
            Spec(sb, "姿态随波次切换（Sit→Shift→Up）", "A08");
            Spec(sb, "工具栏解锁顺序（橡皮 Wave2 / 吸管 Wave3）", "A06");
            Spec(sb, "吸管取色命中目标色", "D");
            Spec(sb, "橡皮修 Window/Dust", "F2", "F3");
            Spec(sb, "Hold 入口：六处修满", "H01");
            Spec(sb, "Hold 入口：放下笔", "P0");
            Spec(sb, "Reveal 顺序", "H04");
            Spec(sb, "结局分歧按修复数开放", "E02");
            Spec(sb, "结局文本与数据一致", "E03");
            Spec(sb, "终幕台词与淡出", "E08", "E09");
            Spec(sb, "Exit 黑场与缺 Scene_Gallery 警告", "X02", "X03");
        }

        private void Spec(StringBuilder sb, string title, params string[] prefixes)
        {
            int pass = 0, fail = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                Item it = _items[i];
                if (it.id == null) continue;
                for (int j = 0; j < prefixes.Length; j++)
                {
                    if (it.id.StartsWith(prefixes[j], StringComparison.Ordinal))
                    {
                        if (it.pass) pass++; else fail++;
                        break;
                    }
                }
            }
            string st = fail > 0 ? "FAIL" : (pass > 0 ? "PASS" : "未执行");
            sb.Append("[").Append(st).Append("] ").Append(title)
              .Append("  (pass=").Append(pass).Append(" fail=").Append(fail).AppendLine(")");
        }

        /// <summary>写盘并解绑日志；返回实际路径。</summary>
        public string Flush()
        {
            Application.logMessageReceived -= HandleLog;
            string text = BuildText();
            try
            {
                string dir = Path.GetDirectoryName(_path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_path, text, new UTF8Encoding(false));
                Debug.Log("[Chie][QA] 报告已写入 " + _path);
            }
            catch (Exception e)
            {
                Debug.LogError("[Chie][QA] 报告写入失败：" + e.Message);
                return null;
            }
            return _path;
        }
    }
}
