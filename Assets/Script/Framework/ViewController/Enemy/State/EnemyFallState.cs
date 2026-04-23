using QFramework.ViewController.FSM;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class EnemyFallState : AbstractState<EnemyController>
    {
        public EnemyFallState(EnemyController owner, StateMachine<EnemyController> fsm)
            : base(owner, fsm) { }

        public float gravity = -9.81f; // 重力加速度
        private float verticalVelocity = 0;   // 当前垂直速度

        public override void OnEnter()
        {
            verticalVelocity = 0;
            Entity.Collider.enabled = false;
        }

        public override void OnUpdate()
        {
            if(Entity.IsGrounded())
            {
                FSM.ChangeState<EnemyIdleState>();
            }

            
            // 1. 先把局部坐标转成世界坐标
            Vector3 worldPos = Entity.Mesh.TransformPoint(Entity.Mesh.localPosition);

            // 2. 应用世界坐标的重力位移
            worldPos.y += verticalVelocity * Time.deltaTime;
            
            verticalVelocity += gravity * Time.deltaTime;  // 重力加速度

            // 3. 转回局部坐标并赋值
            Entity.Mesh.localPosition = Entity.Mesh.InverseTransformPoint(worldPos);
        }

        public override void OnExit()
        {
            Entity.Mesh.localPosition = Vector2.zero;
            Entity.Collider.enabled = true;
        }
    }
}