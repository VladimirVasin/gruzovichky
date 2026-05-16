using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void ProduceForestWood()
    {
        UpdateLumberyardSystem();
    }

    private void UpdateSawmillProcessing()
    {
        foreach (LocationData sawmill in EnumerateLocationsOfType(LocationType.Sawmill))
        {
            if (sawmill == null)
            {
                continue;
            }

            if (sawmill.LogsStored <= 0)
            {
                sawmill.ProductionProcessingTimer = 0f;
                continue;
            }

            if (!IsLocationOperational(sawmill))
            {
                continue;
            }

            sawmill.ProductionProcessingTimer += Time.deltaTime * gameSpeedMultiplier;
            if (sawmill.ProductionProcessingTimer < 4.5f)
            {
                continue;
            }

            sawmill.ProductionProcessingTimer = 0f;
            sawmill.LogsStored = Mathf.Max(0, sawmill.LogsStored - 1);
            sawmill.BoardsStored += 1;
            SessionDebugLogger.Log("SAWMILL", $"{GetBuildingInstanceDisplayName(LocationType.Sawmill, sawmill.InstanceId)} processed 1 Logs into Boards. Logs={sawmill.LogsStored}, Boards={sawmill.BoardsStored}.");
        }
    }

    private void UpdateFurnitureFactoryProcessing()
    {
        foreach (LocationData furnitureFactory in EnumerateLocationsOfType(LocationType.FurnitureFactory))
        {
            if (furnitureFactory == null)
            {
                continue;
            }

            if (furnitureFactory.BoardsStored <= 0 ||
                furnitureFactory.TextileStored <= 0 ||
                furnitureFactory.FurnitureStored >= FurnitureFactoryMaxFurnitureStorage)
            {
                furnitureFactory.ProductionProcessingTimer = 0f;
                continue;
            }

            if (!IsLocationOperational(furnitureFactory))
            {
                continue;
            }

            furnitureFactory.ProductionProcessingTimer += Time.deltaTime * gameSpeedMultiplier;
            if (furnitureFactory.ProductionProcessingTimer < FurnitureFactoryProcessingDuration)
            {
                continue;
            }

            furnitureFactory.ProductionProcessingTimer = 0f;
            furnitureFactory.BoardsStored = Mathf.Max(0, furnitureFactory.BoardsStored - 1);
            furnitureFactory.TextileStored = Mathf.Max(0, furnitureFactory.TextileStored - 1);
            furnitureFactory.FurnitureStored = Mathf.Min(FurnitureFactoryMaxFurnitureStorage, furnitureFactory.FurnitureStored + 1);
            SessionDebugLogger.Log(
                "FACTORY",
                $"{GetBuildingInstanceDisplayName(LocationType.FurnitureFactory, furnitureFactory.InstanceId)} produced 1 Furniture. Boards={furnitureFactory.BoardsStored}, Textile={furnitureFactory.TextileStored}, Furniture={furnitureFactory.FurnitureStored}.");
        }
    }

    private void UpdateTruckMovement()
    {
        if (isTruckInteracting || isDriverRescueActive)
        {
            UpdateTruckVisuals(0f, false);
            return;
        }

        if (!isTruckMoving || activePath.Count == 0)
        {
            UpdateTruckVisuals(0f, false);
            return;
        }

        if (truckSegmentDuration <= 0.0001f)
        {
            BeginNextTruckSegment(activePath[0]);
        }

        Vector3 segmentDirection = truckTargetWorld - truckSegmentStartWorld;
        float segmentDistance = segmentDirection.magnitude;
        if (segmentDistance <= 0.0001f)
        {
            CompleteTruckSegment();
            return;
        }

        float dt = Time.deltaTime * Mathf.Max(0f, gameSpeedMultiplier);
        truckSegmentProgress += dt / truckSegmentDuration;
        float segmentProgress = Mathf.Clamp01(truckSegmentProgress);
        Vector3 currentPosition = Vector3.Lerp(truckSegmentStartWorld, truckTargetWorld, segmentProgress);
        currentPosition = WithRoadVehicleHeight(currentPosition, TruckSegmentStartLift);
        truckObject.transform.position = currentPosition;

        Vector3 desiredForward = segmentDirection;
        desiredForward.y = 0f;
        if (desiredForward.sqrMagnitude > 0.0001f)
        {
            desiredForward = desiredForward.normalized;
            float forwardLerp = 1f - Mathf.Exp(-12f * dt);
            truckSmoothedForward = Vector3.Slerp(truckSmoothedForward, desiredForward, forwardLerp).normalized;
        }
        if (truckSmoothedForward.sqrMagnitude > 0.0001f)
        {
            float rotationLerp = 1f - Mathf.Exp(-14f * dt);
            truckObject.transform.rotation = Quaternion.Slerp(
                truckObject.transform.rotation,
                Quaternion.LookRotation(truckSmoothedForward, Vector3.up),
                rotationLerp);
        }

        float segmentSpeed = segmentDistance / Mathf.Max(truckSegmentDuration, 0.001f) * Mathf.Max(0f, gameSpeedMultiplier);
        UpdateTruckVisuals(segmentSpeed, true);

        if (truckSegmentProgress < 1f)
        {
            return;
        }

        CompleteTruckSegment();
    }

    private bool WouldIdleDriverOverlapAtPosition(DriverAgent driver, Vector3 proposedPosition)
    {
        float personalSpaceSqr = DriverIdlePersonalSpace * DriverIdlePersonalSpace;
        const float separationImprovementEpsilonSqr = 0.00001f;
        Vector3 currentPosition = driver.DriverObject.transform.position;
        Vector2Int currentCell = WorldToCell(currentPosition);
        Vector2Int proposedCell = WorldToCell(proposedPosition);
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent other = driverAgents[i];
            if (other == null || other == driver || other.DriverObject == null || !other.DriverObject.activeSelf)
            {
                continue;
            }

            Vector3 otherPosition = other.DriverObject.transform.position;
            Vector2Int otherCell = WorldToCell(otherPosition);
            Vector3 currentDelta = otherPosition - currentPosition;
            currentDelta.y = 0f;
            Vector3 proposedDelta = otherPosition - proposedPosition;
            proposedDelta.y = 0f;

            bool currentlyOverlapping = otherCell == currentCell || currentDelta.sqrMagnitude < personalSpaceSqr;
            bool proposedOverlapping = otherCell == proposedCell || proposedDelta.sqrMagnitude < personalSpaceSqr;
            if (!proposedOverlapping)
            {
                continue;
            }

            if (currentlyOverlapping &&
                proposedDelta.sqrMagnitude > currentDelta.sqrMagnitude + separationImprovementEpsilonSqr)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void SeparateOverlappingDrivers()
    {
        if (driverAgents.Count < 2) return;
        const float minSep = DriverIdlePersonalSpace;
        float pushStrength = 2.2f * Time.deltaTime * Mathf.Max(0f, gameSpeedMultiplier);
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent a = driverAgents[i];
            if (!IsDriverNudgeable(a)) continue;
            for (int j = i + 1; j < driverAgents.Count; j++)
            {
                DriverAgent b = driverAgents[j];
                if (!IsDriverNudgeable(b)) continue;
                Vector3 delta = a.DriverObject.transform.position - b.DriverObject.transform.position;
                delta.y = 0f;
                float dist = delta.magnitude;
                if (dist >= minSep) continue;
                Vector3 dir = dist < 0.001f
                    ? new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)).normalized
                    : delta / dist;
                float push = (minSep - dist) * pushStrength;
                Vector3 halfNudge = dir * (push * 0.5f);
                TryApplyDriverSeparationNudge(a, halfNudge);
                TryApplyDriverSeparationNudge(b, -halfNudge);
            }
        }
    }

    private bool TryApplyDriverSeparationNudge(DriverAgent driver, Vector3 nudge)
    {
        if (driver?.DriverObject == null || nudge.sqrMagnitude <= 0.000001f)
        {
            return false;
        }

        Vector3 currentPosition = driver.DriverObject.transform.position;
        Vector3 proposedPosition = currentPosition + nudge;
        if (!IsDriverSafeWalkCell(WorldToCell(proposedPosition)) ||
            DoesWalkSegmentCrossBlockedWalkCell(currentPosition, proposedPosition, driver.WalkPhase))
        {
            return false;
        }

        proposedPosition.y = SampleTerrainHeight(proposedPosition.x, proposedPosition.z);
        driver.DriverObject.transform.position = proposedPosition;
        UpdateDriverLastSafeWalkPosition(driver, proposedPosition);
        return true;
    }

    private static bool IsDriverNudgeable(DriverAgent driver)
    {
        if (driver == null ||
            driver.DriverObject == null ||
            !driver.DriverObject.activeSelf ||
            driver.IsInsideBuilding)
        {
            return false;
        }

        return driver.WalkPhase == DriverRescuePhase.None ||
               driver.WalkPhase == DriverRescuePhase.IdleWander ||
               driver.WalkPhase == DriverRescuePhase.IdleSmoking ||
               driver.WalkPhase == DriverRescuePhase.IdlePhoneCall;
    }

    private void UpdateDriverVisualAnimation(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        if (driver.DriverObject == null || driver.DriverVisualRoot == null)
        {
            return;
        }

        if (!driver.DriverObject.activeSelf)
        {
            driver.WalkAnimationTime = 0f;
            StopDriverSmokingParticles(driver);
            ApplyDriverPose(driver, 0f, 0f);
            return;
        }

        if (driver.WalkPhase != DriverRescuePhase.IdleSmoking)
        {
            StopDriverSmokingParticles(driver);
        }

        Vector3 toTarget = driver.WalkTargetWorld - driver.DriverObject.transform.position;
        toTarget.y = 0f;
        bool isWalking = driver.WalkPhase != DriverRescuePhase.None && toTarget.sqrMagnitude > 0.012f;
        bool isConversing = IsDriverIdleConversing(driver);
        if (isWalking)
        {
            driver.WalkAnimationTime += Time.deltaTime * 8.2f;
        }
        else
        {
            driver.WalkAnimationTime += Time.deltaTime * (isConversing ? 3.1f : 4.2f);
        }
        UpdateDriverFootsteps(driver, isWalking);

        if (!isWalking && isConversing)
        {
            DriverAgent partner = GetDriverAgentById(driver.IdleConversationPartnerId);
            if (partner?.DriverObject != null)
            {
                Vector3 faceDirection = partner.DriverObject.transform.position - driver.DriverObject.transform.position;
                faceDirection.y = 0f;
                if (faceDirection.sqrMagnitude > 0.0001f)
                {
                    driver.DriverObject.transform.rotation = Quaternion.Slerp(
                        driver.DriverObject.transform.rotation,
                        Quaternion.LookRotation(faceDirection.normalized, Vector3.up),
                        7f * Time.deltaTime);
                }
            }
        }

        if (!isWalking && driver.WalkPhase == DriverRescuePhase.IdlePettingCat)
        {
            int catIdx = driver.IdleCatPetTargetIndex;
            if (catIdx >= 0 && catIdx < ambientCats.Count)
            {
                AmbientCatData petCat = ambientCats[catIdx];
                if (petCat?.RootTransform != null)
                {
                    Vector3 faceDir = petCat.RootTransform.position - driver.DriverObject.transform.position;
                    faceDir.y = 0f;
                    if (faceDir.sqrMagnitude > 0.0001f)
                    {
                        driver.DriverObject.transform.rotation = Quaternion.Slerp(
                            driver.DriverObject.transform.rotation,
                            Quaternion.LookRotation(faceDir.normalized, Vector3.up),
                            6f * Time.deltaTime);
                    }
                }
            }
        }

        float swing = isWalking ? Mathf.Sin(driver.WalkAnimationTime) : 0f;
        float bob = isWalking
            ? Mathf.Abs(Mathf.Sin(driver.WalkAnimationTime * 2f)) * 0.06f
            : isConversing ? Mathf.Sin(driver.WalkAnimationTime * 1.8f) * 0.012f : 0f;

        switch (driver.WalkPhase)
        {
            case DriverRescuePhase.IdleSittingOnBench:
                ApplyDriverSittingPose(driver);
                return;
            case DriverRescuePhase.IdleAtBar:
            case DriverRescuePhase.IdleAtGamblingHall:
                if (TryApplyImportedServiceSeatPose(driver))
                {
                    return;
                }
                break;
            case DriverRescuePhase.IdleSmoking:
                ApplyDriverSmokingPose(driver);
                return;
            case DriverRescuePhase.IdlePhoneCall:
                ApplyDriverPhoneCallPose(driver);
                return;
            case DriverRescuePhase.IdleAtTrashCan:
                ApplyDriverTrashMealPose(driver);
                return;
            case DriverRescuePhase.IdlePettingCat:
                ApplyDriverPettingCatPose(driver);
                return;
            case DriverRescuePhase.IdleAtCityPark:
                ApplyDriverCityParkPose(driver);
                return;
            case DriverRescuePhase.LumberChopping:
            case DriverRescuePhase.LumberPlanting:
                return;
            case DriverRescuePhase.LumberReturnToBuilding:
                break;
        }

        ApplyDriverPose(driver, swing, bob);
    }

    private void ApplyDriverTrashMealPose(DriverAgent driver)
    {
        float rummage = Mathf.Sin(Time.time * 8f) * 8f;
        driver.DriverVisualRoot.localPosition = new Vector3(0f, -0.04f, 0f);
        driver.DriverVisualRoot.localRotation = Quaternion.Euler(0f, rummage * 0.18f, 0f);

        if (driver.DriverBodyTransform != null)
            driver.DriverBodyTransform.localRotation = Quaternion.Euler(24f, 0f, 0f);
        if (driver.DriverHeadTransform != null)
            driver.DriverHeadTransform.localRotation = Quaternion.Euler(16f, 0f, 0f);
        if (driver.DriverLeftArmTransform != null)
            driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(72f + rummage, -18f, -8f);
        if (driver.DriverRightArmTransform != null)
            driver.DriverRightArmTransform.localRotation = Quaternion.Euler(72f - rummage, 18f, 8f);
        if (driver.DriverLeftLegTransform != null)
            driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(-5f, 0f, 0f);
        if (driver.DriverRightLegTransform != null)
            driver.DriverRightLegTransform.localRotation = Quaternion.Euler(5f, 0f, 0f);

        ApplyImportedDriverPoseMotion(driver, ImportedDriverPoseKind.TrashMeal);
    }

    private void ApplyDriverSittingPose(DriverAgent driver)
    {
        float sway = Mathf.Sin(Time.time * 0.25f) * 1.5f;
        driver.DriverVisualRoot.localPosition = new Vector3(0f, -0.15f, 0f);
        driver.DriverVisualRoot.localRotation = Quaternion.Euler(0f, sway, 0f);

        if (driver.DriverBodyTransform != null)
            driver.DriverBodyTransform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        if (driver.DriverHeadTransform != null)
            driver.DriverHeadTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        if (driver.DriverCapTransform != null)
            driver.DriverCapTransform.localRotation = Quaternion.identity;
        if (driver.DriverLeftArmTransform != null)
            driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(35f, 0f, 0f);
        if (driver.DriverRightArmTransform != null)
            driver.DriverRightArmTransform.localRotation = Quaternion.Euler(35f, 0f, 0f);
        if (driver.DriverLeftLegTransform != null)
            driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(-85f, 0f, 0f);
        if (driver.DriverRightLegTransform != null)
            driver.DriverRightLegTransform.localRotation = Quaternion.Euler(-85f, 0f, 0f);

        ApplyImportedDriverPoseMotion(driver, ImportedDriverPoseKind.Sitting);
    }

    private void ApplyDriverCityParkPose(DriverAgent driver)
    {
        float t = Time.time + driver.DriverId * 0.37f;
        switch (driver.CityParkActivityStyle)
        {
            case 1:
                ApplyDriverSittingPose(driver);
                return;

            case 2:
                driver.DriverVisualRoot.localPosition = new Vector3(0f, Mathf.Sin(t * 1.4f) * 0.015f, 0f);
                driver.DriverVisualRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.9f) * 2.2f);
                if (driver.DriverBodyTransform != null)
                    driver.DriverBodyTransform.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.1f) * 4f, 0f, 0f);
                if (driver.DriverHeadTransform != null)
                    driver.DriverHeadTransform.localRotation = Quaternion.Euler(-4f + Mathf.Sin(t * 1.3f) * 3f, 0f, 0f);
                if (driver.DriverLeftArmTransform != null)
                    driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(-118f + Mathf.Sin(t * 1.8f) * 12f, 0f, 0f);
                if (driver.DriverRightArmTransform != null)
                    driver.DriverRightArmTransform.localRotation = Quaternion.Euler(-112f + Mathf.Sin(t * 1.6f + 0.7f) * 12f, 0f, 0f);
                if (driver.DriverLeftLegTransform != null)
                    driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(-4f, 0f, 0f);
                if (driver.DriverRightLegTransform != null)
                    driver.DriverRightLegTransform.localRotation = Quaternion.Euler(4f, 0f, 0f);
                ApplyImportedDriverPoseMotion(driver, ImportedDriverPoseKind.CityParkExercise);
                return;

            case 3:
                driver.DriverVisualRoot.localPosition = new Vector3(0f, 0f, 0f);
                driver.DriverVisualRoot.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.35f) * 8f, 0f);
                if (driver.DriverBodyTransform != null)
                    driver.DriverBodyTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.45f) * 2f);
                if (driver.DriverHeadTransform != null)
                    driver.DriverHeadTransform.localRotation = Quaternion.Euler(-8f, Mathf.Sin(t * 0.65f) * 18f, 0f);
                if (driver.DriverLeftArmTransform != null)
                    driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(12f, 0f, 0f);
                if (driver.DriverRightArmTransform != null)
                    driver.DriverRightArmTransform.localRotation = Quaternion.Euler(-18f + Mathf.Sin(t * 1.2f) * 8f, 0f, 0f);
                if (driver.DriverLeftLegTransform != null)
                    driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(2f, 0f, 0f);
                if (driver.DriverRightLegTransform != null)
                    driver.DriverRightLegTransform.localRotation = Quaternion.Euler(-2f, 0f, 0f);
                ApplyImportedDriverPoseMotion(driver, ImportedDriverPoseKind.CityParkLookAround);
                return;

            case 4:
                driver.DriverVisualRoot.localPosition = new Vector3(0f, -0.11f + Mathf.Sin(t * 0.7f) * 0.006f, 0f);
                driver.DriverVisualRoot.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.28f) * 4f, 0f);
                if (driver.DriverBodyTransform != null)
                    driver.DriverBodyTransform.localRotation = Quaternion.Euler(18f, 0f, 0f);
                if (driver.DriverHeadTransform != null)
                    driver.DriverHeadTransform.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.9f) * 4f, 0f, 0f);
                if (driver.DriverLeftArmTransform != null)
                    driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(48f + Mathf.Sin(t * 1.4f) * 10f, 0f, 0f);
                if (driver.DriverRightArmTransform != null)
                    driver.DriverRightArmTransform.localRotation = Quaternion.Euler(58f + Mathf.Sin(t * 1.3f + 0.4f) * 10f, 0f, 0f);
                if (driver.DriverLeftLegTransform != null)
                    driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(-70f, 0f, 0f);
                if (driver.DriverRightLegTransform != null)
                    driver.DriverRightLegTransform.localRotation = Quaternion.Euler(-52f, 0f, 0f);
                ApplyImportedDriverPoseMotion(driver, ImportedDriverPoseKind.CityParkRest);
                return;

            default:
                float sway = Mathf.Sin(t * 1.2f) * 0.45f;
                ApplyDriverPose(driver, sway, Mathf.Sin(t * 1.6f) * 0.012f);
                return;
        }
    }

    private void ApplyDriverSmokingPose(DriverAgent driver)
    {
        float drag = Mathf.Sin(Time.time * 1.1f) * 8f;
        driver.DriverVisualRoot.localPosition = Vector3.zero;
        driver.DriverVisualRoot.localRotation = Quaternion.identity;

        if (driver.DriverBodyTransform != null)
            driver.DriverBodyTransform.localRotation = Quaternion.Euler(0f, 0f, 3f);
        if (driver.DriverHeadTransform != null)
            driver.DriverHeadTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        if (driver.DriverCapTransform != null)
            driver.DriverCapTransform.localRotation = Quaternion.identity;
        if (driver.DriverLeftArmTransform != null)
            driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        if (driver.DriverRightArmTransform != null)
            driver.DriverRightArmTransform.localRotation = Quaternion.Euler(-65f + drag, 0f, 0f);
        if (driver.DriverLeftLegTransform != null)
            driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(3f, 0f, 0f);
        if (driver.DriverRightLegTransform != null)
            driver.DriverRightLegTransform.localRotation = Quaternion.Euler(-3f, 0f, 0f);

        ApplyImportedDriverPoseMotion(driver, ImportedDriverPoseKind.Smoking);
        UpdateDriverSmokingParticles(driver);
    }

    private void InitDriverSmokingParticles(DriverAgent driver)
    {
        driver.SmokingParticles = new Transform[SmokingParticlePoolSize];
        driver.SmokingParticleMaterials = new Material[SmokingParticlePoolSize];
        driver.SmokingParticleLives = new float[SmokingParticlePoolSize];
        driver.SmokingParticleVelocities = new Vector3[SmokingParticlePoolSize];
        for (int i = 0; i < SmokingParticlePoolSize; i++)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            p.name = "SmokePuff";
            p.transform.SetParent(worldRoot, false);
            p.transform.localScale = Vector3.one * 0.04f;
            Material mat = CreateTransparentOverlayMaterial(new Color(0.82f, 0.82f, 0.82f, 0f));
            Renderer r = p.GetComponent<Renderer>();
            r.material = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (p.TryGetComponent(out Collider col)) Object.Destroy(col);
            p.SetActive(false);
            driver.SmokingParticles[i] = p.transform;
            driver.SmokingParticleMaterials[i] = mat;
        }
    }

    private void UpdateDriverSmokingParticles(DriverAgent driver)
    {
        if (driver.SmokingParticles == null)
            InitDriverSmokingParticles(driver);

        float dt = Time.deltaTime * gameSpeedMultiplier;

        for (int i = 0; i < SmokingParticlePoolSize; i++)
        {
            if (driver.SmokingParticleLives[i] <= 0f) continue;
            driver.SmokingParticleLives[i] -= dt;
            Transform p = driver.SmokingParticles[i];
            if (p == null) continue;
            p.position += driver.SmokingParticleVelocities[i] * dt;
            float t = 1f - Mathf.Clamp01(driver.SmokingParticleLives[i] / SmokingParticleMaxLife);
            float alpha = (1f - t * t) * 0.4f;
            float scale = Mathf.Lerp(0.04f, 0.13f, t);
            p.localScale = Vector3.one * scale;
            driver.SmokingParticleMaterials[i].color = new Color(0.84f, 0.84f, 0.84f, alpha);
            if (driver.SmokingParticleLives[i] <= 0f)
                p.gameObject.SetActive(false);
        }

        driver.SmokingEmitTimer -= dt;
        if (driver.SmokingEmitTimer > 0f || driver.DriverRightArmTransform == null) return;

        driver.SmokingEmitTimer = SmokingParticleEmitInterval;
        int slot = -1;
        for (int i = 0; i < SmokingParticlePoolSize; i++)
        {
            if (driver.SmokingParticleLives[i] <= 0f) { slot = i; break; }
        }
        if (slot < 0) return;

        Vector3 spawnPos = driver.DriverRightArmTransform.position
            + driver.DriverRightArmTransform.up * 0.18f;
        Transform pSlot = driver.SmokingParticles[slot];
        pSlot.position = spawnPos;
        pSlot.gameObject.SetActive(true);
        driver.SmokingParticleLives[slot] = SmokingParticleMaxLife;
        driver.SmokingParticleVelocities[slot] = new Vector3(
            Random.Range(-0.014f, 0.014f),
            Random.Range(0.09f, 0.15f),
            Random.Range(-0.014f, 0.014f));
    }

    private void StopDriverSmokingParticles(DriverAgent driver)
    {
        if (driver == null || driver.SmokingParticles == null)
        {
            return;
        }

        driver.SmokingEmitTimer = 0f;
        for (int i = 0; i < driver.SmokingParticles.Length; i++)
        {
            driver.SmokingParticleLives[i] = 0f;
            Transform particle = driver.SmokingParticles[i];
            if (particle != null)
            {
                particle.gameObject.SetActive(false);
            }

            if (driver.SmokingParticleMaterials != null &&
                i < driver.SmokingParticleMaterials.Length &&
                driver.SmokingParticleMaterials[i] != null)
            {
                driver.SmokingParticleMaterials[i].color = new Color(0.84f, 0.84f, 0.84f, 0f);
            }
        }
    }

    private void ApplyDriverPettingCatPose(DriverAgent driver)
    {
        float pet = Mathf.Sin(Time.time * 2.8f) * 8f;
        driver.DriverVisualRoot.localPosition = new Vector3(0f, -0.08f, 0f);
        driver.DriverVisualRoot.localRotation = Quaternion.identity;

        if (driver.DriverBodyTransform != null)
            driver.DriverBodyTransform.localRotation = Quaternion.Euler(22f, 0f, 0f);
        if (driver.DriverHeadTransform != null)
            driver.DriverHeadTransform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        if (driver.DriverCapTransform != null)
            driver.DriverCapTransform.localRotation = Quaternion.identity;
        if (driver.DriverLeftArmTransform != null)
            driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(40f + pet, 0f, 0f);
        if (driver.DriverRightArmTransform != null)
            driver.DriverRightArmTransform.localRotation = Quaternion.Euler(15f, 0f, 0f);
        if (driver.DriverLeftLegTransform != null)
            driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        if (driver.DriverRightLegTransform != null)
            driver.DriverRightLegTransform.localRotation = Quaternion.Euler(-10f, 0f, 0f);

        ApplyImportedDriverPoseMotion(driver, ImportedDriverPoseKind.PettingCat);
    }

    private void ApplyDriverPhoneCallPose(DriverAgent driver)
    {
        driver.DriverVisualRoot.localPosition = Vector3.zero;
        driver.DriverVisualRoot.localRotation = Quaternion.identity;
        driver.DriverObject.transform.Rotate(Vector3.up, 6f * Time.deltaTime * gameSpeedMultiplier);

        if (driver.DriverBodyTransform != null)
            driver.DriverBodyTransform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        if (driver.DriverHeadTransform != null)
            driver.DriverHeadTransform.localRotation = Quaternion.Euler(5f, 0f, 0f);
        if (driver.DriverCapTransform != null)
            driver.DriverCapTransform.localRotation = Quaternion.identity;
        if (driver.DriverLeftArmTransform != null)
            driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        if (driver.DriverRightArmTransform != null)
            driver.DriverRightArmTransform.localRotation = Quaternion.Euler(-75f, 0f, 0f);
        if (driver.DriverLeftLegTransform != null)
            driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(3f, 0f, 0f);
        if (driver.DriverRightLegTransform != null)
            driver.DriverRightLegTransform.localRotation = Quaternion.Euler(-3f, 0f, 0f);

        ApplyImportedDriverPoseMotion(driver, ImportedDriverPoseKind.PhoneCall);
    }

    private void ApplyDriverPose(DriverAgent driver, float swing, float bob)
    {
        bool isConversing = IsDriverIdleConversing(driver);
        float speechWeight = GetWorkerIdleDialogueSpeechWeight(driver);
        float conversationEnergy = isConversing ? Mathf.Clamp01(0.35f + speechWeight * 0.8f) : 0f;
        driver.DriverVisualRoot.localPosition = new Vector3(0f, bob, 0f);
        driver.DriverVisualRoot.localRotation = Quaternion.Euler(0f, 0f, isConversing ? Mathf.Sin(driver.WalkAnimationTime * 1.3f) * Mathf.Lerp(1.2f, 3.1f, conversationEnergy) : swing * 2.5f);

        if (driver.DriverBodyTransform != null)
        {
            driver.DriverBodyTransform.localRotation = Quaternion.Euler(isConversing ? Mathf.Sin(driver.WalkAnimationTime * 1.1f) * Mathf.Lerp(2.6f, 6.4f, conversationEnergy) : swing * 4f, 0f, 0f);
        }

        if (driver.DriverHeadTransform != null)
        {
            float headPitch = isConversing ? Mathf.Sin(driver.WalkAnimationTime * 1.7f) * Mathf.Lerp(2.4f, 7.2f, conversationEnergy) : -swing * 2f;
            driver.DriverHeadTransform.localRotation = Quaternion.Euler(headPitch, 0f, 0f);
        }

        if (driver.DriverCapTransform != null)
        {
            driver.DriverCapTransform.localRotation = Quaternion.Euler(-swing * 1.5f, 0f, 0f);
        }

        if (driver.DriverLeftArmTransform != null)
        {
            float leftArmPitch = isConversing
                ? 8f + Mathf.Sin(driver.WalkAnimationTime * 1.9f + driver.DriverId * 0.4f) * Mathf.Lerp(9f, 25f, conversationEnergy)
                : swing * 28f;
            driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(leftArmPitch, 0f, 0f);
        }

        if (driver.DriverRightArmTransform != null)
        {
            float carryOffset = driver.DriverFuelCanTransform != null && driver.DriverFuelCanTransform.gameObject.activeSelf ? 18f : 0f;
            float rightArmPitch = isConversing
                ? -10f + Mathf.Sin(driver.WalkAnimationTime * 2.1f + driver.DriverId * 0.7f + 1.3f) * Mathf.Lerp(10f, 29f, conversationEnergy)
                : -swing * 28f - carryOffset;
            driver.DriverRightArmTransform.localRotation = Quaternion.Euler(rightArmPitch, 0f, 0f);
        }

        if (driver.DriverLeftLegTransform != null)
        {
            driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(isConversing ? 3f : -swing * 24f, 0f, 0f);
        }

        if (driver.DriverRightLegTransform != null)
        {
            driver.DriverRightLegTransform.localRotation = Quaternion.Euler(isConversing ? -3f : swing * 24f, 0f, 0f);
        }

        if (driver.DriverFuelCanTransform != null && driver.DriverFuelCanTransform.gameObject.activeSelf)
        {
            driver.DriverFuelCanTransform.localPosition = new Vector3(0.2f, 0.4f - bob * 0.2f, 0.04f);
            driver.DriverFuelCanTransform.localRotation = Quaternion.Euler(0f, 0f, -10f + swing * 6f);
        }
        else if (driver.DriverFuelCanTransform != null)
        {
            driver.DriverFuelCanTransform.localPosition = new Vector3(0.18f, 0.42f, 0f);
            driver.DriverFuelCanTransform.localRotation = Quaternion.identity;
        }

        if (driver.DriverFlashlightTransform != null)
        {
            driver.DriverFlashlightTransform.localPosition = new Vector3(0.24f, 0.57f - bob * 0.12f, 0.1f);
            driver.DriverFlashlightTransform.localRotation = Quaternion.Euler(16f + swing * 10f, swing * 5f, 0f);
        }

        ImportedDriverPoseKind poseKind = isConversing
            ? ImportedDriverPoseKind.Conversation
            : Mathf.Abs(swing) > 0.001f || Mathf.Abs(bob) > 0.001f
                ? ImportedDriverPoseKind.Walk
                : ImportedDriverPoseKind.Neutral;
        ApplyImportedDriverPoseMotion(driver, poseKind, swing: swing, bob: bob);
    }

    private void ResolveWorkerGamblingSpinResult(DriverAgent driver)
    {
        bool hasGamblingWeakness = HasWorkerWeakness(driver, WorkerWeaknessKind.Gambling);
        var rng = new System.Random();

        int casinoBank = locations.TryGetValue(LocationType.GamblingHall, out LocationData gh) ? gh.BuildingBank : 0;

        int minBet = WorkerGamblingMinBet;
        int maxBet;
        if (hasGamblingWeakness)
        {
            if (driver.Money < minBet || casinoBank < minBet)
            {
                driver.GamblingBroke = true;
                driver.GamblingBet = 0;
                driver.GamblingPayout = 0;
                driver.GamblingMultiplier = 0;
                driver.GamblingMoneyPending = false;
                RecordWorkerGamblingAffect(driver, 0, broke: true);
                SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} [GAMBLING_WEAKNESS] is broke (money=${driver.Money}, casinoBank=${casinoBank}) - skipping bet.");
                return;
            }
            driver.GamblingBroke = false;
            maxBet = Mathf.Min(driver.Money, casinoBank);
            if (driver.GamblingLostLastTime)
                maxBet = Mathf.Min(driver.Money, Mathf.RoundToInt(maxBet * 1.5f));
            maxBet = Mathf.Max(minBet, maxBet);
        }
        else
        {
            maxBet = Mathf.Clamp(driver.Money, WorkerGamblingMinBet, WorkerGamblingMaxBet);
        }

        int bet = rng.Next(minBet, maxBet + 1);
        float roll = (float)rng.NextDouble();
        int multiplier = roll < 0.55f ? 0 : roll < 0.85f ? 1 : roll < 0.97f ? 5 : 10;

        int payout;
        if (hasGamblingWeakness)
        {
            payout = multiplier switch
            {
                10 => bet * 12,
                5  => bet * 6,
                1  => bet,
                _  => Mathf.RoundToInt(bet * 0.2f)  // lose only 80%
            };
            driver.GamblingLostLastTime = (multiplier == 0);
        }
        else
        {
            payout = bet * multiplier;
        }

        int net = payout - bet;
        driver.GamblingBet          = bet;
        driver.GamblingPayout       = payout;
        driver.GamblingMultiplier   = multiplier;
        driver.GamblingMoneyPending = true;
        driver.GamblingBetCount++;
        RecordWorkerGamblingAffect(driver, net, broke: false);

        string outcomeStr = multiplier == 0 ? "LOSS" : $"WIN x{multiplier}";
        string gamblerTag = hasGamblingWeakness ? $" [GAMBLING_WEAKNESS bet#{driver.GamblingBetCount}]" : "";
        SessionDebugLogger.Log("NEEDS", $"{driver.DriverName}{gamblerTag} gambling; bet=${bet}, roll={roll:0.00}, outcome={outcomeStr}, payout=${payout}, net={net:+#;-#;0}, balance pending=${driver.Money}; need={FormatWorkerNeedDebug(driver, WorkerNeedKind.Leisure)}, snapshot={FormatWorkerNeedsDebug(driver)}.");
    }

}

