using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private bool IsDriverOnShift(DriverAgent driver)
    {
        if (driver == null) return false;
        if (IsDriverIntercity(driver)) return false;
        if (driver.DutyMode == DriverDutyMode.Logistics) return driver.IsOnActiveShift;
        if (driver.ShiftStartHour < 0) return false; // Idle вЂ” no shift assigned
        return driver.IsOnActiveShift;
    }

    private static bool IsDriverIntercity(DriverAgent driver)
    {
        return driver != null && driver.DutyMode == DriverDutyMode.Intercity;
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
            if (driver.IsInsideBuilding && locations.TryGetValue(driver.AssignedBuildingType.Value, out LocationData bd))
            {
                bd.Workers = 0;
                driver.IsInsideBuilding = false;
                driver.DriverObject?.SetActive(true);
                driver.DriverObject.transform.position = driver.MotelIdlePosition;
            }
            driver.AssignedBuildingType = null;
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

        if (driver.IsInsideBuilding && locations.TryGetValue(driver.AssignedBuildingType.Value, out LocationData building))
        {
            // Normal exit: worker is invisible inside the building
            locations[driver.AssignedBuildingType.Value].Workers = 0;
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
        else if (onDeliveryWalk)
        {
            // Shift ended mid-delivery: cancel delivery, walk to motel from current position
            if (locations.TryGetValue(driver.AssignedBuildingType.Value, out LocationData deliveryBuilding))
                locations[driver.AssignedBuildingType.Value].Workers = 0;

            driver.IsInsideBuilding = false;
            driver.IsOnActiveShift = false;
            driver.IsShiftSalaryPending = true;
            driver.WarehouseDeliveryTarget = null;
            PayDriverSalary(driver);

            StartWorkerLifeCycleAfterWork(driver, driver.DriverObject.transform.position, "interrupted delivery");
        }
        else
        {
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

        // Deduct resource from Warehouse
        switch (deliveryTarget.Value)
        {
            case LocationType.GasStation: warehouse.FuelStored--;    break;
            case LocationType.Bar:        warehouse.AlcoholStored--; break;
            case LocationType.Canteen:    warehouse.FoodStored--;    break;
        }

        driver.WarehouseDeliveryTarget = deliveryTarget;
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
        driver.WalkTargetWorld = targetPos;
        driver.WalkPhase = DriverRescuePhase.WarehouseDeliveryToService;
        BuildDriverWalkPath(driver, spawnPos, targetPos);
        SessionDebugLogger.Log("WAREHOUSE", $"{driver.DriverName} started delivery of {deliveryTarget.Value} resource to {locations[deliveryTarget.Value].Label}.");
    }

    private void UpdateWorkerLifeCycleDailyState(DriverAgent driver)
    {
        if (driver == null) return;

        int hour = GetCurrentHour();
        if (driver.LifeCycleLastHour >= 0 && hour < driver.LifeCycleLastHour)
        {
            driver.NeedsCycleResetPending = true;
            SessionDebugLogger.Log("LIFE", $"{driver.DriverName} queued a new daily life cycle; currentGoal={driver.LifeGoal}, rest={driver.RestPhase}, needs={FormatWorkerNeedsDebug(driver)}.");
        }

        driver.LifeCycleLastHour = hour;
        if (!driver.NeedsCycleResetPending || driver.LifeGoal != WorkerLifeGoal.Idle && driver.LifeGoal != WorkerLifeGoal.None)
        {
            return;
        }

        if (driver.RestPhase != DriverRestPhase.None || IsDriverBusyWalkPhase(driver) || IsDriverInIdleActivity(driver))
        {
            return;
        }

        driver.WorkedToday = false;
        driver.AteToday = false;
        driver.HadLeisureToday = false;
        driver.SleptToday = false;
        driver.LifeGoal = WorkerLifeGoal.None;
        driver.NeedsCycleResetPending = false;
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} started a new daily life cycle; helperFlags reset; needs remain timer-based: {FormatWorkerNeedsDebug(driver)}.");
    }

    private bool TryStartDueWorkerLifeCycle(DriverAgent driver)
    {
        if (driver == null ||
            driver.DutyMode != DriverDutyMode.Local ||
            driver.ShiftStartHour >= 0 ||
            driver.AssignedTruckNumber > 0 ||
            IsDriverIntercity(driver))
        {
            return false;
        }

        int hour = GetCurrentHour();
        bool hasDueNeed = (!driver.AteToday && ShouldWorkerSeekMeal(driver)) ||
                          (!driver.HadLeisureToday && ShouldWorkerSeekLeisure(driver)) ||
                          (!driver.SleptToday && ShouldWorkerSeekSleep(driver));
        if (hour < ProductionWorkEndHour || !hasDueNeed || (driver.AteToday && driver.HadLeisureToday && driver.SleptToday))
        {
            return false;
        }

        // Unemployed workers skip WORK, then run the same evening life chain.
        driver.WorkedToday = true;
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} skipped WORK (unemployed/local); dueNeeds={FormatWorkerNeedsDebug(driver)}.");
        return ContinueWorkerLifeCycle(driver, driver.DriverObject.transform.position);
    }

    private void StartWorkerLifeCycleAfterWork(DriverAgent driver, Vector3 startPosition, string sourceLabel)
    {
        if (driver == null) return;

        UpdateWorkerLifeCycleDailyState(driver);
        driver.WorkedToday = true;
        driver.AteToday = false;
        driver.HadLeisureToday = false;
        driver.SleptToday = false;
        driver.LifeGoal = WorkerLifeGoal.None;
        driver.RestPhase = DriverRestPhase.None;
        driver.IdleWanderPauseTimer = 0f;
        driver.IdleWanderPointIndex = -1;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        ReleaseBench(driver);

        ApplyWorkerAfterWorkEffects(driver, driver.AssignedBuildingType);
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} completed WORK ({sourceLabel}); evaluating needs: {FormatWorkerNeedsDebug(driver)}.");
        if (!ContinueWorkerLifeCycle(driver, startPosition))
        {
            driver.IdleWanderPauseTimer = Random.Range(WorkerFreeIdleMinDuration, WorkerFreeIdleMaxDuration);
        }
    }

    private bool ContinueWorkerLifeCycle(DriverAgent driver, Vector3 startPosition)
    {
        if (driver == null || driver.DriverObject == null) return false;

        if (!driver.AteToday && ShouldWorkerSeekMeal(driver) && TryStartWorkerLifeGoal(driver, WorkerLifeGoal.Eat, startPosition))
        {
            return true;
        }

        if (!driver.HadLeisureToday && ShouldWorkerSeekLeisure(driver) && TryStartWorkerLifeGoal(driver, WorkerLifeGoal.Leisure, startPosition))
        {
            return true;
        }

        if (!driver.SleptToday && ShouldWorkerSeekSleep(driver) && TryStartWorkerLifeGoal(driver, WorkerLifeGoal.Sleep, startPosition))
        {
            return true;
        }

        driver.LifeGoal = WorkerLifeGoal.Idle;
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} has no due life goals; entering Idle. helperFlags work={driver.WorkedToday}, eat={driver.AteToday}, leisure={driver.HadLeisureToday}, sleep={driver.SleptToday}; needs={FormatWorkerNeedsDebug(driver)}.");
        return false;
    }

    private bool TryStartWorkerLifeGoal(DriverAgent driver, WorkerLifeGoal goal, Vector3 startPosition)
    {
        switch (goal)
        {
            case WorkerLifeGoal.Eat:
                if (TryStartWorkerServiceVisit(driver, LocationType.Canteen, WorkerLifeGoal.Eat, DriverRescuePhase.IdleWalkToCanteen, WorkerCanteenDuration, startPosition))
                {
                    return true;
                }
                driver.AteToday = true;
                SessionDebugLogger.Log("LIFE", $"{driver.DriverName} skipped Canteen today; reason={GetWorkerServiceUnavailableReason(driver, LocationType.Canteen)}; need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Meal)}; snapshot={FormatWorkerNeedsDebug(driver)}.");
                return ContinueWorkerLifeCycle(driver, startPosition);

            case WorkerLifeGoal.Leisure:
                if (driver.Money >= WorkerGamblingMinBalance &&
                    TryStartWorkerServiceVisit(driver, LocationType.GamblingHall, WorkerLifeGoal.Leisure, DriverRescuePhase.IdleWalkToGamblingHall, WorkerGamblingHallDuration, startPosition))
                    return true;
                if (TryStartWorkerServiceVisit(driver, LocationType.Bar, WorkerLifeGoal.Leisure, DriverRescuePhase.IdleWalkToBar, WorkerLeisureDuration, startPosition))
                    return true;
                StartWorkerFreeIdle(driver, startPosition, "leisure fallback");
                return true;

            case WorkerLifeGoal.Sleep:
                if (TryStartWorkerSleep(driver, startPosition))
                {
                    return true;
                }
                driver.SleptToday = true;
                SessionDebugLogger.Log("LIFE", $"{driver.DriverName} skipped Motel sleep today; reason={GetWorkerServiceUnavailableReason(driver, LocationType.Motel)}; need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Sleep)}; snapshot={FormatWorkerNeedsDebug(driver)}.");
                return false;
        }

        return false;
    }

    private bool TryStartWorkerServiceVisit(DriverAgent driver, LocationType type, WorkerLifeGoal goal, DriverRescuePhase walkPhase, float duration, Vector3 startPosition)
    {
        if (driver == null || !locations.TryGetValue(type, out LocationData service))
        {
            return false;
        }

        bool hasResource = type switch
        {
            LocationType.Canteen => service.FoodStored > 0,
            LocationType.Bar     => service.AlcoholStored > 0,
            _                    => true
        };
        if (!hasResource || driver.Money < service.ServiceFee)
        {
            return false;
        }

        float x = (service.Min.x + service.Max.x + 1) * 0.5f;
        float z = (service.Min.y + service.Max.y + 1) * 0.5f;
        Vector3 target = new(x + Random.Range(-0.2f, 0.2f), 0f, z + Random.Range(-0.2f, 0.2f));
        driver.LifeGoal = goal;
        driver.IdleActivityTimer = duration;
        driver.WalkTargetWorld = target;
        driver.WalkPhase = walkPhase;
        driver.WalkAnimationTime = 0f;
        BuildDriverWalkPath(driver, startPosition, target);
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} heading to {type} for {goal}; serviceFee=${service.ServiceFee}, need={FormatWorkerNeedDebug(driver, goal == WorkerLifeGoal.Eat ? WorkerNeedKind.Meal : WorkerNeedKind.Leisure)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
        return true;
    }

    private bool TryStartWorkerSleep(DriverAgent driver, Vector3 startPosition)
    {
        if (driver == null || !locations.TryGetValue(LocationType.Motel, out LocationData motel) || driver.Money < motel.ServiceFee)
        {
            return false;
        }

        driver.LifeGoal = WorkerLifeGoal.Sleep;
        driver.WalkTargetWorld = GetDriverStandPointNearLocation(LocationType.Motel);
        driver.WalkPhase = DriverRescuePhase.ToMotelEntrance;
        driver.RestPhase = DriverRestPhase.DriverWalkToMotel;
        driver.WalkAnimationTime = 0f;
        BuildDriverWalkPath(driver, startPosition, driver.WalkTargetWorld);
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} heading to Motel for SLEEP; serviceFee=${motel.ServiceFee}, need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Sleep)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
        return true;
    }

    private void StartWorkerFreeIdle(DriverAgent driver, Vector3 startPosition, string reason)
    {
        if (driver == null) return;

        driver.LifeGoal = WorkerLifeGoal.Leisure;
        driver.HadLeisureToday = false;
        driver.IdleActivityTimer = Random.Range(WorkerFreeIdleMinDuration, WorkerFreeIdleMaxDuration);
        driver.WalkPhase = DriverRescuePhase.IdlePhoneCall;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} started free idle ({reason}) for {driver.IdleActivityTimer:0.0}s; need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Leisure)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
    }

    private void UpdateDriverIdleWander(DriverAgent driver)
    {
        if (driver == null || driver.DriverObject == null)
        {
            return;
        }

        UpdateWorkerLifeCycleDailyState(driver);

        if (driver.IsArrivingByBus ||
            driver.RestPhase != DriverRestPhase.None ||
            driver.IsOnActiveShift ||
            driver.WaitingForShiftAtParking ||
            GetCurrentTruckForDriver(driver) != null)
        {
            if (IsDriverIdleWanderPhase(driver) || IsDriverInIdleActivity(driver))
            {
                ReleaseBench(driver);
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
            }

            driver.IdleConversationTimer = 0f;
            driver.IdleConversationPartnerId = -1;

            return;
        }

        if (driver.IdleConversationCooldownTimer > 0f)
        {
            driver.IdleConversationCooldownTimer = Mathf.Max(0f, driver.IdleConversationCooldownTimer - Time.deltaTime * gameSpeedMultiplier);
        }

        if (IsDriverBusyWalkPhase(driver))
        {
            return;
        }

        if (IsDriverIdleConversing(driver))
        {
            DriverAgent partner = GetDriverAgentById(driver.IdleConversationPartnerId);
            if (!CanDriverContinueIdleConversation(driver, partner))
            {
                StopDriverIdleConversation(driver, true);
                return;
            }

            driver.IdleConversationTimer -= Time.deltaTime * gameSpeedMultiplier;
            if (driver.IdleConversationTimer <= 0f)
            {
                StopDriverIdleConversation(driver, true);
            }
            return;
        }

        if (IsDriverIdleWanderPhase(driver))
        {
            return;
        }

        // Handle stationary idle activities
        if (driver.WalkPhase == DriverRescuePhase.IdleSittingOnBench ||
            driver.WalkPhase == DriverRescuePhase.IdleAtBar ||
            driver.WalkPhase == DriverRescuePhase.IdleAtCanteen ||
            driver.WalkPhase == DriverRescuePhase.IdleAtGamblingHall ||
            driver.WalkPhase == DriverRescuePhase.IdleSmoking ||
            driver.WalkPhase == DriverRescuePhase.IdlePhoneCall)
        {
            driver.IdleActivityTimer -= Time.deltaTime * gameSpeedMultiplier;
            if (driver.IdleActivityTimer <= 0f)
            {
                WorkerLifeGoal completedGoal = driver.LifeGoal;
                if (driver.WalkPhase == DriverRescuePhase.IdleSittingOnBench)
                {
                    ReleaseBench(driver);
                }

                driver.WalkPhase = DriverRescuePhase.None;
                if (completedGoal == WorkerLifeGoal.Eat)
                {
                    ResetWorkerNeedTimer(driver, WorkerNeedKind.Meal);
                    driver.AteToday = true;
                    driver.LifeGoal = WorkerLifeGoal.None;
                    ContinueWorkerLifeCycle(driver, driver.DriverObject.transform.position);
                    return;
                }

                if (completedGoal == WorkerLifeGoal.Leisure)
                {
                    ResetWorkerNeedTimer(driver, WorkerNeedKind.Leisure);
                    driver.HadLeisureToday = true;
                    // Fallback: apply gambling money if animation never completed (HUD was closed)
                    if (driver.GamblingMoneyPending)
                    {
                        driver.GamblingMoneyPending = false;
                        int net = driver.GamblingPayout - driver.GamblingBet;
                        driver.Money = Mathf.Max(0, driver.Money + net);
                        SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} gambling fallback applied: net={net:+#;-#;0}, balance=${driver.Money}.");
                    }
                    driver.GamblingBet = 0;
                    driver.GamblingPayout = 0;
                    driver.GamblingMultiplier = 0;
                    driver.LifeGoal = WorkerLifeGoal.None;
                    ContinueWorkerLifeCycle(driver, driver.DriverObject.transform.position);
                    return;
                }

                driver.IdleWanderPauseTimer = Random.Range(0.5f, 1.5f);
            }

            return;
        }

        if (driver.DutyMode == DriverDutyMode.Logistics &&
            (ShouldLogisticsWorkerHeadToBuilding(driver) || IsProductionWorkHour(GetCurrentHour())))
        {
            return;
        }

        if (driver.ShiftStartHour >= 0 &&
            (ShouldDriverHeadToShift(driver) || IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour)))
        {
            return;
        }

        if (TryStartDueWorkerLifeCycle(driver))
        {
            return;
        }

        if (driver.IdleWanderPauseTimer > 0f)
        {
            driver.IdleWanderPauseTimer -= Time.deltaTime * gameSpeedMultiplier;
            return;
        }

        if (TryStartIdleConversation(driver))
        {
            return;
        }

        Vector3 startPosition = driver.DriverObject.transform.position;
        Vector3 targetPosition = FindDriverIdleWanderTarget(driver, startPosition);
        if ((targetPosition - startPosition).sqrMagnitude < 0.04f)
        {
            driver.IdleWanderPointIndex++;
            driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
            return;
        }

        SelectNextIdleActivity(driver, startPosition, targetPosition);
    }

    private void SelectNextIdleActivity(DriverAgent driver, Vector3 startPosition, Vector3 wanderTarget)
    {
        float roll = Random.value;

        if (roll < 0.45f)
        {
            driver.WalkPhase = DriverRescuePhase.IdleWander;
            driver.IdleWanderPointIndex++;
            driver.WalkAnimationTime = 0f;
            BuildDriverWalkPath(driver, startPosition, wanderTarget);
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} started motel idle walk.");
        }
        else if (roll < 0.75f && TryGetNearestFreeBench(startPosition, 14f, out int bIdx, out Vector3 bPos))
        {
            if (bIdx < benchOccupied.Length) benchOccupied[bIdx] = true;
            driver.SittingBenchIndex = bIdx;
            driver.IdleActivityTimer = Random.Range(15f, 45f);
            driver.WalkTargetWorld = bPos;
            BuildDriverWalkPath(driver, startPosition, bPos);
            driver.WalkPhase = DriverRescuePhase.IdleWalkToBench;
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} heading to bench {bIdx}.");
        }
        else if (roll < 0.88f)
        {
            driver.IdleActivityTimer = Random.Range(20f, 45f);
            driver.WalkPhase = DriverRescuePhase.IdleSmoking;
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} started smoking break.");
        }
        else
        {
            driver.IdleActivityTimer = Random.Range(8f, 25f);
            driver.WalkPhase = DriverRescuePhase.IdlePhoneCall;
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} making a phone call.");
        }
    }

    private Vector3 FindDriverIdleWanderTarget(DriverAgent driver, Vector3 startPosition)
    {
        int nextPointIndex = driver.IdleWanderPointIndex + 1;
        float personalSpaceSqr = DriverIdlePersonalSpace * DriverIdlePersonalSpace;
        for (int attempt = 0; attempt < 16; attempt++)
        {
            Vector3 candidate = GetDriverIdleMotelWanderPosition(driver.DriverId - 1, nextPointIndex + attempt);
            Vector3 flatDelta = candidate - startPosition;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude < 0.04f)
            {
                continue;
            }

            bool blockedByOtherDriver = false;
            for (int i = 0; i < driverAgents.Count; i++)
            {
                DriverAgent other = driverAgents[i];
                if (other == null || other == driver || other.DriverObject == null || !other.DriverObject.activeSelf)
                {
                    continue;
                }

                Vector3 otherDelta = other.DriverObject.transform.position - candidate;
                otherDelta.y = 0f;
                if (otherDelta.sqrMagnitude < personalSpaceSqr)
                {
                    blockedByOtherDriver = true;
                    break;
                }
            }

            if (!blockedByOtherDriver)
            {
                return candidate;
            }
        }

        return startPosition;
    }

    private void UpdateHiringDriverArrival()
    {
        if (hiringDriverArrival == null)
        {
            return;
        }

        DriverAgent driver = hiringDriverArrival.Driver;
        if (driver == null)
        {
            CleanupHiringDriverArrival(false);
            return;
        }

        float dt = Time.deltaTime * gameSpeedMultiplier;
        if (dt <= 0f)
        {
            return;
        }

        switch (hiringDriverArrival.Phase)
        {
            case HiringDriverArrivalPhase.WaitingLaneClear:
                if (!HasActiveCitySideAmbientBus())
                {
                    CreateHiringArrivalBusVisual();
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.ApproachingStop;
                    SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} arrival bus entered the edge highway.");
                }
                break;

            case HiringDriverArrivalPhase.ApproachingStop:
                if (hiringDriverArrival.BusRootTransform == null)
                {
                    CleanupHiringDriverArrival(false);
                    return;
                }

                hiringDriverArrival.BusWorldX += hiringDriverArrival.BusSpeed * dt;
                float stopX = GetHiringBusStopWorldX();
                if (hiringDriverArrival.BusWorldX >= stopX)
                {
                    hiringDriverArrival.BusWorldX = stopX;
                    hiringDriverArrival.StopTimer = HiringBusStopDuration;
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.StoppedForDropoff;
                    SpawnDriverFromHiringBus();
                    SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} arrival bus stopped at Bus Stop.");
                }

                UpdateHiringBusTransform();
                break;

            case HiringDriverArrivalPhase.StoppedForDropoff:
                UpdateHiringBusTransform();
                hiringDriverArrival.StopTimer -= dt;
                if (hiringDriverArrival.StopTimer <= 0f)
                {
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.Departing;
                    SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} finished disembarking; arrival bus departing immediately.");
                }
                break;

            case HiringDriverArrivalPhase.DriverWalkingToMotel:
                UpdateHiringBusTransform();
                if (!driver.IsArrivingByBus)
                {
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.Departing;
                    SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} reached Motel; arrival bus departing.");
                }
                break;

            case HiringDriverArrivalPhase.Departing:
                if (hiringDriverArrival.BusRootTransform == null)
                {
                    CleanupHiringDriverArrival(false);
                    return;
                }

                hiringDriverArrival.BusWorldX += hiringDriverArrival.BusSpeed * dt;
                UpdateHiringBusTransform();
                if (hiringDriverArrival.BusWorldX >= GridWidth)
                {
                    CleanupHiringDriverArrival(true);
                }
                break;
        }
    }

    private bool HasActiveCitySideAmbientBus()
    {
        for (int i = 0; i < edgeHighwayBuses.Count; i++)
        {
            EdgeHighwayBusData bus = edgeHighwayBuses[i];
            if (bus != null && bus.RootTransform != null && bus.IsCitySideLane)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsDriverMotelArrivalInProgress()
    {
        return hiringDriverArrival != null;
    }

    private float GetHiringBusStopWorldX()
    {
        if (!locations.TryGetValue(LocationType.BusStop, out _))
        {
            return GridWidth * 0.5f;
        }

        Vector3 busStopCenter = GetLocationCenter(LocationType.BusStop);
        return Mathf.Clamp(busStopCenter.x + 0.4f, 1.2f, GridWidth - 1.2f);
    }

    private Vector3 GetHiringBusDropoffWorld()
    {
        if (!locations.TryGetValue(LocationType.BusStop, out _))
        {
            Vector3 fallback = new(GridWidth * 0.5f, 0f, 3.2f);
            fallback.y = SampleTerrainHeight(fallback.x, fallback.z);
            return fallback;
        }

        Vector3 center = GetLocationCenter(LocationType.BusStop);
        Vector3 dropoff = new(GetHiringBusStopWorldX() - 0.22f, 0f, center.z + 0.48f);
        dropoff.y = SampleTerrainHeight(dropoff.x, dropoff.z);
        return dropoff;
    }

    private void SpawnDriverFromHiringBus()
    {
        if (hiringDriverArrival == null || hiringDriverArrival.Driver == null)
        {
            return;
        }

        DriverAgent driver = hiringDriverArrival.Driver;
        if (driver.DriverObject == null)
        {
            return;
        }

        Vector3 dropoff = GetHiringBusDropoffWorld();
        driver.DriverObject.SetActive(true);
        driver.DriverObject.transform.position = dropoff;
        driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        driver.WalkAnimationTime = 0f;
        driver.WalkPhase = DriverRescuePhase.ToMotelFromBusStop;
        driver.WalkTargetWorld = driver.MotelIdlePosition;
        driver.IdleWanderPauseTimer = 0f;
        driver.IdleWanderPointIndex = -1;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        ApplyDriverPose(driver, 0f, 0f);
        BuildDriverWalkPath(driver, dropoff, driver.MotelIdlePosition);
        SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} exited the arrival bus and started walking to Motel.");
    }

    private void CleanupHiringDriverArrival(bool destroyBus)
    {
        if (hiringDriverArrival == null)
        {
            return;
        }

        if (destroyBus && hiringDriverArrival.BusRootTransform != null)
        {
            Destroy(hiringDriverArrival.BusRootTransform.gameObject);
        }

        hiringDriverArrival = null;
    }

    private DriverAgent GetDriverAgentById(int driverId)
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            if (driverAgents[i].DriverId == driverId)
            {
                return driverAgents[i];
            }
        }

        return null;
    }

    private bool CanDriverStartIdleConversation(DriverAgent driver)
    {
        return driver != null &&
               driver.DriverObject != null &&
               driver.DriverObject.activeSelf &&
               !driver.IsArrivingByBus &&
               driver.ShiftStartHour < 0 &&
               !driver.IsOnActiveShift &&
               !driver.WaitingForShiftAtParking &&
               driver.RestPhase == DriverRestPhase.None &&
               driver.WalkPhase == DriverRescuePhase.None &&
               driver.IdleConversationTimer <= 0f &&
               driver.IdleConversationCooldownTimer <= 0f &&
               GetCurrentTruckForDriver(driver) == null;
    }

    private bool TryStartIdleConversation(DriverAgent driver)
    {
        if (!CanDriverStartIdleConversation(driver))
        {
            return false;
        }

        DriverAgent bestPartner = null;
        float bestDistanceSqr = DriverIdleConversationDistance * DriverIdleConversationDistance;
        Vector3 driverPosition = driver.DriverObject.transform.position;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent candidate = driverAgents[i];
            if (candidate == driver || !CanDriverStartIdleConversation(candidate))
            {
                continue;
            }

            Vector3 delta = candidate.DriverObject.transform.position - driverPosition;
            delta.y = 0f;
            float sqrDistance = delta.sqrMagnitude;
            if (sqrDistance < 0.12f || sqrDistance > bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = sqrDistance;
            bestPartner = candidate;
        }

        if (bestPartner == null)
        {
            return false;
        }

        if (Random.value > DriverIdleConversationStartChance)
        {
            driver.IdleWanderPauseTimer = Random.Range(0.8f, 1.6f);
            return false;
        }

        float duration = Random.Range(DriverIdleConversationDurationMin, DriverIdleConversationDurationMax);
        driver.IdleConversationTimer = duration;
        driver.IdleConversationPartnerId = bestPartner.DriverId;
        bestPartner.IdleConversationTimer = duration;
        bestPartner.IdleConversationPartnerId = driver.DriverId;
        driver.IdleWanderPauseTimer = 0f;
        bestPartner.IdleWanderPauseTimer = 0f;
        driver.WalkAnimationTime = 0f;
        bestPartner.WalkAnimationTime = 0f;
        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} and {bestPartner.DriverName} started an idle conversation.");
        return true;
    }

    private bool CanDriverContinueIdleConversation(DriverAgent driver, DriverAgent partner)
    {
        if (!CanDriverStartIdleConversation(driver) || partner == null || !CanDriverStartIdleConversation(partner))
        {
            return false;
        }

        if (partner.IdleConversationPartnerId != driver.DriverId)
        {
            return false;
        }

        Vector3 delta = partner.DriverObject.transform.position - driver.DriverObject.transform.position;
        delta.y = 0f;
        float sqrDistance = delta.sqrMagnitude;
        return sqrDistance >= 0.12f &&
               sqrDistance <= DriverIdleConversationDistance * DriverIdleConversationDistance * 1.2f;
    }

    private void StopDriverIdleConversation(DriverAgent driver, bool addPause)
    {
        if (driver == null)
        {
            return;
        }

        int partnerId = driver.IdleConversationPartnerId;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        driver.IdleConversationCooldownTimer = Random.Range(DriverIdleConversationCooldownMin, DriverIdleConversationCooldownMax);
        driver.IdleWanderPointIndex++;
        if (addPause)
        {
            driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
        }

        DriverAgent partner = GetDriverAgentById(partnerId);
        if (partner != null && partner.IdleConversationPartnerId == driver.DriverId)
        {
            partner.IdleConversationTimer = 0f;
            partner.IdleConversationPartnerId = -1;
            partner.IdleConversationCooldownTimer = Random.Range(DriverIdleConversationCooldownMin, DriverIdleConversationCooldownMax);
            partner.IdleWanderPointIndex += 2;
            if (addPause)
            {
                partner.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
            }
        }
    }

    private void UpdateDriverShiftActivation(DriverAgent driver)
    {
        if (driver == null) return;
        if (IsDriverIntercity(driver)) return;
        if (driver.DutyMode == DriverDutyMode.Logistics) return;
        if (driver.IsArrivingByBus) return;
        if (driver.ShiftStartHour < 0) return;
        if (driver.IsOnActiveShift) return;
        if (driver.RestPhase != DriverRestPhase.None) return;
        if (!IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour)) return;

        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (assignedTruck == null)
        {
            return;
        }

        if (assignedTruck.Driver != driver && !TryBoardDriverToAssignedTruck(driver))
        {
            return;
        }

        driver.IsOnActiveShift = true;
        driver.IsShiftSalaryPending = false;
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} shift started ({GetShiftRangeLabel(driver.ShiftStartHour)}).");
        SetTruckAutoMode(assignedTruck, true);
    }

    private void UpdateDriverShiftEnd(TruckAgent truckAgent, DriverAgent driver)
    {
        if (truckAgent == null || driver == null || IsDriverIntercity(driver) || !driver.IsOnActiveShift || driver.ShiftStartHour < 0)
        {
            return;
        }

        if (IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour))
        {
            return;
        }

        if (driver.NeedsShiftEndReturn)
        {
            if (truckCell == locations[LocationType.Parking].Anchor &&
                !isTruckMoving &&
                !isTruckInteracting &&
                !isDriverRescueActive &&
                currentAssignedTrip == TripType.None &&
                currentRefuelPhase == RefuelPhase.None)
            {
                PayDriverSalary(driver);
                StartDriverMotelRest(truckAgent, driver);
            }
            return;
        }

        driver.IsOnActiveShift = false;
        truckAgent.IsTruckAutoModeEnabled = false;
        driver.NeedsShiftEndReturn = true;
        driver.IsShiftSalaryPending = true;
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} shift ended. {truckAgent.DisplayName} returning to Parking for handoff.");

        if (currentAssignedTrip == TripType.None &&
            currentRefuelPhase == RefuelPhase.None &&
            !isTruckMoving &&
            !isTruckInteracting &&
            !isDriverRescueActive)
        {
            if (truckCell != locations[LocationType.Parking].Anchor)
            {
                StartMoveTo(locations[LocationType.Parking].Anchor);
            }
            else
            {
                PayDriverSalary(driver);
                StartDriverMotelRest(truckAgent, driver);
            }
        }
    }

    private void PayDriverSalary(DriverAgent driver)
    {
        if (driver == null || driver.Salary <= 0 || !driver.IsShiftSalaryPending) return;
        int treasuryBefore = money;
        driver.Money += driver.Salary;
        money = Mathf.Max(0, money - driver.Salary);
        int actualTreasuryDelta = money - treasuryBefore;
        driver.IsShiftSalaryPending = false;
        RecordMoneyMovement(
            actualTreasuryDelta,
            "Treasury",
            driver.DriverName,
            driver.DutyMode == DriverDutyMode.Logistics
                ? $"Salary payout ({GetProductionWorkRangeLabel()})"
                : $"Salary payout ({GetShiftRangeLabel(driver.ShiftStartHour)})",
            money,
            driver.Money);
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        SessionDebugLogger.Log("PAY", $"{driver.DriverName} paid ${driver.Salary}. Personal balance: ${driver.Money}. Treasury: ${money}.");
    }

    private void EnsurePendingShiftSalaryPaid(DriverAgent driver)
    {
        if (driver == null) return;
        // Driver may leave before shift end time for the life-cycle flow; keep salary payout pending.
        if (driver.IsOnActiveShift && !driver.IsShiftSalaryPending)
        {
            driver.IsShiftSalaryPending = true;
        }
        PayDriverSalary(driver);
    }

    private void RecordMoneyMovement(int treasuryDelta, string fromLabel, string toLabel, string reason, int? treasuryAfter = null, int? recipientBalanceAfter = null)
    {
        MoneyLedgerEntry entry = new()
        {
            TimeLabel = GetDayNightClockLabel(),
            TreasuryDelta = treasuryDelta,
            FromLabel = fromLabel,
            ToLabel = toLabel,
            Reason = reason,
            TreasuryAfter = treasuryAfter,
            RecipientBalanceAfter = recipientBalanceAfter
        };

        moneyLedgerEntries.Insert(0, entry);
        if (moneyLedgerEntries.Count > MaxMoneyLedgerEntries)
        {
            moneyLedgerEntries.RemoveAt(moneyLedgerEntries.Count - 1);
        }

        isEconomyScreenDirty = true;
    }

    private void UpdateIdleRecall(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        if (IsDriverIntercity(driver) || IsDriverOnActiveTradeRun(driver))
        {
            return;
        }

        // Driver has no shift assigned — ensure truck returns to parking
        if (driver.DutyMode != DriverDutyMode.Local) return;
        if (driver.ShiftStartHour >= 0) return;
        if (driver.RestPhase != DriverRestPhase.None) return;
        if (isDriverRescueActive) return;
        if (truckCell == locations[LocationType.Parking].Anchor)
        {
            if (GetCurrentTruckForDriver(driver) is TruckAgent currentTruck)
            {
                StartDriverMotelRest(currentTruck, driver);
            }
            return;
        }
        if (isTruckMoving) return;

        // Cancel any active orders then head home
        currentAssignedTrip = TripType.None;
        currentTripPhase = TripPhase.None;
        currentRefuelPhase = RefuelPhase.None;
        isTruckAutoModeEnabled = false;
        currentAssignedTripReward = 0;
        StartMoveTo(locations[LocationType.Parking].Anchor);
        SessionDebugLogger.Log("IDLE", $"{GetLoadedTruckDisplayName()} returning to parking — driver is idle.");
    }

}

