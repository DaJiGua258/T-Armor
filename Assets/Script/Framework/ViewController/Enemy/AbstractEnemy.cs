using DG.Tweening;
using QFramework.Command;
using QFramework.Enum;
using QFramework.System;
using QFramework.Utility;
using QFramework.ViewController.FSM;
using QFramework.ViewController.Player;
using Unity.VisualScripting;
using UnityEngine;

namespace QFramework.ViewController.Enemy
{
    public abstract class AbstractEnemy : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;
        public IEnemyInstanceSystem EnemyInstanceSystem => this.GetSystem<IEnemyInstanceSystem>();
        public IObjectPoolUtility ObjectPoolUtility => this.GetUtility<IObjectPoolUtility>();
        public IResourceLoad ResourceLoad => this.GetUtility<IResourceLoad>();

        [Header("实例标识")]
        public int enemyId;
        public EnemyTypeEnum enemyType;

        [Header("感知范围")]
        public float DetectionRange;
        public float AttackMaxRange;
        public float AttackMinRange;

        [Header("组件")]
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private Rigidbody2D _rb;
        public Collider2D Collider;
        private static Material s_deathMaterial;
        private static Material s_meshMaterial;

        [Header("目标引用")]
        public Transform Target;

        [Header("预制体引用")]
        public GameObject Pf_bullet;
        public GameObject Pf_deathVFX;
        public ParticleSystem ShoottingVFX;
        

        [Header("引用")]
        public Transform Mesh;
        public Transform Body;
        public Transform Legs;
        public Transform Shadow;

        [Header("武器引用")]
        public Transform Weapon;
        public Transform Muzzle;
        

        [Header("特殊引用")]
        protected StateMachine<AbstractEnemy> _fsm;
        private Tweener _tweener;
        [Header("移动参数")]
        [SerializeField] private float _rotateSpeed = 5f;
        [SerializeField] private float _turnSpeed = 5f;
        private Vector3 _smoothVelocity;
        
        #region ----- 生命周期 -------------------------
        void Start()
        {
            // ----- 添加实例 -------------------------
            enemyId = this.SendCommand(new EnemyCommand.Add(enemyType, enemyId));

            if (Target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) Target = player.transform;
            }
            
            
            // ----- 初始化 -------------------------
            InitTransofrm();
            InitFSM();
            InitData();
        }

        protected virtual void Update()
        {
            _fsm.Update();


            if(EnemyInstanceSystem.GetData(enemyId).CurrentHealth.Value <= 0)
            {
                _fsm.ChangeState<EnemyDeathState>();
            }

            // if(!IsGrounded())
            // {
            //     _fsm.ChangeState<EnemyFallState>();
            // }
        }

        void FixedUpdate()
        {
            _fsm.FixedUpdate();
        }

        #endregion

        #region  ----- 初始化 -------------------------

        protected virtual void InitFSM()
        {
            _fsm = new StateMachine<AbstractEnemy>();
            _fsm.AddState(new EnemyIdleState(this, _fsm));
            _fsm.AddState(new EnemyMoveState(this, _fsm));
            _fsm.AddState(new EnemyAttackState(this, _fsm));
            _fsm.AddState(new EnemyDeathState(this, _fsm));
            _fsm.AddState(new EnemyFallState(this, _fsm));
            _fsm.AddState(new EnemyLockState(this, _fsm));

            // ----- 启动状态机 -------------------------
            _fsm.StartState<EnemyIdleState>();   
        }

        public void InitTransofrm()
        {
            // 身体部件
            Mesh = transform.Find("Mesh");
            Body = Mesh.Find("Body");
            Legs = Mesh.Find("Legs");

            Shadow = transform.Find("Shadow");

            // 武器
            Weapon = Body.Find("WeaponSlot");
            if(enemyType != EnemyTypeEnum.Dropper_Mid)
            {
                Muzzle = Weapon.GetChild(0).Find("Muzzle");
            }

            // 物理组件
            _rb = transform.GetComponent<Rigidbody2D>();
            Collider = Mesh.GetComponent<Collider2D>();
        }

        protected virtual void InitData() { }

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
            Vector2 targetDir = ((Vector2)targetPos - _rb.position).normalized;
    
            // 当前移动方向，静止时直接用目标方向
            Vector2 currentDir = _rb.velocity.sqrMagnitude > 0.01f
                ? _rb.velocity.normalized
                : targetDir;
            
            // 每帧最多转这么多角度
            Vector2 newDir = Vector2.MoveTowards(currentDir, targetDir, _turnSpeed * Time.fixedDeltaTime);
            
            _rb.velocity = newDir * _moveSpeed;
            
            Rotate(targetPos);
        }

        public void Rotate(Vector3 targetPos)
        {
            Vector2 dir = (targetPos - transform.position).normalized;

            // 如果方向太小，则不旋转
            if(dir.sqrMagnitude < 0.01f) return;

            // 计算目标角度
            float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.Euler(0, 0, z);

            // 平滑旋转
            float currentAngle = Body.rotation.eulerAngles.z;
            float smoothAngle = Mathf.LerpAngle(currentAngle, z, _rotateSpeed * Time.fixedDeltaTime);

            Body.rotation = Quaternion.Euler(0, 0, smoothAngle);
            Shadow.rotation = Quaternion.Euler(0, 0, smoothAngle);

            if(Weapon != null)
            {
                Weapon.rotation = Quaternion.Euler(0, 0, smoothAngle);
            }
        }

        public void RotateInLock(float z)
        {
            Body.rotation = Quaternion.Euler(0, 0, z);
            Legs.rotation = Quaternion.Euler(0, 0, z);
            Shadow.rotation = Quaternion.Euler(0, 0, z);
        }

        public void StopMovement()
        {
            _rb.velocity = Vector2.zero;
        }

        public bool IsGrounded()
        {
            // 使用 sqrMagnitude (平方长度) 比 Distance (开方运算) 性能更高
            // 这里需要用2维，z轴一直被使用，会导致无法计算到0.1f以下
            Vector2 pos = Mesh.transform.localPosition;
            if (pos.sqrMagnitude <= 0.1f) // 0.01f 的平方
            {
                float z = Mesh.transform.localPosition.z;
                Mesh.transform.localPosition = new Vector3(0, 0, z);
                return true;
            }
            return false;   
        }

        public void ChangeState<TState>() where TState : AbstractState<AbstractEnemy>
        {
            _fsm.ChangeState<TState>();
        }

        #endregion

        #region ----- 死亡相关 -------------------------
        public bool IsDead()
        {
            return _fsm.CurrentStateType == typeof(EnemyDeathState);
        }

        public void ShowDeathVFX()
        {
            var obj = ObjectPoolUtility.GetObject(Pf_deathVFX, transform.position, Quaternion.identity);
            this.GetUtility<ITimerUtility>().AddOnce(() => {
                this.GetUtility<IObjectPoolUtility>().PushObject(obj);
            }, 5f);
            gameObject.SetActive(false);
        }


        // public void InitDeathObject()
        // {
        //     DeathVFX.gameObject.SetActive(false);

        //     // if(s_deathMaterial == null)
        //     // {
        //     //     s_meshMaterial = new Material(Mesh.GetComponent<Renderer>().material);
        //     //     s_deathMaterial = new Material(s_meshMaterial);
        //     //     s_deathMaterial.name += "_death";
        //     //     float gray = 0.6f;
        //     //     s_deathMaterial.SetColor("_Color", new Color(gray, gray, gray, 1f));
        //     // }
        // }

        // public void LockDeathObject()
        // {
        //     DeathVFX.gameObject.transform.rotation = Quaternion.identity;
        // }

        // public void ActiveDeathMesh()
        // {
        //     Mesh.GetComponent<MeshRenderer>().material = s_deathMaterial;
        //     DeathVFX.gameObject.SetActive(true);
        // }


        // public void SetDeathObjectPos(Vector3 offset)
        // {
        //     var pos = transform.position;
        //     offset = (offset - pos).normalized;
        //     pos += offset * 0.1f;
        //     DeathVFX.gameObject.transform.position = pos;
        // }

        #endregion

        #region ----- 武器 -------------------------

        public abstract void Attack();

        public abstract void Shoot();

        #endregion

        #region ----- 杂项 -------------------------

        public void ForcePush(Vector2 forcePos, int force, float torque)
        {
            var dir = (Vector2)transform.position - forcePos;
            _rb.AddForce(dir.normalized * force, ForceMode2D.Impulse);

            // 随机产生 1 或 -1
            float torDir = (Random.value > 0.5f) ? 1f : -1f;
            _rb.AddTorque(torque * torDir, ForceMode2D.Impulse);
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
