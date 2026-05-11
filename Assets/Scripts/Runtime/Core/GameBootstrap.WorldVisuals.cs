using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private void SetupSurfaceMaterials()
    {
        groundSurfaceTexture = CreateStylizedGroundTexture(128);
        grassSurfaceTexture = CreateStylizedGrassTexture(128);
        footpathSurfaceTexture = CreateStylizedFootpathTexture(128);
        roadSurfaceTexture = CreateStylizedRoadTexture(128);
        groundSurfaceMaterial = CreateSurfaceMaterial(groundSurfaceTexture, new Color(0.95f, 0.91f, 0.84f), 0.1f);
        grassSurfaceMaterial = CreateSurfaceMaterial(grassSurfaceTexture, new Color(0.72f, 0.82f, 0.69f), 0.08f);
        shoreSurfaceMaterial = CreateSurfaceMaterial(groundSurfaceTexture, new Color(0.76f, 0.67f, 0.55f), 0.08f);
        beachSurfaceMaterial = CreateSurfaceMaterial(groundSurfaceTexture, new Color(0.92f, 0.84f, 0.70f), 0.1f);
        roadSurfaceMaterial = CreateSurfaceMaterial(roadSurfaceTexture, new Color(0.21f, 0.22f, 0.24f), VisualSmoothnessAsphalt);
        roadShoulderMaterial = CreateSurfaceMaterial(roadSurfaceTexture, new Color(0.54f, 0.49f, 0.41f), 0.11f);
        highwaySurfaceMaterial = CreateSurfaceMaterial(roadSurfaceTexture, new Color(0.16f, 0.17f, 0.19f), VisualSmoothnessAsphalt);
        highwayShoulderMaterial = CreateSurfaceMaterial(roadSurfaceTexture, new Color(0.44f, 0.46f, 0.49f), 0.14f);
        waterShallowMaterial = CreateSurfaceMaterial(null, new Color(0.48f, 0.82f, 0.92f), 0.96f);
        waterDeepMaterial    = CreateSurfaceMaterial(null, new Color(0.09f, 0.31f, 0.62f), 0.99f);
    }

    private static Shader GetUrpLitShader()
    {
        return ShaderRefs.Lit;
    }

    private Material CreateSurfaceMaterial(Texture2D texture, Color tint, float smoothness)
    {
        Material material = new(GetUrpLitShader());
        material.color = tint;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        material.mainTexture = texture;
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", smoothness);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        return material;
    }

    private Material CreateTransparentOverlayMaterial(Color tint)
    {
        Shader sh = ShaderRefs.Unlit ?? ShaderRefs.Sprites;
        Material mat = new(sh);
        mat.color = tint;
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", tint);
        }

        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);
        }

        if (mat.HasProperty("_Blend"))
        {
            mat.SetFloat("_Blend", 0f);
        }

        if (mat.HasProperty("_SrcBlend"))
        {
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (mat.HasProperty("_DstBlend"))
        {
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (mat.HasProperty("_ZWrite"))
        {
            mat.SetFloat("_ZWrite", 0f);
        }

        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return mat;
    }

    private Texture2D CreateStylizedGroundTexture(int size)
    {
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        texture.name = "StylizedGroundTexture";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color baseColor = new(0.8f, 0.74f, 0.62f);
        Color warmPatch = new(0.9f, 0.82f, 0.67f);
        Color coolPatch = new(0.67f, 0.62f, 0.52f);
        Color dustyPatch = new(0.74f, 0.66f, 0.55f);
        Vector2[] blotchCenters =
        {
            new(0.18f, 0.22f),
            new(0.72f, 0.28f),
            new(0.36f, 0.68f),
            new(0.82f, 0.78f)
        };

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                float largeNoise = Mathf.PerlinNoise(u * 2.4f + 0.11f, v * 2.4f + 0.37f);
                float detailNoise = Mathf.PerlinNoise(u * 7.2f + 1.2f, v * 7.2f + 2.4f);
                float diagonal = Mathf.Sin((u + v) * 8.5f) * 0.015f;
                float dryNoise = Mathf.PerlinNoise(u * 5.1f + 6.4f, v * 4.7f + 8.9f);

                float warmMask = 0f;
                for (int i = 0; i < blotchCenters.Length; i++)
                {
                    float dist = Vector2.Distance(new Vector2(u, v), blotchCenters[i]);
                    warmMask += Mathf.Clamp01(1f - dist * 3.2f);
                }
                warmMask = Mathf.Clamp01(warmMask * 0.42f);

                Color color = Color.Lerp(coolPatch, baseColor, largeNoise);
                color = Color.Lerp(color, warmPatch, warmMask);
                color = Color.Lerp(color, dustyPatch, Mathf.Clamp01((dryNoise - 0.58f) * 0.55f));
                color *= 0.96f + detailNoise * 0.08f + diagonal;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

    private Texture2D CreateStylizedGrassTexture(int size)
    {
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        texture.name = "StylizedGrassTexture";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color darkGreen = new(0.12f, 0.28f, 0.11f);
        Color baseGreen = new(0.22f, 0.44f, 0.2f);
        Color lightGreen = new(0.38f, 0.61f, 0.3f);
        Color mossGreen = new(0.2f, 0.38f, 0.16f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                float broadNoise = Mathf.PerlinNoise(u * 2.5f + 0.5f, v * 2.5f + 0.9f);
                float fineNoise = Mathf.PerlinNoise(u * 9.2f + 2.3f, v * 9.2f + 3.7f);
                float bladeNoise = Mathf.PerlinNoise(u * 17.5f + 4.1f, v * 15.2f + 5.6f);
                float stripe = Mathf.Sin((u * 0.95f + v * 1.2f) * 20f) * 0.055f;
                float mossNoise = Mathf.PerlinNoise(u * 4.1f + 9.3f, v * 4.6f + 10.8f);

                Color color = Color.Lerp(darkGreen, baseGreen, broadNoise);
                color = Color.Lerp(color, lightGreen, Mathf.Clamp01(fineNoise * 0.78f));
                color = Color.Lerp(color, mossGreen, Mathf.Clamp01((mossNoise - 0.55f) * 0.45f));
                color = Color.Lerp(color, lightGreen * 1.05f, Mathf.Clamp01((bladeNoise - 0.52f) * 1.8f));
                color *= 0.96f + stripe;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

    private Texture2D CreateStylizedFootpathTexture(int size)
    {
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        texture.name = "StylizedFootpathTexture";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color packedDust = new(0.48f, 0.39f, 0.25f);
        Color drySoil = new(0.66f, 0.56f, 0.38f);
        Color straw = new(0.81f, 0.73f, 0.49f);
        Color dampSoil = new(0.34f, 0.28f, 0.20f);
        Color edgeGrass = new(0.36f, 0.43f, 0.24f);
        Color pebble = new(0.40f, 0.37f, 0.31f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                float broadNoise = Mathf.PerlinNoise(u * 3.1f + 4.2f, v * 2.7f + 1.4f);
                float frayNoise = Mathf.PerlinNoise(u * 7.4f + 12.2f, v * 5.8f + 3.7f);
                float gritNoise = Mathf.PerlinNoise(u * 24.5f + 8.3f, v * 21.2f + 5.7f);
                float fineNoise = Mathf.PerlinNoise(u * 47.0f + 2.8f, v * 43.0f + 9.1f);
                float centerOffset = (Mathf.PerlinNoise(u * 2.2f + 0.6f, 5.3f) - 0.5f) * 0.16f;
                float lateral = Mathf.Abs(v - (0.5f + centerOffset)) * 2f;
                float centerWear = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.86f, lateral));
                float edgeWear = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.52f, 1f, lateral));

                float leftTrack = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.035f, 0.12f, Mathf.Abs(v - 0.39f - centerOffset * 0.35f)));
                float rightTrack = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.035f, 0.12f, Mathf.Abs(v - 0.61f - centerOffset * 0.35f)));
                float trackWear = Mathf.Max(leftTrack, rightTrack) * Mathf.Clamp01(0.55f + broadNoise * 0.55f);

                float footprint = 0f;
                for (int i = 0; i < 6; i++)
                {
                    float stepU = (i + 0.45f) / 6f;
                    float stepV = ((i & 1) == 0 ? 0.43f : 0.57f) + centerOffset * 0.25f;
                    float du = (u - stepU) / 0.055f;
                    float dv = (v - stepV) / 0.04f;
                    footprint = Mathf.Max(footprint, Mathf.Clamp01(1f - (du * du + dv * dv)));
                }

                float frayedEdge = edgeWear * Mathf.Clamp01((frayNoise - 0.32f) * 1.45f);
                float strawMask = Mathf.Clamp01((broadNoise - 0.56f) * 0.75f + Mathf.Abs(Mathf.Sin((u * 1.8f + v * 0.35f) * 38f)) * 0.05f);
                float pebbleMask = Mathf.Clamp01((gritNoise - 0.70f) * 1.75f);
                float dampMask = Mathf.Clamp01((fineNoise - 0.73f) * 1.4f) * centerWear;

                Color color = Color.Lerp(drySoil, packedDust, centerWear * 0.64f + trackWear * 0.18f);
                color = Color.Lerp(color, dampSoil, dampMask * 0.28f + footprint * 0.18f);
                color = Color.Lerp(color, straw, strawMask * (0.16f + edgeWear * 0.18f));
                color = Color.Lerp(color, edgeGrass, frayedEdge * 0.42f);
                color = Color.Lerp(color, pebble, pebbleMask * 0.24f);
                color *= 0.92f + centerWear * 0.10f + fineNoise * 0.09f;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

    private Texture2D CreateStylizedRoadTexture(int size)
    {
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        texture.name = "StylizedRoadTexture";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;

        Color darkAsphalt = new(0.18f, 0.19f, 0.22f);
        Color baseAsphalt = new(0.24f, 0.25f, 0.28f);
        Color lightAsphalt = new(0.31f, 0.32f, 0.35f);
        Color dustTint = new(0.42f, 0.38f, 0.31f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                float broadNoise = Mathf.PerlinNoise(u * 3.1f + 0.7f, v * 3.1f + 1.4f);
                float detailNoise = Mathf.PerlinNoise(u * 12.4f + 3.6f, v * 12.4f + 5.2f);
                float edgeDust = Mathf.Pow(Mathf.Abs(v - 0.5f) * 2f, 1.6f);

                Color color = Color.Lerp(darkAsphalt, baseAsphalt, broadNoise);
                color = Color.Lerp(color, lightAsphalt, Mathf.Clamp01((detailNoise - 0.42f) * 1.25f));
                color = Color.Lerp(color, dustTint, edgeDust * 0.12f);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return texture;
    }

    private void ApplyStylizedGroundMaterial(GameObject target, int x, int y)
    {
        if (target == null || groundSurfaceMaterial == null)
        {
            ApplyColor(target, new Color(0.72f, 0.67f, 0.55f));
            return;
        }

        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        bool useGrassPatch = IsGrassGroundCell(x, y);
        float tintNoise = Mathf.PerlinNoise((x + 1) * 0.37f, (y + 1) * 0.41f);
        Color tint = useGrassPatch
            ? Color.Lerp(new Color(0.74f, 0.82f, 0.7f), new Color(0.84f, 0.9f, 0.78f), tintNoise)
            : Color.Lerp(new Color(0.95f, 0.91f, 0.84f), new Color(1.01f, 0.98f, 0.92f), tintNoise);
        tint = QuantizeVisualTint(tint, 12f);
        Texture texture = useGrassPatch ? grassSurfaceMaterial.mainTexture : groundSurfaceMaterial.mainTexture;
        Vector2 textureScale = useGrassPatch ? new Vector2(0.54f, 0.54f) : new Vector2(0.62f, 0.62f);
        float smoothness = useGrassPatch ? 0.08f : 0.1f;
        renderer.sharedMaterial = GetCachedLitMaterial(texture, tint, smoothness, textureScale);
    }

    private void ApplyStylizedRoadMaterial(GameObject target, int x, int y, bool isHighway, bool isShoulder)
    {
        if (target == null)
        {
            return;
        }

        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        Material sourceMaterial =
            isHighway
                ? (isShoulder ? highwayShoulderMaterial : highwaySurfaceMaterial)
                : (isShoulder ? roadShoulderMaterial : roadSurfaceMaterial);

        if (sourceMaterial == null)
        {
            ApplyColor(target, isHighway
                ? (isShoulder ? new Color(0.44f, 0.46f, 0.49f) : new Color(0.16f, 0.17f, 0.19f))
                : (isShoulder ? new Color(0.54f, 0.49f, 0.41f) : new Color(0.21f, 0.22f, 0.24f)));
            return;
        }

        float tintNoise = Mathf.PerlinNoise((x + 1) * 0.29f + (isShoulder ? 5.3f : 1.7f), (y + 1) * 0.31f + (isHighway ? 8.1f : 2.9f));
        Color darkTint;
        Color lightTint;

        if (isHighway)
        {
            darkTint = isShoulder ? new Color(0.39f, 0.41f, 0.44f) : new Color(0.14f, 0.15f, 0.17f);
            lightTint = isShoulder ? new Color(0.52f, 0.54f, 0.57f) : new Color(0.2f, 0.21f, 0.24f);
        }
        else
        {
            darkTint = isShoulder ? new Color(0.49f, 0.44f, 0.37f) : new Color(0.18f, 0.19f, 0.21f);
            lightTint = isShoulder ? new Color(0.61f, 0.56f, 0.46f) : new Color(0.25f, 0.26f, 0.29f);
        }

        Color tint = Color.Lerp(darkTint, lightTint, tintNoise);
        tint = QuantizeVisualTint(tint, 10f);
        Vector2 textureScale = isShoulder ? new Vector2(0.52f, 0.9f) : new Vector2(0.7f, 1.15f);
        float smoothness = isShoulder ? 0.12f : VisualSmoothnessAsphalt;
        renderer.sharedMaterial = GetCachedLitMaterial(sourceMaterial.mainTexture, tint, smoothness, textureScale);
    }

    private static Color QuantizeVisualTint(Color color, float steps)
    {
        return new Color(
            Mathf.Round(color.r * steps) / steps,
            Mathf.Round(color.g * steps) / steps,
            Mathf.Round(color.b * steps) / steps,
            color.a);
    }

    private bool IsGrassGroundCell(int x, int y)
    {
        if (grassSurfaceMaterial == null)
        {
            return false;
        }

        float grassPatchNoise = Mathf.PerlinNoise((x + 1) * 0.18f + 4.2f, (y + 1) * 0.2f + 7.4f);
        if (IsDenseForestCell(x, y) || IsNaturalForestZoneCell(x, y))
        {
            return true;
        }

        return grassPatchNoise > 0.5f;
    }

    private bool IsDenseForestCell(int x, int y)
    {
        if (IsNaturalForestZoneCell(x, y))
        {
            return true;
        }

        int shoreRow = GridHeight - WaterRiverWidth;
        float centerX = GridWidth * 0.28f;
        float centerY = shoreRow * 0.34f;

        float dx = (x - centerX) / Mathf.Max(1f, GridWidth * 0.18f);
        float dy = (y - centerY) / Mathf.Max(1f, GridHeight * 0.16f);
        float radialFalloff = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));

        if (radialFalloff <= 0f)
        {
            return false;
        }

        float edgeBreakupNoise = Mathf.PerlinNoise((x + 3) * 0.16f + 18.4f, (y + 5) * 0.17f + 29.1f);
        return radialFalloff > 0.18f && edgeBreakupNoise > 0.24f;
    }

    private float GetDenseForestCellPriority(Vector2Int cell)
    {
        float zoneInfluence = GetForestZoneInfluence(cell.x, cell.y);
        if (!IsDenseForestCell(cell.x, cell.y) && zoneInfluence <= 0f)
        {
            return 0f;
        }

        int shoreRow = GridHeight - WaterRiverWidth;
        float centerX = GridWidth * 0.28f;
        float centerY = shoreRow * 0.34f;
        float dx = (cell.x - centerX) / Mathf.Max(1f, GridWidth * 0.18f);
        float dy = (cell.y - centerY) / Mathf.Max(1f, GridHeight * 0.16f);
        float radialFalloff = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
        float localNoise = Mathf.PerlinNoise((cell.x + 7) * 0.19f + 11.7f, (cell.y + 9) * 0.21f + 23.5f);
        return radialFalloff * 10f + zoneInfluence * 14f + localNoise;
    }

    private void ApplyStylizedGrassMaterial(GameObject target, float seedX, float seedY)
    {
        if (target == null || grassSurfaceMaterial == null)
        {
            ApplyColor(target, new Color(0.24f, 0.34f, 0.16f));
            return;
        }

        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        Material material = new(grassSurfaceMaterial);
        float tintNoise = Mathf.PerlinNoise(seedX * 0.29f + 1.1f, seedY * 0.33f + 2.4f);
        Color tint = Color.Lerp(new Color(0.72f, 0.8f, 0.68f), new Color(0.82f, 0.88f, 0.74f), tintNoise);
        material.color = tint;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        material.mainTextureScale = new Vector2(0.42f, 0.42f);
        material.mainTextureOffset = new Vector2(seedX * 0.03f, seedY * 0.03f);
        renderer.material = material;
    }



}

