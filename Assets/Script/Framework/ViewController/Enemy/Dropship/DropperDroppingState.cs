using QFramework.ViewController.FSM;
using Unity.VisualScripting;
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
        private float _dropDuration = 5.0f;

        public DropperDroppingState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            _dropTimer = 0f;
            Entity.StopMovement();
            (Entity as Dropper).DropCargos();
        }

        public override void OnUpdate()
        {
            // ----- 运动相关 -------------------------
            _dropTimer += Time.deltaTime;
            (Entity as Dropper).AirFloat(_dropTimer);

            if((Entity as Dropper).IsDropped)
            {
                FSM.ChangeState<DropperIdelState>();
            }
        }
    }
}
