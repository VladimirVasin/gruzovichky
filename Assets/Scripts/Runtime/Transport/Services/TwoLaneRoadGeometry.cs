using System.Collections.Generic;
using UnityEngine;

public static class TwoLaneRoadGeometry
{
    public static Vector2Int NormalizeDirection(Vector2Int direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x >= 0 ? Vector2Int.right : Vector2Int.left;
        }

        if (direction.y != 0)
        {
            return direction.y >= 0 ? Vector2Int.up : Vector2Int.down;
        }

        return Vector2Int.up;
    }

    public static Vector2Int GetRightLaneOffset(Vector2Int roadDirection)
    {
        Vector2Int dir = NormalizeDirection(roadDirection);
        return new Vector2Int(dir.y, -dir.x);
    }

    public static IReadOnlyList<Vector2Int> GetFootprintCells(Vector2Int primaryCell, Vector2Int roadDirection)
    {
        Vector2Int offset = GetRightLaneOffset(roadDirection);
        return new[] { primaryCell, primaryCell + offset };
    }

    public static void GetTurnFillBounds(
        Vector2Int previousCell,
        Vector2Int previousDirection,
        Vector2Int currentCell,
        Vector2Int currentDirection,
        out int minX,
        out int maxX,
        out int minY,
        out int maxY)
    {
        Vector2Int previousOffset = GetRightLaneOffset(previousDirection);
        Vector2Int currentOffset = GetRightLaneOffset(currentDirection);
        Vector2Int previousSide = previousCell + previousOffset;
        Vector2Int currentSide = currentCell + currentOffset;

        minX = Mathf.Min(Mathf.Min(previousCell.x, previousSide.x), Mathf.Min(currentCell.x, currentSide.x));
        maxX = Mathf.Max(Mathf.Max(previousCell.x, previousSide.x), Mathf.Max(currentCell.x, currentSide.x));
        minY = Mathf.Min(Mathf.Min(previousCell.y, previousSide.y), Mathf.Min(currentCell.y, currentSide.y));
        maxY = Mathf.Max(Mathf.Max(previousCell.y, previousSide.y), Mathf.Max(currentCell.y, currentSide.y));
    }
}
