using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private void BuildImportedDogAnimationRig(
        Transform modelRoot,
        out Transform bodyRig,
        out Transform headRig,
        out Transform tailRig,
        out Transform[] legRigs,
        out Quaternion[] legBaseRotations)
    {
        bodyRig = modelRoot;
        headRig = CreateImportedDogRigPivot(modelRoot, "ImportedDogHeadRig", FindImportedDogParts(modelRoot, IsImportedDogHeadPart), ImportedDogPivotKind.Head);
        tailRig = CreateImportedDogRigPivot(modelRoot, "ImportedDogTailRig", FindImportedDogParts(modelRoot, IsImportedDogTailPart), ImportedDogPivotKind.Tail);

        List<Transform> legs = new();
        AddImportedDogLegRig(modelRoot, new[] { "FrontLeg_L", "Front_Left", "FrontLeft", "Leg_Front_Left" }, "Front_Left", legs);
        AddImportedDogLegRig(modelRoot, new[] { "FrontLeg_R", "Front_Right", "FrontRight", "Leg_Front_Right" }, "Front_Right", legs);
        AddImportedDogLegRig(modelRoot, new[] { "BackLeg_L", "HindLeg_L", "Back_Left", "Hind_Left", "Leg_Back_Left" }, "Back_Left", legs);
        AddImportedDogLegRig(modelRoot, new[] { "BackLeg_R", "HindLeg_R", "Back_Right", "Hind_Right", "Leg_Back_Right" }, "Back_Right", legs);
        legRigs = legs.ToArray();
        legBaseRotations = new Quaternion[legRigs.Length];
        for (int i = 0; i < legRigs.Length; i++)
        {
            legBaseRotations[i] = legRigs[i] != null ? legRigs[i].localRotation : Quaternion.identity;
        }
    }

    private enum ImportedDogPivotKind
    {
        Head,
        Tail,
        Leg
    }

    private void AddImportedDogLegRig(Transform modelRoot, string[] aliases, string suffix, List<Transform> legs)
    {
        List<Transform> parts = FindImportedDogParts(modelRoot, name => IsImportedDogLegPart(name, aliases, suffix));
        Transform rig = CreateImportedDogRigPivot(modelRoot, "ImportedDogLegRig_" + suffix, parts, ImportedDogPivotKind.Leg);
        if (rig != null)
        {
            legs.Add(rig);
        }
    }

    private Transform CreateImportedDogRigPivot(Transform modelRoot, string name, List<Transform> parts, ImportedDogPivotKind kind)
    {
        if (modelRoot == null || parts.Count == 0 || !TryGetImportedDogPartsBounds(parts, out Bounds bounds))
        {
            return null;
        }

        Vector3 pivotPosition = ResolveImportedDogPivot(bounds, kind);
        Transform parent = FindImportedTransform(modelRoot, "Dog_Root") ??
            FindImportedTransform(modelRoot, "Root") ??
            modelRoot;
        GameObject pivotObject = new(name);
        Transform pivot = pivotObject.transform;
        pivot.SetParent(parent, false);
        pivot.position = pivotPosition;
        pivot.rotation = parent.rotation;
        pivot.localScale = Vector3.one;

        for (int i = 0; i < parts.Count; i++)
        {
            Transform part = parts[i];
            if (part != null && part != pivot && !pivot.IsChildOf(part))
            {
                part.SetParent(pivot, true);
            }
        }

        return pivot;
    }

    private static Vector3 ResolveImportedDogPivot(Bounds bounds, ImportedDogPivotKind kind)
    {
        Vector3 center = bounds.center;
        return kind switch
        {
            ImportedDogPivotKind.Head => new Vector3(center.x, bounds.min.y + bounds.size.y * 0.36f, bounds.min.z + bounds.size.z * 0.30f),
            ImportedDogPivotKind.Tail => new Vector3(center.x, bounds.min.y + bounds.size.y * 0.44f, bounds.min.z + bounds.size.z * 0.18f),
            ImportedDogPivotKind.Leg => new Vector3(center.x, bounds.max.y, center.z),
            _ => center
        };
    }

    private static List<Transform> FindImportedDogParts(Transform root, System.Predicate<string> predicate)
    {
        List<Transform> matches = new();
        if (root == null || predicate == null)
        {
            return matches;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current != null && predicate(current.name) && !HasImportedDogMatchedAncestor(current, root, predicate))
            {
                matches.Add(current);
            }
        }

        return matches;
    }

    private static bool HasImportedDogMatchedAncestor(Transform transform, Transform root, System.Predicate<string> predicate)
    {
        Transform parent = transform.parent;
        while (parent != null && parent != root)
        {
            if (predicate(parent.name))
            {
                return true;
            }

            parent = parent.parent;
        }

        return false;
    }

    private static bool TryGetImportedDogPartsBounds(List<Transform> parts, out Bounds bounds)
    {
        bounds = default;
        bool hasBounds = false;
        for (int i = 0; i < parts.Count; i++)
        {
            Renderer[] renderers = parts[i] != null ? parts[i].GetComponentsInChildren<Renderer>(true) : null;
            if (renderers == null)
            {
                continue;
            }

            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null)
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

    private static bool IsImportedDogHeadPart(string name)
    {
        return !string.IsNullOrEmpty(name) &&
            (name.StartsWith("Head", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Ear_", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Eye", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Dog_Eye", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Nose", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Dog_Nose", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsImportedDogTailPart(string name)
    {
        return !string.IsNullOrEmpty(name) &&
            (name.StartsWith("Tail", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Tail_Base", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Tail_Tip", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsImportedDogLegPart(string name, string[] aliases, string suffix)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        for (int i = 0; i < aliases.Length; i++)
        {
            if (name.StartsWith(aliases[i], System.StringComparison.OrdinalIgnoreCase) ||
                name.IndexOf(aliases[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        string pawName = suffix.StartsWith("Front", System.StringComparison.OrdinalIgnoreCase)
            ? "FrontPaw_" + (suffix.EndsWith("Left", System.StringComparison.OrdinalIgnoreCase) ? "L" : "R")
            : "BackPaw_" + (suffix.EndsWith("Left", System.StringComparison.OrdinalIgnoreCase) ? "L" : "R");
        return name.StartsWith(pawName, System.StringComparison.OrdinalIgnoreCase) ||
               name.IndexOf(pawName, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static void ApplyDogBodyScale(AmbientDogData dog, Vector3 proceduralScale, Vector3 importedMultiplier)
    {
        if (dog?.BodyTransform == null)
        {
            return;
        }

        dog.BodyTransform.localScale = dog.UsesImportedModel
            ? Vector3.Scale(dog.BodyBaseScale, importedMultiplier)
            : proceduralScale;
    }

    private static void ApplyDogHeadMotion(AmbientDogData dog, Quaternion motion)
    {
        if (dog?.HeadTransform == null)
        {
            return;
        }

        dog.HeadTransform.localRotation = dog.UsesImportedModel
            ? dog.HeadBaseRotation * motion
            : motion;
    }

    private static void ApplyDogTailMotion(AmbientDogData dog, Quaternion importedMotion, Quaternion proceduralMotion)
    {
        if (dog?.TailTransform == null)
        {
            return;
        }

        dog.TailTransform.localRotation = dog.UsesImportedModel
            ? dog.TailBaseRotation * importedMotion
            : proceduralMotion;
    }

    private static void AnimateDogLegs(AmbientDogData dog, float time, float speed, float amplitude, float crouch)
    {
        if (dog == null || dog.LegTransforms == null || dog.LegBaseRotations == null)
        {
            return;
        }

        int count = Mathf.Min(dog.LegTransforms.Length, dog.LegBaseRotations.Length);
        for (int i = 0; i < count; i++)
        {
            Transform leg = dog.LegTransforms[i];
            if (leg == null)
            {
                continue;
            }

            bool diagonalA = i == 0 || i == 3;
            float phase = diagonalA ? 0f : Mathf.PI;
            float swing = Mathf.Sin(time * speed + dog.AnimationPhase + phase) * amplitude;
            float side = Mathf.Sin(time * speed * 0.5f + dog.AnimationPhase + phase) * amplitude * 0.08f;
            leg.localRotation = dog.LegBaseRotations[i] * Quaternion.Euler(crouch + swing, 0f, side);
        }
    }
}
