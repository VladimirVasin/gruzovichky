using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap
{
    private enum WaterShoreTransitionKind
    {
        WaterFeather,
        FoamLace
    }

    private sealed class WaterShoreTransitionMeshBuilder
    {
        public readonly List<Vector3> Vertices = new();
        public readonly List<Vector2> Uvs = new();
        public readonly List<int> Triangles = new();
    }

    private void CreateWaterShoreTransitionOverlays()
    {
        if (waterEffectsRoot == null || waterCells.Count == 0)
        {
            return;
        }

        Dictionary<WaterShoreTransitionKind, WaterShoreTransitionMeshBuilder> builders = new();
        foreach (Vector2Int waterCell in waterCells)
        {
            TryAddWaterShoreTransition(builders, waterCell, Vector2Int.right);
            TryAddWaterShoreTransition(builders, waterCell, Vector2Int.left);
            TryAddWaterShoreTransition(builders, waterCell, Vector2Int.up);
            TryAddWaterShoreTransition(builders, waterCell, Vector2Int.down);
        }

        foreach (KeyValuePair<WaterShoreTransitionKind, WaterShoreTransitionMeshBuilder> pair in builders)
        {
            if (pair.Value.Vertices.Count == 0)
            {
                continue;
            }

            GameObject root = new($"WaterShoreTransition_{pair.Key}");
            root.transform.SetParent(waterEffectsRoot, false);
            Mesh mesh = new() { name = $"{root.name}_Mesh" };
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(pair.Value.Vertices);
            mesh.SetUVs(0, pair.Value.Uvs);
            mesh.SetTriangles(pair.Value.Triangles, 0);
            mesh.RecalculateBounds();

            MeshFilter filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateWaterShoreTransitionMaterial(pair.Key);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void TryAddWaterShoreTransition(
        Dictionary<WaterShoreTransitionKind, WaterShoreTransitionMeshBuilder> builders,
        Vector2Int waterCell,
        Vector2Int direction)
    {
        Vector2Int landCell = waterCell + direction;
        if (!IsInsideGrid(landCell) || waterCells.Contains(landCell) || IsRoadGroundReplacementCell(landCell))
        {
            return;
        }

        float noise = Mathf.PerlinNoise((waterCell.x + 5) * 0.31f + direction.x * 3.7f, (waterCell.y + 7) * 0.29f + direction.y * 5.1f);
        Vector2 normal2 = new(direction.x, direction.y);
        Vector2 tangent2 = direction.x != 0 ? Vector2.up : Vector2.right;
        Vector2 center = new(
            waterCell.x + 0.5f + direction.x * 0.5f,
            waterCell.y + 0.5f + direction.y * 0.5f);

        center += normal2 * Mathf.Lerp(0.02f, 0.16f, noise);
        center += tangent2 * ((noise - 0.5f) * 0.30f);

        if (!builders.TryGetValue(WaterShoreTransitionKind.WaterFeather, out WaterShoreTransitionMeshBuilder featherBuilder))
        {
            featherBuilder = new WaterShoreTransitionMeshBuilder();
            builders[WaterShoreTransitionKind.WaterFeather] = featherBuilder;
        }

        AddWaterShoreTransitionPatch(
            featherBuilder,
            center,
            new Vector3(tangent2.x, 0f, tangent2.y),
            new Vector3(normal2.x, 0f, normal2.y),
            Mathf.Lerp(1.18f, 1.72f, noise),
            Mathf.Lerp(1.04f, 1.58f, 1f - noise),
            0.058f,
            noise);

        float foamNoise = Mathf.PerlinNoise((waterCell.x + 11) * 0.53f, (waterCell.y + 13) * 0.49f);
        if (foamNoise < 0.28f)
        {
            return;
        }

        if (!builders.TryGetValue(WaterShoreTransitionKind.FoamLace, out WaterShoreTransitionMeshBuilder foamBuilder))
        {
            foamBuilder = new WaterShoreTransitionMeshBuilder();
            builders[WaterShoreTransitionKind.FoamLace] = foamBuilder;
        }

        AddWaterShoreTransitionPatch(
            foamBuilder,
            center - normal2 * 0.06f + tangent2 * ((foamNoise - 0.5f) * 0.18f),
            new Vector3(tangent2.x, 0f, tangent2.y),
            new Vector3(normal2.x, 0f, normal2.y),
            Mathf.Lerp(0.64f, 1.16f, foamNoise),
            Mathf.Lerp(0.18f, 0.42f, noise),
            0.072f,
            foamNoise);
    }

    private void AddWaterShoreTransitionPatch(
        WaterShoreTransitionMeshBuilder builder,
        Vector2 center,
        Vector3 tangent,
        Vector3 normal,
        float along,
        float across,
        float lift,
        float noise)
    {
        Vector3 tangentHalf = tangent * (along * 0.5f);
        Vector3 normalHalf = normal * (across * 0.5f);
        Vector3 p0 = WaterShoreTransitionPoint(center, -tangentHalf - normalHalf, lift);
        Vector3 p1 = WaterShoreTransitionPoint(center, tangentHalf - normalHalf, lift);
        Vector3 p2 = WaterShoreTransitionPoint(center, -tangentHalf + normalHalf, lift);
        Vector3 p3 = WaterShoreTransitionPoint(center, tangentHalf + normalHalf, lift);

        int start = builder.Vertices.Count;
        builder.Vertices.Add(p0);
        builder.Vertices.Add(p1);
        builder.Vertices.Add(p2);
        builder.Vertices.Add(p3);

        float uvPad = Mathf.Lerp(0.03f, 0.22f, noise);
        builder.Uvs.Add(new Vector2(uvPad, uvPad));
        builder.Uvs.Add(new Vector2(1f - uvPad, uvPad));
        builder.Uvs.Add(new Vector2(uvPad, 1f - uvPad));
        builder.Uvs.Add(new Vector2(1f - uvPad, 1f - uvPad));

        bool clockwiseFromTop = Vector3.Cross(p2 - p0, p1 - p0).y < 0f;
        if (clockwiseFromTop)
        {
            builder.Triangles.Add(start);
            builder.Triangles.Add(start + 1);
            builder.Triangles.Add(start + 2);
            builder.Triangles.Add(start + 1);
            builder.Triangles.Add(start + 3);
            builder.Triangles.Add(start + 2);
        }
        else
        {
            builder.Triangles.Add(start);
            builder.Triangles.Add(start + 2);
            builder.Triangles.Add(start + 1);
            builder.Triangles.Add(start + 1);
            builder.Triangles.Add(start + 2);
            builder.Triangles.Add(start + 3);
        }
    }

    private Vector3 WaterShoreTransitionPoint(Vector2 center, Vector3 offset, float lift)
    {
        float x = Mathf.Clamp(center.x + offset.x, 0.02f, GridWidth - 0.02f);
        float z = Mathf.Clamp(center.y + offset.z, 0.02f, GridHeight - 0.02f);
        return new Vector3(x, SampleTerrainHeight(x, z) + lift, z);
    }

    private Material CreateWaterShoreTransitionMaterial(WaterShoreTransitionKind kind)
    {
        bool foam = kind == WaterShoreTransitionKind.FoamLace;
        Texture2D source = foam
            ? (riverFoamTexture != null ? riverFoamTexture : riverRippleTexture)
            : (lakeRippleTexture != null ? lakeRippleTexture : riverRippleTexture != null ? riverRippleTexture : lakeSurfaceTexture);

        Texture2D texture = CreateWaterShoreTransitionTexture(source, foam ? "WaterShoreFoamLace" : "WaterShoreFeather", foam);
        Color tint = foam
            ? new Color(0.78f, 0.90f, 0.86f, 0.24f)
            : new Color(0.48f, 0.78f, 0.86f, 0.34f);
        Material material = CreateTransparentOverlayMaterial(tint, texture, Vector2.one, Vector2.zero);
        material.renderQueue = (int)RenderQueue.Transparent + (foam ? 16 : 14);
        return material;
    }

    private static Texture2D CreateWaterShoreTransitionTexture(Texture2D source, string textureName, bool foam)
    {
        const int size = 96;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
        {
            name = textureName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int y = 0; y < size; y++)
        {
            float v = y / (float)(size - 1);
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                Color color = source != null
                    ? source.GetPixelBilinear(u, v)
                    : Color.white;

                float edgeDistance = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
                float edgeFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.01f, foam ? 0.28f : 0.36f, edgeDistance));
                float largeNoise = Mathf.PerlinNoise(u * 3.1f + 4.7f, v * 3.4f + 8.2f);
                float detailNoise = Mathf.PerlinNoise(u * 13.2f + 12.1f, v * 11.6f + 3.8f);
                float brokenMask = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(largeNoise * 0.74f + detailNoise * 0.26f));

                if (foam)
                {
                    float lace = Mathf.Clamp01((detailNoise - 0.44f) * 1.8f + largeNoise * 0.34f);
                    color = Color.Lerp(color, Color.white, 0.42f);
                    color.a = edgeFade * lace;
                }
                else
                {
                    color.a = edgeFade * Mathf.Lerp(0.48f, 1f, brokenMask);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(false, false);
        return texture;
    }
}
