using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private enum TutorialTrigger
    {
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
        UserBuildLaborExchangePrompt,
        UserLaborExchangeBuiltInfo,
        UserMigrationInfo,
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
        UserEconomyTaxesInfo,
        UserTradeIntroInfo,
        UserTradeRouteInfo,
        UserDocksPrompt,
        UserDocksBuiltInfo,
        UserTradePolicyInfo,
        UserDemoCompleteInfo,
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
        public Toggle SkipToggle;
        public Text SkipToggleText;
        public Button OkButton;
        public Text OkButtonText;
    }

    private const float TutorialSidePanelBaseX  = 570f;   // right of the 760-wide Drivers panel
    private const float TutorialCameraFocusSpeed = 2.8f;
    private static readonly Vector3 TutorialForestZoomOffset = new(-8f, 12f, -8f);
    private bool  isTutorialSideMode;
    private float tutorialBobTime;
    private bool isTutorialCameraFocusActive;
    private Vector3 tutorialCameraFocusTarget;
    private Vector3 tutorialCameraFocusOffset = TutorialForestZoomOffset;
    private TruckAgent tutorialCameraFollowTruck;
    private bool tutorialCameraFollowHiringBus;
    private string tutorialWindowFullText = string.Empty;
    private float tutorialWindowTypeTime;
    private const float TutorialWindowTypeSpeed = 40f;

    private TutorialHudRefs tutorialHud;
    private TutorialTrigger activeTutorialTrigger;
    private const int NewUserTutorialStepCount = 34;
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
            or TutorialTrigger.UserBuildLaborExchangePrompt
            or TutorialTrigger.UserLaborExchangeBuiltInfo
            or TutorialTrigger.UserMigrationInfo
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
            or TutorialTrigger.UserLocalBusRoutesInfo
            or TutorialTrigger.UserEconomyTaxesInfo
            or TutorialTrigger.UserTradeIntroInfo
            or TutorialTrigger.UserTradeRouteInfo
            or TutorialTrigger.UserDocksPrompt
            or TutorialTrigger.UserDocksBuiltInfo
            or TutorialTrigger.UserTradePolicyInfo
            or TutorialTrigger.UserDemoCompleteInfo;
    }

    private int GetNextUserCoreBuildingInfoTutorialStep()
    {
        int step = Mathf.Clamp(nextUserCoreBuildingInfoTutorialStep, 4, NewUserTutorialStepCount);
        nextUserCoreBuildingInfoTutorialStep = Mathf.Clamp(nextUserCoreBuildingInfoTutorialStep + 1, 4, NewUserTutorialStepCount);
        return step;
    }

    private int GetNextUserServiceBuildingInfoTutorialStep()
    {
        int step = Mathf.Clamp(nextUserServiceBuildingInfoTutorialStep, 19, 23);
        nextUserServiceBuildingInfoTutorialStep = Mathf.Clamp(nextUserServiceBuildingInfoTutorialStep + 1, 19, 23);
        return step;
    }

    private bool IsTutorialEnabledForCurrentMode()
    {
        return selectedGameStartMode == GameStartMode.Tutorial;
    }

    private bool ShouldPauseSimulationForTutorial()
    {
        return !IsNonPausingTutorialTrigger(activeTutorialTrigger);
    }

    private static bool IsNonPausingTutorialTrigger(TutorialTrigger trigger)
    {
        return trigger is TutorialTrigger.UserTruckPurchasedArrivalInfo
            or TutorialTrigger.UserTruckAssignedFreightInfo
            or TutorialTrigger.UserBuildLaborExchangePrompt
            or TutorialTrigger.UserLaborExchangeBuiltInfo
            or TutorialTrigger.UserMigrationInfo
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
            or TutorialTrigger.UserLocalBusRoutesInfo
            or TutorialTrigger.UserEconomyTaxesInfo
            or TutorialTrigger.UserTradeIntroInfo
            or TutorialTrigger.UserTradeRouteInfo
            or TutorialTrigger.UserDocksPrompt
            or TutorialTrigger.UserDocksBuiltInfo
            or TutorialTrigger.UserTradePolicyInfo
            or TutorialTrigger.UserDemoCompleteInfo;
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
        hasShownUserBuildLaborExchangeTutorial = false;
        hasShownUserLaborExchangeBuiltTutorial = false;
        hasShownUserMigrationInfoTutorial = false;
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
        hasShownUserEconomyTaxesTutorial = false;
        hasShownUserTradeIntroTutorial = false;
        hasShownUserTradeRouteTutorial = false;
        hasShownUserDocksTutorial = false;
        hasShownUserDocksBuiltTutorial = false;
        hasShownUserTradePolicyTutorial = false;
        hasShownUserDemoCompleteTutorial = false;
        nextUserServiceBuildingInfoTutorialStep = 19;
        pendingTutorialTrigger = null;
        pendingTutorialDelay = 0f;
        areTutorialVacanciesFullyUnlocked = false;
        isTutorialTruckDriverVacancyUnlocked = false;
        isTutorialCameraFocusActive = false;
        tutorialCameraFollowTruck = null;
        tutorialCameraFollowHiringBus = false;
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

        if (selectedGameStartMode == GameStartMode.Tutorial &&
            !IsNewUserTutorialTrigger(trigger) &&
            !isBeeEasterEgg)
        {
            return;
        }

        switch (trigger)
        {
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
                    "Welcome to Tutorial mode.\n\nYou start with an almost empty map, a highway connection, a bus stop, and a few workers.\n\nBefore building, learn the camera controls: zoom in, zoom out, move the map, and rotate the view.");
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
                        ? "\u041f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0441\u0442\u0432\u0435\u043d\u043d\u044b\u0435 \u0437\u0434\u0430\u043d\u0438\u044f \u043d\u0430\u043a\u0430\u043f\u043b\u0438\u0432\u0430\u044e\u0442 \u0440\u0435\u0441\u0443\u0440\u0441\u044b \u0443 \u0441\u0435\u0431\u044f. \u041d\u0430 \u0441\u043a\u043b\u0430\u0434 \u043e\u043d\u0438 \u0441\u0430\u043c\u0438 \u043d\u0435 \u043f\u043e\u043f\u0430\u0434\u0443\u0442.\n\n\u0427\u0442\u043e\u0431\u044b \u0433\u043e\u0440\u043e\u0434 \u043d\u0430\u0447\u0430\u043b \u0440\u0430\u0431\u043e\u0442\u0430\u0442\u044c \u043a\u0430\u043a \u0441\u0438\u0441\u0442\u0435\u043c\u0430, \u043d\u0443\u0436\u0435\u043d \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a \u0438 \u0432\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u043d\u0430 \u0441\u043c\u0435\u043d\u0435.\n\nParking \u0443\u0436\u0435 \u0434\u0430\u0451\u0442 \u0441\u043b\u043e\u0442\u044b \u0430\u0432\u0442\u043e\u043f\u0430\u0440\u043a\u0430: \u0432\u044b\u0431\u0435\u0440\u0438 \u0412\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0430 \u0438 \u0441\u043c\u0435\u043d\u0443, \u0430 \u043c\u0430\u0448\u0438\u043d\u0430 \u043f\u043e\u044f\u0432\u0438\u0442\u0441\u044f \u0430\u0432\u0442\u043e\u043c\u0430\u0442\u0438\u0447\u0435\u0441\u043a\u0438."
                        : "Production buildings store resources locally. Those resources will not walk to the Warehouse by themselves.\n\nTo make the town work as a system, you need a truck and a driver assigned to a shift.\n\nParking already provides fleet slots: select Truck Driver and a shift, and the vehicle will appear automatically.");
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
                    buyTruckRu ? "\u041d\u0430\u0437\u043d\u0430\u0447\u044c \u0432\u043e\u0434\u0438\u0442\u0435\u043b\u044f" : "Assign the truck driver",
                    buyTruckRu
                        ? "\u0420\u0435\u0441\u0443\u0440\u0441\u044b \u043d\u0435 \u043f\u0435\u0440\u0435\u043c\u0435\u0449\u0430\u044e\u0442\u0441\u044f \u0441\u0430\u043c\u0438. \u0413\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438 \u043f\u0435\u0440\u0435\u0432\u043e\u0437\u044f\u0442 \u0433\u0440\u0443\u0437 \u043c\u0435\u0436\u0434\u0443 \u0437\u0434\u0430\u043d\u0438\u044f\u043c\u0438, \u0430 \u043f\u043e\u0437\u0436\u0435 \u0441\u043c\u043e\u0433\u0443\u0442 \u0432\u043e\u0437\u0438\u0442\u044c \u0442\u043e\u0432\u0430\u0440\u044b \u0432\u043e \u0432\u043d\u0435\u0448\u043d\u0438\u0435 \u0442\u043e\u0440\u0433\u043e\u0432\u044b\u0435 \u043c\u0430\u0440\u0448\u0440\u0443\u0442\u044b.\n\n\u041e\u0442\u043a\u0440\u043e\u0439 \u0412\u0430\u043a\u0430\u043d\u0441\u0438\u0438, \u0432\u044b\u0431\u0435\u0440\u0438 \u0412\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0430 \u0438 \u0441\u043c\u0435\u043d\u0443. Parking \u0437\u0430\u0440\u0435\u0437\u0435\u0440\u0432\u0438\u0440\u0443\u0435\u0442 \u0441\u043b\u043e\u0442 \u0438 \u0432\u044b\u0434\u0430\u0441\u0442 \u043c\u0430\u0448\u0438\u043d\u0443 \u0431\u0435\u0437 \u043e\u0442\u0434\u0435\u043b\u044c\u043d\u043e\u0439 \u043f\u043e\u043a\u0443\u043f\u043a\u0438."
                        : "Resources do not move by themselves. Trucks move cargo between buildings and, later, toward outside trade routes.\n\nOpen Vacancies, select Truck Driver, and choose a shift. Parking reserves a slot and provides the vehicle without a separate purchase.");
                break;
            case TutorialTrigger.UserTruckPurchasedArrivalInfo:
                if (hasShownUserTruckArrivalTutorial)
                {
                    return;
                }

                hasShownUserTruckArrivalTutorial = true;
                bool truckArrivalRu = IsRussianLanguage();
                isTutorialSideMode = false;
                ShowTutorialWindow(
                    TutorialTrigger.UserTruckPurchasedArrivalInfo,
                    11,
                    truckArrivalRu ? "Грузовик готов" : "Truck ready",
                    truckArrivalRu
                        ? "Parking подготовил грузовик из свободного слота автопарка.\n\nКогда водитель получит смену, эта машина будет использоваться в логистике."
                        : "Parking prepared a truck from a free fleet slot.\n\nWhen a driver gets a shift, this vehicle can be used for logistics work.");
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
            case TutorialTrigger.UserBuildLaborExchangePrompt:
                if (hasShownUserBuildLaborExchangeTutorial)
                {
                    return;
                }

                hasShownUserBuildLaborExchangeTutorial = true;
                UnlockBuildTool(BuildTool.LaborExchange);
                bool laborBuildRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserBuildLaborExchangePrompt,
                    13,
                    laborBuildRu ? "\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430" : "Labor Exchange",
                    laborBuildRu
                        ? "\u0420\u0443\u0447\u043d\u043e\u0439 \u043d\u0430\u0439\u043c \u043d\u0443\u0436\u0435\u043d \u0442\u043e\u043b\u044c\u043a\u043e \u0434\u043b\u044f \u043f\u0435\u0440\u0432\u044b\u0445 \u0448\u0430\u0433\u043e\u0432.\n\n\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u0411\u0438\u0440\u0436\u0443 \u0442\u0440\u0443\u0434\u0430. \u0415\u0451 \u043a\u043b\u0435\u0440\u043a \u0441 \u0432\u044b\u0441\u0448\u0438\u043c \u043e\u0431\u0440\u0430\u0437\u043e\u0432\u0430\u043d\u0438\u0435\u043c \u0431\u0443\u0434\u0435\u0442 \u043f\u0443\u0431\u043b\u0438\u043a\u043e\u0432\u0430\u0442\u044c \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u0438, \u0430 \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u044b\u0435 \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u0441\u0430\u043c\u0438 \u0431\u0443\u0434\u0443\u0442 \u043f\u0440\u0438\u0445\u043e\u0434\u0438\u0442\u044c \u0441\u044e\u0434\u0430 \u0437\u0430 \u0440\u0430\u0431\u043e\u0442\u043e\u0439."
                        : "Manual assignment is only for the first steps.\n\nBuild a Labor Exchange. Its higher-educated clerk publishes vacancies, and free workers will come here to apply for jobs automatically.");
                break;
            case TutorialTrigger.UserLaborExchangeBuiltInfo:
                if (hasShownUserLaborExchangeBuiltTutorial)
                {
                    return;
                }

                hasShownUserLaborExchangeBuiltTutorial = true;
                bool laborBuiltRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserLaborExchangeBuiltInfo,
                    14,
                    laborBuiltRu ? "\u0412\u0430\u043a\u0430\u043d\u0441\u0438\u0438 \u043e\u0436\u0438\u0432\u0430\u044e\u0442" : "Vacancies Come Alive",
                    laborBuiltRu
                        ? "\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430 \u0438\u0449\u0435\u0442 \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u044b\u0435 \u0441\u043b\u043e\u0442\u044b \u0432 \u0437\u0434\u0430\u043d\u0438\u044f\u0445 \u0438 \u043f\u043e\u0441\u0442\u0435\u043f\u0435\u043d\u043d\u043e \u0432\u044b\u0432\u0435\u0448\u0438\u0432\u0430\u0435\u0442 \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u0438.\n\n\u0415\u0441\u043b\u0438 \u0441\u0440\u0435\u0434\u0438 \u0441\u0442\u0430\u0440\u0442\u043e\u0432\u044b\u0445 \u0440\u0430\u0431\u043e\u0447\u0438\u0445 \u0435\u0441\u0442\u044c \u043f\u043e\u0434\u0445\u043e\u0434\u044f\u0449\u0438\u0439 \u0447\u0435\u043b\u043e\u0432\u0435\u043a \u0441 \u0432\u044b\u0441\u0448\u0438\u043c \u043e\u0431\u0440\u0430\u0437\u043e\u0432\u0430\u043d\u0438\u0435\u043c, \u043e\u043d \u043d\u0430\u0437\u043d\u0430\u0447\u0430\u0435\u0442\u0441\u044f \u0441\u044e\u0434\u0430 \u0430\u0432\u0442\u043e\u043c\u0430\u0442\u0438\u0447\u0435\u0441\u043a\u0438."
                        : "The Labor Exchange scans open building slots and gradually posts vacancies.\n\nIf a suitable higher-educated starting worker is available, they are assigned here automatically.");
                break;
            case TutorialTrigger.UserMigrationInfo:
                if (hasShownUserMigrationInfoTutorial)
                {
                    return;
                }

                hasShownUserMigrationInfoTutorial = true;
                bool migrationRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserMigrationInfo,
                    15,
                    migrationRu ? "\u041f\u0440\u0438\u0435\u0437\u0434 \u0440\u0430\u0431\u043e\u0447\u0438\u0445" : "Worker Arrivals",
                    migrationRu
                        ? "\u041a\u043e\u0433\u0434\u0430 \u0432 \u0433\u043e\u0440\u043e\u0434\u0435 \u0435\u0441\u0442\u044c \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u0438, \u0441\u044e\u0434\u0430 \u0447\u0430\u0449\u0435 \u043f\u0440\u0438\u0435\u0437\u0436\u0430\u044e\u0442 \u043d\u043e\u0432\u044b\u0435 \u0440\u0430\u0431\u043e\u0447\u0438\u0435.\n\n\u041e\u0442\u043a\u0440\u043e\u0439 \u0420\u0430\u0431\u043e\u0447\u0438\u0445: \u0442\u044b \u0443\u0432\u0438\u0434\u0438\u0448\u044c \u0438\u0445 \u0434\u0435\u043d\u044c\u0433\u0438, \u043d\u0443\u0436\u0434\u044b, \u043f\u0435\u0440\u043a\u0438, \u043e\u0431\u0440\u0430\u0437\u043e\u0432\u0430\u043d\u0438\u0435, \u0440\u0430\u0431\u043e\u0442\u0443 \u0438 \u0440\u0430\u0441\u043f\u043e\u0440\u044f\u0434\u043e\u043a."
                        : "When the town has vacancies, new workers are more likely to arrive.\n\nOpen Workers: each card shows money, needs, perks, education, job, and daily routine.");
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
                    17,
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
                    18,
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
                        ? "\u0411\u0430\u0440 \u0434\u0430\u0451\u0442 \u0440\u0430\u0431\u043e\u0447\u0438\u043c \u0434\u043e\u0441\u0443\u0433 \u0438 \u0437\u0430\u0431\u0438\u0440\u0430\u0435\u0442 \u0447\u0430\u0441\u0442\u044c \u0438\u0445 \u0437\u0430\u0440\u043f\u043b\u0430\u0442\u044b \u0432 \u043a\u0430\u0441\u0441\u0443 \u0437\u0434\u0430\u043d\u0438\u044f.\n\n\u0410\u043b\u043a\u043e\u0433\u043e\u043b\u044c \u0442\u0435\u043f\u0435\u0440\u044c \u043f\u0440\u043e\u0441\u0442\u043e \u0447\u0430\u0441\u0442\u044c \u0434\u043e\u0441\u0443\u0433\u0430 \u0431\u0435\u0437 \u0432\u0440\u0435\u043c\u0435\u043d\u043d\u044b\u0445 \u043c\u043e\u0434\u0438\u0444\u0438\u043a\u0430\u0442\u043e\u0440\u043e\u0432."
                        : "The Bar gives workers leisure and moves some of their wages into the building bank.\n\nIt works directly through service fees.");
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
                        ? "\u0421\u0442\u043e\u043b\u043e\u0432\u0430\u044f \u0437\u0430\u043a\u0440\u044b\u0432\u0430\u0435\u0442 \u043f\u043e\u0442\u0440\u0435\u0431\u043d\u043e\u0441\u0442\u044c \u0432 \u0435\u0434\u0435.\n\n\u041a\u043e\u0433\u0434\u0430 \u0440\u0430\u0431\u043e\u0447\u0438\u0439 \u0435\u0441\u0442, \u043e\u043d \u043f\u043b\u0430\u0442\u0438\u0442 \u0441\u0435\u0440\u0432\u0438\u0441\u043d\u044b\u0439 \u0441\u0431\u043e\u0440 \u0438 \u0441\u0431\u0440\u0430\u0441\u044b\u0432\u0430\u0435\u0442 \u043d\u0443\u0436\u0434\u0443 \u0432 \u0435\u0434\u0435."
                        : "The Canteen satisfies the Food need.\n\nWhen a worker eats, they pay a service fee and reset their Food need.");
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
                    24,
                    overviewRu ? "\u0420\u0430\u0431\u043e\u0447\u0438\u0435" : "Workers",
                    overviewRu
                        ? "\u0423 \u043a\u0430\u0436\u0434\u043e\u0433\u043e \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e \u0435\u0441\u0442\u044c \u0434\u0435\u043d\u044c\u0433\u0438, \u043d\u0443\u0436\u0434\u044b, \u043f\u0435\u0440\u043a\u0438, \u0440\u0430\u0431\u043e\u0442\u0430 \u0438 \u0434\u043d\u0435\u0432\u043d\u043e\u0439 \u0440\u0430\u0441\u043f\u043e\u0440\u044f\u0434\u043e\u043a.\n\n\u041d\u0443\u0436\u0434\u044b \u043f\u043e\u0434\u0441\u043a\u0430\u0436\u0443\u0442, \u0447\u0442\u043e \u0447\u0435\u043b\u043e\u0432\u0435\u043a\u0443 \u0441\u0435\u0439\u0447\u0430\u0441 \u043d\u0443\u0436\u043d\u043e: \u0435\u0434\u0430, \u0441\u043e\u043d \u0438\u043b\u0438 \u0434\u043e\u0441\u0443\u0433.\n\n\u041e\u0442\u043a\u0440\u043e\u0439 \u0420\u0430\u0431\u043e\u0447\u0438\u0445 \u0438 \u043f\u043e\u0441\u043c\u043e\u0442\u0440\u0438 \u043a\u0430\u0440\u0442\u043e\u0447\u043a\u0443: \u043d\u043e\u0432\u044b\u0435 \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u0442\u0435\u043f\u0435\u0440\u044c \u043f\u0440\u0438\u0435\u0437\u0436\u0430\u044e\u0442 \u0441\u0430\u043c\u0438."
                        : "Each worker has money, needs, perks, a job, and a daily routine.\n\nNeeds tell you what the person currently requires: food, sleep, or leisure.\n\nOpen Workers and inspect a worker card: new workers now arrive automatically.");
                break;
            case TutorialTrigger.UserWorkerHiringBusInfo:
                if (hasShownUserWorkerHiringBusTutorial) return;
                hasShownUserWorkerHiringBusTutorial = true;
                FocusCameraOnHiringBusForTutorial();
                bool hireBusRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserWorkerHiringBusInfo,
                    16,
                    hireBusRu ? "\u041d\u043e\u0432\u044b\u0435 \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u0432 \u043f\u0443\u0442\u0438" : "New Workers En Route",
                    hireBusRu
                        ? "\u041d\u043e\u0432\u044b\u0435 \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u043f\u0440\u0438\u0435\u0437\u0436\u0430\u044e\u0442 \u0438\u0437\u0432\u043d\u0435 \u043d\u0430 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0435. \u041e\u043d\u0438 \u0432\u044b\u0439\u0434\u0443\u0442 \u0443 \u043c\u0435\u0436\u0434\u0443\u0433\u043e\u0440\u043e\u0434\u043d\u0435\u0439 \u043e\u0441\u0442\u0430\u043d\u043e\u0432\u043a\u0438 \u0438 \u043f\u043e\u0439\u0434\u0443\u0442 \u043a \u041c\u043e\u0442\u0435\u043b\u044e.\n\n\u0412 \u044d\u0442\u043e\u043c \u043e\u0431\u0443\u0447\u0430\u044e\u0449\u0435\u043c \u0437\u0430\u0435\u0437\u0434\u0435 \u0430\u0432\u0442\u043e\u0431\u0443\u0441 \u043f\u0440\u0438\u0432\u0435\u0437\u0451\u0442 \u0441\u0440\u0430\u0437\u0443 10 \u0440\u0430\u0431\u043e\u0447\u0438\u0445, \u0447\u0442\u043e\u0431\u044b \u0433\u043e\u0440\u043e\u0434 \u0431\u044b\u0441\u0442\u0440\u0435\u0435 \u043e\u0436\u0438\u043b."
                        : "New workers arrive from outside by bus. They get off at the intercity stop and walk to the Motel.\n\nFor this tutorial run, the bus brings 10 workers at once so the town can become busy faster.");
                break;
            case TutorialTrigger.UserWarehouseLoadersInfo:
                if (hasShownUserWarehouseLoadersTutorial) return;
                hasShownUserWarehouseLoadersTutorial = true;
                bool warehouseLoadersRu = IsRussianLanguage();
                ShowTutorialWindow(
                    TutorialTrigger.UserWarehouseLoadersInfo,
                    25,
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
            case TutorialTrigger.UserEconomyTaxesInfo:
                ShowUserEconomyTaxesTutorial();
                break;
            case TutorialTrigger.UserTradeIntroInfo:
                ShowUserTradeIntroTutorial();
                break;
            case TutorialTrigger.UserTradeRouteInfo:
                ShowUserTradeRouteTutorial();
                break;
            case TutorialTrigger.UserDocksPrompt:
                ShowUserDocksTutorial();
                break;
            case TutorialTrigger.UserDocksBuiltInfo:
                ShowUserDocksBuiltTutorial();
                break;
            case TutorialTrigger.UserTradePolicyInfo:
                ShowUserTradePolicyTutorial();
                break;
            case TutorialTrigger.UserDemoCompleteInfo:
                ShowUserDemoCompleteTutorial();
                break;
        }
    }

    private void NotifyTutorialLumberjackCampBuilt()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
        {
            return;
        }

        MarkTutorialGoalComplete(TutorialGoalKind.BuildLumberjackCamp);
        TryShowTutorial(TutorialTrigger.UserLumberjackCampBuiltInfo);
        SessionDebugLogger.Log("TUTORIAL", "Lumberjack Camp build tutorial notified.");
    }

    private void NotifyTutorialLumberjackWorkerAssigned()
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
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
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
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
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
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
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped)
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
            case LocationType.LaborExchange:
                goal = TutorialGoalKind.BuildLaborExchange;
                trigger = TutorialTrigger.UserLaborExchangeBuiltInfo;
                break;
            default:
                return;
        }

        MarkTutorialGoalComplete(goal);
        TryShowTutorial(trigger);
        SessionDebugLogger.Log("TUTORIAL", $"Service building tutorial notified: {type}.");
    }

}
