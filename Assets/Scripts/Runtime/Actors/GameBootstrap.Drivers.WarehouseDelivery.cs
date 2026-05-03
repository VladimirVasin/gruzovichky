using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private void UpdateWarehouseDelivery(DriverAgent driver)
    {
        if (driver == null ||
            driver.DutyMode != DriverDutyMode.Logistics ||
            driver.AssignedBuildingType != LocationType.Warehouse ||
            !driver.IsOnActiveShift ||
            !driver.IsInsideBuilding ||
            driver.WalkPhase != DriverRescuePhase.None)
        {
            return;
        }

        if (!IsProductionWorkHour(GetCurrentHour()))
        {
            return;
        }

        if (!locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse))
        {
            return;
        }

        if (!TryChooseWarehouseDeliveryTarget(driver, warehouse, out LocationType deliveryTarget, out int deliveryAmount, out string deliveryReason))
        {
            return;
        }

        WarehouseResourceType resourceType = GetWarehouseResourceTypeForLocation(deliveryTarget);
        deliveryAmount = Mathf.Min(deliveryAmount, GetWarehouseResourceStored(warehouse, resourceType));
        if (deliveryAmount <= 0 || !TryDeductWarehouseResource(warehouse, resourceType, deliveryAmount))
        {
            SessionDebugLogger.Log("WAREHOUSE", $"{driver.DriverName} delivery cancelled: Warehouse lacks {GetWarehouseResourceTypeLabel(resourceType)} for {deliveryTarget}.");
            return;
        }

        driver.WarehouseDeliveryTarget = deliveryTarget;
        driver.WarehouseDeliveryResourceType = resourceType;
        driver.WarehouseDeliveryAmount = deliveryAmount;
        driver.IsCarryingWarehouseDelivery = true;
        driver.IsInsideBuilding = false;

        // Spawn visible at Warehouse anchor
        Vector3 spawnPos = GetCellCenter(locations[LocationType.Warehouse].Anchor);
        spawnPos.y += 0.05f;
        driver.DriverObject.SetActive(true);
        driver.DriverObject.transform.position = spawnPos;
        driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        driver.WalkAnimationTime = 0f;
        ApplyDriverPose(driver, 0f, 0f);

        Vector3 targetPos = GetCellCenter(locations[deliveryTarget].Anchor);
        targetPos.y += 0.05f;
        ResetWorkerLocalBusTripState(driver);
        List<LocationData> orderedStops = GetOrderedLocalStops();
        Vector2Int startCell = WorldToCell(spawnPos);
        Vector2Int targetCell = WorldToCell(targetPos);
        int directDistance = EstimateGridDistance(startCell, targetCell);
        int accessWalkDistance = int.MaxValue;
        int exitWalkDistance = int.MaxValue;
        int originStopNumber = -1;
        int destinationStopNumber = -1;
        foreach (LocationData stop in orderedStops)
        {
            int startToStop = EstimateGridDistance(startCell, stop.Anchor);
            if (startToStop < accessWalkDistance)
            {
                accessWalkDistance = startToStop;
                originStopNumber = stop.StopNumber;
            }

            int targetToStop = EstimateGridDistance(targetCell, stop.Anchor);
            if (targetToStop < exitWalkDistance)
            {
                exitWalkDistance = targetToStop;
                destinationStopNumber = stop.StopNumber;
            }
        }

        WarehouseBusDeliveryDecision busDecision = WarehouseBusDeliveryService.Evaluate(
            workerCanDeliver: true,
            hasCargo: driver.IsCarryingWarehouseDelivery,
            orderedStopCount: orderedStops.Count,
            directDistance: directDistance,
            accessWalkDistance: accessWalkDistance,
            exitWalkDistance: exitWalkDistance,
            directWalkThreshold: WorkerLocalBusDirectWalkThreshold,
            maxAccessWalkDistance: WorkerLocalBusMaxAccessWalkDistance,
            minimumSavings: WorkerLocalBusMinimumSavings,
            sameStop: originStopNumber == destinationStopNumber);
        if (busDecision.Kind == WarehouseBusDeliveryDecisionKind.UseBus &&
            TryStartWorkerLocalBusTrip(
                    driver,
                    spawnPos,
                    targetPos,
                    DriverRescuePhase.WarehouseDeliveryToService,
                    $"{locations[deliveryTarget].Label} delivery",
                    true))
        {
            SessionDebugLogger.Log(
                "WAREHOUSE",
                $"{driver.DriverName} started bus-assisted delivery of {GetWarehouseResourceTypeLabel(resourceType)} x{deliveryAmount} to {locations[deliveryTarget].Label}; reason={deliveryReason}.");
            return;
        }

        driver.WalkTargetWorld = targetPos;
        driver.WalkPhase = DriverRescuePhase.WarehouseDeliveryToService;
        BuildDriverWalkPath(driver, spawnPos, targetPos);
        SessionDebugLogger.Log("WAREHOUSE", $"{driver.DriverName} started walking delivery of {GetWarehouseResourceTypeLabel(resourceType)} x{deliveryAmount} to {locations[deliveryTarget].Label}; reason={deliveryReason}; busDecision={busDecision.Kind} ({busDecision.Reason}).");
    }

    private bool TryChooseWarehouseDeliveryTarget(DriverAgent driver, LocationData warehouse, out LocationType deliveryTarget, out int deliveryAmount, out string reason)
    {
        deliveryTarget = default;
        deliveryAmount = 0;
        reason = "no target";
        if (warehouse == null)
        {
            return false;
        }

        float bestScore = float.MaxValue;
        bool found = false;
        EvaluateWarehouseDeliveryCandidate(driver, warehouse, LocationType.GasStation, WarehouseResourceType.Fuel, GasStationMaxFuelStorage, ref found, ref deliveryTarget, ref deliveryAmount, ref reason, ref bestScore);
        EvaluateWarehouseDeliveryCandidate(driver, warehouse, LocationType.Canteen, WarehouseResourceType.Food, CanteenMaxFoodStorage, ref found, ref deliveryTarget, ref deliveryAmount, ref reason, ref bestScore);
        EvaluateWarehouseDeliveryCandidate(driver, warehouse, LocationType.Bar, WarehouseResourceType.Alcohol, BarMaxAlcoholStorage, ref found, ref deliveryTarget, ref deliveryAmount, ref reason, ref bestScore);
        return found;
    }

    private void EvaluateWarehouseDeliveryCandidate(
        DriverAgent driver,
        LocationData warehouse,
        LocationType serviceType,
        WarehouseResourceType resourceType,
        int serviceCapacity,
        ref bool found,
        ref LocationType deliveryTarget,
        ref int deliveryAmount,
        ref string reason,
        ref float bestScore)
    {
        if (!locations.TryGetValue(serviceType, out LocationData service) || serviceCapacity <= 0)
        {
            return;
        }

        int stored = GetServiceResourceStored(service, resourceType);
        int freeSpace = Mathf.Max(0, serviceCapacity - stored);
        int warehouseStock = GetWarehouseResourceStored(warehouse, resourceType);
        if (freeSpace <= 0 || warehouseStock <= 0)
        {
            return;
        }

        float fillRatio = stored / (float)serviceCapacity;
        float missingRatio = freeSpace / (float)serviceCapacity;
        float demandBoost = GetWarehouseServiceDemandBoost(serviceType, stored, serviceCapacity);
        float score = -(missingRatio + demandBoost);
        if (found && score >= bestScore)
        {
            return;
        }

        int carryAmount = Mathf.Min(GetWarehouseLoaderCarryAmount(driver), warehouseStock, freeSpace);
        if (carryAmount <= 0)
        {
            return;
        }

        found = true;
        deliveryTarget = serviceType;
        deliveryAmount = carryAmount;
        bestScore = score;
        reason = $"{service.Label} fill-to-full={stored}/{serviceCapacity} ({fillRatio:P0}), missing={freeSpace}, demandBoost={demandBoost:0.00}, warehouseStock={warehouseStock}, carry={carryAmount}";
    }

    private float GetWarehouseServiceDemandBoost(LocationType serviceType, int stored, int serviceCapacity)
    {
        float stockUrgency = serviceCapacity <= 0
            ? 0f
            : Mathf.Clamp01(1f - stored / (float)serviceCapacity) * 0.12f;

        int waitingNeedCount = serviceType switch
        {
            LocationType.Canteen => CountWorkersSeekingNeed(WorkerNeedKind.Meal),
            LocationType.Bar => CountWorkersSeekingNeed(WorkerNeedKind.Leisure),
            _ => 0
        };

        float workerDemand = Mathf.Min(0.45f, waitingNeedCount * 0.055f);
        float emptyBonus = stored <= 0 ? 0.35f : 0f;
        return stockUrgency + workerDemand + emptyBonus;
    }

    private int CountWorkersSeekingNeed(WorkerNeedKind need)
    {
        int count = 0;
        foreach (DriverAgent driver in driverAgents)
        {
            if (driver == null)
            {
                continue;
            }

            bool shouldSeek = need switch
            {
                WorkerNeedKind.Meal => ShouldWorkerSeekMeal(driver),
                WorkerNeedKind.Sleep => ShouldWorkerSeekSleep(driver),
                WorkerNeedKind.Leisure => ShouldWorkerSeekLeisure(driver),
                _ => false
            };
            if (shouldSeek)
            {
                count++;
            }
        }

        return count;
    }

    private int GetWarehouseLoaderCarryAmount(DriverAgent driver)
    {
        int logistics = GetWorkerEffectiveSkill(driver, WorkerSkillKind.Logistics);
        if (logistics >= 10) return 4;
        if (logistics >= 7) return 3;
        return 2;
    }

    private static int GetWarehouseResourceStored(LocationData warehouse, WarehouseResourceType resourceType)
    {
        if (warehouse == null)
        {
            return 0;
        }

        return resourceType switch
        {
            WarehouseResourceType.Fuel => warehouse.FuelStored,
            WarehouseResourceType.Alcohol => warehouse.AlcoholStored,
            WarehouseResourceType.Food => warehouse.FoodStored,
            _ => 0
        };
    }

    private static int GetServiceResourceStored(LocationData service, WarehouseResourceType resourceType)
    {
        if (service == null)
        {
            return 0;
        }

        return resourceType switch
        {
            WarehouseResourceType.Fuel => service.FuelStored,
            WarehouseResourceType.Alcohol => service.AlcoholStored,
            WarehouseResourceType.Food => service.FoodStored,
            _ => 0
        };
    }

    private bool TryDeductWarehouseResource(LocationData warehouse, WarehouseResourceType resourceType, int amount)
    {
        if (warehouse == null || amount <= 0 || GetWarehouseResourceStored(warehouse, resourceType) < amount)
        {
            return false;
        }

        switch (resourceType)
        {
            case WarehouseResourceType.Fuel:
                warehouse.FuelStored -= amount;
                break;
            case WarehouseResourceType.Alcohol:
                warehouse.AlcoholStored -= amount;
                break;
            case WarehouseResourceType.Food:
                warehouse.FoodStored -= amount;
                break;
            default:
                return false;
        }

        return true;
    }

    private int AddServiceResource(LocationData service, WarehouseResourceType resourceType, int amount)
    {
        if (service == null || amount <= 0)
        {
            return 0;
        }

        switch (resourceType)
        {
            case WarehouseResourceType.Fuel:
            {
                int before = service.FuelStored;
                service.FuelStored = Mathf.Min(service.FuelStored + amount, GasStationMaxFuelStorage);
                return service.FuelStored - before;
            }
            case WarehouseResourceType.Alcohol:
            {
                int before = service.AlcoholStored;
                service.AlcoholStored = Mathf.Min(service.AlcoholStored + amount, BarMaxAlcoholStorage);
                return service.AlcoholStored - before;
            }
            case WarehouseResourceType.Food:
            {
                int before = service.FoodStored;
                service.FoodStored = Mathf.Min(service.FoodStored + amount, CanteenMaxFoodStorage);
                return service.FoodStored - before;
            }
            default:
                return 0;
        }
    }

}
