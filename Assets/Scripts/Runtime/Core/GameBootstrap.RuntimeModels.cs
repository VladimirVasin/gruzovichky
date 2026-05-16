using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private sealed class TruckAgent
    {
        public int TruckNumber;
        public string DisplayName;
        public GameObject TruckObject;
        public Transform TruckVisualRoot;
        public Transform TruckBodyTransform;
        public Transform TruckCabinTransform;
        public Transform TruckCargoVisualRoot;
        public CargoType TruckCargoVisualType = CargoType.None;
        public int TruckCargoVisualAmount = -1;
        public Renderer TruckHeadlightLeftRenderer;
        public Renderer TruckHeadlightRightRenderer;
        public Material TruckHeadlightLeftMaterial;
        public Material TruckHeadlightRightMaterial;
        public readonly List<Transform> TruckWheels = new();
        public readonly List<Transform> TruckFrontWheels = new();
        public readonly List<Light> TruckHeadlights = new();
        public readonly List<DriverAgent> AssignedDrivers = new();
        public DriverAgent Driver;
        public AudioSource TruckLoopAudioSource;
        public float EngineAudioPhaseOffset;
        public float EngineAudioWobbleSpeed = 1f;
        public float EngineAudioPitchBias = 1f;
        public float EngineAudioVolumeBias = 1f;
        public readonly List<Vector2Int> ActivePath = new();
        public Vector2Int TruckCell;
        public Vector3 TruckTargetWorld;
        public Vector3 TruckSegmentStartWorld;
        public Vector3 TruckSmoothedForward = Vector3.forward;
        public bool IsTruckMoving;
        public bool IsTruckInteracting;
        public bool IsTruckWaitingForService;
        public bool IsDriverRescueActive;
        public bool IsTruckAutoModeEnabled;
        public CargoType TruckCargoType = CargoType.None;
        public int TruckCargoAmount;
        public float TruckSegmentProgress;
        public float TruckSegmentDuration;
        public float TruckWheelSpinAngle;
        public float TruckSteerAngle;
        public float TruckInteractionTimer;
        public float TruckFuel = TruckFuelCapacity;
        public int CurrentAssignedTripReward;
        public TripType CurrentAssignedTrip = TripType.None;
        public int CurrentTripPickupLocationInstanceId;
        public int CurrentTripDropoffLocationInstanceId;
        public TripPhase CurrentTripPhase = TripPhase.None;
        public RefuelPhase CurrentRefuelPhase = RefuelPhase.None;
        public TruckInteractionType ActiveTruckInteraction = TruckInteractionType.None;
        public TruckInteractionType QueuedTruckInteraction = TruckInteractionType.None;
        public Quaternion TruckInteractionTargetRotation;
        public Vector3 TruckInteractionBuildingPoint;
        public LocationType? ActiveServiceLocation;
        public LocationType? QueuedServiceLocation;
        public int ActiveServiceLocationInstanceId;
        public int QueuedServiceLocationInstanceId;
        public int ParkingSlotIndex;
        public float ExhaustEmitTimer;
        public float DirtDustEmitTimer;
        public bool IsPurchaseArrivalActive;
        public readonly List<Vector3> PurchaseArrivalWaypoints = new();
        public int PurchaseArrivalWaypointIndex;
        public float PurchaseArrivalSpeed = 4.6f;
    }

    private sealed class BusAgent
    {
        public int BusNumber;
        public string DisplayName;
        public GameObject BusObject;
        public Renderer HeadlightLeftRenderer;
        public Renderer HeadlightRightRenderer;
        public Material HeadlightLeftMaterial;
        public Material HeadlightRightMaterial;
        public Light HeadlightLeft;
        public Light HeadlightRight;
        public DriverAgent Driver;
        public int ParkingSlotIndex;
        public int PassengerCount;
        public int PassengerCapacity = LocalBusMaxPassengers;
        public int Bank;
        public bool IsPurchaseArrivalActive;
        public readonly List<Vector3> PurchaseArrivalWaypoints = new();
        public int PurchaseArrivalWaypointIndex;
        public float PurchaseArrivalSpeed = 4.3f;
    }

    private enum WorkerPerkKind
    {
        Alcoholism,
        Gambler,
        Socialite,
        Frugal,
        Quicklearner
    }

    private enum WorkerTraitKind
    {
        Sociable,
        Reserved,
        Frugal,
        Curious,
        Anxious,
        Impulsive,
        Cautious,
        Trusting,
        Skeptical,
        Stubborn,
        Adaptable,
        Meticulous,
        Dutiful
    }

    private enum WorkerWeaknessKind
    {
        None,
        Alcoholism,
        Gambling
    }

    private enum WorkerSocialLeadershipStatus
    {
        None,
        SocialLeader
    }

    public enum WorkerRaceKind
    {
        Rovian,
        Zelen,
        Iskrian
    }

    private enum WorkerHeritageKind
    {
        Rovian,
        Zelen,
        Iskrian
    }

    private enum WorkerAffectKind
    {
        FinancialPressure,
        FamilyAnxiety,
        ReliefAfterRest,
        Hangover,
        GamblingExcitement,
        GamblingRegret,
        IrritatedByLitter,
        StableRoutine
    }

    private enum WorkerGender { Male, Female }

    private enum WorkerEducationLevel
    {
        Basic,
        Vocational,
        Higher
    }

    private enum WorkerProfessionalTrack
    {
        None,
        Logistics,
        Production,
        Service
    }

    private enum CitizenProfessionKind
    {
        Resident,
        TruckDriver,
        IntercityDriver,
        BusDriver,
        ProductionWorker,
        Lumberjack,
        SawmillWorker,
        Carpenter,
        WarehouseWorker,
        DockWorker,
        ServiceWorker,
        Cleaner,
        EmploymentClerk,
        ChildcareWorker,
        Teacher,
        CarDealer
    }

    private enum WorkerThoughtKind
    {
        Need,
        Work,
        Money,
        Social,
        Family,
        Transport,
        City
    }

    private enum WorkerThoughtTone
    {
        Positive,
        Neutral,
        Negative
    }

    private enum WorkerThoughtSubjectType
    {
        None,
        Text,
        Worker,
        BuildingType,
        Need,
        Family,
        Child
    }

    private enum WorkerThoughtPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    private enum WorkerMemoryKind
    {
        ConversationTopic,
        BuildingExistence
    }

    private enum WorkerCognitionKind
    {
        Fact,
        Experience,
        Opinion,
        Rumor
    }

    private enum WorkerKnowledgeFormationStage
    {
        Heard,
        Comparing,
        Judging
    }

    private enum WorkerKnowledgeOpinionTone
    {
        Neutral,
        Positive,
        Negative
    }

    private enum WorkerKnowledgeSourceAttitude
    {
        Neutral,
        Positive,
        Negative
    }

    private enum NoosphereKnowledgeEventKind
    {
        Received,
        Burned,
        Canonized
    }

    private enum WorkerLifeOpinionCategory
    {
        Work,
        City,
        Money,
        Housing,
        Social
    }

    private enum WorkerDailyOpinionTone
    {
        Positive,
        Negative
    }

    private enum WorkerDailyOpinionFactorKind
    {
        Need,
        Work,
        Money,
        Social,
        Family,
        Transport,
        City,
        Housing
    }

    private sealed class DriverAgent
    {
        public int DriverId;
        public int CitizenId;
        public CitizenProfessionKind CitizenProfession = CitizenProfessionKind.Resident;
        public string DriverName;
        public WorkerGender Gender;
        public WorkerRaceKind Race = WorkerRaceKind.Rovian;
        public bool HasAssignedRace;
        public WorkerHeritageKind Heritage = WorkerHeritageKind.Rovian;
        public bool HasAssignedHeritage;
        public WorkerEducationLevel Education;
        public bool HasPortrait;
        public int PortraitSkinTone;
        public int PortraitHairStyle;
        public int PortraitHairColor;
        public int PortraitEyeStyle;
        public int PortraitMouthStyle;
        public int PortraitAccessory;
        public int PortraitHeadShape;
        public readonly List<WorkerPerkKind> Perks = new();
        public readonly List<WorkerTraitKind> Traits = new();
        public WorkerWeaknessKind Weakness = WorkerWeaknessKind.None;
        public readonly List<WorkerAffect> Affects = new();
        public DriverDutyMode DutyMode = DriverDutyMode.Local;
        public int ShiftStartHour = -1;
        public bool IsOnActiveShift;
        public GameObject DriverObject;
        public Transform DriverVisualRoot;
        public Transform DriverBodyTransform;
        public Transform DriverHeadTransform;
        public Transform DriverCapTransform;
        public Transform DriverLeftArmTransform;
        public Transform DriverRightArmTransform;
        public Transform DriverLeftLegTransform;
        public Transform DriverRightLegTransform;
        public bool DriverImportedCitizenVisual;
        public bool DriverImportedCitizenFemaleVisual;
        public Transform DriverImportedModelTransform;
        public Transform[] DriverImportedHairFlowTransforms;
        public Transform[] DriverImportedClothFlowTransforms;
        public Transform[] DriverImportedGlowTransforms;
        public Transform DriverFuelCanTransform;
        public Transform DriverFlashlightTransform;
        public Light DriverFlashlightLight;
        public Light DriverFlashlightHaloLight;
        public Renderer DriverFlashlightRenderer;
        public Material DriverFlashlightMaterial;
        public DriverRestPhase RestPhase = DriverRestPhase.None;
        public float SleepTimer;
        public Vector3 MotelIdlePosition;
        public int AssignedTruckNumber;
        public int Salary = 25;
        public int ContractSalary;
        public int ContractTotalWorkDays;
        public int ContractWorkedDays;
        public int ContractStartedDay;
        public VacancyKind ContractVacancyKind = VacancyKind.None;
        public LocationType? ContractBuildingType;
        public int ContractBuildingInstanceId;
        public int ContractSlotIndex = -1;
        public int ContractShiftIndex = -1;
        public WorkerProfessionalTrack ContractProfessionalTrack = WorkerProfessionalTrack.None;
        public int ContractRequiredProfessionalLevel = 1;
        public int Money = 30;
        public bool WaitingForShiftAtParking;
        public bool NeedsShiftEndReturn;
        public bool IsShiftSalaryPending;
        public int LastSalaryPaidShiftDay = -1;
        public int LastSalaryPaidShiftStartHour = -999;
        public DriverRescuePhase WalkPhase = DriverRescuePhase.None;
        public Vector3 WalkTargetWorld;
        public readonly List<Vector3> WalkPath = new();
        public int WalkWaypointIndex;
        public float WalkAnimationTime;
        public AudioSource FootstepAudioSource;
        public int FootstepPhaseIndex;
        public float FootstepCooldown;
        public int FootstepClipCursor;
        public bool HasLastSafeWalkPosition;
        public Vector3 LastSafeWalkPosition;
        public bool HasLastFootpathWearCell;
        public Vector2Int LastFootpathWearCell;
        public Vector2Int CleanerTargetCell;
        public bool HasCleanerTargetCell;
        public float CleanerActionTimer;
        public float CleanerSearchCooldown;
        public int IdleWanderPointIndex = -1;
        public float IdleWanderPauseTimer;
        public float IdleOverlapRerouteCooldown;
        public float IdleConversationTimer;
        public int IdleConversationPartnerId = -1;
        public float IdleConversationCooldownTimer;
        public bool IsArrivingByBus;
        public int SittingBenchIndex = -1;
        public int CityParkBenchIndex = -1;
        public int ImportedBarSeatLocationInstanceId;
        public int ImportedBarSeatIndex = -1;
        public int CityParkPromenadeStep;
        public int CityParkActivityStyle;
        public float IdleActivityTimer;
        public Transform[] SmokingParticles;
        public Material[] SmokingParticleMaterials;
        public float[] SmokingParticleLives;
        public Vector3[] SmokingParticleVelocities;
        public float SmokingEmitTimer;
        public WorkerLifeGoal LifeGoal = WorkerLifeGoal.None;
        public int LifeCycleLastHour = -1;
        public bool NeedsCycleResetPending;
        public bool WorkedToday;
        public bool AteToday;
        public bool HadLeisureToday;
        public int PendingVendorLocationInstanceId;
        public string PendingVendorItemId = string.Empty;
        public int PendingServiceLocationInstanceId;
        public int GamblingBet;
        public int GamblingPayout;
        public int GamblingMultiplier;
        public bool GamblingMoneyPending;
        public int GamblingBetCount;
        public bool GamblingLostLastTime;
        public bool GamblingBroke;
        public bool SleptToday;
        public float HoursSinceMeal = 0f;
        public float HoursSinceSleep = 0f;
        public float HoursSinceLeisure = 0f;
        public float NextMealRetryAtWorldHour;
        public float NextSleepRetryAtWorldHour;
        public float NextLeisureRetryAtWorldHour;
        public WorkerNeedStatus LastMealNeedStatus = WorkerNeedStatus.Ok;
        public WorkerNeedStatus LastSleepNeedStatus = WorkerNeedStatus.Ok;
        public WorkerNeedStatus LastLeisureNeedStatus = WorkerNeedStatus.Ok;
        public int BusOriginStopNumber = -1;
        public int BusDestinationStopNumber = -1;
        public DriverRescuePhase BusFinalWalkPhase = DriverRescuePhase.None;
        public Vector3 BusFinalTargetWorld;
        public string BusTravelReason = string.Empty;
        public bool BusRideFareExempt;
        public int AssignedPersonalHouseIndex = -1;
        public int FamilyId = -1;
        public int IdleCatPetTargetIndex = -1;
        public int Age;
        public int DaysOnMap;
        public int OwnedCarModelIndex = -1;
        public GameObject OwnedCarObject;
        public bool HasOwnedCarParking;
        public Vector3 OwnedCarParkedWorld;
        public Vector2Int OwnedCarParkedDriveCell;
        public bool IsDrivingPersonalCar;
        public bool CompletedPersonalCarTrip;
        public DriverRescuePhase PersonalCarFinalPhase = DriverRescuePhase.None;
        public Vector3 PersonalCarFinalTargetWorld;
        public string PersonalCarTravelReason = string.Empty;
        public readonly List<Vector3> PersonalCarPath = new();
        public int PersonalCarWaypointIndex;
        public LocationType? AssignedBuildingType;
        public int AssignedBuildingInstanceId;
        public int AssignedBuildingSlotIndex = -1;
        public int LogisticsExperienceDays;
        public int ProductionExperienceDays;
        public int ServiceExperienceDays;
        public int ReservedLaborExchangePostingId;
        public float LaborExchangeInterviewTimer;
        public bool IsInsideBuilding;
        public LocationType? InsideBuildingType;
        public int InsideBuildingInstanceId;
        public WorkerSocialLeadershipStatus SocialLeadershipStatus = WorkerSocialLeadershipStatus.None;
        public float SocialLeadershipScore;
        public int SocialLeadershipRank;
        public int SocialLeadershipLinkCount;
        public readonly List<WorkerSocialMemory> SocialMemories = new();
        public readonly List<WorkerThought> Thoughts = new();
        public readonly List<PendingWorkerThought> PendingThoughts = new();
        public readonly List<PendingWorkerKnowledge> PendingKnowledge = new();
        public readonly List<WorkerMemory> Memories = new();
        public readonly List<WorkerOpinion> Opinions = new();
        public readonly List<WorkerLifeOpinion> LifeOpinions = new();
        public readonly List<WorkerDailyOpinion> DailyOpinions = new();
        public readonly List<WorkerTopicOpinion> TopicOpinions = new();
        public readonly List<WorkerInventoryEntry> Inventory = new();
        public readonly Dictionary<string, float> WorkerThoughtCooldownWorldHours = new();
        public int LastDailyOpinionDay = -1;
        public int StreetLitterExposureDay = -1;
        public float StreetLitterExposureToday;
        public float StreetLitterPeakToday;
        public int StreetLitterExposureSamplesToday;
        public float StreetLitterExposureMemory;
        public int StreetLitterExposureMemoryDay = -1;
        public int StreetLitterExposureStreakDays;
        public int Satisfaction = 70;
        public int UnhappyDays;
        public bool DepartureIntent;
        public bool IsLeavingTown;
        public bool HasDepartedTown;
        public string DepartureReason = string.Empty;
        public string LastWorkerDecisionDebugKey;
        public readonly Dictionary<string, DebugThrottleStamp> WorkerDecisionDebugThrottle = new();
        public readonly Dictionary<string, DebugThrottleStamp> LocalBusSkipDebugThrottle = new();
    }

    private sealed class WorkerInventoryEntry
    {
        public string ItemId = string.Empty;
        public int Quantity;
        public float Condition01 = 1f;
        public int AcquiredDay;
        public string SourceKey = string.Empty;
        public int InstanceId;
    }

    private sealed class WorkerAffect
    {
        public WorkerAffectKind Kind;
        public int Intensity;
        public int StartedDay;
        public float StartedWorldHour;
        public float ExpiresWorldHour;
        public LocationType? SourceLocationType;
        public int SourceInstanceId;
        public string SourceKey = string.Empty;
        public string ReasonRu = string.Empty;
        public string ReasonEn = string.Empty;
    }

    private sealed class DebugThrottleStamp
    {
        public float RealTime;
        public float WorldHour;
    }

    private sealed class WorkerThought
    {
        public string Key;
        public WorkerThoughtKind Kind;
        public WorkerThoughtTone Tone;
        public WorkerThoughtPriority Priority = WorkerThoughtPriority.Normal;
        public int Intensity;
        public string TemplateKey;
        public int CreatedDay;
        public float CreatedWorldHour;
        public bool Active;
        public int ResolvedDay;
        public float ResolvedWorldHour;
        public string ResolveReason;
        public float ExpiresWorldHour;
        public readonly List<WorkerThoughtPlaceholder> Placeholders = new();
    }

    private sealed class PendingWorkerThought
    {
        public string FormationKey;
        public string ThoughtKey;
        public WorkerThoughtKind Kind;
        public WorkerThoughtTone Tone;
        public WorkerThoughtPriority Priority = WorkerThoughtPriority.Normal;
        public int Intensity;
        public string TemplateKey;
        public WorkerThoughtSubjectType OpinionSubjectType;
        public int OpinionSubjectId;
        public string OpinionSubjectKey;
        public string OpinionFallbackLabel;
        public int OpinionDelta;
        public string CooldownKey;
        public float CooldownHours;
        public bool Active;
        public float ExpiresWorldHour;
        public int StartedDay;
        public float StartedWorldHour;
        public float ReadyWorldHour;
        public float LastRefreshedWorldHour;
        public string FormationReason;
        public bool HasKnowledgeSnapshot;
        public bool HasThoughtInfluenceApplied;
        public WorkerMemoryKind KnowledgeKind;
        public int KnowledgeOtherWorkerId;
        public string KnowledgeTopic = string.Empty;
        public int KnowledgeRumorRootId;
        public string KnowledgeOriginalTopic = string.Empty;
        public LocationType? KnowledgeBuildingType;
        public int KnowledgeBuildingInstanceId;
        public string KnowledgeBuildingLabel = string.Empty;
        public float KnowledgeExpiresWorldHour;
        public bool KnowledgeIsCityCanon;
        public readonly List<WorkerThoughtPlaceholder> Placeholders = new();
    }

    private sealed class PendingWorkerKnowledge
    {
        public string FormationKey = string.Empty;
        public WorkerCognitionKind CognitionKind = WorkerCognitionKind.Fact;
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
        public WorkerKnowledgeSourceAttitude SourceAttitude = WorkerKnowledgeSourceAttitude.Neutral;
        public int RumorRootId;
        public string OriginalTopic = string.Empty;
        public string RumorTopic = string.Empty;
        public int RumorDistortionPercent;
        public int RumorConnotationScore;
        public int RumorConnotationConfidence;
        public int SourceWorkerId;
        public WorkerSocialInteractionKind SourceInteractionKind;
        public LocationType? SourceLocationType;
        public WorkerKnowledgeFormationStage Stage = WorkerKnowledgeFormationStage.Heard;
        public int StartedDay;
        public float StartedWorldHour;
        public float StageStartedWorldHour;
        public float NextStageWorldHour;
        public float LastRefreshedWorldHour;
        public WorkerKnowledgeOpinionTone OpinionTone = WorkerKnowledgeOpinionTone.Neutral;
        public int OpinionScore;
        public int OpinionConfidence;
        public string OpinionReasonRu = string.Empty;
        public string OpinionReasonEn = string.Empty;
    }

    private sealed class WorkerThoughtPlaceholder
    {
        public string Key;
        public WorkerThoughtSubjectType SubjectType;
        public int SubjectId;
        public string SubjectKey;
        public string FallbackLabel;
    }

    private sealed class WorkerMemory
    {
        public WorkerCognitionKind CognitionKind = WorkerCognitionKind.Fact;
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
        public WorkerKnowledgeSourceAttitude SourceAttitude = WorkerKnowledgeSourceAttitude.Neutral;
        public int RumorRootId;
        public string OriginalTopic = string.Empty;
        public string RumorTopic = string.Empty;
        public int RumorDistortionPercent;
        public int RumorConnotationScore;
        public int RumorConnotationConfidence;
        public WorkerKnowledgeOpinionTone OpinionTone = WorkerKnowledgeOpinionTone.Neutral;
        public int OpinionScore;
        public int OpinionConfidence;
        public string OpinionReasonRu = string.Empty;
        public string OpinionReasonEn = string.Empty;
        public int FormedFromWorkerId;
        public bool IsCityCanonKnowledge;
        public int CityCanonAdoptionCount;
        public int CityCanonAdoptionRequired;
        public float FormationStartedWorldHour;
        public float FormationCompletedWorldHour;
        public int CreatedDay;
        public float CreatedWorldHour;
        public float ExpiresWorldHour;
    }

    private sealed class NoosphereKnowledgeLogEntry
    {
        public NoosphereKnowledgeEventKind EventKind;
        public WorkerCognitionKind CognitionKind = WorkerCognitionKind.Fact;
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
        public WorkerKnowledgeSourceAttitude SourceAttitude = WorkerKnowledgeSourceAttitude.Neutral;
        public int RumorRootId;
        public string OriginalTopic = string.Empty;
        public string RumorTopic = string.Empty;
        public int RumorDistortionPercent;
        public int RumorConnotationScore;
        public int RumorConnotationConfidence;
        public WorkerKnowledgeOpinionTone OpinionTone = WorkerKnowledgeOpinionTone.Neutral;
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

    private sealed class WorkerOpinion
    {
        public WorkerThoughtSubjectType SubjectType;
        public int SubjectId;
        public string SubjectKey;
        public string FallbackLabel;
        public int Score;
        public int Confidence;
        public float LastUpdatedWorldHour;
    }

    private sealed class WorkerLifeOpinion
    {
        public WorkerLifeOpinionCategory Category;
        public bool HasScore;
        public int Score;
        public int Confidence;
        public float LastUpdatedWorldHour;
    }

    private sealed class WorkerDailyOpinion
    {
        public WorkerCognitionKind CognitionKind = WorkerCognitionKind.Experience;
        public int Day;
        public WorkerDailyOpinionTone FinalTone;
        public int Score;
        public int Confidence;
        public string SummaryRu = string.Empty;
        public string SummaryEn = string.Empty;
        public string MainReasonRu = string.Empty;
        public string MainReasonEn = string.Empty;
        public string SecondaryReasonRu = string.Empty;
        public string SecondaryReasonEn = string.Empty;
        public string PositiveReasonRu = string.Empty;
        public string PositiveReasonEn = string.Empty;
        public string NegativeReasonRu = string.Empty;
        public string NegativeReasonEn = string.Empty;
        public int PositiveThoughtCount;
        public int NegativeThoughtCount;
        public int CriticalActiveThoughtCount;
        public int SatisfactionDeltaHint;
        public int EmittedSocialSignalCount;
        public WorkerDailyOpinionFactorKind DominantKind = WorkerDailyOpinionFactorKind.City;
        public readonly List<WorkerDailyOpinionFactor> Factors = new();
    }

    private sealed class WorkerDailyOpinionFactor
    {
        public WorkerDailyOpinionFactorKind Kind;
        public int Score;
        public string ReasonRu = string.Empty;
        public string ReasonEn = string.Empty;
    }

    private sealed class WorkerSocialMemory
    {
        public int OtherWorkerId;
        public int Exposure;
        public int Familiarity;
        public int Relationship;
        public int InteractionCount;
        public int LastInteractionDay;
        public float LastInteractionWorldHour;
        public float NextFamiliarityDecayWorldHour;
        public WorkerSocialInteractionKind LastKind;
        public LocationType? LastLocationType;
        public string LastConversationTopic = string.Empty;
        public bool LastConversationTopicWasPositive;
        public WorkerKnowledgeSourceAttitude LastConversationTopicAttitude = WorkerKnowledgeSourceAttitude.Neutral;
    }

    private enum WorkerSocialInteractionKind
    {
        IdleConversation,
        ServiceCoPresence,
        CoworkerShift,
        FamilyFormation,
        PlayerPromptedConversation,
        PlayerPromptedConversationFailed
    }

    private enum WorkerChildStage
    {
        Baby,
        Toddler,
        Child,
        Teen,
        YoungAdult
    }

    private sealed class WorkerFamily
    {
        public int Id;
        public int HouseIndex;
        public int CreatedDay;
        public float CreatedWorldHour;
        public float NextChildBirthWorldHour;
        public float LastChildBornWorldHour;
        public int Happiness = 70;
        public int BirthJoyUntilDay;
        public int LastDailyUpdateDay;
        public int LastHappinessDelta;
        public string LastHappinessReason = "New household";
        public int LastDailyUpkeepDay;
        public int LastDailyUpkeepAmount;
        public int LastDailyUpkeepPaidAmount;
        public int LastDailyUpkeepShortfall;
        public int LastAdultMoneyTotal;
        public readonly List<int> MemberWorkerIds = new();
        public readonly List<int> ChildIds = new();
    }

    private sealed class WorkerFamilyPendingFormation
    {
        public int WorkerId;
        public int HouseIndex;
        public float CreatedWorldHour;
        public float DueWorldHour;
    }

    private sealed class WorkerChild
    {
        public int Id;
        public int FamilyId;
        public int HouseIndex;
        public string Name;
        public WorkerGender Gender;
        public WorkerChildStage Stage = WorkerChildStage.Baby;
        public int BornDay;
        public float BornWorldHour;
        public int StageStartedDay;
        public int NextStageDay;
        public float YardLateralOffset;
        public float YardDepthOffset;
        public float AnimationPhase;
        public GameObject RootObject;
        public Transform VisualRoot;
        public Transform HeadTransform;
        public Transform BodyTransform;
        public Transform LeftArmTransform;
        public Transform RightArmTransform;
        public Transform LeftLegTransform;
        public Transform RightLegTransform;
    }

    private sealed class LaborExchangePosting
    {
        public int Id;
        public VacancyKind Kind;
        public LocationType BuildingType;
        public int BuildingInstanceId;
        public int SlotIndex;
        public int ShiftIndex = -1;
        public int TruckNumber;
        public int ReservedWorkerId;
        public float CreatedAtWorldHour;
        public int OfferedSalary;
        public int ContractWorkDays;
        public int MarketPressure;
        public int RequiredProfessionalLevel = 1;
        public float LastSalaryRevisionWorldHour;
    }

    private readonly struct LaborExchangeCandidate
    {
        public LaborExchangeCandidate(VacancyKind kind, LocationType buildingType, int buildingInstanceId, int slotIndex, int shiftIndex, int truckNumber, int priority)
        {
            Kind = kind;
            BuildingType = buildingType;
            BuildingInstanceId = buildingInstanceId;
            SlotIndex = slotIndex;
            ShiftIndex = shiftIndex;
            TruckNumber = truckNumber;
            Priority = priority;
        }

        public readonly VacancyKind Kind;
        public readonly LocationType BuildingType;
        public readonly int BuildingInstanceId;
        public readonly int SlotIndex;
        public readonly int ShiftIndex;
        public readonly int TruckNumber;
        public readonly int Priority;
    }
}
