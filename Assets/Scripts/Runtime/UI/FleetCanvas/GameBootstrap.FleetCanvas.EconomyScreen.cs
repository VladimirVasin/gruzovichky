using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void SetupEconomyScreenUi()
    {
        if (economyScreenUi != null) return;

        EnsureDefaultTaxPolicies();
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
        SetCenteredWindow(windowRect, 940f, 700f, -16f);
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
        Text titleText = CreateHeaderText("EconomyTitle", headerRow, font, "Economy", 30, TextAnchor.MiddleLeft, Color.white);
        titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        economyScreenUi.HeaderCountText = CreateHeaderText("EconomyCount", headerRow, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        RectTransform tabRow = CreateTabRow("EconomyTabRow", windowRoot.transform, 40f, 8f);
        economyScreenUi.TaxesTabButton = CreateButton("EconomyTaxesTabBtn", tabRow, font, out Text taxesTabText, "Taxes", 13, FleetPrimaryButtonColor, Color.white);
        economyScreenUi.TaxesTabText = taxesTabText;
        taxesTabText.fontStyle = FontStyle.Bold;
        economyScreenUi.TaxesTabButton.transition = Selectable.Transition.None;
        LayoutElement taxesTabButtonLayout = economyScreenUi.TaxesTabButton.gameObject.AddComponent<LayoutElement>();
        taxesTabButtonLayout.preferredHeight = 40f;
        taxesTabButtonLayout.flexibleWidth = 1f;
        economyScreenUi.TaxesTabButton.onClick.AddListener(() =>
        {
            isEconomyTaxesTabActive = true;
            isEconomyScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.8f);
        });
        economyScreenUi.TradeTabButton = CreateButton("EconomyTradeTabBtn", tabRow, font, out Text tradeTabText, "Trade", 13, new Color(0.08f, 0.10f, 0.14f, 1f), Color.white);
        economyScreenUi.TradeTabText = tradeTabText;
        tradeTabText.fontStyle = FontStyle.Bold;
        economyScreenUi.TradeTabButton.transition = Selectable.Transition.None;
        LayoutElement tradeTabButtonLayout = economyScreenUi.TradeTabButton.gameObject.AddComponent<LayoutElement>();
        tradeTabButtonLayout.preferredHeight = 40f;
        tradeTabButtonLayout.flexibleWidth = 1f;
        economyScreenUi.TradeTabButton.onClick.AddListener(() =>
        {
            isEconomyTaxesTabActive = false;
            isEconomyScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.8f);
        });
        RectTransform taxesPanel = CreateUiObject("EconomyTaxesPanel", windowRoot.transform).GetComponent<RectTransform>();
        LayoutElement taxesPanelElement = taxesPanel.gameObject.AddComponent<LayoutElement>();
        taxesPanelElement.preferredHeight = 560f;
        taxesPanelElement.flexibleHeight = 1f;
        VerticalLayoutGroup taxesPanelLayout = taxesPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        taxesPanelLayout.spacing = 12f;
        taxesPanelLayout.childControlWidth = true;
        taxesPanelLayout.childControlHeight = true;
        taxesPanelLayout.childForceExpandWidth = true;
        taxesPanelLayout.childForceExpandHeight = false;
        economyScreenUi.TaxesPanel = taxesPanel;

        RectTransform taxesCardsRow = CreateLayoutRow("TaxesCardsRow", taxesPanel, 182f, 12f);
        HorizontalLayoutGroup taxesCardsRowLayout = taxesCardsRow.GetComponent<HorizontalLayoutGroup>();
        taxesCardsRowLayout.childForceExpandWidth = true;
        taxesCardsRowLayout.childForceExpandHeight = true;

        RectTransform rateCard = CreateSectionCard(taxesCardsRow, font, "Tax rate", out RectTransform rateBody);
        LayoutElement rateCardLayout = rateCard.gameObject.AddComponent<LayoutElement>();
        rateCardLayout.preferredWidth = 250f;
        rateCardLayout.preferredHeight = 182f;
        rateCardLayout.flexibleWidth = 1f;
        VerticalLayoutGroup rateBodyLayout = rateBody.GetComponent<VerticalLayoutGroup>();
        rateBodyLayout.spacing = 14f;
        rateBodyLayout.childAlignment = TextAnchor.MiddleCenter;

        RectTransform rateControlsRow = CreateLayoutRow("TaxesRateControlsRow", rateBody, 40f, 12f);
        HorizontalLayoutGroup rateControlsLayout = rateControlsRow.GetComponent<HorizontalLayoutGroup>();
        rateControlsLayout.childAlignment = TextAnchor.MiddleCenter;
        rateControlsLayout.childForceExpandWidth = false;
        rateControlsLayout.childForceExpandHeight = false;
        CreateSpacer("TaxesRateLeftSpacer", rateControlsRow, flexibleWidth: 1f);
        economyScreenUi.TaxesRateMinusButton = CreateButton("TaxesRateMinus", rateControlsRow, font, out Text taxMinusText, "-", 18, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        taxMinusText.fontStyle = FontStyle.Bold;
        LayoutElement taxMinusLayout = economyScreenUi.TaxesRateMinusButton.gameObject.AddComponent<LayoutElement>();
        taxMinusLayout.preferredWidth = 40f;
        taxMinusLayout.preferredHeight = 40f;
        economyScreenUi.TaxesRateMinusButton.onClick.AddListener(() =>
        {
            AdjustPrimaryTaxRate(-1);
        });
        economyScreenUi.TaxesRateValueText = CreateHeaderText("TaxesRateValue", rateControlsRow, font, string.Empty, 18, TextAnchor.MiddleCenter, FleetAccentColor);
        LayoutElement taxRateValueLayout = economyScreenUi.TaxesRateValueText.gameObject.AddComponent<LayoutElement>();
        taxRateValueLayout.preferredWidth = 68f;
        taxRateValueLayout.preferredHeight = 24f;
        economyScreenUi.TaxesRatePlusButton = CreateButton("TaxesRatePlus", rateControlsRow, font, out Text taxPlusText, "+", 18, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        taxPlusText.fontStyle = FontStyle.Bold;
        LayoutElement taxPlusLayout = economyScreenUi.TaxesRatePlusButton.gameObject.AddComponent<LayoutElement>();
        taxPlusLayout.preferredWidth = 40f;
        taxPlusLayout.preferredHeight = 40f;
        economyScreenUi.TaxesRatePlusButton.onClick.AddListener(() =>
        {
            AdjustPrimaryTaxRate(1);
        });
        CreateSpacer("TaxesRateRightSpacer", rateControlsRow, flexibleWidth: 1f);

        RectTransform incomeCard = CreateSectionCard(taxesCardsRow, font, "Income", out RectTransform incomeBody);
        LayoutElement incomeCardLayout = incomeCard.gameObject.AddComponent<LayoutElement>();
        incomeCardLayout.preferredWidth = 320f;
        incomeCardLayout.preferredHeight = 182f;
        incomeCardLayout.flexibleWidth = 1f;
        economyScreenUi.TaxesIncomeSummaryText = CreateBodyText("TaxesIncomeSummary", incomeBody, font, string.Empty, 14, TextAnchor.UpperLeft, Color.white);
        economyScreenUi.TaxesIncomeSummaryText.lineSpacing = 1.08f;
        LayoutElement incomeLayout = economyScreenUi.TaxesIncomeSummaryText.gameObject.AddComponent<LayoutElement>();
        incomeLayout.flexibleHeight = 1f;

        RectTransform timerCard = CreateSectionCard(taxesCardsRow, font, "Timer", out RectTransform timerBody);
        LayoutElement timerCardLayout = timerCard.gameObject.AddComponent<LayoutElement>();
        timerCardLayout.preferredWidth = 220f;
        timerCardLayout.preferredHeight = 182f;
        timerCardLayout.flexibleWidth = 1f;
        economyScreenUi.TaxesTimerSummaryText = CreateBodyText("TaxesTimerSummary", timerBody, font, string.Empty, 14, TextAnchor.UpperLeft, Color.white);
        economyScreenUi.TaxesTimerSummaryText.lineSpacing = 1.08f;
        LayoutElement timerLayout = economyScreenUi.TaxesTimerSummaryText.gameObject.AddComponent<LayoutElement>();
        timerLayout.flexibleHeight = 1f;

        RectTransform policyFrame = CreateSectionCard(taxesPanel, font, "Tax policies", out RectTransform policyBody);
        LayoutElement policyFrameLayout = policyFrame.gameObject.AddComponent<LayoutElement>();
        policyFrameLayout.preferredHeight = 340f;
        policyFrameLayout.flexibleHeight = 1f;

        economyScreenUi.TaxesPolicySummaryText = CreateBodyText("TaxesPolicySummary", policyBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        economyScreenUi.TaxesPolicySummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        GameObject policyScrollObj = CreateUiObject("TaxPolicyScrollView", policyBody);
        RectTransform policyScrollRoot = policyScrollObj.GetComponent<RectTransform>();
        policyScrollRoot.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        Image policyScrollImage = policyScrollObj.AddComponent<Image>();
        policyScrollImage.color = new Color(0f, 0f, 0f, 0f);
        ScrollRect policyScroll = policyScrollObj.AddComponent<ScrollRect>();
        policyScroll.horizontal = false;
        policyScroll.scrollSensitivity = 24f;

        GameObject policyViewportObj = CreateUiObject("Viewport", policyScrollObj.transform);
        StretchRect(policyViewportObj.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        Image policyViewportImage = policyViewportObj.AddComponent<Image>();
        policyViewportImage.color = new Color(0f, 0f, 0f, 0.04f);
        policyViewportObj.AddComponent<Mask>().showMaskGraphic = false;

        GameObject policyRowsObj = CreateUiObject("TaxPolicyRows", policyViewportObj.transform);
        RectTransform policyRowsRoot = policyRowsObj.GetComponent<RectTransform>();
        policyRowsRoot.anchorMin = new Vector2(0f, 1f);
        policyRowsRoot.anchorMax = new Vector2(1f, 1f);
        policyRowsRoot.pivot = new Vector2(0.5f, 1f);
        policyRowsRoot.anchoredPosition = Vector2.zero;
        policyRowsRoot.sizeDelta = Vector2.zero;
        VerticalLayoutGroup policyRowsLayout = policyRowsObj.AddComponent<VerticalLayoutGroup>();
        policyRowsLayout.spacing = 6f;
        policyRowsLayout.childControlWidth = true;
        policyRowsLayout.childControlHeight = true;
        policyRowsLayout.childForceExpandWidth = true;
        policyRowsLayout.childForceExpandHeight = false;
        policyRowsObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        policyScroll.viewport = policyViewportObj.GetComponent<RectTransform>();
        policyScroll.content = policyRowsRoot;
        economyScreenUi.TaxPoliciesContent = policyRowsRoot;

        for (int i = 0; i < taxPolicies.Count; i++)
        {
            TaxPolicyRowUi row = CreateTaxPolicyRow(policyRowsRoot, font, i);
            row.ToggleButton.onClick.AddListener(() => ToggleTaxPolicy(row.PolicyId));
            row.RateMinusButton.onClick.AddListener(() => AdjustTaxPolicyRate(row.PolicyId, -1, checkTutorialGoal: true));
            row.RatePlusButton.onClick.AddListener(() => AdjustTaxPolicyRate(row.PolicyId, 1, checkTutorialGoal: true));
            economyScreenUi.TaxPolicyRows.Add(row);
        }

        RectTransform tradePanel = CreateUiObject("EconomyTradePanel", windowRoot.transform).GetComponent<RectTransform>();
        tradePanel.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        VerticalLayoutGroup tradePanelLayout = tradePanel.gameObject.AddComponent<VerticalLayoutGroup>();
        tradePanelLayout.spacing = 16f;
        tradePanelLayout.childControlWidth = true;
        tradePanelLayout.childControlHeight = true;
        tradePanelLayout.childForceExpandWidth = true;
        tradePanelLayout.childForceExpandHeight = false;
        economyScreenUi.TradePanel = tradePanel;

        RectTransform tradeCard = CreateSectionCard(tradePanel, font, "Create Order", out RectTransform tradeBody);
        tradeCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 214f;

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
            selectedTradeOrderAmount = Mathf.Min(selectedTradeOrderAmount + 1, TruckCargoCapacity);
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

        RectTransform ordersFrame = CreateSectionCard(tradePanel, font, "Active Orders", out RectTransform ordersBody);
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

        RectTransform regionTag = CreateStyledPanel($"TradeOrderRegion{rowIndex}", card, new Color(0.18f, 0.24f, 0.32f, 1f));
        regionTag.gameObject.AddComponent<LayoutElement>().preferredWidth = 136f;
        row.RegionText = CreateBodyText("RegionText", regionTag, font, string.Empty, 11, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
        StretchRect(row.RegionText.rectTransform, 6f, 2f, 6f, 2f);

        row.RemoveButton = CreateButton($"TradeOrderRemove{rowIndex}", card, font, out Text removeText, "X", 18, new Color(0.74f, 0.55f, 0.08f, 1f), Color.white);
        removeText.fontStyle = FontStyle.Bold;
        row.RemoveButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 46f;
        return row;
    }

    private static TaxPolicyRowUi CreateTaxPolicyRow(RectTransform parent, Font font, int rowIndex)
    {
        TaxPolicyRowUi row = new();
        RectTransform card = CreateStyledPanel($"TaxPolicyRow{rowIndex}", parent, new Color(0.055f, 0.090f, 0.125f, 0.96f));
        row.Root = card;
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

        HorizontalLayoutGroup layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 5, 5);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.childAlignment = TextAnchor.MiddleCenter;

        row.ToggleButton = CreateButton($"TaxPolicyToggle{rowIndex}", card, font, out Text toggleText, string.Empty, 12, new Color(0.14f, 0.18f, 0.23f, 1f), Color.white);
        row.ToggleText = toggleText;
        row.ToggleText.fontStyle = FontStyle.Bold;
        row.ToggleButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 58f;

        RectTransform textStack = CreateUiObject($"TaxPolicyText{rowIndex}", card).GetComponent<RectTransform>();
        textStack.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup textLayout = textStack.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = 2f;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;

        row.NameText = CreateHeaderText("Name", textStack, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        row.NameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 19f;
        row.MetaText = CreateBodyText("Meta", textStack, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        row.MetaText.gameObject.AddComponent<LayoutElement>().preferredHeight = 17f;

        row.RateMinusButton = CreateButton($"TaxPolicyMinus{rowIndex}", card, font, out Text minusText, "-", 16, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        minusText.fontStyle = FontStyle.Bold;
        row.RateMinusButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 34f;

        row.RateText = CreateHeaderText("Rate", card, font, string.Empty, 14, TextAnchor.MiddleCenter, FleetAccentColor);
        row.RateText.gameObject.AddComponent<LayoutElement>().preferredWidth = 54f;

        row.RatePlusButton = CreateButton($"TaxPolicyPlus{rowIndex}", card, font, out Text plusText, "+", 16, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        plusText.fontStyle = FontStyle.Bold;
        row.RatePlusButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 34f;
        return row;
    }

    private void UpdateEconomyScreenUi()
    {
        if (economyScreenUi == null) return;

        EnsureDefaultTaxPolicies();
        EnsureTaxDayStatsCurrent();
        bool shouldShow = isEconomyPanelOpen;
        if (economyScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            economyScreenUi.CanvasRoot.SetActive(shouldShow);
            isEconomyScreenDirty = true;
        }

        if (!shouldShow) return;
        bool forceLayoutRebuild = isEconomyScreenDirty;

        EnsureTradeHudSelectionValid();
        int taxableBankTotal = GetCurrentTaxableBuildingBankTotal();
        if (economyScreenUi.TaxesPanel != null) economyScreenUi.TaxesPanel.gameObject.SetActive(isEconomyTaxesTabActive);
        if (economyScreenUi.TradePanel != null) economyScreenUi.TradePanel.gameObject.SetActive(!isEconomyTaxesTabActive);
        ApplyShiftsTabVisual(economyScreenUi.TaxesTabButton, economyScreenUi.TaxesTabText, isEconomyTaxesTabActive);
        ApplyShiftsTabVisual(economyScreenUi.TradeTabButton, economyScreenUi.TradeTabText, !isEconomyTaxesTabActive);
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
            if (row.RegionText != null)
            {
                bool hasRegion = order.TargetRegionIndex >= 0;
                row.RegionText.transform.parent.gameObject.SetActive(hasRegion);
                row.RegionText.text = hasRegion ? GetTradeOrderRegionTag(order.TargetRegionIndex) : string.Empty;
            }
        }

        if (forceLayoutRebuild)
        {
            if (economyScreenUi.ActiveOrdersContent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(economyScreenUi.ActiveOrdersContent);
            if (economyScreenUi.TaxesPanel != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(economyScreenUi.TaxesPanel);
            if (economyScreenUi.TradePanel != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(economyScreenUi.TradePanel);
            LayoutRebuilder.ForceRebuildLayoutImmediate(economyScreenUi.WindowRoot);
        }

        LocalizeCanvas(economyScreenUi.CanvasRoot);
        TaxPolicy primaryPolicy = GetPrimaryTaxPolicy();
        economyScreenUi.HeaderCountText.text = $"{L("Treasury")}: {FormatTreasuryAmount()}";
        economyScreenUi.HeaderCountText.color = money < 0 ? GetTreasuryDisplayColor() : FleetSecondaryTextColor;
        economyScreenUi.TaxesRateValueText.text = primaryPolicy != null ? $"{primaryPolicy.RatePercent}%" : "0%";
        economyScreenUi.TaxesIncomeSummaryText.text =
            $"{L("Current taxable bank")}: ${taxableBankTotal}\n" +
            $"{L("Taxes today")}: ${taxCollectedToday} / {taxEventsToday}\n" +
            $"{L("Previous day")}: ${taxCollectedPreviousDay} / {taxEventsPreviousDay}";
        economyScreenUi.TaxesTimerSummaryText.text =
            $"{L("Next collection")}: 00:00\n" +
            $"{L("Day")}: {currentDay} ({GetWeekDayLabel()})\n" +
            $"{L("Last daily reserve tax")}: ${lastTaxCollectedAmount} / {lastTaxedBuildingCount}";
        if (economyScreenUi.TaxesPolicySummaryText != null)
        {
            int enabledPolicies = 0;
            for (int i = 0; i < taxPolicies.Count; i++)
                if (taxPolicies[i].IsEnabled) enabledPolicies++;
            economyScreenUi.TaxesPolicySummaryText.text =
                $"{L("Enabled policies")}: {enabledPolicies}/{taxPolicies.Count}  -  {L("Primary rate controls service sales tax")}";
        }

        economyScreenUi.TaxesRateMinusButton.interactable = primaryPolicy != null && primaryPolicy.RatePercent > MinDailyBuildingTaxPercent;
        economyScreenUi.TaxesRatePlusButton.interactable = primaryPolicy != null && primaryPolicy.RatePercent < MaxDailyBuildingTaxPercent;
        UpdateTaxPolicyRows();
        isEconomyScreenDirty = false;
    }

    private void UpdateTaxPolicyRows()
    {
        if (economyScreenUi == null)
        {
            return;
        }

        EnsureTaxPolicyRowCapacity(taxPolicies.Count);

        for (int i = 0; i < economyScreenUi.TaxPolicyRows.Count; i++)
        {
            TaxPolicyRowUi row = economyScreenUi.TaxPolicyRows[i];
            bool active = i < taxPolicies.Count;
            row.Root.gameObject.SetActive(active);
            if (!active)
            {
                row.PolicyId = 0;
                continue;
            }

            TaxPolicy policy = taxPolicies[i];
            row.PolicyId = policy.Id;
            row.ToggleText.text = policy.IsEnabled ? L("ON") : L("OFF");
            row.ToggleButton.image.color = policy.IsEnabled
                ? new Color(0.18f, 0.42f, 0.24f, 1f)
                : new Color(0.17f, 0.19f, 0.23f, 1f);
            row.NameText.text = L(policy.Name);
            row.MetaText.text = $"{GetTaxSourceLabel(policy.SourceKind)} - {GetTaxFrequencyLabel(policy.Frequency)} - {GetTaxIncidenceLabel(policy.Incidence)}";
            row.RateText.text = $"{policy.RatePercent}%";
            row.RateMinusButton.interactable = policy.RatePercent > MinDailyBuildingTaxPercent;
            row.RatePlusButton.interactable = policy.RatePercent < MaxDailyBuildingTaxPercent;
        }
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

        TradeHudOrder order = TradeOrderQueueService.CreateOrder(
            nextTradeOrderId++,
            selectedTradeResourceType,
            selectedTradeOrderType,
            selectedTradeOrderAmount);
        activeTradeHudOrders.Add(order);
        NotifyTutorialTradePolicyChanged(order.ResourceType);
        isTradeResourceDropdownOpen = false;
        isTradeActionDropdownOpen = false;
        SessionDebugLogger.Log(
            "TRADE_HUD",
            $"Created order #{order.Id}: {selectedTradeOrderType} {selectedTradeOrderAmount} {GetTradeResourceShortLabel(selectedTradeResourceType)}; queue={activeTradeHudOrders.Count}; targetRegion={order.TargetRegionIndex}.");
        string tradeActionEn = selectedTradeOrderType == TradeOrderType.Buy ? "Buy" : "Sell";
        string tradeActionRu = selectedTradeOrderType == TradeOrderType.Buy ? "Покупка" : "Продажа";
        string resourceLabelEn = GetTradeResourceShortLabel(selectedTradeResourceType);
        string resourceLabelRu = L(resourceLabelEn);
        PushFeedEvent(
            $"{tradeActionEn} order placed: {resourceLabelEn} x{selectedTradeOrderAmount}.",
            $"Создан ордер: {tradeActionRu} {resourceLabelRu} x{selectedTradeOrderAmount}.",
            FeedEventType.Money);
        PlayUiSound(uiSelectClip, 0.9f);
        TryAutoDispatchNextHudOrder();
    }

    private void RemoveTradeHudOrder(int orderId)
    {
        int removed = TradeOrderQueueService.RemoveById(activeTradeHudOrders, orderId);
        if (removed <= 0)
        {
            return;
        }

        isEconomyScreenDirty = true;
        isWorldMapScreenDirty = true;
        SessionDebugLogger.Log("TRADE_HUD", $"Removed trade order #{orderId}; removed={removed}; queue={activeTradeHudOrders.Count}.");
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
            TradeResourceType.Alcohol => "Alcohol",
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
            TradeResourceType.Alcohol => "Medium",
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
            TradeResourceType.Alcohol   => "Alcohol",
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

        if (tradeDispatchStatusText == "Assign a Truck Driver shift to unlock trade dispatch.")
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
        detail += $" - {entry.FromAccountKind} -> {entry.ToAccountKind}";
        if (entry.RecipientBalanceAfter.HasValue)
        {
            detail += $" - {entry.ToLabel} balance: ${entry.RecipientBalanceAfter.Value}";
        }

        if (entry.TreasuryAfter.HasValue)
        {
            detail += $" - Treasury: ${entry.TreasuryAfter.Value}";
        }

        return detail;
    }

}
