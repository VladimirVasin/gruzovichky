using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float WorkerDecisionRepeatThrottleSeconds = 30f;
    private const float WorkerDecisionRepeatThrottleWorldHours = 1f;
    private const float LocalBusPassengerSkipRepeatThrottleSeconds = 20f;
    private const float RuntimeFrameGapLogThresholdSeconds = 1f;
    private const float RuntimeUpdateDurationLogThresholdSeconds = 0.25f;
    private const float RuntimePerfLogCooldownSeconds = 2f;

    private float lastRuntimeUpdateStartRealtime = -1f;
    private float lastRuntimeFrameGapLogRealtime = -10f;
    private float lastRuntimeUpdateDurationLogRealtime = -10f;

    private void TrackRuntimeFrameGap(float updateStartedRealtime)
    {
        if (lastRuntimeUpdateStartRealtime >= 0f)
        {
            float gapSeconds = updateStartedRealtime - lastRuntimeUpdateStartRealtime;
            if (gapSeconds >= RuntimeFrameGapLogThresholdSeconds &&
                updateStartedRealtime - lastRuntimeFrameGapLogRealtime >= RuntimePerfLogCooldownSeconds)
            {
                lastRuntimeFrameGapLogRealtime = updateStartedRealtime;
                SessionDebugLogger.Log(
                    "PERF",
                    $"Frame gap before Update: {gapSeconds:0.000}s; {FormatRuntimePerfSnapshot()}.");
            }
        }

        lastRuntimeUpdateStartRealtime = updateStartedRealtime;
    }

    private void TrackRuntimeUpdateDuration(float updateStartedRealtime)
    {
        float now = Time.realtimeSinceStartup;
        float durationSeconds = now - updateStartedRealtime;
        if (durationSeconds < RuntimeUpdateDurationLogThresholdSeconds ||
            now - lastRuntimeUpdateDurationLogRealtime < RuntimePerfLogCooldownSeconds)
        {
            return;
        }

        lastRuntimeUpdateDurationLogRealtime = now;
        SessionDebugLogger.Log(
            "PERF",
            $"Slow Update duration: {durationSeconds:0.000}s; {FormatRuntimePerfSnapshot()}.");
    }

    private string FormatRuntimePerfSnapshot()
    {
        int driverFlashlightLights = CountActiveDriverFlashlightLights(out int driverHaloLights);
        float cameraY = mainCamera != null ? mainCamera.transform.position.y : cameraOffset.y;
        string hudState = $"fleet={isFleetPanelOpen}, build={isBuildPanelOpen}/{activeBuildTool}, noosphere={isNoospherePanelOpen}, citySocial={isCitySocialRequestSceneOpen}, barInterior={isBarInteriorSceneOpen}";
        return
            $"workers={driverAgents.Count}, trucks={truckAgents.Count}, buses={busAgents.Count}, locations={locations.Count}, " +
            $"roadLanterns={roadLanterns.Count}, activeRoadLights={CountActiveRoadLanternLights()}, activeLocationLights={CountEnabledLights(locationNightLights)}, " +
            $"driverFlashlights={driverFlashlightLights}, driverHalos={driverHaloLights}, truckHeadlights={CountActiveTruckHeadlights()}, busHeadlights={CountActiveBusHeadlights()}, " +
            $"farLod={isFarZoomVisualLodActive}, cameraY={cameraY:0.0}, hud=[{hudState}]";
    }

    private static int CountEnabledLights(List<Light> lights)
    {
        if (lights == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < lights.Count; i++)
        {
            if (lights[i] != null && lights[i].enabled)
            {
                count++;
            }
        }

        return count;
    }

    private int CountActiveRoadLanternLights()
    {
        int count = 0;
        for (int i = 0; i < roadLanterns.Count; i++)
        {
            Light lightComponent = roadLanterns[i]?.Light;
            if (lightComponent != null && lightComponent.enabled)
            {
                count++;
            }
        }

        return count;
    }

    private int CountActiveDriverFlashlightLights(out int haloCount)
    {
        int beamCount = 0;
        haloCount = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver?.DriverFlashlightLight != null && driver.DriverFlashlightLight.enabled)
            {
                beamCount++;
            }

            if (driver?.DriverFlashlightHaloLight != null && driver.DriverFlashlightHaloLight.enabled)
            {
                haloCount++;
            }
        }

        return beamCount;
    }

    private int CountActiveTruckHeadlights()
    {
        int count = 0;
        for (int i = 0; i < truckAgents.Count; i++)
        {
            TruckAgent truck = truckAgents[i];
            if (truck == null)
            {
                continue;
            }

            count += CountEnabledLights(truck.TruckHeadlights);
        }

        return count;
    }

    private int CountActiveBusHeadlights()
    {
        int count = 0;
        for (int i = 0; i < busAgents.Count; i++)
        {
            BusAgent bus = busAgents[i];
            if (bus?.HeadlightLeft != null && bus.HeadlightLeft.enabled)
            {
                count++;
            }

            if (bus?.HeadlightRight != null && bus.HeadlightRight.enabled)
            {
                count++;
            }
        }

        return count;
    }

    private void LogWorkerDecision(
        DriverAgent driver,
        string decision,
        string reason,
        bool force = false,
        WorkerLifeGoal? selectedGoalBefore = null,
        WorkerLifeGoal? selectedGoalAfter = null,
        bool verboseOnly = false)
    {
        if (driver == null)
        {
            return;
        }

        if (verboseOnly && !SessionDebugLogger.IsVerboseEnabled("WORKER_DECISION"))
        {
            return;
        }

        WorkerLifeGoal goalBefore = selectedGoalBefore ?? driver.LifeGoal;
        WorkerLifeGoal goalAfter = selectedGoalAfter ?? driver.LifeGoal;
        bool shouldThrottleRepeats = ShouldThrottleWorkerDecisionRepeats(decision);
        string key = shouldThrottleRepeats
            ? BuildWorkerDecisionThrottleKey(driver, decision, reason, goalBefore, goalAfter)
            : BuildWorkerDecisionDebugKey(driver, decision, reason, goalBefore, goalAfter);
        float worldHour = shouldThrottleRepeats ? GetCurrentWorldHour() : 0f;
        if (shouldThrottleRepeats && ShouldSuppressDebugThrottle(
                driver.WorkerDecisionDebugThrottle,
                key,
                Time.unscaledTime,
                WorkerDecisionRepeatThrottleSeconds,
                worldHour,
                WorkerDecisionRepeatThrottleWorldHours))
        {
            return;
        }

        if (!shouldThrottleRepeats && !force && driver.LastWorkerDecisionDebugKey == key)
        {
            return;
        }

        driver.LastWorkerDecisionDebugKey = key;

        string message = $"{driver.DriverName}: {decision}; reason={reason}; selectedGoalBefore={goalBefore}; selectedGoalAfter={goalAfter}; {FormatWorkerDecisionSnapshot(driver)}.";
        if (verboseOnly)
        {
            SessionDebugLogger.LogVerbose("WORKER_DECISION", message);
        }
        else
        {
            SessionDebugLogger.Log("WORKER_DECISION", message);
        }
    }

    private static string BuildWorkerDecisionDebugKey(
        DriverAgent driver,
        string decision,
        string reason,
        WorkerLifeGoal goalBefore,
        WorkerLifeGoal goalAfter)
    {
        return $"{decision}|{reason}|before={goalBefore}|after={goalAfter}|{driver.WalkPhase}|{driver.RestPhase}|{driver.LifeGoal}|{driver.DutyMode}|{driver.ShiftStartHour}|{driver.AssignedBuildingType}|{driver.AssignedTruckNumber}|{driver.IsOnActiveShift}|{driver.IsInsideBuilding}";
    }

    private static string BuildWorkerDecisionThrottleKey(
        DriverAgent driver,
        string decision,
        string reason,
        WorkerLifeGoal goalBefore,
        WorkerLifeGoal goalAfter)
    {
        return $"{decision}|{NormalizeWorkerDecisionThrottleReason(reason)}|before={goalBefore}|after={goalAfter}|{driver.RestPhase}|{driver.DutyMode}|{driver.ShiftStartHour}|{GetWorkerDecisionAssignmentDebugKey(driver)}|inside={driver.IsInsideBuilding}";
    }

    private static string GetWorkerDecisionAssignmentDebugKey(DriverAgent driver)
    {
        if (driver.AssignedBuildingType.HasValue)
        {
            return $"{driver.AssignedBuildingType.Value}#{driver.AssignedBuildingInstanceId}:slot={driver.AssignedBuildingSlotIndex}";
        }

        return driver.AssignedTruckNumber > 0 ? $"Truck#{driver.AssignedTruckNumber}" : "none";
    }

    private static string NormalizeWorkerDecisionThrottleReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return string.Empty;
        }

        return CollapseVolatileNumberRuns(reason.Trim());
    }

    private static string CollapseVolatileNumberRuns(string value)
    {
        System.Text.StringBuilder builder = new(value.Length);
        bool inNumberRun = false;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool numberLike = char.IsDigit(c) || c == '.' || c == ',';
            if (numberLike)
            {
                if (!inNumberRun)
                {
                    if (builder.Length == 0 || builder[builder.Length - 1] != '#')
                    {
                        builder.Append('#');
                    }

                    inNumberRun = true;
                }

                continue;
            }

            inNumberRun = false;
            builder.Append(c);
        }

        return builder.ToString();
    }

    private static bool ShouldThrottleWorkerDecisionRepeats(string decision)
    {
        return decision == "idle-blocked" ||
               decision == "idle-activity" ||
               decision == "idle-activity-blocked" ||
               decision == "life-goal-selected" ||
               decision == "start-life-cycle" ||
               decision == "service-unavailable" ||
               decision == "skip-meal-service" ||
               decision == "skip-sleep-service" ||
               decision == "skip-canteen-home-owner" ||
               decision == "buy-house-skip" ||
               decision == "skip-due-life-cycle" ||
               decision == "skip-labor-exchange" ||
               decision == "leisure-no-candidates" ||
               decision == "sleep-unavailable" ||
               decision == "need-fallback-walk" ||
               decision == "need-fallback" ||
               decision == "trash-meal-unavailable" ||
               decision == "need-retry-cooldown";
    }

    private void LogLocalBusPassengerSkip(DriverAgent driver, string travelReason, string skipReason)
    {
        if (driver == null || localStops.Count == 0)
        {
            return;
        }

        string reason = string.IsNullOrEmpty(travelReason) ? "trip" : travelReason;
        string key = $"{reason}|{skipReason}|{driver.WalkPhase}|{driver.RestPhase}|{driver.LifeGoal}|{driver.DutyMode}|{driver.ShiftStartHour}|{driver.AssignedBuildingType}|{driver.AssignedTruckNumber}|{driver.BusOriginStopNumber}|{driver.BusDestinationStopNumber}";
        if (ShouldSuppressDebugThrottle(
                driver.LocalBusSkipDebugThrottle,
                key,
                Time.unscaledTime,
                LocalBusPassengerSkipRepeatThrottleSeconds,
                0f,
                0f))
        {
            return;
        }

        SessionDebugLogger.Log("BUS_PASSENGER", $"{driver.DriverName} skipped local bus for {reason}: {skipReason}.");
    }

    private static bool ShouldSuppressDebugThrottle(
        System.Collections.Generic.Dictionary<string, DebugThrottleStamp> throttle,
        string key,
        float realTime,
        float realSeconds,
        float worldHour,
        float worldHours)
    {
        if (throttle.TryGetValue(key, out DebugThrottleStamp stamp))
        {
            bool withinRealWindow = realSeconds > 0f && realTime - stamp.RealTime < realSeconds;
            bool withinWorldWindow = worldHours > 0f && worldHour - stamp.WorldHour < worldHours;
            if (withinRealWindow || withinWorldWindow)
            {
                return true;
            }
        }

        throttle[key] = new DebugThrottleStamp
        {
            RealTime = realTime,
            WorldHour = worldHour
        };
        return false;
    }

    private string FormatWorkerDecisionSnapshot(DriverAgent driver)
    {
        if (driver == null)
        {
            return "worker=null";
        }

        Vector2Int cell = GetWorkerDebugCell(driver);
        string assignment = driver.AssignedBuildingType.HasValue
            ? driver.AssignedBuildingType.Value.ToString()
            : driver.AssignedTruckNumber > 0
                ? $"Truck #{driver.AssignedTruckNumber}"
                : "none";
        string bus = driver.BusDestinationStopNumber > 0
            ? $"bus={driver.WalkPhase} Stop#{driver.BusOriginStopNumber}->Stop#{driver.BusDestinationStopNumber} ({driver.BusTravelReason})"
            : "bus=none";

        return $"cell=({cell.x},{cell.y}), citizen=#{driver.CitizenId}, profession={GetCitizenProfessionLabel(driver)}, duty={driver.DutyMode}, assignment={assignment}, shift={driver.ShiftStartHour}, onShift={driver.IsOnActiveShift}, inside={driver.IsInsideBuilding}, walk={driver.WalkPhase}, rest={driver.RestPhase}, goal={driver.LifeGoal}, money=${driver.Money}, satisfaction={driver.Satisfaction}, unhappyDays={driver.UnhappyDays}, leaving={driver.IsLeavingTown}, needs=[{FormatWorkerNeedsDebug(driver)}], {bus}";
    }

    private Vector2Int GetWorkerDebugCell(DriverAgent driver)
    {
        if (driver == null || driver.DriverObject == null)
        {
            return new Vector2Int(-1, -1);
        }

        return WorldToCell(driver.DriverObject.transform.position);
    }

    private void LogBuildingBankTransaction(LocationData location, DriverAgent driver, int amount, string reason, int driverMoneyBefore, int bankBefore)
    {
        if (location == null)
        {
            return;
        }

        string workerLabel = driver != null ? driver.DriverName : "n/a";
        int driverMoneyAfter = driver != null ? driver.Money : driverMoneyBefore;
        SessionDebugLogger.Log(
            "BUILDING_BANK",
            $"{location.Label}: +${amount} from {workerLabel}; reason={reason}; workerMoney ${driverMoneyBefore}->{driverMoneyAfter}; bank ${bankBefore}->{location.BuildingBank}; resources logs={location.LogsStored}, boards={location.BoardsStored}, textile={location.TextileStored}, furniture={location.FurnitureStored}.");

        if (driver != null && amount > 0)
        {
            RecordMoneyMovement(
                0,
                workerLabel,
                location.Label,
                reason,
                null,
                location.BuildingBank,
                MoneyAccountKind.ResidentWallet,
                MoneyAccountKind.BuildingCash,
                InferBuildingBankTransactionReasonKind(reason),
                fromOwnerId: driver.DriverId,
                toOwnerId: location.InstanceId);
        }
    }

    private void LogEconomyMovement(MoneyLedgerEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        string treasuryAfter = entry.TreasuryAfter.HasValue ? $" treasuryAfter=${entry.TreasuryAfter.Value};" : string.Empty;
        string recipientAfter = entry.RecipientBalanceAfter.HasValue ? $" recipientBalanceAfter=${entry.RecipientBalanceAfter.Value};" : string.Empty;
        SessionDebugLogger.Log(
            "ECON",
            $"ledger delta={entry.TreasuryDelta:+#;-#;0}; from={entry.FromLabel}({entry.FromAccountKind}#{entry.FromOwnerId}); to={entry.ToLabel}({entry.ToAccountKind}#{entry.ToOwnerId}); reasonKind={entry.ReasonKind}; reason={entry.Reason};{treasuryAfter}{recipientAfter} time={entry.TimeLabel}.");
    }

    private static MoneyTransactionReasonKind InferBuildingBankTransactionReasonKind(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return MoneyTransactionReasonKind.ServiceFee;
        }

        string normalized = reason.ToLowerInvariant();
        if (normalized.Contains("gambl"))
        {
            return MoneyTransactionReasonKind.Gambling;
        }

        if (normalized.Contains("house"))
        {
            return MoneyTransactionReasonKind.PropertyPurchase;
        }

        if (normalized.Contains("car"))
        {
            return MoneyTransactionReasonKind.VehiclePurchase;
        }

        return MoneyTransactionReasonKind.ServiceFee;
    }
}
