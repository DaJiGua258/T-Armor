using QFramework.Command;
using QFramework.Enum;
using QFramework.Event;
using QFramework.Manager;
using QFramework.ViewController.Enemy;
using QFramework.ViewController.Misc;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController
{
    /// <summary>
    /// 命中检测处理器：封装子弹射线检测与伤害分发逻辑。
    /// 调用方自行处理命中 VFX，本类仅负责检测与伤害逻辑。
    /// </summary>
    public static class HitDetectionUtility
    {
        /// <summary>
        /// 执行子弹的多重射线检测（RaycastNonAlloc），返回命中数量。
        /// 适用于多个碰撞箱重叠的场景，确保不会漏检。零分配。
        /// </summary>
        public static int BulletRaycastAll(Vector2 origin, Vector2 direction, float speed, LayerMask layerMask, float minDistance, Color debugColor, RaycastHit2D[] results)
        {
            bool hasVelocity = speed > Mathf.Epsilon;
            if (!hasVelocity && minDistance <= 0f) return 0;

            float distance = minDistance;
            direction = direction.normalized;

            if (hasVelocity)
            {
                float velDistance = speed * Time.deltaTime + 0.5f;
                distance = Mathf.Max(minDistance, velDistance);
            }

            int hitCount = Physics2D.RaycastNonAlloc(origin, direction, results, distance, layerMask);

            // 绘制调试射线
            if (hasVelocity && distance > minDistance && minDistance > 0f)
            {
                Debug.DrawRay(origin, direction * minDistance, new Color(debugColor.r, debugColor.g, debugColor.b, 0.3f));
                Debug.DrawRay(origin + (Vector2)(direction * minDistance), direction * (distance - minDistance), debugColor);
            }
            else
            {
                Debug.DrawRay(origin, direction * distance, debugColor);
            }

            return hitCount;
        }

        /// <summary>
        /// 执行子弹的射线检测，返回命中的碰撞体。
        /// </summary>
        public static RaycastHit2D BulletRaycast(Vector2 origin, Vector2 direction, float speed, LayerMask layerMask, float minDistance, Color debugColor)
        {
            bool hasVelocity = speed > Mathf.Epsilon;
            if (!hasVelocity && minDistance <= 0f) return default;

            float distance = minDistance;
            direction = direction.normalized;

            if (hasVelocity)
            {
                float velDistance = speed * Time.deltaTime + 0.5f;
                distance = Mathf.Max(minDistance, velDistance);
            }

            RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, layerMask);

            // 最小距离段用半透明绘制，速度延伸段用实色
            if (hasVelocity && distance > minDistance && minDistance > 0f)
            {
                Debug.DrawRay(origin, direction * minDistance, new Color(debugColor.r, debugColor.g, debugColor.b, 0.3f));
                Debug.DrawRay(origin + (Vector2)(direction * minDistance), direction * (distance - minDistance), debugColor);
            }
            else
            {
                Debug.DrawRay(origin, direction * distance, debugColor);
            }

            return hit;
        }

        /// <summary>
        /// 对 Collider2D 执行伤害处理（常用于范围爆炸、AOE 等场景）。
        /// </summary>
        public static void ProcessHit(Collider2D collider, DamageInfo damageInfo)
        {
            if (collider == null) return;

            var tag = collider.gameObject.tag;

            if (tag == "Player")
            {
                var player = collider.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    TArmorArchitecture.Interface.SendCommand(PlayerCommand.Damage.Instance.Init(damageInfo.Damage));
                    TypeEventSystem.Global.Send(new StatsEvent.OnDamageTaken
                    {
                        Damage = damageInfo.Damage,
                        CurrentHealth = player.PlayerModel.CurrentHealth.Value
                    });
                }
                else
                {
                    var friendly = collider.GetComponentInParent<AbstractEnemy>();
                    if (friendly != null)
                    {
                        TArmorArchitecture.Interface.SendCommand(
                            EnemyCommand.Damage.Instance.Init(friendly.enemyId, damageInfo.Damage));
                    }
                }
            }
            else if (tag == "DesEnv")
            {
                var destructible = collider.GetComponentInParent<DestructibleEnv>();
                if (destructible != null)
                    destructible.TakeDamage(damageInfo.Damage);
            }
            else if (tag != "Env")
            {
                var enemy = collider.GetComponentInParent<AbstractEnemy>();
                if (enemy == null) return;

                int enemyId = enemy.enemyId;
                TArmorArchitecture.Interface.SendCommand(EnemyCommand.Damage.Instance.Init(enemyId, damageInfo.Damage));
                TypeEventSystem.Global.Send(new WeaponInfoEvent.UpdateEnemyInfo());

                // 受击音效
                AudioManager.Instance.PlaySFX(SFXType.enemy_hit, collider.transform.position);

                // 异常状态效果
                if (damageInfo.KnockbackValue > 0f)
                    enemy.AddKnockback(damageInfo.KnockbackValue, damageInfo.AttackDirection);
                if (damageInfo.BurnValue > 0f)
                    enemy.AddBurn(damageInfo.BurnValue, damageInfo.Damage);
                if (damageInfo.SlowValue > 0f)
                    enemy.AddSlow(damageInfo.SlowValue);

                if (enemy.EnemyInstanceSystem.GetData(enemyId).CurrentHealth.Value <= 0)
                {
                    // 死亡事件移至 EnemyDeathState 发送
                }
            }
        }

    }
}
