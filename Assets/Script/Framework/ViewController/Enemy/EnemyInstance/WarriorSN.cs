using System.Collections;
using QFramework.Utility;
using QFramework.ViewController.Misc;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class WarriorSN : AbstractEnemy
    {
        [Header("攻击提示")]
        public LineRenderer AttackWarningLine;

        public override void RotateToTarget(Vector3 targetPos)
        {
            base.RotateToTarget(targetPos);
            RotateWeaponToTarget(targetPos);
        }

        public override void Attack()
        {
            StartCoroutine(AttackWarningRoutine());
        }

        private IEnumerator AttackWarningRoutine()
        {
            const float maxLength = 15f;
            AttackWarningLine.enabled = true;
            AttackWarningLine.positionCount = 2;
            AttackWarningLine.useWorldSpace = true;

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;

                Vector3 origin = AttackWarningLine.transform.position;
                Vector3 dir;
                float dist;

                if (Target != null)
                {
                    dir = (Target.position - origin).normalized;
                    dist = Mathf.Min(Vector3.Distance(origin, Target.position), maxLength);
                }
                else
                {
                    dir = Muzzle.right;
                    dist = maxLength;
                }

                AttackWarningLine.SetPosition(0, origin);
                AttackWarningLine.SetPosition(1, origin + dir * dist);

                yield return null;
            }

            AttackWarningLine.enabled = false;
            Shoot();
        }

        public override void Shoot()
        {
            if (AttackWarningLine != null)
                AttackWarningLine.enabled = false;

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
