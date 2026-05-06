using UnityEngine;

public partial class GameBootstrap
{
    private int GetStoredTradeResourceAmount(TradeResourceType resourceType)
    {
        return GetWarehouseTradeResourceAmount(resourceType);
    }

    private int GetWarehouseTradeResourceAmount(TradeResourceType resourceType)
    {
        locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse);
        return tradeState.GetStoredResourceAmount(
            resourceType,
            warehouse?.LogsStored ?? 0,
            warehouse?.BoardsStored ?? 0);
    }

    private int GetTradePolicyIndex(TradeResourceType resourceType)
    {
        return tradeState.GetPolicyIndex(resourceType, TradeHudResources);
    }

    private TradePolicyMode GetTradePolicyMode(TradeResourceType resourceType)
    {
        return tradeState.GetPolicyMode(resourceType, TradeHudResources);
    }

    private int GetTradePolicyTarget(TradeResourceType resourceType)
    {
        return tradeState.GetPolicyTarget(resourceType, TradeHudResources);
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

        return TradePolicyRuntime.IsModeSupported(resourceType, mode, TradeImportCatalog, TradeExportCatalog);
    }

    private bool HasBuiltTradeRouteForOrder(TradeResourceType resourceType, TradeOrderType orderType)
    {
        return HasBuiltRegionalTradeRoute(resourceType, orderType);
    }

    private string GetTradeRouteMissingLabel(TradeResourceType resourceType, TradeOrderType orderType)
    {
        bool ru = IsRussianLanguage();
        string action = orderType == TradeOrderType.Buy
            ? (ru ? "покупки" : "buying")
            : (ru ? "продажи" : "selling");
        return ru
            ? $"нет торгового маршрута для {action} {GetTradeResourceDisplayLabel(resourceType)}"
            : $"no trade route for {action} {GetTradeResourceShortLabel(resourceType)}";
    }

    private int CountActiveTradePolicies()
    {
        return tradeState.CountActivePolicies();
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

            TradeOrderType orderType = mode == TradePolicyMode.SellAbove ? TradeOrderType.Sell : TradeOrderType.Buy;
            TradePolicyDispatchDecision decision = TradePolicyRuntime.EvaluateDispatch(
                resourceType,
                mode,
                GetWarehouseTradeResourceAmount(resourceType),
                GetTradePolicyTarget(resourceType),
                IsTradePolicyModeSupported(resourceType, mode),
                HasBuiltRegionalTradeRoute(resourceType, orderType, RegionalTradeRouteMode.Land),
                HasBuiltRegionalTradeRoute(resourceType, orderType, RegionalTradeRouteMode.River),
                -1,
                TruckCargoCapacity);
            if (decision.ShouldDispatch)
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

            TradeOrderType orderType = mode == TradePolicyMode.SellAbove ? TradeOrderType.Sell : TradeOrderType.Buy;
            bool hasLandRoute = TryFindBuiltRegionalTradeRoute(resourceType, orderType, RegionalTradeRouteMode.Land, out RegionalCityData city);
            bool hasRiverRoute = HasBuiltRegionalTradeRoute(resourceType, orderType, RegionalTradeRouteMode.River);
            TradePolicyDispatchDecision decision = TradePolicyRuntime.EvaluateDispatch(
                resourceType,
                mode,
                GetWarehouseTradeResourceAmount(resourceType),
                GetTradePolicyTarget(resourceType),
                IsTradePolicyModeSupported(resourceType, mode),
                hasLandRoute,
                hasRiverRoute,
                city?.RegionIndex ?? -1,
                TruckCargoCapacity);
            if (decision.Kind == TradePolicyDispatchDecisionKind.None)
            {
                continue;
            }

            if (decision.Kind == TradePolicyDispatchDecisionKind.RiverRoute)
            {
                SessionDebugLogger.LogVerbose("TRADE_AUTO", $"Policy skipped for land dispatch: {orderType} {resourceType} uses a built river route and will be handled by Docks.");
                continue;
            }

            if (decision.Kind == TradePolicyDispatchDecisionKind.MissingRoute)
            {
                SessionDebugLogger.Log(
                    "TRADE_AUTO",
                    $"Policy skipped for land dispatch: {orderType} {resourceType}: {DescribeRegionalTradeRouteAvailability(resourceType, orderType, RegionalTradeRouteMode.Land)}.");
                continue;
            }

            request = TradeOrderQueueService.CreateOrder(
                0,
                decision.ResourceType,
                decision.OrderType,
                decision.Amount,
                decision.TargetRegionIndex);
            return true;
        }

        return false;
    }
}
