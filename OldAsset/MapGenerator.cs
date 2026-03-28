using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [Header("地图尺寸")]
    public int Width = 64;
    public int Height = 64;

    [Header("噪声参数")]
    public int Seed = 0;
    public float Scale = 30f;
    [Range(1, 8)] public int Octaves = 4;
    [Range(0f, 1f)] public float Persistence = 0.5f;
    public float Lacunarity = 2f;

    [Header("地形阈值")]
    [Range(0f, 1f)] public float SandThreshold = 0.35f;     // < 此值 = 沙地
    [Range(0f, 1f)] public float MountainThreshold = 0.65f; // > 此值 = 山体区域（泥土地面 + 生成山体）

    [Header("Tilemap 引用")]
    public Tilemap GroundTilemap;
    public Tilemap CollisionTilemap;

    [Header("地面 Tile")]
    public TileBase SandTile;    // 噪声 < SandThreshold
    public TileBase GrassTile;   // 噪声在两阈值之间
    public TileBase DirtTile;    // 噪声 >= MountainThreshold（山体底面）

    [Header("地形颜色（素材需为灰阶）")]
    public Color SandColor  = new Color(0.88f, 0.76f, 0.44f);
    public Color GrassColor = new Color(0.22f, 0.68f, 0.28f);
    public Color DirtColor  = new Color(0.48f, 0.32f, 0.18f);

    [Header("碰撞 Tile（CollisionTilemap 用，可不可见）")]
    public TileBase CollisionTile;

    [Header("山体预制体")]
    public GameObject MountainPrefab1x1;
    public GameObject MountainPrefab2x2;
    public GameObject MountainPrefab3x3;

    [Header("山体父节点")]
    public Transform PropsParent;

    [Header("Gizmos 调试")]
    public bool ShowGizmos = true;
    [Tooltip("Inspector 参数变化时自动刷新 Gizmos 预览（只更新数据，不修改场景）")]
    public bool AutoPreview = true;

    // ── 运行时 / 预览数据 ────────────────────────────────────
    private float[,] _noiseMap;
    // 0=空地  1=1×1山体  2=2×2山体  3=3×3山体
    private int[,] _occupied;
    // 每个山体的起点坐标与尺寸，供 Gizmos 精确绘制
    private System.Collections.Generic.List<(int x, int y, int size)> _mountainOrigins;

    // ───────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!ShowGizmos || !AutoPreview) return;

        // 延迟一帧：避免在序列化回调中直接调用 Unity API 导致警告
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null) return;
            RefreshPreview();
        };
    }

    /// <summary>
    /// 仅重算噪声图和模拟占位，不修改 Tilemap 也不实例化 GameObject。
    /// </summary>
    private void RefreshPreview()
    {
        if (Width <= 0 || Height <= 0) return;
        _noiseMap = NoiseMapUtil.Generate(Width, Height, Seed, Scale, Octaves, Persistence, Lacunarity);
        SimulatePlacement();
        UnityEditor.SceneView.RepaintAll();
    }
#endif

    // ── 生成 / 清除 ───────────────────────────────────────────

    [ContextMenu("生成地图")]
    public void Generate()
    {
        Clear();

        _noiseMap = NoiseMapUtil.Generate(Width, Height, Seed, Scale, Octaves, Persistence, Lacunarity);
        _occupied = new int[Width, Height];

        PaintGround();
        PlaceMountains();
    }

    [ContextMenu("清除地图")]
    public void Clear()
    {
        if (GroundTilemap != null) GroundTilemap.ClearAllTiles();
        if (CollisionTilemap != null) CollisionTilemap.ClearAllTiles();

        if (PropsParent != null)
        {
            for (int i = PropsParent.childCount - 1; i >= 0; i--)
            {
                GameObject child = PropsParent.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
        }

        _occupied = null;
    }

    // ── 地面 Tile ────────────────────────────────────────────

    private void PaintGround()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float v = _noiseMap[x, y];
                TileBase tile;
                Color color;

                if (v < SandThreshold)
                    { tile = SandTile;  color = SandColor; }
                else if (v < MountainThreshold)
                    { tile = GrassTile; color = GrassColor; }
                else
                    { tile = DirtTile;  color = DirtColor; }

                Vector3Int pos = new Vector3Int(x, y, 0);
                GroundTilemap.SetTile(pos, tile);
                GroundTilemap.SetTileFlags(pos, TileFlags.None);
                GroundTilemap.SetColor(pos, color);
            }
        }
    }

    // ── 山体放置 ─────────────────────────────────────────────

    private void PlaceMountains()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_occupied[x, y] != 0) continue;
                if (_noiseMap[x, y] < MountainThreshold) continue;

                if (CanPlace(x, y, 3))
                    SpawnMountain(MountainPrefab3x3, x, y, 3);
                else if (CanPlace(x, y, 2))
                    SpawnMountain(MountainPrefab2x2, x, y, 2);
                else
                    SpawnMountain(MountainPrefab1x1, x, y, 1);
            }
        }
    }

    /// <summary>
    /// 只计算 _occupied 和 _mountainOrigins，不接触场景。供编辑器预览调用。
    /// 与 PlaceMountains 逻辑完全一致，确保 Gizmos 与实际生成吻合。
    /// </summary>
    private void SimulatePlacement()
    {
        _occupied = new int[Width, Height];
        _mountainOrigins = new System.Collections.Generic.List<(int x, int y, int size)>();

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (_occupied[x, y] != 0) continue;
                if (_noiseMap[x, y] < MountainThreshold) continue;

                int size = CanPlace(x, y, 3) ? 3 : CanPlace(x, y, 2) ? 2 : 1;
                _mountainOrigins.Add((x, y, size));
                for (int dy = 0; dy < size; dy++)
                    for (int dx = 0; dx < size; dx++)
                        _occupied[x + dx, y + dy] = size;
            }
        }
    }

    /// <summary>
    /// 检查以 (originX, originY) 为左下角、边长为 size 的区域是否可放置：
    /// 全部格子需在边界内、未被占用、且噪声值满足山体阈值。
    /// </summary>
    private bool CanPlace(int originX, int originY, int size)
    {
        for (int dy = 0; dy < size; dy++)
        {
            for (int dx = 0; dx < size; dx++)
            {
                int nx = originX + dx;
                int ny = originY + dy;
                if (nx >= Width || ny >= Height) return false;
                if (_occupied[nx, ny] != 0) return false;
                if (_noiseMap[nx, ny] < MountainThreshold) return false;
            }
        }
        return true;
    }

    /// <summary>
    /// 实例化山体 Prefab，标记 occupied、刷碰撞 Tile，并禁用 SpriteStacking.Update。
    /// </summary>
    private void SpawnMountain(GameObject prefab, int originX, int originY, int size)
    {
        if (prefab == null) return;

        for (int dy = 0; dy < size; dy++)
        {
            for (int dx = 0; dx < size; dx++)
            {
                int nx = originX + dx;
                int ny = originY + dy;
                _occupied[nx, ny] = size;

                if (CollisionTile != null && CollisionTilemap != null)
                    CollisionTilemap.SetTile(new Vector3Int(nx, ny, 0), CollisionTile);
            }
        }

        Vector3 cellBottomLeft = GroundTilemap.CellToWorld(new Vector3Int(originX, originY, 0));
        Vector3 cellSize = GroundTilemap.cellSize;
        Vector3 pos = cellBottomLeft + new Vector3(cellSize.x * size * 0.5f, cellSize.y * size * 0.5f, 0f);

        GameObject go = Instantiate(prefab, pos, Quaternion.identity, PropsParent);
        go.name = $"Mountain_{size}x{size}_{originX}_{originY}";

        SpriteStacking ss = go.GetComponent<SpriteStacking>();
        if (ss != null) ss.enabled = false;
    }

    // ── Gizmos 可视化 ─────────────────────────────────────────

    // 三种山体尺寸的 Gizmos 颜色
    private static readonly Color _color1x1 = new Color(1.00f, 0.92f, 0.22f, 0.90f); // 黄
    private static readonly Color _color2x2 = new Color(1.00f, 0.55f, 0.10f, 0.90f); // 橙
    private static readonly Color _color3x3 = new Color(0.90f, 0.15f, 0.15f, 0.90f); // 红

    private void OnDrawGizmos()
    {
        if (!ShowGizmos) return;

        Vector3 cellSize = GroundTilemap != null ? GroundTilemap.cellSize : Vector3.one;
        Vector3 origin   = GroundTilemap != null ? GroundTilemap.CellToWorld(Vector3Int.zero) : transform.position;

        // 地图整体边界（白色框，始终可见）
        Vector3 mapWorldSize = new Vector3(cellSize.x * Width, cellSize.y * Height, 0f);
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(origin + mapWorldSize * 0.5f, mapWorldSize);

        if (_noiseMap == null) return;

        Vector3 cubeSize = new Vector3(cellSize.x * 0.96f, cellSize.y * 0.96f, 0.01f);

        // 地形底色
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float v = _noiseMap[x, y];
                Vector3 center = origin + new Vector3((x + 0.5f) * cellSize.x, (y + 0.5f) * cellSize.y, 0f);

                if (v < SandThreshold)
                    Gizmos.color = new Color(SandColor.r,  SandColor.g,  SandColor.b,  0.5f);
                else if (v < MountainThreshold)
                    Gizmos.color = new Color(GrassColor.r, GrassColor.g, GrassColor.b, 0.5f);
                else
                    Gizmos.color = new Color(DirtColor.r,  DirtColor.g,  DirtColor.b,  0.5f);

                Gizmos.DrawCube(center, cubeSize);
            }
        }

        if (_mountainOrigins == null) return;

        // 用记录的起点列表直接绘制山体轮廓，不受相邻同尺寸山体干扰
        foreach (var (mx, my, size) in _mountainOrigins)
        {
            Color c = size == 3 ? _color3x3 : size == 2 ? _color2x2 : _color1x1;

            Vector3 fillCenter = origin + new Vector3(
                (mx + size * 0.5f) * cellSize.x,
                (my + size * 0.5f) * cellSize.y, 0f);

            // 半透明填充
            Gizmos.color = new Color(c.r, c.g, c.b, 0.25f);
            Gizmos.DrawCube(fillCenter, new Vector3(cellSize.x * size * 0.94f, cellSize.y * size * 0.94f, 0.01f));

            // 整体轮廓框
            Gizmos.color = c;
            Gizmos.DrawWireCube(fillCenter, new Vector3(cellSize.x * size, cellSize.y * size, 0.01f));
        }
    }
}
