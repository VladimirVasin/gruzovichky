using System.Collections.Generic;

public partial class GameBootstrap
{
    private bool HasAvailableWorkOpportunityForWorker(DriverAgent worker)
    {
        return HasAvailableWorkOpportunityForWorker(worker, requireImmediateAvailability: true);
    }

    private bool HasSufficientAvailableWorkForUnassignedWorkers(out int availableWork, out int unassignedWorkers)
    {
        availableWork = CountOpenWorkerVacancyDemand();
        unassignedWorkers = 0;
        int workersWithOpportunity = 0;

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (!IsWorkerEligibleForWorkAvailability(worker, requireImmediateAvailability: false))
            {
                continue;
            }

            unassignedWorkers++;
            if (HasAvailableWorkOpportunityForWorker(worker, requireImmediateAvailability: false))
            {
                workersWithOpportunity++;
            }
        }

        return unassignedWorkers > 0 &&
               availableWork >= unassignedWorkers &&
               workersWithOpportunity >= unassignedWorkers;
    }

    private bool HasAvailableWorkOpportunityForWorker(DriverAgent worker, bool requireImmediateAvailability)
    {
        if (worker == null)
        {
            return false;
        }

        if (worker.ReservedLaborExchangePostingId > 0)
        {
            return true;
        }

        if (!IsWorkerEligibleForWorkAvailability(worker, requireImmediateAvailability))
        {
            return false;
        }

        return HasAvailableLaborExchangePostingForWorker(worker, requireImmediateAvailability) ||
               HasAvailableOpenVacancyForWorker(worker, requireImmediateAvailability);
    }

    private bool HasAvailableLaborExchangePostingForWorker(DriverAgent worker, bool requireImmediateAvailability)
    {
        for (int i = 0; i < laborExchangePostings.Count; i++)
        {
            LaborExchangePosting posting = laborExchangePostings[i];
            if (posting == null || posting.ReservedWorkerId > 0 || IsLaborExchangePostingFilled(posting))
            {
                continue;
            }

            if (CanWorkerFillWorkOpportunity(
                    worker,
                    posting.Kind,
                    posting.BuildingType,
                    posting.BuildingInstanceId,
                    posting.SlotIndex,
                    posting.ShiftIndex,
                    posting.RequiredProfessionalLevel,
                    requireImmediateAvailability))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAvailableOpenVacancyForWorker(DriverAgent worker, bool requireImmediateAvailability)
    {
        List<LaborExchangeCandidate> candidates = new();
        AddLaborExchangeShiftCandidates(candidates);
        AddLaborExchangeBuildingCandidates(candidates);
        for (int i = 0; i < candidates.Count; i++)
        {
            LaborExchangeCandidate candidate = candidates[i];
            VacancyOffer offer = CalculateVacancyOffer(
                candidate.Kind,
                candidate.BuildingType,
                candidate.SlotIndex,
                candidate.ShiftIndex);
            if (CanWorkerFillWorkOpportunity(
                    worker,
                    candidate.Kind,
                    candidate.BuildingType,
                    candidate.BuildingInstanceId,
                    candidate.SlotIndex,
                    candidate.ShiftIndex,
                    offer.RequiredProfessionalLevel,
                    requireImmediateAvailability))
            {
                return true;
            }
        }

        return false;
    }

    private bool CanWorkerFillWorkOpportunity(
        DriverAgent worker,
        VacancyKind kind,
        LocationType buildingType,
        int buildingInstanceId,
        int slotIndex,
        int shiftIndex,
        int requiredProfessionalLevel,
        bool requireImmediateAvailability)
    {
        if (!IsWorkerEligibleForWorkAvailability(worker, requireImmediateAvailability))
        {
            return false;
        }

        if ((kind == VacancyKind.Production || kind == VacancyKind.Service) &&
            !CanWorkerMeetBuildingEducationRequirement(worker, buildingType, out _))
        {
            return false;
        }

        if (!CanWorkerMeetProfessionalRequirement(worker, kind, buildingType, requiredProfessionalLevel, out _))
        {
            return false;
        }

        return kind switch
        {
            VacancyKind.Production or VacancyKind.Service =>
                IsLocationInstanceBuilt(buildingType, buildingInstanceId) &&
                GetNthLogisticsWorker(buildingType, slotIndex, buildingInstanceId) == null,
            VacancyKind.TruckDriver =>
                shiftIndex >= 0 &&
                shiftIndex < ShiftPresetHours.Length &&
                !IsAnyTruckDriverAssignedToShift(shiftIndex) &&
                HasAvailableTruckInParking(),
            VacancyKind.BusDriver =>
                shiftIndex >= 0 &&
                shiftIndex < ShiftPresetHours.Length &&
                GetBusAssignedDriver(shiftIndex) == null &&
                HasAvailableBusInParking() &&
                HasWorkingLocalBusStopNetwork(),
            _ => false
        };
    }

    private bool IsWorkerEligibleForWorkAvailability(DriverAgent worker, bool requireImmediateAvailability)
    {
        if (requireImmediateAvailability)
        {
            return IsWorkerVacantForVacancyAssignment(worker);
        }

        return worker != null &&
               !worker.IsArrivingByBus &&
               !worker.IsLeavingTown &&
               !worker.HasDepartedTown &&
               worker.DutyMode == DriverDutyMode.Local &&
               worker.ShiftStartHour < 0 &&
               worker.AssignedTruckNumber <= 0 &&
               !worker.AssignedBuildingType.HasValue &&
               !IsDriverBusDriver(worker) &&
               !IsDriverIntercity(worker) &&
               !IsDriverOnActiveTradeRun(worker) &&
               !IsBusDriverOnActiveRoute(worker);
    }
}
