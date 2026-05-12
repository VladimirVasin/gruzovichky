using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerRumorMaxPercent = 100;
    private const int WorkerRumorPolarizationBaseChancePercent = 4;

    private void InitializeWorkerRumorState(
        WorkerMemory memory,
        DriverAgent owner,
        DriverAgent other,
        string topic,
        bool success,
        WorkerKnowledgeSourceAttitude sourceAttitude)
    {
        if (memory == null || memory.Kind != WorkerMemoryKind.ConversationTopic)
        {
            return;
        }

        string normalizedTopic = NormalizeWorkerRumorTopic(topic);
        memory.RumorRootId = BuildWorkerRumorRootId(owner?.DriverId ?? 0, other?.DriverId ?? memory.OtherWorkerId, normalizedTopic);
        memory.OriginalTopic = normalizedTopic;
        memory.RumorTopic = normalizedTopic;
        memory.Topic = normalizedTopic;
        memory.RumorDistortionPercent = 0;
        memory.RumorConnotationScore = GetInitialWorkerRumorConnotationScore(success, sourceAttitude);
        memory.RumorConnotationConfidence = GetInitialWorkerRumorConnotationConfidence(success, sourceAttitude);
    }

    private void AdvanceSharedWorkerRumorState(WorkerMemory received, WorkerMemory source, WorkerKnowledgeShareTransfer transfer, float now)
    {
        if (received == null || source == null || received.Kind != WorkerMemoryKind.ConversationTopic)
        {
            return;
        }

        string originalTopic = GetWorkerRumorOriginalTopic(source);
        string rumorTopic = GetWorkerRumorTopic(source);
        received.RumorRootId = source.RumorRootId > 0
            ? source.RumorRootId
            : BuildWorkerRumorRootId(source.FormedFromWorkerId, source.OtherWorkerId, originalTopic);
        received.OriginalTopic = originalTopic;
        received.RumorTopic = rumorTopic;
        received.Topic = rumorTopic;
        received.RumorDistortionPercent = CalculateSharedWorkerRumorDistortion(source, transfer, now);
        received.RumorConnotationScore = CalculateSharedWorkerRumorConnotation(source, transfer, now, out int confidence);
        received.RumorConnotationConfidence = confidence;
    }

    private static void CopyWorkerRumorState(PendingWorkerKnowledge pending, WorkerMemory source)
    {
        if (pending == null || source == null || pending.Kind != WorkerMemoryKind.ConversationTopic)
        {
            return;
        }

        pending.RumorRootId = source.RumorRootId;
        pending.ConversationTopicKey = string.IsNullOrWhiteSpace(source.ConversationTopicKey)
            ? BuildConversationTopicKey(GetWorkerRumorOriginalTopic(source))
            : source.ConversationTopicKey;
        pending.OriginalTopic = GetWorkerRumorOriginalTopic(source);
        pending.RumorTopic = GetWorkerRumorTopic(source);
        pending.Topic = pending.RumorTopic;
        pending.RumorDistortionPercent = Mathf.Clamp(source.RumorDistortionPercent, 0, WorkerRumorMaxPercent);
        pending.RumorConnotationScore = Mathf.Clamp(source.RumorConnotationScore, -WorkerRumorMaxPercent, WorkerRumorMaxPercent);
        pending.RumorConnotationConfidence = Mathf.Clamp(source.RumorConnotationConfidence, 0, WorkerRumorMaxPercent);
    }

    private static void CopyWorkerRumorState(WorkerMemory memory, PendingWorkerKnowledge source)
    {
        if (memory == null || source == null || memory.Kind != WorkerMemoryKind.ConversationTopic)
        {
            return;
        }

        memory.RumorRootId = source.RumorRootId;
        memory.ConversationTopicKey = string.IsNullOrWhiteSpace(source.ConversationTopicKey)
            ? BuildConversationTopicKey(GetWorkerRumorOriginalTopic(source))
            : source.ConversationTopicKey;
        memory.OriginalTopic = GetWorkerRumorOriginalTopic(source);
        memory.RumorTopic = GetWorkerRumorTopic(source);
        memory.Topic = memory.RumorTopic;
        memory.RumorDistortionPercent = Mathf.Clamp(source.RumorDistortionPercent, 0, WorkerRumorMaxPercent);
        memory.RumorConnotationScore = Mathf.Clamp(source.RumorConnotationScore, -WorkerRumorMaxPercent, WorkerRumorMaxPercent);
        memory.RumorConnotationConfidence = Mathf.Clamp(source.RumorConnotationConfidence, 0, WorkerRumorMaxPercent);
    }

    private static void CopyWorkerRumorState(NoosphereKnowledgeLogEntry entry, WorkerMemory source)
    {
        if (entry == null || source == null || source.Kind != WorkerMemoryKind.ConversationTopic)
        {
            return;
        }

        entry.RumorRootId = source.RumorRootId;
        entry.ConversationTopicKey = string.IsNullOrWhiteSpace(source.ConversationTopicKey)
            ? BuildConversationTopicKey(GetWorkerRumorOriginalTopic(source))
            : source.ConversationTopicKey;
        entry.OriginalTopic = GetWorkerRumorOriginalTopic(source);
        entry.RumorTopic = GetWorkerRumorTopic(source);
        entry.Topic = entry.RumorTopic;
        entry.RumorDistortionPercent = Mathf.Clamp(source.RumorDistortionPercent, 0, WorkerRumorMaxPercent);
        entry.RumorConnotationScore = Mathf.Clamp(source.RumorConnotationScore, -WorkerRumorMaxPercent, WorkerRumorMaxPercent);
        entry.RumorConnotationConfidence = Mathf.Clamp(source.RumorConnotationConfidence, 0, WorkerRumorMaxPercent);
    }

    private static void ApplyRumorConnotationOpinion(
        PendingWorkerKnowledge pending,
        ref int score,
        ref int confidence,
        ref string reasonRu,
        ref string reasonEn)
    {
        if (pending == null || pending.Kind != WorkerMemoryKind.ConversationTopic)
        {
            return;
        }

        int connotation = Mathf.Clamp(pending.RumorConnotationScore, -WorkerRumorMaxPercent, WorkerRumorMaxPercent);
        int connotationConfidence = Mathf.Clamp(pending.RumorConnotationConfidence, 0, WorkerRumorMaxPercent);
        score += Mathf.RoundToInt(connotation * 0.22f);
        confidence += Mathf.RoundToInt(connotationConfidence * 0.08f);

        if (Mathf.Abs(connotation) >= 65)
        {
            reasonRu = connotation > 0
                ? "\u0421\u043b\u0443\u0445 \u0434\u043e\u0448\u0435\u043b \u0441 \u044f\u0432\u043d\u043e \u0434\u043e\u0431\u0440\u043e\u0436\u0435\u043b\u0430\u0442\u0435\u043b\u044c\u043d\u043e\u0439 \u043a\u043e\u043d\u043d\u043e\u0442\u0430\u0446\u0438\u0435\u0439."
                : "\u0421\u043b\u0443\u0445 \u0434\u043e\u0448\u0435\u043b \u0441 \u044f\u0432\u043d\u043e \u0442\u0440\u0435\u0432\u043e\u0436\u043d\u043e\u0439 \u043a\u043e\u043d\u043d\u043e\u0442\u0430\u0446\u0438\u0435\u0439.";
            reasonEn = connotation > 0
                ? "The rumor arrived with a clearly favorable connotation."
                : "The rumor arrived with a clearly alarming connotation.";
        }

        if (pending.RumorDistortionPercent >= 70)
        {
            confidence -= 8;
        }
    }

    private static string GetWorkerRumorTopic(WorkerMemory memory)
    {
        if (memory == null)
        {
            return string.Empty;
        }

        return NormalizeWorkerRumorTopic(string.IsNullOrWhiteSpace(memory.RumorTopic) ? memory.Topic : memory.RumorTopic);
    }

    private static string GetWorkerRumorTopic(PendingWorkerKnowledge pending)
    {
        if (pending == null)
        {
            return string.Empty;
        }

        return NormalizeWorkerRumorTopic(string.IsNullOrWhiteSpace(pending.RumorTopic) ? pending.Topic : pending.RumorTopic);
    }

    private static string GetWorkerRumorTopic(NoosphereKnowledgeLogEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        return NormalizeWorkerRumorTopic(string.IsNullOrWhiteSpace(entry.RumorTopic) ? entry.Topic : entry.RumorTopic);
    }

    private static string GetWorkerRumorOriginalTopic(WorkerMemory memory)
    {
        if (memory == null)
        {
            return string.Empty;
        }

        string original = string.IsNullOrWhiteSpace(memory.OriginalTopic) ? memory.Topic : memory.OriginalTopic;
        return NormalizeWorkerRumorTopic(original);
    }

    private static string GetWorkerRumorOriginalTopic(PendingWorkerKnowledge pending)
    {
        if (pending == null)
        {
            return string.Empty;
        }

        string original = string.IsNullOrWhiteSpace(pending.OriginalTopic) ? pending.Topic : pending.OriginalTopic;
        return NormalizeWorkerRumorTopic(original);
    }

    private static string GetWorkerRumorOriginalTopic(NoosphereKnowledgeLogEntry entry)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        string original = string.IsNullOrWhiteSpace(entry.OriginalTopic) ? entry.Topic : entry.OriginalTopic;
        return NormalizeWorkerRumorTopic(original);
    }

    private static bool AreWorkerRumorsSameRoot(int firstRootId, string firstTopic, int secondRootId, string secondTopic)
    {
        if (firstRootId > 0 && secondRootId > 0)
        {
            return firstRootId == secondRootId;
        }

        return NormalizeWorkerKnowledgeTopicKey(firstTopic) == NormalizeWorkerKnowledgeTopicKey(secondTopic);
    }

    private static string FormatWorkerRumorStateMeta(WorkerMemory memory, bool ru)
    {
        if (memory == null || memory.Kind != WorkerMemoryKind.ConversationTopic)
        {
            return string.Empty;
        }

        return FormatWorkerRumorStateMeta(memory.RumorDistortionPercent, memory.RumorConnotationScore, memory.RumorConnotationConfidence, ru);
    }

    private static string FormatWorkerRumorStateMeta(PendingWorkerKnowledge pending, bool ru)
    {
        if (pending == null || pending.Kind != WorkerMemoryKind.ConversationTopic)
        {
            return string.Empty;
        }

        return FormatWorkerRumorStateMeta(pending.RumorDistortionPercent, pending.RumorConnotationScore, pending.RumorConnotationConfidence, ru);
    }

    private static string FormatWorkerRumorStateMeta(NoosphereKnowledgeLogEntry entry, bool ru)
    {
        if (entry == null || entry.MemoryKind != WorkerMemoryKind.ConversationTopic)
        {
            return string.Empty;
        }

        return FormatWorkerRumorStateMeta(entry.RumorDistortionPercent, entry.RumorConnotationScore, entry.RumorConnotationConfidence, ru);
    }

    private static string FormatWorkerRumorStateMeta(int distortionPercent, int connotationScore, int connotationConfidence, bool ru)
    {
        int distortion = Mathf.Clamp(distortionPercent, 0, WorkerRumorMaxPercent);
        int connotation = Mathf.Clamp(connotationScore, -WorkerRumorMaxPercent, WorkerRumorMaxPercent);
        int confidence = Mathf.Clamp(connotationConfidence, 0, WorkerRumorMaxPercent);
        string connotationLabel = FormatWorkerRumorConnotationLabel(connotation, ru);
        return ru
            ? $"\u0418\u0441\u043a\u0430\u0436\u0435\u043d\u0438\u0435: {distortion}%; \u041a\u043e\u043d\u043d\u043e\u0442\u0430\u0446\u0438\u044f: {connotationLabel} ({confidence}%)"
            : $"Distortion: {distortion}%; Connotation: {connotationLabel} ({confidence}%)";
    }

    private static string FormatWorkerRumorConnotationLabel(int connotationScore, bool ru)
    {
        int clamped = Mathf.Clamp(connotationScore, -WorkerRumorMaxPercent, WorkerRumorMaxPercent);
        if (clamped >= 100)
        {
            return ru ? "\u043f\u043e\u0437\u0438\u0442\u0438\u0432 100%" : "positive 100%";
        }

        if (clamped <= -100)
        {
            return ru ? "\u043d\u0435\u0433\u0430\u0442\u0438\u0432 100%" : "negative 100%";
        }

        if (clamped > 8)
        {
            return ru ? $"\u043f\u043e\u0437\u0438\u0442\u0438\u0432 +{clamped}" : $"positive +{clamped}";
        }

        if (clamped < -8)
        {
            return ru ? $"\u043d\u0435\u0433\u0430\u0442\u0438\u0432 {clamped}" : $"negative {clamped}";
        }

        return ru ? "\u043d\u0435\u0439\u0442\u0440\u0430\u043b\u044c\u043d\u0430\u044f" : "neutral";
    }

    private static int GetInitialWorkerRumorConnotationScore(bool success, WorkerKnowledgeSourceAttitude sourceAttitude)
    {
        int score = success ? 14 : -14;
        score += GetWorkerRumorSourceAttitudeBias(sourceAttitude);
        if (sourceAttitude == WorkerKnowledgeSourceAttitude.Positive && !success)
        {
            score -= 8;
        }
        else if (sourceAttitude == WorkerKnowledgeSourceAttitude.Negative && success)
        {
            score += 8;
        }

        return Mathf.Clamp(score, -WorkerRumorMaxPercent, WorkerRumorMaxPercent);
    }

    private static int GetInitialWorkerRumorConnotationConfidence(bool success, WorkerKnowledgeSourceAttitude sourceAttitude)
    {
        int confidence = 48 + (success ? 8 : 4);
        if (sourceAttitude != WorkerKnowledgeSourceAttitude.Neutral)
        {
            confidence += 14;
        }

        if (sourceAttitude == WorkerKnowledgeSourceAttitude.Positive && !success ||
            sourceAttitude == WorkerKnowledgeSourceAttitude.Negative && success)
        {
            confidence -= 12;
        }

        return Mathf.Clamp(confidence, 1, WorkerRumorMaxPercent);
    }

    private int CalculateSharedWorkerRumorDistortion(WorkerMemory source, WorkerKnowledgeShareTransfer transfer, float now)
    {
        int iteration = Mathf.Max(2, GetWorkerKnowledgeIteration(source) + 1);
        int seed = BuildWorkerRumorSpreadSeed(source, transfer, now, 101);
        int previous = Mathf.Clamp(source?.RumorDistortionPercent ?? 0, 0, WorkerRumorMaxPercent);
        int connotation = Mathf.Abs(source?.RumorConnotationScore ?? 0);
        int relation = GetWorkerRumorRelationship(transfer?.Receiver, transfer?.Sharer);

        int drift = 3 + PositiveModulo(seed, 8) + Mathf.RoundToInt(connotation * 0.05f);
        if (relation >= 30)
        {
            drift -= Mathf.Clamp(relation / 25, 1, 4);
        }
        else if (relation <= -20)
        {
            drift += Mathf.Clamp(Mathf.Abs(relation) / 18, 1, 5);
        }

        int result = previous + Mathf.Max(1, drift);
        int radicalChance = Mathf.Clamp(3 + iteration + previous / 10 + connotation / 25, 3, 34);
        if (PositiveModulo(seed / 17 + iteration * 19, 100) < radicalChance)
        {
            result += 25 + PositiveModulo(seed / 31, 55);
        }

        if (PositiveModulo(seed / 47 + iteration * 7, 100) < Mathf.Max(1, radicalChance / 5))
        {
            result = WorkerRumorMaxPercent;
        }

        return Mathf.Clamp(result, 0, WorkerRumorMaxPercent);
    }

    private int CalculateSharedWorkerRumorConnotation(WorkerMemory source, WorkerKnowledgeShareTransfer transfer, float now, out int confidence)
    {
        int iteration = Mathf.Max(2, GetWorkerKnowledgeIteration(source) + 1);
        int seed = BuildWorkerRumorSpreadSeed(source, transfer, now, 211);
        int previous = Mathf.Clamp(source?.RumorConnotationScore ?? 0, -WorkerRumorMaxPercent, WorkerRumorMaxPercent);
        int relation = GetWorkerRumorRelationship(transfer?.Receiver, transfer?.Sharer);
        int attitudeBias = GetWorkerRumorSourceAttitudeBias(source?.SourceAttitude ?? WorkerKnowledgeSourceAttitude.Neutral);
        int opinionBias = source?.OpinionScore ?? 0;
        int noise = PositiveModulo(seed, 25) - 12;

        int score = Mathf.RoundToInt(previous * 0.72f + opinionBias * 0.18f + attitudeBias * 0.12f + relation * 0.10f + noise);
        int polarizationChance = Mathf.Clamp(
            WorkerRumorPolarizationBaseChancePercent +
            iteration +
            Mathf.Abs(previous) / 15 +
            (source?.RumorDistortionPercent ?? 0) / 25,
            WorkerRumorPolarizationBaseChancePercent,
            42);

        if (PositiveModulo(seed / 11 + iteration * 23, 100) < polarizationChance)
        {
            int positiveWeight = Mathf.Clamp(50 + previous / 2 + (source?.Positive == true ? 8 : -8) + relation / 10, 8, 92);
            score = PositiveModulo(seed / 23 + iteration * 5, 100) < positiveWeight
                ? WorkerRumorMaxPercent
                : -WorkerRumorMaxPercent;
            confidence = WorkerRumorMaxPercent;
            return score;
        }

        confidence = Mathf.Clamp(
            (source?.RumorConnotationConfidence ?? 45) +
            Mathf.Clamp(iteration, 0, 10) +
            Mathf.Abs(score - previous) / 3 -
            Mathf.Max(0, (source?.RumorDistortionPercent ?? 0) - 65) / 8,
            1,
            WorkerRumorMaxPercent);
        return Mathf.Clamp(score, -WorkerRumorMaxPercent, WorkerRumorMaxPercent);
    }

    private int GetWorkerRumorRelationship(DriverAgent receiver, DriverAgent sharer)
    {
        WorkerSocialMemory memory = FindWorkerSocialMemory(receiver, sharer?.DriverId ?? 0);
        return memory?.Relationship ?? 0;
    }

    private static int GetWorkerRumorSourceAttitudeBias(WorkerKnowledgeSourceAttitude sourceAttitude)
    {
        return sourceAttitude switch
        {
            WorkerKnowledgeSourceAttitude.Positive => 34,
            WorkerKnowledgeSourceAttitude.Negative => -34,
            _ => 0
        };
    }

    private static int BuildWorkerRumorRootId(int firstWorkerId, int secondWorkerId, string topic)
    {
        unchecked
        {
            int low = Mathf.Min(firstWorkerId, secondWorkerId);
            int high = Mathf.Max(firstWorkerId, secondWorkerId);
            int hash = 23;
            hash = hash * 31 + low;
            hash = hash * 31 + high;
            string normalized = NormalizeWorkerKnowledgeTopicKey(topic);
            for (int i = 0; i < normalized.Length; i++)
            {
                hash = hash * 31 + normalized[i];
            }

            return Mathf.Max(1, hash & 0x7fffffff);
        }
    }

    private static int BuildWorkerRumorSpreadSeed(WorkerMemory source, WorkerKnowledgeShareTransfer transfer, float now, int salt)
    {
        unchecked
        {
            int seed = salt;
            seed = seed * 31 + (source?.RumorRootId ?? 0);
            seed = seed * 31 + (transfer?.Sharer?.DriverId ?? 0);
            seed = seed * 31 + (transfer?.Receiver?.DriverId ?? 0);
            seed = seed * 31 + GetWorkerKnowledgeIteration(source);
            seed = seed * 31 + Mathf.FloorToInt(now * 4f);
            seed = seed * 31 + (source?.RumorDistortionPercent ?? 0);
            seed = seed * 31 + (source?.RumorConnotationScore ?? 0);
            return seed;
        }
    }

    private static int PositiveModulo(int value, int modulo)
    {
        if (modulo <= 0)
        {
            return 0;
        }

        int result = value % modulo;
        return result < 0 ? result + modulo : result;
    }

    private static string NormalizeWorkerRumorTopic(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return string.Empty;
        }

        string normalized = topic.Trim();
        while (normalized.Contains("  "))
        {
            normalized = normalized.Replace("  ", " ");
        }

        return normalized;
    }
}
