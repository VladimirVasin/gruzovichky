using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerAffectMemoryCap = 8;
    private const int WorkerAffectStrongIntensity = 55;
    private const float WorkerAffectDefaultDurationHours = 8f;

    private static readonly WorkerAffectKind[] WorkerAffectCatalog =
    {
        WorkerAffectKind.FinancialPressure,
        WorkerAffectKind.FamilyAnxiety,
        WorkerAffectKind.ReliefAfterRest,
        WorkerAffectKind.Hangover,
        WorkerAffectKind.Loneliness,
        WorkerAffectKind.InspiredByNature,
        WorkerAffectKind.IrritatedByLitter,
        WorkerAffectKind.GamblingExcitement,
        WorkerAffectKind.GamblingRegret,
        WorkerAffectKind.StableRoutine
    };

    private void UpdateWorkerAffects(DriverAgent worker)
    {
        if (worker == null || worker.HasDepartedTown || worker.IsLeavingTown)
        {
            return;
        }

        RemoveExpiredWorkerAffects(worker);
        UpdateWorkerFinancialPressureAffect(worker);
        UpdateWorkerFamilyAnxietyAffect(worker);
        UpdateWorkerLonelinessAffect(worker);
        UpdateWorkerStreetLitterAffect(worker);
        UpdateWorkerStableRoutineAffect(worker);
        ApplyWorkerAffectThoughts(worker);
    }

    private void UpdateWorkerFinancialPressureAffect(DriverAgent worker)
    {
        bool blocked = IsWorkerDueButBlockedByMoney(worker);
        if (worker.Money > 15 && !blocked)
        {
            ClearWorkerAffect(worker, WorkerAffectKind.FinancialPressure, "money recovered");
            return;
        }

        int intensity = worker.Money <= 5 ? 86 : worker.Money <= 10 ? 72 : 58;
        if (blocked)
        {
            intensity = Mathf.Max(intensity, 74);
        }

        SetWorkerAffect(
            worker,
            WorkerAffectKind.FinancialPressure,
            intensity,
            6f,
            null,
            0,
            "money",
            blocked
                ? "\u043d\u0443\u0436\u043d\u044b\u0439 \u0441\u0435\u0440\u0432\u0438\u0441 \u0441\u0442\u0430\u043b \u0441\u043b\u0438\u0448\u043a\u043e\u043c \u0434\u043e\u0440\u043e\u0433\u0438\u043c"
                : "\u043d\u0430 \u0441\u0447\u0435\u0442\u0443 \u043c\u0430\u043b\u043e \u0434\u0435\u043d\u0435\u0433",
            blocked
                ? "a needed service is too expensive"
                : "money is running low");
    }

    private void UpdateWorkerFamilyAnxietyAffect(DriverAgent worker)
    {
        WorkerFamily family = GetWorkerFamilyById(worker.FamilyId);
        if (family == null)
        {
            ClearWorkerAffect(worker, WorkerAffectKind.FamilyAnxiety, "no family");
            return;
        }

        int childCareNeed = CountWorkerFamilyChildrenNeedingChildCare(family);
        bool missingChildCare = childCareNeed > 0 && !IsWorkerFamilyChildCareCovered(family);
        bool missingSchool = GetWorkerFamilySchoolPressure(family) > 0;
        bool anxious = family.Happiness <= 42 || missingChildCare || missingSchool || family.LastDailyUpkeepShortfall > 0;
        if (!anxious)
        {
            ClearWorkerAffect(worker, WorkerAffectKind.FamilyAnxiety, "family stable");
            return;
        }

        int intensity = 54 + Mathf.Clamp(48 - family.Happiness, 0, 34);
        if (missingChildCare || missingSchool)
        {
            intensity += 10;
        }

        if (family.LastDailyUpkeepShortfall > 0)
        {
            intensity += 8;
        }

        string reasonEn = string.IsNullOrWhiteSpace(family.LastHappinessReason)
            ? "family pressure"
            : family.LastHappinessReason;
        SetWorkerAffect(
            worker,
            WorkerAffectKind.FamilyAnxiety,
            Mathf.Clamp(intensity, 50, 95),
            10f,
            LocationType.PersonalHouse,
            family.HouseIndex,
            $"family:{family.Id}",
            "\u0441\u0435\u043c\u044c\u044f \u0447\u0443\u0432\u0441\u0442\u0432\u0443\u0435\u0442 \u043d\u0430\u043f\u0440\u044f\u0436\u0435\u043d\u0438\u0435",
            reasonEn);
    }

    private void UpdateWorkerLonelinessAffect(DriverAgent worker)
    {
        if (worker.DaysOnMap < 1 || worker.SocialMemories.Count > 0 || HasWorkerPerk(worker, WorkerPerkKind.Socialite))
        {
            ClearWorkerAffect(worker, WorkerAffectKind.Loneliness, "social contact present");
            return;
        }

        SetWorkerAffect(
            worker,
            WorkerAffectKind.Loneliness,
            worker.LastLeisureNeedStatus == WorkerNeedStatus.Critical ? 66 : 48,
            8f,
            null,
            0,
            "social",
            "\u043d\u0435\u0442 \u0437\u043d\u0430\u043a\u043e\u043c\u0441\u0442\u0432 \u0438 \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u043e\u0432",
            "no acquaintances or conversations yet");
    }

    private void UpdateWorkerStreetLitterAffect(DriverAgent worker)
    {
        bool irritated = worker.StreetLitterPeakToday >= WorkerStreetLitterMediumThoughtThreshold ||
                         worker.StreetLitterExposureMemory >= 3.2f;
        if (!irritated)
        {
            ClearWorkerAffect(worker, WorkerAffectKind.IrritatedByLitter, "litter irritation faded");
            return;
        }

        int intensity = worker.StreetLitterPeakToday >= WorkerStreetLitterHighThoughtThreshold ||
                        worker.StreetLitterExposureMemory >= 6f
            ? 80
            : 58;
        SetWorkerAffect(
            worker,
            WorkerAffectKind.IrritatedByLitter,
            intensity,
            8f,
            null,
            0,
            "litter",
            "\u043c\u0443\u0441\u043e\u0440 \u043d\u0430 \u043c\u0430\u0440\u0448\u0440\u0443\u0442\u0435 \u0443\u0436\u0435 \u0440\u0430\u0437\u0434\u0440\u0430\u0436\u0430\u0435\u0442",
            "street litter on daily routes is irritating");
    }

    private void UpdateWorkerStableRoutineAffect(DriverAgent worker)
    {
        bool stable = worker.Money >= 45 &&
                      !HasCriticalWorkerNeed(worker) &&
                      IsWorkerEmployedForMigration(worker) &&
                      FindWorkerAffect(worker, WorkerAffectKind.FinancialPressure) == null &&
                      FindWorkerAffect(worker, WorkerAffectKind.FamilyAnxiety) == null;
        if (!stable)
        {
            ClearWorkerAffect(worker, WorkerAffectKind.StableRoutine, "routine not stable");
            return;
        }

        SetWorkerAffect(
            worker,
            WorkerAffectKind.StableRoutine,
            worker.Satisfaction >= 70 ? 60 : 48,
            12f,
            worker.AssignedBuildingType,
            worker.AssignedBuildingInstanceId,
            "routine",
            "\u0440\u0430\u0431\u043e\u0442\u0430, \u0434\u0435\u043d\u044c\u0433\u0438 \u0438 \u0431\u044b\u0442 \u0434\u0435\u0440\u0436\u0430\u0442\u0441\u044f \u0440\u043e\u0432\u043d\u043e",
            "work, money, and daily needs feel steady");
    }

    private void RecordWorkerLeisureAffect(DriverAgent worker, LocationType service)
    {
        if (worker == null)
        {
            return;
        }

        LocationData location = GetDriverPendingServiceLocation(worker, service);
        int instanceId = location?.InstanceId ?? 0;
        switch (service)
        {
            case LocationType.Bar:
                if (worker.LastSleepNeedStatus == WorkerNeedStatus.Critical || worker.HoursSinceSleep >= WorkerSleepWarningHours)
                {
                    SetWorkerAffect(worker, WorkerAffectKind.Hangover, 58, 8f, service, instanceId, "bar_hangover", "\u0431\u0430\u0440 \u043f\u043e\u043c\u043e\u0433, \u043d\u043e \u0443\u0441\u0442\u0430\u043b\u043e\u0441\u0442\u044c \u043d\u0430\u043a\u0440\u044b\u043b\u0430", "bar rest helped, but fatigue caught up");
                }
                else
                {
                    SetWorkerAffect(worker, WorkerAffectKind.ReliefAfterRest, 58, 6f, service, instanceId, "bar_relief", "\u0431\u0430\u0440 \u0434\u0430\u043b \u0432\u044b\u0434\u043e\u0445\u043d\u0443\u0442\u044c", "the Bar gave a moment of relief");
                }
                break;
            case LocationType.CityPark:
                SetWorkerAffect(worker, WorkerAffectKind.InspiredByNature, 64, 10f, service, instanceId, "park_nature", "\u043f\u0430\u0440\u043a \u0438 \u0437\u0435\u043b\u0435\u043d\u044c \u043f\u043e\u043c\u043e\u0433\u043b\u0438 \u0441\u043e\u0431\u0440\u0430\u0442\u044c\u0441\u044f", "park and nature helped them reset");
                break;
        }

        ApplyWorkerAffectThoughts(worker);
    }

    private void RecordWorkerGamblingAffect(DriverAgent worker, int net, bool broke)
    {
        if (worker == null)
        {
            return;
        }

        if (broke || net < 0)
        {
            SetWorkerAffect(
                worker,
                WorkerAffectKind.GamblingRegret,
                broke ? 82 : 66,
                12f,
                LocationType.GamblingHall,
                worker.PendingServiceLocationInstanceId,
                "gambling_regret",
                broke ? "\u0434\u0435\u043d\u0435\u0433 \u043d\u0435 \u0445\u0432\u0430\u0442\u0438\u043b\u043e \u0434\u0430\u0436\u0435 \u043d\u0430 \u0441\u0442\u0430\u0432\u043a\u0443" : "\u0441\u0442\u0430\u0432\u043a\u0430 \u0443\u0448\u043b\u0430 \u0432 \u043c\u0438\u043d\u0443\u0441",
                broke ? "there was not enough money even to bet" : "the bet ended in a loss");
        }
        else if (net > 0)
        {
            SetWorkerAffect(
                worker,
                WorkerAffectKind.GamblingExcitement,
                Mathf.Clamp(56 + net * 2, 56, 86),
                8f,
                LocationType.GamblingHall,
                worker.PendingServiceLocationInstanceId,
                "gambling_excitement",
                "\u0441\u0442\u0430\u0432\u043a\u0430 \u0441\u044b\u0433\u0440\u0430\u043b\u0430 \u0432 \u043f\u043b\u044e\u0441",
                "the bet paid off");
        }

        ApplyWorkerAffectThoughts(worker);
    }

    private WorkerAffect SetWorkerAffect(
        DriverAgent worker,
        WorkerAffectKind kind,
        int intensity,
        float durationHours,
        LocationType? sourceLocationType,
        int sourceInstanceId,
        string sourceKey,
        string reasonRu,
        string reasonEn)
    {
        if (worker == null)
        {
            return null;
        }

        float now = GetCurrentWorldHour();
        intensity = Mathf.Clamp(intensity, 0, 100);
        float expires = now + Mathf.Max(0.25f, durationHours <= 0f ? WorkerAffectDefaultDurationHours : durationHours);
        WorkerAffect existing = FindWorkerAffect(worker, kind);
        if (existing != null)
        {
            bool materialRefresh = Mathf.Abs(existing.Intensity - intensity) >= 10 ||
                                   existing.SourceInstanceId != sourceInstanceId ||
                                   existing.SourceLocationType != sourceLocationType;
            existing.Intensity = intensity;
            existing.ExpiresWorldHour = Mathf.Max(existing.ExpiresWorldHour, expires);
            existing.SourceLocationType = sourceLocationType;
            existing.SourceInstanceId = sourceInstanceId;
            existing.SourceKey = sourceKey ?? string.Empty;
            existing.ReasonRu = reasonRu ?? string.Empty;
            existing.ReasonEn = reasonEn ?? string.Empty;
            if (materialRefresh)
            {
                SessionDebugLogger.Log("AFFECT", $"{worker.DriverName}: affect refreshed {kind}, intensity={intensity}, reason={existing.ReasonEn}.");
            }

            return existing;
        }

        WorkerAffect affect = new()
        {
            Kind = kind,
            Intensity = intensity,
            StartedDay = currentDay,
            StartedWorldHour = now,
            ExpiresWorldHour = expires,
            SourceLocationType = sourceLocationType,
            SourceInstanceId = sourceInstanceId,
            SourceKey = sourceKey ?? string.Empty,
            ReasonRu = reasonRu ?? string.Empty,
            ReasonEn = reasonEn ?? string.Empty
        };
        worker.Affects.Insert(0, affect);
        TrimWorkerAffects(worker);
        isDriversScreenDirty = true;
        SessionDebugLogger.Log("AFFECT", $"{worker.DriverName}: affect appeared {kind}, intensity={intensity}, reason={affect.ReasonEn}.");
        return affect;
    }

    private void RemoveExpiredWorkerAffects(DriverAgent worker)
    {
        if (worker == null)
        {
            return;
        }

        float now = GetCurrentWorldHour();
        for (int i = worker.Affects.Count - 1; i >= 0; i--)
        {
            WorkerAffect affect = worker.Affects[i];
            if (affect == null || affect.ExpiresWorldHour <= 0f || now < affect.ExpiresWorldHour)
            {
                continue;
            }

            worker.Affects.RemoveAt(i);
            ResolveActiveWorkerThought(worker, GetWorkerAffectThoughtKey(affect.Kind), "affect expired");
            SessionDebugLogger.Log("AFFECT", $"{worker.DriverName}: affect expired {affect.Kind}.");
            isDriversScreenDirty = true;
        }
    }

    private bool ClearWorkerAffect(DriverAgent worker, WorkerAffectKind kind, string reason)
    {
        if (worker == null)
        {
            return false;
        }

        bool removed = false;
        for (int i = worker.Affects.Count - 1; i >= 0; i--)
        {
            if (worker.Affects[i]?.Kind != kind)
            {
                continue;
            }

            worker.Affects.RemoveAt(i);
            removed = true;
        }

        if (removed)
        {
            ResolveActiveWorkerThought(worker, GetWorkerAffectThoughtKey(kind), reason);
            SessionDebugLogger.Log("AFFECT", $"{worker.DriverName}: affect cleared {kind}, reason={reason}.");
            isDriversScreenDirty = true;
        }

        return removed;
    }

    private void TrimWorkerAffects(DriverAgent worker)
    {
        while (worker != null && worker.Affects.Count > WorkerAffectMemoryCap)
        {
            int lowestIndex = 0;
            int lowestIntensity = int.MaxValue;
            for (int i = 0; i < worker.Affects.Count; i++)
            {
                int intensity = worker.Affects[i]?.Intensity ?? 0;
                if (intensity < lowestIntensity)
                {
                    lowestIntensity = intensity;
                    lowestIndex = i;
                }
            }

            worker.Affects.RemoveAt(lowestIndex);
        }
    }

    private WorkerAffect FindWorkerAffect(DriverAgent worker, WorkerAffectKind kind)
    {
        if (worker == null)
        {
            return null;
        }

        float now = GetCurrentWorldHour();
        for (int i = 0; i < worker.Affects.Count; i++)
        {
            WorkerAffect affect = worker.Affects[i];
            if (affect != null && affect.Kind == kind && (affect.ExpiresWorldHour <= 0f || now < affect.ExpiresWorldHour))
            {
                return affect;
            }
        }

        return null;
    }

    private WorkerAffect GetStrongestWorkerAffect(DriverAgent worker)
    {
        WorkerAffect strongest = null;
        if (worker == null)
        {
            return null;
        }

        float now = GetCurrentWorldHour();
        for (int i = 0; i < worker.Affects.Count; i++)
        {
            WorkerAffect affect = worker.Affects[i];
            if (affect == null || affect.ExpiresWorldHour > 0f && now >= affect.ExpiresWorldHour)
            {
                continue;
            }

            if (strongest == null || affect.Intensity > strongest.Intensity)
            {
                strongest = affect;
            }
        }

        return strongest;
    }

    private bool HasWorkerAffect(DriverAgent worker, WorkerAffectKind kind)
    {
        return FindWorkerAffect(worker, kind) != null;
    }

    private int CountActiveWorkerAffects(DriverAgent worker)
    {
        int count = 0;
        if (worker == null)
        {
            return count;
        }

        float now = GetCurrentWorldHour();
        for (int i = 0; i < worker.Affects.Count; i++)
        {
            WorkerAffect affect = worker.Affects[i];
            if (affect != null && (affect.ExpiresWorldHour <= 0f || now < affect.ExpiresWorldHour))
            {
                count++;
            }
        }

        return count;
    }

    private string FormatWorkerAffectsInline(DriverAgent worker, bool ru, int maxVisible)
    {
        if (worker == null || worker.Affects.Count == 0)
        {
            return ru ? "\u043d\u0435\u0442" : "none";
        }

        float now = GetCurrentWorldHour();
        List<WorkerAffect> active = new();
        for (int i = 0; i < worker.Affects.Count; i++)
        {
            WorkerAffect affect = worker.Affects[i];
            if (affect != null && (affect.ExpiresWorldHour <= 0f || now < affect.ExpiresWorldHour))
            {
                active.Add(affect);
            }
        }

        if (active.Count == 0)
        {
            return ru ? "\u043d\u0435\u0442" : "none";
        }

        active.Sort((a, b) => b.Intensity.CompareTo(a.Intensity));
        int count = Mathf.Min(Mathf.Max(1, maxVisible), active.Count);
        string result = string.Empty;
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                result += ", ";
            }

            result += GetWorkerAffectDisplayName(active[i].Kind, ru);
        }

        if (active.Count > count)
        {
            result += $" +{active.Count - count}";
        }

        return result;
    }

    private void ApplyWorkerAffectThoughts(DriverAgent worker)
    {
        if (worker == null)
        {
            return;
        }

        for (int i = 0; i < WorkerAffectCatalog.Length; i++)
        {
            WorkerAffectKind kind = WorkerAffectCatalog[i];
            WorkerAffect affect = FindWorkerAffect(worker, kind);
            string thoughtKey = GetWorkerAffectThoughtKey(kind);
            if (affect == null || affect.Intensity < 42)
            {
                ResolveActiveWorkerThought(worker, thoughtKey, "affect inactive");
                continue;
            }

            bool hadActiveThought = FindActiveWorkerThought(worker, thoughtKey) != null;
            bool hadPendingThought = FindPendingWorkerThought(worker, thoughtKey) != null;
            AddOrKeepActiveWorkerThought(
                worker,
                thoughtKey,
                GetWorkerAffectThoughtKind(kind),
                GetWorkerAffectThoughtTone(kind),
                affect.Intensity,
                thoughtKey,
                new[]
                {
                    ThoughtText("reason", IsRussianLanguage() ? affect.ReasonRu : affect.ReasonEn)
                },
                GetWorkerAffectThoughtPriority(affect),
                WorkerThoughtSubjectType.Text,
                0,
                GetWorkerAffectSubjectKey(kind),
                GetWorkerAffectDisplayName(kind, false),
                GetWorkerAffectOpinionDelta(kind),
                0f);
            if (!hadActiveThought && !hadPendingThought)
            {
                SessionDebugLogger.Log("AFFECT", $"{worker.DriverName}: affect {kind} created thought {thoughtKey}.");
            }
        }
    }

    private void ApplyWorkerAffectsToBuildingKnowledge(
        DriverAgent worker,
        PendingWorkerKnowledge pending,
        ref int score,
        ref int confidence,
        ref string reasonRu,
        ref string reasonEn)
    {
        if (worker == null || pending?.BuildingType == null)
        {
            return;
        }

        LocationType type = pending.BuildingType.Value;
        WorkerAffect financial = FindWorkerAffect(worker, WorkerAffectKind.FinancialPressure);
        if (financial != null && type is LocationType.Motel or LocationType.Bar or LocationType.GamblingHall)
        {
            score -= type == LocationType.GamblingHall ? 20 : 12;
            confidence += 7;
            reasonRu = "\u0434\u043e\u0440\u043e\u0433\u0438\u0435 \u0441\u0435\u0440\u0432\u0438\u0441\u044b \u0441\u0435\u0439\u0447\u0430\u0441 \u043e\u0449\u0443\u0449\u0430\u044e\u0442\u0441\u044f \u0431\u043e\u043b\u0435\u0437\u043d\u0435\u043d\u043d\u043e";
            reasonEn = "expensive services feel painful under financial pressure";
            SessionDebugLogger.Log("AFFECT", $"{worker.DriverName}: FinancialPressure influenced knowledge about {type}.");
        }

        WorkerAffect family = FindWorkerAffect(worker, WorkerAffectKind.FamilyAnxiety);
        if (family != null && type is LocationType.Kindergarten or LocationType.PrimarySchool or LocationType.SecondarySchool or LocationType.PersonalHouse or LocationType.CityHall)
        {
            score += 14;
            confidence += 8;
            reasonRu = "\u0441\u0435\u043c\u0435\u0439\u043d\u0430\u044f \u0442\u0440\u0435\u0432\u043e\u0433\u0430 \u0434\u0435\u043b\u0430\u0435\u0442 \u044d\u0442\u0443 \u0442\u0435\u043c\u0443 \u0432\u0430\u0436\u043d\u043e\u0439";
            reasonEn = "family anxiety makes this topic feel important";
            SessionDebugLogger.Log("AFFECT", $"{worker.DriverName}: FamilyAnxiety influenced knowledge about {type}.");
        }

        if (type == LocationType.CityPark && FindWorkerAffect(worker, WorkerAffectKind.InspiredByNature) != null)
        {
            score += 22;
            confidence += 10;
            reasonRu = "\u043f\u0440\u0438\u0440\u043e\u0434\u0430 \u0443\u0436\u0435 \u043f\u043e\u043c\u043e\u0433\u043b\u0430, \u043f\u0430\u0440\u043a \u0447\u0443\u0432\u0441\u0442\u0432\u0443\u0435\u0442\u0441\u044f \u043d\u0430\u0434\u0435\u0436\u043d\u044b\u043c \u043c\u0435\u0441\u0442\u043e\u043c";
            reasonEn = "nature already helped, so the park feels reliable";
            SessionDebugLogger.Log("AFFECT", $"{worker.DriverName}: InspiredByNature influenced knowledge about CityPark.");
        }

        if (type == LocationType.Bar)
        {
            if (FindWorkerAffect(worker, WorkerAffectKind.Hangover) != null)
            {
                score -= 18;
                confidence += 6;
                reasonRu = "\u043f\u043e\u0441\u043b\u0435 \u0431\u0430\u0440\u0430 \u043e\u0441\u0442\u0430\u043b\u0430\u0441\u044c \u0442\u044f\u0436\u0435\u0441\u0442\u044c";
                reasonEn = "the Bar left a heavy aftertaste";
                SessionDebugLogger.Log("AFFECT", $"{worker.DriverName}: Hangover influenced knowledge about Bar.");
            }
            else if (FindWorkerAffect(worker, WorkerAffectKind.ReliefAfterRest) != null)
            {
                score += 10;
                confidence += 5;
                reasonRu = "\u043e\u0442\u0434\u044b\u0445 \u0432 \u0431\u0430\u0440\u0435 \u0441\u043d\u044f\u043b \u043d\u0430\u043f\u0440\u044f\u0436\u0435\u043d\u0438\u0435";
                reasonEn = "Bar leisure relieved pressure";
                SessionDebugLogger.Log("AFFECT", $"{worker.DriverName}: ReliefAfterRest influenced knowledge about Bar.");
            }
        }

        if (type == LocationType.GamblingHall)
        {
            if (FindWorkerAffect(worker, WorkerAffectKind.GamblingRegret) != null)
            {
                bool prefersGambling = HasWorkerLeisurePreference(worker, WorkerLeisurePreferenceKind.RiskPlayer);
                score -= prefersGambling ? 14 : 26;
                confidence += 10;
                reasonRu = prefersGambling
                    ? "\u0442\u044f\u043d\u0435\u0442 \u043a \u0440\u0438\u0441\u043a\u0443, \u043d\u043e \u043f\u0440\u043e\u0438\u0433\u0440\u044b\u0448 \u0441\u0434\u0435\u043b\u0430\u043b \u043e\u0442\u043d\u043e\u0448\u0435\u043d\u0438\u0435 \u043f\u0440\u043e\u0442\u0438\u0432\u043e\u0440\u0435\u0447\u0438\u0432\u044b\u043c"
                    : "\u043f\u0440\u043e\u0438\u0433\u0440\u044b\u0448 \u0441\u0434\u0435\u043b\u0430\u043b \u0438\u0433\u0440\u043e\u0432\u043e\u0439 \u0437\u0430\u043b \u043e\u043f\u0430\u0441\u043d\u044b\u043c";
                reasonEn = prefersGambling
                    ? "risk is tempting, but the loss made the opinion conflicted"
                    : "the loss made the Gambling Hall feel dangerous";
                SessionDebugLogger.Log("AFFECT", $"{worker.DriverName}: GamblingRegret influenced knowledge about GamblingHall.");
            }
            else if (FindWorkerAffect(worker, WorkerAffectKind.GamblingExcitement) != null)
            {
                score += 18;
                confidence += 7;
                reasonRu = "\u0432\u044b\u0438\u0433\u0440\u044b\u0448 \u0441\u0434\u0435\u043b\u0430\u043b \u0440\u0438\u0441\u043a \u043f\u0440\u0438\u0442\u044f\u0433\u0430\u0442\u0435\u043b\u044c\u043d\u044b\u043c";
                reasonEn = "a win made the risk feel attractive";
                SessionDebugLogger.Log("AFFECT", $"{worker.DriverName}: GamblingExcitement influenced knowledge about GamblingHall.");
            }
        }

        confidence = Mathf.Clamp(confidence, 0, 100);
    }

    private static string GetWorkerAffectDisplayName(WorkerAffectKind affect, bool ru)
    {
        return affect switch
        {
            WorkerAffectKind.FinancialPressure => ru ? "\u0424\u0438\u043d\u0430\u043d\u0441\u043e\u0432\u043e\u0435 \u043d\u0430\u043f\u0440\u044f\u0436\u0435\u043d\u0438\u0435" : "Financial pressure",
            WorkerAffectKind.FamilyAnxiety => ru ? "\u0421\u0435\u043c\u0435\u0439\u043d\u0430\u044f \u0442\u0440\u0435\u0432\u043e\u0433\u0430" : "Family anxiety",
            WorkerAffectKind.ReliefAfterRest => ru ? "\u041e\u0431\u043b\u0435\u0433\u0447\u0435\u043d\u0438\u0435 \u043f\u043e\u0441\u043b\u0435 \u043e\u0442\u0434\u044b\u0445\u0430" : "Relief after rest",
            WorkerAffectKind.Hangover => ru ? "\u0422\u044f\u0436\u0435\u0441\u0442\u044c \u043f\u043e\u0441\u043b\u0435 \u0431\u0430\u0440\u0430" : "Hangover",
            WorkerAffectKind.Loneliness => ru ? "\u041e\u0434\u0438\u043d\u043e\u0447\u0435\u0441\u0442\u0432\u043e" : "Loneliness",
            WorkerAffectKind.InspiredByNature => ru ? "\u0412\u0434\u043e\u0445\u043d\u043e\u0432\u043b\u0451\u043d \u043f\u0440\u0438\u0440\u043e\u0434\u043e\u0439" : "Inspired by nature",
            WorkerAffectKind.IrritatedByLitter => ru ? "\u0420\u0430\u0437\u0434\u0440\u0430\u0436\u0451\u043d \u043c\u0443\u0441\u043e\u0440\u043e\u043c" : "Irritated by litter",
            WorkerAffectKind.GamblingExcitement => ru ? "\u0410\u0437\u0430\u0440\u0442 \u043f\u043e\u0441\u043b\u0435 \u0432\u044b\u0438\u0433\u0440\u044b\u0448\u0430" : "Gambling excitement",
            WorkerAffectKind.GamblingRegret => ru ? "\u0421\u043e\u0436\u0430\u043b\u0435\u043d\u0438\u0435 \u043f\u043e\u0441\u043b\u0435 \u0441\u0442\u0430\u0432\u043a\u0438" : "Gambling regret",
            WorkerAffectKind.StableRoutine => ru ? "\u0421\u0442\u0430\u0431\u0438\u043b\u044c\u043d\u044b\u0439 \u0440\u0438\u0442\u043c" : "Stable routine",
            _ => ru ? "\u0421\u043e\u0441\u0442\u043e\u044f\u043d\u0438\u0435" : "State"
        };
    }

    private static Color GetWorkerAffectColor(WorkerAffectKind affect)
    {
        return GetWorkerAffectThoughtTone(affect) switch
        {
            WorkerThoughtTone.Positive => new Color(0.42f, 0.92f, 0.58f, 1f),
            WorkerThoughtTone.Negative => new Color(1f, 0.48f, 0.34f, 1f),
            _ => new Color(0.62f, 0.82f, 1f, 1f)
        };
    }

    private static string GetWorkerAffectReferenceDescription(WorkerAffectKind affect, bool ru)
    {
        return affect switch
        {
            WorkerAffectKind.FinancialPressure => ru ? "\u041c\u0430\u043b\u043e \u0434\u0435\u043d\u0435\u0433 \u0438\u043b\u0438 \u0441\u0435\u0440\u0432\u0438\u0441 \u0441\u0442\u0430\u043b \u043d\u0435\u0434\u043e\u0441\u0442\u0443\u043f\u0435\u043d. \u0414\u0430\u0451\u0442 \u043c\u044b\u0441\u043b\u0438 \u043e \u0434\u0435\u043d\u044c\u0433\u0430\u0445 \u0438 \u0441\u043d\u0438\u0436\u0430\u0435\u0442 \u043e\u0446\u0435\u043d\u043a\u0443 \u043f\u043b\u0430\u0442\u043d\u044b\u0445 \u043c\u0435\u0441\u0442." : "Low money or unaffordable services. Creates money thoughts and lowers judgement of paid places.",
            WorkerAffectKind.FamilyAnxiety => ru ? "\u0421\u0435\u043c\u044c\u044f, \u0434\u0435\u0442\u0438, \u0441\u0430\u0434\u0438\u043a, \u0448\u043a\u043e\u043b\u044b \u0438\u043b\u0438 \u0441\u0447\u0435\u0442\u0430 \u0434\u0430\u0432\u044f\u0442 \u043d\u0430 \u0436\u0438\u0442\u0435\u043b\u044f. \u0414\u0430\u0451\u0442 family-\u043c\u044b\u0441\u043b\u0438 \u0438 \u0443\u0441\u0438\u043b\u0438\u0432\u0430\u0435\u0442 \u0437\u043d\u0430\u0447\u0438\u043c\u043e\u0441\u0442\u044c \u0441\u0435\u043c\u0435\u0439\u043d\u044b\u0445 \u0437\u043d\u0430\u043d\u0438\u0439." : "Family, children, schools, care, or bills are pressing. Creates family thoughts and raises family-related knowledge importance.",
            WorkerAffectKind.ReliefAfterRest => ru ? "\u041e\u0442\u0434\u044b\u0445 \u0441\u043d\u044f\u043b \u0447\u0430\u0441\u0442\u044c \u043d\u0430\u043f\u0440\u044f\u0436\u0435\u043d\u0438\u044f. \u0414\u0430\u0451\u0442 \u043c\u044f\u0433\u043a\u0438\u0435 \u043f\u043e\u0437\u0438\u0442\u0438\u0432\u043d\u044b\u0435 \u043c\u044b\u0441\u043b\u0438." : "Rest eased pressure. Creates mild positive need thoughts.",
            WorkerAffectKind.Hangover => ru ? "\u0411\u0430\u0440 \u0438\u043b\u0438 \u0442\u044f\u0436\u0451\u043b\u044b\u0439 \u043e\u0442\u0434\u044b\u0445 \u043e\u0441\u0442\u0430\u0432\u0438\u043b \u043d\u0435\u043f\u0440\u0438\u044f\u0442\u043d\u044b\u0439 \u0441\u043b\u0435\u0434. \u0412\u043b\u0438\u044f\u0435\u0442 \u043d\u0430 \u043c\u044b\u0441\u043b\u0438 \u0438 \u043e\u0446\u0435\u043d\u043a\u0443 \u0411\u0430\u0440\u0430." : "Heavy rest left a bad aftertaste. Affects thoughts and Bar judgement.",
            WorkerAffectKind.Loneliness => ru ? "\u041c\u0430\u043b\u043e \u0437\u043d\u0430\u043a\u043e\u043c\u0441\u0442\u0432 \u0438 \u0441\u043b\u0430\u0431\u0430\u044f \u0441\u043e\u0446\u0438\u0430\u043b\u043a\u0430. \u0414\u0430\u0451\u0442 \u0441\u043e\u0446\u0438\u0430\u043b\u044c\u043d\u044b\u0435 \u043c\u044b\u0441\u043b\u0438." : "Few acquaintances and weak social contact. Creates social thoughts.",
            WorkerAffectKind.InspiredByNature => ru ? "\u041f\u0430\u0440\u043a \u0438 \u043f\u0440\u0438\u0440\u043e\u0434\u0430 \u0434\u0430\u043b\u0438 \u0445\u043e\u0440\u043e\u0448\u0438\u0439 \u0441\u043b\u0435\u0434. \u0423\u0441\u0438\u043b\u0438\u0432\u0430\u0435\u0442 \u043c\u044b\u0441\u043b\u0438 \u043e \u0433\u043e\u0440\u043e\u0434\u0435 \u0438 \u043e\u0446\u0435\u043d\u043a\u0443 \u043f\u0430\u0440\u043a\u0430." : "Park and nature left a good trace. Strengthens city thoughts and park judgement.",
            WorkerAffectKind.IrritatedByLitter => ru ? "\u041c\u0443\u0441\u043e\u0440 \u043d\u0430 \u043c\u0430\u0440\u0448\u0440\u0443\u0442\u0430\u0445 \u0441\u043e\u0431\u0438\u0440\u0430\u0435\u0442 \u0440\u0430\u0437\u0434\u0440\u0430\u0436\u0435\u043d\u0438\u0435. \u0414\u0430\u0451\u0442 city-\u043c\u044b\u0441\u043b\u0438 \u0438 \u0441\u0438\u0433\u043d\u0430\u043b \u043d\u043e\u043e\u0441\u0444\u0435\u0440\u0435." : "Street litter builds irritation. Creates city thoughts and a Noosphere signal.",
            WorkerAffectKind.GamblingExcitement => ru ? "\u0412\u044b\u0438\u0433\u0440\u044b\u0448 \u0443\u0441\u0438\u043b\u0438\u043b \u0430\u0437\u0430\u0440\u0442. \u041c\u0435\u043d\u044f\u0435\u0442 \u0442\u043e\u043d \u043c\u044b\u0441\u043b\u0435\u0439 \u0438 \u043e\u0446\u0435\u043d\u043a\u0443 \u0438\u0433\u0440\u043e\u0432\u043e\u0433\u043e \u0437\u0430\u043b\u0430." : "A win increased excitement. Changes thought tone and Gambling Hall judgement.",
            WorkerAffectKind.GamblingRegret => ru ? "\u041f\u0440\u043e\u0438\u0433\u0440\u044b\u0448 \u0438\u043b\u0438 \u0431\u0440\u043e\u0443\u043a \u0434\u0430\u044e\u0442 \u0441\u043b\u043e\u0436\u043d\u043e\u0435 \u043e\u0442\u043d\u043e\u0448\u0435\u043d\u0438\u0435 \u043a \u0438\u0433\u0440\u043e\u0432\u043e\u043c\u0443 \u0437\u0430\u043b\u0443." : "A loss or being broke creates a conflicted view of Gambling Hall.",
            WorkerAffectKind.StableRoutine => ru ? "\u0420\u0430\u0431\u043e\u0442\u0430, \u0431\u044b\u0442 \u0438 \u0434\u0435\u043d\u044c\u0433\u0438 \u0434\u0435\u0440\u0436\u0430\u0442\u0441\u044f \u0440\u043e\u0432\u043d\u043e. \u0414\u0430\u0451\u0442 \u0442\u0438\u0445\u0443\u044e \u043f\u043e\u0437\u0438\u0442\u0438\u0432\u043d\u0443\u044e \u043c\u044b\u0441\u043b\u044c." : "Work, daily life, and money feel steady. Creates a quiet positive thought.",
            _ => ru ? "\u0412\u0440\u0435\u043c\u0435\u043d\u043d\u043e\u0435 \u044d\u043c\u043e\u0446\u0438\u043e\u043d\u0430\u043b\u044c\u043d\u043e\u0435 \u0441\u043e\u0441\u0442\u043e\u044f\u043d\u0438\u0435." : "Temporary emotional state."
        };
    }

    private static string GetWorkerAffectThoughtKey(WorkerAffectKind affect)
    {
        return affect switch
        {
            WorkerAffectKind.FinancialPressure => "affect_financial_pressure",
            WorkerAffectKind.FamilyAnxiety => "affect_family_anxiety",
            WorkerAffectKind.ReliefAfterRest => "affect_relief_after_rest",
            WorkerAffectKind.Hangover => "affect_hangover",
            WorkerAffectKind.Loneliness => "affect_loneliness",
            WorkerAffectKind.InspiredByNature => "affect_inspired_by_nature",
            WorkerAffectKind.IrritatedByLitter => "affect_litter_irritation",
            WorkerAffectKind.GamblingExcitement => "affect_gambling_excitement",
            WorkerAffectKind.GamblingRegret => "affect_gambling_regret",
            WorkerAffectKind.StableRoutine => "affect_stable_routine",
            _ => "affect_state"
        };
    }

    private static WorkerThoughtKind GetWorkerAffectThoughtKind(WorkerAffectKind affect)
    {
        return affect switch
        {
            WorkerAffectKind.FinancialPressure or WorkerAffectKind.GamblingExcitement or WorkerAffectKind.GamblingRegret => WorkerThoughtKind.Money,
            WorkerAffectKind.FamilyAnxiety => WorkerThoughtKind.Family,
            WorkerAffectKind.Loneliness => WorkerThoughtKind.Social,
            WorkerAffectKind.ReliefAfterRest or WorkerAffectKind.Hangover => WorkerThoughtKind.Need,
            _ => WorkerThoughtKind.City
        };
    }

    private static WorkerThoughtTone GetWorkerAffectThoughtTone(WorkerAffectKind affect)
    {
        return affect switch
        {
            WorkerAffectKind.ReliefAfterRest or WorkerAffectKind.InspiredByNature or WorkerAffectKind.GamblingExcitement or WorkerAffectKind.StableRoutine => WorkerThoughtTone.Positive,
            WorkerAffectKind.FinancialPressure or WorkerAffectKind.FamilyAnxiety or WorkerAffectKind.Hangover or WorkerAffectKind.Loneliness or WorkerAffectKind.IrritatedByLitter or WorkerAffectKind.GamblingRegret => WorkerThoughtTone.Negative,
            _ => WorkerThoughtTone.Neutral
        };
    }

    private static WorkerThoughtPriority GetWorkerAffectThoughtPriority(WorkerAffect affect)
    {
        if (affect == null)
        {
            return WorkerThoughtPriority.Normal;
        }

        if (affect.Intensity >= 82)
        {
            return WorkerThoughtPriority.Critical;
        }

        return affect.Intensity >= WorkerAffectStrongIntensity
            ? WorkerThoughtPriority.High
            : WorkerThoughtPriority.Normal;
    }

    private static string GetWorkerAffectSubjectKey(WorkerAffectKind affect)
    {
        return affect switch
        {
            WorkerAffectKind.FinancialPressure => "money",
            WorkerAffectKind.FamilyAnxiety => "family",
            WorkerAffectKind.Loneliness => "social",
            WorkerAffectKind.IrritatedByLitter => "litter",
            WorkerAffectKind.GamblingExcitement or WorkerAffectKind.GamblingRegret => "gambling",
            WorkerAffectKind.InspiredByNature => "nature",
            _ => "city"
        };
    }

    private static int GetWorkerAffectOpinionDelta(WorkerAffectKind affect)
    {
        return GetWorkerAffectThoughtTone(affect) switch
        {
            WorkerThoughtTone.Positive => 3,
            WorkerThoughtTone.Negative => -3,
            _ => 0
        };
    }

    private static SocialSignalCategory GetWorkerAffectSocialCategory(WorkerAffectKind affect)
    {
        return affect switch
        {
            WorkerAffectKind.FinancialPressure or WorkerAffectKind.GamblingExcitement or WorkerAffectKind.GamblingRegret => SocialSignalCategory.Money,
            WorkerAffectKind.FamilyAnxiety => SocialSignalCategory.Family,
            WorkerAffectKind.Loneliness => SocialSignalCategory.Social,
            WorkerAffectKind.ReliefAfterRest or WorkerAffectKind.Hangover => SocialSignalCategory.Need,
            WorkerAffectKind.IrritatedByLitter => SocialSignalCategory.Litter,
            _ => SocialSignalCategory.City
        };
    }
}
