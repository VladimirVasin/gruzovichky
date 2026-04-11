using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private const float TreeHeightScale = 1.1f;
    private const float BerryBushSpawnChance = 0.28f;
    private const float FlowerPatchSpawnChance = 0.18f;

    private void SetupLocations()
    {
        locations.Clear();

        GeneratedWorldLayout layout = WorldLayoutGenerator.Generate(GridWidth, GridHeight, waterCells, HasRequiredLayoutRoads);
        CreateLocation(LocationType.Parking, "Parking", layout.Parking.Min, layout.Parking.Max, layout.Parking.Anchor, new Color(0.46f, 0.46f, 0.52f));
        CreateLocation(LocationType.GasStation, "Gas Station", layout.GasStation.Min, layout.GasStation.Max, layout.GasStation.Anchor, new Color(0.84f, 0.68f, 0.26f));
        CreateLocation(LocationType.Forest, "Forest", layout.Forest.Min, layout.Forest.Max, layout.Forest.Anchor, new Color(0.22f, 0.55f, 0.24f));
        CreateLocation(LocationType.Warehouse, "Warehouse", layout.Warehouse.Min, layout.Warehouse.Max, layout.Warehouse.Anchor, new Color(0.7f, 0.52f, 0.3f));
        CreateLocation(LocationType.Sawmill, "Sawmill", layout.Sawmill.Min, layout.Sawmill.Max, layout.Sawmill.Anchor, new Color(0.3f, 0.52f, 0.8f));
        CreateLocation(LocationType.Motel, "Motel", layout.Motel.Min, layout.Motel.Max, layout.Motel.Anchor, new Color(0.91f, 0.87f, 0.74f));
        CreateLocation(LocationType.BusStop, "Bus Stop", layout.BusStop.Min, layout.BusStop.Max, layout.BusStop.Anchor, new Color(0.82f, 0.24f, 0.22f));

        SessionDebugLogger.Log(
            "WORLD",
            $"Generated layout: Parking {FormatPlacement(layout.Parking)}, GasStation {FormatPlacement(layout.GasStation)}, Forest {FormatPlacement(layout.Forest)}, Warehouse {FormatPlacement(layout.Warehouse)}, Sawmill {FormatPlacement(layout.Sawmill)}, Motel {FormatPlacement(layout.Motel)}, BusStop {FormatPlacement(layout.BusStop)}.");
    }

    private bool HasRequiredLayoutRoads(GeneratedWorldLayout layout)
    {
        return FindRoadBuildPath(layout.Parking.Anchor, layout.GasStation.Anchor, cell => IsPlacementCell(layout, cell)) != null &&
               FindRoadBuildPath(layout.GasStation.Anchor, layout.Warehouse.Anchor, cell => IsPlacementCell(layout, cell)) != null &&
               FindRoadBuildPath(layout.Warehouse.Anchor, layout.Forest.Anchor, cell => IsPlacementCell(layout, cell)) != null &&
               FindRoadBuildPath(layout.Forest.Anchor, layout.Sawmill.Anchor, cell => IsPlacementCell(layout, cell)) != null &&
               FindRoadBuildPath(layout.Sawmill.Anchor, layout.Warehouse.Anchor, cell => IsPlacementCell(layout, cell)) != null &&
               FindRoadBuildPath(layout.Warehouse.Anchor, layout.Motel.Anchor, cell => IsPlacementCell(layout, cell)) != null &&
               FindRoadBuildPath(layout.Motel.Anchor, layout.BusStop.Anchor, cell => IsPlacementCell(layout, cell)) != null;
    }

    private static bool IsPlacementCell(GeneratedWorldLayout layout, Vector2Int cell)
    {
        foreach (WorldLocationPlacement placement in layout.GetAllPlacements())
        {
            if (placement.Anchor == cell || placement.Contains(cell))
            {
                return true;
            }
        }

        return false;
    }

    private void GenerateTerrainHeights()
    {
        terrainHeights = TerrainHeightGenerator.Generate(GridWidth, GridHeight, GetWorldPlacements());
    }

    private IEnumerable<WorldLocationPlacement> GetWorldPlacements()
    {
        foreach (LocationData location in locations.Values)
        {
            yield return new WorldLocationPlacement
            {
                Min = location.Min,
                Max = location.Max,
                Anchor = location.Anchor
            };
        }
    }

    private void ApplyTerrainHeightsToWorld()
    {
        foreach (KeyValuePair<LocationType, LocationData> pair in locations)
        {
            if (pair.Value.RootObject != null)
            {
                pair.Value.RootObject.transform.position = new Vector3(0f, GetLocationBaseHeight(pair.Key), 0f);
            }
        }

        foreach (KeyValuePair<Vector2Int, GameObject> pair in roadVisuals)
        {
            if (pair.Value != null)
            {
                pair.Value.transform.position = GetCellCenter(pair.Key) + new Vector3(0f, RoadHeight, 0f);
            }
        }
    }

    private void PopulateMiscTrees()
    {
        if (miscRoot == null)
        {
            return;
        }

        miscOccupiedCells.Clear();
        miscTreeSways.Clear();
        miscTreePerchPoints.Clear();
        flowerBeePoints.Clear();
        List<Vector2Int> plannedCells = MiscTreePlanner.Plan(
            GridWidth,
            GridHeight,
            roadCells,
            edgeHighwayCells,
            IsLocationCell,
            cell => IsGrassGroundCell(cell.x, cell.y) && !IsWaterOrBeachCell(cell));
        int bushCount = 0;
        int flowerCount = 0;
        SessionDebugLogger.Log("WORLD", $"Planning {plannedCells.Count} misc cells");
        for (int i = 0; i < plannedCells.Count; i++)
        {
            float roll = Random.value;
            if (roll < FlowerPatchSpawnChance)
            {
                CreateFlowerPatch(plannedCells[i], i);
                flowerCount++;
            }
            else if (roll < FlowerPatchSpawnChance + BerryBushSpawnChance)
            {
                CreateBerryBush(plannedCells[i], i);
                bushCount++;
            }
            else
            {
                CreateMiscTree(plannedCells[i], i % 6);
            }
        }

        SessionDebugLogger.Log("WORLD", $"Placed {plannedCells.Count} misc props ({plannedCells.Count - bushCount - flowerCount} trees, {bushCount} berry bushes, {flowerCount} flower patches).");
    }

    private static string FormatPlacement(WorldLocationPlacement placement)
    {
        return $"min({placement.Min.x},{placement.Min.y}) max({placement.Max.x},{placement.Max.y}) anchor({placement.Anchor.x},{placement.Anchor.y})";
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
        treeRoot.transform.localScale = Vector3.one * Random.Range(1.18f, 1.58f);
        CreateTreeVariant(treeRoot.transform, variantIndex);
        RegisterMiscTreeSway(treeRoot.transform, cell, variantIndex);
        RegisterMiscTreePerchPoint(treeRoot.transform, cell, variantIndex);
        miscOccupiedCells.Add(cell);
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

            float primary = Mathf.Sin(time * sway.Speed + sway.PhaseOffset);
            float secondary = Mathf.Sin(time * (sway.Speed * 0.63f) + sway.SecondaryPhaseOffset);
            float pitch = primary * sway.PitchAmplitude;
            float roll = secondary * sway.RollAmplitude;
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
        ConfigureStaticVisual(trunk);

        GameObject crownBottom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crownBottom.transform.SetParent(parent, false);
        crownBottom.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.9f, 0f));
        crownBottom.transform.localScale = ScaleTreeLocalScale(new Vector3(0.62f, 0.42f, 0.62f));
        ApplyColor(crownBottom, new Color(0.22f, 0.56f, 0.27f));
        ConfigureStaticVisual(crownBottom);

        GameObject crownTop = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crownTop.transform.SetParent(parent, false);
        crownTop.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 1.16f, 0f));
        crownTop.transform.localScale = ScaleTreeLocalScale(new Vector3(0.44f, 0.34f, 0.44f));
        ApplyColor(crownTop, new Color(0.18f, 0.5f, 0.24f));
        ConfigureStaticVisual(crownTop);
    }

    private void CreateMiscTreeRound(Transform parent)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(parent, false);
        trunk.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.28f, 0f));
        trunk.transform.localScale = ScaleTreeLocalScale(new Vector3(0.11f, 0.28f, 0.11f));
        ApplyColor(trunk, new Color(0.42f, 0.25f, 0.15f));
        ConfigureStaticVisual(trunk);

        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.transform.SetParent(parent, false);
        canopy.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.84f, 0f));
        canopy.transform.localScale = ScaleTreeLocalScale(new Vector3(0.72f, 0.66f, 0.72f));
        ApplyColor(canopy, new Color(0.3f, 0.62f, 0.31f));
        ConfigureStaticVisual(canopy);

        GameObject sideBlob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sideBlob.transform.SetParent(parent, false);
        sideBlob.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0.18f, 0.78f, -0.1f));
        sideBlob.transform.localScale = ScaleTreeLocalScale(new Vector3(0.34f, 0.28f, 0.34f));
        ApplyColor(sideBlob, new Color(0.24f, 0.56f, 0.28f));
        ConfigureStaticVisual(sideBlob);
    }

    private void CreateMiscTreePine(Transform parent)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.transform.SetParent(parent, false);
        trunk.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.24f, 0f));
        trunk.transform.localScale = ScaleTreeLocalScale(new Vector3(0.1f, 0.24f, 0.1f));
        ApplyColor(trunk, new Color(0.4f, 0.24f, 0.14f));
        ConfigureStaticVisual(trunk);

        GameObject lower = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lower.transform.SetParent(parent, false);
        lower.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 0.7f, 0f));
        lower.transform.localScale = ScaleTreeLocalScale(new Vector3(0.36f, 0.24f, 0.36f));
        ApplyColor(lower, new Color(0.16f, 0.44f, 0.23f));
        ConfigureStaticVisual(lower);

        GameObject upper = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        upper.transform.SetParent(parent, false);
        upper.transform.localPosition = ScaleTreeLocalPosition(new Vector3(0f, 1.05f, 0f));
        upper.transform.localScale = ScaleTreeLocalScale(new Vector3(0.24f, 0.22f, 0.24f));
        ApplyColor(upper, new Color(0.12f, 0.36f, 0.2f));
        ConfigureStaticVisual(upper);
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
