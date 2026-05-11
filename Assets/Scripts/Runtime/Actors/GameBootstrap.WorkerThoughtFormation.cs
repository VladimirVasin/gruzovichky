using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerPendingThoughtCap = 8;
    private const float WorkerThoughtFormationCheckIntervalHours = 0.25f;
    private const float WorkerThoughtDefaultFormationHours = 1.4f;

    private static readonly LocationType[] WorkerThoughtMealKnowledgeTypes =
    {
        LocationType.Canteen,
        LocationType.Kiosk
    };

    private static readonly LocationType[] WorkerThoughtSleepKnowledgeTypes =
    {
        LocationType.PersonalHouse,
        LocationType.Motel
    };

    private static readonly LocationType[] WorkerThoughtLeisureKnowledgeTypes =
    {
        LocationType.Bar,
        LocationType.CityPark,
        LocationType.GamblingHall
    };

    private static readonly LocationType[] WorkerThoughtWorkKnowledgeTypes =
    {
        LocationType.LaborExchange,
        LocationType.Warehouse,
        LocationType.Forest,
        LocationType.Sawmill,
        LocationType.FurnitureFactory,
        LocationType.Canteen,
        LocationType.Bar,
        LocationType.Motel,
        LocationType.GasStation,
        LocationType.CleaningDepot
    };

    private float lastWorkerThoughtFormationCheckWorldHour = -1f;

    private PendingWorkerThought AddOrKeepPendingWorkerThought(
        DriverAgent worker,
        string formationKey,
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
        string cooldownKey = null,
        float cooldownHours = WorkerThoughtDefaultCooldownHours,
        bool active = false,
        float expiresWorldHour = 0f,
        float formationHours = -1f,
        string formationReason = null,
        WorkerMemory sourceKnowledge = null)
    {
        if (worker == null || string.IsNullOrWhiteSpace(templateKey) || worker.HasDepartedTown)
        {
            return null;
        }

        float now = GetCurrentWorldHour();
        string resolvedThoughtKey = string.IsNullOrWhiteSpace(thoughtKey) ? templateKey : thoughtKey;
        string resolvedFormationKey = string.IsNullOrWhiteSpace(formationKey)
            ? resolvedThoughtKey
            : formationKey;
        string resolvedCooldownKey = string.IsNullOrWhiteSpace(cooldownKey)
            ? $"{templateKey}|{opinionSubjectType}|{opinionSubjectId}|{opinionSubjectKey}"
            : cooldownKey;
        if (cooldownHours > 0f &&
            worker.WorkerThoughtCooldownWorldHours.TryGetValue(resolvedCooldownKey, out float nextAllowedHour) &&
            now < nextAllowedHour)
        {
            return null;
        }

        List<WorkerThoughtPlaceholder> pendingPlaceholders = CopyWorkerThoughtPlaceholders(placeholders);
        WorkerMemory knowledgeContext = sourceKnowledge;
        ApplyWorkerThoughtKnowledgeContext(
            worker,
            resolvedThoughtKey,
            ref templateKey,
            pendingPlaceholders,
            ref opinionSubjectType,
            ref opinionSubjectId,
            ref opinionSubjectKey,
            ref opinionFallbackLabel,
            ref opinionDelta,
            ref knowledgeContext);

        float resolvedFormationHours = formationHours > 0f
            ? formationHours
            : GetWorkerThoughtFormationHours(worker, priority, intensity, knowledgeContext);
        PendingWorkerThought pending = FindPendingWorkerThought(worker, resolvedFormationKey);
        if (pending == null)
        {
            pending = new PendingWorkerThought
            {
                FormationKey = resolvedFormationKey,
                Priority = priority,
                StartedDay = currentDay,
                StartedWorldHour = now,
                ReadyWorldHour = now + resolvedFormationHours
            };
            worker.PendingThoughts.Insert(0, pending);
            TrimWorkerPendingThoughts(worker);
        }
        else
        {
            pending.ReadyWorldHour = Mathf.Min(pending.ReadyWorldHour, now + resolvedFormationHours);
        }

        pending.ThoughtKey = resolvedThoughtKey;
        pending.Kind = kind;
        pending.Tone = tone;
        pending.Priority = HighestWorkerThoughtPriority(pending.Priority, priority);
        pending.Intensity = Mathf.Max(pending.Intensity, Mathf.Clamp(intensity, 0, 100));
        pending.TemplateKey = templateKey;
        pending.OpinionSubjectType = opinionSubjectType;
        pending.OpinionSubjectId = opinionSubjectId;
        pending.OpinionSubjectKey = opinionSubjectKey;
        pending.OpinionFallbackLabel = opinionFallbackLabel;
        pending.OpinionDelta = opinionDelta;
        pending.CooldownKey = resolvedCooldownKey;
        pending.CooldownHours = cooldownHours;
        pending.Active = active;
        pending.ExpiresWorldHour = expiresWorldHour;
        pending.LastRefreshedWorldHour = now;
        pending.FormationReason = formationReason ?? string.Empty;
        pending.Placeholders.Clear();
        pending.Placeholders.AddRange(pendingPlaceholders);
        SnapshotPendingWorkerThoughtKnowledge(pending, knowledgeContext);
        isDriversScreenDirty = true;
        return pending;
    }

    private static List<WorkerThoughtPlaceholder> CopyWorkerThoughtPlaceholders(IReadOnlyList<WorkerThoughtPlaceholder> placeholders)
    {
        List<WorkerThoughtPlaceholder> result = new();
        if (placeholders == null)
        {
            return result;
        }

        for (int i = 0; i < placeholders.Count; i++)
        {
            WorkerThoughtPlaceholder placeholder = placeholders[i];
            if (placeholder == null)
            {
                continue;
            }

            result.Add(new WorkerThoughtPlaceholder
            {
                Key = placeholder.Key,
                SubjectType = placeholder.SubjectType,
                SubjectId = placeholder.SubjectId,
                SubjectKey = placeholder.SubjectKey,
                FallbackLabel = placeholder.FallbackLabel
            });
        }

        return result;
    }

    private float GetWorkerThoughtFormationHours(
        DriverAgent worker,
        WorkerThoughtPriority priority,
        int intensity,
        WorkerMemory knowledgeContext)
    {
        float baseHours = priority switch
        {
            WorkerThoughtPriority.Critical => 0.22f,
            WorkerThoughtPriority.High => 0.55f,
            WorkerThoughtPriority.Normal => WorkerThoughtDefaultFormationHours,
            _ => 2.1f
        };

        baseHours -= Mathf.Clamp01(intensity / 100f) * 0.28f;
        if (knowledgeContext != null)
        {
            baseHours *= knowledgeContext.Kind == WorkerMemoryKind.ConversationTopic ? 0.75f : 0.86f;
        }

        int seed = (worker?.DriverId ?? 0) * 37 + Mathf.RoundToInt(GetCurrentWorldHour() * 5f);
        float wobble = (Mathf.Abs(seed) % 17) / 100f;
        return Mathf.Clamp(baseHours + wobble, 0.12f, 3.4f);
    }

    private PendingWorkerThought FindPendingWorkerThought(DriverAgent worker, string formationKey)
    {
        if (worker == null || string.IsNullOrWhiteSpace(formationKey))
        {
            return null;
        }

        for (int i = 0; i < worker.PendingThoughts.Count; i++)
        {
            PendingWorkerThought pending = worker.PendingThoughts[i];
            if (pending != null && string.Equals(pending.FormationKey, formationKey, System.StringComparison.Ordinal))
            {
                return pending;
            }
        }

        return null;
    }

    private void TrimWorkerPendingThoughts(DriverAgent worker)
    {
        while (worker != null && worker.PendingThoughts.Count > WorkerPendingThoughtCap)
        {
            worker.PendingThoughts.RemoveAt(worker.PendingThoughts.Count - 1);
        }
    }

    private bool CancelPendingWorkerThought(DriverAgent worker, string thoughtKeyOrFormationKey, string reason)
    {
        if (worker == null || string.IsNullOrWhiteSpace(thoughtKeyOrFormationKey))
        {
            return false;
        }

        bool changed = false;
        for (int i = worker.PendingThoughts.Count - 1; i >= 0; i--)
        {
            PendingWorkerThought pending = worker.PendingThoughts[i];
            if (pending == null ||
                (!string.Equals(pending.ThoughtKey, thoughtKeyOrFormationKey, System.StringComparison.Ordinal) &&
                 !string.Equals(pending.FormationKey, thoughtKeyOrFormationKey, System.StringComparison.Ordinal)))
            {
                continue;
            }

            worker.PendingThoughts.RemoveAt(i);
            changed = true;
        }

        if (changed)
        {
            isDriversScreenDirty = true;
            SessionDebugLogger.Log("THOUGHT", $"{GetWorkerDisplayNameSafe(worker)} stopped forming thought '{thoughtKeyOrFormationKey}': {reason}.");
        }

        return changed;
    }

    private bool CancelAllPendingWorkerThoughts(DriverAgent worker, string reason)
    {
        if (worker == null || worker.PendingThoughts.Count == 0)
        {
            return false;
        }

        worker.PendingThoughts.Clear();
        isDriversScreenDirty = true;
        SessionDebugLogger.Log("THOUGHT", $"{GetWorkerDisplayNameSafe(worker)} cleared pending thoughts: {reason}.");
        return true;
    }

    private void UpdateWorkerThoughtFormationRuntime()
    {
        float now = GetCurrentWorldHour();
        if (lastWorkerThoughtFormationCheckWorldHour >= 0f &&
            now >= lastWorkerThoughtFormationCheckWorldHour &&
            now - lastWorkerThoughtFormationCheckWorldHour < WorkerThoughtFormationCheckIntervalHours)
        {
            return;
        }

        lastWorkerThoughtFormationCheckWorldHour = now;
        bool changed = false;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (worker == null)
            {
                continue;
            }

            EvaluateWorkerActiveThoughtRules(worker);
            changed |= UpdatePendingWorkerThoughtFormation(worker, now);
        }

        if (changed)
        {
            isDriversScreenDirty = true;
        }
    }

    private bool UpdatePendingWorkerThoughtFormation(DriverAgent worker, float now)
    {
        if (worker == null || worker.PendingThoughts.Count == 0)
        {
            return false;
        }

        bool changed = false;
        for (int i = worker.PendingThoughts.Count - 1; i >= 0; i--)
        {
            PendingWorkerThought pending = worker.PendingThoughts[i];
            if (pending == null)
            {
                worker.PendingThoughts.RemoveAt(i);
                changed = true;
                continue;
            }

            if (!IsPendingWorkerThoughtStillValid(worker, pending, now))
            {
                worker.PendingThoughts.RemoveAt(i);
                changed = true;
                continue;
            }

            if (now < pending.ReadyWorldHour)
            {
                continue;
            }

            RecordWorkerThought(
                worker,
                pending.Kind,
                pending.Tone,
                pending.Intensity,
                pending.TemplateKey,
                pending.Placeholders,
                pending.OpinionSubjectType,
                pending.OpinionSubjectId,
                pending.OpinionSubjectKey,
                pending.OpinionFallbackLabel,
                pending.OpinionDelta,
                pending.CooldownKey,
                pending.CooldownHours,
                pending.ThoughtKey,
                pending.Priority,
                pending.Active,
                pending.ExpiresWorldHour);
            worker.PendingThoughts.RemoveAt(i);
            changed = true;
        }

        return changed;
    }

    private bool IsPendingWorkerThoughtStillValid(DriverAgent worker, PendingWorkerThought pending, float now)
    {
        if (worker == null || pending == null || worker.HasDepartedTown || worker.IsLeavingTown)
        {
            return false;
        }

        if (!IsPendingWorkerKnowledgeStillKnown(worker, pending, now))
        {
            return false;
        }

        string key = pending.ThoughtKey ?? string.Empty;
        if (string.Equals(key, "no_job_warning", System.StringComparison.Ordinal) ||
            string.Equals(key, "starter_job_suggestion", System.StringComparison.Ordinal))
        {
            return IsWorkerUnemployedForThoughts(worker);
        }

        if (string.Equals(key, "low_money", System.StringComparison.Ordinal))
        {
            return worker.Money < 15;
        }

        if (string.Equals(key, GetWorkerNeedThoughtKey(WorkerNeedKind.Meal, WorkerNeedStatus.Critical), System.StringComparison.Ordinal))
        {
            return worker.LastMealNeedStatus == WorkerNeedStatus.Critical;
        }

        if (string.Equals(key, GetWorkerNeedThoughtKey(WorkerNeedKind.Sleep, WorkerNeedStatus.Critical), System.StringComparison.Ordinal))
        {
            return worker.LastSleepNeedStatus == WorkerNeedStatus.Critical;
        }

        if (string.Equals(key, GetWorkerNeedThoughtKey(WorkerNeedKind.Leisure, WorkerNeedStatus.Critical), System.StringComparison.Ordinal))
        {
            return worker.LastLeisureNeedStatus == WorkerNeedStatus.Critical;
        }

        return true;
    }

    private void ApplyWorkerThoughtKnowledgeContext(
        DriverAgent worker,
        string thoughtKey,
        ref string templateKey,
        List<WorkerThoughtPlaceholder> placeholders,
        ref WorkerThoughtSubjectType opinionSubjectType,
        ref int opinionSubjectId,
        ref string opinionSubjectKey,
        ref string opinionFallbackLabel,
        ref int opinionDelta,
        ref WorkerMemory knowledgeContext)
    {
        if (worker == null || placeholders == null)
        {
            return;
        }

        float now = GetCurrentWorldHour();
        WorkerMemory memory = null;
        if (string.Equals(thoughtKey, "no_job_warning", System.StringComparison.Ordinal))
        {
            if (TryFindWorkerBuildingKnowledge(worker, now, WorkerThoughtWorkKnowledgeTypes, out memory))
            {
                templateKey = "no_job_warning_known_place";
            }
        }
        else if (string.Equals(thoughtKey, GetWorkerNeedThoughtKey(WorkerNeedKind.Meal, WorkerNeedStatus.Critical), System.StringComparison.Ordinal))
        {
            if (TryFindWorkerBuildingKnowledge(worker, now, WorkerThoughtMealKnowledgeTypes, out memory))
            {
                templateKey = "need_meal_critical_known_place";
            }
        }
        else if (string.Equals(thoughtKey, GetWorkerNeedThoughtKey(WorkerNeedKind.Sleep, WorkerNeedStatus.Critical), System.StringComparison.Ordinal))
        {
            if (TryFindWorkerBuildingKnowledge(worker, now, WorkerThoughtSleepKnowledgeTypes, out memory))
            {
                templateKey = "need_sleep_critical_known_place";
            }
        }
        else if (string.Equals(thoughtKey, GetWorkerNeedThoughtKey(WorkerNeedKind.Leisure, WorkerNeedStatus.Critical), System.StringComparison.Ordinal))
        {
            if (TryFindWorkerBuildingKnowledge(worker, now, WorkerThoughtLeisureKnowledgeTypes, out memory))
            {
                templateKey = "need_leisure_critical_known_place";
            }
        }

        if (memory == null)
        {
            return;
        }

        UpsertWorkerThoughtPlaceholder(placeholders, ThoughtText("knownPlace", GetWorkerKnowledgeBuildingDisplayName(memory, IsRussianLanguage())));
        knowledgeContext ??= memory;
        if (memory.BuildingType.HasValue)
        {
            opinionSubjectType = WorkerThoughtSubjectType.BuildingType;
            opinionSubjectId = 0;
            opinionSubjectKey = memory.BuildingType.Value.ToString();
            opinionFallbackLabel = GetWorkerKnowledgeBuildingDisplayName(memory, false);
            opinionDelta = Mathf.Max(opinionDelta, 1);
        }
    }

    private bool TryFindWorkerBuildingKnowledge(
        DriverAgent worker,
        float now,
        IReadOnlyList<LocationType> buildingTypes,
        out WorkerMemory result)
    {
        result = null;
        if (worker == null || buildingTypes == null)
        {
            return false;
        }

        for (int typeIndex = 0; typeIndex < buildingTypes.Count; typeIndex++)
        {
            LocationType type = buildingTypes[typeIndex];
            WorkerMemory newestForType = null;
            float newestWorldHour = float.MinValue;
            for (int i = 0; i < worker.Memories.Count; i++)
            {
                WorkerMemory memory = worker.Memories[i];
                if (memory == null ||
                    memory.Kind != WorkerMemoryKind.BuildingExistence ||
                    memory.BuildingType != type ||
                    !IsWorkerMemoryDisplayable(memory) ||
                    ShouldExpireWorkerMemory(memory, now))
                {
                    continue;
                }

                if (memory.CreatedWorldHour > newestWorldHour)
                {
                    newestWorldHour = memory.CreatedWorldHour;
                    newestForType = memory;
                }
            }

            if (newestForType != null)
            {
                result = newestForType;
                return true;
            }

            if (TryFindCityCanonBuildingKnowledge(type, out WorkerMemory cityCanonMemory))
            {
                result = cityCanonMemory;
                return true;
            }
        }

        return false;
    }

    private static void UpsertWorkerThoughtPlaceholder(List<WorkerThoughtPlaceholder> placeholders, WorkerThoughtPlaceholder placeholder)
    {
        if (placeholders == null || placeholder == null || string.IsNullOrWhiteSpace(placeholder.Key))
        {
            return;
        }

        for (int i = 0; i < placeholders.Count; i++)
        {
            if (string.Equals(placeholders[i]?.Key, placeholder.Key, System.StringComparison.Ordinal))
            {
                placeholders[i] = placeholder;
                return;
            }
        }

        placeholders.Add(placeholder);
    }

    private void QueueWorkerKnowledgeReflectionThought(DriverAgent owner, DriverAgent other, WorkerMemory memory, float now)
    {
        if (owner == null || memory == null || !IsWorkerMemoryDisplayable(memory) || ShouldExpireWorkerMemory(memory, now))
        {
            return;
        }

        if (memory.Kind == WorkerMemoryKind.ConversationTopic)
        {
            string topic = GetWorkerRumorTopic(memory);
            string normalizedTopic = NormalizeWorkerKnowledgeTopicKey(topic);
            AddOrKeepPendingWorkerThought(
                owner,
                $"knowledge_topic|{owner.DriverId}|{memory.OtherWorkerId}|{normalizedTopic}",
                "social_learned_new_topic",
                WorkerThoughtKind.Social,
                memory.Positive ? WorkerThoughtTone.Positive : WorkerThoughtTone.Neutral,
                memory.Positive ? 58 : 44,
                "social_learned_new_topic",
                new[]
                {
                    ThoughtWorker("otherWorker", other ?? GetDriverAgentById(memory.OtherWorkerId)),
                    ThoughtText("topic", topic)
                },
                WorkerThoughtPriority.Normal,
                WorkerThoughtSubjectType.Worker,
                memory.OtherWorkerId,
                null,
                other?.DriverName ?? GetDriverAgentById(memory.OtherWorkerId)?.DriverName,
                memory.Positive ? 3 : 1,
                $"social_learned_new_topic|{owner.DriverId}|{memory.OtherWorkerId}|{normalizedTopic}",
                0f,
                false,
                0f,
                GetWorkerKnowledgeReflectionFormationHours(memory),
                "knowledge topic",
                memory);
            return;
        }

        if (memory.Kind != WorkerMemoryKind.BuildingExistence || !memory.BuildingType.HasValue)
        {
            return;
        }

        string label = GetWorkerKnowledgeBuildingDisplayName(memory, IsRussianLanguage());
        AddOrKeepPendingWorkerThought(
            owner,
            $"knowledge_building|{owner.DriverId}|{memory.BuildingType.Value}|{memory.BuildingInstanceId}",
            "knowledge_reflection_building",
            WorkerThoughtKind.City,
            WorkerThoughtTone.Neutral,
            Mathf.Clamp(32 + GetWorkerKnowledgeIteration(memory) * 3, 30, 58),
            "knowledge_reflection_building",
            new[]
            {
                ThoughtText("knownPlace", label)
            },
            WorkerThoughtPriority.Low,
            WorkerThoughtSubjectType.BuildingType,
            0,
            memory.BuildingType.Value.ToString(),
            GetWorkerKnowledgeBuildingDisplayName(memory, false),
            1,
            $"knowledge_reflection_building|{owner.DriverId}|{memory.BuildingType.Value}|{memory.BuildingInstanceId}",
            0f,
            false,
            0f,
            GetWorkerKnowledgeReflectionFormationHours(memory),
            "building knowledge",
            memory);
    }

    private static float GetWorkerKnowledgeReflectionFormationHours(WorkerMemory memory)
    {
        if (memory == null)
        {
            return WorkerThoughtDefaultFormationHours;
        }

        float baseHours = memory.Kind == WorkerMemoryKind.ConversationTopic ? 0.85f : 1.35f;
        return Mathf.Clamp(baseHours + Mathf.Max(0, GetWorkerKnowledgeIteration(memory) - 1) * 0.12f, 0.45f, 2.6f);
    }

    private static void SnapshotPendingWorkerThoughtKnowledge(PendingWorkerThought pending, WorkerMemory memory)
    {
        if (pending == null)
        {
            return;
        }

        pending.HasKnowledgeSnapshot = memory != null;
        if (memory == null)
        {
            pending.KnowledgeTopic = string.Empty;
            pending.KnowledgeRumorRootId = 0;
            pending.KnowledgeOriginalTopic = string.Empty;
            pending.KnowledgeBuildingType = null;
            pending.KnowledgeBuildingInstanceId = 0;
            pending.KnowledgeBuildingLabel = string.Empty;
            pending.KnowledgeExpiresWorldHour = 0f;
            pending.KnowledgeIsCityCanon = false;
            return;
        }

        pending.KnowledgeKind = memory.Kind;
        pending.KnowledgeOtherWorkerId = memory.OtherWorkerId;
        pending.KnowledgeTopic = GetWorkerRumorTopic(memory);
        pending.KnowledgeRumorRootId = memory.RumorRootId;
        pending.KnowledgeOriginalTopic = GetWorkerRumorOriginalTopic(memory);
        pending.KnowledgeBuildingType = memory.BuildingType;
        pending.KnowledgeBuildingInstanceId = memory.BuildingInstanceId;
        pending.KnowledgeBuildingLabel = memory.BuildingLabel ?? string.Empty;
        pending.KnowledgeExpiresWorldHour = GetWorkerMemoryExpiresWorldHour(memory);
        pending.KnowledgeIsCityCanon = memory.IsCityCanonKnowledge;
    }

    private bool IsPendingWorkerKnowledgeStillKnown(DriverAgent worker, PendingWorkerThought pending, float now)
    {
        if (worker == null || pending == null || !pending.HasKnowledgeSnapshot)
        {
            return true;
        }

        if (pending.KnowledgeIsCityCanon)
        {
            return true;
        }

        WorkerMemory canonProbe = new()
        {
            Kind = pending.KnowledgeKind,
            OtherWorkerId = pending.KnowledgeOtherWorkerId,
            Topic = pending.KnowledgeTopic,
            RumorRootId = pending.KnowledgeRumorRootId,
            OriginalTopic = pending.KnowledgeOriginalTopic,
            RumorTopic = pending.KnowledgeTopic,
            BuildingType = pending.KnowledgeBuildingType,
            BuildingInstanceId = pending.KnowledgeBuildingInstanceId,
            BuildingLabel = pending.KnowledgeBuildingLabel
        };
        if (HasCityKnowledgeCanonEquivalent(canonProbe))
        {
            return true;
        }

        for (int i = 0; i < worker.Memories.Count; i++)
        {
            WorkerMemory memory = worker.Memories[i];
            if (memory == null ||
                memory.Kind != pending.KnowledgeKind ||
                ShouldExpireWorkerMemory(memory, now))
            {
                continue;
            }

            if (pending.KnowledgeKind == WorkerMemoryKind.ConversationTopic &&
                memory.OtherWorkerId == pending.KnowledgeOtherWorkerId &&
                string.Equals(
                    NormalizeWorkerKnowledgeTopicKey(GetWorkerRumorTopic(memory)),
                    NormalizeWorkerKnowledgeTopicKey(pending.KnowledgeTopic),
                    System.StringComparison.Ordinal))
            {
                return true;
            }

            if (pending.KnowledgeKind == WorkerMemoryKind.BuildingExistence &&
                memory.BuildingType == pending.KnowledgeBuildingType &&
                memory.BuildingInstanceId == pending.KnowledgeBuildingInstanceId)
            {
                return true;
            }
        }

        return false;
    }

    private PendingWorkerThought GetMostImportantPendingWorkerThought(DriverAgent worker)
    {
        if (worker == null || worker.PendingThoughts.Count == 0)
        {
            return null;
        }

        float now = GetCurrentWorldHour();
        PendingWorkerThought best = null;
        int bestScore = int.MinValue;
        for (int i = 0; i < worker.PendingThoughts.Count; i++)
        {
            PendingWorkerThought pending = worker.PendingThoughts[i];
            if (pending == null || !IsPendingWorkerThoughtStillValid(worker, pending, now))
            {
                continue;
            }

            int score = CalculatePendingWorkerThoughtImportanceScore(pending, now);
            if (score > bestScore)
            {
                bestScore = score;
                best = pending;
            }
        }

        return best;
    }

    private static int CalculatePendingWorkerThoughtImportanceScore(PendingWorkerThought pending, float now)
    {
        if (pending == null)
        {
            return 0;
        }

        int score = pending.Priority switch
        {
            WorkerThoughtPriority.Critical => 90,
            WorkerThoughtPriority.High => 62,
            WorkerThoughtPriority.Normal => 38,
            _ => 18
        };
        score += Mathf.RoundToInt(pending.Intensity * 0.25f);
        float total = Mathf.Max(0.01f, pending.ReadyWorldHour - pending.StartedWorldHour);
        float progress = Mathf.Clamp01((now - pending.StartedWorldHour) / total);
        score += Mathf.RoundToInt(progress * 24f);
        if (pending.HasKnowledgeSnapshot)
        {
            score += 8;
        }

        return score;
    }

    private string RenderPendingWorkerThought(PendingWorkerThought pending, bool ru)
    {
        if (pending == null)
        {
            return string.Empty;
        }

        string text = GetWorkerThoughtTemplate(pending.TemplateKey, ru);
        for (int i = 0; i < pending.Placeholders.Count; i++)
        {
            WorkerThoughtPlaceholder placeholder = pending.Placeholders[i];
            if (placeholder == null || string.IsNullOrWhiteSpace(placeholder.Key))
            {
                continue;
            }

            text = text.Replace("{" + placeholder.Key + "}", ResolveWorkerThoughtPlaceholder(placeholder, ru));
        }

        return text;
    }

    private string FormatPendingWorkerThoughtProgress(PendingWorkerThought pending, bool ru)
    {
        if (pending == null)
        {
            return string.Empty;
        }

        float now = GetCurrentWorldHour();
        float total = Mathf.Max(0.01f, pending.ReadyWorldHour - pending.StartedWorldHour);
        float progress = Mathf.Clamp01((now - pending.StartedWorldHour) / total);
        int percent = Mathf.RoundToInt(progress * 100f);
        return ru ? $"\u0424\u043e\u0440\u043c\u0438\u0440\u0443\u0435\u0442\u0441\u044f: {percent}%" : $"Forming: {percent}%";
    }
}
