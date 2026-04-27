using System.Collections.Generic;
using UnityEngine;

public static class TerrainHeightGenerator
{
    public static float[,] Generate(
        int gridWidth,
        int gridHeight,
        IEnumerable<WorldLocationPlacement> flatAreas,
        IEnumerable<NaturalZoneData> hillZones = null)
    {
        float[,] heights = new float[gridWidth, gridHeight];
        float noiseOffsetX = Random.Range(0f, 1000f);
        float noiseOffsetY = Random.Range(0f, 1000f);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float broadHills  = Mathf.PerlinNoise(noiseOffsetX * 0.19f + x * 0.095f, noiseOffsetY * 0.19f + y * 0.095f);
                float primaryNoise   = Mathf.PerlinNoise(noiseOffsetX + x * 0.19f, noiseOffsetY + y * 0.19f);
                float secondaryNoise = Mathf.PerlinNoise(noiseOffsetX * 0.37f + x * 0.41f, noiseOffsetY * 0.37f + y * 0.41f);
                float combined = broadHills * 0.42f + primaryNoise * 0.43f + secondaryNoise * 0.15f;
                heights[x, y] = Mathf.Lerp(0.02f, 0.78f, combined);
            }
        }

        ApplyHillZones(heights, hillZones);
        SmoothHeights(heights, 2);
        FlattenAreas(heights, flatAreas);
        return heights;
    }

    private static void ApplyHillZones(float[,] heights, IEnumerable<NaturalZoneData> hillZones)
    {
        if (hillZones == null)
        {
            return;
        }

        int width = heights.GetLength(0);
        int height = heights.GetLength(1);
        foreach (NaturalZoneData zone in hillZones)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(zone.Center.x - zone.Radius.x - 2f));
            int maxX = Mathf.Min(width - 1, Mathf.CeilToInt(zone.Center.x + zone.Radius.x + 2f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(zone.Center.y - zone.Radius.y - 2f));
            int maxY = Mathf.Min(height - 1, Mathf.CeilToInt(zone.Center.y + zone.Radius.y + 2f));

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    float influence = zone.GetInfluence(x + 0.5f, y + 0.5f);
                    if (influence <= 0f)
                    {
                        continue;
                    }

                    float ridgeNoise = Mathf.PerlinNoise((x + zone.NoiseSeed) * 0.18f, (y - zone.NoiseSeed) * 0.18f);
                    float lift = zone.Strength * Mathf.Lerp(0.55f, 1.08f, ridgeNoise) * influence;
                    heights[x, y] += lift;
                }
            }
        }
    }

    private static void SmoothHeights(float[,] heights, int passes)
    {
        int width = heights.GetLength(0);
        int height = heights.GetLength(1);
        float[,] buffer = new float[width, height];

        for (int pass = 0; pass < passes; pass++)
        {
            // interior cells — no bounds check needed
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    buffer[x, y] = (heights[x, y] * 2f
                        + heights[x + 1, y] + heights[x - 1, y]
                        + heights[x, y + 1] + heights[x, y - 1]) / 6f;
                }
            }

            // edges (4 strips, corners handled inline)
            for (int x = 0; x < width; x++)
            {
                buffer[x, 0]          = SmoothEdgeCell(heights, x, 0,          width, height);
                buffer[x, height - 1] = SmoothEdgeCell(heights, x, height - 1, width, height);
            }
            for (int y = 1; y < height - 1; y++)
            {
                buffer[0,         y] = SmoothEdgeCell(heights, 0,         y, width, height);
                buffer[width - 1, y] = SmoothEdgeCell(heights, width - 1, y, width, height);
            }

            // copy buffer back into heights
            for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                heights[x, y] = buffer[x, y];
        }
    }

    private static float SmoothEdgeCell(float[,] h, int x, int y, int w, int ht)
    {
        float total = h[x, y] * 2f;
        float weight = 2f;
        if (x + 1 < w)  { total += h[x + 1, y]; weight += 1f; }
        if (x - 1 >= 0) { total += h[x - 1, y]; weight += 1f; }
        if (y + 1 < ht) { total += h[x, y + 1]; weight += 1f; }
        if (y - 1 >= 0) { total += h[x, y - 1]; weight += 1f; }
        return total / weight;
    }

    private static void FlattenAreas(float[,] heights, IEnumerable<WorldLocationPlacement> flatAreas)
    {
        int width = heights.GetLength(0);
        int height = heights.GetLength(1);

        foreach (WorldLocationPlacement area in flatAreas)
        {
            float total = 0f;
            int count = 0;

            for (int x = area.Min.x; x <= area.Max.x; x++)
            {
                for (int y = area.Min.y; y <= area.Max.y; y++)
                {
                    total += heights[x, y];
                    count++;
                }
            }

            total += heights[area.Anchor.x, area.Anchor.y];
            count++;
            float flatHeight = total / Mathf.Max(1, count);

            for (int x = Mathf.Max(0, area.Min.x - 1); x <= Mathf.Min(width - 1, area.Max.x + 1); x++)
            {
                for (int y = Mathf.Max(0, area.Min.y - 1); y <= Mathf.Min(height - 1, area.Max.y + 1); y++)
                {
                    bool onPad   = x >= area.Min.x && x <= area.Max.x && y >= area.Min.y && y <= area.Max.y;
                    bool isAnchor = x == area.Anchor.x && y == area.Anchor.y;
                    float blend  = (onPad || isAnchor) ? 1f : 0.45f;
                    heights[x, y] = Mathf.Lerp(heights[x, y], flatHeight, blend);
                }
            }
        }
    }
}
