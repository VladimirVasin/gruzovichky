using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private const float BuildingDecorScale = 1.56f;
    private const float LargePropScale = 1.40f;


    private Vector3 GetCellCenter(Vector2Int cell)
    {
        return new Vector3(cell.x + 0.5f, GetTerrainHeight(cell), cell.y + 0.5f);
    }

    private Vector3 GetTruckWorldPosition(Vector2Int cell)
    {
        return GetCellCenter(cell) + new Vector3(0f, TruckSegmentStartLift, 0f);
    }

    private static Vector2Int WorldToCell(Vector3 point)
    {
        return new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.z));
    }

    private float GetTerrainHeight(Vector2Int cell)
    {
        if (!IsInsideGrid(cell))
        {
            return 0f;
        }

        return terrainHeights[cell.x, cell.y];
    }

    private float GetLocationBaseHeight(LocationType locationType)
    {
        if (!locations.TryGetValue(locationType, out LocationData location))
        {
            return 0f;
        }

        return GetLocationBaseHeight(location);
    }

    private float GetLocationBaseHeight(LocationData location)
    {
        if (location == null)
        {
            return 0f;
        }

        float total = 0f;
        int count = 0;
        for (int x = location.Min.x; x <= location.Max.x; x++)
        {
            for (int y = location.Min.y; y <= location.Max.y; y++)
            {
                total += terrainHeights[x, y];
                count++;
            }
        }

        total += terrainHeights[location.Anchor.x, location.Anchor.y];
        count++;
        return total / Mathf.Max(1, count);
    }

    private float SampleTerrainHeight(float worldX, float worldZ)
    {
        float clampedX = Mathf.Clamp(worldX - 0.5f, 0f, GridWidth - 1.001f);
        float clampedZ = Mathf.Clamp(worldZ - 0.5f, 0f, GridHeight - 1.001f);
        int x0 = Mathf.Clamp(Mathf.FloorToInt(clampedX), 0, GridWidth - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(clampedZ), 0, GridHeight - 1);
        int x1 = Mathf.Min(GridWidth - 1, x0 + 1);
        int z1 = Mathf.Min(GridHeight - 1, z0 + 1);
        float tx = clampedX - x0;
        float tz = clampedZ - z0;
        float h00 = terrainHeights[x0, z0];
        float h10 = terrainHeights[x1, z0];
        float h01 = terrainHeights[x0, z1];
        float h11 = terrainHeights[x1, z1];
        float hx0 = Mathf.Lerp(h00, h10, tx);
        float hx1 = Mathf.Lerp(h01, h11, tx);
        return Mathf.Lerp(hx0, hx1, tz);
    }

    private float GetVerticalEdgeHeight(int x, int y)
    {
        if (x <= 0)
        {
            return terrainHeights[0, Mathf.Clamp(y, 0, GridHeight - 1)];
        }

        if (x >= GridWidth)
        {
            return terrainHeights[GridWidth - 1, Mathf.Clamp(y, 0, GridHeight - 1)];
        }

        int clampedY = Mathf.Clamp(y, 0, GridHeight - 1);
        return (terrainHeights[x - 1, clampedY] + terrainHeights[x, clampedY]) * 0.5f;
    }

    private float GetHorizontalEdgeHeight(int x, int y)
    {
        if (y <= 0)
        {
            return terrainHeights[Mathf.Clamp(x, 0, GridWidth - 1), 0];
        }

        if (y >= GridHeight)
        {
            return terrainHeights[Mathf.Clamp(x, 0, GridWidth - 1), GridHeight - 1];
        }

        int clampedX = Mathf.Clamp(x, 0, GridWidth - 1);
        return (terrainHeights[clampedX, y - 1] + terrainHeights[clampedX, y]) * 0.5f;
    }

    private const float VisualSmoothnessDefault      = 0.14f;
    private const float VisualSmoothnessRubber       = 0.10f;
    private const float VisualSmoothnessWood         = 0.20f;
    private const float VisualSmoothnessFabric       = 0.18f;
    private const float VisualSmoothnessBuildingWall = 0.40f;
    private const float VisualSmoothnessGlass        = 0.92f;
    private const float VisualSmoothnessVehicleMetal = 0.72f;
    private const float VisualSmoothnessRoofMetal    = 0.55f;
    private const float VisualSmoothnessSkin         = 0.58f;
    private const float VisualSmoothnessAsphalt      = 0.12f;

    private readonly struct VisualMaterialCacheKey : System.IEquatable<VisualMaterialCacheKey>
    {
        private readonly int shaderKind;
        private readonly int textureId;
        private readonly int colorR;
        private readonly int colorG;
        private readonly int colorB;
        private readonly int colorA;
        private readonly int smoothness;
        private readonly int scaleX;
        private readonly int scaleY;

        public VisualMaterialCacheKey(int shaderKind, Texture texture, Color color, float smoothness, Vector2 scale)
        {
            this.shaderKind = shaderKind;
            textureId = texture != null ? texture.GetHashCode() : 0;
            colorR = Mathf.RoundToInt(Mathf.Clamp01(color.r) * 255f);
            colorG = Mathf.RoundToInt(Mathf.Clamp01(color.g) * 255f);
            colorB = Mathf.RoundToInt(Mathf.Clamp01(color.b) * 255f);
            colorA = Mathf.RoundToInt(Mathf.Clamp01(color.a) * 255f);
            this.smoothness = Mathf.RoundToInt(Mathf.Clamp01(smoothness) * 1000f);
            scaleX = Mathf.RoundToInt(scale.x * 1000f);
            scaleY = Mathf.RoundToInt(scale.y * 1000f);
        }

        public bool Equals(VisualMaterialCacheKey other)
        {
            return shaderKind == other.shaderKind &&
                   textureId == other.textureId &&
                   colorR == other.colorR &&
                   colorG == other.colorG &&
                   colorB == other.colorB &&
                   colorA == other.colorA &&
                   smoothness == other.smoothness &&
                   scaleX == other.scaleX &&
                   scaleY == other.scaleY;
        }

        public override bool Equals(object obj)
        {
            return obj is VisualMaterialCacheKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = shaderKind;
                hash = (hash * 397) ^ textureId;
                hash = (hash * 397) ^ colorR;
                hash = (hash * 397) ^ colorG;
                hash = (hash * 397) ^ colorB;
                hash = (hash * 397) ^ colorA;
                hash = (hash * 397) ^ smoothness;
                hash = (hash * 397) ^ scaleX;
                hash = (hash * 397) ^ scaleY;
                return hash;
            }
        }
    }

    private static readonly Dictionary<VisualMaterialCacheKey, Material> visualMaterialCache = new();

    private static Material GetCachedLitMaterial(Texture texture, Color color, float smoothness, Vector2 textureScale)
    {
        VisualMaterialCacheKey key = new(0, texture, color, smoothness, textureScale);
        if (visualMaterialCache.TryGetValue(key, out Material cached) && cached != null)
        {
            return cached;
        }

        Material material = new(ShaderRefs.Lit)
        {
            color = color,
            mainTexture = texture,
            mainTextureScale = textureScale
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        visualMaterialCache[key] = material;
        return material;
    }

    private static Material GetCachedUnlitMaterial(Color color)
    {
        Shader shader = ShaderRefs.Unlit ?? ShaderRefs.Sprites;
        VisualMaterialCacheKey key = new(1, null, color, 0f, Vector2.one);
        if (visualMaterialCache.TryGetValue(key, out Material cached) && cached != null)
        {
            return cached;
        }

        Material material = new(shader)
        {
            color = color
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        visualMaterialCache[key] = material;
        return material;
    }

    private static void ClearVisualMaterialCache()
    {
        foreach (Material material in visualMaterialCache.Values)
        {
            if (material == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(material);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }

        visualMaterialCache.Clear();
    }

    private static void ApplyColor(GameObject target, Color color, float smoothness = -1f)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        float resolvedSmoothness = smoothness >= 0f
            ? smoothness
            : GuessVisualSmoothness(target.name, VisualSmoothnessDefault);
        renderer.sharedMaterial = GetCachedLitMaterial(null, color, resolvedSmoothness, Vector2.one);
    }

    private static void ApplyColor(GameObject target, Color color)
    {
        ApplyColor(target, color, -1f);
    }

    private static float GuessVisualSmoothness(string objectName, float fallback)
    {
        string n = objectName.ToLowerInvariant();
        if (n.Contains("wheel")) return VisualSmoothnessRubber;
        if (n.Contains("window") || n.Contains("glass") || n.Contains("windshield")) return VisualSmoothnessGlass;
        if (n.Contains("roof") || n.Contains("awning")) return VisualSmoothnessRoofMetal;
        if (n.Contains("road") || n.Contains("asphalt")) return VisualSmoothnessAsphalt;
        if (n.Contains("log") || n.Contains("board") || n.Contains("bench") || n.Contains("wood")) return VisualSmoothnessWood;
        if (n.Contains("shirt") || n.Contains("cloth") || n.Contains("textile")) return VisualSmoothnessFabric;
        return fallback;
    }

    private static void ApplyUnlitColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.sharedMaterial = GetCachedUnlitMaterial(color);
    }

    private static void ConfigureStaticVisual(GameObject target, float smoothness = -1f)
    {
        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = true;
        ApplyMaterialSmoothness(renderer, smoothness);
    }

    private static void ConfigureStaticVisual(GameObject target)
    {
        ConfigureStaticVisual(target, -1f);
    }

    private void ConfigureShadowVisual(GameObject target, float smoothness = -1f)
    {
        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
        ApplyMaterialSmoothness(renderer, smoothness);
        RegisterShadowLodRenderer(renderer);
    }

    private void ConfigureShadowVisual(GameObject target)
    {
        ConfigureShadowVisual(target, -1f);
    }

    private static void ApplyMaterialSmoothness(Renderer renderer, float smoothness)
    {
        if (smoothness < 0f || renderer.sharedMaterial == null || !renderer.sharedMaterial.HasProperty("_Smoothness"))
        {
            return;
        }

        Material source = renderer.sharedMaterial;
        Color color = source.HasProperty("_BaseColor")
            ? source.GetColor("_BaseColor")
            : source.color;
        Texture texture = source.mainTexture;
        Vector2 textureScale = source.mainTextureScale;
        renderer.sharedMaterial = GetCachedLitMaterial(texture, color, smoothness, textureScale);
    }

    private bool IsInsideGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < GridWidth && cell.y >= 0 && cell.y < GridHeight;
    }
}


