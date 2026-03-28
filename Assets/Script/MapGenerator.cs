using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ─── 数据结构 ──────────────────────────────────────────────────────────────────

/// <summary>
/// 一套 Tile 资源：FULL 变体（支持多张随机）+ 12 种固定边缘/转角。
/// borderTiles 索引顺序：
///   0  EDGE_T   1  EDGE_B   2  EDGE_L   3  EDGE_R
///   4  OUTER_TL 5  OUTER_TR 6  OUTER_BL 7  OUTER_BR
///   8  INNER_TL 9  INNER_TR 10 INNER_BL 11 INNER_BR
/// </summary>
[System.Serializable]
public class TileSet
{
    [Tooltip("FULL 类型，可填多张；运行时按坐标哈希随机选取，只有一张时直接使用")]
    public TileBase[] fullVariants;

    [Tooltip("12 种边缘/转角，顺序固定（见 TileSet 注释），长度须为 12")]
    public TileBase[] borderTiles;
}

/// <summary>
/// 单层地形配置。
/// </summary>
[System.Serializable]
public class LayerConfig
{
    public string name;

    [Tooltip("高于此噪声值的格子绘制本层 Tile")]
    [Range(0f, 1f)]
    public float threshold = 0.5f;

    [Tooltip("Tilemap 整体着色（白色 = 不染色）")]
    public Color tint = Color.white;

    [Tooltip("本层对应的 Tilemap 组件")]
    public Tilemap tilemap;

    [Tooltip("留空时使用 MapGenerator.defaultTileSet")]
    public TileSet tileSet;
}

// ─── MapGenerator ──────────────────────────────────────────────────────────────

public class MapGenerator : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────

    [Header("地图尺寸")]
    [Min(8)]
    public int mapSize = 64;

    [Header("噪声参数")]
    [Min(0.01f)] public float noiseScale    = 30f;
    [Range(1, 8)] public int  octaves       = 4;
    [Range(0f, 1f)] public float persistence = 0.5f;
    [Min(1f)] public float lacunarity       = 2f;
    public int     seed       = 0;
    public Vector2 noiseOffset;
    public AnimationCurve curve;

    [Header("边缘封闭（Falloff Map）")]
    public bool useFalloff = true;

    [Header("默认 Tile 集（所有层共享回退）")]
    public TileSet defaultTileSet;

    [Header("地形层（threshold 从低到高排列）")]
    public LayerConfig[] layers;

    [Header("噪声后处理（修复无效形状）")]
    [Tooltip("开启后对无法用素材表达的格子形状进行降级处理")]
    public bool enableShapePostProcess = true;
    [Tooltip("最大迭代次数，防止极端情况下反复降级（建议 3~5）")]
    [Range(1, 10)]
    public int postProcessIterations = 4;

    [Header("障碍物")]
    [Tooltip("噪声值高于此阈值且无地形层覆盖的格子放置障碍物")]
    [Range(0f, 1f)]
    public float obstacleThreshold = 0.45f;
    public GameObject prefab1x1;
    public GameObject prefab2x2;
    public GameObject prefab3x3;
    [Tooltip("障碍物挂载父节点，为空时挂在本 GameObject 下")]
    public Transform obstacleParent;

    [Header("Gizmos")]
    public bool showGizmos             = true;
    public bool showTerrainGizmos      = true;
    public bool showObstacleZoneGizmos = true; // 障碍区底色（紫色）
    public bool showObstacleGizmos     = true; // 已放置障碍物线框

    // ── 运行时数据 ──────────────────────────────────────────────────

    private float[,] _noise;
    private int[,]   _layerIndexInMap;
    private bool[,]  _occupied;

    private struct ObstacleGizmo { public Vector3 center; public int size; }
    private List<ObstacleGizmo> _gizmoObstacles = new List<ObstacleGizmo>();

    // ── 编辑器实时预览 ───────────────────────────────────────────────

#if UNITY_EDITOR
    private bool _previewPending;

    private void OnValidate()
    {
        if (_previewPending) return;
        _previewPending = true;
        EditorApplication.delayCall += () =>
        {
            _previewPending = false;
            if (this == null) return;
            RebuildPreview();
        };
    }

    private void RebuildPreview()
    {
        if (layers == null || layers.Length == 0 || mapSize < 1) return;

        BuildNoise();
        BuildLayerMap();

        // 仅计算障碍物位置，不生成预制体
        _occupied       = new bool[mapSize, mapSize];
        _gizmoObstacles = new List<ObstacleGizmo>();
        PlaceObstacleSize(null, 3);
        PlaceObstacleSize(null, 2);
        PlaceObstacleSize(null, 1);

        UnityEditor.SceneView.RepaintAll();
    }
#endif

    // ── 公共 API ────────────────────────────────────────────────────

    [ContextMenu("生成地图")]
    public void Generate()
    {
        ClearMap();
        BuildNoise();
        BuildLayerMap();
        PaintTilemaps();
        SpawnObstacles();
    }

    [ContextMenu("清空地图")]
    public void ClearMap()
    {
        // 清空地形层
        if (layers != null)
        {
            foreach (var cfg in layers)
            {
                cfg.tilemap?.ClearAllTiles();
            }
        }

        Transform parent = obstacleParent != null ? obstacleParent : transform;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i).gameObject;
#if UNITY_EDITOR
            // 编辑器模式下用 DestroyImmediate 避免延迟
            DestroyImmediate(child);
#else
            Destroy(child);
#endif
        }

        _noise           = null;
        _layerIndexInMap        = null;
        _gizmoObstacles  = new List<ObstacleGizmo>();
    }

    // ── 噪声 & 层级图 ────────────────────────────────────────────────

    private void BuildNoise()
    {
        _noise = NoiseUtility.GenerateNoiseMap(
            mapSize, mapSize,
            noiseScale, octaves, persistence, lacunarity,
            seed, noiseOffset);

        if (useFalloff)
        {
            float[,] falloff = NoiseUtility.GenerateFalloffMap(mapSize, mapSize, curve);
            _noise = NoiseUtility.ApplyFalloff(_noise, falloff);
        }
    }

    private void BuildLayerMap()
    {
        _layerIndexInMap = new int[mapSize, mapSize];

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                _layerIndexInMap[x, y] = -1;
                float n = _noise[x, y];

                for (int l = 0; l < layers.Length; l++)
                {
                    if (n <= layers[l].threshold)
                    {
                        _layerIndexInMap[x, y] = l;
                        break;
                    }
                }
            }
        }

        if (enableShapePostProcess)
            PostProcessLayerMap();
    }

    /// <summary>
    /// 将无法用素材表达的地形格子形状降级为下一层，迭代直到稳定或达到最大次数。
    /// 邻居判断用 InLayerOrHigher，与 SelectTile 一致：同层或更高层均视为有效邻居，
    /// 最高层朝向空白区（-1）的边界不会被误判为无效形状。
    /// 无效形状：上下同缺 / 左右同缺 / 三面以上缺失 → 降级为 l-1。
    /// </summary>
    private void PostProcessLayerMap()
    {
        for (int iter = 0; iter < postProcessIterations; iter++)
        {
            bool changed = false;

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    int l = _layerIndexInMap[x, y];
                    if (l <= 0) continue; // 第 0 层和障碍层（-1）不处理

                    // 与 SelectTile 保持一致：同层或更高层均视为有效邻居
                    bool top    = InLayerOrHigher(x, y + 1, l);
                    bool bottom = InLayerOrHigher(x, y - 1, l);
                    bool lft    = InLayerOrHigher(x - 1, y, l);
                    bool rgt    = InLayerOrHigher(x + 1, y, l);

                    int missing = (top ? 0 : 1) + (bottom ? 0 : 1) + (lft ? 0 : 1) + (rgt ? 0 : 1);

                    bool invalid = missing >= 3
                        || (!top && !bottom)
                        || (!lft && !rgt);

                    if (invalid)
                    {
                        _layerIndexInMap[x, y] = l - 1;
                        changed = true;
                    }
                }
            }

            if (!changed) break;
        }
    }

    // ── Tilemap 绘制 ─────────────────────────────────────────────────

    private void PaintTilemaps()
    {
        if (layers == null) return;

        for (int l = 0; l < layers.Length; l++)
        {
            var cfg = layers[l];
            if (cfg.tilemap == null) continue;

            cfg.tilemap.color = cfg.tint;
            TileSet ts = ResolveTileSet(cfg);

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    int cellLayer = _layerIndexInMap[x, y];

                    if (l == 0)
                    {
                        // 第 0 层：所有格子都绘制（作为底层背景）
                        cfg.tilemap.SetTile(new Vector3Int(x, y, 0), SelectTile(ts, x, y, l));
                    }
                    else if (cellLayer == l)
                    {
                        // 本格属于当前层：正常选 Tile
                        cfg.tilemap.SetTile(new Vector3Int(x, y, 0), SelectTile(ts, x, y, l));
                    }
                    else if (cellLayer > l)
                    {
                        // 本格属于更高层：在当前层 Tilemap 上全部填充 FULL（无论距离边界多远）
                        cfg.tilemap.SetTile(new Vector3Int(x, y, 0), PickFull(ts, x, y));
                    }
                }
            }
        }
    }

    // ── Tile 选取 ────────────────────────────────────────────────────

    /// <summary>
    /// 解析层级配置中的 TileSet
    /// </summary>
    /// <param name="cfg"></param>
    /// <returns></returns>
    private TileSet ResolveTileSet(LayerConfig cfg)
    {
        var ts = cfg.tileSet;
        if (ts != null && ts.borderTiles != null && ts.borderTiles.Length == 12)
            return ts;
        return defaultTileSet;  // 返回默认 tileSet
    }

    /// <summary>
    /// 根据 8 邻居 bitmask 选出正确的 TileBase。
    /// 当某方向邻居属于更高层时，视为"有邻居"（使用 FULL 而非边缘）。
    /// 优先级：INNER → FULL → EDGE → OUTER
    /// </summary>
    private TileBase SelectTile(TileSet ts, int x, int y, int layerIdx)
    {
        if (layerIdx == 0) return PickFull(ts, x, y);

        // InLayerOrHigher：本层或更高层均视为"有邻居"，使边界不出现在高层接触面
        bool top    = InLayerOrHigher(x, y + 1, layerIdx);
        bool bottom = InLayerOrHigher(x, y - 1, layerIdx);
        bool left   = InLayerOrHigher(x + 1, y, layerIdx);
        bool right  = InLayerOrHigher(x - 1, y, layerIdx);

        if (top && bottom && left && right)
        {
            // 四周均有邻居 → 检查对角线决定内转角 / FULL
            bool topLeft     = InLayerOrHigher(x - 1, y + 1, layerIdx);
            bool topRight    = InLayerOrHigher(x + 1, y + 1, layerIdx);
            bool bottomLeft  = InLayerOrHigher(x - 1, y - 1, layerIdx);
            bool bottomRight = InLayerOrHigher(x + 1, y - 1, layerIdx);

            if (!topLeft)     return Border(ts, 11); // INNER_BL
            if (!topRight)    return Border(ts, 10); // INNER_BR
            if (!bottomLeft)  return Border(ts, 9);  // INNER_TL
            if (!bottomRight) return Border(ts, 8);  // INNER_TR

            return PickFull(ts, x, y);
        }

        // 单侧缺失 → 边缘
        if (!top && bottom && left && right) return Border(ts, 0); // EDGE_T
        if (top && !bottom && left && right) return Border(ts, 1); // EDGE_B
        if (top && bottom && left && !right) return Border(ts, 2); // EDGE_L
        if (top && bottom && !left && right) return Border(ts, 3); // EDGE_R

        // 两相邻侧缺失 → 外转角
        if (!top && !right && bottom && left) return Border(ts, 4); // OUTER_TL
        if (!top && !left  && bottom && right) return Border(ts, 5); // OUTER_TR
        if (!bottom && !right && top && left)  return Border(ts, 6); // OUTER_BL
        if (!bottom && !left  && top && right) return Border(ts, 7); // OUTER_BR

        // 后处理后理论上不会到达这里，兜底 FULL
        return PickFull(ts, x, y);
    }

    /// <summary>
    /// 判断坐标 (x,y) 是否精确属于层级 layerIdx（后处理后以 _layerIndexInMap 为准）。
    /// </summary>
    private bool InLayer(int x, int y, int layerIdx)
    {
        if (_layerIndexInMap == null || layers == null) return false;
        if (x < 0 || x >= mapSize || y < 0 || y >= mapSize) return false;
        return _layerIndexInMap[x, y] == layerIdx;
    }

    /// <summary>
    /// 判断坐标 (x,y) 是否属于层级 layerIdx 或更高层（更大索引）。
    /// 用于 SelectTile：与更高层接触的边不显示边缘 Tile，而是视为"有邻居"。
    /// </summary>
    private bool InLayerOrHigher(int x, int y, int layerIdx)
    {
        if (_layerIndexInMap == null || layers == null) return false;
        if (x < 0 || x >= mapSize || y < 0 || y >= mapSize) return false;
        int cellLayer = _layerIndexInMap[x, y];
        return cellLayer >= layerIdx;
    }

    private TileBase Border(TileSet ts, int index)
    {
        if (ts?.borderTiles == null || index >= ts.borderTiles.Length) 
            return null;
        return ts.borderTiles[index];
    }

    /// <summary>
    /// FULL 变体选取：单张直取；多张时用坐标哈希保证同 seed 结果一致。
    /// </summary>
    private TileBase PickFull(TileSet ts, int x, int y)
    {
        if (ts?.fullVariants == null || ts.fullVariants.Length == 0) return null;
        if (ts.fullVariants.Length == 1) return ts.fullVariants[0];
        int idx = Mathf.Abs((x * 73856093) ^ (y * 19349663)) % ts.fullVariants.Length;
        return ts.fullVariants[idx];
    }

    // ── 障碍物放置 ───────────────────────────────────────────────────

    private void SpawnObstacles()
    {
        _occupied       = new bool[mapSize, mapSize];
        _gizmoObstacles = new List<ObstacleGizmo>();

        PlaceObstacleSize(prefab3x3, 3);
        PlaceObstacleSize(prefab2x2, 2);
        PlaceObstacleSize(prefab1x1, 1);
    }

    private void PlaceObstacleSize(GameObject prefab, int size)
    {
        for (int y = 0; y <= mapSize - size; y++)
        {
            for (int x = 0; x <= mapSize - size; x++)
            {
                if (!CanPlace(x, y, size)) continue;

                MarkOccupied(x, y, size);
                Vector3 pos = CellCenter(x, y, size);
                _gizmoObstacles.Add(new ObstacleGizmo { center = pos, size = size });
                if (prefab != null) DoSpawn(prefab, pos);
            }
        }
    }

    private bool CanPlace(int ox, int oy, int size)
    {
        for (int dy = 0; dy < size; dy++)
        {
            for (int dx = 0; dx < size; dx++)
            {
                int cx = ox + dx, cy = oy + dy;
                if (cx >= mapSize || cy >= mapSize)    return false;
                if (_layerIndexInMap[cx, cy] != -1)           return false; // 有地形层，不放障碍
                if (_noise[cx, cy] > obstacleThreshold) return false; // 高于障碍阈值（阈值越大障碍越多）
                if (_occupied[cx, cy])                 return false; // 已占用
            }
        }
        return true;
    }

    private void MarkOccupied(int ox, int oy, int size)
    {
        for (int dy = 0; dy < size; dy++)
            for (int dx = 0; dx < size; dx++)
                _occupied[ox + dx, oy + dy] = true;
    }

    private void DoSpawn(GameObject prefab, Vector3 pos)
    {
        Transform parent = obstacleParent != null ? obstacleParent : transform;
#if UNITY_EDITOR
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        obj.transform.position = pos;
        Undo.RegisterCreatedObjectUndo(obj, "MapGen Obstacle");
#else
        Instantiate(prefab, pos, Quaternion.identity, parent);
#endif
    }

    // ── 坐标工具 ─────────────────────────────────────────────────────

    /// <summary>
    /// 返回从格子 (x,y) 开始、占据 size×size 格的障碍物世界中心坐标。
    /// </summary>
    private Vector3 CellCenter(int x, int y, int size)
    {
        Tilemap tm = GetReferenceTilemap();
        if (tm != null)
        {
            // GetCellCenterWorld 返回格子 (x,y) 的世界中心
            Vector3 cellCenter = tm.GetCellCenterWorld(new Vector3Int(x, y, 0));
            // 对于 size > 1，向右上偏移半个 size（单位格数）
            cellCenter.x += (size - 1) * 0.5f * tm.cellSize.x;
            cellCenter.y += (size - 1) * 0.5f * tm.cellSize.y;
            return cellCenter;
        }
        return new Vector3(x + size * 0.5f, y + size * 0.5f, 0f);
    }

    private Tilemap GetReferenceTilemap()
    {
        if (layers == null) return null;
        foreach (var cfg in layers)
            if (cfg.tilemap != null) return cfg.tilemap;
        return null;
    }

    // ── Gizmos ────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Tilemap tm = GetReferenceTilemap();

        // cellSize：有 Tilemap 时取真实格子世界大小，否则按 1×1
        Vector2 cs = tm != null ? (Vector2)tm.cellSize : Vector2.one;

        // 地图左下角世界坐标（格子 (0,0) 的底左顶点）
        Vector3 worldMin = tm != null
            ? tm.CellToWorld(Vector3Int.zero)
            : transform.position;

        // 地图右上角世界坐标（格子 (mapSize, mapSize) 的底左顶点 = 最后一格的右上顶点）
        Vector3 worldMax = worldMin + new Vector3(mapSize * cs.x, mapSize * cs.y, 0f);

        // 边界框
        Vector3 boxCenter = (worldMin + worldMax) * 0.5f;
        boxCenter.z = 0f;
        Vector3 boxSize = new Vector3(mapSize * cs.x, mapSize * cs.y, 0.01f);

        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireCube(boxCenter, boxSize);

        // 每格 Wire Cube 大小（相对于格子尺寸）
        Vector3 cubeSize = new Vector3(cs.x * 0.88f, cs.y * 0.88f, 0.01f);

        // 地形层格子（Inspector 修改或生成后均有数据）
        if (showTerrainGizmos && _layerIndexInMap != null && layers != null)
        {
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    int l = _layerIndexInMap[x, y];

                    if (l >= 0 && l < layers.Length)
                    {
                        Color c = layers[l].tint;
                        c.a = 1f;
                        Gizmos.color = c;
                        Gizmos.DrawCube(CellCenter(x, y, 1), cubeSize);
                    }
                    else if (showObstacleZoneGizmos && l == -1
                             && _noise != null && _noise[x, y] <= obstacleThreshold)
                    {
                        Gizmos.color = new Color(0.65f, 0.15f, 0.85f, 1f);
                        Gizmos.DrawCube(CellCenter(x, y, 1), cubeSize);
                    }
                }
            }
        }

        // 障碍物线框
        if (showObstacleGizmos && _gizmoObstacles != null)
        {
            foreach (var g in _gizmoObstacles)
            {
                Gizmos.color = GetObstacleGizmoColor(g.size);
                Vector3 obstacleCubeSize = new Vector3(
                    g.size * cs.x * 0.92f,
                    g.size * cs.y * 0.92f,
                    0.01f);
                Gizmos.DrawCube(g.center, obstacleCubeSize);
            }
        }
    }

    private static Color GetObstacleGizmoColor(int size)
    {
        switch (size)
        {
            case 3: return new Color(1f, 0.25f, 0.21f); // 红色
            case 2: return new Color(1f, 0.86f, 0f);    // 黄色
            case 1: return new Color(0.5f, 0.86f, 1f);  // 青色
            default: return Color.white;
        }
    }
}