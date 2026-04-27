using UnityEngine;

public enum TruckTripRuntimeActionKind
{
    Wait,
    MoveToPickup,
    StartLoading,
    AdvanceToDropoff,
    MoveToDropoff,
    StartUnloading,
    AdvanceToParking,
    MoveToParking,
    Complete
}

public readonly struct TruckTripRuntimeAction
{
    public readonly TruckTripRuntimeActionKind Kind;
    public readonly Vector2Int TargetCell;
    public readonly GameBootstrap.TripPhase NextPhase;

    public TruckTripRuntimeAction(
        TruckTripRuntimeActionKind kind,
        Vector2Int targetCell = default,
        GameBootstrap.TripPhase nextPhase = GameBootstrap.TripPhase.None)
    {
        Kind = kind;
        TargetCell = targetCell;
        NextPhase = nextPhase;
    }
}

public static class TruckTripRuntimeService
{
    public static TruckTripRuntimeAction Evaluate(
        GameBootstrap.TripPhase phase,
        Vector2Int truckCell,
        Vector2Int pickupCell,
        Vector2Int dropoffCell,
        Vector2Int parkingCell,
        bool truckInteracting,
        bool queuedInteractionResumed)
    {
        switch (phase)
        {
            case GameBootstrap.TripPhase.ToPickup:
                return truckCell == pickupCell
                    ? new TruckTripRuntimeAction(TruckTripRuntimeActionKind.StartLoading)
                    : new TruckTripRuntimeAction(TruckTripRuntimeActionKind.MoveToPickup, pickupCell);

            case GameBootstrap.TripPhase.Loading:
                if (truckInteracting || queuedInteractionResumed)
                {
                    return new TruckTripRuntimeAction(TruckTripRuntimeActionKind.Wait);
                }

                return new TruckTripRuntimeAction(
                    TruckTripRuntimeActionKind.AdvanceToDropoff,
                    nextPhase: GameBootstrap.TripPhase.ToDropoff);

            case GameBootstrap.TripPhase.ToDropoff:
                return truckCell == dropoffCell
                    ? new TruckTripRuntimeAction(TruckTripRuntimeActionKind.StartUnloading)
                    : new TruckTripRuntimeAction(TruckTripRuntimeActionKind.MoveToDropoff, dropoffCell);

            case GameBootstrap.TripPhase.Unloading:
                if (truckInteracting || queuedInteractionResumed)
                {
                    return new TruckTripRuntimeAction(TruckTripRuntimeActionKind.Wait);
                }

                return new TruckTripRuntimeAction(
                    TruckTripRuntimeActionKind.AdvanceToParking,
                    nextPhase: GameBootstrap.TripPhase.ReturnToParking);

            case GameBootstrap.TripPhase.ReturnToParking:
                return truckCell == parkingCell
                    ? new TruckTripRuntimeAction(TruckTripRuntimeActionKind.Complete)
                    : new TruckTripRuntimeAction(TruckTripRuntimeActionKind.MoveToParking, parkingCell);

            default:
                return new TruckTripRuntimeAction(TruckTripRuntimeActionKind.Wait);
        }
    }
}
