using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private readonly struct VacancyOffer
    {
        public VacancyOffer(int salary, int contractWorkDays, int marketPressure, int requiredProfessionalLevel = 1)
        {
            Salary = salary;
            ContractWorkDays = contractWorkDays;
            MarketPressure = marketPressure;
            RequiredProfessionalLevel = requiredProfessionalLevel;
        }

        public readonly int Salary;
        public readonly int ContractWorkDays;
        public readonly int MarketPressure;
        public readonly int RequiredProfessionalLevel;
    }

    private bool IsWorkingDay() => !IsWeekend();

    private VacancyOffer CalculateVacancyOffer(VacancyKind kind, LocationType buildingType, int slotIndex, int shiftIndex, float vacancyAgeWorldHours = 0f)
    {
        int openDemand = CountOpenWorkerVacancyDemand();
        int freeWorkers = CountFreeMigrationWorkers();
        int marketPressure = Mathf.Clamp(openDemand * 8 - freeWorkers * 6 + recentWorkerDeparturesToday * 8, -30, 90);
        int baseSalary = GetBaseVacancySalary(kind, buildingType, shiftIndex);
        int ageBonus = Mathf.Clamp(Mathf.FloorToInt(vacancyAgeWorldHours / 2f) * 2, 0, 18);
        int shortageBonus = Mathf.Max(0, marketPressure / 4);
        int abundancePenalty = marketPressure < 0 ? Mathf.Abs(marketPressure) / 5 : 0;
        int baseOfferSalary = RoundToNearestFive(Mathf.Clamp(baseSalary + ageBonus + shortageBonus - abundancePenalty, 15, 90));
        int requiredProfessionalLevel = DetermineVacancyProfessionalLevelRequirement(kind, buildingType, slotIndex, shiftIndex, marketPressure, vacancyAgeWorldHours);
        int salary = ApplyVacancyProfessionalSalaryPremium(baseOfferSalary, requiredProfessionalLevel);

        int baseDays = kind switch
        {
            VacancyKind.TruckDriver => 5,
            VacancyKind.BusDriver => 6,
            VacancyKind.Production => buildingType == LocationType.Warehouse ? 7 : 8,
            VacancyKind.Service => 8,
            _ => 6
        };
        int stableJitter = StableVacancyJitter(kind, buildingType, slotIndex, shiftIndex);
        int pressureShortening = Mathf.Max(0, marketPressure / 25);
        int contractDays = Mathf.Clamp(baseDays + stableJitter - pressureShortening, 3, 10);
        return new VacancyOffer(salary, contractDays, marketPressure, requiredProfessionalLevel);
    }

    private int GetBaseVacancySalary(VacancyKind kind, LocationType buildingType, int shiftIndex)
    {
        int salary = kind switch
        {
            VacancyKind.TruckDriver => 48,
            VacancyKind.BusDriver => 42,
            VacancyKind.Intercity => 52,
            VacancyKind.Production => buildingType == LocationType.Warehouse ? 34 : 32,
            VacancyKind.Service => buildingType == LocationType.LaborExchange ? 44 : 24,
            _ => 25
        };

        if (buildingType == LocationType.GasStation || buildingType == LocationType.CarMarket)
        {
            salary += 5;
        }

        if (shiftIndex >= 0 && shiftIndex < ShiftPresetHours.Length)
        {
            int shiftHour = ShiftPresetHours[shiftIndex];
            if (shiftHour >= 18 || shiftHour < 6)
            {
                salary += 6;
            }
        }

        return salary;
    }

    private int StableVacancyJitter(VacancyKind kind, LocationType buildingType, int slotIndex, int shiftIndex)
    {
        int hash = ((int)kind * 73856093) ^ ((int)buildingType * 19349663) ^ ((slotIndex + 17) * 83492791) ^ ((shiftIndex + 31) * 265443576);
        hash ^= currentDay * 374761393;
        return Mathf.Abs(hash % 3) - 1;
    }

    private static int RoundToNearestFive(int value) => Mathf.RoundToInt(value / 5f) * 5;

    private void ApplyVacancyOfferToViewModel(VacancyViewModel vacancy, int shiftIndex = -1)
    {
        if (vacancy == null)
        {
            return;
        }

        if (vacancy.AssignedWorker != null)
        {
            vacancy.OfferSalary = vacancy.AssignedWorker.Salary;
            vacancy.ContractWorkDays = vacancy.AssignedWorker.ContractTotalWorkDays > 0
                ? Mathf.Max(0, vacancy.AssignedWorker.ContractTotalWorkDays - vacancy.AssignedWorker.ContractWorkedDays)
                : 0;
            vacancy.MarketPressure = 0;
            vacancy.RequiredProfessionalLevel = vacancy.AssignedWorker.ContractRequiredProfessionalLevel;
            return;
        }

        int actualShiftIndex = shiftIndex >= 0 ? shiftIndex : vacancy.ShiftIndex;
        VacancyOffer offer = CalculateVacancyOffer(vacancy.Kind, vacancy.BuildingType, vacancy.SlotIndex, actualShiftIndex);
        vacancy.OfferSalary = offer.Salary;
        vacancy.ContractWorkDays = offer.ContractWorkDays;
        vacancy.MarketPressure = offer.MarketPressure;
        vacancy.RequiredProfessionalLevel = offer.RequiredProfessionalLevel;
    }

    private VacancyOffer GetCurrentVacancyOffer(VacancyViewModel vacancy)
    {
        if (vacancy == null)
        {
            return new VacancyOffer(0, 0, 0);
        }

        if (vacancy.AssignedWorker != null)
        {
            return new VacancyOffer(vacancy.AssignedWorker.Salary, Mathf.Max(0, vacancy.AssignedWorker.ContractTotalWorkDays - vacancy.AssignedWorker.ContractWorkedDays), 0);
        }

        int shiftIndex = selectedVacancyShiftIndex >= 0 ? selectedVacancyShiftIndex : vacancy.ShiftIndex;
        int slotIndex = IsGroupedWarehouseVacancy(vacancy) && selectedVacancyShiftIndex >= 0 ? selectedVacancyShiftIndex : vacancy.SlotIndex;
        return CalculateVacancyOffer(vacancy.Kind, vacancy.BuildingType, slotIndex, shiftIndex);
    }

    private string FormatVacancyOffer(VacancyOffer offer, bool ru)
    {
        if (offer.Salary <= 0)
        {
            return ru ? "\u0431\u0435\u0437 \u043e\u0444\u0444\u0435\u0440\u0430" : "no offer";
        }

        return ru
            ? $"${offer.Salary}/\u0434\u0435\u043d\u044c, {offer.ContractWorkDays} \u0440\u0430\u0431. \u0434\u043d., \u0443\u0440. {Mathf.Max(1, offer.RequiredProfessionalLevel)}+"
            : $"${offer.Salary}/day, {offer.ContractWorkDays} workdays, lvl {Mathf.Max(1, offer.RequiredProfessionalLevel)}+";
    }

    private int EstimateAverageOpenVacancySalary()
    {
        int salaryTotal = 0;
        int count = 0;
        List<LaborExchangeCandidate> candidates = new();
        AddLaborExchangeShiftCandidates(candidates);
        AddLaborExchangeBuildingCandidates(candidates);
        for (int i = 0; i < candidates.Count; i++)
        {
            LaborExchangeCandidate candidate = candidates[i];
            VacancyOffer offer = CalculateVacancyOffer(candidate.Kind, candidate.BuildingType, candidate.SlotIndex, candidate.ShiftIndex);
            salaryTotal += offer.Salary;
            count++;
        }

        for (int i = 0; i < laborExchangePostings.Count; i++)
        {
            LaborExchangePosting posting = laborExchangePostings[i];
            if (posting == null || posting.ReservedWorkerId > 0 || IsLaborExchangePostingFilled(posting))
            {
                continue;
            }

            salaryTotal += posting.OfferedSalary;
            count++;
        }

        return count > 0 ? Mathf.RoundToInt((float)salaryTotal / count) : 0;
    }

    private string FormatWorkerSalaryContract(DriverAgent driver, bool ru)
    {
        if (driver == null)
        {
            return "$0";
        }

        if (driver.ContractTotalWorkDays <= 0)
        {
            return ru ? $"${driver.Salary}/\u0434\u0435\u043d\u044c, \u0431\u0435\u0437 \u043a\u043e\u043d\u0442\u0440\u0430\u043a\u0442\u0430" : $"${driver.Salary}/day, no contract";
        }

        int remaining = Mathf.Max(0, driver.ContractTotalWorkDays - driver.ContractWorkedDays);
        string requirement = FormatVacancyProfessionalRequirement(
            driver.ContractVacancyKind,
            driver.ContractBuildingType ?? LocationType.Parking,
            driver.ContractRequiredProfessionalLevel,
            ru);
        return ru
            ? $"${driver.Salary}/\u0434\u0435\u043d\u044c, {remaining}/{driver.ContractTotalWorkDays} \u0440\u0430\u0431. \u0434\u043d., {requirement}"
            : $"${driver.Salary}/day, {remaining}/{driver.ContractTotalWorkDays} workdays, {requirement}";
    }

    private void ApplyWorkerContract(DriverAgent worker, VacancyKind kind, LocationType buildingType, int slotIndex, int shiftIndex, int salary, int contractWorkDays, int requiredProfessionalLevel, string source, int buildingInstanceId = 0)
    {
        if (worker == null || kind == VacancyKind.None)
        {
            return;
        }

        worker.Salary = Mathf.Max(0, salary);
        worker.ContractSalary = worker.Salary;
        worker.ContractTotalWorkDays = Mathf.Clamp(contractWorkDays, 3, 10);
        worker.ContractWorkedDays = 0;
        worker.ContractStartedDay = currentDay;
        worker.ContractVacancyKind = kind;
        worker.ContractBuildingType = buildingType;
        worker.ContractBuildingInstanceId = ResolveBuildingInstanceId(buildingType, buildingInstanceId);
        worker.ContractSlotIndex = slotIndex;
        worker.ContractShiftIndex = shiftIndex;
        worker.ContractProfessionalTrack = GetVacancyProfessionalTrack(kind, buildingType);
        worker.ContractRequiredProfessionalLevel = Mathf.Clamp(requiredProfessionalLevel, 1, WorkerProfessionalMaxLevel);
        int workerLevel = GetWorkerProfessionalLevel(worker, worker.ContractProfessionalTrack);
        SessionDebugLogger.Log("CONTRACT", $"{worker.DriverName} signed {kind} contract from {source}: salary=${worker.Salary}, days={worker.ContractTotalWorkDays}, building={buildingType}, instance={worker.ContractBuildingInstanceId}, slot={slotIndex}, shift={shiftIndex}, track={worker.ContractProfessionalTrack}, requiredLevel={worker.ContractRequiredProfessionalLevel}, workerLevel={workerLevel}.");
    }

    private void ClearWorkerContract(DriverAgent worker, string reason)
    {
        if (worker == null)
        {
            return;
        }

        if (worker.ContractTotalWorkDays > 0)
        {
            SessionDebugLogger.Log("CONTRACT", $"{worker.DriverName} contract cleared: {reason}.");
        }

        worker.ContractSalary = 0;
        worker.ContractTotalWorkDays = 0;
        worker.ContractWorkedDays = 0;
        worker.ContractStartedDay = 0;
        worker.ContractVacancyKind = VacancyKind.None;
        worker.ContractBuildingType = null;
        worker.ContractBuildingInstanceId = 0;
        worker.ContractSlotIndex = -1;
        worker.ContractShiftIndex = -1;
        worker.ContractProfessionalTrack = WorkerProfessionalTrack.None;
        worker.ContractRequiredProfessionalLevel = 1;
    }

    private void AdvanceWorkerContractAfterPaidShift(DriverAgent worker)
    {
        if (worker == null || worker.ContractTotalWorkDays <= 0)
        {
            return;
        }

        worker.ContractWorkedDays = Mathf.Clamp(worker.ContractWorkedDays + 1, 0, worker.ContractTotalWorkDays);
        SessionDebugLogger.Log("CONTRACT", $"{worker.DriverName} completed contract workday {worker.ContractWorkedDays}/{worker.ContractTotalWorkDays}.");
        RecordWorkerProfessionalDay(worker);
        if (worker.ContractWorkedDays < worker.ContractTotalWorkDays)
        {
            return;
        }

        CompleteWorkerContract(worker);
    }

    private void CompleteWorkerContract(DriverAgent worker)
    {
        if (worker == null)
        {
            return;
        }

        VacancyKind kind = worker.ContractVacancyKind;
        string label = kind == VacancyKind.None ? "work" : kind.ToString();
        ReleaseWorkerAssignmentAfterContract(worker);
        ClearWorkerContract(worker, "contract completed");
        PushFeedEvent(
            $"{worker.DriverName} completed a {label} contract.",
            $"{worker.DriverName} \u0437\u0430\u0432\u0435\u0440\u0448\u0438\u043b \u043a\u043e\u043d\u0442\u0440\u0430\u043a\u0442.",
            FeedEventType.Info);
        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
    }

    private void ReleaseWorkerAssignmentAfterContract(DriverAgent worker)
    {
        if (worker == null)
        {
            return;
        }

        if (IsDriverBusDriver(worker))
        {
            int busSlot = GetBusDriverShiftSlotIndex(worker);
            if (busSlot >= 0)
            {
                busDriverShiftIds[busSlot] = 0;
            }
        }

        TruckAgent assignedTruck = GetAssignedTruckForDriver(worker);
        if (assignedTruck != null)
        {
            if (assignedTruck.Driver == worker)
            {
                assignedTruck.Driver = null;
            }

            RemoveDriverFromTruckRoster(assignedTruck, worker);
        }

        if (IsDriverIntercity(worker) && intercityDriverId == worker.DriverId)
        {
            intercityDriverId = 0;
        }

        if (worker.DutyMode == DriverDutyMode.Logistics && worker.AssignedBuildingType.HasValue)
        {
            SetDriverDutyMode(worker, DriverDutyMode.Local);
        }

        worker.AssignedTruckNumber = 0;
        worker.ShiftStartHour = -1;
        worker.IsOnActiveShift = false;
        worker.WaitingForShiftAtParking = false;
        worker.NeedsShiftEndReturn = false;
        worker.IsShiftSalaryPending = false;
        worker.DutyMode = DriverDutyMode.Local;
        worker.AssignedBuildingType = null;
        worker.AssignedBuildingInstanceId = 0;
        worker.AssignedBuildingSlotIndex = -1;
        SessionDebugLogger.Log("CONTRACT", $"{worker.DriverName} returned to the free worker pool after contract completion.");
    }
}
