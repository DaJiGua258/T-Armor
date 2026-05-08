using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class SG : AbstractWeapon
    {
        public override void ShootDetal()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 shootDir = Muzzle.right;
                float randomAngle = Random.Range(-5f, 5f);
                shootDir = Quaternion.Euler(0, 0, randomAngle) * shootDir;

                SpawnBullet(shootDir);
            }
        }
    }
}
