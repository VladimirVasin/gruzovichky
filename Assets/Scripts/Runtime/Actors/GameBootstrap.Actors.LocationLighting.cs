using UnityEngine;

public partial class GameBootstrap
{
    private const float NightLightActivationDarkness = 0.64f;
    private const float NightLightFullDarkness = 0.94f;
    private const int MaxActiveLocationUnityLights = 14;
    private const int MaxActiveRoadLanternUnityLights = 18;
    private const float LocationUnityLightFocusDistance = 26f;
    private const float RoadLanternUnityLightFocusDistance = 18f;
    private const float DriverFlashlightUnityLightFocusDistance = 18f;

    private void UpdateLocationNightLights(float stylizedDaylight)
    {
        float darkness = 1f - stylizedDaylight;
        bool lightsOn = darkness > NightLightActivationDarkness;
        float nightT = lightsOn ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(NightLightActivationDarkness, NightLightFullDarkness, darkness)) : 0f;

        int activeLocationUnityLights = 0;
        for (int i = 0; i < locationNightLights.Count; i++)
        {
            Light lightComponent = locationNightLights[i];
            if (lightComponent == null)
            {
                continue;
            }

            float maxIntensity = i < locationNightPointLightMaxIntensities.Count ? locationNightPointLightMaxIntensities[i] : 1.15f;
            float range = i < locationNightPointLightRanges.Count ? locationNightPointLightRanges[i] : 3.2f;
            Color onColor = WarmLightSourceColor(i < locationNightPointLightOnColors.Count ? locationNightPointLightOnColors[i] : new Color(1f, 0.9f, 0.72f));
            bool realLightOn = lightsOn &&
                activeLocationUnityLights < MaxActiveLocationUnityLights &&
                ShouldEnableNightUnityLight(lightComponent.transform.position, LocationUnityLightFocusDistance);
            if (realLightOn)
            {
                activeLocationUnityLights++;
            }

            lightComponent.enabled = realLightOn;
            lightComponent.intensity = realLightOn ? Mathf.Lerp(0.18f, maxIntensity, nightT) : 0f;
            lightComponent.color = onColor;
            lightComponent.range = range;
        }

        for (int i = 0; i < locationNightLightMaterials.Count; i++)
        {
            Material material = locationNightLightMaterials[i];
            if (material == null)
            {
                continue;
            }

            Color offColor = WarmLightOffColor(i < locationNightLightOffColors.Count ? locationNightLightOffColors[i] : new Color(0.28f, 0.24f, 0.18f));
            Color onColor = WarmLightSourceColor(i < locationNightLightOnColors.Count ? locationNightLightOnColors[i] : new Color(1f, 0.9f, 0.72f));
            Color lampColor = Color.Lerp(offColor, onColor, nightT);
            SetLocationNightLightMaterialColor(material, lampColor);
        }

        UpdateRoadLanternLights(darkness);
    }

    private static void SetLocationNightLightMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        bool setAnyColor = false;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
            setAnyColor = true;
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
            setAnyColor = true;
        }

        if (!setAnyColor && material.HasProperty("_TintColor"))
        {
            material.SetColor("_TintColor", color);
        }
    }

    private bool ShouldEnableNightUnityLight(Vector3 worldPosition, float focusDistance)
    {
        if (isFarZoomVisualLodActive || focusDistance <= 0f)
        {
            return false;
        }

        Vector3 focus = cameraFocusPoint;
        float dx = worldPosition.x - focus.x;
        float dz = worldPosition.z - focus.z;
        return dx * dx + dz * dz <= focusDistance * focusDistance;
    }

    private bool IsLocationNightLightStaffed(int ownerInstanceId)
    {
        LocationData location = FindLocationByInstanceId(ownerInstanceId);
        if (location == null || GetMaxBuildingWorkerSlots(location.Type) <= 0)
        {
            return false;
        }

        if (location.Workers > 0)
        {
            return true;
        }

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver == null ||
                driver.DutyMode != DriverDutyMode.Logistics ||
                !driver.IsOnActiveShift ||
                driver.RestPhase != DriverRestPhase.None ||
                driver.IsArrivingByBus ||
                driver.AssignedBuildingType != location.Type)
            {
                continue;
            }

            int assignedInstanceId = ResolveBuildingInstanceId(location.Type, driver.AssignedBuildingInstanceId);
            if (assignedInstanceId == location.InstanceId)
            {
                return true;
            }
        }

        return false;
    }

    private void UpdateRoadLanternLights(float darkness)
    {
        float time = Time.time;
        int activeRoadLanternUnityLights = 0;
        NightLightProfile lanternProfile = GetRoadLanternLightProfile();
        foreach (RoadLanternData roadLantern in roadLanterns)
        {
            if (roadLantern.Light == null || roadLantern.GlowMaterial == null)
            {
                continue;
            }

            float activationThreshold = NightLightActivationDarkness + roadLantern.ActivationOffset * 0.35f;
            float baseActivation = Mathf.InverseLerp(activationThreshold, NightLightFullDarkness, darkness);
            baseActivation = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(baseActivation));

            bool lightsOn = baseActivation > 0.01f;
            float flickerBlend = 1f;
            if (lightsOn)
            {
                float softPulse = Mathf.Lerp(
                    0.84f,
                    1f,
                    Mathf.PerlinNoise(roadLantern.FlickerSeed, time * roadLantern.FlickerSpeed));
                float irregularNoise = Mathf.PerlinNoise(
                    roadLantern.FlickerSeed * 2.31f,
                    17.5f + time * (roadLantern.FlickerSpeed * 2.2f));
                float randomPulse = 1f;
                if (irregularNoise > roadLantern.FlickerThreshold)
                {
                    randomPulse = Mathf.Lerp(
                        1f - roadLantern.FlickerStrength,
                        1f,
                        Mathf.PerlinNoise(31.2f + time * 14.5f, roadLantern.FlickerSeed * 0.7f));
                }

                float blinkNoise = Mathf.PerlinNoise(
                    51.8f + roadLantern.FlickerSeed * 0.19f,
                    time * (roadLantern.FlickerSpeed * 5.5f));
                float blinkPulse = blinkNoise > 0.88f
                    ? Mathf.Lerp(0.5f, 1f, Mathf.PerlinNoise(72.4f + time * 19f, roadLantern.FlickerSeed * 1.17f))
                    : 1f;

                flickerBlend = softPulse * randomPulse * blinkPulse;
            }

            float lightIntensity = Mathf.Lerp(0.35f, lanternProfile.UnityIntensity, baseActivation) * flickerBlend;
            float glowStrength = Mathf.Lerp(0.18f, 1.18f, baseActivation) * Mathf.Lerp(0.92f, 1f, flickerBlend);
            Color lanternColor = Color.Lerp(
                new Color(0.34f, 0.18f, 0.07f),
                lanternProfile.Color,
                Mathf.Clamp01(glowStrength));

            bool realLightOn = lightsOn &&
                activeRoadLanternUnityLights < MaxActiveRoadLanternUnityLights &&
                ShouldEnableNightUnityLight(roadLantern.Light.transform.position, RoadLanternUnityLightFocusDistance);
            if (realLightOn)
            {
                activeRoadLanternUnityLights++;
            }

            roadLantern.Light.enabled = realLightOn;
            roadLantern.Light.intensity = realLightOn ? lightIntensity : 0f;
            roadLantern.Light.color = lanternColor;
            roadLantern.Light.range = lanternProfile.UnityRange;
            roadLantern.GlowMaterial.color = lanternColor;
        }
    }
}
