using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人攻击状态。
    /// 条件：玩家必须在攻击范围内才允许进入（OnCondition）。
    /// 每隔一段冷却时间对玩家造成伤害；玩家离开范围后返回待机状态。
    /// </summary>
    public class EnemyAttackState : AbstractState<EnemyController>
    {
        private float _attackCooldown = 1.5f;
        private float _attackTimer;

        public EnemyAttackState(EnemyController owner, StateMachine<EnemyController> fsm)
            : base(owner, fsm) { }

        /// <summary>仅当玩家在攻击范围内时才允许进入攻击状态。</summary>
        public override bool OnCondition()
        {
            return Entity.IsPlayerInRange(Entity.AttackRange);
        }

        public override void OnEnter()
        {
            _attackTimer = _attackCooldown;
            Entity.StopMovement();
        }

        public override void OnUpdate()
        {
            if (!Entity.IsPlayerInRange(Entity.DetectionRange))
            {
                FSM.ChangeState<EnemyIdleState>();
                return;
            }

            _attackTimer += Time.deltaTime;
            if (_attackTimer >= _attackCooldown)
            {
                _attackTimer = 0f;
                Entity.PerformAttack();
            }
        }
    }
}
