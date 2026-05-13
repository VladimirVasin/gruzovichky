using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float CitySocialRequestInitialDelayWorldHours = 10f;
    private const float CitySocialRequestSpacingMinWorldHours = 18f;
    private const float CitySocialRequestSpacingMaxWorldHours = 24f;
    private const int CitySocialRequestMaxTargetFamiliarity = 29;
    private const int CitySocialRequestMaxTargetRelationship = 19;
    private const int CitySocialIntroSuccessFamiliarityDelta = 22;
    private const int CitySocialIntroSuccessRelationshipDelta = 13;
    private const int CitySocialIntroFailureFamiliarityDelta = 10;
    private const int CitySocialIntroFailureRelationshipDelta = -7;

    private sealed class CitySocialIntroductionRequest
    {
        public int Id;
        public int ComplaintId;
        public int RequesterId;
        public int TargetId;
        public string RequesterName = string.Empty;
        public string TargetName = string.Empty;
        public int CreatedDay;
        public float CreatedWorldHour;
        public string Topic = string.Empty;
    }

    private CitySocialIntroductionRequest activeCitySocialIntroductionRequest;
    private int nextCitySocialIntroductionRequestId = 1;
    private float nextCitySocialIntroductionRequestAllowedWorldHour = -1f;
    private float observedCitySocialRequestCityHallStartWorldHour = -1f;

    private void UpdateCitySocialIntroductionRequests()
    {
        if (!locations.ContainsKey(LocationType.CityHall) ||
            gameSpeedMultiplier <= 0 ||
            isCitySocialRequestSceneOpen)
        {
            return;
        }

        float now = GetCurrentWorldHour();
        if (cityHallRuntimeStartedWorldHour < 0f)
        {
            cityHallRuntimeStartedWorldHour = now;
        }

        if (!Mathf.Approximately(observedCitySocialRequestCityHallStartWorldHour, cityHallRuntimeStartedWorldHour))
        {
            observedCitySocialRequestCityHallStartWorldHour = cityHallRuntimeStartedWorldHour;
            nextCitySocialIntroductionRequestAllowedWorldHour =
                cityHallRuntimeStartedWorldHour +
                CitySocialRequestInitialDelayWorldHours +
                Random.Range(1.5f, 4.5f);
        }

        if (activeCitySocialIntroductionRequest != null ||
            HasActiveCitySocialIntroductionComplaint() ||
            now < nextCitySocialIntroductionRequestAllowedWorldHour ||
            IsCitySocialIntroductionBlockedByUi())
        {
            return;
        }

        if (!TryCreateCitySocialIntroductionRequest(out CitySocialIntroductionRequest request))
        {
            nextCitySocialIntroductionRequestAllowedWorldHour = now + 3f;
            return;
        }

        FileCitySocialIntroductionComplaint(request);
    }

    private bool TryFileDebugCitySocialIntroductionRequest(out string result)
    {
        result = string.Empty;
        if (locations == null || !locations.ContainsKey(LocationType.CityHall))
        {
            result = "city hall is not built";
            return false;
        }

        if (activeCitySocialIntroductionRequest != null || isCitySocialRequestSceneOpen)
        {
            result = "social request scene is already active";
            return false;
        }

        if (HasActiveCitySocialIntroductionComplaint())
        {
            result = "social request is already filed in City Hall";
            return false;
        }

        if (driverAgents == null || driverAgents.Count < 2)
        {
            result = "need at least 2 residents";
            return false;
        }

        if (!TryCreateCitySocialIntroductionRequest(out CitySocialIntroductionRequest request) &&
            !TryCreateFallbackCitySocialIntroductionRequest(out request))
        {
            result = "no valid resident pair";
            return false;
        }

        FileCitySocialIntroductionComplaint(request);
        result = $"filed in City Hall: {request.RequesterName} -> {request.TargetName}";
        return true;
    }

    private void FileCitySocialIntroductionComplaint(CitySocialIntroductionRequest request)
    {
        if (request == null)
        {
            return;
        }

        float now = GetCurrentWorldHour();
        CityComplaint complaint = new()
        {
            Id = nextCityComplaintId++,
            WorkerId = request.RequesterId,
            WorkerName = request.RequesterName,
            GroupKey = GetCitySocialIntroductionGroupKey(request.RequesterId, request.TargetId),
            Category = CityComplaintCategory.SocialIntroduction,
            State = CityComplaintState.Open,
            Severity = 2,
            CreatedWorldHour = now,
            CreatedDay = currentDay,
            DueWorldHour = 0f,
            IsUnread = true,
            SocialTargetWorkerId = request.TargetId,
            SocialTargetWorkerName = request.TargetName
        };
        complaint.SignerIds.Add(request.RequesterId);
        complaint.SignerNames.Add(request.RequesterName);
        cityComplaints.Add(complaint);
        cityComplaintCooldownByKey[complaint.GroupKey] = now + CitySocialRequestSpacingMinWorldHours;
        nextCitySocialIntroductionRequestAllowedWorldHour =
            now + Random.Range(CitySocialRequestSpacingMinWorldHours, CitySocialRequestSpacingMaxWorldHours);
        NotifyCityHallNewRequest(complaint);
        RecordWorkerBuildingKnowledge(
            GetDriverAgentById(request.RequesterId),
            LocationType.CityHall,
            "\u041e\u0431\u0440\u0430\u0442\u0438\u043b\u0441\u044f \u0432 \u0440\u0430\u0442\u0443\u0448\u0443 \u0437\u0430 \u0437\u043d\u0430\u043a\u043e\u043c\u0441\u0442\u0432\u043e\u043c",
            "Filed a social introduction request at City Hall");
        SessionDebugLogger.Log(
            "CITY_SOCIAL_REQUEST",
            $"Social introduction request #{complaint.Id} filed in City Hall: requester={request.RequesterName}, target={request.TargetName}, nextAllowed={nextCitySocialIntroductionRequestAllowedWorldHour:0.0}.");
    }

    private void BeginCitySocialIntroductionRequest(CitySocialIntroductionRequest request, bool manualDebug)
    {
        if (request == null)
        {
            return;
        }

        float now = GetCurrentWorldHour();
        activeCitySocialIntroductionRequest = request;
        nextCitySocialIntroductionRequestAllowedWorldHour =
            now + Random.Range(CitySocialRequestSpacingMinWorldHours, CitySocialRequestSpacingMaxWorldHours);
        StartCitySocialIntroductionScene(request);
        PushFeedEvent(
            manualDebug ? "Debug social request started." : "A citizen wants help starting a conversation.",
            $"{request.RequesterName} ищет тему для разговора с {request.TargetName}. Ратуша снова занимается тонкой настройкой человеческой души.",
            FeedEventType.Info);
        SessionDebugLogger.Log(
            "CITY_SOCIAL_REQUEST",
            $"Social introduction request #{request.Id}: requester={request.RequesterName}, target={request.TargetName}, manualDebug={(manualDebug ? "yes" : "no")}, nextAllowed={nextCitySocialIntroductionRequestAllowedWorldHour:0.0}.");
    }

    private void StartAcceptedCitySocialIntroductionRequest(int complaintId)
    {
        CityComplaint complaint = GetCityComplaintById(complaintId);
        if (complaint == null ||
            complaint.Category != CityComplaintCategory.SocialIntroduction ||
            complaint.State != CityComplaintState.Accepted)
        {
            return;
        }

        if (!TryCreateCitySocialIntroductionRequestFromComplaint(complaint, out CitySocialIntroductionRequest request))
        {
            ResolveCityComplaint(complaint, "social participant unavailable", manually: false);
            return;
        }

        BeginCitySocialIntroductionRequest(request, manualDebug: false);
    }

    private bool IsCitySocialIntroductionBlockedByUi()
    {
        return isMainMenuOpen ||
               isTutorialOpen ||
               isRacingActive ||
               isLoadingWorld ||
               isFleetPanelOpen ||
               isShiftsPanelOpen ||
               isDriversPanelOpen ||
               isResourcesPanelOpen ||
               isEconomyPanelOpen ||
               isTradePanelOpen ||
               isBuildPanelOpen ||
               isWorldMapPanelOpen ||
               isStatesPanelOpen ||
               isSocialGraphPanelOpen ||
               isCityHallPanelOpen ||
               isTruckDetailsOpen ||
               isLocalBusDetailsOpen ||
               isDriverDetailsOpen ||
               activeBuildTool != BuildTool.None;
    }

    private bool TryCreateCitySocialIntroductionRequest(out CitySocialIntroductionRequest request)
    {
        request = null;
        List<DriverAgent> candidates = new();
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (CanWorkerParticipateInCitySocialIntroduction(worker))
            {
                candidates.Add(worker);
            }
        }

        if (candidates.Count < 2)
        {
            return false;
        }

        while (candidates.Count > 0)
        {
            int requesterIndex = Random.Range(0, candidates.Count);
            DriverAgent requester = candidates[requesterIndex];
            candidates.RemoveAt(requesterIndex);
            if (!TryPickCitySocialIntroductionTarget(requester, out DriverAgent target))
            {
                continue;
            }

            request = CreateCitySocialIntroductionRequest(requester, target);
            return true;
        }

        return false;
    }

    private bool TryCreateFallbackCitySocialIntroductionRequest(out CitySocialIntroductionRequest request)
    {
        request = null;
        List<DriverAgent> candidates = new();
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (CanWorkerParticipateInCitySocialIntroduction(worker))
            {
                candidates.Add(worker);
            }
        }

        while (candidates.Count > 0)
        {
            int requesterIndex = Random.Range(0, candidates.Count);
            DriverAgent requester = candidates[requesterIndex];
            candidates.RemoveAt(requesterIndex);
            if (!TryPickFallbackCitySocialIntroductionTarget(requester, out DriverAgent target))
            {
                continue;
            }

            request = CreateCitySocialIntroductionRequest(requester, target);
            return true;
        }

        return false;
    }

    private bool TryPickCitySocialIntroductionTarget(DriverAgent requester, out DriverAgent target)
    {
        target = null;
        if (requester == null)
        {
            return false;
        }

        int bestScore = int.MinValue;
        List<DriverAgent> bestTargets = new();
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent candidate = driverAgents[i];
            int score = ScoreCitySocialIntroductionTarget(requester, candidate);
            if (score == int.MinValue)
            {
                continue;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestTargets.Clear();
            }

            if (score == bestScore)
            {
                bestTargets.Add(candidate);
            }
        }

        if (bestTargets.Count == 0)
        {
            return false;
        }

        target = bestTargets[Random.Range(0, bestTargets.Count)];
        return target != null;
    }

    private bool TryPickFallbackCitySocialIntroductionTarget(DriverAgent requester, out DriverAgent target)
    {
        target = null;
        if (requester == null)
        {
            return false;
        }

        int bestScore = int.MaxValue;
        List<DriverAgent> bestTargets = new();
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent candidate = driverAgents[i];
            if (!CanWorkerParticipateInCitySocialIntroduction(candidate) ||
                requester == candidate ||
                candidate.DriverId == requester.DriverId ||
                (requester.FamilyId > 0 && requester.FamilyId == candidate.FamilyId))
            {
                continue;
            }

            WorkerSocialMemory requesterMemory = FindWorkerSocialMemory(requester, candidate.DriverId);
            WorkerSocialMemory candidateMemory = FindWorkerSocialMemory(candidate, requester.DriverId);
            int familiarity = Mathf.Max(requesterMemory?.Familiarity ?? 0, candidateMemory?.Familiarity ?? 0);
            int relationship = Mathf.Max(requesterMemory?.Relationship ?? 0, candidateMemory?.Relationship ?? 0);
            int interactions = Mathf.Max(requesterMemory?.InteractionCount ?? 0, candidateMemory?.InteractionCount ?? 0);
            int score = familiarity * 4 + Mathf.Abs(relationship) * 2 + interactions * 8;

            if (score < bestScore)
            {
                bestScore = score;
                bestTargets.Clear();
            }

            if (score == bestScore)
            {
                bestTargets.Add(candidate);
            }
        }

        if (bestTargets.Count == 0)
        {
            return false;
        }

        target = bestTargets[Random.Range(0, bestTargets.Count)];
        return target != null;
    }

    private CitySocialIntroductionRequest CreateCitySocialIntroductionRequest(DriverAgent requester, DriverAgent target)
    {
        float now = GetCurrentWorldHour();
        return new CitySocialIntroductionRequest
        {
            Id = nextCitySocialIntroductionRequestId++,
            RequesterId = requester.DriverId,
            TargetId = target.DriverId,
            RequesterName = GetWorkerDisplayNameSafe(requester),
            TargetName = GetWorkerDisplayNameSafe(target),
            CreatedDay = currentDay,
            CreatedWorldHour = now
        };
    }

    private bool TryCreateCitySocialIntroductionRequestFromComplaint(CityComplaint complaint, out CitySocialIntroductionRequest request)
    {
        request = null;
        if (complaint == null)
        {
            return false;
        }

        DriverAgent requester = GetDriverAgentById(complaint.WorkerId);
        DriverAgent target = GetDriverAgentById(complaint.SocialTargetWorkerId);
        if (!CanWorkerParticipateInCitySocialIntroduction(requester) ||
            !CanWorkerParticipateInCitySocialIntroduction(target))
        {
            return false;
        }

        request = CreateCitySocialIntroductionRequest(requester, target);
        request.Id = complaint.Id;
        request.ComplaintId = complaint.Id;
        return true;
    }

    private bool HasActiveCitySocialIntroductionComplaint()
    {
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (IsCityComplaintActive(complaint) &&
                complaint.Category == CityComplaintCategory.SocialIntroduction)
            {
                return true;
            }
        }

        return false;
    }

    private bool DoesCitySocialIntroductionConditionRemain(CityComplaint complaint, out string reason)
    {
        reason = string.Empty;
        if (complaint == null || complaint.Category != CityComplaintCategory.SocialIntroduction)
        {
            reason = "not a social introduction";
            return false;
        }

        DriverAgent requester = GetDriverAgentById(complaint.WorkerId);
        DriverAgent target = GetDriverAgentById(complaint.SocialTargetWorkerId);
        bool valid = CanWorkerParticipateInCitySocialIntroduction(requester) &&
                     CanWorkerParticipateInCitySocialIntroduction(target);
        if (!valid)
        {
            reason = "social participant unavailable";
        }

        return valid;
    }

    private static string GetCitySocialIntroductionGroupKey(int requesterId, int targetId)
    {
        return $"SocialIntroduction:{requesterId}:{targetId}";
    }

    private int ScoreCitySocialIntroductionTarget(DriverAgent requester, DriverAgent candidate)
    {
        if (!CanWorkerParticipateInCitySocialIntroduction(candidate) ||
            requester == null ||
            requester == candidate ||
            candidate.DriverId == requester.DriverId)
        {
            return int.MinValue;
        }

        if (requester.FamilyId > 0 && requester.FamilyId == candidate.FamilyId)
        {
            return int.MinValue;
        }

        WorkerSocialMemory requesterMemory = FindWorkerSocialMemory(requester, candidate.DriverId);
        WorkerSocialMemory candidateMemory = FindWorkerSocialMemory(candidate, requester.DriverId);
        int familiarity = Mathf.Max(requesterMemory?.Familiarity ?? 0, candidateMemory?.Familiarity ?? 0);
        int relationship = Mathf.Max(requesterMemory?.Relationship ?? 0, candidateMemory?.Relationship ?? 0);
        int exposure = Mathf.Max(requesterMemory?.Exposure ?? 0, candidateMemory?.Exposure ?? 0);
        int interactions = Mathf.Max(requesterMemory?.InteractionCount ?? 0, candidateMemory?.InteractionCount ?? 0);

        if (familiarity > CitySocialRequestMaxTargetFamiliarity ||
            relationship > CitySocialRequestMaxTargetRelationship)
        {
            return int.MinValue;
        }

        int score = 120;
        if (requesterMemory == null && candidateMemory == null)
        {
            score += 55;
        }
        else if (!IsWorkerSocialMemoryVisible(requesterMemory) && !IsWorkerSocialMemoryVisible(candidateMemory))
        {
            score += 28;
        }

        score -= familiarity * 3;
        score -= Mathf.Abs(relationship) * 2;
        score -= exposure;
        score -= interactions * 5;
        if (HasWorkerTrait(requester, WorkerTraitKind.Sociable))
        {
            score += 18;
        }
        else if (HasWorkerTrait(requester, WorkerTraitKind.Reserved))
        {
            score -= 12;
        }

        return score;
    }

    private static bool CanWorkerParticipateInCitySocialIntroduction(DriverAgent worker)
    {
        return worker != null &&
               worker.DriverId > 0 &&
               !worker.IsArrivingByBus &&
               !worker.IsLeavingTown &&
               !worker.HasDepartedTown;
    }

    private static string GetWorkerDisplayNameSafe(DriverAgent worker)
    {
        if (worker == null)
        {
            return "Неизвестный житель";
        }

        return string.IsNullOrWhiteSpace(worker.DriverName) ? $"#{worker.DriverId}" : worker.DriverName;
    }

    private void CompleteCitySocialIntroductionRequest(string topic, bool success)
    {
        CitySocialIntroductionRequest request = activeCitySocialIntroductionRequest;
        activeCitySocialIntroductionRequest = null;
        if (request == null)
        {
            return;
        }

        request.Topic = SanitizeCitySocialTopic(topic);
        DriverAgent requester = GetDriverAgentById(request.RequesterId);
        DriverAgent target = GetDriverAgentById(request.TargetId);
        if (requester == null || target == null)
        {
            SessionDebugLogger.Log("CITY_SOCIAL_REQUEST", $"Social introduction request #{request.Id} finished without valid workers.");
            return;
        }

        WorkerSocialInteractionKind kind = success
            ? WorkerSocialInteractionKind.PlayerPromptedConversation
            : WorkerSocialInteractionKind.PlayerPromptedConversationFailed;
        const WorkerKnowledgeSourceAttitude sourceAttitude = WorkerKnowledgeSourceAttitude.Neutral;
        RecordWorkerSocialInteraction(requester, target, kind, null, allowKnowledgeShare: false);
        RecordWorkerPromptedConversationTopicMemory(requester, target, request.Topic, success, kind, sourceAttitude);
        WorkerSocialMemory requesterMemory = FindWorkerSocialMemory(requester, target.DriverId);
        WorkerSocialMemory targetMemory = FindWorkerSocialMemory(target, requester.DriverId);
        int familiarity = GetWorkerSocialPairAverageFamiliarity(requesterMemory, targetMemory);
        int relationship = GetWorkerSocialPairAverageRelationship(requesterMemory, targetMemory);
        PushFeedEvent(
            success ? "Разговор состоялся." : "Разговор не сложился.",
            success
                ? $"{GetWorkerDisplayNameSafe(requester)} и {GetWorkerDisplayNameSafe(target)} обсудили «{request.Topic}». Тема сработала: знакомство крепнет, симпатия растет."
                : $"{GetWorkerDisplayNameSafe(requester)} и {GetWorkerDisplayNameSafe(target)} попробовали обсудить «{request.Topic}». Тема пошла боком: знакомство все же появилось, но симпатии стало меньше.",
            FeedEventType.Info);
        if (request.ComplaintId > 0)
        {
            CityComplaint complaint = GetCityComplaintById(request.ComplaintId);
            if (complaint != null && complaint.State == CityComplaintState.Accepted)
            {
                ResolveCityComplaint(complaint, success ? "social introduction completed" : "social introduction failed", manually: false);
            }
        }

        SessionDebugLogger.Log(
            "CITY_SOCIAL_REQUEST",
            $"Social introduction request #{request.Id} completed: requester={request.RequesterName}, target={request.TargetName}, topic='{request.Topic}', firstIterationFraming={sourceAttitude}, outcome={(success ? "success" : "failure")}, familiarity={familiarity}, relationship={relationship}.");
    }

    private static string SanitizeCitySocialTopic(string rawTopic)
    {
        string topic = string.IsNullOrWhiteSpace(rawTopic)
            ? "городские скамейки как форму тихого сопротивления"
            : rawTopic.Trim();

        topic = topic.Replace("\r", " ").Replace("\n", " ");
        while (topic.Contains("  "))
        {
            topic = topic.Replace("  ", " ");
        }

        if (topic.Length > 64)
        {
            topic = topic.Substring(0, 64).TrimEnd() + "...";
        }

        return topic;
    }
}
