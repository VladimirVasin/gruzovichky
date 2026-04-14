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
    private bool isEconomyScreenDirty = true;
    private const int MaxDriverCardSlots = 8;
    private const int MaxShiftDriverSlots = 16;
    private const int MaxEconomyRowSlots = 64;
    private static readonly Color ShiftsScreenTint = new(0.06f, 0.08f, 0.11f, 0.76f);
    private static readonly Color ShiftsCardColor = new(0.13f, 0.16f, 0.21f, 0.98f);
    private static readonly Color ShiftsCardSelected = new(0.29f, 0.25f, 0.13f, 0.98f);
    private ShiftsScreenUiRefs shiftsScreenUi;
    private bool isShiftsScreenDirty = true;
    private BuildScreenUiRefs buildScreenUi;
    private bool isBuildScreenDirty = true;
    private WorldMapScreenUiRefs worldMapScreenUi;
    private bool isWorldMapScreenDirty = true;

    private sealed class DriversScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public RectTransform CardListContent;
        public readonly List<DriverCardUi> Cards = new();
        public Button HireButton;
        public Text   HireButtonText;
        public Text   HireStatusText;
        public Text   HeaderCountText;
    }

    private sealed class ShiftsScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public RectTransform DriverListContent;
        public Text HeaderCountText;
        public readonly List<ShiftDriverRowUi> DriverRows = new();
        public readonly List<ShiftCardUi> ShiftCards = new();
        public IntercitySlotUi IntercitySlot;
    }

    private sealed class ShiftDriverRowUi
    {
        public int DriverId;
        public RectTransform Root;
        public Image Background;
        public Text NameText;
        public Text StatusText;
        public Button SelectButton;
    }

    private sealed class ShiftCardUi
    {
        public int ShiftHour;
        public Text HeaderText;
        public RectTransform AssignedListRoot;
        public Text EmptyText;
        public readonly List<GameObject> AssignedRows = new();
        public readonly List<Text> AssignedDriverTexts = new();
        public readonly List<Button> RemoveButtons = new();
        public Text AssignButtonText;
        public Button AssignButton;
    }

    private sealed class IntercitySlotUi
    {
        public Text AssignedDriverText;
        public Text StatusText;
        public Text AssignButtonText;
        public Button AssignButton;
        public Button RemoveButton;
    }

    private sealed class BuildScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Button RoadButton;
        public Text RoadButtonText;
        public Text RoadTitleText;
        public Text RoadDescriptionText;
        public Button FurnitureFactoryButton;
        public Text FurnitureFactoryButtonText;
        public Text FurnitureFactoryTitleText;
        public Text FurnitureFactoryDescriptionText;
    }

    private sealed class DriverCardUi
    {
        public int   DriverId;
        public RectTransform Root;
        public Image Background;
        public Image StatusBadgeBackground;
        public Text  NameText;
        public Text  StatusText;
        public Text  TruckText;
        public Text  EnergyText;
        public Text  SalaryText;
        public Text  BalanceText;
        public Button SelectButton;
    }

    private sealed class ResourcesScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Text HeaderCountText;
        public Text TreasuryValueText;
        public string LastHeaderCount;
        public string LastTreasuryValue;
        public readonly List<ResourceSummaryRowUi> Rows = new();
    }

    private sealed class ResourceSummaryRowUi
    {
        public Text NameText;
        public Text ValueText;
        public string LastName;
        public string LastValue;
        public TradeResourceType ResourceType;
        public Button ModeButton;
        public Text ModeButtonText;
        public RectTransform ThresholdControls;
        public Button DecrBtn;
        public Text ThresholdText;
        public Button IncrBtn;
    }

    private sealed class EconomyScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Text HeaderCountText;
        public Text TradeResourceText;
        public Text TradeModeText;
        public Text TradePriceText;
        public Text TradeEtaText;
        public Text TradeStatusText;
        public Button TradePrevButton;
        public Button TradeNextButton;
        public Button TradeModeButton;
        public Button TradeDispatchButton;
        public Text TradeDispatchButtonText;
        public RectTransform EntryListContent;
        public Text EmptyText;
        public readonly List<EconomyEntryRowUi> Rows = new();
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
        public Text DetailsDescriptionText;
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

    private sealed class EconomyEntryRowUi
    {
        public RectTransform Root;
        public Text TimeText;
        public Text AmountText;
        public Text FlowText;
        public Text ReasonText;
    }

    private ResourcesScreenUiRefs resourcesScreenUi;
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

        // Window root
        GameObject windowRoot = CreateUiObject("DriversWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        SetCenteredWindow(windowRect, 760f, 560f, -16f);
        driversScreenUi.WindowRoot = windowRect;
        Image windowBg = windowRoot.AddComponent<Image>();
        windowBg.color = DriversScreenTint;
        Outline windowOutline = windowRoot.AddComponent<Outline>();
        windowOutline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        windowOutline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup rootLayout = windowRoot.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 14;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        // Header
        RectTransform headerRow = CreateLayoutRow("DriversHeaderRow", windowRoot.transform, 40f, 0f);
        Text driversTitleText = CreateHeaderText("DriversTitle", headerRow, font, "Drivers", 24, TextAnchor.MiddleLeft, Color.white);
        driversTitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        driversScreenUi.HeaderCountText = CreateHeaderText("DriversCount", headerRow, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        // Scroll area
        RectTransform listFrame = CreateStyledPanel("DriversListFrame", windowRoot.transform, FleetInsetColor);
        LayoutElement listFrameLayout = listFrame.gameObject.AddComponent<LayoutElement>();
        listFrameLayout.flexibleHeight = 1f;
        listFrameLayout.minHeight = 280f;

        // ScrollRect setup
        GameObject scrollObj = CreateUiObject("DriversScrollView", listFrame);
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
        viewportImage.raycastTarget = true;
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
        driversScreenUi.CardListContent = contentRect;

        // Pre-create card slots
        for (int i = 0; i < MaxDriverCardSlots; i++)
        {
            driversScreenUi.Cards.Add(CreateDriverCardV2(contentRect, font, i));
        }

        // Hire section
        RectTransform hireSection = CreateStyledPanel("HireSection", windowRoot.transform, FleetInsetColor);
        LayoutElement hireSectionLayout = hireSection.gameObject.AddComponent<LayoutElement>();
        hireSectionLayout.preferredHeight = 96f;
        VerticalLayoutGroup hireLayout = hireSection.gameObject.AddComponent<VerticalLayoutGroup>();
        hireLayout.padding = new RectOffset(18, 18, 16, 14);
        hireLayout.spacing = 10;
        hireLayout.childAlignment = TextAnchor.MiddleCenter;
        hireLayout.childControlWidth = true;
        hireLayout.childControlHeight = true;
        hireLayout.childForceExpandWidth = true;
        hireLayout.childForceExpandHeight = false;

        driversScreenUi.HireButton = CreateButton("HireDriverButton", hireSection, font, out driversScreenUi.HireButtonText, "Hire New Driver", 16, FleetPrimaryButtonColor, Color.white);
        LayoutElement hireButtonLayout = driversScreenUi.HireButton.gameObject.AddComponent<LayoutElement>();
        hireButtonLayout.preferredHeight = 44f;
        hireButtonLayout.minWidth = 320f;
        driversScreenUi.HireButton.onClick.AddListener(() =>
        {
            LogUiInput("Drivers Canvas: clicked Hire New Driver");
            HireNewDriver();
            isDriversScreenDirty = true;
        });
        driversScreenUi.HireStatusText = CreateBodyText("HireStatus", hireSection, font, string.Empty, 12, TextAnchor.MiddleCenter, FleetSecondaryTextColor);

        AddOverlayCloseButton(windowRect, font);
        driversScreenUi.CanvasRoot.SetActive(false);
        UpdateDriversScreenUi();
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

        // Row 3: Energy
        card.EnergyText = CreateBodyText("Energy", cardObj.transform, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        card.EnergyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        // Row 4: Salary + Balance
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
        CreateBodyText("TruckLabel", truckPanel, font, "Truck", 11, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;
        card.TruckText = CreateBodyText("TruckValue", truckPanel, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        card.TruckText.fontStyle = FontStyle.Bold;
        card.TruckText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform energyPanel = CreateStyledPanel($"DriverEnergyPanel{cardIndex}", infoRow, FleetCardMutedColor);
        energyPanel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup energyPanelLayout = energyPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        energyPanelLayout.padding = new RectOffset(12, 12, 8, 8);
        energyPanelLayout.spacing = 4;
        energyPanelLayout.childControlWidth = true;
        energyPanelLayout.childControlHeight = true;
        energyPanelLayout.childForceExpandWidth = true;
        energyPanelLayout.childForceExpandHeight = false;
        CreateBodyText("EnergyLabel", energyPanel, font, "Energy", 11, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;
        card.EnergyText = CreateBodyText("EnergyValue", energyPanel, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        card.EnergyText.fontStyle = FontStyle.Bold;
        card.EnergyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

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

        driversScreenUi.HeaderCountText.text = $"{driverAgents.Count} Driver{(driverAgents.Count == 1 ? "" : "s")}";

        for (int i = 0; i < driversScreenUi.Cards.Count; i++)
        {
            DriverCardUi card = driversScreenUi.Cards[i];
            bool active = i < driverAgents.Count;
            card.Root.gameObject.SetActive(active);
            if (!active) continue;

            DriverAgent d = driverAgents[i];
            TruckAgent truck = GetAssignedTruckForDriver(d);

            card.DriverId = d.DriverId;
            bool isSelected = selectedShiftDriverId == d.DriverId || selectedDriverId == d.DriverId;
            card.Background.color = isSelected ? DriversCardSelected : DriversCardColor;
            if (card.StatusBadgeBackground != null)
            {
                card.StatusBadgeBackground.color = d.IsArrivingByBus
                    ? new Color(0.22f, 0.36f, 0.54f, 1f)
                    : truck != null
                    ? new Color(0.35f, 0.29f, 0.14f, 1f)
                    : new Color(0.24f, 0.29f, 0.36f, 1f);
            }

            card.NameText.text = d.DriverName;
            card.StatusText.text = d.IsArrivingByBus
                ? "Arriving by Bus"
                : IsDriverOnActiveTradeRun(d)
                    ? "Trade Run"
                    : IsDriverIntercity(d)
                        ? "Intercity"
                        : truck != null
                            ? "Assigned"
                            : "Idle";

            card.TruckText.text = truck != null ? truck.DisplayName : "Unassigned";

            string energyMark = d.Energy <= DriverEnergyCriticalThreshold ? "  ⚠" : "";
            card.EnergyText.text = $"{Mathf.CeilToInt(d.Energy)} / {Mathf.CeilToInt(DriverEnergyMax)}{energyMark}";

            card.SalaryText.text = $"${d.Salary} / shift";
            card.BalanceText.text = $"${d.Money}";
        }

        bool canHire = money >= HireDriverCost && hiringDriverArrival == null;
        driversScreenUi.HireButton.interactable = canHire;
        driversScreenUi.HireButtonText.text = $"Hire New Driver — ${HireDriverCost}";
        driversScreenUi.HireStatusText.text = hiringDriverArrival != null
            ? "Another driver is currently arriving by bus."
            : canHire
                ? "New hires arrive at the bus stop before checking in at the motel."
                : $"Need ${HireDriverCost} to hire a new driver.";
        driversScreenUi.HireStatusText.color = canHire ? FleetSecondaryTextColor : new Color(0.96f, 0.72f, 0.42f, 1f);

        LayoutRebuilder.ForceRebuildLayoutImmediate(driversScreenUi.CardListContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(driversScreenUi.WindowRoot);
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
        SetCenteredWindow(windowRect, 980f, 600f, -16f);
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
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = true;

        RectTransform leftPanel = CreateStyledPanel("ShiftsDriverListPanel", windowRoot.transform, FleetPanelColor);
        LayoutElement leftPanelLayout = leftPanel.gameObject.AddComponent<LayoutElement>();
        leftPanelLayout.preferredWidth = 300f;
        leftPanelLayout.flexibleWidth = 0f;
        VerticalLayoutGroup leftLayout = leftPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        leftLayout.padding = new RectOffset(16, 16, 16, 16);
        leftLayout.spacing = 14;
        leftLayout.childControlWidth = true;
        leftLayout.childControlHeight = true;
        leftLayout.childForceExpandWidth = true;
        leftLayout.childForceExpandHeight = false;

        RectTransform leftHeader = CreateLayoutRow("ShiftsHeaderRow", leftPanel, 40f, 0f);
        Text shiftsTitle = CreateHeaderText("ShiftsTitle", leftHeader, font, "Shifts", 24, TextAnchor.MiddleLeft, Color.white);
        shiftsTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
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

            row.NameText = CreateHeaderText($"ShiftDriverName{i + 1}", rowObj.transform, font, string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
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
            });

            shiftsScreenUi.DriverRows.Add(row);
        }

        RectTransform rightPanel = CreateStyledPanel("ShiftsCardsPanel", windowRoot.transform, FleetPanelColor);
        LayoutElement rightPanelLayout = rightPanel.gameObject.AddComponent<LayoutElement>();
        rightPanelLayout.flexibleWidth = 1f;
        VerticalLayoutGroup rightLayout = rightPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        rightLayout.padding = new RectOffset(16, 16, 16, 16);
        rightLayout.spacing = 14;
        rightLayout.childControlWidth = true;
        rightLayout.childControlHeight = true;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = false;

        for (int i = 0; i < ShiftPresetHours.Length; i++)
        {
            int shiftHour = ShiftPresetHours[i];
            ShiftCardUi card = new() { ShiftHour = shiftHour };
            RectTransform cardRoot = CreateSectionCard(rightPanel, font, string.Empty, out RectTransform cardBody, false);
            cardRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 158f;
            VerticalLayoutGroup cardBodyLayout = cardBody.GetComponent<VerticalLayoutGroup>();
            cardBodyLayout.spacing = 8;

            card.HeaderText = CreateHeaderText($"ShiftHeader{i}", cardBody, font, $"{ShiftNames[i]}  {GetShiftRangeLabel(shiftHour)}", 16, TextAnchor.MiddleLeft, Color.white);

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
                if (inWindow && selectedDriver.RestPhase == DriverRestPhase.None && !IsDriverBusyWalkPhase(selectedDriver))
                {
                    StartDriverShiftCommute(selectedDriver);
                }

                PlayUiSound(uiSelectClip, 0.85f);
                SessionDebugLogger.Log("SHIFT", $"{selectedDriver.DriverName} assigned to {ShiftNames[currentShiftIndex]} ({GetShiftRangeLabel(ShiftPresetHours[currentShiftIndex])}).");
                LogDriverReaction(selectedDriver, $"assigned to {ShiftNames[currentShiftIndex]} ({GetShiftRangeLabel(ShiftPresetHours[currentShiftIndex])})");
                isShiftsScreenDirty = true;
                isDriversScreenDirty = true;
            });

            shiftsScreenUi.ShiftCards.Add(card);
        }

        shiftsScreenUi.IntercitySlot = new IntercitySlotUi();
        RectTransform intercityCard = CreateSectionCard(rightPanel, font, string.Empty, out RectTransform intercityBody, false);
        intercityCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 142f;
        VerticalLayoutGroup intercityLayout = intercityBody.GetComponent<VerticalLayoutGroup>();
        intercityLayout.spacing = 8;

        CreateHeaderText("IntercityHeader", intercityBody, font, "Intercity", 16, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.IntercitySlot.AssignedDriverText = CreateHeaderText("IntercityAssigned", intercityBody, font, string.Empty, 15, TextAnchor.MiddleLeft, FleetAccentColor);
        shiftsScreenUi.IntercitySlot.AssignedDriverText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        shiftsScreenUi.IntercitySlot.StatusText = CreateBodyText("IntercityStatus", intercityBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        shiftsScreenUi.IntercitySlot.StatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

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
        }

        if (!shouldShow) return;
        if (!isShiftsScreenDirty) return;

        shiftsScreenUi.HeaderCountText.text = $"{driverAgents.Count} Driver{(driverAgents.Count == 1 ? "" : "s")}";

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
            bool isAssigned = driver.ShiftStartHour >= 0;
            bool isIntercity = IsDriverIntercity(driver);
            row.StatusText.text = isIntercity
                ? "Intercity"
                : isAssigned
                    ? $"Assigned: {GetShiftRangeLabel(driver.ShiftStartHour)}"
                    : "Idle";
            row.StatusText.color = isIntercity
                ? FleetAccentColor
                : isAssigned ? new Color(0.62f, 0.92f, 0.62f, 1f) : FleetMutedTextColor;
        }

        DriverAgent selectedDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);

        for (int i = 0; i < shiftsScreenUi.ShiftCards.Count; i++)
        {
            ShiftCardUi card = shiftsScreenUi.ShiftCards[i];
            List<DriverAgent> assignedDrivers = new();
            foreach (DriverAgent driver in driverAgents)
            {
                if (!IsDriverIntercity(driver) && driver.ShiftStartHour == card.ShiftHour)
                {
                    assignedDrivers.Add(driver);
                }
            }

            card.EmptyText.gameObject.SetActive(assignedDrivers.Count == 0);
            for (int rowIndex = 0; rowIndex < card.AssignedRows.Count; rowIndex++)
            {
                bool rowActive = rowIndex < assignedDrivers.Count;
                card.AssignedRows[rowIndex].SetActive(rowActive);
                if (!rowActive) continue;

                card.AssignedDriverTexts[rowIndex].text = assignedDrivers[rowIndex].DriverName;
            }

            bool alreadyAssigned = selectedDriver != null && selectedDriver.ShiftStartHour == card.ShiftHour;
            bool intercitySelected = IsDriverIntercity(selectedDriver);
            card.AssignButton.interactable = selectedDriver != null && !alreadyAssigned && !intercitySelected;
            card.AssignButtonText.text = selectedDriver == null
                ? "Select a driver to assign"
                : intercitySelected
                    ? $"{selectedDriver.DriverName} is Intercity"
                    : alreadyAssigned
                        ? $"{selectedDriver.DriverName} already assigned"
                        : $"Assign {selectedDriver.DriverName} -> {ShiftNames[i]}";
        }

        UpdateIntercitySlotUi(selectedDriver);

        LayoutRebuilder.ForceRebuildLayoutImmediate(shiftsScreenUi.DriverListContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(shiftsScreenUi.WindowRoot);
        isShiftsScreenDirty = false;
    }

    private void UpdateIntercitySlotUi(DriverAgent selectedDriver)
    {
        if (shiftsScreenUi?.IntercitySlot == null)
        {
            return;
        }

        DriverAgent intercityDriver = GetIntercityAssignedDriver();
        bool selectedIsIntercity = IsDriverIntercity(selectedDriver);
        shiftsScreenUi.IntercitySlot.AssignedDriverText.text = intercityDriver != null ? intercityDriver.DriverName : "No driver assigned";
        shiftsScreenUi.IntercitySlot.StatusText.text = intercityDriver != null
            ? "Reserved for future trade runs"
            : "Assign one dedicated driver to intercity duty";
        shiftsScreenUi.IntercitySlot.AssignButton.interactable = selectedDriver != null && !selectedIsIntercity;
        shiftsScreenUi.IntercitySlot.AssignButtonText.text = selectedDriver == null
            ? "Select a driver"
            : selectedIsIntercity
                ? $"{selectedDriver.DriverName} already Intercity"
                : $"Assign {selectedDriver.DriverName}";
        shiftsScreenUi.IntercitySlot.RemoveButton.interactable = intercityDriver != null;
    }

    private DriverAgent GetIntercityAssignedDriver()
    {
        return driverAgents.Find(driver => driver.DriverId == intercityDriverId && IsDriverIntercity(driver));
    }

    private void AssignDriverToIntercitySlot(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

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

}
