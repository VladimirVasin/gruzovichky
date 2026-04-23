using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static readonly string[] SlotSymbols = { "7", "в…", "в™¦", "в™Ґ", "в™Ј", "в™ " };

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

    private sealed class BuildingQuickHudRefs
    {
        public GameObject CanvasRoot;
        public RectTransform Root;
        public Text HeaderText;
        public Text TypeText;
        public Text StatusText;
        public Text ResourceText;
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
        public ServiceWorkerSlotUi[] WorkerSlots;
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
               isBuildPanelOpen ||
               isWorldMapPanelOpen ||
               isStatesPanelOpen ||
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
            RefreshSelectionVisuals();
            PlayUiSound(uiPanelCloseClip, 0.82f);
        });

        RectTransform summaryCard = CreateSectionCard(root, uiFont, string.Empty, out RectTransform summaryBody, false);
        summaryCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 120f;
        buildingQuickHud.TypeText = CreateBodyText("TypeText", summaryBody, uiFont, string.Empty, 17, TextAnchor.MiddleLeft, Color.white);
        buildingQuickHud.TypeText.fontStyle = FontStyle.Bold;
        buildingQuickHud.TypeText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        buildingQuickHud.StatusText = CreateBodyText("StatusText", summaryBody, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        buildingQuickHud.StatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        buildingQuickHud.ResourceText = CreateBodyText("ResourceText", summaryBody, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        buildingQuickHud.ResourceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;

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

        const int MaxServiceSlots = 4;
        buildingQuickHud.WorkerSlots = new ServiceWorkerSlotUi[MaxServiceSlots];
        for (int i = 0; i < MaxServiceSlots; i++)
        {
            ServiceWorkerSlotUi slot = new ServiceWorkerSlotUi();
            buildingQuickHud.WorkerSlots[i] = slot;

            RectTransform slotRoot = CreateUiObject($"WorkerSlot{i}", workerSection).GetComponent<RectTransform>();
            Image slotBg = slotRoot.gameObject.AddComponent<Image>();
            slotBg.color = new Color(0.14f, 0.18f, 0.25f, 1f);
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

        // Flash overlay вЂ” full-panel transparent image rendered on top
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
            (selectedLocation.HasValue || selectedLocalStopIndex >= 0) &&
            !isTruckDetailsOpen &&
            !isFleetPanelOpen &&
            !isDriversPanelOpen &&
            !isShiftsPanelOpen &&
            !isResourcesPanelOpen &&
            !isBuildPanelOpen;

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
        buildingQuickHud.HeaderText.text = location.Label;
        string categoryTag = IsProductionLocation(selectedBuildingType) ? "  [Production]" : "  [Service]";
        buildingQuickHud.TypeText.text = GetSelectedLocationDisplayName(selectedBuildingType) + categoryTag;
        buildingQuickHud.StatusText.text = GetBuildingQuickStatusText(selectedBuildingType);
        buildingQuickHud.ResourceText.text = GetBuildingQuickResourceText(selectedBuildingType);
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

        UpdateBuildingServiceWorkerSlots(selectedBuildingType, ru);
        UpdateHudGamblingEffects();

        LocalizeCanvas(buildingQuickHud.CanvasRoot);
        LayoutRebuilder.ForceRebuildLayoutImmediate(buildingQuickHud.Root);
    }

    private void UpdateBuildingServiceWorkerSlots(LocationType locationType, bool ru)
    {
        if (buildingQuickHud?.WorkerSlots == null) return;

        bool isServiceWithVisitors = locationType == LocationType.Bar ||
                                     locationType == LocationType.Canteen ||
                                     locationType == LocationType.GamblingHall ||
                                     locationType == LocationType.Motel;

        buildingQuickHud.WorkerSlotsSection.gameObject.SetActive(isServiceWithVisitors);
        if (!isServiceWithVisitors) return;

        buildingQuickHud.WorkerSlotsSectionHeader.text = ru ? "Рабочие внутри" : "Workers inside";

        // Collect workers currently inside this building
        List<DriverAgent> inside = new();
        foreach (DriverAgent d in driverAgents)
        {
            bool isInside = locationType == LocationType.Motel
                ? d.RestPhase == DriverRestPhase.Sleeping
                : GetDriverServiceLocation(d.WalkPhase) == locationType;
            if (isInside) inside.Add(d);
        }

        float maxDuration = GetServiceBuildingVisitDuration(locationType);
        string activityLabel = GetServiceBuildingActivityLabel(locationType, ru);

        for (int i = 0; i < buildingQuickHud.WorkerSlots.Length; i++)
        {
            ServiceWorkerSlotUi slot = buildingQuickHud.WorkerSlots[i];
            if (i >= inside.Count)
            {
                slot.Root.gameObject.SetActive(false);
                if (slot.CurrentDriverId != -1)
                {
                    slot.CurrentDriverId = -1;
                    for (int c = slot.PortraitRoot.childCount - 1; c >= 0; c--)
                        Destroy(slot.PortraitRoot.GetChild(c).gameObject);
                }
                continue;
            }

            DriverAgent d = inside[i];
            slot.Root.gameObject.SetActive(true);

            if (slot.CurrentDriverId != d.DriverId)
            {
                slot.CurrentDriverId = d.DriverId;
                DrawWorkerPortraitScaled(d, slot.PortraitRoot, 0.37f);
            }

            slot.NameText.text = d.DriverName;

            if (locationType == LocationType.GamblingHall)
            {
                UpdateGamblingReels(slot, d);

                bool spinDone = slot.SlotPhase == GamblingSlotPhase.Done || slot.SlotPhase == GamblingSlotPhase.ResultPause;
                if (spinDone)
                {
                    string outcomeLabel = d.GamblingMultiplier == 0 ? (ru ? "Проигрыш" : "Loss")
                                        : d.GamblingMultiplier == 1 ? (ru ? "Ставка x1" : "Break even")
                                        : d.GamblingMultiplier == 5 ? (ru ? "Выигрыш x5" : "Win x5")
                                        :                             (ru ? "Джекпот x10" : "Jackpot x10");
                    int net = d.GamblingPayout - d.GamblingBet;
                    string netStr = net >= 0 ? $"+${net}" : $"-${-net}";
                    slot.ActivityText.text  = $"{(ru ? "Ставка" : "Bet")}: ${d.GamblingBet}  {outcomeLabel}\n{(ru ? "Итог" : "Net")}: {netStr}  ->  ${d.GamblingPayout}";
                    slot.ActivityText.color = GetReelResultColor(d.GamblingMultiplier);
                    slot.ActivityTextLayout.preferredHeight = 28f;
                }
                else if (slot.SlotPhase == GamblingSlotPhase.ShowBet && d.GamblingBet > 0)
                {
                    slot.ActivityText.text  = $"{(ru ? "Ставка" : "Bet")}: ${d.GamblingBet}";
                    slot.ActivityText.color = new Color(1f, 0.85f, 0.3f);
                    slot.ActivityTextLayout.preferredHeight = 14f;
                }
                else if (d.GamblingBet > 0)
                {
                    slot.ActivityText.text  = ru ? "Вращение..." : "Spinning...";
                    slot.ActivityText.color = Color.gray;
                    slot.ActivityTextLayout.preferredHeight = 14f;
                }
                else if (d.GamblerBroke)
                {
                    slot.ActivityText.text  = ru ? "На мели" : "Broke";
                    slot.ActivityText.color = new Color(0.6f, 0.4f, 0.4f);
                    slot.ActivityTextLayout.preferredHeight = 14f;
                }
                else
                {
                    slot.ActivityText.text  = activityLabel;
                    slot.ActivityText.color = Color.white;
                    slot.ActivityTextLayout.preferredHeight = 14f;
                }
            }
            else
            {
                slot.ReelRow?.gameObject.SetActive(false);
                slot.ActivityText.text  = activityLabel;
                slot.ActivityText.color = Color.white;
                slot.ActivityTextLayout.preferredHeight = 14f;
            }

            float remaining = locationType == LocationType.Motel ? d.SleepTimer : d.IdleActivityTimer;
            float progress   = Mathf.Clamp01(remaining / Mathf.Max(maxDuration, 0.01f));
            slot.TimeText.text = FormatGameTimeRemaining(remaining);

            slot.ProgressBarFill.anchorMax = new Vector2(progress, 1f);
            slot.ProgressBarFillImage.color = progress > 0.5f
                ? new Color(0.30f, 0.80f, 0.32f)
                : progress > 0.2f
                    ? new Color(0.90f, 0.70f, 0.20f)
                    : new Color(0.85f, 0.30f, 0.22f);
        }
    }

    private void UpdateGamblingReels(ServiceWorkerSlotUi slot, DriverAgent d)
    {
        bool hasBet = d.GamblingBet > 0;
        slot.SlotLayout.preferredHeight = hasBet ? 90f : 54f;

        if (!hasBet)
        {
            slot.ReelRow.gameObject.SetActive(false);
            slot.SlotPhase   = GamblingSlotPhase.Idle;
            slot.LastSpinBet = 0;
            return;
        }

        // Detect new bet (first visit or second bet after result pause)
        if (d.GamblingBet != slot.LastSpinBet)
        {
            slot.LastSpinBet          = d.GamblingBet;
            slot.SlotPhase            = GamblingSlotPhase.ShowBet;
            slot.SpinTimer            = 0f;
            slot.SpinCycleTimer       = 0f;
            slot.ResultDisplayTimer   = 0f;
            slot.ReelStopped          = new bool[3];
            slot.FinalReelChars       = GetReelFinalChars(d.GamblingMultiplier);
            foreach (Text t in slot.ReelTexts) { t.text = "?"; t.color = Color.gray; }
            slot.ReelRow.gameObject.SetActive(false);
        }

        slot.SpinTimer += Time.unscaledDeltaTime;

        switch (slot.SlotPhase)
        {
            case GamblingSlotPhase.ShowBet:
                if (slot.SpinTimer >= 2.5f)
                {
                    slot.SlotPhase = GamblingSlotPhase.Spinning;
                    slot.SpinTimer = 0f;
                    slot.ReelRow.gameObject.SetActive(true);
                }
                break;

            case GamblingSlotPhase.Spinning:
                slot.SpinCycleTimer -= Time.unscaledDeltaTime;
                if (slot.SpinCycleTimer <= 0f)
                {
                    slot.SpinCycleTimer = 0.26f;
                    bool anyStillSpinning = false;
                    for (int r = 0; r < 3; r++)
                    {
                        if (!slot.ReelStopped[r])
                        {
                            slot.ReelTexts[r].text = SlotSymbols[UnityEngine.Random.Range(0, SlotSymbols.Length)];
                            anyStillSpinning = true;
                        }
                    }
                    if (anyStillSpinning) PlayUiSound(slotReelTickClip, 0.45f);
                }

                TryStopReel(slot, 0, 3.5f, d.GamblingMultiplier);
                TryStopReel(slot, 1, 6.0f, d.GamblingMultiplier);
                if (TryStopReel(slot, 2, 9.0f, d.GamblingMultiplier))
                {
                    slot.SlotPhase          = GamblingSlotPhase.Done;
                    slot.ResultDisplayTimer = 3.5f;
                    TriggerHudGamblingResult(d);
                }
                break;

            case GamblingSlotPhase.Done:
                slot.ResultDisplayTimer -= Time.unscaledDeltaTime;
                // Trigger second bet when result has been shown long enough
                if (slot.ResultDisplayTimer <= 0f && d.GamblingBetCount < 2 && d.IdleActivityTimer > 12f && d.Money >= WorkerGamblingMinBet)
                {
                    slot.SlotPhase = GamblingSlotPhase.ResultPause;
                    ResolveWorkerGamblingSpinResult(d);
                    // d.GamblingBet is now a new value; next frame detects it and restarts ShowBet
                }
                break;

            // ResultPause: waiting for next frame to detect the new bet value
        }
    }

    // Returns true if the reel just stopped this frame
    private bool TryStopReel(ServiceWorkerSlotUi slot, int r, float atTime, int multiplier)
    {
        if (!slot.ReelStopped[r] && slot.SpinTimer >= atTime)
        {
            slot.ReelStopped[r]     = true;
            slot.ReelTexts[r].text  = slot.FinalReelChars[r];
            slot.ReelTexts[r].color = GetReelResultColor(multiplier);
            PlayUiSound(uiPanelCloseClip, 0.55f); // reel stop thump
            return true;
        }
        return false;
    }

    private void TriggerHudGamblingResult(DriverAgent d)
    {
        // Apply money now that animation is complete
        if (d.GamblingMoneyPending)
        {
            d.GamblingMoneyPending = false;
            int net = d.GamblingPayout - d.GamblingBet;
            d.Money = Mathf.Max(0, d.Money + net);

            // Update the hall's bank: gains bets, pays out winnings
            if (locations.TryGetValue(LocationType.GamblingHall, out LocationData gh))
                gh.BuildingBank = Mathf.Max(0, gh.BuildingBank - net);

            if (d.DriverObject != null)
            {
                Vector3 pos = d.DriverObject.transform.position;
                if (d.GamblingMultiplier == 0) SpawnMoneySpendPopup(pos, d.GamblingBet - d.GamblingPayout);
                else if (net > 0)              SpawnMoneyEarnPopup(pos, net);
            }
            SessionDebugLogger.Log("NEEDS", $"{d.DriverName} gambling resolved: net={d.GamblingPayout - d.GamblingBet:+#;-#;0}, balance=${d.Money}.");
        }

        if (d.GamblingMultiplier == 0)
        {
            PlayUiSound(slotLoseClip, 0.88f);
            hudFlashColor    = new Color(0.85f, 0.12f, 0.08f);
            hudFlashDuration = hudFlashTimer = 2.2f;
            hudShakeDuration = hudShakeTimer = 0.65f;
        }
        else
        {
            PlayUiSound(slotWinClip, 0.88f);
            hudFlashColor    = d.GamblingMultiplier >= 5 ? new Color(0.05f, 0.82f, 0.18f) : new Color(0.4f, 0.75f, 1f);
            hudFlashDuration = hudFlashTimer = 2.2f;
            hudShakeDuration = 0f;
            hudShakeTimer    = 0f;
        }
    }

    private void UpdateHudGamblingEffects()
    {
        if (buildingQuickHud?.FlashOverlay == null) return;

        if (hudFlashTimer > 0f)
        {
            hudFlashTimer -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(hudFlashTimer / Mathf.Max(hudFlashDuration, 0.01f));
            float alpha = Mathf.SmoothStep(0f, 1f, t) * 0.38f;
            buildingQuickHud.FlashOverlay.color = new Color(hudFlashColor.r, hudFlashColor.g, hudFlashColor.b, alpha);
            buildingQuickHud.FlashOverlay.gameObject.SetActive(true);
        }
        else
        {
            buildingQuickHud.FlashOverlay.gameObject.SetActive(false);
        }

        if (hudShakeTimer > 0f && buildingQuickHud.Root != null)
        {
            hudShakeTimer -= Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(hudShakeTimer / Mathf.Max(hudShakeDuration, 0.01f));
            float shakeX = Mathf.Sin(hudShakeTimer * 42f) * 5f * progress;
            buildingQuickHud.Root.anchoredPosition = buildingQuickHud.OriginalPos + new Vector2(shakeX, 0f);
        }
        else if (buildingQuickHud.Root != null)
        {
            buildingQuickHud.Root.anchoredPosition = buildingQuickHud.OriginalPos;
        }
    }

    private static string[] GetReelFinalChars(int multiplier) => multiplier switch
    {
        10 => new[] { "7", "7", "7" },
        5  => new[] { "в…", "в…", "в…" },
        1  => new[] { "в™¦", "в™¦", "в™¦" },
        _  => new[] { "в™Ґ", "в™ ", "в—Џ" }
    };

    private static Color GetReelResultColor(int multiplier) => multiplier switch
    {
        10 => new Color(1f,    0.85f, 0.1f),
        5  => new Color(0.95f, 0.80f, 0.2f),
        1  => new Color(0.4f,  0.85f, 1f),
        _  => new Color(0.6f,  0.3f,  0.3f)
    };

    private static LocationType? GetDriverServiceLocation(DriverRescuePhase phase)
    {
        return phase switch
        {
            DriverRescuePhase.IdleAtBar          => LocationType.Bar,
            DriverRescuePhase.IdleAtCanteen      => LocationType.Canteen,
            DriverRescuePhase.IdleAtGamblingHall => LocationType.GamblingHall,
            _                                    => (LocationType?)null
        };
    }

    private float GetServiceBuildingVisitDuration(LocationType type)
    {
        return type switch
        {
            LocationType.Bar          => WorkerLeisureDuration,
            LocationType.Canteen      => WorkerCanteenDuration,
            LocationType.GamblingHall => WorkerGamblingHallDuration,
            LocationType.Motel        => DriverSleepDuration,
            _                         => 1f
        };
    }

    private static string GetServiceBuildingActivityLabel(LocationType type, bool ru)
    {
        return type switch
        {
            LocationType.Bar          => ru ? "Пьёт" : "Drinking",
            LocationType.Canteen      => ru ? "Ест" : "Eating",
            LocationType.GamblingHall => ru ? "Играет в автоматы" : "Playing slots",
            LocationType.Motel        => ru ? "Спит" : "Sleeping",
            _                         => ru ? "Внутри" : "Inside"
        };
    }

    private string FormatGameTimeRemaining(float realSeconds)
    {
        float hours = realSeconds * 24f / DayNightCycleDuration;
        int h = Mathf.FloorToInt(hours);
        int m = Mathf.FloorToInt((hours - h) * 60f);
        return h > 0 ? $"{h}h {m}m" : $"{m}m";
    }

    private void OpenContextPanelFromBuildingQuickHud()
    {
        if (!selectedLocation.HasValue)
        {
            return;
        }

        LocationType locationType = selectedLocation.Value;
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
            default:
                LogUiInput($"Quick HUD: opened Resources from {locations[locationType].Label}");
                isResourcesPanelOpen = true;
                isFleetPanelOpen = false;
                isDriversPanelOpen = false;
                isShiftsPanelOpen = false;
                isBuildPanelOpen = false;
                break;
        }

        isFleetScreenDirty = true;
        PlayUiSound(uiPanelOpenClip, 0.86f);
    }

    private string GetBuildingQuickStatusText(LocationType locationType)
    {
        if (IsProductionLocation(locationType) && !IsLocationOperational(locationType))
        {
            return locationType switch
            {
            LocationType.Forest           => "Offline - no workers assigned",
                LocationType.Sawmill          => "Offline - no workers assigned",
                LocationType.FurnitureFactory => "Offline - no workers assigned",
                LocationType.Warehouse        => "Offline - no workers assigned",
                _                             => "Offline"
            };
        }

        return locationType switch
        {
            LocationType.Parking          => "Logistics hub and truck handoff point",
            LocationType.GasStation       => "Fuel service online",
            LocationType.Forest           => "Lumberyard operations",
            LocationType.Sawmill          => "Processing logs into boards",
            LocationType.FurnitureFactory => "Crafting furniture from boards and textile",
            LocationType.Warehouse        => IsLocationOperational(LocationType.Warehouse)
                ? HasActiveTradeRun() &&
                  activeTradeRun.OrderType == TradeOrderType.Buy &&
                  (activeTradeRun.Phase == TradeRunPhase.ReturningToWarehouse || activeTradeRun.Phase == TradeRunPhase.UnloadingAtWarehouse)
                    ? "Receiving imported trade delivery"
                    : "Warehouse operational - resources available"
                : "Finished goods storage",
            LocationType.Motel   => "Drivers rest and idle here",
            LocationType.IntercityStop => "Intercity worker arrival stop by the highway",
            LocationType.Stop    => IsRussianLanguage() ? "Местная автобусная остановка" : "Local worker bus stop",
            LocationType.Canteen      => "Service canteen - visitors pay $10 for meals",
            LocationType.Bar          => "Social hub - idle drivers gather here",
            LocationType.GamblingHall => "Gambling Hall - free leisure for workers.",
            _                         => string.Empty
        };
    }

    private string GetBuildingQuickResourceText(LocationType locationType)
    {
        return locationType switch
        {
            LocationType.Parking => FormatValueLine("Parked Trucks", $"{GetParkingTruckCount()} / {MaxTruckCount}"),
            LocationType.Forest => $"{FormatValueLine("Workers", $"{locations[LocationType.Forest].Workers} / 1")}\n{FormatValueLine("Logs", $"{locations[LocationType.Forest].LogsStored} / {ForestMaxLogsStorage}")}",
            LocationType.Sawmill => $"{FormatValueLine("Workers", $"{locations[LocationType.Sawmill].Workers} / 1")}\n{FormatValueLine("Logs", locations[LocationType.Sawmill].LogsStored.ToString())}\n{FormatValueLine("Boards", locations[LocationType.Sawmill].BoardsStored.ToString())}",
            LocationType.FurnitureFactory => $"{FormatValueLine("Workers", $"{locations[LocationType.FurnitureFactory].Workers} / 1")}\n{FormatValueLine("Boards", $"{locations[LocationType.FurnitureFactory].BoardsStored} / {FurnitureFactoryMaxBoardsStorage}")}\n{FormatValueLine("Textile", $"{locations[LocationType.FurnitureFactory].TextileStored} / {FurnitureFactoryMaxTextileStorage}")}\n{FormatValueLine("Furniture", $"{locations[LocationType.FurnitureFactory].FurnitureStored} / {FurnitureFactoryMaxFurnitureStorage}")}",
            LocationType.Warehouse => GetWarehouseQuickResourceText(),
            LocationType.GasStation => GetGasStationQuickResourceText(),
            LocationType.IntercityStop    => IsRussianLanguage()
                ? FormatValueLine("Статус", "Готова к приёму")
                : FormatValueLine("Status", "Intercity arrivals ready"),
            LocationType.Stop       => IsRussianLanguage()
                ? FormatValueLine("Статус", "Остановка готова")
                : FormatValueLine("Status", "Local route stop ready"),
            LocationType.Motel      => GetServiceBuildingQuickResourceText(locationType),
            LocationType.Bar          => GetBarQuickResourceText(),
            LocationType.Canteen      => GetCanteenQuickResourceText(),
            LocationType.GamblingHall => GetGamblingHallQuickResourceText(),
            _ => string.Empty
        };
    }

    private string GetServiceBuildingQuickResourceText(LocationType locationType)
    {
        if (!locations.TryGetValue(locationType, out LocationData location))
        {
            return string.Empty;
        }

        string text = location.ServiceFee > 0
            ? FormatValueLine("Service Fee", $"${location.ServiceFee}")
            : FormatValueLine("Service Fee", "Free");
        text += "\n" + FormatValueLine("Workers inside", location.Workers.ToString());
        text += "\n" + FormatValueLine("Building Bank", $"${location.BuildingBank}");
        return text;
    }

    private string GetGasStationQuickResourceText()
    {
        locations.TryGetValue(LocationType.GasStation, out LocationData gs);
        int fuel = gs != null ? gs.FuelStored : 0;
        return $"{FormatValueLine("Fuel", $"{fuel} / {GasStationMaxFuelStorage}")}\n" +
               $"{FormatValueLine("Truck Fuel Service", "Ready")}";
    }

    private string GetBarQuickResourceText()
    {
        locations.TryGetValue(LocationType.Bar, out LocationData bar);
        int alcohol = bar != null ? bar.AlcoholStored : 0;
        string text = FormatValueLine("Alcohol", $"{alcohol} / {BarMaxAlcoholStorage}");
        if (bar != null && bar.ServiceFee > 0)
            text += "\n" + FormatValueLine("Service Fee", $"${bar.ServiceFee}");
        text += "\n" + FormatValueLine("Workers inside", bar != null ? bar.Workers.ToString() : "0");
        text += "\n" + FormatValueLine("Building Bank", $"${(bar != null ? bar.BuildingBank : 0)}");
        return text;
    }

    private string GetCanteenQuickResourceText()
    {
        locations.TryGetValue(LocationType.Canteen, out LocationData canteen);
        int food = canteen != null ? canteen.FoodStored : 0;
        string text = FormatValueLine("Food", $"{food} / {CanteenMaxFoodStorage}");
        if (canteen != null && canteen.ServiceFee > 0)
            text += "\n" + FormatValueLine("Service Fee", $"${canteen.ServiceFee}");
        text += "\n" + FormatValueLine("Workers inside", canteen != null ? canteen.Workers.ToString() : "0");
        text += "\n" + FormatValueLine("Building Bank", $"${(canteen != null ? canteen.BuildingBank : 0)}");
        return text;
    }

    private string GetGamblingHallQuickResourceText()
    {
        locations.TryGetValue(LocationType.GamblingHall, out LocationData gh);
        string text = FormatValueLine("Entry", "Free");
        text += "\n" + FormatValueLine("Workers inside", gh != null ? gh.Workers.ToString() : "0");
        text += "\n" + FormatValueLine("Building Bank", $"${(gh != null ? gh.BuildingBank : 0)}");
        return text;
    }

    private string GetWarehouseQuickResourceText()
    {
        locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse);
        int workers = warehouse != null ? warehouse.Workers : 0;
        return FormatValueLine("Workers", $"{workers} / {WarehouseMaxWorkers}");
    }

    private static bool HasBuildingContextAction(LocationType locationType)
    {
        return locationType != LocationType.GamblingHall &&
               locationType != LocationType.IntercityStop &&
               locationType != LocationType.Stop;
    }

    private string GetBuildingQuickContextButtonText(LocationType locationType)
    {
        return locationType switch
        {
            LocationType.Parking => "Open Fleet",
            LocationType.Motel => "Open Drivers",
            _ => "Open Resources"
        };
    }

    private void ShiftSelectedStopNumber(int delta)
    {
        if (selectedLocalStopIndex < 0 || selectedLocalStopIndex >= localStops.Count)
        {
            return;
        }

        NormalizeLocalStopNumbers();
        LocationData location = localStops[selectedLocalStopIndex];
        int stopCount = localStops.Count;
        int currentNumber = Mathf.Clamp(location.StopNumber, 1, Mathf.Max(1, stopCount));
        int targetNumber = Mathf.Clamp(currentNumber + delta, 1, Mathf.Max(1, stopCount));
        if (targetNumber == currentNumber)
        {
            UpdateBuildingQuickHud();
            return;
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            if (i == selectedLocalStopIndex)
            {
                continue;
            }

            if (localStops[i].StopNumber == targetNumber)
            {
                localStops[i].StopNumber = currentNumber;
                break;
            }
        }

        location.StopNumber = targetNumber;
        NormalizeLocalStopNumbers();
        isFleetScreenDirty = true;
        LogUiInput($"Quick HUD: changed stop number for {location.Label} to {location.StopNumber}");
        SessionDebugLogger.Log("BUILD", $"{location.Label} stop number changed to {location.StopNumber}.");
        PlayUiSound(uiSelectClip, 0.72f);
        UpdateBuildingQuickHud();
    }

}


