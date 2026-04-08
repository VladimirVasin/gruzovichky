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
        public Text InfoStateText;
        public Button DriverLinkButton;
        public Text DriverLinkButtonText;
        public Button AssignDriverButton;
        public Text AssignDriverButtonText;
        public RectTransform AssignDriverPickerPanel;
        public Text AssignDriverPickerTitleText;
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
        infoCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 208f;
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
        driverBlock.gameObject.AddComponent<LayoutElement>().preferredHeight = 62f;
        VerticalLayoutGroup driverBlockGroup = driverBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        driverBlockGroup.padding = new RectOffset(12, 12, 10, 10);
        driverBlockGroup.spacing = 6;
        driverBlockGroup.childControlWidth = true;
        driverBlockGroup.childControlHeight = true;
        driverBlockGroup.childForceExpandWidth = true;
        driverBlockGroup.childForceExpandHeight = false;
        CreateHeaderText("AssignedDriverLabel", driverBlock, uiFont, "Assigned Driver", 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        RectTransform driverRow = CreateLayoutRow("AssignedDriverRow", driverBlock, 28f, 10f);
        fleetScreenUi.DriverLinkButton = CreateButton("DriverLinkButton", driverRow, uiFont, out fleetScreenUi.DriverLinkButtonText, "Driver: None", 13, new Color(0.23f, 0.29f, 0.36f, 1f), Color.white);
        LayoutElement driverLinkLayout = fleetScreenUi.DriverLinkButton.gameObject.AddComponent<LayoutElement>();
        driverLinkLayout.preferredHeight = 28f;
        driverLinkLayout.flexibleWidth = 1f;
        fleetScreenUi.DriverLinkButtonText.alignment = TextAnchor.MiddleLeft;
        fleetScreenUi.DriverLinkButton.onClick.AddListener(() =>
        {
            TruckAgent selectedTruck = GetFleetSelectedTruck();
            if (selectedTruck?.Driver == null)
            {
                return;
            }

            LogUiInput($"Fleet Canvas: opened Drivers for {selectedTruck.Driver.DriverName} from {selectedTruck.DisplayName}");
            OpenDriversPanelForDriver(selectedTruck.Driver.DriverId);
        });
        fleetScreenUi.AssignDriverButton = CreateButton("AssignDriverButton", driverRow, uiFont, out fleetScreenUi.AssignDriverButtonText, "Assign", 12, new Color(0.39f, 0.44f, 0.56f, 1f), Color.white);
        LayoutElement assignDriverLayout = fleetScreenUi.AssignDriverButton.gameObject.AddComponent<LayoutElement>();
        assignDriverLayout.preferredWidth = 138f;
        assignDriverLayout.preferredHeight = 28f;
        fleetScreenUi.AssignDriverButton.onClick.AddListener(() =>
        {
            TruckAgent selectedTruck = GetFleetSelectedTruck();
            if (selectedTruck != null)
            {
                LogUiInput($"Fleet Canvas: clicked {fleetScreenUi.AssignDriverButtonText.text} for {selectedTruck.DisplayName}");
            }

            ToggleFleetDriverAssignmentPicker();
        });

        fleetScreenUi.AssignDriverPickerPanel = CreateStyledPanel("AssignDriverPickerPanel", infoBody, FleetCardMutedColor);
        fleetScreenUi.AssignDriverPickerPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 128f;
        VerticalLayoutGroup pickerGroup = fleetScreenUi.AssignDriverPickerPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        pickerGroup.padding = new RectOffset(12, 12, 12, 12);
        pickerGroup.spacing = 6;
        pickerGroup.childControlWidth = true;
        pickerGroup.childControlHeight = true;
        pickerGroup.childForceExpandWidth = true;
        pickerGroup.childForceExpandHeight = false;
        fleetScreenUi.AssignDriverPickerTitleText = CreateHeaderText("AssignDriverPickerTitle", fleetScreenUi.AssignDriverPickerPanel, uiFont, "Select Driver", 13, TextAnchor.MiddleLeft, Color.white);
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
            row.DriverText.text = truckAgent.Driver != null ? truckAgent.Driver.DriverName : "Driver: None";
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
        fleetScreenUi.DriverLinkButtonText.text = driver != null ? driver.DriverName : "Driver: None";
        fleetScreenUi.DriverLinkButton.interactable = driver != null;
        fleetScreenUi.ResourcesFuelText.text = FormatValueLine("Fuel", $"{Mathf.CeilToInt(truckFuel)} / {Mathf.CeilToInt(TruckFuelCapacity)}");
        fleetScreenUi.ResourcesEnergyText.text = FormatValueLine("Energy", $"{(driver != null ? Mathf.CeilToInt(driver.Energy) : 0)} / {Mathf.CeilToInt(DriverEnergyMax)}");
        fleetScreenUi.ResourcesCargoText.text = FormatValueLine("Cargo", $"{truckCargoAmount}/1 ({truckCargoType})");
        fleetScreenUi.NavigationCellText.text = FormatValueLine("Grid Cell", $"{truckCell.x}, {truckCell.y}");
        fleetScreenUi.NavigationRouteText.text = FormatValueLine("Assigned Route", GetTripTitle(currentAssignedTrip));
        fleetScreenUi.NavigationPayoutText.text = FormatValueLine("Trip Payout", $"${currentAssignedTripReward}");
        fleetScreenUi.AssignDriverButtonText.text = driver == null ? "Assign Driver" : "Change";
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
        return truckAgent?.Driver != null ? truckAgent.Driver.DriverName : "None";
    }

    private void ToggleFleetDriverAssignmentPicker()
    {
        if (fleetScreenUi?.AssignDriverPickerPanel == null)
        {
            return;
        }

        bool nextState = !fleetScreenUi.AssignDriverPickerPanel.gameObject.activeSelf;
        fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(nextState);
        TruckAgent selectedTruck = GetFleetSelectedTruck();
        if (selectedTruck != null)
        {
            LogUiInput($"Fleet Canvas: {(nextState ? "opened" : "closed")} driver picker for {selectedTruck.DisplayName}");
        }
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
            return;
        }

        List<DriverAgent> candidates = GetDriverAssignmentCandidates(selectedTruck);
        fleetScreenUi.AssignDriverPickerTitleText.text = candidates.Count == 0 ? "No available drivers" : "Select Driver";
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

        if (fleetScreenUi.AssignDriverPickerPanel.gameObject.activeSelf && candidates.Count == 0)
        {
            fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(false);
        }
    }

    private List<DriverAgent> GetDriverAssignmentCandidates(TruckAgent selectedTruck)
    {
        List<DriverAgent> candidates = new();
        foreach (TruckAgent truckAgent in truckAgents)
        {
            DriverAgent driver = truckAgent.Driver;
            if (driver == null)
            {
                continue;
            }

            if (driver == selectedTruck.Driver)
            {
                continue;
            }

            if (!CanReassignTruckDriver(selectedTruck, truckAgent, driver))
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
            isFleetScreenDirty = true;
        }
    }

    private bool CanReassignTruckDriver(TruckAgent targetTruck, TruckAgent sourceTruck, DriverAgent candidateDriver)
    {
        if (targetTruck == null || sourceTruck == null || candidateDriver == null)
        {
            return false;
        }

        if (candidateDriver.RestPhase != DriverRestPhase.None || candidateDriver.WalkPhase != DriverRescuePhase.None)
        {
            return false;
        }

        return IsTruckSafeForDriverSwap(targetTruck) && IsTruckSafeForDriverSwap(sourceTruck);
    }

    private bool IsTruckSafeForDriverSwap(TruckAgent truckAgent)
    {
        if (truckAgent == null)
        {
            return false;
        }

        LoadTruckState(truckAgent);
        bool isSafe =
            !isTruckMoving &&
            !isTruckInteracting &&
            !isDriverRescueActive &&
            currentAssignedTrip == TripType.None &&
            currentRefuelPhase == RefuelPhase.None &&
            IsTruckInsideParking();
        SaveTruckState(truckAgent);
        return isSafe;
    }

    private TruckAgent GetAssignedTruckForDriver(DriverAgent driver)
    {
        foreach (TruckAgent truckAgent in truckAgents)
        {
            if (truckAgent.Driver == driver)
            {
                return truckAgent;
            }
        }

        return null;
    }

    private bool AssignDriverToTruck(TruckAgent targetTruck, DriverAgent driver)
    {
        TruckAgent sourceTruck = GetAssignedTruckForDriver(driver);
        LogCommand($"AssignDriver({targetTruck?.DisplayName ?? "null"}, {driver?.DriverName ?? "null"})");
        if (targetTruck == null || driver == null || sourceTruck == null || sourceTruck == targetTruck)
        {
            if (targetTruck != null)
            {
                LogTruckReaction(targetTruck, "driver assignment rejected: invalid assignment target");
            }
            return false;
        }

        if (!CanReassignTruckDriver(targetTruck, sourceTruck, driver))
        {
            LogTruckReaction(targetTruck, $"driver assignment rejected for {driver.DriverName}: swap is not safe right now");
            return false;
        }

        DriverAgent previousDriver = targetTruck.Driver;
        targetTruck.Driver = driver;
        sourceTruck.Driver = previousDriver;
        SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} assigned to {targetTruck.DisplayName}; {previousDriver.DriverName} moved to {sourceTruck.DisplayName}.");
        LogDriverReaction(driver, $"assigned to {targetTruck.DisplayName}");
        LogDriverReaction(previousDriver, $"moved to {sourceTruck.DisplayName}");
        LogTruckReaction(targetTruck, $"accepted driver {driver.DriverName}");
        LogTruckReaction(sourceTruck, $"received driver {previousDriver.DriverName}");
        PlayUiSound(uiSelectClip, 0.88f);
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
}
