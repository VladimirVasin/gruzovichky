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
        // Opaque water body sits low; transparent surface stack floats above it.
        float waterSurfaceCenterY    = waterCellHeight - 0.005f;
        float waterMidSurfaceCenterY = waterCellHeight - 0.012f;
        float waterLowSurfaceCenterY = waterCellHeight - 0.019f;
        waterSurfaceTiles.Clear();
        waterBodyTiles.Clear();

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                bool isWater = waterCells.Contains(new Vector2Int(x, y));
                bool isNearBeach = y == beachNearRow || naturalBeachCells.Contains(new Vector2Int(x, y));
                bool isFarBeach = y == beachFarRow;
                bool isBeach = isNearBeach || isFarBeach;

                float terrainHeight = isWater ? waterBaseTopHeight : terrainHeights[x, y];
                float thickness = isWater ? 0.18f : 0.28f + terrainHeight;

                float bottomY = terrainHeight - thickness - 0.02f;
                GameObject groundTile = isWater
                    ? CreateWaterBodyCellMesh($"Water_{x}_{y}", x, y, waterBaseTopHeight, bottomY)
                    : CreateTerrainCellMesh($"Ground_{x}_{y}", x, y, bottomY, 0.01f);

                if (isWater)
                {
                    float t = lakeWaterCells.Contains(new Vector2Int(x, y))
                        ? GetLakeWaterDepth01(x, y)
                        : (float)(y - shoreRow) / Mathf.Max(WaterRiverWidth - 1, 1);

                    // Solid tile: per-tile depth colour (instance material)
                    Color baseWaterColor = Color.Lerp(
                        new Color(0.26f, 0.62f, 0.78f),
                        new Color(0.03f, 0.14f, 0.38f),
                        t);
                    Renderer r = groundTile.GetComponent<Renderer>();
                    r.material = CreateSurfaceMaterial(null, baseWaterColor, 0.92f);
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = true;
                    float wavePhase = Random.Range(0f, 10f);
                    waterBodyTiles.Add(new WaterBodyTileData
                    {
                        Transform = groundTile.transform,
                        Mesh = groundTile.GetComponent<MeshFilter>().sharedMesh,
                        BaseY = 0f,
                        BaseTopY = waterBaseTopHeight,
                        BottomY = bottomY,
                        CurrentTopY = waterBaseTopHeight,
                        Cell = new Vector2Int(x, y),
                        PhaseOffset = wavePhase
                    });

                    // Top shimmer layer вЂ” wide, light, animated bob
                    Color topColor = Color.Lerp(
                        new Color(0.86f, 0.98f, 1.00f, 0.11f),
                        new Color(0.14f, 0.44f, 0.78f, 0.055f), t);
                    GameObject waterSurface = CreateWaterSurfaceCellMesh($"WaterSurface_{x}_{y}", x, y, waterSurfaceCenterY, 1f);
                    Renderer surfaceRenderer = waterSurface.GetComponent<Renderer>();
                    surfaceRenderer.material = CreateTransparentOverlayMaterial(topColor);
                    surfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    surfaceRenderer.receiveShadows = false;

                    waterSurfaceTiles.Add(new WaterSurfaceTileData
                    {
                        Renderer = surfaceRenderer,
                        Material = surfaceRenderer.material,
                        Transform = waterSurface.transform,
                        Mesh = waterSurface.GetComponent<MeshFilter>().sharedMesh,
                        BaseY = waterSurfaceCenterY,
                        CurrentTopY = waterSurfaceCenterY,
                        Cell = new Vector2Int(x, y),
                        BobAmplitude = y == shoreRow ? 0.028f : 0.04f,
                        BobSpeed = y == shoreRow ? 0.78f : 1.02f,
                        PhaseOffset = wavePhase,
                        LayerIndex = 0
                    });

                    // Mid layer вЂ” slightly smaller, more saturated
                    Color midColor = Color.Lerp(
                        new Color(0.44f, 0.78f, 0.92f, 0.09f),
                        new Color(0.07f, 0.28f, 0.58f, 0.055f), t);
                    GameObject waterMidSurface = CreateWaterSurfaceCellMesh($"WaterMidSurface_{x}_{y}", x, y, waterMidSurfaceCenterY, 1.0f);
                    Renderer midSurfaceRenderer = waterMidSurface.GetComponent<Renderer>();
                    midSurfaceRenderer.material = CreateTransparentOverlayMaterial(midColor);
                    midSurfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    midSurfaceRenderer.receiveShadows = false;
                    waterSurfaceTiles.Add(new WaterSurfaceTileData
                    {
                        Renderer = midSurfaceRenderer,
                        Material = midSurfaceRenderer.material,
                        Transform = waterMidSurface.transform,
                        Mesh = waterMidSurface.GetComponent<MeshFilter>().sharedMesh,
                        BaseY = waterMidSurfaceCenterY,
                        CurrentTopY = waterMidSurfaceCenterY,
                        Cell = new Vector2Int(x, y),
                        BobAmplitude = y == shoreRow ? 0.022f : 0.03f,
                        BobSpeed = y == shoreRow ? 0.74f : 0.96f,
                        PhaseOffset = wavePhase,
                        LayerIndex = 1
                    });

                    // Low layer вЂ” noticeably smaller, darkest, acts as "depth shadow"
                    Color lowColor = Color.Lerp(
                        new Color(0.16f, 0.46f, 0.68f, 0.16f),
                        new Color(0.02f, 0.12f, 0.34f, 0.10f), t);
                    GameObject waterLowSurface = CreateWaterSurfaceCellMesh($"WaterLowSurface_{x}_{y}", x, y, waterLowSurfaceCenterY, 1f);
                    Renderer lowSurfaceRenderer = waterLowSurface.GetComponent<Renderer>();
                    lowSurfaceRenderer.material = CreateTransparentOverlayMaterial(lowColor);
                    lowSurfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    lowSurfaceRenderer.receiveShadows = false;
                    waterSurfaceTiles.Add(new WaterSurfaceTileData
                    {
                        Renderer = lowSurfaceRenderer,
                        Material = lowSurfaceRenderer.material,
                        Transform = waterLowSurface.transform,
                        Mesh = waterLowSurface.GetComponent<MeshFilter>().sharedMesh,
                        BaseY = waterLowSurfaceCenterY,
                        CurrentTopY = waterLowSurfaceCenterY,
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
                        foamRenderer.material.color = new Color(0.88f, 0.94f, 0.98f);
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

    private GameObject CreateTerrainCellMesh(string name, int x, int y, float bottomY, float lift)
    {
        GameObject cell = new(name);
        cell.transform.SetParent(groundRoot, false);

        float x0 = x;
        float x1 = x + 1f;
        float z0 = y;
        float z1 = y + 1f;

        float h00 = SampleTerrainHeight(x0, z0) + lift;
        float h10 = SampleTerrainHeight(x1, z0) + lift;
        float h01 = SampleTerrainHeight(x0, z1) + lift;
        float h11 = SampleTerrainHeight(x1, z1) + lift;
        CreateCellBoxMesh(cell, x0, x1, z0, z1, h00, h10, h01, h11, bottomY);
        return cell;
    }

    private GameObject CreateWaterBodyCellMesh(string name, int x, int y, float topY, float bottomY)
    {
        GameObject cell = new(name);
        cell.transform.SetParent(groundRoot, false);
        CreateCellBoxMesh(cell, x, x + 1f, y, y + 1f, topY, topY, topY, topY, bottomY);
        cell.GetComponent<MeshFilter>().sharedMesh.MarkDynamic();
        return cell;
    }

    private GameObject CreateWaterSurfaceCellMesh(string name, int x, int y, float topY, float size)
    {
        GameObject cell = new(name);
        cell.transform.SetParent(groundRoot, false);

        float inset = (1f - size) * 0.5f;
        float x0 = x + inset;
        float x1 = x + 1f - inset;
        float z0 = y + inset;
        float z1 = y + 1f - inset;

        Mesh mesh = new();
        mesh.name = $"{name}_Mesh";
        mesh.MarkDynamic();
        mesh.vertices = new[]
        {
            new Vector3(x0, topY, z0),
            new Vector3(x1, topY, z0),
            new Vector3(x0, topY, z1),
            new Vector3(x1, topY, z1),
        };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = cell.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        cell.AddComponent<MeshRenderer>();
        return cell;
    }

    private static void CreateCellBoxMesh(
        GameObject target,
        float x0,
        float x1,
        float z0,
        float z1,
        float h00,
        float h10,
        float h01,
        float h11,
        float bottomY)
    {
        Mesh mesh = new();
        mesh.name = $"{target.name}_Mesh";
        mesh.vertices = new[]
        {
            new Vector3(x0, h00, z0),
            new Vector3(x1, h10, z0),
            new Vector3(x0, h01, z1),
            new Vector3(x1, h11, z1),
            new Vector3(x0, bottomY, z0),
            new Vector3(x1, bottomY, z0),
            new Vector3(x0, bottomY, z1),
            new Vector3(x1, bottomY, z1),
        };
        mesh.triangles = new[]
        {
            0, 2, 1, 1, 2, 3,
            4, 5, 6, 5, 7, 6,
            0, 1, 4, 1, 5, 4,
            2, 6, 3, 3, 6, 7,
            0, 4, 2, 2, 4, 6,
            1, 3, 5, 3, 7, 5,
        };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        target.AddComponent<MeshFilter>().sharedMesh = mesh;
        target.AddComponent<MeshRenderer>();
    }

    private void PopulateWaterCells()
    {
        waterCells.Clear();
        lakeWaterCells.Clear();
        naturalBeachCells.Clear();
        int topStart = GridHeight - WaterRiverWidth;
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = topStart; y < GridHeight; y++)
            {
                waterCells.Add(new Vector2Int(x, y));
            }
        }

        AddLakeWaterCellsFromNaturalZones();
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

        foreach (Vector2Int cell in naturalBeachCells)
        {
            terrainHeights[cell.x, cell.y] = Mathf.Min(terrainHeights[cell.x, cell.y], 0.06f);
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int slope = new(cell.x + dx, cell.y + dy);
                    if (slope.x < 0 || slope.x >= GridWidth || slope.y < 0 || slope.y >= GridHeight ||
                        waterCells.Contains(slope) || naturalBeachCells.Contains(slope))
                    {
                        continue;
                    }

                    terrainHeights[slope.x, slope.y] = Mathf.Min(terrainHeights[slope.x, slope.y], 0.26f);
                }
            }
        }

        if (beachNearRow >= 0)
            for (int x = 0; x < GridWidth; x++)
                terrainHeights[x, beachNearRow] = 0.02f;

        if (beachFarRow >= 0)
            for (int x = 0; x < GridWidth; x++)
                terrainHeights[x, beachFarRow] = Mathf.Min(terrainHeights[x, beachFarRow], 0.08f);

        // One gradient row before the sand shelf вЂ” gentle slope down into the beach.
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
        for (int ring = 0; ring < 3; ring++)
        {
            int targetRow = ring == 0 ? beachNearRow : ring == 1 ? beachFarRow : shoreRow;
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
                float baseAlpha = ring == 0 ? 0.20f : ring == 1 ? 0.14f : 0.1f;
                washRenderer.sharedMaterial = CreateTransparentOverlayMaterial(new Color(0.78f, 0.95f, 1f, baseAlpha));
                washRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                washRenderer.receiveShadows = false;
                if (washPatch.TryGetComponent(out Collider washCollider))
                {
                    washCollider.enabled = false;
                }

                float baseX = 0.4f + segmentWidth * 0.5f + segmentIndex * (fullWidth / segmentCount);
                float baseZ = ring == 0 ? targetRow + 0.56f : ring == 1 ? targetRow + 0.52f : targetRow + 0.18f;
                float baseY = ring == 0 ? 0.04f : ring == 1 ? 0.028f : 0.224f;
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
                    Depth = ring == 0 ? 0.92f : ring == 1 ? 0.78f : 0.46f,
                    ShoreRingIndex = ring,
                    SegmentIndex = segmentIndex,
                    PhaseOffset = Random.Range(0f, 1f)
                });
            }
        }

        for (int i = 0; i < 4; i++)
        {
            GameObject foam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foam.name = $"WaterShoreFoamBand_{i + 1}";
            foam.transform.SetParent(waterEffectsRoot, false);
            foam.transform.localScale = new Vector3(fullWidth, 0.01f, 0.07f + i * 0.02f);
            ApplyColor(foam, new Color(0.92f, 0.97f, 1f));
            ConfigureStaticVisual(foam);
            if (foam.TryGetComponent(out Collider foamCollider))
            {
                foamCollider.enabled = false;
            }

            float z = shoreRow + 0.12f + i * 0.06f;
            foam.transform.position = new Vector3(centerX, 0.236f + i * 0.0025f, z);

            Renderer foamRenderer = foam.GetComponent<Renderer>();
            waterShoreFoams.Add(new WaterShoreFoamData
            {
                RootTransform = foam.transform,
                Renderer = foamRenderer,
                Material = foamRenderer != null ? foamRenderer.material : null,
                BaseY = 0.236f + i * 0.0025f,
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

    private void SetupNightSky()
    {
        nightStars.Clear();
        if (nightSkyRoot != null)
        {
            Destroy(nightSkyRoot.gameObject);
        }

        if (worldRoot == null)
        {
            return;
        }

        nightSkyRoot = new GameObject("NightSky").transform;
        nightSkyRoot.SetParent(worldRoot, false);

        GameObject moonObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moonObj.name = "Moon";
        moonObj.transform.SetParent(nightSkyRoot, false);
        moonObj.transform.position = new Vector3(GridWidth * 0.5f, 68f, GridHeight + 62f);
        moonObj.transform.localScale = Vector3.one * 7f;
        moonMaterial = CreateTransparentOverlayMaterial(new Color(1f, 0.98f, 0.92f, 0f));
        Renderer moonRend = moonObj.GetComponent<Renderer>();
        moonRend.sharedMaterial = moonMaterial;
        moonRend.shadowCastingMode = ShadowCastingMode.Off;
        moonRend.receiveShadows = false;
        if (moonObj.TryGetComponent(out Collider moonCol)) moonCol.enabled = false;

        for (int i = 0; i < NightStarCount; i++)
        {
            float x = Random.Range(-30f, GridWidth + 30f);
            float y = Random.Range(54f, 84f);
            float z = Random.Range(-40f, GridHeight + 50f);
            float scale = Random.Range(0.24f, 0.62f);

            int ct = i % 3;
            Color baseColor = ct == 0
                ? new Color(1f, 0.90f, 0.76f, 0f)
                : ct == 1
                  ? new Color(0.90f, 0.94f, 1f, 0f)
                  : new Color(1f, 1f, 0.98f, 0f);

            GameObject starObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            starObj.name = $"Star_{i + 1}";
            starObj.transform.SetParent(nightSkyRoot, false);
            starObj.transform.position = new Vector3(x, y, z);
            starObj.transform.localScale = Vector3.one * scale;

            Material starMat = CreateTransparentOverlayMaterial(baseColor);
            Renderer sr = starObj.GetComponent<Renderer>();
            sr.sharedMaterial = starMat;
            sr.shadowCastingMode = ShadowCastingMode.Off;
            sr.receiveShadows = false;
            if (starObj.TryGetComponent(out Collider starCol)) starCol.enabled = false;

            nightStars.Add(new NightStarData
            {
                Transform    = starObj.transform,
                Material     = starMat,
                BaseColor    = baseColor,
                TwinkleSpeed = Random.Range(0.7f, 2.4f),
                TwinklePhase = Random.Range(0f, 10f)
            });
        }
    }

    private void UpdateNightSky()
    {
        float nightStrength = 1f - Mathf.SmoothStep(0.05f, 0.38f, currentStylizedDaylight);
        float time = Time.time;

        if (moonMaterial != null)
        {
            moonMaterial.color = new Color(1f, 0.98f, 0.92f, nightStrength * 0.97f);
        }

        for (int i = nightStars.Count - 1; i >= 0; i--)
        {
            NightStarData star = nightStars[i];
            if (star.Transform == null)
            {
                nightStars.RemoveAt(i);
                continue;
            }

            float twinkle = 1f + Mathf.Sin(time * star.TwinkleSpeed + star.TwinklePhase) * 0.17f;
            float alpha = nightStrength * Mathf.Clamp01(twinkle);
            Color c = star.BaseColor;
            star.Material.color = new Color(c.r, c.g, c.b, alpha);
        }
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
        dioramaVolume = volume;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = profile;
        dioramaVolumeProfile = profile;

        ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.postExposure.Override(0.04f);
        colorAdjustments.contrast.Override(14f);
        colorAdjustments.saturation.Override(10f);
        colorAdjustments.colorFilter.Override(new Color(1f, 0.98f, 0.95f, 1f));
        dioramaColorAdjustments = colorAdjustments;

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.84f);
        bloom.intensity.Override(0.14f);
        bloom.scatter.Override(0.58f);
        bloom.tint.Override(new Color(1f, 0.93f, 0.84f, 1f));
        bloom.highQualityFiltering.Override(true);
        dioramaBloom = bloom;

        DepthOfField depthOfField = profile.Add<DepthOfField>(true);
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(32f);
        depthOfField.gaussianEnd.Override(72f);
        depthOfField.gaussianMaxRadius.Override(0.022f);
        depthOfField.highQualitySampling.Override(true);
        dioramaDepthOfField = depthOfField;

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.052f);
        vignette.smoothness.Override(0.44f);
        vignette.rounded.Override(true);
        dioramaVignette = vignette;

        UpdateDioramaPostProcessing(1f, 0f, 1f, mainCamera.backgroundColor);
    }

    private void UpdateDioramaPostProcessing(float stylizedDaylight, float lowSun, float sunArc, Color backgroundColor)
    {
        if (dioramaColorAdjustments == null || dioramaBloom == null || dioramaDepthOfField == null || dioramaVignette == null)
        {
            return;
        }

        float dawnDuskStrength = Mathf.Clamp01(lowSun * Mathf.Lerp(0.2f, 1f, stylizedDaylight));
        float nightStrength = 1f - stylizedDaylight;
        float zoomT = Mathf.InverseLerp(CameraMinHeight, CameraMaxHeight, cameraOffset.y);
        float farBloomBoost = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.32f, 1f, zoomT));

        dioramaColorAdjustments.postExposure.Override(Mathf.Lerp(-0.18f, 0.1f, stylizedDaylight) + dawnDuskStrength * 0.03f + farBloomBoost * 0.08f);
        dioramaColorAdjustments.contrast.Override(Mathf.Lerp(8f, 17f, stylizedDaylight));
        dioramaColorAdjustments.saturation.Override(Mathf.Lerp(-6f, 14f, stylizedDaylight));
        dioramaColorAdjustments.colorFilter.Override(Color.Lerp(
            Color.Lerp(new Color(0.84f, 0.88f, 1f, 1f), new Color(1f, 0.88f, 0.78f, 1f), dawnDuskStrength),
            new Color(1f, 0.985f, 0.955f, 1f),
            stylizedDaylight));

        dioramaBloom.threshold.Override(Mathf.Max(0.34f, Mathf.Lerp(0.72f, 0.86f, stylizedDaylight) - farBloomBoost * 0.32f));
        dioramaBloom.intensity.Override(Mathf.Lerp(0.22f, 0.16f, stylizedDaylight) + dawnDuskStrength * 0.34f + farBloomBoost * Mathf.Lerp(0.55f, 0.82f, stylizedDaylight));
        dioramaBloom.scatter.Override(Mathf.Min(0.95f, Mathf.Lerp(0.66f, 0.56f, stylizedDaylight) + farBloomBoost * 0.24f + dawnDuskStrength * 0.18f));
        dioramaBloom.tint.Override(Color.Lerp(
            new Color(0.88f, 0.92f, 1f, 1f),
            Color.Lerp(new Color(1f, 0.84f, 0.72f, 1f), new Color(1f, 0.95f, 0.88f, 1f), sunArc),
            Mathf.Lerp(stylizedDaylight, 1f, dawnDuskStrength)));

        dioramaDepthOfField.gaussianStart.Override(Mathf.Lerp(26f, 40f, zoomT));
        dioramaDepthOfField.gaussianEnd.Override(Mathf.Lerp(60f, 86f, zoomT));
        dioramaDepthOfField.gaussianMaxRadius.Override(Mathf.Lerp(0.026f, 0.016f, zoomT));

        dioramaVignette.intensity.Override(Mathf.Lerp(0.07f, 0.045f, stylizedDaylight) + nightStrength * 0.01f);
        dioramaVignette.smoothness.Override(Mathf.Lerp(0.5f, 0.4f, stylizedDaylight));
    }

    private void SetupSurfaceMaterials()
    {
        groundSurfaceTexture = CreateStylizedGroundTexture(128);
        grassSurfaceTexture = CreateStylizedGrassTexture(128);
        roadSurfaceTexture = CreateStylizedRoadTexture(128);
        groundSurfaceMaterial = CreateSurfaceMaterial(groundSurfaceTexture, new Color(0.95f, 0.91f, 0.84f), 0.1f);
        grassSurfaceMaterial = CreateSurfaceMaterial(grassSurfaceTexture, new Color(0.72f, 0.82f, 0.69f), 0.08f);
        shoreSurfaceMaterial = CreateSurfaceMaterial(groundSurfaceTexture, new Color(0.76f, 0.67f, 0.55f), 0.08f);
        beachSurfaceMaterial = CreateSurfaceMaterial(groundSurfaceTexture, new Color(0.92f, 0.84f, 0.70f), 0.1f);
        roadSurfaceMaterial = CreateSurfaceMaterial(roadSurfaceTexture, new Color(0.21f, 0.22f, 0.24f), 0.16f);
        roadShoulderMaterial = CreateSurfaceMaterial(roadSurfaceTexture, new Color(0.54f, 0.49f, 0.41f), 0.11f);
        highwaySurfaceMaterial = CreateSurfaceMaterial(roadSurfaceTexture, new Color(0.16f, 0.17f, 0.19f), 0.2f);
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

        Material material = new(sourceMaterial);
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
        material.color = tint;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", tint);
        }

        material.mainTextureScale = isShoulder ? new Vector2(0.52f, 0.9f) : new Vector2(0.7f, 1.15f);
        material.mainTextureOffset = new Vector2((x % 7) * 0.09f, (y % 7) * 0.08f);
        renderer.material = material;
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

    private bool IsWaterOrBeachCell(Vector2Int cell)
    {
        if (waterCells.Contains(cell))
        {
            return true;
        }

        return IsNaturalBeachCell(cell);
    }

    private float GetLakeWaterDepth01(int x, int y)
    {
        int neighborWaterCount = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                if (lakeWaterCells.Contains(new Vector2Int(x + dx, y + dy)))
                {
                    neighborWaterCount++;
                }
            }
        }

        return Mathf.Clamp01(neighborWaterCount / 8f);
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
        yield return null; // one-frame defer so callers stay async-compatible

        GameObject gridRoot = new("GridLines");
        gridRoot.transform.SetParent(worldRoot, false);
        gridLinesRoot = gridRoot.transform;

        Material lineMaterial = new(ShaderRefs.Sprites)
        {
            color = new Color(0f, 0f, 0f, 0.18f)
        };

        BuildGridLineMesh(gridRoot.transform, lineMaterial);
    }

    private void BuildGridLineMesh(Transform parent, Material material)
    {
        const float halfW = 0.015f; // half of the 0.03 line width
        int vertCount = (GridWidth + 1) * GridHeight + GridWidth * (GridHeight + 1);
        Vector3[] verts = new Vector3[vertCount * 4];
        int[]     tris  = new int[vertCount * 6];
        int vi = 0, ti = 0;

        // vertical segments: run along Z at each X line
        for (int x = 0; x <= GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                float h = GetVerticalEdgeHeight(x, y) + 0.025f;
                int b = vi;
                verts[vi++] = new Vector3(x - halfW, h, y);
                verts[vi++] = new Vector3(x + halfW, h, y);
                verts[vi++] = new Vector3(x - halfW, h, y + 1f);
                verts[vi++] = new Vector3(x + halfW, h, y + 1f);
                tris[ti++] = b; tris[ti++] = b + 2; tris[ti++] = b + 1;
                tris[ti++] = b + 1; tris[ti++] = b + 2; tris[ti++] = b + 3;
            }
        }

        // horizontal segments: run along X at each Y line
        for (int y = 0; y <= GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                float h = GetHorizontalEdgeHeight(x, y) + 0.025f;
                int b = vi;
                verts[vi++] = new Vector3(x,       h, y - halfW);
                verts[vi++] = new Vector3(x,       h, y + halfW);
                verts[vi++] = new Vector3(x + 1f,  h, y - halfW);
                verts[vi++] = new Vector3(x + 1f,  h, y + halfW);
                tris[ti++] = b; tris[ti++] = b + 1; tris[ti++] = b + 2;
                tris[ti++] = b + 2; tris[ti++] = b + 1; tris[ti++] = b + 3;
            }
        }

        Mesh mesh = new() { name = "GridLinesMesh" };
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.UploadMeshData(true); // mark as non-readable for GPU memory savings

        GameObject obj = new("GridLinesMesh");
        obj.transform.SetParent(parent, false);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

}

