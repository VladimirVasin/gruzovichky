using System.Collections.Generic;
using UnityEngine;

public static class RoadSegmentBuildService
{
    public static List<Vector2Int> BuildManhattanSegment(Vector2Int start, Vector2Int end, bool constrainToDominantAxis)
    {
        List<Vector2Int> path = new();
        path.Add(start);

        if (start == end)
        {
            return path;
        }

        Vector2Int delta = end - start;
        bool horizontalFirst = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);

        Vector2Int target = end;
        if (constrainToDominantAxis)
        {
            target = horizontalFirst
                ? new Vector2Int(end.x, start.y)
                : new Vector2Int(start.x, end.y);
        }

        if (horizontalFirst)
        {
            AddAxisCells(path, new Vector2Int(target.x, start.y));
            if (!constrainToDominantAxis)
            {
                AddAxisCells(path, target);
            }
        }
        else
        {
            AddAxisCells(path, new Vector2Int(start.x, target.y));
            if (!constrainToDominantAxis)
            {
                AddAxisCells(path, target);
            }
        }

        return path;
    }

    private static void AddAxisCells(List<Vector2Int> path, Vector2Int target)
    {
        if (path.Count == 0)
        {
            path.Add(target);
            return;
        }

        Vector2Int current = path[^1];
        while (current.x != target.x)
        {
            current.x += target.x > current.x ? 1 : -1;
            AddIfNew(path, current);
        }

        while (current.y != target.y)
        {
            current.y += target.y > current.y ? 1 : -1;
            AddIfNew(path, current);
        }
    }

    private static void AddIfNew(List<Vector2Int> path, Vector2Int cell)
    {
        if (path.Count == 0 || path[^1] != cell)
        {
            path.Add(cell);
        }
    }
}
