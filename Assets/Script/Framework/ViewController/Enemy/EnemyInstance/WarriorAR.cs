using System.Collections;
using QFramework.Utility;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class WarriorAR : AbstractEnemy
    {
        public override void RotateToTarget(Vector3 targetPos)
        {
            // 保持身体朝向过渡，同时让武器节点进行独立精确瞄准
            base.RotateToTarget(targetPos);
            RotateWeaponToTarget(targetPos);
        }

        public override void Attack()
        {
            BurstAttack();
        }

        public override void Shoot()
        {
            // 基类统一处理“朝向目标 + 准度散布”。
            Vector3 shootDir = GetShootDirectionWithAccuracy(Muzzle.position, Muzzle.right);
            shootDir.z = 0f; // 排除 z 轴深度偏移对 2D 速度的影响
            float bulletZ = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
            Quaternion bulletRotation = Quaternion.Euler(0f, 0f, bulletZ);

            // 子弹朝向与速度方向保持一致，避免视觉朝向和飞行轨迹不一致。
            var bullet = this.GetUtility<IObjectPoolUtility>().GetObject(pf_Bullet, Muzzle.position, bulletRotation);

            ShoottingVFX.Play();

            Projectile bulletComponent = bullet.GetComponent<Projectile>();
            bulletComponent.InitBullet(shootDir, 20, 10);
            bulletComponent.SetLayerMask(TargetLayerMask);
        }
    }
}
