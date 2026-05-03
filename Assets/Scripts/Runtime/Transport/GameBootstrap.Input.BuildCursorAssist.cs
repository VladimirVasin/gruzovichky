using UnityEngine;

public partial class GameBootstrap
{
    private void DecorateRoadPreviewTile(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        GameObject centerDash = GameObject.CreatePrimitive(PrimitiveType.Cube);
        centerDash.name = "RoadPreviewCenterDash";
        centerDash.transform.SetParent(root.transform, false);
        centerDash.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        centerDash.transform.localScale = new Vector3(0.14f, 0.22f, 0.82f);
        centerDash.GetComponent<Collider>().enabled = false;
        ApplyColor(centerDash, new Color(0.95f, 0.82f, 0.32f));
        ConfigureStaticVisual(centerDash);

        GameObject leftEdge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftEdge.name = "RoadPreviewLeftEdge";
        leftEdge.transform.SetParent(root.transform, false);
        leftEdge.transform.localPosition = new Vector3(-0.34f, 0.7f, 0f);
        leftEdge.transform.localScale = new Vector3(0.06f, 0.22f, 0.84f);
        leftEdge.GetComponent<Collider>().enabled = false;
        ApplyColor(leftEdge, new Color(0.95f, 0.93f, 0.82f));
        ConfigureStaticVisual(leftEdge);

        GameObject rightEdge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightEdge.name = "RoadPreviewRightEdge";
        rightEdge.transform.SetParent(root.transform, false);
        rightEdge.transform.localPosition = new Vector3(0.34f, 0.7f, 0f);
        rightEdge.transform.localScale = new Vector3(0.06f, 0.22f, 0.84f);
        rightEdge.GetComponent<Collider>().enabled = false;
        ApplyColor(rightEdge, new Color(0.95f, 0.93f, 0.82f));
        ConfigureStaticVisual(rightEdge);
    }

    private void SetupBuildCursorAssist()
    {
        buildCursorAssistRoot = new GameObject("BuildCursorAssist");
        buildCursorAssistRoot.transform.SetParent(worldRoot, false);

        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glow.name = "BuildCursorAssistGlow";
        glow.transform.SetParent(buildCursorAssistRoot.transform, false);
        glow.transform.localScale = new Vector3(1f, 0.012f, 1f);
        glow.GetComponent<Collider>().enabled = false;
        buildCursorAssistGlowRenderer = glow.GetComponent<Renderer>();
        buildCursorAssistGlowMaterial = CreateTransparentOverlayMaterial(new Color(1f, 0.82f, 0.42f, 0f));
        buildCursorAssistGlowRenderer.material = buildCursorAssistGlowMaterial;

        GameObject lightObject = new("BuildCursorAssistLight");
        lightObject.transform.SetParent(buildCursorAssistRoot.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 2.4f, 0f);
        buildCursorAssistLight = lightObject.AddComponent<Light>();
        buildCursorAssistLight.type = LightType.Point;
        buildCursorAssistLight.color = new Color(1f, 0.78f, 0.46f);
        buildCursorAssistLight.range = 7.5f;
        buildCursorAssistLight.intensity = 0f;
        buildCursorAssistLight.shadows = LightShadows.None;
        buildCursorAssistLight.enabled = false;
        buildCursorAssistRoot.SetActive(false);
    }

    private void HideBuildCursorAssist()
    {
        if (buildCursorAssistRoot != null)
        {
            buildCursorAssistRoot.SetActive(false);
        }

        if (buildCursorAssistLight != null)
        {
            buildCursorAssistLight.enabled = false;
            buildCursorAssistLight.intensity = 0f;
        }
    }

    private void UpdateBuildCursorAssistFromPreview(bool canBuild)
    {
        if (buildPreviewFootprintCells.Count == 0)
        {
            HideBuildCursorAssist();
            return;
        }

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;
        float maxHeight = 0f;
        bool hasCell = false;

        for (int i = 0; i < buildPreviewFootprintCells.Count; i++)
        {
            Vector2Int cell = buildPreviewFootprintCells[i];
            if (!IsInsideGrid(cell))
            {
                continue;
            }

            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x + 1f);
            minZ = Mathf.Min(minZ, cell.y);
            maxZ = Mathf.Max(maxZ, cell.y + 1f);
            Vector3 center = GetCellCenter(cell);
            maxHeight = Mathf.Max(maxHeight, SampleTerrainHeight(center.x, center.z));
            hasCell = true;
        }

        if (!hasCell)
        {
            HideBuildCursorAssist();
            return;
        }

        Vector3 position = new((minX + maxX) * 0.5f, maxHeight + RoadHeight + 0.025f, (minZ + maxZ) * 0.5f);
        float radius = Mathf.Max(maxX - minX, maxZ - minZ) + 1.5f;
        UpdateBuildCursorAssist(position, radius, canBuild);
    }

    private void UpdateBuildCursorAssist(Vector3 position, float radius, bool canBuild)
    {
        if (buildCursorAssistRoot == null || buildCursorAssistGlowMaterial == null)
        {
            return;
        }

        float nightFactor = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.72f, 0.24f, currentStylizedDaylight));
        if (nightFactor <= 0.015f)
        {
            HideBuildCursorAssist();
            return;
        }

        Color tint = canBuild
            ? new Color(1f, 0.78f, 0.38f, Mathf.Lerp(0.06f, 0.26f, nightFactor))
            : new Color(1f, 0.28f, 0.18f, Mathf.Lerp(0.08f, 0.30f, nightFactor));

        buildCursorAssistRoot.SetActive(true);
        buildCursorAssistRoot.transform.position = position;
        buildCursorAssistRoot.transform.localScale = new Vector3(radius, 1f, radius);
        buildCursorAssistGlowMaterial.color = tint;
        if (buildCursorAssistGlowMaterial.HasProperty("_BaseColor"))
        {
            buildCursorAssistGlowMaterial.SetColor("_BaseColor", tint);
        }

        if (buildCursorAssistLight != null)
        {
            buildCursorAssistLight.enabled = true;
            buildCursorAssistLight.color = canBuild ? new Color(1f, 0.78f, 0.46f) : new Color(1f, 0.36f, 0.24f);
            buildCursorAssistLight.range = Mathf.Clamp(radius * 1.45f, 4.5f, 12f);
            buildCursorAssistLight.intensity = Mathf.Lerp(0.25f, 1.45f, nightFactor);
        }

        if (buildCursorAssistGlowRenderer != null)
        {
            buildCursorAssistGlowRenderer.enabled = true;
        }
    }
}
