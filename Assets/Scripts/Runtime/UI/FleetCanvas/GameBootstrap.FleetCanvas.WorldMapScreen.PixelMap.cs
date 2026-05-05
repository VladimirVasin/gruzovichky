using UnityEngine;

public partial class GameBootstrap
{
    private static Sprite CreateRegionalWorldMapPixelSprite()
    {
        const int width = 384;
        const int height = 216;
        Texture2D tex = new(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[width * height];
        Color grass = new(0.29f, 0.50f, 0.30f, 1f);
        Color dryGrass = new(0.50f, 0.53f, 0.34f, 1f);
        Color sand = new(0.74f, 0.63f, 0.38f, 1f);
        Color desert = new(0.66f, 0.54f, 0.31f, 1f);
        Color water = new(0.16f, 0.45f, 0.62f, 1f);
        Color deepWater = new(0.10f, 0.31f, 0.47f, 1f);
        Color shore = new(0.62f, 0.62f, 0.38f, 1f);

        for (int y = 0; y < height; y++)
        {
            float ny = y / (float)(height - 1);
            for (int x = 0; x < width; x++)
            {
                float nx = x / (float)(width - 1);
                Color baseColor = nx < 0.34f && ny > 0.50f ? grass : nx > 0.62f ? sand : dryGrass;
                if (nx > 0.72f && ny < 0.62f) baseColor = desert;
                if (nx < 0.10f && ny > 0.44f) baseColor = water;
                if (nx > 0.80f && ny < 0.44f) baseColor = water;
                if (ny > 0.80f && nx < 0.78f) baseColor = Color.Lerp(water, deepWater, 0.35f);

                float hash = PixelMapHash01(x / 6, y / 6);
                float shade = 0.94f + hash * 0.12f;
                pixels[y * width + x] = baseColor * shade;
            }
        }

        DrawPixelPatch(pixels, width, height, new Vector2(0.21f, 0.72f), new Vector2(0.34f, 0.16f), new Color(0.23f, 0.43f, 0.25f, 1f));
        DrawPixelPatch(pixels, width, height, new Vector2(0.28f, 0.27f), new Vector2(0.30f, 0.16f), new Color(0.27f, 0.46f, 0.27f, 1f));
        DrawPixelPatch(pixels, width, height, new Vector2(0.47f, 0.45f), new Vector2(0.22f, 0.10f), new Color(0.34f, 0.49f, 0.29f, 1f));
        DrawPixelPatch(pixels, width, height, new Vector2(0.73f, 0.18f), new Vector2(0.22f, 0.09f), new Color(0.51f, 0.48f, 0.40f, 1f));

        Color river = new(0.14f, 0.43f, 0.60f, 1f);
        DrawPixelPath(pixels, width, height, shore, 8,
            new Vector2(0.50f, 1.03f), new Vector2(0.49f, 0.82f), new Vector2(0.53f, 0.64f),
            new Vector2(0.49f, 0.47f), new Vector2(0.54f, 0.30f), new Vector2(0.58f, -0.03f));
        DrawPixelPath(pixels, width, height, river, 6,
            new Vector2(0.50f, 1.03f), new Vector2(0.49f, 0.82f), new Vector2(0.53f, 0.64f),
            new Vector2(0.49f, 0.47f), new Vector2(0.54f, 0.30f), new Vector2(0.58f, -0.03f));
        DrawPixelPath(pixels, width, height, new Color(0.22f, 0.56f, 0.70f, 1f), 2,
            new Vector2(0.50f, 1.03f), new Vector2(0.49f, 0.82f), new Vector2(0.53f, 0.64f),
            new Vector2(0.49f, 0.47f), new Vector2(0.54f, 0.30f), new Vector2(0.58f, -0.03f));
        DrawPixelPath(pixels, width, height, shore, 5, new Vector2(0.50f, 0.55f), new Vector2(0.68f, 0.43f));
        DrawPixelPath(pixels, width, height, river, 3, new Vector2(0.50f, 0.55f), new Vector2(0.68f, 0.43f));
        DrawPixelPath(pixels, width, height, shore, 5, new Vector2(0.49f, 0.70f), new Vector2(0.34f, 0.76f));
        DrawPixelPath(pixels, width, height, river, 3, new Vector2(0.49f, 0.70f), new Vector2(0.34f, 0.76f));

        DrawPixelTreeCluster(pixels, width, height, new Vector2(0.18f, 0.74f), 32);
        DrawPixelTreeCluster(pixels, width, height, new Vector2(0.27f, 0.24f), 28);
        DrawPixelTreeCluster(pixels, width, height, new Vector2(0.34f, 0.14f), 12);
        DrawPixelShrubCluster(pixels, width, height, new Vector2(0.62f, 0.62f), 22);
        DrawPixelShrubCluster(pixels, width, height, new Vector2(0.66f, 0.30f), 16);
        DrawPixelMountainCluster(pixels, width, height, new Vector2(0.25f, 0.86f), 9);
        DrawPixelMountainCluster(pixels, width, height, new Vector2(0.73f, 0.19f), 8);

        for (int i = 0; i < 9; i++)
        {
            if (IsWorldMapRegionKnown(i))
            {
                DrawPixelTown(pixels, width, height, GetWorldMapRegionPosition(i), i == 4, true);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static void DrawPixelPatch(Color[] pixels, int width, int height, Vector2 center, Vector2 size, Color color)
    {
        int minX = Mathf.RoundToInt((center.x - size.x * 0.5f) * width);
        int maxX = Mathf.RoundToInt((center.x + size.x * 0.5f) * width);
        int minY = Mathf.RoundToInt((center.y - size.y * 0.5f) * height);
        int maxY = Mathf.RoundToInt((center.y + size.y * 0.5f) * height);
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                float nx = (x / (float)width - center.x) / Mathf.Max(0.001f, size.x * 0.5f);
                float ny = (y / (float)height - center.y) / Mathf.Max(0.001f, size.y * 0.5f);
                if (nx * nx + ny * ny > 1f) continue;
                SetPixel(pixels, width, height, x, y, Color.Lerp(GetPixel(pixels, width, height, x, y), color, 0.78f));
            }
        }
    }

    private static void DrawPixelPath(Color[] pixels, int width, int height, Color color, int radius, params Vector2[] points)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector2 a = new(points[i].x * (width - 1), points[i].y * (height - 1));
            Vector2 b = new(points[i + 1].x * (width - 1), points[i + 1].y * (height - 1));
            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));
            for (int s = 0; s <= steps; s++)
            {
                Vector2 p = Vector2.Lerp(a, b, s / (float)steps);
                FillPixelCircle(pixels, width, height, Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y), radius, color);
            }
        }
    }

    private static void DrawPixelTreeCluster(Color[] pixels, int width, int height, Vector2 center, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = i * 2.399963f;
            float radius = 0.018f + 0.070f * Mathf.Sqrt((i + 1f) / count);
            Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            DrawPixelTree(pixels, width, height, p);
        }
    }

    private static void DrawPixelTree(Color[] pixels, int width, int height, Vector2 p)
    {
        int x = Mathf.RoundToInt(p.x * (width - 1));
        int y = Mathf.RoundToInt(p.y * (height - 1));
        Color trunk = new(0.20f, 0.13f, 0.06f, 1f);
        Color leaf = new(0.08f, 0.31f, 0.13f, 1f);
        FillRect(pixels, width, height, x, y - 2, 1, 3, trunk);
        FillRect(pixels, width, height, x - 2, y, 5, 3, leaf);
        FillRect(pixels, width, height, x - 1, y + 2, 3, 2, leaf * 1.16f);
    }

    private static void DrawPixelShrubCluster(Color[] pixels, int width, int height, Vector2 center, int count)
    {
        Color shrub = new(0.22f, 0.42f, 0.18f, 1f);
        Color light = new(0.38f, 0.54f, 0.24f, 1f);
        for (int i = 0; i < count; i++)
        {
            float angle = i * 2.399963f;
            float radius = 0.014f + 0.055f * Mathf.Sqrt((i + 1f) / count);
            Vector2 p = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            int x = Mathf.RoundToInt(p.x * (width - 1));
            int y = Mathf.RoundToInt(p.y * (height - 1));
            FillRect(pixels, width, height, x - 2, y - 1, 4, 3, shrub);
            SetPixel(pixels, width, height, x - 1, y + 1, light);
        }
    }

    private static void DrawPixelMountainCluster(Color[] pixels, int width, int height, Vector2 center, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2 p = center + new Vector2((i - count * 0.5f) * 0.025f, Mathf.Sin(i * 1.7f) * 0.016f);
            DrawPixelMountain(pixels, width, height, p);
        }
    }

    private static void DrawPixelMountain(Color[] pixels, int width, int height, Vector2 p)
    {
        int cx = Mathf.RoundToInt(p.x * (width - 1));
        int cy = Mathf.RoundToInt(p.y * (height - 1));
        Color dark = new(0.35f, 0.33f, 0.31f, 1f);
        Color light = new(0.75f, 0.72f, 0.62f, 1f);
        for (int y = 0; y < 9; y++)
        {
            int half = 5 - y / 2;
            FillRect(pixels, width, height, cx - half, cy - 4 + y, half * 2 + 1, 1, dark);
        }
        FillRect(pixels, width, height, cx - 2, cy + 2, 2, 3, light);
    }

    private static void DrawPixelTown(Color[] pixels, int width, int height, Vector2 p, bool current, bool known)
    {
        int x = Mathf.RoundToInt(p.x * (width - 1));
        int y = Mathf.RoundToInt(p.y * (height - 1));
        Color wall = known ? new Color(0.86f, 0.66f, 0.34f, 1f) : new Color(0.34f, 0.30f, 0.24f, 1f);
        Color roof = current ? new Color(0.95f, 0.72f, 0.16f, 1f) : new Color(0.52f, 0.28f, 0.12f, 1f);
        FillRect(pixels, width, height, x - 8, y - 6, 16, 10, new Color(0.14f, 0.10f, 0.06f, 0.95f));
        FillRect(pixels, width, height, x - 5, y - 3, 10, 7, wall);
        FillRect(pixels, width, height, x - 7, y + 4, 14, 3, roof);
        FillRect(pixels, width, height, x - 1, y - 3, 3, 4, new Color(0.16f, 0.10f, 0.06f, 1f));
        FillRect(pixels, width, height, x + 4, y - 2, 2, 3, wall * 1.16f);
        if (current) FillRect(pixels, width, height, x + 7, y + 2, 2, 11, roof);
    }

    private static void DrawPixelPort(Color[] pixels, int width, int height, Vector2 p, bool riverPort)
    {
        int x = Mathf.RoundToInt(p.x * (width - 1));
        int y = Mathf.RoundToInt(p.y * (height - 1));
        Color pier = new(0.42f, 0.23f, 0.09f, 1f);
        Color wall = new(0.84f, 0.63f, 0.30f, 1f);
        FillRect(pixels, width, height, x - 7, y - 3, 14, 7, wall);
        FillRect(pixels, width, height, x - 9, y + 4, 18, 3, new Color(0.52f, 0.28f, 0.12f, 1f));
        FillRect(pixels, width, height, x - 2, y - 10, 4, 9, pier);
        FillRect(pixels, width, height, x + (riverPort ? 5 : -9), y - 8, 9, 3, pier);
    }

    private static void DrawPixelBridge(Color[] pixels, int width, int height, Vector2 p, int length)
    {
        int x = Mathf.RoundToInt(p.x * (width - 1));
        int y = Mathf.RoundToInt(p.y * (height - 1));
        Color dark = new(0.31f, 0.18f, 0.07f, 1f);
        Color plank = new(0.70f, 0.47f, 0.18f, 1f);
        FillRect(pixels, width, height, x - length / 2, y - 3, length, 7, dark);
        FillRect(pixels, width, height, x - length / 2 + 2, y - 1, length - 4, 3, plank);
        for (int i = -length / 2 + 4; i < length / 2; i += 6)
        {
            FillRect(pixels, width, height, x + i, y - 3, 2, 7, dark);
        }
    }

    private static void FillPixelCircle(Color[] pixels, int width, int height, int cx, int cy, int radius, Color color)
    {
        int r2 = radius * radius;
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= r2) SetPixel(pixels, width, height, cx + x, cy + y, color);
            }
        }
    }

    private static void FillRect(Color[] pixels, int width, int height, int x, int y, int w, int h, Color color)
    {
        for (int py = y; py < y + h; py++)
        {
            for (int px = x; px < x + w; px++)
            {
                SetPixel(pixels, width, height, px, py, color);
            }
        }
    }

    private static void SetPixel(Color[] pixels, int width, int height, int x, int y, Color color)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        pixels[y * width + x] = color;
    }

    private static Color GetPixel(Color[] pixels, int width, int height, int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return Color.clear;
        return pixels[y * width + x];
    }

    private static float PixelMapHash01(int x, int y)
    {
        uint h = (uint)(x * 374761393 + y * 668265263);
        h = (h ^ (h >> 13)) * 1274126177u;
        return (h ^ (h >> 16)) / 4294967295f;
    }
}
