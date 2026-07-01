using QFramework.Enum;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class SG : AbstractWeapon
    {
        public override SFXType ShootSFXType => SFXType.weapon_shoot_sg;

        public override void ShootDetal()
        {
            float halfSpread = WeaponDataModel.SpreadAngle * 0.5f;

            for (int i = 0; i < 8; i++)
            {
                Vector3 shootDir = Muzzle.right;
                float randomAngle = Random.Range(-halfSpread, halfSpread);
                shootDir = Quaternion.Euler(0, 0, randomAngle) * shootDir;

                SpawnBullet(shootDir);
            }
        }
    }
}
