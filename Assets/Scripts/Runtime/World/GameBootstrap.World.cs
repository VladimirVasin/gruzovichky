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
        extraServiceLocations.Clear();
        nextLocationInstanceId = 1;
        localStops.Clear();
        locationTrashCanMealTargets.Clear();
        hasShownLocalBusStopMinimumHint = false;
        personalHouses.Clear();
        personalHouseSelectionHighlights.Clear();
        workerFamilies.Clear();
        pendingWorkerFamilyFormations.Clear();
        workerChildren.Clear();
        nextWorkerFamilyId = 1;
        nextWorkerChildId = 1;
        selectedPersonalHouseIndex = -1;

        GeneratedWorldLayout layout = WorldLayoutGenerator.Generate(GridWidth, GridHeight, waterCells, HasRequiredLayoutRoads);

        CreateLocation(LocationType.IntercityStop, "Intercity Stop", layout.BusStop.Min, layout.BusStop.Max, layout.BusStop.Anchor, new Color(0.82f, 0.24f, 0.22f), layout.BusStop.RoadAccess);

        SessionDebugLogger.Log(
            "WORLD",
            $"Generated player layout ({selectedGameStartMode}): BusStop {FormatPlacement(layout.BusStop)}. Parking/Warehouse skipped for Build menu; all production/service buildings are player-built.");
    }

    private bool HasRequiredLayoutRoads(GeneratedWorldLayout layout)
    {
        return true;
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

        foreach (LocationData location in extraServiceLocations)
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

        foreach (LocationData location in extraServiceLocations)
        {
            if (location.RootObject != null)
            {
                location.RootObject.transform.position = new Vector3(0f, GetLocationBaseHeight(location), 0f);
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
        NotifyCityComplaintServiceBuilt(LocationType.Bar);
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
        NotifyCityComplaintServiceBuilt(LocationType.Canteen);
        return true;
    }

    private bool TryPlaceKioskAtAnchor(Vector2Int anchorCell)
    {
        if (!TryGetKioskPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Kiosk placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.Kiosk, "Kiosk", min, max, anchorCell, new Color(0.86f, 0.58f, 0.24f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        SessionDebugLogger.Log("BUILD", $"Placed Kiosk at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        NotifyCityComplaintServiceBuilt(LocationType.Kiosk);
        return true;
    }

    private bool TryPlaceGamblingHallAtAnchor(Vector2Int anchorCell)
    {
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
        NotifyCityComplaintServiceBuilt(LocationType.GamblingHall);
        return true;
    }

    private bool TryPlaceCityParkAtAnchor(Vector2Int anchorCell)
    {
        if (!TryGetCityParkPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"City Park placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.CityPark, "City Park", min, max, anchorCell, new Color(0.30f, 0.52f, 0.22f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        SessionDebugLogger.Log("BUILD", $"Placed City Park at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        NotifyTutorialServiceBuildingBuilt(LocationType.CityPark);
        NotifyCityComplaintServiceBuilt(LocationType.CityPark);
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
        QueueMotelBootstrapWorkerWave();
        SessionDebugLogger.Log("BUILD", $"Placed Motel at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        NotifyCityComplaintServiceBuilt(LocationType.Motel);
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

    private bool TryGetKioskPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, LocationType.Kiosk, 2, 1, out min, out max);
    }

    private bool TryGetGamblingHallPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetRotatedBuildingPlacement(anchorCell, LocationType.GamblingHall, 3, 3, out min, out max);
    }

    private bool TryGetCityParkPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        // Park has no driveway or road requirement; the clicked cell is only a pedestrian entrance marker.
        GetRotatedBuildingFootprint(anchorCell, 8, 8, out min, out max);

        if (!IsInsideGrid(anchorCell) || IsBuildingPlacementBlockedCell(anchorCell))
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
        GetRotatedBuildingFootprint(anchorCell, 8, 8, out Vector2Int min, out Vector2Int max);
        SetBuildFootprintPreviewCells(min, max);
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

        if (locations.ContainsKey(type) && !IsMultiInstanceServiceBuildType(type))
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

    private void SetBuildFootprintPreviewCells(Vector2Int min, Vector2Int max, Vector2Int? drivewayCell = null)
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

    private bool GetKioskPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetRotatedBuildingPlacementPreview(anchorCell, LocationType.Kiosk, 2, 1, out previewPosition, out previewScale);
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
        SetBuildFootprintPreviewCells(previewMin, previewMax, DoesLocationRequireRoadAccess(type) ? anchorCell : null);
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


}
