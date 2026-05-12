using System.Collections.Generic;

public partial class GameBootstrap
{
    private readonly Dictionary<string, ConversationTopic> conversationTopicsByKey = new();
    private readonly List<ConversationTopic> conversationTopics = new();

    private ConversationTopic EnsureConversationTopic(string topic, bool? positiveConversation = null)
    {
        string key = BuildConversationTopicKey(topic);
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (!conversationTopicsByKey.TryGetValue(key, out ConversationTopic record))
        {
            string display = NormalizeConversationTopicDisplayText(topic);
            float now = GetCurrentWorldHour();
            record = new ConversationTopic
            {
                Key = key,
                OriginalText = topic?.Trim() ?? string.Empty,
                DisplayText = string.IsNullOrWhiteSpace(display) ? key : display,
                FirstDay = currentDay,
                FirstWorldHour = now
            };
            conversationTopicsByKey[key] = record;
            conversationTopics.Insert(0, record);
        }

        record.MentionCount++;
        record.LastMentionedDay = currentDay;
        record.LastMentionedWorldHour = GetCurrentWorldHour();
        if (positiveConversation == true)
        {
            record.PositiveConversationCount++;
        }
        else if (positiveConversation == false)
        {
            record.NegativeConversationCount++;
        }

        return record;
    }

    private void RecordConversationTopicOpinion(WorkerTopicOpinion opinion)
    {
        if (opinion == null)
        {
            return;
        }

        ConversationTopic topic = FindConversationTopic(opinion.ConversationTopicKey);
        if (topic == null)
        {
            topic = EnsureConversationTopic(opinion.OriginalTopic);
            if (topic != null)
            {
                opinion.ConversationTopicKey = topic.Key;
            }
        }

        if (topic == null)
        {
            return;
        }

        if (opinion.Tone == WorkerKnowledgeOpinionTone.Positive)
        {
            topic.PositiveOpinionCount++;
        }
        else if (opinion.Tone == WorkerKnowledgeOpinionTone.Negative)
        {
            topic.NegativeOpinionCount++;
        }
    }

    private ConversationTopic FindConversationTopic(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        conversationTopicsByKey.TryGetValue(NormalizeWorkerKnowledgeTopicKey(key), out ConversationTopic topic);
        return topic;
    }

    private static string BuildConversationTopicKey(string topic)
    {
        return NormalizeWorkerKnowledgeTopicKey(topic);
    }

    private static string NormalizeConversationTopicDisplayText(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            return string.Empty;
        }

        string value = topic.Trim()
            .Replace("\r", " ")
            .Replace("\n", " ");
        while (value.Contains("  "))
        {
            value = value.Replace("  ", " ");
        }

        return value;
    }

    private static bool DoesConversationTopicAssociationMatchSocialSignal(SocialSignal signal, string normalizedTopic)
    {
        if (signal == null || string.IsNullOrWhiteSpace(normalizedTopic))
        {
            return false;
        }

        if (TopicContainsAny(normalizedTopic, "LITTER", "TRASH", "GARBAGE", "\u041c\u0423\u0421\u041e\u0420", "\u0413\u0420\u042f\u0417", "\u0423\u0411\u041e\u0420"))
        {
            return signal.Category == SocialSignalCategory.Litter ||
                   SignalTextContainsAny(signal, "LITTER", "TRASH", "GARBAGE", "\u041c\u0423\u0421\u041e\u0420", "\u0413\u0420\u042f\u0417");
        }

        if (TopicContainsAny(normalizedTopic, "FOOD", "MEAL", "CANTEEN", "KIOSK", "\u0415\u0414\u0410", "\u041f\u0418\u0422\u0410\u041d", "\u0421\u0422\u041e\u041b\u041e\u0412", "\u041a\u0418\u041e\u0421\u041a"))
        {
            return signal.Category == SocialSignalCategory.Need ||
                   SignalTextContainsAny(signal, "FOOD", "MEAL", "CANTEEN", "KIOSK", "\u0415\u0414\u0410", "\u041f\u0418\u0422\u0410\u041d", "\u0421\u0422\u041e\u041b\u041e\u0412", "\u041a\u0418\u041e\u0421\u041a");
        }

        if (TopicContainsAny(normalizedTopic, "GAMBL", "CASINO", "BAR", "\u0410\u0417\u0410\u0420\u0422", "\u041a\u0410\u0417\u0418\u041d\u041e", "\u0411\u0410\u0420"))
        {
            return signal.Category is SocialSignalCategory.Money or SocialSignalCategory.Social or SocialSignalCategory.Family ||
                   SignalTextContainsAny(signal, "GAMBL", "CASINO", "BAR", "\u0410\u0417\u0410\u0420\u0422", "\u041a\u0410\u0417\u0418\u041d\u041e", "\u0411\u0410\u0420");
        }

        if (TopicContainsAny(normalizedTopic, "CITY HALL", "GOVERN", "MAYOR", "\u041c\u042d\u0420", "\u041c\u042d\u0420\u0418", "\u0420\u0410\u0422\u0423\u0428", "\u041e\u0411\u0429\u0415\u0421\u0422\u0412"))
        {
            return signal.Category == SocialSignalCategory.Governance ||
                   SignalTextContainsAny(signal, "CITY HALL", "GOVERN", "MAYOR", "\u041c\u042d\u0420", "\u041c\u042d\u0420\u0418", "\u0420\u0410\u0422\u0423\u0428");
        }

        if (TopicContainsAny(normalizedTopic, "SAFETY", "SAFE", "DANGER", "\u0411\u0415\u0417\u041e\u041f\u0410\u0421", "\u041e\u041f\u0410\u0421\u041d"))
        {
            return signal.Category is SocialSignalCategory.City or SocialSignalCategory.Transport or SocialSignalCategory.Governance ||
                   SignalTextContainsAny(signal, "SAFETY", "SAFE", "DANGER", "\u0411\u0415\u0417\u041e\u041f\u0410\u0421", "\u041e\u041f\u0410\u0421\u041d");
        }

        return false;
    }

    private static bool TopicContainsAny(string normalizedTopic, params string[] tokens)
    {
        if (string.IsNullOrWhiteSpace(normalizedTopic) || tokens == null)
        {
            return false;
        }

        for (int i = 0; i < tokens.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(tokens[i]) &&
                normalizedTopic.Contains(tokens[i], System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static bool SignalTextContainsAny(SocialSignal signal, params string[] tokens)
    {
        if (signal == null)
        {
            return false;
        }

        string key = NormalizeWorkerKnowledgeTopicKey(signal.TopicKey);
        string labelRu = NormalizeWorkerKnowledgeTopicKey(signal.TopicLabelRu);
        string labelEn = NormalizeWorkerKnowledgeTopicKey(signal.TopicLabelEn);
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i];
            if (string.IsNullOrWhiteSpace(token))
            {
                continue;
            }

            if (key.Contains(token, System.StringComparison.Ordinal) ||
                labelRu.Contains(token, System.StringComparison.Ordinal) ||
                labelEn.Contains(token, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
