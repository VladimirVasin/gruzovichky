using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
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
            if (!IsRoadVisualReady(roadCell))
            {
                continue;
            }

            if (!TryGetRoadLanternPlacement(roadCell, out Vector3 worldPosition, out Quaternion worldRotation))
            {
                continue;
            }

            CreateRoadLanternForCell(roadCell, worldPosition, worldRotation);
        }

        RebuildEdgeHighwayLanterns();
        RefreshAmbientLanternMoths();
    }

    private void RefreshRoadsideDecorationsAround(Vector2Int cell)
    {
        RefreshRoadLanternsAround(cell);
        RefreshRoadsideBenchesAround(cell);
        RefreshRoadSignsAround(cell);
    }

    private void FlushPendingRoadsideRefreshes()
    {
        if (pendingRoadsideRefreshCells.Count == 0)
        {
            return;
        }

        HashSet<Vector2Int> affectedCells = new();
        foreach (Vector2Int cell in pendingRoadsideRefreshCells)
        {
            if (!IsInsideGrid(cell))
            {
                continue;
            }

            affectedCells.Add(cell);
            foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(cell))
            {
                if (IsInsideGrid(neighbor))
                {
                    affectedCells.Add(neighbor);
                }
            }
        }

        pendingRoadsideRefreshCells.Clear();
        RefreshRoadLanternCells(affectedCells, refreshMoths: true);
        RefreshRoadsideBenchCells(affectedCells);
        RefreshRoadSignCells(affectedCells);
    }

    private void RefreshRoadLanternsAround(Vector2Int cell)
    {
        if (lanternsRoot == null) return;

        System.Collections.Generic.List<Vector2Int> toRefresh = new() { cell };
        foreach (Vector2Int n in GridPathService.GetCardinalNeighbors(cell))
            if (IsRoadVisualReady(n)) toRefresh.Add(n);

        RefreshRoadLanternCells(toRefresh, refreshMoths: true);
    }

    private void RefreshRoadLanternCells(IEnumerable<Vector2Int> cells, bool refreshMoths)
    {
        if (lanternsRoot == null) return;

        foreach (Vector2Int c in cells)
        {
            if (!roadCellLanternMap.TryGetValue(c, out var info)) continue;
            roadLanterns.Remove(info.Data);
            Destroy(info.Root);
            roadCellLanternMap.Remove(c);
        }

        foreach (Vector2Int c in cells)
        {
            if (!IsRoadVisualReady(c)) continue;
            if (!TryGetRoadLanternPlacement(c, out Vector3 pos, out Quaternion rot)) continue;
            CreateRoadLanternForCell(c, pos, rot);
        }

        if (refreshMoths)
        {
            RefreshAmbientLanternMoths();
        }
    }
    private void CreateRoadLanternForCell(Vector2Int cell, Vector3 worldPosition, Quaternion worldRotation)
    {
        CreateRoadLantern(worldPosition, worldRotation);
        roadCellLanternMap[cell] = (lanternsRoot.GetChild(lanternsRoot.childCount - 1).gameObject, roadLanterns[roadLanterns.Count - 1]);
    }
    private bool TryGetRoadLanternPlacement(Vector2Int cell, out Vector3 worldPosition, out Quaternion worldRotation)
    {
        worldPosition = default;
        worldRotation = Quaternion.identity;

        if (edgeHighwayCells.Contains(cell + Vector2Int.up) ||
            edgeHighwayCells.Contains(cell + Vector2Int.down) ||
            edgeHighwayCells.Contains(cell + Vector2Int.left) ||
            edgeHighwayCells.Contains(cell + Vector2Int.right))
        {
            return false;
        }

        if (!TryGetRoadsidePropSide(cell, null, out bool isHorizontal, out Vector2Int sideOffset))
        {
            return false;
        }

        int cadence = isHorizontal ? cell.x : cell.y;
        if (cadence % 2 != 0)
        {
            return false;
        }

        Vector3 baseCenter = GetCellCenter(cell);
        Vector3 sideDirection = new Vector3(sideOffset.x, 0f, sideOffset.y).normalized;
        worldPosition = baseCenter + sideDirection * 0.56f + new Vector3(0f, 0.04f, 0f);
        worldRotation = isHorizontal
            ? (sideOffset.y > 0 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity)
            : (sideOffset.x > 0 ? Quaternion.Euler(0f, -90f, 0f) : Quaternion.Euler(0f, 90f, 0f));
        return true;
    }
    private bool TryGetRoadsidePropSide(
        Vector2Int cell,
        HashSet<Vector2Int> reservedSideCells,
        out bool isHorizontal,
        out Vector2Int sideOffset)
    {
        if (!TryGetRoadOutsideSide(cell, out isHorizontal, out sideOffset))
        {
            return false;
        }

        if (CanPlaceRoadsidePropInCell(cell + sideOffset, reservedSideCells))
        {
            return true;
        }

        Vector2Int alternateOffset = -sideOffset;
        if (CanPlaceRoadsidePropInCell(cell + alternateOffset, reservedSideCells))
        {
            sideOffset = alternateOffset;
            return true;
        }

        return false;
    }
    private bool TryGetRoadOutsideSide(Vector2Int cell, out bool isHorizontal, out Vector2Int sideOffset)
    {
        if (TryGetTwoLaneRoadOutsideSide(cell, out isHorizontal, out sideOffset))
        {
            return true;
        }

        return TryGetSingleLaneRoadOutsideSide(cell, out isHorizontal, out sideOffset);
    }
    private bool TryGetTwoLaneRoadOutsideSide(Vector2Int cell, out bool isHorizontal, out Vector2Int sideOffset)
    {
        bool east = ConnectsToRoadOrAnchor(cell, Vector2Int.right);
        bool west = ConnectsToRoadOrAnchor(cell, Vector2Int.left);
        bool north = ConnectsToRoadOrAnchor(cell, Vector2Int.up);
        bool south = ConnectsToRoadOrAnchor(cell, Vector2Int.down);

        bool northLane = IsRoadVisualReady(cell + Vector2Int.up);
        bool southLane = IsRoadVisualReady(cell + Vector2Int.down);
        bool eastLane = IsRoadVisualReady(cell + Vector2Int.right);
        bool westLane = IsRoadVisualReady(cell + Vector2Int.left);

        bool horizontalCandidate = (east || west) && (northLane ^ southLane);
        bool verticalCandidate = (north || south) && (eastLane ^ westLane);

        if (horizontalCandidate == verticalCandidate)
        {
            isHorizontal = true;
            sideOffset = Vector2Int.zero;
            return false;
        }

        if (horizontalCandidate)
        {
            isHorizontal = true;
            sideOffset = northLane ? Vector2Int.down : Vector2Int.up;
            return true;
        }

        isHorizontal = false;
        sideOffset = eastLane ? Vector2Int.left : Vector2Int.right;
        return true;
    }
    private bool TryGetSingleLaneRoadOutsideSide(Vector2Int cell, out bool isHorizontal, out Vector2Int sideOffset)
    {
        bool east = ConnectsToRoadOrAnchor(cell, Vector2Int.right);
        bool west = ConnectsToRoadOrAnchor(cell, Vector2Int.left);
        bool north = ConnectsToRoadOrAnchor(cell, Vector2Int.up);
        bool south = ConnectsToRoadOrAnchor(cell, Vector2Int.down);

        bool horizontalCandidate = (east || west) && !north && !south;
        bool verticalCandidate = (north || south) && !east && !west;
        if (horizontalCandidate == verticalCandidate)
        {
            isHorizontal = true;
            sideOffset = Vector2Int.zero;
            return false;
        }

        isHorizontal = horizontalCandidate;
        sideOffset = isHorizontal
            ? (((cell.x / 2) & 1) == 0 ? Vector2Int.up : Vector2Int.down)
            : (((cell.y / 2) & 1) == 0 ? Vector2Int.right : Vector2Int.left);
        return true;
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

            if (IsRoadVisualReady(neighbor) || IsAnchorCell(neighbor))
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
            if (!IsRoadVisualReady(roadCell))
            {
                continue;
            }

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
            if (IsRoadVisualReady(n)) toRefresh.Add(n);

        RefreshRoadsideBenchCells(toRefresh);
    }

    private void RefreshRoadsideBenchCells(IEnumerable<Vector2Int> cells)
    {
        if (roadsidePropsRoot == null) return;

        foreach (Vector2Int c in cells)
        {
            if (!roadCellBenchMap.TryGetValue(c, out var info)) continue;
            benchSideCells.Remove(info.SideCell);
            roadsideBenchPositions.Remove(info.Root.transform.position);
            Destroy(info.Root);
            roadCellBenchMap.Remove(c);
        }

        foreach (Vector2Int c in cells)
        {
            if (!IsRoadVisualReady(c)) continue;
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

        if (!TryGetRoadsidePropSide(roadCell, reservedSideCells, out _, out Vector2Int sideOffset))
        {
            return false;
        }

        int hash = Mathf.Abs(roadCell.x * 92821 + roadCell.y * 68917 + 17);
        if ((hash % 100) >= 15)
        {
            return false;
        }

        Vector2Int candidateSideCell = roadCell + sideOffset;

        Vector3 center = GetCellCenter(roadCell);
        Vector3 sideDirection = new Vector3(sideOffset.x, 0f, sideOffset.y).normalized;
        Vector3 candidateWorld = center + sideDirection * 0.62f;
        candidateWorld.y = SampleTerrainHeight(candidateWorld.x, candidateWorld.z) + 0.02f;
        if (IsRoadsideBenchTooCloseToLantern(candidateWorld))
        {
            return false;
        }

        worldPosition = candidateWorld;
        worldRotation = Quaternion.LookRotation(sideDirection, Vector3.up);
        sideCell = candidateSideCell;
        return true;
    }
    private bool CanPlaceRoadsidePropInCell(Vector2Int cell, HashSet<Vector2Int> reservedSideCells)
    {
        return IsInsideGrid(cell) &&
               !roadCells.Contains(cell) &&
               !edgeHighwayCells.Contains(cell) &&
               !IsLocationCell(cell) &&
               !miscOccupiedCells.Contains(cell) &&
               (reservedSideCells == null || !reservedSideCells.Contains(cell));
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
    private void RebuildRoadSigns()
    {
        if (roadsideSignsRoot == null) return;

        for (int i = roadsideSignsRoot.childCount - 1; i >= 0; i--)
            Destroy(roadsideSignsRoot.GetChild(i).gameObject);

        roadCellSignMap.Clear();
        signSideCells.Clear();

        foreach (Vector2Int roadCell in roadCells)
        {
            if (!IsRoadVisualReady(roadCell))
            {
                continue;
            }

            if (!TryGetRoadSignPlacement(roadCell, signSideCells, out Vector3 pos, out Quaternion rot,
                out Vector2Int sideCell, out bool isTrafficLight)) continue;

            CreateRoadSignAtCell(pos, rot, roadCell, sideCell, isTrafficLight);
            signSideCells.Add(sideCell);
        }
    }
    private void RefreshRoadSignsAround(Vector2Int cell)
    {
        if (roadsideSignsRoot == null) return;

        System.Collections.Generic.List<Vector2Int> toRefresh = new() { cell };
        foreach (Vector2Int n in GridPathService.GetCardinalNeighbors(cell))
            if (IsRoadVisualReady(n)) toRefresh.Add(n);

        RefreshRoadSignCells(toRefresh);
    }

    private void RefreshRoadSignCells(IEnumerable<Vector2Int> cells)
    {
        if (roadsideSignsRoot == null) return;

        foreach (Vector2Int c in cells)
        {
            if (!roadCellSignMap.TryGetValue(c, out var info)) continue;
            signSideCells.Remove(info.SideCell);
            Destroy(info.Root);
            roadCellSignMap.Remove(c);
        }

        foreach (Vector2Int c in cells)
        {
            if (!IsRoadVisualReady(c)) continue;
            if (!TryGetRoadSignPlacement(c, signSideCells, out Vector3 pos, out Quaternion rot,
                out Vector2Int sideCell, out bool isTrafficLight)) continue;
            CreateRoadSignAtCell(pos, rot, c, sideCell, isTrafficLight);
            signSideCells.Add(sideCell);
        }
    }
    private bool TryGetRoadSignPlacement(
        Vector2Int cell,
        HashSet<Vector2Int> reservedSideCells,
        out Vector3 worldPosition,
        out Quaternion worldRotation,
        out Vector2Int sideCell,
        out bool isTrafficLight)
    {
        worldPosition = default;
        worldRotation = Quaternion.identity;
        sideCell = default;
        isTrafficLight = false;

        if (edgeHighwayCells.Contains(cell + Vector2Int.up)   ||
            edgeHighwayCells.Contains(cell + Vector2Int.down)  ||
            edgeHighwayCells.Contains(cell + Vector2Int.left)  ||
            edgeHighwayCells.Contains(cell + Vector2Int.right)) return false;

        bool isStraight = TryGetRoadOutsideSide(cell, out _, out _);
        bool isCorner   = TryGetRoadCorner(cell, out int hSign, out int vSign);

        if (isStraight) return false; // straight road — lanterns/benches handle it

        if (isCorner)
        {
            // Corner warning sign: 1 in 2 corners
            int hash = Mathf.Abs(cell.x * 73793 + cell.y * 40237);
            if ((hash & 1) != 0) return false;

            // Try the two "outside" sides: opposite of each connection direction
            Vector2Int[] candidates = { new(-hSign, 0), new(0, -vSign) };
            foreach (Vector2Int offset in candidates)
            {
                Vector2Int candidate = cell + offset;
                if (!CanPlaceRoadsidePropInCell(candidate, reservedSideCells)) continue;

                Vector3 center   = GetCellCenter(cell);
                Vector3 sideDir  = new Vector3(offset.x, 0f, offset.y);
                worldPosition    = center + sideDir * 0.52f;
                worldPosition.y  = SampleRoadSurfaceHeight(worldPosition.x, worldPosition.z) + RoadHeight;
                worldRotation    = Quaternion.LookRotation(-sideDir, Vector3.up);
                sideCell         = candidate;
                isTrafficLight   = false;
                return true;
            }
            return false;
        }

        // Junction cell (3+ connections, not straight, not corner) — traffic light
        int connCount = 0;
        bool e = ConnectsToRoadOrAnchor(cell, Vector2Int.right);
        bool w = ConnectsToRoadOrAnchor(cell, Vector2Int.left);
        bool n = ConnectsToRoadOrAnchor(cell, Vector2Int.up);
        bool s = ConnectsToRoadOrAnchor(cell, Vector2Int.down);
        if (e) connCount++; if (w) connCount++; if (n) connCount++; if (s) connCount++;
        if (connCount < 3) return false;

        // 1 in 4 junction cells gets a traffic light
        int jHash = Mathf.Abs(cell.x * 39769 + cell.y * 54133);
        if ((jHash % 4) != 0) return false;

        // Place on the free cardinal side (or any free side for 4-way)
        Vector2Int[] jCandidates = new Vector2Int[4];
        int jCount = 0;
        if (!e) jCandidates[jCount++] = Vector2Int.right;
        if (!w) jCandidates[jCount++] = Vector2Int.left;
        if (!n) jCandidates[jCount++] = Vector2Int.up;
        if (!s) jCandidates[jCount++] = Vector2Int.down;
        // If all 4 are connected (4-way), fall back to any cardinal
        if (jCount == 0)
        {
            jCandidates[0] = Vector2Int.right; jCandidates[1] = Vector2Int.left;
            jCandidates[2] = Vector2Int.up;    jCandidates[3] = Vector2Int.down;
            jCount = 4;
        }

        for (int i = 0; i < jCount; i++)
        {
            Vector2Int offset    = jCandidates[i];
            Vector2Int candidate = cell + offset;
            if (!CanPlaceRoadsidePropInCell(candidate, reservedSideCells)) continue;

            Vector3 center  = GetCellCenter(cell);
            Vector3 sideDir = new Vector3(offset.x, 0f, offset.y);
            worldPosition   = center + sideDir * 0.50f;
            worldPosition.y = SampleRoadSurfaceHeight(worldPosition.x, worldPosition.z) + RoadHeight;
            worldRotation   = Quaternion.LookRotation(-sideDir, Vector3.up);
            sideCell        = candidate;
            isTrafficLight  = true;
            return true;
        }
        return false;
    }
    private void CreateRoadSignAtCell(Vector3 worldPosition, Quaternion worldRotation,
        Vector2Int roadCell, Vector2Int sideCell, bool isTrafficLight)
    {
        if (roadsideSignsRoot == null) return;

        GameObject root = new(isTrafficLight ? $"TrafficLight_{roadCell.x}_{roadCell.y}" : $"RoadSign_{roadCell.x}_{roadCell.y}");
        root.transform.SetParent(roadsideSignsRoot, false);
        root.transform.position = worldPosition;
        root.transform.rotation = worldRotation;

        if (isTrafficLight)
            BuildTrafficLightVisual(root.transform);
        else
            BuildCurveSignVisual(root.transform, roadCell);

        roadCellSignMap[roadCell] = (root, sideCell);
    }
    private void BuildTrafficLightVisual(Transform parent)
    {
        Color poleColor    = new(0.22f, 0.22f, 0.24f);
        Color housingColor = new(0.12f, 0.12f, 0.13f);

        // Pole
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.transform.SetParent(parent, false);
        pole.transform.localPosition = new Vector3(0f, 0.44f, 0f);
        pole.transform.localScale    = new Vector3(0.07f, 0.88f, 0.07f);
        ApplyColor(pole, poleColor);
        ConfigureShadowVisual(pole);

        // Housing box
        GameObject housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        housing.transform.SetParent(parent, false);
        housing.transform.localPosition = new Vector3(0f, 0.99f, 0.02f);
        housing.transform.localScale    = new Vector3(0.12f, 0.30f, 0.09f);
        ApplyColor(housing, housingColor);
        ConfigureShadowVisual(housing);

        // Visor cap
        GameObject visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visor.transform.SetParent(parent, false);
        visor.transform.localPosition = new Vector3(0f, 1.15f, 0.05f);
        visor.transform.localScale    = new Vector3(0.14f, 0.02f, 0.05f);
        ApplyColor(visor, housingColor);
        ConfigureShadowVisual(visor);

        // Lights: red / yellow / green
        (Color col, float y)[] lights = {
            (new Color(0.88f, 0.16f, 0.12f), 1.09f),
            (new Color(0.96f, 0.78f, 0.12f), 0.99f),
            (new Color(0.16f, 0.76f, 0.22f), 0.89f),
        };
        foreach (var (col, y) in lights)
        {
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.transform.SetParent(parent, false);
            bulb.transform.localPosition = new Vector3(0f, y, 0.06f);
            bulb.transform.localScale    = new Vector3(0.038f, 0.038f, 0.038f);
            ApplyColor(bulb, col);
            ConfigureShadowVisual(bulb);
        }
    }
    private void BuildCurveSignVisual(Transform parent, Vector2Int roadCell)
    {
        Color poleColor = new(0.75f, 0.75f, 0.72f);

        // Tint variation so not every sign is identical
        int tint = Mathf.Abs(roadCell.x * 5821 + roadCell.y * 7193) % 2;
        Color panelColor = tint == 0 ? new Color(0.95f, 0.82f, 0.10f) : new Color(0.92f, 0.92f, 0.90f);
        Color borderColor = new(0.14f, 0.14f, 0.14f);

        // Pole
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.transform.SetParent(parent, false);
        pole.transform.localPosition = new Vector3(0f, 0.30f, 0f);
        pole.transform.localScale    = new Vector3(0.05f, 0.60f, 0.05f);
        ApplyColor(pole, poleColor);
        ConfigureShadowVisual(pole);

        // Diamond panel (cube rotated 45° on Z axis)
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.transform.SetParent(parent, false);
        panel.transform.localPosition   = new Vector3(0f, 0.68f, 0f);
        panel.transform.localScale      = new Vector3(0.20f, 0.20f, 0.03f);
        panel.transform.localRotation   = Quaternion.Euler(0f, 0f, 45f);
        ApplyColor(panel, panelColor);
        ConfigureShadowVisual(panel);

        // Thin border ring (slightly larger, behind panel)
        GameObject border = GameObject.CreatePrimitive(PrimitiveType.Cube);
        border.transform.SetParent(parent, false);
        border.transform.localPosition   = new Vector3(0f, 0.68f, -0.008f);
        border.transform.localScale      = new Vector3(0.22f, 0.22f, 0.028f);
        border.transform.localRotation   = Quaternion.Euler(0f, 0f, 45f);
        ApplyColor(border, borderColor);
        ConfigureShadowVisual(border);
    }
    private bool TryGetNearestFreeBench(Vector3 fromPos, float maxDist, out int idx, out Vector3 pos)
    {
        idx = -1;
        pos = default;
        float best = maxDist * maxDist;
        for (int i = 0; i < roadsideBenchPositions.Count; i++)
        {
            if (IsRoadsideBenchOccupied(i)) continue;
            float d = (roadsideBenchPositions[i] - fromPos).sqrMagnitude;
            if (d < best) { best = d; idx = i; pos = roadsideBenchPositions[i]; }
        }
        return idx >= 0;
    }

    private bool IsRoadsideBenchOccupied(int benchIndex)
    {
        if (benchIndex < 0)
        {
            return true;
        }

        if (benchIndex < benchOccupied.Length && benchOccupied[benchIndex])
        {
            return true;
        }

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver == null || driver.SittingBenchIndex != benchIndex)
            {
                continue;
            }

            if (driver.WalkPhase == DriverRescuePhase.IdleWalkToBench ||
                driver.WalkPhase == DriverRescuePhase.IdleSittingOnBench)
            {
                return true;
            }
        }

        return false;
    }

    private void MarkRoadsideBenchOccupied(int benchIndex)
    {
        if (benchIndex >= 0 && benchIndex < benchOccupied.Length)
        {
            benchOccupied[benchIndex] = true;
        }
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
        ApplyColor(lampGlow, new Color(0.28f, 0.18f, 0.1f));
        ConfigureStaticVisual(lampGlow);
        Renderer lampGlowRenderer = lampGlow.GetComponent<Renderer>();

        GameObject lightObject = new("LanternLight");
        lightObject.transform.SetParent(lanternRoot.transform, false);
        lightObject.transform.localPosition = new Vector3(0.26f, 1.02f, 0f);

        Light lanternLight = lightObject.AddComponent<Light>();
        lanternLight.type = LightType.Point;
        lanternLight.color = new Color(1f, 0.78f, 0.42f);
        lanternLight.range = 6.2f;
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
