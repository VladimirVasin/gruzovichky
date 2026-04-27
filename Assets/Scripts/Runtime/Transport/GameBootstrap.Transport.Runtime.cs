using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private bool TryHandleTruckSelection(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            return false;
        }

        if (hit.transform == null)
        {
            return false;
        }

        foreach (TruckAgent truckAgent in truckAgents)
        {
            if (truckAgent.TruckObject != null && hit.transform.IsChildOf(truckAgent.TruckObject.transform))
            {
                FocusTruck(truckAgent.TruckNumber);
                return true;
            }
        }

        return false;
    }

    private bool TryHandleLocalBusSelection(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            return false;
        }

        if (hit.transform == null || localBusRoute?.RootTransform == null)
        {
            return false;
        }

        if (!hit.transform.IsChildOf(localBusRoute.RootTransform))
        {
            return false;
        }

        FocusLocalBus();
        return true;
    }

    private bool TryHandleDriverSelection(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f)) return false;
        if (hit.transform == null) return false;

        foreach (DriverAgent driver in driverAgents)
        {
            if (driver.DriverObject != null && driver.DriverObject.activeSelf &&
                hit.transform.IsChildOf(driver.DriverObject.transform))
            {
                FocusDriver(driver.DriverId);
                return true;
            }
        }

        return false;
    }

    private void ProduceForestWood()
    {
        UpdateLumberyardSystem();
    }

    private void UpdateSawmillProcessing()
    {
        if (!locations.TryGetValue(LocationType.Sawmill, out LocationData sawmill))
        {
            return;
        }

        if (sawmill.LogsStored <= 0)
        {
            sawmillProcessingTimer = 0f;
            return;
        }

        if (!IsLocationOperational(LocationType.Sawmill))
        {
            return;
        }

        sawmillProcessingTimer += Time.deltaTime * gameSpeedMultiplier;
        if (sawmillProcessingTimer < 4.5f)
        {
            return;
        }

        sawmillProcessingTimer = 0f;
        sawmill.LogsStored = Mathf.Max(0, sawmill.LogsStored - 1);
        sawmill.BoardsStored += 1;
        SessionDebugLogger.Log("SAWMILL", $"Sawmill processed 1 Logs into Boards. Logs={sawmill.LogsStored}, Boards={sawmill.BoardsStored}.");
    }

    private void UpdateFurnitureFactoryProcessing()
    {
        if (!locations.TryGetValue(LocationType.FurnitureFactory, out LocationData furnitureFactory))
        {
            furnitureFactoryProcessingTimer = 0f;
            return;
        }

        if (furnitureFactory.BoardsStored <= 0 ||
            furnitureFactory.TextileStored <= 0 ||
            furnitureFactory.FurnitureStored >= FurnitureFactoryMaxFurnitureStorage)
        {
            furnitureFactoryProcessingTimer = 0f;
            return;
        }

        if (!IsLocationOperational(LocationType.FurnitureFactory))
        {
            return;
        }

        furnitureFactoryProcessingTimer += Time.deltaTime * gameSpeedMultiplier;
        if (furnitureFactoryProcessingTimer < FurnitureFactoryProcessingDuration)
        {
            return;
        }

        furnitureFactoryProcessingTimer = 0f;
        furnitureFactory.BoardsStored = Mathf.Max(0, furnitureFactory.BoardsStored - 1);
        furnitureFactory.TextileStored = Mathf.Max(0, furnitureFactory.TextileStored - 1);
        furnitureFactory.FurnitureStored = Mathf.Min(FurnitureFactoryMaxFurnitureStorage, furnitureFactory.FurnitureStored + 1);
        SessionDebugLogger.Log(
            "FACTORY",
            $"Furniture Factory produced 1 Furniture. Boards={furnitureFactory.BoardsStored}, Textile={furnitureFactory.TextileStored}, Furniture={furnitureFactory.FurnitureStored}.");
    }

    private void UpdateTruckMovement()
    {
        if (isTruckInteracting || isDriverRescueActive)
        {
            UpdateTruckVisuals(0f, false);
            return;
        }

        if (!isTruckMoving || activePath.Count == 0)
        {
            UpdateTruckVisuals(0f, false);
            return;
        }

        if (truckSegmentDuration <= 0.0001f)
        {
            BeginNextTruckSegment(activePath[0]);
        }

        Vector3 segmentDirection = truckTargetWorld - truckSegmentStartWorld;
        float segmentDistance = segmentDirection.magnitude;
        if (segmentDistance <= 0.0001f)
        {
            CompleteTruckSegment();
            return;
        }

        truckSegmentProgress += Time.deltaTime / truckSegmentDuration;
        float easedProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(truckSegmentProgress));
        Vector3 currentPosition = Vector3.Lerp(truckSegmentStartWorld, truckTargetWorld, easedProgress);
        truckObject.transform.position = currentPosition;

        Vector3 desiredForward = segmentDirection.normalized;
        truckSmoothedForward = Vector3.Slerp(truckSmoothedForward, desiredForward, 6f * Time.deltaTime).normalized;
        if (truckSmoothedForward.sqrMagnitude > 0.0001f)
        {
            truckObject.transform.rotation = Quaternion.Slerp(
                truckObject.transform.rotation,
                Quaternion.LookRotation(truckSmoothedForward, Vector3.up),
                9f * Time.deltaTime);
        }

        float segmentSpeed = segmentDistance / Mathf.Max(truckSegmentDuration, 0.001f);
        UpdateTruckVisuals(segmentSpeed, true);

        if (truckSegmentProgress < 1f)
        {
            return;
        }

        CompleteTruckSegment();
    }

    private void UpdateDriverWalk(DriverAgent driver)
    {
        if (driver == null || driver.WalkPhase == DriverRescuePhase.None || driver.DriverObject == null)
        {
            return;
        }

        if (driver.WalkPhase == DriverRescuePhase.WaitingAtLocalBusStop ||
            driver.WalkPhase == DriverRescuePhase.RidingLocalBus)
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
            SessionDebugLogger.Log(
                "DRIVER",
                $"{driver.DriverName} advanced to waypoint {driver.WalkWaypointIndex + 1}/{driver.WalkPath.Count} during {driver.WalkPhase}.");
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
                    SessionDebugLogger.Log("REST", $"{driver.DriverName} couldn't afford motel (${driver.Money} < ${motelData?.ServiceFee ?? 0}) - staying outside.");
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
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
                SessionDebugLogger.Log("IDLE", $"{driver.DriverName} reached motel idle point.");
                return;

            case DriverRescuePhase.IdleWalkToBench:
                driver.WalkPhase = DriverRescuePhase.IdleSittingOnBench;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                SessionDebugLogger.Log("IDLE", $"{driver.DriverName} reached bench and is sitting down.");
                return;

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
                        driver.HadLeisureToday = true;
                        driver.LifeGoal = WorkerLifeGoal.None;
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
                if (locations.TryGetValue(LocationType.Canteen, out LocationData canteenData) && canteenData.FoodStored > 0)
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
                        driver.AteToday = true;
                        driver.LifeGoal = WorkerLifeGoal.None;
                        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} could not complete Canteen meal after arrival; reason={GetWorkerServiceUnavailableReason(driver, LocationType.Canteen)}; snapshot={FormatWorkerNeedsDebug(driver)}.");
                        ContinueWorkerLifeCycle(driver, currentPosition);
                        return;
                    }

                    driver.WalkPhase = DriverRescuePhase.IdleWander;
                    driver.IdleWanderPointIndex++;
                    BuildDriverWalkPath(driver, currentPosition, driver.MotelIdlePosition);
                    SessionDebugLogger.Log("IDLE", $"{driver.DriverName} arrived at Canteen - no Food left, wandering back.");
                }
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

            case DriverRescuePhase.WarehouseDeliveryToService:
                // Arrived at service building - unload the carried warehouse unit
                if (driver.WarehouseDeliveryTarget.HasValue &&
                    locations.TryGetValue(driver.WarehouseDeliveryTarget.Value, out LocationData serviceBuilding))
                {
                    switch (driver.WarehouseDeliveryResourceType)
                    {
                        case WarehouseResourceType.Fuel:
                            serviceBuilding.FuelStored = Mathf.Min(serviceBuilding.FuelStored + 1, GasStationMaxFuelStorage);
                            break;
                        case WarehouseResourceType.Alcohol:
                            serviceBuilding.AlcoholStored = Mathf.Min(serviceBuilding.AlcoholStored + 1, BarMaxAlcoholStorage);
                            break;
                        case WarehouseResourceType.Food:
                            serviceBuilding.FoodStored = Mathf.Min(serviceBuilding.FoodStored + 1, CanteenMaxFoodStorage);
                            break;
                    }
                    SessionDebugLogger.Log(
                        "WAREHOUSE",
                        $"{driver.DriverName} delivered {GetWarehouseResourceTypeLabel(driver.WarehouseDeliveryResourceType)} x{Mathf.Max(1, driver.WarehouseDeliveryAmount)} to {serviceBuilding.Label}.");
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
                    NotifyTutorialProductionWorkerEntered(driver.AssignedBuildingType.Value);
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

    private bool WouldIdleDriverOverlapAtPosition(DriverAgent driver, Vector3 proposedPosition)
    {
        float personalSpaceSqr = DriverIdlePersonalSpace * DriverIdlePersonalSpace;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent other = driverAgents[i];
            if (other == null || other == driver || other.DriverObject == null || !other.DriverObject.activeSelf)
            {
                continue;
            }

            Vector3 otherDelta = other.DriverObject.transform.position - proposedPosition;
            otherDelta.y = 0f;
            if (otherDelta.sqrMagnitude < personalSpaceSqr)
            {
                return true;
            }
        }

        return false;
    }

    private void SeparateOverlappingDrivers()
    {
        if (driverAgents.Count < 2) return;
        const float minSep = DriverIdlePersonalSpace * 0.55f;
        float pushStrength = 2.2f * Time.deltaTime * gameSpeedMultiplier;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent a = driverAgents[i];
            if (!IsDriverNudgeable(a)) continue;
            for (int j = i + 1; j < driverAgents.Count; j++)
            {
                DriverAgent b = driverAgents[j];
                if (!IsDriverNudgeable(b)) continue;
                Vector3 delta = a.DriverObject.transform.position - b.DriverObject.transform.position;
                delta.y = 0f;
                float dist = delta.magnitude;
                if (dist >= minSep) continue;
                Vector3 dir = dist < 0.001f
                    ? new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized
                    : delta / dist;
                float push = (minSep - dist) * pushStrength;
                a.DriverObject.transform.position += dir * (push * 0.5f);
                b.DriverObject.transform.position -= dir * (push * 0.5f);
            }
        }
    }

    private static bool IsDriverNudgeable(DriverAgent driver)
    {
        if (driver == null || driver.DriverObject == null || !driver.DriverObject.activeSelf) return false;
        return driver.WalkPhase == DriverRescuePhase.None ||
               driver.WalkPhase == DriverRescuePhase.IdleWander ||
               driver.WalkPhase == DriverRescuePhase.IdleSmoking ||
               driver.WalkPhase == DriverRescuePhase.IdlePhoneCall;
    }

    private void UpdateDriverVisualAnimation(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        if (driver.DriverObject == null || driver.DriverVisualRoot == null)
        {
            return;
        }

        if (!driver.DriverObject.activeSelf)
        {
            driver.WalkAnimationTime = 0f;
            StopDriverSmokingParticles(driver);
            ApplyDriverPose(driver, 0f, 0f);
            return;
        }

        if (driver.WalkPhase != DriverRescuePhase.IdleSmoking)
        {
            StopDriverSmokingParticles(driver);
        }

        Vector3 toTarget = driver.WalkTargetWorld - driver.DriverObject.transform.position;
        toTarget.y = 0f;
        bool isWalking = driver.WalkPhase != DriverRescuePhase.None && toTarget.sqrMagnitude > 0.012f;
        bool isConversing = IsDriverIdleConversing(driver);
        if (isWalking)
        {
            driver.WalkAnimationTime += Time.deltaTime * 8.2f;
        }
        else
        {
            driver.WalkAnimationTime += Time.deltaTime * (isConversing ? 3.1f : 4.2f);
        }

        if (!isWalking && isConversing)
        {
            DriverAgent partner = GetDriverAgentById(driver.IdleConversationPartnerId);
            if (partner?.DriverObject != null)
            {
                Vector3 faceDirection = partner.DriverObject.transform.position - driver.DriverObject.transform.position;
                faceDirection.y = 0f;
                if (faceDirection.sqrMagnitude > 0.0001f)
                {
                    driver.DriverObject.transform.rotation = Quaternion.Slerp(
                        driver.DriverObject.transform.rotation,
                        Quaternion.LookRotation(faceDirection.normalized, Vector3.up),
                        7f * Time.deltaTime);
                }
            }
        }

        float swing = isWalking ? Mathf.Sin(driver.WalkAnimationTime) : 0f;
        float bob = isWalking
            ? Mathf.Abs(Mathf.Sin(driver.WalkAnimationTime * 2f)) * 0.06f
            : isConversing ? Mathf.Sin(driver.WalkAnimationTime * 1.8f) * 0.012f : 0f;

        switch (driver.WalkPhase)
        {
            case DriverRescuePhase.IdleSittingOnBench:
                ApplyDriverSittingPose(driver);
                return;
            case DriverRescuePhase.IdleSmoking:
                ApplyDriverSmokingPose(driver);
                return;
            case DriverRescuePhase.IdlePhoneCall:
                ApplyDriverPhoneCallPose(driver);
                return;
            case DriverRescuePhase.LumberChopping:
            case DriverRescuePhase.LumberPlanting:
                return;
            case DriverRescuePhase.LumberReturnToBuilding:
                break;
        }

        ApplyDriverPose(driver, swing, bob);
    }

    private void ApplyDriverSittingPose(DriverAgent driver)
    {
        float sway = Mathf.Sin(Time.time * 0.25f) * 1.5f;
        driver.DriverVisualRoot.localPosition = new Vector3(0f, -0.15f, 0f);
        driver.DriverVisualRoot.localRotation = Quaternion.Euler(0f, sway, 0f);

        if (driver.DriverBodyTransform != null)
            driver.DriverBodyTransform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        if (driver.DriverHeadTransform != null)
            driver.DriverHeadTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        if (driver.DriverCapTransform != null)
            driver.DriverCapTransform.localRotation = Quaternion.identity;
        if (driver.DriverLeftArmTransform != null)
            driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(35f, 0f, 0f);
        if (driver.DriverRightArmTransform != null)
            driver.DriverRightArmTransform.localRotation = Quaternion.Euler(35f, 0f, 0f);
        if (driver.DriverLeftLegTransform != null)
            driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(-85f, 0f, 0f);
        if (driver.DriverRightLegTransform != null)
            driver.DriverRightLegTransform.localRotation = Quaternion.Euler(-85f, 0f, 0f);
    }

    private void ApplyDriverSmokingPose(DriverAgent driver)
    {
        float drag = Mathf.Sin(Time.time * 1.1f) * 8f;
        driver.DriverVisualRoot.localPosition = Vector3.zero;
        driver.DriverVisualRoot.localRotation = Quaternion.identity;

        if (driver.DriverBodyTransform != null)
            driver.DriverBodyTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
        if (driver.DriverHeadTransform != null)
            driver.DriverHeadTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        if (driver.DriverCapTransform != null)
            driver.DriverCapTransform.localRotation = Quaternion.identity;
        if (driver.DriverLeftArmTransform != null)
            driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        if (driver.DriverRightArmTransform != null)
            driver.DriverRightArmTransform.localRotation = Quaternion.Euler(-65f + drag, 0f, 0f);
        if (driver.DriverLeftLegTransform != null)
            driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(3f, 0f, 0f);
        if (driver.DriverRightLegTransform != null)
            driver.DriverRightLegTransform.localRotation = Quaternion.Euler(-3f, 0f, 0f);

        UpdateDriverSmokingParticles(driver);
    }

    private void InitDriverSmokingParticles(DriverAgent driver)
    {
        driver.SmokingParticles = new Transform[SmokingParticlePoolSize];
        driver.SmokingParticleMaterials = new Material[SmokingParticlePoolSize];
        driver.SmokingParticleLives = new float[SmokingParticlePoolSize];
        driver.SmokingParticleVelocities = new Vector3[SmokingParticlePoolSize];
        for (int i = 0; i < SmokingParticlePoolSize; i++)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.name = "SmokePuff";
            p.transform.SetParent(worldRoot, false);
            p.transform.localScale = Vector3.one * 0.04f;
            Material mat = CreateTransparentOverlayMaterial(new Color(0.82f, 0.82f, 0.82f, 0f));
            Renderer r = p.GetComponent<Renderer>();
            r.material = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (p.TryGetComponent(out Collider col)) Object.Destroy(col);
            p.SetActive(false);
            driver.SmokingParticles[i] = p.transform;
            driver.SmokingParticleMaterials[i] = mat;
        }
    }

    private void UpdateDriverSmokingParticles(DriverAgent driver)
    {
        if (driver.SmokingParticles == null)
            InitDriverSmokingParticles(driver);

        float dt = Time.deltaTime * gameSpeedMultiplier;

        for (int i = 0; i < SmokingParticlePoolSize; i++)
        {
            if (driver.SmokingParticleLives[i] <= 0f) continue;
            driver.SmokingParticleLives[i] -= dt;
            Transform p = driver.SmokingParticles[i];
            if (p == null) continue;
            p.position += driver.SmokingParticleVelocities[i] * dt;
            float t = 1f - Mathf.Clamp01(driver.SmokingParticleLives[i] / SmokingParticleMaxLife);
            float alpha = (1f - t * t) * 0.4f;
            float scale = Mathf.Lerp(0.04f, 0.13f, t);
            p.localScale = Vector3.one * scale;
            driver.SmokingParticleMaterials[i].color = new Color(0.84f, 0.84f, 0.84f, alpha);
            if (driver.SmokingParticleLives[i] <= 0f)
                p.gameObject.SetActive(false);
        }

        driver.SmokingEmitTimer -= dt;
        if (driver.SmokingEmitTimer > 0f || driver.DriverRightArmTransform == null) return;

        driver.SmokingEmitTimer = SmokingParticleEmitInterval;
        int slot = -1;
        for (int i = 0; i < SmokingParticlePoolSize; i++)
        {
            if (driver.SmokingParticleLives[i] <= 0f) { slot = i; break; }
        }
        if (slot < 0) return;

        Vector3 spawnPos = driver.DriverRightArmTransform.position
            + driver.DriverRightArmTransform.up * 0.18f;
        Transform pSlot = driver.SmokingParticles[slot];
        pSlot.position = spawnPos;
        pSlot.gameObject.SetActive(true);
        driver.SmokingParticleLives[slot] = SmokingParticleMaxLife;
        driver.SmokingParticleVelocities[slot] = new Vector3(
            Random.Range(-0.014f, 0.014f),
            Random.Range(0.09f, 0.15f),
            Random.Range(-0.014f, 0.014f));
    }

    private void StopDriverSmokingParticles(DriverAgent driver)
    {
        if (driver == null || driver.SmokingParticles == null)
        {
            return;
        }

        driver.SmokingEmitTimer = 0f;
        for (int i = 0; i < driver.SmokingParticles.Length; i++)
        {
            driver.SmokingParticleLives[i] = 0f;
            Transform particle = driver.SmokingParticles[i];
            if (particle != null)
            {
                particle.gameObject.SetActive(false);
            }

            if (driver.SmokingParticleMaterials != null &&
                i < driver.SmokingParticleMaterials.Length &&
                driver.SmokingParticleMaterials[i] != null)
            {
                driver.SmokingParticleMaterials[i].color = new Color(0.84f, 0.84f, 0.84f, 0f);
            }
        }
    }

    private void ApplyDriverPhoneCallPose(DriverAgent driver)
    {
        driver.DriverVisualRoot.localPosition = Vector3.zero;
        driver.DriverVisualRoot.localRotation = Quaternion.identity;
        driver.DriverObject.transform.Rotate(Vector3.up, 6f * Time.deltaTime * gameSpeedMultiplier);

        if (driver.DriverBodyTransform != null)
            driver.DriverBodyTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        if (driver.DriverHeadTransform != null)
            driver.DriverHeadTransform.localRotation = Quaternion.Euler(5f, 0f, 0f);
        if (driver.DriverCapTransform != null)
            driver.DriverCapTransform.localRotation = Quaternion.identity;
        if (driver.DriverLeftArmTransform != null)
            driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        if (driver.DriverRightArmTransform != null)
            driver.DriverRightArmTransform.localRotation = Quaternion.Euler(-75f, 0f, 0f);
        if (driver.DriverLeftLegTransform != null)
            driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(3f, 0f, 0f);
        if (driver.DriverRightLegTransform != null)
            driver.DriverRightLegTransform.localRotation = Quaternion.Euler(-3f, 0f, 0f);
    }

    private void ApplyDriverPose(DriverAgent driver, float swing, float bob)
    {
        bool isConversing = IsDriverIdleConversing(driver);
        driver.DriverVisualRoot.localPosition = new Vector3(0f, bob, 0f);
        driver.DriverVisualRoot.localRotation = Quaternion.Euler(0f, 0f, isConversing ? Mathf.Sin(driver.WalkAnimationTime * 1.3f) * 1.4f : swing * 2.5f);

        if (driver.DriverBodyTransform != null)
        {
            driver.DriverBodyTransform.localRotation = Quaternion.Euler(isConversing ? Mathf.Sin(driver.WalkAnimationTime * 1.1f) * 3.5f : swing * 4f, 0f, 0f);
        }

        if (driver.DriverHeadTransform != null)
        {
            float headPitch = isConversing ? Mathf.Sin(driver.WalkAnimationTime * 1.7f) * 3.2f : -swing * 2f;
            driver.DriverHeadTransform.localRotation = Quaternion.Euler(headPitch, 0f, 0f);
        }

        if (driver.DriverCapTransform != null)
        {
            driver.DriverCapTransform.localRotation = Quaternion.Euler(-swing * 1.5f, 0f, 0f);
        }

        if (driver.DriverLeftArmTransform != null)
        {
            float leftArmPitch = isConversing
                ? 10f + Mathf.Sin(driver.WalkAnimationTime * 1.9f + driver.DriverId * 0.4f) * 16f
                : swing * 28f;
            driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(leftArmPitch, 0f, 0f);
        }

        if (driver.DriverRightArmTransform != null)
        {
            float carryOffset = driver.DriverFuelCanTransform != null && driver.DriverFuelCanTransform.gameObject.activeSelf ? 18f : 0f;
            float rightArmPitch = isConversing
                ? -12f + Mathf.Sin(driver.WalkAnimationTime * 2.1f + driver.DriverId * 0.7f + 1.3f) * 20f
                : -swing * 28f - carryOffset;
            driver.DriverRightArmTransform.localRotation = Quaternion.Euler(rightArmPitch, 0f, 0f);
        }

        if (driver.DriverLeftLegTransform != null)
        {
            driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(isConversing ? 3f : -swing * 24f, 0f, 0f);
        }

        if (driver.DriverRightLegTransform != null)
        {
            driver.DriverRightLegTransform.localRotation = Quaternion.Euler(isConversing ? -3f : swing * 24f, 0f, 0f);
        }

        if (driver.DriverFuelCanTransform != null && driver.DriverFuelCanTransform.gameObject.activeSelf)
        {
            driver.DriverFuelCanTransform.localPosition = new Vector3(0.2f, 0.4f - bob * 0.2f, 0.04f);
            driver.DriverFuelCanTransform.localRotation = Quaternion.Euler(0f, 0f, -10f + swing * 6f);
        }
        else if (driver.DriverFuelCanTransform != null)
        {
            driver.DriverFuelCanTransform.localPosition = new Vector3(0.18f, 0.42f, 0f);
            driver.DriverFuelCanTransform.localRotation = Quaternion.identity;
        }

        if (driver.DriverFlashlightTransform != null)
        {
            driver.DriverFlashlightTransform.localPosition = new Vector3(0.24f, 0.57f - bob * 0.12f, 0.1f);
            driver.DriverFlashlightTransform.localRotation = Quaternion.Euler(16f + swing * 10f, swing * 5f, 0f);
        }
    }

    private void ResolveWorkerGamblingSpinResult(DriverAgent driver)
    {
        bool isGambler = HasWorkerPerk(driver, WorkerPerkKind.Gambler);
        var rng = new System.Random();

        int casinoBank = locations.TryGetValue(LocationType.GamblingHall, out LocationData gh) ? gh.BuildingBank : 0;

        int minBet = WorkerGamblingMinBet;
        int maxBet;
        if (isGambler)
        {
            // Gambler goes broke if can't cover minimum bet or casino has no funds
            if (driver.Money < minBet || casinoBank < minBet)
            {
                driver.GamblerBroke = true;
                driver.GamblingBet = 0;
                driver.GamblingPayout = 0;
                driver.GamblingMultiplier = 0;
                driver.GamblingMoneyPending = false;
                SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} [GAMBLER] is broke (money=${driver.Money}, casinoBank=${casinoBank}) - skipping bet.");
                return;
            }
            driver.GamblerBroke = false;
            // Bet full balance, capped by casino bank; 1.5x cap if lost last time (double-down)
            maxBet = Mathf.Min(driver.Money, casinoBank);
            if (driver.GamblerLostLastTime)
                maxBet = Mathf.Min(driver.Money, Mathf.RoundToInt(maxBet * 1.5f));
            maxBet = Mathf.Max(minBet, maxBet);
        }
        else
        {
            maxBet = Mathf.Clamp(driver.Money, WorkerGamblingMinBet, WorkerGamblingMaxBet);
        }

        int bet = rng.Next(minBet, maxBet + 1);
        float roll = (float)rng.NextDouble();
        int multiplier = roll < 0.55f ? 0 : roll < 0.85f ? 1 : roll < 0.97f ? 5 : 10;

        int payout;
        if (isGambler)
        {
            payout = multiplier switch
            {
                10 => bet * 12,
                5  => bet * 6,
                1  => bet,
                _  => Mathf.RoundToInt(bet * 0.2f)  // lose only 80%
            };
            driver.GamblerLostLastTime = (multiplier == 0);
        }
        else
        {
            payout = bet * multiplier;
        }

        int net = payout - bet;
        driver.GamblingBet          = bet;
        driver.GamblingPayout       = payout;
        driver.GamblingMultiplier   = multiplier;
        driver.GamblingMoneyPending = true;
        driver.GamblingBetCount++;

        if (multiplier > 0)
            ApplyWorkerLuckyEffect(driver);

        string outcomeStr = multiplier == 0 ? "LOSS" : $"WIN x{multiplier}";
        string gamblerTag = isGambler ? $" [GAMBLER bet#{driver.GamblingBetCount}]" : "";
        SessionDebugLogger.Log("NEEDS", $"{driver.DriverName}{gamblerTag} gambling; bet=${bet}, roll={roll:0.00}, outcome={outcomeStr}, payout=${payout}, net={net:+#;-#;0}, balance pending=${driver.Money}; need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Leisure)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
    }

}

