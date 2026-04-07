using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void UpdateAssignedTrip()
    {
        if (currentAssignedTrip == TripType.None || currentRefuelPhase != RefuelPhase.None ||
            currentDriverRestPhase != DriverRestPhase.None || isDriverRescueActive || isTruckMoving || isTruckInteracting)
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

                PlayTruckFx(parkingReturnCueClip, 0.64f);
                AwardMoney(currentAssignedTripReward);
                SessionDebugLogger.Log("TRIP", $"{GetLoadedTruckDisplayName()} completed trip {GetTripTitle(currentAssignedTrip)} and earned ${currentAssignedTripReward}.");
                currentAssignedTrip = TripType.None;
                currentTripPhase = TripPhase.None;
                currentAssignedTripReward = 0;
                if (needsRestAfterTrip)
                {
                    needsRestAfterTrip = false;
                    currentDriverRestPhase = DriverRestPhase.ToMotel;
                    SessionDebugLogger.Log("REST", $"{GetLoadedTruckDisplayName()} energy low — starting rest at Motel.");
                }
                return;
        }
    }

    private void UpdateRefuelOrder()
    {
        if (currentRefuelPhase == RefuelPhase.None || currentAssignedTrip != TripType.None ||
            currentDriverRestPhase != DriverRestPhase.None || isDriverRescueActive || isTruckMoving || isTruckInteracting)
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

                PlayTruckFx(parkingReturnCueClip, 0.58f);
                SessionDebugLogger.Log("FUEL", $"{GetLoadedTruckDisplayName()} finished refuel order and returned to parking.");
                currentRefuelPhase = RefuelPhase.None;
                return;
        }
    }

    private void UpdateDriverRest()
    {
        if (currentDriverRestPhase == DriverRestPhase.None)
        {
            return;
        }

        switch (currentDriverRestPhase)
        {
            case DriverRestPhase.ToMotel:
                if (isTruckMoving || isTruckInteracting || isDriverRescueActive)
                {
                    return;
                }
                if (truckCell != locations[LocationType.Motel].Anchor)
                {
                    StartMoveTo(locations[LocationType.Motel].Anchor);
                    return;
                }
                motelParkingSlotIndex = PickFreeMotelSlot();
                motelParkedPosition = GetMotelParkingSlotWorldPosition(motelParkingSlotIndex);
                currentDriverRestPhase = DriverRestPhase.ParkAtMotel;
                return;

            case DriverRestPhase.ParkAtMotel:
                if (isTruckMoving || isTruckInteracting)
                {
                    return;
                }
                // Snap truck to motel parking slot
                truckObject.transform.position = motelParkedPosition;
                truckTargetWorld = motelParkedPosition;
                truckSegmentStartWorld = motelParkedPosition;
                // Driver exits and walks to motel entrance
                driverObject.SetActive(true);
                driverObject.transform.position = GetDriverStandPointNearTruck();
                driverObject.transform.rotation = truckObject.transform.rotation;
                driverWalkAnimationTime = 0f;
                ApplyDriverPose(0f, 0f);
                driverRescueTargetWorld = GetDriverStandPointNearLocation(LocationType.Motel);
                BuildDriverRescuePath(driverObject.transform.position, driverRescueTargetWorld);
                isDriverRescueActive = true;
                currentDriverRescuePhase = DriverRescuePhase.ToMotelEntrance;
                currentDriverRestPhase = DriverRestPhase.DriverWalkToMotel;
                SessionDebugLogger.Log("REST", $"{GetLoadedTruckDisplayName()} parked at Motel slot {motelParkingSlotIndex}. Driver walking to entrance.");
                return;

            case DriverRestPhase.DriverWalkToMotel:
                // Handled by UpdateDriverRescue (ToMotelEntrance case) which transitions to Sleeping
                return;

            case DriverRestPhase.Sleeping:
                driverSleepTimer -= Time.deltaTime * gameSpeedMultiplier;
                if (driverSleepTimer > 0f)
                {
                    return;
                }
                driverEnergy = DriverEnergyMax;
                SessionDebugLogger.Log("REST", $"{GetLoadedTruckDisplayName()} driver rested. Energy restored to {DriverEnergyMax}.");
                // Driver walks back to truck
                driverObject.SetActive(true);
                driverObject.transform.position = GetDriverStandPointNearLocation(LocationType.Motel);
                driverWalkAnimationTime = 0f;
                ApplyDriverPose(0f, 0f);
                driverRescueTargetWorld = motelParkedPosition + new Vector3(0.32f, 0f, -0.32f);
                BuildDriverRescuePath(driverObject.transform.position, driverRescueTargetWorld);
                isDriverRescueActive = true;
                currentDriverRescuePhase = DriverRescuePhase.ToTruckAtMotel;
                currentDriverRestPhase = DriverRestPhase.DriverWalkToTruck;
                return;

            case DriverRestPhase.DriverWalkToTruck:
                // Handled by UpdateDriverRescue (ToTruckAtMotel case) which transitions to ReturnToParking
                return;

            case DriverRestPhase.ReturnToParking:
                if (isTruckMoving || isTruckInteracting || isDriverRescueActive)
                {
                    return;
                }
                if (truckCell != locations[LocationType.Parking].Anchor)
                {
                    StartMoveTo(locations[LocationType.Parking].Anchor);
                    return;
                }
                PlayTruckFx(parkingReturnCueClip, 0.58f);
                SessionDebugLogger.Log("REST", $"{GetLoadedTruckDisplayName()} returned from Motel to Parking. Ready for orders.");
                currentDriverRestPhase = DriverRestPhase.None;
                return;
        }
    }
}
