using UnityEngine;

public enum TradeRunDriverParkingActionKind
{
    Wait,
    StartCommute,
    TryBoard,
    Advance
}

public readonly struct TradeRunDriverParkingAction
{
    public readonly TradeRunDriverParkingActionKind Kind;
    public readonly GameBootstrap.TradeRunPhase NextPhase;

    public TradeRunDriverParkingAction(
        TradeRunDriverParkingActionKind kind,
        GameBootstrap.TradeRunPhase nextPhase = GameBootstrap.TradeRunPhase.None)
    {
        Kind = kind;
        NextPhase = nextPhase;
    }
}

public enum TradeRunTruckTargetActionKind
{
    Wait,
    MoveToTarget,
    Arrived
}

public readonly struct TradeRunTruckTargetAction
{
    public readonly TradeRunTruckTargetActionKind Kind;
    public readonly Vector2Int TargetCell;

    public TradeRunTruckTargetAction(TradeRunTruckTargetActionKind kind, Vector2Int targetCell = default)
    {
        Kind = kind;
        TargetCell = targetCell;
    }
}

public enum TradeRunHighwayDepartureActionKind
{
    Wait,
    RestartDriverCommute,
    StartHighwayMove,
    LeaveMap
}

public readonly struct TradeRunHighwayDepartureAction
{
    public readonly TradeRunHighwayDepartureActionKind Kind;

    public TradeRunHighwayDepartureAction(TradeRunHighwayDepartureActionKind kind)
    {
        Kind = kind;
    }
}

public readonly struct TradeRunOutOfMapTick
{
    public readonly float Timer;
    public readonly bool ShouldReturn;

    public TradeRunOutOfMapTick(float timer, bool shouldReturn)
    {
        Timer = timer;
        ShouldReturn = shouldReturn;
    }
}

public static class TradeRunRuntimeService
{
    public static GameBootstrap.TradeRunPhase GetLoadedNextPhase(bool isSellOrder)
    {
        return isSellOrder
            ? GameBootstrap.TradeRunPhase.DrivingToWarehouse
            : GameBootstrap.TradeRunPhase.DrivingToHighway;
    }

    public static GameBootstrap.TradeRunPhase GetReturnEntryNextPhase(bool isBuyOrder)
    {
        return isBuyOrder
            ? GameBootstrap.TradeRunPhase.ReturningToWarehouse
            : GameBootstrap.TradeRunPhase.ReturningToParking;
    }

    public static TradeRunDriverParkingAction EvaluateDriverToParking(
        bool driverAlreadyBoarded,
        bool driverWaitingAtParking,
        bool driverCanStartCommute,
        bool isSellOrder)
    {
        if (driverAlreadyBoarded)
        {
            return new TradeRunDriverParkingAction(TradeRunDriverParkingActionKind.Advance, GetLoadedNextPhase(isSellOrder));
        }

        if (driverCanStartCommute)
        {
            return new TradeRunDriverParkingAction(TradeRunDriverParkingActionKind.StartCommute);
        }

        if (driverWaitingAtParking)
        {
            return new TradeRunDriverParkingAction(TradeRunDriverParkingActionKind.TryBoard);
        }

        return new TradeRunDriverParkingAction(TradeRunDriverParkingActionKind.Wait);
    }

    public static TradeRunTruckTargetAction EvaluateTruckTarget(
        Vector2Int currentCell,
        Vector2Int targetCell,
        bool truckMoving,
        bool truckInteracting)
    {
        if (currentCell == targetCell && !truckMoving && !truckInteracting)
        {
            return new TradeRunTruckTargetAction(TradeRunTruckTargetActionKind.Arrived, targetCell);
        }

        if (!truckMoving && !truckInteracting)
        {
            return new TradeRunTruckTargetAction(TradeRunTruckTargetActionKind.MoveToTarget, targetCell);
        }

        return new TradeRunTruckTargetAction(TradeRunTruckTargetActionKind.Wait, targetCell);
    }

    public static TradeRunHighwayDepartureAction EvaluateDrivingToHighway(
        bool driverOnTruck,
        bool driverCanStartCommute,
        Vector2Int truckCell,
        Vector2Int edgeCell,
        bool truckMoving,
        bool truckInteracting)
    {
        if (!driverOnTruck)
        {
            return new TradeRunHighwayDepartureAction(
                driverCanStartCommute
                    ? TradeRunHighwayDepartureActionKind.RestartDriverCommute
                    : TradeRunHighwayDepartureActionKind.Wait);
        }

        if (truckCell == edgeCell && !truckMoving && !truckInteracting)
        {
            return new TradeRunHighwayDepartureAction(TradeRunHighwayDepartureActionKind.LeaveMap);
        }

        if (!truckMoving && !truckInteracting)
        {
            return new TradeRunHighwayDepartureAction(TradeRunHighwayDepartureActionKind.StartHighwayMove);
        }

        return new TradeRunHighwayDepartureAction(TradeRunHighwayDepartureActionKind.Wait);
    }

    public static TradeRunOutOfMapTick TickOutOfMap(
        bool racingActive,
        float timer,
        float deltaTime,
        float gameSpeedMultiplier)
    {
        if (racingActive)
        {
            return new TradeRunOutOfMapTick(timer, false);
        }

        float nextTimer = Mathf.Max(0f, timer - deltaTime * gameSpeedMultiplier);
        return new TradeRunOutOfMapTick(nextTimer, nextTimer <= 0f);
    }
}
