using QFramework.Enum;
using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    /// <summary>
    /// 敌人停车射击状态，锁定位置向目标持续开火
    /// </summary>
    public class EnemyAttackState : AbstractState<AbstractEnemy>
    {
        private float _attackTimer;  // 攻击间隔计时器
        private int _shotCount;  // 本次停车射击已射击次数
        private int _maxShotCount;  // 本次停车射击次数上限（2~3 随机）

        public EnemyAttackState(AbstractEnemy owner, StateMachine<AbstractEnemy> fsm)
            : base(owner, fsm) { }

        public override void OnEnter()
        {
            // 锁定 RVO 使敌人原地不动
            Entity.SetAgentRvoLocked(true);
            _attackTimer = 0f;
            _shotCount = 0;
            _maxShotCount = Random.Range(2, 4);  // 2~3 次射击后调整位置
        }

        public override void OnUpdate()
        {
            if (Entity.Target == null) return;

            // 目标超出最大攻击范围 → 切回追击
            float distance = Vector2.Distance(Entity.transform.position, Entity.Target.position);
            if (distance > Entity.AttackMaxRange)
            {
                FSM.ChangeState<EnemyMoveState>();
                return;
            }

            // 转向目标
            Entity.RotateToTarget(Entity.Target.position);

            // 按冷却间隔射击
            _attackTimer += Time.deltaTime;
            if (_attackTimer < Entity.AttackCooldown) return;

            Entity.Attack();
            _attackTimer = 0f;
            _shotCount++;

            // 射满次数后重新调整位置（Worker 不随机跑）
            if (_shotCount >= _maxShotCount)
            {
                if (Entity.enemyType != EnemyTypeEnum.Worker)
                    Entity.RepositionRequested = true;
                FSM.ChangeState<EnemyMoveState>();
            }
        }

        public override void OnExit()
        {
            // 解锁 RVO，允许移动
            Entity.SetAgentRvoLocked(false);
        }
    }
}
