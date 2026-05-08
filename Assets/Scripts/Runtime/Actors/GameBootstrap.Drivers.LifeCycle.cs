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
        TryReleaseShiftWaitForCriticalNeed(driver);
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
            LogWorkerDecision(
                driver,
                "skip-due-life-cycle",
                $"hour={hour}, hasDueNeed=False, retry meal={driver.NextMealRetryAtWorldHour:0.0}, leisure={driver.NextLeisureRetryAtWorldHour:0.0}, sleep={driver.NextSleepRetryAtWorldHour:0.0}, flags eat={driver.AteToday}, leisure={driver.HadLeisureToday}, sleep={driver.SleptToday}",
                verboseOnly: true);
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
            (ShouldLogisticsWorkerHeadToBuilding(driver) || IsLogisticsWorkerWorkHour(driver)))
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

    private bool TryReleaseShiftWaitForCriticalNeed(DriverAgent driver)
    {
        if (driver == null ||
            !driver.WaitingForShiftAtParking ||
            !HasCriticalWorkerNeed(driver) ||
            driver.DriverObject == null ||
            driver.IsArrivingByBus ||
            driver.IsOnActiveShift ||
            driver.IsInsideBuilding ||
            driver.NeedsShiftEndReturn ||
            driver.RestPhase != DriverRestPhase.None ||
            IsDriverOnActiveTradeRun(driver) ||
            IsBusDriverOnActiveRoute(driver) ||
            GetCurrentTruckForDriver(driver) != null)
        {
            return false;
        }

        if (ShouldWorkerStayInShiftWait(driver))
        {
            return false;
        }

        driver.WaitingForShiftAtParking = false;
        driver.WalkPhase = DriverRescuePhase.None;
        driver.WalkTargetWorld = driver.DriverObject.transform.position;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;
        driver.IdleActivityTimer = 0f;
        driver.IdleWanderPauseTimer = 0f;
        driver.IdleWanderPointIndex = -1;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        driver.LifeGoal = WorkerLifeGoal.None;
        LogWorkerDecision(
            driver,
            "shift-wait-interrupted-critical-need",
            $"needs={FormatWorkerNeedsDebug(driver)}; minutesUntilShift={GetMinutesUntilShiftStart(driver)}",
            true);
        return true;
    }

    private bool ShouldWorkerStayInShiftWait(DriverAgent driver)
    {
        if (driver == null)
        {
            return false;
        }

        if (driver.DutyMode == DriverDutyMode.Logistics)
        {
            return ShouldLogisticsWorkerHeadToBuilding(driver) || IsLogisticsWorkerWorkHour(driver);
        }

        int hour = GetCurrentHour();
        if (IsDriverBusDriver(driver))
        {
            return ShouldBusDriverHeadToShift(driver) ||
                   driver.ShiftStartHour >= 0 && IsHourInShiftWindow(hour, driver.ShiftStartHour);
        }

        return ShouldDriverHeadToShift(driver) ||
               driver.ShiftStartHour >= 0 && IsHourInShiftWindow(hour, driver.ShiftStartHour);
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
        if (driver.AssignedPersonalHouseIndex < 0 &&
            TryStartWorkerBuyHouse(driver, startPosition))
        {
            LogWorkerDecision(driver, "life-goal-selected", "Buying personal house", true, selectedGoalBefore, driver.LifeGoal);
            return true;
        }

        selectedGoalBefore = driver.LifeGoal;
        if (driver.OwnedCarModelIndex < 0 &&
            driver.AssignedPersonalHouseIndex >= 0 &&
            driver.Money >= CarPurchasePrice &&
            locations.ContainsKey(LocationType.CarMarket) &&
            TryStartWorkerBuyCar(driver, startPosition))
        {
            LogWorkerDecision(driver, "life-goal-selected", "Buying car", true, selectedGoalBefore, driver.LifeGoal);
            return true;
        }

        driver.LifeGoal = WorkerLifeGoal.Idle;
        SessionDebugLogger.Log("LIFE", $"{driver.DriverName} has no due life goals; entering Idle. helperFlags work={driver.WorkedToday}, eat={driver.AteToday}, leisure={driver.HadLeisureToday}, sleep={driver.SleptToday}; needs={FormatWorkerNeedsDebug(driver)}.");
        LogWorkerDecision(driver, "enter-idle", "no due life goals after evaluation", true);
        return false;
    }

    private bool TryStartWorkerLifeGoal(DriverAgent driver, WorkerLifeGoal goal, Vector3 startPosition)
    {
        switch (goal)
        {
            case WorkerLifeGoal.Eat:
                if (TryStartWorkerMealAtHome(driver, startPosition))
                {
                    return true;
                }

                if (driver.AssignedPersonalHouseIndex >= 0 && driver.AssignedPersonalHouseIndex < personalHouses.Count)
                {
                    string homeMealUnavailableReason = "personal house meal path unavailable";
                    if (TryStartWorkerCriticalNeedVendorPurchase(driver, WorkerNeedKind.Meal, startPosition))
                    {
                        return true;
                    }

                    SessionDebugLogger.LogVerbose("LIFE", $"{driver.DriverName} skipped Canteen because they own PersonalHouse #{driver.AssignedPersonalHouseIndex}; home meal unavailable; need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Meal)}; snapshot={FormatWorkerNeedsDebug(driver)}.");
                    LogWorkerDecision(driver, "skip-canteen-home-owner", homeMealUnavailableReason, true);
                    driver.LifeGoal = WorkerLifeGoal.None;
                    SetWorkerNeedRetryCooldown(driver, WorkerNeedKind.Meal, homeMealUnavailableReason);
                    return ContinueWorkerLifeCycle(driver, startPosition);
                }

                if (TryStartWorkerCriticalNeedVendorPurchase(driver, WorkerNeedKind.Meal, startPosition))
                {
                    return true;
                }

                if (TryStartWorkerServiceVisit(driver, LocationType.Canteen, WorkerLifeGoal.Eat, DriverRescuePhase.IdleWalkToCanteen, WorkerCanteenDuration, startPosition))
                {
                    return true;
                }
                string mealUnavailableReason = GetWorkerServiceUnavailableReason(driver, LocationType.Canteen);
                SessionDebugLogger.LogVerbose("LIFE", $"{driver.DriverName} skipped Canteen today; reason={mealUnavailableReason}; need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Meal)}; snapshot={FormatWorkerNeedsDebug(driver)}.");
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
                if (TryStartWorkerCriticalNeedVendorPurchase(driver, WorkerNeedKind.Sleep, startPosition))
                {
                    return true;
                }

                if (TryStartWorkerSleep(driver, startPosition))
                {
                    return true;
                }
                SessionDebugLogger.LogVerbose("LIFE", $"{driver.DriverName} skipped Motel sleep today; reason={GetWorkerServiceUnavailableReason(driver, LocationType.Motel)}; need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Sleep)}; snapshot={FormatWorkerNeedsDebug(driver)}.");
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
               IsWorkerServiceBlockedByMoney(reason);
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
        if (!BuildDriverWalkPath(driver, startPosition, target))
        {
            driver.WalkPhase = DriverRescuePhase.None;
            driver.IdleActivityTimer = 0f;
            return false;
        }
        SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} cannot afford Canteen and is heading to a trash can meal; reason={reason}; target=({target.x:0.0},{target.z:0.0}); need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Meal)}.");
        LogWorkerDecision(driver, "trash-meal-fallback", $"{reason}; target=({target.x:0.0},{target.z:0.0})", true);
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
            driver.WalkPhase = DriverRescuePhase.IdleWalkToBench;
            if (!BuildDriverWalkPath(driver, startPosition, benchPosition))
            {
                ReleaseBench(driver);
                driver.WalkPhase = DriverRescuePhase.None;
                return false;
            }
            if (IsWorkerServiceBlockedByMoney(reason))
            {
                SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} cannot afford Motel and is heading to bench sleep fallback; reason={reason}; bench={benchIndex}; need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Sleep)}.");
            }
            RecordWorkerNeedFallbackThought(driver, need, reason);
            LogWorkerDecision(driver, "need-fallback", $"{need}: emergency bench rest; reason={reason}", true);
            return true;
        }

        if (TryGetCityIdleWanderTarget(driver, startPosition, driver.IdleWanderPointIndex + 3, out Vector3 fallbackTarget))
        {
            driver.WalkTargetWorld = fallbackTarget;
            if (need == WorkerNeedKind.Sleep)
            {
                driver.WalkPhase = DriverRescuePhase.IdleWalkToBench;
                if (!BuildDriverWalkPath(driver, startPosition, fallbackTarget))
                {
                    driver.WalkPhase = DriverRescuePhase.None;
                    return false;
                }
                RecordWorkerNeedFallbackThought(driver, need, reason);
                LogWorkerDecision(driver, "need-fallback", $"{need}: walking to free ground rest; reason={reason}", true);
                return true;
            }

            driver.WalkPhase = DriverRescuePhase.IdleWander;
            driver.IdleWanderPointIndex++;
            if (!BuildDriverWalkPath(driver, startPosition, fallbackTarget))
            {
                driver.WalkPhase = DriverRescuePhase.None;
                return false;
            }
            RecordWorkerNeedFallbackThought(driver, need, reason);
            LogWorkerDecision(driver, "need-fallback-walk", $"{need}: walking to free city fallback; reason={reason}", true);
            return true;
        }

        driver.WalkPhase = need == WorkerNeedKind.Sleep
            ? DriverRescuePhase.IdleSittingOnBench
            : DriverRescuePhase.IdlePhoneCall;
        RecordWorkerNeedFallbackThought(driver, need, reason);
        LogWorkerDecision(driver, "need-fallback", $"{need}: immediate free fallback; reason={reason}", true);
        return true;
    }

    private void RecordWorkerNeedFallbackThought(DriverAgent driver, WorkerNeedKind need, string reason)
    {
        RecordWorkerThought(
            driver,
            WorkerThoughtKind.Need,
            WorkerThoughtTone.Negative,
            72,
            "need_fallback_bad",
            new[]
            {
                ThoughtNeed("need", need),
                ThoughtText("reason", reason)
            },
            WorkerThoughtSubjectType.Need,
            0,
            need.ToString(),
            need.ToString(),
            -8,
            $"need_fallback|{need}",
            6f);
    }

    private bool TryStartWorkerSleep(DriverAgent driver, Vector3 startPosition)
    {
        if (driver == null)
            return false;

        if (driver.AssignedPersonalHouseIndex >= 0 && driver.AssignedPersonalHouseIndex < personalHouses.Count)
            return TryStartWorkerSleepAtHome(driver, startPosition);

        if (TryStartWorkerBuyHouseBeforeMotelSleep(driver, startPosition))
            return true;

        if (!locations.TryGetValue(LocationType.Motel, out LocationData motel))
        {
            RecordWorkerServiceMissingThought(driver, LocationType.Motel, WorkerNeedKind.Sleep, "Motel not built");
            LogWorkerDecision(driver, "sleep-unavailable", GetWorkerServiceUnavailableReason(driver, LocationType.Motel), true);
            return false;
        }

        if (driver.Money < motel.ServiceFee)
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
        if (!BuildDriverWalkPath(driver, startPosition, driver.WalkTargetWorld))
        {
            driver.WalkPhase = DriverRescuePhase.None;
            driver.RestPhase = DriverRestPhase.None;
            driver.LifeGoal = WorkerLifeGoal.None;
            return false;
        }
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
        if (TryStartWorkerPersonalCarTrip(driver, startPosition, target, DriverRescuePhase.ToPersonalHouseEntrance, "Home sleep"))
        {
            LogWorkerDecision(driver, "sleep-home-by-car", $"House #{driver.AssignedPersonalHouseIndex}", true);
            return true;
        }
        if (CanWorkerUsePersonalCar(driver))
        {
            LogWorkerDecision(driver, "sleep-home-car-blocked", $"House #{driver.AssignedPersonalHouseIndex}: no personal car route", true);
            return false;
        }

        if (TryStartWorkerLocalBusTrip(driver, startPosition, target, DriverRescuePhase.ToPersonalHouseEntrance, "Home sleep"))
        {
            LogWorkerDecision(driver, "sleep-home-via-bus", $"House #{driver.AssignedPersonalHouseIndex}", true);
            return true;
        }
        driver.WalkTargetWorld = target;
        driver.WalkPhase = DriverRescuePhase.ToPersonalHouseEntrance;
        driver.WalkAnimationTime = 0f;
        if (!BuildDriverWalkPath(driver, startPosition, target))
        {
            driver.WalkPhase = DriverRescuePhase.None;
            driver.RestPhase = DriverRestPhase.None;
            driver.LifeGoal = WorkerLifeGoal.None;
            return false;
        }
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


}
