using System;
using System.Collections.Generic;
using UnityEngine;

public readonly struct RoadFootprintResolveResult
{
    public readonly bool CanPlace;
    public readonly Vector2Int WidthOffset;

    public RoadFootprintResolveResult(bool canPlace, Vector2Int widthOffset)
    {
        CanPlace = canPlace;
        WidthOffset = widthOffset;
    }
}

public static class RoadBuildPlacementService
{
    public static RoadFootprintResolveResult ResolveFootprintOffset(
        Vector2Int cell,
        Vector2Int roadDirection,
        bool requireNewRoadCell,
        int gridWidth,
        int gridHeight,
        ISet<Vector2Int> roadCells,
        ISet<Vector2Int> edgeHighwayCells,
        ISet<Vector2Int> miscOccupiedCells,
        Func<Vector2Int, bool> isBlockedLocationCell)
    {
        Vector2Int dir = TwoLaneRoadGeometry.NormalizeDirection(roadDirection);
        Vector2Int preferredOffset = TwoLaneRoadGeometry.GetRightLaneOffset(dir);
        Vector2Int alternateOffset = -preferredOffset;

        if (Contains(roadCells, cell + preferredOffset) &&
            CanPlaceRoadFootprintWithOffset(cell, preferredOffset, requireNewRoadCell, gridWidth, gridHeight, roadCells, edgeHighwayCells, miscOccupiedCells, isBlockedLocationCell))
        {
            return new RoadFootprintResolveResult(true, preferredOffset);
        }

        if (Contains(roadCells, cell + alternateOffset) &&
            CanPlaceRoadFootprintWithOffset(cell, alternateOffset, requireNewRoadCell, gridWidth, gridHeight, roadCells, edgeHighwayCells, miscOccupiedCells, isBlockedLocationCell))
        {
            return new RoadFootprintResolveResult(true, alternateOffset);
        }

        if (Contains(roadCells, cell))
        {
            if (Contains(roadCells, cell + preferredOffset))
            {
                return new RoadFootprintResolveResult(true, preferredOffset);
            }

            if (Contains(roadCells, cell + alternateOffset))
            {
                return new RoadFootprintResolveResult(true, alternateOffset);
            }
        }

        if (CanPlaceRoadFootprintWithOffset(cell, preferredOffset, requireNewRoadCell, gridWidth, gridHeight, roadCells, edgeHighwayCells, miscOccupiedCells, isBlockedLocationCell))
        {
            return new RoadFootprintResolveResult(true, preferredOffset);
        }

        if (CanPlaceRoadFootprintWithOffset(cell, alternateOffset, requireNewRoadCell, gridWidth, gridHeight, roadCells, edgeHighwayCells, miscOccupiedCells, isBlockedLocationCell))
        {
            return new RoadFootprintResolveResult(true, alternateOffset);
        }

        return new RoadFootprintResolveResult(false, preferredOffset);
    }

    public static bool CanPlaceRoadFootprintWithOffset(
        Vector2Int cell,
        Vector2Int widthOffset,
        bool requireNewRoadCell,
        int gridWidth,
        int gridHeight,
        ISet<Vector2Int> roadCells,
        ISet<Vector2Int> edgeHighwayCells,
        ISet<Vector2Int> miscOccupiedCells,
        Func<Vector2Int, bool> isBlockedLocationCell)
    {
        Vector2Int sideCell = cell + widthOffset;
        if (!IsRoadFootprintCellStructurallyClear(cell, gridWidth, gridHeight, roadCells, edgeHighwayCells, miscOccupiedCells, isBlockedLocationCell, out bool primaryIsNew))
        {
            return false;
        }

        if (!IsRoadFootprintCellStructurallyClear(sideCell, gridWidth, gridHeight, roadCells, edgeHighwayCells, miscOccupiedCells, isBlockedLocationCell, out bool sideIsNew))
        {
            return false;
        }

        return !requireNewRoadCell || primaryIsNew || sideIsNew;
    }

    public static bool IsRoadFootprintCellStructurallyClear(
        Vector2Int cell,
        int gridWidth,
        int gridHeight,
        ISet<Vector2Int> roadCells,
        ISet<Vector2Int> edgeHighwayCells,
        ISet<Vector2Int> miscOccupiedCells,
        Func<Vector2Int, bool> isBlockedLocationCell,
        out bool isNewRoadCell)
    {
        isNewRoadCell = false;
        if (cell.x < 0 || cell.x >= gridWidth || cell.y < 0 || cell.y >= gridHeight)
        {
            return false;
        }

        if ((isBlockedLocationCell != null && isBlockedLocationCell(cell)) ||
            Contains(edgeHighwayCells, cell) ||
            Contains(miscOccupiedCells, cell))
        {
            return false;
        }

        isNewRoadCell = !Contains(roadCells, cell);
        return true;
    }

    public static bool WouldCreateThirdParallelRoadLane(Vector2Int cell, Vector2Int roadDirection, ISet<Vector2Int> roadCells)
    {
        Vector2Int offset = TwoLaneRoadGeometry.GetRightLaneOffset(roadDirection);
        return Contains(roadCells, cell + offset) && Contains(roadCells, cell - offset);
    }

    private static bool Contains(ISet<Vector2Int> cells, Vector2Int cell)
    {
        return cells != null && cells.Contains(cell);
    }
}
