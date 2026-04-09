using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private const int GridWidth = 28;
    private const int GridHeight = 28;
    private const float CellSize = 1f;
    private const float RoadHeight = 0.12f;
    private const float TruckCruiseSpeed = 1.9f;
    private const int ForestMaxLogsStorage = 10;
    private const float ForestLogProgressPerChop = 0.08f;
    private const float CameraPanSpeed = 9f;
    private const float CameraDragPanMultiplier = 0.035f;
    private const float CameraZoomSpeed = 8f;
    private const float CameraMinHeight = 6f;
    private const float CameraMaxHeight = 70f;
    private const float CameraMinDistance = 6f;
    private const float CameraMaxDistance = 70f;
    private const float EdgeHighwayBusSpeed = 2.8f;
    private const float EdgeHighwayBusSpawnIntervalMin = 14f;
    private const float EdgeHighwayBusSpawnIntervalMax = 30f;
    private const float EdgeHighwayBusSpawnSpacing = 5.5f;
    private const float EdgeHighwayBusLaneOffset = 0.32f;
    private const float EdgeHighwayBusLift = 0.24f;
    private const float EdgeHighwayBusPassbyDistance = 5.5f;
    private const float HiringBusStopDuration = 2.8f;
    private const float TruckFollowDistance = 4.4f;
    private const float TruckFollowHeight = 2.05f;
    private const float TruckFollowLookHeight = 1.05f;
    private const float TruckSegmentStartLift = 0.24f;
    private const float TruckSuspensionBobAmount = 0.045f;
    private const float TruckWheelRadius = 0.12f;
    private const float TruckCargoInteractionDuration = 3f;
    private const float TruckFuelCapacity = 100f;
    private const float TruckFuelPerCell = 1f;
    private const float DriverEnergyMax = 100f;
    private const float DriverEnergyCriticalThreshold = 30f;
    private const float DriverEnergyDrainPerSecond = DriverEnergyMax / (DayNightCycleDuration / 24f * 16f);
    private const float DriverSleepDuration = DayNightCycleDuration / 24f * 6f;
    private const float DriverWalkSpeed = 2.2f;
    private const float DriverIdleWanderSpeed = 1.35f;
    private const float DriverIdleWanderPauseMin = 2.2f;
    private const float DriverIdleWanderPauseMax = 4.6f;
    private const float DriverIdlePersonalSpace = 0.62f;
    private const float DriverIdleConversationDistance = 1.5f;
    private const float DriverIdleConversationDurationMin = 3.4f;
    private const float DriverIdleConversationDurationMax = 6.2f;
    private const float DriverIdleConversationCooldownMin = 7.5f;
    private const float DriverIdleConversationCooldownMax = 12.5f;
    private const float DriverIdleConversationStartChance = 0.3f;
    private const int StartingTreasury = 350;
    private const float MoneyPopupDuration = 1.4f;
    private const int AudioSampleRate = 22050;
    private const int MaxTruckCount = 5;
    private const int HireTruckCost = 300;
    private const int HireDriverCost = 50;
    private const int MaxMoneyLedgerEntries = 128;
    private const float DayNightCycleDuration = 300f;
    private const float DriverShiftArrivalLeadHours = 1f;
    private const float DioramaCameraPitch = 42f;
    private static readonly Vector3 DioramaCameraOffset = new(-11.5f, 15f, -11.5f);
    private static readonly Vector3 CloudTravelDir = new Vector3(1f, 0f, 0.4f).normalized;
    private const float CloudTravelLength = 80f;  // full path spawn→exit (wider spawn X=-30 needs longer path)

    private readonly HashSet<Vector2Int> roadCells = new();
    private readonly HashSet<Vector2Int> edgeHighwayCells = new();
    private readonly HashSet<Vector2Int> miscOccupiedCells = new();
    private readonly Dictionary<Vector2Int, GameObject> roadVisuals = new();
    private readonly Dictionary<LocationType, LocationData> locations = new();
    private readonly List<Vector2Int> activePath = new();
    private readonly List<Transform> truckWheels = new();
    private readonly List<Transform> truckFrontWheels = new();
    private readonly List<Light> truckHeadlights = new();
    private readonly List<Light> locationNightLights = new();
    private readonly List<Renderer> locationNightLightRenderers = new();
    private readonly List<RoadLanternData> roadLanterns = new();
    private readonly List<TruckAgent> truckAgents = new();
    private readonly List<DriverAgent> driverAgents = new();
    private readonly List<ForestWorkerAmbient> forestWorkers = new();
    private readonly List<Vector3> forestWorkPoints = new();
    private readonly List<Transform> forestWorkTargetTrees = new();
    private readonly List<ForestTreeWobble> forestTreeWobbles = new();
    private readonly List<MiscTreeSway> miscTreeSways = new();
    private readonly List<DistantCloudData> distantClouds = new();
    private readonly List<MoneyLedgerEntry> moneyLedgerEntries = new();
    private readonly HashSet<LocationType> occupiedServiceLocations = new();
    private readonly Dictionary<LocationType, GameObject> locationSelectionHighlights = new();
    private readonly List<EdgeHighwayBusData> edgeHighwayBuses = new();
    private float[,] terrainHeights = new float[GridWidth, GridHeight];

    private Camera mainCamera;
    private GameObject truckObject;
    private Transform truckVisualRoot;
    private Transform truckBodyTransform;
    private Transform truckCabinTransform;
    private Renderer truckHeadlightLeftRenderer;
    private Renderer truckHeadlightRightRenderer;
    private Transform worldRoot;
    private Transform groundRoot;
    private Transform roadsRoot;
    private Transform lanternsRoot;
    private Transform miscRoot;
    private AudioSource uiAudioSource;
    private AudioSource ambientAudioSource;
    private AudioSource forestAudioSource;
    private AudioSource forestWorkerAudioSource;
    private AudioSource dayBirdsAudioSource;
    private AudioSource nightWindAudioSource;
    private AudioSource nightCricketsAudioSource;
    private AudioSource gasStationAudioSource;
    private AudioSource warehouseAudioSource;
    private AudioSource ambienceFxAudioSource;
    private AudioSource townAudioSource;
    private AudioSource truckLoopAudioSource;
    private AudioSource truckFxAudioSource;
    private Material groundSurfaceMaterial;
    private Material grassSurfaceMaterial;
    private Texture2D groundSurfaceTexture;
    private Texture2D grassSurfaceTexture;
    private Light mainDirectionalLight;
    private GameObject selectedLocationLabelRoot;
    private TextMesh selectedLocationLabelText;
    private readonly List<TextMesh> selectedLocationLabelOutlines = new();
    private GameObject cargoTransferCrate;
    private GameObject buildHoverHighlight;
    private Transform edgeHighwayBusRoot;
    private Vector2Int truckCell;
    private Vector3 truckTargetWorld;
    private Vector3 truckSegmentStartWorld;
    private Vector3 truckSmoothedForward = Vector3.forward;
    private bool isTruckMoving;
    private bool isTruckInteracting;
    private bool isTruckWaitingForService;
    private bool isDriverRescueActive;
    private CargoType truckCargoType = CargoType.None;
    private int truckCargoAmount;
    private float productionTimer;
    private Vector3 cameraFocusPoint;
    private Vector3 cameraOffset;
    private Vector3 cameraTargetOffset;
    private Vector2 lastMousePosition;
    private Vector2 rightMousePressPosition;
    private float truckSegmentProgress;
    private float truckSegmentDuration;
    private float truckWheelSpinAngle;
    private float truckSteerAngle;
    private float truckInteractionTimer;
    private float moneyPopupTimer;
    private float truckFuel = TruckFuelCapacity;
    private float dayNightCycleTimer = DayNightCycleDuration * (5f / 24f);
    private float currentStylizedDaylight = 1f;
    private float forestProductionProgress;
    private float sawmillProcessingTimer;
    private float dayBirdTimer;
    private float nightOwlTimer;
    private float lanternBuzzTimer;
    private float warehouseCreakTimer;
    private float terrainNoiseOffsetX;
    private float terrainNoiseOffsetY;
    private LocationType? selectedLocation;
    private bool isTruckDetailsOpen;
    private bool isRightMouseDragging;
    private bool isCameraReturningToDiorama;
    private bool isCameraRotatingToTarget;
    private bool isTruckCameraFocused;
    private bool isTruckAutoModeEnabled;
    private bool isFleetScreenDirty = true;
    private bool isMainMenuOpen = true;
    private int selectedTruckNumber = 1;
    private int money;
    private int currentAssignedTripReward;
    private bool isFleetPanelOpen;
    private bool isShiftsPanelOpen;
    private bool isDriversPanelOpen;
    private int selectedShiftDriverId; // DriverId, 0 = none
    private bool isResourcesPanelOpen;
    private bool isEconomyPanelOpen;
    private bool isBuildPanelOpen;
    private int moneyPopupAmount;
    private int gameSpeedMultiplier = 1;
    private int lastActiveGameSpeedMultiplier = 1;
    private int nextHireTruckNumber = 2;
    private int nextDriverId = 1;
    private TripType currentAssignedTrip = TripType.None;
    private BuildTool activeBuildTool = BuildTool.None;
    private Vector2Int? hoveredBuildCell;
    private float edgeHighwayBusSpawnTimerCitySide;
    private float edgeHighwayBusSpawnTimerOuterSide;
    private HiringDriverArrivalData hiringDriverArrival;

    private sealed class RoadLanternData
    {
        public Light Light;
        public Renderer GlowRenderer;
        public float ActivationOffset;
        public float FlickerSeed;
        public float FlickerSpeed;
        public float FlickerStrength;
        public float FlickerThreshold;
    }

    private sealed class MoneyLedgerEntry
    {
        public string TimeLabel;
        public int TreasuryDelta;
        public string FromLabel;
        public string ToLabel;
        public string Reason;
        public int? TreasuryAfter;
        public int? RecipientBalanceAfter;
    }

    private sealed class EdgeHighwayBusData
    {
        public Transform RootTransform;
        public float WorldX;
        public float TravelDirection;
        public bool IsCitySideLane;
        public float Speed;
        public float BobPhase;
        public Color BodyColor;
        public bool HasPlayedPassbyAudio;
        public bool HasEnteredRoadStrip;
        public Renderer HeadlightLeftRenderer;
        public Renderer HeadlightRightRenderer;
        public Light HeadlightLeft;
        public Light HeadlightRight;
    }

    private sealed class HiringDriverArrivalData
    {
        public DriverAgent Driver;
        public Transform BusRootTransform;
        public Renderer HeadlightLeftRenderer;
        public Renderer HeadlightRightRenderer;
        public Light HeadlightLeft;
        public Light HeadlightRight;
        public float BusWorldX;
        public float BusSpeed;
        public float StopTimer;
        public float BobPhase;
        public HiringDriverArrivalPhase Phase;
    }

    private enum HiringDriverArrivalPhase
    {
        WaitingLaneClear,
        ApproachingStop,
        StoppedForDropoff,
        DriverWalkingToMotel,
        Departing
    }
    private TripPhase currentTripPhase = TripPhase.None;
    private RefuelPhase currentRefuelPhase = RefuelPhase.None;
    private TruckInteractionType activeTruckInteraction = TruckInteractionType.None;
    private TruckInteractionType queuedTruckInteraction = TruckInteractionType.None;
    private Quaternion truckInteractionTargetRotation;
    private Vector3 truckInteractionBuildingPoint;
    private LocationType? activeServiceLocation;
    private LocationType? queuedServiceLocation;
    private AudioClip uiSelectClip;
    private AudioClip menuHoverClip;
    private AudioClip uiPanelOpenClip;
    private AudioClip uiPanelCloseClip;
    private AudioClip ambientWindClip;
    private AudioClip dayBirdsClip;
    private AudioClip forestRustleClip;
    private AudioClip forestChopClip;
    private AudioClip nightWindClip;
    private AudioClip nightCricketsClip;
    private AudioClip gasStationHumClip;
    private AudioClip sawmillHumClip;
    private AudioClip warehouseCreakClip;
    private AudioClip owlClip;
    private AudioClip lanternBuzzClip;
    private AudioClip truckIdleClip;
    private AudioClip truckRollClip;
    private AudioClip cargoPickupClip;
    private AudioClip cargoDropClip;
    private AudioClip routeAssignForestSawmillClip;
    private AudioClip routeAssignSawmillWarehouseClip;
    private AudioClip routeAssignRefuelClip;
    private AudioClip forestLoadCueClip;
    private AudioClip sawmillUnloadCueClip;
    private AudioClip sawmillLoadCueClip;
    private AudioClip warehouseUnloadBoardsCueClip;
    private AudioClip gasStationRefuelCueClip;
    private AudioClip parkingReturnCueClip;
    private AudioClip moneyRewardClip;
    private AudioClip edgeHighwayBusPassbyClip;
    private float truckEngineAudioPhaseOffset;
    private float truckEngineAudioWobbleSpeed = 1f;
    private float truckEngineAudioPitchBias = 1f;
    private float truckEngineAudioVolumeBias = 1f;

    private enum LocationType
    {
        Parking,
        GasStation,
        Forest,
        Warehouse,
        Sawmill,
        Motel,
        BusStop
    }

    private enum CargoType
    {
        None,
        Logs,
        Boards
    }

    private enum TransportTask
    {
        None,
        ReturnToParking,
        PickUpAtForest,
        DeliverToSawmill,
        PickUpAtSawmill,
        DeliverToWarehouse
    }

    private enum TruckInteractionType
    {
        None,
        LoadAtForest,
        UnloadAtSawmill,
        LoadAtSawmill,
        UnloadAtWarehouse,
        RefuelAtGasStation
    }

    private enum TripType
    {
        None,
        ForestToSawmill,
        SawmillToWarehouse
    }

    private enum BuildTool
    {
        None,
        Road
    }

    private enum TripPhase
    {
        None,
        ToPickup,
        Loading,
        ToDropoff,
        Unloading,
        ReturnToParking
    }

    private enum RefuelPhase
    {
        None,
        ToGasStation,
        Refueling,
        ReturnToParking
    }

    private enum DriverRescuePhase
    {
        None,
        IdleWander,
        ToMotelFromBusStop,
        ToGasStation,
        ToTruck,
        ToMotelEntrance,
        ToTruckAtMotel,
        ToParkingForShift
    }

    private enum DriverRestPhase
    {
        None,
        ToMotel,
        ParkAtMotel,
        DriverWalkToMotel,
        Sleeping,
        DriverWalkToTruck,
        ReturnToParking
    }


    private sealed class LocationData
    {
        public string Label;
        public Vector2Int Min;
        public Vector2Int Max;
        public Vector2Int Anchor;
        public Color BaseColor;
        public int LogsStored;
        public int BoardsStored;
        public GameObject RootObject;
        public Renderer BaseRenderer;
        public readonly List<GameObject> StoredLogVisuals = new();
        public readonly List<GameObject> StoredBoardVisuals = new();

        public bool Contains(Vector2Int cell)
        {
            return cell.x >= Min.x && cell.x <= Max.x && cell.y >= Min.y && cell.y <= Max.y;
        }
    }

    private sealed class TripOption
    {
        public TripType Type;
        public string Title;
        public string Description;
        public int Reward;
    }

    private enum ForestWorkerState
    {
        Walking,
        Chopping,
        Pausing
    }

    private sealed class ForestWorkerAmbient
    {
        public string Name;
        public GameObject RootObject;
        public Transform VisualRoot;
        public Transform BodyTransform;
        public Transform HeadTransform;
        public Transform CapTransform;
        public Transform LeftArmTransform;
        public Transform RightArmTransform;
        public Transform LeftLegTransform;
        public Transform RightLegTransform;
        public Transform AxeTransform;
        public Transform FlashlightTransform;
        public Light FlashlightLight;
        public Renderer FlashlightRenderer;
        public Vector3 TargetWorldPosition;
        public ForestWorkerState State;
        public float MoveSpeed;
        public float StateTimer;
        public float AnimationTime;
        public float ChopSoundCooldown;
        public float PauseYaw;
        public int WorkPointIndex;
    }

    private sealed class ForestTreeWobble
    {
        public Transform TreeTransform;
        public Quaternion BaseRotation;
        public Vector3 Axis;
        public float Timer;
        public float Duration;
        public float Amplitude;
    }

    private sealed class MiscTreeSway
    {
        public Transform RootTransform;
        public Quaternion BaseRotation;
        public float PhaseOffset;
        public float SecondaryPhaseOffset;
        public float Speed;
        public float PitchAmplitude;
        public float RollAmplitude;
    }

    private sealed class DistantCloudData
    {
        public Transform RootTransform;
        public Vector3 SpawnPosition;   // world position at TravelOffset = 0
        public float TravelOffset;      // current distance along CloudTravelDir
        public float TravelSpeed;       // units/sec
        public float VerticalBobAmplitude;
        public float VerticalBobSpeed;
        public float PhaseOffset;
    }

    private sealed class TruckAgent
    {
        public int TruckNumber;
        public string DisplayName;
        public GameObject TruckObject;
        public Transform TruckVisualRoot;
        public Transform TruckBodyTransform;
        public Transform TruckCabinTransform;
        public Renderer TruckHeadlightLeftRenderer;
        public Renderer TruckHeadlightRightRenderer;
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
    }

    private sealed class DriverAgent
    {
        public int DriverId;
        public string DriverName;
        // ShiftStartHour: -1 = idle/no shift assigned
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
        public float Energy = DriverEnergyMax;
        public float SleepStartEnergy = DriverEnergyMax;
        public bool NeedsRestAfterTrip;
        public DriverRestPhase RestPhase = DriverRestPhase.None;
        public float SleepTimer;
        public Vector3 MotelIdlePosition;
        public int AssignedTruckNumber;
        public int Salary = 25;
        public int Money;
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
    }

    private int GetCurrentHour()
    {
        float normalized = dayNightCycleTimer / DayNightCycleDuration;
        return Mathf.FloorToInt(normalized * 24f) % 24;
    }

    private int GetCurrentTotalMinutes()
    {
        float normalized = dayNightCycleTimer / DayNightCycleDuration;
        return Mathf.FloorToInt(normalized * 24f * 60f) % (24 * 60);
    }

    private int GetMinutesUntilShiftStart(DriverAgent driver)
    {
        if (driver == null || driver.ShiftStartHour < 0)
        {
            return int.MaxValue;
        }

        int currentMinutes = GetCurrentTotalMinutes();
        int shiftStartMinutes = driver.ShiftStartHour * 60;
        return (shiftStartMinutes - currentMinutes + 24 * 60) % (24 * 60);
    }

    // Returns true if 'hour' falls within the 8-hour window starting at 'shiftStart'
    private static bool IsHourInShiftWindow(int hour, int shiftStart)
    {
        return (hour - shiftStart + 24) % 24 < 8;
    }

    // Shift display string: "06:00 – 14:00"
    private static string GetShiftRangeLabel(int shiftStart)
    {
        int end = (shiftStart + 8) % 24;
        return $"{shiftStart:00}:00 – {end:00}:00";
    }

    private bool IsDriverOnShift(DriverAgent driver)
    {
        if (driver == null) return false;
        if (driver.ShiftStartHour < 0) return false; // Idle — no shift assigned
        return driver.IsOnActiveShift;
    }

    private static bool IsDriverIdleWanderPhase(DriverAgent driver)
    {
        return driver != null && driver.WalkPhase == DriverRescuePhase.IdleWander;
    }

    private static bool IsDriverIdleConversing(DriverAgent driver)
    {
        return driver != null && driver.IdleConversationTimer > 0f && driver.WalkPhase == DriverRescuePhase.None;
    }

    private static bool IsDriverBusyWalkPhase(DriverAgent driver)
    {
        return driver != null &&
               driver.WalkPhase != DriverRescuePhase.None &&
               driver.WalkPhase != DriverRescuePhase.IdleWander;
    }

    private bool ShouldDriverHeadToShift(DriverAgent driver)
    {
        if (driver == null || driver.ShiftStartHour < 0 || driver.AssignedTruckNumber <= 0)
        {
            return false;
        }

        int minutesUntilShiftStart = GetMinutesUntilShiftStart(driver);
        return minutesUntilShiftStart > 0 && minutesUntilShiftStart <= Mathf.RoundToInt(DriverShiftArrivalLeadHours * 60f);
    }

    private bool TryBoardDriverToAssignedTruck(DriverAgent driver)
    {
        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (driver == null || assignedTruck == null)
        {
            return false;
        }

        if (!driver.WaitingForShiftAtParking)
        {
            return false;
        }

        if (assignedTruck.Driver != null && assignedTruck.Driver != driver)
        {
            return false;
        }

        LoadTruckState(assignedTruck);
        bool canBoard =
            !isTruckMoving &&
            !isTruckInteracting &&
            !isDriverRescueActive &&
            currentAssignedTrip == TripType.None &&
            currentRefuelPhase == RefuelPhase.None &&
            IsTruckInsideParking();
        SaveTruckState(assignedTruck);
        if (!canBoard)
        {
            return false;
        }

        assignedTruck.Driver = driver;
        driver.WaitingForShiftAtParking = false;
        driver.DriverObject.SetActive(false);
        driver.WalkPhase = DriverRescuePhase.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;
        SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} boarded {assignedTruck.DisplayName} in Parking.");
        return true;
    }

    private void StartDriverShiftCommute(DriverAgent driver)
    {
        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (driver == null || assignedTruck == null || driver.DriverObject == null)
        {
            return;
        }

        if (driver.DriverObject.activeSelf == false)
        {
            driver.DriverObject.SetActive(true);
            driver.DriverObject.transform.position = driver.MotelIdlePosition;
            driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }

        driver.WaitingForShiftAtParking = false;
        driver.IdleWanderPauseTimer = 0f;
        driver.IdleWanderPointIndex = -1;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        driver.WalkAnimationTime = 0f;
        ApplyDriverPose(driver, 0f, 0f);
        driver.WalkPhase = DriverRescuePhase.ToParkingForShift;
        driver.WalkTargetWorld = GetDriverParkingWaitPosition(assignedTruck);
        BuildDriverWalkPath(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld);
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} started commute to Parking for {assignedTruck.DisplayName}.");
    }

    private void StartDriverMotelRest(TruckAgent truckAgent, DriverAgent driver)
    {
        if (driver == null || truckAgent == null || driver.DriverObject == null)
        {
            return;
        }

        truckAgent.Driver = null;
        driver.IsOnActiveShift = false;
        driver.WaitingForShiftAtParking = false;
        driver.NeedsRestAfterTrip = false;
        driver.NeedsShiftEndReturn = false;
        driver.DriverObject.SetActive(true);
        driver.DriverObject.transform.position = GetDriverStandPointNearTruck();
        driver.DriverObject.transform.rotation = truckAgent.TruckObject != null ? truckAgent.TruckObject.transform.rotation : Quaternion.LookRotation(Vector3.forward, Vector3.up);
        driver.WalkAnimationTime = 0f;
        ApplyDriverPose(driver, 0f, 0f);
        driver.WalkTargetWorld = GetDriverStandPointNearLocation(LocationType.Motel);
        BuildDriverWalkPath(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld);
        driver.WalkPhase = DriverRescuePhase.ToMotelEntrance;
        driver.RestPhase = DriverRestPhase.DriverWalkToMotel;
        SessionDebugLogger.Log("REST", $"{driver.DriverName} left {truckAgent.DisplayName} in Parking and is walking to Motel.");
    }

    private void UpdateDriverShiftPreparation(DriverAgent driver)
    {
        if (driver == null || driver.IsArrivingByBus || driver.ShiftStartHour < 0 || driver.IsOnActiveShift || driver.RestPhase != DriverRestPhase.None || IsDriverBusyWalkPhase(driver) || driver.AssignedTruckNumber <= 0)
        {
            return;
        }

        bool shouldCommuteToShift = ShouldDriverHeadToShift(driver) || IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour);
        if (!shouldCommuteToShift)
        {
            return;
        }

        if (driver.WaitingForShiftAtParking)
        {
            TryBoardDriverToAssignedTruck(driver);
            return;
        }

        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (assignedTruck == null)
        {
            return;
        }

        StartDriverShiftCommute(driver);
    }

    private void UpdateDriverIdleWander(DriverAgent driver)
    {
        if (driver == null || driver.DriverObject == null)
        {
            return;
        }

        if (driver.IsArrivingByBus ||
            driver.RestPhase != DriverRestPhase.None ||
            driver.IsOnActiveShift ||
            driver.WaitingForShiftAtParking ||
            GetCurrentTruckForDriver(driver) != null)
        {
            if (IsDriverIdleWanderPhase(driver))
            {
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
            }

            driver.IdleConversationTimer = 0f;
            driver.IdleConversationPartnerId = -1;

            return;
        }

        if (driver.IdleConversationCooldownTimer > 0f)
        {
            driver.IdleConversationCooldownTimer = Mathf.Max(0f, driver.IdleConversationCooldownTimer - Time.deltaTime * gameSpeedMultiplier);
        }

        if (IsDriverBusyWalkPhase(driver))
        {
            return;
        }

        if (IsDriverIdleConversing(driver))
        {
            DriverAgent partner = GetDriverAgentById(driver.IdleConversationPartnerId);
            if (!CanDriverContinueIdleConversation(driver, partner))
            {
                StopDriverIdleConversation(driver, true);
                return;
            }

            driver.IdleConversationTimer -= Time.deltaTime * gameSpeedMultiplier;
            if (driver.IdleConversationTimer <= 0f)
            {
                StopDriverIdleConversation(driver, true);
            }
            return;
        }

        if (IsDriverIdleWanderPhase(driver))
        {
            return;
        }

        if (driver.ShiftStartHour >= 0 &&
            (ShouldDriverHeadToShift(driver) || IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour)))
        {
            return;
        }

        if (driver.IdleWanderPauseTimer > 0f)
        {
            driver.IdleWanderPauseTimer -= Time.deltaTime * gameSpeedMultiplier;
            return;
        }

        if (TryStartIdleConversation(driver))
        {
            return;
        }

        Vector3 startPosition = driver.DriverObject.transform.position;
        Vector3 targetPosition = FindDriverIdleWanderTarget(driver, startPosition);
        if ((targetPosition - startPosition).sqrMagnitude < 0.04f)
        {
            driver.IdleWanderPointIndex++;
            driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
            return;
        }

        driver.WalkPhase = DriverRescuePhase.IdleWander;
        driver.IdleWanderPointIndex++;
        driver.WalkAnimationTime = 0f;
        BuildDriverWalkPath(driver, startPosition, targetPosition);
        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} started motel idle walk.");
    }

    private Vector3 FindDriverIdleWanderTarget(DriverAgent driver, Vector3 startPosition)
    {
        int nextPointIndex = driver.IdleWanderPointIndex + 1;
        float personalSpaceSqr = DriverIdlePersonalSpace * DriverIdlePersonalSpace;
        for (int attempt = 0; attempt < 16; attempt++)
        {
            Vector3 candidate = GetDriverIdleMotelWanderPosition(driver.DriverId - 1, nextPointIndex + attempt);
            Vector3 flatDelta = candidate - startPosition;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude < 0.04f)
            {
                continue;
            }

            bool blockedByOtherDriver = false;
            for (int i = 0; i < driverAgents.Count; i++)
            {
                DriverAgent other = driverAgents[i];
                if (other == null || other == driver || other.DriverObject == null || !other.DriverObject.activeSelf)
                {
                    continue;
                }

                Vector3 otherDelta = other.DriverObject.transform.position - candidate;
                otherDelta.y = 0f;
                if (otherDelta.sqrMagnitude < personalSpaceSqr)
                {
                    blockedByOtherDriver = true;
                    break;
                }
            }

            if (!blockedByOtherDriver)
            {
                return candidate;
            }
        }

        return startPosition;
    }

    private void UpdateHiringDriverArrival()
    {
        if (hiringDriverArrival == null)
        {
            return;
        }

        DriverAgent driver = hiringDriverArrival.Driver;
        if (driver == null)
        {
            CleanupHiringDriverArrival(false);
            return;
        }

        float dt = Time.deltaTime * gameSpeedMultiplier;
        if (dt <= 0f)
        {
            return;
        }

        switch (hiringDriverArrival.Phase)
        {
            case HiringDriverArrivalPhase.WaitingLaneClear:
                if (!HasActiveCitySideAmbientBus())
                {
                    CreateHiringArrivalBusVisual();
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.ApproachingStop;
                    SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} arrival bus entered the edge highway.");
                }
                break;

            case HiringDriverArrivalPhase.ApproachingStop:
                if (hiringDriverArrival.BusRootTransform == null)
                {
                    CleanupHiringDriverArrival(false);
                    return;
                }

                hiringDriverArrival.BusWorldX += hiringDriverArrival.BusSpeed * dt;
                float stopX = GetHiringBusStopWorldX();
                if (hiringDriverArrival.BusWorldX >= stopX)
                {
                    hiringDriverArrival.BusWorldX = stopX;
                    hiringDriverArrival.StopTimer = HiringBusStopDuration;
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.StoppedForDropoff;
                    SpawnDriverFromHiringBus();
                    SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} arrival bus stopped at Bus Stop.");
                }

                UpdateHiringBusTransform();
                break;

            case HiringDriverArrivalPhase.StoppedForDropoff:
                UpdateHiringBusTransform();
                hiringDriverArrival.StopTimer -= dt;
                if (hiringDriverArrival.StopTimer <= 0f)
                {
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.Departing;
                    SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} finished disembarking; arrival bus departing immediately.");
                }
                break;

            case HiringDriverArrivalPhase.DriverWalkingToMotel:
                UpdateHiringBusTransform();
                if (!driver.IsArrivingByBus)
                {
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.Departing;
                    SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} reached Motel; arrival bus departing.");
                }
                break;

            case HiringDriverArrivalPhase.Departing:
                if (hiringDriverArrival.BusRootTransform == null)
                {
                    CleanupHiringDriverArrival(false);
                    return;
                }

                hiringDriverArrival.BusWorldX += hiringDriverArrival.BusSpeed * dt;
                UpdateHiringBusTransform();
                if (hiringDriverArrival.BusWorldX >= GridWidth)
                {
                    CleanupHiringDriverArrival(true);
                }
                break;
        }
    }

    private bool HasActiveCitySideAmbientBus()
    {
        for (int i = 0; i < edgeHighwayBuses.Count; i++)
        {
            EdgeHighwayBusData bus = edgeHighwayBuses[i];
            if (bus != null && bus.RootTransform != null && bus.IsCitySideLane)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsDriverMotelArrivalInProgress()
    {
        return hiringDriverArrival != null;
    }

    private float GetHiringBusStopWorldX()
    {
        if (!locations.TryGetValue(LocationType.BusStop, out _))
        {
            return GridWidth * 0.5f;
        }

        Vector3 busStopCenter = GetLocationCenter(LocationType.BusStop);
        return Mathf.Clamp(busStopCenter.x + 0.4f, 1.2f, GridWidth - 1.2f);
    }

    private Vector3 GetHiringBusDropoffWorld()
    {
        if (!locations.TryGetValue(LocationType.BusStop, out _))
        {
            Vector3 fallback = new(GridWidth * 0.5f, 0f, 3.2f);
            fallback.y = SampleTerrainHeight(fallback.x, fallback.z);
            return fallback;
        }

        Vector3 center = GetLocationCenter(LocationType.BusStop);
        Vector3 dropoff = new(GetHiringBusStopWorldX() - 0.22f, 0f, center.z + 0.48f);
        dropoff.y = SampleTerrainHeight(dropoff.x, dropoff.z);
        return dropoff;
    }

    private void SpawnDriverFromHiringBus()
    {
        if (hiringDriverArrival == null || hiringDriverArrival.Driver == null)
        {
            return;
        }

        DriverAgent driver = hiringDriverArrival.Driver;
        if (driver.DriverObject == null)
        {
            return;
        }

        Vector3 dropoff = GetHiringBusDropoffWorld();
        driver.DriverObject.SetActive(true);
        driver.DriverObject.transform.position = dropoff;
        driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        driver.WalkAnimationTime = 0f;
        driver.WalkPhase = DriverRescuePhase.ToMotelFromBusStop;
        driver.WalkTargetWorld = driver.MotelIdlePosition;
        driver.IdleWanderPauseTimer = 0f;
        driver.IdleWanderPointIndex = -1;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        ApplyDriverPose(driver, 0f, 0f);
        BuildDriverWalkPath(driver, dropoff, driver.MotelIdlePosition);
        SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} exited the arrival bus and started walking to Motel.");
    }

    private void CleanupHiringDriverArrival(bool destroyBus)
    {
        if (hiringDriverArrival == null)
        {
            return;
        }

        if (destroyBus && hiringDriverArrival.BusRootTransform != null)
        {
            Destroy(hiringDriverArrival.BusRootTransform.gameObject);
        }

        hiringDriverArrival = null;
    }

    private DriverAgent GetDriverAgentById(int driverId)
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            if (driverAgents[i].DriverId == driverId)
            {
                return driverAgents[i];
            }
        }

        return null;
    }

    private bool CanDriverStartIdleConversation(DriverAgent driver)
    {
        return driver != null &&
               driver.DriverObject != null &&
               driver.DriverObject.activeSelf &&
               !driver.IsArrivingByBus &&
               driver.ShiftStartHour < 0 &&
               !driver.IsOnActiveShift &&
               !driver.WaitingForShiftAtParking &&
               driver.RestPhase == DriverRestPhase.None &&
               driver.WalkPhase == DriverRescuePhase.None &&
               driver.IdleConversationTimer <= 0f &&
               driver.IdleConversationCooldownTimer <= 0f &&
               GetCurrentTruckForDriver(driver) == null;
    }

    private bool TryStartIdleConversation(DriverAgent driver)
    {
        if (!CanDriverStartIdleConversation(driver))
        {
            return false;
        }

        DriverAgent bestPartner = null;
        float bestDistanceSqr = DriverIdleConversationDistance * DriverIdleConversationDistance;
        Vector3 driverPosition = driver.DriverObject.transform.position;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent candidate = driverAgents[i];
            if (candidate == driver || !CanDriverStartIdleConversation(candidate))
            {
                continue;
            }

            Vector3 delta = candidate.DriverObject.transform.position - driverPosition;
            delta.y = 0f;
            float sqrDistance = delta.sqrMagnitude;
            if (sqrDistance < 0.12f || sqrDistance > bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = sqrDistance;
            bestPartner = candidate;
        }

        if (bestPartner == null)
        {
            return false;
        }

        if (Random.value > DriverIdleConversationStartChance)
        {
            driver.IdleWanderPauseTimer = Random.Range(0.8f, 1.6f);
            return false;
        }

        float duration = Random.Range(DriverIdleConversationDurationMin, DriverIdleConversationDurationMax);
        driver.IdleConversationTimer = duration;
        driver.IdleConversationPartnerId = bestPartner.DriverId;
        bestPartner.IdleConversationTimer = duration;
        bestPartner.IdleConversationPartnerId = driver.DriverId;
        driver.IdleWanderPauseTimer = 0f;
        bestPartner.IdleWanderPauseTimer = 0f;
        driver.WalkAnimationTime = 0f;
        bestPartner.WalkAnimationTime = 0f;
        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} and {bestPartner.DriverName} started an idle conversation.");
        return true;
    }

    private bool CanDriverContinueIdleConversation(DriverAgent driver, DriverAgent partner)
    {
        if (!CanDriverStartIdleConversation(driver) || partner == null || !CanDriverStartIdleConversation(partner))
        {
            return false;
        }

        if (partner.IdleConversationPartnerId != driver.DriverId)
        {
            return false;
        }

        Vector3 delta = partner.DriverObject.transform.position - driver.DriverObject.transform.position;
        delta.y = 0f;
        float sqrDistance = delta.sqrMagnitude;
        return sqrDistance >= 0.12f &&
               sqrDistance <= DriverIdleConversationDistance * DriverIdleConversationDistance * 1.2f;
    }

    private void StopDriverIdleConversation(DriverAgent driver, bool addPause)
    {
        if (driver == null)
        {
            return;
        }

        int partnerId = driver.IdleConversationPartnerId;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        driver.IdleConversationCooldownTimer = Random.Range(DriverIdleConversationCooldownMin, DriverIdleConversationCooldownMax);
        driver.IdleWanderPointIndex++;
        if (addPause)
        {
            driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
        }

        DriverAgent partner = GetDriverAgentById(partnerId);
        if (partner != null && partner.IdleConversationPartnerId == driver.DriverId)
        {
            partner.IdleConversationTimer = 0f;
            partner.IdleConversationPartnerId = -1;
            partner.IdleConversationCooldownTimer = Random.Range(DriverIdleConversationCooldownMin, DriverIdleConversationCooldownMax);
            partner.IdleWanderPointIndex += 2;
            if (addPause)
            {
                partner.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
            }
        }
    }

    private void UpdateDriverShiftActivation(DriverAgent driver)
    {
        if (driver == null) return;
        if (driver.IsArrivingByBus) return;
        if (driver.ShiftStartHour < 0) return;
        if (driver.IsOnActiveShift) return;
        if (driver.RestPhase != DriverRestPhase.None) return;
        if (!IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour)) return;

        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (assignedTruck == null)
        {
            return;
        }

        if (assignedTruck.Driver != driver && !TryBoardDriverToAssignedTruck(driver))
        {
            return;
        }

        driver.IsOnActiveShift = true;
        driver.IsShiftSalaryPending = false;
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} shift started ({GetShiftRangeLabel(driver.ShiftStartHour)}).");
        SetTruckAutoMode(assignedTruck, true);
    }

    private void UpdateDriverShiftEnd(TruckAgent truckAgent, DriverAgent driver)
    {
        if (truckAgent == null || driver == null || !driver.IsOnActiveShift || driver.ShiftStartHour < 0)
        {
            return;
        }

        if (IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour))
        {
            return;
        }

        if (driver.NeedsShiftEndReturn)
        {
            if (truckCell == locations[LocationType.Parking].Anchor &&
                !isTruckMoving &&
                !isTruckInteracting &&
                !isDriverRescueActive &&
                currentAssignedTrip == TripType.None &&
                currentRefuelPhase == RefuelPhase.None)
            {
                PayDriverSalary(driver);
                StartDriverMotelRest(truckAgent, driver);
            }
            return;
        }

        driver.IsOnActiveShift = false;
        truckAgent.IsTruckAutoModeEnabled = false;
        driver.NeedsShiftEndReturn = true;
        driver.IsShiftSalaryPending = true;
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} shift ended. {truckAgent.DisplayName} returning to Parking for handoff.");

        if (currentAssignedTrip == TripType.None &&
            currentRefuelPhase == RefuelPhase.None &&
            !isTruckMoving &&
            !isTruckInteracting &&
            !isDriverRescueActive)
        {
            if (truckCell != locations[LocationType.Parking].Anchor)
            {
                StartMoveTo(locations[LocationType.Parking].Anchor);
            }
            else
            {
                PayDriverSalary(driver);
                StartDriverMotelRest(truckAgent, driver);
            }
        }
    }

    private void PayDriverSalary(DriverAgent driver)
    {
        if (driver == null || driver.Salary <= 0 || !driver.IsShiftSalaryPending) return;
        int treasuryBefore = money;
        driver.Money += driver.Salary;
        money = Mathf.Max(0, money - driver.Salary);
        int actualTreasuryDelta = money - treasuryBefore;
        driver.IsShiftSalaryPending = false;
        RecordMoneyMovement(
            actualTreasuryDelta,
            "Treasury",
            driver.DriverName,
            $"Salary payout ({GetShiftRangeLabel(driver.ShiftStartHour)})",
            money,
            driver.Money);
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        SessionDebugLogger.Log("PAY", $"{driver.DriverName} paid ${driver.Salary}. Personal balance: ${driver.Money}. Treasury: ${money}.");
    }

    private void EnsurePendingShiftSalaryPaid(DriverAgent driver)
    {
        if (driver == null) return;
        // Driver may leave before shift end time (energy rest) — mark salary as pending so PayDriverSalary will execute
        if (driver.IsOnActiveShift && !driver.IsShiftSalaryPending)
        {
            driver.IsShiftSalaryPending = true;
        }
        PayDriverSalary(driver);
    }

    private void RecordMoneyMovement(int treasuryDelta, string fromLabel, string toLabel, string reason, int? treasuryAfter = null, int? recipientBalanceAfter = null)
    {
        MoneyLedgerEntry entry = new()
        {
            TimeLabel = GetDayNightClockLabel(),
            TreasuryDelta = treasuryDelta,
            FromLabel = fromLabel,
            ToLabel = toLabel,
            Reason = reason,
            TreasuryAfter = treasuryAfter,
            RecipientBalanceAfter = recipientBalanceAfter
        };

        moneyLedgerEntries.Insert(0, entry);
        if (moneyLedgerEntries.Count > MaxMoneyLedgerEntries)
        {
            moneyLedgerEntries.RemoveAt(moneyLedgerEntries.Count - 1);
        }

        isEconomyScreenDirty = true;
    }

    private void UpdateIdleRecall(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        // Driver has no shift assigned — ensure truck returns to parking
        if (driver.ShiftStartHour >= 0) return;
        if (driver.RestPhase != DriverRestPhase.None) return;
        if (isDriverRescueActive) return;
        if (truckCell == locations[LocationType.Parking].Anchor)
        {
            if (GetCurrentTruckForDriver(driver) is TruckAgent currentTruck)
            {
                StartDriverMotelRest(currentTruck, driver);
            }
            return;
        }
        if (isTruckMoving) return;

        // Cancel any active orders then head home
        currentAssignedTrip = TripType.None;
        currentTripPhase = TripPhase.None;
        currentRefuelPhase = RefuelPhase.None;
        isTruckAutoModeEnabled = false;
        currentAssignedTripReward = 0;
        StartMoveTo(locations[LocationType.Parking].Anchor);
        SessionDebugLogger.Log("IDLE", $"{GetLoadedTruckDisplayName()} returning to parking — driver is idle.");
    }

    private void Awake()
    {
        SessionDebugLogger.SetGameTimeProvider(() => GetDayNightClockLabel());
        SessionDebugLogger.StartNewSession($"{nameof(GameBootstrap)} on {gameObject.scene.name}");
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;
        gameSpeedMultiplier = 0;
        lastActiveGameSpeedMultiplier = 1;
        SessionDebugLogger.Log("BOOT", "Initializing runtime bootstrap.");
        BuildPrototypeScene();
        SessionDebugLogger.Log("BOOT", $"Scene bootstrap complete. Locations={locations.Count}, Roads={roadCells.Count}, Trucks={truckAgents.Count}.");
    }

    private void OnApplicationQuit()
    {
        SessionDebugLogger.EndSession("Application quit");
    }

    private void OnDestroy()
    {
        SessionDebugLogger.EndSession("Play mode object destroyed");
    }

    private void Update()
    {
        UpdateMainMenuHud();
        if (isMainMenuOpen)
        {
            return;
        }

        HandleHotkeys();
        HandleCameraInput();
        HandleRoadRemovalInput();
        HandleRoadPlacementInput();
        UpdateBuildHoverHighlight();
        ProduceForestWood();
        UpdateSawmillProcessing();
        UpdateDayNightCycle();
        UpdateSelectedLocationLabel();
        UpdateForestTreeWobbles();
        UpdateMiscTreeSways();
        UpdateHiringDriverArrival();
        UpdateEdgeHighwayBuses();
        UpdateDistantClouds();
        UpdateForestWorkers();
        for (int i = 0; i < truckAgents.Count; i++)
        {
            TruckAgent ta = truckAgents[i];
            DriverAgent da = ta.Driver;
            LoadTruckState(ta);
            UpdateTruckMovement();
            UpdateTruckInteraction();
            UpdateDriverEnergy(da);
            UpdateAssignedTrip(da);   // award trip money BEFORE salary deduction
            UpdateRefuelOrder(da);
            UpdateDriverShiftEnd(ta, da);
            UpdateDriverShiftActivation(da);
            UpdateIdleRecall(da);
            UpdateDriverWalk(da);
            UpdateDriverRest(da);
            UpdateDriverVisualAnimation(da);
            UpdateTruckAutoMode();
            UpdateAudio();

            if (!isTruckMoving &&
                !isTruckInteracting &&
                !isDriverRescueActive &&
                (da == null || da.RestPhase == DriverRestPhase.None) &&
                truckCell == locations[LocationType.Parking].Anchor)
            {
                Vector3 parkedPosition = GetParkingSlotWorldPosition(ta.ParkingSlotIndex);
                truckObject.transform.position = parkedPosition;
                truckTargetWorld = parkedPosition;
                truckSegmentStartWorld = parkedPosition;
                truckObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            }

            SaveTruckState(ta);
        }

        foreach (DriverAgent driver in driverAgents)
        {
            if (GetCurrentTruckForDriver(driver) != null)
            {
                continue;
            }

            UpdateDriverShiftPreparation(driver);
            UpdateDriverShiftActivation(driver);
            UpdateDriverRest(driver);
            UpdateDriverIdleWander(driver);
            UpdateDriverWalk(driver);
            UpdateDriverVisualAnimation(driver);
            UpdateDriverFlashlight(driver, currentStylizedDaylight);
        }

        UpdateMoneyPopup();
        UpdateFleetScreenUi();
        UpdateDriversScreenUi();
        UpdateShiftsScreenUi();
        UpdateResourcesScreenUi();
        UpdateEconomyScreenUi();
        UpdateBuildScreenUi();
        UpdateTruckQuickHud();
        UpdateDriverQuickHud();
        UpdateBuildingQuickHud();
    }

    private void OnGUI()
    {
        if (isMainMenuOpen)
        {
            return;
        }

        DrawMoneyHud();
        DrawTimeHud();
        DrawSpeedHud();
        DrawPauseOverlay();
        DrawMenuBar();

        if (isFleetPanelOpen) DrawFleetPanel();
        // Shifts panel is now Canvas-based (ShiftsScreenCanvas)
        // Drivers panel is now Canvas-based (DriversScreenCanvas)
        // Resources panel is now Canvas-based (ResourcesScreenCanvas)
        // Economy panel is now Canvas-based (EconomyScreenCanvas)
        // Build panel is now Canvas-based (BuildScreenCanvas)

    }

    private void BuildPrototypeScene()
    {
        worldRoot = new GameObject("PrototypeWorld").transform;
        roadsRoot = new GameObject("Roads").transform;
        roadsRoot.SetParent(worldRoot, false);
        lanternsRoot = new GameObject("RoadLanterns").transform;
        lanternsRoot.SetParent(worldRoot, false);
        miscRoot = new GameObject("Misc").transform;
        miscRoot.SetParent(worldRoot, false);

        SetupCamera();
        SetupLighting();
        SetupDioramaPostProcessing();
        SetupSurfaceMaterials();
        SetupLocations();
        GenerateInitialRoadNetwork();
        GenerateTerrainHeights();
        ApplyTerrainHeightsToWorld();
        SetupGround();
        SetupGrid();
        SetupEdgeHighway();
        SetupEdgeHighwayBuses();
        SetupDistantClouds();
        RebuildRoadLanterns();
        PopulateMiscTrees();
        SetupBuildHoverHighlight();
        SetupForestWorkers();
        SetupSelectionVisuals();
        SetupTruck();
        money = StartingTreasury;
        SetupCargoTransferVisual();
        SetupAudio();
        SetupFleetScreenUi();
        SetupDriversScreenUi();
        SetupShiftsScreenUi();
        SetupResourcesScreenUi();
        SetupEconomyScreenUi();
        SetupBuildScreenUi();
        SetupTruckQuickHud();
        SetupDriverQuickHud();
        SetupBuildingQuickHud();
        SetupMainMenuHud();
    }

    private void SetupCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }

        mainCamera.transform.position = DioramaCameraOffset;
        mainCamera.transform.rotation = GetDioramaCameraRotation();
        mainCamera.fieldOfView = 30f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 120f;
        mainCamera.backgroundColor = new Color(0.82f, 0.9f, 0.97f);
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        cameraFocusPoint = new Vector3(GridWidth * 0.5f, 0f, GridHeight * 0.5f);
        cameraOffset = DioramaCameraOffset;
        cameraTargetOffset = DioramaCameraOffset;
    }

    private void SetupLighting()
    {
        Light keyLight = FindFirstObjectByType<Light>();
        if (keyLight == null)
        {
            keyLight = new GameObject("Directional Light").AddComponent<Light>();
        }

        mainDirectionalLight = keyLight;
        keyLight.type = LightType.Directional;
        keyLight.transform.rotation = Quaternion.Euler(48f, -34f, 0f);
        keyLight.color = new Color(1f, 0.94f, 0.78f);
        keyLight.intensity = 1.22f;
        keyLight.shadows = LightShadows.Soft;
        keyLight.shadowStrength = 0.9f;
        keyLight.shadowBias = 0.04f;
        keyLight.shadowNormalBias = 0.32f;
        keyLight.shadowNearPlane = 0.2f;
        keyLight.shadowResolution = UnityEngine.Rendering.LightShadowResolution.VeryHigh;

        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light lightComponent in allLights)
        {
            lightComponent.enabled = lightComponent == keyLight;
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.84f, 0.84f, 0.8f);
    }

    private void UpdateDayNightCycle()
    {
        if (mainDirectionalLight == null || mainCamera == null)
        {
            return;
        }

        dayNightCycleTimer = Mathf.Repeat(dayNightCycleTimer + Time.deltaTime, DayNightCycleDuration);
        float normalizedTime = dayNightCycleTimer / DayNightCycleDuration;
        float dayHour = normalizedTime * 24f;
        float sunriseBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.8f, 7.2f, dayHour));
        float sunsetBlend = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(18f, 20.6f, dayHour));
        float daylight = Mathf.Clamp01(sunriseBlend * sunsetBlend);
        float stylizedDaylight = Mathf.SmoothStep(0.04f, 1f, daylight);
        currentStylizedDaylight = stylizedDaylight;

        float sunTravel = Mathf.Clamp01(Mathf.InverseLerp(4.5f, 20.5f, dayHour));
        float sunArc = Mathf.Sin(sunTravel * Mathf.PI);
        float lowSun = 1f - Mathf.SmoothStep(0.18f, 0.72f, sunArc);
        float sunPitch = Mathf.Lerp(14f, 68f, sunArc);
        float sunYaw = Mathf.Lerp(-62f, -8f, sunTravel);
        mainDirectionalLight.transform.rotation = Quaternion.Euler(sunPitch, sunYaw, 0f);
        mainDirectionalLight.intensity = Mathf.Lerp(0.18f, 1.38f, stylizedDaylight) * Mathf.Lerp(0.82f, 1f, sunArc);
        mainDirectionalLight.color = Color.Lerp(
            new Color(0.66f, 0.54f, 0.58f),
            Color.Lerp(
                new Color(1f, 0.72f, 0.46f),
                new Color(1f, 0.97f, 0.84f),
                Mathf.SmoothStep(0f, 1f, sunArc)),
            stylizedDaylight);
        mainDirectionalLight.shadowStrength = Mathf.Lerp(0.76f, 0.95f, stylizedDaylight);
        mainDirectionalLight.shadowBias = Mathf.Lerp(0.06f, 0.03f, sunArc);
        mainDirectionalLight.shadowNormalBias = Mathf.Lerp(0.48f, 0.24f, sunArc);

        RenderSettings.ambientLight = Color.Lerp(
            Color.Lerp(new Color(0.16f, 0.12f, 0.14f), new Color(0.35f, 0.24f, 0.18f), lowSun * daylight),
            new Color(0.93f, 0.88f, 0.76f),
            stylizedDaylight);

        Color backgroundColor = Color.Lerp(
            Color.Lerp(new Color(0.08f, 0.06f, 0.09f), new Color(0.52f, 0.3f, 0.24f), lowSun * daylight),
            new Color(0.56f, 0.74f, 0.94f),
            stylizedDaylight);
        mainCamera.backgroundColor = backgroundColor;

        float zoomT = Mathf.InverseLerp(CameraMinHeight, CameraMaxHeight, cameraOffset.y);
        float fogZoom = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 0.96f, zoomT));
        RenderSettings.fog = fogZoom > 0.001f;
        RenderSettings.fogMode = FogMode.Linear;
        float fogStrength = fogZoom * 0.26f;
        RenderSettings.fogColor = Color.Lerp(backgroundColor, Color.Lerp(backgroundColor, Color.white, 0.12f), fogStrength);
        RenderSettings.fogStartDistance = Mathf.Lerp(110f, 62f, fogZoom);
        RenderSettings.fogEndDistance = Mathf.Lerp(150f, 92f, fogZoom);

        for (int i = 0; i < truckAgents.Count; i++)
        {
            LoadTruckState(truckAgents[i]);
            UpdateTruckHeadlights(stylizedDaylight, truckAgents[i].Driver);
            SaveTruckState(truckAgents[i]);
        }
    }

    private void SetupGround()
    {
        groundRoot = new GameObject("Ground").transform;
        groundRoot.SetParent(worldRoot, false);

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                float terrainHeight = terrainHeights[x, y];
                float thickness = 0.28f + terrainHeight;

                GameObject groundTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                groundTile.name = $"Ground_{x}_{y}";
                groundTile.transform.SetParent(groundRoot, false);
                groundTile.transform.position = new Vector3(x + 0.5f, terrainHeight - thickness * 0.5f - 0.02f, y + 0.5f);
                groundTile.transform.localScale = new Vector3(1.02f, thickness, 1.02f);
                ApplyStylizedGroundMaterial(groundTile, x, y);
                ConfigureStaticVisual(groundTile);
            }
        }

        CreateDioramaBase();
    }

    private void SetupDioramaPostProcessing()
    {
        UniversalAdditionalCameraData cameraData = mainCamera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.Medium;

        RenderSettings.fog = false;
        RenderSettings.fogMode = FogMode.Linear;

        GameObject volumeObject = new("DioramaVolume");
        volumeObject.transform.SetParent(worldRoot, false);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = profile;

        ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.postExposure.Override(0.08f);
        colorAdjustments.contrast.Override(10f);
        colorAdjustments.saturation.Override(12f);

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.98f);
        bloom.intensity.Override(0.045f);
        bloom.scatter.Override(0.44f);
        bloom.highQualityFiltering.Override(false);

        DepthOfField depthOfField = profile.Add<DepthOfField>(true);
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(36f);
        depthOfField.gaussianEnd.Override(78f);
        depthOfField.gaussianMaxRadius.Override(0.018f);
        depthOfField.highQualitySampling.Override(false);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.045f);
        vignette.smoothness.Override(0.42f);
        vignette.rounded.Override(false);
    }

    private void SetupSurfaceMaterials()
    {
        groundSurfaceTexture = CreateStylizedGroundTexture(128);
        grassSurfaceTexture = CreateStylizedGrassTexture(128);
        groundSurfaceMaterial = CreateSurfaceMaterial(groundSurfaceTexture, new Color(0.96f, 0.93f, 0.86f), 0.09f);
        grassSurfaceMaterial = CreateSurfaceMaterial(grassSurfaceTexture, new Color(0.74f, 0.82f, 0.72f), 0.07f);
    }

    private Material CreateSurfaceMaterial(Texture2D texture, Color tint, float smoothness)
    {
        Material material = new(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = tint;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        material.mainTexture = texture;
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        return material;
    }

    private Texture2D CreateStylizedGroundTexture(int size)
    {
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        texture.name = "StylizedGroundTexture";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color baseColor = new(0.79f, 0.73f, 0.61f);
        Color warmPatch = new(0.87f, 0.8f, 0.66f);
        Color coolPatch = new(0.69f, 0.64f, 0.54f);
        Vector2[] blotchCenters =
        {
            new(0.18f, 0.22f),
            new(0.72f, 0.28f),
            new(0.36f, 0.68f),
            new(0.82f, 0.78f)
        };

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                float largeNoise = Mathf.PerlinNoise(u * 2.4f + 0.11f, v * 2.4f + 0.37f);
                float detailNoise = Mathf.PerlinNoise(u * 7.2f + 1.2f, v * 7.2f + 2.4f);
                float diagonal = Mathf.Sin((u + v) * 8.5f) * 0.015f;

                float warmMask = 0f;
                for (int i = 0; i < blotchCenters.Length; i++)
                {
                    float dist = Vector2.Distance(new Vector2(u, v), blotchCenters[i]);
                    warmMask += Mathf.Clamp01(1f - dist * 3.2f);
                }
                warmMask = Mathf.Clamp01(warmMask * 0.42f);

                Color color = Color.Lerp(coolPatch, baseColor, largeNoise);
                color = Color.Lerp(color, warmPatch, warmMask);
                color *= 0.96f + detailNoise * 0.08f + diagonal;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

    private Texture2D CreateStylizedGrassTexture(int size)
    {
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        texture.name = "StylizedGrassTexture";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color darkGreen = new(0.12f, 0.3f, 0.11f);
        Color baseGreen = new(0.22f, 0.46f, 0.2f);
        Color lightGreen = new(0.38f, 0.6f, 0.29f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                float broadNoise = Mathf.PerlinNoise(u * 2.5f + 0.5f, v * 2.5f + 0.9f);
                float fineNoise = Mathf.PerlinNoise(u * 9.2f + 2.3f, v * 9.2f + 3.7f);
                float bladeNoise = Mathf.PerlinNoise(u * 17.5f + 4.1f, v * 15.2f + 5.6f);
                float stripe = Mathf.Sin((u * 0.95f + v * 1.2f) * 20f) * 0.055f;

                Color color = Color.Lerp(darkGreen, baseGreen, broadNoise);
                color = Color.Lerp(color, lightGreen, Mathf.Clamp01(fineNoise * 0.78f));
                color = Color.Lerp(color, lightGreen * 1.05f, Mathf.Clamp01((bladeNoise - 0.52f) * 1.8f));
                color *= 0.96f + stripe;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

    private void ApplyStylizedGroundMaterial(GameObject target, int x, int y)
    {
        if (target == null || groundSurfaceMaterial == null)
        {
            ApplyColor(target, new Color(0.72f, 0.67f, 0.55f));
            return;
        }

        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        bool useGrassPatch = IsGrassGroundCell(x, y);
        Material material = new(useGrassPatch ? grassSurfaceMaterial : groundSurfaceMaterial);
        float tintNoise = Mathf.PerlinNoise((x + 1) * 0.37f, (y + 1) * 0.41f);
        Color tint = useGrassPatch
            ? Color.Lerp(new Color(0.74f, 0.82f, 0.7f), new Color(0.84f, 0.9f, 0.78f), tintNoise)
            : Color.Lerp(new Color(0.95f, 0.91f, 0.84f), new Color(1.01f, 0.98f, 0.92f), tintNoise);
        material.color = tint;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        material.mainTextureScale = useGrassPatch ? new Vector2(0.54f, 0.54f) : new Vector2(0.62f, 0.62f);
        material.mainTextureOffset = new Vector2((x % 5) * 0.13f, (y % 5) * 0.11f);
        renderer.material = material;
    }

    private bool IsGrassGroundCell(int x, int y)
    {
        if (grassSurfaceMaterial == null)
        {
            return false;
        }

        float grassPatchNoise = Mathf.PerlinNoise((x + 1) * 0.18f + 4.2f, (y + 1) * 0.2f + 7.4f);
        return grassPatchNoise > 0.5f;
    }

    private void ApplyStylizedGrassMaterial(GameObject target, float seedX, float seedY)
    {
        if (target == null || grassSurfaceMaterial == null)
        {
            ApplyColor(target, new Color(0.24f, 0.34f, 0.16f));
            return;
        }

        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        Material material = new(grassSurfaceMaterial);
        float tintNoise = Mathf.PerlinNoise(seedX * 0.29f + 1.1f, seedY * 0.33f + 2.4f);
        Color tint = Color.Lerp(new Color(0.72f, 0.8f, 0.68f), new Color(0.82f, 0.88f, 0.74f), tintNoise);
        material.color = tint;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        material.mainTextureScale = new Vector2(0.42f, 0.42f);
        material.mainTextureOffset = new Vector2(seedX * 0.03f, seedY * 0.03f);
        renderer.material = material;
    }

    private void CreateDioramaBase()
    {
        Vector3 center = new(GridWidth * 0.5f, -0.38f, GridHeight * 0.5f);

        GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plinth.name = "DioramaPlinth";
        plinth.transform.SetParent(worldRoot, false);
        plinth.transform.position = center;
        plinth.transform.localScale = new Vector3(GridWidth + 2.4f, 0.82f, GridHeight + 2.4f);
        ApplyColor(plinth, new Color(0.73f, 0.66f, 0.56f));

        GameObject baseLip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseLip.name = "DioramaLip";
        baseLip.transform.SetParent(worldRoot, false);
        baseLip.transform.position = new Vector3(center.x, 0.08f, center.z);
        baseLip.transform.localScale = new Vector3(GridWidth + 0.8f, 0.18f, GridHeight + 0.8f);
        ApplyColor(baseLip, new Color(0.87f, 0.81f, 0.7f));

        CreateDioramaBoundary(new Vector3(GridWidth * 0.5f, 0.22f, -0.3f), new Vector3(GridWidth + 0.9f, 0.32f, 0.24f));
        CreateDioramaBoundary(new Vector3(GridWidth * 0.5f, 0.22f, GridHeight + 0.3f), new Vector3(GridWidth + 0.9f, 0.32f, 0.24f));
        CreateDioramaBoundary(new Vector3(-0.3f, 0.22f, GridHeight * 0.5f), new Vector3(0.24f, 0.32f, GridHeight + 0.9f));
        CreateDioramaBoundary(new Vector3(GridWidth + 0.3f, 0.22f, GridHeight * 0.5f), new Vector3(0.24f, 0.32f, GridHeight + 0.9f));
    }

    private void CreateDioramaBoundary(Vector3 position, Vector3 scale)
    {
        GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundary.transform.SetParent(worldRoot, false);
        boundary.transform.position = position;
        boundary.transform.localScale = scale;
        ApplyColor(boundary, new Color(0.9f, 0.85f, 0.75f));
    }

    private void SetupGrid()
    {
        GameObject gridRoot = new("GridLines");
        gridRoot.transform.SetParent(worldRoot, false);

        Material lineMaterial = new(Shader.Find("Sprites/Default"))
        {
            color = new Color(0f, 0f, 0f, 0.18f)
        };

        for (int x = 0; x <= GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                float edgeHeight = GetVerticalEdgeHeight(x, y) + 0.025f;
                CreateGridLine(gridRoot.transform, lineMaterial, new Vector3(x, edgeHeight, y), new Vector3(x, edgeHeight, y + 1f));
            }
        }

        for (int y = 0; y <= GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                float edgeHeight = GetHorizontalEdgeHeight(x, y) + 0.025f;
                CreateGridLine(gridRoot.transform, lineMaterial, new Vector3(x, edgeHeight, y), new Vector3(x + 1f, edgeHeight, y));
            }
        }
    }

    private void SetupEdgeHighway()
    {
        if (worldRoot == null)
        {
            return;
        }

        edgeHighwayCells.Clear();
        Transform highwayRoot = new GameObject("EdgeHighway").transform;
        highwayRoot.SetParent(worldRoot, false);

        int bottomLaneY = 0;
        int upperLaneY = 1;
        for (int x = 0; x < GridWidth; x++)
        {
            CreateEdgeHighwayTile(highwayRoot, new Vector2Int(x, bottomLaneY), horizontal: true, vertical: false);
            CreateEdgeHighwayTile(highwayRoot, new Vector2Int(x, upperLaneY), horizontal: true, vertical: false);
        }

        CreateEdgeHighwayCenterLine(highwayRoot, bottomLaneY, upperLaneY);
    }

    private void SetupEdgeHighwayBuses()
    {
        edgeHighwayBuses.Clear();
        hiringDriverArrival = null;
        if (edgeHighwayBusRoot != null)
        {
            Destroy(edgeHighwayBusRoot.gameObject);
        }

        edgeHighwayBusRoot = new GameObject("EdgeHighwayBuses").transform;
        edgeHighwayBusRoot.SetParent(worldRoot, false);
        edgeHighwayBusSpawnTimerCitySide = Random.Range(6f, 16f);
        edgeHighwayBusSpawnTimerOuterSide = Random.Range(12f, 24f);
    }

    private void CreateEdgeHighwayTile(Transform parent, Vector2Int cell, bool horizontal, bool vertical)
    {
        if (!IsInsideGrid(cell) || edgeHighwayCells.Contains(cell))
        {
            return;
        }

        edgeHighwayCells.Add(cell);

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = $"EdgeHighway_{cell.x}_{cell.y}";
        road.transform.SetParent(parent, false);
        road.transform.position = GetCellCenter(cell) + new Vector3(0f, RoadHeight - 0.015f, 0f);
        road.transform.localScale = new Vector3(horizontal ? 1.12f : 0.94f, 0.16f, vertical ? 1.12f : 0.94f);
        ApplyColor(road, new Color(0.16f, 0.17f, 0.19f));
        ConfigureStaticVisual(road);

        GameObject roadTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadTop.name = "EdgeHighwayTop";
        roadTop.transform.SetParent(road.transform, false);
        roadTop.transform.localPosition = new Vector3(0f, 0.32f, 0f);
        roadTop.transform.localScale = new Vector3(horizontal ? 0.94f : 0.74f, 0.16f, vertical ? 0.94f : 0.74f);
        ApplyColor(roadTop, new Color(0.56f, 0.58f, 0.6f));
        ConfigureStaticVisual(roadTop);

        if (horizontal)
        {
            CreateEdgeHighwayLaneStripe(road.transform, new Vector3(0f, 0.46f, 0.23f), new Vector3(0.84f, 0.06f, 0.08f));
            CreateEdgeHighwayLaneStripe(road.transform, new Vector3(0f, 0.46f, -0.23f), new Vector3(0.84f, 0.06f, 0.08f));
        }

        if (vertical)
        {
            CreateEdgeHighwayLaneStripe(road.transform, new Vector3(0.23f, 0.46f, 0f), new Vector3(0.08f, 0.06f, 0.84f));
            CreateEdgeHighwayLaneStripe(road.transform, new Vector3(-0.23f, 0.46f, 0f), new Vector3(0.08f, 0.06f, 0.84f));
        }
    }

    private void CreateEdgeHighwayLaneStripe(Transform parent, Vector3 localPosition, Vector3 localScale)
    {
        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.name = "EdgeHighwayStripe";
        stripe.transform.SetParent(parent, false);
        stripe.transform.localPosition = localPosition;
        stripe.transform.localScale = localScale;
        ApplyColor(stripe, new Color(0.88f, 0.83f, 0.68f));
        ConfigureStaticVisual(stripe);
    }

    private void CreateEdgeHighwayCenterLine(Transform parent, int lowerLaneY, int upperLaneY)
    {
        float centerZ = (lowerLaneY + upperLaneY + 1f) * 0.5f;
        for (int x = 0; x < GridWidth; x += 2)
        {
            GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dash.name = $"EdgeHighwayCenterDash_{x}";
            dash.transform.SetParent(parent, false);
            dash.transform.position = new Vector3(x + 0.5f, GetTerrainHeight(new Vector2Int(x, lowerLaneY)) + RoadHeight + 0.06f, centerZ);
            dash.transform.localScale = new Vector3(0.68f, 0.04f, 0.08f);
            ApplyColor(dash, new Color(0.92f, 0.92f, 0.9f));
            ConfigureStaticVisual(dash);
        }
    }

    private void UpdateEdgeHighwayBuses()
    {
        if (edgeHighwayBusRoot == null)
        {
            return;
        }

        float dt = Time.deltaTime * gameSpeedMultiplier;
        edgeHighwayBusSpawnTimerCitySide -= dt;
        edgeHighwayBusSpawnTimerOuterSide -= dt;

        if (edgeHighwayBusSpawnTimerCitySide <= 0f)
        {
            if (!IsDriverMotelArrivalInProgress())
            {
                TrySpawnEdgeHighwayBus(isCitySideLane: true);
            }
            edgeHighwayBusSpawnTimerCitySide = Random.Range(EdgeHighwayBusSpawnIntervalMin, EdgeHighwayBusSpawnIntervalMax);
            edgeHighwayBusSpawnTimerOuterSide += Random.Range(1.5f, 4.5f);
        }

        if (edgeHighwayBusSpawnTimerOuterSide <= 0f)
        {
            TrySpawnEdgeHighwayBus(isCitySideLane: false);
            edgeHighwayBusSpawnTimerOuterSide = Random.Range(EdgeHighwayBusSpawnIntervalMin, EdgeHighwayBusSpawnIntervalMax);
            edgeHighwayBusSpawnTimerCitySide += Random.Range(1.5f, 4.5f);
        }

        for (int i = edgeHighwayBuses.Count - 1; i >= 0; i--)
        {
            EdgeHighwayBusData bus = edgeHighwayBuses[i];
            if (bus.RootTransform == null)
            {
                edgeHighwayBuses.RemoveAt(i);
                continue;
            }

            bus.WorldX += bus.Speed * dt * bus.TravelDirection;
            if (!bus.HasEnteredRoadStrip && bus.WorldX > 0f && bus.WorldX < GridWidth)
            {
                bus.HasEnteredRoadStrip = true;
            }

            if (bus.HasEnteredRoadStrip &&
                ((bus.TravelDirection > 0f && bus.WorldX >= GridWidth) ||
                 (bus.TravelDirection < 0f && bus.WorldX <= 0f)))
            {
                Destroy(bus.RootTransform.gameObject);
                edgeHighwayBuses.RemoveAt(i);
                continue;
            }

            float laneZ = GetEdgeHighwayBusLaneWorldZ(bus.IsCitySideLane);
            float bob = Mathf.Sin(Time.time * 3.2f + bus.BobPhase) * 0.015f;
            float y = SampleTerrainHeight(bus.WorldX, laneZ) + RoadHeight + EdgeHighwayBusLift + bob;
            bus.RootTransform.position = new Vector3(bus.WorldX, y, laneZ);
            bus.RootTransform.rotation = bus.TravelDirection > 0f
                ? Quaternion.identity
                : Quaternion.Euler(0f, 180f, 0f);

            float darkness = 1f - currentStylizedDaylight;
            bool headlightsOn = darkness > 0.55f;
            float headlightIntensity = headlightsOn ? Mathf.Lerp(0.4f, 1.75f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
            Color lampColor = Color.Lerp(
                new Color(0.34f, 0.3f, 0.22f),
                new Color(1f, 0.94f, 0.78f),
                Mathf.Clamp01(headlightIntensity / 1.75f));

            if (bus.HeadlightLeft != null)
            {
                bus.HeadlightLeft.enabled = headlightsOn;
                bus.HeadlightLeft.intensity = headlightIntensity;
            }

            if (bus.HeadlightRight != null)
            {
                bus.HeadlightRight.enabled = headlightsOn;
                bus.HeadlightRight.intensity = headlightIntensity;
            }

            if (bus.HeadlightLeftRenderer != null)
            {
                bus.HeadlightLeftRenderer.material.color = lampColor;
            }

            if (bus.HeadlightRightRenderer != null)
            {
                bus.HeadlightRightRenderer.material.color = lampColor;
            }

            if (!bus.HasPlayedPassbyAudio)
            {
                Vector3 audioDelta = bus.RootTransform.position - cameraFocusPoint;
                audioDelta.y = 0f;
                if (audioDelta.sqrMagnitude <= EdgeHighwayBusPassbyDistance * EdgeHighwayBusPassbyDistance)
                {
                    PlayAmbientFx(edgeHighwayBusPassbyClip, bus.RootTransform.position, 0.34f);
                    bus.HasPlayedPassbyAudio = true;
                }
            }
        }
    }

    private void TrySpawnEdgeHighwayBus(bool isCitySideLane)
    {
        float travelDirection = isCitySideLane ? 1f : -1f;
        float spawnX = travelDirection > 0f ? -1.6f : GridWidth + 1.6f;
        for (int i = 0; i < edgeHighwayBuses.Count; i++)
        {
            EdgeHighwayBusData existing = edgeHighwayBuses[i];
            if (existing == null || existing.RootTransform == null || existing.IsCitySideLane != isCitySideLane)
            {
                continue;
            }

            if (Mathf.Abs(existing.WorldX - spawnX) < EdgeHighwayBusSpawnSpacing)
            {
                return;
            }
        }

        CreateEdgeHighwayBus(travelDirection, isCitySideLane);
    }

    private void CreateEdgeHighwayBus(float travelDirection, bool isCitySideLane)
    {
        if (edgeHighwayBusRoot == null)
        {
            return;
        }

        float spawnX = travelDirection > 0f ? -1.6f : GridWidth + 1.6f;
        float laneZ = GetEdgeHighwayBusLaneWorldZ(isCitySideLane);
        GameObject busRoot = new($"EdgeHighwayBus_{edgeHighwayBuses.Count + 1}");
        busRoot.transform.SetParent(edgeHighwayBusRoot, false);

        Color bodyColor = Random.value < 0.5f
            ? new Color(0.9f, 0.26f, 0.22f)
            : new Color(0.24f, 0.5f, 0.86f);
        Color roofColor = new Color(0.94f, 0.92f, 0.84f);
        Color windowColor = new Color(0.72f, 0.88f, 0.95f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(busRoot.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.26f, 0f);
        body.transform.localScale = new Vector3(1.24f, 0.42f, 0.44f);
        ApplyColor(body, bodyColor);
        ConfigureShadowVisual(body);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(busRoot.transform, false);
        roof.transform.localPosition = new Vector3(-0.02f, 0.56f, 0f);
        roof.transform.localScale = new Vector3(1.02f, 0.12f, 0.4f);
        ApplyColor(roof, roofColor);
        ConfigureShadowVisual(roof);

        GameObject windowBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windowBand.transform.SetParent(busRoot.transform, false);
        windowBand.transform.localPosition = new Vector3(-0.02f, 0.38f, 0f);
        windowBand.transform.localScale = new Vector3(0.94f, 0.18f, 0.46f);
        ApplyColor(windowBand, windowColor);
        ConfigureShadowVisual(windowBand);

        GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windshield.transform.SetParent(busRoot.transform, false);
        windshield.transform.localPosition = new Vector3(0.56f, 0.41f, 0f);
        windshield.transform.localScale = new Vector3(0.12f, 0.2f, 0.38f);
        ApplyColor(windshield, new Color(0.66f, 0.86f, 0.94f));
        ConfigureShadowVisual(windshield);

        GameObject rearWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rearWindow.transform.SetParent(busRoot.transform, false);
        rearWindow.transform.localPosition = new Vector3(-0.56f, 0.39f, 0f);
        rearWindow.transform.localScale = new Vector3(0.08f, 0.17f, 0.34f);
        ApplyColor(rearWindow, new Color(0.66f, 0.84f, 0.92f));
        ConfigureShadowVisual(rearWindow);

        GameObject headlightLeftVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlightLeftVisual.transform.SetParent(busRoot.transform, false);
        headlightLeftVisual.transform.localPosition = new Vector3(0.61f, 0.26f, -0.14f);
        headlightLeftVisual.transform.localScale = new Vector3(0.04f, 0.06f, 0.08f);
        ApplyColor(headlightLeftVisual, new Color(0.34f, 0.3f, 0.22f));
        ConfigureShadowVisual(headlightLeftVisual);

        GameObject headlightRightVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlightRightVisual.transform.SetParent(busRoot.transform, false);
        headlightRightVisual.transform.localPosition = new Vector3(0.61f, 0.26f, 0.14f);
        headlightRightVisual.transform.localScale = new Vector3(0.04f, 0.06f, 0.08f);
        ApplyColor(headlightRightVisual, new Color(0.34f, 0.3f, 0.22f));
        ConfigureShadowVisual(headlightRightVisual);

        GameObject sideStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sideStripe.transform.SetParent(busRoot.transform, false);
        sideStripe.transform.localPosition = new Vector3(0f, 0.23f, 0f);
        sideStripe.transform.localScale = new Vector3(1.08f, 0.06f, 0.47f);
        ApplyColor(sideStripe, new Color(0.98f, 0.86f, 0.2f));
        ConfigureShadowVisual(sideStripe);

        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.transform.SetParent(busRoot.transform, false);
        door.transform.localPosition = new Vector3(0.18f, 0.23f, -0.22f);
        door.transform.localScale = new Vector3(0.24f, 0.32f, 0.05f);
        ApplyColor(door, new Color(0.92f, 0.94f, 0.98f));
        ConfigureShadowVisual(door);

        float[] wheelX = { -0.38f, 0.38f };
        float[] wheelZ = { -0.18f, 0.18f };
        foreach (float wx in wheelX)
        {
            foreach (float wz in wheelZ)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.transform.SetParent(busRoot.transform, false);
                wheel.transform.localPosition = new Vector3(wx, 0.1f, wz);
                wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                wheel.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);
                ApplyColor(wheel, new Color(0.12f, 0.12f, 0.12f));
                ConfigureShadowVisual(wheel);
            }
        }

        GameObject routePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        routePlate.transform.SetParent(busRoot.transform, false);
        routePlate.transform.localPosition = new Vector3(0.48f, 0.53f, 0f);
        routePlate.transform.localScale = new Vector3(0.18f, 0.08f, 0.3f);
        ApplyColor(routePlate, new Color(0.98f, 0.84f, 0.14f));
        ConfigureShadowVisual(routePlate);

        GameObject leftLightObject = new("BusHeadlightLeft");
        leftLightObject.transform.SetParent(busRoot.transform, false);
        leftLightObject.transform.localPosition = new Vector3(0.64f, 0.28f, -0.14f);
        leftLightObject.transform.localRotation = Quaternion.Euler(8f, 90f, 0f);
        Light leftLight = leftLightObject.AddComponent<Light>();
        leftLight.type = LightType.Spot;
        leftLight.color = new Color(1f, 0.9f, 0.72f);
        leftLight.range = 3.6f;
        leftLight.spotAngle = 42f;
        leftLight.innerSpotAngle = 22f;
        leftLight.intensity = 0f;
        leftLight.shadows = LightShadows.None;
        leftLight.enabled = false;

        GameObject rightLightObject = new("BusHeadlightRight");
        rightLightObject.transform.SetParent(busRoot.transform, false);
        rightLightObject.transform.localPosition = new Vector3(0.64f, 0.28f, 0.14f);
        rightLightObject.transform.localRotation = Quaternion.Euler(8f, 90f, 0f);
        Light rightLight = rightLightObject.AddComponent<Light>();
        rightLight.type = LightType.Spot;
        rightLight.color = new Color(1f, 0.9f, 0.72f);
        rightLight.range = 3.6f;
        rightLight.spotAngle = 42f;
        rightLight.innerSpotAngle = 22f;
        rightLight.intensity = 0f;
        rightLight.shadows = LightShadows.None;
        rightLight.enabled = false;

        float y = SampleTerrainHeight(spawnX, laneZ) + RoadHeight + EdgeHighwayBusLift;
        busRoot.transform.position = new Vector3(spawnX, y, laneZ);
        busRoot.transform.rotation = Quaternion.LookRotation(travelDirection > 0f ? Vector3.right : Vector3.left, Vector3.up);

        edgeHighwayBuses.Add(new EdgeHighwayBusData
        {
            RootTransform = busRoot.transform,
            WorldX = spawnX,
            TravelDirection = travelDirection,
            IsCitySideLane = isCitySideLane,
            Speed = EdgeHighwayBusSpeed * Random.Range(0.92f, 1.08f),
            BobPhase = Random.Range(0f, 10f),
            BodyColor = bodyColor,
            HasPlayedPassbyAudio = false,
            HasEnteredRoadStrip = false,
            HeadlightLeftRenderer = headlightLeftVisual.GetComponent<Renderer>(),
            HeadlightRightRenderer = headlightRightVisual.GetComponent<Renderer>(),
            HeadlightLeft = leftLight,
            HeadlightRight = rightLight
        });
    }

    private void CreateHiringArrivalBusVisual()
    {
        if (hiringDriverArrival == null || edgeHighwayBusRoot == null)
        {
            return;
        }

        float spawnX = -1.6f;
        float laneZ = GetEdgeHighwayBusLaneWorldZ(isCitySideLane: true);
        GameObject busRoot = new($"HiringBus_{hiringDriverArrival.Driver?.DriverId ?? 0}");
        busRoot.transform.SetParent(edgeHighwayBusRoot, false);

        Color bodyColor = new(0.28f, 0.58f, 0.9f);
        Color roofColor = new(0.94f, 0.92f, 0.84f);
        Color windowColor = new(0.72f, 0.88f, 0.95f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(busRoot.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.26f, 0f);
        body.transform.localScale = new Vector3(1.24f, 0.42f, 0.44f);
        ApplyColor(body, bodyColor);
        ConfigureShadowVisual(body);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(busRoot.transform, false);
        roof.transform.localPosition = new Vector3(-0.02f, 0.56f, 0f);
        roof.transform.localScale = new Vector3(1.02f, 0.12f, 0.4f);
        ApplyColor(roof, roofColor);
        ConfigureShadowVisual(roof);

        GameObject windowBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windowBand.transform.SetParent(busRoot.transform, false);
        windowBand.transform.localPosition = new Vector3(-0.02f, 0.38f, 0f);
        windowBand.transform.localScale = new Vector3(0.94f, 0.18f, 0.46f);
        ApplyColor(windowBand, windowColor);
        ConfigureShadowVisual(windowBand);

        GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windshield.transform.SetParent(busRoot.transform, false);
        windshield.transform.localPosition = new Vector3(0.56f, 0.41f, 0f);
        windshield.transform.localScale = new Vector3(0.12f, 0.2f, 0.38f);
        ApplyColor(windshield, new Color(0.66f, 0.86f, 0.94f));
        ConfigureShadowVisual(windshield);

        GameObject rearWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rearWindow.transform.SetParent(busRoot.transform, false);
        rearWindow.transform.localPosition = new Vector3(-0.56f, 0.39f, 0f);
        rearWindow.transform.localScale = new Vector3(0.08f, 0.17f, 0.34f);
        ApplyColor(rearWindow, new Color(0.66f, 0.84f, 0.92f));
        ConfigureShadowVisual(rearWindow);

        GameObject headlightLeftVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlightLeftVisual.transform.SetParent(busRoot.transform, false);
        headlightLeftVisual.transform.localPosition = new Vector3(0.61f, 0.26f, -0.14f);
        headlightLeftVisual.transform.localScale = new Vector3(0.04f, 0.06f, 0.08f);
        ApplyColor(headlightLeftVisual, new Color(0.34f, 0.3f, 0.22f));
        ConfigureShadowVisual(headlightLeftVisual);

        GameObject headlightRightVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlightRightVisual.transform.SetParent(busRoot.transform, false);
        headlightRightVisual.transform.localPosition = new Vector3(0.61f, 0.26f, 0.14f);
        headlightRightVisual.transform.localScale = new Vector3(0.04f, 0.06f, 0.08f);
        ApplyColor(headlightRightVisual, new Color(0.34f, 0.3f, 0.22f));
        ConfigureShadowVisual(headlightRightVisual);

        GameObject sideStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sideStripe.transform.SetParent(busRoot.transform, false);
        sideStripe.transform.localPosition = new Vector3(0f, 0.23f, 0f);
        sideStripe.transform.localScale = new Vector3(1.08f, 0.06f, 0.47f);
        ApplyColor(sideStripe, new Color(0.98f, 0.86f, 0.2f));
        ConfigureShadowVisual(sideStripe);

        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.transform.SetParent(busRoot.transform, false);
        door.transform.localPosition = new Vector3(0.18f, 0.23f, -0.22f);
        door.transform.localScale = new Vector3(0.24f, 0.32f, 0.05f);
        ApplyColor(door, new Color(0.92f, 0.94f, 0.98f));
        ConfigureShadowVisual(door);

        float[] wheelX = { -0.38f, 0.38f };
        float[] wheelZ = { -0.18f, 0.18f };
        foreach (float wx in wheelX)
        {
            foreach (float wz in wheelZ)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.transform.SetParent(busRoot.transform, false);
                wheel.transform.localPosition = new Vector3(wx, 0.1f, wz);
                wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                wheel.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);
                ApplyColor(wheel, new Color(0.12f, 0.12f, 0.12f));
                ConfigureShadowVisual(wheel);
            }
        }

        GameObject routePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        routePlate.transform.SetParent(busRoot.transform, false);
        routePlate.transform.localPosition = new Vector3(0.48f, 0.53f, 0f);
        routePlate.transform.localScale = new Vector3(0.18f, 0.08f, 0.3f);
        ApplyColor(routePlate, new Color(0.98f, 0.84f, 0.14f));
        ConfigureShadowVisual(routePlate);

        GameObject leftLightObject = new("HiringBusHeadlightLeft");
        leftLightObject.transform.SetParent(busRoot.transform, false);
        leftLightObject.transform.localPosition = new Vector3(0.64f, 0.28f, -0.14f);
        leftLightObject.transform.localRotation = Quaternion.Euler(8f, 90f, 0f);
        Light leftLight = leftLightObject.AddComponent<Light>();
        leftLight.type = LightType.Spot;
        leftLight.color = new Color(1f, 0.9f, 0.72f);
        leftLight.range = 3.6f;
        leftLight.spotAngle = 42f;
        leftLight.innerSpotAngle = 22f;
        leftLight.intensity = 0f;
        leftLight.shadows = LightShadows.None;
        leftLight.enabled = false;

        GameObject rightLightObject = new("HiringBusHeadlightRight");
        rightLightObject.transform.SetParent(busRoot.transform, false);
        rightLightObject.transform.localPosition = new Vector3(0.64f, 0.28f, 0.14f);
        rightLightObject.transform.localRotation = Quaternion.Euler(8f, 90f, 0f);
        Light rightLight = rightLightObject.AddComponent<Light>();
        rightLight.type = LightType.Spot;
        rightLight.color = new Color(1f, 0.9f, 0.72f);
        rightLight.range = 3.6f;
        rightLight.spotAngle = 42f;
        rightLight.innerSpotAngle = 22f;
        rightLight.intensity = 0f;
        rightLight.shadows = LightShadows.None;
        rightLight.enabled = false;

        hiringDriverArrival.BusRootTransform = busRoot.transform;
        hiringDriverArrival.HeadlightLeftRenderer = headlightLeftVisual.GetComponent<Renderer>();
        hiringDriverArrival.HeadlightRightRenderer = headlightRightVisual.GetComponent<Renderer>();
        hiringDriverArrival.HeadlightLeft = leftLight;
        hiringDriverArrival.HeadlightRight = rightLight;
        hiringDriverArrival.BusWorldX = spawnX;
        hiringDriverArrival.BusSpeed = EdgeHighwayBusSpeed * 0.92f;
        hiringDriverArrival.BobPhase = Random.Range(0f, 10f);
        UpdateHiringBusTransform();
    }

    private void UpdateHiringBusTransform()
    {
        if (hiringDriverArrival == null || hiringDriverArrival.BusRootTransform == null)
        {
            return;
        }

        float laneZ = GetEdgeHighwayBusLaneWorldZ(isCitySideLane: true);
        float bob = Mathf.Sin(Time.time * 3.2f + hiringDriverArrival.BobPhase) * 0.015f;
        float y = SampleTerrainHeight(hiringDriverArrival.BusWorldX, laneZ) + RoadHeight + EdgeHighwayBusLift + bob;
        hiringDriverArrival.BusRootTransform.position = new Vector3(hiringDriverArrival.BusWorldX, y, laneZ);
        hiringDriverArrival.BusRootTransform.rotation = Quaternion.identity;

        float darkness = 1f - currentStylizedDaylight;
        bool headlightsOn = darkness > 0.55f;
        float headlightIntensity = headlightsOn ? Mathf.Lerp(0.4f, 1.75f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
        Color lampColor = Color.Lerp(
            new Color(0.34f, 0.3f, 0.22f),
            new Color(1f, 0.94f, 0.78f),
            Mathf.Clamp01(headlightIntensity / 1.75f));

        if (hiringDriverArrival.HeadlightLeft != null)
        {
            hiringDriverArrival.HeadlightLeft.enabled = headlightsOn;
            hiringDriverArrival.HeadlightLeft.intensity = headlightIntensity;
        }

        if (hiringDriverArrival.HeadlightRight != null)
        {
            hiringDriverArrival.HeadlightRight.enabled = headlightsOn;
            hiringDriverArrival.HeadlightRight.intensity = headlightIntensity;
        }

        if (hiringDriverArrival.HeadlightLeftRenderer != null)
        {
            hiringDriverArrival.HeadlightLeftRenderer.material.color = lampColor;
        }

        if (hiringDriverArrival.HeadlightRightRenderer != null)
        {
            hiringDriverArrival.HeadlightRightRenderer.material.color = lampColor;
        }
    }

    private float GetEdgeHighwayBusLaneWorldZ(bool isCitySideLane)
    {
        float centerZ = 1f;
        return centerZ + (isCitySideLane ? EdgeHighwayBusLaneOffset : -EdgeHighwayBusLaneOffset);
    }

    private void SetupDistantClouds()
    {
        distantClouds.Clear();

        // SpawnPosition = behind left/near edge; clouds travel along CloudTravelDir, staggered via initialOffset
        // Args: spawnPosition, travelSpeed, bobAmplitude, bobSpeed, phaseOffset, scale, initialOffset
        // Z spread covers from well before the grid to well beyond it, matching full screen top-to-bottom
        Vector3 center = new(GridWidth * 0.5f, 0f, GridHeight * 0.5f);
        Vector3 spawnBase = center + new Vector3(-30f, 0f, -4f);

        // Near (bottom screen) — lower Y so they sit closer to ground-level perspective
        CreateDistantCloud(spawnBase + new Vector3(0f, 10f, -18f), 1.0f, 0.7f,  0.44f, 0.30f, 1.8f,   4f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 11f, -12f), 0.8f, 0.85f, 0.38f, 1.50f, 2.0f,  22f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 12f,  -7f), 1.2f, 0.75f, 0.41f, 0.80f, 2.15f,  0f);
        // Mid (center screen)
        CreateDistantCloud(spawnBase + new Vector3(0f, 14f,  -1f), 1.1f, 0.9f,  0.36f, 2.10f, 2.35f, 14f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 15f,   5f), 0.9f, 0.72f, 0.48f, 0.60f, 2.05f, 36f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 16f,  11f), 1.3f, 0.95f, 0.32f, 1.90f, 2.4f,  50f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 14f,   8f), 0.8f, 0.68f, 0.34f, 1.20f, 1.95f, 28f);
        // Far (top screen)
        CreateDistantCloud(spawnBase + new Vector3(0f, 17f,  17f), 1.5f, 0.82f, 0.38f, 2.80f, 2.2f,   9f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 18f,  23f), 1.0f, 0.9f,  0.42f, 0.40f, 2.35f, 42f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 17f,  30f), 1.2f, 0.78f, 0.46f, 1.60f, 2.1f,  18f);
    }

    private void CreateDistantCloud(Vector3 spawnPosition, float travelSpeed, float bobAmplitude, float bobSpeed, float phaseOffset, float scaleMultiplier, float initialOffset)
    {
        GameObject cloudRoot = new($"DistantCloud_{distantClouds.Count + 1}");
        cloudRoot.transform.SetParent(worldRoot, false);
        cloudRoot.transform.position = spawnPosition + CloudTravelDir * initialOffset;
        cloudRoot.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        cloudRoot.transform.localScale = Vector3.one * scaleMultiplier;

        CreateCloudLump(cloudRoot.transform, new Vector3(-0.95f, 0f, 0f), new Vector3(1.5f, 0.8f, 0.9f));
        CreateCloudLump(cloudRoot.transform, new Vector3(0f, 0.18f, 0f), new Vector3(1.9f, 1f, 1f));
        CreateCloudLump(cloudRoot.transform, new Vector3(1.02f, 0.02f, 0.08f), new Vector3(1.4f, 0.74f, 0.86f));
        CreateCloudLump(cloudRoot.transform, new Vector3(0.18f, -0.12f, 0.18f), new Vector3(1.7f, 0.54f, 0.86f));

        cloudRoot.SetActive(true);
        distantClouds.Add(new DistantCloudData
        {
            RootTransform = cloudRoot.transform,
            SpawnPosition = spawnPosition,
            TravelOffset = initialOffset,
            TravelSpeed = travelSpeed,
            VerticalBobAmplitude = bobAmplitude,
            VerticalBobSpeed = bobSpeed,
            PhaseOffset = phaseOffset
        });
    }

    private void CreateCloudLump(Transform parent, Vector3 localPosition, Vector3 localScale)
    {
        GameObject lump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lump.transform.SetParent(parent, false);
        lump.transform.localPosition = localPosition;
        lump.transform.localScale = localScale;
        ApplyColor(lump, new Color(0.97f, 0.98f, 1f));
        Renderer rendererComponent = lump.GetComponent<Renderer>();
        if (rendererComponent != null)
        {
            rendererComponent.shadowCastingMode = ShadowCastingMode.On;
            rendererComponent.receiveShadows = false;
        }

        Collider colliderComponent = lump.GetComponent<Collider>();
        if (colliderComponent != null)
        {
            colliderComponent.enabled = false;
        }
    }

    private void UpdateDistantClouds()
    {
        if (distantClouds.Count == 0 || mainCamera == null)
        {
            return;
        }

        float dt = Time.deltaTime * gameSpeedMultiplier;
        float time = Time.time;

        for (int i = 0; i < distantClouds.Count; i++)
        {
            DistantCloudData cloud = distantClouds[i];
            if (cloud.RootTransform == null)
            {
                continue;
            }

            cloud.TravelOffset += cloud.TravelSpeed * dt;
            if (cloud.TravelOffset > CloudTravelLength)
            {
                cloud.TravelOffset -= CloudTravelLength;
            }

            float bob = Mathf.Sin(time * cloud.VerticalBobSpeed + cloud.PhaseOffset * 1.9f) * cloud.VerticalBobAmplitude;
            Vector3 pos = cloud.SpawnPosition + CloudTravelDir * cloud.TravelOffset;
            pos.y += bob;
            cloud.RootTransform.position = pos;
        }
    }

}
