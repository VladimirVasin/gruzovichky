using System.Collections.Generic;

public sealed class TradeState
{
    public GameBootstrap.TradeResourceType SelectedResourceType = GameBootstrap.TradeResourceType.Cotton;
    public GameBootstrap.TradeOrderType SelectedOrderType = GameBootstrap.TradeOrderType.Buy;
    public int SelectedOrderAmount = 5;
    public int NextOrderId = 1;

    public int CottonStored;
    public int TextileStored;
    public int FurnitureStored;

    public readonly List<GameBootstrap.TradeHudOrder> ActiveHudOrders = new();
    public GameBootstrap.TradePolicyMode[] PolicyModes { get; } = new GameBootstrap.TradePolicyMode[5];
    public int[] PolicyTargets { get; } = { 0, 0, 0, 0, 0 };

    public int GetStoredResourceAmount(GameBootstrap.TradeResourceType resourceType, int warehouseLogs, int warehouseBoards)
    {
        return TradeResourceLedger.GetAmount(GetStoredResourceStock(warehouseLogs, warehouseBoards), resourceType);
    }

    public TradeResourceStock GetStoredResourceStock(int warehouseLogs, int warehouseBoards)
    {
        return new TradeResourceStock(warehouseLogs, warehouseBoards, CottonStored, TextileStored, FurnitureStored);
    }

    public int GetPolicyIndex(GameBootstrap.TradeResourceType resourceType, GameBootstrap.TradeResourceType[] resources)
    {
        return TradePolicyRuntime.GetPolicyIndex(resourceType, resources);
    }

    public GameBootstrap.TradePolicyMode GetPolicyMode(GameBootstrap.TradeResourceType resourceType, GameBootstrap.TradeResourceType[] resources)
    {
        int index = GetPolicyIndex(resourceType, resources);
        return index >= 0 ? PolicyModes[index] : GameBootstrap.TradePolicyMode.None;
    }

    public int GetPolicyTarget(GameBootstrap.TradeResourceType resourceType, GameBootstrap.TradeResourceType[] resources)
    {
        int index = GetPolicyIndex(resourceType, resources);
        return index >= 0 ? ClampInt(PolicyTargets[index], 0, int.MaxValue) : 0;
    }

    public bool TrySetPolicyMode(GameBootstrap.TradeResourceType resourceType, GameBootstrap.TradePolicyMode mode, GameBootstrap.TradeResourceType[] resources)
    {
        int index = GetPolicyIndex(resourceType, resources);
        if (index < 0)
        {
            return false;
        }

        PolicyModes[index] = mode;
        return true;
    }

    public int AdjustPolicyTarget(GameBootstrap.TradeResourceType resourceType, int delta, int minTarget, int maxTarget, GameBootstrap.TradeResourceType[] resources)
    {
        int index = GetPolicyIndex(resourceType, resources);
        if (index < 0)
        {
            return 0;
        }

        PolicyTargets[index] = ClampInt(GetPolicyTarget(resourceType, resources) + delta, minTarget, maxTarget);
        return PolicyTargets[index];
    }

    public int CountActivePolicies()
    {
        return TradePolicyRuntime.CountActivePolicies(PolicyModes);
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
