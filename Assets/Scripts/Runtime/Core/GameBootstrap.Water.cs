using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private void ApplyWaterVisualLod(int lodLevel)
    {
        for (int i = 0; i < waterSurfaceTiles.Count; i++)
        {
            WaterSurfaceTileData tile = waterSurfaceTiles[i];
            if (tile?.Transform == null)
            {
                continue;
            }

            bool visible = lodLevel switch
            {
                0 => true,
                1 => tile.LayerIndex <= 1,
                _ => tile.LayerIndex == 0
            };
            if (tile.Transform.gameObject.activeSelf != visible)
            {
                tile.Transform.gameObject.SetActive(visible);
            }
        }

        for (int i = 0; i < waterShoreWashPatches.Count; i++)
        {
            WaterShoreWashPatchData patch = waterShoreWashPatches[i];
            if (patch?.RootTransform == null)
            {
                continue;
            }

            bool visible = lodLevel switch
            {
                0 => true,
                1 => patch.ShoreRingIndex == 0,
                _ => false
            };
            if (patch.RootTransform.gameObject.activeSelf != visible)
            {
                patch.RootTransform.gameObject.SetActive(visible);
            }
        }

        for (int i = 0; i < waterShoreFoams.Count; i++)
        {
            WaterShoreFoamData foam = waterShoreFoams[i];
            if (foam?.RootTransform == null)
            {
                continue;
            }

            bool visible = lodLevel switch
            {
                0 => true,
                1 => i < 2,
                _ => i == 0
            };
            if (foam.RootTransform.gameObject.activeSelf != visible)
            {
                foam.RootTransform.gameObject.SetActive(visible);
            }
        }
    }

    private void GetWaterWaveState(float time, Vector2Int cell, float bobSpeed, float phaseOffset, out float nearShoreT, out float localWaveHeight, out float travelImpulse)
    {
        nearShoreT = Mathf.InverseLerp(GridHeight - WaterRiverWidth, GridHeight - 1, cell.y);

        float along = Mathf.Sin(time * bobSpeed + cell.x * 0.44f + phaseOffset);
        float cross = Mathf.Sin(time * (bobSpeed * 0.73f) - cell.y * 0.82f + phaseOffset * 1.17f);
        float diagonal = Mathf.Sin(time * 0.58f + (cell.x + cell.y) * 0.27f + phaseOffset * 0.61f);
        float chaotic = Mathf.PerlinNoise(
            cell.x * 0.23f + time * 0.18f + phaseOffset,
            cell.y * 0.37f + time * 0.14f + phaseOffset * 0.3f) * 2f - 1f;

        float pulseFrontWidth = 0.95f;
        float pulseTailWidth = 10.0f;
        float pulseSpeed = 3.0f;
        int waterMinY = GridHeight - WaterRiverWidth;
        int waterMaxY = GridHeight - 1;
        float maxPulseDistance = GridWidth - 1f;
        float pulseCycleLength = maxPulseDistance + pulseTailWidth;
        float pulseTravel = time * pulseSpeed;
        int pulseCycleIndex = Mathf.FloorToInt(pulseTravel / pulseCycleLength);
        float cycleNoise = Mathf.PerlinNoise(pulseCycleIndex * 0.713f + 0.19f, 0.271f);
        int sourceY = Mathf.Clamp(waterMinY + Mathf.FloorToInt(cycleNoise * WaterRiverWidth), waterMinY, waterMaxY);
        float pulseHeadDistance = Mathf.Repeat(pulseTravel, pulseCycleLength) - pulseTailWidth;
        float horizontalDistance = cell.x;
        float distFromPulseHead = horizontalDistance - pulseHeadDistance;
        travelImpulse = 0f;
        if (distFromPulseHead >= 0f && distFromPulseHead <= pulseFrontWidth)
        {
            float frontT = 1f - distFromPulseHead / pulseFrontWidth;
            travelImpulse = Mathf.SmoothStep(0f, 1f, frontT);
        }
        else if (distFromPulseHead < 0f && distFromPulseHead >= -pulseTailWidth)
        {
            float trailDistance = -distFromPulseHead;
            float steppedFalloff = Mathf.Clamp01(1f - Mathf.Floor(trailDistance) * 0.1f);
            if (steppedFalloff <= 0.1f)
            {
                float weakNoise = Mathf.PerlinNoise(
                    pulseCycleIndex * 0.417f + cell.x * 0.193f,
                    cell.y * 0.271f + 0.618f);
                steppedFalloff = weakNoise > 0.5f ? 0.1f : 0f;
            }
            travelImpulse = steppedFalloff;
        }

        float rowDistance = Mathf.Abs(cell.y - sourceY);
        float rowFalloff = Mathf.Clamp01(1f - rowDistance * 0.28f);
        float rowBias = 0.94f + Mathf.InverseLerp(GridHeight - WaterRiverWidth, GridHeight - 1f, cell.y) * 0.12f;
        travelImpulse *= rowFalloff * rowBias;
        if (travelImpulse <= 0.001f)
        {
            int idlePatternIndex = Mathf.FloorToInt(time * 3.2f);
            float idleImpulse = 0f;
            for (int hotspotIndex = 0; hotspotIndex < 2; hotspotIndex++)
            {
                float hotspotXNoise = Mathf.PerlinNoise(idlePatternIndex * 0.213f + hotspotIndex * 1.137f, 0.411f);
                float hotspotYNoise = Mathf.PerlinNoise(idlePatternIndex * 0.317f + hotspotIndex * 1.731f, 0.683f);
                int hotspotX = Mathf.Clamp(Mathf.FloorToInt(hotspotXNoise * GridWidth), 0, GridWidth - 1);
                int hotspotY = Mathf.Clamp(waterMinY + Mathf.FloorToInt(hotspotYNoise * WaterRiverWidth), waterMinY, waterMaxY);

                int manhattan = Mathf.Abs(cell.x - hotspotX) + Mathf.Abs(cell.y - hotspotY);
                float hotspotImpulse =
                    manhattan == 0 ? 0.2f :
                    manhattan == 1 ? 0.1f :
                    manhattan == 2 ? 0.05f :
                    0f;

                if (hotspotImpulse > idleImpulse)
                {
                    idleImpulse = hotspotImpulse;
                }
            }

            travelImpulse = idleImpulse;
        }
        localWaveHeight = along * 0.11f + cross * 0.06f + diagonal * 0.05f + chaotic * 0.07f;
    }

    private float GetCurrentVisualWaterHeight(Vector2Int cell)
    {
        float bestTop = SampleTerrainHeight(cell.x + 0.5f, cell.y + 0.5f);
        for (int i = 0; i < waterBodyTiles.Count; i++)
        {
            WaterBodyTileData body = waterBodyTiles[i];
            if (body?.Transform == null || body.Cell != cell)
            {
                continue;
            }

            bestTop = Mathf.Max(bestTop, body.Mesh != null ? body.CurrentTopY : body.Transform.position.y + 0.07f);
        }

        for (int i = 0; i < waterSurfaceTiles.Count; i++)
        {
            WaterSurfaceTileData tile = waterSurfaceTiles[i];
            if (tile?.Transform == null || tile.Cell != cell)
            {
                continue;
            }

            bestTop = Mathf.Max(bestTop, tile.Mesh != null ? tile.CurrentTopY : tile.Transform.position.y);
        }

        return bestTop;
    }

    private void UpdateWaterBodyMesh(WaterBodyTileData waterBody, float time)
    {
        if (waterBody?.Mesh == null)
        {
            return;
        }

        Vector3[] vertices = waterBody.Mesh.vertices;
        float maxY = waterBody.BaseTopY;
        for (int i = 0; i < 4 && i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            vertex.y = waterBody.BaseTopY + GetSharedWaterCornerWaveOffset(vertex.x, vertex.z, time, 0.98f);
            vertices[i] = vertex;
            maxY = Mathf.Max(maxY, vertex.y);
        }

        waterBody.Mesh.vertices = vertices;
        waterBody.Mesh.RecalculateNormals();
        waterBody.Mesh.RecalculateBounds();
        waterBody.CurrentTopY = maxY;
    }

    private void UpdateWaterSurfaceMesh(WaterSurfaceTileData surfaceTile, float time)
    {
        if (surfaceTile?.Mesh == null)
        {
            return;
        }

        Vector3[] vertices = surfaceTile.Mesh.vertices;
        float maxY = surfaceTile.BaseY;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            vertex.y = surfaceTile.BaseY + GetSharedWaterCornerWaveOffset(vertex.x, vertex.z, time, surfaceTile.BobSpeed);
            vertices[i] = vertex;
            maxY = Mathf.Max(maxY, vertex.y);
        }

        surfaceTile.Mesh.vertices = vertices;
        surfaceTile.Mesh.RecalculateNormals();
        surfaceTile.Mesh.RecalculateBounds();
        surfaceTile.CurrentTopY = maxY;
    }

    private float GetSharedWaterCornerWaveOffset(float worldX, float worldZ, float time, float bobSpeed)
    {
        int gridX = Mathf.FloorToInt(worldX);
        int gridY = Mathf.FloorToInt(worldZ);
        float total = 0f;
        int count = 0;

        for (int dx = -1; dx <= 0; dx++)
        {
            for (int dy = -1; dy <= 0; dy++)
            {
                Vector2Int cell = new(gridX + dx, gridY + dy);
                if (!waterCells.Contains(cell))
                {
                    continue;
                }

                GetWaterWaveState(time, cell, bobSpeed, GetDeterministicWaterPhase(cell), out _, out float localWaveHeight, out float travelImpulse);
                total += lakeWaterCells.Contains(cell) ? (localWaveHeight * 0.5f + 0.5f) * 0.026f : travelImpulse * 1.0f;
                count++;
            }
        }

        return count == 0 ? 0f : total / count;
    }

    private static float GetDeterministicWaterPhase(Vector2Int cell)
    {
        return Mathf.PerlinNoise((cell.x + 11) * 0.37f, (cell.y + 23) * 0.41f) * 10f;
    }

    private void UpdateWaterEffects()
    {
        if (waterSurfaceTiles.Count == 0 && waterBodyTiles.Count == 0 && waterShoreFoams.Count == 0)
        {
            return;
        }

        float time = Time.time;
        float shorelineGlow = Mathf.Lerp(0.18f, 0.92f, currentStylizedDaylight);
        float waveWashBrightness = Mathf.Lerp(0.12f, 0.9f, currentStylizedDaylight);
        bool animateWaterBodyMeshes = true;

        for (int i = waterBodyTiles.Count - 1; i >= 0; i--)
        {
            WaterBodyTileData waterBody = waterBodyTiles[i];
            if (waterBody?.Transform == null)
            {
                waterBodyTiles.RemoveAt(i);
                continue;
            }

            if (!animateWaterBodyMeshes)
            {
                waterBody.CurrentTopY = waterBody.BaseTopY;
                continue;
            }

            GetWaterWaveState(time, waterBody.Cell, 0.98f, waterBody.PhaseOffset, out _, out _, out float travelImpulse);
            float bodyLift = travelImpulse * 1.0f;
            if (waterBody.Mesh != null)
            {
                UpdateWaterBodyMesh(waterBody, time);
                continue;
            }

            Vector3 position = waterBody.Transform.position;
            position.y = waterBody.BaseY + bodyLift;
            waterBody.Transform.position = position;
            waterBody.CurrentTopY = waterBody.BaseTopY + bodyLift;
        }

        for (int i = waterSurfaceTiles.Count - 1; i >= 0; i--)
        {
            WaterSurfaceTileData surfaceTile = waterSurfaceTiles[i];
            if (surfaceTile?.Transform == null || surfaceTile.Material == null)
            {
                waterSurfaceTiles.RemoveAt(i);
                continue;
            }

            if (!surfaceTile.Transform.gameObject.activeSelf)
            {
                continue;
            }

            GetWaterWaveState(time, surfaceTile.Cell, surfaceTile.BobSpeed, surfaceTile.PhaseOffset, out float nearShoreT, out float localWaveHeight, out float travelImpulse);
            float sharedWaveOffset = travelImpulse * 1.0f;
            if (surfaceTile.Mesh != null)
            {
                UpdateWaterSurfaceMesh(surfaceTile, time);
            }
            else
            {
                float y = surfaceTile.BaseY + sharedWaveOffset;
                surfaceTile.Transform.position = new Vector3(surfaceTile.Cell.x + 0.5f, y, surfaceTile.Cell.y + 0.5f);
                surfaceTile.CurrentTopY = y;
            }

            Color shoreColor;
            Color deepColor;
            float baseAlpha;
            float alphaRange;
            float highlightBase;
            float highlightWave;
            switch (surfaceTile.LayerIndex)
            {
                case 0:
                    shoreColor = new Color(0.64f, 0.90f, 0.96f);
                    deepColor = new Color(0.12f, 0.42f, 0.72f);
                    baseAlpha = Mathf.Lerp(0.16f, 0.09f, nearShoreT);
                    alphaRange = 0.018f;
                    highlightBase = 0.08f;
                    highlightWave = 0.045f;
                    break;
                case 1:
                    shoreColor = new Color(0.34f, 0.70f, 0.84f);
                    deepColor = new Color(0.08f, 0.28f, 0.56f);
                    baseAlpha = Mathf.Lerp(0.11f, 0.07f, nearShoreT);
                    alphaRange = 0.016f;
                    highlightBase = 0.035f;
                    highlightWave = 0.03f;
                    break;
                default:
                    shoreColor = new Color(0.16f, 0.44f, 0.64f);
                    deepColor = new Color(0.03f, 0.13f, 0.34f);
                    baseAlpha = Mathf.Lerp(0.15f, 0.09f, nearShoreT);
                    alphaRange = 0.010f;
                    highlightBase = 0.015f;
                    highlightWave = 0.012f;
                    break;
            }

            float nightDim = Mathf.Lerp(0.14f, 1f, currentStylizedDaylight);
            Color tileColor = Color.Lerp(shoreColor, deepColor, nearShoreT) * nightDim;
            float whitecap = Mathf.Clamp01(travelImpulse * Mathf.Lerp(1f, 0.28f, nearShoreT));
            float highlight = highlightBase + (localWaveHeight * 0.5f + 0.5f) * highlightWave + whitecap * 0.04f;
            tileColor = Color.Lerp(tileColor, new Color(0.9f, 0.97f, 1f), highlight);
            tileColor.a = Mathf.Clamp01(baseAlpha + localWaveHeight * alphaRange);
            surfaceTile.Material.color = tileColor;
        }

        for (int i = waterShoreWashPatches.Count - 1; i >= 0; i--)
        {
            WaterShoreWashPatchData patch = waterShoreWashPatches[i];
            if (patch?.RootTransform == null || patch.Material == null)
            {
                waterShoreWashPatches.RemoveAt(i);
                continue;
            }

            if (!patch.RootTransform.gameObject.activeSelf)
            {
                continue;
            }

            float cycleT = Mathf.Repeat(time * 0.24f + patch.PhaseOffset, 1f);
            float waveT = Mathf.Sin(cycleT * Mathf.PI);
            waveT = Mathf.Pow(Mathf.Clamp01(waveT), 1.5f);

            bool isSecondRing = patch.ShoreRingIndex == 1;
            bool isWaterEdgeRing = patch.ShoreRingIndex == 2;
            bool active = true;
            if (isSecondRing)
            {
                int cycleIndex = Mathf.FloorToInt(time * 0.24f + patch.PhaseOffset);
                active = Mathf.PerlinNoise(patch.SegmentIndex * 0.73f + 0.21f, cycleIndex * 0.41f + 0.37f) <= 0.7f;
            }

            float alpha = active
                ? (isWaterEdgeRing
                    ? 0.05f + waveT * 0.07f
                    : isSecondRing
                        ? 0.09f + waveT * 0.11f
                        : 0.13f + waveT * 0.13f) * Mathf.Lerp(0.12f, 1f, currentStylizedDaylight)
                : 0f;
            float zPush = isWaterEdgeRing ? waveT * 0.012f : isSecondRing ? waveT * 0.024f : waveT * 0.04f;
            float y = patch.BaseY + waveT * (isWaterEdgeRing ? 0.0025f : isSecondRing ? 0.0035f : 0.006f);
            patch.RootTransform.position = new Vector3(
                patch.BaseX + Mathf.Sin(time * 0.35f + patch.PhaseOffset * 6f) * 0.08f,
                y,
                patch.BaseZ + zPush);
            patch.RootTransform.localScale = new Vector3(
                patch.Width,
                0.008f,
                patch.Depth * (isWaterEdgeRing
                    ? (0.68f + waveT * 0.12f)
                    : isSecondRing
                        ? (0.45f + waveT * 0.34f)
                        : (0.7f + waveT * 0.18f)));

            Color washColor = isWaterEdgeRing
                ? Color.Lerp(new Color(0.34f, 0.52f, 0.62f, 0f), new Color(0.9f, 0.98f, 1f, alpha), 0.4f + waveT * 0.6f)
                : Color.Lerp(new Color(0.28f, 0.38f, 0.48f, 0f), new Color(0.9f, 0.98f, 1f, alpha), 0.5f + waveT * 0.5f);
            washColor *= waveWashBrightness;
            washColor.a = alpha;
            patch.Material.color = washColor;
        }

        float centerX = GridWidth * 0.5f;
        for (int i = waterShoreFoams.Count - 1; i >= 0; i--)
        {
            WaterShoreFoamData foam = waterShoreFoams[i];
            if (foam?.RootTransform == null)
            {
                waterShoreFoams.RemoveAt(i);
                continue;
            }

            if (!foam.RootTransform.gameObject.activeSelf)
            {
                continue;
            }

            float drift = Mathf.Sin(time * foam.DriftSpeed + foam.DriftOffset) * 0.14f;
            float pulse = 0.72f + (Mathf.Sin(time * foam.PulseSpeed + foam.PhaseOffset) * 0.5f + 0.5f) * 0.22f;
            foam.RootTransform.position = new Vector3(centerX + drift, foam.BaseY, foam.BaseZ + Mathf.Sin(time * 0.52f + foam.PhaseOffset) * 0.016f);

            if (foam.Material != null)
            {
                Color foamColor = Color.Lerp(new Color(0.76f, 0.86f, 0.92f), new Color(0.94f, 0.98f, 1f), currentStylizedDaylight) * (pulse * shorelineGlow);
                foamColor.a = 1f;
                foam.Material.color = foamColor;
            }
        }
    }

    private void UpdateRiverFish()
    {
        if (riverFishRoot == null)
        {
            return;
        }

        float dt = Time.deltaTime * Mathf.Max(0.35f, gameSpeedMultiplier);
        riverFishSpawnTimer -= dt;
        if (riverFishSpawnTimer <= 0f)
        {
            SpawnRiverFish();
            riverFishSpawnTimer = Random.Range(5.5f, 11f);
        }

        float time = Time.time;
        float endX = GridWidth - 0.18f;
        for (int i = riverFish.Count - 1; i >= 0; i--)
        {
            RiverFishData fish = riverFish[i];
            if (fish?.RootTransform == null)
            {
                riverFish.RemoveAt(i);
                continue;
            }

            fish.WorldX += fish.SwimSpeed * dt;
            fish.WorldZ += Mathf.Sin(time * fish.LateralDriftSpeed + fish.BobPhase) * fish.LateralDriftAmplitude * dt;
            fish.WorldZ = Mathf.Clamp(fish.WorldZ, GridHeight - WaterRiverWidth + 0.3f, GridHeight - 0.35f);

            float bob = Mathf.Sin(time * 1.8f + fish.BobPhase) * 0.01f;
            fish.RootTransform.position = new Vector3(fish.WorldX, fish.DepthY + bob, fish.WorldZ);
            fish.RootTransform.rotation = Quaternion.Slerp(
                fish.RootTransform.rotation,
                Quaternion.Euler(0f, Mathf.Sin(time * fish.LateralDriftSpeed + fish.BobPhase) * 8f, 0f),
                5f * Time.deltaTime);

            if (fish.BodyTransform != null)
            {
                fish.BodyTransform.localRotation = Quaternion.Euler(0f, 0f, 90f + Mathf.Sin(time * 6.8f + fish.TailPhase) * 4f);
            }

            if (fish.TailTransform != null)
            {
                fish.TailTransform.localRotation = Quaternion.Euler(0f, Mathf.Sin(time * 13.5f + fish.TailPhase) * 28f, 0f);
            }

            float fishBrightness = Mathf.Lerp(0.35f, 0.78f, currentStylizedDaylight);
            if (fish.BodyMaterial != null)
            {
                fish.BodyMaterial.color = fish.BodyColor * fishBrightness;
            }

            if (fish.TailMaterial != null)
            {
                fish.TailMaterial.color = Color.Lerp(fish.BodyColor * 0.88f, new Color(0.62f, 0.72f, 0.76f), 0.18f) * fishBrightness;
            }

            if (fish.WorldX >= endX)
            {
                Destroy(fish.RootTransform.gameObject);
                riverFish.RemoveAt(i);
            }
        }
    }

    private void UpdateLakeFish()
    {
        if (lakeFishRoot == null) return;

        float dt = Time.deltaTime * Mathf.Max(0.35f, gameSpeedMultiplier);
        float time = Time.time;
        float brightness = Mathf.Lerp(0.3f, 0.72f, currentStylizedDaylight);

        for (int i = lakeFish.Count - 1; i >= 0; i--)
        {
            LakeFishData fish = lakeFish[i];
            if (fish?.RootTransform == null) { lakeFish.RemoveAt(i); continue; }

            fish.JumpCooldown -= dt;

            if (fish.IsJumping)
            {
                fish.JumpProgress += dt / fish.JumpDuration;
                if (fish.JumpProgress >= 1f)
                {
                    fish.IsJumping = false;
                    fish.JumpProgress = 0f;
                    fish.IdleTimer = Random.Range(0.8f, 2f);
                    fish.RootTransform.position = new Vector3(fish.WorldX, fish.DepthY, fish.WorldZ);
                    fish.RootTransform.rotation = Quaternion.Euler(0f, fish.Yaw, 0f);
                }
                else
                {
                    float t = fish.JumpProgress;
                    float jumpY = fish.DepthY + fish.JumpPeakHeight * 4f * t * (1f - t);
                    float pitch = Mathf.Lerp(-52f, 52f, t);
                    fish.RootTransform.position = new Vector3(fish.WorldX, jumpY, fish.WorldZ);
                    fish.RootTransform.rotation = Quaternion.Euler(pitch, fish.Yaw, 0f);
                    if (fish.BodyTransform != null)
                        fish.BodyTransform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    if (fish.TailTransform != null)
                        fish.TailTransform.localRotation = Quaternion.Euler(0f, Mathf.Sin(time * 14f + fish.TailPhase) * 30f, 0f);
                }

                if (fish.BodyMaterial != null) fish.BodyMaterial.color = fish.BodyColor * brightness;
                if (fish.TailMaterial != null) fish.TailMaterial.color = Color.Lerp(fish.BodyColor * 0.85f, new Color(0.54f, 0.66f, 0.52f), 0.2f) * brightness;
                continue;
            }

            // Trigger jump when cooldown expires and fish is resting
            if (fish.JumpCooldown <= 0f && fish.IdleTimer > 0f)
            {
                fish.IsJumping = true;
                fish.JumpProgress = 0f;
                fish.JumpDuration = Random.Range(0.5f, 0.8f);
                fish.JumpPeakHeight = Random.Range(0.28f, 0.55f);
                fish.JumpCooldown = Random.Range(10f, 24f);
                continue;
            }

            if (fish.IdleTimer > 0f)
            {
                fish.IdleTimer -= dt;
                float bob = Mathf.Sin(time * 1.4f + fish.BobPhase) * 0.008f;
                fish.RootTransform.position = new Vector3(fish.WorldX, fish.DepthY + bob, fish.WorldZ);
                fish.RootTransform.rotation = Quaternion.Euler(0f, fish.Yaw, 0f);
                if (fish.TailTransform != null)
                    fish.TailTransform.localRotation = Quaternion.Euler(0f, Mathf.Sin(time * 4f + fish.TailPhase) * 12f, 0f);
            }
            else
            {
                float dx = fish.TargetX - fish.WorldX;
                float dz = fish.TargetZ - fish.WorldZ;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist < 0.25f)
                {
                    fish.IdleTimer = Random.Range(1.5f, 3.5f);
                    PickLakeFishTarget(fish);
                }
                else
                {
                    float step = fish.SwimSpeed * dt / dist;
                    fish.WorldX += dx * step;
                    fish.WorldZ += dz * step;
                    fish.Yaw = Mathf.Atan2(dx, dz) * Mathf.Rad2Deg;
                    float bob = Mathf.Sin(time * 1.8f + fish.BobPhase) * 0.01f;
                    fish.RootTransform.position = new Vector3(fish.WorldX, fish.DepthY + bob, fish.WorldZ);
                    fish.RootTransform.rotation = Quaternion.Slerp(
                        fish.RootTransform.rotation,
                        Quaternion.Euler(0f, fish.Yaw, 0f),
                        4f * Time.deltaTime);
                    if (fish.TailTransform != null)
                        fish.TailTransform.localRotation = Quaternion.Euler(0f, Mathf.Sin(time * 10f + fish.TailPhase) * 22f, 0f);
                }
            }

            if (fish.BodyTransform != null)
                fish.BodyTransform.localRotation = Quaternion.Euler(90f, 0f, Mathf.Sin(time * 5f + fish.TailPhase) * 3f);

            if (fish.BodyMaterial != null) fish.BodyMaterial.color = fish.BodyColor * brightness;
            if (fish.TailMaterial != null) fish.TailMaterial.color = Color.Lerp(fish.BodyColor * 0.85f, new Color(0.54f, 0.66f, 0.52f), 0.2f) * brightness;
        }
    }

    private void PickLakeFishTarget(LakeFishData fish)
    {
        if (fish.LakeIndex < 0 || fish.LakeIndex >= perLakeWaterCells.Count) return;
        List<Vector2Int> cells = perLakeWaterCells[fish.LakeIndex];
        if (cells.Count == 0) return;

        for (int attempt = 0; attempt < 8; attempt++)
        {
            Vector2Int cell = cells[Random.Range(0, cells.Count)];
            float tx = cell.x + 0.5f;
            float tz = cell.y + 0.5f;
            float d = Mathf.Sqrt((fish.WorldX - tx) * (fish.WorldX - tx) + (fish.WorldZ - tz) * (fish.WorldZ - tz));
            if (d >= 0.8f && d <= 5f)
            {
                fish.TargetX = tx;
                fish.TargetZ = tz;
                return;
            }
        }

        Vector2Int fallback = cells[Random.Range(0, cells.Count)];
        fish.TargetX = fallback.x + 0.5f;
        fish.TargetZ = fallback.y + 0.5f;
    }

    private void CreateCloudLump(Transform parent, Vector3 localPosition, Vector3 localScale)
    {
        GameObject lump = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lump.transform.SetParent(parent, false);
        lump.transform.localPosition = localPosition;
        lump.transform.localScale = localScale;
        ApplyColor(lump, new Color(0.97f, 0.98f, 1f));
        Renderer rendererComponent = lump.GetComponent<Renderer>();
        if (rendererComponent != null)
        {
            rendererComponent.shadowCastingMode = ShadowCastingMode.On;
            rendererComponent.receiveShadows = false;
        }

        Collider colliderComponent = lump.GetComponent<Collider>();
        if (colliderComponent != null)
        {
            colliderComponent.enabled = false;
        }
    }

    private void UpdateDistantClouds()
    {
        if (distantClouds.Count == 0 || mainCamera == null)
        {
            return;
        }

        float dt = Time.deltaTime * gameSpeedMultiplier;
        float time = Time.time;

        for (int i = 0; i < distantClouds.Count; i++)
        {
            DistantCloudData cloud = distantClouds[i];
            if (cloud.RootTransform == null)
            {
                continue;
            }

            cloud.TravelOffset += cloud.TravelSpeed * dt;
            if (cloud.TravelOffset > CloudTravelLength)
            {
                cloud.TravelOffset -= CloudTravelLength;
            }

            float bob = Mathf.Sin(time * cloud.VerticalBobSpeed + cloud.PhaseOffset * 1.9f) * cloud.VerticalBobAmplitude;
            Vector3 pos = cloud.SpawnPosition + CloudTravelDir * cloud.TravelOffset;
            pos.y += bob;
            cloud.RootTransform.position = pos;
        }
    }

}

