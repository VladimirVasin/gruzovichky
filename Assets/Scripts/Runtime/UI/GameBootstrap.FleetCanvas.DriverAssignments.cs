using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void ToggleFleetDriverAssignmentPicker()
    {
        if (fleetScreenUi?.AssignDriverPickerPanel == null)
        {
            return;
        }

        bool nextState = !fleetScreenUi.AssignDriverPickerPanel.gameObject.activeSelf;
        if (nextState && fleetAssignDriverTargetSlot < 0)
        {
            return;
        }

        fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(nextState);
        UpdateFleetDriverPickerLayout(nextState, true);

        TruckAgent selectedTruck = GetFleetSelectedTruck();
        if (selectedTruck != null)
        {
            LogUiInput($"Fleet Canvas: {(nextState ? "opened" : "closed")} driver picker for {selectedTruck.DisplayName}");
        }

        if (!nextState)
        {
            fleetAssignDriverTargetSlot = -1;
        }

        isFleetScreenDirty = true;
        UpdateFleetDriverAssignmentPicker(selectedTruck);
        PlayUiSound(nextState ? uiPanelOpenClip : uiPanelCloseClip, 0.78f);
    }

    private void UpdateFleetDriverAssignmentPicker(TruckAgent selectedTruck)
    {
        if (fleetScreenUi?.AssignDriverPickerPanel == null)
        {
            return;
        }

        if (selectedTruck == null)
        {
            fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(false);
            UpdateFleetDriverPickerLayout(false, false);
            fleetAssignDriverTargetSlot = -1;
            return;
        }

        if (fleetAssignDriverTargetSlot < 0 || fleetAssignDriverTargetSlot > 1 || fleetAssignDriverTargetSlot < selectedTruck.AssignedDrivers.Count)
        {
            fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(false);
            UpdateFleetDriverPickerLayout(false, false);
            fleetAssignDriverTargetSlot = -1;
            return;
        }

        List<DriverAgent> candidates = GetDriverAssignmentCandidates(selectedTruck);
        bool hasCandidates = candidates.Count > 0;
        fleetScreenUi.AssignDriverPickerTitleText.text = IsRussianLanguage()
            ? $"Выберите водителя для слота {fleetAssignDriverTargetSlot + 1}"
            : $"Select Driver for Slot {fleetAssignDriverTargetSlot + 1}";
        fleetScreenUi.AssignDriverPickerEmptyText.gameObject.SetActive(!hasCandidates);
        fleetScreenUi.AssignDriverPickerEmptyText.text = IsRussianLanguage()
            ? "Нет доступных водителей."
            : "No available drivers.";
        for (int i = 0; i < fleetScreenUi.DriverPickerButtons.Count; i++)
        {
            bool active = i < candidates.Count;
            fleetScreenUi.DriverPickerButtons[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            DriverAgent driver = candidates[i];
            TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
            fleetScreenUi.DriverPickerButtonTexts[i].text = assignedTruck == null
                ? driver.DriverName
                : $"{driver.DriverName}  ({assignedTruck.DisplayName})";
        }

        fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(fleetScreenUi.AssignDriverPickerPanel.gameObject.activeSelf);
        UpdateFleetDriverPickerLayout(fleetScreenUi.AssignDriverPickerPanel.gameObject.activeSelf, hasCandidates);
    }

    private void UpdateFleetDriverPickerLayout(bool isVisible, bool hasCandidates)
    {
        if (fleetScreenUi?.AssignDriverPickerPanel == null)
        {
            return;
        }

        LayoutElement pickerLayout = fleetScreenUi.AssignDriverPickerPanel.GetComponent<LayoutElement>();
        if (pickerLayout != null)
        {
            pickerLayout.preferredHeight = isVisible ? (hasCandidates ? 128f : 72f) : 0f;
        }
    }

    private List<DriverAgent> GetDriverAssignmentCandidates(TruckAgent selectedTruck)
    {
        List<DriverAgent> candidates = new();
        if (selectedTruck == null || selectedTruck.AssignedDrivers.Count >= 2)
        {
            return candidates;
        }

        foreach (DriverAgent driver in driverAgents)
        {
            if (!CanAssignDriverToTruckRoster(selectedTruck, driver))
            {
                continue;
            }

            candidates.Add(driver);
        }

        return candidates;
    }

    private bool CanAssignDriverToTruckRoster(TruckAgent targetTruck, DriverAgent driver)
    {
        if (targetTruck == null || driver == null)
        {
            return false;
        }

        if (targetTruck.AssignedDrivers.Count >= 2)
        {
            return false;
        }

        if (driver.DutyMode == DriverDutyMode.Logistics || driver.AssignedBuildingType.HasValue)
        {
            return false;
        }

        if (IsDriverBusDriver(driver))
        {
            return false;
        }

        if (driver.AssignedTruckNumber > 0)
        {
            return false;
        }

        if (driver.IsArrivingByBus || IsDriverOnActiveTradeRun(driver))
        {
            return false;
        }

        if (driver.RestPhase != DriverRestPhase.None || IsDriverBusyWalkPhase(driver))
        {
            return false;
        }

        return driver.DutyMode == DriverDutyMode.Local || driver.DutyMode == DriverDutyMode.Intercity;
    }

    private void OnFleetDriverOptionPressed(int optionIndex)
    {
        TruckAgent selectedTruck = GetFleetSelectedTruck();
        if (selectedTruck == null)
        {
            return;
        }

        List<DriverAgent> candidates = GetDriverAssignmentCandidates(selectedTruck);
        if (optionIndex < 0 || optionIndex >= candidates.Count)
        {
            return;
        }

        if (AssignDriverToTruck(selectedTruck, candidates[optionIndex]))
        {
            LogUiInput($"Fleet Canvas: picked {candidates[optionIndex].DriverName} for {selectedTruck.DisplayName}");
            fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(false);
            UpdateFleetDriverPickerLayout(false, false);
            fleetAssignDriverTargetSlot = -1;
            isFleetScreenDirty = true;
        }
    }

    private bool AssignDriverToTruck(TruckAgent targetTruck, DriverAgent driver)
    {
        LogCommand($"AssignDriver({targetTruck?.DisplayName ?? "null"}, {driver?.DriverName ?? "null"})");
        if (targetTruck == null || driver == null)
        {
            if (targetTruck != null)
            {
                LogTruckReaction(targetTruck, "driver assignment rejected: invalid assignment target");
            }
            return false;
        }

        if (driver.AssignedTruckNumber > 0 && driver.AssignedTruckNumber != targetTruck.TruckNumber)
        {
            LogTruckReaction(targetTruck, $"driver assignment rejected for {driver.DriverName}: already assigned to another truck");
            return false;
        }

        if (!CanAssignDriverToTruckRoster(targetTruck, driver))
        {
            LogTruckReaction(targetTruck, $"driver assignment rejected for {driver.DriverName}: driver is not free");
            return false;
        }

        if (!AssignDriverToTruckRoster(targetTruck, driver))
        {
            LogTruckReaction(targetTruck, $"driver assignment rejected for {driver.DriverName}");
            return false;
        }

        SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} added to {targetTruck.DisplayName} roster.");
        LogDriverReaction(driver, $"assigned to {targetTruck.DisplayName}");
        LogTruckReaction(targetTruck, $"added roster driver {driver.DriverName}");
        PushFeedEvent(
            $"{driver.DriverName} assigned to {targetTruck.DisplayName}.",
            $"{driver.DriverName} назначен в {targetTruck.DisplayName}.",
            FeedEventType.Info);
        PlayUiSound(uiSelectClip, 0.88f);
        return true;
    }

    private bool CanUnassignDriverFromTruck(TruckAgent targetTruck, DriverAgent driver)
    {
        if (targetTruck == null || driver == null)
        {
            return false;
        }

        if (targetTruck.Driver != driver)
        {
            return true;
        }

        return !targetTruck.IsTruckMoving &&
               !targetTruck.IsTruckInteracting &&
               !targetTruck.IsDriverRescueActive &&
               targetTruck.CurrentAssignedTrip == TripType.None &&
               targetTruck.CurrentRefuelPhase == RefuelPhase.None &&
               IsTruckInsideParking(targetTruck);
    }

    private bool UnassignDriverFromTruck(TruckAgent targetTruck, DriverAgent driver)
    {
        LogCommand($"UnassignDriver({targetTruck?.DisplayName ?? "null"}, {driver?.DriverName ?? "null"})");
        if (!CanUnassignDriverFromTruck(targetTruck, driver))
        {
            if (targetTruck != null && driver != null)
            {
                LogTruckReaction(targetTruck, $"driver unassign rejected for {driver.DriverName}: truck is currently using that driver");
            }
            return false;
        }

        if (!RemoveDriverFromTruckRoster(targetTruck, driver))
        {
            if (targetTruck != null && driver != null)
            {
                LogTruckReaction(targetTruck, $"driver unassign rejected for {driver.DriverName}");
            }
            return false;
        }

        if (targetTruck.Driver == driver)
        {
            targetTruck.Driver = null;
            driver.IsOnActiveShift = false;
            driver.WaitingForShiftAtParking = false;
            driver.NeedsShiftEndReturn = false;
            driver.RestPhase = DriverRestPhase.None;
            driver.WalkPhase = DriverRescuePhase.None;
            driver.WalkPath.Clear();
            driver.WalkWaypointIndex = 0;
            driver.WalkAnimationTime = 0f;
            if (driver.DriverObject != null)
            {
                driver.DriverObject.SetActive(true);
                driver.DriverObject.transform.position = driver.MotelIdlePosition;
                driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            }
        }

        fleetAssignDriverTargetSlot = -1;
        if (fleetScreenUi?.AssignDriverPickerPanel != null)
        {
            fleetScreenUi.AssignDriverPickerPanel.gameObject.SetActive(false);
        }

        SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} removed from {targetTruck.DisplayName} roster.");
        LogDriverReaction(driver, $"unassigned from {targetTruck.DisplayName} and returned to free pool");
        LogTruckReaction(targetTruck, $"removed roster driver {driver.DriverName}");
        isFleetScreenDirty = true;
        PlayUiSound(uiSelectClip, 0.86f);
        return true;
    }

    private void OpenDriversPanelForDriver(int driverId)
    {
        isFleetPanelOpen = false;
        isDriversPanelOpen = true;
        isShiftsPanelOpen = false;
        isResourcesPanelOpen = false;
        isBuildPanelOpen = false;
        selectedShiftDriverId = driverId;
        PlayUiSound(uiPanelOpenClip, 0.86f);
    }


}
