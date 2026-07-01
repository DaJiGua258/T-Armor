using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人停车射击状态。
    /// 进入时在 StopRange~AttackMinRange 之间随机选停车距离，
    /// 到达后原地攻击；视线丢失持续超过容忍时间或目标超出范围则切回待机。
    /// </summary>
    public class EnemyAttackState : AbstractState<AbstractEnemy>
    {
        private const float LosLostBuffer = 0.5f;
        private float _attackTimer;
        private float _losLostTimer;

        public EnemyAttackState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            Entity.SetAgentRvoLocked(true);

            // 首次攻击冷却：面板值≥0用面板，-1则等同 AttackCooldown
            _attackTimer = Entity.FirstAttackCooldown >= 0f
                ? Mathf.Max(0f, Entity.AttackCooldown - Entity.FirstAttackCooldown)
                : 0f;
            _losLostTimer = 0f;
        }

        public override void OnUpdate()
        {
            Entity.RefreshTargetInCombat();
            Entity.RotateToTarget(Entity.Target.position);

            // 远程敌人距离过近时拉开距离
            if (Entity.AttackMinRange > Entity.StopRange + 2f)
            {
                float distance = Vector2.Distance(Entity.transform.position, Entity.Target.position);
                if (distance < Entity.StopRange)
                {
                    FSM.ChangeState<EnemyMoveState>();
                    return;
                }
            }

            // 超出最大攻击范围 → 追击
            if (!Entity.IsInAttackMaxRange())
            {
                FSM.ChangeState<EnemyMoveState>();
                return;
            }

            // 视线丢失 → 累积计时，超过容忍值则追击
            if (!Entity.HasLineOfSightToTarget())
            {
                _losLostTimer += Time.deltaTime;
                if (_losLostTimer >= LosLostBuffer)
                {
                    FSM.ChangeState<EnemyMoveState>();
                    return;
                }
            }
            else
            {
                _losLostTimer = 0f;
            }

            _attackTimer += Time.deltaTime;
            if (_attackTimer < Entity.AttackCooldown) return;

            Entity.Attack();
            _attackTimer = 0f;
        }

        public override void OnExit()
        {
            Entity.SetAgentRvoLocked(false);
        }
    }
}
