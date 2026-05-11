using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private void UpdateLaborExchangeRuntime()
    {
        if (laborExchangeApplicantDay != currentDay)
        {
            laborExchangeApplicantDay = currentDay;
            laborExchangeApplicantsToday = 0;
        }

        bool built = locations.ContainsKey(LocationType.LaborExchange);
        if (built != wasLaborExchangeBuiltLastTick)
        {
            wasLaborExchangeBuiltLastTick = built;
            SessionDebugLogger.Log("LABOR_EXCHANGE", built
                ? "Labor Exchange is built; waiting for higher-educated clerk during work hours."
                : "Labor Exchange is missing; vacancy automation is offline.");
        }

        PruneLaborExchangePostings();
        if (!built)
        {
            ClearLaborExchangePostings("building missing");
            wasLaborExchangeStaffedLastTick = false;
            return;
        }

        bool staffed = IsLaborExchangeStaffed();
        if (staffed != wasLaborExchangeStaffedLastTick)
        {
            wasLaborExchangeStaffedLastTick = staffed;
            SessionDebugLogger.Log("LABOR_EXCHANGE", staffed
                ? "Labor Exchange staffed; vacancy generation online."
                : "Labor Exchange has no active clerk; vacancy generation paused.");
        }

        if (!staffed)
        {
            return;
        }

        UpdateLaborExchangePostingOffers();
        laborExchangePostingTimer -= Time.deltaTime * Mathf.Max(0f, gameSpeedMultiplier);
        if (laborExchangePostings.Count == 0 || laborExchangePostingTimer <= 0f)
        {
            bool posted = TryPublishNextLaborExchangePosting();
            laborExchangePostingTimer = posted
                ? LaborExchangePostingInterval
                : LaborExchangePostingInterval * 0.5f;
        }
    }

    private bool IsLaborExchangeStaffed()
    {
        return locations.ContainsKey(LocationType.LaborExchange) &&
               CountWorkersOnShiftAt(LocationType.LaborExchange) > 0;
    }

    private void TryAutoAssignHigherEducatedLaborExchangeClerk(string reason)
    {
        if (!locations.ContainsKey(LocationType.LaborExchange))
        {
            return;
        }

        if (CountLogisticsWorkers(LocationType.LaborExchange) > 0)
        {
            SessionDebugLogger.Log("LABOR_EXCHANGE", $"Auto clerk assignment skipped ({reason}): Labor Exchange already has assigned staff.");
            return;
        }

        LogisticsSlotUi slot = FindLogisticsSlot(LocationType.LaborExchange, 0);
        if (slot == null)
        {
            SessionDebugLogger.Log("LABOR_EXCHANGE", $"Auto clerk assignment skipped ({reason}): Labor Exchange staff slot is not initialized.");
            return;
        }

        DriverAgent candidate = FindAutoLaborExchangeClerkCandidate();
        if (candidate == null)
        {
            SessionDebugLogger.Log("LABOR_EXCHANGE", $"Auto clerk assignment skipped ({reason}): no vacant higher-educated worker found.");
            return;
        }

        AssignWorkerToBuilding(candidate, slot);
        if (candidate.DutyMode == DriverDutyMode.Logistics &&
            candidate.AssignedBuildingType == LocationType.LaborExchange)
        {
            SessionDebugLogger.Log("LABOR_EXCHANGE", $"Auto-assigned {candidate.DriverName} as Labor Exchange clerk ({reason}).");
        }
    }

    private DriverAgent FindAutoLaborExchangeClerkCandidate()
    {
        DriverAgent fallback = null;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (!HasHigherEducation(driver))
            {
                continue;
            }

            if (!IsWorkerVacantForVacancyAssignment(driver))
            {
                fallback ??= driver;
                continue;
            }

            return driver;
        }

        if (fallback != null)
        {
            SessionDebugLogger.Log("LABOR_EXCHANGE", $"Higher-educated worker exists but is unavailable for clerk auto-assignment: {fallback.DriverName}.");
        }

        return null;
    }

    private bool IsLaborExchangeReadyForApplicants(out string reason)
    {
        if (!locations.ContainsKey(LocationType.LaborExchange))
        {
            reason = "Labor Exchange is not built";
            return false;
        }

        if (!IsLaborExchangeStaffed())
        {
            reason = "Labor Exchange has no clerk on shift";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private void ClearLaborExchangePostings(string reason)
    {
        if (laborExchangePostings.Count == 0)
        {
            return;
        }

        for (int i = 0; i < laborExchangePostings.Count; i++)
        {
            ReleaseLaborExchangePostingReservation(laborExchangePostings[i]);
        }

        int count = laborExchangePostings.Count;
        laborExchangePostings.Clear();
        SessionDebugLogger.Log("LABOR_EXCHANGE", $"Cleared {count} posting(s): {reason}.");
    }

    private void PruneLaborExchangePostings()
    {
        for (int i = laborExchangePostings.Count - 1; i >= 0; i--)
        {
            LaborExchangePosting posting = laborExchangePostings[i];
            if (IsLaborExchangePostingFilled(posting))
            {
                if (IsLaborExchangeReservationActive(posting))
                {
                    continue;
                }

                ReleaseLaborExchangePostingReservation(posting);
                laborExchangePostings.RemoveAt(i);
                SessionDebugLogger.Log("LABOR_EXCHANGE", $"Posting #{posting.Id} removed because target is already filled: {GetLaborExchangePostingLabel(posting)}.");
                continue;
            }

            if (posting.ReservedWorkerId <= 0)
            {
                continue;
            }

            DriverAgent worker = GetDriverAgentById(posting.ReservedWorkerId);
            bool stillApplying = worker != null &&
                                  worker.ReservedLaborExchangePostingId == posting.Id &&
                                  worker.LifeGoal == WorkerLifeGoal.FindJob &&
                                  IsWorkerTravellingToLaborExchange(worker);
            if (!stillApplying)
            {
                SessionDebugLogger.Log("LABOR_EXCHANGE", $"Posting #{posting.Id} reservation released: worker no longer applying.");
                ReleaseLaborExchangePostingReservation(posting);
            }
        }
    }

    private bool IsLaborExchangeReservationActive(LaborExchangePosting posting)
    {
        if (posting == null || posting.ReservedWorkerId <= 0)
        {
            return false;
        }

        DriverAgent worker = GetDriverAgentById(posting.ReservedWorkerId);
        return worker != null &&
               worker.ReservedLaborExchangePostingId == posting.Id &&
               worker.LifeGoal == WorkerLifeGoal.FindJob &&
               IsWorkerTravellingToLaborExchange(worker);
    }

    private static bool IsWorkerTravellingToLaborExchange(DriverAgent worker)
    {
        return worker != null &&
               (worker.WalkPhase == DriverRescuePhase.ToLaborExchangeForJob ||
                worker.WalkPhase == DriverRescuePhase.AtLaborExchange ||
                worker.WalkPhase == DriverRescuePhase.WalkToLocalBusStop ||
                worker.WalkPhase == DriverRescuePhase.WaitingAtLocalBusStop ||
                worker.WalkPhase == DriverRescuePhase.RidingLocalBus);
    }

    private void ReleaseLaborExchangePostingReservation(LaborExchangePosting posting)
    {
        if (posting == null || posting.ReservedWorkerId <= 0)
        {
            return;
        }

        DriverAgent worker = GetDriverAgentById(posting.ReservedWorkerId);
        if (worker != null && worker.ReservedLaborExchangePostingId == posting.Id)
        {
            worker.ReservedLaborExchangePostingId = 0;
            worker.LaborExchangeInterviewTimer = 0f;
            if (worker.LifeGoal == WorkerLifeGoal.FindJob)
            {
                worker.LifeGoal = WorkerLifeGoal.Idle;
            }
        }

        posting.ReservedWorkerId = 0;
    }

    private bool TryPublishNextLaborExchangePosting()
    {
        List<LaborExchangeCandidate> candidates = new();
        AddLaborExchangeShiftCandidates(candidates);
        AddLaborExchangeBuildingCandidates(candidates);
        if (candidates.Count == 0)
        {
            if (SessionDebugLogger.IsVerboseEnabled("LABOR_EXCHANGE"))
            {
                SessionDebugLogger.LogVerbose("LABOR_EXCHANGE", "No open target vacancies found for posting generation.");
            }
            return false;
        }

        candidates.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        LaborExchangeCandidate candidate = candidates[0];
        if (laborExchangePostings.Count >= LaborExchangeMaxActivePostings &&
            !TryMakeRoomForPriorityLaborExchangePosting(candidate))
        {
            return false;
        }

        VacancyOffer offer = CalculateVacancyOffer(candidate.Kind, candidate.BuildingType, candidate.SlotIndex, candidate.ShiftIndex);
        LaborExchangePosting posting = new()
        {
            Id = nextLaborExchangePostingId++,
            Kind = candidate.Kind,
            BuildingType = candidate.BuildingType,
            BuildingInstanceId = ResolveBuildingInstanceId(candidate.BuildingType, candidate.BuildingInstanceId),
            SlotIndex = candidate.SlotIndex,
            ShiftIndex = candidate.ShiftIndex,
            TruckNumber = candidate.TruckNumber,
            CreatedAtWorldHour = GetCurrentWorldHour(),
            OfferedSalary = offer.Salary,
            ContractWorkDays = offer.ContractWorkDays,
            MarketPressure = offer.MarketPressure,
            RequiredProfessionalLevel = offer.RequiredProfessionalLevel,
            LastSalaryRevisionWorldHour = GetCurrentWorldHour()
        };

        laborExchangePostings.Add(posting);
        SessionDebugLogger.Log("LABOR_EXCHANGE", $"Posted vacancy #{posting.Id}: {GetLaborExchangePostingLabel(posting)}; salary=${posting.OfferedSalary}, contract={posting.ContractWorkDays} workdays, pressure={posting.MarketPressure}, requiredLevel={posting.RequiredProfessionalLevel}; active={laborExchangePostings.Count}/{LaborExchangeMaxActivePostings}.");
        PushFeedEvent(
            $"Labor Exchange posted: {GetLaborExchangePostingLabel(posting)} (${posting.OfferedSalary}, {posting.ContractWorkDays} workdays).",
            $"\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430: \u043d\u043e\u0432\u0430\u044f \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u044f {GetLaborExchangePostingLabel(posting)}.",
            FeedEventType.Info);
        isShiftsScreenDirty = true;
        return true;
    }

    private void UpdateLaborExchangePostingOffers()
    {
        float currentWorldHour = GetCurrentWorldHour();
        for (int i = 0; i < laborExchangePostings.Count; i++)
        {
            LaborExchangePosting posting = laborExchangePostings[i];
            if (posting == null || posting.ReservedWorkerId > 0 || IsLaborExchangePostingFilled(posting))
            {
                continue;
            }

            if (currentWorldHour - posting.LastSalaryRevisionWorldHour < 1.5f)
            {
                continue;
            }

            float ageHours = Mathf.Max(0f, currentWorldHour - posting.CreatedAtWorldHour);
            VacancyOffer offer = CalculateVacancyOffer(posting.Kind, posting.BuildingType, posting.SlotIndex, posting.ShiftIndex, ageHours);
            if (offer.Salary != posting.OfferedSalary ||
                offer.ContractWorkDays != posting.ContractWorkDays ||
                offer.RequiredProfessionalLevel != posting.RequiredProfessionalLevel)
            {
                SessionDebugLogger.Log(
                    "LABOR_EXCHANGE",
                    $"Repriced posting #{posting.Id}: {GetLaborExchangePostingLabel(posting)} salary ${posting.OfferedSalary}->{offer.Salary}, contract {posting.ContractWorkDays}->{offer.ContractWorkDays}, requiredLevel {posting.RequiredProfessionalLevel}->{offer.RequiredProfessionalLevel}, pressure={offer.MarketPressure}.");
                posting.OfferedSalary = offer.Salary;
                posting.ContractWorkDays = offer.ContractWorkDays;
                posting.MarketPressure = offer.MarketPressure;
                posting.RequiredProfessionalLevel = offer.RequiredProfessionalLevel;
                isShiftsScreenDirty = true;
            }

            posting.LastSalaryRevisionWorldHour = currentWorldHour;
        }
    }

    private void AddLaborExchangeShiftCandidates(List<LaborExchangeCandidate> candidates)
    {
        int truckPriority = GetLaborExchangeTruckDriverPriority();
        int busPriority = GetLaborExchangeBusDriverPriority();
        if (IsVacancyUnlockedForCurrentTutorial(VacancyKind.TruckDriver) &&
            locations.ContainsKey(LocationType.Parking) &&
            HasAvailableTruckInParking())
        {
            for (int i = 0; i < ShiftPresetHours.Length; i++)
            {
                if (!IsAnyTruckDriverAssignedToShift(i) &&
                    !HasLaborExchangePosting(VacancyKind.TruckDriver, LocationType.Parking, 0, -1, i))
                {
                    candidates.Add(new LaborExchangeCandidate(VacancyKind.TruckDriver, LocationType.Parking, 0, -1, i, 0, truckPriority));
                }
            }
        }

        if (IsVacancyUnlockedForCurrentTutorial(VacancyKind.BusDriver) &&
            locations.ContainsKey(LocationType.Parking) &&
            HasAvailableBusInParking())
        {
            for (int i = 0; i < ShiftPresetHours.Length; i++)
            {
                if (GetBusAssignedDriver(i) == null &&
                    !HasLaborExchangePosting(VacancyKind.BusDriver, LocationType.Parking, 0, -1, i))
                {
                    candidates.Add(new LaborExchangeCandidate(VacancyKind.BusDriver, LocationType.Parking, 0, -1, i, 0, busPriority));
                }
            }
        }
    }

    private void AddLaborExchangeBuildingCandidates(List<LaborExchangeCandidate> candidates)
    {
        if (locations.ContainsKey(LocationType.Warehouse) &&
            IsVacancyUnlockedForCurrentTutorial(VacancyKind.Production, LocationType.Warehouse))
        {
            for (int i = 0; i < WarehouseMaxWorkers; i++)
            {
                if (GetNthLogisticsWorker(LocationType.Warehouse, i) == null &&
                    !HasLaborExchangePosting(VacancyKind.Production, LocationType.Warehouse, 0, i, -1))
                {
                    candidates.Add(new LaborExchangeCandidate(VacancyKind.Production, LocationType.Warehouse, 0, i, -1, 0, 15));
                }
            }
        }

        LocationType[] buildingTypes =
        {
            LocationType.Forest,
            LocationType.Sawmill,
            LocationType.FurnitureFactory,
            LocationType.Docks,
            LocationType.Motel,
            LocationType.Bar,
            LocationType.Canteen,
            LocationType.GasStation,
            LocationType.GamblingHall,
            LocationType.Kindergarten,
            LocationType.CarMarket,
            LocationType.LaborExchange,
            LocationType.CleaningDepot
        };

        for (int ti = 0; ti < buildingTypes.Length; ti++)
        {
            LocationType buildingType = buildingTypes[ti];
            int maxSlots = GetMaxBuildingWorkerSlots(buildingType);
            if (maxSlots <= 0)
            {
                continue;
            }

            VacancyKind kind = IsProductionLocation(buildingType) ? VacancyKind.Production : VacancyKind.Service;
            if (!IsVacancyUnlockedForCurrentTutorial(kind, buildingType))
            {
                continue;
            }

            foreach (LocationData location in EnumerateAssignableBuildingLocations(buildingType))
            {
                for (int slotIndex = 0; slotIndex < maxSlots; slotIndex++)
                {
                    int shiftIndex = HasServiceWorkerSlot(buildingType)
                        ? GetBuildingWorkerShiftPresetIndex(buildingType, slotIndex)
                        : -1;
                    if (GetNthLogisticsWorker(buildingType, slotIndex, location.InstanceId) != null ||
                        HasLaborExchangePosting(kind, buildingType, location.InstanceId, slotIndex, shiftIndex))
                    {
                        continue;
                    }

                    int priority = kind == VacancyKind.Production ? 20 : 40;
                    if (buildingType == LocationType.LaborExchange)
                    {
                        priority = 12;
                    }

                    candidates.Add(new LaborExchangeCandidate(kind, buildingType, location.InstanceId, slotIndex, shiftIndex, 0, priority));
                }
            }
        }
    }

    private bool HasLaborExchangePosting(VacancyKind kind, LocationType buildingType, int buildingInstanceId, int slotIndex, int shiftIndex)
    {
        int resolvedInstanceId = ResolveBuildingInstanceId(buildingType, buildingInstanceId);
        for (int i = 0; i < laborExchangePostings.Count; i++)
        {
            LaborExchangePosting posting = laborExchangePostings[i];
            if (posting.Kind == kind &&
                posting.BuildingType == buildingType &&
                ResolveBuildingInstanceId(posting.BuildingType, posting.BuildingInstanceId) == resolvedInstanceId &&
                posting.SlotIndex == slotIndex &&
                posting.ShiftIndex == shiftIndex)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryStartLaborExchangeJobSearch(DriverAgent driver)
    {
        string readyReason = string.Empty;
        if (driver == null ||
            driver.DriverObject == null ||
            driver.ReservedLaborExchangePostingId > 0 ||
            !IsLaborExchangeReadyForApplicants(out readyReason) ||
            HasCriticalWorkerNeed(driver) ||
            !IsWorkerVacantForVacancyAssignment(driver))
        {
            if (driver != null && !string.IsNullOrEmpty(readyReason))
            {
                LogWorkerDecision(
                    driver,
                    "skip-labor-exchange",
                    readyReason,
                    verboseOnly: IsLaborExchangeReadinessSkipVerboseOnly(readyReason));
            }
            return false;
        }

        if (!TryReserveLaborExchangePostingForWorker(driver, out LaborExchangePosting posting, out string reserveReason))
        {
            LogWorkerDecision(driver, "skip-labor-exchange", reserveReason);
            return false;
        }

        Vector3 startPosition = driver.DriverObject.transform.position;
        Vector3 target = GetDriverStandPointNearLocation(LocationType.LaborExchange);
        driver.LifeGoal = WorkerLifeGoal.FindJob;
        driver.IdleWanderPauseTimer = 0f;
        driver.IdleWanderPointIndex = -1;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        ReleaseBench(driver);
        ReleaseCatInteraction(driver);
        ResetWorkerLocalBusTripState(driver);

        if (TryStartWorkerPersonalCarTrip(driver, startPosition, target, DriverRescuePhase.ToLaborExchangeForJob, "Labor Exchange job search"))
        {
            SessionDebugLogger.Log("LABOR_EXCHANGE", $"{driver.DriverName} reserved posting #{posting.Id} and drives to Labor Exchange: {GetLaborExchangePostingLabel(posting)}.");
            return true;
        }
        if (CanWorkerUsePersonalCar(driver))
        {
            ReleaseLaborExchangePostingReservation(posting);
            driver.LifeGoal = WorkerLifeGoal.Idle;
            SessionDebugLogger.Log("LABOR_EXCHANGE", $"{driver.DriverName} released posting #{posting.Id}: no personal car route to Labor Exchange.");
            LogWorkerDecision(driver, "labor-exchange-car-path-blocked", $"posting #{posting.Id}: no personal car route to Labor Exchange", true);
            return false;
        }

        if (TryStartWorkerLocalBusTrip(driver, startPosition, target, DriverRescuePhase.ToLaborExchangeForJob, "Labor Exchange job search"))
        {
            SessionDebugLogger.Log("LABOR_EXCHANGE", $"{driver.DriverName} reserved posting #{posting.Id} and travels by bus: {GetLaborExchangePostingLabel(posting)}.");
            return true;
        }

        driver.WalkPhase = DriverRescuePhase.ToLaborExchangeForJob;
        driver.WalkTargetWorld = target;
        driver.WalkAnimationTime = 0f;
        if (!BuildDriverWalkPath(driver, startPosition, target))
        {
            ReleaseLaborExchangePostingReservation(posting);
            driver.WalkPhase = DriverRescuePhase.None;
            driver.WalkTargetWorld = startPosition;
            driver.LifeGoal = WorkerLifeGoal.Idle;
            SessionDebugLogger.Log(
                "LABOR_EXCHANGE",
                $"{driver.DriverName} released posting #{posting.Id}: no safe walk path to Labor Exchange access cell.");
            LogWorkerDecision(driver, "labor-exchange-path-blocked", $"posting #{posting.Id}: no safe walk path to Labor Exchange", true);
            return false;
        }

        SessionDebugLogger.Log("LABOR_EXCHANGE", $"{driver.DriverName} reserved posting #{posting.Id} and walks to Labor Exchange: {GetLaborExchangePostingLabel(posting)}.");
        LogWorkerDecision(driver, "labor-exchange-apply", $"posting #{posting.Id}: {GetLaborExchangePostingLabel(posting)}", true);
        return true;
    }

    private static bool IsLaborExchangeReadinessSkipVerboseOnly(string reason)
    {
        return reason == "Labor Exchange is not built" ||
               reason == "Labor Exchange has no clerk on shift";
    }

    private bool TryReserveLaborExchangePostingForWorker(DriverAgent driver, out LaborExchangePosting posting, out string reason)
    {
        posting = null;
        reason = "no suitable labor exchange postings";
        int bestScore = int.MinValue;
        for (int i = 0; i < laborExchangePostings.Count; i++)
        {
            LaborExchangePosting candidate = laborExchangePostings[i];
            if (candidate.ReservedWorkerId > 0)
            {
                continue;
            }

            if (!CanWorkerFillLaborExchangePosting(candidate, driver, out reason))
            {
                continue;
            }

            int score = candidate.OfferedSalary * 3 - candidate.ContractWorkDays + candidate.MarketPressure;
            if (score > bestScore)
            {
                bestScore = score;
                posting = candidate;
            }
        }

        if (posting == null)
        {
            return false;
        }

        posting.ReservedWorkerId = driver.DriverId;
        driver.ReservedLaborExchangePostingId = posting.Id;
        return true;
    }

    private bool CanWorkerFillLaborExchangePosting(LaborExchangePosting posting, DriverAgent driver, out string reason)
    {
        reason = string.Empty;
        if (posting == null || driver == null)
        {
            reason = "missing worker or posting";
            return false;
        }

        if (!IsWorkerVacantForVacancyAssignment(driver))
        {
            reason = "worker is not vacant";
            return false;
        }

        if (IsLaborExchangePostingFilled(posting))
        {
            reason = "posting already filled";
            return false;
        }

        if ((posting.Kind == VacancyKind.Production || posting.Kind == VacancyKind.Service) &&
            !CanWorkerMeetBuildingEducationRequirement(driver, posting.BuildingType, out reason))
        {
            return false;
        }

        if (!CanWorkerMeetProfessionalRequirement(driver, posting.Kind, posting.BuildingType, posting.RequiredProfessionalLevel, out reason))
        {
            return false;
        }

        return posting.Kind switch
        {
            VacancyKind.Production or VacancyKind.Service =>
                IsLocationInstanceBuilt(posting.BuildingType, posting.BuildingInstanceId) &&
                GetNthLogisticsWorker(posting.BuildingType, posting.SlotIndex, posting.BuildingInstanceId) == null,
            VacancyKind.TruckDriver =>
                posting.ShiftIndex >= 0 &&
                posting.ShiftIndex < ShiftPresetHours.Length &&
                !IsAnyTruckDriverAssignedToShift(posting.ShiftIndex) &&
                HasAvailableTruckInParking(),
            VacancyKind.BusDriver =>
                posting.ShiftIndex >= 0 &&
                posting.ShiftIndex < ShiftPresetHours.Length &&
                GetBusAssignedDriver(posting.ShiftIndex) == null &&
                HasAvailableBusInParking(),
            _ => false
        };
    }

    private bool IsLaborExchangePostingFilled(LaborExchangePosting posting)
    {
        if (posting == null)
        {
            return true;
        }

        return posting.Kind switch
        {
            VacancyKind.Production or VacancyKind.Service =>
                !IsLocationInstanceBuilt(posting.BuildingType, posting.BuildingInstanceId) ||
                GetNthLogisticsWorker(posting.BuildingType, posting.SlotIndex, posting.BuildingInstanceId) != null,
            VacancyKind.TruckDriver =>
                posting.ShiftIndex < 0 ||
                posting.ShiftIndex >= ShiftPresetHours.Length ||
                IsAnyTruckDriverAssignedToShift(posting.ShiftIndex) ||
                !HasAvailableTruckInParking(),
            VacancyKind.BusDriver =>
                posting.ShiftIndex < 0 ||
                posting.ShiftIndex >= ShiftPresetHours.Length ||
                GetBusAssignedDriver(posting.ShiftIndex) != null ||
                !HasAvailableBusInParking(),
            _ => true
        };
    }

    private void StartLaborExchangeInterview(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        driver.WalkPhase = DriverRescuePhase.AtLaborExchange;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;
        driver.LaborExchangeInterviewTimer = LaborExchangeInterviewDuration;
        driver.IdleActivityTimer = LaborExchangeInterviewDuration;
        laborExchangeApplicantsToday++;
        EnterWorkerServiceInterior(driver, LocationType.LaborExchange);
        SessionDebugLogger.Log("LABOR_EXCHANGE", $"{driver.DriverName} entered Labor Exchange interview; reservedPosting={driver.ReservedLaborExchangePostingId}.");
        isDriversScreenDirty = true;
    }

    private void CompleteLaborExchangeApplication(DriverAgent driver, Vector3 exitPosition)
    {
        if (driver == null)
        {
            return;
        }

        int postingId = driver.ReservedLaborExchangePostingId;
        LaborExchangePosting posting = FindLaborExchangePosting(postingId);
        driver.ReservedLaborExchangePostingId = 0;
        driver.LaborExchangeInterviewTimer = 0f;
        driver.IdleActivityTimer = 0f;
        driver.WalkPhase = DriverRescuePhase.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        ResetWorkerLocalBusTripState(driver);

        bool assigned = false;
        string resultReason = string.Empty;
        if (posting == null)
        {
            resultReason = $"posting #{postingId} no longer exists";
        }
        else if (!CanWorkerFillLaborExchangePosting(posting, driver, out resultReason))
        {
            posting.ReservedWorkerId = 0;
        }
        else
        {
            assigned = TryAssignLaborExchangePosting(driver, posting);
            resultReason = assigned ? "assigned" : "assignment failed";
        }

        if (assigned)
        {
            laborExchangePostings.Remove(posting);
            driver.LifeGoal = WorkerLifeGoal.None;
            SessionDebugLogger.Log("LABOR_EXCHANGE", $"{driver.DriverName} hired via Labor Exchange posting #{posting.Id}: {GetLaborExchangePostingLabel(posting)}.");
            PushFeedEvent(
                $"{driver.DriverName} found work: {GetLaborExchangePostingLabel(posting)}.",
                $"{driver.DriverName} \u0443\u0441\u0442\u0440\u043e\u0438\u043b\u0441\u044f \u0447\u0435\u0440\u0435\u0437 \u0411\u0438\u0440\u0436\u0443 \u0442\u0440\u0443\u0434\u0430.",
                FeedEventType.Success);
        }
        else
        {
            driver.LifeGoal = WorkerLifeGoal.Idle;
            driver.IdleWanderPauseTimer = Random.Range(0.5f, 1.5f);
            SessionDebugLogger.Log("LABOR_EXCHANGE", $"{driver.DriverName} left Labor Exchange without assignment: {resultReason}.");
            if (exitPosition != Vector3.zero)
            {
                driver.WalkPhase = DriverRescuePhase.IdleWander;
                if (!BuildDriverWalkPath(driver, exitPosition, FindDriverIdleWanderTarget(driver, exitPosition)))
                {
                    driver.WalkPhase = DriverRescuePhase.None;
                    driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
                }
            }
        }

        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
    }

    private LaborExchangePosting FindLaborExchangePosting(int postingId)
    {
        for (int i = 0; i < laborExchangePostings.Count; i++)
        {
            if (laborExchangePostings[i].Id == postingId)
            {
                return laborExchangePostings[i];
            }
        }

        return null;
    }

    private bool TryAssignLaborExchangePosting(DriverAgent worker, LaborExchangePosting posting)
    {
        int oldShiftIndex = selectedVacancyShiftIndex;
        int oldTruckNumber = selectedVacancyTruckNumber;
        int oldVacancyIndex = selectedVacancyIndex;

        try
        {
            switch (posting.Kind)
            {
                case VacancyKind.Production:
                case VacancyKind.Service:
                    AssignWorkerToBuilding(worker, FindLogisticsSlot(posting.BuildingType, posting.SlotIndex, posting.BuildingInstanceId));
                    bool buildingAssigned = worker.DutyMode == DriverDutyMode.Logistics &&
                                            IsDriverAssignedToBuildingSlot(worker, posting.BuildingType, posting.BuildingInstanceId);
                    if (buildingAssigned)
                    {
                        ApplyWorkerContract(worker, posting.Kind, posting.BuildingType, posting.SlotIndex, posting.ShiftIndex, posting.OfferedSalary, posting.ContractWorkDays, posting.RequiredProfessionalLevel, $"Labor Exchange posting #{posting.Id}", posting.BuildingInstanceId);
                    }
                    return buildingAssigned;
                case VacancyKind.TruckDriver:
                    selectedVacancyShiftIndex = posting.ShiftIndex;
                    selectedVacancyTruckNumber = posting.TruckNumber;
                    bool truckAssigned = AssignTruckDriverVacancy(worker);
                    if (truckAssigned)
                    {
                        ApplyWorkerContract(worker, posting.Kind, posting.BuildingType, posting.SlotIndex, posting.ShiftIndex, posting.OfferedSalary, posting.ContractWorkDays, posting.RequiredProfessionalLevel, $"Labor Exchange posting #{posting.Id}");
                    }
                    return truckAssigned;
                case VacancyKind.BusDriver:
                    AssignDriverToBusSlot(worker, posting.ShiftIndex);
                    bool busAssigned = GetBusAssignedDriver(posting.ShiftIndex) == worker;
                    if (busAssigned)
                    {
                        ApplyWorkerContract(worker, posting.Kind, posting.BuildingType, posting.SlotIndex, posting.ShiftIndex, posting.OfferedSalary, posting.ContractWorkDays, posting.RequiredProfessionalLevel, $"Labor Exchange posting #{posting.Id}");
                    }
                    return busAssigned;
                default:
                    return false;
            }
        }
        finally
        {
            selectedVacancyShiftIndex = oldShiftIndex;
            selectedVacancyTruckNumber = oldTruckNumber;
            selectedVacancyIndex = oldVacancyIndex;
        }
    }

    private int CountAvailableLaborExchangePostings()
    {
        int count = 0;
        for (int i = 0; i < laborExchangePostings.Count; i++)
        {
            if (laborExchangePostings[i].ReservedWorkerId <= 0 && !IsLaborExchangePostingFilled(laborExchangePostings[i]))
            {
                count++;
            }
        }

        return count;
    }

    private int CountReservedLaborExchangePostings()
    {
        int count = 0;
        for (int i = 0; i < laborExchangePostings.Count; i++)
        {
            if (laborExchangePostings[i].ReservedWorkerId > 0)
            {
                count++;
            }
        }

        return count;
    }

    private string FormatLaborExchangePostingLines(int maxLines, bool ru)
    {
        int emitted = 0;
        string text = string.Empty;
        for (int i = 0; i < laborExchangePostings.Count && emitted < maxLines; i++)
        {
            LaborExchangePosting posting = laborExchangePostings[i];
            if (posting.ReservedWorkerId > 0 || IsLaborExchangePostingFilled(posting))
            {
                continue;
            }

            text += (text.Length > 0 ? "\n" : string.Empty) + FormatValueLine($"{emitted + 1}.", $"{GetLaborExchangePostingDisplayLabel(posting, ru)} ({FormatVacancyOffer(new VacancyOffer(posting.OfferedSalary, posting.ContractWorkDays, posting.MarketPressure, posting.RequiredProfessionalLevel), ru)})");
            emitted++;
        }

        if (emitted == 0)
        {
            text = FormatValueLine(ru ? "\u0421\u043f\u0438\u0441\u043e\u043a" : "List", ru ? "\u043f\u0443\u0441\u0442\u043e" : "empty");
        }

        return text;
    }

    private string GetLaborExchangePostingLabel(LaborExchangePosting posting)
    {
        if (posting == null)
        {
            return "Unknown vacancy";
        }

        return posting.Kind switch
        {
            VacancyKind.Production or VacancyKind.Service =>
                $"{GetBuildingInstanceDisplayName(posting.BuildingType, posting.BuildingInstanceId)} slot {posting.SlotIndex + 1}",
            VacancyKind.TruckDriver =>
                posting.ShiftIndex >= 0 && posting.ShiftIndex < ShiftNames.Length
                    ? $"Truck Driver {ShiftNames[posting.ShiftIndex]}"
                    : "Truck Driver",
            VacancyKind.BusDriver =>
                posting.ShiftIndex >= 0 && posting.ShiftIndex < ShiftNames.Length
                    ? $"Bus Driver {ShiftNames[posting.ShiftIndex]}"
                    : "Bus Driver",
            _ => "Unknown vacancy"
        };
    }

    private string GetLaborExchangePostingDisplayLabel(LaborExchangePosting posting, bool ru)
    {
        if (posting == null)
        {
            return ru ? "\u043d\u0435\u0438\u0437\u0432\u0435\u0441\u0442\u043d\u043e" : "unknown";
        }

        if (posting.Kind == VacancyKind.Production || posting.Kind == VacancyKind.Service)
        {
            string role = L(GetBuildingWorkerRoleLabel(posting.BuildingType));
            string building = L(GetBuildingInstanceDisplayName(posting.BuildingType, posting.BuildingInstanceId));
            string slot = GetMaxBuildingWorkerSlots(posting.BuildingType) > 1 ? $" #{posting.SlotIndex + 1}" : string.Empty;
            return $"{role}: {building}{slot}";
        }

        if (posting.ShiftIndex >= 0 && posting.ShiftIndex < ShiftPresetHours.Length)
        {
            string title = posting.Kind == VacancyKind.BusDriver
                ? (ru ? "\u0412\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0430" : "Bus Driver")
                : (ru ? "\u0412\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0430" : "Truck Driver");
            return $"{title}: {L(ShiftNames[posting.ShiftIndex])}";
        }

        return GetLaborExchangePostingLabel(posting);
    }
}
