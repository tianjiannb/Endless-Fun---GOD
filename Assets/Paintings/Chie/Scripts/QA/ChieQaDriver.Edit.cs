using System.Collections;
using System.Collections.Generic;
using Numen.Chie.UI;
using UnityEngine;

namespace Numen.Chie.QA
{
    /// <summary>修画阶段：真实工具栏点击 → 真实吸管 → 真实笔迹 → 真实结算。</summary>
    public partial class ChieQaDriver
    {
        private static readonly FlawId[] QaOrder =
        {
            FlawId.Tip, FlawId.Face, FlawId.Dust, FlawId.Window, FlawId.Mouth, FlawId.Shadow
        };

        private readonly bool[] _negDone = new bool[7];

        private IEnumerator CoEditPhase()
        {
            yield return CoWait(delegate { return _flow.State == FlowState.Edit && !_flow.Busy; }, 45f);
            if (_flow.State != FlowState.Edit)
            {
                _report.Fail("A04", "进入编辑状态", "state=" + _flow.State + " busy=" + _flow.Busy);
                yield break;
            }
            _report.Pass("A04", "进入编辑状态", "restored=" + _flow.QaJudge.RestoredCount);

            CheckChoiceSequence("A05", new[] { "ask_1", "ask_2" }, "开场两次询问");
            yield return CheckToolbar("A06", 1);

            if (_mode == ChieQaArgs.ModeDrop)
            {
                yield return CoDropRoute();
                yield break;
            }

            for (int i = 0; i < QaOrder.Length; i++)
            {
                if (_flow.State != FlowState.Edit || _flow.QaHoldRequested) break;
                yield return CoRepair(QaOrder[i], i);
                if (!_flow.QaJudge.IsRestored(QaOrder[i])) break;
            }

            int restored = _flow.QaJudge.RestoredCount;
            if (restored >= FlawInfo.Count) _report.Pass("F99", "六处全部修好", "restored=" + restored);
            else _report.Fail("F99", "六处全部修好", "restored=" + restored + " state=" + _flow.State);

            yield return CoWait(delegate { return _choiceNodes.Contains("ask_3"); }, 8f);
            CheckChoiceSequence("A07", new[] { "ask_3" }, "第三波追加一问");
            CheckPoses();
            _editDoneAt = Time.realtimeSinceStartup - _t0;
        }

        /// <summary>姿态随波次真实切换：Sit→Shift→Up（drop 路线只到 Sit）。</summary>
        private void CheckPoses()
        {
            var want = new List<int>(3);
            want.Add((int)PoseId.Sit);
            if (_mode != ChieQaArgs.ModeDrop)
            {
                want.Add((int)PoseId.Shift);
                want.Add((int)PoseId.Up);
            }
            int wp = 0;
            for (int i = 0; i < _poseSeq.Count && wp < want.Count; i++)
            {
                if (_poseSeq[i] == want[wp]) wp++;
            }
            var got = new List<string>(_poseSeq.Count);
            for (int i = 0; i < _poseSeq.Count; i++) got.Add(((PoseId)_poseSeq[i]).ToString());
            string d = "期望=" + (want.Count == 1 ? "Sit" : "Sit>Shift>Up") + " 实际=" + string.Join(">", got.ToArray());
            if (wp >= want.Count) _report.Pass("A08", "姿态随波次真实切换", d);
            else _report.Fail("A08", "姿态随波次真实切换", d);
        }

        /// <summary>对比已点击过的对话节点顺序。</summary>
        private void CheckChoiceSequence(string id, string[] expect, string title)
        {
            bool ok = true;
            var got = new List<string>(8);
            for (int i = 0; i < expect.Length; i++)
            {
                if (!_choiceNodes.Contains(expect[i])) ok = false;
                got.Add(_choiceNodes.Contains(expect[i]) ? expect[i] : "-");
            }
            string d = "期望=" + string.Join(",", expect) + " 实际=" + string.Join(",", _choiceNodes.ToArray())
                + " 选择=" + string.Join(",", _choicePicks.ToArray());
            if (ok) _report.Pass(id, title, d);
            else _report.Fail(id, title, d);
        }

        /// <summary>工具栏解锁顺序：Wave>=2 才有橡皮，Wave>=3 才有吸管。</summary>
        private IEnumerator CheckToolbar(string tag, int wave)
        {
            yield return null;
            WaveUnlock w = _flow.QaWave;
            int cur = w != null ? w.Wave : wave;
            bool e = w == null || w.IsToolUnlocked(ToolId.Eraser);
            bool d = w == null || w.IsToolUnlocked(ToolId.Eyedropper);
            bool wantE = cur >= 2;
            bool wantD = cur >= 3;
            string detail = "wave=" + cur + " 橡皮=" + e + " 吸管=" + d;
            if (e == wantE) _report.Pass(tag + ".eraser", "橡皮解锁时机（Wave>=2）", detail);
            else _report.Fail(tag + ".eraser", "橡皮解锁时机（Wave>=2）", detail);
            if (d == wantD) _report.Pass(tag + ".dropper", "吸管解锁时机（Wave>=3）", detail);
            else _report.Fail(tag + ".dropper", "吸管解锁时机（Wave>=3）", detail);

            ChieToolButton brush = ChieQaUi.ToolButton(ToolId.Brush);
            if (brush == null || !brush.gameObject.activeInHierarchy) _report.Fail(tag + ".button", "画笔按钮可见", "button=" + (brush == null ? "null" : "inactive"));
            else _report.Pass(tag + ".button", "画笔按钮可见", "wave=" + cur);
        }

        /// <summary>修一处：负向 → 选工具 → （吸管）→ 真实笔画 → 波次校验。</summary>
        private IEnumerator CoRepair(FlawId id, int idx)
        {
            yield return CoWait(delegate { return _flow.State == FlowState.Edit && !_flow.Busy; }, 30f);
            if (_flow.State != FlowState.Edit) yield break;

            ToolId tool = FlawInfo.ToolOf(id);
            string name = FlawInfo.NameOf(id);

            if (!_negDone[idx] && (_flow.QaJudge.Coverage(id) <= 0f))
            {
                _negDone[idx] = true;
                bool needBlock = tool != ToolId.Brush || FlawInfo.NeedDip(id);
                if (needBlock)
                {
                    yield return ClickTool(ToolId.Brush, "负向检查");
                    yield return StrokeFlaw(id, 18f);
                    float after = _flow.QaJudge.Coverage(id);
                    if (after <= 0.0001f) _report.Pass("N" + idx, "负向：条件不满足时笔迹不进覆盖（" + name + "）", "coverage=" + after.ToString("0.000"));
                    else _report.Fail("N" + idx, "负向：条件不满足时笔迹不进覆盖（" + name + "）", "coverage=" + after.ToString("0.000"));
                }
            }

            yield return ClickTool(tool, "修 " + name);

            if (FlawInfo.NeedDip(id))
            {
                Vector2 dip;
                string dd;
                bool found = ChieQaMask.TryDip(_flow, id, out dip, out dd);
                if (found)
                {
                    yield return ClickTool(ToolId.Eyedropper, "吸管取 " + name);
                    bool ok = _flow.QaDipAt(dip) && _flow.QaGate.ColorOk(id);
                    if (ok) _report.Pass("D" + idx, "吸管取到目标色（" + name + "）", dd);
                    else _report.Fail("D" + idx, "吸管取到目标色（" + name + "）", dd + " dipped=" + _flow.QaGate.Dipped);
                    yield return ClickTool(ToolId.Brush, "回到笔");
                }
                else
                {
                    _report.Fail("D" + idx, "吸管取到目标色（" + name + "）", dd);
                }
            }

            for (int pass = 0; pass < 3 && !_flow.QaJudge.IsRestored(id); pass++)
            {
                yield return CoWait(delegate { return _flow.State == FlowState.Edit && !_flow.Busy; }, 30f);
                if (_flow.State != FlowState.Edit) break;
                yield return StrokeFlaw(id, 18f);
                yield return null;
            }

            if (!_flow.QaJudge.IsRestored(id))
            {
                _report.Fail("F" + idx, "修好 " + name, "coverage=" + _flow.QaJudge.Coverage(id).ToString("0.00") + " tool=" + tool + " needDip=" + FlawInfo.NeedDip(id));
                yield break;
            }
            _report.Pass("F" + idx, "修好 " + name, "restored=" + _flow.QaJudge.RestoredCount + " tool=" + tool);

            int restored = _flow.QaJudge.RestoredCount;
            int expWave = restored >= 2 ? 3 : 2;
            int wave = _flow.QaWave != null ? _flow.QaWave.Wave : 1;
            if (wave >= expWave) _report.Pass("W" + idx, "波次随修复数推进", "restored=" + restored + " wave=" + wave);
            else _report.Fail("W" + idx, "波次随修复数推进", "restored=" + restored + " wave=" + wave);
            if (_flow.State == FlowState.Edit && !_flow.QaHoldRequested)
            {
                yield return CoWait(delegate { return _flow.State != FlowState.Edit || !_flow.Busy; }, 30f);
                yield return CheckToolbar("W" + idx + ".tool", wave);
            }
            else _report.Note("第 " + (idx + 1) + " 处修复后流程已离场（" + _flow.State + "），工具栏时序改由前序波次判定");
        }

        /// <summary>真实点击工具按钮；按钮不可用时退回 flow 内部同一处理。</summary>
        private IEnumerator ClickTool(ToolId tool, string why)
        {
            yield return null;
            ChieToolButton btn = ChieQaUi.ToolButton(tool);
            string tag = "T." + tool + "." + why;
            if (btn == null || !btn.gameObject.activeInHierarchy)
            {
                _report.Note("工具 " + tool + " 按钮不可点（" + why + "），走内部入口");
                _flow.QaPickTool(tool);
                yield return null;
                yield break;
            }
            string detail;
            if (ChieQaUi.ClickCenter((RectTransform)btn.transform, out detail))
                _report.Pass(tag, "真实点击工具按钮 " + tool, detail);
            else
            {
                _report.Fail(tag, "真实点击工具按钮 " + tool, detail);
                _flow.QaPickTool(tool);
            }
            yield return null;
        }

        /// <summary>按 mask 行真实下笔；每行一次 down → held… → 收笔结算。</summary>
        private IEnumerator StrokeFlaw(FlawId id, float rowGap)
        {
            BrushPainter painter = _flow.QaPainter;
            List<Vector2> cells = ChieQaMask.Cells(_flow.QaConfig, _flow.QaJudge.Pose, id);
            if (cells.Count == 0)
            {
                _report.Fail("S" + (int)id, "取到 " + FlawInfo.NameOf(id) + " 的 mask 格", ChieQaMask.Bounds(cells));
                yield break;
            }
            List<Vector2[]> rows = ChieQaMask.Rows(cells, rowGap);
            float step = Mathf.Max(4f, _flow.QaConfig.BrushStepPx);
            for (int r = 0; r < rows.Count; r++)
            {
                if (_flow.State != FlowState.Edit) yield break;
                Vector2 a = rows[r][0];
                Vector2 b = rows[r][1];
                painter.Stroke(a, true, false, false);
                int k = 0;
                for (float x = a.x + step; x < b.x; x += step)
                {
                    painter.Stroke(new Vector2(x, a.y), false, true, false);
                    if (++k % 32 == 0) yield return null;
                }
                painter.Stroke(b, false, true, false);
                _flow.QaEndStroke(b);
                yield return null;
                if (_flow.QaJudge.IsRestored(id)) yield break;
            }
        }

        /// <summary>-chieqa=drop：不修任何处，真实点"放下笔"进 Hold。</summary>
        private IEnumerator CoDropRoute()
        {
            yield return null;
            ChieToolButton btn = ChieQaUi.ToolButton(ToolId.PutDown);
            if (btn == null || !btn.gameObject.activeInHierarchy)
            {
                _report.Fail("P01", "放下笔按钮可点", btn == null ? "没找到按钮" : "按钮未激活");
                _flow.QaPutDown();
            }
            else
            {
                string detail;
                if (ChieQaUi.ClickCenter((RectTransform)btn.transform, out detail))
                    _report.Pass("P01", "真实点击放下笔", detail);
                else
                {
                    _report.Fail("P01", "真实点击放下笔", detail);
                    _flow.QaPutDown();
                }
            }
            yield return CoWait(delegate { return _flow.QaHoldRequested || _flow.State != FlowState.Edit; }, 10f);
            if (_flow.QaHoldRequested || _flow.State == FlowState.Hold)
                _report.Pass("P02", "放下笔路线进入 Hold", "state=" + _flow.State + " restored=" + _flow.QaJudge.RestoredCount);
            else
                _report.Fail("P02", "放下笔路线进入 Hold", "state=" + _flow.State);
            _editDoneAt = Time.realtimeSinceStartup - _t0;
        }
    }
}
