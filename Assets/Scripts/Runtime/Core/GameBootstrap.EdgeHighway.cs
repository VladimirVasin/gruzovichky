using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private const float EdgeHighwayCellBaseLift = 0.022f;
    private const float EdgeHighwayShoulderLift = 0.040f;
    private const float EdgeHighwaySurfaceLift = 0.058f;
    private const float EdgeHighwayMarkingLift = 0.076f;

    private void SetupEdgeHighway()
    {
        if (worldRoot == null)
        {
            return;
        }

        edgeHighwayCells.Clear();
        Transform highwayRoot = new GameObject("EdgeHighway").transform;
        highwayRoot.SetParent(worldRoot, false);

        int bottomLaneY = 0;
        int upperLaneY = 1;
        for (int x = 0; x < GridWidth; x++)
        {
            CreateEdgeHighwayTile(highwayRoot, new Vector2Int(x, bottomLaneY), horizontal: true, vertical: false);
            CreateEdgeHighwayTile(highwayRoot, new Vector2Int(x, upperLaneY), horizontal: true, vertical: false);
        }

        CreateEdgeHighwayCellBaseStrip(highwayRoot, bottomLaneY, upperLaneY);
        CreateEdgeHighwayCenterLine(highwayRoot, bottomLaneY, upperLaneY);
        surfaceTransitionOverlayRebuildPending = true;
        FlushSurfaceTransitionOverlayRebuild();
        UpdateRoadAccessWarningMarkers();
    }

    private void SetupEdgeHighwayBuses()
    {
        edgeHighwayBuses.Clear();
        hiringDriverArrival = null;
        isMotelBootstrapWorkerWavePending = false;
        hasMotelBootstrapWorkerWaveStarted = false;
        hasMotelBootstrapWorkerWaveDisembarked = false;
        if (edgeHighwayBusRoot != null)
        {
            Destroy(edgeHighwayBusRoot.gameObject);
        }

        edgeHighwayBusRoot = new GameObject("EdgeHighwayBuses").transform;
        edgeHighwayBusRoot.SetParent(worldRoot, false);
        edgeHighwayBusSpawnTimerCitySide = Random.Range(6f, 16f);
        edgeHighwayBusSpawnTimerOuterSide = Random.Range(12f, 24f);
    }

    private void CreateEdgeHighwayTile(Transform parent, Vector2Int cell, bool horizontal, bool vertical)
    {
        if (!IsInsideGrid(cell) || edgeHighwayCells.Contains(cell))
        {
            return;
        }

        edgeHighwayCells.Add(cell);
        RefreshTerrainCellVisual(cell);
        RefreshGroundCellSurfaceMaterial(cell);
        HideEdgeHighwayGroundCellVisual(cell);

        GameObject road = new($"EdgeHighway_{cell.x}_{cell.y}");
        road.name = $"EdgeHighway_{cell.x}_{cell.y}";
        road.transform.SetParent(parent, false);
        road.transform.localPosition = Vector3.zero;

        CreateEdgeHighwaySurface(road.transform, "EdgeHighwayShoulder", cell, horizontal ? 1.12f : 0.94f, vertical ? 1.12f : 0.94f, EdgeHighwayShoulderLift, isShoulder: true);
        CreateEdgeHighwaySurface(road.transform, "EdgeHighwaySurface", cell, horizontal ? 0.94f : 0.74f, vertical ? 0.94f : 0.74f, EdgeHighwaySurfaceLift, isShoulder: false);
    }

    private void HideEdgeHighwayGroundCellVisual(Vector2Int cell)
    {
        if (groundRoot == null)
        {
            return;
        }

        Transform tile = groundRoot.Find($"Ground_{cell.x}_{cell.y}");
        if (tile != null && tile.TryGetComponent(out Renderer renderer))
        {
            renderer.enabled = false;
        }
    }

    private void CreateEdgeHighwayCellBaseStrip(Transform parent, int lowerLaneY, int upperLaneY)
    {
        GameObject baseStrip = new("EdgeHighwayCellBase");
        baseStrip.transform.SetParent(parent, false);
        baseStrip.transform.localPosition = Vector3.zero;

        float x0 = -0.02f;
        float x1 = GridWidth + 0.02f;
        float z0 = lowerLaneY - 0.02f;
        float z1 = upperLaneY + 1.02f;
        CreateSampledRoadRectMesh(baseStrip, x0, x1, z0, z1, EdgeHighwayCellBaseLift, GridWidth, Mathf.Max(2, upperLaneY - lowerLaneY + 1));
        ApplyStylizedRoadMaterial(baseStrip, 0, lowerLaneY, isHighway: true, isShoulder: false);
        ConfigureStaticVisual(baseStrip, VisualSmoothnessAsphalt);
    }

    private void CreateEdgeHighwaySurface(Transform parent, string name, Vector2Int cell, float sizeX, float sizeZ, float lift, bool isShoulder)
    {
        GameObject surface = new(name);
        surface.transform.SetParent(parent, false);
        surface.transform.localPosition = Vector3.zero;

        float centerX = cell.x + 0.5f;
        float centerZ = cell.y + 0.5f;
        CreateFlatRoadQuadMesh(surface, centerX, centerZ, sizeX, sizeZ, lift);
        ApplyStylizedRoadMaterial(surface, cell.x, cell.y, isHighway: true, isShoulder: isShoulder);
        ConfigureStaticVisual(surface, VisualSmoothnessAsphalt);
    }

    private void CreateEdgeHighwayCenterLine(Transform parent, int lowerLaneY, int upperLaneY)
    {
        float centerZ = (lowerLaneY + upperLaneY + 1f) * 0.5f;
        for (int x = 0; x < GridWidth; x += 2)
        {
            GameObject dash = new($"EdgeHighwayCenterDash_{x}");
            dash.name = $"EdgeHighwayCenterDash_{x}";
            dash.transform.SetParent(parent, false);
            dash.transform.localPosition = Vector3.zero;
            CreateFlatRoadQuadMesh(dash, x + 0.5f, centerZ, 0.68f, 0.08f, EdgeHighwayMarkingLift);
            ApplyColor(dash, new Color(0.92f, 0.92f, 0.9f));
            ConfigureStaticVisual(dash, VisualSmoothnessAsphalt);
        }
    }

    private void CreateFlatRoadQuadMesh(GameObject target, float centerX, float centerZ, float sizeX, float sizeZ, float lift)
    {
        float halfX = sizeX * 0.5f;
        float halfZ = sizeZ * 0.5f;
        float x0 = centerX - halfX;
        float x1 = centerX + halfX;
        float z0 = centerZ - halfZ;
        float z1 = centerZ + halfZ;

        Mesh mesh = new();
        mesh.name = $"{target.name}_Mesh";
        mesh.vertices = new[]
        {
            new Vector3(x0, SampleRoadSurfaceHeight(x0, z0) + lift, z0),
            new Vector3(x1, SampleRoadSurfaceHeight(x1, z0) + lift, z0),
            new Vector3(x0, SampleRoadSurfaceHeight(x0, z1) + lift, z1),
            new Vector3(x1, SampleRoadSurfaceHeight(x1, z1) + lift, z1),
        };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = target.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        target.AddComponent<MeshRenderer>();
    }

    private void CreateSampledRoadRectMesh(
        GameObject target,
        float x0,
        float x1,
        float z0,
        float z1,
        float lift,
        int xSegments,
        int zSegments)
    {
        xSegments = Mathf.Max(1, xSegments);
        zSegments = Mathf.Max(1, zSegments);
        int columns = xSegments + 1;
        int rows = zSegments + 1;
        Vector3[] vertices = new Vector3[columns * rows];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[xSegments * zSegments * 6];

        int vi = 0;
        for (int z = 0; z <= zSegments; z++)
        {
            float tz = z / (float)zSegments;
            float worldZ = Mathf.Lerp(z0, z1, tz);
            for (int x = 0; x <= xSegments; x++)
            {
                float tx = x / (float)xSegments;
                float worldX = Mathf.Lerp(x0, x1, tx);
                vertices[vi] = new Vector3(worldX, SampleRoadSurfaceHeight(worldX, worldZ) + lift, worldZ);
                uvs[vi] = new Vector2(worldX, worldZ);
                vi++;
            }
        }

        int ti = 0;
        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int i0 = z * columns + x;
                int i1 = i0 + 1;
                int i2 = i0 + columns;
                int i3 = i2 + 1;
                triangles[ti++] = i0;
                triangles[ti++] = i2;
                triangles[ti++] = i1;
                triangles[ti++] = i1;
                triangles[ti++] = i2;
                triangles[ti++] = i3;
            }
        }

        Mesh mesh = new() { name = $"{target.name}_Mesh" };
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = target.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        target.AddComponent<MeshRenderer>();
    }

    private void UpdateEdgeHighwayBuses()
    {
        if (edgeHighwayBusRoot == null)
        {
            return;
        }

        float dt = Time.deltaTime * gameSpeedMultiplier;
        edgeHighwayBusSpawnTimerCitySide -= dt;
        edgeHighwayBusSpawnTimerOuterSide -= dt;

        bool pauseCitySideLane = ShouldPauseEdgeHighwayBusLaneForTrade(isCitySideLane: true);
        bool pauseOuterSideLane = ShouldPauseEdgeHighwayBusLaneForTrade(isCitySideLane: false);

        if (edgeHighwayBusSpawnTimerCitySide <= 0f)
        {
            if (IsDriverMotelArrivalInProgress())
            {
                SessionDebugLogger.LogVerbose("BUS_SPAWN", "CityLane spawn skipped: hiring-arrival bus is active.");
            }
            else if (pauseCitySideLane)
            {
                SessionDebugLogger.LogVerbose("BUS_SPAWN", "CityLane spawn skipped: lane paused.");
            }
            else
            {
                TrySpawnEdgeHighwayBus(isCitySideLane: true);
            }
            edgeHighwayBusSpawnTimerCitySide = Random.Range(EdgeHighwayBusSpawnIntervalMin, EdgeHighwayBusSpawnIntervalMax);
            edgeHighwayBusSpawnTimerOuterSide += Random.Range(1.5f, 4.5f);
        }

        if (edgeHighwayBusSpawnTimerOuterSide <= 0f)
        {
            if (pauseOuterSideLane)
            {
                SessionDebugLogger.LogVerbose("BUS_SPAWN", "OuterLane spawn skipped: lane paused.");
            }
            else
            {
                TrySpawnEdgeHighwayBus(isCitySideLane: false);
            }
            edgeHighwayBusSpawnTimerOuterSide = Random.Range(EdgeHighwayBusSpawnIntervalMin, EdgeHighwayBusSpawnIntervalMax);
            edgeHighwayBusSpawnTimerCitySide += Random.Range(1.5f, 4.5f);
        }

        for (int i = edgeHighwayBuses.Count - 1; i >= 0; i--)
        {
            EdgeHighwayBusData bus = edgeHighwayBuses[i];
            if (bus.RootTransform == null)
            {
                edgeHighwayBuses.RemoveAt(i);
                continue;
            }

            bus.WorldX += bus.Speed * dt * bus.TravelDirection;
            if (!bus.HasEnteredRoadStrip && bus.WorldX > 0f && bus.WorldX < GridWidth)
            {
                bus.HasEnteredRoadStrip = true;
            }

            if (bus.HasEnteredRoadStrip &&
                ((bus.TravelDirection > 0f && bus.WorldX >= GridWidth) ||
                 (bus.TravelDirection < 0f && bus.WorldX <= 0f)))
            {
                Destroy(bus.RootTransform.gameObject);
                edgeHighwayBuses.RemoveAt(i);
                continue;
            }

            float laneZ = GetEdgeHighwayBusLaneWorldZ(bus.IsCitySideLane);
            float y = SampleTerrainHeight(bus.WorldX, laneZ) + RoadHeight + EdgeHighwayBusLift;
            bus.RootTransform.position = new Vector3(bus.WorldX, y, laneZ);
            bus.RootTransform.rotation = bus.TravelDirection > 0f
                ? Quaternion.identity
                : Quaternion.Euler(0f, 180f, 0f);
            ApplySharedBusMotionAnimation(
                bus.RootTransform,
                bus.Speed / Mathf.Max(0.01f, EdgeHighwayBusSpeed * 1.1f),
                true,
                bus.BobPhase);

            float darkness = 1f - currentStylizedDaylight;
            bool headlightsOn = darkness > 0.55f;
            float headlightIntensity = headlightsOn ? Mathf.Lerp(0.48f, 1.95f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
            Color lampColor = Color.Lerp(
                new Color(0.36f, 0.20f, 0.08f),
                new Color(1f, 0.68f, 0.34f),
                Mathf.Clamp01(headlightIntensity / 1.95f));

            if (bus.HeadlightLeft != null)
            {
                bus.HeadlightLeft.enabled = headlightsOn;
                bus.HeadlightLeft.intensity = headlightIntensity;
            }

            if (bus.HeadlightRight != null)
            {
                bus.HeadlightRight.enabled = headlightsOn;
                bus.HeadlightRight.intensity = headlightIntensity;
            }

            if (bus.HeadlightLeftMaterial != null)
            {
                bus.HeadlightLeftMaterial.color = lampColor;
            }

            if (bus.HeadlightRightMaterial != null)
            {
                bus.HeadlightRightMaterial.color = lampColor;
            }

            if (!bus.HasPlayedPassbyAudio)
            {
                Vector3 audioDelta = bus.RootTransform.position - cameraFocusPoint;
                audioDelta.y = 0f;
                if (audioDelta.sqrMagnitude <= EdgeHighwayBusPassbyDistance * EdgeHighwayBusPassbyDistance)
                {
                    bus.HasPlayedPassbyAudio = true;
                }
            }
        }
    }

    private void TrySpawnEdgeHighwayBus(bool isCitySideLane)
    {
        float travelDirection = isCitySideLane ? -1f : 1f;
        float spawnX = travelDirection > 0f ? -1.6f : GridWidth + 1.6f;
        string laneLabel = isCitySideLane ? "CityLane" : "OuterLane";
        for (int i = 0; i < edgeHighwayBuses.Count; i++)
        {
            EdgeHighwayBusData existing = edgeHighwayBuses[i];
            if (existing == null || existing.RootTransform == null || existing.IsCitySideLane != isCitySideLane)
            {
                continue;
            }

            if (Mathf.Abs(existing.WorldX - spawnX) < EdgeHighwayBusSpawnSpacing)
            {
                SessionDebugLogger.LogVerbose(
                    "BUS_SPAWN",
                    $"{laneLabel} spawn blocked by spacing: existingX={existing.WorldX:0.00}, spawnX={spawnX:0.00}, direction={(travelDirection > 0f ? "+" : "-")}.");
                return;
            }
        }

        SessionDebugLogger.LogVerbose(
            "BUS_SPAWN",
            $"{laneLabel} spawn ok: spawnX={spawnX:0.00}, direction={(travelDirection > 0f ? "+" : "-")}, laneZ={GetEdgeHighwayBusLaneWorldZ(isCitySideLane):0.00}.");
        CreateEdgeHighwayBus(travelDirection, isCitySideLane);
    }

    private bool ShouldPauseEdgeHighwayBusLaneForTrade(bool isCitySideLane)
    {
        if (!HasActiveTradeRun())
        {
            return false;
        }

        int laneRow = isCitySideLane ? 1 : 0;
        bool isRelevantTradePhase =
            activeTradeRun.Phase == TradeRunPhase.DrivingToHighway ||
            activeTradeRun.Phase == TradeRunPhase.ReturningFromOffMap;
        if (!isRelevantTradePhase)
        {
            return false;
        }

        TruckAgent truckAgent = GetTruckAgent(activeTradeRun.TruckNumber);
        if (truckAgent == null)
        {
            return false;
        }

        if (truckAgent.TruckCell.y == laneRow && edgeHighwayCells.Contains(truckAgent.TruckCell))
        {
            return true;
        }

        if (!truckAgent.IsTruckMoving || truckAgent.ActivePath == null || truckAgent.ActivePath.Count == 0)
        {
            return false;
        }

        Vector2Int nextStep = truckAgent.ActivePath[0];
        if (nextStep.y != laneRow)
        {
            return false;
        }

        return edgeHighwayCells.Contains(nextStep) || edgeHighwayCells.Contains(truckAgent.TruckCell);
    }

}

