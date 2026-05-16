using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap
{
    private const string IskrianMaleImportedDriverResourcePath = "Citizens/IskryaninMale";
    private const string IskrianFemaleImportedDriverResourcePath = "Citizens/IskryaninFemale";
    private const string ZelenMaleImportedDriverResourcePath = "Citizens/ZeleninMale";
    private const string ZelenFemaleImportedDriverResourcePath = "Citizens/ZeleninFemale";
    private const string RovianMaleImportedDriverResourcePath = "Citizens/RovianMale";
    private const string RovianFemaleImportedDriverResourcePath = "Citizens/RovianFemale";
    private const float CharacterWorldVisualScale = 0.88f;
    private const float ImportedDriverFemaleHeightOffset = 0.045f;
    private const float ImportedDriverHeightJitter = 0.025f;

    private struct DriverVisualRecipe
    {
        public Color ShirtColor;
        public Color TrouserColor;
        public Color AccentColor;
        public Color SecondaryAccentColor;
        public Color TrimColor;
        public Color BeltColor;
        public Color SkinColor;
        public Color HairColor;
        public Vector3 BodyPosition;
        public Vector3 BodyScale;
        public Vector3 HeadPosition;
        public Vector3 HeadScale;
        public Vector3 HeadwearPosition;
        public Vector3 LeftArmPosition;
        public Vector3 RightArmPosition;
        public Vector3 ArmScale;
        public Vector3 LeftLegPosition;
        public Vector3 RightLegPosition;
        public Vector3 LegScale;
        public Vector3 ShadowScale;
        public float ShadowAlpha;
    }

    private void BuildDriverVisualModel(DriverAgent driver)
    {
        if (driver?.DriverVisualRoot == null)
        {
            return;
        }

        ResetImportedDriverVisualAnimationHooks(driver);
        EnsureWorkerRace(driver);
        int variant = GetDriverVisualModelVariant(driver);
        DriverVisualRecipe recipe = CreateDriverVisualRecipe(driver, variant);

        CreateDriverShadowBlob(driver.DriverVisualRoot, recipe);

        if (TryCreateImportedDriverVisualModel(driver))
        {
            CreateDriverCarryProps(driver);
            return;
        }

        Transform bodyAnchor = CreateDriverVisualAnchor("DriverBody", driver.DriverVisualRoot, recipe.BodyPosition);
        driver.DriverBodyTransform = bodyAnchor;
        CreateDriverModelPart(bodyAnchor, "DriverTorso", PrimitiveType.Capsule, Vector3.zero, Vector3.zero, recipe.BodyScale, recipe.ShirtColor, VisualSmoothnessFabric);
        CreateDriverModelPart(bodyAnchor, "DriverBelt", PrimitiveType.Cube, new Vector3(0f, -0.16f, 0.03f), Vector3.zero, new Vector3(0.30f, 0.035f, 0.24f), recipe.BeltColor, VisualSmoothnessFabric);

        Transform headAnchor = CreateDriverVisualAnchor("DriverHead", driver.DriverVisualRoot, recipe.HeadPosition);
        driver.DriverHeadTransform = headAnchor;
        CreateDriverModelPart(headAnchor, "DriverFace", PrimitiveType.Sphere, Vector3.zero, Vector3.zero, recipe.HeadScale, recipe.SkinColor, VisualSmoothnessSkin);

        Transform headwearAnchor = CreateDriverVisualAnchor("DriverHeadwear", driver.DriverVisualRoot, recipe.HeadwearPosition);
        driver.DriverCapTransform = headwearAnchor;
        BuildDriverBaseHair(headwearAnchor, driver, recipe, variant);
        BuildDriverRaceModelDetails(bodyAnchor, headAnchor, headwearAnchor, driver.Race, variant, recipe);

        driver.DriverLeftArmTransform  = CreateDriverLimb(driver.DriverVisualRoot, "DriverLeftArm",  recipe.LeftArmPosition,  recipe.ArmScale, recipe.ShirtColor);
        driver.DriverRightArmTransform = CreateDriverLimb(driver.DriverVisualRoot, "DriverRightArm", recipe.RightArmPosition, recipe.ArmScale, recipe.ShirtColor);
        driver.DriverLeftLegTransform  = CreateDriverLimb(driver.DriverVisualRoot, "DriverLeftLeg",  recipe.LeftLegPosition,  recipe.LegScale, recipe.TrouserColor);
        driver.DriverRightLegTransform = CreateDriverLimb(driver.DriverVisualRoot, "DriverRightLeg", recipe.RightLegPosition, recipe.LegScale, recipe.TrouserColor);

        CreateDriverCarryProps(driver);
    }

    private bool TryCreateImportedDriverVisualModel(DriverAgent driver)
    {
        if (!TryGetImportedDriverVisualConfig(driver, out string resourcePath, out string modelName, out string rootName))
        {
            return false;
        }

        bool isFemale = driver.Gender == WorkerGender.Female;
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            return false;
        }

        GameObject model = Instantiate(prefab, driver.DriverVisualRoot);
        model.name = modelName;
        driver.DriverImportedCitizenVisual = true;
        driver.DriverImportedCitizenFemaleVisual = isFemale;
        driver.DriverImportedModelTransform = model.transform;
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            ResetImportedDriverVisualAnimationHooks(driver);
            Destroy(model);
            return false;
        }

        ConfigureImportedDriverVisual(model, renderers);
        model.transform.localRotation = SelectImportedDriverUprightRotation(driver.DriverVisualRoot, model.transform, renderers);

        if (!TryGetLocalRendererBounds(driver.DriverVisualRoot, renderers, out Bounds bounds))
        {
            ResetImportedDriverVisualAnimationHooks(driver);
            Destroy(model);
            return false;
        }

        float targetHeight = GetImportedDriverTargetHeight(driver);
        float scale = targetHeight / Mathf.Max(bounds.size.y, 0.01f);
        if (scale <= 0f || float.IsNaN(scale) || float.IsInfinity(scale))
        {
            ResetImportedDriverVisualAnimationHooks(driver);
            Destroy(model);
            return false;
        }

        model.transform.localScale = Vector3.one * scale;
        if (TryGetLocalRendererBounds(driver.DriverVisualRoot, renderers, out Bounds scaledBounds))
        {
            model.transform.localPosition = new Vector3(
                -scaledBounds.center.x,
                -scaledBounds.min.y,
                -scaledBounds.center.z);
        }

        Transform poseRoot = CreateImportedDriverPoseRoot(driver.DriverVisualRoot, model.transform);
        BuildImportedDriverAnimationRig(driver, model.transform, rootName, isFemale, poseRoot);
        return true;
    }

    private static float GetImportedDriverTargetHeight(DriverAgent driver)
    {
        float height = driver?.Race switch
        {
            WorkerRaceKind.Iskrian => 1.24f,
            WorkerRaceKind.Zelen => 1.12f,
            _ => 1.18f
        };
        if (driver != null && driver.Gender == WorkerGender.Female) height -= ImportedDriverFemaleHeightOffset;
        uint seed = (uint)(StableDriverVisualHash(driver?.DriverName) ^ ((driver?.DriverId ?? 0) * 1103515245));
        return height + (((seed % 1001u) / 500f) - 1f) * ImportedDriverHeightJitter;
    }

    private static bool TryGetImportedDriverVisualConfig(
        DriverAgent driver,
        out string resourcePath,
        out string modelName,
        out string rootName)
    {
        resourcePath = null;
        modelName = null;
        rootName = null;
        if (driver == null)
        {
            return false;
        }

        bool isFemale = driver.Gender == WorkerGender.Female;
        switch (driver.Race)
        {
            case WorkerRaceKind.Rovian:
                resourcePath = isFemale ? RovianFemaleImportedDriverResourcePath : RovianMaleImportedDriverResourcePath;
                modelName = isFemale ? "RovianFemaleImportedModel" : "RovianMaleImportedModel";
                rootName = isFemale ? "RovianFemale_Root" : "RovianMale_Root";
                return true;
            case WorkerRaceKind.Zelen:
                resourcePath = isFemale ? ZelenFemaleImportedDriverResourcePath : ZelenMaleImportedDriverResourcePath;
                modelName = isFemale ? "ZelenFemaleImportedModel" : "ZelenMaleImportedModel";
                rootName = isFemale ? "ZelenkaFemale_Root" : "ZeleninMale_Root";
                return true;
            case WorkerRaceKind.Iskrian:
                resourcePath = isFemale ? IskrianFemaleImportedDriverResourcePath : IskrianMaleImportedDriverResourcePath;
                modelName = isFemale ? "IskrianFemaleImportedModel" : "IskrianMaleImportedModel";
                rootName = isFemale ? "IskryaninFemale_Root" : "IskryaninMale_Root";
                return true;
            default:
                return false;
        }
    }

    private void ConfigureImportedDriverVisual(GameObject model, Renderer[] renderers)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
            {
                continue;
            }

            if (IsImportedDriverHelperRenderer(renderer))
            {
                renderer.enabled = false;
                continue;
            }

            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
            ApplyMaterialSmoothness(renderer, GuessImportedDriverSmoothness(renderer.name));
            NormalizeImportedBuildingMaterial(renderer);
            RegisterShadowLodRenderer(renderer);
        }

        Collider[] colliders = model.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }

        Camera[] cameras = model.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = false;
        }

        Light[] lights = model.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].enabled = false;
        }

        Animator[] animators = model.GetComponentsInChildren<Animator>(true);
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].enabled = false;
        }

        Animation[] animations = model.GetComponentsInChildren<Animation>(true);
        for (int i = 0; i < animations.Length; i++)
        {
            animations[i].enabled = false;
        }
    }

    private static bool IsImportedDriverHelperRenderer(Renderer renderer)
    {
        Transform current = renderer != null ? renderer.transform : null;
        while (current != null)
        {
            string name = current.name;
            if (DriverPartNameStartsWith(name, "P_") ||
                DriverPartNameContains(name, "Marker") ||
                DriverPartNameContains(name, "Helper"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static Quaternion SelectImportedDriverUprightRotation(Transform root, Transform model, Renderer[] renderers)
    {
        Quaternion[] candidateRotations =
        {
            Quaternion.identity,
            Quaternion.Euler(-90f, 0f, 0f),
            Quaternion.Euler(90f, 0f, 0f),
            Quaternion.Euler(0f, 0f, -90f),
            Quaternion.Euler(0f, 0f, 90f)
        };

        Quaternion bestRotation = Quaternion.identity;
        float bestScore = float.NegativeInfinity;
        for (int i = 0; i < candidateRotations.Length; i++)
        {
            Quaternion rotation = candidateRotations[i];
            model.localRotation = rotation;
            if (!TryGetLocalRendererBounds(root, renderers, out Bounds bounds))
            {
                continue;
            }

            Vector3 size = bounds.size;
            float horizontal = Mathf.Max(size.x, size.z);
            float score = (size.y * 2f) - horizontal;
            if (score > bestScore)
            {
                bestScore = score;
                bestRotation = rotation;
            }
        }

        model.localRotation = bestRotation;
        return bestRotation;
    }

    private static Transform CreateImportedDriverPoseRoot(Transform visualRoot, Transform modelRoot)
    {
        if (visualRoot == null || modelRoot == null)
        {
            return modelRoot;
        }

        Transform poseRoot = new GameObject("ImportedDriverPoseRoot").transform;
        poseRoot.SetParent(visualRoot, false);
        poseRoot.localPosition = Vector3.zero;
        poseRoot.localRotation = Quaternion.identity;
        poseRoot.localScale = Vector3.one;
        modelRoot.SetParent(poseRoot, false);
        return poseRoot;
    }

    private void BuildImportedDriverAnimationRig(
        DriverAgent driver,
        Transform modelRoot,
        string rootName,
        bool isFemale,
        Transform poseRoot)
    {
        Transform rigRoot = FindImportedTransform(modelRoot, rootName) ?? modelRoot;
        driver.DriverBodyTransform = poseRoot ?? modelRoot;
        driver.DriverHeadTransform = null;
        driver.DriverCapTransform = null;
        driver.DriverLeftArmTransform = null;
        driver.DriverRightArmTransform = null;
        driver.DriverLeftLegTransform = null;
        driver.DriverRightLegTransform = null;
        CreateImportedDriverLegMotionPivots(driver, rigRoot);
        driver.DriverImportedHairFlowTransforms = CreateImportedDriverFlowPivots(
            rigRoot,
            rigRoot,
            GetImportedDriverHairFlowPrefixes(driver.Race, isFemale),
            0.85f);
        driver.DriverImportedClothFlowTransforms = CreateImportedDriverFlowPivots(
            rigRoot,
            rigRoot,
            GetImportedDriverClothFlowPrefixes(driver.Race, isFemale),
            0.90f);
        driver.DriverImportedGlowTransforms = CreateImportedDriverFlowPivots(
            rigRoot,
            rigRoot,
            GetImportedDriverGlowPrefixes(driver.Race, isFemale),
            0.50f);
    }

    private static Transform CreateImportedDriverRigPivot(Transform rigRoot, string name, List<Transform> parts, float verticalPivot)
    {
        if (rigRoot == null || parts == null || parts.Count == 0 || !TryGetWorldRendererBounds(parts, out Bounds bounds))
        {
            return null;
        }

        Transform pivot = new GameObject(name).transform;
        pivot.SetParent(rigRoot, false);
        Vector3 worldPivot = new(
            bounds.center.x,
            Mathf.Lerp(bounds.min.y, bounds.max.y, Mathf.Clamp01(verticalPivot)),
            bounds.center.z);
        pivot.localPosition = rigRoot.InverseTransformPoint(worldPivot);
        pivot.localRotation = Quaternion.identity;
        pivot.localScale = Vector3.one;

        for (int i = 0; i < parts.Count; i++)
        {
            Transform part = parts[i];
            if (part != null)
            {
                part.SetParent(pivot, true);
            }
        }

        return pivot;
    }

    private static bool TryGetWorldRendererBounds(List<Transform> parts, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        for (int i = 0; i < parts.Count; i++)
        {
            Transform part = parts[i];
            if (part == null)
            {
                continue;
            }

            Renderer[] renderers = part.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null || !renderer.enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        return hasBounds;
    }

    private static List<Transform> FindImportedDriverParts(Transform root, System.Predicate<string> matchName)
    {
        List<Transform> parts = new();
        if (root == null || matchName == null)
        {
            return parts;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current == null || current == root || !matchName(current.name))
            {
                continue;
            }

            if (current.GetComponentsInChildren<Renderer>(true).Length > 0)
            {
                parts.Add(current);
            }
        }

        return parts;
    }

    private static float GuessImportedDriverSmoothness(string rendererName)
    {
        if (DriverPartNameContains(rendererName, "Eye") ||
            DriverPartNameContains(rendererName, "InnerSpark") ||
            DriverPartNameContains(rendererName, "Copper") ||
            DriverPartNameContains(rendererName, "Buckle"))
        {
            return VisualSmoothnessVehicleMetal;
        }

        if (DriverPartNameContains(rendererName, "Skin") ||
            DriverPartNameContains(rendererName, "Head") ||
            DriverPartNameContains(rendererName, "Face") ||
            DriverPartNameContains(rendererName, "Hand"))
        {
            return VisualSmoothnessSkin;
        }

        return VisualSmoothnessFabric;
    }

    private static bool IsImportedDriverBodyPartName(string name)
    {
        return DriverPartNameEquals(name, "Body") ||
            DriverPartNameEquals(name, "Neck") ||
            DriverPartNameEquals(name, "Pelvis") ||
            DriverPartNameStartsWith(name, "Apron") ||
            DriverPartNameStartsWith(name, "Belt") ||
            DriverPartNameStartsWith(name, "ChestBadge") ||
            DriverPartNameStartsWith(name, "Collar") ||
            DriverPartNameStartsWith(name, "FieldShirt") ||
            DriverPartNameStartsWith(name, "FieldTunic") ||
            DriverPartNameStartsWith(name, "GlowTrim_Collar") ||
            DriverPartNameStartsWith(name, "InnerSpark") ||
            DriverPartNameStartsWith(name, "Jacket") ||
            DriverPartNameStartsWith(name, "LeafBadge") ||
            DriverPartNameStartsWith(name, "LightCloak") ||
            DriverPartNameStartsWith(name, "LongVest") ||
            DriverPartNameStartsWith(name, "RouteStripe") ||
            DriverPartNameStartsWith(name, "Satchel") ||
            DriverPartNameStartsWith(name, "Shawl") ||
            DriverPartNameStartsWith(name, "Shirt_FrontVisible") ||
            DriverPartNameStartsWith(name, "ShortJacket") ||
            DriverPartNameStartsWith(name, "Shoulder") ||
            DriverPartNameStartsWith(name, "SideSatchel") ||
            DriverPartNameStartsWith(name, "SkinLine_Collarbone") ||
            DriverPartNameStartsWith(name, "SkinLine_Neck") ||
            DriverPartNameStartsWith(name, "Torso_Tunic") ||
            DriverPartNameStartsWith(name, "Tunic") ||
            DriverPartNameStartsWith(name, "Vest");
    }

    private static bool IsImportedDriverHeadPartName(string name)
    {
        return DriverPartNameEquals(name, "Head") ||
            DriverPartNameStartsWith(name, "Hair") ||
            DriverPartNameStartsWith(name, "Brow") ||
            DriverPartNameStartsWith(name, "Cheek") ||
            DriverPartNameStartsWith(name, "Chin") ||
            DriverPartNameStartsWith(name, "Ear") ||
            DriverPartNameStartsWith(name, "Eye") ||
            DriverPartNameStartsWith(name, "Headband") ||
            DriverPartNameStartsWith(name, "Hood") ||
            DriverPartNameStartsWith(name, "Mouth") ||
            DriverPartNameStartsWith(name, "Nose") ||
            DriverPartNameStartsWith(name, "SkinLine_Temple") ||
            DriverPartNameStartsWith(name, "SoftCap") ||
            DriverPartNameStartsWith(name, "WorkCap");
    }

    private static bool IsImportedDriverLeftArmPartName(string name)
    {
        return DriverPartNameStartsWith(name, "Arm_Forearm_Left") ||
            DriverPartNameStartsWith(name, "Arm_Upper_Left") ||
            DriverPartNameEquals(name, "Hand_Left") ||
            DriverPartNameStartsWith(name, "Cuff_Left") ||
            DriverPartNameStartsWith(name, "GlowTrim_Cuff_Left") ||
            DriverPartNameStartsWith(name, "SkinLine_Arm_Left") ||
            DriverPartNameStartsWith(name, "SkinLine_Forearm_Left") ||
            DriverPartNameStartsWith(name, "SkinLine_Hand_Left") ||
            DriverPartNameStartsWith(name, "SleeveLeaf_Branch_Left") ||
            DriverPartNameStartsWith(name, "SleeveLeaf_Line_Left");
    }

    private static bool IsImportedDriverRightArmPartName(string name)
    {
        return DriverPartNameStartsWith(name, "Arm_Forearm_Right") ||
            DriverPartNameStartsWith(name, "Arm_Upper_Right") ||
            DriverPartNameEquals(name, "Hand_Right") ||
            DriverPartNameStartsWith(name, "Cuff_Right") ||
            DriverPartNameStartsWith(name, "GlowTrim_Cuff_Right") ||
            DriverPartNameStartsWith(name, "SkinLine_Arm_Right") ||
            DriverPartNameStartsWith(name, "SkinLine_Forearm_Right") ||
            DriverPartNameStartsWith(name, "SkinLine_Hand_Right") ||
            DriverPartNameStartsWith(name, "SleeveLeaf_Branch_Right") ||
            DriverPartNameStartsWith(name, "SleeveLeaf_Line_Right");
    }

    private static bool IsImportedDriverLeftLegPartName(string name)
    {
        return DriverPartNameStartsWith(name, "Leg_Left") ||
            DriverPartNameStartsWith(name, "Boot_Left") ||
            DriverPartNameStartsWith(name, "Knee_Left") ||
            (DriverPartNameStartsWith(name, "Boot") && DriverPartNameContains(name, "_Left")) ||
            (DriverPartNameStartsWith(name, "Knee") && DriverPartNameContains(name, "_Left"));
    }

    private static bool IsImportedDriverRightLegPartName(string name)
    {
        return DriverPartNameStartsWith(name, "Leg_Right") ||
            DriverPartNameStartsWith(name, "Boot_Right") ||
            DriverPartNameStartsWith(name, "Knee_Right") ||
            (DriverPartNameStartsWith(name, "Boot") && DriverPartNameContains(name, "_Right")) ||
            (DriverPartNameStartsWith(name, "Knee") && DriverPartNameContains(name, "_Right"));
    }

    private static bool DriverPartNameEquals(string name, string expected)
    {
        return string.Equals(name, expected, System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool DriverPartNameStartsWith(string name, string prefix)
    {
        return !string.IsNullOrEmpty(name) &&
            name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool DriverPartNameContains(string name, string value)
    {
        return !string.IsNullOrEmpty(name) &&
            name.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static int GetDriverVisualModelVariant(DriverAgent driver)
    {
        if (driver == null)
        {
            return 0;
        }

        unchecked
        {
            uint hash = (uint)StableDriverVisualHash(driver.DriverName);
            hash ^= (uint)driver.DriverId * 2654435761u;
            hash ^= (uint)((int)driver.Race + 1) * 2246822519u;
            return (int)(hash % 2u);
        }
    }

    private static int StableDriverVisualHash(string value)
    {
        unchecked
        {
            int hash = 17;
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    hash = (hash * 31) + value[i];
                }
            }

            return hash;
        }
    }

    private static DriverVisualRecipe CreateDriverVisualRecipe(DriverAgent driver, int variant)
    {
        bool isFemale = driver.Gender == WorkerGender.Female;
        Color skin = WorkerPortraitSkinTones[Mathf.Clamp(driver.PortraitSkinTone, 0, WorkerPortraitSkinTones.Length - 1)];
        Color hair = WorkerPortraitHairColors[Mathf.Clamp(driver.PortraitHairColor, 0, WorkerPortraitHairColors.Length - 1)];
        float bodyWidth = isFemale ? 0.20f : 0.22f;
        float shoulderWidth = isFemale ? 0.19f : 0.205f;

        DriverVisualRecipe recipe = new()
        {
            SkinColor = skin,
            HairColor = hair,
            BodyPosition = new Vector3(0f, 0.38f, 0f),
            BodyScale = new Vector3(bodyWidth, 0.34f, bodyWidth),
            HeadPosition = new Vector3(0f, 0.88f, 0f),
            HeadScale = new Vector3(0.24f, 0.24f, 0.24f),
            HeadwearPosition = new Vector3(0f, 1.02f, 0f),
            LeftArmPosition = new Vector3(-shoulderWidth, 0.56f, 0f),
            RightArmPosition = new Vector3(shoulderWidth, 0.56f, 0f),
            ArmScale = new Vector3(0.09f, 0.34f, 0.09f),
            LeftLegPosition = new Vector3(-0.09f, 0.15f, 0f),
            RightLegPosition = new Vector3(0.09f, 0.15f, 0f),
            LegScale = new Vector3(0.10f, 0.42f, 0.10f),
            ShadowScale = new Vector3(0.34f, 0.008f, 0.34f),
            ShadowAlpha = 0.16f
        };

        switch (driver.Race)
        {
            case WorkerRaceKind.Rovian:
                ApplyRovianVisualRecipe(ref recipe, variant);
                break;
            case WorkerRaceKind.Zelen:
                ApplyZelenVisualRecipe(ref recipe, variant);
                break;
            case WorkerRaceKind.Iskrian:
                ApplyIskrianVisualRecipe(ref recipe, variant);
                break;
            default:
                recipe.ShirtColor = WorkerPortraitShirtColors[Mathf.Abs(driver.DriverId) % WorkerPortraitShirtColors.Length];
                recipe.TrouserColor = new Color(0.18f, 0.22f, 0.30f, 1f);
                recipe.AccentColor = GetWorkerRaceAccentColor(driver.Race);
                recipe.SecondaryAccentColor = GetWorkerRaceColor(driver.Race);
                recipe.TrimColor = Color.Lerp(recipe.ShirtColor, Color.white, 0.18f);
                recipe.BeltColor = new Color(0.16f, 0.12f, 0.10f, 1f);
                break;
        }

        return recipe;
    }

    private static void ApplyRovianVisualRecipe(ref DriverVisualRecipe recipe, int variant)
    {
        recipe.ShirtColor = variant == 0
            ? new Color(0.22f, 0.43f, 0.62f, 1f)
            : new Color(0.18f, 0.34f, 0.43f, 1f);
        recipe.TrouserColor = variant == 0
            ? new Color(0.16f, 0.20f, 0.31f, 1f)
            : new Color(0.12f, 0.17f, 0.22f, 1f);
        recipe.AccentColor = new Color(1f, 0.78f, 0.22f, 1f);
        recipe.SecondaryAccentColor = new Color(0.50f, 0.70f, 0.86f, 1f);
        recipe.TrimColor = new Color(0.80f, 0.88f, 0.92f, 1f);
        recipe.BeltColor = new Color(0.17f, 0.13f, 0.11f, 1f);

        if (variant == 1)
        {
            recipe.BodyScale += new Vector3(0.015f, 0.015f, 0.015f);
            recipe.ShadowScale = new Vector3(0.38f, 0.008f, 0.36f);
        }
    }

    private static void ApplyZelenVisualRecipe(ref DriverVisualRecipe recipe, int variant)
    {
        recipe.ShirtColor = variant == 0
            ? new Color(0.28f, 0.50f, 0.29f, 1f)
            : new Color(0.37f, 0.45f, 0.28f, 1f);
        recipe.TrouserColor = variant == 0
            ? new Color(0.24f, 0.18f, 0.12f, 1f)
            : new Color(0.31f, 0.23f, 0.16f, 1f);
        recipe.AccentColor = new Color(0.68f, 0.82f, 0.42f, 1f);
        recipe.SecondaryAccentColor = new Color(0.58f, 0.40f, 0.25f, 1f);
        recipe.TrimColor = new Color(0.78f, 0.65f, 0.45f, 1f);
        recipe.BeltColor = new Color(0.22f, 0.15f, 0.10f, 1f);

        if (variant == 0)
        {
            recipe.BodyScale += new Vector3(0.005f, 0.02f, 0.005f);
            recipe.HeadwearPosition += new Vector3(0f, 0.01f, 0f);
        }
        else
        {
            recipe.ArmScale += new Vector3(0.005f, -0.02f, 0.005f);
            recipe.ShadowScale = new Vector3(0.36f, 0.008f, 0.35f);
        }
    }

    private static void ApplyIskrianVisualRecipe(ref DriverVisualRecipe recipe, int variant)
    {
        recipe.ShirtColor = variant == 0
            ? new Color(0.31f, 0.25f, 0.48f, 1f)
            : new Color(0.42f, 0.30f, 0.22f, 1f);
        recipe.TrouserColor = variant == 0
            ? new Color(0.13f, 0.15f, 0.25f, 1f)
            : new Color(0.20f, 0.16f, 0.23f, 1f);
        recipe.AccentColor = new Color(0.95f, 0.64f, 0.22f, 1f);
        recipe.SecondaryAccentColor = new Color(0.39f, 0.78f, 0.84f, 1f);
        recipe.TrimColor = new Color(0.88f, 0.82f, 0.64f, 1f);
        recipe.BeltColor = new Color(0.12f, 0.10f, 0.16f, 1f);

        if (variant == 1)
        {
            recipe.BodyScale += new Vector3(0.01f, 0.03f, 0.01f);
            recipe.HeadScale += new Vector3(0.005f, 0.005f, 0.005f);
        }
    }

    private void BuildDriverBaseHair(Transform headwearAnchor, DriverAgent driver, DriverVisualRecipe recipe, int variant)
    {
        bool isFemale = driver.Gender == WorkerGender.Female;
        if (isFemale)
        {
            CreateDriverModelPart(headwearAnchor, "DriverHairBun", PrimitiveType.Sphere, new Vector3(0f, 0f, -0.05f), Vector3.zero, new Vector3(0.14f, 0.14f, 0.14f), recipe.HairColor, VisualSmoothnessFabric);
            if (variant == 1)
            {
                CreateDriverModelPart(headwearAnchor, "DriverSideHair", PrimitiveType.Cube, new Vector3(-0.11f, -0.05f, 0.01f), new Vector3(0f, 0f, 10f), new Vector3(0.055f, 0.20f, 0.055f), recipe.HairColor, VisualSmoothnessFabric);
            }
        }
        else
        {
            CreateDriverModelPart(headwearAnchor, "DriverHairCap", PrimitiveType.Cube, new Vector3(0f, -0.01f, 0f), Vector3.zero, new Vector3(0.23f, 0.07f, 0.23f), recipe.HairColor, VisualSmoothnessFabric);
        }
    }

    private void BuildDriverRaceModelDetails(Transform bodyAnchor, Transform headAnchor, Transform headwearAnchor, WorkerRaceKind race, int variant, DriverVisualRecipe recipe)
    {
        switch (race)
        {
            case WorkerRaceKind.Rovian:
                BuildRovianDriverDetails(bodyAnchor, headwearAnchor, variant, recipe);
                break;
            case WorkerRaceKind.Zelen:
                BuildZelenDriverDetails(bodyAnchor, headAnchor, headwearAnchor, variant, recipe);
                break;
            case WorkerRaceKind.Iskrian:
                BuildIskrianDriverDetails(bodyAnchor, headwearAnchor, variant, recipe);
                break;
        }
    }

    private void BuildRovianDriverDetails(Transform bodyAnchor, Transform headwearAnchor, int variant, DriverVisualRecipe recipe)
    {
        if (variant == 0)
        {
            CreateDriverModelPart(bodyAnchor, "RovianRouteStripe", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0.15f), Vector3.zero, new Vector3(0.055f, 0.43f, 0.026f), recipe.AccentColor, VisualSmoothnessFabric);
            CreateDriverModelPart(bodyAnchor, "RovianChestBadge", PrimitiveType.Cube, new Vector3(0.085f, 0.12f, 0.17f), Vector3.zero, new Vector3(0.065f, 0.055f, 0.026f), recipe.TrimColor, VisualSmoothnessFabric);
            CreateDriverModelPart(headwearAnchor, "RovianDriverCap", PrimitiveType.Cube, new Vector3(0f, 0.005f, 0f), Vector3.zero, new Vector3(0.27f, 0.055f, 0.24f), recipe.ShirtColor, VisualSmoothnessFabric);
            CreateDriverModelPart(headwearAnchor, "RovianCapBrim", PrimitiveType.Cube, new Vector3(0f, -0.015f, 0.13f), Vector3.zero, new Vector3(0.22f, 0.028f, 0.10f), recipe.AccentColor, VisualSmoothnessFabric);
            return;
        }

        CreateDriverModelPart(bodyAnchor, "RovianDispatchVest", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0.15f), Vector3.zero, new Vector3(0.23f, 0.38f, 0.028f), Color.Lerp(recipe.ShirtColor, Color.black, 0.16f), VisualSmoothnessFabric);
        CreateDriverModelPart(bodyAnchor, "RovianShoulderL", PrimitiveType.Cube, new Vector3(-0.14f, 0.17f, 0.03f), new Vector3(0f, 0f, -12f), new Vector3(0.11f, 0.055f, 0.18f), recipe.SecondaryAccentColor, VisualSmoothnessFabric);
        CreateDriverModelPart(bodyAnchor, "RovianShoulderR", PrimitiveType.Cube, new Vector3(0.14f, 0.17f, 0.03f), new Vector3(0f, 0f, 12f), new Vector3(0.11f, 0.055f, 0.18f), recipe.SecondaryAccentColor, VisualSmoothnessFabric);
        CreateDriverModelPart(bodyAnchor, "RovianSatchel", PrimitiveType.Cube, new Vector3(-0.16f, -0.03f, 0.06f), new Vector3(0f, 0f, -12f), new Vector3(0.09f, 0.14f, 0.07f), recipe.BeltColor, VisualSmoothnessFabric);
        CreateDriverModelPart(headwearAnchor, "RovianSoftCap", PrimitiveType.Cube, new Vector3(0f, 0f, 0.015f), Vector3.zero, new Vector3(0.25f, 0.075f, 0.25f), recipe.AccentColor, VisualSmoothnessFabric);
    }

    private void BuildZelenDriverDetails(Transform bodyAnchor, Transform headAnchor, Transform headwearAnchor, int variant, DriverVisualRecipe recipe)
    {
        if (variant == 0)
        {
            CreateDriverModelPart(bodyAnchor, "ZelenFieldScarf", PrimitiveType.Cube, new Vector3(0f, 0.23f, 0.08f), Vector3.zero, new Vector3(0.25f, 0.055f, 0.13f), recipe.TrimColor, VisualSmoothnessFabric);
            CreateDriverModelPart(bodyAnchor, "ZelenLeafPin", PrimitiveType.Cube, new Vector3(0.09f, 0.11f, 0.17f), new Vector3(0f, 0f, -28f), new Vector3(0.07f, 0.035f, 0.025f), recipe.AccentColor, VisualSmoothnessFabric);
            CreateDriverModelPart(headAnchor, "ZelenHoodBack", PrimitiveType.Sphere, new Vector3(0f, 0.01f, -0.12f), Vector3.zero, new Vector3(0.28f, 0.24f, 0.10f), Color.Lerp(recipe.ShirtColor, Color.black, 0.08f), VisualSmoothnessFabric);
            CreateDriverModelPart(headwearAnchor, "ZelenHoodBand", PrimitiveType.Cube, new Vector3(0f, -0.035f, 0.07f), Vector3.zero, new Vector3(0.24f, 0.045f, 0.12f), recipe.AccentColor, VisualSmoothnessFabric);
            return;
        }

        CreateDriverModelPart(bodyAnchor, "ZelenCareApron", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0.16f), Vector3.zero, new Vector3(0.22f, 0.40f, 0.030f), recipe.TrimColor, VisualSmoothnessFabric);
        CreateDriverModelPart(bodyAnchor, "ZelenApronPocket", PrimitiveType.Cube, new Vector3(0f, -0.07f, 0.18f), Vector3.zero, new Vector3(0.12f, 0.07f, 0.024f), recipe.SecondaryAccentColor, VisualSmoothnessFabric);
        CreateDriverModelPart(bodyAnchor, "ZelenWovenShawlL", PrimitiveType.Cube, new Vector3(-0.12f, 0.18f, 0.04f), new Vector3(0f, 0f, -20f), new Vector3(0.09f, 0.21f, 0.045f), recipe.SecondaryAccentColor, VisualSmoothnessFabric);
        CreateDriverModelPart(bodyAnchor, "ZelenWovenShawlR", PrimitiveType.Cube, new Vector3(0.12f, 0.18f, 0.04f), new Vector3(0f, 0f, 20f), new Vector3(0.09f, 0.21f, 0.045f), recipe.SecondaryAccentColor, VisualSmoothnessFabric);
        CreateDriverModelPart(headwearAnchor, "ZelenClothWrap", PrimitiveType.Cube, new Vector3(0f, -0.02f, 0f), Vector3.zero, new Vector3(0.27f, 0.055f, 0.22f), recipe.TrimColor, VisualSmoothnessFabric);
    }

    private void BuildIskrianDriverDetails(Transform bodyAnchor, Transform headwearAnchor, int variant, DriverVisualRecipe recipe)
    {
        if (variant == 0)
        {
            CreateDriverModelPart(bodyAnchor, "IskrianSignalSash", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0.17f), new Vector3(0f, 0f, -24f), new Vector3(0.055f, 0.47f, 0.026f), recipe.SecondaryAccentColor, VisualSmoothnessFabric);
            CreateDriverModelPart(bodyAnchor, "IskrianSparkBadge", PrimitiveType.Sphere, new Vector3(0.085f, 0.13f, 0.18f), Vector3.zero, new Vector3(0.055f, 0.055f, 0.024f), recipe.AccentColor, VisualSmoothnessFabric);
            CreateDriverModelPart(headwearAnchor, "IskrianHeadband", PrimitiveType.Cube, new Vector3(0f, -0.035f, 0.03f), Vector3.zero, new Vector3(0.26f, 0.035f, 0.21f), recipe.SecondaryAccentColor, VisualSmoothnessFabric);
            CreateDriverModelPart(headwearAnchor, "IskrianSparkClip", PrimitiveType.Cube, new Vector3(0.11f, -0.02f, 0.07f), new Vector3(0f, 0f, 45f), new Vector3(0.055f, 0.055f, 0.025f), recipe.AccentColor, VisualSmoothnessFabric);
            return;
        }

        CreateDriverModelPart(bodyAnchor, "IskrianLongCoatFront", PrimitiveType.Cube, new Vector3(0f, -0.05f, 0.15f), Vector3.zero, new Vector3(0.24f, 0.44f, 0.030f), Color.Lerp(recipe.ShirtColor, Color.black, 0.10f), VisualSmoothnessFabric);
        CreateDriverModelPart(bodyAnchor, "IskrianMemoryCord", PrimitiveType.Cube, new Vector3(-0.07f, 0.05f, 0.18f), new Vector3(0f, 0f, 22f), new Vector3(0.045f, 0.40f, 0.024f), recipe.AccentColor, VisualSmoothnessFabric);
        CreateDriverModelPart(bodyAnchor, "IskrianSleeveGlowL", PrimitiveType.Cube, new Vector3(-0.14f, 0.01f, 0.08f), Vector3.zero, new Vector3(0.035f, 0.20f, 0.035f), recipe.SecondaryAccentColor, VisualSmoothnessFabric);
        CreateDriverModelPart(bodyAnchor, "IskrianSleeveGlowR", PrimitiveType.Cube, new Vector3(0.14f, 0.01f, 0.08f), Vector3.zero, new Vector3(0.035f, 0.20f, 0.035f), recipe.SecondaryAccentColor, VisualSmoothnessFabric);
        CreateDriverModelPart(headwearAnchor, "IskrianArchivistBand", PrimitiveType.Cube, new Vector3(0f, -0.03f, 0.01f), Vector3.zero, new Vector3(0.27f, 0.045f, 0.23f), recipe.AccentColor, VisualSmoothnessFabric);
    }

    private Transform CreateDriverVisualAnchor(string name, Transform parent, Vector3 localPosition)
    {
        Transform anchor = new GameObject(name).transform;
        anchor.SetParent(parent, false);
        anchor.localPosition = localPosition;
        anchor.localRotation = Quaternion.identity;
        anchor.localScale = Vector3.one;
        return anchor;
    }

    private GameObject CreateDriverModelPart(Transform parent, string name, PrimitiveType primitive, Vector3 localPosition, Vector3 localEuler, Vector3 localScale, Color color, float smoothness)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEuler);
        part.transform.localScale = localScale;
        ApplyColor(part, color, smoothness);
        ConfigureShadowVisual(part, smoothness);
        return part;
    }

    private void CreateDriverShadowBlob(Transform parent, DriverVisualRecipe recipe)
    {
        GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shadow.name = "DriverShadowBlob";
        shadow.transform.SetParent(parent, false);
        shadow.transform.localPosition = new Vector3(0f, -0.01f, 0f);
        shadow.transform.localScale = recipe.ShadowScale;
        Renderer renderer = shadow.GetComponent<Renderer>();
        renderer.material = CreateTransparentOverlayMaterial(new Color(0f, 0f, 0f, recipe.ShadowAlpha));
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (shadow.TryGetComponent(out Collider collider))
        {
            Object.Destroy(collider);
        }
    }

    private void CreateDriverCarryProps(DriverAgent driver)
    {
        GameObject fuelCan = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fuelCan.transform.SetParent(driver.DriverVisualRoot, false);
        fuelCan.transform.localPosition = new Vector3(0.18f, 0.42f, 0f);
        fuelCan.transform.localScale = new Vector3(0.14f, 0.2f, 0.1f);
        ApplyColor(fuelCan, new Color(0.9f, 0.76f, 0.18f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(fuelCan, VisualSmoothnessVehicleMetal);
        driver.DriverFuelCanTransform = fuelCan.transform;
        driver.DriverFuelCanTransform.gameObject.SetActive(false);

        GameObject flashlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flashlight.transform.SetParent(driver.DriverVisualRoot, false);
        flashlight.transform.localPosition = new Vector3(0.24f, 0.57f, 0.1f);
        flashlight.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        flashlight.transform.localScale = new Vector3(0.06f, 0.06f, 0.18f);
        ApplyColor(flashlight, new Color(0.24f, 0.24f, 0.26f), VisualSmoothnessVehicleMetal);
        ConfigureShadowVisual(flashlight, VisualSmoothnessVehicleMetal);
        driver.DriverFlashlightTransform = flashlight.transform;
        driver.DriverFlashlightRenderer = flashlight.GetComponent<Renderer>();
        driver.DriverFlashlightMaterial = driver.DriverFlashlightRenderer != null ? driver.DriverFlashlightRenderer.material : null;
        if (driver.DriverImportedCitizenVisual && driver.DriverFlashlightRenderer != null)
        {
            driver.DriverFlashlightRenderer.enabled = false;
        }

        GameObject flashlightBeamObject = new("DriverFlashlight");
        flashlightBeamObject.transform.SetParent(driver.DriverFlashlightTransform, false);
        flashlightBeamObject.transform.localPosition = new Vector3(0f, 0f, 0.14f);
        flashlightBeamObject.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
        driver.DriverFlashlightLight = flashlightBeamObject.AddComponent<Light>();
        driver.DriverFlashlightLight.type = LightType.Spot;
        driver.DriverFlashlightLight.color = new Color(1f, 0.72f, 0.38f);
        driver.DriverFlashlightLight.range = 5.2f;
        driver.DriverFlashlightLight.spotAngle = 46f;
        driver.DriverFlashlightLight.innerSpotAngle = 22f;
        driver.DriverFlashlightLight.shadows = LightShadows.None;
        driver.DriverFlashlightLight.intensity = 0f;
        driver.DriverFlashlightLight.enabled = false;
        driver.DriverFlashlightHaloLight = CreateDriverFlashlightHalo(driver.DriverVisualRoot);
    }
}
