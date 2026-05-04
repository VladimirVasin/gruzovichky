using UnityEngine;

public partial class GameBootstrap
{
    private const int DocksFootprintWidth = 4;
    private const int DocksFootprintDepth = 3;

    private static readonly TradeResourceType[] DocksExportCatalog =
    {
        TradeResourceType.Logs,
        TradeResourceType.Boards,
        TradeResourceType.Furniture
    };

    private static readonly TradeResourceType[] DocksImportCatalog =
    {
        TradeResourceType.Cotton,
        TradeResourceType.Textile,
        TradeResourceType.Furniture
    };

    private bool TryPlaceDocksAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.Docks))
        {
            SessionDebugLogger.Log("BUILD", "Docks placement rejected: docks already exist.");
            return false;
        }

        if (!TryGetDocksPlacement(anchorCell, out Vector2Int min, out Vector2Int max, out Vector2Int placementAnchor))
        {
            SessionDebugLogger.Log("BUILD", $"Docks placement rejected at anchor ({anchorCell.x},{anchorCell.y}); needs a clear river bank.");
            return false;
        }

        CreateLocation(LocationType.Docks, "Docks", min, max, placementAnchor, new Color(0.46f, 0.32f, 0.18f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        SessionDebugLogger.Log("BUILD", $"Placed Docks at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = placementAnchor })}.");
        return true;
    }

    private bool TryGetDocksPlacement(Vector2Int clickedCell, out Vector2Int min, out Vector2Int max, out Vector2Int placementAnchor)
    {
        if (TryGetRotatedBuildingPlacement(clickedCell, LocationType.Docks, DocksFootprintWidth, DocksFootprintDepth, out min, out max) &&
            IsDocksOnRiverBank(min, max))
        {
            placementAnchor = clickedCell;
            return true;
        }

        return TryGetRiverFacingDocksPlacement(clickedCell, out min, out max, out placementAnchor);
    }

    private bool GetDocksPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        previewPosition = GetCellCenter(anchorCell) + new Vector3(0f, RoadHeight + 0.03f, 0f);
        previewScale = new Vector3(0.98f, 0.04f, 0.98f);
        GetRotatedBuildingFootprint(anchorCell, DocksFootprintWidth, DocksFootprintDepth, out Vector2Int min, out Vector2Int max);
        SetBuildFootprintPreviewCells(min, max, anchorCell);
        bool canPlace = TryGetDocksPlacement(anchorCell, out min, out max, out Vector2Int placementAnchor);
        if (canPlace)
        {
            SetBuildFootprintPreviewCells(min, max, placementAnchor);
        }

        BuildingPlacementPreview preview = BuildingPlacementService.CreatePreview(min, max);
        Vector2 center = BuildingPlacementService.GetFootprintCenter(preview.Min, preview.Max);
        previewPosition = new Vector3(center.x, SampleTerrainHeight(center.x, center.y) + RoadHeight + 0.03f, center.y);
        previewScale = preview.Scale;
        return canPlace;
    }

    private bool TryGetRiverFacingDocksPlacement(Vector2Int clickedCell, out Vector2Int min, out Vector2Int max, out Vector2Int placementAnchor)
    {
        int shoreRow = GridHeight - WaterRiverWidth;
        int anchorY = shoreRow - DocksFootprintDepth - 1;
        int minShoreClickY = anchorY - 4;
        int searchRadius = DocksFootprintWidth + 2;

        if (!IsInsideGrid(clickedCell) || clickedCell.y < minShoreClickY)
        {
            min = clickedCell;
            max = clickedCell;
            placementAnchor = clickedCell;
            return false;
        }

        placementAnchor = new Vector2Int(clickedCell.x, anchorY);
        BuildingPlacementService.GetRotatedFootprint(
            placementAnchor,
            DocksFootprintWidth,
            DocksFootprintDepth,
            0,
            out min,
            out max);
        Vector2Int fallbackMin = min;
        Vector2Int fallbackMax = max;
        Vector2Int fallbackAnchor = placementAnchor;

        for (int dx = 0; dx <= searchRadius; dx++)
        {
            int[] candidateXs = dx == 0
                ? new[] { clickedCell.x }
                : new[] { clickedCell.x - dx, clickedCell.x + dx };

            foreach (int candidateX in candidateXs)
            {
                placementAnchor = new Vector2Int(candidateX, anchorY);
                BuildingPlacementService.GetRotatedFootprint(
                    placementAnchor,
                    DocksFootprintWidth,
                    DocksFootprintDepth,
                    0,
                    out min,
                    out max);

                if (IsDocksPlacementClear(placementAnchor, min, max) && IsDocksOnRiverBank(min, max))
                {
                    return true;
                }
            }
        }

        min = fallbackMin;
        max = fallbackMax;
        placementAnchor = fallbackAnchor;
        return false;
    }

    private bool IsDocksPlacementClear(Vector2Int placementAnchor, Vector2Int min, Vector2Int max)
    {
        if (locations.ContainsKey(LocationType.Docks))
        {
            return false;
        }

        if (!IsInsideGrid(placementAnchor) ||
            edgeHighwayCells.Contains(placementAnchor) ||
            IsLocationCell(placementAnchor) ||
            IsWaterOrBeachCell(placementAnchor))
        {
            return false;
        }

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector2Int cell = new(x, y);
                if (!IsInsideGrid(cell) ||
                    roadCells.Contains(cell) ||
                    edgeHighwayCells.Contains(cell) ||
                    IsLocationCell(cell) ||
                    IsWaterOrBeachCell(cell))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool IsDocksOnRiverBank(Vector2Int min, Vector2Int max)
    {
        for (int x = min.x; x <= max.x; x++)
        {
            if (IsRiverWaterCell(new Vector2Int(x, max.y + 1)))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsRiverWaterCell(Vector2Int cell)
    {
        return IsInsideGrid(cell) && cell.y >= GridHeight - WaterRiverWidth && waterCells.Contains(cell);
    }

    private Vector2Int GetDocksWaterCell(LocationData docks)
    {
        int x = Mathf.Clamp((docks.Min.x + docks.Max.x) / 2, 0, GridWidth - 1);
        Vector2Int candidate = new(x, docks.Max.y + 1);
        if (IsRiverWaterCell(candidate))
        {
            return candidate;
        }

        for (int cx = docks.Min.x; cx <= docks.Max.x; cx++)
        {
            candidate = new Vector2Int(cx, docks.Max.y + 1);
            if (IsRiverWaterCell(candidate))
            {
                return candidate;
            }
        }

        return new Vector2Int(x, GridHeight - WaterRiverWidth);
    }

    private void CreateDocksDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Color timber = new(0.42f, 0.27f, 0.13f);
        Color darkTimber = new(0.24f, 0.15f, 0.08f);
        Color roof = new(0.18f, 0.22f, 0.24f);
        Color rope = new(0.78f, 0.68f, 0.48f);
        Color metal = new(0.42f, 0.44f, 0.46f);

        Transform root = CreateAnchorOrientedBuildingRoot(parent, "DocksModel", center, min, max, anchor);
        CreateBuildingBox(root, "WarehouseShed", new Vector3(-0.65f, 0.34f, -0.22f), new Vector3(1.25f, 0.62f, 0.88f), timber, VisualSmoothnessWood, true, true);
        CreateBuildingBox(root, "WarehouseRoof", new Vector3(-0.65f, 0.72f, -0.22f), new Vector3(1.42f, 0.12f, 1.02f), roof, VisualSmoothnessRoofMetal, true, true);
        CreateBuildingBox(root, "LoadingDoor", new Vector3(-0.65f, 0.26f, 0.25f), new Vector3(0.48f, 0.44f, 0.08f), darkTimber, VisualSmoothnessWood, true);

        for (int i = 0; i < 4; i++)
        {
            float x = -1.55f + i * 0.82f;
            CreateBuildingBox(parent, "PierDeck", new Vector3(center.x + x, 0.08f, max.y + 1.08f), new Vector3(0.72f, 0.10f, 1.70f), timber, VisualSmoothnessWood);
            CreateBuildingCylinder(parent, "PierPost", new Vector3(center.x + x - 0.24f, 0.18f, max.y + 0.36f), new Vector3(0.08f, 0.34f, 0.08f), darkTimber, VisualSmoothnessWood);
            CreateBuildingCylinder(parent, "PierPost", new Vector3(center.x + x + 0.24f, 0.18f, max.y + 1.84f), new Vector3(0.08f, 0.34f, 0.08f), darkTimber, VisualSmoothnessWood);
        }

        CreateBuildingBox(parent, "CraneMast", new Vector3(center.x + 1.25f, 0.76f, center.z + 0.22f), new Vector3(0.14f, 1.20f, 0.14f), metal, VisualSmoothnessVehicleMetal);
        CreateBuildingBox(parent, "CraneBoom", new Vector3(center.x + 1.08f, 1.34f, center.z + 0.72f), new Vector3(0.12f, 0.10f, 1.35f), metal, VisualSmoothnessVehicleMetal);
        CreateBuildingBox(parent, "CraneHookLine", new Vector3(center.x + 1.08f, 0.98f, center.z + 1.28f), new Vector3(0.035f, 0.58f, 0.035f), darkTimber, VisualSmoothnessVehicleMetal);
        CreateBuildingBox(parent, "CraneHook", new Vector3(center.x + 1.08f, 0.66f, center.z + 1.28f), new Vector3(0.16f, 0.08f, 0.08f), metal, VisualSmoothnessVehicleMetal);

        CreateBuildingCrateStack(parent, new Vector3(center.x + 0.50f, 0.13f, center.z - 0.62f), 6);
        CreateBuildingCylinder(parent, "RopeCoil", new Vector3(center.x + 1.55f, 0.13f, center.z - 0.58f), new Vector3(0.22f, 0.035f, 0.22f), rope, VisualSmoothnessWood);
        CreateDrivewayToAnchor(parent, min, max, anchor, 0.62f);
    }

    private void CycleDocksOrders(LocationData docks)
    {
        docks.DocksExportResource = CycleDocksResource(docks.DocksExportResource, DocksExportCatalog);
        docks.DocksImportResource = CycleDocksResource(docks.DocksImportResource, DocksImportCatalog);
        SessionDebugLogger.Log("DOCKS", $"Orders changed: export={docks.DocksExportResource}, import={docks.DocksImportResource}.");
    }

    private static TradeResourceType CycleDocksResource(TradeResourceType current, TradeResourceType[] catalog)
    {
        if (catalog == null || catalog.Length == 0)
        {
            return current;
        }

        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] == current)
            {
                return catalog[(i + 1) % catalog.Length];
            }
        }

        return catalog[0];
    }
}
