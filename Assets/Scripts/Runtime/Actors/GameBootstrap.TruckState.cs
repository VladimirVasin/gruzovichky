using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void LoadTruckState(TruckAgent truckAgent)
    {
        truckObject = truckAgent.TruckObject;
        truckVisualRoot = truckAgent.TruckVisualRoot;
        truckBodyTransform = truckAgent.TruckBodyTransform;
        truckCabinTransform = truckAgent.TruckCabinTransform;
        truckHeadlightLeftRenderer = truckAgent.TruckHeadlightLeftRenderer;
        truckHeadlightRightRenderer = truckAgent.TruckHeadlightRightRenderer;
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
        return parkingSlots[slotIndex];
    }

    private Vector3 GetDriverIdleMotelPosition(int driverIndex)
    {
        Vector3 center = GetLocationCenter(LocationType.Motel);
        int ringIndex = Mathf.Max(0, driverIndex);
        Vector3[] offsets =
        {
            new(-0.35f, 0f, -0.25f),
            new(0.35f, 0f, -0.25f),
            new(-0.35f, 0f, 0.25f),
            new(0.35f, 0f, 0.25f),
            new(0f, 0f, -0.45f),
            new(0f, 0f, 0.45f)
        };

        Vector3 position = center + offsets[ringIndex % offsets.Length];
        position.y = SampleTerrainHeight(position.x, position.z);
        return position;
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
