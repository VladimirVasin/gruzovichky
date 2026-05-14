using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void SetupTradeScreenUi()
    {
        if (tradeScreenUi != null) return;

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tradeScreenUi = new TradeScreenUiRefs();

        GameObject canvasObject = new("TradeScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 70;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        tradeScreenUi.CanvasRoot = canvasObject;

        GameObject windowRoot = CreateUiObject("TradeWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        StretchRect(windowRect, 0f, 0f, 0f, 0f);
        tradeScreenUi.WindowRoot = windowRect;

        Image windowBg = windowRoot.AddComponent<Image>();
        windowBg.color = new Color(0.07f, 0.10f, 0.15f, 0.96f);
        Outline outline = windowRoot.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        outline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup rootLayout = windowRoot.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(32, 32, 28, 28);
        rootLayout.spacing = 14f;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("TradeHeaderRow", windowRoot.transform, 52f, 10f);
        Text titleText = CreateHeaderText("TradeTitle", headerRow, font, "Trade", 32, TextAnchor.MiddleLeft, Color.white);
        titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        tradeScreenUi.HeaderCountText = CreateHeaderText("TradeHeaderCount", headerRow, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        RectTransform statusCard = CreateSectionCard(windowRoot.transform, font, IsRussianLanguage() ? "Политики торговли" : "Trade Policies", out RectTransform statusBody);
        statusCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 76f;
        tradeScreenUi.StatusText = CreateBodyText("TradeStatus", statusBody, font, string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
        tradeScreenUi.StatusText.lineSpacing = 1.08f;

        RectTransform tableCard = CreateSectionCard(windowRoot.transform, font, IsRussianLanguage() ? "Склад" : "Warehouse Stock", out RectTransform tableBody);
        LayoutElement tableLayout = tableCard.gameObject.AddComponent<LayoutElement>();
        tableLayout.flexibleHeight = 1f;
        tableLayout.minHeight = 430f;

        RectTransform header = CreateLayoutRow("TradeTableHeader", tableBody, 30f, 10f);
        CreateTradeTableHeaderText(header, font, "Resource", 180f);
        CreateTradeTableHeaderText(header, font, IsRussianLanguage() ? "Склад" : "Stock", 80f);
        CreateTradeTableHeaderText(header, font, IsRussianLanguage() ? "Политика" : "Policy", 390f);
        CreateTradeTableHeaderText(header, font, IsRussianLanguage() ? "Норма" : "Target", 130f);
        CreateTradeTableHeaderText(header, font, "Status", 0f);

        FleetCanvasUiFactory.ScrollPanelRefs scroll = CreateVerticalScrollList("TradeRowsScroll", tableBody, "TradeRowsContent", 6f, preferredHeight: 390f);
        tradeScreenUi.RowsContent = scroll.Content;

        for (int i = 0; i < TradeHudResources.Length; i++)
        {
            tradeScreenUi.PolicyRows.Add(CreateTradePolicyRow(tradeScreenUi.RowsContent, font, TradeHudResources[i], i));
        }

        AddOverlayCloseButton(windowRect, font);
        tradeScreenUi.CanvasRoot.SetActive(false);
        UpdateTradeScreenUi();
    }

    private static void CreateTradeTableHeaderText(RectTransform parent, Font font, string label, float width)
    {
        Text text = CreateBodyText($"TradeHeader_{label}", parent, font, label, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        text.fontStyle = FontStyle.Bold;
        LayoutElement layout = text.gameObject.AddComponent<LayoutElement>();
        if (width > 0f)
            layout.preferredWidth = width;
        else
            layout.flexibleWidth = 1f;
    }

    private TradePolicyRowUi CreateTradePolicyRow(RectTransform parent, Font font, TradeResourceType resourceType, int rowIndex)
    {
        TradePolicyRowUi row = new() { ResourceType = resourceType };
        RectTransform rowRoot = CreateStyledPanel($"TradePolicyRow_{resourceType}", parent, FleetInsetColor);
        row.Root = rowRoot;
        rowRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;

        VerticalLayoutGroup rowLayout = rowRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        rowLayout.padding = new RectOffset(10, 10, 8, 8);
        rowLayout.spacing = 0f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = false;

        RectTransform mainRow = CreateLayoutRow($"TradePolicyMain_{resourceType}", rowRoot, 44f, 10f);
        mainRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        row.ResourceText = CreateHeaderText($"TradeResource_{resourceType}", mainRow, font, string.Empty, 15, TextAnchor.MiddleLeft, Color.white);
        row.ResourceText.gameObject.AddComponent<LayoutElement>().preferredWidth = 180f;
        row.WarehouseText = CreateHeaderText($"TradeWarehouse_{resourceType}", mainRow, font, string.Empty, 15, TextAnchor.MiddleLeft, Color.white);
        row.WarehouseText.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;

        row.ModeOptionsRoot = CreateLayoutRow($"TradeModeOptions_{resourceType}", mainRow, 36f, 4f);
        row.ModeOptionsRoot.gameObject.AddComponent<LayoutElement>().preferredWidth = 390f;
        row.NoTradeButton = CreateTradePolicyModeButton(row.ModeOptionsRoot, font, out row.NoTradeButtonText, TradePolicyMode.None, resourceType);
        row.SellAboveButton = CreateTradePolicyModeButton(row.ModeOptionsRoot, font, out row.SellAboveButtonText, TradePolicyMode.SellAbove, resourceType);
        row.BuyUpToButton = CreateTradePolicyModeButton(row.ModeOptionsRoot, font, out row.BuyUpToButtonText, TradePolicyMode.BuyUpTo, resourceType);

        RectTransform targetRow = CreateLayoutRow($"TradeTarget_{resourceType}", mainRow, 36f, 6f);
        targetRow.gameObject.AddComponent<LayoutElement>().preferredWidth = 130f;
        row.TargetMinusButton = CreateButton($"TradeTargetMinus_{resourceType}", targetRow, font, out row.TargetMinusText, "-", 16, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        row.TargetMinusButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 32f;
        row.TargetText = CreateHeaderText($"TradeTargetValue_{resourceType}", targetRow, font, string.Empty, 15, TextAnchor.MiddleCenter, Color.white);
        row.TargetText.gameObject.AddComponent<LayoutElement>().preferredWidth = 52f;
        row.TargetPlusButton = CreateButton($"TradeTargetPlus_{resourceType}", targetRow, font, out row.TargetPlusText, "+", 16, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        row.TargetPlusButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 32f;
        row.TargetMinusButton.onClick.AddListener(() => AdjustTradePolicyTarget(resourceType, -1));
        row.TargetPlusButton.onClick.AddListener(() => AdjustTradePolicyTarget(resourceType, 1));

        row.StatusText = CreateBodyText($"TradePolicyStatus_{resourceType}", mainRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        row.StatusText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        return row;
    }

    private Button CreateTradePolicyModeButton(RectTransform parent, Font font, out Text text, TradePolicyMode mode, TradeResourceType resourceType)
    {
        Button button = CreateButton($"TradePolicy_{resourceType}_{mode}", parent, font, out text, GetTradePolicyModeLabel(mode), 12, new Color(0.13f, 0.17f, 0.23f, 1f), Color.white);
        button.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = 9;
        text.resizeTextMaxSize = 12;
        button.onClick.AddListener(() => SetTradePolicyMode(resourceType, mode));
        return button;
    }

    private void UpdateTradeScreenUi()
    {
        if (tradeScreenUi == null) return;

        if (tradeScreenUi.CanvasRoot.activeSelf)
        {
            tradeScreenUi.CanvasRoot.SetActive(false);
        }
        isTradeScreenDirty = false;
    }

    private void UpdateTradePolicyRow(TradePolicyRowUi row, int rowIndex)
    {
        TradePolicyRowModel model = BuildTradePolicyRowModel(row.ResourceType);

        row.ResourceText.text = model.ResourceLabel;
        row.WarehouseText.text = model.WarehouseAmountText;
        row.TargetText.text = model.TargetText;
        row.StatusText.text = model.StatusText;
        row.NoTradeButtonText.text = model.NoTradeButton.Label;
        row.SellAboveButtonText.text = model.SellAboveButton.Label;
        row.BuyUpToButtonText.text = model.BuyUpToButton.Label;
        row.Root.gameObject.GetComponent<LayoutElement>().preferredHeight = 60f;

        row.TargetMinusButton.interactable = model.CanDecreaseTarget;
        row.TargetPlusButton.interactable = model.CanIncreaseTarget;

        UpdateTradeModeOptionButton(row.NoTradeButton, model.NoTradeButton);
        UpdateTradeModeOptionButton(row.SellAboveButton, model.SellAboveButton);
        UpdateTradeModeOptionButton(row.BuyUpToButton, model.BuyUpToButton);
    }

    private TradePolicyRowModel BuildTradePolicyRowModel(TradeResourceType resourceType)
    {
        TradePolicyMode mode = GetTradePolicyMode(resourceType);
        int target = GetTradePolicyTarget(resourceType);
        return TradeScreenModel.CreatePolicyRow(
            resourceType,
            L(GetTradeResourceShortLabel(resourceType)),
            GetWarehouseTradeResourceAmount(resourceType),
            mode,
            target,
            GetTradePolicyStatusText(resourceType),
            GetTradePolicyModeLabel(TradePolicyMode.None),
            GetTradePolicyModeLabel(TradePolicyMode.SellAbove),
            GetTradePolicyModeLabel(TradePolicyMode.BuyUpTo),
            IsTradePolicyModeSupported(resourceType, TradePolicyMode.None),
            IsTradePolicyModeSupported(resourceType, TradePolicyMode.SellAbove),
            IsTradePolicyModeSupported(resourceType, TradePolicyMode.BuyUpTo));
    }

    private void UpdateTradeModeOptionButton(Button button, TradePolicyModeButtonModel model)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = model.Supported;
        button.image.color = !model.Supported
            ? new Color(0.08f, 0.10f, 0.13f, 0.86f)
            : model.Selected ? FleetPrimaryButtonColor : new Color(0.13f, 0.17f, 0.23f, 1f);
    }

    private string BuildTradePoliciesSummaryText()
    {
        if (HasActiveTradeRun())
        {
            return GetTradeRunStatusLabel();
        }

        if (CountActiveTradePolicies() == 0)
        {
            return IsRussianLanguage()
                ? "Торговля выключена. Для ресурса выбери режим и целевой остаток склада."
                : "Trade is disabled. Pick a mode and warehouse target for a resource.";
        }

        return IsRussianLanguage()
            ? $"Активных политик: {CountActiveTradePolicies()}. Рейсы запускаются автоматически при наличии водителя и грузовика."
            : $"Active policies: {CountActiveTradePolicies()}. Land routes send merchant trucks; river routes wait for a ship at Docks.";
    }

    private void SetTradePolicyMode(TradeResourceType resourceType, TradePolicyMode mode)
    {
        int index = GetTradePolicyIndex(resourceType);
        if (index < 0)
        {
            return;
        }

        if (!IsTradePolicyModeSupported(resourceType, mode))
        {
            return;
        }

        if (!tradeState.TrySetPolicyMode(resourceType, mode, TradeHudResources))
        {
            return;
        }

        isTradeScreenDirty = true;

        SessionDebugLogger.Log("TRADE_POLICY", $"{resourceType} policy set to {mode}, target={GetTradePolicyTarget(resourceType)}.");
        NotifyTutorialTradePolicyChanged(resourceType);
        TryAutoDispatchNextHudOrder();
        PlayUiSound(uiSelectClip, 0.82f);
    }

    private void AdjustTradePolicyTarget(TradeResourceType resourceType, int delta)
    {
        int index = GetTradePolicyIndex(resourceType);
        if (index < 0)
        {
            return;
        }

        tradeState.AdjustPolicyTarget(resourceType, delta, 0, 99, TradeHudResources);
        isTradeScreenDirty = true;
        SessionDebugLogger.Log("TRADE_POLICY", $"{resourceType} target set to {GetTradePolicyTarget(resourceType)}.");
        NotifyTutorialTradePolicyChanged(resourceType);
        TryAutoDispatchNextHudOrder();
        PlayUiSound(uiSelectClip, 0.7f);
    }

    private string GetTradePolicyStatusText(TradeResourceType resourceType)
    {
        TradePolicyMode mode = GetTradePolicyMode(resourceType);
        int target = GetTradePolicyTarget(resourceType);
        int amount = GetWarehouseTradeResourceAmount(resourceType);
        bool ru = IsRussianLanguage();

        if (mode == TradePolicyMode.None)
        {
            return ru ? "не торгуется" : "no trade";
        }

        if (!IsTradePolicyModeSupported(resourceType, mode))
        {
            return ru ? "нет доступного направления" : "no available direction";
        }

        TradeOrderType orderType = mode == TradePolicyMode.SellAbove ? TradeOrderType.Sell : TradeOrderType.Buy;
        if (!HasBuiltTradeRouteForOrder(resourceType, orderType))
        {
            return GetTradeRouteMissingLabel(resourceType, orderType);
        }

        if (mode == TradePolicyMode.SellAbove)
        {
            int excess = amount - target;
            return excess > 0
                ? (ru ? $"продать избыток: {Mathf.Min(5, excess)}" : $"sell surplus: {Mathf.Min(5, excess)}")
                : (ru ? "избытка нет" : "no surplus");
        }

        int deficit = target - amount;
        return deficit > 0
            ? (ru ? $"докупить: {Mathf.Min(5, deficit)}" : $"buy missing: {Mathf.Min(5, deficit)}")
            : (ru ? "норма достигнута" : "target reached");
    }
}
