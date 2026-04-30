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

        public override void OnUpdate()
        {
            var dropper = Entity as Dropper;
            if (dropper == null) return;
            if (!dropper.IsRouteReady()) return;

            Entity.MoveToward(dropper.GetCurrentRouteTarget());

            if (!dropper.IsReachCurrentRouteTarget()) return;
            if (Entity.Rb != null && Entity.Rb.velocity.sqrMagnitude >= 0.01f) return;

            if (!dropper.IsDropped)
            {
                FSM.ChangeState<DropperDroppingState>();
                return;
            }

            dropper.DespawnAtRouteEnd();
        }
    }
}
