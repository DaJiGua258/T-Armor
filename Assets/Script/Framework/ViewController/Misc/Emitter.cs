using System;
using System.Collections;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController.Misc
{
    /// <summary>
    /// 定点发射器：在 SpawnOffset 处生成子弹，朝自身位置飞行。
    /// </summary>
    public class Emitter : MonoBehaviour
    {
        [Header("子弹预制体")]
        public GameObject bulletPrefab;

        [Header("发射参数")]
        public float Speed = 20f;
        public int Damage = 1;

        [Header("连发参数")]
        public int RoundCount = 1;  // 发射轮次
        public float RoundInterval = 0.5f;  // 每轮次间隔时间（秒）
        public int BulletsPerRound = 1;  // 每轮次数量
        public float BulletInterval = 0.1f;  // 数量之间的间隔时间（秒）

        [Header("生成坐标偏移")]
        public Vector2 SpawnOffset = new Vector2(5f, 0f);

        [Header("散布范围")]
        public float SpreadRadius = 0f;  // 落点随机散布半径，0 为全部聚集到中心

        [Header("爆炸检测")]
        public LayerMask BulletLayerMask;  // 子弹爆炸时检测的 LayerMask

        [Header("追踪")]
        public bool EnableHoming;  // 是否开启追踪（会向发射器位置弯曲）

        [Header("运行状态")]
        public Action OnFireStarted;
        public Action OnFireEnded;
        public Action OnAllBulletsLanded;

        public bool IsFiring { get; private set; }

        private int _totalBulletCount;
        private int _explodedBulletCount;

        private Vector2 SpawnPoint => (Vector2)transform.position + SpawnOffset;

        /// <summary>
        /// 按轮次发射子弹，每轮的所有子弹发射完后再进入下一轮
        /// </summary>
        [ContextMenu("Fire")]
        public void Fire()
        {
            if (IsFiring) return;
            IsFiring = true;
            OnFireStarted?.Invoke();
            StartCoroutine(FireRoutine());
        }

        private IEnumerator FireRoutine()
        {
            _totalBulletCount = RoundCount * BulletsPerRound;
            _explodedBulletCount = 0;

            try
            {
                if (bulletPrefab == null) yield break;

                for (int round = 0; round < RoundCount; round++)
                {
                    // 首轮不等待，后续每轮按间隔延时
                    if (round > 0)
                        yield return new WaitForSeconds(RoundInterval);

                    // 每轮开始时预随机所有子弹的落点位置（基于当前发射器位置）
                    Vector2[] roundTargets = new Vector2[BulletsPerRound];
                    Vector2 currentCenter = transform.position;
                    for (int i = 0; i < BulletsPerRound; i++)
                    {
                        roundTargets[i] = SpreadRadius > 0f
                            ? currentCenter + (Vector2)UnityEngine.Random.insideUnitCircle * SpreadRadius
                            : currentCenter;
                    }

                    // 发射当前轮次的所有子弹
                    for (int i = 0; i < BulletsPerRound; i++)
                    {
                        SpawnBullet(roundTargets[i]);

                        // 最后一发不等待
                        if (i < BulletsPerRound - 1)
                            yield return new WaitForSeconds(BulletInterval);
                    }
                }
            }
            finally
            {
                IsFiring = false;
                OnFireEnded?.Invoke();
            }
        }

        // 生成一颗子弹并初始化
        private void SpawnBullet(Vector2 targetPos)
        {
            Vector2 spawnPos = SpawnPoint;

            var bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            var projectile = bullet.GetComponent<Projectile>();
            if (projectile == null) return;

            projectile.OnExploded += OnBulletExploded;

            Vector2 dir = (targetPos - spawnPos).normalized;
            bullet.transform.right = dir;

            projectile.SetDetectMode(ProjectileMode.PointOnly);
            projectile.SetLayerMask(BulletLayerMask);
            var damageInfo = new DamageInfo(Damage, 0f, 0f, dir);
            projectile.InitProjectile(targetPos, (int)Speed, damageInfo);

            if (EnableHoming)
                projectile.SetHomingTarget(transform);
        }

        private void OnBulletExploded(Projectile projectile)
        {
            projectile.OnExploded -= OnBulletExploded;
            _explodedBulletCount++;

            if (_explodedBulletCount >= _totalBulletCount)
                OnAllBulletsLanded?.Invoke();
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 spawnPos = SpawnPoint;
            Vector2 center = transform.position;

            // 生成点
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(spawnPos, 0.3f);

            // 连线（生成点 → 目标点）
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(spawnPos, center);

            // 散布范围
            if (SpreadRadius > 0f)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.15f);
                Gizmos.DrawWireSphere(center, SpreadRadius);
            }

            // 飞行方向
            Vector2 dir = (center - spawnPos).normalized;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(spawnPos, dir * 1f);
        }
    }
}
