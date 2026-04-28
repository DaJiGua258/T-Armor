using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人待机状态。
    /// 进入后在原地等待，计时结束后切换到巡逻移动状态。
    /// 若玩家进入检测范围则立即切换到攻击状态。
    /// </summary>
    public class EnemyIdleState : AbstractState<AbstractEnemy>
    {
        private float _idleTimer;
        private float _idleDuration;

        public EnemyIdleState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            Entity.StopMovement();
            _idleDuration = 0.5f;
            _idleTimer = 0f;
        }

        public override void OnUpdate()
        {
            _idleTimer += Time.deltaTime;
            if(_idleTimer < _idleDuration) return;

            // ----- 冷却时间结束后执行 -------------------------

            if(Entity.IsInDetectRange())
            {
                FSM.ChangeState<EnemyMoveState>();
            }

            if(Entity.IsInAttackMaxRange())
            {
                FSM.ChangeState<EnemyAttackState>();
            }
        }

    }
}
