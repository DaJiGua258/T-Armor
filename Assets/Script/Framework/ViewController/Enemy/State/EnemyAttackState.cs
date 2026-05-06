using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人攻击状态。
    /// 条件：玩家必须在攻击范围内才允许进入（OnCondition）。
    /// 每隔一段冷却时间对玩家造成伤害；玩家离开范围后返回待机状态。
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
            // Debug.Log("进入攻击状态");
            return Entity.IsInAttackMaxRange();
        }

        public override void OnEnter()
        {
            Entity.SetAgentRvoLocked(true);
        }

        public override void OnUpdate()
        {
            // 战斗中定期刷新到最近目标
            Entity.RefreshTargetInCombat();

            // ----- 如果不在攻击范围内，则返回待机状态 -------------------------
            if(!Entity.IsInAttackMaxRange())
            {
                FSM.ChangeState<EnemyIdleState>();
                return;
            }

            // ----- 攻击 -------------------------
            Entity.RotateToTarget(Entity.Target.position);

            _attackTimer += Time.deltaTime;
            if(_attackTimer > _attackCooldown)
            {
                Entity.Attack();
                _attackTimer = 0f;
            }
        }

        public override void OnExit()
        {
            Entity.SetAgentRvoLocked(false);
        }
    }
}
