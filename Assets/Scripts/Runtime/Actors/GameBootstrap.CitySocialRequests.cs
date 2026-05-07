using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float CitySocialRequestInitialDelayWorldHours = 10f;
    private const float CitySocialRequestSpacingMinWorldHours = 18f;
    private const float CitySocialRequestSpacingMaxWorldHours = 24f;
    private const int CitySocialRequestMaxTargetFamiliarity = 29;
    private const int CitySocialRequestMaxTargetRelationship = 19;

    private sealed class CitySocialIntroductionRequest
    {
        public int Id;
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

        BeginCitySocialIntroductionRequest(request, manualDebug: false);
    }

    private bool TryStartDebugCitySocialIntroductionRequest(out string result)
    {
        result = string.Empty;
        if (activeCitySocialIntroductionRequest != null || isCitySocialRequestSceneOpen)
        {
            result = "social request scene is already active";
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

        isDebugServicePanelOpen = false;
        BeginCitySocialIntroductionRequest(request, manualDebug: true);
        result = $"{request.RequesterName} -> {request.TargetName}";
        return true;
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
        if (HasWorkerPerk(requester, WorkerPerkKind.Socialite))
        {
            score += 18;
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

    private void CompleteCitySocialIntroductionRequest(string topic)
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

        RecordWorkerSocialInteraction(requester, target, WorkerSocialInteractionKind.PlayerPromptedConversation);
        WorkerSocialMemory requesterMemory = FindWorkerSocialMemory(requester, target.DriverId);
        WorkerSocialMemory targetMemory = FindWorkerSocialMemory(target, requester.DriverId);
        int familiarity = GetWorkerSocialPairAverageFamiliarity(requesterMemory, targetMemory);
        int relationship = GetWorkerSocialPairAverageRelationship(requesterMemory, targetMemory);
        PushFeedEvent(
            "A conversation landed.",
            $"{GetWorkerDisplayNameSafe(requester)} и {GetWorkerDisplayNameSafe(target)} обсудили «{request.Topic}». Город стал на один неловкий мостик человечнее.",
            FeedEventType.Info);
        SessionDebugLogger.Log(
            "CITY_SOCIAL_REQUEST",
            $"Social introduction request #{request.Id} completed: requester={request.RequesterName}, target={request.TargetName}, topic='{request.Topic}', familiarity={familiarity}, relationship={relationship}.");
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
