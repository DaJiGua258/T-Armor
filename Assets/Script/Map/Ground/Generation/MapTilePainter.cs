using UnityEngine;
using UnityEngine.Tilemaps;

public static class MapTilePainter
{
    public static void Paint(CellData[,] grid, LayerConfig[] layers, int mapSize, TileSet defaultTileSet)
    {
        if (grid == null || layers == null) return;

        for (int l = 0; l < layers.Length; l++)
        {
            LayerConfig cfg = layers[l];
            if (cfg.tilemap == null) continue;
            bool isBottomLayer = l == layers.Length - 1;

            cfg.tilemap.color = cfg.tint;
            TileSet ts = TileResolveService.ResolveTileSet(cfg, defaultTileSet);

            var tilemapRenderer = cfg.tilemap.GetComponent<TilemapRenderer>();
            if (tilemapRenderer != null) tilemapRenderer.sortingOrder = -l;

            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    if (isBottomLayer)
                    {
                        TileBase bottomTile = TileResolveService.PickFull(ts, x, y);
                        if (bottomTile != null) cfg.tilemap.SetTile(new Vector3Int(x, y, 0), bottomTile);
                        continue;
                    }

                    int cellLayer = grid[x, y].layerIndex;
                    if (cellLayer < 0 || cellLayer > l) continue;

                    TileBase tile;
                    if (cellLayer < l)
                    {
                        tile = TileResolveService.PickFull(ts, x, y);
                    }
                    else
                    {
                        int ti = grid[x, y].tileIndex;
                        tile = ti == CellData.TILE_FULL ? TileResolveService.PickFull(ts, x, y) : TileResolveService.Border(ts, ti);
                    }

                    if (tile != null) cfg.tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }
    }
}
