using QFramework.ViewController.FSM;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class EnemyDeathState : AbstractState<AbstractEnemy>
    {
        public EnemyDeathState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }
            

        public override void OnEnter()
        {
            Debug.Log("EnemyDeathState OnEnter");
            Entity.StopMovement();
            Entity.ShowDeathVFX();
        }
    }
}