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

        driversScreenUi.HeaderCountText.text = $"{driverAgents.Count} {(driverAgents.Count == 1 ? L("Resident") : L("Residents"))}";
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

            row.SubText.text = d.IsArrivingByBus              ? (ru ? "\u0412 \u043f\u0443\u0442\u0438..." : "On the way...")
                : isLogistics && d.AssignedBuildingType.HasValue ? GetSelectedLocationDisplayName(d.AssignedBuildingType.Value)
                : truck != null                                   ? truck.DisplayName
                : L(GetWorkerOccupationLabel(d));
            if (row.BalanceText != null)
            {
                row.BalanceText.text = $"${d.Money}";
                row.BalanceText.color = d.Money < 15 ? new Color(0.96f, 0.72f, 0.42f, 1f) : FleetAccentColor;
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

        LayoutRebuilder.ForceRebuildLayoutImmediate(driversScreenUi.WorkerListContent);
        LayoutRebuilder.ForceRebuildLayoutImmediate(driversScreenUi.WindowRoot);
        ScrollWorkersListToSelectedIfRequested();

        // Localize static texts first — BEFORE setting detail panel strings so they aren't re-processed
        LocalizeCanvas(driversScreenUi.CanvasRoot);

        // Set right-panel detail texts after localization to avoid substring corruption
        if (hasSel)
        {
            bool ru = IsRussianLanguage();
            TruckAgent truck = GetAssignedTruckForDriver(sel);
            bool isLogistics = sel.DutyMode == DriverDutyMode.Logistics;

            ApplyWorkerDetailTabUi(ru);
            driversScreenUi.DetailNameText.text = sel.DriverName;
            if (driversScreenUi.DetailProfileTitleText != null)
                driversScreenUi.DetailProfileTitleText.text = ru ? "\u041f\u0440\u043e\u0444\u0438\u043b\u044c" : "Profile";
            if (driversScreenUi.DetailRoleText != null)
                driversScreenUi.DetailRoleText.text = $"{L(GetWorkerOccupationLabel(sel))} | {GetWorkerGenderLabel(sel, ru)} | {GetWorkerEducationDisplayName(sel.Education, ru)} | {FormatWorkerProfessionalSummary(sel, ru)}";
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
                driversScreenUi.DetailAssignmentLabel.text = isLogistics ? (ru ? "\u0417\u0434\u0430\u043d\u0438\u0435" : "Building") : (ru ? "\u0413\u0440\u0443\u0437\u043e\u0432\u0438\u043a" : "Truck");
            if (driversScreenUi.DetailShiftLabel != null)
                driversScreenUi.DetailShiftLabel.text = ru ? "\u0421\u043c\u0435\u043d\u0430" : "Shift";
            if (driversScreenUi.DetailDutyLabel != null)
                driversScreenUi.DetailDutyLabel.text = ru ? "\u0421\u0442\u0430\u0442\u0443\u0441" : "Status";
            if (driversScreenUi.DetailHomeLabel != null)
                driversScreenUi.DetailHomeLabel.text = ru ? "\u0416\u0438\u043b\u044c\u0435" : "Home";
            if (driversScreenUi.DetailCarLabel != null)
                driversScreenUi.DetailCarLabel.text = ru ? "\u0410\u0432\u0442\u043e" : "Car";
            if (driversScreenUi.DetailAgeLabel != null)
                driversScreenUi.DetailAgeLabel.text = ru ? "\u0412\u043e\u0437\u0440\u0430\u0441\u0442" : "Age";
            if (driversScreenUi.DetailWorkTitleText != null)
                driversScreenUi.DetailWorkTitleText.text = ru ? "\u0420\u0430\u0431\u043e\u0442\u0430" : "Work";
            if (driversScreenUi.DetailBalanceLabel != null)
                driversScreenUi.DetailBalanceLabel.text = ru ? "\u0411\u0430\u043b\u0430\u043d\u0441" : "Balance";

            // Values
            driversScreenUi.DetailAssignmentValue.text = isLogistics && sel.AssignedBuildingType.HasValue
                ? GetSelectedLocationDisplayName(sel.AssignedBuildingType.Value)
                : truck != null ? truck.DisplayName : "\u2014";

            if (driversScreenUi.DetailHomeText != null)
            {
                bool hasHome = sel.AssignedPersonalHouseIndex >= 0 && sel.AssignedPersonalHouseIndex < personalHouses.Count;
                driversScreenUi.DetailHomeText.text = hasHome ? FormatWorkerHomeLabel(sel, ru) : "\u2014";
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
                : (ru ? "\u041d\u0435 \u043d\u0430\u0437\u043d\u0430\u0447\u0435\u043d\u0430" : "Not assigned");
            driversScreenUi.DetailShiftText.color = (hasShift || isLogistics) ? FleetAccentColor : FleetMutedTextColor;

            string dutyState = GetWorkerDutySummaryLabel(sel, ru);
            driversScreenUi.DetailDutyStateText.text = dutyState;

            driversScreenUi.DetailBalanceText.text = $"${sel.Money}";
            UpdateWorkerSocialUi(sel, ru);
            UpdateWorkerThoughtsUi(sel, ru);
            UpdateWorkerInventoryUi(sel, ru);

            bool canFocus = CanFocusDriver(sel);
            driversScreenUi.DetailFocusButton.interactable = canFocus;
            driversScreenUi.DetailFocusButtonText.text = canFocus
                ? (ru ? $"\u0421\u043b\u0435\u0434\u0438\u0442\u044c \u0437\u0430 {sel.DriverName}" : $"Focus on {sel.DriverName}")
                : (ru ? $"{sel.DriverName} \u043d\u0435 \u0432\u0438\u0434\u0435\u043d \u043d\u0430 \u043a\u0430\u0440\u0442\u0435" : $"{sel.DriverName} is not on the map");
        }

        isDriversScreenDirty = false;
    }

    private void ScrollWorkersListToSelectedIfRequested()
    {
        if (!shouldScrollWorkersListToSelected ||
            driversScreenUi?.WorkerListScrollRect == null ||
            selectedWorkerPanelDriverId <= 0)
        {
            return;
        }

        int selectedIndex = driverAgents.FindIndex(d => d.DriverId == selectedWorkerPanelDriverId);
        if (selectedIndex < 0)
        {
            shouldScrollWorkersListToSelected = false;
            return;
        }

        Canvas.ForceUpdateCanvases();
        int maxIndex = Mathf.Max(1, driverAgents.Count - 1);
        float normalized = 1f - Mathf.Clamp01(selectedIndex / (float)maxIndex);
        driversScreenUi.WorkerListScrollRect.verticalNormalizedPosition = normalized;
        shouldScrollWorkersListToSelected = false;
    }

    private void ApplyWorkerDetailTabUi(bool ru)
    {
        if (driversScreenUi == null)
        {
            return;
        }

        if (driversScreenUi.DetailProfileTabText != null)
        {
            driversScreenUi.DetailProfileTabText.text = ru ? "\u041f\u0440\u043e\u0444\u0438\u043b\u044c" : "Profile";
        }

        if (driversScreenUi.DetailSocialTabText != null)
        {
            driversScreenUi.DetailSocialTabText.text = ru ? "\u0421\u043e\u0446\u0438\u0430\u043b\u044c\u043d\u044b\u0435 \u0441\u0432\u044f\u0437\u0438" : "Social Links";
        }

        if (driversScreenUi.DetailThoughtsTabText != null)
        {
            driversScreenUi.DetailThoughtsTabText.text = ru ? "\u041c\u044b\u0441\u043b\u0438" : "Thoughts";
        }

        if (driversScreenUi.DetailInventoryTabText != null)
        {
            driversScreenUi.DetailInventoryTabText.text = ru ? "\u0418\u043d\u0432\u0435\u043d\u0442\u0430\u0440\u044c" : "Inventory";
        }

        if (driversScreenUi.DetailProfileTabRoot != null)
        {
            driversScreenUi.DetailProfileTabRoot.SetActive(activeWorkerDetailTab == WorkerDetailTab.Profile);
        }

        if (driversScreenUi.DetailSocialTabRoot != null)
        {
            driversScreenUi.DetailSocialTabRoot.SetActive(activeWorkerDetailTab == WorkerDetailTab.Social);
        }

        if (driversScreenUi.DetailThoughtsTabRoot != null)
        {
            driversScreenUi.DetailThoughtsTabRoot.SetActive(activeWorkerDetailTab == WorkerDetailTab.Thoughts);
        }

        if (driversScreenUi.DetailInventoryTabRoot != null)
        {
            driversScreenUi.DetailInventoryTabRoot.SetActive(activeWorkerDetailTab == WorkerDetailTab.Inventory);
        }

        ApplyShiftsTabVisual(driversScreenUi.DetailProfileTabButton, driversScreenUi.DetailProfileTabText, activeWorkerDetailTab == WorkerDetailTab.Profile);
        ApplyShiftsTabVisual(driversScreenUi.DetailSocialTabButton, driversScreenUi.DetailSocialTabText, activeWorkerDetailTab == WorkerDetailTab.Social);
        ApplyShiftsTabVisual(driversScreenUi.DetailThoughtsTabButton, driversScreenUi.DetailThoughtsTabText, activeWorkerDetailTab == WorkerDetailTab.Thoughts);
        ApplyShiftsTabVisual(driversScreenUi.DetailInventoryTabButton, driversScreenUi.DetailInventoryTabText, activeWorkerDetailTab == WorkerDetailTab.Inventory);
    }

}
