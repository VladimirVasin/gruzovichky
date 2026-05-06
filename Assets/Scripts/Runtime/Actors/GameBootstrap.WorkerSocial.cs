using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int WorkerSocialMemoryCap = 16;
    private const int WorkerSocialHudRowCount = 8;
    private const float WorkerSocialRepeatCooldownHours = 1.5f;
    private const float WorkerSocialDecayCheckIntervalHours = 1f;
    private const float WorkerSocialFamiliarityDecayGraceHours = 24f;
    private const float WorkerSocialFamiliarityDecayStepHours = 12f;
    private const int WorkerSocialFamiliarityDecayMaxPerStep = 8;

    private float lastWorkerSocialDecayCheckWorldHour = -1f;

    private void RecordWorkerSocialInteraction(
        DriverAgent first,
        DriverAgent second,
        WorkerSocialInteractionKind kind,
        LocationType? locationType = null)
    {
        if (first == null ||
            second == null ||
            first == second ||
            first.DriverId <= 0 ||
            second.DriverId <= 0 ||
            first.HasDepartedTown ||
            second.HasDepartedTown)
        {
            return;
        }

        float now = GetCurrentWorldHour();
        WorkerSocialMemory existing = FindWorkerSocialMemory(first, second.DriverId);
        if (existing != null &&
            existing.LastKind == kind &&
            existing.LastLocationType == locationType &&
            now - existing.LastInteractionWorldHour < WorkerSocialRepeatCooldownHours)
        {
            return;
        }

        GetWorkerSocialDeltas(kind, out int familiarityDelta, out int relationshipDelta);
        RecordWorkerSocialOneWay(first, second, kind, locationType, familiarityDelta, relationshipDelta, now);
        RecordWorkerSocialOneWay(second, first, kind, locationType, familiarityDelta, relationshipDelta, now);
        isDriversScreenDirty = true;

        if (kind == WorkerSocialInteractionKind.IdleConversation ||
            kind == WorkerSocialInteractionKind.ServiceCoPresence)
        {
            string context = locationType.HasValue ? locationType.Value.ToString() : kind.ToString();
            SessionDebugLogger.Log(
                "SOCIAL",
                $"{first.DriverName} and {second.DriverName} social memory updated: {kind}, context={context}, familiarity+={familiarityDelta}, relationship+={relationshipDelta}.");
        }
    }

    private void RecordWorkerSocialOneWay(
        DriverAgent owner,
        DriverAgent other,
        WorkerSocialInteractionKind kind,
        LocationType? locationType,
        int familiarityDelta,
        int relationshipDelta,
        float now)
    {
        WorkerSocialMemory memory = FindWorkerSocialMemory(owner, other.DriverId);
        if (memory == null)
        {
            memory = new WorkerSocialMemory { OtherWorkerId = other.DriverId };
            owner.SocialMemories.Add(memory);
        }

        memory.Familiarity = Mathf.Clamp(memory.Familiarity + familiarityDelta, 0, 100);
        memory.Relationship = Mathf.Clamp(memory.Relationship + relationshipDelta, -100, 100);
        memory.InteractionCount++;
        memory.LastInteractionDay = currentDay;
        memory.LastInteractionWorldHour = now;
        memory.NextFamiliarityDecayWorldHour = now + WorkerSocialFamiliarityDecayGraceHours;
        memory.LastKind = kind;
        memory.LastLocationType = locationType;
        TrimWorkerSocialMemories(owner);
    }

    private static WorkerSocialMemory FindWorkerSocialMemory(DriverAgent owner, int otherWorkerId)
    {
        if (owner == null || otherWorkerId <= 0)
        {
            return null;
        }

        for (int i = 0; i < owner.SocialMemories.Count; i++)
        {
            WorkerSocialMemory memory = owner.SocialMemories[i];
            if (memory != null && memory.OtherWorkerId == otherWorkerId)
            {
                return memory;
            }
        }

        return null;
    }

    private void TrimWorkerSocialMemories(DriverAgent worker)
    {
        if (worker == null)
        {
            return;
        }

        while (worker.SocialMemories.Count > WorkerSocialMemoryCap)
        {
            int removeIndex = 0;
            int removeScore = int.MaxValue;
            for (int i = 0; i < worker.SocialMemories.Count; i++)
            {
                WorkerSocialMemory memory = worker.SocialMemories[i];
                int score = memory == null
                    ? int.MinValue
                    : memory.Familiarity + Mathf.Abs(memory.Relationship) / 2 + memory.InteractionCount;
                if (score < removeScore)
                {
                    removeScore = score;
                    removeIndex = i;
                }
            }

            worker.SocialMemories.RemoveAt(removeIndex);
        }
    }

    private static void GetWorkerSocialDeltas(WorkerSocialInteractionKind kind, out int familiarityDelta, out int relationshipDelta)
    {
        switch (kind)
        {
            case WorkerSocialInteractionKind.IdleConversation:
                familiarityDelta = 8;
                relationshipDelta = 2;
                break;
            case WorkerSocialInteractionKind.ServiceCoPresence:
                familiarityDelta = 3;
                relationshipDelta = 1;
                break;
            case WorkerSocialInteractionKind.CoworkerShift:
                familiarityDelta = 4;
                relationshipDelta = 1;
                break;
            case WorkerSocialInteractionKind.ArrivalWave:
                familiarityDelta = 2;
                relationshipDelta = 0;
                break;
            case WorkerSocialInteractionKind.FamilyFormation:
                familiarityDelta = 0;
                relationshipDelta = 0;
                break;
            default:
                familiarityDelta = 1;
                relationshipDelta = 0;
                break;
        }
    }

    private void RecordWorkerServiceCoPresence(DriverAgent worker, LocationType locationType)
    {
        if (worker == null)
        {
            return;
        }

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent other = driverAgents[i];
            if (other == null ||
                other == worker ||
                other.HasDepartedTown ||
                !other.IsInsideBuilding ||
                other.InsideBuildingType != locationType)
            {
                continue;
            }

            RecordWorkerSocialInteraction(worker, other, WorkerSocialInteractionKind.ServiceCoPresence, locationType);
        }
    }

    private void RecordWorkerCoworkerShiftSocial(DriverAgent worker, LocationData building)
    {
        if (worker == null || building == null)
        {
            return;
        }

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent other = driverAgents[i];
            if (other == null ||
                other == worker ||
                other.HasDepartedTown ||
                !other.IsInsideBuilding ||
                other.InsideBuildingType != building.Type ||
                other.InsideBuildingInstanceId != building.InstanceId)
            {
                continue;
            }

            RecordWorkerSocialInteraction(worker, other, WorkerSocialInteractionKind.CoworkerShift, building.Type);
        }
    }

    private void RecordWorkerArrivalWaveSocial(IReadOnlyList<DriverAgent> workers)
    {
        if (workers == null || workers.Count < 2)
        {
            return;
        }

        for (int i = 0; i < workers.Count; i++)
        {
            for (int j = i + 1; j < workers.Count; j++)
            {
                RecordWorkerSocialInteraction(workers[i], workers[j], WorkerSocialInteractionKind.ArrivalWave);
            }
        }
    }

    private void UpdateWorkerSocialDecay()
    {
        float now = GetCurrentWorldHour();
        if (lastWorkerSocialDecayCheckWorldHour >= 0f &&
            now - lastWorkerSocialDecayCheckWorldHour < WorkerSocialDecayCheckIntervalHours)
        {
            return;
        }

        lastWorkerSocialDecayCheckWorldHour = now;
        bool changed = false;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (worker == null || worker.SocialMemories.Count == 0)
            {
                continue;
            }

            for (int j = 0; j < worker.SocialMemories.Count; j++)
            {
                WorkerSocialMemory memory = worker.SocialMemories[j];
                if (memory == null)
                {
                    continue;
                }

                if (memory.Familiarity <= 0)
                {
                    if (memory.Relationship != 0)
                    {
                        memory.Relationship = 0;
                        changed = true;
                    }

                    continue;
                }

                if (memory.NextFamiliarityDecayWorldHour <= 0f)
                {
                    memory.NextFamiliarityDecayWorldHour = memory.LastInteractionWorldHour + WorkerSocialFamiliarityDecayGraceHours;
                }

                if (now < memory.NextFamiliarityDecayWorldHour)
                {
                    continue;
                }

                int decaySteps = Mathf.FloorToInt((now - memory.NextFamiliarityDecayWorldHour) / WorkerSocialFamiliarityDecayStepHours) + 1;
                int decayAmount = 0;
                for (int step = 0; step < decaySteps; step++)
                {
                    float stepWorldHour = memory.NextFamiliarityDecayWorldHour + step * WorkerSocialFamiliarityDecayStepHours;
                    decayAmount += GetWorkerSocialFamiliarityDecayAmount(memory, stepWorldHour);
                }

                int oldFamiliarity = memory.Familiarity;
                memory.Familiarity = Mathf.Max(0, memory.Familiarity - decayAmount);
                if (memory.Familiarity == 0)
                {
                    memory.Relationship = 0;
                }

                memory.NextFamiliarityDecayWorldHour += decaySteps * WorkerSocialFamiliarityDecayStepHours;
                changed |= memory.Familiarity != oldFamiliarity;
            }
        }

        if (changed)
        {
            isDriversScreenDirty = true;
        }
    }

    private static int GetWorkerSocialFamiliarityDecayAmount(WorkerSocialMemory memory, float stepWorldHour)
    {
        if (memory == null)
        {
            return 0;
        }

        int daysWithoutContact = Mathf.Max(1, Mathf.FloorToInt((stepWorldHour - memory.LastInteractionWorldHour) / 24f));
        return Mathf.Clamp(daysWithoutContact, 1, WorkerSocialFamiliarityDecayMaxPerStep);
    }

    private List<WorkerSocialMemory> GetWorkerSocialMemoriesSorted(DriverAgent worker)
    {
        List<WorkerSocialMemory> result = new();
        if (worker == null)
        {
            return result;
        }

        for (int i = 0; i < worker.SocialMemories.Count; i++)
        {
            WorkerSocialMemory memory = worker.SocialMemories[i];
            if (memory == null || memory.Familiarity <= 0)
            {
                continue;
            }

            DriverAgent other = memory != null ? GetDriverAgentById(memory.OtherWorkerId) : null;
            if (other == null || other.HasDepartedTown || other.IsLeavingTown)
            {
                continue;
            }

            result.Add(memory);
        }

        result.Sort((a, b) =>
        {
            int familiarity = b.Familiarity.CompareTo(a.Familiarity);
            if (familiarity != 0) return familiarity;
            int relationship = Mathf.Abs(b.Relationship).CompareTo(Mathf.Abs(a.Relationship));
            if (relationship != 0) return relationship;
            return b.LastInteractionWorldHour.CompareTo(a.LastInteractionWorldHour);
        });
        return result;
    }

    private void UpdateWorkerSocialUi(DriverAgent worker, bool ru)
    {
        if (driversScreenUi == null)
        {
            return;
        }

        if (driversScreenUi.DetailSocialTitleText != null)
        {
            driversScreenUi.DetailSocialTitleText.text = ru ? "\u0421\u043e\u0446\u0438\u0430\u043b\u044c\u043d\u044b\u0435 \u0441\u0432\u044f\u0437\u0438" : "Social Links";
        }

        if (driversScreenUi.DetailSocialNameHeaderText != null)
        {
            driversScreenUi.DetailSocialNameHeaderText.text = ru ? "\u0418\u043c\u044f" : "Name";
        }

        if (driversScreenUi.DetailSocialRelationHeaderText != null)
        {
            driversScreenUi.DetailSocialRelationHeaderText.text = ru ? "\u0421\u0432\u044f\u0437\u044c" : "Relation";
        }

        if (driversScreenUi.DetailSocialFamiliarityHeaderText != null)
        {
            driversScreenUi.DetailSocialFamiliarityHeaderText.text = ru ? "\u0417\u043d\u0430\u043a." : "Know";
        }

        if (driversScreenUi.DetailSocialContextHeaderText != null)
        {
            driversScreenUi.DetailSocialContextHeaderText.text = ru ? "\u0413\u0434\u0435" : "Last";
        }

        List<WorkerSocialMemory> memories = GetWorkerSocialMemoriesSorted(worker);
        bool hasRows = memories.Count > 0;
        if (driversScreenUi.DetailSocialEmptyText != null)
        {
            driversScreenUi.DetailSocialEmptyText.gameObject.SetActive(!hasRows);
            driversScreenUi.DetailSocialEmptyText.text = ru
                ? "\u041f\u043e\u043a\u0430 \u043d\u0438 \u0441 \u043a\u0435\u043c \u043d\u0435 \u0437\u043d\u0430\u043a\u043e\u043c"
                : "No social memories yet";
        }

        for (int i = 0; i < driversScreenUi.DetailSocialRows.Count; i++)
        {
            WorkerSocialRowUi row = driversScreenUi.DetailSocialRows[i];
            bool active = i < memories.Count;
            row.Root.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            WorkerSocialMemory memory = memories[i];
            DriverAgent other = GetDriverAgentById(memory.OtherWorkerId);
            row.NameText.text = other != null ? other.DriverName : (ru ? "\u041d\u0435\u0442 \u0434\u0430\u043d\u043d\u044b\u0445" : "Unknown");
            row.RelationText.text = GetWorkerSocialRelationshipLabel(memory.Relationship, ru);
            row.RelationText.color = GetWorkerSocialRelationshipColor(memory.Relationship);
            row.FamiliarityText.text = memory.Familiarity.ToString();
            row.ContextText.text = FormatWorkerSocialContext(memory, ru);
        }
    }

    private static string GetWorkerSocialRelationshipLabel(int relationship, bool ru)
    {
        if (relationship >= 50) return ru ? "\u0414\u0440\u0443\u0433" : "Friend";
        if (relationship >= 20) return ru ? "\u041f\u0440\u0438\u044f\u0442\u0435\u043b\u044c" : "Pal";
        if (relationship <= -50) return ru ? "\u041a\u043e\u043d\u0444\u043b\u0438\u043a\u0442" : "Conflict";
        if (relationship <= -20) return ru ? "\u041d\u0430\u043f\u0440\u044f\u0436\u0435\u043d\u0438\u0435" : "Tense";
        return ru ? "\u041d\u0435\u0439\u0442\u0440\u0430\u043b\u044c\u043d\u043e" : "Neutral";
    }

    private static Color GetWorkerSocialRelationshipColor(int relationship)
    {
        if (relationship >= 20) return new Color(0.55f, 0.86f, 0.58f, 1f);
        if (relationship <= -20) return new Color(0.96f, 0.58f, 0.45f, 1f);
        return new Color(0.78f, 0.84f, 0.92f, 1f);
    }

    private string FormatWorkerSocialContext(WorkerSocialMemory memory, bool ru)
    {
        string context = memory.LastKind switch
        {
            WorkerSocialInteractionKind.IdleConversation => ru ? "\u0420\u0430\u0437\u0433\u043e\u0432\u043e\u0440" : "Talk",
            WorkerSocialInteractionKind.ServiceCoPresence => memory.LastLocationType.HasValue
                ? GetSelectedLocationDisplayName(memory.LastLocationType.Value)
                : (ru ? "\u0421\u0435\u0440\u0432\u0438\u0441" : "Service"),
            WorkerSocialInteractionKind.CoworkerShift => ru ? "\u0421\u043c\u0435\u043d\u0430" : "Shift",
            WorkerSocialInteractionKind.ArrivalWave => ru ? "\u041e\u0434\u0438\u043d \u0430\u0432\u0442\u043e\u0431\u0443\u0441" : "Same bus",
            WorkerSocialInteractionKind.FamilyFormation => ru ? "\u0421\u0435\u043c\u044c\u044f" : "Family",
            _ => ru ? "\u041a\u043e\u043d\u0442\u0430\u043a\u0442" : "Contact"
        };

        return ru
            ? $"{context}, \u0434\u0435\u043d\u044c {memory.LastInteractionDay}"
            : $"{context}, day {memory.LastInteractionDay}";
    }
}
