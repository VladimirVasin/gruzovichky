using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const string AmbientSquirrelModelResourcePath = "Misc/squirel";
    private const string AmbientSquirrelModelResourcePathFallback = "Misc/squirrel";
    private const float AmbientSquirrelImportedTargetWidth = 0.24f;
    private const float AmbientSquirrelImportedTargetHeight = 0.28f;
    private const float AmbientSquirrelImportedTargetLength = 0.38f;

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

        GameObject sqRoot = new($"AmbientSquirrel_{squirrelIndex + 1}");
        sqRoot.transform.SetParent(ambientSquirrelRoot, false);

        bool usesImportedModel = TryCreateImportedAmbientSquirrelModel(
            sqRoot.transform,
            out Transform bodyTransform,
            out Transform headTransform,
            out Transform tailTransform,
            out Vector3 bodyBaseScale,
            out Quaternion headBaseRotation,
            out Quaternion tailBaseRotation,
            out Transform[] legTransforms,
            out Quaternion[] legBaseRotations);

        if (!usesImportedModel)
        {
            CreateProceduralAmbientSquirrelModel(
                sqRoot.transform,
                out bodyTransform,
                out headTransform,
                out tailTransform,
                out bodyBaseScale,
                out headBaseRotation,
                out tailBaseRotation,
                out legTransforms,
                out legBaseRotations);
        }

        int step = Mathf.Max(1, ambientSquirrelRoamPoints.Count / totalCount);
        int pointIndex = squirrelIndex * step % ambientSquirrelRoamPoints.Count;
        Vector3 position = ambientSquirrelRoamPoints[pointIndex];
        float yaw = Random.Range(0f, 360f);
        sqRoot.transform.position = position;
        sqRoot.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        ambientSquirrels.Add(new AmbientSquirrelData
        {
            RootTransform    = sqRoot.transform,
            BodyTransform    = bodyTransform,
            HeadTransform    = headTransform,
            TailTransform    = tailTransform,
            UsesImportedModel = usesImportedModel,
            BodyBaseScale    = bodyBaseScale,
            HeadBaseRotation = headBaseRotation,
            TailBaseRotation = tailBaseRotation,
            LegTransforms    = legTransforms,
            LegBaseRotations = legBaseRotations,
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

    private bool TryCreateImportedAmbientSquirrelModel(
        Transform squirrelRoot,
        out Transform bodyTransform,
        out Transform headTransform,
        out Transform tailTransform,
        out Vector3 bodyBaseScale,
        out Quaternion headBaseRotation,
        out Quaternion tailBaseRotation,
        out Transform[] legTransforms,
        out Quaternion[] legBaseRotations)
    {
        bodyTransform = null;
        headTransform = null;
        tailTransform = null;
        bodyBaseScale = Vector3.one;
        headBaseRotation = Quaternion.identity;
        tailBaseRotation = Quaternion.identity;
        legTransforms = System.Array.Empty<Transform>();
        legBaseRotations = System.Array.Empty<Quaternion>();

        GameObject prefab = Resources.Load<GameObject>(AmbientSquirrelModelResourcePath) ??
            Resources.Load<GameObject>(AmbientSquirrelModelResourcePathFallback);
        if (prefab == null)
        {
            return false;
        }

        GameObject model = Instantiate(prefab, squirrelRoot);
        model.name = "AmbientSquirrelImportedModel";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (!TryGetLocalRendererBounds(squirrelRoot, renderers, out Bounds bounds))
        {
            Destroy(model);
            return false;
        }

        float scale = Mathf.Min(
            AmbientSquirrelImportedTargetWidth / Mathf.Max(bounds.size.x, 0.001f),
            AmbientSquirrelImportedTargetHeight / Mathf.Max(bounds.size.y, 0.001f),
            AmbientSquirrelImportedTargetLength / Mathf.Max(bounds.size.z, 0.001f));
        model.transform.localScale = Vector3.one * scale;

        if (TryGetLocalRendererBounds(squirrelRoot, renderers, out Bounds scaledBounds))
        {
            model.transform.localPosition = new Vector3(
                -scaledBounds.center.x,
                -scaledBounds.min.y,
                -scaledBounds.center.z);
        }

        ConfigureImportedAmbientSquirrelModel(model, renderers);
        BuildImportedSquirrelAnimationRig(
            model.transform,
            out Transform importedBodyRig,
            out Transform importedHeadRig,
            out Transform importedTailRig,
            out legTransforms,
            out legBaseRotations);

        bodyTransform = importedBodyRig ?? model.transform;
        headTransform = importedHeadRig;
        tailTransform = importedTailRig;
        bodyBaseScale = bodyTransform != null ? bodyTransform.localScale : Vector3.one;
        headBaseRotation = headTransform != null ? headTransform.localRotation : Quaternion.identity;
        tailBaseRotation = tailTransform != null ? tailTransform.localRotation : Quaternion.identity;
        return true;
    }

    private void ConfigureImportedAmbientSquirrelModel(GameObject model, Renderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            ApplyMaterialSmoothness(renderer, VisualSmoothnessFabric);
            NormalizeImportedBuildingMaterial(renderer);
        }

        Collider[] colliders = model.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Camera[] cameras = model.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            Destroy(cameras[i]);
        }

        Light[] lights = model.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            Destroy(lights[i]);
        }

        Animator[] animators = model.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            Destroy(animators[i]);
        }

        Animation[] animations = model.GetComponentsInChildren<Animation>(true);
        for (int i = 0; i < animations.Length; i++)
        {
            Destroy(animations[i]);
        }
    }

    private void CreateProceduralAmbientSquirrelModel(
        Transform squirrelRoot,
        out Transform bodyTransform,
        out Transform headTransform,
        out Transform tailTransform,
        out Vector3 bodyBaseScale,
        out Quaternion headBaseRotation,
        out Quaternion tailBaseRotation,
        out Transform[] legTransforms,
        out Quaternion[] legBaseRotations)
    {
        Color bodyColor = new(0.72f, 0.42f, 0.14f);
        Color headColor = new(0.80f, 0.50f, 0.20f);
        Color tailColor = new(0.78f, 0.48f, 0.18f);
        Color earColor = new(0.68f, 0.38f, 0.12f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(squirrelRoot, false);
        body.transform.localPosition = new Vector3(0f, 0.10f, 0f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.14f, 0.10f, 0.20f);
        ApplyColor(body, bodyColor, VisualSmoothnessFabric);
        ConfigureStaticVisual(body, VisualSmoothnessFabric);
        if (body.TryGetComponent(out Collider bodyCol)) bodyCol.enabled = false;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(squirrelRoot, false);
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
        tail.transform.SetParent(squirrelRoot, false);
        tail.transform.localPosition = new Vector3(0f, 0.18f, -0.12f);
        tail.transform.localRotation = Quaternion.Euler(-55f, 0f, 0f);
        tail.transform.localScale = new Vector3(0.06f, 0.16f, 0.06f);
        ApplyColor(tail, tailColor, VisualSmoothnessFabric);
        ConfigureStaticVisual(tail, VisualSmoothnessFabric);
        if (tail.TryGetComponent(out Collider tailCol)) tailCol.enabled = false;

        bodyTransform = body.transform;
        headTransform = head.transform;
        tailTransform = tail.transform;
        bodyBaseScale = body.transform.localScale;
        headBaseRotation = head.transform.localRotation;
        tailBaseRotation = tail.transform.localRotation;
        legTransforms = System.Array.Empty<Transform>();
        legBaseRotations = System.Array.Empty<Quaternion>();
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

                    ApplySquirrelBodyScale(
                        sq,
                        new Vector3(0.14f, 0.10f, 0.20f),
                        new Vector3(1f, 1f + Mathf.Sin(time * 1.8f + sq.AnimationPhase) * 0.025f, 1f));

                    if (sq.HeadTransform != null)
                    {
                        ApplySquirrelHeadMotion(
                            sq,
                            Quaternion.Euler(
                                Mathf.Sin(time * 1.1f + sq.AnimationPhase) * 6f,
                                Mathf.Sin(time * 0.7f + sq.AnimationPhase) * 12f,
                                0f));
                    }

                    if (sq.TailTransform != null)
                    {
                        ApplySquirrelTailMotion(
                            sq,
                            Quaternion.Euler(
                                Mathf.Sin(time * 1.8f + sq.TailPhase) * 7f,
                                Mathf.Sin(time * 1.4f + sq.TailPhase) * 14f,
                                Mathf.Sin(time * 1.1f + sq.TailPhase) * 5f),
                            Quaternion.Euler(
                                -55f + Mathf.Sin(time * 1.8f + sq.TailPhase) * 8f,
                                Mathf.Sin(time * 1.4f + sq.TailPhase) * 10f,
                                0f));
                    }

                    AnimateImportedSquirrelLegs(sq, time, 2.4f, 1.4f, -2f);

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
                    ApplySquirrelBodyScale(sq, new Vector3(0.14f, 0.09f, 0.21f), new Vector3(1.05f, 0.92f, 1.08f));

                    if (sq.HeadTransform != null)
                    {
                        float nod = Mathf.Sin(time * 7f + sq.AnimationPhase) * 22f;
                        ApplySquirrelHeadMotion(sq, Quaternion.Euler(nod, 0f, 0f));
                    }

                    if (sq.TailTransform != null)
                    {
                        ApplySquirrelTailMotion(
                            sq,
                            Quaternion.Euler(-8f + Mathf.Sin(time * 5f + sq.TailPhase) * 5f, 0f, 10f),
                            Quaternion.Euler(-72f, 0f, 0f));
                    }

                    AnimateImportedSquirrelLegs(sq, time, 6f, 6f, -12f);

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
                        ApplySquirrelBodyScale(
                            sq,
                            new Vector3(0.14f, 0.09f, 0.20f),
                            new Vector3(
                                1.04f + Mathf.Sin(time * 13f + sq.AnimationPhase) * 0.025f,
                                0.96f + Mathf.Abs(Mathf.Sin(time * 13f + sq.AnimationPhase)) * 0.06f,
                                1.02f));
                    }

                    if (sq.TailTransform != null)
                    {
                        ApplySquirrelTailMotion(
                            sq,
                            Quaternion.Euler(
                                -12f + Mathf.Sin(time * 10f + sq.TailPhase) * 9f,
                                Mathf.Sin(time * 7f + sq.TailPhase) * 18f,
                                8f),
                            Quaternion.Euler(
                                -10f + Mathf.Sin(time * 10f + sq.TailPhase) * 8f,
                                0f, 0f));
                    }

                    AnimateImportedSquirrelLegs(sq, time, 14f, 30f, 0f);

                    if (runT >= 1f)
                    {
                        sq.CurrentPointIndex = sq.TargetPointIndex;
                        sq.CurrentPosition   = sq.TargetPosition;
                        sq.Yaw               = sq.RootTransform.eulerAngles.y;
                        if (sq.BodyTransform != null)
                        {
                            ApplySquirrelBodyScale(sq, new Vector3(0.14f, 0.10f, 0.20f), Vector3.one);
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
                        ApplySquirrelBodyScale(sq, new Vector3(0.14f, 0.09f, 0.20f), new Vector3(1.02f, 0.94f, 1.06f));

                    if (sq.TailTransform != null)
                        ApplySquirrelTailMotion(
                            sq,
                            Quaternion.Euler(
                                -4f + Mathf.Sin(time * 9f + sq.TailPhase) * 12f,
                                Mathf.Sin(time * 6f + sq.TailPhase) * 14f,
                                10f),
                            Quaternion.Euler(
                                -10f + Mathf.Sin(time * 9f + sq.TailPhase) * 14f,
                                Mathf.Sin(time * 6f + sq.TailPhase) * 10f, 0f));

                    AnimateImportedSquirrelLegs(sq, time, 12f, 26f, 0f);

                    if (climbUpT >= 1f)
                    {
                        sq.IsAtTreeTop    = true;
                        sq.CurrentPosition = sq.TargetPosition;
                        if (sq.BodyTransform != null)
                            ApplySquirrelBodyScale(sq, new Vector3(0.14f, 0.10f, 0.20f), Vector3.one);
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
                        ApplySquirrelBodyScale(sq, new Vector3(0.14f, 0.09f, 0.20f), new Vector3(1.02f, 0.94f, 1.06f));

                    if (sq.TailTransform != null)
                        ApplySquirrelTailMotion(
                            sq,
                            Quaternion.Euler(
                                -4f + Mathf.Sin(time * 9f + sq.TailPhase) * 12f,
                                Mathf.Sin(time * 6f + sq.TailPhase) * 14f,
                                -10f),
                            Quaternion.Euler(
                                -10f + Mathf.Sin(time * 9f + sq.TailPhase) * 14f,
                                Mathf.Sin(time * 6f + sq.TailPhase) * 10f, 0f));

                    AnimateImportedSquirrelLegs(sq, time, 12f, 26f, 0f);

                    if (climbDownT >= 1f)
                    {
                        sq.IsAtTreeTop     = false;
                        sq.CurrentPosition = sq.TargetPosition;
                        if (sq.BodyTransform != null)
                            ApplySquirrelBodyScale(sq, new Vector3(0.14f, 0.10f, 0.20f), Vector3.one);
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
    private int FindNextSquirrelRoamPoint(AmbientSquirrelData sq)
    {
        int current = sq?.CurrentPointIndex ?? -1;
        if (ambientSquirrelRoamPoints.Count < 2 || current < 0 || current >= ambientSquirrelRoamPoints.Count)
        {
            return -1;
        }

        List<int> candidates = new();
        Vector3 currentPos = ambientSquirrelRoamPoints[current];
        for (int i = 0; i < ambientSquirrelRoamPoints.Count; i++)
        {
            if (i == current)
            {
                continue;
            }

            float dist = Vector3.Distance(currentPos, ambientSquirrelRoamPoints[i]);
            if (dist >= 1.5f && dist <= 8f)
            {
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0)
        {
            return (current + 1) % ambientSquirrelRoamPoints.Count;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private int FindNearestSquirrelRoamPoint(Vector3 position)
    {
        if (ambientSquirrelRoamPoints.Count == 0)
        {
            return -1;
        }

        int bestIndex = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < ambientSquirrelRoamPoints.Count; i++)
        {
            float distance = (ambientSquirrelRoamPoints[i] - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
