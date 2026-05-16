using UnityEngine;

public partial class GameBootstrap
{
    private bool TryPlacePrimarySchoolAtAnchor(Vector2Int anchorCell)
    {
        if (!TryGetPrimarySchoolPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Primary School placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.PrimarySchool, "Primary School", min, max, anchorCell, new Color(0.64f, 0.70f, 0.78f));
        OnEducationBuildingPlaced(LocationType.PrimarySchool, anchorCell, min, max);
        return true;
    }

    private bool TryPlaceSecondarySchoolAtAnchor(Vector2Int anchorCell)
    {
        if (!TryGetSecondarySchoolPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Secondary School placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.SecondarySchool, "Secondary School", min, max, anchorCell, new Color(0.58f, 0.62f, 0.72f));
        OnEducationBuildingPlaced(LocationType.SecondarySchool, anchorCell, min, max);
        return true;
    }

    private void OnEducationBuildingPlaced(LocationType schoolType, Vector2Int anchorCell, Vector2Int min, Vector2Int max)
    {
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        NotifyTutorialServiceBuildingBuilt(schoolType);
        SessionDebugLogger.Log("BUILD", $"Placed {schoolType} at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
    }

    private bool TryGetPrimarySchoolPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, LocationType.PrimarySchool, 5, 3, out min, out max);
    }

    private bool TryGetSecondarySchoolPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, LocationType.SecondarySchool, 6, 3, out min, out max);
    }

    private bool GetPrimarySchoolPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetRotatedBuildingPlacementPreview(anchorCell, LocationType.PrimarySchool, 5, 3, out previewPosition, out previewScale);
    }

    private bool GetSecondarySchoolPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetRotatedBuildingPlacementPreview(anchorCell, LocationType.SecondarySchool, 6, 3, out previewPosition, out previewScale);
    }

    private void CreateSchoolDecoration(LocationData owner, LocationType schoolType, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        if (TryCreateImportedSchoolModel(owner, schoolType, parent, center, min, max, anchor))
        {
            return;
        }

        bool secondary = schoolType == LocationType.SecondarySchool;
        Transform root = CreateAnchorOrientedBuildingRoot(parent, secondary ? "SecondarySchoolDetailRoot" : "PrimarySchoolDetailRoot", center, min, max, anchor, BuildingDecorScale);

        Color wall = secondary ? new Color(0.68f, 0.70f, 0.78f) : new Color(0.78f, 0.82f, 0.76f);
        Color roof = secondary ? new Color(0.28f, 0.32f, 0.40f) : new Color(0.36f, 0.45f, 0.52f);
        Color trim = secondary ? new Color(0.86f, 0.74f, 0.38f) : new Color(0.92f, 0.76f, 0.34f);
        Color glass = new(0.58f, 0.82f, 0.94f);
        Color asphalt = new(0.32f, 0.35f, 0.36f);
        Color brick = secondary ? new Color(0.52f, 0.30f, 0.24f) : new Color(0.72f, 0.48f, 0.34f);

        float width = secondary ? 5.20f : 4.12f;
        float depth = secondary ? 1.78f : 1.58f;
        float height = secondary ? 1.20f : 0.90f;

        CreateBuildingBox(root, "SchoolMainHall", new Vector3(0f, height * 0.50f, -0.10f), new Vector3(width, height, depth), wall, VisualSmoothnessBuildingWall, true, true);
        CreateBuildingBox(root, "SchoolRoof", new Vector3(0f, height + 0.10f, -0.10f), new Vector3(width + 0.24f, 0.18f, depth + 0.22f), roof, VisualSmoothnessRoofMetal, true, true);
        CreateBuildingBox(root, "SchoolEntrance", new Vector3(0f, 0.36f, 0.80f), new Vector3(0.72f, 0.70f, 0.10f), brick, VisualSmoothnessBuildingWall, true, true);
        CreateBuildingBox(root, "SchoolDoor", new Vector3(0f, 0.30f, 0.86f), new Vector3(0.42f, 0.58f, 0.06f), new Color(0.18f, 0.22f, 0.28f), VisualSmoothnessVehicleMetal, true, true);
        CreateBuildingBox(root, "SchoolSign", new Vector3(0f, secondary ? 1.10f : 0.84f, 0.88f), new Vector3(1.32f, 0.22f, 0.05f), new Color(0.22f, 0.27f, 0.32f), VisualSmoothnessVehicleMetal, true, true);

        int windowCount = secondary ? 5 : 4;
        float windowStartX = secondary ? -1.72f : -1.42f;
        CreateBuildingWindowRow(root, new Vector3(windowStartX, secondary ? 0.72f : 0.54f, 0.78f), new Vector3(0.70f, 0f, 0f), windowCount, new Vector3(0.34f, 0.24f, 0.04f), glass);
        if (secondary)
        {
            CreateBuildingWindowRow(root, new Vector3(windowStartX, 1.02f, 0.78f), new Vector3(0.70f, 0f, 0f), windowCount, new Vector3(0.32f, 0.20f, 0.04f), glass);
        }

        CreateBuildingBox(root, "SchoolYard", new Vector3(0f, -0.19f, 1.14f), new Vector3(secondary ? 4.80f : 3.70f, 0.025f, 1.18f), asphalt, VisualSmoothnessAsphalt, true);
        CreateBuildingBox(root, "SchoolPath", new Vector3(0f, -0.16f, 0.42f), new Vector3(0.70f, 0.035f, 0.96f), new Color(0.74f, 0.68f, 0.54f), VisualSmoothnessAsphalt, true);

        if (secondary)
        {
            CreateBuildingBox(root, "SchoolBasketCourt", new Vector3(-1.55f, -0.145f, 1.20f), new Vector3(1.38f, 0.02f, 0.72f), new Color(0.44f, 0.48f, 0.50f), VisualSmoothnessAsphalt, true);
            CreateBuildingBox(root, "SchoolCourtLine", new Vector3(-1.55f, -0.125f, 1.20f), new Vector3(1.24f, 0.012f, 0.04f), trim, VisualSmoothnessDefault, true);
            CreateBuildingCylinder(root, "SchoolFlagPole", new Vector3(2.10f, 0.36f, 0.92f), new Vector3(0.035f, 0.72f, 0.035f), new Color(0.82f, 0.84f, 0.82f), VisualSmoothnessVehicleMetal, true);
            CreateBuildingBox(root, "SchoolFlag", new Vector3(2.28f, 0.72f, 0.92f), new Vector3(0.34f, 0.20f, 0.03f), trim, VisualSmoothnessDefault, true);
        }
        else
        {
            CreateBuildingBox(root, "SchoolBench", new Vector3(-1.46f, 0.02f, 1.18f), new Vector3(0.70f, 0.10f, 0.18f), new Color(0.50f, 0.32f, 0.20f), VisualSmoothnessWood, true);
            CreateBuildingCylinder(root, "SchoolBell", new Vector3(1.52f, 0.78f, 0.82f), new Vector3(0.16f, 0.12f, 0.16f), trim, VisualSmoothnessVehicleMetal, true);
            CreateBuildingBox(root, "SchoolPlanter", new Vector3(1.46f, -0.05f, 1.24f), new Vector3(0.66f, 0.14f, 0.34f), new Color(0.42f, 0.30f, 0.18f), VisualSmoothnessWood, true);
            CreateBuildingCylinder(root, "SchoolPlanterLeaf", new Vector3(1.40f, 0.08f, 1.22f), new Vector3(0.18f, 0.08f, 0.18f), new Color(0.30f, 0.52f, 0.26f), VisualSmoothnessDefault, true);
            CreateBuildingCylinder(root, "SchoolPlanterLeaf", new Vector3(1.60f, 0.09f, 1.26f), new Vector3(0.16f, 0.08f, 0.16f), new Color(0.36f, 0.58f, 0.28f), VisualSmoothnessDefault, true);
        }
    }
}
