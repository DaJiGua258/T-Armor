using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 空投船移动状态骨架。
    /// 这里可以做追踪玩家、移动到投放点、编队飞行等逻辑。
    /// </summary>
    public class DropperMoveState : AbstractState<AbstractEnemy>
    {
        public DropperMoveState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            // TODO: 进入移动状态时的初始化
        }

        public override void OnUpdate()
        {
            if (Entity.Target != null)
            {
                Entity.MoveToward(Entity.Target.position);
            }

            // TODO: 到达投放条件后切换到投放状态
            if (Entity.IsInAttackMaxRange())
            {
                FSM.ChangeState<DropperDroppingState>();
            }
        }

        public override void OnExit()
        {
            Entity.StopMovement();
        }
    }
}
