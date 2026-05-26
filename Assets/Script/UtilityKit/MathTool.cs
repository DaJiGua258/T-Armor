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

        /// <summary>
        /// 计算武器预瞄提前量（迭代收敛版）
        /// </summary>
        /// <returns>返回相对于”正对着目标”需要偏转的度数 (Degrees)</returns>
        public static float CalculateLeadAngle2D(Vector2 muzzlePos, float bulletSpeed, Vector2 targetPos, Vector2 targetVelocity)
        {
            if (bulletSpeed <= 0f) return 0f;

            Vector2 relPos = targetPos - muzzlePos;

            // 二次方程系数 at² + bt + c = 0
            float a = bulletSpeed * bulletSpeed - Vector2.Dot(targetVelocity, targetVelocity);
            float b = -2f * Vector2.Dot(relPos, targetVelocity);
            float c = -Vector2.Dot(relPos, relPos);

            float t = -1f;

            if (Mathf.Abs(a) < 1e-6f)
            {
                // a ≈ 0，退化为线性方程
                if (Mathf.Abs(b) > 1e-6f)
                    t = -c / b;
            }
            else
            {
                float discriminant = b * b - 4f * a * c;
                if (discriminant < 0f) return 0f; // 子弹追不上目标

                float sqrtD = Mathf.Sqrt(discriminant);
                float t1 = (-b + sqrtD) / (2f * a);
                float t2 = (-b - sqrtD) / (2f * a);

                // 取最小正数解（最近的拦截时间）
                if (t1 > 0f && t2 > 0f)
                    t = Mathf.Min(t1, t2);
                else
                    t = Mathf.Max(t1, t2);
            }

            if (t <= 0f) return 0f; // 无有效解

            // 计算预测位置与角度差
            Vector2 predictedPos = targetPos + targetVelocity * t;
            Vector2 dirToCurrent   = relPos;
            Vector2 dirToPredicted = predictedPos - muzzlePos;

            float currentAngle   = Mathf.Atan2(dirToCurrent.y,   dirToCurrent.x)   * Mathf.Rad2Deg;
            float predictedAngle = Mathf.Atan2(dirToPredicted.y, dirToPredicted.x) * Mathf.Rad2Deg;

            return Mathf.DeltaAngle(currentAngle, predictedAngle);
        }
    }
}