using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerProfessionalMaxLevel = 3;
    private const int WorkerProfessionalLevel2Days = 3;
    private const int WorkerProfessionalLevel3Days = 9;

    private WorkerProfessionalTrack GetVacancyProfessionalTrack(VacancyKind kind, LocationType buildingType)
    {
        return kind switch
        {
            VacancyKind.TruckDriver or VacancyKind.BusDriver or VacancyKind.Intercity => WorkerProfessionalTrack.Logistics,
            VacancyKind.Production => WorkerProfessionalTrack.Production,
            VacancyKind.Service => WorkerProfessionalTrack.Service,
            _ => IsProductionLocation(buildingType) ? WorkerProfessionalTrack.Production : WorkerProfessionalTrack.Service
        };
    }

    private int GetWorkerProfessionalExperienceDays(DriverAgent worker, WorkerProfessionalTrack track)
    {
        if (worker == null)
        {
            return 0;
        }

        return track switch
        {
            WorkerProfessionalTrack.Logistics => worker.LogisticsExperienceDays,
            WorkerProfessionalTrack.Production => worker.ProductionExperienceDays,
            WorkerProfessionalTrack.Service => worker.ServiceExperienceDays,
            _ => 0
        };
    }

    private int GetWorkerProfessionalLevel(DriverAgent worker, WorkerProfessionalTrack track)
    {
        int days = GetWorkerProfessionalExperienceDays(worker, track);

        if (days >= WorkerProfessionalLevel3Days)
        {
            return 3;
        }

        return days >= WorkerProfessionalLevel2Days ? 2 : 1;
    }

    private void AddWorkerProfessionalExperienceDay(DriverAgent worker, WorkerProfessionalTrack track)
    {
        if (worker == null || track == WorkerProfessionalTrack.None)
        {
            return;
        }

        switch (track)
        {
            case WorkerProfessionalTrack.Logistics:
                worker.LogisticsExperienceDays++;
                break;
            case WorkerProfessionalTrack.Production:
                worker.ProductionExperienceDays++;
                break;
            case WorkerProfessionalTrack.Service:
                worker.ServiceExperienceDays++;
                break;
        }
    }

    private void RecordWorkerProfessionalDay(DriverAgent worker)
    {
        if (worker == null || worker.ContractVacancyKind == VacancyKind.None)
        {
            return;
        }

        WorkerProfessionalTrack track = worker.ContractProfessionalTrack != WorkerProfessionalTrack.None
            ? worker.ContractProfessionalTrack
            : GetVacancyProfessionalTrack(worker.ContractVacancyKind, worker.ContractBuildingType ?? LocationType.Parking);
        int oldLevel = GetWorkerProfessionalLevel(worker, track);
        AddWorkerProfessionalExperienceDay(worker, track);
        int newLevel = GetWorkerProfessionalLevel(worker, track);
        int days = GetWorkerProfessionalExperienceDays(worker, track);

        SessionDebugLogger.Log(
            "PROFESSION",
            $"{worker.DriverName} gained {track} experience: days={days}, level={newLevel}, contract={worker.ContractVacancyKind}.");

        if (newLevel > oldLevel)
        {
            SessionDebugLogger.Log(
                "PROFESSION",
                $"{worker.DriverName} reached {track} professional level {newLevel}.");
            PushFeedEvent(
                $"{worker.DriverName} reached professional level {newLevel} in {track}.",
                $"{worker.DriverName} \u043f\u043e\u0432\u044b\u0441\u0438\u043b \u043f\u0440\u043e\u0444\u0443\u0440\u043e\u0432\u0435\u043d\u044c {FormatWorkerProfessionalTrack(track, true)} \u0434\u043e {newLevel}.",
                FeedEventType.Success);
        }

        isDriversScreenDirty = true;
    }

    private int DetermineVacancyProfessionalLevelRequirement(
        VacancyKind kind,
        LocationType buildingType,
        int slotIndex,
        int shiftIndex,
        int marketPressure,
        float vacancyAgeWorldHours)
    {
        WorkerProfessionalTrack track = GetVacancyProfessionalTrack(kind, buildingType);
        if (track == WorkerProfessionalTrack.None)
        {
            return 1;
        }

        int importanceBonus = kind switch
        {
            VacancyKind.Intercity => 10,
            VacancyKind.TruckDriver => 8,
            VacancyKind.BusDriver => 6,
            VacancyKind.Production => 8,
            VacancyKind.Service when buildingType == LocationType.LaborExchange => 8,
            VacancyKind.Service when buildingType == LocationType.GasStation || buildingType == LocationType.CarMarket => 6,
            _ => 0
        };

        int nightBonus = 0;
        if (shiftIndex >= 0 && shiftIndex < ShiftPresetHours.Length)
        {
            int hour = ShiftPresetHours[shiftIndex];
            nightBonus = hour >= 18 || hour < 6 ? 5 : 0;
        }

        int pressureBonus = marketPressure >= 45 ? 14 : marketPressure >= 20 ? 8 : marketPressure >= 5 ? 3 : 0;
        int ageBonus = Mathf.Clamp(Mathf.FloorToInt(vacancyAgeWorldHours / 6f), 0, 5);
        int level3Chance = Mathf.Clamp(2 + importanceBonus / 3 + pressureBonus / 3 + nightBonus / 2 + ageBonus / 2, 0, 15);
        int level2Chance = Mathf.Clamp(10 + importanceBonus + pressureBonus + nightBonus + ageBonus, 0, 45);
        int roll = StableVacancyRoll(kind, buildingType, slotIndex, shiftIndex, 911);

        int targetLevel = roll < level3Chance
            ? 3
            : roll < level3Chance + level2Chance ? 2 : 1;

        while (targetLevel > 1 && !HasAvailableWorkerForProfessionalLevel(kind, buildingType, targetLevel))
        {
            targetLevel--;
        }

        return Mathf.Clamp(targetLevel, 1, WorkerProfessionalMaxLevel);
    }

    private int ApplyVacancyProfessionalSalaryPremium(int salary, int requiredLevel)
    {
        return requiredLevel switch
        {
            3 => RoundToNearestFive(Mathf.Clamp(salary + Mathf.Max(50, Mathf.RoundToInt(salary * 0.75f)), 80, 160)),
            2 => RoundToNearestFive(Mathf.Clamp(salary + Mathf.Max(20, Mathf.RoundToInt(salary * 0.35f)), 45, 120)),
            _ => RoundToNearestFive(Mathf.Clamp(salary, 15, 90))
        };
    }

    private int StableVacancyRoll(VacancyKind kind, LocationType buildingType, int slotIndex, int shiftIndex, int salt)
    {
        int hash = ((int)kind * 73856093) ^
                   ((int)buildingType * 19349663) ^
                   ((slotIndex + 17) * 83492791) ^
                   ((shiftIndex + 31) * 265443576) ^
                   (salt * 374761393) ^
                   (currentDay * 668265263);
        return Mathf.Abs(hash % 100);
    }

    private bool HasAvailableWorkerForProfessionalLevel(VacancyKind kind, LocationType buildingType, int requiredLevel)
    {
        WorkerProfessionalTrack track = GetVacancyProfessionalTrack(kind, buildingType);
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (!IsWorkerVacantForVacancyAssignment(worker) ||
                worker.ReservedLaborExchangePostingId > 0 ||
                GetWorkerProfessionalLevel(worker, track) < requiredLevel)
            {
                continue;
            }

            if ((kind == VacancyKind.Production || kind == VacancyKind.Service) &&
                !CanWorkerMeetBuildingEducationRequirement(worker, buildingType, out _))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool CanWorkerMeetProfessionalRequirement(
        DriverAgent worker,
        VacancyKind kind,
        LocationType buildingType,
        int requiredLevel,
        out string reason)
    {
        bool ru = IsRussianLanguage();
        WorkerProfessionalTrack track = GetVacancyProfessionalTrack(kind, buildingType);
        int workerLevel = GetWorkerProfessionalLevel(worker, track);
        if (workerLevel >= requiredLevel)
        {
            reason = string.Empty;
            return true;
        }

        reason = ru
            ? $"\u043d\u0443\u0436\u0435\u043d \u0443\u0440. {requiredLevel} {FormatWorkerProfessionalTrack(track, ru)}, \u0443 \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e \u0443\u0440. {workerLevel}"
            : $"requires level {requiredLevel} {FormatWorkerProfessionalTrack(track, ru)}, worker has level {workerLevel}";
        return false;
    }

    private string FormatWorkerProfessionalTrack(WorkerProfessionalTrack track, bool ru)
    {
        return track switch
        {
            WorkerProfessionalTrack.Logistics => ru ? "\u043b\u043e\u0433\u0438\u0441\u0442\u0438\u043a\u0438" : "Logistics",
            WorkerProfessionalTrack.Production => ru ? "\u043f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0441\u0442\u0432\u0430" : "Production",
            WorkerProfessionalTrack.Service => ru ? "\u0441\u0435\u0440\u0432\u0438\u0441\u0430" : "Service",
            _ => ru ? "\u043e\u043f\u044b\u0442\u0430" : "Experience"
        };
    }

    private string FormatWorkerProfessionalSummary(DriverAgent worker, bool ru)
    {
        if (worker == null)
        {
            return "\u2014";
        }

        string logistics = ru ? "\u041b\u043e\u0433" : "Log";
        string production = ru ? "\u041f\u0440\u043e\u0438\u0437" : "Prod";
        string service = ru ? "\u0421\u0435\u0440\u0432" : "Serv";
        return $"{logistics} {GetWorkerProfessionalLevel(worker, WorkerProfessionalTrack.Logistics)} " +
               $"/ {production} {GetWorkerProfessionalLevel(worker, WorkerProfessionalTrack.Production)} " +
               $"/ {service} {GetWorkerProfessionalLevel(worker, WorkerProfessionalTrack.Service)}";
    }

    private string FormatVacancyProfessionalRequirement(VacancyKind kind, LocationType buildingType, int requiredLevel, bool ru)
    {
        if (requiredLevel <= 1)
        {
            return ru ? "\u0443\u0440. 1+" : "lvl 1+";
        }

        WorkerProfessionalTrack track = GetVacancyProfessionalTrack(kind, buildingType);
        return ru
            ? $"\u0443\u0440. {requiredLevel}+ {FormatWorkerProfessionalTrack(track, ru)}"
            : $"lvl {requiredLevel}+ {FormatWorkerProfessionalTrack(track, ru)}";
    }
}
