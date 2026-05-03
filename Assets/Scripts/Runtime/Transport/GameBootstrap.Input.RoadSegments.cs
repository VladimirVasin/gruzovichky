using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class GameBootstrap
{
    private bool TryHandleTwoLaneRoadSegmentPlacement(Vector2Int cell)
    {
        if (!roadPathStart.HasValue)
        {
            if (!IsInsideGrid(cell))
            {
                SessionDebugLogger.Log("BUILD_ROAD", $"segment-start rejected cell={FormatCell(cell)} reason=outside-grid.");
                return false;
            }

            Vector2Int startDirection = GetAdjacentRoadPreviewDirection(cell);
            if (!CanPlaceRoadFootprint(cell, startDirection, requireNewRoadCell: false))
            {
                SessionDebugLogger.Log("BUILD_ROAD", $"segment-start rejected tool={activeBuildTool} cell={FormatCell(cell)} dir={FormatCell(startDirection)} reason={GetRoadBuildBlockReason(cell)}.");
                return false;
            }

            roadPathStart = cell;
            SetRoadPathStartHighlights(cell, startDirection, true);
            PlayUiSound(uiSelectClip, 0.65f);
            SessionDebugLogger.Log("BUILD_ROAD", $"segment-start accepted tool={activeBuildTool} cell={FormatCell(cell)} dir={FormatCell(startDirection)}.");
            return false;
        }

        SessionDebugLogger.Log("BUILD_ROAD", $"segment-finish requested tool={activeBuildTool} start={FormatCell(roadPathStart.Value)} requestedEnd={FormatCell(cell)} axisLocked={IsActiveRoadSegmentAxisLocked()} previewCells={FormatCellList(buildPreviewFootprintCells)}.");
        bool built = TryBuildRoadPath(roadPathStart.Value, cell);
        if (built)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.RoadSingleCell);
            MarkTutorialGoalComplete(TutorialGoalKind.RoadShiftPath);
        }

        roadPathStart = null;
        CancelRoadPathMode();
        return built;
    }

    private List<Vector2Int> GetRoadBuildToolPath(Vector2Int start, Vector2Int end)
    {
        if (activeBuildTool == BuildTool.Road)
        {
            return RoadSegmentBuildService.BuildManhattanSegment(start, end, IsRoadSegmentAxisLocked());
        }

        return FindRoadBuildPath(start, end, IsBuildRoadBlockedCell);
    }

    private static bool IsRoadSegmentAxisLocked()
    {
        return Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
    }

    private bool IsActiveRoadSegmentAxisLocked()
    {
        return activeBuildTool == BuildTool.Road && IsRoadSegmentAxisLocked();
    }

    private bool CanCommitTwoLaneRoadSegmentPath(List<Vector2Int> path, out string blockedReason)
    {
        blockedReason = "ok";
        if (path == null || path.Count == 0)
        {
            blockedReason = "empty-path";
            return false;
        }

        bool hasNewCell = false;
        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int direction = GetRoadPathPreviewDirection(path, i);
            if (!TryResolveRoadFootprintOffset(path[i], direction, requireNewRoadCell: false, IsBuildRoadBlockedCell, out Vector2Int widthOffset))
            {
                blockedReason = $"blocked-footprint cell={FormatCell(path[i])} dir={FormatCell(direction)} reason={GetRoadBuildBlockReason(path[i])}";
                return false;
            }

            Vector2Int sideCell = path[i] + widthOffset;
            if (!roadCells.Contains(path[i]) || !roadCells.Contains(sideCell))
            {
                hasNewCell = true;
            }
        }

        if (!hasNewCell)
        {
            blockedReason = "already-road";
            return false;
        }

        return true;
    }
}
