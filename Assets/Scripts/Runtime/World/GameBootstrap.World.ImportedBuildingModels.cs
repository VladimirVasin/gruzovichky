using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap
{
    private const string BarImportedModelResourcePath = "Buildings/bar";
    private const string GamblingHallImportedModelResourcePath = "Buildings/casino";
    private const float BarImportedModelFootprintFill = 2.45f;
    private const float GamblingHallImportedModelFootprintFill = 2.45f;
    private const float ImportedBuildingModelGroundY = -0.20f;

    private bool TryCreateImportedBarModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner,
            parent,
            center,
            min,
            max,
            anchor,
            LocationType.Bar,
            BarImportedModelResourcePath,
            "BarImportedModelRoot",
            "BarImportedModel",
            BarImportedModelFootprintFill,
            new Color(1f, 0.74f, 0.30f, 1f),
            new Color(1f, 0.64f, 0.24f));
    }

    private bool TryCreateImportedGamblingHallModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner,
            parent,
            center,
            min,
            max,
            anchor,
            LocationType.GamblingHall,
            GamblingHallImportedModelResourcePath,
            "GamblingHallImportedModelRoot",
            "GamblingHallImportedModel",
            GamblingHallImportedModelFootprintFill,
            new Color(1f, 0.34f, 0.98f, 1f),
            new Color(1f, 0.48f, 0.95f));
    }

    private bool TryCreateImportedServiceModel(
        LocationData owner,
        Transform parent,
        Vector3 center,
        Vector2Int min,
        Vector2Int max,
        Vector2Int anchor,
        LocationType type,
        string resourcePath,
        string rootName,
        string modelName,
        float footprintFill,
        Color windowOnColor,
        Color markerLightColor)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            return false;
        }

        Transform root = CreateAnchorOrientedBuildingRoot(parent, rootName, center, min, max, anchor);
        GameObject model = Instantiate(prefab, root);
        model.name = modelName;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        HideBuildingLightSourceVisuals(model.transform);
        HideImportedBuildingPlatformRenderers(renderers, type);
        if (!TryGetLocalRendererBounds(root, renderers, out Bounds bounds))
        {
            Destroy(model);
            Destroy(root.gameObject);
            return false;
        }

        float targetWidth = Mathf.Max(0.5f, (max.x - min.x + 1) * footprintFill);
        float targetDepth = Mathf.Max(0.5f, (max.y - min.y + 1) * footprintFill);
        float scale = Mathf.Min(
            targetWidth / Mathf.Max(bounds.size.x, 0.01f),
            targetDepth / Mathf.Max(bounds.size.z, 0.01f));
        model.transform.localScale = Vector3.one * scale;

        if (TryGetLocalRendererBounds(root, renderers, out Bounds scaledBounds))
        {
            model.transform.localPosition = new Vector3(
                -scaledBounds.center.x,
                ImportedBuildingModelGroundY - scaledBounds.min.y,
                -scaledBounds.center.z);
        }

        ConfigureImportedBuildingModel(model);
        RegisterImportedServiceInteractionMetadata(owner, model.transform);
        RegisterImportedBuildingNightLighting(owner, model.transform, windowOnColor, markerLightColor);
        return true;
    }

    private static bool HasImportedBuildingModel(Transform parent)
    {
        return parent != null &&
            (parent.Find("BarImportedModelRoot") != null ||
             parent.Find("GamblingHallImportedModelRoot") != null);
    }

    private static void HideImportedBuildingPlatformRenderers(Renderer[] renderers, LocationType type)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (IsImportedBuildingPlatformRenderer(renderer, type))
            {
                renderer.enabled = false;
            }
        }
    }

    private static bool IsImportedBuildingPlatformRenderer(Renderer renderer, LocationType type)
    {
        if (renderer == null)
        {
            return false;
        }

        if (IsImportedBuildingPlatformName(renderer.name, type) || HasImportedBuildingPlatformMaterial(renderer, type))
        {
            return true;
        }

        Transform transform = renderer.transform;
        while (transform != null)
        {
            if (IsImportedBuildingPlatformName(transform.name, type))
            {
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    private static bool IsImportedBuildingPlatformName(string name, LocationType type)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (name.StartsWith("Base_Grass_Tile", System.StringComparison.OrdinalIgnoreCase) ||
            name.IndexOf("Platform", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return type == LocationType.Bar &&
            (name.StartsWith("Roof", System.StringComparison.OrdinalIgnoreCase) ||
             name.IndexOf("Awning", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool HasImportedBuildingPlatformMaterial(Renderer renderer, LocationType type)
    {
        Material[] materials = renderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
            {
                continue;
            }

            string materialName = material.name;
            if (materialName.IndexOf("Grass_Base", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                materialName.IndexOf("Platform", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (type == LocationType.Bar &&
                materialName.IndexOf("Roof", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateBarEntranceLight(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "BarLightRoot", center, min, max, anchor);
        GameObject lightObj = new("BarLight");
        lightObj.transform.SetParent(root, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.9f * BuildingDecorScale, 0.9f * BuildingDecorScale);
        Light barLight = lightObj.AddComponent<Light>();
        barLight.type = LightType.Point;
        ServiceDecorationLightStyle barLightStyle = ServiceDecorationStyleService.GetLightStyle(ServiceDecorationKind.Bar);
        barLight.color = barLightStyle.Color;
        barLight.intensity = barLightStyle.Intensity;
        barLight.range = barLightStyle.Range;
        barLight.shadows = LightShadows.None;
    }

    private void CreateGamblingHallEntranceLight(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "GamblingHallLightRoot", center, min, max, anchor);
        GameObject lightObj = new("GamblingHallLight");
        lightObj.transform.SetParent(root, false);
        lightObj.transform.localPosition = new Vector3(0f, 1.08f * BuildingDecorScale, 0.86f * BuildingDecorScale);
        Light gamblingLight = lightObj.AddComponent<Light>();
        gamblingLight.type = LightType.Point;
        ServiceDecorationLightStyle gamblingLightStyle = ServiceDecorationStyleService.GetLightStyle(ServiceDecorationKind.GamblingHall);
        gamblingLight.color = gamblingLightStyle.Color;
        gamblingLight.intensity = gamblingLightStyle.Intensity;
        gamblingLight.range = gamblingLightStyle.Range;
        gamblingLight.shadows = LightShadows.None;
    }

    private void RegisterImportedBuildingNightLighting(LocationData owner, Transform modelRoot, Color windowOnColor, Color markerLightColor)
    {
        if (modelRoot == null)
        {
            return;
        }

        bool hasExplicitWindowGlowMarkers = HasImportedWindowGlowMarkers(modelRoot);
        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled || !IsImportedWindowGlassRenderer(renderer))
            {
                continue;
            }

            Color offColor = GetImportedMaterialColor(renderer, new Color(0.46f, 0.66f, 0.72f, 1f));
            Color onColor = Color.Lerp(new Color(offColor.r, offColor.g, offColor.b, Mathf.Max(0.86f, offColor.a)), windowOnColor, 0.82f);
            RegisterImportedNightLightMaterial(owner, renderer, offColor, onColor);
            if (!hasExplicitWindowGlowMarkers)
            {
                CreateImportedNightPointLight(owner, modelRoot, renderer.bounds.center, renderer.name + "_WindowLight", windowOnColor, 0.44f, 2.45f);
            }
        }

        Transform[] transforms = modelRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform marker = transforms[i];
            if (!IsImportedNightLightMarker(marker))
            {
                continue;
            }

            bool windowMarker = IsImportedWindowGlowMarker(marker.name);
            CreateImportedNightPointLight(
                owner,
                modelRoot,
                marker.position,
                marker.name + "_NightLight",
                windowMarker ? windowOnColor : markerLightColor,
                windowMarker ? 0.62f : 0.92f,
                windowMarker ? 2.8f : 3.2f);
        }
    }

    private void RegisterImportedNightLightMaterial(LocationData owner, Renderer renderer, Color offColor, Color onColor)
    {
        Material material = renderer.material;
        SetLocationNightLightMaterialColor(material, offColor);
        locationNightLightRenderers.Add(renderer);
        locationNightLightMaterials.Add(material);
        locationNightLightOffColors.Add(offColor);
        locationNightLightOnColors.Add(onColor);
        locationNightLightMaxIntensities.Add(0f);
        locationNightLightRanges.Add(0f);
        locationNightLightMaterialOwnerInstanceIds.Add(owner?.InstanceId ?? 0);
    }

    private void CreateImportedNightPointLight(
        LocationData owner,
        Transform parent,
        Vector3 worldPosition,
        string lightName,
        Color onColor,
        float maxIntensity,
        float range)
    {
        if (owner != null)
        {
            range = ExpandLocationNightLightRange(owner.Type, range);
        }

        GameObject lightObject = new(lightName);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = worldPosition;

        Light lightComponent = lightObject.AddComponent<Light>();
        lightComponent.type = LightType.Point;
        lightComponent.color = onColor;
        lightComponent.range = range;
        lightComponent.intensity = 0f;
        lightComponent.shadows = LightShadows.None;
        lightComponent.enabled = false;

        locationNightLights.Add(lightComponent);
        locationNightLightOwnerInstanceIds.Add(owner?.InstanceId ?? 0);
        locationNightPointLightOnColors.Add(onColor);
        locationNightPointLightMaxIntensities.Add(maxIntensity);
        locationNightPointLightRanges.Add(range);
        MarkCellLightingDirty();
    }

    private static bool IsImportedWindowGlassRenderer(Renderer renderer)
    {
        if (renderer == null)
        {
            return false;
        }

        if (IsImportedWindowGlassName(renderer.name) || HasImportedWindowGlassInHierarchy(renderer.transform))
        {
            return true;
        }

        Material[] materials = renderer.sharedMaterials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
            {
                continue;
            }

            if (material.name.IndexOf("Glass", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                HasImportedWindowInHierarchy(renderer.transform))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasImportedWindowGlassInHierarchy(Transform transform)
    {
        while (transform != null)
        {
            if (IsImportedWindowGlassName(transform.name))
            {
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    private static bool HasImportedWindowInHierarchy(Transform transform)
    {
        while (transform != null)
        {
            if (!string.IsNullOrEmpty(transform.name) &&
                transform.name.IndexOf("Window", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    private static bool IsImportedWindowGlassName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return name.IndexOf("Window", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
            (name.IndexOf("Glass", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
             name.IndexOf("Pane", System.StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool IsImportedNightLightMarker(Transform transform)
    {
        if (transform == null || string.IsNullOrEmpty(transform.name) ||
            !transform.name.StartsWith("P_", System.StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return transform.name.IndexOf("Light", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            transform.name.IndexOf("Neon", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            IsImportedWindowGlowMarker(transform.name);
    }

    private static bool HasImportedWindowGlowMarkers(Transform root)
    {
        if (root == null)
        {
            return false;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null && IsImportedWindowGlowMarker(transforms[i].name))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsImportedWindowGlowMarker(string name)
    {
        return !string.IsNullOrEmpty(name) &&
            name.IndexOf("WindowGlow", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Color GetImportedMaterialColor(Renderer renderer, Color fallback)
    {
        Material material = renderer != null ? renderer.sharedMaterial : null;
        if (material == null)
        {
            return fallback;
        }

        if (material.HasProperty("_BaseColor"))
        {
            return material.GetColor("_BaseColor");
        }

        if (material.HasProperty("_Color"))
        {
            return material.GetColor("_Color");
        }

        return fallback;
    }

    private static bool TryGetLocalRendererBounds(Transform root, Renderer[] renderers, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
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

    private void ConfigureImportedBuildingModel(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            ApplyMaterialSmoothness(renderer, GuessVisualSmoothness(renderer.name, VisualSmoothnessBuildingWall));
        }

        Collider[] colliders = model.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }
}
