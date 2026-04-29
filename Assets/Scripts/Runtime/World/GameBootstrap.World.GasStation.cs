using UnityEngine;

public partial class GameBootstrap
{
    private bool TryPlaceGasStationAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.GasStation))
        {
            SessionDebugLogger.Log("BUILD", "Gas Station placement rejected: gas station already exists.");
            return false;
        }

        if (!TryGetGasStationPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Gas Station placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.GasStation, "Gas Station", min, max, anchorCell, new Color(0.84f, 0.68f, 0.26f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        NotifyTutorialServiceBuildingBuilt(LocationType.GasStation);
        SessionDebugLogger.Log("BUILD", $"Placed Gas Station at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool TryGetGasStationPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetTwoByTwoBuildingPlacement(anchorCell, LocationType.GasStation, out min, out max);
    }

    private bool GetGasStationPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetTwoByTwoBuildingPlacementPreview(anchorCell, LocationType.GasStation, out previewPosition, out previewScale);
    }
}
