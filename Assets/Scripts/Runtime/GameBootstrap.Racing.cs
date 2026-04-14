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

    private readonly List<RaceSegment> raceSegments = new();
    private Vector3 raceFinishPos;

    private Canvas racingHudCanvas;
    private Text racingHudText;

    private GameObject joinRaceButtonRoot;  // the "JOIN THE RACE" button canvas
    private Button joinRaceButton;
    private Text joinRaceButtonText;

    // ── Constants ────────────────────────────────────────────────────────────

    private const float RacingAcceleration  = 18f;
    private const float RacingMaxSpeed      = 14f;     // units/s
    private const float RacingDrag          = 0.94f;   // base per-second factor
    private const float RacingAngularDrag   = 0.80f;
    private const float RacingSteerForce    = 110f;    // deg/s per (speed unit)
    private const float RacingLateralFriction = 52f;
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
                          activeTradeRun != null &&
                          activeTradeRun.Phase == TradeRunPhase.OutOfMap;

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
        CreateRacingTruck();
        SetupRacingCamera();
        SetupRacingHud();

        // Place truck at start
        racingTruckPos   = raceSegments[0].Center - raceSegments[0].Rotation * Vector3.forward * raceSegments[0].Length * 0.45f;
        racingTruckPos.y = 0.35f;
        racingTruckAngle = raceSegments[0].Rotation.eulerAngles.y;
        racingVelocity   = Vector2.zero;
        racingAngularVel = 0f;

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

        // Destroy racing scene
        if (racingCamera != null) { Object.Destroy(racingCamera.gameObject); racingCamera = null; }
        if (racingSceneRoot != null) { Object.Destroy(racingSceneRoot); racingSceneRoot = null; }
        if (racingTruckVisual != null) { Object.Destroy(racingTruckVisual); racingTruckVisual = null; }
        if (racingHudCanvas != null) { Object.Destroy(racingHudCanvas.gameObject); racingHudCanvas = null; }
        racingHudText = null;
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
        float throttle = 0f, steer = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    throttle += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  throttle -= 0.55f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  steer    -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steer    += 1f;

            if (kb.escapeKey.wasPressedThisFrame)
            {
                FinishRace(success: false);
                return;
            }
        }

        // ── Physics ──────────────────────────────────────
        float speed = racingVelocity.magnitude;

        // Steering (speed-proportional, low speed → less steer)
        float steerAmount = steer * RacingSteerForce * Mathf.Clamp01(speed / 3.5f) * dt;
        racingAngularVel += steerAmount;
        racingAngularVel *= Mathf.Pow(RacingAngularDrag, dt * 60f);

        racingTruckAngle += racingAngularVel * dt;

        // Forward direction in XZ world
        float rad = racingTruckAngle * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        Vector2 right   = new Vector2(Mathf.Cos(rad), -Mathf.Sin(rad));

        // Acceleration
        racingVelocity += forward * throttle * RacingAcceleration * dt;

        // Lateral friction (simulates grip)
        float lateralSpeed = Vector2.Dot(racingVelocity, right);
        racingVelocity -= right * (lateralSpeed * RacingLateralFriction * dt);

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
            Vector3 targetPos = racingTruckPos + Vector3.up * 14f;
            racingCamera.transform.position = Vector3.Lerp(
                racingCamera.transform.position, targetPos, 5f * dt);
            racingCamera.transform.rotation = Quaternion.Euler(90f, racingTruckAngle, 0f);
        }

        // ── Finish check ──────────────────────────────────
        float distToFinish = Vector3.Distance(
            new Vector3(racingTruckPos.x, 0f, racingTruckPos.z),
            new Vector3(raceFinishPos.x, 0f, raceFinishPos.z));

        // ── HUD update ────────────────────────────────────
        if (racingHudText != null)
        {
            float kmh = speed * 3.6f;
            racingHudText.text =
                "INTERCITY DELIVERY\n" +
                "────────────────\n" +
                $"Speed:   {kmh:F0} km/h\n" +
                $"Finish:  {distToFinish:F0} m\n" +
                "────────────────\n" +
                "[ESC]  Skip";
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
        racingCamera.orthographic     = true;
        racingCamera.orthographicSize = 9f;
        racingCamera.clearFlags       = CameraClearFlags.SolidColor;
        racingCamera.backgroundColor  = new Color(0.05f, 0.06f, 0.08f);
        racingCamera.depth            = mainCamera != null ? mainCamera.depth + 10 : 10;
        racingCamera.cullingMask      = ~0;
        racingCamera.nearClipPlane    = 0.1f;
        racingCamera.farClipPlane     = 200f;

        racingCamera.transform.position = racingTruckPos + Vector3.up * 14f;
        racingCamera.transform.rotation = Quaternion.Euler(90f, racingTruckAngle, 0f);
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
    }
}
