using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void UpdateLogisticsTabUi(DriverAgent selectedDriver)
    {
        bool forestTutorialActive = false;
        bool ru = IsRussianLanguage();

        // For Warehouse: only show the card once (slot index 3 controls Root visibility)
        bool warehouseCardShown = false;

        for (int i = 0; i < logisticsSlots.Length; i++)
        {
            LogisticsSlotUi slot = logisticsSlots[i];
            if (slot == null) continue;

            bool isWarehouse = slot.BuildingType == LocationType.Warehouse;

            // Root visibility: only toggle for single-building slots and the first warehouse slot
            if (slot.Root != null && (!isWarehouse || !warehouseCardShown))
            {
                bool isBuilt = locations.ContainsKey(slot.BuildingType);
                bool visible = isBuilt && (!forestTutorialActive || slot.BuildingType == LocationType.Forest);
                slot.Root.gameObject.SetActive(visible);
                if (isWarehouse) warehouseCardShown = true;
                if (!visible) continue;
            }
            else if (slot.Root != null && !slot.Root.gameObject.activeSelf)
            {
                continue;
            }

            DriverAgent assigned = GetNthLogisticsWorker(slot.BuildingType, slot.SlotIndex, slot.LocationInstanceId);

            if (slot.BuildingNameText != null)
            {
                slot.BuildingNameText.text = L(GetBuildingWorkerSlotTitle(slot.BuildingType, slot.SlotIndex));
            }

            if (slot.WorkHoursText != null)
            {
                slot.WorkHoursText.text = GetBuildingWorkerWorkRangeLabel(slot.BuildingType, slot.SlotIndex);
                slot.WorkHoursText.color = IsBuildingWorkerWorkHour(slot.BuildingType, slot.SlotIndex, GetCurrentHour()) ? FleetAccentColor : FleetSecondaryTextColor;
            }

            if (isWarehouse)
            {
                // Compact row: "Loader N: Name — hours" or "Loader N: —"
                string loaderLabel = ru ? $"Кладовщик {slot.SlotIndex + 1}: " : $"Loader {slot.SlotIndex + 1}: ";
                slot.AssignedWorkerText.text = assigned != null
                    ? $"{loaderLabel}{assigned.DriverName}"
                    : $"{loaderLabel}—";
                slot.AssignedWorkerText.color = assigned != null ? FleetAccentColor : FleetSecondaryTextColor;
            }
            else
            {
                string professionLabel = L(GetBuildingWorkerRoleLabel(slot.BuildingType));
                string workerLabel = assigned != null
                    ? (ru ? $"Назначен: {assigned.DriverName}" : $"Assigned: {assigned.DriverName}")
                    : ru ? $"{professionLabel} не назначен" : $"{professionLabel} not assigned";
                slot.AssignedWorkerText.text = workerLabel;
                slot.AssignedWorkerText.color = assigned != null ? FleetAccentColor : FleetSecondaryTextColor;
            }

            bool selectedIsIdle = IsWorkerVacantForVacancyAssignment(selectedDriver);
            bool selectedAlreadyHere = selectedDriver != null &&
                IsDriverAssignedToBuildingSlot(selectedDriver, slot.BuildingType, slot.LocationInstanceId);
            string educationReason = string.Empty;
            bool meetsEducation = selectedDriver == null ||
                                  CanWorkerMeetBuildingEducationRequirement(selectedDriver, slot.BuildingType, out educationReason);
            bool canAssign = selectedIsIdle && assigned == null && !selectedAlreadyHere && meetsEducation;
            slot.AssignButton.interactable = canAssign;
            slot.AssignButtonText.text = selectedDriver == null
                ? (isWarehouse ? (ru ? "Выбери рабочего" : "Select worker") : L("Select a worker"))
                : assigned != null
                    ? (isWarehouse ? (ru ? "Занято" : "Occupied") : L("Slot occupied"))
                    : selectedAlreadyHere
                        ? (isWarehouse ? L("Assigned") : (ru ? "Уже назначен сюда" : "Already assigned here"))
                        : !selectedIsIdle
                            ? (isWarehouse ? (ru ? "Не свободен" : "Not idle") : (ru ? $"{selectedDriver.DriverName} не свободен" : $"{selectedDriver.DriverName} is not idle"))
                            : isWarehouse ? (ru ? "Назначить" : "Assign") : (ru ? $"Назначить: {selectedDriver.DriverName}" : $"Assign: {selectedDriver.DriverName}");

            if (selectedDriver != null && selectedIsIdle && assigned == null && !selectedAlreadyHere && !meetsEducation)
            {
                slot.AssignButtonText.text = educationReason;
            }

            slot.RemoveButton.interactable = assigned != null;
        }
    }

    private DriverAgent GetNthLogisticsWorker(LocationType buildingType, int slotIndex, int locationInstanceId = 0)
    {
        int count = 0;
        int resolvedInstanceId = ResolveBuildingInstanceId(buildingType, locationInstanceId);
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent d = driverAgents[i];
            if (d.DutyMode == DriverDutyMode.Logistics &&
                d.AssignedBuildingType == buildingType &&
                d.AssignedBuildingSlotIndex == slotIndex &&
                (locationInstanceId <= 0 || d.AssignedBuildingInstanceId == resolvedInstanceId))
            {
                return d;
            }

            if (d.DutyMode == DriverDutyMode.Logistics &&
                d.AssignedBuildingType == buildingType &&
                d.AssignedBuildingSlotIndex < 0 &&
                (locationInstanceId <= 0 || d.AssignedBuildingInstanceId == resolvedInstanceId))
            {
                if (count == slotIndex)
                {
                    return d;
                }

                count++;
            }
        }
        return null;
    }

    private int CountLogisticsWorkers(LocationType buildingType)
    {
        return CountLogisticsWorkers(buildingType, 0);
    }

    private int CountLogisticsWorkers(LocationType buildingType, int locationInstanceId)
    {
        int count = 0;
        int resolvedInstanceId = ResolveBuildingInstanceId(buildingType, locationInstanceId);
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent d = driverAgents[i];
            if (d.DutyMode == DriverDutyMode.Logistics &&
                d.AssignedBuildingType == buildingType &&
                (locationInstanceId <= 0 || d.AssignedBuildingInstanceId == resolvedInstanceId))
            {
                count++;
            }
        }

        return count;
    }

    private void AssignWorkerToBuilding(DriverAgent driver, LogisticsSlotUi slot)
    {
        if (driver == null || slot == null) return;
        if (driver.IsArrivingByBus) return;
        if (!CanWorkerMeetBuildingEducationRequirement(driver, slot.BuildingType, out string educationReason))
        {
            SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} building assignment to {slot.BuildingType} blocked: {educationReason}.");
            LogDriverReaction(driver, $"cannot start work at {slot.BuildingType}: {educationReason}");
            return;
        }

        if (IsDriverBusDriver(driver))
        {
            int busSlotIndex = GetBusDriverShiftSlotIndex(driver);
            if (busSlotIndex >= 0)
            {
                busDriverShiftIds[busSlotIndex] = 0;
            }
        }

        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (assignedTruck != null)
        {
            if (!UnassignDriverFromTruck(assignedTruck, driver))
            {
                SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} building assignment to {slot.BuildingType} blocked: could not unassign from {assignedTruck.DisplayName}.");
                LogDriverReaction(driver, $"cannot start work at {slot.BuildingType}: still assigned to {assignedTruck.DisplayName}");
                return;
            }

            SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} auto-unassigned from {assignedTruck.DisplayName} before building assignment to {slot.BuildingType}.");
        }

        // Remove from any existing building assignment first
        if (driver.DutyMode == DriverDutyMode.Logistics && driver.AssignedBuildingType.HasValue)
        {
            SetDriverDutyMode(driver, DriverDutyMode.Local);
        }

        SetDriverDutyMode(driver, DriverDutyMode.Logistics);
        driver.AssignedBuildingType = slot.BuildingType;
        driver.AssignedBuildingInstanceId = ResolveBuildingInstanceId(slot.BuildingType, slot.LocationInstanceId);
        driver.AssignedBuildingSlotIndex = slot.SlotIndex;
        driver.ShiftStartHour = GetBuildingWorkerShiftStartHour(slot.BuildingType, slot.SlotIndex);
        if (slot.BuildingType == LocationType.Forest)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.AssignLumberjackWorker);
            if (selectedGameStartMode == GameStartMode.Tutorial &&
                !isTutorialSkipped &&
                isTutorialGoalsActive &&
                tutorialGoalsMode == TutorialGoalsMode.LumberjackCamp)
            {
                isShiftsPanelOpen = false;
                if (shiftsScreenUi?.CanvasRoot != null)
                {
                    shiftsScreenUi.CanvasRoot.SetActive(false);
                }
                SessionDebugLogger.Log("TUTORIAL", "Closed Vacancies after Lumberjack Camp worker assignment.");
            }
        }
        else if (slot.BuildingType == LocationType.Warehouse)
        {
            NotifyTutorialWarehouseLoaderAssigned();
        }
        else
        {
            NotifyTutorialBuildingWorkerAssigned(slot.BuildingType);
        }

        string workKind = IsProductionLocation(slot.BuildingType) ? "production" : "service";
        string buildingName = GetBuildingInstanceDisplayName(slot.BuildingType, slot.LocationInstanceId);
        string workRange = GetBuildingWorkerWorkRangeLabel(slot.BuildingType, slot.SlotIndex);
        LogUiInput($"Shifts Canvas: assigned {driver.DriverName} to {buildingName} ({workRange})");
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} assigned to {slot.BuildingType}#{driver.AssignedBuildingInstanceId} slot={slot.SlotIndex} {workKind} work ({workRange}).");
        PushFeedEvent(
            $"{driver.DriverName} assigned to {buildingName}.",
            $"{driver.DriverName} \u043d\u0430\u0437\u043d\u0430\u0447\u0435\u043d \u0432 {GetSelectedLocationDisplayName(slot.BuildingType)}.",
            FeedEventType.Info);
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void RemoveWorkerFromBuilding(LogisticsSlotUi slot)
    {
        if (slot == null) return;
        DriverAgent assigned = GetNthLogisticsWorker(slot.BuildingType, slot.SlotIndex, slot.LocationInstanceId);
        if (assigned == null) return;
        LogUiInput($"Shifts Canvas: removed {assigned.DriverName} from {slot.BuildingType}");
        SetDriverDutyMode(assigned, DriverDutyMode.Local);
        ClearWorkerContract(assigned, $"removed from {slot.BuildingType}");
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }


}
