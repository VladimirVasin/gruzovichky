using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private bool ResolveDuplicatePublicConcernCityComplaints()
    {
        if (cityComplaints.Count < 2)
        {
            return false;
        }

        bool changed = false;
        Dictionary<string, CityComplaint> keeperByKey = new();
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (!IsCityComplaintActive(complaint) ||
                complaint.Category != CityComplaintCategory.PublicConcern)
            {
                continue;
            }

            NormalizePublicConcernComplaintCanonicalTopic(complaint);
            string semanticKey = GetPublicConcernSemanticKey(complaint);
            if (string.IsNullOrWhiteSpace(semanticKey))
            {
                continue;
            }

            if (!keeperByKey.TryGetValue(semanticKey, out CityComplaint keeper))
            {
                keeperByKey[semanticKey] = complaint;
                continue;
            }

            if (ShouldReplacePublicConcernDuplicateKeeper(keeper, complaint))
            {
                MergePublicConcernComplaintInto(complaint, keeper);
                ResolveDuplicatePublicConcern(keeper, complaint, semanticKey);
                keeperByKey[semanticKey] = complaint;
            }
            else
            {
                MergePublicConcernComplaintInto(keeper, complaint);
                ResolveDuplicatePublicConcern(complaint, keeper, semanticKey);
            }

            changed = true;
        }

        return changed;
    }

    private static bool IsStreetLitterPublicConcernSignal(SocialSignal signal)
    {
        return signal != null &&
               (signal.Category == SocialSignalCategory.Litter ||
                IsStreetLitterPublicConcernTopic(signal.TopicKey) ||
                IsStreetLitterPublicConcernTopic(signal.TopicLabelEn) ||
                IsStreetLitterPublicConcernTopic(signal.SourceKey));
    }

    private static bool IsStreetLitterPublicConcernTopic(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        string normalized = NormalizeWorkerKnowledgeTopicKey(key);
        return normalized == "LITTER" ||
               normalized == "STREET_LITTER" ||
               normalized == "TEXT:LITTER" ||
               normalized == "TEXT:STREET_LITTER" ||
               normalized.Contains("AFFECT_LITTER_IRRITATION", System.StringComparison.Ordinal) ||
               normalized.Contains("STREET_LITTER_", System.StringComparison.Ordinal);
    }

    private static string GetPublicConcernSemanticKey(CityComplaintSocialSignalCluster cluster)
    {
        return cluster == null
            ? string.Empty
            : GetPublicConcernSemanticKey(cluster.Category, cluster.TopicKey, cluster.GroupKey);
    }

    private static string GetPublicConcernSemanticKey(CityComplaint complaint)
    {
        return complaint == null || complaint.Category != CityComplaintCategory.PublicConcern
            ? string.Empty
            : GetPublicConcernSemanticKey(complaint.IssueSignalCategory, complaint.IssueTopicKey, complaint.GroupKey);
    }

    private static string GetPublicConcernSemanticKey(
        SocialSignalCategory category,
        string topicKey,
        string groupKey)
    {
        if (category == SocialSignalCategory.Litter ||
            IsStreetLitterPublicConcernTopic(topicKey) ||
            IsStreetLitterPublicConcernTopic(groupKey))
        {
            return "public_concern:litter:street_litter";
        }

        return string.IsNullOrWhiteSpace(groupKey)
            ? $"public_concern:{category}:{NormalizeSocialSignalTopicKey(topicKey, null, category)}"
            : groupKey;
    }

    private CityComplaint FindActivePublicConcernBySemanticKey(string semanticKey)
    {
        if (string.IsNullOrWhiteSpace(semanticKey))
        {
            return null;
        }

        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (IsCityComplaintActive(complaint) &&
                complaint.Category == CityComplaintCategory.PublicConcern &&
                string.Equals(GetPublicConcernSemanticKey(complaint), semanticKey, System.StringComparison.Ordinal))
            {
                return complaint;
            }
        }

        return null;
    }

    private bool TryMergePublicConcernClusterIntoActiveComplaint(CityComplaintSocialSignalCluster cluster)
    {
        CityComplaint active = FindActivePublicConcernBySemanticKey(GetPublicConcernSemanticKey(cluster));
        if (active == null)
        {
            return false;
        }

        int beforeSignerCount = active.SignerIds.Count;
        NormalizePublicConcernComplaintCanonicalTopic(active);
        active.Severity = Mathf.Max(active.Severity, CalculatePublicConcernCityComplaintSeverity(cluster));
        active.IssueSourceStrength = Mathf.Max(active.IssueSourceStrength, GetCityComplaintSocialClusterNetStrength(cluster));
        MergePublicConcernSigners(active, cluster.SignerIds, cluster.SignerNames);
        if (active.SignerIds.Count != beforeSignerCount)
        {
            SessionDebugLogger.Log(
                "CITY_HALL",
                $"Merged public concern cluster into active request #{active.Id}: key={active.GroupKey}, signers={active.SignerIds.Count}, semantic={GetPublicConcernSemanticKey(active)}.");
            isCityHallScreenDirty = true;
        }

        return true;
    }

    private static bool ShouldReplacePublicConcernDuplicateKeeper(CityComplaint keeper, CityComplaint candidate)
    {
        if (keeper == null) return true;
        if (candidate == null) return false;
        if (candidate.State == CityComplaintState.Accepted && keeper.State != CityComplaintState.Accepted) return true;
        if (keeper.State == CityComplaintState.Accepted && candidate.State != CityComplaintState.Accepted) return false;
        bool candidateCanonical = string.Equals(candidate.GroupKey, "social_cluster:litter:street_litter", System.StringComparison.Ordinal);
        bool keeperCanonical = string.Equals(keeper.GroupKey, "social_cluster:litter:street_litter", System.StringComparison.Ordinal);
        if (candidateCanonical != keeperCanonical) return candidateCanonical;
        int signerCompare = candidate.SignerIds.Count.CompareTo(keeper.SignerIds.Count);
        if (signerCompare != 0) return signerCompare > 0;
        int severityCompare = candidate.Severity.CompareTo(keeper.Severity);
        if (severityCompare != 0) return severityCompare > 0;
        return candidate.CreatedWorldHour < keeper.CreatedWorldHour;
    }

    private void MergePublicConcernComplaintInto(CityComplaint target, CityComplaint source)
    {
        if (target == null || source == null)
        {
            return;
        }

        NormalizePublicConcernComplaintCanonicalTopic(target);
        NormalizePublicConcernComplaintCanonicalTopic(source);
        target.Severity = Mathf.Max(target.Severity, source.Severity);
        target.IssueSourceStrength = Mathf.Max(target.IssueSourceStrength, source.IssueSourceStrength);
        MergePublicConcernSigners(target, source.SignerIds, source.SignerNames);
    }

    private static void MergePublicConcernSigners(
        CityComplaint target,
        List<int> signerIds,
        List<string> signerNames)
    {
        if (target == null || signerIds == null)
        {
            return;
        }

        for (int i = 0; i < signerIds.Count; i++)
        {
            int signerId = signerIds[i];
            if (signerId <= 0 || target.SignerIds.Contains(signerId))
            {
                continue;
            }

            target.SignerIds.Add(signerId);
            target.SignerNames.Add(signerNames != null && i < signerNames.Count && !string.IsNullOrWhiteSpace(signerNames[i]) ? signerNames[i] : $"#{signerId}");
        }
    }

    private static void NormalizePublicConcernComplaintCanonicalTopic(CityComplaint complaint)
    {
        if (complaint == null ||
            complaint.Category != CityComplaintCategory.PublicConcern ||
            !string.Equals(GetPublicConcernSemanticKey(complaint), "public_concern:litter:street_litter", System.StringComparison.Ordinal))
        {
            return;
        }

        complaint.GroupKey = "social_cluster:litter:street_litter";
        complaint.IssueTopicKey = "street_litter";
        complaint.IssueSignalCategory = SocialSignalCategory.Litter;
        complaint.IssueTitleRu = "\u043c\u0443\u0441\u043e\u0440 \u043d\u0430 \u0443\u043b\u0438\u0446\u0430\u0445";
        complaint.IssueTitleEn = "street litter";
    }

    private void ResolveDuplicatePublicConcern(CityComplaint duplicate, CityComplaint keeper, string semanticKey)
    {
        if (!IsCityComplaintActive(duplicate))
        {
            return;
        }

        float now = GetCurrentWorldHour();
        duplicate.State = CityComplaintState.Resolved;
        duplicate.ResolvedWorldHour = now;
        duplicate.ResolvedDay = currentDay;
        duplicate.ResolveReason = "duplicate public concern";
        duplicate.ManuallyResolved = true;
        duplicate.IsUnread = false;
        cityComplaintCooldownByKey[GetCityComplaintCooldownKey(duplicate)] = now + CityComplaintCooldownWorldHours;
        SessionDebugLogger.Log(
            "CITY_HALL",
            $"Resolved duplicate public concern #{duplicate.Id}; kept #{keeper?.Id ?? 0}, semantic={semanticKey}.");
    }
}
