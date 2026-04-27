using UnityEngine;

public enum ServiceDecorationKind
{
    Bar,
    Canteen,
    GamblingHall
}

public readonly struct ServiceDecorationLightStyle
{
    public ServiceDecorationLightStyle(Color color, float intensity, float range)
    {
        Color = color;
        Intensity = intensity;
        Range = range;
    }

    public Color Color { get; }
    public float Intensity { get; }
    public float Range { get; }
}

public static class ServiceDecorationStyleService
{
    public static ServiceDecorationLightStyle GetLightStyle(ServiceDecorationKind kind)
    {
        return kind switch
        {
            ServiceDecorationKind.Bar => new ServiceDecorationLightStyle(new Color(1f, 0.85f, 0.5f), 0.35f, 3f),
            ServiceDecorationKind.Canteen => new ServiceDecorationLightStyle(new Color(1f, 0.78f, 0.42f), 0.32f, 2.8f),
            ServiceDecorationKind.GamblingHall => new ServiceDecorationLightStyle(new Color(1f, 0.48f, 0.82f), 0.48f, 4.2f),
            _ => new ServiceDecorationLightStyle(new Color(1f, 0.82f, 0.55f), 0.3f, 3f)
        };
    }
}
