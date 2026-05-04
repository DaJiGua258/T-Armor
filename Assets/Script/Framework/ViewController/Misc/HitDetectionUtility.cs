using QFramework.Command;
using QFramework.Event;
using QFramework.ViewController.Enemy;
using QFramework.ViewController.Player;
using UnityEngine;

namespace QFramework.ViewController
{
    /// <summary>
    /// 命中检测处理器：根据碰撞体标签分发伤害命令与事件。
    /// 调用方自行处理命中 VFX，本类仅负责伤害逻辑。
    /// </summary>
    public static class HitDetectionUtility
    {
        /// <summary>
        /// 对 Collider2D 执行伤害处理（常用于范围爆炸、AOE 等场景）。
        /// </summary>
        public static void ProcessHit(Collider2D collider, int damage)
        {
            if (collider == null) return;

            var tag = collider.gameObject.tag;

            if (tag == "Player")
            {
                var player = collider.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    TArmorArchitecture.Interface.SendCommand(PlayerCommand.Damage.Instance.Init(damage));
                }
                else
                {
                    var friendly = collider.GetComponentInParent<AbstractEnemy>();
                    if (friendly != null)
                    {
                        TArmorArchitecture.Interface.SendCommand(
                            EnemyCommand.Damage.Instance.Init(friendly.enemyId, damage));
                    }
                }
            }
            else if (tag != "Env")
            {
                var enemy = collider.GetComponentInParent<AbstractEnemy>();
                if (enemy == null) return;

                int enemyId = enemy.enemyId;
                TArmorArchitecture.Interface.SendCommand(EnemyCommand.Damage.Instance.Init(enemyId, damage));
                TypeEventSystem.Global.Send(new WeaponInfoEvent.UpdateEnemyInfo());

                if (enemy.EnemyInstanceSystem.GetData(enemyId).CurrentHealth.Value <= 0)
                {
                    TypeEventSystem.Global.Send(new MissionEvent.KillEnemyEvent());
                }

                TypeEventSystem.Global.Send(new DebugEvent.GetEnemyId() { Id = enemyId });
            }
        }

    }
}
