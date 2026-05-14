using System.Collections.Generic;
using UnityEngine;

public static class EnvironmentSpawnService
{
    public static List<Vector2> GeneratePoissonPoints(
        int mapSize,
        float radius,
        int maxSamplesPerPoint,
        int edgePaddingCells,
        int seed)
    {
        var points = new List<Vector2>();
        if (mapSize <= 0) return points;

        float minX = Mathf.Clamp(edgePaddingCells, 0, mapSize);
        float minY = minX;
        float maxX = Mathf.Clamp(mapSize - edgePaddingCells, 0, mapSize);
        float maxY = maxX;
        float width = maxX - minX;
        float height = maxY - minY;
        if (width <= 0f || height <= 0f) return points;

        float safeRadius = Mathf.Max(0.1f, radius);
        int safeMaxSamples = Mathf.Max(1, maxSamplesPerPoint);
        float cellSize = safeRadius / Mathf.Sqrt(2f);
        int gridWidth = Mathf.CeilToInt(width / cellSize);
        int gridHeight = Mathf.CeilToInt(height / cellSize);

        Vector2[,] grid = new Vector2[gridWidth, gridHeight];
        bool[,] hasPoint = new bool[gridWidth, gridHeight];
        var active = new List<Vector2>();
        var rng = new System.Random(seed);

        Vector2 first = new Vector2(
            minX + (float)rng.NextDouble() * width,
            minY + (float)rng.NextDouble() * height);

        points.Add(first);
        active.Add(first);
        InsertPoint(first, minX, minY, cellSize, grid, hasPoint);

        while (active.Count > 0)
        {
            int index = rng.Next(active.Count);
            Vector2 center = active[index];
            bool found = false;

            for (int i = 0; i < safeMaxSamples; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float distance = safeRadius * (1f + (float)rng.NextDouble());
                Vector2 candidate = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;

                if (candidate.x < minX || candidate.x >= maxX || candidate.y < minY || candidate.y >= maxY)
                    continue;
                if (!IsValidCandidate(candidate, safeRadius, minX, minY, cellSize, grid, hasPoint))
                    continue;

                points.Add(candidate);
                active.Add(candidate);
                InsertPoint(candidate, minX, minY, cellSize, grid, hasPoint);
                found = true;
                break;
            }

            if (!found)
                active.RemoveAt(index);
        }

        return points;
    }

    private static void InsertPoint(
        Vector2 point,
        float minX,
        float minY,
        float cellSize,
        Vector2[,] grid,
        bool[,] hasPoint)
    {
        int gx = Mathf.Clamp((int)((point.x - minX) / cellSize), 0, grid.GetLength(0) - 1);
        int gy = Mathf.Clamp((int)((point.y - minY) / cellSize), 0, grid.GetLength(1) - 1);
        grid[gx, gy] = point;
        hasPoint[gx, gy] = true;
    }

    private static bool IsValidCandidate(
        Vector2 candidate,
        float radius,
        float minX,
        float minY,
        float cellSize,
        Vector2[,] grid,
        bool[,] hasPoint)
    {
        int gx = Mathf.Clamp((int)((candidate.x - minX) / cellSize), 0, grid.GetLength(0) - 1);
        int gy = Mathf.Clamp((int)((candidate.y - minY) / cellSize), 0, grid.GetLength(1) - 1);
        int range = 2;

        int xMin = Mathf.Max(0, gx - range);
        int xMax = Mathf.Min(grid.GetLength(0) - 1, gx + range);
        int yMin = Mathf.Max(0, gy - range);
        int yMax = Mathf.Min(grid.GetLength(1) - 1, gy + range);
        float radiusSqr = radius * radius;

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                if (!hasPoint[x, y]) continue;
                Vector2 delta = grid[x, y] - candidate;
                if (delta.sqrMagnitude < radiusSqr) return false;
            }
        }

        return true;
    }

    public static EnvironmentPrefabRule PickRule(EnvironmentPrefabRule[] rules, float noiseValue, System.Random rng)
    {
        if (rules == null || rules.Length == 0) return null;

        float totalWeight = 0f;
        for (int i = 0; i < rules.Length; i++)
        {
            EnvironmentPrefabRule rule = rules[i];
            if (rule == null || rule.prefab == null) continue;
            if (noiseValue < rule.noiseMin || noiseValue > rule.noiseMax) continue;
            if (rule.weight <= 0f) continue;
            totalWeight += rule.weight;
        }

        if (totalWeight <= 0f) return null;

        float pick = (float)rng.NextDouble() * totalWeight;
        for (int i = 0; i < rules.Length; i++)
        {
            EnvironmentPrefabRule rule = rules[i];
            if (rule == null || rule.prefab == null) continue;
            if (noiseValue < rule.noiseMin || noiseValue > rule.noiseMax) continue;
            if (rule.weight <= 0f) continue;

            if (pick <= rule.weight) return rule;
            pick -= rule.weight;
        }

        return null;
    }

    public static bool IsFarEnoughFromOccupied(Vector2 candidate, CellData[,] grid, int mapSize, float paddingInCells)
    {
        int cx = Mathf.Clamp(Mathf.FloorToInt(candidate.x), 0, mapSize - 1);
        int cy = Mathf.Clamp(Mathf.FloorToInt(candidate.y), 0, mapSize - 1);
        if (paddingInCells <= 0f) return !grid[cx, cy].occupied;

        int searchRadius = Mathf.CeilToInt(paddingInCells);
        for (int y = Mathf.Max(0, cy - searchRadius); y <= Mathf.Min(mapSize - 1, cy + searchRadius); y++)
        {
            for (int x = Mathf.Max(0, cx - searchRadius); x <= Mathf.Min(mapSize - 1, cx + searchRadius); x++)
            {
                if (!grid[x, y].occupied) continue;
                Vector2 occupiedCenter = new Vector2(x + 0.5f, y + 0.5f);
                if ((occupiedCenter - candidate).sqrMagnitude <= paddingInCells * paddingInCells)
                    return false;
            }
        }

        return true;
    }
}
