using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static readonly Color DriversScreenTint   = new(0.035f, 0.075f, 0.12f, 0.88f);
    private static readonly Color DriversCardColor    = new(0.055f, 0.115f, 0.18f, 0.98f);
    private static readonly Color DriversCardSelected = new(0.22f, 0.17f, 0.075f, 0.98f);

    private DriversScreenUiRefs driversScreenUi;
    private bool isDriversScreenDirty = true;
    private WorkerDetailTab activeWorkerDetailTab = WorkerDetailTab.Profile;
    private int  selectedWorkerPanelDriverId = 0;
    private bool shouldScrollWorkersListToSelected;
    private bool isEconomyScreenDirty = true;
    private const int InitialWorkerRowSlots = 8;
    private const int MaxShiftDriverSlots = 32;
    private const int MaxVacancyOptionRows = 18;
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
    private int selectedVacancyIndex = -1;
    private int selectedVacancyShiftIndex = -1;
    private int selectedVacancyTruckNumber = 0;
    private string vacancySuccessMessage = string.Empty;
    private float vacancySuccessTimer;
    private Button shiftsLogisticsTabBtn;
    private Button shiftsTransportTabBtn;
    private Text shiftsLogisticsTabText;
    private Text shiftsTransportTabText;
    private RectTransform shiftsLogisticsPanel;
    private RectTransform shiftsTransportPanel;
    private string lastShiftsHudDebugState = string.Empty;
    private bool hasLoggedLegacyShiftsHudDraw;
    private readonly LogisticsSlotUi[] logisticsSlots = new LogisticsSlotUi[AssignableBuildingWorkerSlotCapacity];
    private readonly List<VacancyViewModel> vacancyViewModels = new();
    private readonly List<VacancyFlowOption> vacancyFlowOptions = new();
    private BuildScreenUiRefs buildScreenUi;
    private bool isBuildScreenDirty = true;
    private WorldMapScreenUiRefs worldMapScreenUi;
    private bool isWorldMapScreenDirty = true;

    private enum WorkerDetailTab
    {
        Profile,
        Social,
        Thoughts,
        Knowledge,
        Inventory
    }

    private sealed class DriversScreenUiRefs
    {
        public GameObject    CanvasRoot;
        public RectTransform WindowRoot;
        public RectTransform LeftPanel;
        public RectTransform RightPanel;
        public RectTransform WorkerListContent;
        public ScrollRect WorkerListScrollRect;
        public readonly List<WorkerRowUi> WorkerRows = new();
        public Text   HeaderCountText;
        // Right panel detail view.
        public GameObject DetailPlaceholderCard;
        public GameObject DetailContentRoot;
        public Button DetailProfileTabButton;
        public Text DetailProfileTabText;
        public Button DetailSocialTabButton;
        public Text DetailSocialTabText;
        public Button DetailThoughtsTabButton;
        public Text DetailThoughtsTabText;
        public Button DetailKnowledgeTabButton;
        public Text DetailKnowledgeTabText;
        public Button DetailInventoryTabButton;
        public Text DetailInventoryTabText;
        public GameObject DetailProfileTabRoot;
        public GameObject DetailSocialTabRoot;
        public GameObject DetailThoughtsTabRoot;
        public GameObject DetailKnowledgeTabRoot;
        public GameObject DetailInventoryTabRoot;
        public Text  DetailNameText;
        public Text  DetailProfileTitleText;
        public RectTransform DetailPortraitRoot;
        public ResidentInfoTileUi DetailGenderTile;
        public ResidentInfoTileUi DetailAgeTile;
        public ResidentInfoTileUi DetailEducationTile;
        public ResidentInfoTileUi DetailMoneyTile;
        public ResidentInfoTileUi DetailHomeTile;
        public ResidentInfoTileUi DetailCarTile;
        public Text DetailSkillsTitleText;
        public WorkerSkillTileUi DetailLogisticsSkillTile;
        public WorkerSkillTileUi DetailProductionSkillTile;
        public WorkerSkillTileUi DetailServiceSkillTile;
        public Text  DetailNeedsTitleText;
        public WorkerNeedRowUi DetailMealNeedRow;
        public WorkerNeedRowUi DetailSleepNeedRow;
        public WorkerNeedRowUi DetailLeisureNeedRow;
        public Text DetailOverallNeedsLabelText;
        public Text DetailOverallNeedsValueText;
        public Text  DetailPerksTitleText;
        public Text  DetailPerksEmptyText;
        public readonly List<Text> DetailPerkTexts = new();
        public GameObject DetailTraitTooltipRoot;
        public Text DetailTraitTooltipTitleText;
        public Text DetailTraitTooltipBodyText;
        public Image DetailStatusBadge;
        public Text  DetailStatusText;
        public RectTransform DetailAssignmentRow;
        public Text  DetailAssignmentLabel;
        public Text  DetailAssignmentValue;
        public RectTransform DetailShiftRow;
        public Text  DetailShiftLabel;
        public Text  DetailShiftText;
        public RectTransform DetailDutyRow;
        public Text  DetailDutyLabel;
        public Text  DetailDutyStateText;
        public LayoutElement DetailWorkCardLayout;
        public Text  DetailWorkTitleText;
        public Text  DetailSocialTitleText;
        public RectTransform DetailSocialGraphCanvas;
        public Text  DetailSocialEmptyText;
        public Text  DetailCurrentThoughtTitleText;
        public Image DetailCurrentThoughtBackground;
        public Outline DetailCurrentThoughtOutline;
        public Image DetailCurrentThoughtIcon;
        public Text  DetailCurrentThoughtHeadlineText;
        public Text  DetailCurrentThoughtDescriptionText;
        public RectTransform DetailCurrentThoughtTimeRow;
        public Image DetailCurrentThoughtTimeIcon;
        public Text  DetailCurrentThoughtTimeText;
        public Text  DetailDailyOpinionTitleText;
        public Image DetailDailyOpinionBackground;
        public Outline DetailDailyOpinionOutline;
        public Image DetailDailyOpinionIcon;
        public Text  DetailDailyOpinionToneText;
        public Text  DetailDailyOpinionSummaryText;
        public Text  DetailDailyOpinionReasonText;
        public Text  DetailDailyOpinionScoreText;
        public Text  DetailRecentThoughtsTitleText;
        public Text  DetailRecentThoughtsEmptyText;
        public readonly List<WorkerThoughtRowUi> DetailThoughtRows = new();
        public Text  DetailLifeOpinionsTitleText;
        public readonly List<WorkerLifeOpinionRowUi> DetailLifeOpinionRows = new();
        public Text  DetailKnowledgeTitleText;
        public Text  DetailKnowledgeEmptyText;
        public readonly List<WorkerKnowledgeRowUi> DetailKnowledgeRows = new();
        public Text  DetailInventoryTitleText;
        public GameObject DetailInventoryCardRowRoot;
        public Text  DetailInventoryEmptyText;
        public WorkerAutoConsumableCardUi DetailSnackCard;
        public WorkerAutoConsumableCardUi DetailCoffeeCard;
        public Button DetailFocusButton;
        public Text   DetailFocusButtonText;
    }

    private sealed class WorkerRowUi
    {
        public int           DriverId;
        public RectTransform Root;
        public RectTransform PortraitRoot;
        public Image         Background;
        public Outline       Outline;
        public Text          NameText;
        public Text          RaceText;
        public Text          JobText;
        public Text          BalanceText;
        public Button        SelectButton;
    }

    private sealed class ResidentInfoTileUi
    {
        public Text LabelText;
        public Text ValueText;
    }

    private sealed class WorkerSkillTileUi
    {
        public Text LabelText;
        public Text ValueText;
    }

    private sealed class WorkerNeedRowUi
    {
        public Image IconImage;
        public Text LabelText;
        public readonly List<Image> SegmentImages = new();
        public Text StatusText;
    }

    private sealed class WorkerThoughtRowUi
    {
        public RectTransform Root;
        public Image Background;
        public Text TimeText;
        public Image IconImage;
        public Text TitleText;
        public Text DescriptionText;
    }

    private sealed class WorkerLifeOpinionRowUi
    {
        public RectTransform Root;
        public Image IconImage;
        public Text CategoryText;
        public Image StatusDot;
        public Text StatusText;
    }

    private sealed class WorkerKnowledgeRowUi
    {
        public RectTransform Root;
        public Image Background;
        public Image IconImage;
        public Text TimeText;
        public Text TitleText;
        public Text DescriptionText;
        public Text MetaText;
        public RectTransform ExpiryFillRect;
        public Image ExpiryFillImage;
        public Text ExpiryText;
    }

    private sealed class WorkerAutoConsumableCardUi
    {
        public RectTransform Root;
        public Image Background;
        public Image ItemIcon;
        public Image TriggerIcon;
        public Image EffectIcon;
        public Image AutoIcon;
        public Text NameText;
        public Text QuantityText;
        public Text TypeText;
        public Text TriggerText;
        public Text EffectText;
        public Text AutoText;
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
        public RectTransform TabRowRoot;
        public RectTransform VacancyFlowPanel;
        public readonly List<Image> VacancyStepBackgrounds = new();
        public readonly List<Text> VacancyStepTexts = new();
        public Text VacancySuccessText;
        public RectTransform VacancyTransportParkCard;
        public Text VacancyTransportParkTitleText;
        public Text VacancyTransportParkCountText;
        public Text VacancyTransportParkSummaryText;
        public Button VacancyBuyTruckButton;
        public Text VacancyBuyTruckButtonText;
        public Text VacancyBuyTruckStatusText;
        public Text VacancyFlowTitleText;
        public Text VacancyFlowHintText;
        public readonly List<VacancyOptionRowUi> VacancyOptionRows = new();
        public Text LogisticsSectionTitleText;
        public Text LogisticsSectionSummaryText;
        public Text FleetSectionTitleText;
        public Text FleetSectionSummaryText;
        public Text FleetCountText;
        public Button FleetBuyTruckButton;
        public Text FleetBuyTruckButtonText;
        public Text FleetBuyTruckStatusText;
        public Text ProductionSectionTitleText;
        public Text ProductionSectionSummaryText;
        public Text BusDriverGroupTitleText;
        public Text BusDriverGroupSummaryText;
        public readonly List<ShiftDriverRowUi> DriverRows = new();
        public readonly List<ShiftCardUi> ShiftCards = new();
        public readonly List<ShiftsFleetTruckRowUi> FleetTruckRows = new();
        public IntercitySlotUi IntercitySlot;
        public readonly List<IntercitySlotUi> BusDriverSlots = new();
    }

    private sealed class ShiftDriverRowUi
    {
        public int DriverId;
        public RectTransform Root;
        public Image Background;
        public Image SelectedBorder;
        public Image BadgeBackground;
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

    private enum VacancyKind
    {
        None,
        TruckDriver,
        Intercity,
        BusDriver,
        Production,
        Service
    }

    private enum VacancyFlowOptionKind
    {
        Shift,
        Truck,
        Worker,
        Remove,
        BuyTruck
    }

    private sealed class VacancyViewModel
    {
        public VacancyKind Kind;
        public string Title;
        public string Subtitle;
        public string Schedule;
        public bool IsOccupied;
        public DriverAgent AssignedWorker;
        public LocationType BuildingType;
        public int LocationInstanceId;
        public int SlotIndex;
        public int ShiftIndex = -1;
        public int TruckNumber;
        public bool IsGroupedWarehouse;
        public int FilledSlots;
        public int MaxSlots;
        public int OfferSalary;
        public int ContractWorkDays;
        public int MarketPressure;
        public int RequiredProfessionalLevel = 1;
    }

    private sealed class VacancyFlowOption
    {
        public VacancyFlowOptionKind Kind;
        public string Title;
        public string Subtitle;
        public int ShiftIndex = -1;
        public int TruckNumber;
        public DriverAgent Worker;
    }

    private sealed class VacancyOptionRowUi
    {
        public RectTransform Root;
        public Image Background;
        public Text TitleText;
        public Text SubtitleText;
        public Image BadgeBackground;
        public Text BadgeText;
        public Button Button;
    }

    private sealed class ShiftsFleetTruckRowUi
    {
        public int TruckNumber;
        public RectTransform Root;
        public Image Background;
        public Text NameText;
        public Text StatusText;
        public Text CrewText;
        public Text CargoText;
        public Button FocusButton;
        public Button AssignButton;
        public Text AssignButtonText;
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
        public string        TitleFallback;
        public Color         DefaultAccentColor;
        public bool          IsHovered;
        public float         HoverT;
        public RectTransform Root;
        public Button        Button;
        public Image         CardBg;
        public Image         UnlockGlow;
        public Image         AccentBg;
        public Text          TitleText;
        public Text          StatusText;
        public Image         StatusBg;
        public bool          IsUnaffordable;
    }

    private sealed class BuildCategoryUi
    {
        public string        LabelEn;
        public string        LabelRu;
        public int           Index;
        public bool          IsExpanded;
        public bool          IsHovered;
        public float         HoverT;
        public RectTransform HeaderRoot;
        public Button        HeaderButton;
        public Image         HeaderBg;
        public Image         UnlockGlow;
        public RectTransform IconRoot;
        public Text          HeaderText;
        public BuildItemUi[] Items;
    }

    private sealed class BuildScreenUiRefs
    {
        public GameObject        CanvasRoot;
        public RectTransform     WindowRoot;
        public RectTransform     DockRoot;
        public RectTransform     CategoryRowRoot;
        public RectTransform     ItemTrayRoot;
        public CanvasGroup       PanelGroup;
        public CanvasGroup       ItemTrayGroup;
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
        public Text RegionText;
        public Button RemoveButton;
        public int OrderId;
    }

    private sealed class TradePolicyRowUi
    {
        public RectTransform Root;
        public TradeResourceType ResourceType;
        public Text ResourceText;
        public Text WarehouseText;
        public RectTransform ModeOptionsRoot;
        public Button NoTradeButton;
        public Text NoTradeButtonText;
        public Button SellAboveButton;
        public Text SellAboveButtonText;
        public Button BuyUpToButton;
        public Text BuyUpToButtonText;
        public Button TargetMinusButton;
        public Text TargetMinusText;
        public Text TargetText;
        public Button TargetPlusButton;
        public Text TargetPlusText;
        public Text StatusText;
    }

    private sealed class TradeScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Text HeaderCountText;
        public Text StatusText;
        public RectTransform RowsContent;
        public readonly List<TradePolicyRowUi> PolicyRows = new();
    }

    private sealed class WorldMapResourceRowUi
    {
        public GameObject Root;
        public TradeResourceType ResourceType;
    }

    private sealed class WorldMapScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Text TitleText;
        public Text SubtitleText;
        public Text SelectionHintText;
        public RectTransform MapRoot;
        public readonly List<Image> RegionRouteLines = new();
        public readonly List<WorldMapCellUi> Cells = new();
        public GameObject DetailsPanelRoot;
        public Text DetailsNameText;
        public Text DetailsStatusText;
        public Text DetailsSellsLabelText;
        public Text DetailsResourcesText;
        public Text DetailsBuysLabelText;
        public Text DetailsImportsText;
        public RectTransform DetailsSellsListRoot;
        public RectTransform DetailsBuysListRoot;
        public readonly List<WorldMapResourceRowUi> DetailsSellsRows = new();
        public readonly List<WorldMapResourceRowUi> DetailsBuysRows = new();
        public Text DetailsDescriptionText;
        // trade route bottom panel
        public GameObject RoutePanelRoot;
        public Text RoutePanelTitleText;
        public Text RouteStatusText;
        public Button BuildRouteButton;
        public Text BuildRouteButtonText;
        public Button OpenTradeButton;
        public Text OpenTradeButtonText;
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
        public int           LocationInstanceId;
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
    private TradeScreenUiRefs tradeScreenUi;

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
        tex.filterMode = FilterMode.Point;
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
