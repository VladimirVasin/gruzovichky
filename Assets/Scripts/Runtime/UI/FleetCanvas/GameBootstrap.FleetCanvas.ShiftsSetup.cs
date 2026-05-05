using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void SetupShiftsScreenUi()
    {
        if (shiftsScreenUi != null) return;

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        shiftsScreenUi = new ShiftsScreenUiRefs();

        GameObject canvasObject = new("ShiftsScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        shiftsScreenUi.CanvasRoot = canvasObject;

        GameObject windowRoot = CreateUiObject("ShiftsWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        SetCenteredWindow(windowRect, ShiftsWindowWidth, ShiftsWindowHeight, -16f);
        shiftsScreenUi.WindowRoot = windowRect;

        Image windowBg = windowRoot.AddComponent<Image>();
        windowBg.color = ShiftsScreenTint;
        Outline windowOutline = windowRoot.AddComponent<Outline>();
        windowOutline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        windowOutline.effectDistance = new Vector2(2f, -2f);

        HorizontalLayoutGroup rootLayout = windowRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 16;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = false;
        rootLayout.childForceExpandHeight = true;

        RectTransform leftPanel = CreateStyledPanel("ShiftsDriverListPanel", windowRoot.transform, FleetPanelColor);
        shiftsScreenUi.LeftPanel = leftPanel;
        LayoutElement leftPanelLayout = leftPanel.gameObject.AddComponent<LayoutElement>();
        leftPanelLayout.preferredWidth = ShiftsLeftPanelWidth;
        leftPanelLayout.minWidth = ShiftsLeftPanelWidth;
        leftPanelLayout.preferredHeight = ShiftsInnerPanelHeight;
        leftPanelLayout.minHeight = ShiftsInnerPanelHeight;
        leftPanelLayout.flexibleWidth = 0f;
        leftPanelLayout.flexibleHeight = 0f;
        VerticalLayoutGroup leftLayout = leftPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        leftLayout.padding = new RectOffset(16, 16, 16, 16);
        leftLayout.spacing = 14;
        leftLayout.childControlWidth = true;
        leftLayout.childControlHeight = true;
        leftLayout.childForceExpandWidth = true;
        leftLayout.childForceExpandHeight = false;

        RectTransform leftHeader = CreateLayoutRow("ShiftsHeaderRow", leftPanel, 40f, 0f);
        Text shiftsTitle = CreateHeaderText("ShiftsTitle", leftHeader, font, "Roles", 24, TextAnchor.MiddleLeft, Color.white);
        shiftsTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        shiftsScreenUi.TitleText = shiftsTitle;
        shiftsScreenUi.HeaderCountText = CreateHeaderText("ShiftsCount", leftHeader, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        RectTransform listFrame = CreateStyledPanel("ShiftsDriverListFrame", leftPanel, FleetInsetColor);
        LayoutElement listFrameLayout = listFrame.gameObject.AddComponent<LayoutElement>();
        listFrameLayout.flexibleHeight = 1f;
        listFrameLayout.minHeight = 320f;

        GameObject scrollObj = CreateUiObject("ShiftsDriverScrollView", listFrame);
        StretchRect(scrollObj.GetComponent<RectTransform>(), 8f, 8f, 8f, 8f);
        Image scrollImg = scrollObj.AddComponent<Image>();
        scrollImg.color = new Color(0f, 0f, 0f, 0f);
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 28f;

        GameObject viewportObj = CreateUiObject("Viewport", scrollObj.transform);
        StretchRect(viewportObj.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        Image viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.04f);
        Mask viewportMask = viewportObj.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        scrollRect.viewport = viewportObj.GetComponent<RectTransform>();

        GameObject contentObj = CreateUiObject("ShiftsDriverListContent", viewportObj.transform);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 12;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        scrollRect.content = contentRect;
        shiftsScreenUi.DriverListContent = contentRect;

        CreateShiftsDriverRows(contentRect, font);

        RectTransform rightPanel = CreateStyledPanel("ShiftsCardsPanel", windowRoot.transform, FleetPanelColor);
        shiftsScreenUi.RightPanel = rightPanel;
        LayoutElement rightPanelLayout = rightPanel.gameObject.AddComponent<LayoutElement>();
        rightPanelLayout.preferredWidth = ShiftsRightPanelWidth;
        rightPanelLayout.minWidth = ShiftsRightPanelWidth;
        rightPanelLayout.flexibleWidth = 0f;
        rightPanelLayout.preferredHeight = ShiftsInnerPanelHeight;
        rightPanelLayout.minHeight = ShiftsInnerPanelHeight;
        rightPanelLayout.flexibleHeight = 0f;
        VerticalLayoutGroup rightLayout = rightPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        rightLayout.padding = new RectOffset(16, 16, 14, 16);
        rightLayout.spacing = 12;
        rightLayout.childControlWidth = true;
        rightLayout.childControlHeight = true;
        rightLayout.childForceExpandWidth = true;
        rightLayout.childForceExpandHeight = false;

        RectTransform selectionCard = CreateSectionCard(rightPanel, font, string.Empty, out RectTransform selectionBody, false);
        selectionCard.gameObject.AddComponent<LayoutElement>().preferredHeight = ShiftsSelectionCardHeight;
        shiftsScreenUi.SelectionTitleText = CreateHeaderText("AssignmentsSelectionTitle", selectionBody, font, "Selected Worker", 16, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.SelectionTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        RectTransform selectionInfoPanel = CreateStyledPanel("AssignmentsSelectionInfoPanel", selectionBody, FleetCardMutedColor);
        selectionInfoPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 98f;
        VerticalLayoutGroup selectionInfoLayout = selectionInfoPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        selectionInfoLayout.padding = new RectOffset(12, 12, 10, 10);
        selectionInfoLayout.spacing = 4;
        selectionInfoLayout.childControlWidth = true;
        selectionInfoLayout.childControlHeight = true;
        selectionInfoLayout.childForceExpandWidth = true;
        selectionInfoLayout.childForceExpandHeight = false;
        shiftsScreenUi.SelectionNameText = CreateHeaderText("AssignmentsSelectionName", selectionInfoPanel, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        shiftsScreenUi.SelectionNameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
        shiftsScreenUi.SelectionNameText.verticalOverflow = VerticalWrapMode.Overflow;
        shiftsScreenUi.SelectionProfessionText = CreateBodyText("AssignmentsSelectionProfession", selectionInfoPanel, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        shiftsScreenUi.SelectionProfessionText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        shiftsScreenUi.SelectionProfessionText.verticalOverflow = VerticalWrapMode.Truncate;
        shiftsScreenUi.SelectionStatusText = CreateBodyText("AssignmentsSelectionStatus", selectionInfoPanel, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.SelectionStatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        shiftsScreenUi.SelectionStatusText.verticalOverflow = VerticalWrapMode.Truncate;
        shiftsScreenUi.SelectionHintText = CreateBodyText("AssignmentsSelectionHint", selectionBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        shiftsScreenUi.SelectionHintText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        shiftsScreenUi.VacancySuccessText = CreateBodyText("AssignmentsSuccessText", selectionBody, font, string.Empty, 12, TextAnchor.MiddleLeft, new Color(0.65f, 0.95f, 0.66f, 1f));
        shiftsScreenUi.VacancySuccessText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        // ── Tab toggle row ───────────────────────────────────────────────────
        RectTransform tabRow = CreateTabRow("ShiftsTabRow", rightPanel, ShiftsTabRowHeight, 8f);
        shiftsScreenUi.TabRowRoot = tabRow;
        LayoutElement tabRowLE = tabRow.GetComponent<LayoutElement>();
        tabRowLE.minHeight = ShiftsTabRowHeight;
        tabRowLE.flexibleHeight = 0f;
        HorizontalLayoutGroup tabRowLayout = tabRow.GetComponent<HorizontalLayoutGroup>();
        tabRowLayout.childForceExpandWidth = true;
        tabRowLayout.childForceExpandHeight = true;
        shiftsTransportTabBtn = CreateButton("LogisticsTabBtn", tabRow, font, out shiftsTransportTabText, "Logistics", 13, FleetPrimaryButtonColor, Color.white);
        shiftsTransportTabText.fontStyle = FontStyle.Bold;
        shiftsTransportTabBtn.transition = Selectable.Transition.None;
        shiftsTransportTabBtn.onClick.AddListener(() =>
        {
            isLogisticsTabActive = false;
            ApplyShiftsTabVisuals();
            EnforceShiftsWindowLayout();
            LogShiftsHudState("clicked Logistics tab", force: true);
            isShiftsScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.8f);
        });
        shiftsLogisticsTabBtn = CreateButton("ProductionsTabBtn", tabRow, font, out shiftsLogisticsTabText, "Productions", 13, new Color(0.22f, 0.26f, 0.32f, 1f), Color.white);
        shiftsLogisticsTabText.fontStyle = FontStyle.Bold;
        shiftsLogisticsTabBtn.transition = Selectable.Transition.None;
        shiftsLogisticsTabBtn.onClick.AddListener(() =>
        {
            isLogisticsTabActive = true;
            ApplyShiftsTabVisuals();
            EnforceShiftsWindowLayout();
            LogShiftsHudState("clicked Productions tab", force: true);
            isShiftsScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.8f);
        });

        // ── Transportation panel (existing shift cards + intercity) ──────────
        RectTransform vacancyFlowPanel = CreateStyledPanel("VacancyFlowPanel", rightPanel, ShiftsCardColor);
        shiftsScreenUi.VacancyFlowPanel = vacancyFlowPanel;
        LayoutElement vacancyFlowLayoutElement = vacancyFlowPanel.gameObject.AddComponent<LayoutElement>();
        vacancyFlowLayoutElement.preferredHeight = GetShiftsTabContentHeight();
        vacancyFlowLayoutElement.minHeight = GetShiftsTabContentHeight();
        vacancyFlowLayoutElement.flexibleHeight = 0f;
        VerticalLayoutGroup vacancyFlowLayout = vacancyFlowPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        vacancyFlowLayout.padding = new RectOffset(14, 14, 12, 12);
        vacancyFlowLayout.spacing = 8;
        vacancyFlowLayout.childControlWidth = true;
        vacancyFlowLayout.childControlHeight = true;
        vacancyFlowLayout.childForceExpandWidth = true;
        vacancyFlowLayout.childForceExpandHeight = false;

        RectTransform stepRow = CreateLayoutRow("VacancyStepProgressRow", vacancyFlowPanel, 34f, 6f);
        string[] stepLabels = { "Vacancy", "Shift", "Truck", "Worker" };
        for (int i = 0; i < stepLabels.Length; i++)
        {
            RectTransform stepRoot = CreateStyledPanel($"VacancyStep{i + 1}", stepRow, new Color(0.08f, 0.10f, 0.14f, 0.94f));
            LayoutElement stepLayout = stepRoot.gameObject.AddComponent<LayoutElement>();
            stepLayout.flexibleWidth = 1f;
            stepLayout.preferredHeight = 28f;
            Text stepText = CreateBodyText($"VacancyStepText{i + 1}", stepRoot, font, stepLabels[i], 11, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
            StretchRect(stepText.GetComponent<RectTransform>(), 4f, 2f, 4f, 2f);
            shiftsScreenUi.VacancyStepBackgrounds.Add(stepRoot.GetComponent<Image>());
            shiftsScreenUi.VacancyStepTexts.Add(stepText);
        }

        shiftsScreenUi.VacancyFlowTitleText = CreateHeaderText("VacancyFlowTitle", vacancyFlowPanel, font, string.Empty, 17, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.VacancyFlowTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        shiftsScreenUi.VacancyFlowHintText = CreateBodyText("VacancyFlowHint", vacancyFlowPanel, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        shiftsScreenUi.VacancyFlowHintText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        RectTransform transportParkCard = CreateStyledPanel("VacancyTransportParkCard", vacancyFlowPanel, FleetCardMutedColor);
        shiftsScreenUi.VacancyTransportParkCard = transportParkCard;
        transportParkCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 112f;
        VerticalLayoutGroup transportParkLayout = transportParkCard.gameObject.AddComponent<VerticalLayoutGroup>();
        transportParkLayout.padding = new RectOffset(12, 12, 8, 8);
        transportParkLayout.spacing = 5f;
        transportParkLayout.childControlWidth = true;
        transportParkLayout.childControlHeight = true;
        transportParkLayout.childForceExpandWidth = true;
        transportParkLayout.childForceExpandHeight = false;

        RectTransform transportParkHeader = CreateLayoutRow("VacancyTransportParkHeader", transportParkCard, 22f, 8f);
        shiftsScreenUi.VacancyTransportParkTitleText = CreateHeaderText("VacancyTransportParkTitle", transportParkHeader, font, "Transport Park", 14, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.VacancyTransportParkTitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        shiftsScreenUi.VacancyTransportParkCountText = CreateBodyText("VacancyTransportParkCount", transportParkHeader, font, string.Empty, 12, TextAnchor.MiddleRight, FleetSecondaryTextColor);
        shiftsScreenUi.VacancyTransportParkCountText.gameObject.AddComponent<LayoutElement>().preferredWidth = 112f;
        shiftsScreenUi.VacancyTransportParkSummaryText = CreateBodyText("VacancyTransportParkSummary", transportParkCard, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        shiftsScreenUi.VacancyTransportParkSummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        shiftsScreenUi.VacancyBuyTruckButton = CreateButton("VacancyTransportParkBuyTruck", transportParkCard, font, out shiftsScreenUi.VacancyBuyTruckButtonText, "Parking Slots", 12, FleetPrimaryButtonColor, Color.white);
        shiftsScreenUi.VacancyBuyTruckButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        shiftsScreenUi.VacancyBuyTruckButton.onClick.AddListener(() =>
        {
            HireTransportForSelectedVacancy();
            isShiftsScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.85f);
        });
        shiftsScreenUi.VacancyBuyTruckStatusText = CreateBodyText("VacancyTransportParkStatus", transportParkCard, font, string.Empty, 11, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
        shiftsScreenUi.VacancyBuyTruckStatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        for (int i = 0; i < MaxVacancyOptionRows; i++)
        {
            GameObject optionObj = CreateUiObject($"VacancyOptionRow{i + 1}", vacancyFlowPanel);
            RectTransform optionRoot = optionObj.GetComponent<RectTransform>();
            optionObj.AddComponent<LayoutElement>().preferredHeight = 48f;
            Image optionBg = optionObj.AddComponent<Image>();
            optionBg.color = FleetCardMutedColor;
            optionBg.raycastTarget = true;
            VerticalLayoutGroup optionLayout = optionObj.AddComponent<VerticalLayoutGroup>();
            optionLayout.padding = new RectOffset(12, 12, 6, 6);
            optionLayout.spacing = 2;
            optionLayout.childControlWidth = true;
            optionLayout.childControlHeight = true;
            optionLayout.childForceExpandWidth = true;
            optionLayout.childForceExpandHeight = false;

            RectTransform optionHeader = CreateLayoutRow($"VacancyOptionHeader{i + 1}", optionRoot, 20f, 8f);
            SetGraphicRaycast(optionHeader.gameObject, false);
            Text optionTitle = CreateHeaderText($"VacancyOptionTitle{i + 1}", optionHeader, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
            optionTitle.raycastTarget = false;
            optionTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            RectTransform optionBadge = CreateStyledPanel($"VacancyOptionBadge{i + 1}", optionHeader, new Color(0.12f, 0.18f, 0.14f, 0.95f));
            optionBadge.GetComponent<Image>().raycastTarget = false;
            LayoutElement optionBadgeLayout = optionBadge.gameObject.AddComponent<LayoutElement>();
            optionBadgeLayout.preferredWidth = 86f;
            optionBadgeLayout.preferredHeight = 18f;
            Text optionBadgeText = CreateBodyText($"VacancyOptionBadgeText{i + 1}", optionBadge, font, string.Empty, 10, TextAnchor.MiddleCenter, Color.white);
            optionBadgeText.raycastTarget = false;
            StretchRect(optionBadgeText.GetComponent<RectTransform>(), 5f, 1f, 5f, 1f);
            Text optionSubtitle = CreateBodyText($"VacancyOptionSubtitle{i + 1}", optionRoot, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            optionSubtitle.raycastTarget = false;
            optionSubtitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            Button optionButton = optionObj.AddComponent<Button>();
            optionButton.targetGraphic = optionBg;
            ColorBlock optionColors = optionButton.colors;
            optionColors.normalColor = Color.white;
            optionColors.highlightedColor = new Color(1.12f, 1.12f, 1.08f, 1f);
            optionColors.pressedColor = new Color(0.86f, 0.86f, 0.82f, 1f);
            optionColors.selectedColor = new Color(1.06f, 1.02f, 0.88f, 1f);
            optionColors.fadeDuration = 0.08f;
            optionButton.colors = optionColors;
            int optionIndex = i;
            optionButton.onClick.AddListener(() => OnVacancyFlowOptionPressed(optionIndex));

            shiftsScreenUi.VacancyOptionRows.Add(new VacancyOptionRowUi
            {
                Root = optionRoot,
                Background = optionBg,
                TitleText = optionTitle,
                SubtitleText = optionSubtitle,
                BadgeBackground = optionBadge.GetComponent<Image>(),
                BadgeText = optionBadgeText,
                Button = optionButton
            });
        }

        GameObject transportPanelObj = CreateUiObject("TransportPanel", rightPanel);
        shiftsTransportPanel = transportPanelObj.GetComponent<RectTransform>();
        LayoutElement transportPanelLayout = transportPanelObj.AddComponent<LayoutElement>();
        transportPanelLayout.preferredHeight = GetShiftsTabContentHeight();
        transportPanelLayout.minHeight = GetShiftsTabContentHeight();
        transportPanelLayout.flexibleHeight = 0f;
        ScrollRect transportScroll = transportPanelObj.AddComponent<ScrollRect>();
        shiftsScreenUi.TransportScrollRect = transportScroll;
        transportScroll.horizontal = false; transportScroll.vertical = true;
        transportScroll.movementType = ScrollRect.MovementType.Clamped;
        transportScroll.scrollSensitivity = 30f; transportScroll.inertia = false;
        transportScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        GameObject transportViewportObj = CreateUiObject("Viewport", transportPanelObj.transform);
        RectTransform transportViewport = transportViewportObj.GetComponent<RectTransform>();
        StretchRect(transportViewport, 0f, 18f, 0f, 0f);
        Image transportViewportImage = transportViewportObj.AddComponent<Image>();
        transportViewportImage.color = new Color(0f, 0f, 0f, 0.02f);
        transportViewportObj.AddComponent<Mask>().showMaskGraphic = false;

        Scrollbar transportScrollbar = CreatePanelScrollbar("TransportScrollbar", transportPanelObj.transform);
        transportScroll.viewport = transportViewport;
        transportScroll.verticalScrollbar = transportScrollbar;

        GameObject transportContentGo = CreateUiObject("TransportContent", transportViewportObj.transform);
        RectTransform transportContent = transportContentGo.GetComponent<RectTransform>();
        transportContent.anchorMin = new Vector2(0f, 1f); transportContent.anchorMax = new Vector2(1f, 1f);
        transportContent.pivot = new Vector2(0.5f, 1f);
        transportContent.anchoredPosition = Vector2.zero; transportContent.sizeDelta = Vector2.zero;
        VerticalLayoutGroup transportLayout = transportContentGo.AddComponent<VerticalLayoutGroup>();
        transportLayout.spacing = 14;
        transportLayout.childControlWidth = true;
        transportLayout.childControlHeight = true;
        transportLayout.childForceExpandWidth = true;
        transportLayout.childForceExpandHeight = false;
        transportContentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        transportScroll.content = transportContent;

        RectTransform transportIntroCard = CreateSectionCard(transportContent, font, string.Empty, out RectTransform transportIntroBody, false);
        transportIntroCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;
        VerticalLayoutGroup transportIntroLayout = transportIntroBody.GetComponent<VerticalLayoutGroup>();
        transportIntroLayout.spacing = 6f;
        shiftsScreenUi.LogisticsSectionTitleText = CreateHeaderText("AssignmentsLogisticsSectionTitle", transportIntroBody, font, "Logistics", 16, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.LogisticsSectionSummaryText = CreateBodyText("AssignmentsLogisticsSectionSummary", transportIntroBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);

        RectTransform fleetCard = CreateSectionCard(transportContent, font, string.Empty, out RectTransform fleetBody, false);
        fleetCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 392f;
        VerticalLayoutGroup fleetBodyLayout = fleetBody.GetComponent<VerticalLayoutGroup>();
        fleetBodyLayout.spacing = 8f;

        RectTransform fleetHeaderRow = CreateLayoutRow("AssignmentsFleetHeaderRow", fleetBody, 26f, 8f);
        shiftsScreenUi.FleetSectionTitleText = CreateHeaderText("AssignmentsFleetTitle", fleetHeaderRow, font, "Fleet", 16, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.FleetSectionTitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        shiftsScreenUi.FleetCountText = CreateBodyText("AssignmentsFleetCount", fleetHeaderRow, font, string.Empty, 12, TextAnchor.MiddleRight, FleetSecondaryTextColor);
        shiftsScreenUi.FleetCountText.gameObject.AddComponent<LayoutElement>().preferredWidth = 150f;
        shiftsScreenUi.FleetSectionSummaryText = CreateBodyText("AssignmentsFleetSummary", fleetBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        shiftsScreenUi.FleetSectionSummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 32f;

        RectTransform fleetBuyPanel = CreateStyledPanel("AssignmentsFleetBuyPanel", fleetBody, FleetCardMutedColor);
        fleetBuyPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;
        VerticalLayoutGroup fleetBuyLayout = fleetBuyPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        fleetBuyLayout.padding = new RectOffset(10, 10, 8, 8);
        fleetBuyLayout.spacing = 5f;
        fleetBuyLayout.childControlWidth = true;
        fleetBuyLayout.childControlHeight = true;
        fleetBuyLayout.childForceExpandWidth = true;
        fleetBuyLayout.childForceExpandHeight = false;
        shiftsScreenUi.FleetBuyTruckButton = CreateButton("AssignmentsFleetBuyTruckButton", fleetBuyPanel, font, out shiftsScreenUi.FleetBuyTruckButtonText, "Parking Slots", 13, FleetPrimaryButtonColor, Color.white);
        shiftsScreenUi.FleetBuyTruckButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        shiftsScreenUi.FleetBuyTruckButton.onClick.AddListener(() =>
        {
            HireNewTruck();
            isShiftsScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.85f);
        });
        shiftsScreenUi.FleetBuyTruckStatusText = CreateBodyText("AssignmentsFleetBuyTruckStatus", fleetBuyPanel, font, string.Empty, 11, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
        shiftsScreenUi.FleetBuyTruckStatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform fleetListPanel = CreateStyledPanel("AssignmentsFleetListPanel", fleetBody, FleetInsetColor);
        fleetListPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 238f;
        VerticalLayoutGroup fleetListLayout = fleetListPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        fleetListLayout.padding = new RectOffset(8, 8, 8, 8);
        fleetListLayout.spacing = 6f;
        fleetListLayout.childControlWidth = true;
        fleetListLayout.childControlHeight = true;
        fleetListLayout.childForceExpandWidth = true;
        fleetListLayout.childForceExpandHeight = false;

        for (int fleetRowIndex = 0; fleetRowIndex < MaxTruckCount; fleetRowIndex++)
        {
            ShiftsFleetTruckRowUi row = new() { TruckNumber = fleetRowIndex + 1 };
            GameObject rowObj = CreateUiObject($"AssignmentsFleetTruckRow{fleetRowIndex + 1}", fleetListPanel);
            row.Root = rowObj.GetComponent<RectTransform>();
            row.Root.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
            row.Background = rowObj.AddComponent<Image>();
            row.Background.color = ShiftsCardColor;
            Outline rowOutline = rowObj.AddComponent<Outline>();
            rowOutline.effectColor = new Color(0f, 0f, 0f, 0.22f);
            rowOutline.effectDistance = new Vector2(1f, -1f);

            HorizontalLayoutGroup rowLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
            rowLayout.padding = new RectOffset(10, 8, 5, 5);
            rowLayout.spacing = 8f;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;

            RectTransform textColumn = CreateUiObject($"AssignmentsFleetTruckTextColumn{fleetRowIndex + 1}", row.Root).GetComponent<RectTransform>();
            textColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            VerticalLayoutGroup textLayout = textColumn.gameObject.AddComponent<VerticalLayoutGroup>();
            textLayout.spacing = 1f;
            textLayout.childControlWidth = true;
            textLayout.childControlHeight = true;
            textLayout.childForceExpandWidth = true;
            textLayout.childForceExpandHeight = false;

            RectTransform topLine = CreateLayoutRow($"AssignmentsFleetTruckTopLine{fleetRowIndex + 1}", textColumn, 17f, 6f);
            row.NameText = CreateHeaderText($"AssignmentsFleetTruckName{fleetRowIndex + 1}", topLine, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
            row.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            row.StatusText = CreateBodyText($"AssignmentsFleetTruckStatus{fleetRowIndex + 1}", topLine, font, string.Empty, 11, TextAnchor.MiddleRight, FleetAccentColor);
            row.StatusText.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;
            RectTransform bottomLine = CreateLayoutRow($"AssignmentsFleetTruckBottomLine{fleetRowIndex + 1}", textColumn, 16f, 8f);
            row.CrewText = CreateBodyText($"AssignmentsFleetTruckCrew{fleetRowIndex + 1}", bottomLine, font, string.Empty, 10, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            row.CrewText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            row.CargoText = CreateBodyText($"AssignmentsFleetTruckCargo{fleetRowIndex + 1}", bottomLine, font, string.Empty, 10, TextAnchor.MiddleRight, FleetSecondaryTextColor);
            row.CargoText.gameObject.AddComponent<LayoutElement>().preferredWidth = 110f;

            row.AssignButton = CreateButton($"AssignmentsFleetTruckAssign{fleetRowIndex + 1}", row.Root, font, out row.AssignButtonText, "Assign", 10, FleetPrimaryButtonColor, Color.white);
            row.AssignButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 110f;
            int capturedTruckNumber = fleetRowIndex + 1;
            row.AssignButton.onClick.AddListener(() =>
            {
                TruckAgent truck = GetTruckAgent(capturedTruckNumber);
                DriverAgent selectedDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);
                if (truck == null || selectedDriver == null)
                {
                    return;
                }

                if (AssignDriverToTruck(truck, selectedDriver))
                {
                    FocusTruck(truck.TruckNumber);
                    isShiftsScreenDirty = true;
                    isDriversScreenDirty = true;
                    isFleetScreenDirty = true;
                }
            });

            row.FocusButton = rowObj.AddComponent<Button>();
            row.FocusButton.targetGraphic = row.Background;
            int focusTruckNumber = fleetRowIndex + 1;
            row.FocusButton.onClick.AddListener(() =>
            {
                TruckAgent truck = GetTruckAgent(focusTruckNumber);
                if (truck == null)
                {
                    return;
                }

                FocusTruck(truck.TruckNumber);
                selectedTruckNumber = truck.TruckNumber;
                isFleetScreenDirty = true;
                isShiftsScreenDirty = true;
                PlayUiSound(uiSelectClip, 0.72f);
            });

            shiftsScreenUi.FleetTruckRows.Add(row);
        }

        for (int i = 0; i < ShiftPresetHours.Length; i++)
        {
            int shiftHour = ShiftPresetHours[i];
            ShiftCardUi card = new() { ShiftHour = shiftHour };
            RectTransform cardRoot = CreateSectionCard(transportContent, font, string.Empty, out RectTransform cardBody, false);
            cardRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 158f;

            GameObject borderObj = CreateUiObject($"ShiftBorder{i}", cardRoot);
            RectTransform borderRect = borderObj.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0f, 0f);
            borderRect.anchorMax = new Vector2(0f, 1f);
            borderRect.pivot = new Vector2(0f, 0.5f);
            borderRect.anchoredPosition = Vector2.zero;
            borderRect.sizeDelta = new Vector2(4f, 0f);
            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.color = Color.clear;
            borderImg.raycastTarget = false;
            borderObj.AddComponent<LayoutElement>().ignoreLayout = true;
            card.ActiveBorderImage = borderImg;

            VerticalLayoutGroup cardBodyLayout = cardBody.GetComponent<VerticalLayoutGroup>();
            cardBodyLayout.spacing = 8;

            card.HeaderText = CreateHeaderText($"ShiftHeader{i}", cardBody, font, $"{ShiftNames[i]}  {GetShiftRangeLabel(shiftHour)}", 16, TextAnchor.MiddleLeft, Color.white);
            card.SummaryText = CreateBodyText($"ShiftSummary{i}", cardBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            card.SummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

            RectTransform assignedPanel = CreateStyledPanel($"ShiftAssignedPanel{i}", cardBody, FleetCardMutedColor);
            card.AssignedListRoot = assignedPanel;
            VerticalLayoutGroup assignedLayout = assignedPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            assignedLayout.padding = new RectOffset(10, 10, 10, 10);
            assignedLayout.spacing = 6;
            assignedLayout.childControlWidth = true;
            assignedLayout.childControlHeight = true;
            assignedLayout.childForceExpandWidth = true;
            assignedLayout.childForceExpandHeight = false;
            assignedPanel.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            card.EmptyText = CreateBodyText($"ShiftEmpty{i}", assignedPanel, font, "No drivers assigned", 12, TextAnchor.MiddleLeft, FleetMutedTextColor);

            for (int rowIndex = 0; rowIndex < MaxShiftDriverSlots; rowIndex++)
            {
                GameObject assignedRow = CreateUiObject($"ShiftAssignedRow{i}_{rowIndex}", assignedPanel);
                assignedRow.AddComponent<LayoutElement>().preferredHeight = 26f;
                HorizontalLayoutGroup assignedRowLayout = assignedRow.AddComponent<HorizontalLayoutGroup>();
                assignedRowLayout.spacing = 8;
                assignedRowLayout.childControlWidth = true;
                assignedRowLayout.childControlHeight = true;
                assignedRowLayout.childForceExpandWidth = false;
                assignedRowLayout.childForceExpandHeight = false;

                Text assignedText = CreateBodyText($"ShiftAssignedText{i}_{rowIndex}", assignedRow.transform, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
                assignedText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

                Button removeButton = CreateButton($"ShiftRemoveButton{i}_{rowIndex}", assignedRow.transform, font, out Text removeButtonText, "Remove", 11, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
                LayoutElement removeLayout = removeButton.gameObject.AddComponent<LayoutElement>();
                removeLayout.preferredWidth = 72f;
                removeLayout.preferredHeight = 22f;
                int shiftCardIndex = i;
                int assignedSlotIndex = rowIndex;
                removeButton.onClick.AddListener(() =>
                {
                    List<DriverAgent> assignedDrivers = new();
                    foreach (DriverAgent candidateDriver in driverAgents)
                    {
                        if (candidateDriver.ShiftStartHour == ShiftPresetHours[shiftCardIndex])
                        {
                            assignedDrivers.Add(candidateDriver);
                        }
                    }

                    if (assignedSlotIndex >= assignedDrivers.Count)
                    {
                        return;
                    }

                    DriverAgent assignedDriver = assignedDrivers[assignedSlotIndex];
                    LogUiInput($"Shifts Canvas: removed {assignedDriver.DriverName} from {ShiftNames[shiftCardIndex]}");
                    LogCommand($"RemoveShift({assignedDriver.DriverName})");
                    assignedDriver.ShiftStartHour = -1;
                    assignedDriver.IsOnActiveShift = false;
                    assignedDriver.WaitingForShiftAtParking = false;
                    assignedDriver.NeedsShiftEndReturn = false;
                    TruckAgent assignedTruck = GetAssignedTruckForDriver(assignedDriver);
                    if (assignedTruck != null)
                    {
                        assignedTruck.IsTruckAutoModeEnabled = false;
                    }

                    if (selectedShiftDriverId == assignedDriver.DriverId)
                    {
                        selectedShiftDriverId = 0;
                    }

                    PlayUiSound(uiSelectClip, 0.85f);
                    SessionDebugLogger.Log("SHIFT", $"{assignedDriver.DriverName} removed from shift — now Idle.");
                    LogDriverReaction(assignedDriver, "shift removed; now idle");
                    isShiftsScreenDirty = true;
                    isDriversScreenDirty = true;
                    isFleetScreenDirty = true;
                });

                card.AssignedRows.Add(assignedRow);
                card.AssignedDriverTexts.Add(assignedText);
                card.RemoveButtons.Add(removeButton);
            }

            card.AssignButton = CreateButton($"ShiftAssignButton{i}", cardBody, font, out Text assignText, string.Empty, 12, FleetPrimaryButtonColor, Color.white);
            card.AssignButtonText = assignText;
            card.AssignButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
            int currentShiftIndex = i;
            card.AssignButton.onClick.AddListener(() =>
            {
                DriverAgent selectedDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);
                if (selectedDriver == null || selectedDriver.ShiftStartHour == ShiftPresetHours[currentShiftIndex])
                {
                    return;
                }

                LogUiInput($"Shifts Canvas: assigned {selectedDriver.DriverName} to {ShiftNames[currentShiftIndex]}");
                LogCommand($"AssignShift({selectedDriver.DriverName}, {ShiftNames[currentShiftIndex]})");
                selectedDriver.ShiftStartHour = ShiftPresetHours[currentShiftIndex];
                selectedDriver.IsOnActiveShift = false;
                selectedDriver.WaitingForShiftAtParking = false;
                bool inWindow = IsHourInShiftWindow(GetCurrentHour(), ShiftPresetHours[currentShiftIndex]);
                if (inWindow && selectedDriver.RestPhase == DriverRestPhase.None)
                {
                    if (IsDriverBusDriver(selectedDriver))
                    {
                        StartBusDriverShiftCommute(selectedDriver);
                    }
                    else if (!IsDriverBusyWalkPhase(selectedDriver))
                    {
                        StartDriverShiftCommute(selectedDriver);
                    }
                }

                PlayUiSound(uiSelectClip, 0.85f);
                SessionDebugLogger.Log("SHIFT", $"{selectedDriver.DriverName} assigned to {ShiftNames[currentShiftIndex]} ({GetShiftRangeLabel(ShiftPresetHours[currentShiftIndex])}).");
                LogDriverReaction(selectedDriver, $"assigned to {ShiftNames[currentShiftIndex]} ({GetShiftRangeLabel(ShiftPresetHours[currentShiftIndex])})");
                PushFeedEvent(
                    $"{selectedDriver.DriverName} assigned to {ShiftNames[currentShiftIndex]} shift.",
                    $"{selectedDriver.DriverName} назначен на смену {L(ShiftNames[currentShiftIndex])}.",
                    FeedEventType.Info);
                isShiftsScreenDirty = true;
                isDriversScreenDirty = true;
            });

            shiftsScreenUi.ShiftCards.Add(card);
        }

        shiftsScreenUi.IntercitySlot = new IntercitySlotUi();
        RectTransform intercityCard = CreateSectionCard(transportContent, font, string.Empty, out RectTransform intercityBody, false);
        intercityCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 142f;
        VerticalLayoutGroup intercityLayout = intercityBody.GetComponent<VerticalLayoutGroup>();
        intercityLayout.spacing = 8;

        shiftsScreenUi.IntercitySlot.HeaderText = CreateHeaderText("IntercityHeader", intercityBody, font, "Intercity", 16, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.IntercitySlot.SummaryText = CreateBodyText("IntercitySummary", intercityBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        shiftsScreenUi.IntercitySlot.SummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        shiftsScreenUi.IntercitySlot.AssignedDriverText = CreateHeaderText("IntercityAssigned", intercityBody, font, string.Empty, 15, TextAnchor.MiddleLeft, FleetAccentColor);
        shiftsScreenUi.IntercitySlot.AssignedDriverText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        shiftsScreenUi.IntercitySlot.StatusText = CreateBodyText("IntercityStatus", intercityBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        shiftsScreenUi.IntercitySlot.StatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        RectTransform intercityButtonRow = CreateLayoutRow("IntercityButtonRow", intercityBody, 30f, 8f);
        shiftsScreenUi.IntercitySlot.AssignButton = CreateButton("IntercityAssignButton", intercityButtonRow, font, out Text intercityAssignText, string.Empty, 12, FleetPrimaryButtonColor, Color.white);
        shiftsScreenUi.IntercitySlot.AssignButtonText = intercityAssignText;
        LayoutElement intercityAssignLayout = shiftsScreenUi.IntercitySlot.AssignButton.gameObject.AddComponent<LayoutElement>();
        intercityAssignLayout.flexibleWidth = 1f;
        intercityAssignLayout.preferredHeight = 30f;
        shiftsScreenUi.IntercitySlot.AssignButton.onClick.AddListener(() =>
        {
            DriverAgent selectedDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);
            if (selectedDriver == null)
            {
                return;
            }

            AssignDriverToIntercitySlot(selectedDriver);
        });

        shiftsScreenUi.IntercitySlot.RemoveButton = CreateButton("IntercityRemoveButton", intercityButtonRow, font, out Text intercityRemoveText, "Remove", 12, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
        LayoutElement intercityRemoveLayout = shiftsScreenUi.IntercitySlot.RemoveButton.gameObject.AddComponent<LayoutElement>();
        intercityRemoveLayout.preferredWidth = 92f;
        intercityRemoveLayout.preferredHeight = 30f;
        shiftsScreenUi.IntercitySlot.RemoveButton.onClick.AddListener(RemoveIntercityDriverAssignment);

        RectTransform busDriverCard = CreateSectionCard(transportContent, font, string.Empty, out RectTransform busDriverBody, false);
        LayoutElement busDriverCardLayout = busDriverCard.gameObject.AddComponent<LayoutElement>();
        busDriverCardLayout.preferredHeight = 176f;
        busDriverCardLayout.flexibleHeight = 0f;
        VerticalLayoutGroup busDriverLayout = busDriverBody.GetComponent<VerticalLayoutGroup>();
        busDriverLayout.spacing = 4f;

        shiftsScreenUi.BusDriverGroupTitleText = CreateHeaderText("BusDriverGroupTitle", busDriverBody, font, "Bus Driver", 16, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.BusDriverGroupSummaryText = CreateBodyText("BusDriverGroupSummary", busDriverBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        shiftsScreenUi.BusDriverGroupSummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        for (int busShiftIndex = 0; busShiftIndex < ShiftPresetHours.Length; busShiftIndex++)
        {
            IntercitySlotUi busSlot = new();
            shiftsScreenUi.BusDriverSlots.Add(busSlot);

            RectTransform busShiftRow = CreateLayoutRow($"BusDriverShiftRow{busShiftIndex}", busDriverBody, 28f, 4f);
            busShiftRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;

            busSlot.AssignedDriverText = CreateHeaderText($"BusDriverAssigned{busShiftIndex}", busShiftRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            busSlot.AssignedDriverText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            busSlot.AssignButton = CreateButton($"BusDriverAssignButton{busShiftIndex}", busShiftRow, font, out Text busDriverAssignText, string.Empty, 11, FleetPrimaryButtonColor, Color.white);
            busSlot.AssignButtonText = busDriverAssignText;
            LayoutElement busDriverAssignLayout = busSlot.AssignButton.gameObject.AddComponent<LayoutElement>();
            busDriverAssignLayout.preferredWidth = 104f;
            busDriverAssignLayout.preferredHeight = 28f;
            int capturedBusShiftIndex = busShiftIndex;
            busSlot.AssignButton.onClick.AddListener(() =>
            {
                DriverAgent selectedDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);
                if (selectedDriver == null)
                {
                    return;
                }

                AssignDriverToBusSlot(selectedDriver, capturedBusShiftIndex);
            });

            busSlot.RemoveButton = CreateButton($"BusDriverRemoveButton{busShiftIndex}", busShiftRow, font, out _, "×", 12, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
            LayoutElement busDriverRemoveLayout = busSlot.RemoveButton.gameObject.AddComponent<LayoutElement>();
            busDriverRemoveLayout.preferredWidth = 28f;
            busDriverRemoveLayout.preferredHeight = 28f;
            busSlot.RemoveButton.onClick.AddListener(() => RemoveBusDriverAssignment(capturedBusShiftIndex));
        }

        // ── Logistics panel ──────────────────────────────────────────────────
        GameObject logisticsPanelObj = CreateUiObject("LogisticsPanel", rightPanel);
        shiftsLogisticsPanel = logisticsPanelObj.GetComponent<RectTransform>();
        LayoutElement logisticsPanelLayout = logisticsPanelObj.AddComponent<LayoutElement>();
        logisticsPanelLayout.preferredHeight = GetShiftsTabContentHeight();
        logisticsPanelLayout.minHeight = GetShiftsTabContentHeight();
        logisticsPanelLayout.flexibleHeight = 0f;
        ScrollRect logisticsScroll = logisticsPanelObj.AddComponent<ScrollRect>();
        shiftsScreenUi.ProductionScrollRect = logisticsScroll;
        logisticsScroll.horizontal = false; logisticsScroll.vertical = true;
        logisticsScroll.movementType = ScrollRect.MovementType.Clamped;
        logisticsScroll.scrollSensitivity = 30f; logisticsScroll.inertia = false;
        logisticsScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        GameObject logisticsViewportObj = CreateUiObject("Viewport", logisticsPanelObj.transform);
        RectTransform logisticsViewport = logisticsViewportObj.GetComponent<RectTransform>();
        StretchRect(logisticsViewport, 0f, 18f, 0f, 0f);
        Image logisticsViewportImage = logisticsViewportObj.AddComponent<Image>();
        logisticsViewportImage.color = new Color(0f, 0f, 0f, 0.02f);
        logisticsViewportObj.AddComponent<Mask>().showMaskGraphic = false;

        Scrollbar logisticsScrollbar = CreatePanelScrollbar("ProductionScrollbar", logisticsPanelObj.transform);
        logisticsScroll.viewport = logisticsViewport;
        logisticsScroll.verticalScrollbar = logisticsScrollbar;

        GameObject logisticsContentGo = CreateUiObject("LogisticsContent", logisticsViewportObj.transform);
        RectTransform logisticsContent = logisticsContentGo.GetComponent<RectTransform>();
        logisticsContent.anchorMin = new Vector2(0f, 1f); logisticsContent.anchorMax = new Vector2(1f, 1f);
        logisticsContent.pivot = new Vector2(0.5f, 1f);
        logisticsContent.anchoredPosition = Vector2.zero; logisticsContent.sizeDelta = Vector2.zero;
        VerticalLayoutGroup logLayout = logisticsContentGo.AddComponent<VerticalLayoutGroup>();
        logLayout.spacing = 14;
        logLayout.childControlWidth = true;
        logLayout.childControlHeight = true;
        logLayout.childForceExpandWidth = true;
        logLayout.childForceExpandHeight = false;
        logLayout.childAlignment = TextAnchor.UpperLeft;
        logisticsContentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        logisticsScroll.content = logisticsContent;

        RectTransform productionIntroCard = CreateSectionCard(logisticsContent, font, string.Empty, out RectTransform productionIntroBody, false);
        productionIntroCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 78f;
        VerticalLayoutGroup productionIntroLayout = productionIntroBody.GetComponent<VerticalLayoutGroup>();
        productionIntroLayout.spacing = 6f;
        shiftsScreenUi.ProductionSectionTitleText = CreateHeaderText("AssignmentsProductionSectionTitle", productionIntroBody, font, "Production", 16, TextAnchor.MiddleLeft, Color.white);
        shiftsScreenUi.ProductionSectionSummaryText = CreateBodyText("AssignmentsProductionSectionSummary", productionIntroBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);

        int slotArrayIndex = 0;
        LocationType[] productionTypes = { LocationType.Forest, LocationType.Sawmill, LocationType.FurnitureFactory, LocationType.Docks };
        for (int pi = 0; pi < productionTypes.Length; pi++)
        {
            int maxWorkerSlots = GetMaxBuildingWorkerSlots(productionTypes[pi]);
            for (int workerSlot = 0; workerSlot < maxWorkerSlots; workerSlot++)
            {
                slotArrayIndex = AddBuildingWorkerSlotCard(logisticsContent, font, slotArrayIndex, productionTypes[pi], workerSlot);
            }
        }

        slotArrayIndex = AddWarehouseWorkerSlotCard(logisticsContent, font, slotArrayIndex);

        RectTransform serviceIntroCard = CreateSectionCard(logisticsContent, font, string.Empty, out RectTransform serviceIntroBody, false);
        serviceIntroCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        VerticalLayoutGroup serviceIntroLayout = serviceIntroBody.GetComponent<VerticalLayoutGroup>();
        serviceIntroLayout.spacing = 6f;
        CreateHeaderText("AssignmentsServiceSectionTitle", serviceIntroBody, font, "Services", 16, TextAnchor.MiddleLeft, Color.white);
        CreateBodyText("AssignmentsServiceSectionSummary", serviceIntroBody, font, "One staff slot per service building.", 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);

        LocationType[] serviceTypes = { LocationType.Motel, LocationType.Bar, LocationType.Canteen, LocationType.GasStation, LocationType.GamblingHall, LocationType.CarMarket, LocationType.LaborExchange };
        for (int si = 0; si < serviceTypes.Length; si++)
        {
            slotArrayIndex = AddBuildingWorkerSlotCard(logisticsContent, font, slotArrayIndex, serviceTypes[si], 0);
        }

        // Start with Transport tab visible
        shiftsLogisticsPanel.gameObject.SetActive(false);
        ApplyShiftsTabVisuals();
        EnforceShiftsWindowLayout();

        AddOverlayCloseButton(windowRect, font);
        shiftsScreenUi.CanvasRoot.SetActive(false);
        UpdateShiftsScreenUi();
    }

    private int AddBuildingWorkerSlotCard(RectTransform parent, Font font, int slotArrayIndex, LocationType buildingType, int workerSlot)
    {
        LogisticsSlotUi slot = new() { BuildingType = buildingType, SlotIndex = workerSlot };
        RectTransform slotCard = CreateSectionCard(parent, font, string.Empty, out RectTransform slotBody, false);
        slot.Root = slotCard;
        LayoutElement slotCardLE = slotCard.gameObject.AddComponent<LayoutElement>();
        slotCardLE.preferredHeight = 102f;
        slotCardLE.flexibleHeight = 0f;
        VerticalLayoutGroup slotBodyLayout = slotBody.GetComponent<VerticalLayoutGroup>();
        slotBodyLayout.spacing = 4;

        slot.BuildingNameText = CreateHeaderText($"LogBldgName{slotArrayIndex}", slotBody, font, GetBuildingWorkerSlotTitle(buildingType, workerSlot), 16, TextAnchor.MiddleLeft, Color.white);
        slot.AssignedWorkerText = CreateHeaderText($"LogWorker{slotArrayIndex}", slotBody, font, "No worker assigned", 14, TextAnchor.MiddleLeft, FleetAccentColor);
        slot.AssignedWorkerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform workRow = CreateLayoutRow($"LogWorkRow{slotArrayIndex}", slotBody, 22f, 8f);
        workRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        CreateBodyText($"LogWorkLabel{slotArrayIndex}", workRow, font, "Hours:", 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        slot.WorkHoursText = CreateHeaderText($"LogWorkHours{slotArrayIndex}", workRow, font, GetProductionWorkRangeLabel(), 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        slot.WorkHoursText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform actionRow = CreateLayoutRow($"LogActionRow{slotArrayIndex}", slotBody, 26f, 8f);
        actionRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        slot.AssignButton = CreateButton($"LogAssignBtn{slotArrayIndex}", actionRow, font, out slot.AssignButtonText, "Assign Worker", 12, FleetPrimaryButtonColor, Color.white);
        slot.AssignButton.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        slot.RemoveButton = CreateButton($"LogRemoveBtn{slotArrayIndex}", actionRow, font, out _, "Remove", 12, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
        slot.RemoveButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;

        int capturedIndex = slotArrayIndex;
        slot.AssignButton.onClick.AddListener(() =>
        {
            DriverAgent selectedDriver = driverAgents.Find(d => d.DriverId == selectedShiftDriverId);
            if (selectedDriver == null) return;
            AssignWorkerToBuilding(selectedDriver, logisticsSlots[capturedIndex]);
            PlayUiSound(uiSelectClip, 0.85f);
        });
        slot.RemoveButton.onClick.AddListener(() =>
        {
            RemoveWorkerFromBuilding(logisticsSlots[capturedIndex]);
            PlayUiSound(uiSelectClip, 0.85f);
        });
        logisticsSlots[slotArrayIndex] = slot;
        return slotArrayIndex + 1;
    }

    private int AddWarehouseWorkerSlotCard(RectTransform parent, Font font, int slotArrayIndex)
    {
        RectTransform warehouseCard = CreateSectionCard(parent, font, string.Empty, out RectTransform warehouseBody, false);
        LayoutElement warehouseCardLE = warehouseCard.gameObject.AddComponent<LayoutElement>();
        warehouseCardLE.preferredHeight = 126f + WarehouseMaxWorkers * 28f;
        warehouseCardLE.flexibleHeight = 0f;
        VerticalLayoutGroup warehouseBodyLayout = warehouseBody.GetComponent<VerticalLayoutGroup>();
        warehouseBodyLayout.spacing = 4;

        Text warehouseTitle = CreateHeaderText("LogBldgNameWarehouse", warehouseBody, font, "Warehouse", 16, TextAnchor.MiddleLeft, Color.white);
        RectTransform workRow = CreateLayoutRow("LogWorkRowWarehouse", warehouseBody, 22f, 8f);
        workRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        CreateBodyText("LogWorkLabelWarehouse", workRow, font, "Hours:", 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        Text warehouseWorkHours = CreateHeaderText("LogWorkHoursWarehouse", workRow, font, GetProductionWorkRangeLabel(), 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        warehouseWorkHours.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        for (int wi = 0; wi < WarehouseMaxWorkers; wi++)
        {
            LogisticsSlotUi wSlot = new() { BuildingType = LocationType.Warehouse, SlotIndex = wi, Root = warehouseCard };
            if (wi == 0)
            {
                wSlot.WorkHoursText = warehouseWorkHours;
                wSlot.BuildingNameText = warehouseTitle;
            }

            RectTransform wActionRow = CreateLayoutRow($"LogActionRowWarehouse{wi}", warehouseBody, 26f, 4f);
            wActionRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            wSlot.AssignedWorkerText = CreateHeaderText($"LogWorkerWarehouse{wi}", wActionRow, font, $"Loader {wi + 1}: -", 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            wSlot.AssignedWorkerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            wSlot.AssignButton = CreateButton($"LogAssignBtnWarehouse{wi}", wActionRow, font, out wSlot.AssignButtonText, "Assign", 11, FleetPrimaryButtonColor, Color.white);
            wSlot.AssignButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 104f;
            wSlot.RemoveButton = CreateButton($"LogRemoveBtnWarehouse{wi}", wActionRow, font, out _, "X", 12, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
            wSlot.RemoveButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 28f;

            int capturedIdx = slotArrayIndex;
            wSlot.AssignButton.onClick.AddListener(() =>
            {
                DriverAgent sel = driverAgents.Find(d => d.DriverId == selectedShiftDriverId);
                if (sel == null) return;
                AssignWorkerToBuilding(sel, logisticsSlots[capturedIdx]);
                PlayUiSound(uiSelectClip, 0.85f);
            });
            wSlot.RemoveButton.onClick.AddListener(() =>
            {
                RemoveWorkerFromBuilding(logisticsSlots[capturedIdx]);
                PlayUiSound(uiSelectClip, 0.85f);
            });
            logisticsSlots[slotArrayIndex] = wSlot;
            slotArrayIndex++;
        }

        return slotArrayIndex;
    }

    private string GetBuildingWorkerSlotTitle(LocationType buildingType, int workerSlot, int locationInstanceId = 0) =>
        GetMaxBuildingWorkerSlots(buildingType) > 1
            ? $"{GetBuildingInstanceDisplayName(buildingType, locationInstanceId)} #{workerSlot + 1}"
            : GetBuildingInstanceDisplayName(buildingType, locationInstanceId);
}
