using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void AddWorkerOptionsForVacancy(VacancyViewModel vacancy)
    {
        bool ru = IsRussianLanguage();
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (!CanWorkerFillVacancy(vacancy, driver, out string reason))
            {
                continue;
            }

            vacancyFlowOptions.Add(new VacancyFlowOption
            {
                Kind = VacancyFlowOptionKind.Worker,
                Title = driver.DriverName,
                Subtitle = string.IsNullOrWhiteSpace(reason)
                    ? $"{L(GetWorkerOccupationLabel(driver))} · {GetWorkerEducationLabel(driver.Education)}"
                    : reason,
                Worker = driver
            });
        }

        if (vacancyFlowOptions.Count == 0)
        {
            vacancyFlowOptions.Add(new VacancyFlowOption
            {
                Kind = VacancyFlowOptionKind.Worker,
                Title = ru ? "Нет доступных рабочих" : "No available workers",
                Subtitle = ru ? "Освободи рабочего или дождись нового." : "Free someone up or hire another worker."
            });
        }
    }

    private bool IsVacancyOptionBlocked(VacancyViewModel vacancy, VacancyFlowOption option)
    {
        if (option == null)
        {
            return true;
        }

        if (option.Kind == VacancyFlowOptionKind.Worker && option.Worker == null)
        {
            return true;
        }

        if (vacancy == null)
        {
            return true;
        }

        if (option.Kind == VacancyFlowOptionKind.Shift && vacancy.Kind == VacancyKind.BusDriver)
        {
            return GetBusAssignedDriver(option.ShiftIndex) != null;
        }

        if (option.Kind == VacancyFlowOptionKind.Shift && vacancy.Kind == VacancyKind.TruckDriver)
        {
            return IsAnyTruckDriverAssignedToShift(option.ShiftIndex);
        }

        if (option.Kind == VacancyFlowOptionKind.Shift && IsGroupedWarehouseVacancy(vacancy))
        {
            return option.ShiftIndex < 0 ||
                   option.ShiftIndex >= WarehouseMaxWorkers ||
                   GetNthLogisticsWorker(LocationType.Warehouse, option.ShiftIndex) != null;
        }

        if (option.Kind == VacancyFlowOptionKind.Truck)
        {
            TruckAgent truck = GetTruckAgent(option.TruckNumber);
            if (truck == null)
            {
                return true;
            }

            if (vacancy.Kind == VacancyKind.Intercity)
            {
                return truck.AssignedDrivers.Count >= 2;
            }

            return IsTruckShiftOccupied(truck, selectedVacancyShiftIndex) || (truck.AssignedDrivers.Count >= 2 && !HasTruckRosterDriverWithoutShift(truck));
        }

        if (option.Kind == VacancyFlowOptionKind.BuyTruck)
        {
            return !locations.ContainsKey(LocationType.Parking) || GetOwnedTruckCount() >= MaxTruckCount || money < HireTruckCost;
        }

        return false;
    }

    private void OnVacancyFlowOptionPressed(int optionIndex)
    {
        if (selectedVacancyIndex < 0 || selectedVacancyIndex >= vacancyViewModels.Count)
        {
            return;
        }

        if (optionIndex < 0 || optionIndex >= vacancyFlowOptions.Count)
        {
            return;
        }

        VacancyViewModel vacancy = vacancyViewModels[selectedVacancyIndex];
        VacancyFlowOption option = vacancyFlowOptions[optionIndex];
        if (IsVacancyOptionBlocked(vacancy, option))
        {
            return;
        }

        switch (option.Kind)
        {
            case VacancyFlowOptionKind.Shift:
                selectedVacancyShiftIndex = option.ShiftIndex;
                selectedVacancyTruckNumber = 0;
                PlayUiSound(uiSelectClip, 0.8f);
                isShiftsScreenDirty = true;
                return;
            case VacancyFlowOptionKind.Truck:
                selectedVacancyTruckNumber = option.TruckNumber;
                PlayUiSound(uiSelectClip, 0.8f);
                isShiftsScreenDirty = true;
                return;
            case VacancyFlowOptionKind.Worker:
                AssignVacancyToWorker(vacancy, option.Worker);
                return;
            case VacancyFlowOptionKind.Remove:
                RemoveVacancyAssignment(vacancy);
                return;
            case VacancyFlowOptionKind.BuyTruck:
                HireNewTruck();
                isShiftsScreenDirty = true;
                return;
        }
    }

    private bool CanWorkerFillVacancy(VacancyViewModel vacancy, DriverAgent driver, out string reason)
    {
        bool ru = IsRussianLanguage();
        reason = string.Empty;
        if (vacancy == null || driver == null || driver.IsArrivingByBus || IsDriverBusyWalkPhase(driver))
        {
            reason = ru ? "недоступен" : "unavailable";
            return false;
        }

        if (!CanWorkerMeetEducationRequirement(driver, vacancy.RequiredEducation))
        {
            reason = ru ? "недостаточно образования" : "education too low";
            return false;
        }

        if (!IsWorkerVacantForVacancyAssignment(driver))
        {
            reason = ru ? "\u0437\u0430\u043d\u044f\u0442" : "already assigned";
            return false;
        }

        if (vacancy.Kind == VacancyKind.Production)
        {
            if (IsGroupedWarehouseVacancy(vacancy) &&
                (selectedVacancyShiftIndex < 0 ||
                 selectedVacancyShiftIndex >= WarehouseMaxWorkers ||
                 GetNthLogisticsWorker(LocationType.Warehouse, selectedVacancyShiftIndex) != null))
            {
                return false;
            }

            if (driver.DutyMode == DriverDutyMode.Logistics && driver.AssignedBuildingType == vacancy.BuildingType)
            {
                return false;
            }
            if (IsDriverOnActiveTradeRun(driver) || IsBusDriverOnActiveRoute(driver))
            {
                return false;
            }
            return true;
        }

        if (vacancy.Kind == VacancyKind.Intercity)
        {
            if (driver.DutyMode == DriverDutyMode.Logistics || IsDriverBusDriver(driver) || IsDriverOnActiveTradeRun(driver))
            {
                return false;
            }
            return driver.DutyMode == DriverDutyMode.Local;
        }

        if (vacancy.Kind == VacancyKind.BusDriver)
        {
            if (selectedVacancyShiftIndex < 0)
            {
                return false;
            }
            if (driver.AssignedTruckNumber > 0 || driver.DutyMode == DriverDutyMode.Logistics || IsDriverIntercity(driver) || IsDriverBusDriver(driver) || IsBusDriverOnActiveRoute(driver))
            {
                return false;
            }
            return true;
        }

        if (vacancy.Kind == VacancyKind.TruckDriver)
        {
            if (selectedVacancyShiftIndex < 0)
            {
                return false;
            }
            if (driver.DutyMode == DriverDutyMode.Logistics || IsDriverBusDriver(driver) || IsDriverOnActiveTradeRun(driver))
            {
                return false;
            }
            return true;
        }

        return false;
    }

    private bool IsWorkerVacantForVacancyAssignment(DriverAgent driver)
    {
        if (driver == null)
        {
            return false;
        }

        return !driver.IsArrivingByBus &&
               !IsDriverBusyWalkPhase(driver) &&
               driver.RestPhase == DriverRestPhase.None &&
               driver.DutyMode == DriverDutyMode.Local &&
               driver.ShiftStartHour < 0 &&
               driver.AssignedTruckNumber <= 0 &&
               !driver.AssignedBuildingType.HasValue &&
               !IsDriverBusDriver(driver) &&
               !IsDriverIntercity(driver) &&
               !IsDriverOnActiveTradeRun(driver) &&
               !IsBusDriverOnActiveRoute(driver);
    }

    private static bool CanWorkerMeetEducationRequirement(DriverAgent driver, WorkerEducation requiredEducation)
    {
        return driver != null && driver.Education >= requiredEducation;
    }

    private string GetWorkerEducationLabel(WorkerEducation education)
    {
        bool ru = IsRussianLanguage();
        return education == WorkerEducation.Skilled
            ? (ru ? "Квалифицированный" : "Skilled")
            : (ru ? "Базовое" : "Basic");
    }

    private void AssignVacancyToWorker(VacancyViewModel vacancy, DriverAgent worker)
    {
        if (vacancy == null || worker == null)
        {
            return;
        }

        bool assigned = true;
        switch (vacancy.Kind)
        {
            case VacancyKind.Production:
                int slotIndex = IsGroupedWarehouseVacancy(vacancy) ? selectedVacancyShiftIndex : vacancy.SlotIndex;
                AssignWorkerToBuilding(worker, FindLogisticsSlot(vacancy.BuildingType, slotIndex));
                break;
            case VacancyKind.Intercity:
                assigned = AssignIntercityVacancy(worker);
                break;
            case VacancyKind.BusDriver:
                AssignDriverToBusSlot(worker, selectedVacancyShiftIndex);
                break;
            case VacancyKind.TruckDriver:
                assigned = AssignTruckDriverVacancy(worker);
                break;
        }

        if (!assigned)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        vacancySuccessMessage = ru
            ? $"✓ Назначено: {worker.DriverName}"
            : $"✓ Assigned: {worker.DriverName}";
        vacancySuccessTimer = 4f;
        BuildVacancyViewModels();
        selectedVacancyIndex = FindVacancyIndexForAssignedWorker(worker);
        selectedVacancyShiftIndex = -1;
        selectedVacancyTruckNumber = 0;
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private bool AssignTruckDriverVacancy(DriverAgent worker)
    {
        if (worker == null || selectedVacancyShiftIndex < 0)
        {
            return false;
        }

        if (IsDriverIntercity(worker))
        {
            intercityDriverId = 0;
            SetDriverDutyMode(worker, DriverDutyMode.Local);
        }

        worker.ShiftStartHour = ShiftPresetHours[selectedVacancyShiftIndex];
        worker.IsOnActiveShift = false;
        worker.WaitingForShiftAtParking = false;
        worker.NeedsShiftEndReturn = false;
        SessionDebugLogger.Log("SHIFT", $"{worker.DriverName} assigned to freight shift {ShiftNames[selectedVacancyShiftIndex]} ({GetShiftRangeLabel(worker.ShiftStartHour)}); truck will be auto-picked from Parking.");
        LogDriverReaction(worker, $"assigned to {ShiftNames[selectedVacancyShiftIndex]} freight shift");
        PushFeedEvent(
            $"{worker.DriverName} assigned to freight shift {ShiftNames[selectedVacancyShiftIndex]}.",
            $"{worker.DriverName} назначен на грузоперевозки: {L(ShiftNames[selectedVacancyShiftIndex])}.",
            FeedEventType.Info);

        bool inWindow = IsHourInShiftWindow(GetCurrentHour(), worker.ShiftStartHour);
        if (inWindow && worker.RestPhase == DriverRestPhase.None)
        {
            StartDriverShiftCommute(worker);
        }

        MarkTutorialGoalComplete(TutorialGoalKind.AssignTruckDriverShift);
        NotifyTutorialTradeDriverAssigned();
        if (selectedGameStartMode == GameStartMode.User &&
            !isTutorialSkipped &&
            isTutorialGoalsActive &&
            tutorialGoalsMode == TutorialGoalsMode.BuyTruck)
        {
            isShiftsPanelOpen = false;
            if (shiftsScreenUi?.CanvasRoot != null)
            {
                shiftsScreenUi.CanvasRoot.SetActive(false);
            }

            SessionDebugLogger.Log("TUTORIAL", "Closed Vacancies after Truck Driver assignment.");
        }

        return true;
    }

    private bool AssignIntercityVacancy(DriverAgent worker)
    {
        if (worker == null)
        {
            return false;
        }

        worker.ShiftStartHour = -1;
        worker.IsOnActiveShift = false;
        worker.WaitingForShiftAtParking = false;
        worker.NeedsShiftEndReturn = false;
        AssignDriverToIntercitySlot(worker);

        bool assigned = IsDriverIntercity(worker);
        if (assigned)
        {
            SessionDebugLogger.Log("SHIFT", $"{worker.DriverName} assigned to Intercity; truck will be auto-picked from Parking.");
            NotifyTutorialIntercityDriverAssigned();
        }
        return assigned;
    }

    private void RemoveVacancyAssignment(VacancyViewModel vacancy)
    {
        if (vacancy == null)
        {
            return;
        }

        switch (vacancy.Kind)
        {
            case VacancyKind.Production:
                RemoveWorkerFromBuilding(FindLogisticsSlot(vacancy.BuildingType, vacancy.SlotIndex));
                break;
            case VacancyKind.Intercity:
                RemoveIntercityDriverAssignment();
                break;
            case VacancyKind.BusDriver:
                RemoveBusDriverAssignment(vacancy.ShiftIndex);
                break;
            case VacancyKind.TruckDriver:
                RemoveTruckShiftAssignment(vacancy);
                break;
        }

        selectedVacancyIndex = -1;
        selectedVacancyShiftIndex = -1;
        selectedVacancyTruckNumber = 0;
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void RemoveTruckShiftAssignment(VacancyViewModel vacancy)
    {
        DriverAgent worker = vacancy?.AssignedWorker;
        if (worker == null)
        {
            return;
        }

        TruckAgent truck = GetTruckAgent(worker.AssignedTruckNumber);
        if (truck != null && !UnassignDriverFromTruck(truck, worker))
        {
            return;
        }

        worker.ShiftStartHour = -1;
        worker.IsOnActiveShift = false;
        worker.WaitingForShiftAtParking = false;
        worker.NeedsShiftEndReturn = false;
        SessionDebugLogger.Log("SHIFT", $"{worker.DriverName} removed from truck vacancy.");
        LogDriverReaction(worker, "truck vacancy removed");
    }

    private LogisticsSlotUi FindLogisticsSlot(LocationType buildingType, int slotIndex)
    {
        for (int i = 0; i < logisticsSlots.Length; i++)
        {
            LogisticsSlotUi slot = logisticsSlots[i];
            if (slot != null && slot.BuildingType == buildingType && slot.SlotIndex == slotIndex)
            {
                return slot;
            }
        }
        return null;
    }

    private int FindVacancyIndexForAssignedWorker(DriverAgent worker)
    {
        if (worker == null)
        {
            return -1;
        }

        for (int i = 0; i < vacancyViewModels.Count; i++)
        {
            if (vacancyViewModels[i].AssignedWorker == worker)
            {
                return i;
            }

            if (IsGroupedWarehouseVacancy(vacancyViewModels[i]) &&
                worker.DutyMode == DriverDutyMode.Logistics &&
                worker.AssignedBuildingType == LocationType.Warehouse)
            {
                return i;
            }
        }
        return -1;
    }

    private bool IsAnyTruckDriverAssignedToShift(int shiftIndex)
    {
        if (shiftIndex < 0 || shiftIndex >= ShiftPresetHours.Length)
        {
            return false;
        }

        int shiftHour = ShiftPresetHours[shiftIndex];
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver.ShiftStartHour == shiftHour && driver.DutyMode == DriverDutyMode.Local && !IsDriverBusDriver(driver))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsTruckShiftOccupied(TruckAgent truck, int shiftIndex)
    {
        if (truck == null || shiftIndex < 0 || shiftIndex >= ShiftPresetHours.Length)
        {
            return true;
        }

        int shiftHour = ShiftPresetHours[shiftIndex];
        for (int i = 0; i < truck.AssignedDrivers.Count; i++)
        {
            DriverAgent driver = truck.AssignedDrivers[i];
            if (driver != null && driver.ShiftStartHour == shiftHour)
            {
                return true;
            }
        }
        return false;
    }

    private bool HasTruckRosterDriverWithoutShift(TruckAgent truck)
    {
        if (truck == null)
        {
            return false;
        }

        for (int i = 0; i < truck.AssignedDrivers.Count; i++)
        {
            DriverAgent driver = truck.AssignedDrivers[i];
            if (driver != null && driver.ShiftStartHour < 0)
            {
                return true;
            }
        }
        return false;
    }

    private int GetShiftIndexForHour(int shiftHour)
    {
        for (int i = 0; i < ShiftPresetHours.Length; i++)
        {
            if (ShiftPresetHours[i] == shiftHour)
            {
                return i;
            }
        }
        return -1;
    }

    private string GetShiftNameForHour(int shiftHour)
    {
        int index = GetShiftIndexForHour(shiftHour);
        return index >= 0 ? $"{L(ShiftNames[index])} {GetShiftRangeLabel(shiftHour)}" : GetShiftRangeLabel(shiftHour);
    }

    private static float GetShiftsTabContentHeight()
    {
        const float rightPanelVerticalPadding = 30f;
        const float rightPanelInterBlockSpacing = 24f;
        float available = ShiftsInnerPanelHeight - rightPanelVerticalPadding - rightPanelInterBlockSpacing - ShiftsSelectionCardHeight - ShiftsTabRowHeight;
        return Mathf.Max(280f, available);
    }

    private static void ApplyFixedLayoutSize(RectTransform rect, float width, float height)
    {
        if (rect == null)
        {
            return;
        }

        LayoutElement layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
        {
            return;
        }

        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.flexibleWidth = 0f;
        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;
    }

    private static void ApplyFixedLayoutHeight(RectTransform rect, float height)
    {
        if (rect == null)
        {
            return;
        }

        LayoutElement layout = rect.GetComponent<LayoutElement>();
        if (layout == null)
        {
            return;
        }

        layout.minHeight = height;
        layout.preferredHeight = height;
        layout.flexibleHeight = 0f;
    }

}
