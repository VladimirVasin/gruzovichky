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
        AssignSawmillProductionWorker,
        SawmillWorkerAssigned,
        NeedSawmill,
        SawmillBuilt,
        FirstTradeOpened,
        UserWelcome,
        UserBuildRoadPrompt,
        UserCoreBuildingsPrompt,
        UserWarehouseBuiltInfo,
        UserMotelBuiltInfo,
        UserParkingBuiltInfo,
        UserBuildLumberjackCampPrompt,
        UserLumberjackCampBuiltInfo,
        UserLumberjackWorkerAssignedInfo,
        UserBuyTruckPrompt,
        UserTruckPurchasedArrivalInfo,
        BeeEasterEgg
    }

    private sealed class TutorialHudRefs
    {
        public GameObject     CanvasRoot;
        public GameObject     OverlayRoot;          // dark fullscreen bg parent
        public Image          OverlayImage;         // tinted in center mode, transparent in side mode
        public RectTransform  WindowRect;           // the floating card, repositioned per mode
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
    private Vector3 tutorialCameraFocusOffset = TutorialForestZoomOffset;
    private TruckAgent tutorialCameraFollowTruck;
    private float tutorialCameraWanderTime;
    private string tutorialWindowFullText = string.Empty;
    private float tutorialWindowTypeTime;
    private GameObject tutorialOrbitHudRoot;
    private RectTransform tutorialOrbitHudPanel;
    private Text tutorialOrbitHudText;
    private Text tutorialOrbitHudStepText;
    private Button tutorialOrbitHudOkButton;
    private System.Action tutorialOrbitHudOnOk;
    private DriverAgent tutorialOrbitHudDriver;
    private string tutorialOrbitHudSpeakerPrefix = string.Empty;
    private string tutorialOrbitHudBodyText = string.Empty;
    private float tutorialOrbitHudTypeTime;
    private const float TutorialOrbitHudDefaultTypeSpeed = 40f;
    private float tutorialOrbitHudTypeSpeed = TutorialOrbitHudDefaultTypeSpeed;
    private float tutorialOrbitHudOrbitTime;
    private bool tutorialOrbitHudDetached;
    private bool tutorialPendingForestWorkerStartedAfterOrbitOk;
    private DriverRescuePhase tutorialOrbitHudAttachedWalkPhase = DriverRescuePhase.None;
    private static readonly string[] TutorialOrbitHudDescriptorKeys =
    {
        "Hardworking",
        "Patient",
        "Persistent",
        "Diligent",
        "Reliable",
        "Attentive",
        "Careful",
        "Brave",
        "Practical",
        "Humble",
        "Energetic",
        "Polite",
        "Seasoned",
        "Thoughtful",
        "Honest",
        "Steady",
        "Observant",
        "Decent",
        "Quiet",
        "Hopeful"
    };

    private TutorialHudRefs tutorialHud;
    private TutorialTrigger activeTutorialTrigger;
    private const int TutorialStepCount = 17;
    private const int NewUserTutorialStepCount = 11;
    private static readonly Vector3 UserWelcomeCameraOffset = new(-13f, 14f, -13f);

    private static bool IsNewUserTutorialTrigger(TutorialTrigger trigger)
    {
        return trigger is TutorialTrigger.UserWelcome
            or TutorialTrigger.UserBuildRoadPrompt
            or TutorialTrigger.UserCoreBuildingsPrompt
            or TutorialTrigger.UserWarehouseBuiltInfo
            or TutorialTrigger.UserMotelBuiltInfo
            or TutorialTrigger.UserParkingBuiltInfo
            or TutorialTrigger.UserBuildLumberjackCampPrompt
            or TutorialTrigger.UserLumberjackCampBuiltInfo
            or TutorialTrigger.UserLumberjackWorkerAssignedInfo
            or TutorialTrigger.UserBuyTruckPrompt
            or TutorialTrigger.UserTruckPurchasedArrivalInfo;
    }

    private int GetNextUserCoreBuildingInfoTutorialStep()
    {
        int step = Mathf.Clamp(nextUserCoreBuildingInfoTutorialStep, 4, NewUserTutorialStepCount);
        nextUserCoreBuildingInfoTutorialStep = Mathf.Clamp(nextUserCoreBuildingInfoTutorialStep + 1, 4, NewUserTutorialStepCount);
        return step;
    }

    private bool IsTutorialEnabledForCurrentMode()
    {
        return selectedGameStartMode == GameStartMode.User;
    }

    private bool ShouldPauseSimulationForTutorial()
    {
        return activeTutorialTrigger != TutorialTrigger.ForestWorkerStarted &&
            activeTutorialTrigger != TutorialTrigger.UserTruckPurchasedArrivalInfo;
    }

    private void ScheduleTutorial(TutorialTrigger trigger, float delay = 0.12f)
    {
        pendingTutorialTrigger = trigger;
        pendingTutorialDelay   = Mathf.Max(0f, delay);
    }

    private void ResetTutorialFlowForNewGame()
    {
        isTutorialSkipped = false;
        isTutorialOpen = false;
        hasShownWelcomeTutorial = false;
        hasShownFirstMotelTutorial = false;
        hasShownWorkersPanelTutorial = false;
        hasShownFirstDriverHiredTutorial = false;
        hasShownForestIntroTutorial = false;
        hasShownForestWorkerStartedTutorial = false;
        hasShownNeedSawmillTutorial = false;
        hasShownFleetIntroTutorial = false;
        hasShownUserCoreBuildingsTutorial = false;
        hasShownUserWarehouseBuiltTutorial = false;
        hasShownUserMotelBuiltTutorial = false;
        hasShownUserParkingBuiltTutorial = false;
        nextUserCoreBuildingInfoTutorialStep = 4;
        hasShownUserBuildLumberjackCampTutorial = false;
        hasShownUserLumberjackCampBuiltTutorial = false;
        hasShownUserLumberjackWorkerAssignedTutorial = false;
        hasShownUserBuyTruckTutorial = false;
        hasShownUserTruckArrivalTutorial = false;
        hasShownFleetSelectTruckTutorial = false;
        hasShownFleetAssignDriverTutorial = false;
        hasShownFleetPickDriverTutorial = false;
        hasShownAssignSawmillWorkerTutorial = false;
        hasShownSawmillWorkerAssignedTutorial = false;
        hasShownSawmillBuiltTutorial = false;
        pendingTutorialTrigger = null;
        pendingTutorialDelay = 0f;
        isTutorialCameraFocusActive = false;
        tutorialCameraFollowTruck = null;
        HideTutorialOrbitHud();
        if (tutorialHud?.CanvasRoot != null)
        {
            tutorialHud.CanvasRoot.SetActive(false);
        }
    }

    private void TryShowTutorial(TutorialTrigger trigger)
    {
        bool isBeeEasterEgg = trigger == TutorialTrigger.BeeEasterEgg;
        if ((!isBeeEasterEgg && !IsTutorialEnabledForCurrentMode()) || isTutorialOpen)
        {
            return;
        }

        if (selectedGameStartMode == GameStartMode.User &&
            !IsNewUserTutorialTrigger(trigger) &&
            !isBeeEasterEgg)
        {
            return;
        }

        switch (trigger)
        {
            case TutorialTrigger.GameStarted:
                return;
            case TutorialTrigger.UserWelcome:
                if (hasShownWelcomeTutorial)
                {
                    return;
                }

                hasShownWelcomeTutorial = true;
                FocusCameraOnUserStartStopForTutorial();
                ShowTutorialWindow(
                    TutorialTrigger.UserWelcome,
                    1,
                    "Welcome to Lo-Fi Delivery Co.",
                    "Welcome to User mode.\n\nYou start with an almost empty map, a highway connection, a bus stop, and a few workers.\n\nBefore building, learn the camera controls: zoom in, zoom out, move the map, and rotate the view.");
                break;
            case TutorialTrigger.UserBuildRoadPrompt:
                ShowTutorialWindow(
                    TutorialTrigger.UserBuildRoadPrompt,
                    2,
                    "Build the first road",
                    "Now build your first road.\n\nOpen the Build menu from the top HUD or press B. Choose a road tool, then left-click a cell to place the road.\n\nHold Shift and drag to build a longer road segment. Press R to rotate the road direction before placing.");
                break;
            case TutorialTrigger.UserCoreBuildingsPrompt:
                if (hasShownUserCoreBuildingsTutorial)
                {
                    return;
                }

                hasShownUserCoreBuildingsTutorial = true;
                UnlockBuildTool(BuildTool.Warehouse);
                UnlockBuildTool(BuildTool.Motel);
                UnlockBuildTool(BuildTool.Parking);
                isBuildHighlightPersistent = true;
                ShowTutorialWindow(
                    TutorialTrigger.UserCoreBuildingsPrompt,
                    3,
                    "Build the town core",
                    "The road is only useful when it connects important places.\n\nThree core buildings are now unlocked: Warehouse, Motel, and Parking.\n\nBuild all three from the Build menu. You can open Build from the top HUD or press B.");
                break;
            case TutorialTrigger.UserWarehouseBuiltInfo:
                if (hasShownUserWarehouseBuiltTutorial)
                {
                    return;
                }

                hasShownUserWarehouseBuiltTutorial = true;
                ShowTutorialWindow(
                    TutorialTrigger.UserWarehouseBuiltInfo,
                    GetNextUserCoreBuildingInfoTutorialStep(),
                    "Warehouse",
                    "Warehouse is your central storage.\n\nFinished resources are collected here, and future routes will use it as the main place for loading and unloading goods.");
                break;
            case TutorialTrigger.UserMotelBuiltInfo:
                if (hasShownUserMotelBuiltTutorial)
                {
                    return;
                }

                hasShownUserMotelBuiltTutorial = true;
                ShowTutorialWindow(
                    TutorialTrigger.UserMotelBuiltInfo,
                    GetNextUserCoreBuildingInfoTutorialStep(),
                    "Motel",
                    "Motel gives workers a place to check in, rest, and return to when they have no active task.\n\nAfter this building exists, workers and cats can move from the starting stop into their normal idle area.");
                break;
            case TutorialTrigger.UserParkingBuiltInfo:
                if (hasShownUserParkingBuiltTutorial)
                {
                    return;
                }

                hasShownUserParkingBuiltTutorial = true;
                ShowTutorialWindow(
                    TutorialTrigger.UserParkingBuiltInfo,
                    GetNextUserCoreBuildingInfoTutorialStep(),
                    "Parking",
                    "Parking is the base for vehicles.\n\nTrucks and local buses start from here, return here, and use it as the town transport yard.");
                break;
            case TutorialTrigger.UserBuildLumberjackCampPrompt:
                if (hasShownUserBuildLumberjackCampTutorial)
                {
                    return;
                }

                hasShownUserBuildLumberjackCampTutorial = true;
                UnlockBuildTool(BuildTool.Forest);
                isBuildHighlightPersistent = true;
                ShowTutorialWindow(
                    TutorialTrigger.UserBuildLumberjackCampPrompt,
                    7,
                    "Build a Lumberjack Camp",
                    "The town needs its first production building.\n\nLumberjack Camp gathers Logs from nearby trees. Build it close to a dense patch of forest so workers do not spend the whole day walking.");
                break;
            case TutorialTrigger.UserLumberjackCampBuiltInfo:
                if (hasShownUserLumberjackCampBuiltTutorial)
                {
                    return;
                }

                hasShownUserLumberjackCampBuiltTutorial = true;
                isBuildHighlightPersistent = false;
                isShiftsHighlightPersistent = true;
                ShowTutorialWindow(
                    TutorialTrigger.UserLumberjackCampBuiltInfo,
                    8,
                    "Assign a Lumberjack",
                    "The Lumberjack Camp is built, but buildings do not work by themselves.\n\nOpen Vacancies, select the Lumberjack Camp vacancy, choose a worker, and assign them.");
                break;
            case TutorialTrigger.UserLumberjackWorkerAssignedInfo:
                if (hasShownUserLumberjackWorkerAssignedTutorial)
                {
                    return;
                }

                hasShownUserLumberjackWorkerAssignedTutorial = true;
                isShiftsHighlightPersistent = false;
                ShowTutorialWindow(
                    TutorialTrigger.UserLumberjackWorkerAssignedInfo,
                    9,
                    "Lumberjack Work",
                    "The assigned worker will go to the camp during production hours, walk to nearby trees, chop them into Logs, and carry those Logs back one by one.\n\nLater you will move those Logs through the logistics chain.");
                break;
            case TutorialTrigger.UserBuyTruckPrompt:
                if (hasShownUserBuyTruckTutorial)
                {
                    return;
                }

                hasShownUserBuyTruckTutorial = true;
                bool buyTruckRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserBuyTruckPrompt,
                    10,
                    buyTruckRu ? "Купи первый грузовик" : "Buy the first truck",
                    buyTruckRu
                        ? "Ресурсы не перемещаются сами. Грузовики перевозят груз между зданиями, а позже смогут возить товары во внешние торговые маршруты.\n\nОткрой Вакансии в верхнем HUD и купи первый грузовик. Он стоит $300."
                        : "Resources do not move by themselves. Trucks move cargo between buildings and, later, toward outside trade routes.\n\nOpen Vacancies from the top HUD and buy your first truck. It costs $300.");
                break;
            case TutorialTrigger.UserTruckPurchasedArrivalInfo:
                if (hasShownUserTruckArrivalTutorial)
                {
                    return;
                }

                hasShownUserTruckArrivalTutorial = true;
                bool truckArrivalRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserTruckPurchasedArrivalInfo,
                    11,
                    truckArrivalRu ? "Грузовик в пути" : "Truck en route",
                    truckArrivalRu
                        ? "Купленный грузовик въезжает с магистрали и направляется к Парковке.\n\nКогда он припаркуется, он станет частью автопарка и его можно будет использовать в логистике."
                        : "The purchased truck is entering from the highway and heading to your Parking.\n\nWhen it parks, it becomes part of your fleet and can be assigned to logistics work.");
                break;
            case TutorialTrigger.BuildMotelPrompt:
                isBuildHighlightPersistent = true;
                ShowTutorialWindow(
                    TutorialTrigger.BuildMotelPrompt,
                    2,
                    "Build a Motel",
                    "The Motel unlocks worker hiring and gives workers a place to rest.\n\nOpen Building at the top, or press B. Choose Motel and place it near your road plan.\n\nIn Build mode, press R to rotate the building before placing it.");
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
                    "The Motel is ready, so you can hire workers.\n\nOpen the Workers panel at the top of the screen. This is where new workers are hired and tracked.");
                break;
            case TutorialTrigger.WorkersPanelOpened:
                if (hasShownWorkersPanelTutorial) return;
                hasShownWorkersPanelTutorial = true;
                isHireWorkerHighlightPersistent = true;
                ShowTutorialWindow(
                    TutorialTrigger.WorkersPanelOpened,
                    4,
                    "Hire a Worker",
                    "Use the Hire New Worker button at the bottom of the Workers panel.\n\nHiring costs money. New workers arrive by bus before they become available.");
                break;
            case TutorialTrigger.FirstDriverHired:
                if (hasShownFirstDriverHiredTutorial) return;
                hasShownFirstDriverHiredTutorial = true;
                ShowTutorialWindow(
                    TutorialTrigger.FirstDriverHired,
                    5,
                    "The Worker is on Their Way!",
                    "Your new worker is arriving by bus.\n\nWait for the bus to stop and for the worker to walk to the Motel. After that, the worker can be assigned to jobs.");
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
                    "Forest Production",
                    "Forest produces Logs.\n\nTo start production, assign a worker to Forest in Shifts > Productions. Production workers operate from 08:00 to 18:00.");
                break;
            case TutorialTrigger.SelectProductionWorker:
                if (driverAgents.Count == 0) return;
                isTutorialSideMode = true;
                tutorialSideOnLeft = true;
                ShowTutorialWindow(
                    TutorialTrigger.SelectProductionWorker,
                    7,
                    "Select a Worker",
                    "Select a free worker from the list on the left.\n\nYou can also press OK and the first available worker will be selected automatically.");
                break;
            case TutorialTrigger.AssignForestProductionWorker:
                if (GetFirstAssignableProductionWorker() == null) return;
                isTutorialSideMode = true;
                tutorialSideOnLeft = false;
                ShowTutorialWindow(
                    TutorialTrigger.AssignForestProductionWorker,
                    8,
                    "Assign to Forest",
                    "Press Assign on the Forest row to send the selected worker there.\n\nYou can also press OK and the tutorial will assign the worker for you.");
                break;
            case TutorialTrigger.ForestWorkerStarted:
                if (hasShownForestWorkerStartedTutorial) return;
                hasShownForestWorkerStartedTutorial = true;
                if (locations.ContainsKey(LocationType.Forest))
                {
                    StartTutorialCameraFocus(LocationType.Forest, placeLocationOnRight: true);
                }
                selectedLocation = null;
                selectedLocalStopIndex = -1;
                selectedPersonalHouseIndex = -1;
                ClearSelectedDebugCell();
                RefreshSelectionVisuals();
                isTutorialSideMode = true;
                tutorialSideOnLeft = true;
                ShowTutorialWindow(
                    TutorialTrigger.ForestWorkerStarted,
                    9,
                    "Forest Is Working",
                    "The worker is now producing Logs at Forest.\n\nLogs are raw material. They must be moved and processed before they become useful for the town.");
                tutorialHud.BodyText.fontSize = 13;   // compact font so text fits neatly
                break;
            case TutorialTrigger.FleetIntroduction:
                if (hasShownFleetIntroTutorial) return;
                hasShownFleetIntroTutorial = true;
                isFleetHighlightPersistent = true;
                ShowTutorialWindow(
                    TutorialTrigger.FleetIntroduction,
                    10,
                    "Use Trucks",
                    "Resources do not move automatically.\n\nOpen Fleet, assign a driver to a truck, then choose a route to move cargo between buildings.");
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
                    "Select Truck #1 in the Fleet list.\n\nYou can also press OK and the tutorial will select the truck automatically.");
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
                    "Assign a Driver",
                    "Truck #1 needs a driver before it can run routes.\n\nPress Assign in Driver Slot 1. Only free workers can be assigned to trucks.");
                tutorialHud.BodyText.fontSize = 13;
                break;
            case TutorialTrigger.FleetPickDriver:
                if (hasShownFleetPickDriverTutorial) return;
                hasShownFleetPickDriverTutorial = true;
                isTutorialSideMode = true;
                tutorialSideOnLeft = true;
                ShowTutorialWindow(
                    TutorialTrigger.FleetPickDriver,
                    15,
                    "Choose a Driver",
                    "Choose any free worker from the driver list.\n\nWorkers already assigned to production are not shown here. The tutorial will continue after you assign a driver.");
                tutorialHud.BodyText.fontSize = 13;
                break;
            case TutorialTrigger.AssignSawmillProductionWorker:
                if (hasShownAssignSawmillWorkerTutorial) return;
                hasShownAssignSawmillWorkerTutorial = true;
                isTutorialSideMode = false;
                tutorialSideOnLeft = false;
                ShowTutorialWindow(
                    TutorialTrigger.AssignSawmillProductionWorker,
                    16,
                    "Staff the Sawmill",
                    "You have assigned a worker to Forest. Now assign a worker to Sawmill.\n\nOpen Shifts, go to Productions, and assign a free worker to the Sawmill row.");
                break;
            case TutorialTrigger.SawmillWorkerAssigned:
                if (hasShownSawmillWorkerAssignedTutorial) return;
                hasShownSawmillWorkerAssignedTutorial = true;
                isTutorialSideMode = false;
                tutorialSideOnLeft = false;
                ShowTutorialWindow(
                    TutorialTrigger.SawmillWorkerAssigned,
                    17,
                    "Sawmill Ready",
                    "The Sawmill now has a worker.\n\nNext, use Fleet routes to deliver Logs from Forest to Sawmill, then move Boards onward to Warehouse.");
                break;
            case TutorialTrigger.NeedSawmill:
                if (hasShownNeedSawmillTutorial) return;
                hasShownNeedSawmillTutorial = true;
                UnlockBuildTool(BuildTool.Sawmill);
                isBuildHighlightPersistent = true;
                isTutorialSideMode = false;
                tutorialSideOnLeft = false;
                ShowTutorialWindow(
                    TutorialTrigger.NeedSawmill,
                    10,
                    "Build a Sawmill",
                    "Logs must be processed into Boards.\n\nOpen Building, choose Sawmill, and place it with its entrance connected to a road.");
                break;
            case TutorialTrigger.SawmillBuilt:
                if (hasShownSawmillBuiltTutorial) return;
                hasShownSawmillBuiltTutorial = true;
                isBuildHighlightPersistent = false;
                ShowTutorialWindow(
                    TutorialTrigger.SawmillBuilt,
                    11,
                    "Sawmill Placed",
                    "Sawmill converts Logs into Boards.\n\nResources still need transport: use trucks to deliver Logs from Forest to Sawmill and move finished Boards onward.");
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
        bool isEasterEgg = trigger == TutorialTrigger.BeeEasterEgg;
        bool isNewUserTutorial = IsNewUserTutorialTrigger(trigger);
        int stepCount = isNewUserTutorial ? NewUserTutorialStepCount : TutorialStepCount;
        tutorialHud.StepText.text = isEasterEgg ? string.Empty : $"{stepNumber}/{stepCount}";
        tutorialWindowFullText = L(body);
        tutorialWindowTypeTime = 0f;
        tutorialHud.BodyText.text = string.Empty;
        tutorialHud.SkipToggle.isOn = false;
        tutorialHud.SkipToggle.gameObject.SetActive(!isEasterEgg);
        isTutorialOpen = true;
        tutorialHud.CanvasRoot.SetActive(true);
        PlayUiSound(uiPanelOpenClip, 0.82f);
        SessionDebugLogger.Log("TUTORIAL", $"Shown tutorial window: {title} (side={isTutorialSideMode}).");
    }

    private void ShowBeeEasterEggHud()
    {
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        ShowTutorialWindow(
            TutorialTrigger.BeeEasterEgg,
            0,
            "Bees",
            "Р”СѓСЂР°С‡РѕРє, РЅРµ РјРµС€Р°Р№ РїС‡С‘Р»РєР°Рј");
        if (tutorialHud?.WindowRect != null)
        {
            tutorialHud.WindowRect.sizeDelta = new Vector2(500f, 260f);
        }
        if (tutorialHud?.BodyPanelLayout != null)
        {
            tutorialHud.BodyPanelLayout.preferredHeight = 100f;
        }
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
        // Close all panels then open Build, same effect as clicking the Building button.
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
                TutorialTrigger.FleetPickDriver => -500f,
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
        tutorialHud.WorkersMenuOutlineRoot = CreateTutorialMenuButtonOutline("TutorialWorkersMenuOutline", canvasObject.transform, 17f);
        tutorialHud.ShiftsMenuOutlineRoot  = CreateTutorialMenuButtonOutline("TutorialShiftsMenuOutline",  canvasObject.transform, 112f);
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

    private void FocusCameraOnUserStartStopForTutorial()
    {
        if (mainCamera == null)
        {
            return;
        }

        isTruckCameraFocused = false;
        isCameraReturningToDiorama = false;
        isCameraRotatingToTarget = false;

        Vector3 focus = new Vector3(GridWidth * 0.5f, 0f, GridHeight * 0.5f);
        if (locations.TryGetValue(LocationType.IntercityStop, out LocationData stop))
        {
            focus = GetLocationCenter(stop);
            focus += new Vector3(-1.6f, 0f, -1.0f);
        }

        focus.y = 0f;
        tutorialCameraFocusTarget = focus;
        tutorialCameraFocusOffset = UserWelcomeCameraOffset;
        tutorialCameraWanderTime = 0f;
        isTutorialCameraFocusActive = true;
        cameraTargetOffset = UserWelcomeCameraOffset;
        SessionDebugLogger.Log("TUTORIAL", $"Started smooth User welcome camera focus on start stop at ({focus.x:F1},{focus.z:F1}).");
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
            ResetTutorialGoalsForNewGame();
            isBuildHighlightPersistent = false;
            isWorkersHighlightPersistent = false;
            isHireWorkerHighlightPersistent = false;
            isShiftsHighlightPersistent = false;
            HideTutorialOrbitHud();
            selectedLocation = null;
            selectedLocalStopIndex = -1;
            selectedPersonalHouseIndex = -1;
            RefreshSelectionVisuals();
            SessionDebugLogger.Log("TUTORIAL", "Tutorial skipped by player.");
        }

        isTutorialOpen     = false;
        isTutorialSideMode = false;
        tutorialSideOnLeft = false;
        tutorialWindowFullText = string.Empty;
        tutorialWindowTypeTime = 0f;
        if (tutorialHud.SkipToggle != null)
        {
            tutorialHud.SkipToggle.gameObject.SetActive(true);
        }
        PlayUiSound(uiPanelCloseClip, 0.82f);

        if (activeTutorialTrigger == TutorialTrigger.ForestIntroduction)
        {
            isTutorialCameraFocusActive = false;
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.GameStarted)
        {
            ScheduleTutorial(TutorialTrigger.BuildMotelPrompt);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserWelcome)
        {
            BeginCameraControlTutorialGoals();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserBuildRoadPrompt)
        {
            BeginRoadBuildTutorialGoals();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserCoreBuildingsPrompt)
        {
            BeginCoreBuildingsTutorialGoals();
            OpenBuildPanelFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserBuildLumberjackCampPrompt)
        {
            BeginLumberjackCampTutorialGoals();
            OpenBuildPanelFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserBuyTruckPrompt)
        {
            BeginBuyTruckTutorialGoals();
            isFleetPanelOpen = false;
            isFleetHighlightPersistent = false;
            isLogisticsTabActive = false;
            isShiftsPanelOpen = true;
            isShiftsScreenDirty = true;
            LogUiInput("Tutorial: opened Vacancies for first truck purchase.");
            PlayUiSound(uiPanelOpenClip, 0.9f);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserLumberjackCampBuiltInfo)
        {
            isBuildPanelOpen = false;
            isShiftsHighlightPersistent = false;
            isLogisticsTabActive = true;
            isShiftsPanelOpen = true;
            isShiftsScreenDirty = true;
            LogUiInput("Tutorial: opened Vacancies for Lumberjack Camp assignment.");
            PlayUiSound(uiPanelOpenClip, 0.9f);
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
            selectedLocalStopIndex      = -1;
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
            selectedLocalStopIndex = -1;
            selectedPersonalHouseIndex = -1;
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
            LogUiInput("Tutorial: Fleet driver picker hint closed; waiting for player driver choice.");
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.AssignSawmillProductionWorker)
        {
            isLogisticsTabActive = true;
            isShiftsPanelOpen = true;
            isShiftsScreenDirty = true;
            LogUiInput("Tutorial: opened Shifts/Productions for Sawmill worker assignment.");
            PlayUiSound(uiPanelOpenClip, 0.9f);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.SawmillBuilt)
        {
            StartTutorialBusStopWorkerArrival();
        }

        if (activeTutorialTrigger == TutorialTrigger.UserTruckPurchasedArrivalInfo)
        {
            tutorialCameraFollowTruck = null;
            isTutorialCameraFocusActive = false;
            isCameraReturningToDiorama = true;
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserLumberjackWorkerAssignedInfo)
        {
            ScheduleTutorial(TutorialTrigger.UserBuyTruckPrompt, 0.8f);
        }
    }

    private void NotifyTutorialLumberjackCampBuilt()
    {
        if (selectedGameStartMode != GameStartMode.User || isTutorialSkipped)
        {
            return;
        }

        MarkTutorialGoalComplete(TutorialGoalKind.BuildLumberjackCamp);
        TryShowTutorial(TutorialTrigger.UserLumberjackCampBuiltInfo);
        SessionDebugLogger.Log("TUTORIAL", "Lumberjack Camp build tutorial notified.");
    }

    private void NotifyTutorialLumberjackWorkerAssigned()
    {
        if (selectedGameStartMode != GameStartMode.User || isTutorialSkipped)
        {
            return;
        }

        MarkTutorialGoalComplete(TutorialGoalKind.AssignLumberjackWorker);
        if (!hasShownUserLumberjackWorkerAssignedTutorial)
        {
            isShiftsPanelOpen = false;
            isShiftsScreenDirty = true;
            TryShowTutorial(TutorialTrigger.UserLumberjackWorkerAssignedInfo);
        }

        SessionDebugLogger.Log("TUTORIAL", "Lumberjack worker assignment tutorial notified.");
    }

    private void NotifyTutorialTruckPurchased(TruckAgent truckAgent)
    {
        if (selectedGameStartMode != GameStartMode.User || isTutorialSkipped)
        {
            return;
        }

        MarkTutorialGoalComplete(TutorialGoalKind.BuyFirstTruck);
        if (!hasShownUserTruckArrivalTutorial && truckAgent != null)
        {
            isFleetPanelOpen = false;
            isFleetHighlightPersistent = false;
            isShiftsPanelOpen = false;
            isShiftsScreenDirty = true;
            FocusTruck(truckAgent.TruckNumber);
            isTruckCameraFocused = false;
            isCameraReturningToDiorama = false;
            isCameraRotatingToTarget = false;
            tutorialCameraFollowTruck = truckAgent;
            tutorialCameraFocusTarget = truckAgent.TruckObject != null
                ? new Vector3(truckAgent.TruckObject.transform.position.x, 0f, truckAgent.TruckObject.transform.position.z)
                : Vector3.zero;
            tutorialCameraFocusOffset = new Vector3(-13f, 18f, -13f);
            isTutorialCameraFocusActive = true;
            ScheduleTutorial(TutorialTrigger.UserTruckPurchasedArrivalInfo, 0.25f);
            SessionDebugLogger.Log("TUTORIAL", $"First truck purchase tutorial focus started for {truckAgent.DisplayName}.");
        }
    }

    private void NotifyTutorialCoreBuildingBuilt(LocationType type)
    {
        if (selectedGameStartMode != GameStartMode.User || isTutorialSkipped)
        {
            return;
        }

        TutorialGoalKind goal;
        TutorialTrigger trigger;
        switch (type)
        {
            case LocationType.Warehouse:
                goal = TutorialGoalKind.BuildWarehouse;
                trigger = TutorialTrigger.UserWarehouseBuiltInfo;
                break;
            case LocationType.Motel:
                goal = TutorialGoalKind.BuildMotel;
                trigger = TutorialTrigger.UserMotelBuiltInfo;
                break;
            case LocationType.Parking:
                goal = TutorialGoalKind.BuildParking;
                trigger = TutorialTrigger.UserParkingBuiltInfo;
                break;
            default:
                return;
        }

        MarkTutorialGoalComplete(goal);
        TryShowTutorial(trigger);
        SessionDebugLogger.Log("TUTORIAL", $"Core building tutorial notified: {type}.");
    }

}
