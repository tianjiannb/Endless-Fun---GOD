using System;
using System.Collections.Generic;
using System.Reflection;
using Numen.Chie.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Numen.Chie.QA
{
    /// <summary>真实 EventSystem / GraphicRaycaster 探针与点击派发。</summary>
    public static class ChieQaUi
    {
        private static readonly FieldInfo PartsField =
            typeof(InCanvasDialogueView).GetField("_parts", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo MatField =
            typeof(PaintingView).GetField("_mat", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo EndingGroupField =
            typeof(EndingView).GetField("_group", BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>对话视图内部引用集合。</summary>
        public static DialogueCanvasParts Parts(InCanvasDialogueView dlg)
        {
            if (dlg == null || PartsField == null) return null;
            return PartsField.GetValue(dlg) as DialogueCanvasParts;
        }

        /// <summary>画面材质（读 _BaseTex_ST 用）。</summary>
        public static Material PaintMaterial(PaintingView view)
        {
            if (view == null || MatField == null) return null;
            return MatField.GetValue(view) as Material;
        }

        /// <summary>取控件所在 Canvas 的相机。</summary>
        public static Camera CameraOf(Component c)
        {
            Canvas canvas = c != null ? c.GetComponentInParent<Canvas>() : null;
            if (canvas != null && canvas.worldCamera != null) return canvas.worldCamera;
            return Camera.main;
        }

        /// <summary>RectTransform 中心的屏幕坐标。</summary>
        public static bool TryCenterScreen(RectTransform rt, out Vector2 screen, out string detail)
        {
            screen = Vector2.zero;
            detail = "";
            if (rt == null)
            {
                detail = "RectTransform 为空";
                return false;
            }
            if (!rt.gameObject.activeInHierarchy)
            {
                detail = rt.name + " 未激活";
                return false;
            }
            Camera cam = CameraOf(rt);
            Vector3 world = rt.TransformPoint(rt.rect.center);
            if (cam == null)
            {
                screen = new Vector2(world.x, world.y);
                detail = "无相机，按世界坐标取点";
                return true;
            }
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0f)
            {
                detail = rt.name + " 在相机背后";
                return false;
            }
            screen = new Vector2(sp.x, sp.y);
            detail = rt.name + " center=(" + screen.x.ToString("0.0") + "," + screen.y.ToString("0.0") + ")";
            return true;
        }

        /// <summary>RaycastAll，按遮挡顺序返回。</summary>
        public static List<RaycastResult> Raycast(Vector2 screen)
        {
            var hits = new List<RaycastResult>(8);
            if (EventSystem.current == null) return hits;
            var ped = new PointerEventData(EventSystem.current);
            ped.position = screen;
            ped.button = PointerEventData.InputButton.Left;
            EventSystem.current.RaycastAll(ped, hits);
            return hits;
        }

        /// <summary>命中列表压成一行名字，失败时写进报告。</summary>
        public static string Describe(List<RaycastResult> hits)
        {
            if (hits == null || hits.Count == 0) return "无命中";
            string s = "";
            for (int i = 0; i < hits.Count && i < 4; i++)
            {
                GameObject go = hits[i].gameObject;
                if (i > 0) s += " > ";
                s += go != null ? go.name : "null";
            }
            return s;
        }

        /// <summary>选项可用点击的那一层矩形（有透明命中框就用它）。</summary>
        public static RectTransform ClickRect(ChieOptionLine line)
        {
            if (line == null) return null;
            Image hit = line.Hitbox;
            if (hit != null) return (RectTransform)hit.transform;
            return (RectTransform)line.transform;
        }

        /// <summary>验证某控件中心点能命中它自己（或其子物体上的点击处理器）。</summary>
        public static bool ProbeCenter(RectTransform rt, out string detail)
        {
            detail = "";
            Vector2 screen;
            string pos;
            if (!TryCenterScreen(rt, out screen, out pos))
            {
                detail = pos;
                return false;
            }
            List<RaycastResult> hits = Raycast(screen);
            string top = Describe(hits);
            if (hits.Count == 0)
            {
                detail = pos + " | 无命中";
                return false;
            }
            GameObject hitGo = hits[0].gameObject;
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitGo);
            // 命中框是选项/按钮的子物体，处理器在祖先上：两个方向都算命中
            bool ok = handler != null && (handler == rt.gameObject
                || handler.transform.IsChildOf(rt)
                || rt.IsChildOf(handler.transform));
            detail = pos + " | top=" + top + " | handler=" + (handler != null ? handler.name : "null");
            return ok;
        }

        /// <summary>在控件中心派发一次真实点击。</summary>
        public static bool ClickCenter(RectTransform rt, out string detail)
        {
            detail = "";
            Vector2 screen;
            string pos;
            if (!TryCenterScreen(rt, out screen, out pos))
            {
                detail = pos;
                return false;
            }
            List<RaycastResult> hits = Raycast(screen);
            if (hits.Count == 0)
            {
                detail = pos + " | 无命中";
                return false;
            }
            GameObject hitGo = hits[0].gameObject;
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(hitGo);
            if (handler == null)
            {
                detail = pos + " | top=" + Describe(hits) + " | 没有点击处理器";
                return false;
            }
            var ped = new PointerEventData(EventSystem.current);
            ped.position = screen;
            ped.button = PointerEventData.InputButton.Left;
            ped.clickCount = 1;
            ExecuteEvents.Execute(handler, ped, ExecuteEvents.pointerClickHandler);
            detail = pos + " | 点击 " + handler.name + " | top=" + Describe(hits);
            return true;
        }

        /// <summary>射线被遮挡时的兜底：直接在控件自身派发同一点击链。</summary>
        public static bool ClickDirect(RectTransform rt, out string detail)
        {
            detail = "";
            if (rt == null) { detail = "控件为空"; return false; }
            GameObject handler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(rt.gameObject);
            if (handler == null) { detail = "自身没有点击处理器 " + rt.name; return false; }
            Vector2 screen;
            string pos;
            TryCenterScreen(rt, out screen, out pos);
            var ped = new PointerEventData(EventSystem.current);
            ped.position = screen;
            ped.button = PointerEventData.InputButton.Left;
            ped.clickCount = 1;
            ExecuteEvents.Execute(handler, ped, ExecuteEvents.pointerClickHandler);
            detail = pos + " | 直接派发 " + handler.name;
            return true;
        }

        /// <summary>对话三行选项（含未激活的空行）。</summary>
        public static ChieOptionLine[] Options(InCanvasDialogueView dlg)
        {
            DialogueCanvasParts parts = Parts(dlg);
            return parts != null ? parts.options : null;
        }

        /// <summary>选项文字。</summary>
        public static string OptionText(ChieOptionLine line)
        {
            if (line == null) return "";
            // 文字已挪到子物体 Label，先走公开引用，再退回子物体查找
            TextMeshProUGUI t = line.Label;
            if (t == null) t = line.GetComponentInChildren<TextMeshProUGUI>(true);
            return t != null ? t.text : "";
        }

        /// <summary>对话正文当前文字。</summary>
        public static string BodyText(InCanvasDialogueView dlg)
        {
            DialogueCanvasParts parts = Parts(dlg);
            if (parts == null || parts.body == null) return "";
            return parts.body.text;
        }

        /// <summary>结局第 i 行（InCanvas 命名固定）。</summary>
        public static ChieOptionLine EndingLine(int i)
        {
            GameObject go = GameObject.Find("EndingOption_" + i);
            return go != null ? go.GetComponent<ChieOptionLine>() : null;
        }

        /// <summary>工具栏按钮的可点矩形（透明命中框）。</summary>
        public static RectTransform ToolRect(ChieToolButton btn)
        {
            if (btn == null) return null;
            Image hit = btn.Hitbox;
            if (hit != null) return (RectTransform)hit.transform;
            return (RectTransform)btn.transform;
        }

        /// <summary>按工具 id 找工具栏按钮；未解锁时对象不激活，返回 null。</summary>
        public static ChieToolButton ToolButton(ToolId tool)
        {
            ChieToolButton[] all = UnityEngine.Object.FindObjectsOfType<ChieToolButton>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].Tool == tool) return all[i];
            }
            return null;
        }

        /// <summary>结局画布的当前透明度（-1 = 找不到）。</summary>
        public static float EndingAlpha()
        {
            EndingView view = UnityEngine.Object.FindObjectOfType<EndingView>();
            if (view == null || EndingGroupField == null) return -1f;
            CanvasGroup g = EndingGroupField.GetValue(view) as CanvasGroup;
            return g != null ? g.alpha : -1f;
        }

        /// <summary>取屏幕坐标系下的控件中心（供报告记录）。</summary>
        public static string CenterText(RectTransform rt)
        {
            Vector2 screen;
            string detail;
            return TryCenterScreen(rt, out screen, out detail) ? detail : detail;
        }
    }
}
