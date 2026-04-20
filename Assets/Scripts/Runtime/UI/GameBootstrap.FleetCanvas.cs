using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static readonly Color FleetScreenTint = new(0.06f, 0.08f, 0.11f, 0.76f);
    private static readonly Color FleetPanelColor = new(0.10f, 0.13f, 0.18f, 0.97f);
    private static readonly Color FleetInsetColor = new(0.14f, 0.17f, 0.23f, 0.98f);
    private static readonly Color FleetCardMutedColor = new(0.13f, 0.16f, 0.21f, 0.98f);
    private static readonly Color FleetRowColor = new(0.18f, 0.21f, 0.28f, 0.98f);
    private static readonly Color FleetSelectedRowColor = new(0.29f, 0.25f, 0.13f, 0.98f);
    private static readonly Color FleetAccentColor = new(0.90f, 0.76f, 0.27f, 1f);
    private static readonly Color FleetPrimaryButtonColor = new(0.74f, 0.53f, 0.18f, 1f);
    private static readonly Color FleetSecondaryTextColor = new(0.73f, 0.79f, 0.87f, 1f);
    private static readonly Color FleetMutedTextColor = new(0.56f, 0.63f, 0.74f, 1f);

    private FleetScreenUiRefs fleetScreenUi;

    private sealed class FleetScreenUiRefs
    {
        public GameObject CanvasRoot;
        public GameObject ScreenRoot;
        public RectTransform LeftPanel;
        public RectTransform RightPanel;
        public ScrollRect TruckListScrollRect;
        public RectTransform TruckListContent;
        public readonly List<FleetTruckRowUi> TruckRows = new();
        public RectTransform BuyTruckContainer;
        public Button BuyTruckButton;
        public Text BuyTruckButtonText;
        public Text BuyTruckStatusText;
        public Text FleetCountText;
        public Text DetailsHeaderText;
        public Text DetailsPlaceholderText;
        public GameObject DetailsContentRoot;
        public LayoutElement InfoCardLayout;
        public Text InfoStateText;
        public readonly List<Button> DriverLinkButtons = new();
        public readonly List<Text> DriverLinkButtonTexts = new();
        public readonly List<Button> AssignDriverButtons = new();
        public readonly List<Text> AssignDriverButtonTexts = new();
        public readonly List<Button> RemoveDriverButtons = new();
        public readonly List<Text> RemoveDriverButtonTexts = new();
        public RectTransform AssignDriverPickerPanel;
        public LayoutElement AssignDriverPickerLayout;
        public Text AssignDriverPickerTitleText;
        public Text AssignDriverPickerEmptyText;
        public readonly List<Button> DriverPickerButtons = new();
        public readonly List<Text> DriverPickerButtonTexts = new();
        public Text ResourcesFuelText;
        public Text ResourcesCargoText;
        public Text NavigationCellText;
        public Text NavigationRouteText;
        public Text NavigationPayoutText;
    }

    private sealed class FleetTruckRowUi
    {
        public int TruckNumber;
        public RectTransform Root;
        public Image Background;
        public Image Accent;
        public Outline Outline;
        public Button Button;
        public Text TruckNameText;
        public Text DriverText;
        public Text StateText;
        public Text ResourceText;
    }

    private int fleetAssignDriverTargetSlot = -1;

    private void SetupFleetScreenUi()
    {
        if (fleetScreenUi != null)
        {
            return;
        }

        EnsureFleetEventSystem();

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fleetScreenUi = new FleetScreenUiRefs();

        GameObject canvasObject = new("FleetScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        fleetScreenUi.CanvasRoot = canvasObject;

        GameObject screenRoot = CreateUiObject("FleetScreenRoot", canvasObject.transform);
        RectTransform screenRect = screenRoot.GetComponent<RectTransform>();
        SetCenteredWindow(screenRect, 1180f, 560f, -16f);
        Image screenBackground = screenRoot.AddComponent<Image>();
        screenBackground.color = FleetScreenTint;
        Outline screenOutline = screenRoot.AddComponent<Outline>();
        screenOutline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        screenOutline.effectDistance = new Vector2(2f, -2f);
        HorizontalLayoutGroup rootLayout = screenRoot.AddComponent<HorizontalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 16;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = true;
        fleetScreenUi.ScreenRoot = screenRoot;

        fleetScreenUi.LeftPanel = CreateStyledPanel("FleetListPanel", screenRoot.transform, FleetPanelColor);
        LayoutElement leftPanelLayout = fleetScreenUi.LeftPanel.gameObject.AddComponent<LayoutElement>();
        leftPanelLayout.preferredWidth = 324f;
        leftPanelLayout.flexibleWidth = 0f;
        leftPanelLayout.flexibleHeight = 1f;
        VerticalLayoutGroup leftLayout = fleetScreenUi.LeftPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        leftLayout.padding = new RectOffset(16, 16, 16, 16);
        leftLayout.spacing = 16;
        leftLayout.childControlWidth = true;
        leftLayout.childControlHeight = true;
        leftLayout.childForceExpandWidth = true;
        leftLayout.childForceExpandHeight = false;

        RectTransform leftHeader = CreateLayoutRow("FleetHeaderRow", fleetScreenUi.LeftPanel, 40f, 0f);
        fleetScreenUi.FleetCountText = CreateHeaderText("FleetCount", leftHeader, uiFont, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);
        fleetScreenUi.FleetCountText.transform.SetAsFirstSibling();
        CreateHeaderText("FleetHeader", leftHeader, uiFont, "Fleet", 24, TextAnchor.MiddleLeft, Color.white);

        RectTransform listFrame = CreateStyledPanel("TruckListFrame", fleetScreenUi.LeftPanel, FleetInsetColor);
        LayoutElement listFrameLayout = listFrame.gameObject.AddComponent<LayoutElement>();
        listFrameLayout.flexibleHeight = 1f;
        listFrameLayout.minHeight = 260f;
        SetupFleetScrollView(listFrame);

        RectTransform buyContainer = CreateStyledPanel("BuyTruckButtonContainer", fleetScreenUi.LeftPanel, FleetInsetColor);
        fleetScreenUi.BuyTruckContainer = buyContainer;
        LayoutElement buyLayoutElement = buyContainer.gameObject.AddComponent<LayoutElement>();
        buyLayoutElement.preferredHeight = 92f;
        VerticalLayoutGroup buyLayout = buyContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        buyLayout.padding = new RectOffset(14, 14, 14, 12);
        buyLayout.spacing = 8;
        buyLayout.childControlWidth = true;
        buyLayout.childControlHeight = true;
        buyLayout.childForceExpandWidth = true;
        buyLayout.childForceExpandHeight = false;
        fleetScreenUi.BuyTruckButton = CreateButton("BuyTruckButton", buyContainer, uiFont, out fleetScreenUi.BuyTruckButtonText, "Buy New Truck", 16, FleetPrimaryButtonColor, Color.white);
        fleetScreenUi.BuyTruckButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
        fleetScreenUi.BuyTruckButton.onClick.AddListener(() =>
        {
            LogUiInput("Fleet Canvas: clicked Buy New Truck");
            HireNewTruck();
        });
        fleetScreenUi.BuyTruckStatusText = CreateBodyText("BuyTruckStatus", buyContainer, uiFont, string.Empty, 12, TextAnchor.MiddleCenter, FleetSecondaryTextColor);

        fleetScreenUi.RightPanel = CreateStyledPanel("TruckDetailsPanel", screenRoot.transform, FleetPanelColor);
        LayoutElement rightPanelLayout = fleetScreenUi.RightPanel.gameObject.AddComponent<LayoutElement>();
        rightPanelLayout.flexibleWidth = 1f;
        rightPanelLayout.flexibleHeight = 1f;
        VerticalLayoutGroup rightLayout = fleetScreenUi.RightPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        rightLayout.padding = new RectOffset(18, 18, 18, 18);
        rightLayout.spacing = 18;
        rightLayout.childControlWidth = true;
        rightLayout.childControlHeight = true;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = false;

        RectTransform detailsHeaderRow = CreateLayoutRow("DetailsHeaderRow", fleetScreenUi.RightPanel, 42f, 0f);
        fleetScreenUi.DetailsHeaderText = CreateHeaderText("DetailsHeader", detailsHeaderRow, uiFont, "Truck Details", 24, TextAnchor.MiddleLeft, Color.white);

        RectTransform placeholderCard = CreateSectionCard(fleetScreenUi.RightPanel, uiFont, string.Empty, out RectTransform placeholderBody, false);
        LayoutElement placeholderLayout = placeholderCard.gameObject.AddComponent<LayoutElement>();
        placeholderLayout.preferredHeight = 118f;
        placeholderLayout.flexibleHeight = 1f;
        VerticalLayoutGroup placeholderBodyLayout = placeholderBody.GetComponent<VerticalLayoutGroup>();
        placeholderBodyLayout.childAlignment = TextAnchor.MiddleCenter;
        placeholderBodyLayout.childForceExpandHeight = true;
        fleetScreenUi.DetailsPlaceholderText = CreateBodyText("DetailsPlaceholder", placeholderBody, uiFont, "No truck selected. Select a truck from the fleet list.", 16, TextAnchor.MiddleCenter, FleetSecondaryTextColor);

        fleetScreenUi.DetailsContentRoot = CreateUiObject("DetailsContentRoot", fleetScreenUi.RightPanel).gameObject;
        LayoutElement detailsContentLayout = fleetScreenUi.DetailsContentRoot.AddComponent<LayoutElement>();
        detailsContentLayout.flexibleHeight = 1f;
        VerticalLayoutGroup detailsContentGroup = fleetScreenUi.DetailsContentRoot.AddComponent<VerticalLayoutGroup>();
        detailsContentGroup.spacing = 18;
        detailsContentGroup.childControlWidth = true;
        detailsContentGroup.childControlHeight = true;
        detailsContentGroup.childForceExpandWidth = true;
        detailsContentGroup.childForceExpandHeight = false;

        RectTransform infoCard = CreateSectionCard(fleetScreenUi.DetailsContentRoot.transform, uiFont, "Truck Overview", out RectTransform infoBody);
        fleetScreenUi.InfoCardLayout = infoCard.gameObject.AddComponent<LayoutElement>();
        fleetScreenUi.InfoCardLayout.preferredHeight = 232f;
        RectTransform stateBlock = CreateStyledPanel("StateBlock", infoBody, FleetCardMutedColor);
        stateBlock.gameObject.AddComponent<LayoutElement>().preferredHeight = 66f;
        VerticalLayoutGroup stateBlockGroup = stateBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        stateBlockGroup.padding = new RectOffset(12, 12, 10, 10);
        stateBlockGroup.spacing = 4;
        stateBlockGroup.childControlWidth = true;
        stateBlockGroup.childControlHeight = true;
        stateBlockGroup.childForceExpandWidth = true;
        stateBlockGroup.childForceExpandHeight = false;
        CreateHeaderText("StateLabel", stateBlock, uiFont, "Current State", 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        fleetScreenUi.InfoStateText = CreateBodyText("InfoState", stateBlock, uiFont, string.Empty, 18, TextAnchor.MiddleLeft, Color.white);
        fleetScreenUi.InfoStateText.fontStyle = FontStyle.Bold;
        fleetScreenUi.InfoStateText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        RectTransform driverBlock = CreateStyledPanel("AssignedDriverBlock", infoBody, FleetCardMutedColor);
        driverBlock.gameObject.AddComponent<LayoutElement>().preferredHeight = 120f;
        VerticalLayoutGroup driverBlockGroup = driverBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        driverBlockGroup.padding = new RectOffset(12, 12, 10, 10);
        driverBlockGroup.spacing = 6;
        driverBlockGroup.childControlWidth = true;
        driverBlockGroup.childControlHeight = true;
        driverBlockGroup.childForceExpandWidth = true;
        driverBlockGroup.childForceExpandHeight = false;
        for (int slotIndex = 0; slotIndex < 2; slotIndex++)
        {
            CreateHeaderText($"AssignedDriverLabel{slotIndex + 1}", driverBlock, uiFont, $"Assigned Driver {slotIndex + 1}", 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
            RectTransform driverRow = CreateLayoutRow($"AssignedDriverRow{slotIndex + 1}", driverBlock, 28f, 10f);

            Button driverButton = CreateButton($"DriverLinkButton{slotIndex + 1}", driverRow, uiFont, out Text driverButtonText, "Empty", 13, new Color(0.23f, 0.29f, 0.36f, 1f), Color.white);
            LayoutElement driverLinkLayout = driverButton.gameObject.AddComponent<LayoutElement>();
            driverLinkLayout.preferredHeight = 28f;
            driverLinkLayout.flexibleWidth = 1f;
            driverButtonText.alignment = TextAnchor.MiddleLeft;
            int driverSlotIndex = slotIndex;
            driverButton.onClick.AddListener(() =>
            {
                TruckAgent selectedTruck = GetFleetSelectedTruck();
                if (selectedTruck == null || driverSlotIndex >= selectedTruck.AssignedDrivers.Count)
                {
                    return;
                }

                DriverAgent rosterDriver = selectedTruck.AssignedDrivers[driverSlotIndex];
                LogUiInput($"Fleet Canvas: opened Drivers for {rosterDriver.DriverName} from {selectedTruck.DisplayName}");
                OpenDriversPanelForDriver(rosterDriver.DriverId);
            });
            fleetScreenUi.DriverLinkButtons.Add(driverButton);
            fleetScreenUi.DriverLinkButtonTexts.Add(driverButtonText);

            Button removeButton = CreateButton($"RemoveDriverButton{slotIndex + 1}", driverRow, uiFont, out Text removeButtonText, "X", 12, new Color(0.66f, 0.20f, 0.20f, 1f), Color.white);
            LayoutElement removeDriverLayout = removeButton.gameObject.AddComponent<LayoutElement>();
            removeDriverLayout.preferredWidth = 28f;
            removeDriverLayout.preferredHeight = 28f;
            int removeSlotIndex = slotIndex;
            removeButton.onClick.AddListener(() =>
            {
                TruckAgent currentTruck = GetFleetSelectedTruck();
                if (currentTruck == null || removeSlotIndex >= currentTruck.AssignedDrivers.Count)
                {
                    return;
                }

                UnassignDriverFromTruck(currentTruck, currentTruck.AssignedDrivers[removeSlotIndex]);
            });
            fleetScreenUi.RemoveDriverButtons.Add(removeButton);
            fleetScreenUi.RemoveDriverButtonTexts.Add(removeButtonText);

            Button assignButton = CreateButton($"AssignDriverButton{slotIndex + 1}", driverRow, uiFont, out Text assignButtonText, "Assign", 12, new Color(0.39f, 0.44f, 0.56f, 1f), Color.white);
            LayoutElement assignDriverLayout = assignButton.gameObject.AddComponent<LayoutElement>();
            assignDriverLayout.preferredWidth = 100f;
            assignDriverLayout.preferredHeight = 28f;
            int assignSlotIndex = slotIndex;
            assignButton.onClick.AddListener(() =>
            {
                TruckAgent selectedTruck = GetFleetSelectedTruck();
                if (selectedTruck != null)
                {
                    LogUiInput($"Fleet Canvas: clicked assign driver slot {assignSlotIndex + 1} for {selectedTruck.DisplayName}");
                }

                fleetAssignDriverTargetSlot = assignSlotIndex;
                if (isTutorialOpen && activeTutorialTrigger == TutorialTrigger.FleetAssignDriver && assignSlotIndex == 0)
                {
                    CompleteFleetAssignDriverTutorial();
                    return;
                }

                ToggleFleetDriverAssignmentPicker();
            });
            fleetScreenUi.AssignDriverButtons.Add(assignButton);
            fleetScreenUi.AssignDriverButtonTexts.Add(assignButtonText);
        }

        fleetScreenUi.AssignDriverPickerPanel = CreateStyledPanel("AssignDriverPickerPanel", infoBody, FleetCardMutedColor);
        fleetScreenUi.AssignDriverPickerLayout = fleetScreenUi.AssignDriverPickerPanel.gameObject.AddComponent<LayoutElement>();
        fleetScreenUi.AssignDriverPickerLayout.preferredHeight = 0f;
        VerticalLayoutGroup pickerGroup = fleetScreenUi.AssignDriverPickerPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        pickerGroup.padding = new RectOffset(12, 12, 12, 12);
        pickerGroup.spacing = 6;
        pickerGroup.childControlWidth = true;
        pickerGroup.childControlHeight = true;
        pickerGroup.childForceExpandWidth = true;
        pickerGroup.childForceExpandHeight = false;
        fleetScreenUi.AssignDriverPickerTitleText = CreateHeaderText("AssignDriverPickerTitle", fleetScreenUi.AssignDriverPickerPanel, uiFont, "Select Driver", 13, TextAnchor.MiddleLeft, Color.white);
        fleetScreenUi.AssignDriverPickerEmptyText = CreateBodyText("AssignDriverPickerEmpty", fleetScreenUi.AssignDriverPickerPanel, uiFont, "No available drivers.", 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        fleetScreenUi.AssignDriverPickerEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
        for (int i = 0; i < 3; i++)
        {
            Button pickerButton = CreateButton($"AssignDriverOption{i + 1}", fleetScreenUi.AssignDriverPickerPanel, uiFont, out Text pickerText, string.Empty, 12, new Color(0.25f, 0.31f, 0.39f, 1f), Color.white);
            pickerButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;
            pickerText.alignment = TextAnchor.MiddleLeft;
            int driverOptionIndex = i;
            pickerButton.onClick.AddListener(() => OnFleetDriverOptionPressed(driverOptionIndex));
            fleetScreenUi.DriverPickerButtons.Add(pickerButton);
            fleetScreenUi.DriverPickerButtonTexts.Add(pickerText);
        }
        fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(false);

        RectTransform lowerRow = CreateUiObject("DetailsLowerRow", fleetScreenUi.DetailsContentRoot.transform).GetComponent<RectTransform>();
        HorizontalLayoutGroup lowerRowLayout = lowerRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        lowerRowLayout.spacing = 14;
        lowerRowLayout.childControlWidth = true;
        lowerRowLayout.childControlHeight = true;
        lowerRowLayout.childForceExpandWidth = true;
        lowerRowLayout.childForceExpandHeight = true;
        lowerRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 164f;

        RectTransform resourcesCard = CreateSectionCard(lowerRow, uiFont, "Resources", out RectTransform resourcesBody);
        resourcesCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        fleetScreenUi.ResourcesFuelText = CreateValueText("ResourcesFuel", resourcesBody, uiFont);
        fleetScreenUi.ResourcesCargoText = CreateValueText("ResourcesCargo", resourcesBody, uiFont);

        RectTransform navigationCard = CreateSectionCard(lowerRow, uiFont, "Navigation", out RectTransform navigationBody);
        navigationCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        fleetScreenUi.NavigationCellText = CreateValueText("NavigationCell", navigationBody, uiFont);
        fleetScreenUi.NavigationRouteText = CreateValueText("NavigationRoute", navigationBody, uiFont);
        fleetScreenUi.NavigationPayoutText = CreateValueText("NavigationPayout", navigationBody, uiFont);

        AddOverlayCloseButton(screenRect, uiFont);
        fleetScreenUi.CanvasRoot.SetActive(false);
        UpdateFleetScreenUi();
    }

    private void EnsureFleetEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystemObject.GetComponent<EventSystem>().sendNavigationEvents = true;
    }

    private void UpdateFleetScreenUi()
    {
        if (fleetScreenUi == null)
        {
            return;
        }

        bool shouldShow = isFleetPanelOpen;
        if (fleetScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            fleetScreenUi.CanvasRoot.SetActive(shouldShow);
            isFleetScreenDirty = true;
        }

        if (!shouldShow)
        {
            return;
        }

        ApplyFleetTutorialVisibility();
        if (!isFleetScreenDirty && shouldShow)
        {
            return;
        }

        EnsureFleetTruckRows();
        UpdateFleetListPanel();
        UpdateFleetDetailsPanel();
        LocalizeCanvas(fleetScreenUi.CanvasRoot);
        isFleetScreenDirty = false;
    }

    private void ApplyFleetTutorialVisibility()
    {
        if (fleetScreenUi?.BuyTruckContainer == null)
        {
            return;
        }

        bool hideBuyPanelForTutorial = isTutorialOpen && activeTutorialTrigger == TutorialTrigger.FleetSelectTruck;
        if (fleetScreenUi.BuyTruckContainer.gameObject.activeSelf == !hideBuyPanelForTutorial)
        {
            return;
        }

        fleetScreenUi.BuyTruckContainer.gameObject.SetActive(!hideBuyPanelForTutorial);
        if (fleetScreenUi.LeftPanel != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(fleetScreenUi.LeftPanel);
        }
    }

    private void EnsureFleetTruckRows()
    {
        while (fleetScreenUi.TruckRows.Count < truckAgents.Count)
        {
            fleetScreenUi.TruckRows.Add(CreateFleetTruckRow(fleetScreenUi.TruckListContent, fleetScreenUi.TruckRows.Count));
        }

        for (int i = 0; i < fleetScreenUi.TruckRows.Count; i++)
        {
            fleetScreenUi.TruckRows[i].Root.gameObject.SetActive(i < truckAgents.Count);
        }
    }

    private void UpdateFleetListPanel()
    {
        fleetScreenUi.FleetCountText.text = $"{GetOwnedTruckCount()} / {MaxTruckCount} Trucks";

        for (int i = 0; i < truckAgents.Count; i++)
        {
            TruckAgent truckAgent = truckAgents[i];
            FleetTruckRowUi row = fleetScreenUi.TruckRows[i];
            LoadTruckState(truckAgent);
            bool isSelected = isTruckDetailsOpen && selectedTruckNumber == truckAgent.TruckNumber;
            row.TruckNumber = truckAgent.TruckNumber;
            row.TruckNameText.text = truckAgent.DisplayName;
        row.DriverText.text = $"Assigned: {GetTruckAssignedDriverSummary(truckAgent)}";
            row.StateText.text = GetTruckListStatusForFleet(truckAgent);
            row.ResourceText.text = $"{L("Fuel")} {Mathf.CeilToInt(truckAgent.TruckFuel)} / {Mathf.CeilToInt(TruckFuelCapacity)}    {GetTruckCargoSummary(truckAgent)}";
            row.Background.color = isSelected ? FleetSelectedRowColor : FleetRowColor;
            row.Accent.enabled = isSelected;
            row.Outline.effectColor = isSelected ? new Color(FleetAccentColor.r, FleetAccentColor.g, FleetAccentColor.b, 0.42f) : new Color(0f, 0f, 0f, 0.26f);
            row.TruckNameText.color = Color.white;
            row.DriverText.color = isSelected ? new Color(1f, 0.96f, 0.80f) : FleetSecondaryTextColor;
            row.StateText.color = isSelected ? new Color(1f, 0.93f, 0.72f) : new Color(0.83f, 0.88f, 0.95f);
            row.ResourceText.color = isSelected ? new Color(0.99f, 0.94f, 0.76f) : FleetMutedTextColor;
            SaveTruckState(truckAgent);
        }

        bool canHireTruck = GetOwnedTruckCount() < MaxTruckCount && money >= HireTruckCost;
        bool hideBuyPanelForTutorial = isTutorialOpen && activeTutorialTrigger == TutorialTrigger.FleetSelectTruck;
        ApplyFleetTutorialVisibility();

        if (hideBuyPanelForTutorial)
        {
            return;
        }

        fleetScreenUi.BuyTruckButton.interactable = canHireTruck;
        fleetScreenUi.BuyTruckButtonText.text = $"{L("Buy New Truck")} — ${HireTruckCost}";
        fleetScreenUi.BuyTruckStatusText.text = GetFleetBuyStatusLabel();
        fleetScreenUi.BuyTruckStatusText.color = canHireTruck ? FleetSecondaryTextColor : new Color(0.96f, 0.72f, 0.42f, 1f);
    }

    private void UpdateFleetDetailsPanel()
    {
        TruckAgent selectedTruck = GetFleetSelectedTruck();
        bool hasSelection = selectedTruck != null && isTruckDetailsOpen;
        fleetScreenUi.DetailsPlaceholderText.transform.parent.parent.gameObject.SetActive(!hasSelection);
        fleetScreenUi.DetailsContentRoot.SetActive(hasSelection);

        if (!hasSelection)
        {
            fleetScreenUi.DetailsHeaderText.text = "Truck Details";
            return;
        }

        LoadTruckState(selectedTruck);
        DriverAgent driver = selectedTruck.Driver;
        fleetScreenUi.DetailsHeaderText.text = selectedTruck.DisplayName;
        fleetScreenUi.InfoStateText.text = GetTruckFleetStatusLabel();
        for (int slotIndex = 0; slotIndex < fleetScreenUi.DriverLinkButtons.Count; slotIndex++)
        {
            bool hasRosterDriver = slotIndex < selectedTruck.AssignedDrivers.Count;
            DriverAgent rosterDriver = hasRosterDriver ? selectedTruck.AssignedDrivers[slotIndex] : null;
            bool isCurrentDriver = rosterDriver != null && rosterDriver == driver;
            fleetScreenUi.DriverLinkButtonTexts[slotIndex].text = rosterDriver == null
                ? "Empty"
                : (isCurrentDriver ? $"{rosterDriver.DriverName}  (Current)" : rosterDriver.DriverName);
            fleetScreenUi.DriverLinkButtons[slotIndex].interactable = rosterDriver != null;
            fleetScreenUi.AssignDriverButtonTexts[slotIndex].text = rosterDriver == null ? "Assign" : "Assigned";
            fleetScreenUi.AssignDriverButtons[slotIndex].interactable = rosterDriver == null;
            fleetScreenUi.RemoveDriverButtons[slotIndex].gameObject.SetActive(rosterDriver != null);
            fleetScreenUi.RemoveDriverButtons[slotIndex].interactable = CanUnassignDriverFromTruck(selectedTruck, rosterDriver);
        }
        fleetScreenUi.ResourcesFuelText.text = FormatValueLine("Fuel", $"{Mathf.CeilToInt(truckFuel)} / {Mathf.CeilToInt(TruckFuelCapacity)}");
        fleetScreenUi.ResourcesCargoText.text = FormatValueLine("Cargo", FormatTruckCargoValue(truckCargoAmount, truckCargoType));
        fleetScreenUi.NavigationCellText.text = FormatValueLine("Grid Cell", $"{truckCell.x}, {truckCell.y}");
        fleetScreenUi.NavigationRouteText.text = FormatValueLine("Assigned Route", GetTripTitle(currentAssignedTrip));
        fleetScreenUi.NavigationPayoutText.text = FormatValueLine("Trip Payout", $"${currentAssignedTripReward}");
        UpdateFleetDriverAssignmentPicker(selectedTruck);

        SaveTruckState(selectedTruck);
    }

    private void OnFleetRouteButtonPressed(int tripIndex)
    {
        TruckAgent selectedTruck = GetFleetSelectedTruck();
        if (selectedTruck == null)
        {
            return;
        }

        List<TripOption> trips = GetAvailableTrips();
        if (tripIndex < 0 || tripIndex >= trips.Count)
        {
            return;
        }

        AssignTripToTruck(selectedTruck, trips[tripIndex]);
        isFleetScreenDirty = true;
    }

    private TruckAgent GetFleetSelectedTruck()
    {
        if (!isTruckDetailsOpen)
        {
            return null;
        }

        return GetTruckAgent(selectedTruckNumber);
    }

    private string GetTruckListStatusForFleet(TruckAgent truckAgent)
    {
        return GetTruckFleetStatusLabel();
    }

    private string GetTruckCargoSummary(TruckAgent truckAgent)
    {
        if (truckAgent.TruckCargoAmount <= 0 || truckAgent.TruckCargoType == CargoType.None)
        {
            return $"{L("Cargo")} {L("Empty")}";
        }

        return $"{L("Cargo")} {FormatTruckCargoValue(truckAgent.TruckCargoAmount, truckAgent.TruckCargoType)}";
    }

    private string GetFleetBuyStatusLabel()
    {
        if (GetOwnedTruckCount() >= MaxTruckCount)
        {
            return L("Fleet capacity reached.");
        }

        if (money < HireTruckCost)
        {
            return $"{L("Need")} ${HireTruckCost} {L("to hire a new truck.")}";
        }

        return L("Adds a new truck directly to parking.");
    }

    private string GetTruckAssignedDriverName(TruckAgent truckAgent)
    {
        return GetTruckAssignedDriverSummary(truckAgent);
    }

    private void ToggleFleetDriverAssignmentPicker()
    {
        if (fleetScreenUi?.AssignDriverPickerPanel == null)
        {
            return;
        }

        bool nextState = !fleetScreenUi.AssignDriverPickerPanel.gameObject.activeSelf;
        if (nextState && fleetAssignDriverTargetSlot < 0)
        {
            return;
        }

        fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(nextState);
        if (fleetScreenUi.AssignDriverPickerLayout != null)
        {
            fleetScreenUi.AssignDriverPickerLayout.preferredHeight = nextState ? 128f : 0f;
        }

        if (fleetScreenUi.InfoCardLayout != null)
        {
            fleetScreenUi.InfoCardLayout.preferredHeight = nextState ? 360f : 232f;
        }

        TruckAgent selectedTruck = GetFleetSelectedTruck();
        if (selectedTruck != null)
        {
            LogUiInput($"Fleet Canvas: {(nextState ? "opened" : "closed")} driver picker for {selectedTruck.DisplayName}");
        }

        if (!nextState)
        {
            fleetAssignDriverTargetSlot = -1;
        }

        isFleetScreenDirty = true;
        UpdateFleetDriverAssignmentPicker(selectedTruck);
        PlayUiSound(nextState ? uiPanelOpenClip : uiPanelCloseClip, 0.78f);
    }

    private void UpdateFleetDriverAssignmentPicker(TruckAgent selectedTruck)
    {
        if (fleetScreenUi?.AssignDriverPickerPanel == null)
        {
            return;
        }

        if (selectedTruck == null)
        {
            fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(false);
            if (fleetScreenUi.AssignDriverPickerLayout != null) fleetScreenUi.AssignDriverPickerLayout.preferredHeight = 0f;
            if (fleetScreenUi.InfoCardLayout != null) fleetScreenUi.InfoCardLayout.preferredHeight = 232f;
            fleetAssignDriverTargetSlot = -1;
            return;
        }

        if (fleetAssignDriverTargetSlot < 0 || fleetAssignDriverTargetSlot > 1 || fleetAssignDriverTargetSlot < selectedTruck.AssignedDrivers.Count)
        {
            fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(false);
            if (fleetScreenUi.AssignDriverPickerLayout != null) fleetScreenUi.AssignDriverPickerLayout.preferredHeight = 0f;
            if (fleetScreenUi.InfoCardLayout != null) fleetScreenUi.InfoCardLayout.preferredHeight = 232f;
            fleetAssignDriverTargetSlot = -1;
            return;
        }

        List<DriverAgent> candidates = GetDriverAssignmentCandidates(selectedTruck);
        bool hasCandidates = candidates.Count > 0;
        fleetScreenUi.AssignDriverPickerTitleText.text = $"Select Driver for Slot {fleetAssignDriverTargetSlot + 1}";
        fleetScreenUi.AssignDriverPickerEmptyText.gameObject.SetActive(!hasCandidates);
        for (int i = 0; i < fleetScreenUi.DriverPickerButtons.Count; i++)
        {
            bool active = i < candidates.Count;
            fleetScreenUi.DriverPickerButtons[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            DriverAgent driver = candidates[i];
            TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
            fleetScreenUi.DriverPickerButtonTexts[i].text = assignedTruck == null
                ? driver.DriverName
                : $"{driver.DriverName}  ({assignedTruck.DisplayName})";
        }

        fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(fleetScreenUi.AssignDriverPickerPanel.gameObject.activeSelf);
        if (fleetScreenUi.AssignDriverPickerLayout != null)
        {
            fleetScreenUi.AssignDriverPickerLayout.preferredHeight = fleetScreenUi.AssignDriverPickerPanel.gameObject.activeSelf ? (hasCandidates ? 128f : 72f) : 0f;
        }

        if (fleetScreenUi.InfoCardLayout != null)
        {
            fleetScreenUi.InfoCardLayout.preferredHeight = fleetScreenUi.AssignDriverPickerPanel.gameObject.activeSelf ? (hasCandidates ? 360f : 304f) : 232f;
        }
    }

    private List<DriverAgent> GetDriverAssignmentCandidates(TruckAgent selectedTruck)
    {
        List<DriverAgent> candidates = new();
        if (selectedTruck == null || selectedTruck.AssignedDrivers.Count >= 2)
        {
            return candidates;
        }

        foreach (DriverAgent driver in driverAgents)
        {
            if (!CanAssignDriverToTruckRoster(selectedTruck, driver))
            {
                continue;
            }

            candidates.Add(driver);
        }

        return candidates;
    }

    private bool CanAssignDriverToTruckRoster(TruckAgent targetTruck, DriverAgent driver)
    {
        if (targetTruck == null || driver == null)
        {
            return false;
        }

        if (targetTruck.AssignedDrivers.Count >= 2)
        {
            return false;
        }

        if (driver.DutyMode == DriverDutyMode.Logistics || driver.AssignedBuildingType.HasValue)
        {
            return false;
        }

        if (driver.AssignedTruckNumber > 0)
        {
            return false;
        }

        if (driver.IsArrivingByBus || IsDriverOnActiveTradeRun(driver))
        {
            return false;
        }

        if (driver.RestPhase != DriverRestPhase.None || IsDriverBusyWalkPhase(driver))
        {
            return false;
        }

        return driver.DutyMode == DriverDutyMode.Local || driver.DutyMode == DriverDutyMode.Intercity;
    }

    private void OnFleetDriverOptionPressed(int optionIndex)
    {
        TruckAgent selectedTruck = GetFleetSelectedTruck();
        if (selectedTruck == null)
        {
            return;
        }

        List<DriverAgent> candidates = GetDriverAssignmentCandidates(selectedTruck);
        if (optionIndex < 0 || optionIndex >= candidates.Count)
        {
            return;
        }

        if (AssignDriverToTruck(selectedTruck, candidates[optionIndex]))
        {
            LogUiInput($"Fleet Canvas: picked {candidates[optionIndex].DriverName} for {selectedTruck.DisplayName}");
            fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(false);
            if (fleetScreenUi.AssignDriverPickerLayout != null) fleetScreenUi.AssignDriverPickerLayout.preferredHeight = 0f;
            if (fleetScreenUi.InfoCardLayout != null) fleetScreenUi.InfoCardLayout.preferredHeight = 232f;
            fleetAssignDriverTargetSlot = -1;
            isFleetScreenDirty = true;
            CompleteFleetPickDriverTutorial();
        }
    }

    private bool AssignDriverToTruck(TruckAgent targetTruck, DriverAgent driver)
    {
        LogCommand($"AssignDriver({targetTruck?.DisplayName ?? "null"}, {driver?.DriverName ?? "null"})");
        if (targetTruck == null || driver == null)
        {
            if (targetTruck != null)
            {
                LogTruckReaction(targetTruck, "driver assignment rejected: invalid assignment target");
            }
            return false;
        }

        if (driver.AssignedTruckNumber > 0 && driver.AssignedTruckNumber != targetTruck.TruckNumber)
        {
            LogTruckReaction(targetTruck, $"driver assignment rejected for {driver.DriverName}: already assigned to another truck");
            return false;
        }

        if (!CanAssignDriverToTruckRoster(targetTruck, driver))
        {
            LogTruckReaction(targetTruck, $"driver assignment rejected for {driver.DriverName}: driver is not free");
            return false;
        }

        if (!AssignDriverToTruckRoster(targetTruck, driver))
        {
            LogTruckReaction(targetTruck, $"driver assignment rejected for {driver.DriverName}");
            return false;
        }

        SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} added to {targetTruck.DisplayName} roster.");
        LogDriverReaction(driver, $"assigned to {targetTruck.DisplayName}");
        LogTruckReaction(targetTruck, $"added roster driver {driver.DriverName}");
        PlayUiSound(uiSelectClip, 0.88f);
        return true;
    }

    private bool CanUnassignDriverFromTruck(TruckAgent targetTruck, DriverAgent driver)
    {
        if (targetTruck == null || driver == null)
        {
            return false;
        }

        if (targetTruck.Driver != driver)
        {
            return true;
        }

        return !targetTruck.IsTruckMoving &&
               !targetTruck.IsTruckInteracting &&
               !targetTruck.IsDriverRescueActive &&
               targetTruck.CurrentAssignedTrip == TripType.None &&
               targetTruck.CurrentRefuelPhase == RefuelPhase.None &&
               IsTruckInsideParking(targetTruck);
    }

    private bool UnassignDriverFromTruck(TruckAgent targetTruck, DriverAgent driver)
    {
        LogCommand($"UnassignDriver({targetTruck?.DisplayName ?? "null"}, {driver?.DriverName ?? "null"})");
        if (!CanUnassignDriverFromTruck(targetTruck, driver))
        {
            if (targetTruck != null && driver != null)
            {
                LogTruckReaction(targetTruck, $"driver unassign rejected for {driver.DriverName}: truck is currently using that driver");
            }
            return false;
        }

        if (!RemoveDriverFromTruckRoster(targetTruck, driver))
        {
            if (targetTruck != null && driver != null)
            {
                LogTruckReaction(targetTruck, $"driver unassign rejected for {driver.DriverName}");
            }
            return false;
        }

        if (targetTruck.Driver == driver)
        {
            targetTruck.Driver = null;
            driver.IsOnActiveShift = false;
            driver.WaitingForShiftAtParking = false;
            driver.NeedsShiftEndReturn = false;
            driver.RestPhase = DriverRestPhase.None;
            driver.WalkPhase = DriverRescuePhase.None;
            driver.WalkPath.Clear();
            driver.WalkWaypointIndex = 0;
            driver.WalkAnimationTime = 0f;
            if (driver.DriverObject != null)
            {
                driver.DriverObject.SetActive(true);
                driver.DriverObject.transform.position = driver.MotelIdlePosition;
                driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            }
        }

        fleetAssignDriverTargetSlot = -1;
        if (fleetScreenUi?.AssignDriverPickerPanel != null)
        {
            fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(false);
        }

        SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} removed from {targetTruck.DisplayName} roster.");
        LogDriverReaction(driver, $"unassigned from {targetTruck.DisplayName} and returned to free pool");
        LogTruckReaction(targetTruck, $"removed roster driver {driver.DriverName}");
        isFleetScreenDirty = true;
        PlayUiSound(uiSelectClip, 0.86f);
        return true;
    }

    private void OpenDriversPanelForDriver(int driverId)
    {
        isFleetPanelOpen = false;
        isDriversPanelOpen = true;
        isShiftsPanelOpen = false;
        isResourcesPanelOpen = false;
        isBuildPanelOpen = false;
        selectedShiftDriverId = driverId;
        PlayUiSound(uiPanelOpenClip, 0.86f);
    }

    private void SetupFleetScrollView(RectTransform parent)
    {
        GameObject scrollObject = CreateUiObject("TruckListScrollView", parent);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        StretchRect(scrollRect, 0f, 0f, 0f, 0f);
        Image scrollImage = scrollObject.AddComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0f);
        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.scrollSensitivity = 28f;
        fleetScreenUi.TruckListScrollRect = scroll;

        GameObject viewportObject = CreateUiObject("Viewport", scrollObject.transform);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        StretchRect(viewportRect, 8f, 8f, 8f, 8f);
        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.04f);
        viewportImage.raycastTarget = true;
        viewportObject.AddComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = CreateUiObject("Content", viewportObject.transform);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 10f;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        contentObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        fleetScreenUi.TruckListContent = contentRect;
    }

    private FleetTruckRowUi CreateFleetTruckRow(RectTransform parent, int index)
    {
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        FleetTruckRowUi row = new();
        GameObject rowObject = CreateUiObject($"TruckRow{index + 1}", parent);
        row.Root = rowObject.GetComponent<RectTransform>();
        row.Root.sizeDelta = new Vector2(0f, 92f);
        rowObject.AddComponent<LayoutElement>().preferredHeight = 92f;
        row.Background = rowObject.AddComponent<Image>();
        row.Background.color = FleetRowColor;
        row.Outline = rowObject.AddComponent<Outline>();
        row.Outline.effectColor = new Color(0f, 0f, 0f, 0.26f);
        row.Outline.effectDistance = new Vector2(1f, -1f);
        row.Button = rowObject.AddComponent<Button>();
        row.Button.targetGraphic = row.Background;
        ColorBlock colors = row.Button.colors;
        colors.normalColor = FleetRowColor;
        colors.highlightedColor = new Color(0.23f, 0.28f, 0.36f, 1f);
        colors.pressedColor = new Color(0.16f, 0.19f, 0.25f, 1f);
        colors.selectedColor = new Color(0.24f, 0.28f, 0.34f, 1f);
        colors.disabledColor = new Color(0.12f, 0.14f, 0.18f, 0.65f);
        row.Button.colors = colors;
        row.Button.onClick.AddListener(() =>
        {
            LogUiInput($"Fleet Canvas: selected {GetTruckDisplayName(row.TruckNumber)} from fleet list");
            FocusTruck(row.TruckNumber);
            CompleteFleetTruckSelectionTutorial(row.TruckNumber);
        });

        GameObject accentObject = CreateUiObject("Accent", row.Root);
        RectTransform accentRect = accentObject.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(5f, 0f);
        accentRect.anchoredPosition = Vector2.zero;
        row.Accent = accentObject.AddComponent<Image>();
        row.Accent.color = FleetAccentColor;
        row.Accent.enabled = false;

        VerticalLayoutGroup contentLayout = rowObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(16, 14, 12, 12);
        contentLayout.spacing = 4;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        row.TruckNameText = CreateBodyText("TruckName", row.Root, uiFont, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        row.TruckNameText.fontStyle = FontStyle.Bold;
        row.TruckNameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
        row.DriverText = CreateBodyText("Driver", row.Root, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        row.DriverText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        row.StateText = CreateBodyText("State", row.Root, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, new Color(0.83f, 0.88f, 0.95f));
        row.StateText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        row.ResourceText = CreateBodyText("Resources", row.Root, uiFont, string.Empty, 11, TextAnchor.MiddleLeft, FleetMutedTextColor);
        row.ResourceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        return row;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform CreateStyledPanel(string name, Transform parent, Color color)
    {
        GameObject go = CreateUiObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.18f);
        outline.effectDistance = new Vector2(1f, -1f);
        return go.GetComponent<RectTransform>();
    }

    private static RectTransform CreateLayoutRow(string name, Transform parent, float preferredHeight, float spacing)
    {
        RectTransform row = CreateUiObject(name, parent).GetComponent<RectTransform>();
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        return row;
    }

    private static RectTransform CreateSectionCard(Transform parent, Font font, string title, out RectTransform body, bool addTitle = true)
    {
        RectTransform card = CreateStyledPanel($"{title}Card", parent, FleetInsetColor);
        VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(16, 16, 14, 16);
        cardLayout.spacing = 12;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;
        if (addTitle && !string.IsNullOrEmpty(title))
        {
            CreateHeaderText("Header", card, font, title, 18, TextAnchor.MiddleLeft, Color.white);
        }

        body = CreateUiObject("Body", card).GetComponent<RectTransform>();
        VerticalLayoutGroup bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.spacing = 8;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;
        return card;
    }

    private static Text CreateHeaderText(string name, Transform parent, Font font, string value, int fontSize, TextAnchor alignment, Color color)
    {
        Text text = CreateBodyText(name, parent, font, value, fontSize, alignment, color);
        text.fontStyle = FontStyle.Bold;
        return text;
    }

    private static Text CreateBodyText(string name, Transform parent, Font font, string value, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject go = CreateUiObject(name, parent);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = L(value);
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Text CreateValueText(string name, Transform parent, Font font)
    {
        Text text = CreateBodyText(name, parent, font, string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, Font font, out Text label, string buttonText, int fontSize, Color normalColor, Color textColor)
    {
        GameObject go = CreateUiObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = normalColor;
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.15f);
        colors.selectedColor = normalColor;
        colors.disabledColor = new Color(normalColor.r * 0.42f, normalColor.g * 0.42f, normalColor.b * 0.42f, 0.75f);
        button.colors = colors;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.20f);
        outline.effectDistance = new Vector2(1f, -1f);
        label = CreateBodyText("Label", go.transform, font, buttonText, fontSize, TextAnchor.MiddleCenter, textColor);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 0f);
        labelRect.offsetMax = new Vector2(-12f, 0f);
        return button;
    }

    private static string FormatValueLine(string label, string value)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(FleetMutedTextColor)}>{L(label)}:</color>  <color=#FFFFFF>{L(value)}</color>";
    }

    private static string FormatTruckCargoValue(int amount, CargoType cargoType, int capacity = 5)
    {
        if (amount <= 0 || cargoType == CargoType.None)
        {
            return L("Empty");
        }

        return $"{amount}/{capacity} ({GetCargoTypeLabel(cargoType)})";
    }

    private static string GetCargoTypeLabel(CargoType cargoType)
    {
        return cargoType switch
        {
            CargoType.Logs      => L("Logs"),
            CargoType.Boards    => L("Boards"),
            CargoType.Cotton    => L("Cotton"),
            CargoType.Textile   => L("Textile"),
            CargoType.Furniture => L("Furniture"),
            CargoType.Fuel      => L("Fuel"),
            CargoType.Alcohol   => L("Alcohol"),
            CargoType.Food      => L("Food"),
            _                   => L("None")
        };
    }

    private static void StretchRect(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetCenteredWindow(RectTransform rect, float width, float height, float yOffset)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, yOffset);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void AddOverlayCloseButton(RectTransform parent, Font font)
    {
        GameObject go = new("OverlayCloseButton");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-10f, -10f);
        rt.sizeDelta = new Vector2(36f, 36f);

        // Exclude from layout so it floats over the window
        go.AddComponent<LayoutElement>().ignoreLayout = true;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.62f, 0.14f, 0.10f, 0.94f);

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.0f, 0.36f, 0.28f, 1f);
        cb.pressedColor = new Color(0.5f, 0.10f, 0.08f, 1f);
        cb.selectedColor = Color.white;
        btn.colors = cb;
        btn.targetGraphic = img;
        btn.onClick.AddListener(CloseAllMenus);

        GameObject textGo = new("X");
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        StretchRect(textRt, 0f, 0f, 0f, 0f);
        Text txt = textGo.AddComponent<Text>();
        txt.text = "X";
        txt.font = font;
        txt.fontSize = 18;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.raycastTarget = false;

        // Render on top вЂ” move to last sibling
        go.transform.SetAsLastSibling();
    }

    // в”Ђв”Ђ Drivers Canvas Screen в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
}
