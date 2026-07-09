using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 一套 Tile 资源：FULL 变体（支持多张随机）+ 12 种固定边缘/转角。
/// borderTiles 索引顺序：
///   0  EDGE_T   1  EDGE_B   2  EDGE_L   3  EDGE_R
///   4  OUTER_TL 5  OUTER_TR 6  OUTER_BL 7  OUTER_BR
///   8  INNER_TL 9  INNER_TR 10 INNER_BL 11 INNER_BR
/// </summary>
[Serializable]
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
[Serializable]
public class LayerConfig
{
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

/// <summary>
/// 地图单个格子的完整信息。
/// tileIndex: TILE_FULL(-1) = FULL 变体, 0-11 = borderTiles 索引。
/// </summary>
public struct CellData
{
    public const int LAYER_NONE = -1;
    public const int LAYER_OBSTACLE = -2;
    public const int TILE_FULL = -1;

    public float noise;
    public int layerIndex;
    public int tileIndex;
    public bool occupied;
    public CellFlags flags;

    public static CellData Create(float noise = 0f, int layer = LAYER_NONE)
    {
        return new CellData
        {
            noise = noise,
            layerIndex = layer,
            tileIndex = TILE_FULL,
            occupied = false,
            flags = CellFlags.None
            
        };
    }
}

[Flags]
public enum CellFlags : byte
{
    None = 0,
    Walkable = 1 << 0
}

public struct ObstaclePlacement
{
    public int originX;
    public int originY;
    public Vector3 center;
    public int size;
}
