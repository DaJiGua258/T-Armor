using System.Collections.Generic;
using UnityEngine;
using QFramework.Utility;
using QFramework.ViewController.Misc;

namespace QFramework.ViewController.Player
{
    public enum ProjectileMode
    {
        Normal,
        Pointed,
        PointOnly,  // 飞行不检测碰撞，到目标点后直接爆炸
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour, IController
    {
        public IArchitecture GetArchitecture() => TArmorArchitecture.Interface;

        [Header("检测模式")]
        [SerializeField] private ProjectileMode _detectMode;
        public void SetDetectMode(ProjectileMode mode) => _detectMode = mode;

        [Header("组件")]
        [SerializeField] private LayerMask _layerMask;
        public void SetLayerMask(LayerMask mask) => _layerMask = mask;
        private Rigidbody2D _rb;
        private Transform _bulletMesh;
        private GameObject _vfxNode;
        [Header("爆炸参数")]
        [SerializeField] private bool _hasExplosion;
        [SerializeField] private GameObject _pf_bulletExplosionVFX;

        [Header("追踪参数")]
        [SerializeField] private bool _enableHoming;
        [SerializeField] private float _homingRotationSpeed = 360f;  // 每秒转向角度

        [Header("终点参数（定点模式）")]
        [SerializeField] private float _arrivalThreshold = 1f;

        [Header("检测参数")]
        [SerializeField] private float _minRayDistance = 1f;

        private bool _hasExploded;
        private int _damage;
        private int _speed;
        private Vector3 _targetPosition;
        private GameObject _owner;  // 发射者，检测时跳过自身
        private Transform _homingTarget;

        private static readonly RaycastHit2D[] _hitBuffer = new RaycastHit2D[16];
        private static readonly string[] _hitTags = { "Player", "Enemy", "Env" };

        private bool TagMatches(string tag)
        {
            for (int i = 0; i < _hitTags.Length; i++)
            {
                if (_hitTags[i] == tag) return true;
            }
            return false;
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
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
            UpdateHoming();
            UpdateRotation();
            Detect();
        }

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

        private void DetectNormal()
        {
            int hitCount = HitDetectionUtility.BulletRaycastAll(_rb, _layerMask, _minRayDistance, Color.red, _hitBuffer);
            if (hitCount == 0) return;

            HashSet<GameObject> processed = new HashSet<GameObject>();
            bool hasExploded = false;

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

        private void DetectPointed()
        {
            RaycastHit2D hit = HitDetectionUtility.BulletRaycast(_rb, _layerMask, _minRayDistance, Color.yellow);

            bool hasCollision = hit.collider != null;
            // 如果命中发射者自身，当作未命中处理
            if (hasCollision && _owner != null && hit.collider.transform.IsChildOf(_owner.transform))
                hasCollision = false;

            bool hasArrived = Vector2.Distance(transform.position, _targetPosition) <= _arrivalThreshold;

            if (!hasCollision && !hasArrived) return;

            Vector3 explosionPoint = hasCollision ? hit.point : _targetPosition;

            if (hasCollision)
            {
                Debug.Log($"[Projectile] Hit: {hit.collider.name} | Tag: {hit.collider.tag} | Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
                HitDetectionUtility.ProcessHit(hit.collider, _damage);
            }

            Explode(explosionPoint);
        }

        // 到点爆炸模式：飞行全程不检测碰撞，到达目标位置后直接爆炸
        private void DetectPointOnly()
        {
            bool hasArrived = Vector2.Distance(transform.position, _targetPosition) <= _arrivalThreshold;
            if (!hasArrived) return;

            Explode(_targetPosition);
        }

        public void InitBullet(Vector3 direction, int speed, int damage, GameObject owner = null)
        {
            _damage = damage;
            _speed = speed;
            _owner = owner;
            _hasExploded = false;
            _bulletMesh.gameObject.SetActive(true);
            _rb.velocity = ((Vector2)direction).normalized * speed;
        }

        public void InitProjectile(Vector3 targetPosition, int speed, int damage, GameObject owner = null)
        {
            _targetPosition = targetPosition;
            InitBullet((targetPosition - transform.position).normalized, speed, damage, owner);
        }

        public void SetHomingTarget(Transform target)
        {
            _homingTarget = target;
            _enableHoming = target != null;
        }

        private void UpdateHoming()
        {
            if (!_enableHoming) return;

            if (_homingTarget == null)
            {
                Debug.LogWarning($"[Projectile] 启用了追踪但未设置追踪目标 (预制体: {gameObject.name})");
                _enableHoming = false;
                return;
            }

            // 同步目标位置，使到达检测跟随追踪目标
            _targetPosition = _homingTarget.position;

            Vector3 targetDir = (_homingTarget.position - transform.position).normalized;
            Vector3 newDir = Vector3.RotateTowards(_rb.velocity.normalized, targetDir,
                _homingRotationSpeed * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f);
            _rb.velocity = newDir * _speed;
        }

        private void UpdateRotation()
        {
            if (_rb.velocity.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(_rb.velocity.y, _rb.velocity.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        private void Explode(Vector3 hitPos)
        {
            if (_hasExploded) return;
            _hasExploded = true;

            _rb.velocity = Vector2.zero;
            _bulletMesh.gameObject.SetActive(false);
            if (_vfxNode != null) _vfxNode.SetActive(false);

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

        private void OnDrawGizmosSelected()
        {
            // 在预制体编辑模式下可视化最小射线检测距离
            Vector3 origin = transform.position;
            Vector3 direction = transform.right;
            Gizmos.color = _detectMode >= ProjectileMode.Pointed ? Color.yellow : Color.red;
            Gizmos.DrawRay(origin, direction * _minRayDistance);
            Gizmos.DrawWireSphere(origin + direction * _minRayDistance, 0.1f);
        }
    }
}
