using UnityEngine;
using UnityEngine.Tilemaps;

public partial class MapGenerator
{
    private void OnDrawGizmos()
    {
        EnsureSettingsObjects();
        if (!gizmo.showGizmos) return;

        Tilemap tm = GetReferenceTilemap();
        Vector2 cs = tm != null ? (Vector2)tm.cellSize : Vector2.one;

        Matrix4x4 prevMatrix = Gizmos.matrix;
        Transform drawBase = tm != null ? tm.transform : transform;
        Gizmos.matrix = drawBase.localToWorldMatrix;

        Vector3 localOrigin = tm != null
            ? (Vector3)tm.CellToLocal(Vector3Int.zero)
            : Vector3.zero;
        Vector3 boxCenter = localOrigin + new Vector3(mapSize * cs.x * 0.5f, mapSize * cs.y * 0.5f, 0f);
        Vector3 boxSize = new Vector3(mapSize * cs.x, mapSize * cs.y, 0.01f);

        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireCube(boxCenter, boxSize);

        Vector3 cubeSize = new Vector3(cs.x * 0.88f, cs.y * 0.88f, 0.01f);

        if (gizmo.showTerrainGizmos && _grid != null && terrain.layers != null)
        {
            float obstacleNoiseThreshold = GetObstacleNoiseThreshold();
            for (int y = 0; y < mapSize; y++)
            {
                for (int x = 0; x < mapSize; x++)
                {
                    int l = _grid[x, y].layerIndex;
                    Vector3 cellLocal = CellCenterLocal(x, y, 1, tm, cs);

                    if (l >= 0 && l < terrain.layers.Length)
                    {
                        Color c = terrain.layers[l].tint;
                        c.a = 1f;
                        Gizmos.color = c;
                        Gizmos.DrawCube(cellLocal, cubeSize);
                    }
                    else if (gizmo.showObstacleZoneGizmos
                             && _grid[x, y].noise > obstacleNoiseThreshold)
                    {
                        Gizmos.color = new Color(0.65f, 0.15f, 0.85f, 1f);
                        Gizmos.DrawCube(cellLocal, cubeSize);
                    }
                }
            }
        }

        if (gizmo.showObstacleGizmos && _gizmoObstacles != null)
        {
            foreach (ObstaclePlacement g in _gizmoObstacles)
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

    private Color GetObstacleGizmoColor(int size)
    {
        switch (size)
        {
            case 3: return new Color(0.5f, 0.86f, 1f, 1f);
            case 2: return new Color(1f, 0.86f, 0f, 1f);
            case 1: return new Color(1f, 0.25f, 0.21f, 1f);
            default: return Color.white;
        }
    }
}
