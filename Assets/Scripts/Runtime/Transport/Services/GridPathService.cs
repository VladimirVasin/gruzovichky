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

    public static List<Vector2Int> FindWeightedPath(
        Vector2Int start,
        Vector2Int goal,
        Func<Vector2Int, IEnumerable<Vector2Int>> getNeighbors,
        Func<Vector2Int, bool> isWalkable,
        Func<Vector2Int, float> getEnterCost)
    {
        if (start == goal)
        {
            return new List<Vector2Int> { start };
        }

        List<Vector2Int> frontier = new() { start };
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        Dictionary<Vector2Int, float> costSoFar = new();
        cameFrom[start] = start;
        costSoFar[start] = 0f;

        while (frontier.Count > 0)
        {
            Vector2Int current = DequeueLowestCost(frontier, costSoFar, goal);
            if (current == goal)
            {
                return ReconstructPath(cameFrom, start, goal);
            }

            foreach (Vector2Int neighbor in getNeighbors(current))
            {
                if (!isWalkable(neighbor))
                {
                    continue;
                }

                float enterCost = Mathf.Max(0.05f, getEnterCost(neighbor));
                float newCost = costSoFar[current] + enterCost;
                if (costSoFar.TryGetValue(neighbor, out float oldCost) && newCost >= oldCost - 0.0001f)
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                costSoFar[neighbor] = newCost;
                if (!frontier.Contains(neighbor))
                {
                    frontier.Add(neighbor);
                }
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

    private static Vector2Int DequeueLowestCost(List<Vector2Int> frontier, Dictionary<Vector2Int, float> costSoFar, Vector2Int goal)
    {
        int bestIndex = 0;
        float bestCost = float.PositiveInfinity;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < frontier.Count; i++)
        {
            Vector2Int cell = frontier[i];
            float cost = costSoFar[cell];
            int distance = Mathf.Abs(cell.x - goal.x) + Mathf.Abs(cell.y - goal.y);
            if (cost < bestCost - 0.0001f || (Mathf.Abs(cost - bestCost) <= 0.0001f && distance < bestDistance))
            {
                bestCost = cost;
                bestDistance = distance;
                bestIndex = i;
            }
        }

        Vector2Int current = frontier[bestIndex];
        frontier.RemoveAt(bestIndex);
        return current;
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

