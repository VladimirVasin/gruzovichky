using UnityEngine;

public partial class GameBootstrap
{
    private enum SocialSignalTone
    {
        Positive,
        Neutral,
        Negative
    }

    private enum SocialSignalCategory
    {
        Need,
        Work,
        Money,
        Social,
        Family,
        Transport,
        City,
        Housing,
        Litter,
        Governance,
        Knowledge,
        Topic
    }

    private enum SocialSignalSourceKind
    {
        Thought,
        StreetLitter,
        DailyExperience,
        TopicOpinion,
        CityComplaint,
        CityHallDecision,
        CityTrust
    }

    private sealed class SocialSignal
    {
        public WorkerCognitionKind CognitionKind = WorkerCognitionKind.Experience;
        public int Id;
        public int WorkerId;
        public string WorkerName = string.Empty;
        public int Day;
        public float WorldHour;
        public string TopicKey = string.Empty;
        public string TopicLabelRu = string.Empty;
        public string TopicLabelEn = string.Empty;
        public SocialSignalTone Tone = SocialSignalTone.Neutral;
        public int Strength;
        public int Confidence;
        public SocialSignalCategory Category = SocialSignalCategory.City;
        public SocialSignalSourceKind SourceKind = SocialSignalSourceKind.Thought;
        public string SourceKey = string.Empty;
        public LocationType? LocationType;
        public int LocationInstanceId;
        public bool HasCell;
        public Vector2Int Cell;
        public string ReasonRu = string.Empty;
        public string ReasonEn = string.Empty;
        public int DailyScoreHint;
        public bool IncludeInDailyExperience = true;
        public bool PublicForNoosphere = true;
    }

    private sealed class WorkerTopicOpinion
    {
        public WorkerCognitionKind CognitionKind = WorkerCognitionKind.Opinion;
        public string TopicKey = string.Empty;
        public string ConversationTopicKey = string.Empty;
        public int RumorRootId;
        public string OriginalTopic = string.Empty;
        public string CurrentTopic = string.Empty;
        public WorkerKnowledgeOpinionTone Tone = WorkerKnowledgeOpinionTone.Neutral;
        public int Score;
        public int Confidence;
        public string ReasonRu = string.Empty;
        public string ReasonEn = string.Empty;
        public int TimesHeard;
        public int PositiveSignalCount;
        public int NegativeSignalCount;
        public int ContradictionCount;
        public int LastSourceWorkerId;
        public WorkerSocialInteractionKind LastSourceInteractionKind;
        public int LastKnowledgeIteration;
        public int LastUpdatedDay;
        public float LastUpdatedWorldHour;
    }

    private sealed class ConversationTopic
    {
        public string Key = string.Empty;
        public string OriginalText = string.Empty;
        public string DisplayText = string.Empty;
        public int FirstDay;
        public float FirstWorldHour;
        public int LastMentionedDay;
        public float LastMentionedWorldHour;
        public int MentionCount;
        public int PositiveConversationCount;
        public int NegativeConversationCount;
        public int PositiveOpinionCount;
        public int NegativeOpinionCount;
    }
}
