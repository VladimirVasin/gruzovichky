using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private const int GridWidth = 20;
    private const int GridHeight = 20;
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
    private const float CloudTravelLength = 64f;  // full path spawn→exit

    private readonly HashSet<Vector2Int> roadCells = new();
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
        Motel
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
        driver.WalkAnimationTime = 0f;
        ApplyDriverPose(driver, 0f, 0f);
        driver.WalkTargetWorld = GetDriverParkingWaitPosition(assignedTruck);
        BuildDriverWalkPath(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld);
        driver.WalkPhase = DriverRescuePhase.ToParkingForShift;
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
        if (driver == null || driver.ShiftStartHour < 0 || driver.IsOnActiveShift || driver.RestPhase != DriverRestPhase.None || driver.WalkPhase != DriverRescuePhase.None || driver.AssignedTruckNumber <= 0)
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

    private void UpdateDriverShiftActivation(DriverAgent driver)
    {
        if (driver == null) return;
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
            UpdateDriverWalk(driver);
            UpdateDriverRest(driver);
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
        SetupDistantClouds();
        RebuildRoadLanterns();
        PopulateMiscTrees();
        SetupBuildHoverHighlight();
        SetupForestWorkers();
        SetupSelectionVisuals();
        SetupTruck();
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

        float grassPatchNoise = Mathf.PerlinNoise((x + 1) * 0.18f + 4.2f, (y + 1) * 0.2f + 7.4f);
        bool useGrassPatch = grassSurfaceMaterial != null && grassPatchNoise > 0.5f;
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

    private void SetupDistantClouds()
    {
        distantClouds.Clear();

        // SpawnPosition = behind left/near edge; clouds travel along CloudTravelDir, staggered via initialOffset
        // Args: spawnPosition, travelSpeed, bobAmplitude, bobSpeed, phaseOffset, scale, initialOffset
        Vector3 center = new(GridWidth * 0.5f, 0f, GridHeight * 0.5f);
        Vector3 spawnBase = center + new Vector3(-24f, 0f, -4f);

        CreateDistantCloud(spawnBase + new Vector3(0f, 15f, -5f),  1.0f, 0.9f,  0.42f, 0.95f, 2.35f,  0f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 17f,  3f),  0.8f, 0.8f,  0.36f, 1.8f,  2.15f,  9f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 14f,  9f),  1.3f, 0.72f, 0.48f, 0.7f,  2.05f, 18f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 16f, -1f),  1.1f, 0.95f, 0.32f, 2.4f,  2.4f,  27f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 13f,  6f),  0.9f, 0.68f, 0.34f, 1.2f,  1.95f, 36f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 15f, 13f),  1.5f, 0.82f, 0.38f, 2.9f,  2.2f,  45f);
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
