using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float CityComplaintGroupMinAgeWorldHours = 4f;
    private const float CityComplaintCriticalGroupMinAgeWorldHours = 2f;
    private const float CityComplaintSingleSignerMinAgeWorldHours = 10f;
    private const float CityComplaintPendingForgetWorldHours = 8f;
    private const float CityComplaintDueWorldHours = 24f;

    private sealed class CityComplaintPendingGroup
    {
        public string GroupKey = string.Empty;
        public CityComplaintCategory Category;
        public WorkerNeedKind? LinkedNeed;
        public LocationType? LinkedLocationType;
        public int Severity;
        public float FirstObservedWorldHour;
        public float LastObservedWorldHour;
        public readonly List<int> SignerIds = new();
        public readonly List<string> SignerNames = new();
    }

    private readonly Dictionary<string, CityComplaintPendingGroup> cityComplaintPendingGroups = new();

    private bool TryRecordCityComplaintSignal(
        DriverAgent worker,
        CityComplaintCategory category,
        int severity,
        WorkerNeedKind? linkedNeed,
        LocationType? linkedLocationType)
    {
        if (worker == null || worker.DriverId <= 0)
        {
            return false;
        }

        string groupKey = GetCityComplaintGroupKey(category, linkedNeed, linkedLocationType);
        CityComplaint openComplaint = FindActiveCityComplaintByGroupKey(groupKey);
        if (openComplaint != null)
        {
            bool changed = AddCityComplaintSigner(openComplaint, worker);
            int clampedSeverity = Mathf.Clamp(severity, 1, 4);
            if (clampedSeverity > openComplaint.Severity)
            {
                openComplaint.Severity = clampedSeverity;
                changed = true;
            }

            return changed;
        }

        float now = GetCurrentWorldHour();
        if (cityComplaintCooldownByKey.TryGetValue(groupKey, out float nextAllowedWorldHour) && now < nextAllowedWorldHour)
        {
            return false;
        }

        if (!cityComplaintPendingGroups.TryGetValue(groupKey, out CityComplaintPendingGroup group))
        {
            group = new CityComplaintPendingGroup
            {
                GroupKey = groupKey,
                Category = category,
                LinkedNeed = linkedNeed,
                LinkedLocationType = linkedLocationType,
                Severity = Mathf.Clamp(severity, 1, 4),
                FirstObservedWorldHour = now,
                LastObservedWorldHour = now
            };
            cityComplaintPendingGroups[groupKey] = group;
        }

        group.LastObservedWorldHour = now;
        group.Severity = Mathf.Max(group.Severity, Mathf.Clamp(severity, 1, 4));
        AddCityComplaintPendingSigner(group, worker);
        if (!IsCityComplaintPendingGroupMature(group, now))
        {
            return false;
        }

        CityComplaint complaint = CreateCityComplaintFromPendingGroup(group, now);
        cityComplaints.Add(complaint);
        cityComplaintPendingGroups.Remove(groupKey);
        cityComplaintCooldownByKey[groupKey] = now + CityComplaintCooldownWorldHours;

        SessionDebugLogger.Log(
            "CITY_HALL",
            $"Grouped complaint #{complaint.Id} filed: key={groupKey}, signers={complaint.SignerIds.Count}, severity={complaint.Severity}, due={complaint.DueWorldHour:0.0}.");
        PushFeedEvent(
            $"City Hall received a grouped complaint from {complaint.SignerIds.Count} citizens.",
            $"\u0412 \u0440\u0430\u0442\u0443\u0448\u0443 \u043f\u043e\u0434\u0430\u043b\u0438 \u043a\u043e\u043b\u043b\u0435\u043a\u0442\u0438\u0432\u043d\u0443\u044e \u0436\u0430\u043b\u043e\u0431\u0443: {complaint.SignerIds.Count} \u0436\u0438\u0442.",
            complaint.Severity >= 4 ? FeedEventType.Warning : FeedEventType.Info);
        return true;
    }

    private bool IsCityComplaintPendingGroupMature(CityComplaintPendingGroup group, float now)
    {
        if (group == null)
        {
            return false;
        }

        float age = now - group.FirstObservedWorldHour;
        int signerCount = group.SignerIds.Count;
        if (signerCount >= 3 && age >= CityComplaintCriticalGroupMinAgeWorldHours)
        {
            return true;
        }

        if (signerCount >= 2)
        {
            float requiredAge = group.Severity >= 4
                ? CityComplaintCriticalGroupMinAgeWorldHours
                : CityComplaintGroupMinAgeWorldHours;
            return age >= requiredAge;
        }

        return signerCount == 1 && age >= CityComplaintSingleSignerMinAgeWorldHours;
    }

    private CityComplaint CreateCityComplaintFromPendingGroup(CityComplaintPendingGroup group, float now)
    {
        int primaryWorkerId = group.SignerIds.Count > 0 ? group.SignerIds[0] : 0;
        string primaryName = group.SignerNames.Count > 0
            ? group.SignerNames[0]
            : (IsRussianLanguage() ? "\u0416\u0438\u0442\u0435\u043b\u0438" : "Citizens");

        CityComplaint complaint = new()
        {
            Id = nextCityComplaintId++,
            WorkerId = primaryWorkerId,
            WorkerName = primaryName,
            GroupKey = group.GroupKey,
            Category = group.Category,
            State = CityComplaintState.Open,
            Severity = Mathf.Clamp(group.Severity, 1, 4),
            LinkedNeed = group.LinkedNeed,
            LinkedLocationType = group.LinkedLocationType,
            CreatedWorldHour = now,
            CreatedDay = currentDay,
            DueWorldHour = now + CityComplaintDueWorldHours
        };

        complaint.SignerIds.AddRange(group.SignerIds);
        complaint.SignerNames.AddRange(group.SignerNames);
        return complaint;
    }

    private void AddCityComplaintPendingSigner(CityComplaintPendingGroup group, DriverAgent worker)
    {
        if (group == null || worker == null || worker.DriverId <= 0 || group.SignerIds.Contains(worker.DriverId))
        {
            return;
        }

        group.SignerIds.Add(worker.DriverId);
        group.SignerNames.Add(string.IsNullOrWhiteSpace(worker.DriverName) ? $"#{worker.DriverId}" : worker.DriverName);
    }

    private bool AddCityComplaintSigner(CityComplaint complaint, DriverAgent worker)
    {
        if (complaint == null || worker == null || worker.DriverId <= 0 || complaint.SignerIds.Contains(worker.DriverId))
        {
            return false;
        }

        complaint.SignerIds.Add(worker.DriverId);
        complaint.SignerNames.Add(string.IsNullOrWhiteSpace(worker.DriverName) ? $"#{worker.DriverId}" : worker.DriverName);
        if (complaint.WorkerId <= 0)
        {
            complaint.WorkerId = worker.DriverId;
            complaint.WorkerName = worker.DriverName;
        }

        SessionDebugLogger.Log("CITY_HALL", $"{worker.DriverName} joined grouped complaint #{complaint.Id}: key={complaint.GroupKey}.");
        return true;
    }

    private void PruneStaleCityComplaintPendingGroups()
    {
        if (cityComplaintPendingGroups.Count == 0)
        {
            return;
        }

        float now = GetCurrentWorldHour();
        List<string> staleKeys = new();
        foreach (KeyValuePair<string, CityComplaintPendingGroup> pair in cityComplaintPendingGroups)
        {
            CityComplaintPendingGroup group = pair.Value;
            if (group == null || now - group.LastObservedWorldHour >= CityComplaintPendingForgetWorldHours)
            {
                staleKeys.Add(pair.Key);
            }
        }

        for (int i = 0; i < staleKeys.Count; i++)
        {
            cityComplaintPendingGroups.Remove(staleKeys[i]);
        }
    }

    private void ExpireOverdueCityComplaints()
    {
        float now = GetCurrentWorldHour();
        bool changed = false;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (complaint == null ||
                complaint.State != CityComplaintState.Accepted ||
                complaint.DueWorldHour <= 0f ||
                now < complaint.DueWorldHour)
            {
                continue;
            }

            complaint.State = CityComplaintState.Expired;
            complaint.ResolvedWorldHour = now;
            complaint.ResolvedDay = currentDay;
            complaint.ResolveReason = "deadline expired";
            complaint.IsUnread = false;
            cityComplaintCooldownByKey[GetCityComplaintCooldownKey(complaint)] =
                now + CityComplaintCooldownWorldHours;
            if (!complaint.TrustPenaltyApplied)
            {
                complaint.TrustPenaltyApplied = true;
                ApplyCityTrustDelta(CityTrustComplaintExpiredPenalty, $"citizen request #{complaint.Id} expired");
            }

            StartCityRequestGoalFeedback(success: false, complaint);
            changed = true;
            SessionDebugLogger.Log("CITY_HALL", $"Citizen request #{complaint.Id} expired after 24h: key={complaint.GroupKey}.");
            PushFeedEvent(
                "A City Hall request expired.",
                "Обращение в Ратуше просрочено.",
                FeedEventType.Warning);
        }

        if (changed)
        {
            isCityHallScreenDirty = true;
        }
    }

    private bool DoesCityComplaintConditionRemainForAnySigner(CityComplaint complaint, out string reason)
    {
        reason = string.Empty;
        if (complaint == null)
        {
            reason = "complaint missing";
            return false;
        }

        if (complaint.Category == CityComplaintCategory.ServiceMissing)
        {
            bool targetMissing = complaint.LinkedLocationType.HasValue && !locations.ContainsKey(complaint.LinkedLocationType.Value);
            reason = targetMissing ? string.Empty : "requested service exists";
            return targetMissing;
        }

        if (complaint.Category == CityComplaintCategory.SocialIntroduction)
        {
            return DoesCitySocialIntroductionConditionRemain(complaint, out reason);
        }

        bool sawAvailableSigner = false;
        for (int i = 0; i < complaint.SignerIds.Count; i++)
        {
            DriverAgent worker = GetDriverAgentById(complaint.SignerIds[i]);
            if (worker == null || worker.HasDepartedTown || worker.IsLeavingTown)
            {
                continue;
            }

            sawAvailableSigner = true;
            if (IsCityComplaintConditionActiveForWorker(complaint, worker))
            {
                return true;
            }
        }

        reason = sawAvailableSigner ? "group issue cleared" : "signers unavailable";
        return false;
    }

    private bool IsCityComplaintConditionActiveForWorker(CityComplaint complaint, DriverAgent worker)
    {
        if (complaint == null || worker == null)
        {
            return false;
        }

        return complaint.Category switch
        {
            CityComplaintCategory.NeedPressure => complaint.LinkedNeed.HasValue &&
                                                  GetWorkerNeedLastStatus(worker, complaint.LinkedNeed.Value) == WorkerNeedStatus.Critical,
            CityComplaintCategory.NoJob => IsWorkerVacantForVacancyAssignment(worker),
            CityComplaintCategory.LowMoney => worker.Money < 15,
            CityComplaintCategory.FamilyStress => worker.FamilyId > 0 &&
                                                  worker.Satisfaction < 60 &&
                                                  HasCriticalWorkerNeed(worker),
            CityComplaintCategory.SocialIntroduction => DoesCitySocialIntroductionConditionRemain(complaint, out _),
            _ => false
        };
    }

    private void ApplyCityComplaintSatisfactionDelta(CityComplaint complaint, int delta)
    {
        if (complaint == null || delta == 0)
        {
            return;
        }

        if (complaint.SignerIds.Count == 0)
        {
            DriverAgent worker = GetDriverAgentById(complaint.WorkerId);
            if (worker != null)
            {
                worker.Satisfaction = Mathf.Clamp(worker.Satisfaction + delta, 0, 100);
            }

            return;
        }

        for (int i = 0; i < complaint.SignerIds.Count; i++)
        {
            DriverAgent worker = GetDriverAgentById(complaint.SignerIds[i]);
            if (worker != null)
            {
                worker.Satisfaction = Mathf.Clamp(worker.Satisfaction + delta, 0, 100);
            }
        }
    }

    private CityComplaint FindActiveCityComplaintByGroupKey(string groupKey)
    {
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (complaint != null &&
                IsCityComplaintActive(complaint) &&
                string.Equals(complaint.GroupKey, groupKey, System.StringComparison.Ordinal))
            {
                return complaint;
            }
        }

        return null;
    }

    private static string GetCityComplaintGroupKey(CityComplaintCategory category, WorkerNeedKind? need, LocationType? target)
    {
        return $"{category}:{(need.HasValue ? need.Value.ToString() : "none")}:{(target.HasValue ? target.Value.ToString() : "none")}";
    }

    private static string GetCityComplaintCooldownKey(CityComplaint complaint)
    {
        if (!string.IsNullOrWhiteSpace(complaint?.GroupKey))
        {
            return complaint.GroupKey;
        }

        return complaint == null
            ? "unknown"
            : GetCityComplaintGroupKey(complaint.Category, complaint.LinkedNeed, complaint.LinkedLocationType);
    }

    private static int GetCityComplaintDisplayStateRank(CityComplaintState state)
    {
        return state switch
        {
            CityComplaintState.Open => 0,
            CityComplaintState.Accepted => 1,
            CityComplaintState.Expired => 2,
            CityComplaintState.Rejected => 3,
            _ => 4
        };
    }

    private static bool IsCityComplaintActive(CityComplaint complaint)
    {
        return complaint != null &&
               (complaint.State == CityComplaintState.Open ||
                complaint.State == CityComplaintState.Accepted);
    }

    private int CountExpiredCityComplaints()
    {
        int count = 0;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            if (cityComplaints[i]?.State == CityComplaintState.Expired)
            {
                count++;
            }
        }

        return count;
    }

    private static int GetCityComplaintSignerCount(CityComplaint complaint)
    {
        if (complaint == null)
        {
            return 0;
        }

        return complaint.SignerIds.Count > 0 ? complaint.SignerIds.Count : 1;
    }

    private static string FormatCityComplaintSignerNames(CityComplaint complaint, bool ru)
    {
        if (complaint == null)
        {
            return ru ? "\u2014" : "-";
        }

        if (complaint.SignerNames.Count == 0)
        {
            return string.IsNullOrWhiteSpace(complaint.WorkerName)
                ? (ru ? "\u0436\u0438\u0442\u0435\u043b\u0438" : "citizens")
                : complaint.WorkerName;
        }

        const int visibleLimit = 5;
        int visibleCount = Mathf.Min(visibleLimit, complaint.SignerNames.Count);
        string[] names = new string[visibleCount];
        for (int i = 0; i < visibleCount; i++)
        {
            names[i] = complaint.SignerNames[i];
        }

        string text = string.Join(", ", names);
        int hidden = complaint.SignerNames.Count - visibleCount;
        if (hidden > 0)
        {
            text += ru ? $" \u0438 \u0435\u0449\u0435 {hidden}" : $" and {hidden} more";
        }

        return text;
    }

    private static string GetCityComplaintIssueTitle(CityComplaint complaint, bool ru)
    {
        if (complaint == null || !complaint.LinkedNeed.HasValue)
        {
            return ru ? "\u041f\u0440\u043e\u0431\u043b\u0435\u043c\u0430 \u0441 \u043d\u0443\u0436\u0434\u0430\u043c\u0438" : "Needs pressure";
        }

        return complaint.LinkedNeed.Value switch
        {
            WorkerNeedKind.Meal => ru ? "\u0413\u043e\u043b\u043e\u0434 \u0443 \u0436\u0438\u0442\u0435\u043b\u0435\u0439" : "Hunger pressure",
            WorkerNeedKind.Sleep => ru ? "\u0423\u0441\u0442\u0430\u043b\u043e\u0441\u0442\u044c \u0443 \u0436\u0438\u0442\u0435\u043b\u0435\u0439" : "Fatigue pressure",
            WorkerNeedKind.Leisure => ru ? "\u041d\u0435 \u0445\u0432\u0430\u0442\u0430\u0435\u0442 \u0434\u043e\u0441\u0443\u0433\u0430" : "Leisure pressure",
            _ => ru ? "\u041f\u0440\u043e\u0431\u043b\u0435\u043c\u0430 \u0441 \u043d\u0443\u0436\u0434\u0430\u043c\u0438" : "Needs pressure"
        };
    }

    private static string GetCityComplaintStateLabel(CityComplaintState state, bool ru)
    {
        return state switch
        {
            CityComplaintState.Open => ru ? "ожидает решения" : "pending",
            CityComplaintState.Accepted => ru ? "принято" : "accepted",
            CityComplaintState.Rejected => ru ? "отклонено" : "rejected",
            CityComplaintState.Expired => ru ? "просрочено" : "expired",
            CityComplaintState.Resolved => ru ? "выполнено" : "resolved",
            _ => ru ? "\u2014" : "-"
        };
    }

    private string FormatCityComplaintTimeLeft(CityComplaint complaint, bool ru)
    {
        if (complaint == null || complaint.State != CityComplaintState.Accepted || complaint.DueWorldHour <= 0f)
        {
            return string.Empty;
        }

        float remaining = Mathf.Max(0f, complaint.DueWorldHour - GetCurrentWorldHour());
        int hours = Mathf.CeilToInt(remaining);
        return ru ? $"\u043e\u0441\u0442\u0430\u043b\u043e\u0441\u044c {hours}\u0447" : $"{hours}h left";
    }
}
