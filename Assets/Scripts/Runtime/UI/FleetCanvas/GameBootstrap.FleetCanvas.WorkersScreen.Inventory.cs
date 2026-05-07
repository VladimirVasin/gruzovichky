using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int WorkerInventoryHudRowCount = 8;

    private void SetupWorkerInventoryUi(RectTransform inventoryTabRoot, Font font)
    {
        RectTransform inventoryCard = CreateSectionCard(inventoryTabRoot, font, string.Empty, out RectTransform inventoryBody, false);
        LayoutElement inventoryCardLayout = inventoryCard.gameObject.AddComponent<LayoutElement>();
        inventoryCardLayout.preferredHeight = 336f;
        inventoryCardLayout.flexibleHeight = 1f;
        inventoryCard.GetComponent<VerticalLayoutGroup>().padding = new RectOffset(16, 16, 10, 10);
        inventoryCard.GetComponent<VerticalLayoutGroup>().spacing = 6;
        inventoryBody.GetComponent<VerticalLayoutGroup>().spacing = 6;

        driversScreenUi.DetailInventoryTitleText = CreateHeaderText("WorkerInventoryTitle", inventoryBody, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailInventoryTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform header = CreateLayoutRow("WorkerInventoryHeader", inventoryBody, 16f, 8f);
        driversScreenUi.DetailInventoryNameHeaderText = CreateBodyText("InventoryItemHeader", header, font, "Item", 10, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailInventoryNameHeaderText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        driversScreenUi.DetailInventoryCategoryHeaderText = CreateBodyText("InventoryCategoryHeader", header, font, "Type", 10, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailInventoryCategoryHeaderText.gameObject.AddComponent<LayoutElement>().preferredWidth = 96f;
        driversScreenUi.DetailInventoryQuantityHeaderText = CreateBodyText("InventoryQuantityHeader", header, font, "Qty", 10, TextAnchor.MiddleRight, FleetMutedTextColor);
        driversScreenUi.DetailInventoryQuantityHeaderText.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;
        driversScreenUi.DetailInventoryMetaHeaderText = CreateBodyText("InventoryMetaHeader", header, font, "State", 10, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailInventoryMetaHeaderText.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;

        driversScreenUi.DetailInventoryEmptyText = CreateBodyText("WorkerInventoryEmpty", inventoryBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailInventoryEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        for (int i = 0; i < WorkerInventoryHudRowCount; i++)
        {
            driversScreenUi.DetailInventoryRows.Add(CreateWorkerInventoryRow(inventoryBody, font, i));
        }
    }

    private WorkerInventoryRowUi CreateWorkerInventoryRow(RectTransform parent, Font font, int index)
    {
        WorkerInventoryRowUi row = new();
        row.Root = CreateLayoutRow($"WorkerInventoryRow{index + 1}", parent, 26f, 8f);
        row.NameText = CreateHeaderText("Item", row.Root, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white);
        row.NameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        row.CategoryText = CreateBodyText("Type", row.Root, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        row.CategoryText.gameObject.AddComponent<LayoutElement>().preferredWidth = 96f;
        row.QuantityText = CreateHeaderText("Qty", row.Root, font, string.Empty, 12, TextAnchor.MiddleRight, FleetAccentColor);
        row.QuantityText.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;
        row.MetaText = CreateBodyText("State", row.Root, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetMutedTextColor);
        row.MetaText.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;
        row.Root.gameObject.SetActive(false);
        return row;
    }

    private void UpdateWorkerInventoryUi(DriverAgent worker, bool ru)
    {
        if (driversScreenUi == null)
        {
            return;
        }

        List<WorkerInventoryEntry> entries = GetWorkerInventoryEntriesForHud(worker);
        UpdateWorkerInventoryHeaderUi(ru);
        if (driversScreenUi.DetailInventoryTitleText != null)
        {
            driversScreenUi.DetailInventoryTitleText.text = ru
                ? $"\u0418\u043d\u0432\u0435\u043d\u0442\u0430\u0440\u044c ({entries.Count}/{WorkerInventoryMaxStacks})"
                : $"Inventory ({entries.Count}/{WorkerInventoryMaxStacks})";
        }

        bool hasItems = entries.Count > 0;
        if (driversScreenUi.DetailInventoryEmptyText != null)
        {
            driversScreenUi.DetailInventoryEmptyText.gameObject.SetActive(!hasItems);
            driversScreenUi.DetailInventoryEmptyText.text = ru
                ? "\u041f\u0440\u0435\u0434\u043c\u0435\u0442\u043e\u0432 \u043f\u043e\u043a\u0430 \u043d\u0435\u0442"
                : "No owned items yet";
        }

        for (int i = 0; i < driversScreenUi.DetailInventoryRows.Count; i++)
        {
            WorkerInventoryRowUi row = driversScreenUi.DetailInventoryRows[i];
            bool active = i < entries.Count;
            row.Root.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            WorkerInventoryEntry entry = entries[i];
            WorkerItemData itemData = GetWorkerInventoryItemData(entry.ItemId);
            row.NameText.text = GetWorkerInventoryItemTitle(entry.ItemId, ru);
            row.CategoryText.text = GetWorkerInventoryCategoryLabel(itemData?.category, ru);
            row.QuantityText.text = entry.Quantity > 1 ? $"x{entry.Quantity}" : string.Empty;
            row.MetaText.text = FormatWorkerInventoryEntryMeta(entry, ru);
        }
    }

    private void UpdateWorkerInventoryHeaderUi(bool ru)
    {
        if (driversScreenUi.DetailInventoryNameHeaderText != null)
        {
            driversScreenUi.DetailInventoryNameHeaderText.text = ru ? "\u041f\u0440\u0435\u0434\u043c\u0435\u0442" : "Item";
        }

        if (driversScreenUi.DetailInventoryCategoryHeaderText != null)
        {
            driversScreenUi.DetailInventoryCategoryHeaderText.text = ru ? "\u0422\u0438\u043f" : "Type";
        }

        if (driversScreenUi.DetailInventoryQuantityHeaderText != null)
        {
            driversScreenUi.DetailInventoryQuantityHeaderText.text = ru ? "\u041a-\u0432\u043e" : "Qty";
        }

        if (driversScreenUi.DetailInventoryMetaHeaderText != null)
        {
            driversScreenUi.DetailInventoryMetaHeaderText.text = ru ? "\u0421\u043e\u0441\u0442." : "State";
        }
    }

    private static string FormatWorkerInventoryEntryMeta(WorkerInventoryEntry entry, bool ru)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        int condition = Mathf.RoundToInt(Mathf.Clamp01(entry.Condition01) * 100f);
        string day = entry.AcquiredDay > 0
            ? (ru ? $"\u0414\u0435\u043d\u044c {entry.AcquiredDay}" : $"Day {entry.AcquiredDay}")
            : (ru ? "\u0414\u0435\u043d\u044c ?" : "Day ?");
        return $"{day} | {condition}%";
    }
}
