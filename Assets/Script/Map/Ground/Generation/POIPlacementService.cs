using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class POIPlacementService
{
    /// <summary>
    /// 扫描全网格，对每个可行空位按分散度评分，返回最优位置。
    /// 评分 = 到最近 mission 的距离 + 到最近已放置 POI 的距离（越高越好）。
    /// 硬约束：avoidMissionRadius / minPOIDistance 以内的位置直接淘汰。
    /// </summary>
    public static bool TryFindBestPlacement(
        CellData[,] grid, int mapSize, Vector2Int footprint,
        List<Vector3> missionPositions, List<Vector3> poiPositions,
        float avoidMissionRadius, float minPOIDistance,
        Tilemap tilemap,
        out Vector2Int origin)
    {
        origin = Vector2Int.zero;
        int width = Mathf.Max(1, footprint.x);
        int height = Mathf.Max(1, footprint.y);

        bool found = false;
        float bestScore = float.MinValue;
        Vector2Int bestOrigin = Vector2Int.zero;

        for (int y = 0; y <= mapSize - height; y++)
        {
            for (int x = 0; x <= mapSize - width; x++)
            {
                if (!MissionPlacementService.CanPlaceAt(grid, mapSize, x, y, width, height))
                    continue;

                Vector3 centerPos = CalculateCellRectCenter(x, y, width, height, tilemap);

                if (avoidMissionRadius > 0f && !IsFarEnough(centerPos, missionPositions, avoidMissionRadius))
                    continue;

                if (minPOIDistance > 0f && !IsFarEnough(centerPos, poiPositions, minPOIDistance))
                    continue;

                float nearestMissionDist = NearestDistance(centerPos, missionPositions);
                float nearestPOIDist = NearestDistance(centerPos, poiPositions);
                float score = nearestMissionDist + nearestPOIDist;

                if (!found || score > bestScore)
                {
                    found = true;
                    bestScore = score;
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

    private static bool IsFarEnough(Vector3 position, List<Vector3> targets, float minDistance)
    {
        if (targets == null || targets.Count == 0)
            return true;

        float sqrMin = minDistance * minDistance;
        for (int i = 0; i < targets.Count; i++)
        {
            if ((targets[i] - position).sqrMagnitude <= sqrMin)
                return false;
        }

        return true;
    }

    private static float NearestDistance(Vector3 position, List<Vector3> targets)
    {
        if (targets == null || targets.Count == 0)
            return float.MaxValue;

        float nearestSqr = float.MaxValue;
        for (int i = 0; i < targets.Count; i++)
        {
            float sqrDist = (targets[i] - position).sqrMagnitude;
            if (sqrDist < nearestSqr)
                nearestSqr = sqrDist;
        }

        return Mathf.Sqrt(nearestSqr);
    }
}
