public enum TradePolicyDispatchDecisionKind
{
    None,
    Dispatch,
    RiverRoute,
    MissingRoute
}

public readonly struct TradePolicyDispatchDecision
{
    public readonly TradePolicyDispatchDecisionKind Kind;
    public readonly GameBootstrap.TradeResourceType ResourceType;
    public readonly GameBootstrap.TradeOrderType OrderType;
    public readonly int Amount;
    public readonly int TargetRegionIndex;

    public TradePolicyDispatchDecision(
        TradePolicyDispatchDecisionKind kind,
        GameBootstrap.TradeResourceType resourceType,
        GameBootstrap.TradeOrderType orderType,
        int amount,
        int targetRegionIndex)
    {
        Kind = kind;
        ResourceType = resourceType;
        OrderType = orderType;
        Amount = amount;
        TargetRegionIndex = targetRegionIndex;
    }

    public bool ShouldDispatch => Kind == TradePolicyDispatchDecisionKind.Dispatch;
}

public static class TradePolicyRuntime
{
    public static int GetPolicyIndex(GameBootstrap.TradeResourceType resourceType, GameBootstrap.TradeResourceType[] resources)
    {
        if (resources == null)
        {
            return -1;
        }

        for (int i = 0; i < resources.Length; i++)
        {
            if (resources[i] == resourceType)
            {
                return i;
            }
        }

        return -1;
    }

    public static bool IsModeSupported(
        GameBootstrap.TradeResourceType resourceType,
        GameBootstrap.TradePolicyMode mode,
        GameBootstrap.TradeResourceType[] importCatalog,
        GameBootstrap.TradeResourceType[] exportCatalog)
    {
        if (mode == GameBootstrap.TradePolicyMode.None)
        {
            return true;
        }

        GameBootstrap.TradeResourceType[] catalog = mode == GameBootstrap.TradePolicyMode.BuyUpTo
            ? importCatalog
            : exportCatalog;
        return GetPolicyIndex(resourceType, catalog) >= 0;
    }

    public static int CountActivePolicies(GameBootstrap.TradePolicyMode[] policyModes)
    {
        if (policyModes == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < policyModes.Length; i++)
        {
            if (policyModes[i] != GameBootstrap.TradePolicyMode.None)
            {
                count++;
            }
        }

        return count;
    }

    public static TradePolicyDispatchDecision EvaluateDispatch(
        GameBootstrap.TradeResourceType resourceType,
        GameBootstrap.TradePolicyMode mode,
        int storedAmount,
        int targetAmount,
        bool modeSupported,
        bool hasLandRoute,
        bool hasRiverRoute,
        int targetRegionIndex,
        int cargoCapacity)
    {
        if (mode == GameBootstrap.TradePolicyMode.None || !modeSupported)
        {
            return NoDispatch(resourceType);
        }

        GameBootstrap.TradeOrderType orderType = mode == GameBootstrap.TradePolicyMode.SellAbove
            ? GameBootstrap.TradeOrderType.Sell
            : GameBootstrap.TradeOrderType.Buy;
        int delta = mode == GameBootstrap.TradePolicyMode.SellAbove
            ? storedAmount - targetAmount
            : targetAmount - storedAmount;
        if (delta <= 0)
        {
            return NoDispatch(resourceType, orderType);
        }

        if (!hasLandRoute)
        {
            return new TradePolicyDispatchDecision(
                hasRiverRoute ? TradePolicyDispatchDecisionKind.RiverRoute : TradePolicyDispatchDecisionKind.MissingRoute,
                resourceType,
                orderType,
                0,
                -1);
        }

        int amount = ClampInt(delta, 1, cargoCapacity <= 0 ? 1 : cargoCapacity);
        return new TradePolicyDispatchDecision(
            TradePolicyDispatchDecisionKind.Dispatch,
            resourceType,
            orderType,
            amount,
            targetRegionIndex);
    }

    private static TradePolicyDispatchDecision NoDispatch(
        GameBootstrap.TradeResourceType resourceType,
        GameBootstrap.TradeOrderType orderType = GameBootstrap.TradeOrderType.Buy)
    {
        return new TradePolicyDispatchDecision(TradePolicyDispatchDecisionKind.None, resourceType, orderType, 0, -1);
    }

    private static int ClampInt(int value, int min, int max)
    {
        if (value < min)
        {
            return min;
        }

        return value > max ? max : value;
    }
}
