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
        public Text HeaderText;
        public LayoutElement SummaryCardLayout;
        public Text TypeText;
        public Text StatusText;
        public LayoutElement StatusTextLayout;
        public Text ResourceText;
        public LayoutElement ResourceTextLayout;
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
        public ScrollRect WorkerSlotsScroll;
        public RectTransform WorkerSlotsContent;
        public ServiceWorkerSlotUi[] WorkerSlots;
        public RectTransform PersonalHouseSection;
        public Text PersonalHouseSectionHeader;
        public PersonalHouseResidentRowUi[] ResidentRows;
        public Image      FlashOverlay;
        public Vector2    OriginalPos;
    }

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

        RectTransform headerRow = CreateLayoutRow("BuildingQuickHudHeaderRow", root, 30f, 10f);
        buildingQuickHud.HeaderText = CreateHeaderText("Header", headerRow, uiFont, "Location", 21, TextAnchor.MiddleLeft, Color.white);
        buildingQuickHud.HeaderText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        buildingQuickHud.CloseButton = CreateButton("CloseButton", headerRow, uiFont, out buildingQuickHud.CloseButtonText, "X", 12, new Color(0.26f, 0.30f, 0.36f, 1f), Color.white);
        LayoutElement closeLayout = buildingQuickHud.CloseButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 28f;
        closeLayout.preferredHeight = 28f;
        buildingQuickHud.CloseButton.onClick.AddListener(() =>
        {
            if (TryGetSelectedBuilding(out LocationData selectedBuilding, out _, out _))
            {
                LogUiInput($"Quick HUD: closed {selectedBuilding.Label}");
            }

            selectedLocation = null;
            selectedLocalStopIndex = -1;
            selectedPersonalHouseIndex = -1;
            RefreshSelectionVisuals();
            PlayUiSound(uiPanelCloseClip, 0.82f);
        });

        RectTransform summaryCard = CreateSectionCard(root, uiFont, string.Empty, out RectTransform summaryBody, false);
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

        RectTransform actionRow = CreateLayoutRow("BuildingQuickHudActionRow", root, 34f, 10f);
        buildingQuickHud.ContextButton = CreateButton("ContextButton", actionRow, uiFont, out buildingQuickHud.ContextButtonText, "Open", 13, FleetPrimaryButtonColor, Color.white);
        LayoutElement contextLayout = buildingQuickHud.ContextButton.gameObject.AddComponent<LayoutElement>();
        contextLayout.flexibleWidth = 1f;
        contextLayout.preferredHeight = 34f;
        buildingQuickHud.ContextButton.onClick.AddListener(OpenContextPanelFromBuildingQuickHud);

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
            return;
        }

        if (!TryGetSelectedBuilding(out LocationData location, out LocationType selectedBuildingType, out _))
        {
            buildingQuickHud.CanvasRoot.SetActive(false);
            return;
        }

        bool ru = IsRussianLanguage();
        buildingQuickHud.HeaderText.text = selectedBuildingType == LocationType.Docks
            ? GetSelectedLocationDisplayName(selectedBuildingType)
            : location.Label;
        string categoryTag = IsProductionLocation(selectedBuildingType) ? "  [Production]"
            : selectedBuildingType == LocationType.PersonalHouse ? "  [Housing]"
            : "  [Service]";
        buildingQuickHud.TypeText.text = GetSelectedLocationDisplayName(selectedBuildingType) + categoryTag;
        string quickStatus = GetBuildingQuickStatusText(selectedBuildingType);
        string quickResource = selectedBuildingType == LocationType.PersonalHouse
            ? GetPersonalHouseQuickResourceText()
            : GetBuildingQuickResourceText(selectedBuildingType);
        buildingQuickHud.StatusText.text = quickStatus;
        buildingQuickHud.ResourceText.text = quickResource;
        ConfigureBuildingQuickHudSummaryLayout(quickStatus, quickResource);
        bool showContextBtn = HasBuildingContextAction(selectedBuildingType);
        buildingQuickHud.ContextButton.gameObject.SetActive(showContextBtn);
        if (showContextBtn)
            buildingQuickHud.ContextButtonText.text = GetBuildingQuickContextButtonText(selectedBuildingType);
        bool showStopNumberRow = selectedBuildingType == LocationType.Stop;
        buildingQuickHud.StopNumberRow.gameObject.SetActive(showStopNumberRow);
        if (showStopNumberRow)
        {
            NormalizeLocalStopNumbers();
            int stopNumber = Mathf.Max(1, location.StopNumber);
            buildingQuickHud.StopNumberLabelText.text = ru ? $"Номер остановки: {stopNumber}" : $"Stop Number: {stopNumber}";
        }

        bool isPersonalHouse = selectedBuildingType == LocationType.PersonalHouse;
        if (buildingQuickHud.PersonalHouseSection != null)
            buildingQuickHud.PersonalHouseSection.gameObject.SetActive(isPersonalHouse);
        if (isPersonalHouse)
        {
            if (buildingQuickHud.WorkerSlotsSection != null)
                buildingQuickHud.WorkerSlotsSection.gameObject.SetActive(false);
            UpdatePersonalHouseResidentsSection();
        }
        else
        {
            UpdateBuildingServiceWorkerSlots(selectedBuildingType, ru);
        }
        UpdateHudGamblingEffects();

        LocalizeCanvas(buildingQuickHud.CanvasRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(buildingQuickHud.Root);
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

