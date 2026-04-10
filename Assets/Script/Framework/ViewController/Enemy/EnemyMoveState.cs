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
            PickRandomTarget();
        }

        public override void OnUpdate()
        {
            if (Entity.IsPlayerInRange(Entity.DetectionRange))
            {
                FSM.ChangeState<EnemyAttackState>();
                return;
            }

            MoveToTarget();

            if (Vector2.Distance(Entity.transform.position, _targetPos) < _arrivedThreshold)
            {
                FSM.ChangeState<EnemyIdleState>();
            }
        }

        public override void OnExit()
        {
            Entity.StopMovement();
        }

        private void PickRandomTarget()
        {
            Vector2 origin = Entity.transform.position;
            Vector2 offset = Random.insideUnitCircle * _patrolRadius;
            _targetPos = origin + offset;
        }

        private void MoveToTarget()
        {
            Vector2 dir = (_targetPos - (Vector2)Entity.transform.position).normalized;
            Entity.MoveToward(dir);
        }
    }
}
