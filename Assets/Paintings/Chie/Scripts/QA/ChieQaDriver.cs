using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Numen.Chie.QA
{
    /// <summary>-chieqa 自动演示/验收驱动器：真实 UI 点击 + 真实笔迹链路。</summary>
    public partial class ChieQaDriver : MonoBehaviour
    {
        private const float RunLimit = 300f;

        private ChieQaReport _report;
        private ChieFlow _flow;
        private string _mode = ChieQaArgs.ModeFull;
        private float _t0;
        private bool _done;

        private readonly List<string> _lines = new List<string>(128);
        private readonly List<float> _lineTimes = new List<float>(128);
        private readonly List<string> _choiceNodes = new List<string>(8);
        private readonly List<string> _choicePicks = new List<string>(8);

        private string _lastBody = "";
        private float _lastAdvance;
        private string _lastChoiceKey = "";
        private float _choiceAt;
        private int _pickCursor;
        private float _editDoneAt = -1f;
        private readonly List<int> _poseSeq = new List<int>(8);

        /// <summary>Bootstrap 调用的入口。</summary>
        public void Begin(string mode)
        {
            _mode = mode;
            _t0 = Time.realtimeSinceStartup;
            _report = new ChieQaReport();
            StartCoroutine(CoSafe());
        }

        /// <summary>异常兜底：任何阶段抛异常都要留下报告并退出，不能卡死。</summary>
        private IEnumerator CoSafe()
        {
            IEnumerator inner = CoRun();
            while (true)
            {
                object cur;
                bool moved;
                try
                {
                    moved = inner.MoveNext();
                    cur = moved ? inner.Current : null;
                }
                catch (System.Exception ex)
                {
                    if (_report != null) _report.Fail("X90", "驱动器全程未抛异常", ex.GetType().Name + ": " + ex.Message);
                    Debug.LogError("[Chie][QA] 驱动器异常：" + ex);
                    break;
                }
                if (!moved) break;
                yield return cur;
            }
            if (!_done && _report != null)
            {
                _report.Note("异常/中断后强制收尾");
                yield return CoFinish();
            }
        }

        private IEnumerator CoRun()
        {
            _report.Begin(_mode, ChieQaArgs.ReportPath());

            float t = 0f;
            while ((_flow = Object.FindObjectOfType<ChieFlow>()) == null && t < 10f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_flow == null)
            {
                _report.Fail("A01", "场景内存在 ChieFlow", "10s 内没找到");
                yield return CoFinish();
                yield break;
            }
            t = 0f;
            while (!_flow.QaReady && t < 10f)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!_flow.QaReady)
            {
                _report.Fail("A02", "ChieFlow 依赖绑定完成", "QaReady=false state=" + _flow.State);
                yield return CoFinish();
                yield break;
            }
            _report.Pass("A02", "ChieFlow 依赖绑定完成", "state=" + _flow.State);

            yield return CoWait(delegate { return _flow.QaDialogue.Visible; }, 5f);
            float delay = Time.realtimeSinceStartup - _t0;
            if (delay <= 3f) _report.Pass("A03", "开场 3 秒内出现对话", delay.ToString("0.00") + "s");
            else _report.Fail("A03", "开场 3 秒内出现对话", delay.ToString("0.00") + "s");

            yield return CoEditPhase();
            yield return CoHoldPhase();
            yield return CoEndingPhase();
            yield return CoExitPhase();
            yield return CoFinish();
        }

        /// <summary>等条件成立；期间持续推进对话。</summary>
        private IEnumerator CoWait(System.Func<bool> cond, float timeout)
        {
            float t = 0f;
            while (!cond() && t < timeout)
            {
                Pump();
                SamplePose();
                t += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                if (Time.realtimeSinceStartup - _t0 > RunLimit) break;
                yield return null;
            }
        }

        /// <summary>记录真实 judge 姿态的变化序列（Sit→Shift→Up→Stand）。</summary>
        private void SamplePose()
        {
            if (_flow == null) return;
            int cur = (int)_flow.QaJudge.Pose;
            if (_poseSeq.Count > 0 && _poseSeq[_poseSeq.Count - 1] == cur) return;
            _poseSeq.Add(cur);
            Debug.Log("[Chie][QA] 姿态 → " + (PoseId)cur + " t=" + (Time.realtimeSinceStartup - _t0).ToString("0.00") + "s");
        }

        /// <summary>一帧的对话推进：立即显示整句 → 记录 → 下一句。</summary>
        private void Pump()
        {
            if (_flow == null) return;
            InCanvasDialogueView dlg = _flow.QaDialogue;
            if (dlg == null) return;
            if (dlg.HasChoices) { HandleChoices(dlg); return; }
            if (!dlg.IsPlaying) return;

            dlg.CompleteTyping();
            string body = ChieQaUi.BodyText(dlg);
            if (!string.IsNullOrEmpty(body) && body != _lastBody)
            {
                _lastBody = body;
                _lines.Add(body);
                _lineTimes.Add(Time.realtimeSinceStartup - _t0);
            }
            if (Time.realtimeSinceStartup - _lastAdvance < 0.05f) return;
            _lastAdvance = Time.realtimeSinceStartup;
            dlg.Advance();
        }

        /// <summary>选项：先逐行验证中心命中，再按轮换索引真实点击。</summary>
        private void HandleChoices(InCanvasDialogueView dlg)
        {
            ChieOptionLine[] opts = ChieQaUi.Options(dlg);
            if (opts == null) return;

            var active = new List<ChieOptionLine>(3);
            var texts = new List<string>(3);
            for (int i = 0; i < opts.Length; i++)
            {
                if (opts[i] == null || !opts[i].gameObject.activeInHierarchy) continue;
                string tx = ChieQaUi.OptionText(opts[i]);
                if (string.IsNullOrEmpty(tx)) continue;
                active.Add(opts[i]);
                texts.Add(tx);
            }
            if (active.Count == 0) return;

            string key = string.Join("|", texts.ToArray());
            if (key == _lastChoiceKey && Time.realtimeSinceStartup - _choiceAt < 0.5f) return;
            _lastChoiceKey = key;
            _choiceAt = Time.realtimeSinceStartup;

            string node = MatchChoiceNode(texts);
            _choiceNodes.Add(node);

            for (int i = 0; i < active.Count; i++)
            {
                string detail;
                bool ok = ChieQaUi.ProbeCenter(ChieQaUi.ClickRect(active[i]), out detail);
                string id = "C" + _choiceNodes.Count + "." + i;
                if (ok) _report.Pass(id, "选项中心命中 ChieOptionLine（" + node + " / " + texts[i] + "）", detail);
                else _report.Fail(id, "选项中心命中 ChieOptionLine（" + node + " / " + texts[i] + "）", detail);
            }

            int pick = _pickCursor % active.Count;
            _pickCursor++;
            string cd;
            RectTransform prt = ChieQaUi.ClickRect(active[pick]);
            if (ChieQaUi.ClickCenter(prt, out cd))
                _report.Pass("C" + _choiceNodes.Count + ".pick", "真实点击选项（" + node + " → " + texts[pick] + "）", cd);
            else if (ChieQaUi.ClickDirect(prt, out cd))
                _report.Pass("C" + _choiceNodes.Count + ".pick", "点击选项（射线未命中，直接派发同一点击链）", cd);
            else
                _report.Fail("C" + _choiceNodes.Count + ".pick", "真实点击选项（" + node + "）", cd);
            _choicePicks.Add(node + ":" + texts[pick]);
        }

        /// <summary>用可见文本反查对话节点 id（不硬编码中文）。</summary>
        private string MatchChoiceNode(List<string> texts)
        {
            DialogueRunner runner = _flow != null ? _flow.QaRunner : null;
            DialogueData data = runner != null ? runner.Data : null;
            if (data == null || data.nodes == null) return "?";
            for (int i = 0; i < data.nodes.Length; i++)
            {
                DialogueNode n = data.nodes[i];
                if (n == null || n.kind != "choice" || n.options == null) continue;
                if (n.options.Length != texts.Count) continue;
                bool ok = true;
                for (int j = 0; j < texts.Count; j++)
                {
                    if (n.options[j] == null || n.options[j].text != texts[j]) { ok = false; break; }
                }
                if (ok) return n.id;
            }
            return "?";
        }

        /// <summary>记录里是否出现过某句。</summary>
        private bool HasLine(string needle)
        {
            if (string.IsNullOrEmpty(needle)) return false;
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i] != null && _lines[i].IndexOf(needle, System.StringComparison.Ordinal) >= 0) return true;
            }
            return false;
        }

        /// <summary>取第一句非空文本（数据侧），用于比对。</summary>
        private static string FirstText(DialogueLine[] lines)
        {
            if (lines == null) return "";
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] != null && !string.IsNullOrEmpty(lines[i].t)) return lines[i].t;
            }
            return "";
        }

        /// <summary>收尾：写报告 → 退出。</summary>
        private IEnumerator CoFinish()
        {
            if (_done) yield break;
            _done = true;
            yield return null;
            yield return null;
            _report.Note("对话句数=" + _lines.Count + " 选项节点=" + _choiceNodes.Count);
            string path = _report.Flush();
            Debug.Log("[Chie][QA] 汇总 pass=" + _report.PassCount + " fail=" + _report.FailCount + " report=" + path);
            yield return new WaitForSecondsRealtime(0.3f);
            Debug.Log("[Chie][QA] Application.Quit()");
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
