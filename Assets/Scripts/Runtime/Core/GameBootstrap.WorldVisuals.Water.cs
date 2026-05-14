using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
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

        // One gradient row before the sand shelf - gentle slope down into the beach.
        if (gradRow >= 0)
            for (int x = 0; x < GridWidth; x++)
                terrainHeights[x, gradRow] = Mathf.Min(terrainHeights[x, gradRow], 0.22f);
    }

    private void CreateWaterLayer()
    {
        waterShoreFoams.Clear();
        waterShoreWashPatches.Clear();
        riverFish.Clear();
        lakeFish.Clear();
        perLakeWaterCells.Clear();
        lakeFishRoot = null;
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
                washPatch.transform.localScale = new Vector3(segmentWidth, 0.008f, ring == 0 ? 1.02f : 0.86f);
                Renderer washRenderer = washPatch.GetComponent<Renderer>();
                float baseAlpha = ring == 0 ? 0.13f : ring == 1 ? 0.09f : 0.07f;
                washRenderer.sharedMaterial = CreateTransparentOverlayMaterial(
                    new Color(0.62f, 0.78f, 0.74f, baseAlpha),
                    riverFoamTexture,
                    new Vector2(1.15f + ring * 0.18f, 0.42f + ring * 0.12f),
                    new Vector2(segmentIndex * 0.17f, ring * 0.23f));
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
                    Depth = ring == 0 ? 1.08f : ring == 1 ? 0.86f : 0.52f,
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
            Renderer foamRenderer = foam.GetComponent<Renderer>();
            if (foamRenderer != null)
            {
                foamRenderer.sharedMaterial = CreateTransparentOverlayMaterial(
                    new Color(0.74f, 0.86f, 0.86f, 0.58f),
                    riverFoamTexture,
                    new Vector2(2.8f + i * 0.38f, 0.36f + i * 0.08f),
                    new Vector2(i * 0.21f, i * 0.09f));
            }
            ConfigureStaticVisual(foam);
            if (foam.TryGetComponent(out Collider foamCollider))
            {
                foamCollider.enabled = false;
            }

            float z = shoreRow + 0.12f + i * 0.06f;
            foam.transform.position = new Vector3(centerX, 0.236f + i * 0.0025f, z);

            waterShoreFoams.Add(new WaterShoreFoamData
            {
                RootTransform = foam.transform,
                Renderer = foamRenderer,
                Material = foamRenderer != null ? foamRenderer.sharedMaterial : null,
                BaseY = 0.236f + i * 0.0025f,
                BaseZ = z,
                Width = fullWidth,
                DriftSpeed = Random.Range(0.18f, 0.34f),
                DriftOffset = Random.Range(0f, 10f),
                PulseSpeed = Random.Range(0.7f, 1.2f),
                PhaseOffset = Random.Range(0f, 10f)
            });
        }

        CreateWaterShoreTransitionOverlays();
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

    private void SetupLakeFish()
    {
        lakeFish.Clear();
        perLakeWaterCells.Clear();
        if (lakeFishRoot != null)
        {
            for (int i = lakeFishRoot.childCount - 1; i >= 0; i--)
                Destroy(lakeFishRoot.GetChild(i).gameObject);
        }

        if (lakeWaterCells.Count < 4 || waterEffectsRoot == null) return;

        lakeFishRoot = new GameObject("LakeFish").transform;
        lakeFishRoot.SetParent(waterEffectsRoot, false);

        // Flood-fill lakeWaterCells into connected components (individual lakes).
        var allCells = new List<Vector2Int>(lakeWaterCells);
        var visited = new HashSet<Vector2Int>();
        var bfsQueue = new Queue<Vector2Int>();
        foreach (Vector2Int seed in allCells)
        {
            if (visited.Contains(seed)) continue;
            var component = new List<Vector2Int>();
            bfsQueue.Enqueue(seed);
            visited.Add(seed);
            while (bfsQueue.Count > 0)
            {
                Vector2Int cur = bfsQueue.Dequeue();
                component.Add(cur);
                Vector2Int[] neighbors = { cur + Vector2Int.right, cur + Vector2Int.left, cur + Vector2Int.up, cur + Vector2Int.down };
                foreach (Vector2Int nb in neighbors)
                {
                    if (!visited.Contains(nb) && lakeWaterCells.Contains(nb))
                    {
                        visited.Add(nb);
                        bfsQueue.Enqueue(nb);
                    }
                }
            }
            if (component.Count >= 2) perLakeWaterCells.Add(component);
        }

        for (int lakeIdx = 0; lakeIdx < perLakeWaterCells.Count; lakeIdx++)
        {
            List<Vector2Int> cells = perLakeWaterCells[lakeIdx];
            int fishForLake = Mathf.Clamp(cells.Count / 4, 1, LakeFishMaxCount);
            for (int i = 0; i < fishForLake; i++)
                CreateLakeFish(lakeIdx, cells);
        }
    }

    private void CreateLakeFish(int lakeIndex, List<Vector2Int> cells)
    {
        Vector2Int startCell = cells[Random.Range(0, cells.Count)];
        float startX = startCell.x + Random.Range(0.2f, 0.8f);
        float startZ = startCell.y + Random.Range(0.2f, 0.8f);
        float waterTop = GetCurrentVisualWaterHeight(startCell);
        float depthY = waterTop - Random.Range(0.04f, 0.08f);

        GameObject fishRoot = new($"LakeFish_{lakeFish.Count + 1}");
        fishRoot.transform.SetParent(lakeFishRoot, false);
        fishRoot.transform.position = new Vector3(startX, depthY, startZ);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(fishRoot.transform, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.09f, 0.15f, 0.07f);
        Color bodyColor = Color.Lerp(new Color(0.26f, 0.42f, 0.28f), new Color(0.42f, 0.58f, 0.38f), Random.value);
        ApplyColor(body, bodyColor);
        ConfigureStaticVisual(body);
        if (body.TryGetComponent(out Collider bodyCol)) bodyCol.enabled = false;

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tail.transform.SetParent(fishRoot.transform, false);
        tail.transform.localPosition = new Vector3(0f, 0f, -0.14f);
        tail.transform.localScale = new Vector3(0.08f, 0.065f, 0.018f);
        ApplyColor(tail, Color.Lerp(bodyColor * 0.9f, new Color(0.5f, 0.62f, 0.48f), 0.2f));
        ConfigureStaticVisual(tail);
        if (tail.TryGetComponent(out Collider tailCol)) tailCol.enabled = false;

        Renderer bodyRenderer = body.GetComponent<Renderer>();
        Renderer tailRenderer = tail.GetComponent<Renderer>();

        Vector2Int targetCell = cells[Random.Range(0, cells.Count)];
        lakeFish.Add(new LakeFishData
        {
            RootTransform = fishRoot.transform,
            BodyTransform = body.transform,
            TailTransform = tail.transform,
            BodyMaterial = bodyRenderer != null ? bodyRenderer.material : null,
            TailMaterial = tailRenderer != null ? tailRenderer.material : null,
            WorldX = startX,
            WorldZ = startZ,
            DepthY = depthY,
            BobPhase = Random.Range(0f, 10f),
            TailPhase = Random.Range(0f, 10f),
            IdleTimer = Random.Range(0.5f, 2f),
            TargetX = targetCell.x + 0.5f,
            TargetZ = targetCell.y + 0.5f,
            SwimSpeed = Random.Range(0.28f, 0.52f),
            BodyColor = bodyColor,
            LakeIndex = lakeIndex,
            Yaw = Random.Range(0f, 360f),
            JumpCooldown = Random.Range(5f, 18f),
        });
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


}
