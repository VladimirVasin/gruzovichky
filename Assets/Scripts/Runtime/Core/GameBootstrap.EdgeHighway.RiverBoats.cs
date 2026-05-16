using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
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
                if (tile.Cell.x == cellX && tile.Transform != null && tile.CurrentTopY > bestY)
                {
                    bestY = tile.CurrentTopY;
                    waterY = bestY;
                }
            }

            // Two overlapping sines в†’ organic bob
            float bob  = Mathf.Sin(Time.time * 1.1f  + boat.BobPhase) * 0.022f
                       + Mathf.Sin(Time.time * 0.43f + boat.BobPhase * 0.7f) * 0.010f;
            // Two overlapping sines в†’ smooth roll
            float roll = Mathf.Sin(Time.time * 0.7f  + boat.RockPhase) * 1.2f
                       + Mathf.Sin(Time.time * 1.3f  + boat.RockPhase * 1.4f) * 0.5f;

            float laneZ = GetRiverBoatLaneZ(boat.TravelDirection);
            // +0.13f lifts hull so bottom sits on water surface, not center
            boat.RootTransform.position = new Vector3(boat.WorldX, waterY + 0.13f + bob, laneZ);
            boat.RootTransform.rotation = Quaternion.Euler(0f, boat.TravelDirection > 0f ? 90f : -90f, roll);

            // Lantern - on at night, same threshold as bus headlights
            float darkness = 1f - currentStylizedDaylight;
            bool lanternOn = darkness > 0.55f;
            float lanternIntensity = lanternOn
                ? Mathf.Lerp(0.3f, 1.2f, Mathf.InverseLerp(0.55f, 1f, darkness))
                : 0f;
            Color lampColor = Color.Lerp(
                new Color(0.36f, 0.20f, 0.08f),
                new Color(1f, 0.66f, 0.32f),
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

            // Boat motor audio - fade in when inside river, fade out approaching edge
            if (boat.BoatAudioSource != null)
            {
                float margin = 2f;
                float edgeFade = Mathf.Clamp01(Mathf.Min(boat.WorldX / margin, (GridWidth - boat.WorldX) / margin));
                float targetVol = boat.HasEnteredRiver ? 0.28f * edgeFade * GetAudioClipVolumeMultiplier(boatMotorClip) : 0f;
                boat.BoatAudioSource.volume = Mathf.MoveTowards(
                    boat.BoatAudioSource.volume, targetVol, 0.6f * Time.deltaTime);
            }
        }
    }

    private float GetRiverBoatLaneZ(float direction)
    {
        // Two lanes inside the river strip (y=28..31 в†’ Zв‰€28.5 and 30.5)
        int shoreRow = GridHeight - WaterRiverWidth;
        return direction > 0f
            ? shoreRow + 1.5f   // mid-river lane, left-to-right
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
        // No extra 90 deg Y rotation needed - boat spawns facing +Z or -Z via Y=0/180.

        // Hull - main flat body, long along Z (travel axis)
        GameObject hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hull.transform.SetParent(boatRoot.transform, false);
        hull.transform.localPosition = new Vector3(0f, -0.06f, 0f);
        hull.transform.localScale = new Vector3(0.52f, 0.14f, 1.2f);
        ApplyColor(hull, hullColor, VisualSmoothnessWood);
        ConfigureShadowVisual(hull, VisualSmoothnessWood);

        // Hull sides - raised gunwale strips along the length
        foreach (float sx in new[] { -0.24f, 0.24f })
        {
            GameObject gunwale = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gunwale.transform.SetParent(boatRoot.transform, false);
            gunwale.transform.localPosition = new Vector3(sx, 0.01f, 0f);
            gunwale.transform.localScale = new Vector3(0.06f, 0.08f, 1.18f);
            ApplyColor(gunwale, hullColor, VisualSmoothnessWood);
            ConfigureShadowVisual(gunwale, VisualSmoothnessWood);
        }

        // Bow - tapered front end (positive Z = front)
        GameObject bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bow.transform.SetParent(boatRoot.transform, false);
        bow.transform.localPosition = new Vector3(0f, -0.05f, 0.62f);
        bow.transform.localScale = new Vector3(0.36f, 0.12f, 0.22f);
        bow.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        ApplyColor(bow, hullColor, VisualSmoothnessWood);
        ConfigureShadowVisual(bow, VisualSmoothnessWood);

        // Cabin - sits toward the rear
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(boatRoot.transform, false);
        cabin.transform.localPosition = new Vector3(0f, 0.18f, -0.14f);
        cabin.transform.localScale = new Vector3(0.42f, 0.32f, 0.48f);
        ApplyColor(cabin, cabinColor, VisualSmoothnessBuildingWall);
        ConfigureShadowVisual(cabin, VisualSmoothnessBuildingWall);

        // Roof
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(boatRoot.transform, false);
        roof.transform.localPosition = new Vector3(0f, 0.36f, -0.14f);
        roof.transform.localScale = new Vector3(0.46f, 0.07f, 0.52f);
        ApplyColor(roof, roofColor, VisualSmoothnessRoofMetal);
        ConfigureShadowVisual(roof, VisualSmoothnessRoofMetal);

        // Front cabin window (faces forward = +Z)
        GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
        window.transform.SetParent(boatRoot.transform, false);
        window.transform.localPosition = new Vector3(0f, 0.18f, 0.1f);
        window.transform.localScale = new Vector3(0.3f, 0.18f, 0.03f);
        ApplyColor(window, windowColor, VisualSmoothnessGlass);
        ConfigureShadowVisual(window, VisualSmoothnessGlass);

        // Chimney / smokestack
        GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        chimney.transform.SetParent(boatRoot.transform, false);
        chimney.transform.localPosition = new Vector3(0f, 0.52f, -0.22f);
        chimney.transform.localScale = new Vector3(0.07f, 0.14f, 0.07f);
        ApplyColor(chimney, new Color(0.14f, 0.14f, 0.16f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(chimney, VisualSmoothnessVehicleMetal);

        // Mast
        GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mast.transform.SetParent(boatRoot.transform, false);
        mast.transform.localPosition = new Vector3(0f, 0.62f, 0.28f);
        mast.transform.localScale = new Vector3(0.03f, 0.28f, 0.03f);
        ApplyColor(mast, new Color(0.72f, 0.64f, 0.52f), VisualSmoothnessWood);
        ConfigureShadowVisual(mast, VisualSmoothnessWood);

        // Lantern head - glowing cube on top of mast
        GameObject lanternHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lanternHead.transform.SetParent(boatRoot.transform, false);
        lanternHead.transform.localPosition = new Vector3(0f, 0.94f, 0.28f);
        lanternHead.transform.localScale = new Vector3(0.07f, 0.07f, 0.07f);
        ApplyColor(lanternHead, new Color(0.34f, 0.30f, 0.22f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(lanternHead, VisualSmoothnessVehicleMetal);
        Renderer lanternRenderer = lanternHead.GetComponent<Renderer>();

        // Lantern point light
        GameObject lanternLightObj = new("BoatLantern");
        lanternLightObj.transform.SetParent(boatRoot.transform, false);
        lanternLightObj.transform.localPosition = new Vector3(0f, 0.98f, 0.28f);
        Light lanternLight = lanternLightObj.AddComponent<Light>();
        lanternLight.type = LightType.Point;
        lanternLight.color = new Color(1f, 0.66f, 0.32f);
        lanternLight.range = 2.8f;
        lanternLight.intensity = 0f;
        lanternLight.shadows = LightShadows.None;
        lanternLight.enabled = false;

        const float waterSurfaceY = 0.22f;
        boatRoot.transform.position = new Vector3(spawnX, waterSurfaceY, laneZ);
        boatRoot.transform.rotation = Quaternion.Euler(0f, travelDirection > 0f ? 90f : -90f, 0f);

        // Boat motor audio source - spatial, loops quietly while in scene
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


}
