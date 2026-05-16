using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private void BuildImportedSquirrelAnimationRig(
        Transform modelRoot,
        out Transform bodyRig,
        out Transform headRig,
        out Transform tailRig,
        out Transform[] legRigs,
        out Quaternion[] legBaseRotations)
    {
        bodyRig = modelRoot;
        headRig = CreateImportedSquirrelRigPivot(modelRoot, "ImportedSquirrelHeadRig", FindImportedSquirrelParts(modelRoot, IsImportedSquirrelHeadPart), ImportedSquirrelPivotKind.Head);
        tailRig = CreateImportedSquirrelRigPivot(modelRoot, "ImportedSquirrelTailRig", FindImportedSquirrelParts(modelRoot, IsImportedSquirrelTailPart), ImportedSquirrelPivotKind.Tail);

        List<Transform> legs = new();
        AddImportedSquirrelLegRig(modelRoot, new[] { "FrontLeg_L", "Front_Left", "FrontLeft", "Leg_Front_Left" }, legs);
        AddImportedSquirrelLegRig(modelRoot, new[] { "FrontLeg_R", "Front_Right", "FrontRight", "Leg_Front_Right" }, legs);
        AddImportedSquirrelLegRig(modelRoot, new[] { "HindLeg_L", "Back_Left", "Hind_Left", "BackLeft", "Leg_Back_Left" }, legs);
        AddImportedSquirrelLegRig(modelRoot, new[] { "HindLeg_R", "Back_Right", "Hind_Right", "BackRight", "Leg_Back_Right" }, legs);
        legRigs = legs.ToArray();
        legBaseRotations = new Quaternion[legRigs.Length];
        for (int i = 0; i < legRigs.Length; i++)
        {
            legBaseRotations[i] = legRigs[i] != null ? legRigs[i].localRotation : Quaternion.identity;
        }
    }

    private enum ImportedSquirrelPivotKind
    {
        Head,
        Tail,
        Leg
    }

    private void AddImportedSquirrelLegRig(Transform modelRoot, string[] aliases, List<Transform> legs)
    {
        List<Transform> parts = FindImportedSquirrelParts(modelRoot, name => IsImportedSquirrelLegPart(name, aliases));
        Transform rig = CreateImportedSquirrelRigPivot(modelRoot, "ImportedSquirrelLegRig_" + aliases[0], parts, ImportedSquirrelPivotKind.Leg);
        if (rig != null)
        {
            legs.Add(rig);
        }
    }

    private Transform CreateImportedSquirrelRigPivot(Transform modelRoot, string name, List<Transform> parts, ImportedSquirrelPivotKind kind)
    {
        if (modelRoot == null || parts.Count == 0 || !TryGetImportedSquirrelPartsBounds(parts, out Bounds bounds))
        {
            return null;
        }

        Vector3 pivotPosition = ResolveImportedSquirrelPivot(bounds, kind);
        Transform parent = FindImportedTransform(modelRoot, "Squirrel_Root") ??
            FindImportedTransform(modelRoot, "Squirel_Root") ??
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

    private static Vector3 ResolveImportedSquirrelPivot(Bounds bounds, ImportedSquirrelPivotKind kind)
    {
        Vector3 center = bounds.center;
        return kind switch
        {
            ImportedSquirrelPivotKind.Head => new Vector3(center.x, bounds.min.y + bounds.size.y * 0.36f, bounds.min.z + bounds.size.z * 0.35f),
            ImportedSquirrelPivotKind.Tail => new Vector3(center.x, bounds.min.y + bounds.size.y * 0.28f, bounds.min.z + bounds.size.z * 0.24f),
            ImportedSquirrelPivotKind.Leg => new Vector3(center.x, bounds.max.y, center.z),
            _ => center
        };
    }

    private static List<Transform> FindImportedSquirrelParts(Transform root, System.Predicate<string> predicate)
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
            if (current != null && predicate(current.name) && !HasImportedSquirrelMatchedAncestor(current, root, predicate))
            {
                matches.Add(current);
            }
        }

        return matches;
    }

    private static bool HasImportedSquirrelMatchedAncestor(Transform transform, Transform root, System.Predicate<string> predicate)
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

    private static bool TryGetImportedSquirrelPartsBounds(List<Transform> parts, out Bounds bounds)
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

    private static bool IsImportedSquirrelHeadPart(string name)
    {
        return !string.IsNullOrEmpty(name) &&
            (name.StartsWith("Head", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Ear_", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Eye_", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Nose", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Muzzle", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("Whisker", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsImportedSquirrelTailPart(string name)
    {
        return !string.IsNullOrEmpty(name) &&
            (name.StartsWith("Tail", System.StringComparison.OrdinalIgnoreCase) ||
             name.StartsWith("FurStripe_Tail", System.StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsImportedSquirrelLegPart(string name, string[] aliases)
    {
        if (string.IsNullOrEmpty(name) || aliases == null)
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

        return false;
    }

    private static void ApplySquirrelBodyScale(AmbientSquirrelData sq, Vector3 proceduralScale, Vector3 importedMultiplier)
    {
        if (sq?.BodyTransform == null)
        {
            return;
        }

        sq.BodyTransform.localScale = sq.UsesImportedModel
            ? Vector3.Scale(sq.BodyBaseScale, importedMultiplier)
            : proceduralScale;
    }

    private static void ApplySquirrelHeadMotion(AmbientSquirrelData sq, Quaternion motion)
    {
        if (sq?.HeadTransform == null)
        {
            return;
        }

        sq.HeadTransform.localRotation = sq.UsesImportedModel
            ? sq.HeadBaseRotation * motion
            : motion;
    }

    private static void ApplySquirrelTailMotion(AmbientSquirrelData sq, Quaternion importedMotion, Quaternion proceduralMotion)
    {
        if (sq?.TailTransform == null)
        {
            return;
        }

        sq.TailTransform.localRotation = sq.UsesImportedModel
            ? sq.TailBaseRotation * importedMotion
            : proceduralMotion;
    }

    private static void AnimateImportedSquirrelLegs(AmbientSquirrelData sq, float time, float speed, float amplitude, float crouch)
    {
        if (sq == null || !sq.UsesImportedModel || sq.LegTransforms == null || sq.LegBaseRotations == null)
        {
            return;
        }

        int count = Mathf.Min(sq.LegTransforms.Length, sq.LegBaseRotations.Length);
        for (int i = 0; i < count; i++)
        {
            Transform leg = sq.LegTransforms[i];
            if (leg == null)
            {
                continue;
            }

            float phase = (i % 2 == 0 ? 0f : Mathf.PI) + (i >= 2 ? Mathf.PI * 0.35f : 0f);
            float swing = Mathf.Sin(time * speed + sq.AnimationPhase + phase) * amplitude;
            float sideTwitch = Mathf.Sin(time * speed * 0.55f + sq.AnimationPhase + phase) * amplitude * 0.12f;
            leg.localRotation = sq.LegBaseRotations[i] * Quaternion.Euler(crouch + swing, 0f, sideTwitch);
        }
    }
}
