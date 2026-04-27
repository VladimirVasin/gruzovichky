using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct BuildingPlacementPreview
{
    public readonly Vector2Int Min;
    public readonly Vector2Int Max;
    public readonly Vector3 Scale;

    public BuildingPlacementPreview(Vector2Int min, Vector2Int max, Vector3 scale)
    {
        Min = min;
        Max = max;
        Scale = scale;
    }
}

public static class BuildingPlacementService
{
    public static void GetRotatedFootprint(
        Vector2Int anchorCell,
        int width,
        int depth,
        int rotationIndex,
        out Vector2Int min,
        out Vector2Int max)
    {
        int left = width / 2;
        int right = width - left - 1;
        switch (rotationIndex % 4)
        {
            case 1:
                min = new Vector2Int(anchorCell.x + 1, anchorCell.y - left);
                max = new Vector2Int(anchorCell.x + depth, anchorCell.y + right);
                break;
            case 2:
                min = new Vector2Int(anchorCell.x - left, anchorCell.y - depth);
                max = new Vector2Int(anchorCell.x + right, anchorCell.y - 1);
                break;
            case 3:
                min = new Vector2Int(anchorCell.x - depth, anchorCell.y - left);
                max = new Vector2Int(anchorCell.x - 1, anchorCell.y + right);
                break;
            default:
                min = new Vector2Int(anchorCell.x - left, anchorCell.y + 1);
                max = new Vector2Int(anchorCell.x + right, anchorCell.y + depth);
                break;
        }
    }

    public static bool IsFootprintClear(
        Vector2Int anchorCell,
        Vector2Int min,
        Vector2Int max,
        Func<Vector2Int, bool> isInsideGrid,
        Func<Vector2Int, bool> isBlockedCell)
    {
        if (isInsideGrid == null || !isInsideGrid(anchorCell) || !isInsideGrid(min) || !isInsideGrid(max))
        {
            return false;
        }

        if (isBlockedCell != null && isBlockedCell(anchorCell))
        {
            return false;
        }

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector2Int cell = new(x, y);
                if (!isInsideGrid(cell) || (isBlockedCell != null && isBlockedCell(cell)))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static void FillFootprintCells(List<Vector2Int> target, Vector2Int min, Vector2Int max)
    {
        if (target == null)
        {
            return;
        }

        target.Clear();
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                target.Add(new Vector2Int(x, y));
            }
        }
    }

    public static BuildingPlacementPreview CreatePreview(Vector2Int min, Vector2Int max)
    {
        return new BuildingPlacementPreview(
            min,
            max,
            new Vector3((max.x - min.x + 1) * 0.94f, 0.04f, (max.y - min.y + 1) * 0.94f));
    }

    public static Vector2 GetFootprintCenter(Vector2Int min, Vector2Int max)
    {
        return new Vector2((min.x + max.x + 1) * 0.5f, (min.y + max.y + 1) * 0.5f);
    }
}
