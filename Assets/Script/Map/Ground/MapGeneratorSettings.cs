using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class MapGeneratorSettings
{
    [Header("地图尺寸")]
    [Min(8)]
    public int mapSize = 64;

    [Header("噪声参数")]
    public NoiseSettings noise = new NoiseSettings();

    [Header("边缘封闭（Falloff Map）")]
    public FalloffSettings falloff = new FalloffSettings();

    [Header("地形参数")]
    public TerrainSettings terrain = new TerrainSettings();

    [Header("障碍参数")]
    public ObstacleSettings obstacle = new ObstacleSettings();

    [Header("任务点参数")]
    public MissionPlacementSettings mission = new MissionPlacementSettings();

    [Header("兴趣点参数")]
    public POISettings poi = new POISettings();

    [Header("环境物体参数")]
    public EnvironmentSettings environment = new EnvironmentSettings();

    [Header("Gizmos")]
    public GizmoSettings gizmo = new GizmoSettings();
}

[Serializable]
public class NoiseSettings
{
    [Min(0.01f)] public float noiseScale = 30f;
    [Range(1, 8)] public int octaves = 4;
    [Range(0f, 1f)] public float persistence = 0.5f;
    [Min(1f)] public float lacunarity = 2f;
    public int seed = 0;
    public Vector2 noiseOffset;
}

[Serializable]
public class FalloffSettings
{
    public bool useFalloff = true;
    public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
}

[Serializable]
public class TerrainSettings
{
    [Tooltip("默认 Tile 集（所有层共享回退）")]
    public TileSet defaultTileSet;

    [Tooltip("地形层（threshold 从低到高排列）")]
    public LayerConfig[] layers;

    [Tooltip("开启后对无法用素材表达的格子形状进行降级处理")]
    public bool enableShapePostProcess = true;

    [Range(1, 10)]
    public int postProcessIterations = 4;
}

[Serializable]
public class ObstacleSettings
{
    public Color obstacleColor = new Color(0.5f, 0.86f, 1f, 1f);

    [FormerlySerializedAs("_pf_obstacle1x1")]
    public GameObject pfObstacle1x1;
    [FormerlySerializedAs("_pf_obstacle2x2")]
    public GameObject pfObstacle2x2;
    [FormerlySerializedAs("_pf_obstacle3x3")]
    public GameObject pfObstacle3x3;

    [Tooltip("障碍物挂载父节点，为空时挂在本 GameObject 下")]
    public Transform obstacleParent;
}

[Serializable]
public class MissionPlacementSettings
{
    [Tooltip("任务物体挂载父节点，为空时挂在本 GameObject 下")]
    public Transform missionObject;

    [Tooltip("是否优先选择低噪声区域放置任务点")]
    public bool preferLowestNoise = true;

    [Tooltip("任务点占地区域向外额外留白格数")]
    [Min(0)]
    public int extraMarginCells = 0;

    [Tooltip("任务之间的最小距离（世界坐标），放置时会尽量满足此距离")]
    [Min(0f)]
    public float minDistanceBetweenMissions = 3f;

    [Tooltip("距离不满足时递减步长，逐步降低距离要求重试")]
    [Min(0f)]
    public float distanceStep = 1f;
}

[Serializable]
public class POISettings
{
    [Tooltip("兴趣点挂载父节点，为空时挂在本 GameObject 下")]
    public Transform poiParent;

    [Tooltip("每次生成的 POI 数量")]
    [Min(1)]
    public int poiCount = 3;

    [Tooltip("POI 占地区域向外额外留白格数")]
    [Min(0)]
    public int extraMarginCells = 0;

    [Tooltip("与任务点保持的最小距离（世界坐标）")]
    [Min(0f)]
    public float avoidMissionRadius = 2.5f;

    [Tooltip("POI 之间的最小距离（世界坐标）")]
    [Min(0f)]
    public float minPOIDistance = 3f;
}

[Serializable]
public class EnvironmentSettings
{
    [Tooltip("是否启用环境物体生成")]
    public bool enabled = true;

    [Tooltip("环境物体挂载父节点，为空时挂在本 GameObject 下")]
    public Transform environmentParent;

    [Tooltip("环境物体生成规则")]
    public EnvironmentPrefabRule[] rules;

    [Min(0.1f)]
    [Tooltip("Poisson 最小采样间距（单位：格）")]
    public float poissonRadius = 2.2f;

    [Range(1, 64)]
    [Tooltip("Poisson 每个活动点最大尝试次数")]
    public int maxSamplesPerPoint = 24;

    [Range(0f, 1f)]
    [Tooltip("候选点总体生成概率")]
    public float spawnChance = 0.75f;

    [Range(0f, 1f)]
    [Tooltip("允许生成的噪声最小值")]
    public float validNoiseMin = 0.15f;

    [Range(0f, 1f)]
    [Tooltip("允许生成的噪声最大值")]
    public float validNoiseMax = 0.75f;

    [Min(0)]
    [Tooltip("与地图边缘保持的最小格数")]
    public int edgePaddingCells = 1;

    [Min(0f)]
    [Tooltip("与障碍占位格保持的最小距离（单位：格）")]
    public float avoidObstaclePadding = 0.25f;

    [Min(0f)]
    [Tooltip("与任务点保持的最小距离（单位：世界坐标）")]
    public float avoidMissionRadius = 2.5f;

    [Range(0f, 0.49f)]
    [Tooltip("在所属格子内的随机偏移比例（0=不偏移，0.49=接近格子边缘）")]
    public float cellJitterRatio = 0.25f;

    [Tooltip("环境随机种子偏移，和地图 seed 叠加后得到环境 seed")]
    public int seedOffset = 9973;
}

[Serializable]
public class EnvironmentPrefabRule
{
    public GameObject prefab;

    [Min(0f)]
    public float weight = 1f;

    [Range(0f, 1f)]
    public float spawnChance = 1f;

    [Range(0f, 1f)]
    public float noiseMin = 0f;

    [Range(0f, 1f)]
    public float noiseMax = 1f;
}

[Serializable]
public class GizmoSettings
{
    public bool showGizmos = true;
    public bool showTerrainGizmos = true;
    public bool showObstacleZoneGizmos = true;
    public bool showObstacleGizmos = true;
}
