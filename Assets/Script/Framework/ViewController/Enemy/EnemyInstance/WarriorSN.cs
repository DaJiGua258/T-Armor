using System.Collections;
using QFramework.Utility;
using QFramework.ViewController.Misc;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class WarriorSN : AbstractEnemy
    {
        public override void RotateToTarget(Vector3 targetPos)
        {
            base.RotateToTarget(targetPos);
            RotateWeaponToTarget(targetPos);
        }

        public override void Attack()
        {
            Shoot();
        }

        public override void Shoot()
        {
            Vector3 shootDir = GetShootDirectionWithAccuracy(Muzzle.position, Muzzle.right);
            shootDir.z = 0f;
            float bulletZ = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
            Quaternion bulletRotation = Quaternion.Euler(0f, 0f, bulletZ);

            var bullet = this.GetUtility<IObjectPoolUtility>().GetObject(pf_Bullet, Muzzle.position, bulletRotation);

            ShoottingVFX.Play();

            Projectile bulletComponent = bullet.GetComponent<Projectile>();
            var damageInfo = new DamageInfo(10, 0f, 0f, shootDir, penetration: 3);
            bulletComponent.InitBullet(shootDir, BulletSpeed, damageInfo, gameObject);
            bulletComponent.SetLayerMask(TargetLayerMask);
        }
    }
}
