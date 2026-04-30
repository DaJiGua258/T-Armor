using System.Collections;
using QFramework.Utility;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class WarriorAR : AbstractEnemy
    {
        
        [SerializeField] private int _burstCount = 5;  // 一次攻击动作内的连发数量（默认 5 连发）
        [SerializeField] private float _burstInterval = 0.08f;  // 连发间隔，数值越小射速越高
        private bool _isBurstShooting;  // 防止在连发尚未结束时重复开启新的连发协程

        public override void RotateToTarget(Vector3 targetPos)
        {
            // 保持身体朝向过渡，同时让武器节点进行独立精确瞄准
            base.RotateToTarget(targetPos);
            RotateWeaponToTarget(targetPos);
        }

        public override void Attack()
        {
            if(_isBurstShooting) return;
            StartCoroutine(BurstShootRoutine());
        }

        public override void Shoot()
        {
            // 基类统一处理“朝向目标 + 准度散布”。
            Vector3 shootDir = GetShootDirectionWithAccuracy(Muzzle.position, Muzzle.right);
            float bulletZ = Mathf.Atan2(shootDir.y, shootDir.x) * Mathf.Rad2Deg;
            Quaternion bulletRotation = Quaternion.Euler(0f, 0f, bulletZ);

            // 子弹朝向与速度方向保持一致，避免视觉朝向和飞行轨迹不一致。
            var bullet = this.GetUtility<IObjectPoolUtility>().GetObject(Pf_bullet, Muzzle.position, bulletRotation);

            ShoottingVFX.Play();

            Bullet bulletComponent = bullet.GetComponent<Bullet>();
            bulletComponent.InitBullet(shootDir, 20, 10);
        }

        private IEnumerator BurstShootRoutine()
        {
            _isBurstShooting = true;

            // 进行连射
            for (int i = 0; i < _burstCount; i++)
            {
                Shoot();
                if(i < _burstCount - 1)
                {
                    yield return new WaitForSeconds(_burstInterval);
                }
            }

            _isBurstShooting = false;
        }
    }
}
