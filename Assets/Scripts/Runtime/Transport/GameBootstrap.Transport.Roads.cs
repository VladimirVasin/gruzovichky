using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private void StartMoveTo(Vector2Int destination)
    {
        List<Vector2Int> path = FindPath(truckCell, destination);
        if (path == null || path.Count < 2)
        {
            SessionDebugLogger.Log("PATH", $"{GetLoadedTruckDisplayName()} failed to find path from ({truckCell.x},{truckCell.y}) to ({destination.x},{destination.y}).");
            return;
        }

        activePath.Clear();
        for (int i = 1; i < path.Count; i++)
        {
            activePath.Add(path[i]);
        }

        isTruckMoving = true;
        BeginNextTruckSegment(activePath[0]);
        SessionDebugLogger.Log("PATH", $"{GetLoadedTruckDisplayName()} started moving from ({truckCell.x},{truckCell.y}) to ({destination.x},{destination.y}) over {activePath.Count} steps.");
    }
    private bool HasPath(Vector2Int start, Vector2Int goal)
    {
        return FindPath(start, goal) != null;
    }
    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        LocationType? startLocation = GetContainingLocation(start);
        LocationType? goalLocation = GetContainingLocation(goal);
        return GridPathService.FindPath(
            start,
            goal,
            GridPathService.GetCardinalNeighbors,
            neighbor => IsDriveableForPath(neighbor, startLocation, goalLocation));
    }
    private List<Vector2Int> FindRoadBuildPath(Vector2Int start, Vector2Int goal, System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        return GridPathService.FindPath(
            start,
            goal,
            GridPathService.GetCardinalNeighbors,
            neighbor => CanBuildRoadThroughCell(neighbor, start, goal, isBlockedLocationCell));
    }
    private bool IsDriveable(Vector2Int cell)
    {
        return IsInsideGrid(cell) && (roadCells.Contains(cell) || edgeHighwayCells.Contains(cell) || IsAnchorCell(cell));
    }
    private bool CanBuildRoadThroughCell(Vector2Int cell, Vector2Int start, Vector2Int goal, System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        if (!IsInsideGrid(cell) || waterCells.Contains(cell))
        {
            return false;
        }

        if (cell == start || cell == goal || roadCells.Contains(cell))
        {
            return true;
        }

        return !isBlockedLocationCell(cell);
    }
    private bool IsDriveableForPath(Vector2Int cell, LocationType? startLocation, LocationType? goalLocation)
    {
        return IsDriveable(cell);
    }
    private bool IsAnchorCell(Vector2Int cell)
    {
        foreach (LocationData location in locations.Values)
        {
            if (DoesLocationRequireRoadAccess(location.Type) &&
                (location.Anchor == cell || location.RoadAccess == cell))
            {
                return true;
            }
        }

        for (int i = 0; i < extraServiceLocations.Count; i++)
        {
            LocationData location = extraServiceLocations[i];
            if (DoesLocationRequireRoadAccess(location.Type) &&
                (location.Anchor == cell || location.RoadAccess == cell))
            {
                return true;
            }
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            LocationData stop = localStops[i];
            if (DoesLocationRequireRoadAccess(stop.Type) &&
                (stop.Anchor == cell || stop.RoadAccess == cell))
            {
                return true;
            }
        }

        return false;
    }
    private LocationType? GetContainingLocation(Vector2Int cell)
    {
        foreach (KeyValuePair<LocationType, LocationData> pair in locations)
        {
            if (pair.Value.Contains(cell) || pair.Value.Anchor == cell || pair.Value.RoadAccess == cell)
            {
                return pair.Key;
            }
        }

        for (int i = 0; i < extraServiceLocations.Count; i++)
        {
            if (extraServiceLocations[i].Contains(cell) || extraServiceLocations[i].Anchor == cell || extraServiceLocations[i].RoadAccess == cell)
            {
                return null;
            }
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            if (localStops[i].Contains(cell) || localStops[i].Anchor == cell || localStops[i].RoadAccess == cell)
            {
                return LocationType.Stop;
            }
        }

        return null;
    }
    private bool IsLocationCell(Vector2Int cell)
    {
        foreach (LocationData location in locations.Values)
        {
            if (location.Contains(cell) || location.Anchor == cell)
            {
                return true;
            }
        }

        for (int i = 0; i < extraServiceLocations.Count; i++)
        {
            if (extraServiceLocations[i].Contains(cell) || extraServiceLocations[i].Anchor == cell)
            {
                return true;
            }
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            if (localStops[i].Contains(cell) || localStops[i].Anchor == cell)
            {
                return true;
            }
        }

        for (int i = 0; i < personalHouses.Count; i++)
        {
            if (personalHouses[i].Contains(cell) || personalHouses[i].Anchor == cell)
            {
                return true;
            }
        }

        return false;
    }
    private bool IsRoadBuildCellBlocked(Vector2Int cell)
    {
        return !IsInsideGrid(cell) || IsLocationCell(cell) || roadCells.Contains(cell) || edgeHighwayCells.Contains(cell) || miscOccupiedCells.Contains(cell) || waterCells.Contains(cell);
    }
    private void AddRoad(Vector2Int cell)
    {
        if (IsRoadBuildCellBlocked(cell))
        {
            return;
        }

        AddRoadCellUnchecked(cell);
    }

    private void EnsureLocationRoadAccessRoadCell(LocationData location, string source)
    {
        Vector2Int cell = location.RoadAccess;
        if (roadCells.Contains(cell))
        {
            return;
        }

        if (!IsInsideGrid(cell) || edgeHighwayCells.Contains(cell) || IsWaterOrBeachCell(cell))
        {
            SessionDebugLogger.Log("BUILD_ROAD", $"{source} driveway road skipped at ({cell.x},{cell.y}): blocked.");
            return;
        }

        if (location.Contains(cell))
        {
            SessionDebugLogger.Log("BUILD_ROAD", $"{source} driveway road skipped at ({cell.x},{cell.y}): inside building footprint.");
            return;
        }

        RemoveMiscObjectAtCell(cell);
        AddRoadCellUnchecked(cell);
        SessionDebugLogger.Log("BUILD_ROAD", $"{source} driveway road added at ({cell.x},{cell.y}).");
    }

    private void AddRoadCellUnchecked(Vector2Int cell)
    {
        ClearFootpathAtCell(cell);
        ClearStreetLitterAtCell(cell);
        if (!roadCells.Add(cell))
        {
            return;
        }

        RefreshTerrainCellVisual(cell);
        RefreshGroundCellSurfaceMaterial(cell);

        GameObject road = new($"Road_{cell.x}_{cell.y}");
        road.name = $"Road_{cell.x}_{cell.y}";
        road.transform.SetParent(roadsRoot, false);
        road.transform.localPosition = Vector3.zero;
        roadVisuals[cell] = road;

        RefreshRoadVisual(cell);
        foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(cell))
        {
            if (roadVisuals.ContainsKey(neighbor))
            {
                RefreshRoadVisual(neighbor);
            }
        }

        QueueRoadsideRefreshAround(cell);
        if (!suppressUnifiedRoadVisualRebuild)
        {
            RebuildUnifiedRoadVisuals();
        }
        QueueSurfaceTransitionOverlayRebuild();
        UpdateRoadAccessWarningMarkers();
        if (SessionDebugLogger.IsVerboseEnabled("ROAD_TRACE"))
        {
            SessionDebugLogger.LogVerbose("ROAD_TRACE", $"Added road at cell ({cell.x},{cell.y}).");
        }
    }

    private void RemoveRoad(Vector2Int cell)
    {
        if (!roadCells.Remove(cell))
        {
            return;
        }

        if (roadVisuals.TryGetValue(cell, out GameObject road))
        {
            roadVisuals.Remove(cell);
            Destroy(road);
        }

        RefreshTerrainCellVisual(cell);
        RefreshGroundCellSurfaceMaterial(cell);

        foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(cell))
        {
            if (roadVisuals.ContainsKey(neighbor))
            {
                RefreshRoadVisual(neighbor);
            }
        }

        QueueRoadsideRefreshAround(cell);
        if (!suppressUnifiedRoadVisualRebuild)
        {
            RebuildUnifiedRoadVisuals();
        }
        QueueSurfaceTransitionOverlayRebuild();
        UpdateRoadAccessWarningMarkers();
        SessionDebugLogger.Log("ROAD", $"Removed road at cell ({cell.x},{cell.y}).");
    }
    private bool ConnectsToRoadOrAnchor(Vector2Int cell, Vector2Int offset)
    {
        Vector2Int neighbor = cell + offset;
        return IsRoadVisualReady(neighbor) || IsAnchorCell(neighbor);
    }

    private void QueueRoadsideRefreshAround(Vector2Int cell)
    {
        if (!suppressRoadsideRefresh)
        {
            RefreshRoadsideDecorationsAround(cell);
            return;
        }

        pendingRoadsideRefreshCells.Add(cell);
        foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(cell))
        {
            pendingRoadsideRefreshCells.Add(neighbor);
        }
    }

    private void RefreshRoadConnectivityAround(IEnumerable<Vector2Int> cells)
    {
        if (cells == null)
        {
            return;
        }

        HashSet<Vector2Int> affectedCells = new();
        foreach (Vector2Int cell in cells)
        {
            if (!IsInsideGrid(cell))
            {
                continue;
            }

            affectedCells.Add(cell);
            foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(cell))
            {
                if (IsInsideGrid(neighbor))
                {
                    affectedCells.Add(neighbor);
                }
            }
        }

        foreach (Vector2Int cell in affectedCells)
        {
            if (roadVisuals.ContainsKey(cell))
            {
                RefreshRoadVisual(cell);
            }

            QueueRoadsideRefreshAround(cell);
        }
    }
}
