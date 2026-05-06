public readonly struct TradePolicyModeButtonModel
{
    public readonly GameBootstrap.TradePolicyMode Mode;
    public readonly string Label;
    public readonly bool Supported;
    public readonly bool Selected;

    public TradePolicyModeButtonModel(GameBootstrap.TradePolicyMode mode, string label, bool supported, bool selected)
    {
        Mode = mode;
        Label = label ?? string.Empty;
        Supported = supported;
        Selected = selected;
    }
}

public readonly struct TradePolicyRowModel
{
    public readonly GameBootstrap.TradeResourceType ResourceType;
    public readonly string ResourceLabel;
    public readonly string WarehouseAmountText;
    public readonly string TargetText;
    public readonly string StatusText;
    public readonly bool CanDecreaseTarget;
    public readonly bool CanIncreaseTarget;
    public readonly TradePolicyModeButtonModel NoTradeButton;
    public readonly TradePolicyModeButtonModel SellAboveButton;
    public readonly TradePolicyModeButtonModel BuyUpToButton;

    public TradePolicyRowModel(
        GameBootstrap.TradeResourceType resourceType,
        string resourceLabel,
        string warehouseAmountText,
        string targetText,
        string statusText,
        bool canDecreaseTarget,
        bool canIncreaseTarget,
        TradePolicyModeButtonModel noTradeButton,
        TradePolicyModeButtonModel sellAboveButton,
        TradePolicyModeButtonModel buyUpToButton)
    {
        ResourceType = resourceType;
        ResourceLabel = resourceLabel ?? string.Empty;
        WarehouseAmountText = warehouseAmountText ?? string.Empty;
        TargetText = targetText ?? string.Empty;
        StatusText = statusText ?? string.Empty;
        CanDecreaseTarget = canDecreaseTarget;
        CanIncreaseTarget = canIncreaseTarget;
        NoTradeButton = noTradeButton;
        SellAboveButton = sellAboveButton;
        BuyUpToButton = buyUpToButton;
    }

    public TradePolicyModeButtonModel GetButton(GameBootstrap.TradePolicyMode mode)
    {
        return mode switch
        {
            GameBootstrap.TradePolicyMode.SellAbove => SellAboveButton,
            GameBootstrap.TradePolicyMode.BuyUpTo => BuyUpToButton,
            _ => NoTradeButton
        };
    }
}

public static class TradeScreenModel
{
    public static TradePolicyRowModel CreatePolicyRow(
        GameBootstrap.TradeResourceType resourceType,
        string resourceLabel,
        int warehouseAmount,
        GameBootstrap.TradePolicyMode selectedMode,
        int target,
        string statusText,
        string noTradeLabel,
        string sellAboveLabel,
        string buyUpToLabel,
        bool noTradeSupported,
        bool sellAboveSupported,
        bool buyUpToSupported)
    {
        return new TradePolicyRowModel(
            resourceType,
            resourceLabel,
            warehouseAmount.ToString(),
            target.ToString(),
            statusText,
            target > 0,
            target < 99,
            new TradePolicyModeButtonModel(GameBootstrap.TradePolicyMode.None, noTradeLabel, noTradeSupported, selectedMode == GameBootstrap.TradePolicyMode.None),
            new TradePolicyModeButtonModel(GameBootstrap.TradePolicyMode.SellAbove, sellAboveLabel, sellAboveSupported, selectedMode == GameBootstrap.TradePolicyMode.SellAbove),
            new TradePolicyModeButtonModel(GameBootstrap.TradePolicyMode.BuyUpTo, buyUpToLabel, buyUpToSupported, selectedMode == GameBootstrap.TradePolicyMode.BuyUpTo));
    }
}
