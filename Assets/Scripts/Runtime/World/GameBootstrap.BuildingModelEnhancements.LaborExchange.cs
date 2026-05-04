using UnityEngine;

public partial class GameBootstrap
{
    private void EnhanceLaborExchangeModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "LaborExchangeOfficeRoot", center, min, max, anchor, BuildingDecorScale);
        Color metal = new(0.24f, 0.30f, 0.34f);
        Color glass = new(0.52f, 0.78f, 0.92f);
        Color paper = new(0.92f, 0.88f, 0.74f);

        CreateBuildingBox(root, "SideWingLeft", new Vector3(-1.02f, 0.48f, -0.44f), new Vector3(0.66f, 0.62f, 0.58f), new Color(0.30f, 0.43f, 0.52f), VisualSmoothnessBuildingWall, true, true);
        CreateBuildingBox(root, "SideWingRight", new Vector3(1.02f, 0.48f, -0.44f), new Vector3(0.66f, 0.62f, 0.58f), new Color(0.30f, 0.43f, 0.52f), VisualSmoothnessBuildingWall, true, true);
        CreateBuildingBox(root, "CentralGlassLobby", new Vector3(0f, 0.46f, 0.18f), new Vector3(0.76f, 0.68f, 0.08f), glass, VisualSmoothnessGlass, true, true);
        CreateBuildingBox(root, "LobbyMullionV", new Vector3(0f, 0.48f, 0.24f), new Vector3(0.04f, 0.62f, 0.04f), metal, VisualSmoothnessVehicleMetal, true, true);
        CreateBuildingBox(root, "LobbyMullionH", new Vector3(0f, 0.60f, 0.24f), new Vector3(0.72f, 0.04f, 0.04f), metal, VisualSmoothnessVehicleMetal, true, true);
        CreateBuildingBox(root, "FlagPole", new Vector3(1.34f, 0.72f, 0.54f), new Vector3(0.035f, 0.96f, 0.035f), metal, VisualSmoothnessVehicleMetal, true, true);
        CreateBuildingBox(root, "OfficeFlag", new Vector3(1.50f, 1.12f, 0.54f), new Vector3(0.32f, 0.18f, 0.035f), new Color(0.84f, 0.18f, 0.16f), VisualSmoothnessFabric, true, true);
        CreateBuildingBox(root, "NoticeFrame", new Vector3(-1.34f, 0.56f, 0.48f), new Vector3(0.08f, 0.68f, 0.54f), metal, VisualSmoothnessVehicleMetal, true, true);

        for (int i = 0; i < 4; i++)
        {
            CreateBuildingBox(root, "NoticeSlip", new Vector3(-1.39f, 0.34f + i * 0.12f, 0.34f), new Vector3(0.025f, 0.055f, 0.36f), paper, VisualSmoothnessDefault, true);
        }

        for (int i = 0; i < 3; i++)
        {
            float x = -0.54f + i * 0.54f;
            CreateBuildingBox(root, "RoofSkylight", new Vector3(x, 1.12f, -0.28f), new Vector3(0.34f, 0.05f, 0.44f), glass, VisualSmoothnessGlass, true, true);
        }
    }
}
