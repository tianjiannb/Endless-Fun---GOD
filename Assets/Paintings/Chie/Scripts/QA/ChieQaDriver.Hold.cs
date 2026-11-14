using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Numen.Chie.QA
{
    /// <summary>Hold / 回放 / 结局 / 退出 的验收。</summary>
    public partial class ChieQaDriver
    {
        private static readonly FlawId[] RevealOrder =
        {
            FlawId.Tip, FlawId.Face, FlawId.Dust, FlawId.Window, FlawId.Mouth, FlawId.Shadow
        };

        private readonly List<int> _revealSeq = new List<int>(8);
        private float _holdAt = -1f;
        private float _exitAt = -1f;

        private IEnumerator CoHoldPhase()
        {
            yield return CoWait(delegate { return _flow.State == FlowState.Hold; }, 90f);
            if (_flow.State != FlowState.Hold)
            {
                _report.Fail("H01", "进入 Hold", "state=" + _flow.State + " restored=" + _flow.QaJudge.RestoredCount);
                yield break;
            }
            _holdAt = Time.realtimeSinceStartup - _t0;
            int restored = _flow.QaJudge.RestoredCount;
            int all = _flow.QaConfig != null ? _flow.QaConfig.AllOpenAt : 6;
            string entry = restored >= all ? "六处修满" : (_mode == ChieQaArgs.ModeDrop ? "放下笔" : "修复不足");
            _report.Pass("H01", "进入 Hold", "入口=" + entry + " restored=" + restored + " t=" + _holdAt.ToString("0.00") + "s");

            yield return CoWait(delegate { return _flow.QaJudge.Pose == PoseId.Stand; }, 6f);
            if (_flow.QaJudge.Pose == PoseId.Stand) _report.Pass("H02", "Hold 期间切到 Stand 姿态", "pose=" + _flow.QaJudge.Pose);
            else _report.Fail("H02", "Hold 期间切到 Stand 姿态", "pose=" + _flow.QaJudge.Pose);

            Material mat = ChieQaUi.PaintMaterial(_flow.QaView);
            float guard = 0f;
            while (_flow.State == FlowState.Hold && guard < 90f)
            {
                Pump();
                SampleZoom(mat);
                guard += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                yield return null;
            }
            _report.Note("Hold 用时=" + guard.ToString("0.00") + "s 回放序列=" + SeqText());

            DialogueData data = _flow.QaRunner != null ? _flow.QaRunner.Data : null;
            DialogueLine[] stand = data != null ? (restored >= all ? data.standAll : data.standPart) : null;
            string standText = FirstText(stand);
            if (standText.Length > 0 && HasLine(standText)) _report.Pass("H03", "站起台词（" + (restored >= all ? "standAll" : "standPart") + "）", standText);
            else _report.Fail("H03", "站起台词（" + (restored >= all ? "standAll" : "standPart") + "）", "未在日志中出现：" + standText);

            CheckRevealOrder(restored);
        }

        /// <summary>按材质 _BaseTex_ST 反解当前推近中心，记录回放顺序。</summary>
        private void SampleZoom(Material mat)
        {
            if (mat == null || !mat.HasProperty("_BaseTex_ST")) return;
            Vector4 st = mat.GetVector("_BaseTex_ST");
            float zoom = st.x > 0.0001f ? 1f / st.x : 1f;
            if (zoom < 1.05f) return;
            Vector2 center = new Vector2(st.z + st.x * 0.5f, st.w + st.y * 0.5f);
            int best = -1;
            float bestD = 999f;
            for (int i = 0; i < FlawInfo.Count; i++)
            {
                Vector4 r;
                if (_flow.QaConfig == null || !_flow.QaConfig.TryGetZone((FlawId)i, PoseId.Stand, out r)) continue;
                Vector2 c = new Vector2(r.x + r.z * 0.5f, r.y + r.w * 0.5f);
                float d = Vector2.Distance(c, center);
                if (d < bestD) { bestD = d; best = i; }
            }
            if (best < 0 || bestD > 0.02f) return;
            if (_revealSeq.Count > 0 && _revealSeq[_revealSeq.Count - 1] == best) return;
            _revealSeq.Add(best);
            Debug.Log("[Chie][QA] 回放推近 " + FlawInfo.NameOf((FlawId)best) + " zoom=" + zoom.ToString("0.00") + " d=" + bestD.ToString("0.000"));
        }

        private string SeqText()
        {
            if (_revealSeq.Count == 0) return "(无)";
            var parts = new List<string>(_revealSeq.Count);
            for (int i = 0; i < _revealSeq.Count; i++) parts.Add(((FlawId)_revealSeq[i]).ToString());
            return string.Join(">", parts.ToArray());
        }

        /// <summary>回放顺序：只回放已修复项，且按 Tip>Face>Dust>Window>Mouth>Shadow。</summary>
        private void CheckRevealOrder(int restored)
        {
            var expect = new List<int>(6);
            for (int i = 0; i < RevealOrder.Length; i++)
            {
                if (_flow.QaJudge.IsRestored(RevealOrder[i])) expect.Add((int)RevealOrder[i]);
            }
            bool ok = expect.Count == _revealSeq.Count;
            if (ok)
            {
                for (int i = 0; i < expect.Count; i++)
                {
                    if (expect[i] != _revealSeq[i]) { ok = false; break; }
                }
            }
            string d = "期望=" + (expect.Count == 0 ? "(无)" : string.Join(">", NamesOf(expect))) + " 实际=" + SeqText();
            if (ok) _report.Pass("H04", "回放顺序与已修复项一致", d);
            else _report.Fail("H04", "回放顺序与已修复项一致", d + " restored=" + restored);
        }

        private static string[] NamesOf(List<int> ids)
        {
            var a = new string[ids.Count];
            for (int i = 0; i < ids.Count; i++) a[i] = ((FlawId)ids[i]).ToString();
            return a;
        }

        private IEnumerator CoEndingPhase()
        {
            yield return CoWait(delegate { return _flow.State == FlowState.Ending; }, 40f);
            if (_flow.State != FlowState.Ending)
            {
                _report.Fail("E01", "进入结局状态", "state=" + _flow.State);
                yield break;
            }
            _report.Pass("E01", "进入结局状态", "restored=" + _flow.QaJudge.RestoredCount);

            yield return CoWait(delegate { return ChieQaUi.EndingLine(0) != null && ChieQaUi.EndingLine(0).gameObject.activeInHierarchy; }, 10f);

            int restored = _flow.QaJudge.RestoredCount;
            int all = _flow.QaConfig != null ? _flow.QaConfig.AllOpenAt : 6;
            int part = _flow.QaConfig != null ? _flow.QaConfig.PartOpenAt : 3;
            int expect = restored >= all ? 3 : (restored >= part ? 2 : 1);

            int count = 0;
            for (int i = 0; i < 3; i++)
            {
                ChieOptionLine l = ChieQaUi.EndingLine(i);
                if (l != null && l.gameObject.activeInHierarchy) count++;
            }
            if (count == expect) _report.Pass("E02", "结局选项数量按修复数开放", "restored=" + restored + " count=" + count);
            else _report.Fail("E02", "结局选项数量按修复数开放", "restored=" + restored + " count=" + count + " 期望=" + expect);

            DialogueData data = _flow.QaRunner != null ? _flow.QaRunner.Data : null;
            DialogueNode node = data != null ? data.GetNode("ask_end") : null;
            DialogueOption[] opts = node != null ? node.options : null;
            int baseIdx = opts != null ? Mathf.Max(0, opts.Length - expect) : 0;
            bool textOk = opts != null && opts.Length >= expect;
            var shown = new List<string>(3);
            for (int i = 0; i < count; i++)
            {
                ChieOptionLine l = ChieQaUi.EndingLine(i);
                string tx = ChieQaUi.OptionText(l);
                shown.Add(tx);
                if (!textOk || opts[baseIdx + i] == null || opts[baseIdx + i].text != tx) textOk = false;
            }
            string td = "期望=" + (opts != null ? baseIdx : -1) + " 实际=" + string.Join("/", shown.ToArray());
            if (textOk) _report.Pass("E03", "结局选项文本与 ask_end 数据一致", td);
            else _report.Fail("E03", "结局选项文本与 ask_end 数据一致", td);

            for (int i = 0; i < count; i++)
            {
                string detail;
                bool hit = ChieQaUi.ProbeCenter((RectTransform)ChieQaUi.EndingLine(i).transform, out detail);
                if (hit) _report.Pass("E04." + i, "结局选项中心命中 ChieOptionLine", detail);
                else _report.Fail("E04." + i, "结局选项中心命中 ChieOptionLine", detail);
            }

            string cd;
            ChieOptionLine first = ChieQaUi.EndingLine(0);
            RectTransform frt = first != null ? (RectTransform)first.transform : null;
            if (ChieQaUi.ClickCenter(frt, out cd))
                _report.Pass("E05", "真实点击结局选项", cd);
            else if (ChieQaUi.ClickDirect(frt, out cd))
                _report.Pass("E05", "点击结局选项（射线未命中，直接派发同一点击链）", cd);
            else
                _report.Fail("E05", "真实点击结局选项", cd);

            yield return CoWait(delegate { return _flow.State == FlowState.Finale; }, 15f);
            if (_flow.State == FlowState.Finale) _report.Pass("E06", "选完进入 Finale", "state=" + _flow.State);
            else _report.Fail("E06", "选完进入 Finale", "state=" + _flow.State);

            string replyText = opts != null && opts[baseIdx] != null ? FirstText(opts[baseIdx].reply) : "";
            yield return CoWait(delegate { return replyText.Length == 0 || HasLine(replyText) || _flow.State == FlowState.Exit; }, 25f);
            if (replyText.Length == 0 || HasLine(replyText)) _report.Pass("E07", "结局回应台词出现", replyText);
            else _report.Fail("E07", "结局回应台词出现", "未出现：" + replyText);

            string finText = data != null ? FirstText(data.finale) : "";
            yield return CoWait(delegate { return finText.Length == 0 || HasLine(finText) || _flow.State == FlowState.Exit; }, 25f);
            if (finText.Length == 0 || HasLine(finText)) _report.Pass("E08", "终幕收尾台词出现", finText);
            else _report.Fail("E08", "终幕收尾台词出现", "未出现：" + finText);

            yield return CoWait(delegate { return ChieQaUi.EndingAlpha() <= 0.02f || _flow.State == FlowState.Exit; }, 12f);
            float ea = ChieQaUi.EndingAlpha();
            if (ea <= 0.02f) _report.Pass("E09", "终幕淡出到全透明", "endingAlpha=" + ea.ToString("0.00"));
            else _report.Fail("E09", "终幕淡出到全透明", "endingAlpha=" + ea.ToString("0.00"));
        }

        private IEnumerator CoExitPhase()
        {
            yield return CoWait(delegate { return _flow.State == FlowState.Exit; }, 20f);
            if (_flow.State != FlowState.Exit)
            {
                _report.Fail("X01", "进入 Exit 状态", "state=" + _flow.State);
                yield break;
            }
            _exitAt = Time.realtimeSinceStartup - _t0;
            _report.Pass("X01", "进入 Exit 状态", "t=" + _exitAt.ToString("0.00") + "s");

            ChieFade fade = _flow.QaFade;
            float guard = 0f;
            while (fade != null && fade.Alpha < 0.99f && guard < 5f)
            {
                guard += Mathf.Min(Time.unscaledDeltaTime, 0.1f);
                yield return null;
            }
            float a = fade != null ? fade.Alpha : -1f;
            if (a >= 0.99f) _report.Pass("X02", "退出黑场淡到全黑", "alpha=" + a.ToString("0.00"));
            else _report.Fail("X02", "退出黑场淡到全黑", "alpha=" + a.ToString("0.00"));

            yield return CoWait(delegate { return _report.SawLog("Scene_Gallery") || _flow.State != FlowState.Exit; }, 4f);
            bool canLoad = Application.CanStreamedLevelBeLoaded("Scene_Gallery");
            bool warned = _report.SawLog("未找到 Scene_Gallery") || _report.SawLog("Scene_Gallery");
            if (!canLoad && warned) _report.Pass("X03", "缺 Scene_Gallery 时警告且不崩", "canLoad=" + canLoad + " 日志=" + _report.LastLogOf("Scene_Gallery"));
            else _report.Fail("X03", "缺 Scene_Gallery 时警告且不崩", "canLoad=" + canLoad + " warned=" + warned);
        }
    }
}
