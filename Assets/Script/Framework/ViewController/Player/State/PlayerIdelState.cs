using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class PlayerIdelState : AbstractState<PlayerController>
    {
        public PlayerIdelState(PlayerController entity, StateMachine<PlayerController> fsm)
            : base(entity, fsm) { }
        
        public override void OnEnter()
        {
            Entity.StopMovement();
            Entity.ResumeLegParticles();
        }

        public override void OnUpdate()
        {
            if(Entity.InputUtility.GetMovementDir() != Vector3.zero)
            {
                FSM.ChangeState<PlayerMoveState>();
            }
        }
    }
}