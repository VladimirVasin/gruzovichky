using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap : MonoBehaviour
{
    private const string SharedBusVisualMotionRootName = "BusVisualMotionRoot";
    private static readonly Quaternion SharedBusProceduralWheelBaseRotation = Quaternion.Euler(90f, 0f, 0f);

    private void BuildSharedBusVisual(
        Transform parent,
        Color bodyColor,
        string leftLightName,
        string rightLightName,
        out Renderer headlightLeftRenderer,
        out Renderer headlightRightRenderer,
        out Material headlightLeftMaterial,
        out Material headlightRightMaterial,
        out Light leftLight,
        out Light rightLight)
    {
        Transform visualRoot = CreateSharedBusVisualMotionRoot(parent);
        if (TryBuildImportedBusVisual(
            visualRoot,
            bodyColor,
            leftLightName,
            rightLightName,
            out headlightLeftRenderer,
            out headlightRightRenderer,
            out headlightLeftMaterial,
            out headlightRightMaterial,
            out leftLight,
            out rightLight))
        {
            return;
        }

        BuildProceduralSharedBusVisual(
            visualRoot,
            bodyColor,
            leftLightName,
            rightLightName,
            out headlightLeftRenderer,
            out headlightRightRenderer,
            out headlightLeftMaterial,
            out headlightRightMaterial,
            out leftLight,
            out rightLight);
    }

    private static Transform CreateSharedBusVisualMotionRoot(Transform parent)
    {
        Transform visualRoot = new GameObject(SharedBusVisualMotionRootName).transform;
        visualRoot.SetParent(parent, false);
        visualRoot.localPosition = Vector3.zero;
        visualRoot.localRotation = Quaternion.identity;
        visualRoot.localScale = Vector3.one;
        return visualRoot;
    }

    private static Transform GetSharedBusVisualMotionRoot(Transform busRoot)
    {
        return busRoot != null ? busRoot.Find(SharedBusVisualMotionRootName) : null;
    }

    private void ApplySharedBusMotionAnimation(Transform busRoot, float speed01, bool moving, float phase, float turnLean = 0f)
    {
        Transform visualRoot = GetSharedBusVisualMotionRoot(busRoot);
        if (visualRoot == null)
        {
            return;
        }

        float speed = Mathf.Clamp01(speed01);
        float activity = moving ? Mathf.Max(0.22f, speed) : 0.08f;
        float time = Time.time;
        float bounce = moving
            ? (Mathf.Sin(time * 6.4f + phase) * 0.024f + Mathf.Sin(time * 12.8f + phase * 0.7f) * 0.006f) * activity
            : Mathf.Sin(time * 1.45f + phase) * 0.004f;
        float pitch = moving
            ? Mathf.Sin(time * 4.8f + phase) * 1.25f * activity
            : Mathf.Sin(time * 1.2f + phase) * 0.22f;
        float roll = moving
            ? (Mathf.Sin(time * 5.7f + phase * 1.3f) * 0.85f * activity) - turnLean * 0.35f
            : Mathf.Sin(time * 1.1f + phase) * 0.16f;

        visualRoot.localPosition = new Vector3(0f, bounce, 0f);
        visualRoot.localRotation = Quaternion.Euler(pitch, 0f, roll);
        SpinSharedBusWheels(visualRoot, speed, moving, phase);
    }

    private void SpinSharedBusWheels(Transform visualRoot, float speed01, bool moving, float phase)
    {
        Transform[] transforms = visualRoot.GetComponentsInChildren<Transform>(true);
        float spin = moving
            ? Time.time * Mathf.Lerp(180f, 760f, Mathf.Clamp01(speed01)) + phase * 21f
            : Mathf.Sin(Time.time * 1.2f + phase) * 2f;
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform wheel = transforms[i];
            if (wheel == null)
            {
                continue;
            }

            bool imported = wheel.name.StartsWith("ImportedBusWheelPivot_", System.StringComparison.Ordinal);
            bool procedural = wheel.name.StartsWith("SharedBusWheel_", System.StringComparison.Ordinal);
            if (imported || procedural)
            {
                ApplyVehicleWheelSpin(wheel, spin);
            }
        }
    }

    private void BuildProceduralSharedBusVisual(
        Transform parent,
        Color bodyColor,
        string leftLightName,
        string rightLightName,
        out Renderer headlightLeftRenderer,
        out Renderer headlightRightRenderer,
        out Material headlightLeftMaterial,
        out Material headlightRightMaterial,
        out Light leftLight,
        out Light rightLight)
    {
        Color roofColor = new(0.94f, 0.92f, 0.84f);
        Color windowColor = new(0.72f, 0.88f, 0.95f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(parent, false);
        body.transform.localPosition = new Vector3(0f, 0.26f, 0f);
        body.transform.localScale = new Vector3(1.24f, 0.42f, 0.44f);
        ApplyColor(body, bodyColor, VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(body, VisualSmoothnessVehicleMetal);

        GameObject lowerBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lowerBody.transform.SetParent(parent, false);
        lowerBody.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        lowerBody.transform.localScale = new Vector3(1.18f, 0.1f, 0.42f);
        ApplyColor(lowerBody, bodyColor * 0.72f, VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(lowerBody, VisualSmoothnessVehicleMetal);

        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.localPosition = new Vector3(-0.02f, 0.56f, 0f);
        roof.transform.localScale = new Vector3(1.02f, 0.12f, 0.4f);
        ApplyColor(roof, roofColor, VisualSmoothnessRoofMetal);
        ConfigureShadowVisual(roof, VisualSmoothnessRoofMetal);

        GameObject windowBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windowBand.transform.SetParent(parent, false);
        windowBand.transform.localPosition = new Vector3(-0.02f, 0.38f, 0f);
        windowBand.transform.localScale = new Vector3(0.94f, 0.18f, 0.46f);
        ApplyColor(windowBand, windowColor, VisualSmoothnessGlass);
        ConfigureShadowVisual(windowBand, VisualSmoothnessGlass);

        GameObject windshield = GameObject.CreatePrimitive(PrimitiveType.Cube);
        windshield.transform.SetParent(parent, false);
        windshield.transform.localPosition = new Vector3(0.56f, 0.41f, 0f);
        windshield.transform.localScale = new Vector3(0.12f, 0.2f, 0.38f);
        ApplyColor(windshield, new Color(0.66f, 0.86f, 0.94f), VisualSmoothnessGlass);
        ConfigureShadowVisual(windshield, VisualSmoothnessGlass);

        GameObject rearWindow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rearWindow.transform.SetParent(parent, false);
        rearWindow.transform.localPosition = new Vector3(-0.56f, 0.39f, 0f);
        rearWindow.transform.localScale = new Vector3(0.08f, 0.17f, 0.34f);
        ApplyColor(rearWindow, new Color(0.66f, 0.84f, 0.92f), VisualSmoothnessGlass);
        ConfigureShadowVisual(rearWindow, VisualSmoothnessGlass);

        GameObject headlightLeftVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlightLeftVisual.transform.SetParent(parent, false);
        headlightLeftVisual.transform.localPosition = new Vector3(0.61f, 0.26f, -0.14f);
        headlightLeftVisual.transform.localScale = new Vector3(0.04f, 0.06f, 0.08f);
        ApplyColor(headlightLeftVisual, new Color(0.34f, 0.3f, 0.22f), VisualSmoothnessGlass);
        ConfigureShadowVisual(headlightLeftVisual, VisualSmoothnessGlass);

        GameObject headlightRightVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        headlightRightVisual.transform.SetParent(parent, false);
        headlightRightVisual.transform.localPosition = new Vector3(0.61f, 0.26f, 0.14f);
        headlightRightVisual.transform.localScale = new Vector3(0.04f, 0.06f, 0.08f);
        ApplyColor(headlightRightVisual, new Color(0.34f, 0.3f, 0.22f), VisualSmoothnessGlass);
        ConfigureShadowVisual(headlightRightVisual, VisualSmoothnessGlass);

        GameObject sideStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sideStripe.transform.SetParent(parent, false);
        sideStripe.transform.localPosition = new Vector3(0f, 0.23f, 0f);
        sideStripe.transform.localScale = new Vector3(1.08f, 0.06f, 0.47f);
        ApplyColor(sideStripe, new Color(0.98f, 0.86f, 0.2f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(sideStripe, VisualSmoothnessVehicleMetal);

        GameObject roofStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roofStripe.transform.SetParent(parent, false);
        roofStripe.transform.localPosition = new Vector3(-0.02f, 0.64f, 0f);
        roofStripe.transform.localScale = new Vector3(1.08f, 0.03f, 0.42f);
        ApplyColor(roofStripe, new Color(0.98f, 0.9f, 0.7f), VisualSmoothnessRoofMetal);
        ConfigureShadowVisual(roofStripe, VisualSmoothnessRoofMetal);

        GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.transform.SetParent(parent, false);
        door.transform.localPosition = new Vector3(0.18f, 0.23f, -0.22f);
        door.transform.localScale = new Vector3(0.24f, 0.32f, 0.05f);
        ApplyColor(door, new Color(0.92f, 0.94f, 0.98f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(door, VisualSmoothnessVehicleMetal);

        GameObject shadowBlob = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shadowBlob.transform.SetParent(parent, false);
        shadowBlob.transform.localPosition = new Vector3(0f, -0.01f, 0f);
        shadowBlob.transform.localScale = new Vector3(1.28f, 0.008f, 0.52f);
        Renderer shadowRenderer = shadowBlob.GetComponent<Renderer>();
        shadowRenderer.material = CreateTransparentOverlayMaterial(new Color(0f, 0f, 0f, 0.14f));
        shadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
        shadowRenderer.receiveShadows = false;
        if (shadowBlob.TryGetComponent(out Collider shadowCollider))
        {
            Object.Destroy(shadowCollider);
        }

        float[] wheelX = { -0.38f, 0.38f };
        float[] wheelZ = { -0.18f, 0.18f };
        int wheelIndex = 0;
        foreach (float wx in wheelX)
        {
            foreach (float wz in wheelZ)
            {
                GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                wheel.name = "SharedBusWheel_" + wheelIndex++;
                wheel.transform.SetParent(parent, false);
                wheel.transform.localPosition = new Vector3(wx, 0.1f, wz);
                wheel.transform.localRotation = SharedBusProceduralWheelBaseRotation;
                wheel.transform.localScale = new Vector3(0.1f, 0.05f, 0.1f);
                ApplyColor(wheel, new Color(0.12f, 0.12f, 0.12f), VisualSmoothnessRubber);
                ConfigureShadowVisual(wheel, VisualSmoothnessRubber);
                ConfigureVehicleWheelSpin(wheel.transform, Vector3.up);
            }
        }

        GameObject routePlate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        routePlate.transform.SetParent(parent, false);
        routePlate.transform.localPosition = new Vector3(0.48f, 0.53f, 0f);
        routePlate.transform.localScale = new Vector3(0.18f, 0.08f, 0.3f);
        ApplyColor(routePlate, new Color(0.98f, 0.84f, 0.14f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(routePlate, VisualSmoothnessVehicleMetal);

        GameObject leftLightObject = new(leftLightName);
        leftLightObject.transform.SetParent(parent, false);
        leftLightObject.transform.localPosition = new Vector3(0.64f, 0.28f, -0.14f);
        leftLightObject.transform.localRotation = Quaternion.Euler(8f, 90f, 0f);
        leftLight = leftLightObject.AddComponent<Light>();
        leftLight.type = LightType.Spot;
        leftLight.color = new Color(1f, 0.66f, 0.34f);
        leftLight.range = 3.6f;
        leftLight.spotAngle = 42f;
        leftLight.innerSpotAngle = 22f;
        leftLight.intensity = 0f;
        leftLight.shadows = LightShadows.None;
        leftLight.enabled = false;

        GameObject rightLightObject = new(rightLightName);
        rightLightObject.transform.SetParent(parent, false);
        rightLightObject.transform.localPosition = new Vector3(0.64f, 0.28f, 0.14f);
        rightLightObject.transform.localRotation = Quaternion.Euler(8f, 90f, 0f);
        rightLight = rightLightObject.AddComponent<Light>();
        rightLight.type = LightType.Spot;
        rightLight.color = new Color(1f, 0.66f, 0.34f);
        rightLight.range = 3.6f;
        rightLight.spotAngle = 42f;
        rightLight.innerSpotAngle = 22f;
        rightLight.intensity = 0f;
        rightLight.shadows = LightShadows.None;
        rightLight.enabled = false;

        headlightLeftRenderer = headlightLeftVisual.GetComponent<Renderer>();
        headlightRightRenderer = headlightRightVisual.GetComponent<Renderer>();
        headlightLeftMaterial = headlightLeftRenderer != null ? headlightLeftRenderer.material : null;
        headlightRightMaterial = headlightRightRenderer != null ? headlightRightRenderer.material : null;
    }

    private void CreateEdgeHighwayBus(float travelDirection, bool isCitySideLane)
    {
        if (edgeHighwayBusRoot == null)
        {
            return;
        }

        float spawnX = travelDirection > 0f ? -1.6f : GridWidth + 1.6f;
        float laneZ = GetEdgeHighwayBusLaneWorldZ(isCitySideLane);
        GameObject busRoot = new($"EdgeHighwayBus_{edgeHighwayBuses.Count + 1}");
        busRoot.transform.SetParent(edgeHighwayBusRoot, false);

        Color bodyColor = Random.value < 0.5f
            ? new Color(0.9f, 0.26f, 0.22f)
            : new Color(0.24f, 0.5f, 0.86f);
        BuildSharedBusVisual(
            busRoot.transform,
            bodyColor,
            "BusHeadlightLeft",
            "BusHeadlightRight",
            out Renderer headlightLeftRenderer,
            out Renderer headlightRightRenderer,
            out Material headlightLeftMaterial,
            out Material headlightRightMaterial,
            out Light leftLight,
            out Light rightLight);

        float y = SampleTerrainHeight(spawnX, laneZ) + RoadHeight + EdgeHighwayBusLift;
        busRoot.transform.position = new Vector3(spawnX, y, laneZ);
        busRoot.transform.rotation = Quaternion.LookRotation(travelDirection > 0f ? Vector3.right : Vector3.left, Vector3.up);

        edgeHighwayBuses.Add(new EdgeHighwayBusData
        {
            RootTransform = busRoot.transform,
            WorldX = spawnX,
            TravelDirection = travelDirection,
            IsCitySideLane = isCitySideLane,
            Speed = EdgeHighwayBusSpeed * Random.Range(0.92f, 1.08f),
            BobPhase = Random.Range(0f, 10f),
            BodyColor = bodyColor,
            HasPlayedPassbyAudio = false,
            HasEnteredRoadStrip = false,
            HeadlightLeftRenderer = headlightLeftRenderer,
            HeadlightRightRenderer = headlightRightRenderer,
            HeadlightLeftMaterial = headlightLeftMaterial,
            HeadlightRightMaterial = headlightRightMaterial,
            HeadlightLeft = leftLight,
            HeadlightRight = rightLight
        });
    }

    private void CreateHiringArrivalBusVisual()
    {
        if (hiringDriverArrival == null || edgeHighwayBusRoot == null)
        {
            return;
        }

        float spawnX = -1.6f;
        float laneZ = GetEdgeHighwayBusLaneWorldZ(isCitySideLane: false);
        GameObject busRoot = new($"HiringBus_{hiringDriverArrival.Driver?.DriverId ?? 0}");
        busRoot.transform.SetParent(edgeHighwayBusRoot, false);

        Color bodyColor = new(0.28f, 0.58f, 0.9f);
        BuildSharedBusVisual(
            busRoot.transform,
            bodyColor,
            "HiringBusHeadlightLeft",
            "HiringBusHeadlightRight",
            out Renderer headlightLeftRenderer,
            out Renderer headlightRightRenderer,
            out Material headlightLeftMaterial,
            out Material headlightRightMaterial,
            out Light leftLight,
            out Light rightLight);

        hiringDriverArrival.BusRootTransform = busRoot.transform;
        hiringDriverArrival.HeadlightLeftRenderer = headlightLeftRenderer;
        hiringDriverArrival.HeadlightRightRenderer = headlightRightRenderer;
        hiringDriverArrival.HeadlightLeftMaterial = headlightLeftMaterial;
        hiringDriverArrival.HeadlightRightMaterial = headlightRightMaterial;
        hiringDriverArrival.HeadlightLeft = leftLight;
        hiringDriverArrival.HeadlightRight = rightLight;
        hiringDriverArrival.BusWorldX = spawnX;
        hiringDriverArrival.BusSpeed = EdgeHighwayBusSpeed * 0.92f;
        hiringDriverArrival.BobPhase = Random.Range(0f, 10f);
        UpdateHiringBusTransform();
    }

    private void UpdateHiringBusTransform()
    {
        if (hiringDriverArrival == null || hiringDriverArrival.BusRootTransform == null)
        {
            return;
        }

        float laneZ = GetEdgeHighwayBusLaneWorldZ(isCitySideLane: false);
        float y = SampleTerrainHeight(hiringDriverArrival.BusWorldX, laneZ) + RoadHeight + EdgeHighwayBusLift;
        hiringDriverArrival.BusRootTransform.position = new Vector3(hiringDriverArrival.BusWorldX, y, laneZ);
        hiringDriverArrival.BusRootTransform.rotation = Quaternion.identity;
        bool moving = hiringDriverArrival.Phase == HiringDriverArrivalPhase.ApproachingStop ||
            hiringDriverArrival.Phase == HiringDriverArrivalPhase.Departing;
        ApplySharedBusMotionAnimation(
            hiringDriverArrival.BusRootTransform,
            hiringDriverArrival.BusSpeed / Mathf.Max(0.01f, EdgeHighwayBusSpeed),
            moving,
            hiringDriverArrival.BobPhase);

        float darkness = 1f - currentStylizedDaylight;
        bool headlightsOn = darkness > 0.55f;
        float headlightIntensity = headlightsOn ? Mathf.Lerp(0.4f, 1.75f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
        Color lampColor = Color.Lerp(
            new Color(0.36f, 0.22f, 0.10f),
            new Color(1f, 0.68f, 0.36f),
            Mathf.Clamp01(headlightIntensity / 1.75f));

        if (hiringDriverArrival.HeadlightLeft != null)
        {
            hiringDriverArrival.HeadlightLeft.enabled = headlightsOn;
            hiringDriverArrival.HeadlightLeft.intensity = headlightIntensity;
        }

        if (hiringDriverArrival.HeadlightRight != null)
        {
            hiringDriverArrival.HeadlightRight.enabled = headlightsOn;
            hiringDriverArrival.HeadlightRight.intensity = headlightIntensity;
        }

        if (hiringDriverArrival.HeadlightLeftMaterial != null)
        {
            hiringDriverArrival.HeadlightLeftMaterial.color = lampColor;
        }

        if (hiringDriverArrival.HeadlightRightMaterial != null)
        {
            hiringDriverArrival.HeadlightRightMaterial.color = lampColor;
        }
    }

    private float GetEdgeHighwayBusLaneWorldZ(bool isCitySideLane)
    {
        float centerZ = 1f;
        return centerZ + (isCitySideLane ? EdgeHighwayBusLaneOffset : -EdgeHighwayBusLaneOffset);
    }

}
