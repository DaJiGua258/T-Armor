using QFramework.ViewController.FSM;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人路径巡逻状态：沿注入的路径点循环移动。
    /// </summary>
    public class EnemyPatrolState : AbstractState<AbstractEnemy>
    {
        private const float ReachThreshold = 0.25f;

        public EnemyPatrolState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }


        public override void OnEnter()
        {
            Entity.StartPatrolMovement();
        }

        public override void OnUpdate()
        {
            // 保留原有战斗优先级：可攻击 > 可追击 > 巡逻
            if (Entity.IsInAttackMaxRange())
            {
                FSM.ChangeState<EnemyAttackState>();
                return;
            }

            if (Entity.IsInDetectRange())
            {
                FSM.ChangeState<EnemyMoveState>();
                return;
            }

            // 持有有效 target 但超出检测范围 → 仍进入移动状态去追
            if (Entity.Target != null && Entity.Target.gameObject.activeInHierarchy)
            {
                FSM.ChangeState<EnemyMoveState>();
                return;
            }

            // 无巡逻路径 = formation follower，由 FormationController 外部驱动，不覆盖其目标点
            if (!Entity.HasPatrolPath()) return;

            var target = Entity.GetCurrentPatrolPoint();
            Entity.MoveToward(target);

            if (Entity.IsInSpecifiedRange(target, ReachThreshold))
            {
                Entity.AdvancePatrolPoint();
            }
        }

        public override void OnExit()
        {
            Entity.StopMovement();
        }


    }
}