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
        public Text ProfileTitleText;
        public Text ProfileStateTitleText;
        public Text InfoStateText;
        public Text ProfileDriverSummaryText;
        public Text ProfileRosterSummaryText;
        public Text CrewTitleText;
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
        public Text ResourcesTitleText;
        public Text ResourcesFuelText;
        public Text ResourcesCargoText;
        public Text NavigationTitleText;
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
        SetCenteredWindow(screenRect, 1280f, 620f, -16f);
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
        leftPanelLayout.preferredWidth = 360f;
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
        fleetScreenUi.BuyTruckButton = CreateButton("BuyTruckButton", buyContainer, uiFont, out fleetScreenUi.BuyTruckButtonText, "Parking Slots", 16, FleetPrimaryButtonColor, Color.white);
        fleetScreenUi.BuyTruckButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
        fleetScreenUi.BuyTruckButton.onClick.AddListener(() =>
        {
            LogUiInput("Fleet Canvas: requested Parking truck slot");
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

        RectTransform infoCard = CreateSectionCard(fleetScreenUi.DetailsContentRoot.transform, uiFont, string.Empty, out RectTransform infoBody, false);
        LayoutElement infoCardLayout = infoCard.gameObject.AddComponent<LayoutElement>();
        infoCardLayout.preferredHeight = 156f;
        fleetScreenUi.InfoCardLayout = infoCardLayout;
        fleetScreenUi.ProfileTitleText = CreateHeaderText("ProfileTitle", infoBody, uiFont, string.Empty, 18, TextAnchor.MiddleLeft, Color.white);
        fleetScreenUi.ProfileTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        RectTransform profileTopRow = CreateLayoutRow("ProfileTopRow", infoBody, 72f, 12f);
        RectTransform stateBlock = CreateStyledPanel("StateBlock", profileTopRow, FleetCardMutedColor);
        stateBlock.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup stateBlockGroup = stateBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        stateBlockGroup.padding = new RectOffset(12, 12, 10, 10);
        stateBlockGroup.spacing = 4;
        stateBlockGroup.childControlWidth = true;
        stateBlockGroup.childControlHeight = true;
        stateBlockGroup.childForceExpandWidth = true;
        stateBlockGroup.childForceExpandHeight = false;
        fleetScreenUi.ProfileStateTitleText = CreateHeaderText("StateLabel", stateBlock, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        fleetScreenUi.InfoStateText = CreateBodyText("InfoState", stateBlock, uiFont, string.Empty, 18, TextAnchor.MiddleLeft, Color.white);
        fleetScreenUi.InfoStateText.fontStyle = FontStyle.Bold;
        fleetScreenUi.InfoStateText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        RectTransform summaryBlock = CreateStyledPanel("ProfileSummaryBlock", profileTopRow, FleetCardMutedColor);
        summaryBlock.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup summaryBlockGroup = summaryBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        summaryBlockGroup.padding = new RectOffset(12, 12, 10, 10);
        summaryBlockGroup.spacing = 4;
        summaryBlockGroup.childControlWidth = true;
        summaryBlockGroup.childControlHeight = true;
        summaryBlockGroup.childForceExpandWidth = true;
        summaryBlockGroup.childForceExpandHeight = false;
        fleetScreenUi.ProfileDriverSummaryText = CreateValueText("ProfileDriverSummary", summaryBlock, uiFont);
        fleetScreenUi.ProfileRosterSummaryText = CreateValueText("ProfileRosterSummary", summaryBlock, uiFont);

        RectTransform driverBlock = CreateSectionCard(fleetScreenUi.DetailsContentRoot.transform, uiFont, string.Empty, out RectTransform driverBody, false);
        LayoutElement driverCardLayout = driverBlock.gameObject.AddComponent<LayoutElement>();
        driverCardLayout.preferredHeight = 214f;
        fleetScreenUi.CrewTitleText = CreateHeaderText("CrewTitle", driverBody, uiFont, string.Empty, 18, TextAnchor.MiddleLeft, Color.white);
        fleetScreenUi.CrewTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        RectTransform driverBlockPanel = CreateStyledPanel("AssignedDriverBlock", driverBody, FleetCardMutedColor);
        driverBlockPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 150f;
        VerticalLayoutGroup driverBlockGroup = driverBlockPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        driverBlockGroup.padding = new RectOffset(12, 12, 10, 10);
        driverBlockGroup.spacing = 6;
        driverBlockGroup.childControlWidth = true;
        driverBlockGroup.childControlHeight = true;
        driverBlockGroup.childForceExpandWidth = true;
        driverBlockGroup.childForceExpandHeight = false;
        for (int slotIndex = 0; slotIndex < 2; slotIndex++)
        {
            CreateHeaderText($"AssignedDriverLabel{slotIndex + 1}", driverBlockPanel, uiFont, $"Assigned Driver {slotIndex + 1}", 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
            RectTransform driverRow = CreateLayoutRow($"AssignedDriverRow{slotIndex + 1}", driverBlockPanel, 28f, 10f);

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

        fleetScreenUi.AssignDriverPickerPanel = CreateStyledPanel("AssignDriverPickerPanel", driverBody, FleetCardMutedColor);
        LayoutElement assignDriverPickerLayout = fleetScreenUi.AssignDriverPickerPanel.gameObject.AddComponent<LayoutElement>();
        assignDriverPickerLayout.preferredHeight = 0f;
        fleetScreenUi.AssignDriverPickerLayout = assignDriverPickerLayout;
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

        RectTransform resourcesCard = CreateSectionCard(lowerRow, uiFont, string.Empty, out RectTransform resourcesBody, false);
        resourcesCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        fleetScreenUi.ResourcesTitleText = CreateHeaderText("ResourcesTitle", resourcesBody, uiFont, string.Empty, 18, TextAnchor.MiddleLeft, Color.white);
        fleetScreenUi.ResourcesTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        fleetScreenUi.ResourcesFuelText = CreateValueText("ResourcesFuel", resourcesBody, uiFont);
        fleetScreenUi.ResourcesCargoText = CreateValueText("ResourcesCargo", resourcesBody, uiFont);

        RectTransform navigationCard = CreateSectionCard(lowerRow, uiFont, string.Empty, out RectTransform navigationBody, false);
        navigationCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        fleetScreenUi.NavigationTitleText = CreateHeaderText("NavigationTitle", navigationBody, uiFont, string.Empty, 18, TextAnchor.MiddleLeft, Color.white);
        fleetScreenUi.NavigationTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
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

        bool hideBuyPanelForTutorial = false;
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
        bool ru = IsRussianLanguage();
        fleetScreenUi.FleetCountText.text = $"{GetOwnedTruckCount()} / {GetTruckParkingCapacity()} {(ru ? "грузовиков" : "Trucks")}";

        for (int i = 0; i < truckAgents.Count; i++)
        {
            TruckAgent truckAgent = truckAgents[i];
            FleetTruckRowUi row = fleetScreenUi.TruckRows[i];
            LoadTruckState(truckAgent);
            bool isSelected = isTruckDetailsOpen && selectedTruckNumber == truckAgent.TruckNumber;
            row.TruckNumber = truckAgent.TruckNumber;
            row.TruckNameText.text = truckAgent.DisplayName;
            row.DriverText.text = $"{(ru ? "Экипаж" : "Crew")}: {GetTruckAssignedDriverSummary(truckAgent)}";
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

        bool hideBuyPanelForTutorial = false;
        ApplyFleetTutorialVisibility();

        if (hideBuyPanelForTutorial)
        {
            return;
        }

        fleetScreenUi.BuyTruckButton.interactable = false;
        fleetScreenUi.BuyTruckButtonText.text = IsRussianLanguage() ? "Слоты Parking" : "Parking Slots";
        fleetScreenUi.BuyTruckStatusText.text = GetFleetBuyStatusLabel();
        fleetScreenUi.BuyTruckStatusText.color = locations.ContainsKey(LocationType.Parking) ? FleetSecondaryTextColor : new Color(0.96f, 0.72f, 0.42f, 1f);
    }

    private void UpdateFleetDetailsPanel()
    {
        TruckAgent selectedTruck = GetFleetSelectedTruck();
        bool hasSelection = selectedTruck != null && isTruckDetailsOpen;
        fleetScreenUi.DetailsPlaceholderText.transform.parent.parent.gameObject.SetActive(!hasSelection);
        fleetScreenUi.DetailsContentRoot.SetActive(hasSelection);

        if (!hasSelection)
        {
            fleetScreenUi.DetailsHeaderText.text = IsRussianLanguage() ? "Грузовик" : "Truck Details";
            return;
        }

        LoadTruckState(selectedTruck);
        bool ru = IsRussianLanguage();
        DriverAgent driver = selectedTruck.Driver;
        fleetScreenUi.DetailsHeaderText.text = selectedTruck.DisplayName;
        if (fleetScreenUi.ProfileTitleText != null)
        {
            fleetScreenUi.ProfileTitleText.text = ru ? "Профиль грузовика" : "Truck Profile";
        }
        if (fleetScreenUi.ProfileStateTitleText != null)
        {
            fleetScreenUi.ProfileStateTitleText.text = ru ? "Текущее состояние" : "Current State";
        }
        if (fleetScreenUi.CrewTitleText != null)
        {
            fleetScreenUi.CrewTitleText.text = ru ? "Экипаж" : "Crew";
        }
        if (fleetScreenUi.ResourcesTitleText != null)
        {
            fleetScreenUi.ResourcesTitleText.text = ru ? "Ресурсы" : "Resources";
        }
        if (fleetScreenUi.NavigationTitleText != null)
        {
            fleetScreenUi.NavigationTitleText.text = ru ? "Маршрут" : "Route";
        }
        fleetScreenUi.InfoStateText.text = GetTruckFleetStatusLabel();
        if (fleetScreenUi.ProfileDriverSummaryText != null)
        {
            string activeDriverLabel = driver != null ? driver.DriverName : (ru ? "Нет водителя" : "No active driver");
            fleetScreenUi.ProfileDriverSummaryText.text = FormatValueLine(ru ? "Активный водитель" : "Active driver", activeDriverLabel);
        }
        if (fleetScreenUi.ProfileRosterSummaryText != null)
        {
            fleetScreenUi.ProfileRosterSummaryText.text = FormatValueLine(
                ru ? "Слоты экипажа" : "Crew slots",
                $"{selectedTruck.AssignedDrivers.Count}/2");
        }
        for (int slotIndex = 0; slotIndex < fleetScreenUi.DriverLinkButtons.Count; slotIndex++)
        {
            bool hasRosterDriver = slotIndex < selectedTruck.AssignedDrivers.Count;
            DriverAgent rosterDriver = hasRosterDriver ? selectedTruck.AssignedDrivers[slotIndex] : null;
            bool isCurrentDriver = rosterDriver != null && rosterDriver == driver;
            fleetScreenUi.DriverLinkButtonTexts[slotIndex].text = rosterDriver == null
                ? (ru ? "Пусто" : "Empty")
                : (isCurrentDriver ? $"{rosterDriver.DriverName}  {(ru ? "(Текущий)" : "(Current)")}" : rosterDriver.DriverName);
            fleetScreenUi.DriverLinkButtons[slotIndex].interactable = rosterDriver != null;
            fleetScreenUi.AssignDriverButtonTexts[slotIndex].text = rosterDriver == null
                ? (ru ? "Назначить" : "Assign")
                : (ru ? "Назначен" : "Assigned");
            fleetScreenUi.AssignDriverButtons[slotIndex].interactable = rosterDriver == null;
            fleetScreenUi.RemoveDriverButtons[slotIndex].gameObject.SetActive(rosterDriver != null);
            fleetScreenUi.RemoveDriverButtons[slotIndex].interactable = CanUnassignDriverFromTruck(selectedTruck, rosterDriver);
            fleetScreenUi.RemoveDriverButtonTexts[slotIndex].text = "X";
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
        if (truckAgent == null) return "Idle";
        if (IsTruckOnActiveTradeRun(truckAgent)) return "Trade";
        if (truckAgent.IsPurchaseArrivalActive) return "Moving";
        if (truckAgent.IsTruckInteracting) return "Busy";
        if (truckAgent.IsTruckWaitingForService) return "Queue";
        if (truckAgent.IsDriverRescueActive) return "Rescue";
        if (truckAgent.IsTruckMoving) return "Moving";
        if (truckAgent.CurrentRefuelPhase != RefuelPhase.None) return "Refuel";
        if (truckAgent.CurrentAssignedTrip != TripType.None) return "Assigned";
        return IsTruckInsideParking(truckAgent) ? "Parked" : "Idle";
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
        if (!locations.ContainsKey(LocationType.Parking))
        {
            return IsRussianLanguage() ? "Сначала построй Парковку." : "Build Parking first.";
        }

        if (!CanProvisionTruckFromParkingCapacity())
        {
            return IsRussianLanguage() ? "Все слоты грузовиков Parking заняты." : "All Parking truck slots are in use.";
        }

        return IsRussianLanguage()
            ? "Parking создаёт грузовик автоматически, когда водитель получает смену."
            : "Parking provisions a truck automatically when a driver needs one.";
    }

    private string GetTruckAssignedDriverName(TruckAgent truckAgent)
    {
        return GetTruckAssignedDriverSummary(truckAgent);
    }


}

