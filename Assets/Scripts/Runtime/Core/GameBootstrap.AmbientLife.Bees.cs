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


}
