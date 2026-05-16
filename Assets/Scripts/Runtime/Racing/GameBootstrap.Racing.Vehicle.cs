using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap : MonoBehaviour
{
    private void CreateRacingTruck()
    {
        racingTruckVisual = new GameObject("RacingTruck");
        racingTruckVisual.transform.localScale = Vector3.one * 1.6f;

        Color bodyColor   = new Color(0.85f, 0.20f, 0.18f);
        Color cabinColor  = new Color(0.95f, 0.82f, 0.28f);
        Color wheelColor  = new Color(0.14f, 0.14f, 0.14f);

        // в”Ђв”Ђ BodyGroup вЂ” both body cubes roll together (suspension) в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        // Wheels are outside this group so they stay flat.
        GameObject bodyGroupObj = new("BodyGroup");
        bodyGroupObj.transform.SetParent(racingTruckVisual.transform, false);
        bodyGroupObj.transform.localPosition = Vector3.zero;
        racingBodyGroup = bodyGroupObj.transform;

        // Red cargo body вЂ” child of BodyGroup
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.transform.SetParent(racingBodyGroup, false);
        body.transform.localPosition = new Vector3(0f, 0.22f, 0f);
        body.transform.localScale    = new Vector3(0.72f, 0.30f, 1.0f);
        ApplyColor(body, bodyColor);
        ConfigureShadowVisual(body);

        // CabinPivot inside BodyGroup вЂ” receives FWD delta Y so cabin steers ahead
        GameObject cabinPivotObj = new("CabinPivot");
        cabinPivotObj.transform.SetParent(racingBodyGroup, false);
        cabinPivotObj.transform.localPosition = Vector3.zero;
        racingCabinGroup = cabinPivotObj.transform;

        // Yellow cabin вЂ” child of CabinPivot (rolls with body, steers ahead)
        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.transform.SetParent(racingCabinGroup, false);
        cabin.transform.localPosition = new Vector3(0f, 0.40f, 0.20f);
        cabin.transform.localScale    = new Vector3(0.58f, 0.34f, 0.44f);
        ApplyColor(cabin, cabinColor);
        ConfigureShadowVisual(cabin);

        // в”Ђв”Ђ FrontAxle вЂ” front wheels + headlights, steers for FWD visual в”Ђ
        // NOT a parent of body parts, so steering here doesn't tilt the body
        GameObject frontObj = new("FrontAxle");
        frontObj.transform.SetParent(racingTruckVisual.transform, false);
        frontObj.transform.localPosition = Vector3.zero;
        racingFrontAssembly = frontObj.transform;

        racingTruckWheelFL = CreateRacingWheel(racingFrontAssembly, new Vector3(-0.40f, 0.12f,  0.32f), wheelColor);
        racingTruckWheelFR = CreateRacingWheel(racingFrontAssembly, new Vector3( 0.40f, 0.12f,  0.32f), wheelColor);

        // Headlights on FrontAxle вЂ” point forward regardless of body roll
        racingHeadlightL = CreateRacingHeadlight(racingFrontAssembly, new Vector3(-0.28f, 0.28f, 0.52f));
        racingHeadlightR = CreateRacingHeadlight(racingFrontAssembly, new Vector3( 0.28f, 0.28f, 0.52f));

        racingBrakeLightLeftRenderer = CreateRacingBrakeLight(racingBodyGroup, new Vector3(-0.28f, 0.34f, -0.53f), out racingBrakeLightLeftMaterial);
        racingBrakeLightRightRenderer = CreateRacingBrakeLight(racingBodyGroup, new Vector3(0.28f, 0.34f, -0.53f), out racingBrakeLightRightMaterial);

        // в”Ђв”Ђ Rear wheels вЂ” on root, flat в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
        racingTruckWheelRL = CreateRacingWheel(racingTruckVisual.transform, new Vector3(-0.40f, 0.12f, -0.32f), wheelColor);
        racingTruckWheelRR = CreateRacingWheel(racingTruckVisual.transform, new Vector3( 0.40f, 0.12f, -0.32f), wheelColor);
    }

    private Renderer CreateRacingBrakeLight(Transform parent, Vector3 localPos, out Material material)
    {
        GameObject light = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(light.GetComponent<Collider>());
        light.name = "BrakeLight";
        light.transform.SetParent(parent, false);
        light.transform.localPosition = localPos;
        light.transform.localScale = new Vector3(0.16f, 0.08f, 0.03f);

        Renderer renderer = light.GetComponent<Renderer>();
        Shader unlitShader = ShaderRefs.Unlit ?? ShaderRefs.Sprites;
        material = unlitShader != null ? new Material(unlitShader) : renderer.material;
        material.color = new Color(0.35f, 0.02f, 0.01f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", material.color);
        renderer.material = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return renderer;
    }

    private Light CreateRacingHeadlight(Transform parent, Vector3 localPos)
    {
        GameObject go = new("RacingHeadlight");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(5f, 0f, 0f);

        // Visible lens вЂ” unlit bright white disc so it's always visible as a glowing element
        GameObject lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(lens.GetComponent<Collider>());
        lens.name = "HeadlightLens";
        lens.transform.SetParent(go.transform, false);
        lens.transform.localPosition = Vector3.zero;
        lens.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // face forward
        lens.transform.localScale    = new Vector3(0.10f, 0.015f, 0.10f);
        Renderer lensR = lens.GetComponent<Renderer>();
        Shader unlitShader = ShaderRefs.Unlit ?? ShaderRefs.Sprites;
        Material lensMat = unlitShader != null ? new Material(unlitShader) : lensR.material;
        lensMat.color = new Color(1f, 0.78f, 0.46f);
        if (lensMat.HasProperty("_BaseColor")) lensMat.SetColor("_BaseColor", lensMat.color);
        lensR.material = lensMat;
        lensR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Actual spot light
        Light l = go.AddComponent<Light>();
        l.type      = LightType.Spot;
        l.spotAngle = 55f;
        l.innerSpotAngle = 22f;
        l.range     = 32f;
        l.color     = new Color(1f, 0.66f, 0.32f);
        l.intensity = 1.2f;
        l.shadows   = LightShadows.Soft;
        l.enabled   = true;
        return l;
    }

    private Transform CreateRacingWheel(Transform parent, Vector3 localPos, Color color)
    {
        GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.transform.SetParent(parent, false);
        wheel.transform.localPosition = localPos;
        // Euler(0, 0, -90): cylinder axis (local Y) points along world X (left-right)
        // flat circles face sideways, curved surface rolls forward/back correctly
        wheel.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        // x = z = radius scale (0.28 в†’ diameter 0.28), y = half-thickness (0.065 в†’ 0.13 wide)
        wheel.transform.localScale    = new Vector3(0.28f, 0.065f, 0.28f);
        ApplyColor(wheel, color);
        ConfigureShadowVisual(wheel);
        return wheel.transform;
    }

    private void UpdateRacingBrakeLights(bool braking, float speed)
    {
        float intensity = braking && speed > 0.6f ? 1f : 0f;
        Color color = Color.Lerp(new Color(0.35f, 0.02f, 0.01f), new Color(1f, 0.04f, 0.015f), intensity);
        if (racingBrakeLightLeftMaterial != null)
        {
            racingBrakeLightLeftMaterial.color = color;
            if (racingBrakeLightLeftMaterial.HasProperty("_BaseColor")) racingBrakeLightLeftMaterial.SetColor("_BaseColor", color);
        }
        if (racingBrakeLightRightMaterial != null)
        {
            racingBrakeLightRightMaterial.color = color;
            if (racingBrakeLightRightMaterial.HasProperty("_BaseColor")) racingBrakeLightRightMaterial.SetColor("_BaseColor", color);
        }
    }

    private void UpdateRacingTrailParticles(float dt, float speed, bool braking)
    {
        for (int i = racingTrailParticles.Count - 1; i >= 0; i--)
        {
            RacingTrailParticleData p = racingTrailParticles[i];
            if (p.Root == null)
            {
                racingTrailParticles.RemoveAt(i);
                continue;
            }

            p.Life -= dt;
            if (p.Life <= 0f)
            {
                Object.Destroy(p.Root.gameObject);
                racingTrailParticles.RemoveAt(i);
                continue;
            }

            float t = 1f - Mathf.Clamp01(p.Life / p.MaxLife);
            p.Root.position += p.Velocity * dt;
            float scale = Mathf.Lerp(p.StartScale, p.StartScale * 2.6f, t);
            p.Root.localScale = Vector3.one * scale;
            if (p.Material != null)
            {
                Color c = p.Material.color;
                c.a = Mathf.Lerp(0.24f, 0f, t);
                p.Material.color = c;
                if (p.Material.HasProperty("_BaseColor")) p.Material.SetColor("_BaseColor", c);
            }

            racingTrailParticles[i] = p;
        }

        if (racingSceneRoot == null || speed < 1.4f)
        {
            return;
        }

        racingTrailEmitTimer -= dt;
        float emitInterval = braking ? 0.035f : Mathf.Lerp(0.12f, 0.055f, Mathf.Clamp01(speed / RacingMaxSpeed));
        if (racingTrailEmitTimer > 0f)
        {
            return;
        }

        racingTrailEmitTimer = emitInterval;
        float rad = racingTruckAngle * Mathf.Deg2Rad;
        Vector3 forward = new(Mathf.Sin(rad), 0f, Mathf.Cos(rad));
        Vector3 right = new(forward.z, 0f, -forward.x);
        Vector3 basePos = racingTruckPos - forward * 0.86f + Vector3.up * 0.08f;
        SpawnRacingTrailParticle(basePos + right * 0.42f, -forward, speed, braking);
        SpawnRacingTrailParticle(basePos - right * 0.42f, -forward, speed, braking);
        if (speed > RacingMaxSpeed * 0.65f)
        {
            SpawnRacingTrailParticle(racingTruckPos - forward * 0.52f + Vector3.up * 0.42f, -forward + Vector3.up * 0.28f, speed, false);
        }
    }

    private void SpawnRacingTrailParticle(Vector3 position, Vector3 driftDir, float speed, bool braking)
    {
        GameObject particle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        particle.name = braking ? "RacingBrakeDust" : "RacingTrailDust";
        particle.transform.SetParent(racingSceneRoot.transform, false);
        particle.transform.position = position + new Vector3(Random.Range(-0.06f, 0.06f), 0f, Random.Range(-0.06f, 0.06f));
        float startScale = braking ? Random.Range(0.14f, 0.22f) : Random.Range(0.09f, 0.15f);
        particle.transform.localScale = Vector3.one * startScale;
        Renderer renderer = particle.GetComponent<Renderer>();
        Color color = braking
            ? new Color(0.76f, 0.64f, 0.48f, 0.24f)
            : new Color(0.62f, 0.58f, 0.50f, 0.18f);
        Material material = CreateTransparentOverlayMaterial(color);
        renderer.material = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (particle.TryGetComponent(out Collider collider))
        {
            Object.Destroy(collider);
        }

        Vector3 randomSide = new(Random.Range(-0.18f, 0.18f), Random.Range(0.06f, 0.22f), Random.Range(-0.18f, 0.18f));
        racingTrailParticles.Add(new RacingTrailParticleData
        {
            Root = particle.transform,
            Material = material,
            Velocity = driftDir.normalized * Mathf.Lerp(0.35f, 1.1f, Mathf.Clamp01(speed / RacingMaxSpeed)) + randomSide,
            Life = braking ? 0.62f : 0.48f,
            MaxLife = braking ? 0.62f : 0.48f,
            StartScale = startScale
        });
    }

    // в”Ђв”Ђ Racing camera в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void SetupRacingCamera()
    {
        GameObject camObj = new("RacingCamera");
        racingCamera = camObj.AddComponent<Camera>();
        racingCamera.orthographic    = false;
        racingCamera.fieldOfView     = 65f;
        racingCamera.clearFlags      = CameraClearFlags.SolidColor;
        racingCamera.backgroundColor = new Color(0.38f, 0.62f, 0.92f);
        racingCamera.depth           = mainCamera != null ? mainCamera.depth + 10 : 10;
        racingCamera.cullingMask     = ~0;
        racingCamera.nearClipPlane   = 0.1f;
        racingCamera.farClipPlane    = 600f;

        Quaternion initRot = Quaternion.Euler(14f, racingTruckAngle, 0f);
        racingCamera.transform.position = racingTruckPos + Vector3.up * 1.4f + initRot * Vector3.back * 2.8f;
        racingCamera.transform.rotation = initRot;

        // Directional light вЂ” provides ambient shadows for the whole race scene
        GameObject dirObj = new("RacingDirectionalLight");
        dirObj.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
        racingDirectionalLight = dirObj.AddComponent<Light>();
        racingDirectionalLight.type      = LightType.Directional;
        racingDirectionalLight.intensity = 1.0f;
        racingDirectionalLight.color     = new Color(1f, 0.90f, 0.72f);
        racingDirectionalLight.shadows   = LightShadows.Soft;

        // Expand shadow distance so the whole visible road receives shadows
        racingSavedShadowDistance          = QualitySettings.shadowDistance;
        QualitySettings.shadowDistance     = 120f;

        SetupRacingSkydome();
    }

    private static void NoShadow(GameObject go)
    {
        if (go.TryGetComponent(out Renderer r))
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows    = false;
        }
    }

    // в”Ђв”Ђ 3D Steering wheel вЂ” child of racing camera в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ
    //
    // The wheel is parented directly to racingCamera so it stays fixed
    // in screen space (bottom-centre) like a dashboard prop.
    // It lies flat in the camera's local XZ plane, spinning around local Y.

    private void CreateSteeringWheel()
    {
        if (racingCamera == null) return;

        // Anchor: child of camera вЂ” lower and more tilted forward so it's below the truck visual
        // X-tilt of -55В° means the wheel face tilts toward the player (dashboard angle)
        GameObject anchor = new("SteeringWheelAnchor");
        anchor.transform.SetParent(racingCamera.transform, false);
        anchor.transform.localPosition = new Vector3(0f, -0.80f, 1.1f); // lower, slightly closer
        anchor.transform.localRotation = Quaternion.Euler(-55f, 0f, 0f); // steep forward tilt

        // Dedicated backlight so the wheel is always visible regardless of scene lighting
        GameObject lightObj = new("WheelBacklight");
        lightObj.transform.SetParent(anchor.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.6f, 0f); // above the wheel face
        Light wl = lightObj.AddComponent<Light>();
        wl.type      = LightType.Point;
        wl.intensity = 2.2f;
        wl.range     = 1.4f;
        wl.color     = new Color(1f, 0.68f, 0.36f); // warm dashboard glow

        // Spinner: child of anchor вЂ” this is what rotates each frame
        racingSteeringWheelRoot = new("SteeringWheelRoot");
        racingSteeringWheelRoot.transform.SetParent(anchor.transform, false);
        racingSteeringWheelRoot.transform.localPosition = Vector3.zero;
        racingSteeringWheelRoot.transform.localRotation = Quaternion.identity;

        Color hubColor   = new Color(0.30f, 0.28f, 0.32f);
        Color spokeColor = new Color(0.25f, 0.23f, 0.26f);
        Color rimColor   = new Color(0.22f, 0.20f, 0.24f);
        Transform root   = racingSteeringWheelRoot.transform;
        float rimRadius  = 0.30f;

        // Yellow dot at top of rim вЂ” moves WITH the wheel, shows rotation amount
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(marker.GetComponent<Collider>());
        marker.name = "WheelCenterMarker";
        marker.transform.SetParent(root, false);
        marker.transform.localPosition = new Vector3(0f, 0.022f, rimRadius);
        marker.transform.localRotation = Quaternion.identity;
        marker.transform.localScale    = new Vector3(0.042f, 0.022f, 0.028f);
        NoShadow(marker);
        ApplyColor(marker, new Color(1f, 0.88f, 0.08f));

        // Hub вЂ” flat cylinder, lies in XZ plane (Y is the face normal)
        GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(hub.GetComponent<Collider>());
        hub.name = "WheelHub";
        hub.transform.SetParent(root, false);
        hub.transform.localPosition = Vector3.zero;
        hub.transform.localScale    = new Vector3(0.10f, 0.012f, 0.10f);
        NoShadow(hub);
        ApplyColor(hub, hubColor);

        // 3 spokes вЂ” radiate from hub edge outward to rim, 120В° apart
        // Each spoke's LOCAL position must be computed along its own direction,
        // otherwise all three shift to the same offset in parent space.
        float spokeLen    = rimRadius - 0.06f;        // 0.24 units long
        float spokeCentre = 0.06f + spokeLen * 0.5f; // 0.18: midpoint along spoke axis
        for (int i = 0; i < 3; i++)
        {
            float rad = i * 120f * Mathf.Deg2Rad;
            // Centre of this spoke in root-local XZ space
            float sx = Mathf.Sin(rad) * spokeCentre;
            float sz = Mathf.Cos(rad) * spokeCentre;

            GameObject spoke = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(spoke.GetComponent<Collider>());
            spoke.name = $"Spoke{i}";
            spoke.transform.SetParent(root, false);
            spoke.transform.localPosition = new Vector3(sx, 0f, sz);
            spoke.transform.localRotation = Quaternion.Euler(0f, i * 120f, 0f);
            spoke.transform.localScale    = new Vector3(0.040f, 0.014f, spokeLen);
            NoShadow(spoke);
            ApplyColor(spoke, spokeColor);
        }

        // Outer rim вЂ” 8 flat bars forming a connected octagon
        // Each bar is placed at the midpoint of its chord and rotated +90В° so its
        // long axis (local Z) is tangential to the circle, not radial.
        int   rimCount  = 8;
        // chord length between adjacent vertices: 2rВ·sin(ПЂ/n), +6% overlap for clean joins
        float chordLen  = 2f * rimRadius * Mathf.Sin(Mathf.PI / rimCount) * 1.06f;
        for (int i = 0; i < rimCount; i++)
        {
            float midA = (i + 0.5f) / rimCount * Mathf.PI * 2f; // angle at mid-chord
            float mx   = Mathf.Sin(midA) * rimRadius;
            float mz   = Mathf.Cos(midA) * rimRadius;

            GameObject seg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(seg.GetComponent<Collider>());
            seg.name = $"Rim{i}";
            seg.transform.SetParent(root, false);
            seg.transform.localPosition = new Vector3(mx, 0f, mz);
            // +90В° makes local Z tangential (along the rim edge) instead of radial
            seg.transform.localRotation = Quaternion.Euler(0f, midA * Mathf.Rad2Deg + 90f, 0f);
            seg.transform.localScale    = new Vector3(0.055f, 0.018f, chordLen);
            NoShadow(seg);
            ApplyColor(seg, rimColor);
        }

        racingWheelAngle = 0f;
    }

    // в”Ђв”Ђ Pedals в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void CreateRacingPedals()
    {
        if (racingCamera == null) return;

        // Anchor: left of the steering wheel, raised so it stays inside camera frustum
        // Rule: |y/z| < tan(FOV/2=32.5В°)=0.637  в†’  y must be > -0.637*z
        // With z=1.0: y must be > -0.637, so -0.55 is safe
        GameObject anchor = new("PedalAnchor");
        anchor.transform.SetParent(racingCamera.transform, false);
        anchor.transform.localPosition = new Vector3(-0.54f, -0.55f, 1.0f);
        anchor.transform.localRotation = Quaternion.Euler(-55f, 5f, 0f); // same tilt as steering wheel

        // Shared backlight
        GameObject lightObj = new("PedalLight");
        lightObj.transform.SetParent(anchor.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        Light pl = lightObj.AddComponent<Light>();
        pl.type      = LightType.Point;
        pl.intensity = 1.8f;
        pl.range     = 1.2f;
        pl.color     = new Color(1f, 0.68f, 0.36f);

        // Brake (left) вЂ” red tint
        racingPedalBrake = CreateSinglePedal("BrakePedal", anchor.transform,
            new Vector3(-0.13f, 0f, 0f), new Color(0.55f, 0.12f, 0.12f));

        // Gas (right) вЂ” green tint
        racingPedalGas = CreateSinglePedal("GasPedal", anchor.transform,
            new Vector3(0.13f, 0f, 0f), new Color(0.14f, 0.44f, 0.14f));
    }

    private Transform CreateSinglePedal(string pedName, Transform parent, Vector3 offset, Color surfaceColor)
    {
        Color stemColor = new Color(0.18f, 0.18f, 0.20f);

        GameObject root = new(pedName);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = offset;
        root.transform.localRotation = Quaternion.identity;

        // Stem вЂ” thin vertical post
        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(stem.GetComponent<Collider>());
        stem.name = "Stem";
        stem.transform.SetParent(root.transform, false);
        stem.transform.localPosition = new Vector3(0f, -0.08f, -0.02f);
        stem.transform.localScale    = new Vector3(0.032f, 0.14f, 0.032f);
        NoShadow(stem);
        ApplyColor(stem, stemColor);

        // Surface вЂ” flat pedal plate
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(surface.GetComponent<Collider>());
        surface.name = "Surface";
        surface.transform.SetParent(root.transform, false);
        surface.transform.localPosition = new Vector3(0f, 0f, 0f);
        surface.transform.localScale    = new Vector3(0.11f, 0.018f, 0.15f);
        NoShadow(surface);
        ApplyColor(surface, surfaceColor);

        return root.transform;
    }

    private void UpdatePedals(float dt, float throttle, bool braking)
    {
        UpdateSinglePedal(racingPedalGas,   dt, throttle > 0.05f);
        UpdateSinglePedal(racingPedalBrake, dt, braking);
    }

    private void UpdateSinglePedal(Transform pedal, float dt, bool pressed)
    {
        if (pedal == null) return;
        float target = pressed ? 18f : 0f;
        float cur = pedal.localEulerAngles.x;
        if (cur > 180f) cur -= 360f;
        pedal.localRotation = Quaternion.Euler(Mathf.Lerp(cur, target, 14f * dt), 0f, 0f);
    }

    // в”Ђв”Ђ Gear shift в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

    private void CreateGearShift()
    {
        if (racingCamera == null) return;

        // Mirror of pedal anchor вЂ” right side of steering wheel
        GameObject anchor = new("GearShiftAnchor");
        anchor.transform.SetParent(racingCamera.transform, false);
        anchor.transform.localPosition = new Vector3(0.54f, -0.55f, 1.0f);
        anchor.transform.localRotation = Quaternion.Euler(-55f, -5f, 0f);

        // Backlight
        GameObject lightObj = new("GearLight");
        lightObj.transform.SetParent(anchor.transform, false);
        lightObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        Light gl = lightObj.AddComponent<Light>();
        gl.type      = LightType.Point;
        gl.intensity = 1.8f;
        gl.range     = 1.0f;
        gl.color     = new Color(1f, 0.68f, 0.36f);

        // Gear shift root вЂ” this tilts forward/backward
        racingGearShift = new GameObject("GearShiftRoot").transform;
        racingGearShift.SetParent(anchor.transform, false);
        racingGearShift.localPosition = Vector3.zero;
        racingGearShift.localRotation = Quaternion.identity;

        Color stickColor = new Color(0.20f, 0.20f, 0.22f);
        Color knobColor  = new Color(0.28f, 0.26f, 0.30f);

        // Stick вЂ” thin vertical rod
        GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(stick.GetComponent<Collider>());
        stick.name = "Stick";
        stick.transform.SetParent(racingGearShift, false);
        stick.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        stick.transform.localScale    = new Vector3(0.032f, 0.22f, 0.032f);
        NoShadow(stick);
        ApplyColor(stick, stickColor);

        // Knob вЂ” sphere-ish cube on top
        GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(knob.GetComponent<Collider>());
        knob.name = "Knob";
        knob.transform.SetParent(racingGearShift, false);
        knob.transform.localPosition = new Vector3(0f, 0.17f, 0f);
        knob.transform.localScale    = new Vector3(0.07f, 0.07f, 0.07f);
        NoShadow(knob);
        ApplyColor(knob, knobColor);

        // Gear label on knob вЂ” tiny flat cubes for "D" and "R" indicator
        // Forward indicator: small yellow stripe on front of knob
        GameObject fwdDot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.Destroy(fwdDot.GetComponent<Collider>());
        fwdDot.name = "GearForwardDot";
        fwdDot.transform.SetParent(racingGearShift, false);
        fwdDot.transform.localPosition = new Vector3(0f, 0.17f, 0.04f);
        fwdDot.transform.localScale    = new Vector3(0.022f, 0.022f, 0.008f);
        NoShadow(fwdDot);
        ApplyColor(fwdDot, new Color(1f, 0.88f, 0.08f)); // yellow = forward
    }

    private void UpdateGearShift(float dt)
    {
        if (racingGearShift == null) return;
        float target = GearShiftAngles[Mathf.Clamp(racingCurrentGear, 0, 4)];
        float cur = racingGearShift.localEulerAngles.x;
        if (cur > 180f) cur -= 360f;
        float snap = racingGearFlashTimer > 0f ? Mathf.Sin(Time.unscaledTime * 42f) * 2.5f : 0f;
        racingGearShift.localRotation = Quaternion.Euler(Mathf.Lerp(cur, target + snap, 9f * dt), 0f, 0f);
    }

    private void UpdateManualGearState(float fwdDot, float dt)
    {
        racingGearChangeTimer = Mathf.Max(0f, racingGearChangeTimer - dt);
        racingGearFlashTimer = Mathf.Max(0f, racingGearFlashTimer - dt);

        if (fwdDot < -0.2f)
        {
            racingCurrentGear = 0;
            return;
        }

        if (racingCurrentGear == 0)
        {
            racingCurrentGear = 1;
        }
    }

    private void TryShiftRacingGear(int direction)
    {
        if (direction == 0 || racingGearChangeTimer > 0f)
        {
            return;
        }

        int current = Mathf.Clamp(racingCurrentGear == 0 ? 1 : racingCurrentGear, 1, 4);
        int next = Mathf.Clamp(current + direction, 1, 4);
        if (next == current)
        {
            racingGearDragAccumY = 0f;
            return;
        }

        racingCurrentGear = next;
        racingGearChangeTimer = GearChangeCooldown;
        racingGearFlashTimer = 0.18f;
    }

    private void SetupRacingSkydome()
    {
        racingSkydome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        racingSkydome.name = "RacingSkydome";
        Object.Destroy(racingSkydome.GetComponent<Collider>());

        // Negative X scale flips winding order вЂ” renders from the inside
        racingSkydome.transform.localScale = new Vector3(-480f, 480f, 480f);

        racingSkydomeRenderer = racingSkydome.GetComponent<Renderer>();
        racingSkydomeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        racingSkydomeRenderer.receiveShadows    = false;

        // Unlit material so it isn't affected by scene lighting
        Shader unlitShader = ShaderRefs.Unlit ?? ShaderRefs.Sprites;
        Material mat = unlitShader != null
            ? new Material(unlitShader)
            : racingSkydomeRenderer.material;
        racingSkydomeRenderer.material = mat;
    }

    private void UpdateRacingSkydome()
    {
        if (racingSkydome == null || racingSkydomeRenderer == null) return;

        // Follow camera so the dome is always around us
        if (racingCamera != null)
            racingSkydome.transform.position = racingCamera.transform.position;

        // 4-stop sky gradient keyed to daylight
        //  0 = full night,  1 = full day
        float dl = Mathf.Clamp01(currentStylizedDaylight);

        Color nightColor   = new Color(0.03f, 0.04f, 0.09f);  // deep navy
        Color dawnColor    = new Color(0.72f, 0.32f, 0.12f);  // burnt orange
        Color morningColor = new Color(0.82f, 0.56f, 0.28f);  // warm gold
        Color dayColor     = new Color(0.38f, 0.62f, 0.92f);  // clear blue

        Color skyColor;
        if      (dl < 0.25f) skyColor = Color.Lerp(nightColor,   dawnColor,    dl / 0.25f);
        else if (dl < 0.55f) skyColor = Color.Lerp(dawnColor,    morningColor, (dl - 0.25f) / 0.30f);
        else                 skyColor = Color.Lerp(morningColor,  dayColor,     (dl - 0.55f) / 0.45f);

        racingSkydomeRenderer.material.color = skyColor;

        // Also tint camera background to match (visible if dome ever has gaps)
        if (racingCamera != null)
            racingCamera.backgroundColor = skyColor;

        // Directional light intensity follows daylight: night ~0.1, day ~1.1
        if (racingDirectionalLight != null)
        {
            racingDirectionalLight.intensity = Mathf.Lerp(0.10f, 1.10f, dl);
            // Shift color from cool night to warm day
            racingDirectionalLight.color = skyColor * 1.1f;
        }
    }

    // в”Ђв”Ђ Racing HUD в”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђв”Ђ

}
