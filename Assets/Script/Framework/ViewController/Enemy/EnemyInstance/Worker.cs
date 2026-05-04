using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class Worker : AbstractEnemy
    {
        [Header("特殊引用")]
        [SerializeField] private ElecShock _light;  // 电击特效引用

        /// <summary>
        /// 执行攻击，触发电击
        /// </summary>
        public override void Attack()
        {
            Shoot();
        }

        /// <summary>
        /// 电击射击：从枪口发射射线到目标，检测路径上的障碍与目标并处理伤害。
        /// </summary>
        public override void Shoot()
        {
            _light.Draw(Target);
            if (Target == null || Muzzle == null) return;

            Vector2 origin = Muzzle.position;
            Vector2 direction = ((Vector2)Target.position - origin).normalized;
            float distance = Vector2.Distance(origin, Target.position);

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, TargetLayerMask);

            if (hit.collider != null)
            {
                HitDetectionUtility.ProcessHit(hit.collider, EnemyInstanceSystem.GetData(enemyId).Damage);
            }
        }

        /// <summary>
        /// 移动时根据速度方向旋转，低速时保持当前朝向
        /// </summary>
        public override void Rotate(Vector3 targetPos)
        {
            Vector2 dir;
            if(Agent.velocity.sqrMagnitude > 0.01f)
            {
                // 跟随状态优先使用实际移动方向
                dir = Agent.velocity.normalized;
            }
            else
            {
                // 低速/静止时沿用 Body 当前朝向，避免缓存方向过期导致折返
                float z = Body.rotation.eulerAngles.z * Mathf.Deg2Rad;
                dir = new Vector2(Mathf.Cos(z), Mathf.Sin(z));
            }

            ApplyRotation(dir);
        }

        /// <summary>
        /// 攻击时强制朝向目标位置
        /// </summary>
        public override void RotateToTarget(Vector3 targetPos)
        {
            Vector2 dir = (targetPos - transform.position).normalized;
            ApplyRotation(dir);
        }

        // 应用平滑旋转到 Body 和 Shadow
        private void ApplyRotation(Vector2 dir)
        {
            if(dir.sqrMagnitude < 0.01f) return;

            float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float currentAngle = Body.rotation.eulerAngles.z;

            // 统一平滑旋转，减少状态切换时瞬时跳角
            float smoothAngle = Mathf.LerpAngle(currentAngle, z, _rotateSpeed * Time.deltaTime);

            Body.rotation = Quaternion.Euler(0, 0, smoothAngle);
            Shadow.rotation = Quaternion.Euler(0, 0, smoothAngle);
        }
    }
}
