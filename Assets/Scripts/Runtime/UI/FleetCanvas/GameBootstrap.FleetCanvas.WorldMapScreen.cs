using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
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

        RectTransform mapCard = CreateSectionCard(contentRow, font, "Region Grid", out RectTransform mapBody);
        LayoutElement mapCardLayout = mapCard.gameObject.AddComponent<LayoutElement>();
        mapCardLayout.preferredWidth = 600f;
        mapCardLayout.flexibleWidth = 1f;
        mapCardLayout.flexibleHeight = 1f;

        worldMapScreenUi.SelectionHintText = CreateBodyText(
            "WorldMapHint",
            mapBody,
            font,
            "Click a region cell to inspect its resource profile.",
            13,
            TextAnchor.MiddleLeft,
            FleetSecondaryTextColor);

        RectTransform mapFrame = CreateStyledPanel("WorldMapFrame", mapBody, FleetCardMutedColor);
        LayoutElement mapFrameLayout = mapFrame.gameObject.AddComponent<LayoutElement>();
        mapFrameLayout.preferredHeight = 480f;
        VerticalLayoutGroup mapFrameLayoutGroup = mapFrame.gameObject.AddComponent<VerticalLayoutGroup>();
        mapFrameLayoutGroup.padding = new RectOffset(16, 16, 16, 16);
        mapFrameLayoutGroup.spacing = 10;
        mapFrameLayoutGroup.childControlWidth = true;
        mapFrameLayoutGroup.childControlHeight = true;
        mapFrameLayoutGroup.childForceExpandWidth = true;
        mapFrameLayoutGroup.childForceExpandHeight = false;

        RectTransform mapSurface = CreateUiObject("WorldMapSurface", mapFrame).GetComponent<RectTransform>();
        LayoutElement mapSurfaceLayout = mapSurface.gameObject.AddComponent<LayoutElement>();
        mapSurfaceLayout.preferredHeight = 420f;
        Image mapSurfaceBackground = mapSurface.gameObject.AddComponent<Image>();
        mapSurfaceBackground.color = new Color(0.11f, 0.14f, 0.19f, 0.72f);

        RectTransform gridRoot = CreateUiObject("WorldMapGridRoot", mapSurface).GetComponent<RectTransform>();
        gridRoot.anchorMin = Vector2.zero;
        gridRoot.anchorMax = Vector2.one;
        gridRoot.offsetMin = Vector2.zero;
        gridRoot.offsetMax = Vector2.zero;
        LayoutElement gridLayoutElement = gridRoot.gameObject.AddComponent<LayoutElement>();
        gridLayoutElement.preferredHeight = 420f;
        GridLayoutGroup gridLayout = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(170f, 120f);
        gridLayout.spacing = new Vector2(10f, 10f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        for (int regionIndex = 0; regionIndex < 9; regionIndex++)
        {
            worldMapScreenUi.Cells.Add(CreateWorldMapCell(gridRoot, font, regionIndex));
        }

        RectTransform detailsCard = CreateSectionCard(contentRow, font, "Region Preview", out RectTransform detailsBody);
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
        CreateHeaderText("WorldMapResourcesLabel", infoPanel, font, "Produced Resources", 11, TextAnchor.MiddleLeft, FleetMutedTextColor);
        worldMapScreenUi.DetailsResourcesText = CreateHeaderText("WorldMapDetailsResources", infoPanel, font, string.Empty, 17, TextAnchor.MiddleLeft, FleetAccentColor);
        CreateHeaderText("WorldMapImportsLabel", infoPanel, font, "Buy / Imports", 11, TextAnchor.MiddleLeft, FleetMutedTextColor);
        worldMapScreenUi.DetailsImportsText = CreateHeaderText("WorldMapDetailsImports", infoPanel, font, string.Empty, 17, TextAnchor.MiddleLeft, FleetAccentColor);
        worldMapScreenUi.DetailsDescriptionText = CreateBodyText("WorldMapDetailsDescription", infoPanel, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        worldMapScreenUi.DetailsDescriptionText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        // Trade Routes bottom panel.
        RectTransform routeCard = CreateSectionCard(windowRoot.transform, font, string.Empty, out RectTransform routeBody);
        routeCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 110f;
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

    private WorldMapCellUi CreateWorldMapCell(RectTransform parent, Font font, int regionIndex)
    {
        WorldMapCellUi cell = new();
        GameObject cellObject = CreateUiObject($"WorldMapCell_{regionIndex}", parent);
        RectTransform cellRect = cellObject.GetComponent<RectTransform>();
        LayoutElement layoutElement = cellRect.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 170f;
        layoutElement.preferredHeight = 120f;

        Image background = cellObject.AddComponent<Image>();
        background.color = FleetInsetColor;
        Button button = cellObject.AddComponent<Button>();
        Outline outline = cellObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.22f);
        outline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup layout = cellObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform previewRoot = CreateStyledPanel($"WorldMapCellPreview_{regionIndex}", cellRect, new Color(0.12f, 0.15f, 0.20f, 0.98f));
        LayoutElement previewLayout = previewRoot.gameObject.AddComponent<LayoutElement>();
        previewLayout.preferredHeight = 70f;
        cell.PreviewBackground = previewRoot.GetComponent<Image>();

        cell.PreviewPlaceholderText = CreateBodyText($"WorldMapCellPreviewPlaceholder_{regionIndex}", previewRoot, font, string.Empty, 12, TextAnchor.MiddleCenter, FleetMutedTextColor);
        StretchRect(cell.PreviewPlaceholderText.rectTransform, 10f, 10f, 10f, 10f);

        cell.WaterShape = CreateWorldMapPreviewShape(previewRoot, $"WorldMapWater_{regionIndex}", new Color(0.54f, 0.77f, 0.92f, 0.95f), 0f, 0.76f, 1f, 0.24f);
        cell.HighwayShape = CreateWorldMapPreviewShape(previewRoot, $"WorldMapHighway_{regionIndex}", new Color(0.15f, 0.17f, 0.20f, 1f), 0.04f, 0.08f, 0.92f, 0.16f);
        cell.ForestShape = CreateWorldMapPreviewShape(previewRoot, $"WorldMapForest_{regionIndex}", new Color(0.19f, 0.39f, 0.24f, 0.98f), 0.06f, 0.36f, 0.28f, 0.30f);
        cell.TownBlockA = CreateWorldMapPreviewShape(previewRoot, $"WorldMapTownA_{regionIndex}", new Color(0.83f, 0.72f, 0.46f, 0.96f), 0.40f, 0.30f, 0.16f, 0.16f);
        cell.TownBlockB = CreateWorldMapPreviewShape(previewRoot, $"WorldMapTownB_{regionIndex}", new Color(0.86f, 0.78f, 0.55f, 0.96f), 0.58f, 0.30f, 0.18f, 0.18f);
        cell.TownBlockC = CreateWorldMapPreviewShape(previewRoot, $"WorldMapTownC_{regionIndex}", new Color(0.76f, 0.63f, 0.34f, 0.96f), 0.50f, 0.50f, 0.12f, 0.12f);
        cell.HighwayDashA = CreateWorldMapPreviewShape(previewRoot, $"WorldMapDashA_{regionIndex}", new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.18f, 0.12f, 0.10f, 0.03f);
        cell.HighwayDashB = CreateWorldMapPreviewShape(previewRoot, $"WorldMapDashB_{regionIndex}", new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.45f, 0.12f, 0.10f, 0.03f);
        cell.HighwayDashC = CreateWorldMapPreviewShape(previewRoot, $"WorldMapDashC_{regionIndex}", new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.72f, 0.12f, 0.10f, 0.03f);

        cell.NameText = CreateHeaderText($"WorldMapCellName_{regionIndex}", cellRect, font, string.Empty, 15, TextAnchor.MiddleLeft, Color.white);
        cell.TypeText = CreateBodyText($"WorldMapCellType_{regionIndex}", cellRect, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetMutedTextColor);

        // Route status dot — top-right corner of cell
        GameObject dotObj = CreateUiObject($"WorldMapRouteDot_{regionIndex}", cellRect);
        RectTransform dotRect = dotObj.GetComponent<RectTransform>();
        dotRect.anchorMin = new Vector2(1f, 1f);
        dotRect.anchorMax = new Vector2(1f, 1f);
        dotRect.pivot = new Vector2(1f, 1f);
        dotRect.anchoredPosition = new Vector2(-6f, -6f);
        dotRect.sizeDelta = new Vector2(10f, 10f);
        Image dotImage = dotObj.AddComponent<Image>();
        dotImage.color = new Color(0.3f, 0.8f, 0.4f, 0f); // hidden by default
        cell.RouteStatusDot = dotImage;

        cell.Button = button;
        cell.Background = background;
        cell.Outline = outline;
        cell.RegionIndex = regionIndex;
        cell.Button.onClick.AddListener(() => SelectWorldMapRegion(regionIndex));
        return cell;
    }

    private WorldMapDetailPreviewUi CreateWorldMapDetailPreview(RectTransform parent, Font font)
    {
        WorldMapDetailPreviewUi preview = new();
        preview.PreviewBackground = parent.GetComponent<Image>();

        preview.PlaceholderText = CreateBodyText("WorldMapDetailPlaceholder", parent, font,
            "No regional map yet", 14, TextAnchor.MiddleCenter, FleetMutedTextColor);
        StretchRect(preview.PlaceholderText.rectTransform, 10f, 10f, 10f, 10f);

        preview.WaterShape   = CreateWorldMapPreviewShape(parent, "WorldMapDetailWater",    new Color(0.54f, 0.77f, 0.92f, 0.95f), 0f,    0.76f, 1f,    0.24f);
        preview.HighwayShape = CreateWorldMapPreviewShape(parent, "WorldMapDetailHighway",  new Color(0.15f, 0.17f, 0.20f, 1f),    0.04f, 0.08f, 0.92f, 0.16f);
        preview.ForestShape  = CreateWorldMapPreviewShape(parent, "WorldMapDetailForest",   new Color(0.19f, 0.39f, 0.24f, 0.98f), 0.06f, 0.36f, 0.28f, 0.30f);
        preview.TownBlockA   = CreateWorldMapPreviewShape(parent, "WorldMapDetailTownA",    new Color(0.83f, 0.72f, 0.46f, 0.96f), 0.40f, 0.30f, 0.16f, 0.16f);
        preview.TownBlockB   = CreateWorldMapPreviewShape(parent, "WorldMapDetailTownB",    new Color(0.86f, 0.78f, 0.55f, 0.96f), 0.58f, 0.30f, 0.18f, 0.18f);
        preview.TownBlockC   = CreateWorldMapPreviewShape(parent, "WorldMapDetailTownC",    new Color(0.76f, 0.63f, 0.34f, 0.96f), 0.50f, 0.50f, 0.12f, 0.12f);
        preview.HighwayDashA = CreateWorldMapPreviewShape(parent, "WorldMapDetailDashA",    new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.18f, 0.12f, 0.10f, 0.03f);
        preview.HighwayDashB = CreateWorldMapPreviewShape(parent, "WorldMapDetailDashB",    new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.45f, 0.12f, 0.10f, 0.03f);
        preview.HighwayDashC = CreateWorldMapPreviewShape(parent, "WorldMapDetailDashC",    new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.72f, 0.12f, 0.10f, 0.03f);

        return preview;
    }

    private Image CreateWorldMapPreviewShape(RectTransform parent, string name, Color color, float x, float y, float width, float height)
    {
        GameObject obj = CreateUiObject(name, parent);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(x + width, y + height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
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

    private static string GetWorldMapRegionTypeLabel(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "Current region",
            2 or 5 or 6 => "Neighbor region",
            _ => "Empty region slot"
        };
    }

    private static string GetWorldMapRegionProducedResources(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "Logs, Boards, Furniture",
            5 => "Cotton, Textile",
            6 => "Grain, Alcohol",
            2 => "Trade logistics",
            _ => "No confirmed survey data"
        };
    }

    private static string GetWorldMapRegionImportedResources(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "Cotton, Textile, Fuel, Alcohol, Food",
            5 => "—",
            6 => "Boards",
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

        worldMapScreenUi.TitleText.text = "Regional Map";
        worldMapScreenUi.SubtitleText.text = "Open/Close: M";
        worldMapScreenUi.SelectionHintText.text = "Each cell is a mini-map of a larger world region. Only the current region is drawn for now.";

        for (int i = 0; i < worldMapScreenUi.Cells.Count; i++)
        {
            WorldMapCellUi cell = worldMapScreenUi.Cells[i];
            bool isSelected = i == selectedWorldMapRegionIndex;
            bool isCurrent = i == 4;
            bool isKnown = IsWorldMapRegionKnown(i);
            bool hasRealPreview = isCurrent;

            cell.NameText.text = GetWorldMapRegionName(i);
            cell.TypeText.text = GetWorldMapRegionTypeLabel(i);
            cell.Background.color = isSelected
                ? FleetSelectedRowColor
                : isCurrent
                    ? new Color(0.30f, 0.24f, 0.10f, 0.98f)
                    : isKnown
                        ? FleetInsetColor
                        : FleetCardMutedColor;
            cell.NameText.color = isKnown || isCurrent ? Color.white : FleetSecondaryTextColor;
            cell.TypeText.color = isSelected ? FleetAccentColor : FleetMutedTextColor;
            cell.PreviewBackground.color = hasRealPreview
                ? new Color(0.18f, 0.20f, 0.17f, 1f)
                : new Color(0.15f, 0.17f, 0.21f, 0.98f);
            cell.PreviewPlaceholderText.gameObject.SetActive(!hasRealPreview);
            cell.PreviewPlaceholderText.text = i == 4 ? string.Empty : "No regional map yet";
            cell.WaterShape.gameObject.SetActive(hasRealPreview);
            cell.HighwayShape.gameObject.SetActive(hasRealPreview);
            cell.ForestShape.gameObject.SetActive(hasRealPreview);
            cell.TownBlockA.gameObject.SetActive(hasRealPreview);
            cell.TownBlockB.gameObject.SetActive(hasRealPreview);
            cell.TownBlockC.gameObject.SetActive(hasRealPreview);
            cell.HighwayDashA.gameObject.SetActive(hasRealPreview);
            cell.HighwayDashB.gameObject.SetActive(hasRealPreview);
            cell.HighwayDashC.gameObject.SetActive(hasRealPreview);
            if (cell.Outline != null)
            {
                cell.Outline.effectColor = isSelected
                    ? new Color(FleetAccentColor.r, FleetAccentColor.g, FleetAccentColor.b, 0.72f)
                    : new Color(0f, 0f, 0f, 0.22f);
                cell.Outline.effectDistance = isSelected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            }

            if (cell.RouteStatusDot != null)
            {
                bool isNeighborCell = !isCurrent && isKnown;
                bool hasRoute = isNeighborCell && HasRegionTradeRoute(i);
                cell.RouteStatusDot.color = hasRoute
                    ? new Color(0.3f, 0.85f, 0.45f, 1f)
                    : new Color(0f, 0f, 0f, 0f);
            }
        }

        int selected = Mathf.Clamp(selectedWorldMapRegionIndex, 0, 8);
        bool detailHasPreview = selected == 4;

        if (worldMapScreenUi.DetailPreview != null)
        {
            worldMapScreenUi.DetailPreview.PlaceholderText.gameObject.SetActive(!detailHasPreview);
            worldMapScreenUi.DetailPreview.PreviewBackground.color = detailHasPreview
                ? new Color(0.18f, 0.20f, 0.17f, 1f)
                : new Color(0.12f, 0.15f, 0.20f, 0.98f);
            worldMapScreenUi.DetailPreview.WaterShape.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.HighwayShape.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.ForestShape.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.TownBlockA.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.TownBlockB.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.TownBlockC.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.HighwayDashA.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.HighwayDashB.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.HighwayDashC.gameObject.SetActive(detailHasPreview);
        }

        worldMapScreenUi.DetailsNameText.text = GetWorldMapRegionName(selected);
        worldMapScreenUi.DetailsStatusText.text = GetWorldMapRegionTypeLabel(selected);
        worldMapScreenUi.DetailsResourcesText.text = GetWorldMapRegionProducedResources(selected);
        worldMapScreenUi.DetailsImportsText.text = GetWorldMapRegionImportedResources(selected);
        worldMapScreenUi.DetailsDescriptionText.text = GetWorldMapRegionDescription(selected);

        // ── Route panel ────────────────────────────────────────────────────
        bool isNeighbor = IsWorldMapRegionKnown(selected) && selected != 4;
        worldMapScreenUi.RoutePanelRoot.SetActive(isNeighbor);

        if (isNeighbor)
        {
            bool ru = IsRussianLanguage();
            worldMapScreenUi.RoutePanelTitleText.text = (ru ? "Торговые маршруты: " : "Trade Routes: ") + GetWorldMapRegionName(selected);

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

        string name = GetWorldMapRegionName(regionIndex);
        return IsRussianLanguage() ? $"Регион: {name}" : $"Region: {name}";
    }

    private static (TradeResourceType[] buyable, TradeResourceType[] sellable) GetRegionTradeCatalog(int regionIndex)
    {
        return regionIndex switch
        {
            5 => (new[] { TradeResourceType.Cotton, TradeResourceType.Textile }, System.Array.Empty<TradeResourceType>()),
            6 => (new[] { TradeResourceType.Alcohol }, new[] { TradeResourceType.Boards }),
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
        LogUiInput($"Map trade order placed: {worldMapRouteOrderType} {worldMapRouteResource} ×{worldMapRouteAmount} → region {regionIndex}");
    }
}
