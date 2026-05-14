using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class GameBootstrap
{
    private bool TryPlaceRoadAtCell(Vector2Int cell)
    {
        Vector2Int direction = GetBuildRoadDirection();
        SessionDebugLogger.Log("BUILD_ROAD", $"click tool={activeBuildTool} cell={FormatCell(cell)} dir={FormatCell(direction)} mode=single-click.");
        bool built = RunBatchedRoadPlacement(() =>
        {
            HashSet<Vector2Int> roadsBeforeBuild = new(roadCells);
            bool placed = activeBuildTool == BuildTool.SingleRoad
                ? TryPlaceSingleRoadCell(cell, "player")
                : TryPlaceRoadFootprint(cell, direction, "player");
            if (placed)
            {
                StartRoadConstructionWave(new[] { cell }, CollectNewRoadCells(roadsBeforeBuild));
            }

            return placed;
        });
        SessionDebugLogger.Log("BUILD_ROAD", $"click-result tool={activeBuildTool} cell={FormatCell(cell)} built={built}.");
        return built;
    }

    private bool TryPlaceSingleRoadCell(Vector2Int cell, string source)
    {
        if (!CanPlaceSingleRoadCell(cell, requireNewRoadCell: true))
        {
            SessionDebugLogger.Log("BUILD_ROAD", $"{source} single rejected cell={FormatCell(cell)} reason={GetRoadBuildBlockReason(cell)}.");
            return false;
        }

        bool built = TryAddRoadFootprintCell(cell);
        if (built)
        {
            SessionDebugLogger.Log("BUILD_ROAD", $"{source} single built cell={FormatCell(cell)}.");
        }

        return built;
    }

    private bool TryPlaceRoadFootprint(Vector2Int cell, Vector2Int roadDirection)
    {
        return TryPlaceRoadFootprint(cell, roadDirection, "system");
    }

    private bool TryPlaceRoadFootprint(Vector2Int cell, Vector2Int roadDirection, string source)
    {
        Vector2Int dir = NormalizeRoadDirection(roadDirection);
        if (!TryResolveRoadFootprintOffset(cell, dir, requireNewRoadCell: true, IsBuildRoadBlockedCell, out Vector2Int widthOffset))
        {
            Vector2Int preferredOffset = GetRoadWidthOffset(dir);
            Vector2Int alternateOffset = -preferredOffset;
            SessionDebugLogger.Log(
                "BUILD_ROAD",
                $"{source} two-lane rejected anchor={FormatCell(cell)} dir={FormatCell(dir)} preferred={FormatCell(cell + preferredOffset)}:{GetRoadBuildBlockReason(cell + preferredOffset)} alternate={FormatCell(cell + alternateOffset)}:{GetRoadBuildBlockReason(cell + alternateOffset)} anchorReason={GetRoadBuildBlockReason(cell)}.");
            return false;
        }

        bool built = false;
        Vector2Int sideCell = cell + widthOffset;
        built |= TryAddRoadFootprintCell(cell);
        built |= TryAddRoadFootprintCell(sideCell);
        if (built)
        {
            SessionDebugLogger.Log("BUILD_ROAD", $"{source} two-lane built anchor={FormatCell(cell)} side={FormatCell(sideCell)} offset={FormatCell(widthOffset)} dir={FormatCell(dir)}.");
        }
        else
        {
            SessionDebugLogger.Log("BUILD_ROAD", $"{source} two-lane no-op anchor={FormatCell(cell)} side={FormatCell(sideCell)} dir={FormatCell(dir)} reason=already-road.");
        }

        return built;
    }

    private bool TryPlaceRoadFootprintWithOffset(Vector2Int cell, Vector2Int roadDirection, Vector2Int widthOffset, string source)
    {
        Vector2Int dir = NormalizeRoadDirection(roadDirection);
        Vector2Int offset = NormalizeRoadDirection(widthOffset);
        Vector2Int sideCell = cell + offset;
        if (roadCells.Contains(cell) &&
            roadCells.Contains(sideCell) &&
            CanPlaceRoadFootprintWithOffset(cell, offset, requireNewRoadCell: false, IsBuildRoadBlockedCell))
        {
            SessionDebugLogger.Log("BUILD_ROAD", $"{source} two-lane fixed-offset no-op anchor={FormatCell(cell)} side={FormatCell(sideCell)} dir={FormatCell(dir)} reason=already-road.");
            return false;
        }

        if (!CanPlaceRoadFootprintWithOffset(cell, offset, requireNewRoadCell: true, IsBuildRoadBlockedCell))
        {
            SessionDebugLogger.Log(
                "BUILD_ROAD",
                $"{source} two-lane fixed-offset rejected anchor={FormatCell(cell)} side={FormatCell(sideCell)} offset={FormatCell(offset)} dir={FormatCell(dir)} anchorReason={GetRoadBuildBlockReason(cell)} sideReason={GetRoadBuildBlockReason(sideCell)}.");
            return false;
        }

        bool built = false;
        built |= TryAddRoadFootprintCell(cell);
        built |= TryAddRoadFootprintCell(sideCell);
        if (built)
        {
            SessionDebugLogger.Log("BUILD_ROAD", $"{source} two-lane fixed-offset built anchor={FormatCell(cell)} side={FormatCell(sideCell)} offset={FormatCell(offset)} dir={FormatCell(dir)}.");
        }
        else
        {
            SessionDebugLogger.Log("BUILD_ROAD", $"{source} two-lane fixed-offset no-op anchor={FormatCell(cell)} side={FormatCell(sideCell)} dir={FormatCell(dir)} reason=already-road.");
        }

        return built;
    }

    private bool RunBatchedRoadPlacement(System.Func<bool> buildAction)
    {
        bool previousUnifiedSuppression = suppressUnifiedRoadVisualRebuild;
        bool previousRoadsideSuppression = suppressRoadsideRefresh;
        suppressUnifiedRoadVisualRebuild = true;
        suppressRoadsideRefresh = true;

        bool built = false;
        try
        {
            built = buildAction();
        }
        finally
        {
            suppressUnifiedRoadVisualRebuild = previousUnifiedSuppression;
            suppressRoadsideRefresh = previousRoadsideSuppression;
            if (!suppressRoadsideRefresh)
            {
                FlushPendingRoadsideRefreshes();
            }

            if (built && !suppressUnifiedRoadVisualRebuild)
            {
                RebuildUnifiedRoadVisuals();
            }

            if (built)
            {
                FlushSurfaceTransitionOverlayRebuild();
            }
        }

        return built;
    }

    private bool TryFillRoadTurnFootprint(
        Vector2Int previousCell,
        Vector2Int previousDirection,
        Vector2Int currentCell,
        Vector2Int currentDirection,
        System.Func<Vector2Int, bool> isBlockedLocationCell,
        string source,
        List<Vector2Int> debugTurnFillCells = null)
    {
        return TryFillRoadTurnFootprint(
            previousCell,
            previousDirection,
            GetExistingRoadWidthOffset(previousCell, previousDirection),
            currentCell,
            currentDirection,
            GetExistingRoadWidthOffset(currentCell, currentDirection),
            isBlockedLocationCell,
            source,
            debugTurnFillCells);
    }

    private bool TryFillRoadTurnFootprint(
        Vector2Int previousCell,
        Vector2Int previousDirection,
        Vector2Int previousOffset,
        Vector2Int currentCell,
        Vector2Int currentDirection,
        Vector2Int currentOffset,
        System.Func<Vector2Int, bool> isBlockedLocationCell,
        string source,
        List<Vector2Int> debugTurnFillCells = null)
    {
        Vector2Int prevDir = NormalizeRoadDirection(previousDirection);
        Vector2Int curDir = NormalizeRoadDirection(currentDirection);
        if (prevDir == curDir)
        {
            return false;
        }

        GetRoadTurnFillBounds(previousCell, previousOffset, currentCell, currentOffset, out int minX, out int maxX, out int minY, out int maxY);

        bool anyBuilt = false;
        List<Vector2Int> filledCells = new();
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int fillCell = new(x, y);
                if (!IsRoadFootprintCellStructurallyClear(fillCell, isBlockedLocationCell, out bool isNewRoadCell))
                {
                    continue;
                }

                if (!isNewRoadCell)
                {
                    continue;
                }

                if (WouldCreateThirdParallelRoadLane(fillCell, prevDir) ||
                    WouldCreateThirdParallelRoadLane(fillCell, curDir))
                {
                    continue;
                }

                if (TryAddRoadFootprintCell(fillCell))
                {
                    anyBuilt = true;
                    filledCells.Add(fillCell);
                    debugTurnFillCells?.Add(fillCell);
                }
            }
        }

        if (anyBuilt)
        {
            SessionDebugLogger.Log(
                "BUILD_ROAD",
                $"{source} turn-fill built prev={FormatCell(previousCell)} dir={FormatCell(prevDir)} current={FormatCell(currentCell)} dir={FormatCell(curDir)} cells={FormatCellList(filledCells)}.");
        }
        else
        {
            SessionDebugLogger.LogVerbose(
                "BUILD_ROAD",
                $"{source} turn-fill no-op prev={FormatCell(previousCell)} dir={FormatCell(prevDir)} current={FormatCell(currentCell)} dir={FormatCell(curDir)} bounds=({minX},{minY})..({maxX},{maxY}).");
        }

        return anyBuilt;
    }

    private bool CanFillRoadTurnFootprint(
        Vector2Int previousCell,
        Vector2Int previousDirection,
        Vector2Int currentCell,
        Vector2Int currentDirection,
        System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        Vector2Int prevDir = NormalizeRoadDirection(previousDirection);
        Vector2Int curDir = NormalizeRoadDirection(currentDirection);
        if (prevDir == curDir)
        {
            return true;
        }

        GetExistingRoadTurnFillBounds(previousCell, prevDir, currentCell, curDir, out int minX, out int maxX, out int minY, out int maxY);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int fillCell = new(x, y);
                if (!IsRoadFootprintCellStructurallyClear(fillCell, isBlockedLocationCell, out _))
                {
                    continue;
                }
            }
        }

        return true;
    }

    private bool WouldCreateThirdParallelRoadLane(Vector2Int cell, Vector2Int roadDirection)
    {
        return RoadBuildPlacementService.WouldCreateThirdParallelRoadLane(cell, roadDirection, roadCells);
    }

    private void GetExistingRoadTurnFillBounds(
        Vector2Int previousCell,
        Vector2Int previousDirection,
        Vector2Int currentCell,
        Vector2Int currentDirection,
        out int minX,
        out int maxX,
        out int minY,
        out int maxY)
    {
        Vector2Int previousOffset = GetExistingRoadWidthOffset(previousCell, previousDirection);
        Vector2Int currentOffset = GetExistingRoadWidthOffset(currentCell, currentDirection);
        GetRoadTurnFillBounds(previousCell, previousOffset, currentCell, currentOffset, out minX, out maxX, out minY, out maxY);
    }

    private static void GetRoadTurnFillBounds(
        Vector2Int previousCell,
        Vector2Int previousOffset,
        Vector2Int currentCell,
        Vector2Int currentOffset,
        out int minX,
        out int maxX,
        out int minY,
        out int maxY)
    {
        RoadBuildPlacementService.GetTurnFillBoundsFromOffsets(
            previousCell,
            previousOffset,
            currentCell,
            currentOffset,
            out minX,
            out maxX,
            out minY,
            out maxY);
    }

    private Vector2Int GetExistingRoadWidthOffset(Vector2Int cell, Vector2Int roadDirection)
    {
        Vector2Int dir = NormalizeRoadDirection(roadDirection);
        Vector2Int preferredOffset = TwoLaneRoadGeometry.GetRightLaneOffset(dir);
        Vector2Int alternateOffset = -preferredOffset;
        bool hasPreferred = roadCells.Contains(cell + preferredOffset);
        bool hasAlternate = roadCells.Contains(cell + alternateOffset);
        if (hasPreferred && !hasAlternate)
        {
            return preferredOffset;
        }

        if (hasAlternate && !hasPreferred)
        {
            return alternateOffset;
        }

        return preferredOffset;
    }

    private string GetRoadBuildBlockReason(Vector2Int cell)
    {
        if (!IsInsideGrid(cell)) return "outside-grid";
        if (IsLocationCell(cell)) return "location-cell";
        if (IsWaterOrBeachCell(cell)) return "water-or-beach";
        if (edgeHighwayCells.Contains(cell)) return "edge-highway";
        if (roadCells.Contains(cell)) return "existing-road";
        return "clear";
    }

    private bool TryAddRoadFootprintCell(Vector2Int cell)
    {
        if (roadCells.Contains(cell))
        {
            return false;
        }

        RemoveMiscObjectAtCell(cell);
        AddRoad(cell);
        return roadCells.Contains(cell);
    }

    private bool CanPlaceRoadFootprint(Vector2Int cell, Vector2Int roadDirection, bool requireNewRoadCell)
    {
        return CanPlaceRoadFootprint(cell, roadDirection, requireNewRoadCell, IsBuildRoadBlockedCell);
    }

    private bool CanPlaceSingleRoadCell(Vector2Int cell, bool requireNewRoadCell)
    {
        if (!IsRoadFootprintCellStructurallyClear(cell, IsBuildRoadBlockedCell, out bool isNewRoadCell))
        {
            return false;
        }

        return !requireNewRoadCell || isNewRoadCell;
    }

    private bool CanPlaceRoadFootprint(Vector2Int cell, Vector2Int roadDirection, bool requireNewRoadCell, System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        Vector2Int dir = NormalizeRoadDirection(roadDirection);
        return TryResolveRoadFootprintOffset(cell, dir, requireNewRoadCell, isBlockedLocationCell, out _);
    }

    private bool TryResolveContinuingPathRoadFootprintOffset(
        List<Vector2Int> path,
        int pathIndex,
        Vector2Int roadDirection,
        System.Func<Vector2Int, bool> isBlockedLocationCell,
        out Vector2Int widthOffset)
    {
        widthOffset = Vector2Int.zero;
        if (path == null || pathIndex <= 0 || pathIndex >= path.Count)
        {
            return false;
        }

        Vector2Int previousDirection = GetRoadPathPreviewDirection(path, pathIndex - 1);
        Vector2Int direction = NormalizeRoadDirection(roadDirection);
        if (NormalizeRoadDirection(previousDirection) != direction)
        {
            return false;
        }

        Vector2Int previousCell = path[pathIndex - 1];
        Vector2Int cell = path[pathIndex];
        Vector2Int preferredOffset = TwoLaneRoadGeometry.GetRightLaneOffset(direction);
        Vector2Int alternateOffset = -preferredOffset;

        if (roadCells.Contains(previousCell + preferredOffset) &&
            CanPlaceRoadFootprintWithOffset(cell, preferredOffset, requireNewRoadCell: true, isBlockedLocationCell))
        {
            widthOffset = preferredOffset;
            return true;
        }

        if (roadCells.Contains(previousCell + alternateOffset) &&
            CanPlaceRoadFootprintWithOffset(cell, alternateOffset, requireNewRoadCell: true, isBlockedLocationCell))
        {
            widthOffset = alternateOffset;
            return true;
        }

        return false;
    }

    private bool TryResolveRoadFootprintOffset(
        Vector2Int cell,
        Vector2Int roadDirection,
        bool requireNewRoadCell,
        System.Func<Vector2Int, bool> isBlockedLocationCell,
        out Vector2Int widthOffset)
    {
        RoadFootprintResolveResult result = RoadBuildPlacementService.ResolveFootprintOffset(
            cell,
            roadDirection,
            requireNewRoadCell,
            GridWidth,
            GridHeight,
            roadCells,
            edgeHighwayCells,
            null,
            isBlockedLocationCell);
        widthOffset = result.WidthOffset;
        return result.CanPlace;
    }

    private bool CanPlaceRoadFootprintWithOffset(
        Vector2Int cell,
        Vector2Int widthOffset,
        bool requireNewRoadCell,
        System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        return RoadBuildPlacementService.CanPlaceRoadFootprintWithOffset(
            cell,
            widthOffset,
            requireNewRoadCell,
            GridWidth,
            GridHeight,
            roadCells,
            edgeHighwayCells,
            null,
            isBlockedLocationCell);
    }

    private bool IsRoadFootprintCellStructurallyClear(Vector2Int cell, System.Func<Vector2Int, bool> isBlockedLocationCell, out bool isNewRoadCell)
    {
        return RoadBuildPlacementService.IsRoadFootprintCellStructurallyClear(
            cell,
            GridWidth,
            GridHeight,
            roadCells,
            edgeHighwayCells,
            null,
            isBlockedLocationCell,
            out isNewRoadCell);
    }

    private bool IsBuildRoadBlockedCell(Vector2Int cell)
    {
        return IsLocationCell(cell) || IsWaterOrBeachCell(cell);
    }


}
