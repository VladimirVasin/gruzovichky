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

        RectTransform mapSurface = CreateUiObject("WorldMapSurface", windowRoot.transform).GetComponent<RectTransform>();
        StretchRect(mapSurface, 0f, 0f, 0f, 0f);
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

        RectTransform topOverlay = CreateUiObject("WorldMapTopOverlay", windowRoot.transform).GetComponent<RectTransform>();
        topOverlay.anchorMin = new Vector2(0f, 1f);
        topOverlay.anchorMax = new Vector2(1f, 1f);
        topOverlay.pivot = new Vector2(0.5f, 1f);
        topOverlay.offsetMin = new Vector2(0f, -84f);
        topOverlay.offsetMax = Vector2.zero;
        topOverlay.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.08f, 0.48f);

        worldMapScreenUi.TitleText = CreateHeaderText("WorldMapTitle", topOverlay, font, "Regional Map", 24, TextAnchor.MiddleLeft, Color.white);
        RectTransform titleRect = worldMapScreenUi.TitleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(22f, -14f);
        titleRect.sizeDelta = new Vector2(420f, 30f);

        worldMapScreenUi.SubtitleText = CreateBodyText("WorldMapSubtitle", topOverlay, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);
        RectTransform subtitleRect = worldMapScreenUi.SubtitleText.rectTransform;
        subtitleRect.anchorMin = new Vector2(1f, 1f);
        subtitleRect.anchorMax = new Vector2(1f, 1f);
        subtitleRect.pivot = new Vector2(1f, 1f);
        subtitleRect.anchoredPosition = new Vector2(-56f, -36f);
        subtitleRect.sizeDelta = new Vector2(260f, 22f);

        worldMapScreenUi.SelectionHintText = CreateBodyText(
            "WorldMapHint",
            topOverlay,
            font,
            "Click a city on the regional map to inspect trade options.",
            13,
            TextAnchor.MiddleLeft,
            FleetSecondaryTextColor);
        RectTransform hintRect = worldMapScreenUi.SelectionHintText.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 1f);
        hintRect.anchorMax = new Vector2(0f, 1f);
        hintRect.pivot = new Vector2(0f, 1f);
        hintRect.anchoredPosition = new Vector2(22f, -50f);
        hintRect.sizeDelta = new Vector2(760f, 22f);

        RectTransform detailsPanel = CreateUiObject("WorldMapDetailsOverlay", windowRoot.transform).GetComponent<RectTransform>();
        detailsPanel.anchorMin = new Vector2(1f, 0f);
        detailsPanel.anchorMax = new Vector2(1f, 0f);
        detailsPanel.pivot = new Vector2(1f, 0f);
        detailsPanel.anchoredPosition = new Vector2(-24f, 24f);
        detailsPanel.sizeDelta = new Vector2(470f, 540f);
        worldMapScreenUi.DetailsPanelRoot = detailsPanel.gameObject;
        detailsPanel.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.08f, 0.11f, 0.78f);

        VerticalLayoutGroup detailsLayout = detailsPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        detailsLayout.padding = new RectOffset(16, 16, 14, 14);
        detailsLayout.spacing = 8f;
        detailsLayout.childControlWidth = true;
        detailsLayout.childControlHeight = true;
        detailsLayout.childForceExpandWidth = true;
        detailsLayout.childForceExpandHeight = false;

        RectTransform infoPanel = CreateUiObject("WorldMapDetailInfoPanel", detailsPanel).GetComponent<RectTransform>();
        VerticalLayoutGroup infoLayout = infoPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        infoLayout.spacing = 6f;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;
        infoPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 180f;

        worldMapScreenUi.DetailsNameText = CreateHeaderText("WorldMapDetailsName", infoPanel, font, string.Empty, 22, TextAnchor.MiddleLeft, Color.white);
        worldMapScreenUi.DetailsStatusText = CreateBodyText("WorldMapDetailsStatus", infoPanel, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetMutedTextColor);
        worldMapScreenUi.DetailsDescriptionText = CreateBodyText("WorldMapDetailsDescription", infoPanel, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        worldMapScreenUi.DetailsDescriptionText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        RectTransform resourceTablesRow = CreateLayoutRow("WorldMapResourceTablesRow", detailsPanel, 170f, 10f);
        RectTransform sellsTable = CreateWorldMapResourceTable(resourceTablesRow, font, "WorldMapSellsTable", out worldMapScreenUi.DetailsSellsLabelText, out worldMapScreenUi.DetailsSellsListRoot);
        sellsTable.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        RectTransform buysTable = CreateWorldMapResourceTable(resourceTablesRow, font, "WorldMapBuysTable", out worldMapScreenUi.DetailsBuysLabelText, out worldMapScreenUi.DetailsBuysListRoot);
        buysTable.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        worldMapScreenUi.DetailsResourcesText = CreateHeaderText("WorldMapDetailsResources", sellsTable, font, string.Empty, 1, TextAnchor.MiddleLeft, Color.clear);
        worldMapScreenUi.DetailsResourcesText.gameObject.SetActive(false);
        worldMapScreenUi.DetailsImportsText = CreateHeaderText("WorldMapDetailsImports", buysTable, font, string.Empty, 1, TextAnchor.MiddleLeft, Color.clear);
        worldMapScreenUi.DetailsImportsText.gameObject.SetActive(false);

        RectTransform routeBody = CreateUiObject("WorldMapRouteOverlay", detailsPanel).GetComponent<RectTransform>();
        routeBody.gameObject.AddComponent<LayoutElement>().preferredHeight = 148f;
        worldMapScreenUi.RoutePanelRoot = routeBody.gameObject;

        VerticalLayoutGroup routeBodyLayout = routeBody.gameObject.AddComponent<VerticalLayoutGroup>();
        routeBodyLayout.spacing = 8f;
        routeBodyLayout.childControlWidth  = true;
        routeBodyLayout.childControlHeight = true;
        routeBodyLayout.childForceExpandWidth  = true;
        routeBodyLayout.childForceExpandHeight = false;

        worldMapScreenUi.RoutePanelTitleText = CreateHeaderText("RoutePanelTitle", routeBody, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetMutedTextColor);
        worldMapScreenUi.RoutePanelTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        worldMapScreenUi.RouteStatusText = CreateBodyText("RouteStatusText", routeBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        worldMapScreenUi.RouteStatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        worldMapScreenUi.BuildRouteButton = CreateButton(
            "BuildRouteButton",
            routeBody,
            font,
            out worldMapScreenUi.BuildRouteButtonText,
            string.Empty,
            15,
            new Color(0.74f, 0.40f, 0.06f),
            Color.white);
        worldMapScreenUi.BuildRouteButtonText.fontStyle = FontStyle.Bold;
        worldMapScreenUi.BuildRouteButtonText.horizontalOverflow = HorizontalWrapMode.Wrap;
        worldMapScreenUi.BuildRouteButtonText.verticalOverflow = VerticalWrapMode.Truncate;
        worldMapScreenUi.BuildRouteButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
        worldMapScreenUi.BuildRouteButton.onClick.AddListener(BuildSelectedWorldMapTradeRoute);

        worldMapScreenUi.OpenTradeButton = CreateButton(
            "OpenTradeFromMapButton",
            routeBody,
            font,
            out worldMapScreenUi.OpenTradeButtonText,
            string.Empty,
            15,
            new Color(0.13f, 0.33f, 0.48f),
            Color.white);
        worldMapScreenUi.OpenTradeButtonText.fontStyle = FontStyle.Bold;
        worldMapScreenUi.OpenTradeButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
        worldMapScreenUi.OpenTradeButton.onClick.AddListener(OpenTradePanelFromWorldMap);
        worldMapScreenUi.OpenTradeButton.gameObject.SetActive(false);

        AddOverlayCloseButton(windowRect, font);
        worldMapScreenUi.CanvasRoot.SetActive(false);
        UpdateWorldMapScreenUi();
    }

    private RectTransform CreateWorldMapResourceTable(RectTransform parent, Font font, string name, out Text titleText, out RectTransform listRoot)
    {
        RectTransform table = CreateUiObject(name, parent).GetComponent<RectTransform>();
        Image bg = table.gameObject.AddComponent<Image>();
        bg.color = new Color(0.03f, 0.05f, 0.07f, 0.38f);
        VerticalLayoutGroup layout = table.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 7, 7);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        titleText = CreateHeaderText($"{name}Title", table, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        titleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        listRoot = CreateUiObject($"{name}List", table).GetComponent<RectTransform>();
        VerticalLayoutGroup listLayout = listRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.spacing = 5f;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        listRoot.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        return table;
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
            0 => "Undeveloped North",
            1 => "Undeveloped Woods",
            2 => "Undeveloped Port",
            3 => "Undeveloped Flats",
            4 => "Your Town",
            5 => "Weaverford",
            6 => "Oakbarrel",
            7 => "Undeveloped Steppe",
            8 => "Undeveloped Coast",
            _ => "Unknown Region"
        };
    }

    private string GetWorldMapRegionDisplayName(int regionIndex)
    {
        if (!IsRussianLanguage())
            return GetWorldMapRegionName(regionIndex);

        return regionIndex switch
        {
            0 => "Неразведанный север",
            1 => "Неразведанный лес",
            2 => "Неразведанный порт",
            3 => "Неразведанные равнины",
            4 => "Твой город",
            5 => "Ткацкая Слобода",
            6 => "Винокуренный Яр",
            7 => "Неразведанная степь",
            8 => "Неразведанный берег",
            _ => "Неизвестный регион"
        };
    }

    private static string GetWorldMapRegionTypeLabel(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "Current region",
            5 or 6 => "Neighbor city",
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
            5 or 6 => "Соседний город",
            _ => "Пустой слот региона"
        };
    }

    private string GetWorldMapRegionStatusDisplayLabel(int regionIndex)
    {
        string type = GetWorldMapRegionTypeDisplayLabel(regionIndex);
        if (regionIndex == 4)
        {
            return type;
        }

        bool hasRoute = IsWorldMapTradeRouteBuilt(regionIndex);
        if (IsRussianLanguage())
        {
            return hasRoute ? $"{type} · \u043c\u0430\u0440\u0448\u0440\u0443\u0442 \u043f\u0440\u043e\u043b\u043e\u0436\u0435\u043d" : $"{type} · \u043c\u0430\u0440\u0448\u0440\u0443\u0442 \u043d\u0435 \u043f\u0440\u043e\u043b\u043e\u0436\u0435\u043d";
        }

        return hasRoute ? $"{type} · route built" : $"{type} · no trade route";
    }

    private static string GetWorldMapRegionProducedResources(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "Logs, Boards, Furniture",
            5 => "Textile",
            6 => "Alcohol",
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
            5 => "Текстиль",
            6 => "Алкоголь",
            _ => "Нет подтверждённых данных"
        };
    }

    private static string GetWorldMapRegionImportedResources(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "Cotton, Textile",
            5 => "Furniture",
            6 => "Boards, Furniture",
            _ => "—"
        };
    }

    private string GetWorldMapRegionImportedResourcesDisplay(int regionIndex)
    {
        if (!IsRussianLanguage())
            return GetWorldMapRegionImportedResources(regionIndex);

        return regionIndex switch
        {
            4 => "Хлопок, текстиль",
            5 => "Мебель",
            6 => "Доски, мебель",
            _ => "—"
        };
    }

    private static string GetWorldMapRegionDescription(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "This is your active simulation region. It contains the current town, highways, production buildings, and local roads.",
            5 => "A compact textile town built around old weaving mills. It sells finished textile and buys furniture for workshops, offices, and worker housing.",
            6 => "A river-valley distillery town with barrel houses and bottling shops. It sells alcohol, while local workshops buy boards and furniture.",
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
            5 => "Небольшой текстильный город со старыми ткацкими цехами. Продаёт готовый текстиль и закупает мебель для мастерских, контор и жилья рабочих.",
            6 => "Городок в речной долине с винокурнями, бондарнями и линиями розлива. Продаёт алкоголь, а местные мастерские закупают доски и мебель.",
            _ => "Регион есть на глобальной карте, но пока не разведан и не имеет подробной производственной схемы."
        };
    }

    private static bool IsWorldMapRegionKnown(int regionIndex)
    {
        return regionIndex == 4 || regionIndex == 5 || regionIndex == 6;
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

        if (selectedWorldMapRegionIndex >= 0 && !IsWorldMapRegionKnown(selectedWorldMapRegionIndex))
        {
            selectedWorldMapRegionIndex = 4;
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
            if (cell.Button != null)
            {
                cell.Button.gameObject.SetActive(isKnown);
                cell.Button.interactable = isKnown;
            }

            if (!isKnown)
            {
                continue;
            }

            cell.NameText.text = GetWorldMapRegionDisplayName(i);
            cell.TypeText.text = GetWorldMapRegionTypeDisplayLabel(i);
            cell.NameText.gameObject.SetActive(false);
            cell.TypeText.gameObject.SetActive(false);
            cell.Background.color = isSelected
                ? new Color(1f, 0.72f, 0.16f, 0.10f)
                : isCurrent
                    ? new Color(1f, 0.72f, 0.16f, 0.04f)
                    : isKnown
                        ? new Color(0.16f, 0.12f, 0.07f, 0f)
                        : new Color(0.10f, 0.08f, 0.06f, 0f);
            cell.NameText.color = isKnown || isCurrent ? new Color(1f, 0.94f, 0.78f, 1f) : new Color(0.78f, 0.70f, 0.55f, 1f);
            cell.TypeText.color = isSelected ? new Color(1f, 0.82f, 0.28f, 1f) : new Color(0.68f, 0.61f, 0.48f, 1f);
            if (cell.PreviewBackground != null)
            {
                ApplyWorldMapCellPreview(cell, i, isKnown);
            }
            if (cell.Outline != null)
            {
                cell.Outline.effectColor = isSelected
                    ? new Color(1f, 0.78f, 0.20f, 0.72f)
                    : Color.clear;
                cell.Outline.effectDistance = isSelected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            }

            if (cell.RouteStatusDot != null)
            {
                bool isNeighborCell = !isCurrent && isKnown;
                bool hasRoute = isNeighborCell && IsWorldMapTradeRouteBuilt(i);
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
            bool hasRoute = routeRegionKnown && IsWorldMapTradeRouteBuilt(i);
            Color lineColor = new(0.62f, 0.38f, 0.14f, 0.90f);
            UpdateWorldMapRouteLine(worldMapScreenUi.RegionRouteLines[i], worldMapScreenUi.MapRoot, i, hasRoute, lineColor);
        }

        bool hasSelectedRegion = selectedWorldMapRegionIndex >= 0;
        int selected = hasSelectedRegion ? Mathf.Clamp(selectedWorldMapRegionIndex, 0, 8) : -1;

        if (worldMapScreenUi.DetailsPanelRoot != null)
        {
            worldMapScreenUi.DetailsPanelRoot.SetActive(hasSelectedRegion);
        }

        if (hasSelectedRegion)
        {
            worldMapScreenUi.DetailsNameText.text = GetWorldMapRegionDisplayName(selected);
            worldMapScreenUi.DetailsStatusText.text = GetWorldMapRegionStatusDisplayLabel(selected);
            worldMapScreenUi.DetailsResourcesText.text = string.Empty;
            worldMapScreenUi.DetailsImportsText.text = string.Empty;
            worldMapScreenUi.DetailsDescriptionText.text = GetWorldMapRegionDescriptionDisplay(selected);
            RefreshWorldMapResourceTables(selected);
        }

        // ── Route panel ────────────────────────────────────────────────────
        bool isNeighbor = hasSelectedRegion && IsWorldMapRegionKnown(selected) && selected != 4;
        worldMapScreenUi.RoutePanelRoot.SetActive(isNeighbor);

        if (isNeighbor)
        {
            bool routeBuilt = IsWorldMapTradeRouteBuilt(selected);
            worldMapScreenUi.RoutePanelTitleText.text = ru ? "\u0422\u043e\u0440\u0433\u043e\u0432\u044b\u0439 \u043c\u0430\u0440\u0448\u0440\u0443\u0442" : "Trade route";
            worldMapScreenUi.RouteStatusText.text = routeBuilt
                ? (ru ? "\u0421\u0442\u0430\u0442\u0443\u0441: \u043c\u0430\u0440\u0448\u0440\u0443\u0442 \u043f\u0440\u043e\u043b\u043e\u0436\u0435\u043d" : "Status: route built")
                : (ru ? "\u0421\u0442\u0430\u0442\u0443\u0441: \u043c\u0430\u0440\u0448\u0440\u0443\u0442 \u043d\u0435 \u043f\u0440\u043e\u043b\u043e\u0436\u0435\u043d" : "Status: no route");
            worldMapScreenUi.BuildRouteButtonText.text = routeBuilt
                ? (ru ? "\u041c\u0430\u0440\u0448\u0440\u0443\u0442 \u043f\u0440\u043e\u043b\u043e\u0436\u0435\u043d" : "Route built")
                : (ru ? "\u041f\u0440\u043e\u043b\u043e\u0436\u0438\u0442\u044c \u0442\u043e\u0440\u0433\u043e\u0432\u044b\u0439 \u043c\u0430\u0440\u0448\u0440\u0443\u0442" : "Build trade route");
            worldMapScreenUi.BuildRouteButton.interactable = !routeBuilt;
            worldMapScreenUi.BuildRouteButton.image.color = routeBuilt
                ? new Color(0.18f, 0.26f, 0.20f, 0.95f)
                : new Color(0.74f, 0.40f, 0.06f, 1f);
            worldMapScreenUi.BuildRouteButtonText.color = Color.white;
            if (worldMapScreenUi.OpenTradeButton != null)
            {
                worldMapScreenUi.OpenTradeButton.gameObject.SetActive(routeBuilt);
                worldMapScreenUi.OpenTradeButton.interactable = routeBuilt;
                worldMapScreenUi.OpenTradeButtonText.text = ru ? "\u041f\u0435\u0440\u0435\u0439\u0442\u0438 \u0432 \u0442\u043e\u0440\u0433\u043e\u0432\u043b\u044e" : "Open Trade";
                worldMapScreenUi.OpenTradeButton.image.color = new Color(0.13f, 0.33f, 0.48f, 1f);
                worldMapScreenUi.OpenTradeButtonText.color = Color.white;
            }
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(worldMapScreenUi.WindowRoot);
        LocalizeCanvas(worldMapScreenUi.CanvasRoot);
        isWorldMapScreenDirty = false;
    }

    private bool IsWorldMapTradeRouteBuilt(int regionIndex)
    {
        return regionIndex >= 0 && regionIndex < worldMapTradeRoutesBuilt.Length && worldMapTradeRoutesBuilt[regionIndex];
    }

    private void RefreshWorldMapResourceTables(int regionIndex)
    {
        ClearWorldMapResourceRows(worldMapScreenUi.DetailsSellsRows);
        ClearWorldMapResourceRows(worldMapScreenUi.DetailsBuysRows);

        (TradeResourceType[] citySells, TradeResourceType[] cityBuys) = GetRegionTradeCatalog(regionIndex);
        FillWorldMapResourceRows(worldMapScreenUi.DetailsSellsListRoot, worldMapScreenUi.DetailsSellsRows, citySells);
        FillWorldMapResourceRows(worldMapScreenUi.DetailsBuysListRoot, worldMapScreenUi.DetailsBuysRows, cityBuys);
    }

    private void ClearWorldMapResourceRows(List<WorldMapResourceRowUi> rows)
    {
        for (int i = 0; i < rows.Count; i++)
        {
            if (rows[i].Root != null) Destroy(rows[i].Root);
        }
        rows.Clear();
    }

    private void FillWorldMapResourceRows(RectTransform parent, List<WorldMapResourceRowUi> rows, TradeResourceType[] resources)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (resources == null || resources.Length == 0)
        {
            RectTransform emptyRow = CreateLayoutRow("WorldMapResourceEmptyRow", parent, 30f, 6f);
            Text emptyText = CreateBodyText("WorldMapResourceEmptyText", emptyRow, font, IsRussianLanguage() ? "\u2014" : "—", 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
            emptyText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            rows.Add(new WorldMapResourceRowUi { Root = emptyRow.gameObject });
            return;
        }

        for (int i = 0; i < resources.Length; i++)
        {
            TradeResourceType resource = resources[i];
            RectTransform row = CreateLayoutRow($"WorldMapResourceRow_{resource}", parent, 34f, 7f);
            Image rowBg = row.gameObject.AddComponent<Image>();
            rowBg.color = new Color(1f, 1f, 1f, 0.035f);

            RectTransform iconRoot = CreateUiObject($"WorldMapResourceIcon_{resource}", row).GetComponent<RectTransform>();
            LayoutElement iconLayout = iconRoot.gameObject.AddComponent<LayoutElement>();
            iconLayout.preferredWidth = 28f;
            iconLayout.preferredHeight = 28f;
            Image iconBg = iconRoot.gameObject.AddComponent<Image>();
            iconBg.color = new Color(1f, 1f, 1f, 0.08f);
            DrawResourceIcon(iconRoot, GetWorldMapResourceVisualKind(resource));

            Text label = CreateBodyText($"WorldMapResourceLabel_{resource}", row, font, GetTradeResourceDisplayLabel(resource), 12, TextAnchor.MiddleLeft, Color.white);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            rows.Add(new WorldMapResourceRowUi { Root = row.gameObject, ResourceType = resource });
        }
    }

    private static ResourceVisualKind GetWorldMapResourceVisualKind(TradeResourceType resourceType)
    {
        return resourceType switch
        {
            TradeResourceType.Logs => ResourceVisualKind.Logs,
            TradeResourceType.Boards => ResourceVisualKind.Boards,
            TradeResourceType.Cotton => ResourceVisualKind.Cotton,
            TradeResourceType.Textile => ResourceVisualKind.Textile,
            TradeResourceType.Furniture => ResourceVisualKind.Furniture,
            TradeResourceType.Alcohol => ResourceVisualKind.Alcohol,
            _ => ResourceVisualKind.Logs
        };
    }

    private string GetTradeResourceDisplayLabel(TradeResourceType resourceType)
    {
        if (!IsRussianLanguage())
        {
            return GetTradeResourceShortLabel(resourceType);
        }

        return resourceType switch
        {
            TradeResourceType.Logs => "\u0411\u0440\u0435\u0432\u043d\u0430",
            TradeResourceType.Boards => "\u0414\u043e\u0441\u043a\u0438",
            TradeResourceType.Cotton => "\u0425\u043b\u043e\u043f\u043e\u043a",
            TradeResourceType.Textile => "\u0422\u0435\u043a\u0441\u0442\u0438\u043b\u044c",
            TradeResourceType.Furniture => "\u041c\u0435\u0431\u0435\u043b\u044c",
            TradeResourceType.Alcohol => "\u0410\u043b\u043a\u043e\u0433\u043e\u043b\u044c",
            _ => GetTradeResourceShortLabel(resourceType)
        };
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
            5 => (new[] { TradeResourceType.Textile }, new[] { TradeResourceType.Furniture }),
            6 => (new[] { TradeResourceType.Alcohol }, new[] { TradeResourceType.Boards, TradeResourceType.Furniture }),
            _ => (TradeImportCatalog, TradeExportCatalog)
        };
    }

    private void BuildSelectedWorldMapTradeRoute()
    {
        int regionIndex = Mathf.Clamp(selectedWorldMapRegionIndex, 0, 8);
        if (regionIndex == 4 || !IsWorldMapRegionKnown(regionIndex) || IsWorldMapTradeRouteBuilt(regionIndex))
        {
            return;
        }

        worldMapTradeRoutesBuilt[regionIndex] = true;
        isWorldMapScreenDirty = true;
        SessionDebugLogger.Log("TRADE_HUD", $"Built regional trade route: region={regionIndex}; name={GetWorldMapRegionName(regionIndex)}.");
        PlayUiSound(uiPanelOpenClip, 0.88f);
        LogUiInput($"Map trade route built -> region {regionIndex}");
    }

    private void OpenTradePanelFromWorldMap()
    {
        int regionIndex = Mathf.Clamp(selectedWorldMapRegionIndex, 0, 8);
        if (regionIndex == 4 || !IsWorldMapRegionKnown(regionIndex) || !IsWorldMapTradeRouteBuilt(regionIndex))
        {
            return;
        }

        CloseWorldMapPanel();
        isFleetPanelOpen = false;
        isShiftsPanelOpen = false;
        isDriversPanelOpen = false;
        isResourcesPanelOpen = false;
        isEconomyPanelOpen = false;
        isBuildPanelOpen = false;
        isStatesPanelOpen = false;
        isTradePanelOpen = true;
        isTradeScreenDirty = true;
        isWorldMapScreenDirty = true;
        SessionDebugLogger.Log("TRADE_UI", $"Opened Trade HUD from regional map route card: region={regionIndex}; name={GetWorldMapRegionName(regionIndex)}.");
        LogUiInput($"Map route card -> open Trade for region {regionIndex}");
        PlayUiSound(uiPanelOpenClip, 0.88f);
    }
}
