using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private void GenerateInitialRoadNetwork()
    {
        if (selectedGameStartMode == GameStartMode.Clear)
        {
            SessionDebugLogger.Log("ROAD", "Initial road network skipped for Clear mode.");
            return;
        }

        if (selectedGameStartMode == GameStartMode.User)
        {
            SessionDebugLogger.Log("ROAD", "Initial road network skipped for User mode: player starts without generated city roads.");
            return;
        }

        suppressUnifiedRoadVisualRebuild = true;
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.Parking, LocationType.GasStation);
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.GasStation, LocationType.Warehouse);
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.Warehouse, LocationType.Forest);
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.Forest, LocationType.Sawmill);
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.Sawmill, LocationType.Warehouse);
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.Warehouse, LocationType.Motel);
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.Warehouse, LocationType.IntercityStop);
        suppressUnifiedRoadVisualRebuild = false;

        SessionDebugLogger.Log("ROAD", $"Generated starter road network with {roadCells.Count} road cells.");
        LogStarterRoadValidation();
    }

    private void GenerateUserStarterRoadLoop()
    {
        int centerX = GridWidth / 2;
        int centerY = GridHeight / 2;
        int left = Mathf.Clamp(centerX - 5, 2, GridWidth - 12);
        int right = Mathf.Clamp(centerX + 5, 11, GridWidth - 3);
        int bottom = Mathf.Clamp(centerY - 5, 2, GridHeight - 12);
        int top = Mathf.Clamp(centerY + 5, 11, GridHeight - 3);

        suppressUnifiedRoadVisualRebuild = true;

        for (int x = left; x <= right; x++)
        {
            TryAddRoadFootprintCell(new Vector2Int(x, bottom));
            TryAddRoadFootprintCell(new Vector2Int(x, bottom + 1));
            TryAddRoadFootprintCell(new Vector2Int(x, top));
            TryAddRoadFootprintCell(new Vector2Int(x, top + 1));
        }

        for (int y = bottom; y <= top + 1; y++)
        {
            TryAddRoadFootprintCell(new Vector2Int(left, y));
            TryAddRoadFootprintCell(new Vector2Int(left + 1, y));
            TryAddRoadFootprintCell(new Vector2Int(right, y));
            TryAddRoadFootprintCell(new Vector2Int(right + 1, y));
        }

        suppressUnifiedRoadVisualRebuild = false;
        RebuildUnifiedRoadVisuals();
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();

        SessionDebugLogger.Log(
            "ROAD",
            $"Generated User starter two-lane road loop: left={left}, right={right + 1}, bottom={bottom}, top={top + 1}, roadCells={roadCells.Count}.");
        LogStarterRoadValidation();
    }

    private void LogStarterRoadValidation()
    {
        int accessMisses = 0;
        foreach (LocationData location in locations.Values)
        {
            Vector2Int access = GetRoadConnectionCell(location);
            bool connected = roadCells.Contains(access) || edgeHighwayCells.Contains(access) || location.Anchor == access;
            if (!connected)
            {
                accessMisses++;
            }

            SessionDebugLogger.Log(
                "ROAD_ACCESS",
                $"{location.Label}: anchor=({location.Anchor.x},{location.Anchor.y}) access=({access.x},{access.y}) connected={connected}.");
        }

        int tripleLaneWarnings = 0;
        foreach (Vector2Int cell in roadCells)
        {
            if (!TryGetRoadVisualAxis(cell, out bool isHorizontal))
            {
                continue;
            }

            if (isHorizontal &&
                roadCells.Contains(cell + Vector2Int.down) &&
                roadCells.Contains(cell + Vector2Int.up))
            {
                tripleLaneWarnings++;
                SessionDebugLogger.Log(
                    "ROAD_VALIDATE",
                    $"Possible horizontal-road 3-wide artifact centered at ({cell.x},{cell.y}) with side lanes ({cell.x},{cell.y - 1}) and ({cell.x},{cell.y + 1}).");
            }

            if (!isHorizontal &&
                roadCells.Contains(cell + Vector2Int.left) &&
                roadCells.Contains(cell + Vector2Int.right))
            {
                tripleLaneWarnings++;
                SessionDebugLogger.Log(
                    "ROAD_VALIDATE",
                    $"Possible vertical-road 3-wide artifact centered at ({cell.x},{cell.y}) with side lanes ({cell.x - 1},{cell.y}) and ({cell.x + 1},{cell.y}).");
            }
        }

        SessionDebugLogger.Log(
            "ROAD_VALIDATE",
            $"Starter road validation complete: accessMisses={accessMisses}, possibleTripleLaneWarnings={tripleLaneWarnings}, roadCells={roadCells.Count}.");
    }
    private void CreateGuaranteedRoadConnectionIfLocationsExist(LocationType startType, LocationType endType)
    {
        if (!locations.TryGetValue(startType, out LocationData start) ||
            !locations.TryGetValue(endType, out LocationData end))
        {
            return;
        }

        Vector2Int startCell = GetRoadConnectionCell(start);
        Vector2Int endCell = GetRoadConnectionCell(end);
        SessionDebugLogger.Log(
            "ROAD_ACCESS",
            $"Connecting {start.Label} anchor=({start.Anchor.x},{start.Anchor.y}) access=({startCell.x},{startCell.y}) -> {end.Label} anchor=({end.Anchor.x},{end.Anchor.y}) access=({endCell.x},{endCell.y}).");
        CreateGuaranteedRoadConnection(startCell, endCell);
    }
    private static Vector2Int GetRoadConnectionCell(LocationData location)
    {
        return location.RoadAccess == default ? location.Anchor : location.RoadAccess;
    }
    private void CreateGuaranteedRoadConnection(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = FindRoadBuildPath(start, end, IsLocationCell);
        if (path == null || path.Count < 2)
        {
            return;
        }

        for (int i = 0; i < path.Count; i++)
        {
            TryAddStarterRoadFootprint(path, i, IsLocationCell);
        }
    }
    private void TryAddStarterRoadFootprint(List<Vector2Int> path, int pathIndex, System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        if (path == null || pathIndex < 0 || pathIndex >= path.Count)
        {
            return;
        }

        Vector2Int cell = path[pathIndex];
        if (IsRoadPathTerminalCell(path, pathIndex) && (IsLocationCell(cell) || edgeHighwayCells.Contains(cell)))
        {
            return;
        }

        Vector2Int direction = GetRoadPathPreviewDirection(path, pathIndex);
        if (!TryResolveContinuingStarterRoadFootprintOffset(path, pathIndex, direction, isBlockedLocationCell, out Vector2Int widthOffset) &&
            !TryResolveRoadFootprintOffset(cell, direction, requireNewRoadCell: true, isBlockedLocationCell, out widthOffset))
        {
            SessionDebugLogger.Log(
                "ROAD",
                $"Skipped starter road footprint at ({cell.x},{cell.y}) dir=({direction.x},{direction.y}): both lane sides blocked.");
            return;
        }

        bool taperLocationTerminal = IsRoadPathTerminalCell(path, pathIndex) && IsLocationRoadAccessCell(cell);
        TryAddStarterRoadFootprintCell(cell, direction, "primary");
        if (!taperLocationTerminal)
        {
            TryAddStarterRoadFootprintCell(cell + widthOffset, direction, "side");
        }

        if (pathIndex > 1)
        {
            Vector2Int previousDirection = GetRoadPathPreviewDirection(path, pathIndex - 1);
            TryFillRoadTurnFootprint(path[pathIndex - 1], previousDirection, cell, direction, isBlockedLocationCell, "starter");
        }
    }

    private bool TryResolveContinuingStarterRoadFootprintOffset(
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

    private bool CanBuildWideRoadPath(Vector2Int start, Vector2Int goal, System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        return WorldLayoutRoadValidator.CanBuildWideRoadPath(
            start,
            goal,
            GridWidth,
            GridHeight,
            isBlockedLocationCell,
            IsAnchorCell,
            roadCells,
            edgeHighwayCells,
            miscOccupiedCells);
    }
    private static bool IsRoadPathTerminalCell(List<Vector2Int> path, int pathIndex)
    {
        return path != null && (pathIndex == 0 || pathIndex == path.Count - 1);
    }

    private bool TryAddStarterRoadFootprintCell(Vector2Int cell, Vector2Int roadDirection, string laneLabel)
    {
        if (WouldCreateThirdParallelStarterLane(cell, roadDirection))
        {
            SessionDebugLogger.Log(
                "ROAD",
                $"Skipped starter road {laneLabel} cell ({cell.x},{cell.y}) dir=({roadDirection.x},{roadDirection.y}): would create third parallel lane.");
            return false;
        }

        return TryAddRoadFootprintCell(cell);
    }

    private bool WouldCreateThirdParallelStarterLane(Vector2Int candidate, Vector2Int roadDirection)
    {
        if (roadCells.Contains(candidate))
        {
            return false;
        }

        Vector2Int dir = NormalizeRoadDirection(roadDirection);
        Vector2Int widthAxis = dir.x != 0 ? Vector2Int.up : Vector2Int.right;
        return (roadCells.Contains(candidate + widthAxis) && roadCells.Contains(candidate + widthAxis * 2)) ||
               (roadCells.Contains(candidate - widthAxis) && roadCells.Contains(candidate - widthAxis * 2));
    }

    private bool IsLocationRoadAccessCell(Vector2Int cell)
    {
        foreach (LocationData location in locations.Values)
        {
            if (location.RoadAccess == cell)
            {
                return true;
            }
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            if (localStops[i].RoadAccess == cell)
            {
                return true;
            }
        }

        return false;
    }
}
