using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private sealed class AmbientFallingLeafData
    {
        public Transform Transform;
        public Material Material;
        public int PerchPointIndex;
        public Vector3 Center;
        public Vector3 DriftDirection;
        public float GroundY;
        public float Angle;
        public float SpiralRadius;
        public float SpiralSpeed;
        public float FallSpeed;
        public float SpinSpeed;
        public float PhaseOffset;
        public Color BaseColor;
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

    private void SetupAmbientFireflies()
    {
        ambientFireflies.Clear();
        if (ambientFireflyRoot != null)
        {
            Destroy(ambientFireflyRoot.gameObject);
        }

        if (worldRoot == null) return;

        ambientFireflyRoot = new GameObject("AmbientFireflies").transform;
        ambientFireflyRoot.SetParent(worldRoot, false);

        // Anchor candidates: lake shoreline cells + tree base positions.
        List<Vector3> anchors = new();
        foreach (Vector2Int cell in naturalBeachCells)
        {
            anchors.Add(new Vector3(cell.x + 0.5f, SampleTerrainHeight(cell.x + 0.5f, cell.y + 0.5f), cell.y + 0.5f));
        }
        foreach (Vector3 perch in miscTreePerchPoints)
        {
            anchors.Add(new Vector3(perch.x, SampleTerrainHeight(perch.x, perch.z), perch.z));
        }

        if (anchors.Count == 0) return;

        for (int i = 0; i < AmbientFireflyCount; i++)
        {
            Vector3 anchor = anchors[Random.Range(0, anchors.Count)];
            anchor.x += Random.Range(-1.2f, 1.2f);
            anchor.z += Random.Range(-1.2f, 1.2f);
            CreateAmbientFirefly(anchor);
        }

        wereAmbientFirefliesActiveLastFrame = AreAmbientFirefliesActive();
    }

    private void CreateAmbientFirefly(Vector3 anchor)
    {
        if (ambientFireflyRoot == null) return;

        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        obj.name = $"Firefly_{ambientFireflies.Count + 1}";
        obj.transform.SetParent(ambientFireflyRoot, false);
        obj.transform.localScale = Vector3.one * Random.Range(0.022f, 0.036f);
        ApplyUnlitColor(obj, new Color(0.1f, 0.12f, 0.04f));
        ConfigureStaticVisual(obj);
        if (obj.TryGetComponent(out Collider col)) col.enabled = false;

        Renderer rend = obj.GetComponent<Renderer>();
        AmbientFireflyData fly = new()
        {
            Transform    = obj.transform,
            Material     = rend != null ? rend.material : null,
            BasePosition = anchor,
            BaseGroundY  = anchor.y,
            DriftPhaseX  = Random.Range(0f, 10f),
            DriftPhaseY  = Random.Range(0f, 10f),
            DriftPhaseZ  = Random.Range(0f, 10f),
            DriftSpeedX  = Random.Range(0.18f, 0.38f),
            DriftSpeedY  = Random.Range(0.22f, 0.44f),
            DriftSpeedZ  = Random.Range(0.16f, 0.34f),
            DriftRadius  = Random.Range(0.5f, 1.4f),
            GlowPhase    = Random.Range(0f, 10f),
            GlowSpeed    = Random.Range(1.2f, 2.8f),
            Visibility   = 0f,
        };
        fly.Transform.gameObject.SetActive(false);
        ambientFireflies.Add(fly);
    }

    private void UpdateAmbientFireflies()
    {
        if (ambientFireflies.Count == 0) return;

        bool active = AreAmbientFirefliesActive();
        if (active && !wereAmbientFirefliesActiveLastFrame)
        {
            // Randomise positions when becoming active each night.
            List<Vector3> anchors = new();
            foreach (Vector2Int cell in naturalBeachCells)
                anchors.Add(new Vector3(cell.x + 0.5f, SampleTerrainHeight(cell.x + 0.5f, cell.y + 0.5f), cell.y + 0.5f));
            foreach (Vector3 perch in miscTreePerchPoints)
                anchors.Add(new Vector3(perch.x, SampleTerrainHeight(perch.x, perch.z), perch.z));
            if (anchors.Count > 0)
            {
                foreach (AmbientFireflyData fly in ambientFireflies)
                {
                    Vector3 anchor = anchors[Random.Range(0, anchors.Count)];
                    fly.BasePosition = new Vector3(
                        anchor.x + Random.Range(-1.2f, 1.2f),
                        anchor.y,
                        anchor.z + Random.Range(-1.2f, 1.2f));
                    fly.BaseGroundY = anchor.y;
                }
            }
        }
        wereAmbientFirefliesActiveLastFrame = active;

        float dt   = Time.deltaTime * gameSpeedMultiplier;
        float time = Time.time;

        for (int i = ambientFireflies.Count - 1; i >= 0; i--)
        {
            AmbientFireflyData fly = ambientFireflies[i];
            if (fly?.Transform == null) { ambientFireflies.RemoveAt(i); continue; }

            fly.Visibility = Mathf.MoveTowards(fly.Visibility, active ? 1f : 0f, dt * 0.6f);

            bool visible = fly.Visibility > 0.001f;
            if (fly.Transform.gameObject.activeSelf != visible)
                fly.Transform.gameObject.SetActive(visible);

            if (!visible) continue;

            // Slow sinusoidal drift around base position.
            float x = fly.BasePosition.x + Mathf.Sin(time * fly.DriftSpeedX + fly.DriftPhaseX) * fly.DriftRadius;
            float z = fly.BasePosition.z + Mathf.Sin(time * fly.DriftSpeedZ + fly.DriftPhaseZ) * fly.DriftRadius * 0.8f;
            float yOffset = Mathf.Sin(time * fly.DriftSpeedY + fly.DriftPhaseY) * 0.45f + 0.75f;
            float y = fly.BaseGroundY + Mathf.Clamp(yOffset, 0.3f, 1.2f);
            fly.Transform.position = new Vector3(x, y, z);

            // Pulse: 0→1 glow cycle.
            float glowPulse = Mathf.Sin(time * fly.GlowSpeed + fly.GlowPhase) * 0.5f + 0.5f;
            float brightness = fly.Visibility * (0.55f + glowPulse * 0.45f);
            if (fly.Material != null)
            {
                fly.Material.color = new Color(
                    1.2f * brightness,
                    1.1f * brightness,
                    0.05f * brightness);
            }
        }
    }

    private bool AreAmbientFirefliesActive()
    {
        int hour = GetCurrentHour();
        return hour >= 20 || hour < 4;
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
