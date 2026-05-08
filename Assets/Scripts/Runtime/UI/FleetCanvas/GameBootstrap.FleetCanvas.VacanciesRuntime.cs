using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static bool UseVacanciesScreen() => true;
    private const int SingleShiftVacancySelection = -2;

    private void UpdateVacanciesScreenUi()
    {
        bool ru = IsRussianLanguage();
        if (vacancySuccessTimer > 0f)
        {
            vacancySuccessTimer -= Time.unscaledDeltaTime;
            if (vacancySuccessTimer <= 0f)
            {
                vacancySuccessMessage = string.Empty;
            }
        }
        BuildVacancyViewModels();

        if (selectedVacancyIndex >= vacancyViewModels.Count)
        {
            selectedVacancyIndex = -1;
            selectedVacancyShiftIndex = -1;
            selectedVacancyTruckNumber = 0;
        }

        if (shiftsScreenUi.TitleText != null)
        {
            shiftsScreenUi.TitleText.text = GetStaffingScreenTitle(ru);
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
            shiftsScreenUi.HeaderCountText.text = GetStaffingScreenCountLabel(freeCount, vacancyViewModels.Count, ru);
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
            row.Background.color = selected
                ? new Color(0.33f, 0.28f, 0.13f, 0.98f)
                : vacancy.IsOccupied ? ShiftsCardColor : FleetCardMutedColor;
            if (row.SelectedBorder != null)
            {
                row.SelectedBorder.gameObject.SetActive(selected);
            }
            row.NameText.text = vacancy.Title;
            row.NameText.color = selected ? Color.white : new Color(0.88f, 0.91f, 0.96f, 1f);
            if (row.ProfessionText != null)
            {
                SetVacancyBadge(row.BadgeBackground, row.ProfessionText, vacancy.IsOccupied, false);
            }
            string workerPart = vacancy.AssignedWorker != null
                ? vacancy.AssignedWorker.DriverName
                : (ru ? "без рабочего" : "no worker");
            row.StatusText.text = string.IsNullOrWhiteSpace(vacancy.Schedule)
                ? $"{vacancy.Subtitle} · {workerPart}"
                : $"{vacancy.Subtitle} · {vacancy.Schedule} · {workerPart}";
            row.StatusText.text += $" | {FormatVacancyOffer(new VacancyOffer(vacancy.OfferSalary, vacancy.ContractWorkDays, vacancy.MarketPressure, vacancy.RequiredProfessionalLevel), ru)}";
            row.StatusText.color = selected ? Color.white : vacancy.IsOccupied ? FleetAccentColor : FleetSecondaryTextColor;
        }

        VacancyViewModel selectedVacancy = selectedVacancyIndex >= 0 && selectedVacancyIndex < vacancyViewModels.Count
            ? vacancyViewModels[selectedVacancyIndex]
            : null;
        UpdateVacancyStepProgress(selectedVacancy);

        if (shiftsScreenUi.SelectionTitleText != null)
        {
            shiftsScreenUi.SelectionTitleText.text = ru ? "Обзор рабочего места" : "Labor Market";
        }
        UpdateVacancyContextCard(selectedVacancy);
        BuildVacancyFlowOptions(selectedVacancy);
        UpdateVacancyTransportParkCard(selectedVacancy);
        RenderVacancyFlowOptions(selectedVacancy);

        LayoutRebuilder.ForceRebuildLayoutImmediate(shiftsScreenUi.DriverListContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(shiftsScreenUi.WindowRoot);
        EnforceShiftsWindowLayout();
        LogShiftsHudState("rebuilt Vacancies canvas");
        isShiftsScreenDirty = false;
    }

    private void UpdateVacancyStepProgress(VacancyViewModel vacancy)
    {
        bool ru = IsRussianLanguage();
        string[] labels = ru
            ? new[] { "Вакансия", "Смена", "Грузовик", "Рабочий" }
            : new[] { "Vacancy", "Shift", "Truck", "Worker" };
        int currentStep = GetVacancyCurrentStep(vacancy);
        for (int i = 0; i < shiftsScreenUi.VacancyStepBackgrounds.Count && i < labels.Length; i++)
        {
            bool applies = IsVacancyStepApplicable(vacancy, i);
            Image bg = shiftsScreenUi.VacancyStepBackgrounds[i];
            Text text = shiftsScreenUi.VacancyStepTexts[i];
            GameObject stepRoot = bg != null ? bg.gameObject : text != null ? text.transform.parent?.gameObject : null;
            if (stepRoot != null)
            {
                stepRoot.SetActive(applies);
            }
            if (!applies)
            {
                continue;
            }

            bool completed = applies && i < currentStep;
            bool current = applies && i == currentStep;
            if (bg != null)
            {
                bg.color = completed
                    ? new Color(0.15f, 0.34f, 0.20f, 0.96f)
                    : current
                        ? FleetPrimaryButtonColor
                        : new Color(0.08f, 0.10f, 0.14f, 0.95f);
            }
            if (text != null)
            {
                text.text = completed ? $"✓ {labels[i]}" : labels[i];
                text.color = completed || current ? Color.white : FleetMutedTextColor;
                text.fontStyle = current ? FontStyle.Bold : FontStyle.Normal;
            }
        }
    }

    private int GetVacancyCurrentStep(VacancyViewModel vacancy)
    {
        if (vacancy == null) return 0;
        return VacancyFlowRulesService.GetCurrentStep(
            vacancy.IsOccupied,
            VacancyRequiresShiftStep(vacancy),
            HasSelectedVacancyShift(vacancy),
            VacancyRequiresTruckStep(vacancy),
            selectedVacancyTruckNumber > 0 || vacancy.TruckNumber > 0);
    }

    private static bool IsVacancyStepApplicable(VacancyViewModel vacancy, int step)
    {
        if (vacancy == null) return step == 0;
        if (step == 0 || step == 3) return true;
        if (step == 1) return VacancyRequiresShiftStep(vacancy);
        if (step == 2) return VacancyRequiresTruckStep(vacancy);
        return false;
    }

    private static bool VacancyRequiresTruckStep(VacancyViewModel vacancy)
    {
        return vacancy != null && VacancyFlowRulesService.RequiresTruckStep(ToVacancyFlowKind(vacancy.Kind));
    }

    private static bool VacancyRequiresShiftStep(VacancyViewModel vacancy)
    {
        return vacancy != null && VacancyFlowRulesService.RequiresShiftStep(ToVacancyFlowKind(vacancy.Kind));
    }

    private static VacancyFlowKind ToVacancyFlowKind(VacancyKind kind)
    {
        return kind switch
        {
            VacancyKind.TruckDriver => VacancyFlowKind.TruckDriver,
            VacancyKind.Intercity => VacancyFlowKind.Intercity,
            VacancyKind.BusDriver => VacancyFlowKind.BusDriver,
            VacancyKind.Service => VacancyFlowKind.Service,
            _ => VacancyFlowKind.Production
        };
    }

    private bool HasSelectedVacancyShift(VacancyViewModel vacancy)
    {
        if (vacancy == null || !VacancyRequiresShiftStep(vacancy))
        {
            return false;
        }

        if (vacancy.Kind == VacancyKind.TruckDriver || vacancy.Kind == VacancyKind.BusDriver)
        {
            return selectedVacancyShiftIndex >= 0 && selectedVacancyShiftIndex < ShiftPresetHours.Length;
        }

        return selectedVacancyShiftIndex == SingleShiftVacancySelection ||
               (selectedVacancyShiftIndex >= 0 && selectedVacancyShiftIndex < ShiftPresetHours.Length);
    }

    private static bool IsGroupedWarehouseVacancy(VacancyViewModel vacancy)
    {
        return vacancy != null && vacancy.IsGroupedWarehouse && vacancy.BuildingType == LocationType.Warehouse;
    }

    private void UpdateVacancyContextCard(VacancyViewModel vacancy)
    {
        bool ru = IsRussianLanguage();
        string empty = ru ? "— не выбрано" : "— not selected";
        string vacancyValue = vacancy?.Title ?? empty;
        string shiftValue = GetSelectedVacancyShiftLabel(vacancy, empty);
        string truckValue = GetSelectedVacancyTruckLabel(vacancy, empty);
        string workerValue = vacancy?.AssignedWorker?.DriverName ?? empty;
        VacancyOffer offer = GetCurrentVacancyOffer(vacancy);

        if (shiftsScreenUi.SelectionNameText != null)
        {
            shiftsScreenUi.SelectionNameText.text = $"{(ru ? "Вакансия" : "Vacancy")}: {vacancyValue}";
            shiftsScreenUi.SelectionNameText.color = vacancy != null ? FleetAccentColor : FleetSecondaryTextColor;
        }
        if (shiftsScreenUi.SelectionProfessionText != null)
        {
            shiftsScreenUi.SelectionProfessionText.text = $"{(ru ? "Смена" : "Shift")}: {shiftValue}";
        }
        if (shiftsScreenUi.SelectionStatusText != null)
        {
            shiftsScreenUi.SelectionStatusText.text = $"{(ru ? "Грузовик" : "Truck")}: {truckValue}";
            shiftsScreenUi.SelectionStatusText.color = selectedVacancyTruckNumber > 0 || vacancy?.TruckNumber > 0 ? FleetAccentColor : FleetSecondaryTextColor;
        }
        if (shiftsScreenUi.SelectionHintText != null)
        {
            shiftsScreenUi.SelectionHintText.text = $"{(ru ? "Рабочий" : "Worker")}: {workerValue}";
            shiftsScreenUi.SelectionHintText.text += $" | {FormatVacancyOffer(offer, ru)}";
            shiftsScreenUi.SelectionHintText.color = vacancy?.AssignedWorker != null ? FleetAccentColor : FleetMutedTextColor;
        }
        if (shiftsScreenUi.VacancySuccessText != null)
        {
            bool hasSuccess = !string.IsNullOrWhiteSpace(vacancySuccessMessage);
            shiftsScreenUi.VacancySuccessText.text = hasSuccess ? vacancySuccessMessage : string.Empty;
            shiftsScreenUi.VacancySuccessText.color = new Color(0.65f, 0.95f, 0.66f, hasSuccess ? 1f : 0f);
        }
    }

    private string GetSelectedVacancyShiftLabel(VacancyViewModel vacancy, string empty)
    {
        if (vacancy == null) return empty;
        if (vacancy.IsOccupied && !string.IsNullOrWhiteSpace(vacancy.Schedule)) return vacancy.Schedule;
        if (selectedVacancyShiftIndex == SingleShiftVacancySelection)
        {
            return !string.IsNullOrWhiteSpace(vacancy.Schedule) ? vacancy.Schedule : "—";
        }
        if (IsGroupedWarehouseVacancy(vacancy) &&
            selectedVacancyShiftIndex >= 0 &&
            selectedVacancyShiftIndex < WarehouseMaxWorkers)
        {
            return IsRussianLanguage()
                ? $"\u0421\u043a\u043b\u0430\u0434\u0441\u043a\u0430\u044f \u0441\u043c\u0435\u043d\u0430 {selectedVacancyShiftIndex + 1}"
                : $"Warehouse shift {selectedVacancyShiftIndex + 1}";
        }

        if (selectedVacancyShiftIndex >= 0 && selectedVacancyShiftIndex < ShiftPresetHours.Length)
        {
            return $"{L(ShiftNames[selectedVacancyShiftIndex])} {GetShiftRangeLabel(ShiftPresetHours[selectedVacancyShiftIndex])}";
        }
        return empty;
    }

    private string GetSelectedVacancyTruckLabel(VacancyViewModel vacancy, string empty)
    {
        if (vacancy == null) return empty;
        if (vacancy.TruckNumber > 0) return $"Truck #{vacancy.TruckNumber}";
        if (selectedVacancyTruckNumber > 0) return $"Truck #{selectedVacancyTruckNumber}";
        if (vacancy.Kind == VacancyKind.TruckDriver)
        {
            return IsRussianLanguage() ? "\u0430\u0432\u0442\u043e \u0438\u0437 Parking" : "auto from Parking";
        }
        return VacancyRequiresTruckStep(vacancy) ? empty : "—";
    }

    private void UnlockAllTutorialVacancies()
    {
        areTutorialVacanciesFullyUnlocked = true;
        isTutorialTruckDriverVacancyUnlocked = true;
        isShiftsScreenDirty = true;
        SessionDebugLogger.Log("TUTORIAL", "All vacancies unlocked.");
    }

    private void UnlockTutorialTruckDriverVacancy()
    {
        if (isTutorialTruckDriverVacancyUnlocked)
        {
            return;
        }

        isTutorialTruckDriverVacancyUnlocked = true;
        isShiftsScreenDirty = true;
        SessionDebugLogger.Log("TUTORIAL", "Truck Driver vacancy unlocked.");
    }

    private bool IsVacancyUnlockedForCurrentTutorial(VacancyKind kind, LocationType? buildingType = null)
    {
        if (selectedGameStartMode != GameStartMode.Tutorial || isTutorialSkipped || areTutorialVacanciesFullyUnlocked)
        {
            return true;
        }

        if (kind == VacancyKind.TruckDriver)
        {
            return isTutorialTruckDriverVacancyUnlocked;
        }

        return kind == VacancyKind.Production && buildingType == LocationType.Forest;
    }

    private void BuildVacancyViewModels()
    {
        bool ru = IsRussianLanguage();
        vacancyViewModels.Clear();

        if (IsVacancyUnlockedForCurrentTutorial(VacancyKind.TruckDriver))
        {
            vacancyViewModels.Add(new VacancyViewModel
            {
                Kind = VacancyKind.TruckDriver,
                Title = ru ? "Водитель грузовика" : "Truck Driver",
                Subtitle = ru ? "грузоперевозки" : "freight routes",
                IsOccupied = false,
            });
        }

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (!IsVacancyUnlockedForCurrentTutorial(VacancyKind.TruckDriver) ||
                driver.ShiftStartHour < 0 ||
                IsDriverBusDriver(driver) ||
                driver.DutyMode != DriverDutyMode.Local)
            {
                continue;
            }

            vacancyViewModels.Add(new VacancyViewModel
            {
                Kind = VacancyKind.TruckDriver,
                Title = ru ? $"\u0413\u0440\u0443\u0437\u043e\u043f\u0435\u0440\u0435\u0432\u043e\u0437\u043a\u0438: {GetShiftRangeLabel(driver.ShiftStartHour)}" : $"Freight: {GetShiftRangeLabel(driver.ShiftStartHour)}",
                Subtitle = ru ? "водитель грузовика" : "truck driver",
                Schedule = GetShiftNameForHour(driver.ShiftStartHour),
                IsOccupied = true,
                AssignedWorker = driver,
                ShiftIndex = GetShiftIndexForHour(driver.ShiftStartHour),
                TruckNumber = driver.AssignedTruckNumber,
            });
        }

        if (IsVacancyUnlockedForCurrentTutorial(VacancyKind.BusDriver))
        {
            vacancyViewModels.Add(new VacancyViewModel
            {
                Kind = VacancyKind.BusDriver,
                Title = ru ? "Водитель автобуса" : "Bus Driver",
                Subtitle = ru ? "городской маршрут" : "local bus route",
                IsOccupied = false,
            });
        }

        for (int i = 0; i < ShiftPresetHours.Length; i++)
        {
            DriverAgent busDriver = GetBusAssignedDriver(i);
            if (!IsVacancyUnlockedForCurrentTutorial(VacancyKind.BusDriver) || busDriver == null)
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
                ShiftIndex = i,
            });
        }

        if (locations.ContainsKey(LocationType.Warehouse) &&
            IsVacancyUnlockedForCurrentTutorial(VacancyKind.Production, LocationType.Warehouse))
        {
            int assignedWarehouseLoaders = CountLogisticsWorkers(LocationType.Warehouse);
            vacancyViewModels.Add(new VacancyViewModel
            {
                Kind = VacancyKind.Production,
                Title = ru ? "\u0421\u043a\u043b\u0430\u0434\u0441\u043a\u043e\u0439 \u0433\u0440\u0443\u0437\u0447\u0438\u043a" : "Warehouse Loader",
                Subtitle = ru ? "\u0441\u043a\u043b\u0430\u0434" : "warehouse",
                Schedule = ru
                    ? $"{assignedWarehouseLoaders}/{WarehouseMaxWorkers} \u0441\u043c\u0435\u043d"
                    : $"{assignedWarehouseLoaders}/{WarehouseMaxWorkers} shifts",
                IsOccupied = false,
                BuildingType = LocationType.Warehouse,
                SlotIndex = -1,
                IsGroupedWarehouse = true,
                FilledSlots = assignedWarehouseLoaders,
                MaxSlots = WarehouseMaxWorkers
            });
        }

        AddVacancyModelsForAssignableBuildings(ru);

        for (int i = logisticsSlots.Length; i < logisticsSlots.Length; i++)
        {
            LogisticsSlotUi slot = logisticsSlots[i];
            if (slot == null ||
                slot.BuildingType == LocationType.Warehouse ||
                !locations.ContainsKey(slot.BuildingType))
            {
                continue;
            }

            VacancyKind slotKind = IsProductionLocation(slot.BuildingType) ? VacancyKind.Production : VacancyKind.Service;
            if (!IsVacancyUnlockedForCurrentTutorial(slotKind, slot.BuildingType))
            {
                continue;
            }

            DriverAgent assigned = GetNthLogisticsWorker(slot.BuildingType, slot.SlotIndex);
            string buildingName = L(GetSelectedLocationDisplayName(slot.BuildingType));
            string slotLabel = slot.BuildingType == LocationType.Warehouse
                ? (ru ? $"Складской слот {slot.SlotIndex + 1}" : $"Warehouse slot {slot.SlotIndex + 1}")
                : GetMaxBuildingWorkerSlots(slot.BuildingType) > 1
                    ? $"{buildingName} #{slot.SlotIndex + 1}"
                    : buildingName;
            vacancyViewModels.Add(new VacancyViewModel
            {
                Kind = slotKind,
                Title = slotLabel,
                Subtitle = slot.BuildingType == LocationType.LaborExchange
                    ? (ru ? "\u0441\u0435\u0440\u0432\u0438\u0441, \u043d\u0443\u0436\u043d\u043e \u0432\u044b\u0441\u0448\u0435\u0435 \u043e\u0431\u0440\u0430\u0437\u043e\u0432\u0430\u043d\u0438\u0435" : "service, higher education required")
                    : slotKind == VacancyKind.Service
                    ? (ru ? "сервис" : "service")
                    : (ru ? "производство" : "production"),
                Schedule = GetBuildingWorkerWorkRangeLabel(slot.BuildingType, slot.SlotIndex),
                IsOccupied = assigned != null,
                AssignedWorker = assigned,
                BuildingType = slot.BuildingType,
                SlotIndex = slot.SlotIndex,
            });
        }

        for (int i = 0; i < vacancyViewModels.Count; i++)
        {
            ApplyVacancyOfferToViewModel(vacancyViewModels[i]);
        }

        vacancyViewModels.Sort((a, b) =>
        {
            int occupiedCompare = a.IsOccupied.CompareTo(b.IsOccupied);
            return occupiedCompare != 0 ? occupiedCompare : string.CompareOrdinal(a.Title, b.Title);
        });
    }

    private void AddVacancyModelsForAssignableBuildings(bool ru)
    {
        LocationType[] buildingTypes =
        {
            LocationType.Forest,
            LocationType.Sawmill,
            LocationType.FurnitureFactory,
            LocationType.Docks,
            LocationType.Motel,
            LocationType.Bar,
            LocationType.Canteen,
            LocationType.GasStation,
            LocationType.GamblingHall,
            LocationType.Kindergarten,
            LocationType.CarMarket,
            LocationType.LaborExchange
        };

        for (int ti = 0; ti < buildingTypes.Length; ti++)
        {
            LocationType buildingType = buildingTypes[ti];
            int maxSlots = GetMaxBuildingWorkerSlots(buildingType);
            if (maxSlots <= 0)
            {
                continue;
            }

            VacancyKind slotKind = IsProductionLocation(buildingType) ? VacancyKind.Production : VacancyKind.Service;
            if (!IsVacancyUnlockedForCurrentTutorial(slotKind, buildingType))
            {
                continue;
            }

            foreach (LocationData location in EnumerateAssignableBuildingLocations(buildingType))
            {
                for (int slotIndex = 0; slotIndex < maxSlots; slotIndex++)
                {
                    DriverAgent assigned = GetNthLogisticsWorker(buildingType, slotIndex, location.InstanceId);
                    vacancyViewModels.Add(new VacancyViewModel
                    {
                        Kind = slotKind,
                        Title = L(GetBuildingWorkerSlotTitle(buildingType, slotIndex, location.InstanceId)),
                        Subtitle = buildingType == LocationType.LaborExchange
                            ? (ru ? "\u0441\u0435\u0440\u0432\u0438\u0441, \u043d\u0443\u0436\u043d\u043e \u0432\u044b\u0441\u0448\u0435\u0435 \u043e\u0431\u0440\u0430\u0437\u043e\u0432\u0430\u043d\u0438\u0435" : "service, higher education required")
                            : slotKind == VacancyKind.Service
                                ? (ru ? "\u0441\u0435\u0440\u0432\u0438\u0441" : "service")
                                : (ru ? "\u043f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0441\u0442\u0432\u043e" : "production"),
                        Schedule = GetBuildingWorkerWorkRangeLabel(buildingType, slotIndex),
                        IsOccupied = assigned != null,
                        AssignedWorker = assigned,
                        BuildingType = buildingType,
                        LocationInstanceId = location.InstanceId,
                        SlotIndex = slotIndex,
                    });
                }
            }
        }
    }

    private void BuildVacancyFlowOptions(VacancyViewModel vacancy)
    {
        bool ru = IsRussianLanguage();
        vacancyFlowOptions.Clear();
        if (vacancy == null)
        {
            return;
        }

        if (IsGroupedWarehouseVacancy(vacancy))
        {
            for (int i = 0; i < WarehouseMaxWorkers; i++)
            {
                DriverAgent assigned = GetNthLogisticsWorker(LocationType.Warehouse, i);
                vacancyFlowOptions.Add(new VacancyFlowOption
                {
                    Kind = VacancyFlowOptionKind.Shift,
                    Title = ru
                        ? $"\u0421\u043a\u043b\u0430\u0434\u0441\u043a\u0430\u044f \u0441\u043c\u0435\u043d\u0430 {i + 1}"
                        : $"Warehouse shift {i + 1}",
                    Subtitle = assigned != null
                        ? assigned.DriverName
                        : (ru ? "\u0434\u043e\u0441\u0442\u0443\u043f\u043d\u0430" : "available"),
                    ShiftIndex = i
                });
            }

            if (HasSelectedVacancyShift(vacancy))
            {
                vacancyFlowOptions.Clear();
                AddWorkerOptionsForVacancy(vacancy);
            }

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

        if (VacancyRequiresShiftStep(vacancy) && !HasSelectedVacancyShift(vacancy))
        {
            if (vacancy.Kind == VacancyKind.TruckDriver || vacancy.Kind == VacancyKind.BusDriver)
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

            vacancyFlowOptions.Add(new VacancyFlowOption
            {
                Kind = VacancyFlowOptionKind.Shift,
                Title = !string.IsNullOrWhiteSpace(vacancy.Schedule)
                    ? vacancy.Schedule
                    : (ru ? "\u041e\u0441\u043d\u043e\u0432\u043d\u0430\u044f \u0441\u043c\u0435\u043d\u0430" : "Main shift"),
                Subtitle = ru ? "\u0435\u0434\u0438\u043d\u0441\u0442\u0432\u0435\u043d\u043d\u0430\u044f \u0434\u043e\u0441\u0442\u0443\u043f\u043d\u0430\u044f \u0441\u043c\u0435\u043d\u0430" : "only available shift",
                ShiftIndex = SingleShiftVacancySelection
            });
            return;
        }

        if (VacancyRequiresTruckStep(vacancy) && selectedVacancyTruckNumber <= 0)
        {
            bool hasAvailableTruck = false;
            for (int i = 1; i <= MaxTruckCount; i++)
            {
                TruckAgent truck = GetTruckAgent(i);
                if (truck == null)
                {
                    continue;
                }

                bool shiftOccupiedOnTruck = vacancy.Kind == VacancyKind.TruckDriver && IsTruckShiftOccupied(truck, selectedVacancyShiftIndex);
                bool hasCrewSpace = vacancy.Kind == VacancyKind.TruckDriver
                    ? truck.AssignedDrivers.Count < 2 || HasTruckRosterDriverWithoutShift(truck)
                    : truck.AssignedDrivers.Count < 2;
                hasAvailableTruck = hasAvailableTruck || (!shiftOccupiedOnTruck && hasCrewSpace);
                vacancyFlowOptions.Add(new VacancyFlowOption
                {
                    Kind = VacancyFlowOptionKind.Truck,
                    Title = truck.DisplayName,
                    Subtitle = shiftOccupiedOnTruck
                        ? (ru ? "\u044d\u0442\u0430 \u0441\u043c\u0435\u043d\u0430 \u0437\u0430\u043d\u044f\u0442\u0430" : "shift occupied")
                        : hasCrewSpace ? GetTruckAssignedDriverSummary(truck) : (ru ? "\u044d\u043a\u0438\u043f\u0430\u0436 \u043f\u043e\u043b\u043e\u043d" : "crew full"),
                    TruckNumber = i
                });
            }
            if (!hasAvailableTruck)
            {
                vacancyFlowOptions.Add(new VacancyFlowOption
                {
                    Kind = VacancyFlowOptionKind.Truck,
                    Title = ru ? "\u041d\u0435\u0442 \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u043e\u0433\u043e \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0430" : "No free truck",
                    Subtitle = ru
                        ? "\u0414\u043b\u044f \u044d\u0442\u043e\u0439 \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u0438 \u043d\u0443\u0436\u0435\u043d \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a \u0431\u0435\u0437 \u0437\u0430\u043d\u044f\u0442\u043e\u0439 \u0432\u044b\u0431\u0440\u0430\u043d\u043d\u043e\u0439 \u0441\u043c\u0435\u043d\u044b. \u041a\u0443\u043f\u0438 \u0435\u0433\u043e \u0432 \u0431\u043b\u043e\u043a\u0435 \u0422\u0440\u0430\u043d\u0441\u043f\u043e\u0440\u0442\u043d\u044b\u0439 \u043f\u0430\u0440\u043a."
                        : "This vacancy needs a truck without this shift occupied. Buy one in Transport Park.",
                    TruckNumber = 0
                });
            }
            return;
        }

        AddWorkerOptionsForVacancy(vacancy);
    }

    private void UpdateVacancyTransportParkCard(VacancyViewModel selectedVacancy)
    {
        if (shiftsScreenUi?.VacancyTransportParkCard == null)
        {
            return;
        }

        bool isTruckDriverVacancy = selectedVacancy?.Kind == VacancyKind.TruckDriver;
        bool isBusDriverVacancy = selectedVacancy?.Kind == VacancyKind.BusDriver;
        bool show = selectedVacancy != null &&
                    !selectedVacancy.IsOccupied &&
                    (isTruckDriverVacancy || isBusDriverVacancy);
        shiftsScreenUi.VacancyTransportParkCard.gameObject.SetActive(show);
        if (!show)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        int ownedVehicleCount = isBusDriverVacancy ? GetOwnedBusCount() : GetOwnedTruckCount();
        int maxVehicleCount = isBusDriverVacancy ? GetBusParkingCapacity() : GetTruckParkingCapacity();
        bool hasParking = locations.ContainsKey(LocationType.Parking);
        bool hasSelectedShift = HasSelectedVacancyShift(selectedVacancy);
        bool hasAvailableVehicle = isBusDriverVacancy ? HasAvailableBusForVacancy(selectedVacancy) : HasAvailableTruckForVacancy(selectedVacancy);

        if (shiftsScreenUi.VacancyTransportParkTitleText != null)
        {
            shiftsScreenUi.VacancyTransportParkTitleText.text = ru
                ? "\u0422\u0440\u0430\u043d\u0441\u043f\u043e\u0440\u0442\u043d\u044b\u0439 \u043f\u0430\u0440\u043a"
                : "Transport Park";
        }

        if (shiftsScreenUi.VacancyTransportParkCountText != null)
        {
            shiftsScreenUi.VacancyTransportParkCountText.text = isBusDriverVacancy
                ? (ru
                    ? $"\u0410\u0432\u0442\u043e\u0431\u0443\u0441\u044b: {ownedVehicleCount} / {maxVehicleCount}"
                    : $"Buses: {ownedVehicleCount} / {maxVehicleCount}")
                : (ru
                    ? $"\u0413\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438: {ownedVehicleCount} / {maxVehicleCount}"
                    : $"Trucks: {ownedVehicleCount} / {maxVehicleCount}");
        }

        if (shiftsScreenUi.VacancyTransportParkSummaryText != null)
        {
            string vehicleRu = isBusDriverVacancy ? "\u0430\u0432\u0442\u043e\u0431\u0443\u0441" : "\u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a";
            string vehicleEn = isBusDriverVacancy ? "bus" : "truck";
            string vehicleRuStart = isBusDriverVacancy ? "\u0410\u0432\u0442\u043e\u0431\u0443\u0441" : "\u0413\u0440\u0443\u0437\u043e\u0432\u0438\u043a";
            string summary = !hasSelectedShift
                ? (ru
                    ? $"\u0421\u043d\u0430\u0447\u0430\u043b\u0430 \u0432\u044b\u0431\u0435\u0440\u0438 \u0441\u043c\u0435\u043d\u0443. {vehicleRuStart} \u043f\u043e\u044f\u0432\u0438\u0442\u0441\u044f \u0438\u0437 \u0441\u043b\u043e\u0442\u0430 Parking \u0430\u0432\u0442\u043e\u043c\u0430\u0442\u0438\u0447\u0435\u0441\u043a\u0438."
                    : $"Choose a shift first. A {vehicleEn} will be provisioned automatically from a Parking slot.")
                : isBusDriverVacancy && hasAvailableVehicle
                    ? (ru
                        ? "\u0421\u043b\u043e\u0442 \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0430 \u0434\u043e\u0441\u0442\u0443\u043f\u0435\u043d. Parking \u0432\u044b\u0434\u0430\u0441\u0442 \u0442\u0440\u0430\u043d\u0441\u043f\u043e\u0440\u0442 \u043f\u0440\u0438 \u0441\u0442\u0430\u0440\u0442\u0435 \u0441\u043c\u0435\u043d\u044b."
                        : "A bus slot is available. Parking will provide the vehicle when the shift starts.")
                : hasAvailableVehicle
                    ? (ru
                        ? $"\u0421\u043b\u043e\u0442 \u0434\u043b\u044f {vehicleRu} \u0434\u043e\u0441\u0442\u0443\u043f\u0435\u043d. \u0412\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u0437\u0430\u0440\u0435\u0437\u0435\u0440\u0432\u0438\u0440\u0443\u0435\u0442 \u0435\u0433\u043e \u043f\u0440\u0438 \u043d\u0430\u0437\u043d\u0430\u0447\u0435\u043d\u0438\u0438."
                        : $"A {vehicleEn} slot is available. The worker reserves it when assigned.")
                    : (ru
                        ? $"\u041d\u0443\u0436\u0435\u043d \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u044b\u0439 \u0441\u043b\u043e\u0442 Parking \u0434\u043b\u044f {vehicleRu}."
                        : $"A free Parking slot is needed for this {vehicleEn}.");
            shiftsScreenUi.VacancyTransportParkSummaryText.text = summary;
            shiftsScreenUi.VacancyTransportParkSummaryText.color = !hasAvailableVehicle
                ? new Color(0.96f, 0.72f, 0.42f, 1f)
                : FleetSecondaryTextColor;
        }

        if (shiftsScreenUi.VacancyBuyTruckButton != null)
        {
            shiftsScreenUi.VacancyBuyTruckButton.interactable = false;
        }

        if (shiftsScreenUi.VacancyBuyTruckButtonText != null)
        {
            shiftsScreenUi.VacancyBuyTruckButtonText.text = isBusDriverVacancy
                ? (ru
                    ? "\u0421\u043b\u043e\u0442\u044b \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u043e\u0432"
                    : "Bus slots")
                : (ru
                    ? "\u0421\u043b\u043e\u0442\u044b \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u043e\u0432"
                    : "Truck slots");
        }

        if (shiftsScreenUi.VacancyBuyTruckStatusText != null)
        {
            shiftsScreenUi.VacancyBuyTruckStatusText.text = isBusDriverVacancy ? GetBusBuyStatusLabel() : GetFleetBuyStatusLabel();
            shiftsScreenUi.VacancyBuyTruckStatusText.color = hasParking && hasAvailableVehicle
                ? FleetSecondaryTextColor
                : new Color(0.96f, 0.72f, 0.42f, 1f);
        }
    }

    private void HireTransportForSelectedVacancy()
    {
        VacancyViewModel selectedVacancy = selectedVacancyIndex >= 0 && selectedVacancyIndex < vacancyViewModels.Count
            ? vacancyViewModels[selectedVacancyIndex]
            : null;

        if (selectedVacancy?.Kind == VacancyKind.BusDriver)
        {
            HireNewBus();
            return;
        }

        HireNewTruck();
    }

    private bool HasAvailableTruckForVacancy(VacancyViewModel vacancy)
    {
        if (vacancy == null || vacancy.Kind != VacancyKind.TruckDriver)
        {
            return false;
        }

        return HasAvailableTruckInParking();
    }

    private bool HasAvailableBusForVacancy(VacancyViewModel vacancy)
    {
        if (vacancy == null || vacancy.Kind != VacancyKind.BusDriver)
        {
            return false;
        }

        return HasAvailableBusInParking();
    }

    private void RenderVacancyFlowOptions(VacancyViewModel vacancy)
    {
        bool ru = IsRussianLanguage();
        if (shiftsScreenUi.VacancyFlowTitleText != null)
        {
            shiftsScreenUi.VacancyFlowTitleText.text = vacancy == null
                ? (locations.ContainsKey(LocationType.LaborExchange)
                    ? (ru ? "\u0410\u0432\u0442\u043e\u043d\u0430\u0451\u043c" : "Automated Hiring")
                    : (ru ? "\u0420\u0443\u0447\u043d\u044b\u0435 \u0448\u0430\u0433\u0438" : "Manual Steps"))
                : GetVacancyFlowTitle(vacancy);
        }
        if (shiftsScreenUi.VacancyFlowHintText != null)
        {
            shiftsScreenUi.VacancyFlowHintText.text = vacancy == null
                ? GetStaffingScreenEmptyHint(ru)
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
            bool optionOccupied = option.Kind == VacancyFlowOptionKind.Remove || blocked;
            row.Background.color = option.Kind == VacancyFlowOptionKind.Remove
                ? new Color(0.32f, 0.10f, 0.10f, 0.92f)
                : blocked ? new Color(0.12f, 0.12f, 0.14f, 0.82f) : new Color(0.15f, 0.18f, 0.24f, 0.98f);
            row.TitleText.text = option.Title;
            row.TitleText.color = blocked ? FleetMutedTextColor : Color.white;
            row.SubtitleText.text = option.Subtitle;
            row.SubtitleText.color = blocked ? FleetMutedTextColor : FleetSecondaryTextColor;
            SetVacancyBadge(row.BadgeBackground, row.BadgeText, optionOccupied, blocked);
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
            VacancyKind.TruckDriver when !HasSelectedVacancyShift(vacancy) => ru ? "Выбери смену" : "Choose Shift",
            VacancyKind.BusDriver when !HasSelectedVacancyShift(vacancy) => ru ? "Выбери смену" : "Choose Shift",
            VacancyKind.Production when !HasSelectedVacancyShift(vacancy) => ru ? "Выбери смену" : "Choose Shift",
            VacancyKind.Service when !HasSelectedVacancyShift(vacancy) => ru ? "Выбери смену" : "Choose Shift",
            VacancyKind.Intercity when !HasSelectedVacancyShift(vacancy) => ru ? "Выбери смену" : "Choose Shift",
            _ => ru ? "Выбери рабочего" : "Choose Worker"
        };
    }

    private void SetVacancyBadge(Image background, Text label, bool occupied, bool disabled)
    {
        bool ru = IsRussianLanguage();
        if (background != null)
        {
            background.color = disabled
                ? new Color(0.18f, 0.18f, 0.20f, 0.96f)
                : occupied
                    ? new Color(0.70f, 0.38f, 0.10f, 0.96f)
                    : new Color(0.16f, 0.42f, 0.20f, 0.96f);
        }
        if (label != null)
        {
            label.text = disabled
                ? (ru ? "Недоступно" : "Blocked")
                : occupied
                    ? (ru ? "Занято" : "Occupied")
                    : (ru ? "Свободно" : "Open");
            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;
        }
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
            VacancyKind.TruckDriver when !HasSelectedVacancyShift(vacancy) => ru ? "Сначала выбери смену для грузоперевозок." : "First choose the freight shift.",
            VacancyKind.BusDriver when !HasSelectedVacancyShift(vacancy) => ru ? "Сначала выбери смену городского автобуса." : "First choose the local bus shift.",
            VacancyKind.Production when !HasSelectedVacancyShift(vacancy) => ru ? "Подтверди рабочую смену этого здания." : "Confirm this building's work shift.",
            VacancyKind.Service when !HasSelectedVacancyShift(vacancy) => ru ? "Подтверди сервисную смену этого здания." : "Confirm this building's service shift.",
            VacancyKind.Intercity when !HasSelectedVacancyShift(vacancy) => ru ? "Подтверди межгороднюю смену." : "Confirm the intercity duty shift.",
            _ => locations.ContainsKey(LocationType.LaborExchange)
                ? (ru ? "\u0411\u0438\u0440\u0436\u0430 \u0437\u0430\u043a\u0440\u043e\u0435\u0442 \u044d\u0442\u043e \u0441\u0430\u043c\u0430; \u0440\u0443\u0447\u043d\u043e\u0439 \u0432\u044b\u0431\u043e\u0440 \u043e\u0441\u0442\u0430\u0432\u043b\u0435\u043d \u043a\u0430\u043a override." : "The Labor Exchange can fill this automatically; manual selection remains as an override.")
                : (ru ? "Выбери доступного рабочего из списка." : "Choose an available worker from the list.")
        };
    }


}
