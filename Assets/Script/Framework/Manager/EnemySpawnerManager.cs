using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using QFramework.Enum;
using QFramework.UtilityKit;
using QFramework.ViewController.Enemy;
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
        [SerializeField, Min(0f)] private float _dropRadiusMin = 0f;
        [SerializeField, Min(0f)] private float _dropRadius = 0f;
        [SerializeField, Min(1)] private int _walkableSampleCnt = 10;
        [SerializeField, Min(0f)] private float _walkableSnapDist = 2f;

        [Header("目标配置")]
        [SerializeField] private Transform _player;

        [Header("挂载敌人模板池（每个模板最多6个）")]
        [SerializeField] private List<CargoEnemyTemplate> _cargoTemplates;

        [Header("Tick生成系统")]
        [SerializeField] private EnemySpawnConfigSO _spawnConfigSO;
        [SerializeField] private bool _useTickSystemOnStart = true;
        [SerializeField, Range(0f, 1f)] private float _progressT = 0f;
        [SerializeField, Min(0f)] private float _spawnMinDist = 20f;
        [SerializeField, Min(0f)] private float _spawnMaxDist = 40f;
        [SerializeField, Min(1)] private int _spawnPosSampleCnt = 10;

        [Header("Gizmos调试")]
        [SerializeField] private bool _canDrawGizmos = true;
        [SerializeField, Min(0.05f)] private float _gizmoPointRadius = 0.25f;

        private Coroutine _spawnCoroutine;
        private Coroutine _timedWaveCoroutine;
        private Coroutine _tickCoroutine;
        private Transform _timedWaveDropPoint;
        private readonly List<Vector3> _lastSpawnPoints = new List<Vector3>();
        private readonly List<Vector3> _lastDropPoints = new List<Vector3>();

        #endregion

        #region ----- 对象池 -------------------------

        private readonly Dictionary<EnemyTypeEnum, Queue<AbstractEnemy>> _enemyPool
            = new Dictionary<EnemyTypeEnum, Queue<AbstractEnemy>>();

        /// <summary>
        /// 从池中取出一个敌人，返回 null 表示池空。
        /// </summary>
        private AbstractEnemy GetFromPool(EnemyTypeEnum enemyType)
        {
            if (_enemyPool.TryGetValue(enemyType, out var queue) && queue.Count > 0)
            {
                var enemy = queue.Dequeue();
                if (enemy != null)
                {
                    enemy.gameObject.SetActive(true);
                    return enemy;
                }
            }
            return null;
        }

        /// <summary>
        /// 将敌人归还到池中，禁用对象并排队等待复用。
        /// </summary>
        private void ReturnEnemyToPool(AbstractEnemy enemy)
        {
            if (enemy == null) return;
            enemy.gameObject.SetActive(false);
            enemy.transform.SetParent(_spawnRoot);

            var type = enemy.enemyType;
            if (!_enemyPool.ContainsKey(type))
                _enemyPool[type] = new Queue<AbstractEnemy>();
            _enemyPool[type].Enqueue(enemy);
        }

        #endregion

        #region ----- 生命周期 -------------------------

        private void Start()
        {
            TypeEventSystem.Global.Register<PlayerEvent.InitCompleted>(OnPlayerInitCompleted)
                .UnRegisterWhenGameObjectDestroyed(this);

            if (_isSpawnOnStart) StartSpawn();
            if (_useTickSystemOnStart) StartTickSpawning();
        }

        private void OnPlayerInitCompleted(PlayerEvent.InitCompleted e)
        {
            _player = e.PlayerTransform;
            _dropPoint = e.PlayerTransform;
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

            var cargoTemplate = GetRandomTemplate();

            var dropper = Instantiate(_dropperPrefab, startPos, Quaternion.identity, _spawnRoot);
            dropper.SetupRoute(startPos, dropPos, _endPoint.position);
            InitCargos(dropper, cargoTemplate);
            dropper.LockCargos();

            // 投放的敌人以玩家为目标
            if (_player != null)
                dropper.OnCargoDropped += enemy => enemy.GetTarget(_player);

            _lastSpawnPoints.Add(startPos);
            _lastDropPoints.Add(dropPos);
            return dropper;
        }

        private void InitCargos(Dropper dropper, CargoEnemyTemplate cargoTemplate)
        {
            if (dropper == null) return;
            if (cargoTemplate == null || cargoTemplate.EnemyTypes == null || cargoTemplate.EnemyTypes.Count == 0)
                return;

            int slotCount = dropper.GetCargoSlotCount();
            int configCount = cargoTemplate.EnemyTypes.Count;
            int spawnCount = Mathf.Min(Mathf.Min(slotCount, MaxCargoCnt), configCount);

            for (int i = 0; i < spawnCount; i++)
            {
                var enemy = CreateCargoEnemy(cargoTemplate.EnemyTypes[i]);
                if (enemy == null) continue;
                dropper.BindCargoEnemy(i, enemy);
            }
        }

        /// <summary>
        /// 创建敌人实例（优先从对象池获取，池空则 Instantiate）。
        /// </summary>
        private AbstractEnemy CreateCargoEnemy(EnemyTypeEnum enemyType)
        {
            // 优先从池中取
            var pooled = GetFromPool(enemyType);
            if (pooled != null)
            {
                pooled.ResetEnemy();
                pooled.transform.rotation = Quaternion.identity;
                return pooled;
            }

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

            // 挂载回收回调，死亡时自动归还池
            enemy.OnRecycle = ReturnEnemyToPool;

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
                var candidate = GetRandomPointAround(center, _dropRadius, _dropRadiusMin);
                if (TryGetWalkablePoint(candidate, out var walkablePoint))
                    return walkablePoint;
            }

            return GetWalkableOrDefault(fallback, fallback);
        }

        #endregion

        #region ----- Tick 生成系统 -------------------------

        public void StartTickSpawning()
        {
            if (_spawnConfigSO == null || _spawnConfigSO.Weights.Count == 0) return;
            InterruptTickSpawning();
            _progressT = 0f;
            _tickCoroutine = StartCoroutine(TickSpawnRoutine());
        }

        public void StopTickSpawning()
        {
            InterruptTickSpawning();
        }

        private void InterruptTickSpawning()
        {
            if (_tickCoroutine == null) return;
            StopCoroutine(_tickCoroutine);
            _tickCoroutine = null;
        }

        private IEnumerator TickSpawnRoutine()
        {
            var config = _spawnConfigSO;
            float tickDuration = config.TickDuration;

            while (true)
            {
                _progressT = Mathf.Clamp01(_progressT + tickDuration / config.MaxTime);
                float curveValue = config.ProgressionCurve.Evaluate(_progressT);

                int spawnCount = Mathf.RoundToInt(Mathf.Lerp(
                    config.InitialEnemyCount, config.MaxEnemyCount, curveValue));

                if (spawnCount <= 0)
                {
                    yield return new WaitForSeconds(tickDuration);
                    continue;
                }

                // 计算当前插值后的权重
                var entries = config.Weights;
                var weightedTypes = new List<EnemyTypeEnum>();
                var weightedValues = new List<float>();
                float totalWeight = 0f;

                for (int i = 0; i < entries.Count; i++)
                {
                    var w = entries[i];
                    if (w == null || w.EnemyType == EnemyTypeEnum.None) continue;
                    float weight = Mathf.Lerp(w.InitialWeight, w.MaxWeight, curveValue);
                    if (weight <= 0f) continue;
                    weightedTypes.Add(w.EnemyType);
                    weightedValues.Add(weight);
                    totalWeight += weight;
                }

                if (weightedTypes.Count == 0)
                {
                    yield return new WaitForSeconds(tickDuration);
                    continue;
                }

                float interval = spawnCount > 1
                    ? tickDuration / spawnCount
                    : tickDuration;

                for (int i = 0; i < spawnCount; i++)
                {
                    var enemyType = PickWeightedType(weightedTypes, weightedValues, totalWeight);
                    SpawnEnemyDirect(enemyType);

                    if (i < spawnCount - 1)
                        yield return new WaitForSeconds(interval);
                }
            }
        }

        private static EnemyTypeEnum PickWeightedType(
            List<EnemyTypeEnum> types, List<float> weights, float totalWeight)
        {
            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < types.Count; i++)
            {
                cumulative += weights[i];
                if (roll <= cumulative) return types[i];
            }
            return types[types.Count - 1];
        }

        private void SpawnEnemyDirect(EnemyTypeEnum enemyType)
        {
            if (_player == null) return;

            Vector3 spawnPos = GetSpawnPosAroundPlayer();
            var enemy = CreateCargoEnemy(enemyType);
            if (enemy == null) return;

            enemy.transform.position = spawnPos;
            enemy.transform.SetParent(_spawnRoot);
            enemy.GetTarget(_player);
        }

        private Vector3 GetSpawnPosAroundPlayer()
        {
            for (int i = 0; i < _spawnPosSampleCnt; i++)
            {
                float distance = Random.Range(_spawnMinDist, _spawnMaxDist);
                Vector2 dir = Random.insideUnitCircle.normalized;
                if (dir.sqrMagnitude <= 0.0001f) dir = Vector2.right;

                var candidate = new Vector3(
                    _player.position.x + dir.x * distance,
                    _player.position.y + dir.y * distance,
                    _player.position.z);

                if (TryGetAnyWalkablePoint(candidate, out var walkablePoint))
                    return walkablePoint;
            }

            // 兜底：直接找玩家附近最近的可行走点
            if (AstarPath.active != null)
            {
                var nearest = AstarPath.active.GetNearest(_player.position, NearestNodeConstraint.Walkable);
                if (nearest.node != null && nearest.node.Walkable)
                    return (Vector3)nearest.position;
            }

            return _player.position + Vector3.right * _spawnMinDist;
        }

        /// <summary>
        /// 尝试获取候选点附近最近的可行走点。
        /// 吸附距离不超过 _spawnMaxDist 的 20%，且必须与玩家在同一个 Astar 连通区域。
        /// </summary>
        private bool TryGetAnyWalkablePoint(Vector3 candidate, out Vector3 walkablePoint)
        {
            walkablePoint = candidate;
            if (AstarPath.active == null) return true;

            var nearest = AstarPath.active.GetNearest(candidate, NearestNodeConstraint.Walkable);
            if (nearest.node == null || !nearest.node.Walkable) return false;

            float maxSnap = Mathf.Max(3f, _spawnMaxDist * 0.2f);
            float dist = Vector2.Distance(candidate, (Vector3)nearest.position);
            if (dist > maxSnap) return false;

            // 与玩家在同一连通区域，避免生成在墙对侧
            var playerNearest = AstarPath.active.GetNearest(_player.position, NearestNodeConstraint.Walkable);
            if (playerNearest.node == null || !playerNearest.node.Walkable) return false;
            if (nearest.node.Area != playerNearest.node.Area) return false;

            walkablePoint = (Vector3)nearest.position;
            walkablePoint.z = candidate.z;
            return true;
        }

        #endregion

        #region ----- 随机点与可行走采样 -------------------------
        private Vector3 GetRandomPointAround(Vector3 center, float radius, float minRadius = 0f)
        {
            if (radius <= 0f) return center;
            float r = minRadius > 0f ? Random.Range(minRadius, radius) : radius;
            var offset = Random.insideUnitCircle.normalized * r;
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

            // 绘制 Tick 生成系统范围圈
            if (_player != null && _spawnMinDist > 0f && _spawnMaxDist > _spawnMinDist)
            {
                Gizmos.color = new Color(0.35f, 1f, 0.35f, 0.6f);
                Gizmos.DrawWireSphere(_player.position, _spawnMinDist);
                Gizmos.DrawWireSphere(_player.position, _spawnMaxDist);
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
