using DG.Tweening;
using Pathfinding;
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
        public bool IsInit = false;

        [Header("寻路与感知")]
        public FollowerEntity Agent;
        public float DetectionRange;
        public float AttackMaxRange;
        public float AttackMinRange;

        [Header("组件")]
        [SerializeField] private float _moveSpeed = 3f;
        public Rigidbody2D Rb;
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
        public Transform ColliderTrans;

        [Header("武器引用")]
        public Transform Weapon;
        public Transform Muzzle;
        
        [Header("武器参数")]
        [SerializeField] protected float _weaponAimRotateSpeed = 10f;
        [SerializeField] protected float _weaponAimLimitDeg = 75f;
        // 0 = 最大散布，1 = 完全精准（无偏移）
        [Range(0f, 1f)]
        [SerializeField] protected float _shootAccuracy = 1f;
        

        [Header("特殊引用")]
        protected StateMachine<AbstractEnemy> _fsm;
        private bool _hasPatrolRoute;
        private bool _patrolToEndPoint = true;
        private Vector3 _patrolStartPoint;
        private Vector3 _patrolEndPoint;
        private bool _hasPatrolMoveSpeed;
        private float _patrolMoveSpeed = 1f;
        private Tweener _tweener;
        [Header("移动参数")]
        [SerializeField] protected float _rotateSpeed = 5f;
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
            if(IsInit) return;

            InitEnemy();
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

        /// <summary>
        /// 用于外部调用初始化
        /// </summary>
        public void InitEnemy()
        {
            if(IsInit) return;
            InitTransofrm();
            InitFSM();
            InitData();
            IsInit = true;
        }

        protected virtual void InitFSM()
        {
            _fsm = new StateMachine<AbstractEnemy>();
            _fsm.AddState(new EnemyIdleState(this, _fsm));
            _fsm.AddState(new EnemyMoveState(this, _fsm));
            _fsm.AddState(new EnemyAttackState(this, _fsm));
            _fsm.AddState(new EnemyDeathState(this, _fsm));
            _fsm.AddState(new EnemyFallState(this, _fsm));
            _fsm.AddState(new EnemyLockState(this, _fsm));
            _fsm.AddState(new EnemyPatrolState(this, _fsm));

            // ----- 启动状态机 -------------------------
            _fsm.StartState<EnemyIdleState>();   
        }

        public void InitTransofrm()
        {
            // 身体部件
            Mesh = transform.Find("Mesh");
            Body = Mesh.Find("Body");
            Legs = Mesh.Find("Legs");
            ColliderTrans = Mesh.Find("Collider");

            Shadow = transform.Find("Shadow");

            // 武器
            Weapon = Body.Find("WeaponSlot");
            if(enemyType != EnemyTypeEnum.Dropper_Mid)
            {
                Muzzle = Weapon.GetChild(0).Find("Muzzle");
            }

            // 物理组件
            Rb = transform.GetComponent<Rigidbody2D>();
            Collider = ColliderTrans.GetComponent<Collider2D>();

            Agent = transform.GetComponent<FollowerEntity>();
        }

        protected virtual void InitData() { }

        #endregion

        #region ----- 目标判断 -------------------------

        /// <summary>
        /// 获取目标
        /// </summary>
        public void GetTarget(Transform player)
        {
            Target = player;
        }

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
            return IsInSpecifiedRange(Target.position, range);
        }

        /// <summary>
        /// 判断自身到任意目标点是否在指定范围内。
        /// </summary>
        public bool IsInSpecifiedRange(Vector3 targetPos, float range)
        {
            float dis = Vector2.Distance(transform.position, targetPos);
            if (Agent != null)
            {
                Agent.stopDistance = range;
            }
            return dis <= range;
        }

        #endregion

        #region ----- 移动相关 -------------------------
        
        public virtual void MoveToward(Vector3 targetPos)
        {
            // Vector2 targetDir = ((Vector2)targetPos - Rb.position).normalized;
    
            // // 当前移动方向，静止时直接用目标方向
            // Vector2 currentDir = Rb.velocity.sqrMagnitude > 0.01f
            //     ? Rb.velocity.normalized
            //     : targetDir;
            
            // // 每帧最多转这么多角度
            // Vector2 newDir = Vector2.MoveTowards(currentDir, targetDir, _turnSpeed * Time.fixedDeltaTime);
            
            // Rb.velocity = newDir * _moveSpeed;
            
            // Rotate(targetPos);

            Agent.destination = targetPos;
            Rotate(targetPos);
        }

        public virtual void Rotate(Vector3 targetPos)
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

            // ----- 应用旋转 -------------------------
            Body.rotation = Quaternion.Euler(0, 0, smoothAngle);
            Shadow.rotation = Quaternion.Euler(0, 0, smoothAngle);

            if(Weapon != null)
            {
                Weapon.rotation = Quaternion.Euler(0, 0, smoothAngle);
            }

            // ----- 腿部旋转 -------------------------
            Vector2 v = Agent != null
                ? Agent.velocity
                : (Rb != null ? Rb.velocity : Vector2.zero);

            if(v.sqrMagnitude < 0.01f || Legs == null) return;

            float vz = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
            Quaternion legTarget = Quaternion.Euler(0f, 0f, vz);
            // 平滑插值
            Legs.rotation = Quaternion.Slerp(
                Legs.rotation,
                legTarget,
                _rotateSpeed * Time.deltaTime * 0.25f
            );
        }

        public virtual void RotateToTarget(Vector3 targetPos)
        {
            // 默认行为与 Rotate 一致；子类可重写为攻击特化朝向
            Rotate(targetPos);
        }

        /// <summary>
        /// 武器节点独立瞄准目标（平滑 + 相对身体角度限制）。
        /// </summary>
        protected void RotateWeaponToTarget(Vector3 targetPos)
        {
            if(Weapon == null || Body == null) return;

            Vector2 dir = (targetPos - Weapon.position).normalized;
            if(dir.sqrMagnitude < 0.01f) return;

            // 与 WeaponController 对齐：使用 Atan2 + LerpAngle 的瞄准方式
            float targetZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float bodyZ = Body.rotation.eulerAngles.z;

            // 限制武器偏离身体的夹角，避免瞬间大幅甩动
            float delta = Mathf.DeltaAngle(bodyZ, targetZ);
            delta = Mathf.Clamp(delta, -_weaponAimLimitDeg, _weaponAimLimitDeg);
            targetZ = bodyZ + delta;

            float currentZ = Weapon.rotation.eulerAngles.z;
            float smoothZ = Mathf.LerpAngle(currentZ, targetZ, _weaponAimRotateSpeed * Time.deltaTime);
            Weapon.rotation = Quaternion.Euler(0f, 0f, smoothZ);
        }

        public void RotateInLock(float z)
        {
            Body.rotation = Quaternion.Euler(0, 0, z);
            Legs.rotation = Quaternion.Euler(0, 0, z);
            Shadow.rotation = Quaternion.Euler(0, 0, z);
        }

        public void StartMovement()
        {
            if (Agent != null)
            {
                Agent.maxSpeed = _moveSpeed;
            }
        }

        public void StartPatrolMovement()
        {
            if (Agent == null) return;
            Agent.maxSpeed = _hasPatrolMoveSpeed ? _patrolMoveSpeed : _moveSpeed;
        }
        
        public void StopMovement()
        {
            if (Agent != null)
            {
                Agent.maxSpeed = 0;
            }
            else if (Rb != null)
            {
                Rb.velocity = Vector2.zero;
            }
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

        #region ----- 射击相关 -------------------------

        /// <summary>
        /// 基于准度参数，返回朝向目标的带随机偏移射击方向。
        /// 准度越高，偏移越小（1 = 完全精准）。
        /// </summary>
        protected Vector3 GetShootDirectionWithAccuracy(Vector3 shootOrigin, Vector3 fallbackDir)
        {
            // 兜底方向：目标丢失/距离过近等异常情况下，仍保证子弹可发射。
            Vector3 safeFallback = fallbackDir.sqrMagnitude > 0.0001f ? fallbackDir.normalized : transform.right;
            if(Target == null) return safeFallback;

            // 计算目标方向
            Vector3 toTarget = Target.position - shootOrigin;
            if(toTarget.sqrMagnitude < 0.0001f) return safeFallback;

            // 计算基础方向
            Vector3 baseDir = toTarget.normalized;
            const float maxSpreadDegAtZeroAccuracy = 20f; // 最大偏移角度，准度为0时
            float maxOffsetDeg = (1f - _shootAccuracy) * maxSpreadDegAtZeroAccuracy; // 计算最大偏移角度

            if(maxOffsetDeg <= 0.001f) return baseDir; // 如果最大偏移角度小于0.001，则直接返回基础方向

            // 以目标方向为中心，在 [-maxOffsetDeg, +maxOffsetDeg] 内随机偏移。
            float randomOffsetDeg = Random.Range(-maxOffsetDeg, maxOffsetDeg);
            return Quaternion.Euler(0f, 0f, randomOffsetDeg) * baseDir;
        }

        #endregion


        

        public void ChangeState<TState>() where TState : AbstractState<AbstractEnemy>
        {
            _fsm.ChangeState<TState>();
        }

        public void SetPatrolRoute(Vector3 startPoint, Vector3 endPoint)
        {
            _patrolStartPoint = startPoint;
            _patrolEndPoint = endPoint;
            _patrolToEndPoint = true;
            _hasPatrolRoute = true;
        }

        public void SetPatrolMoveSpeed(float patrolMoveSpeed)
        {
            _patrolMoveSpeed = Mathf.Max(0f, patrolMoveSpeed);
            _hasPatrolMoveSpeed = true;
        }

        public bool HasPatrolPath()
        {
            return _hasPatrolRoute;
        }

        public Vector3 GetCurrentPatrolPoint()
        {
            if (!_hasPatrolRoute) return transform.position;
            return _patrolToEndPoint ? _patrolEndPoint : _patrolStartPoint;
        }

        public void AdvancePatrolPoint()
        {
            if (!_hasPatrolRoute) return;
            _patrolToEndPoint = !_patrolToEndPoint;
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
            Rb.AddForce(dir.normalized * force, ForceMode2D.Impulse);

            // 随机产生 1 或 -1
            float torDir = (Random.value > 0.5f) ? 1f : -1f;
            Rb.AddTorque(torque * torDir, ForceMode2D.Impulse);
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
                QFramework.ViewController.Enemy.EnemyPatrolState => "Patrol",
                _ => "Unknown"
            };
        }

        #endregion

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, DetectionRange);
            Gizmos.DrawWireSphere(transform.position, AttackMaxRange);
            Gizmos.DrawWireSphere(transform.position, AttackMinRange);
        }
    }
}
