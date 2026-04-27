public static class TruckRuntimeGuardService
{
    public static bool CanUpdateAssignedTrip(
        bool hasAssignedTrip,
        bool hasActiveRefuelOrder,
        bool driverIsResting,
        bool driverRescueActive,
        bool truckMoving,
        bool truckInteracting)
    {
        return hasAssignedTrip &&
               !hasActiveRefuelOrder &&
               !driverIsResting &&
               !driverRescueActive &&
               !truckMoving &&
               !truckInteracting;
    }

    public static bool CanUpdateRefuelOrder(
        bool hasActiveRefuelOrder,
        bool hasAssignedTrip,
        bool driverIsResting,
        bool driverRescueActive,
        bool truckMoving,
        bool truckInteracting)
    {
        return hasActiveRefuelOrder &&
               !hasAssignedTrip &&
               !driverIsResting &&
               !driverRescueActive &&
               !truckMoving &&
               !truckInteracting;
    }
}
