using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void UpdateDriverWalk(DriverAgent driver)
    {
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

        Vector3 currentPosition = driver.DriverObject.transform.position;
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
                SessionDebugLogger.Log("IDLE", $"{driver.DriverName} paused idle walk to avoid overlapping another driver.");
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
                if (locations.TryGetValue(LocationType.GasStation, out LocationData gsEmergency))
                    gsEmergency.FuelStored = GasStationMaxFuelStorage;
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
                        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} bought house #{hIdx} for ${HousePurchasePrice} (balance: ${driver.Money}).");
                    }
                    else
                    {
                        driver.AssignedPersonalHouseIndex = -1;
                        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} couldn't complete house purchase (money=${driver.Money}) — reservation released.");
                    }
                }
                driver.LifeGoal = WorkerLifeGoal.Idle;
                isDriversScreenDirty = true;
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
                    SpawnWorkerCarAtParking(driver);
                    SpawnMoneySpendPopup(driver.DriverObject.transform.position, CarPurchasePrice);
                    LogBuildingBankTransaction(market, driver, CarPurchasePrice, "Car purchase", moneyBefore, bankBefore);
                    SessionDebugLogger.Log("LIFE", $"{driver.DriverName} bought {CarModelNames[driver.OwnedCarModelIndex]} for ${CarPurchasePrice} (balance: ${driver.Money}).");
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
                SessionDebugLogger.Log("REST", $"{driver.DriverName} sleeping at home (house #{driver.AssignedPersonalHouseIndex}).");
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
                SessionDebugLogger.Log("IDLE", $"{driver.DriverName} reached bench and is sitting down.");
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
                if (locations.TryGetValue(LocationType.Bar, out LocationData barData) && barData.AlcoholStored > 0)
                {
                    driver.WalkPhase = DriverRescuePhase.IdleAtBar;
                    barData.AlcoholStored = Mathf.Max(0, barData.AlcoholStored - 1);
                    if (HasWorkerEffect(driver, WorkerHangoverEffectId))
                    {
                        RemoveWorkerEffect(driver, WorkerHangoverEffectId);
                    }
                    ApplyWorkerDrunkEffect(driver);
                    if (barData.ServiceFee > 0)
                    {
                        int moneyBefore = driver.Money;
                        int bankBefore = barData.BuildingBank;
                        driver.Money -= barData.ServiceFee;
                        barData.BuildingBank += barData.ServiceFee;
                        SpawnMoneySpendPopup(driver.DriverObject.transform.position, barData.ServiceFee);
                        LogBuildingBankTransaction(barData, driver, barData.ServiceFee, "Bar leisure visit", moneyBefore, bankBefore);
                        SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} entered Bar for Leisure; paid=${barData.ServiceFee}, consumed=1 Alcohol, balance=${driver.Money}, need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Leisure)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
                        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} entered Bar - paid ${barData.ServiceFee}, consumed 1 Alcohol (balance: ${driver.Money}).");
                    }
                    else
                    {
                        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} entered Bar - consumed 1 Alcohol.");
                    }
                }
                else
                {
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
                    SessionDebugLogger.Log("IDLE", $"{driver.DriverName} arrived at Bar - no Alcohol left, wandering back.");
                }
                return;

            case DriverRescuePhase.IdleWalkToCanteen:
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                if (locations.TryGetValue(LocationType.Canteen, out LocationData canteenData) &&
                    canteenData.FoodStored > 0 &&
                    driver.Money >= canteenData.ServiceFee)
                {
                    driver.WalkPhase = DriverRescuePhase.IdleAtCanteen;
                    canteenData.FoodStored = Mathf.Max(0, canteenData.FoodStored - 1);
                    ApplyWorkerFedEffect(driver);
                    if (canteenData.ServiceFee > 0)
                    {
                        int moneyBefore = driver.Money;
                        int bankBefore = canteenData.BuildingBank;
                        driver.Money -= canteenData.ServiceFee;
                        canteenData.BuildingBank += canteenData.ServiceFee;
                        SpawnMoneySpendPopup(driver.DriverObject.transform.position, canteenData.ServiceFee);
                        LogBuildingBankTransaction(canteenData, driver, canteenData.ServiceFee, "Canteen meal visit", moneyBefore, bankBefore);
                        SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} entered Canteen for Meal; paid=${canteenData.ServiceFee}, consumed=1 Food, balance=${driver.Money}, need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Meal)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
                        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} entered Canteen - paid ${canteenData.ServiceFee}, consumed 1 Food (balance: ${driver.Money}).");
                    }
                    else
                    {
                        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} entered Canteen - consumed 1 Food.");
                    }
                }
                else
                {
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
                    SessionDebugLogger.Log("IDLE", $"{driver.DriverName} arrived at Canteen - no Food left, wandering back.");
                }
                return;

            case DriverRescuePhase.IdleWalkToTrashCan:
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.WalkPhase = DriverRescuePhase.IdleAtTrashCan;
                ApplyWorkerMoneyFallbackEffect(driver);
                SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} started trash can meal fallback; balance=${driver.Money}, need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Meal)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
                return;

            case DriverRescuePhase.IdleWalkToGamblingHall:
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.WalkPhase = DriverRescuePhase.IdleAtGamblingHall;
                ResolveWorkerGamblingSpinResult(driver);
                return;

            case DriverRescuePhase.IdleWalkToCityPark:
                if (TryStartCityParkPromenade(driver, currentPosition))
                {
                    return;
                }

                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.WalkPhase = DriverRescuePhase.IdleAtCityPark;
                SessionDebugLogger.Log("IDLE", $"{driver.DriverName} arrived at City Park for Leisure.");
                return;

            case DriverRescuePhase.IdleExitCityPark:
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                CompleteCityParkLeisure(driver, currentPosition);
                return;

            case DriverRescuePhase.WarehouseDeliveryToService:
                // Arrived at service building - unload the carried warehouse unit
                if (driver.WarehouseDeliveryTarget.HasValue &&
                    locations.TryGetValue(driver.WarehouseDeliveryTarget.Value, out LocationData serviceBuilding))
                {
                    int carriedAmount = Mathf.Max(1, driver.WarehouseDeliveryAmount);
                    int acceptedAmount = AddServiceResource(serviceBuilding, driver.WarehouseDeliveryResourceType, carriedAmount);
                    int overflowAmount = Mathf.Max(0, carriedAmount - acceptedAmount);
                    if (overflowAmount > 0 &&
                        locations.TryGetValue(LocationType.Warehouse, out LocationData warehouseOverflow))
                    {
                        switch (driver.WarehouseDeliveryResourceType)
                        {
                            case WarehouseResourceType.Fuel:
                                warehouseOverflow.FuelStored = Mathf.Min(warehouseOverflow.FuelStored + overflowAmount, WarehouseMaxFuelStorage);
                                break;
                            case WarehouseResourceType.Alcohol:
                                warehouseOverflow.AlcoholStored = Mathf.Min(warehouseOverflow.AlcoholStored + overflowAmount, WarehouseMaxAlcoholStorage);
                                break;
                            case WarehouseResourceType.Food:
                                warehouseOverflow.FoodStored = Mathf.Min(warehouseOverflow.FoodStored + overflowAmount, WarehouseMaxFoodStorage);
                                break;
                        }
                    }
                    SessionDebugLogger.Log(
                        "WAREHOUSE",
                        $"{driver.DriverName} delivered {GetWarehouseResourceTypeLabel(driver.WarehouseDeliveryResourceType)} x{acceptedAmount}/{carriedAmount} to {serviceBuilding.Label}; overflowReturned={overflowAmount}.");
                }
                ClearWarehouseDeliveryCargo(driver);
                // Return to Warehouse, using the local bus if it makes sense
                if (driver.AssignedBuildingType.HasValue &&
                    locations.TryGetValue(driver.AssignedBuildingType.Value, out LocationData warehouseReturn))
                {
                    Vector3 returnTarget = GetCellCenter(warehouseReturn.Anchor);
                    returnTarget.y += 0.05f;
                    ResetWorkerLocalBusTripState(driver);
                    if (TryStartWorkerLocalBusTrip(
                            driver,
                            currentPosition,
                            returnTarget,
                            DriverRescuePhase.WarehouseDeliveryReturn,
                            "Warehouse return",
                            true))
                    {
                        SessionDebugLogger.Log(
                            "WAREHOUSE",
                            $"{driver.DriverName} started bus-assisted return to Warehouse after delivery.");
                        return;
                    }

                    driver.WalkTargetWorld = returnTarget;
                    driver.WalkPhase = DriverRescuePhase.WarehouseDeliveryReturn;
                    BuildDriverWalkPath(driver, currentPosition, returnTarget);
                    SessionDebugLogger.Log("WAREHOUSE", $"{driver.DriverName} started walking back to Warehouse after delivery.");
                }
                return;

            case DriverRescuePhase.WarehouseDeliveryReturn:
                // Back at Warehouse - become invisible, ready for next delivery
                driver.WalkPhase         = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.IsInsideBuilding  = true;
                driver.WarehouseDeliveryTarget = null;
                ClearWarehouseDeliveryCargo(driver);
                driver.DriverObject.SetActive(false);
                SessionDebugLogger.Log("WAREHOUSE", $"{driver.DriverName} returned to Warehouse - ready for next delivery.");
                return;

            case DriverRescuePhase.ToBuildingForShift:
                driver.WalkPhase        = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.IsOnActiveShift   = true;
                driver.IsInsideBuilding  = true;
                driver.IsShiftSalaryPending = true;
                driver.DriverObject.SetActive(false);
                if (driver.AssignedBuildingType.HasValue && locations.TryGetValue(driver.AssignedBuildingType.Value, out LocationData enteredBuilding))
                {
                    enteredBuilding.Workers = driver.AssignedBuildingType == LocationType.Warehouse
                        ? Mathf.Min(enteredBuilding.Workers + 1, WarehouseMaxWorkers)
                        : 1;
                    SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} entered {enteredBuilding.Label} - building operational.");
                }
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


}
