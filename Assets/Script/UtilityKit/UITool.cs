using UnityEngine;
using UnityEngine.UI;

namespace QFramework.UtilityKit
{
    public static class UITool
    {
        /// <summary>
        /// 强制递归刷新layout
        /// </summary>
        public static void ForceRebuildFormRoot(RectTransform root)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(root);
            
            foreach (RectTransform child in root)
            {
                ForceRebuildFormRoot(child);
            }
        }

        public static Vector2 WorldToCanvasPoint(RectTransform canvas, Vector3 worldPos)
        {
            // 第一步：世界坐标 → 屏幕坐标
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            // 第二步：屏幕坐标 → Canvas本地坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas,
                screenPos,
                null,        // Overlay传null
                out Vector2 localPoint
            );

            return localPoint;
        }

        public static Vector2 ScreenToCanvasPoint(RectTransform canvas, Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas, 
                Input.mousePosition,
                null, // 如果是 Overlay 模式内部会自动处理
                out Vector2 localPoint);
            return localPoint;
        }
    }
}