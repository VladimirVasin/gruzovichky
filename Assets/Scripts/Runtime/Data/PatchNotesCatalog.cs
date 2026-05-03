using System;
using UnityEngine;

[Serializable]
public sealed class PatchNotesCatalogData
{
    public LocalizedContentData intro;
    public PatchNotesVersionData[] versions;
}

[Serializable]
public sealed class PatchNotesVersionData
{
    public string version;
    public LocalizedContentData title;
    public PatchNotesSectionData[] sections;

    public string GetTitle(bool useRussian)
    {
        string localizedTitle = title?.Get(useRussian);
        return string.IsNullOrWhiteSpace(localizedTitle) ? version ?? string.Empty : localizedTitle;
    }
}

[Serializable]
public sealed class PatchNotesSectionData
{
    public LocalizedContentData title;
    public LocalizedContentData body;

    public string GetTitle(bool useRussian) => title?.Get(useRussian) ?? string.Empty;

    public string GetBody(bool useRussian) => body?.Get(useRussian) ?? string.Empty;
}

public static class PatchNotesCatalog
{
    private const string ResourcePath = "GameData/patch-notes";
    private static readonly PatchNotesCatalogData Empty = new()
    {
        versions = Array.Empty<PatchNotesVersionData>()
    };

    private static PatchNotesCatalogData cached;

    public static PatchNotesCatalogData Load()
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
            cached = JsonUtility.FromJson<PatchNotesCatalogData>(asset.text);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to load patch notes catalog '{ResourcePath}': {ex.Message}");
            cached = Empty;
        }

        cached ??= Empty;
        cached.versions ??= Array.Empty<PatchNotesVersionData>();
        return cached;
    }
}
