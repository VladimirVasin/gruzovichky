using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap
{
    private const string ImportedTruckResourcePath = "Vehicles/Trucks/Truck";
    private static readonly Vector3 ImportedTruckTargetSize = new(1.20f, 0.96f, 1.60f);
    private const float ImportedTruckBottomY = 0.02f;
    private static readonly Quaternion TruckProceduralWheelBaseRotation = Quaternion.Euler(0f, 0f, 90f);

    private Transform BuildTruckVisualModel()
    {
        if (TryBuildImportedTruckVisual(out Transform importedCargoRoot))
        {
            return importedCargoRoot;
        }

        return BuildProceduralTruckVisualModel();
    }

    private bool TryBuildImportedTruckVisual(out Transform cargoVisualRoot)
    {
        cargoVisualRoot = null;
        GameObject prefab = Resources.Load<GameObject>(ImportedTruckResourcePath);
        if (prefab == null)
        {
            return false;
        }

        Transform bodyAnchor = CreateTruckVisualAnchor("TruckBodyImported", Vector3.zero);
        truckBodyTransform = bodyAnchor;
        truckCabinTransform = CreateTruckVisualAnchor("TruckCabinImported", Vector3.zero);

        GameObject imported = Instantiate(prefab, bodyAnchor);
        imported.name = "ImportedTruckModel";
        imported.transform.localPosition = Vector3.zero;
        imported.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
        imported.transform.localScale = Vector3.one;
        DisableImportedTruckRuntimeComponents(imported);
        DisableVisualColliders(imported);
        ConfigureImportedTruckRenderers(imported);
        if (!FitImportedTruckModel(bodyAnchor, imported, ImportedTruckTargetSize, ImportedTruckBottomY))
        {
            Destroy(imported);
            Destroy(bodyAnchor.gameObject);
            Destroy(truckCabinTransform.gameObject);
            truckBodyTransform = null;
            truckCabinTransform = null;
            SessionDebugLogger.Log("TRUCK", $"Imported truck model from Resources/{ImportedTruckResourcePath} has no usable renderers; using procedural fallback.");
            return false;
        }

        CreateImportedTruckWheelPivots(imported.transform);
        CreateTruckShadowBlob();
        CreateTruckHeadlightVisual(new Vector3(-0.25f, 0.51f, 0.64f), true);
        CreateTruckHeadlightVisual(new Vector3(0.25f, 0.51f, 0.64f), false);
        CreateTruckHeadlightBeam(new Vector3(-0.25f, 0.51f, 0.69f));
        CreateTruckHeadlightBeam(new Vector3(0.25f, 0.51f, 0.69f));

        cargoVisualRoot = CreateTruckCargoVisualRoot(new Vector3(0f, 0.62f, -0.33f));
        SessionDebugLogger.Log("TRUCK", $"Loaded imported truck model from Resources/{ImportedTruckResourcePath}.");
        return true;
    }

    private Transform BuildProceduralTruckVisualModel()
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(truckVisualRoot, false);
        body.transform.localPosition = new Vector3(0f, 0.25f, 0f);
        body.transform.localScale = new Vector3(0.7f, 0.35f, 1f);
        ApplyColor(body, new Color(0.85f, 0.2f, 0.18f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(body, VisualSmoothnessVehicleMetal);
        truckBodyTransform = body.transform;

        GameObject bodyStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bodyStripe.transform.SetParent(truckVisualRoot, false);
        bodyStripe.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        bodyStripe.transform.localScale = new Vector3(0.74f, 0.05f, 1.02f);
        ApplyColor(bodyStripe, new Color(0.96f, 0.9f, 0.72f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(bodyStripe, VisualSmoothnessVehicleMetal);

        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(truckVisualRoot, false);
        cabin.transform.localPosition = new Vector3(0f, 0.4f, 0.2f);
        cabin.transform.localScale = new Vector3(0.55f, 0.4f, 0.45f);
        ApplyColor(cabin, new Color(0.95f, 0.82f, 0.28f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(cabin, VisualSmoothnessVehicleMetal);
        truckCabinTransform = cabin.transform;

        GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windshield.transform.SetParent(truckVisualRoot, false);
        windshield.transform.localPosition = new Vector3(0f, 0.43f, 0.42f);
        windshield.transform.localScale = new Vector3(0.42f, 0.18f, 0.04f);
        ApplyColor(windshield, new Color(0.68f, 0.86f, 0.94f), VisualSmoothnessGlass);
        ConfigureShadowVisual(windshield, VisualSmoothnessGlass);

        CreateTruckShadowBlob();
        CreateTruckHeadlightVisual(new Vector3(-0.18f, 0.39f, 0.46f), true);
        CreateTruckHeadlightVisual(new Vector3(0.18f, 0.39f, 0.46f), false);
        CreateTruckHeadlightBeam(new Vector3(-0.18f, 0.39f, 0.5f));
        CreateTruckHeadlightBeam(new Vector3(0.18f, 0.39f, 0.5f));

        Vector3[] wheelOffsets =
        {
            new(-0.28f, 0.08f, 0.32f),
            new(0.28f, 0.08f, 0.32f),
            new(-0.28f, 0.08f, -0.32f),
            new(0.28f, 0.08f, -0.32f)
        };

        for (int i = 0; i < wheelOffsets.Length; i++)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.transform.SetParent(truckVisualRoot, false);
            wheel.transform.localPosition = wheelOffsets[i];
            wheel.transform.localRotation = TruckProceduralWheelBaseRotation;
            wheel.transform.localScale = new Vector3(0.12f, 0.05f, 0.12f);
            ApplyColor(wheel, new Color(0.14f, 0.14f, 0.14f), VisualSmoothnessRubber);
            ConfigureShadowVisual(wheel, VisualSmoothnessRubber);
            truckWheels.Add(wheel.transform);
            if (i < 2)
            {
                truckFrontWheels.Add(wheel.transform);
            }
        }

        return CreateTruckCargoVisualRoot(new Vector3(0f, 0.47f, -0.24f));
    }

    private Transform CreateTruckVisualAnchor(string name, Vector3 localPosition)
    {
        Transform anchor = new GameObject(name).transform;
        anchor.SetParent(truckVisualRoot, false);
        anchor.localPosition = localPosition;
        anchor.localRotation = Quaternion.identity;
        anchor.localScale = Vector3.one;
        return anchor;
    }

    private Transform CreateTruckCargoVisualRoot(Vector3 localPosition)
    {
        Transform cargoRoot = new GameObject("CargoVisualRoot").transform;
        cargoRoot.SetParent(truckVisualRoot, false);
        cargoRoot.localPosition = localPosition;
        cargoRoot.gameObject.SetActive(false);
        return cargoRoot;
    }

    private void CreateTruckShadowBlob()
    {
        GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shadow.name = "TruckShadowBlob";
        shadow.transform.SetParent(truckVisualRoot, false);
        shadow.transform.localPosition = new Vector3(0f, -0.01f, 0f);
        shadow.transform.localScale = new Vector3(1.20f, 0.01f, 1.58f);
        Renderer renderer = shadow.GetComponent<Renderer>();
        renderer.material = CreateTransparentOverlayMaterial(new Color(0f, 0f, 0f, 0.14f));
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (shadow.TryGetComponent(out Collider collider))
        {
            Object.Destroy(collider);
        }
    }

    private void ConfigureImportedTruckRenderers(GameObject imported)
    {
        Renderer[] renderers = imported.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (ShouldIgnoreImportedTruckRenderer(renderer))
            {
                renderer.enabled = false;
                continue;
            }

            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private static bool ShouldIgnoreImportedTruckRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            return true;
        }

        Transform current = renderer.transform;
        while (current != null)
        {
            string name = current.name.ToLowerInvariant();
            if (name.Contains("camera") || name.Contains("light") || name.Contains("view"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static void DisableImportedTruckRuntimeComponents(GameObject root)
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

    private static void DisableVisualColliders(GameObject root)
    {
        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            Object.Destroy(colliders[i]);
        }
    }

    private void CreateImportedTruckWheelPivots(Transform importedRoot)
    {
        List<Transform> wheelParts = FindImportedVehicleParts(importedRoot, IsImportedTruckWheelPartName);
        for (int i = 0; i < wheelParts.Count; i++)
        {
            Transform pivot = CreateImportedVehicleWheelPivot(wheelParts[i], "ImportedTruckWheelPivot_" + i);
            if (pivot == null)
            {
                continue;
            }

            truckWheels.Add(pivot);
            if (IsImportedTruckFrontWheelPartName(wheelParts[i].name))
            {
                truckFrontWheels.Add(pivot);
            }
        }
    }

    private static Transform CreateImportedVehicleWheelPivot(Transform part, string name)
    {
        if (part == null || part.parent == null || !TryGetWorldRendererBounds(new List<Transform> { part }, out Bounds bounds))
        {
            return null;
        }

        Transform originalParent = part.parent;
        Transform pivot = new GameObject(name).transform;
        pivot.SetParent(originalParent, false);
        pivot.position = bounds.center;
        pivot.localRotation = Quaternion.identity;
        pivot.localScale = Vector3.one;
        part.SetParent(pivot, true);
        return pivot;
    }

    private static List<Transform> FindImportedVehicleParts(Transform root, System.Predicate<string> matchName)
    {
        List<Transform> parts = new();
        if (root == null || matchName == null)
        {
            return parts;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null || current == root || !matchName(current.name))
            {
                continue;
            }

            if (HasImportedVehicleAncestor(current, root, matchName))
            {
                continue;
            }

            if (current.GetComponentsInChildren<Renderer>(true).Length > 0)
            {
                parts.Add(current);
            }
        }

        return parts;
    }

    private static bool HasImportedVehicleAncestor(Transform current, Transform root, System.Predicate<string> matchName)
    {
        Transform parent = current.parent;
        while (parent != null && parent != root)
        {
            if (matchName(parent.name))
            {
                return true;
            }

            parent = parent.parent;
        }

        return false;
    }

    private static bool IsImportedTruckWheelPartName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        string lower = name.ToLowerInvariant();
        return (lower.Contains("wheel") || lower.Contains("tire") || lower.Contains("tyre")) &&
            !lower.Contains("fender");
    }

    private static bool IsImportedTruckFrontWheelPartName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        string lower = name.ToLowerInvariant();
        return lower.Contains("front") || lower.Contains("wheel 1") || lower.Contains("wheel_1");
    }

    private bool FitImportedTruckModel(Transform boundsRoot, GameObject imported, Vector3 targetSize, float bottomY)
    {
        Renderer[] renderers = imported.GetComponentsInChildren<Renderer>(true);
        if (!TryGetTruckLocalRendererBounds(boundsRoot, renderers, out Bounds bounds))
        {
            return false;
        }

        float scaleX = targetSize.x / Mathf.Max(0.001f, bounds.size.x);
        float scaleY = targetSize.y / Mathf.Max(0.001f, bounds.size.y);
        float scaleZ = targetSize.z / Mathf.Max(0.001f, bounds.size.z);
        float scale = Mathf.Clamp(Mathf.Min(scaleX, scaleY, scaleZ), 0.035f, 2.5f);
        imported.transform.localScale = Vector3.one * scale;

        if (!TryGetTruckLocalRendererBounds(boundsRoot, renderers, out Bounds scaledBounds))
        {
            return false;
        }

        imported.transform.localPosition += new Vector3(
            -scaledBounds.center.x,
            bottomY - scaledBounds.min.y,
            -scaledBounds.center.z);

        SessionDebugLogger.Log(
            "TRUCK",
            $"Imported truck bounds raw={FormatImportedTruckSize(bounds.size)} fitted={FormatImportedTruckSize(scaledBounds.size)} target={FormatImportedTruckSize(targetSize)} scale={scale:0.000}.");
        return true;
    }

    private static bool TryGetTruckLocalRendererBounds(Transform root, Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || ShouldIgnoreImportedTruckRenderer(renderer))
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

    private static string FormatImportedTruckSize(Vector3 size)
    {
        return $"{size.x:0.00}x{size.y:0.00}x{size.z:0.00}";
    }
}
