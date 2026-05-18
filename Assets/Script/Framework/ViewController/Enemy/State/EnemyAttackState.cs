using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人攻击状态。
    /// 条件：玩家必须在攻击范围内才允许进入（OnCondition）。
    /// 每隔一段冷却时间对玩家造成伤害；每次攻击结束后检测脱离，冷却期间不回空闲状态。
    /// </summary>
    public class EnemyAttackState : AbstractState<AbstractEnemy>
    {
        private float _attackCooldown = 1.5f;
        private float _attackTimer;

        public EnemyAttackState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        /// <summary>仅当玩家在攻击范围内时才允许进入攻击状态。</summary>
        public override bool OnCondition()
        {
            return Entity.IsInAttackMaxRange();
        }

        public override void OnEnter()
        {
            Entity.SetAgentRvoLocked(true);
            _attackTimer = _attackCooldown;
        }

        public override void OnUpdate()
        {
            Entity.RefreshTargetInCombat();
            Entity.RotateToTarget(Entity.Target.position);

            _attackTimer += Time.deltaTime;
            if (_attackTimer < _attackCooldown) return;

            // ----- 攻击 -------------------------
            Entity.Attack();
            _attackTimer = 0f;

            // ----- 攻击后检测脱离 -------------------------
            if (!Entity.IsInAttackMaxRange())
            {
                FSM.ChangeState<EnemyIdleState>();
            }
        }

        public override void OnExit()
        {
            Entity.SetAgentRvoLocked(false);
        }
    }
}
