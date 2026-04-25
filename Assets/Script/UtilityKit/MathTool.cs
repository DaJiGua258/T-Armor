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
        /// 
        /// </summary>
        /// <returns>返回相对于“正对着目标”需要偏转的度数 (Degrees)</returns>
        public static float CalculateLeadAngle2D(Vector2 muzzlePos, float bulletSpeed, Vector2 targetPos, Vector2 targetVelocity)
        {
            // 1. 计算距离
            float distance = Vector2.Distance(muzzlePos, targetPos);

            // 2. 估算飞行时间 (t = d / v)
            float travelTime = distance / bulletSpeed;

            // 3. 预测目标未来位置
            Vector2 predictedPos = targetPos + (targetVelocity * travelTime);

            // 4. 计算当前角度（直接指向目标的角度）
            Vector2 dirToCurrent = targetPos - muzzlePos;
            float currentAngle = Mathf.Atan2(dirToCurrent.y, dirToCurrent.x) * Mathf.Rad2Deg;

            // 5. 计算预测角度（指向预测位置的角度）
            Vector2 dirToPredicted = predictedPos - muzzlePos;
            float predictedAngle = Mathf.Atan2(dirToPredicted.y, dirToPredicted.x) * Mathf.Rad2Deg;

            // 6. 计算差值并规范化角度
            float angleDifference = Mathf.DeltaAngle(currentAngle, predictedAngle);

            return angleDifference;
        }
    }
}