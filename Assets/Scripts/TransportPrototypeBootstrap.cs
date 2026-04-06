using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TransportPrototypeBootstrap : MonoBehaviour
{
    private const int GridWidth = 20;
    private const int GridHeight = 20;
    private const float CellSize = 1f;
    private const float RoadHeight = 0.12f;
    private const float TruckCruiseSpeed = 1.9f;
    private const float ForestProductionInterval = 2.5f;
    private const float CameraPanSpeed = 9f;
    private const float CameraDragPanMultiplier = 0.035f;
    private const float CameraZoomSpeed = 8f;
    private const string TruckDisplayName = "Truck #1";
    private const float CameraMinHeight = 6f;
    private const float CameraMaxHeight = 18f;
    private const float CameraMinDistance = 6f;
    private const float CameraMaxDistance = 18f;
    private const float TruckSegmentStartLift = 0.24f;
    private const float TruckSuspensionBobAmount = 0.045f;
    private const float TruckWheelRadius = 0.12f;
    private const float TruckCargoInteractionDuration = 3f;
    private const float TruckFuelCapacity = 100f;
    private const float TruckFuelPerCell = 1f;
    private const float DriverWalkSpeed = 2.2f;
    private const float MoneyPopupDuration = 1.4f;
    private const int AudioSampleRate = 22050;
    private const float DayNightCycleDuration = 180f;
    private static readonly Rect MainHudRect = new Rect(12f, 12f, 280f, 212f);
    private static readonly Rect HelpHudRect = new Rect(12f, 232f, 380f, 152f);
    private static readonly Vector3 DioramaCameraRotation = new(42f, 45f, 0f);
    private static readonly Vector3 DioramaCameraOffset = new(-11.5f, 15f, -11.5f);

    private readonly HashSet<Vector2Int> roadCells = new();
    private readonly Dictionary<Vector2Int, GameObject> roadVisuals = new();
    private readonly Dictionary<LocationType, LocationData> locations = new();
    private readonly List<Vector2Int> activePath = new();
    private readonly List<Transform> truckWheels = new();
    private readonly List<Transform> truckFrontWheels = new();
    private readonly List<Light> truckHeadlights = new();
    private readonly List<Light> locationNightLights = new();
    private readonly List<Renderer> locationNightLightRenderers = new();

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
    private Transform roadsRoot;
    private AudioSource uiAudioSource;
    private AudioSource ambientAudioSource;
    private AudioSource forestAudioSource;
    private AudioSource townAudioSource;
    private AudioSource truckLoopAudioSource;
    private AudioSource truckFxAudioSource;
    private Light mainDirectionalLight;
    private GameObject parkingSelectionHighlight;
    private GameObject cargoTransferCrate;
    private Vector2Int truckCell;
    private Vector3 truckTargetWorld;
    private Vector3 truckSegmentStartWorld;
    private Vector3 truckSmoothedForward = Vector3.forward;
    private bool isTruckMoving;
    private bool isTruckInteracting;
    private bool isDriverRescueActive;
    private CargoSource truckCargoSource = CargoSource.None;
    private int truckCargoWood;
    private float productionTimer;
    private Vector3 cameraFocusPoint;
    private Vector3 cameraOffset;
    private Vector2 lastMousePosition;
    private Vector2 rightMousePressPosition;
    private Vector3 driverRescueTargetWorld;
    private float truckSegmentProgress;
    private float truckSegmentDuration;
    private float truckWheelSpinAngle;
    private float truckSteerAngle;
    private float truckInteractionTimer;
    private float moneyPopupTimer;
    private float truckFuel = TruckFuelCapacity;
    private float dayNightCycleTimer = DayNightCycleDuration * 0.3f;
    private float driverWalkAnimationTime;
    private LocationType? selectedLocation;
    private bool isTruckDetailsOpen;
    private bool isRightMouseDragging;
    private bool isTruckAutoModeEnabled;
    private int money;
    private int currentAssignedTripReward;
    private int moneyPopupAmount;
    private int gameSpeedMultiplier = 1;
    private TripType currentAssignedTrip = TripType.None;
    private TripPhase currentTripPhase = TripPhase.None;
    private RefuelPhase currentRefuelPhase = RefuelPhase.None;
    private DriverRescuePhase currentDriverRescuePhase = DriverRescuePhase.None;
    private TruckInteractionType activeTruckInteraction = TruckInteractionType.None;
    private Quaternion truckInteractionTargetRotation;
    private Vector3 truckInteractionBuildingPoint;
    private AudioClip uiSelectClip;
    private AudioClip uiPanelOpenClip;
    private AudioClip uiPanelCloseClip;
    private AudioClip ambientWindClip;
    private AudioClip forestRustleClip;
    private AudioClip townHumClip;
    private AudioClip truckIdleClip;
    private AudioClip truckRollClip;
    private AudioClip cargoPickupClip;
    private AudioClip cargoDropClip;
    private AudioClip moneyRewardClip;

    private enum LocationType
    {
        Parking,
        GasStation,
        Forest,
        Warehouse,
        Town
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

    private void Awake()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        gameSpeedMultiplier = 1;
        BuildPrototypeScene();
    }

    private void Update()
    {
        HandleHotkeys();
        HandleCameraInput();
        HandleRoadRemovalInput();
        HandleRoadPlacementInput();
        ProduceForestWood();
        UpdateDayNightCycle();
        UpdateTruckMovement();
        UpdateTruckInteraction();
        UpdateAssignedTrip();
        UpdateRefuelOrder();
        UpdateDriverRescue();
        UpdateDriverVisualAnimation();
        UpdateTruckAutoMode();
        UpdateMoneyPopup();
        UpdateAudio();
    }

    private void OnGUI()
    {
        DrawMoneyHud();
        GUI.Box(MainHudRect, "Transport Prototype");
        GUI.Label(new Rect(24, 42, 240, 24), "Mode: Road placement");
        GUI.Label(new Rect(24, 66, 240, 24), $"Parking: {locations[LocationType.Parking].Label}");
        GUI.Label(new Rect(24, 90, 240, 24), $"Forest logs: {locations[LocationType.Forest].WoodStored}");
        GUI.Label(new Rect(24, 114, 240, 24), $"Warehouse logs: {locations[LocationType.Warehouse].WoodStored}");
        GUI.Label(new Rect(24, 138, 240, 24), $"Town logs received: {locations[LocationType.Town].WoodStored}");
        GUI.Label(new Rect(24, 162, 240, 24), $"Speed: {gameSpeedMultiplier}x");
        GUI.Label(new Rect(24, 186, 240, 24), $"Time: {GetDayNightClockLabel()} ({GetTimeOfDayLabel()})");

        GUI.Box(HelpHudRect, "How To Play");
        GUI.Label(new Rect(24, 214, 340, 24), "Left click empty cells to place roads.");
        GUI.Label(new Rect(24, 238, 340, 24), "Right click road to remove, RMB drag to move camera.");
        GUI.Label(new Rect(24, 262, 340, 24), "F1/F2/F3 change speed, key 1 selects Truck #1.");
        GUI.Label(new Rect(24, 286, 340, 24), $"Truck fuel: {Mathf.CeilToInt(truckFuel)}/{Mathf.CeilToInt(TruckFuelCapacity)}");
        GUI.Label(new Rect(24, 310, 340, 24), $"Truck cargo: {truckCargoWood}/1 ({truckCargoSource})");
        GUI.Label(new Rect(24, 334, 340, 24), GetTruckStatusLabel());

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

        SetupCamera();
        SetupLighting();
        SetupGround();
        SetupDioramaPostProcessing();
        SetupGrid();
        SetupLocations();
        GenerateInitialRoadNetwork();
        SetupSelectionVisuals();
        SetupTruck();
        SetupDriver();
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
        mainCamera.transform.rotation = Quaternion.Euler(DioramaCameraRotation);
        mainCamera.fieldOfView = 30f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 120f;
        mainCamera.backgroundColor = new Color(0.82f, 0.9f, 0.97f);
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        cameraFocusPoint = new Vector3(GridWidth * 0.5f, 0f, GridHeight * 0.5f);
        cameraOffset = DioramaCameraOffset;
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
        keyLight.shadowStrength = 0.78f;

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
        float stylizedDaylight = Mathf.SmoothStep(0.08f, 1f, daylight);

        float sunPitch = Mathf.Lerp(205f, 335f, normalizedTime);
        mainDirectionalLight.transform.rotation = Quaternion.Euler(sunPitch, -34f, 0f);
        mainDirectionalLight.intensity = Mathf.Lerp(0.2f, 1.18f, stylizedDaylight);
        mainDirectionalLight.color = Color.Lerp(
            new Color(0.34f, 0.41f, 0.68f),
            new Color(1f, 0.95f, 0.86f),
            stylizedDaylight);

        RenderSettings.ambientLight = Color.Lerp(
            new Color(0.12f, 0.15f, 0.22f),
            new Color(0.78f, 0.82f, 0.88f),
            stylizedDaylight);

        mainCamera.backgroundColor = Color.Lerp(
            new Color(0.03f, 0.05f, 0.11f),
            new Color(0.82f, 0.9f, 0.97f),
            stylizedDaylight);

        UpdateTruckHeadlights(stylizedDaylight);
    }

    private void SetupGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(worldRoot, false);
        ground.transform.position = new Vector3(GridWidth / 2f, -0.01f, GridHeight / 2f);
        ground.transform.localScale = new Vector3((GridWidth - 2f) / 10f, 1f, (GridHeight - 2f) / 10f);
        ApplyColor(ground, new Color(0.79f, 0.74f, 0.61f));
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
            CreateGridLine(gridRoot.transform, lineMaterial, new Vector3(x, 0.01f, 0f), new Vector3(x, 0.01f, GridHeight));
        }

        for (int y = 0; y <= GridHeight; y++)
        {
            CreateGridLine(gridRoot.transform, lineMaterial, new Vector3(0f, 0.01f, y), new Vector3(GridWidth, 0.01f, y));
        }
    }

    private void SetupLocations()
    {
        CreateLocation(LocationType.Parking, "Parking", new Vector2Int(2, 2), new Vector2Int(4, 3), new Vector2Int(3, 3), new Color(0.46f, 0.46f, 0.52f));
        CreateLocation(LocationType.GasStation, "Gas Station", new Vector2Int(6, 1), new Vector2Int(7, 2), new Vector2Int(6, 3), new Color(0.84f, 0.68f, 0.26f));
        CreateLocation(LocationType.Forest, "Forest", new Vector2Int(3, 15), new Vector2Int(4, 16), new Vector2Int(4, 14), new Color(0.22f, 0.55f, 0.24f));
        CreateLocation(LocationType.Warehouse, "Warehouse", new Vector2Int(9, 9), new Vector2Int(10, 10), new Vector2Int(9, 8), new Color(0.7f, 0.52f, 0.3f));
        CreateLocation(LocationType.Town, "Town", new Vector2Int(14, 2), new Vector2Int(15, 3), new Vector2Int(13, 3), new Color(0.3f, 0.52f, 0.8f));
    }

    private void SetupTruck()
    {
        truckCell = locations[LocationType.Parking].Anchor;
        truckTargetWorld = GetTruckWorldPosition(truckCell);
        truckSegmentStartWorld = truckTargetWorld;

        truckObject = new GameObject("Truck");
        truckObject.transform.SetParent(worldRoot, false);
        truckVisualRoot = new GameObject("VisualRoot").transform;
        truckVisualRoot.SetParent(truckObject.transform, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(truckVisualRoot, false);
        body.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        body.transform.localScale = new Vector3(0.7f, 0.35f, 1f);
        ApplyColor(body, new Color(0.85f, 0.2f, 0.18f));
        truckBodyTransform = body.transform;

        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(truckVisualRoot, false);
        cabin.transform.localPosition = new Vector3(0f, 0.4f, 0.2f);
        cabin.transform.localScale = new Vector3(0.55f, 0.4f, 0.45f);
        ApplyColor(cabin, new Color(0.95f, 0.82f, 0.28f));
        truckCabinTransform = cabin.transform;

        CreateTruckHeadlightVisual(new Vector3(-0.18f, 0.39f, 0.46f), true);
        CreateTruckHeadlightVisual(new Vector3(0.18f, 0.39f, 0.46f), false);
        CreateTruckHeadlightBeam(new Vector3(-0.18f, 0.39f, 0.5f));
        CreateTruckHeadlightBeam(new Vector3(0.18f, 0.39f, 0.5f));

        Vector3[] wheelOffsets =
        {
            new(-0.28f, 0.08f, 0.32f),
            new(0.28f, 0.08f, 0.32f),
            new(-0.28f, 0.08f, -0.32f),
            new(0.28f, 0.08f, -0.32f)
        };

        for (int i = 0; i < wheelOffsets.Length; i++)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.transform.SetParent(truckVisualRoot, false);
            wheel.transform.localPosition = wheelOffsets[i];
            wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            wheel.transform.localScale = new Vector3(0.12f, 0.05f, 0.12f);
            ApplyColor(wheel, new Color(0.14f, 0.14f, 0.14f));
            truckWheels.Add(wheel.transform);
            if (i < 2)
            {
                truckFrontWheels.Add(wheel.transform);
            }
        }

        truckObject.transform.position = truckTargetWorld;
        truckObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        truckInteractionTargetRotation = truckObject.transform.rotation;
    }

    private void CreateTruckHeadlightVisual(Vector3 localPosition, bool isLeft)
    {
        GameObject headlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlight.transform.SetParent(truckVisualRoot, false);
        headlight.transform.localPosition = localPosition;
        headlight.transform.localScale = new Vector3(0.12f, 0.08f, 0.04f);
        ApplyColor(headlight, new Color(1f, 0.93f, 0.75f));

        Renderer rendererComponent = headlight.GetComponent<Renderer>();
        if (isLeft)
        {
            truckHeadlightLeftRenderer = rendererComponent;
        }
        else
        {
            truckHeadlightRightRenderer = rendererComponent;
        }
    }

    private void CreateTruckHeadlightBeam(Vector3 localPosition)
    {
        GameObject lightObject = new("Headlight");
        lightObject.transform.SetParent(truckVisualRoot, false);
        lightObject.transform.localPosition = localPosition;
        lightObject.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);

        Light headlight = lightObject.AddComponent<Light>();
        headlight.type = LightType.Spot;
        headlight.color = new Color(1f, 0.86f, 0.62f);
        headlight.intensity = 0f;
        headlight.range = 5.4f;
        headlight.spotAngle = 44f;
        headlight.innerSpotAngle = 22f;
        headlight.shadows = LightShadows.None;
        headlight.enabled = false;
        truckHeadlights.Add(headlight);
    }

    private void UpdateTruckHeadlights(float stylizedDaylight)
    {
        float darkness = 1f - stylizedDaylight;
        bool headlightsOn = darkness > 0.55f;
        float headlightIntensity = headlightsOn ? Mathf.Lerp(0.7f, 3.1f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
        Color lampColor = Color.Lerp(
            new Color(0.3f, 0.26f, 0.2f),
            new Color(1f, 0.94f, 0.78f),
            Mathf.Clamp01(headlightIntensity / 3.1f));

        foreach (Light headlight in truckHeadlights)
        {
            if (headlight == null)
            {
                continue;
            }

            headlight.enabled = headlightsOn;
            headlight.intensity = headlightIntensity;
        }

        if (truckHeadlightLeftRenderer != null)
        {
            truckHeadlightLeftRenderer.material.color = lampColor;
        }

        if (truckHeadlightRightRenderer != null)
        {
            truckHeadlightRightRenderer.material.color = lampColor;
        }

        UpdateLocationNightLights(stylizedDaylight);
        UpdateDriverFlashlight(stylizedDaylight);
    }

    private void UpdateLocationNightLights(float stylizedDaylight)
    {
        float darkness = 1f - stylizedDaylight;
        bool lightsOn = darkness > 0.5f;
        float lightIntensity = lightsOn ? Mathf.Lerp(0.18f, 1.15f, Mathf.InverseLerp(0.5f, 1f, darkness)) : 0f;
        Color lampColor = Color.Lerp(
            new Color(0.28f, 0.24f, 0.18f),
            new Color(1f, 0.9f, 0.72f),
            Mathf.Clamp01(lightIntensity / 1.15f));

        foreach (Light lightComponent in locationNightLights)
        {
            if (lightComponent == null)
            {
                continue;
            }

            lightComponent.enabled = lightsOn;
            lightComponent.intensity = lightIntensity;
        }

        foreach (Renderer rendererComponent in locationNightLightRenderers)
        {
            if (rendererComponent == null)
            {
                continue;
            }

            rendererComponent.material.color = lampColor;
        }
    }

    private void UpdateDriverFlashlight(float stylizedDaylight)
    {
        if (driverFlashlightLight == null)
        {
            return;
        }

        float darkness = 1f - stylizedDaylight;
        bool flashlightOn = isDriverRescueActive && driverObject != null && driverObject.activeSelf && darkness > 0.55f;
        float flashlightIntensity = flashlightOn ? Mathf.Lerp(0.65f, 2.2f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
        Color flashlightColor = Color.Lerp(
            new Color(0.24f, 0.22f, 0.18f),
            new Color(1f, 0.92f, 0.74f),
            Mathf.Clamp01(flashlightIntensity / 2.2f));

        driverFlashlightLight.enabled = flashlightOn;
        driverFlashlightLight.intensity = flashlightIntensity;
        driverFlashlightLight.color = flashlightColor;

        if (driverFlashlightRenderer != null)
        {
            driverFlashlightRenderer.material.color = flashlightColor;
        }
    }

    private void SetupDriver()
    {
        driverObject = new GameObject("Driver");
        driverObject.transform.SetParent(worldRoot, false);
        driverVisualRoot = new GameObject("DriverVisualRoot").transform;
        driverVisualRoot.SetParent(driverObject.transform, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(driverVisualRoot, false);
        body.transform.localPosition = new Vector3(0f, 0.38f, 0f);
        body.transform.localScale = new Vector3(0.22f, 0.34f, 0.22f);
        ApplyColor(body, new Color(0.22f, 0.44f, 0.88f));
        driverBodyTransform = body.transform;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(driverVisualRoot, false);
        head.transform.localPosition = new Vector3(0f, 0.88f, 0f);
        head.transform.localScale = new Vector3(0.24f, 0.24f, 0.24f);
        ApplyColor(head, new Color(0.96f, 0.82f, 0.68f));
        driverHeadTransform = head.transform;

        GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap.transform.SetParent(driverVisualRoot, false);
        cap.transform.localPosition = new Vector3(0f, 1.02f, 0f);
        cap.transform.localScale = new Vector3(0.26f, 0.08f, 0.26f);
        ApplyColor(cap, new Color(0.84f, 0.22f, 0.18f));
        driverCapTransform = cap.transform;

        driverLeftArmTransform = CreateDriverLimb("DriverLeftArm", new Vector3(-0.2f, 0.56f, 0f), new Vector3(0.09f, 0.34f, 0.09f), new Color(0.22f, 0.44f, 0.88f));
        driverRightArmTransform = CreateDriverLimb("DriverRightArm", new Vector3(0.2f, 0.56f, 0f), new Vector3(0.09f, 0.34f, 0.09f), new Color(0.22f, 0.44f, 0.88f));
        driverLeftLegTransform = CreateDriverLimb("DriverLeftLeg", new Vector3(-0.09f, 0.15f, 0f), new Vector3(0.1f, 0.42f, 0.1f), new Color(0.18f, 0.22f, 0.36f));
        driverRightLegTransform = CreateDriverLimb("DriverRightLeg", new Vector3(0.09f, 0.15f, 0f), new Vector3(0.1f, 0.42f, 0.1f), new Color(0.18f, 0.22f, 0.36f));

        GameObject fuelCan = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fuelCan.transform.SetParent(driverVisualRoot, false);
        fuelCan.transform.localPosition = new Vector3(0.18f, 0.42f, 0f);
        fuelCan.transform.localScale = new Vector3(0.14f, 0.2f, 0.1f);
        ApplyColor(fuelCan, new Color(0.9f, 0.76f, 0.18f));
        driverFuelCanTransform = fuelCan.transform;
        driverFuelCanTransform.gameObject.SetActive(false);

        GameObject flashlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flashlight.transform.SetParent(driverVisualRoot, false);
        flashlight.transform.localPosition = new Vector3(0.24f, 0.57f, 0.1f);
        flashlight.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        flashlight.transform.localScale = new Vector3(0.06f, 0.06f, 0.18f);
        ApplyColor(flashlight, new Color(0.24f, 0.24f, 0.26f));
        driverFlashlightTransform = flashlight.transform;
        driverFlashlightRenderer = flashlight.GetComponent<Renderer>();

        GameObject flashlightBeamObject = new("DriverFlashlight");
        flashlightBeamObject.transform.SetParent(driverFlashlightTransform, false);
        flashlightBeamObject.transform.localPosition = new Vector3(0f, 0f, 0.14f);
        flashlightBeamObject.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        driverFlashlightLight = flashlightBeamObject.AddComponent<Light>();
        driverFlashlightLight.type = LightType.Spot;
        driverFlashlightLight.color = new Color(1f, 0.88f, 0.66f);
        driverFlashlightLight.range = 4.2f;
        driverFlashlightLight.spotAngle = 40f;
        driverFlashlightLight.innerSpotAngle = 18f;
        driverFlashlightLight.shadows = LightShadows.None;
        driverFlashlightLight.intensity = 0f;
        driverFlashlightLight.enabled = false;

        driverObject.SetActive(false);
    }

    private Transform CreateDriverLimb(string name, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject limb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        limb.name = name;
        limb.transform.SetParent(driverVisualRoot, false);
        limb.transform.localPosition = localPosition;
        limb.transform.localScale = localScale;
        ApplyColor(limb, color);
        return limb.transform;
    }

    private void SetupCargoTransferVisual()
    {
        cargoTransferCrate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cargoTransferCrate.name = "CargoTransferCrate";
        cargoTransferCrate.transform.SetParent(worldRoot, false);
        cargoTransferCrate.transform.localScale = new Vector3(0.28f, 0.28f, 0.28f);
        cargoTransferCrate.GetComponent<Collider>().enabled = false;
        ApplyColor(cargoTransferCrate, new Color(0.78f, 0.58f, 0.28f));
        cargoTransferCrate.SetActive(false);
    }

    private void SetupAudio()
    {
        uiSelectClip = CreateUiPulseClip("UI_Select", 280f, 0.09f, 0.018f);
        uiPanelOpenClip = CreateUiPulseClip("UI_Open", 420f, 0.12f, 0.025f);
        uiPanelCloseClip = CreateUiPulseClip("UI_Close", 220f, 0.1f, 0.02f);
        ambientWindClip = CreateWindClip("Ambient_Wind", 6f, 0.022f);
        forestRustleClip = CreateRustleClip("Forest_Rustle", 5.5f, 0.03f);
        townHumClip = CreateTownHumClip("Town_Hum", 5f, 0.018f);
        truckIdleClip = CreateTruckIdleClip("Truck_Idle", 2.6f, 0.032f);
        truckRollClip = CreateTruckRollClip("Truck_Roll", 1.6f, 0.03f);
        cargoPickupClip = CreateCargoThunkClip("Cargo_Pickup", 0.42f, 0.06f, 0.05f);
        cargoDropClip = CreateCargoThunkClip("Cargo_Drop", 0.46f, 0.085f, 0.08f);
        moneyRewardClip = CreateMoneyRewardClip("Money_Reward", 0.6f, 0.08f);

        uiAudioSource = CreateAudioSource("UIAudio", null, false, 0.65f, 1f, false);
        ambientAudioSource = CreateAudioSource("AmbientWind", worldRoot, true, 0.24f, 0f, false);
        forestAudioSource = CreateAudioSource("ForestAmbience", locations[LocationType.Forest].RootObject.transform, true, 0.28f, 0.82f, false);
        townAudioSource = CreateAudioSource("TownAmbience", locations[LocationType.Town].RootObject.transform, true, 0.22f, 0.9f, false);
        truckLoopAudioSource = CreateAudioSource("TruckLoop", truckObject.transform, true, 0.18f, 0.65f, false);
        truckFxAudioSource = CreateAudioSource("TruckFX", truckObject.transform, false, 0.42f, 0.8f, false);

        ambientAudioSource.clip = ambientWindClip;
        ambientAudioSource.Play();

        forestAudioSource.clip = forestRustleClip;
        forestAudioSource.Play();

        townAudioSource.clip = townHumClip;
        townAudioSource.Play();

        truckLoopAudioSource.clip = truckIdleClip;
        truckLoopAudioSource.Play();
    }

    private void HandleHotkeys()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            SetGameSpeed(1);
        }
        else if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            SetGameSpeed(2);
        }
        else if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            SetGameSpeed(3);
        }

        if ((Keyboard.current.digit1Key != null && Keyboard.current.digit1Key.wasPressedThisFrame) ||
            (Keyboard.current.numpad1Key != null && Keyboard.current.numpad1Key.wasPressedThisFrame))
        {
            FocusTruck();
        }
    }

    private void SetGameSpeed(int multiplier)
    {
        gameSpeedMultiplier = Mathf.Clamp(multiplier, 1, 3);
        Time.timeScale = gameSpeedMultiplier;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        PlayUiSound(uiSelectClip, 0.8f);
    }

    private void FocusTruck()
    {
        selectedLocation = null;
        isTruckDetailsOpen = true;
        RefreshSelectionVisuals();
        PlayUiSound(uiPanelOpenClip, 0.9f);
    }

    private void HandleCameraInput()
    {
        if (mainCamera == null || Keyboard.current == null)
        {
            return;
        }

        Vector3 forward = Vector3.ProjectOnPlane(mainCamera.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(mainCamera.transform.right, Vector3.up).normalized;
        Vector3 pan = Vector3.zero;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            pan += forward;
        }

        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            pan -= forward;
        }

        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            pan += right;
        }

        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            pan -= right;
        }

        if (pan.sqrMagnitude > 0.0001f)
        {
            cameraFocusPoint += pan.normalized * (CameraPanSpeed * Time.deltaTime);
        }

        if (Mouse.current != null)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                lastMousePosition = mousePosition;
                rightMousePressPosition = mousePosition;
                isRightMouseDragging = false;
            }

            if (Mouse.current.rightButton.isPressed)
            {
                Vector2 mouseDelta = mousePosition - lastMousePosition;
                if (mouseDelta.sqrMagnitude > 0.01f)
                {
                    isRightMouseDragging = true;
                }

                if (isRightMouseDragging)
                {
                    cameraFocusPoint -= right * (mouseDelta.x * CameraDragPanMultiplier);
                    cameraFocusPoint -= forward * (mouseDelta.y * CameraDragPanMultiplier);
                }

                lastMousePosition = mousePosition;
            }

            float scroll = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                float currentDistance = cameraOffset.magnitude;
                float targetDistance = Mathf.Clamp(currentDistance - scroll * 0.01f * CameraZoomSpeed, CameraMinDistance, CameraMaxDistance);
                Vector3 nextOffset = cameraOffset.normalized * targetDistance;
                float clampedHeight = Mathf.Clamp(cameraFocusPoint.y + nextOffset.y, CameraMinHeight, CameraMaxHeight);
                if (Mathf.Abs(nextOffset.y) > 0.0001f)
                {
                    float heightScale = (clampedHeight - cameraFocusPoint.y) / nextOffset.y;
                    nextOffset *= heightScale;
                }

                cameraOffset = nextOffset;
            }
        }

        ClampCameraFocus();
        if (cameraOffset.sqrMagnitude < 0.0001f)
        {
            cameraOffset = new Vector3(-8f, 10f, -8f);
        }

        mainCamera.transform.position = cameraFocusPoint + cameraOffset;
        mainCamera.transform.rotation = Quaternion.Euler(DioramaCameraRotation);
    }

    private void HandleRoadRemovalInput()
    {
        if (mainCamera == null ||
            Mouse.current == null ||
            !Mouse.current.rightButton.wasReleasedThisFrame ||
            isRightMouseDragging)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if ((mousePosition - rightMousePressPosition).sqrMagnitude > 16f || IsPointerOverHud(mousePosition))
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Plane plane = new(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
        {
            return;
        }

        Vector2Int cell = WorldToCell(ray.GetPoint(distance));
        if (!roadCells.Contains(cell))
        {
            return;
        }

        selectedLocation = null;
        isTruckDetailsOpen = false;
        RefreshSelectionVisuals();
        RemoveRoad(cell);
    }

    private void HandleRoadPlacementInput()
    {
        if (mainCamera == null ||
            Mouse.current == null ||
            Mouse.current.rightButton.isPressed ||
            !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (IsPointerOverHud(mousePosition))
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Plane plane = new(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
        {
            return;
        }

        if (TryHandleTruckSelection(ray))
        {
            return;
        }

        Vector2Int cell = WorldToCell(ray.GetPoint(distance));
        if (TryHandleLocationSelection(cell))
        {
            return;
        }

        if (!IsInsideGrid(cell) || IsLocationCell(cell) || roadCells.Contains(cell))
        {
            selectedLocation = null;
            isTruckDetailsOpen = false;
            RefreshSelectionVisuals();
            return;
        }

        selectedLocation = null;
        isTruckDetailsOpen = false;
        RefreshSelectionVisuals();
        AddRoad(cell);
    }

    private bool TryHandleTruckSelection(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            return false;
        }

        if (truckObject == null || hit.transform == null || !hit.transform.IsChildOf(truckObject.transform))
        {
            return false;
        }

        selectedLocation = null;
        isTruckDetailsOpen = true;
        RefreshSelectionVisuals();
        PlayUiSound(uiPanelOpenClip, 0.9f);
        return true;
    }

    private void ProduceForestWood()
    {
        productionTimer += Time.deltaTime;
        if (productionTimer < ForestProductionInterval)
        {
            return;
        }

        productionTimer = 0f;
        locations[LocationType.Forest].WoodStored += 1;
    }

    private void UpdateTruckMovement()
    {
        if (isTruckInteracting || isDriverRescueActive)
        {
            UpdateTruckVisuals(0f, false);
            return;
        }

        if (!isTruckMoving || activePath.Count == 0)
        {
            UpdateTruckVisuals(0f, false);
            return;
        }

        if (truckSegmentDuration <= 0.0001f)
        {
            BeginNextTruckSegment(activePath[0]);
        }

        Vector3 segmentDirection = truckTargetWorld - truckSegmentStartWorld;
        float segmentDistance = segmentDirection.magnitude;
        if (segmentDistance <= 0.0001f)
        {
            CompleteTruckSegment();
            return;
        }

        truckSegmentProgress += Time.deltaTime / truckSegmentDuration;
        float easedProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(truckSegmentProgress));
        Vector3 currentPosition = Vector3.Lerp(truckSegmentStartWorld, truckTargetWorld, easedProgress);
        truckObject.transform.position = currentPosition;

        Vector3 desiredForward = segmentDirection.normalized;
        truckSmoothedForward = Vector3.Slerp(truckSmoothedForward, desiredForward, 6f * Time.deltaTime).normalized;
        if (truckSmoothedForward.sqrMagnitude > 0.0001f)
        {
            truckObject.transform.rotation = Quaternion.Slerp(
                truckObject.transform.rotation,
                Quaternion.LookRotation(truckSmoothedForward, Vector3.up),
                9f * Time.deltaTime);
        }

        float segmentSpeed = segmentDistance / Mathf.Max(truckSegmentDuration, 0.001f);
        UpdateTruckVisuals(segmentSpeed, true);

        if (truckSegmentProgress < 1f)
        {
            return;
        }

        CompleteTruckSegment();
    }

    private void UpdateAssignedTrip()
    {
        if (currentAssignedTrip == TripType.None || currentRefuelPhase != RefuelPhase.None || isDriverRescueActive || isTruckMoving || isTruckInteracting)
        {
            return;
        }

        switch (currentTripPhase)
        {
            case TripPhase.ToPickup:
            {
                LocationType pickupLocation = GetPickupLocation(currentAssignedTrip);
                if (truckCell != locations[pickupLocation].Anchor)
                {
                    StartMoveTo(locations[pickupLocation].Anchor);
                    return;
                }

                currentTripPhase = TripPhase.Loading;
                StartTruckInteraction(GetLoadInteraction(currentAssignedTrip), pickupLocation);
                return;
            }

            case TripPhase.Loading:
                currentTripPhase = TripPhase.ToDropoff;
                return;

            case TripPhase.ToDropoff:
            {
                LocationType dropoffLocation = GetDropoffLocation(currentAssignedTrip);
                if (truckCell != locations[dropoffLocation].Anchor)
                {
                    StartMoveTo(locations[dropoffLocation].Anchor);
                    return;
                }

                currentTripPhase = TripPhase.Unloading;
                StartTruckInteraction(GetUnloadInteraction(currentAssignedTrip), dropoffLocation);
                return;
            }

            case TripPhase.Unloading:
                currentTripPhase = TripPhase.ReturnToParking;
                return;

            case TripPhase.ReturnToParking:
                if (truckCell != locations[LocationType.Parking].Anchor)
                {
                    StartMoveTo(locations[LocationType.Parking].Anchor);
                    return;
                }

                AwardMoney(currentAssignedTripReward);
                currentAssignedTrip = TripType.None;
                currentTripPhase = TripPhase.None;
                currentAssignedTripReward = 0;
                return;
        }
    }

    private void UpdateRefuelOrder()
    {
        if (currentRefuelPhase == RefuelPhase.None || currentAssignedTrip != TripType.None || isDriverRescueActive || isTruckMoving || isTruckInteracting)
        {
            return;
        }

        switch (currentRefuelPhase)
        {
            case RefuelPhase.ToGasStation:
                if (truckCell != locations[LocationType.GasStation].Anchor)
                {
                    StartMoveTo(locations[LocationType.GasStation].Anchor);
                    return;
                }

                currentRefuelPhase = RefuelPhase.Refueling;
                StartTruckInteraction(TruckInteractionType.RefuelAtGasStation, LocationType.GasStation);
                return;

            case RefuelPhase.Refueling:
                currentRefuelPhase = RefuelPhase.ReturnToParking;
                return;

            case RefuelPhase.ReturnToParking:
                if (truckCell != locations[LocationType.Parking].Anchor)
                {
                    StartMoveTo(locations[LocationType.Parking].Anchor);
                    return;
                }

                currentRefuelPhase = RefuelPhase.None;
                return;
        }
    }

    private void UpdateDriverRescue()
    {
        if (!isDriverRescueActive || driverObject == null)
        {
            return;
        }

        Vector3 currentPosition = driverObject.transform.position;
        Vector3 targetPosition = driverRescueTargetWorld;
        Vector3 flatDirection = targetPosition - currentPosition;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 step = flatDirection.normalized * (DriverWalkSpeed * Time.deltaTime);
            if (step.sqrMagnitude >= flatDirection.sqrMagnitude)
            {
                currentPosition = targetPosition;
            }
            else
            {
                currentPosition += step;
            }

            driverObject.transform.position = currentPosition;
            driverObject.transform.rotation = Quaternion.Slerp(
                driverObject.transform.rotation,
                Quaternion.LookRotation(flatDirection.normalized, Vector3.up),
                10f * Time.deltaTime);
        }

        if ((driverObject.transform.position - targetPosition).sqrMagnitude > 0.001f)
        {
            return;
        }

        switch (currentDriverRescuePhase)
        {
            case DriverRescuePhase.ToGasStation:
                currentDriverRescuePhase = DriverRescuePhase.ToTruck;
                if (driverFuelCanTransform != null)
                {
                    driverFuelCanTransform.gameObject.SetActive(true);
                }

                driverRescueTargetWorld = GetDriverStandPointNearTruck();
                return;

            case DriverRescuePhase.ToTruck:
                truckFuel = TruckFuelCapacity;
                isDriverRescueActive = false;
                currentDriverRescuePhase = DriverRescuePhase.None;
                if (driverFuelCanTransform != null)
                {
                    driverFuelCanTransform.gameObject.SetActive(false);
                }

                driverObject.SetActive(false);
                driverWalkAnimationTime = 0f;
                if (activePath.Count > 0)
                {
                    isTruckMoving = true;
                    BeginNextTruckSegment(activePath[0]);
                }

                return;
        }
    }

    private void UpdateDriverVisualAnimation()
    {
        if (driverObject == null || driverVisualRoot == null)
        {
            return;
        }

        if (!driverObject.activeSelf)
        {
            driverWalkAnimationTime = 0f;
            ApplyDriverPose(0f, 0f);
            return;
        }

        Vector3 toTarget = driverRescueTargetWorld - driverObject.transform.position;
        toTarget.y = 0f;
        bool isWalking = isDriverRescueActive && toTarget.sqrMagnitude > 0.012f;
        if (isWalking)
        {
            driverWalkAnimationTime += Time.deltaTime * 8.2f;
        }
        else
        {
            driverWalkAnimationTime = Mathf.MoveTowards(driverWalkAnimationTime, 0f, Time.deltaTime * 6f);
        }

        float swing = isWalking ? Mathf.Sin(driverWalkAnimationTime) : 0f;
        float bob = isWalking ? Mathf.Abs(Mathf.Sin(driverWalkAnimationTime * 2f)) * 0.06f : 0f;
        ApplyDriverPose(swing, bob);
    }

    private void ApplyDriverPose(float swing, float bob)
    {
        driverVisualRoot.localPosition = new Vector3(0f, bob, 0f);
        driverVisualRoot.localRotation = Quaternion.Euler(0f, 0f, swing * 2.5f);

        if (driverBodyTransform != null)
        {
            driverBodyTransform.localRotation = Quaternion.Euler(swing * 4f, 0f, 0f);
        }

        if (driverHeadTransform != null)
        {
            driverHeadTransform.localRotation = Quaternion.Euler(-swing * 2f, 0f, 0f);
        }

        if (driverCapTransform != null)
        {
            driverCapTransform.localRotation = Quaternion.Euler(-swing * 1.5f, 0f, 0f);
        }

        if (driverLeftArmTransform != null)
        {
            driverLeftArmTransform.localRotation = Quaternion.Euler(swing * 28f, 0f, 0f);
        }

        if (driverRightArmTransform != null)
        {
            float carryOffset = driverFuelCanTransform != null && driverFuelCanTransform.gameObject.activeSelf ? 18f : 0f;
            driverRightArmTransform.localRotation = Quaternion.Euler(-swing * 28f - carryOffset, 0f, 0f);
        }

        if (driverLeftLegTransform != null)
        {
            driverLeftLegTransform.localRotation = Quaternion.Euler(-swing * 24f, 0f, 0f);
        }

        if (driverRightLegTransform != null)
        {
            driverRightLegTransform.localRotation = Quaternion.Euler(swing * 24f, 0f, 0f);
        }

        if (driverFuelCanTransform != null && driverFuelCanTransform.gameObject.activeSelf)
        {
            driverFuelCanTransform.localPosition = new Vector3(0.2f, 0.4f - bob * 0.2f, 0.04f);
            driverFuelCanTransform.localRotation = Quaternion.Euler(0f, 0f, -10f + swing * 6f);
        }
        else if (driverFuelCanTransform != null)
        {
            driverFuelCanTransform.localPosition = new Vector3(0.18f, 0.42f, 0f);
            driverFuelCanTransform.localRotation = Quaternion.identity;
        }

        if (driverFlashlightTransform != null)
        {
            driverFlashlightTransform.localPosition = new Vector3(0.24f, 0.57f - bob * 0.12f, 0.1f);
            driverFlashlightTransform.localRotation = Quaternion.Euler(16f + swing * 10f, swing * 5f, 0f);
        }
    }

    private void StartMoveTo(Vector2Int destination)
    {
        List<Vector2Int> path = FindPath(truckCell, destination);
        if (path == null || path.Count < 2)
        {
            return;
        }

        activePath.Clear();
        for (int i = 1; i < path.Count; i++)
        {
            activePath.Add(path[i]);
        }

        isTruckMoving = true;
        BeginNextTruckSegment(activePath[0]);
    }

    private bool HasPath(Vector2Int start, Vector2Int goal)
    {
        return FindPath(start, goal) != null;
    }

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        if (start == goal)
        {
            return new List<Vector2Int> { start };
        }

        LocationType? startLocation = GetContainingLocation(start);
        LocationType? goalLocation = GetContainingLocation(goal);

        Queue<Vector2Int> frontier = new();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        frontier.Enqueue(start);
        cameFrom[start] = start;

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            foreach (Vector2Int neighbor in GetNeighbors(current))
            {
                if (cameFrom.ContainsKey(neighbor) || !IsDriveableForPath(neighbor, startLocation, goalLocation))
                {
                    continue;
                }

                cameFrom[neighbor] = current;
                if (neighbor == goal)
                {
                    return ReconstructPath(cameFrom, start, goal);
                }

                frontier.Enqueue(neighbor);
            }
        }

        return null;
    }

    private static List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> path = new() { goal };
        Vector2Int current = goal;
        while (current != start)
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private IEnumerable<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        yield return new Vector2Int(cell.x + 1, cell.y);
        yield return new Vector2Int(cell.x - 1, cell.y);
        yield return new Vector2Int(cell.x, cell.y + 1);
        yield return new Vector2Int(cell.x, cell.y - 1);
    }

    private bool IsDriveable(Vector2Int cell)
    {
        return IsInsideGrid(cell) && (roadCells.Contains(cell) || IsAnchorCell(cell));
    }

    private bool IsDriveableForPath(Vector2Int cell, LocationType? startLocation, LocationType? goalLocation)
    {
        if (IsDriveable(cell))
        {
            return true;
        }

        LocationType? containingLocation = GetContainingLocation(cell);
        if (!containingLocation.HasValue)
        {
            return false;
        }

        return containingLocation == startLocation || containingLocation == goalLocation;
    }

    private bool IsAnchorCell(Vector2Int cell)
    {
        foreach (LocationData location in locations.Values)
        {
            if (location.Anchor == cell)
            {
                return true;
            }
        }

        return false;
    }

    private LocationType? GetContainingLocation(Vector2Int cell)
    {
        foreach (KeyValuePair<LocationType, LocationData> pair in locations)
        {
            if (pair.Value.Contains(cell) || pair.Value.Anchor == cell)
            {
                return pair.Key;
            }
        }

        return null;
    }

    private bool IsLocationCell(Vector2Int cell)
    {
        foreach (LocationData location in locations.Values)
        {
            if (location.Contains(cell) || location.Anchor == cell)
            {
                return true;
            }
        }

        return false;
    }

    private void AddRoad(Vector2Int cell)
    {
        if (roadCells.Contains(cell) || !IsInsideGrid(cell) || IsLocationCell(cell))
        {
            return;
        }

        roadCells.Add(cell);

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = $"Road_{cell.x}_{cell.y}";
        road.transform.SetParent(roadsRoot, false);
        road.transform.position = GetCellCenter(cell) + new Vector3(0f, RoadHeight, 0f);
        road.transform.localScale = new Vector3(1.04f, 0.18f, 1.04f);
        ApplyColor(road, new Color(0.18f, 0.19f, 0.21f));
        ConfigureStaticVisual(road);

        GameObject roadTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadTop.name = "RoadTop";
        roadTop.transform.SetParent(road.transform, false);
        roadTop.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        roadTop.transform.localScale = new Vector3(0.84f, 0.16f, 0.84f);
        ApplyColor(roadTop, new Color(0.76f, 0.71f, 0.58f));
        ConfigureStaticVisual(roadTop);
        roadVisuals[cell] = road;

        RefreshRoadVisual(cell);
        foreach (Vector2Int neighbor in GetNeighbors(cell))
        {
            if (roadVisuals.ContainsKey(neighbor))
            {
                RefreshRoadVisual(neighbor);
            }
        }
    }

    private void RemoveRoad(Vector2Int cell)
    {
        if (!roadCells.Remove(cell))
        {
            return;
        }

        if (roadVisuals.TryGetValue(cell, out GameObject road))
        {
            roadVisuals.Remove(cell);
            Destroy(road);
        }

        foreach (Vector2Int neighbor in GetNeighbors(cell))
        {
            if (roadVisuals.ContainsKey(neighbor))
            {
                RefreshRoadVisual(neighbor);
            }
        }
    }

    private void GenerateInitialRoadNetwork()
    {
        Vector2Int parkingExit = new Vector2Int(4, 4);
        Vector2Int gasStationApproach = new Vector2Int(5, 3);
        AddRoad(parkingExit);
        CreateStarterRoadPath(parkingExit, gasStationApproach);
        CreateStarterRoadPath(gasStationApproach, locations[LocationType.GasStation].Anchor);
        CreateStarterRoadPath(locations[LocationType.GasStation].Anchor, locations[LocationType.Warehouse].Anchor);
        CreateStarterRoadPath(locations[LocationType.Warehouse].Anchor, locations[LocationType.Forest].Anchor);
        CreateStarterRoadPath(locations[LocationType.Warehouse].Anchor, locations[LocationType.Town].Anchor);
    }

    private void CreateStarterRoadPath(Vector2Int start, Vector2Int end)
    {
        Vector2Int current = start;

        while (current.x != end.x)
        {
            current = new Vector2Int(current.x + (end.x > current.x ? 1 : -1), current.y);
            TryAddStarterRoadCell(current, start, end);
        }

        while (current.y != end.y)
        {
            current = new Vector2Int(current.x, current.y + (end.y > current.y ? 1 : -1));
            TryAddStarterRoadCell(current, start, end);
        }
    }

    private void TryAddStarterRoadCell(Vector2Int cell, Vector2Int start, Vector2Int end)
    {
        if (cell == start || cell == end)
        {
            return;
        }

        AddRoad(cell);
    }

    private void RefreshRoadVisual(Vector2Int cell)
    {
        if (!roadVisuals.TryGetValue(cell, out GameObject road))
        {
            return;
        }

        bool horizontal = ConnectsToRoadOrAnchor(cell, new Vector2Int(1, 0)) || ConnectsToRoadOrAnchor(cell, new Vector2Int(-1, 0));
        bool vertical = ConnectsToRoadOrAnchor(cell, new Vector2Int(0, 1)) || ConnectsToRoadOrAnchor(cell, new Vector2Int(0, -1));

        Vector3 scale = road.transform.localScale;
        scale.x = horizontal ? 1.12f : 0.82f;
        scale.z = vertical ? 1.12f : 0.82f;
        road.transform.localScale = scale;

        if (road.transform.childCount > 0)
        {
            Transform roadTop = road.transform.GetChild(0);
            Vector3 topScale = roadTop.localScale;
            topScale.x = horizontal ? 0.92f : 0.62f;
            topScale.z = vertical ? 0.92f : 0.62f;
            roadTop.localScale = topScale;
        }
    }

    private bool ConnectsToRoadOrAnchor(Vector2Int cell, Vector2Int offset)
    {
        Vector2Int neighbor = cell + offset;
        return roadCells.Contains(neighbor) || IsAnchorCell(neighbor);
    }

    private void CreateLocation(LocationType type, string label, Vector2Int min, Vector2Int max, Vector2Int anchor, Color baseColor)
    {
        LocationData data = new()
        {
            Label = label,
            Min = min,
            Max = max,
            Anchor = anchor
            ,
            BaseColor = baseColor
        };

        GameObject root = new(label);
        root.transform.SetParent(worldRoot, false);
        data.RootObject = root;

        Vector2Int size = new(max.x - min.x + 1, max.y - min.y + 1);
        Vector3 center = new Vector3((min.x + max.x + 1) * 0.5f, 0.35f, (min.y + max.y + 1) * 0.5f);

        GameObject baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseBlock.transform.SetParent(root.transform, false);
        baseBlock.transform.position = center;
        baseBlock.transform.localScale = new Vector3(size.x * 0.95f, 0.7f, size.y * 0.95f);
        ApplyColor(baseBlock, baseColor);
        data.BaseRenderer = baseBlock.GetComponent<Renderer>();

        if (type == LocationType.Parking)
        {
            CreateParkingDecoration(root.transform, center);
        }
        else if (type == LocationType.GasStation)
        {
            CreateGasStationDecoration(root.transform, center);
        }
        else if (type == LocationType.Forest)
        {
            CreateForestDecoration(root.transform, min, max);
        }
        else if (type == LocationType.Warehouse)
        {
            CreateWarehouseDecoration(root.transform, center);
        }
        else
        {
            CreateTownDecoration(root.transform, center);
        }

        CreateLocationNightLights(type, root.transform, center, size);

        GameObject anchorMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        anchorMarker.transform.SetParent(root.transform, false);
        anchorMarker.transform.position = GetCellCenter(anchor) + new Vector3(0f, 0.05f, 0f);
        anchorMarker.transform.localScale = new Vector3(0.22f, 0.02f, 0.22f);
        ApplyColor(anchorMarker, new Color(1f, 0.9f, 0.35f));

        GameObject labelObject = new($"{label}_Label");
        labelObject.transform.SetParent(root.transform, false);
        labelObject.transform.position = center + new Vector3(0f, 0.8f, 0f);
        labelObject.transform.rotation = Quaternion.Euler(60f, 45f, 0f);
        TextMesh textMesh = labelObject.AddComponent<TextMesh>();
        textMesh.text = data.Label;
        textMesh.characterSize = 0.22f;
        textMesh.fontSize = 36;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.color = Color.black;

        locations[type] = data;
    }

    private void CreateParkingDecoration(Transform parent, Vector3 center)
    {
        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        canopy.transform.SetParent(parent, false);
        canopy.transform.position = center + new Vector3(0f, 0.6f, -0.15f);
        canopy.transform.localScale = new Vector3(2.8f, 0.12f, 1.4f);
        ApplyColor(canopy, new Color(0.18f, 0.2f, 0.24f));

        Vector3[] postOffsets =
        {
            new(-1.15f, 0.28f, -0.55f),
            new(1.15f, 0.28f, -0.55f),
            new(-1.15f, 0.28f, 0.25f),
            new(1.15f, 0.28f, 0.25f)
        };

        foreach (Vector3 offset in postOffsets)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(parent, false);
            post.transform.position = center + offset;
            post.transform.localScale = new Vector3(0.12f, 0.56f, 0.12f);
            ApplyColor(post, new Color(0.3f, 0.32f, 0.36f));
        }

        CreateDriveway(parent, new Vector3(4.02f, 0.11f, 3.56f), GetCellCenter(new Vector2Int(4, 4)) + new Vector3(0f, 0.11f, 0f), 0.62f);
    }

    private void CreateForestDecoration(Transform parent, Vector2Int min, Vector2Int max)
    {
        Vector3[] treePositions =
        {
            GetCellCenter(min),
            GetCellCenter(new Vector2Int(max.x, min.y)),
            GetCellCenter(new Vector2Int(min.x, max.y)),
            GetCellCenter(max)
        };

        foreach (Vector3 position in treePositions)
        {
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(parent, false);
            trunk.transform.position = position + new Vector3(0f, 0.45f, 0f);
            trunk.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            ApplyColor(trunk, new Color(0.42f, 0.26f, 0.14f));

            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.transform.SetParent(parent, false);
            leaves.transform.position = position + new Vector3(0f, 0.95f, 0f);
            leaves.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            ApplyColor(leaves, new Color(0.14f, 0.5f, 0.2f));
        }
    }

    private void CreateGasStationDecoration(Transform parent, Vector3 center)
    {
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + new Vector3(0f, 0.72f, -0.18f);
        roof.transform.localScale = new Vector3(2.15f, 0.12f, 1.08f);
        ApplyColor(roof, new Color(0.95f, 0.3f, 0.22f));

        Vector3[] postOffsets =
        {
            new(-0.8f, 0.32f, -0.44f),
            new(0.8f, 0.32f, -0.44f),
            new(-0.8f, 0.32f, 0.08f),
            new(0.8f, 0.32f, 0.08f)
        };

        foreach (Vector3 offset in postOffsets)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(parent, false);
            post.transform.position = center + offset;
            post.transform.localScale = new Vector3(0.12f, 0.64f, 0.12f);
            ApplyColor(post, new Color(0.96f, 0.94f, 0.88f));
        }

        GameObject kiosk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kiosk.transform.SetParent(parent, false);
        kiosk.transform.position = center + new Vector3(0f, 0.36f, 0.38f);
        kiosk.transform.localScale = new Vector3(1.25f, 0.52f, 0.5f);
        ApplyColor(kiosk, new Color(0.98f, 0.92f, 0.78f));

        GameObject pump = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pump.transform.SetParent(parent, false);
        pump.transform.position = center + new Vector3(0f, 0.32f, -0.12f);
        pump.transform.localScale = new Vector3(0.24f, 0.42f, 0.24f);
        ApplyColor(pump, new Color(0.2f, 0.22f, 0.26f));

        CreateDriveway(parent, new Vector3(6.55f, 0.11f, 2.96f), GetCellCenter(new Vector2Int(5, 3)) + new Vector3(0f, 0.11f, 0f), 0.58f);
    }

    private void CreateDriveway(Transform parent, Vector3 worldStart, Vector3 worldEnd, float width)
    {
        GameObject driveway = GameObject.CreatePrimitive(PrimitiveType.Cube);
        driveway.name = "Driveway";
        driveway.transform.SetParent(parent, false);

        Vector3 delta = worldEnd - worldStart;
        float length = delta.magnitude;
        driveway.transform.position = worldStart + delta * 0.5f;
        driveway.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        driveway.transform.localScale = new Vector3(width, 0.1f, length);

        ApplyColor(driveway, new Color(0.2f, 0.21f, 0.23f));
        ConfigureStaticVisual(driveway);

        GameObject drivewayTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        drivewayTop.name = "DrivewayTop";
        drivewayTop.transform.SetParent(driveway.transform, false);
        drivewayTop.transform.localPosition = new Vector3(0f, 0.58f, 0f);
        drivewayTop.transform.localScale = new Vector3(0.72f, 0.18f, 0.88f);
        ApplyColor(drivewayTop, new Color(0.76f, 0.71f, 0.58f));
        ConfigureStaticVisual(drivewayTop);
    }

    private void CreateWarehouseDecoration(Transform parent, Vector3 center)
    {
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + new Vector3(0f, 0.47f, 0f);
        roof.transform.localScale = new Vector3(2.05f, 0.12f, 2.05f);
        ApplyColor(roof, new Color(0.88f, 0.24f, 0.2f));
    }

    private void CreateTownDecoration(Transform parent, Vector3 center)
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.transform.SetParent(parent, false);
            house.transform.position = center + new Vector3(-0.3f + i * 0.6f, 0.4f, 0f);
            house.transform.localScale = new Vector3(0.45f, 0.5f, 0.45f);
            ApplyColor(house, new Color(0.92f, 0.84f, 0.66f));
        }
    }

    private void CreateLocationNightLights(LocationType type, Transform parent, Vector3 center, Vector2Int size)
    {
        if (type == LocationType.Forest)
        {
            CreateLocationNightLight(parent, center + new Vector3(0f, 1.15f, -0.95f));
            return;
        }

        float xOffset = Mathf.Max(0.45f, size.x * 0.28f);
        float zOffset = Mathf.Max(0.38f, size.y * 0.28f);
        CreateLocationNightLight(parent, center + new Vector3(-xOffset, 0.92f, -zOffset));
        CreateLocationNightLight(parent, center + new Vector3(xOffset, 0.92f, -zOffset));

        if (type == LocationType.Town)
        {
            CreateLocationNightLight(parent, center + new Vector3(0f, 0.86f, zOffset));
        }
    }

    private void CreateLocationNightLight(Transform parent, Vector3 localPosition)
    {
        GameObject lampVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampVisual.transform.SetParent(parent, false);
        lampVisual.transform.localPosition = localPosition;
        lampVisual.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
        ApplyColor(lampVisual, new Color(0.28f, 0.24f, 0.18f));
        ConfigureStaticVisual(lampVisual);
        locationNightLightRenderers.Add(lampVisual.GetComponent<Renderer>());

        GameObject lightObject = new("NightLamp");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition + new Vector3(0f, 0.06f, 0f);

        Light lamp = lightObject.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, 0.9f, 0.72f);
        lamp.range = 3.2f;
        lamp.intensity = 0f;
        lamp.shadows = LightShadows.None;
        lamp.enabled = false;
        locationNightLights.Add(lamp);
    }

    private void CreateGridLine(Transform parent, Material lineMaterial, Vector3 start, Vector3 end)
    {
        GameObject lineObject = new($"GridLine_{start.x}_{start.z}_{end.x}_{end.z}");
        lineObject.transform.SetParent(parent, false);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.widthMultiplier = 0.03f;
        lineRenderer.material = lineMaterial;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }

    private Vector3 GetCellCenter(Vector2Int cell)
    {
        return new Vector3(cell.x + 0.5f, 0f, cell.y + 0.5f);
    }

    private Vector3 GetTruckWorldPosition(Vector2Int cell)
    {
        return GetCellCenter(cell) + new Vector3(0f, TruckSegmentStartLift, 0f);
    }

    private static Vector2Int WorldToCell(Vector3 point)
    {
        return new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.z));
    }

    private static void ApplyColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = new(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.14f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        renderer.material = material;
    }

    private static void ApplyUnlitColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new(shader)
        {
            color = color
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        renderer.material = material;
    }

    private static void ConfigureStaticVisual(GameObject target)
    {
        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private bool IsInsideGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < GridWidth && cell.y >= 0 && cell.y < GridHeight;
    }

    private void SetupSelectionVisuals()
    {
        parkingSelectionHighlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        parkingSelectionHighlight.name = "ParkingSelectionHighlight";
        parkingSelectionHighlight.transform.SetParent(worldRoot, false);
        parkingSelectionHighlight.GetComponent<Collider>().enabled = false;
        ApplyColor(parkingSelectionHighlight, new Color(1f, 0.86f, 0.28f));
        parkingSelectionHighlight.SetActive(false);
    }

    private void ClampCameraFocus()
    {
        cameraFocusPoint.x = Mathf.Clamp(cameraFocusPoint.x, -2f, GridWidth + 2f);
        cameraFocusPoint.y = 0f;
        cameraFocusPoint.z = Mathf.Clamp(cameraFocusPoint.z, -2f, GridHeight + 2f);
    }

    private void BeginNextTruckSegment(Vector2Int nextCell)
    {
        truckSegmentStartWorld = truckObject.transform.position;
        truckTargetWorld = GetTruckWorldPosition(nextCell);
        truckSegmentProgress = 0f;
        float distance = Vector3.Distance(truckSegmentStartWorld, truckTargetWorld);
        truckSegmentDuration = Mathf.Max(0.38f, distance / TruckCruiseSpeed);
    }

    private void CompleteTruckSegment()
    {
        truckObject.transform.position = truckTargetWorld;
        truckCell = activePath[0];
        truckFuel = Mathf.Max(0f, truckFuel - TruckFuelPerCell);
        activePath.RemoveAt(0);

        if (truckFuel <= 0f)
        {
            isTruckMoving = false;
            truckSegmentDuration = 0f;
            truckSegmentProgress = 0f;
            UpdateTruckVisuals(0f, false);

            if (!isDriverRescueActive)
            {
                StartDriverRescue();
            }

            return;
        }

        if (activePath.Count == 0)
        {
            isTruckMoving = false;
            truckSegmentDuration = 0f;
            truckSegmentProgress = 0f;
            UpdateTruckVisuals(0f, false);
            return;
        }

        BeginNextTruckSegment(activePath[0]);
    }

    private void StartDriverRescue()
    {
        if (driverObject == null)
        {
            return;
        }

        isDriverRescueActive = true;
        currentDriverRescuePhase = DriverRescuePhase.ToGasStation;
        driverObject.SetActive(true);
        if (driverFuelCanTransform != null)
        {
            driverFuelCanTransform.gameObject.SetActive(false);
        }

        driverObject.transform.position = GetDriverStandPointNearTruck();
        driverObject.transform.rotation = truckObject.transform.rotation;
        driverWalkAnimationTime = 0f;
        ApplyDriverPose(0f, 0f);
        driverRescueTargetWorld = GetDriverStandPointNearLocation(LocationType.GasStation);
        PlayUiSound(uiSelectClip, 0.9f);
    }

    private Vector3 GetDriverStandPointNearTruck()
    {
        Vector3 truckPosition = truckObject.transform.position;
        return new Vector3(truckPosition.x + 0.32f, 0f, truckPosition.z - 0.32f);
    }

    private Vector3 GetDriverStandPointNearLocation(LocationType locationType)
    {
        Vector3 center = GetLocationCenter(locationType);
        return new Vector3(center.x - 0.45f, 0f, center.z + 0.45f);
    }

    private void UpdateTruckVisuals(float speed, bool moving)
    {
        if (truckVisualRoot == null)
        {
            return;
        }

        float normalizedSpeed = Mathf.Clamp01(speed / TruckCruiseSpeed);
        float idleServiceBob = isTruckInteracting ? Mathf.Sin(Time.time * 8f) * 0.02f : 0f;
        float bob = moving ? Mathf.Sin(Time.time * 10f) * TruckSuspensionBobAmount * normalizedSpeed : idleServiceBob;
        float pitch = moving ? -Mathf.Sin(Mathf.Clamp01(truckSegmentProgress) * Mathf.PI) * 2.2f * normalizedSpeed : 0f;

        truckVisualRoot.localPosition = new Vector3(0f, bob, 0f);

        float targetSteer = 0f;
        if (moving && activePath.Count > 1)
        {
            Vector3 currentDir = (GetTruckWorldPosition(activePath[0]) - truckObject.transform.position).normalized;
            Vector3 nextDir = (GetTruckWorldPosition(activePath[1]) - GetTruckWorldPosition(activePath[0])).normalized;
            targetSteer = Mathf.Clamp(Vector3.SignedAngle(currentDir, nextDir, Vector3.up), -18f, 18f);
        }

        truckSteerAngle = Mathf.Lerp(truckSteerAngle, targetSteer, 8f * Time.deltaTime);
        float roll = moving ? -truckSteerAngle * 0.18f * normalizedSpeed : 0f;

        truckBodyTransform.localRotation = Quaternion.Euler(pitch, 0f, roll);
        truckCabinTransform.localRotation = Quaternion.Euler(pitch * 0.6f, 0f, roll * 0.55f);
        truckWheelSpinAngle += moving ? speed / Mathf.Max(TruckWheelRadius, 0.01f) * Mathf.Rad2Deg * Time.deltaTime : 0f;

        foreach (Transform wheel in truckWheels)
        {
            bool isFrontWheel = truckFrontWheels.Contains(wheel);
            float steer = isFrontWheel ? truckSteerAngle : 0f;
            wheel.localRotation = Quaternion.Euler(90f + truckWheelSpinAngle, steer, 0f);
        }
    }

    private void UpdateTruckInteraction()
    {
        if (!isTruckInteracting)
        {
            if (cargoTransferCrate != null && cargoTransferCrate.activeSelf)
            {
                cargoTransferCrate.SetActive(false);
            }

            return;
        }

        truckObject.transform.rotation = Quaternion.Slerp(
            truckObject.transform.rotation,
            truckInteractionTargetRotation,
            7f * Time.deltaTime);

        truckInteractionTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(truckInteractionTimer / TruckCargoInteractionDuration);
        UpdateCargoTransferVisual(progress);
        UpdateTruckVisuals(0f, false);

        if (truckInteractionTimer < TruckCargoInteractionDuration)
        {
            return;
        }

        CompleteTruckInteraction();
    }

    private void StartTruckInteraction(TruckInteractionType interactionType, LocationType locationType)
    {
        if (isTruckInteracting)
        {
            return;
        }

        isTruckMoving = false;
        isTruckInteracting = true;
        activeTruckInteraction = interactionType;
        truckInteractionTimer = 0f;

        Vector3 buildingCenter = GetLocationCenter(locationType);
        Vector3 directionToBuilding = buildingCenter - truckObject.transform.position;
        directionToBuilding.y = 0f;
        if (directionToBuilding.sqrMagnitude < 0.0001f)
        {
            directionToBuilding = Vector3.forward;
        }

        truckInteractionTargetRotation = Quaternion.LookRotation(-directionToBuilding.normalized, Vector3.up);
        truckInteractionBuildingPoint = buildingCenter + directionToBuilding.normalized * -0.2f + Vector3.up * 0.3f;

        bool isCargoTransfer = interactionType != TruckInteractionType.RefuelAtGasStation;
        if (cargoTransferCrate != null)
        {
            cargoTransferCrate.SetActive(isCargoTransfer);
        }

        bool loading = interactionType == TruckInteractionType.LoadAtForest || interactionType == TruckInteractionType.LoadAtWarehouse;
        if (isCargoTransfer)
        {
            PlayTruckFx(loading ? cargoPickupClip : cargoDropClip, 0.8f);
        }
        else
        {
            PlayTruckFx(truckIdleClip, 0.55f);
        }
    }

    private void CompleteTruckInteraction()
    {
        TruckInteractionType completedInteraction = activeTruckInteraction;
        bool completedLoad = activeTruckInteraction == TruckInteractionType.LoadAtForest || activeTruckInteraction == TruckInteractionType.LoadAtWarehouse;

        switch (activeTruckInteraction)
        {
            case TruckInteractionType.LoadAtForest:
                locations[LocationType.Forest].WoodStored -= 1;
                truckCargoWood = 1;
                truckCargoSource = CargoSource.Forest;
                break;

            case TruckInteractionType.UnloadAtWarehouse:
                locations[LocationType.Warehouse].WoodStored += truckCargoWood;
                truckCargoWood = 0;
                truckCargoSource = CargoSource.None;
                break;

            case TruckInteractionType.LoadAtWarehouse:
                locations[LocationType.Warehouse].WoodStored -= 1;
                truckCargoWood = 1;
                truckCargoSource = CargoSource.Warehouse;
                break;

            case TruckInteractionType.UnloadAtTown:
                locations[LocationType.Town].WoodStored += truckCargoWood;
                truckCargoWood = 0;
                truckCargoSource = CargoSource.None;
                break;

            case TruckInteractionType.RefuelAtGasStation:
                truckFuel = TruckFuelCapacity;
                break;
        }

        isTruckInteracting = false;
        activeTruckInteraction = TruckInteractionType.None;
        truckInteractionTimer = 0f;
        if (cargoTransferCrate != null)
        {
            cargoTransferCrate.SetActive(false);
        }

        if (completedInteraction == TruckInteractionType.RefuelAtGasStation)
        {
            PlayTruckFx(uiPanelOpenClip, 0.55f);
        }
        else
        {
            PlayTruckFx(completedLoad ? cargoDropClip : cargoPickupClip, 0.55f);
        }
    }

    private void UpdateCargoTransferVisual(float progress)
    {
        if (cargoTransferCrate == null || !cargoTransferCrate.activeSelf)
        {
            return;
        }

        Vector3 truckRearPoint = truckObject.transform.position - truckObject.transform.forward * 0.52f + Vector3.up * 0.18f;
        bool loadingIntoTruck = activeTruckInteraction == TruckInteractionType.LoadAtForest || activeTruckInteraction == TruckInteractionType.LoadAtWarehouse;
        Vector3 from = loadingIntoTruck ? truckInteractionBuildingPoint : truckRearPoint;
        Vector3 to = loadingIntoTruck ? truckRearPoint : truckInteractionBuildingPoint;

        float arc = Mathf.Sin(progress * Mathf.PI) * 0.45f;
        cargoTransferCrate.transform.position = Vector3.Lerp(from, to, progress) + Vector3.up * arc;
        cargoTransferCrate.transform.rotation = Quaternion.Euler(0f, Time.time * 140f, 0f);
    }

    private Vector3 GetLocationCenter(LocationType locationType)
    {
        LocationData location = locations[locationType];
        return new Vector3((location.Min.x + location.Max.x + 1) * 0.5f, 0f, (location.Min.y + location.Max.y + 1) * 0.5f);
    }

    private string GetTruckStatusLabel()
    {
        if (isTruckInteracting)
        {
            return activeTruckInteraction switch
            {
                TruckInteractionType.LoadAtForest => "Loading at Forest...",
                TruckInteractionType.UnloadAtWarehouse => "Unloading at Warehouse...",
                TruckInteractionType.LoadAtWarehouse => "Loading at Warehouse...",
                TruckInteractionType.UnloadAtTown => "Unloading at Town...",
                TruckInteractionType.RefuelAtGasStation => "Refueling at Gas Station...",
                _ => "Truck servicing cargo..."
            };
        }

        if (isTruckMoving)
        {
            return "Truck is moving.";
        }

        if (currentAssignedTrip != TripType.None)
        {
            return $"Assigned route: {GetTripTitle(currentAssignedTrip)}";
        }

        if (isDriverRescueActive)
        {
            return currentDriverRescuePhase == DriverRescuePhase.ToGasStation
                ? "Out of fuel. Driver is walking to Gas Station."
                : "Driver is bringing fuel back to the truck.";
        }

        if (currentRefuelPhase != RefuelPhase.None)
        {
            return "Refuel order in progress.";
        }

        if (isTruckAutoModeEnabled)
        {
            return "Auto mode is waiting for the next task.";
        }

        return "Truck is awaiting manual orders.";
    }

    private bool TryHandleLocationSelection(Vector2Int cell)
    {
        foreach (KeyValuePair<LocationType, LocationData> pair in locations)
        {
            if (!pair.Value.Contains(cell))
            {
                continue;
            }

            selectedLocation = pair.Key;
            isTruckDetailsOpen = false;
            RefreshSelectionVisuals();
            PlayUiSound(uiSelectClip, 0.9f);
            return true;
        }

        return false;
    }

    private void RefreshSelectionVisuals()
    {
        if (parkingSelectionHighlight == null)
        {
            return;
        }

        if (selectedLocation != LocationType.Parking)
        {
            parkingSelectionHighlight.SetActive(false);
            return;
        }

        LocationData parking = locations[LocationType.Parking];
        Vector3 center = new Vector3((parking.Min.x + parking.Max.x + 1) * 0.5f, 0.03f, (parking.Min.y + parking.Max.y + 1) * 0.5f);
        Vector3 size = new Vector3(parking.Max.x - parking.Min.x + 1.16f, 0.08f, parking.Max.y - parking.Min.y + 1.16f);
        parkingSelectionHighlight.transform.position = center;
        parkingSelectionHighlight.transform.localScale = size;
        parkingSelectionHighlight.SetActive(true);
    }

    private void DrawParkingHud()
    {
        Rect panelRect = GetParkingHudRect();
        GUI.Box(panelRect, "Parking HUD");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 30, 250, 22), "Selected building: Parking");
        int trucksInsideCount = IsTruckInsideParking() ? 1 : 0;
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 54, 250, 22), $"Trucks inside parking: {trucksInsideCount}/1");

        if (trucksInsideCount > 0)
        {
            GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 82, 250, 22), "Click the truck icon to inspect it:");

            Rect iconRect = new Rect(panelRect.x + 16, panelRect.y + 110, 76, 60);
            bool iconPressed = GUI.Button(iconRect, "TRUCK");

            if (iconPressed)
            {
                isTruckDetailsOpen = !isTruckDetailsOpen;
                PlayUiSound(isTruckDetailsOpen ? uiPanelOpenClip : uiPanelCloseClip, 0.9f);
            }

            GUI.Label(new Rect(panelRect.x + 106, panelRect.y + 114, 150, 22), TruckDisplayName);
            GUI.Label(new Rect(panelRect.x + 106, panelRect.y + 136, 150, 22), "Status: Parked");
            GUI.Label(new Rect(panelRect.x + 106, panelRect.y + 158, 150, 22), "Open detail card");
        }
        else
        {
            GUI.Box(new Rect(panelRect.x + 12, panelRect.y + 82, 252, 86), "No trucks inside");
            GUI.Label(new Rect(panelRect.x + 24, panelRect.y + 114, 220, 22), "The truck is currently out on route.");
            isTruckDetailsOpen = false;
        }
    }

    private void DrawAvailableTripsHud()
    {
        Rect panelRect = GetAvailableTripsHudRect();
        GUI.Box(panelRect, "Available Routes");

        List<TripOption> trips = GetAvailableTrips();
        if (trips.Count == 0)
        {
            GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 34, 240, 22), "No routes available right now.");
            GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 58, 240, 22), "Routes appear when cargo and roads exist.");
            return;
        }

        float y = panelRect.y + 32f;
        int shown = 0;
        foreach (TripOption trip in trips)
        {
            if (shown >= 5)
            {
                break;
            }

            GUI.Box(new Rect(panelRect.x + 10, y, panelRect.width - 20, 52), string.Empty);
            GUI.Label(new Rect(panelRect.x + 18, y + 8, panelRect.width - 36, 20), trip.Title);
            GUI.Label(new Rect(panelRect.x + 18, y + 26, panelRect.width - 120, 18), trip.Description);
            GUI.Label(new Rect(panelRect.x + panelRect.width - 88, y + 26, 60, 18), $"${trip.Reward}");
            y += 58f;
            shown++;
        }
    }

    private void DrawTruckDetailsHud()
    {
        Rect panelRect = GetTruckDetailsHudRect();
        GUI.Box(panelRect, "Truck HUD");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 30, 220, 22), $"Truck: {TruckDisplayName}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 56, 220, 22), $"State: {GetTruckDetailStatus()}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 82, 220, 22), $"Fuel: {Mathf.CeilToInt(truckFuel)}/{Mathf.CeilToInt(TruckFuelCapacity)}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 108, 220, 22), $"Cargo: {truckCargoWood}/1 ({truckCargoSource})");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 134, 220, 22), $"Grid cell: {truckCell.x}, {truckCell.y}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 160, 240, 22), $"Assigned route: {GetTripTitle(currentAssignedTrip)}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 182, 240, 22), $"Trip payout: ${currentAssignedTripReward}");
        GUI.Label(new Rect(panelRect.x + 12, panelRect.y + 204, 240, 22), isDriverRescueActive ? "Driver: On foot fuel rescue" : "Driver: In truck");
        if (GUI.Button(new Rect(panelRect.x + 12, panelRect.y + 230, panelRect.width - 24, 26), isTruckAutoModeEnabled ? "Auto Mode: ON" : "Auto Mode: OFF"))
        {
            isTruckAutoModeEnabled = !isTruckAutoModeEnabled;
            PlayUiSound(uiSelectClip, 0.9f);
        }

        List<TripOption> trips = GetAvailableTrips();
        bool truckAvailable = currentAssignedTrip == TripType.None && currentRefuelPhase == RefuelPhase.None && !isTruckMoving && !isTruckInteracting && IsTruckInsideParking();
        float y = panelRect.y + 266f;

        GUI.enabled = truckAvailable;
        if (GUI.Button(new Rect(panelRect.x + 12, y, panelRect.width - 24, 26), "Refuel At Gas Station"))
        {
            StartRefuelOrder();
        }

        GUI.enabled = true;
        y += 32f;

        GUI.Label(new Rect(panelRect.x + 12, y, 220, 20), "Assign route:");
        y += 24f;

        if (trips.Count == 0)
        {
            GUI.Label(new Rect(panelRect.x + 12, y, 220, 20), "No trips available.");
            y += 26f;
        }
        else
        {
            foreach (TripOption trip in trips)
            {
                GUI.enabled = truckAvailable;
                if (GUI.Button(new Rect(panelRect.x + 12, y, panelRect.width - 24, 26), $"{trip.Title}  ${trip.Reward}"))
                {
                    AssignTrip(trip);
                }

                GUI.enabled = true;
                y += 30f;
            }
        }

        if (GUI.Button(new Rect(panelRect.x + 12, panelRect.y + panelRect.height - 32, 120, 24), "Close"))
        {
            isTruckDetailsOpen = false;
            PlayUiSound(uiPanelCloseClip, 0.8f);
        }
    }

    private string GetTruckDetailStatus()
    {
        if (isTruckInteracting)
        {
            return "Servicing cargo";
        }

        if (isTruckMoving)
        {
            return "Moving";
        }

        if (isDriverRescueActive)
        {
            return currentDriverRescuePhase == DriverRescuePhase.ToGasStation ? "Driver fetching fuel" : "Driver returning with fuel";
        }

        if (currentRefuelPhase != RefuelPhase.None)
        {
            return currentRefuelPhase == RefuelPhase.ReturnToParking ? "Returning from refuel" : "Refuel order";
        }

        if (currentAssignedTrip != TripType.None)
        {
            return $"Queued: {GetTripTitle(currentAssignedTrip)}";
        }

        return IsTruckInsideParking() ? "Parked in parking" : "Idle in world";
    }

    private string GetTimeOfDayLabel()
    {
        float normalizedTime = dayNightCycleTimer / DayNightCycleDuration;
        if (normalizedTime < 0.25f)
        {
            return "Night";
        }

        if (normalizedTime < 0.5f)
        {
            return "Morning";
        }

        if (normalizedTime < 0.75f)
        {
            return "Day";
        }

        return "Evening";
    }

    private string GetDayNightClockLabel()
    {
        float normalizedTime = dayNightCycleTimer / DayNightCycleDuration;
        int totalMinutes = Mathf.FloorToInt(normalizedTime * 24f * 60f);
        int hours = (totalMinutes / 60) % 24;
        int minutes = totalMinutes % 60;
        return $"{hours:00}:{minutes:00}";
    }

    private void DrawMoneyHud()
    {
        Rect panelRect = GetMoneyHudRect();
        GUI.Box(panelRect, "Treasury");
        GUI.Label(new Rect(panelRect.x + 14, panelRect.y + 22, 160, 26), $"Money: ${money}");

        if (moneyPopupTimer <= 0f || moneyPopupAmount <= 0)
        {
            return;
        }

        float normalized = 1f - Mathf.Clamp01(moneyPopupTimer / MoneyPopupDuration);
        float rise = Mathf.Lerp(0f, 26f, normalized);
        float alpha = 1f - normalized;
        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 0.95f, 0.55f, alpha);
        GUI.Label(new Rect(panelRect.x + 92, panelRect.y - 8f - rise, 120, 24), $"+${moneyPopupAmount}");
        GUI.color = previousColor;
    }

    private List<TripOption> GetAvailableTrips()
    {
        List<TripOption> trips = new();

        bool canReachForestTrip =
            HasPath(locations[LocationType.Parking].Anchor, locations[LocationType.Forest].Anchor) &&
            HasPath(locations[LocationType.Forest].Anchor, locations[LocationType.Warehouse].Anchor);
        if (locations[LocationType.Forest].WoodStored > 0 && canReachForestTrip)
        {
            trips.Add(new TripOption
            {
                Type = TripType.ForestToWarehouse,
                Title = "Deliver Logs: Forest -> Warehouse",
                Description = "Pick up logs in Forest and deliver them to Warehouse.",
                Reward = GetTripReward(TripType.ForestToWarehouse)
            });
        }

        bool canReachTownTrip =
            HasPath(locations[LocationType.Parking].Anchor, locations[LocationType.Warehouse].Anchor) &&
            HasPath(locations[LocationType.Warehouse].Anchor, locations[LocationType.Town].Anchor);
        if (locations[LocationType.Warehouse].WoodStored > 0 && canReachTownTrip)
        {
            trips.Add(new TripOption
            {
                Type = TripType.WarehouseToTown,
                Title = "Deliver Logs: Warehouse -> Town",
                Description = "Take stored logs from Warehouse to Town.",
                Reward = GetTripReward(TripType.WarehouseToTown)
            });
        }

        return trips;
    }

    private void AssignTrip(TripOption trip)
    {
        if (trip == null || trip.Type == TripType.None || currentAssignedTrip != TripType.None || currentRefuelPhase != RefuelPhase.None)
        {
            return;
        }

        currentAssignedTrip = trip.Type;
        currentTripPhase = TripPhase.ToPickup;
        currentAssignedTripReward = trip.Reward;
        PlayUiSound(uiSelectClip, 1f);
    }

    private void StartRefuelOrder()
    {
        if (currentAssignedTrip != TripType.None || currentRefuelPhase != RefuelPhase.None)
        {
            return;
        }

        currentRefuelPhase = RefuelPhase.ToGasStation;
        PlayUiSound(uiSelectClip, 1f);
    }

    private void UpdateTruckAutoMode()
    {
        if (!isTruckAutoModeEnabled ||
            currentAssignedTrip != TripType.None ||
            currentRefuelPhase != RefuelPhase.None ||
            isTruckMoving ||
            isTruckInteracting ||
            isDriverRescueActive ||
            !IsTruckInsideParking())
        {
            return;
        }

        if (truckFuel < 30f)
        {
            StartRefuelOrder();
            return;
        }

        List<TripOption> trips = GetAvailableTrips();
        if (trips.Count == 0)
        {
            return;
        }

        TripOption selectedTrip = trips[Random.Range(0, trips.Count)];
        AssignTrip(selectedTrip);
    }

    private int GetTripReward(TripType tripType)
    {
        if (tripType == TripType.None)
        {
            return 0;
        }

        LocationType pickup = GetPickupLocation(tripType);
        LocationType dropoff = GetDropoffLocation(tripType);
        int totalSteps =
            GetPathStepCount(locations[LocationType.Parking].Anchor, locations[pickup].Anchor) +
            GetPathStepCount(locations[pickup].Anchor, locations[dropoff].Anchor) +
            GetPathStepCount(locations[dropoff].Anchor, locations[LocationType.Parking].Anchor);

        int handlingBonus = 12;
        int locationBonus = tripType == TripType.WarehouseToTown ? 10 : 6;
        return Mathf.Max(18, totalSteps * 3 + handlingBonus + locationBonus);
    }

    private int GetPathStepCount(Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> path = FindPath(start, goal);
        return path == null ? 0 : Mathf.Max(0, path.Count - 1);
    }

    private LocationType GetPickupLocation(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToWarehouse => LocationType.Forest,
            TripType.WarehouseToTown => LocationType.Warehouse,
            _ => LocationType.Parking
        };
    }

    private LocationType GetDropoffLocation(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToWarehouse => LocationType.Warehouse,
            TripType.WarehouseToTown => LocationType.Town,
            _ => LocationType.Parking
        };
    }

    private TruckInteractionType GetLoadInteraction(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToWarehouse => TruckInteractionType.LoadAtForest,
            TripType.WarehouseToTown => TruckInteractionType.LoadAtWarehouse,
            _ => TruckInteractionType.None
        };
    }

    private TruckInteractionType GetUnloadInteraction(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToWarehouse => TruckInteractionType.UnloadAtWarehouse,
            TripType.WarehouseToTown => TruckInteractionType.UnloadAtTown,
            _ => TruckInteractionType.None
        };
    }

    private string GetTripTitle(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToWarehouse => "Forest -> Warehouse",
            TripType.WarehouseToTown => "Warehouse -> Town",
            _ => "None"
        };
    }

    private bool IsTruckInsideParking()
    {
        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking) || truckObject == null)
        {
            return false;
        }

        Vector3 position = truckObject.transform.position;
        bool insideParkingBounds =
            position.x >= parking.Min.x &&
            position.x <= parking.Max.x + 1f &&
            position.z >= parking.Min.y &&
            position.z <= parking.Max.y + 1f;

        return insideParkingBounds && !isTruckMoving;
    }

    private bool IsPointerOverHud(Vector2 screenPosition)
    {
        Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);

        if (MainHudRect.Contains(guiPosition) || HelpHudRect.Contains(guiPosition) || GetMoneyHudRect().Contains(guiPosition))
        {
            return true;
        }

        if (selectedLocation == LocationType.Parking && GetParkingHudRect().Contains(guiPosition))
        {
            return true;
        }

        if (selectedLocation == LocationType.Parking && GetAvailableTripsHudRect().Contains(guiPosition))
        {
            return true;
        }

        return selectedLocation == LocationType.Parking && isTruckDetailsOpen && GetTruckDetailsHudRect().Contains(guiPosition);
    }

    private Rect GetParkingHudRect()
    {
        return new Rect(Screen.width - 290, 12, 278, 190);
    }

    private Rect GetTruckDetailsHudRect()
    {
        return new Rect(Screen.width - 290, 392, 278, 388);
    }

    private Rect GetAvailableTripsHudRect()
    {
        return new Rect(Screen.width - 290, 212, 278, 170);
    }

    private Rect GetMoneyHudRect()
    {
        return new Rect(Screen.width * 0.5f - 90f, 12f, 180f, 54f);
    }

    private void UpdateMoneyPopup()
    {
        if (moneyPopupTimer <= 0f)
        {
            return;
        }

        moneyPopupTimer = Mathf.Max(0f, moneyPopupTimer - Time.deltaTime);
        if (moneyPopupTimer <= 0f)
        {
            moneyPopupAmount = 0;
        }
    }

    private void AwardMoney(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        money += amount;
        moneyPopupAmount = amount;
        moneyPopupTimer = MoneyPopupDuration;
        PlayUiSound(moneyRewardClip, 0.95f);
    }

    private void UpdateAudio()
    {
        if (truckLoopAudioSource == null)
        {
            return;
        }

        AudioClip targetClip = isTruckMoving ? truckRollClip : truckIdleClip;
        if (truckLoopAudioSource.clip != targetClip)
        {
            truckLoopAudioSource.clip = targetClip;
            truckLoopAudioSource.Play();
        }

        float targetVolume = isTruckMoving ? 0.18f : 0.11f;
        if (isTruckInteracting)
        {
            targetVolume = 0.07f;
        }

        truckLoopAudioSource.volume = Mathf.Lerp(truckLoopAudioSource.volume, targetVolume, 2.5f * Time.deltaTime);
        truckLoopAudioSource.pitch = Mathf.Lerp(truckLoopAudioSource.pitch, isTruckMoving ? 1.05f : 0.94f, 2.5f * Time.deltaTime);
    }

    private AudioSource CreateAudioSource(string name, Transform parent, bool loop, float volume, float spatialBlend, bool playOnAwake)
    {
        GameObject audioObject = new(name);
        if (parent != null)
        {
            audioObject.transform.SetParent(parent, false);
        }

        AudioSource source = audioObject.AddComponent<AudioSource>();
        source.playOnAwake = playOnAwake;
        source.loop = loop;
        source.volume = volume;
        source.spatialBlend = spatialBlend;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 3f;
        source.maxDistance = 18f;
        source.dopplerLevel = 0f;
        return source;
    }

    private void PlayUiSound(AudioClip clip, float volumeScale)
    {
        if (uiAudioSource == null || clip == null)
        {
            return;
        }

        uiAudioSource.PlayOneShot(clip, volumeScale);
    }

    private void PlayTruckFx(AudioClip clip, float volumeScale)
    {
        if (truckFxAudioSource == null || clip == null)
        {
            return;
        }

        truckFxAudioSource.PlayOneShot(clip, volumeScale);
    }

    private AudioClip CreateUiPulseClip(string clipName, float frequency, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float envelope = Mathf.Exp(-26f * t);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * amplitude;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateWindClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float bed =
                Mathf.Sin(2f * Mathf.PI * 0.18f * t) * 0.45f +
                Mathf.Sin(2f * Mathf.PI * 0.31f * t + 1.7f) * 0.35f +
                Mathf.Sin(2f * Mathf.PI * 0.54f * t + 0.8f) * 0.2f;

            float hiss =
                Mathf.Sin(2f * Mathf.PI * 180f * t + Mathf.Sin(2f * Mathf.PI * 0.09f * t)) * 0.08f +
                Mathf.Sin(2f * Mathf.PI * 260f * t + 1.1f) * 0.05f;

            samples[i] = Mathf.Clamp((bed + hiss) * amplitude, -1f, 1f);
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateRustleClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float pulse =
                Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 0.41f * t + 0.6f)) * 0.7f +
                Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 0.73f * t + 2.1f)) * 0.3f;

            float leafy =
                Mathf.Sin(2f * Mathf.PI * 510f * t + Mathf.Sin(2f * Mathf.PI * 1.4f * t)) * 0.06f +
                Mathf.Sin(2f * Mathf.PI * 690f * t + 0.8f) * 0.05f;

            samples[i] = leafy * pulse * amplitude;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateTownHumClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float low =
                Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.4f +
                Mathf.Sin(2f * Mathf.PI * 142f * t + 0.3f) * 0.25f +
                Mathf.Sin(2f * Mathf.PI * 210f * t + 1.2f) * 0.14f;

            float sway = 0.75f + Mathf.Sin(2f * Mathf.PI * 0.12f * t) * 0.2f;
            samples[i] = low * sway * amplitude;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateTruckIdleClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float engine =
                Mathf.Sin(2f * Mathf.PI * 48f * t) * 0.55f +
                Mathf.Sin(2f * Mathf.PI * 96f * t + 0.3f) * 0.2f +
                Mathf.Sin(2f * Mathf.PI * 144f * t + 0.8f) * 0.12f;

            float wobble = 0.82f + Mathf.Sin(2f * Mathf.PI * 1.25f * t) * 0.08f;
            samples[i] = engine * wobble * amplitude;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateTruckRollClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float wheel =
                Mathf.Sin(2f * Mathf.PI * 78f * t) * 0.28f +
                Mathf.Sin(2f * Mathf.PI * 118f * t + 0.4f) * 0.18f;
            float road =
                Mathf.Sin(2f * Mathf.PI * 320f * t + Mathf.Sin(2f * Mathf.PI * 4.5f * t)) * 0.06f +
                Mathf.Sin(2f * Mathf.PI * 440f * t + 1.4f) * 0.04f;

            samples[i] = (wheel + road) * amplitude;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateCargoThunkClip(string clipName, float duration, float impactAmplitude, float tailAmplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float impactEnvelope = Mathf.Exp(-40f * t);
            float tailEnvelope = Mathf.Exp(-9f * t);
            float impact =
                Mathf.Sin(2f * Mathf.PI * 170f * t) * impactEnvelope * impactAmplitude +
                Mathf.Sin(2f * Mathf.PI * 240f * t + 0.7f) * impactEnvelope * impactAmplitude * 0.55f;
            float tail =
                Mathf.Sin(2f * Mathf.PI * 620f * t + 0.2f) * tailEnvelope * tailAmplitude +
                Mathf.Sin(2f * Mathf.PI * 820f * t + 1.1f) * tailEnvelope * tailAmplitude * 0.6f;

            samples[i] = impact + tail;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateMoneyRewardClip(string clipName, float duration, float amplitude)
    {
        int sampleCount = Mathf.CeilToInt(duration * AudioSampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)AudioSampleRate;
            float envelope = Mathf.Exp(-5.5f * t);
            float sparkle =
                Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.45f +
                Mathf.Sin(2f * Mathf.PI * 880f * t + 0.25f) * 0.3f +
                Mathf.Sin(2f * Mathf.PI * 1110f * t + 0.55f) * 0.16f;
            float glide = 1f + t * 0.8f;
            samples[i] = sparkle * glide * envelope * amplitude;
        }

        return CreateClipFromSamples(clipName, samples);
    }

    private AudioClip CreateClipFromSamples(string clipName, float[] samples)
    {
        AudioClip clip = AudioClip.Create(clipName, samples.Length, 1, AudioSampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
