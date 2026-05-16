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
            ServiceDecorationKind.Bar => new ServiceDecorationLightStyle(new Color(1f, 0.66f, 0.32f), 0.42f, 5.8f),
            ServiceDecorationKind.Canteen => new ServiceDecorationLightStyle(new Color(1f, 0.64f, 0.30f), 0.38f, 5.2f),
            ServiceDecorationKind.GamblingHall => new ServiceDecorationLightStyle(new Color(1f, 0.42f, 0.58f), 0.58f, 7.2f),
            _ => new ServiceDecorationLightStyle(new Color(1f, 0.66f, 0.34f), 0.34f, 5.4f)
        };
    }
}
