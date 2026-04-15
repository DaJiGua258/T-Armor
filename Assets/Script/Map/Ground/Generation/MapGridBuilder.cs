using UnityEngine;

public struct MapGridBuildInput
{
    public int mapSize;
    public NoiseSettings noise;
    public FalloffSettings falloff;
    public TerrainSettings terrain;
}

public static class MapGridBuilder
{
    public static CellData[,] Build(MapGridBuildInput input)
    {
        var grid = BuildNoiseGrid(input);
        AssignLayers(grid, input.mapSize, input.terrain.layers);

        if (input.terrain.enableShapePostProcess)
            PostProcessLayers(grid, input.mapSize, input.terrain.layers, input.terrain.postProcessIterations);

        ComputeTileIndices(grid, input.mapSize);
        return grid;
    }

    private static CellData[,] BuildNoiseGrid(MapGridBuildInput input)
    {
        float[,] noise = NoiseUtility.GenerateNoiseMap(
            input.mapSize,
            input.mapSize,
            input.noise.noiseScale,
            input.noise.octaves,
            input.noise.persistence,
            input.noise.lacunarity,
            input.noise.seed,
            input.noise.noiseOffset);

        if (input.falloff.useFalloff)
        {
            float[,] falloff = NoiseUtility.GenerateFalloffMap(input.mapSize, input.mapSize, input.falloff.curve);
            noise = NoiseUtility.ApplyFalloff(noise, falloff);
        }

        var grid = new CellData[input.mapSize, input.mapSize];
        for (int y = 0; y < input.mapSize; y++)
        for (int x = 0; x < input.mapSize; x++)
            grid[x, y] = CellData.Create(noise[x, y]);

        return grid;
    }

    private static void AssignLayers(CellData[,] grid, int mapSize, LayerConfig[] layers)
    {
        if (layers == null) return;

        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                float n = grid[x, y].noise;
                grid[x, y].layerIndex = CellData.LAYER_NONE;

                for (int l = 0; l < layers.Length; l++)
                {
                    if (n <= layers[l].threshold)
                    {
                        grid[x, y].layerIndex = l;
                        break;
                    }
                }
            }
        }
    }

    private static void PostProcessLayers(
        CellData[,] grid,
        int mapSize,
        LayerConfig[] layers,
        int postProcessIterations)
    {
        if (layers == null) return;

        for (int iter = 0; iter < postProcessIterations; iter++)
        {
            bool changed = false;
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    int l = grid[x, y].layerIndex;
                    if (l < 0) continue;

                    bool top = TileResolveService.InLayerOrLower(grid, mapSize, x, y + 1, l);
                    bool bottom = TileResolveService.InLayerOrLower(grid, mapSize, x, y - 1, l);
                    bool left = TileResolveService.InLayerOrLower(grid, mapSize, x - 1, y, l);
                    bool right = TileResolveService.InLayerOrLower(grid, mapSize, x + 1, y, l);

                    int missing = (top ? 0 : 1) + (bottom ? 0 : 1) + (left ? 0 : 1) + (right ? 0 : 1);
                    bool invalid = missing >= 3
                                   || (!top && !bottom)
                                   || (!left && !right);

                    if (!invalid) continue;
                    grid[x, y].layerIndex = (l + 1 < layers.Length) ? l + 1 : CellData.LAYER_NONE;
                    changed = true;
                }
            }

            if (!changed) break;
        }
    }

    private static void ComputeTileIndices(CellData[,] grid, int mapSize)
    {
        for (int y = 0; y < mapSize; y++)
        {
            for (int x = 0; x < mapSize; x++)
            {
                int l = grid[x, y].layerIndex;
                if(l < 0)
                {
                    grid[x, y].tileIndex = CellData.TILE_FULL;
                }
                else
                {
                    grid[x, y].tileIndex = TileResolveService.ResolveIndex(grid, mapSize, x, y, l);
                }
            }
        }
    }
}
