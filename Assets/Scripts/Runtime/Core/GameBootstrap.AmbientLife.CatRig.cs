using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private void BuildImportedCatAnimationRig(
        Transform modelRoot,
        out Transform bodyRig,
        out Transform headRig,
        out Transform tailRig,
        out Transform[] legRigs,
        out Quaternion[] legBaseRotations)
    {
        bodyRig = modelRoot;
        headRig = CreateImportedCatRigPivot(modelRoot, "ImportedCatHeadRig", FindImportedCatParts(modelRoot, IsImportedCatHeadPart), ImportedCatPivotKind.Head);
        tailRig = CreateImportedCatRigPivot(modelRoot, "ImportedCatTailRig", FindImportedCatParts(modelRoot, IsImportedCatTailPart), ImportedCatPivotKind.Tail);

        List<Transform> legs = new();
        AddImportedCatLegRig(modelRoot, "Front_Left", legs);
        AddImportedCatLegRig(modelRoot, "Front_Right", legs);
        AddImportedCatLegRig(modelRoot, "Back_Left", legs);
        AddImportedCatLegRig(modelRoot, "Back_Right", legs);
        legRigs = legs.ToArray();
        legBaseRotations = new Quaternion[legRigs.Length];
        for (int i = 0; i < legRigs.Length; i++)
        {
            legBaseRotations[i] = legRigs[i] != null ? legRigs[i].localRotation : Quaternion.identity;
        }
    }

    private enum ImportedCatPivotKind
    {
        Center,
        Head,
        Tail,
        Leg
    }

    private void AddImportedCatLegRig(Transform modelRoot, string suffix, List<Transform> legs)
    {
        List<Transform> parts = FindImportedCatParts(modelRoot, name =>
            name.StartsWith("Leg_" + suffix, System.StringComparison.OrdinalIgnoreCase) ||
            name.IndexOf("Sock_" + suffix, System.StringComparison.OrdinalIgnoreCase) >= 0);
        Transform rig = CreateImportedCatRigPivot(modelRoot, "ImportedCatLegRig_" + suffix, parts, ImportedCatPivotKind.Leg);
        if (rig != null)
        {
            legs.Add(rig);
        }
    }

    private Transform CreateImportedCatRigPivot(Transform modelRoot, string name, List<Transform> parts, ImportedCatPivotKind kind)
    {
        if (modelRoot == null || parts.Count == 0 || !TryGetImportedCatPartsBounds(parts, out Bounds bounds))
        {
            return null;
        }

        Vector3 pivotPosition = ResolveImportedCatPivot(bounds, kind);
        Transform parent = FindImportedTransform(modelRoot, "Cat_Root") ?? modelRoot;
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

    private static Vector3 ResolveImportedCatPivot(Bounds bounds, ImportedCatPivotKind kind)
    {
        Vector3 center = bounds.center;
        return kind switch
        {
            ImportedCatPivotKind.Head => new Vector3(center.x, bounds.min.y + bounds.size.y * 0.38f, bounds.min.z + bounds.size.z * 0.32f),
            ImportedCatPivotKind.Tail => new Vector3(center.x, bounds.min.y + bounds.size.y * 0.48f, bounds.max.z),
            ImportedCatPivotKind.Leg => new Vector3(center.x, bounds.max.y, center.z),
            _ => center
        };
    }

    private static List<Transform> FindImportedCatParts(Transform root, System.Predicate<string> predicate)
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
            if (current != null && predicate(current.name) && !HasImportedCatMatchedAncestor(current, root, predicate))
            {
                matches.Add(current);
            }
        }

        return matches;
    }

    private static bool HasImportedCatMatchedAncestor(Transform transform, Transform root, System.Predicate<string> predicate)
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

    private static bool TryGetImportedCatPartsBounds(List<Transform> parts, out Bounds bounds)
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
                if (renderers[r] == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderers[r].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderers[r].bounds);
                }
            }
        }

        return hasBounds;
    }

    private static bool IsImportedCatHeadPart(string name)
    {
        return !string.IsNullOrEmpty(name) &&
            (name.StartsWith("Head", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Ear_", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Eye_", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Nose", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Whiskers_", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("FurPatch_Muzzle", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("FurStripe_Head", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsImportedCatTailPart(string name)
    {
        return !string.IsNullOrEmpty(name) &&
            (name.StartsWith("Tail", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("FurStripe_Tail", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("FurPatch_TailTip", System.StringComparison.OrdinalIgnoreCase));
    }

    private static void AnimateImportedCatLegs(AmbientCatData cat, float time, float speed, float amplitude, float crouch)
    {
        if (cat == null || !cat.UsesImportedModel || cat.LegTransforms == null || cat.LegBaseRotations == null)
        {
            return;
        }

        int count = Mathf.Min(cat.LegTransforms.Length, cat.LegBaseRotations.Length);
        for (int i = 0; i < count; i++)
        {
            Transform leg = cat.LegTransforms[i];
            if (leg == null)
            {
                continue;
            }

            float phase = (i % 2 == 0 ? 0f : Mathf.PI) + (i >= 2 ? Mathf.PI * 0.45f : 0f);
            float swing = Mathf.Sin(time * speed + cat.AnimationPhase + phase) * amplitude;
            leg.localRotation = cat.LegBaseRotations[i] * Quaternion.Euler(crouch + swing, 0f, 0f);
        }
    }
}
