using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerPendingKnowledgeCap = 12;
    private const float WorkerKnowledgeFormationCheckIntervalHours = 0.25f;
    private const float WorkerKnowledgeFormationBaseStageHours = 0.42f;

    private float lastWorkerKnowledgeFormationCheckWorldHour = -1f;

    private WorkerMemory QueueWorkerKnowledgeFormation(
        DriverAgent owner,
        DriverAgent sourceWorker,
        WorkerMemory seed,
        float now,
        WorkerSocialInteractionKind sourceKind = WorkerSocialInteractionKind.IdleConversation,
        LocationType? sourceLocationType = null)
    {
        if (owner == null ||
            seed == null ||
            owner.HasDepartedTown ||
            owner.IsLeavingTown ||
            !IsWorkerMemoryDisplayable(seed) ||
            HasEquivalentFormedWorkerKnowledge(owner, seed, now))
        {
            return null;
        }

        PendingWorkerKnowledge pending = FindPendingWorkerKnowledge(owner, seed);
        if (pending == null)
        {
            pending = new PendingWorkerKnowledge
            {
                FormationKey = BuildWorkerKnowledgeFormationKey(seed),
                CognitionKind = GetWorkerMemoryCognitionKind(seed),
                Kind = seed.Kind,
                StartedDay = currentDay,
                StartedWorldHour = now,
                StageStartedWorldHour = now,
                NextStageWorldHour = now + GetWorkerKnowledgeFormationStageHours(owner, seed, WorkerKnowledgeFormationStage.Heard)
            };
            InsertPendingWorkerKnowledge(owner, pending);
        }
        else
        {
            pending.NextStageWorldHour = Mathf.Min(
                pending.NextStageWorldHour,
                now + GetWorkerKnowledgeFormationStageHours(owner, seed, pending.Stage));
        }

        ApplyWorkerKnowledgeSeed(pending, sourceWorker, seed, sourceKind, sourceLocationType, now);
        RefreshPendingWorkerKnowledgeOpinion(owner, pending);
        CopyWorkerKnowledgeOpinionState(seed, pending);
        isDriversScreenDirty = true;
        SessionDebugLogger.Log(
            "KNOWLEDGE",
            $"{GetWorkerDisplayNameSafe(owner)} started thinking about {FormatPendingWorkerKnowledgeDebugLabel(pending)}.");
        return seed;
    }

    private void InsertPendingWorkerKnowledge(DriverAgent owner, PendingWorkerKnowledge pending)
    {
        owner.PendingKnowledge.Insert(0, pending);
        TrimWorkerPendingKnowledge(owner);
    }

    private static void ApplyWorkerKnowledgeSeed(
        PendingWorkerKnowledge pending,
        DriverAgent sourceWorker,
        WorkerMemory seed,
        WorkerSocialInteractionKind sourceKind,
        LocationType? sourceLocationType,
        float now)
    {
        pending.OtherWorkerId = seed.OtherWorkerId;
        pending.CognitionKind = GetWorkerMemoryCognitionKind(seed);
        pending.ConversationTopicKey = seed.ConversationTopicKey ?? string.Empty;
        if (seed.Kind == WorkerMemoryKind.ConversationTopic && string.IsNullOrWhiteSpace(pending.ConversationTopicKey))
        {
            pending.ConversationTopicKey = BuildConversationTopicKey(GetWorkerRumorOriginalTopic(seed));
        }

        pending.Topic = seed.Topic ?? string.Empty;
        pending.BuildingType = seed.BuildingType;
        pending.BuildingInstanceId = seed.BuildingInstanceId;
        pending.BuildingLabel = seed.BuildingLabel ?? string.Empty;
        pending.SourceRu = seed.SourceRu ?? string.Empty;
        pending.SourceEn = seed.SourceEn ?? string.Empty;
        pending.Positive = seed.Positive;
        pending.KnowledgeIteration = Mathf.Max(1, seed.KnowledgeIteration);
        pending.SourceAttitude = seed.SourceAttitude;
        CopyWorkerRumorState(pending, seed);
        CopyWorkerKnowledgeOpinionState(pending, seed);
        pending.SourceWorkerId = sourceWorker?.DriverId ?? seed.FormedFromWorkerId;
        pending.SourceInteractionKind = sourceKind;
        pending.SourceLocationType = sourceLocationType;
        pending.LastRefreshedWorldHour = now;
    }

    private void UpdateWorkerKnowledgeFormationRuntime()
    {
        float now = GetCurrentWorldHour();
        if (lastWorkerKnowledgeFormationCheckWorldHour >= 0f &&
            now >= lastWorkerKnowledgeFormationCheckWorldHour &&
            now - lastWorkerKnowledgeFormationCheckWorldHour < WorkerKnowledgeFormationCheckIntervalHours)
        {
            return;
        }

        lastWorkerKnowledgeFormationCheckWorldHour = now;
        bool changed = false;
        bool hasActivePending = false;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            hasActivePending |= worker != null && worker.PendingKnowledge.Count > 0;
            changed |= UpdatePendingWorkerKnowledgeFormation(worker, now);
        }

        if (changed ||
            hasActivePending &&
            isDriversPanelOpen &&
            activeWorkerDetailTab == WorkerDetailTab.Knowledge)
        {
            isDriversScreenDirty = true;
        }

        if (changed)
        {
            isNoosphereScreenDirty = true;
        }
    }

    private bool UpdatePendingWorkerKnowledgeFormation(DriverAgent worker, float now)
    {
        if (worker == null || worker.PendingKnowledge.Count == 0)
        {
            return false;
        }

        bool changed = false;
        for (int i = worker.PendingKnowledge.Count - 1; i >= 0; i--)
        {
            if (i >= worker.PendingKnowledge.Count)
            {
                i = worker.PendingKnowledge.Count - 1;
                if (i < 0)
                {
                    break;
                }
            }

            PendingWorkerKnowledge pending = worker.PendingKnowledge[i];
            if (!IsPendingWorkerKnowledgeValid(worker, pending, now))
            {
                changed |= RemovePendingWorkerKnowledgeIfPresent(worker, i, pending);
                continue;
            }

            if (now < pending.NextStageWorldHour)
            {
                continue;
            }

            if (pending.Stage == WorkerKnowledgeFormationStage.Heard)
            {
                pending.Stage = WorkerKnowledgeFormationStage.Comparing;
                pending.StageStartedWorldHour = now;
                pending.NextStageWorldHour = now + GetWorkerKnowledgeFormationStageHours(worker, pending, pending.Stage);
                RefreshPendingWorkerKnowledgeOpinion(worker, pending);
                changed = true;
                continue;
            }

            if (pending.Stage == WorkerKnowledgeFormationStage.Comparing)
            {
                pending.Stage = WorkerKnowledgeFormationStage.Judging;
                pending.StageStartedWorldHour = now;
                pending.NextStageWorldHour = now + GetWorkerKnowledgeFormationStageHours(worker, pending, pending.Stage);
                RefreshPendingWorkerKnowledgeOpinion(worker, pending);
                changed = true;
                continue;
            }

            WorkerMemory formed = FormWorkerKnowledgeFromPending(worker, pending, now);
            bool removed = RemovePendingWorkerKnowledgeIfPresent(worker, i, pending);
            changed = formed != null || removed || changed;
        }

        return changed;
    }

    private static bool RemovePendingWorkerKnowledgeIfPresent(DriverAgent worker, int index, PendingWorkerKnowledge pending)
    {
        if (worker == null)
        {
            return false;
        }

        if (index >= 0 &&
            index < worker.PendingKnowledge.Count &&
            ReferenceEquals(worker.PendingKnowledge[index], pending))
        {
            worker.PendingKnowledge.RemoveAt(index);
            return true;
        }

        int currentIndex = worker.PendingKnowledge.IndexOf(pending);
        if (currentIndex >= 0)
        {
            worker.PendingKnowledge.RemoveAt(currentIndex);
            return true;
        }

        return false;
    }

    private bool IsPendingWorkerKnowledgeValid(DriverAgent worker, PendingWorkerKnowledge pending, float now)
    {
        if (worker == null ||
            pending == null ||
            worker.HasDepartedTown ||
            worker.IsLeavingTown ||
            !IsPendingWorkerKnowledgeDisplayable(pending))
        {
            return false;
        }

        WorkerMemory probe = CreateWorkerMemoryProbe(pending);
        return !HasEquivalentFormedWorkerKnowledge(worker, probe, now);
    }

    private WorkerMemory FormWorkerKnowledgeFromPending(DriverAgent worker, PendingWorkerKnowledge pending, float now)
    {
        if (!IsPendingWorkerKnowledgeValid(worker, pending, now))
        {
            return null;
        }

        RefreshPendingWorkerKnowledgeOpinion(worker, pending);
        WorkerMemory memory = new()
        {
            CognitionKind = GetPendingWorkerKnowledgeCognitionKind(pending),
            Kind = pending.Kind,
            ConversationTopicKey = pending.ConversationTopicKey ?? string.Empty,
            OtherWorkerId = pending.OtherWorkerId,
            Topic = pending.Topic,
            BuildingType = pending.BuildingType,
            BuildingInstanceId = pending.BuildingInstanceId,
            BuildingLabel = pending.BuildingLabel,
            SourceRu = pending.SourceRu,
            SourceEn = pending.SourceEn,
            Positive = pending.Positive,
            KnowledgeIteration = Mathf.Max(1, pending.KnowledgeIteration),
            SourceAttitude = pending.SourceAttitude,
            OpinionTone = pending.OpinionTone,
            OpinionScore = pending.OpinionScore,
            OpinionConfidence = pending.OpinionConfidence,
            OpinionReasonRu = pending.OpinionReasonRu,
            OpinionReasonEn = pending.OpinionReasonEn,
            FormedFromWorkerId = pending.SourceWorkerId,
            FormationStartedWorldHour = pending.StartedWorldHour,
            FormationCompletedWorldHour = now,
            CreatedDay = currentDay,
            CreatedWorldHour = now,
            ExpiresWorldHour = now + WorkerPersonalMemoryLifetimeHours
        };
        CopyWorkerRumorState(memory, pending);

        DriverAgent sourceWorker = GetDriverAgentById(pending.SourceWorkerId);
        if (memory.Kind == WorkerMemoryKind.ConversationTopic)
        {
            CommitWorkerTopicOpinion(worker, sourceWorker, memory, pending, now);
        }

        if (memory.Kind == WorkerMemoryKind.BuildingExistence &&
            memory.BuildingType.HasValue &&
            string.IsNullOrWhiteSpace(memory.BuildingLabel))
        {
            memory.BuildingLabel = GetWorkerKnowledgeBuildingDisplayName(memory.BuildingType.Value, memory.BuildingInstanceId, IsRussianLanguage());
        }

        worker.Memories.Insert(0, memory);
        RecordNoosphereKnowledgeReceived(worker, sourceWorker, memory, now);
        TryCanonizeCityKnowledge(memory, now);
        TrimWorkerMemories(worker, now);
        QueueWorkerKnowledgeReflectionThought(worker, sourceWorker, memory, now);
        isDriversScreenDirty = true;
        SessionDebugLogger.Log(
            "KNOWLEDGE",
            $"{GetWorkerDisplayNameSafe(worker)} formed {GetPendingWorkerKnowledgeCognitionKind(pending)} about {FormatPendingWorkerKnowledgeDebugLabel(pending)} (tone={pending.OpinionTone}, score={pending.OpinionScore}, confidence={pending.OpinionConfidence}).");
        return memory;
    }

    private static WorkerMemory CreateWorkerMemoryProbe(PendingWorkerKnowledge pending)
    {
        return new WorkerMemory
        {
            CognitionKind = GetPendingWorkerKnowledgeCognitionKind(pending),
            Kind = pending.Kind,
            ConversationTopicKey = pending.ConversationTopicKey ?? string.Empty,
            OtherWorkerId = pending.OtherWorkerId,
            Topic = pending.Topic,
            BuildingType = pending.BuildingType,
            BuildingInstanceId = pending.BuildingInstanceId,
            BuildingLabel = pending.BuildingLabel,
            RumorRootId = pending.RumorRootId,
            OriginalTopic = pending.OriginalTopic,
            RumorTopic = pending.RumorTopic,
            RumorDistortionPercent = pending.RumorDistortionPercent,
            RumorConnotationScore = pending.RumorConnotationScore,
            RumorConnotationConfidence = pending.RumorConnotationConfidence
        };
    }

    private void RefreshPendingWorkerKnowledgeOpinion(DriverAgent worker, PendingWorkerKnowledge pending)
    {
        if (worker == null || pending == null)
        {
            return;
        }

        if (pending.Kind == WorkerMemoryKind.BuildingExistence)
        {
            EvaluateBuildingKnowledgeFact(pending);
        }
        else
        {
            EvaluateTopicKnowledgeOpinion(worker, pending);
        }

        int heritageScore = pending.OpinionScore;
        int heritageConfidence = pending.OpinionConfidence;
        string heritageReasonRu = pending.OpinionReasonRu;
        string heritageReasonEn = pending.OpinionReasonEn;
        ApplyWorkerHeritageKnowledgeBias(worker, pending, ref heritageScore, ref heritageConfidence, ref heritageReasonRu, ref heritageReasonEn);
        pending.OpinionScore = heritageScore;
        pending.OpinionConfidence = heritageConfidence;
        pending.OpinionReasonRu = heritageReasonRu;
        pending.OpinionReasonEn = heritageReasonEn;
        pending.OpinionTone = GetWorkerKnowledgeOpinionTone(pending.OpinionScore);
        pending.OpinionScore = Mathf.Clamp(pending.OpinionScore, -100, 100);
        pending.OpinionConfidence = Mathf.Clamp(pending.OpinionConfidence, 1, 100);
    }

    private static void EvaluateBuildingKnowledgeFact(PendingWorkerKnowledge pending)
    {
        if (pending == null)
        {
            return;
        }

        pending.CognitionKind = WorkerCognitionKind.Fact;
        pending.OpinionScore = 0;
        pending.OpinionConfidence = 96;
        pending.OpinionReasonRu = "это факт о наличии места, а не оценка места";
        pending.OpinionReasonEn = "this is a fact that the place exists, not a judgement about it";
    }

    private void EvaluateBuildingKnowledgeOpinion(DriverAgent worker, PendingWorkerKnowledge pending)
    {
        LocationType type = pending.BuildingType ?? LocationType.Parking;
        int score = 6;
        int confidence = 48 + Mathf.Clamp(Mathf.Max(1, pending.KnowledgeIteration) * 3, 0, 12);
        string reasonRu = "Пока это просто ориентир на карте города.";
        string reasonEn = "For now this is just a landmark in town.";

        switch (type)
        {
            case LocationType.LaborExchange:
                if (IsWorkerUnemployedForThoughts(worker))
                {
                    score += 42;
                    reasonRu = "Может помочь найти работу, значит место полезное.";
                    reasonEn = "It may help find work, so the place feels useful.";
                }
                else
                {
                    score += 12;
                    reasonRu = "Полезно знать, где в городе решают вопросы работы.";
                    reasonEn = "It is useful to know where work questions are handled.";
                }
                break;
            case LocationType.Canteen:
            case LocationType.Kiosk:
                ApplyNeedBuildingOpinion(worker.LastMealNeedStatus, ref score, ref reasonRu, ref reasonEn, "поесть", "eat", 38);
                break;
            case LocationType.PersonalHouse:
                score += worker.LastSleepNeedStatus == WorkerNeedStatus.Critical ? 42 : 22;
                reasonRu = "Жилье выглядит как место, где можно нормально восстановиться.";
                reasonEn = "Housing looks like a place where one can recover properly.";
                break;
            case LocationType.Motel:
                ApplyNeedBuildingOpinion(worker.LastSleepNeedStatus, ref score, ref reasonRu, ref reasonEn, "переночевать", "sleep", 34);
                if (worker.Money < 12)
                {
                    score -= 18;
                    reasonRu = "Ночлег нужен, но денег на него почти нет.";
                    reasonEn = "Sleep is needed, but there is barely enough money for it.";
                }
                break;
            case LocationType.CityPark:
                ApplyNeedBuildingOpinion(worker.LastLeisureNeedStatus, ref score, ref reasonRu, ref reasonEn, "передохнуть", "take a break", 32);
                break;
            case LocationType.Bar:
                ApplyNeedBuildingOpinion(worker.LastLeisureNeedStatus, ref score, ref reasonRu, ref reasonEn, "отдохнуть", "relax", HasWorkerWeakness(worker, WorkerWeaknessKind.Alcoholism) ? 40 : 24);
                if (worker.Money < 10)
                {
                    score -= 12;
                    reasonRu = "Отдохнуть хочется, но бар может быстро съесть деньги.";
                    reasonEn = "Rest sounds good, but a bar can eat money quickly.";
                }
                break;
            case LocationType.GamblingHall:
                score += HasWorkerWeakness(worker, WorkerWeaknessKind.Gambling) ? 18 : -8;
                if (worker.GamblingBroke || worker.Money < 15)
                {
                    score -= 36;
                    reasonRu = "Место выглядит рискованным для денег.";
                    reasonEn = "The place looks risky for money.";
                }
                else if (worker.LastLeisureNeedStatus != WorkerNeedStatus.Ok)
                {
                    score += 12;
                    reasonRu = "Может быть развлечением, но с риском.";
                    reasonEn = "It could be leisure, but with risk.";
                }
                else
                {
                    reasonRu = "Польза неочевидна, зато риск заметен.";
                    reasonEn = "The value is unclear, while the risk is visible.";
                }
                break;
            case LocationType.Forest:
            case LocationType.Warehouse:
            case LocationType.Sawmill:
            case LocationType.FurnitureFactory:
            case LocationType.Docks:
                score += worker.AssignedBuildingType == type ? 34 : IsWorkerUnemployedForThoughts(worker) ? 22 : 10;
                reasonRu = worker.AssignedBuildingType == type
                    ? "Это связано с его работой, значит знание сразу к месту."
                    : "Может пригодиться для работы или городских дел.";
                reasonEn = worker.AssignedBuildingType == type
                    ? "This connects to their work, so the knowledge is immediately relevant."
                    : "It may help with work or town errands.";
                break;
            case LocationType.CityHall:
                score += SourceLooksLikeCityHallRequest(pending) ? 24 : 10;
                reasonRu = "Ратуша связана с просьбами и решениями города.";
                reasonEn = "City Hall is tied to requests and town decisions.";
                break;
            case LocationType.GasStation:
            case LocationType.Stop:
            case LocationType.IntercityStop:
            case LocationType.CarMarket:
            case LocationType.Kindergarten:
            case LocationType.PrimarySchool:
            case LocationType.SecondarySchool:
                score += 8;
                reasonRu = "Пока непонятно, насколько это пригодится лично ему.";
                reasonEn = "It is not yet clear how personally useful this will be.";
                break;
        }

        ApplyWorkerKnowledgePersonality(worker, pending, ref score, ref confidence);
        ApplyWorkerAffectsToBuildingKnowledge(worker, pending, ref score, ref confidence, ref reasonRu, ref reasonEn);
        pending.OpinionScore = score;
        pending.OpinionConfidence = confidence;
        pending.OpinionReasonRu = reasonRu;
        pending.OpinionReasonEn = reasonEn;
    }

    private void EvaluateTopicKnowledgeOpinion(DriverAgent worker, PendingWorkerKnowledge pending)
    {
        if (pending != null)
        {
            pending.CognitionKind = WorkerCognitionKind.Rumor;
        }

        if (TryEvaluateWorkerTopicOpinion(worker, pending))
        {
            return;
        }

        int score = pending.Positive ? 20 : -20;
        int confidence = pending.Positive ? 52 : 46;
        string reasonRu = pending.Positive
            ? "Тема уже сработала в разговоре, поэтому кажется полезной."
            : "Тема связалась с неловким разговором, доверия к ней меньше.";
        string reasonEn = pending.Positive
            ? "The topic already worked in a conversation, so it feels useful."
            : "The topic is tied to an awkward conversation, so trust is lower.";

        ApplyTopicSourceAttitudeOpinion(pending, ref score, ref confidence, ref reasonRu, ref reasonEn);
        ApplyRumorConnotationOpinion(pending, ref score, ref confidence, ref reasonRu, ref reasonEn);

        WorkerSocialMemory otherMemory = FindWorkerSocialMemory(worker, pending.OtherWorkerId);
        if (otherMemory != null)
        {
            score += Mathf.Clamp(Mathf.RoundToInt(otherMemory.Relationship / 5f), -18, 18);
            confidence += Mathf.Clamp(otherMemory.Familiarity / 3 + otherMemory.InteractionCount * 2, 0, 26);
            if (otherMemory.Relationship <= -25)
            {
                reasonRu = "Отношение к собеседнику портит впечатление от темы.";
                reasonEn = "The relationship with the speaker hurts the impression of the topic.";
            }
            else if (otherMemory.Relationship >= 35)
            {
                reasonRu = "Собеседнику он доверяет, поэтому тема звучит весомее.";
                reasonEn = "The speaker is trusted, so the topic carries more weight.";
            }
        }

        if (pending.SourceWorkerId > 0 && pending.SourceWorkerId != pending.OtherWorkerId)
        {
            WorkerSocialMemory sourceMemory = FindWorkerSocialMemory(worker, pending.SourceWorkerId);
            if (sourceMemory != null)
            {
                score += Mathf.Clamp(Mathf.RoundToInt(sourceMemory.Relationship / 8f), -10, 10);
                confidence += Mathf.Clamp(sourceMemory.Familiarity / 5, 0, 14);
            }
        }

        if (pending.SourceInteractionKind == WorkerSocialInteractionKind.PlayerPromptedConversationFailed)
        {
            score -= 10;
            reasonRu = "Ратуша свела людей неудачно, и тема кажется сомнительной.";
            reasonEn = "City Hall introduced them poorly, and the topic feels questionable.";
        }

        confidence += Mathf.Clamp(18 - (Mathf.Max(1, pending.KnowledgeIteration) - 1) * 5, 2, 18);
        ApplyWorkerKnowledgePersonality(worker, pending, ref score, ref confidence);
        pending.OpinionScore = score;
        pending.OpinionConfidence = confidence;
        pending.OpinionReasonRu = reasonRu;
        pending.OpinionReasonEn = reasonEn;
    }

    private static void ApplyTopicSourceAttitudeOpinion(
        PendingWorkerKnowledge pending,
        ref int score,
        ref int confidence,
        ref string reasonRu,
        ref string reasonEn)
    {
        if (pending == null)
        {
            return;
        }

        switch (pending.SourceAttitude)
        {
            case WorkerKnowledgeSourceAttitude.Positive:
                score += 16;
                confidence += 7;
                reasonRu = "Игрок подал тему как полезную, поэтому первая оценка теплее.";
                reasonEn = "The player framed the topic as useful, so the first judgement is warmer.";
                if (!pending.Positive)
                {
                    score -= 14;
                    confidence -= 6;
                    reasonRu = "Игрок подал тему тепло, но разговор споткнулся, поэтому уверенности меньше.";
                    reasonEn = "The player framed the topic warmly, but the conversation stumbled, so confidence is lower.";
                }
                break;
            case WorkerKnowledgeSourceAttitude.Negative:
                score -= 16;
                confidence += 7;
                reasonRu = "Игрок подал тему как предупреждение, поэтому отношение осторожнее.";
                reasonEn = "The player framed the topic as a warning, so the judgement is more cautious.";
                if (!pending.Positive)
                {
                    confidence += 10;
                    reasonRu = "Предупреждение совпало с неловким разговором, и сомнение закрепилось.";
                    reasonEn = "The warning matched an awkward conversation, so the doubt settled in.";
                }
                else
                {
                    score += 6;
                    reasonRu = "Осторожная подача не помешала разговору, но тема все еще звучит спорно.";
                    reasonEn = "The cautious framing did not hurt the conversation, but the topic still sounds debatable.";
                }
                break;
            default:
                confidence += 2;
                break;
        }
    }

    private static void ApplyNeedBuildingOpinion(
        WorkerNeedStatus status,
        ref int score,
        ref string reasonRu,
        ref string reasonEn,
        string needRu,
        string needEn,
        int criticalBonus)
    {
        if (status == WorkerNeedStatus.Critical)
        {
            score += criticalBonus;
            reasonRu = $"Сейчас важно {needRu}, поэтому место выглядит полезным.";
            reasonEn = $"It is important to {needEn} now, so the place looks useful.";
        }
        else if (status == WorkerNeedStatus.Warning)
        {
            score += Mathf.RoundToInt(criticalBonus * 0.62f);
            reasonRu = $"Скоро может понадобиться {needRu}, знание стоит держать в голове.";
            reasonEn = $"They may need to {needEn} soon, so the knowledge is worth keeping.";
        }
        else
        {
            score += 8;
            reasonRu = "Лично сейчас не горит, но ориентир может пригодиться позже.";
            reasonEn = "It is not urgent personally, but the landmark may help later.";
        }
    }

    private static void ApplyWorkerKnowledgePersonality(
        DriverAgent worker,
        PendingWorkerKnowledge pending,
        ref int score,
        ref int confidence)
    {
        if (HasWorkerTrait(worker, WorkerTraitKind.Curious))
        {
            confidence += 12;
        }

        if (HasWorkerTrait(worker, WorkerTraitKind.Frugal) &&
            pending.BuildingType is LocationType.Bar or LocationType.GamblingHall or LocationType.Motel)
        {
            score -= worker.Money < 25 ? 10 : 4;
        }

        if (HasWorkerTrait(worker, WorkerTraitKind.Cautious) &&
            pending.BuildingType is LocationType.Bar or LocationType.GamblingHall or LocationType.Motel)
        {
            score -= 6;
            confidence += 3;
        }

        if (HasWorkerTrait(worker, WorkerTraitKind.Impulsive))
        {
            score += score > 0 ? 5 : score < 0 ? -5 : 0;
            confidence -= 4;
        }

        if (worker?.Education == WorkerEducationLevel.Higher)
        {
            confidence += 6;
        }
        else if (worker?.Education == WorkerEducationLevel.Basic)
        {
            confidence -= 4;
        }
    }

    private static WorkerKnowledgeOpinionTone GetWorkerKnowledgeOpinionTone(int score)
    {
        if (score >= 15)
        {
            return WorkerKnowledgeOpinionTone.Positive;
        }

        return score <= -15
            ? WorkerKnowledgeOpinionTone.Negative
            : WorkerKnowledgeOpinionTone.Neutral;
    }

    private static bool SourceLooksLikeCityHallRequest(PendingWorkerKnowledge pending)
    {
        string combined = $"{pending?.SourceRu} {pending?.SourceEn}".ToUpperInvariant();
        return combined.Contains("ЖАЛОБ") ||
               combined.Contains("ПРОСЬ") ||
               combined.Contains("REQUEST") ||
               combined.Contains("COMPLAINT");
    }

    private static string GetWorkerKnowledgeSourceAttitudeLabel(WorkerKnowledgeSourceAttitude attitude, bool ru)
    {
        return attitude switch
        {
            WorkerKnowledgeSourceAttitude.Positive => ru ? "\u043f\u043e\u0434\u0434\u0435\u0440\u0436\u0430\u0442\u044c" : "support",
            WorkerKnowledgeSourceAttitude.Negative => ru ? "\u043f\u0440\u0435\u0434\u043e\u0441\u0442\u0435\u0440\u0435\u0447\u044c" : "warn",
            _ => ru ? "\u043d\u0435\u0439\u0442\u0440\u0430\u043b\u044c\u043d\u043e" : "neutral"
        };
    }

    private static string FormatWorkerKnowledgeSourceAttitudeMeta(WorkerKnowledgeSourceAttitude attitude, bool ru)
    {
        string label = GetWorkerKnowledgeSourceAttitudeLabel(attitude, ru);
        return ru ? $"\u041f\u043e\u0434\u0430\u0447\u0430: {label}" : $"Framing: {label}";
    }

    private float GetWorkerKnowledgeFormationStageHours(
        DriverAgent worker,
        WorkerMemory seed,
        WorkerKnowledgeFormationStage stage)
    {
        float hours = GetWorkerKnowledgeFormationStageHours(worker, seed?.Kind ?? WorkerMemoryKind.ConversationTopic, seed?.KnowledgeIteration ?? 1, stage);
        return ApplyWorkerHeritageKnowledgeFormationBias(worker, seed?.Kind ?? WorkerMemoryKind.ConversationTopic, seed?.BuildingType, GetWorkerMemoryHeritageTopicKey(seed), hours);
    }

    private float GetWorkerKnowledgeFormationStageHours(
        DriverAgent worker,
        PendingWorkerKnowledge pending,
        WorkerKnowledgeFormationStage stage)
    {
        float hours = GetWorkerKnowledgeFormationStageHours(worker, pending?.Kind ?? WorkerMemoryKind.ConversationTopic, pending?.KnowledgeIteration ?? 1, stage);
        return ApplyWorkerHeritageKnowledgeFormationBias(worker, pending?.Kind ?? WorkerMemoryKind.ConversationTopic, pending?.BuildingType, GetPendingWorkerHeritageTopicKey(pending), hours);
    }

    private static float GetWorkerKnowledgeFormationStageHours(
        DriverAgent worker,
        WorkerMemoryKind kind,
        int knowledgeIteration,
        WorkerKnowledgeFormationStage stage)
    {
        float hours = WorkerKnowledgeFormationBaseStageHours;
        if (kind == WorkerMemoryKind.ConversationTopic)
        {
            hours *= 0.86f;
        }

        hours += Mathf.Max(0, knowledgeIteration - 1) * 0.05f;
        if (stage == WorkerKnowledgeFormationStage.Judging)
        {
            hours += 0.18f;
        }

        if (HasWorkerTrait(worker, WorkerTraitKind.Curious))
        {
            hours *= 0.72f;
        }

        int seed = (worker?.DriverId ?? 0) * 43 + Mathf.Max(1, knowledgeIteration) * 17 + (int)stage * 11;
        float wobble = (Mathf.Abs(seed) % 9) / 100f;
        return Mathf.Clamp(hours + wobble, 0.18f, 1.15f);
    }

    private PendingWorkerKnowledge FindPendingWorkerKnowledge(DriverAgent worker, WorkerMemory memory)
    {
        if (worker == null || memory == null)
        {
            return null;
        }

        for (int i = 0; i < worker.PendingKnowledge.Count; i++)
        {
            PendingWorkerKnowledge pending = worker.PendingKnowledge[i];
            if (ArePendingWorkerKnowledgeEquivalent(pending, memory))
            {
                return pending;
            }
        }

        return null;
    }

    private static bool ArePendingWorkerKnowledgeEquivalent(PendingWorkerKnowledge pending, WorkerMemory memory)
    {
        if (pending == null || memory == null || pending.Kind != memory.Kind)
        {
            return false;
        }

        return pending.Kind switch
        {
            WorkerMemoryKind.ConversationTopic =>
                AreWorkerRumorsSameRoot(
                    pending.RumorRootId,
                    GetWorkerRumorOriginalTopic(pending),
                    memory.RumorRootId,
                    GetWorkerRumorOriginalTopic(memory)),
            WorkerMemoryKind.BuildingExistence =>
                pending.BuildingType == memory.BuildingType &&
                pending.BuildingInstanceId == memory.BuildingInstanceId,
            _ => false
        };
    }

    private bool HasEquivalentPendingWorkerKnowledge(DriverAgent worker, WorkerMemory memory)
    {
        return FindPendingWorkerKnowledge(worker, memory) != null;
    }

    private bool HasEquivalentFormedWorkerKnowledge(DriverAgent worker, WorkerMemory source, float now)
    {
        if (worker == null || source == null)
        {
            return true;
        }

        if (HasCityKnowledgeCanonEquivalent(source))
        {
            return true;
        }

        for (int i = 0; i < worker.Memories.Count; i++)
        {
            WorkerMemory existing = worker.Memories[i];
            if (existing == null ||
                !IsWorkerMemoryDisplayable(existing) ||
                ShouldExpireWorkerMemory(existing, now))
            {
                continue;
            }

            if (AreWorkerKnowledgeEquivalent(existing, source))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPendingWorkerKnowledgeDisplayable(PendingWorkerKnowledge pending)
    {
        if (pending == null)
        {
            return false;
        }

        return pending.Kind switch
        {
            WorkerMemoryKind.ConversationTopic => !string.IsNullOrWhiteSpace(GetWorkerRumorTopic(pending)),
            WorkerMemoryKind.BuildingExistence => pending.BuildingType.HasValue && pending.BuildingInstanceId > 0,
            _ => false
        };
    }

    private void TrimWorkerPendingKnowledge(DriverAgent worker)
    {
        while (worker != null && worker.PendingKnowledge.Count > WorkerPendingKnowledgeCap)
        {
            worker.PendingKnowledge.RemoveAt(worker.PendingKnowledge.Count - 1);
        }
    }

    private static string BuildWorkerKnowledgeFormationKey(WorkerMemory seed)
    {
        if (seed == null)
        {
            return string.Empty;
        }

        return seed.Kind == WorkerMemoryKind.BuildingExistence
            ? $"building|{seed.BuildingType}|{seed.BuildingInstanceId}"
            : seed.RumorRootId > 0
                ? $"topic-root|{seed.RumorRootId}"
                : $"topic|{NormalizeWorkerKnowledgeTopicKey(GetWorkerRumorOriginalTopic(seed))}";
    }

    private string FormatPendingWorkerKnowledgeDebugLabel(PendingWorkerKnowledge pending)
    {
        if (pending == null)
        {
            return "knowledge";
        }

        return pending.Kind == WorkerMemoryKind.BuildingExistence
            ? $"building {pending.BuildingType}/{pending.BuildingInstanceId}"
            : $"topic '{GetWorkerRumorTopic(pending)}'";
    }
}
