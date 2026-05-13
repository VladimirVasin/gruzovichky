using UnityEngine;

public partial class GameBootstrap
{
    private enum CellLightLevel
    {
        Dark,
        Moonlit,
        Dim,
        Lit,
        Bright,
        Landmark
    }

    private sealed class CellLightData
    {
        public float Intensity;
        public Color Color;
        public CellLightLevel Level;
        public int SourceCount;
        public LocationType? DominantLocationType;
        public float DominantIntensity;
    }

    private readonly struct NightLightProfile
    {
        public NightLightProfile(Color color, float unityRange, float unityIntensity, float cellCoreRadius, float cellFalloffRadius, float cellIntensity)
        {
            Color = color;
            UnityRange = unityRange;
            UnityIntensity = unityIntensity;
            CellCoreRadius = cellCoreRadius;
            CellFalloffRadius = cellFalloffRadius;
            CellIntensity = cellIntensity;
        }

        public Color Color { get; }
        public float UnityRange { get; }
        public float UnityIntensity { get; }
        public float CellCoreRadius { get; }
        public float CellFalloffRadius { get; }
        public float CellIntensity { get; }
    }

    private void SetupCellLightingSystem()
    {
        EnsureCellLightingData();
        MarkCellLightingDirty();
    }

    private void EnsureCellLightingData()
    {
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                cellLighting[x, y] ??= new CellLightData();
            }
        }
    }

    private void MarkCellLightingDirty()
    {
        isCellLightingDirty = true;
    }

    private void UpdateCellLightingRuntime(float stylizedDaylight)
    {
        float nightStrength = Mathf.SmoothStep(
            0f,
            1f,
            Mathf.InverseLerp(NightLightActivationDarkness, NightLightFullDarkness, 1f - stylizedDaylight));
        if (nightStrength <= 0.015f)
        {
            lastCellLightingNightStrength = nightStrength;
            return;
        }

        if (!isCellLightingDirty && Mathf.Abs(nightStrength - lastCellLightingNightStrength) < 0.035f)
        {
            return;
        }

        RebuildCellLightingMap(nightStrength);
        lastCellLightingNightStrength = nightStrength;
        isCellLightingDirty = false;
    }

    private void RebuildCellLightingMap(float nightStrength)
    {
        EnsureCellLightingData();
        ResetCellLightingMap(nightStrength);
        AddRoadLanternCellLighting(nightStrength);
        AddLocationCellLighting(nightStrength);
    }

    private void ResetCellLightingMap(float nightStrength)
    {
        Color moonColor = Color.Lerp(new Color(0.12f, 0.15f, 0.24f), new Color(0.28f, 0.32f, 0.44f), nightStrength);
        float baseIntensity = 0.035f + nightStrength * 0.085f;
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                CellLightData data = cellLighting[x, y];
                data.Intensity = baseIntensity;
                data.Color = moonColor;
                data.Level = GetCellLightLevel(baseIntensity);
                data.SourceCount = 0;
                data.DominantLocationType = null;
                data.DominantIntensity = 0f;
            }
        }
    }

    private void AddRoadLanternCellLighting(float nightStrength)
    {
        NightLightProfile profile = GetRoadLanternLightProfile();
        for (int i = 0; i < roadLanterns.Count; i++)
        {
            RoadLanternData roadLantern = roadLanterns[i];
            if (roadLantern?.Light == null)
            {
                continue;
            }

            AddCellLightSource(
                roadLantern.Light.transform.position,
                profile,
                profile.Color,
                nightStrength,
                null);
        }
    }

    private void AddLocationCellLighting(float nightStrength)
    {
        for (int i = 0; i < locationNightLights.Count; i++)
        {
            Light lightComponent = locationNightLights[i];
            if (lightComponent == null)
            {
                continue;
            }

            LocationType? type = null;
            if (i < locationNightLightOwnerInstanceIds.Count)
            {
                LocationData owner = FindLocationByInstanceId(locationNightLightOwnerInstanceIds[i]);
                if (owner != null)
                {
                    type = owner.Type;
                }
            }

            Color color = i < locationNightPointLightOnColors.Count ? locationNightPointLightOnColors[i] : new Color(1f, 0.86f, 0.58f);
            float unityRange = i < locationNightPointLightRanges.Count ? locationNightPointLightRanges[i] : 6.4f;
            float unityIntensity = i < locationNightPointLightMaxIntensities.Count ? locationNightPointLightMaxIntensities[i] : 1f;
            NightLightProfile profile = GetLocationCellLightProfile(type, color, unityRange, unityIntensity);
            AddCellLightSource(lightComponent.transform.position, profile, color, nightStrength, type);
        }
    }

    private void AddCellLightSource(Vector3 worldPosition, NightLightProfile profile, Color color, float nightStrength, LocationType? sourceType)
    {
        Vector2 source = new(worldPosition.x, worldPosition.z);
        int minX = Mathf.Clamp(Mathf.FloorToInt(source.x - profile.CellFalloffRadius), 0, GridWidth - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(source.x + profile.CellFalloffRadius), 0, GridWidth - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt(source.y - profile.CellFalloffRadius), 0, GridHeight - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(source.y + profile.CellFalloffRadius), 0, GridHeight - 1);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2 center = new(x + 0.5f, y + 0.5f);
                float distance = Vector2.Distance(source, center);
                if (distance > profile.CellFalloffRadius)
                {
                    continue;
                }

                float falloff = distance <= profile.CellCoreRadius
                    ? 1f
                    : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(profile.CellCoreRadius, profile.CellFalloffRadius, distance));
                float contribution = profile.CellIntensity * falloff * nightStrength;
                if (contribution <= 0.002f)
                {
                    continue;
                }

                CellLightData data = cellLighting[x, y];
                float previous = data.Intensity;
                float next = Mathf.Clamp01(previous + contribution);
                float blend = contribution / Mathf.Max(next, 0.001f);
                data.Color = Color.Lerp(data.Color, color, Mathf.Clamp01(blend));
                data.Intensity = next;
                data.Level = GetCellLightLevel(next);
                data.SourceCount++;
                if (sourceType.HasValue && contribution > data.DominantIntensity)
                {
                    data.DominantLocationType = sourceType.Value;
                    data.DominantIntensity = contribution;
                }
            }
        }
    }

    private static CellLightLevel GetCellLightLevel(float intensity)
    {
        if (intensity < 0.055f) return CellLightLevel.Dark;
        if (intensity < 0.16f) return CellLightLevel.Moonlit;
        if (intensity < 0.32f) return CellLightLevel.Dim;
        if (intensity < 0.56f) return CellLightLevel.Lit;
        if (intensity < 0.82f) return CellLightLevel.Bright;
        return CellLightLevel.Landmark;
    }

    private static NightLightProfile GetRoadLanternLightProfile()
    {
        return new NightLightProfile(new Color(1f, 0.78f, 0.42f), 9.4f, 2.6f, 1.7f, 5.2f, 0.34f);
    }

    private static NightLightProfile GetLocationCellLightProfile(LocationType? type, Color color, float unityRange, float unityIntensity)
    {
        float core = 1.6f;
        float falloff = Mathf.Clamp(unityRange * 0.58f, 3.4f, 6.2f);
        float intensity = Mathf.Clamp(0.16f + unityIntensity * 0.14f, 0.18f, 0.44f);

        if (type == LocationType.GamblingHall)
        {
            return new NightLightProfile(color, unityRange, unityIntensity, 2.4f, 7.6f, 0.52f);
        }
        if (type == LocationType.Bar)
        {
            return new NightLightProfile(color, unityRange, unityIntensity, 2.2f, 6.7f, 0.46f);
        }
        if (type == LocationType.CityPark)
        {
            return new NightLightProfile(color, unityRange, unityIntensity, 2.1f, 6.4f, 0.36f);
        }
        if (type == LocationType.Parking || type == LocationType.Warehouse || type == LocationType.Docks ||
            type == LocationType.GasStation || type == LocationType.CarMarket)
        {
            return new NightLightProfile(color, unityRange, unityIntensity, 2.0f, 6.8f, 0.40f);
        }
        if (type == LocationType.PersonalHouse || type == LocationType.Motel)
        {
            return new NightLightProfile(color, unityRange, unityIntensity, 1.8f, 5.8f, 0.32f);
        }

        return new NightLightProfile(color, unityRange, unityIntensity, core, falloff, intensity);
    }

    private static float ExpandLocationNightLightRange(LocationType type, float baseRange)
    {
        float minimum = type switch
        {
            LocationType.GamblingHall => 8.8f,
            LocationType.Bar => 7.4f,
            LocationType.Canteen => 6.8f,
            LocationType.CityPark => 6.8f,
            LocationType.Parking => 7.2f,
            LocationType.Warehouse => 7.2f,
            LocationType.Docks => 7.8f,
            LocationType.GasStation => 7.6f,
            LocationType.CarMarket => 7.6f,
            LocationType.Motel => 6.4f,
            LocationType.PersonalHouse => 5.8f,
            _ => 6.2f
        };

        return Mathf.Max(baseRange * 1.55f, minimum);
    }
}
