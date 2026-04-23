using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class TruckQuickHudRefs
    {
        public GameObject CanvasRoot;
        public RectTransform Root;
        public Text HeaderText;
        public Text StateText;
        public Button DriverButton;
        public Text DriverButtonText;
        public Text FuelText;
        public Text CargoText;
        public Text RouteText;
        public Button FleetButton;
        public Text FleetButtonText;
        public Button CameraButton;
        public Text CameraButtonText;
        public Button CloseButton;
        public Text CloseButtonText;
    }

    private TruckQuickHudRefs truckQuickHud;

    private void SetupTruckQuickHud()
    {
        if (truckQuickHud != null)
        {
            return;
        }

        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        truckQuickHud = new TruckQuickHudRefs();

        GameObject canvasObject = new("TruckQuickHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        truckQuickHud.CanvasRoot = canvasObject;

        RectTransform root = CreateStyledPanel("TruckQuickHudRoot", canvasObject.transform, FleetPanelColor);
        root.anchorMin = new Vector2(1f, 0f);
        root.anchorMax = new Vector2(1f, 0f);
        root.pivot = new Vector2(1f, 0f);
        root.anchoredPosition = new Vector2(-18f, 104f);
        root.sizeDelta = new Vector2(390f, 316f);
        VerticalLayoutGroup rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(16, 16, 16, 16);
        rootLayout.spacing = 14;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        truckQuickHud.Root = root;

        RectTransform headerRow = CreateLayoutRow("QuickHudHeaderRow", root, 30f, 10f);
        truckQuickHud.HeaderText = CreateHeaderText("Header", headerRow, uiFont, "Truck", 21, TextAnchor.MiddleLeft, Color.white);
        LayoutElement headerTextLayout = truckQuickHud.HeaderText.gameObject.AddComponent<LayoutElement>();
        headerTextLayout.flexibleWidth = 1f;
        truckQuickHud.CloseButton = CreateButton("CloseButton", headerRow, uiFont, out truckQuickHud.CloseButtonText, "X", 12, new Color(0.26f, 0.30f, 0.36f, 1f), Color.white);
        LayoutElement closeLayout = truckQuickHud.CloseButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 28f;
        closeLayout.preferredHeight = 28f;
        truckQuickHud.CloseButton.onClick.AddListener(() =>
        {
            LogUiInput($"Quick HUD: closed {GetTruckDisplayName(selectedTruckNumber)}");
            ClearTruckFocus();
        });

        RectTransform summaryCard = CreateSectionCard(root, uiFont, string.Empty, out RectTransform summaryBody, false);
        summaryCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 166f;
        truckQuickHud.StateText = CreateBodyText("State", summaryBody, uiFont, string.Empty, 17, TextAnchor.MiddleLeft, Color.white);
        truckQuickHud.StateText.fontStyle = FontStyle.Bold;
        truckQuickHud.StateText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

        RectTransform driverRow = CreateLayoutRow("DriverRow", summaryBody, 28f, 8f);
        CreateHeaderText("DriverLabel", driverRow, uiFont, "Driver", 12, TextAnchor.MiddleLeft, FleetMutedTextColor).gameObject.AddComponent<LayoutElement>().preferredWidth = 54f;
        truckQuickHud.DriverButton = CreateButton("DriverButton", driverRow, uiFont, out truckQuickHud.DriverButtonText, "None", 12, new Color(0.23f, 0.29f, 0.36f, 1f), Color.white);
        LayoutElement driverButtonLayout = truckQuickHud.DriverButton.gameObject.AddComponent<LayoutElement>();
        driverButtonLayout.flexibleWidth = 1f;
        driverButtonLayout.preferredHeight = 28f;
        truckQuickHud.DriverButtonText.alignment = TextAnchor.MiddleLeft;
        truckQuickHud.DriverButton.onClick.AddListener(() =>
        {
            TruckAgent selectedTruck = GetTruckAgent(selectedTruckNumber);
            if (selectedTruck?.Driver == null)
            {
                return;
            }

            LogUiInput($"Quick HUD: opened Drivers for {selectedTruck.Driver.DriverName} from {selectedTruck.DisplayName}");
            OpenDriversPanelForDriver(selectedTruck.Driver.DriverId);
        });

        RectTransform statsGrid = CreateUiObject("StatsGrid", summaryBody).GetComponent<RectTransform>();
        GridLayoutGroup statsLayout = statsGrid.gameObject.AddComponent<GridLayoutGroup>();
        statsLayout.cellSize = new Vector2(318f, 26f);
        statsLayout.spacing = new Vector2(0f, 6f);
        statsLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        statsLayout.constraintCount = 1;
        statsGrid.gameObject.AddComponent<LayoutElement>().preferredHeight = 92f;
        truckQuickHud.FuelText = CreateBodyText("FuelText", statsGrid, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        truckQuickHud.CargoText = CreateBodyText("CargoText", statsGrid, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        truckQuickHud.RouteText = CreateBodyText("RouteText", statsGrid, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);

        RectTransform actionRow = CreateLayoutRow("QuickHudActionRow", root, 34f, 10f);
        truckQuickHud.FleetButton = CreateButton("OpenFleetButton", actionRow, uiFont, out truckQuickHud.FleetButtonText, "Open Fleet", 13, FleetPrimaryButtonColor, Color.white);
        LayoutElement fleetButtonLayout = truckQuickHud.FleetButton.gameObject.AddComponent<LayoutElement>();
        fleetButtonLayout.flexibleWidth = 1f;
        fleetButtonLayout.preferredHeight = 34f;
        truckQuickHud.FleetButton.onClick.AddListener(OpenFleetFromQuickHud);

        truckQuickHud.CameraButton = CreateButton("CameraButton", actionRow, uiFont, out truckQuickHud.CameraButtonText, "Follow Camera", 13, new Color(0.25f, 0.33f, 0.46f, 1f), Color.white);
        LayoutElement cameraButtonLayout = truckQuickHud.CameraButton.gameObject.AddComponent<LayoutElement>();
        cameraButtonLayout.flexibleWidth = 1f;
        cameraButtonLayout.preferredHeight = 34f;
        truckQuickHud.CameraButton.onClick.AddListener(ToggleTruckCameraFocus);

        truckQuickHud.CanvasRoot.SetActive(false);
        UpdateTruckQuickHud();
    }

    private void UpdateTruckQuickHud()
    {
        if (truckQuickHud == null)
        {
            return;
        }

        bool shouldShow =
            isTruckDetailsOpen &&
            !isFleetPanelOpen &&
            !isDriversPanelOpen &&
            !isShiftsPanelOpen &&
            !isResourcesPanelOpen &&
            !isBuildPanelOpen;

        if (truckQuickHud.CanvasRoot.activeSelf != shouldShow)
        {
            truckQuickHud.CanvasRoot.SetActive(shouldShow);
        }

        if (!shouldShow)
        {
            return;
        }

        TruckAgent selectedTruck = GetTruckAgent(selectedTruckNumber);
        if (selectedTruck == null)
        {
            truckQuickHud.CanvasRoot.SetActive(false);
            return;
        }

        LoadTruckState(selectedTruck);
        DriverAgent driver = selectedTruck.Driver;
        truckQuickHud.HeaderText.text = selectedTruck.DisplayName;
        truckQuickHud.StateText.text = L(GetTruckFleetStatusLabel());
        truckQuickHud.DriverButtonText.text = driver != null ? driver.DriverName : L("None");
        truckQuickHud.DriverButton.interactable = driver != null;
        truckQuickHud.FuelText.text = FormatValueLine("Fuel", $"{Mathf.CeilToInt(truckFuel)} / {Mathf.CeilToInt(TruckFuelCapacity)}");
        truckQuickHud.CargoText.text = FormatValueLine("Cargo", FormatTruckCargoValue(truckCargoAmount, truckCargoType));
        truckQuickHud.RouteText.text = FormatValueLine("Route", GetTruckQuickHudRouteLabel(selectedTruck));
        truckQuickHud.FleetButtonText.text = L("Open Fleet");
        truckQuickHud.CameraButtonText.text = L(isTruckCameraFocused ? "Exit Follow" : "Follow Camera");
        SaveTruckState(selectedTruck);
    }

    private string GetTruckQuickHudRouteLabel(TruckAgent selectedTruck)
    {
        if (IsTruckOnActiveTradeRun(selectedTruck))
        {
            return GetCompactTradeRunStatusLabel();
        }

        return GetTripTitle(currentAssignedTrip);
    }

    private string GetCompactTradeRunStatusLabel()
    {
        if (!HasActiveTradeRun())
        {
            return "None";
        }

        string orderLabel = activeTradeRun.OrderType == TradeOrderType.Buy ? "Buy" : "Sell";
        string resourceLabel = GetTradeResourceLabel(activeTradeRun.ResourceType);
        string phaseLabel = activeTradeRun.Phase switch
        {
            TradeRunPhase.DriverToParking    => "driver to parking",
            TradeRunPhase.DrivingToWarehouse => "to warehouse",
            TradeRunPhase.LoadingAtWarehouse => "loading",
            TradeRunPhase.DrivingToHighway   => "to highway",
            TradeRunPhase.DrivingOffMap      => "leaving map",
            TradeRunPhase.OutOfMap           => $"away {Mathf.CeilToInt(activeTradeRun.OutOfMapTimer)}s",
            TradeRunPhase.ReturningFromOffMap => "entering map",
            TradeRunPhase.ReturningToWarehouse => "to warehouse",
            TradeRunPhase.UnloadingAtWarehouse => "unloading",
            TradeRunPhase.ReturningToParking => "to parking",
            _                                => "active"
        };

        return $"{orderLabel} {resourceLabel}: {phaseLabel}";
    }

    private void OpenFleetFromQuickHud()
    {
        LogUiInput($"Quick HUD: opened Fleet for {GetTruckDisplayName(selectedTruckNumber)}");
        isFleetPanelOpen = true;
        isDriversPanelOpen = false;
        isShiftsPanelOpen = false;
        isResourcesPanelOpen = false;
        isBuildPanelOpen = false;
        isFleetScreenDirty = true;
        PlayUiSound(uiPanelOpenClip, 0.86f);
    }
}

