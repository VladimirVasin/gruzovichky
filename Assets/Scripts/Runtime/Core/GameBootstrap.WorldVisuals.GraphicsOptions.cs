using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private const float GfxDefaultBloomIntensity = 0.55f;
    private const float GfxDefaultBloomReach     = 0.55f;
    private const float GfxDefaultSaturation     = 0.55f;
    private const float GfxDefaultContrast       = 0.60f;
    private const float GfxDefaultWarmth         = 0.55f;
    private const float GfxDefaultVignette       = 0.45f;
    private const float GfxDefaultDepthOfField   = 0.55f;
    private const float GfxDefaultFilmGrain      = 0.35f;
    private const float GfxDefaultChromAberr     = 0.35f;

    private void SetupOptionalGraphicsPostProcessing(VolumeProfile profile)
    {
        FilmGrain filmGrain = profile.Add<FilmGrain>(false);
        filmGrain.active = false;
        filmGrain.type.Override(FilmGrainLookup.Thin2);
        filmGrain.intensity.Override(0.055f);
        filmGrain.response.Override(0.80f);
        dioramaFilmGrain = filmGrain;

        ShadowsMidtonesHighlights smh = profile.Add<ShadowsMidtonesHighlights>(false);
        smh.active = false;
        dioramaSMH = smh;

        ChromaticAberration chromAbr = profile.Add<ChromaticAberration>(false);
        chromAbr.active = false;
        chromAbr.intensity.Override(0.038f);
        dioramaChromaticAberration = chromAbr;
    }

    private void UpdateOptionalGraphicsPostProcessing(float stylizedDaylight, float dawnDuskStrength)
    {
        ApplyGraphicsOptionsNow();

        if (dioramaFilmGrain != null)
        {
            if (gfxFilmGrainEnabled)
            {
                float grain = Mathf.Lerp(0.075f, 0.035f, stylizedDaylight)
                            + (1f - Mathf.InverseLerp(0.5f, 1f, activeWeatherParams.FogMult)) * 0.02f;
                dioramaFilmGrain.intensity.Override(grain * MapGfxAroundDefault(gfxFilmGrainIntensity, GfxDefaultFilmGrain, 0f, 3.2f));
            }
        }

        if (dioramaSMH != null)
        {
            if (gfxSmhEnabled)
            {
                Vector4 shadows = Vector4.Lerp(
                    new Vector4(0.88f, 0.92f, 1.05f, 0f),
                    Vector4.Lerp(
                        new Vector4(1.04f, 0.94f, 0.88f, 0f),
                        new Vector4(1f, 1f, 1f, 0f),
                        stylizedDaylight),
                    stylizedDaylight);
                dioramaSMH.shadows.Override(shadows);
                dioramaSMH.highlights.Override(Vector4.Lerp(
                    new Vector4(1f, 1f, 1f, 0f),
                    new Vector4(1.02f, 0.98f, 0.94f, 0f),
                    dawnDuskStrength));
            }
        }

        if (dioramaChromaticAberration != null)
        {
            if (gfxChromAberrEnabled)
            {
                dioramaChromaticAberration.intensity.Override(
                    0.038f * MapGfxAroundDefault(gfxChromAberrIntensity, GfxDefaultChromAberr, 0f, 3.4f) + dawnDuskStrength * 0.022f);
            }
        }
    }

    private void ApplyGraphicsOptionsNow()
    {
        if (dioramaFilmGrain != null)
        {
            dioramaFilmGrain.active = gfxFilmGrainEnabled;
        }

        if (dioramaSMH != null)
        {
            dioramaSMH.active = gfxSmhEnabled;
        }

        if (dioramaChromaticAberration != null)
        {
            dioramaChromaticAberration.active = gfxChromAberrEnabled;
        }

        if (dioramaDepthOfField != null)
        {
            dioramaDepthOfField.active = gfxDepthOfFieldEnabled;
        }
    }

    private float ApplyGfxBloomIntensity(float value) =>
        value * MapGfxAroundDefault(gfxBloomIntensity, GfxDefaultBloomIntensity, 0f, 3.0f);

    private float ApplyGfxBloomThreshold(float value)
    {
        if (gfxBloomReach <= GfxDefaultBloomReach)
        {
            return Mathf.Lerp(1.25f, value, Mathf.InverseLerp(0f, GfxDefaultBloomReach, gfxBloomReach));
        }

        return Mathf.Lerp(value, 0.20f, Mathf.InverseLerp(GfxDefaultBloomReach, 1f, gfxBloomReach));
    }

    private float ApplyGfxSaturation(float value) =>
        value * MapGfxAroundDefault(gfxSaturation, GfxDefaultSaturation, 0.15f, 2.2f);

    private float ApplyGfxContrast(float value) =>
        value * MapGfxAroundDefault(gfxContrast, GfxDefaultContrast, 0.25f, 2.1f);

    private float ApplyGfxVignette(float value) =>
        value * MapGfxAroundDefault(gfxVignette, GfxDefaultVignette, 0f, 2.8f);

    private float ApplyGfxDepthOfField(float value) =>
        value * MapGfxAroundDefault(gfxDepthOfFieldAmount, GfxDefaultDepthOfField, 0f, 2.6f);

    private Color ApplyGfxWarmth(Color value)
    {
        Color cooler = new(0.84f, 0.91f, 1f, value.a);
        Color warmer = Color.Lerp(value, new Color(1.08f, 0.90f, 0.72f, value.a), 0.75f);
        if (gfxWarmth <= GfxDefaultWarmth)
        {
            return Color.Lerp(cooler, value, Mathf.InverseLerp(0f, GfxDefaultWarmth, gfxWarmth));
        }

        return Color.Lerp(value, warmer, Mathf.InverseLerp(GfxDefaultWarmth, 1f, gfxWarmth));
    }

    private static float MapGfxAroundDefault(float value, float defaultValue, float minMultiplier, float maxMultiplier)
    {
        value = Mathf.Clamp01(value);
        if (value <= defaultValue)
        {
            return Mathf.Lerp(minMultiplier, 1f, Mathf.InverseLerp(0f, defaultValue, value));
        }

        return Mathf.Lerp(1f, maxMultiplier, Mathf.InverseLerp(defaultValue, 1f, value));
    }
}
