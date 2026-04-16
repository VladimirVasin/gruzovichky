using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private const float TreeHeightScale = 1.1f;
    private const float BerryBushSpawnChance = 0.28f;
    private const float FlowerPatchSpawnChance = 0.18f;

    private void SetupLocations()
    {
        locations.Clear();

        GeneratedWorldLayout layout = WorldLayoutGenerator.Generate(GridWidth, GridHeight, waterCells, HasRequiredLayoutRoads);
        CreateLocation(LocationType.Parking, "Parking", layout.Parking.Min, layout.Parking.Max, layout.Parking.Anchor, new Color(0.46f, 0.46f, 0.52f));
        CreateLocation(LocationType.GasStation, "Gas Station", layout.GasStation.Min, layout.GasStation.Max, layout.GasStation.Anchor, new Color(0.84f, 0.68f, 0.26f));
        CreateLocation(LocationType.Forest, "Forest", layout.Forest.Min, layout.Forest.Max, layout.Forest.Anchor, new Color(0.22f, 0.55f, 0.24f));
        CreateLocation(LocationType.Warehouse, "Warehouse", layout.Warehouse.Min, layout.Warehouse.Max, layout.Warehouse.Anchor, new Color(0.7f, 0.52f, 0.3f));
        if (selectedGameStartMode == GameStartMode.Debug)
        {
            CreateLocation(LocationType.Sawmill, "Sawmill", layout.Sawmill.Min, layout.Sawmill.Max, layout.Sawmill.Anchor, new Color(0.3f, 0.52f, 0.8f));
            CreateLocation(LocationType.Motel, "Motel", layout.Motel.Min, layout.Motel.Max, layout.Motel.Anchor, new Color(0.91f, 0.87f, 0.74f));
        }

        CreateLocation(LocationType.BusStop, "Bus Stop", layout.BusStop.Min, layout.BusStop.Max, layout.BusStop.Anchor, new Color(0.82f, 0.24f, 0.22f));

        SessionDebugLogger.Log(
            "WORLD",
            selectedGameStartMode == GameStartMode.Debug
                ? $"Generated debug layout: Parking {FormatPlacement(layout.Parking)}, GasStation {FormatPlacement(layout.GasStation)}, Forest {FormatPlacement(layout.Forest)}, Warehouse {FormatPlacement(layout.Warehouse)}, Sawmill {FormatPlacement(layout.Sawmill)}, Motel {FormatPlacement(layout.Motel)}, BusStop {FormatPlacement(layout.BusStop)}."
                : $"Generated user layout: Parking {FormatPlacement(layout.Parking)}, GasStation {FormatPlacement(layout.GasStation)}, Forest {FormatPlacement(layout.Forest)}, Warehouse {FormatPlacement(layout.Warehouse)}, BusStop {FormatPlacement(layout.BusStop)}. Motel/Sawmill skipped for Build menu.");
    }

    private bool HasRequiredLayoutRoads(GeneratedWorldLayout layout)
    {
        if (FindRoadBuildPath(layout.Parking.Anchor, layout.GasStation.Anchor, cell => IsPlacementCell(layout, cell)) == null ||
            FindRoadBuildPath(layout.GasStation.Anchor, layout.Warehouse.Anchor, cell => IsPlacementCell(layout, cell)) == null ||
            FindRoadBuildPath(layout.Warehouse.Anchor, layout.Forest.Anchor, cell => IsPlacementCell(layout, cell)) == null)
        {
            return false;
        }

        if (selectedGameStartMode == GameStartMode.User)
        {
            return FindRoadBuildPath(layout.Warehouse.Anchor, layout.BusStop.Anchor, cell => IsPlacementCell(layout, cell)) != null;
        }

        return FindRoadBuildPath(layout.Forest.Anchor, layout.Sawmill.Anchor, cell => IsPlacementCell(layout, cell)) != null &&
               FindRoadBuildPath(layout.Sawmill.Anchor, layout.Warehouse.Anchor, cell => IsPlacementCell(layout, cell)) != null &&
               FindRoadBuildPath(layout.Warehouse.Anchor, layout.Motel.Anchor, cell => IsPlacementCell(layout, cell)) != null &&
               FindRoadBuildPath(layout.Motel.Anchor, layout.BusStop.Anchor, cell => IsPlacementCell(layout, cell)) != null;
    }

    private static bool IsPlacementCell(GeneratedWorldLayout layout, Vector2Int cell)
    {
        foreach (WorldLocationPlacement placement in layout.GetAllPlacements())
        {
            if (placement.Anchor == cell || placement.Contains(cell))
            {
                return true;
            }
        }

        return false;
    }

    private void GenerateTerrainHeights()
    {
        terrainHeights = TerrainHeightGenerator.Generate(GridWidth, GridHeight, GetWorldPlacements());
    }

    private IEnumerable<WorldLocationPlacement> GetWorldPlacements()
    {
        foreach (LocationData location in locations.Values)
        {
            yield return new WorldLocationPlacement
            {
                Min = location.Min,
                Max = location.Max,
                Anchor = location.Anchor
            };
        }
    }

    private void ApplyTerrainHeightsToWorld()
    {
        foreach (KeyValuePair<LocationType, LocationData> pair in locations)
        {
            if (pair.Value.RootObject != null)
            {
                pair.Value.RootObject.transform.position = new Vector3(0f, GetLocationBaseHeight(pair.Key), 0f);
            }
        }

        foreach (KeyValuePair<Vector2Int, GameObject> pair in roadVisuals)
        {
            if (pair.Value != null)
            {
                pair.Value.transform.position = GetCellCenter(pair.Key) + new Vector3(0f, RoadHeight, 0f);
            }
        }
    }

    private void PopulateMiscTrees()
    {
        if (miscRoot == null)
        {
            return;
        }

        miscOccupiedCells.Clear();
        miscTreeSways.Clear();
        miscTreePerchPoints.Clear();
        flowerBeePoints.Clear();
        List<Vector2Int> plannedCells = MiscTreePlanner.Plan(
            GridWidth,
            GridHeight,
            roadCells,
            edgeHighwayCells,
            IsLocationCell,
            cell => IsGrassGroundCell(cell.x, cell.y) && !IsWaterOrBeachCell(cell));
        int bushCount = 0;
        int flowerCount = 0;
        SessionDebugLogger.Log("WORLD", $"Planning {plannedCells.Count} misc cells");
        for (int i = 0; i < plannedCells.Count; i++)
        {
            float roll = Random.value;
            if (roll < FlowerPatchSpawnChance)
            {
                CreateFlowerPatch(plannedCells[i], i);
                flowerCount++;
            }
            else if (roll < FlowerPatchSpawnChance + BerryBushSpawnChance)
            {
                CreateBerryBush(plannedCells[i], i);
                bushCount++;
            }
            else
            {
                CreateMiscTree(plannedCells[i], i % 6);
            }
        }

        SessionDebugLogger.Log("WORLD", $"Placed {plannedCells.Count} misc props ({plannedCells.Count - bushCount - flowerCount} trees, {bushCount} berry bushes, {flowerCount} flower patches).");
    }

    private static string FormatPlacement(WorldLocationPlacement placement)
    {
        return $"min({placement.Min.x},{placement.Min.y}) max({placement.Max.x},{placement.Max.y}) anchor({placement.Anchor.x},{placement.Anchor.y})";
    }

    private bool GetFurnitureFactoryPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        previewPosition = GetCellCenter(anchorCell) + new Vector3(0f, RoadHeight + 0.03f, 0f);
        previewScale = new Vector3(0.98f, 0.04f, 0.98f);
        GetRotatedBuildingFootprint(anchorCell, 3, 2, out Vector2Int previewMin, out Vector2Int previewMax);
        SetBuildFootprintPreviewCells(previewMin, previewMax, anchorCell);
        bool canPlace = TryGetFurnitureFactoryPlacement(anchorCell, out Vector2Int min, out Vector2Int max);
        if (!canPlace)
        {
            return false;
        }

        float centerX = (min.x + max.x + 1) * 0.5f;
        float centerZ = (min.y + max.y + 1) * 0.5f;
        previewPosition = new Vector3(centerX, SampleTerrainHeight(centerX, centerZ) + RoadHeight + 0.03f, centerZ);
        previewScale = new Vector3((max.x - min.x + 1) * 0.94f, 0.04f, (max.y - min.y + 1) * 0.94f);
        return true;
    }

    private bool TryPlaceFurnitureFactoryAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.FurnitureFactory))
        {
            SessionDebugLogger.Log("BUILD", "Furniture Factory placement rejected: factory already exists.");
            return false;
        }

        if (!TryGetFurnitureFactoryPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Furniture Factory placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.FurnitureFactory, "Furniture Factory", min, max, anchorCell, new Color(0.74f, 0.62f, 0.42f));
        selectedLocation = LocationType.FurnitureFactory;
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RefreshSelectionVisuals();
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        SessionDebugLogger.Log("BUILD", $"Placed Furniture Factory at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool TryPlaceBarAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.Bar))
        {
            SessionDebugLogger.Log("BUILD", "Bar placement rejected: bar already exists.");
            return false;
        }

        if (!TryGetBarPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Bar placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.Bar, "Bar", min, max, anchorCell, new Color(0.38f, 0.18f, 0.12f));
        selectedLocation = LocationType.Bar;
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RefreshSelectionVisuals();
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        SessionDebugLogger.Log("BUILD", $"Placed Bar at anchor ({anchorCell.x},{anchorCell.y}).");
        return true;
    }

    private bool TryPlaceCanteenAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.Canteen))
        {
            SessionDebugLogger.Log("BUILD", "Canteen placement rejected: canteen already exists.");
            return false;
        }

        if (!TryGetTwoByTwoBuildingPlacement(anchorCell, LocationType.Canteen, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Canteen placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.Canteen, "Canteen", min, max, anchorCell, new Color(0.58f, 0.42f, 0.24f));
        selectedLocation = LocationType.Canteen;
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RefreshSelectionVisuals();
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        SessionDebugLogger.Log("BUILD", $"Placed Canteen at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool TryPlaceSawmillAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.Sawmill))
        {
            SessionDebugLogger.Log("BUILD", "Sawmill placement rejected: sawmill already exists.");
            return false;
        }

        if (!TryGetTwoByTwoBuildingPlacement(anchorCell, LocationType.Sawmill, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Sawmill placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.Sawmill, "Sawmill", min, max, anchorCell, new Color(0.3f, 0.52f, 0.8f));
        selectedLocation = LocationType.Sawmill;
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RefreshSelectionVisuals();
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        SessionDebugLogger.Log("BUILD", $"Placed Sawmill at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool TryPlaceMotelAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.Motel))
        {
            SessionDebugLogger.Log("BUILD", "Motel placement rejected: motel already exists.");
            return false;
        }

        if (!TryGetTwoByTwoBuildingPlacement(anchorCell, LocationType.Motel, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Motel placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.Motel, "Motel", min, max, anchorCell, new Color(0.91f, 0.87f, 0.74f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        RefreshSelectionVisuals();
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        TryShowTutorial(TutorialTrigger.FirstMotelBuilt);
        SessionDebugLogger.Log("BUILD", $"Placed Motel at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool TryGetBarPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        // Anchor is on the road side; building footprint is 2×2 one row north of anchor
        return TryGetTwoByTwoBuildingPlacement(anchorCell, LocationType.Bar, out min, out max);
    }

    private bool TryGetTwoByTwoBuildingPlacement(Vector2Int anchorCell, LocationType type, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, type, 2, 2, out min, out max);
    }

    private bool TryGetRotatedBuildingPlacement(Vector2Int anchorCell, LocationType type, int width, int depth, out Vector2Int min, out Vector2Int max)
    {
        GetRotatedBuildingFootprint(anchorCell, width, depth, out min, out max);

        if (locations.ContainsKey(type))
        {
            return false;
        }

        if (!IsInsideGrid(anchorCell) || !IsInsideGrid(min) || !IsInsideGrid(max))
        {
            return false;
        }

        if (roadCells.Contains(anchorCell) || edgeHighwayCells.Contains(anchorCell) ||
            miscOccupiedCells.Contains(anchorCell) || IsLocationCell(anchorCell) || IsWaterOrBeachCell(anchorCell))
        {
            return false;
        }

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector2Int cell = new(x, y);
                if (!IsInsideGrid(cell) || roadCells.Contains(cell) || edgeHighwayCells.Contains(cell) ||
                    miscOccupiedCells.Contains(cell) || IsLocationCell(cell) || IsWaterOrBeachCell(cell))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void GetRotatedBuildingFootprint(Vector2Int anchorCell, int width, int depth, out Vector2Int min, out Vector2Int max)
    {
        int left = width / 2;
        int right = width - left - 1;
        switch (buildPlacementRotationIndex % 4)
        {
            case 1:
                min = new Vector2Int(anchorCell.x + 1, anchorCell.y - left);
                max = new Vector2Int(anchorCell.x + depth, anchorCell.y + right);
                break;
            case 2:
                min = new Vector2Int(anchorCell.x - left, anchorCell.y - depth);
                max = new Vector2Int(anchorCell.x + right, anchorCell.y - 1);
                break;
            case 3:
                min = new Vector2Int(anchorCell.x - depth, anchorCell.y - left);
                max = new Vector2Int(anchorCell.x - 1, anchorCell.y + right);
                break;
            default:
                min = new Vector2Int(anchorCell.x - left, anchorCell.y + 1);
                max = new Vector2Int(anchorCell.x + right, anchorCell.y + depth);
                break;
        }
    }

    private void SetBuildFootprintPreviewCells(Vector2Int min, Vector2Int max, Vector2Int drivewayCell)
    {
        buildPreviewFootprintCells.Clear();
        buildPreviewDrivewayCell = drivewayCell;
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                buildPreviewFootprintCells.Add(new Vector2Int(x, y));
            }
        }
    }

    private bool GetBarPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        previewPosition = GetCellCenter(anchorCell) + new Vector3(0f, RoadHeight + 0.03f, 0f);
        previewScale = new Vector3(0.98f, 0.04f, 0.98f);
        GetRotatedBuildingFootprint(anchorCell, 2, 2, out Vector2Int previewMin, out Vector2Int previewMax);
        SetBuildFootprintPreviewCells(previewMin, previewMax, anchorCell);
        bool canPlace = TryGetBarPlacement(anchorCell, out Vector2Int min, out Vector2Int max);
        if (!canPlace) return false;

        float centerX = (min.x + max.x + 1) * 0.5f;
        float centerZ = (min.y + max.y + 1) * 0.5f;
        previewPosition = new Vector3(centerX, SampleTerrainHeight(centerX, centerZ) + RoadHeight + 0.03f, centerZ);
        previewScale = new Vector3((max.x - min.x + 1) * 0.94f, 0.04f, (max.y - min.y + 1) * 0.94f);
        return true;
    }

    private bool GetSawmillPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetTwoByTwoBuildingPlacementPreview(anchorCell, LocationType.Sawmill, out previewPosition, out previewScale);
    }

    private bool GetMotelPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetTwoByTwoBuildingPlacementPreview(anchorCell, LocationType.Motel, out previewPosition, out previewScale);
    }

    private bool GetCanteenPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetTwoByTwoBuildingPlacementPreview(anchorCell, LocationType.Canteen, out previewPosition, out previewScale);
    }

    private bool GetTwoByTwoBuildingPlacementPreview(Vector2Int anchorCell, LocationType type, out Vector3 previewPosition, out Vector3 previewScale)
    {
        previewPosition = GetCellCenter(anchorCell) + new Vector3(0f, RoadHeight + 0.03f, 0f);
        previewScale = new Vector3(0.98f, 0.04f, 0.98f);
        GetRotatedBuildingFootprint(anchorCell, 2, 2, out Vector2Int previewMin, out Vector2Int previewMax);
        SetBuildFootprintPreviewCells(previewMin, previewMax, anchorCell);
        if (!TryGetTwoByTwoBuildingPlacement(anchorCell, type, out Vector2Int min, out Vector2Int max))
        {
            return false;
        }

        float centerX = (min.x + max.x + 1) * 0.5f;
        float centerZ = (min.y + max.y + 1) * 0.5f;
        previewPosition = new Vector3(centerX, SampleTerrainHeight(centerX, centerZ) + RoadHeight + 0.03f, centerZ);
        previewScale = new Vector3((max.x - min.x + 1) * 0.94f, 0.04f, (max.y - min.y + 1) * 0.94f);
        return true;
    }

    private void CreateBarDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Color bodyColor = new Color(0.38f, 0.18f, 0.12f);
        float scale = BuildingDecorScale;

        // Main body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(parent, false);
        body.transform.position = center + new Vector3(0f, 0.42f * scale, 0f);
        body.transform.localScale = new Vector3(1.6f * scale, 0.84f * scale, 1.6f * scale);
        ApplyColor(body, bodyColor);
        ConfigureStaticVisual(body);

        // Roof overhang
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + new Vector3(0f, 0.88f * scale, 0f);
        roof.transform.localScale = new Vector3(1.76f * scale, 0.07f * scale, 1.76f * scale);
        ApplyColor(roof, bodyColor * 0.72f);
        ConfigureStaticVisual(roof);

        // Small chimney
        GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cube);
        chimney.transform.SetParent(parent, false);
        chimney.transform.position = center + new Vector3(-0.38f * scale, 1.18f * scale, 0.28f * scale);
        chimney.transform.localScale = new Vector3(0.16f * scale, 0.58f * scale, 0.16f * scale);
        ApplyColor(chimney, new Color(0.28f, 0.22f, 0.18f));
        ConfigureStaticVisual(chimney);

        // Door (faces south toward anchor) — on south face of body
        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.transform.SetParent(parent, false);
        door.transform.position = center + new Vector3(0f, 0.22f * scale, -0.81f * scale);
        door.transform.localScale = new Vector3(0.36f * scale, 0.52f * scale, 0.04f * scale);
        ApplyColor(door, new Color(0.18f, 0.10f, 0.04f));
        ConfigureStaticVisual(door);

        // Door frame
        GameObject doorFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorFrame.transform.SetParent(parent, false);
        doorFrame.transform.position = center + new Vector3(0f, 0.26f * scale, -0.82f * scale);
        doorFrame.transform.localScale = new Vector3(0.44f * scale, 0.62f * scale, 0.03f * scale);
        ApplyColor(doorFrame, new Color(0.55f, 0.35f, 0.18f));
        ConfigureStaticVisual(doorFrame);

        // Sign board above door
        GameObject signBg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        signBg.transform.SetParent(parent, false);
        signBg.transform.position = center + new Vector3(0f, 0.68f * scale, -0.82f * scale);
        signBg.transform.localScale = new Vector3(0.68f * scale, 0.22f * scale, 0.04f * scale);
        ApplyColor(signBg, new Color(0.92f, 0.88f, 0.72f));
        ConfigureStaticVisual(signBg);

        // Warm point light above entrance
        GameObject lightObj = new("BarLight");
        lightObj.transform.SetParent(parent, false);
        lightObj.transform.position = center + new Vector3(0f, 0.9f * scale, -0.9f * scale);
        Light barLight = lightObj.AddComponent<Light>();
        barLight.type = LightType.Point;
        barLight.color = new Color(1f, 0.85f, 0.5f);
        barLight.intensity = 0.35f;
        barLight.range = 3f;
        barLight.shadows = LightShadows.None;

        // Walkway from entrance to anchor
        CreateDrivewayToAnchor(parent, min, max, anchor, 0.52f);
    }

    private void CreateCanteenDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        float scale = BuildingDecorScale;
        Color wallColor = new Color(0.66f, 0.48f, 0.27f);
        Color roofColor = new Color(0.85f, 0.32f, 0.16f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(parent, false);
        body.transform.position = center + new Vector3(0f, 0.4f * scale, 0f);
        body.transform.localScale = new Vector3(1.65f * scale, 0.8f * scale, 1.45f * scale);
        ApplyColor(body, wallColor);
        ConfigureStaticVisual(body);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + new Vector3(0f, 0.86f * scale, 0f);
        roof.transform.localScale = new Vector3(1.9f * scale, 0.1f * scale, 1.7f * scale);
        ApplyColor(roof, roofColor);
        ConfigureStaticVisual(roof);

        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.transform.SetParent(parent, false);
        door.transform.position = center + new Vector3(-0.28f * scale, 0.2f * scale, -0.74f * scale);
        door.transform.localScale = new Vector3(0.34f * scale, 0.48f * scale, 0.04f * scale);
        ApplyColor(door, new Color(0.22f, 0.13f, 0.07f));
        ConfigureStaticVisual(door);

        GameObject servingWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        servingWindow.transform.SetParent(parent, false);
        servingWindow.transform.position = center + new Vector3(0.38f * scale, 0.48f * scale, -0.75f * scale);
        servingWindow.transform.localScale = new Vector3(0.56f * scale, 0.3f * scale, 0.04f * scale);
        ApplyColor(servingWindow, new Color(0.95f, 0.88f, 0.58f));
        ConfigureStaticVisual(servingWindow);

        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.transform.SetParent(parent, false);
        sign.transform.position = center + new Vector3(0f, 0.69f * scale, -0.77f * scale);
        sign.transform.localScale = new Vector3(0.82f * scale, 0.18f * scale, 0.04f * scale);
        ApplyColor(sign, new Color(0.98f, 0.86f, 0.34f));
        ConfigureStaticVisual(sign);

        for (int i = 0; i < 2; i++)
        {
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.transform.SetParent(parent, false);
            table.transform.position = center + new Vector3((-0.38f + i * 0.76f) * scale, 0.11f * scale, 0.62f * scale);
            table.transform.localScale = new Vector3(0.46f * scale, 0.1f * scale, 0.28f * scale);
            ApplyColor(table, new Color(0.55f, 0.34f, 0.16f));
            ConfigureStaticVisual(table);
        }

        GameObject lightObj = new("CanteenLight");
        lightObj.transform.SetParent(parent, false);
        lightObj.transform.position = center + new Vector3(0.35f * scale, 0.88f * scale, -0.9f * scale);
        Light canteenLight = lightObj.AddComponent<Light>();
        canteenLight.type = LightType.Point;
        canteenLight.color = new Color(1f, 0.78f, 0.42f);
        canteenLight.intensity = 0.32f;
        canteenLight.range = 2.8f;
        canteenLight.shadows = LightShadows.None;

        CreateDrivewayToAnchor(parent, min, max, anchor, 0.52f);
    }

    private bool TryGetFurnitureFactoryPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, LocationType.FurnitureFactory, 3, 2, out min, out max);
    }

    private void CreateMiscTree(Vector2Int cell, int variantIndex)
    {
        if (miscRoot == null)
        {
            return;
        }

        GameObject treeRoot = new($"MiscTree_{cell.x}_{cell.y}");
        treeRoot.transform.SetParent(miscRoot, false);
        treeRoot.transform.position = GetCellCenter(cell);
        treeRoot.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        treeRoot.transform.localScale = Vector3.one * Random.Range(1.18f, 1.58f);
        CreateTreeVariant(treeRoot.transform, variantIndex);
        RegisterMiscTreeSway(treeRoot.transform, cell, variantIndex);
        RegisterMiscTreePerchPoint(treeRoot.transform, cell, variantIndex);
        miscOccupiedCells.Add(cell);
    }

    private void CreateBerryBush(Vector2Int cell, int variantSeed)
    {
        if (miscRoot == null)
        {
            return;
        }

        GameObject bushRoot = new($"BerryBush_{cell.x}_{cell.y}");
        bushRoot.transform.SetParent(miscRoot, false);
        bushRoot.transform.position = GetCellCenter(cell);
        bushRoot.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        bushRoot.transform.localScale = Vector3.one * Random.Range(0.78f, 1.02f);

        Color leafDark = new Color(0.16f, 0.42f, 0.2f);
        Color leafLight = new Color(0.22f, 0.52f, 0.26f);
        Color berryColor = new Color(0.9f, 0.08f, 0.12f);
        Color berryHighlight = new Color(0.98f, 0.2f, 0.24f);

        Vector3[] clumpPositions =
        {
            new Vector3(-0.12f, 0.18f, -0.02f),
            new Vector3(0.14f, 0.22f, 0.04f),
            new Vector3(0.02f, 0.25f, -0.14f)
        };

        Vector3[] clumpScales =
        {
            new Vector3(0.32f, 0.24f, 0.3f),
            new Vector3(0.36f, 0.28f, 0.32f),
            new Vector3(0.28f, 0.22f, 0.26f)
        };

        for (int i = 0; i < clumpPositions.Length; i++)
        {
            GameObject clump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            clump.transform.SetParent(bushRoot.transform, false);
            clump.transform.localPosition = clumpPositions[i];
            clump.transform.localScale = clumpScales[i];
            ApplyColor(clump, i % 2 == 0 ? leafLight : leafDark);
            ConfigureStaticVisual(clump);
        }

        for (int i = 0; i < 12; i++)
        {
            float angle = (i / 12f) * Mathf.PI * 2f + variantSeed * 0.37f;
            float radius = 0.08f + (i % 3) * 0.035f;
            GameObject berry = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            berry.transform.SetParent(bushRoot.transform, false);
            berry.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * radius,
                0.13f + (i % 4) * 0.045f,
                Mathf.Sin(angle) * radius * 0.88f);
            berry.transform.localScale = new Vector3(0.085f, 0.085f, 0.085f);
            ApplyColor(berry, i % 3 == 0 ? berryHighlight : berryColor);
            ConfigureStaticVisual(berry);
        }

        miscOccupiedCells.Add(cell);
    }

    private void CreateFlowerPatch(Vector2Int cell, int variantSeed)
    {
        if (miscRoot == null)
        {
            return;
        }

        GameObject patchRoot = new($"FlowerPatch_{cell.x}_{cell.y}");
        patchRoot.transform.SetParent(miscRoot, false);
        patchRoot.transform.position = GetCellCenter(cell);
        patchRoot.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        patchRoot.transform.localScale = Vector3.one * Random.Range(0.82f, 1.08f);

        Color stemColor = new Color(0.2f, 0.5f, 0.24f);
        Color[] petalColors =
        {
            new Color(0.94f, 0.88f, 0.24f),
            new Color(0.96f, 0.62f, 0.22f),
            new Color(0.92f, 0.48f, 0.58f),
            new Color(0.86f, 0.78f, 0.96f)
        };

        int flowerCount = 4 + (variantSeed % 3);
        for (int i = 0; i < flowerCount; i++)
        {
            float angle = (i / Mathf.Max(1f, flowerCount)) * Mathf.PI * 2f + variantSeed * 0.31f;
            float radius = 0.08f + (i % 2) * 0.06f;
            Vector3 baseOffset = new Vector3(
                Mathf.Cos(angle) * radius,
                0.04f,
                Mathf.Sin(angle) * radius);

            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(patchRoot.transform, false);
            stem.transform.localPosition = baseOffset + new Vector3(0f, 0.08f, 0f);
            stem.transform.localScale = new Vector3(0.018f, 0.08f, 0.018f);
            ApplyColor(stem, stemColor);
            ConfigureStaticVisual(stem);

            GameObject bloom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bloom.transform.SetParent(patchRoot.transform, false);
            bloom.transform.localPosition = baseOffset + new Vector3(0f, 0.17f + (i % 2) * 0.015f, 0f);
            bloom.transform.localScale = new Vector3(0.08f, 0.035f, 0.08f);
            ApplyColor(bloom, petalColors[(variantSeed + i) % petalColors.Length]);
            ConfigureStaticVisual(bloom);
        }

        GameObject grassClump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        grassClump.transform.SetParent(patchRoot.transform, false);
        grassClump.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        grassClump.transform.localScale = new Vector3(0.28f, 0.08f, 0.24f);
        ApplyColor(grassClump, new Color(0.24f, 0.56f, 0.28f));
        ConfigureStaticVisual(grassClump);

        flowerBeePoints.Add(patchRoot.transform.position + new Vector3(0f, 0.08f, 0f));
        miscOccupiedCells.Add(cell);
    }

    private void RegisterMiscTreeSway(Transform treeRoot, Vector2Int cell, int variantIndex)
    {
        if (treeRoot == null)
        {
            return;
        }

        miscTreeSways.Add(new MiscTreeSway
        {
            RootTransform = treeRoot,
            BaseRotation = treeRoot.localRotation,
            PhaseOffset = (cell.x * 0.73f) + (cell.y * 1.17f) + variantIndex * 0.41f,
            SecondaryPhaseOffset = (cell.x * 1.31f) + (cell.y * 0.57f) + variantIndex * 0.88f,
            Speed = 0.55f + ((cell.x + cell.y + variantIndex) % 5) * 0.06f,
            PitchAmplitude = 1.2f + ((cell.x + variantIndex) % 4) * 0.18f,
            RollAmplitude = 0.9f + ((cell.y + variantIndex) % 4) * 0.14f
        });
    }

    private void RegisterMiscTreePerchPoint(Transform treeRoot, Vector2Int cell, int variantIndex)
    {
        if (treeRoot == null)
        {
            return;
        }

        float canopyHeight = treeRoot.localScale.y * Random.Range(0.76f, 0.9f);
        Vector3 perchOffset = new(
            (((cell.x + variantIndex) % 3) - 1) * 0.05f,
            canopyHeight,
            (((cell.y + variantIndex * 2) % 3) - 1) * 0.05f);
        miscTreePerchPoints.Add(treeRoot.position + perchOffset);
    }

    private void UpdateMiscTreeSways()
    {
        if (miscTreeSways.Count == 0)
        {
            return;
        }

        float time = Time.time;
        for (int i = miscTreeSways.Count - 1; i >= 0; i--)
        {
            MiscTreeSway sway = miscTreeSways[i];
            if (sway.RootTransform == null)
            {
                miscTreeSways.RemoveAt(i);
                continue;
            }

            float primary = Mathf.Sin(time * sway.Speed + sway.PhaseOffset);
            float secondary = Mathf.Sin(time * (sway.Speed * 0.63f) + sway.SecondaryPhaseOffset);
            float pitch = primary * sway.PitchAmplitude;
            float roll = secondary * sway.RollAmplitude;
            sway.RootTransform.localRotation = sway.BaseRotation * Quaternion.Euler(pitch, 0f, roll);
        }
    }

    private void CreateTreeVariant(Transform parent, int variantIndex)
    {
        int variant = Mathf.Abs(variantIndex) % 3;
        switch (variant)
        {
            case 0:
                CreateMiscTreeTall(parent);
                break;
            case 1:
                CreateMiscTreeRound(parent);
                break;
            default:
                CreateMiscTreePine(parent);
                break;
        }
    }

    private void CreateMiscTreeTall(Transform parent)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(parent, false);
        trunk.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.34f, 0f));
        trunk.transform.localScale = ScaleTreeLocalScale(new Vector3(0.12f, 0.34f, 0.12f));
        ApplyColor(trunk, new Color(0.44f, 0.28f, 0.16f));
        ConfigureStaticVisual(trunk);

        GameObject crownBottom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crownBottom.transform.SetParent(parent, false);
        crownBottom.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.9f, 0f));
        crownBottom.transform.localScale = ScaleTreeLocalScale(new Vector3(0.62f, 0.42f, 0.62f));
        ApplyColor(crownBottom, new Color(0.22f, 0.56f, 0.27f));
        ConfigureStaticVisual(crownBottom);

        GameObject crownTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crownTop.transform.SetParent(parent, false);
        crownTop.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 1.16f, 0f));
        crownTop.transform.localScale = ScaleTreeLocalScale(new Vector3(0.44f, 0.34f, 0.44f));
        ApplyColor(crownTop, new Color(0.18f, 0.5f, 0.24f));
        ConfigureStaticVisual(crownTop);
    }

    private void CreateMiscTreeRound(Transform parent)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(parent, false);
        trunk.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.28f, 0f));
        trunk.transform.localScale = ScaleTreeLocalScale(new Vector3(0.11f, 0.28f, 0.11f));
        ApplyColor(trunk, new Color(0.42f, 0.25f, 0.15f));
        ConfigureStaticVisual(trunk);

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.transform.SetParent(parent, false);
        canopy.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.84f, 0f));
        canopy.transform.localScale = ScaleTreeLocalScale(new Vector3(0.72f, 0.66f, 0.72f));
        ApplyColor(canopy, new Color(0.3f, 0.62f, 0.31f));
        ConfigureStaticVisual(canopy);

        GameObject sideBlob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sideBlob.transform.SetParent(parent, false);
        sideBlob.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0.18f, 0.78f, -0.1f));
        sideBlob.transform.localScale = ScaleTreeLocalScale(new Vector3(0.34f, 0.28f, 0.34f));
        ApplyColor(sideBlob, new Color(0.24f, 0.56f, 0.28f));
        ConfigureStaticVisual(sideBlob);
    }

    private void CreateMiscTreePine(Transform parent)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(parent, false);
        trunk.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.24f, 0f));
        trunk.transform.localScale = ScaleTreeLocalScale(new Vector3(0.1f, 0.24f, 0.1f));
        ApplyColor(trunk, new Color(0.4f, 0.24f, 0.14f));
        ConfigureStaticVisual(trunk);

        GameObject lower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lower.transform.SetParent(parent, false);
        lower.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.7f, 0f));
        lower.transform.localScale = ScaleTreeLocalScale(new Vector3(0.36f, 0.24f, 0.36f));
        ApplyColor(lower, new Color(0.16f, 0.44f, 0.23f));
        ConfigureStaticVisual(lower);

        GameObject upper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        upper.transform.SetParent(parent, false);
        upper.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 1.05f, 0f));
        upper.transform.localScale = ScaleTreeLocalScale(new Vector3(0.24f, 0.22f, 0.24f));
        ApplyColor(upper, new Color(0.12f, 0.36f, 0.2f));
        ConfigureStaticVisual(upper);
    }

    private static Vector3 ScaleTreeLocalPosition(Vector3 source)
    {
        source.y *= TreeHeightScale;
        return source;
    }

    private static Vector3 ScaleTreeLocalScale(Vector3 source)
    {
        source.y *= TreeHeightScale;
        return source;
    }
}
