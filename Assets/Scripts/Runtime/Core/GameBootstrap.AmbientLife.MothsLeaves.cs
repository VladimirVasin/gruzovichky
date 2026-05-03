using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
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

    private void SetupAmbientFallingLeaves()
    {
        ambientFallingLeaves.Clear();
        if (ambientFallingLeafRoot != null)
        {
            Destroy(ambientFallingLeafRoot.gameObject);
        }

        if (worldRoot == null || miscTreePerchPoints.Count == 0)
        {
            return;
        }

        ambientFallingLeafRoot = new GameObject("AmbientFallingLeaves").transform;
        ambientFallingLeafRoot.SetParent(worldRoot, false);

        Shader leafShader = ShaderRefs.Sprites ?? ShaderRefs.Unlit;
        for (int i = 0; i < AmbientFallingLeafCount; i++)
        {
            GameObject leafObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leafObj.name = $"FallingLeaf_{i + 1}";
            leafObj.transform.SetParent(ambientFallingLeafRoot, false);
            leafObj.transform.localScale = new Vector3(
                Random.Range(0.035f, 0.06f),
                0.006f,
                Random.Range(0.022f, 0.04f));

            if (leafObj.TryGetComponent(out Collider leafCollider))
            {
                leafCollider.enabled = false;
            }

            Color leafColor = GetRandomFallingLeafColor();
            Material leafMaterial = new(leafShader) { color = leafColor };
            Renderer leafRenderer = leafObj.GetComponent<Renderer>();
            if (leafRenderer != null)
            {
                leafRenderer.material = leafMaterial;
                leafRenderer.shadowCastingMode = ShadowCastingMode.Off;
                leafRenderer.receiveShadows = false;
            }

            AmbientFallingLeafData leaf = new()
            {
                Transform = leafObj.transform,
                Material = leafMaterial,
                BaseColor = leafColor
            };
            ambientFallingLeaves.Add(leaf);
            ResetAmbientFallingLeaf(leaf, randomizeHeight: true);
        }
    }

    private void UpdateAmbientFallingLeaves()
    {
        if (ambientFallingLeaves.Count == 0 || miscTreePerchPoints.Count == 0)
        {
            return;
        }

        float dt = Time.deltaTime * gameSpeedMultiplier;
        float time = Time.time;
        for (int i = ambientFallingLeaves.Count - 1; i >= 0; i--)
        {
            AmbientFallingLeafData leaf = ambientFallingLeaves[i];
            if (leaf?.Transform == null)
            {
                ambientFallingLeaves.RemoveAt(i);
                continue;
            }

            if (leaf.PerchPointIndex < 0 || leaf.PerchPointIndex >= miscTreePerchPoints.Count)
            {
                ResetAmbientFallingLeaf(leaf, randomizeHeight: false);
                continue;
            }

            leaf.Angle += leaf.SpiralSpeed * dt;
            Vector3 position = leaf.Transform.position;
            position.y -= leaf.FallSpeed * dt;

            float spiralStrength = leaf.SpiralRadius * (0.72f + Mathf.Sin(time * 0.55f + leaf.PhaseOffset) * 0.28f);
            Vector3 spiralDrift = new(
                Mathf.Cos(leaf.Angle) * spiralStrength,
                0f,
                Mathf.Sin(leaf.Angle * 0.82f) * spiralStrength);
            position += (leaf.DriftDirection + spiralDrift) * dt;

            leaf.Transform.position = position;
            leaf.Transform.Rotate(
                leaf.SpinSpeed * dt,
                leaf.SpinSpeed * 0.37f * dt,
                leaf.SpinSpeed * 0.61f * dt,
                Space.Self);

            if (position.y <= leaf.GroundY)
            {
                ResetAmbientFallingLeaf(leaf, randomizeHeight: false);
            }
        }
    }

    private void ResetAmbientFallingLeaf(AmbientFallingLeafData leaf, bool randomizeHeight)
    {
        if (leaf?.Transform == null || miscTreePerchPoints.Count == 0)
        {
            return;
        }

        int perchIndex = Random.Range(0, miscTreePerchPoints.Count);
        Vector3 perch = miscTreePerchPoints[perchIndex];
        Vector3 spawnOffset = new(
            Random.Range(-0.34f, 0.34f),
            randomizeHeight ? Random.Range(-0.9f, 0.35f) : Random.Range(-0.12f, 0.22f),
            Random.Range(-0.34f, 0.34f));

        Vector3 position = perch + spawnOffset;
        float groundY = SampleTerrainHeight(position.x, position.z) + 0.05f;
        if (position.y <= groundY + 0.55f)
        {
            position.y = groundY + Random.Range(1.2f, 2.2f);
        }

        leaf.PerchPointIndex = perchIndex;
        leaf.Center = new Vector3(perch.x, 0f, perch.z);
        leaf.GroundY = groundY;
        leaf.Angle = Random.Range(0f, Mathf.PI * 2f);
        leaf.SpiralRadius = Random.Range(0.05f, 0.18f);
        leaf.SpiralSpeed = Random.Range(0.7f, 1.45f);
        leaf.FallSpeed = Random.Range(0.18f, 0.34f);
        leaf.SpinSpeed = Random.Range(55f, 135f);
        leaf.PhaseOffset = Random.Range(0f, 20f);
        leaf.DriftDirection = new Vector3(Random.Range(-0.075f, 0.075f), 0f, Random.Range(-0.055f, 0.055f)) + CloudTravelDir * Random.Range(0.015f, 0.04f);
        leaf.Transform.position = position;
        leaf.Transform.rotation = Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(0f, 360f), Random.Range(-25f, 25f));

        if (leaf.Material != null)
        {
            leaf.BaseColor = GetRandomFallingLeafColor();
            leaf.Material.color = leaf.BaseColor;
        }
    }

    private static Color GetRandomFallingLeafColor()
    {
        Color[] colors =
        {
            new(0.95f, 0.58f, 0.12f, 0.92f),
            new(0.88f, 0.72f, 0.18f, 0.9f),
            new(0.78f, 0.26f, 0.12f, 0.88f),
            new(0.96f, 0.42f, 0.08f, 0.9f)
        };
        return colors[Random.Range(0, colors.Length)];
    }


}
