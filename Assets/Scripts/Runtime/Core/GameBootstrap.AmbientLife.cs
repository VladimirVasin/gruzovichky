using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private void SetupDistantClouds()
    {
        distantClouds.Clear();

        // SpawnPosition = behind left/near edge; clouds travel along CloudTravelDir, staggered via initialOffset
        // Args: spawnPosition, travelSpeed, bobAmplitude, bobSpeed, phaseOffset, scale, initialOffset
        // Z spread covers from well before the grid to well beyond it, matching full screen top-to-bottom
        Vector3 center = new(GridWidth * 0.5f, 0f, GridHeight * 0.5f);
        Vector3 spawnBase = center + new Vector3(-30f, 0f, -4f);

        // Near (bottom screen) — lower Y so they sit closer to ground-level perspective
        CreateDistantCloud(spawnBase + new Vector3(0f, 10f, -18f), 1.0f, 0.7f,  0.44f, 0.30f, 1.8f,   4f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 11f, -12f), 0.8f, 0.85f, 0.38f, 1.50f, 2.0f,  22f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 12f,  -7f), 1.2f, 0.75f, 0.41f, 0.80f, 2.15f,  0f);
        // Mid (center screen)
        CreateDistantCloud(spawnBase + new Vector3(0f, 14f,  -1f), 1.1f, 0.9f,  0.36f, 2.10f, 2.35f, 14f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 15f,   5f), 0.9f, 0.72f, 0.48f, 0.60f, 2.05f, 36f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 16f,  11f), 1.3f, 0.95f, 0.32f, 1.90f, 2.4f,  50f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 14f,   8f), 0.8f, 0.68f, 0.34f, 1.20f, 1.95f, 28f);
        // Far (top screen)
        CreateDistantCloud(spawnBase + new Vector3(0f, 17f,  17f), 1.5f, 0.82f, 0.38f, 2.80f, 2.2f,   9f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 18f,  23f), 1.0f, 0.9f,  0.42f, 0.40f, 2.35f, 42f);
        CreateDistantCloud(spawnBase + new Vector3(0f, 17f,  30f), 1.2f, 0.78f, 0.46f, 1.60f, 2.1f,  18f);
    }

    private void CreateDistantCloud(Vector3 spawnPosition, float travelSpeed, float bobAmplitude, float bobSpeed, float phaseOffset, float scaleMultiplier, float initialOffset)
    {
        GameObject cloudRoot = new($"DistantCloud_{distantClouds.Count + 1}");
        cloudRoot.transform.SetParent(worldRoot, false);
        cloudRoot.transform.position = spawnPosition + CloudTravelDir * initialOffset;
        cloudRoot.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        cloudRoot.transform.localScale = Vector3.one * scaleMultiplier;

        CreateCloudLump(cloudRoot.transform, new Vector3(-0.95f, 0f, 0f), new Vector3(1.5f, 0.8f, 0.9f));
        CreateCloudLump(cloudRoot.transform, new Vector3(0f, 0.18f, 0f), new Vector3(1.9f, 1f, 1f));
        CreateCloudLump(cloudRoot.transform, new Vector3(1.02f, 0.02f, 0.08f), new Vector3(1.4f, 0.74f, 0.86f));
        CreateCloudLump(cloudRoot.transform, new Vector3(0.18f, -0.12f, 0.18f), new Vector3(1.7f, 0.54f, 0.86f));

        cloudRoot.SetActive(true);
        distantClouds.Add(new DistantCloudData
        {
            RootTransform = cloudRoot.transform,
            SpawnPosition = spawnPosition,
            TravelOffset = initialOffset,
            TravelSpeed = travelSpeed,
            VerticalBobAmplitude = bobAmplitude,
            VerticalBobSpeed = bobSpeed,
            PhaseOffset = phaseOffset
        });
    }

    private void SetupAmbientAirParticles()
    {
        ambientAirParticles.Clear();
        if (ambientAirRoot != null)
        {
            Destroy(ambientAirRoot.gameObject);
        }

        if (worldRoot == null)
        {
            return;
        }

        ambientAirRoot = new GameObject("AmbientAirParticles").transform;
        ambientAirRoot.SetParent(worldRoot, false);

        Vector3 globalCenter = new(GridWidth * 0.5f, 0f, GridHeight * 0.54f);
        for (int i = 0; i < AmbientAirGlobalParticleCount; i++)
        {
            CreateAmbientAirParticle(
                $"AmbientDust_{i + 1}",
                globalCenter,
                halfTravelRange: 20f,
                halfLateralRange: 13f,
                heightMin: 0.95f,
                heightMax: 2.4f,
                speedMin: 0.1f,
                speedMax: 0.19f,
                bobAmplitudeMin: 0.018f,
                bobAmplitudeMax: 0.05f,
                baseColor: new Color(0.96f, 0.94f, 0.86f),
                scaleMin: 0.03f,
                scaleMax: 0.055f,
                isForestLocal: false,
                isHighwayDust: false);
        }

        Vector3 highwayCenter = new(GridWidth * 0.5f, 0f, 1.05f);
        for (int i = 0; i < AmbientAirHighwayDustParticleCount; i++)
        {
            CreateAmbientAirParticle(
                $"HighwayDust_{i + 1}",
                highwayCenter,
                halfTravelRange: GridWidth * 0.56f,
                halfLateralRange: 0.95f,
                heightMin: 0.32f,
                heightMax: 0.82f,
                speedMin: 0.14f,
                speedMax: 0.26f,
                bobAmplitudeMin: 0.014f,
                bobAmplitudeMax: 0.04f,
                baseColor: new Color(0.9f, 0.84f, 0.72f),
                scaleMin: 0.032f,
                scaleMax: 0.06f,
                isForestLocal: false,
                isHighwayDust: true);
        }

        if (locations.TryGetValue(LocationType.Forest, out LocationData forest))
        {
            Vector3 forestCenter = GetLocationCenter(LocationType.Forest);
            float forestRadiusX = Mathf.Max(3.2f, (forest.Max.x - forest.Min.x + 1) * 1.15f);
            float forestRadiusZ = Mathf.Max(2.6f, (forest.Max.y - forest.Min.y + 1) * 1.15f);
            for (int i = 0; i < AmbientAirForestParticleCount; i++)
            {
                CreateAmbientAirParticle(
                    $"ForestMote_{i + 1}",
                    forestCenter,
                    halfTravelRange: forestRadiusX,
                    halfLateralRange: forestRadiusZ,
                    heightMin: 0.7f,
                    heightMax: 1.75f,
                    speedMin: 0.12f,
                    speedMax: 0.23f,
                    bobAmplitudeMin: 0.026f,
                    bobAmplitudeMax: 0.065f,
                    baseColor: new Color(0.88f, 0.94f, 0.74f),
                    scaleMin: 0.035f,
                    scaleMax: 0.07f,
                    isForestLocal: true,
                    isHighwayDust: false);
            }
        }
    }

    private void CreateAmbientAirParticle(
        string name,
        Vector3 center,
        float halfTravelRange,
        float halfLateralRange,
        float heightMin,
        float heightMax,
        float speedMin,
        float speedMax,
        float bobAmplitudeMin,
        float bobAmplitudeMax,
        Color baseColor,
        float scaleMin,
        float scaleMax,
        bool isForestLocal,
        bool isHighwayDust)
    {
        if (ambientAirRoot == null)
        {
            return;
        }

        GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        particle.name = name;
        particle.transform.SetParent(ambientAirRoot, false);
        float scale = Random.Range(scaleMin, scaleMax);
        particle.transform.localScale = Vector3.one * scale;
        if (particle.TryGetComponent(out Collider collider))
        {
            collider.enabled = false;
        }

        Color color = Color.Lerp(baseColor * 0.92f, Color.white, Random.Range(0.08f, 0.22f));
        ApplyUnlitColor(particle, color);
        ConfigureStaticVisual(particle);

        Renderer particleRenderer = particle.GetComponent<Renderer>();
        AmbientAirParticleData data = new()
        {
            RootTransform = particle.transform,
            Renderer = particleRenderer,
            Material = particleRenderer != null ? particleRenderer.material : null,
            Center = center,
            HalfTravelRange = halfTravelRange,
            HalfLateralRange = halfLateralRange,
            HeightMin = heightMin,
            HeightMax = heightMax,
            TravelSpeed = Random.Range(speedMin, speedMax),
            BobAmplitude = Random.Range(bobAmplitudeMin, bobAmplitudeMax),
            BobSpeed = Random.Range(0.35f, 0.75f),
            PhaseOffset = Random.Range(0f, 10f),
            BaseColor = color,
            IsForestLocal = isForestLocal,
            IsHighwayDust = isHighwayDust
        };

        ResetAmbientAirParticle(data, randomizeAlongPath: true);
        ambientAirParticles.Add(data);
    }

    private void ResetAmbientAirParticle(AmbientAirParticleData particle, bool randomizeAlongPath)
    {
        if (particle == null)
        {
            return;
        }

        particle.TravelOffset = randomizeAlongPath
            ? Random.Range(-particle.HalfTravelRange, particle.HalfTravelRange)
            : -particle.HalfTravelRange;
        particle.LateralOffset = Random.Range(-particle.HalfLateralRange, particle.HalfLateralRange);
        particle.BaseHeightOffset = Random.Range(particle.HeightMin, particle.HeightMax);
        particle.PhaseOffset = Random.Range(0f, 10f);
    }

    private void UpdateAmbientAirParticles()
    {
        if (ambientAirParticles.Count == 0)
        {
            return;
        }

        Vector3 windDirection = CloudTravelDir;
        Vector3 windRight = new(-windDirection.z, 0f, windDirection.x);
        float dt = Time.deltaTime * gameSpeedMultiplier;
        float daylightStrength = Mathf.Lerp(0.38f, 1f, currentStylizedDaylight);
        float time = Time.time;

        for (int i = ambientAirParticles.Count - 1; i >= 0; i--)
        {
            AmbientAirParticleData particle = ambientAirParticles[i];
            if (particle.RootTransform == null)
            {
                ambientAirParticles.RemoveAt(i);
                continue;
            }

            particle.TravelOffset += particle.TravelSpeed * dt;
            if (particle.TravelOffset > particle.HalfTravelRange)
            {
                ResetAmbientAirParticle(particle, randomizeAlongPath: false);
            }

            Vector3 worldPosition = particle.Center + windDirection * particle.TravelOffset + windRight * particle.LateralOffset;
            float bob = Mathf.Sin(time * (particle.IsForestLocal ? 2.1f : 1.4f) * particle.BobSpeed + particle.PhaseOffset) * particle.BobAmplitude;
            float shimmer = Mathf.Sin(time * (particle.IsForestLocal ? 1.7f : 1.15f) + particle.PhaseOffset * 1.3f) * 0.01f;
            worldPosition.y = SampleTerrainHeight(worldPosition.x, worldPosition.z) + particle.BaseHeightOffset + bob + shimmer;
            particle.RootTransform.position = worldPosition;

            if (particle.Material != null)
            {
                float twinkle = 0.88f + Mathf.Sin(time * (particle.IsForestLocal ? 1.6f : particle.IsHighwayDust ? 1.9f : 1.05f) + particle.PhaseOffset * 1.7f) * 0.12f;
                float busDustBoost = particle.IsHighwayDust ? GetEdgeHighwayDustBoostAtPosition(worldPosition) : 0f;
                float intensity = daylightStrength * twinkle * (1f + busDustBoost * 0.45f);
                particle.Material.color = particle.BaseColor * intensity;
            }
        }
    }

    private float GetEdgeHighwayDustBoostAtPosition(Vector3 worldPosition)
    {
        float bestBoost = 0f;
        for (int i = 0; i < edgeHighwayBuses.Count; i++)
        {
            EdgeHighwayBusData bus = edgeHighwayBuses[i];
            if (bus?.RootTransform == null)
            {
                continue;
            }

            Vector3 delta = worldPosition - bus.RootTransform.position;
            delta.y = 0f;
            float boost = 1f - Mathf.Clamp01(delta.magnitude / 1.8f);
            if (boost > bestBoost)
            {
                bestBoost = boost;
            }
        }

        if (hiringDriverArrival?.BusRootTransform != null)
        {
            Vector3 delta = worldPosition - hiringDriverArrival.BusRootTransform.position;
            delta.y = 0f;
            float boost = 1f - Mathf.Clamp01(delta.magnitude / 1.8f);
            if (boost > bestBoost)
            {
                bestBoost = boost;
            }
        }

        return bestBoost;
    }

    private void SetupMiscBirds()
    {
        miscBirds.Clear();
        if (miscBirdRoot != null)
        {
            Destroy(miscBirdRoot.gameObject);
        }

        if (worldRoot == null || miscTreePerchPoints.Count < 2)
        {
            return;
        }

        miscBirdRoot = new GameObject("MiscBirds").transform;
        miscBirdRoot.SetParent(worldRoot, false);

        int birdCount = Mathf.Min(MiscBirdCount, miscTreePerchPoints.Count);
        for (int i = 0; i < birdCount; i++)
        {
            CreateMiscBird(i);
        }
    }

    private void CreateMiscBird(int birdIndex)
    {
        if (miscBirdRoot == null || miscTreePerchPoints.Count == 0)
        {
            return;
        }

        GameObject birdRoot = new($"MiscBird_{birdIndex + 1}");
        birdRoot.transform.SetParent(miscBirdRoot, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.transform.SetParent(birdRoot.transform, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(0.12f, 0.09f, 0.18f);
        ApplyColor(body, new Color(0.22f, 0.2f, 0.18f));
        ConfigureStaticVisual(body);
        if (body.TryGetComponent(out Collider bodyCollider))
        {
            bodyCollider.enabled = false;
        }

        GameObject leftWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWing.transform.SetParent(birdRoot.transform, false);
        leftWing.transform.localPosition = new Vector3(-0.06f, 0.01f, 0f);
        leftWing.transform.localScale = new Vector3(0.12f, 0.02f, 0.18f);
        ApplyColor(leftWing, new Color(0.28f, 0.26f, 0.24f));
        ConfigureStaticVisual(leftWing);
        if (leftWing.TryGetComponent(out Collider leftCollider))
        {
            leftCollider.enabled = false;
        }

        GameObject rightWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWing.transform.SetParent(birdRoot.transform, false);
        rightWing.transform.localPosition = new Vector3(0.06f, 0.01f, 0f);
        rightWing.transform.localScale = new Vector3(0.12f, 0.02f, 0.18f);
        ApplyColor(rightWing, new Color(0.28f, 0.26f, 0.24f));
        ConfigureStaticVisual(rightWing);
        if (rightWing.TryGetComponent(out Collider rightCollider))
        {
            rightCollider.enabled = false;
        }

        GameObject beak = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beak.transform.SetParent(birdRoot.transform, false);
        beak.transform.localPosition = new Vector3(0f, 0f, 0.11f);
        beak.transform.localScale = new Vector3(0.03f, 0.02f, 0.05f);
        ApplyColor(beak, new Color(0.92f, 0.74f, 0.2f));
        ConfigureStaticVisual(beak);
        if (beak.TryGetComponent(out Collider beakCollider))
        {
            beakCollider.enabled = false;
        }

        int perchIndex = Mathf.Abs(birdIndex * 3) % miscTreePerchPoints.Count;
        Vector3 perchPosition = miscTreePerchPoints[perchIndex];
        birdRoot.transform.position = perchPosition;
        float perchYaw = Random.Range(0f, 360f);
        birdRoot.transform.rotation = Quaternion.Euler(0f, perchYaw, 0f);

        miscBirds.Add(new MiscBirdData
        {
            RootTransform = birdRoot.transform,
            BodyTransform = body.transform,
            LeftWingTransform = leftWing.transform,
            RightWingTransform = rightWing.transform,
            StartPosition = perchPosition,
            TargetPosition = perchPosition,
            CurrentPerchIndex = perchIndex,
            TargetPerchIndex = perchIndex,
            StateTimer = Random.Range(3.8f, 8.2f),
            FlightDuration = 0f,
            FlightProgress = 0f,
            BobPhase = Random.Range(0f, 10f),
            WingPhase = Random.Range(0f, 10f),
            PerchYaw = perchYaw,
            State = MiscBirdState.Perched
        });
    }

    private void UpdateMiscBirds()
    {
        if (miscBirds.Count == 0 || miscTreePerchPoints.Count < 2)
        {
            return;
        }

        bool birdsShouldFly = AreMiscBirdsInActiveFlightWindow();
        float dt = Time.deltaTime * gameSpeedMultiplier;
        float time = Time.time;
        for (int i = miscBirds.Count - 1; i >= 0; i--)
        {
            MiscBirdData bird = miscBirds[i];
            if (bird.RootTransform == null)
            {
                miscBirds.RemoveAt(i);
                continue;
            }

            switch (bird.State)
            {
                case MiscBirdState.Perched:
                    bird.StateTimer -= dt;
                    {
                        float perchedBob = Mathf.Sin(time * 2.1f + bird.BobPhase) * 0.012f;
                        bird.RootTransform.position = bird.StartPosition + new Vector3(0f, perchedBob, 0f);
                        bird.RootTransform.rotation = Quaternion.Slerp(
                            bird.RootTransform.rotation,
                            Quaternion.Euler(0f, bird.PerchYaw, 0f),
                            6f * Time.deltaTime);
                        float wingFold = 8f + Mathf.Sin(time * 1.8f + bird.WingPhase) * 3f;
                        ApplyMiscBirdWingPose(bird, wingFold);
                    }

                    if (bird.StateTimer <= 0f)
                    {
                        if (!birdsShouldFly)
                        {
                            bird.StateTimer = Random.Range(3.8f, 8.2f);
                            break;
                        }

                        int nextPerchIndex = FindNextMiscBirdPerchIndex(bird.CurrentPerchIndex);
                        if (nextPerchIndex >= 0 && nextPerchIndex != bird.CurrentPerchIndex)
                        {
                            bird.TargetPerchIndex = nextPerchIndex;
                            bird.TargetPosition = miscTreePerchPoints[nextPerchIndex];
                            bird.FlightProgress = 0f;
                            float travelDistance = Vector3.Distance(bird.StartPosition, bird.TargetPosition);
                            bird.FlightDuration = Mathf.Clamp(travelDistance / 2.9f, 0.8f, 2.1f);
                            bird.State = MiscBirdState.Flying;
                        }
                        else
                        {
                            bird.StateTimer = Random.Range(3.8f, 8.2f);
                        }
                    }
                    break;

                case MiscBirdState.Flying:
                    bird.FlightProgress += dt / Mathf.Max(0.001f, bird.FlightDuration);
                    float flightT = Mathf.Clamp01(bird.FlightProgress);
                    Vector3 flightPosition = Vector3.Lerp(bird.StartPosition, bird.TargetPosition, flightT);
                    flightPosition.y += Mathf.Sin(flightT * Mathf.PI) * 0.75f + Mathf.Sin(time * 8.5f + bird.BobPhase) * 0.03f;
                    bird.RootTransform.position = flightPosition;

                    Vector3 toTarget = bird.TargetPosition - flightPosition;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.0001f)
                    {
                        bird.RootTransform.rotation = Quaternion.Slerp(
                            bird.RootTransform.rotation,
                            Quaternion.LookRotation(toTarget.normalized, Vector3.up),
                            10f * Time.deltaTime);
                    }

                    float wingFlap = 42f + Mathf.Sin(time * 18f + bird.WingPhase) * 24f;
                    ApplyMiscBirdWingPose(bird, wingFlap);

                    if (flightT >= 1f)
                    {
                        bird.CurrentPerchIndex = bird.TargetPerchIndex;
                        bird.StartPosition = bird.TargetPosition;
                        bird.PerchYaw = bird.RootTransform.eulerAngles.y;
                        bird.State = MiscBirdState.Perched;
                        bird.StateTimer = Random.Range(4.4f, 9.6f);
                    }
                    break;
            }
        }
    }

    private bool AreMiscBirdsInActiveFlightWindow()
    {
        int hour = GetCurrentHour();
        return hour >= 6 && hour < 18;
    }

    private static void ApplyMiscBirdWingPose(MiscBirdData bird, float wingAngle)
    {
        if (bird == null)
        {
            return;
        }

        if (bird.LeftWingTransform != null)
        {
            bird.LeftWingTransform.localRotation = Quaternion.Euler(0f, 0f, wingAngle);
        }

        if (bird.RightWingTransform != null)
        {
            bird.RightWingTransform.localRotation = Quaternion.Euler(0f, 0f, -wingAngle);
        }
    }

    private int FindNextMiscBirdPerchIndex(int currentPerchIndex)
    {
        if (miscTreePerchPoints.Count < 2 || currentPerchIndex < 0 || currentPerchIndex >= miscTreePerchPoints.Count)
        {
            return -1;
        }

        List<int> candidateIndices = new();
        Vector3 current = miscTreePerchPoints[currentPerchIndex];
        for (int i = 0; i < miscTreePerchPoints.Count; i++)
        {
            if (i == currentPerchIndex)
            {
                continue;
            }

            float distance = Vector3.Distance(current, miscTreePerchPoints[i]);
            if (distance < 0.75f)
            {
                continue;
            }

            candidateIndices.Add(i);
        }

        if (candidateIndices.Count == 0)
        {
            return (currentPerchIndex + 1) % miscTreePerchPoints.Count;
        }

        if (candidateIndices.Count == 1)
        {
            return candidateIndices[0];
        }

        int preferredJumpCount = Mathf.Clamp(Mathf.CeilToInt(candidateIndices.Count * 0.45f), 1, candidateIndices.Count);
        int chosenPoolIndex = Random.Range(0, preferredJumpCount);
        candidateIndices.Sort((a, b) =>
        {
            float distanceA = Vector3.Distance(current, miscTreePerchPoints[a]);
            float distanceB = Vector3.Distance(current, miscTreePerchPoints[b]);
            return distanceB.CompareTo(distanceA);
        });

        return candidateIndices[chosenPoolIndex];
    }

    private void SetupAmbientCats()
    {
        ambientCats.Clear();
        ambientCatRoamPoints.Clear();
        if (ambientCatRoot != null)
        {
            Destroy(ambientCatRoot.gameObject);
        }

        if (worldRoot == null)
        {
            return;
        }

        RegisterAmbientCatRoamPoints();
        if (ambientCatRoamPoints.Count == 0)
        {
            return;
        }

        ambientCatRoot = new GameObject("AmbientCats").transform;
        ambientCatRoot.SetParent(worldRoot, false);

        int catCount = Mathf.Min(AmbientCatCount, ambientCatRoamPoints.Count);
        for (int i = 0; i < catCount; i++)
        {
            CreateAmbientCat(i);
        }
    }

    private void RegisterAmbientCatRoamPoints()
    {
        if (locations.TryGetValue(LocationType.Motel, out _))
        {
            RegisterAmbientCatPointsNearLocation(
                LocationType.Motel,
                new[]
                {
                    new Vector2(-2.1f, -1.7f),
                    new Vector2(-1.2f, -2.25f),
                    new Vector2(0.3f, -2.2f),
                    new Vector2(1.8f, -1.95f),
                    new Vector2(2.35f, -0.55f),
                    new Vector2(-2.35f, 0.7f)
                });
        }

        if (locations.TryGetValue(LocationType.BusStop, out _))
        {
            RegisterAmbientCatPointsNearLocation(
                LocationType.BusStop,
                new[]
                {
                    new Vector2(-2.2f, 1.1f),
                    new Vector2(-1.3f, 1.75f),
                    new Vector2(0.15f, 1.95f),
                    new Vector2(1.45f, 1.5f),
                    new Vector2(2.2f, 0.95f)
                });
        }
    }

    private void RegisterAmbientCatPointsNearLocation(LocationType type, IReadOnlyList<Vector2> offsets)
    {
        Vector3 center = GetLocationCenter(type);
        for (int i = 0; i < offsets.Count; i++)
        {
            Vector3 point = new(center.x + offsets[i].x, 0f, center.z + offsets[i].y);
            Vector2Int cell = WorldToCell(point);
            if (!IsInsideGrid(cell) || roadCells.Contains(cell) || edgeHighwayCells.Contains(cell) || IsLocationCell(cell))
            {
                continue;
            }

            point.y = SampleTerrainHeight(point.x, point.z);
            ambientCatRoamPoints.Add(point);
        }
    }

    private void CreateAmbientCat(int catIndex)
    {
        if (ambientCatRoot == null || ambientCatRoamPoints.Count == 0)
        {
            return;
        }

        GameObject catRoot = new($"AmbientCat_{catIndex + 1}");
        catRoot.transform.SetParent(ambientCatRoot, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(catRoot.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        body.transform.localScale = new Vector3(0.16f, 0.12f, 0.26f);
        ApplyColor(body, Color.Lerp(new Color(0.24f, 0.22f, 0.2f), new Color(0.82f, 0.54f, 0.18f), (catIndex % 3) * 0.35f));
        ConfigureStaticVisual(body);
        if (body.TryGetComponent(out Collider bodyCollider))
        {
            bodyCollider.enabled = false;
        }

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(catRoot.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.18f, 0.16f);
        head.transform.localScale = new Vector3(0.14f, 0.12f, 0.13f);
        ApplyColor(head, body.GetComponent<Renderer>().material.color * 1.02f);
        ConfigureStaticVisual(head);
        if (head.TryGetComponent(out Collider headCollider))
        {
            headCollider.enabled = false;
        }

        GameObject leftEar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftEar.transform.SetParent(head.transform, false);
        leftEar.transform.localPosition = new Vector3(-0.04f, 0.07f, 0f);
        leftEar.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
        leftEar.transform.localScale = new Vector3(0.035f, 0.06f, 0.03f);
        ApplyColor(leftEar, head.GetComponent<Renderer>().material.color * 0.96f);
        ConfigureStaticVisual(leftEar);
        if (leftEar.TryGetComponent(out Collider leftEarCollider))
        {
            leftEarCollider.enabled = false;
        }

        GameObject rightEar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightEar.transform.SetParent(head.transform, false);
        rightEar.transform.localPosition = new Vector3(0.04f, 0.07f, 0f);
        rightEar.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        rightEar.transform.localScale = new Vector3(0.035f, 0.06f, 0.03f);
        ApplyColor(rightEar, head.GetComponent<Renderer>().material.color * 0.96f);
        ConfigureStaticVisual(rightEar);
        if (rightEar.TryGetComponent(out Collider rightEarCollider))
        {
            rightEarCollider.enabled = false;
        }

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tail.transform.SetParent(catRoot.transform, false);
        tail.transform.localPosition = new Vector3(0f, 0.16f, -0.14f);
        tail.transform.localRotation = Quaternion.Euler(68f, 0f, 0f);
        tail.transform.localScale = new Vector3(0.028f, 0.16f, 0.028f);
        ApplyColor(tail, body.GetComponent<Renderer>().material.color * 0.92f);
        ConfigureStaticVisual(tail);
        if (tail.TryGetComponent(out Collider tailCollider))
        {
            tailCollider.enabled = false;
        }

        int pointIndex = Mathf.Abs(catIndex * 2) % ambientCatRoamPoints.Count;
        Vector3 position = ambientCatRoamPoints[pointIndex];
        float yaw = Random.Range(0f, 360f);
        catRoot.transform.position = position;
        catRoot.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        ambientCats.Add(new AmbientCatData
        {
            RootTransform = catRoot.transform,
            BodyTransform = body.transform,
            HeadTransform = head.transform,
            TailTransform = tail.transform,
            CurrentPosition = position,
            StartPosition = position,
            TargetPosition = position,
            CurrentPointIndex = pointIndex,
            TargetPointIndex = pointIndex,
            StateTimer = Random.Range(5.2f, 11.5f),
            MoveDuration = 0f,
            MoveProgress = 0f,
            AnimationPhase = Random.Range(0f, 10f),
            TailPhase = Random.Range(0f, 10f),
            Yaw = yaw,
            State = AmbientCatState.Lazing
        });
    }

    private void UpdateAmbientCats()
    {
        if (ambientCats.Count == 0 || ambientCatRoamPoints.Count == 0)
        {
            return;
        }

        bool catsShouldSleep = AreAmbientCatsSleepingNight();
        float dt = Time.deltaTime * gameSpeedMultiplier;
        float time = Time.time;
        for (int i = ambientCats.Count - 1; i >= 0; i--)
        {
            AmbientCatData cat = ambientCats[i];
            if (cat.RootTransform == null)
            {
                ambientCats.RemoveAt(i);
                continue;
            }

            switch (cat.State)
            {
                case AmbientCatState.Lazing:
                    cat.StateTimer -= dt;
                    {
                        float bodyBob = catsShouldSleep ? Mathf.Sin(time * 0.7f + cat.AnimationPhase) * 0.005f : Mathf.Sin(time * 1.25f + cat.AnimationPhase) * 0.012f;
                        Vector3 pos = cat.CurrentPosition + new Vector3(0f, bodyBob, 0f);
                        cat.RootTransform.position = pos;
                        cat.RootTransform.rotation = Quaternion.Slerp(
                            cat.RootTransform.rotation,
                            Quaternion.Euler(0f, cat.Yaw, 0f),
                            5f * Time.deltaTime);
                        if (cat.BodyTransform != null)
                        {
                            cat.BodyTransform.localScale = catsShouldSleep
                                ? new Vector3(0.19f, 0.085f, 0.28f)
                                : new Vector3(0.16f, 0.11f + Mathf.Sin(time * 1.7f + cat.AnimationPhase) * 0.01f, 0.26f);
                        }

                        if (cat.HeadTransform != null)
                        {
                            cat.HeadTransform.localRotation = catsShouldSleep
                                ? Quaternion.Euler(-18f, 0f, 0f)
                                : Quaternion.Euler(
                                    Mathf.Sin(time * 1.6f + cat.AnimationPhase) * 4f,
                                    Mathf.Sin(time * 0.9f + cat.AnimationPhase) * 8f,
                                    0f);
                        }

                        if (cat.TailTransform != null)
                        {
                            cat.TailTransform.localRotation = catsShouldSleep
                                ? Quaternion.Euler(34f, -22f, 0f)
                                : Quaternion.Euler(
                                    64f + Mathf.Sin(time * 2.4f + cat.TailPhase) * 8f,
                                    Mathf.Sin(time * 2.1f + cat.TailPhase) * 10f,
                                    0f);
                        }
                    }

                    if (cat.StateTimer <= 0f)
                    {
                        if (catsShouldSleep)
                        {
                            cat.StateTimer = Random.Range(7.5f, 14.5f);
                            break;
                        }

                        int nextPointIndex = FindNextAmbientCatRoamPoint(cat);
                        if (nextPointIndex >= 0 && nextPointIndex != cat.CurrentPointIndex)
                        {
                            cat.TargetPointIndex = nextPointIndex;
                            cat.StartPosition = cat.CurrentPosition;
                            cat.TargetPosition = ambientCatRoamPoints[nextPointIndex];
                            cat.MoveProgress = 0f;
                            cat.MoveDuration = Mathf.Clamp(Vector3.Distance(cat.StartPosition, cat.TargetPosition) / 0.9f, 1f, 3.4f);
                            cat.State = AmbientCatState.Walking;
                        }
                        else
                        {
                            cat.StateTimer = Random.Range(5.2f, 11.5f);
                        }
                    }
                    break;

                case AmbientCatState.Walking:
                    if (catsShouldSleep)
                    {
                        cat.CurrentPosition = cat.RootTransform.position;
                        cat.StartPosition = cat.CurrentPosition;
                        cat.TargetPosition = cat.CurrentPosition;
                        cat.Yaw = cat.RootTransform.eulerAngles.y;
                        cat.State = AmbientCatState.Lazing;
                        cat.StateTimer = Random.Range(8.5f, 16f);
                        break;
                    }

                    cat.MoveProgress += dt / Mathf.Max(0.001f, cat.MoveDuration);
                    float walkT = Mathf.Clamp01(cat.MoveProgress);
                    Vector3 walkPosition = Vector3.Lerp(cat.StartPosition, cat.TargetPosition, walkT);
                    walkPosition.y += Mathf.Abs(Mathf.Sin(time * 9f + cat.AnimationPhase)) * 0.03f;
                    if (IsAmbientCatPositionCrowded(cat, walkPosition, 0.3f))
                    {
                        cat.CurrentPosition = cat.RootTransform.position;
                        cat.StartPosition = cat.CurrentPosition;
                        cat.TargetPosition = cat.CurrentPosition;
                        cat.Yaw = cat.RootTransform.eulerAngles.y;
                        cat.State = AmbientCatState.Lazing;
                        cat.StateTimer = Random.Range(2.8f, 5f);
                        break;
                    }

                    cat.RootTransform.position = walkPosition;

                    Vector3 toTarget = cat.TargetPosition - walkPosition;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.0001f)
                    {
                        cat.RootTransform.rotation = Quaternion.Slerp(
                            cat.RootTransform.rotation,
                            Quaternion.LookRotation(toTarget.normalized, Vector3.up),
                            9f * Time.deltaTime);
                    }

                    if (cat.BodyTransform != null)
                    {
                        cat.BodyTransform.localScale = new Vector3(0.15f, 0.12f, 0.25f);
                    }

                    if (cat.HeadTransform != null)
                    {
                        cat.HeadTransform.localRotation = Quaternion.Euler(Mathf.Sin(time * 10f + cat.AnimationPhase) * 6f, 0f, 0f);
                    }

                    if (cat.TailTransform != null)
                    {
                        cat.TailTransform.localRotation = Quaternion.Euler(
                            72f + Mathf.Sin(time * 8.5f + cat.TailPhase) * 12f,
                            Mathf.Sin(time * 6.2f + cat.TailPhase) * 14f,
                            0f);
                    }

                    if (walkT >= 1f)
                    {
                        cat.CurrentPointIndex = cat.TargetPointIndex;
                        cat.CurrentPosition = cat.TargetPosition;
                        cat.Yaw = cat.RootTransform.eulerAngles.y;
                        cat.State = AmbientCatState.Lazing;
                        cat.StateTimer = Random.Range(6.4f, 13.5f);
                    }
                    break;
            }
        }
    }

    private bool AreAmbientCatsSleepingNight()
    {
        int hour = GetCurrentHour();
        return hour >= 22 || hour < 6;
    }

    private bool AreAmbientBeesActive()
    {
        int hour = GetCurrentHour();
        return hour >= 6 && hour < 18;
    }

    private bool AreAmbientLanternMothsActive()
    {
        int hour = GetCurrentHour();
        return hour >= 20 || hour < 6;
    }

    private bool IsAmbientCatPositionCrowded(AmbientCatData currentCat, Vector3 position, float minDistance)
    {
        for (int i = 0; i < ambientCats.Count; i++)
        {
            AmbientCatData otherCat = ambientCats[i];
            if (otherCat == null || otherCat == currentCat || otherCat.RootTransform == null)
            {
                continue;
            }

            Vector3 otherPosition = otherCat.RootTransform.position;
            otherPosition.y = position.y;
            if (Vector3.Distance(position, otherPosition) < minDistance)
            {
                return true;
            }
        }

        return false;
    }

    private int FindNextAmbientCatRoamPoint(AmbientCatData cat)
    {
        int currentPointIndex = cat?.CurrentPointIndex ?? -1;
        if (ambientCatRoamPoints.Count < 2 || currentPointIndex < 0 || currentPointIndex >= ambientCatRoamPoints.Count)
        {
            return -1;
        }

        List<int> candidates = new();
        Vector3 current = ambientCatRoamPoints[currentPointIndex];
        for (int i = 0; i < ambientCatRoamPoints.Count; i++)
        {
            if (i == currentPointIndex)
            {
                continue;
            }

            float distance = Vector3.Distance(current, ambientCatRoamPoints[i]);
            if (distance < 0.8f || distance > 4.2f)
            {
                continue;
            }

            if (IsAmbientCatPositionCrowded(cat, ambientCatRoamPoints[i], 0.42f))
            {
                continue;
            }

            candidates.Add(i);
        }

        if (candidates.Count == 0)
        {
            return (currentPointIndex + 1) % ambientCatRoamPoints.Count;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void SetupAmbientBees()
    {
        ambientBees.Clear();
        if (ambientBeeRoot != null)
        {
            Destroy(ambientBeeRoot.gameObject);
        }

        if (worldRoot == null || flowerBeePoints.Count == 0)
        {
            return;
        }

        ambientBeeRoot = new GameObject("AmbientBees").transform;
        ambientBeeRoot.SetParent(worldRoot, false);

        int beeCount = Mathf.Min(AmbientBeeCount, flowerBeePoints.Count * 2);
        for (int i = 0; i < beeCount; i++)
        {
            CreateAmbientBee(i);
        }
    }

    private void CreateAmbientBee(int beeIndex)
    {
        if (ambientBeeRoot == null || flowerBeePoints.Count == 0)
        {
            return;
        }

        GameObject beeRoot = new($"AmbientBee_{beeIndex + 1}");
        beeRoot.transform.SetParent(ambientBeeRoot, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(beeRoot.transform, false);
        body.transform.localPosition = Vector3.zero;
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.035f, 0.055f, 0.035f);
        ApplyColor(body, new Color(0.96f, 0.78f, 0.12f));
        ConfigureStaticVisual(body);
        if (body.TryGetComponent(out Collider bodyCollider))
        {
            bodyCollider.enabled = false;
        }

        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.transform.SetParent(beeRoot.transform, false);
        stripe.transform.localPosition = new Vector3(0f, 0f, -0.005f);
        stripe.transform.localScale = new Vector3(0.04f, 0.04f, 0.018f);
        ApplyColor(stripe, new Color(0.16f, 0.14f, 0.12f));
        ConfigureStaticVisual(stripe);
        if (stripe.TryGetComponent(out Collider stripeCollider))
        {
            stripeCollider.enabled = false;
        }

        GameObject leftWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWing.transform.SetParent(beeRoot.transform, false);
        leftWing.transform.localPosition = new Vector3(-0.025f, 0.02f, 0f);
        leftWing.transform.localScale = new Vector3(0.055f, 0.01f, 0.035f);
        ApplyColor(leftWing, new Color(0.92f, 0.96f, 1f));
        ConfigureStaticVisual(leftWing);
        if (leftWing.TryGetComponent(out Collider leftWingCollider))
        {
            leftWingCollider.enabled = false;
        }

        GameObject rightWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWing.transform.SetParent(beeRoot.transform, false);
        rightWing.transform.localPosition = new Vector3(0.025f, 0.02f, 0f);
        rightWing.transform.localScale = new Vector3(0.055f, 0.01f, 0.035f);
        ApplyColor(rightWing, new Color(0.92f, 0.96f, 1f));
        ConfigureStaticVisual(rightWing);
        if (rightWing.TryGetComponent(out Collider rightWingCollider))
        {
            rightWingCollider.enabled = false;
        }

        int flowerIndex = beeIndex % flowerBeePoints.Count;
        Vector3 flowerPoint = flowerBeePoints[flowerIndex];
        float orbitAngle = Random.Range(0f, Mathf.PI * 2f);
        beeRoot.transform.position = flowerPoint + new Vector3(Mathf.Cos(orbitAngle), 0f, Mathf.Sin(orbitAngle)) * 0.12f + new Vector3(0f, 0.24f, 0f);

        Renderer bodyRenderer = body.GetComponent<Renderer>();
        Renderer stripeRenderer = stripe.GetComponent<Renderer>();
        Renderer leftWingRenderer = leftWing.GetComponent<Renderer>();
        Renderer rightWingRenderer = rightWing.GetComponent<Renderer>();
        ambientBees.Add(new AmbientBeeData
        {
            RootTransform = beeRoot.transform,
            BodyRenderer = bodyRenderer,
            StripeRenderer = stripeRenderer,
            LeftWingRenderer = leftWingRenderer,
            RightWingRenderer = rightWingRenderer,
            BodyMaterial = bodyRenderer != null ? bodyRenderer.material : null,
            StripeMaterial = stripeRenderer != null ? stripeRenderer.material : null,
            LeftWingMaterial = leftWingRenderer != null ? leftWingRenderer.material : null,
            RightWingMaterial = rightWingRenderer != null ? rightWingRenderer.material : null,
            LeftWingTransform = leftWing.transform,
            RightWingTransform = rightWing.transform,
            FlowerPointIndex = flowerIndex,
            OrbitRadius = Random.Range(0.08f, 0.16f),
            OrbitHeight = Random.Range(0.18f, 0.28f),
            OrbitSpeed = Random.Range(1.6f, 2.6f),
            OrbitAngle = orbitAngle,
            VerticalBobAmplitude = Random.Range(0.015f, 0.04f),
            VerticalBobSpeed = Random.Range(2.2f, 3.6f),
            PhaseOffset = Random.Range(0f, 10f),
            Visibility = AreAmbientBeesActive() ? 1f : 0f
        });
    }

    private void UpdateAmbientBees()
    {
        if (ambientBees.Count == 0 || flowerBeePoints.Count == 0)
        {
            return;
        }

        bool beesActive = AreAmbientBeesActive();
        float dt = Time.deltaTime * gameSpeedMultiplier;
        float time = Time.time;
        for (int i = ambientBees.Count - 1; i >= 0; i--)
        {
            AmbientBeeData bee = ambientBees[i];
            if (bee.RootTransform == null)
            {
                ambientBees.RemoveAt(i);
                continue;
            }

            int flowerIndex = Mathf.Clamp(bee.FlowerPointIndex, 0, flowerBeePoints.Count - 1);
            Vector3 flowerPoint = flowerBeePoints[flowerIndex];
            float targetVisibility = beesActive ? 1f : 0f;
            bee.Visibility = Mathf.MoveTowards(bee.Visibility, targetVisibility, dt * 0.85f);

            ApplyAmbientBeeVisibility(bee);
            if (bee.Visibility <= 0.001f && !beesActive)
            {
                continue;
            }

            if (beesActive)
            {
                bee.OrbitAngle += dt * bee.OrbitSpeed;
            }

            Vector3 offset = new Vector3(Mathf.Cos(bee.OrbitAngle), 0f, Mathf.Sin(bee.OrbitAngle)) * bee.OrbitRadius;
            float verticalBob = beesActive
                ? Mathf.Sin(time * bee.VerticalBobSpeed + bee.PhaseOffset) * bee.VerticalBobAmplitude
                : Mathf.Sin(time * 0.8f + bee.PhaseOffset) * 0.004f;
            Vector3 position = flowerPoint + offset + new Vector3(0f, bee.OrbitHeight + verticalBob, 0f);
            bee.RootTransform.position = position;

            Vector3 tangent = new Vector3(-Mathf.Sin(bee.OrbitAngle), 0f, Mathf.Cos(bee.OrbitAngle));
            if (tangent.sqrMagnitude > 0.0001f)
            {
                bee.RootTransform.rotation = Quaternion.Slerp(
                    bee.RootTransform.rotation,
                    Quaternion.LookRotation(tangent.normalized, Vector3.up),
                    10f * Time.deltaTime);
            }

            float wingAngle = beesActive
                ? 48f + Mathf.Sin(time * 34f + bee.PhaseOffset) * 32f
                : 12f + Mathf.Sin(time * 4.5f + bee.PhaseOffset) * 5f;
            if (bee.LeftWingTransform != null)
            {
                bee.LeftWingTransform.localRotation = Quaternion.Euler(0f, 0f, wingAngle);
            }

            if (bee.RightWingTransform != null)
            {
                bee.RightWingTransform.localRotation = Quaternion.Euler(0f, 0f, -wingAngle);
            }
        }
    }

    private static void ApplyAmbientBeeVisibility(AmbientBeeData bee)
    {
        if (bee?.RootTransform == null)
        {
            return;
        }

        bool visible = bee.Visibility > 0.001f;
        if (bee.RootTransform.gameObject.activeSelf != visible)
        {
            bee.RootTransform.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            return;
        }

        Color bodyColor = new Color(0.96f, 0.78f, 0.12f) * Mathf.Lerp(0.15f, 1f, bee.Visibility);
        Color stripeColor = new Color(0.16f, 0.14f, 0.12f) * Mathf.Lerp(0.2f, 1f, bee.Visibility);
        Color wingColor = Color.Lerp(new Color(0.92f, 0.96f, 1f) * 0.2f, new Color(0.92f, 0.96f, 1f), bee.Visibility);

        if (bee.BodyMaterial != null)
        {
            bee.BodyMaterial.color = bodyColor;
        }

        if (bee.StripeMaterial != null)
        {
            bee.StripeMaterial.color = stripeColor;
        }

        if (bee.LeftWingMaterial != null)
        {
            bee.LeftWingMaterial.color = wingColor;
        }

        if (bee.RightWingMaterial != null)
        {
            bee.RightWingMaterial.color = wingColor;
        }
    }

    private void SetupAmbientLanternMoths()
    {
        ambientLanternMothSwarms.Clear();
        if (ambientLanternMothRoot != null)
        {
            Destroy(ambientLanternMothRoot.gameObject);
        }

        if (worldRoot == null)
        {
            return;
        }

        ambientLanternMothRoot = new GameObject("AmbientLanternMoths").transform;
        ambientLanternMothRoot.SetParent(worldRoot, false);

        int swarmCount = Mathf.Clamp(Mathf.CeilToInt(roadLanterns.Count * 0.18f), 1, AmbientLanternMothSwarmMaxCount);
        if (roadLanterns.Count == 0)
        {
            swarmCount = 0;
        }

        for (int i = 0; i < swarmCount; i++)
        {
            CreateAmbientLanternMothSwarm(i);
        }

        ReselectAmbientLanternMothLanterns();
        wereAmbientLanternMothsActiveLastFrame = AreAmbientLanternMothsActive();
    }

    private void RefreshAmbientLanternMoths()
    {
        SetupAmbientLanternMoths();
    }

    private void CreateAmbientLanternMothSwarm(int swarmIndex)
    {
        if (ambientLanternMothRoot == null)
        {
            return;
        }

        GameObject swarmRoot = new($"LanternMothSwarm_{swarmIndex + 1}");
        swarmRoot.transform.SetParent(ambientLanternMothRoot, false);

        AmbientLanternMothSwarmData swarm = new()
        {
            RootTransform = swarmRoot.transform,
            OrbitRadius = Random.Range(0.16f, 0.26f),
            OrbitHeight = Random.Range(0.06f, 0.16f),
            OrbitSpeed = Random.Range(0.8f, 1.45f),
            VerticalBobAmplitude = Random.Range(0.02f, 0.05f),
            VerticalBobSpeed = Random.Range(1.8f, 3.2f),
            PhaseOffset = Random.Range(0f, 10f),
            Visibility = 0f
        };

        int particleCount = Random.Range(5, 9);
        for (int i = 0; i < particleCount; i++)
        {
            GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            particle.name = $"MothDot_{i + 1}";
            particle.transform.SetParent(swarmRoot.transform, false);
            particle.transform.localScale = Vector3.one * Random.Range(0.026f, 0.04f);
            ApplyColor(particle, new Color(0.9f, 0.87f, 0.65f));
            ConfigureStaticVisual(particle);
            if (particle.TryGetComponent(out Collider particleCollider))
            {
                particleCollider.enabled = false;
            }

            Renderer particleRenderer = particle.GetComponent<Renderer>();
            swarm.ParticleTransforms.Add(particle.transform);
            swarm.ParticleRenderers.Add(particleRenderer);
            swarm.ParticleMaterials.Add(particleRenderer != null ? particleRenderer.material : null);
        }

        ambientLanternMothSwarms.Add(swarm);
        ApplyAmbientLanternMothVisibility(swarm);
    }

    private void ReselectAmbientLanternMothLanterns()
    {
        if (ambientLanternMothSwarms.Count == 0)
        {
            return;
        }

        List<int> availableLanternIndices = new();
        for (int i = 0; i < roadLanterns.Count; i++)
        {
            if (roadLanterns[i]?.Light != null)
            {
                availableLanternIndices.Add(i);
            }
        }

        for (int i = 0; i < ambientLanternMothSwarms.Count; i++)
        {
            AmbientLanternMothSwarmData swarm = ambientLanternMothSwarms[i];
            swarm.LanternIndex = -1;
            if (availableLanternIndices.Count == 0)
            {
                continue;
            }

            int pick = Random.Range(0, availableLanternIndices.Count);
            swarm.LanternIndex = availableLanternIndices[pick];
            availableLanternIndices.RemoveAt(pick);
            swarm.OrbitRadius = Random.Range(0.16f, 0.26f);
            swarm.OrbitHeight = Random.Range(0.06f, 0.16f);
            swarm.OrbitSpeed = Random.Range(0.8f, 1.45f);
            swarm.VerticalBobAmplitude = Random.Range(0.02f, 0.05f);
            swarm.VerticalBobSpeed = Random.Range(1.8f, 3.2f);
            swarm.PhaseOffset = Random.Range(0f, 10f);
        }
    }

    private void UpdateAmbientLanternMoths()
    {
        if (ambientLanternMothSwarms.Count == 0)
        {
            return;
        }

        bool mothsActive = AreAmbientLanternMothsActive();
        if (mothsActive && !wereAmbientLanternMothsActiveLastFrame)
        {
            ReselectAmbientLanternMothLanterns();
        }
        wereAmbientLanternMothsActiveLastFrame = mothsActive;

        float dt = Time.deltaTime * gameSpeedMultiplier;
        float time = Time.time;
        for (int i = ambientLanternMothSwarms.Count - 1; i >= 0; i--)
        {
            AmbientLanternMothSwarmData swarm = ambientLanternMothSwarms[i];
            if (swarm?.RootTransform == null)
            {
                ambientLanternMothSwarms.RemoveAt(i);
                continue;
            }

            bool hasLantern = swarm.LanternIndex >= 0 &&
                swarm.LanternIndex < roadLanterns.Count &&
                roadLanterns[swarm.LanternIndex]?.Light != null;

            float targetVisibility = mothsActive && hasLantern ? 1f : 0f;
            swarm.Visibility = Mathf.MoveTowards(swarm.Visibility, targetVisibility, dt * 0.8f);
            ApplyAmbientLanternMothVisibility(swarm);

            if (swarm.Visibility <= 0.001f && !mothsActive)
            {
                continue;
            }

            if (!hasLantern)
            {
                continue;
            }

            Vector3 center = roadLanterns[swarm.LanternIndex].Light.transform.position + new Vector3(0f, 0.05f, 0f);
            for (int p = 0; p < swarm.ParticleTransforms.Count; p++)
            {
                Transform particleTransform = swarm.ParticleTransforms[p];
                if (particleTransform == null)
                {
                    continue;
                }

                float particleT = time * swarm.OrbitSpeed + swarm.PhaseOffset + p * 0.92f;
                float radius = swarm.OrbitRadius * (0.75f + Mathf.Sin(particleT * 1.37f) * 0.18f);
                Vector3 orbit = new Vector3(
                    Mathf.Cos(particleT) * radius,
                    swarm.OrbitHeight + Mathf.Sin(particleT * swarm.VerticalBobSpeed) * swarm.VerticalBobAmplitude,
                    Mathf.Sin(particleT * 1.14f) * radius);
                particleTransform.position = center + orbit;
            }
        }
    }

    private static void ApplyAmbientLanternMothVisibility(AmbientLanternMothSwarmData swarm)
    {
        if (swarm?.RootTransform == null)
        {
            return;
        }

        bool visible = swarm.Visibility > 0.001f;
        if (swarm.RootTransform.gameObject.activeSelf != visible)
        {
            swarm.RootTransform.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            return;
        }

        Color dotColor = Color.Lerp(
            new Color(0.22f, 0.2f, 0.14f),
            new Color(0.94f, 0.9f, 0.72f),
            swarm.Visibility);
        for (int i = 0; i < swarm.ParticleMaterials.Count; i++)
        {
            Material particleMaterial = swarm.ParticleMaterials[i];
            if (particleMaterial != null)
            {
                particleMaterial.color = dotColor;
            }
        }
    }

}
