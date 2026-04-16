using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private enum TutorialTrigger
    {
        GameStarted,
        BuildMotelPrompt,
        BuildMenuOpened,
        FirstRoadBuilt,
        FirstMotelBuilt,
        WorkersPanelOpened,
        FirstDriverHired,
        ForestIntroduction,
        FirstTradeOpened
    }

    private sealed class TutorialHudRefs
    {
        public GameObject     CanvasRoot;
        public GameObject     OverlayRoot;          // dark fullscreen bg parent
        public Image          OverlayImage;         // tinted in center mode, transparent in side mode
        public RectTransform  WindowRect;           // the floating card — repositioned per mode
        public LayoutElement  BodyPanelLayout;      // body height changed per mode
        public Text TitleText;
        public Text StepText;
        public Text BodyText;
        public GameObject BuildMenuOutlineRoot;
        public GameObject WorkersMenuOutlineRoot;
        public GameObject HireWorkerOutlineRoot;
        public GameObject ShiftsMenuOutlineRoot;
        public Toggle SkipToggle;
        public Text SkipToggleText;
        public Button OkButton;
        public Text OkButtonText;
    }

    private const float TutorialSidePanelBaseX  = 570f;   // right of the 760-wide Drivers panel
    private const float TutorialSidePanelLeftX  = -340f;  // left of center for Forest tutorial
    private static readonly Vector3 TutorialCinematicZoomOffset = new(-5f, 8f, -5f); // close-up during hire cinematic
    private bool  isTutorialSideMode;
    private bool  tutorialSideOnLeft;   // side card on left side instead of right
    private float tutorialBobTime;

    private TutorialHudRefs tutorialHud;
    private TutorialTrigger activeTutorialTrigger;
    private const int TutorialStepCount = 6;

    private bool IsTutorialEnabledForCurrentMode()
    {
        return selectedGameStartMode == GameStartMode.User && !isTutorialSkipped;
    }

    private void ScheduleTutorial(TutorialTrigger trigger)
    {
        pendingTutorialTrigger = trigger;
        pendingTutorialDelay   = 0.12f;
    }

    private void TryShowTutorial(TutorialTrigger trigger)
    {
        if (!IsTutorialEnabledForCurrentMode() || isTutorialOpen)
        {
            return;
        }

        switch (trigger)
        {
            case TutorialTrigger.GameStarted:
                if (hasShownWelcomeTutorial)
                {
                    return;
                }

                hasShownWelcomeTutorial = true;
                CenterCameraOnWorldForTutorial();
                ShowTutorialWindow(
                    TutorialTrigger.GameStarted,
                    1,
                    "Welcome to Lo-Fi Delivery Co.",
                    "Alright, buddy, you've been handed a problem the size of a small town. For some reason, it even comes with a steering wheel.\n\nThe map is empty - right where the road is supposed to be. You'll have to... convince it to appear.\nAnd buildings, unfortunately, won't build themselves.\n\nStart gently: take a look around, fill in what's missing, and try not to go bankrupt before noon.");
                break;
            case TutorialTrigger.BuildMotelPrompt:
                isBuildHighlightPersistent = true;
                ShowTutorialWindow(
                    TutorialTrigger.BuildMotelPrompt,
                    2,
                    "Build a Motel",
                    "A motel is a soft landing for people who do not yet understand what they have agreed to.\n\nIn Lo-Fi Delivery Co. it is where drivers are hired, wait between shifts, and pretend the road has not already taken something from them.\n\nOpen the Building menu at the top - or press B - choose Motel, and place it wherever your optimism still fits.");
                break;
            case TutorialTrigger.FirstMotelBuilt:
                if (hasShownFirstMotelTutorial) return;
                hasShownFirstMotelTutorial = true;
                isBuildHighlightPersistent   = false;
                isWorkersHighlightPersistent = true;
                ShowTutorialWindow(
                    TutorialTrigger.FirstMotelBuilt,
                    3,
                    "Open the Workers Panel",
                    "The motel stands. Structurally sound, morally ambiguous - the usual.\n\nNow it needs a person inside it. Someone to drive the truck, carry the crates, and ask no questions about where the money went.\n\nOpen the Workers panel at the top of the screen.");
                break;
            case TutorialTrigger.WorkersPanelOpened:
                if (hasShownWorkersPanelTutorial) return;
                hasShownWorkersPanelTutorial = true;
                isHireWorkerHighlightPersistent = true;
                ShowTutorialWindow(
                    TutorialTrigger.WorkersPanelOpened,
                    4,
                    "Hire a Worker",
                    "The Workers panel is open. Good. You are further along than most people get.\n\nThere is a button at the bottom. Hire New Worker. It costs money — some of which you still have.\n\nPress it.");
                break;
            case TutorialTrigger.FirstDriverHired:
                if (hasShownFirstDriverHiredTutorial) return;
                hasShownFirstDriverHiredTutorial = true;
                ShowTutorialWindow(
                    TutorialTrigger.FirstDriverHired,
                    5,
                    "The Worker is on Their Way!",
                    "A human being will arrive by bus, already wearing the expression of someone who has made several poor decisions to get here.\n\nThat is your employee now. Treat them accordingly.");
                break;
            case TutorialTrigger.ForestIntroduction:
                if (hasShownForestIntroTutorial) return;
                hasShownForestIntroTutorial = true;
                // Focus camera on Forest and select it (microhud appears)
                if (locations.ContainsKey(LocationType.Forest))
                {
                    Vector3 forestCenter = GetLocationCenter(LocationType.Forest);
                    forestCenter.y = 0f;
                    cameraFocusPoint = forestCenter;
                    isCameraReturningToDiorama = true;
                    isCameraRotatingToTarget   = false;
                    selectedLocation = LocationType.Forest;
                }
                isShiftsHighlightPersistent = true;
                isTutorialSideMode = true;
                tutorialSideOnLeft = true;
                ShowTutorialWindow(
                    TutorialTrigger.ForestIntroduction,
                    6,
                    "Your Land is Rich in Timber",
                    "Our region is blessed with magnificent forests that have been patiently waiting to be logged.\n\nA Forest provides raw logs. A Sawmill turns them into boards. That is where the money starts.\n\nPut your new worker to work — assign them a shift at the Forest so the timber starts moving.");
                break;
        }
    }

    private void ShowTutorialWindow(TutorialTrigger trigger, int stepNumber, string title, string body)
    {
        SetupTutorialUi();
        activeTutorialTrigger = trigger;
        // isTutorialSideMode / tutorialSideOnLeft may have been pre-set by the caller; only
        // auto-assign when the caller left them at their default (right-side = drivers panel open).
        if (!tutorialSideOnLeft)
            isTutorialSideMode = isDriversPanelOpen;
        ApplyTutorialWindowLayout();
        tutorialHud.TitleText.text = L(title);
        tutorialHud.StepText.text = $"{stepNumber}/{TutorialStepCount}";
        tutorialHud.BodyText.text = L(body);
        tutorialHud.SkipToggle.isOn = false;
        isTutorialOpen = true;
        tutorialHud.CanvasRoot.SetActive(true);
        PlayUiSound(uiPanelOpenClip, 0.82f);
        SessionDebugLogger.Log("TUTORIAL", $"Shown tutorial window: {title} (side={isTutorialSideMode}).");
    }

    private void OpenWorkersPanelFromTutorial()
    {
        isFleetPanelOpen     = false;
        isShiftsPanelOpen    = false;
        isResourcesPanelOpen = false;
        isEconomyPanelOpen   = false;
        isBuildPanelOpen     = false;
        isWorldMapPanelOpen  = false;
        isDriversPanelOpen   = true;
        isWorkersHighlightPersistent = false;
        isDriversScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.WorkersPanelOpened);
        LogUiInput("Tutorial: auto-opened Workers panel after tutorial 3 OK");
        PlayUiSound(uiPanelOpenClip, 0.9f);
    }

    private void OpenBuildPanelFromTutorial()
    {
        // Close all panels then open Build — same effect as clicking the Building button
        isFleetPanelOpen      = false;
        isShiftsPanelOpen     = false;
        isDriversPanelOpen    = false;
        isResourcesPanelOpen  = false;
        isEconomyPanelOpen    = false;
        isWorldMapPanelOpen   = false;
        isBuildPanelOpen      = true;
        isBuildHighlightPersistent = false;
        isBuildScreenDirty    = true;
        LogUiInput("Tutorial: auto-opened Build panel after tutorial 2 OK");
        PlayUiSound(uiPanelOpenClip, 0.9f);
    }

    private static readonly Color OverlayColorFull        = new(0.02f, 0.03f, 0.05f, 0.56f);
    private static readonly Color OverlayColorTransparent = new(0f, 0f, 0f, 0f);

    private void ApplyTutorialWindowLayout()
    {
        if (tutorialHud?.WindowRect == null) return;
        if (isTutorialSideMode)
        {
            float xBase = tutorialSideOnLeft ? TutorialSidePanelLeftX : TutorialSidePanelBaseX;
            // Left-offset mode needs full-size window to fit longer text; right side uses compact card
            float w          = tutorialSideOnLeft ? 480f  : 360f;
            float h          = tutorialSideOnLeft ? 380f  : 290f;
            float bodyH      = tutorialSideOnLeft ? 200f  : 148f;
            tutorialHud.WindowRect.sizeDelta        = new Vector2(w, h);
            tutorialHud.WindowRect.anchoredPosition = new Vector2(xBase, 0f);
            if (tutorialHud.BodyPanelLayout != null) tutorialHud.BodyPanelLayout.preferredHeight = bodyH;
            if (tutorialHud.OverlayImage    != null) tutorialHud.OverlayImage.color = OverlayColorTransparent;
            tutorialBobTime = 0f;
        }
        else
        {
            tutorialHud.WindowRect.sizeDelta        = new Vector2(580f, 370f);
            tutorialHud.WindowRect.anchoredPosition = Vector2.zero;
            if (tutorialHud.BodyPanelLayout != null) tutorialHud.BodyPanelLayout.preferredHeight = 190f;
            if (tutorialHud.OverlayImage    != null) tutorialHud.OverlayImage.color = OverlayColorFull;
        }
    }

    private void SetupTutorialUi()
    {
        if (tutorialHud != null)
        {
            return;
        }

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tutorialHud = new TutorialHudRefs();

        GameObject canvasObject = new("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        tutorialHud.CanvasRoot = canvasObject;

        RectTransform overlay = CreateStyledPanel("TutorialOverlay", canvasObject.transform, new Color(0.02f, 0.03f, 0.05f, 0.56f));
        StretchRect(overlay, 0f, 0f, 0f, 0f);
        tutorialHud.OverlayRoot  = overlay.gameObject;
        tutorialHud.OverlayImage = overlay.GetComponent<Image>();
        tutorialHud.OverlayImage.raycastTarget = false;   // window button handles its own input
        tutorialHud.BuildMenuOutlineRoot   = CreateTutorialMenuButtonOutline("TutorialBuildMenuOutline",   canvasObject.transform, 397f);
        tutorialHud.WorkersMenuOutlineRoot = CreateTutorialMenuButtonOutline("TutorialWorkersMenuOutline", canvasObject.transform, 112f);
        tutorialHud.ShiftsMenuOutlineRoot  = CreateTutorialMenuButtonOutline("TutorialShiftsMenuOutline",  canvasObject.transform, 207f);
        tutorialHud.HireWorkerOutlineRoot  = CreateTutorialHireButtonOutline("TutorialHireWorkerOutline",  canvasObject.transform);

        // Window stays a child of the overlay (same coordinate space = full canvas)
        RectTransform window = CreateStyledPanel("TutorialWindow", overlay, FleetPanelColor);
        SetCenteredWindow(window, 580f, 370f, 0f);
        tutorialHud.WindowRect = window;

        VerticalLayoutGroup layout = window.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 22, 22);
        layout.spacing = 14;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("TutorialHeaderRow", window, 34f, 12f);
        tutorialHud.TitleText = CreateHeaderText("TutorialTitle", headerRow, font, string.Empty, 24, TextAnchor.MiddleLeft, Color.white);
        LayoutElement titleLayout = tutorialHud.TitleText.gameObject.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;
        titleLayout.preferredHeight = 34f;
        tutorialHud.StepText = CreateHeaderText("TutorialStep", headerRow, font, string.Empty, 14, TextAnchor.MiddleRight, FleetAccentColor);
        LayoutElement stepLayout = tutorialHud.StepText.gameObject.AddComponent<LayoutElement>();
        stepLayout.preferredWidth = 64f;
        stepLayout.preferredHeight = 34f;

        RectTransform bodyPanel = CreateStyledPanel("TutorialBodyPanel", window, FleetInsetColor);
        LayoutElement bodyLayoutElem = bodyPanel.gameObject.AddComponent<LayoutElement>();
        bodyLayoutElem.preferredHeight = 190f;
        tutorialHud.BodyPanelLayout = bodyLayoutElem;
        VerticalLayoutGroup bodyLayout = bodyPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.padding = new RectOffset(14, 14, 12, 12);
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = true;
        tutorialHud.BodyText = CreateBodyText("TutorialBody", bodyPanel, font, string.Empty, 15, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        tutorialHud.BodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        tutorialHud.BodyText.verticalOverflow = VerticalWrapMode.Overflow;

        RectTransform actionRow = CreateLayoutRow("TutorialActionRow", window, 42f, 12f);
        tutorialHud.SkipToggle = CreateTutorialSkipToggle(actionRow, font);
        LayoutElement skipToggleLayout = tutorialHud.SkipToggle.gameObject.AddComponent<LayoutElement>();
        skipToggleLayout.flexibleWidth = 1f;
        skipToggleLayout.preferredHeight = 28f;

        tutorialHud.OkButton = CreateButton("TutorialOkButton", actionRow, font, out tutorialHud.OkButtonText, "OK", 14, FleetPrimaryButtonColor, Color.white);
        LayoutElement okLayout = tutorialHud.OkButton.gameObject.AddComponent<LayoutElement>();
        okLayout.preferredWidth = 120f;
        okLayout.preferredHeight = 34f;
        okLayout.flexibleWidth = 0f;
        tutorialHud.OkButtonText.raycastTarget = false;
        tutorialHud.OkButton.onClick.AddListener(CloseCurrentTutorialWindow);

        tutorialHud.CanvasRoot.SetActive(false);
    }

    private Toggle CreateTutorialSkipToggle(RectTransform parent, Font font)
    {
        RectTransform root = CreateUiObject("TutorialSkipToggle", parent).GetComponent<RectTransform>();
        HorizontalLayoutGroup layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        Toggle toggle = root.gameObject.AddComponent<Toggle>();

        RectTransform box = CreateStyledPanel("CheckBox", root, new Color(0.12f, 0.15f, 0.20f, 1f));
        box.sizeDelta = new Vector2(18f, 18f);
        LayoutElement boxLayout = box.gameObject.AddComponent<LayoutElement>();
        boxLayout.preferredWidth = 18f;
        boxLayout.preferredHeight = 18f;
        boxLayout.minWidth = 18f;
        boxLayout.minHeight = 18f;
        boxLayout.flexibleWidth = 0f;
        boxLayout.flexibleHeight = 0f;
        Image boxImage = box.GetComponent<Image>();

        RectTransform check = CreateStyledPanel("CheckMark", box, FleetAccentColor);
        check.anchorMin = new Vector2(0.22f, 0.22f);
        check.anchorMax = new Vector2(0.78f, 0.78f);
        check.offsetMin = Vector2.zero;
        check.offsetMax = Vector2.zero;
        Image checkImage = check.GetComponent<Image>();
        checkImage.raycastTarget = false;

        tutorialHud.SkipToggleText = CreateBodyText("SkipLabel", root, font, "Skip tutorial", 13, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        tutorialHud.SkipToggleText.raycastTarget = false;
        LayoutElement labelLayout = tutorialHud.SkipToggleText.gameObject.AddComponent<LayoutElement>();
        labelLayout.preferredHeight = 24f;
        labelLayout.preferredWidth = 128f;
        labelLayout.flexibleWidth = 0f;

        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        toggle.isOn = false;
        return toggle;
    }

    private void CenterCameraOnWorldForTutorial()
    {
        if (mainCamera == null)
        {
            return;
        }

        isTruckCameraFocused = false;
        isCameraReturningToDiorama = false;
        isCameraRotatingToTarget = false;
        cameraFocusPoint = new Vector3(GridWidth * 0.5f, 0f, GridHeight * 0.5f);
        cameraOffset = DioramaCameraOffset;
        cameraTargetOffset = DioramaCameraOffset;
        mainCamera.transform.position = cameraFocusPoint + cameraOffset;
        mainCamera.transform.rotation = GetDioramaCameraRotation();
    }

    private void CloseCurrentTutorialWindow()
    {
        if (tutorialHud == null)
        {
            return;
        }

        if (tutorialHud.SkipToggle != null && tutorialHud.SkipToggle.isOn)
        {
            isTutorialSkipped = true;
            SessionDebugLogger.Log("TUTORIAL", "Tutorial skipped by player.");
        }

        isTutorialOpen     = false;
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        PlayUiSound(uiPanelCloseClip, 0.82f);

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.GameStarted)
        {
            ScheduleTutorial(TutorialTrigger.BuildMotelPrompt);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.BuildMotelPrompt)
        {
            OpenBuildPanelFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.FirstMotelBuilt)
        {
            OpenWorkersPanelFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.WorkersPanelOpened)
        {
            HireNewDriver();
        }

        if (activeTutorialTrigger == TutorialTrigger.FirstDriverHired)
        {
            StartHireArrivalCinematic();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.ForestIntroduction)
        {
            selectedLocation            = null;       // close microhud
            isShiftsHighlightPersistent = false;
            isShiftsPanelOpen           = true;
            isShiftsScreenDirty         = true;
            LogUiInput("Tutorial: auto-opened Shifts panel after tutorial 6 OK");
            PlayUiSound(uiPanelOpenClip, 0.9f);
        }
    }

    private void StartHireArrivalCinematic()
    {
        if (hiringDriverArrival == null) return;   // bus already gone — nothing to track
        tutorialCinematicDriver = hiringDriverArrival.Driver;
        tutorialCinematicPhase  = TutorialCinematicPhase.TrackingBus;
        isTruckCameraFocused        = false;
        isCameraReturningToDiorama  = false;
        isCameraRotatingToTarget    = false;
        SessionDebugLogger.Log("TUTORIAL", "Started hire-arrival cinematic.");
    }

    private void UpdateTutorialCinematic(float dt)
    {
        Vector3 worldCenter = new(GridWidth * 0.5f, 0f, GridHeight * 0.5f);

        switch (tutorialCinematicPhase)
        {
            case TutorialCinematicPhase.TrackingBus:
            {
                if (hiringDriverArrival?.BusRootTransform != null)
                {
                    Vector3 busPos = hiringDriverArrival.BusRootTransform.position;
                    busPos.y = 0f;
                    // Faster lock-on once bus has stopped for dropoff
                    float busLerp = hiringDriverArrival.Phase == HiringDriverArrivalPhase.StoppedForDropoff ? 8f : 3.5f;
                    cameraFocusPoint = Vector3.Lerp(cameraFocusPoint, busPos, busLerp * dt);
                }
                // Zoom in toward bus/worker
                cameraOffset       = Vector3.Lerp(cameraOffset,       TutorialCinematicZoomOffset, 2.5f * dt);
                cameraTargetOffset = cameraOffset;

                bool driverSpawned = hiringDriverArrival == null
                    || hiringDriverArrival.Phase == HiringDriverArrivalPhase.DriverWalkingToMotel
                    || hiringDriverArrival.Phase == HiringDriverArrivalPhase.Departing;
                if (driverSpawned)
                {
                    if (tutorialCinematicDriver?.DriverObject != null)
                    {
                        // Snap focus to worker spawn position for a seamless cut
                        Vector3 spawnPos = tutorialCinematicDriver.DriverObject.transform.position;
                        spawnPos.y = 0f;
                        cameraFocusPoint = spawnPos;
                        tutorialCinematicPhase = TutorialCinematicPhase.TrackingWorker;
                    }
                    else
                    {
                        tutorialCinematicPhase = TutorialCinematicPhase.Returning;
                    }
                }
                break;
            }

            case TutorialCinematicPhase.TrackingWorker:
            {
                if (tutorialCinematicDriver?.DriverObject != null && tutorialCinematicDriver.DriverObject.activeSelf)
                {
                    Vector3 workerPos = tutorialCinematicDriver.DriverObject.transform.position;
                    workerPos.y = 0f;
                    cameraFocusPoint = Vector3.Lerp(cameraFocusPoint, workerPos, 4f * dt);
                }
                // Keep zoom in while following worker
                cameraOffset       = Vector3.Lerp(cameraOffset,       TutorialCinematicZoomOffset, 2.5f * dt);
                cameraTargetOffset = cameraOffset;

                if (tutorialCinematicDriver == null || !tutorialCinematicDriver.IsArrivingByBus)
                    tutorialCinematicPhase = TutorialCinematicPhase.Returning;
                break;
            }

            case TutorialCinematicPhase.Returning:
            {
                cameraFocusPoint   = Vector3.Lerp(cameraFocusPoint,   worldCenter,        2f   * dt);
                cameraOffset       = Vector3.Lerp(cameraOffset,       DioramaCameraOffset, 2.5f * dt);
                cameraTargetOffset = cameraOffset;
                bool focusDone  = (cameraFocusPoint - worldCenter).sqrMagnitude        < 0.5f;
                bool offsetDone = (cameraOffset     - DioramaCameraOffset).sqrMagnitude < 0.01f;
                if (focusDone && offsetDone)
                {
                    cameraFocusPoint        = worldCenter;
                    cameraOffset            = DioramaCameraOffset;
                    cameraTargetOffset      = DioramaCameraOffset;
                    tutorialCinematicDriver = null;
                    tutorialCinematicPhase  = TutorialCinematicPhase.None;
                    // Show tutorial 6 immediately — camera pan to Forest happens inside TryShowTutorial
                    ScheduleTutorial(TutorialTrigger.ForestIntroduction);
                    SessionDebugLogger.Log("TUTORIAL", "Hire-arrival cinematic ended.");
                }
                break;
            }
        }

        mainCamera.transform.position = cameraFocusPoint + cameraOffset;
        mainCamera.transform.rotation = GetDioramaCameraRotation();
    }

    private bool IsBuildMenuTutorialHighlightActive()
    {
        return (isTutorialOpen && activeTutorialTrigger == TutorialTrigger.BuildMotelPrompt)
               || isBuildHighlightPersistent;
    }

    private bool IsWorkersTutorialHighlightActive()
    {
        return (isTutorialOpen && (activeTutorialTrigger == TutorialTrigger.FirstMotelBuilt
                                || activeTutorialTrigger == TutorialTrigger.WorkersPanelOpened))
               || isWorkersHighlightPersistent;
    }

    private bool IsHireWorkerTutorialHighlightActive()
    {
        return (isTutorialOpen && activeTutorialTrigger == TutorialTrigger.WorkersPanelOpened)
               || isHireWorkerHighlightPersistent;
    }

    private bool IsShiftsTutorialHighlightActive()
    {
        return (isTutorialOpen && activeTutorialTrigger == TutorialTrigger.ForestIntroduction)
               || isShiftsHighlightPersistent;
    }

    private void UpdateTutorialUi()
    {
        if (pendingTutorialTrigger.HasValue)
        {
            pendingTutorialDelay -= Time.unscaledDeltaTime;
            if (pendingTutorialDelay <= 0f && !isTutorialOpen)
            {
                TutorialTrigger trigger = pendingTutorialTrigger.Value;
                pendingTutorialTrigger = null;
                TryShowTutorial(trigger);
            }
        }

        if (tutorialHud == null || tutorialHud.CanvasRoot == null)
        {
            return;
        }

        // Hide the window card when no tutorial is actively showing
        if (tutorialHud.WindowRect != null)
            tutorialHud.WindowRect.gameObject.SetActive(isTutorialOpen);

        bool anyHighlight = IsBuildMenuTutorialHighlightActive()
                         || IsWorkersTutorialHighlightActive()
                         || IsHireWorkerTutorialHighlightActive()
                         || IsShiftsTutorialHighlightActive();
        bool canvasNeeded = isTutorialOpen || anyHighlight;
        if (tutorialHud.CanvasRoot.activeSelf != canvasNeeded)
            tutorialHud.CanvasRoot.SetActive(canvasNeeded);

        // Bob animation for the floating side card
        if (isTutorialOpen && isTutorialSideMode && tutorialHud.WindowRect != null)
        {
            tutorialBobTime += Time.unscaledDeltaTime;
            float xBase = tutorialSideOnLeft ? TutorialSidePanelLeftX : TutorialSidePanelBaseX;
            float bobX  = Mathf.Sin(tutorialBobTime * 0.65f) * 3.5f;
            float bobY  = Mathf.Sin(tutorialBobTime * 1.15f) * 5f;
            tutorialHud.WindowRect.anchoredPosition = new Vector2(xBase + bobX, bobY);
        }

        if (tutorialHud.BuildMenuOutlineRoot != null)
            tutorialHud.BuildMenuOutlineRoot.SetActive(IsBuildMenuTutorialHighlightActive());

        if (tutorialHud.WorkersMenuOutlineRoot != null)
            tutorialHud.WorkersMenuOutlineRoot.SetActive(IsWorkersTutorialHighlightActive());

        if (tutorialHud.HireWorkerOutlineRoot != null)
            tutorialHud.HireWorkerOutlineRoot.SetActive(IsHireWorkerTutorialHighlightActive());

        if (tutorialHud.ShiftsMenuOutlineRoot != null)
            tutorialHud.ShiftsMenuOutlineRoot.SetActive(IsShiftsTutorialHighlightActive());
    }

    private GameObject CreateTutorialMenuButtonOutline(string name, Transform parent, float anchorX)
    {
        GameObject root = CreateUiObject(name, parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(anchorX, -17f);
        rootRect.sizeDelta = new Vector2(MenuBtnW, MenuBtnH);

        CreateTutorialOutlineBar("Top", root.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, 3f), new Vector2(0f, 3f));
        CreateTutorialOutlineBar("Bottom", root.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, -3f), new Vector2(0f, 3f));
        CreateTutorialOutlineBar("Left", root.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(-3f, 0f), new Vector2(3f, 0f));
        CreateTutorialOutlineBar("Right", root.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(3f, 0f), new Vector2(3f, 0f));
        root.SetActive(false);
        return root;
    }

    private static GameObject CreateTutorialHireButtonOutline(string name, Transform parent)
    {
        // Positioned over the "Hire New Worker" button inside the Drivers panel (760×560, yOffset=-16)
        // Button sits at the bottom of the panel — approximate canvas-space center: (0, -228)
        GameObject root = CreateUiObject(name, parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = new Vector2(0f, -228f);
        rootRect.sizeDelta = new Vector2(688f, 44f);

        CreateTutorialOutlineBar("Top",    root.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f,  3f), new Vector2(0f, 3f));
        CreateTutorialOutlineBar("Bottom", root.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, -3f), new Vector2(0f, 3f));
        CreateTutorialOutlineBar("Left",   root.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(-3f, 0f), new Vector2(3f, 0f));
        CreateTutorialOutlineBar("Right",  root.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2( 3f, 0f), new Vector2(3f, 0f));
        root.SetActive(false);
        return root;
    }

    private static void CreateTutorialOutlineBar(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject bar = CreateUiObject($"TutorialBuildMenuOutline{name}", parent);
        RectTransform rect = bar.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        Image image = bar.AddComponent<Image>();
        image.color = new Color(1f, 0.08f, 0.04f, 1f);
        image.raycastTarget = false;
    }
}
