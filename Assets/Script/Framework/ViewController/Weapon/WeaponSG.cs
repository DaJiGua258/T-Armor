using QFramework.Utility;
using UnityEngine;
using QFramework.ViewController.Misc;

namespace QFramework.ViewController.Player
{
    public class WeaponSG : AbstractWeapon
    {
        public override void ShootDetal()
        {
            for (int i = 0; i < 8; i++)
            {
                    // 计算发射方向：与武器朝向一致
                Vector3 shootDir = Muzzle.right; // local right 是2D武器的默认枪口方向 (一般为右)
                float spread = 5f; // 偏移角度范围（度）
                float randomAngle = Random.Range(-spread, spread);

                // 创建旋转角度，并作用于初始方向
                shootDir = Quaternion.Euler(0, 0, randomAngle) * shootDir;
                
                // 枪口世界坐标

                // 创建子弹和射击特效
                GameObject bullet = this.GetUtility<IObjectPoolUtility>().GetObject(_pf_bullet, Muzzle.position, Muzzle.rotation);
                _vfxShooting.Play();

                AbstractBullet bulletComponent = bullet.GetComponent<AbstractBullet>();
                bulletComponent.InitBullet(shootDir, WeaponDataModel.BulletSpeed, WeaponDataModel.BulletDamage);
            }
        }
    }
}