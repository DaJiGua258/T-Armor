using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using QFramework.Enum;
using QFramework.UtilityKit;
using QFramework.Model;
using QFramework.Utility;
using QFramework;
using QFramework.System;
using QFramework.ViewController.Mission;

#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class MapGenerator : OverrideMonoSingleton<MapGenerator>
{
    private const string ObstacleParentName = "ObstacleRoot";
    private const string MissionParentName = "MissionRoot";
    private const string EnvironmentParentName = "EnvironmentRoot";
    private const string POIRootName = "POIRoot";

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
    private POISettings poiSettings => settings.poi;
    private GizmoSettings gizmo => settings.gizmo;

    private void EnsureSettingsObjects()
    {
        settings ??= new MapGeneratorSettings();
        settings.noise ??= new NoiseSettings();
        settings.falloff ??= new FalloffSettings();
        settings.terrain ??= new TerrainSettings();
        settings.obstacle ??= new ObstacleSettings();
        settings.mission ??= new MissionPlacementSettings();
        settings.poi ??= new POISettings();
        settings.environment ??= new EnvironmentSettings();
        settings.gizmo ??= new GizmoSettings();
    }

    public void GenerateMapByLoadAsset(LevelDataModel levelData)
    {
        parameterAsset = _resourceLoad.Load<MapGeneratorParametersSO>("SOData/MapConfig/" + levelData.EnvironmentData.terrainType.ToString());

        if (parameterAsset == null)
        {
            parameterAsset = _resourceLoad.Load<MapGeneratorParametersSO>("SOData/MapConfig/" + TerrainType.Highlands);
        }

        parameterAsset.seed = levelData.seed.Value;
        ApplyParametersFromAsset(parameterAsset);
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
        SpawnPOIInstances();
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

        Transform poiParent = poiSettings.poiParent != null ? poiSettings.poiParent : transform;
        if (poiParent != obstacleParent && poiParent != missionParent && poiParent != environmentParent)
        {
            ClearChildren(poiParent);
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
        parameterAsset.minDistanceBetweenMissions = mission.minDistanceBetweenMissions;
        parameterAsset.distanceStep = mission.distanceStep;
        parameterAsset.poiCount = poiSettings.poiCount;
        parameterAsset.poiExtraMarginCells = poiSettings.extraMarginCells;
        parameterAsset.poiAvoidMissionRadius = poiSettings.avoidMissionRadius;
        parameterAsset.poiMinDistance = poiSettings.minPOIDistance;
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
        mission.minDistanceBetweenMissions = source.minDistanceBetweenMissions;
        mission.distanceStep = source.distanceStep;
        poiSettings.poiParent = ResolveParentByNameOrDefault(POIRootName);
        poiSettings.poiCount = source.poiCount;
        poiSettings.extraMarginCells = source.poiExtraMarginCells;
        poiSettings.avoidMissionRadius = source.poiAvoidMissionRadius;
        poiSettings.minPOIDistance = source.poiMinDistance;
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
        MapTilePainter.Paint(_grid, terrain.layers, mapSize, terrain.defaultTileSet);
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
            return;
        }

        Transform parent = mission.missionObject != null ? mission.missionObject : transform;
        List<Vector3> placedMissionPositions = new List<Vector3>();
        for (int i = 0; i < missions.Count; i++)
        {
            MissionDataModel missionData = missions[i];
            if (missionData == null || missionData.MissionConfig == null)
            {
                continue;
            }

            // Debug 场景：检测 MissionRoot 下是否已有 Entry
            if (missionData.MissionType == MissionTypeEnum.Entry)
            {
                var existingEntry = parent.GetComponentInChildren<EntryMissionInstance>();
                if (existingEntry != null)
                {
                    _missionSystem.EntrySpawnPosition = existingEntry.transform.position;
                    placedMissionPositions.Add(existingEntry.transform.position);
                    continue;
                }
            }

            string prefabPath = missionData.MissionConfig.PrefabPath;
            if (string.IsNullOrWhiteSpace(prefabPath))
            {
                continue;
            }

            GameObject prefab = _resourceLoad.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                continue;
            }

            GameObject missionObject = Instantiate(prefab, parent);
            var missionInstance = missionObject.GetComponent<AbstractMissionInstance>();
            if (missionInstance == null)
            {
                DestroySpawnedObject(missionObject);
                continue;
            }

            missionInstance.Init(missionData);
            Collider2D missionCollider = missionObject.GetComponent<Collider2D>();
            Vector2 areaSize = missionCollider != null ? missionCollider.bounds.size : Vector2.one;
            Vector2Int footprint = MissionPlacementService.ComputeFootprint(
                areaSize,
                tilemap.cellSize.x,
                tilemap.cellSize.y,
                mission.extraMarginCells);

            if (!MissionPlacementService.TryFindPlacement(
                    _grid, mapSize, footprint, mission.preferLowestNoise,
                    placedMissionPositions, mission.minDistanceBetweenMissions, mission.distanceStep, tilemap, out Vector2Int origin))
            {
                DestroySpawnedObject(missionObject);
                continue;
            }

            Vector3 placedPos = CellRectCenter(origin.x, origin.y, footprint.x, footprint.y);
            missionObject.transform.position = placedPos;
            MissionPlacementService.MarkOccupied(_grid, origin.x, origin.y, footprint.x, footprint.y);
            placedMissionPositions.Add(placedPos);

            // 记录 Entry 位置，用于玩家出生
            if (missionData.MissionType == MissionTypeEnum.Entry)
            {
                _missionSystem.EntrySpawnPosition = placedPos;
            }
        }
    }

    private void SpawnPOIInstances()
    {
        if (_grid == null)
            return;

        GameObject[] allPoiPrefabs = _resourceLoad.LoadAll<GameObject>("Prefab/POI");
        if (allPoiPrefabs == null || allPoiPrefabs.Length == 0)
            return;

        Tilemap tilemap = GetReferenceTilemap();
        if (tilemap == null)
            return;

        Transform parent = poiSettings.poiParent != null ? poiSettings.poiParent : transform;
        List<Vector3> missionPositions = MissionPlacementService.CollectPositions(mission.missionObject);
        List<Vector3> poiPositions = new List<Vector3>();
        System.Random rng = new System.Random();

        for (int i = 0; i < poiSettings.poiCount; i++)
        {
            GameObject prefab = allPoiPrefabs[rng.Next(allPoiPrefabs.Length)];
            GameObject poiObject = Instantiate(prefab, parent);

            Collider2D collider = poiObject.GetComponent<Collider2D>();
            if (collider == null)
            {
                DestroySpawnedObject(poiObject);
                continue;
            }

            Vector2 areaSize = collider.bounds.size;
            Vector2Int footprint = MissionPlacementService.ComputeFootprint(
                areaSize,
                tilemap.cellSize.x,
                tilemap.cellSize.y,
                poiSettings.extraMarginCells);

            if (!POIPlacementService.TryFindBestPlacement(
                    _grid, mapSize, footprint,
                    missionPositions, poiPositions,
                    poiSettings.avoidMissionRadius, poiSettings.minPOIDistance,
                    tilemap,
                    out Vector2Int origin))
            {
                DestroySpawnedObject(poiObject);
                continue;
            }

            poiObject.transform.position = CellRectCenter(origin.x, origin.y, footprint.x, footprint.y);
            poiPositions.Add(poiObject.transform.position);
            MissionPlacementService.MarkOccupied(_grid, origin.x, origin.y, footprint.x, footprint.y);
        }
    }

    private void SpawnEnvironmentObjects()
    {
        if (_grid == null || !environment.enabled) return;
        if (environment.rules == null || environment.rules.Length == 0) return;

        int seed = noise.seed + environment.seedOffset;
        List<Vector2> candidates = EnvironmentSpawnService.GeneratePoissonPoints(
            mapSize,
            environment.poissonRadius,
            environment.maxSamplesPerPoint,
            environment.edgePaddingCells,
            seed);
        if (candidates.Count == 0) return;

        Transform parent = environment.environmentParent != null ? environment.environmentParent : transform;
        List<Vector3> missionPositions = MissionPlacementService.CollectPositions(mission.missionObject);
        var rng = new System.Random(seed);

        foreach (Vector2 candidate in candidates)
        {
            if ((float)rng.NextDouble() > environment.spawnChance) continue;

            int cx = Mathf.Clamp(Mathf.FloorToInt(candidate.x), 0, mapSize - 1);
            int cy = Mathf.Clamp(Mathf.FloorToInt(candidate.y), 0, mapSize - 1);
            CellData cell = _grid[cx, cy];
            if (cell.occupied) continue;
            if (cell.noise < environment.validNoiseMin || cell.noise > environment.validNoiseMax) continue;
            if (!EnvironmentSpawnService.IsFarEnoughFromOccupied(candidate, _grid, mapSize, environment.avoidObstaclePadding)) continue;

            EnvironmentPrefabRule rule = EnvironmentSpawnService.PickRule(environment.rules, cell.noise, rng);
            if (rule == null || rule.prefab == null) continue;
            if ((float)rng.NextDouble() > rule.spawnChance) continue;

            Vector3 worldPosition = GetJitteredEnvironmentWorldPosition(cx, cy, rng);
            if (environment.avoidMissionRadius > 0f && MissionPlacementService.IsNearPosition(worldPosition, missionPositions, environment.avoidMissionRadius))
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
