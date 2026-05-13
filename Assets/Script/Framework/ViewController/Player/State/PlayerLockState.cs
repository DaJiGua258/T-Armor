using QFramework.ViewController.FSM;

namespace QFramework.ViewController.Player
{
    public class PlayerLockState : AbstractState<PlayerController>
    {
        public PlayerLockState(PlayerController entity, StateMachine<PlayerController> fsm)
            : base(entity, fsm) { }

        public override void OnEnter()
        {
            Entity.StopMovement();
        }
    }
}
