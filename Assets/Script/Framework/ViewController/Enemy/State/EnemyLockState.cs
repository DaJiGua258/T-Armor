using QFramework.ViewController.FSM;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class EnemyLockState : AbstractState<AbstractEnemy>
    {
        public EnemyLockState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }
        
        public override void OnEnter()
        {
            Entity.Agent.enabled = false;
        }

        public override void OnExit()
        {
            Entity.Agent.enabled = true;
        }
    }
}