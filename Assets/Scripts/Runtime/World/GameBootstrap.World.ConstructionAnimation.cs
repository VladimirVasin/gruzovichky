using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float ServiceConstructionDuration = 3f;
    private const float ServiceConstructionPartDuration = 0.68f;

    private sealed class ConstructionAnimationPart
    {
        public Transform Transform;
        public Vector3 LocalPosition;
        public Vector3 LocalScale;
        public Quaternion LocalRotation;
        public float Delay;
        public float Drop;
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

    private void StartBarConstructionAnimation(LocationData location)
    {
        StartImportedServiceConstructionAnimation(location);
    }

    private void StartGamblingHallConstructionAnimation(LocationData location)
    {
        StartImportedServiceConstructionAnimation(location);
    }

    private void StartImportedServiceConstructionAnimation(LocationData location)
    {
        if (location?.RootObject == null)
        {
            return;
        }

        StartCoroutine(AnimateImportedServiceConstruction(location));
    }

    private IEnumerator AnimateImportedServiceConstruction(LocationData location)
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
        while (elapsed < ServiceConstructionDuration)
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
            ApplyConstructionDustFrame(puffs, elapsed);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        RestoreConstructionParts(parts);
        RestoreConstructionLights(lights);
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
            float wobbleSeed = target.localPosition.x * 17f + target.localPosition.y * 23f + target.localPosition.z * 31f;
            parts.Add(new ConstructionAnimationPart
            {
                Transform = target,
                LocalPosition = target.localPosition,
                LocalScale = target.localScale,
                LocalRotation = target.localRotation,
                Delay = delay,
                Drop = Mathf.Lerp(0.20f, 0.78f, height01),
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

        float p = Mathf.Clamp01((elapsed - part.Delay) / ServiceConstructionPartDuration);
        if (p <= 0f)
        {
            part.Transform.localPosition = part.LocalPosition + Vector3.down * part.Drop;
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

        part.Transform.localPosition = Vector3.Lerp(part.LocalPosition + Vector3.down * part.Drop, part.LocalPosition, rise);
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

    private List<ConstructionDustPuff> CreateConstructionDustPuffs(Transform root, LocationData location)
    {
        List<ConstructionDustPuff> puffs = new();
        Vector3 center = new(
            (location.Min.x + location.Max.x + 1) * 0.5f,
            0.08f,
            (location.Min.y + location.Max.y + 1) * 0.5f);
        float radiusX = Mathf.Max(0.65f, (location.Max.x - location.Min.x + 1) * 0.48f);
        float radiusZ = Mathf.Max(0.65f, (location.Max.y - location.Min.y + 1) * 0.48f);
        int puffCount = location.Type == LocationType.GamblingHall ? 11 : 9;
        Color color = GetConstructionDustColor(location.Type);

        for (int i = 0; i < puffCount; i++)
        {
            float angle = i / (float)puffCount * Mathf.PI * 2f + 0.28f;
            Vector3 offset = new(Mathf.Cos(angle) * radiusX, 0f, Mathf.Sin(angle) * radiusZ);
            GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = location.Type == LocationType.GamblingHall
                ? "GamblingHallConstructionDustPuff"
                : "BarConstructionDustPuff";
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
        return type == LocationType.GamblingHall
            ? new Color(0.86f, 0.58f, 0.92f, 0.25f)
            : new Color(0.86f, 0.74f, 0.52f, 0.28f);
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
