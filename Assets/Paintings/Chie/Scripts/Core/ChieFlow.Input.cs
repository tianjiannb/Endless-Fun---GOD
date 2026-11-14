using UnityEngine;
using UnityEngine.EventSystems;

namespace Numen.Chie
{
    public partial class ChieFlow
    {
        private void Update()
        {
            if (!_alive) return;

            // 对话占用输入：选项交给 EventSystem，其余点击/空格/回车推进台词
            if (_dlg != null && _dlg.Visible)
            {
                if (_pointer != null) _pointer.CanPaint = false;
                if (!_dlg.HasChoices && DialogueAdvancePressed() && _runner != null) _runner.Advance();
                return;
            }

            bool overUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool allow = _paintAllowed && !_busy && State == FlowState.Edit && !overUi;
            if (_pointer != null) _pointer.CanPaint = allow;
            if (!allow || _pointer == null || _painter == null) return;

            if (_painter.Tool == ToolId.Eyedropper)
            {
                if (_pointer.Down && _pointer.InCanvas) HandleEyedropper();
                return;
            }

            if (!_pointer.InCanvas)
            {
                // 拖到画外：立刻收笔并结算，避免下一帧从边缘拉一道
                if (_painter.IsDrawing) EndStroke(_pointer.Pixel);
                return;
            }

            _painter.Stroke(_pointer.Pixel, _pointer.Down, _pointer.Held, _pointer.Up);
            if (_pointer.Up) EndStroke(_pointer.Pixel);
        }

        /// <summary>收笔：落最后一段覆盖并结算。</summary>
        private void EndStroke(Vector2 px)
        {
            if (_painter != null) _painter.Stroke(px, false, false, true);
            if (_judge != null) _judge.Evaluate();
        }

        private static bool DialogueAdvancePressed()
        {
            if (Input.GetMouseButtonDown(0)) return true;
            if (Input.GetKeyDown(KeyCode.Space)) return true;
            if (Input.GetKeyDown(KeyCode.Return)) return true;
            if (Input.GetKeyDown(KeyCode.KeypadEnter)) return true;
            return false;
        }

        private void HandleEyedropper()
        {
            if (_gate == null || _pointer == null) return;
            _gate.Dip(_pointer.Pixel);
            bool ok = _gate.ColorOk(FlawId.Mouth) || _gate.ColorOk(FlawId.Shadow);
            if (_painter != null) _painter.ColorValid = ok;
            if (_toolbar != null) _toolbar.SetHint(ok ? "颜色对了，换回笔去改。" : "这个颜色不对，再取一次。");
        }
    }
}
