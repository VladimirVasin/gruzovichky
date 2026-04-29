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
        position.y = SampleTerrainHeight(position.x, position.z) + RoadHeight + 0.128f;
        GameObject dash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        dash.name = $"UnifiedRoadDash_{cell.x}_{cell.y}";
        dash.transform.SetParent(unifiedRoadVisualRoot, false);
        dash.transform.position = position;
        dash.transform.localScale = isHorizontal
            ? new Vector3(0.68f, 0.035f, 0.08f)
            : new Vector3(0.08f, 0.035f, 0.68f);
        ApplyColor(dash, new Color(0.92f, 0.92f, 0.9f));
        ConfigureStaticVisual(dash);
        if (dash.TryGetComponent(out Collider collider))
        {
            collider.enabled = false;
        }
    }
    private void CreateUnifiedRoadSideStripe(Vector2Int cell, bool isHorizontal, Vector2Int sideOffset)
    {
        Vector3 center = GetCellCenter(cell);
        Vector3 side = new Vector3(sideOffset.x, 0f, sideOffset.y).normalized;
        Vector3 position = center + side * 0.42f;
        position.y = SampleTerrainHeight(position.x, position.z) + RoadHeight + 0.129f;
        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.name = $"UnifiedRoadSideStripe_{cell.x}_{cell.y}";
        stripe.transform.SetParent(unifiedRoadVisualRoot, false);
        stripe.transform.position = position;
        stripe.transform.localScale = isHorizontal
            ? new Vector3(0.84f, 0.035f, 0.075f)
            : new Vector3(0.075f, 0.035f, 0.84f);
        ApplyColor(stripe, new Color(0.88f, 0.83f, 0.68f));
        ConfigureStaticVisual(stripe);
        if (stripe.TryGetComponent(out Collider collider))
        {
            collider.enabled = false;
        }
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
