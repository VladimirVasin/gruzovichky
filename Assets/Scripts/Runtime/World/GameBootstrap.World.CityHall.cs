using UnityEngine;

public partial class GameBootstrap
{
    private bool TryPlaceCityHallAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.CityHall))
        {
            SessionDebugLogger.Log("BUILD", "City Hall placement rejected: already exists.");
            return false;
        }

        if (!TryGetCityHallPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"City Hall placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.CityHall, "City Hall", min, max, anchorCell, new Color(0.35f, 0.42f, 0.55f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        isCityHallScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        PushFeedEvent(
            "City Hall is open: citizens can now file complaints.",
            "\u0420\u0430\u0442\u0443\u0448\u0430 \u043e\u0442\u043a\u0440\u044b\u0442\u0430: \u0433\u043e\u0440\u043e\u0436\u0430\u043d\u0435 \u043c\u043e\u0433\u0443\u0442 \u043f\u043e\u0434\u0430\u0432\u0430\u0442\u044c \u0436\u0430\u043b\u043e\u0431\u044b.",
            FeedEventType.Success);
        SessionDebugLogger.Log("BUILD", $"Placed City Hall at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool TryGetCityHallPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, LocationType.CityHall, 4, 3, out min, out max);
    }

    private bool GetCityHallPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetRotatedBuildingPlacementPreview(anchorCell, LocationType.CityHall, 4, 3, out previewPosition, out previewScale);
    }

    private void CreateCityHallDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "CityHallDetailRoot", center, min, max, anchor, BuildingDecorScale);
        Color wall = new(0.56f, 0.62f, 0.72f);
        Color trim = new(0.82f, 0.78f, 0.66f);
        Color roof = new(0.20f, 0.26f, 0.36f);
        Color glass = new(0.60f, 0.82f, 0.94f);
        Color stone = new(0.62f, 0.64f, 0.66f);
        Color dark = new(0.12f, 0.14f, 0.18f);
        Color paper = new(0.94f, 0.90f, 0.76f);

        CreateBuildingBox(root, "CityHallMainHall", new Vector3(0f, 0.46f, -0.08f), new Vector3(3.28f, 0.92f, 1.82f), wall, VisualSmoothnessBuildingWall, true, true);
        CreateBuildingBox(root, "CityHallPlinth", new Vector3(0f, 0.10f, 0.84f), new Vector3(3.48f, 0.18f, 0.22f), stone, VisualSmoothnessAsphalt, true, true);
        CreateBuildingBox(root, "CityHallRoof", new Vector3(0f, 1.00f, -0.08f), new Vector3(3.58f, 0.14f, 2.04f), roof, VisualSmoothnessRoofMetal, true, true);
        CreateBuildingBox(root, "CityHallCornice", new Vector3(0f, 1.12f, 0.92f), new Vector3(3.66f, 0.08f, 0.08f), trim, VisualSmoothnessVehicleMetal, true, true);

        CreateBuildingBox(root, "CityHallEntranceFrame", new Vector3(0f, 0.39f, 0.93f), new Vector3(0.78f, 0.72f, 0.08f), trim, VisualSmoothnessBuildingWall, true, true);
        CreateBuildingBox(root, "CityHallEntranceDoor", new Vector3(0f, 0.31f, 0.98f), new Vector3(0.46f, 0.58f, 0.06f), dark, VisualSmoothnessVehicleMetal, true, true);
        CreateBuildingBox(root, "CityHallStepsLow", new Vector3(0f, 0.05f, 1.20f), new Vector3(1.38f, 0.10f, 0.34f), stone, VisualSmoothnessAsphalt, true, true);
        CreateBuildingBox(root, "CityHallStepsHigh", new Vector3(0f, 0.13f, 1.05f), new Vector3(1.08f, 0.10f, 0.24f), stone, VisualSmoothnessAsphalt, true, true);

        for (int i = 0; i < 4; i++)
        {
            float x = -1.26f + i * 0.84f;
            CreateBuildingBox(root, "CityHallColumn", new Vector3(x, 0.50f, 0.97f), new Vector3(0.12f, 0.72f, 0.10f), trim, VisualSmoothnessBuildingWall, true, true);
        }

        CreateBuildingWindowRow(root, new Vector3(-1.23f, 0.56f, 0.96f), new Vector3(0.50f, 0f, 0f), 2, new Vector3(0.28f, 0.26f, 0.04f), glass);
        CreateBuildingWindowRow(root, new Vector3(0.72f, 0.56f, 0.96f), new Vector3(0.50f, 0f, 0f), 2, new Vector3(0.28f, 0.26f, 0.04f), glass);

        CreateBuildingBox(root, "CityHallTowerBase", new Vector3(0f, 1.34f, -0.08f), new Vector3(0.88f, 0.64f, 0.72f), wall, VisualSmoothnessBuildingWall, true, true);
        CreateBuildingBox(root, "CityHallTowerRoof", new Vector3(0f, 1.74f, -0.08f), new Vector3(1.02f, 0.16f, 0.86f), roof, VisualSmoothnessRoofMetal, true, true);
        CreateBuildingCylinder(root, "CityHallClockFace", new Vector3(0f, 1.42f, 0.31f), new Vector3(0.24f, 0.035f, 0.24f), paper, VisualSmoothnessDefault, true);
        CreateBuildingBox(root, "CityHallClockHandV", new Vector3(0f, 1.43f, 0.34f), new Vector3(0.025f, 0.16f, 0.018f), dark, VisualSmoothnessDefault, true);
        CreateBuildingBox(root, "CityHallClockHandH", new Vector3(0.05f, 1.43f, 0.34f), new Vector3(0.12f, 0.025f, 0.018f), dark, VisualSmoothnessDefault, true);

        CreateBuildingCylinder(root, "CityHallFlagPole", new Vector3(1.52f, 0.74f, 1.04f), new Vector3(0.025f, 0.84f, 0.025f), trim, VisualSmoothnessVehicleMetal, true);
        CreateBuildingBox(root, "CityHallFlag", new Vector3(1.66f, 1.26f, 1.04f), new Vector3(0.28f, 0.18f, 0.03f), new Color(0.72f, 0.16f, 0.16f), VisualSmoothnessFabric, true);

        CreateBuildingBox(root, "ComplaintBoard", new Vector3(-1.45f, 0.34f, 1.12f), new Vector3(0.62f, 0.50f, 0.06f), dark, VisualSmoothnessVehicleMetal, true, true);
        for (int i = 0; i < 4; i++)
        {
            CreateBuildingBox(root, "ComplaintPaper", new Vector3(-1.45f, 0.20f + i * 0.10f, 1.16f), new Vector3(0.44f, 0.05f, 0.018f), paper, VisualSmoothnessDefault, true);
        }

        CreateBuildingBox(root, "ReceptionDesk", new Vector3(1.08f, 0.13f, 1.12f), new Vector3(0.68f, 0.20f, 0.30f), new Color(0.38f, 0.25f, 0.15f), VisualSmoothnessWood, true, true);
        CreateBuildingBox(root, "InTray", new Vector3(0.92f, 0.27f, 1.13f), new Vector3(0.24f, 0.06f, 0.18f), paper, VisualSmoothnessDefault, true);
    }
}
