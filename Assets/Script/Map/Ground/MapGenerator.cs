using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using QFramework.UtilityKit;
using QFramework.Model;
using QFramework.Utility;
using QFramework;




#if UNITY_EDITOR
using UnityEditor;
#endif

public partial class MapGenerator : OverrideMonoSingleton<MapGenerator>
{
    private IResourceLoad _resourceLoad => this.GetUtility<IResourceLoad>();
    [Header("参数存档")]
    [Tooltip("用于保存当前生成参数的 ScriptableObject 资源")]
    public MapGeneratorParametersSO parameterAsset;

    [Header("地图生成设置")]
    [SerializeField] private MapGeneratorSettings settings = new MapGeneratorSettings();

    private CellData[,] _grid;
    private List<ObstaclePlacement> _gizmoObstacles = new List<ObstaclePlacement>();
    private bool _isApplyingParameterAsset;
    private MapGeneratorParametersSO _appliedParameterAsset;

    private int mapSize => settings.mapSize;
    private NoiseSettings noise => settings.noise;
    private FalloffSettings falloff => settings.falloff;
    private TerrainSettings terrain => settings.terrain;
    private ObstacleSettings obstacle => settings.obstacle;
    private GizmoSettings gizmo => settings.gizmo;

    private void EnsureSettingsObjects()
    {
        settings ??= new MapGeneratorSettings();
        settings.noise ??= new NoiseSettings();
        settings.falloff ??= new FalloffSettings();
        settings.terrain ??= new TerrainSettings();
        settings.obstacle ??= new ObstacleSettings();
        settings.gizmo ??= new GizmoSettings();
    }
    
    public void GenerateMapByLoadAsset(TerrainType terrainType)
    {
        // 从文件加载数据
        parameterAsset = _resourceLoad.Load<MapGeneratorParametersSO>("SOData/MapConfig/" + terrainType.ToString());

        if (parameterAsset == null)
        {
            parameterAsset = _resourceLoad.Load<MapGeneratorParametersSO>("SOData/MapConfig/" + TerrainType.Plain);
            DebugUtility.LogWarning("MapGenerator: 未找到参数资产，使用默认参数。");
        }

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

        Transform parent = obstacle.obstacleParent != null ? obstacle.obstacleParent : transform;
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
#if UNITY_EDITOR
            DestroyImmediate(child);
#else
            Destroy(child);
#endif
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
        parameterAsset.obstacleThreshold = obstacle.obstacleThreshold;
        parameterAsset.obstacleColor = obstacle.obstacleColor;
        parameterAsset.Pf_obstacle1x1 = obstacle.pfObstacle1x1;
        parameterAsset.Pf_obstacle2x2 = obstacle.pfObstacle2x2;
        parameterAsset.Pf_obstacle3x3 = obstacle.pfObstacle3x3;

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
        obstacle.obstacleThreshold = source.obstacleThreshold;
        obstacle.obstacleColor = source.obstacleColor;
        obstacle.pfObstacle1x1 = source.Pf_obstacle1x1;
        obstacle.pfObstacle2x2 = source.Pf_obstacle2x2;
        obstacle.pfObstacle3x3 = source.Pf_obstacle3x3;

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

        _isApplyingParameterAsset = false;
        _appliedParameterAsset = source;
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

            cfg.tilemap.color = cfg.tint;
            TileSet ts = TileResolveService.ResolveTileSet(cfg, terrain.defaultTileSet);

            var tilemapRenderer = cfg.tilemap.GetComponent<TilemapRenderer>();
            if (tilemapRenderer != null) tilemapRenderer.sortingOrder = -l;

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    int cellLayer = _grid[x, y].layerIndex;
                    if (cellLayer < 0 || cellLayer > l) continue;

                    TileBase tile;
                    if (cellLayer < l || cellLayer == terrain.layers.Length - 1)
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
        List<ObstaclePlacement> placements = ObstaclePlacementService.CalculatePlacements(
            _grid,
            mapSize,
            obstacle.obstacleThreshold,
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

        GameObject obj = Instantiate(prefab, parent);
        obj.transform.position = pos;
        obj.transform.localRotation = Quaternion.Euler(randomRotation);
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
        TryApplyPrefabColor(obstacle.pfObstacle1x1);
        TryApplyPrefabColor(obstacle.pfObstacle2x2);
        TryApplyPrefabColor(obstacle.pfObstacle3x3);
    }

    private void TryApplyPrefabColor(GameObject prefab)
    {
        if (prefab == null) return;
        var renderer = prefab.GetComponent<MeshRenderer>();
        if (renderer?.sharedMaterial == null) return;
        renderer.sharedMaterial.SetColor("_MainColor", obstacle.obstacleColor);
    }
}
