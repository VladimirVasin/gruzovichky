using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static Sprite s_regionalWorldMapSprite;

    private void SetupWorldMapScreenUi()
    {
        if (worldMapScreenUi != null) return;

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        worldMapScreenUi = new WorldMapScreenUiRefs();

        GameObject canvasObject = new("WorldMapScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        worldMapScreenUi.CanvasRoot = canvasObject;

        // Fullscreen backdrop sits directly on the canvas, covering everything below.
        GameObject backdropGo = CreateUiObject("WorldMapBackdrop", canvasObject.transform);
        RectTransform backdropRect = backdropGo.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.sizeDelta = Vector2.zero;
        backdropGo.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.09f, 0.96f);

        GameObject windowRoot = CreateUiObject("WorldMapWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        windowRect.anchorMin = Vector2.zero;
        windowRect.anchorMax = Vector2.one;
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.anchoredPosition = Vector2.zero;
        windowRect.sizeDelta = Vector2.zero;
        worldMapScreenUi.WindowRoot = windowRect;

        VerticalLayoutGroup rootLayout = windowRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(28, 28, 28, 28);
        rootLayout.spacing = 16;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("WorldMapHeaderRow", windowRoot.transform, 44f, 0f);
        worldMapScreenUi.TitleText = CreateHeaderText("WorldMapTitle", headerRow, font, "Regional Map", 24, TextAnchor.MiddleLeft, Color.white);
        worldMapScreenUi.TitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        worldMapScreenUi.SubtitleText = CreateBodyText("WorldMapSubtitle", headerRow, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        RectTransform contentRow = CreateUiObject("WorldMapContentRow", windowRoot.transform).GetComponent<RectTransform>();
        LayoutElement contentLayout = contentRow.gameObject.AddComponent<LayoutElement>();
        contentLayout.flexibleHeight = 1f;
        HorizontalLayoutGroup contentGroup = contentRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        contentGroup.spacing = 16f;
        contentGroup.childControlWidth = true;
        contentGroup.childControlHeight = true;
        contentGroup.childForceExpandWidth = true;
        contentGroup.childForceExpandHeight = true;

        RectTransform mapCard = CreateSectionCard(contentRow, font, "Regional World", out RectTransform mapBody);
        LayoutElement mapCardLayout = mapCard.gameObject.AddComponent<LayoutElement>();
        mapCardLayout.preferredWidth = 1120f;
        mapCardLayout.flexibleWidth = 1f;
        mapCardLayout.flexibleHeight = 1f;

        worldMapScreenUi.SelectionHintText = CreateBodyText(
            "WorldMapHint",
            mapBody,
            font,
            "Click a city on the regional map to inspect trade options.",
            13,
            TextAnchor.MiddleLeft,
            FleetSecondaryTextColor);

        RectTransform mapFrame = CreateStyledPanel("WorldMapFrame", mapBody, new Color(0.18f, 0.13f, 0.07f, 0.96f));
        LayoutElement mapFrameLayout = mapFrame.gameObject.AddComponent<LayoutElement>();
        mapFrameLayout.flexibleHeight = 1f;
        VerticalLayoutGroup mapFrameLayoutGroup = mapFrame.gameObject.AddComponent<VerticalLayoutGroup>();
        mapFrameLayoutGroup.padding = new RectOffset(16, 16, 16, 16);
        mapFrameLayoutGroup.spacing = 10;
        mapFrameLayoutGroup.childControlWidth = true;
        mapFrameLayoutGroup.childControlHeight = true;
        mapFrameLayoutGroup.childForceExpandWidth = true;
        mapFrameLayoutGroup.childForceExpandHeight = true;

        RectTransform mapSurface = CreateUiObject("WorldMapSurface", mapFrame).GetComponent<RectTransform>();
        LayoutElement mapSurfaceLayout = mapSurface.gameObject.AddComponent<LayoutElement>();
        mapSurfaceLayout.flexibleHeight = 1f;
        Image mapSurfaceBackground = mapSurface.gameObject.AddComponent<Image>();
        mapSurfaceBackground.sprite = GetRegionalWorldMapSprite();
        mapSurfaceBackground.type = Image.Type.Simple;
        mapSurfaceBackground.color = Color.white;

        RectTransform mapRoot = CreateUiObject("RegionalWorldMapRoot", mapSurface).GetComponent<RectTransform>();
        StretchRect(mapRoot, 0f, 0f, 0f, 0f);
        worldMapScreenUi.MapRoot = mapRoot;

        CreateWorldMapGeography(mapRoot);

        for (int regionIndex = 0; regionIndex < 9; regionIndex++)
        {
            Image routeLine = CreateWorldMapRouteLine(mapRoot, regionIndex);
            worldMapScreenUi.RegionRouteLines.Add(routeLine);
        }

        for (int regionIndex = 0; regionIndex < 9; regionIndex++)
        {
            worldMapScreenUi.Cells.Add(CreateWorldMapCityMarker(mapRoot, font, regionIndex));
        }

        RectTransform detailsCard = CreateSectionCard(contentRow, font, "Region Preview", out RectTransform detailsBody);
        worldMapScreenUi.DetailsPanelRoot = detailsCard.gameObject;
        LayoutElement detailsCardLayout = detailsCard.gameObject.AddComponent<LayoutElement>();
        detailsCardLayout.preferredWidth = 448f;
        detailsCardLayout.flexibleWidth = 0f;
        detailsCardLayout.flexibleHeight = 1f;

        RectTransform previewContainer = CreateStyledPanel("WorldMapDetailPreviewContainer", detailsBody, new Color(0.12f, 0.15f, 0.20f, 0.98f));
        LayoutElement previewContainerLayout = previewContainer.gameObject.AddComponent<LayoutElement>();
        previewContainerLayout.flexibleHeight = 1f;
        previewContainerLayout.minHeight = 200f;
        worldMapScreenUi.DetailPreview = CreateWorldMapDetailPreview(previewContainer, font);

        RectTransform infoPanel = CreateStyledPanel("WorldMapDetailInfoPanel", detailsBody, FleetCardMutedColor);
        infoPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 230f;
        VerticalLayoutGroup infoLayout = infoPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        infoLayout.padding = new RectOffset(14, 14, 12, 12);
        infoLayout.spacing = 6f;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;

        worldMapScreenUi.DetailsNameText = CreateHeaderText("WorldMapDetailsName", infoPanel, font, string.Empty, 22, TextAnchor.MiddleLeft, Color.white);
        worldMapScreenUi.DetailsStatusText = CreateBodyText("WorldMapDetailsStatus", infoPanel, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetMutedTextColor);
        worldMapScreenUi.DetailsSellsLabelText = CreateHeaderText("WorldMapResourcesLabel", infoPanel, font, "Sells", 11, TextAnchor.MiddleLeft, FleetMutedTextColor);
        worldMapScreenUi.DetailsResourcesText = CreateHeaderText("WorldMapDetailsResources", infoPanel, font, string.Empty, 17, TextAnchor.MiddleLeft, FleetAccentColor);
        worldMapScreenUi.DetailsBuysLabelText = CreateHeaderText("WorldMapImportsLabel", infoPanel, font, "Buys", 11, TextAnchor.MiddleLeft, FleetMutedTextColor);
        worldMapScreenUi.DetailsImportsText = CreateHeaderText("WorldMapDetailsImports", infoPanel, font, string.Empty, 17, TextAnchor.MiddleLeft, FleetAccentColor);
        worldMapScreenUi.DetailsDescriptionText = CreateBodyText("WorldMapDetailsDescription", infoPanel, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        worldMapScreenUi.DetailsDescriptionText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        // Region-scoped trade routes live inside the right region panel.
        RectTransform routeCard = CreateSectionCard(detailsBody, font, string.Empty, out RectTransform routeBody);
        routeCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 122f;
        worldMapScreenUi.RoutePanelRoot = routeCard.gameObject;

        VerticalLayoutGroup routeBodyLayout = routeBody.GetComponent<VerticalLayoutGroup>() ?? routeBody.gameObject.AddComponent<VerticalLayoutGroup>();
        routeBodyLayout.spacing = 6f;
        routeBodyLayout.childControlWidth  = true;
        routeBodyLayout.childControlHeight = true;
        routeBodyLayout.childForceExpandWidth  = true;
        routeBodyLayout.childForceExpandHeight = false;

        // title row: label + orders
        RectTransform routeTitleRow = CreateLayoutRow("RouteTitleRow", routeBody, 22f, 8f);
        worldMapScreenUi.RoutePanelTitleText = CreateHeaderText("RoutePanelTitle", routeTitleRow, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetMutedTextColor);
        worldMapScreenUi.RoutePanelTitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // existing order chips row
        GameObject ordersRowGo = CreateUiObject("RouteOrdersRow", routeBody);
        RectTransform ordersRow = ordersRowGo.GetComponent<RectTransform>();
        HorizontalLayoutGroup ordersLayout = ordersRowGo.AddComponent<HorizontalLayoutGroup>();
        ordersLayout.spacing = 6f;
        ordersLayout.childAlignment   = TextAnchor.MiddleLeft;
        ordersLayout.childControlWidth  = false;
        ordersLayout.childControlHeight = false;
        ordersLayout.childForceExpandWidth  = false;
        ordersLayout.childForceExpandHeight = false;
        ordersRowGo.AddComponent<LayoutElement>().preferredHeight = 28f;
        worldMapScreenUi.RouteOrdersRow = ordersRow;

        // add-order form row
        RectTransform formRow = CreateLayoutRow("RouteFormRow", routeBody, 30f, 6f);

        // resource button
        Button resBtn = CreateButton("RouteResBtn", formRow, font, out Text resBtnTxt, string.Empty, 12, new Color(0.20f, 0.24f, 0.30f), Color.white);
        worldMapScreenUi.RouteResourceLabel = resBtnTxt;
        resBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 100f;
        resBtn.onClick.AddListener(CycleWorldMapRouteResource);

        // amount minus
        Button amtMinus = CreateButton("RouteAmtMinus", formRow, font, out _, "-", 13, new Color(0.20f, 0.24f, 0.30f), Color.white);
        amtMinus.gameObject.AddComponent<LayoutElement>().preferredWidth = 28f;
        amtMinus.onClick.AddListener(() => { worldMapRouteAmount = Mathf.Max(1, worldMapRouteAmount - 1); isWorldMapScreenDirty = true; });

        Text amtLabel = CreateBodyText("RouteAmtLabel", formRow, font, string.Empty, 13, TextAnchor.MiddleCenter, Color.white);
        amtLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 22f;
        worldMapScreenUi.RouteAmountLabel = amtLabel;

        // amount plus
        Button amtPlus = CreateButton("RouteAmtPlus", formRow, font, out _, "+", 13, new Color(0.20f, 0.24f, 0.30f), Color.white);
        amtPlus.gameObject.AddComponent<LayoutElement>().preferredWidth = 28f;
        amtPlus.onClick.AddListener(() => { worldMapRouteAmount = Mathf.Min(5, worldMapRouteAmount + 1); isWorldMapScreenDirty = true; });

        // buy/sell toggle
        worldMapScreenUi.RouteTypeButton = CreateButton("RouteTypeBtn", formRow, font, out worldMapScreenUi.RouteTypeButtonText, string.Empty, 12, FleetPrimaryButtonColor, Color.white);
        worldMapScreenUi.RouteTypeButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;
        worldMapScreenUi.RouteTypeButton.onClick.AddListener(ToggleWorldMapRouteOrderType);

        // place order button
        worldMapScreenUi.RoutePlaceButton = CreateButton("RoutePlaceBtn", formRow, font, out _, "+", 16, new Color(0.18f, 0.42f, 0.22f), Color.white);
        worldMapScreenUi.RoutePlaceButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 34f;
        worldMapScreenUi.RoutePlaceButton.onClick.AddListener(PlaceWorldMapRouteOrder);

        AddOverlayCloseButton(windowRect, font);
        worldMapScreenUi.CanvasRoot.SetActive(false);
        UpdateWorldMapScreenUi();
    }

    private void SelectWorldMapRegion(int regionIndex)
    {
        selectedWorldMapRegionIndex = Mathf.Clamp(regionIndex, 0, 8);
        isWorldMapScreenDirty = true;
        PlayUiSound(uiSelectClip, 0.82f);
    }

    private static string GetWorldMapRegionName(int regionIndex)
    {
        return regionIndex switch
        {
            0 => "North Ridge",
            1 => "Forest Belt",
            2 => "River Port",
            3 => "Barren Flats",
            4 => "Your Town",
            5 => "Cotton & Textile Belt",
            6 => "Dry South",
            7 => "Freight Steppe",
            8 => "Coastal Gate",
            _ => "Unknown Region"
        };
    }

    private string GetWorldMapRegionDisplayName(int regionIndex)
    {
        if (!IsRussianLanguage())
            return GetWorldMapRegionName(regionIndex);

        return regionIndex switch
        {
            0 => "Северный кряж",
            1 => "Лесной пояс",
            2 => "Речной порт",
            3 => "Пустоши",
            4 => "Твой город",
            5 => "Хлопково-текстильный пояс",
            6 => "Сухой юг",
            7 => "Грузовая степь",
            8 => "Прибрежные ворота",
            _ => "Неизвестный регион"
        };
    }

    private static string GetWorldMapRegionTypeLabel(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "Current region",
            2 or 5 or 6 => "Neighbor region",
            _ => "Empty region slot"
        };
    }

    private string GetWorldMapRegionTypeDisplayLabel(int regionIndex)
    {
        if (!IsRussianLanguage())
            return GetWorldMapRegionTypeLabel(regionIndex);

        return regionIndex switch
        {
            4 => "Текущий регион",
            2 or 5 or 6 => "Соседний регион",
            _ => "Пустой слот региона"
        };
    }

    private static string GetWorldMapRegionProducedResources(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "Logs, Boards, Furniture",
            5 => "Cotton, Textile",
            6 => "Boards market",
            2 => "Trade logistics",
            _ => "No confirmed survey data"
        };
    }

    private string GetWorldMapRegionProducedResourcesDisplay(int regionIndex)
    {
        if (!IsRussianLanguage())
            return GetWorldMapRegionProducedResources(regionIndex);

        return regionIndex switch
        {
            4 => "Брёвна, доски, мебель",
            5 => "Хлопок, текстиль",
            6 => "Зерно, алкоголь",
            2 => "Торговая логистика",
            _ => "Нет подтверждённых данных"
        };
    }

    private static string GetWorldMapRegionImportedResources(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "Cotton, Textile",
            5 => "—",
            6 => "Boards",
            2 => "—",
            _ => "—"
        };
    }

    private string GetWorldMapRegionImportedResourcesDisplay(int regionIndex)
    {
        if (!IsRussianLanguage())
            return GetWorldMapRegionImportedResources(regionIndex);

        return regionIndex switch
        {
            4 => "Хлопок, текстиль, топливо, алкоголь, еда",
            5 => "—",
            6 => "Доски",
            2 => "—",
            _ => "—"
        };
    }

    private static string GetWorldMapRegionDescription(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "This is your active simulation region. It contains the current town, highways, production buildings, and local roads.",
            5 => "A combined agricultural and industrial belt. Raw cotton is grown here and processed into textile on-site, making it the primary external source for both resources.",
            6 => "A hot, arid territory dominated by grain farms and distilleries. The region exports alcohol and raw grain, and relies on outside supply of construction materials.",
            2 => "A schematic route hub near the river corridor, reserved for future logistics and regional expansion passes.",
            _ => "This region exists on the wider map, but it has not been fully designed or assigned concrete production data yet."
        };
    }

    private string GetWorldMapRegionDescriptionDisplay(int regionIndex)
    {
        if (!IsRussianLanguage())
            return GetWorldMapRegionDescription(regionIndex);

        return regionIndex switch
        {
            4 => "Это текущий игровой регион: город, магистраль, дороги, производство и все местные проблемы.",
            5 => "Сельскохозяйственно-промышленный пояс. Здесь выращивают хлопок и делают текстиль, поэтому регион важен для внешних закупок.",
            6 => "Сухая территория с фермами и винокурнями. Экспортирует алкоголь и зерно, но нуждается в строительных материалах.",
            2 => "Речной транспортный узел. Сейчас это схематичный маршрутный регион для будущего расширения торговли.",
            _ => "Регион есть на глобальной карте, но пока не разведан и не имеет подробной производственной схемы."
        };
    }

    private static bool IsWorldMapRegionKnown(int regionIndex)
    {
        return regionIndex == 2 || regionIndex == 4 || regionIndex == 5 || regionIndex == 6;
    }

    private void UpdateWorldMapScreenUi()
    {
        if (worldMapScreenUi == null) return;

        bool shouldShow = isWorldMapPanelOpen;
        if (worldMapScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            worldMapScreenUi.CanvasRoot.SetActive(shouldShow);
            isWorldMapScreenDirty = true;
        }

        if (!shouldShow || !isWorldMapScreenDirty)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        worldMapScreenUi.TitleText.text = ru ? "Карта регионов" : "Regional Map";
        worldMapScreenUi.SubtitleText.text = ru ? "Открыть/закрыть: M" : "Open/Close: M";
        worldMapScreenUi.SelectionHintText.text = ru
            ? "Каждая клетка — схематичная карта региона большого мира. Известные соседи уже нанесены на карту."
            : "Each cell is a schematic map of a wider-world region. Known neighbors are already sketched.";
        worldMapScreenUi.SelectionHintText.text = ru
            ? "\u041d\u0430\u0436\u043c\u0438 \u043d\u0430 \u0433\u043e\u0440\u043e\u0434 \u043d\u0430 \u0433\u043b\u043e\u0431\u0430\u043b\u044c\u043d\u043e\u0439 \u043a\u0430\u0440\u0442\u0435, \u0447\u0442\u043e\u0431\u044b \u043f\u043e\u0441\u043c\u043e\u0442\u0440\u0435\u0442\u044c, \u0447\u0442\u043e \u043e\u043d \u043f\u0440\u043e\u0434\u0430\u0451\u0442 \u0438 \u043f\u043e\u043a\u0443\u043f\u0430\u0435\u0442."
            : "Click a city on the regional map to inspect what it sells, buys, and which routes are active.";
        if (worldMapScreenUi.DetailsSellsLabelText != null)
            worldMapScreenUi.DetailsSellsLabelText.text = ru ? "\u041f\u0440\u043e\u0434\u0430\u0451\u0442" : "Sells";
        if (worldMapScreenUi.DetailsBuysLabelText != null)
            worldMapScreenUi.DetailsBuysLabelText.text = ru ? "\u041f\u043e\u043a\u0443\u043f\u0430\u0435\u0442" : "Buys";

        for (int i = 0; i < worldMapScreenUi.Cells.Count; i++)
        {
            WorldMapCellUi cell = worldMapScreenUi.Cells[i];
            bool hasSelection = selectedWorldMapRegionIndex >= 0;
            bool isSelected = hasSelection && i == selectedWorldMapRegionIndex;
            bool isCurrent = i == 4;
            bool isKnown = IsWorldMapRegionKnown(i);

            cell.NameText.text = GetWorldMapRegionDisplayName(i);
            cell.TypeText.text = GetWorldMapRegionTypeDisplayLabel(i);
            cell.Background.color = isSelected
                ? new Color(0.66f, 0.43f, 0.12f, 0.86f)
                : isCurrent
                    ? new Color(0.43f, 0.29f, 0.08f, 0.74f)
                    : isKnown
                        ? new Color(0.16f, 0.12f, 0.07f, 0.58f)
                        : new Color(0.10f, 0.08f, 0.06f, 0.34f);
            cell.NameText.color = isKnown || isCurrent ? new Color(1f, 0.94f, 0.78f, 1f) : new Color(0.78f, 0.70f, 0.55f, 1f);
            cell.TypeText.color = isSelected ? new Color(1f, 0.82f, 0.28f, 1f) : new Color(0.68f, 0.61f, 0.48f, 1f);
            if (cell.PreviewBackground != null)
            {
                ApplyWorldMapCellPreview(cell, i, isKnown);
            }
            if (cell.Outline != null)
            {
                cell.Outline.effectColor = isSelected
                    ? new Color(1f, 0.75f, 0.22f, 0.86f)
                    : new Color(0f, 0f, 0f, 0.22f);
                cell.Outline.effectDistance = isSelected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            }

            if (cell.RouteStatusDot != null)
            {
                bool isNeighborCell = !isCurrent && isKnown;
                bool hasRoute = isNeighborCell && HasRegionTradeRoute(i);
                cell.RouteStatusDot.color = hasRoute
                    ? new Color(0.3f, 0.85f, 0.45f, 1f)
                    : isNeighborCell
                        ? new Color(0.50f, 0.58f, 0.66f, 0.65f)
                        : new Color(0f, 0f, 0f, 0f);
            }
        }

        for (int i = 0; i < worldMapScreenUi.RegionRouteLines.Count; i++)
        {
            if (i == 4)
            {
                UpdateWorldMapRouteLine(worldMapScreenUi.RegionRouteLines[i], worldMapScreenUi.MapRoot, i, false, Color.clear);
                continue;
            }

            bool routeRegionKnown = IsWorldMapRegionKnown(i);
            bool hasRoute = routeRegionKnown && HasRegionTradeRoute(i);
            bool isSelectedRoute = selectedWorldMapRegionIndex >= 0 && routeRegionKnown && i == selectedWorldMapRegionIndex;
            Color lineColor = hasRoute
                ? new Color(0.26f, 0.85f, 0.42f, 0.78f)
                : new Color(FleetAccentColor.r, FleetAccentColor.g, FleetAccentColor.b, 0.38f);
            UpdateWorldMapRouteLine(worldMapScreenUi.RegionRouteLines[i], worldMapScreenUi.MapRoot, i, hasRoute || isSelectedRoute, lineColor);
        }

        bool hasSelectedRegion = selectedWorldMapRegionIndex >= 0;
        int selected = hasSelectedRegion ? Mathf.Clamp(selectedWorldMapRegionIndex, 0, 8) : -1;
        bool detailHasPreview = hasSelectedRegion && IsWorldMapRegionKnown(selected);

        if (worldMapScreenUi.DetailsPanelRoot != null)
        {
            worldMapScreenUi.DetailsPanelRoot.SetActive(hasSelectedRegion);
        }

        if (hasSelectedRegion && worldMapScreenUi.DetailPreview != null)
        {
            ApplyWorldMapDetailPreview(worldMapScreenUi.DetailPreview, selected, detailHasPreview);
        }

        if (hasSelectedRegion)
        {
            worldMapScreenUi.DetailsNameText.text = GetWorldMapRegionDisplayName(selected);
            worldMapScreenUi.DetailsStatusText.text = GetWorldMapRegionTypeDisplayLabel(selected);
            worldMapScreenUi.DetailsResourcesText.text = GetWorldMapRegionProducedResourcesDisplay(selected);
            worldMapScreenUi.DetailsImportsText.text = GetWorldMapRegionImportedResourcesDisplay(selected);
            worldMapScreenUi.DetailsDescriptionText.text = GetWorldMapRegionDescriptionDisplay(selected);
        }

        // ── Route panel ────────────────────────────────────────────────────
        bool isNeighbor = hasSelectedRegion && IsWorldMapRegionKnown(selected) && selected != 4;
        worldMapScreenUi.RoutePanelRoot.SetActive(isNeighbor);

        if (isNeighbor)
        {
            worldMapScreenUi.RoutePanelTitleText.text = (ru ? "Торговые маршруты: " : "Trade Routes: ") + GetWorldMapRegionDisplayName(selected);

            // Clamp form resource to catalog
            (TradeResourceType[] buyable, TradeResourceType[] sellable) = GetRegionTradeCatalog(selected);
            TradeResourceType[] catalog = worldMapRouteOrderType == TradeOrderType.Buy ? buyable : sellable;
            if (catalog.Length == 0)
            {
                worldMapRouteOrderType = worldMapRouteOrderType == TradeOrderType.Buy ? TradeOrderType.Sell : TradeOrderType.Buy;
                catalog = worldMapRouteOrderType == TradeOrderType.Buy ? buyable : sellable;
            }
            if (catalog.Length > 0 && System.Array.IndexOf(catalog, worldMapRouteResource) < 0)
                worldMapRouteResource = catalog[0];

            worldMapScreenUi.RouteResourceLabel.text = GetTradeResourceShortLabel(worldMapRouteResource);
            worldMapScreenUi.RouteAmountLabel.text   = worldMapRouteAmount.ToString();
            worldMapScreenUi.RouteTypeButtonText.text = worldMapRouteOrderType == TradeOrderType.Buy
                ? (ru ? "КУПИТЬ" : "BUY")
                : (ru ? "ПРОДАТЬ" : "SELL");
            worldMapScreenUi.RouteTypeButton.image.color = worldMapRouteOrderType == TradeOrderType.Buy
                ? new Color(0.15f, 0.38f, 0.20f)
                : new Color(0.42f, 0.14f, 0.14f);
            worldMapScreenUi.RoutePlaceButton.interactable = catalog.Length > 0;

            RefreshWorldMapRouteRows(selected);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(worldMapScreenUi.WindowRoot);
        LocalizeCanvas(worldMapScreenUi.CanvasRoot);
        isWorldMapScreenDirty = false;
    }

    private void RefreshWorldMapRouteRows(int regionIndex)
    {
        // destroy old chips
        foreach (WorldMapRouteRowUi row in worldMapScreenUi.RouteRows)
        {
            if (row.Root != null) Destroy(row.Root);
        }
        worldMapScreenUi.RouteRows.Clear();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bool ru = IsRussianLanguage();

        foreach (TradeHudOrder order in activeTradeHudOrders)
        {
            if (order.TargetRegionIndex != regionIndex) continue;

            WorldMapRouteRowUi chip = new WorldMapRouteRowUi { OrderId = order.Id };

            GameObject chipGo = CreateUiObject($"RouteChip_{order.Id}", worldMapScreenUi.RouteOrdersRow);
            chip.Root = chipGo;
            RectTransform chipRect = chipGo.GetComponent<RectTransform>();
            Image chipBg = chipGo.AddComponent<Image>();
            chipBg.color = new Color(0.16f, 0.20f, 0.26f);
            HorizontalLayoutGroup chipLayout = chipGo.AddComponent<HorizontalLayoutGroup>();
            chipLayout.padding  = new RectOffset(6, 4, 2, 2);
            chipLayout.spacing  = 4f;
            chipLayout.childAlignment       = TextAnchor.MiddleLeft;
            chipLayout.childControlWidth    = false;
            chipLayout.childControlHeight   = false;
            chipLayout.childForceExpandWidth  = false;
            chipLayout.childForceExpandHeight = false;
            chipGo.AddComponent<LayoutElement>().preferredHeight = 26f;

            // tag
            string tagStr  = order.OrderType == TradeOrderType.Buy ? (ru ? "КУП" : "BUY") : (ru ? "ПРД" : "SELL");
            Color tagColor = order.OrderType == TradeOrderType.Buy ? new Color(0.25f, 0.72f, 0.38f) : new Color(0.82f, 0.28f, 0.28f);
            chip.TagText = CreateBodyText("Tag", chipRect, font, tagStr, 10, TextAnchor.MiddleCenter, tagColor);
            chip.TagText.fontStyle = FontStyle.Bold;
            chip.TagText.gameObject.AddComponent<LayoutElement>().preferredWidth = 28f;

            // label
            string resLabel = $"{GetTradeResourceShortLabel(order.ResourceType)} ×{order.Amount}";
            chip.OrderText = CreateBodyText("Label", chipRect, font, resLabel, 11, TextAnchor.MiddleLeft, Color.white);
            chip.OrderText.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;

            // remove button
            int capturedId = order.Id;
            chip.RemoveButton = CreateButton("Remove", chipRect, font, out Text removeTxt, "×", 12, new Color(0.30f, 0.15f, 0.15f), new Color(0.9f, 0.5f, 0.5f));
            chip.RemoveButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 20f;
            chip.RemoveButton.onClick.AddListener(() =>
            {
                RemoveTradeHudOrder(capturedId);
                isWorldMapScreenDirty = true;
            });

            worldMapScreenUi.RouteRows.Add(chip);
        }
    }

    private bool HasRegionTradeRoute(int regionIndex)
    {
        foreach (TradeHudOrder order in activeTradeHudOrders)
        {
            if (order.TargetRegionIndex == regionIndex)
                return true;
        }
        return false;
    }

    private string GetTradeOrderRegionTag(int regionIndex)
    {
        if (regionIndex < 0)
        {
            return string.Empty;
        }

        string name = GetWorldMapRegionDisplayName(regionIndex);
        return IsRussianLanguage() ? $"Регион: {name}" : $"Region: {name}";
    }

    private static (TradeResourceType[] buyable, TradeResourceType[] sellable) GetRegionTradeCatalog(int regionIndex)
    {
        return regionIndex switch
        {
            5 => (new[] { TradeResourceType.Cotton, TradeResourceType.Textile }, System.Array.Empty<TradeResourceType>()),
            6 => (System.Array.Empty<TradeResourceType>(), new[] { TradeResourceType.Boards }),
            _ => (TradeImportCatalog, TradeExportCatalog)
        };
    }

    private void CycleWorldMapRouteResource()
    {
        int selected = Mathf.Clamp(selectedWorldMapRegionIndex, 0, 8);
        (TradeResourceType[] buyable, TradeResourceType[] sellable) = GetRegionTradeCatalog(selected);
        TradeResourceType[] catalog = worldMapRouteOrderType == TradeOrderType.Buy ? buyable : sellable;
        if (catalog.Length == 0) return;
        int idx = System.Array.IndexOf(catalog, worldMapRouteResource);
        worldMapRouteResource = catalog[(idx + 1) % catalog.Length];
        isWorldMapScreenDirty = true;
    }

    private void ToggleWorldMapRouteOrderType()
    {
        worldMapRouteOrderType = worldMapRouteOrderType == TradeOrderType.Buy ? TradeOrderType.Sell : TradeOrderType.Buy;
        isWorldMapScreenDirty = true;
    }

    private void PlaceWorldMapRouteOrder()
    {
        int regionIndex = Mathf.Clamp(selectedWorldMapRegionIndex, 0, 8);
        activeTradeHudOrders.Add(TradeOrderQueueService.CreateOrder(
            nextTradeOrderId++,
            worldMapRouteResource,
            worldMapRouteOrderType,
            worldMapRouteAmount,
            regionIndex));
        isWorldMapScreenDirty = true;
        isEconomyScreenDirty  = true;
        SessionDebugLogger.Log(
            "TRADE_HUD",
            $"Created regional order #{nextTradeOrderId - 1}: {worldMapRouteOrderType} {worldMapRouteResource} x{worldMapRouteAmount}; region={regionIndex}; queue={activeTradeHudOrders.Count}.");
        TryAutoDispatchNextHudOrder();
        PlayUiSound(uiPanelOpenClip, 0.88f);
        LogUiInput($"Map trade order placed: {worldMapRouteOrderType} {worldMapRouteResource} x{worldMapRouteAmount} -> region {regionIndex}");
    }
}
