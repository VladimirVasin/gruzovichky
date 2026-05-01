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

    private int GetCurrentShiftPresetIndex()
    {
        int hour = GetCurrentHour();
        for (int i = 0; i < ShiftPresetHours.Length; i++)
        {
            if (IsHourInShiftWindow(hour, ShiftPresetHours[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsLocalBusServiceAvailableForPassengers()
    {
        if (GetOrderedLocalStops().Count < 2)
        {
            return false;
        }

        if (localBusRoute != null &&
            localBusRoute.Driver != null &&
            localBusRoute.RootTransform != null &&
            localBusRoute.Phase != LocalBusPhase.None &&
            localBusRoute.Phase != LocalBusPhase.ReturningToParking)
        {
            return true;
        }

        int shiftIndex = GetCurrentShiftPresetIndex();
        if (shiftIndex < 0 ||
            shiftIndex >= busDriverShiftIds.Length ||
            busDriverShiftIds[shiftIndex] <= 0)
        {
            return false;
        }

        DriverAgent busDriver = GetDriverAgentById(busDriverShiftIds[shiftIndex]);
        return busDriver != null &&
               IsDriverBusDriver(busDriver) &&
               (HasAvailableBusInParking() || IsBusDriverOnActiveRoute(busDriver));
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

    private bool ResumeWorkerLocalBusTripOnFoot(DriverAgent driver, string blockReason)
    {
        if (driver == null ||
            driver.DriverObject == null ||
            driver.WalkPhase != DriverRescuePhase.WaitingAtLocalBusStop)
        {
            return false;
        }

        DriverRescuePhase finalWalkPhase = driver.BusFinalWalkPhase;
        Vector3 finalTarget = driver.BusFinalTargetWorld;
        string travelReason = driver.BusTravelReason;
        if (finalWalkPhase == DriverRescuePhase.None)
        {
            ResetWorkerLocalBusTripState(driver);
            driver.WalkPhase = DriverRescuePhase.None;
            return false;
        }

        Vector3 startPosition = driver.DriverObject.transform.position;
        ResetWorkerLocalBusTripState(driver);
        driver.DriverObject.SetActive(true);
        driver.WalkPhase = finalWalkPhase;
        driver.WalkTargetWorld = finalTarget;
        driver.WalkAnimationTime = 0f;
        BuildDriverWalkPath(driver, startPosition, finalTarget);
        SessionDebugLogger.Log(
            "BUS_PASSENGER",
            $"{driver.DriverName} stopped waiting for local bus and continued on foot: {blockReason}. Trip={travelReason}, finalPhase={finalWalkPhase}.");
        return true;
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

        if (!IsLocalBusServiceAvailableForPassengers())
        {
            SessionDebugLogger.Log(
                "BUS_PASSENGER",
                $"{driver.DriverName} skipped local bus for {reason}: no bus driver assigned to the current shift or no bus available.");
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
               driver.WalkPhase != DriverRescuePhase.IdleAtTrashCan &&
               driver.WalkPhase != DriverRescuePhase.IdleAtGamblingHall &&
               driver.WalkPhase != DriverRescuePhase.IdleAtCityPark &&
               driver.WalkPhase != DriverRescuePhase.IdleSmoking &&
               driver.WalkPhase != DriverRescuePhase.IdlePhoneCall &&
               driver.WalkPhase != DriverRescuePhase.IdlePettingCat;
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
        driver.CityParkActivityStyle = 0;
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
               driver.WalkPhase == DriverRescuePhase.IdleWalkToTrashCan ||
               driver.WalkPhase == DriverRescuePhase.IdleAtTrashCan ||
               driver.WalkPhase == DriverRescuePhase.IdleWalkToGamblingHall ||
               driver.WalkPhase == DriverRescuePhase.IdleAtGamblingHall ||
               driver.WalkPhase == DriverRescuePhase.IdleWalkToCityPark ||
               driver.WalkPhase == DriverRescuePhase.IdleAtCityPark ||
               driver.WalkPhase == DriverRescuePhase.IdleExitCityPark ||
               driver.WalkPhase == DriverRescuePhase.IdleSmoking ||
               driver.WalkPhase == DriverRescuePhase.IdlePhoneCall ||
               driver.WalkPhase == DriverRescuePhase.IdleWalkToCat ||
               driver.WalkPhase == DriverRescuePhase.IdlePettingCat;
    }

    private bool ShouldDriverHeadToShift(DriverAgent driver)
    {
        if (driver == null || IsDriverIntercity(driver) || driver.DutyMode == DriverDutyMode.Logistics || driver.ShiftStartHour < 0)
        {
            return false;
        }

        int minutesUntilShiftStart = GetMinutesUntilShiftStart(driver);
        return minutesUntilShiftStart > 0 && minutesUntilShiftStart <= Mathf.RoundToInt(DriverShiftArrivalLeadHours * 60f);
    }

    private bool TryBoardDriverToAssignedTruck(DriverAgent driver)
    {
        if (driver == null || IsDriverBusDriver(driver))
        {
            return false;
        }

        if (driver == null || !TryReserveAvailableTruckForDriver(driver, out TruckAgent assignedTruck, "boarding freight shift"))
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
        if (driver == null || IsDriverBusDriver(driver) || driver.DriverObject == null || !TryReserveAvailableTruckForDriver(driver, out TruckAgent assignedTruck, "freight shift commute"))
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

        if (driver == null || IsDriverIntercity(driver) || driver.DutyMode == DriverDutyMode.Logistics || driver.IsArrivingByBus || driver.ShiftStartHour < 0 || driver.IsOnActiveShift || driver.RestPhase != DriverRestPhase.None || IsDriverBusyWalkPhase(driver))
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

        if (!TryReserveAvailableTruckForDriver(driver, out _, "freight shift start"))
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
                SyncLocalBusAgentState();
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
        float demandBoost = GetWarehouseServiceDemandBoost(serviceType, stored, serviceCapacity);
        float score = fillRatio - demandBoost;
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
        reason = $"{service.Label} fill={stored}/{serviceCapacity} ({fillRatio:P0}), demandBoost={demandBoost:0.00}, warehouseStock={warehouseStock}, carry={carryAmount}";
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
