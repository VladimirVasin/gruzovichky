using System;
using UnityEngine;

[Serializable]
public sealed class BuildCatalogData
{
    public BuildCatalogCategoryData[] categories;
}

[Serializable]
public sealed class BuildCatalogCategoryData
{
    public LocalizedContentData label;
    public bool expanded;
    public BuildCatalogItemData[] items;

    public string GetLabel(bool useRussian) => label?.Get(useRussian) ?? string.Empty;
}

[Serializable]
public sealed class BuildCatalogItemData
{
    public string tool;
    public string abbrev;
    public LocalizedContentData title;
    public string color;
    public int cost;
    public LocalizedContentData activeDescription;
    public LocalizedContentData description;
    public LocalizedContentData alreadyBuiltDescription;

    public string GetAbbrev() => string.IsNullOrWhiteSpace(abbrev) ? "?" : abbrev;

    public string GetTitle(bool useRussian) => title?.Get(useRussian) ?? tool ?? string.Empty;

    public string GetActiveDescription(bool useRussian) => activeDescription?.Get(useRussian) ?? string.Empty;

    public string GetDescription(bool useRussian) => description?.Get(useRussian) ?? string.Empty;

    public string GetAlreadyBuiltDescription(bool useRussian) => alreadyBuiltDescription?.Get(useRussian) ?? string.Empty;

    public Color GetColor(Color fallback)
    {
        return !string.IsNullOrWhiteSpace(color) && ColorUtility.TryParseHtmlString(color, out Color parsed)
            ? parsed
            : fallback;
    }
}

public static class BuildCatalog
{
    private const string ResourcePath = "GameData/build-catalog";
    private static readonly BuildCatalogData Empty = new()
    {
        categories = Array.Empty<BuildCatalogCategoryData>()
    };

    private static BuildCatalogData cached;

    public static BuildCatalogData Load()
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
            cached = JsonUtility.FromJson<BuildCatalogData>(asset.text);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load build catalog '{ResourcePath}': {ex.Message}");
            cached = Empty;
        }

        cached ??= Empty;
        cached.categories ??= Array.Empty<BuildCatalogCategoryData>();
        return cached;
    }
}
