public readonly struct TradeSimulationTickInput
{
    public readonly bool IsWeekend;
    public readonly bool HasActiveTradeRuntime;
    public readonly int DispatchablePolicyCount;
    public readonly float AutoDispatchRetryTimer;
    public readonly float DeltaTime;
    public readonly float RetryInterval;

    public TradeSimulationTickInput(
        bool isWeekend,
        bool hasActiveTradeRuntime,
        int dispatchablePolicyCount,
        float autoDispatchRetryTimer,
        float deltaTime,
        float retryInterval)
    {
        IsWeekend = isWeekend;
        HasActiveTradeRuntime = hasActiveTradeRuntime;
        DispatchablePolicyCount = dispatchablePolicyCount;
        AutoDispatchRetryTimer = autoDispatchRetryTimer;
        DeltaTime = deltaTime;
        RetryInterval = retryInterval;
    }
}

public readonly struct TradeSimulationTickResult
{
    public readonly float AutoDispatchRetryTimer;
    public readonly bool ShouldAutoDispatch;

    public TradeSimulationTickResult(float autoDispatchRetryTimer, bool shouldAutoDispatch)
    {
        AutoDispatchRetryTimer = autoDispatchRetryTimer;
        ShouldAutoDispatch = shouldAutoDispatch;
    }
}

public sealed class TradeSimulation
{
    public TradeSimulationTickResult Tick(TradeSimulationTickInput input)
    {
        TradeAutoDispatchTick autoDispatchTick = TradeAutoDispatchService.Tick(
            input.IsWeekend,
            input.HasActiveTradeRuntime,
            input.DispatchablePolicyCount,
            input.AutoDispatchRetryTimer,
            input.DeltaTime,
            input.RetryInterval);
        return new TradeSimulationTickResult(autoDispatchTick.RetryTimer, autoDispatchTick.ShouldDispatch);
    }
}
