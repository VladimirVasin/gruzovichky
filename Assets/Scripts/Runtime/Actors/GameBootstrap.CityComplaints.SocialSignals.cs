using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int CityComplaintSocialClusterMinSigners = 2;
    private const int CityComplaintSocialClusterMinStrength = 72;
    private const int CityComplaintSocialClusterMaxCreatedPerDay = 2;
    private const float CityComplaintPublicConcernCooldownWorldHours = 20f;
    private const float CityComplaintPublicConcernResolutionGraceWorldHours = 12f;

    private sealed class CityComplaintSocialSignalCluster
    {
        public string GroupKey = string.Empty;
        public string TopicKey = string.Empty;
        public string TitleRu = string.Empty;
        public string TitleEn = string.Empty;
        public string ReasonRu = string.Empty;
        public string ReasonEn = string.Empty;
        public SocialSignalCategory Category = SocialSignalCategory.City;
        public int SignalCount;
        public int Strength;
        public int ConfidenceTotal;
        public int StrongestSignalStrength;
        public readonly List<int> SignerIds = new();
        public readonly List<string> SignerNames = new();
    }

    private void GenerateCityComplaintsFromNegativeSocialSignalClusters(int endedDay)
    {
        if (endedDay <= 0 || !locations.ContainsKey(LocationType.CityHall) || socialSignals.Count == 0)
        {
            return;
        }

        Dictionary<string, CityComplaintSocialSignalCluster> clusters = new();
        for (int i = 0; i < socialSignals.Count; i++)
        {
            SocialSignal signal = socialSignals[i];
            if (signal == null)
            {
                continue;
            }

            if (signal.Day < endedDay)
            {
                break;
            }

            if (signal.Day != endedDay || !IsNegativeSocialSignalComplaintCandidate(signal))
            {
                continue;
            }

            string groupKey = BuildCityComplaintSocialClusterKey(signal);
            if (!clusters.TryGetValue(groupKey, out CityComplaintSocialSignalCluster cluster))
            {
                cluster = CreateCityComplaintSocialSignalCluster(signal, groupKey);
                clusters[groupKey] = cluster;
            }

            AddSocialSignalToCityComplaintCluster(cluster, signal);
        }

        if (clusters.Count == 0)
        {
            return;
        }

        List<CityComplaintSocialSignalCluster> matureClusters = new();
        foreach (KeyValuePair<string, CityComplaintSocialSignalCluster> pair in clusters)
        {
            CityComplaintSocialSignalCluster cluster = pair.Value;
            if (IsCityComplaintSocialClusterMature(cluster) &&
                CanCreatePublicConcernCityComplaint(cluster))
            {
                matureClusters.Add(cluster);
            }
        }

        if (matureClusters.Count == 0)
        {
            return;
        }

        matureClusters.Sort(CompareCityComplaintSocialSignalClusters);
        int created = 0;
        for (int i = 0; i < matureClusters.Count && created < CityComplaintSocialClusterMaxCreatedPerDay; i++)
        {
            if (TryCreatePublicConcernCityComplaint(matureClusters[i], endedDay))
            {
                created++;
            }
        }

        if (created > 0)
        {
            isCityHallScreenDirty = true;
        }
    }

    private static bool IsNegativeSocialSignalComplaintCandidate(SocialSignal signal)
    {
        if (signal == null ||
            signal.WorkerId <= 0 ||
            !signal.PublicForNoosphere ||
            signal.Tone != SocialSignalTone.Negative ||
            signal.Strength < 12)
        {
            return false;
        }

        if (signal.SourceKind == SocialSignalSourceKind.CityComplaint ||
            signal.SourceKind == SocialSignalSourceKind.CityHallDecision ||
            signal.SourceKind == SocialSignalSourceKind.CityTrust)
        {
            return false;
        }

        return signal.SourceKind != SocialSignalSourceKind.DailyExperience ||
               string.IsNullOrWhiteSpace(signal.SourceKey) ||
               !signal.SourceKey.Contains(":summary");
    }

    private CityComplaintSocialSignalCluster CreateCityComplaintSocialSignalCluster(SocialSignal signal, string groupKey)
    {
        SocialSignalCategory category = signal?.Category ?? SocialSignalCategory.City;
        string topicKey = string.IsNullOrWhiteSpace(signal?.TopicKey)
            ? category.ToString()
            : signal.TopicKey;
        string titleRu = ResolveCityComplaintSocialSignalTitle(signal, true);
        string titleEn = ResolveCityComplaintSocialSignalTitle(signal, false);

        return new CityComplaintSocialSignalCluster
        {
            GroupKey = groupKey,
            TopicKey = NormalizeSocialSignalTopicKey(topicKey, titleEn, category),
            TitleRu = titleRu,
            TitleEn = titleEn,
            Category = category
        };
    }

    private void AddSocialSignalToCityComplaintCluster(CityComplaintSocialSignalCluster cluster, SocialSignal signal)
    {
        if (cluster == null || signal == null)
        {
            return;
        }

        int strength = signal.DailyScoreHint != 0
            ? Mathf.Abs(signal.DailyScoreHint)
            : signal.Strength;
        strength = Mathf.Clamp(strength, 1, 100);
        cluster.SignalCount++;
        cluster.Strength += strength;
        cluster.ConfidenceTotal += Mathf.Clamp(signal.Confidence, 0, 100);

        if (!cluster.SignerIds.Contains(signal.WorkerId))
        {
            cluster.SignerIds.Add(signal.WorkerId);
            cluster.SignerNames.Add(string.IsNullOrWhiteSpace(signal.WorkerName) ? $"#{signal.WorkerId}" : signal.WorkerName);
        }

        if (strength >= cluster.StrongestSignalStrength)
        {
            cluster.StrongestSignalStrength = strength;
            cluster.ReasonRu = string.IsNullOrWhiteSpace(signal.ReasonRu) ? ResolveCityComplaintSocialSignalTitle(signal, true) : signal.ReasonRu;
            cluster.ReasonEn = string.IsNullOrWhiteSpace(signal.ReasonEn) ? ResolveCityComplaintSocialSignalTitle(signal, false) : signal.ReasonEn;
        }
    }

    private bool IsCityComplaintSocialClusterMature(CityComplaintSocialSignalCluster cluster)
    {
        if (cluster == null ||
            cluster.SignerIds.Count < CityComplaintSocialClusterMinSigners ||
            cluster.Strength < CityComplaintSocialClusterMinStrength)
        {
            return false;
        }

        if (cluster.Category == SocialSignalCategory.Litter)
        {
            return cluster.Strength >= 60;
        }

        return cluster.SignerIds.Count >= 3 || cluster.Strength >= 120;
    }

    private bool CanCreatePublicConcernCityComplaint(CityComplaintSocialSignalCluster cluster)
    {
        if (cluster == null || string.IsNullOrWhiteSpace(cluster.GroupKey))
        {
            return false;
        }

        if (FindActiveCityComplaintByGroupKey(cluster.GroupKey) != null)
        {
            return false;
        }

        float now = GetCurrentWorldHour();
        return !cityComplaintCooldownByKey.TryGetValue(cluster.GroupKey, out float nextAllowedWorldHour) ||
               now >= nextAllowedWorldHour;
    }

    private bool TryCreatePublicConcernCityComplaint(CityComplaintSocialSignalCluster cluster, int sourceDay)
    {
        if (!CanCreatePublicConcernCityComplaint(cluster))
        {
            return false;
        }

        float now = GetCurrentWorldHour();
        int severity = CalculatePublicConcernCityComplaintSeverity(cluster);
        int primaryWorkerId = cluster.SignerIds.Count > 0 ? cluster.SignerIds[0] : 0;
        string primaryName = cluster.SignerNames.Count > 0
            ? cluster.SignerNames[0]
            : (IsRussianLanguage() ? "\u0416\u0438\u0442\u0435\u043b\u0438" : "Citizens");

        CityComplaint complaint = new()
        {
            Id = nextCityComplaintId++,
            WorkerId = primaryWorkerId,
            WorkerName = primaryName,
            GroupKey = cluster.GroupKey,
            Category = CityComplaintCategory.PublicConcern,
            State = CityComplaintState.Open,
            Severity = severity,
            CreatedWorldHour = now,
            CreatedDay = currentDay,
            IssueTopicKey = cluster.TopicKey,
            IssueTitleRu = cluster.TitleRu,
            IssueTitleEn = cluster.TitleEn,
            IssueReasonRu = string.IsNullOrWhiteSpace(cluster.ReasonRu) ? cluster.TitleRu : cluster.ReasonRu,
            IssueReasonEn = string.IsNullOrWhiteSpace(cluster.ReasonEn) ? cluster.TitleEn : cluster.ReasonEn,
            IssueSignalCategory = cluster.Category,
            IssueSourceDay = sourceDay,
            IssueSourceStrength = cluster.Strength,
            IsUnread = true
        };

        complaint.SignerIds.AddRange(cluster.SignerIds);
        complaint.SignerNames.AddRange(cluster.SignerNames);
        cityComplaints.Add(complaint);
        cityComplaintCooldownByKey[cluster.GroupKey] = now + CityComplaintPublicConcernCooldownWorldHours;

        RecordCityHallKnowledgeForComplaintSigners(
            complaint,
            "\u041f\u043e\u0434\u0430\u043b \u043e\u0431\u0449\u0443\u044e \u0436\u0430\u043b\u043e\u0431\u0443 \u0432 \u0440\u0430\u0442\u0443\u0448\u0443",
            "Filed a public concern at City Hall");
        RecordCityComplaintSocialSignals(
            complaint,
            SocialSignalSourceKind.CityComplaint,
            SocialSignalTone.Negative,
            Mathf.Clamp(severity * 18 + cluster.SignerIds.Count * 4, 24, 88),
            Mathf.Clamp(cluster.ConfidenceTotal / Mathf.Max(1, cluster.SignalCount), 34, 96),
            "cluster_filed",
            "\u043f\u043e\u0445\u043e\u0436\u0438\u0435 \u043d\u0435\u0433\u0430\u0442\u0438\u0432\u043d\u044b\u0435 \u0441\u0438\u0433\u043d\u0430\u043b\u044b \u0441\u043b\u043e\u0436\u0438\u043b\u0438\u0441\u044c \u0432 \u043e\u0431\u0440\u0430\u0449\u0435\u043d\u0438\u0435",
            "similar negative signals became a public request",
            includeInDailyExperience: true);
        NotifyCityHallNewRequest(complaint);
        SessionDebugLogger.Log(
            "CITY_HALL",
            $"Public concern #{complaint.Id} filed from social signals: key={cluster.GroupKey}, signers={cluster.SignerIds.Count}, strength={cluster.Strength}, severity={severity}.");
        return true;
    }

    private bool DoesCityPublicConcernConditionRemain(CityComplaint complaint, out string reason)
    {
        reason = string.Empty;
        if (complaint == null)
        {
            reason = "public concern missing";
            return false;
        }

        if (complaint.State == CityComplaintState.Open)
        {
            return true;
        }

        float now = GetCurrentWorldHour();
        if (complaint.State == CityComplaintState.Accepted &&
            now - complaint.AcceptedWorldHour < CityComplaintPublicConcernResolutionGraceWorldHours)
        {
            return true;
        }

        if (TryBuildRecentPublicConcernCluster(complaint, out CityComplaintSocialSignalCluster cluster) &&
            IsCityComplaintSocialClusterMature(cluster))
        {
            return true;
        }

        reason = "public concern eased";
        return false;
    }

    private bool HasRecentWorkerNegativeSocialSignalForPublicConcern(CityComplaint complaint, DriverAgent worker)
    {
        if (complaint == null || worker == null || string.IsNullOrWhiteSpace(complaint.GroupKey))
        {
            return false;
        }

        float sinceWorldHour = complaint.AcceptedWorldHour > 0f
            ? complaint.AcceptedWorldHour
            : complaint.CreatedWorldHour;
        for (int i = 0; i < socialSignals.Count; i++)
        {
            SocialSignal signal = socialSignals[i];
            if (signal == null || signal.WorldHour < sinceWorldHour)
            {
                continue;
            }

            if (signal.WorkerId == worker.DriverId &&
                IsNegativeSocialSignalComplaintCandidate(signal) &&
                string.Equals(BuildCityComplaintSocialClusterKey(signal), complaint.GroupKey, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryBuildRecentPublicConcernCluster(CityComplaint complaint, out CityComplaintSocialSignalCluster cluster)
    {
        cluster = null;
        if (complaint == null || string.IsNullOrWhiteSpace(complaint.GroupKey))
        {
            return false;
        }

        float sinceWorldHour = complaint.AcceptedWorldHour > 0f
            ? complaint.AcceptedWorldHour
            : complaint.CreatedWorldHour;
        for (int i = 0; i < socialSignals.Count; i++)
        {
            SocialSignal signal = socialSignals[i];
            if (signal == null)
            {
                continue;
            }

            if (signal.WorldHour < sinceWorldHour)
            {
                break;
            }

            if (!IsNegativeSocialSignalComplaintCandidate(signal) ||
                !string.Equals(BuildCityComplaintSocialClusterKey(signal), complaint.GroupKey, System.StringComparison.Ordinal))
            {
                continue;
            }

            cluster ??= CreateCityComplaintSocialSignalCluster(signal, complaint.GroupKey);
            AddSocialSignalToCityComplaintCluster(cluster, signal);
        }

        return cluster != null;
    }

    private static int CompareCityComplaintSocialSignalClusters(CityComplaintSocialSignalCluster a, CityComplaintSocialSignalCluster b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;
        int strengthCompare = b.Strength.CompareTo(a.Strength);
        if (strengthCompare != 0) return strengthCompare;
        return b.SignerIds.Count.CompareTo(a.SignerIds.Count);
    }

    private static int CalculatePublicConcernCityComplaintSeverity(CityComplaintSocialSignalCluster cluster)
    {
        if (cluster == null)
        {
            return 1;
        }

        if (cluster.Strength >= 260 || cluster.SignerIds.Count >= 6)
        {
            return 4;
        }

        if (cluster.Strength >= 150 || cluster.SignerIds.Count >= 4)
        {
            return 3;
        }

        return 2;
    }

    private static string BuildCityComplaintSocialClusterKey(SocialSignal signal)
    {
        SocialSignalCategory category = signal?.Category ?? SocialSignalCategory.City;
        if (category == SocialSignalCategory.Litter)
        {
            return "social_cluster:litter:street_litter";
        }

        string topic = NormalizeSocialSignalTopicKey(signal?.TopicKey, signal?.TopicLabelEn, category);
        return $"social_cluster:{category}:{topic}";
    }

    private static string ResolveCityComplaintSocialSignalTitle(SocialSignal signal, bool ru)
    {
        if (signal == null)
        {
            return GetSocialSignalCategoryLabel(SocialSignalCategory.City, ru);
        }

        if (signal.Category == SocialSignalCategory.Litter)
        {
            return ru ? "\u043c\u0443\u0441\u043e\u0440 \u043d\u0430 \u0443\u043b\u0438\u0446\u0430\u0445" : "street litter";
        }

        string label = ru ? signal.TopicLabelRu : signal.TopicLabelEn;
        if (!string.IsNullOrWhiteSpace(label))
        {
            return label;
        }

        return GetSocialSignalCategoryLabel(signal.Category, ru);
    }
}
