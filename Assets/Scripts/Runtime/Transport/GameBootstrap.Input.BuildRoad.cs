using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class GameBootstrap
{
    private void HandleRoadRemovalInput()
    {
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            ClearSelectedDebugCell();
        }

        if (mainCamera == null ||
            Mouse.current == null ||
            !IsRoadBuildTool(activeBuildTool) ||
            !Mouse.current.rightButton.wasReleasedThisFrame ||
            isRightMouseDragging)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if ((mousePosition - rightMousePressPosition).sqrMagnitude > 16f || IsPointerOverHud(mousePosition))
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Plane plane = new(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
        {
            return;
        }

        Vector2Int cell = WorldToCell(ray.GetPoint(distance));
        if (!roadCells.Contains(cell))
        {
            return;
        }

        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        isTruckDetailsOpen = false;
        DisableTruckCameraFocus();
        RefreshSelectionVisuals();
        RemoveRoad(cell);
    }

    private void HandleRoadPlacementInput()
    {
        if (mainCamera == null ||
            Mouse.current == null ||
            Mouse.current.rightButton.isPressed ||
            !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        if (TryHandleJoinRaceButtonClick(mousePosition))
        {
            return;
        }

        if (IsPointerOverHud(mousePosition))
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(mousePosition);
        Plane plane = new(Vector3.up, Vector3.zero);
        if (!plane.Raycast(ray, out float distance))
        {
            return;
        }

        if (TryHandleTruckSelection(ray))
        {
            ClearSelectedDebugCell();
            return;
        }

        if (TryHandleLocalBusSelection(ray))
        {
            ClearSelectedDebugCell();
            return;
        }

        if (TryHandleDriverSelection(ray))
        {
            ClearSelectedDebugCell();
            return;
        }

        Vector2Int cell = WorldToCell(ray.GetPoint(distance));
        if (TryHandleLocationSelection(cell))
        {
            ClearSelectedDebugCell();
            return;
        }

        if (activeBuildTool == BuildTool.None)
        {
            if (TryShowBeeEasterEggForCell(cell))
            {
                ClearSelectedDebugCell();
                selectedLocation = null;
                selectedLocalStopIndex = -1;
                selectedPersonalHouseIndex = -1;
                isTruckDetailsOpen = false;
                isLocalBusDetailsOpen = false;
                isDriverDetailsOpen = false;
                DisableTruckCameraFocus();
                RefreshSelectionVisuals();
                return;
            }

            SelectDebugCell(cell);
            selectedLocation = null;
            selectedLocalStopIndex = -1;
            selectedPersonalHouseIndex = -1;
            isTruckDetailsOpen = false;
            isLocalBusDetailsOpen = false;
            isDriverDetailsOpen = false;
            DisableTruckCameraFocus();
            RefreshSelectionVisuals();
            return;
        }

        BuildTool placementTool = activeBuildTool;
        bool buildActionSucceeded = placementTool switch
        {
            BuildTool.Road => TryHandleRoadPlacement(cell),
            BuildTool.SingleRoad => TryHandleRoadPlacement(cell),
            BuildTool.Parking => TryPlaceParkingAtAnchor(cell),
            BuildTool.Warehouse => TryPlaceWarehouseAtAnchor(cell),
            BuildTool.Stop => TryPlaceStopAtAnchor(cell),
            BuildTool.Forest => TryPlaceForestAtAnchor(cell),
            BuildTool.FurnitureFactory => TryPlaceFurnitureFactoryAtAnchor(cell),
            BuildTool.Sawmill => TryPlaceSawmillAtAnchor(cell),
            BuildTool.Motel => TryPlaceMotelAtAnchor(cell),
            BuildTool.Bar => TryPlaceBarAtAnchor(cell),
            BuildTool.Canteen => TryPlaceCanteenAtAnchor(cell),
            BuildTool.GamblingHall => TryPlaceGamblingHallAtAnchor(cell),
            BuildTool.CityPark        => TryPlaceCityParkAtAnchor(cell),
            BuildTool.PersonalHouse   => TryPlacePersonalHouseAtAnchor(cell),
            BuildTool.CarMarket       => TryPlaceCarMarketAtAnchor(cell),
            _ => false
        };

        if (!buildActionSucceeded)
        {
            ClearSelectedDebugCell();
            selectedLocation = null;
            selectedLocalStopIndex = -1;
            selectedPersonalHouseIndex = -1;
            isTruckDetailsOpen = false;
            isDriverDetailsOpen = false;
            DisableTruckCameraFocus();
            RefreshSelectionVisuals();
            return;
        }

        ClearSelectedDebugCell();
        selectedLocation = null;
        selectedLocalStopIndex = -1;
        selectedPersonalHouseIndex = -1;
        isTruckDetailsOpen = false;
        isLocalBusDetailsOpen = false;
        isDriverDetailsOpen = false;
        DisableTruckCameraFocus();
        if (IsBuildingBuildTool(placementTool))
        {
            CompleteBuildingPlacementFlow(placementTool, cell);
            return;
        }

        if (IsRoadBuildTool(placementTool))
        {
            selectedLocation = null;
            selectedLocalStopIndex = -1;
            selectedPersonalHouseIndex = -1;
            RefreshSelectionVisuals();
        }
    }

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

    private void DecorateRoadPreviewTile(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        GameObject centerDash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        centerDash.name = "RoadPreviewCenterDash";
        centerDash.transform.SetParent(root.transform, false);
        centerDash.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        centerDash.transform.localScale = new Vector3(0.14f, 0.22f, 0.82f);
        centerDash.GetComponent<Collider>().enabled = false;
        ApplyColor(centerDash, new Color(0.95f, 0.82f, 0.32f));
        ConfigureStaticVisual(centerDash);

        GameObject leftEdge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftEdge.name = "RoadPreviewLeftEdge";
        leftEdge.transform.SetParent(root.transform, false);
        leftEdge.transform.localPosition = new Vector3(-0.34f, 0.7f, 0f);
        leftEdge.transform.localScale = new Vector3(0.06f, 0.22f, 0.84f);
        leftEdge.GetComponent<Collider>().enabled = false;
        ApplyColor(leftEdge, new Color(0.95f, 0.93f, 0.82f));
        ConfigureStaticVisual(leftEdge);

        GameObject rightEdge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightEdge.name = "RoadPreviewRightEdge";
        rightEdge.transform.SetParent(root.transform, false);
        rightEdge.transform.localPosition = new Vector3(0.34f, 0.7f, 0f);
        rightEdge.transform.localScale = new Vector3(0.06f, 0.22f, 0.84f);
        rightEdge.GetComponent<Collider>().enabled = false;
        ApplyColor(rightEdge, new Color(0.95f, 0.93f, 0.82f));
        ConfigureStaticVisual(rightEdge);
    }

    private void SetRoadPreviewTileVisual(GameObject root, bool canBuild, bool startMarker = false)
    {
        if (root == null)
        {
            return;
        }

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
                return direction;
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
        buildPreviewRoadDirections.Clear();
    }

    private void AddRoadPreviewFootprint(Vector2Int cell, Vector2Int roadDirection)
    {
        Vector2Int dir = NormalizeRoadDirection(roadDirection);
        AddRoadPreviewCell(cell, dir);
        AddRoadPreviewCell(cell + GetRoadWidthOffset(dir), dir);
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

        // Cancel road path mode immediately when Shift is released (runs before any early return)
        if (roadPathStart.HasValue && (Keyboard.current == null || !Keyboard.current.shiftKey.isPressed))
        {
            CancelRoadPathMode();
        }

        hoveredBuildCell = null;
        if (activeBuildTool == BuildTool.None || mainCamera == null || Mouse.current == null || isTruckCameraFocused || isRightMouseDragging)
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

        bool shiftHeld = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

        if (IsRoadBuildTool(activeBuildTool) && shiftHeld)
        {
            if (roadPathStart.HasValue)
            {
                UpdateRoadPathPreview(cell, roadPathStart.Value);
                return;
            }
            UpdateBuildFootprintHoverHighlights(canBuild);
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
        if (buildHoverDrivewayHighlight != null)
        {
            buildHoverDrivewayHighlight.SetActive(false);
        }

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

        EnsureBuildFootprintHoverCount(buildPreviewFootprintCells.Count);
        Color footprintColor = canBuild ? new Color(0.22f, 0.9f, 0.32f) : new Color(0.92f, 0.28f, 0.22f);
        for (int i = 0; i < buildHoverCellHighlights.Count; i++)
        {
            GameObject highlight = buildHoverCellHighlights[i];
            bool active = i < buildPreviewFootprintCells.Count && IsInsideGrid(buildPreviewFootprintCells[i]);
            highlight.SetActive(active);
            if (!active)
            {
                continue;
            }

            Vector2Int cell = buildPreviewFootprintCells[i];
            Vector3 center = GetCellCenter(cell);
            highlight.transform.position = new Vector3(center.x, SampleTerrainHeight(center.x, center.z) + RoadHeight + 0.05f, center.z);
            highlight.transform.rotation = Quaternion.identity;
            if (IsRoadBuildTool(activeBuildTool))
            {
                Vector2Int direction = i < buildPreviewRoadDirections.Count
                    ? buildPreviewRoadDirections[i]
                    : GetBuildRoadDirection();
                SetRoadPreviewTileOrientation(highlight, direction);
                SetRoadPreviewTileVisual(highlight, canBuild);
            }
            else
            {
                Renderer renderer = highlight.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.sharedMaterial.color = footprintColor;
                }
            }
        }

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

    private bool TryPlaceRoadAtCell(Vector2Int cell)
    {
        return RunBatchedRoadPlacement(() => activeBuildTool == BuildTool.SingleRoad
            ? TryPlaceSingleRoadCell(cell, "player")
            : TryPlaceRoadFootprint(cell, GetBuildRoadDirection()));
    }

    private bool TryPlaceSingleRoadCell(Vector2Int cell, string source)
    {
        if (!CanPlaceSingleRoadCell(cell, requireNewRoadCell: true))
        {
            return false;
        }

        bool built = TryAddRoadFootprintCell(cell);
        if (built)
        {
            SessionDebugLogger.Log("ROAD", $"Added single road cell at ({cell.x},{cell.y}) source={source}.");
        }

        return built;
    }

    private bool TryPlaceRoadFootprint(Vector2Int cell, Vector2Int roadDirection)
    {
        Vector2Int dir = NormalizeRoadDirection(roadDirection);
        if (!TryResolveRoadFootprintOffset(cell, dir, requireNewRoadCell: true, IsBuildRoadBlockedCell, out Vector2Int widthOffset))
        {
            return false;
        }

        bool built = false;
        Vector2Int sideCell = cell + widthOffset;
        built |= TryAddRoadFootprintCell(cell);
        built |= TryAddRoadFootprintCell(sideCell);
        if (built)
        {
            SessionDebugLogger.Log("ROAD", $"Added 2-cell road footprint at ({cell.x},{cell.y}) + ({sideCell.x},{sideCell.y}) dir=({dir.x},{dir.y}).");
        }

        return built;
    }

    private bool RunBatchedRoadPlacement(System.Func<bool> buildAction)
    {
        bool previousUnifiedSuppression = suppressUnifiedRoadVisualRebuild;
        bool previousRoadsideSuppression = suppressRoadsideRefresh;
        suppressUnifiedRoadVisualRebuild = true;
        suppressRoadsideRefresh = true;

        bool built = false;
        try
        {
            built = buildAction();
        }
        finally
        {
            suppressUnifiedRoadVisualRebuild = previousUnifiedSuppression;
            suppressRoadsideRefresh = previousRoadsideSuppression;
            if (!suppressRoadsideRefresh)
            {
                FlushPendingRoadsideRefreshes();
            }

            if (built && !suppressUnifiedRoadVisualRebuild)
            {
                RebuildUnifiedRoadVisuals();
            }
        }

        return built;
    }

    private bool TryFillRoadTurnFootprint(
        Vector2Int previousCell,
        Vector2Int previousDirection,
        Vector2Int currentCell,
        Vector2Int currentDirection,
        System.Func<Vector2Int, bool> isBlockedLocationCell,
        string source)
    {
        Vector2Int prevDir = NormalizeRoadDirection(previousDirection);
        Vector2Int curDir = NormalizeRoadDirection(currentDirection);
        if (prevDir == curDir)
        {
            return false;
        }

        GetExistingRoadTurnFillBounds(previousCell, prevDir, currentCell, curDir, out int minX, out int maxX, out int minY, out int maxY);

        bool anyBuilt = false;
        List<Vector2Int> filledCells = new();
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int fillCell = new(x, y);
                if (!IsRoadFootprintCellStructurallyClear(fillCell, isBlockedLocationCell, out bool isNewRoadCell))
                {
                    continue;
                }

                if (!isNewRoadCell)
                {
                    continue;
                }

                if (WouldCreateThirdParallelRoadLane(fillCell, prevDir) ||
                    WouldCreateThirdParallelRoadLane(fillCell, curDir))
                {
                    continue;
                }

                if (TryAddRoadFootprintCell(fillCell))
                {
                    anyBuilt = true;
                    filledCells.Add(fillCell);
                }
            }
        }

        if (anyBuilt)
        {
            SessionDebugLogger.Log(
                "ROAD_TURN",
                $"{source} filled turn prev=({previousCell.x},{previousCell.y}) dir=({prevDir.x},{prevDir.y}) current=({currentCell.x},{currentCell.y}) dir=({curDir.x},{curDir.y}) cells={FormatCellList(filledCells)}.");
        }

        return anyBuilt;
    }

    private bool CanFillRoadTurnFootprint(
        Vector2Int previousCell,
        Vector2Int previousDirection,
        Vector2Int currentCell,
        Vector2Int currentDirection,
        System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        Vector2Int prevDir = NormalizeRoadDirection(previousDirection);
        Vector2Int curDir = NormalizeRoadDirection(currentDirection);
        if (prevDir == curDir)
        {
            return true;
        }

        GetExistingRoadTurnFillBounds(previousCell, prevDir, currentCell, curDir, out int minX, out int maxX, out int minY, out int maxY);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int fillCell = new(x, y);
                if (!IsRoadFootprintCellStructurallyClear(fillCell, isBlockedLocationCell, out _))
                {
                    continue;
                }
            }
        }

        return true;
    }

    private bool WouldCreateThirdParallelRoadLane(Vector2Int cell, Vector2Int roadDirection)
    {
        return RoadBuildPlacementService.WouldCreateThirdParallelRoadLane(cell, roadDirection, roadCells);
    }

    private void GetExistingRoadTurnFillBounds(
        Vector2Int previousCell,
        Vector2Int previousDirection,
        Vector2Int currentCell,
        Vector2Int currentDirection,
        out int minX,
        out int maxX,
        out int minY,
        out int maxY)
    {
        Vector2Int previousOffset = GetExistingRoadWidthOffset(previousCell, previousDirection);
        Vector2Int currentOffset = GetExistingRoadWidthOffset(currentCell, currentDirection);
        Vector2Int previousSide = previousCell + previousOffset;
        Vector2Int currentSide = currentCell + currentOffset;

        minX = Mathf.Min(Mathf.Min(previousCell.x, previousSide.x), Mathf.Min(currentCell.x, currentSide.x));
        maxX = Mathf.Max(Mathf.Max(previousCell.x, previousSide.x), Mathf.Max(currentCell.x, currentSide.x));
        minY = Mathf.Min(Mathf.Min(previousCell.y, previousSide.y), Mathf.Min(currentCell.y, currentSide.y));
        maxY = Mathf.Max(Mathf.Max(previousCell.y, previousSide.y), Mathf.Max(currentCell.y, currentSide.y));
    }

    private Vector2Int GetExistingRoadWidthOffset(Vector2Int cell, Vector2Int roadDirection)
    {
        Vector2Int dir = NormalizeRoadDirection(roadDirection);
        Vector2Int preferredOffset = TwoLaneRoadGeometry.GetRightLaneOffset(dir);
        Vector2Int alternateOffset = -preferredOffset;
        bool hasPreferred = roadCells.Contains(cell + preferredOffset);
        bool hasAlternate = roadCells.Contains(cell + alternateOffset);
        if (hasPreferred && !hasAlternate)
        {
            return preferredOffset;
        }

        if (hasAlternate && !hasPreferred)
        {
            return alternateOffset;
        }

        return preferredOffset;
    }

    private static string FormatCellList(List<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0)
        {
            return "none";
        }

        System.Text.StringBuilder sb = new();
        for (int i = 0; i < cells.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append('(').Append(cells[i].x).Append(',').Append(cells[i].y).Append(')');
        }

        return sb.ToString();
    }

    private bool TryAddRoadFootprintCell(Vector2Int cell)
    {
        if (roadCells.Contains(cell))
        {
            return false;
        }

        RemoveMiscObjectAtCell(cell);
        AddRoad(cell);
        return roadCells.Contains(cell);
    }

    private bool CanPlaceRoadFootprint(Vector2Int cell, Vector2Int roadDirection, bool requireNewRoadCell)
    {
        return CanPlaceRoadFootprint(cell, roadDirection, requireNewRoadCell, IsBuildRoadBlockedCell);
    }

    private bool CanPlaceSingleRoadCell(Vector2Int cell, bool requireNewRoadCell)
    {
        if (!IsRoadFootprintCellStructurallyClear(cell, IsBuildRoadBlockedCell, out bool isNewRoadCell))
        {
            return false;
        }

        return !requireNewRoadCell || isNewRoadCell;
    }

    private bool CanPlaceRoadFootprint(Vector2Int cell, Vector2Int roadDirection, bool requireNewRoadCell, System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        Vector2Int dir = NormalizeRoadDirection(roadDirection);
        return TryResolveRoadFootprintOffset(cell, dir, requireNewRoadCell, isBlockedLocationCell, out _);
    }

    private bool TryResolveRoadFootprintOffset(
        Vector2Int cell,
        Vector2Int roadDirection,
        bool requireNewRoadCell,
        System.Func<Vector2Int, bool> isBlockedLocationCell,
        out Vector2Int widthOffset)
    {
        RoadFootprintResolveResult result = RoadBuildPlacementService.ResolveFootprintOffset(
            cell,
            roadDirection,
            requireNewRoadCell,
            GridWidth,
            GridHeight,
            roadCells,
            edgeHighwayCells,
            null,
            isBlockedLocationCell);
        widthOffset = result.WidthOffset;
        return result.CanPlace;
    }

    private bool CanPlaceRoadFootprintWithOffset(
        Vector2Int cell,
        Vector2Int widthOffset,
        bool requireNewRoadCell,
        System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        return RoadBuildPlacementService.CanPlaceRoadFootprintWithOffset(
            cell,
            widthOffset,
            requireNewRoadCell,
            GridWidth,
            GridHeight,
            roadCells,
            edgeHighwayCells,
            null,
            isBlockedLocationCell);
    }

    private bool IsRoadFootprintCellStructurallyClear(Vector2Int cell, System.Func<Vector2Int, bool> isBlockedLocationCell, out bool isNewRoadCell)
    {
        return RoadBuildPlacementService.IsRoadFootprintCellStructurallyClear(
            cell,
            GridWidth,
            GridHeight,
            roadCells,
            edgeHighwayCells,
            null,
            isBlockedLocationCell,
            out isNewRoadCell);
    }

    private bool IsBuildRoadBlockedCell(Vector2Int cell)
    {
        return IsLocationCell(cell) || IsWaterOrBeachCell(cell);
    }

    private bool TryHandleRoadPlacement(Vector2Int cell)
    {
        bool shiftHeld = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;
        if (!shiftHeld)
        {
            CancelRoadPathMode();
            bool builtSingleCell = TryPlaceRoadAtCell(cell);
            if (builtSingleCell)
            {
                MarkTutorialGoalComplete(TutorialGoalKind.RoadSingleCell);
            }
            return builtSingleCell;
        }

        if (!roadPathStart.HasValue)
        {
            if (!IsInsideGrid(cell)) return false;
            Vector2Int startDirection = GetAdjacentRoadPreviewDirection(cell);
            bool canStart = activeBuildTool == BuildTool.SingleRoad
                ? CanPlaceSingleRoadCell(cell, requireNewRoadCell: false)
                : CanPlaceRoadFootprint(cell, startDirection, requireNewRoadCell: false);
            if (!canStart) return false;
            roadPathStart = cell;
            SetRoadPathStartHighlights(cell, startDirection, true);
            PlayUiSound(uiSelectClip, 0.65f);
            return false;
        }

        bool built = TryBuildRoadPath(roadPathStart.Value, cell);
        if (built)
        {
            MarkTutorialGoalComplete(TutorialGoalKind.RoadShiftPath);
        }
        CancelRoadPathMode();
        return built;
    }

    private bool TryBuildRoadPath(Vector2Int start, Vector2Int end)
    {
        return RunBatchedRoadPlacement(() =>
        {
            if (start == end)
            {
                return activeBuildTool == BuildTool.SingleRoad
                    ? TryPlaceSingleRoadCell(start, "player-path")
                    : TryPlaceRoadFootprint(start, GetBuildRoadDirection());
            }

            List<Vector2Int> path = FindRoadBuildPath(start, end, IsBuildRoadBlockedCell);
            if (path == null || path.Count < 2) return false;

            bool anyBuilt = false;
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int direction = GetRoadPathPreviewDirection(path, i);
                if (activeBuildTool == BuildTool.SingleRoad)
                {
                    anyBuilt |= TryPlaceSingleRoadCell(path[i], "player-path");
                    continue;
                }

                if (CanPlaceRoadFootprint(path[i], direction, requireNewRoadCell: true))
                {
                    anyBuilt |= TryPlaceRoadFootprint(path[i], direction);
                }

                if (i > 0)
                {
                    Vector2Int previousDirection = GetRoadPathPreviewDirection(path, i - 1);
                    anyBuilt |= TryFillRoadTurnFootprint(path[i - 1], previousDirection, path[i], direction, IsBuildRoadBlockedCell, "player");
                }
            }
            return anyBuilt;
        });
    }

    private void UpdateRoadPathPreview(Vector2Int hoverCell, Vector2Int startCell)
    {
        List<Vector2Int> path = FindRoadBuildPath(startCell, hoverCell, IsBuildRoadBlockedCell);
        bool hasPath = path != null && path.Count > 1;
        bool pathBuildable = hasPath;

        ClearBuildRoadPreviewCells();
        if (hasPath)
        {
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int direction = GetRoadPathPreviewDirection(path, i);
                bool canPlacePathCell = activeBuildTool == BuildTool.SingleRoad
                    ? CanPlaceSingleRoadCell(path[i], requireNewRoadCell: false)
                    : CanPlaceRoadFootprint(path[i], direction, requireNewRoadCell: false);
                if (!canPlacePathCell)
                {
                    pathBuildable = false;
                }

                AddRoadPreviewForActiveRoadTool(path[i], direction);
            }
        }
        else if (IsInsideGrid(hoverCell) && hoverCell != startCell)
        {
            Vector2Int direction = GetBuildRoadDirection();
            pathBuildable = activeBuildTool == BuildTool.SingleRoad
                ? CanPlaceSingleRoadCell(hoverCell, requireNewRoadCell: false)
                : CanPlaceRoadFootprint(hoverCell, direction, requireNewRoadCell: false);
            AddRoadPreviewForActiveRoadTool(hoverCell, direction);
        }

        EnsureBuildFootprintHoverCount(buildPreviewFootprintCells.Count);

        for (int i = 0; i < buildHoverCellHighlights.Count; i++)
        {
            bool active = i < buildPreviewFootprintCells.Count && IsInsideGrid(buildPreviewFootprintCells[i]);
            buildHoverCellHighlights[i].SetActive(active);
            if (!active) continue;

            Vector2Int c = buildPreviewFootprintCells[i];
            Vector3 worldCenter = GetCellCenter(c);
            buildHoverCellHighlights[i].transform.position = new Vector3(worldCenter.x, SampleTerrainHeight(worldCenter.x, worldCenter.z) + RoadHeight + 0.05f, worldCenter.z);
            Vector2Int direction = i < buildPreviewRoadDirections.Count ? buildPreviewRoadDirections[i] : GetBuildRoadDirection();
            SetRoadPreviewTileOrientation(buildHoverCellHighlights[i], direction);
            SetRoadPreviewTileVisual(buildHoverCellHighlights[i], pathBuildable);
        }

        buildHoverHighlight.SetActive(false);
        buildHoverDrivewayHighlight.SetActive(false);

        // Keep start marker positioned and visible
        Vector2Int startDirection = hasPath && path.Count > 1 ? path[1] - path[0] : GetAdjacentRoadPreviewDirection(startCell);
        SetRoadPathStartHighlights(startCell, startDirection, pathBuildable);
    }

    private void SetRoadPathStartHighlights(Vector2Int startCell, Vector2Int direction, bool canBuild)
    {
        Vector2Int dir = NormalizeRoadDirection(direction);
        SetRoadPathStartHighlightTile(roadPathStartHighlight, startCell, dir, canBuild);
        if (activeBuildTool == BuildTool.SingleRoad)
        {
            if (roadPathStartSideHighlight != null)
            {
                roadPathStartSideHighlight.SetActive(false);
            }
            return;
        }

        SetRoadPathStartHighlightTile(roadPathStartSideHighlight, startCell + GetRoadWidthOffset(dir), dir, canBuild);
    }

    private void SetRoadPathStartHighlightTile(GameObject highlight, Vector2Int cell, Vector2Int direction, bool canBuild)
    {
        if (highlight == null)
        {
            return;
        }

        Vector3 world = GetCellCenter(cell);
        highlight.transform.position = new Vector3(world.x, SampleTerrainHeight(world.x, world.z) + RoadHeight + 0.065f, world.z);
        SetRoadPreviewTileOrientation(highlight, direction);
        SetRoadPreviewTileVisual(highlight, canBuild, true);
        highlight.SetActive(IsInsideGrid(cell));
    }

    private Vector2Int GetRoadPathPreviewDirection(List<Vector2Int> path, int pathIndex)
    {
        return WorldLayoutRoadValidator.GetPathDirection(path, pathIndex);
    }

    private void CancelRoadPathMode()
    {
        roadPathStart = null;
        if (roadPathStartHighlight != null)
            roadPathStartHighlight.SetActive(false);
        if (roadPathStartSideHighlight != null)
            roadPathStartSideHighlight.SetActive(false);
        HideBuildFootprintHoverHighlights();
    }

    private void CompleteBuildingPlacementFlow(BuildTool placedTool, Vector2Int anchorCell)
    {
        if (placedTool == BuildTool.CityPark)
        {
            activeBuildTool = BuildTool.None;
            hoveredBuildCell = null;
            HideBuildHoverHighlights();
            isBuildScreenDirty = true;
            RefreshSelectionVisuals();
            return;
        }

        bool entranceConnected = IsBuildingEntranceConnectedToRoad(anchorCell);
        if (entranceConnected)
        {
            activeBuildTool = BuildTool.None;
            hoveredBuildCell = null;
            HideBuildHoverHighlights();
            isBuildScreenDirty = true;
            RefreshSelectionVisuals();
            SessionDebugLogger.Log("BUILD", $"{placedTool} entrance at ({anchorCell.x},{anchorCell.y}) is connected to road. Build mode cleared.");
            return;
        }

        activeBuildTool = BuildTool.Road;
        hoveredBuildCell = null;
        isBuildPanelOpen = true;
        isBuildScreenDirty = true;
        UpdateBuildHoverHighlight();
        RefreshSelectionVisuals();
        SessionDebugLogger.Log("BUILD", $"{placedTool} entrance at ({anchorCell.x},{anchorCell.y}) is not connected to road. Switched to Road build tool.");
    }

    private bool IsBuildingEntranceConnectedToRoad(Vector2Int anchorCell)
    {
        foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(anchorCell))
        {
            if (!IsInsideGrid(neighbor))
            {
                continue;
            }

            if (roadCells.Contains(neighbor) || edgeHighwayCells.Contains(neighbor))
            {
                return true;
            }
        }

        return false;
    }

    private bool GetBuildPreviewAtCell(Vector2Int cell, out Vector3 previewPosition, out Vector3 previewScale)
    {
        ClearBuildRoadPreviewCells();
        buildPreviewDrivewayCell = null;
        previewPosition = GetCellCenter(cell) + new Vector3(0f, RoadHeight + 0.03f, 0f);
        previewScale = new Vector3(0.92f, 0.04f, 0.92f);

        if (IsRoadBuildTool(activeBuildTool))
        {
            Vector2Int roadDirection = GetBuildRoadDirection();
            AddRoadPreviewForActiveRoadTool(cell, roadDirection);
            bool canBuildRoad = activeBuildTool == BuildTool.SingleRoad
                ? CanPlaceSingleRoadCell(cell, requireNewRoadCell: true)
                : CanPlaceRoadFootprint(cell, roadDirection, requireNewRoadCell: true);
            if (!canBuildRoad)
            {
                previewScale = new Vector3(0.98f, 0.04f, 0.98f);
            }

            return canBuildRoad;
        }

        if (activeBuildTool == BuildTool.FurnitureFactory)
        {
            return GetFurnitureFactoryPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.Parking)
        {
            return GetParkingPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.Warehouse)
        {
            return GetWarehousePlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.Stop)
        {
            return GetStopPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.Forest)
        {
            return GetForestPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.Sawmill)
        {
            return GetSawmillPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.Motel)
        {
            return GetMotelPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.Bar)
        {
            return GetBarPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.Canteen)
        {
            return GetCanteenPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.GamblingHall)
        {
            return GetGamblingHallPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.CityPark)
        {
            return GetCityParkPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.PersonalHouse)
        {
            return GetPersonalHousePlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.CarMarket)
        {
            return GetCarMarketPlacementPreview(cell, out previewPosition, out previewScale);
        }

        return false;
    }

}


