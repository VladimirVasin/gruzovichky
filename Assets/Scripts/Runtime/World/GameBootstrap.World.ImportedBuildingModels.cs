using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap
{
    private const string BarImportedModelResourcePath = "Buildings/bar";
    private const string GamblingHallImportedModelResourcePath = "Buildings/casino";
    private const string WarehouseImportedModelResourcePath = "Buildings/warehouse";
    private const string MotelImportedModelResourcePath = "Buildings/motel";
    private const string CityHallImportedModelResourcePath = "Buildings/cityhall";
    private const string LoggingCampImportedModelResourcePath = "Buildings/loggingcamp";
    private const string SawmillImportedModelResourcePath = "Buildings/sawmill";
    private const string CanteenImportedModelResourcePath = "Buildings/canteen";
    private const float BarImportedModelFootprintFill = 2.45f;
    private const float GamblingHallImportedModelFootprintFill = 2.45f;
    private const float WarehouseImportedModelFootprintFill = 3.93f;
    private const float MotelImportedModelFootprintFill = 3.70f;
    private const float CityHallImportedModelFootprintFill = 2.30f;
    private const float LoggingCampImportedModelFootprintFill = 2.25f;
    private const float SawmillImportedModelFootprintFill = 3.10f;
    private const float CanteenImportedModelFootprintFill = 2.35f;
    private const float ImportedBuildingModelGroundY = -0.20f;
    private const float WarehouseImportedModelGroundY = -0.35f;
    private const float MotelImportedModelGroundY = -0.35f;
    private const float CityHallImportedModelGroundY = -0.35f;
    private const float LoggingCampImportedModelGroundY = -0.35f;
    private const float SawmillImportedModelGroundY = -0.35f;
    private const float CanteenImportedModelGroundY = -0.35f;
    private const float WarehouseImportedModelPitch = -90f;
    private const float MotelImportedModelPitch = 0f;
    private const float CityHallImportedModelPitch = 0f;
    private const float LoggingCampImportedModelPitch = 0f;
    private const float SawmillImportedModelPitch = 0f;
    private const float CanteenImportedModelPitch = 0f;

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

    private bool TryCreateImportedWarehouseModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner,
            parent,
            center,
            min,
            max,
            anchor,
            LocationType.Warehouse,
            WarehouseImportedModelResourcePath,
            "WarehouseImportedModelRoot",
            "WarehouseImportedModel",
            WarehouseImportedModelFootprintFill,
            new Color(1f, 0.82f, 0.42f, 1f),
            new Color(1f, 0.74f, 0.28f));
    }

    private bool TryCreateImportedMotelModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner,
            parent,
            center,
            min,
            max,
            anchor,
            LocationType.Motel,
            MotelImportedModelResourcePath,
            "MotelImportedModelRoot",
            "MotelImportedModel",
            MotelImportedModelFootprintFill,
            new Color(1f, 0.88f, 0.56f, 1f),
            new Color(1f, 0.74f, 0.34f));
    }

    private bool TryCreateImportedCityHallModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner,
            parent,
            center,
            min,
            max,
            anchor,
            LocationType.CityHall,
            CityHallImportedModelResourcePath,
            "CityHallImportedModelRoot",
            "CityHallImportedModel",
            CityHallImportedModelFootprintFill,
            new Color(1f, 0.88f, 0.58f, 1f),
            new Color(1f, 0.78f, 0.32f));
    }

    private bool TryCreateImportedLoggingCampModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner,
            parent,
            center,
            min,
            max,
            anchor,
            LocationType.Forest,
            LoggingCampImportedModelResourcePath,
            "LoggingCampImportedModelRoot",
            "LoggingCampImportedModel",
            LoggingCampImportedModelFootprintFill,
            new Color(1f, 0.78f, 0.44f, 1f),
            new Color(1f, 0.70f, 0.28f));
    }

    private bool TryCreateImportedSawmillModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner,
            parent,
            center,
            min,
            max,
            anchor,
            LocationType.Sawmill,
            SawmillImportedModelResourcePath,
            "SawmillImportedModelRoot",
            "SawmillImportedModel",
            SawmillImportedModelFootprintFill,
            new Color(1f, 0.80f, 0.42f, 1f),
            new Color(1f, 0.70f, 0.28f));
    }

    private bool TryCreateImportedCanteenModel(LocationData owner, Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        return TryCreateImportedServiceModel(
            owner,
            parent,
            center,
            min,
            max,
            anchor,
            LocationType.Canteen,
            CanteenImportedModelResourcePath,
            "CanteenImportedModelRoot",
            "CanteenImportedModel",
            CanteenImportedModelFootprintFill,
            new Color(1f, 0.90f, 0.58f, 1f),
            new Color(1f, 0.72f, 0.30f));
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
        model.transform.localRotation = GetImportedBuildingModelLocalRotation(type);
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
            float groundY = GetImportedBuildingModelGroundY(type);
            Bounds groundingBounds = scaledBounds;
            if (TryGetImportedBuildingGroundingBounds(type, root, renderers, out Bounds typeGroundingBounds))
            {
                groundingBounds = typeGroundingBounds;
            }

            model.transform.localPosition = new Vector3(
                -scaledBounds.center.x,
                groundY - groundingBounds.min.y,
                -scaledBounds.center.z);

            LogImportedBuildingScaleAudit(
                type,
                resourcePath,
                footprintFill,
                min,
                max,
                bounds,
                scaledBounds,
                scale,
                targetWidth,
                targetDepth);
        }

        HideImportedBuildingGroundMarkerRenderers(renderers, type);
        ConfigureImportedBuildingModel(model);
        RegisterImportedServiceInteractionMetadata(owner, model.transform);
        RegisterImportedBuildingNightLighting(owner, model.transform, WarmLightSourceColor(windowOnColor), WarmLightSourceColor(markerLightColor));
        return true;
    }

    private static void LogImportedBuildingScaleAudit(
        LocationType type,
        string resourcePath,
        float footprintFill,
        Vector2Int min,
        Vector2Int max,
        Bounds unscaledBounds,
        Bounds scaledBounds,
        float scale,
        float targetWidth,
        float targetDepth)
    {
        int footprintWidth = Mathf.Max(1, max.x - min.x + 1);
        int footprintDepth = Mathf.Max(1, max.y - min.y + 1);
        SessionDebugLogger.Log(
            "BUILD_MODEL_SCALE",
            $"{type} model={resourcePath} footprint={footprintWidth}x{footprintDepth} fill={footprintFill:0.00} " +
            $"target={targetWidth:0.00}x{targetDepth:0.00} scale={scale:0.000} " +
            $"unscaled={FormatImportedBuildingBoundsSize(unscaledBounds.size)} scaled={FormatImportedBuildingBoundsSize(scaledBounds.size)}");
    }

    private static string FormatImportedBuildingBoundsSize(Vector3 size)
    {
        return $"{size.x:0.00}x{size.y:0.00}x{size.z:0.00}";
    }

    private static float GetImportedBuildingModelGroundY(LocationType type)
    {
        return type switch
        {
            LocationType.Warehouse => WarehouseImportedModelGroundY,
            LocationType.Motel     => MotelImportedModelGroundY,
            LocationType.CityHall  => CityHallImportedModelGroundY,
            LocationType.Forest    => LoggingCampImportedModelGroundY,
            LocationType.Sawmill   => SawmillImportedModelGroundY,
            LocationType.Canteen   => CanteenImportedModelGroundY,
            LocationType.GasStation or
            LocationType.FurnitureFactory or
            LocationType.Kiosk or
            LocationType.Kindergarten or
            LocationType.PrimarySchool or
            LocationType.SecondarySchool or
            LocationType.CarMarket or
            LocationType.LaborExchange or
            LocationType.CleaningDepot or
            LocationType.Docks or
            LocationType.CityPark or
            LocationType.PersonalHouse => ImportedTownBuildingModelGroundY,
            _                      => ImportedBuildingModelGroundY
        };
    }

    private static Quaternion GetImportedBuildingModelLocalRotation(LocationType type)
    {
        return type switch
        {
            LocationType.Warehouse => Quaternion.Euler(WarehouseImportedModelPitch, 0f, 0f),
            LocationType.Motel     => Quaternion.Euler(MotelImportedModelPitch, 0f, 0f),
            LocationType.CityHall  => Quaternion.Euler(CityHallImportedModelPitch, 0f, 0f),
            LocationType.Forest    => Quaternion.Euler(LoggingCampImportedModelPitch, 0f, 0f),
            LocationType.Sawmill   => Quaternion.Euler(SawmillImportedModelPitch, 0f, 0f),
            LocationType.Canteen   => Quaternion.Euler(CanteenImportedModelPitch, 0f, 0f),
            _                      => Quaternion.identity
        };
    }

    private static bool TryGetImportedBuildingGroundingBounds(LocationType type, Transform root, Renderer[] renderers, out Bounds bounds)
    {
        if (type == LocationType.Warehouse || type == LocationType.Motel || type == LocationType.CityHall)
        {
            return TryGetLocalRendererBounds(root, renderers, out bounds, IsFoundationImportedGroundingRenderer);
        }

        if (type == LocationType.Forest || type == LocationType.Sawmill || type == LocationType.Canteen || type == LocationType.Parking)
        {
            return TryGetLocalRendererBounds(root, renderers, out bounds, IsLoggingCampImportedGroundingRenderer);
        }

        if (IsNewImportedTownBuildingType(type))
        {
            return TryGetLocalRendererBounds(root, renderers, out bounds, IsGeneralImportedGroundingRenderer);
        }

        bounds = default;
        return false;
    }

    private static bool HasImportedBuildingModel(Transform parent)
    {
        if (parent == null)
        {
            return false;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null &&
                child.name.EndsWith("ImportedModelRoot", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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

    private static void HideImportedBuildingGroundMarkerRenderers(Renderer[] renderers, LocationType type)
    {
        if (!ShouldHideImportedBuildingGroundMarkerVisuals(type))
        {
            return;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer != null && IsLoggingCampImportedGroundingRenderer(renderer))
            {
                renderer.enabled = false;
            }
        }
    }

    private static bool ShouldHideImportedBuildingGroundMarkerVisuals(LocationType type) => type switch
    {
        LocationType.Forest           => true,
        LocationType.Sawmill          => true,
        LocationType.Canteen          => true,
        LocationType.GasStation       => true,
        LocationType.FurnitureFactory => true,
        LocationType.Kiosk            => true,
        LocationType.Kindergarten     => true,
        LocationType.PrimarySchool    => true,
        LocationType.SecondarySchool  => true,
        LocationType.LaborExchange    => true,
        LocationType.CleaningDepot    => true,
        LocationType.PersonalHouse    => true,
        _                             => false
    };

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

        if (name.StartsWith("Base_Grass_Tile", System.StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (type != LocationType.Warehouse &&
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
            if (materialName.IndexOf("Grass_Base", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (type != LocationType.Warehouse &&
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

        if (name.IndexOf("WindowGlow", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
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

    private static bool TryGetLocalRendererBounds(
        Transform root,
        Renderer[] renderers,
        out Bounds bounds,
        System.Predicate<Renderer> includeRenderer = null)
    {
        bounds = default;
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled ||
                (includeRenderer != null && !includeRenderer(renderer)))
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

    private static bool IsFoundationImportedGroundingRenderer(Renderer renderer)
    {
        Transform transform = renderer != null ? renderer.transform : null;
        while (transform != null)
        {
            string name = transform.name;
            if (name.StartsWith("MainBuilding_ConcretePlinth", System.StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("OfficeBlock_ConcreteBase", System.StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("MainBuilding_Foundation", System.StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("OfficeBlock_Foundation", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    private static bool IsLoggingCampImportedGroundingRenderer(Renderer renderer)
    {
        Transform transform = renderer != null ? renderer.transform : null;
        while (transform != null)
        {
            string name = transform.name;
            if (name.StartsWith("GroundBase", System.StringComparison.OrdinalIgnoreCase) ||
                name.StartsWith("GroundPatch", System.StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    private static bool IsGeneralImportedGroundingRenderer(Renderer renderer)
    {
        return IsFoundationImportedGroundingRenderer(renderer) ||
            IsLoggingCampImportedGroundingRenderer(renderer);
    }

    private static bool IsNewImportedTownBuildingType(LocationType type) => type switch
    {
        LocationType.GasStation       => true,
        LocationType.FurnitureFactory => true,
        LocationType.Kiosk            => true,
        LocationType.Kindergarten     => true,
        LocationType.PrimarySchool    => true,
        LocationType.SecondarySchool  => true,
        LocationType.CarMarket        => true,
        LocationType.LaborExchange    => true,
        LocationType.CleaningDepot    => true,
        LocationType.Docks            => true,
        LocationType.CityPark         => true,
        LocationType.PersonalHouse    => true,
        _                             => false
    };

}
