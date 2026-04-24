using QFramework.ViewController.FSM;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class EnemyDeathState : AbstractState<EnemyController>
    {
        public EnemyDeathState(EnemyController owner, StateMachine<EnemyController> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            Entity.StopMovement();
            Entity.ActiveDeathMesh();
            Entity.Collider.enabled = false;
        }

        public override void OnUpdate()
        {
            Entity.LockDeathObject();
        }
    }
}