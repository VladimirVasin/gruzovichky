using UnityEngine;

public enum TruckAutoDecisionKind
{
    None,
    Refuel,
    Trip
}

public static class TruckAutoPlanner
{
    public static TruckAutoDecisionKind Decide(bool autoModeEnabled, bool hasTrip, bool hasRefuel, bool moving, bool interacting, bool driverRescueActive, bool insideParking, float truckFuel, int availableTripCount)
    {
        if (!autoModeEnabled || hasTrip || hasRefuel || moving || interacting || driverRescueActive || !insideParking)
        {
            return TruckAutoDecisionKind.None;
        }

        if (truckFuel < 30f)
        {
            return TruckAutoDecisionKind.Refuel;
        }

        return availableTripCount > 0 ? TruckAutoDecisionKind.Trip : TruckAutoDecisionKind.None;
    }

    public static int PickTripIndex(int tripCount)
    {
        return tripCount <= 0 ? -1 : Random.Range(0, tripCount);
    }
}

