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
            string eduPart = vacancy.RequiredEducation == WorkerEducation.Skilled
                ? (ru ? "Квалифицированный" : "Skilled")
                : (ru ? "Базовый" : "Basic");
            row.StatusText.text = string.IsNullOrWhiteSpace(vacancy.Schedule)
                ? $"{vacancy.Subtitle} · {eduPart} · {workerPart}"
                : $"{vacancy.Subtitle} · {vacancy.Schedule} · {eduPart} · {workerPart}";
            row.StatusText.color = selected ? Color.white : vacancy.IsOccupied ? FleetAccentColor : FleetSecondaryTextColor;
        }

        VacancyViewModel selectedVacancy = selectedVacancyIndex >= 0 && selectedVacancyIndex < vacancyViewModels.Count
            ? vacancyViewModels[selectedVacancyIndex]
            : null;
        UpdateVacancyStepProgress(selectedVacancy);

        if (shiftsScreenUi.SelectionTitleText != null)
        {
            shiftsScreenUi.SelectionTitleText.text = ru ? "Контекст назначения" : "Assignment Context";
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
        if (vacancy.IsOccupied) return 4;
        if (VacancyRequiresShiftStep(vacancy) && !HasSelectedVacancyShift(vacancy)) return 1;
        if (vacancy.Kind == VacancyKind.TruckDriver)
        {
            if (selectedVacancyTruckNumber <= 0) return 2;
            return 3;
        }
        return 3;
    }

    private static bool IsVacancyStepApplicable(VacancyViewModel vacancy, int step)
    {
        if (vacancy == null) return step == 0;
        if (step == 0 || step == 3) return true;
        if (step == 1) return VacancyRequiresShiftStep(vacancy);
        if (step == 2) return vacancy.Kind == VacancyKind.TruckDriver;
        return false;
    }

    private static bool VacancyRequiresShiftStep(VacancyViewModel vacancy)
    {
        return vacancy != null;
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

    private void UpdateVacancyContextCard(VacancyViewModel vacancy)
    {
        bool ru = IsRussianLanguage();
        string empty = ru ? "— не выбрано" : "— not selected";
        string vacancyValue = vacancy?.Title ?? empty;
        string shiftValue = GetSelectedVacancyShiftLabel(vacancy, empty);
        string truckValue = GetSelectedVacancyTruckLabel(vacancy, empty);
        string workerValue = vacancy?.AssignedWorker?.DriverName ?? empty;

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
        return vacancy.Kind == VacancyKind.TruckDriver ? empty : "—";
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
        if (selectedGameStartMode != GameStartMode.User || isTutorialSkipped || areTutorialVacanciesFullyUnlocked)
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
                RequiredEducation = WorkerEducation.Basic
            });
        }

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (!IsVacancyUnlockedForCurrentTutorial(VacancyKind.TruckDriver) ||
                driver.AssignedTruckNumber <= 0 ||
                driver.ShiftStartHour < 0 ||
                IsDriverBusDriver(driver) ||
                driver.DutyMode != DriverDutyMode.Local)
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
                TruckNumber = driver.AssignedTruckNumber,
                RequiredEducation = WorkerEducation.Basic
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
                RequiredEducation = WorkerEducation.Basic
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
                RequiredEducation = WorkerEducation.Basic
            });
        }

        DriverAgent intercity = GetIntercityAssignedDriver();
        if (IsVacancyUnlockedForCurrentTutorial(VacancyKind.Intercity))
        {
            vacancyViewModels.Add(new VacancyViewModel
            {
                Kind = VacancyKind.Intercity,
                Title = ru ? "Межгород" : "Intercity",
                Subtitle = ru ? "внешние рейсы и торговля" : "external trade runs",
                IsOccupied = intercity != null,
                AssignedWorker = intercity,
                RequiredEducation = WorkerEducation.Basic
            });
        }

        for (int i = 0; i < logisticsSlots.Length; i++)
        {
            LogisticsSlotUi slot = logisticsSlots[i];
            if (slot == null ||
                !locations.ContainsKey(slot.BuildingType) ||
                !IsVacancyUnlockedForCurrentTutorial(VacancyKind.Production, slot.BuildingType))
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
                SlotIndex = slot.SlotIndex,
                RequiredEducation = WorkerEducation.Basic
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

        if (vacancy.Kind == VacancyKind.TruckDriver && selectedVacancyTruckNumber <= 0)
        {
            bool hasAvailableTruck = false;
            for (int i = 1; i <= MaxTruckCount; i++)
            {
                TruckAgent truck = GetTruckAgent(i);
                if (truck == null)
                {
                    continue;
                }

                bool shiftOccupiedOnTruck = IsTruckShiftOccupied(truck, selectedVacancyShiftIndex);
                bool hasCrewSpace = truck.AssignedDrivers.Count < 2 || HasTruckRosterDriverWithoutShift(truck);
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

        bool show = selectedVacancy?.Kind == VacancyKind.TruckDriver && !selectedVacancy.IsOccupied;
        shiftsScreenUi.VacancyTransportParkCard.gameObject.SetActive(show);
        if (!show)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        int ownedTruckCount = GetOwnedTruckCount();
        bool hasParking = locations.ContainsKey(LocationType.Parking);
        bool canHireTruck = hasParking && ownedTruckCount < MaxTruckCount && money >= HireTruckCost;
        bool hasSelectedShift = selectedVacancyShiftIndex >= 0;
        bool hasAvailableTruck = hasSelectedShift && HasAvailableTruckForVacancyShift();

        if (shiftsScreenUi.VacancyTransportParkTitleText != null)
        {
            shiftsScreenUi.VacancyTransportParkTitleText.text = ru
                ? "\u0422\u0440\u0430\u043d\u0441\u043f\u043e\u0440\u0442\u043d\u044b\u0439 \u043f\u0430\u0440\u043a"
                : "Transport Park";
        }

        if (shiftsScreenUi.VacancyTransportParkCountText != null)
        {
            shiftsScreenUi.VacancyTransportParkCountText.text = ru
                ? $"\u0413\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438: {ownedTruckCount} / {MaxTruckCount}"
                : $"Trucks: {ownedTruckCount} / {MaxTruckCount}";
        }

        if (shiftsScreenUi.VacancyTransportParkSummaryText != null)
        {
            string summary = !hasSelectedShift
                ? (ru
                    ? "\u0421\u043d\u0430\u0447\u0430\u043b\u0430 \u0432\u044b\u0431\u0435\u0440\u0438 \u0441\u043c\u0435\u043d\u0443, \u0437\u0430\u0442\u0435\u043c \u0438\u0433\u0440\u0430 \u043f\u043e\u043a\u0430\u0436\u0435\u0442 \u043f\u043e\u0434\u0445\u043e\u0434\u044f\u0449\u0438\u0435 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438."
                    : "Choose a shift first, then available trucks will appear.")
                : hasAvailableTruck
                    ? (ru
                        ? "\u0421\u0432\u043e\u0431\u043e\u0434\u043d\u044b\u0439 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a \u043d\u0430\u0439\u0434\u0435\u043d. \u0412\u044b\u0431\u0435\u0440\u0438 \u0435\u0433\u043e \u043d\u0438\u0436\u0435 \u0434\u043b\u044f \u044d\u0442\u043e\u0439 \u0441\u043c\u0435\u043d\u044b."
                        : "A free truck is available. Choose it below for this shift.")
                    : (ru
                        ? "\u0414\u043b\u044f \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u0438 \u0432\u043e\u0434\u0438\u0442\u0435\u043b\u044f \u043d\u0443\u0436\u0435\u043d \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u044b\u0439 \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a."
                        : "A truck driver vacancy needs a free truck.");
            shiftsScreenUi.VacancyTransportParkSummaryText.text = summary;
            shiftsScreenUi.VacancyTransportParkSummaryText.color = hasSelectedShift && !hasAvailableTruck
                ? new Color(0.96f, 0.72f, 0.42f, 1f)
                : FleetSecondaryTextColor;
        }

        if (shiftsScreenUi.VacancyBuyTruckButton != null)
        {
            shiftsScreenUi.VacancyBuyTruckButton.interactable = canHireTruck;
        }

        if (shiftsScreenUi.VacancyBuyTruckButtonText != null)
        {
            shiftsScreenUi.VacancyBuyTruckButtonText.text = ru
                ? $"\u041a\u0443\u043f\u0438\u0442\u044c \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a - ${HireTruckCost}"
                : $"Buy truck - ${HireTruckCost}";
        }

        if (shiftsScreenUi.VacancyBuyTruckStatusText != null)
        {
            shiftsScreenUi.VacancyBuyTruckStatusText.text = GetFleetBuyStatusLabel();
            shiftsScreenUi.VacancyBuyTruckStatusText.color = canHireTruck
                ? FleetSecondaryTextColor
                : new Color(0.96f, 0.72f, 0.42f, 1f);
        }
    }

    private bool HasAvailableTruckForVacancyShift()
    {
        if (selectedVacancyShiftIndex < 0)
        {
            return false;
        }

        for (int i = 1; i <= MaxTruckCount; i++)
        {
            TruckAgent truck = GetTruckAgent(i);
            if (truck == null)
            {
                continue;
            }

            bool shiftOccupiedOnTruck = IsTruckShiftOccupied(truck, selectedVacancyShiftIndex);
            bool hasCrewSpace = truck.AssignedDrivers.Count < 2 || HasTruckRosterDriverWithoutShift(truck);
            if (!shiftOccupiedOnTruck && hasCrewSpace)
            {
                return true;
            }
        }

        return false;
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
            VacancyKind.TruckDriver when selectedVacancyTruckNumber <= 0 => ru ? "Выбери грузовик" : "Choose Truck",
            VacancyKind.BusDriver when !HasSelectedVacancyShift(vacancy) => ru ? "Выбери смену" : "Choose Shift",
            VacancyKind.Production when !HasSelectedVacancyShift(vacancy) => ru ? "Выбери смену" : "Choose Shift",
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
            VacancyKind.TruckDriver when selectedVacancyTruckNumber <= 0 => ru ? "Теперь выбери грузовик для этой смены." : "Now choose the truck for that shift.",
            VacancyKind.BusDriver when !HasSelectedVacancyShift(vacancy) => ru ? "Сначала выбери смену городского автобуса." : "First choose the local bus shift.",
            VacancyKind.Production when !HasSelectedVacancyShift(vacancy) => ru ? "Подтверди рабочую смену этого здания." : "Confirm this building's work shift.",
            VacancyKind.Intercity when !HasSelectedVacancyShift(vacancy) => ru ? "Подтверди межгороднюю смену." : "Confirm the intercity duty shift.",
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

        if (!CanWorkerMeetEducationRequirement(driver, vacancy.RequiredEducation))
        {
            reason = ru ? "недостаточно образования" : "education too low";
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
                AssignWorkerToBuilding(worker, FindLogisticsSlot(vacancy.BuildingType, vacancy.SlotIndex));
                break;
            case VacancyKind.Intercity:
                AssignDriverToIntercitySlot(worker);
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
        TruckAgent truck = GetTruckAgent(selectedVacancyTruckNumber);
        if (truck == null || worker == null || selectedVacancyShiftIndex < 0)
        {
            return false;
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
                return false;
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

        MarkTutorialGoalComplete(TutorialGoalKind.AssignTruckDriverShift);
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
