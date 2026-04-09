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
        public Text ResourcesEnergyText;
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
        fleetScreenUi.ResourcesEnergyText = CreateValueText("ResourcesEnergy", resourcesBody, uiFont);
        fleetScreenUi.ResourcesCargoText = CreateValueText("ResourcesCargo", resourcesBody, uiFont);

        RectTransform navigationCard = CreateSectionCard(lowerRow, uiFont, "Navigation", out RectTransform navigationBody);
        navigationCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        fleetScreenUi.NavigationCellText = CreateValueText("NavigationCell", navigationBody, uiFont);
        fleetScreenUi.NavigationRouteText = CreateValueText("NavigationRoute", navigationBody, uiFont);
        fleetScreenUi.NavigationPayoutText = CreateValueText("NavigationPayout", navigationBody, uiFont);

        fleetScreenUi.CanvasRoot.SetActive(false);
        UpdateFleetScreenUi();
    }

    private void EnsureFleetEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
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

        if (!isFleetScreenDirty && shouldShow)
        {
            return;
        }

        EnsureFleetTruckRows();
        UpdateFleetListPanel();
        UpdateFleetDetailsPanel();
        isFleetScreenDirty = false;
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
            row.ResourceText.text = $"Fuel {Mathf.CeilToInt(truckAgent.TruckFuel)} / {Mathf.CeilToInt(TruckFuelCapacity)}    {GetTruckCargoSummary(truckAgent)}";
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
        fleetScreenUi.BuyTruckButton.interactable = canHireTruck;
        fleetScreenUi.BuyTruckButtonText.text = $"Buy New Truck — ${HireTruckCost}";
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
        fleetScreenUi.ResourcesEnergyText.text = FormatValueLine("Energy", $"{(driver != null ? Mathf.CeilToInt(driver.Energy) : 0)} / {Mathf.CeilToInt(DriverEnergyMax)}");
        fleetScreenUi.ResourcesCargoText.text = FormatValueLine("Cargo", $"{truckCargoAmount}/1 ({truckCargoType})");
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
            return "Cargo Empty";
        }

        return $"Cargo {truckAgent.TruckCargoType} {truckAgent.TruckCargoAmount}/1";
    }

    private string GetFleetBuyStatusLabel()
    {
        if (GetOwnedTruckCount() >= MaxTruckCount)
        {
            return "Fleet capacity reached.";
        }

        if (money < HireTruckCost)
        {
            return $"Need ${HireTruckCost} to hire a new truck.";
        }

        return "Adds a new truck directly to parking.";
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
            if (driver == null || driver.AssignedTruckNumber > 0)
            {
                continue;
            }

            if (driver.RestPhase != DriverRestPhase.None || IsDriverBusyWalkPhase(driver))
            {
                continue;
            }

            candidates.Add(driver);
        }

        return candidates;
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

        if (targetTruck.AssignedDrivers.Count >= 2)
        {
            LogTruckReaction(targetTruck, "driver assignment rejected: roster already full");
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
            driver.NeedsRestAfterTrip = false;
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
        text.text = value;
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
        return $"<color=#{ColorUtility.ToHtmlStringRGB(FleetMutedTextColor)}>{label}:</color>  <color=#FFFFFF>{value}</color>";
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

    // ── Drivers Canvas Screen ─────────────────────────────────────────────────

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

    private sealed class BuildScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Button RoadButton;
        public Text RoadButtonText;
        public Text RoadTitleText;
        public Text RoadDescriptionText;
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
        public Text ForestValueText;
        public Text ForestSubText;
        public Text SawmillValueText;
        public Text SawmillSubText;
        public Text WarehouseValueText;
        public Text WarehouseSubText;
        public Text TreasuryValueText;
    }

    private sealed class EconomyScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Text HeaderCountText;
        public RectTransform EntryListContent;
        public Text EmptyText;
        public readonly List<EconomyEntryRowUi> Rows = new();
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
                card.StatusBadgeBackground.color = truck != null
                    ? new Color(0.35f, 0.29f, 0.14f, 1f)
                    : new Color(0.24f, 0.29f, 0.36f, 1f);
            }

            card.NameText.text = d.DriverName;
            card.StatusText.text = truck != null ? "Assigned" : "Idle";

            card.TruckText.text = truck != null ? truck.DisplayName : "Unassigned";

            string energyMark = d.Energy <= DriverEnergyCriticalThreshold ? "  ⚠" : "";
            card.EnergyText.text = $"{Mathf.CeilToInt(d.Energy)} / {Mathf.CeilToInt(DriverEnergyMax)}{energyMark}";

            card.SalaryText.text = $"${d.Salary} / shift";
            card.BalanceText.text = $"${d.Money}";
        }

        bool canHire = money >= HireDriverCost;
        driversScreenUi.HireButton.interactable = canHire;
        driversScreenUi.HireButtonText.text = $"Hire New Driver — ${HireDriverCost}";
        driversScreenUi.HireStatusText.text = canHire
            ? "Adds a new driver to the workforce."
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
            row.StatusText.text = isAssigned ? $"Assigned: {GetShiftRangeLabel(driver.ShiftStartHour)}" : "Idle";
            row.StatusText.color = isAssigned ? new Color(0.62f, 0.92f, 0.62f, 1f) : FleetMutedTextColor;
        }

        DriverAgent selectedDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);

        for (int i = 0; i < shiftsScreenUi.ShiftCards.Count; i++)
        {
            ShiftCardUi card = shiftsScreenUi.ShiftCards[i];
            List<DriverAgent> assignedDrivers = new();
            foreach (DriverAgent driver in driverAgents)
            {
                if (driver.ShiftStartHour == card.ShiftHour)
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
            card.AssignButton.interactable = selectedDriver != null && !alreadyAssigned;
            card.AssignButtonText.text = selectedDriver == null
                ? "Select a driver to assign"
                : alreadyAssigned
                    ? $"{selectedDriver.DriverName} already assigned"
                    : $"Assign {selectedDriver.DriverName} → {ShiftNames[i]}";
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(shiftsScreenUi.DriverListContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(shiftsScreenUi.WindowRoot);
        isShiftsScreenDirty = false;
    }

    private void SetupBuildScreenUi()
    {
        if (buildScreenUi != null) return;

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buildScreenUi = new BuildScreenUiRefs();

        GameObject canvasObject = new("BuildScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        buildScreenUi.CanvasRoot = canvasObject;

        GameObject windowRoot = CreateUiObject("BuildWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        SetCenteredWindow(windowRect, 520f, 220f, -16f);
        buildScreenUi.WindowRoot = windowRect;

        Image windowBg = windowRoot.AddComponent<Image>();
        windowBg.color = FleetScreenTint;
        Outline windowOutline = windowRoot.AddComponent<Outline>();
        windowOutline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        windowOutline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup rootLayout = windowRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 16;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("BuildHeaderRow", windowRoot.transform, 40f, 0f);
        Text buildTitle = CreateHeaderText("BuildTitle", headerRow, font, "Build", 24, TextAnchor.MiddleLeft, Color.white);
        buildTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform toolCard = CreateSectionCard(windowRoot.transform, font, string.Empty, out RectTransform toolBody, false);
        toolCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 110f;
        RectTransform toolRow = CreateUiObject("BuildToolRow", toolBody).GetComponent<RectTransform>();
        HorizontalLayoutGroup toolLayout = toolRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        toolLayout.spacing = 14;
        toolLayout.childControlWidth = true;
        toolLayout.childControlHeight = true;
        toolLayout.childForceExpandWidth = false;
        toolLayout.childForceExpandHeight = false;

        buildScreenUi.RoadButton = CreateButton("BuildRoadButton", toolRow, font, out Text roadButtonText, "ROAD", 16, FleetPrimaryButtonColor, Color.white);
        buildScreenUi.RoadButtonText = roadButtonText;
        LayoutElement roadButtonLayout = buildScreenUi.RoadButton.gameObject.AddComponent<LayoutElement>();
        roadButtonLayout.preferredWidth = 96f;
        roadButtonLayout.preferredHeight = 56f;
        buildScreenUi.RoadButton.onClick.AddListener(() =>
        {
            bool roadModeActive = activeBuildTool == BuildTool.Road;
            activeBuildTool = roadModeActive ? BuildTool.None : BuildTool.Road;
            isBuildPanelOpen = false;
            LogUiInput($"Build Canvas: switched tool to {activeBuildTool}");
            PlayUiSound(uiSelectClip, 0.85f);
            SessionDebugLogger.Log("BUILD", $"Build tool switched to {activeBuildTool}.");
            isBuildScreenDirty = true;
        });

        RectTransform roadInfo = CreateUiObject("RoadInfo", toolRow).GetComponent<RectTransform>();
        roadInfo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup roadInfoLayout = roadInfo.gameObject.AddComponent<VerticalLayoutGroup>();
        roadInfoLayout.spacing = 6;
        roadInfoLayout.childControlWidth = true;
        roadInfoLayout.childControlHeight = true;
        roadInfoLayout.childForceExpandWidth = true;
        roadInfoLayout.childForceExpandHeight = false;

        buildScreenUi.RoadTitleText = CreateHeaderText("RoadTitle", roadInfo, font, "Road", 18, TextAnchor.MiddleLeft, Color.white);
        buildScreenUi.RoadDescriptionText = CreateBodyText("RoadDescription", roadInfo, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);

        buildScreenUi.CanvasRoot.SetActive(false);
        UpdateBuildScreenUi();
    }

    private void UpdateBuildScreenUi()
    {
        if (buildScreenUi == null) return;

        bool shouldShow = isBuildPanelOpen;
        if (buildScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            buildScreenUi.CanvasRoot.SetActive(shouldShow);
            isBuildScreenDirty = true;
        }

        if (!shouldShow) return;
        if (!isBuildScreenDirty) return;

        bool roadModeActive = activeBuildTool == BuildTool.Road;
        Image buttonImage = buildScreenUi.RoadButton.GetComponent<Image>();
        if (buttonImage != null)
        {
            buttonImage.color = roadModeActive ? FleetAccentColor : FleetPrimaryButtonColor;
        }

        buildScreenUi.RoadButtonText.text = "ROAD";
        buildScreenUi.RoadTitleText.text = "Road";
        buildScreenUi.RoadDescriptionText.text = roadModeActive
            ? "Mode active: left click builds, right click removes."
            : "Click to enter road building mode.";

        LayoutRebuilder.ForceRebuildLayoutImmediate(buildScreenUi.WindowRoot);
        isBuildScreenDirty = false;
    }

    private void SetupResourcesScreenUi()
    {
        if (resourcesScreenUi != null) return;

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resourcesScreenUi = new ResourcesScreenUiRefs();

        GameObject canvasObject = new("ResourcesScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        resourcesScreenUi.CanvasRoot = canvasObject;

        GameObject windowRoot = CreateUiObject("ResourcesWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        SetCenteredWindow(windowRect, 700f, 460f, -16f);
        resourcesScreenUi.WindowRoot = windowRect;

        Image windowBg = windowRoot.AddComponent<Image>();
        windowBg.color = DriversScreenTint;
        Outline windowOutline = windowRoot.AddComponent<Outline>();
        windowOutline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        windowOutline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup rootLayout = windowRoot.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 16;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("ResourcesHeaderRow", windowRoot.transform, 40f, 0f);
        Text resourcesTitleText = CreateHeaderText("ResourcesTitle", headerRow, font, "Resources", 24, TextAnchor.MiddleLeft, Color.white);
        resourcesTitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        resourcesScreenUi.HeaderCountText = CreateHeaderText("ResourcesHeaderCount", headerRow, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        RectTransform contentRoot = CreateUiObject("ResourcesContentRoot", windowRoot.transform).GetComponent<RectTransform>();
        LayoutElement contentLayout = contentRoot.gameObject.AddComponent<LayoutElement>();
        contentLayout.flexibleHeight = 1f;
        VerticalLayoutGroup contentGroup = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        contentGroup.spacing = 14;
        contentGroup.childControlWidth = true;
        contentGroup.childControlHeight = true;
        contentGroup.childForceExpandWidth = true;
        contentGroup.childForceExpandHeight = false;

        CreateResourceSummaryCard(contentRoot, font, "Forest", out resourcesScreenUi.ForestValueText, out resourcesScreenUi.ForestSubText);
        CreateResourceSummaryCard(contentRoot, font, "Sawmill", out resourcesScreenUi.SawmillValueText, out resourcesScreenUi.SawmillSubText);
        CreateResourceSummaryCard(contentRoot, font, "Warehouse", out resourcesScreenUi.WarehouseValueText, out resourcesScreenUi.WarehouseSubText);

        RectTransform footerCard = CreateSectionCard(windowRoot.transform, font, "Treasury", out RectTransform footerBody);
        footerCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 74f;
        resourcesScreenUi.TreasuryValueText = CreateHeaderText("TreasuryValue", footerBody, font, string.Empty, 18, TextAnchor.MiddleLeft, FleetAccentColor);

        resourcesScreenUi.CanvasRoot.SetActive(false);
        UpdateResourcesScreenUi();
    }

    private static void CreateResourceSummaryCard(RectTransform parent, Font font, string title, out Text valueText, out Text subText)
    {
        RectTransform card = CreateSectionCard(parent, font, title, out RectTransform body);
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = 88f;
        valueText = CreateHeaderText($"{title}Value", body, font, string.Empty, 18, TextAnchor.MiddleLeft, Color.white);
        valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        subText = CreateBodyText($"{title}Sub", body, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        subText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
    }

    private void UpdateResourcesScreenUi()
    {
        if (resourcesScreenUi == null) return;

        bool shouldShow = isResourcesPanelOpen;
        if (resourcesScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            resourcesScreenUi.CanvasRoot.SetActive(shouldShow);
        }

        if (!shouldShow) return;

        resourcesScreenUi.HeaderCountText.text = "3 Production Nodes";
        resourcesScreenUi.ForestValueText.text = $"{locations[LocationType.Forest].LogsStored} / {ForestMaxLogsStorage} Logs";
        resourcesScreenUi.ForestSubText.text = "Raw logs ready for pickup";
        resourcesScreenUi.SawmillValueText.text = $"{locations[LocationType.Sawmill].BoardsStored} Boards";
        resourcesScreenUi.SawmillSubText.text = $"{locations[LocationType.Sawmill].LogsStored} logs waiting for processing";
        resourcesScreenUi.WarehouseValueText.text = $"{locations[LocationType.Warehouse].BoardsStored} Boards";
        resourcesScreenUi.WarehouseSubText.text = "Finished goods in storage";
        resourcesScreenUi.TreasuryValueText.text = $"${money}";

        LayoutRebuilder.ForceRebuildLayoutImmediate(resourcesScreenUi.WindowRoot);
    }

    private void SetupEconomyScreenUi()
    {
        if (economyScreenUi != null) return;

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        economyScreenUi = new EconomyScreenUiRefs();

        GameObject canvasObject = new("EconomyScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        economyScreenUi.CanvasRoot = canvasObject;

        GameObject windowRoot = CreateUiObject("EconomyWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        SetCenteredWindow(windowRect, 920f, 560f, -16f);
        economyScreenUi.WindowRoot = windowRect;

        Image windowBg = windowRoot.AddComponent<Image>();
        windowBg.color = DriversScreenTint;
        Outline windowOutline = windowRoot.AddComponent<Outline>();
        windowOutline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        windowOutline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup rootLayout = windowRoot.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 16;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("EconomyHeaderRow", windowRoot.transform, 40f, 0f);
        Text titleText = CreateHeaderText("EconomyTitle", headerRow, font, "Economy", 24, TextAnchor.MiddleLeft, Color.white);
        titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        economyScreenUi.HeaderCountText = CreateHeaderText("EconomyCount", headerRow, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        RectTransform ledgerFrame = CreateStyledPanel("EconomyLedgerFrame", windowRoot.transform, FleetInsetColor);
        LayoutElement frameLayout = ledgerFrame.gameObject.AddComponent<LayoutElement>();
        frameLayout.flexibleHeight = 1f;
        frameLayout.minHeight = 360f;

        GameObject scrollObj = CreateUiObject("EconomyScrollView", ledgerFrame);
        StretchRect(scrollObj.GetComponent<RectTransform>(), 8f, 8f, 8f, 8f);
        Image scrollImage = scrollObj.AddComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0f);
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
        contentLayout.spacing = 10f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewportObj.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
        economyScreenUi.EntryListContent = contentRect;

        economyScreenUi.EmptyText = CreateBodyText("EconomyEmptyText", contentRect, font, "No money movements yet.", 15, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
        economyScreenUi.EmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        for (int i = 0; i < MaxEconomyRowSlots; i++)
        {
            economyScreenUi.Rows.Add(CreateEconomyEntryRow(contentRect, font, i));
        }

        economyScreenUi.CanvasRoot.SetActive(false);
        UpdateEconomyScreenUi();
    }

    private static EconomyEntryRowUi CreateEconomyEntryRow(RectTransform parent, Font font, int rowIndex)
    {
        EconomyEntryRowUi row = new();
        RectTransform card = CreateSectionCard(parent, font, string.Empty, out RectTransform body, false);
        row.Root = card;
        LayoutElement cardLayout = card.gameObject.AddComponent<LayoutElement>();
        cardLayout.preferredHeight = 84f;

        RectTransform topRow = CreateLayoutRow($"EconomyTopRow{rowIndex}", body, 24f, 12f);
        row.TimeText = CreateBodyText($"EconomyTime{rowIndex}", topRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        row.TimeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 82f;
        row.FlowText = CreateBodyText($"EconomyFlow{rowIndex}", topRow, font, string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
        row.FlowText.fontStyle = FontStyle.Bold;
        row.FlowText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        row.AmountText = CreateHeaderText($"EconomyAmount{rowIndex}", topRow, font, string.Empty, 15, TextAnchor.MiddleRight, FleetAccentColor);
        row.AmountText.gameObject.AddComponent<LayoutElement>().preferredWidth = 116f;

        row.ReasonText = CreateBodyText($"EconomyReason{rowIndex}", body, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        row.ReasonText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        return row;
    }

    private void UpdateEconomyScreenUi()
    {
        if (economyScreenUi == null) return;

        bool shouldShow = isEconomyPanelOpen;
        if (economyScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            economyScreenUi.CanvasRoot.SetActive(shouldShow);
            isEconomyScreenDirty = true;
        }

        if (!shouldShow) return;
        bool forceLayoutRebuild = isEconomyScreenDirty;

        economyScreenUi.HeaderCountText.text = $"{moneyLedgerEntries.Count} Entr{(moneyLedgerEntries.Count == 1 ? "y" : "ies")}";
        economyScreenUi.EmptyText.gameObject.SetActive(moneyLedgerEntries.Count == 0);

        for (int i = 0; i < economyScreenUi.Rows.Count; i++)
        {
            bool active = i < moneyLedgerEntries.Count;
            EconomyEntryRowUi row = economyScreenUi.Rows[i];
            row.Root.gameObject.SetActive(active);
            if (!active) continue;

            MoneyLedgerEntry entry = moneyLedgerEntries[i];
            row.TimeText.text = entry.TimeLabel;
            row.FlowText.text = $"{entry.FromLabel} → {entry.ToLabel}";
            row.ReasonText.text = BuildEconomyEntryDetail(entry);
            bool isIncome = entry.TreasuryDelta > 0;
            row.AmountText.text = $"{(isIncome ? "+" : "-")}${Mathf.Abs(entry.TreasuryDelta)}";
            row.AmountText.color = isIncome ? new Color(0.56f, 0.92f, 0.57f, 1f) : new Color(0.95f, 0.66f, 0.42f, 1f);
        }

        if (forceLayoutRebuild)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(economyScreenUi.EntryListContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(economyScreenUi.WindowRoot);
        }
        isEconomyScreenDirty = false;
    }

    private static string BuildEconomyEntryDetail(MoneyLedgerEntry entry)
    {
        string detail = entry.Reason;
        if (entry.RecipientBalanceAfter.HasValue)
        {
            detail += $" • {entry.ToLabel} balance: ${entry.RecipientBalanceAfter.Value}";
        }

        if (entry.TreasuryAfter.HasValue)
        {
            detail += $" • Treasury: ${entry.TreasuryAfter.Value}";
        }

        return detail;
    }
}
