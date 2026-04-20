using QFramework.Command;
using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class PlayerDashState : AbstractState<PlayerController>
    {
        private float _dashTime = 0.25f;
        private Vector2 _dashDir;
        private float _timer;

        public PlayerDashState(PlayerController owner, StateMachine<PlayerController> fsm)
            : base(owner, fsm) { }

        
        public override void OnEnter()
        {
            Entity.SendCommand(PlayerCommand.ConsumeFuel.Instance.Init(Entity.PlayerModel.DashCost));
            
            _timer = 0;
            _dashDir = Entity.InputUtility.GetMovementDir();
        }

        public override void OnUpdate()
        {
            Entity.Dash(_dashDir);

            _timer += Time.deltaTime;
            if(_timer >= _dashTime)
            {
                FSM.ChangeState<PlayerIdelState>();
            }
        }
    }
}