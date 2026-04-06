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

        bool loading = interactionType == TruckInteractionType.LoadAtForest || interactionType == TruckInteractionType.LoadAtWarehouse;
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
        bool completedLoad = activeTruckInteraction == TruckInteractionType.LoadAtForest || activeTruckInteraction == TruckInteractionType.LoadAtWarehouse;

        switch (activeTruckInteraction)
        {
            case TruckInteractionType.LoadAtForest:
                locations[LocationType.Forest].WoodStored -= 1;
                truckCargoWood = 1;
                truckCargoSource = CargoSource.Forest;
                break;

            case TruckInteractionType.UnloadAtWarehouse:
                locations[LocationType.Warehouse].WoodStored += truckCargoWood;
                truckCargoWood = 0;
                truckCargoSource = CargoSource.None;
                break;

            case TruckInteractionType.LoadAtWarehouse:
                locations[LocationType.Warehouse].WoodStored -= 1;
                truckCargoWood = 1;
                truckCargoSource = CargoSource.Warehouse;
                break;

            case TruckInteractionType.UnloadAtTown:
                locations[LocationType.Town].WoodStored += truckCargoWood;
                truckCargoWood = 0;
                truckCargoSource = CargoSource.None;
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
            PlayTruckFx(uiPanelOpenClip, 0.55f);
        }
        else
        {
            PlayTruckFx(completedLoad ? cargoDropClip : cargoPickupClip, 0.55f);
        }
    }
}
