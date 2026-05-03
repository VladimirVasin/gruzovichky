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

            DriverAgent assigned = GetNthLogisticsWorker(slot.BuildingType, slot.SlotIndex);

            if (slot.BuildingNameText != null)
            {
                slot.BuildingNameText.text = L(GetSelectedLocationDisplayName(slot.BuildingType));
            }

            if (slot.WorkHoursText != null)
            {
                slot.WorkHoursText.text = GetProductionWorkRangeLabel();
                slot.WorkHoursText.color = IsProductionWorkHour(GetCurrentHour()) ? FleetAccentColor : FleetSecondaryTextColor;
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
                string professionLabel = slot.BuildingType switch
                {
                    LocationType.Forest           => L("Lumberjack"),
                    LocationType.Sawmill          => L("Sawmill Worker"),
                    LocationType.FurnitureFactory => L("Carpenter"),
                    _                             => L("Worker"),
                };
                string workerLabel = assigned != null
                    ? (ru ? $"Назначен: {assigned.DriverName}" : $"Assigned: {assigned.DriverName}")
                    : ru ? $"{professionLabel} не назначен" : $"{professionLabel} not assigned";
                slot.AssignedWorkerText.text = workerLabel;
                slot.AssignedWorkerText.color = assigned != null ? FleetAccentColor : FleetSecondaryTextColor;
            }

            bool selectedIsIdle = selectedDriver != null && !selectedDriver.IsArrivingByBus;
            bool selectedAlreadyHere = selectedDriver != null &&
                selectedDriver.DutyMode == DriverDutyMode.Logistics &&
                selectedDriver.AssignedBuildingType == slot.BuildingType;
            bool canAssign = selectedIsIdle && assigned == null && !selectedAlreadyHere;
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

            slot.RemoveButton.interactable = assigned != null;
        }
    }

    private DriverAgent GetNthLogisticsWorker(LocationType buildingType, int slotIndex)
    {
        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent d = driverAgents[i];
            if (d.DutyMode == DriverDutyMode.Logistics && d.AssignedBuildingType == buildingType)
            {
                if (count == slotIndex) return d;
                count++;
            }
        }
        return null;
    }

    private int CountLogisticsWorkers(LocationType buildingType)
    {
        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent d = driverAgents[i];
            if (d.DutyMode == DriverDutyMode.Logistics && d.AssignedBuildingType == buildingType)
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
                SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} production assignment to {slot.BuildingType} blocked: could not unassign from {assignedTruck.DisplayName}.");
                LogDriverReaction(driver, $"cannot start production at {slot.BuildingType}: still assigned to {assignedTruck.DisplayName}");
                return;
            }

            SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} auto-unassigned from {assignedTruck.DisplayName} before production assignment to {slot.BuildingType}.");
        }

        // Remove from any existing building assignment first
        if (driver.DutyMode == DriverDutyMode.Logistics && driver.AssignedBuildingType.HasValue)
        {
            SetDriverDutyMode(driver, DriverDutyMode.Local);
        }

        SetDriverDutyMode(driver, DriverDutyMode.Logistics);
        driver.AssignedBuildingType = slot.BuildingType;
        driver.ShiftStartHour = -1;
        if (slot.BuildingType == LocationType.Forest)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.AssignLumberjackWorker);
            if (selectedGameStartMode == GameStartMode.User &&
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

        LogUiInput($"Shifts Canvas: assigned {driver.DriverName} to {slot.BuildingType} ({GetProductionWorkRangeLabel()})");
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} assigned to {slot.BuildingType} production work ({GetProductionWorkRangeLabel()}).");
        PushFeedEvent(
            $"{driver.DriverName} assigned to {GetSelectedLocationDisplayName(slot.BuildingType)}.",
            $"{driver.DriverName} назначен в {GetSelectedLocationDisplayName(slot.BuildingType)}.",
            FeedEventType.Info);
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void RemoveWorkerFromBuilding(LogisticsSlotUi slot)
    {
        if (slot == null) return;
        DriverAgent assigned = GetNthLogisticsWorker(slot.BuildingType, slot.SlotIndex);
        if (assigned == null) return;
        LogUiInput($"Shifts Canvas: removed {assigned.DriverName} from {slot.BuildingType}");
        SetDriverDutyMode(assigned, DriverDutyMode.Local);
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }


}
