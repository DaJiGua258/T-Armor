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
    [Range(0f, 1f)]
    public float obstacleThreshold = 0.45f;

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
public class GizmoSettings
{
    public bool showGizmos = true;
    public bool showTerrainGizmos = true;
    public bool showObstacleZoneGizmos = true;
    public bool showObstacleGizmos = true;
}
