using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class GameBootstrap
{
    private void SetupBuildHoverHighlight()
    {
        if (worldRoot == null)
        {
            return;
        }

        buildHoverHighlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buildHoverHighlight.name = "BuildHoverHighlight";
        buildHoverHighlight.transform.SetParent(worldRoot, false);
        buildHoverHighlight.transform.localScale = new Vector3(0.92f, 0.04f, 0.92f);
        buildHoverHighlight.GetComponent<Collider>().enabled = false;
        ApplyColor(buildHoverHighlight, new Color(0.22f, 0.9f, 0.32f));
        ConfigureStaticVisual(buildHoverHighlight);
        DecorateRoadPreviewTile(buildHoverHighlight);
        buildHoverHighlight.SetActive(false);

        buildHoverDrivewayHighlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buildHoverDrivewayHighlight.name = "BuildHoverDrivewayHighlight";
        buildHoverDrivewayHighlight.transform.SetParent(worldRoot, false);
        buildHoverDrivewayHighlight.transform.localScale = new Vector3(0.86f, 0.045f, 0.86f);
        buildHoverDrivewayHighlight.GetComponent<Collider>().enabled = false;
        ApplyColor(buildHoverDrivewayHighlight, new Color(0.32f, 0.62f, 1f));
        ConfigureStaticVisual(buildHoverDrivewayHighlight);
        buildHoverDrivewayHighlight.SetActive(false);

        SetupBuildCursorAssist();

        roadPathStartHighlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadPathStartHighlight.name = "RoadPathStartHighlight";
        roadPathStartHighlight.transform.SetParent(worldRoot, false);
        roadPathStartHighlight.transform.localScale = new Vector3(0.94f, 0.06f, 0.94f);
        roadPathStartHighlight.GetComponent<Collider>().enabled = false;
        ApplyColor(roadPathStartHighlight, new Color(0.22f, 0.72f, 1f));
        ConfigureStaticVisual(roadPathStartHighlight);
        DecorateRoadPreviewTile(roadPathStartHighlight);
        roadPathStartHighlight.SetActive(false);

        roadPathStartSideHighlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadPathStartSideHighlight.name = "RoadPathStartSideHighlight";
        roadPathStartSideHighlight.transform.SetParent(worldRoot, false);
        roadPathStartSideHighlight.transform.localScale = new Vector3(0.94f, 0.06f, 0.94f);
        roadPathStartSideHighlight.GetComponent<Collider>().enabled = false;
        ApplyColor(roadPathStartSideHighlight, new Color(0.22f, 0.72f, 1f));
        ConfigureStaticVisual(roadPathStartSideHighlight);
        DecorateRoadPreviewTile(roadPathStartSideHighlight);
        roadPathStartSideHighlight.SetActive(false);
    }

    private void SetRoadPreviewTileVisual(GameObject root, bool canBuild, bool startMarker = false)
    {
        if (root == null)
        {
            return;
        }

        SetRoadPreviewTileDecorationsActive(root, true);

        Renderer baseRenderer = root.GetComponent<Renderer>();
        if (baseRenderer != null)
        {
            baseRenderer.sharedMaterial.color = startMarker
                ? new Color(0.16f, 0.34f, 0.56f)
                : canBuild
                    ? new Color(0.18f, 0.19f, 0.22f)
                    : new Color(0.42f, 0.14f, 0.12f);
        }

        Transform centerDash = root.transform.Find("RoadPreviewCenterDash");
        Transform leftEdge = root.transform.Find("RoadPreviewLeftEdge");
        Transform rightEdge = root.transform.Find("RoadPreviewRightEdge");
        Color centerColor = startMarker
            ? new Color(0.72f, 0.9f, 1f)
            : canBuild
                ? new Color(0.95f, 0.82f, 0.32f)
                : new Color(1f, 0.62f, 0.24f);
        Color edgeColor = startMarker
            ? new Color(0.86f, 0.95f, 1f)
            : canBuild
                ? new Color(0.95f, 0.93f, 0.82f)
                : new Color(1f, 0.84f, 0.72f);

        if (centerDash != null && centerDash.TryGetComponent(out Renderer centerRenderer))
        {
            centerRenderer.sharedMaterial.color = centerColor;
        }

        if (leftEdge != null && leftEdge.TryGetComponent(out Renderer leftRenderer))
        {
            leftRenderer.sharedMaterial.color = edgeColor;
        }

        if (rightEdge != null && rightEdge.TryGetComponent(out Renderer rightRenderer))
        {
            rightRenderer.sharedMaterial.color = edgeColor;
        }
    }

    private static void SetRoadPreviewTileDecorationsActive(GameObject root, bool active)
    {
        if (root == null)
        {
            return;
        }

        Transform centerDash = root.transform.Find("RoadPreviewCenterDash");
        Transform leftEdge = root.transform.Find("RoadPreviewLeftEdge");
        Transform rightEdge = root.transform.Find("RoadPreviewRightEdge");
        if (centerDash != null)
        {
            centerDash.gameObject.SetActive(active);
        }

        if (leftEdge != null)
        {
            leftEdge.gameObject.SetActive(active);
        }

        if (rightEdge != null)
        {
            rightEdge.gameObject.SetActive(active);
        }
    }

    private void SetRoadPreviewTileOrientation(GameObject root, Vector2Int direction)
    {
        if (root == null)
        {
            return;
        }

        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            return;
        }

        root.transform.rotation = Quaternion.identity;
    }

    private Vector2Int GetAdjacentRoadPreviewDirection(Vector2Int cell)
    {
        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        foreach (Vector2Int direction in directions)
        {
            Vector2Int neighbor = cell + direction;
            if (roadCells.Contains(neighbor) || edgeHighwayCells.Contains(neighbor))
            {
                return -direction; // away from existing road = likely travel direction
            }
        }

        return Vector2Int.up;
    }

    private Vector2Int GetBuildRoadDirection()
    {
        return (buildPlacementRotationIndex % 4) switch
        {
            1 => Vector2Int.right,
            2 => Vector2Int.down,
            3 => Vector2Int.left,
            _ => Vector2Int.up
        };
    }

    private static Vector2Int NormalizeRoadDirection(Vector2Int direction)
    {
        return TwoLaneRoadGeometry.NormalizeDirection(direction);
    }

    private static Vector2Int GetRoadWidthOffset(Vector2Int roadDirection)
    {
        return TwoLaneRoadGeometry.GetRightLaneOffset(roadDirection);
    }

    private void ClearBuildRoadPreviewCells()
    {
        buildPreviewFootprintCells.Clear();
        buildPreviewWalkBufferCells.Clear();
        buildPreviewRoadDirections.Clear();
    }

    private void AddRoadPreviewFootprint(Vector2Int cell, Vector2Int roadDirection)
    {
        Vector2Int dir = NormalizeRoadDirection(roadDirection);
        AddRoadPreviewCell(cell, dir);
        AddRoadPreviewCell(cell + GetRoadWidthOffset(dir), dir);
    }

    private void AddRoadPreviewFootprintWithOffset(Vector2Int cell, Vector2Int roadDirection, Vector2Int widthOffset)
    {
        Vector2Int dir = NormalizeRoadDirection(roadDirection);
        Vector2Int offset = NormalizeRoadDirection(widthOffset);
        AddRoadPreviewCell(cell, dir);
        AddRoadPreviewCell(cell + offset, dir);
    }

    private void AddRoadPreviewForActiveRoadTool(Vector2Int cell, Vector2Int roadDirection)
    {
        if (activeBuildTool == BuildTool.SingleRoad)
        {
            AddRoadPreviewCell(cell, roadDirection);
            return;
        }

        AddRoadPreviewFootprint(cell, roadDirection);
    }

    private void AddRoadTurnPreviewFootprint(
        Vector2Int previousCell,
        Vector2Int previousDirection,
        Vector2Int previousOffset,
        Vector2Int currentCell,
        Vector2Int currentDirection,
        Vector2Int currentOffset)
    {
        Vector2Int prevDir = NormalizeRoadDirection(previousDirection);
        Vector2Int curDir = NormalizeRoadDirection(currentDirection);
        if (prevDir == curDir)
        {
            return;
        }

        Vector2Int previousSide = previousCell + NormalizeRoadDirection(previousOffset);
        Vector2Int currentSide = currentCell + NormalizeRoadDirection(currentOffset);

        int minX = Mathf.Min(Mathf.Min(previousCell.x, previousSide.x), Mathf.Min(currentCell.x, currentSide.x));
        int maxX = Mathf.Max(Mathf.Max(previousCell.x, previousSide.x), Mathf.Max(currentCell.x, currentSide.x));
        int minY = Mathf.Min(Mathf.Min(previousCell.y, previousSide.y), Mathf.Min(currentCell.y, currentSide.y));
        int maxY = Mathf.Max(Mathf.Max(previousCell.y, previousSide.y), Mathf.Max(currentCell.y, currentSide.y));

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int fillCell = new(x, y);
                if (!IsInsideGrid(fillCell))
                {
                    continue;
                }

                AddRoadPreviewCell(fillCell, curDir);
            }
        }
    }

    private void AddRoadPreviewCell(Vector2Int cell, Vector2Int roadDirection)
    {
        int existing = buildPreviewFootprintCells.IndexOf(cell);
        if (existing >= 0)
        {
            buildPreviewRoadDirections[existing] = NormalizeRoadDirection(roadDirection);
            return;
        }

        buildPreviewFootprintCells.Add(cell);
        buildPreviewRoadDirections.Add(NormalizeRoadDirection(roadDirection));
    }

    private void UpdateBuildHoverHighlight()
    {
        if (buildHoverHighlight == null)
        {
            return;
        }

        hoveredBuildCell = null;
        if (activeBuildTool == BuildTool.None || mainCamera == null || Mouse.current == null || isTruckCameraFocused || isRightMouseDragging)
        {
            HideBuildHoverHighlights();
            return;
        }

        if (IsRoadBuildTool(activeBuildTool) && roadConstructionHiddenCells.Count > 0)
        {
            HideBuildHoverHighlights();
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (IsPointerOverHud(mousePosition))
        {
            HideBuildHoverHighlights();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Plane plane = new(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
        {
            HideBuildHoverHighlights();
            return;
        }

        Vector2Int cell = WorldToCell(ray.GetPoint(distance));
        if (!IsInsideGrid(cell))
        {
            HideBuildHoverHighlights();
            return;
        }

        hoveredBuildCell = cell;
        bool canBuild = GetBuildPreviewAtCell(cell, out Vector3 previewPosition, out Vector3 previewScale);
        if (IsBuildingBuildTool(activeBuildTool))
        {
            UpdateBuildFootprintHoverHighlights(canBuild);
            return;
        }

        if (IsRoadBuildTool(activeBuildTool) && roadPathStart.HasValue)
        {
            UpdateRoadPathPreview(cell, roadPathStart.Value);
            return;
        }

        if (IsRoadBuildTool(activeBuildTool))
        {
            UpdateBuildFootprintHoverHighlights(canBuild);
            return;
        }

        HideBuildFootprintHoverHighlights();
        buildHoverHighlight.SetActive(true);
        buildHoverHighlight.transform.position = previewPosition;
        buildHoverHighlight.transform.localScale = previewScale;

        buildHoverHighlight.transform.rotation = Quaternion.identity;
        Renderer renderer = buildHoverHighlight.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial.color = canBuild
                ? new Color(0.22f, 0.9f, 0.32f)
                : new Color(0.92f, 0.28f, 0.22f);
        }

        UpdateBuildCursorAssist(previewPosition, Mathf.Max(previewScale.x, previewScale.z) + 1.2f, canBuild);
    }

    private void HideBuildHoverHighlights()
    {
        if (buildHoverHighlight != null)
        {
            buildHoverHighlight.SetActive(false);
        }

        HideBuildFootprintHoverHighlights();
    }

    private void HideBuildFootprintHoverHighlights()
    {
        HideCleaningDepotBuildRadius();

        if (buildHoverDrivewayHighlight != null)
        {
            buildHoverDrivewayHighlight.SetActive(false);
        }

        HideBuildCursorAssist();

        for (int i = 0; i < buildHoverCellHighlights.Count; i++)
        {
            if (buildHoverCellHighlights[i] != null)
            {
                buildHoverCellHighlights[i].SetActive(false);
            }
        }
    }

    private void EnsureBuildFootprintHoverCount(int count)
    {
        while (buildHoverCellHighlights.Count < count)
        {
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
            highlight.name = $"BuildHoverFootprintCell_{buildHoverCellHighlights.Count}";
            highlight.transform.SetParent(worldRoot, false);
            highlight.transform.localScale = new Vector3(0.86f, 0.045f, 0.86f);
            highlight.GetComponent<Collider>().enabled = false;
            ApplyColor(highlight, new Color(0.22f, 0.9f, 0.32f));
            ConfigureStaticVisual(highlight);
            DecorateRoadPreviewTile(highlight);
            highlight.SetActive(false);
            buildHoverCellHighlights.Add(highlight);
        }
    }

    private void UpdateBuildFootprintHoverHighlights(bool canBuild)
    {
        if (buildHoverHighlight != null)
        {
            buildHoverHighlight.SetActive(false);
        }

        int footprintCount = buildPreviewFootprintCells.Count;
        int bufferCount = IsBuildingBuildTool(activeBuildTool) ? buildPreviewWalkBufferCells.Count : 0;
        int totalCount = footprintCount + bufferCount;
        EnsureBuildFootprintHoverCount(totalCount);
        Color footprintColor = canBuild ? new Color(0.22f, 0.9f, 0.32f) : new Color(0.92f, 0.28f, 0.22f);
        Color bufferColor = canBuild ? new Color(1f, 0.52f, 0.12f) : new Color(1f, 0.24f, 0.14f);
        for (int i = 0; i < buildHoverCellHighlights.Count; i++)
        {
            GameObject highlight = buildHoverCellHighlights[i];
            bool isBufferCell = i >= footprintCount && i < totalCount;
            Vector2Int cell = isBufferCell
                ? buildPreviewWalkBufferCells[i - footprintCount]
                : i < footprintCount
                    ? buildPreviewFootprintCells[i]
                    : default;
            bool active = i < totalCount && IsInsideGrid(cell);
            highlight.SetActive(active);
            if (!active)
            {
                continue;
            }

            Vector3 center = GetCellCenter(cell);
            highlight.transform.position = new Vector3(center.x, SampleTerrainHeight(center.x, center.z) + RoadHeight + 0.05f, center.z);
            highlight.transform.rotation = Quaternion.identity;
            if (IsRoadBuildTool(activeBuildTool))
            {
                highlight.transform.localScale = new Vector3(0.86f, 0.045f, 0.86f);
                Vector2Int direction = i < buildPreviewRoadDirections.Count
                    ? buildPreviewRoadDirections[i]
                    : GetBuildRoadDirection();
                SetRoadPreviewTileOrientation(highlight, direction);
                SetRoadPreviewTileVisual(highlight, canBuild);
            }
            else
            {
                SetRoadPreviewTileDecorationsActive(highlight, false);
                highlight.transform.localScale = isBufferCell
                    ? new Vector3(0.64f, 0.035f, 0.64f)
                    : new Vector3(0.86f, 0.045f, 0.86f);
                Renderer renderer = highlight.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial.color = isBufferCell ? bufferColor : footprintColor;
                }
            }
        }

        UpdateBuildCursorAssistFromPreview(canBuild);
        ShowCleaningDepotBuildRadiusFromPreview();

        if (buildHoverDrivewayHighlight != null)
        {
            bool showDriveway = buildPreviewDrivewayCell.HasValue && IsInsideGrid(buildPreviewDrivewayCell.Value);
            buildHoverDrivewayHighlight.SetActive(showDriveway);
            if (showDriveway)
            {
                Vector2Int drivewayCell = buildPreviewDrivewayCell.Value;
                Vector3 center = GetCellCenter(drivewayCell);
                buildHoverDrivewayHighlight.transform.position = new Vector3(center.x, SampleTerrainHeight(center.x, center.z) + RoadHeight + 0.065f, center.z);
                Renderer renderer = buildHoverDrivewayHighlight.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial.color = canBuild
                        ? new Color(0.28f, 0.58f, 1f)
                        : new Color(1f, 0.58f, 0.18f);
                }
            }
        }
    }


}
