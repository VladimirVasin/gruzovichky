using System;
using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerInventoryMaxStacks = 24;

    private bool TryAddWorkerInventoryItem(
        DriverAgent worker,
        string itemId,
        int quantity = 1,
        string sourceKey = "",
        float condition01 = 1f,
        int instanceId = 0)
    {
        if (worker == null || string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return false;
        }

        WorkerItemData itemData = GetWorkerInventoryItemData(itemId);
        if (itemData == null)
        {
            SessionDebugLogger.Log("WORKER_INVENTORY", $"{worker.DriverName} could not receive unknown item '{itemId}'.");
            return false;
        }

        NormalizeWorkerInventory(worker);
        int maxStack = Mathf.Max(1, itemData.maxStack);
        bool stackable = itemData.stackable && instanceId <= 0;
        if (stackable)
        {
            for (int i = 0; i < worker.Inventory.Count && quantity > 0; i++)
            {
                WorkerInventoryEntry entry = worker.Inventory[i];
                if (entry.ItemId != itemData.id || entry.InstanceId > 0 || entry.Quantity >= maxStack)
                {
                    continue;
                }

                int added = Mathf.Min(quantity, maxStack - entry.Quantity);
                entry.Quantity += added;
                entry.Condition01 = Mathf.Clamp01(Mathf.Max(entry.Condition01, condition01));
                quantity -= added;
            }
        }

        while (quantity > 0)
        {
            if (worker.Inventory.Count >= WorkerInventoryMaxStacks)
            {
                SessionDebugLogger.Log("WORKER_INVENTORY", $"{worker.DriverName} inventory is full; item '{itemData.id}' could not fit.");
                isDriversScreenDirty = true;
                return false;
            }

            int stackQuantity = stackable ? Mathf.Min(quantity, maxStack) : 1;
            worker.Inventory.Add(new WorkerInventoryEntry
            {
                ItemId = itemData.id,
                Quantity = stackQuantity,
                Condition01 = Mathf.Clamp01(condition01),
                AcquiredDay = currentDay,
                SourceKey = sourceKey ?? string.Empty,
                InstanceId = stackable ? 0 : instanceId
            });
            quantity -= stackQuantity;
        }

        isDriversScreenDirty = true;
        SessionDebugLogger.Log("WORKER_INVENTORY", $"{worker.DriverName} received {itemData.id}; now has {GetWorkerInventoryItemQuantity(worker, itemData.id)}.");
        return true;
    }

    private bool TryRemoveWorkerInventoryItem(DriverAgent worker, string itemId, int quantity = 1, string reason = "")
    {
        if (worker == null || string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return false;
        }

        NormalizeWorkerInventory(worker);
        int available = GetWorkerInventoryItemQuantity(worker, itemId);
        if (available < quantity)
        {
            return false;
        }

        int remaining = quantity;
        for (int i = worker.Inventory.Count - 1; i >= 0 && remaining > 0; i--)
        {
            WorkerInventoryEntry entry = worker.Inventory[i];
            if (!string.Equals(entry.ItemId, itemId, StringComparison.Ordinal))
            {
                continue;
            }

            int removed = Mathf.Min(remaining, entry.Quantity);
            entry.Quantity -= removed;
            remaining -= removed;
            if (entry.Quantity <= 0)
            {
                worker.Inventory.RemoveAt(i);
            }
        }

        isDriversScreenDirty = true;
        SessionDebugLogger.Log("WORKER_INVENTORY", $"{worker.DriverName} spent {itemId} x{quantity}; reason={reason}.");
        return true;
    }

    private bool HasWorkerInventoryItem(DriverAgent worker, string itemId, int quantity = 1)
    {
        return GetWorkerInventoryItemQuantity(worker, itemId) >= Mathf.Max(1, quantity);
    }

    private int GetWorkerInventoryItemQuantity(DriverAgent worker, string itemId)
    {
        if (worker == null || string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < worker.Inventory.Count; i++)
        {
            WorkerInventoryEntry entry = worker.Inventory[i];
            if (entry != null && string.Equals(entry.ItemId, itemId, StringComparison.Ordinal))
            {
                total += Mathf.Max(0, entry.Quantity);
            }
        }

        return total;
    }

    private List<WorkerInventoryEntry> GetWorkerInventoryEntriesForHud(DriverAgent worker)
    {
        List<WorkerInventoryEntry> result = new();
        if (worker == null)
        {
            return result;
        }

        NormalizeWorkerInventory(worker);
        result.AddRange(worker.Inventory);
        result.Sort((a, b) =>
        {
            WorkerItemData itemA = GetWorkerInventoryItemData(a.ItemId);
            WorkerItemData itemB = GetWorkerInventoryItemData(b.ItemId);
            string categoryA = itemA?.category ?? string.Empty;
            string categoryB = itemB?.category ?? string.Empty;
            int category = string.Compare(categoryA, categoryB, StringComparison.Ordinal);
            if (category != 0) return category;
            return string.Compare(GetWorkerInventoryItemTitle(a.ItemId, false), GetWorkerInventoryItemTitle(b.ItemId, false), StringComparison.Ordinal);
        });
        return result;
    }

    private WorkerItemData GetWorkerInventoryItemData(string itemId)
    {
        return WorkerItemCatalog.TryGet(itemId, out WorkerItemData itemData) ? itemData : null;
    }

    private string GetWorkerInventoryItemTitle(string itemId, bool ru)
    {
        WorkerItemData itemData = GetWorkerInventoryItemData(itemId);
        return itemData != null ? itemData.GetTitle(ru) : itemId;
    }

    private void NormalizeWorkerInventory(DriverAgent worker)
    {
        if (worker == null)
        {
            return;
        }

        for (int i = worker.Inventory.Count - 1; i >= 0; i--)
        {
            WorkerInventoryEntry entry = worker.Inventory[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.ItemId) || entry.Quantity <= 0)
            {
                worker.Inventory.RemoveAt(i);
                continue;
            }

            WorkerItemData itemData = GetWorkerInventoryItemData(entry.ItemId);
            int maxStack = Mathf.Max(1, itemData?.maxStack ?? entry.Quantity);
            entry.Quantity = Mathf.Clamp(entry.Quantity, 1, maxStack);
            entry.Condition01 = Mathf.Clamp01(entry.Condition01);
            entry.SourceKey ??= string.Empty;
        }
    }

    private static string GetWorkerInventoryCategoryLabel(string category, bool ru)
    {
        return category switch
        {
            "Tool" => ru ? "\u0418\u043d\u0441\u0442\u0440\u0443\u043c\u0435\u043d\u0442" : "Tool",
            "Consumable" => ru ? "\u0420\u0430\u0441\u0445\u043e\u0434\u043d\u0438\u043a" : "Consumable",
            "Document" => ru ? "\u0414\u043e\u043a\u0443\u043c\u0435\u043d\u0442" : "Document",
            "Gift" => ru ? "\u041f\u043e\u0434\u0430\u0440\u043e\u043a" : "Gift",
            "Special" => ru ? "\u041e\u0441\u043e\u0431\u043e\u0435" : "Special",
            _ => ru ? "\u041b\u0438\u0447\u043d\u043e\u0435" : "Personal"
        };
    }
}
