using QFramework.ViewController.FSM;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人路径巡逻状态：沿注入的路径点循环移动。
    /// </summary>
    public class EnemyPatrolState : AbstractState<AbstractEnemy>
    {
        private const float ReachThreshold = 1f;

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

            if (!Entity.HasPatrolPath())
            {
                FSM.ChangeState<EnemyIdleState>();
                return;
            }

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