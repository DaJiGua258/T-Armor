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

        // Debug 信息（临时）
        //[Header("Debug 信息")]
        //[SerializeField] private string _debugState = "未激活";
        //[SerializeField] private string _debugTarget = "无";
        //[SerializeField] private float _debugScanCountdown;
        //[SerializeField] private int _debugScanFound;
        //[SerializeField] private int _debugScanEmpty;
        //[SerializeField] private string _debugScanDetail = "";

        private Transform _currentTarget;
        private Collider2D[] _scanCache = new Collider2D[20];
        private float _scanTimer;
        private float _autoFireTimer;
        private const float SCAN_INTERVAL = 0.5f;

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
            if (!IsActive)
            {
                //_debugState = "未激活";
                //_debugTarget = "无";
                return;
            }

            //_debugState = "激活";

            if (_currentTarget != null && !_currentTarget.gameObject.activeInHierarchy)
            {
                _currentTarget = null;
                //_debugTarget = "目标已失效";
            }

            _scanTimer -= Time.deltaTime;
            //_debugScanCountdown = _scanTimer;
            if (_scanTimer <= 0f)
            {
                _scanTimer = SCAN_INTERVAL;
                ScanForTarget();
            }

            if (_currentTarget != null)
            {
                //_debugTarget = _ScanForTargetText();
                RotateTowardTarget();
                AutoFire();
            }
            //else
            //{
            //    _debugTarget = "搜索中...";
            //}
        }

        private void ScanForTarget()
        {
            int count = Physics2D.OverlapCircleNonAlloc(
                transform.position, _detectionRadius, _scanCache, _targetLayerMask);

            //HashSet<string> tags = new HashSet<string>();
            int filteredInactive = 0, filteredDead = 0;
            float closestSq = float.MaxValue;
            Transform nearest = null;

            for (int i = 0; i < count && i < _scanCache.Length; i++)
            {
                var col = _scanCache[i];
                if (col == null) continue;
                //tags.Add(col.tag);
                if (!col.gameObject.activeInHierarchy) { filteredInactive++; continue; }
                var enemyRoot = col.GetComponentInParent<AbstractEnemy>();
                if (enemyRoot == null) continue;
                if (enemyRoot.IsDead()) { filteredDead++; continue; }

                float sq = ((Vector2)col.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sq < closestSq)
                {
                    closestSq = sq;
                    nearest = col.transform;
                }
            }

            //_debugScanDetail = $"总数:{count} tag:[{string.Join(",", tags)}] 未激活:{filteredInactive} 死亡:{filteredDead}";

            _currentTarget = nearest;

            //if (_currentTarget != null)
            //    _debugScanFound++;
            //else
            //    _debugScanEmpty++;

            //_debugTarget = _currentTarget != null
            //    ? _ScanForTargetText()
            //    : "搜索中...";
        }

        //private string _ScanForTargetText()
        //{
        //    var enemy = _currentTarget.GetComponentInParent<AbstractEnemy>();
        //    string typeName = enemy != null ? enemy.enemyType.ToString() : "?";
        //    int id = enemy != null ? enemy.enemyId : -1;
        //    float dist = Vector2.Distance(transform.position, _currentTarget.position);
        //    return $"{typeName} - {id} (距离:{dist:F1})";
        //
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
