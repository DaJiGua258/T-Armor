using UnityEngine;
using UnityEngine.Tilemaps;

public static class TileResolveService
{
    /// <summary>
    /// 根据层配置和默认TileSet解析TileSet
    /// </summary>
    public static TileSet ResolveTileSet(LayerConfig cfg, TileSet fallback)
    {
        var ts = cfg.tileSet;
        if (ts != null && ts.borderTiles != null && ts.borderTiles.Length == 12)
            return ts;
        return fallback;
    }

    /// <summary>
    /// 根据TileSet和索引获取边界Tile
    /// </summary>
    public static TileBase Border(TileSet ts, int index)
    {
        if (ts?.borderTiles == null || index < 0 || index >= ts.borderTiles.Length)
            return null;
        return ts.borderTiles[index];
    }

    /// <summary>
    /// 根据TileSet和坐标获取FullTile
    /// </summary>
    public static TileBase PickFull(TileSet ts, int x, int y)
    {
        if (ts?.fullVariants == null || ts.fullVariants.Length == 0) 
        {
            return null;
        }

        if (ts.fullVariants.Length == 1) 
        {
            return ts.fullVariants[0];
        }

        // 随机选择一个FullTile
        int idx = Mathf.Abs((x * 73856093) ^ (y * 19349663)) % ts.fullVariants.Length;

        return ts.fullVariants[idx];
    }

    /// <summary>
    /// 根据网格数据和坐标计算Tile索引
    /// </summary>
    public static int ResolveIndex(CellData[,] grid, int mapSize, int x, int y, int layerIdx)
    {
        // 判断当前格子周围各自的状态
        bool top = InLayerOrLower(grid, mapSize, x, y + 1, layerIdx);
        bool bottom = InLayerOrLower(grid, mapSize, x, y - 1, layerIdx);
        bool left = InLayerOrLower(grid, mapSize, x - 1, y, layerIdx);
        bool right = InLayerOrLower(grid, mapSize, x + 1, y, layerIdx);

        
        if (top && bottom && left && right)
        {
            bool topLeft = InLayerOrLower(grid, mapSize, x - 1, y + 1, layerIdx);
            bool topRight = InLayerOrLower(grid, mapSize, x + 1, y + 1, layerIdx);
            bool bottomLeft = InLayerOrLower(grid, mapSize, x - 1, y - 1, layerIdx);
            bool bottomRight = InLayerOrLower(grid, mapSize, x + 1, y - 1, layerIdx);

            if (!topLeft) return 11;     // INNER_BL
            if (!topRight) return 10;    // INNER_BR
            if (!bottomLeft) return 9;   // INNER_TL
            if (!bottomRight) return 8;  // INNER_TR

            return CellData.TILE_FULL;
        }

        if (!top && bottom && left && right) return 0; // EDGE_T
        if (top && !bottom && left && right) return 1; // EDGE_B
        if (top && bottom && left && !right) return 3; // EDGE_L
        if (top && bottom && !left && right) return 2; // EDGE_R

        if (!top && !right && bottom && left) return 5;   // OUTER_TL
        if (!top && !left && bottom && right) return 4;   // OUTER_TR
        if (!bottom && !right && top && left) return 7;   // OUTER_BL
        if (!bottom && !left && top && right) return 6;   // OUTER_BR

        return CellData.TILE_FULL;
    }

    /// <summary>
    /// 判断当前格子是否在指定层或更低层
    /// </summary>
    public static bool InLayerOrLower(CellData[,] grid, int mapSize, int x, int y, int layerIdx)
    {
        // 如果网格数据为空，则返回false
        if (grid == null)
        {
            return false;
        }
        
        // 如果坐标超出范围，则返回false
        if (x < 0 || x >= mapSize || y < 0 || y >= mapSize)
        {
            return false;
        }

        // 获取当前格子的层索引
        int l = grid[x, y].layerIndex;
        if(l >= 0 && l <= layerIdx)
        {
            return true;
        }

        return false;
    }
}
