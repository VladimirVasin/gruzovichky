using UnityEngine;

public partial class GameBootstrap
{
    private enum ImportedDriverPoseKind
    {
        Neutral,
        Walk,
        Conversation,
        Sitting,
        TrashMeal,
        CityParkExercise,
        CityParkLookAround,
        CityParkRest,
        Smoking,
        PhoneCall,
        PettingCat,
        CleanerSweep,
        LumberChop,
        LumberPlant
    }

    private static readonly string[] ImportedIskrianMaleHairFlowPrefixes =
    {
        "Hair_BackSweep",
        "Hair_CopperGlint"
    };

    private static readonly string[] ImportedIskrianFemaleHairFlowPrefixes =
    {
        "Hair_Braid_Segment_01",
        "Hair_Braid_Segment_02",
        "Hair_Braid_Segment_03",
        "Hair_Long_Back",
        "Hair_Long_Lock_Left",
        "Hair_Long_Lock_Right",
        "Hair_CopperHighlight_Back",
        "Hair_CopperHighlight_FrontLeft"
    };

    private static readonly string[] ImportedZelenMaleHairFlowPrefixes =
    {
        "Hair_Back",
        "Hair_Tuft_Front"
    };

    private static readonly string[] ImportedZelenFemaleHairFlowPrefixes =
    {
        "Hair_Braid",
        "Hair_GreenHighlight",
        "Hair_Lock",
        "Hair_Long_Back",
        "Hair_Tuft_Front"
    };

    private static readonly string[] ImportedRovianMaleHairFlowPrefixes =
    {
        "Hair_Side"
    };

    private static readonly string[] ImportedRovianFemaleHairFlowPrefixes =
    {
        "Hair_Bun_Back",
        "Hair_Lock"
    };

    private static readonly string[] ImportedIskrianMaleClothFlowPrefixes =
    {
        "LongVest",
        "Jacket_GoldThread",
        "Jacket_Hem",
        "Jacket_LeftSidePanel",
        "Jacket_RightSidePanel"
    };

    private static readonly string[] ImportedIskrianFemaleClothFlowPrefixes =
    {
        "LightCloak_BackPanel",
        "LightCloak_Edge_Left",
        "LightCloak_Edge_Right",
        "ShortJacket",
        "Torso_Tunic",
        "Tunic"
    };

    private static readonly string[] ImportedZelenMaleClothFlowPrefixes =
    {
        "FieldShirt",
        "Satchel",
        "ShoulderCloth",
        "Vest"
    };

    private static readonly string[] ImportedZelenFemaleClothFlowPrefixes =
    {
        "Apron",
        "FieldTunic",
        "Satchel",
        "Shawl",
        "ShoulderCloth",
        "Tunic_LeafVein",
        "Vest"
    };

    private static readonly string[] ImportedRovianMaleClothFlowPrefixes =
    {
        "Jacket",
        "RouteStripe",
        "Shirt_FrontVisible",
        "ShoulderPatch"
    };

    private static readonly string[] ImportedRovianFemaleClothFlowPrefixes =
    {
        "Jacket",
        "RouteStripe",
        "ShoulderPatch",
        "SideSatchel",
        "Vest"
    };

    private static readonly string[] ImportedIskrianMaleGlowPrefixes =
    {
        "InnerSpark",
        "ShoulderAccent"
    };

    private static readonly string[] ImportedIskrianFemaleGlowPrefixes =
    {
        "GlowTrim_Collar"
    };

    private static readonly string[] ImportedZelenGlowPrefixes =
    {
        "LeafBadge"
    };

    private static readonly string[] ImportedRovianGlowPrefixes =
    {
        "ChestBadge"
    };

    private static void ResetImportedDriverVisualAnimationHooks(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        driver.DriverImportedCitizenVisual = false;
        driver.DriverImportedCitizenFemaleVisual = false;
        driver.DriverImportedModelTransform = null;
        driver.DriverImportedHairFlowTransforms = null;
        driver.DriverImportedClothFlowTransforms = null;
        driver.DriverImportedGlowTransforms = null;
    }

    private static string[] GetImportedDriverHairFlowPrefixes(WorkerRaceKind race, bool isFemale)
    {
        switch (race)
        {
            case WorkerRaceKind.Rovian:
                return isFemale ? ImportedRovianFemaleHairFlowPrefixes : ImportedRovianMaleHairFlowPrefixes;
            case WorkerRaceKind.Zelen:
                return isFemale ? ImportedZelenFemaleHairFlowPrefixes : ImportedZelenMaleHairFlowPrefixes;
            case WorkerRaceKind.Iskrian:
            default:
                return isFemale ? ImportedIskrianFemaleHairFlowPrefixes : ImportedIskrianMaleHairFlowPrefixes;
        }
    }

    private static string[] GetImportedDriverClothFlowPrefixes(WorkerRaceKind race, bool isFemale)
    {
        switch (race)
        {
            case WorkerRaceKind.Rovian:
                return isFemale ? ImportedRovianFemaleClothFlowPrefixes : ImportedRovianMaleClothFlowPrefixes;
            case WorkerRaceKind.Zelen:
                return isFemale ? ImportedZelenFemaleClothFlowPrefixes : ImportedZelenMaleClothFlowPrefixes;
            case WorkerRaceKind.Iskrian:
            default:
                return isFemale ? ImportedIskrianFemaleClothFlowPrefixes : ImportedIskrianMaleClothFlowPrefixes;
        }
    }

    private static string[] GetImportedDriverGlowPrefixes(WorkerRaceKind race, bool isFemale)
    {
        switch (race)
        {
            case WorkerRaceKind.Rovian:
                return ImportedRovianGlowPrefixes;
            case WorkerRaceKind.Zelen:
                return ImportedZelenGlowPrefixes;
            case WorkerRaceKind.Iskrian:
            default:
                return isFemale ? ImportedIskrianFemaleGlowPrefixes : ImportedIskrianMaleGlowPrefixes;
        }
    }

    private static Transform[] CreateImportedDriverFlowPivots(
        Transform parent,
        Transform searchRoot,
        string[] partPrefixes,
        float verticalPivot)
    {
        if (parent == null || searchRoot == null || partPrefixes == null || partPrefixes.Length == 0)
        {
            return null;
        }

        System.Collections.Generic.List<Transform> pivots = new();
        for (int i = 0; i < partPrefixes.Length; i++)
        {
            string prefix = partPrefixes[i];
            if (string.IsNullOrEmpty(prefix))
            {
                continue;
            }

            System.Collections.Generic.List<Transform> parts = FindImportedDriverParts(searchRoot, name => DriverPartNameStartsWith(name, prefix));
            Transform pivot = CreateImportedDriverRigPivot(parent, "DriverFlow_" + prefix, parts, verticalPivot);
            if (pivot != null)
            {
                pivots.Add(pivot);
            }
        }

        return pivots.Count > 0 ? pivots.ToArray() : null;
    }

    private static void CreateImportedDriverLegMotionPivots(DriverAgent driver, Transform rigRoot)
    {
        if (driver == null || rigRoot == null)
        {
            return;
        }

        driver.DriverLeftLegTransform = CreateImportedDriverRigPivot(
            rigRoot,
            "DriverImportedLeftLegMotionPivot",
            FindImportedDriverParts(rigRoot, IsImportedDriverLeftLegPartName),
            0.88f);
        driver.DriverRightLegTransform = CreateImportedDriverRigPivot(
            rigRoot,
            "DriverImportedRightLegMotionPivot",
            FindImportedDriverParts(rigRoot, IsImportedDriverRightLegPartName),
            0.88f);
    }

    private void ApplyImportedDriverPoseMotion(
        DriverAgent driver,
        ImportedDriverPoseKind pose,
        float phase = 0f,
        float swing = 0f,
        float bob = 0f)
    {
        if (driver == null || !driver.DriverImportedCitizenVisual)
        {
            return;
        }

        if (driver.DriverImportedCitizenFemaleVisual)
        {
            ApplyImportedFemaleDriverPoseMotion(driver, pose, phase, swing, bob);
            return;
        }

        ApplyImportedMaleDriverPoseMotion(driver, pose, phase, swing, bob);
    }

    private void ApplyImportedMaleDriverPoseMotion(DriverAgent driver, ImportedDriverPoseKind pose, float phase, float swing, float bob)
    {
        float t = Time.time + driver.DriverId * 0.19f;
        float breath = Mathf.Sin(t * 1.25f);
        float work = Mathf.Lerp(-1f, 1f, Mathf.Clamp01(phase));
        float hairX = breath * 0.8f;
        float hairZ = breath * 0.5f;
        float clothX = -breath * 1.1f;
        float clothZ = breath * 0.8f;
        float glow = 0.025f + Mathf.Abs(breath) * 0.015f + Mathf.Abs(bob) * 0.12f;

        switch (pose)
        {
            case ImportedDriverPoseKind.Walk:
                AddImportedDriverLocalRotation(driver.DriverBodyTransform, swing * 0.9f, 0f, -swing * 0.8f);
                AddImportedDriverLocalRotation(driver.DriverHeadTransform, -swing * 0.4f, -swing * 0.9f, 0f);
                AddImportedDriverLocalRotation(driver.DriverLeftArmTransform, 0f, 0f, -swing * 2.5f);
                AddImportedDriverLocalRotation(driver.DriverRightArmTransform, 0f, 0f, swing * 2.5f);
                hairX = -swing * 2.0f;
                clothX = Mathf.Abs(swing) * 3.2f;
                clothZ = -swing * 2.2f;
                glow += Mathf.Abs(swing) * 0.03f;
                break;
            case ImportedDriverPoseKind.Conversation:
                AddImportedDriverLocalRotation(driver.DriverHeadTransform, 0f, Mathf.Sin(t * 1.7f) * 5f, 0f);
                AddImportedDriverLocalRotation(driver.DriverLeftArmTransform, 0f, 0f, Mathf.Sin(t * 1.9f) * 4f);
                clothZ = Mathf.Sin(t * 1.1f) * 1.4f;
                glow += 0.025f;
                break;
            case ImportedDriverPoseKind.Sitting:
                AddImportedDriverLocalRotation(driver.DriverBodyTransform, -1.5f, 0f, 0f);
                clothX = 5.5f + breath * 0.8f;
                clothZ = breath * 0.7f;
                break;
            case ImportedDriverPoseKind.TrashMeal:
                AddImportedDriverLocalRotation(driver.DriverHeadTransform, 2f, Mathf.Sin(t * 5.0f) * 2.5f, 0f);
                clothX = 6f + Mathf.Sin(t * 8f) * 1.4f;
                glow += 0.015f;
                break;
            case ImportedDriverPoseKind.CityParkExercise:
                hairX = Mathf.Sin(t * 1.8f) * 2.6f;
                clothX = Mathf.Sin(t * 1.9f) * 4.5f;
                clothZ = Mathf.Sin(t * 0.9f) * 2.5f;
                break;
            case ImportedDriverPoseKind.CityParkLookAround:
                AddImportedDriverLocalRotation(driver.DriverHeadTransform, 0f, Mathf.Sin(t * 0.65f) * 3f, 0f);
                hairZ = Mathf.Sin(t * 0.8f) * 1.4f;
                clothZ = Mathf.Sin(t * 0.35f) * 1.2f;
                break;
            case ImportedDriverPoseKind.CityParkRest:
                clothX = 7.5f + breath * 0.5f;
                hairX = breath * 0.5f;
                break;
            case ImportedDriverPoseKind.Smoking:
                AddImportedDriverLocalRotation(driver.DriverRightArmTransform, 0f, 0f, Mathf.Sin(t * 1.1f) * 2.2f);
                glow += 0.02f;
                break;
            case ImportedDriverPoseKind.PhoneCall:
                AddImportedDriverLocalRotation(driver.DriverHeadTransform, 0f, Mathf.Sin(t * 1.4f) * 3.5f, 0f);
                AddImportedDriverLocalRotation(driver.DriverRightArmTransform, 0f, 0f, -5f);
                break;
            case ImportedDriverPoseKind.PettingCat:
                clothX = 5f + Mathf.Sin(t * 2.8f) * 1.8f;
                AddImportedDriverLocalRotation(driver.DriverLeftArmTransform, 0f, 0f, -3f);
                break;
            case ImportedDriverPoseKind.CleanerSweep:
                AddImportedDriverLocalRotation(driver.DriverBodyTransform, 0f, 0f, work * 2.5f);
                hairX = work * 1.4f;
                clothX = Mathf.Abs(work) * 3.6f;
                clothZ = -work * 2.8f;
                glow += Mathf.Abs(work) * 0.025f;
                break;
            case ImportedDriverPoseKind.LumberChop:
                AddImportedDriverLocalRotation(driver.DriverBodyTransform, 0f, 0f, work * 2.0f);
                hairX = -work * 2.0f;
                clothX = Mathf.Abs(work) * 5.4f;
                clothZ = work * 3.0f;
                glow += Mathf.Abs(work) * 0.035f;
                break;
            case ImportedDriverPoseKind.LumberPlant:
                clothX = 8f + Mathf.Abs(work) * 2.2f;
                hairX = work * 0.8f;
                break;
        }

        SetImportedDriverFlowRotation(driver.DriverImportedHairFlowTransforms, hairX, 0f, hairZ, 0.45f);
        SetImportedDriverFlowRotation(driver.DriverImportedClothFlowTransforms, clothX, 0f, clothZ, 0.75f);
        SetImportedDriverGlowScale(driver.DriverImportedGlowTransforms, glow, t);
    }

    private void ApplyImportedFemaleDriverPoseMotion(DriverAgent driver, ImportedDriverPoseKind pose, float phase, float swing, float bob)
    {
        float t = Time.time + driver.DriverId * 0.23f + 0.8f;
        float breath = Mathf.Sin(t * 1.45f);
        float work = Mathf.Lerp(-1f, 1f, Mathf.Clamp01(phase));
        float hairX = breath * 1.6f;
        float hairY = breath * 0.8f;
        float hairZ = Mathf.Sin(t * 0.9f) * 1.2f;
        float clothX = -breath * 1.6f;
        float clothZ = Mathf.Sin(t * 1.1f) * 1.4f;
        float glow = 0.03f + Mathf.Abs(breath) * 0.02f + Mathf.Abs(bob) * 0.16f;

        switch (pose)
        {
            case ImportedDriverPoseKind.Walk:
                AddImportedDriverLocalRotation(driver.DriverBodyTransform, swing * 0.45f, 0f, -swing * 1.1f);
                AddImportedDriverLocalRotation(driver.DriverHeadTransform, -swing * 0.25f, -swing * 1.6f, swing * 0.5f);
                AddImportedDriverLocalRotation(driver.DriverLeftArmTransform, 0f, 0f, -swing * 1.5f);
                AddImportedDriverLocalRotation(driver.DriverRightArmTransform, 0f, 0f, swing * 1.5f);
                hairX = -swing * 4.8f;
                hairY = swing * 1.8f;
                hairZ = -swing * 3.2f;
                clothX = Mathf.Abs(swing) * 4.8f;
                clothZ = -swing * 3.8f;
                glow += Mathf.Abs(swing) * 0.035f;
                break;
            case ImportedDriverPoseKind.Conversation:
                AddImportedDriverLocalRotation(driver.DriverHeadTransform, 0f, Mathf.Sin(t * 1.9f) * 6.5f, Mathf.Sin(t * 1.3f) * 1.2f);
                AddImportedDriverLocalRotation(driver.DriverLeftArmTransform, 0f, 0f, Mathf.Sin(t * 2.0f) * 2.5f);
                hairX = Mathf.Sin(t * 1.7f) * 2.2f;
                hairZ = Mathf.Sin(t * 1.1f) * 2.8f;
                glow += 0.03f;
                break;
            case ImportedDriverPoseKind.Sitting:
                AddImportedDriverLocalRotation(driver.DriverBodyTransform, -1f, 0f, Mathf.Sin(t * 0.45f) * 0.8f);
                hairX = 3.2f + breath * 1.1f;
                clothX = 6.4f + breath * 0.8f;
                clothZ = Mathf.Sin(t * 0.7f) * 1.1f;
                break;
            case ImportedDriverPoseKind.TrashMeal:
                AddImportedDriverLocalRotation(driver.DriverHeadTransform, 1f, Mathf.Sin(t * 4.8f) * 3.2f, 0f);
                hairX = 5.5f + Mathf.Sin(t * 8f) * 2.0f;
                clothX = 6.8f + Mathf.Sin(t * 7f) * 1.6f;
                glow += 0.02f;
                break;
            case ImportedDriverPoseKind.CityParkExercise:
                hairX = Mathf.Sin(t * 1.95f) * 4.2f;
                hairZ = Mathf.Sin(t * 1.45f) * 3.6f;
                clothX = Mathf.Sin(t * 1.7f) * 5.5f;
                clothZ = Mathf.Sin(t * 0.95f) * 3.8f;
                break;
            case ImportedDriverPoseKind.CityParkLookAround:
                AddImportedDriverLocalRotation(driver.DriverHeadTransform, 0f, Mathf.Sin(t * 0.7f) * 4.5f, 0f);
                hairZ = Mathf.Sin(t * 0.8f) * 3.2f;
                clothZ = Mathf.Sin(t * 0.35f) * 1.6f;
                break;
            case ImportedDriverPoseKind.CityParkRest:
                hairX = 4f + breath * 0.9f;
                clothX = 8.2f + breath * 0.6f;
                break;
            case ImportedDriverPoseKind.Smoking:
                AddImportedDriverLocalRotation(driver.DriverRightArmTransform, 0f, 0f, Mathf.Sin(t * 1.2f) * 1.6f);
                hairZ = Mathf.Sin(t * 0.9f) * 1.8f;
                glow += 0.025f;
                break;
            case ImportedDriverPoseKind.PhoneCall:
                AddImportedDriverLocalRotation(driver.DriverHeadTransform, 0f, Mathf.Sin(t * 1.5f) * 5.0f, 0f);
                AddImportedDriverLocalRotation(driver.DriverRightArmTransform, 0f, 0f, -3.5f);
                hairX = 2.2f + Mathf.Sin(t * 1.2f) * 1.4f;
                break;
            case ImportedDriverPoseKind.PettingCat:
                AddImportedDriverLocalRotation(driver.DriverLeftArmTransform, 0f, 0f, -2f);
                hairX = 6f + Mathf.Sin(t * 2.8f) * 1.5f;
                clothX = 5.8f + Mathf.Sin(t * 2.3f) * 1.5f;
                break;
            case ImportedDriverPoseKind.CleanerSweep:
                AddImportedDriverLocalRotation(driver.DriverBodyTransform, 0f, 0f, work * 2.0f);
                hairX = work * 4.6f;
                hairZ = -work * 3.6f;
                clothX = Mathf.Abs(work) * 5.6f;
                clothZ = -work * 4.2f;
                glow += Mathf.Abs(work) * 0.03f;
                break;
            case ImportedDriverPoseKind.LumberChop:
                AddImportedDriverLocalRotation(driver.DriverBodyTransform, 0f, 0f, work * 1.7f);
                hairX = -work * 5.2f;
                hairZ = work * 4.0f;
                clothX = Mathf.Abs(work) * 6.0f;
                clothZ = work * 4.4f;
                glow += Mathf.Abs(work) * 0.035f;
                break;
            case ImportedDriverPoseKind.LumberPlant:
                hairX = 4.5f + work * 1.5f;
                clothX = 8.8f + Mathf.Abs(work) * 2.2f;
                clothZ = -work * 1.4f;
                break;
        }

        SetImportedDriverFlowRotation(driver.DriverImportedHairFlowTransforms, hairX, hairY, hairZ, 0.85f);
        SetImportedDriverFlowRotation(driver.DriverImportedClothFlowTransforms, clothX, 0f, clothZ, 0.95f);
        SetImportedDriverGlowScale(driver.DriverImportedGlowTransforms, glow, t);
    }

    private static void AddImportedDriverLocalRotation(Transform target, float x, float y, float z)
    {
        if (target != null)
        {
            target.localRotation *= Quaternion.Euler(x, y, z);
        }
    }

    private static void SetImportedDriverFlowRotation(Transform[] transforms, float x, float y, float z, float cascade)
    {
        if (transforms == null)
        {
            return;
        }

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null)
            {
                continue;
            }

            float order = i + 1f;
            target.localRotation = Quaternion.Euler(
                x * (1f + order * 0.08f),
                y + cascade * order * 0.35f,
                z + cascade * order);
        }
    }

    private static void SetImportedDriverGlowScale(Transform[] transforms, float amount, float time)
    {
        if (transforms == null)
        {
            return;
        }

        float pulse = 1f + Mathf.Clamp(amount, 0f, 0.12f) * (0.55f + Mathf.Abs(Mathf.Sin(time * 2.2f)) * 0.45f);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null)
            {
                transforms[i].localScale = Vector3.one * pulse;
            }
        }
    }
}
