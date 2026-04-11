using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
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

        CreateEdgeHighwayCenterLine(highwayRoot, bottomLaneY, upperLaneY);
    }

    private void SetupEdgeHighwayBuses()
    {
        edgeHighwayBuses.Clear();
        hiringDriverArrival = null;
        if (edgeHighwayBusRoot != null)
        {
            Destroy(edgeHighwayBusRoot.gameObject);
        }

        edgeHighwayBusRoot = new GameObject("EdgeHighwayBuses").transform;
        edgeHighwayBusRoot.SetParent(worldRoot, false);
        edgeHighwayBusSpawnTimerCitySide = Random.Range(6f, 16f);
        edgeHighwayBusSpawnTimerOuterSide = Random.Range(12f, 24f);
    }

    private void SetupRiverBoats()
    {
        riverBoats.Clear();
        if (riverBoatRoot != null) Destroy(riverBoatRoot.gameObject);
        riverBoatRoot = new GameObject("RiverBoats").transform;
        riverBoatRoot.SetParent(worldRoot, false);
        riverBoatSpawnTimerLeft  = Random.Range(5f, 14f);
        riverBoatSpawnTimerRight = Random.Range(10f, 22f);
    }

    private void UpdateRiverBoats()
    {
        if (riverBoatRoot == null) return;

        const float spawnInterval1 = 18f;
        const float spawnInterval2 = 28f;

        float dt = Time.deltaTime * gameSpeedMultiplier;
        riverBoatSpawnTimerLeft  -= dt;
        riverBoatSpawnTimerRight -= dt;

        if (riverBoatSpawnTimerLeft <= 0f)
        {
            TrySpawnRiverBoat(travelDirection:  1f);
            riverBoatSpawnTimerLeft  = Random.Range(spawnInterval1, spawnInterval2);
            riverBoatSpawnTimerRight += Random.Range(2f, 5f);
        }
        if (riverBoatSpawnTimerRight <= 0f)
        {
            TrySpawnRiverBoat(travelDirection: -1f);
            riverBoatSpawnTimerRight = Random.Range(spawnInterval1, spawnInterval2);
            riverBoatSpawnTimerLeft  += Random.Range(2f, 5f);
        }

        for (int i = riverBoats.Count - 1; i >= 0; i--)
        {
            RiverBoatData boat = riverBoats[i];
            if (boat.RootTransform == null) { riverBoats.RemoveAt(i); continue; }

            boat.WorldX += boat.Speed * dt * boat.TravelDirection;

            if (!boat.HasEnteredRiver && boat.WorldX > 0f && boat.WorldX < GridWidth)
                boat.HasEnteredRiver = true;

            if (boat.HasEnteredRiver &&
                ((boat.TravelDirection > 0f && boat.WorldX >= GridWidth + 1f) ||
                 (boat.TravelDirection < 0f && boat.WorldX <= -1f)))
            {
                Destroy(boat.RootTransform.gameObject);
                riverBoats.RemoveAt(i);
                continue;
            }

            // Sample the highest water surface tile in the boat's current X column
            int cellX = Mathf.Clamp(Mathf.FloorToInt(boat.WorldX), 0, GridWidth - 1);
            float waterY = 0.22f;
            float bestY = float.MinValue;
            for (int j = 0; j < waterSurfaceTiles.Count; j++)
            {
                WaterSurfaceTileData tile = waterSurfaceTiles[j];
                if (tile.Cell.x == cellX && tile.Transform != null && tile.Transform.position.y > bestY)
                {
                    bestY = tile.Transform.position.y;
                    waterY = bestY;
                }
            }

            // Two overlapping sines → organic bob
            float bob  = Mathf.Sin(Time.time * 1.1f  + boat.BobPhase) * 0.022f
                       + Mathf.Sin(Time.time * 0.43f + boat.BobPhase * 0.7f) * 0.010f;
            // Two overlapping sines → smooth roll
            float roll = Mathf.Sin(Time.time * 0.7f  + boat.RockPhase) * 1.2f
                       + Mathf.Sin(Time.time * 1.3f  + boat.RockPhase * 1.4f) * 0.5f;

            float laneZ = GetRiverBoatLaneZ(boat.TravelDirection);
            // +0.13f lifts hull so bottom sits on water surface, not center
            boat.RootTransform.position = new Vector3(boat.WorldX, waterY + 0.13f + bob, laneZ);
            boat.RootTransform.rotation = Quaternion.Euler(0f, boat.TravelDirection > 0f ? 90f : -90f, roll);

            // Lantern — on at night, same threshold as bus headlights
            float darkness = 1f - currentStylizedDaylight;
            bool lanternOn = darkness > 0.55f;
            float lanternIntensity = lanternOn
                ? Mathf.Lerp(0.3f, 1.2f, Mathf.InverseLerp(0.55f, 1f, darkness))
                : 0f;
            Color lampColor = Color.Lerp(
                new Color(0.34f, 0.30f, 0.22f),
                new Color(1f, 0.88f, 0.62f),
                Mathf.Clamp01(lanternIntensity / 1.2f));

            if (boat.LanternLight != null)
            {
                boat.LanternLight.enabled = lanternOn;
                boat.LanternLight.intensity = lanternIntensity;
            }
            if (boat.LanternRenderer != null)
            {
                boat.LanternRenderer.material.color = lampColor;
            }

            // Boat motor audio — fade in when inside river, fade out approaching edge
            if (boat.BoatAudioSource != null)
            {
                float margin = 2f;
                float edgeFade = Mathf.Clamp01(Mathf.Min(boat.WorldX / margin, (GridWidth - boat.WorldX) / margin));
                float targetVol = boat.HasEnteredRiver ? 0.28f * edgeFade : 0f;
                boat.BoatAudioSource.volume = Mathf.MoveTowards(
                    boat.BoatAudioSource.volume, targetVol, 0.6f * Time.deltaTime);
            }
        }
    }

    private float GetRiverBoatLaneZ(float direction)
    {
        // Two lanes inside the river strip (y=28..31 → Z≈28.5 and 30.5)
        int shoreRow = GridHeight - WaterRiverWidth;
        return direction > 0f
            ? shoreRow + 0.5f   // near-shore lane, left-to-right
            : shoreRow + 2.5f;  // deep lane, right-to-left
    }

    private void TrySpawnRiverBoat(float travelDirection)
    {
        float spawnX = travelDirection > 0f ? -1.8f : GridWidth + 1.8f;
        for (int i = 0; i < riverBoats.Count; i++)
        {
            RiverBoatData b = riverBoats[i];
            if (b == null || b.RootTransform == null) continue;
            if (Mathf.Sign(b.TravelDirection) != Mathf.Sign(travelDirection)) continue;
            if (Mathf.Abs(b.WorldX - spawnX) < 6f) return;
        }
        CreateRiverBoat(travelDirection);
    }

    private void CreateRiverBoat(float travelDirection)
    {
        if (riverBoatRoot == null) return;

        float spawnX = travelDirection > 0f ? -1.8f : GridWidth + 1.8f;
        float laneZ  = GetRiverBoatLaneZ(travelDirection);

        // Pick a random boat colour: wooden brown, white, or teal
        Color[] hullColors = {
            new Color(0.62f, 0.42f, 0.24f),
            new Color(0.94f, 0.92f, 0.84f),
            new Color(0.22f, 0.58f, 0.62f),
        };
        Color hullColor = hullColors[Random.Range(0, hullColors.Length)];
        Color cabinColor = new Color(0.96f, 0.94f, 0.86f);
        Color roofColor  = new Color(0.72f, 0.26f, 0.18f);
        Color windowColor = new Color(0.72f, 0.88f, 0.96f);

        GameObject boatRoot = new($"RiverBoat_{riverBoats.Count + 1}");
        boatRoot.transform.SetParent(riverBoatRoot, false);

        // Geometry is built along local Z = forward (travel direction).
        // No extra 90° Y rotation needed — boat spawns facing +Z or -Z via Y=0/180.

        // Hull — main flat body, long along Z (travel axis)
        GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hull.transform.SetParent(boatRoot.transform, false);
        hull.transform.localPosition = new Vector3(0f, -0.06f, 0f);
        hull.transform.localScale = new Vector3(0.52f, 0.14f, 1.2f);
        ApplyColor(hull, hullColor);
        ConfigureShadowVisual(hull);

        // Hull sides — raised gunwale strips along the length
        foreach (float sx in new[] { -0.24f, 0.24f })
        {
            GameObject gunwale = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gunwale.transform.SetParent(boatRoot.transform, false);
            gunwale.transform.localPosition = new Vector3(sx, 0.01f, 0f);
            gunwale.transform.localScale = new Vector3(0.06f, 0.08f, 1.18f);
            ApplyColor(gunwale, hullColor);
            ConfigureShadowVisual(gunwale);
        }

        // Bow — tapered front end (positive Z = front)
        GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bow.transform.SetParent(boatRoot.transform, false);
        bow.transform.localPosition = new Vector3(0f, -0.05f, 0.62f);
        bow.transform.localScale = new Vector3(0.36f, 0.12f, 0.22f);
        bow.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        ApplyColor(bow, hullColor);
        ConfigureShadowVisual(bow);

        // Cabin — sits toward the rear
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(boatRoot.transform, false);
        cabin.transform.localPosition = new Vector3(0f, 0.18f, -0.14f);
        cabin.transform.localScale = new Vector3(0.42f, 0.32f, 0.48f);
        ApplyColor(cabin, cabinColor);
        ConfigureShadowVisual(cabin);

        // Roof
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(boatRoot.transform, false);
        roof.transform.localPosition = new Vector3(0f, 0.36f, -0.14f);
        roof.transform.localScale = new Vector3(0.46f, 0.07f, 0.52f);
        ApplyColor(roof, roofColor);
        ConfigureShadowVisual(roof);

        // Front cabin window (faces forward = +Z)
        GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window.transform.SetParent(boatRoot.transform, false);
        window.transform.localPosition = new Vector3(0f, 0.18f, 0.1f);
        window.transform.localScale = new Vector3(0.3f, 0.18f, 0.03f);
        ApplyColor(window, windowColor);
        ConfigureShadowVisual(window);

        // Chimney / smokestack
        GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        chimney.transform.SetParent(boatRoot.transform, false);
        chimney.transform.localPosition = new Vector3(0f, 0.52f, -0.22f);
        chimney.transform.localScale = new Vector3(0.07f, 0.14f, 0.07f);
        ApplyColor(chimney, new Color(0.14f, 0.14f, 0.16f));
        ConfigureShadowVisual(chimney);

        // Mast
        GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mast.transform.SetParent(boatRoot.transform, false);
        mast.transform.localPosition = new Vector3(0f, 0.62f, 0.28f);
        mast.transform.localScale = new Vector3(0.03f, 0.28f, 0.03f);
        ApplyColor(mast, new Color(0.72f, 0.64f, 0.52f));
        ConfigureShadowVisual(mast);

        // Lantern head — glowing cube on top of mast
        GameObject lanternHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lanternHead.transform.SetParent(boatRoot.transform, false);
        lanternHead.transform.localPosition = new Vector3(0f, 0.94f, 0.28f);
        lanternHead.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
        ApplyColor(lanternHead, new Color(0.34f, 0.30f, 0.22f));
        ConfigureShadowVisual(lanternHead);
        Renderer lanternRenderer = lanternHead.GetComponent<Renderer>();

        // Lantern point light
        GameObject lanternLightObj = new("BoatLantern");
        lanternLightObj.transform.SetParent(boatRoot.transform, false);
        lanternLightObj.transform.localPosition = new Vector3(0f, 0.98f, 0.28f);
        Light lanternLight = lanternLightObj.AddComponent<Light>();
        lanternLight.type = LightType.Point;
        lanternLight.color = new Color(1f, 0.88f, 0.62f);
        lanternLight.range = 2.8f;
        lanternLight.intensity = 0f;
        lanternLight.shadows = LightShadows.None;
        lanternLight.enabled = false;

        const float waterSurfaceY = 0.22f;
        boatRoot.transform.position = new Vector3(spawnX, waterSurfaceY, laneZ);
        boatRoot.transform.rotation = Quaternion.Euler(0f, travelDirection > 0f ? 90f : -90f, 0f);

        // Boat motor audio source — spatial, loops quietly while in scene
        AudioSource boatAudio = null;
        if (boatMotorClip != null)
        {
            boatAudio = boatRoot.AddComponent<AudioSource>();
            boatAudio.clip = boatMotorClip;
            boatAudio.loop = true;
            boatAudio.volume = 0f;
            boatAudio.spatialBlend = 1f;
            boatAudio.rolloffMode = AudioRolloffMode.Linear;
            boatAudio.minDistance = 3f;
            boatAudio.maxDistance = 18f;
            boatAudio.dopplerLevel = 0f;
            boatAudio.pitch = Random.Range(0.94f, 1.06f);
            boatAudio.Play();
        }

        riverBoats.Add(new RiverBoatData
        {
            RootTransform   = boatRoot.transform,
            WorldX          = spawnX,
            TravelDirection = travelDirection,
            Speed           = 1.1f * Random.Range(0.88f, 1.12f),
            BobPhase        = Random.Range(0f, 10f),
            RockPhase       = Random.Range(0f, 10f),
            HasEnteredRiver = false,
            LanternRenderer = lanternRenderer,
            LanternLight    = lanternLight,
            BoatAudioSource = boatAudio,
        });
    }

    private void CreateEdgeHighwayTile(Transform parent, Vector2Int cell, bool horizontal, bool vertical)
    {
        if (!IsInsideGrid(cell) || edgeHighwayCells.Contains(cell))
        {
            return;
        }

        edgeHighwayCells.Add(cell);

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = $"EdgeHighway_{cell.x}_{cell.y}";
        road.transform.SetParent(parent, false);
        road.transform.position = GetCellCenter(cell) + new Vector3(0f, RoadHeight - 0.015f, 0f);
        road.transform.localScale = new Vector3(horizontal ? 1.12f : 0.94f, 0.16f, vertical ? 1.12f : 0.94f);
        ApplyColor(road, new Color(0.16f, 0.17f, 0.19f));
        ConfigureStaticVisual(road);

        GameObject roadTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadTop.name = "EdgeHighwayTop";
        roadTop.transform.SetParent(road.transform, false);
        roadTop.transform.localPosition = new Vector3(0f, 0.32f, 0f);
        roadTop.transform.localScale = new Vector3(horizontal ? 0.94f : 0.74f, 0.16f, vertical ? 0.94f : 0.74f);
        ApplyColor(roadTop, new Color(0.56f, 0.58f, 0.6f));
        ConfigureStaticVisual(roadTop);

        if (horizontal)
        {
            CreateEdgeHighwayLaneStripe(road.transform, new Vector3(0f, 0.46f, 0.23f), new Vector3(0.84f, 0.06f, 0.08f));
            CreateEdgeHighwayLaneStripe(road.transform, new Vector3(0f, 0.46f, -0.23f), new Vector3(0.84f, 0.06f, 0.08f));
        }

        if (vertical)
        {
            CreateEdgeHighwayLaneStripe(road.transform, new Vector3(0.23f, 0.46f, 0f), new Vector3(0.08f, 0.06f, 0.84f));
            CreateEdgeHighwayLaneStripe(road.transform, new Vector3(-0.23f, 0.46f, 0f), new Vector3(0.08f, 0.06f, 0.84f));
        }
    }

    private void CreateEdgeHighwayLaneStripe(Transform parent, Vector3 localPosition, Vector3 localScale)
    {
        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.name = "EdgeHighwayStripe";
        stripe.transform.SetParent(parent, false);
        stripe.transform.localPosition = localPosition;
        stripe.transform.localScale = localScale;
        ApplyColor(stripe, new Color(0.88f, 0.83f, 0.68f));
        ConfigureStaticVisual(stripe);
    }

    private void CreateEdgeHighwayCenterLine(Transform parent, int lowerLaneY, int upperLaneY)
    {
        float centerZ = (lowerLaneY + upperLaneY + 1f) * 0.5f;
        for (int x = 0; x < GridWidth; x += 2)
        {
            GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dash.name = $"EdgeHighwayCenterDash_{x}";
            dash.transform.SetParent(parent, false);
            dash.transform.position = new Vector3(x + 0.5f, GetTerrainHeight(new Vector2Int(x, lowerLaneY)) + RoadHeight + 0.06f, centerZ);
            dash.transform.localScale = new Vector3(0.68f, 0.04f, 0.08f);
            ApplyColor(dash, new Color(0.92f, 0.92f, 0.9f));
            ConfigureStaticVisual(dash);
        }
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
                SessionDebugLogger.Log("BUS_SPAWN", "CityLane spawn skipped: hiring-arrival bus is active.");
            }
            else if (pauseCitySideLane)
            {
                SessionDebugLogger.Log("BUS_SPAWN", "CityLane spawn skipped: lane paused.");
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
                SessionDebugLogger.Log("BUS_SPAWN", "OuterLane spawn skipped: lane paused.");
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
            float bob = Mathf.Sin(Time.time * 3.2f + bus.BobPhase) * 0.015f;
            float y = SampleTerrainHeight(bus.WorldX, laneZ) + RoadHeight + EdgeHighwayBusLift + bob;
            bus.RootTransform.position = new Vector3(bus.WorldX, y, laneZ);
            bus.RootTransform.rotation = bus.TravelDirection > 0f
                ? Quaternion.identity
                : Quaternion.Euler(0f, 180f, 0f);

            float darkness = 1f - currentStylizedDaylight;
            bool headlightsOn = darkness > 0.55f;
            float headlightIntensity = headlightsOn ? Mathf.Lerp(0.4f, 1.75f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
            Color lampColor = Color.Lerp(
                new Color(0.34f, 0.3f, 0.22f),
                new Color(1f, 0.94f, 0.78f),
                Mathf.Clamp01(headlightIntensity / 1.75f));

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
                    PlayAmbientFx(edgeHighwayBusPassbyClip, bus.RootTransform.position, 0.34f);
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
                SessionDebugLogger.Log(
                    "BUS_SPAWN",
                    $"{laneLabel} spawn blocked by spacing: existingX={existing.WorldX:0.00}, spawnX={spawnX:0.00}, direction={(travelDirection > 0f ? "+" : "-")}.");
                return;
            }
        }

        SessionDebugLogger.Log(
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

    private void CreateEdgeHighwayBus(float travelDirection, bool isCitySideLane)
    {
        if (edgeHighwayBusRoot == null)
        {
            return;
        }

        float spawnX = travelDirection > 0f ? -1.6f : GridWidth + 1.6f;
        float laneZ = GetEdgeHighwayBusLaneWorldZ(isCitySideLane);
        GameObject busRoot = new($"EdgeHighwayBus_{edgeHighwayBuses.Count + 1}");
        busRoot.transform.SetParent(edgeHighwayBusRoot, false);

        Color bodyColor = Random.value < 0.5f
            ? new Color(0.9f, 0.26f, 0.22f)
            : new Color(0.24f, 0.5f, 0.86f);
        Color roofColor = new Color(0.94f, 0.92f, 0.84f);
        Color windowColor = new Color(0.72f, 0.88f, 0.95f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(busRoot.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.26f, 0f);
        body.transform.localScale = new Vector3(1.24f, 0.42f, 0.44f);
        ApplyColor(body, bodyColor);
        ConfigureShadowVisual(body);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(busRoot.transform, false);
        roof.transform.localPosition = new Vector3(-0.02f, 0.56f, 0f);
        roof.transform.localScale = new Vector3(1.02f, 0.12f, 0.4f);
        ApplyColor(roof, roofColor);
        ConfigureShadowVisual(roof);

        GameObject windowBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windowBand.transform.SetParent(busRoot.transform, false);
        windowBand.transform.localPosition = new Vector3(-0.02f, 0.38f, 0f);
        windowBand.transform.localScale = new Vector3(0.94f, 0.18f, 0.46f);
        ApplyColor(windowBand, windowColor);
        ConfigureShadowVisual(windowBand);

        GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windshield.transform.SetParent(busRoot.transform, false);
        windshield.transform.localPosition = new Vector3(0.56f, 0.41f, 0f);
        windshield.transform.localScale = new Vector3(0.12f, 0.2f, 0.38f);
        ApplyColor(windshield, new Color(0.66f, 0.86f, 0.94f));
        ConfigureShadowVisual(windshield);

        GameObject rearWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rearWindow.transform.SetParent(busRoot.transform, false);
        rearWindow.transform.localPosition = new Vector3(-0.56f, 0.39f, 0f);
        rearWindow.transform.localScale = new Vector3(0.08f, 0.17f, 0.34f);
        ApplyColor(rearWindow, new Color(0.66f, 0.84f, 0.92f));
        ConfigureShadowVisual(rearWindow);

        GameObject headlightLeftVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlightLeftVisual.transform.SetParent(busRoot.transform, false);
        headlightLeftVisual.transform.localPosition = new Vector3(0.61f, 0.26f, -0.14f);
        headlightLeftVisual.transform.localScale = new Vector3(0.04f, 0.06f, 0.08f);
        ApplyColor(headlightLeftVisual, new Color(0.34f, 0.3f, 0.22f));
        ConfigureShadowVisual(headlightLeftVisual);

        GameObject headlightRightVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlightRightVisual.transform.SetParent(busRoot.transform, false);
        headlightRightVisual.transform.localPosition = new Vector3(0.61f, 0.26f, 0.14f);
        headlightRightVisual.transform.localScale = new Vector3(0.04f, 0.06f, 0.08f);
        ApplyColor(headlightRightVisual, new Color(0.34f, 0.3f, 0.22f));
        ConfigureShadowVisual(headlightRightVisual);

        GameObject sideStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sideStripe.transform.SetParent(busRoot.transform, false);
        sideStripe.transform.localPosition = new Vector3(0f, 0.23f, 0f);
        sideStripe.transform.localScale = new Vector3(1.08f, 0.06f, 0.47f);
        ApplyColor(sideStripe, new Color(0.98f, 0.86f, 0.2f));
        ConfigureShadowVisual(sideStripe);

        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.transform.SetParent(busRoot.transform, false);
        door.transform.localPosition = new Vector3(0.18f, 0.23f, -0.22f);
        door.transform.localScale = new Vector3(0.24f, 0.32f, 0.05f);
        ApplyColor(door, new Color(0.92f, 0.94f, 0.98f));
        ConfigureShadowVisual(door);

        float[] wheelX = { -0.38f, 0.38f };
        float[] wheelZ = { -0.18f, 0.18f };
        foreach (float wx in wheelX)
        {
            foreach (float wz in wheelZ)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.transform.SetParent(busRoot.transform, false);
                wheel.transform.localPosition = new Vector3(wx, 0.1f, wz);
                wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                wheel.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);
                ApplyColor(wheel, new Color(0.12f, 0.12f, 0.12f));
                ConfigureShadowVisual(wheel);
            }
        }

        GameObject routePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        routePlate.transform.SetParent(busRoot.transform, false);
        routePlate.transform.localPosition = new Vector3(0.48f, 0.53f, 0f);
        routePlate.transform.localScale = new Vector3(0.18f, 0.08f, 0.3f);
        ApplyColor(routePlate, new Color(0.98f, 0.84f, 0.14f));
        ConfigureShadowVisual(routePlate);

        GameObject leftLightObject = new("BusHeadlightLeft");
        leftLightObject.transform.SetParent(busRoot.transform, false);
        leftLightObject.transform.localPosition = new Vector3(0.64f, 0.28f, -0.14f);
        leftLightObject.transform.localRotation = Quaternion.Euler(8f, 90f, 0f);
        Light leftLight = leftLightObject.AddComponent<Light>();
        leftLight.type = LightType.Spot;
        leftLight.color = new Color(1f, 0.9f, 0.72f);
        leftLight.range = 3.6f;
        leftLight.spotAngle = 42f;
        leftLight.innerSpotAngle = 22f;
        leftLight.intensity = 0f;
        leftLight.shadows = LightShadows.None;
        leftLight.enabled = false;

        GameObject rightLightObject = new("BusHeadlightRight");
        rightLightObject.transform.SetParent(busRoot.transform, false);
        rightLightObject.transform.localPosition = new Vector3(0.64f, 0.28f, 0.14f);
        rightLightObject.transform.localRotation = Quaternion.Euler(8f, 90f, 0f);
        Light rightLight = rightLightObject.AddComponent<Light>();
        rightLight.type = LightType.Spot;
        rightLight.color = new Color(1f, 0.9f, 0.72f);
        rightLight.range = 3.6f;
        rightLight.spotAngle = 42f;
        rightLight.innerSpotAngle = 22f;
        rightLight.intensity = 0f;
        rightLight.shadows = LightShadows.None;
        rightLight.enabled = false;

        float y = SampleTerrainHeight(spawnX, laneZ) + RoadHeight + EdgeHighwayBusLift;
        busRoot.transform.position = new Vector3(spawnX, y, laneZ);
        busRoot.transform.rotation = Quaternion.LookRotation(travelDirection > 0f ? Vector3.right : Vector3.left, Vector3.up);

        Renderer headlightLeftRenderer = headlightLeftVisual.GetComponent<Renderer>();
        Renderer headlightRightRenderer = headlightRightVisual.GetComponent<Renderer>();
        edgeHighwayBuses.Add(new EdgeHighwayBusData
        {
            RootTransform = busRoot.transform,
            WorldX = spawnX,
            TravelDirection = travelDirection,
            IsCitySideLane = isCitySideLane,
            Speed = EdgeHighwayBusSpeed * Random.Range(0.92f, 1.08f),
            BobPhase = Random.Range(0f, 10f),
            BodyColor = bodyColor,
            HasPlayedPassbyAudio = false,
            HasEnteredRoadStrip = false,
            HeadlightLeftRenderer = headlightLeftRenderer,
            HeadlightRightRenderer = headlightRightRenderer,
            HeadlightLeftMaterial = headlightLeftRenderer != null ? headlightLeftRenderer.material : null,
            HeadlightRightMaterial = headlightRightRenderer != null ? headlightRightRenderer.material : null,
            HeadlightLeft = leftLight,
            HeadlightRight = rightLight
        });
    }

    private void CreateHiringArrivalBusVisual()
    {
        if (hiringDriverArrival == null || edgeHighwayBusRoot == null)
        {
            return;
        }

        float spawnX = -1.6f;
        float laneZ = GetEdgeHighwayBusLaneWorldZ(isCitySideLane: false);
        GameObject busRoot = new($"HiringBus_{hiringDriverArrival.Driver?.DriverId ?? 0}");
        busRoot.transform.SetParent(edgeHighwayBusRoot, false);

        Color bodyColor = new(0.28f, 0.58f, 0.9f);
        Color roofColor = new(0.94f, 0.92f, 0.84f);
        Color windowColor = new(0.72f, 0.88f, 0.95f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(busRoot.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.26f, 0f);
        body.transform.localScale = new Vector3(1.24f, 0.42f, 0.44f);
        ApplyColor(body, bodyColor);
        ConfigureShadowVisual(body);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(busRoot.transform, false);
        roof.transform.localPosition = new Vector3(-0.02f, 0.56f, 0f);
        roof.transform.localScale = new Vector3(1.02f, 0.12f, 0.4f);
        ApplyColor(roof, roofColor);
        ConfigureShadowVisual(roof);

        GameObject windowBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windowBand.transform.SetParent(busRoot.transform, false);
        windowBand.transform.localPosition = new Vector3(-0.02f, 0.38f, 0f);
        windowBand.transform.localScale = new Vector3(0.94f, 0.18f, 0.46f);
        ApplyColor(windowBand, windowColor);
        ConfigureShadowVisual(windowBand);

        GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windshield.transform.SetParent(busRoot.transform, false);
        windshield.transform.localPosition = new Vector3(0.56f, 0.41f, 0f);
        windshield.transform.localScale = new Vector3(0.12f, 0.2f, 0.38f);
        ApplyColor(windshield, new Color(0.66f, 0.86f, 0.94f));
        ConfigureShadowVisual(windshield);

        GameObject rearWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rearWindow.transform.SetParent(busRoot.transform, false);
        rearWindow.transform.localPosition = new Vector3(-0.56f, 0.39f, 0f);
        rearWindow.transform.localScale = new Vector3(0.08f, 0.17f, 0.34f);
        ApplyColor(rearWindow, new Color(0.66f, 0.84f, 0.92f));
        ConfigureShadowVisual(rearWindow);

        GameObject headlightLeftVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlightLeftVisual.transform.SetParent(busRoot.transform, false);
        headlightLeftVisual.transform.localPosition = new Vector3(0.61f, 0.26f, -0.14f);
        headlightLeftVisual.transform.localScale = new Vector3(0.04f, 0.06f, 0.08f);
        ApplyColor(headlightLeftVisual, new Color(0.34f, 0.3f, 0.22f));
        ConfigureShadowVisual(headlightLeftVisual);

        GameObject headlightRightVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlightRightVisual.transform.SetParent(busRoot.transform, false);
        headlightRightVisual.transform.localPosition = new Vector3(0.61f, 0.26f, 0.14f);
        headlightRightVisual.transform.localScale = new Vector3(0.04f, 0.06f, 0.08f);
        ApplyColor(headlightRightVisual, new Color(0.34f, 0.3f, 0.22f));
        ConfigureShadowVisual(headlightRightVisual);

        GameObject sideStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sideStripe.transform.SetParent(busRoot.transform, false);
        sideStripe.transform.localPosition = new Vector3(0f, 0.23f, 0f);
        sideStripe.transform.localScale = new Vector3(1.08f, 0.06f, 0.47f);
        ApplyColor(sideStripe, new Color(0.98f, 0.86f, 0.2f));
        ConfigureShadowVisual(sideStripe);

        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.transform.SetParent(busRoot.transform, false);
        door.transform.localPosition = new Vector3(0.18f, 0.23f, -0.22f);
        door.transform.localScale = new Vector3(0.24f, 0.32f, 0.05f);
        ApplyColor(door, new Color(0.92f, 0.94f, 0.98f));
        ConfigureShadowVisual(door);

        float[] wheelX = { -0.38f, 0.38f };
        float[] wheelZ = { -0.18f, 0.18f };
        foreach (float wx in wheelX)
        {
            foreach (float wz in wheelZ)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.transform.SetParent(busRoot.transform, false);
                wheel.transform.localPosition = new Vector3(wx, 0.1f, wz);
                wheel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                wheel.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);
                ApplyColor(wheel, new Color(0.12f, 0.12f, 0.12f));
                ConfigureShadowVisual(wheel);
            }
        }

        GameObject routePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        routePlate.transform.SetParent(busRoot.transform, false);
        routePlate.transform.localPosition = new Vector3(0.48f, 0.53f, 0f);
        routePlate.transform.localScale = new Vector3(0.18f, 0.08f, 0.3f);
        ApplyColor(routePlate, new Color(0.98f, 0.84f, 0.14f));
        ConfigureShadowVisual(routePlate);

        GameObject leftLightObject = new("HiringBusHeadlightLeft");
        leftLightObject.transform.SetParent(busRoot.transform, false);
        leftLightObject.transform.localPosition = new Vector3(0.64f, 0.28f, -0.14f);
        leftLightObject.transform.localRotation = Quaternion.Euler(8f, 90f, 0f);
        Light leftLight = leftLightObject.AddComponent<Light>();
        leftLight.type = LightType.Spot;
        leftLight.color = new Color(1f, 0.9f, 0.72f);
        leftLight.range = 3.6f;
        leftLight.spotAngle = 42f;
        leftLight.innerSpotAngle = 22f;
        leftLight.intensity = 0f;
        leftLight.shadows = LightShadows.None;
        leftLight.enabled = false;

        GameObject rightLightObject = new("HiringBusHeadlightRight");
        rightLightObject.transform.SetParent(busRoot.transform, false);
        rightLightObject.transform.localPosition = new Vector3(0.64f, 0.28f, 0.14f);
        rightLightObject.transform.localRotation = Quaternion.Euler(8f, 90f, 0f);
        Light rightLight = rightLightObject.AddComponent<Light>();
        rightLight.type = LightType.Spot;
        rightLight.color = new Color(1f, 0.9f, 0.72f);
        rightLight.range = 3.6f;
        rightLight.spotAngle = 42f;
        rightLight.innerSpotAngle = 22f;
        rightLight.intensity = 0f;
        rightLight.shadows = LightShadows.None;
        rightLight.enabled = false;

        hiringDriverArrival.BusRootTransform = busRoot.transform;
        hiringDriverArrival.HeadlightLeftRenderer = headlightLeftVisual.GetComponent<Renderer>();
        hiringDriverArrival.HeadlightRightRenderer = headlightRightVisual.GetComponent<Renderer>();
        hiringDriverArrival.HeadlightLeftMaterial = hiringDriverArrival.HeadlightLeftRenderer != null ? hiringDriverArrival.HeadlightLeftRenderer.material : null;
        hiringDriverArrival.HeadlightRightMaterial = hiringDriverArrival.HeadlightRightRenderer != null ? hiringDriverArrival.HeadlightRightRenderer.material : null;
        hiringDriverArrival.HeadlightLeft = leftLight;
        hiringDriverArrival.HeadlightRight = rightLight;
        hiringDriverArrival.BusWorldX = spawnX;
        hiringDriverArrival.BusSpeed = EdgeHighwayBusSpeed * 0.92f;
        hiringDriverArrival.BobPhase = Random.Range(0f, 10f);
        UpdateHiringBusTransform();
    }

    private void UpdateHiringBusTransform()
    {
        if (hiringDriverArrival == null || hiringDriverArrival.BusRootTransform == null)
        {
            return;
        }

        float laneZ = GetEdgeHighwayBusLaneWorldZ(isCitySideLane: false);
        float bob = Mathf.Sin(Time.time * 3.2f + hiringDriverArrival.BobPhase) * 0.015f;
        float y = SampleTerrainHeight(hiringDriverArrival.BusWorldX, laneZ) + RoadHeight + EdgeHighwayBusLift + bob;
        hiringDriverArrival.BusRootTransform.position = new Vector3(hiringDriverArrival.BusWorldX, y, laneZ);
        hiringDriverArrival.BusRootTransform.rotation = Quaternion.identity;

        float darkness = 1f - currentStylizedDaylight;
        bool headlightsOn = darkness > 0.55f;
        float headlightIntensity = headlightsOn ? Mathf.Lerp(0.4f, 1.75f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
        Color lampColor = Color.Lerp(
            new Color(0.34f, 0.3f, 0.22f),
            new Color(1f, 0.94f, 0.78f),
            Mathf.Clamp01(headlightIntensity / 1.75f));

        if (hiringDriverArrival.HeadlightLeft != null)
        {
            hiringDriverArrival.HeadlightLeft.enabled = headlightsOn;
            hiringDriverArrival.HeadlightLeft.intensity = headlightIntensity;
        }

        if (hiringDriverArrival.HeadlightRight != null)
        {
            hiringDriverArrival.HeadlightRight.enabled = headlightsOn;
            hiringDriverArrival.HeadlightRight.intensity = headlightIntensity;
        }

        if (hiringDriverArrival.HeadlightLeftMaterial != null)
        {
            hiringDriverArrival.HeadlightLeftMaterial.color = lampColor;
        }

        if (hiringDriverArrival.HeadlightRightMaterial != null)
        {
            hiringDriverArrival.HeadlightRightMaterial.color = lampColor;
        }
    }

    private float GetEdgeHighwayBusLaneWorldZ(bool isCitySideLane)
    {
        float centerZ = 1f;
        return centerZ + (isCitySideLane ? EdgeHighwayBusLaneOffset : -EdgeHighwayBusLaneOffset);
    }

}
