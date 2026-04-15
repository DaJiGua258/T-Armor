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
   public float obstacleThreshold = 0.45f;
    public Color obstacleColor = new Color(0.5f, 0.86f, 1f, 1f);
    public GameObject Pf_obstacle1x1;
    public GameObject Pf_obstacle2x2;
    public GameObject Pf_obstacle3x3;  

}
