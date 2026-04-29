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
        public AudioSource TruckFxAudioSource;
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
        public TripPhase CurrentTripPhase = TripPhase.None;
        public RefuelPhase CurrentRefuelPhase = RefuelPhase.None;
        public TruckInteractionType ActiveTruckInteraction = TruckInteractionType.None;
        public TruckInteractionType QueuedTruckInteraction = TruckInteractionType.None;
        public Quaternion TruckInteractionTargetRotation;
        public Vector3 TruckInteractionBuildingPoint;
        public LocationType? ActiveServiceLocation;
        public LocationType? QueuedServiceLocation;
        public int ParkingSlotIndex;
        public float ExhaustEmitTimer;
        public float DirtDustEmitTimer;
        public bool IsPurchaseArrivalActive;
        public readonly List<Vector3> PurchaseArrivalWaypoints = new();
        public int PurchaseArrivalWaypointIndex;
        public float PurchaseArrivalSpeed = 4.6f;
    }

    private sealed class WorkerEffectState
    {
        public string EffectId;
        public string EnglishName;
        public string RussianName;
        public string EnglishDescription;
        public string RussianDescription;
        public int DrivingDelta;
        public int StaminaDelta;
        public int ProductionDelta;
        public int LogisticsDelta;
        public float RemainingHours;
    }

    private enum WorkerPerkKind
    {
        Alcoholism,
        Gambler,
        Nightowl,
        Ironman,
        Motorhead,
        Trader,
        Handyman,
        Socialite,
        Frugal,
        Quicklearner
    }

    private enum WorkerPerkType
    {
        Positive,
        Negative
    }

    private enum WorkerEducation
    {
        Basic,
        Skilled
    }

    private enum WorkerGender { Male, Female }

    private sealed class DriverAgent
    {
        public int DriverId;
        public string DriverName;
        public WorkerGender Gender;
        public bool HasPortrait;
        public int PortraitSkinTone;
        public int PortraitHairStyle;
        public int PortraitHairColor;
        public int PortraitEyeStyle;
        public int PortraitMouthStyle;
        public int PortraitAccessory;
        public int PortraitHeadShape;
        public bool HasWorkerStats;
        public int DrivingSkill;
        public int StaminaSkill;
        public int ProductionSkill;
        public int LogisticsSkill;
        public readonly List<WorkerPerkKind> Perks = new();
        public readonly List<WorkerEffectState> ActiveEffects = new();
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
        public Transform DriverFuelCanTransform;
        public Transform DriverFlashlightTransform;
        public Light DriverFlashlightLight;
        public Renderer DriverFlashlightRenderer;
        public Material DriverFlashlightMaterial;
        public DriverRestPhase RestPhase = DriverRestPhase.None;
        public float SleepTimer;
        public Vector3 MotelIdlePosition;
        public int AssignedTruckNumber;
        public int Salary = 25;
        public int Money = 30;
        public bool WaitingForShiftAtParking;
        public bool NeedsShiftEndReturn;
        public bool IsShiftSalaryPending;
        public DriverRescuePhase WalkPhase = DriverRescuePhase.None;
        public Vector3 WalkTargetWorld;
        public readonly List<Vector3> WalkPath = new();
        public int WalkWaypointIndex;
        public float WalkAnimationTime;
        public int IdleWanderPointIndex = -1;
        public float IdleWanderPauseTimer;
        public float IdleConversationTimer;
        public int IdleConversationPartnerId = -1;
        public float IdleConversationCooldownTimer;
        public bool IsArrivingByBus;
        public int SittingBenchIndex = -1;
        public int CityParkBenchIndex = -1;
        public int CityParkPromenadeStep;
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
        public int GamblingBet;
        public int GamblingPayout;
        public int GamblingMultiplier;
        public bool GamblingMoneyPending;
        public int GamblingBetCount;
        public bool GamblerLostLastTime;
        public bool GamblerBroke;
        public bool SleptToday;
        public float HoursSinceMeal = 0f;
        public float HoursSinceSleep = 0f;
        public float HoursSinceLeisure = 0f;
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
        public int IdleCatPetTargetIndex = -1;
        public WorkerEducation Education = WorkerEducation.Basic;
        public int Age;
        public int DaysOnMap;
        public int OwnedCarModelIndex = -1;
        public GameObject OwnedCarObject;
        public LocationType? AssignedBuildingType;
        public bool IsInsideBuilding;
        public LocationType? WarehouseDeliveryTarget;
        public WarehouseResourceType WarehouseDeliveryResourceType;
        public int WarehouseDeliveryAmount;
        public bool IsCarryingWarehouseDelivery;
        public string LastWorkerDecisionDebugKey;
    }
}
