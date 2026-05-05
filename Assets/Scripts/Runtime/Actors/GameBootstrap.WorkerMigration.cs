using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerMigrationMinDay = 1;
    private const int WorkerMigrationEvaluationHour = 6;
    private const int WorkerMigrationMaxAutoArrivals = 4;
    private const int WorkerMigrationFirstDayMaxAutoArrivals = 3;
    private const int WorkerMigrationArrivalCooldownHours = 2;
    private const int WorkerMigrationVacancyPressureCooldownHours = 1;
    private const int WorkerMigrationGuaranteedMissChecks = 1;
    private const int WorkerMigrationUnhappyThreshold = 35;
    private const int WorkerMigrationDepartureThreshold = 24;
    private const int WorkerMigrationProtectedHigherDays = 5;

    private int lastWorkerMigrationEvaluationDay = -1;
    private int lastWorkerMigrationArrivalCheckKey = -1;
    private int lastWorkerMigrationSuccessfulArrivalKey = -1000;
    private int lastWorkerDepartureDay = -1;
    private int recentWorkerDeparturesToday;
    private int workerMigrationVacancyMissChecks;

    private void UpdateWorkerMigrationRuntime()
    {
        RemoveDepartedWorkers();

        if (lastWorkerDepartureDay != currentDay)
        {
            lastWorkerDepartureDay = currentDay;
            recentWorkerDeparturesToday = 0;
        }

        int currentHour = GetCurrentHour();
        if (currentDay < WorkerMigrationMinDay || currentHour < WorkerMigrationEvaluationHour)
        {
            return;
        }

        if (lastWorkerMigrationEvaluationDay != currentDay)
        {
            lastWorkerMigrationEvaluationDay = currentDay;
            UpdateWorkerMigrationSatisfactionDaily();
        }

        int arrivalCheckKey = currentDay * 24 + currentHour;
        if (lastWorkerMigrationArrivalCheckKey == arrivalCheckKey)
        {
            return;
        }

        lastWorkerMigrationArrivalCheckKey = arrivalCheckKey;
        TryStartAutomaticWorkerArrival();
    }

    private void UpdateWorkerMigrationSatisfactionDaily()
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver == null || driver.IsArrivingByBus || driver.HasDepartedTown || driver.IsLeavingTown)
            {
                continue;
            }

            int delta = CalculateWorkerSatisfactionDelta(driver, out string reason);
            driver.Satisfaction = Mathf.Clamp(driver.Satisfaction + delta, 0, 100);
            bool unhappy = driver.Satisfaction < WorkerMigrationUnhappyThreshold;
            driver.UnhappyDays = unhappy ? driver.UnhappyDays + 1 : 0;
            driver.DepartureIntent = driver.UnhappyDays >= 2;
            driver.DepartureReason = unhappy ? reason : string.Empty;

            SessionDebugLogger.Log(
                "MIGRATION",
                $"{driver.DriverName} satisfaction daily update: delta={delta:+#;-#;0}, value={driver.Satisfaction}, unhappyDays={driver.UnhappyDays}, departureIntent={(driver.DepartureIntent ? "yes" : "no")}, reason={reason}, needs={FormatWorkerNeedsDebug(driver)}, money=${driver.Money}.");

            if (ShouldWorkerLeaveTown(driver) && TryStartWorkerDeparture(driver, reason))
            {
                recentWorkerDeparturesToday++;
            }
        }

        isDriversScreenDirty = true;
    }

    private int CalculateWorkerSatisfactionDelta(DriverAgent driver, out string reason)
    {
        int delta = 0;
        List<string> reasons = new();
        bool employed = IsWorkerEmployedForMigration(driver);
        bool anyCritical = HasCriticalWorkerNeed(driver);

        if (employed)
        {
            delta += 5;
        }
        else
        {
            delta -= 10;
            reasons.Add("no job");
        }

        if (driver.AssignedPersonalHouseIndex >= 0)
        {
            delta += 4;
        }

        if (driver.Money >= 80)
        {
            delta += 3;
        }
        else if (driver.Money < 15)
        {
            delta -= 8;
            reasons.Add("low money");
        }

        if (driver.LastMealNeedStatus == WorkerNeedStatus.Critical)
        {
            delta -= 12;
            reasons.Add("critical food");
        }

        if (driver.LastSleepNeedStatus == WorkerNeedStatus.Critical)
        {
            delta -= 14;
            reasons.Add("critical sleep");
        }

        if (driver.LastLeisureNeedStatus == WorkerNeedStatus.Critical)
        {
            delta -= 8;
            reasons.Add("critical leisure");
        }

        if (IsWorkerDueButBlockedByMoney(driver))
        {
            delta -= 8;
            reasons.Add("services unaffordable");
        }

        if (!anyCritical && employed && driver.Money >= 30)
        {
            delta += 4;
        }

        reason = reasons.Count > 0 ? string.Join(", ", reasons) : "stable life";
        return Mathf.Clamp(delta, -35, 18);
    }

    private bool IsWorkerEmployedForMigration(DriverAgent driver)
    {
        return driver != null &&
               (driver.DutyMode == DriverDutyMode.Logistics ||
                driver.AssignedTruckNumber > 0 ||
                driver.ShiftStartHour >= 0 ||
                IsDriverBusDriver(driver) ||
                IsDriverIntercity(driver));
    }

    private bool ShouldWorkerLeaveTown(DriverAgent driver)
    {
        if (driver == null ||
            driver.UnhappyDays < 3 ||
            driver.Satisfaction > WorkerMigrationDepartureThreshold ||
            !CanWorkerStartDeparture(driver))
        {
            return false;
        }

        if (driver.Education == WorkerEducationLevel.Higher && currentDay <= WorkerMigrationProtectedHigherDays)
        {
            SessionDebugLogger.Log(
                "MIGRATION",
                $"{driver.DriverName} wants to leave but is protected during the early Labor Exchange bootstrap window.");
            return false;
        }

        return true;
    }

    private bool CanWorkerStartDeparture(DriverAgent driver)
    {
        return driver != null &&
               !driver.IsArrivingByBus &&
               !driver.IsLeavingTown &&
               !driver.HasDepartedTown &&
               !driver.IsOnActiveShift &&
               !driver.NeedsShiftEndReturn &&
               !driver.IsInsideBuilding &&
               driver.RestPhase == DriverRestPhase.None &&
               !IsDriverBusyWalkPhase(driver) &&
               !IsDriverOnActiveTradeRun(driver) &&
               !IsBusDriverOnActiveRoute(driver);
    }

    private bool TryStartWorkerDeparture(DriverAgent driver, string reason)
    {
        if (driver?.DriverObject == null)
        {
            return false;
        }

        CleanupWorkerAssignmentsForDeparture(driver);

        Vector3 start = driver.DriverObject.activeSelf
            ? driver.DriverObject.transform.position
            : driver.MotelIdlePosition;
        driver.DriverObject.SetActive(true);
        driver.DriverObject.transform.position = start;
        driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        ApplyDriverPose(driver, 0f, 0f);

        Vector3 target = locations.ContainsKey(LocationType.IntercityStop)
            ? GetDriverStandPointNearLocation(LocationType.IntercityStop)
            : start;

        driver.IsLeavingTown = true;
        driver.DepartureIntent = true;
        driver.DepartureReason = reason;
        driver.LifeGoal = WorkerLifeGoal.Idle;
        driver.RestPhase = DriverRestPhase.None;
        driver.WalkPhase = DriverRescuePhase.ToIntercityStopForDeparture;
        driver.WalkTargetWorld = target;
        driver.WalkAnimationTime = 0f;
        ResetWorkerLocalBusTripState(driver);

        if (TryStartWorkerPersonalCarTrip(driver, start, target, DriverRescuePhase.ToIntercityStopForDeparture, "departure"))
        {
            SessionDebugLogger.Log(
                "MIGRATION",
                $"{driver.DriverName} decided to leave town by personal car: satisfaction={driver.Satisfaction}, unhappyDays={driver.UnhappyDays}, reason={reason}.");
            PushFeedEvent(
                $"{driver.DriverName} is leaving town: {reason}.",
                $"{driver.DriverName} \u0443\u0435\u0437\u0436\u0430\u0435\u0442 \u0438\u0437 \u0433\u043e\u0440\u043e\u0434\u0430: {reason}.",
                FeedEventType.Warning);
            isDriversScreenDirty = true;
            isShiftsScreenDirty = true;
            isFleetScreenDirty = true;
            return true;
        }
        if (CanWorkerUsePersonalCar(driver))
        {
            SessionDebugLogger.Log(
                "MIGRATION",
                $"{driver.DriverName} wants to leave town by personal car but no car route to Intercity Stop exists; departure delayed.");
            return false;
        }

        if (!BuildDriverWalkPath(driver, start, target))
        {
            CompleteWorkerDeparture(driver, "no safe path to Intercity Stop");
            return true;
        }

        SessionDebugLogger.Log(
            "MIGRATION",
            $"{driver.DriverName} decided to leave town: satisfaction={driver.Satisfaction}, unhappyDays={driver.UnhappyDays}, reason={reason}.");
        PushFeedEvent(
            $"{driver.DriverName} is leaving town: {reason}.",
            $"{driver.DriverName} \u0443\u0435\u0437\u0436\u0430\u0435\u0442 \u0438\u0437 \u0433\u043e\u0440\u043e\u0434\u0430: {reason}.",
            FeedEventType.Warning);

        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
        isFleetScreenDirty = true;
        return true;
    }

    private void CleanupWorkerAssignmentsForDeparture(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        LaborExchangePosting posting = FindLaborExchangePosting(driver.ReservedLaborExchangePostingId);
        if (posting != null)
        {
            ReleaseLaborExchangePostingReservation(posting);
        }

        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (assignedTruck != null)
        {
            RemoveDriverFromTruckRoster(assignedTruck, driver);
        }

        if (intercityDriverId == driver.DriverId)
        {
            intercityDriverId = 0;
        }

        for (int i = 0; i < busDriverShiftIds.Length; i++)
        {
            if (busDriverShiftIds[i] == driver.DriverId)
            {
                busDriverShiftIds[i] = 0;
            }
        }

        if (driver.DutyMode == DriverDutyMode.Logistics)
        {
            SetDriverDutyMode(driver, DriverDutyMode.Local);
        }

        driver.AssignedTruckNumber = 0;
        driver.AssignedBuildingType = null;
        driver.AssignedBuildingInstanceId = 0;
        driver.AssignedBuildingSlotIndex = -1;
        driver.ShiftStartHour = -1;
        driver.DutyMode = DriverDutyMode.Local;
        driver.IsOnActiveShift = false;
        driver.WaitingForShiftAtParking = false;
        driver.NeedsShiftEndReturn = false;
        driver.IsShiftSalaryPending = false;
    }

    private void CompleteWorkerDeparture(DriverAgent driver, string reason)
    {
        if (driver == null)
        {
            return;
        }

        driver.IsLeavingTown = false;
        driver.HasDepartedTown = true;
        driver.WalkPhase = DriverRescuePhase.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        if (driver.DriverObject != null)
        {
            driver.DriverObject.SetActive(false);
        }

        SessionDebugLogger.Log("MIGRATION", $"{driver.DriverName} left town. reason={reason}; finalSatisfaction={driver.Satisfaction}; money=${driver.Money}.");
    }

    private void RemoveDepartedWorkers()
    {
        for (int i = driverAgents.Count - 1; i >= 0; i--)
        {
            DriverAgent driver = driverAgents[i];
            if (driver == null || !driver.HasDepartedTown)
            {
                continue;
            }

            if (selectedDriverId == driver.DriverId)
            {
                selectedDriverId = 0;
                isDriverDetailsOpen = false;
            }

            if (selectedWorkerPanelDriverId == driver.DriverId)
            {
                selectedWorkerPanelDriverId = 0;
            }

            if (selectedShiftDriverId == driver.DriverId)
            {
                selectedShiftDriverId = 0;
            }

            if (driver.OwnedCarObject != null)
            {
                Destroy(driver.OwnedCarObject);
            }

            if (driver.DriverObject != null)
            {
                Destroy(driver.DriverObject);
            }

            driverAgents.RemoveAt(i);
            isDriversScreenDirty = true;
            isShiftsScreenDirty = true;
            isFleetScreenDirty = true;
        }
    }

    private void TryStartAutomaticWorkerArrival()
    {
        if (hiringDriverArrival != null ||
            !locations.ContainsKey(LocationType.Motel) ||
            !locations.ContainsKey(LocationType.IntercityStop))
        {
            return;
        }

        int vacancyDemand = CountOpenWorkerVacancyDemand();
        int freeWorkers = CountFreeMigrationWorkers();
        int attraction = CalculateCityWorkerAttraction(vacancyDemand, freeWorkers);
        float chance = CalculateWorkerArrivalChance(vacancyDemand, freeWorkers, attraction);
        int arrivalCheckKey = currentDay * 24 + GetCurrentHour();
        int vacancyShortage = Mathf.Max(0, vacancyDemand - freeWorkers);
        int cooldownHours = vacancyShortage > 0
            ? WorkerMigrationVacancyPressureCooldownHours
            : WorkerMigrationArrivalCooldownHours;
        int cooldownHoursLeft = Mathf.Max(0, cooldownHours - (arrivalCheckKey - lastWorkerMigrationSuccessfulArrivalKey));
        bool guaranteedByOpenVacancies = vacancyDemand > 0 &&
                                         freeWorkers < vacancyDemand &&
                                         workerMigrationVacancyMissChecks >= WorkerMigrationGuaranteedMissChecks;
        bool cooldownReady = cooldownHoursLeft == 0;
        bool shouldArrive = cooldownReady && (guaranteedByOpenVacancies || (chance > 0f && Random.value < chance));
        SessionDebugLogger.Log(
            "MIGRATION",
            $"city migration check: day={currentDay}, hour={GetCurrentHour()}, vacancyDemand={vacancyDemand}, freeWorkers={freeWorkers}, shortage={vacancyShortage}, attraction={attraction}, chance={chance:0.00}, cooldownHoursLeft={cooldownHoursLeft}, missedVacancyChecks={workerMigrationVacancyMissChecks}, guaranteed={(guaranteedByOpenVacancies ? "yes" : "no")}, arrival={(shouldArrive ? "yes" : "no")}.");

        if (!shouldArrive)
        {
            workerMigrationVacancyMissChecks = cooldownReady && vacancyDemand > 0 && freeWorkers < vacancyDemand
                ? workerMigrationVacancyMissChecks + 1
                : 0;
            return;
        }

        int neededWorkers = Mathf.Max(1, vacancyShortage);
        int maxArrivals = currentDay <= WorkerMigrationMinDay
            ? WorkerMigrationFirstDayMaxAutoArrivals
            : WorkerMigrationMaxAutoArrivals;
        if (vacancyShortage >= 5 && currentDay > WorkerMigrationMinDay)
        {
            maxArrivals++;
        }

        int demandArrivalCap = vacancyDemand > 0
            ? Mathf.Clamp(neededWorkers, 1, maxArrivals)
            : 1;
        int minimumDemandArrivals = vacancyShortage >= 3
            ? 2
            : 1;
        int arrivalCount = vacancyDemand > 0 && demandArrivalCap > 1
            ? Random.Range(Mathf.Min(minimumDemandArrivals, demandArrivalCap), demandArrivalCap + 1)
            : 1;
        List<DriverAgent> arrivals = new(arrivalCount);
        for (int i = 0; i < arrivalCount; i++)
        {
            arrivals.Add(CreateAndRegisterDriverAgent(spawnInMotel: false));
        }

        if (!TryStartWorkerArrivalBus(arrivals, false, "automatic migration"))
        {
            return;
        }

        workerMigrationVacancyMissChecks = 0;
        lastWorkerMigrationSuccessfulArrivalKey = arrivalCheckKey;
        PushFeedEvent(
            arrivalCount == 1
                ? $"{arrivals[0].DriverName} is moving into town."
                : $"{arrivalCount} workers are moving into town.",
            arrivalCount == 1
                ? $"{arrivals[0].DriverName} \u043f\u0440\u0438\u0435\u0437\u0436\u0430\u0435\u0442 \u0432 \u0433\u043e\u0440\u043e\u0434."
                : $"\u041d\u043e\u0432\u044b\u0435 \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u0435\u0434\u0443\u0442 \u0432 \u0433\u043e\u0440\u043e\u0434: {arrivalCount}.",
            FeedEventType.Info);
    }

    private int CountOpenWorkerVacancyDemand()
    {
        List<LaborExchangeCandidate> candidates = new();
        AddLaborExchangeShiftCandidates(candidates);
        AddLaborExchangeBuildingCandidates(candidates);
        return candidates.Count + CountAvailableLaborExchangePostings();
    }

    private int CountFreeMigrationWorkers()
    {
        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver == null ||
                driver.IsArrivingByBus ||
                driver.IsLeavingTown ||
                driver.HasDepartedTown ||
                driver.DutyMode != DriverDutyMode.Local ||
                driver.ShiftStartHour >= 0 ||
                driver.AssignedTruckNumber > 0 ||
                driver.AssignedBuildingType.HasValue ||
                IsDriverBusDriver(driver) ||
                IsDriverIntercity(driver))
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private int CalculateCityWorkerAttraction(int vacancyDemand, int freeWorkers)
    {
        int score = 42 + vacancyDemand * 10 - freeWorkers * 4 - recentWorkerDeparturesToday * 8;
        if (locations.ContainsKey(LocationType.Motel)) score += 12;
        if (locations.ContainsKey(LocationType.Canteen)) score += 7;
        if (locations.ContainsKey(LocationType.CityPark)) score += 5;
        if (locations.ContainsKey(LocationType.Bar) || locations.ContainsKey(LocationType.GamblingHall)) score += 5;
        if (locations.ContainsKey(LocationType.LaborExchange) && IsLaborExchangeReadyForApplicants(out _)) score += 14;
        int averageSalary = EstimateAverageOpenVacancySalary();
        if (averageSalary > 30) score += Mathf.Clamp((averageSalary - 30) / 3, 0, 16);
        return Mathf.Clamp(score, 0, 100);
    }

    private float CalculateWorkerArrivalChance(int vacancyDemand, int freeWorkers, int attraction)
    {
        if (vacancyDemand <= 0 && (freeWorkers >= 4 || attraction < 60))
        {
            return 0f;
        }

        if (vacancyDemand <= 0)
        {
            return attraction >= 78 && freeWorkers <= 1 ? 0.08f : 0f;
        }

        if (freeWorkers >= vacancyDemand)
        {
            return 0f;
        }

        int vacancyShortage = Mathf.Max(0, vacancyDemand - freeWorkers);
        float chance = 0.42f + Mathf.Min(vacancyDemand, 8) * 0.075f + Mathf.Max(0, attraction - 50) * 0.005f;
        if (freeWorkers > 0)
        {
            chance -= freeWorkers * 0.05f;
        }

        if (vacancyShortage >= 2)
        {
            chance = Mathf.Max(chance, currentDay <= WorkerMigrationMinDay ? 0.72f : 0.82f);
        }

        if (vacancyShortage >= 5)
        {
            chance = Mathf.Max(chance, currentDay <= WorkerMigrationMinDay ? 0.84f : 0.92f);
        }

        return Mathf.Clamp(chance, 0f, currentDay <= WorkerMigrationMinDay ? 0.88f : 0.96f);
    }

    private bool TryStartWorkerArrivalBus(List<DriverAgent> workers, bool isTutorialWave, string source)
    {
        if (workers == null || workers.Count == 0 || hiringDriverArrival != null)
        {
            return false;
        }

        hiringDriverArrival = new HiringDriverArrivalData
        {
            Driver = workers[0],
            IsTutorialWave = isTutorialWave,
            Phase = HiringDriverArrivalPhase.WaitingLaneClear
        };
        hiringDriverArrival.Drivers.AddRange(workers);
        SessionDebugLogger.Log("MIGRATION", $"{workers.Count} worker(s) started arrival bus flow; source={source}, tutorialWave={(isTutorialWave ? "yes" : "no")}.");
        return true;
    }

    private string GetWorkerMigrationStatusLabel(DriverAgent driver, bool ru)
    {
        if (driver == null)
        {
            return ru ? "\u041d\u0435\u0442 \u0434\u0430\u043d\u043d\u044b\u0445" : "No data";
        }

        if (driver.IsLeavingTown || driver.HasDepartedTown)
        {
            return ru ? "\u0423\u0435\u0437\u0436\u0430\u0435\u0442" : "Leaving";
        }

        if (driver.DepartureIntent)
        {
            return ru ? "\u0414\u0443\u043c\u0430\u0435\u0442 \u0443\u0435\u0445\u0430\u0442\u044c" : "May leave";
        }

        if (driver.Satisfaction >= 70)
        {
            return ru ? "\u0414\u043e\u0432\u043e\u043b\u0435\u043d" : "Happy";
        }

        if (driver.Satisfaction >= WorkerMigrationUnhappyThreshold)
        {
            return ru ? "\u0421\u0442\u0430\u0431\u0438\u043b\u0435\u043d" : "Stable";
        }

        return ru ? "\u041d\u0435\u0434\u043e\u0432\u043e\u043b\u0435\u043d" : "Unhappy";
    }
}
