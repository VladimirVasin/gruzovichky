using UnityEngine;

public readonly struct NaturalZoneData
{
    public readonly Vector2 Center;
    public readonly Vector2 Radius;
    public readonly float Strength;
    public readonly float NoiseSeed;

    public NaturalZoneData(Vector2 center, Vector2 radius, float strength, float noiseSeed)
    {
        Center = center;
        Radius = new Vector2(Mathf.Max(1f, radius.x), Mathf.Max(1f, radius.y));
        Strength = strength;
        NoiseSeed = noiseSeed;
    }

    public float GetInfluence(float x, float y)
    {
        float dx = (x - Center.x) / Radius.x;
        float dy = (y - Center.y) / Radius.y;
        float distance = Mathf.Sqrt(dx * dx + dy * dy);
        if (distance >= 1f)
        {
            return 0f;
        }

        float falloff = 1f - distance;
        return falloff * falloff * (3f - 2f * falloff);
    }

    public bool ContainsCell(int x, int y, float noiseScale = 0.13f, float threshold = 0.08f)
    {
        float influence = GetInfluence(x + 0.5f, y + 0.5f);
        if (influence <= 0f)
        {
            return false;
        }

        float noise = Mathf.PerlinNoise((x + NoiseSeed) * noiseScale, (y - NoiseSeed) * noiseScale);
        return influence + (noise - 0.5f) * 0.34f > threshold;
    }
}
