using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人巡逻移动状态。
    /// 在一定范围内随机选取目标点并向其移动。
    /// 到达目标点后切换到待机状态；若发现玩家则立即切换到攻击状态。
    /// </summary>
    public class EnemyMoveState : AbstractState<EnemyController>
    {
        private Vector2 _targetPos;
        private float _arrivedThreshold = 0.2f;
        private float _patrolRadius = 5f;

        public EnemyMoveState(EnemyController owner, StateMachine<EnemyController> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            


        }

        public override void OnUpdate()
        {
            // 追逐过程中，超出范围返回待机状态
            if(!Entity.IsTargetInRange(Entity.DetectionRange))
            {
                FSM.ChangeState<EnemyIdleState>();
            }

            Entity.MoveToward();
        }

        public override void OnExit()
        {
            Entity.StopMovement();
        }

    }
}
