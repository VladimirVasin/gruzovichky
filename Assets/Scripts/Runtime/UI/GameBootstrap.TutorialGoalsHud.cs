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
        OpenWorkersCard
    }

    private enum TutorialGoalsMode
    {
        CameraControls,
        RoadBuilding,
        CoreBuildings,
        LumberjackCamp,
        BuyTruck,
        ServiceBuildings,
        WorkerCard
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
        activeTutorialGoals.Add(TutorialGoalKind.BuyFirstTruck);
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

        SessionDebugLogger.Log("TUTORIAL", "Worker card goal started.");
    }

    private void ClearTutorialGoalContinuationFlags()
    {
        shouldShowRoadTutorialAfterCameraGoals = false;
        shouldShowCoreBuildingsTutorialAfterRoadGoals = false;
        shouldShowLumberjackCampTutorialAfterCoreGoals = false;
        shouldShowWorkerShiftTutorialAfterLumberjackGoals = false;
        shouldShowTruckFreightTutorialAfterBuyTruckGoals = false;
        shouldShowWorkersOverviewAfterServiceGoals = false;
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
        panel.sizeDelta = new Vector2(340f, 214f);
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
        tutorialGoalsHud.SubtitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

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
        RectTransform row = FleetCanvasUiFactory.CreateLayoutRow(kind.ToString(), parent, 26f, 9f);
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

        Text label = CreateGoalText("Label", row, font, 14, FontStyle.Normal, Color.white, TextAnchor.MiddleLeft);
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
            TutorialGoalsMode.BuyTruck => ru ? "Купи первый грузовик." : "Buy the first truck.",
            _ => ru ? "Освой камеру перед строительством." : "Learn the camera before building."
        };

        tutorialGoalsHud.TitleText.text = ru ? "\u0426\u0435\u043b\u0438" : "Goals";
        tutorialGoalsHud.SubtitleText.text = tutorialGoalsMode switch
        {
            TutorialGoalsMode.RoadBuilding => ru ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u0434\u043e\u0440\u043e\u0433\u0443 \u0434\u0432\u0443\u043c\u044f \u0441\u043f\u043e\u0441\u043e\u0431\u0430\u043c\u0438." : "Build roads in two ways.",
            TutorialGoalsMode.CoreBuildings => ru ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u0431\u0430\u0437\u043e\u0432\u044b\u0435 \u0437\u0434\u0430\u043d\u0438\u044f \u0433\u043e\u0440\u043e\u0434\u0430." : "Build the town core buildings.",
            TutorialGoalsMode.LumberjackCamp => ru ? "\u0417\u0430\u043f\u0443\u0441\u0442\u0438 \u043f\u0435\u0440\u0432\u0443\u044e \u0434\u043e\u0431\u044b\u0447\u0443 \u0434\u0435\u0440\u0435\u0432\u0430." : "Start your first logging production.",
            TutorialGoalsMode.BuyTruck => ru ? "\u041a\u0443\u043f\u0438 \u043f\u0435\u0440\u0432\u044b\u0439 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a." : "Buy the first truck.",
            TutorialGoalsMode.ServiceBuildings => ru ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u0441\u0435\u0440\u0432\u0438\u0441\u044b \u0434\u043b\u044f \u043b\u044e\u0434\u0435\u0439 \u0438 \u0442\u0440\u0430\u043d\u0441\u043f\u043e\u0440\u0442\u0430." : "Build services for workers and vehicles.",
            TutorialGoalsMode.WorkerCard => ru ? "\u041e\u0442\u043a\u0440\u043e\u0439 \u043a\u0430\u0440\u0442\u043e\u0447\u043a\u0443 \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e." : "Open a worker card.",
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
            TutorialGoalKind.BuyFirstTruck => ru ? "Купи первый грузовик" : "Buy the first truck",
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
            TutorialGoalKind.RoadSingleCell => ru ? "\u041f\u043e\u0441\u0442\u0430\u0432\u044c \u043e\u0434\u043d\u0443 \u043a\u043b\u0435\u0442\u043a\u0443 \u0434\u043e\u0440\u043e\u0433\u0438: \u041b\u041a\u041c" : "Place one road cell: left click",
            TutorialGoalKind.RoadShiftPath => ru ? "\u041f\u0440\u043e\u0442\u044f\u043d\u0438 \u0443\u0447\u0430\u0441\u0442\u043e\u043a \u0434\u043e\u0440\u043e\u0433\u0438: Shift + \u041b\u041a\u041c" : "Build a road segment: Shift + left click",
            TutorialGoalKind.BuildWarehouse => ru ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u0421\u043a\u043b\u0430\u0434" : "Build Warehouse",
            TutorialGoalKind.BuildMotel => ru ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u041c\u043e\u0442\u0435\u043b\u044c" : "Build Motel",
            TutorialGoalKind.BuildParking => ru ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u041f\u0430\u0440\u043a\u043e\u0432\u043a\u0443" : "Build Parking",
            TutorialGoalKind.BuildLumberjackCamp => ru ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u041b\u0430\u0433\u0435\u0440\u044c \u043b\u0435\u0441\u043e\u0440\u0443\u0431\u043e\u0432" : "Build Lumberjack Camp",
            TutorialGoalKind.AssignLumberjackWorker => ru ? "\u041d\u0430\u0437\u043d\u0430\u0447\u044c \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e \u0432 \u041b\u0430\u0433\u0435\u0440\u044c \u043b\u0435\u0441\u043e\u0440\u0443\u0431\u043e\u0432" : "Assign a worker to Lumberjack Camp",
            TutorialGoalKind.BuyFirstTruck => ru ? "\u041a\u0443\u043f\u0438 \u043f\u0435\u0440\u0432\u044b\u0439 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a" : "Buy the first truck",
            TutorialGoalKind.AssignTruckDriverShift => ru ? "\u041d\u0430\u0437\u043d\u0430\u0447\u044c \u0432\u043e\u0434\u0438\u0442\u0435\u043b\u044f \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0430 \u043d\u0430 \u0441\u043c\u0435\u043d\u0443" : "Assign a truck driver to a shift",
            TutorialGoalKind.BuildBar => ru ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u0411\u0430\u0440" : "Build Bar",
            TutorialGoalKind.BuildGamblingHall => ru ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u0418\u0433\u0440\u043e\u0432\u044b\u0435 \u0430\u0432\u0442\u043e\u043c\u0430\u0442\u044b" : "Build Gambling Hall",
            TutorialGoalKind.BuildCanteen => ru ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u0421\u0442\u043e\u043b\u043e\u0432\u0443\u044e" : "Build Canteen",
            TutorialGoalKind.BuildGasStation => ru ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 \u0417\u0430\u043f\u0440\u0430\u0432\u043a\u0443" : "Build Gas Station",
            TutorialGoalKind.BuildCityPark => ru ? "\u041f\u043e\u0441\u0442\u0440\u043e\u0439 City Park" : "Build City Park",
            TutorialGoalKind.OpenWorkersCard => ru ? "\u041e\u0442\u043a\u0440\u043e\u0439 \u043c\u0435\u043d\u044e \u0420\u0430\u0431\u043e\u0447\u0438\u0435" : "Open the Workers menu",
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
            TutorialGoalsMode.BuyTruck => kind is TutorialGoalKind.BuyFirstTruck or TutorialGoalKind.AssignTruckDriverShift,
            TutorialGoalsMode.ServiceBuildings => kind is TutorialGoalKind.BuildBar or TutorialGoalKind.BuildGamblingHall or TutorialGoalKind.BuildCanteen or TutorialGoalKind.BuildGasStation or TutorialGoalKind.BuildCityPark,
            TutorialGoalsMode.WorkerCard => kind is TutorialGoalKind.OpenWorkersCard,
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
