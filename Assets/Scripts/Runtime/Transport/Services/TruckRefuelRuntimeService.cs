using UnityEngine;

public enum TruckRefuelRuntimeActionKind
{
    Wait,
    MoveToGasStation,
    StartRefueling,
    AdvanceToParking,
    MoveToParking,
    Complete
}

public readonly struct TruckRefuelRuntimeAction
{
    public readonly TruckRefuelRuntimeActionKind Kind;
    public readonly Vector2Int TargetCell;
    public readonly GameBootstrap.RefuelPhase NextPhase;

    public TruckRefuelRuntimeAction(
        TruckRefuelRuntimeActionKind kind,
        Vector2Int targetCell = default,
        GameBootstrap.RefuelPhase nextPhase = GameBootstrap.RefuelPhase.None)
    {
        Kind = kind;
        TargetCell = targetCell;
        NextPhase = nextPhase;
    }
}

public static class TruckRefuelRuntimeService
{
    public static TruckRefuelRuntimeAction Evaluate(
        GameBootstrap.RefuelPhase phase,
        Vector2Int truckCell,
        Vector2Int gasStationCell,
        Vector2Int parkingCell,
        bool truckInteracting,
        bool queuedInteractionResumed)
    {
        switch (phase)
        {
            case GameBootstrap.RefuelPhase.ToGasStation:
                return truckCell == gasStationCell
                    ? new TruckRefuelRuntimeAction(TruckRefuelRuntimeActionKind.StartRefueling)
                    : new TruckRefuelRuntimeAction(TruckRefuelRuntimeActionKind.MoveToGasStation, gasStationCell);

            case GameBootstrap.RefuelPhase.Refueling:
                if (truckInteracting || queuedInteractionResumed)
                {
                    return new TruckRefuelRuntimeAction(TruckRefuelRuntimeActionKind.Wait);
                }

                return new TruckRefuelRuntimeAction(
                    TruckRefuelRuntimeActionKind.AdvanceToParking,
                    nextPhase: GameBootstrap.RefuelPhase.ReturnToParking);

            case GameBootstrap.RefuelPhase.ReturnToParking:
                return truckCell == parkingCell
                    ? new TruckRefuelRuntimeAction(TruckRefuelRuntimeActionKind.Complete)
                    : new TruckRefuelRuntimeAction(TruckRefuelRuntimeActionKind.MoveToParking, parkingCell);

            default:
                return new TruckRefuelRuntimeAction(TruckRefuelRuntimeActionKind.Wait);
        }
    }
}
