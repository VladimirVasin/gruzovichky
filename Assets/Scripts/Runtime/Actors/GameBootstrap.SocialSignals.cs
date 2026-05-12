using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int SocialSignalHistoryCap = 640;
    private const int SocialSignalNoosphereTopicLimit = 5;

    private readonly List<SocialSignal> socialSignals = new();
    private int nextSocialSignalId = 1;

    private sealed class SocialSignalTopicAggregate
    {
        public string Key = string.Empty;
        public string LabelRu = string.Empty;
        public string LabelEn = string.Empty;
        public int Score;
        public int Strength;
        public int ConfidenceTotal;
        public int Count;
        public SocialSignalCategory Category = SocialSignalCategory.City;
    }

    private SocialSignal RecordSocialSignal(
        DriverAgent worker,
        SocialSignalCategory category,
        SocialSignalSourceKind sourceKind,
        SocialSignalTone tone,
        int strength,
        int confidence,
        string topicKey,
        string topicLabelRu,
        string topicLabelEn,
        string reasonRu,
        string reasonEn,
        string sourceKey = null,
        LocationType? locationType = null,
        int locationInstanceId = 0,
        bool includeInDailyExperience = true,
        bool publicForNoosphere = true,
        float dedupeHours = 0f,
        int dayOverride = 0,
        int dailyScoreHint = 0,
        int cellX = int.MinValue,
        int cellY = int.MinValue)
    {
        int workerId = worker?.DriverId ?? 0;
        float now = GetCurrentWorldHour();
        int signalDay = dayOverride > 0 ? dayOverride : currentDay;
        string normalizedTopic = NormalizeSocialSignalTopicKey(topicKey, topicLabelEn, category);
        string normalizedSource = sourceKey ?? string.Empty;
        if (dedupeHours > 0f &&
            HasRecentEquivalentSocialSignal(workerId, sourceKind, normalizedSource, normalizedTopic, signalDay, now, dedupeHours))
        {
            return null;
        }

        SocialSignal signal = new()
        {
            Id = nextSocialSignalId++,
            WorkerId = workerId,
            WorkerName = string.IsNullOrWhiteSpace(worker?.DriverName) ? string.Empty : worker.DriverName,
            Day = signalDay,
            WorldHour = now,
            TopicKey = normalizedTopic,
            TopicLabelRu = string.IsNullOrWhiteSpace(topicLabelRu) ? GetSocialSignalCategoryLabel(category, true) : topicLabelRu.Trim(),
            TopicLabelEn = string.IsNullOrWhiteSpace(topicLabelEn) ? GetSocialSignalCategoryLabel(category, false) : topicLabelEn.Trim(),
            Tone = tone,
            Strength = Mathf.Clamp(strength, 0, 100),
            Confidence = Mathf.Clamp(confidence, 0, 100),
            Category = category,
            SourceKind = sourceKind,
            SourceKey = normalizedSource,
            LocationType = locationType,
            LocationInstanceId = Mathf.Max(0, locationInstanceId),
            ReasonRu = reasonRu ?? string.Empty,
            ReasonEn = reasonEn ?? string.Empty,
            DailyScoreHint = Mathf.Clamp(dailyScoreHint, -100, 100),
            IncludeInDailyExperience = includeInDailyExperience,
            PublicForNoosphere = publicForNoosphere
        };

        if (cellX != int.MinValue && cellY != int.MinValue)
        {
            signal.HasCell = true;
            signal.Cell = new Vector2Int(cellX, cellY);
        }

        socialSignals.Insert(0, signal);
        while (socialSignals.Count > SocialSignalHistoryCap)
        {
            socialSignals.RemoveAt(socialSignals.Count - 1);
        }

        if (publicForNoosphere)
        {
            isNoosphereScreenDirty = true;
            noosphereVisualDirty = true;
            MarkNoosphereDiveDirty();
        }

        SessionDebugLogger.LogVerbose(
            "SOCIAL_SIGNAL",
            $"#{signal.Id} worker={signal.WorkerId} topic={signal.TopicKey} tone={signal.Tone} strength={signal.Strength} source={signal.SourceKind}/{signal.SourceKey}.");
        return signal;
    }

    private bool HasRecentEquivalentSocialSignal(
        int workerId,
        SocialSignalSourceKind sourceKind,
        string sourceKey,
        string topicKey,
        int day,
        float now,
        float dedupeHours)
    {
        for (int i = 0; i < socialSignals.Count; i++)
        {
            SocialSignal signal = socialSignals[i];
            if (signal == null)
            {
                continue;
            }

            if (signal.Day != day)
            {
                break;
            }

            if (signal.WorkerId == workerId &&
                signal.SourceKind == sourceKind &&
                string.Equals(signal.SourceKey, sourceKey, System.StringComparison.Ordinal) &&
                string.Equals(signal.TopicKey, topicKey, System.StringComparison.Ordinal) &&
                Mathf.Abs(now - signal.WorldHour) <= dedupeHours)
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeSocialSignalTopicKey(string topicKey, string fallbackLabel, SocialSignalCategory category)
    {
        string key = !string.IsNullOrWhiteSpace(topicKey) ? topicKey : fallbackLabel;
        if (string.IsNullOrWhiteSpace(key))
        {
            key = category.ToString();
        }

        return NormalizeWorkerKnowledgeTopicKey(key);
    }

    private void RecordSocialSignalFromWorkerThought(
        DriverAgent worker,
        WorkerThought thought,
        WorkerThoughtSubjectType subjectType,
        int subjectId,
        string subjectKey,
        string fallbackLabel)
    {
        if (worker == null || thought == null)
        {
            return;
        }

        SocialSignalCategory category = MapWorkerThoughtKindToSocialSignalCategory(thought.Kind);
        string topicKey = BuildWorkerThoughtSocialSignalTopicKey(thought, subjectType, subjectKey);
        string topicLabelEn = string.IsNullOrWhiteSpace(fallbackLabel) ? topicKey : fallbackLabel;
        string topicLabelRu = ResolveWorkerThoughtSubjectLabel(subjectType, subjectId, subjectKey, fallbackLabel, true);
        LocationType? locationType = worker.IsInsideBuilding ? worker.InsideBuildingType : null;
        int locationInstanceId = worker.IsInsideBuilding ? worker.InsideBuildingInstanceId : 0;

        RecordSocialSignal(
            worker,
            category,
            SocialSignalSourceKind.Thought,
            MapWorkerThoughtToneToSocialSignalTone(thought.Tone),
            thought.Intensity,
            GetSocialSignalConfidenceForThought(thought),
            topicKey,
            topicLabelRu,
            topicLabelEn,
            RenderWorkerThought(thought, true),
            RenderWorkerThought(thought, false),
            sourceKey: $"thought:{thought.Key}",
            locationType: locationType,
            locationInstanceId: locationInstanceId,
            includeInDailyExperience: false,
            publicForNoosphere: true,
            dedupeHours: 0f);
    }

    private SocialSignal RecordWorkerDailyStreetLitterSocialSignal(
        DriverAgent worker,
        int day,
        int severity,
        int dailyScore,
        string reasonRu,
        string reasonEn)
    {
        int strength = severity switch
        {
            1 => 18,
            2 => 38,
            3 => 62,
            _ => 84
        };

        return RecordSocialSignal(
            worker,
            SocialSignalCategory.Litter,
            SocialSignalSourceKind.StreetLitter,
            SocialSignalTone.Negative,
            strength,
            Mathf.Clamp(46 + severity * 11, 1, 96),
            "street_litter",
            "\u043c\u0443\u0441\u043e\u0440 \u043d\u0430 \u0443\u043b\u0438\u0446\u0430\u0445",
            "street litter",
            reasonRu,
            reasonEn,
            sourceKey: $"street_litter_daily:{day}:{severity}",
            includeInDailyExperience: false,
            publicForNoosphere: true,
            dedupeHours: 24f,
            dayOverride: day,
            dailyScoreHint: dailyScore);
    }

    private void AddDailySocialSignalFactors(DriverAgent worker, int day, List<WorkerDailyOpinionFactor> factors)
    {
        if (worker == null || factors == null)
        {
            return;
        }

        Dictionary<SocialSignalCategory, SocialSignalTopicAggregate> buckets = new();
        for (int i = 0; i < socialSignals.Count; i++)
        {
            SocialSignal signal = socialSignals[i];
            if (signal == null)
            {
                continue;
            }

            if (signal.Day < day)
            {
                break;
            }

            if (signal.Day != day ||
                signal.WorkerId != worker.DriverId ||
                !signal.IncludeInDailyExperience)
            {
                continue;
            }

            int score = CalculateDailySocialSignalScore(signal);
            if (score == 0)
            {
                continue;
            }

            if (!buckets.TryGetValue(signal.Category, out SocialSignalTopicAggregate bucket))
            {
                bucket = new SocialSignalTopicAggregate
                {
                    Key = signal.Category.ToString(),
                    LabelRu = GetSocialSignalCategoryLabel(signal.Category, true),
                    LabelEn = GetSocialSignalCategoryLabel(signal.Category, false),
                    Category = signal.Category
                };
                buckets[signal.Category] = bucket;
            }

            bucket.Score += score;
            bucket.Strength += Mathf.Abs(score);
            bucket.ConfidenceTotal += signal.Confidence;
            bucket.Count++;
            if (Mathf.Abs(score) >= Mathf.Abs(bucket.Score))
            {
                bucket.LabelRu = string.IsNullOrWhiteSpace(signal.ReasonRu) ? signal.TopicLabelRu : signal.ReasonRu;
                bucket.LabelEn = string.IsNullOrWhiteSpace(signal.ReasonEn) ? signal.TopicLabelEn : signal.ReasonEn;
            }
        }

        foreach (SocialSignalTopicAggregate bucket in buckets.Values)
        {
            if (bucket.Count <= 0 || bucket.Score == 0)
            {
                continue;
            }

            AddDailyOpinionFactor(
                factors,
                MapSocialSignalCategoryToDailyFactorKind(bucket.Category),
                Mathf.Clamp(bucket.Score, -36, 36),
                bucket.LabelRu,
                bucket.LabelEn);
        }
    }

    private static int CalculateDailySocialSignalScore(SocialSignal signal)
    {
        if (signal == null || signal.Tone == SocialSignalTone.Neutral)
        {
            return 0;
        }

        if (signal.DailyScoreHint != 0)
        {
            return signal.DailyScoreHint;
        }

        int direction = signal.Tone == SocialSignalTone.Positive ? 1 : -1;
        float confidence = Mathf.Lerp(0.20f, 0.46f, Mathf.Clamp01(signal.Confidence / 100f));
        int magnitude = Mathf.Clamp(Mathf.RoundToInt(signal.Strength * confidence), 1, 32);
        return direction * magnitude;
    }

    private void ApplyTopicOpinionSocialSignals(
        DriverAgent worker,
        PendingWorkerKnowledge pending,
        ref int score,
        ref int confidence,
        ref int strongestReasonMagnitude,
        ref string reasonRu,
        ref string reasonEn)
    {
        if (worker == null || pending == null)
        {
            return;
        }

        string originalTopic = GetWorkerRumorOriginalTopic(pending);
        string currentTopic = GetWorkerRumorTopic(pending);
        int signalScore = 0;
        int signalConfidence = 0;
        int count = 0;
        int latestDay = Mathf.Max(1, currentDay - 3);
        for (int i = 0; i < socialSignals.Count; i++)
        {
            SocialSignal signal = socialSignals[i];
            if (signal == null)
            {
                continue;
            }

            if (signal.Day < latestDay)
            {
                break;
            }

            if (signal.WorkerId != worker.DriverId ||
                signal.SourceKind == SocialSignalSourceKind.Thought ||
                signal.Tone == SocialSignalTone.Neutral ||
                (!DoesSocialSignalMatchConversationTopic(signal, originalTopic) &&
                 !DoesSocialSignalMatchConversationTopic(signal, currentTopic)))
            {
                continue;
            }

            int signed = signal.Tone == SocialSignalTone.Positive ? 1 : -1;
            signalScore += signed * Mathf.Clamp(Mathf.RoundToInt(signal.Strength * 0.18f), 1, 18);
            signalConfidence += Mathf.Clamp(signal.Confidence / 10, 1, 10);
            count++;
        }

        if (count <= 0 || signalScore == 0)
        {
            return;
        }

        signalScore = Mathf.Clamp(signalScore, -24, 24);
        AddTopicOpinionComponent(
            signalScore,
            Mathf.Clamp(signalConfidence, 1, 20),
            signalScore >= 0
                ? "\u043b\u0438\u0447\u043d\u044b\u0435 \u0441\u0438\u0433\u043d\u0430\u043b\u044b \u0432\u043e\u043a\u0440\u0443\u0433 \u0442\u0435\u043c\u044b \u0443\u0436\u0435 \u0441\u043a\u043b\u0430\u0434\u044b\u0432\u0430\u043b\u0438\u0441\u044c \u0432 \u043f\u043b\u044e\u0441"
                : "\u043b\u0438\u0447\u043d\u044b\u0435 \u0441\u0438\u0433\u043d\u0430\u043b\u044b \u0432\u043e\u043a\u0440\u0443\u0433 \u0442\u0435\u043c\u044b \u0443\u0436\u0435 \u0441\u043a\u043b\u0430\u0434\u044b\u0432\u0430\u043b\u0438\u0441\u044c \u0432 \u043c\u0438\u043d\u0443\u0441",
            signalScore >= 0
                ? "personal signals around the topic already leaned positive"
                : "personal signals around the topic already leaned negative",
            ref score,
            ref confidence,
            ref strongestReasonMagnitude,
            ref reasonRu,
            ref reasonEn,
            replaceReasonThreshold: 10);
    }

    private void RecordWorkerTopicOpinionSocialSignal(DriverAgent worker, WorkerMemory memory, WorkerTopicOpinion opinion)
    {
        if (worker == null || memory == null || opinion == null)
        {
            return;
        }

        string originalTopic = GetWorkerRumorOriginalTopic(memory);
        string currentTopic = GetWorkerRumorTopic(memory);
        RecordSocialSignal(
            worker,
            SocialSignalCategory.Topic,
            SocialSignalSourceKind.TopicOpinion,
            MapKnowledgeToneToSocialSignalTone(opinion.Tone),
            Mathf.Abs(opinion.Score),
            opinion.Confidence,
            originalTopic,
            string.IsNullOrWhiteSpace(originalTopic) ? currentTopic : originalTopic,
            string.IsNullOrWhiteSpace(originalTopic) ? currentTopic : originalTopic,
            opinion.ReasonRu,
            opinion.ReasonEn,
            sourceKey: $"topic:{opinion.TopicKey}:{opinion.LastKnowledgeIteration}",
            includeInDailyExperience: true,
            publicForNoosphere: true,
            dedupeHours: 0.20f);
    }

    private void RecordCityComplaintSocialSignals(
        CityComplaint complaint,
        SocialSignalSourceKind sourceKind,
        SocialSignalTone tone,
        int strength,
        int confidence,
        string sourceKeySuffix,
        string reasonRu,
        string reasonEn,
        bool includeInDailyExperience)
    {
        if (complaint == null)
        {
            return;
        }

        string topicKey = BuildCityComplaintSocialSignalTopicKey(complaint, sourceKind);
        string topicLabel = BuildCityComplaintSocialSignalTopicLabel(complaint);
        int signerCount = Mathf.Max(1, complaint.SignerIds.Count);
        for (int i = 0; i < signerCount; i++)
        {
            DriverAgent signer = i < complaint.SignerIds.Count ? GetDriverAgentById(complaint.SignerIds[i]) : GetDriverAgentById(complaint.WorkerId);
            if (signer == null)
            {
                continue;
            }

            RecordSocialSignal(
                signer,
                sourceKind == SocialSignalSourceKind.CityHallDecision ? SocialSignalCategory.Governance : MapCityComplaintCategoryToSocialSignalCategory(complaint.Category),
                sourceKind,
                tone,
                strength,
                confidence,
                topicKey,
                topicLabel,
                topicLabel,
                reasonRu,
                reasonEn,
                sourceKey: $"complaint:{complaint.Id}:{sourceKeySuffix}",
                locationType: LocationType.CityHall,
                includeInDailyExperience: includeInDailyExperience,
                publicForNoosphere: true,
                dedupeHours: 0.25f);
        }
    }

    private string FormatLatestSocialSignalNoosphereSummary(bool ru)
    {
        int latestDay = GetLatestSocialSignalDay();
        if (latestDay <= 0)
        {
            return ru
                ? "\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u0438\u0435 \u0441\u0438\u0433\u043d\u0430\u043b\u044b \u0435\u0449\u0435 \u043d\u0435 \u0441\u043e\u0431\u0440\u0430\u043d\u044b."
                : "City social signals have not gathered yet.";
        }

        List<SocialSignalTopicAggregate> aggregates = BuildSocialSignalTopicAggregates(latestDay, publicOnly: true);
        if (aggregates.Count == 0)
        {
            return ru
                ? "\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u0438\u0435 \u0441\u0438\u0433\u043d\u0430\u043b\u044b \u0437\u0430 \u0434\u0435\u043d\u044c \u0431\u044b\u043b\u0438 \u0442\u0438\u0445\u0438\u043c\u0438."
                : "City social signals were quiet for the day.";
        }

        int totalCount = 0;
        for (int i = 0; i < aggregates.Count; i++)
        {
            totalCount += aggregates[i].Count;
        }

        int limit = Mathf.Min(SocialSignalNoosphereTopicLimit, aggregates.Count);
        List<string> parts = new();
        for (int i = 0; i < limit; i++)
        {
            SocialSignalTopicAggregate aggregate = aggregates[i];
            string label = ru ? aggregate.LabelRu : aggregate.LabelEn;
            string score = $"{aggregate.Score:+#;-#;0}";
            parts.Add($"{label} {score}");
        }

        return ru
            ? $"\u0421\u0438\u0433\u043d\u0430\u043b\u044b \u0434\u043d\u044f {latestDay}: {totalCount}; \u0433\u043b\u0430\u0432\u043d\u044b\u0435 \u0442\u0435\u043c\u044b: {string.Join(", ", parts)}."
            : $"Day {latestDay} signals: {totalCount}; main topics: {string.Join(", ", parts)}.";
    }

    private void AddNoosphereDiveSocialSignalMeanings(Dictionary<string, NoosphereDiveMeaningModel> meanings, bool ru)
    {
        int latestDay = GetLatestSocialSignalDay();
        if (latestDay <= 0)
        {
            return;
        }

        List<SocialSignalTopicAggregate> aggregates = BuildSocialSignalTopicAggregates(latestDay, publicOnly: true);
        int limit = Mathf.Min(SocialSignalNoosphereTopicLimit, aggregates.Count);
        for (int i = 0; i < limit; i++)
        {
            SocialSignalTopicAggregate aggregate = aggregates[i];
            AddNoosphereDiveMeaning(
                meanings,
                ru ? aggregate.LabelRu : aggregate.LabelEn,
                Mathf.Clamp(aggregate.Score, -100, 100),
                Mathf.Clamp(aggregate.ConfidenceTotal / Mathf.Max(1, aggregate.Count), 0, 100),
                Mathf.Clamp(aggregate.Count + Mathf.Abs(aggregate.Score) / 12, 2, 10),
                NoosphereDiveMeaningKind.SocialSignal,
                canon: false,
                burned: false);
        }
    }

    private int CountPublicNoosphereSocialSignals()
    {
        int count = 0;
        for (int i = 0; i < socialSignals.Count; i++)
        {
            if (socialSignals[i]?.PublicForNoosphere == true)
            {
                count++;
            }
        }

        return count;
    }

    private List<SocialSignalTopicAggregate> BuildSocialSignalTopicAggregates(int day, bool publicOnly)
    {
        Dictionary<string, SocialSignalTopicAggregate> byKey = new();
        for (int i = 0; i < socialSignals.Count; i++)
        {
            SocialSignal signal = socialSignals[i];
            if (signal == null)
            {
                continue;
            }

            if (signal.Day < day)
            {
                break;
            }

            if (signal.Day != day ||
                publicOnly && !signal.PublicForNoosphere ||
                signal.Tone == SocialSignalTone.Neutral)
            {
                continue;
            }

            string key = string.IsNullOrWhiteSpace(signal.TopicKey) ? signal.Category.ToString() : signal.TopicKey;
            if (!byKey.TryGetValue(key, out SocialSignalTopicAggregate aggregate))
            {
                aggregate = new SocialSignalTopicAggregate
                {
                    Key = key,
                    LabelRu = string.IsNullOrWhiteSpace(signal.TopicLabelRu) ? GetSocialSignalCategoryLabel(signal.Category, true) : signal.TopicLabelRu,
                    LabelEn = string.IsNullOrWhiteSpace(signal.TopicLabelEn) ? GetSocialSignalCategoryLabel(signal.Category, false) : signal.TopicLabelEn,
                    Category = signal.Category
                };
                byKey[key] = aggregate;
            }

            int signed = signal.Tone == SocialSignalTone.Positive ? 1 : -1;
            int strength = signal.DailyScoreHint != 0 ? Mathf.Abs(signal.DailyScoreHint) : Mathf.Clamp(signal.Strength, 1, 100);
            aggregate.Score += signed * strength;
            aggregate.Strength += strength;
            aggregate.ConfidenceTotal += signal.Confidence;
            aggregate.Count++;
        }

        List<SocialSignalTopicAggregate> aggregates = new(byKey.Values);
        aggregates.Sort((a, b) =>
        {
            int strengthCompare = b.Strength.CompareTo(a.Strength);
            return strengthCompare != 0 ? strengthCompare : b.Count.CompareTo(a.Count);
        });
        return aggregates;
    }

    private int GetLatestSocialSignalDay()
    {
        for (int i = 0; i < socialSignals.Count; i++)
        {
            if (socialSignals[i]?.PublicForNoosphere == true)
            {
                return socialSignals[i].Day;
            }
        }

        return -1;
    }

    private static SocialSignalCategory MapWorkerThoughtKindToSocialSignalCategory(WorkerThoughtKind kind)
    {
        return kind switch
        {
            WorkerThoughtKind.Need => SocialSignalCategory.Need,
            WorkerThoughtKind.Work => SocialSignalCategory.Work,
            WorkerThoughtKind.Money => SocialSignalCategory.Money,
            WorkerThoughtKind.Social => SocialSignalCategory.Social,
            WorkerThoughtKind.Family => SocialSignalCategory.Family,
            WorkerThoughtKind.Transport => SocialSignalCategory.Transport,
            _ => SocialSignalCategory.City
        };
    }

    private static WorkerDailyOpinionFactorKind MapSocialSignalCategoryToDailyFactorKind(SocialSignalCategory category)
    {
        return category switch
        {
            SocialSignalCategory.Need => WorkerDailyOpinionFactorKind.Need,
            SocialSignalCategory.Work => WorkerDailyOpinionFactorKind.Work,
            SocialSignalCategory.Money => WorkerDailyOpinionFactorKind.Money,
            SocialSignalCategory.Social => WorkerDailyOpinionFactorKind.Social,
            SocialSignalCategory.Family => WorkerDailyOpinionFactorKind.Family,
            SocialSignalCategory.Transport => WorkerDailyOpinionFactorKind.Transport,
            SocialSignalCategory.Housing => WorkerDailyOpinionFactorKind.Housing,
            _ => WorkerDailyOpinionFactorKind.City
        };
    }

    private static SocialSignalCategory MapCityComplaintCategoryToSocialSignalCategory(CityComplaintCategory category)
    {
        return category switch
        {
            CityComplaintCategory.NeedPressure => SocialSignalCategory.Need,
            CityComplaintCategory.NoJob => SocialSignalCategory.Work,
            CityComplaintCategory.LowMoney => SocialSignalCategory.Money,
            CityComplaintCategory.FamilyStress => SocialSignalCategory.Family,
            CityComplaintCategory.SocialIntroduction => SocialSignalCategory.Social,
            _ => SocialSignalCategory.City
        };
    }

    private static SocialSignalTone MapWorkerThoughtToneToSocialSignalTone(WorkerThoughtTone tone)
    {
        return tone switch
        {
            WorkerThoughtTone.Positive => SocialSignalTone.Positive,
            WorkerThoughtTone.Negative => SocialSignalTone.Negative,
            _ => SocialSignalTone.Neutral
        };
    }

    private static SocialSignalTone MapKnowledgeToneToSocialSignalTone(WorkerKnowledgeOpinionTone tone)
    {
        return tone switch
        {
            WorkerKnowledgeOpinionTone.Positive => SocialSignalTone.Positive,
            WorkerKnowledgeOpinionTone.Negative => SocialSignalTone.Negative,
            _ => SocialSignalTone.Neutral
        };
    }

    private static int GetSocialSignalConfidenceForThought(WorkerThought thought)
    {
        if (thought == null)
        {
            return 0;
        }

        int baseConfidence = thought.Priority switch
        {
            WorkerThoughtPriority.Critical => 88,
            WorkerThoughtPriority.High => 74,
            WorkerThoughtPriority.Normal => 58,
            _ => 42
        };
        return Mathf.Clamp(baseConfidence + thought.Intensity / 8, 1, 100);
    }

    private static string BuildWorkerThoughtSocialSignalTopicKey(
        WorkerThought thought,
        WorkerThoughtSubjectType subjectType,
        string subjectKey)
    {
        if (!string.IsNullOrWhiteSpace(subjectKey))
        {
            return $"{subjectType}:{subjectKey}";
        }

        return string.IsNullOrWhiteSpace(thought?.TemplateKey)
            ? "thought"
            : $"thought:{thought.TemplateKey}";
    }

    private string ResolveWorkerThoughtSubjectLabel(
        WorkerThoughtSubjectType subjectType,
        int subjectId,
        string subjectKey,
        string fallbackLabel,
        bool ru)
    {
        if (!string.IsNullOrWhiteSpace(fallbackLabel))
        {
            return fallbackLabel;
        }

        if (subjectType == WorkerThoughtSubjectType.BuildingType &&
            System.Enum.TryParse(subjectKey, out LocationType locationType))
        {
            return GetSelectedLocationDisplayName(locationType);
        }

        if (subjectType == WorkerThoughtSubjectType.Need)
        {
            return FormatWorkerThoughtNeed(subjectKey, ru);
        }

        return string.IsNullOrWhiteSpace(subjectKey) ? GetSocialSignalCategoryLabel(SocialSignalCategory.City, ru) : subjectKey;
    }

    private string BuildCityComplaintSocialSignalTopicKey(CityComplaint complaint, SocialSignalSourceKind sourceKind)
    {
        if (complaint == null)
        {
            return "city_hall";
        }

        if (sourceKind == SocialSignalSourceKind.CityHallDecision)
        {
            return "governance:city_hall";
        }

        return complaint.Category switch
        {
            CityComplaintCategory.ServiceMissing when complaint.LinkedLocationType.HasValue => $"service:{complaint.LinkedLocationType.Value}",
            CityComplaintCategory.NeedPressure when complaint.LinkedNeed.HasValue => $"need:{complaint.LinkedNeed.Value}",
            CityComplaintCategory.NoJob => "work:jobs",
            CityComplaintCategory.LowMoney => "money",
            CityComplaintCategory.FamilyStress => "family",
            CityComplaintCategory.SocialIntroduction => "social:introduction",
            _ => "city"
        };
    }

    private string BuildCityComplaintSocialSignalTopicLabel(CityComplaint complaint)
    {
        if (complaint == null)
        {
            return "City Hall";
        }

        if (complaint.LinkedLocationType.HasValue)
        {
            return FormatCityComplaintTargetName(complaint);
        }

        return complaint.Category switch
        {
            CityComplaintCategory.NeedPressure => complaint.LinkedNeed.HasValue ? FormatWorkerThoughtNeed(complaint.LinkedNeed.Value.ToString(), IsRussianLanguage()) : "needs",
            CityComplaintCategory.NoJob => IsRussianLanguage() ? "\u0440\u0430\u0431\u043e\u0442\u0430" : "work",
            CityComplaintCategory.LowMoney => IsRussianLanguage() ? "\u0434\u0435\u043d\u044c\u0433\u0438" : "money",
            CityComplaintCategory.FamilyStress => IsRussianLanguage() ? "\u0441\u0435\u043c\u044c\u044f" : "family",
            CityComplaintCategory.SocialIntroduction => IsRussianLanguage() ? "\u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440" : "conversation",
            _ => IsRussianLanguage() ? "\u0433\u043e\u0440\u043e\u0434" : "city"
        };
    }

    private static string GetSocialSignalCategoryLabel(SocialSignalCategory category, bool ru)
    {
        return category switch
        {
            SocialSignalCategory.Need => ru ? "\u043f\u043e\u0442\u0440\u0435\u0431\u043d\u043e\u0441\u0442\u0438" : "needs",
            SocialSignalCategory.Work => ru ? "\u0440\u0430\u0431\u043e\u0442\u0430" : "work",
            SocialSignalCategory.Money => ru ? "\u0434\u0435\u043d\u044c\u0433\u0438" : "money",
            SocialSignalCategory.Social => ru ? "\u0441\u043e\u0446\u0438\u0430\u043b\u044c\u043d\u043e\u0441\u0442\u044c" : "social",
            SocialSignalCategory.Family => ru ? "\u0441\u0435\u043c\u044c\u044f" : "family",
            SocialSignalCategory.Transport => ru ? "\u0442\u0440\u0430\u043d\u0441\u043f\u043e\u0440\u0442" : "transport",
            SocialSignalCategory.Housing => ru ? "\u0436\u0438\u043b\u044c\u0435" : "housing",
            SocialSignalCategory.Litter => ru ? "\u043c\u0443\u0441\u043e\u0440" : "litter",
            SocialSignalCategory.Governance => ru ? "\u0443\u043f\u0440\u0430\u0432\u043b\u0435\u043d\u0438\u0435" : "governance",
            SocialSignalCategory.Knowledge => ru ? "\u0437\u043d\u0430\u043d\u0438\u0435" : "knowledge",
            SocialSignalCategory.Topic => ru ? "\u0442\u0435\u043c\u0430" : "topic",
            _ => ru ? "\u0433\u043e\u0440\u043e\u0434" : "city"
        };
    }

    private static bool DoesSocialSignalMatchConversationTopic(SocialSignal signal, string topic)
    {
        if (signal == null || string.IsNullOrWhiteSpace(topic))
        {
            return false;
        }

        string target = NormalizeWorkerKnowledgeTopicKey(topic);
        string key = NormalizeWorkerKnowledgeTopicKey(signal.TopicKey);
        string labelRu = NormalizeWorkerKnowledgeTopicKey(signal.TopicLabelRu);
        string labelEn = NormalizeWorkerKnowledgeTopicKey(signal.TopicLabelEn);
        if (DoesNormalizedSignalTextMatch(key, target) ||
            DoesNormalizedSignalTextMatch(labelRu, target) ||
            DoesNormalizedSignalTextMatch(labelEn, target))
        {
            return true;
        }

        return DoesSocialSignalAliasMatch(signal, target);
    }

    private static bool DoesNormalizedSignalTextMatch(string signalText, string target)
    {
        if (string.IsNullOrWhiteSpace(signalText) || string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        return string.Equals(signalText, target, System.StringComparison.Ordinal) ||
               signalText.Contains(target, System.StringComparison.Ordinal) ||
               target.Contains(signalText, System.StringComparison.Ordinal);
    }

    private static bool DoesSocialSignalAliasMatch(SocialSignal signal, string target)
    {
        if (signal.Category == SocialSignalCategory.Litter)
        {
            return target.Contains("LITTER", System.StringComparison.Ordinal) ||
                   target.Contains("TRASH", System.StringComparison.Ordinal) ||
                   target.Contains("GARBAGE", System.StringComparison.Ordinal) ||
                   target.Contains("\u041c\u0423\u0421\u041e\u0420", System.StringComparison.Ordinal) ||
                   target.Contains("\u0413\u0420\u042f\u0417", System.StringComparison.Ordinal);
        }

        if (signal.Category == SocialSignalCategory.Governance)
        {
            return target.Contains("CITY HALL", System.StringComparison.Ordinal) ||
                   target.Contains("GOVERN", System.StringComparison.Ordinal) ||
                   target.Contains("\u041c\u042d\u0420", System.StringComparison.Ordinal) ||
                   target.Contains("\u0420\u0410\u0422\u0423\u0428", System.StringComparison.Ordinal);
        }

        if (signal.Category == SocialSignalCategory.Work)
        {
            return target.Contains("WORK", System.StringComparison.Ordinal) ||
                   target.Contains("JOB", System.StringComparison.Ordinal) ||
                   target.Contains("\u0420\u0410\u0411\u041e\u0422", System.StringComparison.Ordinal);
        }

        if (signal.Category == SocialSignalCategory.Transport)
        {
            return target.Contains("BUS", System.StringComparison.Ordinal) ||
                   target.Contains("TRANSPORT", System.StringComparison.Ordinal) ||
                   target.Contains("\u0422\u0420\u0410\u041d\u0421\u041f\u041e\u0420\u0422", System.StringComparison.Ordinal) ||
                   target.Contains("\u0410\u0412\u0422\u041e\u0411\u0423\u0421", System.StringComparison.Ordinal);
        }

        if (signal.Category == SocialSignalCategory.Money)
        {
            return target.Contains("MONEY", System.StringComparison.Ordinal) ||
                   target.Contains("\u0414\u0415\u041d\u042c\u0413", System.StringComparison.Ordinal);
        }

        return false;
    }
}
