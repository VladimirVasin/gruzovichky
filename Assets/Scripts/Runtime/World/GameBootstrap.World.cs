using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private const float TreeHeightScale = 1.1f;
    private const float MiscTreeWorldScaleMultiplier = 2f;
    private const float BerryBushSpawnChance = 0.36f;
    private const float FlowerPatchSpawnChance = 0.14f;

    private void SetupLocations()
    {
        locations.Clear();
        localStops.Clear();
        locationTrashCanMealTargets.Clear();
        hasShownLocalBusStopMinimumHint = false;
        personalHouses.Clear();
        personalHouseSelectionHighlights.Clear();
        selectedPersonalHouseIndex = -1;

        GeneratedWorldLayout layout = WorldLayoutGenerator.Generate(GridWidth, GridHeight, waterCells, HasRequiredLayoutRoads);

        if (selectedGameStartMode == GameStartMode.Clear)
        {
            CreateLocation(LocationType.IntercityStop, "Intercity Stop", layout.BusStop.Min, layout.BusStop.Max, layout.BusStop.Anchor, new Color(0.82f, 0.24f, 0.22f), layout.BusStop.RoadAccess);
            SessionDebugLogger.Log("WORLD", $"Generated clear layout: BusStop {FormatPlacement(layout.BusStop)} (all other locations skipped).");
            return;
        }

        if (selectedGameStartMode == GameStartMode.Debug)
        {
            CreateLocation(LocationType.Parking, "Parking", layout.Parking.Min, layout.Parking.Max, layout.Parking.Anchor, new Color(0.46f, 0.46f, 0.52f), layout.Parking.RoadAccess);
            CreateLocation(LocationType.Warehouse, "Warehouse", layout.Warehouse.Min, layout.Warehouse.Max, layout.Warehouse.Anchor, new Color(0.7f, 0.52f, 0.3f), layout.Warehouse.RoadAccess);
            CreateLocation(LocationType.GasStation, "Gas Station", layout.GasStation.Min, layout.GasStation.Max, layout.GasStation.Anchor, new Color(0.84f, 0.68f, 0.26f), layout.GasStation.RoadAccess);
            CreateLocation(LocationType.Forest, "Lumberyard", layout.Forest.Min, layout.Forest.Max, layout.Forest.Anchor, new Color(0.58f, 0.42f, 0.24f), layout.Forest.RoadAccess);
            CreateLocation(LocationType.Sawmill, "Sawmill", layout.Sawmill.Min, layout.Sawmill.Max, layout.Sawmill.Anchor, new Color(0.3f, 0.52f, 0.8f), layout.Sawmill.RoadAccess);
            CreateLocation(LocationType.Motel, "Motel", layout.Motel.Min, layout.Motel.Max, layout.Motel.Anchor, new Color(0.91f, 0.87f, 0.74f), layout.Motel.RoadAccess);
        }

        CreateLocation(LocationType.IntercityStop, "Intercity Stop", layout.BusStop.Min, layout.BusStop.Max, layout.BusStop.Anchor, new Color(0.82f, 0.24f, 0.22f), layout.BusStop.RoadAccess);

        SessionDebugLogger.Log(
            "WORLD",
            selectedGameStartMode == GameStartMode.Debug
                ? $"Generated debug layout: Parking {FormatPlacement(layout.Parking)}, GasStation {FormatPlacement(layout.GasStation)}, Forest {FormatPlacement(layout.Forest)}, Warehouse {FormatPlacement(layout.Warehouse)}, Sawmill {FormatPlacement(layout.Sawmill)}, Motel {FormatPlacement(layout.Motel)}, BusStop {FormatPlacement(layout.BusStop)}."
                : $"Generated user layout: BusStop {FormatPlacement(layout.BusStop)}. Parking/Warehouse skipped for Build menu; all production/service tutorial buildings skipped.");
    }

    private bool HasRequiredLayoutRoads(GeneratedWorldLayout layout)
    {
        if (selectedGameStartMode == GameStartMode.Clear)
        {
            return true;
        }

        if (selectedGameStartMode == GameStartMode.User)
        {
            return true;
        }

        if (!CanBuildWideRoadPath(layout.Parking.RoadAccess, layout.GasStation.RoadAccess, cell => IsPlacementCell(layout, cell)) ||
            !CanBuildWideRoadPath(layout.GasStation.RoadAccess, layout.Warehouse.RoadAccess, cell => IsPlacementCell(layout, cell)) ||
            !CanBuildWideRoadPath(layout.Warehouse.RoadAccess, layout.Forest.RoadAccess, cell => IsPlacementCell(layout, cell)))
        {
            return false;
        }

        return CanBuildWideRoadPath(layout.Forest.RoadAccess, layout.Sawmill.RoadAccess, cell => IsPlacementCell(layout, cell)) &&
               CanBuildWideRoadPath(layout.Sawmill.RoadAccess, layout.Warehouse.RoadAccess, cell => IsPlacementCell(layout, cell)) &&
               CanBuildWideRoadPath(layout.Warehouse.RoadAccess, layout.Motel.RoadAccess, cell => IsPlacementCell(layout, cell)) &&
               CanBuildWideRoadPath(layout.Warehouse.RoadAccess, layout.BusStop.RoadAccess, cell => IsPlacementCell(layout, cell));
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
        terrainHeights = TerrainHeightGenerator.Generate(GridWidth, GridHeight, GetWorldPlacements(), hillZones);
    }

    private IEnumerable<WorldLocationPlacement> GetWorldPlacements()
    {
        foreach (LocationData location in locations.Values)
        {
            yield return new WorldLocationPlacement
            {
                Min = location.Min,
                Max = location.Max,
                Anchor = location.Anchor,
                RoadAccess = location.RoadAccess
            };
        }

        foreach (LocationData stop in localStops)
        {
            yield return new WorldLocationPlacement
            {
                Min = stop.Min,
                Max = stop.Max,
                Anchor = stop.Anchor,
                RoadAccess = stop.RoadAccess
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

        for (int i = 0; i < localStops.Count; i++)
        {
            LocationData stop = localStops[i];
            if (stop.RootObject != null)
            {
                stop.RootObject.transform.position = new Vector3(0f, GetLocationBaseHeight(stop), 0f);
            }
        }
    }

    private void PopulateMiscTrees()
    {
        if (miscRoot == null)
        {
            return;
        }

        ResetLumberyardWorldState();
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
            cell => IsGrassGroundCell(cell.x, cell.y) && !IsWaterOrBeachCell(cell),
            GetDenseForestCellPriority);
        int bushCount = 0;
        int flowerCount = 0;
        SessionDebugLogger.Log("WORLD", $"Planning {plannedCells.Count} misc cells");
        for (int i = 0; i < plannedCells.Count; i++)
        {
            MiscDecorationKind kind = MiscDecorationSpawnService.ChooseKind(
                Random.value,
                FlowerPatchSpawnChance,
                BerryBushSpawnChance);
            if (kind == MiscDecorationKind.FlowerPatch)
            {
                CreateFlowerPatch(plannedCells[i], i);
                flowerCount++;
            }
            else if (kind == MiscDecorationKind.BerryBush)
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
        Vector2Int access = placement.RoadAccess == default ? placement.Anchor : placement.RoadAccess;
        return $"min({placement.Min.x},{placement.Min.y}) max({placement.Max.x},{placement.Max.y}) anchor({placement.Anchor.x},{placement.Anchor.y}) access({access.x},{access.y})";
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

    private bool GetStopPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        previewPosition = GetCellCenter(anchorCell) + new Vector3(0f, RoadHeight + 0.03f, 0f);
        previewScale = new Vector3(0.98f, 0.04f, 0.98f);
        GetRotatedBuildingFootprint(anchorCell, 2, 1, out Vector2Int previewMin, out Vector2Int previewMax);
        SetBuildFootprintPreviewCells(previewMin, previewMax, anchorCell);
        bool canPlace = TryGetStopPlacement(anchorCell, out Vector2Int min, out Vector2Int max);
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
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        SessionDebugLogger.Log("BUILD", $"Placed Furniture Factory at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool TryPlaceParkingAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.Parking))
        {
            SessionDebugLogger.Log("BUILD", "Parking placement rejected: parking already exists.");
            return false;
        }

        if (!TryGetRotatedBuildingPlacement(anchorCell, LocationType.Parking, 3, 2, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Parking placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.Parking, "Parking", min, max, anchorCell, new Color(0.46f, 0.46f, 0.52f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        NotifyTutorialCoreBuildingBuilt(LocationType.Parking);
        SessionDebugLogger.Log("BUILD", $"Placed Parking at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool TryPlaceWarehouseAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.Warehouse))
        {
            SessionDebugLogger.Log("BUILD", "Warehouse placement rejected: warehouse already exists.");
            return false;
        }

        if (!TryGetTwoByTwoBuildingPlacement(anchorCell, LocationType.Warehouse, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Warehouse placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.Warehouse, "Warehouse", min, max, anchorCell, new Color(0.7f, 0.52f, 0.3f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        NotifyTutorialCoreBuildingBuilt(LocationType.Warehouse);
        SessionDebugLogger.Log("BUILD", $"Placed Warehouse at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool TryPlaceForestAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.Forest))
        {
            SessionDebugLogger.Log("BUILD", "Lumberjack Camp placement rejected: camp already exists.");
            return false;
        }

        if (!TryGetForestPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Lumberjack Camp placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.Forest, "Lumberjack Camp", min, max, anchorCell, new Color(0.42f, 0.30f, 0.18f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        SessionDebugLogger.Log("BUILD", $"Placed Lumberjack Camp at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        NotifyTutorialLumberjackCampBuilt();
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

        CreateLocation(LocationType.Bar, "Bar", min, max, anchorCell, new Color(0.46f, 0.16f, 0.11f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        SessionDebugLogger.Log("BUILD", $"Placed Bar at anchor ({anchorCell.x},{anchorCell.y}).");
        NotifyTutorialServiceBuildingBuilt(LocationType.Bar);
        return true;
    }

    private bool TryPlaceStopAtAnchor(Vector2Int anchorCell)
    {
        if (!TryGetStopPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Stop placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.Stop, "Bus Stop", min, max, anchorCell, new Color(0.82f, 0.24f, 0.22f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        ShowLocalBusStopMinimumHintIfNeeded();
        SessionDebugLogger.Log("BUILD", $"Placed Stop at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        NotifyTutorialLocalBusStopBuilt();
        return true;
    }

    private bool TryPlaceCanteenAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.Canteen))
        {
            SessionDebugLogger.Log("BUILD", "Canteen placement rejected: canteen already exists.");
            return false;
        }

        if (!TryGetCanteenPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Canteen placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.Canteen, "Canteen", min, max, anchorCell, new Color(0.20f, 0.46f, 0.48f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        SessionDebugLogger.Log("BUILD", $"Placed Canteen at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        NotifyTutorialServiceBuildingBuilt(LocationType.Canteen);
        return true;
    }

    private bool TryPlaceGamblingHallAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.GamblingHall))
        {
            SessionDebugLogger.Log("BUILD", "Gambling Hall placement rejected: already exists.");
            return false;
        }

        if (!TryGetGamblingHallPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Gambling Hall placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.GamblingHall, "Gambling Hall", min, max, anchorCell, new Color(0.34f, 0.16f, 0.46f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        SessionDebugLogger.Log("BUILD", $"Placed Gambling Hall at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        NotifyTutorialServiceBuildingBuilt(LocationType.GamblingHall);
        return true;
    }

    private bool TryPlaceCityParkAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.CityPark))
        {
            SessionDebugLogger.Log("BUILD", "City Park placement rejected: already exists.");
            return false;
        }

        if (!TryGetCityParkPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"City Park placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        // Anchor equals min (park has no road driveway)
        CreateLocation(LocationType.CityPark, "City Park", min, max, min, new Color(0.30f, 0.52f, 0.22f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        SessionDebugLogger.Log("BUILD", $"Placed City Park at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        NotifyTutorialServiceBuildingBuilt(LocationType.CityPark);
        return true;
    }

    private bool TryPlacePersonalHouseAtAnchor(Vector2Int anchorCell)
    {
        if (!TryGetRotatedBuildingPlacement(anchorCell, LocationType.PersonalHouse, 5, 6, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Personal House placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.PersonalHouse, "Personal House", min, max, anchorCell, new Color(0.88f, 0.82f, 0.70f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        SessionDebugLogger.Log("BUILD", $"Placed Personal House at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool TryPlaceCarMarketAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.CarMarket))
        {
            SessionDebugLogger.Log("BUILD", "Car Market placement rejected: already exists.");
            return false;
        }

        if (!TryGetRotatedBuildingPlacement(anchorCell, LocationType.CarMarket, 5, 5, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Car Market placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.CarMarket, "Car Market", min, max, anchorCell, new Color(0.64f, 0.52f, 0.38f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        SessionDebugLogger.Log("BUILD", $"Placed Car Market at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool GetPersonalHousePlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetRotatedBuildingPlacementPreview(anchorCell, LocationType.PersonalHouse, 5, 6, out previewPosition, out previewScale);
    }

    private bool GetCarMarketPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetRotatedBuildingPlacementPreview(anchorCell, LocationType.CarMarket, 5, 5, out previewPosition, out previewScale);
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
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
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
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        NotifyTutorialCoreBuildingBuilt(LocationType.Motel);
        MoveStarterIdleWorkersToMotel();
        MoveAmbientCatsToCurrentHome();
        SessionDebugLogger.Log("BUILD", $"Placed Motel at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool TryGetBarPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetTwoByTwoBuildingPlacement(anchorCell, LocationType.Bar, out min, out max);
    }

    private bool TryGetForestPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, LocationType.Forest, 3, 3, out min, out max);
    }

    private bool TryGetCanteenPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, LocationType.Canteen, 3, 2, out min, out max);
    }

    private bool TryGetGamblingHallPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, LocationType.GamblingHall, 3, 3, out min, out max);
    }

    private bool TryGetCityParkPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        // Park uses the clicked cell as its SW corner (rotation-independent, symmetric 8x8).
        // No road adjacency required — the park can be placed anywhere on open ground.
        min = anchorCell;
        max = new Vector2Int(anchorCell.x + 7, anchorCell.y + 7);

        if (locations.ContainsKey(LocationType.CityPark))
            return false;

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector2Int cell = new(x, y);
                if (!IsInsideGrid(cell) || roadCells.Contains(cell) || edgeHighwayCells.Contains(cell) ||
                    IsLocationCell(cell) || IsWaterOrBeachCell(cell))
                    return false;
            }
        }
        return true;
    }

    private bool GetCityParkPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        previewPosition = GetCellCenter(anchorCell) + new Vector3(0f, RoadHeight + 0.03f, 0f);
        previewScale = new Vector3(0.98f, 0.04f, 0.98f);
        Vector2Int min = anchorCell;
        Vector2Int max = new Vector2Int(anchorCell.x + 7, anchorCell.y + 7);
        buildPreviewFootprintCells.Clear();
        buildPreviewDrivewayCell = null;
        for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
                buildPreviewFootprintCells.Add(new Vector2Int(x, y));
        bool canPlace = TryGetCityParkPlacement(anchorCell, out _, out _);
        float cx = (min.x + max.x + 1) * 0.5f;
        float cz = (min.y + max.y + 1) * 0.5f;
        previewPosition = new Vector3(cx, SampleTerrainHeight(cx, cz) + RoadHeight + 0.03f, cz);
        previewScale = new Vector3(8f * 0.94f, 0.04f, 8f * 0.94f);
        return canPlace;
    }

    private bool TryGetStopPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, LocationType.Stop, 2, 1, out min, out max);
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

        return BuildingPlacementService.IsFootprintClear(anchorCell, min, max, IsInsideGrid, IsBuildingPlacementBlockedCell);
    }

    private void GetRotatedBuildingFootprint(Vector2Int anchorCell, int width, int depth, out Vector2Int min, out Vector2Int max)
    {
        BuildingPlacementService.GetRotatedFootprint(anchorCell, width, depth, buildPlacementRotationIndex, out min, out max);
    }

    private bool IsBuildingPlacementBlockedCell(Vector2Int cell)
    {
        return roadCells.Contains(cell) ||
               edgeHighwayCells.Contains(cell) ||
               IsLocationCell(cell) ||
               IsWaterOrBeachCell(cell);
    }

    private void SetBuildFootprintPreviewCells(Vector2Int min, Vector2Int max, Vector2Int drivewayCell)
    {
        BuildingPlacementService.FillFootprintCells(buildPreviewFootprintCells, min, max);
        buildPreviewDrivewayCell = drivewayCell;
    }

    private bool GetBarPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        previewPosition = GetCellCenter(anchorCell) + new Vector3(0f, RoadHeight + 0.03f, 0f);
        previewScale = new Vector3(0.98f, 0.04f, 0.98f);
        GetRotatedBuildingFootprint(anchorCell, 2, 2, out Vector2Int previewMin, out Vector2Int previewMax);
        SetBuildFootprintPreviewCells(previewMin, previewMax, anchorCell);
        bool canPlace = TryGetBarPlacement(anchorCell, out Vector2Int min, out Vector2Int max);
        if (!canPlace) return false;

        BuildingPlacementPreview preview = BuildingPlacementService.CreatePreview(min, max);
        Vector2 center = BuildingPlacementService.GetFootprintCenter(preview.Min, preview.Max);
        previewPosition = new Vector3(center.x, SampleTerrainHeight(center.x, center.y) + RoadHeight + 0.03f, center.y);
        previewScale = preview.Scale;
        return true;
    }

    private bool GetForestPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetRotatedBuildingPlacementPreview(anchorCell, LocationType.Forest, 3, 3, out previewPosition, out previewScale);
    }

    private bool GetParkingPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetRotatedBuildingPlacementPreview(anchorCell, LocationType.Parking, 3, 2, out previewPosition, out previewScale);
    }

    private bool GetWarehousePlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetTwoByTwoBuildingPlacementPreview(anchorCell, LocationType.Warehouse, out previewPosition, out previewScale);
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
        return GetRotatedBuildingPlacementPreview(anchorCell, LocationType.Canteen, 3, 2, out previewPosition, out previewScale);
    }

    private bool GetGamblingHallPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetRotatedBuildingPlacementPreview(anchorCell, LocationType.GamblingHall, 3, 3, out previewPosition, out previewScale);
    }

    private bool GetTwoByTwoBuildingPlacementPreview(Vector2Int anchorCell, LocationType type, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetRotatedBuildingPlacementPreview(anchorCell, type, 2, 2, out previewPosition, out previewScale);
    }

    private bool GetRotatedBuildingPlacementPreview(Vector2Int anchorCell, LocationType type, int width, int depth, out Vector3 previewPosition, out Vector3 previewScale)
    {
        previewPosition = GetCellCenter(anchorCell) + new Vector3(0f, RoadHeight + 0.03f, 0f);
        previewScale = new Vector3(0.98f, 0.04f, 0.98f);
        GetRotatedBuildingFootprint(anchorCell, width, depth, out Vector2Int previewMin, out Vector2Int previewMax);
        SetBuildFootprintPreviewCells(previewMin, previewMax, anchorCell);
        if (!TryGetRotatedBuildingPlacement(anchorCell, type, width, depth, out Vector2Int min, out Vector2Int max))
        {
            return false;
        }

        BuildingPlacementPreview preview = BuildingPlacementService.CreatePreview(min, max);
        Vector2 center = BuildingPlacementService.GetFootprintCenter(preview.Min, preview.Max);
        previewPosition = new Vector3(center.x, SampleTerrainHeight(center.x, center.y) + RoadHeight + 0.03f, center.y);
        previewScale = preview.Scale;
        return true;
    }

    private void CreateServiceGlowPanel(Transform parent, Vector3 worldPosition, Vector3 localScale, Color tint)
    {
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glow.transform.SetParent(parent, false);
        glow.transform.position = worldPosition;
        glow.transform.localScale = localScale;
        Renderer renderer = glow.GetComponent<Renderer>();
        renderer.material = CreateTransparentOverlayMaterial(tint);
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (glow.TryGetComponent(out Collider collider))
        {
            Object.Destroy(collider);
        }
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

        // Door faces south toward the road anchor.
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
        CreateServiceGlowPanel(parent, center + new Vector3(0f, 0.44f * scale, -0.79f * scale), new Vector3(0.5f * scale, 0.44f * scale, 0.02f * scale), new Color(1f, 0.7f, 0.32f, 0.1f));

        // Sign board above door
        GameObject signBg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        signBg.transform.SetParent(parent, false);
        signBg.transform.position = center + new Vector3(0f, 0.68f * scale, -0.82f * scale);
        signBg.transform.localScale = new Vector3(0.68f * scale, 0.22f * scale, 0.04f * scale);
        ApplyColor(signBg, new Color(0.92f, 0.88f, 0.72f));
        ConfigureStaticVisual(signBg);
        CreateServiceGlowPanel(parent, signBg.transform.position + new Vector3(0f, 0f, -0.025f * scale), new Vector3(0.82f * scale, 0.28f * scale, 0.02f * scale), new Color(1f, 0.72f, 0.34f, 0.18f));

        GameObject facade = GameObject.CreatePrimitive(PrimitiveType.Cube);
        facade.transform.SetParent(parent, false);
        facade.transform.position = center + new Vector3(0f, 1.12f * scale, -0.73f * scale);
        facade.transform.localScale = new Vector3(1.18f * scale, 0.52f * scale, 0.06f * scale);
        ApplyColor(facade, new Color(0.58f, 0.24f, 0.16f));
        ConfigureStaticVisual(facade);

        GameObject porch = GameObject.CreatePrimitive(PrimitiveType.Cube);
        porch.transform.SetParent(parent, false);
        porch.transform.position = center + new Vector3(0f, 0.08f * scale, -1.02f * scale);
        porch.transform.localScale = new Vector3(1.2f * scale, 0.06f * scale, 0.42f * scale);
        ApplyColor(porch, new Color(0.42f, 0.24f, 0.1f));
        ConfigureStaticVisual(porch);

        for (int i = 0; i < 2; i++)
        {
            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.transform.SetParent(parent, false);
            barrel.transform.position = center + new Vector3((-0.54f + i * 1.08f) * scale, 0.14f * scale, -0.88f * scale);
            barrel.transform.localScale = new Vector3(0.12f * scale, 0.16f * scale, 0.12f * scale);
            ApplyColor(barrel, new Color(0.34f, 0.2f, 0.08f));
            ConfigureStaticVisual(barrel);
        }

        // Warm point light above entrance
        GameObject lightObj = new("BarLight");
        lightObj.transform.SetParent(parent, false);
        lightObj.transform.position = center + new Vector3(0f, 0.9f * scale, -0.9f * scale);
        Light barLight = lightObj.AddComponent<Light>();
        barLight.type = LightType.Point;
        ServiceDecorationLightStyle barLightStyle = ServiceDecorationStyleService.GetLightStyle(ServiceDecorationKind.Bar);
        barLight.color = barLightStyle.Color;
        barLight.intensity = barLightStyle.Intensity;
        barLight.range = barLightStyle.Range;
        barLight.shadows = LightShadows.None;

        // Walkway from entrance to anchor
        CreateDrivewayToAnchor(parent, min, max, anchor, 0.52f);
    }

    private void CreateCanteenDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        float scale = BuildingDecorScale;
        float width = max.x - min.x + 1;
        float depth = max.y - min.y + 1;
        Color wallColor = new Color(0.83f, 0.78f, 0.68f);
        Color roofColor = new Color(0.18f, 0.54f, 0.56f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(parent, false);
        body.transform.position = center + new Vector3(0f, 0.4f * scale, 0f);
        body.transform.localScale = new Vector3((width - 0.22f) * scale, 0.78f * scale, (depth - 0.22f) * scale);
        ApplyColor(body, wallColor);
        ConfigureStaticVisual(body);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + new Vector3(0f, 0.86f * scale, 0f);
        roof.transform.localScale = new Vector3((width + 0.18f) * scale, 0.1f * scale, (depth + 0.12f) * scale);
        ApplyColor(roof, roofColor);
        ConfigureStaticVisual(roof);

        GameObject awning = GameObject.CreatePrimitive(PrimitiveType.Cube);
        awning.transform.SetParent(parent, false);
        awning.transform.position = center + new Vector3(0f, 0.58f * scale, -0.96f * scale);
        awning.transform.localScale = new Vector3((width - 0.3f) * scale, 0.08f * scale, 0.5f * scale);
        ApplyColor(awning, new Color(0.94f, 0.84f, 0.42f));
        ConfigureStaticVisual(awning);

        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.transform.SetParent(parent, false);
        door.transform.position = center + new Vector3(-(width * 0.24f) * scale, 0.2f * scale, -0.74f * scale);
        door.transform.localScale = new Vector3(0.34f * scale, 0.48f * scale, 0.04f * scale);
        ApplyColor(door, new Color(0.22f, 0.13f, 0.07f));
        ConfigureStaticVisual(door);

        GameObject servingWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        servingWindow.transform.SetParent(parent, false);
        servingWindow.transform.position = center + new Vector3((width * 0.18f) * scale, 0.46f * scale, -0.75f * scale);
        servingWindow.transform.localScale = new Vector3(0.92f * scale, 0.34f * scale, 0.04f * scale);
        ApplyColor(servingWindow, new Color(0.34f, 0.64f, 0.72f));
        ConfigureStaticVisual(servingWindow);
        CreateServiceGlowPanel(parent, servingWindow.transform.position + new Vector3(0f, 0f, -0.02f * scale), new Vector3(1.02f * scale, 0.4f * scale, 0.02f * scale), new Color(0.54f, 0.9f, 0.98f, 0.12f));

        GameObject sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sign.transform.SetParent(parent, false);
        sign.transform.position = center + new Vector3(0f, 0.69f * scale, -0.77f * scale);
        sign.transform.localScale = new Vector3(1.18f * scale, 0.2f * scale, 0.04f * scale);
        ApplyColor(sign, new Color(0.96f, 0.78f, 0.22f));
        ConfigureStaticVisual(sign);
        CreateServiceGlowPanel(parent, sign.transform.position + new Vector3(0f, 0f, -0.022f * scale), new Vector3(1.34f * scale, 0.26f * scale, 0.02f * scale), new Color(1f, 0.9f, 0.58f, 0.16f));

        for (int i = 0; i < 3; i++)
        {
            GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
            table.transform.SetParent(parent, false);
            table.transform.position = center + new Vector3((-0.8f + i * 0.8f) * scale, 0.11f * scale, 0.58f * scale);
            table.transform.localScale = new Vector3(0.46f * scale, 0.1f * scale, 0.28f * scale);
            ApplyColor(table, new Color(0.55f, 0.34f, 0.16f));
            ConfigureStaticVisual(table);

            for (int stoolSide = -1; stoolSide <= 1; stoolSide += 2)
            {
                GameObject stool = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                stool.transform.SetParent(parent, false);
                stool.transform.position = table.transform.position + new Vector3(0f, -0.02f * scale, 0.18f * stoolSide * scale);
                stool.transform.localScale = new Vector3(0.07f * scale, 0.08f * scale, 0.07f * scale);
                ApplyColor(stool, new Color(0.42f, 0.28f, 0.14f));
                ConfigureStaticVisual(stool);
            }
        }

        GameObject lightObj = new("CanteenLight");
        lightObj.transform.SetParent(parent, false);
        lightObj.transform.position = center + new Vector3(0.35f * scale, 0.88f * scale, -0.9f * scale);
        Light canteenLight = lightObj.AddComponent<Light>();
        canteenLight.type = LightType.Point;
        ServiceDecorationLightStyle canteenLightStyle = ServiceDecorationStyleService.GetLightStyle(ServiceDecorationKind.Canteen);
        canteenLight.color = canteenLightStyle.Color;
        canteenLight.intensity = canteenLightStyle.Intensity;
        canteenLight.range = canteenLightStyle.Range;
        canteenLight.shadows = LightShadows.None;

        CreateDrivewayToAnchor(parent, min, max, anchor, 0.52f);
    }

    private void CreateGamblingHallDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        float scale = BuildingDecorScale;
        float width = max.x - min.x + 1;
        float depth = max.y - min.y + 1;
        Color wallColor = new Color(0.28f, 0.12f, 0.34f);
        Color roofColor = new Color(0.62f, 0.18f, 0.72f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(parent, false);
        body.transform.position = center + new Vector3(0f, 0.4f * scale, 0f);
        body.transform.localScale = new Vector3((width - 0.16f) * scale, 0.82f * scale, (depth - 0.16f) * scale);
        ApplyColor(body, wallColor);
        ConfigureStaticVisual(body);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + new Vector3(0f, 0.86f * scale, 0f);
        roof.transform.localScale = new Vector3((width + 0.24f) * scale, 0.12f * scale, (depth + 0.24f) * scale);
        ApplyColor(roof, roofColor);
        ConfigureStaticVisual(roof);

        GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tower.transform.SetParent(parent, false);
        tower.transform.position = center + new Vector3(0f, 1.18f * scale, -0.12f * scale);
        tower.transform.localScale = new Vector3(1.18f * scale, 0.98f * scale, 1.08f * scale);
        ApplyColor(tower, new Color(0.36f, 0.14f, 0.44f));
        ConfigureStaticVisual(tower);

        GameObject neon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        neon.transform.SetParent(parent, false);
        neon.transform.position = center + new Vector3(0f, 0.77f * scale, -1.08f * scale);
        neon.transform.localScale = new Vector3(1.54f * scale, 0.24f * scale, 0.06f * scale);
        ApplyColor(neon, new Color(1f, 0.82f, 0.08f));
        ConfigureStaticVisual(neon);
        CreateServiceGlowPanel(parent, neon.transform.position + new Vector3(0f, 0f, -0.03f * scale), new Vector3(1.84f * scale, 0.34f * scale, 0.02f * scale), new Color(1f, 0.56f, 0.84f, 0.18f));

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        canopy.transform.SetParent(parent, false);
        canopy.transform.position = center + new Vector3(0f, 0.46f * scale, -1.04f * scale);
        canopy.transform.localScale = new Vector3(1.72f * scale, 0.08f * scale, 0.42f * scale);
        ApplyColor(canopy, new Color(0.96f, 0.74f, 0.16f));
        ConfigureStaticVisual(canopy);

        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.transform.SetParent(parent, false);
        door.transform.position = center + new Vector3(0f, 0.24f * scale, -0.84f * scale);
        door.transform.localScale = new Vector3(0.58f * scale, 0.56f * scale, 0.04f * scale);
        ApplyColor(door, new Color(0.18f, 0.10f, 0.04f));
        ConfigureStaticVisual(door);

        for (int side = -1; side <= 1; side += 2)
        {
            GameObject neonPillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            neonPillar.transform.SetParent(parent, false);
            neonPillar.transform.position = center + new Vector3(0.92f * side * scale, 0.6f * scale, -0.96f * scale);
            neonPillar.transform.localScale = new Vector3(0.14f * scale, 0.92f * scale, 0.12f * scale);
            ApplyColor(neonPillar, side < 0 ? new Color(0.24f, 0.92f, 0.86f) : new Color(1f, 0.36f, 0.72f));
            ConfigureStaticVisual(neonPillar);
            CreateServiceGlowPanel(parent, neonPillar.transform.position + new Vector3(0f, 0f, -0.02f * scale), new Vector3(0.22f * scale, 1.04f * scale, 0.02f * scale), side < 0 ? new Color(0.24f, 0.92f, 0.86f, 0.12f) : new Color(1f, 0.36f, 0.72f, 0.12f));
        }

        GameObject lightObj = new("GamblingHallLight");
        lightObj.transform.SetParent(parent, false);
        lightObj.transform.position = center + new Vector3(0f, 1.08f * scale, -0.86f * scale);
        Light gamblingLight = lightObj.AddComponent<Light>();
        gamblingLight.type = LightType.Point;
        ServiceDecorationLightStyle gamblingLightStyle = ServiceDecorationStyleService.GetLightStyle(ServiceDecorationKind.GamblingHall);
        gamblingLight.color = gamblingLightStyle.Color;
        gamblingLight.intensity = gamblingLightStyle.Intensity;
        gamblingLight.range = gamblingLightStyle.Range;
        gamblingLight.shadows = LightShadows.None;

        CreateDrivewayToAnchor(parent, min, max, anchor, 0.52f);
    }

    private void CreateCityParkDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        cityParkBenchPositions.Clear();
        System.Array.Clear(cityParkBenchOccupied, 0, cityParkBenchOccupied.Length);

        const float groundY    = -0.20f; // grass surface relative to center.y
        Color woodColor    = new Color(0.56f, 0.39f, 0.20f);
        Color stoneColor   = new Color(0.66f, 0.64f, 0.58f);
        Color trunkColor   = new Color(0.40f, 0.24f, 0.12f);
        Color crownA       = new Color(0.22f, 0.52f, 0.18f);
        Color crownB       = new Color(0.28f, 0.48f, 0.14f);
        Color bushColor    = new Color(0.24f, 0.50f, 0.16f);
        Color benchColor   = new Color(0.52f, 0.38f, 0.22f);
        Color lampColor    = new Color(0.34f, 0.34f, 0.34f);

        // ── Paths (cross through center) ──────────────────────────────
        void MakePath(Vector3 offset, Vector3 scale)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.name = "ParkPath";
            p.transform.SetParent(parent, false);
            p.transform.position = center + new Vector3(offset.x, groundY + 0.01f, offset.z);
            p.transform.localScale = scale;
            ApplyColor(p, stoneColor);
            ConfigureStaticVisual(p);
        }
        MakePath(Vector3.zero, new Vector3(0.55f, 0.02f, 8.0f));
        MakePath(Vector3.zero, new Vector3(8.0f,  0.02f, 0.55f));

        // ── Fence ─────────────────────────────────────────────────────
        const float parkHalf = 4.0f;
        const float gapHalf  = 0.85f;
        const float railH    = 0.06f;
        const float railD    = 0.09f;
        const float railTop  = 0.30f;
        const float postH    = 0.54f;

        void MakeFenceRail(Vector3 offset, Vector3 scale)
        {
            GameObject r = GameObject.CreatePrimitive(PrimitiveType.Cube);
            r.name = "FenceRail";
            r.transform.SetParent(parent, false);
            r.transform.position = center + new Vector3(offset.x, groundY + railTop, offset.z);
            r.transform.localScale = scale;
            ApplyColor(r, woodColor);
            ConfigureStaticVisual(r);
        }
        void MakeFencePost(float px, float pz)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.name = "FencePost";
            post.transform.SetParent(parent, false);
            post.transform.position = center + new Vector3(px, groundY + postH * 0.5f, pz);
            post.transform.localScale = new Vector3(0.10f, postH, 0.10f);
            ApplyColor(post, woodColor);
            ConfigureStaticVisual(post);
        }

        float sideLen = parkHalf - gapHalf;
        // Four entrance gaps keep the park usable no matter which side workers approach from.
        MakeFenceRail(new Vector3(-(gapHalf + sideLen * 0.5f), 0, -parkHalf), new Vector3(sideLen, railH, railD));
        MakeFenceRail(new Vector3( (gapHalf + sideLen * 0.5f), 0, -parkHalf), new Vector3(sideLen, railH, railD));
        MakeFenceRail(new Vector3(-(gapHalf + sideLen * 0.5f), 0, parkHalf), new Vector3(sideLen, railH, railD));
        MakeFenceRail(new Vector3( (gapHalf + sideLen * 0.5f), 0, parkHalf), new Vector3(sideLen, railH, railD));
        MakeFenceRail(new Vector3(-parkHalf, 0, -(gapHalf + sideLen * 0.5f)), new Vector3(railD, railH, sideLen));
        MakeFenceRail(new Vector3(-parkHalf, 0,  (gapHalf + sideLen * 0.5f)), new Vector3(railD, railH, sideLen));
        MakeFenceRail(new Vector3( parkHalf, 0, -(gapHalf + sideLen * 0.5f)), new Vector3(railD, railH, sideLen));
        MakeFenceRail(new Vector3( parkHalf, 0,  (gapHalf + sideLen * 0.5f)), new Vector3(railD, railH, sideLen));

        float[] postX = { -parkHalf, -2.65f, -1.32f, 0f, 1.32f, 2.65f, parkHalf };
        float[] postZ = { -parkHalf, -2.65f, -1.32f, 0f, 1.32f, 2.65f, parkHalf };
        // South posts (skip entrance gap)
        foreach (float px in postX)
        {
            if (px > -gapHalf - 0.05f && px < gapHalf + 0.05f) continue;
            MakeFencePost(px, -parkHalf);
        }
        // North posts (skip entrance gap)
        foreach (float px in postX)
        {
            if (px > -gapHalf - 0.05f && px < gapHalf + 0.05f) continue;
            MakeFencePost(px, parkHalf);
        }
        // West/East posts (skip already-placed corners)
        foreach (float pz in postZ)
        {
            if (pz == -parkHalf || pz == parkHalf) continue;
            if (pz > -gapHalf - 0.05f && pz < gapHalf + 0.05f) continue;
            MakeFencePost(-parkHalf, pz);
            MakeFencePost( parkHalf, pz);
        }

        // ── Trees ─────────────────────────────────────────────────────
        (Vector3 pos, Color crown, float scale)[] treeDefs = {
            (center + new Vector3(-2.5f, 0, -2.5f), crownA, 1.0f),
            (center + new Vector3( 2.5f, 0, -2.5f), crownB, 1.0f),
            (center + new Vector3(-2.5f, 0,  2.5f), crownB, 1.0f),
            (center + new Vector3( 2.5f, 0,  2.5f), crownA, 1.0f),
            (center + new Vector3(-0.8f, 0,  3.3f), bushColor, 0.62f),
            (center + new Vector3( 0.8f, 0,  3.3f), bushColor, 0.62f),
            (center + new Vector3(-3.3f, 0,  0.8f), crownB,    0.68f),
            (center + new Vector3( 3.3f, 0, -0.8f), crownA,    0.68f),
        };
        foreach (var (tp, crown, sc) in treeDefs)
        {
            float trunkH = 0.88f * sc;
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.name = "TreeTrunk";
            trunk.transform.SetParent(parent, false);
            trunk.transform.position = new Vector3(tp.x, center.y + groundY + trunkH * 0.5f, tp.z);
            trunk.transform.localScale = new Vector3(0.19f * sc, trunkH * 0.5f, 0.19f * sc);
            ApplyColor(trunk, trunkColor);
            ConfigureStaticVisual(trunk);

            float crownR = 1.20f * sc;
            GameObject crownObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crownObj.name = "TreeCrown";
            crownObj.transform.SetParent(parent, false);
            crownObj.transform.position = new Vector3(tp.x, center.y + groundY + trunkH + crownR * 0.5f, tp.z);
            crownObj.transform.localScale = new Vector3(crownR, crownR * 0.82f, crownR);
            ApplyColor(crownObj, crown);
            ConfigureStaticVisual(crownObj);
        }

        // ── Bushes ────────────────────────────────────────────────────
        float[] bx = { -3.4f,  3.4f, -3.4f,  3.4f, -1.5f,  1.5f, -1.5f,  1.5f };
        float[] bz = { -1.5f, -1.5f,  1.5f,  1.5f, -3.4f, -3.4f,  3.4f,  3.4f };
        for (int i = 0; i < bx.Length; i++)
        {
            GameObject bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "Bush";
            bush.transform.SetParent(parent, false);
            bush.transform.position = center + new Vector3(bx[i], groundY + 0.16f, bz[i]);
            bush.transform.localScale = new Vector3(0.38f, 0.26f, 0.38f);
            ApplyColor(bush, bushColor);
            ConfigureStaticVisual(bush);
        }

        // ── Benches ───────────────────────────────────────────────────
        (Vector3 bpos, float rotY)[] benchDefs = {
            (center + new Vector3(-1.5f, 0, 0f),   90f),
            (center + new Vector3( 1.5f, 0, 0f),  -90f),
            (center + new Vector3(0f, 0, -1.5f),    0f),
            (center + new Vector3(0f, 0,  1.5f),  180f),
        };
        foreach (var (bpos, rotY) in benchDefs)
        {
            Quaternion brot = Quaternion.Euler(0f, rotY, 0f);
            if (cityParkBenchPositions.Count < cityParkBenchOccupied.Length)
            {
                Vector3 sitPos = new(bpos.x, center.y + groundY + 0.08f, bpos.z);
                cityParkBenchPositions.Add(sitPos);
            }
            // Seat
            GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seat.name = "BenchSeat";
            seat.transform.SetParent(parent, false);
            seat.transform.position = new Vector3(bpos.x, center.y + groundY + 0.27f, bpos.z);
            seat.transform.rotation = brot;
            seat.transform.localScale = new Vector3(0.62f, 0.05f, 0.22f);
            ApplyColor(seat, benchColor);
            ConfigureStaticVisual(seat);
            // Legs
            for (int s = -1; s <= 1; s += 2)
            {
                Vector3 legOff = brot * new Vector3(s * 0.24f, 0f, 0f);
                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leg.name = "BenchLeg";
                leg.transform.SetParent(parent, false);
                leg.transform.position = new Vector3(bpos.x + legOff.x, center.y + groundY + 0.12f, bpos.z + legOff.z);
                leg.transform.rotation = brot;
                leg.transform.localScale = new Vector3(0.06f, 0.24f, 0.18f);
                ApplyColor(leg, benchColor);
                ConfigureStaticVisual(leg);
            }
        }

        // ── Flowers ───────────────────────────────────────────────────
        (float fx, float fz, Color fc)[] flowerDefs = {
            (-3.1f,  2.0f, new Color(0.88f, 0.22f, 0.22f)),
            ( 3.1f,  2.0f, new Color(0.88f, 0.82f, 0.18f)),
            (-3.1f, -2.0f, new Color(0.88f, 0.82f, 0.18f)),
            ( 3.1f, -2.0f, new Color(0.88f, 0.22f, 0.22f)),
            (-1.8f,  3.1f, new Color(0.88f, 0.22f, 0.22f)),
            ( 1.8f,  3.1f, new Color(0.88f, 0.82f, 0.18f)),
            (-1.8f, -3.1f, new Color(0.88f, 0.82f, 0.18f)),
            ( 1.8f, -3.1f, new Color(0.88f, 0.22f, 0.22f)),
        };
        foreach (var (fx, fz, fc) in flowerDefs)
        {
            GameObject flower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            flower.name = "Flower";
            flower.transform.SetParent(parent, false);
            flower.transform.position = center + new Vector3(fx, groundY + 0.07f, fz);
            flower.transform.localScale = new Vector3(0.11f, 0.08f, 0.11f);
            ApplyColor(flower, fc);
            ConfigureStaticVisual(flower);
        }

        // ── Lamp posts (lights are added by CreateLocationNightLights) ─
        float[] lpx = { -2.5f,  2.5f, -2.5f,  2.5f };
        float[] lpz = { -2.5f, -2.5f,  2.5f,  2.5f };
        for (int i = 0; i < 4; i++)
        {
            float postHeight = 1.30f;
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "LampPost";
            post.transform.SetParent(parent, false);
            post.transform.position = center + new Vector3(lpx[i], groundY + postHeight * 0.5f, lpz[i]);
            post.transform.localScale = new Vector3(0.07f, postHeight * 0.5f, 0.07f);
            ApplyColor(post, lampColor);
            ConfigureStaticVisual(post);

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "LampHead";
            head.transform.SetParent(parent, false);
            head.transform.position = center + new Vector3(lpx[i], groundY + postHeight + 0.07f, lpz[i]);
            head.transform.localScale = new Vector3(0.16f, 0.12f, 0.16f);
            ApplyColor(head, new Color(0.80f, 0.78f, 0.65f));
            ConfigureStaticVisual(head);
        }

        TryCreateSquirrelMemorialSign(parent, center + new Vector3(-2.2f, groundY + 0.02f, -3.55f), Quaternion.identity);
    }

    private bool TryGetFurnitureFactoryPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, LocationType.FurnitureFactory, 3, 2, out min, out max);
    }

    private void CreatePersonalHouseDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        // Oriented root: local +Z toward anchor (road/front), -Z = back of house.
        Vector3 anchorWorld = new Vector3(anchor.x + 0.5f, center.y, anchor.y + 0.5f);
        Vector3 toAnchorRaw = anchorWorld - center;
        toAnchorRaw.y = 0f;
        Vector3 toAnchor = Mathf.Abs(toAnchorRaw.x) >= Mathf.Abs(toAnchorRaw.z)
            ? new Vector3(Mathf.Sign(toAnchorRaw.x), 0f, 0f)
            : new Vector3(0f, 0f, Mathf.Sign(toAnchorRaw.z));

        GameObject orientedRoot = new("HouseOriented");
        orientedRoot.transform.SetParent(parent, false);
        orientedRoot.transform.position = center;
        orientedRoot.transform.rotation = Quaternion.LookRotation(toAnchor, Vector3.up);
        orientedRoot.transform.localScale = Vector3.one * BuildingDecorScale;
        Transform or = orientedRoot.transform;

        // Local coord reference:
        //   footprint 5×6 grid cells → local ≈ ±1.60 wide (X), -1.92 back to +1.92 front (Z)
        //   base block top at local Y ≈ 0.22 ("ground level" for decorations)

        int variant = Random.Range(0, 5);
        switch (variant)
        {
            case 0:  HouseRanch(or);      break;
            case 1:  HouseCapeCod(or);    break;
            case 2:  HouseColonial(or);   break;
            case 3:  HouseCraftsman(or);  break;
            default: HouseSplitLevel(or); break;
        }

        CreateDrivewayToAnchor(parent, min, max, anchor, 0.72f);
    }

    // ── Local helper shared by all house variants ────────────────────────────

    private GameObject HQ(Transform p, Vector3 pos, Vector3 size, Color col)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localScale = size;
        ApplyColor(go, col);
        ConfigureStaticVisual(go);
        return go;
    }

    private void HouseFence(Transform p, float xFrom, float xTo, float fz, Color col)
    {
        // Two horizontal rails
        float cx = (xFrom + xTo) * 0.5f;
        float len = Mathf.Abs(xTo - xFrom);
        HQ(p, new Vector3(cx, 0.38f, fz), new Vector3(len, 0.04f, 0.04f), col);
        HQ(p, new Vector3(cx, 0.30f, fz), new Vector3(len, 0.04f, 0.04f), col);
        // Posts every ~0.38 units
        int posts = Mathf.Max(2, Mathf.RoundToInt(len / 0.38f) + 1);
        for (int i = 0; i < posts; i++)
        {
            float px = xFrom + (xTo - xFrom) * i / Mathf.Max(1, posts - 1);
            HQ(p, new Vector3(px, 0.30f, fz), new Vector3(0.05f, 0.22f, 0.05f), col);
        }
    }

    private void HouseTree(Transform p, float x, float z)
    {
        Color trunk = new Color(0.30f, 0.20f, 0.12f);
        Color canopy = new Color(0.22f, 0.44f, 0.18f);
        GameObject tr = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tr.transform.SetParent(p, false);
        tr.transform.localPosition = new Vector3(x, 0.44f, z);
        tr.transform.localScale = new Vector3(0.08f, 0.22f, 0.08f);
        ApplyColor(tr, trunk); ConfigureStaticVisual(tr);
        GameObject cn = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cn.transform.SetParent(p, false);
        cn.transform.localPosition = new Vector3(x, 0.82f, z);
        cn.transform.localScale = new Vector3(0.50f, 0.42f, 0.50f);
        ApplyColor(cn, canopy); ConfigureStaticVisual(cn);
    }

    private void HouseShrub(Transform p, float x, float z)
    {
        GameObject s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        s.transform.SetParent(p, false);
        s.transform.localPosition = new Vector3(x, 0.32f, z);
        s.transform.localScale = new Vector3(0.30f, 0.24f, 0.30f);
        ApplyColor(s, new Color(0.26f, 0.48f, 0.20f));
        ConfigureStaticVisual(s);
    }

    // ── Variant 0: Ranch (white, horizontal, left garage) ───────────────────

    private void HouseRanch(Transform p)
    {
        Color white   = new Color(0.94f, 0.92f, 0.88f);
        Color roof    = new Color(0.22f, 0.15f, 0.08f);
        Color gray    = new Color(0.82f, 0.81f, 0.78f);
        Color darkPnl = new Color(0.24f, 0.24f, 0.22f);
        Color brick   = new Color(0.62f, 0.26f, 0.16f);
        Color winBlue = new Color(0.56f, 0.76f, 0.86f);
        Color fence   = new Color(0.94f, 0.92f, 0.88f);
        Color conc    = new Color(0.68f, 0.67f, 0.64f);

        // House body (wide, low, back-center)
        HQ(p, new Vector3(0.30f,  0.47f, -1.05f), new Vector3(2.50f, 0.50f, 1.60f), white);
        // Roof flat cap
        HQ(p, new Vector3(0.30f,  0.75f, -1.05f), new Vector3(2.65f, 0.08f, 1.75f), roof);
        // Gable ends
        HQ(p, new Vector3(-0.95f, 0.82f, -1.05f), new Vector3(0.07f, 0.22f, 1.75f), roof);
        HQ(p, new Vector3( 1.55f, 0.82f, -1.05f), new Vector3(0.07f, 0.22f, 1.75f), roof);
        // Garage (left, front section)
        HQ(p, new Vector3(-0.90f, 0.41f, -0.12f), new Vector3(0.82f, 0.38f, 0.85f), gray);
        HQ(p, new Vector3(-0.90f, 0.63f, -0.12f), new Vector3(0.88f, 0.07f, 0.90f), roof);
        // Garage door (front face: z = -0.12 + 0.425 = 0.305)
        HQ(p, new Vector3(-0.90f, 0.42f,  0.32f), new Vector3(0.72f, 0.35f, 0.04f), darkPnl);
        // Front door (front face of house: z = -1.05 + 0.80 = -0.25)
        HQ(p, new Vector3( 0.90f, 0.42f, -0.24f), new Vector3(0.17f, 0.44f, 0.05f), new Color(0.72f, 0.18f, 0.12f));
        // Step
        HQ(p, new Vector3( 0.90f, 0.15f, -0.06f), new Vector3(0.32f, 0.14f, 0.22f), conc);
        // Windows
        HQ(p, new Vector3(-0.05f, 0.53f, -0.24f), new Vector3(0.30f, 0.20f, 0.04f), winBlue);
        HQ(p, new Vector3( 0.55f, 0.53f, -0.24f), new Vector3(0.30f, 0.20f, 0.04f), winBlue);
        // Chimney (right side)
        HQ(p, new Vector3( 1.30f, 0.82f, -0.90f), new Vector3(0.14f, 0.38f, 0.14f), brick);
        HQ(p, new Vector3( 1.30f, 1.02f, -0.90f), new Vector3(0.18f, 0.06f, 0.18f), brick);
        // Driveway (left side)
        HQ(p, new Vector3(-0.90f, 0.22f,  1.00f), new Vector3(0.76f, 0.012f, 1.70f), conc);
        // Fence (gap at left driveway x = -0.90 ± 0.38)
        HouseFence(p, -1.55f, -1.30f,  1.72f, fence);
        HouseFence(p, -0.50f,  1.55f,  1.72f, fence);
        HouseTree(p, -1.40f, 1.10f);
        HouseTree(p,  1.40f, 0.80f);
        HouseShrub(p, 0.20f, -0.06f);
        HouseShrub(p, 1.10f, -0.06f);
    }

    // ── Variant 1: Cape Cod (steel blue, steep roof, right garage) ──────────

    private void HouseCapeCod(Transform p)
    {
        Color blue   = new Color(0.46f, 0.60f, 0.74f);
        Color dark   = new Color(0.22f, 0.22f, 0.24f);
        Color white  = new Color(0.93f, 0.93f, 0.91f);
        Color navy   = new Color(0.14f, 0.22f, 0.44f);
        Color winBlue= new Color(0.56f, 0.76f, 0.86f);
        Color conc   = new Color(0.68f, 0.67f, 0.64f);
        Color fence  = new Color(0.94f, 0.92f, 0.88f);

        // House body (left-center, back)
        HQ(p, new Vector3(-0.40f, 0.53f, -0.90f), new Vector3(1.90f, 0.62f, 1.55f), blue);
        // Steep roof (tall block on top)
        HQ(p, new Vector3(-0.40f, 0.94f, -0.90f), new Vector3(1.96f, 0.65f, 1.62f), dark);
        // White corner trim
        HQ(p, new Vector3(-1.38f, 0.53f, -0.90f), new Vector3(0.07f, 0.62f, 1.55f), white);
        HQ(p, new Vector3( 0.56f, 0.53f, -0.90f), new Vector3(0.07f, 0.62f, 1.55f), white);
        // Dormer window (small bump on roof front)
        HQ(p, new Vector3(-0.20f, 1.28f, -0.88f), new Vector3(0.50f, 0.36f, 0.40f), blue);
        HQ(p, new Vector3(-0.20f, 1.47f, -0.88f), new Vector3(0.52f, 0.06f, 0.42f), dark);
        HQ(p, new Vector3(-0.20f, 1.30f, -0.67f), new Vector3(0.22f, 0.16f, 0.04f), winBlue);
        // Garage (right, front section)
        HQ(p, new Vector3( 1.00f, 0.41f, -0.12f), new Vector3(0.82f, 0.38f, 0.85f), white);
        HQ(p, new Vector3( 1.00f, 0.63f, -0.12f), new Vector3(0.88f, 0.07f, 0.90f), dark);
        HQ(p, new Vector3( 1.00f, 0.42f,  0.32f), new Vector3(0.72f, 0.35f, 0.04f), new Color(0.26f, 0.26f, 0.24f));
        // Front door (front face at z = -0.90 + 0.775 = -0.125)
        HQ(p, new Vector3(-0.55f, 0.45f, -0.11f), new Vector3(0.18f, 0.46f, 0.05f), navy);
        // Step
        HQ(p, new Vector3(-0.55f, 0.15f,  0.06f), new Vector3(0.34f, 0.14f, 0.24f), conc);
        // Windows
        HQ(p, new Vector3(-1.10f, 0.56f, -0.11f), new Vector3(0.26f, 0.20f, 0.04f), winBlue);
        HQ(p, new Vector3( 0.20f, 0.56f, -0.11f), new Vector3(0.26f, 0.20f, 0.04f), winBlue);
        // Driveway (right side)
        HQ(p, new Vector3( 1.00f, 0.22f,  1.00f), new Vector3(0.76f, 0.012f, 1.70f), conc);
        // Fence (gap at right driveway)
        HouseFence(p, -1.55f, 0.60f,  1.72f, fence);
        HouseFence(p,  1.40f, 1.55f,  1.72f, fence);
        HouseTree(p, -1.40f, 0.90f);
        HouseTree(p,  1.50f, 1.30f);
        HouseShrub(p, -0.80f, -0.05f);
    }

    // ── Variant 2: Colonial (cream, tall, symmetrical, columns) ─────────────

    private void HouseColonial(Transform p)
    {
        Color cream  = new Color(0.96f, 0.94f, 0.86f);
        Color dark   = new Color(0.20f, 0.20f, 0.18f);
        Color winYlw = new Color(0.88f, 0.82f, 0.58f);
        Color conc   = new Color(0.76f, 0.74f, 0.70f);
        Color fence  = new Color(0.94f, 0.92f, 0.88f);

        // House body (tall, centered)
        HQ(p, new Vector3(0f,  0.57f, -0.85f), new Vector3(2.55f, 0.70f, 1.65f), cream);
        // Roof
        HQ(p, new Vector3(0f,  0.96f, -0.85f), new Vector3(2.68f, 0.08f, 1.78f), dark);
        // Gable ends
        HQ(p, new Vector3(-1.30f, 1.02f, -0.85f), new Vector3(0.07f, 0.22f, 1.78f), dark);
        HQ(p, new Vector3( 1.30f, 1.02f, -0.85f), new Vector3(0.07f, 0.22f, 1.78f), dark);
        // Porch platform (front, between columns)
        HQ(p, new Vector3(0f,  0.26f,  0.22f), new Vector3(1.85f, 0.07f, 0.60f), conc);
        // Porch pediment (narrow roof over columns)
        HQ(p, new Vector3(0f,  0.72f,  0.20f), new Vector3(2.0f,  0.07f, 0.62f), dark);
        // 4 white columns (Cylinder)
        foreach (float cx in new float[] { -0.68f, -0.22f, 0.22f, 0.68f })
        {
            GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            col.transform.SetParent(p, false);
            col.transform.localPosition = new Vector3(cx, 0.49f, 0.48f);
            col.transform.localScale = new Vector3(0.09f, 0.24f, 0.09f);
            ApplyColor(col, cream); ConfigureStaticVisual(col);
        }
        // Front door (front face: z = -0.85 + 0.825 = -0.025)
        HQ(p, new Vector3(0f,    0.48f, -0.01f), new Vector3(0.22f, 0.52f, 0.05f), dark);
        // 4 symmetrical windows
        HQ(p, new Vector3(-0.88f, 0.61f, -0.01f), new Vector3(0.24f, 0.26f, 0.04f), winYlw);
        HQ(p, new Vector3(-0.44f, 0.61f, -0.01f), new Vector3(0.24f, 0.26f, 0.04f), winYlw);
        HQ(p, new Vector3( 0.44f, 0.61f, -0.01f), new Vector3(0.24f, 0.26f, 0.04f), winYlw);
        HQ(p, new Vector3( 0.88f, 0.61f, -0.01f), new Vector3(0.24f, 0.26f, 0.04f), winYlw);
        // Small garage on right
        HQ(p, new Vector3( 1.05f, 0.41f, -0.12f), new Vector3(0.82f, 0.38f, 0.90f), cream);
        HQ(p, new Vector3( 1.05f, 0.63f, -0.12f), new Vector3(0.88f, 0.07f, 0.94f), dark);
        HQ(p, new Vector3( 1.05f, 0.42f,  0.33f), new Vector3(0.72f, 0.35f, 0.04f), new Color(0.26f, 0.26f, 0.24f));
        // Driveway (right side)
        HQ(p, new Vector3( 1.05f, 0.22f,  1.00f), new Vector3(0.76f, 0.012f, 1.70f), new Color(0.68f, 0.67f, 0.64f));
        // Fence
        HouseFence(p, -1.55f, 0.65f, 1.72f, fence);
        HouseFence(p,  1.45f, 1.55f, 1.72f, fence);
        HouseTree(p, -1.40f, 0.90f);
        HouseTree(p,  1.50f, 1.20f);
        HouseShrub(p, -1.0f, 0.18f);
        HouseShrub(p,  0.6f, 0.18f);
    }
}
