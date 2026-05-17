using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap : MonoBehaviour
{
    private bool TryCreateImportedAmbientDogModel(
        Transform dogRoot,
        int dogIndex,
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

        GameObject prefab = Resources.Load<GameObject>(AmbientDogModelResourcePath);
        if (prefab == null)
        {
            return false;
        }

        GameObject model = Instantiate(prefab, dogRoot);
        model.name = "AmbientDogImportedModel";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (!TryGetLocalRendererBounds(dogRoot, renderers, out Bounds bounds))
        {
            Destroy(model);
            return false;
        }

        float scale = Mathf.Min(
            AmbientDogImportedTargetWidth / Mathf.Max(bounds.size.x, 0.001f),
            AmbientDogImportedTargetHeight / Mathf.Max(bounds.size.y, 0.001f),
            AmbientDogImportedTargetLength / Mathf.Max(bounds.size.z, 0.001f));
        model.transform.localScale = Vector3.one * scale;

        if (TryGetLocalRendererBounds(dogRoot, renderers, out Bounds scaledBounds))
        {
            model.transform.localPosition = new Vector3(
                -scaledBounds.center.x,
                -scaledBounds.min.y,
                -scaledBounds.center.z);
        }

        ConfigureImportedAmbientDogModel(model, renderers, dogIndex);
        BuildImportedDogAnimationRig(
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

    private void ConfigureImportedAmbientDogModel(GameObject model, Renderer[] renderers, int dogIndex)
    {
        Color[] furOptions =
        {
            new(0.68f, 0.43f, 0.20f),
            new(0.88f, 0.76f, 0.56f),
            new(0.32f, 0.28f, 0.23f),
            new(0.78f, 0.58f, 0.34f)
        };
        Color fur = furOptions[Mathf.Abs(dogIndex) % furOptions.Length];
        Color darkFur = Color.Lerp(fur * 0.55f, new Color(0.12f, 0.10f, 0.08f), 0.45f);
        Color belly = Color.Lerp(new Color(0.96f, 0.86f, 0.68f), Color.white, (dogIndex % 2) * 0.18f);
        Color collar = dogIndex % 2 == 0 ? new Color(0.78f, 0.12f, 0.10f) : new Color(0.12f, 0.35f, 0.76f);

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            ApplyMaterialSmoothness(renderer, VisualSmoothnessFabric);
            NormalizeImportedBuildingMaterial(renderer);
            TintImportedDogRenderer(renderer, fur, darkFur, belly, collar);
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

    private static void TintImportedDogRenderer(Renderer renderer, Color fur, Color darkFur, Color belly, Color collar)
    {
        string surfaceName = GetImportedDogSurfaceName(renderer != null ? renderer.transform : null);
        Material[] materials = renderer.materials;
        for (int i = 0; i < materials.Length; i++)
        {
            Material material = materials[i];
            if (material == null)
            {
                continue;
            }

            string materialName = material.name;
            if (surfaceName.IndexOf("Collar", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                materialName.IndexOf("Collar", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SetImportedDogMaterialColor(material, collar);
            }
            else if (surfaceName.IndexOf("Belly", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     materialName.IndexOf("Belly", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SetImportedDogMaterialColor(material, belly);
            }
            else if (surfaceName.IndexOf("Dark", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     materialName.IndexOf("Dark", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SetImportedDogMaterialColor(material, darkFur);
            }
            else if (surfaceName.IndexOf("Nose", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     materialName.IndexOf("Nose", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SetImportedDogMaterialColor(material, new Color(0.055f, 0.045f, 0.04f));
            }
            else if (surfaceName.IndexOf("Fur", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     materialName.IndexOf("Fur", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SetImportedDogMaterialColor(material, fur);
            }
        }
    }

    private static string GetImportedDogSurfaceName(Transform transform)
    {
        while (transform != null)
        {
            if (!string.IsNullOrEmpty(transform.name) &&
                !transform.name.StartsWith("AmbientDogImportedModel", System.StringComparison.OrdinalIgnoreCase) &&
                !transform.name.StartsWith("Dog_Root", System.StringComparison.OrdinalIgnoreCase))
            {
                return transform.name;
            }

            transform = transform.parent;
        }

        return string.Empty;
    }

    private static void SetImportedDogMaterialColor(Material material, Color color)
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

    private void CreateProceduralAmbientDogModel(
        Transform dogRoot,
        int dogIndex,
        out Transform bodyTransform,
        out Transform headTransform,
        out Transform tailTransform,
        out Vector3 bodyBaseScale,
        out Quaternion headBaseRotation,
        out Quaternion tailBaseRotation,
        out Transform[] legTransforms,
        out Quaternion[] legBaseRotations)
    {
        Color fur = Color.Lerp(new Color(0.45f, 0.28f, 0.14f), new Color(0.83f, 0.68f, 0.46f), (dogIndex % 3) * 0.28f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(dogRoot, false);
        body.transform.localPosition = new Vector3(0f, 0.27f, 0f);
        body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        body.transform.localScale = new Vector3(0.20f, 0.34f, 0.20f);
        ApplyColor(body, fur, VisualSmoothnessFabric);
        ConfigureStaticVisual(body, VisualSmoothnessFabric);
        if (body.TryGetComponent(out Collider bodyCollider)) bodyCollider.enabled = false;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(dogRoot, false);
        head.transform.localPosition = new Vector3(0f, 0.38f, 0.36f);
        head.transform.localScale = new Vector3(0.18f, 0.16f, 0.18f);
        ApplyColor(head, fur * 1.05f, VisualSmoothnessFabric);
        ConfigureStaticVisual(head, VisualSmoothnessFabric);
        if (head.TryGetComponent(out Collider headCollider)) headCollider.enabled = false;

        CreateProceduralDogEar(head.transform, "LeftEar", new Vector3(-0.07f, 0.09f, -0.02f), 18f, fur * 0.9f);
        CreateProceduralDogEar(head.transform, "RightEar", new Vector3(0.07f, 0.09f, -0.02f), -18f, fur * 0.9f);

        GameObject tail = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tail.transform.SetParent(dogRoot, false);
        tail.transform.localPosition = new Vector3(0f, 0.33f, -0.34f);
        tail.transform.localRotation = Quaternion.Euler(-52f, 0f, 0f);
        tail.transform.localScale = new Vector3(0.035f, 0.19f, 0.035f);
        ApplyColor(tail, fur * 0.88f, VisualSmoothnessFabric);
        ConfigureStaticVisual(tail, VisualSmoothnessFabric);
        if (tail.TryGetComponent(out Collider tailCollider)) tailCollider.enabled = false;

        legTransforms = new Transform[4];
        legTransforms[0] = CreateProceduralDogLeg(dogRoot, "FrontLeftLeg", new Vector3(-0.09f, 0.14f, 0.23f), fur * 0.92f);
        legTransforms[1] = CreateProceduralDogLeg(dogRoot, "FrontRightLeg", new Vector3(0.09f, 0.14f, 0.23f), fur * 0.92f);
        legTransforms[2] = CreateProceduralDogLeg(dogRoot, "BackLeftLeg", new Vector3(-0.09f, 0.14f, -0.21f), fur * 0.86f);
        legTransforms[3] = CreateProceduralDogLeg(dogRoot, "BackRightLeg", new Vector3(0.09f, 0.14f, -0.21f), fur * 0.86f);
        legBaseRotations = new Quaternion[legTransforms.Length];
        for (int i = 0; i < legTransforms.Length; i++)
        {
            legBaseRotations[i] = legTransforms[i] != null ? legTransforms[i].localRotation : Quaternion.identity;
        }

        bodyTransform = body.transform;
        headTransform = head.transform;
        tailTransform = tail.transform;
        bodyBaseScale = body.transform.localScale;
        headBaseRotation = head.transform.localRotation;
        tailBaseRotation = tail.transform.localRotation;
    }

    private void CreateProceduralDogEar(Transform parent, string name, Vector3 position, float roll, Color color)
    {
        GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ear.name = name;
        ear.transform.SetParent(parent, false);
        ear.transform.localPosition = position;
        ear.transform.localRotation = Quaternion.Euler(0f, 0f, roll);
        ear.transform.localScale = new Vector3(0.045f, 0.09f, 0.035f);
        ApplyColor(ear, color, VisualSmoothnessFabric);
        ConfigureStaticVisual(ear, VisualSmoothnessFabric);
        if (ear.TryGetComponent(out Collider collider)) collider.enabled = false;
    }

    private Transform CreateProceduralDogLeg(Transform parent, string name, Vector3 position, Color color)
    {
        GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leg.name = name;
        leg.transform.SetParent(parent, false);
        leg.transform.localPosition = position;
        leg.transform.localScale = new Vector3(0.065f, 0.20f, 0.065f);
        ApplyColor(leg, color, VisualSmoothnessFabric);
        ConfigureStaticVisual(leg, VisualSmoothnessFabric);
        if (leg.TryGetComponent(out Collider collider)) collider.enabled = false;
        return leg.transform;
    }
}
