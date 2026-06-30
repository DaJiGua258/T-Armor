using System.Collections;
using QFramework.Utility;
using QFramework.ViewController.Misc;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 突袭型空中敌人，轻型炮艇。
    /// 使用 AirEnemy 的空中 MoveToward 追逐玩家，在攻击范围内进行连射。
    /// </summary>
    public class Raider : AbstractAirEnemy
    {
        /// <summary>
        /// 突袭连射
        /// </summary>
        public override void Attack()
        {
            BurstAttack();
        }

        /// <summary>
        /// 单发射击
        /// </summary>
        public override void Shoot()
        {
            Vector3 shootDir = GetShootDirectionWithAccuracy(Muzzle.position, Muzzle.right);
            shootDir.z = 0f;
            float bulletZ = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
            Quaternion bulletRotation = Quaternion.Euler(0f, 0f, bulletZ);

            var bullet = this.GetUtility<IObjectPoolUtility>().GetObject(pf_Bullet, Muzzle.position, bulletRotation);
            ShoottingVFX.Play();

            var bulletComponent = bullet.GetComponent<Projectile>();
            var damageInfo = new DamageInfo(10, 0f, 0f, shootDir);
            bulletComponent.InitBullet(shootDir, 20, damageInfo, gameObject);
            bulletComponent.SetLayerMask(TargetLayerMask);
        }

        protected override void Update()
        {
            base.Update();

            if (EnemyInstanceSystem.GetData(enemyId).CurrentHealth.Value > 0)
                AirFloat();
        }

        public override void RotateToTarget(Vector3 targetPos)
        {
            base.RotateToTarget(targetPos);
            RotateWeaponToTarget(targetPos);
        }
    }
}
