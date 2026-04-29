using UnityEngine;

public partial class GameBootstrap
{
    private void FocusCameraOnAssignedTutorialTruck()
    {
        TruckAgent truck = null;
        foreach (TruckAgent candidate in truckAgents)
        {
            if (candidate != null && candidate.AssignedDrivers.Count > 0 && candidate.TruckObject != null)
            {
                truck = candidate;
                break;
            }
        }

        if (truck == null)
        {
            return;
        }

        selectedTruckNumber = truck.TruckNumber;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        isTruckDetailsOpen = false;
        isTruckCameraFocused = false;
        isCameraReturningToDiorama = false;
        isCameraRotatingToTarget = false;
        tutorialCameraFollowTruck = truck;
        tutorialCameraFocusTarget = new Vector3(truck.TruckObject.transform.position.x, 0f, truck.TruckObject.transform.position.z);
        tutorialCameraFocusOffset = new Vector3(-13f, 18f, -13f);
        isTutorialCameraFocusActive = true;
        RefreshSelectionVisuals();
        SessionDebugLogger.Log("TUTORIAL", $"Freight tutorial camera focus started for {truck.DisplayName}.");
    }

    private void FocusCameraOnActiveTradeTutorialTruck()
    {
        TruckAgent truck = null;
        if (HasActiveTradeRun())
        {
            truck = GetTruckAgent(activeTradeRun.TruckNumber);
        }

        if (truck == null)
        {
            DriverAgent intercityDriver = GetIntercityAssignedDriver();
            truck = GetAssignedTruckForDriver(intercityDriver);
        }

        if (truck?.TruckObject == null)
        {
            SessionDebugLogger.Log("TUTORIAL", "Trade race tutorial could not focus truck: no active/assigned truck.");
            return;
        }

        selectedTruckNumber = 0;
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        isTruckDetailsOpen = false;
        isTruckCameraFocused = false;
        isCameraReturningToDiorama = false;
        isCameraRotatingToTarget = false;
        tutorialCameraFollowTruck = truck;
        tutorialCameraFocusTarget = new Vector3(truck.TruckObject.transform.position.x, 0f, truck.TruckObject.transform.position.z);
        tutorialCameraFocusOffset = new Vector3(-13f, 18f, -13f);
        isTutorialCameraFocusActive = true;
        RefreshSelectionVisuals();
        SessionDebugLogger.Log("TUTORIAL", $"Trade race tutorial camera focus started for {truck.DisplayName}.");
    }
}
