using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float RoadConstructionWaveBaseDuration = 0.36f;
    private const float RoadConstructionWaveSecondsPerStep = 0.088f;
    private const float RoadConstructionWaveMaxDuration = 2.35f;
    private const float RoadConstructionWaveFrontWidth = 1.15f;
    private const float RoadConstructionWaveVisibleThreshold = 0.14f;
    private const float RoadConstructionWaveRevealLead = 0.32f;
    private const float RoadConstructionWaveHeightLift = 0.17f;
    private const float RoadConstructionWaveMinThickness = 0.018f;
    private const float RoadConstructionWaveMaxThickness = 0.105f;

    private sealed class RoadConstructionWaveTile
    {
        public Vector2Int Cell;
        public int StepIndex;
        public bool IsHorizontal;
        public Transform RootTransform;
        public Material Material;
        public Vector3 BasePosition;
        public float BaseY;
        public float PhaseOffset;
    }

    private bool IsRoadVisualReady(Vector2Int cell)
    {
        return roadCells.Contains(cell) && !roadConstructionHiddenCells.Contains(cell);
    }

    private List<Vector2Int> CollectNewRoadCells(HashSet<Vector2Int> roadsBeforeBuild)
    {
        List<Vector2Int> newRoadCells = new();
        foreach (Vector2Int roadCell in roadCells)
        {
            if (roadsBeforeBuild == null || !roadsBeforeBuild.Contains(roadCell))
            {
                newRoadCells.Add(roadCell);
            }
        }

        return newRoadCells;
    }

    private void StartRoadConstructionWave(IReadOnlyList<Vector2Int> path, IReadOnlyList<Vector2Int> newRoadCells)
    {
        if (newRoadCells == null || newRoadCells.Count == 0)
        {
            return;
        }

        FinishActiveRoadConstructionWave(rebuild: false);
        List<RoadConstructionWaveTile> waveTiles = BuildRoadConstructionWaveTiles(path, newRoadCells);
        if (waveTiles.Count == 0)
        {
            return;
        }

        EnsureRoadConstructionWaveRoot();
        HideRoadBlueprintsForConstructionWave();
        for (int i = 0; i < waveTiles.Count; i++)
        {
            RoadConstructionWaveTile tile = waveTiles[i];
            roadConstructionHiddenCells.Add(tile.Cell);
            CreateRoadConstructionWaveVisual(tile);
        }

        int maxStep = waveTiles[^1].StepIndex;
        roadConstructionWaveCoroutine = StartCoroutine(AnimateRoadConstructionWave(waveTiles, maxStep));
    }

    private List<RoadConstructionWaveTile> BuildRoadConstructionWaveTiles(IReadOnlyList<Vector2Int> path, IReadOnlyList<Vector2Int> newRoadCells)
    {
        List<RoadConstructionWaveTile> waveTiles = new();
        HashSet<Vector2Int> seen = new();
        for (int i = 0; i < newRoadCells.Count; i++)
        {
            Vector2Int cell = newRoadCells[i];
            if (!roadCells.Contains(cell) || !seen.Add(cell))
            {
                continue;
            }

            int nearestPathIndex = FindNearestRoadConstructionPathIndex(path, cell);
            waveTiles.Add(new RoadConstructionWaveTile
            {
                Cell = cell,
                StepIndex = GetRoadConstructionStepIndex(path, nearestPathIndex),
                IsHorizontal = IsRoadConstructionStepHorizontal(path, nearestPathIndex),
                PhaseOffset = GetRoadConstructionPhaseOffset(cell)
            });
        }

        waveTiles.Sort((a, b) =>
        {
            int stepCompare = a.StepIndex.CompareTo(b.StepIndex);
            if (stepCompare != 0)
            {
                return stepCompare;
            }

            int yCompare = a.Cell.y.CompareTo(b.Cell.y);
            return yCompare != 0 ? yCompare : a.Cell.x.CompareTo(b.Cell.x);
        });
        return waveTiles;
    }

    private static int FindNearestRoadConstructionPathIndex(IReadOnlyList<Vector2Int> path, Vector2Int cell)
    {
        if (path == null || path.Count == 0)
        {
            return 0;
        }

        int nearestIndex = path.Count - 1;
        int bestDistance = int.MaxValue;
        for (int i = 0; i < path.Count; i++)
        {
            int distance = Mathf.Abs(cell.x - path[i].x) + Mathf.Abs(cell.y - path[i].y);
            if (distance < bestDistance || (distance == bestDistance && i > nearestIndex))
            {
                bestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private static int GetRoadConstructionStepIndex(IReadOnlyList<Vector2Int> path, int nearestPathIndex)
    {
        int maxIndex = path == null || path.Count == 0 ? 0 : path.Count - 1;
        return Mathf.Max(0, maxIndex - Mathf.Clamp(nearestPathIndex, 0, maxIndex));
    }

    private static bool IsRoadConstructionStepHorizontal(IReadOnlyList<Vector2Int> path, int nearestPathIndex)
    {
        if (path == null || path.Count < 2)
        {
            return true;
        }

        int index = Mathf.Clamp(nearestPathIndex, 0, path.Count - 1);
        Vector2Int direction = index > 0
            ? path[index] - path[index - 1]
            : path[index + 1] - path[index];
        return Mathf.Abs(direction.x) >= Mathf.Abs(direction.y);
    }

    private static float GetRoadConstructionPhaseOffset(Vector2Int cell)
    {
        int hash = Mathf.Abs(cell.x * 92821 + cell.y * 68917 + 53);
        return (hash % 1000) / 1000f;
    }

    private void EnsureRoadConstructionWaveRoot()
    {
        if (roadConstructionWaveRoot != null)
        {
            return;
        }

        roadConstructionWaveRoot = new GameObject("RoadConstructionWave").transform;
        roadConstructionWaveRoot.SetParent(roadsRoot != null ? roadsRoot : worldRoot, false);
    }

    private void HideRoadBlueprintsForConstructionWave()
    {
        ClearBuildRoadPreviewCells();
        HideBuildHoverHighlights();
        if (roadPathStartHighlight != null)
        {
            roadPathStartHighlight.SetActive(false);
        }

        if (roadPathStartSideHighlight != null)
        {
            roadPathStartSideHighlight.SetActive(false);
        }
    }

    private void CreateRoadConstructionWaveVisual(RoadConstructionWaveTile tile)
    {
        GameObject wave = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wave.name = $"RoadConstructionWave_{tile.Cell.x}_{tile.Cell.y}";
        wave.transform.SetParent(roadConstructionWaveRoot, false);

        Vector3 center = GetCellCenter(tile.Cell);
        tile.BaseY = SampleRoadSurfaceHeight(center.x, center.z) + RoadTileSurfaceLift + 0.04f;
        tile.BasePosition = new Vector3(center.x, tile.BaseY, center.z);
        wave.transform.position = tile.BasePosition;
        wave.transform.localScale = tile.IsHorizontal
            ? new Vector3(0.98f, RoadConstructionWaveMinThickness, 0.56f)
            : new Vector3(0.56f, RoadConstructionWaveMinThickness, 0.98f);

        Renderer renderer = wave.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateRoadConstructionWaveMaterial(tile.Cell);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            tile.Material = renderer.sharedMaterial;
        }

        if (wave.TryGetComponent(out Collider collider))
        {
            collider.enabled = false;
        }

        tile.RootTransform = wave.transform;
        wave.SetActive(false);
    }

    private IEnumerator AnimateRoadConstructionWave(List<RoadConstructionWaveTile> waveTiles, int maxStep)
    {
        float duration = Mathf.Clamp(
            RoadConstructionWaveBaseDuration + (maxStep + 1) * RoadConstructionWaveSecondsPerStep,
            0.46f,
            RoadConstructionWaveMaxDuration);
        float elapsed = 0f;
        int lastRevealStep = -1;

        while (elapsed < duration)
        {
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
            float front = Mathf.Lerp(-0.35f, maxStep + 0.95f, easedProgress);
            int revealStep = Mathf.FloorToInt(front + RoadConstructionWaveRevealLead);
            if (revealStep > lastRevealStep)
            {
                RevealRoadConstructionCellsThrough(waveTiles, revealStep);
                lastRevealStep = revealStep;
            }

            ApplyRoadConstructionWaveFrame(waveTiles, front, elapsed);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        RevealRoadConstructionCellsThrough(waveTiles, maxStep);
        ClearRoadConstructionWaveVisuals();
        roadConstructionWaveCoroutine = null;
        RebuildUnifiedRoadVisuals();
        FlushPendingRoadsideRefreshes();
        UpdateRoadAccessWarningMarkers();
    }

    private void RevealRoadConstructionCellsThrough(List<RoadConstructionWaveTile> waveTiles, int revealStep)
    {
        if (waveTiles == null || revealStep < 0)
        {
            return;
        }

        bool changed = false;
        for (int i = 0; i < waveTiles.Count; i++)
        {
            RoadConstructionWaveTile tile = waveTiles[i];
            if (tile.StepIndex > revealStep)
            {
                break;
            }

            if (roadConstructionHiddenCells.Remove(tile.Cell))
            {
                QueueRoadsideRefreshAround(tile.Cell);
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        RebuildUnifiedRoadVisuals();
        FlushPendingRoadsideRefreshes();
        UpdateRoadAccessWarningMarkers();
    }

    private static void ApplyRoadConstructionWaveFrame(List<RoadConstructionWaveTile> waveTiles, float front, float elapsed)
    {
        if (waveTiles == null)
        {
            return;
        }

        for (int i = 0; i < waveTiles.Count; i++)
        {
            RoadConstructionWaveTile tile = waveTiles[i];
            if (tile.RootTransform == null)
            {
                continue;
            }

            float distance = Mathf.Abs(tile.StepIndex - front);
            float waveT = Mathf.Clamp01(1f - distance / RoadConstructionWaveFrontWidth);
            waveT = Mathf.Pow(Mathf.Sin(waveT * Mathf.PI * 0.5f), 1.35f);
            float visibleT = Mathf.InverseLerp(RoadConstructionWaveVisibleThreshold, 1f, waveT);
            bool active = visibleT > 0f;
            tile.RootTransform.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            float shimmer = Mathf.Sin(elapsed * 9f + tile.PhaseOffset * 6.283f) * 0.014f * visibleT;
            Vector3 position = tile.BasePosition;
            position.y = tile.BaseY + visibleT * RoadConstructionWaveHeightLift;
            position.x += shimmer * (tile.IsHorizontal ? 1f : 0.35f);
            position.z += shimmer * (tile.IsHorizontal ? 0.35f : 1f);
            tile.RootTransform.position = position;

            float lengthScale = 0.72f + visibleT * 0.34f;
            float widthScale = 0.42f + visibleT * 0.58f;
            float thickness = Mathf.Lerp(
                RoadConstructionWaveMinThickness,
                RoadConstructionWaveMaxThickness,
                visibleT);
            tile.RootTransform.localScale = tile.IsHorizontal
                ? new Vector3(0.98f * lengthScale, thickness, 0.56f * widthScale)
                : new Vector3(0.56f * widthScale, thickness, 0.98f * lengthScale);

            if (tile.Material != null)
            {
                float alpha = 0.10f + visibleT * 0.46f;
                Color color = Color.Lerp(
                    new Color(0.10f, 0.11f, 0.12f, 0f),
                    new Color(0.26f, 0.27f, 0.29f, alpha),
                    0.35f + visibleT * 0.65f);
                color.a = alpha;
                ApplyRoadConstructionWaveMaterialColor(tile.Material, color);
            }
        }
    }

    private Material CreateRoadConstructionWaveMaterial(Vector2Int cell)
    {
        Material material = CreateTransparentOverlayMaterial(new Color(0.10f, 0.11f, 0.12f, 0f));
        Material sourceMaterial = highwaySurfaceMaterial != null ? highwaySurfaceMaterial : roadSurfaceMaterial;
        if (sourceMaterial != null && sourceMaterial.mainTexture != null)
        {
            material.mainTexture = sourceMaterial.mainTexture;
            material.mainTextureScale = new Vector2(0.7f, 1.15f);
            material.mainTextureOffset = new Vector2(
                Mathf.Repeat(cell.x * 0.17f, 1f),
                Mathf.Repeat(cell.y * 0.23f, 1f));
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", sourceMaterial.mainTexture);
                material.SetTextureScale("_BaseMap", new Vector2(0.7f, 1.15f));
                material.SetTextureOffset("_BaseMap", material.mainTextureOffset);
            }
        }

        return material;
    }

    private static void ApplyRoadConstructionWaveMaterialColor(Material material, Color color)
    {
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
    }

    private void FinishActiveRoadConstructionWave(bool rebuild)
    {
        if (roadConstructionWaveCoroutine != null)
        {
            StopCoroutine(roadConstructionWaveCoroutine);
            roadConstructionWaveCoroutine = null;
        }

        List<Vector2Int> releasedCells = new(roadConstructionHiddenCells);
        roadConstructionHiddenCells.Clear();
        for (int i = 0; i < releasedCells.Count; i++)
        {
            QueueRoadsideRefreshAround(releasedCells[i]);
        }

        ClearRoadConstructionWaveVisuals();
        if (!rebuild)
        {
            return;
        }

        RebuildUnifiedRoadVisuals();
        FlushPendingRoadsideRefreshes();
        UpdateRoadAccessWarningMarkers();
    }

    private void ClearRoadConstructionWaveVisuals()
    {
        if (roadConstructionWaveRoot == null)
        {
            return;
        }

        for (int i = roadConstructionWaveRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(roadConstructionWaveRoot.GetChild(i).gameObject);
        }
    }
}
