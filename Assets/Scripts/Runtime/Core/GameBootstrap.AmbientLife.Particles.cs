using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
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

    private void SetupExhaustSmoke()
    {
        exhaustSmokePool.Clear();
        truckDirtDustPool.Clear();
        if (exhaustSmokeRoot != null)
        {
            Destroy(exhaustSmokeRoot.gameObject);
        }
        if (truckDirtDustRoot != null)
        {
            Destroy(truckDirtDustRoot.gameObject);
        }

        if (worldRoot == null)
        {
            return;
        }

        exhaustSmokeRoot = new GameObject("ExhaustSmoke").transform;
        exhaustSmokeRoot.SetParent(worldRoot, false);
        truckDirtDustRoot = new GameObject("TruckDirtDust").transform;
        truckDirtDustRoot.SetParent(worldRoot, false);

        Shader spriteShader = ShaderRefs.Sprites ?? ShaderRefs.Unlit;
        for (int i = 0; i < ExhaustSmokePoolSize; i++)
        {
            GameObject puffObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puffObj.name = $"ExhaustPuff_{i + 1}";
            puffObj.transform.SetParent(exhaustSmokeRoot, false);
            puffObj.transform.localScale = Vector3.zero;
            if (puffObj.TryGetComponent(out Collider col))
            {
                col.enabled = false;
            }

            Material mat = new(spriteShader) { color = new Color(0.65f, 0.65f, 0.65f, 0f) };
            Renderer rend = puffObj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = mat;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }

            puffObj.SetActive(false);
            exhaustSmokePool.Add(new ExhaustSmokeParticle
            {
                Transform = puffObj.transform,
                Material = mat,
                IsActive = false
            });
        }

        for (int i = 0; i < TruckDirtDustPoolSize; i++)
        {
            GameObject dustObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dustObj.name = $"TruckDirtDust_{i + 1}";
            dustObj.transform.SetParent(truckDirtDustRoot, false);
            dustObj.transform.localScale = Vector3.zero;
            if (dustObj.TryGetComponent(out Collider col))
            {
                col.enabled = false;
            }

            Material mat = new(spriteShader) { color = new Color(0.72f, 0.58f, 0.38f, 0f) };
            Renderer rend = dustObj.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material = mat;
                rend.shadowCastingMode = ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }

            dustObj.SetActive(false);
            truckDirtDustPool.Add(new ExhaustSmokeParticle
            {
                Transform = dustObj.transform,
                Material = mat,
                IsActive = false
            });
        }
    }

    private void EmitBuildingChimneyParticle(Vector3 position)
    {
        ExhaustSmokeParticle slot = null;
        for (int i = 0; i < exhaustSmokePool.Count; i++)
        {
            if (!exhaustSmokePool[i].IsActive)
            {
                slot = exhaustSmokePool[i];
                break;
            }
        }

        if (slot?.Transform == null)
        {
            return;
        }

        slot.IsActive = true;
        slot.Transform.gameObject.SetActive(true);
        slot.LifeTimer = 0f;
        slot.MaxLife = Random.Range(2.2f, 3.2f);
        slot.BaseScale = Random.Range(0.07f, 0.11f);
        slot.Velocity = new Vector3(
            Random.Range(-0.02f, 0.02f),
            Random.Range(0.15f, 0.28f),
            Random.Range(-0.02f, 0.02f)) + CloudTravelDir * 0.06f;
        slot.Transform.position = position + new Vector3(
            Random.Range(-0.06f, 0.06f), 0f, Random.Range(-0.06f, 0.06f));
        slot.Transform.localScale = Vector3.one * slot.BaseScale;
    }

    private void EmitExhaustParticle(Vector3 position)
    {
        ExhaustSmokeParticle slot = null;
        for (int i = 0; i < exhaustSmokePool.Count; i++)
        {
            if (!exhaustSmokePool[i].IsActive)
            {
                slot = exhaustSmokePool[i];
                break;
            }
        }

        if (slot?.Transform == null)
        {
            return;
        }

        slot.IsActive = true;
        slot.Transform.gameObject.SetActive(true);
        slot.LifeTimer = 0f;
        slot.MaxLife = Random.Range(1.6f, 2.4f);
        slot.BaseScale = Random.Range(0.048f, 0.075f);
        slot.Velocity = new Vector3(
            Random.Range(-0.03f, 0.03f),
            Random.Range(0.22f, 0.38f),
            Random.Range(-0.03f, 0.03f)) + CloudTravelDir * 0.04f;
        slot.Transform.position = position + new Vector3(
            Random.Range(-0.04f, 0.04f), 0f, Random.Range(-0.04f, 0.04f));
        slot.Transform.localScale = Vector3.one * slot.BaseScale;
    }

    private void EmitTruckDirtDustParticle(Vector3 position, Vector3 driftDirection)
    {
        ExhaustSmokeParticle slot = null;
        for (int i = 0; i < truckDirtDustPool.Count; i++)
        {
            if (!truckDirtDustPool[i].IsActive)
            {
                slot = truckDirtDustPool[i];
                break;
            }
        }

        if (slot?.Transform == null)
        {
            return;
        }

        Vector3 lateral = new(-driftDirection.z, 0f, driftDirection.x);
        if (lateral.sqrMagnitude < 0.001f)
        {
            lateral = Vector3.right;
        }

        slot.IsActive = true;
        slot.Transform.gameObject.SetActive(true);
        slot.LifeTimer = 0f;
        slot.MaxLife = Random.Range(0.34f, 0.52f);
        slot.BaseScale = Random.Range(0.045f, 0.072f);
        slot.Velocity =
            -driftDirection.normalized * Random.Range(0.16f, 0.28f) +
            lateral.normalized * Random.Range(-0.08f, 0.08f) +
            Vector3.up * Random.Range(0.035f, 0.075f);
        slot.Transform.position = position + new Vector3(
            Random.Range(-0.05f, 0.05f),
            Random.Range(0.0f, 0.025f),
            Random.Range(-0.05f, 0.05f));
        slot.Transform.localScale = Vector3.one * slot.BaseScale;
    }

    private void UpdateExhaustSmoke()
    {
        float dt = Time.deltaTime * gameSpeedMultiplier;

        for (int i = 0; i < exhaustSmokePool.Count; i++)
        {
            ExhaustSmokeParticle p = exhaustSmokePool[i];
            if (!p.IsActive || p.Transform == null)
            {
                continue;
            }

            p.LifeTimer += dt;
            float t = p.LifeTimer / p.MaxLife;

            if (t >= 1f)
            {
                p.IsActive = false;
                p.Transform.gameObject.SetActive(false);
                p.Transform.localScale = Vector3.zero;
                continue;
            }

            p.Transform.position += p.Velocity * dt;
            p.Transform.localScale = Vector3.one * Mathf.Lerp(p.BaseScale, p.BaseScale * 2.8f, t);

            if (p.Material != null)
            {
                float alpha = Mathf.Lerp(0.42f, 0f, Mathf.Pow(t, 0.55f));
                float brightness = Mathf.Lerp(0.7f, 0.52f, t);
                p.Material.color = new Color(brightness, brightness, brightness, alpha);
            }
        }

        UpdateTruckDirtDustParticles(dt);

        for (int i = 0; i < truckAgents.Count; i++)
        {
            TruckAgent truck = truckAgents[i];
            if (!truck.IsTruckMoving || truck.TruckObject == null || !truck.TruckObject.activeSelf)
            {
                truck.ExhaustEmitTimer = 0f;
                truck.DirtDustEmitTimer = 0f;
                continue;
            }

            truck.ExhaustEmitTimer += dt;
            if (truck.ExhaustEmitTimer >= ExhaustEmitInterval)
            {
                truck.ExhaustEmitTimer -= ExhaustEmitInterval;
                Transform tr = truck.TruckObject.transform;
                EmitExhaustParticle(tr.position + tr.forward * -0.38f + Vector3.up * 0.48f);
            }

            if (ShouldTruckEmitDirtDust(truck))
            {
                truck.DirtDustEmitTimer += dt;
                if (truck.DirtDustEmitTimer >= TruckDirtDustEmitInterval)
                {
                    truck.DirtDustEmitTimer -= TruckDirtDustEmitInterval;
                    Transform tr = truck.TruckObject.transform;
                    Vector3 back = tr.position - tr.forward * 0.34f;
                    back.y = SampleTerrainHeight(back.x, back.z) + 0.08f;
                    EmitTruckDirtDustParticle(back + tr.right * -0.22f, tr.forward);
                    EmitTruckDirtDustParticle(back + tr.right * 0.22f, tr.forward);
                }
            }
            else
            {
                truck.DirtDustEmitTimer = 0f;
            }
        }

        if (localBusRoute?.RootTransform != null && localBusRoute.Phase == LocalBusPhase.DrivingRoute)
        {
            localBusExhaustEmitTimer += dt;
            if (localBusExhaustEmitTimer >= ExhaustEmitInterval)
            {
                localBusExhaustEmitTimer -= ExhaustEmitInterval;
                Transform bt = localBusRoute.RootTransform;
                EmitExhaustParticle(bt.position + bt.forward * -0.5f + Vector3.up * 0.55f);
            }
        }
        else
        {
            localBusExhaustEmitTimer = 0f;
        }

        bool isWorkHour = IsProductionWorkHour(GetCurrentHour());

        if (locations.TryGetValue(LocationType.Forest, out LocationData forest) && isWorkHour)
        {
            sawmillSmokeEmitTimer += dt;
            if (sawmillSmokeEmitTimer >= BuildingSmokeEmitInterval)
            {
                sawmillSmokeEmitTimer -= BuildingSmokeEmitInterval;
                Vector3 base3 = GetLocationCenter(forest);
                EmitBuildingChimneyParticle(base3 + new Vector3(0f, 0.92f, 0.22f));
            }
        }
        else
        {
            sawmillSmokeEmitTimer = 0f;
        }

        if (locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse) && isWorkHour)
        {
            furnitureFactorySmokeEmitTimer += dt;
            if (furnitureFactorySmokeEmitTimer >= BuildingSmokeEmitInterval)
            {
                furnitureFactorySmokeEmitTimer -= BuildingSmokeEmitInterval;
                Vector3 base3 = GetLocationCenter(warehouse);
                EmitBuildingChimneyParticle(base3 + new Vector3(0f, 1.22f, 0f));
            }
        }
        else
        {
            furnitureFactorySmokeEmitTimer = 0f;
        }
    }

    private void UpdateTruckDirtDustParticles(float dt)
    {
        for (int i = 0; i < truckDirtDustPool.Count; i++)
        {
            ExhaustSmokeParticle p = truckDirtDustPool[i];
            if (!p.IsActive || p.Transform == null)
            {
                continue;
            }

            p.LifeTimer += dt;
            float t = p.LifeTimer / p.MaxLife;
            if (t >= 1f)
            {
                p.IsActive = false;
                p.Transform.gameObject.SetActive(false);
                p.Transform.localScale = Vector3.zero;
                continue;
            }

            p.Transform.position += p.Velocity * dt;
            p.Transform.localScale = Vector3.one * Mathf.Lerp(p.BaseScale, p.BaseScale * 2.2f, t);

            if (p.Material != null)
            {
                Color dust = Color.Lerp(new Color(0.72f, 0.58f, 0.38f, 0.36f), new Color(0.82f, 0.72f, 0.55f, 0f), Mathf.Pow(t, 0.65f));
                p.Material.color = dust;
            }
        }
    }

    private bool ShouldTruckEmitDirtDust(TruckAgent truck)
    {
        if (truck?.TruckObject == null || !truck.IsTruckMoving)
        {
            return false;
        }

        Vector2Int cell = WorldToCell(truck.TruckObject.transform.position);
        if (!IsInsideGrid(cell) || roadCells.Contains(cell) || edgeHighwayCells.Contains(cell))
        {
            return false;
        }

        return !waterCells.Contains(cell);
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
