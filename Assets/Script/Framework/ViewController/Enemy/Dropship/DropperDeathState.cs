using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 空投船死亡状态：先下落，落地时播放死亡特效。
    /// </summary>
    public class DropperDeathState : AbstractState<AbstractEnemy>
    {
        public DropperDeathState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        private float _verticalVelocity;
        private float _rotationSpeed = 180f;
        private float _horizontalSpeed = 3f;
        private bool _isShownDeathVFX;

        public override void OnEnter()
        {
            Entity.StopMovement();
            Entity.ColliderTrans.gameObject.SetActive(false);
            _verticalVelocity = 0;
            Entity.ShowDamageVFX();
            var dropper = Entity as Dropper;
            if (!dropper.IsDropped)
                dropper.KillAllCargos();

            _isShownDeathVFX = false;
        }

        public override void OnUpdate()
        {
            if (Entity.IsGrounded() && !_isShownDeathVFX)
            {
                Entity.ShowDeathVFX();
                _isShownDeathVFX = true;
                return;
            }

            if(_isShownDeathVFX)
                return;

            Entity.ApplyGravityToMesh(ref _verticalVelocity);
            
            Entity.transform.position += Entity.transform.right * _horizontalSpeed * Time.deltaTime;

            float rotZ = _rotationSpeed * Time.deltaTime;
            Entity.Body.Rotate(0, 0, rotZ);
            Entity.Legs.Rotate(0, 0, rotZ);
            Entity.Shadow.Rotate(0, 0, rotZ);

            Entity.RotateDamageVFX();
        }
    }
}
