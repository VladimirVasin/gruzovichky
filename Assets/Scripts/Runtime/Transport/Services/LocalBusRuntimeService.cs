using UnityEngine;

public readonly struct LocalBusDwellTick
{
    public readonly float Timer;
    public readonly bool IsComplete;

    public LocalBusDwellTick(float timer, bool isComplete)
    {
        Timer = timer;
        IsComplete = isComplete;
    }
}

public readonly struct LocalBusMovementStep
{
    public readonly Vector3 Position;
    public readonly Vector3 FacingDirection;
    public readonly bool ReachedWaypoint;
    public readonly bool HasFacingDirection;

    public LocalBusMovementStep(Vector3 position, Vector3 facingDirection, bool reachedWaypoint, bool hasFacingDirection)
    {
        Position = position;
        FacingDirection = facingDirection;
        ReachedWaypoint = reachedWaypoint;
        HasFacingDirection = hasFacingDirection;
    }
}

public static class LocalBusRuntimeService
{
    public static LocalBusDwellTick TickDwell(float timer, float deltaTime)
    {
        float nextTimer = timer - deltaTime;
        return new LocalBusDwellTick(nextTimer, nextTimer <= 0f);
    }

    public static LocalBusMovementStep StepTowardWaypoint(
        Vector3 currentPosition,
        Vector3 targetPosition,
        float speed,
        float deltaTime,
        System.Func<float, float, float> sampleHeight,
        float verticalOffset)
    {
        Vector3 flatDelta = new(targetPosition.x - currentPosition.x, 0f, targetPosition.z - currentPosition.z);
        float remaining = flatDelta.magnitude;
        if (remaining <= 0.001f)
        {
            Vector3 snapped = WithHeight(targetPosition, sampleHeight, verticalOffset);
            return new LocalBusMovementStep(snapped, flatDelta, reachedWaypoint: true, hasFacingDirection: false);
        }

        float step = Mathf.Max(0f, speed) * Mathf.Max(0f, deltaTime);
        if (step >= remaining)
        {
            Vector3 snapped = WithHeight(targetPosition, sampleHeight, verticalOffset);
            return new LocalBusMovementStep(snapped, flatDelta, reachedWaypoint: true, hasFacingDirection: flatDelta.sqrMagnitude > 0.0001f);
        }

        Vector3 nextPosition = currentPosition + flatDelta.normalized * step;
        nextPosition = WithHeight(nextPosition, sampleHeight, verticalOffset);
        return new LocalBusMovementStep(nextPosition, flatDelta, reachedWaypoint: false, hasFacingDirection: true);
    }

    private static Vector3 WithHeight(Vector3 position, System.Func<float, float, float> sampleHeight, float verticalOffset)
    {
        float height = sampleHeight != null ? sampleHeight(position.x, position.z) : position.y;
        position.y = height + verticalOffset;
        return position;
    }
}
