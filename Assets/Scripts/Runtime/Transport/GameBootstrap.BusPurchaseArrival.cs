using UnityEngine;

public partial class GameBootstrap
{
    private void StartPurchasedBusArrival(BusAgent busAgent)
    {
        if (busAgent?.BusObject == null || !locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            return;
        }

        busAgent.PurchaseArrivalWaypoints.Clear();
        busAgent.PurchaseArrivalWaypointIndex = 0;
        busAgent.PurchaseArrivalSpeed = 4.3f;

        Vector3 parkingSlot = GetBusParkingSlotWorldPosition(busAgent.ParkingSlotIndex);
        float laneZ = GetEdgeHighwayBusLaneWorldZ(isCitySideLane: true);
        float outsideX = GridWidth + 2.6f;
        float entryX = GridWidth - 0.5f;
        Vector2Int approachRoadCell = FindPurchaseArrivalApproachRoadCell(parking);
        Vector3 approachRoadWorld = GetBusArrivalWorldPoint(approachRoadCell.x + 0.5f, approachRoadCell.y + 0.5f);
        float exitX = Mathf.Clamp(approachRoadCell.x + 0.5f, 1.5f, GridWidth - 1.5f);

        busAgent.PurchaseArrivalWaypoints.Add(GetBusArrivalWorldPoint(outsideX, laneZ));
        busAgent.PurchaseArrivalWaypoints.Add(GetBusArrivalWorldPoint(entryX, laneZ));
        busAgent.PurchaseArrivalWaypoints.Add(GetBusArrivalWorldPoint(exitX, laneZ));
        busAgent.PurchaseArrivalWaypoints.Add(approachRoadWorld);
        busAgent.PurchaseArrivalWaypoints.Add(parkingSlot);

        busAgent.BusObject.transform.position = busAgent.PurchaseArrivalWaypoints[0];
        if (busAgent.PurchaseArrivalWaypoints.Count > 1)
        {
            FaceBusTowards(busAgent, busAgent.PurchaseArrivalWaypoints[1]);
        }

        busAgent.IsPurchaseArrivalActive = true;
        SessionDebugLogger.Log(
            "BUS_POOL",
            $"{busAgent.DisplayName} purchase arrival started from edge highway to parking slot {busAgent.ParkingSlotIndex}; approachRoad=({approachRoadCell.x},{approachRoadCell.y}), parkingAnchor=({parking.Anchor.x},{parking.Anchor.y}).");
    }

    private Vector3 GetBusArrivalWorldPoint(float x, float z)
    {
        return GetRoadVehicleWorldPosition(x, z, LocalBusRoadSurfaceLift);
    }

    private bool UpdatePurchasedBusArrival(BusAgent busAgent, float dt)
    {
        if (busAgent == null || !busAgent.IsPurchaseArrivalActive)
        {
            return false;
        }

        if (busAgent.BusObject == null ||
            busAgent.PurchaseArrivalWaypoints.Count == 0 ||
            !locations.ContainsKey(LocationType.Parking))
        {
            busAgent.IsPurchaseArrivalActive = false;
            return true;
        }

        int index = Mathf.Clamp(busAgent.PurchaseArrivalWaypointIndex, 0, busAgent.PurchaseArrivalWaypoints.Count - 1);
        Vector3 target = busAgent.PurchaseArrivalWaypoints[index];
        Vector3 current = busAgent.BusObject.transform.position;
        Vector3 flatDelta = target - current;
        flatDelta.y = 0f;
        if (flatDelta.sqrMagnitude > 0.0001f)
        {
            FaceBusTowards(busAgent, target);
        }

        float speed = busAgent.PurchaseArrivalSpeed * Mathf.Max(gameSpeedMultiplier, 0.1f);
        Vector3 nextPosition = Vector3.MoveTowards(current, target, speed * dt);
        nextPosition = WithRoadVehicleHeight(nextPosition, LocalBusRoadSurfaceLift);
        busAgent.BusObject.transform.position = nextPosition;

        if ((busAgent.BusObject.transform.position - target).sqrMagnitude > 0.035f)
        {
            return true;
        }

        busAgent.PurchaseArrivalWaypointIndex++;
        if (busAgent.PurchaseArrivalWaypointIndex < busAgent.PurchaseArrivalWaypoints.Count)
        {
            return true;
        }

        busAgent.BusObject.transform.position = GetBusParkingSlotWorldPosition(busAgent.ParkingSlotIndex);
        busAgent.BusObject.transform.rotation = Quaternion.identity;
        busAgent.IsPurchaseArrivalActive = false;
        busAgent.PurchaseArrivalWaypoints.Clear();
        busAgent.PurchaseArrivalWaypointIndex = 0;
        SessionDebugLogger.Log("BUS_POOL", $"{busAgent.DisplayName} purchase arrival completed and parked.");
        isShiftsScreenDirty = true;
        return true;
    }

    private void FaceBusTowards(BusAgent busAgent, Vector3 target)
    {
        Vector3 direction = target - busAgent.BusObject.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        busAgent.BusObject.transform.rotation = Quaternion.Slerp(
            busAgent.BusObject.transform.rotation,
            GetLocalBusFacingRotation(direction),
            0.28f);
    }
}
