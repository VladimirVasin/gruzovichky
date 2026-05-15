using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int CityComplaintSocialClusterMinSigners = 2;
    private const int CityComplaintSocialClusterMinStrength = 72;
    private const int CityComplaintSocialClusterMaxCreatedPerDay = 2;
    private const int CityComplaintSocialClusterPositiveMinStrength = 8;
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
        public int PositiveStrength;
        public int PositiveSignalCount;
        public int PositiveSignerCount;
        public int ActiveSignerCount;
        public int NetStrength;
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
            ApplyPublicConcernSignalBalanceForDay(cluster, endedDay);
            if (IsPublicConcernBlockedByExistingServiceBuilding(cluster, out LocationType existingService))
            {
                if (cluster.Strength >= CityComplaintSocialClusterMinStrength)
                {
                    SessionDebugLogger.Log(
                        "CITY_HALL",
                        $"Public concern suppressed because service building exists: key={cluster.GroupKey}, service={existingService}, negative={cluster.Strength}, positive={cluster.PositiveStrength}, net={GetCityComplaintSocialClusterNetStrength(cluster)}, activeSigners={GetCityComplaintSocialClusterActiveSignerCount(cluster)}/{cluster.SignerIds.Count}.");
                }

                continue;
            }

            if (IsPublicConcernBlockedByAvailableWork(cluster, out int availableWork, out int unassignedWorkers))
            {
                if (cluster.Strength >= CityComplaintSocialClusterMinStrength)
                {
                    SessionDebugLogger.Log(
                        "CITY_HALL",
                        $"Public concern suppressed because work is available: key={cluster.GroupKey}, availableWork={availableWork}, unassignedWorkers={unassignedWorkers}, negative={cluster.Strength}, positive={cluster.PositiveStrength}, net={GetCityComplaintSocialClusterNetStrength(cluster)}, activeSigners={GetCityComplaintSocialClusterActiveSignerCount(cluster)}/{cluster.SignerIds.Count}.");
                }

                continue;
            }

            if (IsCityComplaintSocialClusterMature(cluster))
            {
                if (TryMergePublicConcernClusterIntoActiveComplaint(cluster))
                {
                    continue;
                }

                if (CanCreatePublicConcernCityComplaint(cluster))
                {
                    matureClusters.Add(cluster);
                }
            }
            else if (cluster != null &&
                     cluster.PositiveStrength > 0 &&
                     cluster.Strength >= CityComplaintSocialClusterMinStrength)
            {
                SessionDebugLogger.Log(
                    "CITY_HALL",
                    $"Public concern suppressed by balanced signals: key={cluster.GroupKey}, negative={cluster.Strength}, positive={cluster.PositiveStrength}, net={GetCityComplaintSocialClusterNetStrength(cluster)}, activeSigners={GetCityComplaintSocialClusterActiveSignerCount(cluster)}/{cluster.SignerIds.Count}.");
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

    private static bool IsSocialSignalComplaintCandidate(SocialSignal signal)
    {
        if (signal == null ||
            signal.WorkerId <= 0 ||
            !signal.PublicForNoosphere ||
            signal.Tone == SocialSignalTone.Neutral)
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

    private static bool IsNegativeSocialSignalComplaintCandidate(SocialSignal signal)
    {
        return IsSocialSignalComplaintCandidate(signal) &&
               signal.Tone == SocialSignalTone.Negative &&
               signal.Strength >= 12;
    }

    private static bool IsPositiveSocialSignalComplaintBalancer(SocialSignal signal)
    {
        return IsSocialSignalComplaintCandidate(signal) &&
               signal.Tone == SocialSignalTone.Positive &&
               signal.Strength >= CityComplaintSocialClusterPositiveMinStrength;
    }

    private CityComplaintSocialSignalCluster CreateCityComplaintSocialSignalCluster(SocialSignal signal, string groupKey)
    {
        bool streetLitter = IsStreetLitterPublicConcernSignal(signal);
        SocialSignalCategory category = streetLitter ? SocialSignalCategory.Litter : signal?.Category ?? SocialSignalCategory.City;
        string topicKey = streetLitter
            ? "street_litter"
            : string.IsNullOrWhiteSpace(signal?.TopicKey)
                ? category.ToString()
                : signal.TopicKey;
        string titleRu = streetLitter ? "\u043c\u0443\u0441\u043e\u0440 \u043d\u0430 \u0443\u043b\u0438\u0446\u0430\u0445" : ResolveCityComplaintSocialSignalTitle(signal, true);
        string titleEn = streetLitter ? "street litter" : ResolveCityComplaintSocialSignalTitle(signal, false);

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

        int strength = GetCityComplaintSocialSignalStrength(signal);
        cluster.SignalCount++;
        cluster.Strength += strength;
        cluster.NetStrength = Mathf.Max(0, cluster.Strength - cluster.PositiveStrength);
        cluster.ConfidenceTotal += Mathf.Clamp(signal.Confidence, 0, 100);

        if (!cluster.SignerIds.Contains(signal.WorkerId))
        {
            cluster.SignerIds.Add(signal.WorkerId);
            cluster.SignerNames.Add(string.IsNullOrWhiteSpace(signal.WorkerName) ? $"#{signal.WorkerId}" : signal.WorkerName);
        }
        cluster.ActiveSignerCount = cluster.SignerIds.Count;

        if (strength >= cluster.StrongestSignalStrength)
        {
            cluster.StrongestSignalStrength = strength;
            cluster.ReasonRu = string.IsNullOrWhiteSpace(signal.ReasonRu) ? ResolveCityComplaintSocialSignalTitle(signal, true) : signal.ReasonRu;
            cluster.ReasonEn = string.IsNullOrWhiteSpace(signal.ReasonEn) ? ResolveCityComplaintSocialSignalTitle(signal, false) : signal.ReasonEn;
        }
    }

    private bool IsCityComplaintSocialClusterMature(CityComplaintSocialSignalCluster cluster)
    {
        int activeSignerCount = GetCityComplaintSocialClusterActiveSignerCount(cluster);
        int netStrength = GetCityComplaintSocialClusterNetStrength(cluster);
        if (cluster == null ||
            activeSignerCount < CityComplaintSocialClusterMinSigners ||
            netStrength < CityComplaintSocialClusterMinStrength)
        {
            return false;
        }

        if (cluster.Category == SocialSignalCategory.Litter)
        {
            return netStrength >= 60;
        }

        return activeSignerCount >= 3 || netStrength >= 120;
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

        if (FindActivePublicConcernBySemanticKey(GetPublicConcernSemanticKey(cluster)) != null)
        {
            return false;
        }

        if (IsPublicConcernBlockedByExistingServiceBuilding(cluster, out _))
        {
            return false;
        }

        if (IsPublicConcernBlockedByAvailableWork(cluster, out _, out _))
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
        int netStrength = GetCityComplaintSocialClusterNetStrength(cluster);
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
            IssueSourceStrength = netStrength,
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
            $"Public concern #{complaint.Id} filed from social signals: key={cluster.GroupKey}, signers={GetCityComplaintSocialClusterActiveSignerCount(cluster)}/{cluster.SignerIds.Count}, negative={cluster.Strength}, positive={cluster.PositiveStrength}, net={netStrength}, severity={severity}.");
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

        if (IsPublicConcernBlockedByExistingServiceBuilding(
                complaint.IssueSignalCategory,
                complaint.IssueTopicKey,
                complaint.GroupKey,
                out LocationType existingService))
        {
            reason = $"{existingService} already exists";
            return false;
        }

        if (IsPublicConcernBlockedByAvailableWork(
                complaint.IssueSignalCategory,
                complaint.IssueTopicKey,
                complaint.GroupKey,
                out _,
                out _))
        {
            reason = "work is available";
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

    private bool IsPublicConcernBlockedByExistingServiceBuilding(
        CityComplaintSocialSignalCluster cluster,
        out LocationType existingService)
    {
        existingService = default;
        return cluster != null &&
               IsPublicConcernBlockedByExistingServiceBuilding(
                   cluster.Category,
                   cluster.TopicKey,
                   cluster.GroupKey,
                   out existingService);
    }

    private bool IsPublicConcernBlockedByExistingServiceBuilding(
        SocialSignalCategory category,
        string topicKey,
        string groupKey,
        out LocationType existingService)
    {
        existingService = default;
        if (category != SocialSignalCategory.Need)
        {
            return false;
        }

        if (!TryExtractBuildingTypeFromPublicConcernTopic(topicKey, out existingService) &&
            !TryExtractBuildingTypeFromPublicConcernTopic(groupKey, out existingService))
        {
            return false;
        }

        return IsNeedServiceBuilding(existingService) &&
               locations.ContainsKey(existingService);
    }

    private static bool TryExtractBuildingTypeFromPublicConcernTopic(string topic, out LocationType buildingType)
    {
        buildingType = default;
        if (string.IsNullOrWhiteSpace(topic))
        {
            return false;
        }

        string[] parts = topic.Split(':');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (!string.Equals(parts[i].Trim(), "BUILDINGTYPE", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return System.Enum.TryParse(parts[i + 1].Trim(), true, out buildingType);
        }

        return false;
    }

    private static bool IsNeedServiceBuilding(LocationType type)
    {
        return type is LocationType.Canteen or
            LocationType.Kiosk or
            LocationType.Motel or
            LocationType.Bar or
            LocationType.GamblingHall or
            LocationType.CityPark;
    }

    private bool IsPublicConcernBlockedByAvailableWork(
        CityComplaintSocialSignalCluster cluster,
        out int availableWork,
        out int unassignedWorkers)
    {
        availableWork = 0;
        unassignedWorkers = 0;
        return cluster != null &&
               IsPublicConcernBlockedByAvailableWork(
                   cluster.Category,
                   cluster.TopicKey,
                   cluster.GroupKey,
                   out availableWork,
                   out unassignedWorkers);
    }

    private bool IsPublicConcernBlockedByAvailableWork(
        SocialSignalCategory category,
        string topicKey,
        string groupKey,
        out int availableWork,
        out int unassignedWorkers)
    {
        availableWork = 0;
        unassignedWorkers = 0;
        return category == SocialSignalCategory.Work &&
               IsCityWorkPublicConcernTopic(topicKey, groupKey) &&
               HasSufficientAvailableWorkForUnassignedWorkers(out availableWork, out unassignedWorkers);
    }

    private static bool IsCityWorkPublicConcernTopic(string topicKey, string groupKey)
    {
        return IsCityWorkPublicConcernTopic(topicKey) ||
               IsCityWorkPublicConcernTopic(groupKey);
    }

    private static bool IsCityWorkPublicConcernTopic(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        string normalized = NormalizeWorkerKnowledgeTopicKey(key);
        return normalized == "TEXT:CITY_WORK" ||
               normalized.EndsWith(":TEXT:CITY_WORK", System.StringComparison.Ordinal);
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

        if (cluster != null)
        {
            ApplyPublicConcernSignalBalanceSince(cluster, sinceWorldHour);
        }

        return cluster != null;
    }

    private void ApplyPublicConcernSignalBalanceForDay(CityComplaintSocialSignalCluster cluster, int sourceDay)
    {
        ApplyPublicConcernSignalBalance(cluster, sourceDay, 0f);
    }

    private void ApplyPublicConcernSignalBalanceSince(CityComplaintSocialSignalCluster cluster, float sinceWorldHour)
    {
        ApplyPublicConcernSignalBalance(cluster, 0, sinceWorldHour);
    }

    private void ApplyPublicConcernSignalBalance(CityComplaintSocialSignalCluster cluster, int sourceDay, float sinceWorldHour)
    {
        if (cluster == null || string.IsNullOrWhiteSpace(cluster.GroupKey))
        {
            return;
        }

        int positiveStrength = 0;
        int positiveSignalCount = 0;
        HashSet<int> positiveSignerIds = new();
        for (int i = 0; i < socialSignals.Count; i++)
        {
            SocialSignal signal = socialSignals[i];
            if (signal == null)
            {
                continue;
            }

            if (sourceDay > 0)
            {
                if (signal.Day < sourceDay)
                {
                    break;
                }

                if (signal.Day != sourceDay)
                {
                    continue;
                }
            }
            else if (signal.WorldHour < sinceWorldHour)
            {
                break;
            }

            if (!IsPositiveSocialSignalComplaintBalancer(signal) ||
                !string.Equals(BuildCityComplaintSocialClusterKey(signal), cluster.GroupKey, System.StringComparison.Ordinal))
            {
                continue;
            }

            positiveStrength += GetCityComplaintSocialSignalStrength(signal);
            positiveSignalCount++;
            if (signal.WorkerId > 0)
            {
                positiveSignerIds.Add(signal.WorkerId);
            }
        }

        int activeSignerCount = 0;
        for (int i = 0; i < cluster.SignerIds.Count; i++)
        {
            if (!positiveSignerIds.Contains(cluster.SignerIds[i]))
            {
                activeSignerCount++;
            }
        }

        cluster.PositiveStrength = positiveStrength;
        cluster.PositiveSignalCount = positiveSignalCount;
        cluster.PositiveSignerCount = positiveSignerIds.Count;
        cluster.ActiveSignerCount = activeSignerCount;
        cluster.NetStrength = Mathf.Max(0, cluster.Strength - positiveStrength);
    }

    private static int GetCityComplaintSocialSignalStrength(SocialSignal signal)
    {
        if (signal == null)
        {
            return 0;
        }

        int strength = signal.DailyScoreHint != 0
            ? Mathf.Abs(signal.DailyScoreHint)
            : signal.Strength;
        return Mathf.Clamp(strength, 1, 100);
    }

    private static int GetCityComplaintSocialClusterNetStrength(CityComplaintSocialSignalCluster cluster)
    {
        if (cluster == null)
        {
            return 0;
        }

        return cluster.PositiveStrength > 0 || cluster.NetStrength > 0
            ? Mathf.Max(0, cluster.NetStrength)
            : Mathf.Max(0, cluster.Strength);
    }

    private static int GetCityComplaintSocialClusterActiveSignerCount(CityComplaintSocialSignalCluster cluster)
    {
        if (cluster == null)
        {
            return 0;
        }

        return cluster.ActiveSignerCount > 0 || cluster.PositiveSignerCount > 0
            ? Mathf.Max(0, cluster.ActiveSignerCount)
            : cluster.SignerIds.Count;
    }

    private static int CompareCityComplaintSocialSignalClusters(CityComplaintSocialSignalCluster a, CityComplaintSocialSignalCluster b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;
        int netCompare = GetCityComplaintSocialClusterNetStrength(b).CompareTo(GetCityComplaintSocialClusterNetStrength(a));
        if (netCompare != 0) return netCompare;
        int strengthCompare = b.Strength.CompareTo(a.Strength);
        if (strengthCompare != 0) return strengthCompare;
        return GetCityComplaintSocialClusterActiveSignerCount(b).CompareTo(GetCityComplaintSocialClusterActiveSignerCount(a));
    }

    private static int CalculatePublicConcernCityComplaintSeverity(CityComplaintSocialSignalCluster cluster)
    {
        if (cluster == null)
        {
            return 1;
        }

        int netStrength = GetCityComplaintSocialClusterNetStrength(cluster);
        int activeSignerCount = GetCityComplaintSocialClusterActiveSignerCount(cluster);
        if (netStrength >= 260 || activeSignerCount >= 6)
        {
            return 4;
        }

        if (netStrength >= 150 || activeSignerCount >= 4)
        {
            return 3;
        }

        return 2;
    }

    private static string BuildCityComplaintSocialClusterKey(SocialSignal signal)
    {
        if (IsStreetLitterPublicConcernSignal(signal))
        {
            return "social_cluster:litter:street_litter";
        }

        SocialSignalCategory category = signal?.Category ?? SocialSignalCategory.City;
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
