public readonly struct LocalBusNextStopDecision
{
    public readonly bool HasNextStop;
    public readonly int NextStopIndex;
    public readonly int TravelDirection;

    public LocalBusNextStopDecision(bool hasNextStop, int nextStopIndex, int travelDirection)
    {
        HasNextStop = hasNextStop;
        NextStopIndex = nextStopIndex;
        TravelDirection = travelDirection;
    }
}

public static class LocalBusRoutePlanner
{
    public static bool ShouldReturnToParkingAfterCurrentStop(int stopCount, int currentStopIndex, int travelDirection)
    {
        if (stopCount <= 1)
        {
            return true;
        }

        int clampedIndex = ClampIndex(currentStopIndex, stopCount);
        return clampedIndex == 0 && travelDirection < 0;
    }

    public static LocalBusNextStopDecision GetNextStop(int stopCount, int currentStopIndex, int travelDirection)
    {
        if (stopCount <= 1)
        {
            return new LocalBusNextStopDecision(false, 0, travelDirection >= 0 ? 1 : -1);
        }

        int clampedIndex = ClampIndex(currentStopIndex, stopCount);
        int nextDirection = travelDirection >= 0 ? 1 : -1;
        int nextIndex;

        if (nextDirection > 0)
        {
            if (clampedIndex >= stopCount - 1)
            {
                nextDirection = -1;
                nextIndex = stopCount - 2;
            }
            else
            {
                nextIndex = clampedIndex + 1;
            }
        }
        else if (clampedIndex <= 0)
        {
            nextDirection = 1;
            nextIndex = 1;
        }
        else
        {
            nextIndex = clampedIndex - 1;
        }

        return new LocalBusNextStopDecision(true, nextIndex, nextDirection);
    }

    private static int ClampIndex(int index, int stopCount)
    {
        if (stopCount <= 0)
        {
            return 0;
        }

        if (index < 0)
        {
            return 0;
        }

        if (index >= stopCount)
        {
            return stopCount - 1;
        }

        return index;
    }
}
