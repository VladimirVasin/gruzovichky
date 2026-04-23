using System.Collections.Generic;
using UnityEngine;

public static class TerrainHeightGenerator
{
    public static float[,] Generate(int gridWidth, int gridHeight, IEnumerable<WorldLocationPlacement> flatAreas)
    {
        float[,] heights = new float[gridWidth, gridHeight];
        float noiseOffsetX = Random.Range(0f, 1000f);
        float noiseOffsetY = Random.Range(0f, 1000f);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                float broadHills = Mathf.PerlinNoise(noiseOffsetX * 0.19f + x * 0.095f, noiseOffsetY * 0.19f + y * 0.095f);
                float primaryNoise = Mathf.PerlinNoise(noiseOffsetX + x * 0.19f, noiseOffsetY + y * 0.19f);
                float secondaryNoise = Mathf.PerlinNoise(noiseOffsetX * 0.37f + x * 0.41f, noiseOffsetY * 0.37f + y * 0.41f);
                float combined = broadHills * 0.42f + primaryNoise * 0.43f + secondaryNoise * 0.15f;
                heights[x, y] = Mathf.Lerp(0.02f, 0.78f, combined);
            }
        }

        SmoothHeights(heights, 3);
        FlattenAreas(heights, flatAreas);
        return heights;
    }

    private static void SmoothHeights(float[,] heights, int passes)
    {
        int width = heights.GetLength(0);
        int height = heights.GetLength(1);
        float[,] buffer = new float[width, height];

        for (int pass = 0; pass < passes; pass++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float total = heights[x, y] * 2f;
                    float weight = 2f;

                    foreach (Vector2Int neighbor in GetNeighbors(new Vector2Int(x, y)))
                    {
                        if (!IsInsideGrid(neighbor, width, height))
                        {
                            continue;
                        }

                        total += heights[neighbor.x, neighbor.y];
                        weight += 1f;
                    }

                    buffer[x, y] = total / weight;
                }
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    heights[x, y] = buffer[x, y];
                }
            }
        }
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
                    bool onPad = x >= area.Min.x && x <= area.Max.x && y >= area.Min.y && y <= area.Max.y;
                    bool isAnchor = x == area.Anchor.x && y == area.Anchor.y;
                    float blend = (onPad || isAnchor) ? 1f : 0.45f;
                    heights[x, y] = Mathf.Lerp(heights[x, y], flatHeight, blend);
                }
            }
        }
    }

    private static IEnumerable<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        yield return new Vector2Int(cell.x + 1, cell.y);
        yield return new Vector2Int(cell.x - 1, cell.y);
        yield return new Vector2Int(cell.x, cell.y + 1);
        yield return new Vector2Int(cell.x, cell.y - 1);
    }

    private static bool IsInsideGrid(Vector2Int cell, int width, int height)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }
}

