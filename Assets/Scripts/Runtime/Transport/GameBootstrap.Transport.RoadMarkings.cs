using UnityEngine;

public partial class GameBootstrap
{
    private void AddUnifiedRoadMarkings()
    {
        if (unifiedRoadVisualRoot == null)
        {
            return;
        }

        foreach (Vector2Int cell in roadCells)
        {
            if (!TryGetRoadVisualAxis(cell, out bool isHorizontal))
            {
                continue;
            }

            if (ShouldDrawTwoCellRoadCenterDash(cell, isHorizontal))
            {
                CreateUnifiedRoadDash(cell, isHorizontal);
            }
        }
    }
    private void CreateUnifiedRoadDash(Vector2Int cell, bool isHorizontal)
    {
        Vector3 center = GetCellCenter(cell);
        Vector3 position = center + (isHorizontal ? new Vector3(0f, 0f, 0.5f) : new Vector3(0.5f, 0f, 0f));
        CreateRoadMarkingQuad(
            $"UnifiedRoadDash_{cell.x}_{cell.y}",
            position,
            isHorizontal ? 0.68f : 0.08f,
            isHorizontal ? 0.08f : 0.68f,
            new Color(0.92f, 0.92f, 0.9f));
    }
    private void CreateUnifiedRoadSideStripe(Vector2Int cell, bool isHorizontal, Vector2Int sideOffset)
    {
        Vector3 center = GetCellCenter(cell);
        Vector3 side = new Vector3(sideOffset.x, 0f, sideOffset.y).normalized;
        Vector3 position = center + side * 0.42f;
        CreateRoadMarkingQuad(
            $"UnifiedRoadSideStripe_{cell.x}_{cell.y}",
            position,
            isHorizontal ? 0.84f : 0.075f,
            isHorizontal ? 0.075f : 0.84f,
            new Color(0.88f, 0.83f, 0.68f));
    }
    private void CreateRoadMarkingQuad(string name, Vector3 center, float sizeX, float sizeZ, Color color)
    {
        if (unifiedRoadVisualRoot == null)
        {
            return;
        }

        GameObject marking = new(name);
        marking.transform.SetParent(unifiedRoadVisualRoot, false);

        float halfX = sizeX * 0.5f;
        float halfZ = sizeZ * 0.5f;
        float x0 = center.x - halfX;
        float x1 = center.x + halfX;
        float z0 = center.z - halfZ;
        float z1 = center.z + halfZ;
        const float lift = RoadTileSurfaceLift + 0.018f;

        Mesh mesh = new();
        mesh.name = $"{name}_Mesh";
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

        MeshFilter filter = marking.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        marking.AddComponent<MeshRenderer>();
        ApplyColor(marking, color, VisualSmoothnessAsphalt);
        ConfigureStaticVisual(marking, VisualSmoothnessAsphalt);
    }
    private bool TryGetRoadVisualAxis(Vector2Int cell, out bool isHorizontal)
    {
        return RoadMarkingPlanner.TryGetRoadVisualAxis(cell, ConnectsToRoadOrAnchor, out isHorizontal);
    }
    private void AddRoadMarkings(Vector2Int cell, Transform roadTransform, Vector3 roadScale, bool isHorizontal)
    {
        GameObject container = new("RoadMarkings");
        container.transform.SetParent(roadTransform, false);

        Color shoulderColor = new Color(0.88f, 0.83f, 0.68f);
        Color centerColor   = new Color(0.92f, 0.92f, 0.9f);
        const float posY        = 0.42f;
        const float scaleY      = 0.04f;
        const float shoulderOff = 0.23f;
        const float stripeLen   = 0.84f;
        const float stripeWidth = 0.08f;
        const float centerDashLen = 0.68f;
        const float centerDashWidth = 0.08f;

        if (ShouldDrawTwoCellRoadCenterDash(cell, isHorizontal))
        {
            GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dash.transform.SetParent(container.transform, false);
            dash.transform.localPosition = isHorizontal
                ? new Vector3(0f, posY, 0.5f)
                : new Vector3(0.5f, posY, 0f);
            dash.transform.localScale = isHorizontal
                ? new Vector3(centerDashLen, scaleY, centerDashWidth)
                : new Vector3(centerDashWidth, scaleY, centerDashLen);
            ApplyColor(dash, centerColor);
            ConfigureStaticVisual(dash);
            if (dash.TryGetComponent(out Collider dc)) dc.enabled = false;
        }

        // White shoulder stripes — road edges
        for (int s = -1; s <= 1; s += 2)
        {
            GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.transform.SetParent(container.transform, false);
            stripe.transform.localPosition = isHorizontal
                ? new Vector3(0f, posY, s * shoulderOff)
                : new Vector3(s * shoulderOff, posY, 0f);
            stripe.transform.localScale = isHorizontal
                ? new Vector3(stripeLen, scaleY, stripeWidth)
                : new Vector3(stripeWidth, scaleY, stripeLen);
            ApplyColor(stripe, shoulderColor);
            ConfigureStaticVisual(stripe);
            if (stripe.TryGetComponent(out Collider sc)) sc.enabled = false;
        }
    }
    private bool ShouldDrawTwoCellRoadCenterDash(Vector2Int cell, bool isHorizontal)
    {
        return RoadMarkingPlanner.ShouldDrawTwoCellCenterDash(cell, isHorizontal, roadCells, ConnectsToRoadOrAnchor);
    }
    private void AddRoadCornerMarkings(Transform roadTransform, int horizontalSign, int verticalSign)
    {
        GameObject container = new("RoadMarkings");
        container.transform.SetParent(roadTransform, false);

        Color centerColor = new Color(0.92f, 0.92f, 0.9f);
        const float posY = 0.42f;
        const float scaleY = 0.04f;
        const float radius = 0.43f;
        const float segmentLength = 0.18f;
        const float segmentWidth = 0.065f;
        const int segmentCount = 5;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = (i + 0.5f) / segmentCount;
            float angle = t * Mathf.PI * 0.5f;
            float x = horizontalSign * (0.5f - Mathf.Sin(angle) * radius);
            float z = verticalSign * (0.5f - Mathf.Cos(angle) * radius);

            Vector2 tangent = new(
                -horizontalSign * Mathf.Cos(angle),
                verticalSign * Mathf.Sin(angle));
            float yaw = Mathf.Atan2(tangent.x, tangent.y) * Mathf.Rad2Deg;

            GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dash.name = "RoadCornerCenterDash";
            dash.transform.SetParent(container.transform, false);
            dash.transform.localPosition = new Vector3(x, posY, z);
            dash.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            dash.transform.localScale = new Vector3(segmentLength, scaleY, segmentWidth);
            ApplyColor(dash, centerColor);
            ConfigureStaticVisual(dash);
            if (dash.TryGetComponent(out Collider dc)) dc.enabled = false;
        }
    }
}
