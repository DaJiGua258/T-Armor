using QFramework.Model;
using QFramework.System;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "MapGeneratorParameters", menuName = "Map Generator Parameters")]
public class MapGeneratorParametersSO : ScriptableObject
{
    [System.Serializable]
    public class LayerParameter
    {
        [Range(0f, 1f)] public float threshold = 0.5f;
        public Color tint = Color.white;
        public TileSet tileSet;
    }

    [System.Serializable]
    public class EnvironmentRuleParameter
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

    [Header("地图尺寸")]
    [Min(8)]
    public int mapSize = 64;
    // public EnvironmentData envData;

    [Header("噪声参数")]
    [Min(0.01f)] public float noiseScale = 30f;
    [Range(1, 8)] public int octaves = 4;
    [Range(0f, 1f)] public float persistence = 0.5f;
    [Min(1f)] public float lacunarity = 2f;
    public int seed = 0;
    public Vector2 noiseOffset;

    [Header("边缘封闭（Falloff Map）")]
    public bool useFalloff = true;
    public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("默认 Tile 集（所有层共享回退）")]
    public TileSet defaultTileSet;

    [Header("地形层参数（不包含 Tilemap 场景引用）")]
    public LayerParameter[] layers;

    [Header("噪声后处理（修复无效形状）")]
    public bool enableShapePostProcess = true;
    [Range(1, 10)] public int postProcessIterations = 4;

    [Header("障碍物")]
    public Color obstacleColor = new Color(0.5f, 0.86f, 1f, 1f);
    public GameObject Pf_obstacle1x1;
    public GameObject Pf_obstacle2x2;
    public GameObject Pf_obstacle3x3;

    [Header("任务点")]
    public bool preferLowestNoise = true;
    [Min(0)] public int extraMarginCells = 0;
    [Min(0f)] public float minDistanceBetweenMissions = 3f;
    [Min(0f)] public float distanceStep = 1f;

    [Header("兴趣点")]
    public int poiCount = 3;
    [Min(0)] public int poiExtraMarginCells = 0;
    [Min(0f)] public float poiAvoidMissionRadius = 2.5f;
    [Min(0f)] public float poiMinDistance = 3f;

    [Header("环境物体")]
    public bool environmentEnabled = true;
    public EnvironmentRuleParameter[] environmentRules;
    [Min(0.1f)] public float poissonRadius = 2.2f;
    [Range(1, 64)] public int maxSamplesPerPoint = 24;
    [Range(0f, 1f)] public float environmentSpawnChance = 0.75f;
    [Range(0f, 1f)] public float validNoiseMin = 0.15f;
    [Range(0f, 1f)] public float validNoiseMax = 0.75f;
    [Min(0)] public int edgePaddingCells = 1;
    [Min(0f)] public float avoidObstaclePadding = 0.25f;
    [Min(0f)] public float avoidMissionRadius = 2.5f;
    [Range(0f, 0.49f)] public float cellJitterRatio = 0.25f;
    public int environmentSeedOffset = 9973;

}
