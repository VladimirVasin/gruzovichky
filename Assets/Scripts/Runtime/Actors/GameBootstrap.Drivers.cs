using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private const int WorkerLocalBusDirectWalkThreshold = 18;
    private const int WorkerLocalBusMaxAccessWalkDistance = 10;
    private const int WorkerLocalBusMinimumSavings = 6;

    private bool IsDriverOnShift(DriverAgent driver)
    {
        if (driver == null) return false;
        if (IsDriverIntercity(driver)) return false;
        if (driver.DutyMode == DriverDutyMode.Logistics) return driver.IsOnActiveShift;
        if (driver.ShiftStartHour < 0) return false; // Idle: no shift assigned
        return driver.IsOnActiveShift;
    }

    private static bool IsDriverIntercity(DriverAgent driver)
    {
        return driver != null && driver.DutyMode == DriverDutyMode.Intercity;
    }

    private int GetBusDriverShiftSlotIndex(DriverAgent driver)
    {
        if (driver == null)
        {
            return -1;
        }

        for (int i = 0; i < busDriverShiftIds.Length; i++)
        {
            if (busDriverShiftIds[i] == driver.DriverId)
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsDriverBusDriver(DriverAgent driver)
    {
        return GetBusDriverShiftSlotIndex(driver) >= 0;
    }

    private void ResetWorkerLocalBusTripState(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        driver.BusOriginStopNumber = -1;
        driver.BusDestinationStopNumber = -1;
        driver.BusFinalWalkPhase = DriverRescuePhase.None;
        driver.BusFinalTargetWorld = Vector3.zero;
        driver.BusTravelReason = string.Empty;
        driver.BusRideFareExempt = false;
    }

    private static int EstimateGridDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private Vector3 GetLocalStopPassengerWaitPoint(LocationData stop)
    {
        float x = (stop.Min.x + stop.Max.x + 1) * 0.5f;
        float z = (stop.Min.y + stop.Max.y + 1) * 0.5f;
        return new Vector3(x, SampleTerrainHeight(x, z) + RoadHeight + 0.05f, z);
    }

    private bool TryFindWorkerLocalBusTrip(
        Vector3 startPosition,
        Vector3 finalTargetWorld,
        out LocationData originStop,
        out LocationData destinationStop)
    {
        originStop = null;
        destinationStop = null;

        List<LocationData> orderedStops = GetOrderedLocalStops();
        if (orderedStops.Count < 2)
        {
            return false;
        }

        Vector2Int startCell = WorldToCell(startPosition);
        Vector2Int goalCell = WorldToCell(finalTargetWorld);
        int directDistance = EstimateGridDistance(startCell, goalCell);
        if (directDistance < WorkerLocalBusDirectWalkThreshold)
        {
            return false;
        }

        int bestOriginDistance = int.MaxValue;
        int bestDestinationDistance = int.MaxValue;

        for (int i = 0; i < orderedStops.Count; i++)
        {
            LocationData stop = orderedStops[i];
            int startDistance = EstimateGridDistance(startCell, stop.Anchor);
            if (startDistance < bestOriginDistance)
            {
                bestOriginDistance = startDistance;
                originStop = stop;
            }

            int goalDistance = EstimateGridDistance(goalCell, stop.Anchor);
            if (goalDistance < bestDestinationDistance)
            {
                bestDestinationDistance = goalDistance;
                destinationStop = stop;
            }
        }

        if (originStop == null ||
            destinationStop == null ||
            originStop.StopNumber == destinationStop.StopNumber ||
            bestOriginDistance > WorkerLocalBusMaxAccessWalkDistance ||
            bestDestinationDistance > WorkerLocalBusMaxAccessWalkDistance)
        {
            return false;
        }

        int combinedWalkDistance = bestOriginDistance + bestDestinationDistance;
        return combinedWalkDistance <= directDistance - WorkerLocalBusMinimumSavings;
    }

    private bool TryStartWorkerLocalBusTrip(
        DriverAgent driver,
        Vector3 startPosition,
        Vector3 finalTargetWorld,
        DriverRescuePhase finalWalkPhase,
        string reason,
        bool fareExempt = false)
    {
        if (driver == null || driver.DriverObject == null)
        {
            return false;
        }

        if (!TryFindWorkerLocalBusTrip(startPosition, finalTargetWorld, out LocationData originStop, out LocationData destinationStop))
        {
            return false;
        }

        Vector3 waitPoint = GetLocalStopPassengerWaitPoint(originStop);
        driver.BusOriginStopNumber = originStop.StopNumber;
        driver.BusDestinationStopNumber = destinationStop.StopNumber;
        driver.BusFinalWalkPhase = finalWalkPhase;
        driver.BusFinalTargetWorld = finalTargetWorld;
        driver.BusTravelReason = reason ?? string.Empty;
        driver.BusRideFareExempt = fareExempt;
        driver.WalkPhase = DriverRescuePhase.WalkToLocalBusStop;
        driver.WalkTargetWorld = waitPoint;
        driver.WalkAnimationTime = 0f;
        BuildDriverWalkPath(driver, startPosition, waitPoint);
        SessionDebugLogger.Log(
            "BUS_PASSENGER",
            $"{driver.DriverName} chose local bus for {driver.BusTravelReason}: Stop #{originStop.StopNumber} -> Stop #{destinationStop.StopNumber}, finalPhase={finalWalkPhase}, finalCell=({WorldToCell(finalTargetWorld).x},{WorldToCell(finalTargetWorld).y}), fareExempt={(fareExempt ? "yes" : "no")}.");
        return true;
    }

    private static WarehouseResourceType GetWarehouseResourceTypeForLocation(LocationType locationType)
    {
        return locationType switch
        {
            LocationType.GasStation => WarehouseResourceType.Fuel,
            LocationType.Bar => WarehouseResourceType.Alcohol,
            LocationType.Canteen => WarehouseResourceType.Food,
            _ => WarehouseResourceType.Food
        };
    }

    private static string GetWarehouseResourceTypeLabel(WarehouseResourceType resourceType)
    {
        return resourceType switch
        {
            WarehouseResourceType.Fuel => "Fuel",
            WarehouseResourceType.Alcohol => "Alcohol",
            WarehouseResourceType.Food => "Food",
            _ => "Resource"
        };
    }

    private void ClearWarehouseDeliveryCargo(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        driver.WarehouseDeliveryResourceType = default;
        driver.WarehouseDeliveryAmount = 0;
        driver.IsCarryingWarehouseDelivery = false;
    }

    private void RefundWarehouseDeliveryCargoToWarehouse(DriverAgent driver)
    {
        if (driver == null ||
            !driver.IsCarryingWarehouseDelivery ||
            driver.WarehouseDeliveryAmount <= 0 ||
            !locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse))
        {
            return;
        }

        switch (driver.WarehouseDeliveryResourceType)
        {
            case WarehouseResourceType.Fuel:
                warehouse.FuelStored = Mathf.Min(warehouse.FuelStored + driver.WarehouseDeliveryAmount, WarehouseMaxFuelStorage);
                break;
            case WarehouseResourceType.Alcohol:
                warehouse.AlcoholStored = Mathf.Min(warehouse.AlcoholStored + driver.WarehouseDeliveryAmount, WarehouseMaxAlcoholStorage);
                break;
            case WarehouseResourceType.Food:
                warehouse.FoodStored = Mathf.Min(warehouse.FoodStored + driver.WarehouseDeliveryAmount, WarehouseMaxFoodStorage);
                break;
        }

        SessionDebugLogger.Log(
            "WAREHOUSE",
            $"{driver.DriverName} returned {GetWarehouseResourceTypeLabel(driver.WarehouseDeliveryResourceType)} x{driver.WarehouseDeliveryAmount} to Warehouse after delivery interruption.");
        ClearWarehouseDeliveryCargo(driver);
    }

    private void SetDriverDutyMode(DriverAgent driver, DriverDutyMode dutyMode)
    {
        if (driver == null || driver.DutyMode == dutyMode)
        {
            return;
        }

        // Leaving Logistics: release building slot
        if (driver.DutyMode == DriverDutyMode.Logistics && driver.AssignedBuildingType.HasValue)
        {
            if (driver.AssignedBuildingType == LocationType.Forest)
            {
                CancelForestFieldWork(driver);
            }

            if (driver.IsInsideBuilding && locations.TryGetValue(driver.AssignedBuildingType.Value, out LocationData bd))
            {
                bd.Workers = Mathf.Max(0, bd.Workers - 1);
                driver.IsInsideBuilding = false;
                driver.DriverObject?.SetActive(true);
                driver.DriverObject.transform.position = driver.MotelIdlePosition;
            }
            driver.AssignedBuildingType = null;
        }

        if (dutyMode != DriverDutyMode.Local)
        {
            for (int i = 0; i < busDriverShiftIds.Length; i++)
            {
                if (busDriverShiftIds[i] == driver.DriverId)
                {
                    busDriverShiftIds[i] = 0;
                }
            }
        }

        driver.DutyMode = dutyMode;
        if (dutyMode == DriverDutyMode.Intercity || dutyMode == DriverDutyMode.Logistics)
        {
            driver.ShiftStartHour = -1;
            driver.IsOnActiveShift = false;
            driver.WaitingForShiftAtParking = false;
            driver.NeedsShiftEndReturn = false;
            driver.IsShiftSalaryPending = false;
        }

        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
        isFleetScreenDirty = true;
    }
    private static bool IsDriverIdleWanderPhase(DriverAgent driver)
    {
        return driver != null && driver.WalkPhase == DriverRescuePhase.IdleWander;
    }

    private static bool IsDriverIdleConversing(DriverAgent driver)
    {
        return driver != null && driver.IdleConversationTimer > 0f && driver.WalkPhase == DriverRescuePhase.None;
    }

    private static bool IsDriverBusyWalkPhase(DriverAgent driver)
    {
        return driver != null &&
               driver.WalkPhase != DriverRescuePhase.None &&
               driver.WalkPhase != DriverRescuePhase.IdleWander &&
               driver.WalkPhase != DriverRescuePhase.IdleSittingOnBench &&
               driver.WalkPhase != DriverRescuePhase.IdleAtBar &&
               driver.WalkPhase != DriverRescuePhase.IdleAtCanteen &&
               driver.WalkPhase != DriverRescuePhase.IdleAtGamblingHall &&
               driver.WalkPhase != DriverRescuePhase.IdleAtCityPark &&
               driver.WalkPhase != DriverRescuePhase.IdleSmoking &&
               driver.WalkPhase != DriverRescuePhase.IdlePhoneCall;
    }

    private void ReleaseBench(DriverAgent driver)
    {
        if (driver.SittingBenchIndex >= 0 && driver.SittingBenchIndex < benchOccupied.Length)
        {
            benchOccupied[driver.SittingBenchIndex] = false;
        }
        driver.SittingBenchIndex = -1;

        if (driver.CityParkBenchIndex >= 0 && driver.CityParkBenchIndex < cityParkBenchOccupied.Length)
        {
            cityParkBenchOccupied[driver.CityParkBenchIndex] = false;
        }
        driver.CityParkBenchIndex = -1;
        driver.CityParkPromenadeStep = 0;
    }

    private static bool IsDriverInIdleActivity(DriverAgent driver)
    {
        if (driver == null) return false;
        return driver.WalkPhase == DriverRescuePhase.IdleWalkToBench ||
               driver.WalkPhase == DriverRescuePhase.IdleSittingOnBench ||
               driver.WalkPhase == DriverRescuePhase.IdleWalkToBar ||
               driver.WalkPhase == DriverRescuePhase.IdleAtBar ||
               driver.WalkPhase == DriverRescuePhase.IdleWalkToCanteen ||
               driver.WalkPhase == DriverRescuePhase.IdleAtCanteen ||
               driver.WalkPhase == DriverRescuePhase.IdleWalkToGamblingHall ||
               driver.WalkPhase == DriverRescuePhase.IdleAtGamblingHall ||
               driver.WalkPhase == DriverRescuePhase.IdleWalkToCityPark ||
               driver.WalkPhase == DriverRescuePhase.IdleAtCityPark ||
               driver.WalkPhase == DriverRescuePhase.IdleSmoking ||
               driver.WalkPhase == DriverRescuePhase.IdlePhoneCall;
    }

    private bool ShouldDriverHeadToShift(DriverAgent driver)
    {
        if (driver == null || IsDriverIntercity(driver) || driver.DutyMode == DriverDutyMode.Logistics || driver.ShiftStartHour < 0 || driver.AssignedTruckNumber <= 0)
        {
            return false;
        }

        int minutesUntilShiftStart = GetMinutesUntilShiftStart(driver);
        return minutesUntilShiftStart > 0 && minutesUntilShiftStart <= Mathf.RoundToInt(DriverShiftArrivalLeadHours * 60f);
    }

    private bool TryBoardDriverToAssignedTruck(DriverAgent driver)
    {
        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (driver == null || assignedTruck == null)
        {
            return false;
        }

        if (!driver.WaitingForShiftAtParking)
        {
            return false;
        }

        if (assignedTruck.Driver != null && assignedTruck.Driver != driver)
        {
            return false;
        }

        LoadTruckState(assignedTruck);
        bool canBoard =
            !isTruckMoving &&
            !isTruckInteracting &&
            !isDriverRescueActive &&
            currentAssignedTrip == TripType.None &&
            currentRefuelPhase == RefuelPhase.None &&
            IsTruckInsideParking();
        SaveTruckState(assignedTruck);
        if (!canBoard)
        {
            return false;
        }

        assignedTruck.Driver = driver;
        driver.WaitingForShiftAtParking = false;
        driver.DriverObject.SetActive(false);
        driver.WalkPhase = DriverRescuePhase.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;
        SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} boarded {assignedTruck.DisplayName} in Parking.");
        return true;
    }

    private void StartDriverShiftCommute(DriverAgent driver)
    {
        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (driver == null || assignedTruck == null || driver.DriverObject == null)
        {
            return;
        }

        if (driver.DriverObject.activeSelf == false)
        {
            driver.DriverObject.SetActive(true);
            driver.DriverObject.transform.position = driver.MotelIdlePosition;
            driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }

        driver.WaitingForShiftAtParking = false;
        driver.IdleWanderPauseTimer = 0f;
        driver.IdleWanderPointIndex = -1;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        driver.WalkAnimationTime = 0f;
        ReleaseBench(driver);
        ApplyDriverPose(driver, 0f, 0f);
        driver.WalkPhase = DriverRescuePhase.ToParkingForShift;
        driver.WalkTargetWorld = GetDriverParkingWaitPosition(assignedTruck);
        BuildDriverWalkPath(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld);
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} started commute to Parking for {assignedTruck.DisplayName}.");
    }

    private void StartDriverMotelRest(TruckAgent truckAgent, DriverAgent driver)
    {
        if (driver == null || truckAgent == null || driver.DriverObject == null)
        {
            return;
        }

        truckAgent.Driver = null;
        driver.IsOnActiveShift = false;
        driver.WaitingForShiftAtParking = false;
        driver.NeedsShiftEndReturn = false;
        driver.DriverObject.SetActive(true);
        driver.DriverObject.transform.position = GetDriverStandPointNearTruck();
        driver.DriverObject.transform.rotation = truckAgent.TruckObject != null ? truckAgent.TruckObject.transform.rotation : Quaternion.LookRotation(Vector3.forward, Vector3.up);
        driver.WalkAnimationTime = 0f;
        ApplyDriverPose(driver, 0f, 0f);
        StartWorkerLifeCycleAfterWork(driver, driver.DriverObject.transform.position, $"{truckAgent.DisplayName} shift");
    }

    private bool ShouldLogisticsWorkerHeadToBuilding(DriverAgent driver)
    {
        if (driver == null || driver.DutyMode != DriverDutyMode.Logistics || !driver.AssignedBuildingType.HasValue)
        {
            return false;
        }

        return IsProductionWorkHour(GetCurrentHour());
    }

    private void UpdateDriverShiftPreparation(DriverAgent driver)
    {
        // Logistics workers: commute to building instead of parking
        if (driver?.DutyMode == DriverDutyMode.Logistics)
        {
            if (driver.IsArrivingByBus || driver.IsOnActiveShift ||
                driver.RestPhase != DriverRestPhase.None || IsDriverBusyWalkPhase(driver) ||
                !driver.AssignedBuildingType.HasValue)
            {
                return;
            }

            bool shouldHead = ShouldLogisticsWorkerHeadToBuilding(driver) ||
                              IsProductionWorkHour(GetCurrentHour());
            if (shouldHead)
            {
                StartDriverBuildingCommute(driver);
            }

            return;
        }

        if (IsDriverBusDriver(driver))
        {
            if (driver == null ||
                driver.IsArrivingByBus ||
                driver.ShiftStartHour < 0 ||
                driver.IsOnActiveShift ||
                driver.RestPhase != DriverRestPhase.None ||
                IsDriverBusyWalkPhase(driver) ||
                IsBusDriverOnActiveRoute(driver))
            {
                return;
            }

            bool shouldCommuteToBusShift = ShouldBusDriverHeadToShift(driver) || IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour);
            if (!shouldCommuteToBusShift)
            {
                return;
            }

            if (driver.WaitingForShiftAtParking)
            {
                TryBoardBusDriver(driver);
                return;
            }

            StartBusDriverShiftCommute(driver);
            return;
        }

        if (driver == null || IsDriverIntercity(driver) || driver.DutyMode == DriverDutyMode.Logistics || driver.IsArrivingByBus || driver.ShiftStartHour < 0 || driver.IsOnActiveShift || driver.RestPhase != DriverRestPhase.None || IsDriverBusyWalkPhase(driver) || driver.AssignedTruckNumber <= 0)
        {
            return;
        }

        bool shouldCommuteToShift = ShouldDriverHeadToShift(driver) || IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour);
        if (!shouldCommuteToShift)
        {
            return;
        }

        if (driver.WaitingForShiftAtParking)
        {
            TryBoardDriverToAssignedTruck(driver);
            return;
        }

        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (assignedTruck == null)
        {
            return;
        }

        StartDriverShiftCommute(driver);
    }

    private void StartDriverBuildingCommute(DriverAgent driver)
    {
        if (driver == null || !driver.AssignedBuildingType.HasValue || driver.DriverObject == null)
        {
            return;
        }

        if (!locations.TryGetValue(driver.AssignedBuildingType.Value, out LocationData building))
        {
            return;
        }

        if (!driver.DriverObject.activeSelf)
        {
            driver.DriverObject.SetActive(true);
            driver.DriverObject.transform.position = driver.MotelIdlePosition;
            driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }

        driver.IdleWanderPauseTimer = 0f;
        driver.IdleWanderPointIndex = -1;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        driver.WalkAnimationTime = 0f;
        ReleaseBench(driver);
        ApplyDriverPose(driver, 0f, 0f);

        Vector3 target = GetCellCenter(building.Anchor);
        target.y += 0.05f;
        ResetWorkerLocalBusTripState(driver);
        if (TryStartWorkerLocalBusTrip(driver, driver.DriverObject.transform.position, target, DriverRescuePhase.ToBuildingForShift, $"{building.Label} shift"))
        {
            return;
        }

        driver.WalkPhase = DriverRescuePhase.ToBuildingForShift;
        driver.WalkTargetWorld = target;
        BuildDriverWalkPath(driver, driver.DriverObject.transform.position, target);
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} started commute to {building.Label}.");
    }

    private void UpdateLogisticsShiftEnd(DriverAgent driver)
    {
        if (driver == null || driver.DutyMode != DriverDutyMode.Logistics ||
            !driver.IsOnActiveShift || !driver.AssignedBuildingType.HasValue)
        {
            return;
        }

        if (IsProductionWorkHour(GetCurrentHour()))
        {
            return;
        }

        bool onDeliveryWalk = driver.WalkPhase == DriverRescuePhase.WarehouseDeliveryToService ||
                              driver.WalkPhase == DriverRescuePhase.WarehouseDeliveryReturn;
        bool onWarehouseDeliveryBusTrip =
            driver.WarehouseDeliveryTarget.HasValue &&
            (driver.WalkPhase == DriverRescuePhase.WalkToLocalBusStop ||
             driver.WalkPhase == DriverRescuePhase.WaitingAtLocalBusStop ||
             driver.WalkPhase == DriverRescuePhase.RidingLocalBus);

        if (driver.IsInsideBuilding && locations.TryGetValue(driver.AssignedBuildingType.Value, out LocationData building))
        {
            // Normal exit: worker is invisible inside the building
            building.Workers = Mathf.Max(0, building.Workers - 1);
            driver.IsInsideBuilding = false;
            driver.IsOnActiveShift = false;
            driver.IsShiftSalaryPending = true;
            PayDriverSalary(driver);

            Vector3 exitPos = GetCellCenter(building.Anchor);
            exitPos.y += 0.05f;
            driver.DriverObject.SetActive(true);
            driver.DriverObject.transform.position = exitPos;
            driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            driver.WalkAnimationTime = 0f;
            ApplyDriverPose(driver, 0f, 0f);

            StartWorkerLifeCycleAfterWork(driver, exitPos, building.Label);
        }
        else if (onDeliveryWalk || onWarehouseDeliveryBusTrip)
        {
            // Shift ended mid-delivery: cancel delivery, restore cargo to Warehouse if still carried, then go to after-work flow
            if (locations.TryGetValue(driver.AssignedBuildingType.Value, out LocationData deliveryBuilding))
                deliveryBuilding.Workers = Mathf.Max(0, deliveryBuilding.Workers - 1);

            bool wasRidingLocalBus = driver.WalkPhase == DriverRescuePhase.RidingLocalBus;
            if (wasRidingLocalBus && localBusRoute != null)
            {
                localBusRoute.PassengerCount = Mathf.Max(0, localBusRoute.PassengerCount - 1);
            }

            if (!driver.DriverObject.activeSelf)
            {
                driver.DriverObject.SetActive(true);
                if (wasRidingLocalBus && localBusRoute?.RootTransform != null)
                {
                    driver.DriverObject.transform.position = localBusRoute.RootTransform.position;
                }
            }

            driver.WalkPhase = DriverRescuePhase.None;
            driver.WalkPath.Clear();
            driver.WalkWaypointIndex = 0;
            driver.WalkAnimationTime = 0f;
            ApplyDriverPose(driver, 0f, 0f);
            driver.IsInsideBuilding = false;
            driver.IsOnActiveShift = false;
            driver.IsShiftSalaryPending = true;
            RefundWarehouseDeliveryCargoToWarehouse(driver);
            ResetWorkerLocalBusTripState(driver);
            driver.WarehouseDeliveryTarget = null;
            PayDriverSalary(driver);

            StartWorkerLifeCycleAfterWork(driver, driver.DriverObject.transform.position, "interrupted delivery");
        }
        else
        {
            // Field-worker case: lumberyard worker may still be outside with a tree task.
            if (driver.AssignedBuildingType == LocationType.Forest && driver.DriverObject != null && driver.DriverObject.activeSelf)
            {
                CancelForestFieldWork(driver);
                driver.IsOnActiveShift = false;
                driver.IsInsideBuilding = false;
                driver.IsShiftSalaryPending = true;
                PayDriverSalary(driver);
                StartWorkerLifeCycleAfterWork(driver, driver.DriverObject.transform.position, "Lumberyard field work");
                return;
            }

            // Safety: shift ended but driver not inside (edge case)
            driver.IsOnActiveShift = false;
        }
    }

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

        // Find a service building that needs resupply
        LocationType? deliveryTarget = null;
        if (warehouse.FuelStored > 0 &&
            locations.TryGetValue(LocationType.GasStation, out LocationData gs) &&
            gs.FuelStored < GasStationMaxFuelStorage)
        {
            deliveryTarget = LocationType.GasStation;
        }
        else if (warehouse.AlcoholStored > 0 &&
                 locations.TryGetValue(LocationType.Bar, out LocationData bar) &&
                 bar.AlcoholStored < BarMaxAlcoholStorage)
        {
            deliveryTarget = LocationType.Bar;
        }
        else if (warehouse.FoodStored > 0 &&
                 locations.TryGetValue(LocationType.Canteen, out LocationData canteen) &&
                 canteen.FoodStored < CanteenMaxFoodStorage)
        {
            deliveryTarget = LocationType.Canteen;
        }

        if (!deliveryTarget.HasValue)
        {
            return;
        }

        // Deduct resource from Warehouse and remember the carried unit on the worker
        WarehouseResourceType resourceType = GetWarehouseResourceTypeForLocation(deliveryTarget.Value);
        switch (deliveryTarget.Value)
        {
            case LocationType.GasStation: warehouse.FuelStored--;    break;
            case LocationType.Bar:        warehouse.AlcoholStored--; break;
            case LocationType.Canteen:    warehouse.FoodStored--;    break;
        }

        driver.WarehouseDeliveryTarget = deliveryTarget;
        driver.WarehouseDeliveryResourceType = resourceType;
        driver.WarehouseDeliveryAmount = 1;
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

        Vector3 targetPos = GetCellCenter(locations[deliveryTarget.Value].Anchor);
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
                    $"{locations[deliveryTarget.Value].Label} delivery",
                    true))
        {
            SessionDebugLogger.Log(
                "WAREHOUSE",
                $"{driver.DriverName} started bus-assisted delivery of {GetWarehouseResourceTypeLabel(resourceType)} x1 to {locations[deliveryTarget.Value].Label}.");
            return;
        }

        driver.WalkTargetWorld = targetPos;
        driver.WalkPhase = DriverRescuePhase.WarehouseDeliveryToService;
        BuildDriverWalkPath(driver, spawnPos, targetPos);
        SessionDebugLogger.Log("WAREHOUSE", $"{driver.DriverName} started walking delivery of {GetWarehouseResourceTypeLabel(resourceType)} x1 to {locations[deliveryTarget.Value].Label}; busDecision={busDecision.Kind} ({busDecision.Reason}).");
    }

}
