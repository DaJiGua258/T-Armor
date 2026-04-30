using QFramework.Command;
using QFramework.System;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class Worker : AbstractEnemy
    {
        [Header("特殊引用")]
        [SerializeField] private ElecShock _light;
        // 速度过低时沿用上一次有效移动方向，避免 Idle -> Move 首帧抖动
        private Vector2 _cachedMoveDir = Vector2.right;

        public override void Attack()
        {
            Shoot();
        }

        public override void Shoot()
        {
            _light.Draw(Target);
            if(Target.TryGetComponent<PlayerController>(out PlayerController c))
            {
                this.SendCommand(PlayerCommand.Damage.Instance.Init(EnemyInstanceSystem.GetData(enemyId).Damage));
            }
            // var obj = Instantiate(_pf_bullet, Muzzle.position, Muzzle.rotation);
        }

        public override void Rotate(Vector3 targetPos)
        {
            Vector2 dir;
            if(Agent.velocity.sqrMagnitude > 0.01f)
            {
                // 跟随状态优先使用实际移动方向
                dir = Agent.velocity.normalized;
                _cachedMoveDir = dir;
            }
            else
            {
                // 低速/静止时保持最近有效方向，避免方向源突变
                dir = _cachedMoveDir;
            }

            ApplyRotation(dir);
        }

        public override void RotateToTarget(Vector3 targetPos)
        {
            // 攻击状态下强制朝向目标
            Vector2 dir = (targetPos - transform.position).normalized;
            ApplyRotation(dir);
        }

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
