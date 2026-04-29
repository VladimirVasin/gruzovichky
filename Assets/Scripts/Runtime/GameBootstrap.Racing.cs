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

    private struct RacingTrailParticleData
    {
        public Transform Root;
        public Material  Material;
        public Vector3   Velocity;
        public float     Life;
        public float     MaxLife;
        public float     StartScale;
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
    private readonly List<Image> racingSpeedLineImages = new();
    private Light racingHeadlightL;
    private Light racingHeadlightR;
    private Renderer racingBrakeLightLeftRenderer;
    private Renderer racingBrakeLightRightRenderer;
    private Material racingBrakeLightLeftMaterial;
    private Material racingBrakeLightRightMaterial;
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
    private string  racingFinishSummaryText = "";
    private const float RacingFinishDuration = 3.2f;

    // Race result metrics. Bonus is paid later when the intercity trade run returns.
    private float racingElapsedTime;
    private float racingTruckDamage;
    private float racingCargoDamage;
    private float racingBusImpactCooldown;
    private int   racingCollisionCount;
    private int   racingResultBaseReward;
    private int   racingResultSpeedBonus;
    private int   racingResultDamagePenalty;
    private int   racingResultCollisionPenalty;
    private bool  racingFailedByDamage;

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
    private bool       racingGearDragging;
    private Vector2    racingGearDragStart;
    private float      racingGearDragAccumY;
    private bool       racingIsReversing;         // true when truck moving backward
    private int        racingCurrentGear;         // 0=R, 1-4=forward gears (manual)
    private float      racingGearChangeTimer;     // cooldown between gear changes (s)
    private float      racingGearFlashTimer;
    private float      racingGearAccel01;
    private Text       racingGearText;            // HUD gear indicator
    private RectTransform racingGearPowerFill;
    private Text       racingGearPowerText;

    // в”Ђв”Ђ Racing atmosphere в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    private readonly List<RacingBirdData>  racingBirds       = new();
    private readonly List<RacingBeeData>   racingBees        = new();
    private readonly List<RacingMothData>  racingMoths       = new();
    private readonly List<RacingDustData>  racingDustMotes   = new();
    private readonly List<RacingTrailParticleData> racingTrailParticles = new();
    private readonly List<Vector3>         racingFlowerPoints = new();
    private float racingTrailEmitTimer;

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
    private const float RacingAcceleration  = 8.2f;   // raw power, filtered by manual gear torque
    private const float RacingMaxSpeed      = 18.5f;  // ~66 km/h top speed in 4th gear
    private const float RacingDrag          = 0.997f; // very gentle coasting drag
    private const float RacingAngularDrag   = 0.88f;  // high = angular velocity persists (inertial steering)
    private const float RacingSteerForce    = 300f;   // max turn force (balanced with new angular drag)
    private const float RacingLateralFriction = 28f;  // lower = more drift/slide

    // в”Ђв”Ђ Gear shift в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    // Lever X-rotation per gear: 0=R, 1=1st, 2=2nd, 3=3rd, 4=4th
    private static readonly float[] GearShiftAngles  = { -28f, -14f, 0f, 14f, 28f };
    private static readonly float[] GearMaxSpeed     = { 7.0f, 7.2f, 11.4f, 15.0f, 18.5f };
    private static readonly float[] GearMinGoodSpeed = { 0f, 0f, 3.2f, 7.2f, 11.0f };
    private static readonly float[] GearAccelMult    = { 0.55f, 1.42f, 1.12f, 0.88f, 0.68f };
    private const float GearChangeCooldown = 0.34f;   // min seconds between manual shifts
    private const float GearDragThresholdPx = 34f;
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
                          IsRaceJoinWindowOpen();

        if (joinRaceButtonRoot.activeSelf != shouldShow)
            joinRaceButtonRoot.SetActive(shouldShow);
    }

    // в”Ђв”Ђ Minigame entry / exit в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private bool IsRaceJoinWindowOpen()
    {
        if (activeTradeRun == null || activeTradeRun.Phase != TradeRunPhase.OutOfMap) return false;
        float elapsed = activeTradeRun.OutOfMapDuration - activeTradeRun.OutOfMapTimer;
        return elapsed <= DayNightCycleDuration / 24f;
    }

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
            racingGearDragging      = false;
            racingGearDragStart     = Vector2.zero;
            racingGearDragAccumY    = 0f;
            racingCurrentGear   = 1;
            racingGearChangeTimer = 0f;
            racingGearFlashTimer  = 0f;
            racingGearAccel01     = 0f;
            racingElapsedTime     = 0f;
            racingTruckDamage     = 0f;
            racingCargoDamage     = 0f;
            racingBusImpactCooldown = 0f;
            racingCollisionCount  = 0;
            racingResultBaseReward = 0;
            racingResultSpeedBonus = 0;
            racingResultDamagePenalty = 0;
            racingResultCollisionPenalty = 0;
            racingFailedByDamage  = false;
            racingFinishSummaryText = "";
            racingBonusEarned     = 0;
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
            CalculateRacingResult();
            SessionDebugLogger.Log(
                "RACING",
                $"Race completed. time={racingElapsedTime:F1}s, truckDamage={racingTruckDamage:F0}, cargoDamage={racingCargoDamage:F0}, collisions={racingCollisionCount}, bonus=${racingBonusEarned}.");
            StartFinishSequence();
            return;
        }

        // Immediate cleanup (skip or already in sequence)
        isRacingActive = false;
        racingFinishSequenceActive = false;

        if (!success)
        {
            racingBonusEarned = 0;
            SessionDebugLogger.Log("RACING", "Race skipped.");
        }

        // Stop music
        if (racingMusicSource != null) { Object.Destroy(racingMusicSource.gameObject); racingMusicSource = null; }

        CleanupRacingScene();
    }

    private void CalculateRacingResult()
    {
        racingResultBaseReward = racingFailedByDamage ? 0 : 60;

        float targetTime = Mathf.Max(42f, racingTrackLen / 12.5f);
        racingResultSpeedBonus = racingFailedByDamage
            ? 0
            : Mathf.Clamp(Mathf.RoundToInt((targetTime - racingElapsedTime) * 1.8f), 0, 80);

        racingResultDamagePenalty = Mathf.RoundToInt(racingTruckDamage * 0.65f + racingCargoDamage * 0.90f);
        racingResultCollisionPenalty = racingCollisionCount * 5;
        racingBonusEarned = racingFailedByDamage
            ? 0
            : Mathf.Max(0, racingResultBaseReward + racingResultSpeedBonus - racingResultDamagePenalty - racingResultCollisionPenalty);

        racingFinishSummaryText = IsRussianLanguage()
            ? $"Время: {racingElapsedTime:F1} c   Удары: {racingCollisionCount}\n" +
              $"Грузовик: {racingTruckDamage:F0}%   Груз: {racingCargoDamage:F0}%\n" +
              $"База ${racingResultBaseReward} + скорость ${racingResultSpeedBonus} - урон ${racingResultDamagePenalty} - удары ${racingResultCollisionPenalty}\n" +
              $"Бонус к рейсу: ${racingBonusEarned}"
            : $"Time: {racingElapsedTime:F1}s   Impacts: {racingCollisionCount}\n" +
              $"Truck: {racingTruckDamage:F0}%   Cargo: {racingCargoDamage:F0}%\n" +
              $"Base ${racingResultBaseReward} + speed ${racingResultSpeedBonus} - damage ${racingResultDamagePenalty} - impacts ${racingResultCollisionPenalty}\n" +
              $"Run bonus: ${racingBonusEarned}";
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

        // Finish text
        GameObject textObj = new("FinishText");
        textObj.transform.SetParent(overlayObj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.font      = font;
        txt.fontSize  = 52;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color     = racingFailedByDamage ? new Color(1f, 0.35f, 0.25f, 1f) : new Color(1f, 0.92f, 0.2f, 1f);
        txt.text      = racingFailedByDamage
            ? (IsRussianLanguage() ? "РЕЙС РАЗБИТ!" : "DELIVERY WRECKED!")
            : (IsRussianLanguage() ? "ФИНИШ!" : "YOU FINISHED!");

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
        sub.fontSize  = 22;
        sub.alignment = TextAnchor.MiddleCenter;
        sub.color     = new Color(1f, 1f, 1f, 0.85f);
        sub.text      = racingFinishSummaryText;

        RectTransform subRect = sub.rectTransform;
        subRect.anchorMin        = new Vector2(0f, 0.39f);
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
        racingGearDragging       = false;
        racingGearDragStart      = Vector2.zero;
        racingGearDragAccumY     = 0f;
        racingIsReversing        = false;
        racingCurrentGear        = 0;
        racingGearChangeTimer    = 0f;
        racingGearFlashTimer     = 0f;
        racingGearAccel01        = 0f;
        racingGearText           = null;
        racingGearPowerFill      = null;
        racingGearPowerText      = null;
        racingElapsedTime        = 0f;
        racingTruckDamage        = 0f;
        racingCargoDamage        = 0f;
        racingBusImpactCooldown  = 0f;
        racingCollisionCount     = 0;
        racingResultBaseReward   = 0;
        racingResultSpeedBonus   = 0;
        racingResultDamagePenalty = 0;
        racingResultCollisionPenalty = 0;
        racingFailedByDamage     = false;
        racingFinishSummaryText  = "";

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
        racingSpeedLineImages.Clear();
        racingHeadlightL = null;
        racingHeadlightR = null;
        racingBrakeLightLeftRenderer = null;
        racingBrakeLightRightRenderer = null;
        racingBrakeLightLeftMaterial = null;
        racingBrakeLightRightMaterial = null;
        racingWorldLights.Clear();
        racingLanterns.Clear();
        racingTreeObstacles.Clear();
        racingBirds.Clear();
        racingBees.Clear();
        racingMoths.Clear();
        racingDustMotes.Clear();
        racingTrailParticles.Clear();
        racingTrailEmitTimer = 0f;
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
        racingElapsedTime += dt;
        if (racingBusImpactCooldown > 0f)
            racingBusImpactCooldown = Mathf.Max(0f, racingBusImpactCooldown - dt);

        // в”Ђв”Ђ Input в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        float throttle = 0f;
        bool  sBrakeReverse = false;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   throttle += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) sBrakeReverse = true;

            bool gearConsumesMouse = UpdateGearShiftMouseDrag(dt);
            if (!gearConsumesMouse)
            {
                // Steering is intentionally mouse-only: the wheel is the control surface.
                UpdateWheelMouseDrag(dt);
            }

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

        UpdateManualGearState(fwdDot, dt);

        int forwardGear = Mathf.Clamp(racingCurrentGear, 1, 4);
        float gearMaxSpeed = GearMaxSpeed[forwardGear];
        float gearMinGoodSpeed = GearMinGoodSpeed[forwardGear];
        float lowSpeedEfficiency = forwardGear <= 1
            ? 1f
            : Mathf.Lerp(0.24f, 1f, Mathf.InverseLerp(gearMinGoodSpeed * 0.35f, gearMinGoodSpeed, speed));
        float topEndTorque = Mathf.Pow(1f - Mathf.Clamp01(speed / gearMaxSpeed), 0.72f);
        float gearTorque = topEndTorque * lowSpeedEfficiency * GearAccelMult[forwardGear];
        float gearLoad01 = Mathf.Clamp01(speed / Mathf.Max(0.1f, gearMaxSpeed));
        racingGearAccel01 = Mathf.Lerp(racingGearAccel01, throttle > 0.05f ? gearLoad01 : 0f, 8f * dt);
        racingVelocity += forward * throttle * RacingAcceleration * gearTorque * dt;

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
                float revMax    = GearMaxSpeed[0];
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

        bool visualBraking = sBrakeReverse && Vector2.Dot(racingVelocity, forward) > 0.2f;

        // Speed cap follows the selected manual gear while moving forward.
        float activeSpeedLimit = fwdDot < -0.2f ? GearMaxSpeed[0] : gearMaxSpeed;
        if (racingVelocity.sqrMagnitude > activeSpeedLimit * activeSpeedLimit)
            racingVelocity = racingVelocity.normalized * activeSpeedLimit;

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
        if (racingFinishSequenceActive)
            return;
        UpdateRacingAtmosphere(dt);
        UpdateRacingTrailParticles(dt, speed, visualBraking);

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

            UpdateRacingBrakeLights(visualBraking, speed);
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
            float speed01 = Mathf.Clamp01(speed / RacingMaxSpeed);
            float shake = Mathf.Lerp(0.004f, 0.055f, speed01) + Mathf.Clamp01(Mathf.Abs(racingAngularVel) / 48f) * 0.035f;
            Vector3 shakeOffset = camRot * new Vector3(
                Mathf.Sin(Time.unscaledTime * 23.1f) * shake,
                Mathf.Sin(Time.unscaledTime * 31.7f) * shake * 0.45f,
                0f);
            targetPos += shakeOffset;

            racingCamera.transform.position = Vector3.Lerp(
                racingCamera.transform.position, targetPos, 5.5f * dt);
            racingCamera.transform.rotation = Quaternion.Slerp(
                racingCamera.transform.rotation, camRot, 5.5f * dt);
            racingCamera.fieldOfView = Mathf.Lerp(racingCamera.fieldOfView, Mathf.Lerp(64f, 74f, speed01), 3.8f * dt);
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

        if (racingHudText != null)
        {
            racingHudText.text = IsRussianLanguage()
                ? "МЕЖГОРОДНИЙ РЕЙС\n" +
                  "----------------\n" +
                  $"Финиш: {distToFinish:F0} м\n" +
                  $"Время: {racingElapsedTime:F1} c\n" +
                  $"Грузовик: {racingTruckDamage:F0}%\n" +
                  $"Груз: {racingCargoDamage:F0}%\n" +
                  $"Удары: {racingCollisionCount}\n" +
                  "----------------\n" +
                  "[ESC] Выйти"
                : "INTERCITY DELIVERY\n" +
                  "----------------\n" +
                  $"Finish: {distToFinish:F0} m\n" +
                  $"Time: {racingElapsedTime:F1}s\n" +
                  $"Truck: {racingTruckDamage:F0}%\n" +
                  $"Cargo: {racingCargoDamage:F0}%\n" +
                  $"Impacts: {racingCollisionCount}\n" +
                  "----------------\n" +
                  "[ESC] Exit";
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
            Color baseGearColor = racingCurrentGear == 0
                ? new Color(1f, 0.3f, 0.2f)
                : new Color(0.95f, 0.90f, 0.84f);
            racingGearText.color = racingGearFlashTimer > 0f
                ? Color.Lerp(baseGearColor, new Color(1f, 0.82f, 0.20f), Mathf.PingPong(Time.unscaledTime * 12f, 1f))
                : baseGearColor;
        }

        UpdateRacingGearAccelHud();

        UpdateRacingSpeedLines(speed);

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
            Color racingHeadlightColor = Color.Lerp(
                new Color(0.42f, 0.24f, 0.12f),
                new Color(1f, 0.82f, 0.48f),
                Mathf.Clamp01(headlightIntensity / 4.5f));
            racingHeadlightL.color = racingHeadlightColor;
            racingHeadlightR.color = racingHeadlightColor;
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
                wl.color      = Color.Lerp(new Color(0.34f, 0.20f, 0.10f), new Color(1f, 0.78f, 0.42f), Mathf.Clamp01(wIntensity / 1.2f));
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

}
