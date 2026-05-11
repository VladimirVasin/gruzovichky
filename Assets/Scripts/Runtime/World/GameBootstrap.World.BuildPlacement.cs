using UnityEngine;

public partial class GameBootstrap
{
    private void PrepareBuildSiteForLocation(LocationType type, Vector2Int min, Vector2Int max)
    {
        int removedMiscCount = 0;
        int removedFootpathCount = ClearFootpathsInFootprint(min, max);
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                if (RemoveMiscObjectAtCell(new Vector2Int(x, y)))
                {
                    removedMiscCount++;
                }
            }
        }

        bool flattened = FlattenTerrainForBuildingFootprint(min, max, out float flatHeight);
        if (removedMiscCount > 0 || removedFootpathCount > 0 || flattened)
        {
            SessionDebugLogger.Log(
                "BUILD",
                $"Prepared build site for {type}: footprint=({min.x},{min.y})-({max.x},{max.y}), removedMisc={removedMiscCount}, removedFootpaths={removedFootpathCount}, flattened={flattened}, height={flatHeight:0.00}.");
        }
    }

    private bool FlattenTerrainForBuildingFootprint(Vector2Int min, Vector2Int max, out float flatHeight)
    {
        flatHeight = 0f;
        if (terrainHeights == null || groundRoot == null)
        {
            return false;
        }

        float total = 0f;
        int count = 0;
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector2Int cell = new(x, y);
                if (!IsInsideGrid(cell) || IsWaterOrBeachCell(cell) || edgeHighwayCells.Contains(cell))
                {
                    continue;
                }

                total += terrainHeights[x, y];
                count++;
            }
        }

        if (count == 0)
        {
            return false;
        }

        flatHeight = total / count;
        bool changed = false;
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector2Int cell = new(x, y);
                if (!IsInsideGrid(cell) || IsWaterOrBeachCell(cell) || edgeHighwayCells.Contains(cell))
                {
                    continue;
                }

                if (Mathf.Abs(terrainHeights[x, y] - flatHeight) > 0.001f)
                {
                    terrainHeights[x, y] = flatHeight;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            RefreshTerrainCellsAroundFootprint(min, max);
        }

        return changed;
    }

    private void RefreshTerrainCellsAroundFootprint(Vector2Int min, Vector2Int max)
    {
        for (int x = min.x - 1; x <= max.x + 1; x++)
        {
            for (int y = min.y - 1; y <= max.y + 1; y++)
            {
                RefreshTerrainCellVisual(new Vector2Int(x, y));
            }
        }
    }

    private void RefreshTerrainCellVisual(Vector2Int cell)
    {
        if (!IsInsideGrid(cell) || waterCells.Contains(cell) || groundRoot == null)
        {
            return;
        }

        Transform tile = groundRoot.Find($"Ground_{cell.x}_{cell.y}");
        if (tile == null || !tile.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
        {
            return;
        }

        float x0 = cell.x;
        float x1 = cell.x + 1f;
        float z0 = cell.y;
        float z1 = cell.y + 1f;
        const float lift = 0.01f;
        const float bottomY = -0.30f;

        Mesh mesh = filter.sharedMesh;
        mesh.vertices = new[]
        {
            new Vector3(x0, SampleTerrainHeight(x0, z0) + lift, z0),
            new Vector3(x1, SampleTerrainHeight(x1, z0) + lift, z0),
            new Vector3(x0, SampleTerrainHeight(x0, z1) + lift, z1),
            new Vector3(x1, SampleTerrainHeight(x1, z1) + lift, z1),
            new Vector3(x0, bottomY, z0),
            new Vector3(x1, bottomY, z0),
            new Vector3(x0, bottomY, z1),
            new Vector3(x1, bottomY, z1),
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}
