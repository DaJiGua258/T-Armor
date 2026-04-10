using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class PlayerDeathState : AbstractState<PlayerController>
    {
        public PlayerDeathState(PlayerController owner, StateMachine<PlayerController> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            Debug.Log("PlayerDeathState OnEnter");
            Entity.StopMovement();
        }

        public override void OnUpdate()
        {
            Debug.Log("PlayerDeathState OnUpdate");
        }
    }
}