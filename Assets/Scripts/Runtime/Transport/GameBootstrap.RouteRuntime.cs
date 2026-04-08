using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void UpdateAssignedTrip(DriverAgent driver)
    {
        if (currentAssignedTrip == TripType.None || currentRefuelPhase != RefuelPhase.None ||
            driver.RestPhase != DriverRestPhase.None || isDriverRescueActive || isTruckMoving || isTruckInteracting)
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
                if (driver.NeedsRestAfterTrip)
                {
                    driver.NeedsRestAfterTrip = false;
                    driver.IsOnActiveShift = false;
                    driver.RestPhase = DriverRestPhase.ToMotel;
                    SessionDebugLogger.Log("REST", $"{GetLoadedTruckDisplayName()} energy low — starting rest at Motel.");
                }
                return;
        }
    }

    private void UpdateRefuelOrder(DriverAgent driver)
    {
        if (currentRefuelPhase == RefuelPhase.None || currentAssignedTrip != TripType.None ||
            driver.RestPhase != DriverRestPhase.None || isDriverRescueActive || isTruckMoving || isTruckInteracting)
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

    private void UpdateDriverRest(DriverAgent driver)
    {
        if (driver.RestPhase == DriverRestPhase.None)
        {
            return;
        }

        switch (driver.RestPhase)
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
                driver.MotelSlotIndex = PickFreeMotelSlot(driver);
                driver.MotelParkedPosition = GetMotelParkingSlotWorldPosition(driver.MotelSlotIndex);
                driver.RestPhase = DriverRestPhase.ParkAtMotel;
                return;

            case DriverRestPhase.ParkAtMotel:
                if (isTruckMoving || isTruckInteracting)
                {
                    return;
                }
                truckObject.transform.position = driver.MotelParkedPosition;
                truckTargetWorld = driver.MotelParkedPosition;
                truckSegmentStartWorld = driver.MotelParkedPosition;
                driver.DriverObject.SetActive(true);
                driver.DriverObject.transform.position = GetDriverStandPointNearTruck();
                driver.DriverObject.transform.rotation = truckObject.transform.rotation;
                driver.WalkAnimationTime = 0f;
                ApplyDriverPose(driver, 0f, 0f);
                driver.WalkTargetWorld = GetDriverStandPointNearLocation(LocationType.Motel);
                BuildDriverWalkPath(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld);
                isDriverRescueActive = true;
                driver.WalkPhase = DriverRescuePhase.ToMotelEntrance;
                driver.RestPhase = DriverRestPhase.DriverWalkToMotel;
                SessionDebugLogger.Log("REST", $"{GetLoadedTruckDisplayName()} parked at Motel slot {driver.MotelSlotIndex}. Driver walking to entrance.");
                return;

            case DriverRestPhase.DriverWalkToMotel:
                // Handled by UpdateDriverWalk (ToMotelEntrance case) which transitions to Sleeping
                return;

            case DriverRestPhase.Sleeping:
                driver.SleepTimer -= Time.deltaTime * gameSpeedMultiplier;
                float sleepProgress = 1f - Mathf.Clamp01(driver.SleepTimer / DriverSleepDuration);
                driver.Energy = Mathf.Lerp(driver.SleepStartEnergy, DriverEnergyMax, sleepProgress);
                if (driver.SleepTimer > 0f)
                {
                    return;
                }
                driver.Energy = DriverEnergyMax;
                SessionDebugLogger.Log("REST", $"{GetLoadedTruckDisplayName()} driver rested. Energy restored to {DriverEnergyMax}.");
                driver.DriverObject.SetActive(true);
                driver.DriverObject.transform.position = GetDriverStandPointNearLocation(LocationType.Motel);
                driver.WalkAnimationTime = 0f;
                ApplyDriverPose(driver, 0f, 0f);
                driver.WalkTargetWorld = driver.MotelParkedPosition + new Vector3(0.32f, 0f, -0.32f);
                BuildDriverWalkPath(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld);
                isDriverRescueActive = true;
                driver.WalkPhase = DriverRescuePhase.ToTruckAtMotel;
                driver.RestPhase = DriverRestPhase.DriverWalkToTruck;
                return;

            case DriverRestPhase.DriverWalkToTruck:
                // Handled by UpdateDriverWalk (ToTruckAtMotel case) which transitions to ReturnToParking
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
                driver.RestPhase = DriverRestPhase.None;
                return;
        }
    }
}
