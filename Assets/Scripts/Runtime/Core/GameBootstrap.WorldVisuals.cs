#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private void SetupSurfaceMaterials()
    {
        groundSurfaceTexture = LoadArtTextureAsset("Ground", "ground_dry_grass_lowpoly", CreateStylizedGroundTexture(128));
        grassSurfaceTexture = LoadArtTextureAsset("Ground", "ground_fresh_grass_lowpoly", CreateStylizedGrassTexture(128));
        forestSurfaceTexture = LoadArtTextureAsset("Ground", "ground_forest_floor_lowpoly", grassSurfaceTexture);
        beachSurfaceTexture = LoadArtTextureAsset("Ground", "ground_sandy_riverbank_lowpoly", groundSurfaceTexture);
        footpathSurfaceTexture = LoadArtTextureAsset("Ground", "ground_dirt_footpath_lowpoly", CreateStylizedFootpathTexture(128));
        constructionMudSurfaceTexture = LoadArtTextureAsset("Ground", "ground_muddy_construction_lowpoly", groundSurfaceTexture);
        roadSurfaceTexture = LoadArtTextureAsset("Road", "road_town_asphalt_lowpoly", CreateStylizedRoadTexture(128));
        highwaySurfaceTexture = LoadArtTextureAsset("Road", "road_highway_asphalt_lowpoly", roadSurfaceTexture);
        roadShoulderSurfaceTexture = LoadArtTextureAsset("Road", "road_shoulder_gravel_lowpoly", constructionMudSurfaceTexture);
        riverSurfaceTexture = LoadArtTextureAsset("River", "river_calm_flow_lowpoly", null);
        riverDeepTexture = LoadArtTextureAsset("River", "river_deep_channel_lowpoly", riverSurfaceTexture);
        riverRippleTexture = LoadArtTextureAsset("River", "river_wave_ripples_lowpoly", riverSurfaceTexture);
        riverFoamTexture = LoadArtTextureAsset("River", "river_whitewater_foam_waves_lowpoly", riverRippleTexture);
        lakeSurfaceTexture = LoadArtTextureAsset("Lake", "lake_calm_shallow_lowpoly", riverSurfaceTexture);
        lakeDeepTexture = LoadArtTextureAsset("Lake", "lake_deep_water_lowpoly", lakeSurfaceTexture);
        lakeRippleTexture = LoadArtTextureAsset("Lake", "lake_sunlit_ripples_lowpoly", lakeSurfaceTexture);
        groundSurfaceMaterial = CreateSurfaceMaterial(groundSurfaceTexture, new Color(0.72f, 0.78f, 0.58f), 0.08f);
        grassSurfaceMaterial = CreateSurfaceMaterial(grassSurfaceTexture, new Color(0.68f, 0.82f, 0.56f), 0.07f);
        shoreSurfaceMaterial = CreateSurfaceMaterial(beachSurfaceTexture, new Color(0.64f, 0.64f, 0.48f), 0.07f);
        beachSurfaceMaterial = CreateSurfaceMaterial(beachSurfaceTexture, new Color(0.76f, 0.72f, 0.54f), 0.08f);
        roadSurfaceMaterial = CreateSurfaceMaterial(roadSurfaceTexture, new Color(0.21f, 0.22f, 0.24f), VisualSmoothnessAsphalt);
        roadShoulderMaterial = CreateSurfaceMaterial(roadShoulderSurfaceTexture, new Color(0.66f, 0.58f, 0.46f), 0.11f);
        highwaySurfaceMaterial = CreateSurfaceMaterial(highwaySurfaceTexture, new Color(0.16f, 0.17f, 0.19f), VisualSmoothnessAsphalt);
        highwayShoulderMaterial = CreateSurfaceMaterial(roadShoulderSurfaceTexture, new Color(0.50f, 0.50f, 0.46f), 0.14f);
        waterShallowMaterial = CreateSurfaceMaterial(null, new Color(0.48f, 0.82f, 0.92f), 0.96f);
        waterDeepMaterial    = CreateSurfaceMaterial(null, new Color(0.09f, 0.31f, 0.62f), 0.99f);
    }

    private Texture2D LoadArtTextureAsset(string folder, string fileNameWithoutExtension, Texture2D fallback)
    {
        string resourcePath = $"Art/Textures/{folder}/{fileNameWithoutExtension}";
        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
#if UNITY_EDITOR
        texture ??= AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Resources/{resourcePath}.png");
        texture ??= AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Art/Textures/{folder}/{fileNameWithoutExtension}.png");
#endif

        if (texture == null)
        {
            texture = LoadArtTextureFromFile(Path.Combine(Application.dataPath, "Resources", "Art", "Textures", folder, $"{fileNameWithoutExtension}.png"));
            texture ??= LoadArtTextureFromFile(Path.Combine(Application.dataPath, "Art", "Textures", folder, $"{fileNameWithoutExtension}.png"));
        }

        texture ??= fallback;
        if (texture == null)
        {
            return null;
        }

        texture = PrepareRepeatableArtTexture(texture, folder, fileNameWithoutExtension);
        ConfigureRepeatableArtTexture(texture, fileNameWithoutExtension);
        return texture;
    }

    private Texture2D LoadArtTextureFromFile(string absolutePath)
    {
        if (!File.Exists(absolutePath))
        {
            return null;
        }

        byte[] bytes = File.ReadAllBytes(absolutePath);
        Texture2D loaded = new(2, 2, TextureFormat.RGBA32, false);
        if (loaded.LoadImage(bytes, false))
        {
            return loaded;
        }

        Destroy(loaded);
        return null;
    }

    private static void ConfigureRepeatableArtTexture(Texture2D texture, string textureName)
    {
        if (texture == null)
        {
            return;
        }

        texture.name = textureName;
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        texture.anisoLevel = 2;
    }

    private Texture2D PrepareRepeatableArtTexture(Texture2D texture, string folder, string textureName)
    {
        if (texture == null || !ShouldTrimRepeatTextureBorder(folder))
        {
            return texture;
        }

        Texture2D readable = CreateReadableTextureCopy(texture);
        if (readable == null)
        {
            return texture;
        }

        int border = Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(readable.width, readable.height) * 0.022f), 4, 14);
        if (readable.width <= border * 3 || readable.height <= border * 3)
        {
            return readable;
        }

        Texture2D trimmed = ResampleTextureWithoutOuterBorder(readable, border, $"{textureName}_tile_safe");
        Destroy(readable);
        return trimmed != null ? trimmed : texture;
    }

    private static bool ShouldTrimRepeatTextureBorder(string folder)
    {
        return !string.IsNullOrWhiteSpace(folder);
    }

    private static Texture2D CreateReadableTextureCopy(Texture2D source)
    {
        if (source == null)
        {
            return null;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
        try
        {
            Graphics.Blit(source, temporary);
            RenderTexture.active = temporary;
            Texture2D copy = new(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply(false, false);
            return copy;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
        }
    }

    private static Texture2D ResampleTextureWithoutOuterBorder(Texture2D source, int border, string textureName)
    {
        int width = source.width;
        int height = source.height;
        Texture2D result = new(width, height, TextureFormat.RGBA32, false)
        {
            name = textureName
        };

        float minU = border / (float)width;
        float maxU = 1f - minU;
        float minV = border / (float)height;
        float maxV = 1f - minV;
        for (int y = 0; y < height; y++)
        {
            float v = Mathf.Lerp(minV, maxV, height <= 1 ? 0f : y / (float)(height - 1));
            for (int x = 0; x < width; x++)
            {
                float u = Mathf.Lerp(minU, maxU, width <= 1 ? 0f : x / (float)(width - 1));
                result.SetPixel(x, y, source.GetPixelBilinear(u, v));
            }
        }

        BlendTextureWrapEdges(result, Mathf.Clamp(border, 4, 18));
        result.Apply(false, false);
        return result;
    }

    private static void BlendTextureWrapEdges(Texture2D texture, int blendWidth)
    {
        int width = texture.width;
        int height = texture.height;
        Color[] pixels = texture.GetPixels();
        Color[] original = (Color[])pixels.Clone();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < blendWidth; x++)
            {
                float t = (x + 1f) / (blendWidth + 1f);
                int left = y * width + x;
                int right = y * width + (width - 1 - x);
                Color seamColor = Color.Lerp(original[right], original[left], 0.5f);
                pixels[left] = Color.Lerp(seamColor, original[left], t);
                pixels[right] = Color.Lerp(seamColor, original[right], t);
            }
        }

        for (int y = 0; y < blendWidth; y++)
        {
            float t = (y + 1f) / (blendWidth + 1f);
            int oppositeY = height - 1 - y;
            for (int x = 0; x < width; x++)
            {
                int bottom = y * width + x;
                int top = oppositeY * width + x;
                Color seamColor = Color.Lerp(pixels[top], pixels[bottom], 0.5f);
                pixels[bottom] = Color.Lerp(seamColor, pixels[bottom], t);
                pixels[top] = Color.Lerp(seamColor, pixels[top], t);
            }
        }

        texture.SetPixels(pixels);
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
        return CreateTransparentOverlayMaterial(tint, null, Vector2.one, Vector2.zero);
    }

    private Material CreateTransparentOverlayMaterial(Color tint, Texture2D texture, Vector2 textureScale, Vector2 textureOffset)
    {
        Shader sh = ShaderRefs.Unlit ?? ShaderRefs.Sprites;
        Material mat = new(sh);
        mat.color = tint;
        mat.mainTexture = texture;
        mat.mainTextureScale = textureScale;
        mat.mainTextureOffset = textureOffset;
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", tint);
        }

        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", texture);
            mat.SetTextureScale("_BaseMap", textureScale);
            mat.SetTextureOffset("_BaseMap", textureOffset);
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

        float forestBlend = GetForestGroundBlend01(x, y);
        float shorelineBlend = GetShorelineTransition01(new Vector2Int(x, y));
        bool useForestFloor = forestBlend > 0.68f;
        bool useForestEdge = !useForestFloor && forestBlend > 0.26f;
        bool useGrassPatch = useForestEdge || IsGrassGroundCell(x, y);
        float tintNoise = Mathf.PerlinNoise((x + 1) * 0.18f + 3.7f, (y + 1) * 0.19f + 8.3f);

        Color baseTint = useForestFloor
            ? Color.Lerp(new Color(0.47f, 0.55f, 0.40f), new Color(0.58f, 0.62f, 0.46f), tintNoise)
            : useForestEdge
                ? Color.Lerp(new Color(0.61f, 0.70f, 0.48f), new Color(0.52f, 0.58f, 0.42f), forestBlend)
                : useGrassPatch
                    ? Color.Lerp(new Color(0.66f, 0.78f, 0.52f), new Color(0.74f, 0.82f, 0.60f), tintNoise)
                    : Color.Lerp(new Color(0.68f, 0.74f, 0.54f), new Color(0.76f, 0.78f, 0.60f), tintNoise);

        if (shorelineBlend > 0f)
        {
            Color shoreTransition = Color.Lerp(new Color(0.68f, 0.67f, 0.49f), new Color(0.55f, 0.62f, 0.45f), tintNoise);
            baseTint = Color.Lerp(baseTint, shoreTransition, shorelineBlend * 0.72f);
        }

        Color tint = QuantizeVisualTint(baseTint, 22f);
        Texture texture = useForestFloor
            ? (forestSurfaceTexture != null ? forestSurfaceTexture : grassSurfaceMaterial.mainTexture)
            : useForestEdge
                ? (forestBlend > 0.46f && forestSurfaceTexture != null ? forestSurfaceTexture : groundSurfaceMaterial.mainTexture)
                : useGrassPatch
                    ? grassSurfaceMaterial.mainTexture
                    : groundSurfaceMaterial.mainTexture;
        Vector2 textureScale = useForestFloor
            ? new Vector2(0.26f, 0.26f)
            : useForestEdge
                ? new Vector2(0.28f, 0.28f)
                : new Vector2(0.30f, 0.30f);
        float smoothness = useGrassPatch ? 0.07f : 0.08f;
        renderer.sharedMaterial = GetCachedLitMaterial(texture, tint, smoothness, textureScale, GetGroundMacroTextureOffset(useForestFloor ? 17 : useForestEdge ? 13 : useGrassPatch ? 11 : 5));
    }

    private void ApplyBeachGroundMaterial(GameObject target, int x, int y, bool nearWater)
    {
        if (target == null)
        {
            return;
        }

        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        Material sourceMaterial = nearWater ? beachSurfaceMaterial : shoreSurfaceMaterial;
        if (sourceMaterial == null)
        {
            ApplyColor(target, nearWater ? new Color(0.92f, 0.84f, 0.70f) : new Color(0.76f, 0.67f, 0.55f));
            return;
        }

        float tintNoise = Mathf.PerlinNoise((x + 1) * 0.23f + 2.1f, (y + 1) * 0.27f + 4.4f);
        Color tint = nearWater
            ? Color.Lerp(new Color(0.62f, 0.62f, 0.46f), new Color(0.76f, 0.72f, 0.54f), tintNoise)
            : Color.Lerp(new Color(0.55f, 0.60f, 0.44f), new Color(0.68f, 0.66f, 0.50f), tintNoise);
        tint = QuantizeVisualTint(tint, 22f);
        renderer.sharedMaterial = GetCachedLitMaterial(
            sourceMaterial.mainTexture,
            tint,
            nearWater ? 0.08f : 0.07f,
            new Vector2(0.30f, 0.30f),
            GetGroundMacroTextureOffset(nearWater ? 23 : 29));
    }

    private static Vector2 GetGroundMacroTextureOffset(int seed)
    {
        return new Vector2(
            PositiveMod(seed * 37, 97) / 97f,
            PositiveMod(seed * 53, 89) / 89f);
    }

    private static Vector2 GetGroundTextureOffset(int x, int y, int seed)
    {
        int ox = PositiveMod(x * 37 + y * 17 + seed, 8);
        int oy = PositiveMod(x * 13 + y * 31 + seed * 3, 8);
        return new Vector2(ox * 0.125f, oy * 0.125f);
    }

    private static int PositiveMod(int value, int modulo)
    {
        int result = value % modulo;
        return result < 0 ? result + modulo : result;
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
        int offsetSeed = isHighway
            ? (isShoulder ? 59 : 53)
            : (isShoulder ? 43 : 37);
        renderer.sharedMaterial = GetCachedLitMaterial(sourceMaterial.mainTexture, tint, smoothness, textureScale, GetGroundTextureOffset(x, y, offsetSeed));
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

    private float GetForestGroundBlend01(int x, int y)
    {
        if (IsWaterOrBeachCell(new Vector2Int(x, y)))
        {
            return 0f;
        }

        float zoneInfluence = GetForestZoneInfluence(x, y);
        int shoreRow = GridHeight - WaterRiverWidth;
        float centerX = GridWidth * 0.28f;
        float centerY = shoreRow * 0.34f;
        float dx = (x - centerX) / Mathf.Max(1f, GridWidth * 0.18f);
        float dy = (y - centerY) / Mathf.Max(1f, GridHeight * 0.16f);
        float radialFalloff = 1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
        float breakupNoise = Mathf.PerlinNoise((x + 3) * 0.12f + 18.4f, (y + 5) * 0.13f + 29.1f);
        float rawBlend = Mathf.Max(zoneInfluence * 1.12f, radialFalloff * 0.92f) + (breakupNoise - 0.5f) * 0.18f;
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.10f, 0.72f, rawBlend));
    }

    private float GetShorelineTransition01(Vector2Int cell)
    {
        if (waterCells.Contains(cell) || IsNaturalBeachCell(cell))
        {
            return 0f;
        }

        float best = 0f;
        for (int dx = -2; dx <= 2; dx++)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                Vector2Int neighbor = new(cell.x + dx, cell.y + dy);
                if (!IsInsideGrid(neighbor) || (!waterCells.Contains(neighbor) && !IsNaturalBeachCell(neighbor)))
                {
                    continue;
                }

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                best = Mathf.Max(best, Mathf.Clamp01(1f - (dist - 0.6f) / 2.4f));
            }
        }

        return best;
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

