using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void UpdateDriverWalk(DriverAgent driver)
    {
        if (driver != null && driver.IsDrivingPersonalCar)
        {
            return;
        }

        if (driver == null || driver.WalkPhase == DriverRescuePhase.None || driver.DriverObject == null)
        {
            return;
        }

        if (driver.WalkPhase == DriverRescuePhase.WaitingAtLocalBusStop)
        {
            if (!IsLocalBusServiceAvailableForPassengers())
            {
                ResumeWorkerLocalBusTripOnFoot(driver, "local bus service is not available for the current shift");
            }

            return;
        }

        if (driver.WalkPhase == DriverRescuePhase.RidingLocalBus)
        {
            return;
        }

        if (TryRescueDriverFromUnsafeWalkCell(driver, "walk update"))
        {
            return;
        }

        Vector3 currentPosition = driver.DriverObject.transform.position;
        UpdateDriverLastSafeWalkPosition(driver, currentPosition);
        RecordDriverFootpathWear(driver, currentPosition);
        Vector3 targetPosition = driver.WalkTargetWorld;
        if (driver.WalkPath.Count > 0)
        {
            targetPosition = driver.WalkPath[Mathf.Clamp(driver.WalkWaypointIndex, 0, driver.WalkPath.Count - 1)];
        }

        Vector3 flatDirection = targetPosition - currentPosition;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude > 0.0001f)
        {
            float walkSpeed = driver.WalkPhase == DriverRescuePhase.IdleWander ? DriverIdleWanderSpeed : DriverWalkSpeed;
            Vector3 step = flatDirection.normalized * (walkSpeed * Time.deltaTime);
            Vector3 proposedPosition = step.sqrMagnitude >= flatDirection.sqrMagnitude
                ? targetPosition
                : currentPosition + step;

            if (driver.WalkPhase == DriverRescuePhase.IdleWander && WouldIdleDriverOverlapAtPosition(driver, proposedPosition))
            {
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.IdleWanderPauseTimer = Random.Range(0.8f, 1.8f);
                SessionDebugLogger.LogVerbose("IDLE", $"{driver.DriverName} paused idle walk to avoid overlapping another driver.");
                return;
            }

            if (step.sqrMagnitude >= flatDirection.sqrMagnitude)
            {
                currentPosition = targetPosition;
            }
            else
            {
                currentPosition += step;
            }

            driver.DriverObject.transform.position = currentPosition;
            UpdateDriverLastSafeWalkPosition(driver, currentPosition);
            RecordDriverFootpathWear(driver, currentPosition);
            driver.DriverObject.transform.rotation = Quaternion.Slerp(
                driver.DriverObject.transform.rotation,
                Quaternion.LookRotation(flatDirection.normalized, Vector3.up),
                10f * Time.deltaTime);
        }

        Vector3 flatDelta = driver.DriverObject.transform.position - targetPosition;
        flatDelta.y = 0f;
        if (flatDelta.sqrMagnitude > 0.001f)
        {
            return;
        }

        if (driver.WalkPath.Count == 0 && RequiresDriverWalkPathForArrival(driver.WalkPhase) && !driver.CompletedPersonalCarTrip)
        {
            DriverRescuePhase blockedPhase = driver.WalkPhase;
            driver.WalkPhase = DriverRescuePhase.None;
            driver.WalkWaypointIndex = 0;
            driver.WalkAnimationTime = 0f;
            SessionDebugLogger.Log(
                "DRIVER",
                $"{driver.DriverName} halted {blockedPhase}: no valid walk path was available, so the phase will not auto-complete at the start position.");
            return;
        }

        driver.CompletedPersonalCarTrip = false;

        if (driver.WalkPath.Count > 0 && driver.WalkWaypointIndex < driver.WalkPath.Count - 1)
        {
            driver.WalkWaypointIndex++;
            if (SessionDebugLogger.IsVerboseEnabled("DRIVER_TRACE"))
            {
                SessionDebugLogger.LogVerbose(
                    "DRIVER_TRACE",
                    $"{driver.DriverName} advanced to waypoint {driver.WalkWaypointIndex + 1}/{driver.WalkPath.Count} during {driver.WalkPhase}.");
            }
            return;
        }

        switch (driver.WalkPhase)
        {
            case DriverRescuePhase.ToGasStation:
                driver.WalkPhase = DriverRescuePhase.ToTruck;
                if (driver.DriverFuelCanTransform != null)
                    driver.DriverFuelCanTransform.gameObject.SetActive(true);
                if (locations.TryGetValue(LocationType.GasStation, out LocationData gasStation))
                {
                    RecordWorkerBuildingKnowledge(driver, gasStation, "\u0421\u0445\u043e\u0434\u0438\u043b \u0437\u0430 \u0442\u043e\u043f\u043b\u0438\u0432\u043e\u043c", "Picked up fuel");
                }
                SessionDebugLogger.Log("FUEL", $"{GetLoadedTruckDisplayName()} driver reached Gas Station and is returning with fuel.");
                driver.WalkTargetWorld = GetDriverStandPointNearTruck();
                BuildDriverWalkPath(driver, currentPosition, driver.WalkTargetWorld);
                SessionDebugLogger.Log(
                    "DRIVER",
                    $"{driver.DriverName} started return walk from Gas Station to {GetLoadedTruckDisplayName()} at truck cell ({truckCell.x},{truckCell.y}).");
                return;

            case DriverRescuePhase.ToTruck:
                truckFuel = TruckFuelCapacity;
                isDriverRescueActive = false;
                driver.WalkPhase = DriverRescuePhase.None;
                if (driver.DriverFuelCanTransform != null)
                {
                    driver.DriverFuelCanTransform.gameObject.SetActive(false);
                }

                driver.DriverObject.SetActive(false);
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                SessionDebugLogger.Log(
                    "DRIVER",
                    $"{driver.DriverName} reached {GetLoadedTruckDisplayName()} and restored fuel to {Mathf.CeilToInt(truckFuel)}/{Mathf.CeilToInt(TruckFuelCapacity)}.");
                SessionDebugLogger.Log("FUEL", $"{GetLoadedTruckDisplayName()} rescue completed. Fuel restored to {Mathf.CeilToInt(truckFuel)}/{Mathf.CeilToInt(TruckFuelCapacity)}.");
                if (activePath.Count > 0)
                {
                    isTruckMoving = true;
                    BeginNextTruckSegment(activePath[0]);
                }

                return;

            case DriverRescuePhase.ToPersonalHouseForPurchase:
                isDriverRescueActive = false;
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                bool boughtHouse = false;
                int boughtHouseIndex = -1;
                {
                    int hIdx = driver.AssignedPersonalHouseIndex;
                    if (hIdx >= 0 && hIdx < personalHouses.Count && driver.Money >= HousePurchasePrice)
                    {
                        LocationData house = personalHouses[hIdx];
                        int mb = driver.Money, bb = house.BuildingBank;
                        driver.Money -= HousePurchasePrice;
                        house.BuildingBank += HousePurchasePrice;
                        SpawnMoneySpendPopup(driver.DriverObject.transform.position, HousePurchasePrice);
                        LogBuildingBankTransaction(house, driver, HousePurchasePrice, "Personal house purchase", mb, bb);
                        RecordWorkerBuildingKnowledge(driver, house, "\u041a\u0443\u043f\u0438\u043b \u044d\u0442\u043e\u0442 \u0434\u043e\u043c", "Bought this house");
                        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} bought house #{hIdx} for ${HousePurchasePrice} (balance: ${driver.Money}).");
                        RecordWorkerThought(
                            driver,
                            WorkerThoughtKind.Family,
                            WorkerThoughtTone.Positive,
                            80,
                            "house_bought",
                            new[]
                            {
                                ThoughtBuilding("home", LocationType.PersonalHouse)
                            },
                            WorkerThoughtSubjectType.BuildingType,
                            0,
                            LocationType.PersonalHouse.ToString(),
                            GetSelectedLocationDisplayName(LocationType.PersonalHouse),
                            10,
                            $"house_bought|{hIdx}",
                            72f);
                        boughtHouse = true;
                        boughtHouseIndex = hIdx;
                    }
                    else
                    {
                        driver.AssignedPersonalHouseIndex = -1;
                        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} couldn't complete house purchase (money=${driver.Money}) - reservation released.");
                    }
                }
                driver.LifeGoal = WorkerLifeGoal.Idle;
                isDriversScreenDirty = true;
                if (boughtHouse)
                {
                    OnWorkerMovedIntoPersonalHouse(driver, boughtHouseIndex);
                    ContinueWorkerLifeCycle(driver, currentPosition);
                }
                return;

            case DriverRescuePhase.ToCarMarketForPurchase:
                isDriverRescueActive = false;
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                if (locations.TryGetValue(LocationType.CarMarket, out LocationData market) && driver.Money >= CarPurchasePrice)
                {
                    int moneyBefore = driver.Money;
                    int bankBefore = market.BuildingBank;
                    driver.OwnedCarModelIndex = Random.Range(0, CarModelNames.Length);
                    driver.Money -= CarPurchasePrice;
                    market.BuildingBank += CarPurchasePrice;
                    ParkNewlyPurchasedWorkerCarAtMarket(driver);
                    SpawnMoneySpendPopup(driver.DriverObject.transform.position, CarPurchasePrice);
                    LogBuildingBankTransaction(market, driver, CarPurchasePrice, "Car purchase", moneyBefore, bankBefore);
                    RecordWorkerBuildingKnowledge(driver, market, "\u041a\u0443\u043f\u0438\u043b \u043c\u0430\u0448\u0438\u043d\u0443 \u043d\u0430 \u0430\u0432\u0442\u043e\u0440\u044b\u043d\u043a\u0435", "Bought a car at the market");
                    SessionDebugLogger.Log("LIFE", $"{driver.DriverName} bought {CarModelNames[driver.OwnedCarModelIndex]} for ${CarPurchasePrice} (balance: ${driver.Money}).");
                    if (driver.AssignedPersonalHouseIndex >= 0 && driver.AssignedPersonalHouseIndex < personalHouses.Count)
                    {
                        Vector3 homeTarget = GetDriverStandPointNearPersonalHouse(driver.AssignedPersonalHouseIndex);
                        if (TryStartWorkerPersonalCarTrip(driver, currentPosition, homeTarget, DriverRescuePhase.ToPersonalHouseParking, "drive new car home"))
                        {
                            isDriversScreenDirty = true;
                            return;
                        }
                    }
                }
                else
                {
                    SessionDebugLogger.Log("LIFE", $"{driver.DriverName} couldn't complete car purchase (money=${driver.Money}).");
                }

                driver.LifeGoal = WorkerLifeGoal.Idle;
                isDriversScreenDirty = true;
                return;

            case DriverRescuePhase.ToPersonalHouseEntrance:
                isDriverRescueActive = false;
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.DriverObject.SetActive(false);
                driver.SleepTimer = DriverSleepDuration;
                driver.RestPhase = DriverRestPhase.SleepingAtHome;
                if (driver.AssignedPersonalHouseIndex >= 0 && driver.AssignedPersonalHouseIndex < personalHouses.Count)
                {
                    RecordWorkerBuildingKnowledge(driver, personalHouses[driver.AssignedPersonalHouseIndex], "\u0417\u0430\u0448\u0451\u043b \u0434\u043e\u043c\u043e\u0439 \u0441\u043f\u0430\u0442\u044c", "Went home to sleep");
                }
                RecordWorkerThought(
                    driver,
                    WorkerThoughtKind.Need,
                    WorkerThoughtTone.Positive,
                    48,
                    "home_sleep_good",
                    new[]
                    {
                        ThoughtBuilding("home", LocationType.PersonalHouse)
                    },
                    WorkerThoughtSubjectType.BuildingType,
                    0,
                    LocationType.PersonalHouse.ToString(),
                    GetSelectedLocationDisplayName(LocationType.PersonalHouse),
                    3,
                    "home_sleep_good",
                    8f);
                SessionDebugLogger.Log("REST", $"{driver.DriverName} sleeping at home (house #{driver.AssignedPersonalHouseIndex}).");
                return;

            case DriverRescuePhase.ToPersonalHouseMeal:
                isDriverRescueActive = false;
                driver.WalkPhase = DriverRescuePhase.IdleAtPersonalHouseMeal;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.DriverObject.SetActive(false);
                driver.IsInsideBuilding = true;
                driver.InsideBuildingType = LocationType.PersonalHouse;
                driver.InsideBuildingInstanceId = 0;
                driver.IdleActivityTimer = Mathf.Max(1f, driver.IdleActivityTimer);
                if (driver.AssignedPersonalHouseIndex >= 0 && driver.AssignedPersonalHouseIndex < personalHouses.Count)
                {
                    RecordWorkerBuildingKnowledge(driver, personalHouses[driver.AssignedPersonalHouseIndex], "\u041f\u043e\u0435\u043b \u0434\u043e\u043c\u0430", "Ate at home");
                }
                SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} started home meal at PersonalHouse #{driver.AssignedPersonalHouseIndex}; need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Meal)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
                return;

            case DriverRescuePhase.ToPersonalHouseParking:
                isDriverRescueActive = false;
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.LifeGoal = WorkerLifeGoal.Idle;
                SessionDebugLogger.Log("PERSONAL_CAR", $"{driver.DriverName} parked new personal car at home (house #{driver.AssignedPersonalHouseIndex}).");
                ContinueWorkerLifeCycle(driver, currentPosition);
                return;

            case DriverRescuePhase.ToMotelEntrance:
                isDriverRescueActive = false;
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.DriverObject.transform.position = driver.MotelIdlePosition;
                if (locations.TryGetValue(LocationType.Motel, out LocationData motelData) && driver.Money >= motelData.ServiceFee)
                {
                    int moneyBefore = driver.Money;
                    int bankBefore = motelData.BuildingBank;
                    driver.Money -= motelData.ServiceFee;
                    motelData.BuildingBank += motelData.ServiceFee;
                    SpawnMoneySpendPopup(driver.MotelIdlePosition, motelData.ServiceFee);
                    driver.DriverObject.SetActive(false);
                    driver.SleepTimer = DriverSleepDuration;
                    driver.RestPhase = DriverRestPhase.Sleeping;
                    LogBuildingBankTransaction(motelData, driver, motelData.ServiceFee, "Motel sleep check-in", moneyBefore, bankBefore);
                    RecordWorkerBuildingKnowledge(driver, motelData, "\u0417\u0430\u0441\u0435\u043b\u0438\u043b\u0441\u044f \u0432 \u043c\u043e\u0442\u0435\u043b\u044c", "Checked into the motel");
                    RecordWorkerServiceThought(driver, LocationType.Motel, WorkerNeedKind.Sleep, "sleep_service_good", WorkerThoughtTone.Positive, 46, 3);
                    SessionDebugLogger.Log("REST", $"{driver.DriverName} checked into motel - paid ${motelData.ServiceFee} (balance: ${driver.Money}). Sleeping for {DriverSleepDuration}s.");
                }
                else
                {
                    driver.RestPhase = DriverRestPhase.None;
                    string reason = $"not enough money (${driver.Money} < ${motelData?.ServiceFee ?? 0})";
                    SessionDebugLogger.Log("REST", $"{driver.DriverName} couldn't afford motel ({reason}) - looking for fallback sleep.");
                    if (TryStartWorkerNeedFallback(driver, WorkerNeedKind.Sleep, driver.DriverObject.transform.position, reason))
                    {
                        return;
                    }

                    SessionDebugLogger.Log("REST", $"{driver.DriverName} couldn't start fallback sleep after Motel money check.");
                }
                return;

            case DriverRescuePhase.ToTruckAtMotel:
                isDriverRescueActive = false;
                driver.WalkPhase = DriverRescuePhase.None;
                driver.DriverObject.SetActive(false);
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.RestPhase = DriverRestPhase.ReturnToParking;
                SessionDebugLogger.Log("REST", $"{GetLoadedTruckDisplayName()} driver boarded truck at motel. Returning to Parking.");
                return;

            case DriverRescuePhase.ToParkingForShift:
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.WaitingForShiftAtParking = true;
                SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} arrived at Parking for upcoming shift.");
                TryBoardDriverToAssignedTruck(driver);
                TryBoardBusDriver(driver);
                return;

            case DriverRescuePhase.WalkToLocalBusStop:
                driver.WalkPhase = DriverRescuePhase.WaitingAtLocalBusStop;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                LocationData reachedStop = GetLocalStopByNumber(driver.BusOriginStopNumber);
                if (reachedStop != null)
                {
                    RecordWorkerBuildingKnowledge(driver, reachedStop, "\u041f\u0440\u0438\u0448\u0451\u043b \u043d\u0430 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u043d\u0443\u044e \u043e\u0441\u0442\u0430\u043d\u043e\u0432\u043a\u0443", "Reached a bus stop");
                }
                SessionDebugLogger.Log(
                    "BUS_PASSENGER",
                    $"{driver.DriverName} reached Stop #{driver.BusOriginStopNumber} and is waiting for a local bus to Stop #{driver.BusDestinationStopNumber}.");
                return;

            case DriverRescuePhase.ToMotelFromBusStop:
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.IsArrivingByBus = false;
                driver.DriverObject.transform.position = driver.MotelIdlePosition;
                driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
                driver.IdleWanderPointIndex = -1;
                SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} reached Motel after arriving by bus.");
                return;

            case DriverRescuePhase.IdleWander:
                if (driver.LifeGoal == WorkerLifeGoal.Eat ||
                    driver.LifeGoal == WorkerLifeGoal.Sleep ||
                    driver.LifeGoal == WorkerLifeGoal.Leisure)
                {
                    driver.WalkPhase = DriverRescuePhase.IdlePhoneCall;
                    driver.WalkPath.Clear();
                    driver.WalkWaypointIndex = 0;
                    driver.WalkAnimationTime = 0f;
                    SessionDebugLogger.Log("IDLE", $"{driver.DriverName} reached free fallback point for {driver.LifeGoal}.");
                    return;
                }

                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
                SessionDebugLogger.Log("IDLE", $"{driver.DriverName} reached idle point.");
                return;

            case DriverRescuePhase.IdleWalkToBench:
                driver.WalkPhase = DriverRescuePhase.IdleSittingOnBench;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                string restTargetLabel = driver.SittingBenchIndex >= 0 ? "bench" : "free rest spot";
                SessionDebugLogger.Log("IDLE", $"{driver.DriverName} reached {restTargetLabel} and is sitting down.");
                return;

            case DriverRescuePhase.IdleWalkToCat:
            {
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                int catIdx = driver.IdleCatPetTargetIndex;
                if (catIdx >= 0 && catIdx < ambientCats.Count)
                {
                    AmbientCatData targetCat = ambientCats[catIdx];
                    if (targetCat != null && targetCat.RootTransform != null && targetCat.State != AmbientCatState.BeingPetted)
                    {
                        targetCat.State = AmbientCatState.BeingPetted;
                        targetCat.PettedByDriverId = driver.DriverId;
                        targetCat.PettingTimer = driver.IdleActivityTimer + 4f;
                        driver.WalkPhase = DriverRescuePhase.IdlePettingCat;
                        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} started petting a cat.");
                        return;
                    }
                }
                driver.IdleCatPetTargetIndex = -1;
                driver.WalkPhase = DriverRescuePhase.None;
                driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
                return;
            }

            case DriverRescuePhase.IdleWalkToBar:
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                LocationData barData = GetDriverPendingServiceLocation(driver, LocationType.Bar);
                if (barData != null && driver.Money >= barData.ServiceFee)
                {
                    driver.WalkPhase = DriverRescuePhase.IdleAtBar;
                    if (barData.ServiceFee > 0)
                    {
                        int moneyBefore = driver.Money;
                        int bankBefore = barData.BuildingBank;
                        driver.Money -= barData.ServiceFee;
                        barData.BuildingBank += barData.ServiceFee;
                        SpawnMoneySpendPopup(driver.DriverObject.transform.position, barData.ServiceFee);
                        LogBuildingBankTransaction(barData, driver, barData.ServiceFee, "Bar leisure visit", moneyBefore, bankBefore);
                        RecordWorkerServiceThought(driver, LocationType.Bar, WorkerNeedKind.Leisure, "leisure_service_good", WorkerThoughtTone.Positive, 42, 3);
                        SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} entered Bar for Leisure; paid=${barData.ServiceFee}, balance=${driver.Money}, need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Leisure)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
                        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} entered Bar - paid ${barData.ServiceFee} (balance: ${driver.Money}).");
                    }
                    else
                    {
                        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} entered Bar.");
                    }

                    EnterWorkerServiceInterior(driver, LocationType.Bar, barData);
                }
                else
                {
                    driver.PendingServiceLocationInstanceId = 0;
                    if (driver.LifeGoal == WorkerLifeGoal.Leisure)
                    {
                        driver.LifeGoal = WorkerLifeGoal.None;
                        SetWorkerNeedRetryCooldown(driver, WorkerNeedKind.Leisure, GetWorkerServiceUnavailableReason(driver, LocationType.Bar));
                        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} could not complete Bar leisure after arrival; reason={GetWorkerServiceUnavailableReason(driver, LocationType.Bar)}; snapshot={FormatWorkerNeedsDebug(driver)}.");
                        ContinueWorkerLifeCycle(driver, currentPosition);
                        return;
                    }

                    driver.WalkPhase = DriverRescuePhase.IdleWander;
                    driver.IdleWanderPointIndex++;
                    BuildDriverWalkPath(driver, currentPosition, driver.MotelIdlePosition);
                    SessionDebugLogger.Log("IDLE", $"{driver.DriverName} arrived at Bar - service unavailable, wandering back.");
                }
                return;

            case DriverRescuePhase.IdleWalkToCanteen:
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                LocationData canteenData = GetDriverPendingServiceLocation(driver, LocationType.Canteen);
                if (canteenData != null && driver.Money >= canteenData.ServiceFee)
                {
                    driver.WalkPhase = DriverRescuePhase.IdleAtCanteen;
                    if (canteenData.ServiceFee > 0)
                    {
                        int moneyBefore = driver.Money;
                        int bankBefore = canteenData.BuildingBank;
                        driver.Money -= canteenData.ServiceFee;
                        canteenData.BuildingBank += canteenData.ServiceFee;
                        SpawnMoneySpendPopup(driver.DriverObject.transform.position, canteenData.ServiceFee);
                        LogBuildingBankTransaction(canteenData, driver, canteenData.ServiceFee, "Canteen meal visit", moneyBefore, bankBefore);
                        RecordWorkerServiceThought(driver, LocationType.Canteen, WorkerNeedKind.Meal, "meal_service_good", WorkerThoughtTone.Positive, 54, 5);
                        SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} entered Canteen for Meal; paid=${canteenData.ServiceFee}, balance=${driver.Money}, need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Meal)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
                        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} entered Canteen - paid ${canteenData.ServiceFee} (balance: ${driver.Money}).");
                    }
                    else
                    {
                        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} entered Canteen.");
                    }

                    EnterWorkerServiceInterior(driver, LocationType.Canteen, canteenData);
                }
                else
                {
                    driver.PendingServiceLocationInstanceId = 0;
                    if (driver.LifeGoal == WorkerLifeGoal.Eat)
                    {
                        driver.LifeGoal = WorkerLifeGoal.None;
                        string canteenReason = GetWorkerServiceUnavailableReason(driver, LocationType.Canteen);
                        if (IsCanteenBlockedByMoney(driver, canteenReason) && TryStartWorkerTrashCanMealFallback(driver, currentPosition, canteenReason))
                        {
                            return;
                        }

                        SetWorkerNeedRetryCooldown(driver, WorkerNeedKind.Meal, canteenReason);
                        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} could not complete Canteen meal after arrival; reason={canteenReason}; snapshot={FormatWorkerNeedsDebug(driver)}.");
                        ContinueWorkerLifeCycle(driver, currentPosition);
                        return;
                    }

                    driver.WalkPhase = DriverRescuePhase.IdleWander;
                    driver.IdleWanderPointIndex++;
                    BuildDriverWalkPath(driver, currentPosition, driver.MotelIdlePosition);
                    SessionDebugLogger.Log("IDLE", $"{driver.DriverName} arrived at Canteen - service unavailable, wandering back.");
                }
                return;

            case DriverRescuePhase.IdleWalkToKiosk:
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                if (TryGetVendorPurchaseForPhase(driver, driver.WalkPhase, out LocationType vendorType, out string itemId, out DriverRescuePhase atPhase))
                {
                    LocationData vendor = FindLocationByInstanceId(driver.PendingVendorLocationInstanceId);
                    if (vendor != null && vendor.Type == vendorType && CanWorkerReceiveVendorInventoryItem(driver, itemId))
                    {
                        driver.WalkPhase = atPhase;
                        driver.IdleActivityTimer = Mathf.Max(0.6f, driver.IdleActivityTimer);
                        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} reached {vendor.Label}#{vendor.InstanceId} counter for {itemId}.");
                        return;
                    }
                }

                driver.PendingVendorLocationInstanceId = 0;
                driver.PendingVendorItemId = string.Empty;
                driver.WalkPhase = DriverRescuePhase.None;
                driver.IdleWanderPauseTimer = Random.Range(0.5f, 1.5f);
                SessionDebugLogger.Log("IDLE", $"{driver.DriverName} arrived for vendor purchase, but the stand/item is no longer available.");
                return;

            case DriverRescuePhase.IdleWalkToTrashCan:
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.WalkPhase = DriverRescuePhase.IdleAtTrashCan;
                SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} started trash can meal fallback; balance=${driver.Money}, need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Meal)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
                return;

            case DriverRescuePhase.IdleWalkToGamblingHall:
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.WalkPhase = DriverRescuePhase.IdleAtGamblingHall;
                ResolveWorkerGamblingSpinResult(driver);
                EnterWorkerServiceInterior(driver, LocationType.GamblingHall, GetDriverPendingServiceLocation(driver, LocationType.GamblingHall));
                return;

            case DriverRescuePhase.IdleWalkToCityPark:
                driver.PendingServiceLocationInstanceId = 0;
                if (TryStartCityParkPromenade(driver, currentPosition))
                {
                    RecordWorkerServiceThought(driver, LocationType.CityPark, WorkerNeedKind.Leisure, "leisure_service_good", WorkerThoughtTone.Positive, 38, 3);
                    return;
                }

                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.WalkPhase = DriverRescuePhase.IdleAtCityPark;
                RecordWorkerBuildingKnowledge(driver, LocationType.CityPark, "\u041f\u043e\u0441\u0435\u0442\u0438\u043b \u0433\u043e\u0440\u043e\u0434\u0441\u043a\u043e\u0439 \u043f\u0430\u0440\u043a", "Visited the city park");
                RecordWorkerServiceThought(driver, LocationType.CityPark, WorkerNeedKind.Leisure, "leisure_service_good", WorkerThoughtTone.Positive, 38, 3);
                SessionDebugLogger.Log("IDLE", $"{driver.DriverName} arrived at City Park for Leisure.");
                return;

            case DriverRescuePhase.IdleExitCityPark:
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                CompleteCityParkLeisure(driver, currentPosition);
                return;

            case DriverRescuePhase.ToLaborExchangeForJob:
                StartLaborExchangeInterview(driver);
                return;

            case DriverRescuePhase.ToIntercityStopForDeparture:
                CompleteWorkerDeparture(driver, "reached Intercity Stop");
                return;

            case DriverRescuePhase.ToBuildingForShift:
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                if (!IsLogisticsWorkerWorkHour(driver))
                {
                    driver.WalkPhase = DriverRescuePhase.None;
                    driver.WaitingForShiftAtParking = true;
                    LocationData waitingBuilding = GetAssignedBuildingLocation(driver);
                    SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} reached {waitingBuilding?.Label ?? "assigned building"} early and is waiting for shift start.");
                    return;
                }

                StartLogisticsWorkerShiftAtBuilding(driver);
                return;

            case DriverRescuePhase.LumberToTree:
                driver.WalkPhase = DriverRescuePhase.LumberChopping;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                SessionDebugLogger.Log("LUMBER", $"{driver.DriverName} reached a forestry tree and started chopping.");
                return;

            case DriverRescuePhase.LumberCarryLogToBuilding:
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                if (lumberWorkerTasks.TryGetValue(driver.DriverId, out LumberWorkerTaskData carryTask))
                {
                    DeliverForestWorkerLog(driver, carryTask);
                }
                else
                {
                    ReturnForestWorkerInside(driver, "missing carry task on log delivery");
                }
                return;

            case DriverRescuePhase.LumberReturnToTreeForPlanting:
                driver.WalkPhase = DriverRescuePhase.LumberPlanting;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                SessionDebugLogger.Log("LUMBER", $"{driver.DriverName} reached the forestry plot and started planting.");
                return;

            case DriverRescuePhase.LumberReturnToBuilding:
                ReturnForestWorkerInside(driver, "walked back into Lumberyard");
                return;

            case DriverRescuePhase.ToMotelFromBuilding:
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.DriverObject.transform.position = driver.MotelIdlePosition;
                if (locations.TryGetValue(LocationType.Motel, out LocationData motelFromBldg) && driver.Money >= motelFromBldg.ServiceFee)
                {
                    int moneyBefore = driver.Money;
                    int bankBefore = motelFromBldg.BuildingBank;
                    driver.Money -= motelFromBldg.ServiceFee;
                    motelFromBldg.BuildingBank += motelFromBldg.ServiceFee;
                    SpawnMoneySpendPopup(driver.MotelIdlePosition, motelFromBldg.ServiceFee);
                    driver.DriverObject.SetActive(false);
                    driver.SleepTimer       = DriverSleepDuration;
                    driver.RestPhase        = DriverRestPhase.Sleeping;
                    LogBuildingBankTransaction(motelFromBldg, driver, motelFromBldg.ServiceFee, "Motel sleep after production/logistics shift", moneyBefore, bankBefore);
                    RecordWorkerBuildingKnowledge(driver, motelFromBldg, "\u0417\u0430\u0441\u0435\u043b\u0438\u043b\u0441\u044f \u0432 \u043c\u043e\u0442\u0435\u043b\u044c \u043f\u043e\u0441\u043b\u0435 \u0441\u043c\u0435\u043d\u044b", "Checked into the motel after a shift");
                    SessionDebugLogger.Log("REST", $"{driver.DriverName} checked into motel after logistics shift - paid ${motelFromBldg.ServiceFee} (balance: ${driver.Money}).");
                }
                else
                {
                    driver.RestPhase = DriverRestPhase.None;
                    SessionDebugLogger.Log("REST", $"{driver.DriverName} couldn't afford motel after logistics shift (${driver.Money} < ${motelFromBldg?.ServiceFee ?? 0}) - staying outside.");
                }
                return;
        }
    }

    private static bool RequiresDriverWalkPathForArrival(DriverRescuePhase phase)
    {
        return phase switch
        {
            DriverRescuePhase.ToGasStation or
            DriverRescuePhase.ToTruck or
            DriverRescuePhase.ToPersonalHouseForPurchase or
            DriverRescuePhase.ToCarMarketForPurchase or
            DriverRescuePhase.ToPersonalHouseEntrance or
            DriverRescuePhase.ToPersonalHouseMeal or
            DriverRescuePhase.ToPersonalHouseParking or
            DriverRescuePhase.ToMotelEntrance or
            DriverRescuePhase.ToTruckAtMotel or
            DriverRescuePhase.ToParkingForShift or
            DriverRescuePhase.WalkToLocalBusStop or
            DriverRescuePhase.ToMotelFromBusStop or
            DriverRescuePhase.IdleWander or
            DriverRescuePhase.IdleWalkToBench or
            DriverRescuePhase.IdleWalkToCat or
            DriverRescuePhase.IdleWalkToBar or
            DriverRescuePhase.IdleWalkToCanteen or
            DriverRescuePhase.IdleWalkToKiosk or
            DriverRescuePhase.IdleWalkToTrashCan or
            DriverRescuePhase.IdleWalkToGamblingHall or
            DriverRescuePhase.IdleWalkToCityPark or
            DriverRescuePhase.IdleExitCityPark or
            DriverRescuePhase.ToLaborExchangeForJob or
            DriverRescuePhase.ToIntercityStopForDeparture or
            DriverRescuePhase.ToBuildingForShift or
            DriverRescuePhase.LumberToTree or
            DriverRescuePhase.LumberCarryLogToBuilding or
            DriverRescuePhase.LumberReturnToTreeForPlanting or
            DriverRescuePhase.LumberReturnToBuilding or
            DriverRescuePhase.ToMotelFromBuilding => true,
            _ => false
        };
    }


}
