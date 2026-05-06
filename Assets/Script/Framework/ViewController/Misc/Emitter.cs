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

        private Vector2 SpawnPoint => (Vector2)transform.position + SpawnOffset;

        /// <summary>
        /// 按轮次发射子弹，每轮的所有子弹发射完后再进入下一轮
        /// </summary>
        [ContextMenu("Fire")]
        public void Fire()
        {
            StartCoroutine(FireRoutine());
        }

        private IEnumerator FireRoutine()
        {
            if (bulletPrefab == null) yield break;

            for (int round = 0; round < RoundCount; round++)
            {
                // 首轮不等待，后续每轮按间隔延时
                if (round > 0)
                    yield return new WaitForSeconds(RoundInterval);

                // 发射当前轮次的所有子弹
                for (int i = 0; i < BulletsPerRound; i++)
                {
                    SpawnBullet();

                    // 最后一发不等待
                    if (i < BulletsPerRound - 1)
                        yield return new WaitForSeconds(BulletInterval);
                }
            }
        }

        // 生成一颗子弹并初始化
        private void SpawnBullet()
        {
            Vector2 spawnPos = SpawnPoint;

            // 在散布范围内随机选取落点
            Vector2 targetPos = SpreadRadius > 0f
                ? (Vector2)transform.position + (Vector2)Random.insideUnitCircle * SpreadRadius
                : (Vector2)transform.position;

            var bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);
            var projectile = bullet.GetComponent<Projectile>();
            if (projectile == null) return;

            Vector2 dir = (targetPos - spawnPos).normalized;
            bullet.transform.right = dir;

            projectile.SetDetectMode(ProjectileMode.PointOnly);
            projectile.SetLayerMask(BulletLayerMask);
            projectile.InitProjectile(targetPos, (int)Speed, Damage);
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
