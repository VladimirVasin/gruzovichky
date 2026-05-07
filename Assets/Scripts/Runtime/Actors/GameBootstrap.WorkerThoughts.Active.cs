using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private WorkerThought AddOrKeepActiveWorkerThought(
        DriverAgent worker,
        string thoughtKey,
        WorkerThoughtKind kind,
        WorkerThoughtTone tone,
        int intensity,
        string templateKey,
        IReadOnlyList<WorkerThoughtPlaceholder> placeholders,
        WorkerThoughtPriority priority,
        WorkerThoughtSubjectType opinionSubjectType = WorkerThoughtSubjectType.None,
        int opinionSubjectId = 0,
        string opinionSubjectKey = null,
        string opinionFallbackLabel = null,
        int opinionDelta = 0,
        float cooldownHours = 0f)
    {
        if (worker == null || string.IsNullOrWhiteSpace(thoughtKey))
        {
            return null;
        }

        WorkerThought existing = FindActiveWorkerThought(worker, thoughtKey);
        if (existing != null)
        {
            existing.Intensity = Mathf.Max(existing.Intensity, Mathf.Clamp(intensity, 0, 100));
            existing.Priority = HighestWorkerThoughtPriority(existing.Priority, priority);
            existing.Tone = tone;
            return existing;
        }

        return RecordWorkerThought(
            worker,
            kind,
            tone,
            intensity,
            templateKey,
            placeholders,
            opinionSubjectType,
            opinionSubjectId,
            opinionSubjectKey,
            opinionFallbackLabel,
            opinionDelta,
            $"active|{thoughtKey}",
            cooldownHours,
            thoughtKey,
            priority,
            true);
    }

    private WorkerThought FindActiveWorkerThought(DriverAgent worker, string thoughtKey)
    {
        if (worker == null || string.IsNullOrWhiteSpace(thoughtKey))
        {
            return null;
        }

        float now = GetCurrentWorldHour();
        for (int i = 0; i < worker.Thoughts.Count; i++)
        {
            WorkerThought thought = worker.Thoughts[i];
            if (thought == null || !thought.Active || !string.Equals(thought.Key, thoughtKey, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (thought.ExpiresWorldHour > 0f && now >= thought.ExpiresWorldHour)
            {
                ResolveWorkerThought(thought, now, "expired");
                continue;
            }

            return thought;
        }

        return null;
    }

    private bool ResolveActiveWorkerThought(DriverAgent worker, string thoughtKey, string reason)
    {
        if (worker == null || string.IsNullOrWhiteSpace(thoughtKey))
        {
            return false;
        }

        bool resolved = false;
        float now = GetCurrentWorldHour();
        for (int i = 0; i < worker.Thoughts.Count; i++)
        {
            WorkerThought thought = worker.Thoughts[i];
            if (thought == null || !thought.Active || !string.Equals(thought.Key, thoughtKey, System.StringComparison.Ordinal))
            {
                continue;
            }

            ResolveWorkerThought(thought, now, reason);
            resolved = true;
        }

        if (resolved)
        {
            isDriversScreenDirty = true;
        }

        return resolved;
    }

    private void ResolveAllActiveWorkerThoughts(DriverAgent worker, string reason)
    {
        if (worker == null)
        {
            return;
        }

        bool resolved = false;
        float now = GetCurrentWorldHour();
        for (int i = 0; i < worker.Thoughts.Count; i++)
        {
            WorkerThought thought = worker.Thoughts[i];
            if (thought == null || !thought.Active)
            {
                continue;
            }

            ResolveWorkerThought(thought, now, reason);
            resolved = true;
        }

        if (resolved)
        {
            isDriversScreenDirty = true;
        }
    }

    private void ResolveWorkerThought(WorkerThought thought, float now, string reason)
    {
        if (thought == null || !thought.Active)
        {
            return;
        }

        thought.Active = false;
        thought.ResolvedDay = currentDay;
        thought.ResolvedWorldHour = now;
        thought.ResolveReason = reason ?? string.Empty;
    }

    private bool HasWorkerThought(DriverAgent worker, string thoughtKey)
    {
        if (worker == null || string.IsNullOrWhiteSpace(thoughtKey))
        {
            return false;
        }

        for (int i = 0; i < worker.Thoughts.Count; i++)
        {
            WorkerThought thought = worker.Thoughts[i];
            if (thought == null)
            {
                continue;
            }

            if (string.Equals(thought.Key, thoughtKey, System.StringComparison.Ordinal) ||
                string.Equals(thought.TemplateKey, thoughtKey, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static WorkerThoughtPriority HighestWorkerThoughtPriority(WorkerThoughtPriority first, WorkerThoughtPriority second)
    {
        return (WorkerThoughtPriority)Mathf.Max((int)first, (int)second);
    }

    private void EvaluateWorkerActiveThoughtRules(DriverAgent worker)
    {
        if (worker == null)
        {
            return;
        }

        if (worker.HasDepartedTown || worker.IsLeavingTown)
        {
            ResolveAllActiveWorkerThoughts(worker, "left town");
            UpdateWorkerLifeOpinionsSnapshot(worker);
            return;
        }

        UpdateWorkerActiveNoJobThought(worker);
        UpdateWorkerActiveLowMoneyThought(worker);
        UpdateWorkerActiveNeedThought(worker, WorkerNeedKind.Meal, worker.LastMealNeedStatus);
        UpdateWorkerActiveNeedThought(worker, WorkerNeedKind.Sleep, worker.LastSleepNeedStatus);
        UpdateWorkerActiveNeedThought(worker, WorkerNeedKind.Leisure, worker.LastLeisureNeedStatus);
        UpdateWorkerLifeOpinionsSnapshot(worker);
    }

    private void UpdateWorkerActiveNoJobThought(DriverAgent worker)
    {
        bool unemployed = IsWorkerUnemployedForThoughts(worker);
        if (!unemployed)
        {
            ResolveActiveWorkerThought(worker, "no_job_warning", "job assigned");
            ResolveActiveWorkerThought(worker, "starter_job_suggestion", "job assigned");
            return;
        }

        int intensity = worker.Money < 50 ? 70 : 52;
        WorkerThoughtPriority priority = worker.Money < 80 ? WorkerThoughtPriority.High : WorkerThoughtPriority.Normal;
        AddOrKeepActiveWorkerThought(
            worker,
            "no_job_warning",
            WorkerThoughtKind.Work,
            WorkerThoughtTone.Negative,
            intensity,
            "no_job_warning",
            new[]
            {
                ThoughtText("reason", worker.LifeGoal == WorkerLifeGoal.FindJob ? "looking for work" : "no assignment")
            },
            priority,
            WorkerThoughtSubjectType.Text,
            0,
            "city_work",
            "work in town",
            -3);

        if (worker.Money < 100 && !HasWorkerThought(worker, "starter_job_resolved"))
        {
            AddOrKeepActiveWorkerThought(
                worker,
                "starter_job_suggestion",
                WorkerThoughtKind.Work,
                WorkerThoughtTone.Neutral,
                42,
                "starter_job_suggestion",
                null,
                WorkerThoughtPriority.Normal,
                WorkerThoughtSubjectType.Text,
                0,
                "city_work",
                "work in town",
                -1);
        }
        else
        {
            ResolveActiveWorkerThought(worker, "starter_job_suggestion", "enough money");
        }
    }

    private bool IsWorkerUnemployedForThoughts(DriverAgent worker)
    {
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
               !IsDriverOnActiveTradeRun(worker);
    }

    private void UpdateWorkerActiveLowMoneyThought(DriverAgent worker)
    {
        if (worker == null)
        {
            return;
        }

        if (worker.Money >= 15)
        {
            ResolveActiveWorkerThought(worker, "low_money", "money recovered");
            return;
        }

        AddOrKeepActiveWorkerThought(
            worker,
            "low_money",
            WorkerThoughtKind.Money,
            WorkerThoughtTone.Negative,
            worker.Money <= 5 ? 82 : 66,
            "low_money",
            new[]
            {
                ThoughtText("balance", $"${worker.Money}")
            },
            worker.Money <= 5 ? WorkerThoughtPriority.Critical : WorkerThoughtPriority.High,
            WorkerThoughtSubjectType.Text,
            0,
            "money",
            "money",
            -5);
    }

    private void UpdateWorkerActiveNeedThought(DriverAgent worker, WorkerNeedKind need, WorkerNeedStatus status)
    {
        string warningKey = GetWorkerNeedThoughtKey(need, WorkerNeedStatus.Warning);
        string criticalKey = GetWorkerNeedThoughtKey(need, WorkerNeedStatus.Critical);
        if (status == WorkerNeedStatus.Ok)
        {
            ResolveActiveWorkerThought(worker, warningKey, "need recovered");
            ResolveActiveWorkerThought(worker, criticalKey, "need recovered");
            return;
        }

        if (status == WorkerNeedStatus.Warning)
        {
            ResolveActiveWorkerThought(worker, criticalKey, "need less urgent");
        }
        else
        {
            ResolveActiveWorkerThought(worker, warningKey, "need escalated");
        }

        string key = GetWorkerNeedThoughtKey(need, status);
        AddOrKeepActiveWorkerThought(
            worker,
            key,
            WorkerThoughtKind.Need,
            WorkerThoughtTone.Negative,
            status == WorkerNeedStatus.Critical ? 90 : 58,
            key,
            new[]
            {
                ThoughtNeed("need", need)
            },
            status == WorkerNeedStatus.Critical ? WorkerThoughtPriority.Critical : WorkerThoughtPriority.High,
            WorkerThoughtSubjectType.Need,
            0,
            need.ToString(),
            FormatWorkerThoughtNeed(need.ToString(), false),
            status == WorkerNeedStatus.Critical ? -6 : -2);
    }

    private static string GetWorkerNeedThoughtKey(WorkerNeedKind need, WorkerNeedStatus status)
    {
        string needKey = need.ToString().ToLowerInvariant();
        string statusKey = status.ToString().ToLowerInvariant();
        return $"need_{needKey}_{statusKey}";
    }

    private void RecordWorkerNeedConsumableThought(DriverAgent worker, string itemId, WorkerNeedKind need, WorkerNeedStatus oldStatus)
    {
        if (worker == null)
        {
            return;
        }

        bool isSnack = string.Equals(itemId, WorkerSnackItemId, System.StringComparison.Ordinal);
        string templateKey = isSnack ? "used_snack" : "used_coffee";
        RecordWorkerThought(
            worker,
            WorkerThoughtKind.Need,
            WorkerThoughtTone.Positive,
            oldStatus == WorkerNeedStatus.Critical ? 72 : 54,
            templateKey,
            new[]
            {
                ThoughtNeed("need", need)
            },
            WorkerThoughtSubjectType.Need,
            0,
            need.ToString(),
            FormatWorkerThoughtNeed(need.ToString(), false),
            4,
            $"{templateKey}|{Mathf.FloorToInt(GetCurrentWorldHour() * 4f)}",
            0f,
            templateKey,
            WorkerThoughtPriority.Normal);
    }

    private void RecordWorkerJobFoundThought(DriverAgent worker, VacancyKind kind, LocationType buildingType)
    {
        if (worker == null)
        {
            return;
        }

        ResolveActiveWorkerThought(worker, "no_job_warning", "job found");
        ResolveActiveWorkerThought(worker, "starter_job_suggestion", "job found");
        RecordWorkerThought(
            worker,
            WorkerThoughtKind.Work,
            WorkerThoughtTone.Positive,
            66,
            "job_found",
            new[]
            {
                ThoughtText("job", FormatWorkerJobFoundLabel(kind, buildingType))
            },
            WorkerThoughtSubjectType.Text,
            0,
            "city_work",
            "work in town",
            8,
            $"job_found|{worker.DriverId}|{currentDay}|{kind}|{buildingType}",
            0f,
            "job_found",
            WorkerThoughtPriority.Normal);
        RecordWorkerThought(
            worker,
            WorkerThoughtKind.Work,
            WorkerThoughtTone.Neutral,
            8,
            "starter_job_resolved",
            null,
            WorkerThoughtSubjectType.None,
            0,
            null,
            null,
            0,
            $"starter_job_resolved|{worker.DriverId}",
            0f,
            "starter_job_resolved",
            WorkerThoughtPriority.Low);
    }

    private string FormatWorkerJobFoundLabel(VacancyKind kind, LocationType buildingType)
    {
        return kind switch
        {
            VacancyKind.TruckDriver => "truck driving",
            VacancyKind.BusDriver => "bus driving",
            VacancyKind.Production => GetSelectedLocationDisplayName(buildingType),
            VacancyKind.Service => GetSelectedLocationDisplayName(buildingType),
            _ => kind.ToString()
        };
    }

    private WorkerThought GetMostImportantWorkerThought(DriverAgent worker)
    {
        if (worker == null)
        {
            return null;
        }

        WorkerThought best = null;
        int bestScore = int.MinValue;
        for (int i = 0; i < worker.Thoughts.Count; i++)
        {
            WorkerThought thought = worker.Thoughts[i];
            if (thought == null || !thought.Active)
            {
                continue;
            }

            int score = CalculateWorkerThoughtImportanceScore(worker, thought);
            if (score > bestScore)
            {
                bestScore = score;
                best = thought;
            }
        }

        return best;
    }

    private int CalculateWorkerThoughtImportanceScore(DriverAgent worker, WorkerThought thought)
    {
        if (worker == null || thought == null)
        {
            return 0;
        }

        int score = thought.Priority switch
        {
            WorkerThoughtPriority.Critical => 100,
            WorkerThoughtPriority.High => 70,
            WorkerThoughtPriority.Normal => 40,
            _ => 20
        };
        score += Mathf.RoundToInt(thought.Intensity * 0.35f);

        float ageHours = Mathf.Max(0f, GetCurrentWorldHour() - thought.CreatedWorldHour);
        score += Mathf.Clamp(18 - Mathf.RoundToInt(ageHours), 0, 18);

        if (thought.Kind == WorkerThoughtKind.Need)
        {
            score += 12;
        }

        if (string.Equals(thought.Key, "no_job_warning", System.StringComparison.Ordinal) && worker.Money < 80)
        {
            score += 18;
        }

        if (string.Equals(thought.Key, "low_money", System.StringComparison.Ordinal))
        {
            score += Mathf.Clamp(25 - worker.Money, 0, 25);
        }

        return score;
    }

    private void UpdateWorkerLifeOpinionsSnapshot(DriverAgent worker)
    {
        if (worker == null)
        {
            return;
        }

        float now = GetCurrentWorldHour();
        bool employed = !IsWorkerUnemployedForThoughts(worker) && !worker.IsArrivingByBus && !worker.IsLeavingTown && !worker.HasDepartedTown;
        if (employed)
        {
            int workScore = worker.ContractWorkedDays > 0 ? 35 : 25;
            SetWorkerLifeOpinion(worker, WorkerLifeOpinionCategory.Work, true, workScore, 70, now);
        }
        else if (IsWorkerUnemployedForThoughts(worker))
        {
            SetWorkerLifeOpinion(worker, WorkerLifeOpinionCategory.Work, true, worker.Money < 50 ? -65 : -55, 85, now);
        }
        else
        {
            SetWorkerLifeOpinion(worker, WorkerLifeOpinionCategory.Work, false, 0, 0, now);
        }

        int moneyScore = worker.Money switch
        {
            < 15 => -70,
            < 50 => IsWorkerUnemployedForThoughts(worker) ? -45 : -25,
            < 100 => IsWorkerUnemployedForThoughts(worker) ? -25 : 0,
            _ => 15
        };
        SetWorkerLifeOpinion(worker, WorkerLifeOpinionCategory.Money, true, moneyScore, worker.Money < 100 ? 75 : 55, now);

        if (worker.AssignedPersonalHouseIndex >= 0)
        {
            SetWorkerLifeOpinion(worker, WorkerLifeOpinionCategory.Housing, true, 45, 75, now);
        }
        else
        {
            SetWorkerLifeOpinion(worker, WorkerLifeOpinionCategory.Housing, false, 0, 0, now);
        }

        int cityScore = worker.DepartureIntent ? -45
            : worker.Satisfaction >= 80 ? 25
            : worker.Satisfaction < 45 ? -25
            : 0;
        int cityConfidence = Mathf.Clamp(worker.DaysOnMap * 10 + 25, 25, 80);
        SetWorkerLifeOpinion(worker, WorkerLifeOpinionCategory.City, true, cityScore, cityConfidence, now);

        if (worker.SocialMemories.Count <= 0)
        {
            SetWorkerLifeOpinion(worker, WorkerLifeOpinionCategory.Social, false, 0, 0, now);
            return;
        }

        int relationshipSum = 0;
        int familiaritySum = 0;
        int counted = 0;
        for (int i = 0; i < worker.SocialMemories.Count; i++)
        {
            WorkerSocialMemory memory = worker.SocialMemories[i];
            if (memory == null || memory.Familiarity <= 0)
            {
                continue;
            }

            relationshipSum += memory.Relationship;
            familiaritySum += memory.Familiarity;
            counted++;
        }

        if (counted <= 0)
        {
            SetWorkerLifeOpinion(worker, WorkerLifeOpinionCategory.Social, false, 0, 0, now);
            return;
        }

        int socialScore = Mathf.Clamp(Mathf.RoundToInt((float)relationshipSum / counted), -100, 100);
        int socialConfidence = Mathf.Clamp(counted * 12 + familiaritySum / Mathf.Max(1, counted) / 2, 20, 95);
        SetWorkerLifeOpinion(worker, WorkerLifeOpinionCategory.Social, true, socialScore, socialConfidence, now);
    }

    private void SetWorkerLifeOpinion(DriverAgent worker, WorkerLifeOpinionCategory category, bool hasScore, int score, int confidence, float now)
    {
        WorkerLifeOpinion opinion = FindWorkerLifeOpinion(worker, category);
        if (opinion == null)
        {
            opinion = new WorkerLifeOpinion
            {
                Category = category
            };
            worker.LifeOpinions.Add(opinion);
        }

        opinion.HasScore = hasScore;
        opinion.Score = Mathf.Clamp(score, -100, 100);
        opinion.Confidence = Mathf.Clamp(confidence, 0, 100);
        opinion.LastUpdatedWorldHour = now;
    }

    private static WorkerLifeOpinion FindWorkerLifeOpinion(DriverAgent worker, WorkerLifeOpinionCategory category)
    {
        if (worker == null)
        {
            return null;
        }

        for (int i = 0; i < worker.LifeOpinions.Count; i++)
        {
            WorkerLifeOpinion opinion = worker.LifeOpinions[i];
            if (opinion != null && opinion.Category == category)
            {
                return opinion;
            }
        }

        return null;
    }
}

