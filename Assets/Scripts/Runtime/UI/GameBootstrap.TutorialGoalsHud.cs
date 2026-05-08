using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private enum TutorialGoalKind
    {
        CameraZoomIn,
        CameraZoomOut,
        CameraPan,
        CameraRotate,
        RoadSingleCell,
        RoadShiftPath,
        BuildWarehouse,
        BuildMotel,
        BuildParking,
        BuildLumberjackCamp,
        AssignLumberjackWorker,
        BuyFirstTruck,
        AssignTruckDriverShift,
        BuildLaborExchange,
        StaffLaborExchange,
        BuildBar,
        BuildGamblingHall,
        BuildCanteen,
        BuildGasStation,
        BuildCityPark,
        OpenWorkersCard,
        WaitForWorkerArrival,
        AssignWarehouseLoaders,
        BuildLocalBusStops,
        AssignBusDrivers,
        SetTaxRate15,
        OpenRegionalMap,
        BuildTradeRoute,
        BuildDocks,
        AssignDocksWorker,
        CreateBuyTextileOrder,
        ReceiveImportedCargo
    }

    private enum TutorialGoalsMode
    {
        CameraControls,
        RoadBuilding,
        CoreBuildings,
        LumberjackCamp,
        BuyTruck,
        LaborExchange,
        ServiceBuildings,
        WorkerCard,
        WarehouseLoaders,
        LocalTransport,
        EconomyTaxes,
        RegionalMap,
        Docks,
        TradeSetup
    }

    private sealed class TutorialGoalRowUi
    {
        public GameObject Root;
        public TutorialGoalKind Kind;
        public Image CheckBox;
        public Text CheckMarkText;
        public Text LabelText;
    }

    private sealed class TutorialGoalsHudRefs
    {
        public GameObject CanvasRoot;
        public RectTransform PanelRoot;
        public Vector2 OriginalPosition;
        public Text TitleText;
        public Text SubtitleText;
        public Image FlashOverlay;
        public readonly List<TutorialGoalRowUi> Rows = new();
    }

    private TutorialGoalsHudRefs tutorialGoalsHud;
    private readonly HashSet<TutorialGoalKind> activeTutorialGoals = new();
    private readonly HashSet<TutorialGoalKind> completedTutorialGoals = new();
    private bool isTutorialGoalsActive;
    private bool isTutorialGoalsComplete;
    private float tutorialGoalsSuccessTimer;
    private float tutorialGoalsSuccessDuration;
    private bool shouldShowRoadTutorialAfterCameraGoals;
    private bool shouldShowCoreBuildingsTutorialAfterRoadGoals;
    private bool shouldShowLumberjackCampTutorialAfterCoreGoals;
    private bool shouldShowWorkerShiftTutorialAfterLumberjackGoals;
    private bool shouldShowTruckFreightTutorialAfterBuyTruckGoals;
    private bool shouldShowMigrationAfterLaborExchangeGoals;
    private bool shouldShowServiceBuildingsAfterWorkerArrivalGoals;
    private bool shouldShowWorkersOverviewAfterServiceGoals;
    private bool shouldShowLocalTransportAfterWarehouseLoadersGoals;
    private bool shouldShowLocalBusRoutesAfterTransportGoals;
    private bool shouldShowRegionalMapAfterEconomyGoals;
    private bool shouldShowDocksAfterRouteGoals;
    private bool shouldShowTradePolicyAfterDocksGoals;
    private bool shouldShowDemoCompleteAfterTradeGoals;
    private TutorialGoalsMode tutorialGoalsMode;

    private void ResetTutorialGoalsForNewGame()
    {
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        isTutorialGoalsActive = false;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        shouldShowRoadTutorialAfterCameraGoals = false;
        shouldShowCoreBuildingsTutorialAfterRoadGoals = false;
        shouldShowLumberjackCampTutorialAfterCoreGoals = false;
        shouldShowWorkerShiftTutorialAfterLumberjackGoals = false;
        shouldShowTruckFreightTutorialAfterBuyTruckGoals = false;
        shouldShowMigrationAfterLaborExchangeGoals = false;
        shouldShowServiceBuildingsAfterWorkerArrivalGoals = false;
        shouldShowWorkersOverviewAfterServiceGoals = false;
        shouldShowLocalTransportAfterWarehouseLoadersGoals = false;
        shouldShowLocalBusRoutesAfterTransportGoals = false;
        shouldShowRegionalMapAfterEconomyGoals = false;
        shouldShowDocksAfterRouteGoals = false;
        shouldShowTradePolicyAfterDocksGoals = false;
        shouldShowDemoCompleteAfterTradeGoals = false;
        tutorialGoalsMode = TutorialGoalsMode.CameraControls;
        if (tutorialGoalsHud?.CanvasRoot != null)
        {
            tutorialGoalsHud.CanvasRoot.SetActive(false);
        }
    }

    private void BeginCameraControlTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.CameraZoomIn);
        activeTutorialGoals.Add(TutorialGoalKind.CameraZoomOut);
        activeTutorialGoals.Add(TutorialGoalKind.CameraPan);
        activeTutorialGoals.Add(TutorialGoalKind.CameraRotate);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        shouldShowRoadTutorialAfterCameraGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        shouldShowCoreBuildingsTutorialAfterRoadGoals = false;
        shouldShowLumberjackCampTutorialAfterCoreGoals = false;
        shouldShowWorkerShiftTutorialAfterLumberjackGoals = false;
        tutorialGoalsMode = TutorialGoalsMode.CameraControls;
        ShowTutorialGoalsHud();
        SessionDebugLogger.Log("TUTORIAL", "Camera control goals started.");
    }

    private void BeginRoadBuildTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.RoadSingleCell);
        activeTutorialGoals.Add(TutorialGoalKind.RoadShiftPath);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        shouldShowRoadTutorialAfterCameraGoals = false;
        shouldShowCoreBuildingsTutorialAfterRoadGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        shouldShowLumberjackCampTutorialAfterCoreGoals = false;
        shouldShowWorkerShiftTutorialAfterLumberjackGoals = false;
        tutorialGoalsMode = TutorialGoalsMode.RoadBuilding;
        ShowTutorialGoalsHud();
        SessionDebugLogger.Log("TUTORIAL", "Road build goals started.");
    }

    private void BeginCoreBuildingsTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.BuildWarehouse);
        activeTutorialGoals.Add(TutorialGoalKind.BuildMotel);
        activeTutorialGoals.Add(TutorialGoalKind.BuildParking);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        shouldShowRoadTutorialAfterCameraGoals = false;
        shouldShowCoreBuildingsTutorialAfterRoadGoals = false;
        shouldShowLumberjackCampTutorialAfterCoreGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        shouldShowWorkerShiftTutorialAfterLumberjackGoals = false;
        tutorialGoalsMode = TutorialGoalsMode.CoreBuildings;
        ShowTutorialGoalsHud();
        SessionDebugLogger.Log("TUTORIAL", "Core building goals started.");
    }

    private void BeginLumberjackCampTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.BuildLumberjackCamp);
        activeTutorialGoals.Add(TutorialGoalKind.AssignLumberjackWorker);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        shouldShowRoadTutorialAfterCameraGoals = false;
        shouldShowCoreBuildingsTutorialAfterRoadGoals = false;
        shouldShowLumberjackCampTutorialAfterCoreGoals = false;
        shouldShowWorkerShiftTutorialAfterLumberjackGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        tutorialGoalsMode = TutorialGoalsMode.LumberjackCamp;
        ShowTutorialGoalsHud();
        SessionDebugLogger.Log("TUTORIAL", "Lumberjack Camp goals started.");
    }

    private void BeginBuyTruckTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.AssignTruckDriverShift);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        shouldShowRoadTutorialAfterCameraGoals = false;
        shouldShowCoreBuildingsTutorialAfterRoadGoals = false;
        shouldShowLumberjackCampTutorialAfterCoreGoals = false;
        shouldShowWorkerShiftTutorialAfterLumberjackGoals = false;
        shouldShowTruckFreightTutorialAfterBuyTruckGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        tutorialGoalsMode = TutorialGoalsMode.BuyTruck;
        ShowTutorialGoalsHud();
        SessionDebugLogger.Log("TUTORIAL", "Truck logistics goals started.");
    }

    private void BeginLaborExchangeTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.BuildLaborExchange);
        activeTutorialGoals.Add(TutorialGoalKind.StaffLaborExchange);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        tutorialGoalsMode = TutorialGoalsMode.LaborExchange;
        shouldShowMigrationAfterLaborExchangeGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        ShowTutorialGoalsHud();
        CheckTutorialLaborExchangeGoal();
        SessionDebugLogger.Log("TUTORIAL", "Labor Exchange goals started.");
    }

    private void BeginServiceBuildingsTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.BuildBar);
        activeTutorialGoals.Add(TutorialGoalKind.BuildGamblingHall);
        activeTutorialGoals.Add(TutorialGoalKind.BuildCanteen);
        activeTutorialGoals.Add(TutorialGoalKind.BuildGasStation);
        activeTutorialGoals.Add(TutorialGoalKind.BuildCityPark);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        shouldShowWorkersOverviewAfterServiceGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        tutorialGoalsMode = TutorialGoalsMode.ServiceBuildings;
        ShowTutorialGoalsHud();
        SessionDebugLogger.Log("TUTORIAL", "Service building goals started.");
    }

    private void BeginWorkerCardTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.OpenWorkersCard);
        activeTutorialGoals.Add(TutorialGoalKind.WaitForWorkerArrival);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        tutorialGoalsMode = TutorialGoalsMode.WorkerCard;
        shouldShowServiceBuildingsAfterWorkerArrivalGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        ShowTutorialGoalsHud();
        if (isDriversPanelOpen)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.OpenWorkersCard);
        }

        if (hasMotelBootstrapWorkerWaveDisembarked)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.WaitForWorkerArrival);
        }

        SessionDebugLogger.Log("TUTORIAL", "Worker card and hiring goals started.");
    }

    private void BeginWarehouseLoadersTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.AssignWarehouseLoaders);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        tutorialGoalsMode = TutorialGoalsMode.WarehouseLoaders;
        shouldShowLocalTransportAfterWarehouseLoadersGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        ShowTutorialGoalsHud();
        CheckTutorialWarehouseLoaderGoal();
        SessionDebugLogger.Log("TUTORIAL", "Warehouse loader goals started.");
    }

    private void BeginLocalTransportTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.BuildLocalBusStops);
        activeTutorialGoals.Add(TutorialGoalKind.AssignBusDrivers);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        tutorialGoalsMode = TutorialGoalsMode.LocalTransport;
        shouldShowLocalBusRoutesAfterTransportGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        ShowTutorialGoalsHud();
        CheckTutorialLocalTransportGoals();
        SessionDebugLogger.Log("TUTORIAL", "Local transport goals started.");
    }

    private void BeginEconomyTaxesTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.SetTaxRate15);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        tutorialGoalsMode = TutorialGoalsMode.EconomyTaxes;
        shouldShowRegionalMapAfterEconomyGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        ShowTutorialGoalsHud();
        CheckTutorialTaxRateGoal();
        SessionDebugLogger.Log("TUTORIAL", "Economy tax goals started.");
    }

    private void BeginTradeSetupTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.CreateBuyTextileOrder);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        tutorialGoalsMode = TutorialGoalsMode.TradeSetup;
        shouldShowDemoCompleteAfterTradeGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        ShowTutorialGoalsHud();
        CheckTutorialTradeSetupGoals();
        SessionDebugLogger.Log("TUTORIAL", "Trade setup goals started.");
    }

    private void BeginRegionalMapTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.OpenRegionalMap);
        activeTutorialGoals.Add(TutorialGoalKind.BuildTradeRoute);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        tutorialGoalsMode = TutorialGoalsMode.RegionalMap;
        shouldShowDocksAfterRouteGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        ShowTutorialGoalsHud();
        CheckTutorialRegionalMapGoal();
        SessionDebugLogger.Log("TUTORIAL", "Regional map goals started.");
    }

    private void BeginDocksTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.BuildDocks);
        activeTutorialGoals.Add(TutorialGoalKind.AssignDocksWorker);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        tutorialGoalsMode = TutorialGoalsMode.Docks;
        shouldShowTradePolicyAfterDocksGoals = selectedGameStartMode == GameStartMode.Tutorial && !isTutorialSkipped;
        ShowTutorialGoalsHud();
        CheckTutorialDocksGoal();
        SessionDebugLogger.Log("TUTORIAL", "Docks goals started.");
    }

    private void ClearTutorialGoalContinuationFlags()
    {
        shouldShowRoadTutorialAfterCameraGoals = false;
        shouldShowCoreBuildingsTutorialAfterRoadGoals = false;
        shouldShowLumberjackCampTutorialAfterCoreGoals = false;
        shouldShowWorkerShiftTutorialAfterLumberjackGoals = false;
        shouldShowTruckFreightTutorialAfterBuyTruckGoals = false;
        shouldShowMigrationAfterLaborExchangeGoals = false;
        shouldShowServiceBuildingsAfterWorkerArrivalGoals = false;
        shouldShowWorkersOverviewAfterServiceGoals = false;
        shouldShowLocalTransportAfterWarehouseLoadersGoals = false;
        shouldShowLocalBusRoutesAfterTransportGoals = false;
        shouldShowRegionalMapAfterEconomyGoals = false;
        shouldShowDocksAfterRouteGoals = false;
        shouldShowTradePolicyAfterDocksGoals = false;
        shouldShowDemoCompleteAfterTradeGoals = false;
    }

    private void ShowTutorialGoalsHud()
    {
        if (tutorialGoalsHud?.CanvasRoot != null)
        {
            tutorialGoalsHud.CanvasRoot.SetActive(true);
            tutorialGoalsHud.PanelRoot.anchoredPosition = tutorialGoalsHud.OriginalPosition;
            tutorialGoalsHud.PanelRoot.localScale = Vector3.one;
        }

        UpdateTutorialGoalsLocalization();
        UpdateTutorialGoalsUi();
    }

    private void MarkTutorialGoalComplete(TutorialGoalKind kind)
    {
        if (!isTutorialGoalsActive || isTutorialGoalsComplete)
        {
            return;
        }

        if (!activeTutorialGoals.Contains(kind))
        {
            SessionDebugLogger.Log("TUTORIAL", $"Ignored goal from inactive objective set: {kind} while mode={tutorialGoalsMode}.");
            return;
        }

        if (!completedTutorialGoals.Add(kind))
        {
            return;
        }

        SessionDebugLogger.Log("TUTORIAL", $"Goal completed: {kind}.");
        UpdateTutorialGoalsUi();

        if (GetActiveCompletedGoalCount() >= activeTutorialGoals.Count)
        {
            CompleteTutorialGoals();
        }
    }

    private void CompleteTutorialGoals()
    {
        isTutorialGoalsComplete = true;
        tutorialGoalsSuccessDuration = tutorialGoalsSuccessTimer = 2.2f;
        if (tutorialGoalsHud?.FlashOverlay != null)
        {
            tutorialGoalsHud.FlashOverlay.gameObject.SetActive(true);
        }
        PlayUiSound(tutorialGoalSuccessClip, 0.7f);
        SessionDebugLogger.Log("TUTORIAL", $"Tutorial goals completed. mode={tutorialGoalsMode}.");
    }

    private void EnsureTutorialGoalsHud()
    {
        if (tutorialGoalsHud != null)
        {
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tutorialGoalsHud = new TutorialGoalsHudRefs();

        GameObject canvasObject = new("TutorialGoalsCanvas", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 3;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        tutorialGoalsHud.CanvasRoot = canvasObject;

        RectTransform panel = FleetCanvasUiFactory.CreateStyledPanel("GoalsPanel", canvasObject.transform, new Color(0.08f, 0.11f, 0.16f, 0.86f));
        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 1f);
        panel.sizeDelta = new Vector2(390f, 304f);
        panel.anchoredPosition = new Vector2(12f, -58f);
        tutorialGoalsHud.PanelRoot = panel;
        tutorialGoalsHud.OriginalPosition = panel.anchoredPosition;
        SetGraphicRaycast(panel.gameObject, false);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 14, 14);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        tutorialGoalsHud.TitleText = CreateGoalText("Title", panel, font, 19, FontStyle.Bold, new Color(1f, 0.86f, 0.32f), TextAnchor.MiddleLeft);
        tutorialGoalsHud.TitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        tutorialGoalsHud.SubtitleText = CreateGoalText("Subtitle", panel, font, 12, FontStyle.Normal, new Color(0.78f, 0.84f, 0.92f), TextAnchor.MiddleLeft);
        tutorialGoalsHud.SubtitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

        CreateTutorialGoalRow(panel, font, TutorialGoalKind.CameraZoomIn);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.CameraZoomOut);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.CameraPan);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.CameraRotate);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.RoadSingleCell);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.RoadShiftPath);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildWarehouse);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildMotel);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildParking);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildLumberjackCamp);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.AssignLumberjackWorker);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuyFirstTruck);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.AssignTruckDriverShift);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildLaborExchange);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.StaffLaborExchange);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildBar);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildGamblingHall);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildCanteen);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildGasStation);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildCityPark);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.OpenWorkersCard);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.WaitForWorkerArrival);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.AssignWarehouseLoaders);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildLocalBusStops);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.AssignBusDrivers);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.SetTaxRate15);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.OpenRegionalMap);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildTradeRoute);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildDocks);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.AssignDocksWorker);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.CreateBuyTextileOrder);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.ReceiveImportedCargo);

        RectTransform flash = FleetCanvasUiFactory.CreateUiObject("SuccessFlash", panel).GetComponent<RectTransform>();
        flash.anchorMin = Vector2.zero;
        flash.anchorMax = Vector2.one;
        flash.offsetMin = Vector2.zero;
        flash.offsetMax = Vector2.zero;
        Image flashImage = flash.gameObject.AddComponent<Image>();
        flashImage.color = new Color(0f, 0f, 0f, 0f);
        flashImage.raycastTarget = false;
        flash.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        flashImage.gameObject.SetActive(false);
        tutorialGoalsHud.FlashOverlay = flashImage;
        flash.SetAsLastSibling();

        canvasObject.SetActive(false);
    }

    private Text CreateGoalText(string name, Transform parent, Font font, int size, FontStyle style, Color color, TextAnchor alignment)
    {
        Text text = FleetCanvasUiFactory.CreateBodyText(name, parent, font, string.Empty, size, alignment, color, null);
        text.fontStyle = style;
        text.raycastTarget = false;
        return text;
    }

    private void CreateTutorialGoalRow(RectTransform parent, Font font, TutorialGoalKind kind)
    {
        RectTransform row = FleetCanvasUiFactory.CreateLayoutRow(kind.ToString(), parent, 30f, 9f);
        SetGraphicRaycast(row.gameObject, false);

        GameObject boxObject = FleetCanvasUiFactory.CreateUiObject("CheckBox", row);
        Image box = boxObject.AddComponent<Image>();
        box.color = new Color(0.03f, 0.05f, 0.08f, 0.95f);
        box.raycastTarget = false;
        Outline outline = boxObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.78f, 0.25f, 0.62f);
        outline.effectDistance = new Vector2(1f, -1f);
        LayoutElement boxLayout = boxObject.AddComponent<LayoutElement>();
        boxLayout.preferredWidth = 22f;
        boxLayout.preferredHeight = 22f;

        Text mark = CreateGoalText("CheckMark", boxObject.transform, font, 15, FontStyle.Bold, new Color(1f, 0.88f, 0.32f), TextAnchor.MiddleCenter);
        RectTransform markRect = mark.rectTransform;
        markRect.anchorMin = Vector2.zero;
        markRect.anchorMax = Vector2.one;
        markRect.offsetMin = Vector2.zero;
        markRect.offsetMax = Vector2.zero;

        Text label = CreateGoalText("Label", row, font, 12, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        tutorialGoalsHud.Rows.Add(new TutorialGoalRowUi
        {
            Root = row.gameObject,
            Kind = kind,
            CheckBox = box,
            CheckMarkText = mark,
            LabelText = label
        });
    }

    private void UpdateTutorialGoalsLocalization()
    {
        if (tutorialGoalsHud == null)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        tutorialGoalsHud.TitleText.text = ru ? "Цели" : "Goals";
        tutorialGoalsHud.SubtitleText.text = tutorialGoalsMode switch
        {
            TutorialGoalsMode.RoadBuilding => ru ? "Построй дорогу двумя способами." : "Build roads in two ways.",
            TutorialGoalsMode.CoreBuildings => ru ? "Построй базовые здания города." : "Build the town core buildings.",
            TutorialGoalsMode.LumberjackCamp => ru ? "Запусти первую добычу дерева." : "Start your first logging production.",
            TutorialGoalsMode.BuyTruck => ru ? "Назначь первого водителя грузовика." : "Assign the first truck driver.",
            _ => ru ? "Освой камеру перед строительством." : "Learn the camera before building."
        };

        tutorialGoalsHud.TitleText.text = ru ? "\u0426\u0435\u043b\u0438" : "Goals";
        tutorialGoalsHud.SubtitleText.text = tutorialGoalsMode switch
        {
            TutorialGoalsMode.RoadBuilding => ru ? "Открой Стройку (B), выбери дорогу и поставь её двумя способами." : "Open Build (B), choose a road, and place it in two ways.",
            TutorialGoalsMode.CoreBuildings => ru ? "Открой Стройку (B) и поставь три базовых здания." : "Open Build (B) and place the three core buildings.",
            TutorialGoalsMode.LumberjackCamp => ru ? "Открой Стройку (B), поставь лагерь, затем открой Кадры." : "Open Build (B), place the camp, then open Staffing.",
            TutorialGoalsMode.BuyTruck => ru ? "Открой Кадры: выбери смену водителя. Parking выдаст грузовик автоматически." : "Open Staffing: choose a driver shift. Parking will provide the truck automatically.",
            TutorialGoalsMode.ServiceBuildings => ru ? "Открой Стройку (B) и поставь сервисные здания." : "Open Build (B) and place the service buildings.",
            TutorialGoalsMode.WorkerCard => ru ? "\u041e\u0442\u043a\u0440\u043e\u0439 \u0420\u0430\u0431\u043e\u0447\u0438\u0435 \u0438 \u043f\u043e\u0441\u043c\u043e\u0442\u0440\u0438 \u043a\u0430\u0440\u0442\u043e\u0447\u043a\u0443: \u043f\u0440\u0438\u0435\u0437\u0434 \u0440\u0430\u0431\u043e\u0447\u0438\u0445 \u0437\u0430\u043f\u0443\u0441\u0442\u0438\u0442\u0441\u044f \u0441\u0430\u043c." : "Open Workers and inspect a card: worker arrivals start automatically.",
            TutorialGoalsMode.WarehouseLoaders => ru ? "Открой Биржу труда и проверь три складских слота." : "Open the Labor Exchange and review the three Warehouse slots.",
            TutorialGoalsMode.LocalTransport => ru ? "Открой Стройку для остановок, затем Биржу труда для водителей." : "Open Build for stops, then the Labor Exchange for drivers.",
            TutorialGoalsMode.EconomyTaxes => ru ? "Открой Экономика -> Налоги и нажимай + до 15%." : "Open Economy -> Taxes and press + until 15%.",
            TutorialGoalsMode.TradeSetup => ru ? "Открой Торговлю и выставь политику покупки ткани." : "Open Trade and set a Textile buy policy.",
            TutorialGoalsMode.RegionalMap => ru ? "\u041e\u0442\u043a\u0440\u043e\u0439 \u041a\u0430\u0440\u0442\u0443 \u0438 \u043f\u0440\u043e\u043b\u043e\u0436\u0438 \u0440\u0435\u0447\u043d\u043e\u0439 \u043c\u0430\u0440\u0448\u0440\u0443\u0442." : "Open the Map and build a river trade route.",
            TutorialGoalsMode.Docks => ru ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u0414\u043e\u043a\u0438 \u0438 \u043d\u0430\u0437\u043d\u0430\u0447\u044c \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e." : "Build Docks and assign a worker.",
            _ => ru ? "\u041e\u0441\u0432\u043e\u0439 \u043a\u0430\u043c\u0435\u0440\u0443 \u043f\u0435\u0440\u0435\u0434 \u0441\u0442\u0440\u043e\u0439\u043a\u043e\u0439." : "Learn the camera before building."
        };

        foreach (TutorialGoalRowUi row in tutorialGoalsHud.Rows)
        {
            row.Root.SetActive(IsTutorialGoalVisible(row.Kind));
            row.LabelText.text = GetTutorialGoalLabelSafe(row.Kind, ru);
        }
    }

    private static string GetTutorialGoalLabelSafe(TutorialGoalKind kind, bool ru)
    {
        return kind switch
        {
            TutorialGoalKind.CameraZoomIn => ru ? "Zoom In: \u043f\u0440\u0438\u0431\u043b\u0438\u0437\u044c \u043a\u0430\u043c\u0435\u0440\u0443" : "Zoom In: move camera closer",
            TutorialGoalKind.CameraZoomOut => ru ? "Zoom Out: \u043e\u0442\u0434\u0430\u043b\u0438 \u043a\u0430\u043c\u0435\u0440\u0443" : "Zoom Out: move camera away",
            TutorialGoalKind.CameraPan => ru ? "Scroll \u043a\u0430\u0440\u0442\u044b: \u0441\u0434\u0432\u0438\u043d\u044c \u043e\u0431\u0437\u043e\u0440" : "Map scroll: pan the view",
            TutorialGoalKind.CameraRotate => ru ? "\u041f\u043e\u0432\u043e\u0440\u043e\u0442 \u043a\u0430\u0440\u0442\u044b: Q / E" : "Rotate map: Q / E",
            TutorialGoalKind.RoadSingleCell => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> \u0414\u043e\u0440\u043e\u0433\u0430: \u043f\u043e\u0441\u0442\u0430\u0432\u044c 1 \u043a\u043b\u0435\u0442\u043a\u0443 \u041b\u041a\u041c" : "Build (B) -> Road: place 1 cell with left click",
            TutorialGoalKind.RoadShiftPath => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> \u0414\u043e\u0440\u043e\u0433\u0430: \u043f\u0440\u043e\u0442\u044f\u043d\u0438 Shift + \u041b\u041a\u041c" : "Build (B) -> Road: drag with Shift + left click",
            TutorialGoalKind.BuildWarehouse => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> \u0421\u043a\u043b\u0430\u0434: \u0432\u044b\u0431\u0435\u0440\u0438 \u0438 \u043f\u043e\u0441\u0442\u0430\u0432\u044c" : "Build (B) -> Warehouse: select and place",
            TutorialGoalKind.BuildMotel => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> \u041c\u043e\u0442\u0435\u043b\u044c: \u0432\u044b\u0431\u0435\u0440\u0438 \u0438 \u043f\u043e\u0441\u0442\u0430\u0432\u044c" : "Build (B) -> Motel: select and place",
            TutorialGoalKind.BuildParking => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> \u041f\u0430\u0440\u043a\u043e\u0432\u043a\u0430: \u0432\u044b\u0431\u0435\u0440\u0438 \u0438 \u043f\u043e\u0441\u0442\u0430\u0432\u044c" : "Build (B) -> Parking: select and place",
            TutorialGoalKind.BuildLumberjackCamp => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> \u041b\u0430\u0433\u0435\u0440\u044c \u043b\u0435\u0441\u043e\u0440\u0443\u0431\u043e\u0432: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u0443 \u043b\u0435\u0441\u0430" : "Build (B) -> Lumberjack Camp: place near forest",
            TutorialGoalKind.AssignLumberjackWorker => ru ? "\u041a\u0430\u0434\u0440\u044b -> \u041b\u0435\u0441\u043e\u0437\u0430\u0433\u043e\u0442\u043e\u0432\u043a\u0430 -> \u0421\u043c\u0435\u043d\u0430 -> \u0420\u0430\u0431\u043e\u0447\u0438\u0439" : "Staffing -> Logging -> Shift -> Worker",
            TutorialGoalKind.BuyFirstTruck => ru ? "Parking -> \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u044b\u0439 \u0441\u043b\u043e\u0442 \u0430\u0432\u0442\u043e\u043f\u0430\u0440\u043a\u0430" : "Parking -> free fleet slot",
            TutorialGoalKind.AssignTruckDriverShift => ru ? "\u041a\u0430\u0434\u0440\u044b -> \u0412\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0430 -> \u0421\u043c\u0435\u043d\u0430 -> \u0420\u0430\u0431\u043e\u0447\u0438\u0439" : "Staffing -> Truck Driver -> Shift -> Worker",
            TutorialGoalKind.BuildLaborExchange => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> \u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430: \u0432\u044b\u0431\u0435\u0440\u0438 \u0438 \u043f\u043e\u0441\u0442\u0430\u0432\u044c" : "Build (B) -> Labor Exchange: select and place",
            TutorialGoalKind.StaffLaborExchange => ru ? "\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430: \u043a\u043b\u0435\u0440\u043a \u043d\u0430 \u0441\u043c\u0435\u043d\u0435" : "Labor Exchange: clerk on shift",
            TutorialGoalKind.BuildBar => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> \u0411\u0430\u0440: \u0432\u044b\u0431\u0435\u0440\u0438 \u0438 \u043f\u043e\u0441\u0442\u0430\u0432\u044c" : "Build (B) -> Bar: select and place",
            TutorialGoalKind.BuildGamblingHall => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> \u0418\u0433\u0440\u043e\u0432\u044b\u0435 \u0430\u0432\u0442\u043e\u043c\u0430\u0442\u044b: \u0432\u044b\u0431\u0435\u0440\u0438 \u0438 \u043f\u043e\u0441\u0442\u0430\u0432\u044c" : "Build (B) -> Gambling Hall: select and place",
            TutorialGoalKind.BuildCanteen => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> \u0421\u0442\u043e\u043b\u043e\u0432\u0430\u044f: \u0432\u044b\u0431\u0435\u0440\u0438 \u0438 \u043f\u043e\u0441\u0442\u0430\u0432\u044c" : "Build (B) -> Canteen: select and place",
            TutorialGoalKind.BuildGasStation => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> \u0417\u0430\u043f\u0440\u0430\u0432\u043a\u0430: \u0432\u044b\u0431\u0435\u0440\u0438 \u0438 \u043f\u043e\u0441\u0442\u0430\u0432\u044c" : "Build (B) -> Gas Station: select and place",
            TutorialGoalKind.BuildCityPark => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> City Park: \u0432\u044b\u0431\u0435\u0440\u0438 \u0438 \u043f\u043e\u0441\u0442\u0430\u0432\u044c" : "Build (B) -> City Park: select and place",
            TutorialGoalKind.OpenWorkersCard => ru ? "\u0412\u0435\u0440\u0445\u043d\u0435\u0435 \u043c\u0435\u043d\u044e -> \u0420\u0430\u0431\u043e\u0447\u0438\u0435: \u043e\u0442\u043a\u0440\u043e\u0439 \u043a\u0430\u0440\u0442\u043e\u0447\u043a\u0443 \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e" : "Top menu -> Workers: open a worker card",
            TutorialGoalKind.WaitForWorkerArrival => ru ? "\u0420\u0430\u0431\u043e\u0447\u0438\u0435 -> \u0434\u043e\u0436\u0434\u0438\u0441\u044c \u0430\u0432\u0442\u043e-\u043f\u0440\u0438\u0435\u0437\u0434\u0430" : "Workers -> wait for auto-arrival",
            TutorialGoalKind.AssignWarehouseLoaders => ru ? "\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430 -> \u0421\u043a\u043b\u0430\u0434\u0441\u043a\u0438\u0435 \u0441\u043b\u043e\u0442\u044b -> override \u043f\u0440\u0438 \u043d\u0443\u0436\u0434\u0435" : "Labor Exchange -> Warehouse slots -> override if needed",
            TutorialGoalKind.BuildLocalBusStops => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> \u041e\u0441\u0442\u0430\u043d\u043e\u0432\u043a\u0430: \u043f\u043e\u0441\u0442\u0430\u0432\u044c 2" : "Build (B) -> Bus Stop: place 2",
            TutorialGoalKind.AssignBusDrivers => ru ? "\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430 -> \u0412\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0430 -> \u0441\u043c\u0435\u043d\u044b" : "Labor Exchange -> Bus Driver -> shifts",
            TutorialGoalKind.SetTaxRate15 => ru ? "\u042d\u043a\u043e\u043d\u043e\u043c\u0438\u043a\u0430 -> \u041d\u0430\u043b\u043e\u0433\u0438: \u043d\u0430\u0436\u0438\u043c\u0430\u0439 + \u0434\u043e 15%" : "Economy -> Taxes: press + until 15%",
            TutorialGoalKind.OpenRegionalMap => ru ? "\u041a\u0430\u0440\u0442\u0430: \u043e\u0442\u043a\u0440\u043e\u0439 \u044d\u043a\u0440\u0430\u043d \u0440\u0435\u0433\u0438\u043e\u043d\u043e\u0432" : "Map: open the regional screen",
            TutorialGoalKind.BuildTradeRoute => ru ? "\u041a\u0430\u0440\u0442\u0430 -> \u0420\u0435\u0447\u043d\u043e\u0439 \u0433\u043e\u0440\u043e\u0434 -> \u041f\u0440\u043e\u043b\u043e\u0436\u0438\u0442\u044c \u043c\u0430\u0440\u0448\u0440\u0443\u0442" : "Map -> river city -> Build trade route",
            TutorialGoalKind.BuildDocks => ru ? "\u0421\u0442\u0440\u043e\u0439\u043a\u0430 (B) -> Docks: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u043d\u0430 \u0431\u0435\u0440\u0435\u0433\u0443" : "Build (B) -> Docks: place on the river bank",
            TutorialGoalKind.AssignDocksWorker => ru ? "\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430 -> Docks -> \u0421\u043c\u0435\u043d\u0430 -> \u0420\u0430\u0431\u043e\u0447\u0438\u0439" : "Labor Exchange -> Docks -> Shift -> Worker",
            TutorialGoalKind.CreateBuyTextileOrder => ru ? "\u0422\u043e\u0440\u0433\u043e\u0432\u043b\u044f -> \u0422\u0435\u043a\u0441\u0442\u0438\u043b\u044c -> \u0414\u043e\u043a\u0443\u043f\u0438\u0442\u044c \u0434\u043e \u043d\u043e\u0440\u043c\u044b" : "Trade -> Textile -> Buy up to",
            TutorialGoalKind.ReceiveImportedCargo => ru ? "\u0414\u043e\u043a\u0438 -> \u0442\u043e\u0432\u0430\u0440 \u043a\u0443\u043f\u043b\u0435\u043d \u0438\u043b\u0438 \u043e\u0436\u0438\u0434\u0430\u0435\u0442 \u043a\u043e\u0440\u0430\u0431\u043b\u044c" : "Docks -> cargo bought or ship waiting",
            _ => string.Empty
        };
    }

    private int GetActiveCompletedGoalCount()
    {
        int count = 0;
        foreach (TutorialGoalKind kind in completedTutorialGoals)
        {
            if (activeTutorialGoals.Contains(kind))
            {
                count++;
            }
        }

        return count;
    }

    private bool IsTutorialGoalVisible(TutorialGoalKind kind)
    {
        return tutorialGoalsMode switch
        {
            TutorialGoalsMode.RoadBuilding => kind is TutorialGoalKind.RoadSingleCell or TutorialGoalKind.RoadShiftPath,
            TutorialGoalsMode.CoreBuildings => kind is TutorialGoalKind.BuildWarehouse or TutorialGoalKind.BuildMotel or TutorialGoalKind.BuildParking,
            TutorialGoalsMode.LumberjackCamp => kind is TutorialGoalKind.BuildLumberjackCamp or TutorialGoalKind.AssignLumberjackWorker,
            TutorialGoalsMode.BuyTruck => kind is TutorialGoalKind.AssignTruckDriverShift,
            TutorialGoalsMode.LaborExchange => kind is TutorialGoalKind.BuildLaborExchange or TutorialGoalKind.StaffLaborExchange,
            TutorialGoalsMode.ServiceBuildings => kind is TutorialGoalKind.BuildBar or TutorialGoalKind.BuildGamblingHall or TutorialGoalKind.BuildCanteen or TutorialGoalKind.BuildGasStation or TutorialGoalKind.BuildCityPark,
            TutorialGoalsMode.WorkerCard => kind is TutorialGoalKind.OpenWorkersCard or TutorialGoalKind.WaitForWorkerArrival,
            TutorialGoalsMode.WarehouseLoaders => kind is TutorialGoalKind.AssignWarehouseLoaders,
            TutorialGoalsMode.LocalTransport => kind is TutorialGoalKind.BuildLocalBusStops or TutorialGoalKind.AssignBusDrivers,
            TutorialGoalsMode.EconomyTaxes => kind is TutorialGoalKind.SetTaxRate15,
            TutorialGoalsMode.RegionalMap => kind is TutorialGoalKind.OpenRegionalMap or TutorialGoalKind.BuildTradeRoute,
            TutorialGoalsMode.Docks => kind is TutorialGoalKind.BuildDocks or TutorialGoalKind.AssignDocksWorker,
            TutorialGoalsMode.TradeSetup => kind is TutorialGoalKind.CreateBuyTextileOrder,
            _ => kind is TutorialGoalKind.CameraZoomIn or TutorialGoalKind.CameraZoomOut or TutorialGoalKind.CameraPan or TutorialGoalKind.CameraRotate
        };
    }

    private void UpdateTutorialGoalsUi()
    {
        if (tutorialGoalsHud == null)
        {
            return;
        }

        foreach (TutorialGoalRowUi row in tutorialGoalsHud.Rows)
        {
            row.Root.SetActive(IsTutorialGoalVisible(row.Kind));
            bool done = completedTutorialGoals.Contains(row.Kind);
            row.CheckMarkText.text = done ? "X" : string.Empty;
            row.CheckBox.color = done
                ? new Color(0.12f, 0.55f, 0.20f, 0.96f)
                : new Color(0.03f, 0.05f, 0.08f, 0.95f);
            row.LabelText.color = done
                ? new Color(0.65f, 1f, 0.72f, 1f)
                : Color.white;
        }
    }

    private void UpdateTutorialGoalsRuntime()
    {
        if (tutorialGoalsHud?.CanvasRoot == null || !tutorialGoalsHud.CanvasRoot.activeSelf)
        {
            return;
        }

        if (tutorialGoalsSuccessTimer <= 0f)
        {
            return;
        }

        tutorialGoalsSuccessTimer -= Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(tutorialGoalsSuccessTimer / Mathf.Max(tutorialGoalsSuccessDuration, 0.01f));
        float pulse = Mathf.Sin((1f - progress) * Mathf.PI * 5f) * 0.02f * progress;
        tutorialGoalsHud.PanelRoot.localScale = Vector3.one * (1f + pulse);
        tutorialGoalsHud.PanelRoot.anchoredPosition = tutorialGoalsHud.OriginalPosition + new Vector2(Mathf.Sin(tutorialGoalsSuccessTimer * 36f) * 3f * progress, 0f);
        tutorialGoalsHud.FlashOverlay.color = new Color(0.05f, 0.82f, 0.18f, 0.32f * progress);

        if (tutorialGoalsSuccessTimer <= 0f)
        {
            tutorialGoalsHud.PanelRoot.localScale = Vector3.one;
            tutorialGoalsHud.PanelRoot.anchoredPosition = tutorialGoalsHud.OriginalPosition;
            tutorialGoalsHud.FlashOverlay.gameObject.SetActive(false);
            tutorialGoalsHud.CanvasRoot.SetActive(false);
            isTutorialGoalsActive = false;
            if (shouldShowRoadTutorialAfterCameraGoals && !isTutorialSkipped)
            {
                shouldShowRoadTutorialAfterCameraGoals = false;
                ScheduleTutorial(TutorialTrigger.UserBuildRoadPrompt, 2f);
            }
            else if (shouldShowCoreBuildingsTutorialAfterRoadGoals && !isTutorialSkipped)
            {
                shouldShowCoreBuildingsTutorialAfterRoadGoals = false;
                ScheduleTutorial(TutorialTrigger.UserCoreBuildingsPrompt, 2f);
            }
            else if (shouldShowLumberjackCampTutorialAfterCoreGoals && !isTutorialSkipped)
            {
                shouldShowLumberjackCampTutorialAfterCoreGoals = false;
                ScheduleTutorial(TutorialTrigger.UserBuildLumberjackCampPrompt, 2f);
            }
            else if (shouldShowWorkerShiftTutorialAfterLumberjackGoals && !isTutorialSkipped)
            {
                shouldShowWorkerShiftTutorialAfterLumberjackGoals = false;
                ScheduleTutorial(TutorialTrigger.UserWorkerShiftInfo, 0.8f);
            }
            else if (shouldShowTruckFreightTutorialAfterBuyTruckGoals && !isTutorialSkipped)
            {
                shouldShowTruckFreightTutorialAfterBuyTruckGoals = false;
                ScheduleTutorial(TutorialTrigger.UserTruckAssignedFreightInfo, 0.35f);
            }
            else if (shouldShowMigrationAfterLaborExchangeGoals && !isTutorialSkipped)
            {
                shouldShowMigrationAfterLaborExchangeGoals = false;
                ScheduleTutorial(TutorialTrigger.UserMigrationInfo, 0.8f);
            }
            else if (shouldShowServiceBuildingsAfterWorkerArrivalGoals && !isTutorialSkipped)
            {
                shouldShowServiceBuildingsAfterWorkerArrivalGoals = false;
                ScheduleTutorial(TutorialTrigger.UserWorkersLeisureInfo, 0.8f);
            }
            else if (shouldShowWorkersOverviewAfterServiceGoals && !isTutorialSkipped)
            {
                shouldShowWorkersOverviewAfterServiceGoals = false;
                ScheduleTutorial(TutorialTrigger.UserWorkersOverviewInfo, 0.8f);
            }
            else if (shouldShowLocalTransportAfterWarehouseLoadersGoals && !isTutorialSkipped)
            {
                shouldShowLocalTransportAfterWarehouseLoadersGoals = false;
                ScheduleTutorial(TutorialTrigger.UserLocalTransportInfo, 0.8f);
            }
            else if (shouldShowLocalBusRoutesAfterTransportGoals && !isTutorialSkipped)
            {
                shouldShowLocalBusRoutesAfterTransportGoals = false;
                ScheduleTutorial(TutorialTrigger.UserLocalBusRoutesInfo, 0.8f);
            }
            else if (shouldShowRegionalMapAfterEconomyGoals && !isTutorialSkipped)
            {
                shouldShowRegionalMapAfterEconomyGoals = false;
                ScheduleTutorial(TutorialTrigger.UserTradeIntroInfo, 0.8f);
            }
            else if (shouldShowDocksAfterRouteGoals && !isTutorialSkipped)
            {
                shouldShowDocksAfterRouteGoals = false;
                ScheduleTutorial(TutorialTrigger.UserDocksPrompt, 0.8f);
            }
            else if (shouldShowTradePolicyAfterDocksGoals && !isTutorialSkipped)
            {
                shouldShowTradePolicyAfterDocksGoals = false;
                ScheduleTutorial(TutorialTrigger.UserTradePolicyInfo, 0.8f);
            }
            else if (shouldShowDemoCompleteAfterTradeGoals && !isTutorialSkipped)
            {
                shouldShowDemoCompleteAfterTradeGoals = false;
                ScheduleTutorial(TutorialTrigger.UserDemoCompleteInfo, 0.8f);
            }
        }
    }

    private static void SetGraphicRaycast(GameObject root, bool raycastTarget)
    {
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
        {
            graphic.raycastTarget = raycastTarget;
        }
    }
}
