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

        if (!TruckRuntimeGuardService.CanUpdateAssignedTrip(
                currentAssignedTrip != TripType.None,
                currentRefuelPhase != RefuelPhase.None,
                driver.RestPhase != DriverRestPhase.None,
                isDriverRescueActive,
                isTruckMoving,
                isTruckInteracting))
        {
            return;
        }

        LocationType pickupLocation = GetPickupLocation(currentAssignedTrip);
        LocationType dropoffLocation = GetDropoffLocation(currentAssignedTrip);
        if (!TryGetTripLocations(currentAssignedTrip, out LocationData pickup, out LocationData dropoff, out LocationData parking))
        {
            CancelLoadedTruckRuntimeOrder($"trip {GetTripTitle(currentAssignedTrip)} cannot continue because a required building is missing");
            return;
        }

        bool queuedInteractionResumed = false;
        if ((currentTripPhase == TripPhase.Loading || currentTripPhase == TripPhase.Unloading) &&
            !isTruckInteracting)
        {
            queuedInteractionResumed = TryResumeQueuedTruckInteraction();
        }

        TruckTripRuntimeAction action = TruckTripRuntimeService.Evaluate(
            currentTripPhase,
            truckCell,
            pickup.Anchor,
            dropoff.Anchor,
            parking.Anchor,
            isTruckInteracting,
            queuedInteractionResumed);

        switch (action.Kind)
        {
            case TruckTripRuntimeActionKind.MoveToPickup:
            case TruckTripRuntimeActionKind.MoveToDropoff:
            case TruckTripRuntimeActionKind.MoveToParking:
                StartMoveTo(action.TargetCell);
                return;

            case TruckTripRuntimeActionKind.StartLoading:
                if (TryStartTruckInteraction(GetLoadInteraction(currentAssignedTrip), pickupLocation))
                {
                    SessionDebugLogger.Log("TRIP", $"{GetLoadedTruckDisplayName()} started loading at {pickupLocation} for trip {GetTripTitle(currentAssignedTrip)}.");
                    currentTripPhase = TripPhase.Loading;
                }
                return;

            case TruckTripRuntimeActionKind.AdvanceToDropoff:
            case TruckTripRuntimeActionKind.AdvanceToParking:
                currentTripPhase = action.NextPhase;
                return;

            case TruckTripRuntimeActionKind.StartUnloading:
                if (TryStartTruckInteraction(GetUnloadInteraction(currentAssignedTrip), dropoffLocation))
                {
                    SessionDebugLogger.Log("TRIP", $"{GetLoadedTruckDisplayName()} started unloading at {dropoffLocation} for trip {GetTripTitle(currentAssignedTrip)}.");
                    currentTripPhase = TripPhase.Unloading;
                }
                return;

            case TruckTripRuntimeActionKind.Complete:
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

            case TruckTripRuntimeActionKind.Wait:
            default:
                return;
        }
    }

    private void UpdateRefuelOrder(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        if (!TruckRuntimeGuardService.CanUpdateRefuelOrder(
                currentRefuelPhase != RefuelPhase.None,
                currentAssignedTrip != TripType.None,
                driver.RestPhase != DriverRestPhase.None,
                isDriverRescueActive,
                isTruckMoving,
                isTruckInteracting))
        {
            return;
        }

        if (!locations.TryGetValue(LocationType.GasStation, out LocationData gasStation) ||
            !locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            SessionDebugLogger.Log(
                "FUEL",
                $"{GetLoadedTruckDisplayName()} cancelled refuel order: required location missing " +
                $"(GasStation={locations.ContainsKey(LocationType.GasStation)}, Parking={locations.ContainsKey(LocationType.Parking)}).");
            currentRefuelPhase = RefuelPhase.None;
            return;
        }

        bool queuedInteractionResumed = false;
        if (currentRefuelPhase == RefuelPhase.Refueling && !isTruckInteracting)
        {
            queuedInteractionResumed = TryResumeQueuedTruckInteraction();
        }

        TruckRefuelRuntimeAction action = TruckRefuelRuntimeService.Evaluate(
            currentRefuelPhase,
            truckCell,
            gasStation.Anchor,
            parking.Anchor,
            isTruckInteracting,
            queuedInteractionResumed);

        switch (action.Kind)
        {
            case TruckRefuelRuntimeActionKind.MoveToGasStation:
            case TruckRefuelRuntimeActionKind.MoveToParking:
                StartMoveTo(action.TargetCell);
                return;

            case TruckRefuelRuntimeActionKind.StartRefueling:
                if (TryStartTruckInteraction(TruckInteractionType.RefuelAtGasStation, LocationType.GasStation))
                {
                    SessionDebugLogger.Log("FUEL", $"{GetLoadedTruckDisplayName()} started refueling at Gas Station.");
                    currentRefuelPhase = RefuelPhase.Refueling;
                }
                return;

            case TruckRefuelRuntimeActionKind.AdvanceToParking:
                currentRefuelPhase = action.NextPhase;
                return;

            case TruckRefuelRuntimeActionKind.Complete:
                SessionDebugLogger.Log("FUEL", $"{GetLoadedTruckDisplayName()} finished refuel order and returned to parking.");
                currentRefuelPhase = RefuelPhase.None;
                if (driver.NeedsShiftEndReturn && GetCurrentTruckForDriver(driver) is TruckAgent truckAgent)
                {
                    EnsurePendingShiftSalaryPaid(driver);
                    StartDriverMotelRest(truckAgent, driver);
                }
                return;

            case TruckRefuelRuntimeActionKind.Wait:
            default:
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
                driver.SleptToday = true;
                driver.LifeGoal = WorkerLifeGoal.Idle;
                ApplyDriverPose(driver, 0f, 0f);
                driver.RestPhase = DriverRestPhase.None;
                SessionDebugLogger.Log("REST", $"{driver.DriverName} finished sleep; needs={FormatWorkerNeedsDebug(driver)}.");
                return;

            case DriverRestPhase.SleepingAtHome:
                driver.SleepTimer -= Time.deltaTime * gameSpeedMultiplier;
                if (driver.SleepTimer > 0f) return;
                {
                    int hi = driver.AssignedPersonalHouseIndex;
                    Vector3 wakePos = (hi >= 0 && hi < personalHouses.Count)
                        ? GetDriverStandPointNearPersonalHouse(hi)
                        : driver.MotelIdlePosition;
                    driver.DriverObject.SetActive(true);
                    driver.DriverObject.transform.position = wakePos;
                    driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                }
                driver.WalkAnimationTime = 0f;
                driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
                driver.IdleWanderPointIndex = -1;
                driver.IdleConversationTimer = 0f;
                driver.IdleConversationPartnerId = -1;
                ResetWorkerNeedTimer(driver, WorkerNeedKind.Sleep);
                ResetWorkerNeedTimer(driver, WorkerNeedKind.Meal);
                driver.SleptToday = true;
                driver.AteToday = true;
                driver.LifeGoal = WorkerLifeGoal.Idle;
                ApplyDriverPose(driver, 0f, 0f);
                driver.RestPhase = DriverRestPhase.None;
                SessionDebugLogger.Log("REST", $"{driver.DriverName} woke up at home after sleep and breakfast; needs={FormatWorkerNeedsDebug(driver)}.");
                return;
        }
    }
}
