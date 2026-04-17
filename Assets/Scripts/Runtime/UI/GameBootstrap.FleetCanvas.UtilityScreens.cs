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
        SetCenteredWindow(windowRect, 620f, 610f, -16f);
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
        toolCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 480f;
        VerticalLayoutGroup toolBodyLayout = toolBody.gameObject.GetComponent<VerticalLayoutGroup>();
        toolBodyLayout.spacing = 14f;
        toolBodyLayout.childControlWidth = true;
        toolBodyLayout.childControlHeight = true;
        toolBodyLayout.childForceExpandWidth = true;
        toolBodyLayout.childForceExpandHeight = false;

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

        RectTransform sawmillRow = CreateUiObject("BuildSawmillRow", toolBody).GetComponent<RectTransform>();
        HorizontalLayoutGroup sawmillLayout = sawmillRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        sawmillLayout.spacing = 14;
        sawmillLayout.childControlWidth = true;
        sawmillLayout.childControlHeight = true;
        sawmillLayout.childForceExpandWidth = false;
        sawmillLayout.childForceExpandHeight = false;

        buildScreenUi.SawmillButton = CreateButton("BuildSawmillButton", sawmillRow, font, out Text sawmillButtonText, "SAWMILL", 16, FleetPrimaryButtonColor, Color.white);
        buildScreenUi.SawmillButtonText = sawmillButtonText;
        LayoutElement sawmillButtonLayout = buildScreenUi.SawmillButton.gameObject.AddComponent<LayoutElement>();
        sawmillButtonLayout.preferredWidth = 96f;
        sawmillButtonLayout.preferredHeight = 56f;
        buildScreenUi.SawmillButton.onClick.AddListener(() =>
        {
            bool sawmillModeActive = activeBuildTool == BuildTool.Sawmill;
            activeBuildTool = sawmillModeActive ? BuildTool.None : BuildTool.Sawmill;
            isBuildPanelOpen = false;
            LogUiInput($"Build Canvas: switched tool to {activeBuildTool}");
            PlayUiSound(uiSelectClip, 0.85f);
            SessionDebugLogger.Log("BUILD", $"Build tool switched to {activeBuildTool}.");
            isBuildScreenDirty = true;
        });

        RectTransform sawmillInfo = CreateUiObject("SawmillInfo", sawmillRow).GetComponent<RectTransform>();
        sawmillInfo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup sawmillInfoLayout = sawmillInfo.gameObject.AddComponent<VerticalLayoutGroup>();
        sawmillInfoLayout.spacing = 6;
        sawmillInfoLayout.childControlWidth = true;
        sawmillInfoLayout.childControlHeight = true;
        sawmillInfoLayout.childForceExpandWidth = true;
        sawmillInfoLayout.childForceExpandHeight = false;

        buildScreenUi.SawmillTitleText = CreateHeaderText("SawmillTitle", sawmillInfo, font, "Sawmill", 18, TextAnchor.MiddleLeft, Color.white);
        buildScreenUi.SawmillDescriptionText = CreateBodyText("SawmillDescription", sawmillInfo, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);

        RectTransform motelRow = CreateUiObject("BuildMotelRow", toolBody).GetComponent<RectTransform>();
        HorizontalLayoutGroup motelLayout = motelRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        motelLayout.spacing = 14;
        motelLayout.childControlWidth = true;
        motelLayout.childControlHeight = true;
        motelLayout.childForceExpandWidth = false;
        motelLayout.childForceExpandHeight = false;

        buildScreenUi.MotelButton = CreateButton("BuildMotelButton", motelRow, font, out Text motelButtonText, "MOTEL", 16, FleetPrimaryButtonColor, Color.white);
        buildScreenUi.MotelButtonText = motelButtonText;
        LayoutElement motelButtonLayout = buildScreenUi.MotelButton.gameObject.AddComponent<LayoutElement>();
        motelButtonLayout.preferredWidth = 96f;
        motelButtonLayout.preferredHeight = 56f;
        buildScreenUi.MotelButton.onClick.AddListener(() =>
        {
            bool motelModeActive = activeBuildTool == BuildTool.Motel;
            activeBuildTool = motelModeActive ? BuildTool.None : BuildTool.Motel;
            isBuildPanelOpen = false;
            LogUiInput($"Build Canvas: switched tool to {activeBuildTool}");
            PlayUiSound(uiSelectClip, 0.85f);
            SessionDebugLogger.Log("BUILD", $"Build tool switched to {activeBuildTool}.");
            isBuildScreenDirty = true;
        });

        RectTransform motelInfo = CreateUiObject("MotelInfo", motelRow).GetComponent<RectTransform>();
        motelInfo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup motelInfoLayout = motelInfo.gameObject.AddComponent<VerticalLayoutGroup>();
        motelInfoLayout.spacing = 6;
        motelInfoLayout.childControlWidth = true;
        motelInfoLayout.childControlHeight = true;
        motelInfoLayout.childForceExpandWidth = true;
        motelInfoLayout.childForceExpandHeight = false;

        buildScreenUi.MotelTitleText = CreateHeaderText("MotelTitle", motelInfo, font, "Motel", 18, TextAnchor.MiddleLeft, Color.white);
        buildScreenUi.MotelDescriptionText = CreateBodyText("MotelDescription", motelInfo, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);

        RectTransform factoryRow = CreateUiObject("BuildFurnitureFactoryRow", toolBody).GetComponent<RectTransform>();
        HorizontalLayoutGroup factoryLayout = factoryRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        factoryLayout.spacing = 14;
        factoryLayout.childControlWidth = true;
        factoryLayout.childControlHeight = true;
        factoryLayout.childForceExpandWidth = false;
        factoryLayout.childForceExpandHeight = false;

        buildScreenUi.FurnitureFactoryButton = CreateButton("BuildFurnitureFactoryButton", factoryRow, font, out Text factoryButtonText, "FACTORY", 16, FleetPrimaryButtonColor, Color.white);
        buildScreenUi.FurnitureFactoryButtonText = factoryButtonText;
        LayoutElement factoryButtonLayout = buildScreenUi.FurnitureFactoryButton.gameObject.AddComponent<LayoutElement>();
        factoryButtonLayout.preferredWidth = 96f;
        factoryButtonLayout.preferredHeight = 56f;
        buildScreenUi.FurnitureFactoryButton.onClick.AddListener(() =>
        {
            bool factoryModeActive = activeBuildTool == BuildTool.FurnitureFactory;
            activeBuildTool = factoryModeActive ? BuildTool.None : BuildTool.FurnitureFactory;
            isBuildPanelOpen = false;
            LogUiInput($"Build Canvas: switched tool to {activeBuildTool}");
            PlayUiSound(uiSelectClip, 0.85f);
            SessionDebugLogger.Log("BUILD", $"Build tool switched to {activeBuildTool}.");
            isBuildScreenDirty = true;
        });

        RectTransform factoryInfo = CreateUiObject("FurnitureFactoryInfo", factoryRow).GetComponent<RectTransform>();
        factoryInfo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup factoryInfoLayout = factoryInfo.gameObject.AddComponent<VerticalLayoutGroup>();
        factoryInfoLayout.spacing = 6;
        factoryInfoLayout.childControlWidth = true;
        factoryInfoLayout.childControlHeight = true;
        factoryInfoLayout.childForceExpandWidth = true;
        factoryInfoLayout.childForceExpandHeight = false;

        buildScreenUi.FurnitureFactoryTitleText = CreateHeaderText("FurnitureFactoryTitle", factoryInfo, font, "Furniture Factory", 18, TextAnchor.MiddleLeft, Color.white);
        buildScreenUi.FurnitureFactoryDescriptionText = CreateBodyText("FurnitureFactoryDescription", factoryInfo, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);

        RectTransform barRow = CreateUiObject("BuildBarRow", toolBody).GetComponent<RectTransform>();
        HorizontalLayoutGroup barLayout = barRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        barLayout.spacing = 14;
        barLayout.childControlWidth = true;
        barLayout.childControlHeight = true;
        barLayout.childForceExpandWidth = false;
        barLayout.childForceExpandHeight = false;

        buildScreenUi.BarButton = CreateButton("BuildBarButton", barRow, font, out Text barButtonText, "BAR", 16, FleetPrimaryButtonColor, Color.white);
        buildScreenUi.BarButtonText = barButtonText;
        LayoutElement barButtonLayout = buildScreenUi.BarButton.gameObject.AddComponent<LayoutElement>();
        barButtonLayout.preferredWidth = 96f;
        barButtonLayout.preferredHeight = 56f;
        buildScreenUi.BarButton.onClick.AddListener(() =>
        {
            bool barModeActive = activeBuildTool == BuildTool.Bar;
            activeBuildTool = barModeActive ? BuildTool.None : BuildTool.Bar;
            isBuildPanelOpen = false;
            LogUiInput($"Build Canvas: switched tool to {activeBuildTool}");
            PlayUiSound(uiSelectClip, 0.85f);
            SessionDebugLogger.Log("BUILD", $"Build tool switched to {activeBuildTool}.");
            isBuildScreenDirty = true;
        });

        RectTransform barInfo = CreateUiObject("BarInfo", barRow).GetComponent<RectTransform>();
        barInfo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup barInfoLayout = barInfo.gameObject.AddComponent<VerticalLayoutGroup>();
        barInfoLayout.spacing = 6;
        barInfoLayout.childControlWidth = true;
        barInfoLayout.childControlHeight = true;
        barInfoLayout.childForceExpandWidth = true;
        barInfoLayout.childForceExpandHeight = false;

        buildScreenUi.BarTitleText = CreateHeaderText("BarTitle", barInfo, font, "Bar", 18, TextAnchor.MiddleLeft, Color.white);
        buildScreenUi.BarDescriptionText = CreateBodyText("BarDescription", barInfo, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);

        RectTransform canteenRow = CreateUiObject("BuildCanteenRow", toolBody).GetComponent<RectTransform>();
        HorizontalLayoutGroup canteenLayout = canteenRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        canteenLayout.spacing = 14;
        canteenLayout.childControlWidth = true;
        canteenLayout.childControlHeight = true;
        canteenLayout.childForceExpandWidth = false;
        canteenLayout.childForceExpandHeight = false;

        buildScreenUi.CanteenButton = CreateButton("BuildCanteenButton", canteenRow, font, out Text canteenButtonText, "CANTEEN", 16, FleetPrimaryButtonColor, Color.white);
        buildScreenUi.CanteenButtonText = canteenButtonText;
        LayoutElement canteenButtonLayout = buildScreenUi.CanteenButton.gameObject.AddComponent<LayoutElement>();
        canteenButtonLayout.preferredWidth = 96f;
        canteenButtonLayout.preferredHeight = 56f;
        buildScreenUi.CanteenButton.onClick.AddListener(() =>
        {
            bool canteenModeActive = activeBuildTool == BuildTool.Canteen;
            activeBuildTool = canteenModeActive ? BuildTool.None : BuildTool.Canteen;
            isBuildPanelOpen = false;
            LogUiInput($"Build Canvas: switched tool to {activeBuildTool}");
            PlayUiSound(uiSelectClip, 0.85f);
            SessionDebugLogger.Log("BUILD", $"Build tool switched to {activeBuildTool}.");
            isBuildScreenDirty = true;
        });

        RectTransform canteenInfo = CreateUiObject("CanteenInfo", canteenRow).GetComponent<RectTransform>();
        canteenInfo.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup canteenInfoLayout = canteenInfo.gameObject.AddComponent<VerticalLayoutGroup>();
        canteenInfoLayout.spacing = 6;
        canteenInfoLayout.childControlWidth = true;
        canteenInfoLayout.childControlHeight = true;
        canteenInfoLayout.childForceExpandWidth = true;
        canteenInfoLayout.childForceExpandHeight = false;

        buildScreenUi.CanteenTitleText = CreateHeaderText("CanteenTitle", canteenInfo, font, "Canteen", 18, TextAnchor.MiddleLeft, Color.white);
        buildScreenUi.CanteenDescriptionText = CreateBodyText("CanteenDescription", canteenInfo, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);

        AddOverlayCloseButton(windowRect, font);
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
        bool factoryModeActive = activeBuildTool == BuildTool.FurnitureFactory;
        bool sawmillModeActive = activeBuildTool == BuildTool.Sawmill;
        bool motelModeActive = activeBuildTool == BuildTool.Motel;
        bool barModeActive = activeBuildTool == BuildTool.Bar;
        bool canteenModeActive = activeBuildTool == BuildTool.Canteen;
        Image roadButtonImage = buildScreenUi.RoadButton.GetComponent<Image>();
        if (roadButtonImage != null)
        {
            roadButtonImage.color = roadModeActive ? FleetAccentColor : FleetPrimaryButtonColor;
        }

        Image sawmillButtonImage = buildScreenUi.SawmillButton.GetComponent<Image>();
        if (sawmillButtonImage != null)
        {
            sawmillButtonImage.color = sawmillModeActive ? FleetAccentColor : FleetPrimaryButtonColor;
        }

        Image motelButtonImage = buildScreenUi.MotelButton.GetComponent<Image>();
        if (motelButtonImage != null)
        {
            motelButtonImage.color = motelModeActive ? FleetAccentColor : FleetPrimaryButtonColor;
        }

        Image factoryButtonImage = buildScreenUi.FurnitureFactoryButton.GetComponent<Image>();
        if (factoryButtonImage != null)
        {
            factoryButtonImage.color = factoryModeActive ? FleetAccentColor : FleetPrimaryButtonColor;
        }

        Image canteenButtonImage = buildScreenUi.CanteenButton.GetComponent<Image>();
        if (canteenButtonImage != null)
        {
            canteenButtonImage.color = canteenModeActive ? FleetAccentColor : FleetPrimaryButtonColor;
        }

        buildScreenUi.RoadButtonText.text = "ROAD";
        buildScreenUi.RoadTitleText.text = "Road";
        buildScreenUi.RoadDescriptionText.text = roadModeActive
            ? "Mode active: left click builds, right click removes."
            : "Click to enter road building mode.";
        buildScreenUi.SawmillButtonText.text = "SAWMILL";
        buildScreenUi.SawmillTitleText.text = "Sawmill";
        buildScreenUi.SawmillDescriptionText.text = sawmillModeActive
            ? $"Mode active: place one 2x2 sawmill from its driveway cell. R rotates ({GetBuildRotationLabel()})."
            : locations.ContainsKey(LocationType.Sawmill)
                ? "Already built on this map."
                : "Place a 2x2 production building that turns logs into boards.";
        buildScreenUi.MotelButtonText.text = "MOTEL";
        buildScreenUi.MotelTitleText.text = "Motel";
        buildScreenUi.MotelDescriptionText.text = motelModeActive
            ? $"Mode active: place one 2x2 motel from its driveway cell. R rotates ({GetBuildRotationLabel()})."
            : locations.ContainsKey(LocationType.Motel)
                ? "Already built on this map."
                : "Place a 2x2 driver hub. Drivers can idle and be hired after it exists.";
        buildScreenUi.FurnitureFactoryButtonText.text = "FACTORY";
        buildScreenUi.FurnitureFactoryTitleText.text = "Furniture Factory";
        buildScreenUi.FurnitureFactoryDescriptionText.text = factoryModeActive
            ? $"Mode active: place one 3x2 furniture factory from its driveway cell. R rotates ({GetBuildRotationLabel()})."
            : locations.ContainsKey(LocationType.FurnitureFactory)
                ? "Already built on this map."
                : "Place a 3x2 factory that turns 1 Board + 1 Textile into 1 Furniture.";

        Image barButtonImage = buildScreenUi.BarButton.GetComponent<Image>();
        if (barButtonImage != null)
        {
            barButtonImage.color = barModeActive ? FleetAccentColor : FleetPrimaryButtonColor;
        }

        buildScreenUi.BarButtonText.text = "BAR";
        buildScreenUi.BarTitleText.text = "Bar";
        buildScreenUi.BarDescriptionText.text = barModeActive
            ? $"Mode active: place bar from its driveway cell. R rotates ({GetBuildRotationLabel()})."
            : locations.ContainsKey(LocationType.Bar)
                ? "Already built on this map."
                : "Social hub вЂ” idle drivers gather here to rest.";

        buildScreenUi.CanteenButtonText.text = "CANTEEN";
        buildScreenUi.CanteenTitleText.text = "Canteen";
        buildScreenUi.CanteenDescriptionText.text = canteenModeActive
            ? $"Mode active: place one 2x2 canteen from its driveway cell. R rotates ({GetBuildRotationLabel()})."
            : locations.ContainsKey(LocationType.Canteen)
                ? "Already built on this map."
                : "Service building: visiting drivers/workers pay $10 for a quick meal.";

        LayoutRebuilder.ForceRebuildLayoutImmediate(buildScreenUi.WindowRoot);
        LocalizeCanvas(buildScreenUi.CanvasRoot);
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
        SetCenteredWindow(windowRect, 400f, 580f, -16f);
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

        RectTransform headerRow = CreateLayoutRow("ResourcesHeaderRow", windowRoot.transform, 34f, 0f);
        Text resourcesTitleText = CreateHeaderText("ResourcesTitle", headerRow, font, "Resources", 20, TextAnchor.MiddleLeft, Color.white);
        resourcesTitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        resourcesScreenUi.HeaderCountText = CreateHeaderText("ResourcesHeaderCount", headerRow, font, string.Empty, 11, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        // Scroll view вЂ” stretches to fill remaining height
        GameObject scrollGo = CreateUiObject("ResourcesScrollView", windowRoot.transform);
        RectTransform scrollRect = scrollGo.GetComponent<RectTransform>();
        scrollGo.AddComponent<LayoutElement>().flexibleHeight = 1f;
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scrollGo.AddComponent<RectMask2D>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        scroll.inertia = false;

        // Content inside scroll вЂ” auto-sizes vertically
        GameObject contentGo = CreateUiObject("ResourcesContentRoot", scrollGo.transform);
        RectTransform contentRoot = contentGo.GetComponent<RectTransform>();
        contentRoot.anchorMin = new Vector2(0f, 1f);
        contentRoot.anchorMax = new Vector2(1f, 1f);
        contentRoot.pivot = new Vector2(0.5f, 1f);
        contentRoot.anchoredPosition = Vector2.zero;
        contentRoot.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentGroup = contentGo.AddComponent<VerticalLayoutGroup>();
        contentGroup.spacing = 6;
        contentGroup.childControlWidth = true;
        contentGroup.childControlHeight = true;
        contentGroup.childForceExpandWidth = true;
        contentGroup.childForceExpandHeight = false;
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRoot;

        CreateResourceSummaryRow(contentRoot, font, "Logs",      ResourceVisualKind.Logs,      TradeResourceType.Logs,      resourcesScreenUi.Rows);
        CreateResourceSummaryRow(contentRoot, font, "Boards",    ResourceVisualKind.Boards,    TradeResourceType.Boards,    resourcesScreenUi.Rows);
        CreateResourceSummaryRow(contentRoot, font, "Cotton",    ResourceVisualKind.Cotton,    TradeResourceType.Cotton,    resourcesScreenUi.Rows);
        CreateResourceSummaryRow(contentRoot, font, "Textile",   ResourceVisualKind.Textile,   TradeResourceType.Textile,   resourcesScreenUi.Rows);
        CreateResourceSummaryRow(contentRoot, font, "Furniture", ResourceVisualKind.Furniture, TradeResourceType.Furniture, resourcesScreenUi.Rows);
        CreateResourceSummaryRow(contentRoot, font, "Fuel",      ResourceVisualKind.Fuel,      resourcesScreenUi.Rows);
        CreateResourceSummaryRow(contentRoot, font, "Alcohol",   ResourceVisualKind.Alcohol,   resourcesScreenUi.Rows);
        CreateResourceSummaryRow(contentRoot, font, "Food",      ResourceVisualKind.Food,       resourcesScreenUi.Rows);

        RectTransform footerCard = CreateSectionCard(windowRoot.transform, font, "Treasury", out RectTransform footerBody);
        footerCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
        resourcesScreenUi.TreasuryValueText = CreateHeaderText("TreasuryValue", footerBody, font, string.Empty, 16, TextAnchor.MiddleLeft, FleetAccentColor);

        AddOverlayCloseButton(windowRect, font);
        resourcesScreenUi.CanvasRoot.SetActive(false);
        UpdateResourcesScreenUi();
    }

    private enum ResourceVisualKind
    {
        Logs,
        Boards,
        Cotton,
        Textile,
        Furniture,
        Fuel,
        Alcohol,
        Food
    }

    private void CreateResourceSummaryRow(RectTransform parent, Font font, string title, ResourceVisualKind iconKind, List<ResourceSummaryRowUi> rows)
    {
        CreateResourceSummaryRow(parent, font, title, iconKind, TradeResourceType.Logs, rows);
    }

    private void CreateResourceSummaryRow(RectTransform parent, Font font, string title, ResourceVisualKind iconKind, TradeResourceType resourceType, List<ResourceSummaryRowUi> rows)
    {
        RectTransform card = CreateStyledPanel($"{title}RowCard", parent, FleetInsetColor);
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;

        HorizontalLayoutGroup layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        RectTransform iconRoot = CreateUiObject($"{title}IconRoot", card).GetComponent<RectTransform>();
        LayoutElement iconLayout = iconRoot.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 28f;
        iconLayout.preferredHeight = 28f;
        Image iconBackground = iconRoot.gameObject.AddComponent<Image>();
        iconBackground.color = new Color(1f, 1f, 1f, 0.06f);
        DrawResourceIcon(iconRoot, iconKind);

        RectTransform textRoot = CreateUiObject($"{title}TextRoot", card).GetComponent<RectTransform>();
        LayoutElement textLayout = textRoot.gameObject.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1f;
        VerticalLayoutGroup textGroup = textRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        textGroup.spacing = 1f;
        textGroup.childControlWidth = true;
        textGroup.childControlHeight = true;
        textGroup.childForceExpandWidth = true;
        textGroup.childForceExpandHeight = false;

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
            case ResourceVisualKind.Fuel:
                // pump body
                CreateIconBar(parent, new Vector2(12f, 16f), new Vector2(-3f, -2f), new Color(0.30f, 0.36f, 0.44f));
                // arm
                CreateIconBar(parent, new Vector2(7f, 3f), new Vector2(4f, 5f), new Color(0.45f, 0.52f, 0.60f));
                // nozzle
                CreateIconBar(parent, new Vector2(3f, 9f), new Vector2(7f, 0f), new Color(0.45f, 0.52f, 0.60f));
                // fuel fill line
                CreateIconBar(parent, new Vector2(8f, 3f), new Vector2(-3f, -4f), new Color(0.98f, 0.80f, 0.20f));
                break;
            case ResourceVisualKind.Alcohol:
                // bottle body
                CreateIconBar(parent, new Vector2(10f, 14f), new Vector2(0f, -4f), new Color(0.28f, 0.58f, 0.36f));
                // bottle neck
                CreateIconBar(parent, new Vector2(5f, 6f), new Vector2(0f, 7f), new Color(0.24f, 0.50f, 0.30f));
                // cap
                CreateIconBar(parent, new Vector2(7f, 3f), new Vector2(0f, 11f), new Color(0.60f, 0.40f, 0.20f));
                break;
            case ResourceVisualKind.Food:
                // plate
                CreateIconCircle(parent, 20f, new Vector2(0f, -2f), new Color(0.90f, 0.86f, 0.78f));
                // food on plate
                CreateIconCircle(parent, 12f, new Vector2(0f, -2f), new Color(0.94f, 0.68f, 0.32f));
                // bread top
                CreateIconCircle(parent, 7f, new Vector2(0f, 2f), new Color(0.82f, 0.54f, 0.20f));
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
        locations.TryGetValue(LocationType.Warehouse, out LocationData warehouseData);
        string[] resourceNames =
        {
            GetTradeResourceLabel(TradeResourceType.Logs),
            GetTradeResourceLabel(TradeResourceType.Boards),
            GetTradeResourceLabel(TradeResourceType.Cotton),
            GetTradeResourceLabel(TradeResourceType.Textile),
            GetTradeResourceLabel(TradeResourceType.Furniture),
            "Fuel",
            "Alcohol",
            "Food"
        };
        string[] resourceValues =
        {
            (warehouseData?.LogsStored ?? 0).ToString(),
            (warehouseData?.BoardsStored ?? 0).ToString(),
            cottonStored.ToString(),
            textileStored.ToString(),
            furnitureStored.ToString(),
            $"{warehouseData?.FuelStored ?? 0} / {WarehouseMaxFuelStorage}",
            $"{warehouseData?.AlcoholStored ?? 0} / {WarehouseMaxAlcoholStorage}",
            $"{warehouseData?.FoodStored ?? 0} / {WarehouseMaxFoodStorage}"
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

        LocalizeCanvas(resourcesScreenUi.CanvasRoot);
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
        foreach (TruckAgent truck in truckAgents)
        {
            if (truck.TruckCargoType == CargoType.Boards && !IsTruckOnActiveTradeSellRun(truck))
                total += truck.TruckCargoAmount;
        }
        return total;
    }

    private int GetTotalFuelAmount()
    {
        int total = 0;
        if (locations.TryGetValue(LocationType.Warehouse, out LocationData wh)) total += wh.FuelStored;
        if (locations.TryGetValue(LocationType.GasStation, out LocationData gs)) total += gs.FuelStored;
        return total;
    }

    private int GetTotalAlcoholAmount()
    {
        int total = 0;
        if (locations.TryGetValue(LocationType.Warehouse, out LocationData wh)) total += wh.AlcoholStored;
        if (locations.TryGetValue(LocationType.Bar, out LocationData bar)) total += bar.AlcoholStored;
        return total;
    }

    private int GetTotalFoodAmount()
    {
        int total = 0;
        if (locations.TryGetValue(LocationType.Warehouse, out LocationData wh)) total += wh.FoodStored;
        if (locations.TryGetValue(LocationType.Canteen, out LocationData canteen)) total += canteen.FoodStored;
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
        TradeResourceType.Fuel,
        TradeResourceType.Alcohol,
        TradeResourceType.Food,
    };

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
        SetCenteredWindow(windowRect, 840f, 620f, -16f);
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

        RectTransform headerRow = CreateLayoutRow("EconomyHeaderRow", windowRoot.transform, 48f, 0f);
        Text titleText = CreateHeaderText("EconomyTitle", headerRow, font, "Trade", 30, TextAnchor.MiddleLeft, Color.white);
        titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        economyScreenUi.HeaderCountText = CreateHeaderText("EconomyCount", headerRow, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        RectTransform tradeCard = CreateSectionCard(windowRoot.transform, font, "Create Order", out RectTransform tradeBody);
        tradeCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 236f;

        RectTransform resourceRow = CreateLayoutRow("TradeResourceRow", tradeBody, 38f, 8f);
        resourceRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        CreateBodyText("TradeResourceLabel", resourceRow, font, "Resource:", 15, TextAnchor.MiddleLeft, Color.white)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 118f;
        economyScreenUi.TradeResourceDropdownButton = CreateButton("TradeResourceDropdown", resourceRow, font, out Text tradeResourceText, string.Empty, 16, new Color(0.16f, 0.19f, 0.25f, 1f), Color.white);
        economyScreenUi.TradeResourceText = tradeResourceText;
        economyScreenUi.TradeResourceDropdownButton.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        economyScreenUi.TradeResourceDropdownButton.onClick.AddListener(() =>
        {
            isTradeResourceDropdownOpen = !isTradeResourceDropdownOpen;
            isTradeActionDropdownOpen = false;
            isEconomyScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.82f);
        });

        // Resource picker — separate side panel to the right of the trade window
        {
            GameObject pickerObj = CreateUiObject("TradeResourcePickerPanel", canvasObject.transform);
            RectTransform pickerRect = pickerObj.GetComponent<RectTransform>();
            // Anchored to canvas centre, pivot at left-middle — sits just right of the 840px window
            pickerRect.anchorMin = pickerRect.anchorMax = new Vector2(0.5f, 0.5f);
            pickerRect.pivot = new Vector2(0f, 0.5f);
            pickerRect.anchoredPosition = new Vector2(430f, -16f);
            pickerRect.sizeDelta = new Vector2(232f, 0f);   // height driven by ContentSizeFitter
            Image pickerBg = pickerObj.AddComponent<Image>();
            pickerBg.color = DriversScreenTint;
            Outline pickerOutline = pickerObj.AddComponent<Outline>();
            pickerOutline.effectColor = new Color(0f, 0f, 0f, 0.32f);
            pickerOutline.effectDistance = new Vector2(2f, -2f);
            VerticalLayoutGroup pickerLayout = pickerObj.AddComponent<VerticalLayoutGroup>();
            pickerLayout.padding = new RectOffset(0, 0, 0, 8);
            pickerLayout.spacing = 2;
            pickerLayout.childControlWidth = true;
            pickerLayout.childControlHeight = true;
            pickerLayout.childForceExpandWidth = true;
            pickerLayout.childForceExpandHeight = false;
            pickerObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header bar
            GameObject headerObj = CreateUiObject("PickerHeader", pickerObj.transform);
            headerObj.AddComponent<LayoutElement>().preferredHeight = 36f;
            headerObj.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 1f);
            Text headerLbl = CreateHeaderText("PickerTitle", headerObj.transform, font, "Resource", 13, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            StretchRect(headerLbl.rectTransform, 14f, 0f, 14f, 0f);

            // One row per resource
            for (int i = 0; i < TradeHudResources.Length; i++)
            {
                TradeResourceType res = TradeHudResources[i];
                ResourceVisualKind iconKind = (ResourceVisualKind)i;

                Button rowBtn = CreateButton($"ResPickerRow{i}", pickerObj.transform, font, out Text rowLabel, string.Empty, 13, new Color(0.11f, 0.14f, 0.19f, 1f), Color.white);
                rowBtn.transition = Selectable.Transition.None;
                rowBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
                rowLabel.text = L(GetTradeResourceShortLabel(res));
                rowLabel.alignment = TextAnchor.MiddleLeft;

                // Icon
                RectTransform iconRoot = CreateUiObject($"ResPickerIcon{i}", rowBtn.transform).GetComponent<RectTransform>();
                iconRoot.anchorMin = new Vector2(0f, 0.5f);
                iconRoot.anchorMax = new Vector2(0f, 0.5f);
                iconRoot.pivot = new Vector2(0f, 0.5f);
                iconRoot.anchoredPosition = new Vector2(10f, 0f);
                iconRoot.sizeDelta = new Vector2(24f, 24f);
                iconRoot.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
                DrawResourceIcon(iconRoot, iconKind);

                // Offset label right of icon, leave room for amount on the right
                rowLabel.rectTransform.offsetMin = new Vector2(42f, 0f);
                rowLabel.rectTransform.offsetMax = new Vector2(-68f, 0f);

                // Amount text — right-aligned inside row
                Text amountTxt = CreateBodyText($"ResPickerAmt{i}", rowBtn.transform, font, string.Empty, 12, TextAnchor.MiddleRight, FleetSecondaryTextColor);
                amountTxt.rectTransform.anchorMin = new Vector2(1f, 0f);
                amountTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                amountTxt.rectTransform.pivot     = new Vector2(1f, 0.5f);
                amountTxt.rectTransform.anchoredPosition = new Vector2(-10f, 0f);
                amountTxt.rectTransform.sizeDelta  = new Vector2(60f, 0f);

                economyScreenUi.TradeResourceOptionButtons.Add(rowBtn);
                economyScreenUi.TradeResourceOptionTexts.Add(rowLabel);
                economyScreenUi.TradeResourceOptionAmountTexts.Add(amountTxt);

                rowBtn.onClick.AddListener(() =>
                {
                    selectedTradeResourceType = res;
                    isTradeResourceDropdownOpen = false;
                    isEconomyScreenDirty = true;
                    PlayUiSound(uiSelectClip, 0.82f);
                });
            }

            pickerObj.SetActive(false);
            economyScreenUi.TradeResourceOptionsPanel = pickerRect;
        }

        RectTransform actionRow = CreateLayoutRow("TradeActionRow", tradeBody, 38f, 8f);
        actionRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        CreateBodyText("TradeActionLabel", actionRow, font, "Action:", 15, TextAnchor.MiddleLeft, Color.white)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 118f;

        // [<] [Купить / Продать] [>] cycler — no dropdown needed
        Button prevActionBtn = CreateButton("TradeActionPrev", actionRow, font, out Text prevActionTxt, "<", 16, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        prevActionTxt.fontStyle = FontStyle.Bold;
        prevActionBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 38f;
        prevActionBtn.onClick.AddListener(() => CycleTradeOrderType(-1));

        economyScreenUi.TradeActionDropdownButton = CreateButton("TradeActionDisplay", actionRow, font, out Text tradeActionText, string.Empty, 15, new Color(0.12f, 0.15f, 0.20f, 1f), Color.white);
        economyScreenUi.TradeActionDropdownButton.transition = Selectable.Transition.None;
        economyScreenUi.TradeActionText = tradeActionText;
        economyScreenUi.TradeActionDropdownButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 152f;

        Button nextActionBtn = CreateButton("TradeActionNext", actionRow, font, out Text nextActionTxt, ">", 16, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        nextActionTxt.fontStyle = FontStyle.Bold;
        nextActionBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 38f;
        nextActionBtn.onClick.AddListener(() => CycleTradeOrderType(1));

        economyScreenUi.TradeActionOptionsPanel = null;  // no dropdown panel

        // Spacer pushes amount stepper to right edge
        CreateUiObject("AmountSpacer", actionRow).AddComponent<LayoutElement>().flexibleWidth = 1f;
        economyScreenUi.TradeAmountMinusButton = CreateButton("TradeAmountMinus", actionRow, font, out Text minusText, "-", 18, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        minusText.fontStyle = FontStyle.Bold;
        economyScreenUi.TradeAmountMinusButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;
        economyScreenUi.TradeAmountMinusButton.onClick.AddListener(() =>
        {
            selectedTradeOrderAmount = Mathf.Max(1, selectedTradeOrderAmount - 1);
            isEconomyScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.75f);
        });
        economyScreenUi.TradeAmountText = CreateHeaderText("TradeAmountValue", actionRow, font, string.Empty, 18, TextAnchor.MiddleCenter, Color.white);
        economyScreenUi.TradeAmountText.gameObject.AddComponent<LayoutElement>().preferredWidth = 56f;
        economyScreenUi.TradeAmountPlusButton = CreateButton("TradeAmountPlus", actionRow, font, out Text plusText, "+", 18, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        plusText.fontStyle = FontStyle.Bold;
        economyScreenUi.TradeAmountPlusButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;
        economyScreenUi.TradeAmountPlusButton.onClick.AddListener(() =>
        {
            selectedTradeOrderAmount = Mathf.Min(selectedTradeOrderAmount + 1, 5);
            isEconomyScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.75f);
        });

        RectTransform placeOrderRow = CreateLayoutRow("TradePlaceOrderRow", tradeBody, 46f, 0f);
        placeOrderRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        economyScreenUi.TradePlaceOrderButton = CreateButton("TradePlaceOrderButton", placeOrderRow, font, out Text placeOrderText, "PLACE ORDER", 20, new Color(0.24f, 0.64f, 0.10f, 1f), Color.white);
        economyScreenUi.TradePlaceOrderButtonText = placeOrderText;
        placeOrderText.fontStyle = FontStyle.Bold;
        economyScreenUi.TradePlaceOrderButton.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        economyScreenUi.TradePlaceOrderButton.onClick.AddListener(CreateTradeHudOrder);

        RectTransform ordersFrame = CreateSectionCard(windowRoot.transform, font, "Active Orders", out RectTransform ordersBody);
        LayoutElement frameLayout = ordersFrame.gameObject.AddComponent<LayoutElement>();
        frameLayout.flexibleHeight = 1f;
        frameLayout.minHeight = 300f;

        GameObject scrollObj = CreateUiObject("TradeOrdersScrollView", ordersBody);
        RectTransform scrollRoot = scrollObj.GetComponent<RectTransform>();
        scrollRoot.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        Image scrollImage = scrollObj.AddComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0f);
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 28f;

        GameObject viewportObj = CreateUiObject("Viewport", scrollObj.transform);
        StretchRect(viewportObj.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        Image viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.04f);
        viewportObj.AddComponent<Mask>().showMaskGraphic = false;

        GameObject contentObj = CreateUiObject("TradeOrdersContent", viewportObj.transform);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 8f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewportObj.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
        economyScreenUi.ActiveOrdersContent = contentRect;

        economyScreenUi.EmptyOrdersText = CreateBodyText("TradeOrdersEmptyText", contentRect, font, "No active trade orders.", 15, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
        economyScreenUi.EmptyOrdersText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        for (int i = 0; i < MaxEconomyRowSlots; i++)
        {
            TradeOrderRowUi orderRow = CreateTradeOrderRow(contentRect, font, i);
            orderRow.RemoveButton.onClick.AddListener(() =>
            {
                RemoveTradeHudOrder(orderRow.OrderId);
            });
            economyScreenUi.TradeOrderRows.Add(orderRow);
        }

        AddOverlayCloseButton(windowRect, font);
        economyScreenUi.CanvasRoot.SetActive(false);
        UpdateEconomyScreenUi();
    }

    private static void PlaceDropdownBelow(RectTransform anchor, RectTransform dropdown, RectTransform canvasRect)
    {
        Vector3[] corners = new Vector3[4];
        anchor.GetWorldCorners(corners);
        // corners[0] = bottom-left in screen-pixel space (SSO canvas)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, new Vector2(corners[0].x, corners[0].y), null, out Vector2 localPos);
        dropdown.anchoredPosition = localPos;
    }

    private RectTransform CreateTradeDropdownOptionsPanel(string name, Transform parent, Font font, int optionCount, List<Button> buttons, List<Text> texts)
    {
        RectTransform panel = CreateStyledPanel(name, parent, new Color(0.08f, 0.10f, 0.14f, 0.98f));
        LayoutElement layout = panel.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = optionCount * 30f + 8f;
        VerticalLayoutGroup group = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(8, 8, 6, 6);
        group.spacing = 4f;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        for (int i = 0; i < optionCount; i++)
        {
            Button option = CreateButton($"{name}Option{i}", panel, font, out Text optionText, string.Empty, 13, new Color(0.16f, 0.19f, 0.25f, 1f), Color.white);
            option.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;
            buttons.Add(option);
            texts.Add(optionText);
        }

        panel.gameObject.SetActive(false);
        return panel;
    }

    private static TradeOrderRowUi CreateTradeOrderRow(RectTransform parent, Font font, int rowIndex)
    {
        TradeOrderRowUi row = new();
        RectTransform card = CreateStyledPanel($"TradeOrderRow{rowIndex}", parent, FleetInsetColor);
        row.Root = card;
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

        HorizontalLayoutGroup layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.childAlignment = TextAnchor.MiddleCenter;

        RectTransform tagRoot = CreateUiObject($"TradeOrderTag{rowIndex}", card).GetComponent<RectTransform>();
        tagRoot.gameObject.AddComponent<LayoutElement>().preferredWidth = 86f;
        row.TagBackground = tagRoot.gameObject.AddComponent<Image>();
        row.TagBackground.color = new Color(0.23f, 0.62f, 0.10f, 1f);
        row.TagText = CreateHeaderText("TagText", tagRoot, font, "BUY", 16, TextAnchor.MiddleCenter, Color.white);
        StretchRect(row.TagText.rectTransform, 0f, 0f, 0f, 0f);

        row.OrderText = CreateHeaderText($"TradeOrderText{rowIndex}", card, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        row.OrderText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        row.RemoveButton = CreateButton($"TradeOrderRemove{rowIndex}", card, font, out Text removeText, "X", 18, new Color(0.74f, 0.55f, 0.08f, 1f), Color.white);
        removeText.fontStyle = FontStyle.Bold;
        row.RemoveButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 46f;
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

        EnsureTradeHudSelectionValid();
        economyScreenUi.HeaderCountText.text = string.Empty;
        economyScreenUi.TradeResourceText.text = $"{L(GetTradeResourceShortLabel(selectedTradeResourceType))}  ▾";
        economyScreenUi.TradeActionText.text = L(GetTradeModeLabel(selectedTradeOrderType));
        bool isBuyMode = selectedTradeOrderType == TradeOrderType.Buy;
        if (economyScreenUi.TradeActionDropdownButton != null)
            economyScreenUi.TradeActionDropdownButton.image.color = isBuyMode
                ? new Color(0.20f, 0.36f, 0.16f, 1f)
                : new Color(0.40f, 0.16f, 0.14f, 1f);
        economyScreenUi.TradeAmountText.text = selectedTradeOrderAmount.ToString();
        economyScreenUi.TradePlaceOrderButton.interactable = selectedTradeOrderAmount >= 1;
        if (economyScreenUi.TradeResourceOptionsPanel != null)
            economyScreenUi.TradeResourceOptionsPanel.gameObject.SetActive(isTradeResourceDropdownOpen);
        if (economyScreenUi.TradeActionOptionsPanel != null)
        {
            economyScreenUi.TradeActionOptionsPanel.gameObject.SetActive(isTradeActionDropdownOpen);
            if (isTradeActionDropdownOpen)
                PlaceDropdownBelow(economyScreenUi.TradeActionDropdownButton.GetComponent<RectTransform>(), economyScreenUi.TradeActionOptionsPanel, economyScreenUi.CanvasRoot.GetComponent<RectTransform>());
        }
        UpdateTradeDropdownOptions();
        economyScreenUi.EmptyOrdersText.gameObject.SetActive(activeTradeHudOrders.Count == 0);

        for (int i = 0; i < economyScreenUi.TradeOrderRows.Count; i++)
        {
            bool active = i < activeTradeHudOrders.Count;
            TradeOrderRowUi row = economyScreenUi.TradeOrderRows[i];
            row.Root.gameObject.SetActive(active);
            if (!active) continue;

            TradeHudOrder order = activeTradeHudOrders[i];
            row.OrderId = order.Id;
            bool isBuy = order.OrderType == TradeOrderType.Buy;
            row.TagText.text = isBuy ? "BUY" : "SELL";
            row.TagBackground.color = isBuy ? new Color(0.23f, 0.62f, 0.10f, 1f) : new Color(0.72f, 0.12f, 0.10f, 1f);
            row.OrderText.text = $"{row.TagText.text} {order.Amount} {L(GetTradeResourceShortLabel(order.ResourceType))}";
        }

        if (forceLayoutRebuild)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(economyScreenUi.ActiveOrdersContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(economyScreenUi.WindowRoot);
        }

        LocalizeCanvas(economyScreenUi.CanvasRoot);
        isEconomyScreenDirty = false;
    }

    private void EnsureTradeHudSelectionValid()
    {
        if (System.Array.IndexOf(TradeHudResources, selectedTradeResourceType) < 0)
        {
            selectedTradeResourceType = TradeHudResources[0];
        }

        selectedTradeOrderAmount = Mathf.Max(1, selectedTradeOrderAmount);
    }

    private void UpdateTradeDropdownOptions()
    {
        for (int i = 0; i < economyScreenUi.TradeResourceOptionButtons.Count && i < TradeHudResources.Length; i++)
        {
            TradeResourceType resourceType = TradeHudResources[i];
            bool isSelected = resourceType == selectedTradeResourceType;
            Image image = economyScreenUi.TradeResourceOptionButtons[i].GetComponent<Image>();
            image.color = isSelected ? new Color(0.20f, 0.30f, 0.44f, 1f) : new Color(0.11f, 0.14f, 0.19f, 1f);
            if (i < economyScreenUi.TradeResourceOptionAmountTexts.Count)
                economyScreenUi.TradeResourceOptionAmountTexts[i].text = GetStoredTradeResourceAmount(resourceType).ToString();
        }

        TradeOrderType[] orderTypes = { TradeOrderType.Buy, TradeOrderType.Sell };
        for (int i = 0; i < economyScreenUi.TradeActionOptionTexts.Count && i < orderTypes.Length; i++)
        {
            TradeOrderType orderType = orderTypes[i];
            economyScreenUi.TradeActionOptionTexts[i].text = L(GetTradeModeLabel(orderType));
            Image image = economyScreenUi.TradeActionOptionButtons[i].GetComponent<Image>();
            image.color = orderType == selectedTradeOrderType ? FleetPrimaryButtonColor : new Color(0.16f, 0.19f, 0.25f, 1f);
        }
    }

    private void SelectTradeOrderTypeFromHud(TradeOrderType orderType)
    {
        selectedTradeOrderType = orderType;
        isTradeActionDropdownOpen = false;
        isEconomyScreenDirty = true;
        PlayUiSound(uiSelectClip, 0.82f);
    }

    private void CycleTradeOrderType(int direction)
    {
        TradeOrderType[] types = { TradeOrderType.Buy, TradeOrderType.Sell };
        int idx = System.Array.IndexOf(types, selectedTradeOrderType);
        selectedTradeOrderType = types[(idx + direction + types.Length) % types.Length];
        isEconomyScreenDirty = true;
        PlayUiSound(uiSelectClip, 0.82f);
    }

    private void CreateTradeHudOrder()
    {
        if (selectedTradeOrderAmount < 1)
        {
            return;
        }

        activeTradeHudOrders.Add(new TradeHudOrder
        {
            Id = nextTradeOrderId++,
            ResourceType = selectedTradeResourceType,
            OrderType = selectedTradeOrderType,
            Amount = selectedTradeOrderAmount
        });
        isTradeResourceDropdownOpen = false;
        isTradeActionDropdownOpen = false;
        SessionDebugLogger.Log("TRADE_HUD", $"Created {selectedTradeOrderType} order: {selectedTradeOrderAmount} {GetTradeResourceShortLabel(selectedTradeResourceType)}.");
        PlayUiSound(uiSelectClip, 0.9f);
        TryAutoDispatchNextHudOrder();
    }

    private void RemoveTradeHudOrder(int orderId)
    {
        int removed = activeTradeHudOrders.RemoveAll(order => order.Id == orderId);
        if (removed <= 0)
        {
            return;
        }

        isEconomyScreenDirty = true;
        SessionDebugLogger.Log("TRADE_HUD", $"Removed trade order #{orderId}.");
        PlayUiSound(uiPanelCloseClip, 0.76f);
    }

    private string GetTradeResourceShortLabel(TradeResourceType resourceType)
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

    private string GetResourcePriority(TradeResourceType resourceType)
    {
        return resourceType switch
        {
            TradeResourceType.Logs => "Low",
            TradeResourceType.Boards => "Medium",
            TradeResourceType.Cotton => "Low",
            TradeResourceType.Textile => "Medium",
            TradeResourceType.Furniture => "High",
            TradeResourceType.Fuel => "Critical",
            TradeResourceType.Alcohol => "Medium",
            TradeResourceType.Food => "High",
            _ => "Unknown"
        };
    }

    private string GetTradeResourceLabel(TradeResourceType resourceType)
    {
        string baseName = resourceType switch
        {
            TradeResourceType.Logs      => "Logs",
            TradeResourceType.Boards    => "Boards",
            TradeResourceType.Cotton    => "Cotton",
            TradeResourceType.Textile   => "Textile",
            TradeResourceType.Furniture => "Furniture",
            TradeResourceType.Fuel      => "Fuel",
            TradeResourceType.Alcohol   => "Alcohol",
            TradeResourceType.Food      => "Food",
            _ => resourceType.ToString()
        };
        string priority = GetResourcePriority(resourceType);
        return $"{baseName} ({priority})";
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
            detail += $" вЂў {entry.ToLabel} balance: ${entry.RecipientBalanceAfter.Value}";
        }

        if (entry.TreasuryAfter.HasValue)
        {
            detail += $" вЂў Treasury: ${entry.TreasuryAfter.Value}";
        }

        return detail;
    }

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

        // Fullscreen backdrop вЂ” sits directly on the canvas, covers everything below
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
        infoPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 190f;
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
        worldMapScreenUi.DetailsDescriptionText = CreateBodyText("WorldMapDetailsDescription", infoPanel, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        worldMapScreenUi.DetailsDescriptionText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        RectTransform footerCard = CreateSectionCard(windowRoot.transform, font, "Current Trade Context", out RectTransform footerBody);
        footerCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 74f;
        CreateBodyText(
            "WorldMapFooter",
            footerBody,
            font,
            "Current gameplay rule: textile comes from another region, so the map shows where it conceptually enters your economy.",
            13,
            TextAnchor.MiddleLeft,
            FleetAccentColor);

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
            3 => "Cotton Plains",
            4 => "Your Town",
            5 => "Textile District",
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
            2 or 3 or 5 => "Neighbor region",
            _ => "Empty region slot"
        };
    }

    private static string GetWorldMapRegionProducedResources(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "Logs, Boards, Furniture",
            5 => "Textile",
            3 => "Cotton",
            2 => "Trade logistics",
            _ => "No confirmed survey data"
        };
    }

    private static string GetWorldMapRegionDescription(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "This is your active simulation region. It contains the current town, highways, production buildings, and local roads.",
            5 => "A neighboring industrial district focused on textile output. This is the important current external source for textile supply.",
            3 => "A farming-heavy region that supplies raw cotton into the wider trade network.",
            2 => "A schematic route hub near the river corridor, reserved for future logistics and regional expansion passes.",
            _ => "This region exists on the wider map, but it has not been fully designed or assigned concrete production data yet."
        };
    }

    private static bool IsWorldMapRegionKnown(int regionIndex)
    {
        return regionIndex == 2 || regionIndex == 3 || regionIndex == 4 || regionIndex == 5;
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
        worldMapScreenUi.DetailsDescriptionText.text = GetWorldMapRegionDescription(selected);

        LayoutRebuilder.ForceRebuildLayoutImmediate(worldMapScreenUi.WindowRoot);
        LocalizeCanvas(worldMapScreenUi.CanvasRoot);
        isWorldMapScreenDirty = false;
    }
}
