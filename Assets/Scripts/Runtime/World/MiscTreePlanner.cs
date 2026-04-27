using System.Collections.Generic;
using UnityEngine;

public static class MiscTreePlanner
{
    private const float MiscTargetDensity = 0.48f;
    private const int BaseMinMiscCount = 120;
    private const float AreaPerMiscCapCell = 13f;

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
                    candidates.Add(cell);
            }
        }

        Shuffle(candidates);
        if (getCellPriority != null)
            candidates.Sort((a, b) => getCellPriority(b).CompareTo(getCellPriority(a)));

        List<Vector2Int> plannedCells = new();
        // spatial set: blocked cells = placed cell + its 8 neighbors
        HashSet<Vector2Int> blockedByPlaced = new();

        int dynamicMaxCount = Mathf.Max(BaseMinMiscCount, Mathf.RoundToInt((gridWidth * gridHeight) / AreaPerMiscCapCell));
        int targetCount = Mathf.Clamp(Mathf.RoundToInt(candidates.Count * MiscTargetDensity), BaseMinMiscCount, dynamicMaxCount);

        foreach (Vector2Int cell in candidates)
        {
            if (plannedCells.Count >= targetCount) break;

            if (blockedByPlaced.Contains(cell)) continue;

            plannedCells.Add(cell);
            // mark the cell and its 8 neighbors as blocked for future candidates
            blockedByPlaced.Add(cell);
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (dx != 0 || dy != 0)
                    blockedByPlaced.Add(new Vector2Int(cell.x + dx, cell.y + dy));
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
        if (cell.x < 0 || cell.x >= gridWidth || cell.y < 0 || cell.y >= gridHeight ||
            roadCells.Contains(cell) ||
            blockedDecorationCells.Contains(cell) ||
            isLocationCell(cell) ||
            !isAllowedGroundCell(cell))
            return false;

        int x = cell.x, y = cell.y;
        // check 4 cardinal neighbors
        if (CheckNeighborBlocked(new Vector2Int(x + 1, y), gridWidth, gridHeight, roadCells, blockedDecorationCells, isLocationCell)) return false;
        if (CheckNeighborBlocked(new Vector2Int(x - 1, y), gridWidth, gridHeight, roadCells, blockedDecorationCells, isLocationCell)) return false;
        if (CheckNeighborBlocked(new Vector2Int(x, y + 1), gridWidth, gridHeight, roadCells, blockedDecorationCells, isLocationCell)) return false;
        if (CheckNeighborBlocked(new Vector2Int(x, y - 1), gridWidth, gridHeight, roadCells, blockedDecorationCells, isLocationCell)) return false;

        return true;
    }

    private static bool CheckNeighborBlocked(Vector2Int n, int w, int h,
        HashSet<Vector2Int> roadCells, HashSet<Vector2Int> blockedDecorationCells,
        System.Func<Vector2Int, bool> isLocationCell)
    {
        if (n.x < 0 || n.x >= w || n.y < 0 || n.y >= h) return false;
        return roadCells.Contains(n) || blockedDecorationCells.Contains(n) || isLocationCell(n);
    }

    private static void Shuffle(List<Vector2Int> cells)
    {
        for (int i = cells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (cells[i], cells[j]) = (cells[j], cells[i]);
        }
    }
}
