using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 空投船待机状态骨架。
    /// 这里可以做悬停、巡逻前等待、目标搜寻等逻辑。
    /// </summary>
    public class DropperIdelState : AbstractState<AbstractEnemy>
    {
        private float _idleTimer;
        private float _idleDuration;

        public DropperIdelState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            _idleDuration = 0.5f;
            _idleTimer = 0f;
            Entity.StopMovement();
            (Entity as Dropper).LastPos = Entity.transform.position;  // 记录上一次停靠的位置
        }

        public override void OnUpdate()
        {
            _idleTimer += Time.deltaTime;
            if (_idleTimer < _idleDuration) return;

            // TODO: 按你的规则切换到移动/投放状态
            FSM.ChangeState<DropperMoveState>();
        }

        public override void OnExit()
        {
            (Entity as Dropper).LastPos = Entity.transform.position;  // 记录上一次停靠的位置
        }
    }
}
