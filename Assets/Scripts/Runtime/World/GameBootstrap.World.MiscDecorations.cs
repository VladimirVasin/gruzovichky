#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const string ImportedMiscTreeResourcePath = "Nature/Trees";
    private const float ImportedMiscTreeTargetLocalHeight = 1.46f;

    private bool RemoveMiscObjectAtCell(Vector2Int cell)
    {
        bool removed = miscOccupiedCells.Remove(cell);
        bool destroyedRoot = DestroyMiscRootByName($"MiscTree_{cell.x}_{cell.y}");
        destroyedRoot |= DestroyMiscRootByName($"BerryBush_{cell.x}_{cell.y}");
        destroyedRoot |= DestroyMiscRootByName($"FlowerPatch_{cell.x}_{cell.y}");

        if (lumberTrees.TryGetValue(cell, out LumberTreeRuntimeData lumberTree))
        {
            if (lumberTree.RootTransform != null)
            {
                Destroy(lumberTree.RootTransform.gameObject);
                destroyedRoot = true;
            }

            for (int i = 0; i < lumberTree.GroundLogs.Count; i++)
            {
                if (lumberTree.GroundLogs[i]?.RootObject != null)
                {
                    Destroy(lumberTree.GroundLogs[i].RootObject);
                }
            }

            lumberTrees.Remove(cell);
            removed = true;
        }

        for (int i = miscTreeSways.Count - 1; i >= 0; i--)
        {
            if (miscTreeSways[i].Cell == cell)
            {
                miscTreeSways.RemoveAt(i);
                removed = true;
            }
        }

        RemoveNearbyPointEntries(miscTreePerchPoints, cell, 2.5f);
        RemoveNearbyPointEntries(flowerBeePoints, cell, 1.6f);
        RemoveNearbyPointEntries(ambientSquirrelRoamPoints, cell, 1.6f);

        if (destroyedRoot)
        {
            removed = true;
        }

        if (removed)
        {
            SessionDebugLogger.Log("BUILD", $"Removed misc object at ({cell.x},{cell.y}) for construction.");
        }

        return removed;
    }

    private bool DestroyMiscRootByName(string objectName)
    {
        if (miscRoot == null)
        {
            return false;
        }

        Transform child = miscRoot.Find(objectName);
        if (child == null)
        {
            return false;
        }

        Destroy(child.gameObject);
        return true;
    }

    private static void RemoveNearbyPointEntries(System.Collections.Generic.List<Vector3> points, Vector2Int cell, float radius)
    {
        if (points == null || points.Count == 0)
        {
            return;
        }

        Vector3 center = new(cell.x + 0.5f, 0f, cell.y + 0.5f);
        float radiusSqr = radius * radius;
        for (int i = points.Count - 1; i >= 0; i--)
        {
            Vector3 delta = points[i] - center;
            delta.y = 0f;
            if (delta.sqrMagnitude <= radiusSqr)
            {
                points.RemoveAt(i);
            }
        }
    }

    private void CreateMiscTree(Vector2Int cell, int variantIndex)
    {
        if (miscRoot == null)
        {
            return;
        }

        GameObject treeRoot = new($"MiscTree_{cell.x}_{cell.y}");
        treeRoot.transform.SetParent(miscRoot, false);
        treeRoot.transform.position = GetCellCenter(cell);
        treeRoot.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        treeRoot.transform.localScale = Vector3.one * Random.Range(1.18f, 1.58f) * MiscTreeWorldScaleMultiplier;
        CreateMiscTreeShadowBlob(treeRoot.transform, variantIndex);
        if (!TryCreateImportedTreeVariant(treeRoot.transform, variantIndex))
        {
            CreateTreeVariant(treeRoot.transform, variantIndex);
        }

        RegisterLumberTree(cell, treeRoot.transform, variantIndex);
        RegisterMiscTreeSway(treeRoot.transform, cell, variantIndex);
        RegisterMiscTreePerchPoint(treeRoot.transform, cell, variantIndex);
        miscOccupiedCells.Add(cell);
    }

    private bool TryCreateImportedTreeVariant(Transform parent, int variantIndex)
    {
        GameObject prefab = GetImportedTreePrefab(variantIndex);
        if (prefab == null)
        {
            return false;
        }

        GameObject instance = Instantiate(prefab, parent, false);
        instance.name = $"ImportedTree_{prefab.name}";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        if (!TryGetTreeRendererBounds(instance, out Bounds initialBounds) || initialBounds.size.y <= 0.001f)
        {
            Destroy(instance);
            return false;
        }

        float targetWorldHeight = Mathf.Max(parent.lossyScale.y * ImportedMiscTreeTargetLocalHeight * TreeHeightScale, 0.35f);
        float scaleFactor = Mathf.Clamp(targetWorldHeight / initialBounds.size.y, 0.025f, 12f);
        instance.transform.localScale *= scaleFactor;
        AlignImportedTreeToRoot(parent, instance);
        ConfigureImportedTreeVisuals(instance);
        return true;
    }

    private GameObject GetImportedTreePrefab(int variantIndex)
    {
        GameObject[] prefabs = GetImportedTreePrefabs();
        if (prefabs.Length == 0)
        {
            return null;
        }

        return prefabs[System.Math.Abs(variantIndex % prefabs.Length)];
    }

    private GameObject[] GetImportedTreePrefabs()
    {
        if (hasLoadedImportedMiscTreePrefabs)
        {
            return importedMiscTreePrefabs ?? System.Array.Empty<GameObject>();
        }

        importedMiscTreePrefabs = Resources.LoadAll<GameObject>(ImportedMiscTreeResourcePath);
#if UNITY_EDITOR
        if (importedMiscTreePrefabs == null || importedMiscTreePrefabs.Length == 0)
        {
            AssetDatabase.Refresh();
            importedMiscTreePrefabs = LoadImportedTreePrefabsFromAssetDatabase();
        }
#endif
        hasLoadedImportedMiscTreePrefabs = true;
        if (importedMiscTreePrefabs != null && importedMiscTreePrefabs.Length > 0)
        {
            SessionDebugLogger.Log("WORLD", $"Loaded {importedMiscTreePrefabs.Length} imported tree assets from Resources/{ImportedMiscTreeResourcePath}.");
        }

        return importedMiscTreePrefabs ?? System.Array.Empty<GameObject>();
    }

#if UNITY_EDITOR
    private static GameObject[] LoadImportedTreePrefabsFromAssetDatabase()
    {
        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Resources/Nature/Trees" });
        if (guids == null || guids.Length == 0)
        {
            return System.Array.Empty<GameObject>();
        }

        System.Collections.Generic.List<string> assetPaths = new();
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!string.IsNullOrEmpty(path))
            {
                assetPaths.Add(path);
            }
        }

        assetPaths.Sort(System.StringComparer.Ordinal);
        System.Collections.Generic.List<GameObject> prefabs = new();
        for (int i = 0; i < assetPaths.Count; i++)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPaths[i]);
            if (prefab != null)
            {
                prefabs.Add(prefab);
            }
        }

        return prefabs.ToArray();
    }
#endif

    private static bool TryGetTreeRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        if (root == null)
        {
            return false;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private static void AlignImportedTreeToRoot(Transform parent, GameObject instance)
    {
        if (parent == null || instance == null || !TryGetTreeRendererBounds(instance, out Bounds bounds))
        {
            return;
        }

        Vector3 desiredCenter = parent.position;
        Vector3 delta = new(
            desiredCenter.x - bounds.center.x,
            parent.position.y - bounds.min.y,
            desiredCenter.z - bounds.center.z);
        instance.transform.position += delta;
    }

    private void ConfigureImportedTreeVisuals(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            ApplyImportedTreeMaterialTone(renderer);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            RegisterShadowLodRenderer(renderer);
        }
    }

    private static void ApplyImportedTreeMaterialTone(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        string materialName = renderer.sharedMaterial != null ? renderer.sharedMaterial.name.ToLowerInvariant() : string.Empty;
        string rendererName = renderer.name.ToLowerInvariant();
        bool isLeaf = materialName.Contains("leaf") ||
                      rendererName.Contains("leaf") ||
                      rendererName.Contains("crown") ||
                      rendererName.Contains("needle");

        float seed = Mathf.Abs(renderer.transform.position.x * 13.17f + renderer.transform.position.z * 41.91f + rendererName.Length * 0.37f);
        float t = Mathf.Repeat(Mathf.Sin(seed) * 43758.5453f, 1f);
        Color tone = isLeaf
            ? Color.Lerp(new Color(0.70f, 0.82f, 0.60f), new Color(0.58f, 0.75f, 0.66f), t)
            : Color.Lerp(new Color(0.82f, 0.74f, 0.62f), new Color(0.68f, 0.62f, 0.54f), t);

        MaterialPropertyBlock block = new();
        renderer.GetPropertyBlock(block);
        block.SetColor("_BaseColor", tone);
        block.SetColor("_Color", tone);
        renderer.SetPropertyBlock(block);
    }

    private void CreateMiscTreeShadowBlob(Transform parent, int variantIndex)
    {
        GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shadow.name = "TreeContactShadow";
        shadow.transform.SetParent(parent, false);

        int variant = Mathf.Abs(variantIndex) % 3;
        Vector3 scale = variant switch
        {
            0 => new Vector3(0.82f, 0.01f, 0.66f),
            1 => new Vector3(0.92f, 0.01f, 0.84f),
            _ => new Vector3(0.62f, 0.01f, 0.72f)
        };

        shadow.transform.localPosition = new Vector3(0.08f, 0.012f, -0.06f);
        shadow.transform.localRotation = Quaternion.Euler(0f, 28f + variantIndex * 17f, 0f);
        shadow.transform.localScale = scale;

        Renderer renderer = shadow.GetComponent<Renderer>();
        renderer.material = CreateTransparentOverlayMaterial(new Color(0f, 0f, 0f, 0.18f));
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (shadow.TryGetComponent(out Collider collider))
        {
            Object.Destroy(collider);
        }
    }

    private void CreateBerryBush(Vector2Int cell, int variantSeed)
    {
        if (miscRoot == null)
        {
            return;
        }

        GameObject bushRoot = new($"BerryBush_{cell.x}_{cell.y}");
        bushRoot.transform.SetParent(miscRoot, false);
        bushRoot.transform.position = GetCellCenter(cell);
        bushRoot.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        bushRoot.transform.localScale = Vector3.one * Random.Range(0.78f, 1.02f);

        Color leafDark = new Color(0.16f, 0.42f, 0.2f);
        Color leafLight = new Color(0.22f, 0.52f, 0.26f);
        Color berryColor = new Color(0.9f, 0.08f, 0.12f);
        Color berryHighlight = new Color(0.98f, 0.2f, 0.24f);

        Vector3[] clumpPositions =
        {
            new Vector3(-0.12f, 0.18f, -0.02f),
            new Vector3(0.14f, 0.22f, 0.04f),
            new Vector3(0.02f, 0.25f, -0.14f)
        };

        Vector3[] clumpScales =
        {
            new Vector3(0.32f, 0.24f, 0.3f),
            new Vector3(0.36f, 0.28f, 0.32f),
            new Vector3(0.28f, 0.22f, 0.26f)
        };

        for (int i = 0; i < clumpPositions.Length; i++)
        {
            GameObject clump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            clump.transform.SetParent(bushRoot.transform, false);
            clump.transform.localPosition = clumpPositions[i];
            clump.transform.localScale = clumpScales[i];
            ApplyColor(clump, i % 2 == 0 ? leafLight : leafDark);
            ConfigureStaticVisual(clump);
        }

        for (int i = 0; i < 12; i++)
        {
            float angle = (i / 12f) * Mathf.PI * 2f + variantSeed * 0.37f;
            float radius = 0.08f + (i % 3) * 0.035f;
            GameObject berry = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            berry.transform.SetParent(bushRoot.transform, false);
            berry.transform.localPosition = new Vector3(
                Mathf.Cos(angle) * radius,
                0.13f + (i % 4) * 0.045f,
                Mathf.Sin(angle) * radius * 0.88f);
            berry.transform.localScale = new Vector3(0.085f, 0.085f, 0.085f);
            ApplyColor(berry, i % 3 == 0 ? berryHighlight : berryColor);
            ConfigureStaticVisual(berry);
        }

        miscOccupiedCells.Add(cell);
    }

    private void CreateFlowerPatch(Vector2Int cell, int variantSeed)
    {
        if (miscRoot == null)
        {
            return;
        }

        GameObject patchRoot = new($"FlowerPatch_{cell.x}_{cell.y}");
        patchRoot.transform.SetParent(miscRoot, false);
        patchRoot.transform.position = GetCellCenter(cell);
        patchRoot.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        patchRoot.transform.localScale = Vector3.one * Random.Range(0.82f, 1.08f);

        Color stemColor = new Color(0.2f, 0.5f, 0.24f);
        Color[] petalColors =
        {
            new Color(0.94f, 0.88f, 0.24f),
            new Color(0.96f, 0.62f, 0.22f),
            new Color(0.92f, 0.48f, 0.58f),
            new Color(0.86f, 0.78f, 0.96f)
        };

        int flowerCount = 4 + (variantSeed % 3);
        for (int i = 0; i < flowerCount; i++)
        {
            float angle = (i / Mathf.Max(1f, flowerCount)) * Mathf.PI * 2f + variantSeed * 0.31f;
            float radius = 0.08f + (i % 2) * 0.06f;
            Vector3 baseOffset = new Vector3(
                Mathf.Cos(angle) * radius,
                0.04f,
                Mathf.Sin(angle) * radius);

            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(patchRoot.transform, false);
            stem.transform.localPosition = baseOffset + new Vector3(0f, 0.08f, 0f);
            stem.transform.localScale = new Vector3(0.018f, 0.08f, 0.018f);
            ApplyColor(stem, stemColor);
            ConfigureStaticVisual(stem);

            GameObject bloom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bloom.transform.SetParent(patchRoot.transform, false);
            bloom.transform.localPosition = baseOffset + new Vector3(0f, 0.17f + (i % 2) * 0.015f, 0f);
            bloom.transform.localScale = new Vector3(0.08f, 0.035f, 0.08f);
            ApplyColor(bloom, petalColors[(variantSeed + i) % petalColors.Length]);
            ConfigureStaticVisual(bloom);
        }

        GameObject grassClump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        grassClump.transform.SetParent(patchRoot.transform, false);
        grassClump.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        grassClump.transform.localScale = new Vector3(0.28f, 0.08f, 0.24f);
        ApplyColor(grassClump, new Color(0.24f, 0.56f, 0.28f));
        ConfigureStaticVisual(grassClump);

        flowerBeePoints.Add(patchRoot.transform.position + new Vector3(0f, 0.08f, 0f));
        miscOccupiedCells.Add(cell);
    }

    private void RegisterMiscTreeSway(Transform treeRoot, Vector2Int cell, int variantIndex)
    {
        if (treeRoot == null)
        {
            return;
        }

        miscTreeSways.Add(new MiscTreeSway
        {
            Cell = cell,
            RootTransform = treeRoot,
            BaseRotation = treeRoot.localRotation,
            PhaseOffset = (cell.x * 0.73f) + (cell.y * 1.17f) + variantIndex * 0.41f,
            SecondaryPhaseOffset = (cell.x * 1.31f) + (cell.y * 0.57f) + variantIndex * 0.88f,
            Speed = 0.55f + ((cell.x + cell.y + variantIndex) % 5) * 0.06f,
            PitchAmplitude = 1.2f + ((cell.x + variantIndex) % 4) * 0.18f,
            RollAmplitude = 0.9f + ((cell.y + variantIndex) % 4) * 0.14f
        });
    }

    private void RegisterMiscTreePerchPoint(Transform treeRoot, Vector2Int cell, int variantIndex)
    {
        if (treeRoot == null)
        {
            return;
        }

        float canopyHeight = treeRoot.localScale.y * Random.Range(0.76f, 0.9f);
        Vector3 perchOffset = new(
            (((cell.x + variantIndex) % 3) - 1) * 0.05f,
            canopyHeight,
            (((cell.y + variantIndex * 2) % 3) - 1) * 0.05f);
        miscTreePerchPoints.Add(treeRoot.position + perchOffset);
    }

    private void UpdateMiscTreeSways()
    {
        if (miscTreeSways.Count == 0)
        {
            return;
        }

        float time = Time.time;
        for (int i = miscTreeSways.Count - 1; i >= 0; i--)
        {
            MiscTreeSway sway = miscTreeSways[i];
            if (sway.RootTransform == null)
            {
                miscTreeSways.RemoveAt(i);
                continue;
            }

            if (lumberTrees.TryGetValue(sway.Cell, out LumberTreeRuntimeData lumberTree) &&
                (lumberTree.State == LumberTreeRuntimeState.Falling ||
                 lumberTree.State == LumberTreeRuntimeState.Felled))
            {
                continue;
            }

            float primary = Mathf.Sin(time * sway.Speed + sway.PhaseOffset);
            float secondary = Mathf.Sin(time * (sway.Speed * 0.63f) + sway.SecondaryPhaseOffset);
            float pitch = primary * sway.PitchAmplitude * sway.CurrentWindMult;
            float roll = secondary * sway.RollAmplitude * sway.CurrentWindMult;
            sway.RootTransform.localRotation = sway.BaseRotation * Quaternion.Euler(pitch, 0f, roll);
        }
    }

    private void CreateTreeVariant(Transform parent, int variantIndex)
    {
        int variant = Mathf.Abs(variantIndex) % 3;
        switch (variant)
        {
            case 0:
                CreateMiscTreeTall(parent);
                break;
            case 1:
                CreateMiscTreeRound(parent);
                break;
            default:
                CreateMiscTreePine(parent);
                break;
        }
    }

    private void CreateMiscTreeTall(Transform parent)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(parent, false);
        trunk.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.34f, 0f));
        trunk.transform.localScale = ScaleTreeLocalScale(new Vector3(0.12f, 0.34f, 0.12f));
        ApplyColor(trunk, new Color(0.44f, 0.28f, 0.16f));
        ConfigureShadowVisual(trunk);

        GameObject crownBottom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crownBottom.transform.SetParent(parent, false);
        crownBottom.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.9f, 0f));
        crownBottom.transform.localScale = ScaleTreeLocalScale(new Vector3(0.62f, 0.42f, 0.62f));
        ApplyColor(crownBottom, JitterTreeLeafColor(new Color(0.22f, 0.56f, 0.27f), parent));
        ConfigureShadowVisual(crownBottom);

        GameObject crownShade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crownShade.transform.SetParent(parent, false);
        crownShade.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.72f, 0.02f));
        crownShade.transform.localScale = ScaleTreeLocalScale(new Vector3(0.48f, 0.18f, 0.5f));
        ApplyColor(crownShade, new Color(0.12f, 0.32f, 0.16f));
        ConfigureShadowVisual(crownShade);

        GameObject crownTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crownTop.transform.SetParent(parent, false);
        crownTop.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 1.16f, 0f));
        crownTop.transform.localScale = ScaleTreeLocalScale(new Vector3(0.44f, 0.34f, 0.44f));
        ApplyColor(crownTop, JitterTreeLeafColor(new Color(0.18f, 0.5f, 0.24f), parent));
        ConfigureShadowVisual(crownTop);
    }

    private void CreateMiscTreeRound(Transform parent)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(parent, false);
        trunk.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.28f, 0f));
        trunk.transform.localScale = ScaleTreeLocalScale(new Vector3(0.11f, 0.28f, 0.11f));
        ApplyColor(trunk, new Color(0.42f, 0.25f, 0.15f));
        ConfigureShadowVisual(trunk);

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.transform.SetParent(parent, false);
        canopy.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.84f, 0f));
        canopy.transform.localScale = ScaleTreeLocalScale(new Vector3(0.72f, 0.66f, 0.72f));
        ApplyColor(canopy, JitterTreeLeafColor(new Color(0.3f, 0.62f, 0.31f), parent));
        ConfigureShadowVisual(canopy);

        GameObject canopyShade = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopyShade.transform.SetParent(parent, false);
        canopyShade.transform.localPosition = ScaleTreeLocalPosition(new Vector3(-0.02f, 0.62f, 0.04f));
        canopyShade.transform.localScale = ScaleTreeLocalScale(new Vector3(0.56f, 0.22f, 0.58f));
        ApplyColor(canopyShade, new Color(0.15f, 0.36f, 0.18f));
        ConfigureShadowVisual(canopyShade);

        GameObject sideBlob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sideBlob.transform.SetParent(parent, false);
        sideBlob.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0.18f, 0.78f, -0.1f));
        sideBlob.transform.localScale = ScaleTreeLocalScale(new Vector3(0.34f, 0.28f, 0.34f));
        ApplyColor(sideBlob, JitterTreeLeafColor(new Color(0.24f, 0.56f, 0.28f), parent));
        ConfigureShadowVisual(sideBlob);
    }

    private void CreateMiscTreePine(Transform parent)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(parent, false);
        trunk.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.24f, 0f));
        trunk.transform.localScale = ScaleTreeLocalScale(new Vector3(0.1f, 0.24f, 0.1f));
        ApplyColor(trunk, new Color(0.4f, 0.24f, 0.14f));
        ConfigureShadowVisual(trunk);

        GameObject lower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lower.transform.SetParent(parent, false);
        lower.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.7f, 0f));
        lower.transform.localScale = ScaleTreeLocalScale(new Vector3(0.36f, 0.24f, 0.36f));
        ApplyColor(lower, JitterTreeLeafColor(new Color(0.16f, 0.44f, 0.23f), parent));
        ConfigureShadowVisual(lower);

        GameObject lowerShade = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lowerShade.transform.SetParent(parent, false);
        lowerShade.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.54f, 0.02f));
        lowerShade.transform.localScale = ScaleTreeLocalScale(new Vector3(0.31f, 0.08f, 0.31f));
        ApplyColor(lowerShade, new Color(0.08f, 0.25f, 0.13f));
        ConfigureShadowVisual(lowerShade);

        GameObject upper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        upper.transform.SetParent(parent, false);
        upper.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 1.05f, 0f));
        upper.transform.localScale = ScaleTreeLocalScale(new Vector3(0.24f, 0.22f, 0.24f));
        ApplyColor(upper, JitterTreeLeafColor(new Color(0.12f, 0.36f, 0.2f), parent));
        ConfigureShadowVisual(upper);
    }

    private void PopulateLakeDecorations()
    {
        if (miscRoot == null || lakeWaterCells.Count == 0) return;

        // Water lilies on lake perimeter cells (shallow edges).
        foreach (Vector2Int cell in lakeWaterCells)
        {
            // Only cells adjacent to at least one non-lake cell (edge of lake).
            bool isEdge = !lakeWaterCells.Contains(cell + Vector2Int.right)
                       || !lakeWaterCells.Contains(cell + Vector2Int.left)
                       || !lakeWaterCells.Contains(cell + Vector2Int.up)
                       || !lakeWaterCells.Contains(cell + Vector2Int.down);
            if (!isEdge) continue;

            // Sparse: deterministic skip based on cell hash.
            if ((cell.x * 7 + cell.y * 13) % 4 != 0) continue;

            CreateWaterLily(cell);
        }

        // Shoreline detail pass: sparse reeds and pebbles soften hard water edges.
        HashSet<Vector2Int> shorelineCells = new(naturalBeachCells);
        int shoreRow = GridHeight - WaterRiverWidth;
        for (int x = 0; x < GridWidth; x++)
        {
            shorelineCells.Add(new Vector2Int(x, shoreRow - 1));
            shorelineCells.Add(new Vector2Int(x, shoreRow - 2));
        }

        foreach (Vector2Int cell in shorelineCells)
        {
            if (!IsInsideGrid(cell) || waterCells.Contains(cell) || miscOccupiedCells.Contains(cell))
            {
                continue;
            }

            int hash = GetShorelineDecorationHash(cell);
            if (hash % 5 == 0)
            {
                CreateReedCluster(cell);
            }
            else if (hash % 7 == 0)
            {
                CreateShorePebbleCluster(cell);
            }
        }
    }

    private void CreateWaterLily(Vector2Int cell)
    {
        if (miscRoot == null) return;

        float waterY = GetCurrentVisualWaterHeight(cell);
        float x = cell.x + Random.Range(0.15f, 0.85f);
        float z = cell.y + Random.Range(0.15f, 0.85f);

        GameObject lilyRoot = new($"WaterLily_{cell.x}_{cell.y}");
        lilyRoot.transform.SetParent(miscRoot, false);
        lilyRoot.transform.position = new Vector3(x, waterY + 0.01f, z);
        lilyRoot.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        // Pad (leaf) — flat disk
        float padSize = Random.Range(0.22f, 0.32f);
        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.transform.SetParent(lilyRoot.transform, false);
        pad.transform.localPosition = Vector3.zero;
        pad.transform.localScale = new Vector3(padSize, 0.012f, padSize);
        ApplyColor(pad, new Color(0.22f, 0.50f, 0.24f));
        ConfigureStaticVisual(pad);
        if (pad.TryGetComponent(out Collider pc)) pc.enabled = false;

        // Flower — only on ~60% of lilies
        if (Random.value < 0.6f)
        {
            Color flowerColor = Random.value < 0.5f
                ? new Color(0.97f, 0.94f, 0.90f)   // white
                : new Color(0.96f, 0.68f, 0.78f);   // pink

            GameObject flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flower.transform.SetParent(lilyRoot.transform, false);
            flower.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            flower.transform.localScale = new Vector3(0.09f, 0.055f, 0.09f);
            ApplyColor(flower, flowerColor);
            ConfigureStaticVisual(flower);
            if (flower.TryGetComponent(out Collider fc)) fc.enabled = false;

            // Yellow center
            GameObject center = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            center.transform.SetParent(lilyRoot.transform, false);
            center.transform.localPosition = new Vector3(0f, 0.042f, 0f);
            center.transform.localScale = new Vector3(0.038f, 0.028f, 0.038f);
            ApplyColor(center, new Color(0.98f, 0.88f, 0.22f));
            ConfigureStaticVisual(center);
            if (center.TryGetComponent(out Collider cc)) cc.enabled = false;
        }
    }

    private void CreateReedCluster(Vector2Int cell)
    {
        if (miscRoot == null) return;

        float groundY = SampleTerrainHeight(cell.x + 0.5f, cell.y + 0.5f);
        int stemCount = Random.Range(2, 5);

        GameObject clusterRoot = new($"Reed_{cell.x}_{cell.y}");
        clusterRoot.transform.SetParent(miscRoot, false);
        clusterRoot.transform.position = new Vector3(cell.x + 0.5f, groundY, cell.y + 0.5f);

        for (int i = 0; i < stemCount; i++)
        {
            float ox = Random.Range(-0.22f, 0.22f);
            float oz = Random.Range(-0.22f, 0.22f);
            float height = Random.Range(0.5f, 0.85f);
            float lean = Random.Range(-6f, 6f);

            // Stem
            GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            stem.transform.SetParent(clusterRoot.transform, false);
            stem.transform.localPosition = new Vector3(ox, height * 0.5f, oz);
            stem.transform.localRotation = Quaternion.Euler(lean, Random.Range(0f, 360f), 0f);
            stem.transform.localScale = new Vector3(0.022f, height * 0.5f, 0.022f);
            ApplyColor(stem, new Color(0.30f, 0.52f, 0.26f));
            ConfigureStaticVisual(stem);
            if (stem.TryGetComponent(out Collider sc)) sc.enabled = false;

            // Cattail head — dark brown oval
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            head.transform.SetParent(clusterRoot.transform, false);
            head.transform.localPosition = new Vector3(ox, height + 0.05f, oz);
            head.transform.localRotation = stem.transform.localRotation;
            head.transform.localScale = new Vector3(0.042f, 0.065f, 0.042f);
            ApplyColor(head, new Color(0.32f, 0.18f, 0.08f));
            ConfigureStaticVisual(head);
            if (head.TryGetComponent(out Collider hc)) hc.enabled = false;
        }

        // Register for gentle wind sway (reuses tree sway system).
        miscTreeSways.Add(new MiscTreeSway
        {
            Cell                  = cell,
            RootTransform         = clusterRoot.transform,
            BaseRotation          = clusterRoot.transform.localRotation,
            PhaseOffset           = cell.x * 0.83f + cell.y * 1.29f,
            SecondaryPhaseOffset  = cell.x * 1.47f + cell.y * 0.61f,
            Speed                 = 0.65f + (cell.x % 5) * 0.05f,
            PitchAmplitude        = 1.4f,
            RollAmplitude         = 0.9f,
        });
    }

    private void CreateShorePebbleCluster(Vector2Int cell)
    {
        if (miscRoot == null)
        {
            return;
        }

        float groundY = SampleTerrainHeight(cell.x + 0.5f, cell.y + 0.5f);
        int pebbleCount = 2 + PositiveMod(cell.x * 5 + cell.y * 3, 4);
        GameObject clusterRoot = new($"ShorePebbles_{cell.x}_{cell.y}");
        clusterRoot.transform.SetParent(miscRoot, false);
        clusterRoot.transform.position = new Vector3(cell.x + 0.5f, groundY + 0.012f, cell.y + 0.5f);
        clusterRoot.transform.rotation = Quaternion.Euler(0f, PositiveMod(cell.x * 37 + cell.y * 19, 360), 0f);

        for (int i = 0; i < pebbleCount; i++)
        {
            float ox = Mathf.Lerp(-0.28f, 0.28f, Mathf.Repeat(Mathf.Sin((cell.x + i * 11) * 14.13f) * 43758.5453f, 1f));
            float oz = Mathf.Lerp(-0.24f, 0.24f, Mathf.Repeat(Mathf.Sin((cell.y + i * 7) * 21.71f) * 24634.6345f, 1f));
            float size = Mathf.Lerp(0.08f, 0.16f, Mathf.Repeat(Mathf.Sin((cell.x + cell.y + i) * 9.31f) * 15342.521f, 1f));

            GameObject pebble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pebble.transform.SetParent(clusterRoot.transform, false);
            pebble.transform.localPosition = new Vector3(ox, size * 0.15f, oz);
            pebble.transform.localScale = new Vector3(size * 1.35f, size * 0.28f, size);
            Color color = Color.Lerp(new Color(0.42f, 0.43f, 0.39f), new Color(0.62f, 0.58f, 0.48f), i / Mathf.Max(1f, pebbleCount - 1f));
            ApplyColor(pebble, color);
            ConfigureStaticVisual(pebble);
            if (pebble.TryGetComponent(out Collider collider))
            {
                collider.enabled = false;
            }
        }
    }

    private static int GetShorelineDecorationHash(Vector2Int cell)
    {
        unchecked
        {
            int hash = cell.x * 73856093 ^ cell.y * 19349663 ^ 0x5A17;
            return hash & int.MaxValue;
        }
    }

    private static Color JitterTreeLeafColor(Color baseColor, Transform parent)
    {
        float seed = parent != null ? Mathf.Abs(parent.position.x * 12.9898f + parent.position.z * 78.233f) : Random.value;
        float t = Mathf.Repeat(Mathf.Sin(seed) * 43758.5453f, 1f);
        float brightness = Mathf.Lerp(0.80f, 1.02f, t);
        Color huePush = Color.Lerp(new Color(0.84f, 0.94f, 0.76f), new Color(0.68f, 0.86f, 0.80f), Mathf.Repeat(t * 1.7f, 1f));
        return new Color(
            Mathf.Clamp01(baseColor.r * brightness * huePush.r),
            Mathf.Clamp01(baseColor.g * brightness * huePush.g),
            Mathf.Clamp01(baseColor.b * brightness * huePush.b),
            baseColor.a);
    }

    private static Vector3 ScaleTreeLocalPosition(Vector3 source)
    {
        source.y *= TreeHeightScale;
        return source;
    }

    private static Vector3 ScaleTreeLocalScale(Vector3 source)
    {
        source.y *= TreeHeightScale;
        return source;
    }
}
