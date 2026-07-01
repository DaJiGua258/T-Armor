using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人待机状态。
    /// 进入后等待0.5s冷却，过后检测目标范围切换攻击/移动；
    /// 无目标时每隔1秒随机方向旋转一次身体。
    /// </summary>
    public class EnemyIdleState : AbstractState<AbstractEnemy>
    {
        private const float IdleDuration = 0.5f;

        private float _idleTimer;

        // 随机旋转
        private float _spinTimer;
        private float _spinDuration;  // 当前周期旋转时长
        private float _pauseDuration; // 当前周期暂停时长
        private int _spinDir;
        private bool _isSpinning;

        public EnemyIdleState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }


        public override void OnEnter()
        {
            Entity.StopMovement();
            _idleTimer = 0f;
            RandomizeSpin();
        }

        public override void OnUpdate()
        {
            _idleTimer += Time.deltaTime;

            // 冷却时间内，无目标则随机旋转
            if (_idleTimer < IdleDuration)
            {
                if (Entity.Target == null)
                    TickSpin();

                return;
            }

            // 无目标时扫描范围内最近目标
            if (Entity.Target == null)
            {
                var nearest = Entity.FindNearestTarget(Entity.DetectionRange);
                if (nearest != null)
                    Entity.GetTarget(nearest);
            }

            // ----- 冷却结束后检测 -------------------------
            if (Entity.Target != null)
            {
                if (Entity.IsInAttackMinRange() && Entity.HasLineOfSightToTarget())
                {
                    FSM.ChangeState<EnemyAttackState>();
                    return;
                }

                FSM.ChangeState<EnemyMoveState>();
                return;
            }

            // 没有目标，重置timer等待下次扫描
            _idleTimer = 0f;
        }

        private void RandomizeSpin()
        {
            _spinTimer = 0f;
            _spinDuration = Random.Range(0.5f, 1.5f);
            _pauseDuration = Random.Range(1f, 2f);
            _spinDir = Random.value > 0.5f ? 1 : -1;
            _isSpinning = true;
        }

        private void TickSpin()
        {
            _spinTimer += Time.deltaTime;
            float phaseEnd = _isSpinning ? _spinDuration : _pauseDuration;

            if (_spinTimer >= phaseEnd)
            {
                _spinTimer -= phaseEnd;
                _isSpinning = !_isSpinning;

                // 进入旋转阶段时随机化方向与时长的
                if (_isSpinning)
                {
                    _spinDuration = Random.Range(0.5f, 1.5f);
                    _pauseDuration = Random.Range(1f, 2f);
                    _spinDir = Random.value > 0.5f ? 1 : -1;
                }
            }

            if (_isSpinning)
                Entity.Body.Rotate(0, 0, _spinDir * Entity.RotateSpeed * 4f * Time.deltaTime);
        }


    }
}
