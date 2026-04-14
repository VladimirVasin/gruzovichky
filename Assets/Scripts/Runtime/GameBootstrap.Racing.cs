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

    private readonly List<RaceSegment> raceSegments = new();
    private Vector3 raceFinishPos;

    private Canvas racingHudCanvas;
    private Text racingHudText;
    private RectTransform racingSpeedometerNeedle;
    private Text racingSpeedometerText;
    private Light racingHeadlightL;
    private Light racingHeadlightR;
    private readonly List<Light> racingWorldLights = new();

    private AudioSource racingMusicSource;

    private GameObject joinRaceButtonRoot;  // the "JOIN THE RACE" button canvas
    private Button joinRaceButton;
    private Text joinRaceButtonText;

    // ── Constants ────────────────────────────────────────────────────────────

    private const float RacingAcceleration  = 12f;    // raw power, torque curve limits top end
    private const float RacingMaxSpeed      = 11.5f;  // ~41 km/h — torque → 0 at this speed
    private const float RacingDrag          = 0.997f; // very gentle coasting drag
    private const float RacingAngularDrag   = 0.72f;   // lower = more drift tail
    private const float RacingSteerForce    = 380f;    // max wheel-turn force
    private const float RacingLateralFriction = 28f;   // lower = more drift/slide
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
        isRacingActive = false;

        if (success)
        {
            racingBonusEarned = 50;
            SessionDebugLogger.Log("RACING", "Race completed! Bonus earned: $50.");
        }
        else
        {
            SessionDebugLogger.Log("RACING", "Race skipped.");
        }

        // Stop music
        if (racingMusicSource != null) { Object.Destroy(racingMusicSource.gameObject); racingMusicSource = null; }

        // Destroy racing scene
        if (racingCamera != null) { Object.Destroy(racingCamera.gameObject); racingCamera = null; }
        if (racingSceneRoot != null) { Object.Destroy(racingSceneRoot); racingSceneRoot = null; }
        if (racingTruckVisual != null) { Object.Destroy(racingTruckVisual); racingTruckVisual = null; }
        if (racingHudCanvas != null) { Object.Destroy(racingHudCanvas.gameObject); racingHudCanvas = null; }
        racingHudText = null;
        racingSpeedometerNeedle = null;
        racingSpeedometerText = null;
        racingHeadlightL = null;
        racingHeadlightR = null;
        racingWorldLights.Clear();
        raceSegments.Clear();

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
        if (!isRacingActive) return;
        float dt = Time.unscaledDeltaTime;

        // ── Input ────────────────────────────────────────
        float throttle = 0f;
        bool  braking  = false;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)   throttle += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) braking   = true;

            // Steering wheel ramp — builds up to ±1 over ~0.35s, returns to 0 over ~0.2s
            bool steerLeft  = kb.aKey.isPressed || kb.leftArrowKey.isPressed;
            bool steerRight = kb.dKey.isPressed || kb.rightArrowKey.isPressed;
            float steerTarget = steerLeft ? -1f : steerRight ? 1f : 0f;
            float steerSpeed  = (steerLeft || steerRight) ? 2.8f : 5.0f; // ramp in slower than ramp out
            racingSteerInput  = Mathf.MoveTowards(racingSteerInput, steerTarget, steerSpeed * dt);

            if (kb.escapeKey.wasPressedThisFrame)
            {
                FinishRace(success: false);
                return;
            }
        }

        // ── Physics ──────────────────────────────────────
        float speed = racingVelocity.magnitude;

        // Steering — uses ramped input, stronger max force
        float steerAmount = racingSteerInput * RacingSteerForce * Mathf.Clamp01(speed / 3.5f) * dt;
        racingAngularVel += steerAmount;
        racingAngularVel *= Mathf.Pow(RacingAngularDrag, dt * 60f);

        racingTruckAngle += racingAngularVel * dt;

        // Forward direction in XZ world
        float rad = racingTruckAngle * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        Vector2 right   = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));

        // Acceleration with torque curve — full power at low speed, tapers to 0 at max
        float torque = Mathf.Pow(1f - Mathf.Clamp01(speed / RacingMaxSpeed), 0.6f);
        racingVelocity += forward * throttle * RacingAcceleration * torque * dt;

        // Lateral friction (simulates grip)
        float lateralSpeed = Vector2.Dot(racingVelocity, right);
        racingVelocity -= right * (lateralSpeed * RacingLateralFriction * dt);

        // Brake — hard deceleration along current velocity direction
        if (braking && speed > 0.05f)
        {
            float brakeFactor = Mathf.Clamp01(1.4f * dt); // full stop from max in ~7s
            racingVelocity -= racingVelocity.normalized * speed * brakeFactor;
        }

        // Drag
        racingVelocity *= Mathf.Pow(RacingDrag, dt * 60f);

        // Speed cap
        if (racingVelocity.sqrMagnitude > RacingMaxSpeed * RacingMaxSpeed)
            racingVelocity = racingVelocity.normalized * RacingMaxSpeed;

        // Update position
        racingTruckPos.x += racingVelocity.x * dt;
        racingTruckPos.z += racingVelocity.y * dt;

        // ── Apply truck transform ─────────────────────────
        if (racingTruckVisual != null)
        {
            racingTruckVisual.transform.position = racingTruckPos;
            racingTruckVisual.transform.rotation = Quaternion.Euler(0f, racingTruckAngle, 0f);

            float wheelSpin = speed * dt * 180f;
            if (racingTruckWheelFL != null) racingTruckWheelFL.Rotate(Vector3.right, wheelSpin, Space.Self);
            if (racingTruckWheelFR != null) racingTruckWheelFR.Rotate(Vector3.right, wheelSpin, Space.Self);
            if (racingTruckWheelRL != null) racingTruckWheelRL.Rotate(Vector3.right, wheelSpin, Space.Self);
            if (racingTruckWheelRR != null) racingTruckWheelRR.Rotate(Vector3.right, wheelSpin, Space.Self);
        }

        // ── Camera follow ─────────────────────────────────
        if (racingCamera != null)
        {
            Quaternion camRot = Quaternion.Euler(14f, racingTruckAngle, 0f);
            Vector3 camBack   = camRot * Vector3.back * 2.8f;
            Vector3 targetPos = racingTruckPos + Vector3.up * 1.4f + camBack;
            racingCamera.transform.position = Vector3.Lerp(
                racingCamera.transform.position, targetPos, 6f * dt);
            racingCamera.transform.rotation = Quaternion.Slerp(
                racingCamera.transform.rotation, camRot, 6f * dt);
        }

        // ── Finish check ──────────────────────────────────
        float distToFinish = Vector3.Distance(
            new Vector3(racingTruckPos.x, 0f, racingTruckPos.z),
            new Vector3(raceFinishPos.x, 0f, raceFinishPos.z));

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

        if (distToFinish < RaceFinishRadius)
        {
            FinishRace(success: true);
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

        // Body
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(racingTruckVisual.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        body.transform.localScale    = new Vector3(0.72f, 0.30f, 1.0f);
        ApplyColor(body, bodyColor);
        ConfigureShadowVisual(body);

        // Cabin
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(racingTruckVisual.transform, false);
        cabin.transform.localPosition = new Vector3(0f, 0.40f, 0.20f);
        cabin.transform.localScale    = new Vector3(0.58f, 0.34f, 0.44f);
        ApplyColor(cabin, cabinColor);
        ConfigureShadowVisual(cabin);

        // Wheels
        racingTruckWheelFL = CreateRacingWheel(racingTruckVisual.transform, new Vector3(-0.42f, 0.11f,  0.32f), wheelColor);
        racingTruckWheelFR = CreateRacingWheel(racingTruckVisual.transform, new Vector3( 0.42f, 0.11f,  0.32f), wheelColor);
        racingTruckWheelRL = CreateRacingWheel(racingTruckVisual.transform, new Vector3(-0.42f, 0.11f, -0.32f), wheelColor);
        racingTruckWheelRR = CreateRacingWheel(racingTruckVisual.transform, new Vector3( 0.42f, 0.11f, -0.32f), wheelColor);

        // Headlights (forward-facing, match regular truck threshold)
        racingHeadlightL = CreateRacingHeadlight(racingTruckVisual.transform, new Vector3(-0.28f, 0.28f, 0.52f));
        racingHeadlightR = CreateRacingHeadlight(racingTruckVisual.transform, new Vector3( 0.28f, 0.28f, 0.52f));
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
        wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        wheel.transform.localScale    = new Vector3(0.20f, 0.06f, 0.20f);
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
        racingCamera.backgroundColor = new Color(0.05f, 0.06f, 0.08f);
        racingCamera.depth           = mainCamera != null ? mainCamera.depth + 10 : 10;
        racingCamera.cullingMask     = ~0;
        racingCamera.nearClipPlane   = 0.1f;
        racingCamera.farClipPlane    = 200f;

        Quaternion initRot = Quaternion.Euler(14f, racingTruckAngle, 0f);
        racingCamera.transform.position = racingTruckPos + Vector3.up * 1.4f + initRot * Vector3.back * 2.8f;
        racingCamera.transform.rotation = initRot;
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
                obj.transform.localScale = Vector3.one * Random.Range(2.8f, 4.2f);
                CreateTreeVariant(obj.transform, attempt % 3);
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
