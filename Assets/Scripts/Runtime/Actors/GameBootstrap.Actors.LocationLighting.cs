using UnityEngine;

public partial class GameBootstrap
{
    private void UpdateLocationNightLights(float stylizedDaylight)
    {
        float darkness = 1f - stylizedDaylight;
        bool lightsOn = darkness > 0.5f;
        float nightT = lightsOn ? Mathf.InverseLerp(0.5f, 1f, darkness) : 0f;

        for (int i = 0; i < locationNightLights.Count; i++)
        {
            Light lightComponent = locationNightLights[i];
            if (lightComponent == null)
            {
                continue;
            }

            float maxIntensity = i < locationNightPointLightMaxIntensities.Count ? locationNightPointLightMaxIntensities[i] : 1.15f;
            float range = i < locationNightPointLightRanges.Count ? locationNightPointLightRanges[i] : 3.2f;
            Color onColor = i < locationNightPointLightOnColors.Count ? locationNightPointLightOnColors[i] : new Color(1f, 0.9f, 0.72f);
            bool realLightOn = lightsOn;
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

            Color offColor = i < locationNightLightOffColors.Count ? locationNightLightOffColors[i] : new Color(0.28f, 0.24f, 0.18f);
            Color onColor = i < locationNightLightOnColors.Count ? locationNightLightOnColors[i] : new Color(1f, 0.9f, 0.72f);
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
        foreach (RoadLanternData roadLantern in roadLanterns)
        {
            if (roadLantern.Light == null || roadLantern.GlowMaterial == null)
            {
                continue;
            }

            float activationThreshold = 0.43f + roadLantern.ActivationOffset;
            float baseActivation = Mathf.InverseLerp(activationThreshold, activationThreshold + 0.18f, darkness);
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

            float lightIntensity = Mathf.Lerp(0.35f, 2.35f, baseActivation) * flickerBlend;
            float glowStrength = Mathf.Lerp(0.18f, 1.18f, baseActivation) * Mathf.Lerp(0.92f, 1f, flickerBlend);
            Color lanternColor = Color.Lerp(
                new Color(0.26f, 0.16f, 0.08f),
                new Color(1f, 0.78f, 0.42f),
                Mathf.Clamp01(glowStrength));

            bool realLightOn = lightsOn;
            roadLantern.Light.enabled = realLightOn;
            roadLantern.Light.intensity = realLightOn ? lightIntensity : 0f;
            roadLantern.Light.color = lanternColor;
            roadLantern.GlowMaterial.color = lanternColor;
        }
    }
}
