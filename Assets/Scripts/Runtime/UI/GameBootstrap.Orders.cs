using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void UpdateTruckAutoMode()
    {
        TruckAutoDecisionKind decision = TruckAutoPlanner.Decide(
            isTruckAutoModeEnabled,
            currentAssignedTrip != TripType.None,
            currentRefuelPhase != RefuelPhase.None,
            isTruckMoving,
            isTruckInteracting,
            isDriverRescueActive,
            IsTruckInsideParking(),
            truckFuel,
            GetAvailableTrips().Count);

        switch (decision)
        {
            case TruckAutoDecisionKind.Refuel:
                SessionDebugLogger.Log("AUTO", $"{GetLoadedTruckDisplayName()} chose refuel because fuel is low ({Mathf.CeilToInt(truckFuel)}/{Mathf.CeilToInt(TruckFuelCapacity)}).");
                StartRefuelOrder();
                return;
            case TruckAutoDecisionKind.Trip:
            {
                List<TripOption> trips = GetAvailableTrips();
                int tripIndex = TruckAutoPlanner.PickTripIndex(trips.Count);
                if (tripIndex >= 0)
                {
                    SessionDebugLogger.Log("AUTO", $"{GetLoadedTruckDisplayName()} chose trip '{trips[tripIndex].Title}'.");
                    AssignTrip(trips[tripIndex]);
                }

                return;
            }
        }
    }

    private void EvaluateTruckAutoModeNow(TruckAgent truckAgent)
    {
        if (truckAgent == null)
        {
            return;
        }

        LoadTruckState(truckAgent);
        UpdateTruckAutoMode();
        SaveTruckState(truckAgent);
    }

    private void SetTruckAutoMode(TruckAgent truckAgent, bool enabled)
    {
        if (truckAgent == null)
        {
            return;
        }

        truckAgent.IsTruckAutoModeEnabled = enabled;
        SessionDebugLogger.Log("ORDER", $"{truckAgent.DisplayName} auto mode set to {(enabled ? "ON" : "OFF")}.");
        if (enabled)
        {
            bool canTakeOrdersNow =
                truckAgent.CurrentAssignedTrip == TripType.None &&
                truckAgent.CurrentRefuelPhase == RefuelPhase.None &&
                truckAgent.CurrentDriverRestPhase == DriverRestPhase.None &&
                !truckAgent.IsTruckMoving &&
                !truckAgent.IsTruckInteracting &&
                !truckAgent.IsDriverRescueActive &&
                IsTruckInsideParking(truckAgent);

            if (canTakeOrdersNow)
            {
                if (truckAgent.TruckFuel < 30f)
                {
                    truckAgent.CurrentRefuelPhase = RefuelPhase.ToGasStation;
                }
                else
                {
                    LoadTruckState(truckAgent);
                    List<TripOption> trips = GetAvailableTrips();
                    SaveTruckState(truckAgent);
                    int tripIndex = TruckAutoPlanner.PickTripIndex(trips.Count);
                    if (tripIndex >= 0)
                    {
                        TripOption selectedTrip = trips[tripIndex];
                        truckAgent.CurrentAssignedTrip = selectedTrip.Type;
                        truckAgent.CurrentTripPhase = TripPhase.ToPickup;
                        truckAgent.CurrentAssignedTripReward = selectedTrip.Reward;
                    }
                }
            }

            KickTruckDecision(truckAgent);
        }
    }

    private void StartRefuelOrderForTruck(TruckAgent truckAgent)
    {
        if (truckAgent == null ||
            truckAgent.CurrentAssignedTrip != TripType.None ||
            truckAgent.CurrentRefuelPhase != RefuelPhase.None ||
            truckAgent.CurrentDriverRestPhase != DriverRestPhase.None)
        {
            return;
        }

        truckAgent.CurrentRefuelPhase = RefuelPhase.ToGasStation;
        PlayUiSound(routeAssignRefuelClip, 0.94f);
        SessionDebugLogger.Log("ORDER", $"{truckAgent.DisplayName} received manual refuel order.");
        KickTruckDecision(truckAgent);
    }

    private void AssignTripToTruck(TruckAgent truckAgent, TripOption trip)
    {
        if (truckAgent == null ||
            trip == null ||
            trip.Type == TripType.None ||
            truckAgent.CurrentAssignedTrip != TripType.None ||
            truckAgent.CurrentRefuelPhase != RefuelPhase.None ||
            truckAgent.CurrentDriverRestPhase != DriverRestPhase.None)
        {
            return;
        }

        truckAgent.CurrentAssignedTrip = trip.Type;
        truckAgent.CurrentTripPhase = TripPhase.ToPickup;
        truckAgent.CurrentAssignedTripReward = trip.Reward;
        PlayAssignedTripCue(trip.Type, 0.94f);
        SessionDebugLogger.Log("ORDER", $"{truckAgent.DisplayName} assigned trip '{trip.Title}' with reward ${trip.Reward}.");
        KickTruckDecision(truckAgent);
    }

    private void KickTruckDecision(TruckAgent truckAgent)
    {
        if (truckAgent == null)
        {
            return;
        }

        LoadTruckState(truckAgent);
        UpdateAssignedTrip();
        UpdateRefuelOrder();
        UpdateTruckAutoMode();
        if (truckAgent.TruckObject != null)
        {
            truckAgent.TruckObject.transform.position = truckObject.transform.position;
            truckAgent.TruckObject.transform.rotation = truckObject.transform.rotation;
        }

        SaveTruckState(truckAgent);
    }

    private int GetTripReward(TripType tripType)
    {
        if (tripType == TripType.None)
        {
            return 0;
        }

        LocationType pickup = GetPickupLocation(tripType);
        LocationType dropoff = GetDropoffLocation(tripType);
        int totalSteps =
            GetPathStepCount(locations[LocationType.Parking].Anchor, locations[pickup].Anchor) +
            GetPathStepCount(locations[pickup].Anchor, locations[dropoff].Anchor) +
            GetPathStepCount(locations[dropoff].Anchor, locations[LocationType.Parking].Anchor);

        int handlingBonus = 12;
        int locationBonus = tripType == TripType.WarehouseToTown ? 10 : 6;
        return TripRewardCalculator.Calculate(totalSteps, handlingBonus, locationBonus);
    }

    private int GetPathStepCount(Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> path = FindPath(start, goal);
        return path == null ? 0 : Mathf.Max(0, path.Count - 1);
    }

    private LocationType GetPickupLocation(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToWarehouse => LocationType.Forest,
            TripType.WarehouseToTown => LocationType.Warehouse,
            _ => LocationType.Parking
        };
    }

    private LocationType GetDropoffLocation(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToWarehouse => LocationType.Warehouse,
            TripType.WarehouseToTown => LocationType.Town,
            _ => LocationType.Parking
        };
    }

    private TruckInteractionType GetLoadInteraction(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToWarehouse => TruckInteractionType.LoadAtForest,
            TripType.WarehouseToTown => TruckInteractionType.LoadAtWarehouse,
            _ => TruckInteractionType.None
        };
    }

    private TruckInteractionType GetUnloadInteraction(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToWarehouse => TruckInteractionType.UnloadAtWarehouse,
            TripType.WarehouseToTown => TruckInteractionType.UnloadAtTown,
            _ => TruckInteractionType.None
        };
    }
}
