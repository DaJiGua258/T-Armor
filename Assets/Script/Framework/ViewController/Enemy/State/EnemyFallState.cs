using QFramework.ViewController.FSM;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class EnemyFallState : AbstractState<AbstractEnemy>
    {
        public EnemyFallState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        private float verticalVelocity = 0;   // 当前垂直速度

        public override void OnEnter()
        {
            verticalVelocity = 0;
            Entity.ColliderTrans.gameObject.SetActive(false);
            Entity.Agent.enabled = false;
        }

        public override void OnUpdate()
        {
            if(Entity.IsGrounded())
            {
                if (Entity.HasPatrolPath()) FSM.ChangeState<EnemyPatrolState>();
                else FSM.ChangeState<EnemyIdleState>();
                return;
            }


            Entity.ApplyGravityToMesh(ref verticalVelocity);
        }

        public override void OnExit()
        {
            Entity.Mesh.localPosition = new Vector3(0, 0, Entity.Mesh.localPosition.z);
            Entity.ColliderTrans.gameObject.SetActive(true);
            Entity.Agent.enabled = true;
        }
    }

    
}