using System.Collections.Generic;

public static class ServiceSlotCoordinator
{
    public static bool TryAcquire<TSlot>(HashSet<TSlot> occupiedServiceLocations, TSlot locationType)
    {
        if (occupiedServiceLocations.Count > 0)
        {
            return false;
        }

        occupiedServiceLocations.Add(locationType);
        return true;
    }

    public static void Release<TSlot>(HashSet<TSlot> occupiedServiceLocations, TSlot locationType)
    {
        occupiedServiceLocations.Remove(locationType);
    }
}
