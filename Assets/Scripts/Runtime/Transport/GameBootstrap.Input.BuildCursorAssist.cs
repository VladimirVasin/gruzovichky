using UnityEngine;

public partial class GameBootstrap
{
    private const float BuildCursorAssistLightHeight = 3.2f;
    private const float BuildCursorAssistLightRangeMultiplier = 2.75f;
    private const float BuildCursorAssistLightMinRange = 10f;
    private const float BuildCursorAssistLightMaxRange = 24f;
    private const float BuildCursorAssistLightMinIntensity = 0.22f;
    private const float BuildCursorAssistLightMaxIntensity = 1.35f;

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

        GameObject lightObject = new("BuildCursorAssistLight");
        lightObject.transform.SetParent(buildCursorAssistRoot.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, BuildCursorAssistLightHeight, 0f);
        buildCursorAssistLight = lightObject.AddComponent<Light>();
        buildCursorAssistLight.type = LightType.Point;
        buildCursorAssistLight.color = new Color(1f, 0.78f, 0.46f);
        buildCursorAssistLight.range = BuildCursorAssistLightMinRange;
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

        void IncludeCell(Vector2Int cell)
        {
            if (!IsInsideGrid(cell))
            {
                return;
            }

            minX = Mathf.Min(minX, cell.x);
            maxX = Mathf.Max(maxX, cell.x + 1f);
            minZ = Mathf.Min(minZ, cell.y);
            maxZ = Mathf.Max(maxZ, cell.y + 1f);
            Vector3 center = GetCellCenter(cell);
            maxHeight = Mathf.Max(maxHeight, SampleTerrainHeight(center.x, center.z));
            hasCell = true;
        }

        for (int i = 0; i < buildPreviewFootprintCells.Count; i++)
        {
            IncludeCell(buildPreviewFootprintCells[i]);
        }

        if (IsBuildingBuildTool(activeBuildTool))
        {
            for (int i = 0; i < buildPreviewWalkBufferCells.Count; i++)
            {
                IncludeCell(buildPreviewWalkBufferCells[i]);
            }
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
        if (buildCursorAssistRoot == null || buildCursorAssistLight == null)
        {
            return;
        }

        float nightFactor = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.34f, 0.12f, currentStylizedDaylight));
        if (nightFactor <= 0.015f)
        {
            HideBuildCursorAssist();
            return;
        }

        buildCursorAssistRoot.SetActive(true);
        buildCursorAssistRoot.transform.position = position;

        buildCursorAssistLight.enabled = true;
        buildCursorAssistLight.color = canBuild ? new Color(1f, 0.78f, 0.46f) : new Color(1f, 0.36f, 0.24f);
        buildCursorAssistLight.range = Mathf.Clamp(
            radius * BuildCursorAssistLightRangeMultiplier,
            BuildCursorAssistLightMinRange,
            BuildCursorAssistLightMaxRange);
        buildCursorAssistLight.intensity = Mathf.Lerp(
            BuildCursorAssistLightMinIntensity,
            BuildCursorAssistLightMaxIntensity,
            nightFactor);
    }
}
