using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap
{
    private const string ImportedBusResourcePath = "Vehicles/Bus/Bus";
    private static readonly Vector3 ImportedBusTargetSize = new(2.45f, 1.16f, 0.92f);
    private const float ImportedBusBottomY = 0.02f;

    private bool hasLoggedImportedBusVisualInfo;
    private bool hasLoggedImportedBusVisualFailure;

    private bool TryBuildImportedBusVisual(
        Transform parent,
        Color bodyColor,
        string leftLightName,
        string rightLightName,
        out Renderer headlightLeftRenderer,
        out Renderer headlightRightRenderer,
        out Material headlightLeftMaterial,
        out Material headlightRightMaterial,
        out Light leftLight,
        out Light rightLight)
    {
        headlightLeftRenderer = null;
        headlightRightRenderer = null;
        headlightLeftMaterial = null;
        headlightRightMaterial = null;
        leftLight = null;
        rightLight = null;

        GameObject prefab = Resources.Load<GameObject>(ImportedBusResourcePath);
        if (prefab == null)
        {
            return false;
        }

        GameObject imported = Instantiate(prefab, parent);
        imported.name = "ImportedBusModel";
        imported.transform.localPosition = Vector3.zero;
        imported.transform.localRotation = Quaternion.identity;
        imported.transform.localScale = Vector3.one;

        DisableImportedBusRuntimeComponents(imported);
        DisableImportedBusColliders(imported);
        ConfigureImportedBusRenderers(imported);
        if (!FitImportedBusModel(parent, imported, ImportedBusTargetSize, ImportedBusBottomY, out Vector3 rawSize, out Vector3 fittedSize, out float scale))
        {
            Destroy(imported);
            if (!hasLoggedImportedBusVisualFailure)
            {
                hasLoggedImportedBusVisualFailure = true;
                SessionDebugLogger.Log("BUS", $"Imported bus model from Resources/{ImportedBusResourcePath} has no usable renderers; using procedural fallback.");
            }

            return false;
        }

        CreateImportedBusWheelPivots(imported.transform);
        CreateImportedBusShadowBlob(parent);
        CreateImportedBusHeadlightVisual(
            parent,
            leftLightName,
            new Vector3(1.23f, 0.46f, -0.26f),
            out headlightLeftRenderer,
            out headlightLeftMaterial,
            out leftLight);
        CreateImportedBusHeadlightVisual(
            parent,
            rightLightName,
            new Vector3(1.23f, 0.46f, 0.26f),
            out headlightRightRenderer,
            out headlightRightMaterial,
            out rightLight);

        if (!hasLoggedImportedBusVisualInfo)
        {
            hasLoggedImportedBusVisualInfo = true;
            SessionDebugLogger.Log(
                "BUS",
                $"Loaded imported bus model from Resources/{ImportedBusResourcePath}. Bounds raw={FormatImportedBusSize(rawSize)} fitted={FormatImportedBusSize(fittedSize)} target={FormatImportedBusSize(ImportedBusTargetSize)} scale={scale:0.000}.");
        }

        return true;
    }

    private void CreateImportedBusShadowBlob(Transform parent)
    {
        GameObject shadowBlob = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shadowBlob.name = "ImportedBusShadowBlob";
        shadowBlob.transform.SetParent(parent, false);
        shadowBlob.transform.localPosition = new Vector3(0f, -0.01f, 0f);
        shadowBlob.transform.localScale = new Vector3(2.50f, 0.008f, 0.96f);
        Renderer renderer = shadowBlob.GetComponent<Renderer>();
        renderer.material = CreateTransparentOverlayMaterial(new Color(0f, 0f, 0f, 0.14f));
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (shadowBlob.TryGetComponent(out Collider collider))
        {
            Object.Destroy(collider);
        }
    }

    private void CreateImportedBusHeadlightVisual(
        Transform parent,
        string lightName,
        Vector3 localPosition,
        out Renderer renderer,
        out Material material,
        out Light light)
    {
        GameObject headlightVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlightVisual.transform.SetParent(parent, false);
        headlightVisual.transform.localPosition = localPosition;
        headlightVisual.transform.localScale = new Vector3(0.062f, 0.092f, 0.124f);
        ApplyColor(headlightVisual, new Color(0.34f, 0.3f, 0.22f), VisualSmoothnessGlass);
        ConfigureShadowVisual(headlightVisual, VisualSmoothnessGlass);

        GameObject lightObject = new(lightName);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition + new Vector3(0.03f, 0.01f, 0f);
        lightObject.transform.localRotation = Quaternion.Euler(8f, 90f, 0f);
        light = lightObject.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = new Color(1f, 0.66f, 0.34f);
        light.range = 3.6f;
        light.spotAngle = 42f;
        light.innerSpotAngle = 22f;
        light.intensity = 0f;
        light.shadows = LightShadows.None;
        light.enabled = false;

        renderer = headlightVisual.GetComponent<Renderer>();
        material = renderer != null ? renderer.material : null;
    }

    private void ConfigureImportedBusRenderers(GameObject imported)
    {
        Renderer[] renderers = imported.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (ShouldIgnoreImportedBusRenderer(renderer))
            {
                renderer.enabled = false;
                continue;
            }

            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private static bool ShouldIgnoreImportedBusRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            return true;
        }

        Transform current = renderer.transform;
        while (current != null)
        {
            string name = current.name.ToLowerInvariant();
            if (name.Contains("camera") || name.Contains("view"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static void DisableImportedBusRuntimeComponents(GameObject root)
    {
        Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Object.Destroy(cameras[i]);
        }

        Light[] lights = root.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            Object.Destroy(lights[i]);
        }

        Animator[] animators = root.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Object.Destroy(animators[i]);
        }

        Animation[] animations = root.GetComponentsInChildren<Animation>(true);
        for (int i = 0; i < animations.Length; i++)
        {
            Object.Destroy(animations[i]);
        }
    }

    private static void DisableImportedBusColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Object.Destroy(colliders[i]);
        }
    }

    private void CreateImportedBusWheelPivots(Transform importedRoot)
    {
        List<Transform> wheelParts = FindImportedVehicleParts(importedRoot, IsImportedBusWheelPartName);
        for (int i = 0; i < wheelParts.Count; i++)
        {
            CreateImportedVehicleWheelPivot(wheelParts[i], "ImportedBusWheelPivot_" + i);
        }
    }

    private static bool IsImportedBusWheelPartName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        string lower = name.ToLowerInvariant();
        return (lower.Contains("wheel") || lower.Contains("tire") || lower.Contains("tyre")) &&
            !lower.Contains("fender");
    }

    private bool FitImportedBusModel(
        Transform boundsRoot,
        GameObject imported,
        Vector3 targetSize,
        float bottomY,
        out Vector3 rawSize,
        out Vector3 fittedSize,
        out float scale)
    {
        rawSize = Vector3.zero;
        fittedSize = Vector3.zero;
        scale = 1f;

        Renderer[] renderers = imported.GetComponentsInChildren<Renderer>(true);
        if (!TryGetBusLocalRendererBounds(boundsRoot, renderers, out Bounds bounds))
        {
            return false;
        }

        rawSize = bounds.size;
        float scaleX = targetSize.x / Mathf.Max(0.001f, bounds.size.x);
        float scaleY = targetSize.y / Mathf.Max(0.001f, bounds.size.y);
        float scaleZ = targetSize.z / Mathf.Max(0.001f, bounds.size.z);
        scale = Mathf.Clamp(Mathf.Min(scaleX, scaleY, scaleZ), 0.035f, 2.5f);
        imported.transform.localScale = Vector3.one * scale;

        if (!TryGetBusLocalRendererBounds(boundsRoot, renderers, out Bounds scaledBounds))
        {
            return false;
        }

        fittedSize = scaledBounds.size;
        imported.transform.localPosition += new Vector3(
            -scaledBounds.center.x,
            bottomY - scaledBounds.min.y,
            -scaledBounds.center.z);
        return true;
    }

    private static bool TryGetBusLocalRendererBounds(Transform root, Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || ShouldIgnoreImportedBusRenderer(renderer))
            {
                continue;
            }

            Bounds worldBounds = renderer.bounds;
            Vector3 center = worldBounds.center;
            Vector3 extents = worldBounds.extents;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldPoint = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 localPoint = root.InverseTransformPoint(worldPoint);
                        if (!hasBounds)
                        {
                            bounds = new Bounds(localPoint, Vector3.zero);
                            hasBounds = true;
                        }
                        else
                        {
                            bounds.Encapsulate(localPoint);
                        }
                    }
                }
            }
        }

        return hasBounds;
    }

    private static string FormatImportedBusSize(Vector3 size)
    {
        return $"{size.x:0.00}x{size.y:0.00}x{size.z:0.00}";
    }
}
