using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void UpdateDriversScreenUi()
    {
        if (driversScreenUi == null) return;

        bool shouldShow = isDriversPanelOpen;
        if (driversScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            driversScreenUi.CanvasRoot.SetActive(shouldShow);
            isDriversScreenDirty = true;
        }

        if (!shouldShow) return;
        if (!isDriversScreenDirty) return;

        driversScreenUi.HeaderCountText.text = $"{driverAgents.Count} {(driverAgents.Count == 1 ? L("Worker") : L("Workers"))}";
        EnsureWorkerRows(driverAgents.Count);

        // Left panel — compact row list
        for (int i = 0; i < driversScreenUi.WorkerRows.Count; i++)
        {
            WorkerRowUi row = driversScreenUi.WorkerRows[i];
            bool active = i < driverAgents.Count;
            row.Root.gameObject.SetActive(active);
            if (!active)
            {
                row.DriverId = 0;
                continue;
            }

            DriverAgent d = driverAgents[i];
            TruckAgent truck = GetAssignedTruckForDriver(d);
            row.DriverId = d.DriverId;
            bool isSelected  = selectedWorkerPanelDriverId == d.DriverId;
            bool isLogistics = d.DutyMode == DriverDutyMode.Logistics;
            row.Background.color = isSelected ? DriversCardSelected : DriversCardColor;

            row.NameText.text = d.DriverName;

            bool ru = IsRussianLanguage();
            string status = GetWorkerListStatusLabel(d, ru);
            row.StatusText.text = status;
            if (row.StatusBadgeBg != null)
            {
                row.StatusBadgeBg.color = d.IsArrivingByBus
                    ? new Color(0.22f, 0.36f, 0.54f, 1f)
                    : isLogistics  ? new Color(0.20f, 0.38f, 0.26f, 1f)
                    : truck != null ? new Color(0.35f, 0.29f, 0.14f, 1f)
                    : new Color(0.24f, 0.29f, 0.36f, 1f);
            }

            row.SubText.text = d.IsArrivingByBus              ? "On the way..."
                : isLogistics && d.AssignedBuildingType.HasValue ? GetSelectedLocationDisplayName(d.AssignedBuildingType.Value)
                : truck != null                                   ? truck.DisplayName
                : L(GetWorkerOccupationLabel(d));

            if (row.NeedsMealBarFill != null)
            {
                float mealPct    = Mathf.Clamp01(1f - d.HoursSinceMeal    / WorkerMealCriticalHours);
                float sleepPct   = Mathf.Clamp01(1f - d.HoursSinceSleep   / WorkerSleepCriticalHours);
                float leisurePct = Mathf.Clamp01(1f - d.HoursSinceLeisure / WorkerLeisureCriticalHours);
                row.NeedsMealBarFill.sizeDelta    = new Vector2(mealPct    * 60f, 0f);
                row.NeedsSleepBarFill.sizeDelta   = new Vector2(sleepPct   * 60f, 0f);
                row.NeedsLeisureBarFill.sizeDelta = new Vector2(leisurePct * 60f, 0f);
                row.NeedsMealBarFill.GetComponent<Image>().color    = GetNeedBarColor(mealPct);
                row.NeedsSleepBarFill.GetComponent<Image>().color   = GetNeedBarColor(sleepPct);
                row.NeedsLeisureBarFill.GetComponent<Image>().color = GetNeedBarColor(leisurePct);
            }
        }

        // Right panel — visibility toggle (done before LocalizeCanvas so layout is correct)
        DriverAgent sel = driverAgents.Find(d => d.DriverId == selectedWorkerPanelDriverId);
        if (selectedWorkerPanelDriverId != 0 && sel == null)
        {
            selectedWorkerPanelDriverId = 0;
        }

        bool hasSel = sel != null;
        if (driversScreenUi.DetailPlaceholderCard != null)
            driversScreenUi.DetailPlaceholderCard.SetActive(!hasSel);
        if (driversScreenUi.DetailContentRoot != null)
            driversScreenUi.DetailContentRoot.SetActive(hasSel);

        bool hasMotel = locations.ContainsKey(LocationType.Motel);
        bool canHire  = hasMotel && money >= HireDriverCost && hiringDriverArrival == null;
        driversScreenUi.HireButton.interactable = canHire;
        driversScreenUi.HireButtonText.text = $"{L("Hire New Worker")} — ${HireDriverCost}";
        driversScreenUi.HireStatusText.text = hiringDriverArrival != null
            ? L("Another worker is currently arriving by bus.")
            : !hasMotel
                ? L("Build a Motel first so new workers have somewhere to check in.")
            : canHire
                ? L("New hires arrive at the bus stop before checking in at the motel.")
                : $"{L("Need")} ${HireDriverCost} {L("to hire a new worker.")}";
        driversScreenUi.HireStatusText.color = canHire ? FleetSecondaryTextColor : new Color(0.96f, 0.72f, 0.42f, 1f);

        LayoutRebuilder.ForceRebuildLayoutImmediate(driversScreenUi.WorkerListContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(driversScreenUi.WindowRoot);

        // Localize static texts first — BEFORE setting detail panel strings so they aren't re-processed
        LocalizeCanvas(driversScreenUi.CanvasRoot);

        // Set right-panel detail texts after localization to avoid substring corruption
        if (hasSel)
        {
            bool ru = IsRussianLanguage();
            TruckAgent truck = GetAssignedTruckForDriver(sel);
            bool isLogistics = sel.DutyMode == DriverDutyMode.Logistics;

            driversScreenUi.DetailNameText.text = sel.DriverName;
            if (driversScreenUi.DetailProfileTitleText != null)
                driversScreenUi.DetailProfileTitleText.text = ru ? "Профиль" : "Profile";
            if (driversScreenUi.DetailRoleText != null)
                driversScreenUi.DetailRoleText.text = $"{L(GetWorkerOccupationLabel(sel))} • {GetWorkerGenderLabel(sel, ru)} • {GetWorkerEducationDisplayName(sel.Education, ru)}";
            UpdateWorkerPortraitUi(sel);
            UpdateWorkerNeedsUi(sel, ru);

            string statusLabel = GetWorkerListStatusLabel(sel, ru);
            driversScreenUi.DetailStatusText.text = statusLabel;
            if (driversScreenUi.DetailStatusBadge != null)
            {
                driversScreenUi.DetailStatusBadge.color = sel.IsArrivingByBus
                    ? new Color(0.22f, 0.36f, 0.54f, 1f)
                    : isLogistics   ? new Color(0.20f, 0.38f, 0.26f, 1f)
                    : truck != null ? new Color(0.35f, 0.29f, 0.14f, 1f)
                    : new Color(0.24f, 0.29f, 0.36f, 1f);
            }

            // Labels (set post-localization so LocalizeCanvas can't corrupt them)
            if (driversScreenUi.DetailAssignmentLabel != null)
                driversScreenUi.DetailAssignmentLabel.text = isLogistics ? (ru ? "Здание" : "Building") : (ru ? "Грузовик" : "Truck");
            if (driversScreenUi.DetailShiftLabel != null)
                driversScreenUi.DetailShiftLabel.text = ru ? "Смена" : "Shift";
            if (driversScreenUi.DetailDutyLabel != null)
                driversScreenUi.DetailDutyLabel.text = ru ? "Статус" : "Status";
            if (driversScreenUi.DetailHomeLabel != null)
                driversScreenUi.DetailHomeLabel.text = ru ? "Жилой дом" : "Home";
            if (driversScreenUi.DetailCarLabel != null)
                driversScreenUi.DetailCarLabel.text = ru ? "\u0410\u0432\u0442\u043e" : "Car";
            if (driversScreenUi.DetailAgeLabel != null)
                driversScreenUi.DetailAgeLabel.text = ru ? "\u0412\u043e\u0437\u0440\u0430\u0441\u0442" : "Age";
            if (driversScreenUi.DetailWorkTitleText != null)
                driversScreenUi.DetailWorkTitleText.text = ru ? "Работа" : "Work";
            if (driversScreenUi.DetailSalaryLabel != null)
                driversScreenUi.DetailSalaryLabel.text = ru ? "Зарплата" : "Salary";
            if (driversScreenUi.DetailBalanceLabel != null)
                driversScreenUi.DetailBalanceLabel.text = ru ? "Баланс" : "Balance";
            if (driversScreenUi.DetailContractTitleText != null)
                driversScreenUi.DetailContractTitleText.text = ru ? "Контракт" : "Contract";

            // Values
            driversScreenUi.DetailAssignmentValue.text = isLogistics && sel.AssignedBuildingType.HasValue
                ? GetSelectedLocationDisplayName(sel.AssignedBuildingType.Value)
                : truck != null ? truck.DisplayName : "—";

            if (driversScreenUi.DetailHomeText != null)
            {
                bool hasHome = sel.AssignedPersonalHouseIndex >= 0 && sel.AssignedPersonalHouseIndex < personalHouses.Count;
                driversScreenUi.DetailHomeText.text = hasHome ? personalHouses[sel.AssignedPersonalHouseIndex].Label : "\u2014";
                driversScreenUi.DetailHomeText.color = hasHome ? Color.white : FleetMutedTextColor;
            }

            if (driversScreenUi.DetailCarText != null)
            {
                bool hasCar = sel.OwnedCarModelIndex >= 0 && sel.OwnedCarModelIndex < CarModelNames.Length;
                driversScreenUi.DetailCarText.text = hasCar ? CarModelNames[sel.OwnedCarModelIndex] : "\u2014";
                driversScreenUi.DetailCarText.color = hasCar ? Color.white : FleetMutedTextColor;
            }

            if (driversScreenUi.DetailAgeText != null)
            {
                driversScreenUi.DetailAgeText.text = sel.Age > 0 ? (ru ? $"{sel.Age} \u043b\u0435\u0442" : $"{sel.Age} y.o.") : "\u2014";
            }

            bool hasShift = sel.ShiftStartHour >= 0;
            driversScreenUi.DetailShiftText.text = isLogistics ? GetProductionWorkRangeLabel()
                : hasShift                        ? GetShiftRangeLabel(sel.ShiftStartHour)
                : (ru ? "Не назначена" : "Not assigned");
            driversScreenUi.DetailShiftText.color = (hasShift || isLogistics) ? FleetAccentColor : FleetMutedTextColor;

            string dutyState = GetWorkerDutySummaryLabel(sel, ru);
            driversScreenUi.DetailDutyStateText.text = dutyState;

            driversScreenUi.DetailSalaryText.text  = $"${sel.Salary}";
            driversScreenUi.DetailBalanceText.text = $"${sel.Money}";

            bool canFocus = sel.DriverObject != null && sel.DriverObject.activeSelf;
            driversScreenUi.DetailFocusButton.interactable = canFocus;
            driversScreenUi.DetailFocusButtonText.text = canFocus
                ? (ru ? $"Следить за {sel.DriverName}" : $"Focus on {sel.DriverName}")
                : (ru ? $"{sel.DriverName} внутри здания" : $"{sel.DriverName} is inside");
        }

        isDriversScreenDirty = false;
    }

}
