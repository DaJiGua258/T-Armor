using UnityEngine;

namespace QFramework.ViewController.Misc
{
    /// <summary>
    /// 定点打击子弹：飞向目标终点，碰撞或到达终点时爆炸。
    /// </summary>
    public class BulletPointed : AbstractBullet
    {
        [Header("终点参数")]
        [SerializeField] private float _arrivalThreshold = 1f;  // 到达终点的判定距离

        private Vector3 _targetPosition;  // 目标终点

        /// <summary>
        /// 初始化定点飞行参数。
        /// </summary>
        /// <param name="targetPosition">目标终点（世界坐标）</param>
        /// <param name="speed">飞行速度</param>
        /// <param name="damage">命中伤害</param>
        public void InitProjectile(Vector3 targetPosition, int speed, int damage)
        {
            _targetPosition = targetPosition;
            InitBullet((targetPosition - transform.position).normalized, speed, damage);
        }

        protected override void Detect()
        {
            Vector2 velocity = _rb.velocity;
            if (velocity.sqrMagnitude <= Mathf.Epsilon) return;

            float distance = velocity.magnitude * Time.deltaTime + 0.5f;
            Vector2 origin = transform.position;
            Vector2 direction = velocity.normalized;

            // 射线检测飞行路径上的碰撞体
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, _layerMask);
            Debug.DrawRay(origin, direction * distance, Color.yellow);

            bool hasCollision = hit.collider != null;
            // 检测是否到达终点
            bool hasArrived = Vector2.Distance(transform.position, _targetPosition) <= _arrivalThreshold;

            if (!hasCollision && !hasArrived) return;

            // 优先使用碰撞点，无碰撞时使用目标终点
            Vector3 explosionPoint = hasCollision ? hit.point : _targetPosition;

            // 碰撞命中才造成伤害（到达终点让爆炸 VFX 处理范围伤害）
            if (hasCollision)
                HitDetectionUtility.ProcessHit(hit.collider, _damage);

            Explode(explosionPoint);
        }

        /// <summary>
        /// 定点弹的爆炸始终将右方向转到上方向（忽略子弹飞行倾角）。
        /// </summary>
        protected override Quaternion GetExplosionRotation()
        {
            return Quaternion.Euler(0f, 0f, 90f);
        }
    }
}
