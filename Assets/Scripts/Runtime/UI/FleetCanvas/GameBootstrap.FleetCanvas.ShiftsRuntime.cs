using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void UpdateShiftsScreenUi()
    {
        if (shiftsScreenUi == null) return;

        bool shouldShow = isShiftsPanelOpen;
        if (shiftsScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            shiftsScreenUi.CanvasRoot.SetActive(shouldShow);
            isShiftsScreenDirty = true;
            LogShiftsHudState(shouldShow ? "canvas shown" : "canvas hidden", force: true);
        }

        if (!shouldShow) return;
        if (!isShiftsScreenDirty) return;

        if (UseVacanciesScreen())
        {
            UpdateVacanciesScreenUi();
            return;
        }

        bool ru = IsRussianLanguage();
        if (shiftsScreenUi.TitleText != null)
        {
            shiftsScreenUi.TitleText.text = ru ? "Роли" : "Roles";
        }
        if (shiftsScreenUi.LogisticsSectionTitleText != null)
        {
            shiftsScreenUi.LogisticsSectionTitleText.text = ru ? "Логистика" : "Logistics";
        }
        if (shiftsScreenUi.LogisticsSectionSummaryText != null)
        {
            shiftsScreenUi.LogisticsSectionSummaryText.text = ru
                ? "Смены отвечают за локальные рейсы. Межгород резервирует отдельного рабочего под торговлю и выезды по магистрали."
                : "Shifts handle local delivery work. Intercity reserves a dedicated worker for trade and highway trips.";
        }
        if (shiftsScreenUi.ProductionSectionTitleText != null)
        {
            shiftsScreenUi.ProductionSectionTitleText.text = ru ? "Здания" : "Buildings";
        }
        if (shiftsScreenUi.ProductionSectionSummaryText != null)
        {
            shiftsScreenUi.ProductionSectionSummaryText.text = ru
                ? "Здесь рабочий закрепляется прямо за производственным или сервисным зданием и трудится по графику 08:00-18:00."
                : "Assign workers directly to production and service buildings on an 08:00-18:00 schedule.";
        }
        shiftsScreenUi.HeaderCountText.text = $"{driverAgents.Count} {(driverAgents.Count == 1 ? L("Worker") : L("Workers"))}";

        EnforceShiftsWindowLayout();
        ApplyShiftsTabVisuals();
        if (shiftsLogisticsPanel != null) shiftsLogisticsPanel.gameObject.SetActive(isLogisticsTabActive);
        if (shiftsTransportPanel  != null) shiftsTransportPanel.gameObject.SetActive(!isLogisticsTabActive);

        for (int i = 0; i < shiftsScreenUi.DriverRows.Count; i++)
        {
            ShiftDriverRowUi row = shiftsScreenUi.DriverRows[i];
            bool active = i < driverAgents.Count;
            row.Root.gameObject.SetActive(active);
            if (!active) continue;

            DriverAgent driver = driverAgents[i];
            row.DriverId = driver.DriverId;
            bool isSelected = selectedShiftDriverId == driver.DriverId;
            row.Background.color = isSelected ? ShiftsCardSelected : ShiftsCardColor;
            row.NameText.text = driver.DriverName;
            if (row.ProfessionText != null)
            {
                row.ProfessionText.text = L(GetWorkerOccupationLabel(driver));
                row.ProfessionText.color = driver.AssignedTruckNumber > 0 || driver.DutyMode != DriverDutyMode.Local
                    ? FleetAccentColor
                    : FleetSecondaryTextColor;
            }

            bool isAssigned = driver.ShiftStartHour >= 0;
            bool isIntercity = IsDriverIntercity(driver);
            bool isBusDriver = IsDriverBusDriver(driver);
            bool isProduction = driver.DutyMode == DriverDutyMode.Logistics;
            bool isService = isProduction && driver.AssignedBuildingType.HasValue && HasServiceWorkerSlot(driver.AssignedBuildingType.Value);
            bool isTruckAssigned = driver.AssignedTruckNumber > 0;
            row.StatusText.text = isIntercity
                ? L("Intercity")
                : isBusDriver
                    ? (ru ? $"Автобус: {GetShiftRangeLabel(driver.ShiftStartHour)}" : $"Bus: {GetShiftRangeLabel(driver.ShiftStartHour)}")
                : isTruckAssigned
                    ? L("Logistics")
                    : isProduction
                        ? (isService ? L("Service") : L("Production"))
                        : isAssigned
                            ? $"{L("Assigned")}: {GetShiftRangeLabel(driver.ShiftStartHour)}"
                            : L("Idle");
            row.StatusText.color = isIntercity || isBusDriver || isProduction || isTruckAssigned
                ? FleetAccentColor
                : isAssigned ? new Color(0.62f, 0.92f, 0.62f, 1f) : FleetMutedTextColor;
        }

        DriverAgent selectedDriver = driverAgents.Find(driver => driver.DriverId == selectedShiftDriverId);
        if (shiftsScreenUi.SelectionTitleText != null)
        {
            shiftsScreenUi.SelectionTitleText.text = ru ? "Выбранный рабочий" : "Selected Worker";
        }
        if (selectedDriver == null)
        {
            if (shiftsScreenUi.SelectionNameText != null)
            {
                shiftsScreenUi.SelectionNameText.text = ru ? "Никто не выбран" : "No worker selected";
                shiftsScreenUi.SelectionNameText.color = Color.white;
            }
            if (shiftsScreenUi.SelectionProfessionText != null)
            {
                shiftsScreenUi.SelectionProfessionText.text = ru
                    ? "Статус назначения"
                    : "Assignment status";
            }
            if (shiftsScreenUi.SelectionStatusText != null)
            {
                shiftsScreenUi.SelectionStatusText.text = ru
                    ? "Доступные назначения появятся после выбора"
                    : "Available assignments appear after selection";
                shiftsScreenUi.SelectionStatusText.color = FleetSecondaryTextColor;
            }
            if (shiftsScreenUi.SelectionHintText != null)
            {
                shiftsScreenUi.SelectionHintText.text = string.Empty;
            }
        }
        else
        {
            if (shiftsScreenUi.SelectionNameText != null)
            {
                shiftsScreenUi.SelectionNameText.text = selectedDriver.DriverName;
                shiftsScreenUi.SelectionNameText.color = FleetAccentColor;
            }
            if (shiftsScreenUi.SelectionProfessionText != null)
            {
                shiftsScreenUi.SelectionProfessionText.text = L(GetWorkerOccupationLabel(selectedDriver));
            }

            string workerStatusSummary = IsDriverIntercity(selectedDriver)
                ? (ru ? "Закреплён за междугородними рейсами" : "Assigned to intercity duty")
                : IsDriverBusDriver(selectedDriver)
                    ? (ru ? $"Закреплён за автобусной сменой {GetShiftRangeLabel(selectedDriver.ShiftStartHour)}" : $"Assigned to bus duty {GetShiftRangeLabel(selectedDriver.ShiftStartHour)}")
                : selectedDriver.AssignedTruckNumber > 0
                    ? (ru ? $"Закреплён за грузовиком #{selectedDriver.AssignedTruckNumber}" : $"Assigned to Truck #{selectedDriver.AssignedTruckNumber}")
                    : selectedDriver.DutyMode == DriverDutyMode.Logistics && selectedDriver.AssignedBuildingType.HasValue
                        ? (ru ? $"Работает в {GetSelectedLocationDisplayName(selectedDriver.AssignedBuildingType.Value)}" : $"Working at {GetSelectedLocationDisplayName(selectedDriver.AssignedBuildingType.Value)}")
                        : selectedDriver.ShiftStartHour >= 0
                            ? (ru ? $"Логистическая смена: {GetShiftRangeLabel(selectedDriver.ShiftStartHour)}" : $"Logistics shift: {GetShiftRangeLabel(selectedDriver.ShiftStartHour)}")
                            : (ru ? Gend(selectedDriver, "Свободен для назначения", "Свободна для назначения") : "Available for assignment");
            if (shiftsScreenUi.SelectionStatusText != null)
            {
                shiftsScreenUi.SelectionStatusText.text = workerStatusSummary;
                shiftsScreenUi.SelectionStatusText.color =
                    (selectedDriver.DutyMode != DriverDutyMode.Local || selectedDriver.AssignedTruckNumber > 0 || selectedDriver.ShiftStartHour >= 0 || IsDriverBusDriver(selectedDriver))
                    ? FleetAccentColor
                    : Color.white;
            }

            if (shiftsScreenUi.SelectionHintText != null)
            {
                shiftsScreenUi.SelectionHintText.text = isLogisticsTabActive
                    ? (ru ? "Производство: прямое назначение в здание."
                        : "Production: direct building assignment.")
                    : (ru ? "Логистика: смены и межгород."
                        : "Logistics: shifts and intercity.");
            }
        }

        if (shiftsScreenUi.TransportScrollRect != null)
        {
            shiftsScreenUi.TransportScrollRect.verticalNormalizedPosition = 1f;
        }

        if (shiftsScreenUi.ProductionScrollRect != null)
        {
            shiftsScreenUi.ProductionScrollRect.verticalNormalizedPosition = 1f;
        }

        for (int i = 0; i < shiftsScreenUi.ShiftCards.Count; i++)
        {
            ShiftCardUi card = shiftsScreenUi.ShiftCards[i];
            List<DriverAgent> assignedDrivers = new();
            foreach (DriverAgent driver in driverAgents)
            {
                if (driver.DutyMode == DriverDutyMode.Local &&
                    driver.ShiftStartHour == card.ShiftHour &&
                    !IsDriverBusDriver(driver))
                {
                    assignedDrivers.Add(driver);
                }
            }

            bool isActiveShift = IsHourInShiftWindow(GetCurrentHour(), card.ShiftHour);
            if (card.ActiveBorderImage != null)
                card.ActiveBorderImage.color = isActiveShift ? new Color(1f, 0.85f, 0.25f, 1f) : Color.clear;

            if (card.SummaryText != null)
            {
                string shiftPrefix = ru ? "Локальные рейсы" : "Local deliveries";
                string summary = assignedDrivers.Count switch
                {
                    0 => ru ? "никто не назначен" : "no workers assigned",
                    1 => ru ? "1 рабочий назначен" : "1 worker assigned",
                    _ => ru ? $"{assignedDrivers.Count} рабочих назначено" : $"{assignedDrivers.Count} workers assigned"
                };
                string timerSuffix = string.Empty;
                if (isActiveShift)
                {
                    int endMin = ((card.ShiftHour + 8) % 24) * 60;
                    int minLeft = (endMin - GetCurrentTotalMinutes() + 24 * 60) % (24 * 60);
                    timerSuffix = ru ? $" • осталось {minLeft / 60:00}:{minLeft % 60:00}" : $" • {minLeft / 60:00}:{minLeft % 60:00} left";
                }
                card.SummaryText.text = $"{shiftPrefix} • {summary}{timerSuffix}";
            }

            card.EmptyText.gameObject.SetActive(assignedDrivers.Count == 0);
            for (int rowIndex = 0; rowIndex < card.AssignedRows.Count; rowIndex++)
            {
                bool rowActive = rowIndex < assignedDrivers.Count;
                card.AssignedRows[rowIndex].SetActive(rowActive);
                if (!rowActive) continue;

                card.AssignedDriverTexts[rowIndex].text = assignedDrivers[rowIndex].DriverName;
            }

            bool alreadyAssigned = selectedDriver != null && selectedDriver.DutyMode == DriverDutyMode.Local && selectedDriver.ShiftStartHour == card.ShiftHour;
            bool intercitySelected = IsDriverIntercity(selectedDriver);
            bool busDriverSelected = IsDriverBusDriver(selectedDriver);
            bool productionSelected = selectedDriver?.DutyMode == DriverDutyMode.Logistics;
            card.AssignButton.interactable = selectedDriver != null && !alreadyAssigned && !intercitySelected && !productionSelected && !busDriverSelected;
            card.AssignButtonText.text = selectedDriver == null
                ? L("Select a worker to assign")
                : (intercitySelected || productionSelected || busDriverSelected)
                    ? L("Worker not available")
                    : alreadyAssigned
                        ? (ru ? $"{selectedDriver.DriverName} уже назначен" : $"{selectedDriver.DriverName} already assigned")
                        : (ru ? $"Назначить {selectedDriver.DriverName} -> {ShiftNames[i]}" : $"Assign {selectedDriver.DriverName} -> {ShiftNames[i]}");
        }

        UpdateIntercitySlotUi(selectedDriver);
        UpdateBusDriverSlotsUi(selectedDriver);
        UpdateEmbeddedFleetUi(selectedDriver);

        if (isLogisticsTabActive)
        {
            UpdateLogisticsTabUi(selectedDriver);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(shiftsScreenUi.DriverListContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(shiftsScreenUi.WindowRoot);
        EnforceShiftsWindowLayout();
        ApplyShiftsTabVisuals();
        LogShiftsHudState("rebuilt Shifts canvas");
        LocalizeCanvas(shiftsScreenUi.CanvasRoot);
        isShiftsScreenDirty = false;
    }

    private void EnforceShiftsWindowLayout()
    {
        if (shiftsScreenUi?.WindowRoot == null)
        {
            return;
        }

        SetCenteredWindow(shiftsScreenUi.WindowRoot, ShiftsWindowWidth, ShiftsWindowHeight, -16f);
        ApplyFixedLayoutSize(shiftsScreenUi.LeftPanel, ShiftsLeftPanelWidth, ShiftsInnerPanelHeight);
        ApplyFixedLayoutSize(shiftsScreenUi.RightPanel, ShiftsRightPanelWidth, ShiftsInnerPanelHeight);
        float tabContentHeight = GetShiftsTabContentHeight();
        ApplyFixedLayoutHeight(shiftsTransportPanel, tabContentHeight);
        ApplyFixedLayoutHeight(shiftsLogisticsPanel, tabContentHeight);
        ApplyFixedLayoutHeight(shiftsScreenUi.VacancyFlowPanel, tabContentHeight + ShiftsTabRowHeight + 12f);
    }

    private void LogShiftsHudState(string reason, bool force = false)
    {
        string state = BuildShiftsHudDebugState(reason);
        if (!force && state == lastShiftsHudDebugState)
        {
            return;
        }

        lastShiftsHudDebugState = state;
        SessionDebugLogger.LogVerbose("SHIFTS_HUD", state);
    }

    private string BuildShiftsHudDebugState(string reason)
    {
        string canvasActive = shiftsScreenUi?.CanvasRoot != null ? shiftsScreenUi.CanvasRoot.activeSelf.ToString() : "null";
        string windowSize = FormatRectSize(shiftsScreenUi?.WindowRoot);
        string leftSize = FormatRectSize(shiftsScreenUi?.LeftPanel);
        string rightSize = FormatRectSize(shiftsScreenUi?.RightPanel);
        string transportPanel = FormatPanelState(shiftsTransportPanel);
        string productionPanel = FormatPanelState(shiftsLogisticsPanel);
        string logisticsTabColor = FormatButtonColor(shiftsTransportTabBtn);
        string productionsTabColor = FormatButtonColor(shiftsLogisticsTabBtn);
        string logisticsTextColor = FormatTextColor(shiftsTransportTabText);
        string productionsTextColor = FormatTextColor(shiftsLogisticsTabText);
        string activeTab = isLogisticsTabActive ? "Productions" : "Logistics";
        string labels = $"labels(L='{shiftsTransportTabText?.text ?? "null"}', P='{shiftsLogisticsTabText?.text ?? "null"}')";

        return $"{reason}: open={isShiftsPanelOpen}, dirty={isShiftsScreenDirty}, activeTab={activeTab}, " +
               $"canvasActive={canvasActive}, window={windowSize}, left={leftSize}, right={rightSize}, " +
               $"transport={transportPanel}, productions={productionPanel}, " +
               $"tabColors(Logistics={logisticsTabColor}, Productions={productionsTabColor}), " +
               $"textColors(Logistics=#{logisticsTextColor}, Productions=#{productionsTextColor}), {labels}";
    }

    private static string FormatPanelState(RectTransform rect)
    {
        if (rect == null)
        {
            return "null";
        }

        return $"{rect.gameObject.activeSelf}/{FormatRectSize(rect)}";
    }

    private static string FormatRectSize(RectTransform rect)
    {
        if (rect == null)
        {
            return "null";
        }

        return $"{rect.rect.width:0.#}x{rect.rect.height:0.#}/delta({rect.sizeDelta.x:0.#},{rect.sizeDelta.y:0.#})";
    }

    private static string FormatButtonColor(Button button)
    {
        if (button == null)
        {
            return "null";
        }

        Image image = button.targetGraphic as Image ?? button.image;
        string imageColor = image != null ? ColorUtility.ToHtmlStringRGBA(image.color) : "no-image";
        return $"image=#{imageColor}, normal=#{ColorUtility.ToHtmlStringRGBA(button.colors.normalColor)}";
    }

    private static string FormatTextColor(Text text)
    {
        return text != null ? ColorUtility.ToHtmlStringRGBA(text.color) : "null";
    }

    private static Scrollbar CreatePanelScrollbar(string name, Transform parent)
    {
        return FleetCanvasUiFactory.CreateVerticalScrollbar(
            name,
            parent,
            new Color(0.09f, 0.12f, 0.17f, 0.96f),
            FleetPrimaryButtonColor);
    }

    private void ApplyShiftsTabVisuals()
    {
        ApplyShiftsTabVisual(shiftsTransportTabBtn, shiftsTransportTabText, !isLogisticsTabActive);
        ApplyShiftsTabVisual(shiftsLogisticsTabBtn, shiftsLogisticsTabText, isLogisticsTabActive);
    }

    private static void ApplyShiftsTabVisual(Button button, Text label, bool isActive)
    {
        if (button == null)
        {
            return;
        }

        Color activeColor = FleetPrimaryButtonColor;
        Color inactiveColor = new(0.08f, 0.10f, 0.14f, 1f);
        Color targetColor = isActive ? activeColor : inactiveColor;
        Image image = button.targetGraphic as Image ?? button.image;
        if (image != null)
        {
            image.color = targetColor;
        }

        ColorBlock colors = button.colors;
        colors.normalColor = targetColor;
        colors.highlightedColor = isActive ? activeColor : Color.Lerp(inactiveColor, FleetPrimaryButtonColor, 0.24f);
        colors.pressedColor = Color.Lerp(targetColor, Color.black, 0.14f);
        colors.selectedColor = targetColor;
        colors.disabledColor = targetColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;

        if (label != null)
        {
            label.color = isActive ? FleetAccentColor : Color.white;
            label.fontStyle = isActive ? FontStyle.BoldAndItalic : FontStyle.Bold;
        }
    }

    private void UpdateEmbeddedFleetUi(DriverAgent selectedDriver)
    {
        if (shiftsScreenUi == null)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        if (shiftsScreenUi.FleetSectionTitleText != null)
        {
            shiftsScreenUi.FleetSectionTitleText.text = ru ? "\u0410\u0432\u0442\u043e\u043f\u0430\u0440\u043a" : "Fleet";
        }

        if (shiftsScreenUi.FleetCountText != null)
        {
            shiftsScreenUi.FleetCountText.text = $"{GetOwnedTruckCount()} / {GetTruckParkingCapacity()} {(ru ? "\u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u043e\u0432" : "trucks")}";
        }

        if (shiftsScreenUi.FleetSectionSummaryText != null)
        {
            shiftsScreenUi.FleetSectionSummaryText.text = ru
                ? "Parking даёт слоты автопарка. Транспорт появляется автоматически, когда водитель получает смену."
                : "Parking provides fleet slots. Vehicles appear automatically when a driver gets a shift.";
        }

        if (shiftsScreenUi.FleetBuyTruckButton != null)
        {
            shiftsScreenUi.FleetBuyTruckButton.interactable = false;
        }
        if (shiftsScreenUi.FleetBuyTruckButtonText != null)
        {
            shiftsScreenUi.FleetBuyTruckButtonText.text = ru
                ? "Слоты Parking"
                : "Parking Slots";
        }
        if (shiftsScreenUi.FleetBuyTruckStatusText != null)
        {
            shiftsScreenUi.FleetBuyTruckStatusText.text = GetFleetBuyStatusLabel();
            shiftsScreenUi.FleetBuyTruckStatusText.color = locations.ContainsKey(LocationType.Parking) ? FleetSecondaryTextColor : new Color(0.96f, 0.72f, 0.42f, 1f);
        }

        for (int i = 0; i < shiftsScreenUi.FleetTruckRows.Count; i++)
        {
            ShiftsFleetTruckRowUi row = shiftsScreenUi.FleetTruckRows[i];
            TruckAgent truck = GetTruckAgent(row.TruckNumber);
            bool active = truck != null;
            row.Root.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            bool selected = selectedTruckNumber == truck.TruckNumber;
            row.Background.color = selected ? ShiftsCardSelected : ShiftsCardColor;
            row.NameText.text = truck.DisplayName;
            row.StatusText.text = L(GetTruckListStatusForFleet(truck));
            row.CrewText.text = ru
                ? $"\u042d\u043a\u0438\u043f\u0430\u0436: {GetTruckAssignedDriverSummary(truck)}"
                : $"Crew: {GetTruckAssignedDriverSummary(truck)}";
            row.CargoText.text = FormatTruckCargoValue(truck.TruckCargoAmount, truck.TruckCargoType);

            bool canAssignSelected = selectedDriver != null && CanAssignDriverToTruckRoster(truck, selectedDriver);
            row.AssignButton.interactable = canAssignSelected;
            row.AssignButtonText.text = selectedDriver == null
                ? (ru ? "\u0412\u044b\u0431\u0435\u0440\u0438" : "Select")
                : truck.AssignedDrivers.Count >= 2
                    ? (ru ? "\u042d\u043a\u0438\u043f\u0430\u0436 \u043f\u043e\u043b\u043e\u043d" : "Crew full")
                    : canAssignSelected
                        ? (ru ? "\u0412 \u044d\u043a\u0438\u043f\u0430\u0436" : "Assign")
                        : (ru ? "\u041d\u0435\u043b\u044c\u0437\u044f" : "Blocked");
        }
    }

    private void UpdateIntercitySlotUi(DriverAgent selectedDriver)
    {
        if (shiftsScreenUi?.IntercitySlot == null)
        {
            return;
        }

        DriverAgent intercityDriver = GetIntercityAssignedDriver();
        bool selectedIsIntercity = IsDriverIntercity(selectedDriver);
        bool selectedIsIdleForIntercity = selectedDriver != null &&
                                          !selectedDriver.IsArrivingByBus &&
                                          !selectedDriver.IsLeavingTown &&
                                          !selectedDriver.HasDepartedTown &&
                                          !selectedIsIntercity;
        bool ru = IsRussianLanguage();
        if (shiftsScreenUi.IntercitySlot.HeaderText != null)
        {
            shiftsScreenUi.IntercitySlot.HeaderText.text = ru ? "Межгород" : "Intercity";
        }
        if (shiftsScreenUi.IntercitySlot.SummaryText != null)
        {
            shiftsScreenUi.IntercitySlot.SummaryText.text = ru
                ? "Отдельный рабочий для торговли и рейсов за пределы текущего региона."
                : "Dedicated worker for trade and trips beyond the current region.";
        }
        shiftsScreenUi.IntercitySlot.AssignedDriverText.text = intercityDriver != null ? intercityDriver.DriverName : L("No worker assigned");
        shiftsScreenUi.IntercitySlot.StatusText.text = intercityDriver != null
            ? (ru ? "Закреплён под междугородние рейсы" : "Reserved for future trade runs")
            : (ru ? "Назначьте отдельного рабочего на междугородние рейсы" : "Assign one dedicated worker to intercity duty");
        shiftsScreenUi.IntercitySlot.AssignButton.interactable = selectedIsIdleForIntercity && !selectedIsIntercity;
        shiftsScreenUi.IntercitySlot.AssignButtonText.text = selectedDriver == null
            ? L("Select a worker")
            : (selectedIsIntercity || !selectedIsIdleForIntercity)
                ? L("Worker not available")
                : (ru ? $"Назначить {selectedDriver.DriverName}" : $"Assign {selectedDriver.DriverName}");
        shiftsScreenUi.IntercitySlot.RemoveButton.interactable = intercityDriver != null;
    }

    private void UpdateBusDriverSlotsUi(DriverAgent selectedDriver)
    {
        if (shiftsScreenUi == null || shiftsScreenUi.BusDriverSlots.Count == 0)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        if (shiftsScreenUi.BusDriverGroupTitleText != null)
        {
            shiftsScreenUi.BusDriverGroupTitleText.text = ru ? "Водитель автобуса" : "Bus Driver";
        }

        if (shiftsScreenUi.BusDriverGroupSummaryText != null)
        {
            shiftsScreenUi.BusDriverGroupSummaryText.text = ru
                ? "Одна должность, три смены. Водители сменяют друг друга."
                : "One role, three shift slots. Drivers hand the route over to each other.";
        }

        bool selectedIsAvailable = selectedDriver != null &&
                                   !selectedDriver.IsArrivingByBus &&
                                   !selectedDriver.IsLeavingTown &&
                                   !selectedDriver.HasDepartedTown &&
                                   !IsDriverIntercity(selectedDriver) &&
                                   selectedDriver.DutyMode == DriverDutyMode.Local &&
                                   selectedDriver.AssignedTruckNumber <= 0 &&
                                   !IsDriverOnActiveTradeRun(selectedDriver) &&
                                   !IsBusDriverOnActiveRoute(selectedDriver);
        int selectedBusSlotIndex = GetBusDriverShiftSlotIndex(selectedDriver);
        bool selectedAlreadyBusDriver = selectedBusSlotIndex >= 0;

        for (int i = 0; i < shiftsScreenUi.BusDriverSlots.Count && i < ShiftPresetHours.Length; i++)
        {
            IntercitySlotUi slot = shiftsScreenUi.BusDriverSlots[i];
            DriverAgent busDriver = GetBusAssignedDriver(i);
            bool selectedAlreadyInThisSlot = selectedBusSlotIndex == i;
            string shiftLabel = $"{L(ShiftNames[i])} {GetShiftRangeLabel(ShiftPresetHours[i])}";
            string assignedLabel = busDriver != null ? busDriver.DriverName : "—";

            slot.AssignedDriverText.text = $"{shiftLabel}: {assignedLabel}";
            slot.AssignButton.interactable = selectedIsAvailable && !selectedAlreadyBusDriver && !selectedAlreadyInThisSlot && busDriver == null;
            slot.AssignButtonText.text = selectedDriver == null
                ? (ru ? "Выбери рабочего" : "Select worker")
                : busDriver != null
                    ? (ru ? "Занято" : "Occupied")
                    : selectedAlreadyBusDriver
                        ? (ru ? "Уже назначен" : "Already assigned")
                        : !selectedIsAvailable
                            ? (ru ? "Недоступен" : "Unavailable")
                            : (ru ? "Назначить" : "Assign");
            slot.RemoveButton.interactable = busDriver != null && !IsBusDriverOnActiveRoute(busDriver);
        }
    }

    private DriverAgent GetIntercityAssignedDriver()
    {
        return driverAgents.Find(driver => driver.DriverId == intercityDriverId && IsDriverIntercity(driver));
    }

    private DriverAgent GetBusAssignedDriver(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= busDriverShiftIds.Length)
        {
            return null;
        }

        int driverId = busDriverShiftIds[slotIndex];
        return driverId <= 0 ? null : driverAgents.Find(driver => driver.DriverId == driverId);
    }

    private void AssignDriverToIntercitySlot(DriverAgent driver)
    {
        if (driver == null) return;
        if (driver.IsArrivingByBus || driver.IsLeavingTown || driver.HasDepartedTown || driver.DutyMode == DriverDutyMode.Intercity) return;

        if (HasActiveTradeRun())
        {
            tradeDispatchStatusText = "Wait for the active trade run to finish";
            isEconomyScreenDirty = true;
            return;
        }

        DriverAgent currentIntercity = GetIntercityAssignedDriver();
        if (currentIntercity != null && currentIntercity != driver)
        {
            TruckAgent currentTruck = GetAssignedTruckForDriver(currentIntercity);
            if (currentTruck != null)
            {
                UnassignDriverFromTruck(currentTruck, currentIntercity);
            }
            SetDriverDutyMode(currentIntercity, DriverDutyMode.Local);
        }

        SetDriverDutyMode(driver, DriverDutyMode.Intercity);
        intercityDriverId = driver.DriverId;
        PlayUiSound(uiSelectClip, 0.85f);
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} assigned to Intercity slot.");
        LogDriverReaction(driver, "assigned to Intercity duty");
        PushFeedEvent(
            $"{driver.DriverName} reserved for intercity duty.",
            $"{driver.DriverName} закреплён за междугородними рейсами.",
            FeedEventType.Info);
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void AssignDriverToBusSlot(DriverAgent driver, int slotIndex)
    {
        if (driver == null || driver.IsArrivingByBus || driver.IsLeavingTown || driver.HasDepartedTown)
        {
            return;
        }

        if (slotIndex < 0 || slotIndex >= ShiftPresetHours.Length)
        {
            return;
        }

        if (driver.AssignedTruckNumber > 0 || driver.DutyMode == DriverDutyMode.Logistics || IsDriverIntercity(driver))
        {
            return;
        }

        if (!EnsureBusProvisionedForAssignment("bus vacancy assignment"))
        {
            SessionDebugLogger.Log("BUS_SHIFT", $"{driver.DriverName} bus assignment blocked: no Parking bus slot available.");
            LogDriverReaction(driver, "cannot start bus duty: no Parking bus slot available");
            return;
        }

        DriverAgent currentBusDriver = GetBusAssignedDriver(slotIndex);
        if (currentBusDriver != null && currentBusDriver != driver)
        {
            currentBusDriver.ShiftStartHour = -1;
            currentBusDriver.IsOnActiveShift = false;
            currentBusDriver.WaitingForShiftAtParking = false;
            currentBusDriver.NeedsShiftEndReturn = false;
            busDriverShiftIds[slotIndex] = 0;
        }

        int previousSlotIndex = GetBusDriverShiftSlotIndex(driver);
        if (previousSlotIndex >= 0 && previousSlotIndex != slotIndex)
        {
            busDriverShiftIds[previousSlotIndex] = 0;
        }

        busDriverShiftIds[slotIndex] = driver.DriverId;
        driver.ShiftStartHour = ShiftPresetHours[slotIndex];
        driver.IsOnActiveShift = false;
        driver.WaitingForShiftAtParking = false;
        driver.NeedsShiftEndReturn = false;
        PlayUiSound(uiSelectClip, 0.85f);
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} assigned to Bus Driver slot {ShiftNames[slotIndex]} ({GetShiftRangeLabel(ShiftPresetHours[slotIndex])}).");
        SessionDebugLogger.Log("BUS_SHIFT", $"{driver.DriverName} reserved as Bus Driver for {ShiftNames[slotIndex]} ({GetShiftRangeLabel(ShiftPresetHours[slotIndex])}).");
        PushFeedEvent(
            $"{driver.DriverName} assigned as bus driver for {ShiftNames[slotIndex]}.",
            $"{driver.DriverName} назначен водителем автобуса на смену {L(ShiftNames[slotIndex])}.",
            FeedEventType.Info);
        bool inWindow = IsHourInShiftWindow(GetCurrentHour(), ShiftPresetHours[slotIndex]);
        if (inWindow && driver.RestPhase == DriverRestPhase.None)
        {
            StartBusDriverShiftCommute(driver);
        }
        LogDriverReaction(driver, $"assigned to bus duty {ShiftNames[slotIndex]} ({GetShiftRangeLabel(ShiftPresetHours[slotIndex])})");
        NotifyTutorialBusDriverAssigned();
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void RemoveIntercityDriverAssignment()
    {
        DriverAgent intercityDriver = GetIntercityAssignedDriver();
        if (intercityDriver == null)
        {
            return;
        }

        if (IsDriverOnActiveTradeRun(intercityDriver))
        {
            tradeDispatchStatusText = "Intercity driver is currently on a trade run";
            isEconomyScreenDirty = true;
            return;
        }

        SetDriverDutyMode(intercityDriver, DriverDutyMode.Local);
        TruckAgent assignedTruck = GetAssignedTruckForDriver(intercityDriver);
        if (assignedTruck != null)
        {
            UnassignDriverFromTruck(assignedTruck, intercityDriver);
        }
        intercityDriverId = 0;
        PlayUiSound(uiSelectClip, 0.85f);
        SessionDebugLogger.Log("SHIFT", $"{intercityDriver.DriverName} removed from Intercity slot.");
        LogDriverReaction(intercityDriver, "returned from Intercity duty to local pool");
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

    private void RemoveBusDriverAssignment(int slotIndex)
    {
        DriverAgent busDriver = GetBusAssignedDriver(slotIndex);
        if (busDriver == null)
        {
            return;
        }

        busDriverShiftIds[slotIndex] = 0;
        busDriver.ShiftStartHour = -1;
        busDriver.IsOnActiveShift = false;
        busDriver.WaitingForShiftAtParking = false;
        busDriver.NeedsShiftEndReturn = false;
        SessionDebugLogger.Log("SHIFT", $"{busDriver.DriverName} removed from Bus Driver slot {ShiftNames[slotIndex]}.");
        LogDriverReaction(busDriver, $"removed from bus duty {ShiftNames[slotIndex]}");
        isShiftsScreenDirty = true;
        isDriversScreenDirty = true;
    }

}
