using QFramework.Manager;
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
            Entity.StopMovement();
            Entity.PlayDeathVFX();
            GameManager.Instance.SetGameResultState(GameResultState.GameOver);
            UIGameManager.Instance.ShowPanel(UIGamePanelType.GameOverPanel);
        }

        public override void OnUpdate()
        {
            Debug.Log("PlayerDeathState OnUpdate");
        }
    }
}