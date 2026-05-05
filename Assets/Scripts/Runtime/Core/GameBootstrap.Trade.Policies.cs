using UnityEngine;

public partial class GameBootstrap
{
    private int GetStoredTradeResourceAmount(TradeResourceType resourceType)
    {
        return GetWarehouseTradeResourceAmount(resourceType);
    }

    private int GetWarehouseTradeResourceAmount(TradeResourceType resourceType)
    {
        return resourceType switch
        {
            TradeResourceType.Logs => locations.TryGetValue(LocationType.Warehouse, out LocationData logsWarehouse) ? logsWarehouse.LogsStored : 0,
            TradeResourceType.Boards => locations.TryGetValue(LocationType.Warehouse, out LocationData boardsWarehouse) ? boardsWarehouse.BoardsStored : 0,
            TradeResourceType.Cotton => cottonStored,
            TradeResourceType.Textile => textileStored,
            TradeResourceType.Furniture => furnitureStored,
            _ => 0
        };
    }

    private int GetTradePolicyIndex(TradeResourceType resourceType)
    {
        for (int i = 0; i < TradeHudResources.Length; i++)
        {
            if (TradeHudResources[i] == resourceType)
            {
                return i;
            }
        }

        return -1;
    }

    private TradePolicyMode GetTradePolicyMode(TradeResourceType resourceType)
    {
        int index = GetTradePolicyIndex(resourceType);
        return index >= 0 ? tradePolicyModes[index] : TradePolicyMode.None;
    }

    private int GetTradePolicyTarget(TradeResourceType resourceType)
    {
        int index = GetTradePolicyIndex(resourceType);
        return index >= 0 ? Mathf.Max(0, tradePolicyTargets[index]) : 0;
    }

    private string GetTradePolicyModeLabel(TradePolicyMode mode)
    {
        bool ru = IsRussianLanguage();
        return mode switch
        {
            TradePolicyMode.SellAbove => ru ? "Продавать избыток" : "Sell surplus",
            TradePolicyMode.BuyUpTo => ru ? "Докупить до нормы" : "Buy up to",
            _ => ru ? "Нет торговли" : "No trade"
        };
    }

    private bool IsTradePolicyModeSupported(TradeResourceType resourceType, TradePolicyMode mode)
    {
        if (mode == TradePolicyMode.None)
        {
            return true;
        }

        TradeResourceType[] catalog = mode == TradePolicyMode.BuyUpTo ? TradeImportCatalog : TradeExportCatalog;
        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] == resourceType)
            {
                return true;
            }
        }

        return false;
    }

    private int CountActiveTradePolicies()
    {
        int count = 0;
        for (int i = 0; i < tradePolicyModes.Length; i++)
        {
            if (tradePolicyModes[i] != TradePolicyMode.None)
            {
                count++;
            }
        }

        return count;
    }

    private int CountDispatchableTradePolicies()
    {
        int count = 0;
        for (int i = 0; i < TradeHudResources.Length; i++)
        {
            TradeResourceType resourceType = TradeHudResources[i];
            TradePolicyMode mode = GetTradePolicyMode(resourceType);
            if (mode == TradePolicyMode.None || !IsTradePolicyModeSupported(resourceType, mode))
            {
                continue;
            }

            int amount = GetWarehouseTradeResourceAmount(resourceType);
            int target = GetTradePolicyTarget(resourceType);
            if ((mode == TradePolicyMode.SellAbove && amount > target) ||
                (mode == TradePolicyMode.BuyUpTo && amount < target))
            {
                count++;
            }
        }

        return count;
    }

    private bool TryBuildTradePolicyDispatchRequest(out TradeHudOrder request)
    {
        request = null;
        for (int i = 0; i < TradeHudResources.Length; i++)
        {
            TradeResourceType resourceType = TradeHudResources[i];
            TradePolicyMode mode = GetTradePolicyMode(resourceType);
            if (mode == TradePolicyMode.None || !IsTradePolicyModeSupported(resourceType, mode))
            {
                continue;
            }

            int amount = GetWarehouseTradeResourceAmount(resourceType);
            int target = GetTradePolicyTarget(resourceType);
            int delta = mode == TradePolicyMode.SellAbove ? amount - target : target - amount;
            if (delta <= 0)
            {
                continue;
            }

            TradeOrderType orderType = mode == TradePolicyMode.SellAbove ? TradeOrderType.Sell : TradeOrderType.Buy;
            request = TradeOrderQueueService.CreateOrder(0, resourceType, orderType, Mathf.Clamp(delta, 1, 5));
            return true;
        }

        return false;
    }
}
