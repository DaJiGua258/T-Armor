using DG.Tweening;
using QFramework.Command;
using QFramework.Enum;
using QFramework.System;
using QFramework.ViewController.FSM;
using QFramework.ViewController.Player;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public class EnemyController : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        public IEnemyInstanceSystem EnemyInstanceSystem => this.GetSystem<IEnemyInstanceSystem>();

        [Header("实例标识")]
        public int enemyId;
        public EnemyTypeEnum enemyEnum;

        [Header("AI 感知范围")]
        public float DetectionRange;
        public float AttackMaxRange;
        public float AttackMinRange;

        [Header("移动")]
        [SerializeField] private float _moveSpeed = 3f;
        public Rigidbody2D Rigidbody;

        [Header("玩家引用（可留空，运行时自动查找）")]
        public Transform Target;

        [Header("引用")]
        [SerializeField] private GameObject _pf_bullet;
        private static Material s_deathMaterial;
        private static Material s_meshMaterial;

        public Transform Mesh;
        public Transform DeathVFX;
        public Transform Muzzle;
        public Collider2D Collider;
        [Header("特殊引用")]
        public ElecShock Light;

        private StateMachine<EnemyController> _fsm;
        private Tweener _tweener;


        void Start()
        {
            // ----- 添加实例 -------------------------
            enemyId = this.SendCommand(new EnemyCommand.Add(enemyEnum, enemyId));

            if (Target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) Target = player.transform;
            }
            
            
            // ----- 初始化 -------------------------
            InitFSM();
            InitDeathObject();
            InitTransofrm();
        }

        void Update()
        {
            _fsm.Update();


            if(EnemyInstanceSystem.GetData(enemyId).CurrentHealth.Value <= 0)
            {
                _fsm.ChangeState<EnemyDeathState>();
            }

            if(!IsGrounded())
            {
                _fsm.ChangeState<EnemyFallState>();
            }
        }

        void FixedUpdate()
        {
            _fsm.FixedUpdate();
        }

        #region  ----- 初始化 -------------------------

        private void InitFSM()
        {
            _fsm = new StateMachine<EnemyController>();
            _fsm.AddState(new EnemyIdleState(this, _fsm));
            _fsm.AddState(new EnemyMoveState(this, _fsm));
            _fsm.AddState(new EnemyAttackState(this, _fsm));
            _fsm.AddState(new EnemyDeathState(this, _fsm));
            _fsm.AddState(new EnemyFallState(this, _fsm));

            // ----- 启动状态机 -------------------------
            _fsm.StartState<EnemyIdleState>();   
        }

        private void InitTransofrm()
        {
            Mesh = transform.Find("Mesh");
            DeathVFX = transform.Find("DeathVFX");
            Muzzle = transform.Find("Weapon/Muzzle");
            Rigidbody = transform.GetComponent<Rigidbody2D>();
            Collider = transform.GetComponent<Collider2D>();
        }

        #endregion

        #region ----- 移动相关 -------------------------
        /// <summary>
        /// 判断是否在检测范围内
        /// </summary>
        public bool IsInDetectRange()
        {
            if (Target == null) return false;

            float dis = Vector2.Distance(transform.position, Target.position);
            if(dis <= DetectionRange)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 判断是否在攻击范围内
        /// </summary>
        public bool IsInAttackMaxRange()
        {
            if (Target == null) return false;

            float dis = Vector2.Distance(transform.position, Target.position);
            if(dis <= AttackMaxRange)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 判断是否在指定范围内
        /// </summary>
        public bool IsInSpecifiedRange(float range)
        {
            if (Target == null) return false;
            float dis = Vector2.Distance(transform.position, Target.position);
            if(dis <= range)
            {
                return true;
            }

            return false;
        }

        public void MoveToward(Vector3 targetPos)
        {
            Vector2 direction = (targetPos - transform.position).normalized;
            Rigidbody.velocity = direction * _moveSpeed;
            Rotate(targetPos);
        }

        public void Rotate(Vector3 targetPos)
        {
            Vector2 direction = (targetPos - transform.position).normalized;
            float z = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, z);
        }

        public void StopMovement()
        {
            Rigidbody.velocity = Vector2.zero;
        }

        public bool IsGrounded()
        {
            // 使用 sqrMagnitude (平方长度) 比 Distance (开方运算) 性能更高
            // 这里需要用2维，z轴一直被使用，会导致无法计算到0.1f以下
            Vector2 pos = Mesh.transform.localPosition;
            if (pos.sqrMagnitude <= 0.1f) // 0.01f 的平方
            {
                
                Mesh.transform.localPosition = Vector2.zero;
                return true;
            }
            return false;   
        }

        #endregion

        #region ----- 死亡相关 -------------------------
        public bool IsDead()
        {
            return _fsm.CurrentStateType == typeof(EnemyDeathState);
        }

        public void InitDeathObject()
        {
            if(s_deathMaterial == null)
            {
                s_meshMaterial = new Material(Mesh.GetComponent<Renderer>().material);
                s_deathMaterial = new Material(s_meshMaterial);
                s_deathMaterial.name += "_death";
                float gray = 0.6f;
                s_deathMaterial.SetColor("_MainColor", new Color(gray, gray, gray, 1f));
            }
        }

        public void LockDeathObject()
        {
            DeathVFX.gameObject.transform.rotation = Quaternion.identity;
        }

        public void ActiveDeathMesh()
        {
            Mesh.GetComponent<MeshRenderer>().material = s_deathMaterial;
            DeathVFX.gameObject.SetActive(true);
        }


        public void SetDeathObjectPos(Vector3 offset)
        {
            var pos = transform.position;
            offset = (offset - pos).normalized;
            pos += offset * 0.1f;
            DeathVFX.gameObject.transform.position = pos;
        }

        #endregion

        #region ----- 武器 -------------------------

        public void Attack()
        {
            Shoot();
        }

        public void Shoot()
        {
            Light.Draw(Target);
            if(Target.TryGetComponent<PlayerController>(out PlayerController c))
            {
                this.SendCommand(PlayerCommand.Damage.Instance.Init(EnemyInstanceSystem.GetData(enemyId).Damage));
            }
            // var obj = Instantiate(_pf_bullet, Muzzle.position, Muzzle.rotation);

        }

        #endregion

        #region ----- 杂项 -------------------------

        public void ForcePush(Vector2 forcePos, int force, float torque)
        {
            var dir = (Vector2)transform.position - forcePos;
            Rigidbody.AddForce(dir.normalized * force, ForceMode2D.Impulse);

            // 随机产生 1 或 -1
            float torDir = (Random.value > 0.5f) ? 1f : -1f;
            Rigidbody.AddTorque(torque * torDir, ForceMode2D.Impulse);
        }

        public string GetCurrentState()
        {
            return _fsm.CurrentState switch
            {
                EnemyIdleState => "Idle",
                EnemyMoveState => "Move",
                EnemyAttackState => "Attack",
                EnemyDeathState => "Death",
                EnemyFallState => "Fall",
                _ => "Unknown"
            };
        }

        #endregion

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, DetectionRange);
            Gizmos.DrawWireSphere(transform.position, AttackMaxRange);
            Gizmos.DrawWireSphere(transform.position, AttackMinRange);
        }
    }
}
