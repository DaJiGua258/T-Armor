using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using QFramework.Enum;
using QFramework.UtilityKit;
using QFramework.ViewController.Enemy;
using UnityEngine;

namespace QFramework.Manager
{
    /// <summary>
    /// 敌人生成管理器（场景级单例）。
    /// 当前阶段负责运输船生成与挂载敌人实例化。
    /// </summary>
    public class EnemySpawnerManager : SceneMonoSingleton<EnemySpawnerManager>
    {
        private const int MaxDropperCargoCount = 6;
        private enum PatrolDistanceBucket
        {
            Short,
            Medium,
            Long
        }

        #region ----- Inspector 配置 -------------------------

        [Header("运输船配置")]
        [SerializeField] private Dropper _dropperPrefab;
        [SerializeField] private Transform _spawnRoot;
        [SerializeField] private bool _spawnOnStart = true;
        [SerializeField, Min(1)] private int _waveCountPerSpawnProcess = 1;
        [SerializeField, Min(0f)] private float _waveIntervalSeconds = 0f;
        [SerializeField, Min(1)] private int _dropperCountPerSpawn = 1;
        [SerializeField, Min(0f)] private float _shipSpawnIntervalMaxSeconds = 0f;

        [Header("运输船路径点")]
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _dropPoint;
        [SerializeField] private Transform _endPoint;
        [SerializeField, Min(0f)] private float _spawnPointRandomRadius = 0f;
        [SerializeField, Min(0f)] private float _dropPointRandomRadius = 0f;
        [SerializeField, Min(1)] private int _walkablePointSampleAttempts = 10;
        [SerializeField, Min(0f)] private float _walkableSnapMaxDistance = 2f;

        [Header("巡逻敌人投送配置")]
        [SerializeField] private Transform _player;
        [SerializeField, Min(0f)] private float _patrolDropIntervalSeconds = 0f;
        [SerializeField, Min(1)] private int _patrolDropShipCountPerInterval = 1;
        [SerializeField, Min(0f)] private float _patrolDropMinDistanceFromPlayer = 0f;

        [Header("挂载敌人模板池（每个模板最多6个）")]
        [SerializeField] private List<CargoEnemyTemplate> _cargoTemplates;

        [Header("Gizmos调试")]
        [SerializeField] private bool _drawSpawnGizmos = true;
        [SerializeField, Min(0.05f)] private float _gizmoPointRadius = 0.25f;

        private Coroutine _spawnProcessCoroutine;
        private Coroutine _patrolDropProcessCoroutine;
        private readonly List<Vector3> _lastSpawnPoints = new List<Vector3>();
        private readonly List<Vector3> _lastDropPoints = new List<Vector3>();
        private readonly List<List<Vector3>> _lastPatrolPathPoints = new List<List<Vector3>>();

        #endregion

        #region ----- 生命周期 -------------------------

        private void Start()
        {
            if (_spawnOnStart) StartSpawnProcess();
            StartPatrolDropProcessIfNeeded();
        }

        #endregion

        #region ----- 对外 API -------------------------

        /// <summary>
        /// 生成一架运输船并注入路径与挂载敌人。
        /// </summary>
        public Dropper SpawnDropshipWave()
        {
            if (!CanSpawnDropper()) return null;
            Dropper firstDropper = null;
            int spawnCount = Mathf.Max(1, _dropperCountPerSpawn);
            // 按“每次生成数量”逐架创建运输船；每一架都独立计算起飞点与投送点偏移。
            for (int i = 0; i < spawnCount; i++)
            {
                var dropper = SpawnSingleDropper();
                if (firstDropper == null) firstDropper = dropper;
            }

            return firstDropper;
        }

        /// <summary>
        /// 启动“多波次生成流程”。
        /// 如果上一次流程还没结束，会先中断旧流程再启动新流程。
        /// </summary>
        public void StartSpawnProcess()
        {
            if (!CanSpawnDropper()) return;
            InterruptSpawnProcessIfRunning();
            _spawnProcessCoroutine = StartCoroutine(SpawnProcessCoroutine());
        }

        #region ----- Inspector 菜单 -------------------------

        [ContextMenu("生成运输船")]
        private void SpawnDropshipFromMenu()
        {
            if (!Application.isPlaying)
            {
                DebugUtility.LogWarning("[EnemySpawnerManager] 请先进入 Play 模式再通过菜单生成运输船。");
                return;
            }

            StartSpawnProcess();
        }

        [ContextMenu("生成一次巡逻队")]
        private void SpawnPatrolDropFromMenu()
        {
            if (!Application.isPlaying)
            {
                DebugUtility.LogWarning("[EnemySpawnerManager] 请先进入 Play 模式再通过菜单生成巡逻队。");
                return;
            }

            SpawnPatrolDropBatch();
        }

        #endregion

        #endregion

        #region ----- 运输船相关 -------------------------

        #region ----- 生成流程控制 -------------------------

        private bool CanSpawnDropper()
        {
            if (_dropperPrefab == null)
            {
                DebugUtility.LogError("[EnemySpawnerManager] Dropper prefab is not assigned.");
                return false;
            }

            return IsRouteValid();
        }

        private void InterruptSpawnProcessIfRunning()
        {
            if (_spawnProcessCoroutine == null) return;
            StopCoroutine(_spawnProcessCoroutine);
            _spawnProcessCoroutine = null;
            DebugUtility.LogWarning("[EnemySpawnerManager] Previous spawn process interrupted by a new request.");
        }

        private void StartPatrolDropProcessIfNeeded()
        {
            if (_patrolDropIntervalSeconds <= 0f) return;
            if (_patrolDropProcessCoroutine != null) StopCoroutine(_patrolDropProcessCoroutine);
            _patrolDropProcessCoroutine = StartCoroutine(PatrolDropProcessCoroutine());
        }

        private IEnumerator SpawnProcessCoroutine()
        {
            _lastSpawnPoints.Clear();
            _lastDropPoints.Clear();

            int waveCount = Mathf.Max(1, _waveCountPerSpawnProcess);
            int shipCountPerWave = Mathf.Max(1, _dropperCountPerSpawn);

            // 按配置波次执行生成；每一波内逐艘生成，并在两艘之间插入随机间隔。
            for (int waveIndex = 0; waveIndex < waveCount; waveIndex++)
            {
                if (!CanSpawnDropper()) break;

                // 当前波逐艘生成：允许每艘船拥有独立起点偏移与落点偏移。
                for (int shipIndex = 0; shipIndex < shipCountPerWave; shipIndex++)
                {
                    SpawnSingleDropper();
                    if (shipIndex >= shipCountPerWave - 1) continue;

                    float interval = Random.Range(0f, _shipSpawnIntervalMaxSeconds);
                    if (interval > 0f) yield return new WaitForSeconds(interval);
                }

                // 当前波全部生成后，等待波次间隔再进入下一波（最后一波不等待）。
                if (waveIndex >= waveCount - 1) continue;
                if (_waveIntervalSeconds > 0f) yield return new WaitForSeconds(_waveIntervalSeconds);
            }

            _spawnProcessCoroutine = null;
        }

        #endregion

        private bool IsRouteValid()
        {
            if (_startPoint != null && _dropPoint != null && _endPoint != null) return true;
            DebugUtility.LogError("[EnemySpawnerManager] Route points are not fully assigned.");
            return false;
        }

        private Dropper SpawnSingleDropper()
        {
            var startPos = GetRandomPointAround(_startPoint.position, _spawnPointRandomRadius);
            var dropPos = GetRandomWalkableDropPoint();
            return SpawnSingleDropper(startPos, dropPos, null, null, null);
        }

        private Dropper SpawnSingleDropper(
            Vector3 startPos,
            Vector3 dropPos,
            Vector3? patrolStartPoint,
            Vector3? patrolEndPoint,
            float? patrolMoveSpeed
        )
        {
            var cargoTemplate = GetRandomCargoTemplate();

            var dropper = Instantiate(
                _dropperPrefab,
                startPos,
                Quaternion.identity,
                _spawnRoot
            );

            dropper.SetupRoute(startPos, dropPos, _endPoint.position);
            InitDropperCargos(dropper, cargoTemplate, patrolStartPoint, patrolEndPoint, patrolMoveSpeed);
            dropper.LockCargos();

            _lastSpawnPoints.Add(startPos);
            _lastDropPoints.Add(dropPos);
            return dropper;
        }

        /// <summary>
        /// 将原 Dropper.InitAirEnemy 的实例化职责迁移到管理器。
        /// </summary>
        private void InitDropperCargos(
            Dropper dropper,
            CargoEnemyTemplate cargoTemplate,
            Vector3? patrolStartPoint,
            Vector3? patrolEndPoint,
            float? patrolMoveSpeed
        )
        {
            if (dropper == null) return;
            if (cargoTemplate == null || cargoTemplate.EnemyTypes == null || cargoTemplate.EnemyTypes.Count == 0)
            {
                DebugUtility.LogWarning("[EnemySpawnerManager] No valid cargo template found, this dropper will spawn without cargos.");
                return;
            }

            int slotCount = dropper.GetCargoSlotCount();
            int configCount = cargoTemplate.EnemyTypes.Count;
            int spawnCount = Mathf.Min(Mathf.Min(slotCount, MaxDropperCargoCount), configCount);

            if (configCount > MaxDropperCargoCount)
            {
                DebugUtility.LogWarning(
                    $"[EnemySpawnerManager] Cargo config count({configCount}) is greater than {MaxDropperCargoCount}, extra entries are ignored."
                );
            }

            // 逐个货舱位生成并绑定敌人，数量受“货舱位上限/配置数量/系统上限”共同约束。
            for (int i = 0; i < spawnCount; i++)
            {
                var enemy = CreateCargoEnemy(cargoTemplate.EnemyTypes[i]);
                if (enemy == null) continue;
                if (patrolStartPoint.HasValue && patrolEndPoint.HasValue)
                {
                    enemy.SetPatrolRoute(patrolStartPoint.Value, patrolEndPoint.Value);
                }

                if (patrolMoveSpeed.HasValue)
                {
                    enemy.SetPatrolMoveSpeed(patrolMoveSpeed.Value);
                }
                dropper.BindCargoEnemy(i, enemy);
            }
        }

        /// <summary>
        /// 创建敌人实例
        /// </summary>
        /// <param name="enemyType">敌人类型</param>
        /// <returns>返回敌人实例</returns>
        private AbstractEnemy CreateCargoEnemy(EnemyTypeEnum enemyType)
        {
            var prefab = ResourceLoad.Load<GameObject>("Prefab/Enemy/" + enemyType);
            if (prefab == null)
            {
                DebugUtility.LogError($"[EnemySpawnerManager] Can not load enemy prefab: {enemyType}");
                return null;
            }

            var obj = Instantiate(prefab);
            var enemy = obj.GetComponent<AbstractEnemy>();
            if (enemy == null)
            {
                DebugUtility.LogError($"[EnemySpawnerManager] Enemy prefab missing AbstractEnemy: {enemyType}");
                Destroy(obj);
                return null;
            }

            enemy.InitEnemy();
            enemy.transform.rotation = Quaternion.identity;
            return enemy;
        }

        /// <summary>
        /// 获取随机模板
        /// </summary>
        /// <returns>返回随机模板</returns>
        private CargoEnemyTemplate GetRandomCargoTemplate()
        {
            if (_cargoTemplates == null || _cargoTemplates.Count == 0) return null;

            int startIndex = Random.Range(0, _cargoTemplates.Count);
            
            // 从随机起点开始遍历模板池，跳过空模板，保证尽量挑到可用模板。
            for (int i = 0; i < _cargoTemplates.Count; i++)
            {
                int index = (startIndex + i) % _cargoTemplates.Count;
                var template = _cargoTemplates[index];
                if (template == null || template.EnemyTypes == null || template.EnemyTypes.Count == 0) continue;
                return template;
            }

            return null;
        }

        #region ----- 随机点与可行走采样 -------------------------

        private Vector3 GetRandomWalkableDropPoint()
        {
            // 如果目标半径为0，则直接返回目标点
            if (_dropPointRandomRadius <= 0f)
            {
                return GetClosestWalkablePointOrDefault(_dropPoint.position, _dropPoint.position);
            }

            var fallback = _dropPoint.position;  // 回退点
            int attempts = Mathf.Max(1, _walkablePointSampleAttempts);  // 采样尝试次数

            // 在目标半径内多次随机采样，只要命中可行走点就立即返回；否则走回退逻辑。
            for (int i = 0; i < attempts; i++)
            {
                var candidate = GetRandomPointAround(_dropPoint.position, _dropPointRandomRadius);
                if (TryGetWalkablePoint(candidate, out var walkablePoint))
                {
                    return walkablePoint;
                }
            }

            return GetClosestWalkablePointOrDefault(fallback, fallback);
        }

        private IEnumerator PatrolDropProcessCoroutine()
        {
            float interval = Mathf.Max(0f, _patrolDropIntervalSeconds);
            if (interval > 0f) yield return new WaitForSeconds(interval);

            while (true)
            {
                SpawnPatrolDropBatch();
                if (interval > 0f) yield return new WaitForSeconds(interval);
                else yield return null;
            }
        }

        private void SpawnPatrolDropBatch()
        {
            if (!CanSpawnDropper()) return;

            int spawnCount = Mathf.Max(1, _patrolDropShipCountPerInterval);
            _lastPatrolPathPoints.Clear();
            for (int i = 0; i < spawnCount; i++)
            {
                float shipPatrolMoveSpeed = Random.Range(0.5f, 1f);
                SpawnSingleDropperAtPointOutsidePlayerRadius(shipPatrolMoveSpeed);
            }
        }

        private void SpawnSingleDropperAtPointOutsidePlayerRadius(float shipPatrolMoveSpeed)
        {
            if (_player == null)
            {
                DebugUtility.LogWarning("[EnemySpawnerManager] Patrol drop player is not assigned, skip this dropper.");
                return;
            }

            var playerSnapshotPos = _player.position;
            var patrolEndPoint = playerSnapshotPos;
            if (AstarPath.active != null && !TryGetWalkablePoint(playerSnapshotPos, out patrolEndPoint))
            {
                DebugUtility.LogWarning("[EnemySpawnerManager] Player snapshot can not snap to a walkable point, skip this dropper.");
                return;
            }

            var startPos = GetRandomPointAround(_startPoint.position, _spawnPointRandomRadius);
            if (!TryGetRandomWalkableDropPointOutsidePlayerRadius(patrolEndPoint, out var dropPos))
            {
                DebugUtility.LogWarning("[EnemySpawnerManager] Can not find a valid patrol drop point outside player radius, skip this dropper.");
                return;
            }

            CachePatrolPath(dropPos, patrolEndPoint);
            SpawnSingleDropper(startPos, dropPos, dropPos, patrolEndPoint, shipPatrolMoveSpeed);
        }

        private void CachePatrolPath(Vector3 startPoint, Vector3 endPoint)
        {
            var pathPoints = new List<Vector3> { startPoint, endPoint };
            _lastPatrolPathPoints.Add(pathPoints);
        }

        private bool TryGetRandomWalkableDropPointOutsidePlayerRadius(Vector3 patrolEndPoint, out Vector3 dropPos)
        {
            dropPos = default;
            var player = GetPlayerTransform();
            if (player == null) return false;
            if (_patrolDropMinDistanceFromPlayer <= 0f)
            {
                dropPos = GetRandomWalkableDropPoint();
                return true;
            }

            int attempts = Mathf.Max(1, _walkablePointSampleAttempts * 3);
            float minDistance = _patrolDropMinDistanceFromPlayer;
            float maxDistance = minDistance + 2f * attempts;
            var distanceBucket = GetRandomPatrolDistanceBucket();

            for (int i = 0; i < attempts; i++)
            {
                // 方案C：先固定本船的长度档位，再在该档位内采样，保证不同船路径长度差异更明显。
                float distance = GetDistanceInBucket(distanceBucket, minDistance, maxDistance);
                Vector2 dir = Random.insideUnitCircle.normalized;
                if (dir.sqrMagnitude <= 0.0001f) dir = Vector2.right;
                var candidate = new Vector3(
                    player.position.x + dir.x * distance,
                    player.position.y + dir.y * distance,
                    _dropPoint != null ? _dropPoint.position.z : player.position.z
                );

                if (!TryGetWalkablePoint(candidate, out var walkablePoint)) continue;
                if (Vector2.Distance(walkablePoint, player.position) < minDistance) continue;
                if (!IsConnectedToPatrolEndPoint(walkablePoint, patrolEndPoint)) continue;
                dropPos = walkablePoint;
                return true;
            }

            return false;
        }

        private PatrolDistanceBucket GetRandomPatrolDistanceBucket()
        {
            float roll = Random.value;
            if (roll < 0.33f) return PatrolDistanceBucket.Short;
            if (roll < 0.66f) return PatrolDistanceBucket.Medium;
            return PatrolDistanceBucket.Long;
        }

        private float GetDistanceInBucket(PatrolDistanceBucket bucket, float minDistance, float maxDistance)
        {
            float span = Mathf.Max(0.01f, maxDistance - minDistance);
            float startT;
            float endT;

            switch (bucket)
            {
                case PatrolDistanceBucket.Short:
                    startT = 0.05f;
                    endT = 0.25f;
                    break;
                case PatrolDistanceBucket.Medium:
                    startT = 0.45f;
                    endT = 0.65f;
                    break;
                default:
                    startT = 0.80f;
                    endT = 1.00f;
                    break;
            }

            return minDistance + span * Random.Range(startT, endT);
        }

        private bool IsConnectedToPatrolEndPoint(Vector3 startPoint, Vector3 endPoint)
        {
            if (AstarPath.active == null) return true;

            var startNearest = AstarPath.active.GetNearest(startPoint, NearestNodeConstraint.Walkable);
            var endNearest = AstarPath.active.GetNearest(endPoint, NearestNodeConstraint.Walkable);
            if (startNearest.node == null || endNearest.node == null) return false;
            if (!startNearest.node.Walkable || !endNearest.node.Walkable) return false;
            return startNearest.node.Area == endNearest.node.Area;
        }

        private Transform GetPlayerTransform()
        {
            return _player;
        }

        /// <summary>
        /// 获取随机点，如果半径为0则返回中心点
        /// </summary>
        /// <param name="center">中心点</param>
        /// <param name="radius">半径</param>
        /// <returns>返回随机点</returns>
        private Vector3 GetRandomPointAround(Vector3 center, float radius)
        {
            if (radius <= 0f) return center;
            var offset = Random.insideUnitCircle * radius;
            return new Vector3(center.x + offset.x, center.y + offset.y, center.z);
        }

        /// <summary>
        /// 尝试获取最近的可行走点，如果获取失败则返回回退点
        /// </summary>
        /// <param name="candidate">候选点</param>
        /// <param name="fallback">回退点</param>
        /// <returns>返回最近的可行走点或回退点</returns>
        private Vector3 GetClosestWalkablePointOrDefault(Vector3 candidate, Vector3 fallback)
        {
            return TryGetWalkablePoint(candidate, out var walkablePoint) ? walkablePoint : fallback;
        }

        /// <summary>
        /// 尝试获取最近的可行走点，如果获取失败则返回回退点
        /// </summary>
        /// <param name="candidate">候选点</param>
        /// <param name="walkablePoint">可行走点</param>
        /// <returns>返回是否获取成功</returns>
        private bool TryGetWalkablePoint(Vector3 candidate, out Vector3 walkablePoint)
        {
            // 如果AstarPath为空，则返回候选点
            walkablePoint = candidate;
            if (AstarPath.active == null) return true;

            // 获取最近的可行走点
            var nearest = AstarPath.active.GetNearest(candidate, NearestNodeConstraint.Walkable);
            if (nearest.node == null || !nearest.node.Walkable) return false;

            // 获取最近的可行走点位置
            walkablePoint = (Vector3)nearest.position;
            walkablePoint.z = candidate.z;

            // 如果最大吸附距离为0，则返回最近的可行走点
            if (_walkableSnapMaxDistance <= 0f) return true;
            return Vector2.Distance(candidate, walkablePoint) <= _walkableSnapMaxDistance;
        }

        #endregion

        #region ----- Gizmos 可视化 -------------------------

        private void OnDrawGizmosSelected()
        {
            if (!_drawSpawnGizmos) return;

            // 绘制基础随机半径，便于调试起飞点与目标落点的采样范围。
            if (_startPoint != null && _spawnPointRandomRadius > 0f)
            {
                Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
                Gizmos.DrawWireSphere(_startPoint.position, _spawnPointRandomRadius);
            }

            if (_dropPoint != null && _dropPointRandomRadius > 0f)
            {
                Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.9f);
                Gizmos.DrawWireSphere(_dropPoint.position, _dropPointRandomRadius);
            }

            // 绘制“玩家半径外投送”限制圈，便于调试巡逻敌人的最小投送距离。
            if (_player != null && _patrolDropMinDistanceFromPlayer > 0f)
            {
                Gizmos.color = new Color(0.35f, 1f, 0.35f, 0.9f);
                Gizmos.DrawWireSphere(_player.position, _patrolDropMinDistanceFromPlayer);
            }

            // 绘制最近一次生成流程中每艘船的起点与落点。
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _lastSpawnPoints.Count; i++)
            {
                Gizmos.DrawSphere(_lastSpawnPoints[i], _gizmoPointRadius);
            }

            Gizmos.color = Color.red;
            for (int i = 0; i < _lastDropPoints.Count; i++)
            {
                Gizmos.DrawSphere(_lastDropPoints[i], _gizmoPointRadius);
            }

            // 绘制最近一次巡逻投送使用的“投送点 -> player快照点”路径。
            if (_lastPatrolPathPoints.Count > 0)
            {
                Gizmos.color = new Color(1f, 0.15f, 0.9f, 0.95f);
                for (int pathIndex = 0; pathIndex < _lastPatrolPathPoints.Count; pathIndex++)
                {
                    var pathPoints = _lastPatrolPathPoints[pathIndex];
                    if (pathPoints == null || pathPoints.Count < 2) continue;

                    for (int i = 0; i < pathPoints.Count - 1; i++)
                    {
                        Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
                    }
                }
            }
        }

        #endregion

        #endregion
    }

    [global::System.Serializable]
    public class CargoEnemyTemplate
    {
        [Tooltip("模板名称（仅用于编辑器识别）")]
        public string Name;
        [Tooltip("该模板下的挂载敌人顺序（最多6个）")]
        public List<EnemyTypeEnum> EnemyTypes = new List<EnemyTypeEnum>();
    }
}
