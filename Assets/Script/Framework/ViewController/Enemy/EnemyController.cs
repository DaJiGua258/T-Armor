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
        [SerializeField] private Transform _target;

        // ── 对外只读属性，供 State 类访问 ─────────────────────────────────────
        public float DetectionRange => _detectionRange;
        public float AttackRange    => _attackRange;

        // ── 状态机 ────────────────────────────────────────────────────────────
        private StateMachine<EnemyController> _fsm;


        void Start()
        {
            // ----- 添加实例 -------------------------
            enemyId = this.SendCommand(new EnemyCommand.Add(enemyEnum, enemyId));

            if (_target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) _target = player.transform;
            }
            
            // ----- 初始化状态机 -------------------------
            InitFSM();
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

        private void InitFSM()
        {
            _fsm = new StateMachine<EnemyController>();
            _fsm.AddState(new EnemyIdleState(this, _fsm));
            _fsm.AddState(new EnemyMoveState(this, _fsm));
            //     .AddState(new EnemyAttackState(this, _fsm));

            // ----- 启动状态机 -------------------------
            _fsm.StartState<EnemyIdleState>();   
        }

        // ── 供 State 调用的辅助方法 ───────────────────────────────────────────
        public bool IsTargetInRange(float range)
        {
            if (_target == null) return false;

            float dis = Vector2.Distance(transform.position, _target.position);
            if(dis <= range)
            {
                return true;
            }
            return false;
        }

        public void MoveToward()
        {
            Vector2 direction = (_target.position - transform.position).normalized;
            _rigidbody.velocity = direction * _moveSpeed;
            Rotate(direction);
        }

        public void Rotate(Vector2 direction)
        {
            float z = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, z);
        }

        public void StopMovement()
        {
            _rigidbody.velocity = Vector2.zero;
        }

        public void Attack()
        {
            // this.SendCommand(new EnemyCommand.Damage(enemyId, 0));
        }

        public void TakeDamage(int damage)
        {
            // this.SendCommand(new EnemyCommand.Damage(enemyId, damage));
        }
    }
}
