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
            if (!shouldShow)
            {
                PrepareWorkerSocialGraphAnimatedRebuild();
            }
        }

        if (!shouldShow) return;
        if (!isDriversScreenDirty)
        {
            UpdateWorkerSocialGraphAnimations();
            return;
        }

        bool screenRu = IsRussianLanguage();
        driversScreenUi.HeaderCountText.text = screenRu
            ? $"{driverAgents.Count} {FormatResidentCountWordRu(driverAgents.Count)}"
            : $"{driverAgents.Count} {(driverAgents.Count == 1 ? "resident" : "residents")}";
        EnsureWorkerRows(driverAgents.Count);

        // Left panel compact row list.
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
            if (row.Outline != null)
            {
                row.Outline.effectColor = isSelected ? ResidentHudAmberBorderColor : ResidentHudBorderColor;
            }
            if (row.PortraitRoot != null)
            {
                DrawWorkerPortraitScaled(d, row.PortraitRoot, 0.50f);
            }

            row.NameText.text = d.DriverName;
            row.NameText.color = isSelected ? new Color(1f, 0.94f, 0.78f, 1f) : Color.white;

            bool ru = IsRussianLanguage();
            row.SubText.text = d.IsArrivingByBus              ? (ru ? "\u0412 \u043f\u0443\u0442\u0438..." : "On the way...")
                : isLogistics && d.AssignedBuildingType.HasValue ? GetSelectedLocationDisplayName(d.AssignedBuildingType.Value)
                : L(GetWorkerOccupationLabel(d));
            row.SubText.color = isSelected ? new Color(0.90f, 0.82f, 0.62f, 1f) : FleetSecondaryTextColor;
            if (row.BalanceText != null)
            {
                row.BalanceText.text = $"${d.Money}";
                row.BalanceText.color = d.Money < 15 ? new Color(0.96f, 0.72f, 0.42f, 1f) : FleetAccentColor;
            }

        }

        // Right panel visibility toggle before localization so layout is correct.
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

        // Localize static texts first before setting dynamic detail strings.
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
                driversScreenUi.DetailProfileTitleText.text = ru ? "\u0414\u043e\u0441\u044c\u0435" : "Resident file";
            UpdateWorkerDossierUi(sel, ru);
            UpdateWorkerPortraitUi(sel);
            UpdateWorkerNeedsUi(sel, ru);

            string statusLabel = GetWorkerListStatusLabel(sel, ru);
            driversScreenUi.DetailStatusText.text = statusLabel;
            bool isAvailableStatus = !sel.IsArrivingByBus &&
                                     !isLogistics &&
                                     truck == null &&
                                     sel.RestPhase == DriverRestPhase.None &&
                                     sel.WalkPhase == DriverRescuePhase.None &&
                                     !IsDriverOnActiveTradeRun(sel) &&
                                     !IsDriverIntercity(sel) &&
                                     !IsDriverBusDriver(sel);
            driversScreenUi.DetailStatusText.color = isAvailableStatus ? ResidentHudPositiveColor : Color.white;
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
                driversScreenUi.DetailAssignmentLabel.text = ru ? "\u0417\u0430\u043d\u044f\u0442\u043e\u0441\u0442\u044c" : "Employment";
            if (driversScreenUi.DetailShiftLabel != null)
                driversScreenUi.DetailShiftLabel.text = IsWorkerEmployedInHud(sel, truck) ? (ru ? "\u0421\u043c\u0435\u043d\u0430" : "Shift") : (ru ? "\u0421\u043b\u0435\u0434\u0443\u044e\u0449\u0438\u0439 \u0448\u0430\u0433" : "Next step");
            if (driversScreenUi.DetailDutyLabel != null)
                driversScreenUi.DetailDutyLabel.text = ru ? "\u041a\u043e\u043d\u0442\u0440\u0430\u043a\u0442" : "Contract";
            if (driversScreenUi.DetailWorkTitleText != null)
                driversScreenUi.DetailWorkTitleText.text = ru ? "\u0420\u0430\u0431\u043e\u0442\u0430" : "Work";

            // Values
            bool hasHudEmployment = IsWorkerEmployedInHud(sel, truck);
            if (driversScreenUi.DetailAssignmentRow != null)
                driversScreenUi.DetailAssignmentRow.gameObject.SetActive(true);
            if (driversScreenUi.DetailShiftRow != null)
                driversScreenUi.DetailShiftRow.gameObject.SetActive(true);
            driversScreenUi.DetailAssignmentValue.text = FormatWorkerEmploymentSummary(sel, truck, ru);
            driversScreenUi.DetailAssignmentValue.color = hasHudEmployment ? Color.white : FleetMutedTextColor;

            bool hasShift = sel.ShiftStartHour >= 0;
            driversScreenUi.DetailShiftText.text = FormatWorkerShiftOrNextStep(sel, hasHudEmployment, ru);
            driversScreenUi.DetailShiftText.color = hasHudEmployment && (hasShift || isLogistics) ? FleetAccentColor : FleetSecondaryTextColor;

            bool hasContract = sel.ContractTotalWorkDays > 0;
            if (driversScreenUi.DetailWorkCardLayout != null)
                driversScreenUi.DetailWorkCardLayout.preferredHeight = hasContract ? 148f : 112f;
            if (driversScreenUi.DetailDutyRow != null)
                driversScreenUi.DetailDutyRow.gameObject.SetActive(hasContract);
            if (hasContract)
            {
                driversScreenUi.DetailDutyStateText.text = FormatWorkerSalaryContract(sel, ru);
                driversScreenUi.DetailDutyStateText.color = FleetSecondaryTextColor;
            }

            UpdateWorkerSocialUi(sel, ru);
            UpdateWorkerThoughtsUi(sel, ru);
            UpdateWorkerInventoryUi(sel, ru);

            bool canFocus = CanFocusDriver(sel);
            driversScreenUi.DetailFocusButton.interactable = canFocus;
            driversScreenUi.DetailFocusButtonText.text = canFocus
                ? (ru ? "\u0421\u043b\u0435\u0434\u0438\u0442\u044c" : "Focus")
                : (ru ? "\u041d\u0435 \u0432\u0438\u0434\u043d\u043e" : "Hidden");
        }

        isDriversScreenDirty = false;
        UpdateWorkerSocialGraphAnimations();
    }

    private void UpdateWorkerDossierUi(DriverAgent worker, bool ru)
    {
        if (worker == null)
        {
            return;
        }

        SetResidentInfoTile(driversScreenUi.DetailGenderTile, ru ? "\u041f\u043e\u043b" : "Gender", GetWorkerGenderLabel(worker, ru), Color.white);
        SetResidentInfoTile(driversScreenUi.DetailAgeTile, ru ? "\u0412\u043e\u0437\u0440\u0430\u0441\u0442" : "Age", FormatWorkerAgeValue(worker, ru), Color.white);
        SetResidentInfoTile(driversScreenUi.DetailEducationTile, ru ? "\u041e\u0431\u0440\u0430\u0437\u043e\u0432\u0430\u043d\u0438\u0435" : "Education", GetWorkerEducationDisplayName(worker.Education, ru), Color.white);
        SetResidentInfoTile(driversScreenUi.DetailMoneyTile, ru ? "\u0414\u043e\u0445\u043e\u0434" : "Money", $"${worker.Money}", FleetAccentColor);
        SetResidentInfoTile(driversScreenUi.DetailHomeTile, ru ? "\u0416\u0438\u043b\u044c\u0435" : "Home", FormatWorkerHomeValue(worker, ru), Color.white);
        SetResidentInfoTile(driversScreenUi.DetailCarTile, ru ? "\u0410\u0432\u0442\u043e" : "Car", FormatWorkerCarValue(worker, ru), Color.white);

        if (driversScreenUi.DetailSkillsTitleText != null)
        {
            driversScreenUi.DetailSkillsTitleText.text = ru ? "\u041d\u0430\u0432\u044b\u043a\u0438" : "Skills";
        }

        SetWorkerSkillTile(driversScreenUi.DetailLogisticsSkillTile, ru ? "\u041b\u043e\u0433\u0438\u0441\u0442\u0438\u043a\u0430" : "Logistics", GetWorkerProfessionalLevel(worker, WorkerProfessionalTrack.Logistics));
        SetWorkerSkillTile(driversScreenUi.DetailProductionSkillTile, ru ? "\u041f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0441\u0442\u0432\u043e" : "Production", GetWorkerProfessionalLevel(worker, WorkerProfessionalTrack.Production));
        SetWorkerSkillTile(driversScreenUi.DetailServiceSkillTile, ru ? "\u0421\u0435\u0440\u0432\u0438\u0441" : "Service", GetWorkerProfessionalLevel(worker, WorkerProfessionalTrack.Service));
    }

    private static string FormatResidentCountWordRu(int count)
    {
        int mod10 = Mathf.Abs(count) % 10;
        int mod100 = Mathf.Abs(count) % 100;
        if (mod10 == 1 && mod100 != 11) return "\u0436\u0438\u0442\u0435\u043b\u044c";
        if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return "\u0436\u0438\u0442\u0435\u043b\u044f";
        return "\u0436\u0438\u0442\u0435\u043b\u0435\u0439";
    }

    private void SetResidentInfoTile(ResidentInfoTileUi tile, string label, string value, Color valueColor)
    {
        if (tile == null)
        {
            return;
        }

        if (tile.LabelText != null)
        {
            tile.LabelText.text = label;
        }

        if (tile.ValueText != null)
        {
            tile.ValueText.text = string.IsNullOrEmpty(value) ? "\u2014" : value;
            tile.ValueText.color = valueColor;
        }
    }

    private void SetWorkerSkillTile(WorkerSkillTileUi tile, string label, int value)
    {
        if (tile == null)
        {
            return;
        }

        if (tile.LabelText != null)
        {
            tile.LabelText.text = label;
        }

        if (tile.ValueText != null)
        {
            tile.ValueText.text = Mathf.Max(0, value).ToString();
        }
    }

    private static string FormatWorkerAgeValue(DriverAgent worker, bool ru)
    {
        return worker != null && worker.Age > 0
            ? (ru ? $"{worker.Age} \u043b\u0435\u0442" : $"{worker.Age} y.o.")
            : "\u2014";
    }

    private string FormatWorkerHomeValue(DriverAgent worker, bool ru)
    {
        return worker != null && worker.AssignedPersonalHouseIndex >= 0 && worker.AssignedPersonalHouseIndex < personalHouses.Count
            ? FormatWorkerHomeLabel(worker, ru)
            : (ru ? "\u043d\u0435\u0442" : "none");
    }

    private static string FormatWorkerCarValue(DriverAgent worker, bool ru)
    {
        return worker != null && worker.OwnedCarModelIndex >= 0 && worker.OwnedCarModelIndex < CarModelNames.Length
            ? CarModelNames[worker.OwnedCarModelIndex]
            : (ru ? "\u043d\u0435\u0442" : "none");
    }

    private bool IsWorkerEmployedInHud(DriverAgent worker, TruckAgent truck)
    {
        return worker != null &&
               (worker.DutyMode == DriverDutyMode.Logistics && worker.AssignedBuildingType.HasValue ||
                truck != null ||
                IsDriverBusDriver(worker) ||
                IsDriverIntercity(worker) ||
                IsDriverOnActiveTradeRun(worker));
    }

    private string FormatWorkerEmploymentSummary(DriverAgent worker, TruckAgent truck, bool ru)
    {
        if (worker == null)
        {
            return "\u2014";
        }

        if (worker.IsArrivingByBus)
        {
            return ru ? "\u0415\u0434\u0435\u0442 \u0432 \u0433\u043e\u0440\u043e\u0434" : "Arriving to town";
        }

        if (worker.DutyMode == DriverDutyMode.Logistics && worker.AssignedBuildingType.HasValue)
        {
            string role = L(GetWorkerOccupationLabel(worker));
            string building = GetSelectedLocationDisplayName(worker.AssignedBuildingType.Value);
            return $"{role} - {building}";
        }

        if (IsDriverBusDriver(worker))
        {
            return L("Bus Driver");
        }

        if (IsDriverIntercity(worker))
        {
            return L("Intercity Driver");
        }

        if (truck != null || worker.AssignedTruckNumber > 0 || IsDriverOnActiveTradeRun(worker))
        {
            return L("Truck Driver");
        }

        return worker.LifeGoal == WorkerLifeGoal.FindJob
            ? L("Job Seeker")
            : (ru ? "\u0411\u0435\u0437 \u0440\u0430\u0431\u043e\u0442\u044b" : "Unemployed");
    }

    private string FormatWorkerShiftOrNextStep(DriverAgent worker, bool hasEmployment, bool ru)
    {
        if (worker == null)
        {
            return "\u2014";
        }

        if (!hasEmployment)
        {
            if (worker.IsArrivingByBus)
            {
                return ru ? "\u0414\u043e\u0439\u0434\u0451\u0442 \u0434\u043e \u0433\u043e\u0440\u043e\u0434\u0430 \u0438 \u0437\u0430\u0441\u0435\u043b\u0438\u0442\u0441\u044f" : "Reach town and check in";
            }

            return worker.LifeGoal == WorkerLifeGoal.FindJob ||
                   worker.WalkPhase == DriverRescuePhase.ToLaborExchangeForJob ||
                   worker.WalkPhase == DriverRescuePhase.AtLaborExchange
                ? (ru ? "\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430: \u0438\u0449\u0435\u0442 \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u044e" : "Labor Exchange: looking for a vacancy")
                : (ru ? "\u041e\u0436\u0438\u0434\u0430\u0435\u0442 \u043f\u043e\u0434\u0445\u043e\u0434\u044f\u0449\u0443\u044e \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u044e" : "Waiting for a suitable vacancy");
        }

        if (worker.DutyMode == DriverDutyMode.Logistics)
        {
            return GetProductionWorkRangeLabel();
        }

        return worker.ShiftStartHour >= 0
            ? GetShiftRangeLabel(worker.ShiftStartHour)
            : (ru ? "\u041d\u0435 \u043d\u0430\u0437\u043d\u0430\u0447\u0435\u043d\u0430" : "Not assigned");
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

        ApplyResidentDetailTabVisual(driversScreenUi.DetailProfileTabButton, driversScreenUi.DetailProfileTabText, activeWorkerDetailTab == WorkerDetailTab.Profile);
        ApplyResidentDetailTabVisual(driversScreenUi.DetailSocialTabButton, driversScreenUi.DetailSocialTabText, activeWorkerDetailTab == WorkerDetailTab.Social);
        ApplyResidentDetailTabVisual(driversScreenUi.DetailThoughtsTabButton, driversScreenUi.DetailThoughtsTabText, activeWorkerDetailTab == WorkerDetailTab.Thoughts);
        ApplyResidentDetailTabVisual(driversScreenUi.DetailInventoryTabButton, driversScreenUi.DetailInventoryTabText, activeWorkerDetailTab == WorkerDetailTab.Inventory);
    }

    private void ApplyResidentDetailTabVisual(Button button, Text label, bool active)
    {
        Image image = button != null ? button.GetComponent<Image>() : null;
        if (image != null)
        {
            image.color = active ? FleetPrimaryButtonColor : new Color(0.07f, 0.11f, 0.17f, 1f);
            image.raycastTarget = true;
        }

        if (label != null)
        {
            label.color = active ? Color.white : FleetSecondaryTextColor;
            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;
        }

        if (button != null)
        {
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = active ? new Color(1f, 0.94f, 0.82f, 1f) : new Color(0.86f, 0.94f, 1f, 1f);
            colors.pressedColor = new Color(0.78f, 0.84f, 0.90f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (image != null)
            {
                button.targetGraphic = image;
            }
        }
    }

}
