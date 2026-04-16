using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
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
        public Button CloseButton;
        public Text CloseButtonText;
    }

    private BuildingQuickHudRefs buildingQuickHud;

    private void SetupBuildingQuickHud()
    {
        if (buildingQuickHud != null)
        {
            return;
        }

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
        root.sizeDelta = new Vector2(340f, 290f);
        VerticalLayoutGroup rootLayout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(16, 16, 16, 16);
        rootLayout.spacing = 14;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;
        buildingQuickHud.Root = root;

        RectTransform headerRow = CreateLayoutRow("BuildingQuickHudHeaderRow", root, 30f, 10f);
        buildingQuickHud.HeaderText = CreateHeaderText("Header", headerRow, uiFont, "Location", 21, TextAnchor.MiddleLeft, Color.white);
        buildingQuickHud.HeaderText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        buildingQuickHud.CloseButton = CreateButton("CloseButton", headerRow, uiFont, out buildingQuickHud.CloseButtonText, "X", 12, new Color(0.26f, 0.30f, 0.36f, 1f), Color.white);
        LayoutElement closeLayout = buildingQuickHud.CloseButton.gameObject.AddComponent<LayoutElement>();
        closeLayout.preferredWidth = 28f;
        closeLayout.preferredHeight = 28f;
        buildingQuickHud.CloseButton.onClick.AddListener(() =>
        {
            if (selectedLocation.HasValue)
            {
                LogUiInput($"Quick HUD: closed {locations[selectedLocation.Value].Label}");
            }

            selectedLocation = null;
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

        RectTransform actionRow = CreateLayoutRow("BuildingQuickHudActionRow", root, 34f, 10f);
        buildingQuickHud.ContextButton = CreateButton("ContextButton", actionRow, uiFont, out buildingQuickHud.ContextButtonText, "Open", 13, FleetPrimaryButtonColor, Color.white);
        LayoutElement contextLayout = buildingQuickHud.ContextButton.gameObject.AddComponent<LayoutElement>();
        contextLayout.flexibleWidth = 1f;
        contextLayout.preferredHeight = 34f;
        buildingQuickHud.ContextButton.onClick.AddListener(OpenContextPanelFromBuildingQuickHud);

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
            selectedLocation.HasValue &&
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

        if (!locations.TryGetValue(selectedLocation.Value, out LocationData location))
        {
            buildingQuickHud.CanvasRoot.SetActive(false);
            return;
        }

        buildingQuickHud.HeaderText.text = location.Label;
        string categoryTag = IsProductionLocation(selectedLocation.Value) ? "  [Production]" : "  [Service]";
        buildingQuickHud.TypeText.text = GetSelectedLocationDisplayName(selectedLocation.Value) + categoryTag;
        buildingQuickHud.StatusText.text = GetBuildingQuickStatusText(selectedLocation.Value);
        buildingQuickHud.ResourceText.text = GetBuildingQuickResourceText(selectedLocation.Value);
        buildingQuickHud.ContextButtonText.text = GetBuildingQuickContextButtonText(selectedLocation.Value);
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
            LocationType.Forest           => "Logging area in production",
            LocationType.Sawmill          => "Processing logs into boards",
            LocationType.FurnitureFactory => "Crafting furniture from boards and textile",
            LocationType.Warehouse        => IsLocationOperational(LocationType.Warehouse)
                ? HasActiveTradeRun() &&
                  activeTradeRun.OrderType == TradeOrderType.Buy &&
                  (activeTradeRun.Phase == TradeRunPhase.ReturningToWarehouse || activeTradeRun.Phase == TradeRunPhase.UnloadingAtWarehouse)
                    ? "Receiving imported trade delivery"
                    : "Warehouse operational — resources available"
                : "Finished goods storage",
            LocationType.Motel   => "Drivers rest and idle here",
            LocationType.BusStop => "Roadside bus stop by the edge highway",
            LocationType.Canteen => "Service canteen - visitors pay $10 for meals",
            LocationType.Bar     => "Social hub — idle drivers gather here",
            _                    => string.Empty
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
            LocationType.BusStop    => FormatValueLine("Status", "Waiting bay ready"),
            LocationType.Motel      => GetServiceBuildingQuickResourceText(locationType),
            LocationType.Bar        => GetBarQuickResourceText(),
            LocationType.Canteen    => GetCanteenQuickResourceText(),
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
        return text;
    }

    private string GetWarehouseQuickResourceText()
    {
        locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse);
        int logs      = warehouse != null ? warehouse.LogsStored      : 0;
        int boards    = warehouse != null ? warehouse.BoardsStored    : 0;
        int workers   = warehouse != null ? warehouse.Workers         : 0;
        int fuel      = warehouse != null ? warehouse.FuelStored      : 0;
        int alcohol   = warehouse != null ? warehouse.AlcoholStored   : 0;
        int food      = warehouse != null ? warehouse.FoodStored      : 0;
        return $"{FormatValueLine("Workers",  $"{workers} / 1")}\n" +
               $"{FormatValueLine("Fuel",     $"{fuel} / {WarehouseMaxFuelStorage}")}\n" +
               $"{FormatValueLine("Alcohol",  $"{alcohol} / {WarehouseMaxAlcoholStorage}")}\n" +
               $"{FormatValueLine("Food",     $"{food} / {WarehouseMaxFoodStorage}")}\n" +
               $"{FormatValueLine("Logs",     logs.ToString())}\n" +
               $"{FormatValueLine("Boards",   boards.ToString())}\n" +
               $"{FormatValueLine("Textile",  textileStored.ToString())}\n" +
               $"{FormatValueLine("Furniture", furnitureStored.ToString())}";
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

}
