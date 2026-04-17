using System.Collections.Generic;
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
        SelectProductionWorker,
        AssignForestProductionWorker,
        ForestWorkerStarted,
        FleetIntroduction,
        FleetSelectTruck,
        FleetAssignDriver,
        FleetPickDriver,
        NeedSawmill,
        SawmillBuilt,
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
        public GameObject FleetMenuOutlineRoot;
        public GameObject FirstWorkerOutlineRoot;
        public GameObject ForestAssignOutlineRoot;
        public GameObject FleetTruckOutlineRoot;
        public GameObject FleetAssignDriverOutlineRoot;
        public GameObject FleetDriverPickerOutlineRoot;
        public Toggle SkipToggle;
        public Text SkipToggleText;
        public Button OkButton;
        public Text OkButtonText;
    }

    private const float TutorialSidePanelBaseX  = 570f;   // right of the 760-wide Drivers panel
    private const float TutorialSidePanelLeftX  = -340f;  // left of center for Forest tutorial
    private const float TutorialCameraFocusSpeed = 2.8f;
    private static readonly Vector3 TutorialForestZoomOffset = new(-8f, 12f, -8f);
    private static readonly Vector3 TutorialCinematicZoomOffset = new(-5f, 8f, -5f); // close-up during hire cinematic
    private bool  isTutorialSideMode;
    private bool  tutorialSideOnLeft;   // side card on left side instead of right
    private float tutorialBobTime;
    private bool isTutorialCameraFocusActive;
    private Vector3 tutorialCameraFocusTarget;
    private float tutorialCameraWanderTime;
    private GameObject tutorialOrbitHudRoot;
    private RectTransform tutorialOrbitHudPanel;
    private Text tutorialOrbitHudText;
    private Text tutorialOrbitHudStepText;
    private Button tutorialOrbitHudOkButton;
    private System.Action tutorialOrbitHudOnOk;
    private DriverAgent tutorialOrbitHudDriver;
    private string tutorialOrbitHudFullText = string.Empty;
    private float tutorialOrbitHudTypeTime;
    private float tutorialOrbitHudOrbitTime;

    private TutorialHudRefs tutorialHud;
    private TutorialTrigger activeTutorialTrigger;
    private const int TutorialStepCount = 15;

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
                    StartTutorialCameraFocus(LocationType.Forest, placeLocationOnRight: true);
                    selectedLocation = LocationType.Forest;
                    RefreshSelectionVisuals();
                }
                isShiftsHighlightPersistent = true;
                isTutorialSideMode = true;
                tutorialSideOnLeft = true;
                ShowTutorialWindow(
                    TutorialTrigger.ForestIntroduction,
                    6,
                    "Timber, Unfortunately",
                    "Our region is blessed with magnificent forests that have been patiently waiting to be logged.\n\nA Forest provides raw logs. A Sawmill turns them into boards. That is where the money starts.\n\nPut your new worker to work — assign them a shift at the Forest so the timber starts moving.");
                break;
            case TutorialTrigger.SelectProductionWorker:
                if (driverAgents.Count == 0) return;
                isTutorialSideMode = true;
                tutorialSideOnLeft = true;
                ShowTutorialWindow(
                    TutorialTrigger.SelectProductionWorker,
                    7,
                    "Choose the Person",
                    "Before a building can pretend to be productive, it needs a person to blame.\n\nSelect your first worker in the list on the left. Or press OK and I will perform this tiny ceremony for you.");
                break;
            case TutorialTrigger.AssignForestProductionWorker:
                if (GetFirstAssignableProductionWorker() == null) return;
                isTutorialSideMode = true;
                tutorialSideOnLeft = false;
                ShowTutorialWindow(
                    TutorialTrigger.AssignForestProductionWorker,
                    8,
                    "Send Them to the Trees",
                    "There. A worker, selected and vulnerable to management.\n\nPress Assign on the Forest row. Or press OK, and the bureaucracy will complete itself.");
                break;
            case TutorialTrigger.ForestWorkerStarted:
                if (hasShownForestWorkerStartedTutorial) return;
                hasShownForestWorkerStartedTutorial = true;
                if (locations.ContainsKey(LocationType.Forest))
                {
                    StartTutorialCameraFocus(LocationType.Forest, placeLocationOnRight: true);
                    selectedLocation = LocationType.Forest;
                    RefreshSelectionVisuals();
                }
                isTutorialSideMode = true;
                tutorialSideOnLeft = true;
                ShowTutorialWindow(
                    TutorialTrigger.ForestWorkerStarted,
                    9,
                    "Look at Them Go",
                    "Observe our small hero at work. A person, a forest, and the ancient agreement between wage labor and questionable planning.\n\nIt is almost adorable. Do not say that out loud. Management has a reputation to maintain.");
                tutorialHud.BodyText.fontSize = 13;   // compact font so text fits neatly
                break;
            case TutorialTrigger.FleetIntroduction:
                if (hasShownFleetIntroTutorial) return;
                hasShownFleetIntroTutorial = true;
                isFleetHighlightPersistent = true;
                ShowTutorialWindow(
                    TutorialTrigger.FleetIntroduction,
                    10,
                    "Trucks Don't Drive Themselves",
                    "The Forest worker is settled in. But logs in a forest are just expensive scenery.\n\nOpen Fleet, assign a driver to a truck, and set a route. That is how cargo starts moving.");
                break;
            case TutorialTrigger.FleetSelectTruck:
                if (hasShownFleetSelectTruckTutorial) return;
                hasShownFleetSelectTruckTutorial = true;
                isTutorialSideMode = true;
                tutorialSideOnLeft = true;
                ShowTutorialWindow(
                    TutorialTrigger.FleetSelectTruck,
                    13,
                    "Select the Truck",
                    "The Fleet panel is open. Somewhere inside it sits Truck 1, a loyal rectangle with wheels and no opinions worth recording.\n\nSelect Truck 1 in the list. Or press OK, and I will gently point your attention at the machine that will soon inherit all our logistical sins.");
                tutorialHud.BodyText.fontSize = 13;
                break;
            case TutorialTrigger.FleetAssignDriver:
                if (hasShownFleetAssignDriverTutorial) return;
                hasShownFleetAssignDriverTutorial = true;
                isTutorialSideMode = true;
                tutorialSideOnLeft = true;
                ShowTutorialWindow(
                    TutorialTrigger.FleetAssignDriver,
                    14,
                    "Give It a Driver",
                    "A truck without a driver is just furniture with fuel anxiety.\n\nNow assign a free worker to Truck 1. Press Assign in the first driver slot, or press OK and I will open the little personnel drawer for you.");
                tutorialHud.BodyText.fontSize = 13;
                break;
            case TutorialTrigger.FleetPickDriver:
                if (hasShownFleetPickDriverTutorial) return;
                hasShownFleetPickDriverTutorial = true;
                isTutorialSideMode = true;
                tutorialSideOnLeft = false;
                ShowTutorialWindow(
                    TutorialTrigger.FleetPickDriver,
                    15,
                    "Choose a Driver",
                    "There they are: the available souls. Not the one currently serving the trees. We do not rip people out of production just because a dropdown got lonely.\n\nSelect a free worker from the list. Or press OK, and the first available volunteer will become Truck 1's problem.");
                tutorialHud.BodyText.fontSize = 13;
                break;
            case TutorialTrigger.NeedSawmill:
                if (hasShownNeedSawmillTutorial) return;
                hasShownNeedSawmillTutorial = true;
                isBuildHighlightPersistent = true;
                isTutorialSideMode = false;
                tutorialSideOnLeft = false;
                ShowTutorialWindow(
                    TutorialTrigger.NeedSawmill,
                    10,
                    "Logs Are Not Boards",
                    "A log is only furniture in its larval form.\n\nTo turn Logs into Boards, you need a Sawmill. Open Building, choose Sawmill, and place it where the roads can eventually make peace with it.");
                break;
            case TutorialTrigger.SawmillBuilt:
                if (hasShownSawmillBuiltTutorial) return;
                hasShownSawmillBuiltTutorial = true;
                isBuildHighlightPersistent = false;
                ShowTutorialWindow(
                    TutorialTrigger.SawmillBuilt,
                    11,
                    "Sawmill Placed",
                    "The Sawmill stands. A box with teeth, waiting for logs to become boards and for everyone involved to call that progress.\n\nOne delicate problem remains: resources do not teleport. Tragically. Trucks will have to carry them between buildings, because civilization is mostly moving piles from one rectangle to another.");
                break;
        }
    }

    private void ShowTutorialWindow(TutorialTrigger trigger, int stepNumber, string title, string body)
    {
        SetupTutorialUi();
        activeTutorialTrigger = trigger;
        // isTutorialSideMode / tutorialSideOnLeft may have been pre-set by the caller; only
        // auto-assign when the caller left them at their default (right-side = drivers panel open).
        if (!tutorialSideOnLeft && trigger != TutorialTrigger.AssignForestProductionWorker)
            isTutorialSideMode = isDriversPanelOpen;
        tutorialHud.BodyText.fontSize = 15;   // default; callers may override after this returns
        ApplyTutorialWindowLayout();
        tutorialHud.TitleText.text = L(title);
        tutorialHud.StepText.text = $"{stepNumber}/{TutorialStepCount}";
        tutorialHud.BodyText.text = L(body);
        if (trigger == TutorialTrigger.ForestIntroduction)
        {
            tutorialHud.BodyText.text = L("See that Forest? It is not scenery. It is inventory with leaves.\n\nA Forest produces logs. Later, a Sawmill turns those logs into boards. That is the first honest-looking lie your economy will tell.\n\nNext, assign your worker to the Forest in the Productions tab.");
        }
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
            float xBase = activeTutorialTrigger switch
            {
                TutorialTrigger.SelectProductionWorker => -640f,
                TutorialTrigger.AssignForestProductionWorker => 640f,
                TutorialTrigger.FleetSelectTruck => -640f,
                TutorialTrigger.FleetAssignDriver => -640f,
                TutorialTrigger.FleetPickDriver => 640f,
                _ => tutorialSideOnLeft ? TutorialSidePanelLeftX : TutorialSidePanelBaseX
            };
            // Left-offset mode needs full-size window to fit longer text; right side uses compact card
            bool isNarrowTutorial = activeTutorialTrigger is TutorialTrigger.SelectProductionWorker
                or TutorialTrigger.AssignForestProductionWorker
                or TutorialTrigger.FleetSelectTruck
                or TutorialTrigger.FleetAssignDriver
                or TutorialTrigger.FleetPickDriver;
            bool isTallNarrowTutorial = activeTutorialTrigger is TutorialTrigger.SelectProductionWorker
                or TutorialTrigger.FleetSelectTruck
                or TutorialTrigger.FleetAssignDriver
                or TutorialTrigger.FleetPickDriver;
            float w          = isNarrowTutorial ? 320f : tutorialSideOnLeft ? 480f : 360f;
            float h          = isTallNarrowTutorial ? 350f : activeTutorialTrigger == TutorialTrigger.AssignForestProductionWorker ? 270f : tutorialSideOnLeft ? 380f : 290f;
            float bodyH      = isTallNarrowTutorial ? 205f : activeTutorialTrigger == TutorialTrigger.AssignForestProductionWorker ? 128f : tutorialSideOnLeft ? 200f : 148f;
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
        tutorialHud.FleetMenuOutlineRoot   = CreateTutorialMenuButtonOutline("TutorialFleetMenuOutline",   canvasObject.transform, 17f);
        tutorialHud.HireWorkerOutlineRoot  = CreateTutorialHireButtonOutline("TutorialHireWorkerOutline",  canvasObject.transform);
        tutorialHud.FirstWorkerOutlineRoot = CreateTutorialDynamicOutline("TutorialFirstWorkerOutline", canvasObject.transform);
        tutorialHud.ForestAssignOutlineRoot = CreateTutorialDynamicOutline("TutorialForestAssignOutline", canvasObject.transform);
        tutorialHud.FleetTruckOutlineRoot = CreateTutorialDynamicOutline("TutorialFleetTruckOutline", canvasObject.transform);
        tutorialHud.FleetAssignDriverOutlineRoot = CreateTutorialDynamicOutline("TutorialFleetAssignDriverOutline", canvasObject.transform);
        tutorialHud.FleetDriverPickerOutlineRoot = CreateTutorialDynamicOutline("TutorialFleetDriverPickerOutline", canvasObject.transform);

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
        cameraTargetOffset = TutorialForestZoomOffset;
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
            isTutorialCameraFocusActive = false;
            isBuildHighlightPersistent = false;
            isWorkersHighlightPersistent = false;
            isHireWorkerHighlightPersistent = false;
            isShiftsHighlightPersistent = false;
            HideTutorialOrbitHud();
            selectedLocation = null;
            RefreshSelectionVisuals();
            SessionDebugLogger.Log("TUTORIAL", "Tutorial skipped by player.");
        }

        isTutorialOpen     = false;
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        PlayUiSound(uiPanelCloseClip, 0.82f);

        if (activeTutorialTrigger == TutorialTrigger.ForestIntroduction)
        {
            isTutorialCameraFocusActive = false;
        }

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
            isLogisticsTabActive        = true;
            isShiftsPanelOpen           = true;
            isShiftsScreenDirty         = true;
            LogUiInput("Tutorial: auto-opened Shifts panel after tutorial 6 OK");
            PlayUiSound(uiPanelOpenClip, 0.9f);
            ScheduleTutorial(TutorialTrigger.SelectProductionWorker);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.SelectProductionWorker)
        {
            SelectFirstWorkerForProductionTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.AssignForestProductionWorker)
        {
            AssignSelectedWorkerToForestFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.ForestWorkerStarted)
        {
            selectedLocation = null;
            isTutorialCameraFocusActive = false;
            tutorialCinematicPhase = TutorialCinematicPhase.Returning;   // smooth camera back to default
            ScheduleTutorial(TutorialTrigger.NeedSawmill);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.NeedSawmill)
        {
            OpenBuildPanelFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.FleetSelectTruck)
        {
            SelectTruckForFleetTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.FleetAssignDriver)
        {
            OpenFleetDriverPickerForTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.FleetPickDriver)
        {
            PickFirstFleetDriverForTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.SawmillBuilt)
        {
            StartTutorialBusStopWorkerArrival();
        }
    }

    private void StartHireArrivalCinematic()
    {
        if (hiringDriverArrival == null) return;   // bus already gone — nothing to track
        tutorialCinematicDriver = hiringDriverArrival.Driver;
        tutorialCinematicPhase  = TutorialCinematicPhase.TrackingBus;
        tutorialCinematicShouldShowForestIntro = true;
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
                {
                    HideTutorialOrbitHud();
                    tutorialCinematicPhase = TutorialCinematicPhase.Returning;
                }
                break;
            }

            case TutorialCinematicPhase.TrackingWorkerBackCloseup:
            {
                if (tutorialCinematicDriver?.DriverObject != null && tutorialCinematicDriver.DriverObject.activeSelf)
                {
                    Vector3 workerPos = tutorialCinematicDriver.DriverObject.transform.position;
                    Vector3 forward = Vector3.ProjectOnPlane(tutorialCinematicDriver.DriverObject.transform.forward, Vector3.up);
                    if (forward.sqrMagnitude < 0.001f)
                    {
                        forward = Vector3.forward;
                    }

                    forward.Normalize();
                    Vector3 desiredPosition = workerPos - forward * 5.5f + Vector3.up * 3.0f;
                    Vector3 lookTarget = workerPos + Vector3.up * 1.05f + forward * 1.1f;
                    float positionLerp = 1f - Mathf.Exp(-1.8f * dt);
                    float rotationLerp = 1f - Mathf.Exp(-3.5f * dt);
                    mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, desiredPosition, positionLerp);
                    mainCamera.transform.rotation = Quaternion.Slerp(
                        mainCamera.transform.rotation,
                        Quaternion.LookRotation((lookTarget - mainCamera.transform.position).normalized, Vector3.up),
                        rotationLerp);
                    cameraFocusPoint = workerPos;
                    cameraOffset = mainCamera.transform.position - cameraFocusPoint;
                    cameraTargetOffset = cameraOffset;
                    UpdateTutorialOrbitHud(dt);
                }

                // Stay in closeup until the orbit HUD's OK button is pressed (or driver disappears)
                bool orbitHudVisible = tutorialOrbitHudRoot != null && tutorialOrbitHudRoot.activeSelf;
                if (tutorialCinematicDriver?.DriverObject == null || !orbitHudVisible)
                {
                    HideTutorialOrbitHud();
                    tutorialCinematicPhase = TutorialCinematicPhase.Returning;
                }

                return;
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
                    if (tutorialCinematicShouldShowForestIntro)
                    {
                        ScheduleTutorial(TutorialTrigger.ForestIntroduction);
                    }
                    tutorialCinematicShouldShowForestIntro = false;
                    SessionDebugLogger.Log("TUTORIAL", "Hire-arrival cinematic ended.");
                }
                break;
            }
        }

        mainCamera.transform.position = cameraFocusPoint + cameraOffset;
        mainCamera.transform.rotation = GetDioramaCameraRotation();
    }

    private void StartTutorialCameraFocus(LocationType locationType, bool placeLocationOnRight = false)
    {
        if (mainCamera == null || !locations.ContainsKey(locationType))
        {
            return;
        }

        tutorialCameraFocusTarget = GetLocationCenter(locationType);
        tutorialCameraFocusTarget.y = 0f;
        if (placeLocationOnRight)
        {
            Vector3 cameraRight = GetDioramaCameraRotation() * Vector3.right;
            cameraRight.y = 0f;
            if (cameraRight.sqrMagnitude > 0.001f)
            {
                tutorialCameraFocusTarget -= cameraRight.normalized * 3.2f;
            }
        }
        isTutorialCameraFocusActive = true;
        tutorialCameraWanderTime = 0f;
        isTruckCameraFocused = false;
        isCameraReturningToDiorama = false;
        isCameraRotatingToTarget = false;
        cameraTargetOffset = DioramaCameraOffset;
        SessionDebugLogger.Log("TUTORIAL", $"Started smooth tutorial camera focus on {locationType}.");
    }

    private void UpdateTutorialCameraFocus(float dt)
    {
        if (!isTutorialCameraFocusActive || mainCamera == null)
        {
            return;
        }

        // Wander mode: Tutorial 9 (ForestWorkerStarted) — camera drifts gently around Forest while open
        bool isWanderMode = isTutorialOpen && activeTutorialTrigger == TutorialTrigger.ForestWorkerStarted;
        if (isWanderMode)
            tutorialCameraWanderTime += dt;

        // Compute target focus point (with sinusoidal wander in wander mode)
        Vector3 focusTarget = tutorialCameraFocusTarget;
        if (isWanderMode)
        {
            float t = tutorialCameraWanderTime;
            focusTarget += new Vector3(Mathf.Sin(t * 0.22f) * 2.0f, 0f, Mathf.Cos(t * 0.17f) * 1.5f);
        }

        float focusLerp = 1f - Mathf.Exp(-TutorialCameraFocusSpeed * dt);
        float offsetLerp = 1f - Mathf.Exp(-(TutorialCameraFocusSpeed * 1.15f) * dt);
        cameraFocusPoint = Vector3.Lerp(cameraFocusPoint, focusTarget, focusLerp);
        cameraOffset = Vector3.Lerp(cameraOffset, TutorialForestZoomOffset, offsetLerp);
        cameraTargetOffset = cameraOffset;

        mainCamera.transform.position = cameraFocusPoint + cameraOffset;
        mainCamera.transform.rotation = GetDioramaCameraRotation();

        // In wander mode the focus never "completes" — it stays active until OK is pressed
        if (!isWanderMode)
        {
            bool focusDone = (cameraFocusPoint - focusTarget).sqrMagnitude < 0.05f;
            bool offsetDone = (cameraOffset - TutorialForestZoomOffset).sqrMagnitude < 0.01f;
            if (focusDone && offsetDone)
            {
                cameraFocusPoint = tutorialCameraFocusTarget;
                cameraOffset = TutorialForestZoomOffset;
                cameraTargetOffset = TutorialForestZoomOffset;
                mainCamera.transform.position = cameraFocusPoint + cameraOffset;
                mainCamera.transform.rotation = GetDioramaCameraRotation();
                isTutorialCameraFocusActive = false;
                SessionDebugLogger.Log("TUTORIAL", "Completed smooth tutorial camera focus.");
            }
        }
    }

    private bool IsBuildMenuTutorialHighlightActive()
    {
        return (isTutorialOpen && (activeTutorialTrigger == TutorialTrigger.BuildMotelPrompt
                                || activeTutorialTrigger == TutorialTrigger.NeedSawmill))
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

    private bool IsFleetTutorialHighlightActive()
    {
        return (isTutorialOpen && activeTutorialTrigger == TutorialTrigger.FleetIntroduction)
               || isFleetHighlightPersistent;
    }

    private bool IsShiftsTutorialHighlightActive()
    {
        return (isTutorialOpen && activeTutorialTrigger == TutorialTrigger.ForestIntroduction)
               || isShiftsHighlightPersistent;
    }

    private bool IsFirstWorkerTutorialHighlightActive()
    {
        return isTutorialOpen && activeTutorialTrigger == TutorialTrigger.SelectProductionWorker;
    }

    private bool IsForestAssignTutorialHighlightActive()
    {
        return isTutorialOpen && activeTutorialTrigger == TutorialTrigger.AssignForestProductionWorker;
    }

    private bool IsFleetTruckTutorialHighlightActive()
    {
        return isTutorialOpen && activeTutorialTrigger == TutorialTrigger.FleetSelectTruck;
    }

    private bool IsFleetAssignDriverTutorialHighlightActive()
    {
        return isTutorialOpen && activeTutorialTrigger == TutorialTrigger.FleetAssignDriver;
    }

    private bool IsFleetDriverPickerTutorialHighlightActive()
    {
        return isTutorialOpen && activeTutorialTrigger == TutorialTrigger.FleetPickDriver;
    }

    private void UpdateTutorialUi()
    {
        UpdateTutorialCameraFocus(Time.unscaledDeltaTime);

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
                         || IsShiftsTutorialHighlightActive()
                         || IsFleetTutorialHighlightActive()
                         || IsFirstWorkerTutorialHighlightActive()
                         || IsForestAssignTutorialHighlightActive()
                         || IsFleetTruckTutorialHighlightActive()
                         || IsFleetAssignDriverTutorialHighlightActive()
                         || IsFleetDriverPickerTutorialHighlightActive();
        bool canvasNeeded = isTutorialOpen || anyHighlight;
        if (tutorialHud.CanvasRoot.activeSelf != canvasNeeded)
            tutorialHud.CanvasRoot.SetActive(canvasNeeded);

        if (!isTutorialOpen && tutorialHud.OverlayImage != null)
            tutorialHud.OverlayImage.color = OverlayColorTransparent;

        // Bob animation for the floating side card
        if (isTutorialOpen && isTutorialSideMode && tutorialHud.WindowRect != null)
        {
            tutorialBobTime += Time.unscaledDeltaTime;
            float xBase = activeTutorialTrigger switch
            {
                TutorialTrigger.SelectProductionWorker => -640f,
                TutorialTrigger.AssignForestProductionWorker => 640f,
                TutorialTrigger.FleetSelectTruck => -640f,
                TutorialTrigger.FleetAssignDriver => -640f,
                TutorialTrigger.FleetPickDriver => 640f,
                _ => tutorialSideOnLeft ? TutorialSidePanelLeftX : TutorialSidePanelBaseX
            };
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

        if (tutorialHud.FleetMenuOutlineRoot != null)
            tutorialHud.FleetMenuOutlineRoot.SetActive(IsFleetTutorialHighlightActive());

        UpdateTutorialElementOutlines();
    }

    private void UpdateTutorialElementOutlines()
    {
        UpdateTutorialOutlineFromTarget(
            tutorialHud.FirstWorkerOutlineRoot,
            IsFirstWorkerTutorialHighlightActive() && shiftsScreenUi != null && shiftsScreenUi.DriverRows.Count > 0
                ? shiftsScreenUi.DriverRows[0].Root
                : null,
            5f);

        RectTransform forestAssignTarget = null;
        if (IsForestAssignTutorialHighlightActive() &&
            logisticsSlots.Length > 0 &&
            logisticsSlots[0]?.AssignButton != null)
        {
            forestAssignTarget = logisticsSlots[0].AssignButton.GetComponent<RectTransform>();
        }

        UpdateTutorialOutlineFromTarget(tutorialHud.ForestAssignOutlineRoot, forestAssignTarget, 5f);

        RectTransform fleetTruckTarget = null;
        if (IsFleetTruckTutorialHighlightActive() &&
            fleetScreenUi != null &&
            fleetScreenUi.TruckRows.Count > 0 &&
            fleetScreenUi.TruckRows[0]?.Root != null)
        {
            fleetTruckTarget = fleetScreenUi.TruckRows[0].Root;
        }

        UpdateTutorialOutlineFromTarget(tutorialHud.FleetTruckOutlineRoot, fleetTruckTarget, 5f);

        RectTransform fleetAssignTarget = null;
        if (IsFleetAssignDriverTutorialHighlightActive() &&
            fleetScreenUi != null &&
            fleetScreenUi.AssignDriverButtons.Count > 0 &&
            fleetScreenUi.AssignDriverButtons[0] != null)
        {
            fleetAssignTarget = fleetScreenUi.AssignDriverButtons[0].GetComponent<RectTransform>();
        }

        UpdateTutorialOutlineFromTarget(tutorialHud.FleetAssignDriverOutlineRoot, fleetAssignTarget, 5f);

        RectTransform fleetPickerTarget = null;
        if (IsFleetDriverPickerTutorialHighlightActive() &&
            fleetScreenUi != null &&
            fleetScreenUi.DriverPickerButtons.Count > 0 &&
            fleetScreenUi.DriverPickerButtons[0] != null &&
            fleetScreenUi.DriverPickerButtons[0].gameObject.activeInHierarchy)
        {
            fleetPickerTarget = fleetScreenUi.DriverPickerButtons[0].GetComponent<RectTransform>();
        }

        UpdateTutorialOutlineFromTarget(tutorialHud.FleetDriverPickerOutlineRoot, fleetPickerTarget, 5f);
    }

    private DriverAgent GetFirstAssignableProductionWorker()
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver != null &&
                driver.DutyMode == DriverDutyMode.Local &&
                driver.AssignedTruckNumber == 0 &&
                driver.ShiftStartHour < 0 &&
                !driver.IsArrivingByBus)
            {
                return driver;
            }
        }

        return null;
    }

    private void SelectFirstWorkerForProductionTutorial()
    {
        DriverAgent worker = GetFirstAssignableProductionWorker();
        if (worker == null)
        {
            return;
        }

        selectedShiftDriverId = worker.DriverId;
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.AssignForestProductionWorker);
        LogUiInput($"Tutorial: selected {worker.DriverName} for Forest production assignment.");
        PlayUiSound(uiSelectClip, 0.85f);
    }

    private void CompleteProductionWorkerSelectionTutorial(DriverAgent worker)
    {
        if (!isTutorialOpen || activeTutorialTrigger != TutorialTrigger.SelectProductionWorker || worker == null)
        {
            return;
        }

        selectedShiftDriverId = worker.DriverId;
        isTutorialOpen = false;
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.AssignForestProductionWorker);
        LogUiInput($"Tutorial: player selected {worker.DriverName} for production assignment.");
        PlayUiSound(uiPanelCloseClip, 0.82f);
    }

    private void AssignSelectedWorkerToForestFromTutorial()
    {
        DriverAgent selectedDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);
        if (selectedDriver == null)
        {
            selectedDriver = GetFirstAssignableProductionWorker();
            if (selectedDriver != null)
            {
                selectedShiftDriverId = selectedDriver.DriverId;
            }
        }

        if (selectedDriver != null && logisticsSlots.Length > 0 && logisticsSlots[0] != null)
        {
            AssignWorkerToBuilding(selectedDriver, logisticsSlots[0]);
            LogUiInput($"Tutorial: assigned {selectedDriver.DriverName} to Forest production.");
            PlayUiSound(uiSelectClip, 0.85f);
        }

        isShiftsPanelOpen = false;
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void CompleteForestAssignmentTutorial()
    {
        if (!isTutorialOpen || activeTutorialTrigger != TutorialTrigger.AssignForestProductionWorker)
        {
            return;
        }

        isTutorialOpen = false;
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        isShiftsPanelOpen = false;
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
        LogUiInput("Tutorial: Forest production assignment complete; closed Shifts panel.");
        PlayUiSound(uiPanelCloseClip, 0.82f);
    }

    private void NotifyTutorialProductionWorkerEntered(LocationType locationType)
    {
        if (locationType != LocationType.Forest || hasShownForestWorkerStartedTutorial)
        {
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
        if (!locations.ContainsKey(LocationType.BusStop) || !locations.ContainsKey(LocationType.Motel))
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
                "Two new workers have appeared.\n\nTime to put someone in a truck — open Fleet and assign a driver to a route.",
                $"12/{TutorialStepCount}",
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
                    LogUiInput("Tutorial: orbit HUD OK — opened Fleet panel (step 12).");
                    PlayUiSound(uiPanelOpenClip, 0.9f);
                });
            SessionDebugLogger.Log("TUTORIAL", "Spawned two tutorial workers at Bus Stop and started worker-follow camera.");
        }

        isDriversScreenDirty = true;
        isFleetScreenDirty = true;
    }

    private Vector3 GetTutorialBusStopWorkerSpawnPosition(int index)
    {
        LocationData busStop = locations[LocationType.BusStop];
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

    private void ShowTutorialOrbitHud(DriverAgent worker, string message, string stepLabel = "", System.Action onOk = null)
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

            // Step counter — top-right corner (e.g. "12/12")
            tutorialOrbitHudStepText = CreateBodyText("OrbitStepText", panel, font, string.Empty, 13, TextAnchor.UpperRight, FleetAccentColor);
            RectTransform stepRect = tutorialOrbitHudStepText.GetComponent<RectTransform>();
            stepRect.anchorMin = new Vector2(1f, 1f);
            stepRect.anchorMax = new Vector2(1f, 1f);
            stepRect.pivot     = new Vector2(1f, 1f);
            stepRect.anchoredPosition = new Vector2(-10f, -8f);
            stepRect.sizeDelta = new Vector2(72f, 22f);
            tutorialOrbitHudStepText.raycastTarget = false;

            // Main body text — font 13, wrapping enabled, leaves room for step counter top + OK button bottom
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
                tutorialOrbitHudOnOk = null;
                HideTutorialOrbitHud();
                okAction?.Invoke();
                tutorialCinematicPhase = TutorialCinematicPhase.Returning;
            });
        }

        tutorialOrbitHudOnOk = onOk;
        tutorialOrbitHudDriver = worker;
        tutorialOrbitHudFullText = L(message);
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
        if (tutorialOrbitHudRoot == null || tutorialOrbitHudText == null || tutorialOrbitHudDriver?.DriverObject == null || mainCamera == null)
        {
            return;
        }

        tutorialOrbitHudTypeTime += dt;
        tutorialOrbitHudOrbitTime += dt;
        int visibleChars = Mathf.Clamp(Mathf.FloorToInt(tutorialOrbitHudTypeTime * 8f), 0, tutorialOrbitHudFullText.Length);
        tutorialOrbitHudText.text = tutorialOrbitHudFullText.Substring(0, visibleChars);

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
                // Convert screen point → canvas-local position (no UI camera needed for SSO)
                RectTransform canvasRect = tutorialOrbitHudRoot.GetComponent<RectTransform>();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, new Vector2(screenPos.x, screenPos.y), null, out Vector2 localPoint);
                tutorialOrbitHudPanel.anchoredPosition = localPoint;
            }
        }
    }

    private void HideTutorialOrbitHud()
    {
        if (tutorialOrbitHudRoot != null)
        {
            tutorialOrbitHudRoot.SetActive(false);
        }

        tutorialOrbitHudDriver = null;
        tutorialOrbitHudFullText = string.Empty;
        tutorialOrbitHudTypeTime = 0f;
        tutorialOrbitHudOrbitTime = 0f;
    }

    private void SelectTruckForFleetTutorial()
    {
        TruckAgent truck = GetTruckAgent(1);
        if (truck == null)
        {
            return;
        }

        FocusTruck(truck.TruckNumber);
        isFleetPanelOpen = true;
        isFleetScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.FleetAssignDriver);
        LogUiInput("Tutorial: selected Truck 1 for Fleet assignment step.");
    }

    private void CompleteFleetTruckSelectionTutorial(int truckNumber)
    {
        if (!isTutorialOpen || activeTutorialTrigger != TutorialTrigger.FleetSelectTruck || truckNumber != 1)
        {
            return;
        }

        isTutorialOpen = false;
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        FocusTruck(truckNumber);
        isFleetPanelOpen = true;
        isFleetScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.FleetAssignDriver);
        LogUiInput("Tutorial: player selected Truck 1 in Fleet.");
        PlayUiSound(uiPanelCloseClip, 0.82f);
    }

    private void OpenFleetDriverPickerForTutorial()
    {
        TruckAgent truck = GetTruckAgent(1);
        if (truck == null)
        {
            return;
        }

        FocusTruck(truck.TruckNumber);
        isFleetPanelOpen = true;
        fleetAssignDriverTargetSlot = 0;
        if (fleetScreenUi?.AssignDriverPickerPanel != null)
        {
            fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(true);
            if (fleetScreenUi.AssignDriverPickerLayout != null) fleetScreenUi.AssignDriverPickerLayout.preferredHeight = 128f;
            if (fleetScreenUi.InfoCardLayout != null) fleetScreenUi.InfoCardLayout.preferredHeight = 360f;
            UpdateFleetDriverAssignmentPicker(truck);
        }

        isFleetScreenDirty = true;
        ScheduleTutorial(TutorialTrigger.FleetPickDriver);
        LogUiInput("Tutorial: opened Fleet driver picker for Truck 1.");
    }

    private void CompleteFleetAssignDriverTutorial()
    {
        if (!isTutorialOpen || activeTutorialTrigger != TutorialTrigger.FleetAssignDriver)
        {
            return;
        }

        isTutorialOpen = false;
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        OpenFleetDriverPickerForTutorial();
        PlayUiSound(uiPanelCloseClip, 0.82f);
    }

    private void PickFirstFleetDriverForTutorial()
    {
        TruckAgent truck = GetTruckAgent(1);
        if (truck == null)
        {
            return;
        }

        List<DriverAgent> candidates = GetDriverAssignmentCandidates(truck);
        if (candidates.Count == 0)
        {
            return;
        }

        AssignDriverToTruck(truck, candidates[0]);
        if (fleetScreenUi?.AssignDriverPickerPanel != null)
        {
            fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(false);
            if (fleetScreenUi.AssignDriverPickerLayout != null) fleetScreenUi.AssignDriverPickerLayout.preferredHeight = 0f;
            if (fleetScreenUi.InfoCardLayout != null) fleetScreenUi.InfoCardLayout.preferredHeight = 232f;
        }

        fleetAssignDriverTargetSlot = -1;
        isFleetScreenDirty = true;
        LogUiInput($"Tutorial: assigned {candidates[0].DriverName} to Truck 1.");
    }

    private void CompleteFleetPickDriverTutorial()
    {
        if (!isTutorialOpen || activeTutorialTrigger != TutorialTrigger.FleetPickDriver)
        {
            return;
        }

        isTutorialOpen = false;
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        LogUiInput("Tutorial: Fleet driver pick complete.");
        PlayUiSound(uiPanelCloseClip, 0.82f);
    }

    private static GameObject CreateTutorialDynamicOutline(string name, Transform parent)
    {
        GameObject root = CreateUiObject(name, parent);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = Vector2.zero;
        CreateTutorialOutlineBar("Top", root.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(0f, 3f), new Vector2(0f, 3f));
        CreateTutorialOutlineBar("Bottom", root.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, -3f), new Vector2(0f, 3f));
        CreateTutorialOutlineBar("Left", root.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(-3f, 0f), new Vector2(3f, 0f));
        CreateTutorialOutlineBar("Right", root.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0f), new Vector2(3f, 0f), new Vector2(3f, 0f));
        root.SetActive(false);
        return root;
    }

    private void UpdateTutorialOutlineFromTarget(GameObject outlineRoot, RectTransform target, float padding)
    {
        if (outlineRoot == null)
        {
            return;
        }

        if (target == null || tutorialHud?.CanvasRoot == null)
        {
            outlineRoot.SetActive(false);
            return;
        }

        Canvas tutorialCanvas = tutorialHud.CanvasRoot.GetComponent<Canvas>();
        RectTransform canvasRect = tutorialCanvas.GetComponent<RectTransform>();
        Camera uiCamera = tutorialCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : tutorialCanvas.worldCamera;
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);

        Vector2 min = new(float.MaxValue, float.MaxValue);
        Vector2 max = new(float.MinValue, float.MinValue);
        for (int i = 0; i < corners.Length; i++)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, uiCamera, out Vector2 local);
            min = Vector2.Min(min, local);
            max = Vector2.Max(max, local);
        }

        RectTransform outlineRect = outlineRoot.GetComponent<RectTransform>();
        outlineRect.anchoredPosition = (min + max) * 0.5f;
        outlineRect.sizeDelta = (max - min) + new Vector2(padding * 2f, padding * 2f);
        outlineRoot.SetActive(true);
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
