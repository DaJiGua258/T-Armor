using System;
using System.Collections.Generic;
using UnityEngine;
using QFramework.Utility;
using QFramework.ViewController.Misc;

namespace QFramework.ViewController.Player
{
    public enum ProjectileMode
    {
        Normal,  // 常规检测
        Pointed,   // 用于空中射向地面的子弹，飞行过程中只检测角色碰撞，碰撞后爆炸，
        PointOnly,  // 飞行不检测碰撞，到目标点后直接爆炸
    }

    public class Projectile : MonoBehaviour, IController
    {
        /// <summary>
        /// 获取架构实例
        /// </summary>
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        [Header("检测模式")]
        [SerializeField] private ProjectileMode _detectMode;
        /// <summary>
        /// 设置检测模式
        /// </summary>
        public void SetDetectMode(ProjectileMode mode) => _detectMode = mode;

        [Header("组件")]
        [SerializeField] private LayerMask _layerMask;
        /// <summary>
        /// 设置碰撞检测层级
        /// </summary>
        public void SetLayerMask(LayerMask mask) => _layerMask = mask;
        private Vector2 _moveDirection;  // 移动方向
        private Transform _bulletMesh;  // 子弹网格变换
        private GameObject _vfxNode;  // VFX节点
        [Header("爆炸参数")]
        [SerializeField] private bool _hasExplosion;
        [SerializeField] private GameObject _pf_bulletExplosionVFX;

        [Header("追踪参数")]
        [SerializeField] private bool _enableHoming;
        [SerializeField] private float _homingRotationSpeed = 360f;  // 每秒转向角度
        [Header("垂直发射（需启用追踪）")]
        [SerializeField] private bool _enableVerticalLaunch;
        [SerializeField] private float _verticalLaunchHeight = 5f;

        [Header("终点参数（定点模式）")]
        [SerializeField] private float _arrivalThreshold = 1f;
        [SerializeField] private float _maxBufferTime = 1f;  // 超过初始目标距离后的最大缓冲时间

        [Header("检测参数")]
        [SerializeField] private float _minRayDistance = 1f;

        public event Action<Projectile> OnExploded;  // 爆炸完成事件

        private bool _hasExploded;  // 是否已爆炸
        private int _damage;  // 伤害值
        private int _speed;  // 飞行速度
        private Vector3 _targetPosition;  // 目标位置
        private GameObject _owner;  // 发射者，检测时跳过自身
        private Transform _homingTarget;  // 追踪目标
        private Vector3 _homingFixedPosition;  // 固定追踪位置
        private bool _useFixedHomingPosition;  // 是否使用固定位置

        private float _initialDistanceToTarget;  // 初始到目标距离
        private float _distanceTraveled;  // 已飞行距离
        private float _excessTime;  // 超出缓冲时间
        private float _launchStartY;  // 发射起始Y坐标

        private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[16];  // 碰撞检测缓存
        private static readonly string[] _hitTags = { "Player", "Enemy", "Env" };  // 可命中标签列表

        #region ----- 工具方法 -------------------------

        private void SetVfxEmission(bool enabled)
        {
            if (_vfxNode == null) return;
            var particles = _vfxNode.GetComponentsInChildren<ParticleSystem>(true);
            // 遍历所有粒子系统，设置发射状态
            foreach (var ps in particles)
            {
                var emission = ps.emission;
                emission.enabled = enabled;
            }
        }

        private bool TagMatches(string tag)
        {
            // 遍历标签列表，检测是否匹配
            foreach (var t in _hitTags)
            {
                if (t == tag) return true;
            }

            return false;
        }

        #endregion

        #region ----- 生命周期 -------------------------

        void Awake()
        {
            _bulletMesh = transform.Find("Mesh");
            Transform vfxT = transform.Find("VFX");
            if (vfxT != null) _vfxNode = vfxT.gameObject;
        }

        void Update()
        {

        }

        void FixedUpdate()
        {
            if (_hasExploded) return;
            transform.position += (Vector3)_moveDirection * _speed * Time.fixedDeltaTime;
            _distanceTraveled += _speed * Time.fixedDeltaTime;
            UpdateHoming();
            UpdateRotation();
            Detect();
        }

        #endregion

        #region ----- 碰撞检测 -------------------------

        /// <summary>
        ///
        /// </summary>
        private void Detect()
        {
            switch (_detectMode)
            {
                case ProjectileMode.Pointed:
                    DetectPointed();
                    break;
                case ProjectileMode.PointOnly:
                    DetectPointOnly();
                    break;
                default:
                    DetectNormal();
                    break;
            }
        }

        /// <summary>
        ///
        /// </summary>
        private void DetectNormal()
        {
            int hitCount = HitDetectionUtility.BulletRaycastAll(transform.position, _moveDirection, _speed, _layerMask, _minRayDistance, Color.red, _hitBuffer);
            if (hitCount == 0) return;

            HashSet<GameObject> processed = new HashSet<GameObject>();
            bool hasExploded = false;

            // 遍历所有碰撞结果，处理伤害
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = _hitBuffer[i];
                if (!TagMatches(hit.collider.tag)) continue;
                // 跳过发射者自身碰撞体
                if (_owner != null && hit.collider.transform.IsChildOf(_owner.transform)) continue;

                if (!hasExploded)
                {
                    hasExploded = true;
                    Explode(hit.point);
                }

                if (!processed.Add(hit.collider.gameObject)) continue;

                HitDetectionUtility.ProcessHit(hit.collider, _damage);
            }
        }

        /// <summary>
        ///
        /// </summary>
        private void DetectPointed()
        {
            RaycastHit2D hit = HitDetectionUtility.BulletRaycast(transform.position, _moveDirection, _speed, _layerMask, _minRayDistance, Color.yellow);

            bool hasCollision = hit.collider != null;
            // 如果命中发射者自身，当作未命中处理
            if (hasCollision && _owner != null && hit.collider.transform.IsChildOf(_owner.transform))
                hasCollision = false;

            bool hasArrived = !_enableVerticalLaunch && Vector2.Distance(transform.position, _targetPosition) <= _arrivalThreshold;

            if (hasCollision || hasArrived)
            {
                Vector3 explosionPoint = hasCollision ? hit.point : _targetPosition;

                if (hasCollision)
                {
                    Debug.Log($"[Projectile] Hit: {hit.collider.name} | Tag: {hit.collider.tag} | Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                    HitDetectionUtility.ProcessHit(hit.collider, _damage);
                }

                Explode(explosionPoint);
                return;
            }

            // 超过初始目标距离后启用缓冲时间，防止无限追踪
            if (!_enableVerticalLaunch && _distanceTraveled > _initialDistanceToTarget)
            {
                _excessTime += Time.fixedDeltaTime;
                if (_excessTime > _maxBufferTime)
                    Explode(transform.position);
            }
        }

        /// <summary>
        /// 到点爆炸模式：飞行全程不检测碰撞，到达目标位置后直接爆炸
        /// </summary>
        private void DetectPointOnly()
        {
            bool hasArrived = !_enableVerticalLaunch && Vector2.Distance(transform.position, _targetPosition) <= _arrivalThreshold;
            if (hasArrived)
            {
                Explode(_targetPosition);
                return;
            }

            // 超过初始目标距离后启用缓冲时间，防止无限追踪
            if (_distanceTraveled > _initialDistanceToTarget)
            {
                _excessTime += Time.fixedDeltaTime;
                if (_excessTime > _maxBufferTime)
                    Explode(transform.position);
            }
        }

        #endregion

        #region ----- 初始化 -------------------------

        /// <summary>
        /// 初始化子弹（方向、速度、伤害）
        /// </summary>
        /// <param name="direction">飞行方向</param>
        /// <param name="speed">飞行速度</param>
        /// <param name="damage">伤害值</param>
        /// <param name="owner">发射者（可选），检测时跳过自身碰撞</param>
        public void InitBullet(Vector3 direction, int speed, int damage, GameObject owner = null)
        {
            _damage = damage;
            _speed = speed;
            _owner = owner;
            _hasExploded = false;
            _bulletMesh.gameObject.SetActive(true);
            SetVfxEmission(true);
            _moveDirection = ((Vector2)direction).normalized;
            _launchStartY = transform.position.y;
        }

        /// <summary>
        /// 初始化抛射体（目标位置追踪）
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="speed">飞行速度</param>
        /// <param name="damage">伤害值</param>
        /// <param name="owner">发射者（可选）</param>
        public void InitProjectile(Vector3 targetPosition, int speed, int damage, GameObject owner = null)
        {
            _targetPosition = targetPosition;
            _initialDistanceToTarget = Vector3.Distance(transform.position, targetPosition);
            _distanceTraveled = 0f;
            _excessTime = 0f;
            InitBullet((targetPosition - transform.position).normalized, speed, damage, owner);
        }

        #endregion

        #region ----- 飞行控制 -------------------------

        /// <summary>
        /// 设置追踪目标
        /// </summary>
        /// <param name="target">追踪目标，为null时缓存最后位置继续追踪</param>
        public void SetHomingTarget(Transform target)
        {
            if (target != null)
            {
                _homingTarget = target;
                _enableHoming = true;
                _useFixedHomingPosition = false;
            }
            else if (_homingTarget != null)
            {
                // 缓存上一个目标的位置，继续追踪该位置
                _homingFixedPosition = _homingTarget.position;
                _homingTarget = null;
                _useFixedHomingPosition = true;
            }
        }

        /// <summary>
        /// 设置固定追踪位置（无目标时追踪坐标点）
        /// </summary>
        /// <param name="position">目标坐标</param>
        public void SetHomingPosition(Vector3 position)
        {
            _homingFixedPosition = position;
            _useFixedHomingPosition = true;
            _enableHoming = true;
            _homingTarget = null;
        }

        /// <summary>
        /// 设置垂直发射模式（先升空再追踪）
        /// </summary>
        /// <param name="enable">是否启用</param>
        public void SetVerticalLaunch(bool enable)
        {
            _enableVerticalLaunch = enable;
        }

        private void UpdateHoming()
        {
            if (!_enableHoming) return;

            if (_enableVerticalLaunch)
            {
                UpdateVerticalLaunch();
                return;
            }

            // 标准追踪：确定目标位置
            Vector3 targetPos;
            if (_homingTarget != null)
            {
                targetPos = _homingTarget.position;
            }
            else if (_useFixedHomingPosition)
            {
                targetPos = _homingFixedPosition;
            }
            else
            {
                Debug.LogWarning($"[Projectile] 启用了追踪但未设置追踪目标 (预制体: {gameObject.name})");
                _enableHoming = false;
                return;
            }

            _targetPosition = targetPos;
            Vector3 dir = (targetPos - transform.position).normalized;
            _moveDirection = Vector3.RotateTowards(_moveDirection, dir,
                _homingRotationSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f).normalized;
        }

        /// <summary>
        ///
        /// </summary>
        private void UpdateVerticalLaunch()
        {
            float peakY = _launchStartY + _verticalLaunchHeight;

            if (transform.position.y < peakY)
            {
                _moveDirection = Vector2.up;
                if (_homingTarget != null)
                    _targetPosition = _homingTarget.position;
                else if (_useFixedHomingPosition)
                    _targetPosition = _homingFixedPosition;
                return;
            }

            // 到达高度后关闭垂直发射，后续走标准追踪
            _enableVerticalLaunch = false;
        }

        /// <summary>
        ///
        /// </summary>
        private void UpdateRotation()
        {
            if (_moveDirection.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(_moveDirection.y, _moveDirection.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        #endregion

        #region ----- 爆炸处理 -------------------------

        private void Explode(Vector3 hitPos)
        {
            if (_hasExploded) return;
            _hasExploded = true;

            OnExploded?.Invoke(this);

            _moveDirection = Vector2.zero;
            _bulletMesh.gameObject.SetActive(false);
            SetVfxEmission(false);

            var ob = this.GetUtility<IObjectPoolUtility>();
            var timer = this.GetUtility<ITimerUtility>();

            GameObject explosionVFX = ob.GetObject(_pf_bulletExplosionVFX, hitPos, GetExplosionRotation());
            if (_hasExplosion)
            {
                var explosion = explosionVFX.GetComponent<Explosion>();
                if (explosion != null) explosion.Init(_damage, _layerMask);
            }

            timer.AddOnce(
                () => ob.PushObject(explosionVFX),
                3f,
                () => ob.PushObject(gameObject)
            );
        }

        private Quaternion GetExplosionRotation()
        {
            if (_detectMode == ProjectileMode.PointOnly)
                return Quaternion.identity;
            if (_detectMode == ProjectileMode.Pointed)
                return Quaternion.Euler(0f, 0f, 90f);
            Vector3 euler = transform.rotation.eulerAngles;
            euler.z += 180f;
            return Quaternion.Euler(euler);
        }

        #endregion

        #region ----- 编辑器辅助 -------------------------

        private void OnDrawGizmosSelected()
        {
            // 在预制体编辑模式下可视化最小射线检测距离
            Vector3 origin = transform.position;
            Vector3 direction = transform.right;
            Gizmos.color = _detectMode >= ProjectileMode.Pointed ? Color.yellow : Color.red;
            Gizmos.DrawRay(origin, direction * _minRayDistance);
            Gizmos.DrawWireSphere(origin + direction * _minRayDistance, 0.1f);
        }

        #endregion
    }
}
