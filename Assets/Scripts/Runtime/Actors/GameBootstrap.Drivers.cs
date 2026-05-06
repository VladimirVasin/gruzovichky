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
        if (!BuildDriverWalkPath(driver, startPosition, finalTarget))
        {
            HandleFailedLocalBusFinalWalk(driver, finalWalkPhase, travelReason);
            return false;
        }

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
            LogLocalBusPassengerSkip(driver, reason, "no bus driver assigned to the current shift or no bus available");
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
        if (!BuildDriverWalkPath(driver, startPosition, waitPoint))
        {
            ResetWorkerLocalBusTripState(driver);
            driver.WalkPhase = DriverRescuePhase.None;
            LogLocalBusPassengerSkip(driver, reason, $"no safe walk path to Stop #{originStop.StopNumber}");
            return false;
        }
        SessionDebugLogger.Log(
            "BUS_PASSENGER",
            $"{driver.DriverName} chose local bus for {driver.BusTravelReason}: Stop #{originStop.StopNumber} -> Stop #{destinationStop.StopNumber}, finalPhase={finalWalkPhase}, finalCell=({WorldToCell(finalTargetWorld).x},{WorldToCell(finalTargetWorld).y}), fareExempt={(fareExempt ? "yes" : "no")}.");
        return true;
    }

    private void SetDriverDutyMode(DriverAgent driver, DriverDutyMode dutyMode)
    {
        if (driver == null || driver.DutyMode == dutyMode)
        {
            return;
        }

        // Leaving a direct building assignment: release its active inside count.
        if (driver.DutyMode == DriverDutyMode.Logistics && driver.AssignedBuildingType.HasValue)
        {
            if (driver.AssignedBuildingType == LocationType.Forest)
            {
                CancelForestFieldWork(driver);
            }

            LocationData bd = GetAssignedBuildingLocation(driver);
            if (driver.IsInsideBuilding && bd != null)
            {
                bd.Workers = Mathf.Max(0, bd.Workers - 1);
                driver.IsInsideBuilding = false;
                driver.DriverObject?.SetActive(true);
                driver.DriverObject.transform.position = driver.MotelIdlePosition;
            }
            driver.AssignedBuildingType = null;
            driver.AssignedBuildingInstanceId = 0;
            driver.AssignedBuildingSlotIndex = -1;
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
               driver.WalkPhase != DriverRescuePhase.IdleAtPersonalHouseMeal &&
               driver.WalkPhase != DriverRescuePhase.IdleAtTrashCan &&
               driver.WalkPhase != DriverRescuePhase.IdleAtGamblingHall &&
               driver.WalkPhase != DriverRescuePhase.IdleAtCityPark &&
               driver.WalkPhase != DriverRescuePhase.AtLaborExchange &&
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
               driver.WalkPhase == DriverRescuePhase.ToPersonalHouseMeal ||
               driver.WalkPhase == DriverRescuePhase.IdleAtPersonalHouseMeal ||
               driver.WalkPhase == DriverRescuePhase.IdleWalkToTrashCan ||
               driver.WalkPhase == DriverRescuePhase.IdleAtTrashCan ||
               driver.WalkPhase == DriverRescuePhase.IdleWalkToGamblingHall ||
               driver.WalkPhase == DriverRescuePhase.IdleAtGamblingHall ||
               driver.WalkPhase == DriverRescuePhase.IdleWalkToCityPark ||
               driver.WalkPhase == DriverRescuePhase.IdleAtCityPark ||
               driver.WalkPhase == DriverRescuePhase.IdleExitCityPark ||
               driver.WalkPhase == DriverRescuePhase.AtLaborExchange ||
               driver.WalkPhase == DriverRescuePhase.IdleSmoking ||
               driver.WalkPhase == DriverRescuePhase.IdlePhoneCall ||
               driver.WalkPhase == DriverRescuePhase.IdleWalkToCat ||
               driver.WalkPhase == DriverRescuePhase.IdlePettingCat;
    }

    private void InterruptDriverIdleActivityForShift(DriverAgent driver, string destinationLabel)
    {
        if (driver == null || !IsDriverInIdleActivity(driver))
        {
            return;
        }

        DriverRescuePhase interruptedPhase = driver.WalkPhase;
        if (driver.IsInsideBuilding)
        {
            ExitWorkerServiceInterior(driver, interruptedPhase);
        }

        if (interruptedPhase == DriverRescuePhase.IdleSittingOnBench ||
            interruptedPhase == DriverRescuePhase.IdleAtCityPark ||
            interruptedPhase == DriverRescuePhase.IdleExitCityPark)
        {
            ReleaseBench(driver);
        }

        if (interruptedPhase == DriverRescuePhase.IdleSmoking)
        {
            StopDriverSmokingParticles(driver);
        }

        if (interruptedPhase == DriverRescuePhase.IdlePettingCat ||
            interruptedPhase == DriverRescuePhase.IdleWalkToCat)
        {
            ReleaseCatInteraction(driver);
        }

        driver.WalkPhase = DriverRescuePhase.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;
        driver.IdleActivityTimer = 0f;
        driver.IdleWanderPauseTimer = 0f;
        driver.IdleWanderPointIndex = -1;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        driver.LifeGoal = WorkerLifeGoal.None;
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} interrupted {interruptedPhase} to commute to {destinationLabel}.");
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

        InterruptDriverIdleActivityForShift(driver, "Parking");
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
        if (TryStartWorkerPersonalCarTrip(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld, DriverRescuePhase.ToParkingForShift, $"{assignedTruck.DisplayName} shift"))
        {
            SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} started personal car commute to Parking for {assignedTruck.DisplayName}.");
            return;
        }
        if (CanWorkerUsePersonalCar(driver))
        {
            SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} could not start personal car commute to Parking for {assignedTruck.DisplayName}; no car route.");
            return;
        }

        if (!BuildDriverWalkPath(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld))
        {
            driver.WalkPhase = DriverRescuePhase.None;
            driver.WalkTargetWorld = driver.DriverObject.transform.position;
            SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} could not start commute to Parking for {assignedTruck.DisplayName}; no safe walk path.");
            return;
        }
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

        if (!CanBuildingWorkerWorkToday(driver.AssignedBuildingType.Value))
        {
            return false;
        }

        int currentHour = GetCurrentHour();
        if (IsBuildingWorkerWorkHour(driver.AssignedBuildingType.Value, GetLogisticsWorkerSlotIndex(driver), currentHour))
        {
            return true;
        }

        if (!HasServiceWorkerSlot(driver.AssignedBuildingType.Value))
        {
            return false;
        }

        int shiftStart = GetBuildingWorkerShiftStartHour(driver.AssignedBuildingType.Value, GetLogisticsWorkerSlotIndex(driver));
        if (shiftStart < 0)
        {
            return false;
        }

        int currentMinutes = GetCurrentTotalMinutes();
        int shiftStartMinutes = shiftStart * 60;
        int minutesUntilShift = (shiftStartMinutes - currentMinutes + 24 * 60) % (24 * 60);
        return minutesUntilShift > 0 && minutesUntilShift <= Mathf.RoundToInt(DriverShiftArrivalLeadHours * 60f);
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

            if (driver.WaitingForShiftAtParking)
            {
                if (IsLogisticsWorkerWorkHour(driver))
                {
                    StartLogisticsWorkerShiftAtBuilding(driver);
                }
                return;
            }

            bool shouldHead = ShouldLogisticsWorkerHeadToBuilding(driver);
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

        LocationData building = GetAssignedBuildingLocation(driver);
        if (building == null)
        {
            return;
        }

        InterruptDriverIdleActivityForShift(driver, building.Label);
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
        driver.WaitingForShiftAtParking = false;
        ReleaseBench(driver);
        ApplyDriverPose(driver, 0f, 0f);

        Vector3 target = GetCellCenter(building.RoadAccess == default ? building.Anchor : building.RoadAccess);
        target.y = SampleTerrainHeight(target.x, target.z);
        ResetWorkerLocalBusTripState(driver);
        if (TryStartWorkerPersonalCarTrip(driver, driver.DriverObject.transform.position, target, DriverRescuePhase.ToBuildingForShift, $"{building.Label} shift"))
        {
            return;
        }
        if (CanWorkerUsePersonalCar(driver))
        {
            SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} could not start personal car commute to {building.Label}; no car route.");
            return;
        }

        if (TryStartWorkerLocalBusTrip(driver, driver.DriverObject.transform.position, target, DriverRescuePhase.ToBuildingForShift, $"{building.Label} shift"))
        {
            return;
        }

        driver.WalkPhase = DriverRescuePhase.ToBuildingForShift;
        driver.WalkTargetWorld = target;
        if (!BuildDriverWalkPath(driver, driver.DriverObject.transform.position, target))
        {
            driver.WalkPhase = DriverRescuePhase.None;
            driver.WalkTargetWorld = driver.DriverObject.transform.position;
            SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} could not start commute to {building.Label}; no safe walk path to the building access cell.");
            return;
        }

        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} started commute to {building.Label}.");
    }

    private void StartLogisticsWorkerShiftAtBuilding(DriverAgent driver)
    {
        if (driver == null || driver.IsOnActiveShift || !driver.AssignedBuildingType.HasValue)
        {
            return;
        }

        LocationData building = GetAssignedBuildingLocation(driver);
        if (building == null)
        {
            driver.WaitingForShiftAtParking = false;
            return;
        }

        driver.WaitingForShiftAtParking = false;
        driver.WalkPhase = DriverRescuePhase.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;
        driver.IsOnActiveShift = true;
        driver.IsInsideBuilding = true;
        driver.IsShiftSalaryPending = true;
        if (driver.DriverObject != null)
        {
            driver.DriverObject.SetActive(false);
        }

        building.Workers = Mathf.Min(
            building.Workers + 1,
            GetMaxBuildingWorkerSlots(driver.AssignedBuildingType.Value));
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} entered {building.Label} - building operational.");
    }

    private void UpdateLogisticsShiftEnd(DriverAgent driver)
    {
        if (driver == null || driver.DutyMode != DriverDutyMode.Logistics ||
            !driver.IsOnActiveShift || !driver.AssignedBuildingType.HasValue)
        {
            return;
        }

        if (IsLogisticsWorkerWorkHour(driver))
        {
            return;
        }

        LocationData building = GetAssignedBuildingLocation(driver);
        if (driver.IsInsideBuilding && building != null)
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


}
