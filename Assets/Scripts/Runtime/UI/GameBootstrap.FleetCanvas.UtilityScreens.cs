using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
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
        contentGroup.spacing = 10;
        contentGroup.childControlWidth = true;
        contentGroup.childControlHeight = true;
        contentGroup.childForceExpandWidth = true;
        contentGroup.childForceExpandHeight = false;

        CreateResourceSummaryRow(contentRoot, font, "Logs", ResourceVisualKind.Logs, resourcesScreenUi.Rows);
        CreateResourceSummaryRow(contentRoot, font, "Boards", ResourceVisualKind.Boards, resourcesScreenUi.Rows);
        CreateResourceSummaryRow(contentRoot, font, "Cotton", ResourceVisualKind.Cotton, resourcesScreenUi.Rows);
        CreateResourceSummaryRow(contentRoot, font, "Textile", ResourceVisualKind.Textile, resourcesScreenUi.Rows);
        CreateResourceSummaryRow(contentRoot, font, "Furniture", ResourceVisualKind.Furniture, resourcesScreenUi.Rows);

        RectTransform footerCard = CreateSectionCard(windowRoot.transform, font, "Treasury", out RectTransform footerBody);
        footerCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 74f;
        resourcesScreenUi.TreasuryValueText = CreateHeaderText("TreasuryValue", footerBody, font, string.Empty, 18, TextAnchor.MiddleLeft, FleetAccentColor);

        resourcesScreenUi.CanvasRoot.SetActive(false);
        UpdateResourcesScreenUi();
    }

    private enum ResourceVisualKind
    {
        Logs,
        Boards,
        Cotton,
        Textile,
        Furniture
    }

    private void CreateResourceSummaryRow(RectTransform parent, Font font, string title, ResourceVisualKind iconKind, List<ResourceSummaryRowUi> rows)
    {
        RectTransform card = CreateStyledPanel($"{title}RowCard", parent, FleetInsetColor);
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = 62f;

        HorizontalLayoutGroup layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 12;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        RectTransform iconRoot = CreateUiObject($"{title}IconRoot", card).GetComponent<RectTransform>();
        LayoutElement iconLayout = iconRoot.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 34f;
        iconLayout.preferredHeight = 34f;
        Image iconBackground = iconRoot.gameObject.AddComponent<Image>();
        iconBackground.color = new Color(1f, 1f, 1f, 0.06f);
        DrawResourceIcon(iconRoot, iconKind);

        RectTransform textRoot = CreateUiObject($"{title}TextRoot", card).GetComponent<RectTransform>();
        LayoutElement textLayout = textRoot.gameObject.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1f;
        VerticalLayoutGroup textGroup = textRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        textGroup.spacing = 2f;
        textGroup.childControlWidth = true;
        textGroup.childControlHeight = true;
        textGroup.childForceExpandWidth = true;
        textGroup.childForceExpandHeight = false;

        Text nameText = CreateBodyText($"{title}Name", textRoot, font, title, 14, TextAnchor.MiddleLeft, Color.white);
        nameText.fontStyle = FontStyle.Bold;
        nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        Text valueText = CreateHeaderText($"{title}Value", textRoot, font, string.Empty, 18, TextAnchor.MiddleLeft, FleetAccentColor);
        valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        rows.Add(new ResourceSummaryRowUi
        {
            NameText = nameText,
            ValueText = valueText
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

        string headerCount = $"{resourcesScreenUi.Rows.Count} Resources";
        string treasuryValue = $"${money}";
        int totalLogs = GetTotalLogsResourceAmount();
        int totalBoards = GetTotalBoardsResourceAmount();
        string[] resourceNames = { "Logs", "Boards", "Cotton", "Textile", "Furniture" };
        string[] resourceValues =
        {
            totalLogs.ToString(),
            totalBoards.ToString(),
            cottonStored.ToString(),
            textileStored.ToString(),
            furnitureStored.ToString()
        };

        if (resourcesScreenUi.LastHeaderCount != headerCount)
        {
            resourcesScreenUi.HeaderCountText.text = headerCount;
            resourcesScreenUi.LastHeaderCount = headerCount;
            forceLayoutRebuild = true;
        }

        for (int i = 0; i < resourcesScreenUi.Rows.Count && i < resourceNames.Length; i++)
        {
            ResourceSummaryRowUi row = resourcesScreenUi.Rows[i];
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

    private int GetTotalLogsResourceAmount()
    {
        int total = 0;
        if (locations.TryGetValue(LocationType.Forest, out LocationData forest))
        {
            total += forest.LogsStored;
        }

        if (locations.TryGetValue(LocationType.Sawmill, out LocationData sawmill))
        {
            total += sawmill.LogsStored;
        }

        if (truckCargoType == CargoType.Logs)
        {
            total += truckCargoAmount;
        }

        return total;
    }

    private int GetTotalBoardsResourceAmount()
    {
        int total = 0;
        if (locations.TryGetValue(LocationType.Sawmill, out LocationData sawmill))
        {
            total += sawmill.BoardsStored;
        }

        if (locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse))
        {
            total += warehouse.BoardsStored;
        }

        if (truckCargoType == CargoType.Boards)
        {
            total += truckCargoAmount;
        }

        return total;
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

        RectTransform tradeCard = CreateSectionCard(windowRoot.transform, font, "Trade Dispatch", out RectTransform tradeBody);
        tradeCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 216f;

        RectTransform resourceRow = CreateLayoutRow("TradeResourceRow", tradeBody, 34f, 8f);
        CreateBodyText("TradeResourceLabel", resourceRow, font, "Resource", 13, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;
        economyScreenUi.TradePrevButton = CreateButton("TradePrevButton", resourceRow, font, out Text tradePrevText, "<", 13, new Color(0.26f, 0.30f, 0.36f, 1f), Color.white);
        tradePrevText.fontStyle = FontStyle.Bold;
        LayoutElement tradePrevLayout = economyScreenUi.TradePrevButton.gameObject.AddComponent<LayoutElement>();
        tradePrevLayout.preferredWidth = 34f;
        tradePrevLayout.preferredHeight = 34f;
        economyScreenUi.TradePrevButton.onClick.AddListener(() =>
        {
            selectedTradeResourceType = GetAdjacentTradeResource(selectedTradeResourceType, -1);
            isEconomyScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.82f);
        });
        economyScreenUi.TradeResourceText = CreateHeaderText("TradeResourceValue", resourceRow, font, string.Empty, 16, TextAnchor.MiddleCenter, Color.white);
        economyScreenUi.TradeResourceText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        economyScreenUi.TradeNextButton = CreateButton("TradeNextButton", resourceRow, font, out Text tradeNextText, ">", 13, new Color(0.26f, 0.30f, 0.36f, 1f), Color.white);
        tradeNextText.fontStyle = FontStyle.Bold;
        LayoutElement tradeNextLayout = economyScreenUi.TradeNextButton.gameObject.AddComponent<LayoutElement>();
        tradeNextLayout.preferredWidth = 34f;
        tradeNextLayout.preferredHeight = 34f;
        economyScreenUi.TradeNextButton.onClick.AddListener(() =>
        {
            selectedTradeResourceType = GetAdjacentTradeResource(selectedTradeResourceType, 1);
            isEconomyScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.82f);
        });

        RectTransform offerRow = CreateLayoutRow("TradeOfferRow", tradeBody, 30f, 8f);
        CreateBodyText("TradeOfferLabel", offerRow, font, "Offer", 13, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;
        economyScreenUi.TradePriceText = CreateHeaderText("TradePriceValue", offerRow, font, string.Empty, 15, TextAnchor.MiddleLeft, FleetAccentColor);
        economyScreenUi.TradePriceText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        economyScreenUi.TradeEtaText = CreateBodyText("TradeEtaText", offerRow, font, string.Empty, 12, TextAnchor.MiddleRight, FleetSecondaryTextColor);
        economyScreenUi.TradeEtaText.gameObject.AddComponent<LayoutElement>().preferredWidth = 172f;

        RectTransform modeRow = CreateLayoutRow("TradeModeRow", tradeBody, 34f, 8f);
        CreateBodyText("TradeModeLabel", modeRow, font, "Mode", 13, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;
        economyScreenUi.TradeModeButton = CreateButton("TradeModeButton", modeRow, font, out Text tradeModeText, string.Empty, 13, FleetPrimaryButtonColor, Color.white);
        economyScreenUi.TradeModeText = tradeModeText;
        LayoutElement tradeModeLayout = economyScreenUi.TradeModeButton.gameObject.AddComponent<LayoutElement>();
        tradeModeLayout.preferredWidth = 168f;
        tradeModeLayout.preferredHeight = 34f;
        economyScreenUi.TradeModeButton.onClick.AddListener(() =>
        {
            selectedTradeOrderType = selectedTradeOrderType == TradeOrderType.Buy ? TradeOrderType.Sell : TradeOrderType.Buy;
            isEconomyScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.82f);
        });
        RectTransform statusRow = CreateLayoutRow("TradeStatusRow", tradeBody, 32f, 8f);
        CreateBodyText("TradeStatusLabel", statusRow, font, "Status", 13, TextAnchor.MiddleLeft, FleetMutedTextColor)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 72f;
        economyScreenUi.TradeStatusText = CreateBodyText("TradeStatusText", statusRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        economyScreenUi.TradeStatusText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform dispatchRow = CreateLayoutRow("TradeDispatchRow", tradeBody, 34f, 8f);
        economyScreenUi.TradeDispatchButton = CreateButton("TradeDispatchButton", dispatchRow, font, out Text tradeDispatchButtonText, "Send on Trade Run", 13, FleetPrimaryButtonColor, Color.white);
        economyScreenUi.TradeDispatchButtonText = tradeDispatchButtonText;
        LayoutElement tradeDispatchLayout = economyScreenUi.TradeDispatchButton.gameObject.AddComponent<LayoutElement>();
        tradeDispatchLayout.flexibleWidth = 1f;
        tradeDispatchLayout.preferredHeight = 34f;
        economyScreenUi.TradeDispatchButton.onClick.AddListener(HandleTradeDispatchRequested);

        RectTransform ledgerFrame = CreateStyledPanel("EconomyLedgerFrame", windowRoot.transform, FleetInsetColor);
        LayoutElement frameLayout = ledgerFrame.gameObject.AddComponent<LayoutElement>();
        frameLayout.flexibleHeight = 1f;
        frameLayout.minHeight = 250f;

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

        EnsureTradeSelectionMatchesCurrentZone();
        economyScreenUi.HeaderCountText.text = "Economy & Trade";
        economyScreenUi.TradeResourceText.text = GetTradeResourceLabel(selectedTradeResourceType);
        economyScreenUi.TradeModeText.text = GetTradeModeLabel(selectedTradeOrderType);
        economyScreenUi.TradePriceText.text = GetTradeOfferLabel(selectedTradeResourceType, selectedTradeOrderType);
        economyScreenUi.TradeEtaText.text = GetTradeEtaLabel();
        economyScreenUi.TradeStatusText.text = BuildTradeDispatchStatusText();
        bool tradeRunActive = HasActiveTradeRun();
        economyScreenUi.TradeDispatchButton.interactable = CanDispatchTradeRun();
        economyScreenUi.TradePrevButton.interactable = !tradeRunActive && GetTradeCatalogForCurrentZone(selectedTradeOrderType).Length > 1;
        economyScreenUi.TradeNextButton.interactable = !tradeRunActive && GetTradeCatalogForCurrentZone(selectedTradeOrderType).Length > 1;
        economyScreenUi.TradeModeButton.interactable = !tradeRunActive;
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

    private TradeResourceType GetAdjacentTradeResource(TradeResourceType current, int direction)
    {
        TradeResourceType[] values = GetTradeCatalogForCurrentZone(selectedTradeOrderType);
        if (values.Length == 0)
        {
            return current;
        }

        int currentIndex = System.Array.IndexOf(values, current);
        if (currentIndex < 0)
        {
            return values[0];
        }

        currentIndex = (currentIndex + direction + values.Length) % values.Length;
        return values[currentIndex];
    }

    private string GetTradeResourceLabel(TradeResourceType resourceType)
    {
        return resourceType switch
        {
            TradeResourceType.Logs => "Logs",
            TradeResourceType.Boards => "Boards",
            TradeResourceType.Cotton => "Cotton",
            TradeResourceType.Textile => "Textile",
            TradeResourceType.Furniture => "Furniture",
            _ => resourceType.ToString()
        };
    }

    private bool CanDispatchTradeRun()
    {
        return TryGetTradeDispatchContext(out _, out _, out _);
    }

    private string BuildTradeDispatchStatusText()
    {
        if (HasActiveTradeRun())
        {
            return GetTradeRunStatusLabel();
        }

        if (!TryGetTradeDispatchContext(out _, out _, out string blockReason))
        {
            return blockReason;
        }

        if (tradeDispatchStatusText == "Assign an Intercity driver to unlock trade dispatch.")
        {
            return "Ready to dispatch via edge highway";
        }

        return tradeDispatchStatusText;
    }

    private void HandleTradeDispatchRequested()
    {
        if (!BeginTradeRun())
        {
            tradeDispatchStatusText = BuildTradeDispatchStatusText();
            isEconomyScreenDirty = true;
            return;
        }

        PlayUiSound(uiSelectClip, 0.88f);
        isEconomyScreenDirty = true;
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
