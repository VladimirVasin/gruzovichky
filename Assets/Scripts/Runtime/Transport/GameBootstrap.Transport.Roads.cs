using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void StartMoveTo(Vector2Int destination)
    {
        List<Vector2Int> path = FindPath(truckCell, destination);
        if (path == null || path.Count < 2)
        {
            SessionDebugLogger.Log("PATH", $"{GetLoadedTruckDisplayName()} failed to find path from ({truckCell.x},{truckCell.y}) to ({destination.x},{destination.y}).");
            return;
        }

        activePath.Clear();
        for (int i = 1; i < path.Count; i++)
        {
            activePath.Add(path[i]);
        }

        isTruckMoving = true;
        BeginNextTruckSegment(activePath[0]);
        SessionDebugLogger.Log("PATH", $"{GetLoadedTruckDisplayName()} started moving from ({truckCell.x},{truckCell.y}) to ({destination.x},{destination.y}) over {activePath.Count} steps.");
    }

    private bool HasPath(Vector2Int start, Vector2Int goal)
    {
        return FindPath(start, goal) != null;
    }

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        LocationType? startLocation = GetContainingLocation(start);
        LocationType? goalLocation = GetContainingLocation(goal);
        return GridPathService.FindPath(
            start,
            goal,
            GridPathService.GetCardinalNeighbors,
            neighbor => IsDriveableForPath(neighbor, startLocation, goalLocation));
    }

    private List<Vector2Int> FindRoadBuildPath(Vector2Int start, Vector2Int goal, System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        return GridPathService.FindPath(
            start,
            goal,
            GridPathService.GetCardinalNeighbors,
            neighbor => CanBuildRoadThroughCell(neighbor, start, goal, isBlockedLocationCell));
    }

    private bool IsDriveable(Vector2Int cell)
    {
        return IsInsideGrid(cell) && (roadCells.Contains(cell) || edgeHighwayCells.Contains(cell) || IsAnchorCell(cell));
    }

    private bool CanBuildRoadThroughCell(Vector2Int cell, Vector2Int start, Vector2Int goal, System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        if (!IsInsideGrid(cell))
        {
            return false;
        }

        if (cell == start || cell == goal || roadCells.Contains(cell))
        {
            return true;
        }

        return !isBlockedLocationCell(cell);
    }

    private bool IsDriveableForPath(Vector2Int cell, LocationType? startLocation, LocationType? goalLocation)
    {
        return IsDriveable(cell);
    }

    private bool IsAnchorCell(Vector2Int cell)
    {
        foreach (LocationData location in locations.Values)
        {
            if (location.Anchor == cell)
            {
                return true;
            }
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            if (localStops[i].Anchor == cell)
            {
                return true;
            }
        }

        return false;
    }

    private LocationType? GetContainingLocation(Vector2Int cell)
    {
        foreach (KeyValuePair<LocationType, LocationData> pair in locations)
        {
            if (pair.Value.Contains(cell) || pair.Value.Anchor == cell)
            {
                return pair.Key;
            }
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            if (localStops[i].Contains(cell) || localStops[i].Anchor == cell)
            {
                return LocationType.Stop;
            }
        }

        return null;
    }

    private bool IsLocationCell(Vector2Int cell)
    {
        foreach (LocationData location in locations.Values)
        {
            if (location.Contains(cell) || location.Anchor == cell)
            {
                return true;
            }
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            if (localStops[i].Contains(cell) || localStops[i].Anchor == cell)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsRoadBuildCellBlocked(Vector2Int cell)
    {
        return !IsInsideGrid(cell) || IsLocationCell(cell) || roadCells.Contains(cell) || edgeHighwayCells.Contains(cell) || miscOccupiedCells.Contains(cell);
    }

    private void AddRoad(Vector2Int cell)
    {
        if (IsRoadBuildCellBlocked(cell))
        {
            return;
        }

        roadCells.Add(cell);

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = $"Road_{cell.x}_{cell.y}";
        road.transform.SetParent(roadsRoot, false);
        road.transform.position = GetCellCenter(cell) + new Vector3(0f, RoadHeight, 0f);
        road.transform.localScale = new Vector3(1.04f, 0.18f, 1.04f);
        ApplyColor(road, new Color(0.18f, 0.19f, 0.21f));
        ConfigureStaticVisual(road);

        GameObject roadTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadTop.name = "RoadTop";
        roadTop.transform.SetParent(road.transform, false);
        roadTop.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        roadTop.transform.localScale = new Vector3(0.84f, 0.16f, 0.84f);
        ApplyColor(roadTop, new Color(0.76f, 0.71f, 0.58f));
        ConfigureStaticVisual(roadTop);
        roadVisuals[cell] = road;

        RefreshRoadVisual(cell);
        foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(cell))
        {
            if (roadVisuals.ContainsKey(neighbor))
            {
                RefreshRoadVisual(neighbor);
            }
        }

        RefreshRoadLanternsAround(cell);
        RefreshRoadsideBenchesAround(cell);
        SessionDebugLogger.Log("ROAD", $"Added road at cell ({cell.x},{cell.y}).");
    }

    private void RemoveRoad(Vector2Int cell)
    {
        if (!roadCells.Remove(cell))
        {
            return;
        }

        if (roadVisuals.TryGetValue(cell, out GameObject road))
        {
            roadVisuals.Remove(cell);
            Destroy(road);
        }

        foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(cell))
        {
            if (roadVisuals.ContainsKey(neighbor))
            {
                RefreshRoadVisual(neighbor);
            }
        }

        RefreshRoadLanternsAround(cell);
        RefreshRoadsideBenchesAround(cell);
        SessionDebugLogger.Log("ROAD", $"Removed road at cell ({cell.x},{cell.y}).");
    }

    private void GenerateInitialRoadNetwork()
    {
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.Parking, LocationType.GasStation);
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.GasStation, LocationType.Warehouse);
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.Warehouse, LocationType.Forest);
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.Forest, LocationType.Sawmill);
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.Sawmill, LocationType.Warehouse);
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.Warehouse, LocationType.Motel);
        CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.Motel, LocationType.IntercityStop);
        if (!locations.ContainsKey(LocationType.Motel))
        {
            CreateGuaranteedRoadConnectionIfLocationsExist(LocationType.Warehouse, LocationType.IntercityStop);
        }

        SessionDebugLogger.Log("ROAD", $"Generated starter road network with {roadCells.Count} road cells.");
    }

    private void CreateGuaranteedRoadConnectionIfLocationsExist(LocationType startType, LocationType endType)
    {
        if (!locations.TryGetValue(startType, out LocationData start) ||
            !locations.TryGetValue(endType, out LocationData end))
        {
            return;
        }

        CreateGuaranteedRoadConnection(start.Anchor, end.Anchor);
    }

    private void CreateGuaranteedRoadConnection(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = FindRoadBuildPath(start, end, IsLocationCell);
        if (path == null)
        {
            return;
        }

        foreach (Vector2Int cell in path)
        {
            TryAddStarterRoadCell(cell, start, end);
        }
    }

    private void TryAddStarterRoadCell(Vector2Int cell, Vector2Int start, Vector2Int end)
    {
        if (cell == start || cell == end)
        {
            return;
        }

        AddRoad(cell);
    }

    private void RefreshRoadVisual(Vector2Int cell)
    {
        if (!roadVisuals.TryGetValue(cell, out GameObject road))
        {
            return;
        }

        bool horizontal = ConnectsToRoadOrAnchor(cell, new Vector2Int(1, 0)) || ConnectsToRoadOrAnchor(cell, new Vector2Int(-1, 0));
        bool vertical = ConnectsToRoadOrAnchor(cell, new Vector2Int(0, 1)) || ConnectsToRoadOrAnchor(cell, new Vector2Int(0, -1));

        Vector3 scale = road.transform.localScale;
        scale.x = horizontal ? 1.12f : 0.82f;
        scale.z = vertical ? 1.12f : 0.82f;
        road.transform.localScale = scale;

        if (road.transform.childCount > 0)
        {
            Transform roadTop = road.transform.GetChild(0);
            Vector3 topScale = roadTop.localScale;
            topScale.x = horizontal ? 0.92f : 0.62f;
            topScale.z = vertical ? 0.92f : 0.62f;
            roadTop.localScale = topScale;
        }
    }

    private void RebuildRoadLanterns()
    {
        if (lanternsRoot == null)
        {
            return;
        }

        for (int i = lanternsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(lanternsRoot.GetChild(i).gameObject);
        }

        roadLanterns.Clear();
        roadCellLanternMap.Clear();

        foreach (Vector2Int roadCell in roadCells)
        {
            if (!TryGetRoadLanternPlacement(roadCell, out Vector3 worldPosition, out Quaternion worldRotation))
            {
                continue;
            }

            CreateRoadLanternForCell(roadCell, worldPosition, worldRotation);
        }

        RebuildEdgeHighwayLanterns();
        RefreshAmbientLanternMoths();
    }

    private void RefreshRoadLanternsAround(Vector2Int cell)
    {
        if (lanternsRoot == null) return;

        // Collect cell + road neighbors (their lantern eligibility may have changed)
        System.Collections.Generic.List<Vector2Int> toRefresh = new() { cell };
        foreach (Vector2Int n in GridPathService.GetCardinalNeighbors(cell))
            if (roadCells.Contains(n)) toRefresh.Add(n);

        // Remove affected road-cell lanterns
        foreach (Vector2Int c in toRefresh)
        {
            if (!roadCellLanternMap.TryGetValue(c, out var info)) continue;
            roadLanterns.Remove(info.Data);
            Destroy(info.Root);
            roadCellLanternMap.Remove(c);
        }

        // Re-evaluate and add where eligible
        foreach (Vector2Int c in toRefresh)
        {
            if (!roadCells.Contains(c)) continue;
            if (!TryGetRoadLanternPlacement(c, out Vector3 pos, out Quaternion rot)) continue;
            CreateRoadLanternForCell(c, pos, rot);
        }

        RefreshAmbientLanternMoths();
    }

    private void CreateRoadLanternForCell(Vector2Int cell, Vector3 worldPosition, Quaternion worldRotation)
    {
        CreateRoadLantern(worldPosition, worldRotation);
        roadCellLanternMap[cell] = (lanternsRoot.GetChild(lanternsRoot.childCount - 1).gameObject, roadLanterns[roadLanterns.Count - 1]);
    }

    private bool TryGetRoadLanternPlacement(Vector2Int cell, out Vector3 worldPosition, out Quaternion worldRotation)
    {
        if (edgeHighwayCells.Contains(cell + Vector2Int.up) ||
            edgeHighwayCells.Contains(cell + Vector2Int.down) ||
            edgeHighwayCells.Contains(cell + Vector2Int.left) ||
            edgeHighwayCells.Contains(cell + Vector2Int.right))
        {
            worldPosition = default;
            worldRotation = Quaternion.identity;
            return false;
        }

        return RoadLanternPlanner.TryGetPlacement(
            cell,
            neighbor => roadCells.Contains(neighbor) || IsAnchorCell(neighbor),
            IsLocationCell,
            GetCellCenter,
            out worldPosition,
            out worldRotation);
    }

    private bool ConnectsToRoadOrAnchor(Vector2Int cell, Vector2Int offset)
    {
        Vector2Int neighbor = cell + offset;
        return roadCells.Contains(neighbor) || IsAnchorCell(neighbor);
    }

    private void RebuildEdgeHighwayLanterns()
    {
        if (lanternsRoot == null)
        {
            return;
        }

        int lowerLaneY = 0;
        int upperLaneY = 1;
        float outerSouthZ = lowerLaneY + 0.06f;
        float outerNorthZ = upperLaneY + 0.94f;
        for (int x = 0; x < GridWidth; x += 2)
        {
            if (IsEdgeHighwayLanternSuppressedNearConnection(x))
            {
                continue;
            }

            Vector3 southPosition = new Vector3(x + 0.5f, GetTerrainHeight(new Vector2Int(x, lowerLaneY)) + 0.04f, outerSouthZ);
            CreateRoadLantern(southPosition, Quaternion.identity);

            Vector3 northPosition = new Vector3(x + 0.5f, GetTerrainHeight(new Vector2Int(x, upperLaneY)) + 0.04f, outerNorthZ);
            CreateRoadLantern(northPosition, Quaternion.Euler(0f, 180f, 0f));
        }
    }

    private bool IsEdgeHighwayLanternSuppressedNearConnection(int columnX)
    {
        for (int x = Mathf.Max(0, columnX - 1); x <= Mathf.Min(GridWidth - 1, columnX + 1); x++)
        {
            if (DoesEdgeHighwayColumnTouchRegularRoadConnection(x))
            {
                return true;
            }
        }

        return false;
    }

    private bool DoesEdgeHighwayColumnTouchRegularRoadConnection(int columnX)
    {
        return DoesEdgeHighwayCellTouchRegularRoadConnection(new Vector2Int(columnX, 0)) ||
               DoesEdgeHighwayCellTouchRegularRoadConnection(new Vector2Int(columnX, 1));
    }

    private bool DoesEdgeHighwayCellTouchRegularRoadConnection(Vector2Int edgeCell)
    {
        if (!edgeHighwayCells.Contains(edgeCell))
        {
            return false;
        }

        Vector2Int[] offsets =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            Vector2Int neighbor = edgeCell + offsets[i];
            if (!IsInsideGrid(neighbor) || edgeHighwayCells.Contains(neighbor))
            {
                continue;
            }

            if (roadCells.Contains(neighbor) || IsAnchorCell(neighbor))
            {
                return true;
            }
        }

        return false;
    }

    private void RebuildRoadsideBenches()
    {
        if (roadsidePropsRoot == null)
        {
            return;
        }

        for (int i = roadsidePropsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(roadsidePropsRoot.GetChild(i).gameObject);
        }

        roadsideBenchPositions.Clear();
        roadCellBenchMap.Clear();
        benchSideCells.Clear();

        foreach (Vector2Int roadCell in roadCells)
        {
            if (!TryGetRoadsideBenchPlacement(roadCell, benchSideCells, out Vector3 worldPosition, out Quaternion worldRotation, out Vector2Int sideCell))
            {
                continue;
            }

            CreateRoadsideBench(worldPosition, worldRotation, roadCell, sideCell);
            benchSideCells.Add(sideCell);
        }
    }

    private void RefreshRoadsideBenchesAround(Vector2Int cell)
    {
        if (roadsidePropsRoot == null) return;

        System.Collections.Generic.List<Vector2Int> toRefresh = new() { cell };
        foreach (Vector2Int n in GridPathService.GetCardinalNeighbors(cell))
            if (roadCells.Contains(n)) toRefresh.Add(n);

        // Remove existing benches for affected cells
        foreach (Vector2Int c in toRefresh)
        {
            if (!roadCellBenchMap.TryGetValue(c, out var info)) continue;
            benchSideCells.Remove(info.SideCell);
            roadsideBenchPositions.Remove(info.Root.transform.position);
            Destroy(info.Root);
            roadCellBenchMap.Remove(c);
        }

        // Re-evaluate each affected road cell
        foreach (Vector2Int c in toRefresh)
        {
            if (!roadCells.Contains(c)) continue;
            if (!TryGetRoadsideBenchPlacement(c, benchSideCells, out Vector3 pos, out Quaternion rot, out Vector2Int sideCell)) continue;
            CreateRoadsideBench(pos, rot, c, sideCell);
            benchSideCells.Add(sideCell);
        }
    }

    private bool TryGetRoadsideBenchPlacement(
        Vector2Int roadCell,
        HashSet<Vector2Int> reservedSideCells,
        out Vector3 worldPosition,
        out Quaternion worldRotation,
        out Vector2Int sideCell)
    {
        worldPosition = default;
        worldRotation = Quaternion.identity;
        sideCell = default;

        int connectivity = 0;
        bool north = ConnectsToRoadOrAnchor(roadCell, Vector2Int.up);
        bool south = ConnectsToRoadOrAnchor(roadCell, Vector2Int.down);
        bool east = ConnectsToRoadOrAnchor(roadCell, Vector2Int.right);
        bool west = ConnectsToRoadOrAnchor(roadCell, Vector2Int.left);
        connectivity += north ? 1 : 0;
        connectivity += south ? 1 : 0;
        connectivity += east ? 1 : 0;
        connectivity += west ? 1 : 0;
        if (connectivity != 2)
        {
            return false;
        }

        bool isVertical = north && south && !east && !west;
        bool isHorizontal = east && west && !north && !south;
        if (!isVertical && !isHorizontal)
        {
            return false;
        }

        int hash = Mathf.Abs(roadCell.x * 92821 + roadCell.y * 68917 + 17);
        if ((hash % 100) >= 15)
        {
            return false;
        }

        Vector2Int[] sideOffsets = isVertical
            ? ((hash & 1) == 0 ? new[] { Vector2Int.left, Vector2Int.right } : new[] { Vector2Int.right, Vector2Int.left })
            : ((hash & 1) == 0 ? new[] { Vector2Int.down, Vector2Int.up } : new[] { Vector2Int.up, Vector2Int.down });

        for (int i = 0; i < sideOffsets.Length; i++)
        {
            Vector2Int candidateSideCell = roadCell + sideOffsets[i];
            if (!CanPlaceRoadsideBenchInCell(candidateSideCell, reservedSideCells))
            {
                continue;
            }

            Vector3 center = GetCellCenter(roadCell);
            Vector3 sideDirection = new Vector3(sideOffsets[i].x, 0f, sideOffsets[i].y).normalized;
            Vector3 candidateWorld = center + sideDirection * 0.62f;
            candidateWorld.y = SampleTerrainHeight(candidateWorld.x, candidateWorld.z) + 0.02f;
            if (IsRoadsideBenchTooCloseToLantern(candidateWorld))
            {
                continue;
            }

            worldPosition = candidateWorld;
            Vector3 facingAwayFromRoad = sideDirection;
            worldRotation = Quaternion.LookRotation(facingAwayFromRoad, Vector3.up);
            sideCell = candidateSideCell;
            return true;
        }

        return false;
    }

    private bool CanPlaceRoadsideBenchInCell(Vector2Int cell, HashSet<Vector2Int> reservedSideCells)
    {
        return IsInsideGrid(cell) &&
               !roadCells.Contains(cell) &&
               !edgeHighwayCells.Contains(cell) &&
               !IsLocationCell(cell) &&
               !miscOccupiedCells.Contains(cell) &&
               !reservedSideCells.Contains(cell);
    }

    private bool IsRoadsideBenchTooCloseToLantern(Vector3 candidateWorld)
    {
        for (int i = 0; i < roadLanterns.Count; i++)
        {
            Light lantern = roadLanterns[i].Light;
            if (lantern == null)
            {
                continue;
            }

            Vector3 delta = lantern.transform.position - candidateWorld;
            delta.y = 0f;
            if (delta.sqrMagnitude < 0.72f * 0.72f)
            {
                return true;
            }
        }

        return false;
    }

    private void CreateRoadsideBench(Vector3 worldPosition, Quaternion worldRotation, Vector2Int roadCell, Vector2Int sideCell)
    {
        if (roadsidePropsRoot == null)
        {
            return;
        }

        GameObject benchRoot = new($"RoadsideBench_{roadCell.x}_{roadCell.y}");
        benchRoot.transform.SetParent(roadsidePropsRoot, false);
        benchRoot.transform.position = worldPosition;
        benchRoot.transform.rotation = worldRotation;

        int tintHash = Mathf.Abs(roadCell.x * 3343 + roadCell.y * 9283);
        Color woodColor = (tintHash & 1) == 0 ? new Color(0.58f, 0.41f, 0.24f) : new Color(0.46f, 0.34f, 0.2f);
        Color legColor = new Color(0.2f, 0.2f, 0.22f);

        GameObject seat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        seat.transform.SetParent(benchRoot.transform, false);
        seat.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        seat.transform.localScale = new Vector3(0.48f, 0.05f, 0.18f);
        ApplyColor(seat, woodColor);
        ConfigureShadowVisual(seat);

        GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
        back.transform.SetParent(benchRoot.transform, false);
        back.transform.localPosition = new Vector3(0f, 0.31f, -0.07f);
        back.transform.localScale = new Vector3(0.48f, 0.17f, 0.04f);
        ApplyColor(back, woodColor * 0.96f);
        ConfigureShadowVisual(back);

        float[] legX = { -0.16f, 0.16f };
        foreach (float legXPos in legX)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.transform.SetParent(benchRoot.transform, false);
            leg.transform.localPosition = new Vector3(legXPos, 0.08f, 0f);
            leg.transform.localScale = new Vector3(0.045f, 0.16f, 0.045f);
            ApplyColor(leg, legColor);
            ConfigureShadowVisual(leg);
        }

        roadsideBenchPositions.Add(worldPosition);
        roadCellBenchMap[roadCell] = (benchRoot, sideCell);
    }

    private bool TryGetNearestFreeBench(Vector3 fromPos, float maxDist, out int idx, out Vector3 pos)
    {
        idx = -1;
        pos = default;
        float best = maxDist * maxDist;
        for (int i = 0; i < roadsideBenchPositions.Count; i++)
        {
            if (i < benchOccupied.Length && benchOccupied[i]) continue;
            float d = (roadsideBenchPositions[i] - fromPos).sqrMagnitude;
            if (d < best) { best = d; idx = i; pos = roadsideBenchPositions[i]; }
        }
        return idx >= 0;
    }

    private void CreateRoadLantern(Vector3 worldPosition, Quaternion worldRotation)
    {
        GameObject lanternRoot = new("RoadLantern");
        lanternRoot.transform.SetParent(lanternsRoot, false);
        lanternRoot.transform.position = worldPosition;
        lanternRoot.transform.rotation = worldRotation;

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.transform.SetParent(lanternRoot.transform, false);
        pole.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        pole.transform.localScale = new Vector3(0.08f, 1.42f, 0.08f);
        ApplyColor(pole, new Color(0.22f, 0.23f, 0.27f));
        ConfigureShadowVisual(pole);

        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.transform.SetParent(lanternRoot.transform, false);
        arm.transform.localPosition = new Vector3(0.14f, 1.34f, 0f);
        arm.transform.localScale = new Vector3(0.3f, 0.06f, 0.06f);
        ApplyColor(arm, new Color(0.22f, 0.23f, 0.27f));
        ConfigureShadowVisual(arm);

        GameObject lampHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lampHead.transform.SetParent(lanternRoot.transform, false);
        lampHead.transform.localPosition = new Vector3(0.26f, 1.16f, 0f);
        lampHead.transform.localScale = new Vector3(0.16f, 0.22f, 0.16f);
        ApplyColor(lampHead, new Color(0.3f, 0.28f, 0.2f));
        ConfigureShadowVisual(lampHead);

        GameObject lampGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampGlow.transform.SetParent(lanternRoot.transform, false);
        lampGlow.transform.localPosition = new Vector3(0.26f, 1.05f, 0f);
        lampGlow.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
        ApplyColor(lampGlow, new Color(0.26f, 0.22f, 0.18f));
        ConfigureStaticVisual(lampGlow);
        Renderer lampGlowRenderer = lampGlow.GetComponent<Renderer>();

        GameObject lightObject = new("LanternLight");
        lightObject.transform.SetParent(lanternRoot.transform, false);
        lightObject.transform.localPosition = new Vector3(0.26f, 1.02f, 0f);

        Light lanternLight = lightObject.AddComponent<Light>();
        lanternLight.type = LightType.Point;
        lanternLight.color = new Color(1f, 0.9f, 0.72f);
        lanternLight.range = 4.4f;
        lanternLight.intensity = 0f;
        lanternLight.shadows = LightShadows.None;
        lanternLight.enabled = false;

        roadLanterns.Add(new RoadLanternData
        {
            Light = lanternLight,
            GlowRenderer = lampGlowRenderer,
            GlowMaterial = lampGlowRenderer != null ? lampGlowRenderer.material : null,
            ActivationOffset = Random.Range(-0.14f, 0.2f),
            FlickerSeed = Random.Range(0.1f, 100f),
            FlickerSpeed = Random.Range(0.7f, 1.35f),
            FlickerStrength = Random.Range(0.18f, 0.42f),
            FlickerThreshold = Random.Range(0.72f, 0.9f)
        });
    }

}


