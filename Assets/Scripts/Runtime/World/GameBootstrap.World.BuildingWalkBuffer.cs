using UnityEngine;

public partial class GameBootstrap
{
    private bool IsBuildingWalkBufferCell(Vector2Int cell)
    {
        if (!IsInsideGrid(cell) || IsAnyLocationEntranceCell(cell))
        {
            return false;
        }

        foreach (LocationData location in locations.Values)
        {
            if (IsLocationWalkBufferCell(location, cell))
            {
                return true;
            }
        }

        for (int i = 0; i < extraServiceLocations.Count; i++)
        {
            if (IsLocationWalkBufferCell(extraServiceLocations[i], cell))
            {
                return true;
            }
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            if (IsLocationWalkBufferCell(localStops[i], cell))
            {
                return true;
            }
        }

        for (int i = 0; i < personalHouses.Count; i++)
        {
            if (IsLocationWalkBufferCell(personalHouses[i], cell))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsLocationWalkBufferCell(LocationData location, Vector2Int cell)
    {
        if (location == null ||
            !ShouldLocationHaveBuildingWalkBuffer(location.Type) ||
            location.Contains(cell) ||
            IsLocationEntranceCell(location, cell))
        {
            return false;
        }

        return cell.x >= location.Min.x - 1 &&
               cell.x <= location.Max.x + 1 &&
               cell.y >= location.Min.y - 1 &&
               cell.y <= location.Max.y + 1;
    }

    private static bool ShouldLocationHaveBuildingWalkBuffer(LocationType type)
    {
        return type != LocationType.CityPark &&
               type != LocationType.Stop &&
               type != LocationType.IntercityStop;
    }

    private bool IsLocationEntranceCell(LocationData location, Vector2Int cell)
    {
        if (location == null)
        {
            return false;
        }

        if (location.Anchor == cell || location.RoadAccess == cell)
        {
            return true;
        }

        ImportedBuildingRuntime runtime = location.ImportedRuntime;
        return IsImportedEntranceMarkerCell(runtime?.DoorEnterMarker, cell) ||
               IsImportedEntranceMarkerCell(runtime?.DoorInsideMarker, cell) ||
               IsImportedEntranceMarkerCell(runtime?.VisitorStandMarker, cell);
    }

    private bool IsAnyLocationEntranceCell(Vector2Int cell)
    {
        foreach (LocationData location in locations.Values)
        {
            if (IsLocationEntranceCell(location, cell))
            {
                return true;
            }
        }

        for (int i = 0; i < extraServiceLocations.Count; i++)
        {
            if (IsLocationEntranceCell(extraServiceLocations[i], cell))
            {
                return true;
            }
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            if (IsLocationEntranceCell(localStops[i], cell))
            {
                return true;
            }
        }

        for (int i = 0; i < personalHouses.Count; i++)
        {
            if (IsLocationEntranceCell(personalHouses[i], cell))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsImportedEntranceMarkerCell(Transform marker, Vector2Int cell)
    {
        return marker != null && WorldToCell(marker.position) == cell;
    }

    private bool DoesWalkSegmentCrossBlockedWalkCell(Vector3 startWorld, Vector3 targetWorld, DriverRescuePhase walkPhase)
    {
        Vector2Int startCell = WorldToCell(startWorld);
        Vector2Int goalCell = WorldToCell(targetWorld);
        Vector3 delta = targetWorld - startWorld;
        int steps = Mathf.Max(2, Mathf.CeilToInt(delta.magnitude / 0.25f));
        for (int i = 1; i <= steps; i++)
        {
            Vector3 sample = Vector3.Lerp(startWorld, targetWorld, i / (float)steps);
            Vector2Int sampleCell = WorldToCell(sample);
            if (!IsWalkableDriverCell(sampleCell, startCell, goalCell, walkPhase))
            {
                return true;
            }
        }

        return false;
    }

    private int ClearFootpathsInBuildingWalkBuffer(Vector2Int min, Vector2Int max, Vector2Int openingCell)
    {
        int removed = 0;
        for (int x = min.x - 1; x <= max.x + 1; x++)
        {
            for (int y = min.y - 1; y <= max.y + 1; y++)
            {
                Vector2Int cell = new(x, y);
                if ((x >= min.x && x <= max.x && y >= min.y && y <= max.y) ||
                    cell == openingCell)
                {
                    continue;
                }

                if (ClearFootpathAtCell(cell))
                {
                    removed++;
                }
            }
        }

        return removed;
    }
}
