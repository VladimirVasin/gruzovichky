using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap
{
    private enum GroundTextureTransitionKind
    {
        None,
        Dry,
        Grass,
        Forest,
        Beach
    }

    private sealed class GroundTransitionMeshBuilder
    {
        public readonly List<Vector3> Vertices = new();
        public readonly List<Vector2> Uvs = new();
        public readonly List<int> Triangles = new();
    }

    private void CreateGroundTextureTransitionOverlays()
    {
        if (groundRoot == null)
        {
            return;
        }

        Dictionary<GroundTextureTransitionKind, GroundTransitionMeshBuilder> builders = new();
        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                Vector2Int cell = new(x, y);
                GroundTextureTransitionKind here = GetGroundTextureTransitionKind(cell);
                if (here == GroundTextureTransitionKind.None)
                {
                    continue;
                }

                TryAddGroundTextureTransition(builders, cell, here, new Vector2Int(x + 1, y), verticalBoundary: true);
                TryAddGroundTextureTransition(builders, cell, here, new Vector2Int(x, y + 1), verticalBoundary: false);
            }
        }

        foreach (KeyValuePair<GroundTextureTransitionKind, GroundTransitionMeshBuilder> pair in builders)
        {
            if (pair.Value.Vertices.Count == 0)
            {
                continue;
            }

            GameObject root = new($"GroundTextureTransition_{pair.Key}");
            root.transform.SetParent(groundRoot, false);
            Mesh mesh = new() { name = $"{root.name}_Mesh" };
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(pair.Value.Vertices);
            mesh.SetUVs(0, pair.Value.Uvs);
            mesh.SetTriangles(pair.Value.Triangles, 0);
            mesh.RecalculateBounds();

            MeshFilter filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = CreateGroundTextureTransitionMaterial(pair.Key);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
    }

    private void QueueSurfaceTransitionOverlayRebuild()
    {
        surfaceTransitionOverlayRebuildPending = true;
        if (!suppressUnifiedRoadVisualRebuild)
        {
            FlushSurfaceTransitionOverlayRebuild();
        }
    }

    private void FlushSurfaceTransitionOverlayRebuild()
    {
        if (!surfaceTransitionOverlayRebuildPending)
        {
            return;
        }

        surfaceTransitionOverlayRebuildPending = false;
        RebuildSurfaceTransitionOverlays();
    }

    private void RebuildSurfaceTransitionOverlays()
    {
        RebuildGroundTextureTransitionOverlays();
        RebuildWaterShoreTransitionOverlays();
    }

    private void RebuildGroundTextureTransitionOverlays()
    {
        if (groundRoot == null)
        {
            return;
        }

        DestroyChildrenWithPrefix(groundRoot, "GroundTextureTransition_");
        CreateGroundTextureTransitionOverlays();
    }

    private void RebuildWaterShoreTransitionOverlays()
    {
        if (waterEffectsRoot == null)
        {
            return;
        }

        DestroyChildrenWithPrefix(waterEffectsRoot, "WaterShoreTransition_");
        CreateWaterShoreTransitionOverlays();
    }

    private static void DestroyChildrenWithPrefix(Transform parent, string prefix)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child.name.StartsWith(prefix, System.StringComparison.Ordinal))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void TryAddGroundTextureTransition(
        Dictionary<GroundTextureTransitionKind, GroundTransitionMeshBuilder> builders,
        Vector2Int cell,
        GroundTextureTransitionKind here,
        Vector2Int neighbor,
        bool verticalBoundary)
    {
        GroundTextureTransitionKind there = GetGroundTextureTransitionKind(neighbor);
        if (there == GroundTextureTransitionKind.None || there == here)
        {
            return;
        }

        GroundTextureTransitionKind overlayKind = GetGroundTextureOverlayKind(here, there);
        if (!builders.TryGetValue(overlayKind, out GroundTransitionMeshBuilder builder))
        {
            builder = new GroundTransitionMeshBuilder();
            builders[overlayKind] = builder;
        }

        float noise = Mathf.PerlinNoise((cell.x + 1) * 0.37f + (verticalBoundary ? 2.1f : 7.2f), (cell.y + 1) * 0.41f + 5.3f);
        float centerX = verticalBoundary ? cell.x + 1f : cell.x + 0.5f;
        float centerZ = verticalBoundary ? cell.y + 0.5f : cell.y + 1f;
        float tangentJitter = (noise - 0.5f) * 0.34f;
        if (verticalBoundary)
        {
            centerZ += tangentJitter;
        }
        else
        {
            centerX += tangentJitter;
        }

        float across = Mathf.Lerp(0.74f, 1.18f, noise);
        float along = Mathf.Lerp(1.05f, 1.58f, 1f - noise);
        Vector3 tangent = verticalBoundary ? Vector3.forward : Vector3.right;
        Vector3 normal = verticalBoundary ? Vector3.right : Vector3.forward;
        AddGroundTransitionPatch(builder, new Vector2(centerX, centerZ), tangent, normal, along, across, noise);
    }

    private GroundTextureTransitionKind GetGroundTextureTransitionKind(Vector2Int cell)
    {
        if (!IsInsideGrid(cell) || waterCells.Contains(cell) || IsRoadGroundReplacementCell(cell))
        {
            return GroundTextureTransitionKind.None;
        }

        int shoreRow = GridHeight - WaterRiverWidth;
        if (cell.y == shoreRow - 1 || cell.y == shoreRow - 2 || IsNaturalBeachCell(cell))
        {
            return GroundTextureTransitionKind.Beach;
        }

        float forestBlend = GetForestGroundBlend01(cell.x, cell.y);
        if (forestBlend > 0.46f)
        {
            return GroundTextureTransitionKind.Forest;
        }

        return IsGrassGroundCell(cell.x, cell.y)
            ? GroundTextureTransitionKind.Grass
            : GroundTextureTransitionKind.Dry;
    }

    private static GroundTextureTransitionKind GetGroundTextureOverlayKind(GroundTextureTransitionKind a, GroundTextureTransitionKind b)
    {
        if (a == GroundTextureTransitionKind.Beach || b == GroundTextureTransitionKind.Beach)
        {
            return GroundTextureTransitionKind.Beach;
        }

        if (a == GroundTextureTransitionKind.Forest || b == GroundTextureTransitionKind.Forest)
        {
            return GroundTextureTransitionKind.Forest;
        }

        if (a == GroundTextureTransitionKind.Grass || b == GroundTextureTransitionKind.Grass)
        {
            return GroundTextureTransitionKind.Grass;
        }

        return GroundTextureTransitionKind.Dry;
    }

    private void AddGroundTransitionPatch(
        GroundTransitionMeshBuilder builder,
        Vector2 center,
        Vector3 tangent,
        Vector3 normal,
        float along,
        float across,
        float noise)
    {
        Vector3 tangentHalf = tangent * (along * 0.5f);
        Vector3 normalHalf = normal * (across * 0.5f);
        Vector3 p0 = GroundTransitionPoint(center, -tangentHalf - normalHalf);
        Vector3 p1 = GroundTransitionPoint(center, tangentHalf - normalHalf);
        Vector3 p2 = GroundTransitionPoint(center, -tangentHalf + normalHalf);
        Vector3 p3 = GroundTransitionPoint(center, tangentHalf + normalHalf);

        int start = builder.Vertices.Count;
        builder.Vertices.Add(p0);
        builder.Vertices.Add(p1);
        builder.Vertices.Add(p2);
        builder.Vertices.Add(p3);

        float uvPad = Mathf.Lerp(0f, 0.18f, noise);
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

    private Vector3 GroundTransitionPoint(Vector2 center, Vector3 offset)
    {
        float x = center.x + offset.x;
        float z = center.y + offset.z;
        return new Vector3(x, SampleTerrainHeight(x, z) + 0.013f, z);
    }

    private Material CreateGroundTextureTransitionMaterial(GroundTextureTransitionKind kind)
    {
        Texture2D source = kind switch
        {
            GroundTextureTransitionKind.Forest => forestSurfaceTexture,
            GroundTextureTransitionKind.Grass => grassSurfaceTexture,
            GroundTextureTransitionKind.Beach => beachSurfaceTexture,
            _ => groundSurfaceTexture
        };

        Texture2D blendTexture = CreateGroundTextureTransitionBlendTexture(source, $"GroundTransition_{kind}");
        Material material = CreateTransparentOverlayMaterial(new Color(1f, 1f, 1f, GetGroundTextureTransitionAlpha(kind)), blendTexture, Vector2.one, Vector2.zero);
        material.renderQueue = (int)RenderQueue.Transparent + 8;
        return material;
    }

    private static float GetGroundTextureTransitionAlpha(GroundTextureTransitionKind kind)
    {
        return kind switch
        {
            GroundTextureTransitionKind.Forest => 0.34f,
            GroundTextureTransitionKind.Grass => 0.30f,
            GroundTextureTransitionKind.Beach => 0.26f,
            _ => 0.22f
        };
    }

    private static Texture2D CreateGroundTextureTransitionBlendTexture(Texture2D source, string textureName)
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
                float edgeFade = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.02f, 0.34f, edgeDistance));
                float lobe = Mathf.PerlinNoise(u * 3.6f + 11.7f, v * 3.2f + 19.1f);
                float detail = Mathf.PerlinNoise(u * 11.3f + 2.4f, v * 9.7f + 6.8f);
                float irregularFade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(lobe * 0.75f + detail * 0.25f));
                color.a = edgeFade * Mathf.Lerp(0.58f, 1f, irregularFade);
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(false, false);
        return texture;
    }
}
