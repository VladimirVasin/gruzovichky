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
}
