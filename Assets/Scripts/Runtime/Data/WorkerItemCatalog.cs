using System;
using UnityEngine;

[Serializable]
public sealed class WorkerItemCatalogData
{
    public WorkerItemData[] items;
}

[Serializable]
public sealed class WorkerItemData
{
    public string id;
    public string category;
    public bool stackable = true;
    public int maxStack = 1;
    public int baseValue;
    public LocalizedContentData title;
    public LocalizedContentData description;

    public string GetTitle(bool useRussian) => title?.Get(useRussian) ?? id ?? string.Empty;

    public string GetDescription(bool useRussian) => description?.Get(useRussian) ?? string.Empty;
}

public static class WorkerItemCatalog
{
    private const string ResourcePath = "GameData/worker-items";
    private static readonly WorkerItemCatalogData Empty = new()
    {
        items = Array.Empty<WorkerItemData>()
    };

    private static WorkerItemCatalogData cached;

    public static WorkerItemCatalogData Load()
    {
        if (cached != null)
        {
            return cached;
        }

        TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
        if (asset == null || string.IsNullOrWhiteSpace(asset.text))
        {
            cached = Empty;
            return cached;
        }

        try
        {
            cached = JsonUtility.FromJson<WorkerItemCatalogData>(asset.text);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load worker item catalog '{ResourcePath}': {ex.Message}");
            cached = Empty;
        }

        cached ??= Empty;
        cached.items ??= Array.Empty<WorkerItemData>();
        Normalize(cached);
        return cached;
    }

    public static bool TryGet(string itemId, out WorkerItemData itemData)
    {
        itemData = null;
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        WorkerItemCatalogData catalog = Load();
        for (int i = 0; i < catalog.items.Length; i++)
        {
            WorkerItemData candidate = catalog.items[i];
            if (candidate != null && string.Equals(candidate.id, itemId, StringComparison.Ordinal))
            {
                itemData = candidate;
                return true;
            }
        }

        return false;
    }

    private static void Normalize(WorkerItemCatalogData catalog)
    {
        if (catalog?.items == null)
        {
            return;
        }

        for (int i = 0; i < catalog.items.Length; i++)
        {
            WorkerItemData item = catalog.items[i];
            if (item == null)
            {
                continue;
            }

            item.id = item.id?.Trim() ?? string.Empty;
            item.category = string.IsNullOrWhiteSpace(item.category) ? "Personal" : item.category.Trim();
            item.maxStack = Math.Max(1, item.maxStack);
            if (!item.stackable)
            {
                item.maxStack = 1;
            }
        }
    }
}
