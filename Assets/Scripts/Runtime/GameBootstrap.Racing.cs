using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public partial class GameBootstrap : MonoBehaviour
{
    // ── Fields ────────────────────────────────────────────────────────────────

    private struct RaceSegment
    {
        public Vector3 Center;
        public Quaternion Rotation;
        public float Length;
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
    private float racingAngularVel;   // degrees/s
    private float racingSteerInput;   // -1..1, ramps up/down like a steering wheel

    private float racingCameraAngle;  // lagging camera yaw — trails truck angle for inertial feel
    private float racingCameraSwayX;  // smoothed lateral offset (camera sways out of corner)

    private float racingBodyAngle;    // visual body/rear lag — trails physics angle for FWD articulation
    private Transform racingFrontAssembly; // front wheels + cabin assembly, rotated ahead of body
    private Transform racingBodyGroup;     // cargo cube — receives Z body roll
    private Transform racingCabinGroup;    // cabin cube inside FrontAssembly — receives same Z roll
    private float     racingBodyRoll;      // smoothed lean angle, degrees

    private const float RacingRollMax    = 6f;   // peak lean in degrees
    private const float RacingRollSmooth = 4.0f; // Lerp rate

    private readonly List<RaceSegment>      raceSegments          = new();
    private readonly List<RaceSegment>      raceExtensionSegments = new();
    private readonly List<RaceObstacleData> racingLanterns        = new();
    private readonly List<RaceObstacleData> racingTreeObstacles   = new();
    private Vector3 raceFinishPos;
    private Vector3 raceFinishFwd;  // road forward direction at finish — used for strip detection

    private Canvas racingHudCanvas;
    private Text racingHudText;
    private RectTransform racingSpeedometerNeedle;
    private Text racingSpeedometerText;
    private Light racingHeadlightL;
    private Light racingHeadlightR;
    private readonly List<Light> racingWorldLights = new();

    private AudioSource racingMusicSource;

    // ── Cinematic finish fields ──────────────────────────────────────────────
    private bool    racingFinishSequenceActive;
    private float   racingFinishSequenceTimer;
    private Vector3 racingFinishCameraPos;
    private Quaternion racingFinishCameraRot;
    private Vector2 racingFinishDriveDir;    // XZ forward of truck at finish moment
    private float   racingFinishEntrySpeed;  // truck speed at the moment of crossing finish
    private Canvas  racingFinishOverlayCanvas;
    private Text    racingFinishOverlayText;
    private const float RacingFinishDuration = 3.2f;

    // ── Skybox ───────────────────────────────────────────────────────────────
    private GameObject racingSkydome;
    private Renderer   racingSkydomeRenderer;

    private GameObject joinRaceButtonRoot;  // the "JOIN THE RACE" button canvas
    private Button joinRaceButton;
    private Text joinRaceButtonText;

    // ── Constants ────────────────────────────────────────────────────────────

    // ── Lantern collision ────────────────────────────────────────────────────
    private const float LanternPoleRadius      = 0.12f; // world units (pole local 0.08 × scale 3 × 0.5)
    private const float TruckCollisionRadius   = 0.60f; // world units — truck XZ footprint
    private const float LanternCombinedRadius  = LanternPoleRadius + TruckCollisionRadius; // 0.72
    private const float LanternTiltTargetDeg   = 44f;   // how far lantern tips when hit
    private const float LanternTiltSpeed       = 140f;  // deg/s (reaches 44° in ~0.3 s)
    private const float CollisionEnergyLoss    = 0.42f; // fraction of normal velocity lost on hit
    private const float CollisionAngularKick   = 55f;   // deg/s spin impulse on first contact

    // ── Physics ──────────────────────────────────────────────────────────────
    private const float RacingAcceleration  = 6f;     // raw power — halved for heavy truck feel
    private const float RacingMaxSpeed      = 14.5f;  // ~52 km/h — higher ceiling, slow to reach
    private const float RacingDrag          = 0.997f; // very gentle coasting drag
    private const float RacingAngularDrag   = 0.88f;  // high = angular velocity persists (inertial steering)
    private const float RacingSteerForce    = 300f;   // max turn force (balanced with new angular drag)
    private const float RacingLateralFriction = 28f;  // lower = more drift/slide
    private const int   RaceSegmentCount    = 18;
    private const float RaceTrackOffsetX    = 2000f;   // remote position, away from main world
    private const float RaceFinishRadius    = 2.8f;

    // ── Join-Race button setup/update ─────────────────────────────────────────

    private void SetupJoinRaceButton()
    {
        if (joinRaceButtonRoot != null) return;

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
                          (activeTradeRun != null && activeTradeRun.Phase == TradeRunPhase.OutOfMap
                           || UnityEngine.Debug.isDebugBuild || Application.isEditor);

        if (joinRaceButtonRoot.activeSelf != shouldShow)
            joinRaceButtonRoot.SetActive(shouldShow);
    }

    // ── Minigame entry / exit ─────────────────────────────────────────────────

    private void StartRacingMinigame()
    {
        if (isRacingActive) return;
        isRacingActive = true;

        // Pause main game
        lastActiveGameSpeedMultiplier = gameSpeedMultiplier > 0 ? gameSpeedMultiplier : 1;
        gameSpeedMultiplier = 0;
        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0f;

        // Hide join button
        if (joinRaceButtonRoot != null) joinRaceButtonRoot.SetActive(false);

        // Disable main camera
        if (mainCamera != null) mainCamera.enabled = false;

        // Build scene
        GenerateRaceTrack();
        PopulateRacingWorld();
        CreateRacingTruck();
        SetupRacingCamera();
        SetupRacingHud();

        // Place truck at start
        racingTruckPos   = raceSegments[0].Center - raceSegments[0].Rotation * Vector3.forward * raceSegments[0].Length * 0.45f;
        racingTruckPos.y = 0.35f;
        racingTruckAngle = raceSegments[0].Rotation.eulerAngles.y;
        racingVelocity   = Vector2.zero;
        racingAngularVel = 0f;
        racingSteerInput = 0f;
        racingCameraAngle = racingTruckAngle;
        racingCameraSwayX = 0f;
        racingBodyAngle   = racingTruckAngle;
        racingBodyRoll    = 0f;

        // Start looping music
        AudioClip musicClip = Resources.Load<AudioClip>("Race1");
        if (musicClip != null)
        {
            racingMusicSource = CreateAudioSource("RacingMusic", null, true, 0.72f, 0f, false);
            racingMusicSource.ignoreListenerPause = true;
            racingMusicSource.ignoreListenerVolume = false;
            racingMusicSource.clip = musicClip;
            racingMusicSource.Play();
        }

        SessionDebugLogger.Log("RACING", "Racing minigame started.");
    }

    private void FinishRace(bool success)
    {
        if (!isRacingActive) return;

        if (success && !racingFinishSequenceActive)
        {
            // Begin cinematic — don't clean up yet
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

        // Create finish overlay canvas
        GameObject overlayObj = new("FinishOverlayCanvas", typeof(Canvas), typeof(CanvasScaler));
        Canvas ovCanvas = overlayObj.GetComponent<Canvas>();
        ovCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ovCanvas.sortingOrder = 30;

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

        // "Вы финишировали!" text
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

        // Truck floors it past the finish — accelerates into the horizon
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

        // Done — clean up
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
        // Destroy skydome
        if (racingSkydome != null) { Object.Destroy(racingSkydome); racingSkydome = null; racingSkydomeRenderer = null; }

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
        racingSpeedometerNeedle = null;
        racingSpeedometerText = null;
        racingHeadlightL = null;
        racingHeadlightR = null;
        racingWorldLights.Clear();
        racingLanterns.Clear();
        racingTreeObstacles.Clear();
        raceSegments.Clear();
        raceExtensionSegments.Clear();

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

    // ── Per-frame update ──────────────────────────────────────────────────────

    private void UpdateRacingMinigame()
    {
        if (!isRacingActive && !racingFinishSequenceActive) return;

        // Cinematic finish sequence — runs after crossing finish line
        if (racingFinishSequenceActive)
        {
            UpdateFinishSequence();
            return;
        }

        float dt = Time.unscaledDeltaTime;

        // ── Input ────────────────────────────────────────
        float throttle = 0f;
        bool  sBrakeReverse = false;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   throttle += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) sBrakeReverse = true;

            // Steering ramp — fast through first 30% of lock (near-centre response),
            // then slow toward full lock; return to centre also quick through centre zone.
            bool steerLeft  = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
            bool steerRight = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
            bool steering   = steerLeft || steerRight;
            float steerTarget = steerLeft ? -1f : steerRight ? 1f : 0f;
            float absInput  = Mathf.Abs(racingSteerInput);
            float steerSpeed;
            if (steering)
                steerSpeed = absInput < 0.3f ? 4.0f : 0.9f;   // quick near centre, slow to full lock
            else
                steerSpeed = absInput > 0.3f ? 1.2f : 3.5f;   // medium outer zone, quick snap to centre
            racingSteerInput = Mathf.MoveTowards(racingSteerInput, steerTarget, steerSpeed * dt);

            if (kb.escapeKey.wasPressedThisFrame)
            {
                FinishRace(success: false);
                return;
            }
        }

        // ── Physics ──────────────────────────────────────
        float speed = racingVelocity.magnitude;

        // Forward direction (from previous frame angle — used for reverse steer check)
        float rad = racingTruckAngle * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        Vector2 right   = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));

        // When reversing, invert steering so controls feel natural (left = go left)
        float fwdDot    = Vector2.Dot(racingVelocity, forward);
        float steerSign = fwdDot >= 0f ? 1f : -1f;

        // Steering — uses ramped input, stronger max force
        float steerAmount = racingSteerInput * steerSign * RacingSteerForce * Mathf.Clamp01(speed / 3.5f) * dt;
        racingAngularVel += steerAmount;
        racingAngularVel *= Mathf.Pow(RacingAngularDrag, dt * 60f);

        racingTruckAngle += racingAngularVel * dt;

        // Recompute forward/right after angle update
        rad     = racingTruckAngle * Mathf.Deg2Rad;
        forward = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        right   = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));

        // Acceleration with torque curve — full power at low speed, tapers to 0 at max
        float torque = Mathf.Pow(1f - Mathf.Clamp01(speed / RacingMaxSpeed), 0.6f);
        racingVelocity += forward * throttle * RacingAcceleration * torque * dt;

        // Lateral friction (simulates grip)
        float lateralSpeed = Vector2.Dot(racingVelocity, right);
        racingVelocity -= right * (lateralSpeed * RacingLateralFriction * dt);

        // S key — brake while moving forward, then reverse at half speed
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
                // Reverse — half max speed, same torque curve
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

        // Lantern collision (depenetration + velocity response)
        UpdateLanternCollisions(dt);

        // ── Apply truck transform — FWD articulation ─────────────────────
        if (racingTruckVisual != null)
        {
            // Body/rear lags behind physics angle — gives the "rear follows front" look
            racingBodyAngle = Mathf.LerpAngle(racingBodyAngle, racingTruckAngle, 4.5f * dt);

            racingTruckVisual.transform.position = racingTruckPos;
            racingTruckVisual.transform.rotation = Quaternion.Euler(0f, racingBodyAngle, 0f);

            // FWD delta — front axle (wheels) + cabin pivot both steer ahead of body
            float frontDelta = Mathf.DeltaAngle(racingBodyAngle, racingTruckAngle);
            if (racingFrontAssembly != null)
                racingFrontAssembly.localRotation = Quaternion.Euler(0f, frontDelta, 0f);
            if (racingCabinGroup != null)
                racingCabinGroup.localRotation = Quaternion.Euler(0f, frontDelta, 0f);

            // Body roll — both red body and yellow cabin lean together (suspension)
            // Terminal angularVel ≈ 36 deg/s, normalise → ±RacingRollMax degrees
            float rollTarget = Mathf.Clamp(racingAngularVel / 36f, -1f, 1f) * RacingRollMax;
            racingBodyRoll = Mathf.Lerp(racingBodyRoll, rollTarget, RacingRollSmooth * dt);
            if (racingBodyGroup != null)
                racingBodyGroup.localRotation = Quaternion.Euler(0f, 0f, racingBodyRoll);

            // Rotate around the cylinder's local Y (= world X after Euler(0,0,-90)) — rolls forward
            float wheelSpin = speed * dt * 180f;
            if (racingTruckWheelFL != null) racingTruckWheelFL.Rotate(Vector3.up, wheelSpin, Space.Self);
            if (racingTruckWheelFR != null) racingTruckWheelFR.Rotate(Vector3.up, wheelSpin, Space.Self);
            if (racingTruckWheelRL != null) racingTruckWheelRL.Rotate(Vector3.up, wheelSpin, Space.Self);
            if (racingTruckWheelRR != null) racingTruckWheelRR.Rotate(Vector3.up, wheelSpin, Space.Self);
        }

        // ── Camera follow — lagging yaw + lateral sway ───────────────────────
        if (racingCamera != null)
        {
            // Camera yaw trails truck yaw — lower value = more lag / heavier feel
            racingCameraAngle = Mathf.LerpAngle(racingCameraAngle, racingTruckAngle, 2.8f * dt);

            // Lateral sway: camera drifts opposite to steering (centrifugal throw)
            float swayTarget  = racingSteerInput * -0.5f;
            racingCameraSwayX = Mathf.Lerp(racingCameraSwayX, swayTarget, 3.5f * dt);

            // Small roll tilt into the corner
            float roll = racingCameraSwayX * -3.0f;

            Quaternion camRot = Quaternion.Euler(14f, racingCameraAngle, roll);
            Vector3 camBack   = camRot * Vector3.back * 2.8f;
            Vector3 swayWorld = camRot * Vector3.right * racingCameraSwayX;
            Vector3 targetPos = racingTruckPos + Vector3.up * 1.4f + camBack + swayWorld;

            racingCamera.transform.position = Vector3.Lerp(
                racingCamera.transform.position, targetPos, 5.5f * dt);
            racingCamera.transform.rotation = Quaternion.Slerp(
                racingCamera.transform.rotation, camRot, 5.5f * dt);
        }

        // ── Finish check — strip detection ────────────────
        // Narrow along road direction (±2.5 m), very wide across it (±14 m)
        // so missing the line by driving off-road still counts.
        Vector3 toFinish = new Vector3(racingTruckPos.x - raceFinishPos.x, 0f, racingTruckPos.z - raceFinishPos.z);
        float alongRoad   = Vector3.Dot(toFinish, raceFinishFwd);          // depth through the line
        float acrossRoad  = Vector3.Cross(toFinish, raceFinishFwd).magnitude; // lateral distance
        bool  crossedLine = Mathf.Abs(alongRoad) < 2.5f && acrossRoad < 14f;

        // Keep distToFinish for HUD display (visual distance to centre of line)
        float distToFinish = toFinish.magnitude;

        // ── HUD update ────────────────────────────────────
        float kmh = speed * 3.6f;

        if (racingHudText != null)
        {
            racingHudText.text =
                "INTERCITY DELIVERY\n" +
                "────────────────\n" +
                $"Finish:  {distToFinish:F0} m\n" +
                "────────────────\n" +
                "[ESC]  Skip";
        }

        if (racingSpeedometerNeedle != null)
        {
            // Sweep: 0 km/h = 150° CCW from up (7 o'clock), 100 km/h = -150° (5 o'clock)
            float needleZ = 150f - Mathf.Clamp01(kmh / 100f) * 300f;
            racingSpeedometerNeedle.localEulerAngles = new Vector3(0f, 0f, needleZ);
        }

        if (racingSpeedometerText != null)
            racingSpeedometerText.text = $"{kmh:F0}";

        // Headlights — same darkness threshold as regular truck
        if (racingHeadlightL != null && racingHeadlightR != null)
        {
            float darkness         = 1f - currentStylizedDaylight;
            bool  headlightsOn     = darkness > 0.55f;
            float headlightIntensity = headlightsOn
                ? Mathf.Lerp(0.7f, 3.1f, Mathf.InverseLerp(0.55f, 1f, darkness))
                : 0f;
            racingHeadlightL.enabled   = headlightsOn;
            racingHeadlightR.enabled   = headlightsOn;
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

        if (crossedLine)
        {
            FinishRace(success: true);
        }
    }

    // ── Lantern collision ─────────────────────────────────────────────────────

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

    // ── Track generation ──────────────────────────────────────────────────────

    private void GenerateRaceTrack()
    {
        raceSegments.Clear();

        racingSceneRoot = new GameObject("RacingScene");
        racingSceneRoot.transform.position = new Vector3(RaceTrackOffsetX, 0f, 0f);

        Vector3 cursor    = new Vector3(RaceTrackOffsetX, 0f, 0f);
        float   direction = 0f; // degrees Y

        for (int i = 0; i < RaceSegmentCount; i++)
        {
            float segLen = Random.Range(14f, 28f);

            Quaternion rot = Quaternion.Euler(0f, direction, 0f);
            Vector3 fwd  = rot * Vector3.forward;
            Vector3 segCenter = cursor + fwd * segLen * 0.5f;

            RaceSegment seg = new()
            {
                Center   = segCenter,
                Rotation = rot,
                Length   = segLen
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
        CreateRaceMarker(startPos, first.Rotation, new Color(0.18f, 0.82f, 0.28f));

        // Finish marker (yellow + light)
        RaceSegment last = raceSegments[raceSegments.Count - 1];
        raceFinishPos = last.Center + last.Rotation * Vector3.forward * last.Length * 0.45f;
        raceFinishPos.y = 0.35f;
        raceFinishFwd = last.Rotation * Vector3.forward;   // road direction — for strip detection
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

        // ── Road extension beyond finish (decorative — truck drives into horizon) ──
        raceExtensionSegments.Clear();
        Vector3 extCursor = raceFinishPos;
        extCursor.y = 0f;
        Quaternion extRot = last.Rotation;
        Vector3 extFwd = extRot * Vector3.forward;
        for (int i = 0; i < 6; i++)
        {
            float extLen = 24f;
            Vector3 extCenter = extCursor + extFwd * extLen * 0.5f;
            RaceSegment ext = new() { Center = extCenter, Rotation = extRot, Length = extLen };
            CreateRaceSegmentVisuals(ext);
            raceExtensionSegments.Add(ext);
            extCursor += extFwd * extLen;
        }
    }

    private void CreateRaceSegmentVisuals(RaceSegment seg)
    {
        float w = 5.0f;

        // Road surface
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "RoadSeg";
        road.transform.SetParent(racingSceneRoot.transform, false);
        road.transform.position = new Vector3(seg.Center.x, 0.06f, seg.Center.z);
        road.transform.rotation = seg.Rotation;
        road.transform.localScale = new Vector3(w, 0.12f, seg.Length);
        ApplyColor(road, new Color(0.22f, 0.22f, 0.25f));
        ConfigureShadowVisual(road);

        // Left kerb
        CreateKerb(seg, -w * 0.5f - 0.14f);
        // Right kerb
        CreateKerb(seg, w * 0.5f + 0.14f);

        // Center dashed line (every other segment)
        if (Random.value > 0.5f)
        {
            GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dash.name = "CenterDash";
            dash.transform.SetParent(racingSceneRoot.transform, false);
            dash.transform.position = new Vector3(seg.Center.x, 0.13f, seg.Center.z);
            dash.transform.rotation = seg.Rotation;
            dash.transform.localScale = new Vector3(0.12f, 0.01f, seg.Length * 0.6f);
            ApplyColor(dash, new Color(0.92f, 0.88f, 0.56f));
            ConfigureShadowVisual(dash);
        }
    }

    private void CreateKerb(RaceSegment seg, float xOffset)
    {
        GameObject kerb = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kerb.name = "Kerb";
        kerb.transform.SetParent(racingSceneRoot.transform, false);

        Vector3 localOffset = new Vector3(xOffset, 0.09f, 0f);
        Vector3 worldOffset = seg.Rotation * localOffset;
        kerb.transform.position = new Vector3(seg.Center.x, 0f, seg.Center.z) + worldOffset;
        kerb.transform.rotation = seg.Rotation;
        kerb.transform.localScale = new Vector3(0.28f, 0.18f, seg.Length);
        ApplyColor(kerb, new Color(0.72f, 0.72f, 0.72f));
        ConfigureShadowVisual(kerb);
    }

    private void CreateRaceMarker(Vector3 worldPos, Quaternion rot, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "RaceMarker";
        marker.transform.SetParent(racingSceneRoot.transform, false);
        marker.transform.position = new Vector3(worldPos.x, 0.14f, worldPos.z);
        marker.transform.rotation = rot;
        marker.transform.localScale = new Vector3(5.2f, 0.06f, 1.1f);
        ApplyColor(marker, color);
        ConfigureShadowVisual(marker);
    }

    // ── Racing truck ─────────────────────────────────────────────────────────

    private void CreateRacingTruck()
    {
        racingTruckVisual = new GameObject("RacingTruck");
        racingTruckVisual.transform.localScale = Vector3.one * 1.6f;

        Color bodyColor   = new Color(0.85f, 0.20f, 0.18f);
        Color cabinColor  = new Color(0.95f, 0.82f, 0.28f);
        Color wheelColor  = new Color(0.14f, 0.14f, 0.14f);

        // ── BodyGroup — both body cubes roll together (suspension) ────────
        // Wheels are outside this group so they stay flat.
        GameObject bodyGroupObj = new("BodyGroup");
        bodyGroupObj.transform.SetParent(racingTruckVisual.transform, false);
        bodyGroupObj.transform.localPosition = Vector3.zero;
        racingBodyGroup = bodyGroupObj.transform;

        // Red cargo body — child of BodyGroup
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(racingBodyGroup, false);
        body.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        body.transform.localScale    = new Vector3(0.72f, 0.30f, 1.0f);
        ApplyColor(body, bodyColor);
        ConfigureShadowVisual(body);

        // CabinPivot inside BodyGroup — receives FWD delta Y so cabin steers ahead
        GameObject cabinPivotObj = new("CabinPivot");
        cabinPivotObj.transform.SetParent(racingBodyGroup, false);
        cabinPivotObj.transform.localPosition = Vector3.zero;
        racingCabinGroup = cabinPivotObj.transform;

        // Yellow cabin — child of CabinPivot (rolls with body, steers ahead)
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(racingCabinGroup, false);
        cabin.transform.localPosition = new Vector3(0f, 0.40f, 0.20f);
        cabin.transform.localScale    = new Vector3(0.58f, 0.34f, 0.44f);
        ApplyColor(cabin, cabinColor);
        ConfigureShadowVisual(cabin);

        // ── FrontAxle — front wheels + headlights, steers for FWD visual ─
        // NOT a parent of body parts, so steering here doesn't tilt the body
        GameObject frontObj = new("FrontAxle");
        frontObj.transform.SetParent(racingTruckVisual.transform, false);
        frontObj.transform.localPosition = Vector3.zero;
        racingFrontAssembly = frontObj.transform;

        racingTruckWheelFL = CreateRacingWheel(racingFrontAssembly, new Vector3(-0.40f, 0.12f,  0.32f), wheelColor);
        racingTruckWheelFR = CreateRacingWheel(racingFrontAssembly, new Vector3( 0.40f, 0.12f,  0.32f), wheelColor);

        // Headlights on FrontAxle — point forward regardless of body roll
        racingHeadlightL = CreateRacingHeadlight(racingFrontAssembly, new Vector3(-0.28f, 0.28f, 0.52f));
        racingHeadlightR = CreateRacingHeadlight(racingFrontAssembly, new Vector3( 0.28f, 0.28f, 0.52f));

        // ── Rear wheels — on root, flat ───────────────────────────────────
        racingTruckWheelRL = CreateRacingWheel(racingTruckVisual.transform, new Vector3(-0.40f, 0.12f, -0.32f), wheelColor);
        racingTruckWheelRR = CreateRacingWheel(racingTruckVisual.transform, new Vector3( 0.40f, 0.12f, -0.32f), wheelColor);
    }

    private Light CreateRacingHeadlight(Transform parent, Vector3 localPos)
    {
        GameObject go = new("RacingHeadlight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        Light l = go.AddComponent<Light>();
        l.type      = LightType.Spot;
        l.spotAngle = 68f;
        l.range     = 28f;
        l.color     = new Color(1f, 0.96f, 0.82f);
        l.intensity = 0f;
        l.shadows   = LightShadows.None;
        l.enabled   = false;
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
        // x = z = radius scale (0.28 → diameter 0.28), y = half-thickness (0.065 → 0.13 wide)
        wheel.transform.localScale    = new Vector3(0.28f, 0.065f, 0.28f);
        ApplyColor(wheel, color);
        ConfigureShadowVisual(wheel);
        return wheel.transform;
    }

    // ── Racing camera ─────────────────────────────────────────────────────────

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

        SetupRacingSkydome();
    }

    private void SetupRacingSkydome()
    {
        racingSkydome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        racingSkydome.name = "RacingSkydome";
        Object.Destroy(racingSkydome.GetComponent<Collider>());

        // Negative X scale flips winding order — renders from the inside
        racingSkydome.transform.localScale = new Vector3(-480f, 480f, 480f);

        racingSkydomeRenderer = racingSkydome.GetComponent<Renderer>();
        racingSkydomeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        racingSkydomeRenderer.receiveShadows    = false;

        // Unlit material so it isn't affected by scene lighting
        Shader unlitShader = Shader.Find("Unlit/Color");
        Material mat = unlitShader != null
            ? new Material(unlitShader)
            : new Material(Shader.Find("Standard"));
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
    }

    // ── Racing HUD ───────────────────────────────────────────────────────────

    private void SetupRacingHud()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObj = new("RacingHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode    = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder  = 20;

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

        SetupSpeedometer(canvasObj.transform, font);
    }

    private void SetupSpeedometer(Transform canvasParent, Font font)
    {
        const float size   = 160f;
        const float radius = 62f; // tick ring radius from center

        // Root — bottom-right corner
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

        // Arc tick marks: 0–100 km/h → -135° to +135° from vertical (CW positive)
        Color tickDim    = new Color(0.55f, 0.55f, 0.55f);
        Color tickBright = new Color(0.95f, 0.82f, 0.20f);
        int tickCount = 11; // 0, 10, 20 … 100
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

        // Speed readout — center-bottom of gauge
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
    }

    // ── Racing world population ───────────────────────────────────────────────

    private void PopulateRacingWorld()
    {
        racingLanterns.Clear();
        racingTreeObstacles.Clear();

        // Bounding center of the whole track
        Vector3 center = Vector3.zero;
        foreach (var seg in raceSegments) center += seg.Center;
        center /= raceSegments.Count;
        center.y = 0f;

        // ── Ground ──────────────────────────────────────────────────────────
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "RacingGround";
        ground.transform.SetParent(racingSceneRoot.transform, false);
        ground.transform.position  = new Vector3(center.x, -0.06f, center.z);
        ground.transform.localScale = new Vector3(900f, 0.12f, 900f);
        ApplyColor(ground, new Color(0.62f, 0.74f, 0.46f));
        ConfigureShadowVisual(ground);

        // ── Trees, bushes, flowers ──────────────────────────────────────────
        int placed = 0;
        for (int attempt = 0; attempt < 1200 && placed < 380; attempt++)
        {
            float rx = center.x + Random.Range(-280f, 280f);
            float rz = center.z + Random.Range(-280f, 280f);
            Vector3 pos = new Vector3(rx, 0f, rz);

            if (IsPositionOnRaceRoad(pos, 4.5f)) continue;

            int seed = attempt * 7193;
            float roll = (seed % 100) / 100f;

            GameObject obj = new($"RaceVeg_{placed}");
            obj.transform.SetParent(racingSceneRoot.transform, false);
            obj.transform.position  = pos;
            obj.transform.rotation  = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            if (roll < 0.62f)
            {
                // Tree — much bigger
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
            }

            placed++;
        }

        // ── Lanterns — one pair every segment, both sides ────────────────
        for (int i = 0; i < raceSegments.Count; i++)
        {
            RaceSegment seg = raceSegments[i];
            Vector3 fwd   = seg.Rotation * Vector3.forward;
            Vector3 right = seg.Rotation * Vector3.right;
            Vector3 segStart = seg.Center - fwd * seg.Length * 0.5f;

            Quaternion lanternRot = seg.Rotation;
            CreateRacingLantern(segStart + right * 3.2f,  lanternRot);
            CreateRacingLantern(segStart - right * 3.2f,  lanternRot);
        }
    }

    private void CreateRacingLantern(Vector3 worldPos, Quaternion worldRot)
    {
        worldPos.y = 0f;

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
