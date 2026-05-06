public struct TradeResourceStock
{
    public int Logs;
    public int Boards;
    public int Cotton;
    public int Textile;
    public int Furniture;

    public TradeResourceStock(int logs, int boards, int cotton, int textile, int furniture)
    {
        Logs = logs;
        Boards = boards;
        Cotton = cotton;
        Textile = textile;
        Furniture = furniture;
    }

    public int Get(GameBootstrap.TradeResourceType resourceType)
    {
        return resourceType switch
        {
            GameBootstrap.TradeResourceType.Logs => Logs,
            GameBootstrap.TradeResourceType.Boards => Boards,
            GameBootstrap.TradeResourceType.Cotton => Cotton,
            GameBootstrap.TradeResourceType.Textile => Textile,
            GameBootstrap.TradeResourceType.Furniture => Furniture,
            _ => 0
        };
    }

    public void Set(GameBootstrap.TradeResourceType resourceType, int amount)
    {
        int clamped = amount < 0 ? 0 : amount;
        switch (resourceType)
        {
            case GameBootstrap.TradeResourceType.Logs:
                Logs = clamped;
                break;
            case GameBootstrap.TradeResourceType.Boards:
                Boards = clamped;
                break;
            case GameBootstrap.TradeResourceType.Cotton:
                Cotton = clamped;
                break;
            case GameBootstrap.TradeResourceType.Textile:
                Textile = clamped;
                break;
            case GameBootstrap.TradeResourceType.Furniture:
                Furniture = clamped;
                break;
        }
    }
}

public static class TradeResourceLedger
{
    public static int GetAmount(TradeResourceStock stock, GameBootstrap.TradeResourceType resourceType)
    {
        return stock.Get(resourceType);
    }

    public static int Add(ref TradeResourceStock stock, GameBootstrap.TradeResourceType resourceType, int amount, int capacity = int.MaxValue)
    {
        if (amount <= 0 || !IsSupported(resourceType))
        {
            return 0;
        }

        int before = stock.Get(resourceType);
        int capped = ClampInt(before + amount, 0, capacity < 0 ? 0 : capacity);
        stock.Set(resourceType, capped);
        return capped - before;
    }

    public static bool TryConsume(ref TradeResourceStock stock, GameBootstrap.TradeResourceType resourceType, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (!IsSupported(resourceType))
        {
            return false;
        }

        int before = stock.Get(resourceType);
        if (before < amount)
        {
            return false;
        }

        stock.Set(resourceType, before - amount);
        return true;
    }

    public static int GetRoom(TradeResourceStock stock, GameBootstrap.TradeResourceType resourceType, int capacity)
    {
        if (!IsSupported(resourceType))
        {
            return 0;
        }

        return ClampInt(capacity - stock.Get(resourceType), 0, capacity < 0 ? 0 : capacity);
    }

    private static bool IsSupported(GameBootstrap.TradeResourceType resourceType)
    {
        return resourceType == GameBootstrap.TradeResourceType.Logs ||
               resourceType == GameBootstrap.TradeResourceType.Boards ||
               resourceType == GameBootstrap.TradeResourceType.Cotton ||
               resourceType == GameBootstrap.TradeResourceType.Textile ||
               resourceType == GameBootstrap.TradeResourceType.Furniture;
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
