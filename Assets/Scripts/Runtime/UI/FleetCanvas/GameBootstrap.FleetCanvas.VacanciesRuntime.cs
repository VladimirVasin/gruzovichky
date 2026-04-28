using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static bool UseVacanciesScreen() => true;

    private void UpdateVacanciesScreenUi()
    {
        bool ru = IsRussianLanguage();
        BuildVacancyViewModels();

        if (selectedVacancyIndex >= vacancyViewModels.Count)
        {
            selectedVacancyIndex = -1;
            selectedVacancyShiftIndex = -1;
            selectedVacancyTruckNumber = 0;
        }

        if (shiftsScreenUi.TitleText != null)
        {
            shiftsScreenUi.TitleText.text = ru ? "Вакансии" : "Vacancies";
        }

        int freeCount = 0;
        for (int i = 0; i < vacancyViewModels.Count; i++)
        {
            if (!vacancyViewModels[i].IsOccupied)
            {
                freeCount++;
            }
        }

        if (shiftsScreenUi.HeaderCountText != null)
        {
            shiftsScreenUi.HeaderCountText.text = ru
                ? $"{freeCount} свободно / {vacancyViewModels.Count} всего"
                : $"{freeCount} free / {vacancyViewModels.Count} total";
        }

        EnforceShiftsWindowLayout();
        if (shiftsScreenUi.TabRowRoot != null) shiftsScreenUi.TabRowRoot.gameObject.SetActive(false);
        if (shiftsTransportPanel != null) shiftsTransportPanel.gameObject.SetActive(false);
        if (shiftsLogisticsPanel != null) shiftsLogisticsPanel.gameObject.SetActive(false);
        if (shiftsScreenUi.VacancyFlowPanel != null) shiftsScreenUi.VacancyFlowPanel.gameObject.SetActive(true);

        for (int i = 0; i < shiftsScreenUi.DriverRows.Count; i++)
        {
            ShiftDriverRowUi row = shiftsScreenUi.DriverRows[i];
            bool active = i < vacancyViewModels.Count;
            row.Root.gameObject.SetActive(active);
            if (!active) continue;

            VacancyViewModel vacancy = vacancyViewModels[i];
            bool selected = selectedVacancyIndex == i;
            row.DriverId = 0;
            row.Background.color = selected ? ShiftsCardSelected : vacancy.IsOccupied ? ShiftsCardColor : FleetCardMutedColor;
            row.NameText.text = vacancy.Title;
            if (row.ProfessionText != null)
            {
                row.ProfessionText.text = vacancy.IsOccupied
                    ? (ru ? "Занято" : "Occupied")
                    : (ru ? "Свободно" : "Open");
                row.ProfessionText.color = vacancy.IsOccupied ? FleetAccentColor : new Color(0.62f, 0.92f, 0.62f, 1f);
            }

            string workerPart = vacancy.AssignedWorker != null
                ? vacancy.AssignedWorker.DriverName
                : (ru ? "без рабочего" : "no worker");
            row.StatusText.text = string.IsNullOrWhiteSpace(vacancy.Schedule)
                ? $"{vacancy.Subtitle} · {workerPart}"
                : $"{vacancy.Subtitle} · {vacancy.Schedule} · {workerPart}";
            row.StatusText.color = vacancy.IsOccupied ? FleetAccentColor : FleetSecondaryTextColor;
        }

        VacancyViewModel selectedVacancy = selectedVacancyIndex >= 0 && selectedVacancyIndex < vacancyViewModels.Count
            ? vacancyViewModels[selectedVacancyIndex]
            : null;

        if (shiftsScreenUi.SelectionTitleText != null)
        {
            shiftsScreenUi.SelectionTitleText.text = ru ? "Выбранная вакансия" : "Selected Vacancy";
        }
        if (shiftsScreenUi.SelectionNameText != null)
        {
            shiftsScreenUi.SelectionNameText.text = selectedVacancy?.Title ?? (ru ? "Ничего не выбрано" : "Nothing selected");
            shiftsScreenUi.SelectionNameText.color = selectedVacancy != null ? FleetAccentColor : Color.white;
        }
        if (shiftsScreenUi.SelectionProfessionText != null)
        {
            shiftsScreenUi.SelectionProfessionText.text = selectedVacancy?.Subtitle ?? (ru ? "Выбери вакансию слева" : "Pick a vacancy on the left");
        }
        if (shiftsScreenUi.SelectionStatusText != null)
        {
            shiftsScreenUi.SelectionStatusText.text = selectedVacancy == null
                ? (ru ? "После выбора появятся доступные шаги назначения." : "Available assignment steps appear after selection.")
                : selectedVacancy.IsOccupied
                    ? (ru ? $"Назначен: {selectedVacancy.AssignedWorker?.DriverName ?? "—"}" : $"Assigned: {selectedVacancy.AssignedWorker?.DriverName ?? "—"}")
                    : (ru ? "Вакансия свободна." : "Vacancy is open.");
            shiftsScreenUi.SelectionStatusText.color = selectedVacancy?.IsOccupied == true ? FleetAccentColor : FleetSecondaryTextColor;
        }
        if (shiftsScreenUi.SelectionHintText != null)
        {
            shiftsScreenUi.SelectionHintText.text = selectedVacancy?.Schedule ?? string.Empty;
        }

        BuildVacancyFlowOptions(selectedVacancy);
        RenderVacancyFlowOptions(selectedVacancy);

        LayoutRebuilder.ForceRebuildLayoutImmediate(shiftsScreenUi.DriverListContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(shiftsScreenUi.WindowRoot);
        EnforceShiftsWindowLayout();
        LogShiftsHudState("rebuilt Vacancies canvas");
        isShiftsScreenDirty = false;
    }

    private void BuildVacancyViewModels()
    {
        bool ru = IsRussianLanguage();
        vacancyViewModels.Clear();

        vacancyViewModels.Add(new VacancyViewModel
        {
            Kind = VacancyKind.TruckDriver,
            Title = ru ? "Водитель грузовика" : "Truck Driver",
            Subtitle = ru ? "грузоперевозки" : "freight routes",
            IsOccupied = false
        });

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver.AssignedTruckNumber <= 0 || driver.ShiftStartHour < 0 || IsDriverBusDriver(driver) || driver.DutyMode != DriverDutyMode.Local)
            {
                continue;
            }

            vacancyViewModels.Add(new VacancyViewModel
            {
                Kind = VacancyKind.TruckDriver,
                Title = ru ? $"Truck #{driver.AssignedTruckNumber}: {GetShiftRangeLabel(driver.ShiftStartHour)}" : $"Truck #{driver.AssignedTruckNumber}: {GetShiftRangeLabel(driver.ShiftStartHour)}",
                Subtitle = ru ? "водитель грузовика" : "truck driver",
                Schedule = GetShiftNameForHour(driver.ShiftStartHour),
                IsOccupied = true,
                AssignedWorker = driver,
                ShiftIndex = GetShiftIndexForHour(driver.ShiftStartHour),
                TruckNumber = driver.AssignedTruckNumber
            });
        }

        vacancyViewModels.Add(new VacancyViewModel
        {
            Kind = VacancyKind.BusDriver,
            Title = ru ? "Водитель автобуса" : "Bus Driver",
            Subtitle = ru ? "городской маршрут" : "local bus route",
            IsOccupied = false
        });

        for (int i = 0; i < ShiftPresetHours.Length; i++)
        {
            DriverAgent busDriver = GetBusAssignedDriver(i);
            if (busDriver == null)
            {
                continue;
            }

            vacancyViewModels.Add(new VacancyViewModel
            {
                Kind = VacancyKind.BusDriver,
                Title = ru ? $"Автобус: {L(ShiftNames[i])}" : $"Bus: {L(ShiftNames[i])}",
                Subtitle = ru ? "водитель автобуса" : "bus driver",
                Schedule = GetShiftRangeLabel(ShiftPresetHours[i]),
                IsOccupied = true,
                AssignedWorker = busDriver,
                ShiftIndex = i
            });
        }

        DriverAgent intercity = GetIntercityAssignedDriver();
        vacancyViewModels.Add(new VacancyViewModel
        {
            Kind = VacancyKind.Intercity,
            Title = ru ? "Межгород" : "Intercity",
            Subtitle = ru ? "внешние рейсы и торговля" : "external trade runs",
            IsOccupied = intercity != null,
            AssignedWorker = intercity
        });

        for (int i = 0; i < logisticsSlots.Length; i++)
        {
            LogisticsSlotUi slot = logisticsSlots[i];
            if (slot == null || !locations.ContainsKey(slot.BuildingType))
            {
                continue;
            }

            DriverAgent assigned = GetNthLogisticsWorker(slot.BuildingType, slot.SlotIndex);
            string buildingName = L(GetSelectedLocationDisplayName(slot.BuildingType));
            string slotLabel = slot.BuildingType == LocationType.Warehouse
                ? (ru ? $"Складской слот {slot.SlotIndex + 1}" : $"Warehouse slot {slot.SlotIndex + 1}")
                : buildingName;
            vacancyViewModels.Add(new VacancyViewModel
            {
                Kind = VacancyKind.Production,
                Title = slotLabel,
                Subtitle = ru ? "производство" : "production",
                Schedule = GetProductionWorkRangeLabel(),
                IsOccupied = assigned != null,
                AssignedWorker = assigned,
                BuildingType = slot.BuildingType,
                SlotIndex = slot.SlotIndex
            });
        }

        vacancyViewModels.Sort((a, b) =>
        {
            int occupiedCompare = a.IsOccupied.CompareTo(b.IsOccupied);
            return occupiedCompare != 0 ? occupiedCompare : string.CompareOrdinal(a.Title, b.Title);
        });
    }

    private void BuildVacancyFlowOptions(VacancyViewModel vacancy)
    {
        bool ru = IsRussianLanguage();
        vacancyFlowOptions.Clear();
        if (vacancy == null)
        {
            return;
        }

        if (vacancy.IsOccupied)
        {
            vacancyFlowOptions.Add(new VacancyFlowOption
            {
                Kind = VacancyFlowOptionKind.Remove,
                Title = ru ? "Снять назначение" : "Remove assignment",
                Subtitle = vacancy.AssignedWorker?.DriverName ?? "—"
            });
            return;
        }

        if (vacancy.Kind == VacancyKind.TruckDriver || vacancy.Kind == VacancyKind.BusDriver)
        {
            if (selectedVacancyShiftIndex < 0)
            {
                for (int i = 0; i < ShiftPresetHours.Length; i++)
                {
                    bool occupied = vacancy.Kind == VacancyKind.BusDriver
                        ? GetBusAssignedDriver(i) != null
                        : IsAnyTruckDriverAssignedToShift(i);
                    vacancyFlowOptions.Add(new VacancyFlowOption
                    {
                        Kind = VacancyFlowOptionKind.Shift,
                        Title = $"{L(ShiftNames[i])} {GetShiftRangeLabel(ShiftPresetHours[i])}",
                        Subtitle = occupied ? (ru ? "уже есть назначение" : "already has assignment") : (ru ? "доступна" : "available"),
                        ShiftIndex = i
                    });
                }
                return;
            }

            if (vacancy.Kind == VacancyKind.TruckDriver && selectedVacancyTruckNumber <= 0)
            {
                for (int i = 1; i <= MaxTruckCount; i++)
                {
                    TruckAgent truck = GetTruckAgent(i);
                    if (truck == null)
                    {
                        continue;
                    }

                    bool shiftOccupiedOnTruck = IsTruckShiftOccupied(truck, selectedVacancyShiftIndex);
                    bool hasCrewSpace = truck.AssignedDrivers.Count < 2 || HasTruckRosterDriverWithoutShift(truck);
                    vacancyFlowOptions.Add(new VacancyFlowOption
                    {
                        Kind = VacancyFlowOptionKind.Truck,
                        Title = truck.DisplayName,
                        Subtitle = shiftOccupiedOnTruck
                            ? (ru ? "эта смена занята" : "shift occupied")
                            : hasCrewSpace ? GetTruckAssignedDriverSummary(truck) : (ru ? "экипаж полон" : "crew full"),
                        TruckNumber = i
                    });
                }
                vacancyFlowOptions.Add(new VacancyFlowOption
                {
                    Kind = VacancyFlowOptionKind.BuyTruck,
                    Title = ru ? $"Купить грузовик - ${HireTruckCost}" : $"Buy truck - ${HireTruckCost}",
                    Subtitle = GetFleetBuyStatusLabel()
                });
                return;
            }
        }

        AddWorkerOptionsForVacancy(vacancy);
    }

    private void RenderVacancyFlowOptions(VacancyViewModel vacancy)
    {
        bool ru = IsRussianLanguage();
        if (shiftsScreenUi.VacancyFlowTitleText != null)
        {
            shiftsScreenUi.VacancyFlowTitleText.text = vacancy == null
                ? (ru ? "Шаги назначения" : "Assignment Steps")
                : GetVacancyFlowTitle(vacancy);
        }
        if (shiftsScreenUi.VacancyFlowHintText != null)
        {
            shiftsScreenUi.VacancyFlowHintText.text = vacancy == null
                ? (ru ? "Выбери вакансию слева, затем пройди короткую цепочку выбора." : "Pick a vacancy on the left, then follow the short assignment chain.")
                : GetVacancyFlowHint(vacancy);
        }

        for (int i = 0; i < shiftsScreenUi.VacancyOptionRows.Count; i++)
        {
            VacancyOptionRowUi row = shiftsScreenUi.VacancyOptionRows[i];
            bool active = i < vacancyFlowOptions.Count;
            row.Root.gameObject.SetActive(active);
            if (!active) continue;

            VacancyFlowOption option = vacancyFlowOptions[i];
            bool blocked = IsVacancyOptionBlocked(vacancy, option);
            row.Background.color = option.Kind == VacancyFlowOptionKind.Remove
                ? new Color(0.32f, 0.10f, 0.10f, 0.92f)
                : blocked ? new Color(0.12f, 0.12f, 0.14f, 0.82f) : FleetCardMutedColor;
            row.TitleText.text = option.Title;
            row.TitleText.color = blocked ? FleetMutedTextColor : Color.white;
            row.SubtitleText.text = option.Subtitle;
            row.SubtitleText.color = blocked ? FleetMutedTextColor : FleetSecondaryTextColor;
            row.Button.interactable = !blocked;
        }
    }

    private string GetVacancyFlowTitle(VacancyViewModel vacancy)
    {
        bool ru = IsRussianLanguage();
        if (vacancy.IsOccupied)
        {
            return ru ? "Занятая вакансия" : "Occupied Vacancy";
        }

        return vacancy.Kind switch
        {
            VacancyKind.TruckDriver when selectedVacancyShiftIndex < 0 => ru ? "Выбери смену" : "Choose Shift",
            VacancyKind.TruckDriver when selectedVacancyTruckNumber <= 0 => ru ? "Выбери грузовик" : "Choose Truck",
            VacancyKind.BusDriver when selectedVacancyShiftIndex < 0 => ru ? "Выбери смену" : "Choose Shift",
            _ => ru ? "Выбери рабочего" : "Choose Worker"
        };
    }

    private string GetVacancyFlowHint(VacancyViewModel vacancy)
    {
        bool ru = IsRussianLanguage();
        if (vacancy.IsOccupied)
        {
            return ru ? "Можно освободить вакансию, если рабочий не занят активным рейсом." : "You can remove the assignment if the worker is not locked in an active run.";
        }

        return vacancy.Kind switch
        {
            VacancyKind.TruckDriver when selectedVacancyShiftIndex < 0 => ru ? "Сначала выбери смену для грузоперевозок." : "First choose the freight shift.",
            VacancyKind.TruckDriver when selectedVacancyTruckNumber <= 0 => ru ? "Теперь выбери грузовик для этой смены." : "Now choose the truck for that shift.",
            VacancyKind.BusDriver when selectedVacancyShiftIndex < 0 => ru ? "Сначала выбери смену городского автобуса." : "First choose the local bus shift.",
            _ => ru ? "Выбери доступного рабочего из списка." : "Choose an available worker from the list."
        };
    }

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
                    ? L(GetWorkerOccupationLabel(driver))
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

        if (option.Kind == VacancyFlowOptionKind.Truck)
        {
            TruckAgent truck = GetTruckAgent(option.TruckNumber);
            return truck == null || IsTruckShiftOccupied(truck, selectedVacancyShiftIndex) || (truck.AssignedDrivers.Count >= 2 && !HasTruckRosterDriverWithoutShift(truck));
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

        if (vacancy.Kind == VacancyKind.Production)
        {
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
            if (driver.AssignedTruckNumber > 0 || driver.DutyMode == DriverDutyMode.Logistics || IsDriverBusDriver(driver) || IsDriverOnActiveTradeRun(driver))
            {
                return false;
            }
            return driver.DutyMode == DriverDutyMode.Local;
        }

        if (vacancy.Kind == VacancyKind.BusDriver)
        {
            if (driver.AssignedTruckNumber > 0 || driver.DutyMode == DriverDutyMode.Logistics || IsDriverIntercity(driver) || IsDriverBusDriver(driver) || IsBusDriverOnActiveRoute(driver))
            {
                return false;
            }
            return true;
        }

        if (vacancy.Kind == VacancyKind.TruckDriver)
        {
            TruckAgent truck = GetTruckAgent(selectedVacancyTruckNumber);
            if (truck == null || selectedVacancyShiftIndex < 0)
            {
                return false;
            }
            if (driver.DutyMode == DriverDutyMode.Logistics || IsDriverBusDriver(driver) || IsDriverOnActiveTradeRun(driver))
            {
                return false;
            }
            if (driver.AssignedTruckNumber > 0 && driver.AssignedTruckNumber != truck.TruckNumber)
            {
                return false;
            }
            if (driver.AssignedTruckNumber == truck.TruckNumber)
            {
                return true;
            }
            return CanAssignDriverToTruckRoster(truck, driver);
        }

        return false;
    }

    private void AssignVacancyToWorker(VacancyViewModel vacancy, DriverAgent worker)
    {
        if (vacancy == null || worker == null)
        {
            return;
        }

        switch (vacancy.Kind)
        {
            case VacancyKind.Production:
                AssignWorkerToBuilding(worker, FindLogisticsSlot(vacancy.BuildingType, vacancy.SlotIndex));
                break;
            case VacancyKind.Intercity:
                AssignDriverToIntercitySlot(worker);
                break;
            case VacancyKind.BusDriver:
                AssignDriverToBusSlot(worker, selectedVacancyShiftIndex);
                break;
            case VacancyKind.TruckDriver:
                AssignTruckDriverVacancy(worker);
                break;
        }

        selectedVacancyIndex = -1;
        selectedVacancyShiftIndex = -1;
        selectedVacancyTruckNumber = 0;
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void AssignTruckDriverVacancy(DriverAgent worker)
    {
        TruckAgent truck = GetTruckAgent(selectedVacancyTruckNumber);
        if (truck == null || worker == null || selectedVacancyShiftIndex < 0)
        {
            return;
        }

        if (IsDriverIntercity(worker))
        {
            intercityDriverId = 0;
            SetDriverDutyMode(worker, DriverDutyMode.Local);
        }

        if (worker.AssignedTruckNumber != truck.TruckNumber)
        {
            if (!AssignDriverToTruck(truck, worker))
            {
                return;
            }
        }

        worker.ShiftStartHour = ShiftPresetHours[selectedVacancyShiftIndex];
        worker.IsOnActiveShift = false;
        worker.WaitingForShiftAtParking = false;
        worker.NeedsShiftEndReturn = false;
        truck.IsTruckAutoModeEnabled = true;
        SessionDebugLogger.Log("SHIFT", $"{worker.DriverName} assigned to {truck.DisplayName} shift {ShiftNames[selectedVacancyShiftIndex]} ({GetShiftRangeLabel(worker.ShiftStartHour)}).");
        LogDriverReaction(worker, $"assigned to {truck.DisplayName} {ShiftNames[selectedVacancyShiftIndex]} freight shift");
        PushFeedEvent(
            $"{worker.DriverName} assigned to {truck.DisplayName} {ShiftNames[selectedVacancyShiftIndex]}.",
            $"{worker.DriverName} назначен в {truck.DisplayName} на смену {L(ShiftNames[selectedVacancyShiftIndex])}.",
            FeedEventType.Info);

        bool inWindow = IsHourInShiftWindow(GetCurrentHour(), worker.ShiftStartHour);
        if (inWindow && worker.RestPhase == DriverRestPhase.None)
        {
            StartDriverShiftCommute(worker);
        }
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
            if (driver.AssignedTruckNumber > 0 && driver.ShiftStartHour == shiftHour && driver.DutyMode == DriverDutyMode.Local && !IsDriverBusDriver(driver))
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
