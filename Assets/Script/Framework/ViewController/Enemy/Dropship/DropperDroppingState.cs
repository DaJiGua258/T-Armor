using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 空投船投放状态骨架。
    /// 这里可以做投放敌人、投放道具、投弹等行为。
    /// </summary>
    public class DropperDroppingState : AbstractState<AbstractEnemy>
    {
        private float _dropTimer;
        private float _dropCooldown = 1.0f;

        public DropperDroppingState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            _dropTimer = 0f;
            Entity.StopMovement();
        }

        public override void OnUpdate()
        {
            _dropTimer += Time.deltaTime;
            if (_dropTimer < _dropCooldown) return;

            // 调用实体行为，具体投放实现由 Dropper.Attack/Shoot 填充。
            
            _dropTimer = 0f;

            // TODO: 根据你的行为树/状态条件决定后续切换
            FSM.ChangeState<DropperMoveState>();
        }
    }
}
