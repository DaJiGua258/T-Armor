using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人追逐移动状态。
    /// 朝目标侧向偏移点移动，实现包抄效果。
    /// </summary>
    public class EnemyMoveState : AbstractState<AbstractEnemy>
    {
        private const float ModeSwitchInterval = 2f;  // 切换间隔

        private float _arrivedThreshold = 0.2f;
        private float _patrolRadius = 5f;
        private float _curAttackRange = 1;
        private float _flankSide;  // +1 = 偏好左侧, -1 = 偏好右侧
        private float _modeTimer;
        private bool _useFlank;

        public EnemyMoveState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            Entity.StartMovement();

            _curAttackRange = Random.Range((float)Entity.AttackMinRange, (float)Entity.AttackMaxRange);

            if (Entity.Agent != null)
                Entity.Agent.stopDistance = _curAttackRange;

            // 随机选择侧向偏好，确保多个敌人自然分散
            _flankSide = Random.value > 0.5f ? 1f : -1f;

            _modeTimer = 0f;
            _useFlank = Random.value > 0.5f;
        }

        public override void OnUpdate()
        {
            Entity.RefreshTargetInCombat();

            // 超出检测范围且目标已失效 → 返回待机
            if (!Entity.IsInDetectRange())
            {
                if (Entity.Target == null || !Entity.Target.gameObject.activeInHierarchy)
                {
                    FSM.ChangeState<EnemyIdleState>();
                    return;
                }
            }

            if (Entity.IsInSpecifiedRange(_curAttackRange))
            {
                if (Entity.HasLineOfSightToTarget())
                {
                    FSM.ChangeState<EnemyAttackState>();
                    return;
                }
                // 范围内但被障碍阻挡 → 在最小攻击距离外徘徊找角度
                Entity.Agent.stopDistance = Entity.AttackMinRange;
            }
            else
            {
                // 恢复 stopDistance 以便在攻击距离停下
                Entity.Agent.stopDistance = _curAttackRange;
            }

            // 每 2 秒随机切换一次移动模式
            _modeTimer += Time.deltaTime;
            if (_modeTimer >= ModeSwitchInterval)
            {
                _modeTimer = 0f;
                _useFlank = Random.value > 0.5f;
            }

            if (_useFlank)
                Entity.MoveToward(GetFlankTarget());
            else
                Entity.MoveToward(Entity.Target.position);

            Entity.RotateToTarget(Entity.Target.position);  // Body 朝向玩家
        }

        public override void OnExit()
        {
            Entity.StopMovement();
        }

        private Vector3 GetFlankTarget()
        {
            Vector3 targetPos = Entity.Target.position;
            float distance = Vector2.Distance(Entity.transform.position, targetPos);

            // 距离越近偏移越小，进入攻击范围时归零
            float ratio = Mathf.Clamp01(distance / Entity.MaxFlankDistance);

            Vector3 toTarget = ((Vector3)targetPos - Entity.transform.position).normalized;
            Vector3 perpDir = new Vector3(-toTarget.y, toTarget.x, 0) * _flankSide;
            Vector3 offset = perpDir * Entity.FlankWidth * ratio;

            // 最终位置：目标位置 + 侧向偏移，保证整体是在靠近目标
            return targetPos + offset;
        }
    }
}
