using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class PlayerDashState : AbstractState<PlayerController>
    {
        private float _dashTime = 0.25f;
        private float _timer;

        public PlayerDashState(PlayerController owner, StateMachine<PlayerController> fsm)
            : base(owner, fsm) { }

        
        public override void OnEnter()
        {
            Debug.Log("PlayerDashState OnEnter");
            _timer = 0;
        }

        public override void OnUpdate()
        {
            Entity.Dash();

            _timer += Time.deltaTime;
            if(_timer >= _dashTime)
            {
                FSM.ChangeState<PlayerIdelState>();
            }
        }
    }
}