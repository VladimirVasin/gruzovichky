using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public partial class GameBootstrap : MonoBehaviour
{
    // в”Ђв”Ђ Fields в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private struct RaceSegment
    {
        public Vector3 Center;
        public Quaternion Rotation;
        public float Length;
        public float StartY;
        public float EndY;
    }

    private struct RacingBusData
    {
        public float      T;               // distance from start along track centre (m)
        public float      Speed;           // m/s (always positive)
        public int        Direction;       // +1 = same as player, в€’1 = oncoming
        public float      LaneOffset;      // world units along road-right from centre
        public GameObject Root;
        public float      CollisionRadius;
    }

    private struct TerrainBump
    {
        public Vector2 Center;  // world XZ (Vector2.x = worldX, Vector2.y = worldZ)
        public float   Radius;  // gaussian sigma (metres)
        public float   Height;  // peak height above groundY
    }

    private struct RacingBirdData
    {
        public Transform Root;
        public Transform LeftWing;
        public Transform RightWing;
        public Vector3   PerchPos;
        public float     BobPhase;
        public float     WingPhase;
    }

    private struct RacingBeeData
    {
        public Transform Root;
        public Transform LeftWing;
        public Transform RightWing;
        public Vector3   FlowerPos;
        public float     OrbitAngle;
        public float     OrbitRadius;
        public float     OrbitHeight;
        public float     OrbitSpeed;
        public float     BobAmplitude;
        public float     BobSpeed;
        public float     PhaseOffset;
    }

    private struct RacingMothData
    {
        public Transform Root;
        public Vector3   LanternPos;
        public float     OrbitRadius;
        public float     OrbitHeight;
        public float     OrbitAngle;
        public float     OrbitSpeed;
        public float     BobAmplitude;
        public float     BobSpeed;
        public float     PhaseOffset;
    }

    private struct RacingDustData
    {
        public Transform Root;
        public Vector3   AreaCenter;
        public float     HalfRangeX;
        public float     HalfRangeZ;
        public float     TravelOffset;
        public float     Speed;
        public float     BobAmplitude;
        public float     BobPhase;
        public float     BaseY;
    }

    // Shared struct for any tippable road obstacle (lanterns, trees)
    private struct RaceObstacleData
    {
        public Vector2    PoleXZ;           // world XZ of collision centre
        public Transform  Root;
        public Quaternion OriginalLocalRot;
        public float      CollisionRadius;  // pole/trunk radius in world units
        public float      TiltAngle;        // current degrees
        public float      TiltTarget;
        public Vector3    TiltAxisLocal;    // in root local space, set on first hit
        public bool       IsTipped;
    }

    private Camera racingCamera;
    private GameObject racingSceneRoot;
    private GameObject racingTruckVisual;
    private Transform racingTruckWheelFL;
    private Transform racingTruckWheelFR;
    private Transform racingTruckWheelRL;
    private Transform racingTruckWheelRR;

    private Vector3 racingTruckPos;
    private float racingTruckAngle;   // degrees, Y-up
    private Vector2 racingVelocity;   // X = world X, Y = world Z
    private float racingTruckVelY;    // vertical velocity (m/s), positive = up
    private float racingAngularVel;   // degrees/s
    private float racingSteerInput;   // -1..1, ramps up/down like a steering wheel

    private float racingCameraAngle;  // lagging camera yaw вЂ” trails truck angle for inertial feel
    private float racingCameraSwayX;  // smoothed lateral offset (camera sways out of corner)

    private float racingBodyAngle;    // visual body/rear lag вЂ” trails physics angle for FWD articulation
    private Transform racingFrontAssembly; // front wheels + cabin assembly, rotated ahead of body
    private Transform racingBodyGroup;     // cargo cube вЂ” receives Z body roll
    private Transform racingCabinGroup;    // cabin cube inside FrontAssembly вЂ” receives same Z roll
    private float     racingBodyRoll;      // smoothed lean angle, degrees
    private float     racingBodyPitch;     // smoothed terrain pitch (X-axis), degrees
    private float     racingBodyTiltZ;     // smoothed terrain lateral tilt (Z-axis), degrees

    private const float RacingRollMax        = 6f;   // peak lean in degrees
    private const float RacingRollSmooth     = 4.0f; // Lerp rate
    private const float RacingPitchMax       = 20f;  // max terrain pitch +-degrees
    private const float RacingTerrainTiltMax = 14f;  // max terrain lateral tilt +-degrees
    private const float RacingTerrainSmooth  =  6f;  // terrain pitch/tilt lerp rate

    private readonly List<RaceSegment>      raceSegments          = new();
    private readonly List<RaceSegment>      raceExtensionSegments = new();
    private readonly List<RaceObstacleData> racingLanterns        = new();
    private readonly List<RaceObstacleData> racingTreeObstacles   = new();
    private readonly List<RacingBusData>    racingBuses           = new();
    private readonly List<TerrainBump>      terrainBumps          = new();
    private float racingGroundY;  // base ground Y вЂ” set in PopulateRacingWorld
    private float[]  racingSegCumLen;   // cumulative segment lengths for GetTrackPoint
    private float    racingTrackLen;    // total track length (m)
    private Vector3 raceFinishPos;
    private Vector3 raceFinishFwd;  // road forward direction at finish вЂ” used for strip detection

    private Canvas racingHudCanvas;
    private Text racingHudText;
    private Text racingControlHintText;
    private RectTransform racingSpeedometerNeedle;
    private Text racingSpeedometerText;
    private Light racingHeadlightL;
    private Light racingHeadlightR;
    private readonly List<Light> racingWorldLights = new();

    private AudioSource racingMusicSource;
    private float       racingMusicFadeStart; // time when fadeout began (-1 = not fading)

    // в”Ђв”Ђ Cinematic finish fields в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    private bool    racingFinishSequenceActive;
    private float   racingFinishSequenceTimer;
    private Vector3 racingFinishCameraPos;
    private Quaternion racingFinishCameraRot;
    private Vector2 racingFinishDriveDir;    // XZ forward of truck at finish moment
    private float   racingFinishEntrySpeed;  // truck speed at the moment of crossing finish
    private Canvas  racingFinishOverlayCanvas;
    private Text    racingFinishOverlayText;
    private const float RacingFinishDuration = 3.2f;

    // в”Ђв”Ђ Skybox в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    private GameObject racingSkydome;
    private Renderer   racingSkydomeRenderer;
    private Light      racingDirectionalLight;
    private float      racingSavedShadowDistance;

    // в”Ђв”Ђ Steering wheel + pedals (children of racing camera) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    private GameObject racingSteeringWheelRoot;   // the spinner вЂ” rotates around local Y
    private float      racingWheelAngle;          // current wheel angle, unbounded degrees
    private float      racingWheelAngularVel;     // degrees/sec вЂ” inertia after drag release
    private bool       racingWheelDragging;       // mouse held in wheel zone
    private Transform  racingPedalGas;            // gas pedal root (tilts on W press)
    private Transform  racingPedalBrake;          // brake pedal root (tilts on S press)
    private Transform  racingGearShift;           // gear stick root (forward/reverse tilt)
    private bool       racingIsReversing;         // true when truck moving backward
    private int        racingCurrentGear;         // 0=R, 1вЂ“4=forward gears (auto)
    private float      racingGearChangeTimer;     // cooldown between gear changes (s)
    private Text       racingGearText;            // HUD gear indicator

    // в”Ђв”Ђ Racing atmosphere в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    private readonly List<RacingBirdData>  racingBirds       = new();
    private readonly List<RacingBeeData>   racingBees        = new();
    private readonly List<RacingMothData>  racingMoths       = new();
    private readonly List<RacingDustData>  racingDustMotes   = new();
    private readonly List<Vector3>         racingFlowerPoints = new();

    private GameObject joinRaceButtonRoot;  // the "JOIN THE RACE" button canvas
    private Button joinRaceButton;
    private Text joinRaceButtonText;

    // в”Ђв”Ђ Constants в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    // в”Ђв”Ђ Lantern collision в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    private const float LanternPoleRadius      = 0.12f; // world units (pole local 0.08 Г— scale 3 Г— 0.5)
    private const float TruckCollisionRadius   = 0.60f; // world units вЂ” truck XZ footprint
    private const float LanternCombinedRadius  = LanternPoleRadius + TruckCollisionRadius; // 0.72
    private const float LanternTiltTargetDeg   = 44f;   // how far lantern tips when hit
    private const float LanternTiltSpeed       = 140f;  // deg/s (reaches 44В° in ~0.3 s)
    private const float CollisionEnergyLoss    = 0.42f; // fraction of normal velocity lost on hit
    private const float CollisionAngularKick   = 55f;   // deg/s spin impulse on first contact

    // в”Ђв”Ђ Physics в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    private const float RacingAcceleration  = 6f;     // raw power вЂ” halved for heavy truck feel
    private const float RacingMaxSpeed      = 14.5f;  // ~52 km/h вЂ” higher ceiling, slow to reach
    private const float RacingDrag          = 0.997f; // very gentle coasting drag
    private const float RacingAngularDrag   = 0.88f;  // high = angular velocity persists (inertial steering)
    private const float RacingSteerForce    = 300f;   // max turn force (balanced with new angular drag)
    private const float RacingLateralFriction = 28f;  // lower = more drift/slide

    // в”Ђв”Ђ Gear shift в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    // Lever X-rotation per gear: 0=R, 1=1st, 2=2nd, 3=3rd, 4=4th
    private static readonly float[] GearShiftAngles  = { -28f, -14f, 0f, 14f, 28f };
    // Upshift when speed (km/h) exceeds this (index = current gear)
    private static readonly float[] GearUpKmh        = { 0f, 14f, 27f, 41f, float.MaxValue };
    // Downshift when speed (km/h) falls below this (index = current gear)
    private static readonly float[] GearDownKmh      = { 0f, 0f, 11f, 23f, 37f };
    private const float GearChangeCooldown = 0.5f;    // min seconds between shifts
    private const int   RaceSegmentCount    = 36;
    private const float RaceTrackOffsetX    = 2000f;   // remote position, away from main world
    private const float RaceFinishRadius    = 2.8f;

    // в”Ђв”Ђ Join-Race button setup/update в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void SetupJoinRaceButton()
    {
        if (joinRaceButtonRoot != null) return;

        EnsureFleetEventSystem(); // button won't fire without an EventSystem in the scene
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObj = new("JoinRaceCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        joinRaceButtonRoot = canvasObj;

        // Position: top-center of screen
        RectTransform btnRect = CreateUiObject("JoinRaceBtnRoot", canvasObj.transform).GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(0.5f, 1f);
        btnRect.anchorMax = new Vector2(0.5f, 1f);
        btnRect.pivot     = new Vector2(0.5f, 1f);
        btnRect.anchoredPosition = new Vector2(0f, -18f);
        btnRect.sizeDelta = new Vector2(260f, 52f);

        joinRaceButton = btnRect.gameObject.AddComponent<Button>();
        Image btnImg = btnRect.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0.88f, 0.62f, 0.08f);
        joinRaceButton.targetGraphic = btnImg;

        ColorBlock cb = joinRaceButton.colors;
        cb.normalColor      = new Color(0.88f, 0.62f, 0.08f);
        cb.highlightedColor = new Color(1.00f, 0.76f, 0.20f);
        cb.pressedColor     = new Color(0.66f, 0.46f, 0.04f);
        cb.selectedColor    = cb.normalColor;
        joinRaceButton.colors = cb;

        Outline outline = btnRect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.35f);
        outline.effectDistance = new Vector2(2f, -2f);

        joinRaceButtonText = new GameObject("JoinRaceText").AddComponent<Text>();
        joinRaceButtonText.transform.SetParent(btnRect, false);
        joinRaceButtonText.rectTransform.anchorMin = Vector2.zero;
        joinRaceButtonText.rectTransform.anchorMax = Vector2.one;
        joinRaceButtonText.rectTransform.offsetMin = Vector2.zero;
        joinRaceButtonText.rectTransform.offsetMax = Vector2.zero;
        joinRaceButtonText.font = font;
        joinRaceButtonText.fontSize = 18;
        joinRaceButtonText.fontStyle = FontStyle.Bold;
        joinRaceButtonText.alignment = TextAnchor.MiddleCenter;
        joinRaceButtonText.color = Color.white;
        joinRaceButtonText.text = "JOIN THE RACE  >";

        joinRaceButton.onClick.AddListener(StartRacingMinigame);
        joinRaceButtonRoot.SetActive(false);
    }

    private void UpdateJoinRaceButton()
    {
        if (joinRaceButtonRoot == null) return;

        bool shouldShow = isGameStarted &&
                          !isRacingActive &&
                          (selectedGameStartMode == GameStartMode.Debug ||
                           (activeTradeRun != null && activeTradeRun.Phase == TradeRunPhase.OutOfMap));

        if (joinRaceButtonRoot.activeSelf != shouldShow)
            joinRaceButtonRoot.SetActive(shouldShow);
    }

    // в”Ђв”Ђ Minigame entry / exit в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private bool TryHandleJoinRaceButtonClick(Vector2 screenPosition)
    {
        if (joinRaceButtonRoot == null ||
            joinRaceButton == null ||
            !joinRaceButtonRoot.activeSelf ||
            isRacingActive)
        {
            return false;
        }

        RectTransform buttonRect = joinRaceButton.transform as RectTransform;
        if (buttonRect == null ||
            !RectTransformUtility.RectangleContainsScreenPoint(buttonRect, screenPosition))
        {
            return false;
        }

        SessionDebugLogger.Log("RACING", "Join race clicked via direct input fallback.");
        StartRacingMinigame();
        return true;
    }

    private bool UpdateJoinRaceButtonInputFallback()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return false;
        }

        return TryHandleJoinRaceButtonClick(Mouse.current.position.ReadValue());
    }

    private void StartRacingMinigame()
    {
        if (isRacingActive) return;
        try
        {
            isRacingActive = true;

            // Pause main game. Keep fixedDeltaTime valid so standalone builds do not enter a fragile physics state.
            lastActiveGameSpeedMultiplier = gameSpeedMultiplier > 0 ? gameSpeedMultiplier : 1;
            gameSpeedMultiplier = 0;
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0.02f;

            // Close all city-mode panels before entering race
            CloseAllMenus();

            // Mute all city-mode music during race
            if (cityMusicSource != null) cityMusicSource.Pause();
            PauseDayNightMusic();

            // Hide join button
            if (joinRaceButtonRoot != null) joinRaceButtonRoot.SetActive(false);

            // Disable main camera
            if (mainCamera != null) mainCamera.enabled = false;

            SessionDebugLogger.Log("RACING", "Racing minigame bootstrap started.");

            // Build scene geometry first
            GenerateRaceTrack();
            BuildTrackSampler();
            SpawnRacingBuses();
            PopulateRacingWorld();
            CreateRacingTruck();

            // Set truck start position BEFORE camera setup so camera initialises at the right location
            racingTruckPos   = raceSegments[0].Center - raceSegments[0].Rotation * Vector3.forward * raceSegments[0].Length * 0.45f;
            racingTruckPos.y = raceSegments[0].StartY + 0.35f;
            racingTruckAngle = raceSegments[0].Rotation.eulerAngles.y;
            racingVelocity          = Vector2.zero;
            racingTruckVelY         = 0f;
            racingAngularVel        = 0f;
            racingSteerInput        = 0f;
            racingWheelAngle        = 0f;
            racingWheelAngularVel   = 0f;
            racingWheelDragging     = false;
            racingCurrentGear   = 1;
            racingGearChangeTimer = 0f;
            racingCameraAngle = racingTruckAngle;
            racingCameraSwayX = 0f;
            racingBodyAngle   = racingTruckAngle;
            racingBodyRoll    = 0f;
            racingBodyPitch   = 0f;
            racingBodyTiltZ   = 0f;

            SetupRacingCamera();
            CreateSteeringWheel();
            CreateRacingPedals();
            CreateGearShift();
            SetupRacingHud();
            PopulateRacingAtmosphere();

            // Start looping music
            AudioClip musicClip = Resources.Load<AudioClip>("Race1");
            if (musicClip != null)
            {
                racingMusicSource = CreateAudioSource("RacingMusic", null, true, 0.20f, 0f, false);
                racingMusicFadeStart = -1f;
                racingMusicSource.ignoreListenerPause = true;
                racingMusicSource.ignoreListenerVolume = false;
                racingMusicSource.clip = musicClip;
                racingMusicSource.Play();
            }

            SessionDebugLogger.Log("RACING", "Racing minigame started.");
        }
        catch (System.Exception ex)
        {
            SessionDebugLogger.Log("RACING", $"Racing minigame failed during bootstrap: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            isRacingActive = false;
            racingFinishSequenceActive = false;
            CleanupRacingScene();
        }
    }

    private void FinishRace(bool success)
    {
        if (!isRacingActive) return;

        if (success && !racingFinishSequenceActive)
        {
            // Begin cinematic вЂ” don't clean up yet
            racingBonusEarned = 50;
            SessionDebugLogger.Log("RACING", "Race completed! Bonus earned: $50.");
            StartFinishSequence();
            return;
        }

        // Immediate cleanup (skip or already in sequence)
        isRacingActive = false;
        racingFinishSequenceActive = false;

        if (!success)
            SessionDebugLogger.Log("RACING", "Race skipped.");

        // Stop music
        if (racingMusicSource != null) { Object.Destroy(racingMusicSource.gameObject); racingMusicSource = null; }

        CleanupRacingScene();
    }

    private void StartFinishSequence()
    {
        racingFinishSequenceActive = true;
        racingFinishSequenceTimer  = 0f;

        // Freeze camera at current position/rotation
        if (racingCamera != null)
        {
            racingFinishCameraPos = racingCamera.transform.position;
            racingFinishCameraRot = racingCamera.transform.rotation;
        }

        // Remember truck's forward direction and entry speed
        float rad = racingTruckAngle * Mathf.Deg2Rad;
        racingFinishDriveDir  = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        racingFinishEntrySpeed = Mathf.Max(racingVelocity.magnitude, 5f);

        // Align velocity cleanly along heading (no sideways drift)
        racingVelocity   = racingFinishDriveDir * racingFinishEntrySpeed;
        racingAngularVel = 0f;

        // Hide normal HUD
        if (racingHudCanvas != null) racingHudCanvas.gameObject.SetActive(false);

        // Victory sound вЂ” short upbeat chime built from two sine tones
        PlayRacingVictorySound();

        // Start music fadeout over 3 seconds
        racingMusicFadeStart = Time.unscaledTime;

        // Create finish overlay canvas
        GameObject overlayObj = new("FinishOverlayCanvas", typeof(Canvas), typeof(CanvasScaler));
        Canvas ovCanvas = overlayObj.GetComponent<Canvas>();
        ovCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ovCanvas.sortingOrder = 110;

        CanvasScaler ovScaler = overlayObj.GetComponent<CanvasScaler>();
        ovScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        ovScaler.referenceResolution = new Vector2(1600f, 900f);
        ovScaler.matchWidthOrHeight   = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Dark translucent bar behind text
        GameObject barObj = new("FinishBar");
        barObj.transform.SetParent(overlayObj.transform, false);
        RectTransform barRect = barObj.AddComponent<RectTransform>();
        barRect.anchorMin        = new Vector2(0f, 0.55f);
        barRect.anchorMax        = new Vector2(1f, 0.75f);
        barRect.offsetMin        = Vector2.zero;
        barRect.offsetMax        = Vector2.zero;
        Image barImg = barObj.AddComponent<Image>();
        barImg.color = new Color(0f, 0f, 0f, 0.55f);

        // "Р’С‹ С„РёРЅРёС€РёСЂРѕРІР°Р»Рё!" text
        GameObject textObj = new("FinishText");
        textObj.transform.SetParent(overlayObj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.font      = font;
        txt.fontSize  = 52;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = new Color(1f, 0.92f, 0.2f, 1f);
        txt.text      = "YOU FINISHED!";

        RectTransform txtRect = txt.rectTransform;
        txtRect.anchorMin        = new Vector2(0f, 0.55f);
        txtRect.anchorMax        = new Vector2(1f, 0.75f);
        txtRect.offsetMin        = Vector2.zero;
        txtRect.offsetMax        = Vector2.zero;

        // Sub-text: bonus info
        GameObject subObj = new("FinishSubText");
        subObj.transform.SetParent(overlayObj.transform, false);
        Text sub = subObj.AddComponent<Text>();
        sub.font      = font;
        sub.fontSize  = 26;
        sub.alignment = TextAnchor.MiddleCenter;
        sub.color     = new Color(1f, 1f, 1f, 0.85f);
        sub.text      = "+ $50 bonus on cargo delivery";

        RectTransform subRect = sub.rectTransform;
        subRect.anchorMin        = new Vector2(0f, 0.47f);
        subRect.anchorMax        = new Vector2(1f, 0.58f);
        subRect.offsetMin        = Vector2.zero;
        subRect.offsetMax        = Vector2.zero;

        racingFinishOverlayCanvas = ovCanvas;
        racingFinishOverlayText   = txt;
    }

    private void UpdateFinishSequence()
    {
        float dt = Time.unscaledDeltaTime;
        racingFinishSequenceTimer += dt;

        float t = Mathf.Clamp01(racingFinishSequenceTimer / RacingFinishDuration);

        // Truck floors it past the finish вЂ” accelerates into the horizon
        float coastSpeed = Mathf.Lerp(racingFinishEntrySpeed, RacingMaxSpeed * 1.1f, t * t);
        racingVelocity = racingFinishDriveDir * coastSpeed;

        racingTruckPos.x += racingVelocity.x * dt;
        racingTruckPos.z += racingVelocity.y * dt;

        if (racingTruckVisual != null)
        {
            racingTruckVisual.transform.position = racingTruckPos;
            // Spin wheels at coast speed
            float wheelSpin = coastSpeed * dt * 180f;
            if (racingTruckWheelFL != null) racingTruckWheelFL.Rotate(Vector3.up, wheelSpin, Space.Self);
            if (racingTruckWheelFR != null) racingTruckWheelFR.Rotate(Vector3.up, wheelSpin, Space.Self);
            if (racingTruckWheelRL != null) racingTruckWheelRL.Rotate(Vector3.up, wheelSpin, Space.Self);
            if (racingTruckWheelRR != null) racingTruckWheelRR.Rotate(Vector3.up, wheelSpin, Space.Self);
        }

        // Camera frozen at finish position
        if (racingCamera != null)
        {
            racingCamera.transform.position = racingFinishCameraPos;
            racingCamera.transform.rotation = racingFinishCameraRot;
        }

        // Overlay: fade in first 0.12 of normalized duration, hold, fade out last 0.18
        if (racingFinishOverlayCanvas != null)
        {
            float groupAlpha;
            if (t < 0.12f)
                groupAlpha = Mathf.InverseLerp(0f, 0.12f, t);
            else if (t > 0.82f)
                groupAlpha = Mathf.InverseLerp(1f, 0.82f, t);
            else
                groupAlpha = 1f;

            // Use CanvasGroup alpha so individual graphic alphas are preserved
            CanvasGroup cg = racingFinishOverlayCanvas.GetComponent<CanvasGroup>();
            if (cg == null) cg = racingFinishOverlayCanvas.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = groupAlpha;
        }

        UpdateRacingSkydome();

        // Music fadeout over 3 seconds
        if (racingMusicSource != null && racingMusicFadeStart >= 0f)
        {
            float elapsed = Time.unscaledTime - racingMusicFadeStart;
            racingMusicSource.volume = Mathf.Lerp(0.20f, 0f, elapsed / 3f);
        }

        // Wheel returns to center during cinematic; gas stays floored
        racingWheelDragging   = false;
        racingWheelAngularVel = 0f;
        racingWheelAngle      = Mathf.MoveTowards(racingWheelAngle, 0f, 2.5f * dt * 180f);
        racingSteerInput      = Mathf.Clamp(racingWheelAngle / 180f, -1f, 1f);
        UpdateSteeringWheel(dt);
        UpdatePedals(dt, 1f, false);
        racingIsReversing = false;
        racingCurrentGear = 4; // cinematic вЂ” full speed, show top gear
        UpdateGearShift(dt);

        // Done вЂ” clean up
        if (racingFinishSequenceTimer >= RacingFinishDuration)
        {
            isRacingActive             = false;
            racingFinishSequenceActive = false;

            // Stop music
            if (racingMusicSource != null) { Object.Destroy(racingMusicSource.gameObject); racingMusicSource = null; }

            // Overlay
            if (racingFinishOverlayCanvas != null)
            { Object.Destroy(racingFinishOverlayCanvas.gameObject); racingFinishOverlayCanvas = null; racingFinishOverlayText = null; }

            CleanupRacingScene();
        }
    }

    private void CleanupRacingScene()
    {
        // Restore shadow distance
        QualitySettings.shadowDistance = racingSavedShadowDistance;

        // Destroy directional light + skydome
        if (racingDirectionalLight != null) { Object.Destroy(racingDirectionalLight.gameObject); racingDirectionalLight = null; }
        if (racingSkydome != null) { Object.Destroy(racingSkydome); racingSkydome = null; racingSkydomeRenderer = null; }

        // Steering wheel + pedals are children of camera вЂ” destroyed with it
        racingSteeringWheelRoot  = null;
        racingWheelAngle  = 0f;
        racingBodyPitch   = 0f;
        racingBodyTiltZ   = 0f;
        racingPedalGas           = null;
        racingPedalBrake         = null;
        racingGearShift          = null;
        racingIsReversing        = false;
        racingCurrentGear        = 0;
        racingGearChangeTimer    = 0f;
        racingGearText           = null;

        // Destroy racing scene
        if (racingCamera != null) { Object.Destroy(racingCamera.gameObject); racingCamera = null; }
        if (racingSceneRoot != null) { Object.Destroy(racingSceneRoot); racingSceneRoot = null; }
        if (racingTruckVisual != null)
        {
            Object.Destroy(racingTruckVisual);
            racingTruckVisual   = null;
            racingFrontAssembly = null;
            racingBodyGroup     = null;
            racingCabinGroup    = null;
        }
        if (racingHudCanvas != null) { Object.Destroy(racingHudCanvas.gameObject); racingHudCanvas = null; }
        racingHudText = null;
        racingControlHintText = null;
        racingSpeedometerNeedle = null;
        racingSpeedometerText = null;
        racingHeadlightL = null;
        racingHeadlightR = null;
        racingWorldLights.Clear();
        racingLanterns.Clear();
        racingTreeObstacles.Clear();
        racingBirds.Clear();
        racingBees.Clear();
        racingMoths.Clear();
        racingDustMotes.Clear();
        racingFlowerPoints.Clear();
        CleanupRacingBuses();
        raceSegments.Clear();
        raceExtensionSegments.Clear();

        // Resume city music
        if (cityMusicSource != null) cityMusicSource.UnPause();
        ResumeDayNightMusic();

        // Restore main camera
        if (mainCamera != null) mainCamera.enabled = true;

        // Restore game speed
        int restore = lastActiveGameSpeedMultiplier > 0 ? lastActiveGameSpeedMultiplier : 1;
        gameSpeedMultiplier = restore;
        Time.timeScale = restore;
        Time.fixedDeltaTime = 0.02f * restore;

        // Trigger trade run return immediately
        if (activeTradeRun != null)
            activeTradeRun.OutOfMapTimer = 0f;

        UpdateJoinRaceButton();
    }

    // в”Ђв”Ђ Per-frame update в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void UpdateRacingMinigame()
    {
        if (!isRacingActive && !racingFinishSequenceActive) return;

        // Cinematic finish sequence вЂ” runs after crossing finish line
        if (racingFinishSequenceActive)
        {
            UpdateFinishSequence();
            return;
        }

        float dt = Time.unscaledDeltaTime;

        // в”Ђв”Ђ Input в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        float throttle = 0f;
        bool  sBrakeReverse = false;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   throttle += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) sBrakeReverse = true;

            // Steering is intentionally mouse-only: the wheel is the control surface.
            UpdateWheelMouseDrag(dt);

            if (kb.escapeKey.wasPressedThisFrame)
            {
                FinishRace(success: false);
                return;
            }
        }

        // в”Ђв”Ђ Physics в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        float speed = racingVelocity.magnitude;

        // Forward direction (from previous frame angle вЂ” used for reverse steer check)
        float rad = racingTruckAngle * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        Vector2 right   = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));

        // When reversing, invert steering so controls feel natural (left = go left)
        float fwdDot    = Vector2.Dot(racingVelocity, forward);
        float steerSign = fwdDot >= 0f ? 1f : -1f;
        racingIsReversing = fwdDot < -0.2f;

        // Steering вЂ” uses ramped input, stronger max force
        float steerAmount = racingSteerInput * steerSign * RacingSteerForce * Mathf.Clamp01(speed / 3.5f) * dt;
        racingAngularVel += steerAmount;
        racingAngularVel *= Mathf.Pow(RacingAngularDrag, dt * 60f);

        racingTruckAngle += racingAngularVel * dt;

        // Recompute forward/right after angle update
        rad     = racingTruckAngle * Mathf.Deg2Rad;
        forward = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        right   = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));

        // Acceleration with torque curve вЂ” full power at low speed, tapers to 0 at max
        float torque = Mathf.Pow(1f - Mathf.Clamp01(speed / RacingMaxSpeed), 0.6f);
        racingVelocity += forward * throttle * RacingAcceleration * torque * dt;

        // Lateral friction (simulates grip)
        float lateralSpeed = Vector2.Dot(racingVelocity, right);
        racingVelocity -= right * (lateralSpeed * RacingLateralFriction * dt);

        // S key вЂ” brake while moving forward, then reverse at half speed
        if (sBrakeReverse)
        {
            float fwdSpeed = Vector2.Dot(racingVelocity, forward); // + = moving forward

            if (fwdSpeed > 0.25f)
            {
                // Brake
                racingVelocity -= racingVelocity.normalized * speed * Mathf.Clamp01(1.4f * dt);
            }
            else
            {
                // Reverse вЂ” half max speed, same torque curve
                float revMax    = RacingMaxSpeed * 0.5f;
                float revSpeed  = Mathf.Max(0f, -fwdSpeed);
                float revTorque = Mathf.Pow(1f - Mathf.Clamp01(revSpeed / revMax), 0.6f);
                racingVelocity -= forward * RacingAcceleration * 0.55f * revTorque * dt;

                // Cap reverse speed
                float newFwdSpeed = Vector2.Dot(racingVelocity, forward);
                if (newFwdSpeed < -revMax)
                    racingVelocity += forward * (-revMax - newFwdSpeed);
            }
        }

        // Drag
        racingVelocity *= Mathf.Pow(RacingDrag, dt * 60f);

        // Speed cap
        if (racingVelocity.sqrMagnitude > RacingMaxSpeed * RacingMaxSpeed)
            racingVelocity = racingVelocity.normalized * RacingMaxSpeed;

        // Update position
        racingTruckPos.x += racingVelocity.x * dt;
        racingTruckPos.z += racingVelocity.y * dt;
        {
            const float Gravity           = 18f;   // m/sВІ вЂ” arcade-strong
            const float TerminalVelY      = -25f;  // m/s downward cap
            const float GroundedThreshold = 0.05f; // m above floor в†’ airborne

            Vector3 flatPos = new Vector3(racingTruckPos.x, 0f, racingTruckPos.z);
            bool  onRoad = IsPositionOnRaceRoad(flatPos, 4.8f);
            float floorY = onRoad
                ? SampleRaceRoadY(racingTruckPos.x, racingTruckPos.z)
                : SampleGroundMeshY(racingTruckPos.x, racingTruckPos.z, racingGroundY) + 0.35f;

            if (onRoad && floorY > racingTruckPos.y)
            {
                // Uphill вЂ” lerp up to meet the rising surface
                racingTruckPos.y = Mathf.Lerp(racingTruckPos.y, floorY, 18f * dt);
                racingTruckVelY  = 0f;
            }
            else
            {
                // Downhill or off-road вЂ” apply gravity, land on floor
                racingTruckVelY   = Mathf.Max(racingTruckVelY - Gravity * dt, TerminalVelY);
                racingTruckPos.y += racingTruckVelY * dt;
                if (racingTruckPos.y <= floorY + GroundedThreshold)
                {
                    racingTruckPos.y = floorY;
                    racingTruckVelY  = 0f;
                }
            }
        }

        // Bus + lantern collision (depenetration + velocity response)
        UpdateRacingBuses(dt);
        UpdateLanternCollisions(dt);
        UpdateRacingAtmosphere(dt);

        // в”Ђв”Ђ Apply truck transform вЂ” FWD articulation в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        if (racingTruckVisual != null)
        {
            // Body/rear lags behind physics angle вЂ” gives the "rear follows front" look
            racingBodyAngle = Mathf.LerpAngle(racingBodyAngle, racingTruckAngle, 4.5f * dt);

            // в”Ђв”Ђ Terrain pitch + lateral tilt (4-point finite difference) в”Ђв”Ђв”Ђв”Ђв”Ђ
            const float ProbeD = 0.8f;
            float sinY = Mathf.Sin(racingBodyAngle * Mathf.Deg2Rad);
            float cosY = Mathf.Cos(racingBodyAngle * Mathf.Deg2Rad);
            float yFwd   = SampleSurfaceY(racingTruckPos.x + sinY * ProbeD, racingTruckPos.z + cosY * ProbeD);
            float yBack  = SampleSurfaceY(racingTruckPos.x - sinY * ProbeD, racingTruckPos.z - cosY * ProbeD);
            float yRight = SampleSurfaceY(racingTruckPos.x + cosY * ProbeD, racingTruckPos.z - sinY * ProbeD);
            float yLeft  = SampleSurfaceY(racingTruckPos.x - cosY * ProbeD, racingTruckPos.z + sinY * ProbeD);
            float slopeF = (yFwd - yBack)   / (2f * ProbeD);
            float slopeR = (yRight - yLeft)  / (2f * ProbeD);
            float pitchTarget = Mathf.Clamp(-Mathf.Atan(slopeF) * Mathf.Rad2Deg, -RacingPitchMax, RacingPitchMax);
            float tiltTarget  = Mathf.Clamp(-Mathf.Atan(slopeR) * Mathf.Rad2Deg, -RacingTerrainTiltMax, RacingTerrainTiltMax);
            racingBodyPitch = Mathf.Lerp(racingBodyPitch, pitchTarget, RacingTerrainSmooth * dt);
            racingBodyTiltZ = Mathf.Lerp(racingBodyTiltZ, tiltTarget,  RacingTerrainSmooth * dt);

            racingTruckVisual.transform.position = racingTruckPos;
            racingTruckVisual.transform.rotation = Quaternion.Euler(racingBodyPitch, racingBodyAngle, racingBodyTiltZ);

            // FWD delta вЂ” front axle (wheels) + cabin pivot both steer ahead of body
            float frontDelta = Mathf.DeltaAngle(racingBodyAngle, racingTruckAngle);
            if (racingFrontAssembly != null)
                racingFrontAssembly.localRotation = Quaternion.Euler(0f, frontDelta, 0f);
            if (racingCabinGroup != null)
                racingCabinGroup.localRotation = Quaternion.Euler(0f, frontDelta, 0f);

            // Body roll вЂ” both red body and yellow cabin lean together (suspension)
            // Terminal angularVel в‰€ 36 deg/s, normalise в†’ В±RacingRollMax degrees
            float rollTarget = Mathf.Clamp(racingAngularVel / 36f, -1f, 1f) * RacingRollMax;
            racingBodyRoll = Mathf.Lerp(racingBodyRoll, rollTarget, RacingRollSmooth * dt);
            if (racingBodyGroup != null)
                racingBodyGroup.localRotation = Quaternion.Euler(0f, 0f, racingBodyRoll);

            // Rotate around the cylinder's local Y (= world X after Euler(0,0,-90)) вЂ” rolls forward
            float wheelSpin = speed * dt * 180f;
            if (racingTruckWheelFL != null) racingTruckWheelFL.Rotate(Vector3.up, wheelSpin, Space.Self);
            if (racingTruckWheelFR != null) racingTruckWheelFR.Rotate(Vector3.up, wheelSpin, Space.Self);
            if (racingTruckWheelRL != null) racingTruckWheelRL.Rotate(Vector3.up, wheelSpin, Space.Self);
            if (racingTruckWheelRR != null) racingTruckWheelRR.Rotate(Vector3.up, wheelSpin, Space.Self);
        }

        // в”Ђв”Ђ Camera follow вЂ” lagging yaw + lateral sway в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        if (racingCamera != null)
        {
            // Camera yaw trails truck yaw вЂ” lower value = more lag / heavier feel
            racingCameraAngle = Mathf.LerpAngle(racingCameraAngle, racingTruckAngle, 2.8f * dt);

            // Lateral sway: camera drifts opposite to steering (centrifugal throw)
            float swayTarget  = racingSteerInput * -0.5f;
            racingCameraSwayX = Mathf.Lerp(racingCameraSwayX, swayTarget, 3.5f * dt);

            // Small roll tilt into the corner
            float roll = racingCameraSwayX * -3.0f;

            Quaternion camRot = Quaternion.Euler(26f, racingCameraAngle, roll);
            Vector3 camBack   = camRot * Vector3.back * 5f;
            Vector3 swayWorld = camRot * Vector3.right * racingCameraSwayX;
            Vector3 targetPos = racingTruckPos + Vector3.up * 1f + camBack + swayWorld;

            racingCamera.transform.position = Vector3.Lerp(
                racingCamera.transform.position, targetPos, 5.5f * dt);
            racingCamera.transform.rotation = Quaternion.Slerp(
                racingCamera.transform.rotation, camRot, 5.5f * dt);
        }

        // в”Ђв”Ђ Finish check вЂ” strip detection в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        // Narrow along road direction (В±2.5 m), very wide across it (В±14 m)
        // so missing the line by driving off-road still counts.
        Vector3 toFinish = new Vector3(racingTruckPos.x - raceFinishPos.x, 0f, racingTruckPos.z - raceFinishPos.z);
        float alongRoad   = Vector3.Dot(toFinish, raceFinishFwd);          // depth through the line
        float acrossRoad  = Vector3.Cross(toFinish, raceFinishFwd).magnitude; // lateral distance
        bool  crossedLine = Mathf.Abs(alongRoad) < 2.5f && acrossRoad < 14f;

        // Keep distToFinish for HUD display (visual distance to centre of line)
        float distToFinish = toFinish.magnitude;

        // в”Ђв”Ђ HUD update в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        float kmh = speed * 3.6f;

        UpdateAutoGear(fwdDot, kmh, dt);

        if (racingHudText != null)
        {
            racingHudText.text =
                "INTERCITY DELIVERY\n" +
                "в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ\n" +
                $"Finish:  {distToFinish:F0} m\n" +
                "в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ\n" +
                "[ESC]  Skip";
        }

        if (racingSpeedometerNeedle != null)
        {
            // Sweep: 0 km/h = 150В° CCW from up (7 o'clock), 100 km/h = -150В° (5 o'clock)
            float needleZ = 150f - Mathf.Clamp01(kmh / 100f) * 300f;
            racingSpeedometerNeedle.localEulerAngles = new Vector3(0f, 0f, needleZ);
        }

        if (racingSpeedometerText != null)
            racingSpeedometerText.text = $"{kmh:F0}";

        if (racingGearText != null)
        {
            racingGearText.text  = racingCurrentGear == 0 ? "R" : racingCurrentGear.ToString();
            racingGearText.color = racingCurrentGear == 0
                ? new Color(1f, 0.3f, 0.2f)
                : new Color(0.95f, 0.90f, 0.84f);
        }

        // Headlights вЂ” always on, brighter at night
        if (racingHeadlightL != null && racingHeadlightR != null)
        {
            float darkness = 1f - currentStylizedDaylight;
            // Day: 1.2,  dusk/night ramps up to 4.5
            float headlightIntensity = Mathf.Lerp(1.2f, 4.5f, Mathf.Clamp01(darkness * 2f));
            racingHeadlightL.enabled   = true;
            racingHeadlightR.enabled   = true;
            racingHeadlightL.intensity = headlightIntensity;
            racingHeadlightR.intensity = headlightIntensity;
        }

        // World lanterns along track
        if (racingWorldLights.Count > 0)
        {
            float wDarkness  = 1f - currentStylizedDaylight;
            bool  wOn        = wDarkness > 0.55f;
            float wIntensity = wOn ? Mathf.Lerp(0.3f, 1.2f, Mathf.InverseLerp(0.55f, 1f, wDarkness)) : 0f;
            foreach (Light wl in racingWorldLights)
            {
                if (wl == null) continue;
                wl.enabled    = wOn;
                wl.intensity  = wIntensity;
            }
        }

        UpdateRacingSkydome();
        UpdateSteeringWheel(dt);
        UpdatePedals(dt, throttle, sBrakeReverse);
        UpdateGearShift(dt);

        if (crossedLine)
        {
            FinishRace(success: true);
        }
    }

    private void PlayRacingVictorySound()
    {
        // Procedural two-note victory chime: major third (C5 + E5), short attack/decay
        const int sampleRate = 44100;
        const float duration = 0.55f;
        int samples = (int)(sampleRate * duration);

        float[] data = new float[samples * 2]; // stereo
        float[] freqs = { 523.25f, 659.25f, 783.99f }; // C5, E5, G5
        for (int i = 0; i < samples; i++)
        {
            float t    = (float)i / sampleRate;
            float env  = Mathf.Clamp01(t / 0.02f) * Mathf.Pow(1f - t / duration, 1.8f);
            float note = Mathf.Sin(2f * Mathf.PI * freqs[0] * t) * 0.45f
                       + Mathf.Sin(2f * Mathf.PI * freqs[1] * t) * 0.35f
                       + Mathf.Sin(2f * Mathf.PI * freqs[2] * t) * 0.25f;
            float sample = note * env;
            data[i * 2]     = sample;
            data[i * 2 + 1] = sample;
        }

        AudioClip chime = AudioClip.Create("VictoryChime", samples, 2, sampleRate, false);
        chime.SetData(data, 0);

        AudioSource src = CreateAudioSource("VictorySound", null, false, 0.75f, 0f, false);
        src.ignoreListenerPause = true;
        src.clip = chime;
        src.Play();
        Object.Destroy(src.gameObject, duration + 0.5f);
    }

    private void UpdateWheelMouseDrag(float dt)
    {
        if (racingCamera == null) return;
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mousePos = mouse.position.ReadValue();

        // On click inside wheel zone вЂ” start drag
        if (mouse.leftButton.wasPressedThisFrame)
        {
            racingWheelDragging = false;
            float wheelRadius = Screen.height * 0.32f;
            if (racingSteeringWheelRoot != null &&
                ScreenDist(mousePos, racingCamera, racingSteeringWheelRoot.transform.position) < wheelRadius)
            {
                racingWheelDragging   = true;
                racingWheelAngularVel = 0f;
            }
        }

        if (mouse.leftButton.wasReleasedThisFrame)
            racingWheelDragging = false;

        // в”Ђв”Ђ Wheel drag в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        if (racingWheelDragging && mouse.leftButton.isPressed)
        {
            float mouseDX      = mouse.delta.ReadValue().x;
            float targetAngVel = mouseDX * 0.55f / Mathf.Max(dt, 0.001f);
            racingWheelAngularVel = Mathf.Lerp(racingWheelAngularVel, targetAngVel, 12f * dt);
            racingWheelAngle     += racingWheelAngularVel * dt;
        }
        else if (!racingWheelDragging)
        {
            // Inertia вЂ” decays over ~3 s
            racingWheelAngle     += racingWheelAngularVel * dt;
            racingWheelAngularVel *= Mathf.Pow(0.975f, dt * 60f);
            // Spring return вЂ” proportional to angle (stronger the further from centre)
            racingWheelAngle -= racingWheelAngle * 2.8f * dt;
        }

        racingWheelAngle = Mathf.Clamp(racingWheelAngle, -360f, 360f);   // max В±1 full turn
        racingSteerInput = Mathf.Clamp(racingWheelAngle / 180f, -1f, 1f);
    }

    // Distance in screen pixels from mousePos to a world point projected via camera.
    private static float ScreenDist(Vector2 mousePos, Camera cam, Vector3 worldPos)
        => Vector2.Distance(mousePos, ScreenProject(cam, worldPos));

    private static Vector2 ScreenProject(Camera cam, Vector3 worldPos)
    {
        Vector3 vp = cam.WorldToViewportPoint(worldPos);
        return new Vector2(vp.x * Screen.width, vp.y * Screen.height);
    }

    private void UpdateSteeringWheel(float dt)
    {
        if (racingSteeringWheelRoot == null) return;
        // Apply current wheel angle directly вЂ” driven by mouse drag or keyboard
        racingSteeringWheelRoot.transform.localRotation = Quaternion.Euler(0f, racingWheelAngle, 0f);
    }

    // в”Ђв”Ђ Road buses в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void BuildTrackSampler()
    {
        racingSegCumLen = new float[raceSegments.Count];
        float cum = 0f;
        for (int i = 0; i < raceSegments.Count; i++)
        {
            racingSegCumLen[i] = cum;
            cum += raceSegments[i].Length;
        }
        racingTrackLen = cum;
    }

    // Returns road centreline position + segment rotation at distance t from start.
    // Extrapolates linearly before/after the track.
    private void GetTrackPoint(float t, out Vector3 pos, out Quaternion rot)
    {
        if (raceSegments.Count == 0) { pos = Vector3.zero; rot = Quaternion.identity; return; }

        int   segIdx = 0;
        float localT = 0f;

        if (t < 0f)
        {
            segIdx = 0;
            localT = t; // negative в†’ extrapolate behind start
        }
        else if (t >= racingTrackLen)
        {
            segIdx = raceSegments.Count - 1;
            localT = raceSegments[segIdx].Length + (t - racingTrackLen);
        }
        else
        {
            for (int i = raceSegments.Count - 1; i >= 0; i--)
            {
                if (racingSegCumLen[i] <= t)
                {
                    segIdx = i;
                    localT = t - racingSegCumLen[i];
                    break;
                }
            }
        }

        RaceSegment seg = raceSegments[segIdx];
        Vector3 fwd     = seg.Rotation * Vector3.forward;
        Vector3 segStart = seg.Center - fwd * seg.Length * 0.5f;
        pos = segStart + fwd * localT;
        float segFrac = seg.Length > 0.001f ? Mathf.Clamp01(localT / seg.Length) : 0f;
        pos.y = Mathf.Lerp(seg.StartY, seg.EndY, segFrac) + 0.35f;
        rot = seg.Rotation;
    }

    // Returns the terrain bump height at world XZ (add groundY for world Y).
    private float SampleTerrainY(float x, float z)
    {
        float h = 0f;
        foreach (var b in terrainBumps)
        {
            float dx = x - b.Center.x;
            float dz = z - b.Center.y;   // Center.y stores world Z (Vector2 convention)
            float denom = 2f * b.Radius * b.Radius;
            h += b.Height * Mathf.Exp(-(dx * dx + dz * dz) / denom);
        }
        return h;
    }

    // Returns terrain height suppressed to 0 near road segments (for ground mesh vertices).
    // Returns the world Y for a ground mesh vertex at (x,z).
    // Near road: blends toward the road segment's own height so ground follows the road.
    // Far from road: groundY + terrain bumps.
    private float SampleGroundMeshY(float x, float z, float groundY)
    {
        float minDist    = float.MaxValue;
        float nearRoadY  = groundY;
        Vector2 p = new Vector2(x, z);

        foreach (var seg in raceSegments)
        {
            Vector3 fwd   = seg.Rotation * Vector3.forward;
            Vector2 s2    = new Vector2(seg.Center.x - fwd.x * seg.Length * 0.5f,
                                        seg.Center.z - fwd.z * seg.Length * 0.5f);
            Vector2 e2    = new Vector2(seg.Center.x + fwd.x * seg.Length * 0.5f,
                                        seg.Center.z + fwd.z * seg.Length * 0.5f);
            float d = DistXZPointToSegment(p, s2, e2);
            if (d < minDist)
            {
                minDist = d;
                // Interpolated road surface Y at this XZ (slightly below road top)
                float t  = Mathf.Clamp01(Vector2.Dot(p - s2, (e2 - s2).normalized) / seg.Length);
                nearRoadY = Mathf.Lerp(seg.StartY, seg.EndY, t) - 0.35f;
            }
        }

        // mask: 0 = within 6 m of road centre в†’ use road Y; 1 = far away в†’ use terrain
        float mask      = Mathf.Clamp01((minDist - 6f) / 12f);
        float farY      = groundY + SampleTerrainY(x, z);
        return Mathf.Lerp(nearRoadY, farY, mask);
    }

    // Find the road surface Y at world XZ by projecting onto the nearest segment.
    private float SampleRaceRoadY(float wx, float wz)
    {
        float bestDistSq = float.MaxValue;
        float bestY      = 0.35f;

        foreach (RaceSegment seg in raceSegments)
        {
            Vector3 fwd    = seg.Rotation * Vector3.forward;
            float   startX = seg.Center.x - fwd.x * seg.Length * 0.5f;
            float   startZ = seg.Center.z - fwd.z * seg.Length * 0.5f;
            float   t      = Mathf.Clamp01(((wx - startX) * fwd.x + (wz - startZ) * fwd.z) / seg.Length);
            float   cx     = startX + fwd.x * seg.Length * t;
            float   cz     = startZ + fwd.z * seg.Length * t;
            float   dSq    = (wx - cx) * (wx - cx) + (wz - cz) * (wz - cz);

            if (dSq < bestDistSq)
            {
                bestDistSq = dSq;
                bestY      = Mathf.Lerp(seg.StartY, seg.EndY, t) + 0.35f;
            }
        }

        return bestY;
    }

    private void SpawnRacingBuses()
    {
        racingBuses.Clear();
        float len = racingTrackLen;

        // Two oncoming (left lane, orange), two same-direction (right lane, blue)
        SpawnBus(len * 0.25f,  9f, -1, -1.25f, new Color(0.80f, 0.35f, 0.20f));
        SpawnBus(len * 0.65f, 11f, -1, -1.25f, new Color(0.80f, 0.35f, 0.20f));
        SpawnBus(len * 0.15f,  5f, +1, +1.25f, new Color(0.20f, 0.38f, 0.65f));
        SpawnBus(len * 0.55f, 12f, +1, +1.25f, new Color(0.20f, 0.38f, 0.65f));
    }

    private void SpawnBus(float t, float speed, int dir, float laneOffset, Color bodyColor)
    {
        RacingBusData bus = new()
        {
            T = t, Speed = speed, Direction = dir,
            LaneOffset = laneOffset, CollisionRadius = 0.90f
        };

        // Root GO вЂ” positioned every frame
        bus.Root = new GameObject("RacingBus");

        Color roofColor = Color.Lerp(bodyColor, Color.black, 0.25f);
        Color wheelColor = new Color(0.14f, 0.14f, 0.16f);

        // Body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(body.GetComponent<Collider>());
        body.name = "BusBody";
        body.transform.SetParent(bus.Root.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        body.transform.localScale    = new Vector3(0.95f, 0.55f, 2.0f);
        ApplyColor(body, bodyColor);
        NoShadow(body);

        // Roof
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(roof.GetComponent<Collider>());
        roof.name = "BusRoof";
        roof.transform.SetParent(bus.Root.transform, false);
        roof.transform.localPosition = new Vector3(0f, 0.58f, 0f);
        roof.transform.localScale    = new Vector3(0.90f, 0.08f, 1.95f);
        ApplyColor(roof, roofColor);
        NoShadow(roof);

        // Wheels
        CreateBusWheel(bus.Root.transform, new Vector3(-0.52f, 0.10f,  0.65f), wheelColor);
        CreateBusWheel(bus.Root.transform, new Vector3(+0.52f, 0.10f,  0.65f), wheelColor);
        CreateBusWheel(bus.Root.transform, new Vector3(-0.52f, 0.10f, -0.65f), wheelColor);
        CreateBusWheel(bus.Root.transform, new Vector3(+0.52f, 0.10f, -0.65f), wheelColor);

        racingBuses.Add(bus);
    }

    private static void CreateBusWheel(Transform parent, Vector3 localPos, Color color)
    {
        GameObject w = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(w.GetComponent<Collider>());
        w.name = "BusWheel";
        w.transform.SetParent(parent, false);
        w.transform.localPosition = localPos;
        w.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        w.transform.localScale    = new Vector3(0.18f, 0.06f, 0.18f);
        ApplyColor(w, color);
        NoShadow(w);
    }

    private void UpdateRacingBuses(float dt)
    {
        if (racingBuses.Count == 0 || racingSegCumLen == null) return;

        float margin = 40f;
        float loop   = racingTrackLen + margin * 2f;

        for (int i = 0; i < racingBuses.Count; i++)
        {
            RacingBusData bus = racingBuses[i];

            // Advance along track
            bus.T += bus.Speed * bus.Direction * dt;

            // Wrap around
            if (bus.T > racingTrackLen + margin) bus.T -= loop;
            if (bus.T < -margin)                 bus.T += loop;

            // World position + rotation
            GetTrackPoint(bus.T, out Vector3 centre, out Quaternion segRot);
            Vector3 roadRight = segRot * Vector3.right;
            bus.Root.transform.position = centre + roadRight * bus.LaneOffset;
            bus.Root.transform.rotation = bus.Direction == -1
                ? segRot * Quaternion.Euler(0f, 180f, 0f)
                : segRot;

            // Collision with player truck
            Vector2 busXZ   = new(bus.Root.transform.position.x, bus.Root.transform.position.z);
            Vector2 truckXZ = new(racingTruckPos.x, racingTruckPos.z);
            Vector2 delta   = truckXZ - busXZ;
            float combined  = TruckCollisionRadius + bus.CollisionRadius;

            if (delta.sqrMagnitude < combined * combined)
            {
                float dist = delta.magnitude;
                Vector2 norm = dist > 0.001f ? delta / dist : Vector2.right;
                float pen = combined - dist;
                racingTruckPos.x += norm.x * pen;
                racingTruckPos.z += norm.y * pen;
                float vDotN = Vector2.Dot(racingVelocity, norm);
                if (vDotN < 0f)
                {
                    racingVelocity -= norm * vDotN;
                    racingVelocity *= (1f - CollisionEnergyLoss);
                }
                racingAngularVel += norm.x * CollisionAngularKick;
            }

            racingBuses[i] = bus;
        }
    }

    private void CleanupRacingBuses()
    {
        foreach (RacingBusData bus in racingBuses)
            if (bus.Root != null) Object.Destroy(bus.Root);
        racingBuses.Clear();
        racingSegCumLen = null;
        racingTrackLen  = 0f;
    }

    // в”Ђв”Ђ Lantern collision в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void UpdateLanternCollisions(float dt)
    {
        ProcessObstacleCollisions(racingLanterns, dt);
        ProcessObstacleCollisions(racingTreeObstacles, dt);
    }

    private void ProcessObstacleCollisions(List<RaceObstacleData> obstacles, float dt)
    {
        if (obstacles.Count == 0) return;

        Vector2 truckXZ = new Vector2(racingTruckPos.x, racingTruckPos.z);

        for (int i = 0; i < obstacles.Count; i++)
        {
            RaceObstacleData obs = obstacles[i];

            float combined = TruckCollisionRadius + obs.CollisionRadius;
            float dx       = truckXZ.x - obs.PoleXZ.x;
            float dz       = truckXZ.y - obs.PoleXZ.y;
            float distSq   = dx * dx + dz * dz;

            if (distSq < combined * combined)
            {
                float   dist = Mathf.Sqrt(distSq);
                Vector2 norm = dist > 0.001f
                    ? new Vector2(dx / dist, dz / dist)
                    : new Vector2(1f, 0f);
                float penetration = combined - dist;

                // Depenetrate
                racingTruckPos.x += norm.x * penetration;
                racingTruckPos.z += norm.y * penetration;

                // Velocity deflect
                float vDotN = Vector2.Dot(racingVelocity, norm);
                if (vDotN < 0f)
                {
                    racingVelocity -= norm * vDotN;
                    racingVelocity *= (1f - CollisionEnergyLoss);
                }

                // First contact: spin + tip
                if (!obs.IsTipped)
                {
                    float cross = racingVelocity.x * norm.y - racingVelocity.y * norm.x;
                    racingAngularVel += (cross >= 0f ? 1f : -1f) * CollisionAngularKick;

                    obs.IsTipped   = true;
                    obs.TiltTarget = LanternTiltTargetDeg;

                    Vector3 worldAxis = new Vector3(-norm.y, 0f, norm.x);
                    if (obs.Root != null)
                        obs.TiltAxisLocal = obs.Root.InverseTransformDirection(worldAxis);
                }
            }

            // Tipping animation
            if (!Mathf.Approximately(obs.TiltAngle, obs.TiltTarget))
            {
                obs.TiltAngle = Mathf.MoveTowards(
                    obs.TiltAngle, obs.TiltTarget, LanternTiltSpeed * dt);

                if (obs.Root != null)
                    obs.Root.localRotation = obs.OriginalLocalRot
                        * Quaternion.AngleAxis(obs.TiltAngle, obs.TiltAxisLocal);
            }

            obstacles[i] = obs;
        }
    }

    // в”Ђв”Ђ Track generation в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void GenerateRaceTrack()
    {
        raceSegments.Clear();

        racingSceneRoot = new GameObject("RacingScene");
        racingSceneRoot.transform.position = new Vector3(RaceTrackOffsetX, 0f, 0f);

        // Pre-generate joint heights (N+1 joints for N segments)
        float[] jointY = new float[RaceSegmentCount + 1];
        jointY[0] = 0f;
        float heightMomentum = 0f;
        for (int i = 1; i <= RaceSegmentCount; i++)
        {
            heightMomentum = Mathf.Lerp(heightMomentum, Random.Range(-3.0f, 3.0f), 0.45f);
            jointY[i] = Mathf.Clamp(jointY[i - 1] + heightMomentum, -4f, 7f);
        }

        Vector3 cursor    = new Vector3(RaceTrackOffsetX, 0f, 0f);
        float   direction = 0f; // degrees Y

        for (int i = 0; i < RaceSegmentCount; i++)
        {
            float segLen = Random.Range(14f, 28f);

            Quaternion rot = Quaternion.Euler(0f, direction, 0f);
            Vector3 fwd  = rot * Vector3.forward;
            float sy = jointY[i];
            float ey = jointY[i + 1];
            Vector3 segCenter = cursor + fwd * segLen * 0.5f;
            segCenter.y = (sy + ey) * 0.5f;

            RaceSegment seg = new()
            {
                Center   = segCenter,
                Rotation = rot,
                Length   = segLen,
                StartY   = sy,
                EndY     = ey,
            };
            raceSegments.Add(seg);

            // Road surface
            CreateRaceSegmentVisuals(seg);

            cursor += fwd * segLen;

            // Random turn for next segment (constrained to avoid impossible geometry)
            direction += Random.Range(-28f, 28f);
        }

        // Start marker (green)
        RaceSegment first = raceSegments[0];
        Vector3 startPos = first.Center - first.Rotation * Vector3.forward * first.Length * 0.45f;
        startPos.y = first.StartY + 0.14f;
        CreateRaceMarker(startPos, first.Rotation, new Color(0.18f, 0.82f, 0.28f));

        // Finish marker (yellow + light)
        RaceSegment last = raceSegments[raceSegments.Count - 1];
        raceFinishPos = last.Center + last.Rotation * Vector3.forward * last.Length * 0.45f;
        raceFinishPos.y = last.EndY + 0.35f;
        raceFinishFwd = last.Rotation * Vector3.forward;   // road direction вЂ” for strip detection
        CreateRaceMarker(raceFinishPos, last.Rotation, new Color(0.95f, 0.82f, 0.12f));

        // Finish light
        GameObject lightObj = new("FinishLight");
        lightObj.transform.SetParent(racingSceneRoot.transform, false);
        lightObj.transform.position = raceFinishPos + Vector3.up * 1.5f;
        Light fl = lightObj.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(1f, 0.90f, 0.45f);
        fl.intensity = 0.55f;
        fl.range = 7f;
        fl.shadows = LightShadows.None;

        // в”Ђв”Ђ Road extension beyond finish (decorative вЂ” truck drives into horizon) в”Ђв”Ђ
        raceExtensionSegments.Clear();
        float extStartY = last.EndY;
        Vector3 extCursor = raceFinishPos;
        extCursor.y = extStartY;
        Quaternion extRot = last.Rotation;
        Vector3 extFwd = extRot * Vector3.forward;
        for (int i = 0; i < 6; i++)
        {
            float extLen = 24f;
            Vector3 extCenter = extCursor + extFwd * extLen * 0.5f;
            extCenter.y = extStartY;
            RaceSegment ext = new() { Center = extCenter, Rotation = extRot, Length = extLen, StartY = extStartY, EndY = extStartY };
            CreateRaceSegmentVisuals(ext);
            raceExtensionSegments.Add(ext);
            extCursor += extFwd * extLen;
        }
    }

    // Compute a pitch-corrected rotation for a segment so road tiles slope with the terrain.
    private static Quaternion GetSegmentPitchedRot(RaceSegment seg)
    {
        Vector3 fwdFlat  = seg.Rotation * Vector3.forward;
        float   dy       = seg.EndY - seg.StartY;
        Vector3 fwdSloped = new Vector3(fwdFlat.x, dy / seg.Length, fwdFlat.z).normalized;
        return Quaternion.LookRotation(fwdSloped, Vector3.up);
    }

    private void CreateRaceSegmentVisuals(RaceSegment seg)
    {
        float w = 5.0f;
        Quaternion pitchedRot = GetSegmentPitchedRot(seg);
        Vector3 centre = seg.Center;

        // Road surface
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "RoadSeg";
        road.transform.SetParent(racingSceneRoot.transform, false);
        road.transform.position = centre + pitchedRot * new Vector3(0f, 0.06f, 0f);
        road.transform.rotation = pitchedRot;
        road.transform.localScale = new Vector3(w, 0.12f, seg.Length);
        ApplyColor(road, new Color(0.22f, 0.22f, 0.25f));
        ConfigureShadowVisual(road);

        // Left kerb
        CreateKerb(seg, pitchedRot, centre, -w * 0.5f - 0.14f);
        // Right kerb
        CreateKerb(seg, pitchedRot, centre, w * 0.5f + 0.14f);

        // Center dashed line (every other segment)
        if (Random.value > 0.5f)
        {
            GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dash.name = "CenterDash";
            dash.transform.SetParent(racingSceneRoot.transform, false);
            dash.transform.position = centre + pitchedRot * new Vector3(0f, 0.13f, 0f);
            dash.transform.rotation = pitchedRot;
            dash.transform.localScale = new Vector3(0.12f, 0.01f, seg.Length * 0.6f);
            ApplyColor(dash, new Color(0.92f, 0.88f, 0.56f));
            ConfigureShadowVisual(dash);
        }
    }

    private void CreateKerb(RaceSegment seg, Quaternion pitchedRot, Vector3 centre, float xOffset)
    {
        GameObject kerb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kerb.name = "Kerb";
        kerb.transform.SetParent(racingSceneRoot.transform, false);
        kerb.transform.position = centre + pitchedRot * new Vector3(xOffset, 0.09f, 0f);
        kerb.transform.rotation = pitchedRot;
        kerb.transform.localScale = new Vector3(0.28f, 0.18f, seg.Length);
        ApplyColor(kerb, new Color(0.72f, 0.72f, 0.72f));
        ConfigureShadowVisual(kerb);
    }

    private void CreateRaceMarker(Vector3 worldPos, Quaternion rot, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "RaceMarker";
        marker.transform.SetParent(racingSceneRoot.transform, false);
        marker.transform.position = worldPos; // caller supplies correct Y
        marker.transform.rotation = rot;
        marker.transform.localScale = new Vector3(5.2f, 0.06f, 1.1f);
        ApplyColor(marker, color);
        ConfigureShadowVisual(marker);
    }

    // в”Ђв”Ђ Racing truck в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void CreateRacingTruck()
    {
        racingTruckVisual = new GameObject("RacingTruck");
        racingTruckVisual.transform.localScale = Vector3.one * 1.6f;

        Color bodyColor   = new Color(0.85f, 0.20f, 0.18f);
        Color cabinColor  = new Color(0.95f, 0.82f, 0.28f);
        Color wheelColor  = new Color(0.14f, 0.14f, 0.14f);

        // в”Ђв”Ђ BodyGroup вЂ” both body cubes roll together (suspension) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        // Wheels are outside this group so they stay flat.
        GameObject bodyGroupObj = new("BodyGroup");
        bodyGroupObj.transform.SetParent(racingTruckVisual.transform, false);
        bodyGroupObj.transform.localPosition = Vector3.zero;
        racingBodyGroup = bodyGroupObj.transform;

        // Red cargo body вЂ” child of BodyGroup
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(racingBodyGroup, false);
        body.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        body.transform.localScale    = new Vector3(0.72f, 0.30f, 1.0f);
        ApplyColor(body, bodyColor);
        ConfigureShadowVisual(body);

        // CabinPivot inside BodyGroup вЂ” receives FWD delta Y so cabin steers ahead
        GameObject cabinPivotObj = new("CabinPivot");
        cabinPivotObj.transform.SetParent(racingBodyGroup, false);
        cabinPivotObj.transform.localPosition = Vector3.zero;
        racingCabinGroup = cabinPivotObj.transform;

        // Yellow cabin вЂ” child of CabinPivot (rolls with body, steers ahead)
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(racingCabinGroup, false);
        cabin.transform.localPosition = new Vector3(0f, 0.40f, 0.20f);
        cabin.transform.localScale    = new Vector3(0.58f, 0.34f, 0.44f);
        ApplyColor(cabin, cabinColor);
        ConfigureShadowVisual(cabin);

        // в”Ђв”Ђ FrontAxle вЂ” front wheels + headlights, steers for FWD visual в”Ђ
        // NOT a parent of body parts, so steering here doesn't tilt the body
        GameObject frontObj = new("FrontAxle");
        frontObj.transform.SetParent(racingTruckVisual.transform, false);
        frontObj.transform.localPosition = Vector3.zero;
        racingFrontAssembly = frontObj.transform;

        racingTruckWheelFL = CreateRacingWheel(racingFrontAssembly, new Vector3(-0.40f, 0.12f,  0.32f), wheelColor);
        racingTruckWheelFR = CreateRacingWheel(racingFrontAssembly, new Vector3( 0.40f, 0.12f,  0.32f), wheelColor);

        // Headlights on FrontAxle вЂ” point forward regardless of body roll
        racingHeadlightL = CreateRacingHeadlight(racingFrontAssembly, new Vector3(-0.28f, 0.28f, 0.52f));
        racingHeadlightR = CreateRacingHeadlight(racingFrontAssembly, new Vector3( 0.28f, 0.28f, 0.52f));

        // в”Ђв”Ђ Rear wheels вЂ” on root, flat в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        racingTruckWheelRL = CreateRacingWheel(racingTruckVisual.transform, new Vector3(-0.40f, 0.12f, -0.32f), wheelColor);
        racingTruckWheelRR = CreateRacingWheel(racingTruckVisual.transform, new Vector3( 0.40f, 0.12f, -0.32f), wheelColor);
    }

    private Light CreateRacingHeadlight(Transform parent, Vector3 localPos)
    {
        GameObject go = new("RacingHeadlight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);

        // Visible lens вЂ” unlit bright white disc so it's always visible as a glowing element
        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(lens.GetComponent<Collider>());
        lens.name = "HeadlightLens";
        lens.transform.SetParent(go.transform, false);
        lens.transform.localPosition = Vector3.zero;
        lens.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // face forward
        lens.transform.localScale    = new Vector3(0.10f, 0.015f, 0.10f);
        Renderer lensR = lens.GetComponent<Renderer>();
        Shader unlitShader = ShaderRefs.Unlit ?? ShaderRefs.Sprites;
        Material lensMat = unlitShader != null ? new Material(unlitShader) : lensR.material;
        lensMat.color = new Color(1f, 0.97f, 0.85f);
        if (lensMat.HasProperty("_BaseColor")) lensMat.SetColor("_BaseColor", lensMat.color);
        lensR.material = lensMat;
        lensR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Actual spot light
        Light l = go.AddComponent<Light>();
        l.type      = LightType.Spot;
        l.spotAngle = 55f;
        l.innerSpotAngle = 22f;
        l.range     = 32f;
        l.color     = new Color(1f, 0.96f, 0.82f);
        l.intensity = 1.2f;
        l.shadows   = LightShadows.Soft;
        l.enabled   = true;
        return l;
    }

    private Transform CreateRacingWheel(Transform parent, Vector3 localPos, Color color)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.transform.SetParent(parent, false);
        wheel.transform.localPosition = localPos;
        // Euler(0, 0, -90): cylinder axis (local Y) points along world X (left-right)
        // flat circles face sideways, curved surface rolls forward/back correctly
        wheel.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        // x = z = radius scale (0.28 в†’ diameter 0.28), y = half-thickness (0.065 в†’ 0.13 wide)
        wheel.transform.localScale    = new Vector3(0.28f, 0.065f, 0.28f);
        ApplyColor(wheel, color);
        ConfigureShadowVisual(wheel);
        return wheel.transform;
    }

    // в”Ђв”Ђ Racing camera в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void SetupRacingCamera()
    {
        GameObject camObj = new("RacingCamera");
        racingCamera = camObj.AddComponent<Camera>();
        racingCamera.orthographic    = false;
        racingCamera.fieldOfView     = 65f;
        racingCamera.clearFlags      = CameraClearFlags.SolidColor;
        racingCamera.backgroundColor = new Color(0.38f, 0.62f, 0.92f);
        racingCamera.depth           = mainCamera != null ? mainCamera.depth + 10 : 10;
        racingCamera.cullingMask     = ~0;
        racingCamera.nearClipPlane   = 0.1f;
        racingCamera.farClipPlane    = 600f;

        Quaternion initRot = Quaternion.Euler(14f, racingTruckAngle, 0f);
        racingCamera.transform.position = racingTruckPos + Vector3.up * 1.4f + initRot * Vector3.back * 2.8f;
        racingCamera.transform.rotation = initRot;

        // Directional light вЂ” provides ambient shadows for the whole race scene
        GameObject dirObj = new("RacingDirectionalLight");
        dirObj.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        racingDirectionalLight = dirObj.AddComponent<Light>();
        racingDirectionalLight.type      = LightType.Directional;
        racingDirectionalLight.intensity = 1.0f;
        racingDirectionalLight.color     = new Color(1f, 0.95f, 0.82f);
        racingDirectionalLight.shadows   = LightShadows.Soft;

        // Expand shadow distance so the whole visible road receives shadows
        racingSavedShadowDistance          = QualitySettings.shadowDistance;
        QualitySettings.shadowDistance     = 120f;

        SetupRacingSkydome();
    }

    private static void NoShadow(GameObject go)
    {
        if (go.TryGetComponent(out Renderer r))
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows    = false;
        }
    }

    // в”Ђв”Ђ 3D Steering wheel вЂ” child of racing camera в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    //
    // The wheel is parented directly to racingCamera so it stays fixed
    // in screen space (bottom-centre) like a dashboard prop.
    // It lies flat in the camera's local XZ plane, spinning around local Y.

    private void CreateSteeringWheel()
    {
        if (racingCamera == null) return;

        // Anchor: child of camera вЂ” lower and more tilted forward so it's below the truck visual
        // X-tilt of -55В° means the wheel face tilts toward the player (dashboard angle)
        GameObject anchor = new("SteeringWheelAnchor");
        anchor.transform.SetParent(racingCamera.transform, false);
        anchor.transform.localPosition = new Vector3(0f, -0.80f, 1.1f); // lower, slightly closer
        anchor.transform.localRotation = Quaternion.Euler(-55f, 0f, 0f); // steep forward tilt

        // Dedicated backlight so the wheel is always visible regardless of scene lighting
        GameObject lightObj = new("WheelBacklight");
        lightObj.transform.SetParent(anchor.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.6f, 0f); // above the wheel face
        Light wl = lightObj.AddComponent<Light>();
        wl.type      = LightType.Point;
        wl.intensity = 2.2f;
        wl.range     = 1.4f;
        wl.color     = new Color(1f, 0.92f, 0.78f); // warm dashboard glow

        // Spinner: child of anchor вЂ” this is what rotates each frame
        racingSteeringWheelRoot = new("SteeringWheelRoot");
        racingSteeringWheelRoot.transform.SetParent(anchor.transform, false);
        racingSteeringWheelRoot.transform.localPosition = Vector3.zero;
        racingSteeringWheelRoot.transform.localRotation = Quaternion.identity;

        Color hubColor   = new Color(0.30f, 0.28f, 0.32f);
        Color spokeColor = new Color(0.25f, 0.23f, 0.26f);
        Color rimColor   = new Color(0.22f, 0.20f, 0.24f);
        Transform root   = racingSteeringWheelRoot.transform;
        float rimRadius  = 0.30f;

        // Yellow dot at top of rim вЂ” moves WITH the wheel, shows rotation amount
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(marker.GetComponent<Collider>());
        marker.name = "WheelCenterMarker";
        marker.transform.SetParent(root, false);
        marker.transform.localPosition = new Vector3(0f, 0.022f, rimRadius);
        marker.transform.localRotation = Quaternion.identity;
        marker.transform.localScale    = new Vector3(0.042f, 0.022f, 0.028f);
        NoShadow(marker);
        ApplyColor(marker, new Color(1f, 0.88f, 0.08f));

        // Hub вЂ” flat cylinder, lies in XZ plane (Y is the face normal)
        GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(hub.GetComponent<Collider>());
        hub.name = "WheelHub";
        hub.transform.SetParent(root, false);
        hub.transform.localPosition = Vector3.zero;
        hub.transform.localScale    = new Vector3(0.10f, 0.012f, 0.10f);
        NoShadow(hub);
        ApplyColor(hub, hubColor);

        // 3 spokes вЂ” radiate from hub edge outward to rim, 120В° apart
        // Each spoke's LOCAL position must be computed along its own direction,
        // otherwise all three shift to the same offset in parent space.
        float spokeLen    = rimRadius - 0.06f;        // 0.24 units long
        float spokeCentre = 0.06f + spokeLen * 0.5f; // 0.18: midpoint along spoke axis
        for (int i = 0; i < 3; i++)
        {
            float rad = i * 120f * Mathf.Deg2Rad;
            // Centre of this spoke in root-local XZ space
            float sx = Mathf.Sin(rad) * spokeCentre;
            float sz = Mathf.Cos(rad) * spokeCentre;

            GameObject spoke = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(spoke.GetComponent<Collider>());
            spoke.name = $"Spoke{i}";
            spoke.transform.SetParent(root, false);
            spoke.transform.localPosition = new Vector3(sx, 0f, sz);
            spoke.transform.localRotation = Quaternion.Euler(0f, i * 120f, 0f);
            spoke.transform.localScale    = new Vector3(0.040f, 0.014f, spokeLen);
            NoShadow(spoke);
            ApplyColor(spoke, spokeColor);
        }

        // Outer rim вЂ” 8 flat bars forming a connected octagon
        // Each bar is placed at the midpoint of its chord and rotated +90В° so its
        // long axis (local Z) is tangential to the circle, not radial.
        int   rimCount  = 8;
        // chord length between adjacent vertices: 2rВ·sin(ПЂ/n), +6% overlap for clean joins
        float chordLen  = 2f * rimRadius * Mathf.Sin(Mathf.PI / rimCount) * 1.06f;
        for (int i = 0; i < rimCount; i++)
        {
            float midA = (i + 0.5f) / rimCount * Mathf.PI * 2f; // angle at mid-chord
            float mx   = Mathf.Sin(midA) * rimRadius;
            float mz   = Mathf.Cos(midA) * rimRadius;

            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(seg.GetComponent<Collider>());
            seg.name = $"Rim{i}";
            seg.transform.SetParent(root, false);
            seg.transform.localPosition = new Vector3(mx, 0f, mz);
            // +90В° makes local Z tangential (along the rim edge) instead of radial
            seg.transform.localRotation = Quaternion.Euler(0f, midA * Mathf.Rad2Deg + 90f, 0f);
            seg.transform.localScale    = new Vector3(0.055f, 0.018f, chordLen);
            NoShadow(seg);
            ApplyColor(seg, rimColor);
        }

        racingWheelAngle = 0f;
    }

    // в”Ђв”Ђ Pedals в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void CreateRacingPedals()
    {
        if (racingCamera == null) return;

        // Anchor: left of the steering wheel, raised so it stays inside camera frustum
        // Rule: |y/z| < tan(FOV/2=32.5В°)=0.637  в†’  y must be > -0.637*z
        // With z=1.0: y must be > -0.637, so -0.55 is safe
        GameObject anchor = new("PedalAnchor");
        anchor.transform.SetParent(racingCamera.transform, false);
        anchor.transform.localPosition = new Vector3(-0.54f, -0.55f, 1.0f);
        anchor.transform.localRotation = Quaternion.Euler(-55f, 5f, 0f); // same tilt as steering wheel

        // Shared backlight
        GameObject lightObj = new("PedalLight");
        lightObj.transform.SetParent(anchor.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        Light pl = lightObj.AddComponent<Light>();
        pl.type      = LightType.Point;
        pl.intensity = 1.8f;
        pl.range     = 1.2f;
        pl.color     = new Color(1f, 0.92f, 0.78f);

        // Brake (left) вЂ” red tint
        racingPedalBrake = CreateSinglePedal("BrakePedal", anchor.transform,
            new Vector3(-0.13f, 0f, 0f), new Color(0.55f, 0.12f, 0.12f));

        // Gas (right) вЂ” green tint
        racingPedalGas = CreateSinglePedal("GasPedal", anchor.transform,
            new Vector3(0.13f, 0f, 0f), new Color(0.14f, 0.44f, 0.14f));
    }

    private Transform CreateSinglePedal(string pedName, Transform parent, Vector3 offset, Color surfaceColor)
    {
        Color stemColor = new Color(0.18f, 0.18f, 0.20f);

        GameObject root = new(pedName);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = offset;
        root.transform.localRotation = Quaternion.identity;

        // Stem вЂ” thin vertical post
        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(stem.GetComponent<Collider>());
        stem.name = "Stem";
        stem.transform.SetParent(root.transform, false);
        stem.transform.localPosition = new Vector3(0f, -0.08f, -0.02f);
        stem.transform.localScale    = new Vector3(0.032f, 0.14f, 0.032f);
        NoShadow(stem);
        ApplyColor(stem, stemColor);

        // Surface вЂ” flat pedal plate
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(surface.GetComponent<Collider>());
        surface.name = "Surface";
        surface.transform.SetParent(root.transform, false);
        surface.transform.localPosition = new Vector3(0f, 0f, 0f);
        surface.transform.localScale    = new Vector3(0.11f, 0.018f, 0.15f);
        NoShadow(surface);
        ApplyColor(surface, surfaceColor);

        return root.transform;
    }

    private void UpdatePedals(float dt, float throttle, bool braking)
    {
        UpdateSinglePedal(racingPedalGas,   dt, throttle > 0.05f);
        UpdateSinglePedal(racingPedalBrake, dt, braking);
    }

    private void UpdateSinglePedal(Transform pedal, float dt, bool pressed)
    {
        if (pedal == null) return;
        float target = pressed ? 18f : 0f;
        float cur = pedal.localEulerAngles.x;
        if (cur > 180f) cur -= 360f;
        pedal.localRotation = Quaternion.Euler(Mathf.Lerp(cur, target, 14f * dt), 0f, 0f);
    }

    // в”Ђв”Ђ Gear shift в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void CreateGearShift()
    {
        if (racingCamera == null) return;

        // Mirror of pedal anchor вЂ” right side of steering wheel
        GameObject anchor = new("GearShiftAnchor");
        anchor.transform.SetParent(racingCamera.transform, false);
        anchor.transform.localPosition = new Vector3(0.54f, -0.55f, 1.0f);
        anchor.transform.localRotation = Quaternion.Euler(-55f, -5f, 0f);

        // Backlight
        GameObject lightObj = new("GearLight");
        lightObj.transform.SetParent(anchor.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        Light gl = lightObj.AddComponent<Light>();
        gl.type      = LightType.Point;
        gl.intensity = 1.8f;
        gl.range     = 1.0f;
        gl.color     = new Color(1f, 0.92f, 0.78f);

        // Gear shift root вЂ” this tilts forward/backward
        racingGearShift = new GameObject("GearShiftRoot").transform;
        racingGearShift.SetParent(anchor.transform, false);
        racingGearShift.localPosition = Vector3.zero;
        racingGearShift.localRotation = Quaternion.identity;

        Color stickColor = new Color(0.20f, 0.20f, 0.22f);
        Color knobColor  = new Color(0.28f, 0.26f, 0.30f);

        // Stick вЂ” thin vertical rod
        GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(stick.GetComponent<Collider>());
        stick.name = "Stick";
        stick.transform.SetParent(racingGearShift, false);
        stick.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        stick.transform.localScale    = new Vector3(0.032f, 0.22f, 0.032f);
        NoShadow(stick);
        ApplyColor(stick, stickColor);

        // Knob вЂ” sphere-ish cube on top
        GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(knob.GetComponent<Collider>());
        knob.name = "Knob";
        knob.transform.SetParent(racingGearShift, false);
        knob.transform.localPosition = new Vector3(0f, 0.17f, 0f);
        knob.transform.localScale    = new Vector3(0.07f, 0.07f, 0.07f);
        NoShadow(knob);
        ApplyColor(knob, knobColor);

        // Gear label on knob вЂ” tiny flat cubes for "D" and "R" indicator
        // Forward indicator: small yellow stripe on front of knob
        GameObject fwdDot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(fwdDot.GetComponent<Collider>());
        fwdDot.name = "GearForwardDot";
        fwdDot.transform.SetParent(racingGearShift, false);
        fwdDot.transform.localPosition = new Vector3(0f, 0.17f, 0.04f);
        fwdDot.transform.localScale    = new Vector3(0.022f, 0.022f, 0.008f);
        NoShadow(fwdDot);
        ApplyColor(fwdDot, new Color(1f, 0.88f, 0.08f)); // yellow = forward
    }

    private void UpdateGearShift(float dt)
    {
        if (racingGearShift == null) return;
        float target = GearShiftAngles[Mathf.Clamp(racingCurrentGear, 0, 4)];
        float cur = racingGearShift.localEulerAngles.x;
        if (cur > 180f) cur -= 360f;
        racingGearShift.localRotation = Quaternion.Euler(Mathf.Lerp(cur, target, 6f * dt), 0f, 0f);
    }

    private void UpdateAutoGear(float fwdDot, float kmh, float dt)
    {
        racingGearChangeTimer -= dt;

        // Reversing вЂ” triggered by actual velocity direction
        if (fwdDot < -0.2f)
        {
            if (racingCurrentGear != 0)
            {
                racingCurrentGear = 0;
                racingGearChangeTimer = GearChangeCooldown;
            }
            return;
        }

        // Coming out of R вЂ” jump to 1st
        if (racingCurrentGear == 0)
        {
            racingCurrentGear = 1;
            racingGearChangeTimer = GearChangeCooldown;
            return;
        }

        if (racingGearChangeTimer > 0f) return;

        // Upshift
        if (racingCurrentGear < 4 && kmh > GearUpKmh[racingCurrentGear])
        {
            racingCurrentGear++;
            racingGearChangeTimer = GearChangeCooldown;
            return;
        }

        // Downshift
        if (racingCurrentGear > 1 && kmh < GearDownKmh[racingCurrentGear])
        {
            racingCurrentGear--;
            racingGearChangeTimer = GearChangeCooldown;
        }
    }

    private void SetupRacingSkydome()
    {
        racingSkydome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        racingSkydome.name = "RacingSkydome";
        Object.Destroy(racingSkydome.GetComponent<Collider>());

        // Negative X scale flips winding order вЂ” renders from the inside
        racingSkydome.transform.localScale = new Vector3(-480f, 480f, 480f);

        racingSkydomeRenderer = racingSkydome.GetComponent<Renderer>();
        racingSkydomeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        racingSkydomeRenderer.receiveShadows    = false;

        // Unlit material so it isn't affected by scene lighting
        Shader unlitShader = ShaderRefs.Unlit ?? ShaderRefs.Sprites;
        Material mat = unlitShader != null
            ? new Material(unlitShader)
            : racingSkydomeRenderer.material;
        racingSkydomeRenderer.material = mat;
    }

    private void UpdateRacingSkydome()
    {
        if (racingSkydome == null || racingSkydomeRenderer == null) return;

        // Follow camera so the dome is always around us
        if (racingCamera != null)
            racingSkydome.transform.position = racingCamera.transform.position;

        // 4-stop sky gradient keyed to daylight
        //  0 = full night,  1 = full day
        float dl = Mathf.Clamp01(currentStylizedDaylight);

        Color nightColor   = new Color(0.03f, 0.04f, 0.09f);  // deep navy
        Color dawnColor    = new Color(0.72f, 0.32f, 0.12f);  // burnt orange
        Color morningColor = new Color(0.82f, 0.56f, 0.28f);  // warm gold
        Color dayColor     = new Color(0.38f, 0.62f, 0.92f);  // clear blue

        Color skyColor;
        if      (dl < 0.25f) skyColor = Color.Lerp(nightColor,   dawnColor,    dl / 0.25f);
        else if (dl < 0.55f) skyColor = Color.Lerp(dawnColor,    morningColor, (dl - 0.25f) / 0.30f);
        else                 skyColor = Color.Lerp(morningColor,  dayColor,     (dl - 0.55f) / 0.45f);

        racingSkydomeRenderer.material.color = skyColor;

        // Also tint camera background to match (visible if dome ever has gaps)
        if (racingCamera != null)
            racingCamera.backgroundColor = skyColor;

        // Directional light intensity follows daylight: night ~0.1, day ~1.1
        if (racingDirectionalLight != null)
        {
            racingDirectionalLight.intensity = Mathf.Lerp(0.10f, 1.10f, dl);
            // Shift color from cool night to warm day
            racingDirectionalLight.color = skyColor * 1.1f;
        }
    }

    // в”Ђв”Ђ Racing HUD в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void SetupRacingHud()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObj = new("RacingHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder  = 100;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode   = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        racingHudCanvas = canvas;

        // Panel anchored top-left
        RectTransform panel = CreateUiObject("RacingHudPanel", canvasObj.transform).GetComponent<RectTransform>();
        panel.anchorMin        = new Vector2(0f, 1f);
        panel.anchorMax        = new Vector2(0f, 1f);
        panel.pivot            = new Vector2(0f, 1f);
        panel.anchoredPosition = new Vector2(18f, -18f);
        panel.sizeDelta        = new Vector2(220f, 160f);

        Image panelBg = panel.gameObject.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.05f, 0.08f, 0.80f);
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor    = new Color(0.88f, 0.62f, 0.08f, 0.5f);
        outline.effectDistance = new Vector2(2f, -2f);

        racingHudText = new GameObject("RacingHudText").AddComponent<Text>();
        racingHudText.transform.SetParent(panel, false);
        racingHudText.rectTransform.anchorMin = Vector2.zero;
        racingHudText.rectTransform.anchorMax = Vector2.one;
        racingHudText.rectTransform.offsetMin = new Vector2(12f, 10f);
        racingHudText.rectTransform.offsetMax = new Vector2(-12f, -10f);
        racingHudText.font      = font;
        racingHudText.fontSize  = 16;
        racingHudText.color     = new Color(0.92f, 0.90f, 0.86f);
        racingHudText.alignment = TextAnchor.UpperLeft;
        racingHudText.text      = "";

        RectTransform hintPanel = CreateUiObject("RacingControlHintPanel", canvasObj.transform).GetComponent<RectTransform>();
        hintPanel.anchorMin        = new Vector2(1f, 1f);
        hintPanel.anchorMax        = new Vector2(1f, 1f);
        hintPanel.pivot            = new Vector2(1f, 1f);
        hintPanel.anchoredPosition = new Vector2(-18f, -18f);
        hintPanel.sizeDelta        = new Vector2(440f, 132f);

        Image hintBg = hintPanel.gameObject.AddComponent<Image>();
        hintBg.color = new Color(0.04f, 0.05f, 0.08f, 0.86f);
        Outline hintOutline = hintPanel.gameObject.AddComponent<Outline>();
        hintOutline.effectColor    = new Color(0.88f, 0.62f, 0.08f, 0.55f);
        hintOutline.effectDistance = new Vector2(2f, -2f);

        racingControlHintText = new GameObject("RacingControlHintText").AddComponent<Text>();
        racingControlHintText.transform.SetParent(hintPanel, false);
        racingControlHintText.rectTransform.anchorMin = Vector2.zero;
        racingControlHintText.rectTransform.anchorMax = Vector2.one;
        racingControlHintText.rectTransform.offsetMin = new Vector2(18f, 12f);
        racingControlHintText.rectTransform.offsetMax = new Vector2(-18f, -12f);
        racingControlHintText.font      = font;
        racingControlHintText.fontSize  = 17;
        racingControlHintText.color     = new Color(0.96f, 0.93f, 0.84f, 1f);
        racingControlHintText.alignment = TextAnchor.MiddleLeft;
        racingControlHintText.text = IsRussianLanguage()
            ? "Управление\n" +
              "Мышь: руль\n" +
              "W / ↑: газ\n" +
              "S / ↓: тормоз / назад\n" +
              "ESC: выйти"
            : "Controls\n" +
              "Mouse: steering\n" +
              "W / ↑: throttle\n" +
              "S / ↓: brake / reverse\n" +
              "ESC: exit";

        SetupSpeedometer(canvasObj.transform, font);
    }

    private void SetupSpeedometer(Transform canvasParent, Font font)
    {
        const float size   = 160f;
        const float radius = 62f; // tick ring radius from center

        // Root вЂ” bottom-right corner
        RectTransform root = CreateUiObject("Speedometer", canvasParent).GetComponent<RectTransform>();
        root.anchorMin        = new Vector2(1f, 0f);
        root.anchorMax        = new Vector2(1f, 0f);
        root.pivot            = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-18f, 18f);
        root.sizeDelta        = new Vector2(size, size);

        Image bg = root.gameObject.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.08f, 0.85f);
        Outline bgOutline = root.gameObject.AddComponent<Outline>();
        bgOutline.effectColor    = new Color(0.88f, 0.62f, 0.08f, 0.5f);
        bgOutline.effectDistance = new Vector2(2f, -2f);

        // Arc tick marks: 0вЂ“100 km/h в†’ -135В° to +135В° from vertical (CW positive)
        Color tickDim    = new Color(0.55f, 0.55f, 0.55f);
        Color tickBright = new Color(0.95f, 0.82f, 0.20f);
        int tickCount = 11; // 0, 10, 20 вЂ¦ 100
        for (int i = 0; i < tickCount; i++)
        {
            float t        = i / (float)(tickCount - 1);
            float angleDeg = -135f + t * 270f; // CW from vertical
            float angleRad = angleDeg * Mathf.Deg2Rad;

            RectTransform tick = CreateUiObject($"Tick{i}", root).GetComponent<RectTransform>();
            tick.anchorMin        = new Vector2(0.5f, 0.5f);
            tick.anchorMax        = new Vector2(0.5f, 0.5f);
            tick.pivot            = new Vector2(0.5f, 0f); // pivot at rim, extends inward
            tick.anchoredPosition = new Vector2(Mathf.Sin(angleRad) * radius, Mathf.Cos(angleRad) * radius);
            bool isMajor          = (i % 2 == 0);
            tick.sizeDelta        = new Vector2(isMajor ? 3f : 2f, isMajor ? 13f : 8f);
            tick.localEulerAngles = new Vector3(0f, 0f, -angleDeg);

            Image tickImg = tick.gameObject.AddComponent<Image>();
            tickImg.color = (i == 0 || i == tickCount - 1) ? tickBright : tickDim;
        }

        // Speed number labels at 0, 50, 100
        (int kmh, float angleDeg)[] labels = { (0, -135f), (50, 0f), (100, 135f) };
        foreach (var (lKmh, lAngle) in labels)
        {
            float rad = lAngle * Mathf.Deg2Rad;
            float lx  = Mathf.Sin(rad) * (radius + 16f);
            float ly  = Mathf.Cos(rad) * (radius + 16f);

            Text lbl = new GameObject($"Label{lKmh}").AddComponent<Text>();
            lbl.transform.SetParent(root, false);
            lbl.rectTransform.anchorMin        = new Vector2(0.5f, 0.5f);
            lbl.rectTransform.anchorMax        = new Vector2(0.5f, 0.5f);
            lbl.rectTransform.pivot            = new Vector2(0.5f, 0.5f);
            lbl.rectTransform.anchoredPosition = new Vector2(lx, ly);
            lbl.rectTransform.sizeDelta        = new Vector2(34f, 18f);
            lbl.font      = font;
            lbl.fontSize  = 11;
            lbl.color     = new Color(0.68f, 0.68f, 0.68f);
            lbl.alignment = TextAnchor.MiddleCenter;
            lbl.text      = lKmh.ToString();
        }

        // Needle (pivot at bottom-center so it rotates from center of gauge)
        RectTransform needle = CreateUiObject("Needle", root).GetComponent<RectTransform>();
        needle.anchorMin        = new Vector2(0.5f, 0.5f);
        needle.anchorMax        = new Vector2(0.5f, 0.5f);
        needle.pivot            = new Vector2(0.5f, 0.08f); // slightly below center for tail
        needle.anchoredPosition = new Vector2(0f, 0f);
        needle.sizeDelta        = new Vector2(3.5f, 58f);
        needle.localEulerAngles = new Vector3(0f, 0f, 150f); // starts at 0 km/h

        Image needleImg = needle.gameObject.AddComponent<Image>();
        needleImg.color = new Color(0.96f, 0.28f, 0.18f);

        racingSpeedometerNeedle = needle;

        // Center cap
        RectTransform cap = CreateUiObject("Cap", root).GetComponent<RectTransform>();
        cap.anchorMin        = new Vector2(0.5f, 0.5f);
        cap.anchorMax        = new Vector2(0.5f, 0.5f);
        cap.pivot            = new Vector2(0.5f, 0.5f);
        cap.anchoredPosition = Vector2.zero;
        cap.sizeDelta        = new Vector2(12f, 12f);
        cap.gameObject.AddComponent<Image>().color = new Color(0.86f, 0.22f, 0.16f);

        // Speed readout вЂ” center-bottom of gauge
        racingSpeedometerText = new GameObject("SpeedReadout").AddComponent<Text>();
        racingSpeedometerText.transform.SetParent(root, false);
        racingSpeedometerText.rectTransform.anchorMin        = new Vector2(0f, 0f);
        racingSpeedometerText.rectTransform.anchorMax        = new Vector2(1f, 0f);
        racingSpeedometerText.rectTransform.pivot            = new Vector2(0.5f, 0f);
        racingSpeedometerText.rectTransform.anchoredPosition = new Vector2(0f, 12f);
        racingSpeedometerText.rectTransform.sizeDelta        = new Vector2(0f, 28f);
        racingSpeedometerText.font      = font;
        racingSpeedometerText.fontSize  = 22;
        racingSpeedometerText.fontStyle = FontStyle.Bold;
        racingSpeedometerText.color     = new Color(0.95f, 0.90f, 0.84f);
        racingSpeedometerText.alignment = TextAnchor.MiddleCenter;
        racingSpeedometerText.text      = "0";

        // "km/h" sub-label
        Text unit = new GameObject("UnitLabel").AddComponent<Text>();
        unit.transform.SetParent(root, false);
        unit.rectTransform.anchorMin        = new Vector2(0f, 0f);
        unit.rectTransform.anchorMax        = new Vector2(1f, 0f);
        unit.rectTransform.pivot            = new Vector2(0.5f, 0f);
        unit.rectTransform.anchoredPosition = new Vector2(0f, 34f);
        unit.rectTransform.sizeDelta        = new Vector2(0f, 16f);
        unit.font      = font;
        unit.fontSize  = 10;
        unit.color     = new Color(0.55f, 0.55f, 0.55f);
        unit.alignment = TextAnchor.MiddleCenter;
        unit.text      = "km/h";

        // Gear indicator вЂ” sits above the speed readout in the center of the dial
        racingGearText = new GameObject("GearReadout").AddComponent<Text>();
        racingGearText.transform.SetParent(root, false);
        racingGearText.rectTransform.anchorMin        = new Vector2(0f, 1f);
        racingGearText.rectTransform.anchorMax        = new Vector2(1f, 1f);
        racingGearText.rectTransform.pivot            = new Vector2(0.5f, 1f);
        racingGearText.rectTransform.anchoredPosition = new Vector2(0f, -14f);
        racingGearText.rectTransform.sizeDelta        = new Vector2(0f, 30f);
        racingGearText.font      = font;
        racingGearText.fontSize  = 22;
        racingGearText.fontStyle = FontStyle.Bold;
        racingGearText.color     = new Color(0.95f, 0.90f, 0.84f);
        racingGearText.alignment = TextAnchor.MiddleCenter;
        racingGearText.text      = "1";
    }

    // в”Ђв”Ђ Racing world population в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void PopulateRacingWorld()
    {
        racingLanterns.Clear();
        racingTreeObstacles.Clear();
        racingFlowerPoints.Clear();

        // Bounding center of the whole track
        Vector3 center = Vector3.zero;
        foreach (var seg in raceSegments) center += seg.Center;
        center /= raceSegments.Count;
        center.y = 0f;

        // в”Ђв”Ђ Ground base level в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        float minSegY = float.MaxValue;
        foreach (var s in raceSegments)
        {
            if (s.StartY < minSegY) minSegY = s.StartY;
            if (s.EndY   < minSegY) minSegY = s.EndY;
        }
        float groundY = (minSegY < float.MaxValue ? minSegY : 0f) - 1.5f;
        racingGroundY = groundY;

        // в”Ђв”Ђ Terrain bumps (hills + mountains) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        terrainBumps.Clear();

        // Track bounding radius вЂ” used to place mountains well outside the track
        float trackBoundsRadius = 0f;
        foreach (var s in raceSegments)
        {
            float d = Vector2.Distance(new Vector2(center.x, center.z), new Vector2(s.Center.x, s.Center.z));
            if (d + s.Length * 0.5f > trackBoundsRadius) trackBoundsRadius = d + s.Length * 0.5f;
        }

        // Hills вЂ” moderate bumps anywhere in the world area
        for (int i = 0; i < 18; i++)
        {
            terrainBumps.Add(new TerrainBump
            {
                Center = new Vector2(center.x + Random.Range(-320f, 320f),
                                     center.z + Random.Range(-320f, 320f)),
                Radius = Random.Range(22f, 48f),
                Height = Random.Range(1.8f, 5.5f),
            });
        }

        // Mountains вЂ” large, tall, placed far from track
        int mountainsPlaced = 0;
        for (int attempt = 0; attempt < 300 && mountainsPlaced < 7; attempt++)
        {
            float angle  = Random.Range(0f, Mathf.PI * 2f);
            float dist   = trackBoundsRadius + Random.Range(90f, 230f);
            Vector2 cand = new Vector2(center.x + Mathf.Cos(angle) * dist,
                                       center.z + Mathf.Sin(angle) * dist);

            // Reject if too close to any segment centre
            bool tooClose = false;
            foreach (var s in raceSegments)
            {
                float dx = cand.x - s.Center.x;
                float dz = cand.y - s.Center.z;
                if (dx * dx + dz * dz < 95f * 95f) { tooClose = true; break; }
            }
            if (tooClose) continue;

            terrainBumps.Add(new TerrainBump
            {
                Center = cand,
                Radius = Random.Range(65f, 125f),
                Height = Random.Range(10f, 24f),
            });
            mountainsPlaced++;
        }

        // в”Ђв”Ђ Procedural ground mesh в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        const int   GridN     = 88;      // 88Г—88 quads = 89ВІ = 7921 verts (safely under 65535)
        const float WorldSize = 900f;
        float step    = WorldSize / GridN;
        float originX = center.x - WorldSize * 0.5f;
        float originZ = center.z - WorldSize * 0.5f;
        Vector3 rootOfs = racingSceneRoot.transform.position; // parent offset for local coords

        int vCount = (GridN + 1) * (GridN + 1);
        int tCount = GridN * GridN * 6;
        Vector3[] verts = new Vector3[vCount];
        int[]     tris  = new int[tCount];

        for (int j = 0; j <= GridN; j++)
        {
            for (int i = 0; i <= GridN; i++)
            {
                float wx = originX + i * step;
                float wz = originZ + j * step;
                float wy = SampleGroundMeshY(wx, wz, groundY);
                // Store in root-local space (root has no rotation/scale, only XZ translation)
                verts[j * (GridN + 1) + i] = new Vector3(wx - rootOfs.x, wy, wz - rootOfs.z);
            }
        }

        int t = 0;
        for (int j = 0; j < GridN; j++)
        {
            for (int i = 0; i < GridN; i++)
            {
                int bl = j * (GridN + 1) + i;
                int br = bl + 1;
                int tl = bl + (GridN + 1);
                int tr = tl + 1;
                tris[t++] = bl; tris[t++] = tl; tris[t++] = br;
                tris[t++] = br; tris[t++] = tl; tris[t++] = tr;
            }
        }

        Mesh groundMesh = new Mesh();
        groundMesh.name = "RacingGroundMesh";
        groundMesh.vertices  = verts;
        groundMesh.triangles = tris;
        groundMesh.RecalculateNormals();
        groundMesh.RecalculateBounds();

        GameObject ground = new GameObject("RacingGround");
        ground.transform.SetParent(racingSceneRoot.transform, false);
        ground.transform.localPosition = Vector3.zero;
        MeshFilter   mf = ground.AddComponent<MeshFilter>();
        MeshRenderer mr = ground.AddComponent<MeshRenderer>();
        mf.sharedMesh = groundMesh;
        Color groundColor = new Color(0.62f, 0.74f, 0.46f);
        Shader urpLit = ShaderRefs.Lit;
        Material groundMat = new Material(urpLit);
        groundMat.color = groundColor;
        if (groundMat.HasProperty("_BaseColor"))  groundMat.SetColor("_BaseColor", groundColor);
        if (groundMat.HasProperty("_Smoothness")) groundMat.SetFloat("_Smoothness", 0.14f);
        if (groundMat.HasProperty("_Metallic"))   groundMat.SetFloat("_Metallic", 0f);
        mr.sharedMaterial = groundMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        // в”Ђв”Ђ Trees, bushes, flowers в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        int placed = 0;
        for (int attempt = 0; attempt < 1200 && placed < 380; attempt++)
        {
            float rx = center.x + Random.Range(-280f, 280f);
            float rz = center.z + Random.Range(-280f, 280f);
            Vector3 pos = new Vector3(rx, groundY + SampleTerrainY(rx, rz), rz);

            if (IsPositionOnRaceRoad(pos, 4.5f)) continue;

            int seed = attempt * 7193;
            float roll = (seed % 100) / 100f;

            GameObject obj = new($"RaceVeg_{placed}");
            obj.transform.SetParent(racingSceneRoot.transform, false);
            obj.transform.position  = pos;
            obj.transform.rotation  = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            if (roll < 0.62f)
            {
                // Tree вЂ” much bigger
                float treeScale = Random.Range(2.8f, 4.2f);
                obj.transform.localScale = Vector3.one * treeScale;
                CreateTreeVariant(obj.transform, attempt % 3);

                // Register near-road trees as collideable obstacles
                if (IsPositionOnRaceRoad(pos, 10f)) // within 10 u of road centre
                {
                    float trunkRadius = 0.18f * treeScale; // trunk ~0.18 local at scale 1
                    racingTreeObstacles.Add(new RaceObstacleData
                    {
                        PoleXZ           = new Vector2(pos.x, pos.z),
                        Root             = obj.transform,
                        OriginalLocalRot = obj.transform.localRotation,
                        CollisionRadius  = Mathf.Clamp(trunkRadius, 0.35f, 0.70f),
                        TiltAngle        = 0f,
                        TiltTarget       = 0f,
                        TiltAxisLocal    = Vector3.right,
                        IsTipped         = false,
                    });
                }
            }
            else if (roll < 0.82f)
            {
                // Berry bush
                obj.transform.localScale = Vector3.one * Random.Range(1.8f, 2.6f);
                CreateRacingBush(obj.transform, attempt);
            }
            else
            {
                // Flower patch
                obj.transform.localScale = Vector3.one * Random.Range(1.6f, 2.2f);
                CreateRacingFlowers(obj.transform, attempt);
                racingFlowerPoints.Add(pos);
            }

            placed++;
        }

        // в”Ђв”Ђ Lanterns вЂ” one pair every segment, both sides в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        for (int i = 0; i < raceSegments.Count; i++)
        {
            RaceSegment seg = raceSegments[i];
            Vector3 fwd   = seg.Rotation * Vector3.forward;
            Vector3 right = seg.Rotation * Vector3.right;
            Vector3 segStart = seg.Center - fwd * seg.Length * 0.5f;
            segStart.y = seg.StartY;

            Quaternion lanternRot = seg.Rotation;
            CreateRacingLantern(segStart + right * 3.2f,  lanternRot);
            CreateRacingLantern(segStart - right * 3.2f,  lanternRot);
        }
    }

    private void CreateRacingLantern(Vector3 worldPos, Quaternion worldRot)
    {
        // worldPos.y already set to road height by caller

        GameObject root = new("RaceLantern");
        root.transform.SetParent(racingSceneRoot.transform, false);
        root.transform.position   = worldPos;
        root.transform.rotation   = worldRot;
        root.transform.localScale = Vector3.one * 3f;

        // Pole
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.transform.SetParent(root.transform, false);
        pole.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        pole.transform.localScale    = new Vector3(0.08f, 1.42f, 0.08f);
        ApplyColor(pole, new Color(0.22f, 0.23f, 0.27f));
        ConfigureShadowVisual(pole);

        // Arm
        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.transform.SetParent(root.transform, false);
        arm.transform.localPosition = new Vector3(0.14f, 1.34f, 0f);
        arm.transform.localScale    = new Vector3(0.3f, 0.06f, 0.06f);
        ApplyColor(arm, new Color(0.22f, 0.23f, 0.27f));
        ConfigureShadowVisual(arm);

        // Lamp head
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
        head.transform.SetParent(root.transform, false);
        head.transform.localPosition = new Vector3(0.26f, 1.16f, 0f);
        head.transform.localScale    = new Vector3(0.16f, 0.22f, 0.16f);
        ApplyColor(head, new Color(0.3f, 0.28f, 0.2f));
        ConfigureShadowVisual(head);

        // Glow sphere
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.transform.SetParent(root.transform, false);
        glow.transform.localPosition = new Vector3(0.26f, 1.05f, 0f);
        glow.transform.localScale    = new Vector3(0.12f, 0.12f, 0.12f);
        ApplyColor(glow, new Color(0.26f, 0.22f, 0.18f));
        ConfigureStaticVisual(glow);

        // Light
        GameObject lightObj = new("LanternLight");
        lightObj.transform.SetParent(root.transform, false);
        lightObj.transform.localPosition = new Vector3(0.26f, 1.02f, 0f);
        Light l = lightObj.AddComponent<Light>();
        l.type      = LightType.Point;
        l.color     = new Color(1f, 0.9f, 0.72f);
        l.range     = 14f;
        l.intensity = 0f;
        l.shadows   = LightShadows.None;
        l.enabled   = false;

        racingWorldLights.Add(l);

        // Register lantern for manual collision
        racingLanterns.Add(new RaceObstacleData
        {
            PoleXZ           = new Vector2(worldPos.x, worldPos.z),
            Root             = root.transform,
            OriginalLocalRot = root.transform.localRotation,
            CollisionRadius  = LanternPoleRadius,
            TiltAngle        = 0f,
            TiltTarget       = 0f,
            TiltAxisLocal    = Vector3.right,
            IsTipped         = false,
        });
    }

    private static void CreateRacingBush(Transform parent, int seed)
    {
        Color leafA = new Color(0.16f, 0.42f, 0.2f);
        Color leafB = new Color(0.22f, 0.52f, 0.26f);
        Vector3[] pos = { new(-0.12f, 0.18f, -0.02f), new(0.14f, 0.22f, 0.04f), new(0.02f, 0.25f, -0.14f) };
        Vector3[] scl = { new(0.32f, 0.24f, 0.3f),    new(0.36f, 0.28f, 0.32f), new(0.28f, 0.22f, 0.26f) };
        for (int i = 0; i < 3; i++)
        {
            GameObject c = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            c.transform.SetParent(parent, false);
            c.transform.localPosition = pos[i];
            c.transform.localScale    = scl[i];
            ApplyColor(c, i % 2 == 0 ? leafB : leafA);
            ConfigureStaticVisual(c);
        }
    }

    private static void CreateRacingFlowers(Transform parent, int seed)
    {
        Color stemCol = new Color(0.2f, 0.5f, 0.24f);
        Color[] petals = { new(0.94f, 0.88f, 0.24f), new(0.96f, 0.62f, 0.22f), new(0.92f, 0.48f, 0.58f) };
        for (int i = 0; i < 5; i++)
        {
            float a = (i / 5f) * Mathf.PI * 2f + seed * 0.4f;
            float r = 0.06f + (i % 2) * 0.04f;

            // Stem
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(parent, false);
            stem.transform.localPosition = new Vector3(Mathf.Cos(a)*r, 0.09f, Mathf.Sin(a)*r);
            stem.transform.localScale    = new Vector3(0.025f, 0.09f, 0.025f);
            ApplyColor(stem, stemCol);
            ConfigureStaticVisual(stem);

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(parent, false);
            head.transform.localPosition = new Vector3(Mathf.Cos(a)*r, 0.19f, Mathf.Sin(a)*r);
            head.transform.localScale    = new Vector3(0.08f, 0.06f, 0.08f);
            ApplyColor(head, petals[i % petals.Length]);
            ConfigureStaticVisual(head);
        }
    }

    // Returns raw surface Y (no +0.35 truck-centre offset) вЂ” on-road or off-road.
    private float SampleSurfaceY(float x, float z)
    {
        if (IsPositionOnRaceRoad(new Vector3(x, 0f, z), 4.8f))
        {
            float bestDSq = float.MaxValue, bestY = 0f;
            foreach (var seg in raceSegments)
            {
                Vector3 fwd = seg.Rotation * Vector3.forward;
                float sx = seg.Center.x - fwd.x * seg.Length * 0.5f;
                float sz = seg.Center.z - fwd.z * seg.Length * 0.5f;
                float t  = Mathf.Clamp01(((x - sx) * fwd.x + (z - sz) * fwd.z) / seg.Length);
                float cx = sx + fwd.x * seg.Length * t;
                float cz = sz + fwd.z * seg.Length * t;
                float dSq = (x - cx) * (x - cx) + (z - cz) * (z - cz);
                if (dSq < bestDSq) { bestDSq = dSq; bestY = Mathf.Lerp(seg.StartY, seg.EndY, t); }
            }
            return bestY;
        }
        return SampleGroundMeshY(x, z, racingGroundY);
    }

    // в”Ђв”Ђ Racing atmosphere в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void PopulateRacingAtmosphere()
    {
        if (racingSceneRoot == null) return;

        // Track bounding center
        Vector3 center = Vector3.zero;
        foreach (var seg in raceSegments) center += seg.Center;
        center /= raceSegments.Count;
        center.y = 0f;

        // в”Ђв”Ђ Post-processing on racing camera в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        SetupRacingPostProcessing();

        // в”Ђв”Ђ Birds on tree tops в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        int birdCount = Mathf.Min(racingTreeObstacles.Count, 6);
        if (birdCount > 0)
        {
            int step = Mathf.Max(1, racingTreeObstacles.Count / birdCount);
            for (int i = 0; i < birdCount; i++)
            {
                RaceObstacleData tree = racingTreeObstacles[Mathf.Min(i * step, racingTreeObstacles.Count - 1)];
                float treeScale = tree.Root != null ? tree.Root.localScale.x : 3f;
                Vector3 perch = new Vector3(tree.PoleXZ.x,
                                            (tree.Root != null ? tree.Root.position.y : 0f) + treeScale * 2.6f,
                                            tree.PoleXZ.y);
                CreateRacingBird(perch);
            }
        }

        // в”Ђв”Ђ Bees near flower patches в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        int beeCount = Mathf.Min(racingFlowerPoints.Count, 10);
        if (beeCount > 0)
        {
            int step = Mathf.Max(1, racingFlowerPoints.Count / beeCount);
            for (int i = 0; i < beeCount; i++)
                CreateRacingBee(racingFlowerPoints[Mathf.Min(i * step, racingFlowerPoints.Count - 1)]);
        }

        // в”Ђв”Ђ Moths near lanterns в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        int mothCount = Mathf.Min(racingLanterns.Count, 10);
        if (mothCount > 0)
        {
            int step = Mathf.Max(1, racingLanterns.Count / mothCount);
            for (int i = 0; i < mothCount; i++)
            {
                RaceObstacleData lan = racingLanterns[Mathf.Min(i * step, racingLanterns.Count - 1)];
                // Lamp head is at local Y в‰€ 1.05, world scale 3 в†’ ~3.2 m above root
                Vector3 lampPos = lan.Root != null
                    ? lan.Root.position + Vector3.up * 3.2f
                    : new Vector3(lan.PoleXZ.x, 3.5f, lan.PoleXZ.y);
                CreateRacingMothSwarm(lampPos);
            }
        }

        // в”Ђв”Ђ Ambient dust motes в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        for (int i = 0; i < 14; i++)
            CreateRacingDustMote(center, i);

        // в”Ђв”Ђ Boulders near mountains в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        foreach (var bump in terrainBumps)
        {
            if (bump.Height < 9f) continue;
            Vector3 clusterBase = new Vector3(bump.Center.x,
                                              racingGroundY + SampleTerrainY(bump.Center.x, bump.Center.y) * 0.35f,
                                              bump.Center.y);
            CreateRacingBoulderCluster(clusterBase, bump.Radius * 0.28f);
        }

        // в”Ђв”Ђ Small ponds в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        int pondsPlaced = 0;
        for (int attempt = 0; attempt < 300 && pondsPlaced < 4; attempt++)
        {
            float rx = center.x + Random.Range(-220f, 220f);
            float rz = center.z + Random.Range(-220f, 220f);
            if (IsPositionOnRaceRoad(new Vector3(rx, 0f, rz), 14f)) continue;
            float wy = racingGroundY + SampleTerrainY(rx, rz) - 0.08f;
            CreateRacingPond(new Vector3(rx, wy, rz));
            pondsPlaced++;
        }
    }

    private void SetupRacingPostProcessing()
    {
        if (racingCamera == null || racingSceneRoot == null) return;

        UniversalAdditionalCameraData camData = racingCamera.GetUniversalAdditionalCameraData();
        if (camData != null)
        {
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camData.antialiasingQuality = AntialiasingQuality.Medium;
        }

        GameObject volObj = new("RacingPostProcessVolume");
        volObj.transform.SetParent(racingSceneRoot.transform, false);
        Volume vol = volObj.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 90f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        vol.sharedProfile = profile;

        ColorAdjustments ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(0.05f);
        ca.contrast.Override(8f);
        ca.saturation.Override(14f);
        ca.colorFilter.Override(new Color(1f, 0.98f, 0.92f, 1f));

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.9f);
        bloom.intensity.Override(0.08f);
        bloom.scatter.Override(0.48f);
        bloom.tint.Override(new Color(1f, 0.96f, 0.88f, 1f));
    }

    private void CreateRacingBird(Vector3 perchPos)
    {
        GameObject root = new("RacingBird");
        root.transform.SetParent(racingSceneRoot.transform, false);
        root.transform.position = perchPos;
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.transform.SetParent(root.transform, false);
        body.transform.localScale    = new Vector3(0.12f, 0.09f, 0.18f);
        ApplyColor(body, new Color(0.22f, 0.20f, 0.18f));
        ConfigureStaticVisual(body);
        if (body.TryGetComponent(out Collider bc)) bc.enabled = false;

        GameObject lw = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lw.transform.SetParent(root.transform, false);
        lw.transform.localPosition = new Vector3(-0.06f, 0.01f, 0f);
        lw.transform.localScale    = new Vector3(0.12f, 0.02f, 0.18f);
        ApplyColor(lw, new Color(0.28f, 0.26f, 0.24f));
        ConfigureStaticVisual(lw);
        if (lw.TryGetComponent(out Collider lc)) lc.enabled = false;

        GameObject rw = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rw.transform.SetParent(root.transform, false);
        rw.transform.localPosition = new Vector3(0.06f, 0.01f, 0f);
        rw.transform.localScale    = new Vector3(0.12f, 0.02f, 0.18f);
        ApplyColor(rw, new Color(0.28f, 0.26f, 0.24f));
        ConfigureStaticVisual(rw);
        if (rw.TryGetComponent(out Collider rc)) rc.enabled = false;

        racingBirds.Add(new RacingBirdData
        {
            Root      = root.transform,
            LeftWing  = lw.transform,
            RightWing = rw.transform,
            PerchPos  = perchPos,
            BobPhase  = Random.Range(0f, 10f),
            WingPhase = Random.Range(0f, 10f),
        });
    }

    private void CreateRacingBee(Vector3 flowerPos)
    {
        GameObject root = new("RacingBee");
        root.transform.SetParent(racingSceneRoot.transform, false);
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float r     = Random.Range(0.08f, 0.18f);
        root.transform.position = flowerPos + new Vector3(Mathf.Cos(angle) * r, 0.24f, Mathf.Sin(angle) * r);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(root.transform, false);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale    = new Vector3(0.035f, 0.055f, 0.035f);
        ApplyColor(body, new Color(0.96f, 0.78f, 0.12f));
        ConfigureStaticVisual(body);
        if (body.TryGetComponent(out Collider bColl)) bColl.enabled = false;

        GameObject lw = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lw.transform.SetParent(root.transform, false);
        lw.transform.localPosition = new Vector3(-0.025f, 0.02f, 0f);
        lw.transform.localScale    = new Vector3(0.055f, 0.01f, 0.035f);
        ApplyColor(lw, new Color(0.92f, 0.96f, 1f));
        ConfigureStaticVisual(lw);
        if (lw.TryGetComponent(out Collider lwC)) lwC.enabled = false;

        GameObject rw = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rw.transform.SetParent(root.transform, false);
        rw.transform.localPosition = new Vector3(0.025f, 0.02f, 0f);
        rw.transform.localScale    = new Vector3(0.055f, 0.01f, 0.035f);
        ApplyColor(rw, new Color(0.92f, 0.96f, 1f));
        ConfigureStaticVisual(rw);
        if (rw.TryGetComponent(out Collider rwC)) rwC.enabled = false;

        racingBees.Add(new RacingBeeData
        {
            Root          = root.transform,
            LeftWing      = lw.transform,
            RightWing     = rw.transform,
            FlowerPos     = flowerPos,
            OrbitAngle    = angle,
            OrbitRadius   = r,
            OrbitHeight   = Random.Range(0.18f, 0.28f),
            OrbitSpeed    = Random.Range(1.6f, 2.6f),
            BobAmplitude  = Random.Range(0.015f, 0.04f),
            BobSpeed      = Random.Range(2.2f, 3.6f),
            PhaseOffset   = Random.Range(0f, 10f),
        });
    }

    private void CreateRacingMothSwarm(Vector3 lanternPos)
    {
        GameObject root = new("RacingMothSwarm");
        root.transform.SetParent(racingSceneRoot.transform, false);
        root.transform.position = lanternPos;

        int count = Random.Range(4, 7);
        for (int i = 0; i < count; i++)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.name = $"Moth_{i + 1}";
            p.transform.SetParent(root.transform, false);
            p.transform.localScale = Vector3.one * Random.Range(0.026f, 0.042f);
            ApplyColor(p, new Color(0.9f, 0.87f, 0.65f));
            ConfigureStaticVisual(p);
            if (p.TryGetComponent(out Collider pC)) pC.enabled = false;
        }

        racingMoths.Add(new RacingMothData
        {
            Root         = root.transform,
            LanternPos   = lanternPos,
            OrbitRadius  = Random.Range(0.16f, 0.28f),
            OrbitHeight  = Random.Range(0.06f, 0.18f),
            OrbitAngle   = Random.Range(0f, Mathf.PI * 2f),
            OrbitSpeed   = Random.Range(0.9f, 1.6f),
            BobAmplitude = Random.Range(0.02f, 0.06f),
            BobSpeed     = Random.Range(1.8f, 3.4f),
            PhaseOffset  = Random.Range(0f, 10f),
        });
    }

    private void CreateRacingDustMote(Vector3 areaCenter, int index)
    {
        float a  = index / 14f * Mathf.PI * 2f;
        float rx = areaCenter.x + Mathf.Cos(a) * 14f + Random.Range(-10f, 10f);
        float rz = areaCenter.z + Mathf.Sin(a) * 14f + Random.Range(-10f, 10f);
        float wy = racingGroundY + SampleTerrainY(rx, rz) + Random.Range(0.8f, 2.4f);

        GameObject dust = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dust.name = $"RacingDust_{index + 1}";
        dust.transform.SetParent(racingSceneRoot.transform, false);
        dust.transform.position   = new Vector3(rx, wy, rz);
        dust.transform.localScale = Vector3.one * Random.Range(0.030f, 0.056f);
        ApplyUnlitColor(dust, new Color(0.96f, 0.94f, 0.86f));
        if (dust.TryGetComponent(out Collider dC)) dC.enabled = false;

        racingDustMotes.Add(new RacingDustData
        {
            Root         = dust.transform,
            AreaCenter   = new Vector3(rx, 0f, rz),
            HalfRangeX   = Random.Range(3f, 11f),
            HalfRangeZ   = Random.Range(3f, 11f),
            TravelOffset = Random.Range(0f, Mathf.PI * 2f),
            Speed        = Random.Range(0.05f, 0.14f),
            BobAmplitude = Random.Range(0.02f, 0.07f),
            BobPhase     = Random.Range(0f, 10f),
            BaseY        = wy,
        });
    }

    private void CreateRacingBoulderCluster(Vector3 clusterCenter, float radius)
    {
        int count = Random.Range(2, 5);
        for (int i = 0; i < count; i++)
        {
            float a  = Random.Range(0f, Mathf.PI * 2f);
            float d  = Random.Range(0f, radius);
            float wx = clusterCenter.x + Mathf.Cos(a) * d;
            float wz = clusterCenter.z + Mathf.Sin(a) * d;
            if (IsPositionOnRaceRoad(new Vector3(wx, 0f, wz), 8f)) continue;

            float scale   = Random.Range(0.7f, 2.4f);
            float groundY = racingGroundY + SampleTerrainY(wx, wz);

            GameObject boulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boulder.name = "RacingBoulder";
            boulder.transform.SetParent(racingSceneRoot.transform, false);
            boulder.transform.position = new Vector3(wx, groundY + scale * 0.28f, wz);
            boulder.transform.rotation = Quaternion.Euler(
                Random.Range(0f, 25f), Random.Range(0f, 360f), Random.Range(0f, 25f));
            boulder.transform.localScale = new Vector3(
                scale * Random.Range(0.8f, 1.2f),
                scale * Random.Range(0.55f, 0.85f),
                scale * Random.Range(0.8f, 1.2f));

            Color rock = Color.Lerp(new Color(0.52f, 0.50f, 0.46f), new Color(0.40f, 0.38f, 0.34f), Random.value);
            ApplyColor(boulder, rock);
            ConfigureShadowVisual(boulder);
            if (boulder.TryGetComponent(out Collider bColl)) bColl.enabled = false;
        }
    }

    private void CreateRacingPond(Vector3 pos)
    {
        GameObject pond = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pond.name = "RacingPond";
        pond.transform.SetParent(racingSceneRoot.transform, false);
        pond.transform.position = pos;
        float rx = Random.Range(2.0f, 4.5f);
        float rz = Random.Range(1.6f, 3.8f);
        pond.transform.localScale = new Vector3(rx * 2f, 0.16f, rz * 2f);
        pond.transform.rotation   = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        ApplyColor(pond, new Color(0.26f, 0.50f, 0.72f));
        ConfigureStaticVisual(pond);
        if (pond.TryGetComponent(out Collider pC)) pC.enabled = false;
    }

    private void UpdateRacingAtmosphere(float dt)
    {
        float t = Time.unscaledTime;

        // в”Ђв”Ђ Birds (idle perch bob + wing twitch) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        for (int i = 0; i < racingBirds.Count; i++)
        {
            RacingBirdData b = racingBirds[i];
            if (b.Root == null) continue;
            float bob  = Mathf.Sin(t * 2.1f + b.BobPhase) * 0.012f;
            b.Root.position = b.PerchPos + new Vector3(0f, bob, 0f);
            float wingAng = Mathf.Sin(t * 3.4f + b.WingPhase) * 5f;
            if (b.LeftWing  != null) b.LeftWing.localRotation  = Quaternion.Euler(0f, 0f,  wingAng);
            if (b.RightWing != null) b.RightWing.localRotation = Quaternion.Euler(0f, 0f, -wingAng);
        }

        // в”Ђв”Ђ Bees (orbit flower + wing flutter) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        for (int i = racingBees.Count - 1; i >= 0; i--)
        {
            RacingBeeData b = racingBees[i];
            if (b.Root == null) { racingBees.RemoveAt(i); continue; }
            b.OrbitAngle += b.OrbitSpeed * dt;
            float bob  = Mathf.Sin(t * b.BobSpeed + b.PhaseOffset) * b.BobAmplitude;
            b.Root.position = b.FlowerPos + new Vector3(
                Mathf.Cos(b.OrbitAngle) * b.OrbitRadius,
                b.OrbitHeight + bob,
                Mathf.Sin(b.OrbitAngle) * b.OrbitRadius);
            float flap = Mathf.Sin(t * 28f) * 18f;
            if (b.LeftWing  != null) b.LeftWing.localRotation  = Quaternion.Euler( flap, 0f, 0f);
            if (b.RightWing != null) b.RightWing.localRotation = Quaternion.Euler(-flap, 0f, 0f);
            racingBees[i] = b;
        }

        // в”Ђв”Ђ Moths (orbit lantern) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        for (int i = racingMoths.Count - 1; i >= 0; i--)
        {
            RacingMothData m = racingMoths[i];
            if (m.Root == null) { racingMoths.RemoveAt(i); continue; }
            m.OrbitAngle += m.OrbitSpeed * dt;
            float bob = Mathf.Sin(t * m.BobSpeed + m.PhaseOffset) * m.BobAmplitude;
            m.Root.position = m.LanternPos + new Vector3(
                Mathf.Cos(m.OrbitAngle) * m.OrbitRadius,
                m.OrbitHeight + bob,
                Mathf.Sin(m.OrbitAngle) * m.OrbitRadius);
            racingMoths[i] = m;
        }

        // в”Ђв”Ђ Dust (slow lazy drift) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        for (int i = 0; i < racingDustMotes.Count; i++)
        {
            RacingDustData d = racingDustMotes[i];
            if (d.Root == null) continue;
            float phase = t * d.Speed + d.TravelOffset;
            float ox = Mathf.Cos(phase) * d.HalfRangeX;
            float oz = Mathf.Sin(phase * 0.73f) * d.HalfRangeZ;
            float oy = Mathf.Sin(t * 1.4f + d.BobPhase) * d.BobAmplitude;
            d.Root.position = new Vector3(d.AreaCenter.x + ox, d.BaseY + oy, d.AreaCenter.z + oz);
        }
    }

    private bool IsPositionOnRaceRoad(Vector3 pos, float margin)
    {
        Vector2 p = new Vector2(pos.x, pos.z);
        foreach (var seg in raceSegments)
        {
            Vector3 fwd   = seg.Rotation * Vector3.forward;
            Vector3 start = seg.Center - fwd * seg.Length * 0.5f;
            Vector3 end   = seg.Center + fwd * seg.Length * 0.5f;
            if (DistXZPointToSegment(p,
                    new Vector2(start.x, start.z),
                    new Vector2(end.x,   end.z)) < margin)
                return true;
        }
        foreach (var seg in raceExtensionSegments)
        {
            Vector3 fwd   = seg.Rotation * Vector3.forward;
            Vector3 start = seg.Center - fwd * seg.Length * 0.5f;
            Vector3 end   = seg.Center + fwd * seg.Length * 0.5f;
            if (DistXZPointToSegment(p,
                    new Vector2(start.x, start.z),
                    new Vector2(end.x,   end.z)) < margin)
                return true;
        }
        return false;
    }

    private static float DistXZPointToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float sqLen = ab.sqrMagnitude;
        if (sqLen < 0.0001f) return (p - a).magnitude;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / sqLen);
        return (p - (a + ab * t)).magnitude;
    }
}

