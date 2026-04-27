using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void SetTutorialClockToProductionStart()
    {
        if (IsProductionWorkHour(GetCurrentHour()))
        {
            return;
        }

        dayNightCycleTimer = (ProductionWorkStartHour / 24f) * DayNightCycleDuration + 0.01f;
        SessionDebugLogger.Log("TUTORIAL", $"Tutorial forced clock to {ProductionWorkStartHour:00}:00 so the Forest worker can start production.");
    }

    private void NotifyTutorialProductionWorkerEntered(LocationType locationType)
    {
        if (locationType != LocationType.Forest || hasShownForestWorkerStartedTutorial)
        {
            return;
        }

        // The force-commute orbit HUD becomes a centered normal HUD once the worker arrives.
        DetachTutorialOrbitHudFromTarget("production worker reached assigned building");
        if (tutorialCinematicPhase == TutorialCinematicPhase.TrackingWorkerBackCloseup)
            tutorialCinematicPhase = TutorialCinematicPhase.Returning;

        if (tutorialOrbitHudRoot != null && tutorialOrbitHudRoot.activeSelf)
        {
            tutorialPendingForestWorkerStartedAfterOrbitOk = true;
            SessionDebugLogger.Log("TUTORIAL", "Forest worker-start tutorial deferred until orbit HUD OK.");
            return;
        }

        ScheduleTutorial(TutorialTrigger.ForestWorkerStarted);
    }

    private void NotifyTutorialSawmillBuilt()
    {
        if (!hasShownNeedSawmillTutorial || hasShownSawmillBuiltTutorial)
        {
            return;
        }

        ScheduleTutorial(TutorialTrigger.SawmillBuilt);
    }

    private void StartTutorialBusStopWorkerArrival()
    {
        if (!locations.ContainsKey(LocationType.IntercityStop) || !locations.ContainsKey(LocationType.Motel))
        {
            return;
        }

        DriverAgent firstWorker = null;
        for (int i = 0; i < 2; i++)
        {
            DriverAgent worker = CreateAndRegisterDriverAgent(spawnInMotel: false);
            Vector3 spawnPosition = GetTutorialBusStopWorkerSpawnPosition(i);
            worker.DriverObject.SetActive(true);
            worker.DriverObject.transform.position = spawnPosition;
            worker.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            worker.WalkAnimationTime = 0f;
            worker.WalkPhase = DriverRescuePhase.ToMotelFromBusStop;
            worker.WalkTargetWorld = worker.MotelIdlePosition;
            worker.IdleWanderPauseTimer = 0f;
            worker.IdleWanderPointIndex = -1;
            worker.IdleConversationTimer = 0f;
            worker.IdleConversationPartnerId = -1;
            ApplyDriverPose(worker, 0f, 0f);
            BuildDriverWalkPath(worker, spawnPosition, worker.MotelIdlePosition);
            if (firstWorker == null)
            {
                firstWorker = worker;
            }
        }

        if (firstWorker != null)
        {
            SetupTutorialUi();
            isFleetHighlightPersistent = true;
            tutorialCinematicDriver = firstWorker;
            tutorialCinematicPhase = TutorialCinematicPhase.TrackingWorkerBackCloseup;
            tutorialCinematicShouldShowForestIntro = false;
            isTutorialCameraFocusActive = false;
            isTruckCameraFocused = false;
            isCameraReturningToDiorama = false;
            isCameraRotatingToTarget = false;
            ShowTutorialOrbitHud(
                firstWorker,
                "Two new workers have arrived at the Bus Stop and will walk to the Motel.\n\nNext, open Fleet and assign a free worker to Truck #1.",
                $"12/{TutorialStepCount}",
                speakerProfessionOverrideKey: "(for now) unemployed",
                onOk: () =>
                {
                    isFleetHighlightPersistent = false;
                    isTutorialOpen = false;
                    isTutorialSideMode = false;
                    tutorialSideOnLeft = false;
                    isShiftsPanelOpen = false;
                    isDriversPanelOpen = false;
                    isResourcesPanelOpen = false;
                    isEconomyPanelOpen = false;
                    isBuildPanelOpen = false;
                    isWorldMapPanelOpen = false;
                    isFleetPanelOpen = true;
                    isFleetScreenDirty = true;
                    ScheduleTutorial(TutorialTrigger.FleetSelectTruck);
                    LogUiInput("Tutorial: orbit HUD OK вЂ” opened Fleet panel (step 12).");
                    PlayUiSound(uiPanelOpenClip, 0.9f);
                });
            SessionDebugLogger.Log("TUTORIAL", "Spawned two tutorial workers at Bus Stop and started worker-follow camera.");
        }

        isDriversScreenDirty = true;
        isFleetScreenDirty = true;
    }

    private Vector3 GetTutorialBusStopWorkerSpawnPosition(int index)
    {
        LocationData busStop = locations[LocationType.IntercityStop];
        Vector2Int cell = index == 0 ? busStop.Min : busStop.Max;
        if (cell == busStop.Min && index > 0)
        {
            cell = busStop.Anchor != busStop.Min ? busStop.Anchor : new Vector2Int(Mathf.Min(GridWidth - 1, busStop.Min.x + 1), busStop.Min.y);
        }

        Vector3 position = GetCellCenter(cell);
        float sideOffset = index == 0 ? 0.28f : -0.28f;
        position += new Vector3(sideOffset, 0f, 0.62f);
        position.y = SampleTerrainHeight(position.x, position.z);
        return position;
    }

    private void ShowTutorialOrbitHud(DriverAgent worker, string message, string stepLabel = "", System.Action onOk = null, string speakerProfessionOverrideKey = null)
    {
        if (worker?.DriverObject == null)
        {
            return;
        }

        if (tutorialOrbitHudRoot == null)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // ScreenSpaceOverlay so the panel renders on top of all 3D geometry
            tutorialOrbitHudRoot = new GameObject("TutorialOrbitTypewriterHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = tutorialOrbitHudRoot.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 950;

            CanvasScaler scaler = tutorialOrbitHudRoot.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Panel floats at a computed anchor position; repositioned every frame from 3D orbit math
            RectTransform panel = CreateStyledPanel("OrbitTypewriterPanel", tutorialOrbitHudRoot.transform, new Color(0.04f, 0.06f, 0.09f, 0.88f));
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot     = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(380f, 230f);   // wider + taller so all text fits
            tutorialOrbitHudPanel = panel;

            // Step counter вЂ” top-right corner (e.g. "12/12")
            tutorialOrbitHudStepText = CreateBodyText("OrbitStepText", panel, font, string.Empty, 13, TextAnchor.UpperRight, FleetAccentColor);
            RectTransform stepRect = tutorialOrbitHudStepText.GetComponent<RectTransform>();
            stepRect.anchorMin = new Vector2(1f, 1f);
            stepRect.anchorMax = new Vector2(1f, 1f);
            stepRect.pivot     = new Vector2(1f, 1f);
            stepRect.anchoredPosition = new Vector2(-10f, -8f);
            stepRect.sizeDelta = new Vector2(72f, 22f);
            tutorialOrbitHudStepText.raycastTarget = false;

            // Main body text вЂ” font 13, wrapping enabled, leaves room for step counter top + OK button bottom
            tutorialOrbitHudText = CreateBodyText("OrbitTypewriterText", panel, font, string.Empty, 13, TextAnchor.UpperLeft, Color.white);
            tutorialOrbitHudText.horizontalOverflow = HorizontalWrapMode.Wrap;
            tutorialOrbitHudText.verticalOverflow   = VerticalWrapMode.Overflow;
            RectTransform textRect = tutorialOrbitHudText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 1f);
            textRect.offsetMin = new Vector2(16f, 50f);   // bottom: OK button
            textRect.offsetMax = new Vector2(-16f, -28f); // top: step counter

            // OK button anchored to the bottom of the panel
            tutorialOrbitHudOkButton = CreateButton("OrbitOkButton", panel, font, out Text orbitOkText, "OK", 18, FleetPrimaryButtonColor, Color.white);
            RectTransform okRect = tutorialOrbitHudOkButton.GetComponent<RectTransform>();
            okRect.anchorMin        = new Vector2(0f, 0f);
            okRect.anchorMax        = new Vector2(1f, 0f);
            okRect.pivot            = new Vector2(0.5f, 0f);
            okRect.anchoredPosition = new Vector2(0f, 10f);
            okRect.sizeDelta        = new Vector2(-36f, 36f);
            orbitOkText.raycastTarget = false;
            tutorialOrbitHudOkButton.onClick.AddListener(() =>
            {
                System.Action okAction = tutorialOrbitHudOnOk;
                bool shouldShowPendingForestWorkerStarted = tutorialPendingForestWorkerStartedAfterOrbitOk;
                tutorialOrbitHudOnOk = null;
                HideTutorialOrbitHud();
                okAction?.Invoke();
                if (shouldShowPendingForestWorkerStarted &&
                    IsTutorialEnabledForCurrentMode() &&
                    !hasShownForestWorkerStartedTutorial)
                {
                    ScheduleTutorial(TutorialTrigger.ForestWorkerStarted);
                }
                tutorialCinematicPhase = TutorialCinematicPhase.Returning;
            });
        }

        tutorialOrbitHudOnOk = onOk;
        tutorialOrbitHudDriver = worker;
        tutorialOrbitHudAttachedWalkPhase = worker.WalkPhase;
        tutorialOrbitHudDetached = false;
        tutorialOrbitHudSpeakerPrefix = GetOrbitHudSpeakerPrefix(worker, speakerProfessionOverrideKey);
        tutorialOrbitHudBodyText = L(message);
        tutorialOrbitHudTypeTime = 0f;
        tutorialOrbitHudOrbitTime = 0f;
        tutorialOrbitHudText.text = string.Empty;
        if (tutorialOrbitHudStepText != null)
            tutorialOrbitHudStepText.text = stepLabel;
        tutorialOrbitHudRoot.SetActive(true);
        UpdateTutorialOrbitHud(0f);
    }

    private void UpdateTutorialOrbitHud(float dt)
    {
        if (tutorialOrbitHudRoot == null || tutorialOrbitHudText == null)
        {
            return;
        }

        tutorialOrbitHudTypeTime += dt;
        tutorialOrbitHudOrbitTime += dt;
        int visibleChars = Mathf.Clamp(Mathf.FloorToInt(tutorialOrbitHudTypeTime * tutorialOrbitHudTypeSpeed), 0, tutorialOrbitHudBodyText.Length);
        tutorialOrbitHudText.text = $"{tutorialOrbitHudSpeakerPrefix}\n\n{tutorialOrbitHudBodyText.Substring(0, visibleChars)}";

        if (!tutorialOrbitHudDetached && ShouldDetachTutorialOrbitHudFromTarget())
        {
            DetachTutorialOrbitHudFromTarget("attached character reached destination");
        }

        if (tutorialOrbitHudDetached)
        {
            if (tutorialOrbitHudPanel != null)
            {
                tutorialOrbitHudPanel.gameObject.SetActive(true);
                tutorialOrbitHudPanel.anchoredPosition = Vector2.Lerp(
                    tutorialOrbitHudPanel.anchoredPosition,
                    Vector2.zero,
                    1f - Mathf.Exp(-3.2f * dt));
            }
            return;
        }

        if (tutorialOrbitHudDriver?.DriverObject == null || mainCamera == null)
        {
            return;
        }

        // Compute 3D orbit position then project onto the screen-space overlay canvas
        Vector3 workerPos = tutorialOrbitHudDriver.DriverObject.transform.position;
        float angle = tutorialOrbitHudOrbitTime * 0.3f;
        Vector3 orbitOffset = new(Mathf.Cos(angle) * 0.95f, 1.62f + Mathf.Sin(angle * 1.7f) * 0.12f, Mathf.Sin(angle) * 0.95f);
        Vector3 worldPos  = workerPos + orbitOffset;

        if (tutorialOrbitHudPanel != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);
            bool inFront = screenPos.z > 0f;
            tutorialOrbitHudPanel.gameObject.SetActive(inFront);
            if (inFront)
            {
                // Convert screen point в†’ canvas-local position (no UI camera needed for SSO)
                RectTransform canvasRect = tutorialOrbitHudRoot.GetComponent<RectTransform>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, new Vector2(screenPos.x, screenPos.y), null, out Vector2 localPoint);
                tutorialOrbitHudPanel.anchoredPosition = localPoint;
            }
        }
    }

    private string GetOrbitHudSpeakerPrefix(DriverAgent worker, string professionOverrideKey = null)
    {
        string speakerName = string.IsNullOrWhiteSpace(worker?.DriverName) ? L("Unknown") : worker.DriverName;
        string descriptor = GetOrbitHudDescriptor(worker);
        string profession = string.IsNullOrWhiteSpace(professionOverrideKey)
            ? GetOrbitHudProfession(worker)
            : L(professionOverrideKey);
        string colorHex = ColorUtility.ToHtmlStringRGB(FleetAccentColor);
        return $"<color=#{colorHex}><b>{descriptor} {profession} {speakerName} {L("thinks:")}</b></color>";
    }

    private string GetOrbitHudDescriptor(DriverAgent worker)
    {
        int seed = worker?.DriverId ?? 0;
        if (worker?.AssignedBuildingType.HasValue == true)
        {
            seed = seed * 31 + (int)worker.AssignedBuildingType.Value;
        }
        else if (worker != null)
        {
            seed = seed * 31 + (int)worker.DutyMode;
        }

        int index = Mathf.Abs(seed) % TutorialOrbitHudDescriptorKeys.Length;
        return L(TutorialOrbitHudDescriptorKeys[index]);
    }

    private string GetOrbitHudProfession(DriverAgent worker)
    {
        if (worker?.AssignedBuildingType.HasValue == true)
        {
            return worker.AssignedBuildingType.Value switch
            {
                LocationType.Forest           => L("lumberjack"),
                LocationType.Sawmill          => L("sawyer"),
                LocationType.FurnitureFactory => L("cabinetmaker"),
                LocationType.Warehouse        => L("warehouse loader"),
                LocationType.Motel            => L("motel attendant"),
                LocationType.Bar              => L("bartender"),
                LocationType.Canteen          => L("canteen worker"),
                LocationType.Parking          => L("yard driver"),
                LocationType.GasStation       => L("fuel attendant"),
                LocationType.IntercityStop          => L("station hand"),
                _                             => L("worker")
            };
        }

        if (worker?.DutyMode == DriverDutyMode.Intercity)
        {
            return L("intercity driver");
        }

        if (worker?.AssignedTruckNumber > 0)
        {
            return L("driver");
        }

        return L("unemployed");
    }

    private bool ShouldDetachTutorialOrbitHudFromTarget()
    {
        if (tutorialOrbitHudDriver == null)
        {
            return false;
        }

        if (tutorialOrbitHudAttachedWalkPhase != DriverRescuePhase.None &&
            tutorialOrbitHudDriver.WalkPhase != tutorialOrbitHudAttachedWalkPhase)
        {
            return true;
        }

        if (tutorialOrbitHudAttachedWalkPhase == DriverRescuePhase.None ||
            tutorialOrbitHudDriver.DriverObject == null ||
            !tutorialOrbitHudDriver.DriverObject.activeSelf)
        {
            return false;
        }

        Vector3 flatDelta = tutorialOrbitHudDriver.WalkTargetWorld - tutorialOrbitHudDriver.DriverObject.transform.position;
        flatDelta.y = 0f;
        bool atFinalWaypoint = tutorialOrbitHudDriver.WalkPath.Count == 0 ||
                               tutorialOrbitHudDriver.WalkWaypointIndex >= tutorialOrbitHudDriver.WalkPath.Count - 1;
        return atFinalWaypoint && flatDelta.sqrMagnitude < 0.025f;
    }

    private void DetachTutorialOrbitHudFromTarget(string reason)
    {
        if (tutorialOrbitHudRoot == null || !tutorialOrbitHudRoot.activeSelf || tutorialOrbitHudDetached)
        {
            return;
        }

        tutorialOrbitHudDetached = true;
        tutorialOrbitHudAttachedWalkPhase = DriverRescuePhase.None;
        if (tutorialOrbitHudPanel != null)
        {
            tutorialOrbitHudPanel.gameObject.SetActive(true);
        }

        if (tutorialCinematicPhase == TutorialCinematicPhase.TrackingWorkerBackCloseup)
        {
            tutorialCinematicPhase = TutorialCinematicPhase.Returning;
        }
        else
        {
            isCameraReturningToDiorama = true;
            cameraTargetOffset = DioramaCameraOffset;
        }

        SessionDebugLogger.Log("TUTORIAL", $"Orbit HUD detached from target: {reason}.");
    }

    private void HideTutorialOrbitHud()
    {
        if (tutorialOrbitHudRoot != null)
        {
            tutorialOrbitHudRoot.SetActive(false);
        }

        tutorialOrbitHudDriver = null;
        tutorialOrbitHudSpeakerPrefix = string.Empty;
        tutorialOrbitHudBodyText = string.Empty;
        tutorialOrbitHudTypeTime = 0f;
        tutorialOrbitHudTypeSpeed = TutorialOrbitHudDefaultTypeSpeed;
        tutorialOrbitHudOrbitTime = 0f;
        tutorialOrbitHudDetached = false;
        tutorialPendingForestWorkerStartedAfterOrbitOk = false;
        tutorialOrbitHudAttachedWalkPhase = DriverRescuePhase.None;
    }
}
