using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void LoadTruckState(TruckAgent truckAgent)
    {
        currentLoadedTruckAgent = truckAgent;
        truckObject = truckAgent.TruckObject;
        truckVisualRoot = truckAgent.TruckVisualRoot;
        truckBodyTransform = truckAgent.TruckBodyTransform;
        truckCabinTransform = truckAgent.TruckCabinTransform;
        truckHeadlightLeftRenderer = truckAgent.TruckHeadlightLeftRenderer;
        truckHeadlightRightRenderer = truckAgent.TruckHeadlightRightRenderer;
        truckHeadlightLeftMaterial = truckAgent.TruckHeadlightLeftMaterial;
        truckHeadlightRightMaterial = truckAgent.TruckHeadlightRightMaterial;
        truckLoopAudioSource = truckAgent.TruckLoopAudioSource;
        truckEngineAudioPhaseOffset = truckAgent.EngineAudioPhaseOffset;
        truckEngineAudioWobbleSpeed = truckAgent.EngineAudioWobbleSpeed;
        truckEngineAudioPitchBias = truckAgent.EngineAudioPitchBias;
        truckEngineAudioVolumeBias = truckAgent.EngineAudioVolumeBias;
        truckCell = truckAgent.TruckCell;
        truckTargetWorld = truckAgent.TruckTargetWorld;
        truckSegmentStartWorld = truckAgent.TruckSegmentStartWorld;
        truckSmoothedForward = truckAgent.TruckSmoothedForward;
        isTruckMoving = truckAgent.IsTruckMoving;
        isTruckInteracting = truckAgent.IsTruckInteracting;
        isTruckWaitingForService = truckAgent.IsTruckWaitingForService;
        isDriverRescueActive = truckAgent.IsDriverRescueActive;
        isTruckAutoModeEnabled = truckAgent.IsTruckAutoModeEnabled;
        truckCargoType = truckAgent.TruckCargoType;
        truckCargoAmount = truckAgent.TruckCargoAmount;
        truckSegmentProgress = truckAgent.TruckSegmentProgress;
        truckSegmentDuration = truckAgent.TruckSegmentDuration;
        truckWheelSpinAngle = truckAgent.TruckWheelSpinAngle;
        truckSteerAngle = truckAgent.TruckSteerAngle;
        truckInteractionTimer = truckAgent.TruckInteractionTimer;
        truckFuel = truckAgent.TruckFuel;
        currentAssignedTripReward = truckAgent.CurrentAssignedTripReward;
        currentAssignedTrip = truckAgent.CurrentAssignedTrip;
        currentTripPickupLocationInstanceId = truckAgent.CurrentTripPickupLocationInstanceId;
        currentTripDropoffLocationInstanceId = truckAgent.CurrentTripDropoffLocationInstanceId;
        currentTripPhase = truckAgent.CurrentTripPhase;
        currentRefuelPhase = truckAgent.CurrentRefuelPhase;
        activeTruckInteraction = truckAgent.ActiveTruckInteraction;
        queuedTruckInteraction = truckAgent.QueuedTruckInteraction;
        truckInteractionTargetRotation = truckAgent.TruckInteractionTargetRotation;
        truckInteractionBuildingPoint = truckAgent.TruckInteractionBuildingPoint;
        activeServiceLocation = truckAgent.ActiveServiceLocation;
        queuedServiceLocation = truckAgent.QueuedServiceLocation;
        activeServiceLocationInstanceId = truckAgent.ActiveServiceLocationInstanceId;
        queuedServiceLocationInstanceId = truckAgent.QueuedServiceLocationInstanceId;
        activePath.Clear();
        activePath.AddRange(truckAgent.ActivePath);
        truckWheels.Clear();
        truckWheels.AddRange(truckAgent.TruckWheels);
        truckFrontWheels.Clear();
        truckFrontWheels.AddRange(truckAgent.TruckFrontWheels);
        truckHeadlights.Clear();
        truckHeadlights.AddRange(truckAgent.TruckHeadlights);
    }

    private void SaveTruckState(TruckAgent truckAgent)
    {
        truckAgent.TruckObject = truckObject;
        truckAgent.TruckVisualRoot = truckVisualRoot;
        truckAgent.TruckBodyTransform = truckBodyTransform;
        truckAgent.TruckCabinTransform = truckCabinTransform;
        truckAgent.TruckHeadlightLeftRenderer = truckHeadlightLeftRenderer;
        truckAgent.TruckHeadlightRightRenderer = truckHeadlightRightRenderer;
        truckAgent.TruckHeadlightLeftMaterial = truckHeadlightLeftMaterial;
        truckAgent.TruckHeadlightRightMaterial = truckHeadlightRightMaterial;
        truckAgent.TruckWheels.Clear();
        truckAgent.TruckWheels.AddRange(truckWheels);
        truckAgent.TruckFrontWheels.Clear();
        truckAgent.TruckFrontWheels.AddRange(truckFrontWheels);
        truckAgent.TruckHeadlights.Clear();
        truckAgent.TruckHeadlights.AddRange(truckHeadlights);
        truckAgent.TruckLoopAudioSource = truckLoopAudioSource;
        truckAgent.EngineAudioPhaseOffset = truckEngineAudioPhaseOffset;
        truckAgent.EngineAudioWobbleSpeed = truckEngineAudioWobbleSpeed;
        truckAgent.EngineAudioPitchBias = truckEngineAudioPitchBias;
        truckAgent.EngineAudioVolumeBias = truckEngineAudioVolumeBias;
        truckAgent.TruckCell = truckCell;
        truckAgent.TruckTargetWorld = truckTargetWorld;
        truckAgent.TruckSegmentStartWorld = truckSegmentStartWorld;
        truckAgent.TruckSmoothedForward = truckSmoothedForward;
        truckAgent.IsTruckMoving = isTruckMoving;
        truckAgent.IsTruckInteracting = isTruckInteracting;
        truckAgent.IsTruckWaitingForService = isTruckWaitingForService;
        truckAgent.IsDriverRescueActive = isDriverRescueActive;
        truckAgent.IsTruckAutoModeEnabled = isTruckAutoModeEnabled;
        truckAgent.TruckCargoType = truckCargoType;
        truckAgent.TruckCargoAmount = truckCargoAmount;
        truckAgent.TruckSegmentProgress = truckSegmentProgress;
        truckAgent.TruckSegmentDuration = truckSegmentDuration;
        truckAgent.TruckWheelSpinAngle = truckWheelSpinAngle;
        truckAgent.TruckSteerAngle = truckSteerAngle;
        truckAgent.TruckInteractionTimer = truckInteractionTimer;
        truckAgent.TruckFuel = truckFuel;
        truckAgent.CurrentAssignedTripReward = currentAssignedTripReward;
        truckAgent.CurrentAssignedTrip = currentAssignedTrip;
        truckAgent.CurrentTripPickupLocationInstanceId = currentTripPickupLocationInstanceId;
        truckAgent.CurrentTripDropoffLocationInstanceId = currentTripDropoffLocationInstanceId;
        truckAgent.CurrentTripPhase = currentTripPhase;
        truckAgent.CurrentRefuelPhase = currentRefuelPhase;
        truckAgent.ActiveTruckInteraction = activeTruckInteraction;
        truckAgent.QueuedTruckInteraction = queuedTruckInteraction;
        truckAgent.TruckInteractionTargetRotation = truckInteractionTargetRotation;
        truckAgent.TruckInteractionBuildingPoint = truckInteractionBuildingPoint;
        truckAgent.ActiveServiceLocation = activeServiceLocation;
        truckAgent.QueuedServiceLocation = queuedServiceLocation;
        truckAgent.ActiveServiceLocationInstanceId = activeServiceLocationInstanceId;
        truckAgent.QueuedServiceLocationInstanceId = queuedServiceLocationInstanceId;
        truckAgent.ActivePath.Clear();
        truckAgent.ActivePath.AddRange(activePath);
    }

    private int GetOwnedTruckCount()
    {
        return truckAgents.Count;
    }

    private int GetParkingTruckCount()
    {
        int count = 0;
        foreach (TruckAgent truckAgent in truckAgents)
        {
            if (IsTruckInsideParking(truckAgent))
            {
                count++;
            }
        }

        return count;
    }

    private Vector3 GetParkingSlotWorldPosition(int parkingSlotIndex)
    {
        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            return Vector3.zero;
        }

        Vector3 center = GetLocationCenter(LocationType.Parking);
        float halfWidth = Mathf.Max(0.35f, ((parking.Max.x - parking.Min.x + 1) * 0.5f) - 0.45f);
        float halfDepth = Mathf.Max(0.28f, ((parking.Max.y - parking.Min.y + 1) * 0.5f) - 0.38f);
        Vector3[] parkingSlots =
        {
            center + new Vector3(0f, 0f, -halfDepth * 0.1f),
            center + new Vector3(-halfWidth * 0.55f, 0f, -halfDepth * 0.72f),
            center + new Vector3(0f, 0f, -halfDepth * 0.72f),
            center + new Vector3(halfWidth * 0.55f, 0f, -halfDepth * 0.72f),
            center + new Vector3(-halfWidth * 0.7f, 0f, halfDepth * 0.18f)
        };

        int slotIndex = Mathf.Clamp(parkingSlotIndex, 0, parkingSlots.Length - 1);
        return WithRoadVehicleHeight(parkingSlots[slotIndex], TruckSegmentStartLift);
    }

    private void CancelLoadedTruckRuntimeOrder(string reason, bool stopMovement = true)
    {
        currentAssignedTrip = TripType.None;
        currentTripPhase = TripPhase.None;
        currentAssignedTripReward = 0;
        currentTripPickupLocationInstanceId = 0;
        currentTripDropoffLocationInstanceId = 0;
        currentRefuelPhase = RefuelPhase.None;
        activeTruckInteraction = TruckInteractionType.None;
        queuedTruckInteraction = TruckInteractionType.None;
        activeServiceLocation = null;
        queuedServiceLocation = null;
        activeServiceLocationInstanceId = 0;
        queuedServiceLocationInstanceId = 0;
        truckInteractionTimer = 0f;
        isTruckInteracting = false;
        isTruckWaitingForService = false;
        isDriverRescueActive = false;
        isTruckAutoModeEnabled = false;
        activePath.Clear();

        if (stopMovement)
        {
            isTruckMoving = false;
            truckSegmentProgress = 0f;
            truckSegmentDuration = 0f;
            if (truckObject != null)
            {
                truckTargetWorld = WithRoadVehicleHeight(truckObject.transform.position, TruckSegmentStartLift);
                truckSegmentStartWorld = truckTargetWorld;
                truckObject.transform.position = truckTargetWorld;
                truckCell = WorldToCell(truckTargetWorld);
            }
        }

        if (!string.IsNullOrEmpty(reason))
        {
            SessionDebugLogger.Log("TRIP", $"{GetLoadedTruckDisplayName()} cancelled runtime order: {reason}.");
        }
    }

    private Vector3 GetDriverIdleMotelPosition(int driverIndex, DriverAgent driver = null)
    {
        Vector3 position = GetDriverIdleMotelWanderPosition(driverIndex, driverIndex);
        for (int attempt = 0; attempt < 28; attempt++)
        {
            Vector3 candidate = GetDriverIdleMotelWanderPosition(driverIndex, driverIndex + attempt * 3);
            if (!IsDriverIdlePositionReserved(driver, candidate))
            {
                position = candidate;
                break;
            }
        }

        position.y = SampleTerrainHeight(position.x, position.z);
        return position;
    }

    private Vector3 GetDriverIdleMotelWanderPosition(int driverIndex, int pointIndex)
    {
        if (!locations.TryGetValue(LocationType.Motel, out LocationData motel))
        {
            Vector3 fallback = locations.ContainsKey(LocationType.IntercityStop)
                ? GetIntercityStopIdlePosition(driverIndex, pointIndex)
                : locations.ContainsKey(LocationType.Parking)
                    ? GetLocationCenter(LocationType.Parking) + new Vector3(1.25f, 0f, 1.25f)
                : new Vector3(GridWidth * 0.5f, 0f, GridHeight * 0.5f);
            fallback.y = SampleTerrainHeight(fallback.x, fallback.z);
            return fallback;
        }

        Vector3 frontageBase = GetDriverStandPointNearLocation(LocationType.Motel);
        if (frontageBase == Vector3.zero)
        {
            frontageBase.y = SampleTerrainHeight(frontageBase.x, frontageBase.z);
            return frontageBase;
        }

        Vector3 center = GetLocationCenter(LocationType.Motel);
        Vector3 anchorCenter = GetCellCenter(motel.Anchor);
        Vector3 outward = anchorCenter - center;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.0001f)
        {
            outward = Vector3.forward;
        }
        else
        {
            outward.Normalize();
        }

        Vector3 right = new Vector3(outward.z, 0f, -outward.x);
        Vector2[] localPoints =
        {
            new Vector2(0.55f, -1.15f),
            new Vector2(1.05f, -1.35f),
            new Vector2(1.62f, -1.05f),
            new Vector2(2.05f, -0.52f),
            new Vector2(2.22f, 0.14f),
            new Vector2(1.96f, 0.82f),
            new Vector2(1.42f, 1.36f),
            new Vector2(0.82f, 1.74f),
            new Vector2(0.18f, 1.98f),
            new Vector2(-0.34f, 1.52f),
            new Vector2(-0.58f, 0.88f),
            new Vector2(-0.26f, 0.18f),
            new Vector2(0.36f, -0.26f),
            new Vector2(1.22f, 0.34f),
            new Vector2(2.78f, -1.42f),
            new Vector2(3.34f, -0.76f),
            new Vector2(3.58f, 0.12f),
            new Vector2(3.32f, 0.98f),
            new Vector2(2.74f, 1.72f),
            new Vector2(1.92f, 2.38f),
            new Vector2(0.92f, 2.78f),
            new Vector2(-0.18f, 2.66f),
            new Vector2(-0.94f, 2.08f),
            new Vector2(-1.12f, 1.18f),
            new Vector2(-0.92f, -0.18f),
            new Vector2(-0.32f, -1.48f),
            new Vector2(0.82f, -2.16f),
            new Vector2(2.02f, -2.08f)
        };

        int baseIndex = Mathf.Abs(pointIndex + Mathf.Max(0, driverIndex)) % localPoints.Length;
        for (int attempt = 0; attempt < localPoints.Length; attempt++)
        {
            Vector2 localPoint = localPoints[(baseIndex + attempt) % localPoints.Length];
            Vector3 position = frontageBase + right * localPoint.x + outward * localPoint.y;
            position.y = SampleTerrainHeight(position.x, position.z);

            Vector2Int cell = WorldToCell(position);
            if (!IsDriverIdleStandCell(cell))
            {
                continue;
            }

            return position;
        }

        frontageBase.y = SampleTerrainHeight(frontageBase.x, frontageBase.z);
        if (IsDriverIdleStandCell(WorldToCell(frontageBase)))
        {
            return frontageBase;
        }

        if (TryFindNearestDriverSafeWalkCell(WorldToCell(frontageBase), out Vector2Int safeCell))
        {
            return GetCellCenter(safeCell);
        }

        return frontageBase;
    }

    private bool IsDriverIdleStandCell(Vector2Int cell)
    {
        return IsInsideGrid(cell) &&
               !roadCells.Contains(cell) &&
               !edgeHighwayCells.Contains(cell) &&
               !IsLocationCell(cell) &&
               !IsBuildingWalkBufferCell(cell) &&
               !waterCells.Contains(cell);
    }

    private bool IsDriverIdlePositionReserved(DriverAgent driver, Vector3 candidate)
    {
        float personalSpaceSqr = DriverIdlePersonalSpace * DriverIdlePersonalSpace;
        Vector2Int candidateCell = WorldToCell(candidate);
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent other = driverAgents[i];
            if (other == null || other == driver || other.DriverObject == null)
            {
                continue;
            }

            if (other.DriverObject.activeSelf && IsDriverIdlePointTooClose(other.DriverObject.transform.position, candidate, candidateCell, personalSpaceSqr))
            {
                return true;
            }

            if (IsDriverIdlePointTooClose(other.MotelIdlePosition, candidate, candidateCell, personalSpaceSqr))
            {
                return true;
            }

            if (other.WalkPhase != DriverRescuePhase.None &&
                IsDriverIdlePointTooClose(other.WalkTargetWorld, candidate, candidateCell, personalSpaceSqr))
            {
                return true;
            }

            if (other.WalkPath.Count <= 0)
            {
                continue;
            }

            Vector3 finalWaypoint = other.WalkPath[other.WalkPath.Count - 1];
            if (IsDriverIdlePointTooClose(finalWaypoint, candidate, candidateCell, personalSpaceSqr))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsDriverIdlePointTooClose(Vector3 reservedPosition, Vector3 candidate, Vector2Int candidateCell, float personalSpaceSqr)
    {
        if (reservedPosition == Vector3.zero)
        {
            return false;
        }

        Vector3 delta = reservedPosition - candidate;
        delta.y = 0f;
        return delta.sqrMagnitude < personalSpaceSqr || WorldToCell(reservedPosition) == candidateCell;
    }

    private Vector3 GetIntercityStopIdlePosition(int driverIndex, int pointIndex)
    {
        Vector3 center = GetLocationCenter(LocationType.IntercityStop);
        Vector2[] localPoints =
        {
            new Vector2(-2.4f, 1.2f),
            new Vector2(-1.5f, 1.9f),
            new Vector2(-0.2f, 2.2f),
            new Vector2(1.1f, 1.85f),
            new Vector2(2.25f, 1.15f),
            new Vector2(2.55f, 0.15f),
            new Vector2(1.6f, -0.85f),
            new Vector2(0.25f, -1.25f),
            new Vector2(-1.2f, -0.95f),
            new Vector2(-2.35f, -0.1f)
        };

        int baseIndex = Mathf.Abs(pointIndex + Mathf.Max(0, driverIndex)) % localPoints.Length;
        for (int attempt = 0; attempt < localPoints.Length; attempt++)
        {
            Vector2 localPoint = localPoints[(baseIndex + attempt) % localPoints.Length];
            Vector3 position = center + new Vector3(localPoint.x, 0f, localPoint.y);
            Vector2Int cell = WorldToCell(position);
            if (!IsDriverIdleStandCell(cell))
            {
                continue;
            }

            position.y = SampleTerrainHeight(position.x, position.z);
            return position;
        }

        center.y = SampleTerrainHeight(center.x, center.z);
        return center;
    }

    private void MoveStarterIdleWorkersToMotel(bool logWhenNoCandidates = true)
    {
        if (!locations.ContainsKey(LocationType.Motel))
        {
            return;
        }

        int moved = 0;
        int movedWithoutPath = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver?.DriverObject == null ||
                !driver.DriverObject.activeSelf ||
                driver.IsArrivingByBus ||
                driver.IsLeavingTown ||
                driver.HasDepartedTown ||
                driver.IsOnActiveShift ||
                driver.RestPhase != DriverRestPhase.None ||
                driver.IsInsideBuilding ||
                driver.NeedsShiftEndReturn ||
                driver.WaitingForShiftAtParking ||
                driver.IsDrivingPersonalCar ||
                IsDriverBusyWalkPhase(driver) ||
                IsDriverMotelIdlePositionNearCurrentMotel(driver))
            {
                continue;
            }

            Vector3 current = driver.DriverObject.transform.position;
            DriverRescuePhase interruptedPhase = driver.WalkPhase;
            Vector3 target = GetDriverIdleMotelPosition(i, driver);
            driver.WalkTargetWorld = target;
            driver.WalkPhase = DriverRescuePhase.IdleWander;
            driver.IdleWanderPauseTimer = 0f;
            driver.IdleWanderPointIndex = -1;
            driver.WalkAnimationTime = 0f;
            if (!BuildDriverWalkPath(driver, current, target))
            {
                CompleteDriverMotelRelocationWithoutWalkPath(
                    driver,
                    target,
                    interruptedPhase,
                    "starter idle relocation after Motel construction; Motel does not require road access");
                movedWithoutPath++;
                continue;
            }

            if (driver.IsInsideBuilding)
            {
                ExitWorkerServiceInterior(driver, interruptedPhase);
            }

            ReleaseBench(driver);
            ReleaseCatInteraction(driver);
            if (interruptedPhase == DriverRescuePhase.IdleSmoking)
            {
                StopDriverSmokingParticles(driver);
            }

            driver.MotelIdlePosition = target;
            driver.IdleActivityTimer = 0f;
            driver.IdleConversationTimer = 0f;
            driver.IdleConversationPartnerId = -1;
            driver.PendingVendorLocationInstanceId = 0;
            driver.PendingVendorItemId = string.Empty;
            moved++;
        }

        if (moved > 0 || movedWithoutPath > 0 || logWhenNoCandidates)
        {
            SessionDebugLogger.Log(
                "DRIVER",
                $"Starter idle worker Motel relocation checked: moved={moved}, movedWithoutPath={movedWithoutPath}.");
        }
    }

    private bool IsDriverMotelIdlePositionNearCurrentMotel(DriverAgent driver)
    {
        if (driver == null || !locations.TryGetValue(LocationType.Motel, out LocationData motel))
        {
            return false;
        }

        const int margin = 5;
        Vector2Int cell = WorldToCell(driver.MotelIdlePosition);
        return cell.x >= motel.Min.x - margin &&
               cell.x <= motel.Max.x + margin &&
               cell.y >= motel.Min.y - margin &&
               cell.y <= motel.Max.y + margin;
    }

    private Vector3 GetDriverParkingWaitPosition(TruckAgent truckAgent)
    {
        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            return Vector3.zero;
        }

        int slotIndex = truckAgent != null ? truckAgent.ParkingSlotIndex : 0;
        Vector2Int waitCell = GetDriverParkingSafeWaitCell(parking, slotIndex);
        Vector3 waitPosition = GetCellCenter(waitCell);
        waitPosition.y = SampleTerrainHeight(waitPosition.x, waitPosition.z);
        return waitPosition;
    }

    private Vector2Int GetDriverParkingSafeWaitCell(LocationData parking, int parkingSlotIndex)
    {
        Vector2Int access = parking.RoadAccess == default ? parking.Anchor : parking.RoadAccess;
        Vector2Int clampedFootprint = new(
            Mathf.Clamp(access.x, parking.Min.x, parking.Max.x),
            Mathf.Clamp(access.y, parking.Min.y, parking.Max.y));
        Vector2Int outward = GetDominantCardinalDirection(access - clampedFootprint);
        if (outward == Vector2Int.zero)
        {
            Vector2Int footprintCenter = new((parking.Min.x + parking.Max.x) / 2, (parking.Min.y + parking.Max.y) / 2);
            outward = GetDominantCardinalDirection(access - footprintCenter);
        }

        if (outward == Vector2Int.zero)
        {
            outward = Vector2Int.left;
        }

        Vector2Int side = new(-outward.y, outward.x);
        Vector2Int[] candidates =
        {
            access + GetParkingWaitSlotOffset(parkingSlotIndex, outward, side),
            access,
            access + side,
            access - side,
            access + outward,
            access + outward + side,
            access + outward - side
        };

        for (int i = 0; i < candidates.Length; i++)
        {
            if (IsDriverSafeWalkCell(candidates[i]))
            {
                return candidates[i];
            }
        }

        if (TryFindNearestDriverSafeWalkCell(access, out Vector2Int fallbackCell))
        {
            return fallbackCell;
        }

        return access;
    }

    private static Vector2Int GetParkingWaitSlotOffset(int parkingSlotIndex, Vector2Int outward, Vector2Int side)
    {
        return (Mathf.Abs(parkingSlotIndex) % 5) switch
        {
            1 => side,
            2 => -side,
            3 => outward,
            4 => outward + side,
            _ => Vector2Int.zero
        };
    }

    private static Vector2Int GetDominantCardinalDirection(Vector2Int delta)
    {
        if (delta == Vector2Int.zero)
        {
            return Vector2Int.zero;
        }

        return Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
            ? new Vector2Int(delta.x >= 0 ? 1 : -1, 0)
            : new Vector2Int(0, delta.y >= 0 ? 1 : -1);
    }

    private TruckAgent GetAssignedTruckForDriver(DriverAgent driver)
    {
        if (driver == null || driver.AssignedTruckNumber <= 0)
        {
            return null;
        }

        return GetTruckAgent(driver.AssignedTruckNumber);
    }

    private bool TryReserveAvailableTruckForDriver(DriverAgent driver, out TruckAgent truckAgent, string reason)
    {
        truckAgent = null;
        if (driver == null)
        {
            return false;
        }

        TruckAgent current = GetAssignedTruckForDriver(driver);
        if (IsTruckAvailableForDriver(current, driver))
        {
            truckAgent = current;
            EnsureDriverInTruckRoster(truckAgent, driver);
            return true;
        }

        if (current != null && current.Driver != driver && !driver.IsOnActiveShift && !IsDriverOnActiveTradeRun(driver))
        {
            RemoveDriverFromTruckRoster(current, driver);
        }

        for (int i = 0; i < truckAgents.Count; i++)
        {
            TruckAgent candidate = truckAgents[i];
            if (!IsTruckAvailableForDriver(candidate, driver))
            {
                continue;
            }

            EnsureDriverInTruckRoster(candidate, driver);
            truckAgent = candidate;
            SessionDebugLogger.Log("TRUCK_POOL", $"{driver.DriverName} reserved {candidate.DisplayName} automatically for {reason}.");
            return true;
        }

        if (TryProvisionTruckFromParkingCapacity(out TruckAgent provisionedTruck, reason) &&
            IsTruckAvailableForDriver(provisionedTruck, driver))
        {
            EnsureDriverInTruckRoster(provisionedTruck, driver);
            truckAgent = provisionedTruck;
            SessionDebugLogger.Log("TRUCK_POOL", $"{driver.DriverName} reserved {provisionedTruck.DisplayName} automatically for {reason}.");
            return true;
        }

        SessionDebugLogger.Log("TRUCK_POOL", $"{driver.DriverName} could not reserve a truck for {reason}: no available parked trucks.");
        return false;
    }

    private bool HasAvailableTruckInParking()
    {
        for (int i = 0; i < truckAgents.Count; i++)
        {
            TruckAgent truckAgent = truckAgents[i];
            if (IsTruckOperationallyAvailable(truckAgent) &&
                truckAgent.Driver == null &&
                truckAgent.AssignedDrivers.Count < 2)
            {
                return true;
            }
        }

        return CanProvisionTruckFromParkingCapacity();
    }

    private bool IsTruckAvailableForDriver(TruckAgent truckAgent, DriverAgent driver)
    {
        if (truckAgent == null || driver == null || truckAgent.TruckObject == null)
        {
            return false;
        }

        if (truckAgent.Driver != null && truckAgent.Driver != driver)
        {
            return false;
        }

        if (!truckAgent.AssignedDrivers.Contains(driver) && truckAgent.AssignedDrivers.Count >= 2)
        {
            return false;
        }

        return IsTruckOperationallyAvailable(truckAgent);
    }

    private bool IsTruckOperationallyAvailable(TruckAgent truckAgent)
    {
        if (truckAgent == null || truckAgent.TruckObject == null)
        {
            return false;
        }

        if (truckAgent.IsPurchaseArrivalActive ||
            truckAgent.IsTruckMoving ||
            truckAgent.IsTruckInteracting ||
            truckAgent.IsDriverRescueActive ||
            truckAgent.CurrentAssignedTrip != TripType.None ||
            truckAgent.CurrentRefuelPhase != RefuelPhase.None ||
            IsTruckOnActiveTradeRun(truckAgent))
        {
            return false;
        }

        return IsTruckInsideParking(truckAgent);
    }

    private static void EnsureDriverInTruckRoster(TruckAgent truckAgent, DriverAgent driver)
    {
        if (truckAgent == null || driver == null)
        {
            return;
        }

        if (!truckAgent.AssignedDrivers.Contains(driver))
        {
            truckAgent.AssignedDrivers.Add(driver);
        }

        driver.AssignedTruckNumber = truckAgent.TruckNumber;
    }

    private TruckAgent GetCurrentTruckForDriver(DriverAgent driver)
    {
        foreach (TruckAgent truckAgent in truckAgents)
        {
            if (truckAgent.Driver == driver)
            {
                return truckAgent;
            }
        }

        return null;
    }

    private bool IsDriverAssignedToTruck(TruckAgent truckAgent, DriverAgent driver)
    {
        return truckAgent != null && driver != null && truckAgent.AssignedDrivers.Contains(driver);
    }

    private bool AssignDriverToTruckRoster(TruckAgent truckAgent, DriverAgent driver)
    {
        if (truckAgent == null || driver == null)
        {
            return false;
        }

        if (driver.AssignedTruckNumber > 0 && driver.AssignedTruckNumber != truckAgent.TruckNumber)
        {
            return false;
        }

        if (truckAgent.AssignedDrivers.Contains(driver))
        {
            return true;
        }

        if (truckAgent.AssignedDrivers.Count >= 2)
        {
            return false;
        }

        truckAgent.AssignedDrivers.Add(driver);
        driver.AssignedTruckNumber = truckAgent.TruckNumber;
        return true;
    }

    private bool RemoveDriverFromTruckRoster(TruckAgent truckAgent, DriverAgent driver)
    {
        if (truckAgent == null || driver == null)
        {
            return false;
        }

        if (!truckAgent.AssignedDrivers.Remove(driver))
        {
            return false;
        }

        driver.AssignedTruckNumber = 0;
        return true;
    }

    private string GetTruckAssignedDriverSummary(TruckAgent truckAgent)
    {
        if (truckAgent == null || truckAgent.AssignedDrivers.Count == 0)
        {
            return "None";
        }

        if (truckAgent.AssignedDrivers.Count == 1)
        {
            return truckAgent.AssignedDrivers[0].DriverName;
        }

        return $"{truckAgent.AssignedDrivers[0].DriverName} +{truckAgent.AssignedDrivers.Count - 1}";
    }

    private string GetTruckDisplayName(int truckNumber)
    {
        return $"Truck #{truckNumber}";
    }

    private bool IsTruckNumberOwned(int truckNumber)
    {
        foreach (TruckAgent truckAgent in truckAgents)
        {
            if (truckAgent.TruckNumber == truckNumber)
            {
                return true;
            }
        }

        return false;
    }

    private TruckAgent GetTruckAgent(int truckNumber)
    {
        foreach (TruckAgent truckAgent in truckAgents)
        {
            if (truckAgent.TruckNumber == truckNumber)
            {
                return truckAgent;
            }
        }

        return null;
    }

    private string GetLoadedTruckDisplayName()
    {
        foreach (TruckAgent truckAgent in truckAgents)
        {
            if (truckAgent.TruckObject == truckObject)
            {
                return truckAgent.DisplayName;
            }
        }

        return truckObject != null ? truckObject.name : "Truck";
    }

    private bool IsTruckInsideParking()
    {
        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking) || truckObject == null)
        {
            return false;
        }

        Vector3 position = truckObject.transform.position;
        bool insideParkingBounds =
            position.x >= parking.Min.x &&
            position.x <= parking.Max.x + 1f &&
            position.z >= parking.Min.y &&
            position.z <= parking.Max.y + 1f;

        return insideParkingBounds && !isTruckMoving;
    }

    private bool IsTruckInsideParking(TruckAgent truckAgent)
    {
        if (truckAgent == null)
        {
            return false;
        }

        LoadTruckState(truckAgent);
        bool result = IsTruckInsideParking();
        SaveTruckState(truckAgent);
        return result;
    }
}

