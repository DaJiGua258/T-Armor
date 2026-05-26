using DG.Tweening;
using Pathfinding;
using QFramework.Command;
using QFramework.Enum;
using QFramework.Model;
using QFramework.System;
using QFramework.Utility;
using QFramework.ViewController.FSM;
using QFramework.ViewController.Misc;
using QFramework.ViewController.Player;
using System.Collections;
using System.Collections.Generic;
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

        [Header("测试用")]
        public bool UseInspectorRange;
        public EnemyTypeEnum enemyType;
        public bool IsInit = false;
        public bool debugLock = false;

        [Header("寻路与感知")]
        public FollowerEntity Agent;
        protected EnemeyConfig _enemyConfig;
        public float DetectionRange;
        public float AttackMaxRange;
        public float AttackMinRange;
        [Header("攻击距离")]
        public float StopRange = 1.5f;
        public float AttackCooldown = 1.5f;
        [Tooltip("-1 使用 AttackCooldown，≥0 则作为首次攻击冷却")]
        public float FirstAttackCooldown = -1f;

        [Header("包抄参数")]
        [Range(0f, 1f)] public float FlankProbability = 0.75f;  // 包抄概率

        [Header("组件")]
        [SerializeField] private float _moveSpeed = 3f;
        public Rigidbody2D Rb;
        private static Material s_deathMaterial;
        private static Material s_meshMaterial;

        [Header("检测设置")]
        public LayerMask TargetLayerMask;  // 扫描用的 LayerMask
        [SerializeField] private float _findTargetInterval = 0.5f;  // 常规扫描间隔
        private float _findTargetTimer;
        [SerializeField] private float _combatScanInterval = 0.3f;  // 战斗中扫描间隔
        private float _combatScanTimer;
        private Collider2D[] _scanCache = new Collider2D[10];

        [Header("目标引用")]
        public Transform Target;

        [Header("预制体引用")]
        public GameObject pf_Bullet;
        public GameObject pf_DeathVFX;
        public ParticleSystem ShoottingVFX;
        

        [Header("引用")]
        public Transform Mesh;
        public Transform Body;
        public Transform Legs;
        public Transform Shadow;
        public Transform ColliderTrans;
        private Transform _damageVfxNode;
        private List<ParticleSystem> _damageVfxParticles;

        // 受击闪白
        private List<Renderer> _meshRenderers;
        private MaterialPropertyBlock _flashBlock;

        [Header("武器引用")]
        public Transform Weapon;
        public Transform Muzzle;
        
        [Header("武器参数")]
        [SerializeField] protected float _weaponAimRotateSpeed = 10f;
        [SerializeField] protected float _weaponAimLimitDeg = 75f;
        // 0 = 最大散布，1 = 完全精准（无偏移）
        [Range(0f, 1f)]
        [SerializeField] protected float _shootAccuracy = 1f;
        [SerializeField] protected int _burstCount = 3;        // 连发数量
        [SerializeField] protected float _burstInterval = 0.1f; // 连发间隔
        

        [Header("特殊引用")]
        protected StateMachine<AbstractEnemy> _fsm;


        [Header("移动参数")]
        [SerializeField] protected float _rotateSpeed = 5f;
        public float RotateSpeed => _rotateSpeed;
        private Vector3 _smoothVelocity;

        [Header("巡逻参数")]
        private bool _hasPatrolRoute;
        private bool _patrolToEndPoint = true;
        private Vector3 _patrolStartPoint;
        private Vector3 _patrolEndPoint;
        private bool _hasPatrolMoveSpeed;
        private float _patrolMoveSpeed = 1f;
        
        #region ----- 生命周期 -------------------------
        void Start()
        {
            // ----- 添加实例 -------------------------
            enemyId = this.SendCommand(new EnemyCommand.Add(enemyType, enemyId));


            // ----- 初始化 -------------------------
            if(IsInit) return;

            InitEnemy();

            if (debugLock)
                _fsm.ChangeState<EnemyLockState>();
        }

        

        protected virtual void Update()
        {
            _fsm.Update();

            // 目标被销毁或失活时置空
            //if (Target != null && !Target.gameObject.activeInHierarchy)
            //    Target = null;

            if(IsDead())
            {
                _fsm.ChangeState<EnemyDeathState>();
                return;
            }

            

            // 定期扫描范围内最近目标（暂时注释：获取目标后永不丢失，由外部 SetTarget 或初始扫描驱动）
            //_findTargetTimer -= Time.deltaTime;
            //if (_findTargetTimer <= 0f)
            //{
            //    _findTargetTimer = _findTargetInterval;
            //    var nearest = FindNearestTarget(DetectionRange);
            //    Target = nearest;
            //}

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
            InitData();
            InitFSM();
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

            ColliderTrans = Body.Find("Collider");

            Shadow = transform.Find("Shadow");

            // 武器
            Weapon = Body.Find("WeaponSlot");
            if(enemyType != EnemyTypeEnum.Dropper_Mid)
            {
                Muzzle = Weapon.GetChild(0).Find("Muzzle");
            }

            // 物理组件
            Rb = transform.GetComponent<Rigidbody2D>();

            SyncColliderTag();

            Agent = transform.GetComponent<FollowerEntity>();

            // 损伤特效粒子（可选）
            Transform damageVfxT = transform.Find("DamageVFX");
            _damageVfxNode = damageVfxT;
            if (damageVfxT != null)
            {
                _damageVfxParticles = new List<ParticleSystem>(damageVfxT.GetComponentsInChildren<ParticleSystem>(true));
                HideDamageVFX();
            }
            else
            {
                _damageVfxParticles = new List<ParticleSystem>();
            }



            // 收集 Mesh 下所有渲染器（含 Body/Legs/Weapon 等），用于受击闪白
            _meshRenderers = new List<Renderer>();
            Mesh.GetComponentsInChildren(true, _meshRenderers);
            _flashBlock = new MaterialPropertyBlock();
        }

        /// <summary>
        /// 将 ColliderTrans 及其所有子物体的 Tag 同步为根节点 Tag，
        /// 避免因子节点 Tag 不一致导致 FindNearestTarget 的 CompareTag 过滤失效。
        /// </summary>
        private void SyncColliderTag()
        {
            if (ColliderTrans == null) return;

            string rootTag = gameObject.tag;

            ColliderTrans.gameObject.tag = rootTag;
            foreach (Transform child in ColliderTrans)
            {
                child.gameObject.tag = rootTag;
            }
        }

        protected virtual void InitData()
        {
            var config = this.GetModel<IEnemeyConfigModel>().GetEnemyFromCache(enemyType);
            if (config == null) return;

            _enemyConfig = config;
            DetectionRange = config.DetectionRange;
            if (!UseInspectorRange)
            {
                AttackMaxRange = config.AttackMaxRange;
                AttackMinRange = config.AttackMinRange;
            }
            _moveSpeed = config.MoveSpeed;
        }

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
        /// 用 OverlapCircle + LayerMask 扫描范围内最近的有效目标。
        /// 依据自身标签自动判断：Player→检测Enemy，Enemy→检测Player。
        /// </summary>
        /// <summary>
        /// 在指定范围内查找最近的目标
        /// </summary>
        /// <param name="range">搜索范围</param>
        /// <returns>找到的最近目标的Transform，如果没有找到则返回null</returns>
        public Transform FindNearestTarget(float range)
        {
            // 使用OverlapCircleNonAlloc在指定范围内检测所有碰撞体
            // 将结果存储在_scanCache数组中，只检测TargetLayerMask层上的对象
            int count = Physics2D.OverlapCircleNonAlloc(transform.position, range, _scanCache, TargetLayerMask);

            // 初始化最近目标的距离为最大浮点数
            float closestSq = float.MaxValue;
            // 初始化最近目标为null
            Transform nearest = null;

            // 根据自身标签确定目标标签
            // 如果自己是Player，则目标是Enemy；反之亦然
            string targetTag = CompareTag("Player") ? "Enemy" : "Player";

            for (int i = 0; i < count && i < _scanCache.Length; i++)
            {
                var col = _scanCache[i];
                if (col == null) continue;
                // 跳过自身碰撞体，避免把自己当成目标
                if (col.transform.IsChildOf(transform)) continue;

                if (!col.CompareTag(targetTag)) continue;

                float sq = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sq < closestSq)
                {
                    closestSq = sq;
                    nearest = col.transform;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 在战斗状态下刷新目标
        /// 当检测到有效目标时，会自动切换到最近的目标
        /// </summary>
        public void RefreshTargetInCombat()
        {
            _combatScanTimer -= Time.deltaTime;
            if (_combatScanTimer > 0f) return;

            _combatScanTimer = _combatScanInterval;

            var nearest = FindNearestTarget(DetectionRange);
            if (nearest != null) Target = nearest;
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
        /// 判断是否在最大攻击范围内
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
        /// 判断是否在最小攻击范围内（需要停车的距离）
        /// </summary>
        public bool IsInAttackMinRange()
        {
            if (Target == null) return false;
            return Vector2.Distance(transform.position, Target.position) <= AttackMinRange;
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
            return dis <= range;
        }

        #endregion

        #region ----- 移动相关 -------------------------
        
        public virtual void MoveToward(Vector3 targetPos)
        {
            Agent.destination = targetPos;
            Rotate(Target != null ? Target.position : targetPos);
        }

        /// <summary>
        /// 旋转物体以面向目标位置
        /// </summary>
        /// <param name="targetPos">目标位置</param>
        public virtual void Rotate(Vector3 targetPos)
        {
            // 计算从当前位置到目标位置的方向向量，并归一化
            Vector2 dir = (targetPos - transform.position).normalized;

            // 如果方向太小，则不旋转
            if(dir.sqrMagnitude < 0.01f) return;

            // 计算目标角度（弧度转角度）
            float z = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.Euler(0, 0, z);

            // 平滑旋转
            float currentAngle = Body.rotation.eulerAngles.z;
            float smoothAngle = Mathf.LerpAngle(currentAngle, z, _rotateSpeed * Time.fixedDeltaTime);

            // ----- 应用旋转 -------------------------
            // 应用旋转到主体
            Body.rotation = Quaternion.Euler(0, 0, smoothAngle);
            // 应用旋转到阴影
            Shadow.rotation = Quaternion.Euler(0, 0, smoothAngle);
            RotateDamageVFX();

            // 如果武器存在，应用旋转到武器
            if(Weapon != null)
            {
                Weapon.rotation = Quaternion.Euler(0, 0, smoothAngle);
            }

            // ----- 腿部旋转 -------------------------
            // 获取物体的速度
            Vector2 v = Agent != null
                ? Agent.velocity
                : (Rb != null ? Rb.velocity : Vector2.zero);

            // 如果速度太小或腿部不存在，则不旋转腿部
            if(v.sqrMagnitude < 0.01f || Legs == null) return;

            // 计算腿部目标角度
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

        public virtual void RotateDamageVFX()
        {
            if(_damageVfxNode == null) return;

            _damageVfxNode.rotation = Body.rotation;
            _damageVfxNode.localPosition = 
            new Vector3(
                Mesh.localPosition.x, 
                Mesh.localPosition.y, 
                _damageVfxNode.localPosition.z);
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
                Agent.stopDistance = 0.25f;
            }
        }

        public void StartPatrolMovement()
        {
            if (Agent == null) return;
            Agent.maxSpeed = _hasPatrolMoveSpeed ? _patrolMoveSpeed : _moveSpeed;
            Agent.stopDistance = 0.25f;
        }
        
        public void StopMovement()
        {
            if (Agent != null)
            {
                Agent.maxSpeed = 0;
                Agent.destination = transform.position;
            }
            else if (Rb != null)
            {
                Rb.velocity = Vector2.zero;
            }
        }

        public void SetAgentActive(bool active)
        {
            if (Agent != null) Agent.enabled = active;
        }

        public void SetAgentRvoLocked(bool locked)
        {
            if (Agent == null) return;
            var rvo = Agent.rvoSettings;
            rvo.locked = locked;
            Agent.rvoSettings = rvo;
        }

        /// <summary>
        /// 从枪口向目标发射射线检测是否有无障碍物。
        /// 使用 RaycastAll 跳过自身碰撞体，确保不会被自己的 collider 挡住。
        /// </summary>
        public virtual bool HasLineOfSightToTarget()
        {
            if (Target == null) return false;

            Vector3 origin = Muzzle != null ? Muzzle.position : transform.position;
            Vector2 direction = Target.position - origin;
            float distance = direction.magnitude;

            RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction.normalized, distance, TargetLayerMask);
            foreach (var hit in hits)
            {
                if (hit.collider == null) continue;
                // 跳过自身碰撞体
                if (hit.collider.transform.IsChildOf(transform)) continue;
                // 第一个非自身碰撞体是目标 → 视线畅通
                if (hit.collider.transform == Target || hit.collider.transform.IsChildOf(Target))
                    return true;
                // 被其他物体（障碍物）阻挡
                return false;
            }
            return false;
        }

        public bool IsGrounded()
        {
            // 使用 sqrMagnitude (平方长度) 比 Distance (开方运算) 性能更高
            // 这里需要用2维，z轴一直被使用，会导致无法计算到0.1f以下
            Vector2 pos = Mesh.transform.localPosition;
            if (pos.sqrMagnitude <= 0.001f || pos.y <= 0) // 0.01f 的平方
            {
                float z = Mesh.transform.localPosition.z;
                Mesh.transform.localPosition = new Vector3(pos.x, 0, z);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 对 Mesh 应用重力物理，使敌人下落。
        /// </summary>
        public void ApplyGravityToMesh(ref float verticalVelocity)
        {
            Vector3 worldPos = Mesh.TransformPoint(Mesh.localPosition);
            worldPos.y += verticalVelocity * Time.deltaTime;
            verticalVelocity += GameConstants.EnemyGravity * Time.deltaTime;
            Mesh.localPosition = Mesh.InverseTransformPoint(worldPos);
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
            if(EnemyInstanceSystem.GetData(enemyId).CurrentHealth.Value <= 0)
            {
                return true;
            }
            return false;
        }

        public void ShowDeathVFX()
        {
            HideDamageVFX();

            var obj = ObjectPoolUtility.GetObject(pf_DeathVFX, Mesh.position, Quaternion.identity);

            var explosion = obj.GetComponent<Explosion>();
            if (explosion != null) explosion.Init(0, 0);

            this.GetUtility<ITimerUtility>().AddOnce(() => {
                this.GetUtility<IObjectPoolUtility>().PushObject(obj);
            }, 5f);

            Mesh.gameObject.SetActive(false);
            Shadow.gameObject.SetActive(false);
        }

        public void ShowDamageVFX()
        {
            SetDamageVfxEmission(true);
        }

        public void HideDamageVFX()
        {
            SetDamageVfxEmission(false);
        }

        private void SetDamageVfxEmission(bool enable)
        {
            if (_damageVfxParticles == null) return;
            foreach (var ps in _damageVfxParticles)
            {
                if (ps == null) continue;
                var emission = ps.emission;
                emission.enabled = enable;
                ps.Play();
            }
        }

        private Coroutine _flashCoroutine;
        private const float FlashDuration = 0.12f;

        public void Flash()
        {
            if (_meshRenderers == null || _meshRenderers.Count == 0) return;

            if (_flashCoroutine != null)
                StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine());
        }

        /// <summary>
        /// 闪烁效果的协程函数
        /// </summary>
        private IEnumerator FlashRoutine()
        {
            // 初始化计时器，设置为闪烁持续时间
            float timer = FlashDuration;

            // 当计时器大于0时，继续闪烁效果
            while (timer > 0f)
            {
                timer -= Time.deltaTime;
                float t = timer / FlashDuration + 0.5f;

                // 设置材质中的闪烁强度参数
                _flashBlock.SetFloat("_FlashAmount", t);
                // 将属性块应用到所有网格渲染器
                foreach (var r in _meshRenderers)
                    r.SetPropertyBlock(_flashBlock);
                // 等待下一帧
                yield return null;
            }

            // 清除 PropertyBlock，恢复材质默认状态
            foreach (var r in _meshRenderers)
                r.SetPropertyBlock(null);
        }


        #endregion

        #region ----- 武器 -------------------------

        public abstract void Attack();

        public abstract void Shoot();

        protected bool _isBurstShooting;

        /// <summary>
        /// 供子类 Attack() 调用，启动连发射击协程。
        /// </summary>
        protected void BurstAttack()
        {
            if (_isBurstShooting) return;
            StartCoroutine(BurstShootRoutine());
        }

        protected IEnumerator BurstShootRoutine()
        {
            _isBurstShooting = true;

            for (int i = 0; i < _burstCount; i++)
            {
                Shoot();
                if (i < _burstCount - 1)
                    yield return new WaitForSeconds(_burstInterval);
            }

            _isBurstShooting = false;
        }

        #endregion

        #region ----- 杂项 -------------------------

        public void ForcePush(Vector2 forcePos, int force, float torque)
        {
            if (IsDead()) return;
            ChangeState<EnemyIdleState>();

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
                EnemyPatrolState => "Patrol",
                EnemyLockState => "Lock",
                _ => "Unknown"
            };
        }

        #endregion

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, DetectionRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, AttackMaxRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, AttackMinRange);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, StopRange);
        }
    }
}
