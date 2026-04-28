using QFramework.Utility;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class WarriorAR : AbstractEnemy
    {
        public override void Attack()
        {
            Shoot();
        }

        public override void Shoot()
        {
            Vector3 shootDir = Muzzle.right; // local right 是2D武器的默认枪口方向 (一般为右)

            var bullet = this.GetUtility<IObjectPoolUtility>().GetObject(Pf_bullet, Muzzle.position, Muzzle.rotation);

            ShoottingVFX.Play();

            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            bulletComponent.InitBullet(shootDir, 20, 10);
        }
    }
}
