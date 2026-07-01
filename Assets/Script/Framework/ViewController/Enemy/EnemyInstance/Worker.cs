using QFramework.ViewController.Misc;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class Worker : AbstractEnemy
    {
        private static readonly RaycastHit2D[] _hitCache = new RaycastHit2D[8];

        [Header("特殊引用")]
        [SerializeField] private ElecShock _light;  // 电击特效引用

        // 近战单位无视遮挡
        public override bool HasLineOfSightToTarget() => true;

        /// <summary>
        /// 执行攻击，触发电击
        /// </summary>
        public override void Attack()
        {
            Shoot();
        }

        /// <summary>
        /// 近战电击：从枪口到目标做射线检测判定命中。
        /// </summary>
        public override void Shoot()
        {
            _light.Draw(Target);
            if (Target == null || Muzzle == null) return;

            Vector2 origin = Muzzle.position;
            Vector2 direction = ((Vector2)Target.position - origin).normalized;
            float distToTarget = Vector2.Distance(origin, Target.position);

            // 射线起点回退 + 远端延伸：
            // - 回退：避免 Muzzle 在目标碰撞体内部时，Physics2D.Raycast 不检测该碰撞体
            // - 延伸：避免 Target.position 未对齐碰撞体实际中心时射线终点够不到碰撞体表面
            const float backOffset = 0.5f;
            const float forwardExtra = 0.5f;
            Vector2 rayOrigin = origin - direction * backOffset;
            float rayDistance = distToTarget + backOffset + forwardExtra;

            int hitCount = Physics2D.RaycastNonAlloc(rayOrigin, direction, _hitCache, rayDistance, TargetLayerMask);

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _hitCache[i];
                if (hit.collider.transform.IsChildOf(transform)) continue;

                // 跳过非战斗碰撞体（如 MovingBox），继续检测后续是否有 Player/Enemy 碰撞体
                string tag = hit.collider.tag;
                if (tag != "Player" && tag != "Enemy" && tag != "DesEnv") continue;

                Vector2 attackDir = (Target.position - Muzzle.position).normalized;
                int dmg = EnemyInstanceSystem.GetData(enemyId).Damage;
                var damageInfo = new DamageInfo(dmg, 0f, 0f, attackDir);
                HitDetectionUtility.ProcessHit(hit.collider, damageInfo);
                break;
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
