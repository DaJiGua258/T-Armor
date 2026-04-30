using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人巡逻移动状态。
    /// 在一定范围内随机选取目标点并向其移动。
    /// 到达目标点后切换到待机状态；若发现玩家则立即切换到攻击状态。
    /// </summary>
    public class EnemyMoveState : AbstractState<AbstractEnemy>
    {
        private float _arrivedThreshold = 0.2f;
        private float _patrolRadius = 5f;
        private float _curAttackRange = 1;

        public EnemyMoveState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            Entity.StartMovement();

            // ----- 随机生成攻击范围 -------------------------
            _curAttackRange = Random.Range((float)Entity.AttackMinRange, (float)Entity.AttackMaxRange);

            Debug.Log("进入移动状态");
            Debug.Log("攻击范围: " + _curAttackRange);
        }

        public override void OnUpdate()
        {
            // 追逐过程中，超出范围返回待机状态
            if(!Entity.IsInDetectRange()) 
            {
                FSM.ChangeState<EnemyIdleState>();
                return;
            }

            if(Entity.IsInSpecifiedRange(_curAttackRange)) 
            {
                FSM.ChangeState<EnemyAttackState>();
                return;
            }
  

            Entity.MoveToward(Entity.Target.position);
        }

        public override void OnExit()
        {
            Entity.StopMovement();
        }

    }
}
