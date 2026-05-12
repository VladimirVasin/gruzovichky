using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap
{
    private static readonly Color GridLineHiddenColor = new(0.34f, 0.38f, 0.28f, 0f);
    private static readonly Color GridLineBuildColor = new(1.00f, 0.86f, 0.42f, 0.24f);

    private System.Collections.IEnumerator SetupGridAsync()
    {
        yield return null; // one-frame defer so callers stay async-compatible

        GameObject gridRoot = new("GridLines");
        gridRoot.transform.SetParent(worldRoot, false);
        gridLinesRoot = gridRoot.transform;

        Material lineMaterial = new(ShaderRefs.Sprites)
        {
            color = GridLineHiddenColor
        };
        gridLinesMaterial = lineMaterial;
        isGridBuildModeVisualActive = false;

        BuildGridLineMesh(gridRoot.transform, lineMaterial);
        gridRoot.SetActive(false);
    }

    private void UpdateGridLineVisualState()
    {
        if (gridLinesMaterial == null && gridLinesRoot == null)
        {
            return;
        }

        bool buildModeActive = isBuildPanelOpen || IsBuildingBuildTool(activeBuildTool) || IsRoadBuildTool(activeBuildTool);
        SetRootActive(gridLinesRoot, buildModeActive && !isFarZoomVisualLodActive);

        if (gridLinesMaterial == null)
        {
            return;
        }

        if (isGridBuildModeVisualActive == buildModeActive)
        {
            return;
        }

        isGridBuildModeVisualActive = buildModeActive;
        gridLinesMaterial.color = buildModeActive ? GridLineBuildColor : GridLineHiddenColor;
    }

    private void BuildGridLineMesh(Transform parent, Material material)
    {
        const float halfW = 0.015f; // half of the 0.03 line width
        int vertCount = (GridWidth + 1) * GridHeight + GridWidth * (GridHeight + 1);
        Vector3[] verts = new Vector3[vertCount * 4];
        int[] tris = new int[vertCount * 6];
        int vi = 0, ti = 0;

        // vertical segments: run along Z at each X line
        for (int x = 0; x <= GridWidth; x++)
        {
            for (int y = 0; y < GridHeight; y++)
            {
                float h = GetVerticalEdgeHeight(x, y) + 0.025f;
                int b = vi;
                verts[vi++] = new Vector3(x - halfW, h, y);
                verts[vi++] = new Vector3(x + halfW, h, y);
                verts[vi++] = new Vector3(x - halfW, h, y + 1f);
                verts[vi++] = new Vector3(x + halfW, h, y + 1f);
                tris[ti++] = b; tris[ti++] = b + 2; tris[ti++] = b + 1;
                tris[ti++] = b + 1; tris[ti++] = b + 2; tris[ti++] = b + 3;
            }
        }

        // horizontal segments: run along X at each Y line
        for (int y = 0; y <= GridHeight; y++)
        {
            for (int x = 0; x < GridWidth; x++)
            {
                float h = GetHorizontalEdgeHeight(x, y) + 0.025f;
                int b = vi;
                verts[vi++] = new Vector3(x, h, y - halfW);
                verts[vi++] = new Vector3(x, h, y + halfW);
                verts[vi++] = new Vector3(x + 1f, h, y - halfW);
                verts[vi++] = new Vector3(x + 1f, h, y + halfW);
                tris[ti++] = b; tris[ti++] = b + 1; tris[ti++] = b + 2;
                tris[ti++] = b + 2; tris[ti++] = b + 1; tris[ti++] = b + 3;
            }
        }

        Mesh mesh = new() { name = "GridLinesMesh" };
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.UploadMeshData(true); // mark as non-readable for GPU memory savings

        GameObject obj = new("GridLinesMesh");
        obj.transform.SetParent(parent, false);
        obj.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer mr = obj.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }
}
