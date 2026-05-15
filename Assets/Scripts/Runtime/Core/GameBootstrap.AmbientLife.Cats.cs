using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private const string AmbientCatModelResourcePath = "Misc/cat";
    private const float AmbientCatImportedTargetWidth = 0.34f;
    private const float AmbientCatImportedTargetHeight = 0.30f;
    private const float AmbientCatImportedTargetLength = 0.56f;

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

        bool usesImportedModel = TryCreateImportedAmbientCatModel(
            catRoot.transform,
            catIndex,
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
            CreateProceduralAmbientCatModel(
                catRoot.transform,
                catIndex,
                out bodyTransform,
                out headTransform,
                out tailTransform,
                out bodyBaseScale,
                out headBaseRotation,
                out tailBaseRotation,
                out legTransforms,
                out legBaseRotations);
        }

        int pointIndex = Mathf.Abs(catIndex * 2) % ambientCatRoamPoints.Count;
        Vector3 position = ambientCatRoamPoints[pointIndex];
        float yaw = Random.Range(0f, 360f);
        catRoot.transform.position = position;
        catRoot.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        ambientCats.Add(new AmbientCatData
        {
            RootTransform = catRoot.transform,
            BodyTransform = bodyTransform,
            HeadTransform = headTransform,
            TailTransform = tailTransform,
            UsesImportedModel = usesImportedModel,
            BodyBaseScale = bodyBaseScale,
            HeadBaseRotation = headBaseRotation,
            TailBaseRotation = tailBaseRotation,
            LegTransforms = legTransforms,
            LegBaseRotations = legBaseRotations,
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

    private bool TryCreateImportedAmbientCatModel(
        Transform catRoot,
        int catIndex,
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

        GameObject prefab = Resources.Load<GameObject>(AmbientCatModelResourcePath);
        if (prefab == null)
        {
            return false;
        }

        GameObject model = Instantiate(prefab, catRoot);
        model.name = "AmbientCatImportedModel";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (!TryGetLocalRendererBounds(catRoot, renderers, out Bounds bounds))
        {
            Destroy(model);
            return false;
        }

        float scale = Mathf.Min(
            AmbientCatImportedTargetWidth / Mathf.Max(bounds.size.x, 0.001f),
            AmbientCatImportedTargetHeight / Mathf.Max(bounds.size.y, 0.001f),
            AmbientCatImportedTargetLength / Mathf.Max(bounds.size.z, 0.001f));
        model.transform.localScale = Vector3.one * scale;

        if (TryGetLocalRendererBounds(catRoot, renderers, out Bounds scaledBounds))
        {
            model.transform.localPosition = new Vector3(
                -scaledBounds.center.x,
                -scaledBounds.min.y,
                -scaledBounds.center.z);
        }

        ConfigureImportedAmbientCatModel(model, renderers, catIndex);
        BuildImportedCatAnimationRig(
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

    private void ConfigureImportedAmbientCatModel(GameObject model, Renderer[] renderers, int catIndex)
    {
        Color fur = Color.Lerp(new Color(0.25f, 0.23f, 0.21f), new Color(0.86f, 0.53f, 0.18f), (catIndex % 3) * 0.38f);
        Color stripes = Color.Lerp(fur * 0.58f, new Color(0.24f, 0.12f, 0.05f), 0.36f);
        Color patches = Color.Lerp(new Color(0.92f, 0.82f, 0.62f), Color.white, (catIndex % 2) * 0.18f);

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
            TintImportedCatRenderer(renderer, fur, stripes, patches);
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

    private static void TintImportedCatRenderer(Renderer renderer, Color fur, Color stripes, Color patches)
    {
        string surfaceName = GetImportedCatSurfaceName(renderer != null ? renderer.transform : null);
        bool stripeSurface = surfaceName.IndexOf("FurStripe", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool patchSurface = surfaceName.IndexOf("FurPatch", System.StringComparison.OrdinalIgnoreCase) >= 0;
        bool furSurface =
            !surfaceName.Contains("_Inner") &&
            (surfaceName.StartsWith("Body", System.StringComparison.OrdinalIgnoreCase) ||
             surfaceName.StartsWith("Head", System.StringComparison.OrdinalIgnoreCase) ||
             surfaceName.StartsWith("Tail", System.StringComparison.OrdinalIgnoreCase) ||
             surfaceName.StartsWith("Leg_", System.StringComparison.OrdinalIgnoreCase) ||
             surfaceName.StartsWith("Ear_", System.StringComparison.OrdinalIgnoreCase));
        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
            {
                continue;
            }

            string materialName = material.name;
            if (stripeSurface ||
                materialName.IndexOf("Fur Stripes", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SetImportedCatMaterialColor(material, stripes);
            }
            else if (patchSurface ||
                     materialName.IndexOf("Fur Patches", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SetImportedCatMaterialColor(material, patches);
            }
            else if (furSurface ||
                     materialName.IndexOf("Cat Fur", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SetImportedCatMaterialColor(material, fur);
            }
        }
    }

    private static string GetImportedCatSurfaceName(Transform transform)
    {
        while (transform != null)
        {
            if (!string.IsNullOrEmpty(transform.name) &&
                !transform.name.StartsWith("AmbientCatImportedModel", System.StringComparison.OrdinalIgnoreCase) &&
                !transform.name.StartsWith("Cat_Root", System.StringComparison.OrdinalIgnoreCase))
            {
                return transform.name;
            }

            transform = transform.parent;
        }

        return string.Empty;
    }

    private static void SetImportedCatMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        color.a = 1f;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private void CreateProceduralAmbientCatModel(
        Transform catRoot,
        int catIndex,
        out Transform bodyTransform,
        out Transform headTransform,
        out Transform tailTransform,
        out Vector3 bodyBaseScale,
        out Quaternion headBaseRotation,
        out Quaternion tailBaseRotation,
        out Transform[] legTransforms,
        out Quaternion[] legBaseRotations)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(catRoot, false);
        body.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        body.transform.localScale = new Vector3(0.16f, 0.12f, 0.26f);
        ApplyColor(body, Color.Lerp(new Color(0.24f, 0.22f, 0.2f), new Color(0.82f, 0.54f, 0.18f), (catIndex % 3) * 0.35f), VisualSmoothnessFabric);
        ConfigureStaticVisual(body, VisualSmoothnessFabric);
        if (body.TryGetComponent(out Collider bodyCollider))
        {
            bodyCollider.enabled = false;
        }

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(catRoot, false);
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
        tail.transform.SetParent(catRoot, false);
        tail.transform.localPosition = new Vector3(0f, 0.16f, -0.14f);
        tail.transform.localRotation = Quaternion.Euler(68f, 0f, 0f);
        tail.transform.localScale = new Vector3(0.028f, 0.16f, 0.028f);
        ApplyColor(tail, body.GetComponent<Renderer>().material.color * 0.92f, VisualSmoothnessFabric);
        ConfigureStaticVisual(tail, VisualSmoothnessFabric);
        if (tail.TryGetComponent(out Collider tailCollider))
        {
            tailCollider.enabled = false;
        }

        bodyTransform = body.transform;
        headTransform = head.transform;
        tailTransform = tail.transform;
        bodyBaseScale = body.transform.localScale;
        headBaseRotation = head.transform.localRotation;
        tailBaseRotation = tail.transform.localRotation;
        legTransforms = System.Array.Empty<Transform>();
        legBaseRotations = System.Array.Empty<Quaternion>();
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
                            if (cat.UsesImportedModel)
                            {
                                float breathing = catsShouldSleep
                                    ? 0.96f + Mathf.Sin(time * 0.8f + cat.AnimationPhase) * 0.015f
                                    : 1f + Mathf.Sin(time * 1.7f + cat.AnimationPhase) * 0.035f;
                                float stretch = catsShouldSleep ? 1.04f : 1f - (breathing - 1f) * 0.35f;
                                cat.BodyTransform.localScale = Vector3.Scale(cat.BodyBaseScale, new Vector3(stretch, breathing, stretch));
                            }
                            else
                            {
                                cat.BodyTransform.localScale = catsShouldSleep
                                    ? new Vector3(0.19f, 0.085f, 0.28f)
                                    : new Vector3(0.16f, 0.11f + Mathf.Sin(time * 1.7f + cat.AnimationPhase) * 0.01f, 0.26f);
                            }
                        }

                        if (cat.HeadTransform != null)
                        {
                            Quaternion headMotion = catsShouldSleep
                                ? Quaternion.Euler(-18f, 0f, 0f)
                                : Quaternion.Euler(
                                    Mathf.Sin(time * 1.6f + cat.AnimationPhase) * 4f,
                                    Mathf.Sin(time * 0.9f + cat.AnimationPhase) * 8f,
                                    0f);
                            cat.HeadTransform.localRotation = cat.UsesImportedModel
                                ? cat.HeadBaseRotation * headMotion
                                : headMotion;
                        }

                        if (cat.TailTransform != null)
                        {
                            Quaternion tailMotion = cat.UsesImportedModel
                                ? catsShouldSleep
                                    ? Quaternion.Euler(0f, -20f, 14f)
                                    : Quaternion.Euler(
                                        Mathf.Sin(time * 1.9f + cat.TailPhase) * 6f,
                                        Mathf.Sin(time * 2.2f + cat.TailPhase) * 20f,
                                        Mathf.Sin(time * 1.3f + cat.TailPhase) * 8f)
                                : catsShouldSleep
                                    ? Quaternion.Euler(34f, -22f, 0f)
                                    : Quaternion.Euler(
                                        64f + Mathf.Sin(time * 2.4f + cat.TailPhase) * 8f,
                                        Mathf.Sin(time * 2.1f + cat.TailPhase) * 10f,
                                        0f);
                            cat.TailTransform.localRotation = cat.UsesImportedModel
                                ? cat.TailBaseRotation * tailMotion
                                : tailMotion;
                        }

                        AnimateImportedCatLegs(cat, time, catsShouldSleep ? 1.6f : 2.4f, catsShouldSleep ? 1.2f : 2.1f, catsShouldSleep ? -8f : -2f);
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
                        cat.BodyTransform.localScale = cat.UsesImportedModel
                            ? Vector3.Scale(cat.BodyBaseScale, new Vector3(
                                1.02f + Mathf.Sin(time * 9f + cat.AnimationPhase) * 0.025f,
                                0.98f + Mathf.Abs(Mathf.Sin(time * 9f + cat.AnimationPhase)) * 0.06f,
                                1.01f))
                            : new Vector3(0.15f, 0.12f, 0.25f);
                    }

                    if (cat.HeadTransform != null)
                    {
                        Quaternion headMotion = Quaternion.Euler(Mathf.Sin(time * 10f + cat.AnimationPhase) * 6f, 0f, 0f);
                        cat.HeadTransform.localRotation = cat.UsesImportedModel
                            ? cat.HeadBaseRotation * headMotion
                            : headMotion;
                    }

                    if (cat.TailTransform != null)
                    {
                        Quaternion tailMotion = cat.UsesImportedModel
                            ? Quaternion.Euler(
                                -8f + Mathf.Sin(time * 9f + cat.TailPhase) * 5f,
                                Mathf.Sin(time * 7.4f + cat.TailPhase) * 24f,
                                12f)
                            : Quaternion.Euler(
                                72f + Mathf.Sin(time * 8.5f + cat.TailPhase) * 12f,
                                Mathf.Sin(time * 6.2f + cat.TailPhase) * 14f,
                                0f);
                        cat.TailTransform.localRotation = cat.UsesImportedModel
                            ? cat.TailBaseRotation * tailMotion
                            : tailMotion;
                    }

                    AnimateImportedCatLegs(cat, time, 10.5f, 28f, 0f);

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
                        cat.BodyTransform.localScale = cat.UsesImportedModel
                            ? Vector3.Scale(cat.BodyBaseScale, new Vector3(1f, 1.05f, 0.96f))
                            : new Vector3(0.16f, 0.13f, 0.22f);
                    if (cat.HeadTransform != null)
                    {
                        Quaternion headMotion = Quaternion.Euler(-10f, Mathf.Sin(time * 1.5f + cat.AnimationPhase) * 6f, 0f);
                        cat.HeadTransform.localRotation = cat.UsesImportedModel
                            ? cat.HeadBaseRotation * headMotion
                            : headMotion;
                    }
                    if (cat.TailTransform != null)
                    {
                        Quaternion tailMotion = cat.UsesImportedModel
                            ? Quaternion.Euler(
                                Mathf.Sin(time * 3.8f + cat.TailPhase) * 5f,
                                Mathf.Sin(time * 4.2f + cat.TailPhase) * 24f,
                                18f)
                            : Quaternion.Euler(30f + Mathf.Sin(time * 3f + cat.TailPhase) * 10f, 0f, 0f);
                        cat.TailTransform.localRotation = cat.UsesImportedModel
                            ? cat.TailBaseRotation * tailMotion
                            : tailMotion;
                    }
                    AnimateImportedCatLegs(cat, time, 2.4f, 3.5f, -10f);
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


}
