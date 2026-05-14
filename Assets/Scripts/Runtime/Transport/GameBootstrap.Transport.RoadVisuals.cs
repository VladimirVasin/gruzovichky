using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float UnifiedRoadSurfaceLift = 0.016f;
    private const float UnifiedRoadMaskLift = UnifiedRoadSurfaceLift + 0.006f;
    private const float UnifiedRoadShoulderArcLift = UnifiedRoadSurfaceLift + 0.011f;
    private const float RoadTileSurfaceLift = 0.018f;

    private void RebuildUnifiedRoadVisuals()
    {
        if (roadsRoot == null)
        {
            return;
        }

        if (unifiedRoadVisualRoot == null)
        {
            unifiedRoadVisualRoot = new GameObject("UnifiedRoadVisuals").transform;
            unifiedRoadVisualRoot.SetParent(roadsRoot, false);
        }

        for (int i = unifiedRoadVisualRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(unifiedRoadVisualRoot.GetChild(i).gameObject);
        }

        HashSet<Vector2Int> horizontalVisited = new();
        HashSet<Vector2Int> verticalVisited = new();
        HashSet<Vector2Int> coveredCells = new();

        foreach (Vector2Int cell in roadCells)
        {
            if (!IsRoadVisualReady(cell) || horizontalVisited.Contains(cell) || !IsHorizontalUnifiedRoadCell(cell))
            {
                continue;
            }

            int startX = cell.x;
            while (IsHorizontalUnifiedRoadCell(new Vector2Int(startX - 1, cell.y)))
            {
                startX--;
            }

            int endX = cell.x;
            while (IsHorizontalUnifiedRoadCell(new Vector2Int(endX + 1, cell.y)))
            {
                endX++;
            }

            for (int x = startX; x <= endX; x++)
            {
                Vector2Int c = new(x, cell.y);
                horizontalVisited.Add(c);
                coveredCells.Add(c);
            }

            CreateUnifiedRoadRun(new Vector2Int(startX, cell.y), endX - startX + 1, horizontal: true);
        }

        foreach (Vector2Int cell in roadCells)
        {
            if (!IsRoadVisualReady(cell) || coveredCells.Contains(cell) || verticalVisited.Contains(cell) || !IsVerticalUnifiedRoadCell(cell))
            {
                continue;
            }

            int startY = cell.y;
            while (IsVerticalUnifiedRoadCell(new Vector2Int(cell.x, startY - 1)))
            {
                startY--;
            }

            int endY = cell.y;
            while (IsVerticalUnifiedRoadCell(new Vector2Int(cell.x, endY + 1)))
            {
                endY++;
            }

            for (int y = startY; y <= endY; y++)
            {
                Vector2Int c = new(cell.x, y);
                verticalVisited.Add(c);
                coveredCells.Add(c);
            }

            CreateUnifiedRoadRun(new Vector2Int(cell.x, startY), endY - startY + 1, horizontal: false);
        }

        foreach (Vector2Int cell in roadCells)
        {
            if (!IsRoadVisualReady(cell) || coveredCells.Contains(cell))
            {
                continue;
            }

            CreateUnifiedRoadCap(cell);
        }

        AddUnifiedRoadCornerMasks();
        AddUnifiedRoadMarkings();
        SetRoadTileFallbackVisibility(visible: true);
    }
    private bool IsHorizontalUnifiedRoadCell(Vector2Int cell)
    {
        return IsRoadVisualReady(cell) &&
               (IsRoadVisualReady(cell + Vector2Int.left) || IsRoadVisualReady(cell + Vector2Int.right));
    }
    private bool IsVerticalUnifiedRoadCell(Vector2Int cell)
    {
        return IsRoadVisualReady(cell) &&
               (IsRoadVisualReady(cell + Vector2Int.down) || IsRoadVisualReady(cell + Vector2Int.up));
    }
    private void CreateUnifiedRoadRun(Vector2Int startCell, int length, bool horizontal)
    {
        if (length <= 0 || unifiedRoadVisualRoot == null)
        {
            return;
        }

        string name = horizontal
            ? $"UnifiedRoad_H_{startCell.x}_{startCell.y}_{length}"
            : $"UnifiedRoad_V_{startCell.x}_{startCell.y}_{length}";
        CreateUnifiedRoadSurfaceStrip(name, startCell, length, horizontal);
    }
    private void CreateUnifiedRoadCap(Vector2Int cell)
    {
        if (unifiedRoadVisualRoot == null)
        {
            return;
        }

        GameObject cap = new($"UnifiedRoad_Cap_{cell.x}_{cell.y}");
        cap.transform.SetParent(unifiedRoadVisualRoot, false);

        Mesh mesh = new();
        mesh.name = $"{cap.name}_Mesh";

        float x0 = cell.x - 0.01f;
        float x1 = cell.x + 1.01f;
        float z0 = cell.y - 0.01f;
        float z1 = cell.y + 1.01f;
        const float lift = UnifiedRoadSurfaceLift;

        mesh.vertices = new[]
        {
            new Vector3(x0, SampleRoadSurfaceHeight(x0, z0) + lift, z0),
            new Vector3(x1, SampleRoadSurfaceHeight(x1, z0) + lift, z0),
            new Vector3(x0, SampleRoadSurfaceHeight(x0, z1) + lift, z1),
            new Vector3(x1, SampleRoadSurfaceHeight(x1, z1) + lift, z1),
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

        MeshFilter filter = cap.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        cap.AddComponent<MeshRenderer>();
        cap.name = $"UnifiedRoad_Cap_{cell.x}_{cell.y}";
        ApplyStylizedRoadMaterial(cap, cell.x, cell.y, isHighway: false, isShoulder: false);
        ConfigureStaticVisual(cap);
    }
    private void CreateUnifiedRoadSurfaceStrip(string name, Vector2Int startCell, int length, bool horizontal)
    {
        GameObject strip = new(name);
        strip.transform.SetParent(unifiedRoadVisualRoot, false);

        Mesh mesh = new();
        mesh.name = $"{name}_Mesh";

        const float halfWidth = 0.49f;
        const int subdivisionsPerCell = 4;
        const float lift = UnifiedRoadSurfaceLift;
        int segmentCount = Mathf.Max(1, length * subdivisionsPerCell);
        int vertexCount = (segmentCount + 1) * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[segmentCount * 6];

        for (int i = 0; i <= segmentCount; i++)
        {
            float distance = i / (float)subdivisionsPerCell;
            float u = distance;
            if (horizontal)
            {
                float x = startCell.x + distance;
                float zA = startCell.y + 0.5f - halfWidth;
                float zB = startCell.y + 0.5f + halfWidth;
                vertices[i * 2] = new Vector3(x, SampleRoadSurfaceHeight(x, zA) + lift, zA);
                vertices[i * 2 + 1] = new Vector3(x, SampleRoadSurfaceHeight(x, zB) + lift, zB);
            }
            else
            {
                float z = startCell.y + distance;
                float xA = startCell.x + 0.5f - halfWidth;
                float xB = startCell.x + 0.5f + halfWidth;
                vertices[i * 2] = new Vector3(xA, SampleRoadSurfaceHeight(xA, z) + lift, z);
                vertices[i * 2 + 1] = new Vector3(xB, SampleRoadSurfaceHeight(xB, z) + lift, z);
            }

            uvs[i * 2] = new Vector2(u, 0f);
            uvs[i * 2 + 1] = new Vector2(u, 1f);
        }

        for (int i = 0; i < segmentCount; i++)
        {
            int v = i * 2;
            int t = i * 6;
            triangles[t] = v;
            triangles[t + 1] = v + 2;
            triangles[t + 2] = v + 1;
            triangles[t + 3] = v + 1;
            triangles[t + 4] = v + 2;
            triangles[t + 5] = v + 3;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = strip.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        strip.AddComponent<MeshRenderer>();
        ApplyStylizedRoadMaterial(strip, startCell.x, startCell.y, isHighway: false, isShoulder: false);
        ConfigureStaticVisual(strip);
    }

    private void AddUnifiedRoadCornerMasks()
    {
        if (unifiedRoadVisualRoot == null)
        {
            return;
        }

        foreach (Vector2Int cell in roadCells)
        {
            if (!IsRoadVisualReady(cell) || !TryGetRoadCorner(cell, out int horizontalSign, out int verticalSign))
            {
                continue;
            }

            CreateUnifiedRoadCornerMask(cell, horizontalSign, verticalSign);
            CreateUnifiedRoadCornerShoulderArc(cell, horizontalSign, verticalSign);
        }
    }

    private void CreateUnifiedRoadCornerMask(Vector2Int cell, int horizontalSign, int verticalSign)
    {
        GameObject mask = new($"UnifiedRoad_CornerMask_{cell.x}_{cell.y}");
        mask.transform.SetParent(unifiedRoadVisualRoot, false);

        Mesh mesh = new();
        mesh.name = $"{mask.name}_Mesh";

        const int segments = 10;
        const float radius = 0.62f;
        const float lift = UnifiedRoadMaskLift;

        Vector3 corner = new(
            cell.x + (horizontalSign > 0 ? 1f : 0f),
            0f,
            cell.y + (verticalSign > 0 ? 1f : 0f));

        Vector3[] vertices = new Vector3[segments + 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        vertices[0] = WithSampledRoadMaskHeight(corner, lift);
        uvs[0] = Vector2.zero;

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 0.5f;
            Vector3 point = corner + new Vector3(
                -horizontalSign * Mathf.Cos(angle) * radius,
                0f,
                -verticalSign * Mathf.Sin(angle) * radius);
            vertices[i + 1] = WithSampledRoadMaskHeight(point, lift);
            uvs[i + 1] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
        }

        int[] triangles = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            int tri = i * 3;
            triangles[tri] = 0;
            triangles[tri + 1] = i + 1;
            triangles[tri + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = mask.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        mask.AddComponent<MeshRenderer>();
        ApplyStylizedRoadMaterial(mask, cell.x, cell.y, isHighway: false, isShoulder: false);
        ConfigureStaticVisual(mask, VisualSmoothnessAsphalt);
    }

    private void CreateUnifiedRoadCornerShoulderArc(Vector2Int cell, int horizontalSign, int verticalSign)
    {
        GameObject arc = new($"UnifiedRoad_CornerShoulder_{cell.x}_{cell.y}");
        arc.transform.SetParent(unifiedRoadVisualRoot, false);

        Mesh mesh = new();
        mesh.name = $"{arc.name}_Mesh";

        const int segments = 12;
        const float innerRadius = 0.60f;
        const float outerRadius = 0.68f;
        const float lift = UnifiedRoadShoulderArcLift;

        Vector3 corner = new(
            cell.x + (horizontalSign > 0 ? 1f : 0f),
            0f,
            cell.y + (verticalSign > 0 ? 1f : 0f));

        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[segments * 6];

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = t * Mathf.PI * 0.5f;
            Vector3 radial = new(
                -horizontalSign * Mathf.Cos(angle),
                0f,
                -verticalSign * Mathf.Sin(angle));
            vertices[i * 2] = WithSampledRoadMaskHeight(corner + radial * innerRadius, lift);
            vertices[i * 2 + 1] = WithSampledRoadMaskHeight(corner + radial * outerRadius, lift);
            uvs[i * 2] = new Vector2(t, 0f);
            uvs[i * 2 + 1] = new Vector2(t, 1f);
        }

        for (int i = 0; i < segments; i++)
        {
            int v = i * 2;
            int tri = i * 6;
            triangles[tri] = v;
            triangles[tri + 1] = v + 2;
            triangles[tri + 2] = v + 1;
            triangles[tri + 3] = v + 1;
            triangles[tri + 4] = v + 2;
            triangles[tri + 5] = v + 3;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        MeshFilter filter = arc.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        arc.AddComponent<MeshRenderer>();

        Renderer renderer = arc.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material material = new(ShaderRefs.Lit);
            Color color = new(0.88f, 0.83f, 0.68f);
            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.12f);
            }
            renderer.material = material;
        }

        ConfigureStaticVisual(arc);
    }

    private Vector3 WithSampledRoadMaskHeight(Vector3 point, float lift)
    {
        point.y = SampleRoadSurfaceHeight(point.x, point.z) + lift;
        return point;
    }

    private void RefreshRoadVisual(Vector2Int cell)
    {
        if (!roadVisuals.TryGetValue(cell, out GameObject road))
        {
            return;
        }

        bool isCorner = TryGetRoadCorner(cell, out int cornerHorizontalSign, out int cornerVerticalSign);
        bool horizontal = false;
        bool hasRoadAxis = !isCorner && TryGetRoadVisualAxis(cell, out horizontal);
        bool vertical = hasRoadAxis && !horizontal;

        UpdateRoadTileFallbackMesh(road, cell, horizontal, vertical, isCorner);

        for (int i = road.transform.childCount - 1; i >= 0; i--)
        {
            if (road.transform.GetChild(i).name == "RoadMarkings")
            {
                Destroy(road.transform.GetChild(i).gameObject);
                break;
            }
        }

        // Per-cell road fallback is a sloped mesh, not a cube. It keeps isolated/corner
        // cells visible while sharing the same smoothed height model as the unified ribbon.
    }

    private void UpdateRoadTileFallbackMesh(GameObject road, Vector2Int cell, bool horizontal, bool vertical, bool isCorner)
    {
        Transform surface = road.transform.Find("RoadTileSurface");
        GameObject surfaceObject;
        MeshFilter filter;
        if (surface == null)
        {
            surfaceObject = new GameObject("RoadTileSurface");
            surfaceObject.transform.SetParent(road.transform, false);
            surfaceObject.transform.localPosition = Vector3.zero;
            filter = surfaceObject.AddComponent<MeshFilter>();
            surfaceObject.AddComponent<MeshRenderer>();
            ConfigureStaticVisual(surfaceObject, VisualSmoothnessAsphalt);
        }
        else
        {
            surfaceObject = surface.gameObject;
            filter = surfaceObject.GetComponent<MeshFilter>();
            if (filter == null)
            {
                filter = surfaceObject.AddComponent<MeshFilter>();
            }
        }

        ApplyStylizedRoadMaterial(surfaceObject, cell.x, cell.y, isHighway: false, isShoulder: false);

        float halfX = (horizontal || isCorner) ? 0.56f : 0.41f;
        float halfZ = (vertical || isCorner) ? 0.56f : 0.41f;
        float centerX = cell.x + 0.5f;
        float centerZ = cell.y + 0.5f;
        float x0 = centerX - halfX;
        float x1 = centerX + halfX;
        float z0 = centerZ - halfZ;
        float z1 = centerZ + halfZ;

        Mesh mesh = filter.sharedMesh;
        if (mesh == null)
        {
            mesh = new Mesh();
            mesh.name = $"RoadTileSurface_{cell.x}_{cell.y}_Mesh";
            filter.sharedMesh = mesh;
        }

        mesh.Clear();
        mesh.vertices = new[]
        {
            new Vector3(x0, SampleRoadSurfaceHeight(x0, z0) + RoadTileSurfaceLift, z0),
            new Vector3(x1, SampleRoadSurfaceHeight(x1, z0) + RoadTileSurfaceLift, z0),
            new Vector3(x0, SampleRoadSurfaceHeight(x0, z1) + RoadTileSurfaceLift, z1),
            new Vector3(x1, SampleRoadSurfaceHeight(x1, z1) + RoadTileSurfaceLift, z1),
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

        Collider collider = surfaceObject.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }
    }

    private void SetRoadTileFallbackVisibility(bool visible)
    {
        foreach (KeyValuePair<Vector2Int, GameObject> pair in roadVisuals)
        {
            GameObject road = pair.Value;
            if (road == null)
            {
                continue;
            }

            Transform surface = road.transform.Find("RoadTileSurface");
            if (surface == null)
            {
                continue;
            }

            foreach (Renderer renderer in surface.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = visible && IsRoadVisualReady(pair.Key);
            }

            foreach (Collider collider in surface.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }
        }
    }

    private bool TryGetRoadCorner(Vector2Int cell, out int horizontalSign, out int verticalSign)
    {
        if (!IsRoadVisualReady(cell))
        {
            horizontalSign = 0;
            verticalSign = 0;
            return false;
        }

        bool east = ConnectsToRoadOrAnchor(cell, Vector2Int.right);
        bool west = ConnectsToRoadOrAnchor(cell, Vector2Int.left);
        bool north = ConnectsToRoadOrAnchor(cell, Vector2Int.up);
        bool south = ConnectsToRoadOrAnchor(cell, Vector2Int.down);

        bool hasOneHorizontal = east ^ west;
        bool hasOneVertical = north ^ south;
        bool isStraightHorizontal = east && west && !north && !south;
        bool isStraightVertical = north && south && !east && !west;

        horizontalSign = east ? 1 : -1;
        verticalSign = north ? 1 : -1;
        return hasOneHorizontal && hasOneVertical && !isStraightHorizontal && !isStraightVertical;
    }
}
