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
        if (interactionType == TruckInteractionType.None)
        {
            return false;
        }

        if (!TryAcquireServiceLocation(locationType))
        {
            isTruckWaitingForService = true;
            queuedTruckInteraction = interactionType;
            queuedServiceLocation = locationType;
            SessionDebugLogger.Log("SERVICE", $"{GetLoadedTruckDisplayName()} is waiting for service slot at {locationType}.");
            return false;
        }

        isTruckWaitingForService = false;
        queuedTruckInteraction = TruckInteractionType.None;
        queuedServiceLocation = null;
        SessionDebugLogger.Log("SERVICE", $"{GetLoadedTruckDisplayName()} acquired service slot at {locationType} for {interactionType}.");
        StartTruckInteraction(interactionType, locationType);
        return true;
    }

    private bool TryResumeQueuedTruckInteraction()
    {
        if (!isTruckWaitingForService || !queuedServiceLocation.HasValue || queuedTruckInteraction == TruckInteractionType.None)
        {
            return false;
        }

        return TryStartTruckInteraction(queuedTruckInteraction, queuedServiceLocation.Value);
    }

    private bool TryAcquireServiceLocation(LocationType locationType)
    {
        return ServiceSlotCoordinator.TryAcquire(occupiedServiceLocations, locationType);
    }

    private void ReleaseServiceLocation(LocationType locationType)
    {
        ServiceSlotCoordinator.Release(occupiedServiceLocations, locationType);
    }

    private void StartTruckInteraction(TruckInteractionType interactionType, LocationType locationType)
    {
        if (isTruckInteracting)
        {
            return;
        }

        isTruckMoving = false;
        isTruckInteracting = true;
        activeTruckInteraction = interactionType;
        activeServiceLocation = locationType;
        truckInteractionTimer = 0f;

        Vector3 buildingCenter = GetLocationCenter(locationType);
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
                    locations.TryGetValue(LocationType.FurnitureFactory, out LocationData boardFactory))
                {
                    amount = Mathf.Min(amount, Mathf.Max(0, FurnitureFactoryMaxBoardsStorage - boardFactory.BoardsStored));
                }
                break;

            case TripType.WarehouseToFurnitureFactoryTextile:
                if (cargoType == CargoType.Textile &&
                    locations.TryGetValue(LocationType.FurnitureFactory, out LocationData textileFactory))
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
            locations.TryGetValue(LocationType.FurnitureFactory, out LocationData furnitureFactory) &&
            HasPath(truckCell, furnitureFactory.Anchor))
        {
            currentAssignedTrip = TripType.WarehouseToFurnitureFactoryBoards;
            currentAssignedTripReward = GetTripReward(currentAssignedTrip);
            SessionDebugLogger.Log("AUTO", $"{GetLoadedTruckDisplayName()} reprioritized loaded Boards to Furniture Factory because production needs input before Docks export.");
        }
    }

    private int GetDocksExportTripLoadLimit(CargoType cargoType)
    {
        if (!locations.TryGetValue(LocationType.Docks, out LocationData docks))
        {
            return 0;
        }

        TradeResourceType resourceType = CargoTypeToTradeResourceType(cargoType);
        return GetDocksExportTripLoadLimit(resourceType);
    }

    private int GetDocksExportTripLoadLimit(TradeResourceType resourceType)
    {
        if (!locations.TryGetValue(LocationType.Docks, out LocationData docks))
        {
            return 0;
        }

        int surplusLimit = Mathf.Max(0, GetWarehouseExportResourceAmount(resourceType) - GetTradePolicyTarget(resourceType));
        surplusLimit = Mathf.Max(0, surplusLimit - GetProductionReserveForWarehouseExport(resourceType));
        int dockRoom = Mathf.Max(0, DocksResourceCapacity - GetDocksExportStoredResource(docks, resourceType));
        return Mathf.Min(surplusLimit, dockRoom);
    }

    private int GetProductionReserveForWarehouseExport(TradeResourceType resourceType)
    {
        if (!locations.TryGetValue(LocationType.FurnitureFactory, out LocationData furnitureFactory))
        {
            return 0;
        }

        return resourceType switch
        {
            TradeResourceType.Boards => Mathf.Max(0, FurnitureFactoryMaxBoardsStorage - furnitureFactory.BoardsStored),
            TradeResourceType.Textile => Mathf.Max(0, FurnitureFactoryMaxTextileStorage - furnitureFactory.TextileStored),
            _ => 0
        };
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

        switch (activeTruckInteraction)
        {
            case TruckInteractionType.LoadAtForest:
            {
                if (locations.TryGetValue(LocationType.Forest, out LocationData forest))
                {
                    int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Logs, forest.LogsStored);
                    forest.LogsStored = Mathf.Max(0, forest.LogsStored - amount);
                    RefreshForestStoredLogsVisual();
                    SetTruckCargo(CargoType.Logs, amount);
                }
                break;
            }

            case TruckInteractionType.UnloadAtSawmill:
                if (locations.TryGetValue(LocationType.Sawmill, out LocationData sawmillUnload))
                {
                    sawmillUnload.LogsStored += truckCargoAmount;
                }
                ClearTruckCargo();
                break;

            case TruckInteractionType.LoadAtSawmill:
            {
                if (locations.TryGetValue(LocationType.Sawmill, out LocationData sawmillLoad))
                {
                    int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Boards, sawmillLoad.BoardsStored);
                    sawmillLoad.BoardsStored = Mathf.Max(0, sawmillLoad.BoardsStored - amount);
                    SetTruckCargo(CargoType.Boards, amount);
                }
                break;
            }

            case TruckInteractionType.UnloadAtWarehouse:
                if (locations.TryGetValue(LocationType.Warehouse, out LocationData warehouseUnload))
                {
                    if (truckCargoType == CargoType.Logs)
                    {
                        warehouseUnload.LogsStored += truckCargoAmount;
                    }
                    else
                    {
                        warehouseUnload.BoardsStored += truckCargoAmount;
                    }
                }
                ClearTruckCargo();
                break;

            case TruckInteractionType.LoadBoardsAtWarehouse:
            {
                if (locations.TryGetValue(LocationType.Warehouse, out LocationData warehouseBoards))
                {
                    int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Boards, warehouseBoards.BoardsStored);
                    warehouseBoards.BoardsStored = Mathf.Max(0, warehouseBoards.BoardsStored - amount);
                    SetTruckCargo(CargoType.Boards, amount);
                }
                break;
            }

            case TruckInteractionType.LoadTextileAtWarehouse:
            {
                int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Textile, textileStored);
                textileStored = Mathf.Max(0, textileStored - amount);
                SetTruckCargo(CargoType.Textile, amount);
                break;
            }

            case TruckInteractionType.LoadLogsAtWarehouse:
            {
                if (locations.TryGetValue(LocationType.Warehouse, out LocationData warehouseLogs))
                {
                    int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Logs, warehouseLogs.LogsStored);
                    warehouseLogs.LogsStored = Mathf.Max(0, warehouseLogs.LogsStored - amount);
                    SetTruckCargo(CargoType.Logs, amount);
                }
                break;
            }

            case TruckInteractionType.LoadFurnitureAtWarehouse:
            {
                int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Furniture, furnitureStored);
                furnitureStored = Mathf.Max(0, furnitureStored - amount);
                SetTruckCargo(CargoType.Furniture, amount);
                break;
            }

            case TruckInteractionType.UnloadBoardsAtFurnitureFactory:
                if (locations.TryGetValue(LocationType.FurnitureFactory, out LocationData factoryBoards))
                {
                    factoryBoards.BoardsStored += truckCargoAmount;
                }
                ClearTruckCargo();
                break;

            case TruckInteractionType.UnloadTextileAtFurnitureFactory:
                if (locations.TryGetValue(LocationType.FurnitureFactory, out LocationData factoryTextile))
                {
                    factoryTextile.TextileStored += truckCargoAmount;
                }
                ClearTruckCargo();
                break;

            case TruckInteractionType.LoadAtFurnitureFactory:
            {
                if (locations.TryGetValue(LocationType.FurnitureFactory, out LocationData factoryLoad))
                {
                    int amount = GetTruckLoadAmountForCurrentTrip(CargoType.Furniture, factoryLoad.FurnitureStored);
                    factoryLoad.FurnitureStored = Mathf.Max(0, factoryLoad.FurnitureStored - amount);
                    SetTruckCargo(CargoType.Furniture, amount);
                }
                break;
            }

            case TruckInteractionType.UnloadFurnitureAtWarehouse:
                furnitureStored += truckCargoAmount;
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
                if (locations.TryGetValue(LocationType.Docks, out LocationData docksUnload))
                {
                    AddDocksExportStoredResource(docksUnload, CargoTypeToTradeResourceType(truckCargoType), truckCargoAmount);
                }
                ClearTruckCargo();
                break;

            case TruckInteractionType.LoadAtDocks:
                if (locations.TryGetValue(LocationType.Docks, out LocationData docksLoad))
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
                AddStoredTradeResource(CargoTypeToTradeResourceType(truckCargoType), truckCargoAmount);
                ClearTruckCargo();
                break;

            case TruckInteractionType.RefuelAtGasStation:
                truckFuel = TruckFuelCapacity;
                break;
        }

        isTruckInteracting = false;
        activeTruckInteraction = TruckInteractionType.None;
        truckInteractionTimer = 0f;
        isTruckWaitingForService = false;
        queuedTruckInteraction = TruckInteractionType.None;
        queuedServiceLocation = null;
        if (cargoTransferCrate != null)
        {
            cargoTransferCrate.SetActive(false);
        }

        if (activeServiceLocation.HasValue)
        {
            ReleaseServiceLocation(activeServiceLocation.Value);
            activeServiceLocation = null;
        }

        _ = completedInteraction;
        _ = completedLoad;
    }
}

