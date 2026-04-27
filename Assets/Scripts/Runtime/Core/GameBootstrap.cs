using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private const int GridWidth = 100;
    private const int GridHeight = 100;
    private const int WaterRiverWidth = 4;
    private const float CellSize = 1f;
    private const float RoadHeight = 0.12f;
    private const float TruckCruiseSpeed = 1.9f;
    private const int ForestMaxLogsStorage = 10;
    private const int FurnitureFactoryMaxBoardsStorage = 6;
    private const int FurnitureFactoryMaxTextileStorage = 6;
    private const int FurnitureFactoryMaxFurnitureStorage = 6;
    private const float FurnitureFactoryProcessingDuration = 5.5f;
    private const int WarehouseMaxWorkers        = 3;
    private const int WarehouseMaxFuelStorage    = 20;
    private const int WarehouseMaxAlcoholStorage = 20;
    private const int WarehouseMaxFoodStorage    = 20;
    private const int WarehouseResourceStartAmount = 5;
    private const int GasStationMaxFuelStorage   = 5;
    private const int BarMaxAlcoholStorage       = 5;
    private const int CanteenMaxFoodStorage      = 5;
    private const float ForestLogProgressPerChop = 0.08f;
    private const float CameraPanSpeed = 9f;
    private const float CameraDragPanMultiplier = 0.035f;
    private const float CameraZoomSpeed = 8f;
    private const float CameraMinHeight = 3.6f;
    private const float CameraMaxHeight = 120f;
    private const float CameraMinDistance = 6f;
    private const float CameraMaxDistance = 120f;
    private const float EdgeHighwayBusSpeed = 2.8f;
    private const float EdgeHighwayBusSpawnIntervalMin = 14f;
    private const float EdgeHighwayBusSpawnIntervalMax = 30f;
    private const float EdgeHighwayBusSpawnSpacing = 5.5f;
    private const float EdgeHighwayBusLaneOffset = 0.32f;
    private const float RoadLaneOffset = 0.22f;
    private const float EdgeHighwayBusLift = 0.24f;
    private const float EdgeHighwayBusPassbyDistance = 5.5f;
    private const float LocalBusStopDwellGameMinutes = 5f;
    private const float LocalBusSpeedMultiplier = 0.92f;
    private const int LocalBusMaxPassengers = 5;
    private const int LocalBusFare = 1;
    private const float HiringBusStopDuration = 2.8f;
    private const float TruckFollowDistance = 4.4f;
    private const float TruckFollowHeight = 2.05f;
    private const float TruckFollowLookHeight = 1.05f;
    private const float TruckSegmentStartLift = 0.24f;
    private const float TruckSuspensionBobAmount = 0.045f;
    private const float TruckWheelRadius = 0.12f;
    private const float TruckCargoInteractionDuration = 3f;
    private const float TruckFuelCapacity = 100f;
    private const float TruckFuelPerCell = 0.5f;
    internal const float TruckAutoRefuelThreshold = 50f;
    private const float WaterEffectsUpdateInterval = 1f / 24f;
    private const float WaterLodMediumCameraHeight = 18f;
    private const float WaterLodFarCameraHeight = 28f;
    private const float FarZoomVisualLodEnterHeight = 46f;
    private const float FarZoomVisualLodExitHeight = 40f;
    private const float DriverSleepDuration = DayNightCycleDuration / 24f * 8f;
    private const float WorkerCanteenDuration = DayNightCycleDuration / 24f * 1f;
    private const float WorkerLeisureDuration = DayNightCycleDuration / 24f * 2f;
    private const float WorkerGamblingHallDuration = DayNightCycleDuration / 24f * 2.5f;
    private const float WorkerCityParkDuration     = DayNightCycleDuration / 24f * 1.5f;
    private const int   WorkerGamblingMinBalance   = 5;
    private const int   HousePurchasePrice         = 300;
    private const int   MaxPersonalHouseResidents  = 2;
    private const int   CarPurchasePrice           = 100;
    private const int   WorkerGamblingMinBet       = 5;
    private const int   WorkerGamblingMaxBet       = 10;
    private const float WorkerFreeIdleMinDuration = DayNightCycleDuration / 24f * 1f;
    private const float WorkerFreeIdleMaxDuration = DayNightCycleDuration / 24f * 3f;
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
    private const int SmokingParticlePoolSize = 5;
    private const float SmokingParticleEmitInterval = 1.1f;
    private const float SmokingParticleMaxLife = 3.2f;
    private const int AmbientAirGlobalParticleCount = 34;
    private const int AmbientAirForestParticleCount = 18;
    private const int AmbientAirHighwayDustParticleCount = 20;
    private const int MiscBirdCount = 10;
    private const int AmbientCatCount = 3;
    private const int AmbientSquirrelCount = 6;
    private const int NightStarCount = 120;
    private const int AmbientBeeCount = 8;
    private const int AmbientLanternMothSwarmMaxCount = 8;
    private const int RiverFishMaxActiveCount = 6;
    private const int ExhaustSmokePoolSize = 36;
    private const int TruckDirtDustPoolSize = 28;
    private const float ExhaustEmitInterval = 0.28f;
    private const float TruckDirtDustEmitInterval = 0.13f;
    private const float BuildingSmokeEmitInterval = 0.38f;
    private const int StartingTreasury = 350;
    private const int DefaultDailyBuildingTaxPercent = 10;
    private const int MinDailyBuildingTaxPercent = 0;
    private const int MaxDailyBuildingTaxPercent = 50;
    private const float MoneyPopupDuration = 1.4f;
    private const int AudioSampleRate = 22050;
    private const int MaxTruckCount = 5;
    private const int HireTruckCost = 300;
    private const int HireDriverCost = 50;
    private const int InitialWorkerCount = 3;
    private const int MaxMoneyLedgerEntries = 128;
    private const float DayNightCycleDuration = 440f; // 4 periods × 110s = 7m20s full day
    private const float DriverShiftArrivalLeadHours = 1f;
    private const int ProductionWorkStartHour = 8;
    private const int ProductionWorkEndHour = 18;
    private const float DioramaCameraMinPitch = 20f;
    private const float DioramaCameraPitch = 52f;
    private static readonly Vector3 DioramaCameraOffset = new(-24f, 30f, -24f);
    private static readonly Vector3 CloudTravelDir = new Vector3(1f, 0f, 0.4f).normalized;
    private const float CloudTravelLength = 260f; // Full cloud path from off-map spawn to exit.

    private readonly HashSet<Vector2Int> waterCells = new();
    private readonly HashSet<Vector2Int> lakeWaterCells = new();
    private readonly HashSet<Vector2Int> naturalBeachCells = new();
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
    private readonly List<Material> locationNightLightMaterials = new();
    private readonly List<Color> locationNightLightOffColors = new();
    private readonly List<Color> locationNightLightOnColors = new();
    private readonly List<float> locationNightLightMaxIntensities = new();
    private readonly List<float> locationNightLightRanges = new();
    private readonly List<RoadLanternData> roadLanterns = new();
    private readonly Dictionary<Vector2Int, (GameObject Root, RoadLanternData Data)> roadCellLanternMap = new();
    private readonly Dictionary<Vector2Int, (GameObject Root, Vector2Int SideCell)> roadCellBenchMap = new();
    private readonly HashSet<Vector2Int> benchSideCells = new();
    private readonly Dictionary<Vector2Int, (GameObject Root, Vector2Int SideCell)> roadCellSignMap = new();
    private readonly HashSet<Vector2Int> signSideCells = new();
    private readonly List<TruckAgent> truckAgents = new();
    private readonly List<DriverAgent> driverAgents = new();
    private readonly List<ForestWorkerAmbient> forestWorkers = new();
    private readonly List<Vector3> forestWorkPoints = new();
    private readonly List<Transform> forestWorkTargetTrees = new();
    private readonly List<ForestTreeWobble> forestTreeWobbles = new();
    private readonly List<MiscTreeSway> miscTreeSways = new();
    private readonly List<Vector3> miscTreePerchPoints = new();
    private readonly List<MiscBirdData> miscBirds = new();
    private readonly List<Vector3> ambientCatRoamPoints = new();
    private readonly List<AmbientCatData> ambientCats = new();
    private readonly List<Vector3> ambientSquirrelRoamPoints = new();
    private readonly List<AmbientSquirrelData> ambientSquirrels = new();
    private readonly List<NightStarData> nightStars = new();
    private readonly List<Vector3> flowerBeePoints = new();
    private readonly List<AmbientBeeData> ambientBees = new();
    private readonly List<AmbientLanternMothSwarmData> ambientLanternMothSwarms = new();
    private readonly List<WaterSurfaceTileData> waterSurfaceTiles = new();
    private readonly List<WaterBodyTileData> waterBodyTiles = new();
    private readonly List<WaterShoreFoamData> waterShoreFoams = new();
    private readonly List<WaterShoreWashPatchData> waterShoreWashPatches = new();
    private readonly List<RiverFishData> riverFish = new();
    private readonly List<DistantCloudData> distantClouds = new();
    private readonly List<AmbientAirParticleData> ambientAirParticles = new();
    private readonly List<ShadowLodRendererData> shadowLodRenderers = new();
    private readonly List<MoneyLedgerEntry> moneyLedgerEntries = new();
    private readonly HashSet<LocationType> occupiedServiceLocations = new();
    private readonly Dictionary<LocationType, GameObject> locationSelectionHighlights = new();
    private readonly List<GameObject> localStopSelectionHighlights = new();
    private readonly List<LocationData> personalHouses = new();
    private readonly List<GameObject> personalHouseSelectionHighlights = new();
    private int selectedPersonalHouseIndex = -1;
    private readonly List<EdgeHighwayBusData> edgeHighwayBuses = new();
    private readonly List<RiverBoatData> riverBoats = new();
    private LocalBusRouteData localBusRoute;
    private float[,] terrainHeights = new float[GridWidth, GridHeight];
    private readonly List<NaturalZoneData> forestZones = new();
    private readonly List<NaturalZoneData> hillZones = new();
    private readonly List<NaturalZoneData> lakeZones = new();

    private Camera mainCamera;
    private GameObject truckObject;
    private Transform truckVisualRoot;
    private Transform truckBodyTransform;
    private Transform truckCabinTransform;
    private Renderer truckHeadlightLeftRenderer;
    private Renderer truckHeadlightRightRenderer;
    private Material truckHeadlightLeftMaterial;
    private Material truckHeadlightRightMaterial;
    private Transform worldRoot;
    private Transform groundRoot;
    private Transform gridLinesRoot;
    private Transform roadsRoot;
    private Transform unifiedRoadVisualRoot;
    private bool suppressUnifiedRoadVisualRebuild;
    private bool suppressRoadsideRefresh;
    private readonly HashSet<Vector2Int> pendingRoadsideRefreshCells = new();
    private Transform lanternsRoot;
    private Transform roadsidePropsRoot;
    private Transform roadsideSignsRoot;
    private readonly List<Vector3> roadsideBenchPositions = new();
    private readonly bool[] benchOccupied = new bool[GridWidth * GridHeight / 16];
    private readonly List<Vector3> cityParkBenchPositions = new();
    private readonly bool[] cityParkBenchOccupied = new bool[8];
    private Transform miscRoot;
    private Transform ambientAirRoot;
    private Transform exhaustSmokeRoot;
    private Transform truckDirtDustRoot;
    private readonly List<ExhaustSmokeParticle> exhaustSmokePool = new();
    private readonly List<ExhaustSmokeParticle> truckDirtDustPool = new();
    private float localBusExhaustEmitTimer;
    private float sawmillSmokeEmitTimer;
    private float furnitureFactorySmokeEmitTimer;
    private Transform miscBirdRoot;
    private Transform ambientCatRoot;
    private Transform ambientSquirrelRoot;
    private Transform nightSkyRoot;
    private Material moonMaterial;
    private Transform ambientBeeRoot;
    private Transform ambientLanternMothRoot;
    private Transform waterEffectsRoot;
    private Transform riverFishRoot;
    private bool isFarZoomVisualLodActive;
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
    private AudioSource cityMusicSource;      // looping main theme for city mode
    private AudioSource mainMenuMusicSource;  // looping music for main menu / pause screen
    private AudioSource truckFxAudioSource;
    private AudioSource riverAmbientAudioSource;
    private Material groundSurfaceMaterial;
    private Material grassSurfaceMaterial;
    private Material waterShallowMaterial;
    private Material waterDeepMaterial;
    private Material beachSurfaceMaterial;
    private Material shoreSurfaceMaterial;
    private Material roadSurfaceMaterial;
    private Material roadShoulderMaterial;
    private Material highwaySurfaceMaterial;
    private Material highwayShoulderMaterial;
    private Texture2D groundSurfaceTexture;
    private Texture2D grassSurfaceTexture;
    private Texture2D roadSurfaceTexture;
    private Light mainDirectionalLight;
    private Volume dioramaVolume;
    private VolumeProfile dioramaVolumeProfile;
    private ColorAdjustments dioramaColorAdjustments;
    private Bloom dioramaBloom;
    private DepthOfField dioramaDepthOfField;
    private Vignette dioramaVignette;
    private GameObject selectedLocationLabelRoot;
    private TextMesh selectedLocationLabelText;
    private readonly List<TextMesh> selectedLocationLabelOutlines = new();
    private GameObject cargoTransferCrate;
    private GameObject buildHoverHighlight;
    private GameObject buildHoverDrivewayHighlight;
    private readonly List<GameObject> buildHoverCellHighlights = new();
    private readonly List<Vector2Int> buildPreviewFootprintCells = new();
    private readonly List<Vector2Int> buildPreviewRoadDirections = new();
    private Vector2Int? buildPreviewDrivewayCell;
    private Vector2Int? roadPathStart;
    private GameObject roadPathStartHighlight;
    private GameObject roadPathStartSideHighlight;
    private int buildPlacementRotationIndex;
    private GameObject selectedDebugCellHighlight;
    private GameObject selectedDebugCellOutline;
    private Transform edgeHighwayBusRoot;
    private Vector2Int truckCell;
    private Vector3 truckTargetWorld;
    private Vector3 truckSegmentStartWorld;
    private Vector3 truckSmoothedForward = Vector3.forward;
    private bool isTruckMoving;
    private bool isTruckInteracting;
    private bool isTruckWaitingForService;
    private TruckAgent currentLoadedTruckAgent;
    private bool isDriverRescueActive;
    private CargoType truckCargoType = CargoType.None;
    private int truckCargoAmount;
    private float productionTimer;
    private Vector3 cameraFocusPoint;
    private Vector3 cameraOffset;
    private Vector3 cameraTargetOffset;
    private Vector2 lastMousePosition;
    private Vector2 lastMiddleMousePosition;
    private Vector2 rightMousePressPosition;
    private float truckSegmentProgress;
    private float truckSegmentDuration;
    private float truckWheelSpinAngle;
    private float truckSteerAngle;
    private float truckInteractionTimer;
    private float moneyPopupTimer;
    private float truckFuel = TruckFuelCapacity;
    private float dayNightCycleTimer = DayNightCycleDuration * 0.25f; // start at 06:00 вЂ” morning
    private int   currentDay = 1;
    private int   dailyBuildingTaxPercent = DefaultDailyBuildingTaxPercent;
    private int   lastTaxCollectionDay;
    private int   lastTaxCollectedAmount;
    private int   lastTaxedBuildingCount;
    private float currentStylizedDaylight = 1f;
    private float forestProductionProgress;
    private float sawmillProcessingTimer;
    private float furnitureFactoryProcessingTimer;
    private float dayBirdTimer;
    private float nightOwlTimer;
    private float lanternBuzzTimer;
    private float warehouseCreakTimer;
    private float riverSplashTimer;
    private float terrainNoiseOffsetX;
    private float terrainNoiseOffsetY;
    private LocationType? selectedLocation;
    private int selectedLocalStopIndex = -1;
    private bool isTruckDetailsOpen;
    private bool isLocalBusDetailsOpen;
    private bool isRightMouseDragging;
    private bool isCameraReturningToDiorama;
    private bool isCameraRotatingToTarget;
    private bool isTruckCameraFocused;
    private bool isTruckAutoModeEnabled;
    private bool isFleetScreenDirty = true;
    private bool isMainMenuOpen = true;
    private bool isRacingActive;
    private int racingBonusEarned;
    private int selectedTruckNumber = 1;
    private int money;
    private int cottonStored = 0;
    private int textileStored = 0;
    private int furnitureStored = 0;
    private int currentAssignedTripReward;
    private bool isFleetPanelOpen;
    private bool isShiftsPanelOpen;
    private bool isDriversPanelOpen;
    private int selectedShiftDriverId; // DriverId, 0 = none
    private int intercityDriverId;
    private readonly int[] busDriverShiftIds = new int[3];
    private TradeResourceType selectedTradeResourceType = TradeResourceType.Logs;
    private TradeOrderType selectedTradeOrderType = TradeOrderType.Buy;
    private int selectedTradeOrderAmount = 5;
    private bool isTradeResourceDropdownOpen;
    private bool isTradeActionDropdownOpen;
    private int nextTradeOrderId = 1;

    public sealed class TradeHudOrder
    {
        public int Id;
        public TradeResourceType ResourceType;
        public TradeOrderType OrderType;
        public int Amount;
        public int TargetRegionIndex = -1; // -1 = generic (Trade panel), 0-8 = specific region
    }

    private TradeResourceType worldMapRouteResource  = TradeResourceType.Alcohol;
    private int               worldMapRouteAmount    = 1;
    private TradeOrderType    worldMapRouteOrderType = TradeOrderType.Buy;

    private readonly List<TradeHudOrder> activeTradeHudOrders = new();

    private string tradeDispatchStatusText = "Assign an Intercity driver to unlock trade dispatch.";
    private bool isResourcesPanelOpen;
    private bool isEconomyPanelOpen;
    private bool isEconomyTaxesTabActive = true;
    private bool isBuildPanelOpen;
    private bool isWorldMapPanelOpen;
    private bool isStatesPanelOpen;
    private int selectedWorldMapRegionIndex = 4;
    private int moneyPopupAmount;
    private int gameSpeedMultiplier = 1;
    private int lastActiveGameSpeedMultiplier = 1;
    private int nextHireTruckNumber = 2;
    private int nextDriverId = 1;
    private TripType currentAssignedTrip = TripType.None;
    private BuildTool activeBuildTool = BuildTool.None;
    private HashSet<BuildTool> unlockedBuildTools;
    private readonly List<LocationData> localStops = new();
    private Vector2Int? hoveredBuildCell;
    private Vector2Int? selectedDebugCell;
    private float edgeHighwayBusSpawnTimerCitySide;
    private float edgeHighwayBusSpawnTimerOuterSide;
    private float riverBoatSpawnTimerLeft;
    private float riverBoatSpawnTimerRight;
    private Transform riverBoatRoot;
    private HiringDriverArrivalData hiringDriverArrival;

    private sealed class RoadLanternData
    {
        public Light Light;
        public Renderer GlowRenderer;
        public Material GlowMaterial;
        public float ActivationOffset;
        public float FlickerSeed;
        public float FlickerSpeed;
        public float FlickerStrength;
        public float FlickerThreshold;
    }

    private sealed class ShadowLodRendererData
    {
        public Renderer Renderer;
        public ShadowCastingMode OriginalShadowMode;
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

    private sealed class RiverBoatData
    {
        public Transform RootTransform;
        public float WorldX;
        public float TravelDirection;
        public float Speed;
        public float BobPhase;
        public float RockPhase;
        public bool HasEnteredRiver;
        public Renderer LanternRenderer;
        public Light LanternLight;
        public AudioSource BoatAudioSource;
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
        public Material HeadlightLeftMaterial;
        public Material HeadlightRightMaterial;
        public Light HeadlightLeft;
        public Light HeadlightRight;
    }

    private sealed class LocalBusRouteData
    {
        public Transform RootTransform;
        public Renderer HeadlightLeftRenderer;
        public Renderer HeadlightRightRenderer;
        public Material HeadlightLeftMaterial;
        public Material HeadlightRightMaterial;
        public Light HeadlightLeft;
        public Light HeadlightRight;
        public DriverAgent Driver;
        public readonly List<Vector3> Waypoints = new();
        public int WaypointIndex;
        public int CurrentStopIndex = -1;
        public int TravelDirection = 1;
        public float DwellTimer;
        public float BobPhase;
        public float Speed;
        public int PassengerCount;
        public int PassengerCapacity = LocalBusMaxPassengers;
        public int Bank;
        public string LastBoardingBlockReason;
        public LocalBusPhase Phase = LocalBusPhase.None;
    }

    private enum LocalBusPhase
    {
        None,
        ParkedAwaitingShiftStart,
        DrivingRoute,
        WaitingAtStop,
        ReturningToParking
    }

    private sealed class HiringDriverArrivalData
    {
        public DriverAgent Driver;
        public Transform BusRootTransform;
        public Renderer HeadlightLeftRenderer;
        public Renderer HeadlightRightRenderer;
        public Material HeadlightLeftMaterial;
        public Material HeadlightRightMaterial;
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
    private AudioClip moneySpendClip;
    private AudioClip slotReelTickClip;
    private AudioClip slotWinClip;
    private AudioClip slotLoseClip;

    // HUD flash/shake for gambling result
    private float  hudFlashTimer;
    private float  hudFlashDuration;
    private Color  hudFlashColor;
    private float  hudShakeTimer;
    private float  hudShakeDuration;
    private AudioClip edgeHighwayBusPassbyClip;
    private AudioClip riverAmbientClip;
    private AudioClip riverSplashClip;
    private AudioClip boatMotorClip;
    private bool wereAmbientLanternMothsActiveLastFrame;
    private float riverFishSpawnTimer;
    private float truckEngineAudioPhaseOffset;
    private float truckEngineAudioWobbleSpeed = 1f;
    private float truckEngineAudioPitchBias = 1f;
    private float truckEngineAudioVolumeBias = 1f;
    private float waterEffectsUpdateTimer;
    private int waterVisualLodLevel = -1;

    private enum LocationType
    {
        Parking,
        GasStation,
        Forest,
        Warehouse,
        Sawmill,
        FurnitureFactory,
        Motel,
        IntercityStop,
        Stop,
        Bar,
        Canteen,
        GamblingHall,
        CityPark,
        PersonalHouse,
        CarMarket
    }

    /// <summary>
    /// Production locations generate or process cargo.
    /// Service locations support the truck route (fuel, rest, loading/unloading).
    /// </summary>
    private static bool IsProductionLocation(LocationType type) => type switch
    {
        LocationType.Forest           => true,
        LocationType.Sawmill          => true,
        LocationType.FurnitureFactory => true,
        LocationType.Warehouse        => true,
        _                             => false
    };

    private static bool IsServiceLocation(LocationType type) => !IsProductionLocation(type);

    private bool IsLocationOperational(LocationType type) =>
        !IsProductionLocation(type) ||
        (locations.TryGetValue(type, out LocationData d) && d.Workers > 0);

    private enum CargoType
    {
        None,
        Logs,
        Boards,
        Cotton,
        Textile,
        Furniture,
        Fuel,
        Alcohol,
        Food
    }

    private enum TransportTask
    {
        None,
        ReturnToParking,
        PickUpAtForest,
        DeliverToSawmill,
        PickUpAtSawmill,
        DeliverToWarehouse,
        PickUpBoardsAtWarehouse,
        DeliverBoardsToFurnitureFactory,
        PickUpTextileAtWarehouse,
        DeliverTextileToFurnitureFactory,
        PickUpAtFurnitureFactory,
        DeliverFurnitureToWarehouse
    }

    private enum TruckInteractionType
    {
        None,
        LoadAtForest,
        UnloadAtSawmill,
        LoadAtSawmill,
        UnloadAtWarehouse,
        LoadBoardsAtWarehouse,
        LoadTextileAtWarehouse,
        UnloadBoardsAtFurnitureFactory,
        UnloadTextileAtFurnitureFactory,
        LoadAtFurnitureFactory,
        UnloadFurnitureAtWarehouse,
        TradeUnloadAtWarehouse,
        TradeLoadAtWarehouse,
        RefuelAtGasStation
    }

    private enum TripType
    {
        None,
        ForestToSawmill,
        SawmillToWarehouse,
        WarehouseToFurnitureFactoryBoards,
        WarehouseToFurnitureFactoryTextile,
        FurnitureFactoryToWarehouse
    }

    private enum BuildTool
    {
        None,
        Parking,
        Warehouse,
        SingleRoad,
        Road,
        Stop,
        Forest,
        FurnitureFactory,
        Sawmill,
        Motel,
        Bar,
        Canteen,
        GamblingHall,
        CityPark,
        PersonalHouse,
        CarMarket
    }

    private enum GameStartMode
    {
        Debug,
        User,
        Clear
    }

    public enum TripPhase
    {
        None,
        ToPickup,
        Loading,
        ToDropoff,
        Unloading,
        ReturnToParking
    }

    public enum RefuelPhase
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
        ToParkingForShift,
        IdleWalkToBench,
        IdleSittingOnBench,
        IdleWalkToBar,
        IdleAtBar,
        IdleWalkToCanteen,
        IdleAtCanteen,
        IdleWalkToGamblingHall,
        IdleAtGamblingHall,
        IdleWalkToCityPark,
        IdleAtCityPark,
        IdleSmoking,
        IdlePhoneCall,
        WalkToLocalBusStop,
        WaitingAtLocalBusStop,
        RidingLocalBus,
        ToBuildingForShift,        // walking motel -> production building (logistics pre-shift)
        ToMotelFromBuilding,       // walking building -> motel (logistics post-shift)
        WarehouseDeliveryToService, // walking Warehouse -> service building (carrying resource)
        WarehouseDeliveryReturn,    // walking service building -> Warehouse (empty-handed)
        LumberToTree,
        LumberChopping,
        LumberCarryLogToBuilding,
        LumberReturnToTreeForPlanting,
        LumberPlanting,
        LumberReturnToBuilding,
        ToPersonalHouseForPurchase,
        ToPersonalHouseEntrance,
        ToCarMarketForPurchase
    }

    private enum DriverRestPhase
    {
        None,
        ToMotel,
        ParkAtMotel,
        DriverWalkToMotel,
        Sleeping,
        SleepingAtHome,
        DriverWalkToTruck,
        ReturnToParking
    }

    private enum WorkerLifeGoal
    {
        None,
        Work,
        Eat,
        Leisure,
        Sleep,
        BuyHouse,
        BuyCar,
        Idle
    }

    private enum WorkerNeedKind
    {
        Meal,
        Sleep,
        Leisure
    }

    private enum WorkerNeedStatus
    {
        Ok,
        Warning,
        Critical
    }

    private enum DriverDutyMode
    {
        Local,
        Intercity,
        Logistics   // assigned to a production building
    }

    public enum TradeResourceType
    {
        Logs,
        Boards,
        Cotton,
        Textile,
        Furniture,
        Fuel,
        Alcohol,
        Food
    }

    public enum TradeOrderType
    {
        Buy,
        Sell
    }

    private enum WarehouseResourceType
    {
        Fuel,
        Alcohol,
        Food
    }


    private sealed class LocationData
    {
        public string Label;
        public Vector2Int Min;
        public Vector2Int Max;
        public Vector2Int Anchor;
        public Vector2Int RoadAccess;
        public Color BaseColor;
        public int StopNumber;
        public int LogsStored;
        public int BoardsStored;
        public int TextileStored;
        public int FurnitureStored;
        // Warehouse consumable resources
        public int FuelStored;
        public int AlcoholStored;
        public int FoodStored;
        public GameObject RootObject;
        public Renderer BaseRenderer;
        public readonly List<GameObject> StoredLogVisuals = new();
        public readonly List<GameObject> StoredBoardVisuals = new();

        public int Workers;      // production buildings only: >0 = operational, 0 = offline
        public int ServiceFee;   // Service buildings deduct from driver.Money on entry.
        public int BuildingBank; // Internal revenue: service fees in, gambling payouts out.

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
        public int Priority; // 0=Low, 1=Medium, 2=High
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
        public Material FlashlightMaterial;
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
        public Vector2Int Cell;
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

    private sealed class AmbientAirParticleData
    {
        public Transform RootTransform;
        public Renderer Renderer;
        public Material Material;
        public Vector3 Center;
        public float HalfTravelRange;
        public float HalfLateralRange;
        public float HeightMin;
        public float HeightMax;
        public float TravelOffset;
        public float LateralOffset;
        public float BaseHeightOffset;
        public float TravelSpeed;
        public float BobAmplitude;
        public float BobSpeed;
        public float PhaseOffset;
        public Color BaseColor;
        public bool IsForestLocal;
        public bool IsHighwayDust;
    }

    private sealed class ExhaustSmokeParticle
    {
        public Transform Transform;
        public Material Material;
        public Vector3 Velocity;
        public float LifeTimer;
        public float MaxLife;
        public float BaseScale;
        public bool IsActive;
    }

    private enum MiscBirdState
    {
        Perched,
        Flying
    }

    private sealed class NightStarData
    {
        public Transform Transform;
        public Material Material;
        public Color BaseColor;
        public float TwinkleSpeed;
        public float TwinklePhase;
    }

    private sealed class MiscBirdData
    {
        public Transform RootTransform;
        public Transform BodyTransform;
        public Transform LeftWingTransform;
        public Transform RightWingTransform;
        public Vector3 StartPosition;
        public Vector3 TargetPosition;
        public int CurrentPerchIndex;
        public int TargetPerchIndex;
        public float StateTimer;
        public float FlightDuration;
        public float FlightProgress;
        public float BobPhase;
        public float WingPhase;
        public float PerchYaw;
        public MiscBirdState State;
    }

    private enum AmbientCatState
    {
        Lazing,
        Walking
    }

    private enum AmbientSquirrelState
    {
        Idle,
        Running,
        Foraging
    }

    private sealed class AmbientSquirrelData
    {
        public Transform RootTransform;
        public Transform BodyTransform;
        public Transform HeadTransform;
        public Transform TailTransform;
        public Vector3 CurrentPosition;
        public Vector3 StartPosition;
        public Vector3 TargetPosition;
        public int CurrentPointIndex;
        public int TargetPointIndex;
        public float StateTimer;
        public float MoveDuration;
        public float MoveProgress;
        public float AnimationPhase;
        public float TailPhase;
        public float Yaw;
        public AmbientSquirrelState State;
    }

    private sealed class AmbientCatData
    {
        public Transform RootTransform;
        public Transform BodyTransform;
        public Transform HeadTransform;
        public Transform TailTransform;
        public Vector3 CurrentPosition;
        public Vector3 StartPosition;
        public Vector3 TargetPosition;
        public int CurrentPointIndex;
        public int TargetPointIndex;
        public float StateTimer;
        public float MoveDuration;
        public float MoveProgress;
        public float AnimationPhase;
        public float TailPhase;
        public float Yaw;
        public AmbientCatState State;
    }

    private sealed class AmbientBeeData
    {
        public Transform RootTransform;
        public Renderer BodyRenderer;
        public Renderer StripeRenderer;
        public Renderer LeftWingRenderer;
        public Renderer RightWingRenderer;
        public Material BodyMaterial;
        public Material StripeMaterial;
        public Material LeftWingMaterial;
        public Material RightWingMaterial;
        public Transform LeftWingTransform;
        public Transform RightWingTransform;
        public int FlowerPointIndex;
        public float OrbitRadius;
        public float OrbitHeight;
        public float OrbitSpeed;
        public float OrbitAngle;
        public float VerticalBobAmplitude;
        public float VerticalBobSpeed;
        public float PhaseOffset;
        public float Visibility;
    }

    private sealed class AmbientLanternMothSwarmData
    {
        public Transform RootTransform;
        public readonly List<Transform> ParticleTransforms = new();
        public readonly List<Renderer> ParticleRenderers = new();
        public readonly List<Material> ParticleMaterials = new();
        public int LanternIndex = -1;
        public float OrbitRadius;
        public float OrbitHeight;
        public float OrbitSpeed;
        public float VerticalBobAmplitude;
        public float VerticalBobSpeed;
        public float PhaseOffset;
        public float Visibility;
    }

    private sealed class WaterSurfaceTileData
    {
        public Renderer Renderer;
        public Material Material;
        public Transform Transform;
        public Mesh Mesh;
        public float BaseY;
        public float CurrentTopY;
        public Vector2Int Cell;
        public float BobAmplitude;
        public float BobSpeed;
        public float PhaseOffset;
        public int LayerIndex;
    }

    private sealed class WaterBodyTileData
    {
        public Transform Transform;
        public Mesh Mesh;
        public float BaseY;
        public float BaseTopY;
        public float BottomY;
        public float CurrentTopY;
        public Vector2Int Cell;
        public float PhaseOffset;
    }

    private sealed class WaterShoreFoamData
    {
        public Transform RootTransform;
        public Renderer Renderer;
        public Material Material;
        public float BaseY;
        public float BaseZ;
        public float Width;
        public float DriftSpeed;
        public float DriftOffset;
        public float PulseSpeed;
        public float PhaseOffset;
    }

    private sealed class WaterShoreWashPatchData
    {
        public Transform RootTransform;
        public Renderer Renderer;
        public Material Material;
        public float BaseX;
        public float BaseY;
        public float BaseZ;
        public float Width;
        public float Depth;
        public int ShoreRingIndex;
        public int SegmentIndex;
        public float PhaseOffset;
    }

    private sealed class RiverFishData
    {
        public Transform RootTransform;
        public Transform BodyTransform;
        public Transform TailTransform;
        public Renderer BodyRenderer;
        public Renderer TailRenderer;
        public Material BodyMaterial;
        public Material TailMaterial;
        public float WorldX;
        public float WorldZ;
        public float SwimSpeed;
        public float DepthY;
        public float BobPhase;
        public float TailPhase;
        public float LateralDriftAmplitude;
        public float LateralDriftSpeed;
        public Color BodyColor;
    }

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
        Nightowl,        // Works better during night shifts
        Ironman,         // РњРµРґР»РµРЅРЅРµРµ СѓСЃС‚Р°С‘С‚, СЂРµР¶Рµ РЅСѓР¶РµРЅ РѕС‚РґС‹С…
        Motorhead,       // Р‘РѕРЅСѓСЃ Рє РІРѕР¶РґРµРЅРёСЋ Рё С‚РµС…РѕР±СЃР»СѓР¶РёРІР°РЅРёСЋ
        Trader,          // РўРѕСЂРіРѕРІС‹Рµ СЂРµР№СЃС‹ РїСЂРёРЅРѕСЃСЏС‚ Р±РѕР»СЊС€Рµ РїСЂРёР±С‹Р»Рё
        Handyman,        // РЈСЃРєРѕСЂСЏРµС‚ РїСЂРѕРёР·РІРѕРґСЃС‚РІРѕ РЅР° РІСЃРµС… Р·РґР°РЅРёСЏС…
        Socialite,       // Р’РѕСЃСЃС‚Р°РЅР°РІР»РёРІР°РµС‚ РґРѕСЃСѓРі Р±С‹СЃС‚СЂРµРµ Рё РґРµС€РµРІР»Рµ
        Frugal,          // РўСЂР°С‚РёС‚ РјРµРЅСЊС€Рµ РЅР° СЃРµСЂРІРёСЃРЅС‹Рµ РЅСѓР¶РґС‹
        Quicklearner     // Р‘С‹СЃС‚СЂРµРµ РїСЂРѕРєР°С‡РёРІР°РµС‚ РЅР°РІС‹РєРё РѕС‚ РѕРїС‹С‚Р°
    }

    private enum WorkerPerkType
    {
        Positive,
        Negative
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
        public Material DriverFlashlightMaterial;
        public DriverRestPhase RestPhase = DriverRestPhase.None;
        public float SleepTimer;
        public Vector3 MotelIdlePosition;
        public int AssignedTruckNumber;
        public int Salary = 25;
        public int Money  = 30;
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
        public int  GamblingBet;          // bet placed this visit; 0 = not gambling
        public int  GamblingPayout;       // payout received ($0 on loss)
        public int  GamblingMultiplier;   // 0=loss, 1=x1, 5=x5, 10=x10
        public bool GamblingMoneyPending; // true until money is actually applied after animation
        public int  GamblingBetCount;     // how many bets placed this visit (for 2-bet limit)
        public bool GamblerLostLastTime;  // Gambler perk: true after a loss (doubles next bet)
        public bool GamblerBroke;         // Gambler perk: Money < min bet, visits but skips bet
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
        public int OwnedCarModelIndex = -1;
        public GameObject OwnedCarObject;
        public LocationType? AssignedBuildingType;      // logistics only: building this worker is assigned to
        public bool IsInsideBuilding;                   // true while physically inside the assigned building
        public LocationType? WarehouseDeliveryTarget;   // warehouse worker: current delivery destination
        public WarehouseResourceType WarehouseDeliveryResourceType;
        public int WarehouseDeliveryAmount;
        public bool IsCarryingWarehouseDelivery;
        public string LastWorkerDecisionDebugKey;
    }

}
