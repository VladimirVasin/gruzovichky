using System;
using System.Collections.Generic;
using UnityEngine;

public static class WorldLayoutRoadValidator
{
    public static bool CanBuildWideRoadPath(
        Vector2Int start,
        Vector2Int goal,
        int gridWidth,
        int gridHeight,
        Func<Vector2Int, bool> isBlockedLocationCell,
        Func<Vector2Int, bool> isAnchorCell,
        ISet<Vector2Int> roadCells = null,
        ISet<Vector2Int> edgeHighwayCells = null,
        ISet<Vector2Int> miscOccupiedCells = null)
    {
        List<Vector2Int> path = FindRoadBuildPath(start, goal, gridWidth, gridHeight, isBlockedLocationCell, isAnchorCell, roadCells);
        if (path == null || path.Count < 2)
        {
            return false;
        }

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int cell = path[i];
            if (IsRoadPathTerminalCell(path, i) &&
                ((isBlockedLocationCell != null && isBlockedLocationCell(cell)) || Contains(edgeHighwayCells, cell)))
            {
                continue;
            }

            Vector2Int direction = GetPathDirection(path, i);
            if (!TryResolveRoadFootprintOffset(
                    cell,
                    direction,
                    requireNewRoadCell: false,
                    gridWidth,
                    gridHeight,
                    isBlockedLocationCell,
                    roadCells,
                    edgeHighwayCells,
                    miscOccupiedCells,
                    out _))
            {
                return false;
            }

            if (i > 1)
            {
                Vector2Int previousDirection = GetPathDirection(path, i - 1);
                if (!CanFillRoadTurnFootprint(
                        path[i - 1],
                        previousDirection,
                        cell,
                        direction,
                        gridWidth,
                        gridHeight,
                        isBlockedLocationCell,
                        roadCells,
                        edgeHighwayCells,
                        miscOccupiedCells))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static bool TryAppendWideRoadPathCells(
        Vector2Int start,
        Vector2Int goal,
        int gridWidth,
        int gridHeight,
        Func<Vector2Int, bool> isBlockedLocationCell,
        Func<Vector2Int, bool> isAnchorCell,
        ISet<Vector2Int> roadCells,
        ISet<Vector2Int> edgeHighwayCells = null,
        ISet<Vector2Int> miscOccupiedCells = null)
    {
        if (roadCells == null)
        {
            return false;
        }

        List<Vector2Int> path = FindRoadBuildPath(start, goal, gridWidth, gridHeight, isBlockedLocationCell, isAnchorCell, roadCells);
        if (path == null || path.Count < 2)
        {
            return false;
        }

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int cell = path[i];
            Vector2Int direction = GetPathDirection(path, i);
            if (!TryResolveRoadFootprintOffset(
                    cell,
                    direction,
                    requireNewRoadCell: false,
                    gridWidth,
                    gridHeight,
                    isBlockedLocationCell,
                    roadCells,
                    edgeHighwayCells,
                    miscOccupiedCells,
                    out Vector2Int widthOffset))
            {
                return false;
            }

            roadCells.Add(cell);
            roadCells.Add(cell + widthOffset);

            if (i > 1)
            {
                Vector2Int previousDirection = GetPathDirection(path, i - 1);
                if (!TryAppendRoadTurnFillCells(
                        path[i - 1],
                        previousDirection,
                        cell,
                        direction,
                        gridWidth,
                        gridHeight,
                        isBlockedLocationCell,
                        roadCells,
                        edgeHighwayCells,
                        miscOccupiedCells))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public static Vector2Int GetPathDirection(IReadOnlyList<Vector2Int> path, int pathIndex)
    {
        if (path == null || path.Count == 0 || pathIndex < 0 || pathIndex >= path.Count)
        {
            return Vector2Int.up;
        }

        if (pathIndex + 1 < path.Count)
        {
            Vector2Int forward = path[pathIndex + 1] - path[pathIndex];
            if (forward != Vector2Int.zero)
            {
                return forward;
            }
        }

        if (pathIndex - 1 >= 0)
        {
            Vector2Int backward = path[pathIndex] - path[pathIndex - 1];
            if (backward != Vector2Int.zero)
            {
                return backward;
            }
        }

        return Vector2Int.up;
    }

    private static List<Vector2Int> FindRoadBuildPath(
        Vector2Int start,
        Vector2Int goal,
        int gridWidth,
        int gridHeight,
        Func<Vector2Int, bool> isBlockedLocationCell,
        Func<Vector2Int, bool> isAnchorCell,
        ISet<Vector2Int> roadCells)
    {
        return GridPathService.FindPath(
            start,
            goal,
            GridPathService.GetCardinalNeighbors,
            neighbor => CanBuildRoadThroughCell(neighbor, start, goal, gridWidth, gridHeight, isBlockedLocationCell, isAnchorCell, roadCells));
    }

    private static bool CanBuildRoadThroughCell(
        Vector2Int cell,
        Vector2Int start,
        Vector2Int goal,
        int gridWidth,
        int gridHeight,
        Func<Vector2Int, bool> isBlockedLocationCell,
        Func<Vector2Int, bool> isAnchorCell,
        ISet<Vector2Int> roadCells)
    {
        if (!IsInsideGrid(cell, gridWidth, gridHeight))
        {
            return false;
        }

        if (cell == start || cell == goal || Contains(roadCells, cell))
        {
            return true;
        }

        return isBlockedLocationCell == null || !isBlockedLocationCell(cell);
    }

    private static bool TryResolveRoadFootprintOffset(
        Vector2Int cell,
        Vector2Int roadDirection,
        bool requireNewRoadCell,
        int gridWidth,
        int gridHeight,
        Func<Vector2Int, bool> isBlockedLocationCell,
        ISet<Vector2Int> roadCells,
        ISet<Vector2Int> edgeHighwayCells,
        ISet<Vector2Int> miscOccupiedCells,
        out Vector2Int widthOffset)
    {
        Vector2Int dir = TwoLaneRoadGeometry.NormalizeDirection(roadDirection);
        Vector2Int preferredOffset = TwoLaneRoadGeometry.GetRightLaneOffset(dir);
        Vector2Int alternateOffset = -preferredOffset;

        if (Contains(roadCells, cell + preferredOffset) &&
            CanPlaceRoadFootprintWithOffset(cell, preferredOffset, requireNewRoadCell, gridWidth, gridHeight, isBlockedLocationCell, roadCells, edgeHighwayCells, miscOccupiedCells))
        {
            widthOffset = preferredOffset;
            return true;
        }

        if (Contains(roadCells, cell + alternateOffset) &&
            CanPlaceRoadFootprintWithOffset(cell, alternateOffset, requireNewRoadCell, gridWidth, gridHeight, isBlockedLocationCell, roadCells, edgeHighwayCells, miscOccupiedCells))
        {
            widthOffset = alternateOffset;
            return true;
        }

        if (Contains(roadCells, cell))
        {
            if (Contains(roadCells, cell + preferredOffset))
            {
                widthOffset = preferredOffset;
                return true;
            }

            if (Contains(roadCells, cell + alternateOffset))
            {
                widthOffset = alternateOffset;
                return true;
            }
        }

        if (CanPlaceRoadFootprintWithOffset(cell, preferredOffset, requireNewRoadCell, gridWidth, gridHeight, isBlockedLocationCell, roadCells, edgeHighwayCells, miscOccupiedCells))
        {
            widthOffset = preferredOffset;
            return true;
        }

        if (CanPlaceRoadFootprintWithOffset(cell, alternateOffset, requireNewRoadCell, gridWidth, gridHeight, isBlockedLocationCell, roadCells, edgeHighwayCells, miscOccupiedCells))
        {
            widthOffset = alternateOffset;
            return true;
        }

        widthOffset = preferredOffset;
        return false;
    }

    private static bool CanPlaceRoadFootprintWithOffset(
        Vector2Int cell,
        Vector2Int widthOffset,
        bool requireNewRoadCell,
        int gridWidth,
        int gridHeight,
        Func<Vector2Int, bool> isBlockedLocationCell,
        ISet<Vector2Int> roadCells,
        ISet<Vector2Int> edgeHighwayCells,
        ISet<Vector2Int> miscOccupiedCells)
    {
        Vector2Int sideCell = cell + widthOffset;
        if (!IsRoadFootprintCellStructurallyClear(cell, gridWidth, gridHeight, isBlockedLocationCell, roadCells, edgeHighwayCells, miscOccupiedCells, out bool primaryIsNew))
        {
            return false;
        }

        if (!IsRoadFootprintCellStructurallyClear(sideCell, gridWidth, gridHeight, isBlockedLocationCell, roadCells, edgeHighwayCells, miscOccupiedCells, out bool sideIsNew))
        {
            return false;
        }

        return !requireNewRoadCell || primaryIsNew || sideIsNew;
    }

    private static bool CanFillRoadTurnFootprint(
        Vector2Int previousCell,
        Vector2Int previousDirection,
        Vector2Int currentCell,
        Vector2Int currentDirection,
        int gridWidth,
        int gridHeight,
        Func<Vector2Int, bool> isBlockedLocationCell,
        ISet<Vector2Int> roadCells,
        ISet<Vector2Int> edgeHighwayCells,
        ISet<Vector2Int> miscOccupiedCells)
    {
        Vector2Int prevDir = TwoLaneRoadGeometry.NormalizeDirection(previousDirection);
        Vector2Int curDir = TwoLaneRoadGeometry.NormalizeDirection(currentDirection);
        if (prevDir == curDir)
        {
            return true;
        }

        TwoLaneRoadGeometry.GetTurnFillBounds(previousCell, prevDir, currentCell, curDir, out int minX, out int maxX, out int minY, out int maxY);
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int fillCell = new(x, y);
                if (!IsRoadFootprintCellStructurallyClear(fillCell, gridWidth, gridHeight, isBlockedLocationCell, roadCells, edgeHighwayCells, miscOccupiedCells, out _))
                {
                    continue;
                }
            }
        }

        return true;
    }

    private static bool TryAppendRoadTurnFillCells(
        Vector2Int previousCell,
        Vector2Int previousDirection,
        Vector2Int currentCell,
        Vector2Int currentDirection,
        int gridWidth,
        int gridHeight,
        Func<Vector2Int, bool> isBlockedLocationCell,
        ISet<Vector2Int> roadCells,
        ISet<Vector2Int> edgeHighwayCells,
        ISet<Vector2Int> miscOccupiedCells)
    {
        Vector2Int prevDir = TwoLaneRoadGeometry.NormalizeDirection(previousDirection);
        Vector2Int curDir = TwoLaneRoadGeometry.NormalizeDirection(currentDirection);
        if (prevDir == curDir)
        {
            return true;
        }

        TwoLaneRoadGeometry.GetTurnFillBounds(previousCell, prevDir, currentCell, curDir, out int minX, out int maxX, out int minY, out int maxY);
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int fillCell = new(x, y);
                if (!IsRoadFootprintCellStructurallyClear(fillCell, gridWidth, gridHeight, isBlockedLocationCell, roadCells, edgeHighwayCells, miscOccupiedCells, out _))
                {
                    return false;
                }

                roadCells.Add(fillCell);
            }
        }

        return true;
    }

    private static bool IsRoadFootprintCellStructurallyClear(
        Vector2Int cell,
        int gridWidth,
        int gridHeight,
        Func<Vector2Int, bool> isBlockedLocationCell,
        ISet<Vector2Int> roadCells,
        ISet<Vector2Int> edgeHighwayCells,
        ISet<Vector2Int> miscOccupiedCells,
        out bool isNewRoadCell)
    {
        isNewRoadCell = false;
        if (!IsInsideGrid(cell, gridWidth, gridHeight) ||
            (isBlockedLocationCell != null && isBlockedLocationCell(cell)) ||
            Contains(edgeHighwayCells, cell) ||
            Contains(miscOccupiedCells, cell))
        {
            return false;
        }

        isNewRoadCell = !Contains(roadCells, cell);
        return true;
    }

    private static bool IsRoadPathTerminalCell(IReadOnlyList<Vector2Int> path, int pathIndex)
    {
        return path != null && (pathIndex == 0 || pathIndex == path.Count - 1);
    }

    private static bool IsInsideGrid(Vector2Int cell, int gridWidth, int gridHeight)
    {
        return cell.x >= 0 && cell.x < gridWidth && cell.y >= 0 && cell.y < gridHeight;
    }

    private static bool Contains(ISet<Vector2Int> cells, Vector2Int cell)
    {
        return cells != null && cells.Contains(cell);
    }
}
