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
        truckFxAudioSource = truckAgent.TruckFxAudioSource;
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
        currentTripPhase = truckAgent.CurrentTripPhase;
        currentRefuelPhase = truckAgent.CurrentRefuelPhase;
        activeTruckInteraction = truckAgent.ActiveTruckInteraction;
        queuedTruckInteraction = truckAgent.QueuedTruckInteraction;
        truckInteractionTargetRotation = truckAgent.TruckInteractionTargetRotation;
        truckInteractionBuildingPoint = truckAgent.TruckInteractionBuildingPoint;
        activeServiceLocation = truckAgent.ActiveServiceLocation;
        queuedServiceLocation = truckAgent.QueuedServiceLocation;
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
        truckAgent.TruckFxAudioSource = truckFxAudioSource;
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
        truckAgent.CurrentTripPhase = currentTripPhase;
        truckAgent.CurrentRefuelPhase = currentRefuelPhase;
        truckAgent.ActiveTruckInteraction = activeTruckInteraction;
        truckAgent.QueuedTruckInteraction = queuedTruckInteraction;
        truckAgent.TruckInteractionTargetRotation = truckInteractionTargetRotation;
        truckAgent.TruckInteractionBuildingPoint = truckInteractionBuildingPoint;
        truckAgent.ActiveServiceLocation = activeServiceLocation;
        truckAgent.QueuedServiceLocation = queuedServiceLocation;
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

    private Vector3 GetDriverIdleMotelPosition(int driverIndex)
    {
        Vector3 position = GetDriverIdleMotelWanderPosition(driverIndex, driverIndex);
        for (int attempt = 0; attempt < 28; attempt++)
        {
            Vector3 candidate = GetDriverIdleMotelWanderPosition(driverIndex, driverIndex + attempt * 3);
            if (!WouldIdleDriverOverlapAtPosition(null, candidate))
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
            if (!IsInsideGrid(cell) || roadCells.Contains(cell) || edgeHighwayCells.Contains(cell) || IsLocationCell(cell))
            {
                continue;
            }

            return position;
        }

        frontageBase.y = SampleTerrainHeight(frontageBase.x, frontageBase.z);
        return frontageBase;
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
            if (!IsInsideGrid(cell) || roadCells.Contains(cell) || edgeHighwayCells.Contains(cell) || IsLocationCell(cell))
            {
                continue;
            }

            position.y = SampleTerrainHeight(position.x, position.z);
            return position;
        }

        center.y = SampleTerrainHeight(center.x, center.z);
        return center;
    }

    private void MoveStarterIdleWorkersToMotel()
    {
        if (!locations.ContainsKey(LocationType.Motel))
        {
            return;
        }

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver?.DriverObject == null || driver.IsArrivingByBus || driver.IsOnActiveShift || IsDriverBusyWalkPhase(driver))
            {
                continue;
            }

            Vector3 current = driver.DriverObject.transform.position;
            driver.MotelIdlePosition = GetDriverIdleMotelPosition(i);
            driver.WalkTargetWorld = driver.MotelIdlePosition;
            driver.WalkPhase = DriverRescuePhase.IdleWander;
            driver.IdleWanderPauseTimer = 0f;
            driver.IdleWanderPointIndex = -1;
            BuildDriverWalkPath(driver, current, driver.MotelIdlePosition);
        }

        SessionDebugLogger.Log("DRIVER", "Starter idle workers moved from Intercity Stop fallback to newly built Motel idle area.");
    }

    private Vector3 GetDriverParkingWaitPosition(TruckAgent truckAgent)
    {
        Vector3 truckPosition = truckAgent != null ? GetParkingSlotWorldPosition(truckAgent.ParkingSlotIndex) : GetLocationCenter(LocationType.Parking);
        Vector3 waitPosition = truckPosition + new Vector3(-0.42f, 0f, -0.34f);
        waitPosition.y = SampleTerrainHeight(waitPosition.x, waitPosition.z);
        return waitPosition;
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

