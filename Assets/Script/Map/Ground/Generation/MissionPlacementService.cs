using System.Collections.Generic;
using QFramework.ViewController.Mission;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class MissionPlacementService
{
    public static Vector2Int ComputeFootprint(Vector2 areaSize, float cellWidth, float cellHeight, int extraMarginCells)
    {
        float safeCellWidth = Mathf.Max(0.0001f, Mathf.Abs(cellWidth));
        float safeCellHeight = Mathf.Max(0.0001f, Mathf.Abs(cellHeight));
        int width = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(areaSize.x) / safeCellWidth));
        int height = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(areaSize.y) / safeCellHeight));
        int margin = Mathf.Max(0, extraMarginCells);
        return new Vector2Int(width + margin * 2, height + margin * 2);
    }

    public static bool TryFindPlacement(CellData[,] grid, int mapSize, Vector2Int footprint, bool preferLowestNoise, out Vector2Int origin)
    {
        origin = new Vector2Int(-1, -1);
        int width = Mathf.Max(1, footprint.x);
        int height = Mathf.Max(1, footprint.y);

        bool found = false;
        float bestNoise = float.MaxValue;
        Vector2Int bestOrigin = origin;

        for (int y = 0; y <= mapSize - height; y++)
        {
            for (int x = 0; x <= mapSize - width; x++)
            {
                if (!CanPlaceAt(grid, mapSize, x, y, width, height))
                {
                    continue;
                }

                if (!preferLowestNoise)
                {
                    origin = new Vector2Int(x, y);
                    return true;
                }

                float avgNoise = GetAverageNoise(grid, x, y, width, height);
                if (!found || avgNoise < bestNoise)
                {
                    found = true;
                    bestNoise = avgNoise;
                    bestOrigin = new Vector2Int(x, y);
                }
            }
        }

        if (!found)
        {
            return false;
        }

        origin = bestOrigin;
        return true;
    }

    public static bool CanPlaceAt(CellData[,] grid, int mapSize, int originX, int originY, int width, int height)
    {
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                int x = originX + dx;
                int y = originY + dy;
                if (x < 0 || y < 0 || x >= mapSize || y >= mapSize)
                {
                    return false;
                }

                CellData cell = grid[x, y];
                if (cell.layerIndex == CellData.LAYER_NONE)
                {
                    return false;
                }

                if (cell.occupied)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static float GetAverageNoise(CellData[,] grid, int originX, int originY, int width, int height)
    {
        float total = 0f;
        int count = 0;
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                total += grid[originX + dx, originY + dy].noise;
                count++;
            }
        }

        return count == 0 ? float.MaxValue : total / count;
    }

    public static void MarkOccupied(CellData[,] grid, int originX, int originY, int width, int height)
    {
        for (int dy = 0; dy < height; dy++)
        {
            for (int dx = 0; dx < width; dx++)
            {
                grid[originX + dx, originY + dy].occupied = true;
            }
        }
    }

    /// <summary>
    /// 带最小距离约束的放置查找。从 fullDistance 开始尝试，不满足时按 distanceStep 递减，直到 0。
    /// </summary>
    public static bool TryFindPlacement(
        CellData[,] grid, int mapSize, Vector2Int footprint, bool preferLowestNoise,
        List<Vector3> placedPositions, float minDistance, float distanceStep, Tilemap tilemap, out Vector2Int origin)
    {
        origin = new Vector2Int(-1, -1);

        if (placedPositions == null || placedPositions.Count == 0 || minDistance <= 0f)
        {
            return TryFindPlacement(grid, mapSize, footprint, preferLowestNoise, out origin);
        }

        float step = Mathf.Max(0.1f, distanceStep);

        for (float distance = minDistance; distance >= 0f; distance -= step)
        {
            if (TryFindPlacementWithDistance(grid, mapSize, footprint, preferLowestNoise,
                    placedPositions, distance, tilemap, out origin))
            {
                return true;
            }

            if (distance <= 0f) break;
        }

        // 最终降级：无距离约束
        return TryFindPlacement(grid, mapSize, footprint, preferLowestNoise, out origin);
    }

    private static bool TryFindPlacementWithDistance(
        CellData[,] grid, int mapSize, Vector2Int footprint, bool preferLowestNoise,
        List<Vector3> placedPositions, float minDistance, Tilemap tilemap, out Vector2Int origin)
    {
        origin = new Vector2Int(-1, -1);
        int width = Mathf.Max(1, footprint.x);
        int height = Mathf.Max(1, footprint.y);

        bool found = false;
        float bestNoise = float.MaxValue;
        Vector2Int bestOrigin = origin;
        float sqrMinDist = minDistance * minDistance;

        for (int y = 0; y <= mapSize - height; y++)
        {
            for (int x = 0; x <= mapSize - width; x++)
            {
                if (!CanPlaceAt(grid, mapSize, x, y, width, height))
                    continue;

                Vector3 candidateCenter = CalculateCellRectCenter(x, y, width, height, tilemap);

                // 检查与已放置任务的距离
                bool tooClose = false;
                for (int i = 0; i < placedPositions.Count; i++)
                {
                    if ((placedPositions[i] - candidateCenter).sqrMagnitude <= sqrMinDist)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose)
                    continue;

                if (!preferLowestNoise)
                {
                    origin = new Vector2Int(x, y);
                    return true;
                }

                float avgNoise = GetAverageNoise(grid, x, y, width, height);
                if (!found || avgNoise < bestNoise)
                {
                    found = true;
                    bestNoise = avgNoise;
                    bestOrigin = new Vector2Int(x, y);
                }
            }
        }

        if (!found)
            return false;

        origin = bestOrigin;
        return true;
    }

    private static Vector3 CalculateCellRectCenter(int x, int y, int width, int height, Tilemap tilemap)
    {
        if (tilemap != null)
        {
            Vector3 localCenter = tilemap.GetCellCenterLocal(new Vector3Int(x, y, 0));
            localCenter.x += (width - 1) * 0.5f * tilemap.cellSize.x;
            localCenter.y += (height - 1) * 0.5f * tilemap.cellSize.y;
            return tilemap.transform.TransformPoint(localCenter);
        }

        return new Vector3(x + width * 0.5f, y + height * 0.5f, 0f);
    }

    public static List<Vector3> CollectPositions(Transform missionParent)
    {
        var positions = new List<Vector3>();
        if (missionParent == null) return positions;

        for (int i = 0; i < missionParent.childCount; i++)
        {
            Transform child = missionParent.GetChild(i);
            if (child.GetComponent<AbstractMissionInstance>() == null) continue;
            positions.Add(child.position);
        }

        return positions;
    }

    public static bool IsNearPosition(Vector3 worldPosition, List<Vector3> positions, float radius)
    {
        if (positions == null || positions.Count == 0) return false;

        float radiusSqr = radius * radius;
        for (int i = 0; i < positions.Count; i++)
        {
            Vector3 delta = positions[i] - worldPosition;
            if (delta.sqrMagnitude <= radiusSqr) return true;
        }

        return false;
    }
}
