using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private bool TryGetTruckParkingSlotWorldPose(int parkingSlotIndex, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            return false;
        }

        if (TryGetImportedTruckParkingSlotWorldPose(parking, parkingSlotIndex, out position, out rotation))
        {
            return true;
        }

        return TryGetFallbackTruckParkingSlotWorldPose(parking, parkingSlotIndex, out position, out rotation);
    }

    private Quaternion GetParkingSlotWorldRotation(int parkingSlotIndex)
    {
        return TryGetTruckParkingSlotWorldPose(parkingSlotIndex, out _, out Quaternion rotation)
            ? rotation
            : Quaternion.LookRotation(Vector3.forward, Vector3.up);
    }

    private bool TrySnapLoadedTruckToAssignedParkingSlot()
    {
        TruckAgent loaded = GetLoadedTruckAgent();
        if (loaded?.TruckObject == null ||
            !locations.TryGetValue(LocationType.Parking, out LocationData parking) ||
            truckCell != parking.Anchor)
        {
            return false;
        }

        return TryParkTruckAgentInAssignedParkingSlot(loaded, parking);
    }

    private bool TryParkTruckAgentInAssignedParkingSlot(TruckAgent truckAgent, LocationData parking)
    {
        if (truckAgent?.TruckObject == null ||
            parking == null ||
            !TryGetTruckParkingSlotWorldPose(truckAgent.ParkingSlotIndex, out Vector3 parkedPosition, out Quaternion parkedRotation))
        {
            return false;
        }

        Vector3 forward = parkedRotation * Vector3.forward;
        truckAgent.TruckObject.transform.position = parkedPosition;
        truckAgent.TruckObject.transform.rotation = parkedRotation;
        truckAgent.TruckCell = parking.Anchor;
        truckAgent.TruckTargetWorld = parkedPosition;
        truckAgent.TruckSegmentStartWorld = parkedPosition;
        truckAgent.TruckSmoothedForward = forward;
        truckAgent.IsTruckMoving = false;
        truckAgent.TruckSegmentProgress = 0f;
        truckAgent.TruckSegmentDuration = 0f;

        if (truckAgent.TruckObject == truckObject)
        {
            truckObject.transform.position = parkedPosition;
            truckObject.transform.rotation = parkedRotation;
            truckCell = parking.Anchor;
            truckTargetWorld = parkedPosition;
            truckSegmentStartWorld = parkedPosition;
            truckSmoothedForward = forward;
            isTruckMoving = false;
            truckSegmentProgress = 0f;
            truckSegmentDuration = 0f;
        }

        return true;
    }

    private bool TryGetImportedTruckParkingSlotWorldPose(
        LocationData parking,
        int parkingSlotIndex,
        out Vector3 position,
        out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        List<Transform> markers = parking?.ImportedRuntime?.TruckParkingSlotMarkers;
        if (markers == null || markers.Count == 0)
        {
            return false;
        }

        int slotIndex = Mathf.Clamp(parkingSlotIndex, 0, markers.Count - 1);
        Transform marker = markers[slotIndex];
        if (marker == null)
        {
            return false;
        }

        Vector3 forward = marker.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
        {
            forward = GetParkingSlotForward(parking);
        }

        position = WithRoadVehicleHeight(marker.position, TruckSegmentStartLift);
        rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        return true;
    }

    private bool TryGetFallbackTruckParkingSlotWorldPose(
        LocationData parking,
        int parkingSlotIndex,
        out Vector3 position,
        out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (parking == null)
        {
            return false;
        }

        Vector3 center = GetLocationCenter(LocationType.Parking);
        Vector3 forward = GetParkingSlotForward(parking);
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        Vector2 localSize = GetAnchorLocalFootprintSize(parking.Min, parking.Max, parking.Anchor);
        float halfWidth = Mathf.Max(0.35f, localSize.x * 0.5f - 0.45f);
        float halfDepth = Mathf.Max(0.28f, localSize.y * 0.5f - 0.38f);
        Vector3[] localSlots =
        {
            new(0f, 0f, -halfDepth * 0.1f),
            new(-halfWidth * 0.55f, 0f, -halfDepth * 0.72f),
            new(0f, 0f, -halfDepth * 0.72f),
            new(halfWidth * 0.55f, 0f, -halfDepth * 0.72f),
            new(-halfWidth * 0.7f, 0f, halfDepth * 0.18f)
        };

        int slotIndex = Mathf.Clamp(parkingSlotIndex, 0, localSlots.Length - 1);
        Vector3 local = localSlots[slotIndex];
        position = WithRoadVehicleHeight(center + right * local.x + forward * local.z, TruckSegmentStartLift);
        rotation = Quaternion.LookRotation(forward, Vector3.up);
        return true;
    }

    private Vector3 GetParkingSlotForward(LocationData parking)
    {
        Vector3 center = GetLocationCenter(LocationType.Parking);
        Vector3 forward = GetAnchorFacingDirection(center, parking.Min, parking.Max, parking.Anchor);
        forward.y = 0f;
        return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
    }

    private TruckAgent GetLoadedTruckAgent()
    {
        for (int i = 0; i < truckAgents.Count; i++)
        {
            TruckAgent truckAgent = truckAgents[i];
            if (truckAgent?.TruckObject == truckObject)
            {
                return truckAgent;
            }
        }

        return null;
    }
}
