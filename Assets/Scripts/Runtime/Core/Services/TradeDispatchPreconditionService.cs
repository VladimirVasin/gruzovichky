public readonly struct TradeDispatchPreconditionInput
{
    public readonly bool HasActiveRun;
    public readonly bool HasDriver;
    public readonly bool DriverArriving;
    public readonly bool DriverBusy;
    public readonly bool HasTruck;
    public readonly bool TruckBusy;
    public readonly bool TruckHasOtherDriver;
    public readonly bool HighwayConnected;
    public readonly bool TruckParkedOrDriverOnboard;
    public readonly bool IsBuyOrder;
    public readonly int Treasury;
    public readonly int Price;
    public readonly int StoredResourceAmount;
    public readonly int Quantity;
    public readonly string TruckDisplayName;
    public readonly string ResourceLabel;

    public TradeDispatchPreconditionInput(
        bool hasActiveRun,
        bool hasDriver,
        bool driverArriving,
        bool driverBusy,
        bool hasTruck,
        bool truckBusy,
        bool truckHasOtherDriver,
        bool highwayConnected,
        bool truckParkedOrDriverOnboard,
        bool isBuyOrder,
        int treasury,
        int price,
        int storedResourceAmount,
        int quantity,
        string truckDisplayName,
        string resourceLabel)
    {
        HasActiveRun = hasActiveRun;
        HasDriver = hasDriver;
        DriverArriving = driverArriving;
        DriverBusy = driverBusy;
        HasTruck = hasTruck;
        TruckBusy = truckBusy;
        TruckHasOtherDriver = truckHasOtherDriver;
        HighwayConnected = highwayConnected;
        TruckParkedOrDriverOnboard = truckParkedOrDriverOnboard;
        IsBuyOrder = isBuyOrder;
        Treasury = treasury;
        Price = price;
        StoredResourceAmount = storedResourceAmount;
        Quantity = quantity;
        TruckDisplayName = string.IsNullOrEmpty(truckDisplayName) ? "Truck" : truckDisplayName;
        ResourceLabel = string.IsNullOrEmpty(resourceLabel) ? "resource" : resourceLabel;
    }
}

public readonly struct TradeDispatchPreconditionResult
{
    public readonly bool CanDispatch;
    public readonly string BlockReason;

    public TradeDispatchPreconditionResult(bool canDispatch, string blockReason)
    {
        CanDispatch = canDispatch;
        BlockReason = blockReason;
    }
}

public static class TradeDispatchPreconditionService
{
    public static TradeDispatchPreconditionResult Evaluate(TradeDispatchPreconditionInput input)
    {
        if (input.HasActiveRun) return Block("Another trade run is already active");
        if (!input.HasDriver) return Block("No available Truck Driver on shift");
        if (input.DriverArriving) return Block("Trade driver is still arriving");
        if (input.DriverBusy) return Block("Trade driver is busy");
        if (!input.HasTruck) return Block("Trade needs an available parked truck");
        if (input.TruckBusy) return Block($"{input.TruckDisplayName} is busy");
        if (input.TruckHasOtherDriver) return Block($"{input.TruckDisplayName} is using another driver");
        if (!input.HighwayConnected) return Block("Highway access is not connected");
        if (!input.TruckParkedOrDriverOnboard) return Block($"{input.TruckDisplayName} must be parked first");
        if (!input.IsBuyOrder && input.StoredResourceAmount < input.Quantity) return Block($"Need {input.Quantity} {input.ResourceLabel} to sell");
        return new TradeDispatchPreconditionResult(true, string.Empty);
    }

    private static TradeDispatchPreconditionResult Block(string reason)
    {
        return new TradeDispatchPreconditionResult(false, reason);
    }
}
