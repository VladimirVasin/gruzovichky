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
        UserWorkerShiftInfo,
        UserLogisticsSetupInfo,
        UserBuyTruckPrompt,
        UserTruckPurchasedArrivalInfo,
        UserTruckAssignedFreightInfo,
        UserWorkersLeisureInfo,
        UserBuildServiceBuildingsPrompt,
        UserBarBuiltInfo,
        UserCanteenBuiltInfo,
        UserGasStationBuiltInfo,
        UserGamblingHallBuiltInfo,
        UserCityParkBuiltInfo,
        UserWorkersOverviewInfo,
        UserWorkerHiringBusInfo,
        UserWarehouseLoadersInfo,
        UserLocalTransportInfo,
        UserLocalBusRoutesInfo,
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
    private bool tutorialCameraFollowHiringBus;
    private float tutorialCameraWanderTime;
    private string tutorialWindowFullText = string.Empty;
    private float tutorialWindowTypeTime;
    private const float TutorialWindowTypeSpeed = 40f;

    private TutorialHudRefs tutorialHud;
    private TutorialTrigger activeTutorialTrigger;
    private const int TutorialStepCount = 17;
    private const int NewUserTutorialStepCount = 24;
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
            or TutorialTrigger.UserWorkerShiftInfo
            or TutorialTrigger.UserLogisticsSetupInfo
            or TutorialTrigger.UserBuyTruckPrompt
            or TutorialTrigger.UserTruckPurchasedArrivalInfo
            or TutorialTrigger.UserTruckAssignedFreightInfo
            or TutorialTrigger.UserWorkersLeisureInfo
            or TutorialTrigger.UserBuildServiceBuildingsPrompt
            or TutorialTrigger.UserBarBuiltInfo
            or TutorialTrigger.UserCanteenBuiltInfo
            or TutorialTrigger.UserGasStationBuiltInfo
            or TutorialTrigger.UserGamblingHallBuiltInfo
            or TutorialTrigger.UserCityParkBuiltInfo
            or TutorialTrigger.UserWorkersOverviewInfo
            or TutorialTrigger.UserWorkerHiringBusInfo
            or TutorialTrigger.UserWarehouseLoadersInfo
            or TutorialTrigger.UserLocalTransportInfo
            or TutorialTrigger.UserLocalBusRoutesInfo;
    }

    private int GetNextUserCoreBuildingInfoTutorialStep()
    {
        int step = Mathf.Clamp(nextUserCoreBuildingInfoTutorialStep, 4, NewUserTutorialStepCount);
        nextUserCoreBuildingInfoTutorialStep = Mathf.Clamp(nextUserCoreBuildingInfoTutorialStep + 1, 4, NewUserTutorialStepCount);
        return step;
    }

    private int GetNextUserServiceBuildingInfoTutorialStep()
    {
        int step = Mathf.Clamp(nextUserServiceBuildingInfoTutorialStep, 15, 19);
        nextUserServiceBuildingInfoTutorialStep = Mathf.Clamp(nextUserServiceBuildingInfoTutorialStep + 1, 15, 19);
        return step;
    }

    private bool IsTutorialEnabledForCurrentMode()
    {
        return selectedGameStartMode == GameStartMode.User;
    }

    private bool ShouldPauseSimulationForTutorial()
    {
        return !IsNonPausingTutorialTrigger(activeTutorialTrigger);
    }

    private static bool IsNonPausingTutorialTrigger(TutorialTrigger trigger)
    {
        return trigger is TutorialTrigger.ForestWorkerStarted
            or TutorialTrigger.UserTruckPurchasedArrivalInfo
            or TutorialTrigger.UserTruckAssignedFreightInfo
            or TutorialTrigger.UserWorkersLeisureInfo
            or TutorialTrigger.UserBuildServiceBuildingsPrompt
            or TutorialTrigger.UserBarBuiltInfo
            or TutorialTrigger.UserCanteenBuiltInfo
            or TutorialTrigger.UserGasStationBuiltInfo
            or TutorialTrigger.UserGamblingHallBuiltInfo
            or TutorialTrigger.UserCityParkBuiltInfo
            or TutorialTrigger.UserWorkersOverviewInfo
            or TutorialTrigger.UserWorkerHiringBusInfo
            or TutorialTrigger.UserWarehouseLoadersInfo
            or TutorialTrigger.UserLocalTransportInfo
            or TutorialTrigger.UserLocalBusRoutesInfo;
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
        hasShownUserWorkerShiftInfoTutorial = false;
        hasShownUserLogisticsSetupTutorial = false;
        hasShownUserBuyTruckTutorial = false;
        hasShownUserTruckArrivalTutorial = false;
        hasShownUserTruckFreightTutorial = false;
        hasShownUserWorkersLeisureTutorial = false;
        hasShownUserBuildServiceBuildingsTutorial = false;
        hasShownUserBarBuiltTutorial = false;
        hasShownUserCanteenBuiltTutorial = false;
        hasShownUserGasStationBuiltTutorial = false;
        hasShownUserGamblingHallBuiltTutorial = false;
        hasShownUserCityParkBuiltTutorial = false;
        hasShownUserWorkersOverviewTutorial = false;
        hasShownUserWorkerHiringBusTutorial = false;
        hasShownUserWarehouseLoadersTutorial = false;
        hasShownUserLocalTransportTutorial = false;
        hasShownUserLocalBusRoutesTutorial = false;
        nextUserServiceBuildingInfoTutorialStep = 15;
        hasShownFleetSelectTruckTutorial = false;
        hasShownFleetAssignDriverTutorial = false;
        hasShownFleetPickDriverTutorial = false;
        hasShownAssignSawmillWorkerTutorial = false;
        hasShownSawmillWorkerAssignedTutorial = false;
        hasShownSawmillBuiltTutorial = false;
        pendingTutorialTrigger = null;
        pendingTutorialDelay = 0f;
        areTutorialVacanciesFullyUnlocked = false;
        isTutorialTruckDriverVacancyUnlocked = false;
        isTutorialCameraFocusActive = false;
        tutorialCameraFollowTruck = null;
        tutorialCameraFollowHiringBus = false;
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
                    "Now build your first road.\n\nOpen the Build menu from the top HUD or press B. Choose a road tool, then left-click a cell to place the road.\n\nYour first road must connect to the Highway. Otherwise the town is cut off from outside traffic.\n\nHold Shift and drag to build a longer road segment. Press R to rotate the road direction before placing.");
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
                ShowTutorialWindow(
                    TutorialTrigger.UserCoreBuildingsPrompt,
                    3,
                    "Build the town core",
                    "The road is only useful when it connects important places.\n\nThree core buildings are now unlocked: Warehouse, Motel, and Parking.\n\nBuild all three from the Build menu. You can open Build from the top HUD or press B.\n\nEvery building needs road access. If a building is not connected by road, workers and vehicles will not be able to use it properly.");
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
                ShowTutorialWindow(
                    TutorialTrigger.UserLumberjackWorkerAssignedInfo,
                    9,
                    "Lumberjack Work",
                    "The assigned worker will go to the camp during production hours, walk to nearby trees, chop them into Logs, and carry those Logs back one by one.\n\nLater you will move those Logs through the logistics chain.");
                break;
            case TutorialTrigger.UserWorkerShiftInfo:
                if (hasShownUserWorkerShiftInfoTutorial)
                {
                    return;
                }

                hasShownUserWorkerShiftInfoTutorial = true;
                bool workerShiftRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserWorkerShiftInfo,
                    9,
                    workerShiftRu ? "\u0421\u043c\u0435\u043d\u044b \u0438 \u0437\u0430\u0440\u043f\u043b\u0430\u0442\u0430" : "Shifts and Pay",
                    workerShiftRu
                        ? "\u041d\u0430\u0437\u043d\u0430\u0447\u0435\u043d\u043d\u044b\u0435 \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u043d\u0435 \u0440\u0430\u0431\u043e\u0442\u0430\u044e\u0442 \u043f\u043e\u0441\u0442\u043e\u044f\u043d\u043d\u043e. \u041e\u043d\u0438 \u0432\u044b\u0445\u043e\u0434\u044f\u0442 \u043d\u0430 \u0441\u0432\u043e\u0438 \u0441\u043c\u0435\u043d\u044b \u0438 \u0432\u044b\u043f\u043e\u043b\u043d\u044f\u044e\u0442 \u0437\u0430\u043a\u0440\u0435\u043f\u043b\u0451\u043d\u043d\u0443\u044e \u0440\u0430\u0431\u043e\u0442\u0443 \u0432 \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u0447\u0430\u0441\u044b.\n\n\u0412 \u043a\u043e\u043d\u0446\u0435 \u043a\u0430\u0436\u0434\u043e\u0439 \u0441\u043c\u0435\u043d\u044b \u0440\u0430\u0431\u043e\u0447\u0438\u0439 \u043f\u043e\u043b\u0443\u0447\u0430\u0435\u0442 \u0437\u0430\u0440\u043f\u043b\u0430\u0442\u0443. \u0425\u043e\u0440\u043e\u0448\u0438\u0439 \u0433\u043e\u0440\u043e\u0434 \u043f\u043b\u0430\u0442\u0438\u0442 \u043b\u044e\u0434\u044f\u043c \u0432\u043e\u0432\u0440\u0435\u043c\u044f."
                        : "Assigned workers do not work forever. They report for their scheduled shifts and do their assigned job during work hours.\n\nAt the end of each shift, the worker receives wages. A town that pays on time stays a town, not an unfortunate experiment.");
                break;
            case TutorialTrigger.UserLogisticsSetupInfo:
                if (hasShownUserLogisticsSetupTutorial)
                {
                    return;
                }

                hasShownUserLogisticsSetupTutorial = true;
                bool logisticsSetupRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserLogisticsSetupInfo,
                    10,
                    logisticsSetupRu ? "\u041d\u0430\u043b\u0430\u0434\u044c \u0433\u0440\u0443\u0437\u043e\u043f\u0435\u0440\u0435\u0432\u043e\u0437\u043a\u0438" : "Set Up Freight",
                    logisticsSetupRu
                        ? "\u041f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0441\u0442\u0432\u0435\u043d\u043d\u044b\u0435 \u0437\u0434\u0430\u043d\u0438\u044f \u043d\u0430\u043a\u0430\u043f\u043b\u0438\u0432\u0430\u044e\u0442 \u0440\u0435\u0441\u0443\u0440\u0441\u044b \u0443 \u0441\u0435\u0431\u044f. \u041d\u0430 \u0441\u043a\u043b\u0430\u0434 \u043e\u043d\u0438 \u0441\u0430\u043c\u0438 \u043d\u0435 \u043f\u043e\u043f\u0430\u0434\u0443\u0442.\n\n\u0427\u0442\u043e\u0431\u044b \u0433\u043e\u0440\u043e\u0434 \u043d\u0430\u0447\u0430\u043b \u0440\u0430\u0431\u043e\u0442\u0430\u0442\u044c \u043a\u0430\u043a \u0441\u0438\u0441\u0442\u0435\u043c\u0430, \u043d\u0443\u0436\u0435\u043d \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a \u0438 \u0432\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u043d\u0430 \u0441\u043c\u0435\u043d\u0435.\n\n\u041f\u0435\u0440\u0432\u044b\u0439 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a \u043f\u043e\u043a\u0443\u043f\u0430\u0435\u0442\u0441\u044f \u0432 \u043c\u0435\u043d\u044e \u0412\u0430\u043a\u0430\u043d\u0441\u0438\u0438: \u0432\u044b\u0431\u0435\u0440\u0438 \u0412\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0430, \u0437\u0430\u0442\u0435\u043c \u0438\u0441\u043f\u043e\u043b\u044c\u0437\u0443\u0439 \u0431\u043b\u043e\u043a \u0422\u0440\u0430\u043d\u0441\u043f\u043e\u0440\u0442\u043d\u044b\u0439 \u043f\u0430\u0440\u043a. \u041e\u043d \u0441\u0442\u043e\u0438\u0442 $300."
                        : "Production buildings store resources locally. Those resources will not walk to the Warehouse by themselves.\n\nTo make the town work as a system, you need a truck and a driver assigned to a shift.\n\nBuy the first truck from Vacancies: select Truck Driver, then use the Transport Park block. It costs $300.");
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
                    buyTruckRu ? "\u041a\u0443\u043f\u0438 \u043f\u0435\u0440\u0432\u044b\u0439 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a" : "Buy the first truck",
                    buyTruckRu
                        ? "\u0420\u0435\u0441\u0443\u0440\u0441\u044b \u043d\u0435 \u043f\u0435\u0440\u0435\u043c\u0435\u0449\u0430\u044e\u0442\u0441\u044f \u0441\u0430\u043c\u0438. \u0413\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438 \u043f\u0435\u0440\u0435\u0432\u043e\u0437\u044f\u0442 \u0433\u0440\u0443\u0437 \u043c\u0435\u0436\u0434\u0443 \u0437\u0434\u0430\u043d\u0438\u044f\u043c\u0438, \u0430 \u043f\u043e\u0437\u0436\u0435 \u0441\u043c\u043e\u0433\u0443\u0442 \u0432\u043e\u0437\u0438\u0442\u044c \u0442\u043e\u0432\u0430\u0440\u044b \u0432\u043e \u0432\u043d\u0435\u0448\u043d\u0438\u0435 \u0442\u043e\u0440\u0433\u043e\u0432\u044b\u0435 \u043c\u0430\u0440\u0448\u0440\u0443\u0442\u044b.\n\n\u041e\u0442\u043a\u0440\u043e\u0439 \u0412\u0430\u043a\u0430\u043d\u0441\u0438\u0438, \u0432\u044b\u0431\u0435\u0440\u0438 \u0412\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0430 \u0438 \u043d\u0430\u0439\u0434\u0438 \u0431\u043b\u043e\u043a \u0422\u0440\u0430\u043d\u0441\u043f\u043e\u0440\u0442\u043d\u044b\u0439 \u043f\u0430\u0440\u043a. \u0422\u0430\u043c \u043c\u043e\u0436\u043d\u043e \u043a\u0443\u043f\u0438\u0442\u044c \u043f\u0435\u0440\u0432\u044b\u0439 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a \u0437\u0430 $300."
                        : "Resources do not move by themselves. Trucks move cargo between buildings and, later, toward outside trade routes.\n\nOpen Vacancies, select Truck Driver, and find the Transport Park block. Buy your first truck there for $300.");
                break;
            case TutorialTrigger.UserTruckPurchasedArrivalInfo:
                if (hasShownUserTruckArrivalTutorial)
                {
                    return;
                }

                hasShownUserTruckArrivalTutorial = true;
                bool truckArrivalRu = IsRussianLanguage();
                isTutorialSideMode = false;
                tutorialSideOnLeft = false;
                ShowTutorialWindow(
                    TutorialTrigger.UserTruckPurchasedArrivalInfo,
                    11,
                    truckArrivalRu ? "Грузовик в пути" : "Truck en route",
                    truckArrivalRu
                        ? "Купленный грузовик въезжает с магистрали и направляется к Парковке.\n\nКогда он припаркуется, он станет частью автопарка и его можно будет использовать в логистике."
                        : "The purchased truck is entering from the highway and heading to your Parking.\n\nWhen it parks, it becomes part of your fleet and can be assigned to logistics work.");
                break;
            case TutorialTrigger.UserTruckAssignedFreightInfo:
                if (hasShownUserTruckFreightTutorial)
                {
                    return;
                }

                hasShownUserTruckFreightTutorial = true;
                FocusCameraOnAssignedTutorialTruck();
                bool truckFreightRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserTruckAssignedFreightInfo,
                    12,
                    truckFreightRu ? "\u0413\u0440\u0443\u0437\u043e\u043f\u0435\u0440\u0435\u0432\u043e\u0437\u043a\u0438" : "Freight Runs",
                    truckFreightRu
                        ? "\u0413\u0440\u0443\u0437\u043e\u0432\u0438\u043a \u0441 \u0432\u043e\u0434\u0438\u0442\u0435\u043b\u0435\u043c \u0438\u0449\u0435\u0442 \u043f\u043e\u043b\u0435\u0437\u043d\u044b\u0435 \u0440\u0435\u0439\u0441\u044b: \u0437\u0430\u0431\u0438\u0440\u0430\u0435\u0442 \u0440\u0435\u0441\u0443\u0440\u0441\u044b \u0438\u0437 \u0437\u0434\u0430\u043d\u0438\u0439 \u0438 \u0432\u0435\u0437\u0435\u0442 \u0438\u0445 \u0442\u0443\u0434\u0430, \u0433\u0434\u0435 \u043e\u043d\u0438 \u043d\u0443\u0436\u043d\u044b.\n\n\u0415\u0441\u043b\u0438 \u0431\u043e\u043b\u0435\u0435 \u0432\u0430\u0436\u043d\u043e\u0433\u043e \u0437\u0430\u0434\u0430\u043d\u0438\u044f \u043d\u0435\u0442, \u043e\u043d \u0441\u043c\u043e\u0436\u0435\u0442 \u0432\u043e\u0437\u0438\u0442\u044c \u0411\u0440\u0451\u0432\u043d\u0430 \u0438\u0437 \u041b\u0430\u0433\u0435\u0440\u044f \u043b\u0435\u0441\u043e\u0440\u0443\u0431\u043e\u0432 \u043d\u0430 \u0421\u043a\u043b\u0430\u0434."
                        : "A truck with an assigned driver looks for useful freight runs: it picks up resources from buildings and moves them where they are needed.\n\nIf no higher-priority route exists, it can move Logs from the Lumberjack Camp to the Warehouse.");
                break;
            case TutorialTrigger.UserWorkersLeisureInfo:
                if (hasShownUserWorkersLeisureTutorial)
                {
                    return;
                }

                hasShownUserWorkersLeisureTutorial = true;
                bool leisureRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserWorkersLeisureInfo,
                    13,
                    leisureRu ? "\u0416\u0438\u0437\u043d\u044c \u043f\u043e\u0441\u043b\u0435 \u0441\u043c\u0435\u043d\u044b" : "Life After Work",
                    leisureRu
                        ? "\u0420\u0430\u0431\u043e\u0447\u0438\u0435 \u0437\u0430\u0440\u0430\u0431\u0430\u0442\u044b\u0432\u0430\u044e\u0442 \u0434\u0435\u043d\u044c\u0433\u0438, \u043d\u043e \u0438\u043c \u043d\u0443\u0436\u043d\u043e \u043d\u0435 \u0442\u043e\u043b\u044c\u043a\u043e \u0441\u043f\u0430\u0442\u044c \u0438 \u0440\u0430\u0431\u043e\u0442\u0430\u0442\u044c.\n\n\u041e\u043d\u0438 \u0431\u0443\u0434\u0443\u0442 \u0435\u0441\u0442\u044c, \u043e\u0442\u0434\u044b\u0445\u0430\u0442\u044c, \u0438\u0441\u043a\u0430\u0442\u044c \u0434\u043e\u0441\u0443\u0433 \u0438 \u0442\u0440\u0430\u0442\u0438\u0442\u044c \u043d\u0430 \u044d\u0442\u043e \u0441\u0432\u043e\u044e \u0437\u0430\u0440\u043f\u043b\u0430\u0442\u0443. \u0413\u043e\u0440\u043e\u0434 \u0441\u0442\u0430\u043d\u0435\u0442 \u0436\u0438\u0432\u0435\u0435, \u0435\u0441\u043b\u0438 \u0438\u043c \u0435\u0441\u0442\u044c \u043a\u0443\u0434\u0430 \u043f\u043e\u0439\u0442\u0438."
                        : "Workers earn money, but they need more than sleep and work.\n\nThey will eat, rest, look for leisure, and spend wages on those places. The town feels more alive when workers have somewhere to go.");
                break;
            case TutorialTrigger.UserBuildServiceBuildingsPrompt:
                if (hasShownUserBuildServiceBuildingsTutorial)
                {
                    return;
                }

                hasShownUserBuildServiceBuildingsTutorial = true;
                UnlockBuildTool(BuildTool.Bar);
                UnlockBuildTool(BuildTool.GamblingHall);
                UnlockBuildTool(BuildTool.Canteen);
                UnlockBuildTool(BuildTool.GasStation);
                UnlockBuildTool(BuildTool.CityPark);
                bool servicesRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserBuildServiceBuildingsPrompt,
                    14,
                    servicesRu ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u0441\u0435\u0440\u0432\u0438\u0441\u044b" : "Build Services",
                    servicesRu
                        ? "\u0422\u0435\u043f\u0435\u0440\u044c \u043e\u0442\u043a\u0440\u044b\u0442\u044b \u0441\u0435\u0440\u0432\u0438\u0441\u044b: \u0411\u0430\u0440, \u0418\u0433\u0440\u043e\u0432\u044b\u0435 \u0430\u0432\u0442\u043e\u043c\u0430\u0442\u044b, \u0421\u0442\u043e\u043b\u043e\u0432\u0430\u044f, \u0417\u0430\u043f\u0440\u0430\u0432\u043a\u0430 \u0438 City Park.\n\n\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u0438\u0445 \u0447\u0435\u0440\u0435\u0437 \u043c\u0435\u043d\u044e \u0421\u0442\u0440\u043e\u0439\u043a\u0430. \u041a \u0437\u0434\u0430\u043d\u0438\u044f\u043c \u0441\u043d\u043e\u0432\u0430 \u043d\u0443\u0436\u043d\u0430 \u0434\u043e\u0440\u043e\u0433\u0430, \u0430 \u043f\u0430\u0440\u043a\u0443 \u043d\u0443\u0436\u043d\u043e \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u043e\u0435 \u043c\u0435\u0441\u0442\u043e."
                        : "Service buildings are now unlocked: Bar, Gambling Hall, Canteen, Gas Station, and City Park.\n\nBuild them from the Build menu. Buildings still need road access, while the park needs enough free space.");
                break;
            case TutorialTrigger.UserBarBuiltInfo:
                if (hasShownUserBarBuiltTutorial) return;
                hasShownUserBarBuiltTutorial = true;
                bool barRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserBarBuiltInfo,
                    GetNextUserServiceBuildingInfoTutorialStep(),
                    barRu ? "\u0411\u0430\u0440" : "Bar",
                    barRu
                        ? "\u0411\u0430\u0440 \u0434\u0430\u0451\u0442 \u0440\u0430\u0431\u043e\u0447\u0438\u043c \u0434\u043e\u0441\u0443\u0433 \u0438 \u0437\u0430\u0431\u0438\u0440\u0430\u0435\u0442 \u0447\u0430\u0441\u0442\u044c \u0438\u0445 \u0437\u0430\u0440\u043f\u043b\u0430\u0442\u044b \u0432 \u043a\u0430\u0441\u0441\u0443 \u0437\u0434\u0430\u043d\u0438\u044f.\n\n\u0410\u043b\u043a\u043e\u0433\u043e\u043b\u044c \u043c\u043e\u0436\u0435\u0442 \u0434\u0430\u0432\u0430\u0442\u044c \u044d\u0444\u0444\u0435\u043a\u0442\u044b: \u0438\u043d\u043e\u0433\u0434\u0430 \u043f\u043e\u043b\u0435\u0437\u043d\u044b\u0435, \u0438\u043d\u043e\u0433\u0434\u0430 \u0441\u043e\u0432\u0441\u0435\u043c \u043d\u0435 \u0434\u043b\u044f \u0440\u0443\u043b\u044f."
                        : "The Bar gives workers leisure and moves some of their wages into the building bank.\n\nAlcohol can add effects: sometimes useful, sometimes very much not for driving.");
                break;
            case TutorialTrigger.UserCanteenBuiltInfo:
                if (hasShownUserCanteenBuiltTutorial) return;
                hasShownUserCanteenBuiltTutorial = true;
                bool canteenRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserCanteenBuiltInfo,
                    GetNextUserServiceBuildingInfoTutorialStep(),
                    canteenRu ? "\u0421\u0442\u043e\u043b\u043e\u0432\u0430\u044f" : "Canteen",
                    canteenRu
                        ? "\u0421\u0442\u043e\u043b\u043e\u0432\u0430\u044f \u0437\u0430\u043a\u0440\u044b\u0432\u0430\u0435\u0442 \u043f\u043e\u0442\u0440\u0435\u0431\u043d\u043e\u0441\u0442\u044c \u0432 \u0435\u0434\u0435.\n\n\u041a\u043e\u0433\u0434\u0430 \u0440\u0430\u0431\u043e\u0447\u0438\u0439 \u0435\u0441\u0442, \u043e\u043d \u043f\u043b\u0430\u0442\u0438\u0442 \u0441\u0435\u0440\u0432\u0438\u0441\u043d\u044b\u0439 \u0441\u0431\u043e\u0440, \u0430 \u0441\u044b\u0442\u043e\u0441\u0442\u044c \u043f\u043e\u043c\u043e\u0433\u0430\u0435\u0442 \u0435\u043c\u0443 \u0440\u0430\u0431\u043e\u0442\u0430\u0442\u044c \u0441\u0442\u0430\u0431\u0438\u043b\u044c\u043d\u0435\u0435."
                        : "The Canteen satisfies the Food need.\n\nWhen a worker eats, they pay a service fee, and being fed helps them work more steadily.");
                break;
            case TutorialTrigger.UserGasStationBuiltInfo:
                if (hasShownUserGasStationBuiltTutorial) return;
                hasShownUserGasStationBuiltTutorial = true;
                bool gasRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserGasStationBuiltInfo,
                    GetNextUserServiceBuildingInfoTutorialStep(),
                    gasRu ? "\u0417\u0430\u043f\u0440\u0430\u0432\u043a\u0430" : "Gas Station",
                    gasRu
                        ? "\u0414\u0430, \u0443 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u043e\u0432 \u0442\u043e\u0436\u0435 \u0435\u0441\u0442\u044c \u043d\u0443\u0436\u0434\u044b. \u041e\u043d\u0438 \u043d\u0435 \u0434\u0440\u0430\u043c\u0430\u0442\u0438\u0447\u043d\u044b\u0435: \u0442\u043e\u043f\u043b\u0438\u0432\u043e, \u043f\u043e\u043c\u043f\u0430 \u0438 \u043c\u0435\u0441\u0442\u043e, \u0433\u0434\u0435 \u0438\u043c \u0440\u0430\u0437\u0440\u0435\u0448\u0430\u0442 \u043f\u0435\u0440\u0435\u0441\u0442\u0430\u0442\u044c \u0441\u0442\u0440\u0430\u0434\u0430\u0442\u044c.\n\n\u0417\u0430\u043f\u0440\u0430\u0432\u043a\u0430 \u043e\u0431\u0441\u043b\u0443\u0436\u0438\u0432\u0430\u0435\u0442 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438, \u043a\u043e\u0433\u0434\u0430 \u0442\u043e\u043f\u043b\u0438\u0432\u043e \u0437\u0430\u043a\u0430\u043d\u0447\u0438\u0432\u0430\u0435\u0442\u0441\u044f. \u0411\u0435\u0437 \u043d\u0435\u0451 \u043b\u043e\u0433\u0438\u0441\u0442\u0438\u043a\u0430 \u0431\u044b\u0441\u0442\u0440\u043e \u0441\u0442\u0430\u043d\u043e\u0432\u0438\u0442\u0441\u044f \u043f\u0430\u0440\u043a\u043e\u043c \u0434\u043e\u0440\u043e\u0433\u0438\u0445 \u0441\u043a\u0443\u043b\u044c\u043f\u0442\u0443\u0440."
                        : "Trucks have needs too. Less dramatic ones: fuel, a pump, and a place where they are allowed to stop suffering.\n\nThe Gas Station services trucks when fuel runs low. Without it, logistics quickly becomes a park of expensive sculptures.");
                break;
            case TutorialTrigger.UserGamblingHallBuiltInfo:
                if (hasShownUserGamblingHallBuiltTutorial) return;
                hasShownUserGamblingHallBuiltTutorial = true;
                bool gamblingRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserGamblingHallBuiltInfo,
                    GetNextUserServiceBuildingInfoTutorialStep(),
                    gamblingRu ? "\u0418\u0433\u0440\u043e\u0432\u044b\u0435 \u0430\u0432\u0442\u043e\u043c\u0430\u0442\u044b" : "Gambling Hall",
                    gamblingRu
                        ? "\u0418\u0433\u0440\u043e\u0432\u044b\u0435 \u0430\u0432\u0442\u043e\u043c\u0430\u0442\u044b \u0434\u0430\u044e\u0442 \u0434\u043e\u0441\u0443\u0433 \u0438 \u043d\u0435\u043c\u043d\u043e\u0433\u043e \u0440\u0438\u0441\u043a\u0430.\n\n\u0420\u0430\u0431\u043e\u0447\u0438\u0435 \u043c\u043e\u0433\u0443\u0442 \u0442\u0440\u0430\u0442\u0438\u0442\u044c \u0442\u0430\u043c \u0434\u0435\u043d\u044c\u0433\u0438, \u0430 \u0437\u0434\u0430\u043d\u0438\u0435 \u043d\u0430\u043a\u0430\u043f\u043b\u0438\u0432\u0430\u0435\u0442 \u0441\u0432\u043e\u044e \u043a\u0430\u0441\u0441\u0443."
                        : "The Gambling Hall provides leisure with a little risk.\n\nWorkers can spend money there, and the building stores its own bank.");
                break;
            case TutorialTrigger.UserCityParkBuiltInfo:
                if (hasShownUserCityParkBuiltTutorial) return;
                hasShownUserCityParkBuiltTutorial = true;
                bool parkRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserCityParkBuiltInfo,
                    GetNextUserServiceBuildingInfoTutorialStep(),
                    "City Park",
                    parkRu
                        ? "City Park \u0434\u0430\u0451\u0442 \u0440\u0430\u0431\u043e\u0447\u0438\u043c \u0441\u043f\u043e\u043a\u043e\u0439\u043d\u044b\u0439 \u0434\u043e\u0441\u0443\u0433 \u0431\u0435\u0437 \u043a\u0430\u0441\u0441\u044b \u0438 \u0430\u0432\u0442\u043e\u043c\u0430\u0442\u043e\u0432.\n\n\u041e\u043d\u0438 \u043c\u043e\u0433\u0443\u0442 \u0433\u0443\u043b\u044f\u0442\u044c \u043f\u043e \u043f\u0430\u0440\u043a\u0443, \u0441\u0438\u0434\u0435\u0442\u044c \u043d\u0430 \u043b\u0430\u0432\u043e\u0447\u043a\u0430\u0445 \u0438 \u043f\u0440\u043e\u0441\u0442\u043e \u043d\u0435 \u0431\u044b\u0442\u044c \u0447\u0430\u0441\u0442\u044c\u044e \u043b\u043e\u0433\u0438\u0441\u0442\u0438\u0447\u0435\u0441\u043a\u043e\u0439 \u043c\u0430\u0448\u0438\u043d\u044b \u043f\u0430\u0440\u0443 \u0447\u0430\u0441\u043e\u0432."
                        : "City Park gives workers calm leisure without a cash register or slot machine.\n\nThey can walk, sit on benches, and briefly stop being a component in the logistics machine.");
                break;
            case TutorialTrigger.UserWorkersOverviewInfo:
                if (hasShownUserWorkersOverviewTutorial) return;
                hasShownUserWorkersOverviewTutorial = true;
                bool overviewRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserWorkersOverviewInfo,
                    20,
                    overviewRu ? "\u0420\u0430\u0431\u043e\u0447\u0438\u0435" : "Workers",
                    overviewRu
                        ? "\u0423 \u043a\u0430\u0436\u0434\u043e\u0433\u043e \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e \u0435\u0441\u0442\u044c \u043d\u0430\u0432\u044b\u043a\u0438, \u0434\u0435\u043d\u044c\u0433\u0438, \u043d\u0443\u0436\u0434\u044b, \u043f\u0435\u0440\u043a\u0438 \u0438 \u0432\u0440\u0435\u043c\u0435\u043d\u043d\u044b\u0435 \u044d\u0444\u0444\u0435\u043a\u0442\u044b.\n\n\u041d\u0443\u0436\u0434\u044b \u043f\u043e\u0434\u0441\u043a\u0430\u0436\u0443\u0442, \u0447\u0442\u043e \u0447\u0435\u043b\u043e\u0432\u0435\u043a\u0443 \u0441\u0435\u0439\u0447\u0430\u0441 \u043d\u0443\u0436\u043d\u043e: \u0435\u0434\u0430, \u0441\u043e\u043d \u0438\u043b\u0438 \u0434\u043e\u0441\u0443\u0433.\n\n\u041e\u0442\u043a\u0440\u043e\u0439 \u0420\u0430\u0431\u043e\u0447\u0438\u0445, \u043f\u043e\u0441\u043c\u043e\u0442\u0440\u0438 \u043a\u0430\u0440\u0442\u043e\u0447\u043a\u0443 \u0438 \u043d\u0430\u0439\u043c\u0438 \u043d\u043e\u0432\u043e\u0433\u043e \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e."
                        : "Each worker has skills, money, needs, perks, and temporary effects.\n\nNeeds tell you what the person currently requires: food, sleep, or leisure.\n\nOpen Workers, inspect a worker card, and hire a new worker.");
                break;
            case TutorialTrigger.UserWorkerHiringBusInfo:
                if (hasShownUserWorkerHiringBusTutorial) return;
                hasShownUserWorkerHiringBusTutorial = true;
                FocusCameraOnHiringBusForTutorial();
                bool hireBusRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserWorkerHiringBusInfo,
                    21,
                    hireBusRu ? "\u041d\u043e\u0432\u044b\u0435 \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u0432 \u043f\u0443\u0442\u0438" : "New Workers En Route",
                    hireBusRu
                        ? "\u041d\u043e\u0432\u044b\u0435 \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u043f\u0440\u0438\u0435\u0437\u0436\u0430\u044e\u0442 \u0438\u0437\u0432\u043d\u0435 \u043d\u0430 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0435. \u041e\u043d\u0438 \u0432\u044b\u0439\u0434\u0443\u0442 \u0443 \u043c\u0435\u0436\u0434\u0443\u0433\u043e\u0440\u043e\u0434\u043d\u0435\u0439 \u043e\u0441\u0442\u0430\u043d\u043e\u0432\u043a\u0438 \u0438 \u043f\u043e\u0439\u0434\u0443\u0442 \u043a \u041c\u043e\u0442\u0435\u043b\u044e.\n\n\u0412 \u044d\u0442\u043e\u043c \u043e\u0431\u0443\u0447\u0430\u044e\u0449\u0435\u043c \u0437\u0430\u0435\u0437\u0434\u0435 \u0430\u0432\u0442\u043e\u0431\u0443\u0441 \u043f\u0440\u0438\u0432\u0435\u0437\u0451\u0442 \u0441\u0440\u0430\u0437\u0443 7 \u0440\u0430\u0431\u043e\u0447\u0438\u0445, \u0447\u0442\u043e\u0431\u044b \u0433\u043e\u0440\u043e\u0434 \u0431\u044b\u0441\u0442\u0440\u0435\u0435 \u043e\u0436\u0438\u043b."
                        : "New workers arrive from outside by bus. They get off at the intercity stop and walk to the Motel.\n\nFor this tutorial run, the bus brings 7 workers at once so the town can become busy faster.");
                break;
            case TutorialTrigger.UserWarehouseLoadersInfo:
                if (hasShownUserWarehouseLoadersTutorial) return;
                hasShownUserWarehouseLoadersTutorial = true;
                bool warehouseLoadersRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserWarehouseLoadersInfo,
                    22,
                    warehouseLoadersRu ? "\u0420\u0435\u0441\u0443\u0440\u0441\u044b \u0438 \u0433\u0440\u0443\u0437\u0447\u0438\u043a\u0438" : "Resources and Loaders",
                    warehouseLoadersRu
                        ? "\u041c\u0435\u043d\u044e \u0420\u0435\u0441\u0443\u0440\u0441\u044b \u043f\u043e\u043a\u0430\u0437\u044b\u0432\u0430\u0435\u0442, \u0447\u0442\u043e \u043d\u0430\u043a\u043e\u043f\u043b\u0435\u043d\u043e \u0432 \u0433\u043e\u0440\u043e\u0434\u0435 \u0438 \u043d\u0430 \u0421\u043a\u043b\u0430\u0434\u0435.\n\n\u0421\u043a\u043b\u0430\u0434 \u043d\u0435 \u0442\u043e\u043b\u044c\u043a\u043e \u0445\u0440\u0430\u043d\u0438\u0442 \u0442\u043e\u0432\u0430\u0440\u044b. \u0421\u043a\u043b\u0430\u0434\u0441\u043a\u0438\u0435 \u0433\u0440\u0443\u0437\u0447\u0438\u043a\u0438 \u0440\u0430\u0437\u043d\u043e\u0441\u044f\u0442 \u0437\u0430\u043f\u0430\u0441\u044b \u043f\u043e \u0441\u0435\u0440\u0432\u0438\u0441\u043d\u044b\u043c \u0437\u0434\u0430\u043d\u0438\u044f\u043c: \u0435\u0434\u0443, \u0430\u043b\u043a\u043e\u0433\u043e\u043b\u044c \u0438 \u0442\u043e\u043f\u043b\u0438\u0432\u043e.\n\n\u041e\u0442\u043a\u0440\u043e\u0439 \u0412\u0430\u043a\u0430\u043d\u0441\u0438\u0438 \u0438 \u043d\u0430\u0437\u043d\u0430\u0447\u044c 3 \u0433\u0440\u0443\u0437\u0447\u0438\u043a\u043e\u0432 \u043d\u0430 \u0421\u043a\u043b\u0430\u0434."
                        : "The Resources menu shows what the town and Warehouse have stored.\n\nWarehouse does more than store goods. Warehouse loaders carry supplies to service buildings: food, alcohol, and fuel.\n\nOpen Vacancies and assign 3 loaders to the Warehouse.");
                break;
            case TutorialTrigger.UserLocalTransportInfo:
                ShowUserLocalTransportTutorial();
                break;
            case TutorialTrigger.UserLocalBusRoutesInfo:
                ShowUserLocalBusRoutesTutorial();
                break;
            case TutorialTrigger.BuildMotelPrompt:
                ShowTutorialWindow(
                    TutorialTrigger.BuildMotelPrompt,
                    2,
                    "Build a Motel",
                    "The Motel unlocks worker hiring and gives workers a place to rest.\n\nOpen Building at the top, or press B. Choose Motel and place it near your road plan.\n\nIn Build mode, press R to rotate the building before placing it.");
                break;
            case TutorialTrigger.FirstMotelBuilt:
                if (hasShownFirstMotelTutorial) return;
                hasShownFirstMotelTutorial = true;
                ShowTutorialWindow(
                    TutorialTrigger.FirstMotelBuilt,
                    3,
                    "Open the Workers Panel",
                    "The Motel is ready, so you can hire workers.\n\nOpen the Workers panel at the top of the screen. This is where new workers are hired and tracked.");
                break;
            case TutorialTrigger.WorkersPanelOpened:
                if (hasShownWorkersPanelTutorial) return;
                hasShownWorkersPanelTutorial = true;
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

    private static readonly Color OverlayColorFull        = new(0.02f, 0.03f, 0.05f, 0.56f);
    private static readonly Color OverlayColorTransparent = new(0f, 0f, 0f, 0f);

    private void ApplyTutorialWindowLayout()
    {
        if (tutorialHud?.WindowRect == null) return;
        if (activeTutorialTrigger is TutorialTrigger.UserTruckPurchasedArrivalInfo
            or TutorialTrigger.UserWorkerHiringBusInfo
            or TutorialTrigger.UserTruckAssignedFreightInfo
            or TutorialTrigger.UserWorkersLeisureInfo
            or TutorialTrigger.UserBuildServiceBuildingsPrompt
            or TutorialTrigger.UserBarBuiltInfo
            or TutorialTrigger.UserCanteenBuiltInfo
            or TutorialTrigger.UserGasStationBuiltInfo
            or TutorialTrigger.UserGamblingHallBuiltInfo
            or TutorialTrigger.UserCityParkBuiltInfo
            or TutorialTrigger.UserWorkersOverviewInfo
            or TutorialTrigger.UserLocalTransportInfo
            or TutorialTrigger.UserLocalBusRoutesInfo)
        {
            tutorialHud.WindowRect.sizeDelta        = new Vector2(520f, 330f);
            tutorialHud.WindowRect.anchoredPosition = activeTutorialTrigger == TutorialTrigger.UserWorkerHiringBusInfo
                ? new Vector2(350f, 126f)
                : new Vector2(260f, 92f);
            if (tutorialHud.BodyPanelLayout != null) tutorialHud.BodyPanelLayout.preferredHeight = 150f;
            if (tutorialHud.OverlayImage    != null) tutorialHud.OverlayImage.color = OverlayColorTransparent;
            tutorialBobTime = 0f;
            return;
        }

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
            UnlockAllBuildTools();
            UnlockAllTutorialVacancies();
            isTutorialCameraFocusActive = false;
            ResetTutorialGoalsForNewGame();
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

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserLumberjackCampBuiltInfo)
        {
            isBuildPanelOpen = false;
            isShiftsScreenDirty = true;
            LogUiInput("Tutorial: Lumberjack Camp assignment prompt closed; waiting for player to open Vacancies.");
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

        if (activeTutorialTrigger == TutorialTrigger.UserWorkerHiringBusInfo)
        {
            tutorialCameraFollowHiringBus = false;
            isTutorialCameraFocusActive = false;
            isCameraReturningToDiorama = true;
            UnlockAllBuildTools();
            UnlockAllTutorialVacancies();
            SessionDebugLogger.Log("TUTORIAL", "Unlocked all build tools and vacancies after tutorial hiring bus info.");
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserWarehouseLoadersInfo)
        {
            BeginWarehouseLoadersTutorialGoals();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserLocalTransportInfo)
        {
            BeginLocalTransportTutorialGoals();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserTruckAssignedFreightInfo)
        {
            tutorialCameraFollowTruck = null;
            isTutorialCameraFocusActive = false;
            isCameraReturningToDiorama = true;
            ScheduleTutorial(TutorialTrigger.UserWorkersLeisureInfo, 0.35f);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserWorkersLeisureInfo)
        {
            ScheduleTutorial(TutorialTrigger.UserBuildServiceBuildingsPrompt, 0.25f);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserBuildServiceBuildingsPrompt)
        {
            BeginServiceBuildingsTutorialGoals();
            OpenBuildPanelFromTutorial();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserWorkersOverviewInfo)
        {
            BeginWorkerCardTutorialGoals();
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserLumberjackWorkerAssignedInfo)
        {
            ScheduleTutorial(TutorialTrigger.UserWorkerShiftInfo, 0.8f);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserWorkerShiftInfo)
        {
            ScheduleTutorial(TutorialTrigger.UserLogisticsSetupInfo, 0.2f);
        }

        if (!isTutorialSkipped && activeTutorialTrigger == TutorialTrigger.UserLogisticsSetupInfo)
        {
            UnlockTutorialTruckDriverVacancy();
            BeginBuyTruckTutorialGoals();
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
            isShiftsPanelOpen = false;
            isShiftsScreenDirty = true;
            isTruckDetailsOpen = false;
            isLocalBusDetailsOpen = false;
            isDriverDetailsOpen = false;
            selectedLocation = null;
            selectedLocalStopIndex = -1;
            selectedPersonalHouseIndex = -1;
            RefreshSelectionVisuals();
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

    private void NotifyTutorialServiceBuildingBuilt(LocationType type)
    {
        if (selectedGameStartMode != GameStartMode.User || isTutorialSkipped)
        {
            return;
        }

        TutorialGoalKind goal;
        TutorialTrigger trigger;
        switch (type)
        {
            case LocationType.Bar:
                goal = TutorialGoalKind.BuildBar;
                trigger = TutorialTrigger.UserBarBuiltInfo;
                break;
            case LocationType.Canteen:
                goal = TutorialGoalKind.BuildCanteen;
                trigger = TutorialTrigger.UserCanteenBuiltInfo;
                break;
            case LocationType.GasStation:
                goal = TutorialGoalKind.BuildGasStation;
                trigger = TutorialTrigger.UserGasStationBuiltInfo;
                break;
            case LocationType.GamblingHall:
                goal = TutorialGoalKind.BuildGamblingHall;
                trigger = TutorialTrigger.UserGamblingHallBuiltInfo;
                break;
            case LocationType.CityPark:
                goal = TutorialGoalKind.BuildCityPark;
                trigger = TutorialTrigger.UserCityParkBuiltInfo;
                break;
            default:
                return;
        }

        MarkTutorialGoalComplete(goal);
        TryShowTutorial(trigger);
        SessionDebugLogger.Log("TUTORIAL", $"Service building tutorial notified: {type}.");
    }

    private void FocusCameraOnAssignedTutorialTruck()
    {
        TruckAgent truck = null;
        foreach (TruckAgent candidate in truckAgents)
        {
            if (candidate != null && candidate.AssignedDrivers.Count > 0 && candidate.TruckObject != null)
            {
                truck = candidate;
                break;
            }
        }

        if (truck == null)
        {
            return;
        }

        selectedTruckNumber = truck.TruckNumber;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        isTruckDetailsOpen = false;
        isTruckCameraFocused = false;
        isCameraReturningToDiorama = false;
        isCameraRotatingToTarget = false;
        tutorialCameraFollowTruck = truck;
        tutorialCameraFocusTarget = new Vector3(truck.TruckObject.transform.position.x, 0f, truck.TruckObject.transform.position.z);
        tutorialCameraFocusOffset = new Vector3(-13f, 18f, -13f);
        isTutorialCameraFocusActive = true;
        RefreshSelectionVisuals();
        SessionDebugLogger.Log("TUTORIAL", $"Freight tutorial camera focus started for {truck.DisplayName}.");
    }

}
