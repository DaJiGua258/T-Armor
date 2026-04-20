using QFramework.Command;
using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class PlayerSprintState : AbstractState<PlayerController>
    {
        public PlayerSprintState(PlayerController owner, StateMachine<PlayerController> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            
        }

        public override void OnUpdate()
        {
            // 每秒消耗燃料
            Entity.SendCommand(PlayerCommand.ConsumeFuel.Instance.Init(Entity.PlayerModel.SprintCost * Time.deltaTime));
            Entity.Sprint();
            
            if(Entity.InputUtility.GetDashInput()
                && Entity.PlayerModel.CurrentFuel.Value >= Entity.PlayerModel.DashCost)
            {
                FSM.ChangeState<PlayerDashState>();
            }

            if(Entity.InputUtility.GetSprintInput()   // 冲刺输入
                || Entity.InputUtility.GetMovementDir() == Vector3.zero  // 移动方向为零
                || Entity.PlayerModel.CurrentFuel.Value <= 0)  // 燃料不足
            {
                FSM.ChangeState<PlayerMoveState>();
            }
        }
    }
}