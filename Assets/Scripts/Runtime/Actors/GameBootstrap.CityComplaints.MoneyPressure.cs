using System.Collections.Generic;

public partial class GameBootstrap
{
    private bool IsPublicConcernBlockedByWorkIncomePath(
        CityComplaintSocialSignalCluster cluster,
        out int incomePathSigners,
        out int activeSigners,
        out int availableWork,
        out int unassignedWorkers)
    {
        incomePathSigners = 0;
        activeSigners = 0;
        availableWork = 0;
        unassignedWorkers = 0;

        return cluster != null &&
               cluster.Category == SocialSignalCategory.Money &&
               IsMoneyPressurePublicConcernTopic(cluster.TopicKey, cluster.GroupKey) &&
               AreMoneyPressureSignersCoveredByWorkIncomePath(
                   cluster.SignerIds,
                   fallbackWorkerId: 0,
                   out incomePathSigners,
                   out activeSigners,
                   out availableWork,
                   out unassignedWorkers);
    }

    private bool IsPublicConcernBlockedByWorkIncomePath(
        CityComplaint complaint,
        out int incomePathSigners,
        out int activeSigners,
        out int availableWork,
        out int unassignedWorkers)
    {
        incomePathSigners = 0;
        activeSigners = 0;
        availableWork = 0;
        unassignedWorkers = 0;

        return complaint != null &&
               complaint.IssueSignalCategory == SocialSignalCategory.Money &&
               IsMoneyPressurePublicConcernTopic(complaint.IssueTopicKey, complaint.GroupKey) &&
               AreMoneyPressureSignersCoveredByWorkIncomePath(
                   complaint.SignerIds,
                   complaint.WorkerId,
                   out incomePathSigners,
                   out activeSigners,
                   out availableWork,
                   out unassignedWorkers);
    }

    private bool AreMoneyPressureSignersCoveredByWorkIncomePath(
        List<int> signerIds,
        int fallbackWorkerId,
        out int incomePathSigners,
        out int activeSigners,
        out int availableWork,
        out int unassignedWorkers)
    {
        incomePathSigners = 0;
        activeSigners = 0;
        availableWork = 0;
        unassignedWorkers = 0;
        bool usesAvailableWork = false;

        if (signerIds != null)
        {
            for (int i = 0; i < signerIds.Count; i++)
            {
                AddMoneyPressureIncomePathSigner(
                    GetDriverAgentById(signerIds[i]),
                    ref incomePathSigners,
                    ref activeSigners,
                    ref usesAvailableWork);
            }
        }

        if (activeSigners == 0 && fallbackWorkerId > 0)
        {
            AddMoneyPressureIncomePathSigner(
                GetDriverAgentById(fallbackWorkerId),
                ref incomePathSigners,
                ref activeSigners,
                ref usesAvailableWork);
        }

        if (activeSigners <= 0 || incomePathSigners < activeSigners)
        {
            return false;
        }

        return !usesAvailableWork ||
               HasSufficientAvailableWorkForUnassignedWorkers(out availableWork, out unassignedWorkers);
    }

    private void AddMoneyPressureIncomePathSigner(
        DriverAgent worker,
        ref int incomePathSigners,
        ref int activeSigners,
        ref bool usesAvailableWork)
    {
        if (worker == null || worker.IsLeavingTown || worker.HasDepartedTown)
        {
            return;
        }

        activeSigners++;
        if (IsWorkerEmployedForMigration(worker) && !IsWorkerUnemployedForThoughts(worker))
        {
            incomePathSigners++;
            return;
        }

        if (worker.ReservedLaborExchangePostingId > 0)
        {
            incomePathSigners++;
            return;
        }

        if (HasAvailableWorkOpportunityForWorker(worker, requireImmediateAvailability: false))
        {
            incomePathSigners++;
            usesAvailableWork = true;
        }
    }

    private static bool IsMoneyPressurePublicConcernTopic(string topicKey, string groupKey)
    {
        return IsMoneyPressurePublicConcernTopic(topicKey) ||
               IsMoneyPressurePublicConcernTopic(groupKey);
    }

    private static bool IsMoneyPressurePublicConcernTopic(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        string normalized = NormalizeWorkerKnowledgeTopicKey(key);
        return normalized == "TEXT:MONEY" ||
               normalized.EndsWith(":TEXT:MONEY", System.StringComparison.Ordinal);
    }
}
