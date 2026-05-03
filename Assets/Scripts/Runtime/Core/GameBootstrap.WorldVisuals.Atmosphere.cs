using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private void SetupNightSky()
    {
        nightStars.Clear();
        if (nightSkyRoot != null)
        {
            Destroy(nightSkyRoot.gameObject);
        }

        if (worldRoot == null)
        {
            return;
        }

        nightSkyRoot = new GameObject("NightSky").transform;
        nightSkyRoot.SetParent(worldRoot, false);

        GameObject moonObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moonObj.name = "Moon";
        moonObj.transform.SetParent(nightSkyRoot, false);
        moonObj.transform.position = new Vector3(GridWidth * 0.5f, 68f, GridHeight + 62f);
        moonObj.transform.localScale = Vector3.one * 7f;
        moonMaterial = CreateTransparentOverlayMaterial(new Color(1f, 0.98f, 0.92f, 0f));
        Renderer moonRend = moonObj.GetComponent<Renderer>();
        moonRend.sharedMaterial = moonMaterial;
        moonRend.shadowCastingMode = ShadowCastingMode.Off;
        moonRend.receiveShadows = false;
        if (moonObj.TryGetComponent(out Collider moonCol)) moonCol.enabled = false;

        for (int i = 0; i < NightStarCount; i++)
        {
            float x = Random.Range(-30f, GridWidth + 30f);
            float y = Random.Range(54f, 84f);
            float z = Random.Range(-40f, GridHeight + 50f);
            float scale = Random.Range(0.24f, 0.62f);

            int ct = i % 3;
            Color baseColor = ct == 0
                ? new Color(1f, 0.90f, 0.76f, 0f)
                : ct == 1
                  ? new Color(0.90f, 0.94f, 1f, 0f)
                  : new Color(1f, 1f, 0.98f, 0f);

            GameObject starObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            starObj.name = $"Star_{i + 1}";
            starObj.transform.SetParent(nightSkyRoot, false);
            starObj.transform.position = new Vector3(x, y, z);
            starObj.transform.localScale = Vector3.one * scale;

            Material starMat = CreateTransparentOverlayMaterial(baseColor);
            Renderer sr = starObj.GetComponent<Renderer>();
            sr.sharedMaterial = starMat;
            sr.shadowCastingMode = ShadowCastingMode.Off;
            sr.receiveShadows = false;
            if (starObj.TryGetComponent(out Collider starCol)) starCol.enabled = false;

            nightStars.Add(new NightStarData
            {
                Transform    = starObj.transform,
                Material     = starMat,
                BaseColor    = baseColor,
                TwinkleSpeed = Random.Range(0.7f, 2.4f),
                TwinklePhase = Random.Range(0f, 10f)
            });
        }
    }

    private void UpdateNightSky()
    {
        float nightStrength = 1f - Mathf.SmoothStep(0.05f, 0.38f, currentStylizedDaylight);
        float time = Time.time;

        if (moonMaterial != null)
        {
            moonMaterial.color = new Color(1f, 0.98f, 0.92f, nightStrength * 0.97f);
        }

        for (int i = nightStars.Count - 1; i >= 0; i--)
        {
            NightStarData star = nightStars[i];
            if (star.Transform == null)
            {
                nightStars.RemoveAt(i);
                continue;
            }

            float twinkle = 1f + Mathf.Sin(time * star.TwinkleSpeed + star.TwinklePhase) * 0.17f;
            float alpha = nightStrength * Mathf.Clamp01(twinkle);
            Color c = star.BaseColor;
            star.Material.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    private void SetupDioramaPostProcessing()
    {
        UniversalAdditionalCameraData cameraData = mainCamera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.Medium;

        RenderSettings.fog = false;
        RenderSettings.fogMode = FogMode.Linear;

        GameObject volumeObject = new("DioramaVolume");
        volumeObject.transform.SetParent(worldRoot, false);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f;
        dioramaVolume = volume;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = profile;
        dioramaVolumeProfile = profile;

        ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
        colorAdjustments.postExposure.Override(0.04f);
        colorAdjustments.contrast.Override(14f);
        colorAdjustments.saturation.Override(10f);
        colorAdjustments.colorFilter.Override(new Color(1f, 0.98f, 0.95f, 1f));
        dioramaColorAdjustments = colorAdjustments;

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.84f);
        bloom.intensity.Override(0.14f);
        bloom.scatter.Override(0.58f);
        bloom.tint.Override(new Color(1f, 0.93f, 0.84f, 1f));
        bloom.highQualityFiltering.Override(true);
        dioramaBloom = bloom;

        DepthOfField depthOfField = profile.Add<DepthOfField>(true);
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(32f);
        depthOfField.gaussianEnd.Override(72f);
        depthOfField.gaussianMaxRadius.Override(0.022f);
        depthOfField.highQualitySampling.Override(true);
        dioramaDepthOfField = depthOfField;

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.052f);
        vignette.smoothness.Override(0.44f);
        vignette.rounded.Override(true);
        dioramaVignette = vignette;

        SetupOptionalGraphicsPostProcessing(profile);

        UpdateDioramaPostProcessing(1f, 0f, 1f, mainCamera.backgroundColor);
    }

    private void UpdateDioramaPostProcessing(float stylizedDaylight, float lowSun, float sunArc, Color backgroundColor)
    {
        if (dioramaColorAdjustments == null || dioramaBloom == null || dioramaDepthOfField == null || dioramaVignette == null)
        {
            return;
        }

        float dawnDuskStrength = Mathf.Clamp01(lowSun * Mathf.Lerp(0.2f, 1f, stylizedDaylight));
        float nightStrength = 1f - stylizedDaylight;
        float zoomT = Mathf.InverseLerp(CameraMinHeight, CameraMaxHeight, cameraOffset.y);
        float farBloomBoost = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.32f, 1f, zoomT));

        dioramaColorAdjustments.postExposure.Override(Mathf.Lerp(-0.18f, 0.1f, stylizedDaylight) + dawnDuskStrength * 0.03f + farBloomBoost * 0.08f);
        dioramaColorAdjustments.contrast.Override(ApplyGfxContrast(Mathf.Lerp(8f, 17f, stylizedDaylight)));
        dioramaColorAdjustments.saturation.Override(ApplyGfxSaturation(Mathf.Lerp(-6f, 14f, stylizedDaylight)));
        dioramaColorAdjustments.colorFilter.Override(Color.Lerp(
            ApplyGfxWarmth(Color.Lerp(new Color(0.84f, 0.88f, 1f, 1f), new Color(1f, 0.88f, 0.78f, 1f), dawnDuskStrength)),
            ApplyGfxWarmth(new Color(1f, 0.985f, 0.955f, 1f)),
            stylizedDaylight));

        dioramaBloom.threshold.Override(ApplyGfxBloomThreshold(Mathf.Max(0.34f, Mathf.Lerp(0.72f, 0.86f, stylizedDaylight) - farBloomBoost * 0.32f)));
        dioramaBloom.intensity.Override(ApplyGfxBloomIntensity(Mathf.Lerp(0.22f, 0.16f, stylizedDaylight) + dawnDuskStrength * 0.34f + farBloomBoost * Mathf.Lerp(0.55f, 0.82f, stylizedDaylight)));
        dioramaBloom.scatter.Override(Mathf.Min(0.95f, Mathf.Lerp(0.66f, 0.56f, stylizedDaylight) + farBloomBoost * 0.24f + dawnDuskStrength * 0.18f));
        dioramaBloom.tint.Override(Color.Lerp(
            new Color(0.88f, 0.92f, 1f, 1f),
            Color.Lerp(new Color(1f, 0.84f, 0.72f, 1f), new Color(1f, 0.95f, 0.88f, 1f), sunArc),
            Mathf.Lerp(stylizedDaylight, 1f, dawnDuskStrength)));

        dioramaDepthOfField.gaussianStart.Override(Mathf.Lerp(26f, 40f, zoomT));
        dioramaDepthOfField.gaussianEnd.Override(Mathf.Lerp(60f, 86f, zoomT));
        dioramaDepthOfField.gaussianMaxRadius.Override(ApplyGfxDepthOfField(Mathf.Lerp(0.026f, 0.016f, zoomT)));

        dioramaVignette.intensity.Override(ApplyGfxVignette(Mathf.Lerp(0.07f, 0.045f, stylizedDaylight) + nightStrength * 0.01f));
        dioramaVignette.smoothness.Override(Mathf.Lerp(0.5f, 0.4f, stylizedDaylight));

        UpdateOptionalGraphicsPostProcessing(stylizedDaylight, dawnDuskStrength);
    }

    private void SetupWeatherSystem()
    {
        rainDrops.Clear();
        if (rainRoot != null) Destroy(rainRoot.gameObject);
        if (worldRoot == null) return;

        rainRoot = new GameObject("RainRoot").transform;
        rainRoot.SetParent(worldRoot, false);

        Color dropColor = new(0.62f, 0.72f, 0.82f, 0f);
        for (int i = 0; i < RainDropCount; i++)
        {
            GameObject drop = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.Destroy(drop.GetComponent<Collider>());
            drop.name = "RainDrop";
            drop.transform.SetParent(rainRoot, false);
            drop.transform.localScale = new Vector3(0.011f, 0.20f, 0.011f);
            Renderer r = drop.GetComponent<Renderer>();
            Material mat = CreateTransparentOverlayMaterial(dropColor);
            r.material = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.enabled = false;
            rainDrops.Add(new RainDropData
            {
                T        = drop.transform,
                Renderer = r,
                Material = mat,
                Y        = Random.Range(-8f, 12f),
                Speed    = Random.Range(7.5f, 11f),
                XOff     = Random.Range(-22f, 22f),
                ZOff     = Random.Range(-22f, 22f),
            });
        }

        activeWeatherParams = WeatherTargetParams[(int)WeatherState.Clear];
        weatherHoldTimer = Random.Range(180f, 420f);
        weatherRainIntensity = 0f;
    }

    private void UpdateWeather(float dt)
    {
        if (isWeatherTransitioning)
        {
            weatherTransitionTimer += dt;
            float t = Mathf.SmoothStep(0f, 1f, weatherTransitionTimer / weatherTransitionDuration);
            activeWeatherParams = LerpWeatherParams(
                WeatherTargetParams[(int)currentWeatherState],
                WeatherTargetParams[(int)nextWeatherState],
                t);
            if (weatherTransitionTimer >= weatherTransitionDuration)
            {
                currentWeatherState    = nextWeatherState;
                activeWeatherParams    = WeatherTargetParams[(int)currentWeatherState];
                isWeatherTransitioning = false;
                weatherHoldTimer       = GetWeatherHoldDuration(currentWeatherState);
            }
        }
        else
        {
            weatherHoldTimer -= dt;
            if (weatherHoldTimer <= 0f)
            {
                nextWeatherState          = PickNextWeatherState();
                weatherTransitionDuration = Random.Range(28f, 85f);
                weatherTransitionTimer    = 0f;
                isWeatherTransitioning    = true;
            }
        }

        float targetRain = currentWeatherState == WeatherState.Rainy ? 1f : 0f;
        if (isWeatherTransitioning && nextWeatherState == WeatherState.Rainy)
            targetRain = Mathf.SmoothStep(0f, 1f, weatherTransitionTimer / weatherTransitionDuration);
        else if (isWeatherTransitioning && currentWeatherState == WeatherState.Rainy)
            targetRain = 1f - Mathf.SmoothStep(0f, 1f, weatherTransitionTimer / weatherTransitionDuration);
        weatherRainIntensity = Mathf.MoveTowards(weatherRainIntensity, targetRain, dt * 0.35f);

        UpdateRainParticles(dt);
        UpdateLightning(dt);

        float targetWind = activeWeatherParams.WindMult;
        for (int i = 0; i < miscTreeSways.Count; i++)
            miscTreeSways[i].CurrentWindMult = Mathf.MoveTowards(miscTreeSways[i].CurrentWindMult, targetWind, dt * 0.6f);
    }

    private void UpdateRainParticles(float dt)
    {
        if (rainDrops.Count == 0 || mainCamera == null) return;
        bool rainVisible = weatherRainIntensity > 0.005f;
        Vector3 camPos = mainCamera.transform.position;
        Color dropColor = new(0.62f, 0.72f, 0.82f, weatherRainIntensity * 0.68f);

        float windExcess = Mathf.Max(0f, activeWeatherParams.WindMult - 1f);
        float windDrift  = windExcess * 2.8f * dt;
        float tiltAngle  = windExcess * 14f;
        Quaternion dropRot = tiltAngle > 0.5f ? Quaternion.Euler(tiltAngle, 0f, 0f) : Quaternion.identity;

        for (int i = 0; i < rainDrops.Count; i++)
        {
            RainDropData d = rainDrops[i];
            if (d.T == null) continue;

            if (!rainVisible)
            {
                if (d.Renderer.enabled) d.Renderer.enabled = false;
                continue;
            }

            if (!d.Renderer.enabled) d.Renderer.enabled = true;
            d.Y -= d.Speed * dt;
            d.XOff += windDrift;
            if (d.XOff > 22f) d.XOff -= 44f;
            if (d.Y < camPos.y - 9f)
            {
                d.Y    = camPos.y + 13f;
                d.XOff = Random.Range(-22f, 22f);
                d.ZOff = Random.Range(-22f, 22f);
            }
            d.T.position = new Vector3(camPos.x + d.XOff, d.Y, camPos.z + d.ZOff);
            d.T.rotation = dropRot;
            d.Material.color = dropColor;
        }
    }

    private void ApplyWeatherToPostProcessing(float stylizedDaylight)
    {
        if (dioramaColorAdjustments == null || dioramaBloom == null) return;
        float nightStr = 1f - stylizedDaylight;
        float foggyNightExpBoost = activeWeatherParams.FogMult < 0.2f ? nightStr * 0.08f : 0f;

        dioramaColorAdjustments.saturation.Override(dioramaColorAdjustments.saturation.value + activeWeatherParams.SatOffset);
        dioramaColorAdjustments.postExposure.Override(dioramaColorAdjustments.postExposure.value + activeWeatherParams.ExposureOffset + foggyNightExpBoost);
        dioramaBloom.scatter.Override(Mathf.Min(0.95f, dioramaBloom.scatter.value + activeWeatherParams.BloomScatterAdd));

        if (lightningFlashActive > 0f)
        {
            float flashT = lightningFlashActive / lightningFlashDuration;
            dioramaColorAdjustments.postExposure.Override(dioramaColorAdjustments.postExposure.value + flashT * 3.2f);
            dioramaBloom.intensity.Override(dioramaBloom.intensity.value + flashT * 2.8f);
            dioramaBloom.scatter.Override(Mathf.Min(0.95f, dioramaBloom.scatter.value + flashT * 0.18f));
        }
    }

    private void UpdateLightning(float dt)
    {
        if (weatherRainIntensity < 0.5f)
        {
            lightningFlashActive = 0f;
            return;
        }

        if (lightningFlashActive > 0f)
        {
            lightningFlashActive -= dt;
            return;
        }

        lightningFlashTimer -= dt;
        if (lightningFlashTimer <= 0f)
        {
            lightningFlashDuration = Random.Range(0.12f, 0.26f);
            lightningFlashActive   = lightningFlashDuration;
            lightningFlashTimer    = Random.Range(18f, 45f);
        }
    }

    private static WeatherParams LerpWeatherParams(WeatherParams a, WeatherParams b, float t)
    {
        return new WeatherParams
        {
            FogMult         = Mathf.Lerp(a.FogMult,         b.FogMult,         t),
            SatOffset       = Mathf.Lerp(a.SatOffset,       b.SatOffset,       t),
            ExposureOffset  = Mathf.Lerp(a.ExposureOffset,  b.ExposureOffset,  t),
            WindMult        = Mathf.Lerp(a.WindMult,        b.WindMult,        t),
            BloomScatterAdd = Mathf.Lerp(a.BloomScatterAdd, b.BloomScatterAdd, t),
        };
    }

    private WeatherState PickNextWeatherState()
    {
        float r = Random.value;
        WeatherState next = r < 0.38f ? WeatherState.Clear
            : r < 0.64f ? WeatherState.Overcast
            : r < 0.80f ? WeatherState.Rainy
            : r < 0.92f ? WeatherState.Foggy
            : WeatherState.Windy;
        if (next == currentWeatherState)
            next = (WeatherState)(((int)next + 1) % 5);
        return next;
    }

    private static float GetWeatherHoldDuration(WeatherState state)
    {
        return state switch
        {
            WeatherState.Clear    => Random.Range(180f, 480f),
            WeatherState.Overcast => Random.Range(120f, 360f),
            WeatherState.Rainy    => Random.Range(90f,  240f),
            WeatherState.Foggy    => Random.Range(60f,  180f),
            WeatherState.Windy    => Random.Range(60f,  150f),
            _                     => 180f,
        };
    }


}
