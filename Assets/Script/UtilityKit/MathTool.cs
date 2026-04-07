using UnityEngine;

namespace QFramework.UtilityKit
{
    public static class MathTool
    {
        public static float GetAngleDifference(float a, float b)
        {
            // 1. 计算原始差值并对 360 取模
            float diff = (a - b) % 360f;

            // 2. 将结果映射到 (-360, 360) 之间（处理负数取模的情况）
            if (diff < -180f) diff += 360f;
            if (diff > 180f) diff -= 360f;

            // 3. 返回绝对值，即为无视方向的最小差值
            return Mathf.Abs(diff);
        }
    }
}