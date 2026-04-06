using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void UpdateAssignedTrip()
    {
        if (currentAssignedTrip == TripType.None || currentRefuelPhase != RefuelPhase.None || isDriverRescueActive || isTruckMoving || isTruckInteracting)
        {
            return;
        }

        switch (currentTripPhase)
        {
            case TripPhase.ToPickup:
            {
                LocationType pickupLocation = GetPickupLocation(currentAssignedTrip);
                if (truckCell != locations[pickupLocation].Anchor)
                {
                    StartMoveTo(locations[pickupLocation].Anchor);
                    return;
                }

                if (TryStartTruckInteraction(GetLoadInteraction(currentAssignedTrip), pickupLocation))
                {
                    SessionDebugLogger.Log("TRIP", $"{GetLoadedTruckDisplayName()} started loading at {pickupLocation} for trip {GetTripTitle(currentAssignedTrip)}.");
                    currentTripPhase = TripPhase.Loading;
                }

                return;
            }

            case TripPhase.Loading:
                if (isTruckInteracting)
                {
                    return;
                }

                if (TryResumeQueuedTruckInteraction())
                {
                    return;
                }

                currentTripPhase = TripPhase.ToDropoff;
                return;

            case TripPhase.ToDropoff:
            {
                LocationType dropoffLocation = GetDropoffLocation(currentAssignedTrip);
                if (truckCell != locations[dropoffLocation].Anchor)
                {
                    StartMoveTo(locations[dropoffLocation].Anchor);
                    return;
                }

                if (TryStartTruckInteraction(GetUnloadInteraction(currentAssignedTrip), dropoffLocation))
                {
                    SessionDebugLogger.Log("TRIP", $"{GetLoadedTruckDisplayName()} started unloading at {dropoffLocation} for trip {GetTripTitle(currentAssignedTrip)}.");
                    currentTripPhase = TripPhase.Unloading;
                }

                return;
            }

            case TripPhase.Unloading:
                if (isTruckInteracting)
                {
                    return;
                }

                if (TryResumeQueuedTruckInteraction())
                {
                    return;
                }

                currentTripPhase = TripPhase.ReturnToParking;
                return;

            case TripPhase.ReturnToParking:
                if (truckCell != locations[LocationType.Parking].Anchor)
                {
                    StartMoveTo(locations[LocationType.Parking].Anchor);
                    return;
                }

                AwardMoney(currentAssignedTripReward);
                SessionDebugLogger.Log("TRIP", $"{GetLoadedTruckDisplayName()} completed trip {GetTripTitle(currentAssignedTrip)} and earned ${currentAssignedTripReward}.");
                currentAssignedTrip = TripType.None;
                currentTripPhase = TripPhase.None;
                currentAssignedTripReward = 0;
                return;
        }
    }

    private void UpdateRefuelOrder()
    {
        if (currentRefuelPhase == RefuelPhase.None || currentAssignedTrip != TripType.None || isDriverRescueActive || isTruckMoving || isTruckInteracting)
        {
            return;
        }

        switch (currentRefuelPhase)
        {
            case RefuelPhase.ToGasStation:
                if (truckCell != locations[LocationType.GasStation].Anchor)
                {
                    StartMoveTo(locations[LocationType.GasStation].Anchor);
                    return;
                }

                if (TryStartTruckInteraction(TruckInteractionType.RefuelAtGasStation, LocationType.GasStation))
                {
                    SessionDebugLogger.Log("FUEL", $"{GetLoadedTruckDisplayName()} started refueling at Gas Station.");
                    currentRefuelPhase = RefuelPhase.Refueling;
                }

                return;

            case RefuelPhase.Refueling:
                if (isTruckInteracting)
                {
                    return;
                }

                if (TryResumeQueuedTruckInteraction())
                {
                    return;
                }

                currentRefuelPhase = RefuelPhase.ReturnToParking;
                return;

            case RefuelPhase.ReturnToParking:
                if (truckCell != locations[LocationType.Parking].Anchor)
                {
                    StartMoveTo(locations[LocationType.Parking].Anchor);
                    return;
                }

                SessionDebugLogger.Log("FUEL", $"{GetLoadedTruckDisplayName()} finished refuel order and returned to parking.");
                currentRefuelPhase = RefuelPhase.None;
                return;
        }
    }
}
