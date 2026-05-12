using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int NoosphereSocialSignalReasonLimit = 3;

    private sealed class NoosphereSocialSignalInsight
    {
        public int Day;
        public int Count;
        public int PositiveCount;
        public int NegativeCount;
        public int NeutralCount;
        public int Score;
        public int Strength;
        public NoosphereSocialSignalTopicInsight TopPositive;
        public NoosphereSocialSignalTopicInsight TopNegative;
        public NoosphereSocialSignalTopicInsight TopSplit;
        public NoosphereSocialSignalReasonInsight TopReason;
        public NoosphereSocialSignalSourceInsight TopTensionSource;
        public readonly List<NoosphereSocialSignalTopicInsight> Topics = new();
        public readonly List<NoosphereSocialSignalReasonInsight> Reasons = new();
    }

    private sealed class NoosphereSocialSignalTopicInsight
    {
        public string Key = string.Empty;
        public string LabelRu = string.Empty;
        public string LabelEn = string.Empty;
        public SocialSignalCategory Category = SocialSignalCategory.City;
        public int Count;
        public int PositiveCount;
        public int NegativeCount;
        public int NeutralCount;
        public int Score;
        public int Strength;
        public int ConfidenceTotal;
    }

    private sealed class NoosphereSocialSignalReasonInsight
    {
        public string Key = string.Empty;
        public string TextRu = string.Empty;
        public string TextEn = string.Empty;
        public int Count;
        public int Strength;
        public int Score;
    }

    private sealed class NoosphereSocialSignalSourceInsight
    {
        public string Key = string.Empty;
        public string Label = string.Empty;
        public int Count;
        public int Strength;
    }

    private string FormatNoosphereSocialSignalInsight(bool ru)
    {
        int latestDay = GetLatestSocialSignalDay();
        if (latestDay <= 0)
        {
            return ru
                ? "\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u0438\u0435 \u0441\u0438\u0433\u043d\u0430\u043b\u044b \u0435\u0449\u0435 \u043d\u0435 \u0441\u043e\u0431\u0440\u0430\u043d\u044b."
                : "City social signals have not gathered yet.";
        }

        NoosphereSocialSignalInsight insight = BuildNoosphereSocialSignalInsight(latestDay);
        if (insight.Count == 0)
        {
            return ru
                ? "\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u0438\u0435 \u0441\u0438\u0433\u043d\u0430\u043b\u044b \u0437\u0430 \u0434\u0435\u043d\u044c \u0431\u044b\u043b\u0438 \u0442\u0438\u0445\u0438\u043c\u0438."
                : "City social signals were quiet for the day.";
        }

        string tone = FormatNoosphereSocialSignalTone(insight, ru);
        string topics = FormatNoosphereSocialSignalTopics(insight, ru);
        string reasons = FormatNoosphereSocialSignalReasons(insight, ru);
        string tension = FormatNoosphereSocialSignalTension(insight, ru);
        return ru
            ? $"\u0421\u0438\u0433\u043d\u0430\u043b\u044b \u0434\u043d\u044f {insight.Day}: {insight.Count}; \u043e\u0431\u0449\u0438\u0439 \u0442\u043e\u043d: {tone}; \u0440\u0430\u0441\u043a\u043e\u043b +{insight.PositiveCount}/-{insight.NegativeCount}. \u0422\u0435\u043c\u044b: {topics}. {reasons} {tension}"
            : $"Day {insight.Day} signals: {insight.Count}; overall tone: {tone}; split +{insight.PositiveCount}/-{insight.NegativeCount}. Topics: {topics}. {reasons} {tension}";
    }

    private NoosphereSocialSignalInsight BuildNoosphereSocialSignalInsight(int day)
    {
        NoosphereSocialSignalInsight insight = new() { Day = day };
        Dictionary<string, NoosphereSocialSignalTopicInsight> topics = new();
        Dictionary<string, NoosphereSocialSignalReasonInsight> reasons = new();
        Dictionary<string, NoosphereSocialSignalSourceInsight> tensionSources = new();

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

            if (signal.Day != day || !signal.PublicForNoosphere)
            {
                continue;
            }

            int signedStrength = GetNoosphereSocialSignalSignedStrength(signal);
            int strength = Mathf.Abs(signedStrength);
            insight.Count++;
            insight.Score += signedStrength;
            insight.Strength += strength;
            if (signal.Tone == SocialSignalTone.Positive)
            {
                insight.PositiveCount++;
            }
            else if (signal.Tone == SocialSignalTone.Negative)
            {
                insight.NegativeCount++;
            }
            else
            {
                insight.NeutralCount++;
            }

            NoosphereSocialSignalTopicInsight topic = GetOrCreateNoosphereSocialSignalTopic(topics, signal);
            AddNoosphereSocialSignalToTopic(topic, signal, signedStrength, strength);
            AddNoosphereSocialSignalReason(reasons, signal, signedStrength, strength);
            AddNoosphereSocialSignalTensionSource(tensionSources, signal, strength);
        }

        insight.Topics.AddRange(topics.Values);
        insight.Topics.Sort(CompareNoosphereSocialSignalTopics);
        insight.Reasons.AddRange(reasons.Values);
        insight.Reasons.Sort(CompareNoosphereSocialSignalReasons);
        SelectNoosphereSocialSignalHighlights(insight, tensionSources);
        return insight;
    }

    private static int GetNoosphereSocialSignalSignedStrength(SocialSignal signal)
    {
        if (signal == null || signal.Tone == SocialSignalTone.Neutral)
        {
            return 0;
        }

        int strength = signal.DailyScoreHint != 0
            ? Mathf.Abs(signal.DailyScoreHint)
            : Mathf.Clamp(signal.Strength, 1, 100);
        return signal.Tone == SocialSignalTone.Positive ? strength : -strength;
    }

    private static NoosphereSocialSignalTopicInsight GetOrCreateNoosphereSocialSignalTopic(
        Dictionary<string, NoosphereSocialSignalTopicInsight> topics,
        SocialSignal signal)
    {
        string key = string.IsNullOrWhiteSpace(signal.TopicKey) ? signal.Category.ToString() : signal.TopicKey;
        if (topics.TryGetValue(key, out NoosphereSocialSignalTopicInsight topic))
        {
            return topic;
        }

        topic = new NoosphereSocialSignalTopicInsight
        {
            Key = key,
            LabelRu = string.IsNullOrWhiteSpace(signal.TopicLabelRu) ? GetSocialSignalCategoryLabel(signal.Category, true) : signal.TopicLabelRu,
            LabelEn = string.IsNullOrWhiteSpace(signal.TopicLabelEn) ? GetSocialSignalCategoryLabel(signal.Category, false) : signal.TopicLabelEn,
            Category = signal.Category
        };
        topics[key] = topic;
        return topic;
    }

    private static void AddNoosphereSocialSignalToTopic(
        NoosphereSocialSignalTopicInsight topic,
        SocialSignal signal,
        int signedStrength,
        int strength)
    {
        topic.Count++;
        topic.Score += signedStrength;
        topic.Strength += strength;
        topic.ConfidenceTotal += signal.Confidence;
        if (signal.Tone == SocialSignalTone.Positive)
        {
            topic.PositiveCount++;
        }
        else if (signal.Tone == SocialSignalTone.Negative)
        {
            topic.NegativeCount++;
        }
        else
        {
            topic.NeutralCount++;
        }
    }

    private static void AddNoosphereSocialSignalReason(
        Dictionary<string, NoosphereSocialSignalReasonInsight> reasons,
        SocialSignal signal,
        int signedStrength,
        int strength)
    {
        string textEn = string.IsNullOrWhiteSpace(signal.ReasonEn) ? signal.TopicLabelEn : signal.ReasonEn;
        string textRu = string.IsNullOrWhiteSpace(signal.ReasonRu) ? signal.TopicLabelRu : signal.ReasonRu;
        string key = NormalizeWorkerKnowledgeTopicKey(textEn);
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (!reasons.TryGetValue(key, out NoosphereSocialSignalReasonInsight reason))
        {
            reason = new NoosphereSocialSignalReasonInsight
            {
                Key = key,
                TextRu = textRu,
                TextEn = textEn
            };
            reasons[key] = reason;
        }

        reason.Count++;
        reason.Strength += strength;
        reason.Score += signedStrength;
    }

    private static void AddNoosphereSocialSignalTensionSource(
        Dictionary<string, NoosphereSocialSignalSourceInsight> tensionSources,
        SocialSignal signal,
        int strength)
    {
        if (signal.Tone != SocialSignalTone.Negative)
        {
            return;
        }

        string key = signal.WorkerId > 0 ? $"worker:{signal.WorkerId}" : $"source:{signal.SourceKind}";
        string label = !string.IsNullOrWhiteSpace(signal.WorkerName) ? signal.WorkerName : signal.SourceKind.ToString();
        if (!tensionSources.TryGetValue(key, out NoosphereSocialSignalSourceInsight source))
        {
            source = new NoosphereSocialSignalSourceInsight
            {
                Key = key,
                Label = label
            };
            tensionSources[key] = source;
        }

        source.Count++;
        source.Strength += strength;
    }

    private static void SelectNoosphereSocialSignalHighlights(
        NoosphereSocialSignalInsight insight,
        Dictionary<string, NoosphereSocialSignalSourceInsight> tensionSources)
    {
        NoosphereSocialSignalSourceInsight topSource = null;
        foreach (NoosphereSocialSignalSourceInsight source in tensionSources.Values)
        {
            if (topSource == null || source.Strength > topSource.Strength)
            {
                topSource = source;
            }
        }

        insight.TopTensionSource = topSource;
        insight.TopReason = insight.Reasons.Count > 0 ? insight.Reasons[0] : null;
        for (int i = 0; i < insight.Topics.Count; i++)
        {
            NoosphereSocialSignalTopicInsight topic = insight.Topics[i];
            if (topic.Score > 0 && (insight.TopPositive == null || topic.Score > insight.TopPositive.Score))
            {
                insight.TopPositive = topic;
            }

            if (topic.Score < 0 && (insight.TopNegative == null || topic.Score < insight.TopNegative.Score))
            {
                insight.TopNegative = topic;
            }

            if (topic.PositiveCount > 0 &&
                topic.NegativeCount > 0 &&
                (insight.TopSplit == null ||
                 topic.PositiveCount + topic.NegativeCount > insight.TopSplit.PositiveCount + insight.TopSplit.NegativeCount))
            {
                insight.TopSplit = topic;
            }
        }
    }

    private static int CompareNoosphereSocialSignalTopics(
        NoosphereSocialSignalTopicInsight first,
        NoosphereSocialSignalTopicInsight second)
    {
        int strength = second.Strength.CompareTo(first.Strength);
        return strength != 0 ? strength : second.Count.CompareTo(first.Count);
    }

    private static int CompareNoosphereSocialSignalReasons(
        NoosphereSocialSignalReasonInsight first,
        NoosphereSocialSignalReasonInsight second)
    {
        int strength = second.Strength.CompareTo(first.Strength);
        return strength != 0 ? strength : second.Count.CompareTo(first.Count);
    }

    private static string FormatNoosphereSocialSignalTone(NoosphereSocialSignalInsight insight, bool ru)
    {
        if (insight.NegativeCount > 0 && insight.PositiveCount > 0 && Mathf.Abs(insight.Score) <= insight.Strength * 0.22f)
        {
            return ru ? $"\u0441\u043c\u0435\u0448\u0430\u043d\u043d\u044b\u0439 ({insight.Score:+#;-#;0})" : $"mixed ({insight.Score:+#;-#;0})";
        }

        if (insight.Score > 0)
        {
            return ru ? $"\u043f\u043e\u0437\u0438\u0442\u0438\u0432\u043d\u044b\u0439 ({insight.Score:+#;-#;0})" : $"positive ({insight.Score:+#;-#;0})";
        }

        if (insight.Score < 0)
        {
            return ru ? $"\u043d\u0435\u0433\u0430\u0442\u0438\u0432\u043d\u044b\u0439 ({insight.Score:+#;-#;0})" : $"negative ({insight.Score:+#;-#;0})";
        }

        return ru ? "\u0440\u043e\u0432\u043d\u044b\u0439 (0)" : "even (0)";
    }

    private static string FormatNoosphereSocialSignalTopics(NoosphereSocialSignalInsight insight, bool ru)
    {
        int limit = Mathf.Min(SocialSignalNoosphereTopicLimit, insight.Topics.Count);
        List<string> parts = new();
        for (int i = 0; i < limit; i++)
        {
            NoosphereSocialSignalTopicInsight topic = insight.Topics[i];
            string label = ru ? topic.LabelRu : topic.LabelEn;
            parts.Add($"{label} {topic.Score:+#;-#;0} ({topic.Count})");
        }

        return parts.Count > 0 ? string.Join(", ", parts) : (ru ? "\u0442\u0438\u0445\u043e" : "quiet");
    }

    private static string FormatNoosphereSocialSignalReasons(NoosphereSocialSignalInsight insight, bool ru)
    {
        int limit = Mathf.Min(NoosphereSocialSignalReasonLimit, insight.Reasons.Count);
        if (limit <= 0)
        {
            return ru ? "\u041f\u043e\u0432\u0442\u043e\u0440\u044f\u044e\u0449\u0438\u0445\u0441\u044f \u043f\u0440\u0438\u0447\u0438\u043d \u043f\u043e\u043a\u0430 \u043d\u0435\u0442." : "No repeating reasons yet.";
        }

        List<string> parts = new();
        for (int i = 0; i < limit; i++)
        {
            NoosphereSocialSignalReasonInsight reason = insight.Reasons[i];
            string text = ru ? reason.TextRu : reason.TextEn;
            parts.Add($"{text} ({reason.Count})");
        }

        return ru
            ? $"\u041f\u043e\u0432\u0442\u043e\u0440\u044f\u0435\u0442\u0441\u044f: {string.Join(", ", parts)}."
            : $"Recurring reasons: {string.Join(", ", parts)}.";
    }

    private static string FormatNoosphereSocialSignalTension(NoosphereSocialSignalInsight insight, bool ru)
    {
        string negativeTopic = insight.TopNegative != null
            ? ru ? insight.TopNegative.LabelRu : insight.TopNegative.LabelEn
            : string.Empty;
        string splitTopic = insight.TopSplit != null
            ? ru ? insight.TopSplit.LabelRu : insight.TopSplit.LabelEn
            : string.Empty;
        string source = insight.TopTensionSource?.Label ?? string.Empty;

        List<string> parts = new();
        if (!string.IsNullOrWhiteSpace(negativeTopic))
        {
            parts.Add(ru ? $"\u043d\u0430\u043f\u0440\u044f\u0436\u0435\u043d\u0438\u0435: {negativeTopic}" : $"tension: {negativeTopic}");
        }

        if (!string.IsNullOrWhiteSpace(splitTopic))
        {
            parts.Add(ru ? $"\u0440\u0430\u0441\u043a\u043e\u043b \u043f\u043e \u0442\u0435\u043c\u0435: {splitTopic}" : $"split topic: {splitTopic}");
        }

        if (!string.IsNullOrWhiteSpace(source))
        {
            parts.Add(ru ? $"\u0438\u0441\u0442\u043e\u0447\u043d\u0438\u043a: {source}" : $"source: {source}");
        }

        return parts.Count > 0
            ? string.Join("; ", parts) + "."
            : (ru ? "\u041d\u0430\u043f\u0440\u044f\u0436\u0435\u043d\u0438\u0435 \u043f\u043e\u043a\u0430 \u043d\u0435 \u0441\u043e\u0431\u0440\u0430\u043d\u043e." : "No tension source yet.");
    }
}
