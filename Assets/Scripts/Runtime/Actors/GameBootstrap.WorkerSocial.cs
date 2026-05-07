using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int WorkerSocialMemoryCap = 10;
    private const int WorkerSocialFriendRelationshipThreshold = 50;
    private const int WorkerSocialVisibleFamiliarityThreshold = 10;
    private const int WorkerSocialServiceExposureThreshold = 3;
    private const int WorkerSocialCoworkerExposureThreshold = 2;
    private const int WorkerSocialIdleExposureThreshold = 4;
    private const float WorkerSocialNewIdleAcquaintanceChance = 0.65f;
    private const float WorkerSocialSocialiteIdleAcquaintanceChanceBonus = 0.3f;
    private const int WorkerSocialSocialiteExposureBonus = 1;
    private const int WorkerSocialHudRowCount = 8;
    private const float WorkerSocialRepeatCooldownHours = 1f;
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
        WorkerSocialMemory firstExisting = FindWorkerSocialMemory(first, second.DriverId);
        WorkerSocialMemory secondExisting = FindWorkerSocialMemory(second, first.DriverId);
        if (firstExisting != null &&
            secondExisting != null &&
            firstExisting.LastKind == kind &&
            secondExisting.LastKind == kind &&
            firstExisting.LastLocationType == locationType &&
            secondExisting.LastLocationType == locationType &&
            now - firstExisting.LastInteractionWorldHour < WorkerSocialRepeatCooldownHours &&
            now - secondExisting.LastInteractionWorldHour < WorkerSocialRepeatCooldownHours)
        {
            return;
        }

        int socialiteCount = GetWorkerSocialitePairCount(first, second);
        int previousFamiliarity = Mathf.Max(firstExisting?.Familiarity ?? 0, secondExisting?.Familiarity ?? 0);
        int previousInteractionCount = Mathf.Max(firstExisting?.InteractionCount ?? 0, secondExisting?.InteractionCount ?? 0);
        GetWorkerSocialDeltas(kind, out int familiarityDelta, out int relationshipDelta);
        ApplyWorkerSocialContextDeltaBonus(kind, locationType, ref familiarityDelta, ref relationshipDelta);
        ApplyWorkerSocialMomentumBonus(kind, previousFamiliarity, previousInteractionCount, ref relationshipDelta);
        ApplyWorkerSocialiteSocialDeltaBonus(kind, socialiteCount, ref familiarityDelta, ref relationshipDelta);
        bool createsVisibleAcquaintance = ShouldCreateVisibleWorkerSocialMemory(
            kind,
            locationType,
            firstExisting,
            secondExisting,
            socialiteCount);
        bool firstVisible = RecordWorkerSocialOneWay(first, second, kind, locationType, familiarityDelta, relationshipDelta, now, createsVisibleAcquaintance, socialiteCount);
        bool secondVisible = RecordWorkerSocialOneWay(second, first, kind, locationType, familiarityDelta, relationshipDelta, now, createsVisibleAcquaintance, socialiteCount);
        bool visibleInteraction = firstVisible || secondVisible;
        if (visibleInteraction)
        {
            isDriversScreenDirty = true;
            isSocialGraphScreenDirty = true;
        }

        if (visibleInteraction &&
            (kind == WorkerSocialInteractionKind.IdleConversation ||
             kind == WorkerSocialInteractionKind.ServiceCoPresence ||
             kind == WorkerSocialInteractionKind.CoworkerShift ||
             kind == WorkerSocialInteractionKind.PlayerPromptedConversation ||
             kind == WorkerSocialInteractionKind.PlayerPromptedConversationFailed))
        {
            string context = locationType.HasValue ? locationType.Value.ToString() : kind.ToString();
            WorkerSocialMemory firstUpdated = FindWorkerSocialMemory(first, second.DriverId);
            WorkerSocialMemory secondUpdated = FindWorkerSocialMemory(second, first.DriverId);
            int totalFamiliarity = GetWorkerSocialPairAverageFamiliarity(firstUpdated, secondUpdated);
            int totalRelationship = GetWorkerSocialPairAverageRelationship(firstUpdated, secondUpdated);
            int totalInteractions = Mathf.Max(firstUpdated?.InteractionCount ?? 0, secondUpdated?.InteractionCount ?? 0);
            SessionDebugLogger.Log(
                "SOCIAL",
                $"{first.DriverName} and {second.DriverName} social memory updated: {kind}, context={context}, familiarity+={familiarityDelta}, relationship+={relationshipDelta}, totals familiarity={totalFamiliarity}, relationship={totalRelationship}/{WorkerSocialFriendRelationshipThreshold}, interactions={totalInteractions}.");
        }

        if (visibleInteraction &&
            (kind == WorkerSocialInteractionKind.IdleConversation ||
             kind == WorkerSocialInteractionKind.PlayerPromptedConversation))
        {
            RecordWorkerSocialThought(first, second, "social_talk_good", null, 2);
            RecordWorkerSocialThought(second, first, "social_talk_good", null, 2);
        }
        else if (visibleInteraction && kind == WorkerSocialInteractionKind.ServiceCoPresence && locationType.HasValue)
        {
            RecordWorkerSocialThought(first, second, "social_shared_place", locationType.Value, 1);
            RecordWorkerSocialThought(second, first, "social_shared_place", locationType.Value, 1);
        }
    }

    private void RecordWorkerSocialThought(DriverAgent owner, DriverAgent other, string templateKey, LocationType? place, int opinionDelta)
    {
        List<WorkerThoughtPlaceholder> placeholders = new()
        {
            ThoughtWorker("otherWorker", other)
        };
        if (place.HasValue)
        {
            placeholders.Add(ThoughtBuilding("place", place.Value));
        }

        RecordWorkerThought(
            owner,
            WorkerThoughtKind.Social,
            WorkerThoughtTone.Positive,
            place.HasValue ? 36 : 48,
            templateKey,
            placeholders,
            WorkerThoughtSubjectType.Worker,
            other?.DriverId ?? 0,
            null,
            other?.DriverName,
            opinionDelta,
            $"{templateKey}|{other?.DriverId ?? 0}|{place?.ToString() ?? "talk"}",
            place.HasValue ? 8f : 5f);
    }

    private bool RecordWorkerSocialOneWay(
        DriverAgent owner,
        DriverAgent other,
        WorkerSocialInteractionKind kind,
        LocationType? locationType,
        int familiarityDelta,
        int relationshipDelta,
        float now,
        bool createsVisibleAcquaintance,
        int socialiteCount)
    {
        WorkerSocialMemory memory = FindWorkerSocialMemory(owner, other.DriverId);
        if (memory == null)
        {
            memory = new WorkerSocialMemory { OtherWorkerId = other.DriverId };
            owner.SocialMemories.Add(memory);
        }

        bool wasVisible = IsWorkerSocialMemoryVisible(memory);
        memory.Exposure = Mathf.Clamp(memory.Exposure + GetWorkerSocialExposureDelta(kind, locationType, socialiteCount), 0, 100);
        if (wasVisible || createsVisibleAcquaintance)
        {
            memory.Familiarity = Mathf.Clamp(memory.Familiarity + familiarityDelta, 0, 100);
            if (!wasVisible && memory.Familiarity < WorkerSocialVisibleFamiliarityThreshold)
            {
                memory.Familiarity = WorkerSocialVisibleFamiliarityThreshold;
            }

            memory.Relationship = Mathf.Clamp(memory.Relationship + relationshipDelta, -100, 100);
            memory.InteractionCount++;
            memory.Exposure = 0;
        }

        memory.LastInteractionDay = currentDay;
        memory.LastInteractionWorldHour = now;
        memory.NextFamiliarityDecayWorldHour = now + WorkerSocialFamiliarityDecayGraceHours;
        memory.LastKind = kind;
        memory.LastLocationType = locationType;
        TrimWorkerSocialMemories(owner);
        return IsWorkerSocialMemoryVisible(memory);
    }

    private bool ShouldCreateVisibleWorkerSocialMemory(
        WorkerSocialInteractionKind kind,
        LocationType? locationType,
        WorkerSocialMemory first,
        WorkerSocialMemory second,
        int socialiteCount)
    {
        if (IsWorkerSocialMemoryVisible(first) || IsWorkerSocialMemoryVisible(second))
        {
            return true;
        }

        if (kind == WorkerSocialInteractionKind.ArrivalWave)
        {
            return false;
        }

        int exposureAfter = Mathf.Max(
            (first?.Exposure ?? 0) + GetWorkerSocialExposureDelta(kind, locationType, socialiteCount),
            (second?.Exposure ?? 0) + GetWorkerSocialExposureDelta(kind, locationType, socialiteCount));

        return kind switch
        {
            WorkerSocialInteractionKind.IdleConversation =>
                Random.value <= GetWorkerSocialIdleAcquaintanceChance(socialiteCount) ||
                exposureAfter >= WorkerSocialIdleExposureThreshold,
            WorkerSocialInteractionKind.CoworkerShift => exposureAfter >= WorkerSocialCoworkerExposureThreshold,
            WorkerSocialInteractionKind.ServiceCoPresence => exposureAfter >= WorkerSocialServiceExposureThreshold,
            _ => true
        };
    }

    private static bool IsWorkerSocialMemoryVisible(WorkerSocialMemory memory)
    {
        return memory != null && memory.Familiarity >= WorkerSocialVisibleFamiliarityThreshold;
    }

    private static int GetWorkerSocialExposureDelta(WorkerSocialInteractionKind kind, LocationType? locationType, int socialiteCount)
    {
        int baseDelta = kind switch
        {
            WorkerSocialInteractionKind.IdleConversation => 3,
            WorkerSocialInteractionKind.CoworkerShift => 2,
            WorkerSocialInteractionKind.ServiceCoPresence => locationType == LocationType.Bar ? 2 : 1,
            WorkerSocialInteractionKind.ArrivalWave => 1,
            _ => 0
        };
        return baseDelta + Mathf.Max(0, socialiteCount) * WorkerSocialSocialiteExposureBonus;
    }

    private static float GetWorkerSocialIdleAcquaintanceChance(int socialiteCount)
    {
        return Mathf.Clamp01(WorkerSocialNewIdleAcquaintanceChance +
                             Mathf.Max(0, socialiteCount) * WorkerSocialSocialiteIdleAcquaintanceChanceBonus);
    }

    private static int GetWorkerSocialitePairCount(DriverAgent first, DriverAgent second)
    {
        int count = 0;
        if (HasWorkerPerk(first, WorkerPerkKind.Socialite))
        {
            count++;
        }

        if (HasWorkerPerk(second, WorkerPerkKind.Socialite))
        {
            count++;
        }

        return count;
    }

    private static void ApplyWorkerSocialiteSocialDeltaBonus(
        WorkerSocialInteractionKind kind,
        int socialiteCount,
        ref int familiarityDelta,
        ref int relationshipDelta)
    {
        if (socialiteCount <= 0 || kind == WorkerSocialInteractionKind.ArrivalWave)
        {
            return;
        }

        float familiarityMultiplier = 1f + socialiteCount * 0.45f;
        familiarityDelta = Mathf.Max(
            familiarityDelta + socialiteCount,
            Mathf.RoundToInt(familiarityDelta * familiarityMultiplier));
        if (relationshipDelta > 0)
        {
            float relationshipMultiplier = 1f + socialiteCount * 0.45f;
            relationshipDelta = Mathf.Max(
                relationshipDelta + socialiteCount * 2,
                Mathf.RoundToInt(relationshipDelta * relationshipMultiplier));
        }
    }

    private static void ApplyWorkerSocialContextDeltaBonus(
        WorkerSocialInteractionKind kind,
        LocationType? locationType,
        ref int familiarityDelta,
        ref int relationshipDelta)
    {
        if (kind == WorkerSocialInteractionKind.ServiceCoPresence &&
            locationType == LocationType.Bar)
        {
            familiarityDelta += 2;
            relationshipDelta += 2;
        }
    }

    private static void ApplyWorkerSocialMomentumBonus(
        WorkerSocialInteractionKind kind,
        int previousFamiliarity,
        int previousInteractionCount,
        ref int relationshipDelta)
    {
        if (relationshipDelta <= 0 ||
            kind == WorkerSocialInteractionKind.ArrivalWave ||
            kind == WorkerSocialInteractionKind.FamilyFormation)
        {
            return;
        }

        if (previousFamiliarity >= 60)
        {
            relationshipDelta += 3;
        }
        else if (previousFamiliarity >= 30)
        {
            relationshipDelta += 2;
        }
        else if (previousFamiliarity >= WorkerSocialVisibleFamiliarityThreshold)
        {
            relationshipDelta += 1;
        }

        if (previousInteractionCount >= 6)
        {
            relationshipDelta += 2;
        }
        else if (previousInteractionCount >= 3)
        {
            relationshipDelta += 1;
        }

        if (kind == WorkerSocialInteractionKind.IdleConversation && previousInteractionCount > 0)
        {
            relationshipDelta += 1;
        }
    }

    private static int GetWorkerSocialPairAverageFamiliarity(WorkerSocialMemory first, WorkerSocialMemory second)
    {
        return GetWorkerSocialPairAverageValue(first, second, memory => memory.Familiarity);
    }

    private static int GetWorkerSocialPairAverageRelationship(WorkerSocialMemory first, WorkerSocialMemory second)
    {
        return GetWorkerSocialPairAverageValue(first, second, memory => memory.Relationship);
    }

    private static int GetWorkerSocialPairAverageValue(
        WorkerSocialMemory first,
        WorkerSocialMemory second,
        System.Func<WorkerSocialMemory, int> selector)
    {
        int count = 0;
        int total = 0;
        if (IsWorkerSocialMemoryVisible(first))
        {
            total += selector(first);
            count++;
        }

        if (IsWorkerSocialMemoryVisible(second))
        {
            total += selector(second);
            count++;
        }

        return count > 0 ? Mathf.RoundToInt(total / (float)count) : 0;
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
                    : memory.Familiarity + Mathf.Abs(memory.Relationship) / 2 + memory.InteractionCount * 2 + memory.Exposure;
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
                familiarityDelta = 16;
                relationshipDelta = 7;
                break;
            case WorkerSocialInteractionKind.ServiceCoPresence:
                familiarityDelta = 6;
                relationshipDelta = 1;
                break;
            case WorkerSocialInteractionKind.CoworkerShift:
                familiarityDelta = 10;
                relationshipDelta = 5;
                break;
            case WorkerSocialInteractionKind.ArrivalWave:
                familiarityDelta = 0;
                relationshipDelta = 0;
                break;
            case WorkerSocialInteractionKind.FamilyFormation:
                familiarityDelta = 0;
                relationshipDelta = 0;
                break;
            case WorkerSocialInteractionKind.PlayerPromptedConversation:
                familiarityDelta = CitySocialIntroSuccessFamiliarityDelta;
                relationshipDelta = CitySocialIntroSuccessRelationshipDelta;
                break;
            case WorkerSocialInteractionKind.PlayerPromptedConversationFailed:
                familiarityDelta = CitySocialIntroFailureFamiliarityDelta;
                relationshipDelta = CitySocialIntroFailureRelationshipDelta;
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

            for (int j = worker.SocialMemories.Count - 1; j >= 0; j--)
            {
                WorkerSocialMemory memory = worker.SocialMemories[j];
                if (memory == null)
                {
                    worker.SocialMemories.RemoveAt(j);
                    changed = true;
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
                int oldExposure = memory.Exposure;
                int decayAmount = 0;
                for (int step = 0; step < decaySteps; step++)
                {
                    float stepWorldHour = memory.NextFamiliarityDecayWorldHour + step * WorkerSocialFamiliarityDecayStepHours;
                    decayAmount += GetWorkerSocialFamiliarityDecayAmount(memory, stepWorldHour);
                }

                int oldFamiliarity = memory.Familiarity;
                if (memory.Familiarity < WorkerSocialVisibleFamiliarityThreshold)
                {
                    memory.Exposure = Mathf.Max(0, memory.Exposure - decaySteps);
                    memory.Relationship = 0;
                }
                else
                {
                    memory.Exposure = 0;
                }

                memory.Familiarity = Mathf.Max(0, memory.Familiarity - decayAmount);
                if (memory.Familiarity < WorkerSocialVisibleFamiliarityThreshold)
                {
                    memory.Relationship = 0;
                }

                memory.NextFamiliarityDecayWorldHour += decaySteps * WorkerSocialFamiliarityDecayStepHours;
                if (memory.Familiarity <= 0 && memory.Exposure <= 0)
                {
                    worker.SocialMemories.RemoveAt(j);
                    changed = true;
                    continue;
                }

                changed |= memory.Familiarity != oldFamiliarity || memory.Exposure != oldExposure;
            }
        }

        if (changed)
        {
            isDriversScreenDirty = true;
            isSocialGraphScreenDirty = true;
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
            if (!IsWorkerSocialMemoryVisible(memory))
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
        UpdateWorkerSocialGraphUi(worker, ru);
    }

    private static string GetWorkerSocialRelationshipLabel(int relationship, bool ru)
    {
        if (relationship >= WorkerSocialFriendRelationshipThreshold) return ru ? "\u0414\u0440\u0443\u0433" : "Friend";
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
            WorkerSocialInteractionKind.PlayerPromptedConversation => ru ? "\u0422\u0435\u043c\u0430 \u043e\u0442 \u0438\u0433\u0440\u043e\u043a\u0430" : "Player topic",
            WorkerSocialInteractionKind.PlayerPromptedConversationFailed => ru ? "\u041d\u0435\u0443\u0434\u0430\u0447\u043d\u0430\u044f \u0442\u0435\u043c\u0430" : "Awkward topic",
            _ => ru ? "\u041a\u043e\u043d\u0442\u0430\u043a\u0442" : "Contact"
        };

        return ru
            ? $"{context}, \u0434\u0435\u043d\u044c {memory.LastInteractionDay}"
            : $"{context}, day {memory.LastInteractionDay}";
    }
}
