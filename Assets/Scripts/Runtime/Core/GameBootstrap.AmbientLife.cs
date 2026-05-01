using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private void SetupDistantClouds()
    {
        distantClouds.Clear();
        // High sky layer. Rows start off-map and cover the full enlarged grid as they drift.
        float startX = -48f;
        float[] zRows =
        {
            -30f, -12f, 6f, 24f, 42f,
            60f, 78f, 96f, 114f, 132f, 150f
        };
        for (int i = 0; i < zRows.Length; i++)
        {
            float height = 36f + (i % 4) * 4.5f + (i % 3) * 1.2f;
            float speed = 0.55f + (i % 5) * 0.11f;
            float bobAmplitude = 0.55f + (i % 4) * 0.14f;
            float bobSpeed = 0.24f + (i % 5) * 0.045f;
            float phase = i * 0.73f;
            float scale = 2.15f + (i % 5) * 0.22f;
            float initialOffset = (i * 31f) % CloudTravelLength;
            CreateDistantCloud(
                new Vector3(startX, height, zRows[i]),
                speed,
                bobAmplitude,
                bobSpeed,
                phase,
                scale,
                initialOffset);
        }
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
            CreateMiscBird(i, birdCount);
        }
    }

    private void CreateMiscBird(int birdIndex, int totalCount)
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
        body.transform.localScale = new Vector3(0.22f, 0.17f, 0.34f);
        ApplyColor(body, new Color(0.22f, 0.2f, 0.18f), VisualSmoothnessFabric);
        ConfigureStaticVisual(body, VisualSmoothnessFabric);
        if (body.TryGetComponent(out Collider bodyCollider))
        {
            bodyCollider.enabled = false;
        }

        GameObject leftWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftWing.transform.SetParent(birdRoot.transform, false);
        leftWing.transform.localPosition = new Vector3(-0.12f, 0.01f, 0f);
        leftWing.transform.localScale = new Vector3(0.22f, 0.03f, 0.32f);
        ApplyColor(leftWing, new Color(0.28f, 0.26f, 0.24f), VisualSmoothnessFabric);
        ConfigureStaticVisual(leftWing, VisualSmoothnessFabric);
        if (leftWing.TryGetComponent(out Collider leftCollider))
        {
            leftCollider.enabled = false;
        }

        GameObject rightWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightWing.transform.SetParent(birdRoot.transform, false);
        rightWing.transform.localPosition = new Vector3(0.12f, 0.01f, 0f);
        rightWing.transform.localScale = new Vector3(0.22f, 0.03f, 0.32f);
        ApplyColor(rightWing, new Color(0.28f, 0.26f, 0.24f), VisualSmoothnessFabric);
        ConfigureStaticVisual(rightWing, VisualSmoothnessFabric);
        if (rightWing.TryGetComponent(out Collider rightCollider))
        {
            rightCollider.enabled = false;
        }

        GameObject beak = GameObject.CreatePrimitive(PrimitiveType.Cube);
        beak.transform.SetParent(birdRoot.transform, false);
        beak.transform.localPosition = new Vector3(0f, 0f, 0.20f);
        beak.transform.localScale = new Vector3(0.05f, 0.04f, 0.09f);
        ApplyColor(beak, new Color(0.92f, 0.74f, 0.2f), VisualSmoothnessSkin);
        ConfigureStaticVisual(beak, VisualSmoothnessSkin);
        if (beak.TryGetComponent(out Collider beakCollider))
        {
            beakCollider.enabled = false;
        }

        int step = Mathf.Max(1, miscTreePerchPoints.Count / totalCount);
        int perchIndex = birdIndex * step % miscTreePerchPoints.Count;
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
            StateTimer = Random.Range(5.0f, 10.0f),
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
        if (miscTreePerchPoints.Count < 2)
        {
            return;
        }

        if (miscBirds.Count == 0)
        {
            SetupMiscBirds();
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
                        float wingFold = 10f + Mathf.Sin(time * 2.2f + bird.WingPhase) * 5f;
                        ApplyMiscBirdWingPose(bird, wingFold);
                    }

                    if (bird.StateTimer <= 0f)
                    {
                        if (!birdsShouldFly)
                        {
                            bird.StateTimer = Random.Range(5.0f, 10.0f);
                            break;
                        }

                        int nextPerchIndex = FindNextMiscBirdPerchIndex(bird.CurrentPerchIndex);
                        if (nextPerchIndex >= 0 && nextPerchIndex != bird.CurrentPerchIndex)
                        {
                            bird.TargetPerchIndex = nextPerchIndex;
                            bird.TargetPosition = miscTreePerchPoints[nextPerchIndex];
                            bird.FlightProgress = 0f;
                            float travelDistance = Vector3.Distance(bird.StartPosition, bird.TargetPosition);
                            bird.FlightDuration = Mathf.Clamp(travelDistance / 1.45f, 1.6f, 4.8f);
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
                    flightPosition.y += Mathf.Sin(flightT * Mathf.PI) * 0.75f + Mathf.Sin(time * 5.2f + bird.BobPhase) * 0.03f;
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

                    float wingFlap = 52f + Mathf.Sin(time * 14f + bird.WingPhase) * 32f;
                    ApplyMiscBirdWingPose(bird, wingFlap);

                    if (flightT >= 1f)
                    {
                        bird.CurrentPerchIndex = bird.TargetPerchIndex;
                        bird.StartPosition = bird.TargetPosition;
                        bird.PerchYaw = bird.RootTransform.eulerAngles.y;
                        bird.State = MiscBirdState.Perched;
                        bird.StateTimer = Random.Range(6.0f, 12.0f);
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

    private void MoveAmbientCatsToCurrentHome()
    {
        if (ambientCats.Count == 0)
        {
            SetupAmbientCats();
            return;
        }

        ambientCatRoamPoints.Clear();
        RegisterAmbientCatRoamPoints();
        if (ambientCatRoamPoints.Count == 0)
        {
            return;
        }

        for (int i = 0; i < ambientCats.Count; i++)
        {
            AmbientCatData cat = ambientCats[i];
            if (cat == null || cat.RootTransform == null)
            {
                continue;
            }

            Vector3 currentPosition = cat.RootTransform.position;
            currentPosition.y = SampleTerrainHeight(currentPosition.x, currentPosition.z);
            int targetIndex = FindNearestAmbientCatRoamPointIndex(currentPosition, i);
            Vector3 targetPosition = ambientCatRoamPoints[targetIndex];

            cat.CurrentPosition = currentPosition;
            cat.StartPosition = currentPosition;
            cat.TargetPosition = targetPosition;
            cat.CurrentPointIndex = Mathf.Clamp(targetIndex, 0, ambientCatRoamPoints.Count - 1);
            cat.TargetPointIndex = targetIndex;
            cat.MoveProgress = 0f;
            cat.MoveDuration = Mathf.Clamp(Vector3.Distance(currentPosition, targetPosition) / 0.85f, 2.2f, 12f);
            cat.StateTimer = 0f;
            cat.IsRelocatingHome = true;
            cat.State = AmbientCatState.Walking;
        }

        SessionDebugLogger.Log("AMBIENT", $"Moved {ambientCats.Count} ambient cats toward current home points instead of respawning them.");
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
            return;
        }

        if (locations.TryGetValue(LocationType.IntercityStop, out _))
        {
            RegisterAmbientCatPointsNearLocation(
                LocationType.IntercityStop,
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
        ApplyColor(body, Color.Lerp(new Color(0.24f, 0.22f, 0.2f), new Color(0.82f, 0.54f, 0.18f), (catIndex % 3) * 0.35f), VisualSmoothnessFabric);
        ConfigureStaticVisual(body, VisualSmoothnessFabric);
        if (body.TryGetComponent(out Collider bodyCollider))
        {
            bodyCollider.enabled = false;
        }

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(catRoot.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.18f, 0.16f);
        head.transform.localScale = new Vector3(0.14f, 0.12f, 0.13f);
        ApplyColor(head, body.GetComponent<Renderer>().material.color * 1.02f, VisualSmoothnessFabric);
        ConfigureStaticVisual(head, VisualSmoothnessFabric);
        if (head.TryGetComponent(out Collider headCollider))
        {
            headCollider.enabled = false;
        }

        GameObject leftEar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftEar.transform.SetParent(head.transform, false);
        leftEar.transform.localPosition = new Vector3(-0.04f, 0.07f, 0f);
        leftEar.transform.localRotation = Quaternion.Euler(0f, 0f, 24f);
        leftEar.transform.localScale = new Vector3(0.035f, 0.06f, 0.03f);
        ApplyColor(leftEar, head.GetComponent<Renderer>().material.color * 0.96f, VisualSmoothnessFabric);
        ConfigureStaticVisual(leftEar, VisualSmoothnessFabric);
        if (leftEar.TryGetComponent(out Collider leftEarCollider))
        {
            leftEarCollider.enabled = false;
        }

        GameObject rightEar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightEar.transform.SetParent(head.transform, false);
        rightEar.transform.localPosition = new Vector3(0.04f, 0.07f, 0f);
        rightEar.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
        rightEar.transform.localScale = new Vector3(0.035f, 0.06f, 0.03f);
        ApplyColor(rightEar, head.GetComponent<Renderer>().material.color * 0.96f, VisualSmoothnessFabric);
        ConfigureStaticVisual(rightEar, VisualSmoothnessFabric);
        if (rightEar.TryGetComponent(out Collider rightEarCollider))
        {
            rightEarCollider.enabled = false;
        }

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tail.transform.SetParent(catRoot.transform, false);
        tail.transform.localPosition = new Vector3(0f, 0.16f, -0.14f);
        tail.transform.localRotation = Quaternion.Euler(68f, 0f, 0f);
        tail.transform.localScale = new Vector3(0.028f, 0.16f, 0.028f);
        ApplyColor(tail, body.GetComponent<Renderer>().material.color * 0.92f, VisualSmoothnessFabric);
        ConfigureStaticVisual(tail, VisualSmoothnessFabric);
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
                    if (catsShouldSleep && !cat.IsRelocatingHome)
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
                    if (!cat.IsRelocatingHome && IsAmbientCatPositionCrowded(cat, walkPosition, 0.3f))
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
                        cat.IsRelocatingHome = false;
                        cat.State = AmbientCatState.Lazing;
                        cat.StateTimer = Random.Range(6.4f, 13.5f);
                    }
                    break;

                case AmbientCatState.BeingPetted:
                {
                    cat.PettingTimer -= dt;
                    DriverAgent petter = cat.PettedByDriverId >= 0 ? GetDriverAgentById(cat.PettedByDriverId) : null;
                    bool driverStillPetting = petter != null &&
                        petter.WalkPhase == DriverRescuePhase.IdlePettingCat &&
                        petter.IdleCatPetTargetIndex >= 0 &&
                        petter.IdleCatPetTargetIndex < ambientCats.Count &&
                        ambientCats[petter.IdleCatPetTargetIndex] == cat;
                    if (!driverStillPetting || cat.PettingTimer <= 0f)
                    {
                        cat.State = AmbientCatState.Lazing;
                        cat.StateTimer = Random.Range(5f, 10f);
                        cat.PettedByDriverId = -1;
                        break;
                    }

                    if (petter.DriverObject != null)
                    {
                        Vector3 faceDir = petter.DriverObject.transform.position - cat.RootTransform.position;
                        faceDir.y = 0f;
                        if (faceDir.sqrMagnitude > 0.0001f)
                        {
                            cat.RootTransform.rotation = Quaternion.Slerp(
                                cat.RootTransform.rotation,
                                Quaternion.LookRotation(faceDir.normalized, Vector3.up),
                                6f * Time.deltaTime);
                        }
                    }

                    cat.RootTransform.position = cat.CurrentPosition;
                    if (cat.BodyTransform != null)
                        cat.BodyTransform.localScale = new Vector3(0.16f, 0.13f, 0.22f);
                    if (cat.HeadTransform != null)
                        cat.HeadTransform.localRotation = Quaternion.Euler(-10f, Mathf.Sin(time * 1.5f + cat.AnimationPhase) * 6f, 0f);
                    if (cat.TailTransform != null)
                        cat.TailTransform.localRotation = Quaternion.Euler(30f + Mathf.Sin(time * 3f + cat.TailPhase) * 10f, 0f, 0f);
                    break;
                }
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

    private int FindNearestAmbientCatRoamPointIndex(Vector3 sourcePosition, int fallbackOffset)
    {
        if (ambientCatRoamPoints.Count == 0)
        {
            return 0;
        }

        int bestIndex = Mathf.Abs(fallbackOffset) % ambientCatRoamPoints.Count;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < ambientCatRoamPoints.Count; i++)
        {
            Vector3 point = ambientCatRoamPoints[i];
            float distance = Vector3.SqrMagnitude(point - sourcePosition);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        if (ambientCats.Count > 1)
        {
            bestIndex = (bestIndex + Mathf.Abs(fallbackOffset)) % ambientCatRoamPoints.Count;
        }

        return bestIndex;
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

    private void SetupAmbientSquirrels()
    {
        ambientSquirrels.Clear();
        ambientSquirrelRoamPoints.Clear();
        ambientSquirrelPerchHeights.Clear();
        if (ambientSquirrelRoot != null)
        {
            Destroy(ambientSquirrelRoot.gameObject);
        }

        if (worldRoot == null || miscTreePerchPoints.Count < 2)
        {
            return;
        }

        ambientSquirrelRoot = new GameObject("AmbientSquirrels").transform;
        ambientSquirrelRoot.SetParent(worldRoot, false);

        foreach (Vector3 perch in miscTreePerchPoints)
        {
            float groundY = SampleTerrainHeight(perch.x, perch.z);
            ambientSquirrelRoamPoints.Add(new Vector3(perch.x, groundY, perch.z));
            ambientSquirrelPerchHeights.Add(perch.y);
        }

        int count = Mathf.Min(AmbientSquirrelCount, ambientSquirrelRoamPoints.Count);
        for (int i = 0; i < count; i++)
        {
            CreateAmbientSquirrel(i, count);
        }
    }

    private void CreateAmbientSquirrel(int squirrelIndex, int totalCount)
    {
        if (ambientSquirrelRoot == null || ambientSquirrelRoamPoints.Count == 0)
        {
            return;
        }

        Color bodyColor = new(0.72f, 0.42f, 0.14f);
        Color headColor = new(0.80f, 0.50f, 0.20f);
        Color tailColor = new(0.78f, 0.48f, 0.18f);
        Color earColor  = new(0.68f, 0.38f, 0.12f);

        GameObject sqRoot = new($"AmbientSquirrel_{squirrelIndex + 1}");
        sqRoot.transform.SetParent(ambientSquirrelRoot, false);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(sqRoot.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.10f, 0f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.14f, 0.10f, 0.20f);
        ApplyColor(body, bodyColor, VisualSmoothnessFabric);
        ConfigureStaticVisual(body, VisualSmoothnessFabric);
        if (body.TryGetComponent(out Collider bodyCol)) bodyCol.enabled = false;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(sqRoot.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.16f, 0.12f);
        head.transform.localScale = new Vector3(0.10f, 0.09f, 0.09f);
        ApplyColor(head, headColor, VisualSmoothnessFabric);
        ConfigureStaticVisual(head, VisualSmoothnessFabric);
        if (head.TryGetComponent(out Collider headCol)) headCol.enabled = false;

        GameObject leftEar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftEar.transform.SetParent(head.transform, false);
        leftEar.transform.localPosition = new Vector3(-0.35f, 0.55f, 0f);
        leftEar.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
        leftEar.transform.localScale = new Vector3(0.25f, 0.50f, 0.22f);
        ApplyColor(leftEar, earColor, VisualSmoothnessFabric);
        ConfigureStaticVisual(leftEar, VisualSmoothnessFabric);
        if (leftEar.TryGetComponent(out Collider lEarCol)) lEarCol.enabled = false;

        GameObject rightEar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightEar.transform.SetParent(head.transform, false);
        rightEar.transform.localPosition = new Vector3(0.35f, 0.55f, 0f);
        rightEar.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
        rightEar.transform.localScale = new Vector3(0.25f, 0.50f, 0.22f);
        ApplyColor(rightEar, earColor, VisualSmoothnessFabric);
        ConfigureStaticVisual(rightEar, VisualSmoothnessFabric);
        if (rightEar.TryGetComponent(out Collider rEarCol)) rEarCol.enabled = false;

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tail.transform.SetParent(sqRoot.transform, false);
        tail.transform.localPosition = new Vector3(0f, 0.18f, -0.12f);
        tail.transform.localRotation = Quaternion.Euler(-55f, 0f, 0f);
        tail.transform.localScale = new Vector3(0.06f, 0.16f, 0.06f);
        ApplyColor(tail, tailColor, VisualSmoothnessFabric);
        ConfigureStaticVisual(tail, VisualSmoothnessFabric);
        if (tail.TryGetComponent(out Collider tailCol)) tailCol.enabled = false;

        int step = Mathf.Max(1, ambientSquirrelRoamPoints.Count / totalCount);
        int pointIndex = squirrelIndex * step % ambientSquirrelRoamPoints.Count;
        Vector3 position = ambientSquirrelRoamPoints[pointIndex];
        float yaw = Random.Range(0f, 360f);
        sqRoot.transform.position = position;
        sqRoot.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        ambientSquirrels.Add(new AmbientSquirrelData
        {
            RootTransform    = sqRoot.transform,
            BodyTransform    = body.transform,
            HeadTransform    = head.transform,
            TailTransform    = tail.transform,
            CurrentPosition  = position,
            StartPosition    = position,
            TargetPosition   = position,
            CurrentPointIndex = pointIndex,
            TargetPointIndex  = pointIndex,
            StateTimer       = Random.Range(2f, 5f),
            AnimationPhase   = Random.Range(0f, 10f),
            TailPhase        = Random.Range(0f, 10f),
            Yaw              = yaw,
            State            = AmbientSquirrelState.Idle,
            ClimbCooldown    = Random.Range(6f, 18f),
        });
    }

    private void UpdateAmbientSquirrels()
    {
        if (ambientSquirrels.Count == 0 || ambientSquirrelRoamPoints.Count == 0)
        {
            return;
        }

        bool active = AreAmbientSquirrelsActive();
        float dt   = Time.deltaTime * gameSpeedMultiplier;
        float time = Time.time;

        for (int i = ambientSquirrels.Count - 1; i >= 0; i--)
        {
            AmbientSquirrelData sq = ambientSquirrels[i];
            if (sq.RootTransform == null)
            {
                ambientSquirrels.RemoveAt(i);
                continue;
            }

            switch (sq.State)
            {
                case AmbientSquirrelState.Idle:
                    sq.StateTimer -= dt;
                    sq.ClimbCooldown -= dt;

                    float idleBob = Mathf.Sin(time * 2.4f + sq.AnimationPhase) * 0.012f;
                    sq.RootTransform.position = sq.CurrentPosition + new Vector3(0f, idleBob, 0f);
                    sq.RootTransform.rotation = Quaternion.Slerp(
                        sq.RootTransform.rotation,
                        Quaternion.Euler(0f, sq.Yaw, 0f),
                        6f * Time.deltaTime);

                    if (sq.HeadTransform != null)
                    {
                        sq.HeadTransform.localRotation = Quaternion.Euler(
                            Mathf.Sin(time * 1.1f + sq.AnimationPhase) * 6f,
                            Mathf.Sin(time * 0.7f + sq.AnimationPhase) * 12f,
                            0f);
                    }

                    if (sq.TailTransform != null)
                    {
                        sq.TailTransform.localRotation = Quaternion.Euler(
                            -55f + Mathf.Sin(time * 1.8f + sq.TailPhase) * 8f,
                            Mathf.Sin(time * 1.4f + sq.TailPhase) * 10f,
                            0f);
                    }

                    if (sq.StateTimer <= 0f)
                    {
                        if (!active)
                        {
                            // At night force squirrels down from trees
                            if (sq.IsAtTreeTop) StartSquirrelClimbDown(sq);
                            else sq.StateTimer = Random.Range(2f, 5f);
                            break;
                        }

                        // At tree top: forage briefly or climb back down
                        if (sq.IsAtTreeTop)
                        {
                            if (Random.value < 0.35f)
                            {
                                sq.State      = AmbientSquirrelState.Foraging;
                                sq.StateTimer = Random.Range(1f, 2.5f);
                            }
                            else
                            {
                                StartSquirrelClimbDown(sq);
                            }
                            break;
                        }

                        // On ground: maybe climb up if cooldown expired
                        if (sq.ClimbCooldown <= 0f &&
                            sq.CurrentPointIndex >= 0 &&
                            sq.CurrentPointIndex < ambientSquirrelPerchHeights.Count)
                        {
                            float perchY = ambientSquirrelPerchHeights[sq.CurrentPointIndex];
                            if (perchY > sq.CurrentPosition.y + 0.5f)
                            {
                                StartSquirrelClimbUp(sq, perchY);
                                break;
                            }
                        }

                        // Normal roaming on ground
                        int next = FindNextSquirrelRoamPoint(sq);
                        if (next >= 0 && next != sq.CurrentPointIndex)
                        {
                            if (Random.value < 0.3f)
                            {
                                sq.State      = AmbientSquirrelState.Foraging;
                                sq.StateTimer = Random.Range(1.5f, 3f);
                            }
                            else
                            {
                                sq.TargetPointIndex = next;
                                sq.StartPosition    = sq.CurrentPosition;
                                sq.TargetPosition   = ambientSquirrelRoamPoints[next];
                                sq.MoveProgress     = 0f;
                                sq.MoveDuration     = Mathf.Clamp(
                                    Vector3.Distance(sq.StartPosition, sq.TargetPosition) / 2.2f,
                                    0.6f, 2.8f);
                                sq.State = AmbientSquirrelState.Running;
                            }
                        }
                        else
                        {
                            sq.StateTimer = Random.Range(2f, 5f);
                        }
                    }
                    break;

                case AmbientSquirrelState.Foraging:
                    sq.StateTimer -= dt;

                    float forageBob = Mathf.Abs(Mathf.Sin(time * 6f + sq.AnimationPhase)) * 0.06f;
                    sq.RootTransform.position = sq.CurrentPosition + new Vector3(0f, forageBob, 0f);

                    if (sq.HeadTransform != null)
                    {
                        float nod = Mathf.Sin(time * 7f + sq.AnimationPhase) * 22f;
                        sq.HeadTransform.localRotation = Quaternion.Euler(nod, 0f, 0f);
                    }

                    if (sq.TailTransform != null)
                    {
                        sq.TailTransform.localRotation = Quaternion.Euler(-72f, 0f, 0f);
                    }

                    if (sq.StateTimer <= 0f)
                    {
                        sq.State     = AmbientSquirrelState.Idle;
                        sq.StateTimer = Random.Range(2f, 5f);
                    }
                    break;

                case AmbientSquirrelState.Running:
                    sq.MoveProgress += dt / Mathf.Max(0.001f, sq.MoveDuration);
                    float runT = Mathf.Clamp01(sq.MoveProgress);

                    Vector3 runPos = Vector3.Lerp(sq.StartPosition, sq.TargetPosition, runT);
                    runPos.y += Mathf.Abs(Mathf.Sin(time * 14f + sq.AnimationPhase)) * 0.025f;
                    sq.RootTransform.position = runPos;

                    Vector3 toTarget = sq.TargetPosition - runPos;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.0001f)
                    {
                        sq.RootTransform.rotation = Quaternion.Slerp(
                            sq.RootTransform.rotation,
                            Quaternion.LookRotation(toTarget.normalized, Vector3.up),
                            14f * Time.deltaTime);
                    }

                    if (sq.BodyTransform != null)
                    {
                        sq.BodyTransform.localScale = new Vector3(0.14f, 0.09f, 0.20f);
                    }

                    if (sq.TailTransform != null)
                    {
                        sq.TailTransform.localRotation = Quaternion.Euler(
                            -10f + Mathf.Sin(time * 10f + sq.TailPhase) * 8f,
                            0f, 0f);
                    }

                    if (runT >= 1f)
                    {
                        sq.CurrentPointIndex = sq.TargetPointIndex;
                        sq.CurrentPosition   = sq.TargetPosition;
                        sq.Yaw               = sq.RootTransform.eulerAngles.y;
                        if (sq.BodyTransform != null)
                        {
                            sq.BodyTransform.localScale = new Vector3(0.14f, 0.10f, 0.20f);
                        }
                        sq.State      = AmbientSquirrelState.Idle;
                        sq.StateTimer = Random.Range(1.5f, 3.5f);
                    }
                    break;

                case AmbientSquirrelState.ClimbingUp:
                    sq.ClimbProgress += dt / Mathf.Max(0.001f, sq.ClimbDuration);
                    float climbUpT = Mathf.Clamp01(sq.ClimbProgress);

                    sq.CurrentPosition = Vector3.Lerp(sq.StartPosition, sq.TargetPosition, climbUpT);
                    sq.RootTransform.position = sq.CurrentPosition;
                    sq.RootTransform.rotation = Quaternion.Slerp(
                        sq.RootTransform.rotation,
                        Quaternion.Euler(-72f, sq.Yaw, 0f),
                        10f * Time.deltaTime);

                    if (sq.BodyTransform != null)
                        sq.BodyTransform.localScale = new Vector3(0.14f, 0.09f, 0.20f);

                    if (sq.TailTransform != null)
                        sq.TailTransform.localRotation = Quaternion.Euler(
                            -10f + Mathf.Sin(time * 9f + sq.TailPhase) * 14f,
                            Mathf.Sin(time * 6f + sq.TailPhase) * 10f, 0f);

                    if (climbUpT >= 1f)
                    {
                        sq.IsAtTreeTop    = true;
                        sq.CurrentPosition = sq.TargetPosition;
                        if (sq.BodyTransform != null)
                            sq.BodyTransform.localScale = new Vector3(0.14f, 0.10f, 0.20f);
                        sq.RootTransform.rotation = Quaternion.Euler(0f, sq.Yaw, 0f);
                        sq.State      = AmbientSquirrelState.Idle;
                        sq.StateTimer = Random.Range(2.5f, 6f);
                    }
                    break;

                case AmbientSquirrelState.ClimbingDown:
                    sq.ClimbProgress += dt / Mathf.Max(0.001f, sq.ClimbDuration);
                    float climbDownT = Mathf.Clamp01(sq.ClimbProgress);

                    sq.CurrentPosition = Vector3.Lerp(sq.StartPosition, sq.TargetPosition, climbDownT);
                    sq.RootTransform.position = sq.CurrentPosition;
                    sq.RootTransform.rotation = Quaternion.Slerp(
                        sq.RootTransform.rotation,
                        Quaternion.Euler(72f, sq.Yaw, 0f),
                        10f * Time.deltaTime);

                    if (sq.BodyTransform != null)
                        sq.BodyTransform.localScale = new Vector3(0.14f, 0.09f, 0.20f);

                    if (sq.TailTransform != null)
                        sq.TailTransform.localRotation = Quaternion.Euler(
                            -10f + Mathf.Sin(time * 9f + sq.TailPhase) * 14f,
                            Mathf.Sin(time * 6f + sq.TailPhase) * 10f, 0f);

                    if (climbDownT >= 1f)
                    {
                        sq.IsAtTreeTop     = false;
                        sq.CurrentPosition = sq.TargetPosition;
                        if (sq.BodyTransform != null)
                            sq.BodyTransform.localScale = new Vector3(0.14f, 0.10f, 0.20f);
                        sq.RootTransform.rotation = Quaternion.Euler(0f, sq.Yaw, 0f);
                        sq.ClimbCooldown  = Random.Range(12f, 28f);
                        sq.State          = AmbientSquirrelState.Idle;
                        sq.StateTimer     = Random.Range(1.5f, 3f);
                    }
                    break;
            }
        }
    }

    private void StartSquirrelClimbUp(AmbientSquirrelData sq, float perchY)
    {
        sq.StartPosition  = sq.CurrentPosition;
        sq.TargetPosition = new Vector3(sq.CurrentPosition.x, perchY, sq.CurrentPosition.z);
        sq.ClimbDuration  = Mathf.Clamp((perchY - sq.CurrentPosition.y) / 2.8f, 0.4f, 2f);
        sq.ClimbProgress  = 0f;
        sq.ClimbCooldown  = Random.Range(14f, 30f);
        sq.State          = AmbientSquirrelState.ClimbingUp;
    }

    private void StartSquirrelClimbDown(AmbientSquirrelData sq)
    {
        if (sq == null || ambientSquirrelRoamPoints.Count == 0)
        {
            return;
        }

        if (sq.CurrentPointIndex < 0 || sq.CurrentPointIndex >= ambientSquirrelRoamPoints.Count)
        {
            sq.CurrentPointIndex = FindNearestSquirrelRoamPoint(sq.CurrentPosition);
            if (sq.CurrentPointIndex < 0)
            {
                sq.IsAtTreeTop = false;
                sq.State = AmbientSquirrelState.Idle;
                sq.StateTimer = Random.Range(2f, 5f);
                return;
            }
        }

        float groundY     = ambientSquirrelRoamPoints[sq.CurrentPointIndex].y;
        sq.StartPosition  = sq.CurrentPosition;
        sq.TargetPosition = new Vector3(sq.CurrentPosition.x, groundY, sq.CurrentPosition.z);
        sq.ClimbDuration  = Mathf.Clamp((sq.CurrentPosition.y - groundY) / 2.8f, 0.4f, 2f);
        sq.ClimbProgress  = 0f;
        sq.State          = AmbientSquirrelState.ClimbingDown;
    }

    private bool AreAmbientSquirrelsActive()
    {
        int hour = GetCurrentHour();
        return hour >= 6 && hour < 18;
    }

}
