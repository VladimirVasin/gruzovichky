using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void UpdateTruckInteraction()
    {
        if (!isTruckInteracting)
        {
            if (cargoTransferCrate != null && cargoTransferCrate.activeSelf)
            {
                cargoTransferCrate.SetActive(false);
            }

            return;
        }

        truckObject.transform.rotation = Quaternion.Slerp(
            truckObject.transform.rotation,
            truckInteractionTargetRotation,
            7f * Time.deltaTime);

        truckInteractionTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(truckInteractionTimer / TruckCargoInteractionDuration);
        UpdateCargoTransferVisual(progress);
        UpdateTruckVisuals(0f, false);

        if (truckInteractionTimer < TruckCargoInteractionDuration)
        {
            return;
        }

        CompleteTruckInteraction();
    }

    private bool TryStartTruckInteraction(TruckInteractionType interactionType, LocationType locationType)
    {
        LocationData location = ResolveTripLocationInstance(locationType, 0);
        return TryStartTruckInteraction(interactionType, location);
    }

    private bool TryStartTruckInteraction(TruckInteractionType interactionType, LocationData location)
    {
        if (interactionType == TruckInteractionType.None)
        {
            return false;
        }

        if (location == null)
        {
            return false;
        }

        if (!TryAcquireServiceLocation(location))
        {
            isTruckWaitingForService = true;
            queuedTruckInteraction = interactionType;
            queuedServiceLocation = location.Type;
            queuedServiceLocationInstanceId = location.InstanceId;
            SessionDebugLogger.Log("SERVICE", $"{GetLoadedTruckDisplayName()} is waiting for service slot at {GetBuildingInstanceDisplayName(location.Type, location.InstanceId)}.");
            return false;
        }

        isTruckWaitingForService = false;
        queuedTruckInteraction = TruckInteractionType.None;
        queuedServiceLocation = null;
        queuedServiceLocationInstanceId = 0;
        SessionDebugLogger.Log("SERVICE", $"{GetLoadedTruckDisplayName()} acquired service slot at {GetBuildingInstanceDisplayName(location.Type, location.InstanceId)} for {interactionType}.");
        StartTruckInteraction(interactionType, location);
        return true;
    }

    private bool TryResumeQueuedTruckInteraction()
    {
        if (!isTruckWaitingForService || !queuedServiceLocation.HasValue || queuedTruckInteraction == TruckInteractionType.None)
        {
            return false;
        }

        LocationData location = ResolveTripLocationInstance(queuedServiceLocation.Value, queuedServiceLocationInstanceId);
        return TryStartTruckInteraction(queuedTruckInteraction, location);
    }

    private bool TryAcquireServiceLocation(LocationData location)
    {
        return location != null &&
               ServiceSlotCoordinator.TryAcquire(occupiedServiceLocations, location.Type);
    }

    private void ReleaseServiceLocation(LocationType locationType)
    {
        ServiceSlotCoordinator.Release(occupiedServiceLocations, locationType);
    }

    private void StartTruckInteraction(TruckInteractionType interactionType, LocationData location)
    {
        if (isTruckInteracting || location == null)
        {
            return;
        }

        isTruckMoving = false;
        isTruckInteracting = true;
        activeTruckInteraction = interactionType;
        activeServiceLocation = location.Type;
        activeServiceLocationInstanceId = location.InstanceId;
        truckInteractionTimer = 0f;
        RecordWorkerBuildingKnowledge(GetLoadedTruckDriver(), location, "\u0412\u044b\u043f\u043e\u043b\u043d\u0438\u043b \u0440\u0435\u0439\u0441 \u0443 \u043f\u043e\u0441\u0442\u0440\u043e\u0439\u043a\u0438", "Handled a truck stop at this building");

        Vector3 buildingCenter = GetLocationCenter(location);
        Vector3 directionToBuilding = buildingCenter - truckObject.transform.position;
        directionToBuilding.y = 0f;
        if (directionToBuilding.sqrMagnitude < 0.0001f)
        {
            directionToBuilding = Vector3.forward;
        }

        truckInteractionTargetRotation = Quaternion.LookRotation(-directionToBuilding.normalized, Vector3.up);
        truckInteractionBuildingPoint = buildingCenter + directionToBuilding.normalized * -0.2f + Vector3.up * 0.3f;

        bool isCargoTransfer = interactionType != TruckInteractionType.RefuelAtGasStation;
        if (cargoTransferCrate != null)
        {
            cargoTransferCrate.SetActive(isCargoTransfer);
        }

        _ = isCargoTransfer;
    }

    private int GetTruckLoadAmountForCurrentTrip(CargoType cargoType, int availableAtPickup)
    {
        int amount = Mathf.Min(TruckCargoCapacity, Mathf.Max(0, availableAtPickup));
        if (amount <= 0)
        {
            return 0;
        }

        ReprioritizeCurrentTripForCargoPickup(cargoType);

        switch (currentAssignedTrip)
        {
            case TripType.WarehouseToFurnitureFactoryBoards:
                if (cargoType == CargoType.Boards &&
                    ResolveTripLocationInstance(LocationType.FurnitureFactory, currentTripDropoffLocationInstanceId) is LocationData boardFactory)
                {
                    amount = Mathf.Min(amount, Mathf.Max(0, FurnitureFactoryMaxBoardsStorage - boardFactory.BoardsStored));
                }
                break;

            case TripType.WarehouseToFurnitureFactoryTextile:
                if (cargoType == CargoType.Textile &&
                    ResolveTripLocationInstance(LocationType.FurnitureFactory, currentTripDropoffLocationInstanceId) is LocationData textileFactory)
                {
                    amount = Mathf.Min(amount, Mathf.Max(0, FurnitureFactoryMaxTextileStorage - textileFactory.TextileStored));
                }
                break;

            case TripType.WarehouseToDocksLogs:
            case TripType.WarehouseToDocksBoards:
            case TripType.WarehouseToDocksFurniture:
                amount = Mathf.Min(amount, GetDocksExportTripLoadLimit(cargoType));
                break;

            case TripType.DocksToWarehouseCotton:
            case TripType.DocksToWarehouseTextile:
            case TripType.DocksToWarehouseFurniture:
                amount = Mathf.Min(amount, TruckCargoCapacity);
                break;
        }

        return Mathf.Clamp(amount, 0, TruckCargoCapacity);
    }

    private void ReprioritizeCurrentTripForCargoPickup(CargoType cargoType)
    {
        if (cargoType == CargoType.Boards &&
            currentAssignedTrip == TripType.WarehouseToDocksBoards &&
            IsTradeResourceNeededByProduction(TradeResourceType.Boards) &&
            TryFindFactoryNeedingResource(TradeResourceType.Boards, out LocationData furnitureFactory) &&
            HasPath(truckCell, furnitureFactory.Anchor))
        {
            currentAssignedTrip = TripType.WarehouseToFurnitureFactoryBoards;
            currentTripDropoffLocationInstanceId = furnitureFactory.InstanceId;
            currentAssignedTripReward = GetTripReward(currentAssignedTrip);
            SessionDebugLogger.Log("AUTO", $"{GetLoadedTruckDisplayName()} reprioritized loaded Boards to Furniture Factory because production needs input before Docks export.");
        }
    }

    private bool TryFindFactoryNeedingResource(TradeResourceType resourceType, out LocationData factory)
    {
        factory = null;
        foreach (LocationData candidate in EnumerateLocationsOfType(LocationType.FurnitureFactory))
        {
            if (candidate == null)
            {
                continue;
            }

            bool needsResource = resourceType switch
            {
                TradeResourceType.Boards => candidate.BoardsStored < FurnitureFactoryMaxBoardsStorage,
                TradeResourceType.Textile => candidate.TextileStored < FurnitureFactoryMaxTextileStorage,
                _ => false
            };

            if (needsResource)
            {
                factory = candidate;
                return true;
            }
        }

        return false;
    }

    private int GetDocksExportTripLoadLimit(CargoType cargoType)
    {
        TradeResourceType resourceType = CargoTypeToTradeResourceType(cargoType);
        return GetDocksExportTripLoadLimit(resourceType);
    }

    private int GetDocksExportTripLoadLimit(TradeResourceType resourceType)
    {
        LocationData docks = ResolveTripLocationInstance(LocationType.Docks, currentTripDropoffLocationInstanceId);
        LocationData warehouse = ResolveTripLocationInstance(LocationType.Warehouse, currentTripPickupLocationInstanceId);
        return GetDocksExportTripLoadLimit(docks, resourceType, warehouse);
    }

    private int GetDocksExportTripLoadLimit(LocationData docks, TradeResourceType resourceType, LocationData warehouse)
    {
        if (docks == null || warehouse == null)
        {
            return 0;
        }

        int surplusLimit = Mathf.Max(0, GetWarehouseTripResourceAmount(warehouse, resourceType) - GetTradePolicyTarget(resourceType));
        surplusLimit = Mathf.Max(0, surplusLimit - GetProductionReserveForWarehouseExport(resourceType));
        int dockRoom = Mathf.Max(0, DocksResourceCapacity - GetDocksExportStoredResource(docks, resourceType));
        return Mathf.Min(surplusLimit, dockRoom);
    }

    private int GetProductionReserveForWarehouseExport(TradeResourceType resourceType)
    {
        int reserve = 0;
        foreach (LocationData furnitureFactory in EnumerateLocationsOfType(LocationType.FurnitureFactory))
        {
            if (furnitureFactory == null)
            {
                continue;
            }

            reserve += resourceType switch
            {
                TradeResourceType.Boards => Mathf.Max(0, FurnitureFactoryMaxBoardsStorage - furnitureFactory.BoardsStored),
                TradeResourceType.Textile => Mathf.Max(0, FurnitureFactoryMaxTextileStorage - furnitureFactory.TextileStored),
                _ => 0
            };
        }

        return reserve;
    }

    private void SetTruckCargo(CargoType cargoType, int amount)
    {
        if (amount <= 0 || cargoType == CargoType.None)
        {
            ClearTruckCargo();
            return;
        }

        truckCargoAmount = Mathf.Min(amount, TruckCargoCapacity);
        truckCargoType = cargoType;
    }

    private void ClearTruckCargo()
    {
        truckCargoAmount = 0;
        truckCargoType = CargoType.None;
    }

    private LocationData GetActiveTruckInteractionLocation(LocationType expectedType)
    {
        return ResolveTripLocationInstance(expectedType, activeServiceLocationInstanceId);
    }

    private void AddWarehouseTripResource(LocationData warehouse, TradeResourceType resourceType, int amount)
    {
        if (warehouse == null || amount <= 0)
        {
            return;
        }

        switch (resourceType)
        {
            case TradeResourceType.Logs:
                warehouse.LogsStored += amount;
                break;
            case TradeResourceType.Boards:
                warehouse.BoardsStored += amount;
                break;
            case TradeResourceType.Cotton:
                warehouse.CottonStored += amount;
                break;
            case TradeResourceType.Textile:
                warehouse.TextileStored += amount;
                break;
            case TradeResourceType.Furniture:
                warehouse.FurnitureStored += amount;
                break;
        }
    }

    private int ConsumeWarehouseTripResource(LocationData warehouse, TradeResourceType resourceType, int amount)
    {
        if (warehouse == null || amount <= 0)
        {
            return 0;
        }

        int consumed = 0;
        int localAvailable = resourceType switch
        {
            TradeResourceType.Logs      => warehouse.LogsStored,
            TradeResourceType.Boards    => warehouse.BoardsStored,
            TradeResourceType.Cotton    => warehouse.CottonStored,
            TradeResourceType.Textile   => warehouse.TextileStored,
            TradeResourceType.Furniture => warehouse.FurnitureStored,
            _                           => 0
        };

        int fromLocal = Mathf.Min(amount, Mathf.Max(0, localAvailable));
        if (fromLocal > 0)
        {
            switch (resourceType)
            {
                case TradeResourceType.Logs:
                    warehouse.LogsStored -= fromLocal;
                    break;
                case TradeResourceType.Boards:
                    warehouse.BoardsStored -= fromLocal;
                    break;
                case TradeResourceType.Cotton:
                    warehouse.CottonStored -= fromLocal;
                    break;
                case TradeResourceType.Textile:
                    warehouse.TextileStored -= fromLocal;
                    break;
                case TradeResourceType.Furniture:
                    warehouse.FurnitureStored -= fromLocal;
                    break;
            }

            consumed += fromLocal;
        }

        int remaining = amount - consumed;
        bool isPrimaryWarehouse = locations.TryGetValue(LocationType.Warehouse, out LocationData primaryWarehouse) &&
                                  primaryWarehouse == warehouse;
        if (remaining <= 0 || !isPrimaryWarehouse)
        {
            return consumed;
        }

        int fromLegacy = resourceType switch
        {
            TradeResourceType.Cotton    => Mathf.Min(remaining, cottonStored),
            TradeResourceType.Textile   => Mathf.Min(remaining, textileStored),
            TradeResourceType.Furniture => Mathf.Min(remaining, furnitureStored),
            _                           => 0
        };

        if (fromLegacy <= 0)
        {
            return consumed;
        }

        switch (resourceType)
        {
            case TradeResourceType.Cotton:
                cottonStored -= fromLegacy;
                break;
            case TradeResourceType.Textile:
                textileStored -= fromLegacy;
                break;
            case TradeResourceType.Furniture:
                furnitureStored -= fromLegacy;
                break;
        }

        return consumed + fromLegacy;
    }

    private void CompleteTruckInteraction()
    {
        TruckInteractionType completedInteraction = activeTruckInteraction;
        bool completedLoad =
            activeTruckInteraction == TruckInteractionType.LoadAtForest ||
            activeTruckInteraction == TruckInteractionType.LoadAtSawmill ||
            activeTruckInteraction == TruckInteractionType.LoadBoardsAtWarehouse ||
            activeTruckInteraction == TruckInteractionType.LoadTextileAtWarehouse ||
            activeTruckInteraction == TruckInteractionType.LoadAtFurnitureFactory ||
            activeTruckInteraction == TruckInteractionType.TradeLoadAtWarehouse ||
            activeTruckInteraction == TruckInteractionType.LoadLogsAtWarehouse ||
            activeTruckInteraction == TruckInteractionType.LoadFurnitureAtWarehouse ||
            activeTruckInteraction == TruckInteractionType.LoadAtDocks;
        int cargoBeforeAmount = truckCargoAmount;
        CargoType cargoBeforeType = truckCargoType;

        switch (activeTruckInteraction)
        {
            case TruckInteractionType.LoadAtForest:
            {
                LocationData forest = GetActiveTruckInteractionLocation(LocationType.Forest);
                if (forest != null)
                {
                    int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Logs, forest.LogsStored);
                    forest.LogsStored = Mathf.Max(0, forest.LogsStored - amount);
                    RefreshForestStoredLogsVisual(forest);
                    SetTruckCargo(CargoType.Logs, amount);
                }
                break;
            }

            case TruckInteractionType.UnloadAtSawmill:
                if (GetActiveTruckInteractionLocation(LocationType.Sawmill) is LocationData sawmillUnload)
                {
                    sawmillUnload.LogsStored += truckCargoAmount;
                }
                ClearTruckCargo();
                break;

            case TruckInteractionType.LoadAtSawmill:
            {
                LocationData sawmillLoad = GetActiveTruckInteractionLocation(LocationType.Sawmill);
                if (sawmillLoad != null)
                {
                    int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Boards, sawmillLoad.BoardsStored);
                    sawmillLoad.BoardsStored = Mathf.Max(0, sawmillLoad.BoardsStored - amount);
                    SetTruckCargo(CargoType.Boards, amount);
                }
                break;
            }

            case TruckInteractionType.UnloadAtWarehouse:
                if (GetActiveTruckInteractionLocation(LocationType.Warehouse) is LocationData warehouseUnload)
                {
                    if (truckCargoType == CargoType.Logs)
                    {
                        AddWarehouseTripResource(warehouseUnload, TradeResourceType.Logs, truckCargoAmount);
                    }
                    else
                    {
                        AddWarehouseTripResource(warehouseUnload, TradeResourceType.Boards, truckCargoAmount);
                    }
                }
                ClearTruckCargo();
                break;

            case TruckInteractionType.LoadBoardsAtWarehouse:
            {
                LocationData warehouseBoards = GetActiveTruckInteractionLocation(LocationType.Warehouse);
                if (warehouseBoards != null)
                {
                    int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Boards, GetWarehouseTripResourceAmount(warehouseBoards, TradeResourceType.Boards));
                    amount = ConsumeWarehouseTripResource(warehouseBoards, TradeResourceType.Boards, amount);
                    SetTruckCargo(CargoType.Boards, amount);
                }
                break;
            }

            case TruckInteractionType.LoadTextileAtWarehouse:
            {
                LocationData warehouseTextile = GetActiveTruckInteractionLocation(LocationType.Warehouse);
                int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Textile, GetWarehouseTripResourceAmount(warehouseTextile, TradeResourceType.Textile));
                amount = ConsumeWarehouseTripResource(warehouseTextile, TradeResourceType.Textile, amount);
                SetTruckCargo(CargoType.Textile, amount);
                break;
            }

            case TruckInteractionType.LoadLogsAtWarehouse:
            {
                LocationData warehouseLogs = GetActiveTruckInteractionLocation(LocationType.Warehouse);
                if (warehouseLogs != null)
                {
                    int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Logs, GetWarehouseTripResourceAmount(warehouseLogs, TradeResourceType.Logs));
                    amount = ConsumeWarehouseTripResource(warehouseLogs, TradeResourceType.Logs, amount);
                    SetTruckCargo(CargoType.Logs, amount);
                }
                break;
            }

            case TruckInteractionType.LoadFurnitureAtWarehouse:
            {
                LocationData warehouseFurniture = GetActiveTruckInteractionLocation(LocationType.Warehouse);
                int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Furniture, GetWarehouseTripResourceAmount(warehouseFurniture, TradeResourceType.Furniture));
                amount = ConsumeWarehouseTripResource(warehouseFurniture, TradeResourceType.Furniture, amount);
                SetTruckCargo(CargoType.Furniture, amount);
                break;
            }

            case TruckInteractionType.UnloadBoardsAtFurnitureFactory:
                if (GetActiveTruckInteractionLocation(LocationType.FurnitureFactory) is LocationData factoryBoards)
                {
                    factoryBoards.BoardsStored += truckCargoAmount;
                }
                ClearTruckCargo();
                break;

            case TruckInteractionType.UnloadTextileAtFurnitureFactory:
                if (GetActiveTruckInteractionLocation(LocationType.FurnitureFactory) is LocationData factoryTextile)
                {
                    factoryTextile.TextileStored += truckCargoAmount;
                }
                ClearTruckCargo();
                break;

            case TruckInteractionType.LoadAtFurnitureFactory:
            {
                LocationData factoryLoad = GetActiveTruckInteractionLocation(LocationType.FurnitureFactory);
                if (factoryLoad != null)
                {
                    int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Furniture, factoryLoad.FurnitureStored);
                    factoryLoad.FurnitureStored = Mathf.Max(0, factoryLoad.FurnitureStored - amount);
                    SetTruckCargo(CargoType.Furniture, amount);
                }
                break;
            }

            case TruckInteractionType.UnloadFurnitureAtWarehouse:
                AddWarehouseTripResource(GetActiveTruckInteractionLocation(LocationType.Warehouse), TradeResourceType.Furniture, truckCargoAmount);
                ClearTruckCargo();
                break;

            case TruckInteractionType.TradeUnloadAtWarehouse:
                ClearTruckCargo();
                break;

            case TruckInteractionType.TradeLoadAtWarehouse:
                if (activeTradeRun != null)
                {
                    TryConsumeStoredTradeResource(activeTradeRun.ResourceType, activeTradeRun.Quantity);
                    SetTruckCargo(TradeResourceTypeToCargoType(activeTradeRun.ResourceType), activeTradeRun.Quantity);
                }
                break;

            case TruckInteractionType.UnloadAtDocks:
                if (GetActiveTruckInteractionLocation(LocationType.Docks) is LocationData docksUnload)
                {
                    AddDocksExportStoredResource(docksUnload, CargoTypeToTradeResourceType(truckCargoType), truckCargoAmount);
                }
                ClearTruckCargo();
                break;

            case TruckInteractionType.LoadAtDocks:
                if (GetActiveTruckInteractionLocation(LocationType.Docks) is LocationData docksLoad)
                {
                    TradeResourceType resource = GetDocksLoadResourceForTrip(currentAssignedTrip);
                    int amount = GetTruckLoadAmountForCurrentTrip(TradeResourceTypeToCargoType(resource), GetDocksImportStoredResource(docksLoad, resource));
                    if (TryConsumeDocksImportStoredResource(docksLoad, resource, amount))
                    {
                        SetTruckCargo(TradeResourceTypeToCargoType(resource), amount);
                    }
                }
                break;

            case TruckInteractionType.UnloadDocksImportAtWarehouse:
                AddWarehouseTripResource(GetActiveTruckInteractionLocation(LocationType.Warehouse), CargoTypeToTradeResourceType(truckCargoType), truckCargoAmount);
                ClearTruckCargo();
                break;

            case TruckInteractionType.RefuelAtGasStation:
                truckFuel = TruckFuelCapacity;
                break;
        }

        bool cargoChanged = cargoBeforeAmount != truckCargoAmount || cargoBeforeType != truckCargoType;
        isTruckInteracting = false;
        activeTruckInteraction = TruckInteractionType.None;
        truckInteractionTimer = 0f;
        isTruckWaitingForService = false;
        queuedTruckInteraction = TruckInteractionType.None;
        queuedServiceLocation = null;
        queuedServiceLocationInstanceId = 0;
        if (cargoTransferCrate != null)
        {
            cargoTransferCrate.SetActive(false);
        }

        if (activeServiceLocation.HasValue)
        {
            ReleaseServiceLocation(activeServiceLocation.Value);
            activeServiceLocation = null;
            activeServiceLocationInstanceId = 0;
        }

        if (cargoChanged)
        {
            PlayTruckCargoInteractionSound(completedInteraction, completedLoad);
        }
    }

    private void PlayTruckCargoInteractionSound(TruckInteractionType interactionType, bool isLoad)
    {
        AudioClip clip = isLoad
            ? truckLoadClip
            : IsTruckUnloadInteraction(interactionType)
                ? truckUnloadClip
                : null;
        PlayUiSound(clip, 0.58f);
    }

    private static bool IsTruckUnloadInteraction(TruckInteractionType interactionType)
    {
        return interactionType == TruckInteractionType.UnloadAtSawmill ||
               interactionType == TruckInteractionType.UnloadAtWarehouse ||
               interactionType == TruckInteractionType.UnloadBoardsAtFurnitureFactory ||
               interactionType == TruckInteractionType.UnloadTextileAtFurnitureFactory ||
               interactionType == TruckInteractionType.UnloadFurnitureAtWarehouse ||
               interactionType == TruckInteractionType.TradeUnloadAtWarehouse ||
               interactionType == TruckInteractionType.UnloadAtDocks ||
               interactionType == TruckInteractionType.UnloadDocksImportAtWarehouse;
    }
}

