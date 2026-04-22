using System.Collections.Generic;
using UnityEngine;

public static class MiscTreePlanner
{
    private const float MiscTargetDensity = 0.22f;
    private const int BaseMinMiscCount = 18;
    private const float AreaPerMiscCapCell = 48f;

    public static List<Vector2Int> Plan(
        int gridWidth,
        int gridHeight,
        HashSet<Vector2Int> roadCells,
        HashSet<Vector2Int> blockedDecorationCells,
        System.Func<Vector2Int, bool> isLocationCell,
        System.Func<Vector2Int, bool> isAllowedGroundCell,
        System.Func<Vector2Int, float> getCellPriority = null)
    {
        List<Vector2Int> candidates = new();
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (IsCellAvailable(cell, gridWidth, gridHeight, roadCells, blockedDecorationCells, isLocationCell, isAllowedGroundCell))
                {
                    candidates.Add(cell);
                }
            }
        }

        Shuffle(candidates);
        if (getCellPriority != null)
        {
            candidates.Sort((a, b) => getCellPriority(b).CompareTo(getCellPriority(a)));
        }

        List<Vector2Int> plannedCells = new();
        int dynamicMaxCount = Mathf.Max(BaseMinMiscCount, Mathf.RoundToInt((gridWidth * gridHeight) / AreaPerMiscCapCell));
        int targetCount = Mathf.Clamp(Mathf.RoundToInt(candidates.Count * MiscTargetDensity), BaseMinMiscCount, dynamicMaxCount);
        foreach (Vector2Int cell in candidates)
        {
            if (plannedCells.Count >= targetCount)
            {
                break;
            }

            bool tooClose = false;
            foreach (Vector2Int occupiedCell in plannedCells)
            {
                if (Mathf.Abs(occupiedCell.x - cell.x) <= 1 && Mathf.Abs(occupiedCell.y - cell.y) <= 1)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                plannedCells.Add(cell);
            }
        }

        return plannedCells;
    }

    private static bool IsCellAvailable(
        Vector2Int cell,
        int gridWidth,
        int gridHeight,
        HashSet<Vector2Int> roadCells,
        HashSet<Vector2Int> blockedDecorationCells,
        System.Func<Vector2Int, bool> isLocationCell,
        System.Func<Vector2Int, bool> isAllowedGroundCell)
    {
        if (!IsInsideGrid(cell, gridWidth, gridHeight) ||
            roadCells.Contains(cell) ||
            blockedDecorationCells.Contains(cell) ||
            isLocationCell(cell) ||
            !isAllowedGroundCell(cell))
        {
            return false;
        }

        foreach (Vector2Int neighbor in GetNeighbors(cell))
        {
            if (!IsInsideGrid(neighbor, gridWidth, gridHeight))
            {
                continue;
            }

            if (roadCells.Contains(neighbor) || blockedDecorationCells.Contains(neighbor) || isLocationCell(neighbor))
            {
                return false;
            }
        }

        return true;
    }

    private static void Shuffle(List<Vector2Int> cells)
    {
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (cells[i], cells[j]) = (cells[j], cells[i]);
        }
    }

    private static IEnumerable<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        yield return new Vector2Int(cell.x + 1, cell.y);
        yield return new Vector2Int(cell.x - 1, cell.y);
        yield return new Vector2Int(cell.x, cell.y + 1);
        yield return new Vector2Int(cell.x, cell.y - 1);
    }

    private static bool IsInsideGrid(Vector2Int cell, int gridWidth, int gridHeight)
    {
        return cell.x >= 0 && cell.x < gridWidth && cell.y >= 0 && cell.y < gridHeight;
    }
}
