using QFramework.Event;
using QFramework.ViewController.Enemy;
using UnityEngine;

namespace QFramework.ViewController.Player
{
    public class MTT : AbstractHangerWeapon
    {
        [Header("炮台设置")]
        [SerializeField] private float _detectionRadius = 10f;
        [SerializeField] private LayerMask _targetLayerMask;
        [SerializeField] private float _rotationSpeed = 10f;

        private Transform _currentTarget;
        private Collider2D[] _scanCache = new Collider2D[10];
        private float _scanTimer;
        private float _autoFireTimer;
        private const float SCAN_INTERVAL = 2f;

        protected override void Start()
        {
            base.Start();
            TypeEventSystem.Global.Register<StatsEvent.OnEnemyKilled>(
                e => ScanForTarget()
            ).UnRegisterWhenGameObjectDestroyed(gameObject);
        }

        protected override void OnTrigger()
        {
            IsActive = !IsActive;
        }

        protected override void UpdateActive()
        {
            if (!IsActive) return;

            if (_currentTarget != null && !_currentTarget.gameObject.activeInHierarchy)
                _currentTarget = null;

            _scanTimer -= Time.deltaTime;
            if (_scanTimer <= 0f)
            {
                _scanTimer = SCAN_INTERVAL;
                ScanForTarget();
            }

            if (_currentTarget != null)
            {
                RotateTowardTarget();
                AutoFire();
            }
        }

        private void ScanForTarget()
        {
            int count = Physics2D.OverlapCircleNonAlloc(
                transform.position, _detectionRadius, _scanCache, _targetLayerMask);

            float closestSq = float.MaxValue;
            Transform nearest = null;

            for (int i = 0; i < count && i < _scanCache.Length; i++)
            {
                var col = _scanCache[i];
                if (col == null) continue;
                if (!col.CompareTag("Enemy")) continue;
                if (!col.gameObject.activeInHierarchy) continue;
                var enemyRoot = col.GetComponentInParent<AbstractEnemy>();
                if (enemyRoot != null && enemyRoot.IsDead()) continue;

                float sq = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sq < closestSq)
                {
                    closestSq = sq;
                    nearest = col.transform;
                }
            }

            _currentTarget = nearest;
        }

        private void RotateTowardTarget()
        {
            Vector3 dir = _currentTarget.position - transform.position;
            float targetZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            float currentZ = transform.rotation.eulerAngles.z;
            float smoothZ = Mathf.LerpAngle(currentZ, targetZ, _rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, smoothZ);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);

            if (_currentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, _currentTarget.position);
            }
        }

        private void AutoFire()
        {
            _autoFireTimer += Time.deltaTime;

            if (_autoFireTimer >= 60f / WeaponDataModel.Rpm && CanFire())
            {
                Vector3 dir = (_currentTarget.position - Muzzle.position).normalized;
                SpawnBullet(dir, Muzzle.position, Muzzle.rotation);
                ConsumeShot();
                _autoFireTimer = 0;
            }
        }
    }
}
