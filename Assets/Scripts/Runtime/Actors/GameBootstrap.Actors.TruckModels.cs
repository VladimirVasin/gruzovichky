using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap
{
    private const string ImportedTruckResourcePath = "Vehicles/Trucks/Truck";
    private static readonly Vector3 ImportedTruckTargetSize = new(1.59f, 1.27f, 2.12f);
    private const float ImportedTruckBottomY = 0.02f;
    private static readonly Quaternion TruckProceduralWheelBaseRotation = Quaternion.Euler(0f, 0f, 90f);
    private GameObject importedTruckPrefab;
    private bool hasLoadedImportedTruckPrefab;

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
        GameObject prefab = GetImportedTruckPrefab();
        if (prefab == null)
        {
            return false;
        }

        Transform bodyAnchor = CreateTruckVisualAnchor("TruckBodyImported", Vector3.zero);
        truckBodyTransform = bodyAnchor;
        truckCabinTransform = null;

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
            truckBodyTransform = null;
            truckCabinTransform = null;
            SessionDebugLogger.Log("TRUCK", $"Imported truck model from Resources/{ImportedTruckResourcePath} has no usable renderers; using procedural fallback.");
            return false;
        }

        CreateImportedTruckWheelPivots(imported.transform);
        CreateImportedTruckMotionPivots(imported.transform, bodyAnchor);
        CreateTruckShadowBlob();
        CreateTruckHeadlightVisual(new Vector3(-0.34f, 0.68f, 0.84f), true);
        CreateTruckHeadlightVisual(new Vector3(0.34f, 0.68f, 0.84f), false);
        CreateTruckHeadlightBeam(new Vector3(-0.34f, 0.68f, 0.91f));
        CreateTruckHeadlightBeam(new Vector3(0.34f, 0.68f, 0.91f));

        cargoVisualRoot = CreateTruckCargoVisualRoot(new Vector3(0f, 0.82f, -0.44f));
        SessionDebugLogger.Log("TRUCK", $"Instantiated imported truck model from Resources/{ImportedTruckResourcePath}.");
        return true;
    }

    private void PreloadImportedTruckPrefab()
    {
        _ = GetImportedTruckPrefab();
    }

    private GameObject GetImportedTruckPrefab()
    {
        if (hasLoadedImportedTruckPrefab)
        {
            return importedTruckPrefab;
        }

        importedTruckPrefab = Resources.Load<GameObject>(ImportedTruckResourcePath);
        hasLoadedImportedTruckPrefab = true;
        if (importedTruckPrefab != null)
        {
            SessionDebugLogger.Log("TRUCK", $"Preloaded imported truck model from Resources/{ImportedTruckResourcePath}.");
        }

        return importedTruckPrefab;
    }

    private Transform BuildProceduralTruckVisualModel()
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(truckVisualRoot, false);
        body.transform.localPosition = new Vector3(0f, 0.29f, 0f);
        body.transform.localScale = new Vector3(0.81f, 0.40f, 1.15f);
        ApplyColor(body, new Color(0.85f, 0.2f, 0.18f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(body, VisualSmoothnessVehicleMetal);
        truckBodyTransform = body.transform;

        GameObject bodyStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bodyStripe.transform.SetParent(truckVisualRoot, false);
        bodyStripe.transform.localPosition = new Vector3(0f, 0.32f, 0f);
        bodyStripe.transform.localScale = new Vector3(0.85f, 0.06f, 1.17f);
        ApplyColor(bodyStripe, new Color(0.96f, 0.9f, 0.72f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(bodyStripe, VisualSmoothnessVehicleMetal);
        bodyStripe.transform.SetParent(body.transform, true);

        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(truckVisualRoot, false);
        cabin.transform.localPosition = new Vector3(0f, 0.46f, 0.23f);
        cabin.transform.localScale = new Vector3(0.63f, 0.46f, 0.52f);
        ApplyColor(cabin, new Color(0.95f, 0.82f, 0.28f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(cabin, VisualSmoothnessVehicleMetal);
        truckCabinTransform = cabin.transform;

        GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windshield.transform.SetParent(truckVisualRoot, false);
        windshield.transform.localPosition = new Vector3(0f, 0.49f, 0.48f);
        windshield.transform.localScale = new Vector3(0.48f, 0.21f, 0.05f);
        ApplyColor(windshield, new Color(0.68f, 0.86f, 0.94f), VisualSmoothnessGlass);
        ConfigureShadowVisual(windshield, VisualSmoothnessGlass);
        windshield.transform.SetParent(cabin.transform, true);

        CreateTruckShadowBlob();
        CreateTruckHeadlightVisual(new Vector3(-0.21f, 0.45f, 0.53f), true);
        CreateTruckHeadlightVisual(new Vector3(0.21f, 0.45f, 0.53f), false);
        CreateTruckHeadlightBeam(new Vector3(-0.21f, 0.45f, 0.58f));
        CreateTruckHeadlightBeam(new Vector3(0.21f, 0.45f, 0.58f));

        Vector3[] wheelOffsets =
        {
            new(-0.32f, 0.09f, 0.37f),
            new(0.32f, 0.09f, 0.37f),
            new(-0.32f, 0.09f, -0.37f),
            new(0.32f, 0.09f, -0.37f)
        };

        for (int i = 0; i < wheelOffsets.Length; i++)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.transform.SetParent(truckVisualRoot, false);
            wheel.transform.localPosition = wheelOffsets[i];
            wheel.transform.localRotation = TruckProceduralWheelBaseRotation;
            wheel.transform.localScale = new Vector3(0.14f, 0.06f, 0.14f);
            ApplyColor(wheel, new Color(0.14f, 0.14f, 0.14f), VisualSmoothnessRubber);
            ConfigureShadowVisual(wheel, VisualSmoothnessRubber);
            ConfigureVehicleWheelSpin(wheel.transform, Vector3.up);
            truckWheels.Add(wheel.transform);
            if (i < 2)
            {
                truckFrontWheels.Add(wheel.transform);
            }
        }

        return CreateTruckCargoVisualRoot(new Vector3(0f, 0.54f, -0.28f));
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
        shadow.transform.localScale = new Vector3(1.59f, 0.01f, 2.12f);
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

    private void CreateImportedTruckMotionPivots(Transform importedRoot, Transform bodyAnchor)
    {
        Transform cargoPivot = CreateImportedTruckMotionPivot(
            importedRoot,
            IsImportedTruckCargoMotionPartName,
            "ImportedTruckCargoMotionPivot");
        if (cargoPivot != null)
        {
            truckBodyTransform = cargoPivot;
        }
        else
        {
            truckBodyTransform = bodyAnchor;
        }

        truckCabinTransform = CreateImportedTruckMotionPivot(
            importedRoot,
            IsImportedTruckCabinMotionPartName,
            "ImportedTruckCabinMotionPivot");
    }

    private Transform CreateImportedTruckMotionPivot(
        Transform importedRoot,
        System.Predicate<string> matchName,
        string pivotName)
    {
        List<Transform> parts = FindImportedVehicleParts(importedRoot, matchName);
        if (parts.Count == 0 || !TryGetWorldRendererBounds(parts, out Bounds bounds))
        {
            return null;
        }

        Transform pivot = new GameObject(pivotName).transform;
        pivot.SetParent(importedRoot, false);
        pivot.position = bounds.center;
        pivot.localRotation = Quaternion.identity;
        pivot.localScale = Vector3.one;
        for (int i = 0; i < parts.Count; i++)
        {
            parts[i].SetParent(pivot, true);
        }

        return pivot;
    }

    private Transform CreateImportedVehicleWheelPivot(Transform part, string name)
    {
        if (part == null || part.parent == null || !TryGetWorldRendererBounds(new List<Transform> { part }, out Bounds bounds))
        {
            return null;
        }

        Vector3 worldSpinAxis = GetImportedVehicleWheelWorldSpinAxis(bounds);
        Transform originalParent = part.parent;
        Transform pivot = new GameObject(name).transform;
        pivot.SetParent(originalParent, false);
        pivot.position = bounds.center;
        pivot.localRotation = Quaternion.identity;
        pivot.localScale = Vector3.one;
        ConfigureVehicleWheelSpin(pivot, pivot.InverseTransformDirection(worldSpinAxis));
        part.SetParent(pivot, true);
        return pivot;
    }

    private void ConfigureVehicleWheelSpin(Transform wheel, Vector3 localSpinAxis)
    {
        if (wheel == null)
        {
            return;
        }

        vehicleWheelBaseLocalRotations[wheel] = wheel.localRotation;
        vehicleWheelLocalSpinAxes[wheel] = localSpinAxis.sqrMagnitude > 0.0001f
            ? localSpinAxis.normalized
            : Vector3.up;
    }

    private void ApplyVehicleWheelSpin(Transform wheel, float spin, float steer = 0f)
    {
        if (wheel == null)
        {
            return;
        }

        if (!vehicleWheelBaseLocalRotations.TryGetValue(wheel, out Quaternion baseRotation))
        {
            baseRotation = wheel.localRotation;
            vehicleWheelBaseLocalRotations[wheel] = baseRotation;
        }

        if (!vehicleWheelLocalSpinAxes.TryGetValue(wheel, out Vector3 localSpinAxis) ||
            localSpinAxis.sqrMagnitude <= 0.0001f)
        {
            localSpinAxis = Vector3.up;
            vehicleWheelLocalSpinAxes[wheel] = localSpinAxis;
        }

        Quaternion steerRotation = Mathf.Abs(steer) > 0.001f
            ? Quaternion.Euler(0f, steer, 0f)
            : Quaternion.identity;
        wheel.localRotation = steerRotation * baseRotation * Quaternion.AngleAxis(spin, localSpinAxis.normalized);
    }

    private static Vector3 GetImportedVehicleWheelWorldSpinAxis(Bounds bounds)
    {
        Vector3 size = bounds.size;
        if (size.x <= size.y && size.x <= size.z)
        {
            return Vector3.right;
        }

        if (size.y <= size.x && size.y <= size.z)
        {
            return Vector3.up;
        }

        return Vector3.forward;
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

    private static bool IsImportedTruckCabinMotionPartName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        string lower = name.ToLowerInvariant();
        return lower.Contains("cabin") ||
            lower.Contains("hood") ||
            lower.Contains("windshield") ||
            lower.Contains("side window") ||
            lower.Contains("door") ||
            lower.Contains("grille") ||
            lower.Contains("headlight") ||
            lower.Contains("front bumper") ||
            lower.Contains("front license") ||
            lower.Contains("front fender");
    }

    private static bool IsImportedTruckCargoMotionPartName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        string lower = name.ToLowerInvariant();
        return lower.Contains("cargo") ||
            lower.Contains("tailgate") ||
            lower.Contains("rear bumper") ||
            lower.Contains("tail light") ||
            lower.Contains("rear license");
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
