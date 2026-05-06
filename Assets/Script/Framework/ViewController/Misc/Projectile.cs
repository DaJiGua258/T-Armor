using System.Collections.Generic;
using UnityEngine;
using QFramework.Event;
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

        [Header("终点参数（定点模式）")]
        [SerializeField] private float _arrivalThreshold = 1f;

        [Header("检测参数")]
        [SerializeField] private float _minRayDistance = 1f;

        private bool _hasExploded;
        private int _damage;
        private Vector3 _targetPosition;

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

        void Start()
        {
            TypeEventSystem.Global.Register<WeaponEvent.UpdateBulletLayerMask>(OnUpdateBulletLayerMask);
        }

        private void OnUpdateBulletLayerMask(WeaponEvent.UpdateBulletLayerMask e)
        {
            _layerMask = e.LayerMask;
        }

        void Update()
        {
            
        }

        void FixedUpdate()
        {
            if (_hasExploded) return;
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

            // 第一个命中点产生爆炸（仅白名单tag触发）
            if (!TagMatches(_hitBuffer[0].collider.tag)) return;
            Explode(_hitBuffer[0].point);

            // 处理所有唯一目标的伤害（去重，避免同一对象多个碰撞箱重复伤害）
            HashSet<GameObject> processed = new HashSet<GameObject>();
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit2D hit = _hitBuffer[i];
                if (!TagMatches(hit.collider.tag)) continue;
                if (!processed.Add(hit.collider.gameObject)) continue;
               
                HitDetectionUtility.ProcessHit(hit.collider, _damage);
            }
        }

        private void DetectPointed()
        {
            RaycastHit2D hit = HitDetectionUtility.BulletRaycast(_rb, _layerMask, _minRayDistance, Color.yellow);

            bool hasCollision = hit.collider != null;
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

        public void InitBullet(Vector3 direction, int speed, int damage)
        {
            _damage = damage;
            _hasExploded = false;
            _bulletMesh.gameObject.SetActive(true);
            _rb.velocity = ((Vector2)direction).normalized * speed;
        }

        public void InitProjectile(Vector3 targetPosition, int speed, int damage)
        {
            _targetPosition = targetPosition;
            InitBullet((targetPosition - transform.position).normalized, speed, damage);
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
