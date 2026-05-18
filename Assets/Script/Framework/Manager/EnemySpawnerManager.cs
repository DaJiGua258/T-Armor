using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using QFramework.Enum;
using QFramework.UtilityKit;
using QFramework.ViewController.Enemy;
using QFramework.ViewController.Enemy.Formation;
using QFramework;
using QFramework.Event;
using UnityEngine;

namespace QFramework.Manager
{
    /// <summary>
    /// 敌人生成管理器（场景级单例）。
    /// 当前阶段负责运输船生成与挂载敌人实例化。
    /// </summary>
    public class EnemySpawnerManager : SceneMonoSingleton<EnemySpawnerManager>
    {
        private const int MaxCargoCnt = 6;
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
        [SerializeField] private bool _isSpawnOnStart = true;
        [SerializeField, Min(1)] private int _waveCntPerSpawn = 1;
        [SerializeField, Min(0f)] private float _waveInterval = 0f;
        [SerializeField, Min(1)] private int _dropperCnt = 1;
        [SerializeField, Min(0f)] private float _spawnIntervalMax = 0f;

        [Header("运输船路径点")]
        [SerializeField] private Transform _startPoint;
        [SerializeField] private Transform _dropPoint;
        [SerializeField] private Transform _endPoint;
        [SerializeField, Min(0f)] private float _spawnRadius = 0f;
        [SerializeField, Min(0f)] private float _dropRadius = 0f;
        [SerializeField, Min(1)] private int _walkableSampleCnt = 10;
        [SerializeField, Min(0f)] private float _walkableSnapDist = 2f;

        [Header("巡逻队配置")]
        [SerializeField] private Transform _player;
        [SerializeField, Min(1)] private int _patrolSpawnCnt = 1;  // 每次生成数量
        [SerializeField, Min(0f)] private float _patrolSpawnInterval = 0f;
        [SerializeField, Min(0f)] private float _patrolSpawnMinDist = 0f;
        [SerializeField] private List<FormationTypesSO> _patrolFormationTemplates;

        [Header("挂载敌人模板池（每个模板最多6个）")]
        [SerializeField] private List<CargoEnemyTemplate> _cargoTemplates;

        [Header("Gizmos调试")]
        [SerializeField] private bool _canDrawGizmos = true;
        [SerializeField, Min(0.05f)] private float _gizmoPointRadius = 0.25f;

        private Coroutine _spawnCoroutine;
        private Coroutine _patrolSpawnCoroutine;
        private Coroutine _timedWaveCoroutine;
        private Transform _timedWaveDropPoint;
        private readonly List<Vector3> _lastSpawnPoints = new List<Vector3>();
        private readonly List<Vector3> _lastDropPoints = new List<Vector3>();

        #endregion

        #region ----- 生命周期 -------------------------

        private void Start()
        {
            if (_isSpawnOnStart) StartSpawn();
            StartPatrolSpawn();
        }

        #endregion

        #region ----- 对外 API -------------------------

        /// <summary>
        /// 生成一批运输船并注入路径与挂载敌人，返回本次所有 dropper 列表。
        /// </summary>
        public List<Dropper> SpawnWave()
        {
            var droppers = new List<Dropper>();
            if (!CanSpawnDropper()) return droppers;
            int spawnCount = Mathf.Max(1, _dropperCnt);
            for (int i = 0; i < spawnCount; i++)
            {
                var dropper = SpawnDropper();
                if (dropper != null) droppers.Add(dropper);
            }
            return droppers;
        }

        /// <summary>
        /// 启动“多波次生成流程”。
        /// 如果上一次流程还没结束，会先中断旧流程再启动新流程。
        /// </summary>
        public void StartSpawn()
        {
            if (!CanSpawnDropper()) return;
            InterruptSpawn();
            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        /// <summary>
        /// 启动定时波次循环：内部自动刷一波→等全部离场→等 5s→判断时间，直到 duration 耗尽。
        /// dropPoint 不为 null 时，所有 dropper 以该点作为投送位置（固定落点）。
        /// </summary>
        public void StartTimedWaves(float duration, Transform dropPoint = null)
        {
            if (!CanSpawnDropper()) return;
            _timedWaveDropPoint = dropPoint;
            InterruptSpawn();
            _timedWaveCoroutine = StartCoroutine(TimedWaveLoop(duration));
        }

        /// <summary>
        /// 立即停止定时波次循环。
        /// </summary>
        public void StopTimedWaves()
        {
            _timedWaveDropPoint = null;
            if (_timedWaveCoroutine != null)
            {
                StopCoroutine(_timedWaveCoroutine);
                _timedWaveCoroutine = null;
            }
            InterruptSpawn();
        }

        #region ----- Inspector 菜单 -------------------------

        [ContextMenu("生成运输船")]
        private void SpawnFromMenu()
        {
            if (!Application.isPlaying)
            {
                // DebugUtility.LogWarning("[EnemySpawnerManager] 请先进入 Play 模式再通过菜单生成运输船。");
                return;
            }

            StartSpawn();
        }

        [ContextMenu("生成一次巡逻队")]
        private void SpawnPatrolFromMenu()
        {
            if (!Application.isPlaying)
            {
                // DebugUtility.LogWarning("[EnemySpawnerManager] 请先进入 Play 模式再通过菜单生成巡逻队。");
                return;
            }

            SpawnPatrolFormation();
        }

        #endregion

        #endregion

        #region ----- 运输船相关 -------------------------

        #region ----- 生成流程控制 -------------------------

        private bool CanSpawnDropper()
        {
            if (_dropperPrefab == null)
            {
                // DebugUtility.LogError("[EnemySpawnerManager] Dropper prefab is not assigned.");
                return false;
            }

            return IsRouteValid();
        }

        private void InterruptSpawn()
        {
            if (_spawnCoroutine == null) return;
            StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = null;
            TypeEventSystem.Global.Send(new WaveSpawnAlertEvent { Active = false });
        }

        private void StartPatrolSpawn()
        {
            if (_patrolSpawnInterval <= 0f) return;
            if (_patrolSpawnCoroutine != null) StopCoroutine(_patrolSpawnCoroutine);
            _patrolSpawnCoroutine = StartCoroutine(PatrolSpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            _lastSpawnPoints.Clear();
            _lastDropPoints.Clear();
            TypeEventSystem.Global.Send(new WaveSpawnAlertEvent { Active = true });

            int waveCount = Mathf.Max(1, _waveCntPerSpawn);
            int shipCountPerWave = Mathf.Max(1, _dropperCnt);

            // 按配置波次执行生成；每一波内逐艘生成，并在两艘之间插入随机间隔。
            for (int waveIndex = 0; waveIndex < waveCount; waveIndex++)
            {
                if (!CanSpawnDropper()) break;

                // 当前波逐艘生成：允许每艘船拥有独立起点偏移与落点偏移。
                for (int shipIndex = 0; shipIndex < shipCountPerWave; shipIndex++)
                {
                    SpawnDropper();
                    if (shipIndex >= shipCountPerWave - 1) continue;

                    float interval = Random.Range(0f, _spawnIntervalMax);
                    if (interval > 0f) yield return new WaitForSeconds(interval);
                }

                // 当前波全部生成后，等待波次间隔再进入下一波（最后一波不等待）。
                if (waveIndex >= waveCount - 1) continue;
                if (_waveInterval > 0f) yield return new WaitForSeconds(_waveInterval);
            }

            TypeEventSystem.Global.Send(new WaveSpawnAlertEvent { Active = false });
            _spawnCoroutine = null;
        }

        private IEnumerator TimedWaveLoop(float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                // ① 按面板参数刷一次完整波次
                StartSpawn();

                // ② 等候本次 SpawnRoutine 完成
                yield return new WaitUntil(() => _spawnCoroutine == null || elapsed >= duration);
                if (elapsed >= duration) break;

                // ③ 等 5s
                float waitUntil = Time.time + 5f;
                while (Time.time < waitUntil)
                {
                    elapsed += Time.deltaTime;
                    if (elapsed >= duration) break;
                    yield return null;
                }
            }

            _timedWaveCoroutine = null;
        }

        #endregion

        private bool IsRouteValid()
        {
            if (_startPoint != null && _dropPoint != null && _endPoint != null) return true;
            // DebugUtility.LogError("[EnemySpawnerManager] Route points are not fully assigned.");
            return false;
        }

        private Dropper SpawnDropper()
        {
            var startPos = GetRandomPointAround(_startPoint.position, _spawnRadius);
            Vector3 dropPos;
            if (_timedWaveDropPoint != null)
                dropPos = GetWalkableDropPos(_timedWaveDropPoint.position);
            else if (_player != null)
                dropPos = GetWalkableDropPos(_player.position);
            else
                dropPos = GetWalkableDropPos();
            return SpawnDropper(startPos, dropPos, null, null, null);
        }

        private Dropper SpawnDropper(
            Vector3 startPos,
            Vector3 dropPos,
            Vector3? patrolStartPoint,
            Vector3? patrolEndPoint,
            float? patrolMoveSpeed
        )
        {
            var cargoTemplate = GetRandomTemplate();

            var dropper = Instantiate(
                _dropperPrefab,
                startPos,
                Quaternion.identity,
                _spawnRoot
            );

            dropper.SetupRoute(startPos, dropPos, _endPoint.position);
            InitCargos(dropper, cargoTemplate, patrolStartPoint, patrolEndPoint, patrolMoveSpeed);
            dropper.LockCargos();

            _lastSpawnPoints.Add(startPos);
            _lastDropPoints.Add(dropPos);
            return dropper;
        }

        /// <summary>
        /// 将原 Dropper.InitAirEnemy 的实例化职责迁移到管理器。
        /// </summary>
        private void InitCargos(
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
                // DebugUtility.LogWarning("[EnemySpawnerManager] No valid cargo template found, this dropper will spawn without cargos.");
                return;
            }

            int slotCount = dropper.GetCargoSlotCount();
            int configCount = cargoTemplate.EnemyTypes.Count;
            int spawnCount = Mathf.Min(Mathf.Min(slotCount, MaxCargoCnt), configCount);

            if (configCount > MaxCargoCnt)
            {
                // DebugUtility.LogWarning(
                //     $"[EnemySpawnerManager] Cargo config count({configCount}) is greater than {MaxCargoCnt}, extra entries are ignored."
                // );
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
                // DebugUtility.LogError($"[EnemySpawnerManager] Can not load enemy prefab: {enemyType}");
                return null;
            }

            var obj = Instantiate(prefab);
            var enemy = obj.GetComponent<AbstractEnemy>();
            if (enemy == null)
            {
                // DebugUtility.LogError($"[EnemySpawnerManager] Enemy prefab missing AbstractEnemy: {enemyType}");
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
        private CargoEnemyTemplate GetRandomTemplate()
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

        private Vector3 GetWalkableDropPos()
        {
            return GetWalkableDropPos(_dropPoint.position);
        }

        private Vector3 GetWalkableDropPos(Vector3 center)
        {
            if (_dropRadius <= 0f)
                return GetWalkableOrDefault(center, center);

            var fallback = center;
            int attempts = Mathf.Max(1, _walkableSampleCnt);

            for (int i = 0; i < attempts; i++)
            {
                var candidate = GetRandomPointAround(center, _dropRadius);
                if (TryGetWalkablePoint(candidate, out var walkablePoint))
                    return walkablePoint;
            }

            return GetWalkableOrDefault(fallback, fallback);
        }

        private IEnumerator PatrolSpawnRoutine()
        {
            float interval = Mathf.Max(0f, _patrolSpawnInterval);
            if (interval > 0f) yield return new WaitForSeconds(interval);

            while (true)
            {
                int cnt = Mathf.Max(1, _patrolSpawnCnt);
                for (int i = 0; i < cnt; i++)
                    SpawnPatrolFormation();
                if (interval > 0f) yield return new WaitForSeconds(interval);
                else yield return null;
            }
        }

        private void SpawnPatrolFormation()
        {
            if (_player == null) return;

            var template = GetRandomFormationTemplate();
            if (template == null) return;

            var playerSnapshotPos = _player.position;
            var patrolEndPoint = playerSnapshotPos;
            if (AstarPath.active != null && !TryGetWalkablePoint(playerSnapshotPos, out patrolEndPoint))
                return;

            if (!TryGetPatrolDropPos(patrolEndPoint, out var dropPos)) return;

            float patrolMoveSpeed = Random.Range(0.5f, 1f);

            var go = new GameObject("Formation_" + template.name);
            go.transform.position = dropPos;
            go.transform.SetParent(_spawnRoot);
            var controller = go.AddComponent<FormationController>();
            controller.PatrolSpeed = patrolMoveSpeed;
            controller.RingRadius = 3f;
            controller.RingSpacing = 2f;
            controller.SpawnOnStart = false;
            controller.SetPatrolRoute(dropPos, patrolEndPoint);
            controller.SpawnFromSO(template);
        }

        private FormationTypesSO GetRandomFormationTemplate()
        {
            if (_patrolFormationTemplates == null || _patrolFormationTemplates.Count == 0) return null;
            return _patrolFormationTemplates[Random.Range(0, _patrolFormationTemplates.Count)];
        }

        private bool TryGetPatrolDropPos(Vector3 patrolEndPoint, out Vector3 dropPos)
        {
            dropPos = default;
            var player = GetPlayerTransform();
            if (player == null) return false;
            if (_patrolSpawnMinDist <= 0f)
            {
                dropPos = GetWalkableDropPos();
                return true;
            }

            int attempts = Mathf.Max(1, _walkableSampleCnt * 3);
            float minDistance = _patrolSpawnMinDist;
            float maxDistance = minDistance + 2f * attempts;
            var distanceBucket = GetRandomDistBucket();

            for (int i = 0; i < attempts; i++)
            {
                // 方案C：先固定本船的长度档位，再在该档位内采样，保证不同船路径长度差异更明显。
                float distance = GetBucketDist(distanceBucket, minDistance, maxDistance);
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

        private PatrolDistanceBucket GetRandomDistBucket()
        {
            float roll = Random.value;
            if (roll < 0.33f) return PatrolDistanceBucket.Short;
            if (roll < 0.66f) return PatrolDistanceBucket.Medium;
            return PatrolDistanceBucket.Long;
        }

        private float GetBucketDist(PatrolDistanceBucket bucket, float minDistance, float maxDistance)
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
        private Vector3 GetWalkableOrDefault(Vector3 candidate, Vector3 fallback)
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
            if (_walkableSnapDist <= 0f) return true;
            return Vector2.Distance(candidate, walkablePoint) <= _walkableSnapDist;
        }

        #endregion

        #region ----- Gizmos 可视化 -------------------------

        private void OnDrawGizmosSelected()
        {
            if (!_canDrawGizmos) return;

            // 绘制基础随机半径，便于调试起飞点与目标落点的采样范围。
            if (_startPoint != null && _spawnRadius > 0f)
            {
                Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
                Gizmos.DrawWireSphere(_startPoint.position, _spawnRadius);
            }

            if (_dropPoint != null && _dropRadius > 0f)
            {
                Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.9f);
                Gizmos.DrawWireSphere(_dropPoint.position, _dropRadius);
            }

            // 绘制“玩家半径外投送”限制圈，便于调试巡逻敌人的最小投送距离。
            if (_player != null && _patrolSpawnMinDist > 0f)
            {
                Gizmos.color = new Color(0.35f, 1f, 0.35f, 0.9f);
                Gizmos.DrawWireSphere(_player.position, _patrolSpawnMinDist);
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

            // 巡逻队由 FormationController 自身 Gizmos 绘制，此处不再重复绘制。
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
