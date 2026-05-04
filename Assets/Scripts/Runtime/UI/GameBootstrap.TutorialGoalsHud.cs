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
        BuildBar,
        BuildGamblingHall,
        BuildCanteen,
        BuildGasStation,
        BuildCityPark,
        OpenWorkersCard,
        HireNewWorker,
        AssignWarehouseLoaders,
        BuildLocalBusStops,
        AssignBusDrivers,
        SetTaxRate15,
        AssignIntercityDriver,
        CreateBuyTextileOrder,
        JoinRaceParticipation
    }

    private enum TutorialGoalsMode
    {
        CameraControls,
        RoadBuilding,
        CoreBuildings,
        LumberjackCamp,
        BuyTruck,
        ServiceBuildings,
        WorkerCard,
        WarehouseLoaders,
        LocalTransport,
        EconomyTaxes,
        TradeSetup,
        JoinRace
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
    private bool shouldShowWorkersOverviewAfterServiceGoals;
    private bool shouldShowLocalTransportAfterWarehouseLoadersGoals;
    private bool shouldShowLocalBusRoutesAfterTransportGoals;
    private bool shouldShowTradeIntroAfterEconomyGoals;
    private bool shouldShowTradeRaceInfoAfterTradeGoals;
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
        shouldShowWorkersOverviewAfterServiceGoals = false;
        shouldShowLocalTransportAfterWarehouseLoadersGoals = false;
        shouldShowLocalBusRoutesAfterTransportGoals = false;
        shouldShowTradeIntroAfterEconomyGoals = false;
        shouldShowTradeRaceInfoAfterTradeGoals = false;
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
        shouldShowRoadTutorialAfterCameraGoals = selectedGameStartMode == GameStartMode.User && !isTutorialSkipped;
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
        shouldShowCoreBuildingsTutorialAfterRoadGoals = selectedGameStartMode == GameStartMode.User && !isTutorialSkipped;
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
        shouldShowLumberjackCampTutorialAfterCoreGoals = selectedGameStartMode == GameStartMode.User && !isTutorialSkipped;
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
        shouldShowWorkerShiftTutorialAfterLumberjackGoals = selectedGameStartMode == GameStartMode.User && !isTutorialSkipped;
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
        shouldShowTruckFreightTutorialAfterBuyTruckGoals = selectedGameStartMode == GameStartMode.User && !isTutorialSkipped;
        tutorialGoalsMode = TutorialGoalsMode.BuyTruck;
        ShowTutorialGoalsHud();
        SessionDebugLogger.Log("TUTORIAL", "Truck logistics goals started.");
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
        shouldShowWorkersOverviewAfterServiceGoals = selectedGameStartMode == GameStartMode.User && !isTutorialSkipped;
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
        activeTutorialGoals.Add(TutorialGoalKind.HireNewWorker);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        tutorialGoalsMode = TutorialGoalsMode.WorkerCard;
        ShowTutorialGoalsHud();
        if (isDriversPanelOpen)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.OpenWorkersCard);
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
        shouldShowLocalTransportAfterWarehouseLoadersGoals = selectedGameStartMode == GameStartMode.User && !isTutorialSkipped;
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
        shouldShowLocalBusRoutesAfterTransportGoals = selectedGameStartMode == GameStartMode.User && !isTutorialSkipped;
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
        shouldShowTradeIntroAfterEconomyGoals = selectedGameStartMode == GameStartMode.User && !isTutorialSkipped;
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
        activeTutorialGoals.Add(TutorialGoalKind.AssignIntercityDriver);
        activeTutorialGoals.Add(TutorialGoalKind.CreateBuyTextileOrder);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        tutorialGoalsMode = TutorialGoalsMode.TradeSetup;
        shouldShowTradeRaceInfoAfterTradeGoals = selectedGameStartMode == GameStartMode.User && !isTutorialSkipped;
        ShowTutorialGoalsHud();
        CheckTutorialTradeSetupGoals();
        SessionDebugLogger.Log("TUTORIAL", "Trade setup goals started.");
    }

    private void BeginJoinRaceTutorialGoals()
    {
        EnsureTutorialGoalsHud();
        completedTutorialGoals.Clear();
        activeTutorialGoals.Clear();
        ClearTutorialGoalContinuationFlags();
        activeTutorialGoals.Add(TutorialGoalKind.JoinRaceParticipation);
        isTutorialGoalsActive = true;
        isTutorialGoalsComplete = false;
        tutorialGoalsSuccessTimer = 0f;
        tutorialGoalsSuccessDuration = 0f;
        tutorialGoalsMode = TutorialGoalsMode.JoinRace;
        ShowTutorialGoalsHud();
        SessionDebugLogger.Log("TUTORIAL", "Join Race goal started.");
    }

    private void ClearTutorialGoalContinuationFlags()
    {
        shouldShowRoadTutorialAfterCameraGoals = false;
        shouldShowCoreBuildingsTutorialAfterRoadGoals = false;
        shouldShowLumberjackCampTutorialAfterCoreGoals = false;
        shouldShowWorkerShiftTutorialAfterLumberjackGoals = false;
        shouldShowTruckFreightTutorialAfterBuyTruckGoals = false;
        shouldShowWorkersOverviewAfterServiceGoals = false;
        shouldShowLocalTransportAfterWarehouseLoadersGoals = false;
        shouldShowLocalBusRoutesAfterTransportGoals = false;
        shouldShowTradeIntroAfterEconomyGoals = false;
        shouldShowTradeRaceInfoAfterTradeGoals = false;
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

        PlayUiSound(slotWinClip, 0.7f);
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
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildBar);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildGamblingHall);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildCanteen);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildGasStation);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildCityPark);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.OpenWorkersCard);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.HireNewWorker);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.AssignWarehouseLoaders);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.BuildLocalBusStops);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.AssignBusDrivers);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.SetTaxRate15);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.AssignIntercityDriver);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.CreateBuyTextileOrder);
        CreateTutorialGoalRow(panel, font, TutorialGoalKind.JoinRaceParticipation);

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
            TutorialGoalsMode.LumberjackCamp => ru ? "Открой Стройку (B), поставь лагерь, затем открой Вакансии." : "Open Build (B), place the camp, then open Vacancies.",
            TutorialGoalsMode.BuyTruck => ru ? "Открой Вакансии: выбери смену водителя. Parking выдаст грузовик автоматически." : "Open Vacancies: choose a driver shift. Parking will provide the truck automatically.",
            TutorialGoalsMode.ServiceBuildings => ru ? "Открой Стройку (B) и поставь сервисные здания." : "Open Build (B) and place the service buildings.",
            TutorialGoalsMode.WorkerCard => ru ? "\u041e\u0442\u043a\u0440\u043e\u0439 \u0420\u0430\u0431\u043e\u0447\u0438\u0435 \u0438 \u043f\u043e\u0441\u043c\u043e\u0442\u0440\u0438 \u043a\u0430\u0440\u0442\u043e\u0447\u043a\u0443: \u043f\u0440\u0438\u0435\u0437\u0434 \u0440\u0430\u0431\u043e\u0447\u0438\u0445 \u0437\u0430\u043f\u0443\u0441\u0442\u0438\u0442\u0441\u044f \u0441\u0430\u043c." : "Open Workers and inspect a card: worker arrivals start automatically.",
            TutorialGoalsMode.WarehouseLoaders => ru ? "Открой Вакансии и заполни три складских слота." : "Open Vacancies and fill three Warehouse slots.",
            TutorialGoalsMode.LocalTransport => ru ? "Открой Стройку для остановок, затем Вакансии для водителей." : "Open Build for stops, then Vacancies for drivers.",
            TutorialGoalsMode.EconomyTaxes => ru ? "Открой Экономика -> Налоги и нажимай + до 15%." : "Open Economy -> Taxes and press + until 15%.",
            TutorialGoalsMode.TradeSetup => ru ? "Открой Вакансии для межгорода, затем Экономика -> Торговля." : "Open Vacancies for intercity, then Economy -> Trade.",
            TutorialGoalsMode.JoinRace => ru ? "\u0414\u043e\u0436\u0434\u0438\u0441\u044c \u043a\u043d\u043e\u043f\u043a\u0438 Join the Race \u0438 \u043d\u0430\u0436\u043c\u0438 \u0435\u0451." : "Wait for Join the Race and press it.",
            _ => ru ? "\u041e\u0441\u0432\u043e\u0439 \u043a\u0430\u043c\u0435\u0440\u0443 \u043f\u0435\u0440\u0435\u0434 \u0441\u0442\u0440\u043e\u0439\u043a\u043e\u0439." : "Learn the camera before building."
        };

        foreach (TutorialGoalRowUi row in tutorialGoalsHud.Rows)
        {
            row.Root.SetActive(IsTutorialGoalVisible(row.Kind));
            row.LabelText.text = GetTutorialGoalLabelSafe(row.Kind, ru);
        }
    }

    private static string GetTutorialGoalLabel(TutorialGoalKind kind, bool ru)
    {
        return kind switch
        {
            TutorialGoalKind.CameraZoomIn => ru ? "Zoom In: приблизь камеру" : "Zoom In: move camera closer",
            TutorialGoalKind.CameraZoomOut => ru ? "Zoom Out: отдали камеру" : "Zoom Out: move camera away",
            TutorialGoalKind.CameraPan => ru ? "Scroll карты: сдвинь обзор" : "Map scroll: pan the view",
            TutorialGoalKind.CameraRotate => ru ? "Поворот карты: Q / E" : "Rotate map: Q / E",
            TutorialGoalKind.RoadSingleCell => ru ? "Поставь одну клетку дороги: ЛКМ" : "Place one road cell: left click",
            TutorialGoalKind.RoadShiftPath => ru ? "Протяни участок дороги: Shift + ЛКМ" : "Build a road segment: Shift + left click",
            TutorialGoalKind.BuildWarehouse => ru ? "Построй Склад" : "Build Warehouse",
            TutorialGoalKind.BuildMotel => ru ? "Построй Мотель" : "Build Motel",
            TutorialGoalKind.BuildParking => ru ? "Построй Парковку" : "Build Parking",
            TutorialGoalKind.BuildLumberjackCamp => ru ? "Построй Лагерь лесорубов" : "Build Lumberjack Camp",
            TutorialGoalKind.AssignLumberjackWorker => ru ? "Назначь рабочего в Лагерь лесорубов" : "Assign a worker to Lumberjack Camp",
            TutorialGoalKind.BuyFirstTruck => ru ? "Parking выдаст грузовик автоматически" : "Parking provides the truck automatically",
            TutorialGoalKind.AssignTruckDriverShift => ru ? "Назначь водителя грузовика на смену" : "Assign a truck driver to a shift",
            _ => string.Empty
        };
    }

    private static string GetTutorialGoalLabelSafe(TutorialGoalKind kind, bool ru)
    {
        return kind switch
        {
            TutorialGoalKind.CameraZoomIn => ru ? "Zoom In: \u043f\u0440\u0438\u0431\u043b\u0438\u0437\u044c \u043a\u0430\u043c\u0435\u0440\u0443" : "Zoom In: move camera closer",
            TutorialGoalKind.CameraZoomOut => ru ? "Zoom Out: \u043e\u0442\u0434\u0430\u043b\u0438 \u043a\u0430\u043c\u0435\u0440\u0443" : "Zoom Out: move camera away",
            TutorialGoalKind.CameraPan => ru ? "Scroll \u043a\u0430\u0440\u0442\u044b: \u0441\u0434\u0432\u0438\u043d\u044c \u043e\u0431\u0437\u043e\u0440" : "Map scroll: pan the view",
            TutorialGoalKind.CameraRotate => ru ? "\u041f\u043e\u0432\u043e\u0440\u043e\u0442 \u043a\u0430\u0440\u0442\u044b: Q / E" : "Rotate map: Q / E",
            TutorialGoalKind.RoadSingleCell => ru ? "Стройка (B) -> Дорога: поставь 1 клетку ЛКМ" : "Build (B) -> Road: place 1 cell with left click",
            TutorialGoalKind.RoadShiftPath => ru ? "Стройка (B) -> Дорога: протяни Shift + ЛКМ" : "Build (B) -> Road: drag with Shift + left click",
            TutorialGoalKind.BuildWarehouse => ru ? "Стройка (B) -> Склад: выбери и поставь" : "Build (B) -> Warehouse: select and place",
            TutorialGoalKind.BuildMotel => ru ? "Стройка (B) -> Мотель: выбери и поставь" : "Build (B) -> Motel: select and place",
            TutorialGoalKind.BuildParking => ru ? "Стройка (B) -> Парковка: выбери и поставь" : "Build (B) -> Parking: select and place",
            TutorialGoalKind.BuildLumberjackCamp => ru ? "Стройка (B) -> Лагерь лесорубов: поставь у леса" : "Build (B) -> Lumberjack Camp: place near forest",
            TutorialGoalKind.AssignLumberjackWorker => ru ? "Вакансии -> Лесозаготовка -> Смена -> Рабочий" : "Vacancies -> Logging -> Shift -> Worker",
            TutorialGoalKind.BuyFirstTruck => ru ? "Parking -> свободный слот автопарка" : "Parking -> free fleet slot",
            TutorialGoalKind.AssignTruckDriverShift => ru ? "Вакансии -> Водитель грузовика -> Смена -> Рабочий" : "Vacancies -> Truck Driver -> Shift -> Worker",
            TutorialGoalKind.BuildBar => ru ? "Стройка (B) -> Бар: выбери и поставь" : "Build (B) -> Bar: select and place",
            TutorialGoalKind.BuildGamblingHall => ru ? "Стройка (B) -> Игровые автоматы: выбери и поставь" : "Build (B) -> Gambling Hall: select and place",
            TutorialGoalKind.BuildCanteen => ru ? "Стройка (B) -> Столовая: выбери и поставь" : "Build (B) -> Canteen: select and place",
            TutorialGoalKind.BuildGasStation => ru ? "Стройка (B) -> Заправка: выбери и поставь" : "Build (B) -> Gas Station: select and place",
            TutorialGoalKind.BuildCityPark => ru ? "Стройка (B) -> City Park: выбери и поставь" : "Build (B) -> City Park: select and place",
            TutorialGoalKind.OpenWorkersCard => ru ? "Верхнее меню -> Рабочие: открой карточку рабочего" : "Top menu -> Workers: open a worker card",
            TutorialGoalKind.HireNewWorker => ru ? "\u0420\u0430\u0431\u043e\u0447\u0438\u0435 -> \u0434\u043e\u0436\u0434\u0438\u0441\u044c \u0430\u0432\u0442\u043e-\u043f\u0440\u0438\u0435\u0437\u0434\u0430" : "Workers -> wait for auto-arrival",
            TutorialGoalKind.AssignWarehouseLoaders => ru ? "Вакансии -> Складской слот 1-3 -> Смена -> Рабочий" : "Vacancies -> Warehouse slots 1-3 -> Shift -> Worker",
            TutorialGoalKind.BuildLocalBusStops => ru ? "Стройка (B) -> Остановка: поставь ровно 2" : "Build (B) -> Bus Stop: place exactly 2",
            TutorialGoalKind.AssignBusDrivers => ru ? "Вакансии -> Водитель автобуса -> назначь 3 смены" : "Vacancies -> Bus Driver -> assign 3 shifts",
            TutorialGoalKind.SetTaxRate15 => ru ? "Экономика -> Налоги: нажимай + до 15%" : "Economy -> Taxes: press + until 15%",
            TutorialGoalKind.AssignIntercityDriver => ru ? "Вакансии -> Водитель грузовика -> Смена -> Рабочий" : "Vacancies -> Truck Driver -> Shift -> Worker",
            TutorialGoalKind.CreateBuyTextileOrder => ru ? "\u042d\u043a\u043e\u043d\u043e\u043c\u0438\u043a\u0430 -> \u0422\u043e\u0440\u0433\u043e\u0432\u043b\u044f -> Textile -> \u041a\u0443\u043f\u0438\u0442\u044c -> \u0420\u0430\u0437\u043c\u0435\u0441\u0442\u0438\u0442\u044c \u0437\u0430\u043a\u0430\u0437" : "Economy -> Trade -> Textile -> Buy -> Place Order",
            TutorialGoalKind.JoinRaceParticipation => ru ? "\u0414\u043e\u0436\u0434\u0438\u0441\u044c \u0432\u044b\u0435\u0437\u0434\u0430 \u0437\u0430 \u043a\u0430\u0440\u0442\u0443 \u0438 \u043d\u0430\u0436\u043c\u0438 Join the Race" : "Wait until the truck leaves the map, then press Join the Race",
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
            TutorialGoalsMode.ServiceBuildings => kind is TutorialGoalKind.BuildBar or TutorialGoalKind.BuildGamblingHall or TutorialGoalKind.BuildCanteen or TutorialGoalKind.BuildGasStation or TutorialGoalKind.BuildCityPark,
            TutorialGoalsMode.WorkerCard => kind is TutorialGoalKind.OpenWorkersCard or TutorialGoalKind.HireNewWorker,
            TutorialGoalsMode.WarehouseLoaders => kind is TutorialGoalKind.AssignWarehouseLoaders,
            TutorialGoalsMode.LocalTransport => kind is TutorialGoalKind.BuildLocalBusStops or TutorialGoalKind.AssignBusDrivers,
            TutorialGoalsMode.EconomyTaxes => kind is TutorialGoalKind.SetTaxRate15,
            TutorialGoalsMode.TradeSetup => kind is TutorialGoalKind.AssignIntercityDriver or TutorialGoalKind.CreateBuyTextileOrder,
            TutorialGoalsMode.JoinRace => kind is TutorialGoalKind.JoinRaceParticipation,
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
            else if (shouldShowTradeIntroAfterEconomyGoals && !isTutorialSkipped)
            {
                shouldShowTradeIntroAfterEconomyGoals = false;
                ScheduleTutorial(TutorialTrigger.UserTradeIntroInfo, 0.8f);
            }
            else if (shouldShowTradeRaceInfoAfterTradeGoals && !isTutorialSkipped)
            {
                shouldShowTradeRaceInfoAfterTradeGoals = false;
                ScheduleTutorial(TutorialTrigger.UserTradeRaceInfo, 0.8f);
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
