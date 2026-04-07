using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private const float TreeHeightScale = 1.1f;

    private void SetupLocations()
    {
        locations.Clear();

        GeneratedWorldLayout layout = WorldLayoutGenerator.Generate(GridWidth, GridHeight, HasRequiredLayoutRoads);
        CreateLocation(LocationType.Parking, "Parking", layout.Parking.Min, layout.Parking.Max, layout.Parking.Anchor, new Color(0.46f, 0.46f, 0.52f));
        CreateLocation(LocationType.GasStation, "Gas Station", layout.GasStation.Min, layout.GasStation.Max, layout.GasStation.Anchor, new Color(0.84f, 0.68f, 0.26f));
        CreateLocation(LocationType.Forest, "Forest", layout.Forest.Min, layout.Forest.Max, layout.Forest.Anchor, new Color(0.22f, 0.55f, 0.24f));
        CreateLocation(LocationType.Warehouse, "Warehouse", layout.Warehouse.Min, layout.Warehouse.Max, layout.Warehouse.Anchor, new Color(0.7f, 0.52f, 0.3f));
        CreateLocation(LocationType.Town, "Town", layout.Town.Min, layout.Town.Max, layout.Town.Anchor, new Color(0.3f, 0.52f, 0.8f));
        CreateLocation(LocationType.Motel, "Motel", layout.Motel.Min, layout.Motel.Max, layout.Motel.Anchor, new Color(0.91f, 0.87f, 0.74f));

        SessionDebugLogger.Log(
            "WORLD",
            $"Generated layout: Parking {FormatPlacement(layout.Parking)}, GasStation {FormatPlacement(layout.GasStation)}, Forest {FormatPlacement(layout.Forest)}, Warehouse {FormatPlacement(layout.Warehouse)}, Town {FormatPlacement(layout.Town)}.");
    }

    private bool HasRequiredLayoutRoads(GeneratedWorldLayout layout)
    {
        return FindRoadBuildPath(layout.Parking.Anchor, layout.GasStation.Anchor, cell => IsPlacementCell(layout, cell)) != null &&
               FindRoadBuildPath(layout.GasStation.Anchor, layout.Warehouse.Anchor, cell => IsPlacementCell(layout, cell)) != null &&
               FindRoadBuildPath(layout.Warehouse.Anchor, layout.Forest.Anchor, cell => IsPlacementCell(layout, cell)) != null &&
               FindRoadBuildPath(layout.Warehouse.Anchor, layout.Town.Anchor, cell => IsPlacementCell(layout, cell)) != null &&
               FindRoadBuildPath(layout.Warehouse.Anchor, layout.Motel.Anchor, cell => IsPlacementCell(layout, cell)) != null;
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

        List<Vector2Int> plannedCells = MiscTreePlanner.Plan(GridWidth, GridHeight, roadCells, IsLocationCell);
        for (int i = 0; i < plannedCells.Count; i++)
        {
            CreateMiscTree(plannedCells[i], i % 3);
        }

        SessionDebugLogger.Log("WORLD", $"Placed {plannedCells.Count} misc trees.");
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
        treeRoot.transform.localScale = Vector3.one * Random.Range(0.86f, 1.14f);
        CreateTreeVariant(treeRoot.transform, variantIndex);
    }

    private void CreateTreeVariant(Transform parent, int variantIndex)
    {
        switch (Mathf.Abs(variantIndex) % 3)
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
