using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerTopicOpinionCap = 16;
    private const int WorkerTopicOpinionMinimumFinalMagnitude = 15;

    private sealed class WorkerTopicOpinionEvaluation
    {
        public WorkerKnowledgeOpinionTone Tone = WorkerKnowledgeOpinionTone.Neutral;
        public int Score;
        public int Confidence;
        public string ReasonRu = string.Empty;
        public string ReasonEn = string.Empty;
        public bool ContradictsPrevious;
    }

    private bool TryEvaluateWorkerTopicOpinion(DriverAgent worker, PendingWorkerKnowledge pending)
    {
        if (worker == null ||
            pending == null ||
            pending.Kind != WorkerMemoryKind.ConversationTopic ||
            string.IsNullOrWhiteSpace(GetWorkerRumorTopic(pending)))
        {
            return false;
        }

        DriverAgent sourceWorker = GetDriverAgentById(pending.SourceWorkerId);
        WorkerTopicOpinionEvaluation evaluation = BuildWorkerTopicOpinionEvaluation(worker, pending, sourceWorker);
        ApplyWorkerTopicOpinionEvaluation(pending, evaluation);
        return true;
    }

    private void CommitWorkerTopicOpinion(
        DriverAgent worker,
        DriverAgent sourceWorker,
        WorkerMemory memory,
        PendingWorkerKnowledge pending,
        float now)
    {
        if (worker == null ||
            memory == null ||
            pending == null ||
            memory.Kind != WorkerMemoryKind.ConversationTopic)
        {
            return;
        }

        WorkerTopicOpinionEvaluation evaluation = BuildWorkerTopicOpinionEvaluation(worker, pending, sourceWorker);
        WorkerTopicOpinion opinion = FindWorkerTopicOpinion(worker, pending);
        if (opinion == null)
        {
            opinion = new WorkerTopicOpinion();
            worker.TopicOpinions.Insert(0, opinion);
        }
        else
        {
            worker.TopicOpinions.Remove(opinion);
            worker.TopicOpinions.Insert(0, opinion);
        }

        opinion.TopicKey = BuildWorkerTopicOpinionKey(pending.RumorRootId, GetWorkerRumorOriginalTopic(pending));
        opinion.ConversationTopicKey = string.IsNullOrWhiteSpace(pending.ConversationTopicKey)
            ? BuildConversationTopicKey(GetWorkerRumorOriginalTopic(pending))
            : pending.ConversationTopicKey;
        opinion.RumorRootId = pending.RumorRootId;
        opinion.OriginalTopic = GetWorkerRumorOriginalTopic(pending);
        opinion.CurrentTopic = GetWorkerRumorTopic(pending);
        opinion.Tone = evaluation.Tone;
        opinion.Score = evaluation.Score;
        opinion.Confidence = evaluation.Confidence;
        opinion.ReasonRu = evaluation.ReasonRu;
        opinion.ReasonEn = evaluation.ReasonEn;
        opinion.TimesHeard++;
        if (evaluation.Score >= WorkerTopicOpinionMinimumFinalMagnitude)
        {
            opinion.PositiveSignalCount++;
        }
        else if (evaluation.Score <= -WorkerTopicOpinionMinimumFinalMagnitude)
        {
            opinion.NegativeSignalCount++;
        }

        if (evaluation.ContradictsPrevious)
        {
            opinion.ContradictionCount++;
        }

        opinion.LastSourceWorkerId = sourceWorker?.DriverId ?? pending.SourceWorkerId;
        opinion.LastSourceInteractionKind = pending.SourceInteractionKind;
        opinion.LastKnowledgeIteration = Mathf.Max(1, pending.KnowledgeIteration);
        opinion.LastUpdatedDay = currentDay;
        opinion.LastUpdatedWorldHour = now;

        ApplyWorkerTopicOpinionEvaluation(pending, evaluation);
        ApplyWorkerTopicOpinionEvaluation(memory, evaluation);
        memory.RumorConnotationScore = Mathf.Clamp(
            Mathf.RoundToInt(memory.RumorConnotationScore * 0.45f + evaluation.Score * 0.55f),
            -WorkerRumorMaxPercent,
            WorkerRumorMaxPercent);
        memory.RumorConnotationConfidence = Mathf.Clamp(
            Mathf.Max(memory.RumorConnotationConfidence, evaluation.Confidence),
            1,
            WorkerRumorMaxPercent);

        while (worker.TopicOpinions.Count > WorkerTopicOpinionCap)
        {
            worker.TopicOpinions.RemoveAt(worker.TopicOpinions.Count - 1);
        }

        RecordConversationTopicOpinion(opinion);
        RecordWorkerTopicOpinionSocialSignal(worker, memory, opinion);
        SessionDebugLogger.LogVerbose(
            "TOPIC_OPINION",
            $"{GetWorkerDisplayNameSafe(worker)} formed topic opinion root={memory.RumorRootId}, topic='{GetWorkerRumorTopic(memory)}', score={evaluation.Score}, confidence={evaluation.Confidence}, source={GetWorkerDisplayNameSafe(sourceWorker)}.");
    }

    private WorkerTopicOpinionEvaluation BuildWorkerTopicOpinionEvaluation(
        DriverAgent worker,
        PendingWorkerKnowledge pending,
        DriverAgent sourceWorker)
    {
        int score = pending.Positive ? 10 : -12;
        int confidence = pending.Positive ? 42 : 38;
        int strongestReasonMagnitude = Mathf.Abs(score);
        string reasonRu = pending.Positive
            ? "разговор прошел без неловкости"
            : "первый разговор оставил неловкий след";
        string reasonEn = pending.Positive
            ? "the conversation did not stumble"
            : "the first conversation left an awkward trace";

        AddTopicOpinionComponent(
            GetWorkerTopicSourceAttitudeSignal(pending.SourceAttitude),
            pending.SourceAttitude == WorkerKnowledgeSourceAttitude.Neutral ? 0 : 7,
            pending.SourceAttitude == WorkerKnowledgeSourceAttitude.Positive
                ? "игрок подал тему как поддерживающую"
                : "игрок подал тему как предупреждение",
            pending.SourceAttitude == WorkerKnowledgeSourceAttitude.Positive
                ? "the player framed the topic as supportive"
                : "the player framed the topic as a warning",
            ref score,
            ref confidence,
            ref strongestReasonMagnitude,
            ref reasonRu,
            ref reasonEn);

        int rumorSignal = Mathf.RoundToInt(Mathf.Clamp(pending.RumorConnotationScore, -WorkerRumorMaxPercent, WorkerRumorMaxPercent) * 0.42f);
        AddTopicOpinionComponent(
            rumorSignal,
            Mathf.RoundToInt(Mathf.Clamp(pending.RumorConnotationConfidence, 0, WorkerRumorMaxPercent) * 0.14f),
            rumorSignal >= 0 ? "слух пришел с теплой коннотацией" : "слух пришел с тревожной коннотацией",
            rumorSignal >= 0 ? "the rumor arrived with a warm connotation" : "the rumor arrived with a worrying connotation",
            ref score,
            ref confidence,
            ref strongestReasonMagnitude,
            ref reasonRu,
            ref reasonEn);

        WorkerTopicOpinion sourceOpinion = FindWorkerTopicOpinion(sourceWorker, pending);
        if (sourceOpinion != null && sourceOpinion.Confidence >= 20)
        {
            int sourceSignal = Mathf.RoundToInt(sourceOpinion.Score * Mathf.Lerp(0.18f, 0.42f, sourceOpinion.Confidence / 100f));
            AddTopicOpinionComponent(
                sourceSignal,
                Mathf.Clamp(sourceOpinion.Confidence / 5, 0, 18),
                sourceSignal >= 0 ? "источник уже говорил о теме позитивно" : "источник уже говорил о теме негативно",
                sourceSignal >= 0 ? "the source already carried a positive stance" : "the source already carried a negative stance",
                ref score,
                ref confidence,
                ref strongestReasonMagnitude,
                ref reasonRu,
                ref reasonEn);
        }

        ApplyTopicOpinionSocialTrust(worker, pending, ref score, ref confidence, ref strongestReasonMagnitude, ref reasonRu, ref reasonEn);
        ApplyTopicOpinionLivedExperience(worker, ref score, ref confidence, ref strongestReasonMagnitude, ref reasonRu, ref reasonEn);
        ApplyTopicOpinionCityExperience(ref score, ref confidence, ref strongestReasonMagnitude, ref reasonRu, ref reasonEn);
        ApplyTopicOpinionSocialSignals(worker, pending, ref score, ref confidence, ref strongestReasonMagnitude, ref reasonRu, ref reasonEn);

        int disposition = CalculateWorkerTopicOpinionDispositionBias(worker, pending);
        AddTopicOpinionComponent(
            disposition,
            HasWorkerPerk(worker, WorkerPerkKind.Quicklearner) ? 5 : 1,
            disposition >= 0 ? "личная предрасположенность сдвинула оценку в плюс" : "личная предрасположенность сдвинула оценку в минус",
            disposition >= 0 ? "personal disposition nudged the stance positive" : "personal disposition nudged the stance negative",
            ref score,
            ref confidence,
            ref strongestReasonMagnitude,
            ref reasonRu,
            ref reasonEn,
            replaceReasonThreshold: 13);

        WorkerTopicOpinion previous = FindWorkerTopicOpinion(worker, pending);
        bool contradictsPrevious = false;
        if (previous != null)
        {
            int previousScore = Mathf.Clamp(previous.Score, -100, 100);
            int previousWeight = Mathf.Clamp(previous.Confidence, 25, 82);
            int newWeight = Mathf.Clamp(118 - previousWeight, 36, 92);
            contradictsPrevious =
                Mathf.Abs(previousScore) >= WorkerTopicOpinionMinimumFinalMagnitude &&
                Mathf.Abs(score) >= WorkerTopicOpinionMinimumFinalMagnitude &&
                GetWorkerTopicOpinionSign(previousScore) != GetWorkerTopicOpinionSign(score);

            score = Mathf.RoundToInt((previousScore * previousWeight + score * newWeight) / (float)(previousWeight + newWeight));
            confidence += Mathf.Clamp(previous.Confidence / 4 + previous.TimesHeard * 3, 0, 24);
            if (contradictsPrevious)
            {
                confidence -= Mathf.Clamp(14 + previous.ContradictionCount * 4, 14, 30);
                reasonRu = "новая версия спорит с прежней позицией, поэтому мнение пересобирается осторожно";
                reasonEn = "the new version conflicts with the old stance, so the opinion is being rebuilt carefully";
            }
            else if (Mathf.Abs(previousScore) >= WorkerTopicOpinionMinimumFinalMagnitude)
            {
                int reinforcement = GetWorkerTopicOpinionSign(previousScore) * Mathf.Clamp(previous.TimesHeard * 2, 2, 10);
                score += reinforcement;
                reasonRu = previousScore >= 0
                    ? "похожие версии уже повторялись и закрепили позитивное отношение"
                    : "похожие версии уже повторялись и закрепили негативное отношение";
                reasonEn = previousScore >= 0
                    ? "similar versions repeated and reinforced a positive stance"
                    : "similar versions repeated and reinforced a negative stance";
            }
        }

        confidence += Mathf.Clamp(Mathf.Max(1, pending.KnowledgeIteration) * 2, 2, 14);
        confidence -= Mathf.Clamp(Mathf.Max(0, pending.RumorDistortionPercent - 55) / 5, 0, 9);
        score = Mathf.Clamp(score, -100, 100);
        if (Mathf.Abs(score) < WorkerTopicOpinionMinimumFinalMagnitude)
        {
            int sign = score != 0 ? GetWorkerTopicOpinionSign(score) : disposition >= 0 ? 1 : -1;
            score = sign * (WorkerTopicOpinionMinimumFinalMagnitude + Mathf.Abs(disposition) % 7);
            confidence = Mathf.Max(confidence, 36);
            reasonRu = sign > 0
                ? "смысл темы не известен системе, но опыт и источник сдвинули итог в позитив"
                : "смысл темы не известен системе, но опыт и источник сдвинули итог в негатив";
            reasonEn = sign > 0
                ? "the system does not know the topic meaning, but experience and source nudged the result positive"
                : "the system does not know the topic meaning, but experience and source nudged the result negative";
        }

        WorkerKnowledgeOpinionTone tone = GetWorkerKnowledgeOpinionTone(score);
        return new WorkerTopicOpinionEvaluation
        {
            Tone = tone,
            Score = Mathf.Clamp(score, -100, 100),
            Confidence = Mathf.Clamp(confidence, 1, 100),
            ReasonRu = FormatWorkerTopicOpinionReason(tone, reasonRu, true),
            ReasonEn = FormatWorkerTopicOpinionReason(tone, reasonEn, false),
            ContradictsPrevious = contradictsPrevious
        };
    }

    private void ApplyTopicOpinionSocialTrust(
        DriverAgent worker,
        PendingWorkerKnowledge pending,
        ref int score,
        ref int confidence,
        ref int strongestReasonMagnitude,
        ref string reasonRu,
        ref string reasonEn)
    {
        WorkerSocialMemory otherMemory = FindWorkerSocialMemory(worker, pending.OtherWorkerId);
        if (otherMemory == null)
        {
            return;
        }

        int trust = Mathf.Clamp(otherMemory.Relationship, -100, 100);
        int direction = Mathf.Abs(score) >= 8 ? GetWorkerTopicOpinionSign(score) : pending.Positive ? 1 : -1;
        int trustSignal = Mathf.RoundToInt(trust / 5f) * direction;
        AddTopicOpinionComponent(
            trustSignal,
            Mathf.Clamp(otherMemory.Familiarity / 4 + otherMemory.InteractionCount * 2, 0, 22),
            trust >= 0 ? "собеседнику доверяет, поэтому версия звучит убедительнее" : "собеседнику не доверяет, поэтому версия вызывает сопротивление",
            trust >= 0 ? "the speaker is trusted, so the version sounds stronger" : "the speaker is not trusted, so the version meets resistance",
            ref score,
            ref confidence,
            ref strongestReasonMagnitude,
            ref reasonRu,
            ref reasonEn,
            replaceReasonThreshold: 10);
    }

    private static void ApplyTopicOpinionLivedExperience(
        DriverAgent worker,
        ref int score,
        ref int confidence,
        ref int strongestReasonMagnitude,
        ref string reasonRu,
        ref string reasonEn)
    {
        WorkerDailyOpinion daily = GetLatestWorkerDailyOpinion(worker);
        if (daily == null)
        {
            return;
        }

        int dailySignal = Mathf.RoundToInt(Mathf.Clamp(daily.Score, -100, 100) * 0.14f);
        AddTopicOpinionComponent(
            dailySignal,
            Mathf.Clamp(daily.Confidence / 12, 0, 8),
            dailySignal >= 0 ? "пережитый опыт был спокойным и делает тему безопаснее" : "пережитый опыт был тяжелым и делает тему тревожнее",
            dailySignal >= 0 ? "lived experience was steady and makes the topic feel safer" : "lived experience was hard and makes the topic feel more worrying",
            ref score,
            ref confidence,
            ref strongestReasonMagnitude,
            ref reasonRu,
            ref reasonEn,
            replaceReasonThreshold: 8);
    }

    private static void AddTopicOpinionComponent(
        int componentScore,
        int componentConfidence,
        string componentReasonRu,
        string componentReasonEn,
        ref int score,
        ref int confidence,
        ref int strongestReasonMagnitude,
        ref string reasonRu,
        ref string reasonEn,
        int replaceReasonThreshold = 12)
    {
        if (componentScore == 0 && componentConfidence == 0)
        {
            return;
        }

        score += componentScore;
        confidence += componentConfidence;
        int magnitude = Mathf.Abs(componentScore);
        if (magnitude >= replaceReasonThreshold && magnitude >= strongestReasonMagnitude)
        {
            strongestReasonMagnitude = magnitude;
            reasonRu = componentReasonRu;
            reasonEn = componentReasonEn;
        }
    }

    private static int GetWorkerTopicSourceAttitudeSignal(WorkerKnowledgeSourceAttitude attitude)
    {
        return attitude switch
        {
            WorkerKnowledgeSourceAttitude.Positive => 18,
            WorkerKnowledgeSourceAttitude.Negative => -18,
            _ => 0
        };
    }

    private static int CalculateWorkerTopicOpinionDispositionBias(DriverAgent worker, PendingWorkerKnowledge pending)
    {
        int seed = 37;
        seed = seed * 31 + (worker?.DriverId ?? 0);
        string key = NormalizeWorkerKnowledgeTopicKey(GetWorkerRumorOriginalTopic(pending));
        for (int i = 0; i < key.Length; i++)
        {
            seed = seed * 31 + key[i];
        }

        int bias = PositiveModulo(seed, 25) - 12;
        if (HasWorkerPerk(worker, WorkerPerkKind.Socialite) && pending?.Positive == true)
        {
            bias += 3;
        }

        if (HasWorkerPerk(worker, WorkerPerkKind.Frugal) && pending?.SourceInteractionKind == WorkerSocialInteractionKind.PlayerPromptedConversationFailed)
        {
            bias -= 3;
        }

        return Mathf.Clamp(bias, -15, 15);
    }

    private static string FormatWorkerTopicOpinionReason(WorkerKnowledgeOpinionTone tone, string basis, bool ru)
    {
        bool positive = tone == WorkerKnowledgeOpinionTone.Positive;
        string cleanBasis = string.IsNullOrWhiteSpace(basis)
            ? positive
                ? ru ? "сигналы сложились в плюс" : "signals leaned positive"
                : ru ? "сигналы сложились в минус" : "signals leaned negative"
            : basis.Trim();
        return ru
            ? $"{(positive ? "позитивно" : "негативно")}: {cleanBasis}"
            : $"{(positive ? "positive" : "negative")}: {cleanBasis}";
    }

    private static int GetWorkerTopicOpinionSign(int score)
    {
        return score >= 0 ? 1 : -1;
    }

    private static WorkerTopicOpinion FindWorkerTopicOpinion(DriverAgent worker, PendingWorkerKnowledge pending)
    {
        return FindWorkerTopicOpinion(worker, pending?.RumorRootId ?? 0, GetWorkerRumorOriginalTopic(pending));
    }

    private bool CanRefreshWorkerTopicOpinionFromKnowledge(DriverAgent receiver, WorkerMemory source, float now)
    {
        if (receiver == null ||
            source == null ||
            source.Kind != WorkerMemoryKind.ConversationTopic ||
            HasEquivalentPendingWorkerKnowledge(receiver, source) ||
            !TryFindEquivalentWorkerMemory(receiver, source, now, out WorkerMemory existing))
        {
            return false;
        }

        if (GetWorkerKnowledgeIteration(source) > GetWorkerKnowledgeIteration(existing))
        {
            return true;
        }

        if (Mathf.Abs(source.RumorConnotationScore - existing.RumorConnotationScore) >= 18 ||
            Mathf.Abs(source.OpinionScore - existing.OpinionScore) >= 20)
        {
            return true;
        }

        WorkerTopicOpinion opinion = FindWorkerTopicOpinion(receiver, source.RumorRootId, GetWorkerRumorOriginalTopic(source));
        return opinion != null &&
               opinion.TimesHeard < 6 &&
               now - opinion.LastUpdatedWorldHour >= 6f;
    }

    private bool TryAbsorbRepeatedWorkerTopicKnowledge(WorkerKnowledgeShareTransfer transfer, float now)
    {
        if (transfer?.Sharer == null ||
            transfer.Receiver == null ||
            transfer.SourceMemory == null ||
            transfer.SourceMemory.Kind != WorkerMemoryKind.ConversationTopic ||
            !TryFindEquivalentWorkerMemory(transfer.Receiver, transfer.SourceMemory, now, out WorkerMemory existing))
        {
            return false;
        }

        WorkerMemory received = CreateSharedWorkerMemory(transfer, now);
        if (!IsWorkerMemoryDisplayable(received))
        {
            return false;
        }

        PendingWorkerKnowledge pending = new()
        {
            FormationKey = BuildWorkerKnowledgeFormationKey(received),
            Kind = received.Kind,
            StartedDay = currentDay,
            StartedWorldHour = now,
            StageStartedWorldHour = now,
            NextStageWorldHour = now,
            Stage = WorkerKnowledgeFormationStage.Judging
        };
        ApplyWorkerKnowledgeSeed(pending, transfer.Sharer, received, transfer.ShareKind, transfer.ShareLocationType, now);
        RefreshPendingWorkerKnowledgeOpinion(transfer.Receiver, pending);

        existing.OtherWorkerId = received.OtherWorkerId;
        existing.Topic = received.Topic;
        existing.SourceRu = received.SourceRu;
        existing.SourceEn = received.SourceEn;
        existing.Positive = received.Positive;
        existing.KnowledgeIteration = Mathf.Max(GetWorkerKnowledgeIteration(existing), GetWorkerKnowledgeIteration(received));
        existing.SourceAttitude = received.SourceAttitude;
        existing.FormedFromWorkerId = received.FormedFromWorkerId;
        existing.FormationCompletedWorldHour = now;
        existing.RumorRootId = received.RumorRootId;
        existing.OriginalTopic = GetWorkerRumorOriginalTopic(received);
        existing.RumorTopic = GetWorkerRumorTopic(received);
        existing.RumorDistortionPercent = Mathf.Clamp(
            Mathf.Max(existing.RumorDistortionPercent, received.RumorDistortionPercent),
            0,
            WorkerRumorMaxPercent);
        existing.RumorConnotationScore = received.RumorConnotationScore;
        existing.RumorConnotationConfidence = Mathf.Max(existing.RumorConnotationConfidence, received.RumorConnotationConfidence);

        CommitWorkerTopicOpinion(transfer.Receiver, transfer.Sharer, existing, pending, now);
        QueueWorkerKnowledgeReflectionThought(transfer.Receiver, transfer.Sharer, existing, now);
        isDriversScreenDirty = true;
        isNoosphereScreenDirty = true;
        SessionDebugLogger.LogVerbose(
            "TOPIC_OPINION",
            $"{GetWorkerDisplayNameSafe(transfer.Receiver)} refreshed topic opinion from repeated rumor '{GetWorkerRumorTopic(existing)}' via {GetWorkerDisplayNameSafe(transfer.Sharer)}.");
        return true;
    }

    private static WorkerTopicOpinion FindWorkerTopicOpinion(DriverAgent worker, int rumorRootId, string originalTopic)
    {
        if (worker == null)
        {
            return null;
        }

        string key = BuildWorkerTopicOpinionKey(rumorRootId, originalTopic);
        for (int i = 0; i < worker.TopicOpinions.Count; i++)
        {
            WorkerTopicOpinion opinion = worker.TopicOpinions[i];
            if (opinion == null)
            {
                continue;
            }

            if (rumorRootId > 0 && opinion.RumorRootId == rumorRootId ||
                string.Equals(opinion.TopicKey, key, System.StringComparison.Ordinal))
            {
                return opinion;
            }
        }

        return null;
    }

    private static string BuildWorkerTopicOpinionKey(int rumorRootId, string originalTopic)
    {
        string normalized = NormalizeWorkerKnowledgeTopicKey(originalTopic);
        return rumorRootId > 0
            ? $"root:{rumorRootId}"
            : $"topic:{normalized}";
    }

    private static void ApplyWorkerTopicOpinionEvaluation(PendingWorkerKnowledge pending, WorkerTopicOpinionEvaluation evaluation)
    {
        if (pending == null || evaluation == null)
        {
            return;
        }

        pending.OpinionTone = evaluation.Tone;
        pending.OpinionScore = evaluation.Score;
        pending.OpinionConfidence = evaluation.Confidence;
        pending.OpinionReasonRu = evaluation.ReasonRu;
        pending.OpinionReasonEn = evaluation.ReasonEn;
    }

    private static void ApplyWorkerTopicOpinionEvaluation(WorkerMemory memory, WorkerTopicOpinionEvaluation evaluation)
    {
        if (memory == null || evaluation == null)
        {
            return;
        }

        memory.OpinionTone = evaluation.Tone;
        memory.OpinionScore = evaluation.Score;
        memory.OpinionConfidence = evaluation.Confidence;
        memory.OpinionReasonRu = evaluation.ReasonRu;
        memory.OpinionReasonEn = evaluation.ReasonEn;
    }

    private static void CopyWorkerKnowledgeOpinionState(WorkerMemory memory, PendingWorkerKnowledge source)
    {
        if (memory == null || source == null)
        {
            return;
        }

        memory.OpinionTone = source.OpinionTone;
        memory.OpinionScore = source.OpinionScore;
        memory.OpinionConfidence = source.OpinionConfidence;
        memory.OpinionReasonRu = source.OpinionReasonRu ?? string.Empty;
        memory.OpinionReasonEn = source.OpinionReasonEn ?? string.Empty;
    }

    private static void CopyWorkerKnowledgeOpinionState(PendingWorkerKnowledge pending, WorkerMemory source)
    {
        if (pending == null || source == null)
        {
            return;
        }

        pending.OpinionTone = source.OpinionTone;
        pending.OpinionScore = source.OpinionScore;
        pending.OpinionConfidence = source.OpinionConfidence;
        pending.OpinionReasonRu = source.OpinionReasonRu ?? string.Empty;
        pending.OpinionReasonEn = source.OpinionReasonEn ?? string.Empty;
    }
}
