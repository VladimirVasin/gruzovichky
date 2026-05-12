using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerStreetLitterSenseRadius = 2;
    private const float WorkerStreetLitterMinimumPerception = 0.12f;
    private const float WorkerStreetLitterLowThoughtThreshold = 0.75f;
    private const float WorkerStreetLitterMediumThoughtThreshold = 3.8f;
    private const float WorkerStreetLitterHighThoughtThreshold = 7.0f;

    private const string WorkerStreetLitterLowThoughtKey = "street_litter_low";
    private const string WorkerStreetLitterMediumThoughtKey = "street_litter_medium";
    private const string WorkerStreetLitterHighThoughtKey = "street_litter_high";

    private void UpdateWorkerStreetLitterExperience(DriverAgent worker)
    {
        if (worker == null)
        {
            return;
        }

        EnsureWorkerStreetLitterExposureDay(worker, currentDay);
        if (!CanWorkerPerceiveStreetLitter(worker))
        {
            ResolveWorkerStreetLitterActiveThoughts(worker, "not perceiving streets");
            return;
        }

        float perception = CalculateWorkerStreetLitterPerception(worker.DriverObject.transform.position);
        if (perception >= WorkerStreetLitterMinimumPerception)
        {
            worker.StreetLitterExposureToday += perception;
            worker.StreetLitterPeakToday = Mathf.Max(worker.StreetLitterPeakToday, perception);
            worker.StreetLitterExposureSamplesToday++;
        }

        UpdateWorkerStreetLitterActiveThought(worker, perception);
    }

    private static void EnsureWorkerStreetLitterExposureDay(DriverAgent worker, int day)
    {
        if (worker == null || worker.StreetLitterExposureDay == day)
        {
            return;
        }

        worker.StreetLitterExposureDay = day;
        worker.StreetLitterExposureToday = 0f;
        worker.StreetLitterPeakToday = 0f;
        worker.StreetLitterExposureSamplesToday = 0;
    }

    private bool CanWorkerPerceiveStreetLitter(DriverAgent worker)
    {
        return worker != null &&
               worker.DriverObject != null &&
               worker.DriverObject.activeSelf &&
               !worker.HasDepartedTown &&
               !worker.IsLeavingTown &&
               !worker.IsInsideBuilding &&
               !worker.IsDrivingPersonalCar &&
               worker.WalkPhase != DriverRescuePhase.RidingLocalBus &&
               !IsWorkerOnStreetLitterCleanerDuty(worker);
    }

    private static bool IsWorkerOnStreetLitterCleanerDuty(DriverAgent worker)
    {
        return worker != null &&
               (worker.WalkPhase == DriverRescuePhase.CleanerToLitter ||
                worker.WalkPhase == DriverRescuePhase.CleanerCleaning ||
                worker.WalkPhase == DriverRescuePhase.CleanerReturnToDepot);
    }

    private float CalculateWorkerStreetLitterPerception(Vector3 worldPosition)
    {
        if (streetLitterByCell.Count == 0)
        {
            return 0f;
        }

        Vector2Int center = WorldToCell(worldPosition);
        float total = 0f;
        int visibleCells = 0;
        int maxStage = 0;
        for (int dx = -WorkerStreetLitterSenseRadius; dx <= WorkerStreetLitterSenseRadius; dx++)
        {
            for (int dy = -WorkerStreetLitterSenseRadius; dy <= WorkerStreetLitterSenseRadius; dy++)
            {
                int distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (distance > WorkerStreetLitterSenseRadius)
                {
                    continue;
                }

                Vector2Int cell = new(center.x + dx, center.y + dy);
                if (!streetLitterByCell.TryGetValue(cell, out float litterScore))
                {
                    continue;
                }

                int stage = GetStreetLitterStage(litterScore);
                if (stage <= 0)
                {
                    continue;
                }

                visibleCells++;
                maxStage = Mathf.Max(maxStage, stage);
                total += GetWorkerStreetLitterStageWeight(stage) *
                         GetWorkerStreetLitterDistanceWeight(distance) *
                         GetWorkerStreetLitterScoreWeight(litterScore);
            }
        }

        if (visibleCells > 1)
        {
            total += Mathf.Min(visibleCells - 1, 5) * 0.4f;
        }

        if (maxStage >= 3)
        {
            total += 0.5f;
        }

        return Mathf.Clamp(total, 0f, 12f);
    }

    private static float GetWorkerStreetLitterStageWeight(int stage)
    {
        return stage switch
        {
            1 => 0.85f,
            2 => 2.2f,
            _ => 4.2f
        };
    }

    private static float GetWorkerStreetLitterDistanceWeight(int distance)
    {
        return distance switch
        {
            0 => 1.45f,
            1 => 1.0f,
            _ => 0.52f
        };
    }

    private static float GetWorkerStreetLitterScoreWeight(float litterScore)
    {
        float normalized = Mathf.InverseLerp(StreetLitterVisibleScore, StreetLitterMaxScore, litterScore);
        return Mathf.Lerp(0.75f, 1.25f, normalized);
    }

    private void UpdateWorkerStreetLitterActiveThought(DriverAgent worker, float perception)
    {
        int severity = GetWorkerStreetLitterThoughtSeverity(perception);
        if (severity <= 0)
        {
            ResolveWorkerStreetLitterActiveThoughts(worker, "litter not nearby");
            return;
        }

        string activeKey = GetWorkerStreetLitterThoughtKey(severity);
        ResolveInactiveWorkerStreetLitterThoughts(worker, activeKey);
        AddOrKeepActiveWorkerThought(
            worker,
            activeKey,
            WorkerThoughtKind.City,
            WorkerThoughtTone.Negative,
            GetWorkerStreetLitterThoughtIntensity(severity),
            activeKey,
            null,
            GetWorkerStreetLitterThoughtPriority(severity),
            WorkerThoughtSubjectType.Text,
            0,
            "street_litter",
            "street litter",
            -GetWorkerStreetLitterOpinionDelta(severity),
            0f);
    }

    private static int GetWorkerStreetLitterThoughtSeverity(float perception)
    {
        if (perception >= WorkerStreetLitterHighThoughtThreshold) return 3;
        if (perception >= WorkerStreetLitterMediumThoughtThreshold) return 2;
        if (perception >= WorkerStreetLitterLowThoughtThreshold) return 1;
        return 0;
    }

    private static string GetWorkerStreetLitterThoughtKey(int severity)
    {
        return severity switch
        {
            1 => WorkerStreetLitterLowThoughtKey,
            2 => WorkerStreetLitterMediumThoughtKey,
            _ => WorkerStreetLitterHighThoughtKey
        };
    }

    private static int GetWorkerStreetLitterThoughtIntensity(int severity)
    {
        return severity switch
        {
            1 => 32,
            2 => 54,
            _ => 76
        };
    }

    private static int GetWorkerStreetLitterOpinionDelta(int severity)
    {
        return severity switch
        {
            1 => 1,
            2 => 2,
            _ => 4
        };
    }

    private static WorkerThoughtPriority GetWorkerStreetLitterThoughtPriority(int severity)
    {
        return severity switch
        {
            1 => WorkerThoughtPriority.Low,
            2 => WorkerThoughtPriority.Normal,
            _ => WorkerThoughtPriority.High
        };
    }

    private void ResolveInactiveWorkerStreetLitterThoughts(DriverAgent worker, string activeKey)
    {
        ResolveWorkerStreetLitterThoughtIfInactive(worker, WorkerStreetLitterLowThoughtKey, activeKey);
        ResolveWorkerStreetLitterThoughtIfInactive(worker, WorkerStreetLitterMediumThoughtKey, activeKey);
        ResolveWorkerStreetLitterThoughtIfInactive(worker, WorkerStreetLitterHighThoughtKey, activeKey);
    }

    private void ResolveWorkerStreetLitterThoughtIfInactive(DriverAgent worker, string thoughtKey, string activeKey)
    {
        if (!string.Equals(thoughtKey, activeKey, System.StringComparison.Ordinal))
        {
            ResolveActiveWorkerThought(worker, thoughtKey, "litter perception changed");
        }
    }

    private void ResolveWorkerStreetLitterActiveThoughts(DriverAgent worker, string reason)
    {
        ResolveActiveWorkerThought(worker, WorkerStreetLitterLowThoughtKey, reason);
        ResolveActiveWorkerThought(worker, WorkerStreetLitterMediumThoughtKey, reason);
        ResolveActiveWorkerThought(worker, WorkerStreetLitterHighThoughtKey, reason);
    }

    private void AddDailyStreetLitterFactors(DriverAgent worker, int day, List<WorkerDailyOpinionFactor> factors)
    {
        if (worker == null || worker.StreetLitterExposureDay != day)
        {
            return;
        }

        int severity = GetWorkerStreetLitterDailySeverity(worker.StreetLitterExposureToday, worker.StreetLitterPeakToday);
        UpdateWorkerStreetLitterExposureMemory(worker, day, severity);
        if (severity <= 0)
        {
            AddAccumulatedStreetLitterExposureFactor(worker, factors);
            return;
        }

        int dailyScore = GetWorkerStreetLitterDailyScore(severity);
        string reasonRu = GetWorkerStreetLitterDailyReason(severity, true);
        string reasonEn = GetWorkerStreetLitterDailyReason(severity, false);
        SocialSignal signal = RecordWorkerDailyStreetLitterSocialSignal(worker, day, severity, dailyScore, reasonRu, reasonEn);
        AddDailyOpinionFactor(
            factors,
            WorkerDailyOpinionFactorKind.City,
            signal?.DailyScoreHint ?? dailyScore,
            signal?.ReasonRu ?? reasonRu,
            signal?.ReasonEn ?? reasonEn);
        AddAccumulatedStreetLitterExposureFactor(worker, factors);
    }

    private static void UpdateWorkerStreetLitterExposureMemory(DriverAgent worker, int day, int severity)
    {
        if (worker == null || worker.StreetLitterExposureMemoryDay == day)
        {
            return;
        }

        if (worker.StreetLitterExposureMemoryDay > 0 && day > worker.StreetLitterExposureMemoryDay + 1)
        {
            int skippedDays = day - worker.StreetLitterExposureMemoryDay - 1;
            worker.StreetLitterExposureMemory *= Mathf.Pow(0.72f, skippedDays);
        }

        if (severity > 0)
        {
            worker.StreetLitterExposureStreakDays++;
            worker.StreetLitterExposureMemory += severity * 0.62f;
        }
        else
        {
            worker.StreetLitterExposureStreakDays = 0;
            worker.StreetLitterExposureMemory *= 0.58f;
        }

        worker.StreetLitterExposureMemory = Mathf.Clamp(worker.StreetLitterExposureMemory, 0f, 8f);
        worker.StreetLitterExposureMemoryDay = day;
    }

    private static void AddAccumulatedStreetLitterExposureFactor(DriverAgent worker, List<WorkerDailyOpinionFactor> factors)
    {
        if (worker == null || factors == null)
        {
            return;
        }

        int score = GetAccumulatedStreetLitterExposureScore(worker);
        if (score == 0)
        {
            return;
        }

        AddDailyOpinionFactor(
            factors,
            WorkerDailyOpinionFactorKind.City,
            score,
            GetAccumulatedStreetLitterExposureReason(worker, true),
            GetAccumulatedStreetLitterExposureReason(worker, false));
    }

    private static int GetAccumulatedStreetLitterExposureScore(DriverAgent worker)
    {
        if (worker == null)
        {
            return 0;
        }

        if (worker.StreetLitterExposureMemory >= 6f || worker.StreetLitterExposureStreakDays >= 4)
        {
            return -12;
        }

        if (worker.StreetLitterExposureMemory >= 3.2f || worker.StreetLitterExposureStreakDays >= 3)
        {
            return -6;
        }

        return 0;
    }

    private static string GetAccumulatedStreetLitterExposureReason(DriverAgent worker, bool ru)
    {
        bool high = worker != null && (worker.StreetLitterExposureMemory >= 6f || worker.StreetLitterExposureStreakDays >= 4);
        if (high)
        {
            return ru
                ? "\u0433\u0440\u044f\u0437\u043d\u044b\u0435 \u0443\u043b\u0438\u0446\u044b \u0441\u0442\u0430\u043b\u0438 \u0443\u0441\u0442\u043e\u0439\u0447\u0438\u0432\u043e\u0439 \u043f\u0440\u043e\u0431\u043b\u0435\u043c\u043e\u0439"
                : "dirty streets became a persistent problem";
        }

        return ru
            ? "\u043c\u0443\u0441\u043e\u0440 \u043d\u0430 \u043c\u0430\u0440\u0448\u0440\u0443\u0442\u0430\u0445 \u043d\u0430\u043a\u0430\u043f\u043b\u0438\u0432\u0430\u043b \u0440\u0430\u0437\u0434\u0440\u0430\u0436\u0435\u043d\u0438\u0435"
            : "litter on daily routes built up irritation";
    }

    private static int GetWorkerStreetLitterDailySeverity(float exposure, float peak)
    {
        if (exposure >= 44f || peak >= 8.5f) return 4;
        if (exposure >= 22f || peak >= 5.0f) return 3;
        if (exposure >= 6f || peak >= 2.2f) return 2;
        if (exposure >= 0.25f || peak >= WorkerStreetLitterMinimumPerception) return 1;
        return 0;
    }

    private static int GetWorkerStreetLitterDailyScore(int severity)
    {
        return severity switch
        {
            1 => -1,
            2 => -6,
            3 => -13,
            _ => -24
        };
    }

    private static string GetWorkerStreetLitterDailyReason(int severity, bool ru)
    {
        return severity switch
        {
            1 => ru ? "на улице попадался мусор" : "some street litter was noticeable",
            2 => ru ? "улицы местами выглядели грязно" : "some streets looked dirty",
            3 => ru ? "замусоренность улиц раздражала" : "street litter was irritating",
            _ => ru ? "грязные улицы портили день" : "dirty streets dragged the day down"
        };
    }
}
