using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人追逐移动状态。
    /// 3 区间行为：
    ///   > AttackMaxRange       → 纯追击
    ///   AttackMinRange~MaxRange → 移动射击（追击 + 开火）
    ///   < AttackMinRange       → 切到 AttackState 停车射击
    /// </summary>
    public class EnemyMoveState : AbstractState<AbstractEnemy>
    {
        private const float ModeSwitchInterval = 2f;
        private const float ShootCooldown = 1f;
        private const float FlankConeHalfAngle = 120f;   // 玩家前方扇形半角，限制包抄位置范围

        private float _modeTimer;
        private bool _useFlank;
        private float _shootTimer;
        private float _stopDis;
        private float _flankAngle;                        // 当前包抄周期的随机角度

        public EnemyMoveState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            Entity.StartMovement();


            float maxStop = Mathf.Max(Entity.AttackMinRange, Entity.AttackMaxRange * 0.7f);
            _stopDis = Random.Range(Entity.AttackMinRange, maxStop);

            _modeTimer = 0f;
            _useFlank = Random.value < Entity.FlankProbability;
            _flankAngle = Random.Range(-FlankConeHalfAngle, FlankConeHalfAngle);
            _shootTimer = 0f;
        }

        public override void OnUpdate()
        {
            Entity.RefreshTargetInCombat();

            // 超出检测范围且目标失效 → 待机（暂时注释：获取目标后永不丢失，持续追击）
            //if (!Entity.IsInDetectRange())
            //{
            //    if (Entity.Target == null || !Entity.Target.gameObject.activeInHierarchy)
            //    {
            //        FSM.ChangeState<EnemyIdleState>();
            //        return;
            //    }
            //}

            float distance = Vector2.Distance(Entity.transform.position, Entity.Target.position);

            // Zone 3: 进入最小攻击范围且有视线 → 停车射击
            if (distance <= _stopDis && Entity.HasLineOfSightToTarget())
            {
                FSM.ChangeState<EnemyAttackState>();
                return;
            }

            // Zone 2: 在最大~最小攻击范围之间 → 移动射击
            if (distance <= Entity.AttackMaxRange && Entity.HasLineOfSightToTarget())
            {
                _shootTimer += Time.deltaTime;
                if (_shootTimer >= ShootCooldown)
                {
                    _shootTimer = 0f;
                    Entity.Attack();
                }
            }

            // Zone 1 & 2: 都需要移动
            _modeTimer += Time.deltaTime;
            if (_modeTimer >= ModeSwitchInterval)
            {
                _modeTimer = 0f;
                _useFlank = Random.value < Entity.FlankProbability;
                _flankAngle = Random.Range(-FlankConeHalfAngle, FlankConeHalfAngle);
            }

            if (_useFlank)
                Entity.MoveToward(GetFlankTarget());
            else
                Entity.MoveToward(Entity.Target.position);
        }

        public override void OnExit()
        {
            Entity.StopMovement();
        }

        private Vector3 GetFlankTarget()
        {
            Vector3 targetPos = Entity.Target.position;
            float radius = Entity.AttackMinRange + 1f;

            // 以 enemy→target 方向为基准，在 ±FlankConeHalfAngle 内随机方向
            Vector3 toTarget = (targetPos - Entity.transform.position).normalized;
            Vector3 dir = Quaternion.Euler(0, 0, _flankAngle) * toTarget;

            return targetPos + dir * radius;
        }
    }
}
