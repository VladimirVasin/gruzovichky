public readonly struct TradeAutoDispatchTick
{
    public readonly float RetryTimer;
    public readonly bool ShouldDispatch;

    public TradeAutoDispatchTick(float retryTimer, bool shouldDispatch)
    {
        RetryTimer = retryTimer;
        ShouldDispatch = shouldDispatch;
    }
}

public static class TradeAutoDispatchService
{
    public static TradeAutoDispatchTick Tick(
        bool isWeekend,
        bool hasActiveRun,
        int activeOrderCount,
        float retryTimer,
        float deltaTime,
        float retryInterval)
    {
        if (isWeekend || hasActiveRun || activeOrderCount <= 0)
        {
            return new TradeAutoDispatchTick(0f, false);
        }

        float nextRetryTimer = retryTimer + deltaTime;
        if (nextRetryTimer < retryInterval)
        {
            return new TradeAutoDispatchTick(nextRetryTimer, false);
        }

        return new TradeAutoDispatchTick(0f, true);
    }
}
