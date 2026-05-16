using UnityEngine;

public partial class GameBootstrap
{
    private bool TryPlaceKindergartenAtAnchor(Vector2Int anchorCell)
    {
        if (!TryGetKindergartenPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Kindergarten placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.Kindergarten, "Kindergarten", min, max, anchorCell, new Color(0.72f, 0.86f, 0.58f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        isShiftsScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        NotifyTutorialServiceBuildingBuilt(LocationType.Kindergarten);
        SessionDebugLogger.Log("BUILD", $"Placed Kindergarten at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool TryGetKindergartenPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, LocationType.Kindergarten, 4, 3, out min, out max);
    }

    private bool GetKindergartenPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetRotatedBuildingPlacementPreview(anchorCell, LocationType.Kindergarten, 4, 3, out previewPosition, out previewScale);
    }

    private void CreateKindergartenDecoration(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        if (TryCreateImportedKindergartenModel(owner, parent, center, min, max, anchor))
        {
            return;
        }

        Transform root = CreateAnchorOrientedBuildingRoot(parent, "KindergartenDetailRoot", center, min, max, anchor, BuildingDecorScale);
        Color wall = new(0.88f, 0.82f, 0.54f);
        Color roof = new(0.30f, 0.52f, 0.46f);
        Color trim = new(0.96f, 0.58f, 0.30f);
        Color glass = new(0.58f, 0.82f, 0.94f);
        Color mat = new(0.34f, 0.62f, 0.36f);

        CreateBuildingBox(root, "KindergartenHall", new Vector3(0f, 0.42f, -0.18f), new Vector3(3.32f, 0.84f, 1.54f), wall, VisualSmoothnessBuildingWall, true, true);
        CreateBuildingBox(root, "KindergartenRoof", new Vector3(0f, 0.92f, -0.18f), new Vector3(3.58f, 0.14f, 1.74f), roof, VisualSmoothnessRoofMetal, true, true);
        CreateBuildingBox(root, "KindergartenAwning", new Vector3(0f, 0.68f, 0.67f), new Vector3(1.26f, 0.10f, 0.36f), trim, VisualSmoothnessRoofMetal, true, true);
        CreateBuildingBox(root, "KindergartenDoor", new Vector3(0f, 0.30f, 0.63f), new Vector3(0.46f, 0.58f, 0.06f), new Color(0.22f, 0.32f, 0.36f), VisualSmoothnessVehicleMetal, true, true);
        CreateBuildingWindowRow(root, new Vector3(-1.18f, 0.50f, 0.64f), new Vector3(0.52f, 0f, 0f), 2, new Vector3(0.32f, 0.24f, 0.04f), glass);
        CreateBuildingWindowRow(root, new Vector3(0.66f, 0.50f, 0.64f), new Vector3(0.52f, 0f, 0f), 2, new Vector3(0.32f, 0.24f, 0.04f), glass);
        CreateBuildingBox(root, "KindergartenSign", new Vector3(0f, 0.84f, 0.76f), new Vector3(1.28f, 0.24f, 0.05f), new Color(0.24f, 0.36f, 0.42f), VisualSmoothnessVehicleMetal, true, true);
        for (int i = 0; i < 4; i++)
        {
            CreateBuildingBox(root, "KindergartenSignTile", new Vector3(-0.42f + i * 0.28f, 0.84f, 0.795f), new Vector3(0.18f, 0.12f, 0.02f), i % 2 == 0 ? trim : glass, VisualSmoothnessDefault, true);
        }

        CreateBuildingBox(root, "PlayMat", new Vector3(0f, -0.19f, 1.08f), new Vector3(2.82f, 0.025f, 1.18f), mat, VisualSmoothnessAsphalt, true);
        CreateBuildingBox(root, "Sandbox", new Vector3(-1.08f, -0.12f, 1.10f), new Vector3(0.82f, 0.10f, 0.58f), new Color(0.86f, 0.72f, 0.42f), VisualSmoothnessDefault, true);
        CreateBuildingBox(root, "SlideDeck", new Vector3(0.88f, 0.16f, 0.94f), new Vector3(0.38f, 0.34f, 0.38f), trim, VisualSmoothnessVehicleMetal, true);
        GameObject slide = CreateBuildingBox(root, "SlideRamp", new Vector3(0.88f, 0.06f, 1.34f), new Vector3(0.42f, 0.06f, 0.78f), new Color(0.28f, 0.58f, 0.90f), VisualSmoothnessVehicleMetal, true);
        slide.transform.localRotation = Quaternion.Euler(-18f, 0f, 0f);
        CreateBuildingCylinder(root, "MerryGoRound", new Vector3(0f, -0.08f, 1.24f), new Vector3(0.34f, 0.035f, 0.34f), new Color(0.88f, 0.32f, 0.32f), VisualSmoothnessVehicleMetal, true);
        CreateBuildingCylinder(root, "MerryGoRoundPole", new Vector3(0f, 0.18f, 1.24f), new Vector3(0.04f, 0.24f, 0.04f), new Color(0.20f, 0.24f, 0.28f), VisualSmoothnessVehicleMetal, true);

        for (int side = -1; side <= 1; side += 2)
        {
            CreateBuildingBox(root, "PlayFence", new Vector3(side * 1.48f, 0.05f, 1.08f), new Vector3(0.06f, 0.36f, 1.28f), new Color(0.94f, 0.90f, 0.74f), VisualSmoothnessWood, true);
            CreateBuildingBox(root, "PlayFenceFront", new Vector3(side * 0.84f, 0.05f, 1.70f), new Vector3(0.92f, 0.32f, 0.06f), new Color(0.94f, 0.90f, 0.74f), VisualSmoothnessWood, true);
        }
    }
}
