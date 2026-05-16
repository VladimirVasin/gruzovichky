using UnityEngine;

public partial class GameBootstrap
{
    private GameObject CreateBuildingBox(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Color color,
        float smoothness = -1f,
        bool localPosition = false,
        bool castShadows = false)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent, false);
        if (localPosition)
        {
            box.transform.localPosition = position;
        }
        else
        {
            box.transform.position = position;
        }

        box.transform.localScale = scale;
        ApplyColor(box, color, smoothness);
        if (castShadows)
        {
            ConfigureShadowVisual(box, smoothness);
        }
        else
        {
            ConfigureStaticVisual(box, smoothness);
        }

        DisableBuildingCollider(box);
        return box;
    }

    private GameObject CreateBuildingCylinder(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Color color,
        float smoothness = -1f,
        bool localPosition = false)
    {
        GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cylinder.name = name;
        cylinder.transform.SetParent(parent, false);
        if (localPosition)
        {
            cylinder.transform.localPosition = position;
        }
        else
        {
            cylinder.transform.position = position;
        }

        cylinder.transform.localScale = scale;
        ApplyColor(cylinder, color, smoothness);
        ConfigureStaticVisual(cylinder, smoothness);
        DisableBuildingCollider(cylinder);
        return cylinder;
    }

    private GameObject CreateBuildingSphere(
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        Color color,
        float smoothness = -1f,
        bool localPosition = false)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        if (localPosition)
        {
            sphere.transform.localPosition = position;
        }
        else
        {
            sphere.transform.position = position;
        }

        sphere.transform.localScale = scale;
        ApplyColor(sphere, color, smoothness);
        ConfigureStaticVisual(sphere, smoothness);
        DisableBuildingCollider(sphere);
        return sphere;
    }

    private Transform CreateAnchorOrientedBuildingRoot(
        Transform parent,
        string name,
        Vector3 center,
        Vector2Int min,
        Vector2Int max,
        Vector2Int anchor,
        float scale = 1f)
    {
        GameObject root = new(name);
        root.transform.SetParent(parent, false);
        root.transform.position = center;
        root.transform.rotation = Quaternion.LookRotation(GetAnchorFacingDirection(center, min, max, anchor), Vector3.up);
        root.transform.localScale = Vector3.one * scale;
        return root.transform;
    }

    private static Vector2 GetAnchorLocalFootprintSize(Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        float worldWidth = max.x - min.x + 1;
        float worldDepth = max.y - min.y + 1;
        bool anchorOnXSide = anchor.x < min.x || anchor.x > max.x;
        return anchorOnXSide ? new Vector2(worldDepth, worldWidth) : new Vector2(worldWidth, worldDepth);
    }

    private Vector3 GetAnchorFacingDirection(Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Vector3 anchorWorld = new(anchor.x + 0.5f, center.y, anchor.y + 0.5f);
        Vector3 raw = anchorWorld - center;
        raw.y = 0f;
        if (raw.sqrMagnitude < 0.001f)
        {
            raw = new Vector3(0f, 0f, -1f);
        }

        if (Mathf.Abs(raw.x) >= Mathf.Abs(raw.z))
        {
            return new Vector3(Mathf.Sign(raw.x), 0f, 0f);
        }

        return new Vector3(0f, 0f, Mathf.Sign(raw.z));
    }

    private void CreateBuildingWindowRow(
        Transform parent,
        Vector3 first,
        Vector3 step,
        int count,
        Vector3 size,
        Color color,
        bool localPosition = true)
    {
        for (int i = 0; i < count; i++)
        {
            CreateBuildingBox(parent, "Window", first + step * i, size, color, VisualSmoothnessGlass, localPosition);
        }
    }

    private void CreateBuildingCrateStack(Transform parent, Vector3 origin, int count, bool localPosition = false)
    {
        for (int i = 0; i < count; i++)
        {
            int layer = i / 3;
            int column = i % 3;
            Vector3 offset = new((column - 1) * 0.22f, layer * 0.17f, (i % 2) * 0.18f);
            CreateBuildingBox(
                parent,
                "CargoCrate",
                origin + offset,
                new Vector3(0.18f, 0.16f, 0.18f),
                new Color(0.64f, 0.44f, 0.22f),
                VisualSmoothnessWood,
                localPosition);
        }
    }

    private void DisableBuildingCollider(GameObject target)
    {
        if (target != null && target.TryGetComponent(out Collider collider))
        {
            collider.enabled = false;
        }
    }

    private void HideBuildingLightSourceVisuals(Transform root)
    {
        if (root == null)
        {
            return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (IsBuildingLightSourceVisual(renderer.transform))
            {
                renderer.enabled = false;
            }
        }
    }

    private static bool IsBuildingLightSourceVisual(Transform transform)
    {
        while (transform != null)
        {
            if (transform.TryGetComponent(out Light _))
            {
                return true;
            }

            if (IsBuildingLightSourceVisualName(transform.name))
            {
                return true;
            }

            transform = transform.parent;
        }

        return false;
    }

    private static bool IsBuildingLightSourceVisualName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        return name.StartsWith("P_", System.StringComparison.OrdinalIgnoreCase) ||
            name.IndexOf("LightSource", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Light_Source", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("NightLampVisual", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("LampGlow", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("LightGlow", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("GlowSphere", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Lantern_Light", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Lantern_VFX", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Bulb", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("CandleFlame", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Flame", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
