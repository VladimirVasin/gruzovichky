using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
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
        SetCenteredWindow(windowRect, 560f, 680f, -16f);
        resourcesScreenUi.WindowRoot = windowRect;

        Image windowBg = windowRoot.AddComponent<Image>();
        windowBg.color = DriversScreenTint;
        Outline windowOutline = windowRoot.AddComponent<Outline>();
        windowOutline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        windowOutline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup rootLayout = windowRoot.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(14, 14, 14, 14);
        rootLayout.spacing = 8;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        // Header row: title
        RectTransform headerRow = CreateLayoutRow("ResourcesHeaderRow", windowRoot.transform, 34f, 0f);
        Text resourcesTitleText = CreateHeaderText("ResourcesTitle", headerRow, font, "Resources", 20, TextAnchor.MiddleLeft, Color.white);
        resourcesTitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Tab row: two toggle buttons (mirrors Shifts tab pattern)
        const float ResTabRowHeight   = 36f;
        const float ResPanelHeight    = 500f;
        RectTransform tabRow = CreateLayoutRow("ResourcesTabRow", windowRoot.transform, ResTabRowHeight, 0f);
        LayoutElement tabRowLE = tabRow.GetComponent<LayoutElement>();
        tabRowLE.minHeight     = ResTabRowHeight;
        tabRowLE.flexibleHeight = 0f;
        HorizontalLayoutGroup tabHlg = tabRow.GetComponent<HorizontalLayoutGroup>();
        tabHlg.childForceExpandWidth  = true;
        tabHlg.childForceExpandHeight = true;

        resourcesScreenUi.WarehouseTabBtn = CreateButton("WarehouseTabBtn", tabRow, font, out resourcesScreenUi.WarehouseTabText, "Warehouse", 13, FleetPrimaryButtonColor, Color.white);
        resourcesScreenUi.WarehouseTabText.fontStyle = FontStyle.Bold;
        resourcesScreenUi.WarehouseTabBtn.transition = Selectable.Transition.None;
        resourcesScreenUi.WarehouseTabBtn.onClick.AddListener(() => { isResourcesWarehouseTab = true;  UpdateResourcesScreenUi(); });

        resourcesScreenUi.ProductionTabBtn = CreateButton("ProductionTabBtn", tabRow, font, out resourcesScreenUi.ProductionTabText, "Production", 13, new Color(0.22f, 0.26f, 0.32f, 1f), Color.white);
        resourcesScreenUi.ProductionTabText.fontStyle = FontStyle.Bold;
        resourcesScreenUi.ProductionTabBtn.transition = Selectable.Transition.None;
        resourcesScreenUi.ProductionTabBtn.onClick.AddListener(() => { isResourcesWarehouseTab = false; UpdateResourcesScreenUi(); });

        // --- WAREHOUSE PANEL ---
        GameObject warehousePanel = CreateUiObject("WarehousePanel", windowRoot.transform);
        resourcesScreenUi.WarehousePanel = warehousePanel;
        LayoutElement warehousePanelLE = warehousePanel.AddComponent<LayoutElement>();
        warehousePanelLE.preferredHeight = ResPanelHeight;
        warehousePanelLE.minHeight       = ResPanelHeight;
        warehousePanelLE.flexibleHeight  = 0f;

        FleetCanvasUiFactory.ScrollPanelRefs warehouseScroll = CreateVerticalScrollList(
            "WResourcesScrollView",
            warehousePanel.transform,
            "WResourcesContentRoot",
            6f);
        RectTransform wContentRoot = warehouseScroll.Content;

        CreateResourceSummaryRow(wContentRoot, font, "Logs",      ResourceVisualKind.Logs,      TradeResourceType.Logs,      resourcesScreenUi.WarehouseRows);
        CreateResourceSummaryRow(wContentRoot, font, "Boards",    ResourceVisualKind.Boards,    TradeResourceType.Boards,    resourcesScreenUi.WarehouseRows);
        CreateResourceSummaryRow(wContentRoot, font, "Cotton",    ResourceVisualKind.Cotton,    TradeResourceType.Cotton,    resourcesScreenUi.WarehouseRows);
        CreateResourceSummaryRow(wContentRoot, font, "Textile",   ResourceVisualKind.Textile,   TradeResourceType.Textile,   resourcesScreenUi.WarehouseRows);
        CreateResourceSummaryRow(wContentRoot, font, "Furniture", ResourceVisualKind.Furniture, TradeResourceType.Furniture, resourcesScreenUi.WarehouseRows);

        // --- PRODUCTION PANEL ---
        GameObject productionPanel = CreateUiObject("ProductionPanel", windowRoot.transform);
        resourcesScreenUi.ProductionPanel = productionPanel;
        LayoutElement productionPanelLE = productionPanel.AddComponent<LayoutElement>();
        productionPanelLE.preferredHeight = ResPanelHeight;
        productionPanelLE.minHeight       = ResPanelHeight;
        productionPanelLE.flexibleHeight  = 0f;

        FleetCanvasUiFactory.ScrollPanelRefs productionScroll = CreateVerticalScrollList(
            "PResourcesScrollView",
            productionPanel.transform,
            "PResourcesContentRoot",
            10f);
        RectTransform pContentRoot = productionScroll.Content;

        // Production sections: service buildings no longer expose local resource buffers.
        resourcesScreenUi.ProductionSections.Add(CreateProductionSection(pContentRoot, font, LocationType.Forest,           "Forest",            new[] { (ResourceVisualKind.Logs,      TradeResourceType.Logs,      "Logs")      }));
        resourcesScreenUi.ProductionSections.Add(CreateProductionSection(pContentRoot, font, LocationType.Sawmill,          "Sawmill",           new[] { (ResourceVisualKind.Logs,      TradeResourceType.Logs,      "Logs"),      (ResourceVisualKind.Boards,   TradeResourceType.Boards,   "Boards")    }));
        resourcesScreenUi.ProductionSections.Add(CreateProductionSection(pContentRoot, font, LocationType.FurnitureFactory,  "Furniture Factory", new[] { (ResourceVisualKind.Boards,    TradeResourceType.Boards,    "Boards"),    (ResourceVisualKind.Textile,  TradeResourceType.Textile,  "Textile"),   (ResourceVisualKind.Furniture, TradeResourceType.Furniture, "Furniture") }));
        resourcesScreenUi.ProductionSections.Add(CreateProductionSection(pContentRoot, font, LocationType.Docks,             "Docks",             new[] { (ResourceVisualKind.Logs,      TradeResourceType.Logs,      "Logs"),      (ResourceVisualKind.Boards,   TradeResourceType.Boards,   "Boards"),    (ResourceVisualKind.Cotton,    TradeResourceType.Cotton,    "Cotton"),    (ResourceVisualKind.Textile,   TradeResourceType.Textile,   "Textile"),   (ResourceVisualKind.Furniture, TradeResourceType.Furniture, "Furniture") }));

        // Treasury footer
        RectTransform footerCard = CreateSectionCard(windowRoot.transform, font, "Treasury", out RectTransform footerBody);
        footerCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
        resourcesScreenUi.TreasuryValueText = CreateHeaderText("TreasuryValue", footerBody, font, string.Empty, 16, TextAnchor.MiddleLeft, FleetAccentColor);

        AddOverlayCloseButton(windowRect, font);
        resourcesScreenUi.CanvasRoot.SetActive(false);
        UpdateResourcesScreenUi();
    }

    private ProductionBuildingSectionUi CreateProductionSection(RectTransform parent, Font font, LocationType buildingType, string buildingName, (ResourceVisualKind kind, TradeResourceType resType, string label)[] resources)
    {
        ProductionBuildingSectionUi section = new ProductionBuildingSectionUi { BuildingType = buildingType };

        RectTransform sectionRoot = CreateVerticalStack(
            $"ProdSection_{buildingType}",
            parent,
            new RectOffset(),
            4f,
            addContentSizeFitter: true);
        section.Root = sectionRoot.gameObject;

        // Section header
        RectTransform headerRoot = CreateHorizontalLayoutPanel(
            $"ProdSectionHeader_{buildingType}",
            sectionRoot,
            new Color(0.20f, 0.26f, 0.34f, 1f),
            new RectOffset(),
            0f,
            preferredHeight: 26f,
            addOutline: false);
        section.SectionHeaderText = CreateBodyText($"ProdSectionTitle_{buildingType}", headerRoot, font, buildingName, 12, TextAnchor.MiddleLeft, new Color(0.82f, 0.86f, 0.92f, 1f));
        section.SectionHeaderText.fontStyle = FontStyle.Bold;
        section.SectionHeaderText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

        // Resource rows
        foreach (var (kind, resType, label) in resources)
        {
            CreateResourceSummaryRow(sectionRoot, font, label, kind, resType, section.Rows);
        }

        return section;
    }

    private enum ResourceVisualKind
    {
        Logs,
        Boards,
        Cotton,
        Textile,
        Furniture,
        Alcohol
    }

    private void CreateResourceSummaryRow(RectTransform parent, Font font, string title, ResourceVisualKind iconKind, List<ResourceSummaryRowUi> rows)
    {
        CreateResourceSummaryRow(parent, font, title, iconKind, TradeResourceType.Logs, rows);
    }

    private void CreateResourceSummaryRow(RectTransform parent, Font font, string title, ResourceVisualKind iconKind, TradeResourceType resourceType, List<ResourceSummaryRowUi> rows)
    {
        RectTransform card = CreateHorizontalLayoutPanel(
            $"{title}RowCard",
            parent,
            FleetInsetColor,
            new RectOffset(10, 10, 5, 5),
            10,
            preferredHeight: 42f,
            childAlignment: TextAnchor.MiddleLeft);

        RectTransform iconRoot = CreateUiObject($"{title}IconRoot", card).GetComponent<RectTransform>();
        LayoutElement iconLayout = iconRoot.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 28f;
        iconLayout.preferredHeight = 28f;
        Image iconBackground = iconRoot.gameObject.AddComponent<Image>();
        iconBackground.color = new Color(1f, 1f, 1f, 0.06f);
        DrawResourceIcon(iconRoot, iconKind);

        RectTransform textRoot = CreateVerticalStack(
            $"{title}TextRoot",
            card,
            new RectOffset(),
            1f,
            flexibleWidth: 1f);

        Text nameText = CreateBodyText($"{title}Name", textRoot, font, title, 12, TextAnchor.MiddleLeft, Color.white);
        nameText.fontStyle = FontStyle.Bold;
        nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 15f;

        Text valueText = CreateHeaderText($"{title}Value", textRoot, font, string.Empty, 16, TextAnchor.MiddleLeft, FleetAccentColor);
        valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 19f;

        rows.Add(new ResourceSummaryRowUi
        {
            NameText = nameText,
            ValueText = valueText,
            ResourceType = resourceType,
        });
    }

    private static void DrawResourceIcon(RectTransform parent, ResourceVisualKind iconKind)
    {
        switch (iconKind)
        {
            case ResourceVisualKind.Logs:
                CreateIconBar(parent, new Vector2(18f, 7f), new Vector2(0f, 9f), new Color(0.63f, 0.43f, 0.2f));
                CreateIconBar(parent, new Vector2(20f, 7f), new Vector2(2f, 0f), new Color(0.74f, 0.52f, 0.26f));
                CreateIconBar(parent, new Vector2(18f, 7f), new Vector2(-1f, -9f), new Color(0.56f, 0.36f, 0.16f));
                break;
            case ResourceVisualKind.Boards:
                CreateIconBar(parent, new Vector2(20f, 4f), new Vector2(0f, 8f), new Color(0.86f, 0.72f, 0.45f));
                CreateIconBar(parent, new Vector2(20f, 4f), new Vector2(0f, 1f), new Color(0.81f, 0.67f, 0.4f));
                CreateIconBar(parent, new Vector2(20f, 4f), new Vector2(0f, -6f), new Color(0.76f, 0.61f, 0.35f));
                break;
            case ResourceVisualKind.Cotton:
                CreateIconCircle(parent, 11f, new Vector2(-6f, 2f), new Color(0.97f, 0.97f, 0.95f));
                CreateIconCircle(parent, 10f, new Vector2(4f, 5f), new Color(0.98f, 0.98f, 0.96f));
                CreateIconCircle(parent, 9f, new Vector2(0f, -5f), new Color(0.95f, 0.95f, 0.93f));
                break;
            case ResourceVisualKind.Textile:
                CreateIconBar(parent, new Vector2(22f, 18f), Vector2.zero, new Color(0.72f, 0.84f, 0.95f));
                CreateIconBar(parent, new Vector2(3f, 18f), new Vector2(-6f, 0f), new Color(0.55f, 0.74f, 0.9f));
                CreateIconBar(parent, new Vector2(3f, 18f), new Vector2(2f, 0f), new Color(0.55f, 0.74f, 0.9f));
                CreateIconBar(parent, new Vector2(3f, 18f), new Vector2(10f, 0f), new Color(0.55f, 0.74f, 0.9f));
                break;
            case ResourceVisualKind.Furniture:
                CreateIconBar(parent, new Vector2(18f, 4f), new Vector2(0f, 7f), new Color(0.78f, 0.56f, 0.3f));
                CreateIconBar(parent, new Vector2(14f, 4f), new Vector2(0f, -1f), new Color(0.72f, 0.5f, 0.25f));
                CreateIconBar(parent, new Vector2(3f, 10f), new Vector2(-6f, -8f), new Color(0.58f, 0.39f, 0.18f));
                CreateIconBar(parent, new Vector2(3f, 10f), new Vector2(6f, -8f), new Color(0.58f, 0.39f, 0.18f));
                break;
            case ResourceVisualKind.Alcohol:
                CreateIconBar(parent, new Vector2(8f, 16f), new Vector2(0f, -4f), new Color(0.38f, 0.18f, 0.08f));
                CreateIconBar(parent, new Vector2(5f, 7f), new Vector2(0f, 7f), new Color(0.18f, 0.42f, 0.22f));
                CreateIconBar(parent, new Vector2(10f, 3f), new Vector2(0f, -2f), new Color(0.82f, 0.60f, 0.22f));
                CreateIconBar(parent, new Vector2(6f, 3f), new Vector2(0f, -8f), new Color(0.92f, 0.72f, 0.30f));
                break;
        }
    }

    private static void CreateIconBar(RectTransform parent, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        RectTransform rect = CreateUiObject("IconBar", parent).GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
    }

    private static void CreateIconCircle(RectTransform parent, float diameter, Vector2 anchoredPosition, Color color)
    {
        RectTransform rect = CreateUiObject("IconCircle", parent).GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(diameter, diameter);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
    }

    private void UpdateResourcesScreenUi()
    {
        if (resourcesScreenUi == null) return;

        bool shouldShow = isResourcesPanelOpen;
        bool forceLayoutRebuild = false;
        if (resourcesScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            resourcesScreenUi.CanvasRoot.SetActive(shouldShow);
            forceLayoutRebuild = shouldShow;
        }

        if (!shouldShow) return;

        bool ru = IsRussianLanguage();

        // Tab panel visibility
        resourcesScreenUi.WarehousePanel.SetActive(isResourcesWarehouseTab);
        resourcesScreenUi.ProductionPanel.SetActive(!isResourcesWarehouseTab);

        // Tab button highlight
        Color activeTabColor   = FleetPrimaryButtonColor;
        Color inactiveTabColor = new Color(0.16f, 0.21f, 0.28f, 1f);
        Color activeTextColor   = Color.white;
        Color inactiveTextColor = FleetSecondaryTextColor;

        resourcesScreenUi.WarehouseTabBtn .GetComponent<Image>().color = isResourcesWarehouseTab  ? activeTabColor : inactiveTabColor;
        resourcesScreenUi.ProductionTabBtn.GetComponent<Image>().color = !isResourcesWarehouseTab ? activeTabColor : inactiveTabColor;
        resourcesScreenUi.WarehouseTabText .color = isResourcesWarehouseTab  ? activeTextColor : inactiveTextColor;
        resourcesScreenUi.ProductionTabText.color = !isResourcesWarehouseTab ? activeTextColor : inactiveTextColor;

        resourcesScreenUi.WarehouseTabText.text  = ru ? "На складе"    : "Warehouse";
        resourcesScreenUi.ProductionTabText.text = ru ? "Производство" : "Production";

        if (isResourcesWarehouseTab)
        {
            locations.TryGetValue(LocationType.Warehouse, out LocationData warehouseData);
            string[] resourceNames =
            {
                ru ? "Брёвна"    : "Logs",
                ru ? "Доски"     : "Boards",
                ru ? "Хлопок"    : "Cotton",
                ru ? "Ткань"     : "Textile",
                ru ? "Мебель"    : "Furniture"
            };
            string[] resourceValues =
            {
                (warehouseData?.LogsStored ?? 0).ToString(),
                (warehouseData?.BoardsStored ?? 0).ToString(),
                cottonStored.ToString(),
                textileStored.ToString(),
                furnitureStored.ToString()
            };

            for (int i = 0; i < resourcesScreenUi.WarehouseRows.Count && i < resourceNames.Length; i++)
            {
                ResourceSummaryRowUi row = resourcesScreenUi.WarehouseRows[i];
                if (row.LastName != resourceNames[i])
                {
                    row.NameText.text = resourceNames[i];
                    row.LastName = resourceNames[i];
                    forceLayoutRebuild = true;
                }
                if (row.LastValue != resourceValues[i])
                {
                    row.ValueText.text = resourceValues[i];
                    row.LastValue = resourceValues[i];
                    forceLayoutRebuild = true;
                }
            }
        }
        else
        {
            foreach (ProductionBuildingSectionUi section in resourcesScreenUi.ProductionSections)
            {
                bool exists = locations.TryGetValue(section.BuildingType, out LocationData bData);
                section.Root.SetActive(true);

                string headerName = section.BuildingType switch
                {
                    LocationType.Forest          => ru ? "Лесозаготовка"    : "Lumberyard",
                    LocationType.Sawmill         => ru ? "Лесопилка"        : "Sawmill",
                    LocationType.FurnitureFactory => ru ? "Мебельный завод"  : "Furniture Factory",
                    LocationType.GasStation      => ru ? "Заправка"         : "Gas Station",
                    _ => section.BuildingType.ToString()
                };
                if (!exists) headerName += ru ? " (не построено)" : " (not built)";
                section.SectionHeaderText.text = headerName;

                foreach (ResourceSummaryRowUi row in section.Rows)
                {
                    string rowName = row.ResourceType switch
                    {
                        TradeResourceType.Logs      => ru ? "Брёвна"  : "Logs",
                        TradeResourceType.Boards    => ru ? "Доски"   : "Boards",
                        TradeResourceType.Textile   => ru ? "Ткань"   : "Textile",
                        TradeResourceType.Furniture => ru ? "Мебель"  : "Furniture",
                        _ => row.NameText.text
                    };
                    string rowValue = exists ? GetProductionBuildingResourceValue(bData, section.BuildingType, row.ResourceType) : "—";

                    if (row.LastName != rowName) { row.NameText.text = rowName; row.LastName = rowName; forceLayoutRebuild = true; }
                    if (row.LastValue != rowValue) { row.ValueText.text = rowValue; row.LastValue = rowValue; forceLayoutRebuild = true; }
                }
            }
        }

        string treasuryValue = $"${money}";
        if (resourcesScreenUi.LastTreasuryValue != treasuryValue)
        {
            resourcesScreenUi.TreasuryValueText.text = treasuryValue;
            resourcesScreenUi.LastTreasuryValue = treasuryValue;
            forceLayoutRebuild = true;
        }

        if (forceLayoutRebuild)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(resourcesScreenUi.WindowRoot);
        }
    }

    private string GetProductionBuildingResourceValue(LocationData bData, LocationType buildingType, TradeResourceType resType)
    {
        return resType switch
        {
            TradeResourceType.Logs      => bData.LogsStored.ToString(),
            TradeResourceType.Boards    => bData.BoardsStored.ToString(),
            TradeResourceType.Cotton    => bData.CottonStored.ToString(),
            TradeResourceType.Textile   => bData.TextileStored.ToString(),
            TradeResourceType.Furniture => bData.FurnitureStored.ToString(),
            _ => "0"
        };
    }

    private int GetTotalLogsResourceAmount()
    {
        int total = 0;
        if (locations.TryGetValue(LocationType.Forest, out LocationData forest))
            total += forest.LogsStored;
        if (locations.TryGetValue(LocationType.Sawmill, out LocationData sawmill))
            total += sawmill.LogsStored;
        if (locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse))
            total += warehouse.LogsStored;
        if (locations.TryGetValue(LocationType.Docks, out LocationData docks))
            total += docks.LogsStored;
        foreach (TruckAgent truck in truckAgents)
        {
            if (truck.TruckCargoType == CargoType.Logs && !IsTruckOnActiveTradeSellRun(truck))
                total += truck.TruckCargoAmount;
        }
        return total;
    }

    private int GetTotalBoardsResourceAmount()
    {
        int total = 0;
        if (locations.TryGetValue(LocationType.Sawmill, out LocationData sawmill))
            total += sawmill.BoardsStored;
        if (locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse))
            total += warehouse.BoardsStored;
        if (locations.TryGetValue(LocationType.FurnitureFactory, out LocationData furnitureFactory))
            total += furnitureFactory.BoardsStored;
        if (locations.TryGetValue(LocationType.Docks, out LocationData docks))
            total += docks.BoardsStored;
        foreach (TruckAgent truck in truckAgents)
        {
            if (truck.TruckCargoType == CargoType.Boards && !IsTruckOnActiveTradeSellRun(truck))
                total += truck.TruckCargoAmount;
        }
        return total;
    }

    private bool IsTruckOnActiveTradeSellRun(TruckAgent truck)
    {
        return HasActiveTradeRun() &&
               activeTradeRun.OrderType == TradeOrderType.Sell &&
               activeTradeRun.TruckNumber == truck.TruckNumber;
    }

    private int GetTotalTextileResourceAmount()
    {
        int total = textileStored;
        if (locations.TryGetValue(LocationType.FurnitureFactory, out LocationData furnitureFactory))
            total += furnitureFactory.TextileStored;
        if (locations.TryGetValue(LocationType.Docks, out LocationData docks))
            total += docks.TextileStored;
        foreach (TruckAgent truck in truckAgents)
        {
            if (truck.TruckCargoType == CargoType.Textile && !IsTruckOnActiveTradeSellRun(truck))
                total += truck.TruckCargoAmount;
        }
        return total;
    }

    private int GetTotalFurnitureResourceAmount()
    {
        int total = furnitureStored;
        if (locations.TryGetValue(LocationType.FurnitureFactory, out LocationData furnitureFactory))
            total += furnitureFactory.FurnitureStored;
        if (locations.TryGetValue(LocationType.Docks, out LocationData docks))
            total += docks.FurnitureStored;
        foreach (TruckAgent truck in truckAgents)
        {
            if (truck.TruckCargoType == CargoType.Furniture && !IsTruckOnActiveTradeSellRun(truck))
                total += truck.TruckCargoAmount;
        }
        return total;
    }

    private static readonly TradeResourceType[] TradeHudResources =
    {
        TradeResourceType.Logs,
        TradeResourceType.Boards,
        TradeResourceType.Cotton,
        TradeResourceType.Textile,
        TradeResourceType.Furniture,
    };

}
