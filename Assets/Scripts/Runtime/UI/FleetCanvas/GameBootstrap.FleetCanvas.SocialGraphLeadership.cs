using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private sealed class SocialGraphResidentImportance
    {
        public int WorkerId;
        public DriverAgent Worker;
        public int LinkCount;
        public int PositiveLinkCount;
        public int TenseLinkCount;
        public float TotalImportance;
        public float TotalQuality;
        public float LastInteractionWorldHour;
        public float Score;
        public float AverageQuality => LinkCount > 0 ? TotalQuality / LinkCount : 0f;
    }

    private void UpdateCitySocialLeadershipFromRelations(List<SocialRelationViewModel> allRelations, SocialGraphStats stats)
    {
        ResetCitySocialLeadership();
        if (allRelations == null || allRelations.Count == 0)
        {
            return;
        }

        Dictionary<int, SocialGraphResidentImportance> importanceByWorkerId = new();
        for (int i = 0; i < allRelations.Count; i++)
        {
            SocialRelationViewModel relation = allRelations[i];
            AddCitySocialLeadershipContribution(importanceByWorkerId, relation?.FocusWorker, relation);
            AddCitySocialLeadershipContribution(importanceByWorkerId, relation?.OtherWorker, relation);
        }

        if (importanceByWorkerId.Count == 0)
        {
            return;
        }

        int maxLinks = 1;
        float maxTotalImportance = 0.01f;
        List<SocialGraphResidentImportance> residents = new(importanceByWorkerId.Values);
        for (int i = 0; i < residents.Count; i++)
        {
            SocialGraphResidentImportance resident = residents[i];
            maxLinks = Mathf.Max(maxLinks, resident.LinkCount);
            maxTotalImportance = Mathf.Max(maxTotalImportance, resident.TotalImportance);
        }

        for (int i = 0; i < residents.Count; i++)
        {
            SocialGraphResidentImportance resident = residents[i];
            float linkScore = resident.LinkCount / (float)maxLinks;
            float importanceScore = Mathf.Clamp01(resident.TotalImportance / maxTotalImportance);
            float positiveRatio = resident.LinkCount > 0 ? resident.PositiveLinkCount / (float)resident.LinkCount : 0f;
            float tensePenalty = resident.LinkCount > 0 ? resident.TenseLinkCount / (float)resident.LinkCount * 0.16f : 0f;
            resident.Score = Mathf.Clamp01(
                linkScore * 0.38f +
                resident.AverageQuality * 0.34f +
                importanceScore * 0.18f +
                positiveRatio * 0.10f -
                tensePenalty);
        }

        residents.Sort(CompareSocialGraphResidentImportance);
        for (int i = 0; i < residents.Count; i++)
        {
            SocialGraphResidentImportance resident = residents[i];
            if (resident.Worker == null)
            {
                continue;
            }

            resident.Worker.SocialLeadershipScore = resident.Score;
            resident.Worker.SocialLeadershipRank = i + 1;
            resident.Worker.SocialLeadershipLinkCount = resident.LinkCount;
            resident.Worker.SocialLeadershipStatus = i == 0
                ? WorkerSocialLeadershipStatus.SocialLeader
                : WorkerSocialLeadershipStatus.None;
        }

        SocialGraphResidentImportance leader = residents[0];
        if (leader.Worker != null && stats != null)
        {
            stats.SocialLeader = leader.Worker;
            stats.SocialLeaderScore = leader.Score;
            stats.SocialLeaderLinkCount = leader.LinkCount;
        }
    }

    private void ResetCitySocialLeadership()
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (worker == null)
            {
                continue;
            }

            worker.SocialLeadershipStatus = WorkerSocialLeadershipStatus.None;
            worker.SocialLeadershipScore = 0f;
            worker.SocialLeadershipRank = 0;
            worker.SocialLeadershipLinkCount = 0;
        }
    }

    private void AddCitySocialLeadershipContribution(
        Dictionary<int, SocialGraphResidentImportance> importanceByWorkerId,
        DriverAgent worker,
        SocialRelationViewModel relation)
    {
        if (importanceByWorkerId == null ||
            worker == null ||
            relation == null ||
            worker.DriverId <= 0 ||
            worker.HasDepartedTown ||
            worker.IsLeavingTown)
        {
            return;
        }

        if (!importanceByWorkerId.TryGetValue(worker.DriverId, out SocialGraphResidentImportance resident))
        {
            resident = new SocialGraphResidentImportance
            {
                WorkerId = worker.DriverId,
                Worker = worker
            };
            importanceByWorkerId[worker.DriverId] = resident;
        }

        resident.LinkCount++;
        if (relation.Relationship >= 20)
        {
            resident.PositiveLinkCount++;
        }
        else if (relation.Relationship <= -20)
        {
            resident.TenseLinkCount++;
        }

        resident.TotalImportance += Mathf.Clamp01(relation.Importance);
        resident.TotalQuality += CalculateSocialGraphResidentRelationQuality(relation);
        resident.LastInteractionWorldHour = Mathf.Max(resident.LastInteractionWorldHour, relation.LastInteractionWorldHour);
    }

    private static float CalculateSocialGraphResidentRelationQuality(SocialRelationViewModel relation)
    {
        if (relation == null)
        {
            return 0f;
        }

        float quality = relation.Importance * 0.62f +
                        relation.Strength * 0.22f +
                        relation.Recency * 0.08f +
                        relation.EmotionalIntensity * 0.08f;
        quality += relation.Category switch
        {
            RelationCategory.Family => 0.12f,
            RelationCategory.Friend => 0.10f,
            RelationCategory.Work => 0.06f,
            RelationCategory.Neighbor => 0.04f,
            RelationCategory.Conflict => -0.12f,
            _ => 0f
        };

        if (relation.Relationship >= 20)
        {
            quality += 0.08f;
        }
        else if (relation.Relationship <= -20)
        {
            quality -= 0.12f;
        }

        return Mathf.Clamp01(quality);
    }

    private static int CompareSocialGraphResidentImportance(SocialGraphResidentImportance a, SocialGraphResidentImportance b)
    {
        int score = b.Score.CompareTo(a.Score);
        if (score != 0) return score;
        int links = b.LinkCount.CompareTo(a.LinkCount);
        if (links != 0) return links;
        int quality = b.AverageQuality.CompareTo(a.AverageQuality);
        if (quality != 0) return quality;
        int importance = b.TotalImportance.CompareTo(a.TotalImportance);
        if (importance != 0) return importance;
        int recent = b.LastInteractionWorldHour.CompareTo(a.LastInteractionWorldHour);
        if (recent != 0) return recent;
        return a.WorkerId.CompareTo(b.WorkerId);
    }

    private static int GetSocialLeadershipSortRank(DriverAgent worker)
    {
        return worker != null && worker.SocialLeadershipRank > 0
            ? worker.SocialLeadershipRank
            : int.MaxValue;
    }
}
