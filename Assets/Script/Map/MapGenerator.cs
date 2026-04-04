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

    [Tooltip("低于此噪声值的格子绘制本层 Tile（layers 按阈值从低到高排列）")]
    [Range(0f, 1f)]
    public float threshold = 0.5f;

    [Tooltip("Tilemap 整体着色（白色 = 不染色）")]
    public Color tint = Color.white;

    [Tooltip("本层对应的 Tilemap 组件")]
    public Tilemap tilemap;

    [Tooltip("留空时使用 MapGenerator.defaultTileSet")]
    public TileSet tileSet;
}

// ─── 格子数据 ────────────────────────────────────────────────────────────────

/// <summary>
/// 地图单个格子的完整信息。
/// 使用 struct 减少堆分配开销（64×64 = 4096 个实例）。
/// tileIndex: TILE_FULL(-1) = FULL 变体, 0-11 = borderTiles 索引。
/// </summary>
public struct CellData
{
    public const int LAYER_NONE     = -1;
    public const int LAYER_OBSTACLE = -2;
    public const int TILE_FULL      = -1;

    public float noise;  // 噪声值
    public int   layerIndex; // 层级索引
    public int   tileIndex; // 瓦片索引
    public bool  occupied; // 是否被占用
    public CellFlags flags; // 标志位

    /// <summary>
    /// 创建单元格数据
    /// </summary>
    public static CellData Create(float noise = 0f, int layer = LAYER_NONE)
    {
        return new CellData
        {
            noise      = noise,
            layerIndex = layer,
            tileIndex  = TILE_FULL,
            occupied   = false,
            flags      = CellFlags.None,
        };
    }
}

// CellFlags 是用于标记地图格子属性的“位标志”枚举类型（flags enum），
// 可以用按位运算组合多个属性。比如一个格子既可以被标记为可行走（Walkable），
// 同时也可以是危险（Dangerous）或者带有装饰（Decorated）。
// 这样做便于状态快速判断和高效存储。
//
// 具体属性含义：
// None      ：没有任何标志。
// Walkable  ：格子可被角色移动通过。
// Dangerous ：格子带有危险性，如陷阱或危险地形。
// Decorated ：格子上有装饰物（非功能性元素）。

[System.Flags]
public enum CellFlags : byte
{
    None,  // 无标志
    Walkable  = 1 << 0, // 可行走
}

// ─── MapGenerator ──────────────────────────────────────────────────────────────

public class MapGenerator : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────

    [Header("地图尺寸")]
    [Min(8)]
    public int mapSize = 64;

    [Header("噪声参数")]
    [Min(0.01f)] public float noiseScale = 30f;
    [Range(1, 8)] public int octaves = 4;
    [Range(0f, 1f)] public float persistence = 0.5f;
    [Min(1f)] public float lacunarity = 2f;
    public int     seed       = 0;
    public Vector2 noiseOffset;
    

    [Header("边缘封闭（Falloff Map）")]
    public bool useFalloff = true;
    public AnimationCurve curve;

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
    public bool showObstacleZoneGizmos = true;
    public bool showObstacleGizmos     = true;

    // ── 运行时数据 ──────────────────────────────────────────────────

    private CellData[,] _grid;

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

        BuildNoiseMap();
        BuildGridInfo();

        // 仅计算障碍物位置，不生成预制体
        ResetOccupied();
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
        // 清空地图
        ClearMap();
        
        BuildNoiseMap();  // 生成噪声图
        BuildGridInfo();  // 写入网格信息
        PaintTilemaps();  // 绘制 Tilemap
        SpawnObstacles();  // 放置障碍预制体
    }

    [ContextMenu("清空地图")]
    public void ClearMap()
    {
        if (layers != null)
        {
            foreach (var cfg in layers)
                cfg.tilemap?.ClearAllTiles();
        }

        Transform parent = obstacleParent != null ? obstacleParent : transform;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i).gameObject;
#if UNITY_EDITOR
            DestroyImmediate(child);
#else
            Destroy(child);
#endif
        }

        _grid           = null;
        _gizmoObstacles = new List<ObstacleGizmo>();
    }

    // ── Phase 1: 绘制噪声图 ──────────────────────────────────────────

    /// <summary>
    /// 生成噪声图
    /// </summary>
    private void BuildNoiseMap()
    {
        // 生成基础噪声值图
        float[,] noise = NoiseUtility.GenerateNoiseMap(
            mapSize, mapSize,
            noiseScale, 
            octaves, 
            persistence, 
            lacunarity,
            seed, noiseOffset);

        // 应用边缘封闭（Falloff Map）
        if (useFalloff)
        {
            float[,] falloff = NoiseUtility.GenerateFalloffMap(mapSize, mapSize, curve);
            noise = NoiseUtility.ApplyFalloff(noise, falloff);
        }

        // 将噪声值图转换为 CellData 数组
        _grid = new CellData[mapSize, mapSize];
        for (int y = 0; y < mapSize; y++)
            for (int x = 0; x < mapSize; x++)
                _grid[x, y] = CellData.Create(noise[x, y]);
    }

    // ── Phase 2: 写入网格信息 ────────────────────────────────────────

    private void BuildGridInfo()
    {
        AssignLayers();
        PostProcessLayers();
        ComputeTileIndices();
    }

    /// <summary>
    /// 按 threshold 为每个格子指定所属层级，写入 layerIndex。
    /// </summary>
    private void AssignLayers()
    {
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                float n = _grid[x, y].noise;
                _grid[x, y].layerIndex = CellData.LAYER_NONE;

                for (int l = 0; l < layers.Length; l++)
                {
                    if (n <= layers[l].threshold)
                    {
                        _grid[x, y].layerIndex = l;
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 将无法用素材表达的地形格子形状升级到下一包含层，迭代直到稳定或达到最大次数。
    /// 邻居判断用 InLayerOrLower：同层或更低（更排他）的层均视为"同层内"邻居。
    /// 无效形状：上下同缺 / 左右同缺 / 三面以上缺失 → 升级为 l+1，最外层升为空格。
    /// </summary>
    private void PostProcessLayers()
    {
        if (!enableShapePostProcess) return;

        for (int iter = 0; iter < postProcessIterations; iter++)
        {
            bool changed = false;

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    int l = _grid[x, y].layerIndex;
                    if (l < 0) continue;

                    bool top    = InLayerOrLower(x, y + 1, l);
                    bool bottom = InLayerOrLower(x, y - 1, l);
                    bool lft    = InLayerOrLower(x - 1, y, l);
                    bool rgt    = InLayerOrLower(x + 1, y, l);

                    int missing = (top ? 0 : 1) + (bottom ? 0 : 1) + (lft ? 0 : 1) + (rgt ? 0 : 1);

                    bool invalid = missing >= 3
                        || (!top && !bottom)
                        || (!lft && !rgt);

                    if (invalid)
                    {
                        _grid[x, y].layerIndex = (l + 1 < layers.Length) ? l + 1 : CellData.LAYER_NONE;
                        changed = true;
                    }
                }
            }

            if (!changed) break;
        }
    }

    /// <summary>
    /// 为每个有效格子预计算 tileIndex，写入 _grid。
    /// 后续绘制阶段直接读取，不再做邻居查询。
    /// </summary>
    private void ComputeTileIndices()
    {
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                int l = _grid[x, y].layerIndex;
                _grid[x, y].tileIndex = l < 0 ? CellData.TILE_FULL : ResolveIndex(x, y, l);
            }
        }
    }

    /// <summary>
    /// 根据 8 邻居关系返回 tileIndex（TILE_FULL = -1，或 borderTiles 的 0-11 索引）。
    /// "同层内"邻居定义：layerIndex 在 [0, layerIdx] 之间（同层或更排他的低层）。
    /// 优先级：INNER → FULL → EDGE → OUTER
    /// </summary>
    private int ResolveIndex(int x, int y, int layerIdx)
    {
        bool top    = InLayerOrLower(x, y + 1, layerIdx);
        bool bottom = InLayerOrLower(x, y - 1, layerIdx);
        bool left   = InLayerOrLower(x + 1, y, layerIdx);
        bool right  = InLayerOrLower(x - 1, y, layerIdx);

        if (top && bottom && left && right)
        {
            bool topLeft     = InLayerOrLower(x - 1, y + 1, layerIdx);
            bool topRight    = InLayerOrLower(x + 1, y + 1, layerIdx);
            bool bottomLeft  = InLayerOrLower(x - 1, y - 1, layerIdx);
            bool bottomRight = InLayerOrLower(x + 1, y - 1, layerIdx);

            if (!topLeft)     return 11; // INNER_BL
            if (!topRight)    return 10; // INNER_BR
            if (!bottomLeft)  return 9;  // INNER_TL
            if (!bottomRight) return 8;  // INNER_TR

            return CellData.TILE_FULL;
        }

        if (!top && bottom && left && right) return 0; // EDGE_T
        if (top && !bottom && left && right) return 1; // EDGE_B
        if (top && bottom && left && !right) return 2; // EDGE_L
        if (top && bottom && !left && right) return 3; // EDGE_R

        if (!top && !right && bottom && left)  return 4; // OUTER_TL
        if (!top && !left  && bottom && right) return 5; // OUTER_TR
        if (!bottom && !right && top && left)  return 6; // OUTER_BL
        if (!bottom && !left  && top && right) return 7; // OUTER_BR

        return CellData.TILE_FULL;
    }

    // ── Phase 3: 绘制 Tilemap ─────────────────────────────────────────

    /// <summary>
    /// 按层遍历顺序绘制 Tilemap，并在绘制前设置渲染深度（sortingOrder）。
    /// 规则：
    ///   - 每层绘制所有 noise ≤ threshold[l] 的格子（即 cellLayer ≤ l）
    ///   - cellLayer == l：该格是本层的边界格，使用预计算的 tileIndex
    ///   - cellLayer  < l：该格被更排他的层覆盖，本层在此填充 FULL（将被前层遮挡）
    ///   - 渲染深度：层索引越小 sortingOrder 越大（越靠前显示）
    /// </summary>
    private void PaintTilemaps()
    {
        if (layers == null) return;

        for (int l = 0; l < layers.Length; l++)
        {
            var cfg = layers[l];
            if (cfg.tilemap == null) continue;

            cfg.tilemap.color = cfg.tint;
            TileSet ts = ResolveTileSet(cfg);

            // 层索引越小 = 阈值越低 = 覆盖面积越小 = 越靠前渲染
            var tilemapRenderer = cfg.tilemap.GetComponent<TilemapRenderer>();
            if (tilemapRenderer != null)
                tilemapRenderer.sortingOrder = -l;

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    int cellLayer = _grid[x, y].layerIndex;

                    // 只绘制 noise <= threshold[l] 的格子（cellLayer <= l，且不是空格）
                    if (cellLayer < 0 || cellLayer > l) continue;

                    TileBase tile;
                    if (cellLayer < l || cellLayer == layers.Length - 1)
                    {
                        // 本格属于更排他的低层，本层在此填充 FULL（低层在前会遮盖）
                        tile = PickFull(ts, x, y);
                    }
                    else
                    {
                        // cellLayer == l：本格是本层的边界格，读取预计算 tileIndex
                        int ti = _grid[x, y].tileIndex;
                        tile = ti == CellData.TILE_FULL ? PickFull(ts, x, y) : Border(ts, ti);
                    }

                    if (tile != null)
                        cfg.tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }
    }

    // ── Tile 工具 ────────────────────────────────────────────────────

    private TileSet ResolveTileSet(LayerConfig cfg)
    {
        var ts = cfg.tileSet;
        if (ts != null && ts.borderTiles != null && ts.borderTiles.Length == 12)
            return ts;
        return defaultTileSet;
    }

    private bool InLayer(int x, int y, int layerIdx)
    {
        if (_grid == null || layers == null) return false;
        if (x < 0 || x >= mapSize || y < 0 || y >= mapSize) return false;
        return _grid[x, y].layerIndex == layerIdx;
    }

    /// <summary>
    /// 判断坐标 (x,y) 的格子是否"属于层级 layerIdx 内"。
    /// 新模型：层级索引越低 = 越排他（阈值越低）。
    /// "同层内"定义：layerIndex 在 [0, layerIdx] 之间，即该格也在 layerIdx 的绘制范围之内。
    /// </summary>
    private bool InLayerOrLower(int x, int y, int layerIdx)
    {
        if (_grid == null || layers == null) return false;
        if (x < 0 || x >= mapSize || y < 0 || y >= mapSize) return false;
        int l = _grid[x, y].layerIndex;
        return l >= 0 && l <= layerIdx;
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

    // ── Phase 4: 放置障碍预制体 ──────────────────────────────────────

    private void SpawnObstacles()
    {
        ResetOccupied();
        _gizmoObstacles = new List<ObstacleGizmo>();

        PlaceObstacleSize(prefab3x3, 3);
        PlaceObstacleSize(prefab2x2, 2);
        PlaceObstacleSize(prefab1x1, 1);
    }

    private void ResetOccupied()
    {
        if (_grid == null) return;
        for (int y = 0; y < mapSize; y++)
            for (int x = 0; x < mapSize; x++)
                _grid[x, y].occupied = false;
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
                if (cx >= mapSize || cy >= mapSize)        return false;
                if (_grid[cx, cy].layerIndex != -1)          return false;
                if (_grid[cx, cy].noise > obstacleThreshold) return false;
                if (_grid[cx, cy].occupied)                  return false;
            }
        }
        return true;
    }

    private void MarkOccupied(int ox, int oy, int size)
    {
        for (int dy = 0; dy < size; dy++)
            for (int dx = 0; dx < size; dx++)
                _grid[ox + dx, oy + dy].occupied = true;
    }

    private void DoSpawn(GameObject prefab, Vector3 pos)
    {
        Transform parent = obstacleParent != null ? obstacleParent : transform;

        int randomIndex = Random.Range(0, 3);
        Vector3 randomRotation = new Vector3(0f, 0f, 90 * randomIndex);

#if UNITY_EDITOR
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        obj.transform.position = pos;
        obj.transform.localRotation = Quaternion.Euler(randomRotation);
        Undo.RegisterCreatedObjectUndo(obj, "MapGen Obstacle");
#else
        var obj = Instantiate(prefab, parent);
        obj.transform.position = pos;
        obj.transform.localRotation = Quaternion.Euler(randomRotation);
#endif
    }

    // ── 坐标工具 ─────────────────────────────────────────────────────

    /// <summary>
    /// 返回从格子 (x,y) 开始、占据 size×size 格的障碍物世界中心坐标。
    /// 偏移量在本地空间计算后再转换为世界坐标，确保父物体旋转后结果依然正确。
    /// </summary>
    private Vector3 CellCenter(int x, int y, int size)
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
        Vector2 cs = tm != null ? (Vector2)tm.cellSize : Vector2.one;

        Matrix4x4 prevMatrix = Gizmos.matrix;
        Transform drawBase   = tm != null ? tm.transform : transform;
        Gizmos.matrix = drawBase.localToWorldMatrix;

        Vector3 localOrigin = tm != null
            ? (Vector3)tm.CellToLocal(Vector3Int.zero)
            : Vector3.zero;
        Vector3 boxCenter = localOrigin + new Vector3(mapSize * cs.x * 0.5f, mapSize * cs.y * 0.5f, 0f);
        Vector3 boxSize   = new Vector3(mapSize * cs.x, mapSize * cs.y, 0.01f);

        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireCube(boxCenter, boxSize);

        Vector3 cubeSize = new Vector3(cs.x * 0.88f, cs.y * 0.88f, 0.01f);

        if (showTerrainGizmos && _grid != null && layers != null)
        {
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    int l = _grid[x, y].layerIndex;
                    Vector3 cellLocal = CellCenterLocal(x, y, 1, tm, cs);

                    if (l >= 0 && l < layers.Length)
                    {
                        Color c = layers[l].tint;
                        c.a = 1f;
                        Gizmos.color = c;
                        Gizmos.DrawCube(cellLocal, cubeSize);
                    }
                    else if (showObstacleZoneGizmos && l == -1
                             && _grid[x, y].noise <= obstacleThreshold)
                    {
                        Gizmos.color = new Color(0.65f, 0.15f, 0.85f, 1f);
                        Gizmos.DrawCube(cellLocal, cubeSize);
                    }
                }
            }
        }

        if (showObstacleGizmos && _gizmoObstacles != null)
        {
            foreach (var g in _gizmoObstacles)
            {
                Gizmos.color = GetObstacleGizmoColor(g.size);
                Vector3 localPos = drawBase.InverseTransformPoint(g.center);
                Vector3 obstacleCubeSize = new Vector3(
                    g.size * cs.x * 0.92f,
                    g.size * cs.y * 0.92f,
                    0.01f);
                Gizmos.DrawCube(localPos, obstacleCubeSize);
            }
        }

        Gizmos.matrix = prevMatrix;
    }

    /// <summary>
    /// 返回格子 (x,y) size×size 区块中心在 drawBase 本地空间中的坐标（供 Gizmos 使用）。
    /// </summary>
    private Vector3 CellCenterLocal(int x, int y, int size, Tilemap tm, Vector2 cs)
    {
        if (tm != null)
        {
            Vector3 localPos = tm.GetCellCenterLocal(new Vector3Int(x, y, 0));
            localPos.x += (size - 1) * 0.5f * cs.x;
            localPos.y += (size - 1) * 0.5f * cs.y;
            return localPos;
        }
        return new Vector3(x + size * 0.5f, y + size * 0.5f, 0f);
    }

    private static Color GetObstacleGizmoColor(int size)
    {
        switch (size)
        {
            case 3: return new Color(1f, 0.25f, 0.21f);
            case 2: return new Color(1f, 0.86f, 0f);
            case 1: return new Color(0.5f, 0.86f, 1f);
            default: return Color.white;
        }
    }
}
