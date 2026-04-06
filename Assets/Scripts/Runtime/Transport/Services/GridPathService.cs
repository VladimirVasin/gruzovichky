using System;
using System.Collections.Generic;
using UnityEngine;

public static class GridPathService
{
    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal, Func<Vector2Int, IEnumerable<Vector2Int>> getNeighbors, Func<Vector2Int, bool> isWalkable)
    {
        if (start == goal)
        {
            return new List<Vector2Int> { start };
        }

        Queue<Vector2Int> frontier = new();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        frontier.Enqueue(start);
        cameFrom[start] = start;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            foreach (Vector2Int neighbor in getNeighbors(current))
            {
                if (cameFrom.ContainsKey(neighbor) || !isWalkable(neighbor))
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                if (neighbor == goal)
                {
                    return ReconstructPath(cameFrom, start, goal);
                }

                frontier.Enqueue(neighbor);
            }
        }

        return null;
    }

    public static IEnumerable<Vector2Int> GetCardinalNeighbors(Vector2Int cell)
    {
        yield return new Vector2Int(cell.x + 1, cell.y);
        yield return new Vector2Int(cell.x - 1, cell.y);
        yield return new Vector2Int(cell.x, cell.y + 1);
        yield return new Vector2Int(cell.x, cell.y - 1);
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> path = new() { goal };
        Vector2Int current = goal;
        while (current != start)
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
