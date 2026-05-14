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

        CloseBuildMenuFromWorldClick();

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
        if (TryRejectBuildToolForInsufficientFunds(placementTool))
        {
            return;
        }

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
            BuildTool.Kiosk => TryPlaceKioskAtAnchor(cell),
            BuildTool.CleaningDepot => TryPlaceCleaningDepotAtAnchor(cell),
            BuildTool.GasStation => TryPlaceGasStationAtAnchor(cell),
            BuildTool.GamblingHall => TryPlaceGamblingHallAtAnchor(cell),
            BuildTool.CityPark        => TryPlaceCityParkAtAnchor(cell),
            BuildTool.PersonalHouse   => TryPlacePersonalHouseAtAnchor(cell),
            BuildTool.Kindergarten    => TryPlaceKindergartenAtAnchor(cell),
            BuildTool.PrimarySchool   => TryPlacePrimarySchoolAtAnchor(cell),
            BuildTool.SecondarySchool => TryPlaceSecondarySchoolAtAnchor(cell),
            BuildTool.CarMarket       => TryPlaceCarMarketAtAnchor(cell),
            BuildTool.LaborExchange   => TryPlaceLaborExchangeAtAnchor(cell),
            BuildTool.CityHall        => TryPlaceCityHallAtAnchor(cell),
            BuildTool.Docks           => TryPlaceDocksAtAnchor(cell),
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
            SpendBuildToolConstructionCost(placementTool, cell);
            PlayUiSound(buildingCompleteClip, 0.86f);
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

    private void SpendBuildToolConstructionCost(BuildTool tool, Vector2Int anchorCell)
    {
        int cost = GetBuildToolCost(tool);
        if (cost <= 0)
        {
            return;
        }

        string titleEn = GetBuildCatalogTitle(tool, false, tool.ToString());
        string titleRu = GetBuildCatalogTitle(tool, true, titleEn);
        money -= cost;
        RecordMoneyMovement(
            -cost,
            "Treasury",
            "Construction Crew",
            $"Construction: {titleEn}",
            money,
            null,
            MoneyAccountKind.CityBudget,
            MoneyAccountKind.External,
            MoneyTransactionReasonKind.Construction);
        ApplyConstructionPermitTaxes(cost, "Construction Crew", $"Construction: {titleEn}");
        PushFeedEvent(
            $"Built {titleEn}: -${cost}.",
            $"\u041f\u043e\u0441\u0442\u0440\u043e\u0435\u043d\u043e: {titleRu}. -${cost}.",
            FeedEventType.Money);
        SpawnMoneySpendPopup(GetBuildCostPopupPosition(tool, anchorCell), cost);
        moneyPopupAmount = -cost;
        moneyPopupTimer = MoneyPopupDuration;
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isEconomyScreenDirty = true;
        isBuildScreenDirty = true;
        SessionDebugLogger.Log("BUILD", $"Construction paid for {tool}: cost=${cost}, treasury=${money}.");
    }

    private Vector3 GetBuildCostPopupPosition(BuildTool tool, Vector2Int anchorCell)
    {
        if (TryGetBuildToolLocationType(tool, out LocationType type) &&
            TryFindNewestBuiltLocation(type, anchorCell, out LocationData location))
        {
            float x = (location.Min.x + location.Max.x + 1) * 0.5f;
            float z = (location.Min.y + location.Max.y + 1) * 0.5f;
            return new Vector3(x, SampleTerrainHeight(x, z) + 1.0f, z);
        }

        Vector3 fallback = GetCellCenter(anchorCell);
        fallback.y = SampleTerrainHeight(fallback.x, fallback.z) + 1.0f;
        return fallback;
    }

    private bool TryFindNewestBuiltLocation(LocationType type, Vector2Int anchorCell, out LocationData result)
    {
        LocationData best = null;
        int bestScore = int.MinValue;
        void Consider(LocationData location)
        {
            if (location == null || location.Type != type)
            {
                return;
            }

            int score = location.InstanceId;
            if (location.Anchor == anchorCell)
            {
                score += 2000000;
            }
            else if (location.RoadAccess == anchorCell || location.Contains(anchorCell))
            {
                score += 1000000;
            }

            if (score <= bestScore)
            {
                return;
            }

            bestScore = score;
            best = location;
        }

        foreach (LocationData location in locations.Values) Consider(location);
        for (int i = 0; i < extraServiceLocations.Count; i++) Consider(extraServiceLocations[i]);
        for (int i = 0; i < localStops.Count; i++) Consider(localStops[i]);
        for (int i = 0; i < personalHouses.Count; i++) Consider(personalHouses[i]);
        result = best;
        return result != null;
    }

    private bool TryHandleRoadPlacement(Vector2Int cell)
    {
        if (activeBuildTool == BuildTool.Road)
        {
            return TryHandleTwoLaneRoadSegmentPlacement(cell);
        }

        if (!roadPathStart.HasValue)
        {
            if (!IsInsideGrid(cell))
            {
                SessionDebugLogger.Log("BUILD_ROAD", $"path-start rejected cell={FormatCell(cell)} reason=outside-grid.");
                return false;
            }
            Vector2Int startDirection = GetAdjacentRoadPreviewDirection(cell);
            bool canStart = activeBuildTool == BuildTool.SingleRoad
                ? CanPlaceSingleRoadCell(cell, requireNewRoadCell: false)
                : CanPlaceRoadFootprint(cell, startDirection, requireNewRoadCell: false);
            if (!canStart)
            {
                SessionDebugLogger.Log("BUILD_ROAD", $"path-start rejected tool={activeBuildTool} cell={FormatCell(cell)} dir={FormatCell(startDirection)} reason={GetRoadBuildBlockReason(cell)}.");
                return false;
            }
            roadPathStart = cell;
            SetRoadPathStartHighlights(cell, startDirection, true);
            PlayUiSound(buildPlaceStartClip != null ? buildPlaceStartClip : uiSelectClip, 0.65f);
            MarkTutorialGoalComplete(TutorialGoalKind.RoadSegmentStart);
            SessionDebugLogger.Log("BUILD_ROAD", $"path-start accepted tool={activeBuildTool} cell={FormatCell(cell)} dir={FormatCell(startDirection)}.");
            return false;
        }

        SessionDebugLogger.Log("BUILD_ROAD", $"path-finish requested tool={activeBuildTool} start={FormatCell(roadPathStart.Value)} requestedEnd={FormatCell(cell)} axisLocked={IsActiveRoadSegmentAxisLocked()} previewFootprintCells={FormatCellList(buildPreviewFootprintCells)}.");
        bool built = TryBuildRoadPath(roadPathStart.Value, cell);
        if (built)
        {
            PlayUiSound(roadDragClip, 0.84f);
            MarkTutorialGoalComplete(TutorialGoalKind.RoadSegmentEnd);
        }
        roadPathStart = null; // clear before CancelRoadPathMode so it doesn't log a spurious path-cancel
        CancelRoadPathMode();
        return built;
    }

    private bool TryBuildRoadPath(Vector2Int start, Vector2Int end)
    {
        return RunBatchedRoadPlacement(() =>
        {
            if (start == end)
            {
                SessionDebugLogger.Log("BUILD_ROAD", $"path-build same-cell start={FormatCell(start)} tool={activeBuildTool} previewFootprintCells={FormatCellList(buildPreviewFootprintCells)} lanePairId=single.");
                HashSet<Vector2Int> sameCellRoadsBeforeBuild = new(roadCells);
                bool builtSameCell = activeBuildTool == BuildTool.SingleRoad
                    ? TryPlaceSingleRoadCell(start, "player-path")
                    : TryPlaceRoadFootprint(start, GetBuildRoadDirection(), "player-path");
                if (builtSameCell)
                {
                    StartRoadConstructionWave(new[] { start }, CollectNewRoadCells(sameCellRoadsBeforeBuild));
                }

                return builtSameCell;
            }

            List<Vector2Int> path = GetRoadBuildToolPath(start, end);
            if (path == null || path.Count < 2)
            {
                SessionDebugLogger.Log("BUILD_ROAD", $"path-build rejected start={FormatCell(start)} requestedEnd={FormatCell(end)} reason=no-path.");
                return false;
            }

            if (activeBuildTool == BuildTool.Road && !CanCommitTwoLaneRoadSegmentPath(path, out string blockedReason))
            {
                SessionDebugLogger.Log("BUILD_ROAD", $"segment-build rejected start={FormatCell(start)} requestedEnd={FormatCell(end)} resolvedEnd={FormatCell(path[^1])} axisLocked={IsActiveRoadSegmentAxisLocked()} reason={blockedReason} path={FormatCellList(path)} previewFootprintCells={FormatCellList(buildPreviewFootprintCells)}.");
                return false;
            }

            HashSet<Vector2Int> roadsBeforeBuild = new(roadCells);
            List<Vector2Int> turnFillCells = new();
            List<string> lanePairIds = new();
            Vector2Int[] pathOffsets = activeBuildTool == BuildTool.SingleRoad ? null : new Vector2Int[path.Count];
            bool[] pathOffsetResolved = activeBuildTool == BuildTool.SingleRoad ? null : new bool[path.Count];
            SessionDebugLogger.Log("BUILD_ROAD", $"path-build start={FormatCell(start)} requestedEnd={FormatCell(end)} resolvedEnd={FormatCell(path[^1])} axisLocked={IsActiveRoadSegmentAxisLocked()} cells={path.Count} tool={activeBuildTool} path={FormatCellList(path)} previewFootprintCells={FormatCellList(buildPreviewFootprintCells)}.");

            bool anyBuilt = false;
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int direction = GetRoadPathPreviewDirection(path, i);
                if (activeBuildTool == BuildTool.SingleRoad)
                {
                    anyBuilt |= TryPlaceSingleRoadCell(path[i], "player-path");
                    continue;
                }

                List<(Vector2Int Direction, Vector2Int Offset)> junctionTurns = null;
                if (i == 0)
                {
                    Vector2Int behindCell = path[0] - direction;
                    if (roadCells.Contains(behindCell))
                    {
                        junctionTurns = new List<(Vector2Int Direction, Vector2Int Offset)>();
                        foreach (Vector2Int previousDirection in RoadBuildPlacementService.GetConnectedPerpendicularDirections(behindCell, direction, roadCells))
                        {
                            junctionTurns.Add((previousDirection, GetExistingRoadWidthOffset(behindCell, previousDirection)));
                        }
                    }
                }

                Vector2Int widthOffset = Vector2Int.zero;
                bool hasWidthOffset = false;
                if (TryResolveContinuingPathRoadFootprintOffset(path, i, direction, IsBuildRoadBlockedCell, out Vector2Int continuingOffset))
                {
                    widthOffset = continuingOffset;
                    hasWidthOffset = true;
                }
                else if (TryResolveRoadFootprintOffset(path[i], direction, requireNewRoadCell: true, IsBuildRoadBlockedCell, out Vector2Int resolvedOffset))
                {
                    widthOffset = resolvedOffset;
                    hasWidthOffset = true;
                }

                if (hasWidthOffset)
                {
                    pathOffsets[i] = widthOffset;
                    pathOffsetResolved[i] = true;
                    AddUniqueDebugValue(lanePairIds, FormatRoadLanePairId(path[i], widthOffset, direction));
                    anyBuilt |= TryPlaceRoadFootprintWithOffset(path[i], direction, widthOffset, "player-path");
                }
                else
                {
                    SessionDebugLogger.Log("BUILD_ROAD", $"player-path skipped footprint cell={FormatCell(path[i])} dir={FormatCell(direction)} reason={GetRoadBuildBlockReason(path[i])}.");
                }

                if (i > 0 && pathOffsetResolved[i - 1] && pathOffsetResolved[i])
                {
                    Vector2Int previousDirection = GetRoadPathPreviewDirection(path, i - 1);
                    anyBuilt |= TryFillRoadTurnFootprint(path[i - 1], previousDirection, pathOffsets[i - 1], path[i], direction, pathOffsets[i], IsBuildRoadBlockedCell, "player", turnFillCells);
                }
                else if (i == 0 && junctionTurns != null && pathOffsetResolved[i])
                {
                    Vector2Int behindCell = path[0] - direction;
                    foreach ((Vector2Int previousDirection, Vector2Int previousOffset) in junctionTurns)
                    {
                        anyBuilt |= TryFillRoadTurnFootprint(behindCell, previousDirection, previousOffset, path[0], direction, pathOffsets[i], IsBuildRoadBlockedCell, "player-junction", turnFillCells);
                    }
                }
            }

            List<Vector2Int> newRoadCells = CollectNewRoadCells(roadsBeforeBuild);

            if (anyBuilt)
            {
                StartRoadConstructionWave(path, newRoadCells);
                List<Vector2Int> refreshCells = new(path);
                refreshCells.AddRange(buildPreviewFootprintCells);
                refreshCells.AddRange(newRoadCells);
                refreshCells.AddRange(turnFillCells);
                RefreshRoadConnectivityAround(refreshCells);
            }

            SessionDebugLogger.Log(
                "BUILD_ROAD",
                $"path-build result start={FormatCell(start)} requestedEnd={FormatCell(end)} resolvedEnd={FormatCell(path[^1])} axisLocked={IsActiveRoadSegmentAxisLocked()} built={anyBuilt} newRoadCells={FormatCellList(newRoadCells)} turnFillCells={FormatCellList(turnFillCells)} lanePairId={FormatStringList(lanePairIds)} previewFootprintCells={FormatCellList(buildPreviewFootprintCells)}.");
            return anyBuilt;
        });
    }

    private void UpdateRoadPathPreview(Vector2Int hoverCell, Vector2Int startCell)
    {
        List<Vector2Int> path = GetRoadBuildToolPath(startCell, hoverCell);
        bool hasPath = path != null && path.Count > 1;
        bool pathBuildable = hasPath;

        ClearBuildRoadPreviewCells();
        if (hasPath)
        {
            Vector2Int[] pathOffsets = activeBuildTool == BuildTool.SingleRoad ? null : new Vector2Int[path.Count];
            for (int i = 0; i < path.Count; i++)
            {
                Vector2Int direction = GetRoadPathPreviewDirection(path, i);
                bool canPlacePathCell;
                Vector2Int widthOffset = Vector2Int.zero;
                if (activeBuildTool == BuildTool.SingleRoad)
                {
                    canPlacePathCell = CanPlaceSingleRoadCell(path[i], requireNewRoadCell: false);
                }
                else if (TryResolveContinuingPathRoadFootprintOffset(path, i, direction, IsBuildRoadBlockedCell, out Vector2Int continuingOffset))
                {
                    widthOffset = continuingOffset;
                    canPlacePathCell = CanPlaceRoadFootprintWithOffset(path[i], widthOffset, requireNewRoadCell: false, IsBuildRoadBlockedCell);
                }
                else
                {
                    canPlacePathCell = TryResolveRoadFootprintOffset(path[i], direction, requireNewRoadCell: false, IsBuildRoadBlockedCell, out widthOffset);
                }

                if (!canPlacePathCell)
                {
                    pathBuildable = false;
                }

                if (activeBuildTool == BuildTool.SingleRoad)
                {
                    AddRoadPreviewCell(path[i], direction);
                }
                else
                {
                    pathOffsets[i] = widthOffset;
                    AddRoadPreviewFootprintWithOffset(path[i], direction, widthOffset);
                }

                if (activeBuildTool != BuildTool.SingleRoad && i > 0)
                {
                    Vector2Int previousDirection = GetRoadPathPreviewDirection(path, i - 1);
                    AddRoadTurnPreviewFootprint(path[i - 1], previousDirection, pathOffsets[i - 1], path[i], direction, pathOffsets[i]);
                }
                else if (activeBuildTool != BuildTool.SingleRoad)
                {
                    Vector2Int behindCell = path[0] - direction;
                    if (roadCells.Contains(behindCell))
                    {
                        foreach (Vector2Int previousDirection in RoadBuildPlacementService.GetConnectedPerpendicularDirections(behindCell, direction, roadCells))
                        {
                            AddRoadTurnPreviewFootprint(behindCell, previousDirection, GetExistingRoadWidthOffset(behindCell, previousDirection), path[0], direction, widthOffset);
                        }
                    }
                }
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
            buildHoverCellHighlights[i].transform.localScale = new Vector3(0.86f, 0.045f, 0.86f);
            Vector2Int direction = i < buildPreviewRoadDirections.Count ? buildPreviewRoadDirections[i] : GetBuildRoadDirection();
            SetRoadPreviewTileOrientation(buildHoverCellHighlights[i], direction);
            SetRoadPreviewTileVisual(buildHoverCellHighlights[i], pathBuildable);
        }

        buildHoverHighlight.SetActive(false);
        buildHoverDrivewayHighlight.SetActive(false);
        UpdateBuildCursorAssistFromPreview(pathBuildable);

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
        if (roadPathStart.HasValue)
        {
            SessionDebugLogger.Log("BUILD_ROAD", $"path-cancel start={FormatCell(roadPathStart.Value)}.");
        }
        roadPathStart = null;
        if (roadPathStartHighlight != null)
            roadPathStartHighlight.SetActive(false);
        if (roadPathStartSideHighlight != null)
            roadPathStartSideHighlight.SetActive(false);
        HideBuildFootprintHoverHighlights();
    }

    private void CompleteBuildingPlacementFlow(BuildTool placedTool, Vector2Int anchorCell)
    {
        if (IsRoadlessBuildTool(placedTool))
        {
            activeBuildTool = BuildTool.None;
            hoveredBuildCell = null;
            HideBuildHoverHighlights();
            isBuildScreenDirty = true;
            RefreshSelectionVisuals();
            return;
        }

        Vector2Int connectionCell = GetPlacedBuildingConnectionCell(placedTool, anchorCell);
        string connectionLabel = placedTool == BuildTool.Docks ? "road access" : "entrance";
        bool entranceConnected = IsBuildingEntranceConnectedToRoad(connectionCell);
        if (entranceConnected)
        {
            activeBuildTool = BuildTool.None;
            hoveredBuildCell = null;
            HideBuildHoverHighlights();
            isBuildScreenDirty = true;
            RefreshSelectionVisuals();
            SessionDebugLogger.Log("BUILD", $"{placedTool} {connectionLabel} at ({connectionCell.x},{connectionCell.y}) is connected to road. Build mode cleared.");
            return;
        }

        activeBuildTool = BuildTool.SingleRoad;
        hoveredBuildCell = null;
        isBuildPanelOpen = true;
        isSocialGraphPanelOpen = false;
        isBuildScreenDirty = true;
        isSocialGraphScreenDirty = true;
        UpdateBuildHoverHighlight();
        RefreshSelectionVisuals();
        SessionDebugLogger.Log("BUILD", $"{placedTool} {connectionLabel} at ({connectionCell.x},{connectionCell.y}) is not connected to road. Switched to Road build tool.");
    }

    private Vector2Int GetPlacedBuildingConnectionCell(BuildTool placedTool, Vector2Int fallbackCell)
    {
        if (TryGetBuildToolLocationType(placedTool, out LocationType locationType) &&
            TryFindNewestBuiltLocation(locationType, fallbackCell, out LocationData location))
        {
            return location.RoadAccess;
        }

        return fallbackCell;
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

        if (activeBuildTool == BuildTool.Kiosk)
        {
            return GetKioskPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.CleaningDepot)
        {
            return GetCleaningDepotPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.GasStation)
        {
            return GetGasStationPlacementPreview(cell, out previewPosition, out previewScale);
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

        if (activeBuildTool == BuildTool.Kindergarten)
        {
            return GetKindergartenPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.PrimarySchool)
        {
            return GetPrimarySchoolPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.SecondarySchool)
        {
            return GetSecondarySchoolPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.CarMarket)
        {
            return GetCarMarketPlacementPreview(cell, out previewPosition, out previewScale);
        }

        if (activeBuildTool == BuildTool.LaborExchange)
        {
            return GetLaborExchangePlacementPreview(cell, out previewPosition, out previewScale);
        }
        if (activeBuildTool == BuildTool.CityHall)
        {
            return GetCityHallPlacementPreview(cell, out previewPosition, out previewScale);
        }
        if (activeBuildTool == BuildTool.Docks)
        {
            return GetDocksPlacementPreview(cell, out previewPosition, out previewScale);
        }

        return false;
    }

}


