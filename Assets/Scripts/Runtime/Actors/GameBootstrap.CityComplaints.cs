using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float CityComplaintScanIntervalSeconds = 4.5f;
    private const float CityComplaintCooldownWorldHours = 8f;
    private const int CityComplaintMaxStoredResolved = 24;
    private const float CityServiceRequestInitialDelayWorldHours = 8f;
    private const float CityServiceRequestSpacingWorldHours = 12f;
    private const float CityServiceRequestCooldownWorldHours = 18f;

    private enum CityComplaintCategory
    {
        NeedPressure,
        NoJob,
        LowMoney,
        ServiceMissing,
        FamilyStress,
        SocialIntroduction,
        PublicConcern
    }

    private enum CityComplaintState
    {
        Open,
        Accepted,
        Rejected,
        Expired,
        Resolved
    }

    private sealed class CityServiceRequestCandidate
    {
        public LocationType Target;
        public WorkerNeedKind? LinkedNeed;
        public int Severity;
        public int Weight;
        public int RequiredLocationCount = 1;
    }

    private sealed class CityComplaint
    {
        public int Id;
        public int WorkerId;
        public string WorkerName;
        public string GroupKey = string.Empty;
        public CityComplaintCategory Category;
        public CityComplaintState State;
        public int Severity;
        public WorkerNeedKind? LinkedNeed;
        public LocationType? LinkedLocationType;
        public int RequiredLocationCount = 1;
        public float CreatedWorldHour;
        public int CreatedDay;
        public float DueWorldHour;
        public float ResolvedWorldHour;
        public int ResolvedDay;
        public string ResolveReason = string.Empty;
        public bool ManuallyResolved;
        public float AcceptedWorldHour;
        public float RejectedWorldHour;
        public bool TrustPenaltyApplied;
        public bool IsUnread;
        public int SocialTargetWorkerId;
        public string SocialTargetWorkerName = string.Empty;
        public string IssueTopicKey = string.Empty;
        public string IssueTitleRu = string.Empty;
        public string IssueTitleEn = string.Empty;
        public string IssueReasonRu = string.Empty;
        public string IssueReasonEn = string.Empty;
        public SocialSignalCategory IssueSignalCategory = SocialSignalCategory.City;
        public int IssueSourceDay;
        public int IssueSourceStrength;
        public readonly List<int> SignerIds = new();
        public readonly List<string> SignerNames = new();
    }

    private sealed class CityComplaintRowViewModel
    {
        public int Id;
        public string WorkerName;
        public CityComplaintCategory Category;
        public CityComplaintState State;
        public int Severity;
        public string Title;
        public string Detail;
        public string Meta;
        public Color AccentColor;
    }

    private readonly List<CityComplaint> cityComplaints = new();
    private readonly Dictionary<string, float> cityComplaintCooldownByKey = new();
    private int nextCityComplaintId = 1;
    private float cityComplaintScanTimer;
    private bool wasCityHallBuiltLastTick;
    private float cityHallRuntimeStartedWorldHour = -1f;
    private float nextCityServiceRequestAllowedWorldHour;

    private void UpdateCityHallRuntime()
    {
        bool cityHallBuilt = locations.ContainsKey(LocationType.CityHall);
        if (cityHallBuilt != wasCityHallBuiltLastTick)
        {
            wasCityHallBuiltLastTick = cityHallBuilt;
            isCityHallScreenDirty = true;
            if (cityHallBuilt)
            {
                cityHallRuntimeStartedWorldHour = GetCurrentWorldHour();
                nextCityServiceRequestAllowedWorldHour = cityHallRuntimeStartedWorldHour + CityServiceRequestInitialDelayWorldHours;
                SessionDebugLogger.Log("CITY_HALL", "City Hall runtime enabled.");
            }
        }

        if (!cityHallBuilt)
        {
            return;
        }

        ResolveTemporarilyDisabledCityComplaints();

        cityComplaintScanTimer -= Time.deltaTime * Mathf.Max(0f, gameSpeedMultiplier);
        if (cityComplaintScanTimer > 0f)
        {
            return;
        }

        cityComplaintScanTimer = CityComplaintScanIntervalSeconds;
        ScanCityComplaints();
        UpdateCitySocialIntroductionRequests();
        ExpireOverdueCityComplaints();
        ResolveSatisfiedCityComplaints();
        PruneResolvedCityComplaints();
    }

    private void ScanCityComplaints()
    {
        int createdThisScan = 0;
        if (IsCityComplaintCategoryTemporarilyEnabled(CityComplaintCategory.ServiceMissing))
        {
            TryCreateMissingServiceBuildingRequest(ref createdThisScan);
        }

        PruneStaleCityComplaintPendingGroups();

        if (createdThisScan > 0)
        {
            isCityHallScreenDirty = true;
        }
    }

    private void TryCreateMissingServiceBuildingRequest(ref int createdThisScan)
    {
        if (!IsCityComplaintCategoryTemporarilyEnabled(CityComplaintCategory.ServiceMissing))
        {
            return;
        }

        if (cityHallRuntimeStartedWorldHour < 0f)
        {
            cityHallRuntimeStartedWorldHour = GetCurrentWorldHour();
        }

        float now = GetCurrentWorldHour();
        if (now < nextCityServiceRequestAllowedWorldHour ||
            now - cityHallRuntimeStartedWorldHour < CityServiceRequestInitialDelayWorldHours)
        {
            return;
        }

        List<CityServiceRequestCandidate> candidates = BuildMissingServiceRequestCandidates();
        if (candidates.Count == 0 || !TryPickRandomCityRequestSigner(out DriverAgent signer))
        {
            return;
        }

        int totalWeight = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            totalWeight += Mathf.Max(1, candidates[i].Weight);
        }

        int roll = Random.Range(0, Mathf.Max(1, totalWeight));
        CityServiceRequestCandidate selected = candidates[0];
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= Mathf.Max(1, candidates[i].Weight);
            if (roll < 0)
            {
                selected = candidates[i];
                break;
            }
        }

        if (CreateServiceBuildingRequest(signer, selected))
        {
            createdThisScan++;
            nextCityServiceRequestAllowedWorldHour = now + CityServiceRequestSpacingWorldHours;
        }
    }

    private List<CityServiceRequestCandidate> BuildMissingServiceRequestCandidates()
    {
        return BuildCityConstructionRequestCandidates();
    }

    private bool TryPickRandomCityRequestSigner(out DriverAgent signer)
    {
        signer = null;
        List<DriverAgent> candidates = new();
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (CanWorkerFileCityComplaint(worker))
            {
                candidates.Add(worker);
            }
        }

        if (candidates.Count == 0)
        {
            return false;
        }

        signer = candidates[Random.Range(0, candidates.Count)];
        return signer != null;
    }

    private bool CreateServiceBuildingRequest(DriverAgent signer, CityServiceRequestCandidate candidate)
    {
        if (signer == null ||
            candidate == null ||
            !IsCityComplaintCategoryTemporarilyEnabled(CityComplaintCategory.ServiceMissing))
        {
            return false;
        }

        float now = GetCurrentWorldHour();
        string groupKey = GetCityComplaintGroupKey(CityComplaintCategory.ServiceMissing, candidate.LinkedNeed, candidate.Target);
        CityComplaint complaint = new()
        {
            Id = nextCityComplaintId++,
            WorkerId = signer.DriverId,
            WorkerName = string.IsNullOrWhiteSpace(signer.DriverName) ? $"#{signer.DriverId}" : signer.DriverName,
            GroupKey = groupKey,
            Category = CityComplaintCategory.ServiceMissing,
            State = CityComplaintState.Open,
            Severity = Mathf.Clamp(candidate.Severity, 1, 4),
            LinkedNeed = candidate.LinkedNeed,
            LinkedLocationType = candidate.Target,
            RequiredLocationCount = Mathf.Max(1, candidate.RequiredLocationCount),
            CreatedWorldHour = now,
            CreatedDay = currentDay,
            DueWorldHour = 0f,
            IsUnread = true
        };
        complaint.SignerIds.Add(signer.DriverId);
        complaint.SignerNames.Add(complaint.WorkerName);

        cityComplaints.Add(complaint);
        cityComplaintCooldownByKey[groupKey] = now + CityServiceRequestCooldownWorldHours;
        NotifyCityHallNewRequest(complaint);
        RecordWorkerBuildingKnowledge(signer, LocationType.CityHall, "\u041e\u0431\u0440\u0430\u0442\u0438\u043b\u0441\u044f \u0432 \u0440\u0430\u0442\u0443\u0448\u0443", "Filed a request at City Hall");
        RecordCityComplaintSocialSignals(
            complaint,
            SocialSignalSourceKind.CityComplaint,
            SocialSignalTone.Negative,
            Mathf.Clamp(complaint.Severity * 16, 16, 76),
            56,
            "filed",
            "\u0436\u0438\u0442\u0435\u043b\u044c \u0434\u043e\u043d\u0435\u0441 \u043f\u0440\u043e\u0431\u043b\u0435\u043c\u0443 \u0434\u043e \u0440\u0430\u0442\u0443\u0448\u0438",
            "a citizen brought a problem to City Hall",
            includeInDailyExperience: true);
        SessionDebugLogger.Log(
            "CITY_HALL",
            $"Citizen request #{complaint.Id} filed: target={candidate.Target}, required={complaint.RequiredLocationCount}, signer={complaint.WorkerName}, severity={complaint.Severity}.");
        return true;
    }

    private bool CanWorkerFileCityComplaint(DriverAgent worker)
    {
        return worker != null &&
               worker.DriverId > 0 &&
               !worker.IsArrivingByBus &&
               !worker.IsLeavingTown &&
               !worker.HasDepartedTown;
    }

    private void TryCreateNeedComplaint(DriverAgent worker, WorkerNeedKind need, ref int createdThisScan)
    {
        WorkerNeedStatus status = GetWorkerNeedLastStatus(worker, need);
        if (status != WorkerNeedStatus.Critical)
        {
            return;
        }

        TryCreateCityComplaint(worker, CityComplaintCategory.NeedPressure, 4, need, null, ref createdThisScan);
    }

    private void TryCreateLowMoneyComplaint(DriverAgent worker, ref int createdThisScan)
    {
        if (worker.Money > 4 || !HasCriticalWorkerNeed(worker))
        {
            return;
        }

        TryCreateCityComplaint(worker, CityComplaintCategory.LowMoney, 3, null, null, ref createdThisScan);
    }

    private void TryCreateNoJobComplaint(DriverAgent worker, ref int createdThisScan)
    {
        if (currentDay < 2 ||
            !IsWorkerVacantForVacancyAssignment(worker) ||
            HasAvailableWorkOpportunityForWorker(worker))
        {
            return;
        }

        TryCreateCityComplaint(worker, CityComplaintCategory.NoJob, 2, null, LocationType.LaborExchange, ref createdThisScan);
    }

    private void TryCreateServiceMissingComplaint(DriverAgent worker, ref int createdThisScan)
    {
        if (worker.LastMealNeedStatus == WorkerNeedStatus.Critical &&
            !locations.ContainsKey(LocationType.Canteen) &&
            !locations.ContainsKey(LocationType.Kiosk))
        {
            TryCreateCityComplaint(worker, CityComplaintCategory.ServiceMissing, 3, WorkerNeedKind.Meal, LocationType.Canteen, ref createdThisScan);
        }

        if (worker.LastSleepNeedStatus == WorkerNeedStatus.Critical && !locations.ContainsKey(LocationType.Motel))
        {
            TryCreateCityComplaint(worker, CityComplaintCategory.ServiceMissing, 3, WorkerNeedKind.Sleep, LocationType.Motel, ref createdThisScan);
        }

        if (worker.LastLeisureNeedStatus == WorkerNeedStatus.Critical &&
            !locations.ContainsKey(LocationType.Bar) &&
            !locations.ContainsKey(LocationType.GamblingHall) &&
            !locations.ContainsKey(LocationType.CityPark))
        {
            TryCreateCityComplaint(worker, CityComplaintCategory.ServiceMissing, 2, WorkerNeedKind.Leisure, LocationType.Bar, ref createdThisScan);
        }
    }

    private void TryCreateFamilyStressComplaint(DriverAgent worker, ref int createdThisScan)
    {
        WorkerFamily family = GetWorkerFamilyById(worker?.FamilyId ?? -1);
        if (family == null)
        {
            return;
        }

        int childPressure = GetWorkerFamilyChildPressure(family);
        bool parentPressure = worker.Satisfaction < 35 && HasCriticalWorkerNeed(worker);
        if (!parentPressure && childPressure <= 0)
        {
            return;
        }

        int severity = Mathf.Clamp(parentPressure ? 3 + childPressure / 4 : 2 + childPressure / 3, 2, 4);
        LocationType linkedLocation = GetWorkerFamilyMostNeededEducationLocation(family) ?? LocationType.Kindergarten;
        TryCreateCityComplaint(worker, CityComplaintCategory.FamilyStress, severity, null, linkedLocation, ref createdThisScan);
    }

    private void TryCreateCityComplaint(
        DriverAgent worker,
        CityComplaintCategory category,
        int severity,
        WorkerNeedKind? linkedNeed,
        LocationType? linkedLocationType,
        ref int createdThisScan)
    {
        if (TryRecordCityComplaintSignal(worker, category, Mathf.Clamp(severity, 1, 4), linkedNeed, linkedLocationType))
        {
            createdThisScan++;
        }
    }

    private void ResolveSatisfiedCityComplaints()
    {
        bool changed = ResolveDuplicatePublicConcernCityComplaints();
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (!IsCityComplaintActive(complaint))
            {
                continue;
            }

            DriverAgent worker = GetDriverAgentById(complaint.WorkerId);
            if (ShouldResolveCityComplaint(complaint, worker, out string reason))
            {
                ResolveCityComplaint(complaint, reason, manually: false);
                changed = true;
            }
        }

        if (changed)
        {
            isCityHallScreenDirty = true;
        }
    }

    private void NotifyCityComplaintServiceBuilt(LocationType locationType)
    {
        if (cityComplaints.Count == 0)
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (!IsCityComplaintActive(complaint) ||
                complaint.Category != CityComplaintCategory.ServiceMissing ||
                !complaint.LinkedLocationType.HasValue ||
                complaint.LinkedLocationType.Value != locationType)
            {
                continue;
            }

            if (IsCityConstructionRequestSatisfied(complaint))
            {
                ResolveCityComplaint(
                    complaint,
                    "requested service exists",
                    manually: false);
                changed = true;
            }
        }

        if (changed)
        {
            isCityHallScreenDirty = true;
            SessionDebugLogger.Log("CITY_HALL", $"Resolved service request after building {locationType}.");
        }
    }

    private bool ShouldResolveCityComplaint(CityComplaint complaint, DriverAgent worker, out string reason)
    {
        reason = string.Empty;
        if (complaint != null && complaint.SignerIds.Count > 0)
        {
            if (!DoesCityComplaintConditionRemainForAnySigner(complaint, out reason))
            {
                if (string.IsNullOrEmpty(reason))
                {
                    reason = "group issue cleared";
                }

                return true;
            }

            return false;
        }

        if (worker == null || worker.HasDepartedTown || worker.IsLeavingTown)
        {
            reason = "worker unavailable";
            return true;
        }

        switch (complaint.Category)
        {
            case CityComplaintCategory.NeedPressure:
                if (complaint.LinkedNeed.HasValue && GetWorkerNeedLastStatus(worker, complaint.LinkedNeed.Value) != WorkerNeedStatus.Critical)
                {
                    reason = "need no longer critical";
                    return true;
                }
                break;
            case CityComplaintCategory.NoJob:
                bool hasAvailableWork = HasAvailableWorkOpportunityForWorker(worker);
                if (!IsWorkerVacantForVacancyAssignment(worker) || hasAvailableWork)
                {
                    reason = hasAvailableWork ? "work is available" : "worker found an assignment";
                    return true;
                }
                break;
            case CityComplaintCategory.LowMoney:
                if (worker.Money >= 15)
                {
                    reason = "worker recovered money";
                    return true;
                }
                break;
            case CityComplaintCategory.ServiceMissing:
                if (IsCityConstructionRequestSatisfied(complaint))
                {
                    reason = "requested service exists";
                    return true;
                }
                break;
            case CityComplaintCategory.FamilyStress:
                WorkerFamily family = GetWorkerFamilyById(worker.FamilyId);
                if ((worker.Satisfaction >= 60 || !HasCriticalWorkerNeed(worker)) &&
                    GetWorkerFamilyChildPressure(family) <= 0)
                {
                    reason = "family pressure eased";
                    return true;
                }
                break;
            case CityComplaintCategory.SocialIntroduction:
                if (!DoesCitySocialIntroductionConditionRemain(complaint, out reason))
                {
                    return true;
                }
                break;
            case CityComplaintCategory.PublicConcern:
                if (!DoesCityPublicConcernConditionRemain(complaint, out reason))
                {
                    return true;
                }
                break;
        }

        return false;
    }

    private bool ResolveCityComplaintManually(int complaintId)
    {
        CityComplaint complaint = GetCityComplaintById(complaintId);
        if (!IsCityComplaintActive(complaint))
        {
            return false;
        }

        ResolveCityComplaint(complaint, "reviewed by player", manually: true);
        isCityHallScreenDirty = true;
        return true;
    }

    private bool AcceptCityComplaint(int complaintId)
    {
        CityComplaint complaint = GetCityComplaintById(complaintId);
        if (complaint == null || complaint.State != CityComplaintState.Open)
        {
            return false;
        }

        if (ShouldResolveCityComplaint(complaint, GetDriverAgentById(complaint.WorkerId), out string reason))
        {
            ResolveCityComplaint(complaint, string.IsNullOrWhiteSpace(reason) ? "already satisfied" : reason, manually: false);
            return true;
        }

        float now = GetCurrentWorldHour();
        complaint.State = CityComplaintState.Accepted;
        complaint.AcceptedWorldHour = now;
        complaint.DueWorldHour = complaint.Category == CityComplaintCategory.SocialIntroduction
            ? 0f
            : now + GetCityComplaintDueWorldHours();
        complaint.IsUnread = false;
        isCityHallScreenDirty = true;
        RecordCityComplaintSocialSignals(
            complaint,
            SocialSignalSourceKind.CityHallDecision,
            SocialSignalTone.Positive,
            36,
            62,
            "accepted",
            "\u0433\u043e\u0440\u043e\u0434 \u043f\u0440\u0438\u043d\u044f\u043b \u043e\u0431\u0440\u0430\u0449\u0435\u043d\u0438\u0435 \u043a\u0430\u043a \u043e\u0431\u0435\u0449\u0430\u043d\u0438\u0435",
            "the city accepted the request as a public promise",
            includeInDailyExperience: true);
        if (complaint.Category == CityComplaintCategory.SocialIntroduction)
        {
            PushFeedEvent(
                "Citizen request accepted.",
                $"Обращение принято: помочь разговору {complaint.WorkerName} и {FormatCityComplaintTargetName(complaint)}.",
                FeedEventType.Info);
            SessionDebugLogger.Log("CITY_HALL", $"Citizen social request #{complaint.Id} accepted.");
        }
        else
        {
            if (complaint.Category == CityComplaintCategory.ServiceMissing)
            {
                StartCityRequestGoalFeedback(success: false, complaint, previewOnly: true);
            }

            int dueHours = Mathf.RoundToInt(GetCityComplaintDueWorldHours());
            string acceptedGoal = FormatAcceptedCityComplaintGoalText(complaint, dueHours, IsRussianLanguage());
            PushFeedEvent(
                "Citizen request accepted.",
                acceptedGoal,
                FeedEventType.Info);
            SessionDebugLogger.Log("CITY_HALL", $"Citizen request #{complaint.Id} accepted: due={complaint.DueWorldHour:0.0}.");
        }
        return true;
    }

    private bool RejectCityComplaint(int complaintId)
    {
        CityComplaint complaint = GetCityComplaintById(complaintId);
        if (complaint == null || complaint.State != CityComplaintState.Open)
        {
            return false;
        }

        float now = GetCurrentWorldHour();
        complaint.State = CityComplaintState.Rejected;
        complaint.RejectedWorldHour = now;
        complaint.ResolvedWorldHour = now;
        complaint.ResolvedDay = currentDay;
        complaint.ResolveReason = "rejected by player";
        complaint.IsUnread = false;
        complaint.TrustPenaltyApplied = true;
        cityComplaintCooldownByKey[GetCityComplaintCooldownKey(complaint)] =
            now + CityServiceRequestCooldownWorldHours;

        int rejectedPenalty = ApplyCityTrustRequestRejected(complaint.Id);
        RecordCityComplaintSocialSignals(
            complaint,
            SocialSignalSourceKind.CityHallDecision,
            SocialSignalTone.Negative,
            64,
            86,
            "rejected",
            "\u0433\u043e\u0440\u043e\u0434 \u043e\u0442\u043a\u0430\u0437\u0430\u043b \u0432 \u043e\u0431\u0440\u0430\u0449\u0435\u043d\u0438\u0438",
            "the city rejected the request",
            includeInDailyExperience: true);
        isCityHallScreenDirty = true;
        PushFeedEvent(
            "Citizen request rejected.",
            $"Обращение отклонено: доверие {rejectedPenalty}.",
            FeedEventType.Warning);
        SessionDebugLogger.Log("CITY_HALL", $"Citizen request #{complaint.Id} rejected.");
        return true;
    }

    private void ResolveCityComplaint(CityComplaint complaint, string reason, bool manually)
    {
        if (!IsCityComplaintActive(complaint))
        {
            return;
        }

        bool wasAccepted = complaint.State == CityComplaintState.Accepted;
        complaint.State = CityComplaintState.Resolved;
        complaint.ResolvedWorldHour = GetCurrentWorldHour();
        complaint.ResolvedDay = currentDay;
        complaint.ResolveReason = reason ?? string.Empty;
        complaint.ManuallyResolved = manually;
        complaint.IsUnread = false;
        cityComplaintCooldownByKey[GetCityComplaintCooldownKey(complaint)] =
            complaint.ResolvedWorldHour + CityComplaintCooldownWorldHours;

        ApplyCityComplaintSatisfactionDelta(complaint, manually ? 1 : Mathf.Clamp(complaint.Severity, 1, 4));
        bool completedCityRequest = !manually &&
                                    IsCityComplaintResolveReasonSuccessfulPromise(reason) &&
                                    (wasAccepted || IsAutoCompletedCityServiceRequest(complaint, reason));
        if (completedCityRequest)
        {
            ApplyCityTrustPromiseCompleted(complaint.Id);
            RecordCityComplaintSocialSignals(
                complaint,
                SocialSignalSourceKind.CityHallDecision,
                SocialSignalTone.Positive,
                86,
                94,
                "completed",
                "\u0433\u043e\u0440\u043e\u0434 \u0432\u044b\u043f\u043e\u043b\u043d\u0438\u043b \u0434\u0430\u043d\u043d\u043e\u0435 \u043e\u0431\u0435\u0449\u0430\u043d\u0438\u0435",
                "the city completed the accepted promise",
                includeInDailyExperience: true);
            StartCityRequestGoalFeedback(success: true, complaint);
        }
        else
        {
            RecordCityComplaintSocialSignals(
                complaint,
                SocialSignalSourceKind.CityComplaint,
                SocialSignalTone.Positive,
                manually ? 18 : 42,
                manually ? 42 : 68,
                manually ? "reviewed" : "resolved",
                manually
                    ? "\u043e\u0431\u0440\u0430\u0449\u0435\u043d\u0438\u0435 \u0440\u0430\u0437\u043e\u0431\u0440\u0430\u043d\u043e \u0432\u0440\u0443\u0447\u043d\u0443\u044e"
                    : "\u043f\u0440\u043e\u0431\u043b\u0435\u043c\u0430 \u043e\u0441\u043b\u0430\u0431\u043b\u0430",
                manually ? "the request was reviewed manually" : "the problem eased",
                includeInDailyExperience: !manually);
        }

        SessionDebugLogger.Log("CITY_HALL", $"Complaint #{complaint.Id} resolved: manual={manually}, reason={complaint.ResolveReason}.");
    }

    private bool IsAutoCompletedCityServiceRequest(CityComplaint complaint, string reason)
    {
        return complaint != null &&
               complaint.Category == CityComplaintCategory.ServiceMissing &&
               string.Equals(reason, "requested service exists", System.StringComparison.Ordinal) &&
               IsCityConstructionRequestSatisfied(complaint);
    }

    private void PruneResolvedCityComplaints()
    {
        int resolvedCount = 0;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            if (cityComplaints[i] != null && !IsCityComplaintActive(cityComplaints[i]))
            {
                resolvedCount++;
            }
        }

        if (resolvedCount <= CityComplaintMaxStoredResolved)
        {
            return;
        }

        cityComplaints.Sort(CompareCityComplaintsForStorage);
        for (int i = cityComplaints.Count - 1; i >= 0 && resolvedCount > CityComplaintMaxStoredResolved; i--)
        {
            if (cityComplaints[i] == null || IsCityComplaintActive(cityComplaints[i]))
            {
                continue;
            }

            cityComplaints.RemoveAt(i);
            resolvedCount--;
        }
    }

    private static int CompareCityComplaintsForStorage(CityComplaint a, CityComplaint b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return 1;
        if (b == null) return -1;
        int stateCompare = GetCityComplaintDisplayStateRank(a.State).CompareTo(GetCityComplaintDisplayStateRank(b.State));
        if (stateCompare != 0) return stateCompare;
        return b.CreatedWorldHour.CompareTo(a.CreatedWorldHour);
    }

    private int CountOpenCityComplaints()
    {
        int count = 0;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            if (IsCityComplaintActive(cityComplaints[i]))
            {
                count++;
            }
        }

        return count;
    }

    private int CountCriticalCityComplaints()
    {
        int count = 0;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (IsCityComplaintActive(complaint) && complaint.Severity >= 4)
            {
                count++;
            }
        }

        return count;
    }

    private int CountResolvedCityComplaintsToday()
    {
        int count = 0;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (complaint != null && complaint.State == CityComplaintState.Resolved && complaint.ResolvedDay == currentDay)
            {
                count++;
            }
        }

        return count;
    }

    private int CountOpenCityComplaintsForWorker(int workerId)
    {
        int count = 0;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (IsCityComplaintActive(complaint) && complaint.WorkerId == workerId)
            {
                count++;
            }
        }

        return count;
    }

    private CityComplaint FindOpenCityComplaint(int workerId, CityComplaintCategory category, WorkerNeedKind? need, LocationType? target)
    {
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (complaint != null &&
                IsCityComplaintActive(complaint) &&
                complaint.WorkerId == workerId &&
                complaint.Category == category &&
                complaint.LinkedNeed == need &&
                complaint.LinkedLocationType == target)
            {
                return complaint;
            }
        }

        return null;
    }

    private CityComplaint GetCityComplaintById(int complaintId)
    {
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            if (cityComplaints[i]?.Id == complaintId)
            {
                return cityComplaints[i];
            }
        }

        return null;
    }

    private CityComplaint GetHighestPriorityOpenCityComplaint()
    {
        CityComplaint best = null;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (!IsCityComplaintActive(complaint))
            {
                continue;
            }

            if (best == null ||
                complaint.Severity > best.Severity ||
                complaint.Severity == best.Severity && complaint.CreatedWorldHour > best.CreatedWorldHour)
            {
                best = complaint;
            }
        }

        return best;
    }

    private List<CityComplaintRowViewModel> BuildCityHallComplaintRows(bool ru)
    {
        List<CityComplaint> ordered = new(cityComplaints);
        ordered.Sort(CompareCityComplaintsForDisplay);
        List<CityComplaintRowViewModel> rows = new();
        for (int i = 0; i < ordered.Count; i++)
        {
            CityComplaint complaint = ordered[i];
            if (complaint == null)
            {
                continue;
            }

            if (!IsCityComplaintActive(complaint) && !IsCityHallRejectedComplaintDismissing(complaint))
            {
                continue;
            }

            rows.Add(new CityComplaintRowViewModel
            {
                Id = complaint.Id,
                WorkerName = complaint.WorkerName,
                Category = complaint.Category,
                State = complaint.State,
                Severity = complaint.Severity,
                Title = FormatCityComplaintTitle(complaint, ru),
                Detail = FormatCityComplaintDetail(complaint, ru),
                Meta = FormatCityComplaintMeta(complaint, ru),
                AccentColor = GetCityComplaintAccentColor(complaint)
            });
        }

        return rows;
    }

}
