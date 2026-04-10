using QFramework.Command;
using QFramework.Enum;
using QFramework.ViewController.FSM;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class EnemyController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        [Header("实例标识")]
        public int enemyId;
        public EnemyTypeEnum enemyEnum;

        [Header("AI 感知范围")]
        [SerializeField] private float _detectionRange = 8f;
        [SerializeField] private float _attackRange = 2f;

        [Header("移动")]
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private Rigidbody2D _rigidbody;

        [Header("玩家引用（可留空，运行时自动查找）")]
        [SerializeField] private Transform _playerTransform;

        // ── 对外只读属性，供 State 类访问 ─────────────────────────────────────
        public float DetectionRange => _detectionRange;
        public float AttackRange    => _attackRange;

        // ── 状态机 ────────────────────────────────────────────────────────────
        private StateMachine<EnemyController> _fsm;

        // ── 生命周期 ──────────────────────────────────────────────────────────

        void Start()
        {
            enemyId = this.SendCommand(new EnemyCommand.Add(enemyEnum, enemyId));

            if (_playerTransform == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _playerTransform = player.transform;
            }

            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();

            BuildFSM();
        }

        void Update()
        {
            _fsm.Update();
        }

        void FixedUpdate()
        {
            _fsm.FixedUpdate();
        }

        // ── FSM 装配 ──────────────────────────────────────────────────────────

        private void BuildFSM()
        {
            _fsm = new StateMachine<EnemyController>();
            _fsm.AddState(new EnemyIdleState(this, _fsm));
            _fsm.AddState(new EnemyMoveState(this, _fsm));
            //     .AddState(new EnemyAttackState(this, _fsm));
            // _fsm.StartState<EnemyIdleState>();
        }

        // ── 供 State 调用的辅助方法 ───────────────────────────────────────────

        /// <summary>判断玩家是否在指定范围内。</summary>
        public bool IsPlayerInRange(float range)
        {
            if (_playerTransform == null) return false;
            return Vector2.Distance(transform.position, _playerTransform.position) <= range;
        }

        /// <summary>向给定方向施加速度（由 MoveState 驱动）。</summary>
        public void MoveToward(Vector2 direction)
        {
            if (_rigidbody != null)
                _rigidbody.velocity = direction * _moveSpeed;
        }

        /// <summary>停止移动。</summary>
        public void StopMovement()
        {
            if (_rigidbody != null)
                _rigidbody.velocity = Vector2.zero;
        }

        /// <summary>执行一次攻击（由 AttackState 驱动）。</summary>
        public void PerformAttack()
        {
            this.SendCommand(new EnemyCommand.Damage(enemyId, 0));
        }

        /// <summary>受到伤害的外部入口（可由子弹/碰撞调用）。</summary>
        public void TakeDamage(int damage)
        {
            this.SendCommand(new EnemyCommand.Damage(enemyId, damage));
        }
    }
}
