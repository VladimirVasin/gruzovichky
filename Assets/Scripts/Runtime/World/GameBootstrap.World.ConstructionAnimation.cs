using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float LocationConstructionDuration = 3f;
    private const float LocationConstructionPartDuration = 0.68f;

    private sealed class ConstructionAnimationPart
    {
        public Transform Transform;
        public Vector3 LocalPosition;
        public Vector3 HiddenLocalPosition;
        public Vector3 LocalScale;
        public Quaternion LocalRotation;
        public float Delay;
        public float WobbleDegrees;
    }

    private sealed class ConstructionAnimationLight
    {
        public Light Light;
        public float Intensity;
        public float Range;
    }

    private sealed class ConstructionDustPuff
    {
        public Transform Transform;
        public Renderer Renderer;
        public Vector3 LocalPosition;
        public Vector3 Drift;
        public Vector3 Scale;
        public Color Color;
        public float Delay;
        public float Duration;
    }

    private sealed class ConstructionMudPatch
    {
        public Transform Transform;
        public Material Material;
        public Vector3 LocalPosition;
        public Vector3 LocalScale;
        public Vector2 TextureOffset;
        public Vector2 TextureDrift;
        public Color Color;
        public float Delay;
        public float Duration;
    }

    private void StartLocationConstructionAnimation(LocationData location)
    {
        if (location?.RootObject == null)
        {
            return;
        }

        StartCoroutine(AnimateLocationConstruction(location));
    }

    private IEnumerator AnimateLocationConstruction(LocationData location)
    {
        Transform root = location.RootObject != null ? location.RootObject.transform : null;
        if (root == null)
        {
            yield break;
        }

        List<ConstructionAnimationPart> parts = CollectConstructionParts(root);
        if (parts.Count == 0)
        {
            yield break;
        }

        List<ConstructionAnimationLight> lights = CollectConstructionLights(root);
        List<ConstructionMudPatch> mudPatches = CreateConstructionMudPatches(root, location);
        List<ConstructionDustPuff> puffs = CreateConstructionDustPuffs(root, location);

        for (int i = 0; i < lights.Count; i++)
        {
            if (lights[i].Light == null)
            {
                continue;
            }

            lights[i].Light.intensity = 0f;
            lights[i].Light.range = 0f;
        }

        float elapsed = 0f;
        while (elapsed < LocationConstructionDuration)
        {
            if (root == null)
            {
                yield break;
            }

            for (int i = 0; i < parts.Count; i++)
            {
                ApplyConstructionPartFrame(parts[i], elapsed);
            }

            ApplyConstructionLightFrame(lights, elapsed);
            ApplyConstructionMudFrame(mudPatches, elapsed);
            ApplyConstructionDustFrame(puffs, elapsed);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        RestoreConstructionParts(parts);
        RestoreConstructionLights(lights);
        DestroyConstructionMudPatches(mudPatches);
        DestroyConstructionDustPuffs(puffs);
        RequestImportedBuildingDoorOpen(location);
    }

    private List<ConstructionAnimationPart> CollectConstructionParts(Transform root)
    {
        List<ConstructionAnimationPart> parts = new();
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        float minY = float.PositiveInfinity;
        float maxY = float.NegativeInfinity;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!IsConstructionAnimatedRenderer(renderer))
            {
                continue;
            }

            float y = root.InverseTransformPoint(renderer.bounds.center).y;
            minY = Mathf.Min(minY, y);
            maxY = Mathf.Max(maxY, y);
        }

        if (float.IsInfinity(minY) || float.IsInfinity(maxY) || float.IsNaN(minY) || float.IsNaN(maxY))
        {
            return parts;
        }

        HashSet<Transform> animatedTransforms = new();
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!IsConstructionAnimatedRenderer(renderer))
            {
                continue;
            }

            Transform target = renderer.transform;
            if (target == null || !animatedTransforms.Add(target))
            {
                continue;
            }

            float localY = root.InverseTransformPoint(renderer.bounds.center).y;
            float height01 = Mathf.InverseLerp(minY, maxY + 0.001f, localY);
            float delay = GetConstructionPartDelay(target, height01);
            float drop = Mathf.Lerp(0.20f, 0.78f, height01);
            float wobbleSeed = target.localPosition.x * 17f + target.localPosition.y * 23f + target.localPosition.z * 31f;
            parts.Add(new ConstructionAnimationPart
            {
                Transform = target,
                LocalPosition = target.localPosition,
                HiddenLocalPosition = GetConstructionHiddenLocalPosition(root, target, drop),
                LocalScale = target.localScale,
                LocalRotation = target.localRotation,
                Delay = delay,
                WobbleDegrees = Mathf.Lerp(8f, 18f, Mathf.Abs(Mathf.Sin(wobbleSeed)))
            });
        }

        parts.Sort((a, b) => a.Delay.CompareTo(b.Delay));
        return parts;
    }

    private static bool IsConstructionAnimatedRenderer(Renderer renderer)
    {
        return renderer != null &&
               renderer.enabled &&
               renderer.gameObject.activeInHierarchy &&
               renderer.transform != null;
    }

    private static Vector3 GetConstructionHiddenLocalPosition(Transform root, Transform target, float drop)
    {
        if (target == null)
        {
            return Vector3.zero;
        }

        Vector3 worldDrop = root != null
            ? root.TransformVector(Vector3.down * drop)
            : Vector3.down * drop;
        Vector3 localDrop = target.parent != null
            ? target.parent.InverseTransformVector(worldDrop)
            : worldDrop;
        return target.localPosition + localDrop;
    }

    private static float GetConstructionPartDelay(Transform transform, float height01)
    {
        string name = GetConstructionHierarchyName(transform);
        if (ContainsAnyIgnoreCase(name, "floor", "foundation", "base", "porch", "platform", "ground", "step"))
        {
            return Mathf.Lerp(0.00f, 0.14f, height01);
        }

        if (ContainsAnyIgnoreCase(name, "roof", "awning", "chimney", "tower", "dome"))
        {
            return Mathf.Lerp(1.10f, 1.42f, height01);
        }

        if (ContainsAnyIgnoreCase(
                name,
                "sign",
                "door",
                "window",
                "glass",
                "barrel",
                "bottle",
                "table",
                "chair",
                "stool",
                "bench",
                "lamp",
                "light",
                "neon",
                "slot",
                "jackpot",
                "coin",
                "chip",
                "card",
                "dice",
                "canopy"))
        {
            return Mathf.Lerp(1.62f, 2.18f, height01);
        }

        if (ContainsAnyIgnoreCase(name, "body", "wall", "facade", "main", "model", "room", "hall", "casino"))
        {
            return Mathf.Lerp(0.36f, 0.82f, height01);
        }

        return Mathf.Lerp(0.32f, 1.85f, height01);
    }

    private static string GetConstructionHierarchyName(Transform transform)
    {
        string name = string.Empty;
        Transform current = transform;
        int depth = 0;
        while (current != null && depth < 5)
        {
            name += "|" + current.name;
            current = current.parent;
            depth++;
        }

        return name;
    }

    private static bool ContainsAnyIgnoreCase(string text, params string[] tokens)
    {
        for (int i = 0; i < tokens.Length; i++)
        {
            if (text.IndexOf(tokens[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void ApplyConstructionPartFrame(ConstructionAnimationPart part, float elapsed)
    {
        if (part?.Transform == null)
        {
            return;
        }

        float p = Mathf.Clamp01((elapsed - part.Delay) / LocationConstructionPartDuration);
        if (p <= 0f)
        {
            part.Transform.localPosition = part.HiddenLocalPosition;
            part.Transform.localScale = Vector3.Scale(part.LocalScale, new Vector3(0.16f, 0.035f, 0.16f));
            part.Transform.localRotation = part.LocalRotation * Quaternion.Euler(0f, -part.WobbleDegrees, part.WobbleDegrees * 0.22f);
            return;
        }

        if (p >= 1f)
        {
            part.Transform.localPosition = part.LocalPosition;
            part.Transform.localScale = part.LocalScale;
            part.Transform.localRotation = part.LocalRotation;
            return;
        }

        float rise = EaseOutCubic(p);
        float pop = Mathf.Max(0.08f, EaseOutBack(p));
        float squash = 1f + Mathf.Sin(p * Mathf.PI) * 0.16f;
        float wobble = Mathf.Sin((1f - p) * Mathf.PI * 3.2f) * (1f - p) * part.WobbleDegrees;

        part.Transform.localPosition = Vector3.Lerp(part.HiddenLocalPosition, part.LocalPosition, rise);
        part.Transform.localScale = new Vector3(
            part.LocalScale.x * pop,
            part.LocalScale.y * Mathf.Max(0.04f, pop * squash),
            part.LocalScale.z * pop);
        part.Transform.localRotation = part.LocalRotation * Quaternion.Euler(0f, wobble, -wobble * 0.28f);
    }

    private static float EaseOutCubic(float value)
    {
        value = Mathf.Clamp01(value);
        float t = 1f - value;
        return 1f - t * t * t;
    }

    private static float EaseOutBack(float value)
    {
        value = Mathf.Clamp01(value);
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float t = value - 1f;
        return 1f + c3 * t * t * t + c1 * t * t;
    }

    private static List<ConstructionAnimationLight> CollectConstructionLights(Transform root)
    {
        List<ConstructionAnimationLight> lights = new();
        Light[] sourceLights = root.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < sourceLights.Length; i++)
        {
            Light light = sourceLights[i];
            if (light == null)
            {
                continue;
            }

            lights.Add(new ConstructionAnimationLight
            {
                Light = light,
                Intensity = light.intensity,
                Range = light.range
            });
        }

        return lights;
    }

    private static void ApplyConstructionLightFrame(List<ConstructionAnimationLight> lights, float elapsed)
    {
        float p = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - 2.15f) / 0.50f));
        float pulse = 1f + Mathf.Sin(Mathf.Clamp01((elapsed - 2.15f) / 0.50f) * Mathf.PI) * 0.35f;
        for (int i = 0; i < lights.Count; i++)
        {
            ConstructionAnimationLight item = lights[i];
            if (item.Light == null)
            {
                continue;
            }

            item.Light.intensity = item.Intensity * p * pulse;
            item.Light.range = item.Range * p;
        }
    }

    private static void RestoreConstructionLights(List<ConstructionAnimationLight> lights)
    {
        for (int i = 0; i < lights.Count; i++)
        {
            ConstructionAnimationLight item = lights[i];
            if (item.Light == null)
            {
                continue;
            }

            item.Light.intensity = item.Intensity;
            item.Light.range = item.Range;
        }
    }

    private static void RestoreConstructionParts(List<ConstructionAnimationPart> parts)
    {
        for (int i = 0; i < parts.Count; i++)
        {
            ConstructionAnimationPart part = parts[i];
            if (part?.Transform == null)
            {
                continue;
            }

            part.Transform.localPosition = part.LocalPosition;
            part.Transform.localScale = part.LocalScale;
            part.Transform.localRotation = part.LocalRotation;
        }
    }

    private List<ConstructionMudPatch> CreateConstructionMudPatches(Transform root, LocationData location)
    {
        List<ConstructionMudPatch> patches = new();
        if (root == null || location == null)
        {
            return patches;
        }

        int width = Mathf.Max(1, location.Max.x - location.Min.x + 1);
        int depth = Mathf.Max(1, location.Max.y - location.Min.y + 1);
        Vector3 center = new(
            (location.Min.x + location.Max.x + 1) * 0.5f,
            0.025f,
            (location.Min.y + location.Max.y + 1) * 0.5f);

        AddConstructionMudPatch(
            patches,
            root,
            $"{location.Type}ConstructionMud",
            center,
            new Vector3(width * 1.04f, 0.012f, depth * 1.04f),
            new Vector2(width * 0.72f, depth * 0.72f),
            GetGroundTextureOffset(location.Min.x, location.Min.y, 79 + (int)location.Type * 3),
            new Vector2(0.018f, 0.011f),
            GetConstructionMudColor(location.Type, 0.25f),
            0f,
            LocationConstructionDuration);

        AddConstructionMudPatch(
            patches,
            root,
            "ConstructionMudTrackA",
            center + new Vector3(width * 0.18f, 0.004f, -depth * 0.19f),
            new Vector3(width * 0.62f, 0.010f, Mathf.Max(0.26f, depth * 0.18f)),
            new Vector2(width * 0.92f, 0.36f),
            GetGroundTextureOffset(location.Max.x, location.Min.y, 89),
            new Vector2(0.025f, -0.006f),
            new Color(0.30f, 0.24f, 0.18f, 0.18f),
            0.12f,
            LocationConstructionDuration * 0.9f);

        AddConstructionMudPatch(
            patches,
            root,
            "ConstructionMudTrackB",
            center + new Vector3(-width * 0.20f, 0.006f, depth * 0.16f),
            new Vector3(width * 0.54f, 0.010f, Mathf.Max(0.24f, depth * 0.16f)),
            new Vector2(width * 0.82f, 0.32f),
            GetGroundTextureOffset(location.Min.x, location.Max.y, 97),
            new Vector2(-0.018f, 0.009f),
            new Color(0.34f, 0.25f, 0.18f, 0.15f),
            0.22f,
            LocationConstructionDuration * 0.82f);

        return patches;
    }

    private static Color GetConstructionMudColor(LocationType type, float alpha)
    {
        return type switch
        {
            LocationType.GamblingHall => new Color(0.42f, 0.30f, 0.44f, alpha),
            LocationType.CityPark or LocationType.Forest => new Color(0.30f, 0.34f, 0.18f, alpha),
            LocationType.Parking
                or LocationType.Warehouse
                or LocationType.Sawmill
                or LocationType.FurnitureFactory
                or LocationType.Docks
                or LocationType.GasStation
                or LocationType.CleaningDepot
                or LocationType.CarMarket => new Color(0.34f, 0.31f, 0.25f, alpha),
            _ => new Color(0.46f, 0.36f, 0.22f, alpha)
        };
    }

    private void AddConstructionMudPatch(
        List<ConstructionMudPatch> patches,
        Transform root,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Vector2 textureScale,
        Vector2 textureOffset,
        Vector2 textureDrift,
        Color color,
        float delay,
        float duration)
    {
        GameObject patch = GameObject.CreatePrimitive(PrimitiveType.Cube);
        patch.name = name;
        patch.transform.SetParent(root, false);
        patch.transform.localPosition = localPosition;
        patch.transform.localScale = new Vector3(localScale.x, 0.001f, localScale.z);

        Renderer renderer = patch.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color startColor = color;
            startColor.a = 0f;
            renderer.sharedMaterial = CreateTransparentOverlayMaterial(startColor, constructionMudSurfaceTexture, textureScale, textureOffset);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        if (patch.TryGetComponent(out Collider collider))
        {
            Destroy(collider);
        }

        patches.Add(new ConstructionMudPatch
        {
            Transform = patch.transform,
            Material = renderer != null ? renderer.sharedMaterial : null,
            LocalPosition = localPosition,
            LocalScale = localScale,
            TextureOffset = textureOffset,
            TextureDrift = textureDrift,
            Color = color,
            Delay = delay,
            Duration = duration
        });
    }

    private static void ApplyConstructionMudFrame(List<ConstructionMudPatch> patches, float elapsed)
    {
        for (int i = 0; i < patches.Count; i++)
        {
            ConstructionMudPatch patch = patches[i];
            if (patch?.Transform == null)
            {
                continue;
            }

            float p = Mathf.Clamp01((elapsed - patch.Delay) / Mathf.Max(0.01f, patch.Duration));
            float appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(p / 0.20f));
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 1f, p));
            float alpha = patch.Color.a * appear * fade;
            float settle = 0.92f + appear * 0.08f + Mathf.Sin(p * Mathf.PI) * 0.025f;
            patch.Transform.localPosition = patch.LocalPosition + Vector3.up * (0.006f * appear);
            patch.Transform.localScale = new Vector3(patch.LocalScale.x * settle, patch.LocalScale.y, patch.LocalScale.z * settle);

            if (patch.Material != null)
            {
                Color color = patch.Color;
                color.a = alpha;
                SetConstructionOverlayMaterialColor(patch.Material, color);
                SetOverlayTextureOffset(patch.Material, patch.TextureOffset + patch.TextureDrift * elapsed);
            }
        }
    }

    private static void SetConstructionOverlayMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private static void DestroyConstructionMudPatches(List<ConstructionMudPatch> patches)
    {
        for (int i = 0; i < patches.Count; i++)
        {
            ConstructionMudPatch patch = patches[i];
            if (patch?.Transform != null)
            {
                UnityEngine.Object.Destroy(patch.Transform.gameObject);
            }
        }
    }

    private List<ConstructionDustPuff> CreateConstructionDustPuffs(Transform root, LocationData location)
    {
        List<ConstructionDustPuff> puffs = new();
        Vector3 center = new(
            (location.Min.x + location.Max.x + 1) * 0.5f,
            0.08f,
            (location.Min.y + location.Max.y + 1) * 0.5f);
        float radiusX = Mathf.Max(0.65f, (location.Max.x - location.Min.x + 1) * 0.48f);
        float radiusZ = Mathf.Max(0.65f, (location.Max.y - location.Min.y + 1) * 0.48f);
        int footprintArea = Mathf.Max(1, (location.Max.x - location.Min.x + 1) * (location.Max.y - location.Min.y + 1));
        int puffCount = Mathf.Clamp(6 + footprintArea / 4, 7, 14);
        Color color = GetConstructionDustColor(location.Type);

        for (int i = 0; i < puffCount; i++)
        {
            float angle = i / (float)puffCount * Mathf.PI * 2f + 0.28f;
            Vector3 offset = new(Mathf.Cos(angle) * radiusX, 0f, Mathf.Sin(angle) * radiusZ);
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = $"{location.Type}ConstructionDustPuff";
            puff.transform.SetParent(root, false);
            puff.transform.localPosition = center + offset;
            puff.transform.localScale = Vector3.zero;
            Renderer renderer = puff.GetComponent<Renderer>();
            renderer.material = CreateTransparentOverlayMaterial(color);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (puff.TryGetComponent(out Collider collider))
            {
                Destroy(collider);
            }

            puffs.Add(new ConstructionDustPuff
            {
                Transform = puff.transform,
                Renderer = renderer,
                LocalPosition = puff.transform.localPosition,
                Drift = new Vector3(Mathf.Cos(angle) * 0.18f, 0.20f + i % 3 * 0.04f, Mathf.Sin(angle) * 0.18f),
                Scale = Vector3.one * (0.13f + i % 4 * 0.025f),
                Color = color,
                Delay = i % 3 * 0.10f,
                Duration = 1.05f + i % 2 * 0.18f
            });
        }

        return puffs;
    }

    private static Color GetConstructionDustColor(LocationType type)
    {
        return type switch
        {
            LocationType.GamblingHall => new Color(0.86f, 0.58f, 0.92f, 0.25f),
            LocationType.CityPark or LocationType.Forest => new Color(0.58f, 0.72f, 0.38f, 0.22f),
            LocationType.Parking
                or LocationType.Warehouse
                or LocationType.Sawmill
                or LocationType.FurnitureFactory
                or LocationType.Docks
                or LocationType.GasStation
                or LocationType.CleaningDepot
                or LocationType.CarMarket => new Color(0.72f, 0.68f, 0.60f, 0.25f),
            _ => new Color(0.86f, 0.74f, 0.52f, 0.28f)
        };
    }

    private static void ApplyConstructionDustFrame(List<ConstructionDustPuff> puffs, float elapsed)
    {
        for (int i = 0; i < puffs.Count; i++)
        {
            ConstructionDustPuff puff = puffs[i];
            if (puff?.Transform == null)
            {
                continue;
            }

            float p = Mathf.Clamp01((elapsed - puff.Delay) / puff.Duration);
            float scale = Mathf.Sin(p * Mathf.PI);
            if (elapsed < puff.Delay || p >= 1f)
            {
                puff.Transform.localScale = Vector3.zero;
                continue;
            }

            puff.Transform.localPosition = puff.LocalPosition + puff.Drift * EaseOutCubic(p);
            puff.Transform.localScale = puff.Scale * scale;
            if (puff.Renderer != null && puff.Renderer.material != null)
            {
                Color color = puff.Color;
                color.a *= 1f - p;
                puff.Renderer.material.color = color;
            }
        }
    }

    private static void DestroyConstructionDustPuffs(List<ConstructionDustPuff> puffs)
    {
        for (int i = 0; i < puffs.Count; i++)
        {
            ConstructionDustPuff puff = puffs[i];
            if (puff?.Transform != null)
            {
                UnityEngine.Object.Destroy(puff.Transform.gameObject);
            }
        }
    }
}
