using UnityEngine;

public enum WarehouseBusDeliveryDecisionKind
{
    Walk,
    UseBus,
    CannotDeliver
}

public readonly struct WarehouseBusDeliveryDecision
{
    public WarehouseBusDeliveryDecision(WarehouseBusDeliveryDecisionKind kind, string reason)
    {
        Kind = kind;
        Reason = reason ?? string.Empty;
    }

    public WarehouseBusDeliveryDecisionKind Kind { get; }
    public string Reason { get; }
}

public static class WarehouseBusDeliveryService
{
    public static WarehouseBusDeliveryDecision Evaluate(
        bool workerCanDeliver,
        bool hasCargo,
        int orderedStopCount,
        int directDistance,
        int accessWalkDistance,
        int exitWalkDistance,
        int directWalkThreshold,
        int maxAccessWalkDistance,
        int minimumSavings,
        bool sameStop = false)
    {
        if (!workerCanDeliver)
        {
            return new WarehouseBusDeliveryDecision(WarehouseBusDeliveryDecisionKind.CannotDeliver, "worker unavailable");
        }

        if (!hasCargo)
        {
            return new WarehouseBusDeliveryDecision(WarehouseBusDeliveryDecisionKind.CannotDeliver, "no cargo");
        }

        if (orderedStopCount < 2)
        {
            return new WarehouseBusDeliveryDecision(WarehouseBusDeliveryDecisionKind.Walk, "not enough stops");
        }

        if (sameStop)
        {
            return new WarehouseBusDeliveryDecision(WarehouseBusDeliveryDecisionKind.Walk, "origin and destination share a stop");
        }

        if (directDistance < directWalkThreshold)
        {
            return new WarehouseBusDeliveryDecision(WarehouseBusDeliveryDecisionKind.Walk, "direct walk is short");
        }

        if (accessWalkDistance > maxAccessWalkDistance || exitWalkDistance > maxAccessWalkDistance)
        {
            return new WarehouseBusDeliveryDecision(WarehouseBusDeliveryDecisionKind.Walk, "stop access is too far");
        }

        int combinedWalkDistance = Mathf.Max(0, accessWalkDistance) + Mathf.Max(0, exitWalkDistance);
        if (combinedWalkDistance > directDistance - minimumSavings)
        {
            return new WarehouseBusDeliveryDecision(WarehouseBusDeliveryDecisionKind.Walk, "bus does not save enough walking");
        }

        return new WarehouseBusDeliveryDecision(WarehouseBusDeliveryDecisionKind.UseBus, "bus saves walking");
    }
}
