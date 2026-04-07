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
    private const float CameraMaxHeight = 18f;
    private const float CameraMinDistance = 6f;
    private const float CameraMaxDistance = 18f;
    private const float TruckFollowDistance = 4.4f;
    private const float TruckFollowHeight = 2.05f;
    private const float TruckFollowLookHeight = 1.05f;
    private const float TruckSegmentStartLift = 0.24f;
    private const float TruckSuspensionBobAmount = 0.045f;
    private const float TruckWheelRadius = 0.12f;
    private const float TruckCargoInteractionDuration = 3f;
    private const float TruckFuelCapacity = 100f;
    private const float TruckFuelPerCell = 1f;
    private const float DriverWalkSpeed = 2.2f;
    private const float MoneyPopupDuration = 1.4f;
    private const int AudioSampleRate = 22050;
    private const int MaxTruckCount = 5;
    private const int HireTruckCost = 300;
    private const float DayNightCycleDuration = 180f;
    private const float DioramaCameraPitch = 42f;
    private static readonly Vector3 DioramaCameraOffset = new(-11.5f, 15f, -11.5f);

    private readonly HashSet<Vector2Int> roadCells = new();
    private readonly Dictionary<Vector2Int, GameObject> roadVisuals = new();
    private readonly Dictionary<LocationType, LocationData> locations = new();
    private readonly List<Vector2Int> activePath = new();
    private readonly List<Vector3> driverRescuePath = new();
    private readonly List<Transform> truckWheels = new();
    private readonly List<Transform> truckFrontWheels = new();
    private readonly List<Light> truckHeadlights = new();
    private readonly List<Light> locationNightLights = new();
    private readonly List<Renderer> locationNightLightRenderers = new();
    private readonly List<RoadLanternData> roadLanterns = new();
    private readonly List<TruckAgent> truckAgents = new();
    private readonly List<ForestWorkerAmbient> forestWorkers = new();
    private readonly List<Vector3> forestWorkPoints = new();
    private readonly List<Transform> forestWorkTargetTrees = new();
    private readonly List<ForestTreeWobble> forestTreeWobbles = new();
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
    private GameObject driverObject;
    private Transform driverVisualRoot;
    private Transform driverBodyTransform;
    private Transform driverHeadTransform;
    private Transform driverCapTransform;
    private Transform driverLeftArmTransform;
    private Transform driverRightArmTransform;
    private Transform driverLeftLegTransform;
    private Transform driverRightLegTransform;
    private Transform driverFuelCanTransform;
    private Transform driverFlashlightTransform;
    private Light driverFlashlightLight;
    private Renderer driverFlashlightRenderer;
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
    private Light mainDirectionalLight;
    private GameObject selectedLocationLabelRoot;
    private TextMesh selectedLocationLabelText;
    private readonly List<TextMesh> selectedLocationLabelOutlines = new();
    private GameObject cargoTransferCrate;
    private Vector2Int truckCell;
    private Vector3 truckTargetWorld;
    private Vector3 truckSegmentStartWorld;
    private Vector3 truckSmoothedForward = Vector3.forward;
    private bool isTruckMoving;
    private bool isTruckInteracting;
    private bool isTruckWaitingForService;
    private bool isDriverRescueActive;
    private CargoSource truckCargoSource = CargoSource.None;
    private int truckCargoWood;
    private float productionTimer;
    private Vector3 cameraFocusPoint;
    private Vector3 cameraOffset;
    private Vector3 cameraTargetOffset;
    private Vector2 lastMousePosition;
    private Vector2 rightMousePressPosition;
    private Vector3 driverRescueTargetWorld;
    private int driverRescueWaypointIndex;
    private float truckSegmentProgress;
    private float truckSegmentDuration;
    private float truckWheelSpinAngle;
    private float truckSteerAngle;
    private float truckInteractionTimer;
    private float moneyPopupTimer;
    private float truckFuel = TruckFuelCapacity;
    private float dayNightCycleTimer = DayNightCycleDuration * 0.3f;
    private float currentStylizedDaylight = 1f;
    private float driverWalkAnimationTime;
    private float forestProductionProgress;
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
    private int selectedTruckNumber = 1;
    private int money;
    private int currentAssignedTripReward;
    private int moneyPopupAmount;
    private int gameSpeedMultiplier = 1;
    private int nextHireTruckNumber = 2;
    private TripType currentAssignedTrip = TripType.None;

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
    private TripPhase currentTripPhase = TripPhase.None;
    private RefuelPhase currentRefuelPhase = RefuelPhase.None;
    private DriverRescuePhase currentDriverRescuePhase = DriverRescuePhase.None;
    private TruckInteractionType activeTruckInteraction = TruckInteractionType.None;
    private TruckInteractionType queuedTruckInteraction = TruckInteractionType.None;
    private Quaternion truckInteractionTargetRotation;
    private Vector3 truckInteractionBuildingPoint;
    private LocationType? activeServiceLocation;
    private LocationType? queuedServiceLocation;
    private AudioClip uiSelectClip;
    private AudioClip uiPanelOpenClip;
    private AudioClip uiPanelCloseClip;
    private AudioClip ambientWindClip;
    private AudioClip dayBirdsClip;
    private AudioClip forestRustleClip;
    private AudioClip forestChopClip;
    private AudioClip nightWindClip;
    private AudioClip nightCricketsClip;
    private AudioClip gasStationHumClip;
    private AudioClip townHumClip;
    private AudioClip warehouseCreakClip;
    private AudioClip owlClip;
    private AudioClip lanternBuzzClip;
    private AudioClip truckIdleClip;
    private AudioClip truckRollClip;
    private AudioClip cargoPickupClip;
    private AudioClip cargoDropClip;
    private AudioClip routeAssignForestWarehouseClip;
    private AudioClip routeAssignWarehouseTownClip;
    private AudioClip routeAssignRefuelClip;
    private AudioClip forestLoadCueClip;
    private AudioClip warehouseUnloadCueClip;
    private AudioClip warehouseLoadCueClip;
    private AudioClip townUnloadCueClip;
    private AudioClip gasStationRefuelCueClip;
    private AudioClip parkingReturnCueClip;
    private AudioClip moneyRewardClip;

    private enum LocationType
    {
        Parking,
        GasStation,
        Forest,
        Warehouse,
        Town,
        Motel
    }

    private enum CargoSource
    {
        None,
        Forest,
        Warehouse
    }

    private enum TransportTask
    {
        None,
        ReturnToParking,
        PickUpAtForest,
        DeliverToWarehouse,
        PickUpAtWarehouse,
        DeliverToTown
    }

    private enum TruckInteractionType
    {
        None,
        LoadAtForest,
        UnloadAtWarehouse,
        LoadAtWarehouse,
        UnloadAtTown,
        RefuelAtGasStation
    }

    private enum TripType
    {
        None,
        ForestToWarehouse,
        WarehouseToTown
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
        ToTruck
    }

    private sealed class LocationData
    {
        public string Label;
        public Vector2Int Min;
        public Vector2Int Max;
        public Vector2Int Anchor;
        public Color BaseColor;
        public int WoodStored;
        public GameObject RootObject;
        public Renderer BaseRenderer;
        public readonly List<GameObject> StoredLogVisuals = new();

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
        public AudioSource TruckLoopAudioSource;
        public AudioSource TruckFxAudioSource;
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
        public CargoSource TruckCargoSource = CargoSource.None;
        public int TruckCargoWood;
        public Vector3 DriverRescueTargetWorld;
        public readonly List<Vector3> DriverRescuePath = new();
        public int DriverRescueWaypointIndex;
        public float TruckSegmentProgress;
        public float TruckSegmentDuration;
        public float TruckWheelSpinAngle;
        public float TruckSteerAngle;
        public float TruckInteractionTimer;
        public float TruckFuel = TruckFuelCapacity;
        public float DriverWalkAnimationTime;
        public int CurrentAssignedTripReward;
        public TripType CurrentAssignedTrip = TripType.None;
        public TripPhase CurrentTripPhase = TripPhase.None;
        public RefuelPhase CurrentRefuelPhase = RefuelPhase.None;
        public DriverRescuePhase CurrentDriverRescuePhase = DriverRescuePhase.None;
        public TruckInteractionType ActiveTruckInteraction = TruckInteractionType.None;
        public TruckInteractionType QueuedTruckInteraction = TruckInteractionType.None;
        public Quaternion TruckInteractionTargetRotation;
        public Vector3 TruckInteractionBuildingPoint;
        public LocationType? ActiveServiceLocation;
        public LocationType? QueuedServiceLocation;
        public int ParkingSlotIndex;
    }

    private void Awake()
    {
        SessionDebugLogger.StartNewSession($"{nameof(GameBootstrap)} on {gameObject.scene.name}");
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        gameSpeedMultiplier = 1;
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
        HandleHotkeys();
        HandleCameraInput();
        HandleRoadRemovalInput();
        HandleRoadPlacementInput();
        ProduceForestWood();
        UpdateDayNightCycle();
        UpdateSelectedLocationLabel();
        UpdateForestTreeWobbles();
        UpdateForestWorkers();
        for (int i = 0; i < truckAgents.Count; i++)
        {
            LoadTruckState(truckAgents[i]);
            UpdateTruckMovement();
            UpdateTruckInteraction();
            UpdateAssignedTrip();
            UpdateRefuelOrder();
            UpdateDriverRescue();
            UpdateDriverVisualAnimation();
            UpdateTruckAutoMode();
            UpdateAudio();

            if (!isTruckMoving &&
                !isTruckInteracting &&
                !isDriverRescueActive &&
                truckCell == locations[LocationType.Parking].Anchor)
            {
                Vector3 parkedPosition = GetParkingSlotWorldPosition(truckAgents[i].ParkingSlotIndex);
                truckObject.transform.position = parkedPosition;
                truckTargetWorld = parkedPosition;
                truckSegmentStartWorld = parkedPosition;
                truckObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            }

            SaveTruckState(truckAgents[i]);
        }

        UpdateMoneyPopup();
    }

    private void OnGUI()
    {
        DrawMoneyHud();
        DrawTimeHud();
        DrawSpeedHud();
        DrawTruckFleetHud();
        DrawCameraLegendHud();

        if (selectedLocation.HasValue && selectedLocation != LocationType.Parking)
        {
            DrawSelectedBuildingHud(selectedLocation.Value);
        }

        if (selectedLocation == LocationType.Parking)
        {
            DrawParkingHud();
            DrawAvailableTripsHud();
        }

        if (isTruckDetailsOpen)
        {
            DrawTruckDetailsHud();
        }
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
        SetupLocations();
        GenerateInitialRoadNetwork();
        GenerateTerrainHeights();
        ApplyTerrainHeightsToWorld();
        SetupGround();
        SetupGrid();
        RebuildRoadLanterns();
        PopulateMiscTrees();
        SetupForestWorkers();
        SetupSelectionVisuals();
        SetupTruck();
        SetupCargoTransferVisual();
        SetupAudio();
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
        keyLight.color = new Color(1f, 0.95f, 0.86f);
        keyLight.intensity = 1.15f;
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
        RenderSettings.ambientLight = new Color(0.78f, 0.82f, 0.88f);
    }

    private void UpdateDayNightCycle()
    {
        if (mainDirectionalLight == null || mainCamera == null)
        {
            return;
        }

        dayNightCycleTimer = Mathf.Repeat(dayNightCycleTimer + Time.deltaTime, DayNightCycleDuration);
        float normalizedTime = dayNightCycleTimer / DayNightCycleDuration;
        float sunArc = Mathf.Sin(normalizedTime * Mathf.PI * 2f - Mathf.PI * 0.5f);
        float daylight = Mathf.Clamp01((sunArc + 1f) * 0.5f);
        float stylizedDaylight = Mathf.SmoothStep(0.06f, 1f, daylight);
        currentStylizedDaylight = stylizedDaylight;

        float sunPitch = Mathf.Lerp(205f, 335f, normalizedTime);
        mainDirectionalLight.transform.rotation = Quaternion.Euler(sunPitch, -34f, 0f);
        mainDirectionalLight.intensity = Mathf.Lerp(0.1f, 1.18f, stylizedDaylight);
        mainDirectionalLight.color = Color.Lerp(
            new Color(0.34f, 0.41f, 0.68f),
            new Color(1f, 0.95f, 0.86f),
            stylizedDaylight);

        RenderSettings.ambientLight = Color.Lerp(
            new Color(0.055f, 0.07f, 0.12f),
            new Color(0.78f, 0.82f, 0.88f),
            stylizedDaylight);

        mainCamera.backgroundColor = Color.Lerp(
            new Color(0.012f, 0.02f, 0.055f),
            new Color(0.82f, 0.9f, 0.97f),
            stylizedDaylight);

        for (int i = 0; i < truckAgents.Count; i++)
        {
            LoadTruckState(truckAgents[i]);
            UpdateTruckHeadlights(stylizedDaylight);
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
                ApplyColor(groundTile, new Color(0.72f, 0.67f, 0.55f));
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
        cameraData.antialiasingQuality = AntialiasingQuality.High;

        GameObject volumeObject = new("DioramaVolume");
        volumeObject.transform.SetParent(worldRoot, false);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = profile;

        ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.postExposure.Override(0.04f);
        colorAdjustments.contrast.Override(8f);
        colorAdjustments.saturation.Override(10f);

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.96f);
        bloom.intensity.Override(0.08f);
        bloom.scatter.Override(0.58f);
        bloom.highQualityFiltering.Override(false);

        DepthOfField depthOfField = profile.Add<DepthOfField>(true);
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(12f);
        depthOfField.gaussianEnd.Override(20f);
        depthOfField.gaussianMaxRadius.Override(0.12f);
        depthOfField.highQualitySampling.Override(false);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.08f);
        vignette.smoothness.Override(0.48f);
        vignette.rounded.Override(false);
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

}
