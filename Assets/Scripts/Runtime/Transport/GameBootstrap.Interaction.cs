using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void UpdateTruckInteraction()
    {
        if (!isTruckInteracting)
        {
            if (cargoTransferCrate != null && cargoTransferCrate.activeSelf)
            {
                cargoTransferCrate.SetActive(false);
            }

            return;
        }

        truckObject.transform.rotation = Quaternion.Slerp(
            truckObject.transform.rotation,
            truckInteractionTargetRotation,
            7f * Time.deltaTime);

        truckInteractionTimer += Time.deltaTime;
        float progress = Mathf.Clamp01(truckInteractionTimer / TruckCargoInteractionDuration);
        UpdateCargoTransferVisual(progress);
        UpdateTruckVisuals(0f, false);

        if (truckInteractionTimer < TruckCargoInteractionDuration)
        {
            return;
        }

        CompleteTruckInteraction();
    }

    private bool TryStartTruckInteraction(TruckInteractionType interactionType, LocationType locationType)
    {
        if (interactionType == TruckInteractionType.None)
        {
            return false;
        }

        if (!TryAcquireServiceLocation(locationType))
        {
            isTruckWaitingForService = true;
            queuedTruckInteraction = interactionType;
            queuedServiceLocation = locationType;
            SessionDebugLogger.Log("SERVICE", $"{GetLoadedTruckDisplayName()} is waiting for service slot at {locationType}.");
            return false;
        }

        isTruckWaitingForService = false;
        queuedTruckInteraction = TruckInteractionType.None;
        queuedServiceLocation = null;
        SessionDebugLogger.Log("SERVICE", $"{GetLoadedTruckDisplayName()} acquired service slot at {locationType} for {interactionType}.");
        StartTruckInteraction(interactionType, locationType);
        return true;
    }

    private bool TryResumeQueuedTruckInteraction()
    {
        if (!isTruckWaitingForService || !queuedServiceLocation.HasValue || queuedTruckInteraction == TruckInteractionType.None)
        {
            return false;
        }

        return TryStartTruckInteraction(queuedTruckInteraction, queuedServiceLocation.Value);
    }

    private bool TryAcquireServiceLocation(LocationType locationType)
    {
        return ServiceSlotCoordinator.TryAcquire(occupiedServiceLocations, locationType);
    }

    private void ReleaseServiceLocation(LocationType locationType)
    {
        ServiceSlotCoordinator.Release(occupiedServiceLocations, locationType);
    }

    private void StartTruckInteraction(TruckInteractionType interactionType, LocationType locationType)
    {
        if (isTruckInteracting)
        {
            return;
        }

        isTruckMoving = false;
        isTruckInteracting = true;
        activeTruckInteraction = interactionType;
        activeServiceLocation = locationType;
        truckInteractionTimer = 0f;

        Vector3 buildingCenter = GetLocationCenter(locationType);
        Vector3 directionToBuilding = buildingCenter - truckObject.transform.position;
        directionToBuilding.y = 0f;
        if (directionToBuilding.sqrMagnitude < 0.0001f)
        {
            directionToBuilding = Vector3.forward;
        }

        truckInteractionTargetRotation = Quaternion.LookRotation(-directionToBuilding.normalized, Vector3.up);
        truckInteractionBuildingPoint = buildingCenter + directionToBuilding.normalized * -0.2f + Vector3.up * 0.3f;

        bool isCargoTransfer = interactionType != TruckInteractionType.RefuelAtGasStation;
        if (cargoTransferCrate != null)
        {
            cargoTransferCrate.SetActive(isCargoTransfer);
        }

        bool loading = interactionType == TruckInteractionType.LoadAtForest || interactionType == TruckInteractionType.LoadAtSawmill;
        if (isCargoTransfer)
        {
            PlayTruckFx(loading ? cargoPickupClip : cargoDropClip, 0.8f);
        }
        else
        {
            PlayTruckFx(truckIdleClip, 0.55f);
        }
    }

    private void CompleteTruckInteraction()
    {
        TruckInteractionType completedInteraction = activeTruckInteraction;
        bool completedLoad = activeTruckInteraction == TruckInteractionType.LoadAtForest || activeTruckInteraction == TruckInteractionType.LoadAtSawmill;

        switch (activeTruckInteraction)
        {
            case TruckInteractionType.LoadAtForest:
                locations[LocationType.Forest].LogsStored = Mathf.Max(0, locations[LocationType.Forest].LogsStored - 1);
                RefreshForestStoredLogsVisual();
                truckCargoAmount = 1;
                truckCargoType = CargoType.Logs;
                break;

            case TruckInteractionType.UnloadAtSawmill:
                locations[LocationType.Sawmill].LogsStored += truckCargoAmount;
                truckCargoAmount = 0;
                truckCargoType = CargoType.None;
                break;

            case TruckInteractionType.LoadAtSawmill:
                locations[LocationType.Sawmill].BoardsStored = Mathf.Max(0, locations[LocationType.Sawmill].BoardsStored - 1);
                truckCargoAmount = 1;
                truckCargoType = CargoType.Boards;
                break;

            case TruckInteractionType.UnloadAtWarehouse:
                locations[LocationType.Warehouse].BoardsStored += truckCargoAmount;
                truckCargoAmount = 0;
                truckCargoType = CargoType.None;
                break;

            case TruckInteractionType.RefuelAtGasStation:
                truckFuel = TruckFuelCapacity;
                break;
        }

        isTruckInteracting = false;
        activeTruckInteraction = TruckInteractionType.None;
        truckInteractionTimer = 0f;
        isTruckWaitingForService = false;
        queuedTruckInteraction = TruckInteractionType.None;
        queuedServiceLocation = null;
        if (cargoTransferCrate != null)
        {
            cargoTransferCrate.SetActive(false);
        }

        if (activeServiceLocation.HasValue)
        {
            ReleaseServiceLocation(activeServiceLocation.Value);
            activeServiceLocation = null;
        }

        if (completedInteraction == TruckInteractionType.RefuelAtGasStation)
        {
            PlayTruckFx(gasStationRefuelCueClip, 0.72f);
        }
        else
        {
            PlayTruckFx(completedLoad ? cargoDropClip : cargoPickupClip, 0.55f);
            switch (completedInteraction)
            {
                case TruckInteractionType.LoadAtForest:
                    PlayTruckFx(forestLoadCueClip, 0.68f);
                    break;
                case TruckInteractionType.UnloadAtSawmill:
                    PlayTruckFx(sawmillUnloadCueClip, 0.72f);
                    break;
                case TruckInteractionType.LoadAtSawmill:
                    PlayTruckFx(sawmillLoadCueClip, 0.68f);
                    break;
                case TruckInteractionType.UnloadAtWarehouse:
                    PlayTruckFx(warehouseUnloadBoardsCueClip, 0.78f);
                    break;
            }
        }
    }
}
