using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static readonly string[] SlotSymbols =
    {
        "7",
        "\u2605",
        "\u2666",
        "\u2665",
        "\u2663",
        "\u2660"
    };

    private enum GamblingSlotPhase { Idle, ShowBet, Spinning, Done, ResultPause }

    private sealed class ServiceWorkerSlotUi
    {
        public RectTransform Root;
        public LayoutElement  SlotLayout;
        public RectTransform PortraitRoot;
        public Text NameText;
        public Text ActivityText;
        public LayoutElement ActivityTextLayout;
        public Text TimeText;
        public RectTransform ProgressBarFill;
        public Image ProgressBarFillImage;
        public Button FocusButton;
        public int CurrentDriverId = -1;

        // Slot machine reels
        public RectTransform    ReelRow;
        public Text[]           ReelTexts        = new Text[3];
        public GamblingSlotPhase SlotPhase       = GamblingSlotPhase.Idle;
        public float            SpinTimer;
        public float            SpinCycleTimer;
        public float            ResultDisplayTimer;
        public bool[]           ReelStopped      = new bool[3];
        public string[]         FinalReelChars   = new string[3];
        public int              LastSpinBet;
    }

    private sealed class PersonalHouseResidentRowUi
    {
        public RectTransform Root;
        public Text NameText;
        public Button ActionButton;
        public Text ActionButtonText;
        public Image ActionButtonImage;
        public int CurrentDriverId = -1;
    }

    private sealed class BuildingQuickHudRefs
    {
        public GameObject CanvasRoot;
        public RectTransform Root;
        public RectTransform HeaderIconRoot;
        public Image HeaderIconImage;
        public Text HeaderText;
        public RectTransform SummaryCard;
        public LayoutElement SummaryCardLayout;
        public Text TypeText;
        public Text StatusText;
        public LayoutElement StatusTextLayout;
        public Text ResourceText;
        public LayoutElement ResourceTextLayout;
        public RectTransform ContextButtonRow;
        public Button ContextButton;
        public Text ContextButtonText;
        public RectTransform StopNumberRow;
        public Button StopNumberDecreaseButton;
        public Text StopNumberDecreaseText;
        public Text StopNumberLabelText;
        public Button StopNumberIncreaseButton;
        public Text StopNumberIncreaseText;
        public Button CloseButton;
        public Text CloseButtonText;
        public RectTransform WorkerSlotsSection;
        public Text WorkerSlotsSectionHeader;
        public Text WorkerSlotsEmptyText;
        public ScrollRect WorkerSlotsScroll;
        public RectTransform WorkerSlotsContent;
        public ServiceWorkerSlotUi[] WorkerSlots;
        public RectTransform PersonalHouseSection;
        public Text PersonalHouseSectionHeader;
        public PersonalHouseResidentRowUi[] ResidentRows;
        public RectTransform CityHallCard;
        public Text CityHallTitleText;
        public Text CityHallDescriptionText;
        public CityHallQuickHudRowUi CityHallCompletionRow;
        public CityHallQuickHudRowUi CityHallPenaltyRow;
        public CityHallQuickHudRowUi CityHallRequestsStatusRow;
        public CityHallQuickHudRowUi CityHallGoalStatusRow;
        public Button CityHallOpenButton;
        public Text CityHallOpenButtonText;
        public Image CityHallOpenButtonIcon;
        public Image      FlashOverlay;
        public Vector2    OriginalPos;
        public RectTransform LinkLine;
        public Image LinkLineImage;
    }

    private static readonly Color BuildingQuickHudLinkLineColor = new(1f, 0.74f, 0.25f, 0.72f);
    private const float BuildingQuickHudLinkLineThickness = 3f;
    private readonly Vector3[] buildingQuickHudLinkCorners = new Vector3[4];
    private BuildingQuickHudRefs buildingQuickHud;

    private bool HasBlockingHudOpenForQuickHuds()
    {
        return isFleetPanelOpen ||
               isDriversPanelOpen ||
               isShiftsPanelOpen ||
               isResourcesPanelOpen ||
               isEconomyPanelOpen ||
               isTradePanelOpen ||
               isBuildPanelOpen ||
               isWorldMapPanelOpen ||
               isStatesPanelOpen ||
               isSocialGraphPanelOpen ||
               isCityHallPanelOpen ||
               isBarInteriorSceneOpen ||
               activeBuildTool != BuildTool.None;
    }

    private void CloseQuickHudsWhenBlockingHudIsOpen()
    {
        if (!HasBlockingHudOpenForQuickHuds())
        {
            return;
        }

        bool hadAnyQuickHud =
            isTruckDetailsOpen ||
            isLocalBusDetailsOpen ||
            isDriverDetailsOpen ||
            selectedLocation.HasValue ||
            selectedLocalStopIndex >= 0 ||
            selectedDebugCell.HasValue;

        if (!hadAnyQuickHud)
        {
            return;
        }

        bool preserveFleetTruckSelection = isFleetPanelOpen;
        if (!preserveFleetTruckSelection)
        {
            isTruckDetailsOpen = false;
        }

        isLocalBusDetailsOpen = false;
        isDriverDetailsOpen = false;
        selectedDriverId = 0;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        selectedDebugCell = null;

        if (truckQuickHud?.CanvasRoot != null) truckQuickHud.CanvasRoot.SetActive(false);
        if (localBusQuickHud?.CanvasRoot != null) localBusQuickHud.CanvasRoot.SetActive(false);
        if (driverQuickHud?.CanvasRoot != null) driverQuickHud.CanvasRoot.SetActive(false);
        if (buildingQuickHud?.CanvasRoot != null) buildingQuickHud.CanvasRoot.SetActive(false);
        HideBuildingQuickHudSubmenuImmediate();
        if (cellQuickHud?.CanvasRoot != null) cellQuickHud.CanvasRoot.SetActive(false);
        if (selectedDebugCellHighlight != null) selectedDebugCellHighlight.SetActive(false);
        if (selectedDebugCellOutline != null) selectedDebugCellOutline.SetActive(false);

        if (!preserveFleetTruckSelection)
        {
            DisableTruckCameraFocus();
        }

        RefreshSelectionVisuals();
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void SetupBuildingQuickHud()
    {
        if (buildingQuickHud != null)
        {
            return;
        }

        EnsureFleetEventSystem();
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buildingQuickHud = new BuildingQuickHudRefs();

        GameObject canvasObject = new("BuildingQuickHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        buildingQuickHud.CanvasRoot = canvasObject;
        CreateBuildingQuickHudLinkLine(canvasObject.transform);

        RectTransform root = CreateStyledPanel("BuildingQuickHudRoot", canvasObject.transform, FleetPanelColor);
        root.anchorMin = new Vector2(1f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-18f, 104f);
        root.sizeDelta = new Vector2(360f, 500f);
        VerticalLayoutGroup rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(16, 16, 16, 16);
        rootLayout.spacing = 14;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        buildingQuickHud.Root = root;
        buildingQuickHud.OriginalPos = root.anchoredPosition;

        RectTransform headerRow = CreateLayoutRow("BuildingQuickHudHeaderRow", root, 42f, 12f);
        buildingQuickHud.HeaderIconRoot = CreateUiObject("HeaderIcon", headerRow).GetComponent<RectTransform>();
        LayoutElement headerIconLayout = buildingQuickHud.HeaderIconRoot.gameObject.AddComponent<LayoutElement>();
        headerIconLayout.preferredWidth = 38f;
        headerIconLayout.preferredHeight = 38f;
        buildingQuickHud.HeaderIconImage = buildingQuickHud.HeaderIconRoot.gameObject.AddComponent<Image>();
        buildingQuickHud.HeaderIconImage.raycastTarget = false;
        buildingQuickHud.HeaderText = CreateHeaderText("Header", headerRow, uiFont, "Location", 21, TextAnchor.MiddleLeft, Color.white);
        buildingQuickHud.HeaderText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        buildingQuickHud.CloseButton = CreateButton("CloseButton", headerRow, uiFont, out buildingQuickHud.CloseButtonText, "X", 12, new Color(0.26f, 0.30f, 0.36f, 1f), Color.white);
        LayoutElement closeLayout = buildingQuickHud.CloseButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 36f;
        closeLayout.preferredHeight = 36f;
        buildingQuickHud.CloseButton.onClick.AddListener(() =>
        {
            if (TryGetSelectedBuilding(out LocationData selectedBuilding, out _, out _))
            {
                LogUiInput($"Quick HUD: closed {selectedBuilding.Label}");
            }

            selectedLocation = null;
            selectedLocalStopIndex = -1;
            selectedPersonalHouseIndex = -1;
            HideBuildingQuickHudSubmenuImmediate();
            SetBuildingQuickHudLinkLineVisible(false);
            RefreshSelectionVisuals();
            PlayUiSound(uiPanelCloseClip, 0.82f);
        });

        RectTransform summaryCard = CreateSectionCard(root, uiFont, string.Empty, out RectTransform summaryBody, false);
        buildingQuickHud.SummaryCard = summaryCard;
        buildingQuickHud.SummaryCardLayout = summaryCard.gameObject.AddComponent<LayoutElement>();
        buildingQuickHud.SummaryCardLayout.preferredHeight = 170f;
        buildingQuickHud.TypeText = CreateBodyText("TypeText", summaryBody, uiFont, string.Empty, 17, TextAnchor.MiddleLeft, Color.white);
        buildingQuickHud.TypeText.fontStyle = FontStyle.Bold;
        buildingQuickHud.TypeText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        buildingQuickHud.StatusText = CreateBodyText("StatusText", summaryBody, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        buildingQuickHud.StatusTextLayout = buildingQuickHud.StatusText.gameObject.AddComponent<LayoutElement>();
        buildingQuickHud.StatusTextLayout.preferredHeight = 28f;
        buildingQuickHud.ResourceText = CreateBodyText("ResourceText", summaryBody, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        buildingQuickHud.ResourceTextLayout = buildingQuickHud.ResourceText.gameObject.AddComponent<LayoutElement>();
        buildingQuickHud.ResourceTextLayout.preferredHeight = 82f;
        CreateCityHallBuildingQuickHud(root, uiFont);
        CreateMotelBuildingQuickHud(root, uiFont);
        CreateWarehouseBuildingQuickHud(root, uiFont);

        // в”Ђв”Ђ Worker slots section в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        RectTransform workerSection = CreateUiObject("WorkerSlotsSection", root).GetComponent<RectTransform>();
        VerticalLayoutGroup wSectionLayout = workerSection.gameObject.AddComponent<VerticalLayoutGroup>();
        wSectionLayout.spacing = 6f;
        wSectionLayout.childControlWidth  = true;
        wSectionLayout.childControlHeight = true;
        wSectionLayout.childForceExpandWidth  = true;
        wSectionLayout.childForceExpandHeight = false;
        workerSection.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        buildingQuickHud.WorkerSlotsSection = workerSection;

        buildingQuickHud.WorkerSlotsSectionHeader = CreateBodyText("WorkersHeader", workerSection, uiFont, "Workers inside", 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        buildingQuickHud.WorkerSlotsSectionHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        buildingQuickHud.WorkerSlotsEmptyText = CreateBodyText("WorkersEmpty", workerSection, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        buildingQuickHud.WorkerSlotsEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        buildingQuickHud.WorkerSlotsEmptyText.gameObject.SetActive(false);

        FleetCanvasUiFactory.ScrollPanelRefs workerScroll = CreateVerticalScrollList("WorkerSlotsScroll", workerSection, "WorkerSlotsContent", 6f, preferredHeight: 190f);
        buildingQuickHud.WorkerSlotsScroll = workerScroll.ScrollRect;
        buildingQuickHud.WorkerSlotsContent = workerScroll.Content;

        const int MaxServiceSlots = 4;
        buildingQuickHud.WorkerSlots = new ServiceWorkerSlotUi[MaxServiceSlots];
        for (int i = 0; i < MaxServiceSlots; i++)
        {
            ServiceWorkerSlotUi slot = new ServiceWorkerSlotUi();
            buildingQuickHud.WorkerSlots[i] = slot;

            RectTransform slotRoot = CreateUiObject($"WorkerSlot{i}", buildingQuickHud.WorkerSlotsContent).GetComponent<RectTransform>();
            Image slotBg = slotRoot.gameObject.AddComponent<Image>();
            slotBg.color = new Color(0.14f, 0.18f, 0.25f, 1f);
            int capturedWorkerIndex = i;
            slot.FocusButton = slotRoot.gameObject.AddComponent<Button>();
            slot.FocusButton.targetGraphic = slotBg;
            slot.FocusButton.onClick.AddListener(() => OnBuildingWorkerSlotClick(capturedWorkerIndex));
            slot.SlotLayout = slotRoot.gameObject.AddComponent<LayoutElement>();
            slot.SlotLayout.preferredHeight = 54f;
            HorizontalLayoutGroup slotLayout = slotRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            slotLayout.padding = new RectOffset(6, 8, 4, 4);
            slotLayout.spacing = 8f;
            slotLayout.childControlWidth  = true;
            slotLayout.childControlHeight = true;
            slotLayout.childForceExpandWidth  = false;
            slotLayout.childForceExpandHeight = true;
            slot.Root = slotRoot;

            // Portrait container (clips the scaled portrait drawing)
            RectTransform portraitGo = CreateUiObject($"Portrait{i}", slotRoot).GetComponent<RectTransform>();
            portraitGo.gameObject.AddComponent<RectMask2D>();
            portraitGo.gameObject.AddComponent<LayoutElement>().preferredWidth = 40f;
            slot.PortraitRoot = portraitGo;

            // Right side: name row + activity + progress bar
            RectTransform rightSide = CreateUiObject($"SlotRight{i}", slotRoot).GetComponent<RectTransform>();
            rightSide.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup rightLayout = rightSide.gameObject.AddComponent<VerticalLayoutGroup>();
            rightLayout.spacing = 2f;
            rightLayout.padding = new RectOffset(0, 0, 4, 4);
            rightLayout.childControlWidth  = true;
            rightLayout.childControlHeight = true;
            rightLayout.childForceExpandWidth  = true;
            rightLayout.childForceExpandHeight = false;

            // Name row: name + time remaining
            RectTransform nameRow = CreateLayoutRow($"SlotNameRow{i}", rightSide, 17f, 0f);
            slot.NameText = CreateBodyText($"SlotName{i}", nameRow, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
            slot.NameText.fontStyle = FontStyle.Bold;
            slot.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            slot.TimeText = CreateBodyText($"SlotTime{i}", nameRow, uiFont, string.Empty, 11, TextAnchor.MiddleRight, FleetSecondaryTextColor);
            slot.TimeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 50f;

            // Slot machine reel row (hidden by default, shown only for GamblingHall)
            RectTransform reelRow = CreateUiObject($"ReelRow{i}", rightSide).GetComponent<RectTransform>();
            HorizontalLayoutGroup reelHlg = reelRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            reelHlg.spacing = 5f;
            reelHlg.childControlWidth = true;
            reelHlg.childControlHeight = true;
            reelHlg.childForceExpandWidth = false;
            reelHlg.childForceExpandHeight = true;
            reelRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
            slot.ReelRow = reelRow;
            reelRow.gameObject.SetActive(false);

            for (int r = 0; r < 3; r++)
            {
                RectTransform reelBox = CreateUiObject($"Reel{i}_{r}", reelRow).GetComponent<RectTransform>();
                reelBox.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.16f, 1f);
                reelBox.gameObject.AddComponent<LayoutElement>().preferredWidth = 30f;
                Text reelTxt = CreateBodyText($"ReelChar{i}_{r}", reelBox, uiFont, "?", 18, TextAnchor.MiddleCenter, Color.gray);
                reelTxt.fontStyle = FontStyle.Bold;
                slot.ReelTexts[r] = reelTxt;
            }

            // Activity label
            slot.ActivityText = CreateBodyText($"SlotActivity{i}", rightSide, uiFont, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            slot.ActivityTextLayout = slot.ActivityText.gameObject.AddComponent<LayoutElement>();
            slot.ActivityTextLayout.preferredHeight = 14f;

            // Progress bar
            RectTransform barBg = CreateUiObject($"SlotBarBg{i}", rightSide).GetComponent<RectTransform>();
            barBg.gameObject.AddComponent<Image>().color = new Color(0.10f, 0.14f, 0.20f, 1f);
            barBg.gameObject.AddComponent<LayoutElement>().preferredHeight = 4f;

            RectTransform barFill = CreateUiObject($"SlotBarFill{i}", barBg).GetComponent<RectTransform>();
            barFill.anchorMin = new Vector2(0f, 0f);
            barFill.anchorMax = new Vector2(1f, 1f);
            barFill.sizeDelta = Vector2.zero;
            barFill.anchoredPosition = Vector2.zero;
            Image fillImage = barFill.gameObject.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.8f, 0.3f);
            slot.ProgressBarFill = barFill;
            slot.ProgressBarFillImage = fillImage;
        }

        // ── Personal house residents section ─────────────────────────────────────
        RectTransform houseSection = CreateUiObject("PersonalHouseSection", root).GetComponent<RectTransform>();
        VerticalLayoutGroup houseSectionLayout = houseSection.gameObject.AddComponent<VerticalLayoutGroup>();
        houseSectionLayout.spacing = 4f;
        houseSectionLayout.childControlWidth  = true;
        houseSectionLayout.childControlHeight = true;
        houseSectionLayout.childForceExpandWidth  = true;
        houseSectionLayout.childForceExpandHeight = false;
        houseSection.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        buildingQuickHud.PersonalHouseSection = houseSection;

        buildingQuickHud.PersonalHouseSectionHeader = CreateBodyText("HouseResidentsHeader", houseSection, uiFont, "Residents", 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        buildingQuickHud.PersonalHouseSectionHeader.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        const int MaxResidentRows = 8;
        buildingQuickHud.ResidentRows = new PersonalHouseResidentRowUi[MaxResidentRows];
        for (int i = 0; i < MaxResidentRows; i++)
        {
            PersonalHouseResidentRowUi resRow = new PersonalHouseResidentRowUi();
            buildingQuickHud.ResidentRows[i] = resRow;

            RectTransform rowRoot = CreateUiObject($"ResidentRow{i}", houseSection).GetComponent<RectTransform>();
            rowRoot.gameObject.AddComponent<Image>().color = new Color(0.14f, 0.18f, 0.25f, 1f);
            rowRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
            HorizontalLayoutGroup rowLayout = rowRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(10, 6, 4, 4);
            rowLayout.spacing = 8f;
            rowLayout.childControlWidth  = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth  = false;
            rowLayout.childForceExpandHeight = true;
            resRow.Root = rowRoot;

            resRow.NameText = CreateBodyText($"ResidentName{i}", rowRoot, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
            resRow.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            int capturedIndex = i;
            resRow.ActionButton = CreateButton($"ResidentBtn{i}", rowRoot, uiFont, out resRow.ActionButtonText, "+", 12, FleetPrimaryButtonColor, Color.white);
            resRow.ActionButtonImage = resRow.ActionButton.GetComponent<Image>();
            resRow.ActionButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 64f;
            resRow.ActionButton.onClick.AddListener(() => OnResidentRowButtonClick(capturedIndex));

            rowRoot.gameObject.SetActive(false);
        }

        CreateBuildingQuickHudExpandableSubmenu(root, uiFont);

        RectTransform actionRow = CreateLayoutRow("BuildingQuickHudActionRow", root, 34f, 10f);
        buildingQuickHud.ContextButtonRow = actionRow;
        buildingQuickHud.ContextButton = CreateButton("ContextButton", actionRow, uiFont, out buildingQuickHud.ContextButtonText, "Open", 13, FleetPrimaryButtonColor, Color.white);
        LayoutElement contextLayout = buildingQuickHud.ContextButton.gameObject.AddComponent<LayoutElement>();
        contextLayout.flexibleWidth = 1f;
        contextLayout.preferredHeight = 34f;
        buildingQuickHud.ContextButton.onClick.AddListener(OnBuildingQuickHudContextButtonClick);

        RectTransform stopNumberRow = CreateLayoutRow("StopNumberRow", root, 34f, 8f);
        buildingQuickHud.StopNumberRow = stopNumberRow;
        buildingQuickHud.StopNumberDecreaseButton = CreateButton("StopNumberMinus", stopNumberRow, uiFont, out buildingQuickHud.StopNumberDecreaseText, "-", 16, new Color(0.26f, 0.30f, 0.36f, 1f), Color.white);
        LayoutElement stopMinusLayout = buildingQuickHud.StopNumberDecreaseButton.gameObject.AddComponent<LayoutElement>();
        stopMinusLayout.preferredWidth = 40f;
        stopMinusLayout.preferredHeight = 30f;
        buildingQuickHud.StopNumberLabelText = CreateBodyText("StopNumberLabel", stopNumberRow, uiFont, string.Empty, 13, TextAnchor.MiddleCenter, Color.white);
        LayoutElement stopLabelLayout = buildingQuickHud.StopNumberLabelText.gameObject.AddComponent<LayoutElement>();
        stopLabelLayout.flexibleWidth = 1f;
        stopLabelLayout.preferredHeight = 30f;
        buildingQuickHud.StopNumberIncreaseButton = CreateButton("StopNumberPlus", stopNumberRow, uiFont, out buildingQuickHud.StopNumberIncreaseText, "+", 16, FleetPrimaryButtonColor, Color.white);
        LayoutElement stopPlusLayout = buildingQuickHud.StopNumberIncreaseButton.gameObject.AddComponent<LayoutElement>();
        stopPlusLayout.preferredWidth = 40f;
        stopPlusLayout.preferredHeight = 30f;
        buildingQuickHud.StopNumberDecreaseButton.onClick.AddListener(() => ShiftSelectedStopNumber(-1));
        buildingQuickHud.StopNumberIncreaseButton.onClick.AddListener(() => ShiftSelectedStopNumber(1));

        // Flash overlay - full-panel transparent image rendered on top
        RectTransform flashRt = CreateUiObject("FlashOverlay", root).GetComponent<RectTransform>();
        flashRt.anchorMin = Vector2.zero;
        flashRt.anchorMax = Vector2.one;
        flashRt.sizeDelta = Vector2.zero;
        flashRt.anchoredPosition = Vector2.zero;
        Image flashImg = flashRt.gameObject.AddComponent<Image>();
        flashImg.color = new Color(0f, 0f, 0f, 0f);
        flashImg.raycastTarget = false;
        buildingQuickHud.FlashOverlay = flashImg;
        buildingQuickHud.FlashOverlay.gameObject.SetActive(false);

        buildingQuickHud.CanvasRoot.SetActive(false);
        UpdateBuildingQuickHud();
    }

    private void UpdateBuildingQuickHud()
    {
        if (buildingQuickHud == null)
        {
            return;
        }

        bool shouldShow =
            (selectedLocation.HasValue || selectedLocalStopIndex >= 0 || selectedPersonalHouseIndex >= 0) &&
            !isTruckDetailsOpen &&
            !isFleetPanelOpen &&
            !isDriversPanelOpen &&
            !isShiftsPanelOpen &&
            !isResourcesPanelOpen &&
            !isEconomyPanelOpen &&
            !isTradePanelOpen &&
            !isBuildPanelOpen &&
            !isWorldMapPanelOpen &&
            !isStatesPanelOpen &&
            !isSocialGraphPanelOpen &&
            !isCityHallPanelOpen &&
            !isBarInteriorSceneOpen;

        if (buildingQuickHud.CanvasRoot.activeSelf != shouldShow)
        {
            buildingQuickHud.CanvasRoot.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            HideBuildingQuickHudSubmenuImmediate();
            SetBuildingQuickHudLinkLineVisible(false);
            return;
        }

        if (!TryGetSelectedBuilding(out LocationData location, out LocationType selectedBuildingType, out _))
        {
            HideBuildingQuickHudSubmenuImmediate();
            SetBuildingQuickHudLinkLineVisible(false);
            buildingQuickHud.CanvasRoot.SetActive(false);
            return;
        }

        bool ru = IsRussianLanguage();
        bool isCityHall = selectedBuildingType == LocationType.CityHall;
        bool isMotel = selectedBuildingType == LocationType.Motel;
        bool isWarehouse = selectedBuildingType == LocationType.Warehouse;
        ConfigureBuildingQuickHudMode(isCityHall, isMotel, isWarehouse);
        buildingQuickHud.HeaderText.text = isCityHall
            ? "\u0420\u0430\u0442\u0443\u0448\u0430 (\u0421\u0435\u0440\u0432\u0438\u0441)"
            : isMotel
                ? "\u041c\u043e\u0442\u0435\u043b\u044c"
            : isWarehouse
                ? "\u0421\u043a\u043b\u0430\u0434"
                : selectedBuildingType == LocationType.Docks
                    ? GetSelectedLocationDisplayName(selectedBuildingType)
                    : location.Label;
        buildingQuickHud.HeaderText.fontSize = isMotel || isCityHall || isWarehouse ? 22 : 21;
        string categoryTag = IsProductionLocation(selectedBuildingType) ? "  [Production]"
            : selectedBuildingType == LocationType.PersonalHouse ? "  [Housing]"
            : "  [Service]";
        if (isCityHall)
        {
            UpdateCityHallBuildingQuickHud();
        }
        else if (isMotel)
        {
            UpdateMotelBuildingQuickHud(location);
        }
        else if (isWarehouse)
        {
            UpdateWarehouseBuildingQuickHud(location);
        }
        else
        {
            buildingQuickHud.TypeText.text = GetSelectedLocationDisplayName(selectedBuildingType) + categoryTag;
            string quickStatus = GetBuildingQuickStatusText(selectedBuildingType);
            string quickResource = selectedBuildingType == LocationType.PersonalHouse
                ? GetPersonalHouseQuickResourceText()
                : GetBuildingQuickResourceText(selectedBuildingType);
            buildingQuickHud.StatusText.text = quickStatus;
            buildingQuickHud.ResourceText.text = quickResource;
            ConfigureBuildingQuickHudSummaryLayout(quickStatus, quickResource);
        }

        bool showContextBtn = HasBuildingContextAction(selectedBuildingType) && !isCityHall;
        buildingQuickHud.ContextButton.gameObject.SetActive(showContextBtn);
        if (showContextBtn)
            buildingQuickHud.ContextButtonText.text = GetBuildingQuickContextButtonText(selectedBuildingType);
        UpdateBuildingQuickHudSubmenuForSelection(selectedBuildingType, location, showContextBtn);
        bool showStopNumberRow = selectedBuildingType == LocationType.Stop && !isCityHall;
        buildingQuickHud.StopNumberRow.gameObject.SetActive(showStopNumberRow);
        if (showStopNumberRow)
        {
            NormalizeLocalStopNumbers();
            int stopNumber = Mathf.Max(1, location.StopNumber);
            buildingQuickHud.StopNumberLabelText.text = ru ? $"Номер остановки: {stopNumber}" : $"Stop Number: {stopNumber}";
        }

        bool isPersonalHouse = selectedBuildingType == LocationType.PersonalHouse && !isCityHall && !isMotel;
        if (buildingQuickHud.PersonalHouseSection != null)
            buildingQuickHud.PersonalHouseSection.gameObject.SetActive(isPersonalHouse);
        if (isCityHall || isMotel)
        {
            if (buildingQuickHud.WorkerSlotsSection != null)
                buildingQuickHud.WorkerSlotsSection.gameObject.SetActive(false);
            if (buildingQuickHud.PersonalHouseSection != null)
                buildingQuickHud.PersonalHouseSection.gameObject.SetActive(false);
        }
        else if (isPersonalHouse)
        {
            if (buildingQuickHud.WorkerSlotsSection != null)
                buildingQuickHud.WorkerSlotsSection.gameObject.SetActive(false);
            UpdatePersonalHouseResidentsSection();
        }
        else
        {
            UpdateBuildingServiceWorkerSlots(selectedBuildingType, ru);
            if (isWarehouse)
            {
                UpdateWarehouseBuildingWorkerSectionChrome();
            }
        }
        UpdateHudGamblingEffects();

        LocalizeCanvas(buildingQuickHud.CanvasRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(buildingQuickHud.Root);
        UpdateBuildingQuickHudLinkLine(GetBuildingQuickHudLinkTarget(location));
    }

    private static Vector3 GetBuildingQuickHudLinkTarget(LocationData location)
    {
        if (location == null)
        {
            return Vector3.zero;
        }

        const float EdgeInset = 0.08f;
        return new Vector3(location.Min.x + EdgeInset, 0f, location.Min.y + EdgeInset);
    }

    private void CreateBuildingQuickHudLinkLine(Transform canvasTransform)
    {
        GameObject lineObject = CreateUiObject("BuildingQuickHudLinkLine", canvasTransform);
        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0f, 0.5f);
        lineRect.sizeDelta = new Vector2(0f, BuildingQuickHudLinkLineThickness);

        Image lineImage = lineObject.AddComponent<Image>();
        lineImage.color = BuildingQuickHudLinkLineColor;
        lineImage.raycastTarget = false;

        lineObject.SetActive(false);
        buildingQuickHud.LinkLine = lineRect;
        buildingQuickHud.LinkLineImage = lineImage;
    }

    private void UpdateBuildingQuickHudLinkLine(Vector3 targetPosition)
    {
        if (buildingQuickHud?.LinkLine == null ||
            buildingQuickHud.Root == null ||
            buildingQuickHud.CanvasRoot == null ||
            mainCamera == null ||
            !buildingQuickHud.CanvasRoot.activeInHierarchy)
        {
            SetBuildingQuickHudLinkLineVisible(false);
            return;
        }

        Vector3 markerPosition = new(
            targetPosition.x,
            SampleTerrainHeight(targetPosition.x, targetPosition.z) + 0.08f,
            targetPosition.z);
        Vector3 targetScreen3 = mainCamera.WorldToScreenPoint(markerPosition);
        if (targetScreen3.z <= 0f ||
            targetScreen3.x < -32f ||
            targetScreen3.x > Screen.width + 32f ||
            targetScreen3.y < -32f ||
            targetScreen3.y > Screen.height + 32f)
        {
            SetBuildingQuickHudLinkLineVisible(false);
            return;
        }

        Vector2 targetScreen = new(targetScreen3.x, targetScreen3.y);
        Vector2 hudScreenPoint = GetClosestBuildingQuickHudEdgePoint(targetScreen);
        RectTransform canvasRect = buildingQuickHud.CanvasRoot.GetComponent<RectTransform>();
        if (canvasRect == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, hudScreenPoint, null, out Vector2 hudLocalPoint) ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, targetScreen, null, out Vector2 targetLocalPoint))
        {
            SetBuildingQuickHudLinkLineVisible(false);
            return;
        }

        Vector2 delta = targetLocalPoint - hudLocalPoint;
        float length = delta.magnitude;
        if (length < 8f)
        {
            SetBuildingQuickHudLinkLineVisible(false);
            return;
        }

        RectTransform line = buildingQuickHud.LinkLine;
        line.anchoredPosition = hudLocalPoint;
        line.sizeDelta = new Vector2(length, BuildingQuickHudLinkLineThickness);
        line.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        if (buildingQuickHud.LinkLineImage != null)
        {
            buildingQuickHud.LinkLineImage.color = BuildingQuickHudLinkLineColor;
        }

        SetBuildingQuickHudLinkLineVisible(true);
        line.SetAsFirstSibling();
    }

    private Vector2 GetClosestBuildingQuickHudEdgePoint(Vector2 targetScreen)
    {
        buildingQuickHud.Root.GetWorldCorners(buildingQuickHudLinkCorners);
        Vector2 bottomLeft = RectTransformUtility.WorldToScreenPoint(null, buildingQuickHudLinkCorners[0]);
        Vector2 topLeft = RectTransformUtility.WorldToScreenPoint(null, buildingQuickHudLinkCorners[1]);
        Vector2 topRight = RectTransformUtility.WorldToScreenPoint(null, buildingQuickHudLinkCorners[2]);
        Vector2 bottomRight = RectTransformUtility.WorldToScreenPoint(null, buildingQuickHudLinkCorners[3]);

        Vector2 bestPoint = GetClosestPointOnBuildingQuickHudSegment(targetScreen, bottomLeft, topLeft);
        float bestDistance = (targetScreen - bestPoint).sqrMagnitude;
        TryUseCloserBuildingQuickHudSegment(targetScreen, topLeft, topRight, ref bestPoint, ref bestDistance);
        TryUseCloserBuildingQuickHudSegment(targetScreen, topRight, bottomRight, ref bestPoint, ref bestDistance);
        TryUseCloserBuildingQuickHudSegment(targetScreen, bottomRight, bottomLeft, ref bestPoint, ref bestDistance);
        return bestPoint;
    }

    private static void TryUseCloserBuildingQuickHudSegment(
        Vector2 target,
        Vector2 segmentStart,
        Vector2 segmentEnd,
        ref Vector2 bestPoint,
        ref float bestDistance)
    {
        Vector2 candidate = GetClosestPointOnBuildingQuickHudSegment(target, segmentStart, segmentEnd);
        float distance = (target - candidate).sqrMagnitude;
        if (distance < bestDistance)
        {
            bestDistance = distance;
            bestPoint = candidate;
        }
    }

    private static Vector2 GetClosestPointOnBuildingQuickHudSegment(Vector2 point, Vector2 segmentStart, Vector2 segmentEnd)
    {
        Vector2 segment = segmentEnd - segmentStart;
        float lengthSq = segment.sqrMagnitude;
        if (lengthSq <= 0.0001f)
        {
            return segmentStart;
        }

        float t = Mathf.Clamp01(Vector2.Dot(point - segmentStart, segment) / lengthSq);
        return segmentStart + segment * t;
    }

    private void SetBuildingQuickHudLinkLineVisible(bool visible)
    {
        if (buildingQuickHud?.LinkLine != null &&
            buildingQuickHud.LinkLine.gameObject.activeSelf != visible)
        {
            buildingQuickHud.LinkLine.gameObject.SetActive(visible);
        }
    }

    private void ConfigureBuildingQuickHudSummaryLayout(string statusText, string resourceText)
    {
        if (buildingQuickHud == null)
        {
            return;
        }

        int statusLines = Mathf.Clamp(CountHudTextLines(statusText), 1, 3);
        int resourceLines = Mathf.Clamp(CountHudTextLines(resourceText), 1, 8);
        float statusHeight = statusLines * 17f + 6f;
        float resourceHeight = resourceLines * 17f + 6f;

        if (buildingQuickHud.StatusTextLayout != null)
        {
            buildingQuickHud.StatusTextLayout.preferredHeight = statusHeight;
        }

        if (buildingQuickHud.ResourceTextLayout != null)
        {
            buildingQuickHud.ResourceTextLayout.preferredHeight = resourceHeight;
        }

        if (buildingQuickHud.SummaryCardLayout != null)
        {
            const float cardVerticalPadding = 30f;
            const float bodySpacing = 16f;
            const float typeHeight = 24f;
            buildingQuickHud.SummaryCardLayout.preferredHeight =
                cardVerticalPadding + typeHeight + bodySpacing + statusHeight + resourceHeight;
        }
    }

    private static int CountHudTextLines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 1;
        }

        int lines = 1;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                lines++;
            }
        }

        return lines;
    }

    private void OpenContextPanelFromBuildingQuickHud()
    {
        if (!selectedLocation.HasValue)
        {
            return;
        }

        LocationType locationType = selectedLocation.Value;
        isSocialGraphPanelOpen = false;
        isSocialGraphScreenDirty = true;
        isCityHallPanelOpen = false;
        isCityHallScreenDirty = true;
        switch (locationType)
        {
            case LocationType.Parking:
                LogUiInput("Quick HUD: opened Fleet from Parking");
                isFleetPanelOpen = true;
                isDriversPanelOpen = false;
                isShiftsPanelOpen = false;
                isResourcesPanelOpen = false;
                isBuildPanelOpen = false;
                break;
            case LocationType.Motel:
                LogUiInput("Quick HUD: opened Drivers from Motel");
                isDriversPanelOpen = true;
                isFleetPanelOpen = false;
                isShiftsPanelOpen = false;
                isResourcesPanelOpen = false;
                isBuildPanelOpen = false;
                break;
            case LocationType.LaborExchange:
                LogUiInput("Quick HUD: opened Labor Exchange staffing overview");
                isShiftsPanelOpen = true;
                isFleetPanelOpen = false;
                isDriversPanelOpen = false;
                isResourcesPanelOpen = false;
                isEconomyPanelOpen = false;
                isTradePanelOpen = false;
                isBuildPanelOpen = false;
                isStatesPanelOpen = false;
                isWorldMapPanelOpen = false;
                break;
            case LocationType.CityHall:
                LogUiInput("Quick HUD: opened City Hall");
                isCityHallPanelOpen = true;
                isFleetPanelOpen = false;
                isDriversPanelOpen = false;
                isShiftsPanelOpen = false;
                isResourcesPanelOpen = false;
                isEconomyPanelOpen = false;
                isTradePanelOpen = false;
                isBuildPanelOpen = false;
                isWorldMapPanelOpen = false;
                isStatesPanelOpen = false;
                selectedLocation = null;
                selectedLocalStopIndex = -1;
                selectedPersonalHouseIndex = -1;
                break;
            case LocationType.Bar:
                TryGetSelectedBuilding(out LocationData barLocation, out _, out _);
                StartBarInteriorScene(barLocation);
                break;
            case LocationType.Docks:
                LogUiInput("Quick HUD: cycled Dock orders");
                if (locations.TryGetValue(LocationType.Docks, out LocationData docks))
                {
                    CycleDocksOrders(docks);
                }
                break;
            default:
                LogUiInput(locations.TryGetValue(locationType, out LocationData location)
                    ? $"Quick HUD: opened Resources from {location.Label}"
                    : $"Quick HUD: opened Resources from {locationType}");
                isResourcesPanelOpen = true;
                isFleetPanelOpen = false;
                isDriversPanelOpen = false;
                isShiftsPanelOpen = false;
                isBuildPanelOpen = false;
                break;
        }

        isFleetScreenDirty = true;
        isCityHallScreenDirty = true;
        PlayUiSound(uiPanelOpenClip, 0.86f);
    }


}

