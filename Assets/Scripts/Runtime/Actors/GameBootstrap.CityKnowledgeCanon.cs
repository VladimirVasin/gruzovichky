using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int CityKnowledgeCanonMinActiveResidents = 4;
    private const float CityKnowledgeCanonAdoptionShare = 0.35f;

    private readonly List<CityKnowledgeCanonEntry> cityKnowledgeCanon = new();

    private sealed class CityKnowledgeCanonEntry
    {
        public WorkerCognitionKind CognitionKind = WorkerCognitionKind.Fact;
        public WorkerMemoryKind Kind;
        public string ConversationTopicKey = string.Empty;
        public int OtherWorkerId;
        public string Topic = string.Empty;
        public LocationType? BuildingType;
        public int BuildingInstanceId;
        public string BuildingLabel = string.Empty;
        public bool Positive;
        public int KnowledgeIteration;
        public WorkerKnowledgeSourceAttitude SourceAttitude = WorkerKnowledgeSourceAttitude.Neutral;
        public int RumorRootId;
        public string OriginalTopic = string.Empty;
        public string RumorTopic = string.Empty;
        public int RumorDistortionPercent;
        public int RumorConnotationScore;
        public int RumorConnotationConfidence;
        public WorkerKnowledgeOpinionTone OpinionTone = WorkerKnowledgeOpinionTone.Neutral;
        public int OpinionScore;
        public int OpinionConfidence;
        public string OpinionReasonRu = string.Empty;
        public string OpinionReasonEn = string.Empty;
        public int SourceWorkerId;
        public int AdoptionCount;
        public int AdoptionRequired;
        public int CanonizedDay;
        public float CanonizedWorldHour;
    }

    private CityKnowledgeCanonEntry TryCanonizeCityKnowledge(WorkerMemory source, float now)
    {
        if (!IsWorkerMemoryDisplayable(source) ||
            FindCityKnowledgeCanon(source) != null)
        {
            return null;
        }

        int activeResidents = CountActiveCityKnowledgeResidents();
        int requiredAdopters = GetCityKnowledgeCanonRequiredAdopters(activeResidents);
        if (requiredAdopters <= 0)
        {
            return null;
        }

        int adoptionCount = CountCityKnowledgeCanonAdopters(source, now);
        if (adoptionCount < requiredAdopters)
        {
            return null;
        }

        CityKnowledgeCanonEntry entry = CreateCityKnowledgeCanonEntry(source, adoptionCount, requiredAdopters, now);
        if (entry == null)
        {
            return null;
        }

        cityKnowledgeCanon.Insert(0, entry);
        RemovePendingKnowledgeCoveredByCityCanon(entry);
        RecordNoosphereKnowledgeCanonized(entry, now);
        QueueCityKnowledgeCanonReflectionThoughts(entry, now);
        isDriversScreenDirty = true;
        isNoosphereScreenDirty = true;
        SessionDebugLogger.Log(
            "KNOWLEDGE",
            $"City canonized {FormatCityKnowledgeCanonDebugLabel(entry)} after {entry.AdoptionCount}/{entry.AdoptionRequired} residents accepted it.");
        return entry;
    }

    private int CountActiveCityKnowledgeResidents()
    {
        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            if (IsActiveCityKnowledgeResident(driverAgents[i]))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsActiveCityKnowledgeResident(DriverAgent worker)
    {
        return worker != null &&
               !worker.HasDepartedTown &&
               !worker.IsLeavingTown;
    }

    private static int GetCityKnowledgeCanonRequiredAdopters(int activeResidents)
    {
        if (activeResidents < CityKnowledgeCanonMinActiveResidents)
        {
            return 0;
        }

        int shareRequired = Mathf.CeilToInt(activeResidents * CityKnowledgeCanonAdoptionShare);
        return Mathf.Clamp(Mathf.Max(CityKnowledgeCanonMinActiveResidents, shareRequired), 1, activeResidents);
    }

    private int CountCityKnowledgeCanonAdopters(WorkerMemory source, float now)
    {
        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            if (TryFindEquivalentWorkerMemory(driverAgents[i], source, now, out _))
            {
                count++;
            }
        }

        return count;
    }

    private CityKnowledgeCanonEntry CreateCityKnowledgeCanonEntry(WorkerMemory source, int adoptionCount, int requiredAdopters, float now)
    {
        if (!IsWorkerMemoryDisplayable(source))
        {
            return null;
        }

        WorkerMemory selected = source;
        int positiveCount = 0;
        int opinionScoreTotal = 0;
        int opinionConfidenceTotal = 0;
        int opinionCount = 0;
        int rumorDistortionTotal = 0;
        int rumorConnotationTotal = 0;
        int rumorConfidenceTotal = 0;
        int rumorCount = 0;

        for (int i = 0; i < driverAgents.Count; i++)
        {
            if (!TryFindEquivalentWorkerMemory(driverAgents[i], source, now, out WorkerMemory memory))
            {
                continue;
            }

            if (ShouldPreferCityCanonSourceMemory(memory, selected))
            {
                selected = memory;
            }

            if (memory.Positive)
            {
                positiveCount++;
            }

            opinionScoreTotal += memory.OpinionScore;
            opinionConfidenceTotal += memory.OpinionConfidence;
            opinionCount++;

            if (memory.Kind == WorkerMemoryKind.ConversationTopic)
            {
                rumorDistortionTotal += Mathf.Clamp(memory.RumorDistortionPercent, 0, WorkerRumorMaxPercent);
                rumorConnotationTotal += Mathf.Clamp(memory.RumorConnotationScore, -WorkerRumorMaxPercent, WorkerRumorMaxPercent);
                rumorConfidenceTotal += Mathf.Clamp(memory.RumorConnotationConfidence, 0, WorkerRumorMaxPercent);
                rumorCount++;
            }
        }

        int averageOpinionScore = opinionCount > 0
            ? Mathf.RoundToInt(opinionScoreTotal / (float)opinionCount)
            : source.OpinionScore;
        int averageOpinionConfidence = opinionCount > 0
            ? Mathf.RoundToInt(opinionConfidenceTotal / (float)opinionCount)
            : source.OpinionConfidence;

        CityKnowledgeCanonEntry entry = new()
        {
            CognitionKind = GetWorkerMemoryCognitionKind(selected),
            Kind = selected.Kind,
            ConversationTopicKey = selected.ConversationTopicKey ?? string.Empty,
            OtherWorkerId = selected.OtherWorkerId,
            Topic = GetWorkerRumorTopic(selected),
            BuildingType = selected.BuildingType,
            BuildingInstanceId = selected.BuildingInstanceId,
            BuildingLabel = selected.Kind == WorkerMemoryKind.BuildingExistence
                ? GetWorkerKnowledgeBuildingDisplayName(selected, IsRussianLanguage())
                : string.Empty,
            Positive = positiveCount >= Mathf.Max(1, adoptionCount - positiveCount),
            KnowledgeIteration = GetWorkerKnowledgeIteration(selected),
            SourceAttitude = selected.SourceAttitude,
            RumorRootId = selected.RumorRootId,
            OriginalTopic = GetWorkerRumorOriginalTopic(selected),
            RumorTopic = GetWorkerRumorTopic(selected),
            RumorDistortionPercent = rumorCount > 0
                ? Mathf.RoundToInt(rumorDistortionTotal / (float)rumorCount)
                : Mathf.Clamp(selected.RumorDistortionPercent, 0, WorkerRumorMaxPercent),
            RumorConnotationScore = rumorCount > 0
                ? Mathf.RoundToInt(rumorConnotationTotal / (float)rumorCount)
                : Mathf.Clamp(selected.RumorConnotationScore, -WorkerRumorMaxPercent, WorkerRumorMaxPercent),
            RumorConnotationConfidence = rumorCount > 0
                ? Mathf.RoundToInt(rumorConfidenceTotal / (float)rumorCount)
                : Mathf.Clamp(selected.RumorConnotationConfidence, 0, WorkerRumorMaxPercent),
            OpinionScore = Mathf.Clamp(averageOpinionScore, -100, 100),
            OpinionConfidence = Mathf.Clamp(averageOpinionConfidence, 0, 100),
            OpinionReasonRu = selected.OpinionReasonRu ?? string.Empty,
            OpinionReasonEn = selected.OpinionReasonEn ?? string.Empty,
            SourceWorkerId = selected.FormedFromWorkerId,
            AdoptionCount = adoptionCount,
            AdoptionRequired = requiredAdopters,
            CanonizedDay = currentDay,
            CanonizedWorldHour = now
        };
        entry.OpinionTone = GetWorkerKnowledgeOpinionTone(entry.OpinionScore);
        if (entry.Kind == WorkerMemoryKind.ConversationTopic)
        {
            entry.Topic = entry.RumorTopic;
        }

        return entry;
    }

    private static bool ShouldPreferCityCanonSourceMemory(WorkerMemory candidate, WorkerMemory current)
    {
        if (candidate == null)
        {
            return false;
        }

        if (current == null)
        {
            return true;
        }

        int candidateIteration = GetWorkerKnowledgeIteration(candidate);
        int currentIteration = GetWorkerKnowledgeIteration(current);
        if (candidateIteration != currentIteration)
        {
            return candidateIteration > currentIteration;
        }

        return candidate.CreatedWorldHour > current.CreatedWorldHour;
    }

    private bool TryFindEquivalentWorkerMemory(DriverAgent worker, WorkerMemory source, float now, out WorkerMemory result)
    {
        result = null;
        if (!IsActiveCityKnowledgeResident(worker) || source == null)
        {
            return false;
        }

        for (int i = 0; i < worker.Memories.Count; i++)
        {
            WorkerMemory memory = worker.Memories[i];
            if (memory == null ||
                !IsWorkerMemoryDisplayable(memory) ||
                ShouldExpireWorkerMemory(memory, now) ||
                !AreWorkerKnowledgeEquivalent(memory, source))
            {
                continue;
            }

            result = memory;
            return true;
        }

        return false;
    }

    private CityKnowledgeCanonEntry FindCityKnowledgeCanon(WorkerMemory source)
    {
        if (source == null)
        {
            return null;
        }

        for (int i = 0; i < cityKnowledgeCanon.Count; i++)
        {
            CityKnowledgeCanonEntry entry = cityKnowledgeCanon[i];
            if (IsCityKnowledgeCanonEquivalent(entry, source))
            {
                return entry;
            }
        }

        return null;
    }

    private static bool IsCityKnowledgeCanonEquivalent(CityKnowledgeCanonEntry entry, WorkerMemory source)
    {
        if (entry == null || source == null || entry.Kind != source.Kind)
        {
            return false;
        }

        return entry.Kind switch
        {
            WorkerMemoryKind.ConversationTopic =>
                AreWorkerRumorsSameRoot(
                    entry.RumorRootId,
                    string.IsNullOrWhiteSpace(entry.OriginalTopic) ? entry.Topic : entry.OriginalTopic,
                    source.RumorRootId,
                    GetWorkerRumorOriginalTopic(source)),
            WorkerMemoryKind.BuildingExistence =>
                entry.BuildingType == source.BuildingType &&
                entry.BuildingInstanceId == source.BuildingInstanceId,
            _ => false
        };
    }

    private bool HasCityKnowledgeCanonEquivalent(WorkerMemory source)
    {
        return FindCityKnowledgeCanon(source) != null;
    }

    private bool HasAnyCityCanonConversationTopic()
    {
        for (int i = 0; i < cityKnowledgeCanon.Count; i++)
        {
            if (cityKnowledgeCanon[i]?.Kind == WorkerMemoryKind.ConversationTopic)
            {
                return true;
            }
        }

        return false;
    }

    private bool TryFindCityCanonBuildingKnowledge(LocationType type, out WorkerMemory memory)
    {
        memory = null;
        for (int i = 0; i < cityKnowledgeCanon.Count; i++)
        {
            CityKnowledgeCanonEntry entry = cityKnowledgeCanon[i];
            if (entry?.Kind == WorkerMemoryKind.BuildingExistence &&
                entry.BuildingType == type)
            {
                memory = CreateWorkerMemoryFromCityKnowledgeCanon(entry);
                return true;
            }
        }

        return false;
    }

    private int GetCityKnowledgeCanonMemoryCount()
    {
        return cityKnowledgeCanon.Count;
    }

    private bool TryGetCityKnowledgeCanonMemoryAt(int index, out WorkerMemory memory)
    {
        memory = null;
        if (index < 0 || index >= cityKnowledgeCanon.Count)
        {
            return false;
        }

        memory = CreateWorkerMemoryFromCityKnowledgeCanon(cityKnowledgeCanon[index]);
        return IsWorkerMemoryDisplayable(memory);
    }

    private void AddCityKnowledgeCanonHudEntries(List<WorkerKnowledgeHudEntry> result)
    {
        if (result == null)
        {
            return;
        }

        for (int i = 0; i < cityKnowledgeCanon.Count && result.Count < WorkerPersonalMemoryCap; i++)
        {
            WorkerMemory memory = CreateWorkerMemoryFromCityKnowledgeCanon(cityKnowledgeCanon[i]);
            if (IsWorkerMemoryDisplayable(memory))
            {
                result.Add(new WorkerKnowledgeHudEntry(memory));
            }
        }
    }

    private WorkerMemory CreateWorkerMemoryFromCityKnowledgeCanon(CityKnowledgeCanonEntry entry)
    {
        if (entry == null)
        {
            return null;
        }

        WorkerMemory memory = new()
        {
            CognitionKind = entry.CognitionKind,
            Kind = entry.Kind,
            ConversationTopicKey = entry.ConversationTopicKey ?? string.Empty,
            OtherWorkerId = entry.OtherWorkerId,
            Topic = entry.Kind == WorkerMemoryKind.ConversationTopic ? entry.RumorTopic : entry.Topic,
            BuildingType = entry.BuildingType,
            BuildingInstanceId = entry.BuildingInstanceId,
            BuildingLabel = entry.BuildingLabel ?? string.Empty,
            SourceRu = "\u0437\u0430\u043a\u0440\u0435\u043f\u043b\u0435\u043d\u043e \u0432 \u041d\u043e\u043e\u0441\u0444\u0435\u0440\u0435 \u0433\u043e\u0440\u043e\u0434\u0430",
            SourceEn = "canonized in the city Noosphere",
            Positive = entry.Positive,
            KnowledgeIteration = Mathf.Max(1, entry.KnowledgeIteration),
            SourceAttitude = entry.SourceAttitude,
            RumorRootId = entry.RumorRootId,
            OriginalTopic = entry.OriginalTopic ?? string.Empty,
            RumorTopic = entry.RumorTopic ?? string.Empty,
            RumorDistortionPercent = Mathf.Clamp(entry.RumorDistortionPercent, 0, WorkerRumorMaxPercent),
            RumorConnotationScore = Mathf.Clamp(entry.RumorConnotationScore, -WorkerRumorMaxPercent, WorkerRumorMaxPercent),
            RumorConnotationConfidence = Mathf.Clamp(entry.RumorConnotationConfidence, 0, WorkerRumorMaxPercent),
            OpinionTone = entry.OpinionTone,
            OpinionScore = Mathf.Clamp(entry.OpinionScore, -100, 100),
            OpinionConfidence = Mathf.Clamp(entry.OpinionConfidence, 0, 100),
            OpinionReasonRu = entry.OpinionReasonRu ?? string.Empty,
            OpinionReasonEn = entry.OpinionReasonEn ?? string.Empty,
            FormedFromWorkerId = entry.SourceWorkerId,
            IsCityCanonKnowledge = true,
            CityCanonAdoptionCount = entry.AdoptionCount,
            CityCanonAdoptionRequired = entry.AdoptionRequired,
            FormationStartedWorldHour = entry.CanonizedWorldHour,
            FormationCompletedWorldHour = entry.CanonizedWorldHour,
            CreatedDay = entry.CanonizedDay,
            CreatedWorldHour = entry.CanonizedWorldHour,
            ExpiresWorldHour = 0f
        };
        if (memory.Kind == WorkerMemoryKind.ConversationTopic && string.IsNullOrWhiteSpace(memory.ConversationTopicKey))
        {
            memory.ConversationTopicKey = BuildConversationTopicKey(GetWorkerRumorOriginalTopic(memory));
        }

        return memory;
    }

    private void RemovePendingKnowledgeCoveredByCityCanon(CityKnowledgeCanonEntry entry)
    {
        WorkerMemory probe = CreateWorkerMemoryFromCityKnowledgeCanon(entry);
        if (!IsWorkerMemoryDisplayable(probe))
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (worker == null || worker.PendingKnowledge.Count == 0)
            {
                continue;
            }

            for (int j = worker.PendingKnowledge.Count - 1; j >= 0; j--)
            {
                if (ArePendingWorkerKnowledgeEquivalent(worker.PendingKnowledge[j], probe))
                {
                    worker.PendingKnowledge.RemoveAt(j);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            isDriversScreenDirty = true;
        }
    }

    private void QueueCityKnowledgeCanonReflectionThoughts(CityKnowledgeCanonEntry entry, float now)
    {
        WorkerMemory memory = CreateWorkerMemoryFromCityKnowledgeCanon(entry);
        if (!IsWorkerMemoryDisplayable(memory))
        {
            return;
        }

        DriverAgent other = GetDriverAgentById(memory.OtherWorkerId);
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (!IsActiveCityKnowledgeResident(worker))
            {
                continue;
            }

            QueueWorkerKnowledgeReflectionThought(worker, other, memory, now);
        }
    }

    private void RecordNoosphereKnowledgeCanonized(CityKnowledgeCanonEntry entry, float now)
    {
        WorkerMemory memory = CreateWorkerMemoryFromCityKnowledgeCanon(entry);
        if (!IsWorkerMemoryDisplayable(memory))
        {
            return;
        }

        RecordNoosphereKnowledgeEvent(
            NoosphereKnowledgeEventKind.Canonized,
            null,
            GetDriverAgentById(memory.OtherWorkerId),
            memory,
            "\u043f\u0440\u0438\u043d\u044f\u0442\u043e \u0441\u043b\u0438\u0448\u043a\u043e\u043c \u043c\u043d\u043e\u0433\u0438\u043c\u0438 \u0433\u043e\u0440\u043e\u0436\u0430\u043d\u0430\u043c\u0438",
            "accepted by enough residents",
            now);
    }

    private static bool IsPermanentWorkerMemory(WorkerMemory memory)
    {
        return memory?.IsCityCanonKnowledge == true;
    }

    private static int FindWorkerMemoryTrimIndex(DriverAgent worker)
    {
        if (worker == null || worker.Memories.Count == 0)
        {
            return 0;
        }

        for (int i = worker.Memories.Count - 1; i >= 0; i--)
        {
            if (!IsPermanentWorkerMemory(worker.Memories[i]))
            {
                return i;
            }
        }

        return worker.Memories.Count - 1;
    }

    private string FormatCityKnowledgeCanonDebugLabel(CityKnowledgeCanonEntry entry)
    {
        if (entry == null)
        {
            return "knowledge";
        }

        return entry.Kind == WorkerMemoryKind.BuildingExistence
            ? $"building {entry.BuildingType}/{entry.BuildingInstanceId}"
            : $"topic '{entry.Topic}'";
    }
}
