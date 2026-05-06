public readonly struct DocksTradePolicyResourceState
{
    public readonly GameBootstrap.TradeResourceType ResourceType;
    public readonly GameBootstrap.TradePolicyMode Mode;
    public readonly int TargetAmount;
    public readonly bool HasRiverRoute;
    public readonly string MissingRiverRouteReason;
    public readonly int WarehouseAmount;
    public readonly int DockExportAmount;
    public readonly int DockImportAmount;

    public DocksTradePolicyResourceState(
        GameBootstrap.TradeResourceType resourceType,
        GameBootstrap.TradePolicyMode mode,
        int targetAmount,
        bool hasRiverRoute,
        string missingRiverRouteReason,
        int warehouseAmount,
        int dockExportAmount,
        int dockImportAmount)
    {
        ResourceType = resourceType;
        Mode = mode;
        TargetAmount = targetAmount;
        HasRiverRoute = hasRiverRoute;
        MissingRiverRouteReason = missingRiverRouteReason ?? string.Empty;
        WarehouseAmount = warehouseAmount;
        DockExportAmount = dockExportAmount;
        DockImportAmount = dockImportAmount;
    }
}

public readonly struct DocksTradePolicyDecision
{
    public readonly bool CanTrade;
    public readonly GameBootstrap.TradeResourceType ResourceType;
    public readonly int Quantity;
    public readonly string SkipReason;

    public DocksTradePolicyDecision(bool canTrade, GameBootstrap.TradeResourceType resourceType, int quantity, string skipReason)
    {
        CanTrade = canTrade;
        ResourceType = resourceType;
        Quantity = quantity;
        SkipReason = skipReason ?? string.Empty;
    }
}

public static class DocksTradePolicyRuntime
{
    public static DocksTradePolicyDecision FindTrade(
        GameBootstrap.TradeOrderType orderType,
        DocksTradePolicyResourceState[] resources,
        int batchSize,
        int storageCapacity)
    {
        if (resources == null)
        {
            return NoTrade(orderType);
        }

        GameBootstrap.TradePolicyMode requiredMode = GetRequiredMode(orderType);
        for (int i = 0; i < resources.Length; i++)
        {
            DocksTradePolicyResourceState resource = resources[i];
            if (resource.Mode != requiredMode || !resource.HasRiverRoute)
            {
                continue;
            }

            int quantity = GetTradeQuantity(orderType, resource, batchSize, storageCapacity);
            if (quantity > 0)
            {
                return new DocksTradePolicyDecision(true, resource.ResourceType, quantity, string.Empty);
            }
        }

        return NoTrade(orderType, GetSkipReason(orderType, resources, storageCapacity));
    }

    public static string GetSkipReason(
        GameBootstrap.TradeOrderType orderType,
        DocksTradePolicyResourceState[] resources,
        int storageCapacity)
    {
        if (resources == null)
        {
            return GetDefaultSkipReason(orderType);
        }

        GameBootstrap.TradePolicyMode requiredMode = GetRequiredMode(orderType);
        for (int i = 0; i < resources.Length; i++)
        {
            DocksTradePolicyResourceState resource = resources[i];
            if (resource.Mode != requiredMode)
            {
                continue;
            }

            if (!resource.HasRiverRoute)
            {
                return $"{orderType} {resource.ResourceType}: {resource.MissingRiverRouteReason}";
            }

            if (orderType == GameBootstrap.TradeOrderType.Buy)
            {
                int stock = resource.WarehouseAmount + resource.DockImportAmount;
                int room = storageCapacity - resource.DockImportAmount;
                if (stock >= resource.TargetAmount)
                {
                    return $"{resource.ResourceType} stock {stock} already meets target {resource.TargetAmount}";
                }

                if (room <= 0)
                {
                    return $"{resource.ResourceType} import storage full";
                }
            }
            else
            {
                int totalStock = resource.WarehouseAmount + resource.DockExportAmount;
                if (resource.DockExportAmount <= 0)
                {
                    return $"{resource.ResourceType} has no export cargo at Docks";
                }

                if (totalStock <= resource.TargetAmount)
                {
                    return $"{resource.ResourceType} total stock {totalStock} is not above target {resource.TargetAmount}";
                }
            }
        }

        return GetDefaultSkipReason(orderType);
    }

    private static GameBootstrap.TradePolicyMode GetRequiredMode(GameBootstrap.TradeOrderType orderType)
    {
        return orderType == GameBootstrap.TradeOrderType.Buy
            ? GameBootstrap.TradePolicyMode.BuyUpTo
            : GameBootstrap.TradePolicyMode.SellAbove;
    }

    private static int GetTradeQuantity(
        GameBootstrap.TradeOrderType orderType,
        DocksTradePolicyResourceState resource,
        int batchSize,
        int storageCapacity)
    {
        int batch = batchSize <= 0 ? 1 : batchSize;
        if (orderType == GameBootstrap.TradeOrderType.Buy)
        {
            int stock = resource.WarehouseAmount + resource.DockImportAmount;
            int room = storageCapacity - resource.DockImportAmount;
            return Min3(batch, Max0(resource.TargetAmount - stock), Max0(room));
        }

        int totalStock = resource.WarehouseAmount + resource.DockExportAmount;
        return Min3(batch, resource.DockExportAmount, Max0(totalStock - resource.TargetAmount));
    }

    private static DocksTradePolicyDecision NoTrade(GameBootstrap.TradeOrderType orderType, string skipReason = "")
    {
        return new DocksTradePolicyDecision(false, GameBootstrap.TradeResourceType.Logs, 0, string.IsNullOrEmpty(skipReason) ? GetDefaultSkipReason(orderType) : skipReason);
    }

    private static string GetDefaultSkipReason(GameBootstrap.TradeOrderType orderType)
    {
        return orderType == GameBootstrap.TradeOrderType.Buy
            ? "no Buy up to policy with an eligible river route"
            : "no Sell surplus policy with an eligible river route";
    }

    private static int Max0(int value)
    {
        return value < 0 ? 0 : value;
    }

    private static int Min3(int a, int b, int c)
    {
        int min = a < b ? a : b;
        return min < c ? min : c;
    }
}
