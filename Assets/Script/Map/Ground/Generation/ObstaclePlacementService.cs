using System.Collections.Generic;

public static class ObstaclePlacementService
{
    public static List<ObstaclePlacement> CalculatePlacements(
        CellData[,] grid,
        int mapSize,
        float obstacleNoiseThreshold,
        int[] prioritySizes)
    {
        var placements = new List<ObstaclePlacement>();
        if (grid == null || prioritySizes == null || prioritySizes.Length == 0) return placements;

        ResetOccupied(grid, mapSize);

        foreach (int size in prioritySizes)
        {
            for (int y = 0; y <= mapSize - size; y++)
            {
                for (int x = 0; x <= mapSize - size; x++)
                {
                    if (!CanPlace(grid, mapSize, obstacleNoiseThreshold, x, y, size)) continue;
                    MarkOccupied(grid, x, y, size);
                    placements.Add(new ObstaclePlacement
                    {
                        originX = x,
                        originY = y,
                        size = size
                    });
                }
            }
        }

        return placements;
    }

    public static bool CanPlace(CellData[,] grid, int mapSize, float obstacleNoiseThreshold, int ox, int oy, int size)
    {
        for (int dy = 0; dy < size; dy++)
        {
            for (int dx = 0; dx < size; dx++)
            {
                int cx = ox + dx;
                int cy = oy + dy;
                if (cx >= mapSize || cy >= mapSize) return false;
                if (grid[cx, cy].layerIndex != CellData.LAYER_NONE) return false;
                if (grid[cx, cy].noise <= obstacleNoiseThreshold) return false;
                if (grid[cx, cy].occupied) return false;
            }
        }

        return true;
    }

    public static void MarkOccupied(CellData[,] grid, int ox, int oy, int size)
    {
        for (int dy = 0; dy < size; dy++)
        for (int dx = 0; dx < size; dx++)
            grid[ox + dx, oy + dy].occupied = true;
    }

    public static void ResetOccupied(CellData[,] grid, int mapSize)
    {
        for (int y = 0; y < mapSize; y++)
        for (int x = 0; x < mapSize; x++)
            grid[x, y].occupied = false;
    }
}
