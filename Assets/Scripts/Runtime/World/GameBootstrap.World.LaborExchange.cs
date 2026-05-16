using UnityEngine;

public partial class GameBootstrap
{
    private bool TryPlaceLaborExchangeAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.LaborExchange))
        {
            SessionDebugLogger.Log("BUILD", "Labor Exchange placement rejected: already exists.");
            return false;
        }

        if (!TryGetLaborExchangePlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Labor Exchange placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.LaborExchange, "Labor Exchange", min, max, anchorCell, new Color(0.34f, 0.47f, 0.56f));
        TryAutoAssignHigherEducatedLaborExchangeClerk("building placed");
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        isShiftsScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        SessionDebugLogger.Log("BUILD", $"Placed Labor Exchange at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        NotifyTutorialServiceBuildingBuilt(LocationType.LaborExchange);
        return true;
    }

    private bool TryGetLaborExchangePlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, LocationType.LaborExchange, 3, 2, out min, out max);
    }

    private bool GetLaborExchangePlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetRotatedBuildingPlacementPreview(anchorCell, LocationType.LaborExchange, 3, 2, out previewPosition, out previewScale);
    }

    private void CreateLaborExchangeDecoration(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        if (TryCreateImportedLaborExchangeModel(owner, parent, center, min, max, anchor))
        {
            return;
        }

        Transform root = CreateAnchorOrientedBuildingRoot(parent, "LaborExchangeDetailRoot", center, min, max, anchor, BuildingDecorScale);
        Color wall = new(0.36f, 0.50f, 0.60f);
        Color stone = new(0.62f, 0.66f, 0.64f);
        Color glass = new(0.58f, 0.82f, 0.92f);
        Color board = new(0.20f, 0.24f, 0.22f);
        Color paper = new(0.92f, 0.88f, 0.74f);

        CreateBuildingBox(root, "LaborMainHall", new Vector3(0f, 0.42f, -0.05f), new Vector3(2.62f, 0.84f, 1.48f), wall, VisualSmoothnessBuildingWall, true, true);
        CreateBuildingBox(root, "LaborStonePlinth", new Vector3(0f, 0.08f, 0.72f), new Vector3(2.76f, 0.16f, 0.18f), stone, VisualSmoothnessAsphalt, true, true);
        CreateBuildingBox(root, "LaborFlatRoof", new Vector3(0f, 0.91f, -0.05f), new Vector3(2.86f, 0.12f, 1.68f), new Color(0.20f, 0.28f, 0.32f), VisualSmoothnessRoofMetal, true, true);
        CreateBuildingBox(root, "LaborRoofTrim", new Vector3(0f, 1.02f, 0.78f), new Vector3(2.94f, 0.08f, 0.08f), new Color(0.78f, 0.70f, 0.38f), VisualSmoothnessVehicleMetal, true, true);
        CreateBuildingBox(root, "LaborEntrance", new Vector3(0f, 0.28f, 0.76f), new Vector3(0.46f, 0.54f, 0.06f), new Color(0.15f, 0.18f, 0.18f), VisualSmoothnessVehicleMetal, true, true);
        CreateBuildingWindowRow(root, new Vector3(-1.02f, 0.52f, 0.77f), new Vector3(0.42f, 0f, 0f), 2, new Vector3(0.28f, 0.24f, 0.04f), glass);
        CreateBuildingWindowRow(root, new Vector3(0.60f, 0.52f, 0.77f), new Vector3(0.42f, 0f, 0f), 2, new Vector3(0.28f, 0.24f, 0.04f), glass);

        CreateBuildingBox(root, "LaborSignBoard", new Vector3(0f, 0.78f, 0.84f), new Vector3(1.1f, 0.26f, 0.05f), board, VisualSmoothnessVehicleMetal, true, true);
        for (int i = 0; i < 3; i++)
        {
            CreateBuildingBox(root, "LaborSignLine", new Vector3(-0.28f + i * 0.28f, 0.79f, 0.875f), new Vector3(0.18f, 0.035f, 0.02f), paper, VisualSmoothnessDefault, true);
        }

        CreateBuildingBox(root, "VacancyBoard", new Vector3(-1.15f, 0.30f, 1.02f), new Vector3(0.54f, 0.46f, 0.05f), board, VisualSmoothnessVehicleMetal, true, true);
        for (int i = 0; i < 5; i++)
        {
            float y = 0.16f + i * 0.08f;
            CreateBuildingBox(root, "VacancyPaper", new Vector3(-1.15f, y, 1.055f), new Vector3(0.38f, 0.045f, 0.015f), i % 2 == 0 ? paper : new Color(0.84f, 0.90f, 0.82f), VisualSmoothnessDefault, true);
        }

        CreateBuildingBox(root, "InterviewDesk", new Vector3(0.92f, 0.12f, 0.98f), new Vector3(0.62f, 0.18f, 0.28f), new Color(0.42f, 0.28f, 0.16f), VisualSmoothnessWood, true, true);
        CreateBuildingBox(root, "DocumentStack", new Vector3(0.77f, 0.25f, 0.98f), new Vector3(0.22f, 0.08f, 0.18f), paper, VisualSmoothnessDefault, true);
        for (int i = 0; i < 4; i++)
        {
            float x = -0.54f + i * 0.36f;
            CreateBuildingCylinder(root, "QueuePost", new Vector3(x, 0.13f, 1.18f), new Vector3(0.045f, 0.20f, 0.045f), new Color(0.78f, 0.70f, 0.38f), VisualSmoothnessVehicleMetal, true);
            if (i < 3)
            {
                CreateBuildingBox(root, "QueueRope", new Vector3(x + 0.18f, 0.28f, 1.18f), new Vector3(0.34f, 0.035f, 0.035f), new Color(0.18f, 0.20f, 0.24f), VisualSmoothnessFabric, true);
            }
        }
        EnhanceLaborExchangeModel(parent, center, min, max, anchor);
    }
}
