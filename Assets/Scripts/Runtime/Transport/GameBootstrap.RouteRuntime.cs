using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void UpdateAssignedTrip(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

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
                ApplyWorkerRoadFocusEffect(driver);
                SessionDebugLogger.Log("TRIP", $"{GetLoadedTruckDisplayName()} completed trip {GetTripTitle(currentAssignedTrip)}.");
                PushFeedEvent(
                    $"{GetLoadedTruckDisplayName()} completed {GetTripTitle(currentAssignedTrip)}.",
                    $"{GetLoadedTruckDisplayName()} завершил рейс {GetTripTitle(currentAssignedTrip)}.",
                    FeedEventType.Info);
                currentAssignedTrip = TripType.None;
                currentTripPhase = TripPhase.None;
                currentAssignedTripReward = 0;

                if (driver.NeedsShiftEndReturn && GetCurrentTruckForDriver(driver) is TruckAgent truckAgent)
                {
                    EnsurePendingShiftSalaryPaid(driver);
                    StartDriverMotelRest(truckAgent, driver);
                }

                return;
        }
    }

    private void UpdateRefuelOrder(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        if (currentRefuelPhase == RefuelPhase.None || currentAssignedTrip != TripType.None ||
            driver.RestPhase != DriverRestPhase.None || isDriverRescueActive || isTruckMoving || isTruckInteracting)
        {
            return;
        }

        switch (currentRefuelPhase)
        {
            case RefuelPhase.ToGasStation:
                if (locations.TryGetValue(LocationType.GasStation, out LocationData gsRefuel))
                {
                    gsRefuel.FuelStored = GasStationMaxFuelStorage;
                }

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
                if (driver.NeedsShiftEndReturn && GetCurrentTruckForDriver(driver) is TruckAgent truckAgent)
                {
                    EnsurePendingShiftSalaryPaid(driver);
                    StartDriverMotelRest(truckAgent, driver);
                }
                return;
        }
    }

    private void UpdateDriverRest(DriverAgent driver)
    {
        if (driver == null || driver.RestPhase == DriverRestPhase.None)
        {
            return;
        }

        switch (driver.RestPhase)
        {
            case DriverRestPhase.ToMotel:
                if (GetCurrentTruckForDriver(driver) is TruckAgent currentTruck)
                {
                    EnsurePendingShiftSalaryPaid(driver);
                    StartDriverMotelRest(currentTruck, driver);
                }
                return;

            case DriverRestPhase.DriverWalkToMotel:
                return;

            case DriverRestPhase.Sleeping:
                driver.SleepTimer -= Time.deltaTime * gameSpeedMultiplier;
                if (driver.SleepTimer > 0f)
                {
                    return;
                }

                driver.DriverObject.SetActive(true);
                driver.DriverObject.transform.position = driver.MotelIdlePosition;
                driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                driver.WalkAnimationTime = 0f;
                driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
                driver.IdleWanderPointIndex = -1;
                driver.IdleConversationTimer = 0f;
                driver.IdleConversationPartnerId = -1;
                ResetWorkerNeedTimer(driver, WorkerNeedKind.Sleep);
                ApplyWorkerRestedEffect(driver);
                driver.SleptToday = true;
                driver.LifeGoal = WorkerLifeGoal.Idle;
                ApplyDriverPose(driver, 0f, 0f);
                driver.RestPhase = DriverRestPhase.None;
                SessionDebugLogger.Log("REST", $"{driver.DriverName} finished sleep; needs={FormatWorkerNeedsDebug(driver)}.");
                return;
        }
    }
}

