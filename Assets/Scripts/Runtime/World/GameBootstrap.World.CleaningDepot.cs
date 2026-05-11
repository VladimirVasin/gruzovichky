using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap
{
    private const float CleanerCoverageRadius = 18f;
    private const int CleanerCoverageRingSegments = 96;
    private static readonly Color CleanerCoverageRingColor = new(0.22f, 1f, 0.36f, 0.72f);

    private GameObject cleaningDepotSelectionRadiusVisual;
    private GameObject cleaningDepotBuildRadiusVisual;

    private bool TryPlaceCleaningDepotAtAnchor(Vector2Int anchorCell)
    {
        if (locations.ContainsKey(LocationType.CleaningDepot))
        {
            SessionDebugLogger.Log("BUILD", "Cleaning Depot placement rejected: already exists.");
            return false;
        }

        if (!TryGetCleaningDepotPlacement(anchorCell, out Vector2Int min, out Vector2Int max))
        {
            SessionDebugLogger.Log("BUILD", $"Cleaning Depot placement rejected at anchor ({anchorCell.x},{anchorCell.y}).");
            return false;
        }

        CreateLocation(LocationType.CleaningDepot, "Cleaning Depot", min, max, anchorCell, new Color(0.24f, 0.55f, 0.45f));
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        isShiftsScreenDirty = true;
        RebuildRoadLanterns();
        RebuildRoadsideBenches();
        RebuildRoadSigns();
        SessionDebugLogger.Log("BUILD", $"Placed Cleaning Depot at {FormatPlacement(new WorldLocationPlacement { Min = min, Max = max, Anchor = anchorCell })}.");
        return true;
    }

    private bool TryGetCleaningDepotPlacement(Vector2Int anchorCell, out Vector2Int min, out Vector2Int max)
    {
        return TryGetTwoByTwoBuildingPlacement(anchorCell, LocationType.CleaningDepot, out min, out max);
    }

    private bool GetCleaningDepotPlacementPreview(Vector2Int anchorCell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        return GetTwoByTwoBuildingPlacementPreview(anchorCell, LocationType.CleaningDepot, out previewPosition, out previewScale);
    }

    private void CreateCleaningDepotDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        Transform root = CreateAnchorOrientedBuildingRoot(parent, "CleaningDepotDetailRoot", center, min, max, anchor, BuildingDecorScale);
        Color wall = new(0.28f, 0.58f, 0.48f);
        Color roof = new(0.16f, 0.22f, 0.24f);
        Color concrete = new(0.48f, 0.50f, 0.48f);
        Color glass = new(0.58f, 0.82f, 0.90f);
        Color dark = new(0.12f, 0.14f, 0.15f);
        Color binGreen = new(0.18f, 0.38f, 0.30f);
        Color brush = new(0.78f, 0.62f, 0.28f);

        CreateBuildingBox(root, "CleaningDepotHall", new Vector3(0f, 0.36f, -0.10f), new Vector3(1.58f, 0.72f, 1.22f), wall, VisualSmoothnessBuildingWall, true, true);
        CreateBuildingBox(root, "CleaningDepotRoof", new Vector3(0f, 0.78f, -0.10f), new Vector3(1.76f, 0.12f, 1.38f), roof, VisualSmoothnessRoofMetal, true, true);
        CreateBuildingBox(root, "CleaningDepotApron", new Vector3(0f, -0.18f, 0.78f), new Vector3(1.70f, 0.035f, 0.62f), concrete, VisualSmoothnessAsphalt, true);
        CreateBuildingBox(root, "CleaningDepotDoor", new Vector3(0f, 0.24f, 0.54f), new Vector3(0.48f, 0.48f, 0.06f), dark, VisualSmoothnessVehicleMetal, true, true);
        CreateBuildingBox(root, "CleaningDepotWindowL", new Vector3(-0.52f, 0.46f, 0.54f), new Vector3(0.30f, 0.22f, 0.04f), glass, VisualSmoothnessGlass, true);
        CreateBuildingBox(root, "CleaningDepotWindowR", new Vector3(0.52f, 0.46f, 0.54f), new Vector3(0.30f, 0.22f, 0.04f), glass, VisualSmoothnessGlass, true);
        CreateBuildingBox(root, "CleaningDepotSign", new Vector3(0f, 0.66f, 0.60f), new Vector3(0.84f, 0.16f, 0.035f), new Color(0.90f, 0.86f, 0.62f), VisualSmoothnessDefault, true);

        for (int i = 0; i < 2; i++)
        {
            float x = -0.44f + i * 0.88f;
            CreateBuildingBox(root, "CleaningDepotBin", new Vector3(x, 0.05f, 0.98f), new Vector3(0.34f, 0.30f, 0.30f), binGreen, VisualSmoothnessVehicleMetal, true, true);
            CreateBuildingBox(root, "CleaningDepotBinLid", new Vector3(x, 0.22f, 0.98f), new Vector3(0.38f, 0.05f, 0.34f), roof, VisualSmoothnessVehicleMetal, true);
        }

        GameObject broomHandle = CreateBuildingBox(root, "CleaningDepotBroomHandle", new Vector3(-0.80f, 0.30f, 0.70f), new Vector3(0.035f, 0.66f, 0.035f), brush, VisualSmoothnessWood, true);
        broomHandle.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        GameObject broomHead = CreateBuildingBox(root, "CleaningDepotBroomHead", new Vector3(-0.88f, 0.03f, 0.70f), new Vector3(0.24f, 0.08f, 0.10f), new Color(0.76f, 0.70f, 0.46f), VisualSmoothnessWood, true);
        broomHead.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);

        CreateBuildingBox(root, "CleaningDepotCartBase", new Vector3(0.78f, 0.06f, 0.72f), new Vector3(0.36f, 0.14f, 0.26f), new Color(0.34f, 0.42f, 0.44f), VisualSmoothnessVehicleMetal, true, true);
        CreateBuildingCylinder(root, "CleaningDepotCartWheelL", new Vector3(0.62f, 0.04f, 0.58f), new Vector3(0.055f, 0.035f, 0.055f), dark, VisualSmoothnessRubber, true);
        CreateBuildingCylinder(root, "CleaningDepotCartWheelR", new Vector3(0.94f, 0.04f, 0.58f), new Vector3(0.055f, 0.035f, 0.055f), dark, VisualSmoothnessRubber, true);
    }

    private bool TryGetCleanerCoverageCenter(DriverAgent worker, out Vector3 center)
    {
        center = default;
        if (worker == null || worker.AssignedBuildingType != LocationType.CleaningDepot)
        {
            return false;
        }

        LocationData depot = GetAssignedBuildingLocation(worker);
        if (depot == null)
        {
            return false;
        }

        center = GetLocationCenter(depot);
        return true;
    }

    private bool IsCellWithinCleanerCoverage(Vector2Int cell, Vector3 center)
    {
        Vector3 cellCenter = GetCellCenter(cell);
        float dx = cellCenter.x - center.x;
        float dz = cellCenter.z - center.z;
        return dx * dx + dz * dz <= CleanerCoverageRadius * CleanerCoverageRadius;
    }

    private void ShowCleaningDepotSelectionRadius(LocationData depot)
    {
        if (depot == null)
        {
            HideCleaningDepotSelectionRadius();
            return;
        }

        ShowCleaningDepotRadiusVisual(ref cleaningDepotSelectionRadiusVisual, "CleaningDepotSelectionRadius", GetLocationCenter(depot));
    }

    private void HideCleaningDepotSelectionRadius()
    {
        if (cleaningDepotSelectionRadiusVisual != null)
        {
            cleaningDepotSelectionRadiusVisual.SetActive(false);
        }
    }

    private void ShowCleaningDepotBuildRadiusFromPreview()
    {
        if (activeBuildTool != BuildTool.CleaningDepot || !TryGetBuildPreviewFootprintCenter(out Vector3 center))
        {
            HideCleaningDepotBuildRadius();
            return;
        }

        ShowCleaningDepotRadiusVisual(ref cleaningDepotBuildRadiusVisual, "CleaningDepotBuildRadius", center);
    }

    private void HideCleaningDepotBuildRadius()
    {
        if (cleaningDepotBuildRadiusVisual != null)
        {
            cleaningDepotBuildRadiusVisual.SetActive(false);
        }
    }

    private bool TryGetBuildPreviewFootprintCenter(out Vector3 center)
    {
        center = default;
        if (buildPreviewFootprintCells.Count == 0)
        {
            return false;
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        for (int i = 0; i < buildPreviewFootprintCells.Count; i++)
        {
            Vector2Int cell = buildPreviewFootprintCells[i];
            minX = Mathf.Min(minX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxX = Mathf.Max(maxX, cell.x);
            maxY = Mathf.Max(maxY, cell.y);
        }

        float x = (minX + maxX + 1) * 0.5f;
        float z = (minY + maxY + 1) * 0.5f;
        center = new Vector3(x, SampleTerrainHeight(x, z) + RoadHeight + 0.10f, z);
        return true;
    }

    private void ShowCleaningDepotRadiusVisual(ref GameObject visual, string name, Vector3 center)
    {
        if (visual == null)
        {
            visual = CreateCleaningDepotRadiusVisual(name);
        }

        center.y = SampleTerrainHeight(center.x, center.z) + RoadHeight + 0.10f;
        visual.transform.position = center;
        LineRenderer line = visual.GetComponent<LineRenderer>();
        if (line != null)
        {
            line.startColor = CleanerCoverageRingColor;
            line.endColor = CleanerCoverageRingColor;
            line.widthMultiplier = 0.085f;
        }

        visual.SetActive(true);
    }

    private GameObject CreateCleaningDepotRadiusVisual(string name)
    {
        GameObject visual = new(name);
        visual.transform.SetParent(worldRoot != null ? worldRoot : transform, false);
        LineRenderer line = visual.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = CleanerCoverageRingSegments;
        line.widthMultiplier = 0.085f;
        line.numCornerVertices = 3;
        line.numCapVertices = 3;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.material = new Material(ShaderRefs.Sprites);
        line.startColor = CleanerCoverageRingColor;
        line.endColor = CleanerCoverageRingColor;

        for (int i = 0; i < CleanerCoverageRingSegments; i++)
        {
            float angle = i / (float)CleanerCoverageRingSegments * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * CleanerCoverageRadius, 0f, Mathf.Sin(angle) * CleanerCoverageRadius));
        }

        visual.SetActive(false);
        return visual;
    }
}
