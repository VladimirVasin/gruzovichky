using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int NoosphereDayStartSnapshotHistoryCap = 30;

    private readonly List<NoosphereDayStartSnapshot> noosphereDayStartSnapshots = new();
    private readonly HashSet<int> noosphereDayStartSnapshotDays = new();

    private enum NoosphereDayStartSnapshotTrigger
    {
        GameStart,
        DayStart
    }

    private sealed class NoosphereDayStartSnapshot
    {
        public int Day;
        public float WorldHour;
        public string ClockLabel = string.Empty;
        public NoosphereDayStartSnapshotTrigger Trigger;
        public int ActiveResidentCount;
        public int ActiveKnowledgeCount;
        public int PendingKnowledgeCount;
        public int KnowledgeEventCount;
        public int CityCanonCount;
        public int PublicSocialSignalCount;
        public int CityExperienceCount;
        public int ConversationTopicCount;
        public readonly List<NoosphereKnowledgeEventSnapshot> KnowledgeEvents = new();
        public readonly List<NoosphereSocialSignalSnapshot> SocialSignals = new();
        public readonly List<NoosphereSocialInsightSnapshot> SocialInsights = new();
        public readonly List<NoosphereCityExperienceSnapshot> CityExperiences = new();
        public readonly List<NoosphereCityCanonSnapshot> CityCanon = new();
        public readonly List<NoosphereConversationTopicSnapshot> ConversationTopics = new();
        public readonly List<NoosphereWorkerLayerSnapshot> Workers = new();
        public readonly List<NoosphereDiveMeaningSnapshot> DiveMeanings = new();
        public readonly List<NoosphereVisionInsightSnapshot> VisionInsights = new();
        public readonly List<NoosphereVisualNodeSnapshot> VisualNodes = new();
    }

    private sealed class NoosphereKnowledgeEventSnapshot
    {
        public NoosphereKnowledgeEventKind EventKind;
        public WorkerCognitionKind CognitionKind;
        public WorkerMemoryKind MemoryKind;
        public string ConversationTopicKey = string.Empty;
        public int OwnerWorkerId;
        public int OtherWorkerId;
        public string OwnerName = string.Empty;
        public string OtherName = string.Empty;
        public string Topic = string.Empty;
        public LocationType? BuildingType;
        public int BuildingInstanceId;
        public string BuildingLabel = string.Empty;
        public bool Positive;
        public string ReasonRu = string.Empty;
        public string ReasonEn = string.Empty;
        public int KnowledgeIteration;
        public WorkerKnowledgeSourceAttitude SourceAttitude;
        public int RumorRootId;
        public string OriginalTopic = string.Empty;
        public string RumorTopic = string.Empty;
        public int RumorDistortionPercent;
        public int RumorConnotationScore;
        public int RumorConnotationConfidence;
        public WorkerKnowledgeOpinionTone OpinionTone;
        public int OpinionScore;
        public int OpinionConfidence;
        public string OpinionReasonRu = string.Empty;
        public string OpinionReasonEn = string.Empty;
        public bool IsCityCanonKnowledge;
        public int CityCanonAdoptionCount;
        public int CityCanonAdoptionRequired;
        public int EventDay;
        public float EventWorldHour;
        public float MemoryCreatedWorldHour;
        public float MemoryExpiresWorldHour;
    }

    private sealed class NoosphereSocialSignalSnapshot
    {
        public WorkerCognitionKind CognitionKind;
        public int Id;
        public int WorkerId;
        public string WorkerName = string.Empty;
        public int Day;
        public float WorldHour;
        public string TopicKey = string.Empty;
        public string TopicLabelRu = string.Empty;
        public string TopicLabelEn = string.Empty;
        public SocialSignalTone Tone;
        public int Strength;
        public int Confidence;
        public SocialSignalCategory Category;
        public SocialSignalSourceKind SourceKind;
        public string SourceKey = string.Empty;
        public LocationType? LocationType;
        public int LocationInstanceId;
        public bool HasCell;
        public Vector2Int Cell;
        public string ReasonRu = string.Empty;
        public string ReasonEn = string.Empty;
        public int DailyScoreHint;
        public bool IncludeInDailyExperience;
        public bool PublicForNoosphere;
    }

    private sealed class NoosphereSocialInsightSnapshot
    {
        public int Day;
        public int Count;
        public int PositiveCount;
        public int NegativeCount;
        public int NeutralCount;
        public int Score;
        public int Strength;
        public readonly List<NoosphereSocialTopicSnapshot> Topics = new();
        public readonly List<NoosphereSocialReasonSnapshot> Reasons = new();
    }

    private sealed class NoosphereSocialTopicSnapshot
    {
        public string Key = string.Empty;
        public string LabelRu = string.Empty;
        public string LabelEn = string.Empty;
        public SocialSignalCategory Category;
        public int Count;
        public int PositiveCount;
        public int NegativeCount;
        public int NeutralCount;
        public int Score;
        public int Strength;
        public int ConfidenceTotal;
    }

    private sealed class NoosphereSocialReasonSnapshot
    {
        public string Key = string.Empty;
        public string TextRu = string.Empty;
        public string TextEn = string.Empty;
        public int Count;
        public int Strength;
        public int Score;
    }

    private sealed class NoosphereCityExperienceSnapshot
    {
        public int Day;
        public WorkerDailyOpinionTone FinalTone;
        public int Score;
        public int Confidence;
        public int Consensus;
        public int Tension;
        public int ResidentCount;
        public int PositiveResidentCount;
        public int NegativeResidentCount;
        public WorkerDailyOpinionFactorKind DominantKind;
        public string SummaryRu = string.Empty;
        public string SummaryEn = string.Empty;
        public string MainReasonRu = string.Empty;
        public string MainReasonEn = string.Empty;
        public string CounterpointRu = string.Empty;
        public string CounterpointEn = string.Empty;
        public float CreatedWorldHour;
        public readonly List<NoosphereCityExperienceFactorSnapshot> Factors = new();
    }

    private sealed class NoosphereCityExperienceFactorSnapshot
    {
        public WorkerDailyOpinionFactorKind Kind;
        public int Score;
        public int ResidentCount;
        public int PositiveCount;
        public int NegativeCount;
        public int RepresentativeScore;
        public string RepresentativeReasonRu = string.Empty;
        public string RepresentativeReasonEn = string.Empty;
    }

    private sealed class NoosphereCityCanonSnapshot
    {
        public WorkerCognitionKind CognitionKind;
        public WorkerMemoryKind Kind;
        public string ConversationTopicKey = string.Empty;
        public int OtherWorkerId;
        public string Topic = string.Empty;
        public LocationType? BuildingType;
        public int BuildingInstanceId;
        public string BuildingLabel = string.Empty;
        public bool Positive;
        public int KnowledgeIteration;
        public WorkerKnowledgeSourceAttitude SourceAttitude;
        public int RumorRootId;
        public string OriginalTopic = string.Empty;
        public string RumorTopic = string.Empty;
        public int RumorDistortionPercent;
        public int RumorConnotationScore;
        public int RumorConnotationConfidence;
        public WorkerKnowledgeOpinionTone OpinionTone;
        public int OpinionScore;
        public int OpinionConfidence;
        public string OpinionReasonRu = string.Empty;
        public string OpinionReasonEn = string.Empty;
        public int SourceWorkerId;
        public int AdoptionCount;
        public int AdoptionRequired;
        public int CanonizedDay;
        public float CanonizedWorldHour;
    }

    private sealed class NoosphereConversationTopicSnapshot
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

    private sealed class NoosphereWorkerLayerSnapshot
    {
        public int WorkerId;
        public int CitizenId;
        public string WorkerName = string.Empty;
        public CitizenProfessionKind CitizenProfession;
        public WorkerEducationLevel Education;
        public int Satisfaction;
        public int Money;
        public bool IsInsideBuilding;
        public LocationType? InsideBuildingType;
        public int InsideBuildingInstanceId;
        public int FamilyId;
        public int SocialMemoryCount;
        public int ActiveThoughtCount;
        public int MemoryCount;
        public int PendingKnowledgeCount;
        public int TopicOpinionCount;
        public int DailyOpinionCount;
        public readonly List<NoosphereWorkerThoughtSnapshot> Thoughts = new();
        public readonly List<NoospherePendingThoughtSnapshot> PendingThoughts = new();
        public readonly List<NoospherePendingKnowledgeSnapshot> PendingKnowledge = new();
        public readonly List<NoosphereWorkerMemorySnapshot> Memories = new();
        public readonly List<NoosphereWorkerTopicOpinionSnapshot> TopicOpinions = new();
        public readonly List<NoosphereWorkerDailyOpinionSnapshot> DailyOpinions = new();
    }

    private sealed class NoosphereWorkerThoughtSnapshot
    {
        public string Key = string.Empty;
        public WorkerThoughtKind Kind;
        public WorkerThoughtTone Tone;
        public WorkerThoughtPriority Priority;
        public int Intensity;
        public string TemplateKey = string.Empty;
        public int CreatedDay;
        public float CreatedWorldHour;
        public bool Active;
        public float ExpiresWorldHour;
    }

    private sealed class NoospherePendingThoughtSnapshot
    {
        public string FormationKey = string.Empty;
        public string ThoughtKey = string.Empty;
        public WorkerThoughtKind Kind;
        public WorkerThoughtTone Tone;
        public WorkerThoughtPriority Priority;
        public int Intensity;
        public string TemplateKey = string.Empty;
        public int StartedDay;
        public float StartedWorldHour;
        public float ReadyWorldHour;
        public string FormationReason = string.Empty;
    }

    private sealed class NoospherePendingKnowledgeSnapshot
    {
        public string FormationKey = string.Empty;
        public WorkerCognitionKind CognitionKind;
        public WorkerMemoryKind Kind;
        public string ConversationTopicKey = string.Empty;
        public int OtherWorkerId;
        public string Topic = string.Empty;
        public LocationType? BuildingType;
        public int BuildingInstanceId;
        public string BuildingLabel = string.Empty;
        public string SourceRu = string.Empty;
        public string SourceEn = string.Empty;
        public bool Positive;
        public WorkerKnowledgeFormationStage Stage;
        public int StartedDay;
        public float StartedWorldHour;
        public float NextStageWorldHour;
        public WorkerKnowledgeOpinionTone OpinionTone;
        public int OpinionScore;
        public int OpinionConfidence;
        public string OpinionReasonRu = string.Empty;
        public string OpinionReasonEn = string.Empty;
    }

    private sealed class NoosphereWorkerMemorySnapshot
    {
        public WorkerCognitionKind CognitionKind;
        public WorkerMemoryKind Kind;
        public string ConversationTopicKey = string.Empty;
        public int OtherWorkerId;
        public string Topic = string.Empty;
        public LocationType? BuildingType;
        public int BuildingInstanceId;
        public string BuildingLabel = string.Empty;
        public string SourceRu = string.Empty;
        public string SourceEn = string.Empty;
        public bool Positive;
        public int KnowledgeIteration;
        public int RumorRootId;
        public string OriginalTopic = string.Empty;
        public string RumorTopic = string.Empty;
        public WorkerKnowledgeOpinionTone OpinionTone;
        public int OpinionScore;
        public int OpinionConfidence;
        public bool IsCityCanonKnowledge;
        public int CityCanonAdoptionCount;
        public int CityCanonAdoptionRequired;
        public int CreatedDay;
        public float CreatedWorldHour;
        public float ExpiresWorldHour;
    }

    private sealed class NoosphereWorkerTopicOpinionSnapshot
    {
        public string TopicKey = string.Empty;
        public string ConversationTopicKey = string.Empty;
        public int RumorRootId;
        public string OriginalTopic = string.Empty;
        public string CurrentTopic = string.Empty;
        public WorkerKnowledgeOpinionTone Tone;
        public int Score;
        public int Confidence;
        public string ReasonRu = string.Empty;
        public string ReasonEn = string.Empty;
        public int TimesHeard;
        public int PositiveSignalCount;
        public int NegativeSignalCount;
        public int ContradictionCount;
        public int LastUpdatedDay;
        public float LastUpdatedWorldHour;
    }

    private sealed class NoosphereWorkerDailyOpinionSnapshot
    {
        public int Day;
        public WorkerDailyOpinionTone FinalTone;
        public int Score;
        public int Confidence;
        public string SummaryRu = string.Empty;
        public string SummaryEn = string.Empty;
        public string MainReasonRu = string.Empty;
        public string MainReasonEn = string.Empty;
        public int PositiveThoughtCount;
        public int NegativeThoughtCount;
        public int CriticalActiveThoughtCount;
        public int EmittedSocialSignalCount;
        public WorkerDailyOpinionFactorKind DominantKind;
        public readonly List<NoosphereWorkerDailyOpinionFactorSnapshot> Factors = new();
    }

    private sealed class NoosphereWorkerDailyOpinionFactorSnapshot
    {
        public WorkerDailyOpinionFactorKind Kind;
        public int Score;
        public string ReasonRu = string.Empty;
        public string ReasonEn = string.Empty;
    }

    private sealed class NoosphereDiveMeaningSnapshot
    {
        public string Key = string.Empty;
        public string Text = string.Empty;
        public NoosphereDiveMeaningKind Kind;
        public int Score;
        public int Confidence;
        public int Weight;
        public bool IsCanon;
        public bool IsBurned;
        public float Radius;
        public float Height;
        public float Phase;
        public float Speed;
        public float Size;
        public float Wobble;
        public Color Color;
    }

    private sealed class NoosphereVisionInsightSnapshot
    {
        public string Key = string.Empty;
        public string TitleRu = string.Empty;
        public string TitleEn = string.Empty;
        public string SummaryRu = string.Empty;
        public string SummaryEn = string.Empty;
        public string SourceRu = string.Empty;
        public string SourceEn = string.Empty;
        public string EffectRu = string.Empty;
        public string EffectEn = string.Empty;
        public string ActionRu = string.Empty;
        public string ActionEn = string.Empty;
        public NoosphereVisionTone Tone;
        public SocialSignalCategory Category;
        public int Score;
        public int Strength;
        public int SourceCount;
        public readonly List<Vector3> SourceWorldPositions = new();
    }

    private sealed class NoosphereVisualNodeSnapshot
    {
        public int WorkerId;
        public string WorkerName = string.Empty;
        public Vector2 Position;
        public WorkerKnowledgeOpinionTone Tone;
        public bool HasPending;
        public bool HasCanon;
        public int ActiveMemoryCount;
        public int PendingKnowledgeCount;
        public Color Color;
    }
}
