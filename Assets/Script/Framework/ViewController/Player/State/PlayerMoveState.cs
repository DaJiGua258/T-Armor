using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class PlayerMoveState : AbstractState<PlayerController>
    {
        public PlayerMoveState(PlayerController owner, StateMachine<PlayerController> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
        }

        public override void OnUpdate()
        {
            Entity.Move();

            if(Entity.InputUtility.GetMovementDir() == Vector3.zero)
            {
                FSM.ChangeState<PlayerIdelState>();
            }

            if(Entity.InputUtility.GetDashInput()
                && Entity.PlayerModel.CurrentFuel.Value >= Entity.PlayerModel.DashCost)
            {
                FSM.ChangeState<PlayerDashState>();
            }

            if(Entity.InputUtility.GetSprintInput()
                && Entity.PlayerModel.CurrentFuel.Value >= Entity.PlayerModel.SprintCost)
            {
                FSM.ChangeState<PlayerSprintState>();
            }
        }

    }
}