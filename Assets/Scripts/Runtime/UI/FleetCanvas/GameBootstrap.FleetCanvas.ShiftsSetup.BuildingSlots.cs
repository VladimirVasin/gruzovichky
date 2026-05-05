using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private int AddBuildingWorkerSlotCard(RectTransform parent, Font font, int slotArrayIndex, LocationType buildingType, int workerSlot)
    {
        LogisticsSlotUi slot = new() { BuildingType = buildingType, SlotIndex = workerSlot };
        RectTransform slotCard = CreateSectionCard(parent, font, string.Empty, out RectTransform slotBody, false);
        slot.Root = slotCard;
        LayoutElement slotCardLE = slotCard.gameObject.AddComponent<LayoutElement>();
        slotCardLE.preferredHeight = 102f;
        slotCardLE.flexibleHeight = 0f;
        VerticalLayoutGroup slotBodyLayout = slotBody.GetComponent<VerticalLayoutGroup>();
        slotBodyLayout.spacing = 4;

        slot.BuildingNameText = CreateHeaderText($"LogBldgName{slotArrayIndex}", slotBody, font, GetBuildingWorkerSlotTitle(buildingType, workerSlot), 16, TextAnchor.MiddleLeft, Color.white);
        slot.AssignedWorkerText = CreateHeaderText($"LogWorker{slotArrayIndex}", slotBody, font, "No worker assigned", 14, TextAnchor.MiddleLeft, FleetAccentColor);
        slot.AssignedWorkerText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform workRow = CreateLayoutRow($"LogWorkRow{slotArrayIndex}", slotBody, 22f, 8f);
        workRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        CreateBodyText($"LogWorkLabel{slotArrayIndex}", workRow, font, "Hours:", 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        slot.WorkHoursText = CreateHeaderText($"LogWorkHours{slotArrayIndex}", workRow, font, GetBuildingWorkerWorkRangeLabel(buildingType, workerSlot), 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        slot.WorkHoursText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        RectTransform actionRow = CreateLayoutRow($"LogActionRow{slotArrayIndex}", slotBody, 26f, 8f);
        actionRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        slot.AssignButton = CreateButton($"LogAssignBtn{slotArrayIndex}", actionRow, font, out slot.AssignButtonText, "Assign Worker", 12, FleetPrimaryButtonColor, Color.white);
        slot.AssignButton.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        slot.RemoveButton = CreateButton($"LogRemoveBtn{slotArrayIndex}", actionRow, font, out _, "Remove", 12, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
        slot.RemoveButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 80f;

        int capturedIndex = slotArrayIndex;
        slot.AssignButton.onClick.AddListener(() =>
        {
            DriverAgent selectedDriver = driverAgents.Find(d => d.DriverId == selectedShiftDriverId);
            if (selectedDriver == null) return;
            AssignWorkerToBuilding(selectedDriver, logisticsSlots[capturedIndex]);
            PlayUiSound(uiSelectClip, 0.85f);
        });
        slot.RemoveButton.onClick.AddListener(() =>
        {
            RemoveWorkerFromBuilding(logisticsSlots[capturedIndex]);
            PlayUiSound(uiSelectClip, 0.85f);
        });
        logisticsSlots[slotArrayIndex] = slot;
        return slotArrayIndex + 1;
    }

    private int AddWarehouseWorkerSlotCard(RectTransform parent, Font font, int slotArrayIndex)
    {
        RectTransform warehouseCard = CreateSectionCard(parent, font, string.Empty, out RectTransform warehouseBody, false);
        LayoutElement warehouseCardLE = warehouseCard.gameObject.AddComponent<LayoutElement>();
        warehouseCardLE.preferredHeight = 126f + WarehouseMaxWorkers * 28f;
        warehouseCardLE.flexibleHeight = 0f;
        VerticalLayoutGroup warehouseBodyLayout = warehouseBody.GetComponent<VerticalLayoutGroup>();
        warehouseBodyLayout.spacing = 4;

        Text warehouseTitle = CreateHeaderText("LogBldgNameWarehouse", warehouseBody, font, "Warehouse", 16, TextAnchor.MiddleLeft, Color.white);
        RectTransform workRow = CreateLayoutRow("LogWorkRowWarehouse", warehouseBody, 22f, 8f);
        workRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        CreateBodyText("LogWorkLabelWarehouse", workRow, font, "Hours:", 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        Text warehouseWorkHours = CreateHeaderText("LogWorkHoursWarehouse", workRow, font, GetProductionWorkRangeLabel(), 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        warehouseWorkHours.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        for (int wi = 0; wi < WarehouseMaxWorkers; wi++)
        {
            LogisticsSlotUi wSlot = new() { BuildingType = LocationType.Warehouse, SlotIndex = wi, Root = warehouseCard };
            if (wi == 0)
            {
                wSlot.WorkHoursText = warehouseWorkHours;
                wSlot.BuildingNameText = warehouseTitle;
            }

            RectTransform wActionRow = CreateLayoutRow($"LogActionRowWarehouse{wi}", warehouseBody, 26f, 4f);
            wActionRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
            wSlot.AssignedWorkerText = CreateHeaderText($"LogWorkerWarehouse{wi}", wActionRow, font, $"Loader {wi + 1}: -", 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            wSlot.AssignedWorkerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            wSlot.AssignButton = CreateButton($"LogAssignBtnWarehouse{wi}", wActionRow, font, out wSlot.AssignButtonText, "Assign", 11, FleetPrimaryButtonColor, Color.white);
            wSlot.AssignButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 104f;
            wSlot.RemoveButton = CreateButton($"LogRemoveBtnWarehouse{wi}", wActionRow, font, out _, "X", 12, new Color(0.37f, 0.25f, 0.19f, 1f), Color.white);
            wSlot.RemoveButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 28f;

            int capturedIdx = slotArrayIndex;
            wSlot.AssignButton.onClick.AddListener(() =>
            {
                DriverAgent sel = driverAgents.Find(d => d.DriverId == selectedShiftDriverId);
                if (sel == null) return;
                AssignWorkerToBuilding(sel, logisticsSlots[capturedIdx]);
                PlayUiSound(uiSelectClip, 0.85f);
            });
            wSlot.RemoveButton.onClick.AddListener(() =>
            {
                RemoveWorkerFromBuilding(logisticsSlots[capturedIdx]);
                PlayUiSound(uiSelectClip, 0.85f);
            });
            logisticsSlots[slotArrayIndex] = wSlot;
            slotArrayIndex++;
        }

        return slotArrayIndex;
    }

    private string GetBuildingWorkerSlotTitle(LocationType buildingType, int workerSlot, int locationInstanceId = 0)
    {
        string buildingName = GetBuildingInstanceDisplayName(buildingType, locationInstanceId);
        int shiftIndex = GetBuildingWorkerShiftPresetIndex(buildingType, workerSlot);
        if (shiftIndex >= 0 && shiftIndex < ShiftNames.Length)
        {
            return $"{buildingName}: {L(ShiftNames[shiftIndex])}";
        }

        return GetMaxBuildingWorkerSlots(buildingType) > 1
            ? $"{buildingName} #{workerSlot + 1}"
            : buildingName;
    }
}
