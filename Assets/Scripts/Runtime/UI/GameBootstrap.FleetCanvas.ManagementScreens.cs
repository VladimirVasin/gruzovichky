using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static readonly Color DriversScreenTint   = new(0.06f, 0.08f, 0.11f, 0.76f);
    private static readonly Color DriversCardColor    = new(0.13f, 0.16f, 0.21f, 0.98f);
    private static readonly Color DriversCardSelected = new(0.29f, 0.25f, 0.13f, 0.98f);

    private DriversScreenUiRefs driversScreenUi;
    private bool isDriversScreenDirty = true;
    private int  selectedWorkerPanelDriverId = 0;
    private bool isEconomyScreenDirty = true;
    private const int MaxDriverCardSlots = 8;
    private const int MaxShiftDriverSlots = 16;
    private const int MaxEconomyRowSlots = 64;
    private static readonly Color ShiftsScreenTint = new(0.06f, 0.08f, 0.11f, 0.76f);
    private static readonly Color ShiftsCardColor = new(0.13f, 0.16f, 0.21f, 0.98f);
    private static readonly Color ShiftsCardSelected = new(0.29f, 0.25f, 0.13f, 0.98f);
    private const float ShiftsWindowWidth = 1100f;
    private const float ShiftsWindowHeight = 680f;
    private const float ShiftsLeftPanelWidth = 500f;
    private const float ShiftsRightPanelWidth = 548f;
    private const float ShiftsInnerPanelHeight = 644f;
    private const float ShiftsSelectionCardHeight = 156f;
    private const float ShiftsTabRowHeight = 36f;
    private ShiftsScreenUiRefs shiftsScreenUi;
    private bool isShiftsScreenDirty = true;
    private bool isLogisticsTabActive = false;
    private Button shiftsLogisticsTabBtn;
    private Button shiftsTransportTabBtn;
    private Text shiftsLogisticsTabText;
    private Text shiftsTransportTabText;
    private RectTransform shiftsLogisticsPanel;
    private RectTransform shiftsTransportPanel;
    private string lastShiftsHudDebugState = string.Empty;
    private bool hasLoggedLegacyShiftsHudDraw;
    private readonly LogisticsSlotUi[] logisticsSlots = new LogisticsSlotUi[6];
    private BuildScreenUiRefs buildScreenUi;
    private bool isBuildScreenDirty = true;
    private WorldMapScreenUiRefs worldMapScreenUi;
    private bool isWorldMapScreenDirty = true;

    private sealed class DriversScreenUiRefs
    {
        public GameObject    CanvasRoot;
        public RectTransform WindowRoot;
        public RectTransform LeftPanel;
        public RectTransform RightPanel;
        public RectTransform WorkerListContent;
        public readonly List<WorkerRowUi> WorkerRows = new();
        public Button HireButton;
        public Text   HireButtonText;
        public Text   HireStatusText;
        public Text   HeaderCountText;
        // Right panel — detail view
        public GameObject DetailPlaceholderCard;
        public GameObject DetailContentRoot;
        public Text  DetailNameText;
        public Text  DetailProfileTitleText;
        public Text  DetailRoleText;
        public RectTransform DetailPortraitRoot;
        public Text  DetailSkillsTitleText;
        public Text  DetailDrivingSkillText;
        public Text  DetailStaminaSkillText;
        public Text  DetailProductionSkillText;
        public Text  DetailLogisticsSkillText;
        public Text  DetailEffectsTitleText;
        public RectTransform DetailEffectsListRoot;
        public Text  DetailEffectsEmptyText;
        public readonly List<Text> DetailEffectTexts = new();
        public Text  DetailNeedsTitleText;
        public Text  DetailMealNeedText;
        public Text  DetailSleepNeedText;
        public Text  DetailLeisureNeedText;
        public Text  DetailPerksTitleText;
        public Text  DetailPerksEmptyText;
        public readonly List<Text> DetailPerkTexts = new();
        public GameObject DetailSkillTooltipRoot;
        public Text DetailSkillTooltipTitleText;
        public Text DetailSkillTooltipBodyText;
        public Image DetailStatusBadge;
        public Text  DetailStatusText;
        public Text  DetailAssignmentLabel;
        public Text  DetailAssignmentValue;
        public Text  DetailShiftLabel;
        public Text  DetailShiftText;
        public Text  DetailDutyLabel;
        public Text  DetailDutyStateText;
        public Text  DetailWorkTitleText;
        public Text  DetailSalaryLabel;
        public Text  DetailSalaryText;
        public Button DetailSalaryMinusBtn;
        public Button DetailSalaryPlusBtn;
        public Text  DetailBalanceLabel;
        public Text  DetailBalanceText;
        public Text  DetailContractTitleText;
        public Button DetailFocusButton;
        public Text   DetailFocusButtonText;
    }

    private sealed class WorkerRowUi
    {
        public int           DriverId;
        public RectTransform Root;
        public Image         Background;
        public Image         StatusBadgeBg;
        public Text          NameText;
        public Text          StatusText;
        public Text          SubText;
        public Button        SelectButton;
        public RectTransform NeedsMealBarFill;
        public RectTransform NeedsSleepBarFill;
        public RectTransform NeedsLeisureBarFill;
    }

    private sealed class ShiftsScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public RectTransform LeftPanel;
        public RectTransform RightPanel;
        public RectTransform DriverListContent;
        public ScrollRect TransportScrollRect;
        public ScrollRect ProductionScrollRect;
        public Text TitleText;
        public Text HeaderCountText;
        public Text SelectionTitleText;
        public Text SelectionNameText;
        public Text SelectionProfessionText;
        public Text SelectionStatusText;
        public Text SelectionHintText;
        public Text LogisticsSectionTitleText;
        public Text LogisticsSectionSummaryText;
        public Text ProductionSectionTitleText;
        public Text ProductionSectionSummaryText;
        public Text BusDriverGroupTitleText;
        public Text BusDriverGroupSummaryText;
        public readonly List<ShiftDriverRowUi> DriverRows = new();
        public readonly List<ShiftCardUi> ShiftCards = new();
        public IntercitySlotUi IntercitySlot;
        public readonly List<IntercitySlotUi> BusDriverSlots = new();
    }

    private sealed class ShiftDriverRowUi
    {
        public int DriverId;
        public RectTransform Root;
        public Image Background;
        public Text NameText;
        public Text ProfessionText;
        public Text StatusText;
        public Button SelectButton;
    }

    private sealed class ShiftCardUi
    {
        public int ShiftHour;
        public Text HeaderText;
        public Text SummaryText;
        public RectTransform AssignedListRoot;
        public Text EmptyText;
        public readonly List<GameObject> AssignedRows = new();
        public readonly List<Text> AssignedDriverTexts = new();
        public readonly List<Button> RemoveButtons = new();
        public Text AssignButtonText;
        public Button AssignButton;
        public Image ActiveBorderImage;
    }

    private sealed class IntercitySlotUi
    {
        public Text HeaderText;
        public Text SummaryText;
        public Text AssignedDriverText;
        public Text StatusText;
        public Text AssignButtonText;
        public Button AssignButton;
        public Button RemoveButton;
    }

    private sealed class BuildItemUi
    {
        public BuildTool     Tool;
        public Color         DefaultAccentColor;
        public RectTransform Root;
        public Button        Button;
        public Image         CardBg;
        public Image         AccentBg;
        public Text          TitleText;
        public Text          DescText;
        public Text          StatusText;
        public Image         StatusBg;
    }

    private sealed class BuildCategoryUi
    {
        public string        LabelEn;
        public string        LabelRu;
        public bool          IsExpanded;
        public RectTransform HeaderRoot;
        public Text          HeaderText;
        public Text          ArrowText;
        public BuildItemUi[] Items;
    }

    private sealed class BuildScreenUiRefs
    {
        public GameObject        CanvasRoot;
        public RectTransform     WindowRoot;
        public BuildCategoryUi[] Categories;
    }

    private sealed class DriverCardUi
    {
        public RectTransform Root;
        public Image Background;
        public Image StatusBadgeBackground;
        public Text  NameText;
        public Text  StatusText;
        public Text  TruckLabelText;
        public Text  TruckText;
        public Text  SalaryText;
        public Text  BalanceText;
        public Button SelectButton;
    }

    private sealed class ResourcesScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Text TreasuryValueText;
        public string LastTreasuryValue;
        // Tab buttons
        public Button WarehouseTabBtn;
        public Button ProductionTabBtn;
        public Text   WarehouseTabText;
        public Text   ProductionTabText;
        // Tab panels
        public GameObject WarehousePanel;
        public GameObject ProductionPanel;
        // Warehouse tab rows
        public readonly List<ResourceSummaryRowUi> WarehouseRows = new();
        // Production tab sections
        public readonly List<ProductionBuildingSectionUi> ProductionSections = new();
    }

    private sealed class ResourceSummaryRowUi
    {
        public Text NameText;
        public Text ValueText;
        public string LastName;
        public string LastValue;
        public TradeResourceType ResourceType;
    }

    private sealed class ProductionBuildingSectionUi
    {
        public LocationType  BuildingType;
        public Text          SectionHeaderText;
        public GameObject    Root;
        public readonly List<ResourceSummaryRowUi> Rows = new();
    }

    private sealed class EconomyScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Text HeaderCountText;
        public Button TaxesTabButton;
        public Text TaxesTabText;
        public Button TradeTabButton;
        public Text TradeTabText;
        public RectTransform TaxesPanel;
        public RectTransform TradePanel;
        public Button TaxesRateMinusButton;
        public Button TaxesRatePlusButton;
        public Text TaxesRateValueText;
        public Text TaxesIncomeSummaryText;
        public Text TaxesTimerSummaryText;
        public Text TradeResourceText;
        public Button TradeResourceDropdownButton;
        public RectTransform TradeResourceOptionsPanel;
        public readonly List<Button> TradeResourceOptionButtons = new();
        public readonly List<Text> TradeResourceOptionTexts = new();
        public readonly List<Text> TradeResourceOptionAmountTexts = new();
        public Text TradeActionText;
        public Button TradeActionDropdownButton;
        public RectTransform TradeActionOptionsPanel;
        public readonly List<Button> TradeActionOptionButtons = new();
        public readonly List<Text> TradeActionOptionTexts = new();
        public Button TradeAmountMinusButton;
        public Button TradeAmountPlusButton;
        public Text TradeAmountText;
        public Button TradePlaceOrderButton;
        public Text TradePlaceOrderButtonText;
        public RectTransform ActiveOrdersContent;
        public Text EmptyOrdersText;
        public readonly List<TradeOrderRowUi> TradeOrderRows = new();
    }

    private sealed class TradeOrderRowUi
    {
        public RectTransform Root;
        public Image TagBackground;
        public Text TagText;
        public Text OrderText;
        public Button RemoveButton;
        public int OrderId;
    }

    private sealed class WorldMapRouteRowUi
    {
        public GameObject Root;
        public Text TagText;
        public Text OrderText;
        public Button RemoveButton;
        public int OrderId;
    }

    private sealed class WorldMapScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Text TitleText;
        public Text SubtitleText;
        public Text SelectionHintText;
        public readonly List<WorldMapCellUi> Cells = new();
        public WorldMapDetailPreviewUi DetailPreview;
        public Text DetailsNameText;
        public Text DetailsStatusText;
        public Text DetailsResourcesText;
        public Text DetailsImportsText;
        public Text DetailsDescriptionText;
        // trade route bottom panel
        public GameObject RoutePanelRoot;
        public Text RoutePanelTitleText;
        public RectTransform RouteOrdersRow;
        public readonly List<WorldMapRouteRowUi> RouteRows = new();
        // add-order form
        public Text RouteResourceLabel;
        public Text RouteAmountLabel;
        public Button RouteTypeButton;
        public Text RouteTypeButtonText;
        public Button RoutePlaceButton;
    }

    private sealed class WorldMapCellUi
    {
        public Button Button;
        public Image Background;
        public Outline Outline;
        public Image PreviewBackground;
        public Text PreviewPlaceholderText;
        public Image WaterShape;
        public Image HighwayShape;
        public Image ForestShape;
        public Image TownBlockA;
        public Image TownBlockB;
        public Image TownBlockC;
        public Image HighwayDashA;
        public Image HighwayDashB;
        public Image HighwayDashC;
        public Text NameText;
        public Text TypeText;
        public Image RouteStatusDot;
        public int RegionIndex;
    }

    private sealed class WorldMapDetailPreviewUi
    {
        public Image PreviewBackground;
        public Text PlaceholderText;
        public Image WaterShape;
        public Image HighwayShape;
        public Image ForestShape;
        public Image TownBlockA;
        public Image TownBlockB;
        public Image TownBlockC;
        public Image HighwayDashA;
        public Image HighwayDashB;
        public Image HighwayDashC;
    }

    private sealed class LogisticsSlotUi
    {
        public LocationType  BuildingType;
        public int           SlotIndex;  // which worker slot within this building (0-based)
        public RectTransform Root;
        public Text          BuildingNameText;
        public Text          AssignedWorkerText;
        public Text          WorkHoursText;
        public Button        AssignButton;
        public Text          AssignButtonText;
        public Button        RemoveButton;
    }

    private ResourcesScreenUiRefs resourcesScreenUi;
    private bool isResourcesWarehouseTab = true;
    private EconomyScreenUiRefs economyScreenUi;

    private void SetupDriversScreenUi()
    {
        if (driversScreenUi != null) return;
        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        driversScreenUi = new DriversScreenUiRefs();

        // Canvas
        GameObject canvasObject = new("DriversScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        driversScreenUi.CanvasRoot = canvasObject;

        // Window root — two-panel layout (980×560)
        GameObject windowRoot = CreateUiObject("DriversWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        SetCenteredWindow(windowRect, 1080f, 620f, -16f);
        driversScreenUi.WindowRoot = windowRect;
        Image windowBg = windowRoot.AddComponent<Image>();
        windowBg.color = DriversScreenTint;
        Outline windowOutline = windowRoot.AddComponent<Outline>();
        windowOutline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        windowOutline.effectDistance = new Vector2(2f, -2f);
        HorizontalLayoutGroup rootLayout = windowRoot.AddComponent<HorizontalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 16;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = false;
        rootLayout.childForceExpandHeight = true;

        // ── LEFT PANEL ────────────────────────────────────────────────────────
        RectTransform leftPanel = CreateStyledPanel("WorkersLeftPanel", windowRoot.transform, FleetPanelColor);
        driversScreenUi.LeftPanel = leftPanel;
        LayoutElement leftPanelLE = leftPanel.gameObject.AddComponent<LayoutElement>();
        leftPanelLE.preferredWidth = 360f;
        leftPanelLE.minWidth      = 360f;
        leftPanelLE.flexibleWidth  = 0f;
        leftPanelLE.flexibleHeight = 1f;
        VerticalLayoutGroup leftLayout = leftPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        leftLayout.padding = new RectOffset(16, 16, 16, 16);
        leftLayout.spacing = 16;
        leftLayout.childControlWidth = true;
        leftLayout.childControlHeight = true;
        leftLayout.childForceExpandWidth = true;
        leftLayout.childForceExpandHeight = false;

        // Header
        RectTransform headerRow = CreateLayoutRow("DriversHeaderRow", leftPanel, 40f, 0f);
        Text titleText = CreateHeaderText("DriversTitle", headerRow, font, "Workers", 24, TextAnchor.MiddleLeft, Color.white);
        titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        driversScreenUi.HeaderCountText = CreateHeaderText("DriversCount", headerRow, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        // Scrollable compact worker rows
        RectTransform listFrame = CreateStyledPanel("WorkersListFrame", leftPanel, FleetInsetColor);
        listFrame.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        GameObject scrollObj = CreateUiObject("WorkersScrollView", listFrame);
        StretchRect(scrollObj.GetComponent<RectTransform>(), 8f, 8f, 8f, 8f);
        scrollObj.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 28f;
        GameObject viewportObj = CreateUiObject("Viewport", scrollObj.transform);
        StretchRect(viewportObj.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        Image vpImg = viewportObj.AddComponent<Image>();
        vpImg.color = new Color(0f, 0f, 0f, 0.04f);
        vpImg.raycastTarget = true;
        viewportObj.AddComponent<Mask>().showMaskGraphic = false;
        GameObject contentObj = CreateUiObject("Content", viewportObj.transform);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 8f;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewportObj.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
        driversScreenUi.WorkerListContent = contentRect;

        for (int i = 0; i < MaxDriverCardSlots; i++)
            driversScreenUi.WorkerRows.Add(CreateWorkerRow(contentRect, font, i));

        // Hire section
        RectTransform hireSection = CreateStyledPanel("HireSection", leftPanel, FleetInsetColor);
        hireSection.gameObject.AddComponent<LayoutElement>().preferredHeight = 96f;
        VerticalLayoutGroup hireLayout = hireSection.gameObject.AddComponent<VerticalLayoutGroup>();
        hireLayout.padding = new RectOffset(18, 18, 16, 14);
        hireLayout.spacing = 10;
        hireLayout.childAlignment = TextAnchor.MiddleCenter;
        hireLayout.childControlWidth = true;
        hireLayout.childControlHeight = true;
        hireLayout.childForceExpandWidth = true;
        hireLayout.childForceExpandHeight = false;
        driversScreenUi.HireButton = CreateButton("HireDriverButton", hireSection, font, out driversScreenUi.HireButtonText, "Hire New Worker", 16, FleetPrimaryButtonColor, Color.white);
        driversScreenUi.HireButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        driversScreenUi.HireButton.onClick.AddListener(() =>
        {
            LogUiInput("Drivers Canvas: clicked Hire New Driver");
            isHireWorkerHighlightPersistent = false;
            if (isTutorialOpen && activeTutorialTrigger == TutorialTrigger.WorkersPanelOpened)
            {
                isTutorialOpen     = false;
                isTutorialSideMode = false;
            }
            HireNewDriver();
            isDriversScreenDirty = true;
        });
        driversScreenUi.HireStatusText = CreateBodyText("HireStatus", hireSection, font, string.Empty, 12, TextAnchor.MiddleCenter, FleetSecondaryTextColor);

        // ── RIGHT PANEL ───────────────────────────────────────────────────────
        RectTransform rightPanel = CreateStyledPanel("WorkersDetailPanel", windowRoot.transform, FleetPanelColor);
        driversScreenUi.RightPanel = rightPanel;
        LayoutElement rightPanelLE = rightPanel.gameObject.AddComponent<LayoutElement>();
        rightPanelLE.flexibleWidth  = 1f;
        rightPanelLE.flexibleHeight = 1f;
        VerticalLayoutGroup rightLayout = rightPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        rightLayout.padding = new RectOffset(18, 18, 18, 18);
        rightLayout.spacing = 14;
        rightLayout.childControlWidth = true;
        rightLayout.childControlHeight = true;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = false;

        // Placeholder card (shown when nothing selected)
        RectTransform placeholderCard = CreateSectionCard(rightPanel, font, string.Empty, out RectTransform placeholderBody, false);
        placeholderCard.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        VerticalLayoutGroup plBodyLayout = placeholderBody.GetComponent<VerticalLayoutGroup>();
        plBodyLayout.childAlignment = TextAnchor.MiddleCenter;
        plBodyLayout.childForceExpandHeight = true;
        CreateBodyText("DetailPlaceholder", placeholderBody, font, "Select a worker from the list to view details.", 15, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
        driversScreenUi.DetailPlaceholderCard = placeholderCard.gameObject;

        // Detail content (hidden until a worker is selected)
        GameObject detailRoot = CreateUiObject("WorkerDetailRoot", rightPanel);
        driversScreenUi.DetailContentRoot = detailRoot;
        detailRoot.AddComponent<LayoutElement>().flexibleHeight = 1f;
        VerticalLayoutGroup detailGroup = detailRoot.AddComponent<VerticalLayoutGroup>();
        detailGroup.spacing = 14;
        detailGroup.childControlWidth = true;
        detailGroup.childControlHeight = true;
        detailGroup.childForceExpandWidth = true;
        detailGroup.childForceExpandHeight = false;

        // Name + status badge header row
        RectTransform detailHeaderRow = CreateLayoutRow("DetailHeaderRow", detailRoot.transform, 36f, 12f);
        driversScreenUi.DetailNameText = CreateHeaderText("DetailName", detailHeaderRow, font, string.Empty, 22, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailNameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        RectTransform statusBadge = CreateStyledPanel("DetailStatusBadge", detailHeaderRow, new Color(0.24f, 0.29f, 0.36f, 1f));
        driversScreenUi.DetailStatusBadge = statusBadge.GetComponent<Image>();
        statusBadge.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;
        driversScreenUi.DetailStatusText = CreateBodyText("DetailStatus", statusBadge, font, string.Empty, 12, TextAnchor.MiddleCenter, Color.white);
        driversScreenUi.DetailStatusText.fontStyle = FontStyle.Bold;

        // Profile card
        RectTransform profileCard = CreateStyledPanel("WorkerProfileCard", detailRoot.transform, FleetInsetColor);
        profileCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 104f;
        HorizontalLayoutGroup profileLayout = profileCard.gameObject.AddComponent<HorizontalLayoutGroup>();
        profileLayout.padding = new RectOffset(16, 16, 10, 10);
        profileLayout.spacing = 14f;
        profileLayout.childAlignment = TextAnchor.MiddleLeft;
        profileLayout.childControlWidth = true;
        profileLayout.childControlHeight = true;
        profileLayout.childForceExpandWidth = false;
        profileLayout.childForceExpandHeight = false;

        driversScreenUi.DetailPortraitRoot = CreateUiObject("WorkerPortraitRoot", profileCard).GetComponent<RectTransform>();
        LayoutElement portraitRootLayout = driversScreenUi.DetailPortraitRoot.gameObject.AddComponent<LayoutElement>();
        portraitRootLayout.preferredWidth = 112f;
        portraitRootLayout.preferredHeight = 98f;
        portraitRootLayout.minWidth = 112f;
        portraitRootLayout.minHeight = 98f;

        RectTransform profileInfoColumn = CreateUiObject("WorkerProfileInfoColumn", profileCard).GetComponent<RectTransform>();
        LayoutElement profileInfoLayoutElement = profileInfoColumn.gameObject.AddComponent<LayoutElement>();
        profileInfoLayoutElement.flexibleWidth = 1f;
        VerticalLayoutGroup profileInfoLayout = profileInfoColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        profileInfoLayout.spacing = 6f;
        profileInfoLayout.childControlWidth = true;
        profileInfoLayout.childControlHeight = true;
        profileInfoLayout.childForceExpandWidth = true;
        profileInfoLayout.childForceExpandHeight = false;

        driversScreenUi.DetailProfileTitleText = CreateHeaderText("WorkerProfileTitle", profileInfoColumn, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailProfileTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        driversScreenUi.DetailRoleText = CreateHeaderText("WorkerRole", profileInfoColumn, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailRoleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        // Condition row 1: Skills | Effects
        RectTransform conditionTopRow = CreateLayoutRow("WorkerConditionTopRow", detailRoot.transform, 118f, 14f);

        RectTransform skillsCard = CreateStyledPanel("WorkerSkillsCard", conditionTopRow, FleetInsetColor);
        skillsCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup skillsCardLayout = skillsCard.gameObject.AddComponent<VerticalLayoutGroup>();
        skillsCardLayout.padding = new RectOffset(16, 16, 10, 10);
        skillsCardLayout.spacing = 4f;
        skillsCardLayout.childControlWidth = true;
        skillsCardLayout.childControlHeight = true;
        skillsCardLayout.childForceExpandWidth = true;
        skillsCardLayout.childForceExpandHeight = false;

        RectTransform skillsColumn = CreateUiObject("WorkerStatsColumn", skillsCard).GetComponent<RectTransform>();
        LayoutElement skillsColumnLayoutElement = skillsColumn.gameObject.AddComponent<LayoutElement>();
        skillsColumnLayoutElement.flexibleWidth = 1f;
        VerticalLayoutGroup skillsLayout = skillsColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        skillsLayout.spacing = 4f;
        skillsLayout.childControlWidth = true;
        skillsLayout.childControlHeight = true;
        skillsLayout.childForceExpandWidth = true;
        skillsLayout.childForceExpandHeight = false;
        driversScreenUi.DetailSkillsTitleText = CreateHeaderText("WorkerStatsTitle", skillsColumn, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailSkillsTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        driversScreenUi.DetailDrivingSkillText = CreateBodyText("WorkerDrivingSkill", skillsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailDrivingSkillText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        ConfigureWorkerSkillTooltip(driversScreenUi.DetailDrivingSkillText, WorkerSkillKind.Driving);
        driversScreenUi.DetailStaminaSkillText = CreateBodyText("WorkerStaminaSkill", skillsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailStaminaSkillText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        ConfigureWorkerSkillTooltip(driversScreenUi.DetailStaminaSkillText, WorkerSkillKind.Stamina);
        driversScreenUi.DetailProductionSkillText = CreateBodyText("WorkerProductionSkill", skillsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailProductionSkillText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        ConfigureWorkerSkillTooltip(driversScreenUi.DetailProductionSkillText, WorkerSkillKind.Production);
        driversScreenUi.DetailLogisticsSkillText = CreateBodyText("WorkerLogisticsSkill", skillsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailLogisticsSkillText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        ConfigureWorkerSkillTooltip(driversScreenUi.DetailLogisticsSkillText, WorkerSkillKind.Logistics);

        RectTransform effectsCard = CreateStyledPanel("WorkerEffectsCard", conditionTopRow, FleetInsetColor);
        effectsCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup effectsCardLayout = effectsCard.gameObject.AddComponent<VerticalLayoutGroup>();
        effectsCardLayout.padding = new RectOffset(16, 16, 10, 10);
        effectsCardLayout.spacing = 4f;
        effectsCardLayout.childControlWidth = true;
        effectsCardLayout.childControlHeight = true;
        effectsCardLayout.childForceExpandWidth = true;
        effectsCardLayout.childForceExpandHeight = false;

        RectTransform effectsColumn = CreateUiObject("WorkerEffectsColumn", effectsCard).GetComponent<RectTransform>();
        effectsColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup effectsLayout = effectsColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        effectsLayout.spacing = 4f;
        effectsLayout.childControlWidth = true;
        effectsLayout.childControlHeight = true;
        effectsLayout.childForceExpandWidth = true;
        effectsLayout.childForceExpandHeight = false;
        driversScreenUi.DetailEffectsTitleText = CreateHeaderText("WorkerEffectsTitle", effectsColumn, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailEffectsTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        driversScreenUi.DetailEffectsListRoot = CreateUiObject("WorkerEffectsList", effectsColumn).GetComponent<RectTransform>();
        LayoutElement effectsListLayout = driversScreenUi.DetailEffectsListRoot.gameObject.AddComponent<LayoutElement>();
        effectsListLayout.preferredHeight = 64f;
        effectsListLayout.flexibleHeight = 1f;
        VerticalLayoutGroup effectsListGroup = driversScreenUi.DetailEffectsListRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        effectsListGroup.spacing = 1f;
        effectsListGroup.childControlWidth = true;
        effectsListGroup.childControlHeight = true;
        effectsListGroup.childForceExpandWidth = true;
        effectsListGroup.childForceExpandHeight = false;

        driversScreenUi.DetailEffectsEmptyText = CreateBodyText("WorkerEffectsEmpty", driversScreenUi.DetailEffectsListRoot, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailEffectsEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        for (int i = 0; i < WorkerEffectHudRowCount; i++)
        {
            Text effectText = CreateBodyText($"WorkerEffectRow{i + 1}", driversScreenUi.DetailEffectsListRoot, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
            effectText.gameObject.AddComponent<LayoutElement>().preferredHeight = 15f;
            effectText.gameObject.SetActive(false);
            driversScreenUi.DetailEffectTexts.Add(effectText);
        }

        // Condition row 2: Needs | Perks
        RectTransform conditionBottomRow = CreateLayoutRow("WorkerConditionBottomRow", detailRoot.transform, 104f, 14f);

        RectTransform needsCard = CreateStyledPanel("WorkerNeedsCard", conditionBottomRow, FleetInsetColor);
        needsCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup needsCardLayout = needsCard.gameObject.AddComponent<VerticalLayoutGroup>();
        needsCardLayout.padding = new RectOffset(16, 16, 8, 8);
        needsCardLayout.spacing = 4f;
        needsCardLayout.childControlWidth = true;
        needsCardLayout.childControlHeight = true;
        needsCardLayout.childForceExpandWidth = true;
        needsCardLayout.childForceExpandHeight = false;

        RectTransform needsColumn = CreateUiObject("WorkerNeedsColumn", needsCard).GetComponent<RectTransform>();
        needsColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup needsLayout = needsColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        needsLayout.spacing = 4f;
        needsLayout.childControlWidth = true;
        needsLayout.childControlHeight = true;
        needsLayout.childForceExpandWidth = true;
        needsLayout.childForceExpandHeight = false;
        driversScreenUi.DetailNeedsTitleText = CreateHeaderText("WorkerNeedsTitle", needsColumn, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailNeedsTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        driversScreenUi.DetailMealNeedText = CreateBodyText("WorkerMealNeed", needsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailMealNeedText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        driversScreenUi.DetailSleepNeedText = CreateBodyText("WorkerSleepNeed", needsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailSleepNeedText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        driversScreenUi.DetailLeisureNeedText = CreateBodyText("WorkerLeisureNeed", needsColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailLeisureNeedText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        RectTransform perksCard = CreateStyledPanel("WorkerPerksCard", conditionBottomRow, FleetInsetColor);
        perksCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup perksCardLayout = perksCard.gameObject.AddComponent<VerticalLayoutGroup>();
        perksCardLayout.padding = new RectOffset(16, 16, 8, 8);
        perksCardLayout.spacing = 4f;
        perksCardLayout.childControlWidth = true;
        perksCardLayout.childControlHeight = true;
        perksCardLayout.childForceExpandWidth = true;
        perksCardLayout.childForceExpandHeight = false;

        RectTransform perksColumn = CreateUiObject("WorkerPerksColumn", perksCard).GetComponent<RectTransform>();
        perksColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup perksLayout = perksColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        perksLayout.spacing = 4f;
        perksLayout.childControlWidth = true;
        perksLayout.childControlHeight = true;
        perksLayout.childForceExpandWidth = true;
        perksLayout.childForceExpandHeight = false;
        driversScreenUi.DetailPerksTitleText = CreateHeaderText("WorkerPerksTitle", perksColumn, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailPerksTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        driversScreenUi.DetailPerksEmptyText = CreateBodyText("WorkerPerksEmpty", perksColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailPerksEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        for (int i = 0; i < WorkerPerkHudRowCount; i++)
        {
            Text perkText = CreateBodyText($"WorkerPerkRow{i + 1}", perksColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
            perkText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
            perkText.gameObject.SetActive(false);
            driversScreenUi.DetailPerkTexts.Add(perkText);
        }

        // Assignment info card — compact rows, label refs stored for post-localization update
        RectTransform assignCard = CreateSectionCard(detailRoot.transform, font, string.Empty, out RectTransform assignBody, false);
        assignCard.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(16, 16, 10, 10);
        assignCard.GetComponent<VerticalLayoutGroup>().spacing = 4;
        assignBody.GetComponent<VerticalLayoutGroup>().spacing = 4;
        driversScreenUi.DetailWorkTitleText = CreateHeaderText("WorkerWorkTitle", assignBody, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailWorkTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform assignRow = CreateLayoutRow("AssignRow", assignBody, 20f, 12f);
        driversScreenUi.DetailAssignmentLabel = CreateBodyText("AL", assignRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailAssignmentLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailAssignmentValue = CreateHeaderText("AV", assignRow, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        driversScreenUi.DetailAssignmentValue.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform shiftRow = CreateLayoutRow("ShiftRow", assignBody, 20f, 12f);
        driversScreenUi.DetailShiftLabel = CreateBodyText("SL", shiftRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailShiftLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailShiftText = CreateHeaderText("SV", shiftRow, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailShiftText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform dutyRow = CreateLayoutRow("DutyRow", assignBody, 20f, 12f);
        driversScreenUi.DetailDutyLabel = CreateBodyText("DL", dutyRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailDutyLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailDutyStateText = CreateBodyText("DV", dutyRow, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        driversScreenUi.DetailDutyStateText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Stats card (salary, balance) — compact
        RectTransform statsCard = CreateSectionCard(detailRoot.transform, font, string.Empty, out RectTransform statsBody, false);
        statsCard.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(16, 16, 10, 10);
        statsCard.GetComponent<VerticalLayoutGroup>().spacing = 4;
        statsBody.GetComponent<VerticalLayoutGroup>().spacing = 6;
        driversScreenUi.DetailContractTitleText = CreateHeaderText("WorkerContractTitle", statsBody, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailContractTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform salaryRow = CreateLayoutRow("SalaryEditRow", statsBody, 28f, 6f);
        driversScreenUi.DetailSalaryLabel = CreateBodyText("SRL", salaryRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailSalaryLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailSalaryMinusBtn = CreateButton("SalaryMinus", salaryRow, font, out Text minusTxt, "-", 14, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
        minusTxt.fontStyle = FontStyle.Bold;
        LayoutElement minusLE = driversScreenUi.DetailSalaryMinusBtn.gameObject.AddComponent<LayoutElement>();
        minusLE.preferredWidth = 28f; minusLE.preferredHeight = 24f;
        RectTransform salaryValPanel = CreateStyledPanel("SalaryValPanel", salaryRow, new Color(0.09f, 0.13f, 0.19f, 1f));
        LayoutElement salaryValLE = salaryValPanel.gameObject.AddComponent<LayoutElement>();
        salaryValLE.preferredWidth = 80f; salaryValLE.preferredHeight = 24f;
        driversScreenUi.DetailSalaryText = CreateHeaderText("SalaryVal", salaryValPanel, font, string.Empty, 13, TextAnchor.MiddleCenter, Color.white);
        driversScreenUi.DetailSalaryPlusBtn = CreateButton("SalaryPlus", salaryRow, font, out Text plusTxt, "+", 14, new Color(0.24f, 0.38f, 0.24f, 1f), Color.white);
        plusTxt.fontStyle = FontStyle.Bold;
        LayoutElement plusLE = driversScreenUi.DetailSalaryPlusBtn.gameObject.AddComponent<LayoutElement>();
        plusLE.preferredWidth = 28f; plusLE.preferredHeight = 24f;
        CreateBodyText("PerShift", salaryRow, font, "/ shift", 12, TextAnchor.MiddleLeft, FleetMutedTextColor).gameObject.AddComponent<LayoutElement>().preferredWidth = 44f;
        driversScreenUi.DetailSalaryMinusBtn.onClick.AddListener(() =>
        {
            DriverAgent d = driverAgents.Find(x => x.DriverId == selectedWorkerPanelDriverId);
            if (d == null) return;
            d.Salary = Mathf.Max(0, d.Salary - 25);
            isDriversScreenDirty = true;
        });
        driversScreenUi.DetailSalaryPlusBtn.onClick.AddListener(() =>
        {
            DriverAgent d = driverAgents.Find(x => x.DriverId == selectedWorkerPanelDriverId);
            if (d == null) return;
            d.Salary += 25;
            isDriversScreenDirty = true;
        });

        RectTransform balanceRow = CreateLayoutRow("BalanceRow", statsBody, 20f, 12f);
        driversScreenUi.DetailBalanceLabel = CreateBodyText("BL", balanceRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailBalanceLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;
        driversScreenUi.DetailBalanceText = CreateHeaderText("BV", balanceRow, font, string.Empty, 14, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailBalanceText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Focus button
        driversScreenUi.DetailFocusButton = CreateButton("FocusWorkerBtn", detailRoot.transform, font, out driversScreenUi.DetailFocusButtonText, "Focus on Worker", 14, new Color(0.25f, 0.33f, 0.46f, 1f), Color.white);
        driversScreenUi.DetailFocusButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        driversScreenUi.DetailFocusButton.onClick.AddListener(() =>
        {
            DriverAgent d = driverAgents.Find(x => x.DriverId == selectedWorkerPanelDriverId);
            if (d == null) return;
            isDriversPanelOpen = false;
            isDriversScreenDirty = true;
            FocusDriver(d.DriverId);
            if (d.DriverObject != null && d.DriverObject.activeSelf)
            {
                Vector3 pos = d.DriverObject.transform.position;
                cameraFocusPoint = new Vector3(pos.x, 0f, pos.z);
            }
        });

        detailRoot.SetActive(false);
        SetupWorkerSkillTooltip(windowRect, font);

        AddOverlayCloseButton(windowRect, font);
        driversScreenUi.CanvasRoot.SetActive(false);
        UpdateDriversScreenUi();
    }

    private WorkerRowUi CreateWorkerRow(RectTransform parent, Font font, int index)
    {
        WorkerRowUi row = new();
        GameObject rowObj = CreateUiObject($"WorkerRow{index + 1}", parent);
        row.Root = rowObj.GetComponent<RectTransform>();
        rowObj.AddComponent<LayoutElement>().preferredHeight = 80f;
        row.Background = rowObj.AddComponent<Image>();
        row.Background.color = DriversCardColor;
        Outline outline = rowObj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.22f);
        outline.effectDistance = new Vector2(1f, -1f);
        VerticalLayoutGroup rowLayout = rowObj.AddComponent<VerticalLayoutGroup>();
        rowLayout.padding = new RectOffset(14, 14, 10, 10);
        rowLayout.spacing = 5;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;

        RectTransform nameRow = CreateLayoutRow($"WorkerNameRow{index}", rowObj.transform, 22f, 8f);
        row.NameText = CreateHeaderText("Name", nameRow, font, string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
        row.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        RectTransform badge = CreateStyledPanel($"WorkerStatusBadge{index}", nameRow, new Color(0.24f, 0.29f, 0.36f, 1f));
        row.StatusBadgeBg = badge.GetComponent<Image>();
        badge.gameObject.AddComponent<LayoutElement>().preferredWidth = 112f;
        row.StatusText = CreateBodyText("Status", badge, font, string.Empty, 10, TextAnchor.MiddleCenter, Color.white);
        row.StatusText.fontStyle = FontStyle.Bold;

        row.SubText = CreateBodyText("SubText", rowObj.transform, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        row.SubText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        RectTransform needsRow = CreateUiObject($"NeedsRow{index}", rowObj.transform).GetComponent<RectTransform>();
        HorizontalLayoutGroup needsLayout = needsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        needsLayout.spacing = 6f;
        needsLayout.childAlignment = TextAnchor.MiddleLeft;
        needsLayout.childControlWidth = false;
        needsLayout.childControlHeight = false;
        needsLayout.childForceExpandWidth = false;
        needsLayout.childForceExpandHeight = false;
        needsRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 10f;
        row.NeedsMealBarFill    = CreateNeedsMiniBar(needsRow, GetNeedsMealIcon(),    $"Meal{index}",    60f);
        row.NeedsSleepBarFill   = CreateNeedsMiniBar(needsRow, GetNeedsSleepIcon(),   $"Sleep{index}",   60f);
        row.NeedsLeisureBarFill = CreateNeedsMiniBar(needsRow, GetNeedsLeisureIcon(), $"Leisure{index}", 60f);

        row.SelectButton = rowObj.AddComponent<Button>();
        ColorBlock rowColors = row.SelectButton.colors;
        rowColors.normalColor = Color.white;
        rowColors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
        rowColors.pressedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
        rowColors.selectedColor = Color.white;
        rowColors.fadeDuration = 0.08f;
        row.SelectButton.colors = rowColors;

        int rowIndex = index;
        row.SelectButton.onClick.AddListener(() =>
        {
            if (rowIndex >= driverAgents.Count) return;
            DriverAgent d = driverAgents[rowIndex];
            selectedWorkerPanelDriverId = selectedWorkerPanelDriverId == d.DriverId ? 0 : d.DriverId;
            PlayUiSound(uiSelectClip, 0.8f);
            isDriversScreenDirty = true;
        });

        return row;
    }

    private DriverCardUi CreateDriverCard(RectTransform parent, Font font, int cardIndex)
    {
        DriverCardUi card = new();
        GameObject cardObj = CreateUiObject($"DriverCard{cardIndex + 1}", parent);
        card.Root = cardObj.GetComponent<RectTransform>();
        card.Root.anchorMin = new Vector2(0f, 1f);
        card.Root.anchorMax = new Vector2(1f, 1f);
        card.Root.pivot = new Vector2(0.5f, 1f);
        card.Root.sizeDelta = new Vector2(0f, 128f);
        card.Root.anchoredPosition = Vector2.zero;
        LayoutElement cardLayoutElement = cardObj.AddComponent<LayoutElement>();
        cardLayoutElement.preferredHeight = 128f;
        cardLayoutElement.flexibleWidth = 1f;
        card.Background = cardObj.AddComponent<Image>();
        card.Background.color = DriversCardColor;
        Outline cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0f, 0f, 0f, 0.22f);
        cardOutline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup cardLayout = cardObj.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(16, 16, 12, 12);
        cardLayout.spacing = 6;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        // Row 1: Name + Status
        RectTransform nameRow = CreateLayoutRow($"NameRow{cardIndex}", cardObj.transform, 22f, 8f);
        card.NameText = CreateHeaderText("Name", nameRow, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        card.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        card.StatusText = CreateBodyText("Status", nameRow, font, string.Empty, 12, TextAnchor.MiddleRight, FleetMutedTextColor);
        card.StatusText.gameObject.AddComponent<LayoutElement>().preferredWidth = 180f;

        // Row 2: Truck
        card.TruckText = CreateBodyText("Truck", cardObj.transform, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        card.TruckText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        // Row 3: Salary + Balance
        RectTransform salaryRow = CreateLayoutRow($"SalaryRow{cardIndex}", cardObj.transform, 28f, 6f);

        CreateBodyText("SalaryLabel", salaryRow, font, "Salary:", 13, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 56f;

        card.SalaryText = CreateHeaderText("SalaryValue", salaryRow, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        card.SalaryText.gameObject.AddComponent<LayoutElement>().preferredWidth = 46f;

        int ci = cardIndex;
        Button minusBtn = CreateButton($"SalaryMinus{cardIndex}", salaryRow, font, out Text minusTxt, "−", 14, new Color(0.32f, 0.26f, 0.18f, 1f), Color.white);
        minusTxt.fontStyle = FontStyle.Bold;
        LayoutElement minusLayout = minusBtn.gameObject.AddComponent<LayoutElement>();
        minusLayout.preferredWidth = 26f;
        minusLayout.preferredHeight = 26f;
        minusBtn.onClick.AddListener(() =>
        {
            if (ci >= driverAgents.Count) return;
            driverAgents[ci].Salary = Mathf.Max(0, driverAgents[ci].Salary - 25);
            LogUiInput($"Drivers Canvas: {driverAgents[ci].DriverName} salary decreased to ${driverAgents[ci].Salary}");
            isDriversScreenDirty = true;
        });

        Button plusBtn = CreateButton($"SalaryPlus{cardIndex}", salaryRow, font, out Text plusTxt, "+", 14, new Color(0.24f, 0.38f, 0.24f, 1f), Color.white);
        plusTxt.fontStyle = FontStyle.Bold;
        LayoutElement plusLayout = plusBtn.gameObject.AddComponent<LayoutElement>();
        plusLayout.preferredWidth = 26f;
        plusLayout.preferredHeight = 26f;
        plusBtn.onClick.AddListener(() =>
        {
            if (ci >= driverAgents.Count) return;
            driverAgents[ci].Salary += 25;
            LogUiInput($"Drivers Canvas: {driverAgents[ci].DriverName} salary increased to ${driverAgents[ci].Salary}");
            isDriversScreenDirty = true;
        });

        CreateBodyText("PerShift", salaryRow, font, "/shift", 12, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 42f;

        // Spacer
        GameObject spacer = CreateUiObject($"SalarySpacer{cardIndex}", salaryRow);
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;

        card.BalanceText = CreateBodyText("Balance", salaryRow, font, string.Empty, 13, TextAnchor.MiddleRight, FleetAccentColor);
        card.BalanceText.gameObject.AddComponent<LayoutElement>().preferredWidth = 140f;

        return card;
    }

    private DriverCardUi CreateDriverCardV2(RectTransform parent, Font font, int cardIndex)
    {
        DriverCardUi card = new();
        GameObject cardObj = CreateUiObject($"DriverCardV2_{cardIndex + 1}", parent);
        card.Root = cardObj.GetComponent<RectTransform>();
        card.Root.anchorMin = new Vector2(0f, 1f);
        card.Root.anchorMax = new Vector2(1f, 1f);
        card.Root.pivot = new Vector2(0.5f, 1f);
        card.Root.sizeDelta = new Vector2(0f, 170f);
        card.Root.anchoredPosition = Vector2.zero;

        LayoutElement cardLayoutElement = cardObj.AddComponent<LayoutElement>();
        cardLayoutElement.preferredHeight = 170f;
        cardLayoutElement.flexibleWidth = 1f;

        card.Background = cardObj.AddComponent<Image>();
        card.Background.color = DriversCardColor;
        Outline cardOutline = cardObj.AddComponent<Outline>();
        cardOutline.effectColor = new Color(0f, 0f, 0f, 0.22f);
        cardOutline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup cardLayout = cardObj.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(16, 16, 14, 14);
        cardLayout.spacing = 12;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow($"DriverCardHeader{cardIndex}", cardObj.transform, 26f, 10f);
        card.NameText = CreateHeaderText("Name", headerRow, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        card.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform statusBadge = CreateStyledPanel($"DriverStatusBadge{cardIndex}", headerRow, new Color(0.24f, 0.29f, 0.36f, 1f));
        card.StatusBadgeBackground = statusBadge.GetComponent<Image>();
        LayoutElement statusBadgeLayout = statusBadge.gameObject.AddComponent<LayoutElement>();
        statusBadgeLayout.preferredWidth = 90f;
        statusBadgeLayout.preferredHeight = 24f;
        card.StatusText = CreateBodyText("Status", statusBadge, font, string.Empty, 11, TextAnchor.MiddleCenter, Color.white);
        card.StatusText.fontStyle = FontStyle.Bold;

        int cii = cardIndex;
        card.SelectButton = CreateButton($"DriverSelectBtn{cardIndex}", headerRow, font, out Text selectTxt, "Select", 11, new Color(0.25f, 0.33f, 0.46f, 1f), Color.white);
        selectTxt.horizontalOverflow = HorizontalWrapMode.Overflow;
        LayoutElement selectLayout = card.SelectButton.gameObject.AddComponent<LayoutElement>();
        selectLayout.preferredWidth = 64f;
        selectLayout.preferredHeight = 24f;
        card.SelectButton.onClick.AddListener(() =>
        {
            if (cii >= driverAgents.Count) return;
            DriverAgent d = driverAgents[cii];
            isDriversPanelOpen = false;
            isDriversScreenDirty = true;
            FocusDriver(d.DriverId);
            if (d.DriverObject != null && d.DriverObject.activeSelf)
            {
                Vector3 pos = d.DriverObject.transform.position;
                cameraFocusPoint = new Vector3(pos.x, 0f, pos.z);
            }
        });

        RectTransform infoRow = CreateLayoutRow($"DriverCardInfo{cardIndex}", cardObj.transform, 52f, 12f);

        RectTransform truckPanel = CreateStyledPanel($"DriverTruckPanel{cardIndex}", infoRow, FleetCardMutedColor);
        truckPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup truckPanelLayout = truckPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        truckPanelLayout.padding = new RectOffset(12, 12, 8, 8);
        truckPanelLayout.spacing = 4;
        truckPanelLayout.childControlWidth = true;
        truckPanelLayout.childControlHeight = true;
        truckPanelLayout.childForceExpandWidth = true;
        truckPanelLayout.childForceExpandHeight = false;
        card.TruckLabelText = CreateBodyText("TruckLabel", truckPanel, font, "Truck", 11, TextAnchor.MiddleLeft, FleetMutedTextColor);
        card.TruckLabelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;
        card.TruckText = CreateBodyText("TruckValue", truckPanel, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        card.TruckText.fontStyle = FontStyle.Bold;
        card.TruckText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform footerRow = CreateLayoutRow($"DriverCardFooter{cardIndex}", cardObj.transform, 52f, 12f);

        RectTransform salaryPanel = CreateStyledPanel($"DriverSalaryPanel{cardIndex}", footerRow, FleetCardMutedColor);
        salaryPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup salaryPanelLayout = salaryPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        salaryPanelLayout.padding = new RectOffset(12, 12, 8, 8);
        salaryPanelLayout.spacing = 6;
        salaryPanelLayout.childControlWidth = true;
        salaryPanelLayout.childControlHeight = true;
        salaryPanelLayout.childForceExpandWidth = true;
        salaryPanelLayout.childForceExpandHeight = false;
        CreateBodyText("SalaryLabel", salaryPanel, font, "Salary", 11, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;

        RectTransform salaryRow = CreateLayoutRow($"DriverSalaryRow{cardIndex}", salaryPanel, 28f, 8f);
        int ci = cardIndex;

        Button minusBtn = CreateButton($"DriverSalaryMinus{cardIndex}", salaryRow, font, out Text minusTxt, "-", 13, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
        minusTxt.fontStyle = FontStyle.Bold;
        LayoutElement minusLayout = minusBtn.gameObject.AddComponent<LayoutElement>();
        minusLayout.preferredWidth = 30f;
        minusLayout.preferredHeight = 26f;

        RectTransform salaryValuePanel = CreateStyledPanel($"DriverSalaryValuePanel{cardIndex}", salaryRow, new Color(0.09f, 0.13f, 0.19f, 1f));
        LayoutElement salaryValueLayout = salaryValuePanel.gameObject.AddComponent<LayoutElement>();
        salaryValueLayout.flexibleWidth = 1f;
        salaryValueLayout.preferredHeight = 26f;
        card.SalaryText = CreateHeaderText("SalaryValue", salaryValuePanel, font, string.Empty, 12, TextAnchor.MiddleCenter, Color.white);

        minusBtn.onClick.AddListener(() =>
        {
            if (ci >= driverAgents.Count) return;
            driverAgents[ci].Salary = Mathf.Max(0, driverAgents[ci].Salary - 25);
            LogUiInput($"Drivers Canvas: {driverAgents[ci].DriverName} salary decreased to ${driverAgents[ci].Salary}");
            isDriversScreenDirty = true;
        });

        Button plusBtn = CreateButton($"DriverSalaryPlus{cardIndex}", salaryRow, font, out Text plusTxt, "+", 13, new Color(0.24f, 0.38f, 0.24f, 1f), Color.white);
        plusTxt.fontStyle = FontStyle.Bold;
        LayoutElement plusLayout = plusBtn.gameObject.AddComponent<LayoutElement>();
        plusLayout.preferredWidth = 30f;
        plusLayout.preferredHeight = 26f;
        plusBtn.onClick.AddListener(() =>
        {
            if (ci >= driverAgents.Count) return;
            driverAgents[ci].Salary += 25;
            LogUiInput($"Drivers Canvas: {driverAgents[ci].DriverName} salary increased to ${driverAgents[ci].Salary}");
            isDriversScreenDirty = true;
        });

        RectTransform balancePanel = CreateStyledPanel($"DriverBalancePanel{cardIndex}", footerRow, FleetCardMutedColor);
        LayoutElement balanceLayout = balancePanel.gameObject.AddComponent<LayoutElement>();
        balanceLayout.preferredWidth = 120f;
        VerticalLayoutGroup balancePanelLayout = balancePanel.gameObject.AddComponent<VerticalLayoutGroup>();
        balancePanelLayout.padding = new RectOffset(12, 12, 8, 8);
        balancePanelLayout.spacing = 4;
        balancePanelLayout.childControlWidth = true;
        balancePanelLayout.childControlHeight = true;
        balancePanelLayout.childForceExpandWidth = true;
        balancePanelLayout.childForceExpandHeight = false;
        CreateBodyText("BalanceLabel", balancePanel, font, "Balance", 11, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;
        card.BalanceText = CreateHeaderText("BalanceValue", balancePanel, font, string.Empty, 14, TextAnchor.MiddleLeft, FleetAccentColor);
        card.BalanceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        return card;
    }

    private static string Gend(DriverAgent d, string male, string female) =>
        d?.Gender == WorkerGender.Female ? female : male;

    private static string GetWorkerGenderLabel(DriverAgent driver, bool ru)
    {
        if (driver == null)
        {
            return "—";
        }

        return driver.Gender == WorkerGender.Female
            ? (ru ? "Женщина" : "Female")
            : (ru ? "Мужчина" : "Male");
    }

    private string GetWorkerListStatusLabel(DriverAgent driver, bool ru)
    {
        if (driver == null) return ru ? "Свободен" : "Idle";

        TruckAgent truck = GetAssignedTruckForDriver(driver);
        bool isProduction = driver.DutyMode == DriverDutyMode.Logistics;
        return driver.IsArrivingByBus ? (ru ? "В пути" : "Arriving")
            : IsDriverOnActiveTradeRun(driver) ? (ru ? "Торговый рейс" : "Trade Run")
            : IsDriverIntercity(driver) ? (ru ? "Межгород" : "Intercity")
            : IsDriverBusDriver(driver) ? (ru ? "Логистика" : "Logistics")
            : isProduction ? (ru ? "На производстве" : "Production")
            : truck != null ? (ru ? "На логистике" : "Logistics")
            : driver.RestPhase != DriverRestPhase.None ? (ru ? "Отдыхает" : "Resting")
            : (ru ? Gend(driver, "Свободен", "Свободна") : "Idle");
    }

    private string GetWorkerDutySummaryLabel(DriverAgent driver, bool ru)
    {
        if (driver == null) return "—";

        return driver.IsInsideBuilding ? (ru ? "Работает в здании" : "Inside building")
            : IsBusDriverOnActiveRoute(driver) ? (ru ? "На автобусном маршруте" : "On bus route")
            : driver.IsOnActiveShift ? (ru ? "На смене" : "On shift")
            : driver.RestPhase != DriverRestPhase.None ? (ru ? "Отдыхает" : "Resting")
            : IsDriverBusyWalkPhase(driver) ? (ru ? "В пути" : "Commuting")
            : IsDriverIntercity(driver) ? (ru ? "Ожидает межгород" : "Waiting for intercity")
            : IsDriverBusDriver(driver) ? (ru ? "Ожидает автобусный маршрут" : "Waiting for bus route")
            : (ru ? Gend(driver, "Свободен в мотеле", "Свободна в мотеле") : "Idle at motel");
    }

    private void UpdateDriversScreenUi()
    {
        if (driversScreenUi == null) return;

        bool shouldShow = isDriversPanelOpen;
        if (driversScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            driversScreenUi.CanvasRoot.SetActive(shouldShow);
            isDriversScreenDirty = true;
        }

        if (!shouldShow) return;
        if (!isDriversScreenDirty) return;

        driversScreenUi.HeaderCountText.text = $"{driverAgents.Count} {(driverAgents.Count == 1 ? L("Worker") : L("Workers"))}";

        // Left panel — compact row list
        for (int i = 0; i < driversScreenUi.WorkerRows.Count; i++)
        {
            WorkerRowUi row = driversScreenUi.WorkerRows[i];
            bool active = i < driverAgents.Count;
            row.Root.gameObject.SetActive(active);
            if (!active) continue;

            DriverAgent d = driverAgents[i];
            TruckAgent truck = GetAssignedTruckForDriver(d);
            row.DriverId = d.DriverId;
            bool isSelected  = selectedWorkerPanelDriverId == d.DriverId;
            bool isLogistics = d.DutyMode == DriverDutyMode.Logistics;
            row.Background.color = isSelected ? DriversCardSelected : DriversCardColor;

            row.NameText.text = d.DriverName;

            bool ru = IsRussianLanguage();
            string status = GetWorkerListStatusLabel(d, ru);
            row.StatusText.text = status;
            if (row.StatusBadgeBg != null)
            {
                row.StatusBadgeBg.color = d.IsArrivingByBus
                    ? new Color(0.22f, 0.36f, 0.54f, 1f)
                    : isLogistics  ? new Color(0.20f, 0.38f, 0.26f, 1f)
                    : truck != null ? new Color(0.35f, 0.29f, 0.14f, 1f)
                    : new Color(0.24f, 0.29f, 0.36f, 1f);
            }

            row.SubText.text = d.IsArrivingByBus              ? "On the way..."
                : isLogistics && d.AssignedBuildingType.HasValue ? GetSelectedLocationDisplayName(d.AssignedBuildingType.Value)
                : truck != null                                   ? truck.DisplayName
                : L(GetWorkerOccupationLabel(d));

            if (row.NeedsMealBarFill != null)
            {
                float mealPct    = Mathf.Clamp01(1f - d.HoursSinceMeal    / WorkerMealCriticalHours);
                float sleepPct   = Mathf.Clamp01(1f - d.HoursSinceSleep   / WorkerSleepCriticalHours);
                float leisurePct = Mathf.Clamp01(1f - d.HoursSinceLeisure / WorkerLeisureCriticalHours);
                row.NeedsMealBarFill.sizeDelta    = new Vector2(mealPct    * 60f, 0f);
                row.NeedsSleepBarFill.sizeDelta   = new Vector2(sleepPct   * 60f, 0f);
                row.NeedsLeisureBarFill.sizeDelta = new Vector2(leisurePct * 60f, 0f);
                row.NeedsMealBarFill.GetComponent<Image>().color    = GetNeedBarColor(mealPct);
                row.NeedsSleepBarFill.GetComponent<Image>().color   = GetNeedBarColor(sleepPct);
                row.NeedsLeisureBarFill.GetComponent<Image>().color = GetNeedBarColor(leisurePct);
            }
        }

        // Right panel — visibility toggle (done before LocalizeCanvas so layout is correct)
        DriverAgent sel = driverAgents.Find(d => d.DriverId == selectedWorkerPanelDriverId);
        bool hasSel = sel != null;
        if (driversScreenUi.DetailPlaceholderCard != null)
            driversScreenUi.DetailPlaceholderCard.SetActive(!hasSel);
        if (driversScreenUi.DetailContentRoot != null)
            driversScreenUi.DetailContentRoot.SetActive(hasSel);

        bool hasMotel = locations.ContainsKey(LocationType.Motel);
        bool canHire  = hasMotel && money >= HireDriverCost && hiringDriverArrival == null;
        driversScreenUi.HireButton.interactable = canHire;
        driversScreenUi.HireButtonText.text = $"{L("Hire New Worker")} — ${HireDriverCost}";
        driversScreenUi.HireStatusText.text = hiringDriverArrival != null
            ? L("Another worker is currently arriving by bus.")
            : !hasMotel
                ? L("Build a Motel first so new workers have somewhere to check in.")
            : canHire
                ? L("New hires arrive at the bus stop before checking in at the motel.")
                : $"{L("Need")} ${HireDriverCost} {L("to hire a new worker.")}";
        driversScreenUi.HireStatusText.color = canHire ? FleetSecondaryTextColor : new Color(0.96f, 0.72f, 0.42f, 1f);

        LayoutRebuilder.ForceRebuildLayoutImmediate(driversScreenUi.WorkerListContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(driversScreenUi.WindowRoot);

        // Localize static texts first — BEFORE setting detail panel strings so they aren't re-processed
        LocalizeCanvas(driversScreenUi.CanvasRoot);

        // Set right-panel detail texts after localization to avoid substring corruption
        if (hasSel)
        {
            bool ru = IsRussianLanguage();
            TruckAgent truck = GetAssignedTruckForDriver(sel);
            bool isLogistics = sel.DutyMode == DriverDutyMode.Logistics;

            driversScreenUi.DetailNameText.text = sel.DriverName;
            if (driversScreenUi.DetailProfileTitleText != null)
                driversScreenUi.DetailProfileTitleText.text = ru ? "Профиль" : "Profile";
            if (driversScreenUi.DetailRoleText != null)
                driversScreenUi.DetailRoleText.text = $"{L(GetWorkerOccupationLabel(sel))} • {GetWorkerGenderLabel(sel, ru)}";
            UpdateWorkerPortraitUi(sel);
            UpdateWorkerStatsUi(sel, ru);
            UpdateWorkerNeedsUi(sel, ru);

            string statusLabel = GetWorkerListStatusLabel(sel, ru);
            driversScreenUi.DetailStatusText.text = statusLabel;
            if (driversScreenUi.DetailStatusBadge != null)
            {
                driversScreenUi.DetailStatusBadge.color = sel.IsArrivingByBus
                    ? new Color(0.22f, 0.36f, 0.54f, 1f)
                    : isLogistics   ? new Color(0.20f, 0.38f, 0.26f, 1f)
                    : truck != null ? new Color(0.35f, 0.29f, 0.14f, 1f)
                    : new Color(0.24f, 0.29f, 0.36f, 1f);
            }

            // Labels (set post-localization so LocalizeCanvas can't corrupt them)
            if (driversScreenUi.DetailAssignmentLabel != null)
                driversScreenUi.DetailAssignmentLabel.text = isLogistics ? (ru ? "Здание" : "Building") : (ru ? "Грузовик" : "Truck");
            if (driversScreenUi.DetailShiftLabel != null)
                driversScreenUi.DetailShiftLabel.text = ru ? "Смена" : "Shift";
            if (driversScreenUi.DetailDutyLabel != null)
                driversScreenUi.DetailDutyLabel.text = ru ? "Статус" : "Status";
            if (driversScreenUi.DetailWorkTitleText != null)
                driversScreenUi.DetailWorkTitleText.text = ru ? "Работа" : "Work";
            if (driversScreenUi.DetailSalaryLabel != null)
                driversScreenUi.DetailSalaryLabel.text = ru ? "Зарплата" : "Salary";
            if (driversScreenUi.DetailBalanceLabel != null)
                driversScreenUi.DetailBalanceLabel.text = ru ? "Баланс" : "Balance";
            if (driversScreenUi.DetailContractTitleText != null)
                driversScreenUi.DetailContractTitleText.text = ru ? "Контракт" : "Contract";

            // Values
            driversScreenUi.DetailAssignmentValue.text = isLogistics && sel.AssignedBuildingType.HasValue
                ? GetSelectedLocationDisplayName(sel.AssignedBuildingType.Value)
                : truck != null ? truck.DisplayName : "—";

            bool hasShift = sel.ShiftStartHour >= 0;
            driversScreenUi.DetailShiftText.text = isLogistics ? GetProductionWorkRangeLabel()
                : hasShift                        ? GetShiftRangeLabel(sel.ShiftStartHour)
                : (ru ? "Не назначена" : "Not assigned");
            driversScreenUi.DetailShiftText.color = (hasShift || isLogistics) ? FleetAccentColor : FleetMutedTextColor;

            string dutyState = GetWorkerDutySummaryLabel(sel, ru);
            driversScreenUi.DetailDutyStateText.text = dutyState;

            driversScreenUi.DetailSalaryText.text  = $"${sel.Salary}";
            driversScreenUi.DetailBalanceText.text = $"${sel.Money}";

            bool canFocus = sel.DriverObject != null && sel.DriverObject.activeSelf;
            driversScreenUi.DetailFocusButton.interactable = canFocus;
            driversScreenUi.DetailFocusButtonText.text = canFocus
                ? (ru ? $"Следить за {sel.DriverName}" : $"Focus on {sel.DriverName}")
                : (ru ? $"{sel.DriverName} внутри здания" : $"{sel.DriverName} is inside");
        }

        isDriversScreenDirty = false;
    }

    private void SetupShiftsScreenUi()
    {
        if (shiftsScreenUi != null) return;

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        shiftsScreenUi = new ShiftsScreenUiRefs();

        GameObject canvasObject = new("ShiftsScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        shiftsScreenUi.CanvasRoot = canvasObject;

        GameObject windowRoot = CreateUiObject("ShiftsWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        SetCenteredWindow(windowRect, ShiftsWindowWidth, ShiftsWindowHeight, -16f);
        shiftsScreenUi.WindowRoot = windowRect;

        Image windowBg = windowRoot.AddComponent<Image>();
        windowBg.color = ShiftsScreenTint;
        Outline windowOutline = windowRoot.AddComponent<Outline>();
        windowOutline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        windowOutline.effectDistance = new Vector2(2f, -2f);

        HorizontalLayoutGroup rootLayout = windowRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 16;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = false;
        rootLayout.childForceExpandHeight = true;

        RectTransform leftPanel = CreateStyledPanel("ShiftsDriverListPanel", windowRoot.transform, FleetPanelColor);
        shiftsScreenUi.LeftPanel = leftPanel;
        LayoutElement leftPanelLayout = leftPanel.gameObject.AddComponent<LayoutElement>();
        leftPanelLayout.preferredWidth = ShiftsLeftPanelWidth;
        leftPanelLayout.minWidth = ShiftsLeftPanelWidth;
        leftPanelLayout.preferredHeight = ShiftsInnerPanelHeight;
        leftPanelLayout.minHeight = ShiftsInnerPanelHeight;
        leftPanelLayout.flexibleWidth = 0f;
        leftPanelLayout.flexibleHeight = 0f;
        VerticalLayoutGroup leftLayout = leftPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        leftLayout.padding = new RectOffset(16, 16, 16, 16);
        leftLayout.spacing = 14;
        leftLayout.childControlWidth = true;
        leftLayout.childControlHeight = true;
        leftLayout.childForceExpandWidth = true;
        leftLayout.childForceExpandHeight = false;

        RectTransform leftHeader = CreateLayoutRow("ShiftsHeaderRow", leftPanel, 40f, 0f);
        Text shiftsTitle = CreateHeaderText("ShiftsTitle", leftHeader, font, "Roles", 24, TextAnchor.MiddleLeft, Color.white);
        shiftsTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        shiftsScreenUi.TitleText = shiftsTitle;
        shiftsScreenUi.HeaderCountText = CreateHeaderText("ShiftsCount", leftHeader, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        RectTransform listFrame = CreateStyledPanel("ShiftsDriverListFrame", leftPanel, FleetInsetColor);
        LayoutElement listFrameLayout = listFrame.gameObject.AddComponent<LayoutElement>();
        listFrameLayout.flexibleHeight = 1f;
        listFrameLayout.minHeight = 320f;

        GameObject scrollObj = CreateUiObject("ShiftsDriverScrollView", listFrame);
        StretchRect(scrollObj.GetComponent<RectTransform>(), 8f, 8f, 8f, 8f);
        Image scrollImg = scrollObj.AddComponent<Image>();
        scrollImg.color = new Color(0f, 0f, 0f, 0f);
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 28f;

        GameObject viewportObj = CreateUiObject("Viewport", scrollObj.transform);
        StretchRect(viewportObj.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        Image viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.04f);
        Mask viewportMask = viewportObj.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        scrollRect.viewport = viewportObj.GetComponent<RectTransform>();

        GameObject contentObj = CreateUiObject("ShiftsDriverListContent", viewportObj.transform);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 12;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        scrollRect.content = contentRect;
        shiftsScreenUi.DriverListContent = contentRect;

        for (int i = 0; i < MaxShiftDriverSlots; i++)
        {
            ShiftDriverRowUi row = new();
            GameObject rowObj = CreateUiObject($"ShiftDriverRow{i + 1}", contentRect);
            row.Root = rowObj.GetComponent<RectTransform>();
            row.Root.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
            row.Background = rowObj.AddComponent<Image>();
            row.Background.color = ShiftsCardColor;
            Outline rowOutline = rowObj.AddComponent<Outline>();
            rowOutline.effectColor = new Color(0f, 0f, 0f, 0.2f);
            rowOutline.effectDistance = new Vector2(1f, -1f);
            VerticalLayoutGroup rowLayout = rowObj.AddComponent<VerticalLayoutGroup>();
            rowLayout.padding = new RectOffset(12, 12, 10, 10);
            rowLayout.spacing = 4;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            RectTransform workerHeaderRow = CreateLayoutRow($"ShiftDriverHeaderRow{i + 1}", row.Root, 24f, 8f);
            row.NameText = CreateHeaderText($"ShiftDriverName{i + 1}", workerHeaderRow, font, string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
            row.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            row.ProfessionText = CreateBodyText($"ShiftDriverProfession{i + 1}", workerHeaderRow, font, string.Empty, 12, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
            LayoutElement professionLayout = row.ProfessionText.gameObject.AddComponent<LayoutElement>();
            professionLayout.preferredWidth = 170f;
            professionLayout.flexibleWidth = 0f;
            row.StatusText = CreateBodyText($"ShiftDriverStatus{i + 1}", rowObj.transform, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);

            row.SelectButton = rowObj.AddComponent<Button>();
            ColorBlock rowColors = row.SelectButton.colors;
            rowColors.normalColor = Color.white;
            rowColors.highlightedColor = new Color(1f, 1f, 1f, 0.96f);
            rowColors.pressedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            rowColors.selectedColor = Color.white;
            rowColors.fadeDuration = 0.08f;
            row.SelectButton.colors = rowColors;

            int rowIndex = i;
            row.SelectButton.onClick.AddListener(() =>
            {
                if (rowIndex >= driverAgents.Count)
                {
                    return;
                }

                DriverAgent driver = driverAgents[rowIndex];
                bool isSelected = selectedShiftDriverId == driver.DriverId;
                selectedShiftDriverId = isSelected ? 0 : driver.DriverId;
                LogUiInput($"Shifts Canvas: {(isSelected ? $"deselected {driver.DriverName}" : $"selected {driver.DriverName}")}");
                PlayUiSound(uiSelectClip, 0.8f);
                isShiftsScreenDirty = true;
                isDriversScreenDirty = true;
                if (!isSelected)
                {
                    CompleteProductionWorkerSelectionTutorial(driver);
                }
            });

            shiftsScreenUi.DriverRows.Add(row);
        }

        // ── Right panel ─────────────────────────────────────────────────────
        RectTransform rightPanel = CreateStyledPanel("ShiftsCardsPanel", windowRoot.transform, FleetPanelColor);
        shiftsScreenUi.RightPanel = rightPanel;
        LayoutElement rightPanelLayout = rightPanel.gameObject.AddComponent<LayoutElement>();
        rightPanelLayout.preferredWidth = ShiftsRightPanelWidth;
        rightPanelLayout.minWidth = ShiftsRightPanelWidth;
        rightPanelLayout.flexibleWidth = 0f;
        rightPanelLayout.preferredHeight = ShiftsInnerPanelHeight;
        rightPanelLayout.minHeight = ShiftsInnerPanelHeight;
        rightPanelLayout.flexibleHeight = 0f;
        VerticalLayoutGroup rightLayout = rightPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        rightLayout.padding = new RectOffset(16, 16, 14, 16);
        rightLayout.spacing = 12;
        rightLayout.childControlWidth = true;
        rightLayout.childControlHeight = true;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = false;

        RectTransform selectionCard = CreateSectionCard(rightPanel, font, string.Empty, out RectTransform selectionBody, false);
        selectionCard.gameObject.AddComponent<LayoutElement>().preferredHeight = ShiftsSelectionCardHeight;
        shiftsScreenUi.SelectionTitleText = CreateHeaderText("AssignmentsSelectionTitle", selectionBody, font, "Selected Worker", 16, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.SelectionTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        RectTransform selectionInfoPanel = CreateStyledPanel("AssignmentsSelectionInfoPanel", selectionBody, FleetCardMutedColor);
        selectionInfoPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;
        VerticalLayoutGroup selectionInfoLayout = selectionInfoPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        selectionInfoLayout.padding = new RectOffset(12, 12, 10, 10);
        selectionInfoLayout.spacing = 4;
        selectionInfoLayout.childControlWidth = true;
        selectionInfoLayout.childControlHeight = true;
        selectionInfoLayout.childForceExpandWidth = true;
        selectionInfoLayout.childForceExpandHeight = false;
        shiftsScreenUi.SelectionNameText = CreateHeaderText("AssignmentsSelectionName", selectionInfoPanel, font, string.Empty, 15, TextAnchor.MiddleLeft, FleetAccentColor);
        shiftsScreenUi.SelectionNameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        shiftsScreenUi.SelectionNameText.verticalOverflow = VerticalWrapMode.Overflow;
        shiftsScreenUi.SelectionProfessionText = CreateBodyText("AssignmentsSelectionProfession", selectionInfoPanel, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        shiftsScreenUi.SelectionProfessionText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        shiftsScreenUi.SelectionProfessionText.verticalOverflow = VerticalWrapMode.Truncate;
        shiftsScreenUi.SelectionStatusText = CreateBodyText("AssignmentsSelectionStatus", selectionInfoPanel, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.SelectionStatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        shiftsScreenUi.SelectionStatusText.verticalOverflow = VerticalWrapMode.Truncate;
        shiftsScreenUi.SelectionHintText = CreateBodyText("AssignmentsSelectionHint", selectionBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        shiftsScreenUi.SelectionHintText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        // ── Tab toggle row ───────────────────────────────────────────────────
        RectTransform tabRow = CreateLayoutRow("ShiftsTabRow", rightPanel, ShiftsTabRowHeight, 8f);
        LayoutElement tabRowLE = tabRow.GetComponent<LayoutElement>();
        tabRowLE.minHeight = ShiftsTabRowHeight;
        tabRowLE.flexibleHeight = 0f;
        HorizontalLayoutGroup tabRowLayout = tabRow.GetComponent<HorizontalLayoutGroup>();
        tabRowLayout.childForceExpandWidth = true;
        tabRowLayout.childForceExpandHeight = true;
        shiftsTransportTabBtn = CreateButton("LogisticsTabBtn", tabRow, font, out shiftsTransportTabText, "Logistics", 13, FleetPrimaryButtonColor, Color.white);
        shiftsTransportTabText.fontStyle = FontStyle.Bold;
        shiftsTransportTabBtn.transition = Selectable.Transition.None;
        shiftsTransportTabBtn.onClick.AddListener(() =>
        {
            isLogisticsTabActive = false;
            ApplyShiftsTabVisuals();
            EnforceShiftsWindowLayout();
            LogShiftsHudState("clicked Logistics tab", force: true);
            isShiftsScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.8f);
        });
        shiftsLogisticsTabBtn = CreateButton("ProductionsTabBtn", tabRow, font, out shiftsLogisticsTabText, "Productions", 13, new Color(0.22f, 0.26f, 0.32f, 1f), Color.white);
        shiftsLogisticsTabText.fontStyle = FontStyle.Bold;
        shiftsLogisticsTabBtn.transition = Selectable.Transition.None;
        shiftsLogisticsTabBtn.onClick.AddListener(() =>
        {
            isLogisticsTabActive = true;
            ApplyShiftsTabVisuals();
            EnforceShiftsWindowLayout();
            LogShiftsHudState("clicked Productions tab", force: true);
            isShiftsScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.8f);
        });

        // ── Transportation panel (existing shift cards + intercity) ──────────
        GameObject transportPanelObj = CreateUiObject("TransportPanel", rightPanel);
        shiftsTransportPanel = transportPanelObj.GetComponent<RectTransform>();
        LayoutElement transportPanelLayout = transportPanelObj.AddComponent<LayoutElement>();
        transportPanelLayout.preferredHeight = GetShiftsTabContentHeight();
        transportPanelLayout.minHeight = GetShiftsTabContentHeight();
        transportPanelLayout.flexibleHeight = 0f;
        ScrollRect transportScroll = transportPanelObj.AddComponent<ScrollRect>();
        shiftsScreenUi.TransportScrollRect = transportScroll;
        transportScroll.horizontal = false; transportScroll.vertical = true;
        transportScroll.movementType = ScrollRect.MovementType.Clamped;
        transportScroll.scrollSensitivity = 30f; transportScroll.inertia = false;
        transportScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        GameObject transportViewportObj = CreateUiObject("Viewport", transportPanelObj.transform);
        RectTransform transportViewport = transportViewportObj.GetComponent<RectTransform>();
        StretchRect(transportViewport, 0f, 18f, 0f, 0f);
        Image transportViewportImage = transportViewportObj.AddComponent<Image>();
        transportViewportImage.color = new Color(0f, 0f, 0f, 0.02f);
        transportViewportObj.AddComponent<Mask>().showMaskGraphic = false;

        Scrollbar transportScrollbar = CreatePanelScrollbar("TransportScrollbar", transportPanelObj.transform);
        transportScroll.viewport = transportViewport;
        transportScroll.verticalScrollbar = transportScrollbar;

        GameObject transportContentGo = CreateUiObject("TransportContent", transportViewportObj.transform);
        RectTransform transportContent = transportContentGo.GetComponent<RectTransform>();
        transportContent.anchorMin = new Vector2(0f, 1f); transportContent.anchorMax = new Vector2(1f, 1f);
        transportContent.pivot = new Vector2(0.5f, 1f);
        transportContent.anchoredPosition = Vector2.zero; transportContent.sizeDelta = Vector2.zero;
        VerticalLayoutGroup transportLayout = transportContentGo.AddComponent<VerticalLayoutGroup>();
        transportLayout.spacing = 14;
        transportLayout.childControlWidth = true;
        transportLayout.childControlHeight = true;
        transportLayout.childForceExpandWidth = true;
        transportLayout.childForceExpandHeight = false;
        transportContentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        transportScroll.content = transportContent;

        RectTransform transportIntroCard = CreateSectionCard(transportContent, font, string.Empty, out RectTransform transportIntroBody, false);
        transportIntroCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;
        VerticalLayoutGroup transportIntroLayout = transportIntroBody.GetComponent<VerticalLayoutGroup>();
        transportIntroLayout.spacing = 6f;
        shiftsScreenUi.LogisticsSectionTitleText = CreateHeaderText("AssignmentsLogisticsSectionTitle", transportIntroBody, font, "Logistics", 16, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.LogisticsSectionSummaryText = CreateBodyText("AssignmentsLogisticsSectionSummary", transportIntroBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);

        for (int i = 0; i < ShiftPresetHours.Length; i++)
        {
            int shiftHour = ShiftPresetHours[i];
            ShiftCardUi card = new() { ShiftHour = shiftHour };
            RectTransform cardRoot = CreateSectionCard(transportContent, font, string.Empty, out RectTransform cardBody, false);
            cardRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 158f;

            GameObject borderObj = CreateUiObject($"ShiftBorder{i}", cardRoot);
            RectTransform borderRect = borderObj.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0f, 0f);
            borderRect.anchorMax = new Vector2(0f, 1f);
            borderRect.pivot = new Vector2(0f, 0.5f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(4f, 0f);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.color = Color.clear;
            borderImg.raycastTarget = false;
            borderObj.AddComponent<LayoutElement>().ignoreLayout = true;
            card.ActiveBorderImage = borderImg;

            VerticalLayoutGroup cardBodyLayout = cardBody.GetComponent<VerticalLayoutGroup>();
            cardBodyLayout.spacing = 8;

            card.HeaderText = CreateHeaderText($"ShiftHeader{i}", cardBody, font, $"{ShiftNames[i]}  {GetShiftRangeLabel(shiftHour)}", 16, TextAnchor.MiddleLeft, Color.white);
            card.SummaryText = CreateBodyText($"ShiftSummary{i}", cardBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            card.SummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            RectTransform assignedPanel = CreateStyledPanel($"ShiftAssignedPanel{i}", cardBody, FleetCardMutedColor);
            card.AssignedListRoot = assignedPanel;
            VerticalLayoutGroup assignedLayout = assignedPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            assignedLayout.padding = new RectOffset(10, 10, 10, 10);
            assignedLayout.spacing = 6;
            assignedLayout.childControlWidth = true;
            assignedLayout.childControlHeight = true;
            assignedLayout.childForceExpandWidth = true;
            assignedLayout.childForceExpandHeight = false;
            assignedPanel.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            card.EmptyText = CreateBodyText($"ShiftEmpty{i}", assignedPanel, font, "No drivers assigned", 12, TextAnchor.MiddleLeft, FleetMutedTextColor);

            for (int rowIndex = 0; rowIndex < MaxShiftDriverSlots; rowIndex++)
            {
                GameObject assignedRow = CreateUiObject($"ShiftAssignedRow{i}_{rowIndex}", assignedPanel);
                assignedRow.AddComponent<LayoutElement>().preferredHeight = 26f;
                HorizontalLayoutGroup assignedRowLayout = assignedRow.AddComponent<HorizontalLayoutGroup>();
                assignedRowLayout.spacing = 8;
                assignedRowLayout.childControlWidth = true;
                assignedRowLayout.childControlHeight = true;
                assignedRowLayout.childForceExpandWidth = false;
                assignedRowLayout.childForceExpandHeight = false;

                Text assignedText = CreateBodyText($"ShiftAssignedText{i}_{rowIndex}", assignedRow.transform, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
                assignedText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

                Button removeButton = CreateButton($"ShiftRemoveButton{i}_{rowIndex}", assignedRow.transform, font, out Text removeButtonText, "Remove", 11, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
                LayoutElement removeLayout = removeButton.gameObject.AddComponent<LayoutElement>();
                removeLayout.preferredWidth = 72f;
                removeLayout.preferredHeight = 22f;
                int shiftCardIndex = i;
                int assignedSlotIndex = rowIndex;
                removeButton.onClick.AddListener(() =>
                {
                    List<DriverAgent> assignedDrivers = new();
                    foreach (DriverAgent candidateDriver in driverAgents)
                    {
                        if (candidateDriver.ShiftStartHour == ShiftPresetHours[shiftCardIndex])
                        {
                            assignedDrivers.Add(candidateDriver);
                        }
                    }

                    if (assignedSlotIndex >= assignedDrivers.Count)
                    {
                        return;
                    }

                    DriverAgent assignedDriver = assignedDrivers[assignedSlotIndex];
                    LogUiInput($"Shifts Canvas: removed {assignedDriver.DriverName} from {ShiftNames[shiftCardIndex]}");
                    LogCommand($"RemoveShift({assignedDriver.DriverName})");
                    assignedDriver.ShiftStartHour = -1;
                    assignedDriver.IsOnActiveShift = false;
                    assignedDriver.WaitingForShiftAtParking = false;
                    assignedDriver.NeedsShiftEndReturn = false;
                    TruckAgent assignedTruck = GetAssignedTruckForDriver(assignedDriver);
                    if (assignedTruck != null)
                    {
                        assignedTruck.IsTruckAutoModeEnabled = false;
                    }

                    if (selectedShiftDriverId == assignedDriver.DriverId)
                    {
                        selectedShiftDriverId = 0;
                    }

                    PlayUiSound(uiSelectClip, 0.85f);
                    SessionDebugLogger.Log("SHIFT", $"{assignedDriver.DriverName} removed from shift — now Idle.");
                    LogDriverReaction(assignedDriver, "shift removed; now idle");
                    isShiftsScreenDirty = true;
                    isDriversScreenDirty = true;
                    isFleetScreenDirty = true;
                });

                card.AssignedRows.Add(assignedRow);
                card.AssignedDriverTexts.Add(assignedText);
                card.RemoveButtons.Add(removeButton);
            }

            card.AssignButton = CreateButton($"ShiftAssignButton{i}", cardBody, font, out Text assignText, string.Empty, 12, FleetPrimaryButtonColor, Color.white);
            card.AssignButtonText = assignText;
            card.AssignButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
            int currentShiftIndex = i;
            card.AssignButton.onClick.AddListener(() =>
            {
                DriverAgent selectedDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);
                if (selectedDriver == null || selectedDriver.ShiftStartHour == ShiftPresetHours[currentShiftIndex])
                {
                    return;
                }

                LogUiInput($"Shifts Canvas: assigned {selectedDriver.DriverName} to {ShiftNames[currentShiftIndex]}");
                LogCommand($"AssignShift({selectedDriver.DriverName}, {ShiftNames[currentShiftIndex]})");
                selectedDriver.ShiftStartHour = ShiftPresetHours[currentShiftIndex];
                selectedDriver.IsOnActiveShift = false;
                selectedDriver.WaitingForShiftAtParking = false;
                bool inWindow = IsHourInShiftWindow(GetCurrentHour(), ShiftPresetHours[currentShiftIndex]);
                if (inWindow && selectedDriver.RestPhase == DriverRestPhase.None)
                {
                    if (IsDriverBusDriver(selectedDriver))
                    {
                        StartBusDriverShiftCommute(selectedDriver);
                    }
                    else if (!IsDriverBusyWalkPhase(selectedDriver))
                    {
                        StartDriverShiftCommute(selectedDriver);
                    }
                }

                PlayUiSound(uiSelectClip, 0.85f);
                SessionDebugLogger.Log("SHIFT", $"{selectedDriver.DriverName} assigned to {ShiftNames[currentShiftIndex]} ({GetShiftRangeLabel(ShiftPresetHours[currentShiftIndex])}).");
                LogDriverReaction(selectedDriver, $"assigned to {ShiftNames[currentShiftIndex]} ({GetShiftRangeLabel(ShiftPresetHours[currentShiftIndex])})");
                PushFeedEvent(
                    $"{selectedDriver.DriverName} assigned to {ShiftNames[currentShiftIndex]} shift.",
                    $"{selectedDriver.DriverName} назначен на смену {L(ShiftNames[currentShiftIndex])}.",
                    FeedEventType.Info);
                isShiftsScreenDirty = true;
                isDriversScreenDirty = true;
            });

            shiftsScreenUi.ShiftCards.Add(card);
        }

        shiftsScreenUi.IntercitySlot = new IntercitySlotUi();
        RectTransform intercityCard = CreateSectionCard(transportContent, font, string.Empty, out RectTransform intercityBody, false);
        intercityCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 142f;
        VerticalLayoutGroup intercityLayout = intercityBody.GetComponent<VerticalLayoutGroup>();
        intercityLayout.spacing = 8;

        shiftsScreenUi.IntercitySlot.HeaderText = CreateHeaderText("IntercityHeader", intercityBody, font, "Intercity", 16, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.IntercitySlot.SummaryText = CreateBodyText("IntercitySummary", intercityBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        shiftsScreenUi.IntercitySlot.SummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        shiftsScreenUi.IntercitySlot.AssignedDriverText = CreateHeaderText("IntercityAssigned", intercityBody, font, string.Empty, 15, TextAnchor.MiddleLeft, FleetAccentColor);
        shiftsScreenUi.IntercitySlot.AssignedDriverText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        shiftsScreenUi.IntercitySlot.StatusText = CreateBodyText("IntercityStatus", intercityBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        shiftsScreenUi.IntercitySlot.StatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        RectTransform intercityButtonRow = CreateLayoutRow("IntercityButtonRow", intercityBody, 30f, 8f);
        shiftsScreenUi.IntercitySlot.AssignButton = CreateButton("IntercityAssignButton", intercityButtonRow, font, out Text intercityAssignText, string.Empty, 12, FleetPrimaryButtonColor, Color.white);
        shiftsScreenUi.IntercitySlot.AssignButtonText = intercityAssignText;
        LayoutElement intercityAssignLayout = shiftsScreenUi.IntercitySlot.AssignButton.gameObject.AddComponent<LayoutElement>();
        intercityAssignLayout.flexibleWidth = 1f;
        intercityAssignLayout.preferredHeight = 30f;
        shiftsScreenUi.IntercitySlot.AssignButton.onClick.AddListener(() =>
        {
            DriverAgent selectedDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);
            if (selectedDriver == null)
            {
                return;
            }

            AssignDriverToIntercitySlot(selectedDriver);
        });

        shiftsScreenUi.IntercitySlot.RemoveButton = CreateButton("IntercityRemoveButton", intercityButtonRow, font, out Text intercityRemoveText, "Remove", 12, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
        LayoutElement intercityRemoveLayout = shiftsScreenUi.IntercitySlot.RemoveButton.gameObject.AddComponent<LayoutElement>();
        intercityRemoveLayout.preferredWidth = 92f;
        intercityRemoveLayout.preferredHeight = 30f;
        shiftsScreenUi.IntercitySlot.RemoveButton.onClick.AddListener(RemoveIntercityDriverAssignment);

        RectTransform busDriverCard = CreateSectionCard(transportContent, font, string.Empty, out RectTransform busDriverBody, false);
        LayoutElement busDriverCardLayout = busDriverCard.gameObject.AddComponent<LayoutElement>();
        busDriverCardLayout.preferredHeight = 176f;
        busDriverCardLayout.flexibleHeight = 0f;
        VerticalLayoutGroup busDriverLayout = busDriverBody.GetComponent<VerticalLayoutGroup>();
        busDriverLayout.spacing = 4f;

        shiftsScreenUi.BusDriverGroupTitleText = CreateHeaderText("BusDriverGroupTitle", busDriverBody, font, "Bus Driver", 16, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.BusDriverGroupSummaryText = CreateBodyText("BusDriverGroupSummary", busDriverBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        shiftsScreenUi.BusDriverGroupSummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        for (int busShiftIndex = 0; busShiftIndex < ShiftPresetHours.Length; busShiftIndex++)
        {
            IntercitySlotUi busSlot = new();
            shiftsScreenUi.BusDriverSlots.Add(busSlot);

            RectTransform busShiftRow = CreateLayoutRow($"BusDriverShiftRow{busShiftIndex}", busDriverBody, 28f, 4f);
            busShiftRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            busSlot.AssignedDriverText = CreateHeaderText($"BusDriverAssigned{busShiftIndex}", busShiftRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            busSlot.AssignedDriverText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            busSlot.AssignButton = CreateButton($"BusDriverAssignButton{busShiftIndex}", busShiftRow, font, out Text busDriverAssignText, string.Empty, 11, FleetPrimaryButtonColor, Color.white);
            busSlot.AssignButtonText = busDriverAssignText;
            LayoutElement busDriverAssignLayout = busSlot.AssignButton.gameObject.AddComponent<LayoutElement>();
            busDriverAssignLayout.preferredWidth = 104f;
            busDriverAssignLayout.preferredHeight = 28f;
            int capturedBusShiftIndex = busShiftIndex;
            busSlot.AssignButton.onClick.AddListener(() =>
            {
                DriverAgent selectedDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);
                if (selectedDriver == null)
                {
                    return;
                }

                AssignDriverToBusSlot(selectedDriver, capturedBusShiftIndex);
            });

            busSlot.RemoveButton = CreateButton($"BusDriverRemoveButton{busShiftIndex}", busShiftRow, font, out _, "×", 12, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
            LayoutElement busDriverRemoveLayout = busSlot.RemoveButton.gameObject.AddComponent<LayoutElement>();
            busDriverRemoveLayout.preferredWidth = 28f;
            busDriverRemoveLayout.preferredHeight = 28f;
            busSlot.RemoveButton.onClick.AddListener(() => RemoveBusDriverAssignment(capturedBusShiftIndex));
        }

        // ── Logistics panel ──────────────────────────────────────────────────
        GameObject logisticsPanelObj = CreateUiObject("LogisticsPanel", rightPanel);
        shiftsLogisticsPanel = logisticsPanelObj.GetComponent<RectTransform>();
        LayoutElement logisticsPanelLayout = logisticsPanelObj.AddComponent<LayoutElement>();
        logisticsPanelLayout.preferredHeight = GetShiftsTabContentHeight();
        logisticsPanelLayout.minHeight = GetShiftsTabContentHeight();
        logisticsPanelLayout.flexibleHeight = 0f;
        ScrollRect logisticsScroll = logisticsPanelObj.AddComponent<ScrollRect>();
        shiftsScreenUi.ProductionScrollRect = logisticsScroll;
        logisticsScroll.horizontal = false; logisticsScroll.vertical = true;
        logisticsScroll.movementType = ScrollRect.MovementType.Clamped;
        logisticsScroll.scrollSensitivity = 30f; logisticsScroll.inertia = false;
        logisticsScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        GameObject logisticsViewportObj = CreateUiObject("Viewport", logisticsPanelObj.transform);
        RectTransform logisticsViewport = logisticsViewportObj.GetComponent<RectTransform>();
        StretchRect(logisticsViewport, 0f, 18f, 0f, 0f);
        Image logisticsViewportImage = logisticsViewportObj.AddComponent<Image>();
        logisticsViewportImage.color = new Color(0f, 0f, 0f, 0.02f);
        logisticsViewportObj.AddComponent<Mask>().showMaskGraphic = false;

        Scrollbar logisticsScrollbar = CreatePanelScrollbar("ProductionScrollbar", logisticsPanelObj.transform);
        logisticsScroll.viewport = logisticsViewport;
        logisticsScroll.verticalScrollbar = logisticsScrollbar;

        GameObject logisticsContentGo = CreateUiObject("LogisticsContent", logisticsViewportObj.transform);
        RectTransform logisticsContent = logisticsContentGo.GetComponent<RectTransform>();
        logisticsContent.anchorMin = new Vector2(0f, 1f); logisticsContent.anchorMax = new Vector2(1f, 1f);
        logisticsContent.pivot = new Vector2(0.5f, 1f);
        logisticsContent.anchoredPosition = Vector2.zero; logisticsContent.sizeDelta = Vector2.zero;
        VerticalLayoutGroup logLayout = logisticsContentGo.AddComponent<VerticalLayoutGroup>();
        logLayout.spacing = 14;
        logLayout.childControlWidth = true;
        logLayout.childControlHeight = true;
        logLayout.childForceExpandWidth = true;
        logLayout.childForceExpandHeight = false;
        logLayout.childAlignment = TextAnchor.UpperLeft;
        logisticsContentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        logisticsScroll.content = logisticsContent;

        RectTransform productionIntroCard = CreateSectionCard(logisticsContent, font, string.Empty, out RectTransform productionIntroBody, false);
        productionIntroCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;
        VerticalLayoutGroup productionIntroLayout = productionIntroBody.GetComponent<VerticalLayoutGroup>();
        productionIntroLayout.spacing = 6f;
        shiftsScreenUi.ProductionSectionTitleText = CreateHeaderText("AssignmentsProductionSectionTitle", productionIntroBody, font, "Production", 16, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.ProductionSectionSummaryText = CreateBodyText("AssignmentsProductionSectionSummary", productionIntroBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);

        // Slots 0-2: Forest, Sawmill, FurnitureFactory (one card each, one worker each)
        LocationType[] singleTypes = { LocationType.Forest, LocationType.Sawmill, LocationType.FurnitureFactory };
        string[] singleNames = { "Forest", "Sawmill", "Furniture Factory" };
        for (int si = 0; si < singleTypes.Length; si++)
        {
            LogisticsSlotUi slot = new() { BuildingType = singleTypes[si], SlotIndex = 0 };
            RectTransform slotCard = CreateSectionCard(logisticsContent, font, string.Empty, out RectTransform slotBody, false);
            slot.Root = slotCard;
            LayoutElement slotCardLE = slotCard.gameObject.AddComponent<LayoutElement>();
            slotCardLE.preferredHeight = 102f;
            slotCardLE.flexibleHeight  = 0f;
            VerticalLayoutGroup slotBodyLayout = slotBody.GetComponent<VerticalLayoutGroup>();
            slotBodyLayout.spacing = 4;

            slot.BuildingNameText = CreateHeaderText($"LogBldgName{si}", slotBody, font, singleNames[si], 16, TextAnchor.MiddleLeft, Color.white);
            slot.AssignedWorkerText = CreateHeaderText($"LogWorker{si}", slotBody, font, "No worker assigned", 14, TextAnchor.MiddleLeft, FleetAccentColor);
            slot.AssignedWorkerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

            RectTransform workRow = CreateLayoutRow($"LogWorkRow{si}", slotBody, 22f, 8f);
            workRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            CreateBodyText($"LogWorkLabel{si}", workRow, font, "Hours:", 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            slot.WorkHoursText = CreateHeaderText($"LogWorkHours{si}", workRow, font, GetProductionWorkRangeLabel(), 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            slot.WorkHoursText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            RectTransform actionRow = CreateLayoutRow($"LogActionRow{si}", slotBody, 26f, 8f);
            actionRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            slot.AssignButton = CreateButton($"LogAssignBtn{si}", actionRow, font, out slot.AssignButtonText, "Assign Worker", 12, FleetPrimaryButtonColor, Color.white);
            slot.AssignButton.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            slot.RemoveButton = CreateButton($"LogRemoveBtn{si}", actionRow, font, out _, "Remove", 12, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
            slot.RemoveButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;
            int capturedIndex = si;
            slot.AssignButton.onClick.AddListener(() =>
            {
                DriverAgent selectedDriver = driverAgents.Find(d => d.DriverId == selectedShiftDriverId);
                if (selectedDriver == null) return;
                AssignWorkerToBuilding(selectedDriver, logisticsSlots[capturedIndex]);
                PlayUiSound(uiSelectClip, 0.85f);
                if (capturedIndex == 0) CompleteForestAssignmentTutorial();
                else if (capturedIndex == 1) CompleteSawmillWorkerAssignedTutorial();
            });
            slot.RemoveButton.onClick.AddListener(() =>
            {
                RemoveWorkerFromBuilding(logisticsSlots[capturedIndex]);
                PlayUiSound(uiSelectClip, 0.85f);
            });
            logisticsSlots[si] = slot;
        }

        // Slots 3-5: Warehouse — one grouped card with 3 worker rows
        {
            RectTransform warehouseCard = CreateSectionCard(logisticsContent, font, string.Empty, out RectTransform warehouseBody, false);
            LayoutElement warehouseCardLE = warehouseCard.gameObject.AddComponent<LayoutElement>();
            warehouseCardLE.preferredHeight = 170f;
            warehouseCardLE.flexibleHeight  = 0f;
            VerticalLayoutGroup warehouseBodyLayout = warehouseBody.GetComponent<VerticalLayoutGroup>();
            warehouseBodyLayout.spacing = 4;

            Text warehouseTitle = CreateHeaderText("LogBldgNameWarehouse", warehouseBody, font, "Warehouse", 16, TextAnchor.MiddleLeft, Color.white);
            RectTransform workRow = CreateLayoutRow("LogWorkRowWarehouse", warehouseBody, 22f, 8f);
            workRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            CreateBodyText("LogWorkLabelWarehouse", workRow, font, "Hours:", 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            Text warehouseWorkHours = CreateHeaderText("LogWorkHoursWarehouse", workRow, font, GetProductionWorkRangeLabel(), 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            warehouseWorkHours.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            for (int wi = 0; wi < WarehouseMaxWorkers; wi++)
            {
                int slotArrayIdx = 3 + wi;
                LogisticsSlotUi wSlot = new() { BuildingType = LocationType.Warehouse, SlotIndex = wi, Root = warehouseCard };
                // Store WorkHoursText only on first warehouse slot (used by UpdateLogisticsTabUi)
                if (wi == 0)
                {
                    wSlot.WorkHoursText = warehouseWorkHours;
                    wSlot.BuildingNameText = warehouseTitle;
                }

                RectTransform wActionRow = CreateLayoutRow($"LogActionRowWarehouse{wi}", warehouseBody, 26f, 4f);
                wActionRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
                wSlot.AssignedWorkerText = CreateHeaderText($"LogWorkerWarehouse{wi}", wActionRow, font, $"Loader {wi + 1}: —", 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
                wSlot.AssignedWorkerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                wSlot.AssignButton = CreateButton($"LogAssignBtnWarehouse{wi}", wActionRow, font, out wSlot.AssignButtonText, "Assign", 11, FleetPrimaryButtonColor, Color.white);
                wSlot.AssignButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 104f;
                wSlot.RemoveButton = CreateButton($"LogRemoveBtnWarehouse{wi}", wActionRow, font, out _, "×", 12, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
                wSlot.RemoveButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 28f;

                int capturedIdx = slotArrayIdx;
                wSlot.AssignButton.onClick.AddListener(() =>
                {
                    DriverAgent sel = driverAgents.Find(d => d.DriverId == selectedShiftDriverId);
                    if (sel == null) return;
                    AssignWorkerToBuilding(sel, logisticsSlots[capturedIdx]);
                    PlayUiSound(uiSelectClip, 0.85f);
                });
                wSlot.RemoveButton.onClick.AddListener(() =>
                {
                    RemoveWorkerFromBuilding(logisticsSlots[capturedIdx]);
                    PlayUiSound(uiSelectClip, 0.85f);
                });
                logisticsSlots[slotArrayIdx] = wSlot;
            }
        }

        // Start with Transport tab visible
        shiftsLogisticsPanel.gameObject.SetActive(false);
        ApplyShiftsTabVisuals();
        EnforceShiftsWindowLayout();

        AddOverlayCloseButton(windowRect, font);
        shiftsScreenUi.CanvasRoot.SetActive(false);
        UpdateShiftsScreenUi();
    }

    private void UpdateShiftsScreenUi()
    {
        if (shiftsScreenUi == null) return;

        bool shouldShow = isShiftsPanelOpen;
        if (shiftsScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            shiftsScreenUi.CanvasRoot.SetActive(shouldShow);
            isShiftsScreenDirty = true;
            LogShiftsHudState(shouldShow ? "canvas shown" : "canvas hidden", force: true);
        }

        if (!shouldShow) return;
        if (!isShiftsScreenDirty) return;

        bool ru = IsRussianLanguage();
        if (shiftsScreenUi.TitleText != null)
        {
            shiftsScreenUi.TitleText.text = ru ? "Роли" : "Roles";
        }
        if (shiftsScreenUi.LogisticsSectionTitleText != null)
        {
            shiftsScreenUi.LogisticsSectionTitleText.text = ru ? "Логистика" : "Logistics";
        }
        if (shiftsScreenUi.LogisticsSectionSummaryText != null)
        {
            shiftsScreenUi.LogisticsSectionSummaryText.text = ru
                ? "Смены отвечают за локальные рейсы. Межгород резервирует отдельного рабочего под торговлю и выезды по магистрали."
                : "Shifts handle local delivery work. Intercity reserves a dedicated worker for trade and highway trips.";
        }
        if (shiftsScreenUi.ProductionSectionTitleText != null)
        {
            shiftsScreenUi.ProductionSectionTitleText.text = ru ? "Производство" : "Production";
        }
        if (shiftsScreenUi.ProductionSectionSummaryText != null)
        {
            shiftsScreenUi.ProductionSectionSummaryText.text = ru
                ? "Здесь рабочий закрепляется прямо за зданием и трудится по графику 08:00-18:00."
                : "Here a worker is assigned directly to a building and works on an 08:00-18:00 schedule.";
        }
        shiftsScreenUi.HeaderCountText.text = $"{driverAgents.Count} {(driverAgents.Count == 1 ? L("Worker") : L("Workers"))}";

        EnforceShiftsWindowLayout();
        ApplyShiftsTabVisuals();
        if (shiftsLogisticsPanel != null) shiftsLogisticsPanel.gameObject.SetActive(isLogisticsTabActive);
        if (shiftsTransportPanel  != null) shiftsTransportPanel.gameObject.SetActive(!isLogisticsTabActive);

        for (int i = 0; i < shiftsScreenUi.DriverRows.Count; i++)
        {
            ShiftDriverRowUi row = shiftsScreenUi.DriverRows[i];
            bool active = i < driverAgents.Count;
            row.Root.gameObject.SetActive(active);
            if (!active) continue;

            DriverAgent driver = driverAgents[i];
            row.DriverId = driver.DriverId;
            bool isSelected = selectedShiftDriverId == driver.DriverId;
            row.Background.color = isSelected ? ShiftsCardSelected : ShiftsCardColor;
            row.NameText.text = driver.DriverName;
            if (row.ProfessionText != null)
            {
                row.ProfessionText.text = L(GetWorkerOccupationLabel(driver));
                row.ProfessionText.color = driver.AssignedTruckNumber > 0 || driver.DutyMode != DriverDutyMode.Local
                    ? FleetAccentColor
                    : FleetSecondaryTextColor;
            }

            bool isAssigned = driver.ShiftStartHour >= 0;
            bool isIntercity = IsDriverIntercity(driver);
            bool isBusDriver = IsDriverBusDriver(driver);
            bool isProduction = driver.DutyMode == DriverDutyMode.Logistics;
            bool isTruckAssigned = driver.AssignedTruckNumber > 0;
            row.StatusText.text = isIntercity
                ? L("Intercity")
                : isBusDriver
                    ? (ru ? $"Автобус: {GetShiftRangeLabel(driver.ShiftStartHour)}" : $"Bus: {GetShiftRangeLabel(driver.ShiftStartHour)}")
                : isTruckAssigned
                    ? L("Logistics")
                    : isProduction
                        ? L("Production")
                        : isAssigned
                            ? $"{L("Assigned")}: {GetShiftRangeLabel(driver.ShiftStartHour)}"
                            : L("Idle");
            row.StatusText.color = isIntercity || isBusDriver || isProduction || isTruckAssigned
                ? FleetAccentColor
                : isAssigned ? new Color(0.62f, 0.92f, 0.62f, 1f) : FleetMutedTextColor;
        }

        DriverAgent selectedDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);
        if (shiftsScreenUi.SelectionTitleText != null)
        {
            shiftsScreenUi.SelectionTitleText.text = ru ? "Выбранный рабочий" : "Selected Worker";
        }
        if (selectedDriver == null)
        {
            if (shiftsScreenUi.SelectionNameText != null)
            {
                shiftsScreenUi.SelectionNameText.text = ru ? "Никто не выбран" : "No worker selected";
                shiftsScreenUi.SelectionNameText.color = Color.white;
            }
            if (shiftsScreenUi.SelectionProfessionText != null)
            {
                shiftsScreenUi.SelectionProfessionText.text = ru
                    ? "Статус назначения"
                    : "Assignment status";
            }
            if (shiftsScreenUi.SelectionStatusText != null)
            {
                shiftsScreenUi.SelectionStatusText.text = ru
                    ? "Доступные назначения появятся после выбора"
                    : "Available assignments appear after selection";
                shiftsScreenUi.SelectionStatusText.color = FleetSecondaryTextColor;
            }
            if (shiftsScreenUi.SelectionHintText != null)
            {
                shiftsScreenUi.SelectionHintText.text = string.Empty;
            }
        }
        else
        {
            if (shiftsScreenUi.SelectionNameText != null)
            {
                shiftsScreenUi.SelectionNameText.text = selectedDriver.DriverName;
                shiftsScreenUi.SelectionNameText.color = FleetAccentColor;
            }
            if (shiftsScreenUi.SelectionProfessionText != null)
            {
                shiftsScreenUi.SelectionProfessionText.text = L(GetWorkerOccupationLabel(selectedDriver));
            }

            string workerStatusSummary = IsDriverIntercity(selectedDriver)
                ? (ru ? "Закреплён за междугородними рейсами" : "Assigned to intercity duty")
                : IsDriverBusDriver(selectedDriver)
                    ? (ru ? $"Закреплён за автобусной сменой {GetShiftRangeLabel(selectedDriver.ShiftStartHour)}" : $"Assigned to bus duty {GetShiftRangeLabel(selectedDriver.ShiftStartHour)}")
                : selectedDriver.AssignedTruckNumber > 0
                    ? (ru ? $"Закреплён за грузовиком #{selectedDriver.AssignedTruckNumber}" : $"Assigned to Truck #{selectedDriver.AssignedTruckNumber}")
                    : selectedDriver.DutyMode == DriverDutyMode.Logistics && selectedDriver.AssignedBuildingType.HasValue
                        ? (ru ? $"Работает в {GetSelectedLocationDisplayName(selectedDriver.AssignedBuildingType.Value)}" : $"Working at {GetSelectedLocationDisplayName(selectedDriver.AssignedBuildingType.Value)}")
                        : selectedDriver.ShiftStartHour >= 0
                            ? (ru ? $"Логистическая смена: {GetShiftRangeLabel(selectedDriver.ShiftStartHour)}" : $"Logistics shift: {GetShiftRangeLabel(selectedDriver.ShiftStartHour)}")
                            : (ru ? Gend(selectedDriver, "Свободен для назначения", "Свободна для назначения") : "Available for assignment");
            if (shiftsScreenUi.SelectionStatusText != null)
            {
                shiftsScreenUi.SelectionStatusText.text = workerStatusSummary;
                shiftsScreenUi.SelectionStatusText.color =
                    (selectedDriver.DutyMode != DriverDutyMode.Local || selectedDriver.AssignedTruckNumber > 0 || selectedDriver.ShiftStartHour >= 0 || IsDriverBusDriver(selectedDriver))
                    ? FleetAccentColor
                    : Color.white;
            }

            if (shiftsScreenUi.SelectionHintText != null)
            {
                shiftsScreenUi.SelectionHintText.text = isLogisticsTabActive
                    ? (ru ? "Производство: прямое назначение в здание."
                        : "Production: direct building assignment.")
                    : (ru ? "Логистика: смены и межгород."
                        : "Logistics: shifts and intercity.");
            }
        }

        if (shiftsScreenUi.TransportScrollRect != null)
        {
            shiftsScreenUi.TransportScrollRect.verticalNormalizedPosition = 1f;
        }

        if (shiftsScreenUi.ProductionScrollRect != null)
        {
            shiftsScreenUi.ProductionScrollRect.verticalNormalizedPosition = 1f;
        }

        for (int i = 0; i < shiftsScreenUi.ShiftCards.Count; i++)
        {
            ShiftCardUi card = shiftsScreenUi.ShiftCards[i];
            List<DriverAgent> assignedDrivers = new();
            foreach (DriverAgent driver in driverAgents)
            {
                if (driver.DutyMode == DriverDutyMode.Local &&
                    driver.ShiftStartHour == card.ShiftHour &&
                    !IsDriverBusDriver(driver))
                {
                    assignedDrivers.Add(driver);
                }
            }

            bool isActiveShift = IsHourInShiftWindow(GetCurrentHour(), card.ShiftHour);
            if (card.ActiveBorderImage != null)
                card.ActiveBorderImage.color = isActiveShift ? new Color(1f, 0.85f, 0.25f, 1f) : Color.clear;

            if (card.SummaryText != null)
            {
                string shiftPrefix = ru ? "Локальные рейсы" : "Local deliveries";
                string summary = assignedDrivers.Count switch
                {
                    0 => ru ? "никто не назначен" : "no workers assigned",
                    1 => ru ? "1 рабочий назначен" : "1 worker assigned",
                    _ => ru ? $"{assignedDrivers.Count} рабочих назначено" : $"{assignedDrivers.Count} workers assigned"
                };
                string timerSuffix = string.Empty;
                if (isActiveShift)
                {
                    int endMin = ((card.ShiftHour + 8) % 24) * 60;
                    int minLeft = (endMin - GetCurrentTotalMinutes() + 24 * 60) % (24 * 60);
                    timerSuffix = ru ? $" • осталось {minLeft / 60:00}:{minLeft % 60:00}" : $" • {minLeft / 60:00}:{minLeft % 60:00} left";
                }
                card.SummaryText.text = $"{shiftPrefix} • {summary}{timerSuffix}";
            }

            card.EmptyText.gameObject.SetActive(assignedDrivers.Count == 0);
            for (int rowIndex = 0; rowIndex < card.AssignedRows.Count; rowIndex++)
            {
                bool rowActive = rowIndex < assignedDrivers.Count;
                card.AssignedRows[rowIndex].SetActive(rowActive);
                if (!rowActive) continue;

                card.AssignedDriverTexts[rowIndex].text = assignedDrivers[rowIndex].DriverName;
            }

            bool alreadyAssigned = selectedDriver != null && selectedDriver.DutyMode == DriverDutyMode.Local && selectedDriver.ShiftStartHour == card.ShiftHour;
            bool intercitySelected = IsDriverIntercity(selectedDriver);
            bool busDriverSelected = IsDriverBusDriver(selectedDriver);
            bool productionSelected = selectedDriver?.DutyMode == DriverDutyMode.Logistics;
            card.AssignButton.interactable = selectedDriver != null && !alreadyAssigned && !intercitySelected && !productionSelected && !busDriverSelected;
            card.AssignButtonText.text = selectedDriver == null
                ? L("Select a worker to assign")
                : (intercitySelected || productionSelected || busDriverSelected)
                    ? L("Worker not available")
                    : alreadyAssigned
                        ? (ru ? $"{selectedDriver.DriverName} уже назначен" : $"{selectedDriver.DriverName} already assigned")
                        : (ru ? $"Назначить {selectedDriver.DriverName} -> {ShiftNames[i]}" : $"Assign {selectedDriver.DriverName} -> {ShiftNames[i]}");
        }

        UpdateIntercitySlotUi(selectedDriver);
        UpdateBusDriverSlotsUi(selectedDriver);

        if (isLogisticsTabActive)
        {
            UpdateLogisticsTabUi(selectedDriver);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(shiftsScreenUi.DriverListContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(shiftsScreenUi.WindowRoot);
        EnforceShiftsWindowLayout();
        ApplyShiftsTabVisuals();
        LogShiftsHudState("rebuilt Shifts canvas");
        LocalizeCanvas(shiftsScreenUi.CanvasRoot);
        isShiftsScreenDirty = false;
    }

    private void EnforceShiftsWindowLayout()
    {
        if (shiftsScreenUi?.WindowRoot == null)
        {
            return;
        }

        SetCenteredWindow(shiftsScreenUi.WindowRoot, ShiftsWindowWidth, ShiftsWindowHeight, -16f);
        ApplyFixedLayoutSize(shiftsScreenUi.LeftPanel, ShiftsLeftPanelWidth, ShiftsInnerPanelHeight);
        ApplyFixedLayoutSize(shiftsScreenUi.RightPanel, ShiftsRightPanelWidth, ShiftsInnerPanelHeight);
        float tabContentHeight = GetShiftsTabContentHeight();
        ApplyFixedLayoutHeight(shiftsTransportPanel, tabContentHeight);
        ApplyFixedLayoutHeight(shiftsLogisticsPanel, tabContentHeight);
    }

    private static float GetShiftsTabContentHeight()
    {
        const float rightPanelVerticalPadding = 30f;
        const float rightPanelInterBlockSpacing = 24f;
        float available = ShiftsInnerPanelHeight - rightPanelVerticalPadding - rightPanelInterBlockSpacing - ShiftsSelectionCardHeight - ShiftsTabRowHeight;
        return Mathf.Max(280f, available);
    }

    private static void ApplyFixedLayoutSize(RectTransform rect, float width, float height)
    {
        if (rect == null)
        {
            return;
        }

        LayoutElement layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
        {
            return;
        }

        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.flexibleWidth = 0f;
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;
    }

    private static void ApplyFixedLayoutHeight(RectTransform rect, float height)
    {
        if (rect == null)
        {
            return;
        }

        LayoutElement layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
        {
            return;
        }

        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;
    }

    private void LogShiftsHudState(string reason, bool force = false)
    {
        string state = BuildShiftsHudDebugState(reason);
        if (!force && state == lastShiftsHudDebugState)
        {
            return;
        }

        lastShiftsHudDebugState = state;
        SessionDebugLogger.Log("SHIFTS_HUD", state);
    }

    private string BuildShiftsHudDebugState(string reason)
    {
        string canvasActive = shiftsScreenUi?.CanvasRoot != null ? shiftsScreenUi.CanvasRoot.activeSelf.ToString() : "null";
        string windowSize = FormatRectSize(shiftsScreenUi?.WindowRoot);
        string leftSize = FormatRectSize(shiftsScreenUi?.LeftPanel);
        string rightSize = FormatRectSize(shiftsScreenUi?.RightPanel);
        string transportPanel = FormatPanelState(shiftsTransportPanel);
        string productionPanel = FormatPanelState(shiftsLogisticsPanel);
        string logisticsTabColor = FormatButtonColor(shiftsTransportTabBtn);
        string productionsTabColor = FormatButtonColor(shiftsLogisticsTabBtn);
        string logisticsTextColor = FormatTextColor(shiftsTransportTabText);
        string productionsTextColor = FormatTextColor(shiftsLogisticsTabText);
        string activeTab = isLogisticsTabActive ? "Productions" : "Logistics";
        string labels = $"labels(L='{shiftsTransportTabText?.text ?? "null"}', P='{shiftsLogisticsTabText?.text ?? "null"}')";

        return $"{reason}: open={isShiftsPanelOpen}, dirty={isShiftsScreenDirty}, activeTab={activeTab}, " +
               $"canvasActive={canvasActive}, window={windowSize}, left={leftSize}, right={rightSize}, " +
               $"transport={transportPanel}, productions={productionPanel}, " +
               $"tabColors(Logistics={logisticsTabColor}, Productions={productionsTabColor}), " +
               $"textColors(Logistics=#{logisticsTextColor}, Productions=#{productionsTextColor}), {labels}";
    }

    private static string FormatPanelState(RectTransform rect)
    {
        if (rect == null)
        {
            return "null";
        }

        return $"{rect.gameObject.activeSelf}/{FormatRectSize(rect)}";
    }

    private static string FormatRectSize(RectTransform rect)
    {
        if (rect == null)
        {
            return "null";
        }

        return $"{rect.rect.width:0.#}x{rect.rect.height:0.#}/delta({rect.sizeDelta.x:0.#},{rect.sizeDelta.y:0.#})";
    }

    private static string FormatButtonColor(Button button)
    {
        if (button == null)
        {
            return "null";
        }

        Image image = button.targetGraphic as Image ?? button.image;
        string imageColor = image != null ? ColorUtility.ToHtmlStringRGBA(image.color) : "no-image";
        return $"image=#{imageColor}, normal=#{ColorUtility.ToHtmlStringRGBA(button.colors.normalColor)}";
    }

    private static string FormatTextColor(Text text)
    {
        return text != null ? ColorUtility.ToHtmlStringRGBA(text.color) : "null";
    }

    private static Scrollbar CreatePanelScrollbar(string name, Transform parent)
    {
        RectTransform scrollbarRoot = CreateStyledPanel(name, parent, new Color(0.09f, 0.12f, 0.17f, 0.96f));
        scrollbarRoot.anchorMin = new Vector2(1f, 0f);
        scrollbarRoot.anchorMax = new Vector2(1f, 1f);
        scrollbarRoot.pivot = new Vector2(1f, 0.5f);
        scrollbarRoot.sizeDelta = new Vector2(12f, 0f);
        scrollbarRoot.anchoredPosition = Vector2.zero;

        RectTransform slidingArea = CreateUiObject($"{name}_SlidingArea", scrollbarRoot).GetComponent<RectTransform>();
        StretchRect(slidingArea, 2f, 2f, 2f, 2f);

        RectTransform handleRect = CreateStyledPanel($"{name}_Handle", slidingArea, FleetPrimaryButtonColor);
        StretchRect(handleRect, 0f, 0f, 0f, 0f);

        Scrollbar scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleRect.GetComponent<Image>();
        scrollbar.value = 1f;
        scrollbar.size = 0.25f;
        return scrollbar;
    }

    private void ApplyShiftsTabVisuals()
    {
        ApplyShiftsTabVisual(shiftsTransportTabBtn, shiftsTransportTabText, !isLogisticsTabActive);
        ApplyShiftsTabVisual(shiftsLogisticsTabBtn, shiftsLogisticsTabText, isLogisticsTabActive);
    }

    private static void ApplyShiftsTabVisual(Button button, Text label, bool isActive)
    {
        if (button == null)
        {
            return;
        }

        Color activeColor = FleetPrimaryButtonColor;
        Color inactiveColor = new(0.08f, 0.10f, 0.14f, 1f);
        Color targetColor = isActive ? activeColor : inactiveColor;
        Image image = button.targetGraphic as Image ?? button.image;
        if (image != null)
        {
            image.color = targetColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = targetColor;
        colors.highlightedColor = isActive ? activeColor : Color.Lerp(inactiveColor, FleetPrimaryButtonColor, 0.24f);
        colors.pressedColor = Color.Lerp(targetColor, Color.black, 0.14f);
        colors.selectedColor = targetColor;
        colors.disabledColor = targetColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;

        if (label != null)
        {
            label.color = isActive ? FleetAccentColor : Color.white;
            label.fontStyle = isActive ? FontStyle.BoldAndItalic : FontStyle.Bold;
        }
    }

    private void UpdateLogisticsTabUi(DriverAgent selectedDriver)
    {
        bool forestTutorialActive = IsTutorialEnabledForCurrentMode() && !hasShownForestWorkerStartedTutorial;
        bool ru = IsRussianLanguage();

        // For Warehouse: only show the card once (slot index 3 controls Root visibility)
        bool warehouseCardShown = false;

        for (int i = 0; i < logisticsSlots.Length; i++)
        {
            LogisticsSlotUi slot = logisticsSlots[i];
            if (slot == null) continue;

            bool isWarehouse = slot.BuildingType == LocationType.Warehouse;

            // Root visibility: only toggle for single-building slots and the first warehouse slot
            if (slot.Root != null && (!isWarehouse || !warehouseCardShown))
            {
                bool isBuilt = locations.ContainsKey(slot.BuildingType);
                bool visible = isBuilt && (!forestTutorialActive || slot.BuildingType == LocationType.Forest);
                slot.Root.gameObject.SetActive(visible);
                if (isWarehouse) warehouseCardShown = true;
                if (!visible) continue;
            }
            else if (slot.Root != null && !slot.Root.gameObject.activeSelf)
            {
                continue;
            }

            DriverAgent assigned = GetNthLogisticsWorker(slot.BuildingType, slot.SlotIndex);

            if (slot.BuildingNameText != null)
            {
                slot.BuildingNameText.text = L(GetSelectedLocationDisplayName(slot.BuildingType));
            }

            if (slot.WorkHoursText != null)
            {
                slot.WorkHoursText.text = GetProductionWorkRangeLabel();
                slot.WorkHoursText.color = IsProductionWorkHour(GetCurrentHour()) ? FleetAccentColor : FleetSecondaryTextColor;
            }

            if (isWarehouse)
            {
                // Compact row: "Loader N: Name — hours" or "Loader N: —"
                string loaderLabel = ru ? $"Кладовщик {slot.SlotIndex + 1}: " : $"Loader {slot.SlotIndex + 1}: ";
                slot.AssignedWorkerText.text = assigned != null
                    ? $"{loaderLabel}{assigned.DriverName}"
                    : $"{loaderLabel}—";
                slot.AssignedWorkerText.color = assigned != null ? FleetAccentColor : FleetSecondaryTextColor;
            }
            else
            {
                string professionLabel = slot.BuildingType switch
                {
                    LocationType.Forest           => L("Lumberjack"),
                    LocationType.Sawmill          => L("Sawmill Worker"),
                    LocationType.FurnitureFactory => L("Carpenter"),
                    _                             => L("Worker"),
                };
                string workerLabel = assigned != null
                    ? (ru ? $"Назначен: {assigned.DriverName}" : $"Assigned: {assigned.DriverName}")
                    : ru ? $"{professionLabel} не назначен" : $"{professionLabel} not assigned";
                slot.AssignedWorkerText.text = workerLabel;
                slot.AssignedWorkerText.color = assigned != null ? FleetAccentColor : FleetSecondaryTextColor;
            }

            bool selectedIsIdle = selectedDriver != null && !selectedDriver.IsArrivingByBus;
            bool selectedAlreadyHere = selectedDriver != null &&
                selectedDriver.DutyMode == DriverDutyMode.Logistics &&
                selectedDriver.AssignedBuildingType == slot.BuildingType;
            bool canAssign = selectedIsIdle && assigned == null && !selectedAlreadyHere;
            slot.AssignButton.interactable = canAssign;
            slot.AssignButtonText.text = selectedDriver == null
                ? (isWarehouse ? (ru ? "Выбери рабочего" : "Select worker") : L("Select a worker"))
                : assigned != null
                    ? (isWarehouse ? (ru ? "Занято" : "Occupied") : L("Slot occupied"))
                    : selectedAlreadyHere
                        ? (isWarehouse ? L("Assigned") : (ru ? "Уже назначен сюда" : "Already assigned here"))
                        : !selectedIsIdle
                            ? (isWarehouse ? (ru ? "Не свободен" : "Not idle") : (ru ? $"{selectedDriver.DriverName} не свободен" : $"{selectedDriver.DriverName} is not idle"))
                            : isWarehouse ? (ru ? "Назначить" : "Assign") : (ru ? $"Назначить: {selectedDriver.DriverName}" : $"Assign: {selectedDriver.DriverName}");

            slot.RemoveButton.interactable = assigned != null;
        }
    }

    private DriverAgent GetNthLogisticsWorker(LocationType buildingType, int slotIndex)
    {
        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent d = driverAgents[i];
            if (d.DutyMode == DriverDutyMode.Logistics && d.AssignedBuildingType == buildingType)
            {
                if (count == slotIndex) return d;
                count++;
            }
        }
        return null;
    }

    private void AssignWorkerToBuilding(DriverAgent driver, LogisticsSlotUi slot)
    {
        if (driver == null || slot == null) return;
        if (driver.IsArrivingByBus) return;

        if (IsDriverBusDriver(driver))
        {
            int busSlotIndex = GetBusDriverShiftSlotIndex(driver);
            if (busSlotIndex >= 0)
            {
                busDriverShiftIds[busSlotIndex] = 0;
            }
        }

        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (assignedTruck != null)
        {
            if (!UnassignDriverFromTruck(assignedTruck, driver))
            {
                SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} production assignment to {slot.BuildingType} blocked: could not unassign from {assignedTruck.DisplayName}.");
                LogDriverReaction(driver, $"cannot start production at {slot.BuildingType}: still assigned to {assignedTruck.DisplayName}");
                return;
            }

            SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} auto-unassigned from {assignedTruck.DisplayName} before production assignment to {slot.BuildingType}.");
        }

        // Remove from any existing building assignment first
        if (driver.DutyMode == DriverDutyMode.Logistics && driver.AssignedBuildingType.HasValue)
        {
            SetDriverDutyMode(driver, DriverDutyMode.Local);
        }

        SetDriverDutyMode(driver, DriverDutyMode.Logistics);
        driver.AssignedBuildingType = slot.BuildingType;
        driver.ShiftStartHour = -1;
        LogUiInput($"Shifts Canvas: assigned {driver.DriverName} to {slot.BuildingType} ({GetProductionWorkRangeLabel()})");
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} assigned to {slot.BuildingType} production work ({GetProductionWorkRangeLabel()}).");
        PushFeedEvent(
            $"{driver.DriverName} assigned to {GetSelectedLocationDisplayName(slot.BuildingType)}.",
            $"{driver.DriverName} назначен в {GetSelectedLocationDisplayName(slot.BuildingType)}.",
            FeedEventType.Info);
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void RemoveWorkerFromBuilding(LogisticsSlotUi slot)
    {
        if (slot == null) return;
        DriverAgent assigned = GetNthLogisticsWorker(slot.BuildingType, slot.SlotIndex);
        if (assigned == null) return;
        LogUiInput($"Shifts Canvas: removed {assigned.DriverName} from {slot.BuildingType}");
        SetDriverDutyMode(assigned, DriverDutyMode.Local);
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void UpdateIntercitySlotUi(DriverAgent selectedDriver)
    {
        if (shiftsScreenUi?.IntercitySlot == null)
        {
            return;
        }

        DriverAgent intercityDriver = GetIntercityAssignedDriver();
        bool selectedIsIntercity = IsDriverIntercity(selectedDriver);
        bool selectedIsIdleForIntercity = selectedDriver != null && !selectedDriver.IsArrivingByBus && !selectedIsIntercity;
        bool ru = IsRussianLanguage();
        if (shiftsScreenUi.IntercitySlot.HeaderText != null)
        {
            shiftsScreenUi.IntercitySlot.HeaderText.text = ru ? "Межгород" : "Intercity";
        }
        if (shiftsScreenUi.IntercitySlot.SummaryText != null)
        {
            shiftsScreenUi.IntercitySlot.SummaryText.text = ru
                ? "Отдельный рабочий для торговли и рейсов за пределы текущего региона."
                : "Dedicated worker for trade and trips beyond the current region.";
        }
        shiftsScreenUi.IntercitySlot.AssignedDriverText.text = intercityDriver != null ? intercityDriver.DriverName : L("No worker assigned");
        shiftsScreenUi.IntercitySlot.StatusText.text = intercityDriver != null
            ? (ru ? "Закреплён под междугородние рейсы" : "Reserved for future trade runs")
            : (ru ? "Назначьте отдельного рабочего на междугородние рейсы" : "Assign one dedicated worker to intercity duty");
        shiftsScreenUi.IntercitySlot.AssignButton.interactable = selectedIsIdleForIntercity && !selectedIsIntercity;
        shiftsScreenUi.IntercitySlot.AssignButtonText.text = selectedDriver == null
            ? L("Select a worker")
            : (selectedIsIntercity || !selectedIsIdleForIntercity)
                ? L("Worker not available")
                : (ru ? $"Назначить {selectedDriver.DriverName}" : $"Assign {selectedDriver.DriverName}");
        shiftsScreenUi.IntercitySlot.RemoveButton.interactable = intercityDriver != null;
    }

    private void UpdateBusDriverSlotsUi(DriverAgent selectedDriver)
    {
        if (shiftsScreenUi == null || shiftsScreenUi.BusDriverSlots.Count == 0)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        if (shiftsScreenUi.BusDriverGroupTitleText != null)
        {
            shiftsScreenUi.BusDriverGroupTitleText.text = ru ? "Водитель автобуса" : "Bus Driver";
        }

        if (shiftsScreenUi.BusDriverGroupSummaryText != null)
        {
            shiftsScreenUi.BusDriverGroupSummaryText.text = ru
                ? "Одна должность, три смены. Водители сменяют друг друга."
                : "One role, three shift slots. Drivers hand the route over to each other.";
        }

        bool selectedIsAvailable = selectedDriver != null &&
                                   !selectedDriver.IsArrivingByBus &&
                                   !IsDriverIntercity(selectedDriver) &&
                                   selectedDriver.DutyMode == DriverDutyMode.Local &&
                                   selectedDriver.AssignedTruckNumber <= 0 &&
                                   !IsDriverOnActiveTradeRun(selectedDriver) &&
                                   !IsBusDriverOnActiveRoute(selectedDriver);
        int selectedBusSlotIndex = GetBusDriverShiftSlotIndex(selectedDriver);
        bool selectedAlreadyBusDriver = selectedBusSlotIndex >= 0;

        for (int i = 0; i < shiftsScreenUi.BusDriverSlots.Count && i < ShiftPresetHours.Length; i++)
        {
            IntercitySlotUi slot = shiftsScreenUi.BusDriverSlots[i];
            DriverAgent busDriver = GetBusAssignedDriver(i);
            bool selectedAlreadyInThisSlot = selectedBusSlotIndex == i;
            string shiftLabel = $"{L(ShiftNames[i])} {GetShiftRangeLabel(ShiftPresetHours[i])}";
            string assignedLabel = busDriver != null ? busDriver.DriverName : "—";

            slot.AssignedDriverText.text = $"{shiftLabel}: {assignedLabel}";
            slot.AssignButton.interactable = selectedIsAvailable && !selectedAlreadyBusDriver && !selectedAlreadyInThisSlot && busDriver == null;
            slot.AssignButtonText.text = selectedDriver == null
                ? (ru ? "Выбери рабочего" : "Select worker")
                : busDriver != null
                    ? (ru ? "Занято" : "Occupied")
                    : selectedAlreadyBusDriver
                        ? (ru ? "Уже назначен" : "Already assigned")
                        : !selectedIsAvailable
                            ? (ru ? "Недоступен" : "Unavailable")
                            : (ru ? "Назначить" : "Assign");
            slot.RemoveButton.interactable = busDriver != null && !IsBusDriverOnActiveRoute(busDriver);
        }
    }

    private DriverAgent GetIntercityAssignedDriver()
    {
        return driverAgents.Find(driver => driver.DriverId == intercityDriverId && IsDriverIntercity(driver));
    }

    private DriverAgent GetBusAssignedDriver(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= busDriverShiftIds.Length)
        {
            return null;
        }

        int driverId = busDriverShiftIds[slotIndex];
        return driverId <= 0 ? null : driverAgents.Find(driver => driver.DriverId == driverId);
    }

    private void AssignDriverToIntercitySlot(DriverAgent driver)
    {
        if (driver == null) return;
        if (driver.IsArrivingByBus || driver.DutyMode == DriverDutyMode.Intercity) return;

        if (HasActiveTradeRun())
        {
            tradeDispatchStatusText = "Wait for the active trade run to finish";
            isEconomyScreenDirty = true;
            return;
        }

        DriverAgent currentIntercity = GetIntercityAssignedDriver();
        if (currentIntercity != null && currentIntercity != driver)
        {
            SetDriverDutyMode(currentIntercity, DriverDutyMode.Local);
        }

        SetDriverDutyMode(driver, DriverDutyMode.Intercity);
        intercityDriverId = driver.DriverId;
        PlayUiSound(uiSelectClip, 0.85f);
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} assigned to Intercity slot.");
        LogDriverReaction(driver, "assigned to Intercity duty");
        PushFeedEvent(
            $"{driver.DriverName} reserved for intercity duty.",
            $"{driver.DriverName} закреплён за междугородними рейсами.",
            FeedEventType.Info);
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void AssignDriverToBusSlot(DriverAgent driver, int slotIndex)
    {
        if (driver == null || driver.IsArrivingByBus)
        {
            return;
        }

        if (slotIndex < 0 || slotIndex >= ShiftPresetHours.Length)
        {
            return;
        }

        if (driver.AssignedTruckNumber > 0 || driver.DutyMode == DriverDutyMode.Logistics || IsDriverIntercity(driver))
        {
            return;
        }

        DriverAgent currentBusDriver = GetBusAssignedDriver(slotIndex);
        if (currentBusDriver != null && currentBusDriver != driver)
        {
            currentBusDriver.ShiftStartHour = -1;
            currentBusDriver.IsOnActiveShift = false;
            currentBusDriver.WaitingForShiftAtParking = false;
            currentBusDriver.NeedsShiftEndReturn = false;
            busDriverShiftIds[slotIndex] = 0;
        }

        int previousSlotIndex = GetBusDriverShiftSlotIndex(driver);
        if (previousSlotIndex >= 0 && previousSlotIndex != slotIndex)
        {
            busDriverShiftIds[previousSlotIndex] = 0;
        }

        busDriverShiftIds[slotIndex] = driver.DriverId;
        driver.ShiftStartHour = ShiftPresetHours[slotIndex];
        driver.IsOnActiveShift = false;
        driver.WaitingForShiftAtParking = false;
        driver.NeedsShiftEndReturn = false;
        PlayUiSound(uiSelectClip, 0.85f);
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} assigned to Bus Driver slot {ShiftNames[slotIndex]} ({GetShiftRangeLabel(ShiftPresetHours[slotIndex])}).");
        SessionDebugLogger.Log("BUS_SHIFT", $"{driver.DriverName} reserved as Bus Driver for {ShiftNames[slotIndex]} ({GetShiftRangeLabel(ShiftPresetHours[slotIndex])}).");
        PushFeedEvent(
            $"{driver.DriverName} assigned as bus driver for {ShiftNames[slotIndex]}.",
            $"{driver.DriverName} назначен водителем автобуса на смену {L(ShiftNames[slotIndex])}.",
            FeedEventType.Info);
        bool inWindow = IsHourInShiftWindow(GetCurrentHour(), ShiftPresetHours[slotIndex]);
        if (inWindow && driver.RestPhase == DriverRestPhase.None)
        {
            StartBusDriverShiftCommute(driver);
        }
        LogDriverReaction(driver, $"assigned to bus duty {ShiftNames[slotIndex]} ({GetShiftRangeLabel(ShiftPresetHours[slotIndex])})");
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void RemoveIntercityDriverAssignment()
    {
        DriverAgent intercityDriver = GetIntercityAssignedDriver();
        if (intercityDriver == null)
        {
            return;
        }

        if (IsDriverOnActiveTradeRun(intercityDriver))
        {
            tradeDispatchStatusText = "Intercity driver is currently on a trade run";
            isEconomyScreenDirty = true;
            return;
        }

        SetDriverDutyMode(intercityDriver, DriverDutyMode.Local);
        intercityDriverId = 0;
        PlayUiSound(uiSelectClip, 0.85f);
        SessionDebugLogger.Log("SHIFT", $"{intercityDriver.DriverName} removed from Intercity slot.");
        LogDriverReaction(intercityDriver, "returned from Intercity duty to local pool");
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void RemoveBusDriverAssignment(int slotIndex)
    {
        DriverAgent busDriver = GetBusAssignedDriver(slotIndex);
        if (busDriver == null)
        {
            return;
        }

        busDriverShiftIds[slotIndex] = 0;
        busDriver.ShiftStartHour = -1;
        busDriver.IsOnActiveShift = false;
        busDriver.WaitingForShiftAtParking = false;
        busDriver.NeedsShiftEndReturn = false;
        SessionDebugLogger.Log("SHIFT", $"{busDriver.DriverName} removed from Bus Driver slot {ShiftNames[slotIndex]}.");
        LogDriverReaction(busDriver, $"removed from bus duty {ShiftNames[slotIndex]}");
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private static Color GetNeedBarColor(float satisfactionPct)
    {
        if (satisfactionPct > 0.5f) return new Color(0.58f, 0.88f, 0.54f, 1f);
        if (satisfactionPct > 0f)   return new Color(0.96f, 0.72f, 0.30f, 1f);
        return new Color(0.95f, 0.32f, 0.25f, 1f);
    }

    // ── Need icon sprites (generated once, cached) ────────────────────────────
    private static Sprite s_needsMealIcon;
    private static Sprite s_needsSleepIcon;
    private static Sprite s_needsLeisureIcon;

    private static Sprite GetNeedsMealIcon()    => s_needsMealIcon    ??= BuildNeedIcon(PaintMealIcon);
    private static Sprite GetNeedsSleepIcon()   => s_needsSleepIcon   ??= BuildNeedIcon(PaintSleepIcon);
    private static Sprite GetNeedsLeisureIcon() => s_needsLeisureIcon ??= BuildNeedIcon(PaintLeisureIcon);

    private static Sprite BuildNeedIcon(System.Action<Color[], int> paintFn)
    {
        const int sz = 16;
        Texture2D tex = new(sz, sz, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[sz * sz];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
        paintFn(px, sz);
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, sz, sz), new Vector2(0.5f, 0.5f), sz);
    }

    private static void NeedIconSet(Color[] px, int sz, int x, int y, Color c)
    {
        if (x >= 0 && x < sz && y >= 0 && y < sz) px[y * sz + x] = c;
    }

    private static void PaintMealIcon(Color[] px, int sz)
    {
        // Pixel-art fork (warm gold): 3 tines at top, handle at bottom
        Color c = new(0.90f, 0.75f, 0.45f, 1f);
        void S(int x, int y) => NeedIconSet(px, sz, x, y, c);

        // Handle (2 px wide, lower half)
        for (int y = 1; y <= 7; y++) { S(7, y); S(8, y); }

        // Tine junction bar
        for (int x = 3; x <= 12; x++) S(x, 8);

        // Left tine (2 px wide)
        for (int y = 9; y <= 13; y++) { S(3, y); S(4, y); }

        // Centre tine (2 px wide)
        for (int y = 9; y <= 13; y++) { S(7, y); S(8, y); }

        // Right tine (2 px wide)
        for (int y = 9; y <= 13; y++) { S(11, y); S(12, y); }
    }

    private static void PaintSleepIcon(Color[] px, int sz)
    {
        // Light-blue crescent — moon / sleep
        Color c = new(0.45f, 0.70f, 0.95f, 1f);
        float cx = (sz - 1) / 2f, cy = (sz - 1) / 2f;
        float r1 = sz / 2f - 1.5f;
        float r2 = r1 * 0.80f;
        float ox = r1 * 0.55f;
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
            {
                float dx = x - cx, dy = y - cy;
                float dx2 = x - (cx + ox), dy2 = y - cy;
                if (dx * dx + dy * dy <= r1 * r1 &&
                    dx2 * dx2 + dy2 * dy2 > r2 * r2)
                    px[y * sz + x] = c;
            }
    }

    private static void PaintLeisureIcon(Color[] px, int sz)
    {
        // Smiley face — leisure / fun
        Color face   = new(0.96f, 0.88f, 0.12f, 1f);  // yellow
        Color detail = new(0.10f, 0.07f, 0.04f, 1f);  // near-black
        float cx = (sz - 1) / 2f, cy = (sz - 1) / 2f, r = sz / 2f - 1.5f;
        void SF(int x, int y) => NeedIconSet(px, sz, x, y, face);
        void SD(int x, int y) => NeedIconSet(px, sz, x, y, detail);

        // Face fill
        for (int y = 0; y < sz; y++)
            for (int x = 0; x < sz; x++)
                if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                    SF(x, y);

        // Eyes — 2×2 dots, upper third
        int eyeY = sz * 11 / 16;
        SD(5, eyeY); SD(6, eyeY); SD(5, eyeY - 1); SD(6, eyeY - 1);
        SD(9, eyeY); SD(10, eyeY); SD(9, eyeY - 1); SD(10, eyeY - 1);

        // Smile — arc across lower third
        for (float a = 200f; a <= 340f; a += 10f)
        {
            float rad = a * Mathf.Deg2Rad;
            int sx = Mathf.RoundToInt(cx + Mathf.Cos(rad) * r * 0.52f);
            int sy = Mathf.RoundToInt(cy - 1 + Mathf.Sin(rad) * r * 0.40f);
            SD(sx, sy);
            SD(sx, sy - 1);
        }
    }

    private static RectTransform CreateNeedsMiniBar(RectTransform parent, Sprite iconSprite, string barName, float barWidth)
    {
        const float barH    = 6f;
        const float iconSz  = 10f;

        // Icon
        GameObject iconObj = CreateUiObject($"NeedIcon{barName}", parent);
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.anchorMin = iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(iconSz, iconSz);
        Image iconImg = iconObj.AddComponent<Image>();
        iconImg.sprite = iconSprite;
        iconImg.color = Color.white;
        iconImg.raycastTarget = false;
        LayoutElement iconLE = iconObj.AddComponent<LayoutElement>();
        iconLE.preferredWidth = iconSz;
        iconLE.preferredHeight = iconSz;
        iconLE.minWidth = iconSz;
        iconLE.minHeight = iconSz;

        // Bar background
        GameObject bgObj = CreateUiObject($"NeedBg{barName}", parent);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = bgRect.anchorMax = new Vector2(0f, 0.5f);
        bgRect.pivot = new Vector2(0f, 0.5f);
        bgRect.sizeDelta = new Vector2(barWidth, barH);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.10f, 0.14f, 1f);
        bgImg.raycastTarget = false;
        LayoutElement bgLE = bgObj.AddComponent<LayoutElement>();
        bgLE.preferredWidth = barWidth;
        bgLE.preferredHeight = barH;
        bgLE.minWidth = barWidth;
        bgLE.minHeight = barH;

        // Bar fill (left-anchored, width driven via sizeDelta.x at runtime)
        GameObject fillObj = CreateUiObject($"NeedFill{barName}", bgRect);
        Image fillImg = fillObj.AddComponent<Image>();
        fillImg.color = new Color(0.58f, 0.88f, 0.54f, 1f);
        fillImg.raycastTarget = false;
        RectTransform fillRect = fillObj.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = new Vector2(barWidth, 0f);

        return fillRect;
    }

}
