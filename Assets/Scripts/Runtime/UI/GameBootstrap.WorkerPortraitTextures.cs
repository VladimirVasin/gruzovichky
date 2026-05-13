using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public partial class GameBootstrap
{
    private const string WorkerPortraitResourceRoot = "Art/Workers/Portraits";
    private const string WorkerPortraitManifestPath = WorkerPortraitResourceRoot + "/portrait_parts_manifest";
    private const float WorkerPortraitHairLayerYOffset = 7f;

    private static readonly string[] WorkerPortraitRequiredLayerOrder =
    {
        "background",
        "clothes",
        "hair_back",
        "ears",
        "head",
        "eyes",
        "brows",
        "nose",
        "mouth",
        "accessory_face",
        "hair_front",
        "accessory_over"
    };

    private static WorkerPortraitManifest workerPortraitManifest;
    private static bool workerPortraitManifestLoadAttempted;
    private static readonly Dictionary<string, Sprite> WorkerPortraitSpriteCache = new();

    private static bool TryDrawWorkerTexturePortraitScaled(DriverAgent driver, RectTransform root, float scale)
    {
        WorkerPortraitManifest manifest = GetWorkerPortraitManifest();
        if (manifest?.parts == null || manifest.parts.Length == 0)
        {
            return false;
        }

        string profile = GetWorkerPortraitProfile(driver.Race, driver.Gender);
        string raceName = driver.Race.ToString();
        string genderName = driver.Gender.ToString();
        int seed = StableWorkerPortraitHash(driver.DriverName) ^ (driver.DriverId * 73856093);
        string variantTag = GetWorkerPortraitVariantTag(manifest, raceName, genderName, profile, seed + 7);
        List<WorkerPortraitPart> parts = new();

        if (!TryAddWorkerPortraitPartPreferTag(parts, manifest, "background", raceName, genderName, profile, seed + 11, variantTag, null) ||
            !TryAddWorkerPortraitPartPreferTag(parts, manifest, "clothes", raceName, genderName, profile, seed + 13, variantTag, null) ||
            !TryAddWorkerPortraitPartPreferTag(parts, manifest, "head", raceName, genderName, profile, seed + 17, variantTag, null))
        {
            return false;
        }

        WorkerPortraitHairSelection hair = SelectWorkerPortraitHairParts(
            manifest,
            raceName,
            genderName,
            profile,
            seed + driver.PortraitHairStyle + 23,
            variantTag);
        if (hair.Back != null)
        {
            parts.Add(hair.Back);
        }

        TryAddWorkerPortraitPartPreferTag(parts, manifest, "ears", raceName, genderName, profile, seed + driver.PortraitHeadShape + 19, variantTag, null);
        TryAddWorkerPortraitPartPreferTag(parts, manifest, "eyes", raceName, genderName, profile, seed + driver.PortraitEyeStyle + 37, variantTag, null);
        TryAddWorkerPortraitPartPreferTag(parts, manifest, "brows", raceName, genderName, profile, seed + driver.PortraitEyeStyle + 41, variantTag, null);
        TryAddWorkerPortraitPartPreferTag(parts, manifest, "nose", raceName, genderName, profile, seed + driver.PortraitHeadShape + 43, variantTag, null);
        TryAddWorkerPortraitPartPreferTag(parts, manifest, "mouth", raceName, genderName, profile, seed + driver.PortraitMouthStyle + 47, variantTag, null);

        string faceMarkTag = GetWorkerPortraitFaceMarkTag(driver.PortraitAccessory, driver.Gender);
        if (!string.IsNullOrEmpty(variantTag) || !string.IsNullOrEmpty(faceMarkTag))
        {
            TryAddWorkerPortraitPartPreferTag(parts, manifest, "accessory_face", raceName, genderName, profile, seed + driver.PortraitAccessory + 53, variantTag, faceMarkTag);
        }

        if (hair.Front != null)
        {
            parts.Add(hair.Front);
        }

        if (driver.PortraitAccessory == 1)
        {
            TryAddWorkerPortraitPartPreferTag(parts, manifest, "accessory_over", raceName, genderName, profile, seed + 59, variantTag, "glasses");
        }

        parts.Sort((a, b) => CompareWorkerPortraitParts(manifest, a, b));

        Vector2 portraitSize = GetWorkerTexturePortraitSize(manifest, scale);
        List<Sprite> sprites = new(parts.Count);
        for (int i = 0; i < parts.Count; i++)
        {
            Sprite sprite = GetWorkerPortraitSprite(parts[i]);
            if (sprite == null)
            {
                return false;
            }

            sprites.Add(sprite);
        }

        for (int i = 0; i < parts.Count; i++)
        {
            CreatePortraitSpritePart(
                $"PortraitTexture_{parts[i].slot}_{i}",
                root,
                sprites[i],
                parts[i].slot,
                GetWorkerPortraitLayerOffset(parts[i].slot, scale),
                portraitSize);
        }

        return true;
    }

    private static WorkerPortraitManifest GetWorkerPortraitManifest()
    {
        if (workerPortraitManifestLoadAttempted)
        {
            return workerPortraitManifest;
        }

        workerPortraitManifestLoadAttempted = true;
        TextAsset asset = Resources.Load<TextAsset>(WorkerPortraitManifestPath);
        if (asset == null)
        {
            return null;
        }

        try
        {
            workerPortraitManifest = JsonUtility.FromJson<WorkerPortraitManifest>(asset.text);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Failed to parse worker portrait manifest: {ex.Message}");
            workerPortraitManifest = null;
        }

        return workerPortraitManifest;
    }

    private static Vector2 GetWorkerTexturePortraitSize(WorkerPortraitManifest manifest, float scale)
    {
        float width = 108f * scale;
        if (manifest?.canvas == null || manifest.canvas.width <= 0 || manifest.canvas.height <= 0)
        {
            return new Vector2(width, width);
        }

        float height = width * manifest.canvas.height / manifest.canvas.width;
        return new Vector2(width, height);
    }

    private static string GetWorkerPortraitProfile(WorkerRaceKind race, WorkerGender gender)
    {
        string genderSuffix = gender == WorkerGender.Female ? "female" : "male";
        return race switch
        {
            WorkerRaceKind.Zelen => $"zelen_soft_{genderSuffix}",
            WorkerRaceKind.Iskrian => $"iskrian_expressive_{genderSuffix}",
            _ => $"rovian_structured_{genderSuffix}"
        };
    }

    private static string GetWorkerPortraitFaceMarkTag(int accessory, WorkerGender gender)
    {
        return accessory switch
        {
            2 => "freckles",
            4 => "scar",
            5 when gender == WorkerGender.Female => "blush",
            _ => null
        };
    }

    private static string GetWorkerPortraitVariantTag(
        WorkerPortraitManifest manifest,
        string raceName,
        string genderName,
        string profile,
        int seed)
    {
        if (manifest?.parts == null)
        {
            return null;
        }

        List<string> variants = new();
        for (int i = 0; i < manifest.parts.Length; i++)
        {
            WorkerPortraitPart part = manifest.parts[i];
            if (!IsWorkerPortraitPartRaceGenderProfileCompatible(part, raceName, genderName, profile))
            {
                continue;
            }

            for (int tagIndex = 0; part.tags != null && tagIndex < part.tags.Length; tagIndex++)
            {
                string tag = part.tags[tagIndex];
                if (string.IsNullOrEmpty(tag) || !tag.StartsWith("variant_", System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (!variants.Contains(tag))
                {
                    variants.Add(tag);
                }
            }

            string implicitVariant = GetWorkerPortraitImplicitVariantTag(part);
            if (!string.IsNullOrEmpty(implicitVariant) && !variants.Contains(implicitVariant))
            {
                variants.Add(implicitVariant);
            }
        }

        if (variants.Count == 0)
        {
            return null;
        }

        variants.Sort(System.StringComparer.Ordinal);
        return variants[PositiveModulo(seed, variants.Count)];
    }

    private static bool TryAddWorkerPortraitPartPreferTag(
        List<WorkerPortraitPart> parts,
        WorkerPortraitManifest manifest,
        string slot,
        string raceName,
        string genderName,
        string profile,
        int seed,
        string preferredTag,
        string fallbackTag)
    {
        WorkerPortraitPart part = SelectWorkerPortraitPartPreferTag(
            manifest,
            slot,
            raceName,
            genderName,
            profile,
            seed,
            preferredTag,
            fallbackTag);
        if (part == null)
        {
            return false;
        }

        parts.Add(part);
        return true;
    }

    private static WorkerPortraitPart SelectWorkerPortraitPartPreferTag(
        WorkerPortraitManifest manifest,
        string slot,
        string raceName,
        string genderName,
        string profile,
        int seed,
        string preferredTag,
        string fallbackTag)
    {
        WorkerPortraitPart part = !string.IsNullOrEmpty(preferredTag)
            ? SelectWorkerPortraitPart(manifest, slot, raceName, genderName, profile, seed, preferredTag)
            : null;
        if (part != null)
        {
            return part;
        }

        part = !string.IsNullOrEmpty(fallbackTag) && fallbackTag != preferredTag
            ? SelectWorkerPortraitPart(manifest, slot, raceName, genderName, profile, seed, fallbackTag)
            : null;
        return part ?? SelectWorkerPortraitPart(manifest, slot, raceName, genderName, profile, seed, null);
    }

    private static WorkerPortraitPart SelectWorkerPortraitPart(
        WorkerPortraitManifest manifest,
        string slot,
        string raceName,
        string genderName,
        string profile,
        int seed,
        string requiredTag)
    {
        WorkerPortraitPart selected = null;
        int matches = 0;
        for (int i = 0; i < manifest.parts.Length; i++)
        {
            WorkerPortraitPart part = manifest.parts[i];
            if (!IsWorkerPortraitPartCompatible(part, slot, raceName, genderName, profile, requiredTag))
            {
                continue;
            }

            if (matches == 0 || PositiveModulo(seed, matches + 1) == 0)
            {
                selected = part;
            }

            matches++;
        }

        return selected;
    }

    private static bool IsWorkerPortraitPartCompatible(
        WorkerPortraitPart part,
        string slot,
        string raceName,
        string genderName,
        string profile,
        string requiredTag)
    {
        if (part == null || part.slot != slot)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(requiredTag) && !HasWorkerPortraitTag(part, requiredTag))
        {
            return false;
        }

        return IsWorkerPortraitPartRaceGenderProfileCompatible(part, raceName, genderName, profile);
    }

    private static bool IsWorkerPortraitPartRaceGenderProfileCompatible(
        WorkerPortraitPart part,
        string raceName,
        string genderName,
        string profile)
    {
        if (part == null)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(part.race) && part.race != "Shared" && part.race != raceName)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(part.gender) && part.gender != "Shared" && part.gender != genderName)
        {
            return false;
        }

        if (part.compatibleProfiles == null || part.compatibleProfiles.Length == 0)
        {
            return true;
        }

        for (int i = 0; i < part.compatibleProfiles.Length; i++)
        {
            if (part.compatibleProfiles[i] == profile)
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasWorkerPortraitTag(WorkerPortraitPart part, string tag)
    {
        if (part == null || string.IsNullOrEmpty(tag))
        {
            return false;
        }

        if (GetWorkerPortraitImplicitVariantTag(part) == tag)
        {
            return true;
        }

        if (part.tags == null)
        {
            return false;
        }

        for (int i = 0; i < part.tags.Length; i++)
        {
            if (part.tags[i] == tag)
            {
                return true;
            }
        }

        return false;
    }

    private static WorkerPortraitHairSelection SelectWorkerPortraitHairParts(
        WorkerPortraitManifest manifest,
        string raceName,
        string genderName,
        string profile,
        int seed,
        string preferredVariantTag)
    {
        WorkerPortraitPart front = SelectWorkerPortraitPartPreferTag(
            manifest,
            "hair_front",
            raceName,
            genderName,
            profile,
            seed,
            preferredVariantTag,
            null);
        string pairedVariantTag = GetWorkerPortraitImplicitVariantTag(front) ?? preferredVariantTag;
        WorkerPortraitPart back = !string.IsNullOrEmpty(pairedVariantTag)
            ? SelectWorkerPortraitPart(manifest, "hair_back", raceName, genderName, profile, seed + 1, pairedVariantTag)
            : SelectWorkerPortraitPart(manifest, "hair_back", raceName, genderName, profile, seed + 1, null);

        return new WorkerPortraitHairSelection
        {
            Back = back,
            Front = front
        };
    }

    private static string GetWorkerPortraitImplicitVariantTag(WorkerPortraitPart part)
    {
        if (!string.IsNullOrEmpty(part?.id) &&
            part.id.StartsWith("variant_", System.StringComparison.Ordinal))
        {
            return part.id;
        }

        if (string.IsNullOrWhiteSpace(part?.file))
        {
            return null;
        }

        string file = part.file.Replace('\\', '/');
        int slashIndex = file.LastIndexOf('/');
        string name = slashIndex >= 0 ? file.Substring(slashIndex + 1) : file;
        int extensionIndex = name.LastIndexOf('.');
        if (extensionIndex >= 0)
        {
            name = name.Substring(0, extensionIndex);
        }

        return name.StartsWith("variant_", System.StringComparison.Ordinal)
            ? name
            : null;
    }

    private static int CompareWorkerPortraitParts(WorkerPortraitManifest manifest, WorkerPortraitPart a, WorkerPortraitPart b)
    {
        int layerCompare = GetWorkerPortraitLayerIndex(manifest, a?.slot).CompareTo(GetWorkerPortraitLayerIndex(manifest, b?.slot));
        if (layerCompare != 0)
        {
            return layerCompare;
        }

        int orderCompare = (a?.order ?? 0).CompareTo(b?.order ?? 0);
        return orderCompare != 0 ? orderCompare : string.CompareOrdinal(a?.id, b?.id);
    }

    private static int GetWorkerPortraitLayerIndex(WorkerPortraitManifest manifest, string slot)
    {
        if (string.IsNullOrEmpty(slot))
        {
            return int.MaxValue;
        }

        for (int i = 0; i < WorkerPortraitRequiredLayerOrder.Length; i++)
        {
            if (WorkerPortraitRequiredLayerOrder[i] == slot)
            {
                return i;
            }
        }

        if (manifest?.layerOrder == null)
        {
            return int.MaxValue;
        }

        for (int i = 0; i < manifest.layerOrder.Length; i++)
        {
            if (manifest.layerOrder[i] == slot)
            {
                return WorkerPortraitRequiredLayerOrder.Length + i;
            }
        }

        return int.MaxValue;
    }

    private static Sprite GetWorkerPortraitSprite(WorkerPortraitPart part)
    {
        if (part == null || string.IsNullOrWhiteSpace(part.file))
        {
            return null;
        }

        string resourcePath = WorkerPortraitResourceRoot + "/" + part.file;
        int extensionIndex = resourcePath.LastIndexOf('.');
        if (extensionIndex >= 0)
        {
            resourcePath = resourcePath.Substring(0, extensionIndex);
        }

        if (WorkerPortraitSpriteCache.TryGetValue(resourcePath, out Sprite cached))
        {
            return cached;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    texture.width);
            }
        }

        WorkerPortraitSpriteCache[resourcePath] = sprite;
        return sprite;
    }

    private static Image CreatePortraitSpritePart(string name, RectTransform parent, Sprite sprite, string slot, Vector2 anchoredPosition, Vector2 size)
    {
        Image image = CreatePortraitPart(name, parent, anchoredPosition, size, Color.white, slot, true);
        image.sprite = sprite;
        image.preserveAspect = true;
        return image;
    }

    private static Vector2 GetWorkerPortraitLayerOffset(string slot, float scale)
    {
        return slot is "hair_back" or "hair_front"
            ? new Vector2(0f, WorkerPortraitHairLayerYOffset * scale)
            : Vector2.zero;
    }

#pragma warning disable 0649
    [System.Serializable]
    private sealed class WorkerPortraitManifest
    {
        public int version;
        public WorkerPortraitCanvas canvas;
        public string[] layerOrder;
        public WorkerPortraitPart[] parts;
    }

    [System.Serializable]
    private sealed class WorkerPortraitCanvas
    {
        public int width;
        public int height;
    }

    [System.Serializable]
    private sealed class WorkerPortraitPart
    {
        public string id;
        public string file;
        public string slot;
        public string race;
        public string gender;
        public int order;
        public string[] compatibleProfiles;
        public string[] tags;
    }

    private struct WorkerPortraitHairSelection
    {
        public WorkerPortraitPart Back;
        public WorkerPortraitPart Front;
    }
#pragma warning restore 0649
}
