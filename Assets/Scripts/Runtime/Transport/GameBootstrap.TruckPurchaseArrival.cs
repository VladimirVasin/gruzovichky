using UnityEngine;

public partial class GameBootstrap
{
    private void StartPurchasedTruckArrival(TruckAgent truckAgent)
    {
        if (truckAgent?.TruckObject == null || !locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            return;
        }

        truckAgent.PurchaseArrivalWaypoints.Clear();
        truckAgent.PurchaseArrivalWaypointIndex = 0;
        truckAgent.PurchaseArrivalSpeed = 4.6f;

        Vector3 parkingSlot = GetParkingSlotWorldPosition(truckAgent.ParkingSlotIndex);
        float laneZ = GetEdgeHighwayBusLaneWorldZ(isCitySideLane: true);
        float outsideX = GridWidth + 2.4f;
        float entryX = GridWidth - 0.5f;
        Vector2Int approachRoadCell = FindPurchaseArrivalApproachRoadCell(parking);
        Vector3 approachRoadWorld = GetTruckArrivalWorldPoint(approachRoadCell.x + 0.5f, approachRoadCell.y + 0.5f);
        float exitX = Mathf.Clamp(approachRoadCell.x + 0.5f, 1.5f, GridWidth - 1.5f);

        truckAgent.PurchaseArrivalWaypoints.Add(GetTruckArrivalWorldPoint(outsideX, laneZ));
        truckAgent.PurchaseArrivalWaypoints.Add(GetTruckArrivalWorldPoint(entryX, laneZ));
        truckAgent.PurchaseArrivalWaypoints.Add(GetTruckArrivalWorldPoint(exitX, laneZ));
        truckAgent.PurchaseArrivalWaypoints.Add(approachRoadWorld);
        truckAgent.PurchaseArrivalWaypoints.Add(parkingSlot);

        truckAgent.TruckObject.transform.position = truckAgent.PurchaseArrivalWaypoints[0];
        if (truckAgent.PurchaseArrivalWaypoints.Count > 1)
        {
            FaceTruckTowards(truckAgent, truckAgent.PurchaseArrivalWaypoints[1]);
        }

        truckAgent.TruckCell = new Vector2Int(Mathf.Clamp(GridWidth - 1, 0, GridWidth - 1), 1);
        truckAgent.TruckTargetWorld = parkingSlot;
        truckAgent.TruckSegmentStartWorld = truckAgent.TruckObject.transform.position;
        truckAgent.TruckSmoothedForward = truckAgent.TruckObject.transform.forward;
        truckAgent.IsTruckMoving = false;
        truckAgent.IsTruckInteracting = false;
        truckAgent.IsPurchaseArrivalActive = true;

        SessionDebugLogger.Log(
            "TRUCK",
            $"{truckAgent.DisplayName} purchase arrival started from edge highway to parking slot {truckAgent.ParkingSlotIndex}; approachRoad=({approachRoadCell.x},{approachRoadCell.y}), parkingAnchor=({parking.Anchor.x},{parking.Anchor.y}).");
    }

    private Vector2Int FindPurchaseArrivalApproachRoadCell(LocationData parking)
    {
        Vector2Int best = parking.Anchor;
        int bestScore = int.MaxValue;

        foreach (Vector2Int roadCell in roadCells)
        {
            int distance = Mathf.Abs(roadCell.x - parking.Anchor.x) + Mathf.Abs(roadCell.y - parking.Anchor.y);
            int directionBias = roadCell.x <= parking.Anchor.x ? 0 : 4;
            int score = distance * 10 + directionBias;
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            best = roadCell;
        }

        return best;
    }

    private Vector3 GetTruckArrivalWorldPoint(float x, float z)
    {
        return GetRoadVehicleWorldPosition(x, z, TruckSegmentStartLift);
    }

    private bool UpdatePurchasedTruckArrival(TruckAgent truckAgent, float dt)
    {
        if (truckAgent == null || !truckAgent.IsPurchaseArrivalActive)
        {
            return false;
        }

        if (truckAgent.TruckObject == null ||
            truckAgent.PurchaseArrivalWaypoints.Count == 0 ||
            !locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            truckAgent.IsPurchaseArrivalActive = false;
            return true;
        }

        int index = Mathf.Clamp(truckAgent.PurchaseArrivalWaypointIndex, 0, truckAgent.PurchaseArrivalWaypoints.Count - 1);
        Vector3 target = truckAgent.PurchaseArrivalWaypoints[index];
        Vector3 current = truckAgent.TruckObject.transform.position;
        Vector3 delta = target - current;
        delta.y = 0f;
        if (delta.sqrMagnitude > 0.0001f)
        {
            FaceTruckTowards(truckAgent, target);
        }

        float speed = truckAgent.PurchaseArrivalSpeed * Mathf.Max(gameSpeedMultiplier, 0.1f);
        Vector3 nextPosition = Vector3.MoveTowards(current, target, speed * dt);
        nextPosition = WithRoadVehicleHeight(nextPosition, TruckSegmentStartLift);
        truckAgent.TruckObject.transform.position = nextPosition;
        SpinTruckArrivalWheels(truckAgent, speed * dt);
        ApplyTruckArrivalVisualMotion(truckAgent, speed, true);

        if ((truckAgent.TruckObject.transform.position - target).sqrMagnitude > 0.035f)
        {
            return true;
        }

        truckAgent.PurchaseArrivalWaypointIndex++;
        if (truckAgent.PurchaseArrivalWaypointIndex < truckAgent.PurchaseArrivalWaypoints.Count)
        {
            return true;
        }

        Vector3 parkedPosition = GetParkingSlotWorldPosition(truckAgent.ParkingSlotIndex);
        truckAgent.TruckObject.transform.position = parkedPosition;
        truckAgent.TruckObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        truckAgent.TruckCell = parking.Anchor;
        truckAgent.TruckTargetWorld = parkedPosition;
        truckAgent.TruckSegmentStartWorld = parkedPosition;
        truckAgent.TruckSmoothedForward = Vector3.forward;
        truckAgent.IsTruckMoving = false;
        truckAgent.IsPurchaseArrivalActive = false;
        ApplyTruckArrivalVisualMotion(truckAgent, 0f, false);
        truckAgent.PurchaseArrivalWaypoints.Clear();
        truckAgent.PurchaseArrivalWaypointIndex = 0;
        SessionDebugLogger.Log("TRUCK", $"{truckAgent.DisplayName} purchase arrival completed and parked.");
        isFleetScreenDirty = true;
        return true;
    }

    private void FaceTruckTowards(TruckAgent truckAgent, Vector3 target)
    {
        Vector3 direction = target - truckAgent.TruckObject.transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        truckAgent.TruckObject.transform.rotation = Quaternion.Slerp(
            truckAgent.TruckObject.transform.rotation,
            Quaternion.LookRotation(direction.normalized, Vector3.up),
            0.28f);
    }

    private static void SpinTruckArrivalWheels(TruckAgent truckAgent, float distance)
    {
        if (truckAgent.TruckWheels.Count == 0)
        {
            return;
        }

        float spin = distance * 260f;
        for (int i = 0; i < truckAgent.TruckWheels.Count; i++)
        {
            Transform wheel = truckAgent.TruckWheels[i];
            if (wheel == null)
            {
                continue;
            }

            bool importedWheel = wheel.name.StartsWith("ImportedTruckWheelPivot_", System.StringComparison.Ordinal);
            wheel.Rotate(importedWheel ? Vector3.forward : Vector3.up, spin, Space.Self);
        }
    }

    private static void ApplyTruckArrivalVisualMotion(TruckAgent truckAgent, float speed, bool moving)
    {
        if (truckAgent == null || truckAgent.TruckVisualRoot == null)
        {
            return;
        }

        float normalizedSpeed = Mathf.Clamp01(speed / Mathf.Max(0.01f, TruckCruiseSpeed));
        float bob = moving ? Mathf.Sin(Time.time * 10f + truckAgent.TruckNumber * 0.37f) * 0.038f * normalizedSpeed : 0f;
        float pitch = moving ? Mathf.Sin(Time.time * 7.3f + truckAgent.TruckNumber * 0.19f) * 0.55f * normalizedSpeed : 0f;
        float roll = moving ? Mathf.Sin(Time.time * 8.6f + truckAgent.TruckNumber * 0.31f) * 0.38f * normalizedSpeed : 0f;
        truckAgent.TruckVisualRoot.localPosition = new Vector3(0f, bob, 0f);
        if (truckAgent.TruckBodyTransform != null)
        {
            truckAgent.TruckBodyTransform.localRotation = Quaternion.Euler(pitch, 0f, roll);
        }

        if (truckAgent.TruckCabinTransform != null)
        {
            truckAgent.TruckCabinTransform.localRotation = Quaternion.Euler(pitch * 0.72f, 0f, roll * 0.48f);
        }
    }
}
