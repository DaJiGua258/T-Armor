using QFramework.ViewController.FSM;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 空投船死亡状态骨架。
    /// </summary>
    public class DropperDeathState : AbstractState<AbstractEnemy>
    {
        public DropperDeathState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            Entity.StopMovement();
            Entity.ShowDeathVFX();
        }
    }
}
