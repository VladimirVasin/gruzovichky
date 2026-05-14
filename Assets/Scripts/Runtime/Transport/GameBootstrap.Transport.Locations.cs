using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private LocationData CreateLocation(LocationType type, string label, Vector2Int min, Vector2Int max, Vector2Int anchor, Color baseColor, Vector2Int? roadAccess = null)
    {
        return CreateLocation(type, label, min, max, anchor, baseColor, animateConstruction: true, roadAccess: roadAccess);
    }

    private LocationData CreateLocation(LocationType type, string label, Vector2Int min, Vector2Int max, Vector2Int anchor, Color baseColor, bool animateConstruction, Vector2Int? roadAccess = null)
    {
        Vector2Int accessCell = roadAccess ?? anchor;
        PrepareBuildSiteForLocation(type, min, max, accessCell);

        LocationData data = new()
        {
            InstanceId = nextLocationInstanceId++,
            Type = type,
            Label = label,
            Min = min,
            Max = max,
            Anchor = anchor,
            RoadAccess = accessCell,
            BaseColor = baseColor,
            StopNumber = type == LocationType.Stop ? GetNextStopNumber() : 0,
            Workers = 0,
            ServiceFee = type switch
            {
                LocationType.Motel   => 8,
                LocationType.Bar     => 8,
                LocationType.Canteen => 8,
                LocationType.Kiosk   => 4,
                _                    => 0
            },
            BuildingBank  = type == LocationType.GamblingHall ? 50 : 0,
            DocksShipTimer = type == LocationType.Docks ? Random.Range(DocksShipIntervalMin, DocksShipIntervalMax) : 0f,
        };

        GameObject root = new(label);
        root.transform.SetParent(worldRoot, false);
        data.RootObject = root;

        Vector2Int size = new(max.x - min.x + 1, max.y - min.y + 1);
        Vector3 center = new Vector3((min.x + max.x + 1) * 0.5f, 0.35f, (min.y + max.y + 1) * 0.5f);

        GameObject baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseBlock.transform.SetParent(root.transform, false);
        if (type == LocationType.Forest)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.17f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.98f, 0.12f, size.y * 0.98f);
            ApplyColor(baseBlock, new Color(0.42f, 0.3f, 0.18f), VisualSmoothnessWood);
        }
        else if (type == LocationType.Motel)
        {
            // Base block covers only the back half of the footprint (under the building).
            // Anchor direction determines which half is "back" (away from anchor).
            Vector3 toAnchorDir;
            if (anchor.y < min.y) toAnchorDir = new Vector3(0f, 0f, -1f);
            else if (anchor.y > max.y) toAnchorDir = new Vector3(0f, 0f, 1f);
            else if (anchor.x < min.x) toAnchorDir = new Vector3(-1f, 0f, 0f);
            else toAnchorDir = new Vector3(1f, 0f, 0f);

            Vector3 backOffset = -toAnchorDir * 0.5f;
            baseBlock.transform.position = center + backOffset;
            float scaleX = toAnchorDir.z != 0f ? size.x * 0.95f : size.x * 0.47f;
            float scaleZ = toAnchorDir.x != 0f ? size.y * 0.95f : size.y * 0.47f;
            baseBlock.transform.localScale = new Vector3(scaleX, 0.7f, scaleZ);
            ApplyColor(baseBlock, baseColor, VisualSmoothnessBuildingWall);
        }
        else if (type == LocationType.IntercityStop)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.22f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.92f, 0.14f, size.y * 0.64f);
            ApplyColor(baseBlock, new Color(0.78f, 0.74f, 0.68f), VisualSmoothnessAsphalt);
        }
        else if (type == LocationType.Stop)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.22f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.92f, 0.14f, size.y * 0.64f);
            ApplyColor(baseBlock, new Color(0.78f, 0.74f, 0.68f), VisualSmoothnessAsphalt);
        }
        else if (type == LocationType.CityPark)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.24f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.99f, 0.08f, size.y * 0.99f);
            ApplyColor(baseBlock, new Color(0.30f, 0.52f, 0.22f));
        }
        else if (type == LocationType.PersonalHouse)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.24f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.99f, 0.08f, size.y * 0.99f);
            ApplyColor(baseBlock, new Color(0.32f, 0.48f, 0.22f));
        }
        else if (type == LocationType.CarMarket)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.24f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.99f, 0.08f, size.y * 0.99f);
            ApplyColor(baseBlock, new Color(0.18f, 0.19f, 0.20f), VisualSmoothnessAsphalt);
        }
        else if (type == LocationType.CityHall)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.23f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.99f, 0.10f, size.y * 0.99f);
            ApplyColor(baseBlock, new Color(0.42f, 0.44f, 0.48f), VisualSmoothnessAsphalt);
        }
        else if (type == LocationType.Kiosk)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.24f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.99f, 0.08f, size.y * 0.99f);
            ApplyColor(baseBlock, new Color(0.62f, 0.56f, 0.44f), VisualSmoothnessAsphalt);
        }
        else if (type == LocationType.CleaningDepot)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.24f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.99f, 0.08f, size.y * 0.99f);
            ApplyColor(baseBlock, new Color(0.42f, 0.46f, 0.44f), VisualSmoothnessAsphalt);
        }
        else if (type == LocationType.PrimarySchool || type == LocationType.SecondarySchool)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.24f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.99f, 0.08f, size.y * 0.99f);
            ApplyColor(baseBlock, new Color(0.44f, 0.47f, 0.45f), VisualSmoothnessAsphalt);
        }
        else if (type == LocationType.Bar || type == LocationType.GamblingHall)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.26f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.99f, 0.09f, size.y * 0.99f);
            ApplyColor(
                baseBlock,
                type == LocationType.Bar ? new Color(0.34f, 0.22f, 0.14f) : new Color(0.24f, 0.12f, 0.28f),
                VisualSmoothnessWood);
        }
        else if (type == LocationType.Docks)
        {
            baseBlock.transform.position = center + new Vector3(0f, -0.23f, 0f);
            baseBlock.transform.localScale = new Vector3(size.x * 0.99f, 0.10f, size.y * 0.99f);
            ApplyColor(baseBlock, new Color(0.40f, 0.28f, 0.16f), VisualSmoothnessWood);
        }
        else
        {
            baseBlock.transform.position = center;
            baseBlock.transform.localScale = new Vector3(size.x * 0.95f, 0.7f, size.y * 0.95f);
            ApplyColor(baseBlock, baseColor, VisualSmoothnessBuildingWall);
        }

        ConfigureShadowVisual(baseBlock, type == LocationType.CarMarket || type == LocationType.IntercityStop || type == LocationType.Stop
            ? VisualSmoothnessAsphalt
            : type == LocationType.Forest
                ? VisualSmoothnessWood
                : VisualSmoothnessBuildingWall);
        if ((type == LocationType.Bar || type == LocationType.GamblingHall) &&
            baseBlock.TryGetComponent(out Renderer serviceBaseRenderer))
        {
            serviceBaseRenderer.enabled = false;
        }

        data.BaseRenderer = baseBlock.GetComponent<Renderer>();

        if (type == LocationType.Parking)
        {
            CreateParkingDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.GasStation)
        {
            CreateGasStationDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.Forest)
        {
            CreateForestDecoration(data, root.transform, min, max, anchor);
        }
        else if (type == LocationType.Warehouse)
        {
            CreateWarehouseDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.Motel)
        {
            CreateMotelDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.Sawmill)
        {
            CreateSawmillDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.FurnitureFactory)
        {
            CreateFurnitureFactoryDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.IntercityStop)
        {
            CreateBusStopDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.Stop)
        {
            CreateBusStopDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.Bar)
        {
            CreateBarDecoration(data, root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.Canteen)
        {
            CreateCanteenDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.Kiosk)
        {
            CreateVendorStandDecoration(root.transform, center, min, max, anchor, LocationType.Kiosk);
        }
        else if (type == LocationType.GamblingHall)
        {
            CreateGamblingHallDecoration(data, root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.CityPark)
        {
            CreateCityParkDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.PersonalHouse)
        {
            CreatePersonalHouseDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.Kindergarten)
        {
            CreateKindergartenDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.PrimarySchool || type == LocationType.SecondarySchool)
        {
            CreateSchoolDecoration(type, root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.CarMarket)
        {
            CreateCarMarketDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.LaborExchange)
        {
            CreateLaborExchangeDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.CleaningDepot)
        {
            CreateCleaningDepotDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.CityHall)
        {
            CreateCityHallDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.Docks)
        {
            CreateDocksDecoration(root.transform, center, min, max, anchor);
        }
        else
        {
            CreateMotelDecoration(root.transform, center, min, max, anchor);
        }

        CreateLocationTrashCans(type, root.transform, center, min, max, anchor);
        CreateLocationNightLights(data, type, root.transform, center, size);
        CreateLocationWindowLanguage(data, type, root.transform, center, size);

        if (DoesLocationRequireRoadAccess(type))
        {
            GameObject anchorMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            anchorMarker.transform.SetParent(root.transform, false);
            anchorMarker.transform.position = GetCellCenter(anchor) + new Vector3(0f, 0.05f, 0f);
            anchorMarker.transform.localScale = new Vector3(0.22f, 0.02f, 0.22f);
            ApplyColor(anchorMarker, new Color(1f, 0.9f, 0.35f), VisualSmoothnessVehicleMetal);
        }

        HideBuildingLightSourceVisuals(root.transform);

        if (type == LocationType.Stop)
        {
            localStops.Add(data);
            root.transform.position = new Vector3(0f, GetLocationBaseHeight(data), 0f);
            if (worldRoot != null)
            {
                localStopSelectionHighlights.Add(SelectionVisualService.CreateHighlight(
                    worldRoot,
                    data.Label,
                    ApplyColor,
                    ConfigureStaticVisual));
            }
            UpdateLocalBusStopNetworkWarnings();
        }
        else if (type == LocationType.PersonalHouse)
        {
            personalHouses.Add(data);
            root.transform.position = new Vector3(0f, GetLocationBaseHeight(data), 0f);
            if (worldRoot != null)
            {
                personalHouseSelectionHighlights.Add(SelectionVisualService.CreateHighlight(
                    worldRoot,
                    data.Label,
                    ApplyColor,
                    ConfigureStaticVisual));
            }
        }
        else if (locations.ContainsKey(type) && IsMultiInstanceLocationType(type))
        {
            extraServiceLocations.Add(data);
            root.transform.position = new Vector3(0f, GetLocationBaseHeight(data), 0f);
        }
        else
        {
            locations[type] = data;
            root.transform.position = new Vector3(0f, GetLocationBaseHeight(type), 0f);
            if (worldRoot != null && !locationSelectionHighlights.ContainsKey(type))
            {
                locationSelectionHighlights[type] = SelectionVisualService.CreateHighlight(
                    worldRoot,
                    data.Label,
                    ApplyColor,
                    ConfigureStaticVisual);
            }
        }

        if (DoesLocationRequireRoadAccess(type))
        {
            EnsureLocationRoadAccessRoadCell(data, type.ToString());
        }

        UpdateRoadAccessWarningMarkers();
        NotifyNewGameBuildUnlockProgressionBuilt(type);
        NotifyCityComplaintServiceBuilt(type);
        if (animateConstruction)
        {
            StartLocationConstructionAnimation(data);
        }

        return data;
    }

    private static bool DoesLocationRequireRoadAccess(LocationType type) => type switch
    {
        LocationType.Parking          => true,
        LocationType.GasStation       => true,
        LocationType.Forest           => true,
        LocationType.Warehouse        => true,
        LocationType.Sawmill          => true,
        LocationType.FurnitureFactory => true,
        LocationType.Docks            => true,
        LocationType.Stop             => true,
        _                             => false
    };

    private static bool IsMultiInstanceLocationType(LocationType type) => type switch
    {
        LocationType.Bar              => true,
        LocationType.Canteen          => true,
        LocationType.Kiosk            => true,
        LocationType.GamblingHall     => true,
        LocationType.GasStation       => true,
        LocationType.CityPark         => true,
        LocationType.Kindergarten     => true,
        LocationType.PrimarySchool    => true,
        LocationType.SecondarySchool => true,
        LocationType.Forest           => true,
        LocationType.Sawmill          => true,
        LocationType.FurnitureFactory => true,
        LocationType.Warehouse        => true,
        LocationType.Docks            => true,
        _                             => false
    };

    private static bool IsMultiInstanceServiceBuildType(LocationType type) => IsMultiInstanceLocationType(type);

    private void CreateLocationTrashCans(LocationType type, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        if (type == LocationType.Stop || type == LocationType.IntercityStop)
        {
            return;
        }

        int targetCount = type switch
        {
            LocationType.CityPark      => 4,
            LocationType.CarMarket     => 3,
            LocationType.CityHall      => 3,
            LocationType.Kindergarten  => 2,
            LocationType.PrimarySchool => 2,
            LocationType.SecondarySchool => 3,
            LocationType.CleaningDepot => 2,
            LocationType.Parking       => 3,
            LocationType.Warehouse     => 2,
            LocationType.GasStation    => 2,
            LocationType.Sawmill       => 2,
            LocationType.FurnitureFactory => 2,
            _                          => 1
        };

        Vector3[] candidatePositions =
        {
            new(min.x - 0.28f, 0.12f, min.y + 0.38f),
            new(max.x + 1.28f, 0.12f, min.y + 0.38f),
            new(min.x - 0.28f, 0.12f, max.y + 0.62f),
            new(max.x + 1.28f, 0.12f, max.y + 0.62f),
            new(center.x, 0.12f, min.y - 0.26f),
            new(center.x, 0.12f, max.y + 1.26f),
            new(min.x - 0.26f, 0.12f, center.z),
            new(max.x + 1.26f, 0.12f, center.z),
        };

        int placed = 0;
        int startIndex = Mathf.Abs((int)type + min.x * 7 + min.y * 11 + anchor.x * 13 + anchor.y * 17) % candidatePositions.Length;
        for (int i = 0; i < candidatePositions.Length && placed < targetCount; i++)
        {
            Vector3 pos = candidatePositions[(startIndex + i) % candidatePositions.Length];
            Vector2Int cell = new(Mathf.FloorToInt(pos.x), Mathf.FloorToInt(pos.z));
            if (!IsInsideGrid(cell) || waterCells.Contains(cell) || edgeHighwayCells.Contains(cell))
            {
                continue;
            }

            CreateTrashCan(parent, pos, placed);
            RegisterTrashCanMealTarget(pos);
            placed++;
        }
    }

    private void RegisterTrashCanMealTarget(Vector3 localPosition)
    {
        Vector3 target = localPosition;
        target.y = SampleTerrainHeight(localPosition.x, localPosition.z);
        locationTrashCanMealTargets.Add(target);
    }

    private void CreateTrashCan(Transform parent, Vector3 localPosition, int variant)
    {
        GameObject root = new("TrashCan");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = localPosition;
        root.transform.localRotation = Quaternion.Euler(0f, variant * 37f, 0f);

        Color bodyColor = variant % 2 == 0
            ? new Color(0.18f, 0.25f, 0.22f)
            : new Color(0.22f, 0.22f, 0.24f);
        Color rimColor = new Color(0.08f, 0.09f, 0.09f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        body.transform.localScale = new Vector3(0.18f, 0.22f, 0.18f);
        ApplyColor(body, bodyColor, VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(body, VisualSmoothnessVehicleMetal);
        if (body.TryGetComponent(out Collider bodyCollider)) bodyCollider.enabled = false;

        GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lid.transform.SetParent(root.transform, false);
        lid.transform.localPosition = new Vector3(0f, 0.39f, 0f);
        lid.transform.localScale = new Vector3(0.21f, 0.035f, 0.21f);
        ApplyColor(lid, rimColor, VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(lid, VisualSmoothnessVehicleMetal);
        if (lid.TryGetComponent(out Collider lidCollider)) lidCollider.enabled = false;

        for (int side = -1; side <= 1; side += 2)
        {
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handle.transform.SetParent(root.transform, false);
            handle.transform.localPosition = new Vector3(side * 0.12f, 0.27f, 0f);
            handle.transform.localScale = new Vector3(0.035f, 0.10f, 0.04f);
            ApplyColor(handle, rimColor, VisualSmoothnessVehicleMetal);
            ConfigureStaticVisual(handle, VisualSmoothnessVehicleMetal);
            if (handle.TryGetComponent(out Collider handleCollider)) handleCollider.enabled = false;
        }
    }

    private void CreateParkingDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Vector3 ScaleOffset(Vector3 offset) => offset * BuildingDecorScale;
        Vector3 ScaleSize(Vector3 size) => size * BuildingDecorScale;

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        canopy.transform.SetParent(parent, false);
        canopy.transform.position = center + ScaleOffset(new Vector3(0f, 0.6f, -0.15f));
        canopy.transform.localScale = ScaleSize(new Vector3(2.8f, 0.12f, 1.4f));
        ApplyColor(canopy, new Color(0.18f, 0.2f, 0.24f), VisualSmoothnessRoofMetal);

        Vector3[] postOffsets =
        {
            new(-1.15f, 0.28f, -0.55f),
            new(1.15f, 0.28f, -0.55f),
            new(-1.15f, 0.28f, 0.25f),
            new(1.15f, 0.28f, 0.25f)
        };

        foreach (Vector3 offset in postOffsets)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(parent, false);
            post.transform.position = center + ScaleOffset(offset);
            post.transform.localScale = ScaleSize(new Vector3(0.12f, 0.56f, 0.12f));
            ApplyColor(post, new Color(0.3f, 0.32f, 0.36f), VisualSmoothnessVehicleMetal);
        }

        CreateDrivewayToAnchor(parent, min, max, anchor, 0.62f);
        EnhanceParkingModel(parent, center, min, max, anchor);
    }

    private void CreateBusStopDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        GameObject shelterRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shelterRoof.transform.SetParent(parent, false);
        shelterRoof.transform.position = center + new Vector3(0f, 0.72f, 0.05f);
        shelterRoof.transform.localScale = new Vector3(1.55f, 0.08f, 0.52f);
        ApplyColor(shelterRoof, new Color(0.86f, 0.22f, 0.18f), VisualSmoothnessRoofMetal);
        ConfigureStaticVisual(shelterRoof, VisualSmoothnessRoofMetal);

        Vector3[] postOffsets =
        {
            new(-0.58f, 0.33f, -0.1f),
            new(0.58f, 0.33f, -0.1f)
        };

        foreach (Vector3 offset in postOffsets)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(parent, false);
            post.transform.position = center + offset;
            post.transform.localScale = new Vector3(0.08f, 0.62f, 0.08f);
            ApplyColor(post, new Color(0.28f, 0.3f, 0.34f), VisualSmoothnessVehicleMetal);
            ConfigureStaticVisual(post, VisualSmoothnessVehicleMetal);
        }

        GameObject backPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backPanel.transform.SetParent(parent, false);
        backPanel.transform.position = center + new Vector3(0f, 0.38f, 0.18f);
        backPanel.transform.localScale = new Vector3(1.4f, 0.5f, 0.06f);
        ApplyColor(backPanel, new Color(0.9f, 0.92f, 0.95f), VisualSmoothnessBuildingWall);
        ConfigureStaticVisual(backPanel, VisualSmoothnessBuildingWall);

        GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bench.transform.SetParent(parent, false);
        bench.transform.position = center + new Vector3(0f, 0.16f, -0.05f);
        bench.transform.localScale = new Vector3(0.88f, 0.08f, 0.2f);
        ApplyColor(bench, new Color(0.5f, 0.34f, 0.2f), VisualSmoothnessWood);
        ConfigureStaticVisual(bench, VisualSmoothnessWood);

        GameObject stopPole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stopPole.transform.SetParent(parent, false);
        stopPole.transform.position = center + new Vector3(0.92f, 0.5f, 0.16f);
        stopPole.transform.localScale = new Vector3(0.06f, 1f, 0.06f);
        ApplyColor(stopPole, new Color(0.26f, 0.28f, 0.32f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(stopPole, VisualSmoothnessVehicleMetal);

        GameObject stopSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stopSign.transform.SetParent(parent, false);
        stopSign.transform.position = center + new Vector3(0.92f, 0.92f, 0.16f);
        stopSign.transform.localScale = new Vector3(0.34f, 0.28f, 0.04f);
        ApplyColor(stopSign, new Color(0.95f, 0.84f, 0.2f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(stopSign, VisualSmoothnessVehicleMetal);

        EnhanceBusStopModel(parent, center, min, max, anchor);
    }

    private void CreateForestDecoration(LocationData forestLocation, Transform parent, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        forestWorkPoints.Clear();
        forestWorkTargetTrees.Clear();
        forestTreeWobbles.Clear();
        Vector3 center = new Vector3((min.x + max.x + 1) * 0.5f, 0.02f, (min.y + max.y + 1) * 0.5f);

        GameObject yard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        yard.transform.SetParent(parent, false);
        yard.transform.position = center + new Vector3(0f, 0.03f, 0f);
        yard.transform.localScale = new Vector3((max.x - min.x + 1) * 0.92f, 0.06f, (max.y - min.y + 1) * 0.92f);
        ApplyColor(yard, new Color(0.46f, 0.34f, 0.22f), VisualSmoothnessWood);
        ConfigureStaticVisual(yard, VisualSmoothnessWood);

        Vector3 buildingCenter = center + new Vector3(0f, 0.36f, 0.22f);
        GameObject shed = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shed.transform.SetParent(parent, false);
        shed.transform.position = buildingCenter;
        shed.transform.localScale = new Vector3(1.35f, 0.66f, 1.12f);
        ApplyColor(shed, new Color(0.62f, 0.46f, 0.28f), VisualSmoothnessWood);
        ConfigureStaticVisual(shed, VisualSmoothnessWood);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = buildingCenter + new Vector3(0f, 0.44f, 0f);
        roof.transform.localScale = new Vector3(1.55f, 0.12f, 1.34f);
        ApplyColor(roof, new Color(0.72f, 0.18f, 0.12f), VisualSmoothnessRoofMetal);
        ConfigureStaticVisual(roof, VisualSmoothnessRoofMetal);

        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.transform.SetParent(parent, false);
        door.transform.position = buildingCenter + new Vector3(0f, -0.06f, -0.57f);
        door.transform.localScale = new Vector3(0.42f, 0.54f, 0.08f);
        ApplyColor(door, new Color(0.22f, 0.14f, 0.08f), VisualSmoothnessWood);
        ConfigureStaticVisual(door, VisualSmoothnessWood);

        Vector3[] fencePosts =
        {
            center + new Vector3(-0.95f, 0.2f, -0.8f),
            center + new Vector3(0.95f, 0.2f, -0.8f),
            center + new Vector3(-0.95f, 0.2f, 0.95f),
            center + new Vector3(0.95f, 0.2f, 0.95f)
        };

        foreach (Vector3 fencePos in fencePosts)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(parent, false);
            post.transform.position = fencePos;
            post.transform.localScale = new Vector3(0.08f, 0.42f, 0.08f);
            ApplyColor(post, new Color(0.35f, 0.23f, 0.14f), VisualSmoothnessWood);
            ConfigureStaticVisual(post, VisualSmoothnessWood);
        }

        Vector3 depotPos = GetCellCenter(GetForestDepotCell(min, max, anchor)) + new Vector3(0f, 0.12f, 0f);
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.transform.SetParent(parent, false);
        marker.transform.position = depotPos;
        marker.transform.localScale = new Vector3(0.52f, 0.16f, 0.82f);
        ApplyColor(marker, new Color(0.47f, 0.31f, 0.18f), VisualSmoothnessWood);
        ConfigureStaticVisual(marker, VisualSmoothnessWood);

        CreateForestStoredLogsVisuals(forestLocation, parent, depotPos + new Vector3(0f, 0.08f, 0.58f));
        RefreshForestStoredLogsVisual(forestLocation);
        TryCreateSquirrelMemorialSign(parent, center + new Vector3(-1.05f, 0.06f, -1.26f), Quaternion.identity);
        EnhanceForestCampModel(parent, center, min, max, anchor);

        SessionDebugLogger.Log("WORLD", "Built Lumberyard decoration with depot yard and storage stack.");
    }

    private int GetNextStopNumber()
    {
        NormalizeLocalStopNumbers();
        return localStops.Count + 1;
    }

    private void NormalizeLocalStopNumbers()
    {
        if (localStops.Count == 0)
        {
            return;
        }

        List<LocationData> orderedStops = GetOrderedLocalStops();

        for (int i = 0; i < orderedStops.Count; i++)
        {
            orderedStops[i].StopNumber = i + 1;
        }
    }

    private void ShowLocalBusStopMinimumHintIfNeeded()
    {
        if (hasShownLocalBusStopMinimumHint || localStops.Count >= 2)
        {
            return;
        }

        hasShownLocalBusStopMinimumHint = true;
        PushFeedEvent(
            "Bus network is offline: build at least 2 local bus stops.",
            "\u0410\u0432\u0442\u043e\u0431\u0443\u0441\u043d\u0430\u044f \u0441\u0435\u0442\u044c \u043d\u0435 \u0440\u0430\u0431\u043e\u0442\u0430\u0435\u0442: \u043d\u0443\u0436\u043d\u044b \u043c\u0438\u043d\u0438\u043c\u0443\u043c 2 \u043e\u0441\u0442\u0430\u043d\u043e\u0432\u043a\u0438.",
            FeedEventType.Warning);
    }

    private void UpdateLocalBusStopNetworkWarnings()
    {
        bool shouldWarn = localStops.Count == 1;
        for (int i = 0; i < localStops.Count; i++)
        {
            LocationData stop = localStops[i];
            if (stop?.RootObject == null)
            {
                continue;
            }

            if (stop.LocalBusWarningMarker == null)
            {
                stop.LocalBusWarningMarker = CreateLocalBusStopWarningMarker(stop);
            }

            if (stop.LocalBusWarningMarker != null)
            {
                stop.LocalBusWarningMarker.SetActive(shouldWarn);
            }
        }
    }

    private GameObject CreateLocalBusStopWarningMarker(LocationData stop)
    {
        GameObject root = new($"LocalBusStopWarning_{stop.StopNumber}");
        root.transform.SetParent(stop.RootObject.transform, false);

        Vector3 center = new(
            (stop.Min.x + stop.Max.x + 1) * 0.5f,
            1.55f,
            (stop.Min.y + stop.Max.y + 1) * 0.5f);
        root.transform.localPosition = center;

        GameObject backplate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backplate.name = "WarningBackplate";
        backplate.transform.SetParent(root.transform, false);
        backplate.transform.localPosition = Vector3.zero;
        backplate.transform.localScale = new Vector3(0.34f, 0.34f, 0.06f);
        ApplyColor(backplate, new Color(0.95f, 0.72f, 0.16f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(backplate, VisualSmoothnessVehicleMetal);
        if (backplate.TryGetComponent(out Collider backplateCollider))
        {
            backplateCollider.enabled = false;
        }

        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = "WarningLine";
        line.transform.SetParent(root.transform, false);
        line.transform.localPosition = new Vector3(0f, 0.05f, -0.04f);
        line.transform.localScale = new Vector3(0.055f, 0.18f, 0.035f);
        ApplyColor(line, new Color(0.12f, 0.10f, 0.08f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(line, VisualSmoothnessVehicleMetal);
        if (line.TryGetComponent(out Collider lineCollider))
        {
            lineCollider.enabled = false;
        }

        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dot.name = "WarningDot";
        dot.transform.SetParent(root.transform, false);
        dot.transform.localPosition = new Vector3(0f, -0.11f, -0.04f);
        dot.transform.localScale = new Vector3(0.055f, 0.055f, 0.035f);
        ApplyColor(dot, new Color(0.12f, 0.10f, 0.08f), VisualSmoothnessVehicleMetal);
        ConfigureStaticVisual(dot, VisualSmoothnessVehicleMetal);
        if (dot.TryGetComponent(out Collider dotCollider))
        {
            dotCollider.enabled = false;
        }

        root.SetActive(false);
        return root;
    }

    private List<LocationData> GetOrderedLocalStops()
    {
        List<LocationData> orderedStops = new(localStops);
        List<BusStopOrderKey> orderKeys = new();
        for (int i = 0; i < orderedStops.Count; i++)
        {
            orderKeys.Add(new BusStopOrderKey(i, orderedStops[i].StopNumber, orderedStops[i].Anchor));
        }

        List<int> orderedIndices = BusStopOrderingService.GetOrderedIndices(orderKeys);
        List<LocationData> result = new();
        for (int i = 0; i < orderedIndices.Count; i++)
        {
            result.Add(orderedStops[orderedIndices[i]]);
        }

        return result;
    }

    private void CreateForestStoredLogsVisuals(LocationData forestLocation, Transform parent, Vector3 basePosition)
    {
        if (forestLocation == null)
        {
            return;
        }

        forestLocation.StoredLogVisuals.Clear();
        for (int i = 0; i < ForestMaxLogsStorage; i++)
        {
            int row = i / 5;
            int column = i % 5;
            GameObject storedLog = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            storedLog.name = $"StoredLog_{i + 1}";
            storedLog.transform.SetParent(parent, false);
            storedLog.transform.position = basePosition + new Vector3(-0.36f + column * 0.18f, row * 0.12f, 0f);
            storedLog.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            storedLog.transform.localScale = new Vector3(0.11f, 0.2f, 0.11f);
            ApplyColor(storedLog, new Color(0.58f, 0.4f, 0.22f), VisualSmoothnessWood);
            ConfigureStaticVisual(storedLog, VisualSmoothnessWood);
            forestLocation.StoredLogVisuals.Add(storedLog);
        }
    }

    private void RefreshForestStoredLogsVisual()
    {
        if (!locations.TryGetValue(LocationType.Forest, out LocationData forestLocation))
        {
            return;
        }

        RefreshForestStoredLogsVisual(forestLocation);
    }

    private void RefreshForestStoredLogsVisual(LocationData forestLocation)
    {
        if (forestLocation == null)
        {
            return;
        }

        int visibleLogs = Mathf.Clamp(forestLocation.LogsStored, 0, ForestMaxLogsStorage);
        for (int i = 0; i < forestLocation.StoredLogVisuals.Count; i++)
        {
            if (forestLocation.StoredLogVisuals[i] != null)
            {
                forestLocation.StoredLogVisuals[i].SetActive(i < visibleLogs);
            }
        }
    }

    private void TryAddForestLogFromChop()
    {
        if (!locations.TryGetValue(LocationType.Forest, out LocationData forestLocation) ||
            forestLocation.LogsStored >= ForestMaxLogsStorage)
        {
            forestProductionProgress = 0f;
            return;
        }

        forestProductionProgress += ForestLogProgressPerChop;
        if (forestProductionProgress < 1f)
        {
            return;
        }

        forestProductionProgress -= 1f;
        forestLocation.LogsStored = Mathf.Min(ForestMaxLogsStorage, forestLocation.LogsStored + 1);
        RefreshForestStoredLogsVisual();
        SessionDebugLogger.Log("FOREST", $"Forest produced logs. Storage is now {forestLocation.LogsStored}/{ForestMaxLogsStorage}.");
    }

    private Vector2Int GetForestDepotCell(Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        if (anchor.y < min.y)
        {
            return new Vector2Int(Mathf.Clamp(anchor.x, min.x, max.x), min.y);
        }

        if (anchor.y > max.y)
        {
            return new Vector2Int(Mathf.Clamp(anchor.x, min.x, max.x), max.y);
        }

        if (anchor.x < min.x)
        {
            return new Vector2Int(min.x, Mathf.Clamp(anchor.y, min.y, max.y));
        }

        if (anchor.x > max.x)
        {
            return new Vector2Int(max.x, Mathf.Clamp(anchor.y, min.y, max.y));
        }

        return new Vector2Int((min.x + max.x) / 2, (min.y + max.y) / 2);
    }


}


