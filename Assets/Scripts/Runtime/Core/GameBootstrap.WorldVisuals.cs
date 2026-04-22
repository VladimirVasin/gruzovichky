using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private System.Collections.IEnumerator SetupGroundAsync()
    {
        groundRoot = new GameObject("Ground").transform;
        groundRoot.SetParent(worldRoot, false);

        int shoreRow = GridHeight - WaterRiverWidth;
        int beachNearRow = shoreRow - 1;
        int beachFarRow = shoreRow - 2;
        const float waterCellHeight = 0.22f;
        const float waterBaseTopHeight = 0.04f;
        const float waterSurfaceTop = 0.22f;
        const float waterSurfaceThickness = 0.035f;
        // Opaque water body sits low; transparent surface stack floats above it.
        float waterSurfaceCenterY    = waterCellHeight - 0.006f;  // 0.214 — top layer
        float waterMidSurfaceCenterY = waterCellHeight - 0.012f;  // 0.208 — mid layer
        float waterLowSurfaceCenterY = waterCellHeight - 0.018f;  // 0.202 — low layer
        waterSurfaceTiles.Clear();
        waterBodyTiles.Clear();

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                bool isWater = waterCells.Contains(new Vector2Int(x, y));
                bool isNearBeach = y == beachNearRow;
                bool isFarBeach = y == beachFarRow;
                bool isBeach = isNearBeach || isFarBeach;

                float terrainHeight = isWater ? waterBaseTopHeight : terrainHeights[x, y];
                float thickness = isWater ? 0.18f : 0.28f + terrainHeight;

                GameObject groundTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                groundTile.name = isWater ? $"Water_{x}_{y}" : $"Ground_{x}_{y}";
                groundTile.transform.SetParent(groundRoot, false);
                groundTile.transform.position = new Vector3(x + 0.5f, terrainHeight - thickness * 0.5f - 0.02f, y + 0.5f);
                groundTile.transform.localScale = new Vector3(1.02f, thickness, 1.02f);

                if (isWater)
                {
                    // t=0 → shore (shallow), t=1 → deepest row
                    float t = (float)(y - shoreRow) / Mathf.Max(WaterRiverWidth - 1, 1);

                    // Solid tile: per-tile depth colour (instance material)
                    Color baseWaterColor = Color.Lerp(
                        new Color(0.38f, 0.74f, 0.88f),  // shallow: bright cyan
                        new Color(0.08f, 0.26f, 0.58f),  // deep: dark navy
                        t);
                    Renderer r = groundTile.GetComponent<Renderer>();
                    r.material = CreateSurfaceMaterial(null, baseWaterColor, 0.92f);
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = true;
                    if (groundTile.TryGetComponent(out Collider wc)) Object.Destroy(wc);
                    float wavePhase = Random.Range(0f, 10f);
                    waterBodyTiles.Add(new WaterBodyTileData
                    {
                        Transform = groundTile.transform,
                        BaseY = groundTile.transform.position.y,
                        BaseTopY = groundTile.transform.position.y + thickness * 0.5f,
                        Cell = new Vector2Int(x, y),
                        PhaseOffset = wavePhase
                    });

                    // Top shimmer layer — wide, light, animated bob
                    Color topColor = Color.Lerp(
                        new Color(0.75f, 0.94f, 1.00f, 0.18f),
                        new Color(0.18f, 0.50f, 0.85f, 0.11f), t);
                    GameObject waterSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    waterSurface.name = $"WaterSurface_{x}_{y}";
                    waterSurface.transform.SetParent(groundRoot, false);
                    waterSurface.transform.position = new Vector3(x + 0.5f, waterSurfaceCenterY, y + 0.5f);
                    waterSurface.transform.localScale = new Vector3(1.01f, waterSurfaceThickness, 1.01f);
                    Renderer surfaceRenderer = waterSurface.GetComponent<Renderer>();
                    surfaceRenderer.material = CreateTransparentOverlayMaterial(topColor);
                    surfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    surfaceRenderer.receiveShadows = false;
                    if (waterSurface.TryGetComponent(out Collider waterSurfaceCollider)) Object.Destroy(waterSurfaceCollider);

                    waterSurfaceTiles.Add(new WaterSurfaceTileData
                    {
                        Renderer = surfaceRenderer,
                        Material = surfaceRenderer.material,
                        Transform = waterSurface.transform,
                        BaseY = waterSurfaceCenterY,
                        Cell = new Vector2Int(x, y),
                        BobAmplitude = y == shoreRow ? 0.028f : 0.04f,
                        BobSpeed = y == shoreRow ? 0.78f : 1.02f,
                        PhaseOffset = wavePhase,
                        LayerIndex = 0
                    });

                    // Mid layer — slightly smaller, more saturated
                    Color midColor = Color.Lerp(
                        new Color(0.42f, 0.78f, 0.92f, 0.16f),
                        new Color(0.10f, 0.36f, 0.72f, 0.11f), t);
                    GameObject waterMidSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    waterMidSurface.name = $"WaterMidSurface_{x}_{y}";
                    waterMidSurface.transform.SetParent(groundRoot, false);
                    waterMidSurface.transform.position = new Vector3(x + 0.5f, waterMidSurfaceCenterY, y + 0.5f);
                    waterMidSurface.transform.localScale = new Vector3(0.88f, 0.03f, 0.88f);
                    Renderer midSurfaceRenderer = waterMidSurface.GetComponent<Renderer>();
                    midSurfaceRenderer.material = CreateTransparentOverlayMaterial(midColor);
                    midSurfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    midSurfaceRenderer.receiveShadows = false;
                    if (waterMidSurface.TryGetComponent(out Collider waterMidSurfaceCollider)) Object.Destroy(waterMidSurfaceCollider);
                    waterSurfaceTiles.Add(new WaterSurfaceTileData
                    {
                        Renderer = midSurfaceRenderer,
                        Material = midSurfaceRenderer.material,
                        Transform = waterMidSurface.transform,
                        BaseY = waterMidSurfaceCenterY,
                        Cell = new Vector2Int(x, y),
                        BobAmplitude = y == shoreRow ? 0.022f : 0.03f,
                        BobSpeed = y == shoreRow ? 0.74f : 0.96f,
                        PhaseOffset = wavePhase,
                        LayerIndex = 1
                    });

                    // Low layer — noticeably smaller, darkest, acts as "depth shadow"
                    Color lowColor = Color.Lerp(
                        new Color(0.22f, 0.58f, 0.78f, 0.30f),
                        new Color(0.04f, 0.18f, 0.52f, 0.22f), t);
                    GameObject waterLowSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    waterLowSurface.name = $"WaterLowSurface_{x}_{y}";
                    waterLowSurface.transform.SetParent(groundRoot, false);
                    waterLowSurface.transform.position = new Vector3(x + 0.5f, waterLowSurfaceCenterY, y + 0.5f);
                    waterLowSurface.transform.localScale = new Vector3(0.72f, 0.028f, 0.72f);
                    Renderer lowSurfaceRenderer = waterLowSurface.GetComponent<Renderer>();
                    lowSurfaceRenderer.material = CreateTransparentOverlayMaterial(lowColor);
                    lowSurfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    lowSurfaceRenderer.receiveShadows = false;
                    if (waterLowSurface.TryGetComponent(out Collider waterLowSurfaceCollider)) Object.Destroy(waterLowSurfaceCollider);
                    waterSurfaceTiles.Add(new WaterSurfaceTileData
                    {
                        Renderer = lowSurfaceRenderer,
                        Material = lowSurfaceRenderer.material,
                        Transform = waterLowSurface.transform,
                        BaseY = waterLowSurfaceCenterY,
                        Cell = new Vector2Int(x, y),
                        BobAmplitude = y == shoreRow ? 0.016f : 0.022f,
                        BobSpeed = y == shoreRow ? 0.7f : 0.9f,
                        PhaseOffset = wavePhase,
                        LayerIndex = 2
                    });

                    if (y == shoreRow)
                    {
                        GameObject foamStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        foamStrip.name = $"WaterFoam_{x}_{y}";
                        foamStrip.transform.SetParent(groundRoot, false);
                        foamStrip.transform.position = new Vector3(x + 0.5f, waterSurfaceTop + 0.006f, y + 0.12f);
                        foamStrip.transform.localScale = new Vector3(0.94f, 0.012f, 0.12f);
                        Renderer foamRenderer = foamStrip.GetComponent<Renderer>();
                        foamRenderer.sharedMaterial = waterShallowMaterial;
                        foamRenderer.material.color = new Color(0.9f, 0.96f, 1f);
                        foamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        foamRenderer.receiveShadows = false;
                        if (foamStrip.TryGetComponent(out Collider foamCollider)) Object.Destroy(foamCollider);
                    }
                }
                else if (isNearBeach)
                {
                    Renderer r = groundTile.GetComponent<Renderer>();
                    r.sharedMaterial = beachSurfaceMaterial;
                    ConfigureStaticVisual(groundTile);
                }
                else if (isFarBeach)
                {
                    Renderer r = groundTile.GetComponent<Renderer>();
                    r.sharedMaterial = shoreSurfaceMaterial;
                    ConfigureStaticVisual(groundTile);
                }
                else
                {
                    ApplyStylizedGroundMaterial(groundTile, x, y);
                    ConfigureStaticVisual(groundTile);
                }
            }

            if (x % 8 == 7) yield return null;
        }

        CreateDioramaBase();
    }

    private void PopulateWaterCells()
    {
        waterCells.Clear();
        int topStart = GridHeight - WaterRiverWidth;
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = topStart; y < GridHeight; y++)
            {
                waterCells.Add(new Vector2Int(x, y));
            }
        }
    }

    private void FlattenTerrainNearWater()
    {
        int shoreRow = GridHeight - WaterRiverWidth;
        int beachNearRow = shoreRow - 1;
        int beachFarRow = shoreRow - 2;
        int gradRow  = beachFarRow - 1;
        const float waterCellHeight = 0.22f;

        // Keep water cells at a fixed explicit level so the strip reads clearly against the beach shelf.
        foreach (Vector2Int cell in waterCells)
            terrainHeights[cell.x, cell.y] = waterCellHeight;

        if (beachNearRow >= 0)
            for (int x = 0; x < GridWidth; x++)
                terrainHeights[x, beachNearRow] = 0.02f;

        if (beachFarRow >= 0)
            for (int x = 0; x < GridWidth; x++)
                terrainHeights[x, beachFarRow] = Mathf.Min(terrainHeights[x, beachFarRow], 0.08f);

        // One gradient row before the sand shelf — gentle slope down into the beach.
        if (gradRow >= 0)
            for (int x = 0; x < GridWidth; x++)
                terrainHeights[x, gradRow] = Mathf.Min(terrainHeights[x, gradRow], 0.22f);
    }

    private void CreateWaterLayer()
    {
        waterShoreFoams.Clear();
        waterShoreWashPatches.Clear();
        riverFish.Clear();
        if (waterEffectsRoot != null)
        {
            Destroy(waterEffectsRoot.gameObject);
        }

        if (worldRoot == null || waterCells.Count == 0)
        {
            return;
        }

        waterEffectsRoot = new GameObject("WaterEffects").transform;
        waterEffectsRoot.SetParent(worldRoot, false);
        riverFishRoot = new GameObject("RiverFish").transform;
        riverFishRoot.SetParent(waterEffectsRoot, false);

        float fullWidth = GridWidth - 0.8f;
        float centerX = GridWidth * 0.5f;
        int shoreRow = GridHeight - WaterRiverWidth;
        int beachNearRow = shoreRow - 1;
        int beachFarRow = shoreRow - 2;

        int segmentCount = Mathf.Max(6, Mathf.RoundToInt(GridWidth / 4f));
        float segmentWidth = fullWidth / segmentCount + 0.18f;
        for (int ring = 0; ring < 2; ring++)
        {
            int targetRow = ring == 0 ? beachNearRow : beachFarRow;
            if (targetRow < 0)
            {
                continue;
            }

            for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
            {
                GameObject washPatch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                washPatch.name = $"WaterWash_r{ring + 1}_{segmentIndex + 1}";
                washPatch.transform.SetParent(waterEffectsRoot, false);
                washPatch.transform.localScale = new Vector3(segmentWidth, 0.008f, ring == 0 ? 0.86f : 0.78f);
                Renderer washRenderer = washPatch.GetComponent<Renderer>();
                washRenderer.sharedMaterial = CreateTransparentOverlayMaterial(new Color(0.78f, 0.95f, 1f, ring == 0 ? 0.22f : 0.16f));
                washRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                washRenderer.receiveShadows = false;
                if (washPatch.TryGetComponent(out Collider washCollider))
                {
                    washCollider.enabled = false;
                }

                float baseX = 0.4f + segmentWidth * 0.5f + segmentIndex * (fullWidth / segmentCount);
                float baseZ = targetRow + 0.5f;
                float baseY = ring == 0 ? 0.038f : 0.026f;
                washPatch.transform.position = new Vector3(baseX, baseY, baseZ);

                waterShoreWashPatches.Add(new WaterShoreWashPatchData
                {
                    RootTransform = washPatch.transform,
                    Renderer = washRenderer,
                    Material = washRenderer.sharedMaterial,
                    BaseX = baseX,
                    BaseY = baseY,
                    BaseZ = baseZ,
                    Width = segmentWidth,
                    Depth = ring == 0 ? 0.86f : 0.78f,
                    ShoreRingIndex = ring,
                    SegmentIndex = segmentIndex,
                    PhaseOffset = Random.Range(0f, 1f)
                });
            }
        }

        for (int i = 0; i < 3; i++)
        {
            GameObject foam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foam.name = $"WaterShoreFoamBand_{i + 1}";
            foam.transform.SetParent(waterEffectsRoot, false);
            foam.transform.localScale = new Vector3(fullWidth, 0.01f, 0.08f + i * 0.025f);
            ApplyColor(foam, new Color(0.92f, 0.97f, 1f));
            ConfigureStaticVisual(foam);
            if (foam.TryGetComponent(out Collider foamCollider))
            {
                foamCollider.enabled = false;
            }

            float z = shoreRow + 0.14f + i * 0.08f;
            foam.transform.position = new Vector3(centerX, 0.238f + i * 0.003f, z);

            Renderer foamRenderer = foam.GetComponent<Renderer>();
            waterShoreFoams.Add(new WaterShoreFoamData
            {
                RootTransform = foam.transform,
                Renderer = foamRenderer,
                Material = foamRenderer != null ? foamRenderer.material : null,
                BaseY = 0.238f + i * 0.003f,
                BaseZ = z,
                Width = fullWidth,
                DriftSpeed = Random.Range(0.18f, 0.34f),
                DriftOffset = Random.Range(0f, 10f),
                PulseSpeed = Random.Range(0.7f, 1.2f),
                PhaseOffset = Random.Range(0f, 10f)
            });
        }
    }

    private void SetupRiverFish()
    {
        riverFish.Clear();
        if (riverFishRoot != null)
        {
            for (int i = riverFishRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(riverFishRoot.GetChild(i).gameObject);
            }
        }

        riverFishSpawnTimer = Random.Range(2.5f, 5.5f);
        int initialCount = Mathf.Min(3, RiverFishMaxActiveCount);
        for (int i = 0; i < initialCount; i++)
        {
            SpawnRiverFish(Random.Range(0.12f, GridWidth * 0.35f));
        }
    }

    private void SpawnRiverFish(float? forcedX = null)
    {
        if (riverFishRoot == null || riverFish.Count >= RiverFishMaxActiveCount)
        {
            return;
        }

        int shoreRow = GridHeight - WaterRiverWidth;
        float startX = forcedX ?? 0.18f;
        float z = Random.Range(shoreRow + 0.35f, GridHeight - 0.45f);
        float depthY = Random.Range(0.1f, 0.16f);

        GameObject fishRootObject = new($"RiverFish_{riverFish.Count + 1}");
        fishRootObject.transform.SetParent(riverFishRoot, false);
        fishRootObject.transform.position = new Vector3(startX, depthY, z);
        fishRootObject.transform.rotation = Quaternion.identity;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(fishRootObject.transform, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        body.transform.localScale = new Vector3(0.1f, 0.18f, 0.08f);
        Color bodyColor = Color.Lerp(new Color(0.22f, 0.36f, 0.46f), new Color(0.34f, 0.5f, 0.58f), Random.value);
        ApplyColor(body, bodyColor);
        ConfigureStaticVisual(body);
        if (body.TryGetComponent(out Collider bodyCollider))
        {
            bodyCollider.enabled = false;
        }

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tail.transform.SetParent(fishRootObject.transform, false);
        tail.transform.localPosition = new Vector3(-0.16f, 0f, 0f);
        tail.transform.localScale = new Vector3(0.1f, 0.08f, 0.02f);
        Color tailColor = Color.Lerp(bodyColor * 0.9f, new Color(0.56f, 0.68f, 0.72f), 0.25f);
        ApplyColor(tail, tailColor);
        ConfigureStaticVisual(tail);
        if (tail.TryGetComponent(out Collider tailCollider))
        {
            tailCollider.enabled = false;
        }

        Renderer fishBodyRenderer = body.GetComponent<Renderer>();
        Renderer fishTailRenderer = tail.GetComponent<Renderer>();
        riverFish.Add(new RiverFishData
        {
            RootTransform = fishRootObject.transform,
            BodyTransform = body.transform,
            TailTransform = tail.transform,
            BodyRenderer = fishBodyRenderer,
            TailRenderer = fishTailRenderer,
            BodyMaterial = fishBodyRenderer != null ? fishBodyRenderer.material : null,
            TailMaterial = fishTailRenderer != null ? fishTailRenderer.material : null,
            WorldX = startX,
            WorldZ = z,
            SwimSpeed = Random.Range(0.48f, 0.86f),
            DepthY = depthY,
            BobPhase = Random.Range(0f, 10f),
            TailPhase = Random.Range(0f, 10f),
            LateralDriftAmplitude = Random.Range(0.04f, 0.12f),
            LateralDriftSpeed = Random.Range(0.25f, 0.55f),
            BodyColor = bodyColor
        });
    }

    private void SetupDioramaPostProcessing()
    {
        UniversalAdditionalCameraData cameraData = mainCamera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.Medium;

        RenderSettings.fog = false;
        RenderSettings.fogMode = FogMode.Linear;

        GameObject volumeObject = new("DioramaVolume");
        volumeObject.transform.SetParent(worldRoot, false);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = profile;

        ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.postExposure.Override(0.08f);
        colorAdjustments.contrast.Override(10f);
        colorAdjustments.saturation.Override(12f);
        colorAdjustments.colorFilter.Override(new Color(1f, 0.97f, 0.93f, 1f));

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.86f);
        bloom.intensity.Override(0.12f);
        bloom.scatter.Override(0.52f);
        bloom.tint.Override(new Color(1f, 0.94f, 0.84f, 1f));
        bloom.highQualityFiltering.Override(false);

        DepthOfField depthOfField = profile.Add<DepthOfField>(true);
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(36f);
        depthOfField.gaussianEnd.Override(78f);
        depthOfField.gaussianMaxRadius.Override(0.018f);
        depthOfField.highQualitySampling.Override(false);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.045f);
        vignette.smoothness.Override(0.42f);
        vignette.rounded.Override(false);
    }

    private void SetupSurfaceMaterials()
    {
        groundSurfaceTexture = CreateStylizedGroundTexture(128);
        grassSurfaceTexture = CreateStylizedGrassTexture(128);
        groundSurfaceMaterial = CreateSurfaceMaterial(groundSurfaceTexture, new Color(0.96f, 0.93f, 0.86f), 0.09f);
        grassSurfaceMaterial = CreateSurfaceMaterial(grassSurfaceTexture, new Color(0.74f, 0.82f, 0.72f), 0.07f);
        shoreSurfaceMaterial = CreateSurfaceMaterial(groundSurfaceTexture, new Color(0.84f, 0.76f, 0.58f), 0.05f);
        beachSurfaceMaterial = CreateSurfaceMaterial(groundSurfaceTexture, new Color(0.92f, 0.84f, 0.66f), 0.07f);
        waterShallowMaterial = CreateSurfaceMaterial(null, new Color(0.44f, 0.78f, 0.9f), 0.95f);
        waterDeepMaterial    = CreateSurfaceMaterial(null, new Color(0.14f, 0.39f, 0.7f), 0.98f);
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

        Color baseColor = new(0.79f, 0.73f, 0.61f);
        Color warmPatch = new(0.87f, 0.8f, 0.66f);
        Color coolPatch = new(0.69f, 0.64f, 0.54f);
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

                float warmMask = 0f;
                for (int i = 0; i < blotchCenters.Length; i++)
                {
                    float dist = Vector2.Distance(new Vector2(u, v), blotchCenters[i]);
                    warmMask += Mathf.Clamp01(1f - dist * 3.2f);
                }
                warmMask = Mathf.Clamp01(warmMask * 0.42f);

                Color color = Color.Lerp(coolPatch, baseColor, largeNoise);
                color = Color.Lerp(color, warmPatch, warmMask);
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

        Color darkGreen = new(0.12f, 0.3f, 0.11f);
        Color baseGreen = new(0.22f, 0.46f, 0.2f);
        Color lightGreen = new(0.38f, 0.6f, 0.29f);

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

                Color color = Color.Lerp(darkGreen, baseGreen, broadNoise);
                color = Color.Lerp(color, lightGreen, Mathf.Clamp01(fineNoise * 0.78f));
                color = Color.Lerp(color, lightGreen * 1.05f, Mathf.Clamp01((bladeNoise - 0.52f) * 1.8f));
                color *= 0.96f + stripe;
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
        Material material = new(useGrassPatch ? grassSurfaceMaterial : groundSurfaceMaterial);
        float tintNoise = Mathf.PerlinNoise((x + 1) * 0.37f, (y + 1) * 0.41f);
        Color tint = useGrassPatch
            ? Color.Lerp(new Color(0.74f, 0.82f, 0.7f), new Color(0.84f, 0.9f, 0.78f), tintNoise)
            : Color.Lerp(new Color(0.95f, 0.91f, 0.84f), new Color(1.01f, 0.98f, 0.92f), tintNoise);
        material.color = tint;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        material.mainTextureScale = useGrassPatch ? new Vector2(0.54f, 0.54f) : new Vector2(0.62f, 0.62f);
        material.mainTextureOffset = new Vector2((x % 5) * 0.13f, (y % 5) * 0.11f);
        renderer.material = material;
    }

    private bool IsGrassGroundCell(int x, int y)
    {
        if (grassSurfaceMaterial == null)
        {
            return false;
        }

        float grassPatchNoise = Mathf.PerlinNoise((x + 1) * 0.18f + 4.2f, (y + 1) * 0.2f + 7.4f);
        if (IsDenseForestCell(x, y))
        {
            return true;
        }

        return grassPatchNoise > 0.5f;
    }

    private bool IsDenseForestCell(int x, int y)
    {
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
        if (!IsDenseForestCell(cell.x, cell.y))
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
        return radialFalloff * 10f + localNoise;
    }

    private bool IsWaterOrBeachCell(Vector2Int cell)
    {
        if (waterCells.Contains(cell))
        {
            return true;
        }

        int shoreRow = GridHeight - WaterRiverWidth;
        return cell.y == shoreRow - 1 || cell.y == shoreRow - 2;
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

    private void CreateDioramaBase()
    {
        Vector3 center = new(GridWidth * 0.5f, -0.38f, GridHeight * 0.5f);

        GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plinth.name = "DioramaPlinth";
        plinth.transform.SetParent(worldRoot, false);
        plinth.transform.position = center;
        plinth.transform.localScale = new Vector3(GridWidth + 2.4f, 0.82f, GridHeight + 2.4f);
        ApplyColor(plinth, new Color(0.73f, 0.66f, 0.56f));

        GameObject baseLip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseLip.name = "DioramaLip";
        baseLip.transform.SetParent(worldRoot, false);
        baseLip.transform.position = new Vector3(center.x, -0.12f, center.z);
        baseLip.transform.localScale = new Vector3(GridWidth + 0.8f, 0.14f, GridHeight + 0.8f);
        ApplyColor(baseLip, new Color(0.87f, 0.81f, 0.7f));

        CreateDioramaBoundary(new Vector3(GridWidth * 0.5f, 0.22f, -0.3f), new Vector3(GridWidth + 0.9f, 0.32f, 0.24f));
        CreateDioramaBoundary(new Vector3(GridWidth * 0.5f, 0.22f, GridHeight + 0.3f), new Vector3(GridWidth + 0.9f, 0.32f, 0.24f));
        CreateDioramaBoundary(new Vector3(-0.3f, 0.22f, GridHeight * 0.5f), new Vector3(0.24f, 0.32f, GridHeight + 0.9f));
        CreateDioramaBoundary(new Vector3(GridWidth + 0.3f, 0.22f, GridHeight * 0.5f), new Vector3(0.24f, 0.32f, GridHeight + 0.9f));
    }

    private void CreateDioramaBoundary(Vector3 position, Vector3 scale)
    {
        GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundary.transform.SetParent(worldRoot, false);
        boundary.transform.position = position;
        boundary.transform.localScale = scale;
        ApplyColor(boundary, new Color(0.9f, 0.85f, 0.75f));
    }

    private System.Collections.IEnumerator SetupGridAsync()
    {
        GameObject gridRoot = new("GridLines");
        gridRoot.transform.SetParent(worldRoot, false);

        Material lineMaterial = new(ShaderRefs.Sprites)
        {
            color = new Color(0f, 0f, 0f, 0.18f)
        };

        for (int x = 0; x <= GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                float edgeHeight = GetVerticalEdgeHeight(x, y) + 0.025f;
                CreateGridLine(gridRoot.transform, lineMaterial, new Vector3(x, edgeHeight, y), new Vector3(x, edgeHeight, y + 1f));
            }
            if (x % 8 == 7) yield return null;
        }

        for (int y = 0; y <= GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                float edgeHeight = GetHorizontalEdgeHeight(x, y) + 0.025f;
                CreateGridLine(gridRoot.transform, lineMaterial, new Vector3(x, edgeHeight, y), new Vector3(x + 1f, edgeHeight, y));
            }
            if (y % 8 == 7) yield return null;
        }
    }

}
