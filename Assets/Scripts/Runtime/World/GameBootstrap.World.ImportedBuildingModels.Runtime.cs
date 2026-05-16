using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap
{
    private void ConfigureImportedBuildingModel(GameObject model)
    {
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null || !renderer.enabled)
            {
                continue;
            }

            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            ApplyMaterialSmoothness(renderer, GuessVisualSmoothness(renderer.name, VisualSmoothnessBuildingWall));
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
            cameras[i].enabled = false;
        }

        Light[] lights = model.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].enabled = false;
            lights[i].intensity = 0f;
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

    private static void NormalizeImportedBuildingMaterial(Renderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        bool translucentSurface = IsImportedWindowGlassRenderer(renderer);
        Material material = renderer.material;
        if (material == null)
        {
            return;
        }

        if (!translucentSurface)
        {
            ForceImportedMaterialAlpha(material, 1f);
            ForceImportedMaterialOpaque(material);
        }

        ForceImportedMaterialDoubleSided(material);
    }

    private static void ForceImportedMaterialAlpha(Material material, float alpha)
    {
        if (material == null)
        {
            return;
        }

        Color color = material.color;
        color.a = alpha;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            Color baseColor = material.GetColor("_BaseColor");
            baseColor.a = alpha;
            material.SetColor("_BaseColor", baseColor);
        }

        if (material.HasProperty("_Color"))
        {
            Color legacyColor = material.GetColor("_Color");
            legacyColor.a = alpha;
            material.SetColor("_Color", legacyColor);
        }
    }

    private static void ForceImportedMaterialOpaque(Material material)
    {
        if (material == null)
        {
            return;
        }

        material.SetOverrideTag("RenderType", "Opaque");
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 0f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_SrcBlend"))
        {
            material.SetFloat("_SrcBlend", (float)BlendMode.One);
        }

        if (material.HasProperty("_DstBlend"))
        {
            material.SetFloat("_DstBlend", (float)BlendMode.Zero);
        }

        if (material.HasProperty("_ZWrite"))
        {
            material.SetFloat("_ZWrite", 1f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", 0f);
        }

        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Geometry;
    }

    private static void ForceImportedMaterialDoubleSided(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", (float)CullMode.Off);
        }

        if (material.HasProperty("_CullMode"))
        {
            material.SetFloat("_CullMode", (float)CullMode.Off);
        }

        material.doubleSidedGI = true;
    }
}
