using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using QFramework.UtilityKit;
using QFramework.Model;
using QFramework.Utility;
using QFramework;
using QFramework.System;
using QFramework.ViewController.Mission;
using System;





#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class MapGenerator : OverrideMonoSingleton<MapGenerator>
{
    private const string ObstacleParentName = "ObstacleRoot";
    private const string MissionParentName = "MissionRoot";
    private const string EnvironmentParentName = "EnvironmentRoot";

    private IResourceLoad _resourceLoad => this.GetUtility<IResourceLoad>();
    private IMissionSystem _missionSystem => this.GetSystem<IMissionSystem>();
    [Header("参数存档")]
    [Tooltip("用于保存当前生成参数的 ScriptableObject 资源")]
    public MapGeneratorParametersSO parameterAsset;

    [Header("地图生成设置")]
    [SerializeField] private MapGeneratorSettings settings = new MapGeneratorSettings();

    private CellData[,] _grid;
    private List<ObstaclePlacement> _gizmoObstacles = new List<ObstaclePlacement>();
    private bool _isApplyingParameterAsset;
    private MapGeneratorParametersSO _appliedParameterAsset;
    private readonly Dictionary<GameObject, GameObject> _runtimeSpawnTemplateCache = new Dictionary<GameObject, GameObject>();

    private int mapSize => settings.mapSize;
    private NoiseSettings noise => settings.noise;
    private FalloffSettings falloff => settings.falloff;
    private TerrainSettings terrain => settings.terrain;
    private ObstacleSettings obstacle => settings.obstacle;
    private MissionPlacementSettings mission => settings.mission;
    private EnvironmentSettings environment => settings.environment;
    private GizmoSettings gizmo => settings.gizmo;

    private void EnsureSettingsObjects()
    {
        settings ??= new MapGeneratorSettings();
        settings.noise ??= new NoiseSettings();
        settings.falloff ??= new FalloffSettings();
        settings.terrain ??= new TerrainSettings();
        settings.obstacle ??= new ObstacleSettings();
        settings.mission ??= new MissionPlacementSettings();
        settings.environment ??= new EnvironmentSettings();
        settings.gizmo ??= new GizmoSettings();
    }
    
    public void GenerateMapByLoadAsset(LevelDataModel levelData)
    {
        // 从文件加载数据
        parameterAsset = _resourceLoad.Load<MapGeneratorParametersSO>("SOData/MapConfig/" + levelData.EnvironmentData.terrainType.ToString());
        
        if (parameterAsset == null)
        {
            parameterAsset = _resourceLoad.Load<MapGeneratorParametersSO>("SOData/MapConfig/" + TerrainType.Highlands);
            // DebugUtility.LogWarning("MapGenerator: 未找到参数资产，使用默认参数。");
        }

        // 根据关卡信息实时修改的参数
        parameterAsset.seed = levelData.seed.Value;

        // 应用数据
        ApplyParametersFromAsset(parameterAsset);

        // 生成地图
        Generate();
    }

    [ContextMenu("生成地图")]
    public void Generate()
    {
        EnsureSettingsObjects();
        ClearMap();
        InitPrefab();
        BuildGrid();
        PaintTilemaps();
        SpawnObstacles();
        SpawnMissionInstances();
        SpawnEnvironmentObjects();
    }

    [ContextMenu("清空地图")]
    public void ClearMap()
    {
        EnsureSettingsObjects();
        if (terrain.layers != null)
        {
            foreach (LayerConfig cfg in terrain.layers)
                cfg.tilemap?.ClearAllTiles();
        }

        Transform obstacleParent = obstacle.obstacleParent != null ? obstacle.obstacleParent : transform;
        ClearChildren(obstacleParent);

        Transform missionParent = mission.missionObject != null ? mission.missionObject : transform;
        if (missionParent != obstacleParent)
        {
            ClearChildren(missionParent);
        }
        
        Transform environmentParent = environment.environmentParent != null ? environment.environmentParent : transform;
        if (environmentParent != obstacleParent && environmentParent != missionParent)
        {
            ClearChildren(environmentParent);
        }

        _grid = null;
        _gizmoObstacles = new List<ObstaclePlacement>();
    }

    [ContextMenu("保存参数到配置")]
    public void SaveParametersToAsset()
    {
        if (parameterAsset == null)
        {
            Debug.LogWarning("MapGenerator: 未指定 parameterAsset，无法保存参数。", this);
            return;
        }

        EnsureSettingsObjects();
        parameterAsset.mapSize = settings.mapSize;
        parameterAsset.noiseScale = noise.noiseScale;
        parameterAsset.octaves = noise.octaves;
        parameterAsset.persistence = noise.persistence;
        parameterAsset.lacunarity = noise.lacunarity;
        parameterAsset.seed = noise.seed;
        parameterAsset.noiseOffset = noise.noiseOffset;
        parameterAsset.useFalloff = falloff.useFalloff;
        parameterAsset.curve = falloff.curve != null ? new AnimationCurve(falloff.curve.keys) : AnimationCurve.Linear(0f, 0f, 1f, 1f);
        parameterAsset.defaultTileSet = terrain.defaultTileSet;
        parameterAsset.enableShapePostProcess = terrain.enableShapePostProcess;
        parameterAsset.postProcessIterations = terrain.postProcessIterations;
        parameterAsset.obstacleColor = obstacle.obstacleColor;
        parameterAsset.Pf_obstacle1x1 = obstacle.pfObstacle1x1;
        parameterAsset.Pf_obstacle2x2 = obstacle.pfObstacle2x2;
        parameterAsset.Pf_obstacle3x3 = obstacle.pfObstacle3x3;
        parameterAsset.preferLowestNoise = mission.preferLowestNoise;
        parameterAsset.extraMarginCells = mission.extraMarginCells;
        parameterAsset.environmentEnabled = environment.enabled;
        parameterAsset.poissonRadius = environment.poissonRadius;
        parameterAsset.maxSamplesPerPoint = environment.maxSamplesPerPoint;
        parameterAsset.environmentSpawnChance = environment.spawnChance;
        parameterAsset.validNoiseMin = environment.validNoiseMin;
        parameterAsset.validNoiseMax = environment.validNoiseMax;
        parameterAsset.edgePaddingCells = environment.edgePaddingCells;
        parameterAsset.avoidObstaclePadding = environment.avoidObstaclePadding;
        parameterAsset.avoidMissionRadius = environment.avoidMissionRadius;
        parameterAsset.cellJitterRatio = environment.cellJitterRatio;
        parameterAsset.environmentSeedOffset = environment.seedOffset;

        if (terrain.layers == null)
        {
            parameterAsset.layers = null;
        }
        else
        {
            parameterAsset.layers = new MapGeneratorParametersSO.LayerParameter[terrain.layers.Length];
            for (int i = 0; i < terrain.layers.Length; i++)
            {
                LayerConfig src = terrain.layers[i];
                parameterAsset.layers[i] = new MapGeneratorParametersSO.LayerParameter
                {
                    threshold = src.threshold,
                    tint = src.tint,
                    tileSet = src.tileSet
                };
            }
        }

        if (environment.rules == null)
        {
            parameterAsset.environmentRules = null;
        }
        else
        {
            parameterAsset.environmentRules = new MapGeneratorParametersSO.EnvironmentRuleParameter[environment.rules.Length];
            for (int i = 0; i < environment.rules.Length; i++)
            {
                EnvironmentPrefabRule src = environment.rules[i];
                parameterAsset.environmentRules[i] = src == null
                    ? new MapGeneratorParametersSO.EnvironmentRuleParameter()
                    : new MapGeneratorParametersSO.EnvironmentRuleParameter
                    {
                        prefab = src.prefab,
                        weight = src.weight,
                        spawnChance = src.spawnChance,
                        noiseMin = src.noiseMin,
                        noiseMax = src.noiseMax
                    };
            }
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(parameterAsset);
        AssetDatabase.SaveAssets();
#endif
        Debug.Log($"MapGenerator: 参数已保存到 {parameterAsset.name}", this);
    }

    [ContextMenu("从配置加载参数")]
    public void LoadParametersFromAsset()
    {
        if (parameterAsset == null)
        {
            Debug.LogWarning("MapGenerator: 未指定 parameterAsset，无法加载参数。", this);
            return;
        }

        ApplyParametersFromAsset(parameterAsset);
        Debug.Log($"MapGenerator: 已从 {parameterAsset.name} 加载参数。", this);
    }

    public void ApplyParametersFromAsset(MapGeneratorParametersSO source)
    {
        if (source == null) return;
        EnsureSettingsObjects();

        _isApplyingParameterAsset = true;
        settings.mapSize = source.mapSize;
        noise.noiseScale = source.noiseScale;
        noise.octaves = source.octaves;
        noise.persistence = source.persistence;
        noise.lacunarity = source.lacunarity;
        noise.seed = source.seed;
        noise.noiseOffset = source.noiseOffset;
        falloff.useFalloff = source.useFalloff;
        falloff.curve = source.curve != null ? new AnimationCurve(source.curve.keys) : AnimationCurve.Linear(0f, 0f, 1f, 1f);
        terrain.defaultTileSet = source.defaultTileSet;
        terrain.enableShapePostProcess = source.enableShapePostProcess;
        terrain.postProcessIterations = source.postProcessIterations;
        obstacle.obstacleColor = source.obstacleColor;
        obstacle.pfObstacle1x1 = source.Pf_obstacle1x1;
        obstacle.pfObstacle2x2 = source.Pf_obstacle2x2;
        obstacle.pfObstacle3x3 = source.Pf_obstacle3x3;
        obstacle.obstacleParent = ResolveParentByNameOrDefault(ObstacleParentName);
        mission.missionObject = ResolveParentByNameOrDefault(MissionParentName);
        mission.preferLowestNoise = source.preferLowestNoise;
        mission.extraMarginCells = source.extraMarginCells;
        environment.enabled = source.environmentEnabled;
        environment.environmentParent = ResolveParentByNameOrDefault(EnvironmentParentName);
        environment.poissonRadius = source.poissonRadius;
        environment.maxSamplesPerPoint = source.maxSamplesPerPoint;
        environment.spawnChance = source.environmentSpawnChance;
        environment.validNoiseMin = source.validNoiseMin;
        environment.validNoiseMax = source.validNoiseMax;
        environment.edgePaddingCells = source.edgePaddingCells;
        environment.avoidObstaclePadding = source.avoidObstaclePadding;
        environment.avoidMissionRadius = source.avoidMissionRadius;
        environment.cellJitterRatio = source.cellJitterRatio;
        environment.seedOffset = source.environmentSeedOffset;

        LayerConfig[] oldLayers = terrain.layers;
        if (source.layers == null)
        {
            terrain.layers = null;
        }
        else
        {
            terrain.layers = new LayerConfig[source.layers.Length];
            for (int i = 0; i < source.layers.Length; i++)
            {
                MapGeneratorParametersSO.LayerParameter src = source.layers[i];
                terrain.layers[i] = new LayerConfig
                {
                    threshold = src.threshold,
                    tint = src.tint,
                    tileSet = src.tileSet,
                    tilemap = oldLayers != null && i < oldLayers.Length ? oldLayers[i].tilemap : null
                };
            }
        }

        if (source.environmentRules == null)
        {
            environment.rules = null;
        }
        else
        {
            environment.rules = new EnvironmentPrefabRule[source.environmentRules.Length];
            for (int i = 0; i < source.environmentRules.Length; i++)
            {
                MapGeneratorParametersSO.EnvironmentRuleParameter src = source.environmentRules[i];
                environment.rules[i] = src == null
                    ? new EnvironmentPrefabRule()
                    : new EnvironmentPrefabRule
                    {
                        prefab = src.prefab,
                        weight = src.weight,
                        spawnChance = src.spawnChance,
                        noiseMin = src.noiseMin,
                        noiseMax = src.noiseMax
                    };
            }
        }

        _isApplyingParameterAsset = false;
        _appliedParameterAsset = source;
    }

    private Transform ResolveParentByNameOrDefault(string parentName)
    {
        Transform directChild = transform.Find(parentName);
        if (directChild != null)
        {
            return directChild;
        }

        GameObject sceneObject = GameObject.Find(parentName);
        if (sceneObject != null)
        {
            return sceneObject.transform;
        }

        return transform;
    }

    private void BuildGrid()
    {
        if (terrain.layers == null || terrain.layers.Length == 0 || mapSize < 1)
        {
            _grid = null;
            return;
        }

        _grid = MapGridBuilder.Build(new MapGridBuildInput
        {
            mapSize = settings.mapSize,
            noise = noise,
            falloff = falloff,
            terrain = terrain
        });
    }

    private void PaintTilemaps()
    {
        if (_grid == null || terrain.layers == null) return;

        for (int l = 0; l < terrain.layers.Length; l++)
        {
            LayerConfig cfg = terrain.layers[l];
            if (cfg.tilemap == null) continue;
            bool isBottomLayer = l == terrain.layers.Length - 1;

            cfg.tilemap.color = cfg.tint;
            TileSet ts = TileResolveService.ResolveTileSet(cfg, terrain.defaultTileSet);

            var tilemapRenderer = cfg.tilemap.GetComponent<TilemapRenderer>();
            if (tilemapRenderer != null) tilemapRenderer.sortingOrder = -l;

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    if (isBottomLayer)
                    {
                        TileBase bottomTile = TileResolveService.PickFull(ts, x, y);
                        if (bottomTile != null) cfg.tilemap.SetTile(new Vector3Int(x, y, 0), bottomTile);
                        continue;
                    }

                    int cellLayer = _grid[x, y].layerIndex;
                    if (cellLayer < 0 || cellLayer > l) continue;

                    TileBase tile;
                    if (cellLayer < l)
                    {
                        tile = TileResolveService.PickFull(ts, x, y);
                    }
                    else
                    {
                        int ti = _grid[x, y].tileIndex;
                        tile = ti == CellData.TILE_FULL ? TileResolveService.PickFull(ts, x, y) : TileResolveService.Border(ts, ti);
                    }

                    if (tile != null) cfg.tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }
    }

    private void SpawnObstacles()
    {
        List<ObstaclePlacement> placements = ComputeObstaclePlacements();
        foreach (ObstaclePlacement placement in placements)
        {
            GameObject prefab = GetObstaclePrefab(placement.size);
            if (prefab != null) DoSpawn(prefab, placement.center);
        }
    }

    internal List<ObstaclePlacement> ComputeObstaclePlacements()
    {
        if (_grid == null) return new List<ObstaclePlacement>();
        float obstacleNoiseThreshold = GetObstacleNoiseThreshold();
        List<ObstaclePlacement> placements = ObstaclePlacementService.CalculatePlacements(
            _grid,
            mapSize,
            obstacleNoiseThreshold,
            new[] { 3, 2, 1 });

        for (int i = 0; i < placements.Count; i++)
        {
            ObstaclePlacement placement = placements[i];
            placement.center = CellCenter(placement.originX, placement.originY, placement.size);
            placements[i] = placement;
        }

        _gizmoObstacles = placements;
        return placements;
    }

    internal float GetObstacleNoiseThreshold()
    {
        if (terrain.layers == null || terrain.layers.Length == 0)
            return 1f;

        return terrain.layers[terrain.layers.Length - 1].threshold;
    }

    private GameObject GetObstaclePrefab(int size)
    {
        switch (size)
        {
            case 3: return obstacle.pfObstacle3x3;
            case 2: return obstacle.pfObstacle2x2;
            case 1: return obstacle.pfObstacle1x1;
            default: return null;
        }
    }

    private void DoSpawn(GameObject prefab, Vector3 pos)
    {
        Transform parent = obstacle.obstacleParent != null ? obstacle.obstacleParent : transform;
        int randomIndex = UnityEngine.Random.Range(0, 3);
        Vector3 randomRotation = new Vector3(0f, 0f, 90 * randomIndex);

        GameObject template = GetRuntimeSpawnTemplate(prefab);
        if (template == null) return;

        GameObject obj = Instantiate(template, parent);
        obj.transform.position = pos;
        obj.transform.localRotation = Quaternion.Euler(randomRotation);
        if (!obj.activeSelf) obj.SetActive(true);
    }

    private void SpawnMissionInstances()
    {
        if (_grid == null)
        {
            return;
        }

        List<MissionDataModel> missions = _missionSystem?.Missions;
        if (missions == null || missions.Count == 0)
        {
            return;
        }

        Tilemap tilemap = GetReferenceTilemap();
        if (tilemap == null)
        {
            // DebugUtility.LogWarning("MapGenerator: 缺少参考 Tilemap，无法生成任务点。");
            return;
        }

        Transform parent = mission.missionObject != null ? mission.missionObject : transform;
        for (int i = 0; i < missions.Count; i++)
        {
            MissionDataModel missionData = missions[i];
            if (missionData == null || missionData.MissionConfig == null)
            {
                continue;
            }

            string prefabPath = missionData.MissionConfig.PrefabPath;
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                // DebugUtility.LogWarning($"MapGenerator: 任务[{missionData.MissionType}] 预制体路径为空，跳过生成。");
                continue;
            }

            GameObject prefab = _resourceLoad.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                // DebugUtility.LogWarning($"MapGenerator: 未找到任务预制体 {prefabPath}，跳过生成。");
                continue;
            }

            GameObject missionObject = Instantiate(prefab, parent);
            var missionInstance = missionObject.GetComponent<AbstractMissionInstance>();
            if (missionInstance == null)
            {
                // DebugUtility.LogWarning($"MapGenerator: 预制体 {prefabPath} 缺少 AbstractMissionInstance，跳过生成。");
                DestroySpawnedObject(missionObject);
                continue;
            }

            missionInstance.Init(missionData);
            Vector2Int footprint = ComputeMissionFootprint(missionInstance.AreaSize, tilemap);
            if (!TryFindMissionPlacement(footprint, out Vector2Int origin))
            {
                // DebugUtility.LogWarning($"MapGenerator: 任务[{missionData.MissionType}] 没有可放置区域，跳过生成。");
                DestroySpawnedObject(missionObject);
                continue;
            }

            missionObject.transform.position = CellRectCenter(origin.x, origin.y, footprint.x, footprint.y);
            MarkMissionOccupied(origin.x, origin.y, footprint.x, footprint.y);
        }
    }

    private void SpawnEnvironmentObjects()
    {
        if (_grid == null || !environment.enabled) return;
        if (environment.rules == null || environment.rules.Length == 0) return;

        int seed = noise.seed + environment.seedOffset;
        List<Vector2> candidates = GenerateEnvironmentPoissonPoints(
            mapSize,
            environment.poissonRadius,
            environment.maxSamplesPerPoint,
            environment.edgePaddingCells,
            seed);
        if (candidates.Count == 0) return;

        Transform parent = environment.environmentParent != null ? environment.environmentParent : transform;
        List<Vector3> missionPositions = CollectMissionPositions();
        var rng = new System.Random(seed);

        foreach (Vector2 candidate in candidates)
        {
            if ((float)rng.NextDouble() > environment.spawnChance) continue;

            int cx = Mathf.Clamp(Mathf.FloorToInt(candidate.x), 0, mapSize - 1);
            int cy = Mathf.Clamp(Mathf.FloorToInt(candidate.y), 0, mapSize - 1);
            CellData cell = _grid[cx, cy];
            if (cell.occupied) continue;
            if (cell.noise < environment.validNoiseMin || cell.noise > environment.validNoiseMax) continue;
            if (!IsFarEnoughFromOccupiedCells(candidate, environment.avoidObstaclePadding)) continue;

            EnvironmentPrefabRule rule = PickEnvironmentRule(cell.noise, rng);
            if (rule == null || rule.prefab == null) continue;
            if ((float)rng.NextDouble() > rule.spawnChance) continue;

            Vector3 worldPosition = GetJitteredEnvironmentWorldPosition(cx, cy, rng);
            if (environment.avoidMissionRadius > 0f && IsNearMission(worldPosition, missionPositions, environment.avoidMissionRadius))
                continue;
            GameObject template = GetRuntimeSpawnTemplate(rule.prefab);
            if (template == null) continue;

            GameObject obj = Instantiate(template, parent);
            obj.transform.position = worldPosition;
            obj.transform.rotation = Quaternion.identity;
            if (!obj.activeSelf) obj.SetActive(true);
        }
        Physics.SyncTransforms();
    }

    private Vector2Int ComputeMissionFootprint(Vector2 areaSize, Tilemap tilemap)
    {
        float cellWidth = Mathf.Max(0.0001f, Mathf.Abs(tilemap.cellSize.x));
        float cellHeight = Mathf.Max(0.0001f, Mathf.Abs(tilemap.cellSize.y));
        int width = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(areaSize.x) / cellWidth));
        int height = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(areaSize.y) / cellHeight));
        int margin = Mathf.Max(0, mission.extraMarginCells);
        return new Vector2Int(width + margin * 2, height + margin * 2);
    }

    private bool TryFindMissionPlacement(Vector2Int footprint, out Vector2Int origin)
    {
        origin = new Vector2Int(-1, -1);
        int width = Mathf.Max(1, footprint.x);
        int height = Mathf.Max(1, footprint.y);

        bool found = false;
        float bestNoise = float.MaxValue;
        Vector2Int bestOrigin = origin;

        for (int y = 0; y <= mapSize - height; y++)
        {
            for (int x = 0; x <= mapSize - width; x++)
            {
                if (!CanPlaceMissionAt(x, y, width, height))
                {
                    continue;
                }

                if (!mission.preferLowestNoise)
                {
                    origin = new Vector2Int(x, y);
                    return true;
                }

                float avgNoise = GetAverageNoise(x, y, width, height);
                if (!found || avgNoise < bestNoise)
                {
                    found = true;
                    bestNoise = avgNoise;
                    bestOrigin = new Vector2Int(x, y);
                }
            }
        }

        if (!found)
        {
            return false;
        }

        origin = bestOrigin;
        return true;
    }

    private bool CanPlaceMissionAt(int originX, int originY, int width, int height)
    {
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                int x = originX + dx;
                int y = originY + dy;
                if (x < 0 || y < 0 || x >= mapSize || y >= mapSize)
                {
                    return false;
                }

                CellData cell = _grid[x, y];
                if (cell.layerIndex == CellData.LAYER_NONE)
                {
                    return false;
                }

                if (cell.occupied)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private float GetAverageNoise(int originX, int originY, int width, int height)
    {
        float total = 0f;
        int count = 0;
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                total += _grid[originX + dx, originY + dy].noise;
                count++;
            }
        }

        return count == 0 ? float.MaxValue : total / count;
    }

    private void MarkMissionOccupied(int originX, int originY, int width, int height)
    {
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                _grid[originX + dx, originY + dy].occupied = true;
            }
        }
    }

    private Vector3 CellRectCenter(int x, int y, int width, int height)
    {
        Tilemap tm = GetReferenceTilemap();
        if (tm != null)
        {
            Vector3 localCenter = tm.GetCellCenterLocal(new Vector3Int(x, y, 0));
            localCenter.x += (width - 1) * 0.5f * tm.cellSize.x;
            localCenter.y += (height - 1) * 0.5f * tm.cellSize.y;
            return tm.transform.TransformPoint(localCenter);
        }

        return new Vector3(x + width * 0.5f, y + height * 0.5f, 0f);
    }

    private Vector3 CellSpaceToWorld(Vector2 cellPos)
    {
        Tilemap tm = GetReferenceTilemap();
        if (tm != null)
        {
            int ix = Mathf.FloorToInt(cellPos.x);
            int iy = Mathf.FloorToInt(cellPos.y);
            float fx = cellPos.x - ix;
            float fy = cellPos.y - iy;

            Vector3 local = tm.GetCellCenterLocal(new Vector3Int(ix, iy, 0));
            local.x += fx * tm.cellSize.x;
            local.y += fy * tm.cellSize.y;
            return tm.transform.TransformPoint(local);
        }

        return new Vector3(cellPos.x + 0.5f, cellPos.y + 0.5f, 0f);
    }

    private Vector3 GetJitteredEnvironmentWorldPosition(int cellX, int cellY, System.Random rng)
    {
        Tilemap tm = GetReferenceTilemap();
        float jitterRatio = Mathf.Clamp(environment.cellJitterRatio, 0f, 0.49f);

        if (tm != null)
        {
            Vector3 localCenter = tm.GetCellCenterLocal(new Vector3Int(cellX, cellY, 0));
            float halfX = Mathf.Abs(tm.cellSize.x) * 0.5f * jitterRatio;
            float halfY = Mathf.Abs(tm.cellSize.y) * 0.5f * jitterRatio;
            localCenter.x += Mathf.Lerp(-halfX, halfX, (float)rng.NextDouble());
            localCenter.y += Mathf.Lerp(-halfY, halfY, (float)rng.NextDouble());
            return tm.transform.TransformPoint(localCenter);
        }

        float jitter = 0.5f * jitterRatio;
        return new Vector3(
            cellX + 0.5f + Mathf.Lerp(-jitter, jitter, (float)rng.NextDouble()),
            cellY + 0.5f + Mathf.Lerp(-jitter, jitter, (float)rng.NextDouble()),
            0f);
    }

    internal Vector3 CellCenter(int x, int y, int size)
    {
        Tilemap tm = GetReferenceTilemap();
        if (tm != null)
        {
            Vector3 localCenter = tm.GetCellCenterLocal(new Vector3Int(x, y, 0));
            localCenter.x += (size - 1) * 0.5f * tm.cellSize.x;
            localCenter.y += (size - 1) * 0.5f * tm.cellSize.y;
            return tm.transform.TransformPoint(localCenter);
        }

        return new Vector3(x + size * 0.5f, y + size * 0.5f, 0f);
    }

    internal Tilemap GetReferenceTilemap()
    {
        if (terrain.layers == null) return null;
        foreach (LayerConfig cfg in terrain.layers)
            if (cfg.tilemap != null) return cfg.tilemap;
        return null;
    }

    private void InitPrefab()
    {
        // TryApplyPrefabColor(obstacle.pfObstacle1x1);
        // TryApplyPrefabColor(obstacle.pfObstacle2x2);
        // TryApplyPrefabColor(obstacle.pfObstacle3x3);
    }

    private void TryApplyPrefabColor(GameObject prefab)
    {
        if (prefab == null) return;
        var renderer = prefab.GetComponent<MeshRenderer>();
        if (renderer?.sharedMaterial == null) return;
        renderer.sharedMaterial.SetColor("_MainColor", obstacle.obstacleColor);
    }

    private List<Vector3> CollectMissionPositions()
    {
        var positions = new List<Vector3>();
        Transform missionParent = mission.missionObject != null ? mission.missionObject : transform;
        if (missionParent == null) return positions;

        for (int i = 0; i < missionParent.childCount; i++)
        {
            Transform child = missionParent.GetChild(i);
            if (child.GetComponent<AbstractMissionInstance>() == null) continue;
            positions.Add(child.position);
        }

        return positions;
    }

    private bool IsNearMission(Vector3 worldPosition, List<Vector3> missionPositions, float radius)
    {
        if (missionPositions == null || missionPositions.Count == 0) return false;

        float radiusSqr = radius * radius;
        for (int i = 0; i < missionPositions.Count; i++)
        {
            Vector3 delta = missionPositions[i] - worldPosition;
            if (delta.sqrMagnitude <= radiusSqr) return true;
        }

        return false;
    }

    private bool IsFarEnoughFromOccupiedCells(Vector2 candidate, float paddingInCells)
    {
        int cx = Mathf.Clamp(Mathf.FloorToInt(candidate.x), 0, mapSize - 1);
        int cy = Mathf.Clamp(Mathf.FloorToInt(candidate.y), 0, mapSize - 1);
        if (paddingInCells <= 0f) return !_grid[cx, cy].occupied;

        int searchRadius = Mathf.CeilToInt(paddingInCells);
        for (int y = Mathf.Max(0, cy - searchRadius); y <= Mathf.Min(mapSize - 1, cy + searchRadius); y++)
        {
            for (int x = Mathf.Max(0, cx - searchRadius); x <= Mathf.Min(mapSize - 1, cx + searchRadius); x++)
            {
                if (!_grid[x, y].occupied) continue;
                Vector2 occupiedCenter = new Vector2(x + 0.5f, y + 0.5f);
                if ((occupiedCenter - candidate).sqrMagnitude <= paddingInCells * paddingInCells)
                    return false;
            }
        }

        return true;
    }

    private EnvironmentPrefabRule PickEnvironmentRule(float noiseValue, System.Random rng)
    {
        if (environment.rules == null || environment.rules.Length == 0) return null;

        float totalWeight = 0f;
        for (int i = 0; i < environment.rules.Length; i++)
        {
            EnvironmentPrefabRule rule = environment.rules[i];
            if (rule == null || rule.prefab == null) continue;
            if (noiseValue < rule.noiseMin || noiseValue > rule.noiseMax) continue;
            if (rule.weight <= 0f) continue;
            totalWeight += rule.weight;
        }

        if (totalWeight <= 0f) return null;

        float pick = (float)rng.NextDouble() * totalWeight;
        for (int i = 0; i < environment.rules.Length; i++)
        {
            EnvironmentPrefabRule rule = environment.rules[i];
            if (rule == null || rule.prefab == null) continue;
            if (noiseValue < rule.noiseMin || noiseValue > rule.noiseMax) continue;
            if (rule.weight <= 0f) continue;

            if (pick <= rule.weight) return rule;
            pick -= rule.weight;
        }

        return null;
    }

    private static List<Vector2> GenerateEnvironmentPoissonPoints(
        int mapSize,
        float radius,
        int maxSamplesPerPoint,
        int edgePaddingCells,
        int seed)
    {
        var points = new List<Vector2>();
        if (mapSize <= 0) return points;

        float minX = Mathf.Clamp(edgePaddingCells, 0, mapSize);
        float minY = minX;
        float maxX = Mathf.Clamp(mapSize - edgePaddingCells, 0, mapSize);
        float maxY = maxX;
        float width = maxX - minX;
        float height = maxY - minY;
        if (width <= 0f || height <= 0f) return points;

        float safeRadius = Mathf.Max(0.1f, radius);
        int safeMaxSamples = Mathf.Max(1, maxSamplesPerPoint);
        float cellSize = safeRadius / Mathf.Sqrt(2f);
        int gridWidth = Mathf.CeilToInt(width / cellSize);
        int gridHeight = Mathf.CeilToInt(height / cellSize);

        Vector2[,] grid = new Vector2[gridWidth, gridHeight];
        bool[,] hasPoint = new bool[gridWidth, gridHeight];
        var active = new List<Vector2>();
        var rng = new System.Random(seed);

        Vector2 first = new Vector2(
            minX + (float)rng.NextDouble() * width,
            minY + (float)rng.NextDouble() * height);

        points.Add(first);
        active.Add(first);
        InsertEnvironmentPoint(first, minX, minY, cellSize, grid, hasPoint);

        while (active.Count > 0)
        {
            int index = rng.Next(active.Count);
            Vector2 center = active[index];
            bool found = false;

            for (int i = 0; i < safeMaxSamples; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float distance = safeRadius * (1f + (float)rng.NextDouble());
                Vector2 candidate = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

                if (candidate.x < minX || candidate.x >= maxX || candidate.y < minY || candidate.y >= maxY)
                    continue;
                if (!IsValidEnvironmentCandidate(candidate, safeRadius, minX, minY, cellSize, grid, hasPoint))
                    continue;

                points.Add(candidate);
                active.Add(candidate);
                InsertEnvironmentPoint(candidate, minX, minY, cellSize, grid, hasPoint);
                found = true;
                break;
            }

            if (!found)
                active.RemoveAt(index);
        }

        return points;
    }

    private static void InsertEnvironmentPoint(
        Vector2 point,
        float minX,
        float minY,
        float cellSize,
        Vector2[,] grid,
        bool[,] hasPoint)
    {
        int gx = Mathf.Clamp((int)((point.x - minX) / cellSize), 0, grid.GetLength(0) - 1);
        int gy = Mathf.Clamp((int)((point.y - minY) / cellSize), 0, grid.GetLength(1) - 1);
        grid[gx, gy] = point;
        hasPoint[gx, gy] = true;
    }

    private static bool IsValidEnvironmentCandidate(
        Vector2 candidate,
        float radius,
        float minX,
        float minY,
        float cellSize,
        Vector2[,] grid,
        bool[,] hasPoint)
    {
        int gx = Mathf.Clamp((int)((candidate.x - minX) / cellSize), 0, grid.GetLength(0) - 1);
        int gy = Mathf.Clamp((int)((candidate.y - minY) / cellSize), 0, grid.GetLength(1) - 1);
        int range = 2;

        int xMin = Mathf.Max(0, gx - range);
        int xMax = Mathf.Min(grid.GetLength(0) - 1, gx + range);
        int yMin = Mathf.Max(0, gy - range);
        int yMax = Mathf.Min(grid.GetLength(1) - 1, gy + range);
        float radiusSqr = radius * radius;

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                if (!hasPoint[x, y]) continue;
                Vector2 delta = grid[x, y] - candidate;
                if (delta.sqrMagnitude < radiusSqr) return false;
            }
        }

        return true;
    }

    private static void DestroySpawnedObject(GameObject target)
    {
        if (target == null) return;
#if UNITY_EDITOR
        DestroyImmediate(target);
#else
        Destroy(target);
#endif
    }

    private static void ClearChildren(Transform parent)
    {
        if (parent == null) return;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
#if UNITY_EDITOR
            DestroyImmediate(child);
#else
            Destroy(child);
#endif
        }
    }

    private GameObject GetRuntimeSpawnTemplate(GameObject prefab)
    {
        if (prefab == null) return null;
        if (!Application.isPlaying) return prefab;

        GameObject template;
        if (_runtimeSpawnTemplateCache.TryGetValue(prefab, out template) && template != null)
            return template;

        template = Instantiate(prefab);
        template.name = $"{prefab.name}_RuntimeTemplate";
        if (template.activeSelf) template.SetActive(false);

        RemoveMeshRelatedComponentsRecursively(template.transform);
        _runtimeSpawnTemplateCache[prefab] = template;
        return template;
    }

    private static void RemoveMeshRelatedComponentsRecursively(Transform root)
    {
        if (root == null) return;

        var meshFilter = root.GetComponent<MeshFilter>();
        var meshRenderer = root.GetComponent<MeshRenderer>();

        if (meshFilter != null) Destroy(meshFilter);
        if (meshRenderer != null) Destroy(meshRenderer);

        for (int i = 0; i < root.childCount; i++)
        {
            RemoveMeshRelatedComponentsRecursively(root.GetChild(i));
        }
    }

    private void OnDestroy()
    {
        if (_runtimeSpawnTemplateCache.Count == 0) return;

        foreach (KeyValuePair<GameObject, GameObject> pair in _runtimeSpawnTemplateCache)
        {
            GameObject template = pair.Value;
            if (template == null) continue;
#if UNITY_EDITOR
            DestroyImmediate(template);
#else
            Destroy(template);
#endif
        }

        _runtimeSpawnTemplateCache.Clear();
    }
}
