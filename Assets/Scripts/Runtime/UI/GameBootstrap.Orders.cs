using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void UpdateTruckAutoMode()
    {
        // Find the driver for the currently loaded truck
        DriverAgent currentDriver = null;
        foreach (TruckAgent ta in truckAgents)
        {
            if (ta.TruckObject == truckObject) { currentDriver = ta.Driver; break; }
        }
        if (currentDriver == null) return;

        if (!IsDriverOnShift(currentDriver))
        {
            return;
        }

        TruckAutoDecisionKind decision = TruckAutoPlanner.Decide(
            isTruckAutoModeEnabled,
            currentAssignedTrip != TripType.None,
            currentRefuelPhase != RefuelPhase.None,
            isTruckMoving,
            isTruckInteracting,
            isDriverRescueActive || currentDriver.RestPhase != DriverRestPhase.None,
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

        LogCommand($"SetAutoMode({truckAgent.DisplayName}, {(enabled ? "ON" : "OFF")})");
        truckAgent.IsTruckAutoModeEnabled = enabled;
        SessionDebugLogger.Log("ORDER", $"{truckAgent.DisplayName} auto mode set to {(enabled ? "ON" : "OFF")}.");
        LogTruckReaction(truckAgent, $"auto mode set to {(enabled ? "ON" : "OFF")}");
        if (enabled)
        {
            if (truckAgent.Driver == null)
            {
                LogTruckReaction(truckAgent, "auto mode enabled but no current driver is boarded");
                return;
            }

            bool canTakeOrdersNow =
                truckAgent.CurrentAssignedTrip == TripType.None &&
                truckAgent.CurrentRefuelPhase == RefuelPhase.None &&
                truckAgent.Driver.RestPhase == DriverRestPhase.None &&
                !truckAgent.IsTruckMoving &&
                !truckAgent.IsTruckInteracting &&
                !truckAgent.IsDriverRescueActive &&
                IsDriverOnShift(truckAgent.Driver) &&
                IsTruckInsideParking(truckAgent);

            if (canTakeOrdersNow)
            {
                if (truckAgent.TruckFuel < 30f)
                {
                    truckAgent.CurrentRefuelPhase = RefuelPhase.ToGasStation;
                    LogTruckReaction(truckAgent, $"queued refuel because fuel is low ({Mathf.CeilToInt(truckAgent.TruckFuel)}/{Mathf.CeilToInt(TruckFuelCapacity)})");
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
                        LogTruckReaction(truckAgent, $"queued auto trip '{selectedTrip.Title}' for ${selectedTrip.Reward}");
                    }
                }
            }
            else
            {
                LogTruckReaction(truckAgent, "auto mode enabled but no order was queued immediately");
            }

        }
    }

    private void StartRefuelOrderForTruck(TruckAgent truckAgent)
    {
        LogCommand($"StartRefuelOrder({truckAgent?.DisplayName ?? "null"})");
        if (truckAgent == null ||
            truckAgent.Driver == null ||
            truckAgent.CurrentAssignedTrip != TripType.None ||
            truckAgent.CurrentRefuelPhase != RefuelPhase.None ||
            truckAgent.Driver.RestPhase != DriverRestPhase.None ||
            !IsDriverOnShift(truckAgent.Driver))
        {
            if (truckAgent != null)
            {
                LogTruckReaction(truckAgent, $"refuel command rejected: {GetTruckCommandBlockReason(truckAgent)}");
            }
            return;
        }

        truckAgent.CurrentRefuelPhase = RefuelPhase.ToGasStation;
        PlayUiSound(routeAssignRefuelClip, 0.94f);
        SessionDebugLogger.Log("ORDER", $"{truckAgent.DisplayName} received manual refuel order.");
        LogTruckReaction(truckAgent, "accepted manual refuel order");
    }

    private void AssignTripToTruck(TruckAgent truckAgent, TripOption trip)
    {
        LogCommand($"AssignTrip({truckAgent?.DisplayName ?? "null"}, {trip?.Title ?? "None"})");
        if (truckAgent == null ||
            trip == null ||
            trip.Type == TripType.None ||
            truckAgent.Driver == null ||
            truckAgent.CurrentAssignedTrip != TripType.None ||
            truckAgent.CurrentRefuelPhase != RefuelPhase.None ||
            truckAgent.Driver.RestPhase != DriverRestPhase.None ||
            !IsDriverOnShift(truckAgent.Driver))
        {
            if (truckAgent != null)
            {
                LogTruckReaction(truckAgent, $"trip command rejected: {GetTruckCommandBlockReason(truckAgent)}");
            }
            return;
        }

        truckAgent.CurrentAssignedTrip = trip.Type;
        truckAgent.CurrentTripPhase = TripPhase.ToPickup;
        truckAgent.CurrentAssignedTripReward = trip.Reward;
        PlayAssignedTripCue(trip.Type, 0.94f);
        SessionDebugLogger.Log("ORDER", $"{truckAgent.DisplayName} assigned trip '{trip.Title}' with reward ${trip.Reward}.");
        LogTruckReaction(truckAgent, $"accepted trip '{trip.Title}' with reward ${trip.Reward}");
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
        int locationBonus = tripType == TripType.SawmillToWarehouse ? 10 : 6;
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
            TripType.ForestToSawmill => LocationType.Forest,
            TripType.SawmillToWarehouse => LocationType.Sawmill,
            _ => LocationType.Parking
        };
    }

    private LocationType GetDropoffLocation(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToSawmill => LocationType.Sawmill,
            TripType.SawmillToWarehouse => LocationType.Warehouse,
            _ => LocationType.Parking
        };
    }

    private TruckInteractionType GetLoadInteraction(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToSawmill => TruckInteractionType.LoadAtForest,
            TripType.SawmillToWarehouse => TruckInteractionType.LoadAtSawmill,
            _ => TruckInteractionType.None
        };
    }

    private TruckInteractionType GetUnloadInteraction(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToSawmill => TruckInteractionType.UnloadAtSawmill,
            TripType.SawmillToWarehouse => TruckInteractionType.UnloadAtWarehouse,
            _ => TruckInteractionType.None
        };
    }
}
