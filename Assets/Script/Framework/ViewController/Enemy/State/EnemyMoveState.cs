using QFramework.Enum;
using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人追击状态，向目标移动并在进入攻击范围后切换至攻击状态
    /// </summary>
    public class EnemyMoveState : AbstractState<AbstractEnemy>
    {
        private const float FlankConeHalfAngle = 120f;  // 包抄角度范围（玩家前方扇形半角）
        private const float RepositionDistance = 4f;  // 重新调整位置时的随机偏移距离

        private bool _useFlank;  // 本次追击是否包抄
        private float _flankAngle;  // 包抄随机角度
        private float _stopDis;  // 本次追击的停车距离
        private float _shootTimer;  // 移动射击计时器

        // 重新调整位置
        private bool _isRepositioning;  // 是否正在重新调整位置
        private Vector3 _repositionTarget;  // 重新调整位置目标点

        public EnemyMoveState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            // 检测是否需要先重新调整位置
            if (Entity.RepositionRequested)
            {
                Entity.RepositionRequested = false;
                _isRepositioning = true;
                // 从当前位置朝随机方向偏移 2 单位作为调整点
                Vector3 randomDir = Random.insideUnitCircle.normalized;
                _repositionTarget = Entity.transform.position + randomDir * RepositionDistance;
                Entity.StartMovement();
                return;
            }

            _isRepositioning = false;
            Entity.StartMovement();

            // 进入追击时决定本次走包抄还是直冲
            _useFlank = Random.value < Entity.FlankProbability;
            _flankAngle = Random.Range(-FlankConeHalfAngle, FlankConeHalfAngle);

            // 在 StopRange ~ AttackMinRange 之间随机选一个停车距离
            _stopDis = Random.Range(Entity.StopRange, Entity.AttackMinRange);
            _shootTimer = 0f;
        }

        public override void OnUpdate()
        {
            if (Entity.Target == null) return;

            // 正在重新调整位置 → 先跑到随机点再恢复追击
            if (_isRepositioning)
            {
                Entity.MoveToward(_repositionTarget);
                if (Vector2.Distance(Entity.transform.position, _repositionTarget) < 1f)
                {
                    _isRepositioning = false;
                    // 到达后重新初始化追击参数
                    _useFlank = Random.value < Entity.FlankProbability;
                    _flankAngle = Random.Range(-FlankConeHalfAngle, FlankConeHalfAngle);
                    _stopDis = Random.Range(Entity.StopRange, Entity.AttackMinRange);
                    _shootTimer = 0f;
                }
                return;
            }

            float distance = Vector2.Distance(Entity.transform.position, Entity.Target.position);

            // 到达停车距离且有视线 → 停车射击
            if (distance <= _stopDis && Entity.HasLineOfSightToTarget())
            {
                FSM.ChangeState<EnemyAttackState>();
                return;
            }

            // 未超出最大攻击范围且有视线 → 边追边射（Worker 不移动射击）
            if (Entity.enemyType != EnemyTypeEnum.Worker && distance <= Entity.AttackMaxRange && Entity.HasLineOfSightToTarget())
            {
                _shootTimer += Time.deltaTime;
                if (_shootTimer >= Entity.AttackCooldown)
                {
                    _shootTimer = 0f;
                    Entity.Attack();
                }
            }

            // 包抄或直冲
            if (_useFlank)
                Entity.MoveToward(GetFlankTarget());
            else
                Entity.MoveToward(Entity.Target.position);
        }

        public override void OnExit()
        {
            Entity.StopMovement();
        }

        /// <summary>
        /// 计算包抄目标点，以 StopRange 为半径绕到目标侧方
        /// </summary>
        private Vector3 GetFlankTarget()
        {
            Vector3 targetPos = Entity.Target.position;
            float radius = Entity.StopRange;

            // 以 enemy→target 方向为基准，叠加随机偏转角度
            Vector3 toTarget = (targetPos - Entity.transform.position).normalized;
            Vector3 dir = Quaternion.Euler(0, 0, _flankAngle) * toTarget;

            return targetPos + dir * radius;
        }
    }
}
