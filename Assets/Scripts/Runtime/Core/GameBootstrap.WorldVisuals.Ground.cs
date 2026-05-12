using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private System.Collections.IEnumerator SetupGroundAsync()
    {
        groundRoot = new GameObject("Ground").transform;
        groundRoot.SetParent(worldRoot, false);

        int shoreRow = GridHeight - WaterRiverWidth;
        int beachNearRow = shoreRow - 1;
        int beachFarRow = shoreRow - 2;
        const float waterCellHeight = 0.22f;
        const float waterBaseTopHeight = 0.04f;
        const float waterSurfaceTop = 0.22f;
        // Opaque water body sits low; transparent surface stack floats above it.
        float waterSurfaceCenterY    = waterCellHeight - 0.005f;
        float waterMidSurfaceCenterY = waterCellHeight - 0.012f;
        float waterLowSurfaceCenterY = waterCellHeight - 0.019f;
        waterSurfaceTiles.Clear();
        waterBodyTiles.Clear();

        for (int x = 0; x < GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                bool isWater = waterCells.Contains(new Vector2Int(x, y));
                bool isNearBeach = y == beachNearRow || naturalBeachCells.Contains(new Vector2Int(x, y));
                bool isFarBeach = y == beachFarRow;
                bool isBeach = isNearBeach || isFarBeach;

                float terrainHeight = isWater ? waterBaseTopHeight : terrainHeights[x, y];
                float thickness = isWater ? 0.18f : 0.28f + terrainHeight;

                float bottomY = terrainHeight - thickness - 0.02f;
                GameObject groundTile = isWater
                    ? CreateWaterBodyCellMesh($"Water_{x}_{y}", x, y, waterBaseTopHeight, bottomY)
                    : CreateTerrainCellMesh($"Ground_{x}_{y}", x, y, bottomY, 0.01f);

                if (isWater)
                {
                    Vector2Int waterCell = new(x, y);
                    bool isLakeWater = lakeWaterCells.Contains(waterCell);
                    float t = isLakeWater
                        ? GetLakeWaterDepth01(x, y)
                        : (float)(y - shoreRow) / Mathf.Max(WaterRiverWidth - 1, 1);
                    Texture2D waterBodyTexture = isLakeWater
                        ? (lakeDeepTexture != null ? lakeDeepTexture : waterDeepMaterial?.mainTexture as Texture2D)
                        : (riverDeepTexture != null ? riverDeepTexture : waterDeepMaterial?.mainTexture as Texture2D);
                    Texture2D waterTopTexture = isLakeWater
                        ? (lakeSurfaceTexture != null ? lakeSurfaceTexture : waterBodyTexture)
                        : (riverSurfaceTexture != null ? riverSurfaceTexture : waterBodyTexture);
                    Vector2 waterTextureScale = isLakeWater ? new Vector2(0.58f, 0.58f) : new Vector2(0.74f, 0.42f);
                    Vector2 waterTextureOffset = GetGroundTextureOffset(x, y, isLakeWater ? 41 : 47);

                    // Solid tile: per-tile depth colour (instance material)
                    Color baseWaterColor = Color.Lerp(
                        new Color(0.26f, 0.62f, 0.78f),
                        new Color(0.03f, 0.14f, 0.38f),
                        t);
                    Renderer r = groundTile.GetComponent<Renderer>();
                    r.material = CreateSurfaceMaterial(waterBodyTexture, baseWaterColor, 0.92f);
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = true;
                    float wavePhase = Random.Range(0f, 10f);
                    waterBodyTiles.Add(new WaterBodyTileData
                    {
                        Transform = groundTile.transform,
                        Mesh = groundTile.GetComponent<MeshFilter>().sharedMesh,
                        BaseY = 0f,
                        BaseTopY = waterBaseTopHeight,
                        BottomY = bottomY,
                        CurrentTopY = waterBaseTopHeight,
                        Cell = new Vector2Int(x, y),
                        PhaseOffset = wavePhase
                    });

                    // Top shimmer layer - wide, light, animated bob
                    Color topColor = Color.Lerp(
                        new Color(0.86f, 0.98f, 1.00f, 0.11f),
                        new Color(0.14f, 0.44f, 0.78f, 0.055f), t);
                    GameObject waterSurface = CreateWaterSurfaceCellMesh($"WaterSurface_{x}_{y}", x, y, waterSurfaceCenterY, 1f);
                    Renderer surfaceRenderer = waterSurface.GetComponent<Renderer>();
                    surfaceRenderer.material = CreateTransparentOverlayMaterial(topColor, waterTopTexture, waterTextureScale, waterTextureOffset);
                    surfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    surfaceRenderer.receiveShadows = false;

                    waterSurfaceTiles.Add(new WaterSurfaceTileData
                    {
                        Renderer = surfaceRenderer,
                        Material = surfaceRenderer.material,
                        Transform = waterSurface.transform,
                        Mesh = waterSurface.GetComponent<MeshFilter>().sharedMesh,
                        BaseY = waterSurfaceCenterY,
                        CurrentTopY = waterSurfaceCenterY,
                        Cell = new Vector2Int(x, y),
                        BobAmplitude = y == shoreRow ? 0.016f : 0.024f,
                        BobSpeed = y == shoreRow ? 0.68f : 0.86f,
                        PhaseOffset = wavePhase,
                        LayerIndex = 0
                    });

                    // Mid layer - slightly smaller, more saturated
                    Color midColor = Color.Lerp(
                        new Color(0.44f, 0.78f, 0.92f, 0.09f),
                        new Color(0.07f, 0.28f, 0.58f, 0.055f), t);
                    GameObject waterMidSurface = CreateWaterSurfaceCellMesh($"WaterMidSurface_{x}_{y}", x, y, waterMidSurfaceCenterY, 1.0f);
                    Renderer midSurfaceRenderer = waterMidSurface.GetComponent<Renderer>();
                    midSurfaceRenderer.material = CreateTransparentOverlayMaterial(midColor, waterTopTexture, waterTextureScale * 0.82f, waterTextureOffset + new Vector2(0.19f, 0.11f));
                    midSurfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    midSurfaceRenderer.receiveShadows = false;
                    waterSurfaceTiles.Add(new WaterSurfaceTileData
                    {
                        Renderer = midSurfaceRenderer,
                        Material = midSurfaceRenderer.material,
                        Transform = waterMidSurface.transform,
                        Mesh = waterMidSurface.GetComponent<MeshFilter>().sharedMesh,
                        BaseY = waterMidSurfaceCenterY,
                        CurrentTopY = waterMidSurfaceCenterY,
                        Cell = new Vector2Int(x, y),
                        BobAmplitude = y == shoreRow ? 0.013f : 0.018f,
                        BobSpeed = y == shoreRow ? 0.64f : 0.82f,
                        PhaseOffset = wavePhase,
                        LayerIndex = 1
                    });

                    // Low layer - noticeably smaller, darkest, acts as "depth shadow"
                    Color lowColor = Color.Lerp(
                        new Color(0.16f, 0.46f, 0.68f, 0.16f),
                        new Color(0.02f, 0.12f, 0.34f, 0.10f), t);
                    GameObject waterLowSurface = CreateWaterSurfaceCellMesh($"WaterLowSurface_{x}_{y}", x, y, waterLowSurfaceCenterY, 1f);
                    Renderer lowSurfaceRenderer = waterLowSurface.GetComponent<Renderer>();
                    lowSurfaceRenderer.material = CreateTransparentOverlayMaterial(lowColor, waterBodyTexture, waterTextureScale * 0.68f, waterTextureOffset + new Vector2(0.31f, 0.27f));
                    lowSurfaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    lowSurfaceRenderer.receiveShadows = false;
                    waterSurfaceTiles.Add(new WaterSurfaceTileData
                    {
                        Renderer = lowSurfaceRenderer,
                        Material = lowSurfaceRenderer.material,
                        Transform = waterLowSurface.transform,
                        Mesh = waterLowSurface.GetComponent<MeshFilter>().sharedMesh,
                        BaseY = waterLowSurfaceCenterY,
                        CurrentTopY = waterLowSurfaceCenterY,
                        Cell = new Vector2Int(x, y),
                        BobAmplitude = y == shoreRow ? 0.010f : 0.014f,
                        BobSpeed = y == shoreRow ? 0.6f : 0.76f,
                        PhaseOffset = wavePhase,
                        LayerIndex = 2
                    });

                    if (y == shoreRow)
                    {
                        GameObject foamStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        foamStrip.name = $"WaterFoam_{x}_{y}";
                        foamStrip.transform.SetParent(groundRoot, false);
                        foamStrip.transform.position = new Vector3(x + 0.5f, waterSurfaceTop + 0.006f, y + 0.12f);
                        foamStrip.transform.localScale = new Vector3(0.94f, 0.012f, 0.12f);
                        Renderer foamRenderer = foamStrip.GetComponent<Renderer>();
                        foamRenderer.sharedMaterial = CreateTransparentOverlayMaterial(new Color(0.68f, 0.84f, 0.86f, 0.28f));
                        foamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        foamRenderer.receiveShadows = false;
                        if (foamStrip.TryGetComponent(out Collider foamCollider)) Object.Destroy(foamCollider);
                    }
                }
                else if (isNearBeach)
                {
                    ApplyBeachGroundMaterial(groundTile, x, y, true);
                    ConfigureStaticVisual(groundTile);
                }
                else if (isFarBeach)
                {
                    ApplyBeachGroundMaterial(groundTile, x, y, false);
                    ConfigureStaticVisual(groundTile);
                }
                else
                {
                    ApplyGroundCellSurfaceMaterial(groundTile, x, y);
                    ConfigureStaticVisual(groundTile);
                }
            }

            if (x % 8 == 7) yield return null;
        }

        CreateDioramaBase();
    }

    private GameObject CreateTerrainCellMesh(string name, int x, int y, float bottomY, float lift)
    {
        GameObject cell = new(name);
        cell.transform.SetParent(groundRoot, false);

        float x0 = x;
        float x1 = x + 1f;
        float z0 = y;
        float z1 = y + 1f;

        float h00 = SampleTerrainHeight(x0, z0) + lift;
        float h10 = SampleTerrainHeight(x1, z0) + lift;
        float h01 = SampleTerrainHeight(x0, z1) + lift;
        float h11 = SampleTerrainHeight(x1, z1) + lift;
        CreateCellBoxMesh(cell, x0, x1, z0, z1, h00, h10, h01, h11, bottomY);
        return cell;
    }

    private GameObject CreateWaterBodyCellMesh(string name, int x, int y, float topY, float bottomY)
    {
        GameObject cell = new(name);
        cell.transform.SetParent(groundRoot, false);
        CreateCellBoxMesh(cell, x, x + 1f, y, y + 1f, topY, topY, topY, topY, bottomY);
        cell.GetComponent<MeshFilter>().sharedMesh.MarkDynamic();
        return cell;
    }

    private GameObject CreateWaterSurfaceCellMesh(string name, int x, int y, float topY, float size)
    {
        GameObject cell = new(name);
        cell.transform.SetParent(groundRoot, false);

        float inset = (1f - size) * 0.5f;
        float x0 = x + inset;
        float x1 = x + 1f - inset;
        float z0 = y + inset;
        float z1 = y + 1f - inset;

        Mesh mesh = new();
        mesh.name = $"{name}_Mesh";
        mesh.MarkDynamic();
        mesh.vertices = new[]
        {
            new Vector3(x0, topY, z0),
            new Vector3(x1, topY, z0),
            new Vector3(x0, topY, z1),
            new Vector3(x1, topY, z1),
        };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = cell.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        cell.AddComponent<MeshRenderer>();
        return cell;
    }

    private static void CreateCellBoxMesh(
        GameObject target,
        float x0,
        float x1,
        float z0,
        float z1,
        float h00,
        float h10,
        float h01,
        float h11,
        float bottomY)
    {
        Mesh mesh = new();
        mesh.name = $"{target.name}_Mesh";
        mesh.vertices = new[]
        {
            new Vector3(x0, h00, z0),
            new Vector3(x1, h10, z0),
            new Vector3(x0, h01, z1),
            new Vector3(x1, h11, z1),
            new Vector3(x0, bottomY, z0),
            new Vector3(x1, bottomY, z0),
            new Vector3(x0, bottomY, z1),
            new Vector3(x1, bottomY, z1),
        };
        mesh.triangles = new[]
        {
            0, 2, 1, 1, 2, 3,
            4, 5, 6, 5, 7, 6,
            0, 1, 4, 1, 5, 4,
            2, 6, 3, 3, 6, 7,
            0, 4, 2, 2, 4, 6,
            1, 3, 5, 3, 7, 5,
        };
        mesh.uv = new[]
        {
            new Vector2(x0, z0),
            new Vector2(x1, z0),
            new Vector2(x0, z1),
            new Vector2(x1, z1),
            new Vector2(x0, z0),
            new Vector2(x1, z0),
            new Vector2(x0, z1),
            new Vector2(x1, z1),
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        target.AddComponent<MeshFilter>().sharedMesh = mesh;
        target.AddComponent<MeshRenderer>();
    }


    private void CreateDioramaBase()
    {
        Vector3 center = new(GridWidth * 0.5f, -0.38f, GridHeight * 0.5f);

        GameObject plinth = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plinth.name = "DioramaPlinth";
        plinth.transform.SetParent(worldRoot, false);
        plinth.transform.position = center;
        plinth.transform.localScale = new Vector3(GridWidth + 2.4f, 0.82f, GridHeight + 2.4f);
        ApplyColor(plinth, new Color(0.73f, 0.66f, 0.56f));

        GameObject baseLip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseLip.name = "DioramaLip";
        baseLip.transform.SetParent(worldRoot, false);
        baseLip.transform.position = new Vector3(center.x, -0.12f, center.z);
        baseLip.transform.localScale = new Vector3(GridWidth + 0.8f, 0.14f, GridHeight + 0.8f);
        ApplyColor(baseLip, new Color(0.87f, 0.81f, 0.7f));

        CreateDioramaBoundary(new Vector3(GridWidth * 0.5f, 0.22f, -0.3f), new Vector3(GridWidth + 0.9f, 0.32f, 0.24f));
        CreateDioramaBoundary(new Vector3(GridWidth * 0.5f, 0.22f, GridHeight + 0.3f), new Vector3(GridWidth + 0.9f, 0.32f, 0.24f));
        CreateDioramaBoundary(new Vector3(-0.3f, 0.22f, GridHeight * 0.5f), new Vector3(0.24f, 0.32f, GridHeight + 0.9f));
        CreateDioramaBoundary(new Vector3(GridWidth + 0.3f, 0.22f, GridHeight * 0.5f), new Vector3(0.24f, 0.32f, GridHeight + 0.9f));
    }

    private void CreateDioramaBoundary(Vector3 position, Vector3 scale)
    {
        GameObject boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundary.transform.SetParent(worldRoot, false);
        boundary.transform.position = position;
        boundary.transform.localScale = scale;
        ApplyColor(boundary, new Color(0.9f, 0.85f, 0.75f));
    }
}
