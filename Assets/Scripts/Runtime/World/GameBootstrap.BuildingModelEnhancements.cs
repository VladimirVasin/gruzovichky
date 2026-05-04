using UnityEngine;

public partial class GameBootstrap
{
    private void EnhanceParkingModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "ParkingDetailRoot", center, min, max, anchor, BuildingDecorScale);
        Color asphalt = new(0.13f, 0.14f, 0.16f);
        Color line = new(0.92f, 0.9f, 0.74f);
        CreateBuildingBox(root, "TruckYardAsphalt", new Vector3(0f, -0.31f, 0.22f), new Vector3(2.75f, 0.035f, 1.35f), asphalt, VisualSmoothnessAsphalt, true);
        for (int i = 0; i < 4; i++)
        {
            float x = -1.05f + i * 0.7f;
            CreateBuildingBox(root, "ParkingLine", new Vector3(x, -0.27f, 0.34f), new Vector3(0.04f, 0.02f, 0.86f), line, VisualSmoothnessAsphalt, true);
        }

        CreateBuildingBox(root, "DispatchOffice", new Vector3(-0.92f, 0.36f, -0.48f), new Vector3(0.76f, 0.52f, 0.62f), new Color(0.42f, 0.47f, 0.52f), VisualSmoothnessBuildingWall, true);
        CreateBuildingBox(root, "DispatchRoof", new Vector3(-0.92f, 0.67f, -0.48f), new Vector3(0.86f, 0.08f, 0.72f), new Color(0.18f, 0.2f, 0.24f), VisualSmoothnessRoofMetal, true);
        CreateBuildingWindowRow(root, new Vector3(-1.16f, 0.42f, -0.15f), new Vector3(0.24f, 0f, 0f), 3, new Vector3(0.16f, 0.18f, 0.03f), new Color(0.58f, 0.78f, 0.88f));
        CreateBuildingBox(root, "SecurityGate", new Vector3(0.62f, 0.18f, 0.94f), new Vector3(1.2f, 0.05f, 0.06f), new Color(0.95f, 0.78f, 0.24f), VisualSmoothnessVehicleMetal, true);
        CreateBuildingBox(root, "RepairLift", new Vector3(0.8f, -0.18f, -0.35f), new Vector3(0.74f, 0.08f, 0.42f), new Color(0.28f, 0.3f, 0.34f), VisualSmoothnessVehicleMetal, true);
        for (int side = -1; side <= 1; side += 2)
        {
            CreateBuildingCylinder(root, "GatePost", new Vector3(side * 0.2f, 0.18f, 0.94f), new Vector3(0.05f, 0.22f, 0.05f), new Color(0.28f, 0.29f, 0.31f), VisualSmoothnessVehicleMetal, true);
        }
    }

    private void EnhanceWarehouseModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "WarehouseDetailRoot", center, min, max, anchor, BuildingDecorScale);
        Color wall = new(0.62f, 0.46f, 0.28f);
        Color metal = new(0.32f, 0.34f, 0.36f);
        CreateBuildingBox(root, "WarehouseWallInset", new Vector3(0f, 0.44f, -0.08f), new Vector3(1.65f, 0.56f, 1.44f), wall, VisualSmoothnessBuildingWall, true);
        CreateBuildingBox(root, "WarehouseParapet", new Vector3(0f, 0.86f, -0.08f), new Vector3(1.84f, 0.12f, 1.62f), new Color(0.46f, 0.30f, 0.18f), VisualSmoothnessRoofMetal, true);
        for (int i = 0; i < 3; i++)
        {
            float x = -0.55f + i * 0.55f;
            CreateBuildingBox(root, "RollupDoor", new Vector3(x, 0.26f, 0.73f), new Vector3(0.42f, 0.46f, 0.05f), metal, VisualSmoothnessVehicleMetal, true);
            CreateBuildingBox(root, "DockBumper", new Vector3(x, 0.08f, 0.82f), new Vector3(0.48f, 0.12f, 0.12f), new Color(0.12f, 0.12f, 0.12f), VisualSmoothnessRubber, true);
        }

        CreateBuildingWindowRow(root, new Vector3(-0.62f, 0.68f, 0.74f), new Vector3(0.42f, 0f, 0f), 4, new Vector3(0.22f, 0.14f, 0.04f), new Color(0.46f, 0.72f, 0.82f));
        CreateBuildingCrateStack(root, new Vector3(-0.72f, -0.23f, -0.72f), 6, true);
        CreateBuildingBox(root, "ForkliftHint", new Vector3(0.72f, -0.18f, -0.7f), new Vector3(0.42f, 0.16f, 0.26f), new Color(0.86f, 0.62f, 0.12f), VisualSmoothnessVehicleMetal, true);
    }

    private void EnhanceGasStationModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "GasStationDetailRoot", center, min, max, anchor, BuildingDecorScale);
        Color red = new(0.9f, 0.22f, 0.18f);
        CreateBuildingBox(root, "CanopyTrimFront", new Vector3(0f, 0.84f, 0.46f), new Vector3(2.2f, 0.12f, 0.08f), red, VisualSmoothnessRoofMetal, true);
        CreateBuildingBox(root, "PriceSignPole", new Vector3(-1.05f, 0.48f, 0.82f), new Vector3(0.08f, 0.96f, 0.08f), new Color(0.28f, 0.3f, 0.32f), VisualSmoothnessVehicleMetal, true);
        CreateBuildingBox(root, "PriceSign", new Vector3(-1.05f, 0.96f, 0.82f), new Vector3(0.62f, 0.42f, 0.06f), new Color(0.12f, 0.16f, 0.2f), VisualSmoothnessVehicleMetal, true);
        for (int i = 0; i < 2; i++)
        {
            float x = -0.36f + i * 0.72f;
            CreateBuildingBox(root, "PumpScreen", new Vector3(x, 0.42f, 0.02f), new Vector3(0.18f, 0.12f, 0.03f), new Color(0.56f, 0.84f, 0.92f), VisualSmoothnessGlass, true);
            CreateBuildingBox(root, "PumpHose", new Vector3(x + 0.16f, 0.28f, 0.04f), new Vector3(0.04f, 0.32f, 0.04f), new Color(0.05f, 0.05f, 0.05f), VisualSmoothnessRubber, true);
        }

        CreateBuildingCylinder(root, "TankCap", new Vector3(0.88f, -0.2f, -0.72f), new Vector3(0.16f, 0.025f, 0.16f), new Color(0.34f, 0.36f, 0.38f), VisualSmoothnessVehicleMetal, true);
        CreateBuildingBox(root, "AirWaterBox", new Vector3(0.92f, 0.05f, 0.78f), new Vector3(0.26f, 0.22f, 0.2f), new Color(0.28f, 0.52f, 0.78f), VisualSmoothnessVehicleMetal, true);
    }

    private void EnhanceSawmillModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "SawmillDetailRoot", center, min, max, anchor, BuildingDecorScale);
        Color wood = new(0.60f, 0.40f, 0.18f);
        CreateBuildingBox(root, "OpenMillShed", new Vector3(0f, 0.42f, -0.16f), new Vector3(1.68f, 0.5f, 1.12f), new Color(0.74f, 0.58f, 0.34f), VisualSmoothnessWood, true);
        CreateBuildingBox(root, "MillRoofLong", new Vector3(0f, 0.76f, -0.16f), new Vector3(1.92f, 0.08f, 1.32f), new Color(0.5f, 0.18f, 0.12f), VisualSmoothnessRoofMetal, true);
        CreateBuildingBox(root, "LogDeck", new Vector3(-0.8f, 0.02f, 0.6f), new Vector3(0.56f, 0.12f, 0.72f), new Color(0.34f, 0.22f, 0.12f), VisualSmoothnessWood, true);
        for (int i = 0; i < 4; i++)
        {
            GameObject log = CreateBuildingCylinder(root, "MillLog", new Vector3(-0.8f, 0.16f + i * 0.07f, 0.36f + i * 0.13f), new Vector3(0.08f, 0.48f, 0.08f), wood, VisualSmoothnessWood, true);
            log.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        }

        GameObject blade = CreateBuildingCylinder(root, "SawBlade", new Vector3(0.18f, 0.36f, 0.32f), new Vector3(0.28f, 0.025f, 0.28f), new Color(0.78f, 0.78f, 0.72f), VisualSmoothnessVehicleMetal, true);
        blade.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        CreateBuildingBox(root, "BoardStack", new Vector3(0.72f, 0.08f, 0.58f), new Vector3(0.82f, 0.12f, 0.34f), new Color(0.76f, 0.56f, 0.28f), VisualSmoothnessWood, true);
    }

    private void EnhanceForestCampModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "LumberCampDetailRoot", center, min, max, anchor, BuildingDecorScale);
        CreateBuildingBox(root, "CanvasTent", new Vector3(-0.84f, 0.25f, 0.6f), new Vector3(0.72f, 0.42f, 0.62f), new Color(0.68f, 0.58f, 0.38f), VisualSmoothnessFabric, true);
        CreateBuildingBox(root, "TentRidge", new Vector3(-0.84f, 0.52f, 0.6f), new Vector3(0.82f, 0.08f, 0.72f), new Color(0.54f, 0.44f, 0.28f), VisualSmoothnessFabric, true);
        CreateBuildingBox(root, "ToolRack", new Vector3(0.9f, 0.24f, -0.82f), new Vector3(0.08f, 0.48f, 0.84f), new Color(0.30f, 0.20f, 0.12f), VisualSmoothnessWood, true);
        for (int i = 0; i < 3; i++)
        {
            CreateBuildingBox(root, "AxeHandle", new Vector3(0.86f, 0.28f, -1.06f + i * 0.26f), new Vector3(0.04f, 0.34f, 0.04f), new Color(0.45f, 0.28f, 0.12f), VisualSmoothnessWood, true);
            CreateBuildingBox(root, "AxeHead", new Vector3(0.82f, 0.43f, -1.06f + i * 0.26f), new Vector3(0.12f, 0.08f, 0.04f), new Color(0.62f, 0.64f, 0.62f), VisualSmoothnessVehicleMetal, true);
        }

        CreateBuildingCylinder(root, "CutStump", new Vector3(0.2f, 0.06f, -1.0f), new Vector3(0.18f, 0.08f, 0.18f), new Color(0.42f, 0.26f, 0.12f), VisualSmoothnessWood, true);
        CreateBuildingBox(root, "CampFirePit", new Vector3(0.36f, 0.02f, 0.84f), new Vector3(0.42f, 0.04f, 0.42f), new Color(0.18f, 0.16f, 0.14f), VisualSmoothnessAsphalt, true);
    }

    private void EnhanceBusStopModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "BusStopDetailRoot", center, min, max, anchor, BuildingDecorScale);
        CreateBuildingBox(root, "CurbStripe", new Vector3(0f, -0.16f, 0.48f), new Vector3(1.7f, 0.03f, 0.06f), new Color(0.95f, 0.82f, 0.22f), VisualSmoothnessAsphalt, true);
        CreateBuildingCylinder(root, "StopTrashCan", new Vector3(0.82f, 0.08f, -0.18f), new Vector3(0.11f, 0.16f, 0.11f), new Color(0.24f, 0.28f, 0.30f), VisualSmoothnessVehicleMetal, true);
    }

    private void EnhanceFurnitureFactoryModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "FurnitureFactoryDetailRoot", center, min, max, anchor, BuildingDecorScale);
        for (int i = 0; i < 3; i++)
        {
            CreateBuildingBox(root, "SawtoothRoof", new Vector3(-0.75f + i * 0.55f, 0.94f, -0.2f), new Vector3(0.48f, 0.18f, 1.22f), new Color(0.58f, 0.18f, 0.14f), VisualSmoothnessRoofMetal, true);
            CreateBuildingBox(root, "ClerestoryWindow", new Vector3(-0.75f + i * 0.55f, 1.02f, 0.43f), new Vector3(0.34f, 0.12f, 0.04f), new Color(0.62f, 0.82f, 0.88f), VisualSmoothnessGlass, true);
        }

        CreateBuildingBox(root, "DustCollector", new Vector3(1.1f, 0.72f, 0.46f), new Vector3(0.24f, 0.74f, 0.24f), new Color(0.42f, 0.42f, 0.38f), VisualSmoothnessVehicleMetal, true);
        CreateBuildingCylinder(root, "DustCollectorCap", new Vector3(1.1f, 1.12f, 0.46f), new Vector3(0.18f, 0.06f, 0.18f), new Color(0.56f, 0.54f, 0.48f), VisualSmoothnessVehicleMetal, true);
        CreateBuildingBox(root, "LoadingStripe", new Vector3(0.92f, -0.24f, 0.86f), new Vector3(0.78f, 0.02f, 0.08f), new Color(0.95f, 0.82f, 0.2f), VisualSmoothnessAsphalt, true);
        CreateBuildingCrateStack(root, new Vector3(0.78f, -0.12f, -0.78f), 5, true);
    }

    private void EnhanceMotelModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "MotelDetailRoot", center, min, max, anchor, BuildingDecorScale);
        CreateBuildingWindowRow(root, new Vector3(-0.68f, 0.42f, 0.03f), new Vector3(0.34f, 0f, 0f), 5, new Vector3(0.18f, 0.2f, 0.035f), new Color(0.54f, 0.76f, 0.88f));
        for (int i = 0; i < 4; i++)
        {
            float x = -0.54f + i * 0.36f;
            CreateBuildingBox(root, "RoomDoor", new Vector3(x, 0.23f, 0.08f), new Vector3(0.13f, 0.34f, 0.035f), new Color(0.50f, 0.28f, 0.14f), VisualSmoothnessWood, true);
        }

        CreateBuildingBox(root, "MotelPoleSignPole", new Vector3(1.05f, 0.42f, 0.78f), new Vector3(0.06f, 0.82f, 0.06f), new Color(0.34f, 0.34f, 0.36f), VisualSmoothnessVehicleMetal, true);
        CreateBuildingBox(root, "MotelPoleSign", new Vector3(1.05f, 0.86f, 0.78f), new Vector3(0.5f, 0.24f, 0.05f), new Color(0.96f, 0.76f, 0.16f), VisualSmoothnessVehicleMetal, true);
        CreateBuildingSphere(root, "MotelShrubL", new Vector3(-0.92f, -0.14f, 0.78f), new Vector3(0.28f, 0.18f, 0.28f), new Color(0.24f, 0.46f, 0.2f), VisualSmoothnessDefault, true);
        CreateBuildingSphere(root, "MotelShrubR", new Vector3(-0.56f, -0.14f, 0.78f), new Vector3(0.24f, 0.16f, 0.24f), new Color(0.22f, 0.42f, 0.18f), VisualSmoothnessDefault, true);
    }

    private void EnhanceBarModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "BarDetailRoot", center, min, max, anchor, BuildingDecorScale);
        CreateBuildingBox(root, "BarSideWindowL", new Vector3(-0.58f, 0.48f, 0.82f), new Vector3(0.28f, 0.24f, 0.04f), new Color(0.92f, 0.62f, 0.28f), VisualSmoothnessGlass, true);
        CreateBuildingBox(root, "BarSideWindowR", new Vector3(0.58f, 0.48f, 0.82f), new Vector3(0.28f, 0.24f, 0.04f), new Color(0.92f, 0.62f, 0.28f), VisualSmoothnessGlass, true);
        CreateBuildingBox(root, "BeerGardenTable", new Vector3(-0.82f, 0.02f, 1.0f), new Vector3(0.34f, 0.08f, 0.34f), new Color(0.42f, 0.24f, 0.1f), VisualSmoothnessWood, true);
        CreateBuildingCylinder(root, "BarrelSign", new Vector3(0.86f, 0.16f, 0.92f), new Vector3(0.16f, 0.2f, 0.16f), new Color(0.46f, 0.26f, 0.1f), VisualSmoothnessWood, true);
    }

    private void EnhanceCanteenModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "CanteenDetailRoot", center, min, max, anchor, BuildingDecorScale);
        CreateBuildingBox(root, "KitchenVent", new Vector3(0.78f, 1.02f, -0.24f), new Vector3(0.24f, 0.32f, 0.24f), new Color(0.48f, 0.5f, 0.48f), VisualSmoothnessVehicleMetal, true);
        CreateBuildingBox(root, "MenuBoard", new Vector3(-0.72f, 0.5f, 1.05f), new Vector3(0.5f, 0.34f, 0.045f), new Color(0.16f, 0.20f, 0.18f), VisualSmoothnessVehicleMetal, true);
        CreateBuildingBox(root, "OutdoorCounter", new Vector3(0.48f, 0.1f, 1.04f), new Vector3(0.72f, 0.16f, 0.28f), new Color(0.62f, 0.42f, 0.2f), VisualSmoothnessWood, true);
        for (int i = 0; i < 3; i++)
        {
            CreateBuildingBox(root, "FoodTray", new Vector3(0.14f + i * 0.18f, 0.22f, 1.04f), new Vector3(0.12f, 0.035f, 0.12f), new Color(0.9f, 0.74f, 0.24f), VisualSmoothnessVehicleMetal, true);
        }
    }

    private void EnhanceGamblingHallModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "GamblingHallDetailRoot", center, min, max, anchor, BuildingDecorScale);
        for (int i = 0; i < 5; i++)
        {
            CreateBuildingBox(root, "MarqueeBulb", new Vector3(-0.76f + i * 0.38f, 0.66f, 1.08f), new Vector3(0.08f, 0.08f, 0.04f), new Color(1f, 0.82f, 0.2f), VisualSmoothnessGlass, true);
        }

        CreateBuildingBox(root, "VelvetRopeL", new Vector3(-0.46f, 0.12f, 1.18f), new Vector3(0.5f, 0.04f, 0.04f), new Color(0.86f, 0.1f, 0.18f), VisualSmoothnessFabric, true);
        CreateBuildingBox(root, "VelvetRopeR", new Vector3(0.46f, 0.12f, 1.18f), new Vector3(0.5f, 0.04f, 0.04f), new Color(0.86f, 0.1f, 0.18f), VisualSmoothnessFabric, true);
        CreateBuildingCylinder(root, "MarqueePoleL", new Vector3(-0.78f, 0.22f, 1.18f), new Vector3(0.045f, 0.24f, 0.045f), new Color(0.9f, 0.72f, 0.18f), VisualSmoothnessVehicleMetal, true);
        CreateBuildingCylinder(root, "MarqueePoleR", new Vector3(0.78f, 0.22f, 1.18f), new Vector3(0.045f, 0.24f, 0.045f), new Color(0.9f, 0.72f, 0.18f), VisualSmoothnessVehicleMetal, true);
        CreateBuildingBox(root, "RoofGem", new Vector3(0f, 1.62f, -0.1f), new Vector3(0.32f, 0.22f, 0.32f), new Color(0.24f, 0.92f, 0.86f), VisualSmoothnessGlass, true);
    }

    private void EnhanceCityParkModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        CreateBuildingCylinder(parent, "ParkFountainBase", center + new Vector3(0f, -0.13f, 0f), new Vector3(0.68f, 0.08f, 0.68f), new Color(0.60f, 0.62f, 0.60f), VisualSmoothnessAsphalt);
        CreateBuildingCylinder(parent, "ParkFountainWater", center + new Vector3(0f, -0.03f, 0f), new Vector3(0.48f, 0.035f, 0.48f), new Color(0.24f, 0.58f, 0.82f), VisualSmoothnessGlass);
        CreateBuildingBox(parent, "GazeboFloor", center + new Vector3(2.55f, -0.14f, -2.55f), new Vector3(1.1f, 0.05f, 1.1f), new Color(0.62f, 0.54f, 0.42f), VisualSmoothnessWood);
        CreateBuildingBox(parent, "GazeboRoof", center + new Vector3(2.55f, 0.52f, -2.55f), new Vector3(1.28f, 0.12f, 1.28f), new Color(0.36f, 0.18f, 0.12f), VisualSmoothnessRoofMetal);
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sz = -1; sz <= 1; sz += 2)
            {
                CreateBuildingCylinder(parent, "GazeboPost", center + new Vector3(2.55f + sx * 0.46f, 0.2f, -2.55f + sz * 0.46f), new Vector3(0.05f, 0.34f, 0.05f), new Color(0.46f, 0.30f, 0.16f), VisualSmoothnessWood);
            }
        }
    }

    private void EnhancePersonalHouseModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "HouseDetailRoot", center, min, max, anchor, BuildingDecorScale);
        CreateBuildingBox(root, "HouseWalkway", new Vector3(0f, -0.2f, 1.12f), new Vector3(0.56f, 0.025f, 1.25f), new Color(0.66f, 0.65f, 0.60f), VisualSmoothnessAsphalt, true);
        CreateBuildingBox(root, "MailboxPost", new Vector3(-1.34f, 0.02f, 1.55f), new Vector3(0.05f, 0.32f, 0.05f), new Color(0.36f, 0.24f, 0.14f), VisualSmoothnessWood, true);
        CreateBuildingBox(root, "Mailbox", new Vector3(-1.34f, 0.22f, 1.55f), new Vector3(0.28f, 0.16f, 0.18f), new Color(0.18f, 0.24f, 0.32f), VisualSmoothnessVehicleMetal, true);
        CreateBuildingSphere(root, "FlowerBedL", new Vector3(-0.78f, -0.12f, 0.86f), new Vector3(0.34f, 0.12f, 0.24f), new Color(0.26f, 0.48f, 0.20f), VisualSmoothnessDefault, true);
        CreateBuildingSphere(root, "FlowerBedR", new Vector3(0.78f, -0.12f, 0.86f), new Vector3(0.34f, 0.12f, 0.24f), new Color(0.28f, 0.50f, 0.22f), VisualSmoothnessDefault, true);
    }

    private void EnhanceCarMarketModel(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "CarMarketDetailRoot", center, min, max, anchor, BuildingDecorScale);
        for (int i = 0; i < 5; i++)
        {
            float x = -1.8f + i * 0.9f;
            CreateBuildingBox(root, "DisplayLaneStripe", new Vector3(x, -0.18f, 0.6f), new Vector3(0.05f, 0.02f, 2.8f), new Color(0.88f, 0.86f, 0.76f), VisualSmoothnessAsphalt, true);
        }

        for (int i = 0; i < 4; i++)
        {
            float x = -1.5f + i;
            CreateBuildingCylinder(root, "FlagPole", new Vector3(x, 0.56f, -2.0f), new Vector3(0.035f, 0.62f, 0.035f), new Color(0.78f, 0.78f, 0.74f), VisualSmoothnessVehicleMetal, true);
            CreateBuildingBox(root, "SaleFlag", new Vector3(x + 0.16f, 1.02f, -2.0f), new Vector3(0.32f, 0.18f, 0.035f), i % 2 == 0 ? new Color(0.86f, 0.18f, 0.12f) : new Color(0.96f, 0.78f, 0.18f), VisualSmoothnessFabric, true);
        }

        CreateBuildingBox(root, "FinanceDesk", new Vector3(-1.35f, 0.02f, 0.32f), new Vector3(0.78f, 0.12f, 0.4f), new Color(0.36f, 0.28f, 0.2f), VisualSmoothnessWood, true);
        CreateBuildingBox(root, "Billboard", new Vector3(1.75f, 0.66f, -1.7f), new Vector3(0.92f, 0.44f, 0.06f), new Color(0.95f, 0.82f, 0.22f), VisualSmoothnessVehicleMetal, true);
    }
}
