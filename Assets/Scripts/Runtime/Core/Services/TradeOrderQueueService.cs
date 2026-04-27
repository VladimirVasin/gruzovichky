using System.Collections.Generic;

public static class TradeOrderQueueService
{
    public static GameBootstrap.TradeHudOrder CreateOrder(
        int id,
        GameBootstrap.TradeResourceType resourceType,
        GameBootstrap.TradeOrderType orderType,
        int amount,
        int targetRegionIndex = -1)
    {
        return new GameBootstrap.TradeHudOrder
        {
            Id = id,
            ResourceType = resourceType,
            OrderType = orderType,
            Amount = amount,
            TargetRegionIndex = targetRegionIndex
        };
    }

    public static bool TryPeek(IReadOnlyList<GameBootstrap.TradeHudOrder> orders, out GameBootstrap.TradeHudOrder order)
    {
        if (orders != null && orders.Count > 0)
        {
            order = orders[0];
            return true;
        }

        order = null;
        return false;
    }

    public static bool RemoveFirst(IList<GameBootstrap.TradeHudOrder> orders)
    {
        if (orders == null || orders.Count == 0)
        {
            return false;
        }

        orders.RemoveAt(0);
        return true;
    }

    public static int RemoveById(IList<GameBootstrap.TradeHudOrder> orders, int orderId)
    {
        if (orders == null)
        {
            return 0;
        }

        int removed = 0;
        for (int i = orders.Count - 1; i >= 0; i--)
        {
            if (orders[i].Id == orderId)
            {
                orders.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }
}
