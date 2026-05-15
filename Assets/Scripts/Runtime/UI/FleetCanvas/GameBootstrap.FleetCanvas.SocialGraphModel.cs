using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int SocialGraphFocusedRelationLimit = 10;

    private enum RelationCategory
    {
        Family,
        Friend,
        Work,
        Neighbor,
        Conflict,
        Acquaintance
    }

    private enum SocialGraphFilterMode
    {
        Important,
        Conflict,
        Work
    }

    private sealed class SocialRelationViewModel
    {
        public int FocusWorkerId;
        public DriverAgent FocusWorker;
        public int OtherWorkerId;
        public DriverAgent OtherWorker;
        public string OtherWorkerName;
        public int Familiarity;
        public int Relationship;
        public int InteractionCount;
        public float Strength;
        public float Recency;
        public float EmotionalIntensity;
        public float NarrativeRelevance;
        public float Importance;
        public RelationCategory Category;
        public WorkerSocialInteractionKind LastKind;
        public LocationType? LastLocationType;
        public int LastInteractionDay;
        public float LastInteractionWorldHour;
        public long EdgeKey;
    }

    private sealed class SocialGraphStats
    {
        public int TotalKnownLinks;
        public int ShownLinks;
        public int HiddenWeakLinks;
        public int FilteredOutLinks;
        public int PositiveLinks;
        public int NeutralLinks;
        public int TenseLinks;
        public SocialRelationViewModel StrongestRelation;
        public DriverAgent SocialLeader;
        public float SocialLeaderScore;
        public int SocialLeaderLinkCount;
    }

    private List<SocialRelationViewModel> BuildSocialGraphVisibleRelations(
        DriverAgent selected,
        SocialGraphFilterMode filter,
        out SocialGraphStats stats)
    {
        List<SocialRelationViewModel> allRelations = BuildSocialGraphAllRelations(selected);
        stats = BuildSocialGraphStats(allRelations);

        List<SocialRelationViewModel> filteredRelations = new();
        for (int i = 0; i < allRelations.Count; i++)
        {
            SocialRelationViewModel relation = allRelations[i];
            if (DoesSocialGraphRelationPassFilter(relation, filter))
            {
                filteredRelations.Add(relation);
            }
        }

        filteredRelations.Sort(CompareSocialGraphRelationsByImportance);
        stats.FilteredOutLinks = Mathf.Max(0, allRelations.Count - filteredRelations.Count);

        if (filteredRelations.Count > SocialGraphFocusedRelationLimit)
        {
            stats.HiddenWeakLinks = filteredRelations.Count - SocialGraphFocusedRelationLimit;
            filteredRelations.RemoveRange(SocialGraphFocusedRelationLimit, filteredRelations.Count - SocialGraphFocusedRelationLimit);
        }
        else
        {
            stats.HiddenWeakLinks = 0;
        }

        stats.ShownLinks = filteredRelations.Count;
        return filteredRelations;
    }

    private List<SocialRelationViewModel> BuildSocialGraphCityRelations(
        SocialGraphFilterMode filter,
        out SocialGraphStats stats)
    {
        Dictionary<long, SocialRelationViewModel> relationByKey = new();
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (!IsSocialGraphWorkerVisible(worker))
            {
                continue;
            }

            for (int j = 0; j < worker.SocialMemories.Count; j++)
            {
                WorkerSocialMemory memory = worker.SocialMemories[j];
                if (!IsWorkerSocialMemoryVisible(memory) || !IsSocialGraphWorkerIdVisible(memory.OtherWorkerId))
                {
                    continue;
                }

                DriverAgent other = GetDriverAgentById(memory.OtherWorkerId);
                if (other == null || other.DriverId == worker.DriverId)
                {
                    continue;
                }

                SocialRelationViewModel relation = CreateSocialRelationViewModel(worker, other, memory);
                if (!relationByKey.TryGetValue(relation.EdgeKey, out SocialRelationViewModel existing) ||
                    relation.Importance > existing.Importance ||
                    (Mathf.Approximately(relation.Importance, existing.Importance) &&
                     relation.LastInteractionWorldHour > existing.LastInteractionWorldHour))
                {
                    relationByKey[relation.EdgeKey] = relation;
                }
            }
        }

        List<SocialRelationViewModel> allRelations = new(relationByKey.Values);
        allRelations.Sort(CompareSocialGraphRelationsByImportance);
        stats = BuildSocialGraphStats(allRelations);
        UpdateCitySocialLeadershipFromRelations(allRelations, stats);

        List<SocialRelationViewModel> filteredRelations = new();
        for (int i = 0; i < allRelations.Count; i++)
        {
            SocialRelationViewModel relation = allRelations[i];
            if (DoesSocialGraphRelationPassFilter(relation, filter))
            {
                filteredRelations.Add(relation);
            }
        }

        stats.FilteredOutLinks = Mathf.Max(0, allRelations.Count - filteredRelations.Count);
        stats.HiddenWeakLinks = 0;
        stats.ShownLinks = filteredRelations.Count;
        return filteredRelations;
    }

    private List<SocialRelationViewModel> BuildSocialGraphAllRelations(DriverAgent selected)
    {
        List<SocialRelationViewModel> relations = new();
        if (selected == null)
        {
            return relations;
        }

        for (int i = 0; i < selected.SocialMemories.Count; i++)
        {
            WorkerSocialMemory memory = selected.SocialMemories[i];
            if (!IsWorkerSocialMemoryVisible(memory) || !IsSocialGraphWorkerIdVisible(memory.OtherWorkerId))
            {
                continue;
            }

            DriverAgent other = GetDriverAgentById(memory.OtherWorkerId);
            if (other == null)
            {
                continue;
            }

            SocialRelationViewModel relation = CreateSocialRelationViewModel(selected, other, memory);
            relations.Add(relation);
        }

        relations.Sort(CompareSocialGraphRelationsByImportance);
        return relations;
    }

    private SocialRelationViewModel CreateSocialRelationViewModel(DriverAgent selected, DriverAgent other, WorkerSocialMemory memory)
    {
        float strength = Mathf.Clamp01(memory.Familiarity / 100f);
        float recency = CalculateSocialGraphRecency(memory);
        float emotionalIntensity = Mathf.Clamp01(Mathf.Abs(memory.Relationship) / 100f);
        float narrativeRelevance = CalculateSocialGraphNarrativeRelevance(selected, other, memory);
        RelationCategory category = ClassifySocialGraphRelation(selected, other, memory);
        float importance = Mathf.Clamp01(
            strength * 0.45f +
            recency * 0.25f +
            emotionalIntensity * 0.20f +
            narrativeRelevance * 0.10f);

        return new SocialRelationViewModel
        {
            FocusWorkerId = selected.DriverId,
            FocusWorker = selected,
            OtherWorkerId = other.DriverId,
            OtherWorker = other,
            OtherWorkerName = other.DriverName,
            Familiarity = memory.Familiarity,
            Relationship = memory.Relationship,
            InteractionCount = memory.InteractionCount,
            Strength = strength,
            Recency = recency,
            EmotionalIntensity = emotionalIntensity,
            NarrativeRelevance = narrativeRelevance,
            Importance = importance,
            Category = category,
            LastKind = memory.LastKind,
            LastLocationType = memory.LastLocationType,
            LastInteractionDay = memory.LastInteractionDay,
            LastInteractionWorldHour = memory.LastInteractionWorldHour,
            EdgeKey = MakeSocialGraphEdgeKey(selected.DriverId, other.DriverId)
        };
    }

    private SocialGraphStats BuildSocialGraphStats(List<SocialRelationViewModel> allRelations)
    {
        SocialGraphStats stats = new() { TotalKnownLinks = allRelations.Count };
        for (int i = 0; i < allRelations.Count; i++)
        {
            SocialRelationViewModel relation = allRelations[i];
            if (relation.Relationship >= 20)
            {
                stats.PositiveLinks++;
            }
            else if (relation.Relationship <= -20)
            {
                stats.TenseLinks++;
            }
            else
            {
                stats.NeutralLinks++;
            }

            if (stats.StrongestRelation == null ||
                relation.Strength > stats.StrongestRelation.Strength ||
                (Mathf.Approximately(relation.Strength, stats.StrongestRelation.Strength) &&
                 relation.Importance > stats.StrongestRelation.Importance))
            {
                stats.StrongestRelation = relation;
            }
        }

        return stats;
    }

    private static int CompareSocialGraphRelationsByImportance(SocialRelationViewModel a, SocialRelationViewModel b)
    {
        int importance = b.Importance.CompareTo(a.Importance);
        if (importance != 0) return importance;
        int strength = b.Familiarity.CompareTo(a.Familiarity);
        if (strength != 0) return strength;
        int emotion = Mathf.Abs(b.Relationship).CompareTo(Mathf.Abs(a.Relationship));
        if (emotion != 0) return emotion;
        return b.LastInteractionWorldHour.CompareTo(a.LastInteractionWorldHour);
    }

    private bool DoesSocialGraphRelationPassFilter(SocialRelationViewModel relation, SocialGraphFilterMode filter)
    {
        if (relation == null)
        {
            return false;
        }

        return filter switch
        {
            SocialGraphFilterMode.Conflict => relation.Category == RelationCategory.Conflict || relation.Relationship <= -20,
            SocialGraphFilterMode.Work => relation.Category == RelationCategory.Work,
            _ => true
        };
    }

    private float CalculateSocialGraphRecency(WorkerSocialMemory memory)
    {
        if (memory == null || memory.LastInteractionWorldHour <= 0f)
        {
            return 0.35f;
        }

        float ageHours = Mathf.Max(0f, GetCurrentWorldHour() - memory.LastInteractionWorldHour);
        return Mathf.Clamp01(1f - ageHours / 96f);
    }

    private float CalculateSocialGraphNarrativeRelevance(DriverAgent selected, DriverAgent other, WorkerSocialMemory memory)
    {
        if (IsSocialGraphFamilyRelation(selected, other, memory))
        {
            return 1f;
        }

        float value = memory.LastKind switch
        {
            WorkerSocialInteractionKind.PlayerPromptedConversation => 0.78f,
            WorkerSocialInteractionKind.PlayerPromptedConversationFailed => 0.70f,
            WorkerSocialInteractionKind.CoworkerShift => 0.65f,
            WorkerSocialInteractionKind.IdleConversation => 0.55f,
            WorkerSocialInteractionKind.ServiceCoPresence => 0.45f,
            _ => 0.35f
        };

        if (Mathf.Abs(memory.Relationship) >= 50)
        {
            value = Mathf.Max(value, 0.75f);
        }

        return value;
    }

    private RelationCategory ClassifySocialGraphRelation(DriverAgent selected, DriverAgent other, WorkerSocialMemory memory)
    {
        if (IsSocialGraphFamilyRelation(selected, other, memory))
        {
            return RelationCategory.Family;
        }

        if (memory.Relationship <= -20)
        {
            return RelationCategory.Conflict;
        }

        if (IsSocialGraphWorkRelation(selected, other, memory))
        {
            return RelationCategory.Work;
        }

        if (memory.Relationship >= 20 && memory.Familiarity >= 30)
        {
            return RelationCategory.Friend;
        }

        if (IsSocialGraphNeighborRelation(selected, other))
        {
            return RelationCategory.Neighbor;
        }

        return RelationCategory.Acquaintance;
    }

    private static bool IsSocialGraphFamilyRelation(DriverAgent selected, DriverAgent other, WorkerSocialMemory memory)
    {
        return selected != null &&
               other != null &&
               ((selected.FamilyId > 0 && selected.FamilyId == other.FamilyId) ||
                memory.LastKind == WorkerSocialInteractionKind.FamilyFormation);
    }

    private bool IsSocialGraphWorkRelation(DriverAgent selected, DriverAgent other, WorkerSocialMemory memory)
    {
        if (memory.LastKind == WorkerSocialInteractionKind.CoworkerShift)
        {
            return true;
        }

        if (selected == null || other == null)
        {
            return false;
        }

        if (selected.AssignedTruckNumber > 0 && selected.AssignedTruckNumber == other.AssignedTruckNumber)
        {
            return true;
        }

        if (selected.AssignedBuildingType.HasValue &&
            other.AssignedBuildingType.HasValue &&
            selected.AssignedBuildingType.Value == other.AssignedBuildingType.Value)
        {
            return selected.AssignedBuildingInstanceId <= 0 ||
                   other.AssignedBuildingInstanceId <= 0 ||
                   selected.AssignedBuildingInstanceId == other.AssignedBuildingInstanceId;
        }

        return false;
    }

    private static bool IsSocialGraphNeighborRelation(DriverAgent selected, DriverAgent other)
    {
        if (selected == null || other == null)
        {
            return false;
        }

        if (selected.AssignedPersonalHouseIndex >= 0 &&
            other.AssignedPersonalHouseIndex >= 0 &&
            selected.AssignedPersonalHouseIndex == other.AssignedPersonalHouseIndex)
        {
            return true;
        }

        return selected.AssignedPersonalHouseIndex >= 0 &&
               other.AssignedPersonalHouseIndex >= 0 &&
               Mathf.Abs(selected.AssignedPersonalHouseIndex - other.AssignedPersonalHouseIndex) <= 1;
    }

    private Dictionary<int, Vector2> BuildSocialGraphPositions(
        DriverAgent selected,
        List<DriverAgent> visibleWorkers,
        List<SocialRelationViewModel> visibleRelations,
        Vector2 canvasSize)
    {
        if (selected == null)
        {
            return BuildSocialGraphCityPositions(visibleWorkers, visibleRelations, canvasSize);
        }

        Dictionary<int, Vector2> positions = new();
        positions[selected.DriverId] = Vector2.zero;

        for (int i = 0; i < visibleRelations.Count; i++)
        {
            SocialRelationViewModel relation = visibleRelations[i];
            int categoryCount = CountSocialGraphCategory(visibleRelations, relation.Category);
            int categoryIndex = GetSocialGraphCategoryIndex(visibleRelations, relation, i);
            float centeredIndex = categoryIndex - (categoryCount - 1) * 0.5f;
            float categoryShare = categoryCount / (float)Mathf.Max(1, visibleRelations.Count);
            bool dominantCategory = categoryCount >= 5 && categoryShare >= 0.62f;
            float fanAngle = dominantCategory
                ? Mathf.Lerp(112f, 158f, Mathf.InverseLerp(5f, SocialGraphFocusedRelationLimit, categoryCount))
                : Mathf.Min(58f, 14f + categoryCount * 8f);
            float angleOffset = GetSocialGraphCategoryFanAngle(categoryIndex, categoryCount, fanAngle);
            Vector2 direction = RotateSocialGraphDirection(GetSocialGraphCategoryDirection(relation.Category), angleOffset);
            Vector2 tangent = new(-direction.y, direction.x);
            float minRadius = Mathf.Min(canvasSize.x * 0.18f, canvasSize.y * 0.18f);
            float maxRadius = Mathf.Min(canvasSize.x * 0.38f, canvasSize.y * 0.36f);
            float distance = Mathf.Lerp(maxRadius, minRadius, relation.Importance) +
                             (dominantCategory ? Mathf.Abs(centeredIndex) * 8f : Mathf.Abs(centeredIndex) * 4f);
            float tangentOffset = dominantCategory ? centeredIndex * 8f : centeredIndex * 46f;
            Vector2 position = direction * distance + tangent * tangentOffset;
            positions[relation.OtherWorkerId] = ClampSocialGraphPosition(position, canvasSize, 78f);
        }

        return positions;
    }

    private List<DriverAgent> BuildSocialGraphCityVisibleWorkers(List<SocialRelationViewModel> visibleRelations)
    {
        Dictionary<int, int> degreeByWorkerId = new();
        for (int i = 0; i < visibleRelations.Count; i++)
        {
            SocialRelationViewModel relation = visibleRelations[i];
            degreeByWorkerId.TryGetValue(relation.FocusWorkerId, out int focusDegree);
            degreeByWorkerId[relation.FocusWorkerId] = focusDegree + 1;
            degreeByWorkerId.TryGetValue(relation.OtherWorkerId, out int otherDegree);
            degreeByWorkerId[relation.OtherWorkerId] = otherDegree + 1;
        }

        List<DriverAgent> workers = new();
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (!IsSocialGraphWorkerVisible(worker))
            {
                continue;
            }

            degreeByWorkerId.TryGetValue(worker.DriverId, out int visibleDegree);
            bool isSocialLeader = worker.SocialLeadershipStatus == WorkerSocialLeadershipStatus.SocialLeader &&
                                  worker.SocialLeadershipLinkCount > 0;
            if (visibleDegree <= 0 && !isSocialLeader)
            {
                continue;
            }

            workers.Add(worker);
        }

        workers.Sort((a, b) =>
        {
            bool aLeader = a.SocialLeadershipStatus == WorkerSocialLeadershipStatus.SocialLeader;
            bool bLeader = b.SocialLeadershipStatus == WorkerSocialLeadershipStatus.SocialLeader;
            if (aLeader != bLeader) return bLeader.CompareTo(aLeader);

            int rank = GetSocialLeadershipSortRank(a).CompareTo(GetSocialLeadershipSortRank(b));
            if (rank != 0) return rank;

            degreeByWorkerId.TryGetValue(a.DriverId, out int aDegree);
            degreeByWorkerId.TryGetValue(b.DriverId, out int bDegree);
            int degree = bDegree.CompareTo(aDegree);
            if (degree != 0) return degree;
            int score = b.SocialLeadershipScore.CompareTo(a.SocialLeadershipScore);
            if (score != 0) return score;
            return a.DriverId.CompareTo(b.DriverId);
        });
        return workers;
    }

    private Dictionary<int, Vector2> BuildSocialGraphCityPositions(
        List<DriverAgent> visibleWorkers,
        List<SocialRelationViewModel> visibleRelations,
        Vector2 canvasSize)
    {
        Dictionary<int, Vector2> positions = new();
        if (visibleWorkers == null || visibleWorkers.Count == 0)
        {
            return positions;
        }

        DriverAgent leader = null;
        for (int i = 0; i < visibleWorkers.Count; i++)
        {
            DriverAgent worker = visibleWorkers[i];
            if (worker != null && worker.SocialLeadershipStatus == WorkerSocialLeadershipStatus.SocialLeader)
            {
                leader = worker;
                positions[worker.DriverId] = Vector2.zero;
                break;
            }
        }

        float outerRadiusX = Mathf.Max(210f, canvasSize.x * 0.39f);
        float outerRadiusY = Mathf.Max(150f, canvasSize.y * 0.35f);
        float innerRadiusX = Mathf.Max(150f, outerRadiusX * 0.48f);
        float innerRadiusY = Mathf.Max(112f, outerRadiusY * 0.48f);
        int otherCount = Mathf.Max(1, visibleWorkers.Count - (leader != null ? 1 : 0));
        int otherIndex = 0;

        for (int i = 0; i < visibleWorkers.Count; i++)
        {
            DriverAgent worker = visibleWorkers[i];
            if (worker == null || worker == leader)
            {
                continue;
            }

            float t = otherCount <= 1 ? 0f : otherIndex / (float)otherCount;
            float angle = t * Mathf.PI * 2f - Mathf.PI * 0.5f;
            float socialPull = Mathf.Clamp01(worker.SocialLeadershipScore);
            float radiusX = Mathf.Lerp(outerRadiusX, innerRadiusX, socialPull);
            float radiusY = Mathf.Lerp(outerRadiusY, innerRadiusY, socialPull);
            Vector2 position = new(Mathf.Cos(angle) * radiusX, Mathf.Sin(angle) * radiusY);
            positions[worker.DriverId] = ClampSocialGraphPosition(position, canvasSize, 72f);
            otherIndex++;
        }

        return positions;
    }

    private Dictionary<int, SocialRelationViewModel> BuildSocialGraphRelationByWorkerMap(List<SocialRelationViewModel> visibleRelations)
    {
        Dictionary<int, SocialRelationViewModel> result = new();
        for (int i = 0; i < visibleRelations.Count; i++)
        {
            SocialRelationViewModel relation = visibleRelations[i];
            if (!result.TryGetValue(relation.FocusWorkerId, out SocialRelationViewModel focusRelation) ||
                relation.Importance > focusRelation.Importance)
            {
                result[relation.FocusWorkerId] = relation;
            }

            if (!result.TryGetValue(relation.OtherWorkerId, out SocialRelationViewModel otherRelation) ||
                relation.Importance > otherRelation.Importance)
            {
                result[relation.OtherWorkerId] = relation;
            }
        }

        return result;
    }

    private static int CountSocialGraphCategory(List<SocialRelationViewModel> relations, RelationCategory category)
    {
        int count = 0;
        for (int i = 0; i < relations.Count; i++)
        {
            if (relations[i].Category == category)
            {
                count++;
            }
        }

        return count;
    }

    private static int GetSocialGraphCategoryIndex(List<SocialRelationViewModel> relations, SocialRelationViewModel relation, int fallbackIndex)
    {
        int index = 0;
        for (int i = 0; i < relations.Count; i++)
        {
            SocialRelationViewModel candidate = relations[i];
            if (candidate.Category != relation.Category)
            {
                continue;
            }

            if (ReferenceEquals(candidate, relation))
            {
                return index;
            }

            index++;
        }

        return fallbackIndex;
    }

    private static Vector2 GetSocialGraphCategoryDirection(RelationCategory category)
    {
        return category switch
        {
            RelationCategory.Family => Vector2.up,
            RelationCategory.Friend => new Vector2(-0.78f, 0.62f).normalized,
            RelationCategory.Work => Vector2.right,
            RelationCategory.Neighbor => Vector2.left,
            RelationCategory.Conflict => Vector2.down,
            _ => new Vector2(0.78f, -0.62f).normalized
        };
    }

    private static float GetSocialGraphCategoryFanAngle(int categoryIndex, int categoryCount, float maxAngle)
    {
        if (categoryCount <= 1)
        {
            return 0f;
        }

        int ring = (categoryIndex + 1) / 2;
        if (ring <= 0)
        {
            return 0f;
        }

        float side = categoryIndex % 2 == 0 ? 1f : -1f;
        float maxRing = Mathf.Max(1f, Mathf.Ceil((categoryCount - 1) * 0.5f));
        return side * Mathf.Clamp(maxAngle * (ring / maxRing), 0f, maxAngle);
    }

    private static Vector2 RotateSocialGraphDirection(Vector2 direction, float angleDegrees)
    {
        float radians = angleDegrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);
        Vector2 result = new(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
        return result.sqrMagnitude > 0.0001f ? result.normalized : Vector2.right;
    }

    private static Vector2 ClampSocialGraphPosition(Vector2 position, Vector2 canvasSize, float inset)
    {
        float halfX = Mathf.Max(120f, canvasSize.x * 0.5f - inset);
        float halfY = Mathf.Max(90f, canvasSize.y * 0.5f - inset);
        return new Vector2(
            Mathf.Clamp(position.x, -halfX, halfX),
            Mathf.Clamp(position.y, -halfY, halfY));
    }

    private SocialRelationViewModel GetHoveredSocialGraphRelation(List<SocialRelationViewModel> visibleRelations)
    {
        for (int i = 0; i < visibleRelations.Count; i++)
        {
            SocialRelationViewModel relation = visibleRelations[i];
            if (relation.EdgeKey == hoveredSocialGraphEdgeKey || relation.OtherWorkerId == hoveredSocialGraphWorkerId)
            {
                return relation;
            }
        }

        return null;
    }

    private void UpdateSocialGraphFilterButtons(bool ru, bool cityOverview)
    {
        for (int i = 0; i < socialGraphScreenUi.FilterButtons.Count; i++)
        {
            SocialGraphFilterMode mode = (SocialGraphFilterMode)i;
            Button button = socialGraphScreenUi.FilterButtons[i];
            Text text = socialGraphScreenUi.FilterButtonTexts[i];
            bool selected = socialGraphFilterMode == mode;
            text.text = GetSocialGraphFilterLabel(mode, ru, cityOverview);
            Color backgroundColor = selected ? FleetPrimaryButtonColor : FleetCardMutedColor;
            button.image.color = backgroundColor;
            ColorBlock colors = button.colors;
            colors.normalColor = backgroundColor;
            colors.highlightedColor = Color.Lerp(backgroundColor, Color.white, 0.16f);
            colors.pressedColor = Color.Lerp(backgroundColor, Color.black, 0.18f);
            colors.selectedColor = backgroundColor;
            button.colors = colors;
            text.color = selected ? Color.white : FleetSecondaryTextColor;
        }
    }

    private static string GetSocialGraphFilterLabel(SocialGraphFilterMode mode, bool ru, bool cityOverview)
    {
        return mode switch
        {
            SocialGraphFilterMode.Conflict => ru ? "\u041a\u043e\u043d\u0444\u043b\u0438\u043a\u0442\u044b" : "Conflicts",
            SocialGraphFilterMode.Work => ru ? "\u0420\u0430\u0431\u043e\u0442\u0430" : "Work",
            _ => cityOverview
                ? (ru ? "\u0412\u0435\u0441\u044c \u0433\u043e\u0440\u043e\u0434" : "Whole city")
                : (ru ? "\u0412\u0441\u0435 \u0432\u0430\u0436\u043d\u044b\u0435" : "Important")
        };
    }

    private static string GetSocialGraphCategoryLabel(RelationCategory category, bool ru)
    {
        return category switch
        {
            RelationCategory.Family => ru ? "\u0441\u0435\u043c\u044c\u044f" : "family",
            RelationCategory.Friend => ru ? "\u0434\u0440\u0443\u0437\u044c\u044f" : "friend",
            RelationCategory.Work => ru ? "\u0440\u0430\u0431\u043e\u0442\u0430" : "work",
            RelationCategory.Neighbor => ru ? "\u0441\u043e\u0441\u0435\u0434\u0438" : "neighbor",
            RelationCategory.Conflict => ru ? "\u043a\u043e\u043d\u0444\u043b\u0438\u043a\u0442" : "conflict",
            _ => ru ? "\u0437\u043d\u0430\u043a\u043e\u043c\u044b\u0439" : "acquaintance"
        };
    }

    private static string GetSocialGraphToneLabel(int relationship, bool ru)
    {
        if (relationship >= 20) return ru ? "\u043f\u043e\u0437\u0438\u0442\u0438\u0432\u043d\u0430\u044f" : "positive";
        if (relationship <= -20) return ru ? "\u043d\u0430\u043f\u0440\u044f\u0436\u0435\u043d\u043d\u0430\u044f" : "tense";
        return ru ? "\u043d\u0435\u0439\u0442\u0440\u0430\u043b\u044c\u043d\u0430\u044f" : "neutral";
    }

    private string FormatSocialGraphRelationDetail(DriverAgent selected, SocialRelationViewModel relation, bool ru)
    {
        string focusName = selected?.DriverName ?? relation.FocusWorker?.DriverName ?? (ru ? "\u0416\u0438\u0442\u0435\u043b\u044c" : "Citizen");
        string otherName = relation.OtherWorkerName ?? (ru ? "\u0416\u0438\u0442\u0435\u043b\u044c" : "Citizen");
        return ru
            ? $"{focusName} <-> {otherName}\n\u0422\u0438\u043f: {GetSocialGraphCategoryLabel(relation.Category, ru)}; \u0442\u043e\u043d: {GetSocialGraphToneLabel(relation.Relationship, ru)}\n\u0421\u0438\u043b\u0430: {relation.Familiarity}/100; \u0432\u0430\u0436\u043d\u043e\u0441\u0442\u044c: {Mathf.RoundToInt(relation.Importance * 100f)}\n\u041f\u0440\u0438\u0447\u0438\u043d\u0430: {FormatSocialGraphReason(relation, ru)}"
            : $"{focusName} <-> {otherName}\nType: {GetSocialGraphCategoryLabel(relation.Category, ru)}; tone: {GetSocialGraphToneLabel(relation.Relationship, ru)}\nStrength: {relation.Familiarity}/100; importance: {Mathf.RoundToInt(relation.Importance * 100f)}\nReason: {FormatSocialGraphReason(relation, ru)}";
    }

    private string FormatSocialGraphReason(SocialRelationViewModel relation, bool ru)
    {
        if (relation.Category == RelationCategory.Family)
        {
            return ru ? "\u0441\u0435\u043c\u0435\u0439\u043d\u0430\u044f \u0441\u0432\u044f\u0437\u044c" : "family bond";
        }

        if (relation.Category == RelationCategory.Work)
        {
            return relation.LastKind == WorkerSocialInteractionKind.CoworkerShift
                ? (ru ? "\u0440\u0430\u0431\u043e\u0442\u0430\u043b\u0438 \u0432 \u043e\u0434\u043d\u0443 \u0441\u043c\u0435\u043d\u0443" : "worked the same shift")
                : (ru ? "\u043e\u0431\u0449\u0430\u044f \u0440\u0430\u0431\u043e\u0442\u0430" : "shared workplace");
        }

        if (relation.Category == RelationCategory.Conflict)
        {
            return ru ? "\u043e\u0442\u043d\u043e\u0448\u0435\u043d\u0438\u0435 \u0443\u0448\u043b\u043e \u0432 \u043d\u0430\u043f\u0440\u044f\u0436\u0435\u043d\u0438\u0435" : "relationship is tense";
        }

        if (relation.Category == RelationCategory.Friend)
        {
            return ru ? "\u0441\u0438\u043b\u044c\u043d\u0430\u044f \u0438 \u043f\u043e\u0437\u0438\u0442\u0438\u0432\u043d\u0430\u044f \u0441\u0432\u044f\u0437\u044c" : "strong positive bond";
        }

        if (relation.Category == RelationCategory.Neighbor)
        {
            return ru ? "\u0431\u043b\u0438\u0437\u043a\u0438\u0435 \u0434\u043e\u043c\u0430 \u0438\u043b\u0438 \u043e\u0431\u0449\u0438\u0439 \u0434\u043e\u043c" : "nearby or shared home";
        }

        return relation.LastKind switch
        {
            WorkerSocialInteractionKind.PlayerPromptedConversation => ru ? "\u0442\u0435\u043c\u0430 \u043e\u0442 \u0438\u0433\u0440\u043e\u043a\u0430 \u043f\u043e\u043c\u043e\u0433\u043b\u0430 \u043d\u0430\u0447\u0430\u0442\u044c \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440" : "player topic helped them talk",
            WorkerSocialInteractionKind.PlayerPromptedConversationFailed => ru ? "\u0442\u0435\u043c\u0430 \u043e\u0442 \u0438\u0433\u0440\u043e\u043a\u0430 \u043e\u0431\u0435\u0440\u043d\u0443\u043b\u0430\u0441\u044c \u043d\u0435\u043b\u043e\u0432\u043a\u043e\u0441\u0442\u044c\u044e" : "player topic turned awkward",
            WorkerSocialInteractionKind.IdleConversation => ru ? "\u0447\u0430\u0441\u0442\u043e \u043e\u0431\u0449\u0430\u043b\u0438\u0441\u044c" : "talked during idle time",
            WorkerSocialInteractionKind.ServiceCoPresence => ru ? "\u0432\u0441\u0442\u0440\u0435\u0447\u0430\u043b\u0438\u0441\u044c \u0432 \u0441\u0435\u0440\u0432\u0438\u0441\u0435" : "met at a service building",
            _ => ru ? "\u043f\u0430\u043c\u044f\u0442\u044c \u043e \u0432\u0441\u0442\u0440\u0435\u0447\u0435" : "remembered contact"
        };
    }

    private static long MakeSocialGraphEdgeKey(int firstWorkerId, int secondWorkerId)
    {
        int a = Mathf.Min(firstWorkerId, secondWorkerId);
        int b = Mathf.Max(firstWorkerId, secondWorkerId);
        return ((long)a << 32) | (uint)b;
    }
}
