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
        bool forceLayoutRebuild = false;
        if (resourcesScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            resourcesScreenUi.CanvasRoot.SetActive(shouldShow);
            forceLayoutRebuild = shouldShow;
        }

        if (!shouldShow) return;

        string headerCount = "3 Production Nodes";
        string forestValue = $"{locations[LocationType.Forest].LogsStored} / {ForestMaxLogsStorage} Logs";
        string forestSub = "Raw logs ready for pickup";
        string sawmillValue = $"{locations[LocationType.Sawmill].BoardsStored} Boards";
        string sawmillSub = $"{locations[LocationType.Sawmill].LogsStored} logs waiting for processing";
        string warehouseValue = $"{locations[LocationType.Warehouse].BoardsStored} Boards";
        string warehouseSub = "Finished goods in storage";
        string treasuryValue = $"${money}";

        if (resourcesScreenUi.LastHeaderCount != headerCount)
        {
            resourcesScreenUi.HeaderCountText.text = headerCount;
            resourcesScreenUi.LastHeaderCount = headerCount;
            forceLayoutRebuild = true;
        }

        if (resourcesScreenUi.LastForestValue != forestValue)
        {
            resourcesScreenUi.ForestValueText.text = forestValue;
            resourcesScreenUi.LastForestValue = forestValue;
            forceLayoutRebuild = true;
        }

        if (resourcesScreenUi.LastForestSub != forestSub)
        {
            resourcesScreenUi.ForestSubText.text = forestSub;
            resourcesScreenUi.LastForestSub = forestSub;
            forceLayoutRebuild = true;
        }

        if (resourcesScreenUi.LastSawmillValue != sawmillValue)
        {
            resourcesScreenUi.SawmillValueText.text = sawmillValue;
            resourcesScreenUi.LastSawmillValue = sawmillValue;
            forceLayoutRebuild = true;
        }

        if (resourcesScreenUi.LastSawmillSub != sawmillSub)
        {
            resourcesScreenUi.SawmillSubText.text = sawmillSub;
            resourcesScreenUi.LastSawmillSub = sawmillSub;
            forceLayoutRebuild = true;
        }

        if (resourcesScreenUi.LastWarehouseValue != warehouseValue)
        {
            resourcesScreenUi.WarehouseValueText.text = warehouseValue;
            resourcesScreenUi.LastWarehouseValue = warehouseValue;
            forceLayoutRebuild = true;
        }

        if (resourcesScreenUi.LastWarehouseSub != warehouseSub)
        {
            resourcesScreenUi.WarehouseSubText.text = warehouseSub;
            resourcesScreenUi.LastWarehouseSub = warehouseSub;
            forceLayoutRebuild = true;
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
