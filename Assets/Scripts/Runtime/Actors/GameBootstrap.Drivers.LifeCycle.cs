using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private void UpdateWorkerLifeCycleDailyState(DriverAgent driver)
    {
        if (driver == null) return;

        int hour = GetCurrentHour();
        if (driver.LifeCycleLastHour >= 0 && hour < driver.LifeCycleLastHour)
        {
            driver.NeedsCycleResetPending = true;
            SessionDebugLogger.Log("LIFE", $"{driver.DriverName} queued a new daily life cycle; currentGoal={driver.LifeGoal}, rest={driver.RestPhase}, needs={FormatWorkerNeedsDebug(driver)}.");
            LogWorkerDecision(driver, "daily-cycle-queued", "clock wrapped to a new day", true);
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
        LogWorkerDecision(driver, "daily-cycle-started", "worker became free enough to reset daily helper flags", true);
    }

    private bool TryStartDueWorkerLifeCycle(DriverAgent driver)
    {
        if (!IsWorkerEligibleForFreeLifeCycle(driver, out string blockedReason))
        {
            LogWorkerDecision(driver, "skip-due-life-cycle", blockedReason);
            return false;
        }

        int hour = GetCurrentHour();
        bool hasDueNeed = IsWorkerNeedActionReady(driver, WorkerNeedKind.Meal) ||
                          IsWorkerNeedActionReady(driver, WorkerNeedKind.Leisure) ||
                          IsWorkerNeedActionReady(driver, WorkerNeedKind.Sleep);
        if (!hasDueNeed)
        {
            LogWorkerDecision(driver, "skip-due-life-cycle", $"hour={hour}, hasDueNeed=False, retry meal={driver.NextMealRetryAtWorldHour:0.0}, leisure={driver.NextLeisureRetryAtWorldHour:0.0}, sleep={driver.NextSleepRetryAtWorldHour:0.0}, flags eat={driver.AteToday}, leisure={driver.HadLeisureToday}, sleep={driver.SleptToday}");
            return false;
        }

        bool unemployed = driver.DutyMode == DriverDutyMode.Local &&
                          driver.ShiftStartHour < 0 &&
                          driver.AssignedTruckNumber <= 0 &&
                          !IsDriverBusDriver(driver) &&
                          !IsDriverIntercity(driver);
        if (unemployed)
        {
            driver.WorkedToday = true;
        }

        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} starting free-time life cycle; hour={hour}, unemployed={unemployed}, dueNeeds={FormatWorkerNeedsDebug(driver)}.");
        LogWorkerDecision(driver, "start-life-cycle", "free/off-shift worker has due needs", true);
        return ContinueWorkerLifeCycle(driver, driver.DriverObject.transform.position);
    }

    private bool IsWorkerEligibleForFreeLifeCycle(DriverAgent driver, out string blockedReason)
    {
        blockedReason = "eligible";
        if (driver == null)
        {
            blockedReason = "worker is null";
            return false;
        }

        if (driver.DriverObject == null)
        {
            blockedReason = "worker visual is missing";
            return false;
        }

        if (driver.IsArrivingByBus)
        {
            blockedReason = "worker is still arriving by bus";
            return false;
        }

        if (driver.RestPhase != DriverRestPhase.None)
        {
            blockedReason = $"worker is resting ({driver.RestPhase})";
            return false;
        }

        if (driver.IsOnActiveShift || driver.IsInsideBuilding)
        {
            blockedReason = "worker is currently on active shift";
            return false;
        }

        if (driver.NeedsShiftEndReturn || driver.WaitingForShiftAtParking)
        {
            blockedReason = "worker is in shift handoff/parking wait";
            return false;
        }

        if (IsDriverOnActiveTradeRun(driver) || GetCurrentTruckForDriver(driver) != null)
        {
            blockedReason = "worker is on an active truck/trade route";
            return false;
        }

        if (IsDriverBusDriver(driver) && IsBusDriverOnActiveRoute(driver))
        {
            blockedReason = "worker is driving an active bus route";
            return false;
        }

        int hour = GetCurrentHour();
        if (driver.DutyMode == DriverDutyMode.Logistics &&
            (ShouldLogisticsWorkerHeadToBuilding(driver) || IsProductionWorkHour(hour)))
        {
            blockedReason = "logistics worker should be handled by production/logistics runtime";
            return false;
        }

        if (driver.ShiftStartHour >= 0 &&
            (ShouldDriverHeadToShift(driver) || IsHourInShiftWindow(hour, driver.ShiftStartHour)))
        {
            blockedReason = "worker should prepare for assigned shift";
            return false;
        }

        return true;
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
        LogWorkerDecision(driver, "completed-work", sourceLabel, true);
        if (!ContinueWorkerLifeCycle(driver, startPosition))
        {
            driver.IdleWanderPauseTimer = Random.Range(WorkerFreeIdleMinDuration, WorkerFreeIdleMaxDuration);
        }
    }

    private bool ContinueWorkerLifeCycle(DriverAgent driver, Vector3 startPosition)
    {
        if (driver == null || driver.DriverObject == null) return false;

        WorkerLifeGoal selectedGoalBefore = driver.LifeGoal;
        if (TryGetMostUrgentWorkerLifeGoal(driver, out WorkerLifeGoal urgentGoal, out WorkerNeedKind urgentNeed, out float urgencyScore) &&
            TryStartWorkerLifeGoal(driver, urgentGoal, startPosition))
        {
            LogWorkerDecision(driver, "life-goal-selected", $"{urgentNeed} need selected by urgency score={urgencyScore:0.0}", true, selectedGoalBefore, driver.LifeGoal);
            return true;
        }

        selectedGoalBefore = driver.LifeGoal;
        if (driver.OwnedCarModelIndex < 0 &&
            driver.Money >= CarPurchasePrice &&
            locations.ContainsKey(LocationType.CarMarket) &&
            TryStartWorkerBuyCar(driver, startPosition))
        {
            LogWorkerDecision(driver, "life-goal-selected", "Buying car", true, selectedGoalBefore, driver.LifeGoal);
            return true;
        }

        selectedGoalBefore = driver.LifeGoal;
        if (driver.AssignedPersonalHouseIndex < 0 &&
            driver.Money >= HousePurchasePrice &&
            TryStartWorkerBuyHouse(driver, startPosition))
        {
            LogWorkerDecision(driver, "life-goal-selected", "Buying personal house", true, selectedGoalBefore, driver.LifeGoal);
            return true;
        }

        driver.LifeGoal = WorkerLifeGoal.Idle;
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} has no due life goals; entering Idle. helperFlags work={driver.WorkedToday}, eat={driver.AteToday}, leisure={driver.HadLeisureToday}, sleep={driver.SleptToday}; needs={FormatWorkerNeedsDebug(driver)}.");
        LogWorkerDecision(driver, "enter-idle", "no due life goals after evaluation", true);
        return false;
    }

    private bool TryStartWorkerBuyHouse(DriverAgent driver, Vector3 startPosition)
    {
        int targetIndex = -1;
        for (int i = 0; i < personalHouses.Count; i++)
        {
            int residents = 0;
            foreach (DriverAgent d in driverAgents)
                if (d.AssignedPersonalHouseIndex == i) residents++;
            if (residents < MaxPersonalHouseResidents) { targetIndex = i; break; }
        }
        if (targetIndex < 0) return false;

        driver.AssignedPersonalHouseIndex = targetIndex;
        driver.LifeGoal = WorkerLifeGoal.BuyHouse;
        Vector3 target = GetDriverStandPointNearPersonalHouse(targetIndex);
        ResetWorkerLocalBusTripState(driver);
        if (TryStartWorkerLocalBusTrip(driver, startPosition, target, DriverRescuePhase.ToPersonalHouseForPurchase, "House purchase"))
        {
            LogWorkerDecision(driver, "buy-house-via-bus", $"House #{targetIndex}, fee=${HousePurchasePrice}", true);
            return true;
        }
        driver.WalkTargetWorld = target;
        driver.WalkPhase = DriverRescuePhase.ToPersonalHouseForPurchase;
        driver.WalkAnimationTime = 0f;
        BuildDriverWalkPath(driver, startPosition, target);
        LogWorkerDecision(driver, "buy-house-walk", $"House #{targetIndex}, fee=${HousePurchasePrice}", true);
        return true;
    }

    private bool TryStartWorkerBuyCar(DriverAgent driver, Vector3 startPosition)
    {
        if (!locations.ContainsKey(LocationType.CarMarket))
        {
            return false;
        }

        driver.LifeGoal = WorkerLifeGoal.BuyCar;
        Vector3 target = GetDriverStandPointNearLocation(LocationType.CarMarket);
        ResetWorkerLocalBusTripState(driver);
        if (TryStartWorkerLocalBusTrip(driver, startPosition, target, DriverRescuePhase.ToCarMarketForPurchase, "Car purchase"))
        {
            LogWorkerDecision(driver, "buy-car-via-bus", $"Car Market, fee=${CarPurchasePrice}", true);
            return true;
        }

        driver.WalkTargetWorld = target;
        driver.WalkPhase = DriverRescuePhase.ToCarMarketForPurchase;
        driver.WalkAnimationTime = 0f;
        BuildDriverWalkPath(driver, startPosition, target);
        LogWorkerDecision(driver, "buy-car-walk", $"Car Market, fee=${CarPurchasePrice}", true);
        return true;
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
                string mealUnavailableReason = GetWorkerServiceUnavailableReason(driver, LocationType.Canteen);
                SessionDebugLogger.Log("LIFE", $"{driver.DriverName} skipped Canteen today; reason={mealUnavailableReason}; need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Meal)}; snapshot={FormatWorkerNeedsDebug(driver)}.");
                LogWorkerDecision(driver, "skip-meal-service", mealUnavailableReason, true);
                if (IsCanteenBlockedByMoney(driver, mealUnavailableReason) && TryStartWorkerTrashCanMealFallback(driver, startPosition, mealUnavailableReason))
                {
                    return true;
                }
                if (TryStartWorkerNeedFallback(driver, WorkerNeedKind.Meal, startPosition, mealUnavailableReason))
                {
                    return true;
                }
                SetWorkerNeedRetryCooldown(driver, WorkerNeedKind.Meal, mealUnavailableReason);
                return ContinueWorkerLifeCycle(driver, startPosition);

            case WorkerLifeGoal.Leisure:
                if (TryStartWeightedLeisureGoal(driver, startPosition))
                    return true;
                if (TryStartWorkerNeedFallback(driver, WorkerNeedKind.Leisure, startPosition, "no paid/free leisure service could be started"))
                {
                    return true;
                }
                StartWorkerFreeIdle(driver, startPosition, "leisure fallback");
                LogWorkerDecision(driver, "fallback-leisure", "no paid/free leisure service could be started; using free idle", true);
                return true;

            case WorkerLifeGoal.Sleep:
                if (TryStartWorkerSleep(driver, startPosition))
                {
                    return true;
                }
                SessionDebugLogger.Log("LIFE", $"{driver.DriverName} skipped Motel sleep today; reason={GetWorkerServiceUnavailableReason(driver, LocationType.Motel)}; need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Sleep)}; snapshot={FormatWorkerNeedsDebug(driver)}.");
                LogWorkerDecision(driver, "skip-sleep-service", GetWorkerServiceUnavailableReason(driver, LocationType.Motel), true);
                if (TryStartWorkerNeedFallback(driver, WorkerNeedKind.Sleep, startPosition, GetWorkerServiceUnavailableReason(driver, LocationType.Motel)))
                {
                    return true;
                }
                SetWorkerNeedRetryCooldown(driver, WorkerNeedKind.Sleep, GetWorkerServiceUnavailableReason(driver, LocationType.Motel));
                return ContinueWorkerLifeCycle(driver, startPosition);
        }

        return false;
    }

    private bool IsCanteenBlockedByMoney(DriverAgent driver, string reason)
    {
        return driver != null &&
               !string.IsNullOrEmpty(reason) &&
               IsWorkerServiceBlockedByMoney(reason) &&
               locations.TryGetValue(LocationType.Canteen, out LocationData canteen) &&
               canteen.FoodStored > 0;
    }

    private static bool IsWorkerServiceBlockedByMoney(string reason)
    {
        return !string.IsNullOrEmpty(reason) && reason.Contains("not enough money");
    }

    private bool TryStartWorkerTrashCanMealFallback(DriverAgent driver, Vector3 startPosition, string reason)
    {
        if (driver == null || locationTrashCanMealTargets.Count == 0)
        {
            LogWorkerDecision(driver, "trash-meal-unavailable", $"no trash cans registered; reason={reason}", true);
            return false;
        }

        if (!TryGetNearestTrashCanMealTarget(startPosition, out Vector3 target))
        {
            LogWorkerDecision(driver, "trash-meal-unavailable", $"no reachable trash can; reason={reason}", true);
            return false;
        }

        driver.LifeGoal = WorkerLifeGoal.Eat;
        driver.IdleActivityTimer = Mathf.Max(1.2f, WorkerCanteenDuration * 0.55f);
        driver.WalkTargetWorld = target;
        driver.WalkPhase = DriverRescuePhase.IdleWalkToTrashCan;
        driver.WalkAnimationTime = 0f;
        ResetWorkerLocalBusTripState(driver);
        BuildDriverWalkPath(driver, startPosition, target);
        SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} cannot afford Canteen and is heading to a trash can meal; reason={reason}; target=({target.x:0.0},{target.z:0.0}); need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Meal)}.");
        LogWorkerDecision(driver, "trash-meal-fallback", $"reason={reason}; target=({target.x:0.0},{target.z:0.0})", true);
        return true;
    }

    private bool TryGetNearestTrashCanMealTarget(Vector3 startPosition, out Vector3 target)
    {
        target = Vector3.zero;
        float bestScore = float.PositiveInfinity;
        Vector2Int startCell = WorldToCell(startPosition);
        for (int i = 0; i < locationTrashCanMealTargets.Count; i++)
        {
            Vector3 candidate = locationTrashCanMealTargets[i];
            Vector2Int goalCell = WorldToCell(candidate);
            if (!IsInsideGrid(goalCell) || waterCells.Contains(goalCell) || edgeHighwayCells.Contains(goalCell))
            {
                continue;
            }

            List<Vector2Int> path = FindDriverWalkPath(startCell, goalCell, DriverRescuePhase.IdleWalkToTrashCan);
            if (path == null || path.Count == 0)
            {
                continue;
            }

            float score = path.Count + (candidate - startPosition).sqrMagnitude * 0.01f;
            if (score < bestScore)
            {
                bestScore = score;
                target = candidate;
            }
        }

        return bestScore < float.PositiveInfinity;
    }

    private bool TryGetMostUrgentWorkerLifeGoal(DriverAgent driver, out WorkerLifeGoal goal, out WorkerNeedKind need, out float score)
    {
        goal = WorkerLifeGoal.None;
        need = WorkerNeedKind.Meal;
        score = 0f;

        ConsiderWorkerNeedGoal(driver, WorkerLifeGoal.Eat, WorkerNeedKind.Meal, ref goal, ref need, ref score);
        ConsiderWorkerNeedGoal(driver, WorkerLifeGoal.Sleep, WorkerNeedKind.Sleep, ref goal, ref need, ref score);
        ConsiderWorkerNeedGoal(driver, WorkerLifeGoal.Leisure, WorkerNeedKind.Leisure, ref goal, ref need, ref score);
        return goal != WorkerLifeGoal.None;
    }

    private void ConsiderWorkerNeedGoal(DriverAgent driver, WorkerLifeGoal candidateGoal, WorkerNeedKind candidateNeed, ref WorkerLifeGoal bestGoal, ref WorkerNeedKind bestNeed, ref float bestScore)
    {
        if (!IsWorkerNeedActionReady(driver, candidateNeed))
        {
            return;
        }

        float candidateScore = GetWorkerNeedUrgencyScore(driver, candidateNeed);
        if (candidateScore > bestScore)
        {
            bestGoal = candidateGoal;
            bestNeed = candidateNeed;
            bestScore = candidateScore;
        }
    }

    private bool IsWorkerNeedActionReady(DriverAgent driver, WorkerNeedKind need)
    {
        if (driver == null || !IsWorkerNeedRetryReady(driver, need))
        {
            return false;
        }

        return need switch
        {
            WorkerNeedKind.Meal => ShouldWorkerSeekMeal(driver),
            WorkerNeedKind.Sleep => ShouldWorkerSeekSleep(driver),
            WorkerNeedKind.Leisure => ShouldWorkerSeekLeisure(driver),
            _ => false
        };
    }

    private float GetWorkerNeedUrgencyScore(DriverAgent driver, WorkerNeedKind need)
    {
        float hours = GetWorkerNeedHours(driver, need);
        float seek = need switch
        {
            WorkerNeedKind.Meal => WorkerMealSeekHours,
            WorkerNeedKind.Sleep => WorkerSleepSeekHours,
            WorkerNeedKind.Leisure => WorkerLeisureSeekHours,
            _ => 0f
        };
        WorkerNeedStatus status = GetWorkerNeedLastStatus(driver, need);
        float statusBonus = status switch
        {
            WorkerNeedStatus.Critical => 100f,
            WorkerNeedStatus.Warning => 35f,
            _ => 0f
        };

        float needBias = need switch
        {
            WorkerNeedKind.Sleep => 8f,
            WorkerNeedKind.Meal => 5f,
            _ => 0f
        };
        return Mathf.Max(0f, hours - seek) + statusBonus + needBias;
    }

    private bool HasCriticalWorkerNeed(DriverAgent driver)
    {
        return driver != null &&
               (driver.LastMealNeedStatus == WorkerNeedStatus.Critical ||
                driver.LastSleepNeedStatus == WorkerNeedStatus.Critical ||
                driver.LastLeisureNeedStatus == WorkerNeedStatus.Critical);
    }

    private bool TryStartWorkerNeedFallback(DriverAgent driver, WorkerNeedKind need, Vector3 startPosition, string reason)
    {
        if (driver == null || !IsWorkerNeedRetryReady(driver, need))
        {
            return false;
        }

        WorkerLifeGoal goal = need switch
        {
            WorkerNeedKind.Meal => WorkerLifeGoal.Eat,
            WorkerNeedKind.Sleep => WorkerLifeGoal.Sleep,
            WorkerNeedKind.Leisure => WorkerLifeGoal.Leisure,
            _ => WorkerLifeGoal.Idle
        };

        driver.LifeGoal = goal;
        driver.IdleActivityTimer = need == WorkerNeedKind.Sleep
            ? DayNightCycleDuration / 24f * 2.5f
            : DayNightCycleDuration / 24f * 0.75f;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;

        if (need == WorkerNeedKind.Sleep && TryGetNearestFreeBench(startPosition, 18f, out int benchIndex, out Vector3 benchPosition))
        {
            MarkRoadsideBenchOccupied(benchIndex);
            driver.SittingBenchIndex = benchIndex;
            driver.WalkTargetWorld = benchPosition;
            BuildDriverWalkPath(driver, startPosition, benchPosition);
            driver.WalkPhase = DriverRescuePhase.IdleWalkToBench;
            if (IsWorkerServiceBlockedByMoney(reason))
            {
                ApplyWorkerMoneyFallbackEffect(driver);
                SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} cannot afford Motel and is heading to bench sleep fallback; reason={reason}; bench={benchIndex}; need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Sleep)}.");
            }
            LogWorkerDecision(driver, "need-fallback", $"{need}: emergency bench rest; reason={reason}", true);
            return true;
        }

        if (TryGetCityIdleWanderTarget(driver, startPosition, driver.IdleWanderPointIndex + 3, out Vector3 fallbackTarget))
        {
            driver.WalkTargetWorld = fallbackTarget;
            BuildDriverWalkPath(driver, startPosition, fallbackTarget);
            driver.WalkPhase = DriverRescuePhase.IdleWander;
            LogWorkerDecision(driver, "need-fallback-walk", $"{need}: walking to free city fallback; reason={reason}", true);
            return true;
        }

        driver.WalkPhase = DriverRescuePhase.IdlePhoneCall;
        LogWorkerDecision(driver, "need-fallback", $"{need}: immediate free fallback; reason={reason}", true);
        return true;
    }

    private bool TryStartWeightedLeisureGoal(DriverAgent driver, Vector3 startPosition)
    {
        bool isAlcoholic = HasWorkerPerk(driver, WorkerPerkKind.Alcoholism);
        bool isGambler = HasWorkerPerk(driver, WorkerPerkKind.Gambler);

        // Alcoholism remains a strong behavioral rule: if a working Bar exists, it wins first.
        if (isAlcoholic &&
            TryStartWorkerServiceVisit(driver, LocationType.Bar, WorkerLifeGoal.Leisure, DriverRescuePhase.IdleWalkToBar, WorkerLeisureDuration, startPosition))
        {
            SessionDebugLogger.Log("LIFE", $"{driver.DriverName} selected Bar for Leisure due to Alcoholism.");
            return true;
        }

        List<LocationType> weightedChoices = new();
        if (CanWorkerConsiderLeisureService(driver, LocationType.CityPark))
        {
            AddWeightedLeisureChoice(weightedChoices, LocationType.CityPark, isGambler ? 3 : 5);
        }
        if (CanWorkerConsiderLeisureService(driver, LocationType.GamblingHall))
        {
            AddWeightedLeisureChoice(weightedChoices, LocationType.GamblingHall, isGambler ? 7 : 3);
        }
        if (CanWorkerConsiderLeisureService(driver, LocationType.Bar))
        {
            AddWeightedLeisureChoice(weightedChoices, LocationType.Bar, isGambler ? 1 : 2);
        }

        if (weightedChoices.Count == 0)
        {
            LogWorkerDecision(driver, "leisure-no-candidates", "no available Bar/GamblingHall/CityPark candidates", true);
            return false;
        }

        int pickedIndex = Random.Range(0, weightedChoices.Count);
        LocationType picked = weightedChoices[pickedIndex];
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} rolled leisure target {picked}; candidates={weightedChoices.Count}, gambler={isGambler}, alcoholic={isAlcoholic}.");
        if (TryStartLeisureServiceVisit(driver, picked, startPosition))
        {
            return true;
        }

        // If the weighted pick became unavailable between selection and start, try the remaining services once.
        LocationType[] fallbackOrder = { LocationType.CityPark, LocationType.GamblingHall, LocationType.Bar };
        foreach (LocationType fallback in fallbackOrder)
        {
            if (fallback == picked || !CanWorkerConsiderLeisureService(driver, fallback))
            {
                continue;
            }

            if (TryStartLeisureServiceVisit(driver, fallback, startPosition))
            {
                SessionDebugLogger.Log("LIFE", $"{driver.DriverName} switched leisure target from {picked} to {fallback}.");
                return true;
            }
        }

        return false;
    }

    private static void AddWeightedLeisureChoice(List<LocationType> choices, LocationType type, int weight)
    {
        for (int i = 0; i < weight; i++)
        {
            choices.Add(type);
        }
    }

    private bool CanWorkerConsiderLeisureService(DriverAgent driver, LocationType type)
    {
        if (driver == null || !locations.TryGetValue(type, out LocationData service))
        {
            return false;
        }

        if (driver.Money < service.ServiceFee)
        {
            return false;
        }

        if (type == LocationType.Bar && service.AlcoholStored <= 0)
        {
            return false;
        }

        if (type == LocationType.GamblingHall && !HasWorkerPerk(driver, WorkerPerkKind.Gambler) && driver.Money < WorkerGamblingMinBalance)
        {
            return false;
        }

        return true;
    }

    private bool TryStartLeisureServiceVisit(DriverAgent driver, LocationType type, Vector3 startPosition)
    {
        return type switch
        {
            LocationType.Bar => TryStartWorkerServiceVisit(driver, LocationType.Bar, WorkerLifeGoal.Leisure, DriverRescuePhase.IdleWalkToBar, WorkerLeisureDuration, startPosition),
            LocationType.GamblingHall => TryStartWorkerServiceVisit(driver, LocationType.GamblingHall, WorkerLifeGoal.Leisure, DriverRescuePhase.IdleWalkToGamblingHall, WorkerGamblingHallDuration, startPosition),
            LocationType.CityPark => TryStartWorkerServiceVisit(driver, LocationType.CityPark, WorkerLifeGoal.Leisure, DriverRescuePhase.IdleWalkToCityPark, WorkerCityParkDuration, startPosition),
            _ => false
        };
    }

    private bool TryStartWorkerServiceVisit(DriverAgent driver, LocationType type, WorkerLifeGoal goal, DriverRescuePhase walkPhase, float duration, Vector3 startPosition)
    {
        if (driver == null || !locations.TryGetValue(type, out LocationData service))
        {
            LogWorkerDecision(driver, "service-unavailable", $"{type} not built", true);
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
            LogWorkerDecision(driver, "service-unavailable", $"{type}: {GetWorkerServiceUnavailableReason(driver, type)}", true);
            return false;
        }

        Vector3 target;
        if (type == LocationType.CityPark)
        {
            target = GetNearestCityParkEntranceTarget(service, startPosition);
        }
        else
        {
            float x = (service.Min.x + service.Max.x + 1) * 0.5f;
            float z = (service.Min.y + service.Max.y + 1) * 0.5f;
            target = new(x + Random.Range(-0.2f, 0.2f), 0f, z + Random.Range(-0.2f, 0.2f));
        }
        driver.LifeGoal = goal;
        driver.IdleActivityTimer = duration;
        ResetWorkerLocalBusTripState(driver);
        if (TryStartWorkerLocalBusTrip(driver, startPosition, target, walkPhase, $"{type} visit"))
        {
            LogWorkerDecision(driver, "service-visit-via-bus", $"{type} for {goal}; fee=${service.ServiceFee}; duration={duration:0.0}s", true);
            return true;
        }

        driver.WalkTargetWorld = target;
        driver.WalkPhase = walkPhase;
        driver.WalkAnimationTime = 0f;
        BuildDriverWalkPath(driver, startPosition, target);
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} heading to {type} for {goal}; serviceFee=${service.ServiceFee}, need={FormatWorkerNeedDebug(driver, goal == WorkerLifeGoal.Eat ? WorkerNeedKind.Meal : WorkerNeedKind.Leisure)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
        LogWorkerDecision(driver, "service-visit-walk", $"{type} for {goal}; fee=${service.ServiceFee}; duration={duration:0.0}s", true);
        return true;
    }

    private Vector3 GetNearestCityParkEntranceTarget(LocationData park, Vector3 startPosition)
    {
        float centerX = (park.Min.x + park.Max.x + 1) * 0.5f;
        float centerZ = (park.Min.y + park.Max.y + 1) * 0.5f;
        float jitter = Random.Range(-1.15f, 1.15f);
        Vector3[] candidates =
        {
            new(centerX + jitter, 0f, park.Min.y - 0.55f),
            new(centerX - jitter, 0f, park.Max.y + 1.55f),
            new(park.Min.x - 0.55f, 0f, centerZ - jitter),
            new(park.Max.x + 1.55f, 0f, centerZ + jitter)
        };

        Vector3 best = candidates[0];
        float bestScore = float.PositiveInfinity;
        Vector2Int startCell = WorldToCell(startPosition);
        for (int i = 0; i < candidates.Length; i++)
        {
            Vector3 candidate = candidates[i];
            candidate.y = SampleTerrainHeight(candidate.x, candidate.z);
            Vector2Int goalCell = WorldToCell(candidate);
            List<Vector2Int> path = FindDriverWalkPath(startCell, goalCell, DriverRescuePhase.IdleWalkToCityPark);
            float pathPenalty = path == null || path.Count == 0 ? 10000f : path.Count;
            float score = pathPenalty + (candidate - startPosition).sqrMagnitude * 0.01f;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    private bool TryStartWorkerSleep(DriverAgent driver, Vector3 startPosition)
    {
        if (driver.AssignedPersonalHouseIndex >= 0 && driver.AssignedPersonalHouseIndex < personalHouses.Count)
            return TryStartWorkerSleepAtHome(driver, startPosition);

        if (driver == null || !locations.TryGetValue(LocationType.Motel, out LocationData motel) || driver.Money < motel.ServiceFee)
        {
            LogWorkerDecision(driver, "sleep-unavailable", GetWorkerServiceUnavailableReason(driver, LocationType.Motel), true);
            return false;
        }

        driver.LifeGoal = WorkerLifeGoal.Sleep;
        ResetWorkerLocalBusTripState(driver);
        if (TryStartWorkerLocalBusTrip(driver, startPosition, GetDriverStandPointNearLocation(LocationType.Motel), DriverRescuePhase.ToMotelEntrance, "Motel sleep"))
        {
            driver.RestPhase = DriverRestPhase.DriverWalkToMotel;
            LogWorkerDecision(driver, "sleep-via-bus", $"Motel fee=${motel.ServiceFee}", true);
            return true;
        }

        driver.WalkTargetWorld = GetDriverStandPointNearLocation(LocationType.Motel);
        driver.WalkPhase = DriverRescuePhase.ToMotelEntrance;
        driver.RestPhase = DriverRestPhase.DriverWalkToMotel;
        driver.WalkAnimationTime = 0f;
        BuildDriverWalkPath(driver, startPosition, driver.WalkTargetWorld);
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} heading to Motel for SLEEP; serviceFee=${motel.ServiceFee}, need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Sleep)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
        LogWorkerDecision(driver, "sleep-walk", $"Motel fee=${motel.ServiceFee}", true);
        return true;
    }

    private bool TryStartWorkerSleepAtHome(DriverAgent driver, Vector3 startPosition)
    {
        driver.LifeGoal = WorkerLifeGoal.Sleep;
        driver.RestPhase = DriverRestPhase.DriverWalkToMotel;
        Vector3 target = GetDriverStandPointNearPersonalHouse(driver.AssignedPersonalHouseIndex);
        ResetWorkerLocalBusTripState(driver);
        if (TryStartWorkerLocalBusTrip(driver, startPosition, target, DriverRescuePhase.ToPersonalHouseEntrance, "Home sleep"))
        {
            LogWorkerDecision(driver, "sleep-home-via-bus", $"House #{driver.AssignedPersonalHouseIndex}", true);
            return true;
        }
        driver.WalkTargetWorld = target;
        driver.WalkPhase = DriverRescuePhase.ToPersonalHouseEntrance;
        driver.WalkAnimationTime = 0f;
        BuildDriverWalkPath(driver, startPosition, target);
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} heading home to sleep; house=#{driver.AssignedPersonalHouseIndex}, need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Sleep)}.");
        LogWorkerDecision(driver, "sleep-home-walk", $"House #{driver.AssignedPersonalHouseIndex}", true);
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
        LogWorkerDecision(driver, "free-idle", reason, true);
    }

    private bool TryStartCityParkPromenade(DriverAgent driver, Vector3 currentPosition)
    {
        if (driver == null || driver.CityParkPromenadeStep > 0 || !locations.TryGetValue(LocationType.CityPark, out LocationData park))
        {
            return false;
        }

        driver.CityParkPromenadeStep = 1;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;

        Vector3 center = GetLocationCenter(park);
        center.y = SampleTerrainHeight(center.x, center.z);
        int activityStyle = Random.Range(0, 5);
        Vector3 strollPoint = GetCityParkPromenadePoint(park, driver.DriverId, activityStyle);

        if (activityStyle == 1 && TryGetFreeCityParkBench(out int benchIndex, out Vector3 benchPosition))
        {
            driver.CityParkBenchIndex = benchIndex;
            driver.CityParkActivityStyle = 1;
            driver.WalkPath.Add(center);
            driver.WalkPath.Add(benchPosition);
            driver.WalkTargetWorld = benchPosition;
            driver.IdleActivityTimer = WorkerCityParkDuration * Random.Range(0.85f, 1.25f);
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} entered City Park and is walking to park bench {benchIndex}.");
            LogWorkerDecision(driver, "city-park-promenade", $"bench={benchIndex}", true);
            return true;
        }

        driver.CityParkActivityStyle = activityStyle;
        driver.WalkPath.Add(center);
        driver.WalkPath.Add(strollPoint);
        driver.WalkTargetWorld = strollPoint;
        driver.IdleActivityTimer = WorkerCityParkDuration * Random.Range(0.70f, 1.20f);
        string activityLabel = GetCityParkActivityLabel(activityStyle);
        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} entered City Park and started activity={activityLabel}.");
        LogWorkerDecision(driver, "city-park-promenade", activityLabel, true);
        return true;
    }

    private Vector3 GetCityParkPromenadePoint(LocationData park, int seed, int activityStyle)
    {
        float cx = (park.Min.x + park.Max.x + 1) * 0.5f;
        float cz = (park.Min.y + park.Max.y + 1) * 0.5f;
        Vector3[] offsets =
        {
            new(-2.0f, 0f, 0.0f),
            new( 2.0f, 0f, 0.0f),
            new( 0.0f, 0f,-2.0f),
            new( 0.0f, 0f, 2.0f),
            new(-1.2f, 0f, 1.2f),
            new( 1.2f, 0f,-1.2f)
        };

        Vector3 target = new(cx, 0f, cz);
        target += offsets[Mathf.Abs(seed + activityStyle * 17 + Mathf.RoundToInt(Time.time * 10f)) % offsets.Length];
        target.y = SampleTerrainHeight(target.x, target.z);
        return target;
    }

    private static string GetCityParkActivityLabel(int activityStyle)
    {
        return activityStyle switch
        {
            1 => "bench rest",
            2 => "stretching",
            3 => "watching trees",
            4 => "quiet picnic",
            _ => "promenade"
        };
    }

    private bool TryStartCityParkExit(DriverAgent driver, Vector3 currentPosition)
    {
        if (driver == null || !locations.TryGetValue(LocationType.CityPark, out LocationData park))
        {
            return false;
        }

        ReleaseBench(driver);
        driver.CityParkPromenadeStep = 2;
        driver.WalkPhase = DriverRescuePhase.IdleExitCityPark;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;
        Vector3 exitPoint = GetNearestCityParkEntranceTarget(park, currentPosition);
        driver.WalkPath.Add(exitPoint);
        driver.WalkTargetWorld = exitPoint;
        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} finished City Park activity and is walking out via nearest entrance.");
        LogWorkerDecision(driver, "city-park-exit", "walking to park entrance", true);
        return true;
    }

    private void CompleteCityParkLeisure(DriverAgent driver, Vector3 currentPosition)
    {
        driver.CityParkPromenadeStep = 0;
        driver.CityParkActivityStyle = 0;
        driver.WalkPhase = DriverRescuePhase.None;
        ResetWorkerNeedTimer(driver, WorkerNeedKind.Leisure);
        driver.HadLeisureToday = true;
        driver.LifeGoal = WorkerLifeGoal.None;
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} completed City Park leisure after exiting; snapshot={FormatWorkerNeedsDebug(driver)}.");
        ContinueWorkerLifeCycle(driver, currentPosition);
    }

    private bool TryGetFreeCityParkBench(out int idx, out Vector3 pos)
    {
        int count = Mathf.Min(cityParkBenchPositions.Count, cityParkBenchOccupied.Length);
        int start = count > 0 ? Random.Range(0, count) : 0;
        for (int i = 0; i < count; i++)
        {
            int candidate = (start + i) % count;
            if (cityParkBenchOccupied[candidate])
            {
                continue;
            }

            cityParkBenchOccupied[candidate] = true;
            idx = candidate;
            pos = cityParkBenchPositions[candidate];
            pos.y = SampleTerrainHeight(pos.x, pos.z);
            return true;
        }

        idx = -1;
        pos = Vector3.zero;
        return false;
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
            driver.NeedsShiftEndReturn ||
            driver.WaitingForShiftAtParking ||
            GetCurrentTruckForDriver(driver) != null)
        {
            if (IsDriverIdleWanderPhase(driver) || IsDriverInIdleActivity(driver))
            {
                ReleaseBench(driver);
                ReleaseCatInteraction(driver);
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
            }

            driver.IdleConversationTimer = 0f;
            driver.IdleConversationPartnerId = -1;

            LogWorkerDecision(driver, "idle-blocked", "worker is arriving/resting/on shift/returning/waiting/assigned to truck");
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

        if (HasCriticalWorkerNeed(driver) && IsLowPriorityIdleActivity(driver) && TryInterruptIdleForCriticalNeed(driver))
        {
            return;
        }

        // Handle stationary idle activities
        if (driver.WalkPhase == DriverRescuePhase.IdleSittingOnBench ||
            driver.WalkPhase == DriverRescuePhase.IdleAtBar ||
            driver.WalkPhase == DriverRescuePhase.IdleAtCanteen ||
            driver.WalkPhase == DriverRescuePhase.IdleAtTrashCan ||
            driver.WalkPhase == DriverRescuePhase.IdleAtGamblingHall ||
            driver.WalkPhase == DriverRescuePhase.IdleAtCityPark ||
            driver.WalkPhase == DriverRescuePhase.IdleSmoking ||
            driver.WalkPhase == DriverRescuePhase.IdlePhoneCall ||
            driver.WalkPhase == DriverRescuePhase.IdlePettingCat)
        {
            driver.IdleActivityTimer -= Time.deltaTime * gameSpeedMultiplier;
            if (driver.IdleActivityTimer <= 0f)
            {
                WorkerLifeGoal completedGoal = driver.LifeGoal;
                DriverRescuePhase completedPhase = driver.WalkPhase;
                if (driver.WalkPhase == DriverRescuePhase.IdleSittingOnBench)
                {
                    ReleaseBench(driver);
                }
                else if (driver.WalkPhase == DriverRescuePhase.IdleAtCityPark)
                {
                    if (completedGoal == WorkerLifeGoal.Leisure && TryStartCityParkExit(driver, driver.DriverObject.transform.position))
                    {
                        return;
                    }

                    ReleaseBench(driver);
                }

                if (completedPhase == DriverRescuePhase.IdleSmoking)
                {
                    StopDriverSmokingParticles(driver);
                }

                if (completedPhase == DriverRescuePhase.IdlePettingCat)
                {
                    ReleaseCatInteraction(driver);
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
                    driver.GamblingBetCount = 0;
                    driver.GamblerBroke = false;
                    driver.LifeGoal = WorkerLifeGoal.None;
                    ContinueWorkerLifeCycle(driver, driver.DriverObject.transform.position);
                    return;
                }

                if (completedGoal == WorkerLifeGoal.Sleep)
                {
                    ResetWorkerNeedTimer(driver, WorkerNeedKind.Sleep);
                    driver.SleptToday = true;
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
            LogWorkerDecision(driver, "idle-blocked", "logistics worker should be handled by production/logistics runtime");
            return;
        }

        if (driver.ShiftStartHour >= 0 &&
            (ShouldDriverHeadToShift(driver) || IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour)))
        {
            LogWorkerDecision(driver, "idle-blocked", "worker should prepare for assigned shift");
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
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} started idle walk.");
            LogWorkerDecision(driver, "idle-activity", $"wander roll={roll:0.00}", true);
        }
        else if (roll < 0.75f && TryGetNearestFreeBench(startPosition, 14f, out int bIdx, out Vector3 bPos))
        {
            MarkRoadsideBenchOccupied(bIdx);
            driver.SittingBenchIndex = bIdx;
            driver.IdleActivityTimer = Random.Range(15f, 45f);
            driver.WalkTargetWorld = bPos;
            BuildDriverWalkPath(driver, startPosition, bPos);
            driver.WalkPhase = DriverRescuePhase.IdleWalkToBench;
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} heading to bench {bIdx}.");
            LogWorkerDecision(driver, "idle-activity", $"bench={bIdx}, roll={roll:0.00}", true);
        }
        else if (roll < 0.88f)
        {
            driver.IdleActivityTimer = Random.Range(20f, 45f);
            driver.WalkPhase = DriverRescuePhase.IdleSmoking;
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} started smoking break.");
            LogWorkerDecision(driver, "idle-activity", $"smoking, roll={roll:0.00}", true);
        }
        else if (roll < 0.97f && TryStartWorkerPetCat(driver, startPosition))
        {
            LogWorkerDecision(driver, "idle-activity", $"walking to pet cat, roll={roll:0.00}", true);
        }
        else
        {
            driver.IdleActivityTimer = Random.Range(8f, 25f);
            driver.WalkPhase = DriverRescuePhase.IdlePhoneCall;
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} making a phone call.");
            LogWorkerDecision(driver, "idle-activity", $"phone call, roll={roll:0.00}", true);
        }
    }

    private bool TryStartWorkerPetCat(DriverAgent driver, Vector3 startPosition)
    {
        if (ambientCats.Count == 0 || AreAmbientCatsSleepingNight()) return false;

        const float searchRadiusSqr = 49f; // 7 units
        int bestIndex = -1;
        float bestDistSqr = searchRadiusSqr;

        for (int i = 0; i < ambientCats.Count; i++)
        {
            AmbientCatData cat = ambientCats[i];
            if (cat == null || cat.RootTransform == null || cat.State == AmbientCatState.BeingPetted) continue;

            Vector3 delta = cat.RootTransform.position - startPosition;
            delta.y = 0f;
            float distSqr = delta.sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                bestIndex = i;
            }
        }

        if (bestIndex < 0) return false;

        AmbientCatData targetCat = ambientCats[bestIndex];
        Vector3 catPos = targetCat.RootTransform.position;
        catPos.y = SampleTerrainHeight(catPos.x, catPos.z);

        driver.IdleCatPetTargetIndex = bestIndex;
        driver.IdleActivityTimer = Random.Range(4f, 8f);
        driver.WalkAnimationTime = 0f;
        driver.WalkPhase = DriverRescuePhase.IdleWalkToCat;
        BuildDriverWalkPath(driver, startPosition, catPos);
        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} heading toward a cat to pet it.");
        return true;
    }

    private void ReleaseCatInteraction(DriverAgent driver)
    {
        if (driver == null) return;

        int idx = driver.IdleCatPetTargetIndex;
        if (idx >= 0 && idx < ambientCats.Count)
        {
            AmbientCatData cat = ambientCats[idx];
            if (cat != null && cat.PettedByDriverId == driver.DriverId)
            {
                cat.State = AmbientCatState.Lazing;
                cat.StateTimer = Random.Range(4f, 8f);
                cat.PettedByDriverId = -1;
            }
        }

        driver.IdleCatPetTargetIndex = -1;
    }

    private Vector3 FindDriverIdleWanderTarget(DriverAgent driver, Vector3 startPosition)
    {
        int nextPointIndex = driver.IdleWanderPointIndex + 1;
        float personalSpaceSqr = DriverIdlePersonalSpace * DriverIdlePersonalSpace;
        for (int attempt = 0; attempt < 16; attempt++)
        {
            int candidateIndex = nextPointIndex + attempt;
            bool preferCityPoint = (candidateIndex + driver.DriverId) % 3 == 0;
            Vector3 candidate = preferCityPoint && TryGetCityIdleWanderTarget(driver, startPosition, candidateIndex, out Vector3 cityTarget)
                ? cityTarget
                : GetDriverIdleMotelWanderPosition(driver.DriverId - 1, candidateIndex);
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

    private bool IsLowPriorityIdleActivity(DriverAgent driver)
    {
        if (driver == null)
        {
            return false;
        }

        return driver.WalkPhase == DriverRescuePhase.IdleSittingOnBench ||
               driver.WalkPhase == DriverRescuePhase.IdleSmoking ||
               driver.WalkPhase == DriverRescuePhase.IdlePhoneCall ||
               driver.WalkPhase == DriverRescuePhase.IdlePettingCat;
    }

    private bool TryInterruptIdleForCriticalNeed(DriverAgent driver)
    {
        if (driver == null)
        {
            return false;
        }

        DriverRescuePhase interruptedPhase = driver.WalkPhase;
        ReleaseBench(driver);
        ReleaseCatInteraction(driver);
        if (interruptedPhase == DriverRescuePhase.IdleSmoking)
        {
            StopDriverSmokingParticles(driver);
        }

        driver.WalkPhase = DriverRescuePhase.None;
        driver.IdleActivityTimer = 0f;
        driver.LifeGoal = WorkerLifeGoal.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        LogWorkerDecision(driver, "idle-interrupted-critical-need", $"phase={interruptedPhase}; needs={FormatWorkerNeedsDebug(driver)}", true);
        return TryStartDueWorkerLifeCycle(driver);
    }

    private bool TryGetCityIdleWanderTarget(DriverAgent driver, Vector3 startPosition, int pointIndex, out Vector3 target)
    {
        target = Vector3.zero;
        if (driver == null)
        {
            return false;
        }

        LocationType[] interestTypes =
        {
            LocationType.CityPark,
            LocationType.Canteen,
            LocationType.Bar,
            LocationType.GamblingHall,
            LocationType.Stop,
            LocationType.Warehouse,
            LocationType.Parking,
            LocationType.IntercityStop
        };

        int startIndex = Mathf.Abs(driver.DriverId * 11 + pointIndex * 5) % interestTypes.Length;
        for (int i = 0; i < interestTypes.Length; i++)
        {
            LocationType type = interestTypes[(startIndex + i) % interestTypes.Length];
            if (!TryGetIdlePointNearLocation(type, driver.DriverId, pointIndex + i, out Vector3 candidate))
            {
                continue;
            }

            Vector3 flatDelta = candidate - startPosition;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude < 1.44f)
            {
                continue;
            }

            target = candidate;
            return true;
        }

        return false;
    }

    private bool TryGetIdlePointNearLocation(LocationType type, int driverId, int pointIndex, out Vector3 target)
    {
        target = Vector3.zero;
        if (!locations.TryGetValue(type, out LocationData location))
        {
            return false;
        }

        Vector3 center = GetLocationCenter(location);
        Vector3 anchor = GetCellCenter(location.Anchor);
        Vector3 outward = anchor - center;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.0001f)
        {
            outward = Vector3.forward;
        }
        else
        {
            outward.Normalize();
        }

        Vector3 right = new(outward.z, 0f, -outward.x);
        Vector2[] offsets =
        {
            new Vector2(0.35f, 1.25f),
            new Vector2(-0.55f, 1.55f),
            new Vector2(0.95f, 1.80f),
            new Vector2(-1.15f, 1.10f),
            new Vector2(1.35f, 0.72f),
            new Vector2(0.15f, 2.20f)
        };

        int baseIndex = Mathf.Abs(driverId + pointIndex * 3) % offsets.Length;
        for (int attempt = 0; attempt < offsets.Length; attempt++)
        {
            Vector2 offset = offsets[(baseIndex + attempt) % offsets.Length];
            Vector3 candidate = anchor + right * offset.x + outward * offset.y;
            Vector2Int cell = WorldToCell(candidate);
            if (!IsInsideGrid(cell) ||
                roadCells.Contains(cell) ||
                edgeHighwayCells.Contains(cell) ||
                waterCells.Contains(cell) ||
                IsLocationCell(cell))
            {
                continue;
            }

            candidate.y = SampleTerrainHeight(candidate.x, candidate.z);
            target = candidate;
            return true;
        }

        return false;
    }

}
