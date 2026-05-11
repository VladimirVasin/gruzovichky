using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float StreetLitterScanInterval = 1.35f;
    private const float StreetLitterMaxScanTimeScale = 2f;
    private const float StreetLitterCrowdThreshold = 1.15f;
    private const float StreetLitterBaseGainPerTouchedCell = 0.18f;
    private const float StreetLitterGainPerPressureTick = 0.72f;
    private const float StreetLitterDecayPerQuietTick = 0.02f;
    private const float StreetLitterVisibleScore = 2.6f;
    private const float StreetLitterStageTwoScore = 8f;
    private const float StreetLitterStageThreeScore = 16f;
    private const float StreetLitterMaxScore = 28f;
    private const int StreetLitterMaxTrackedCells = 360;
    private const int StreetLitterMaxVisibleCells = 220;
    private const int CleanerTargetMaxPathChecks = 24;

    private readonly Dictionary<Vector2Int, float> streetLitterByCell = new();
    private readonly Dictionary<Vector2Int, float> streetLitterPressureByCell = new();
    private readonly Dictionary<Vector2Int, GameObject> streetLitterVisualsByCell = new();
    private readonly Dictionary<Vector2Int, int> streetLitterReservationsByCell = new();
    private readonly HashSet<Vector2Int> streetLitterTouchedCells = new();
    private readonly List<Vector2Int> streetLitterScratchCells = new();
    private readonly List<Vector2Int> streetLitterCleanerCandidateCells = new();
    private readonly List<float> streetLitterCleanerCandidateScores = new();
    private Transform streetLitterRoot;
    private float streetLitterScanTimer;

    private void ResetStreetLitterSystem()
    {
        streetLitterByCell.Clear();
        streetLitterPressureByCell.Clear();
        streetLitterReservationsByCell.Clear();
        streetLitterTouchedCells.Clear();
        streetLitterScratchCells.Clear();
        streetLitterCleanerCandidateCells.Clear();
        streetLitterCleanerCandidateScores.Clear();
        streetLitterScanTimer = 0f;

        if (streetLitterRoot != null)
        {
            Destroy(streetLitterRoot.gameObject);
            streetLitterRoot = null;
        }

        streetLitterVisualsByCell.Clear();
    }

    private void UpdateStreetLitterRuntime()
    {
        if (driverAgents.Count == 0)
        {
            return;
        }

        float scanTimeScale = Mathf.Min(Mathf.Max(0f, gameSpeedMultiplier), StreetLitterMaxScanTimeScale);
        streetLitterScanTimer -= Time.deltaTime * scanTimeScale;
        if (streetLitterScanTimer > 0f)
        {
            return;
        }

        streetLitterScanTimer = StreetLitterScanInterval;
        Dictionary<Vector2Int, float> pressureByCell = CollectStreetLitterCrowdPressure();
        ApplyStreetLitterPressure(pressureByCell);
        PruneStreetLitterVisuals();
    }

    private Dictionary<Vector2Int, float> CollectStreetLitterCrowdPressure()
    {
        streetLitterPressureByCell.Clear();
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (!CanDriverContributeStreetLitter(driver))
            {
                continue;
            }

            Vector3 position = driver.DriverObject.transform.position;
            Vector2Int cell = WorldToCell(position);
            float weight = GetStreetLitterDriverPressureWeight(driver);
            AddStreetLitterPressure(streetLitterPressureByCell, cell, weight);

            float spillWeight = weight * 0.46f;
            AddStreetLitterPressure(streetLitterPressureByCell, cell + Vector2Int.left, spillWeight);
            AddStreetLitterPressure(streetLitterPressureByCell, cell + Vector2Int.right, spillWeight);
            AddStreetLitterPressure(streetLitterPressureByCell, cell + Vector2Int.up, spillWeight);
            AddStreetLitterPressure(streetLitterPressureByCell, cell + Vector2Int.down, spillWeight);
        }

        return streetLitterPressureByCell;
    }

    private bool CanDriverContributeStreetLitter(DriverAgent driver)
    {
        return driver != null &&
               driver.DriverObject != null &&
               driver.DriverObject.activeSelf &&
               !driver.IsInsideBuilding &&
               !driver.IsDrivingPersonalCar &&
               driver.AssignedBuildingType != LocationType.CleaningDepot &&
               driver.WalkPhase != DriverRescuePhase.RidingLocalBus;
    }

    private static float GetStreetLitterDriverPressureWeight(DriverAgent driver)
    {
        if (driver.WalkPhase == DriverRescuePhase.WaitingAtLocalBusStop)
        {
            return 1.35f;
        }

        if (driver.WalkPhase == DriverRescuePhase.IdleAtTrashCan)
        {
            return 1.45f;
        }

        if (driver.IdleConversationTimer > 0f)
        {
            return 1.25f;
        }

        return driver.WalkPhase switch
        {
            DriverRescuePhase.None => 0.55f,
            DriverRescuePhase.IdleSittingOnBench => 1.05f,
            DriverRescuePhase.IdleAtBar => 1.1f,
            DriverRescuePhase.IdleAtCanteen => 1.1f,
            DriverRescuePhase.IdleAtKiosk => 1.15f,
            DriverRescuePhase.IdleAtGamblingHall => 1.1f,
            DriverRescuePhase.IdleAtCityPark => 0.9f,
            DriverRescuePhase.AtLaborExchange => 0.95f,
            DriverRescuePhase.IdleSmoking => 1.2f,
            DriverRescuePhase.IdlePhoneCall => 0.9f,
            _ => 0.62f
        };
    }

    private void AddStreetLitterPressure(Dictionary<Vector2Int, float> pressureByCell, Vector2Int cell, float pressure)
    {
        if (pressure <= 0f || !CanStreetLitterOccupyCell(cell))
        {
            return;
        }

        pressureByCell.TryGetValue(cell, out float current);
        pressureByCell[cell] = current + pressure;
    }

    private void ApplyStreetLitterPressure(Dictionary<Vector2Int, float> pressureByCell)
    {
        streetLitterTouchedCells.Clear();
        foreach (KeyValuePair<Vector2Int, float> pair in pressureByCell)
        {
            Vector2Int cell = pair.Key;
            float pressure = pair.Value;
            if (pressure < StreetLitterCrowdThreshold)
            {
                continue;
            }

            float gain = (pressure - StreetLitterCrowdThreshold + StreetLitterBaseGainPerTouchedCell) * StreetLitterGainPerPressureTick;
            if (IsVisibleFootpathCell(cell))
            {
                gain *= 1.25f;
            }

            if (IsStreetLitterNearTrashCan(cell))
            {
                gain *= 1.35f;
            }

            streetLitterByCell.TryGetValue(cell, out float previousScore);
            float nextScore = Mathf.Min(StreetLitterMaxScore, previousScore + gain);
            if (!Mathf.Approximately(previousScore, nextScore))
            {
                streetLitterByCell[cell] = nextScore;
                RefreshStreetLitterCell(cell, previousScore, nextScore);
            }

            streetLitterTouchedCells.Add(cell);
        }

        DecayQuietStreetLitterCells(streetLitterTouchedCells);
        PruneStreetLitterTrackedCells();
    }

    private void DecayQuietStreetLitterCells(HashSet<Vector2Int> touchedCells)
    {
        if (streetLitterByCell.Count == 0)
        {
            return;
        }

        streetLitterScratchCells.Clear();
        streetLitterScratchCells.AddRange(streetLitterByCell.Keys);
        for (int i = 0; i < streetLitterScratchCells.Count; i++)
        {
            Vector2Int cell = streetLitterScratchCells[i];
            if (touchedCells.Contains(cell))
            {
                continue;
            }

            if (!CanStreetLitterOccupyCell(cell))
            {
                ClearStreetLitterAtCell(cell);
                continue;
            }

            float previousScore = streetLitterByCell[cell];
            float nextScore = Mathf.Max(0f, previousScore - StreetLitterDecayPerQuietTick);
            if (nextScore < StreetLitterVisibleScore * 0.58f)
            {
                ClearStreetLitterAtCell(cell);
                continue;
            }

            if (!Mathf.Approximately(previousScore, nextScore))
            {
                streetLitterByCell[cell] = nextScore;
                RefreshStreetLitterCell(cell, previousScore, nextScore);
            }
        }
    }

    private bool CanStreetLitterOccupyCell(Vector2Int cell)
    {
        if (!IsInsideGrid(cell) ||
            IsWaterOrBeachCell(cell) ||
            edgeHighwayCells.Contains(cell) ||
            IsBuildingWalkBufferCell(cell))
        {
            return false;
        }

        LocationType? containingLocation = GetContainingLocation(cell);
        bool canUseLocationCell =
            containingLocation == LocationType.CityPark ||
            containingLocation == LocationType.Stop ||
            containingLocation == LocationType.IntercityStop;
        if (IsLocationCell(cell) && !canUseLocationCell)
        {
            return false;
        }

        if (roadCells.Contains(cell))
        {
            return IsRoadVisualReady(cell);
        }

        if (miscOccupiedCells.Contains(cell))
        {
            return false;
        }

        return IsVisibleFootpathCell(cell) ||
               IsAnyLocationEntranceCell(cell) ||
               IsStreetLitterNearPublicAnchor(cell) ||
               IsStreetLitterNearTrashCan(cell);
    }

    private bool IsStreetLitterNearPublicAnchor(Vector2Int cell)
    {
        foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(cell))
        {
            if ((roadCells.Contains(neighbor) && IsRoadVisualReady(neighbor)) ||
                IsAnyLocationEntranceCell(neighbor))
            {
                return true;
            }
        }

        return false;
    }

    private bool IsStreetLitterNearTrashCan(Vector2Int cell)
    {
        if (locationTrashCanMealTargets.Count == 0)
        {
            return false;
        }

        Vector3 center = GetCellCenter(cell);
        for (int i = 0; i < locationTrashCanMealTargets.Count; i++)
        {
            Vector3 target = locationTrashCanMealTargets[i];
            Vector2 flatDelta = new(center.x - target.x, center.z - target.z);
            if (flatDelta.sqrMagnitude <= 3.2f)
            {
                return true;
            }
        }

        return false;
    }

    private void RefreshStreetLitterCell(Vector2Int cell, float previousScore, float nextScore)
    {
        int previousStage = GetStreetLitterStage(previousScore);
        int nextStage = GetStreetLitterStage(nextScore);
        if (previousStage == nextStage && streetLitterVisualsByCell.ContainsKey(cell))
        {
            return;
        }

        DestroyStreetLitterVisual(cell);
        if (nextStage <= 0)
        {
            return;
        }

        EnsureStreetLitterRoot();
        GameObject root = new($"StreetLitter_{cell.x}_{cell.y}");
        root.transform.SetParent(streetLitterRoot, false);
        root.transform.position = Vector3.zero;
        streetLitterVisualsByCell[cell] = root;

        int pieceCount = nextStage switch
        {
            1 => 2,
            2 => 4,
            _ => 7
        };

        for (int i = 0; i < pieceCount; i++)
        {
            CreateStreetLitterPiece(root.transform, cell, i, nextStage);
        }
    }

    private void EnsureStreetLitterRoot()
    {
        if (streetLitterRoot != null)
        {
            return;
        }

        streetLitterRoot = new GameObject("StreetLitter").transform;
        streetLitterRoot.SetParent(worldRoot != null ? worldRoot : transform, false);
    }

    private void CreateStreetLitterPiece(Transform parent, Vector2Int cell, int index, int stage)
    {
        int seed = cell.x * 73856093 ^ cell.y * 19349663 ^ index * 83492791;
        float offsetX = Mathf.Lerp(-0.34f, 0.34f, StreetLitterHash01(seed));
        float offsetZ = Mathf.Lerp(-0.34f, 0.34f, StreetLitterHash01(seed + 17));
        float x = cell.x + 0.5f + offsetX;
        float z = cell.y + 0.5f + offsetZ;
        float surfaceY = GetStreetLitterSurfaceY(cell, x, z);
        int kind = Mathf.FloorToInt(StreetLitterHash01(seed + 29) * (stage >= 3 ? 5 : 4));

        switch (kind)
        {
            case 0:
                CreateStreetLitterPaper(parent, seed, new Vector3(x, surfaceY, z), large: stage >= 2);
                break;
            case 1:
                CreateStreetLitterWrapper(parent, seed, new Vector3(x, surfaceY, z));
                break;
            case 2:
                CreateStreetLitterCan(parent, seed, new Vector3(x, surfaceY, z));
                break;
            case 3:
                CreateStreetLitterStain(parent, seed, new Vector3(x, surfaceY, z), stage);
                break;
            default:
                CreateStreetLitterCarton(parent, seed, new Vector3(x, surfaceY, z));
                break;
        }
    }

    private float GetStreetLitterSurfaceY(Vector2Int cell, float x, float z)
    {
        if (roadCells.Contains(cell))
        {
            return SampleRoadSurfaceHeight(x, z) + RoadTileSurfaceLift + 0.014f;
        }

        return SampleTerrainHeight(x, z) + 0.018f;
    }

    private void CreateStreetLitterPaper(Transform parent, int seed, Vector3 position, bool large)
    {
        GameObject paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        paper.name = "StreetLitterPaper";
        paper.transform.SetParent(parent, false);
        paper.transform.position = position + Vector3.up * 0.003f;
        paper.transform.rotation = Quaternion.Euler(0f, StreetLitterHash01(seed + 3) * 360f, 0f);
        float width = large ? Mathf.Lerp(0.13f, 0.21f, StreetLitterHash01(seed + 5)) : Mathf.Lerp(0.08f, 0.15f, StreetLitterHash01(seed + 5));
        float depth = large ? Mathf.Lerp(0.06f, 0.12f, StreetLitterHash01(seed + 7)) : Mathf.Lerp(0.04f, 0.08f, StreetLitterHash01(seed + 7));
        paper.transform.localScale = new Vector3(width, 0.006f, depth);
        ApplyColor(paper, new Color(0.78f, 0.74f, 0.62f), VisualSmoothnessDefault);
        ConfigureStreetLitterPiece(paper);
    }

    private void CreateStreetLitterWrapper(Transform parent, int seed, Vector3 position)
    {
        GameObject wrapper = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wrapper.name = "StreetLitterWrapper";
        wrapper.transform.SetParent(parent, false);
        wrapper.transform.position = position + Vector3.up * 0.006f;
        wrapper.transform.rotation = Quaternion.Euler(0f, StreetLitterHash01(seed + 11) * 360f, 0f);
        wrapper.transform.localScale = new Vector3(
            Mathf.Lerp(0.07f, 0.13f, StreetLitterHash01(seed + 13)),
            0.010f,
            Mathf.Lerp(0.035f, 0.06f, StreetLitterHash01(seed + 19)));
        Color color = Color.Lerp(new Color(0.74f, 0.62f, 0.34f), new Color(0.58f, 0.20f, 0.16f), StreetLitterHash01(seed + 23));
        ApplyColor(wrapper, color, VisualSmoothnessFabric);
        ConfigureStreetLitterPiece(wrapper);
    }

    private void CreateStreetLitterCan(Transform parent, int seed, Vector3 position)
    {
        GameObject can = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        can.name = "StreetLitterCan";
        can.transform.SetParent(parent, false);
        can.transform.position = position + Vector3.up * 0.025f;
        can.transform.rotation = Quaternion.Euler(0f, StreetLitterHash01(seed + 31) * 360f, 90f);
        can.transform.localScale = new Vector3(0.032f, 0.055f, 0.032f);
        ApplyColor(can, Color.Lerp(new Color(0.38f, 0.42f, 0.46f), new Color(0.34f, 0.56f, 0.48f), StreetLitterHash01(seed + 37)), VisualSmoothnessVehicleMetal);
        ConfigureStreetLitterPiece(can);
    }

    private void CreateStreetLitterStain(Transform parent, int seed, Vector3 position, int stage)
    {
        GameObject stain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stain.name = "StreetLitterStain";
        stain.transform.SetParent(parent, false);
        stain.transform.position = position + Vector3.up * 0.002f;
        stain.transform.rotation = Quaternion.Euler(0f, StreetLitterHash01(seed + 41) * 360f, 0f);
        float radius = stage >= 3 ? Mathf.Lerp(0.08f, 0.14f, StreetLitterHash01(seed + 43)) : Mathf.Lerp(0.045f, 0.08f, StreetLitterHash01(seed + 43));
        stain.transform.localScale = new Vector3(radius, 0.003f, radius * Mathf.Lerp(0.55f, 0.9f, StreetLitterHash01(seed + 47)));
        ApplyColor(stain, new Color(0.12f, 0.11f, 0.09f), VisualSmoothnessDefault);
        ConfigureStreetLitterPiece(stain);
    }

    private void CreateStreetLitterCarton(Transform parent, int seed, Vector3 position)
    {
        GameObject carton = GameObject.CreatePrimitive(PrimitiveType.Cube);
        carton.name = "StreetLitterCarton";
        carton.transform.SetParent(parent, false);
        carton.transform.position = position + Vector3.up * 0.022f;
        carton.transform.rotation = Quaternion.Euler(0f, StreetLitterHash01(seed + 53) * 360f, StreetLitterHash01(seed + 59) * 18f - 9f);
        carton.transform.localScale = new Vector3(0.075f, 0.045f, 0.055f);
        ApplyColor(carton, new Color(0.56f, 0.42f, 0.25f), VisualSmoothnessDefault);
        ConfigureStreetLitterPiece(carton);
    }

    private static void ConfigureStreetLitterPiece(GameObject piece)
    {
        ConfigureStaticVisual(piece, VisualSmoothnessDefault);
        if (piece.TryGetComponent(out Collider collider))
        {
            collider.enabled = false;
        }
    }

    private void PruneStreetLitterVisuals()
    {
        if (streetLitterVisualsByCell.Count <= StreetLitterMaxVisibleCells)
        {
            return;
        }

        streetLitterScratchCells.Clear();
        streetLitterScratchCells.AddRange(streetLitterVisualsByCell.Keys);
        streetLitterScratchCells.Sort((a, b) =>
        {
            streetLitterByCell.TryGetValue(a, out float aScore);
            streetLitterByCell.TryGetValue(b, out float bScore);
            int scoreCompare = aScore.CompareTo(bScore);
            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            int yCompare = a.y.CompareTo(b.y);
            return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
        });

        int removeCount = streetLitterVisualsByCell.Count - StreetLitterMaxVisibleCells;
        for (int i = 0; i < removeCount && i < streetLitterScratchCells.Count; i++)
        {
            ClearStreetLitterAtCell(streetLitterScratchCells[i]);
        }

        streetLitterScratchCells.Clear();
    }

    private void PruneStreetLitterTrackedCells()
    {
        if (streetLitterByCell.Count <= StreetLitterMaxTrackedCells)
        {
            return;
        }

        streetLitterScratchCells.Clear();
        streetLitterScratchCells.AddRange(streetLitterByCell.Keys);
        streetLitterScratchCells.Sort((a, b) =>
        {
            streetLitterByCell.TryGetValue(a, out float aScore);
            streetLitterByCell.TryGetValue(b, out float bScore);
            int scoreCompare = aScore.CompareTo(bScore);
            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            int yCompare = a.y.CompareTo(b.y);
            return yCompare != 0 ? yCompare : a.x.CompareTo(b.x);
        });

        int removeCount = streetLitterByCell.Count - StreetLitterMaxTrackedCells;
        for (int i = 0; i < removeCount && i < streetLitterScratchCells.Count; i++)
        {
            ClearStreetLitterAtCell(streetLitterScratchCells[i]);
        }

        streetLitterScratchCells.Clear();
    }

    private bool ClearStreetLitterAtCell(Vector2Int cell)
    {
        streetLitterReservationsByCell.Remove(cell);
        bool removedScore = streetLitterByCell.Remove(cell);
        bool removedVisual = DestroyStreetLitterVisual(cell);
        return removedScore || removedVisual;
    }

    private int ClearStreetLitterInFootprint(Vector2Int min, Vector2Int max)
    {
        int removed = 0;
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                if (ClearStreetLitterAtCell(new Vector2Int(x, y)))
                {
                    removed++;
                }
            }
        }

        return removed;
    }

    private int ClearStreetLitterInBuildingWalkBuffer(Vector2Int min, Vector2Int max, Vector2Int openingCell)
    {
        int removed = 0;
        for (int x = min.x - 1; x <= max.x + 1; x++)
        {
            for (int y = min.y - 1; y <= max.y + 1; y++)
            {
                Vector2Int cell = new(x, y);
                if ((x >= min.x && x <= max.x && y >= min.y && y <= max.y) ||
                    cell == openingCell)
                {
                    continue;
                }

                if (ClearStreetLitterAtCell(cell))
                {
                    removed++;
                }
            }
        }

        return removed;
    }

    private int CountVisibleStreetLitterCells()
    {
        int count = 0;
        foreach (KeyValuePair<Vector2Int, float> pair in streetLitterByCell)
        {
            if (GetStreetLitterStage(pair.Value) > 0)
            {
                count++;
            }
        }

        return count;
    }

    private bool TryReserveStreetLitterCleanupTarget(DriverAgent worker, Vector3 startWorld, out Vector2Int targetCell, out Vector3 targetWorld)
    {
        targetCell = default;
        targetWorld = default;
        if (worker == null || streetLitterByCell.Count == 0)
        {
            return false;
        }

        if (!TryGetCleanerCoverageCenter(worker, out Vector3 coverageCenter))
        {
            return false;
        }

        Vector2Int startCell = WorldToCell(startWorld);
        float bestScore = float.NegativeInfinity;
        List<Vector2Int> bestPath = null;
        streetLitterCleanerCandidateCells.Clear();
        streetLitterCleanerCandidateScores.Clear();
        foreach (KeyValuePair<Vector2Int, float> pair in streetLitterByCell)
        {
            Vector2Int cell = pair.Key;
            if (GetStreetLitterStage(pair.Value) <= 0 ||
                !CanStreetLitterOccupyCell(cell) ||
                !IsCellWithinCleanerCoverage(cell, coverageCenter))
            {
                continue;
            }

            if (streetLitterReservationsByCell.TryGetValue(cell, out int reservedBy) && reservedBy != worker.DriverId)
            {
                continue;
            }

            float candidateScore = pair.Value * 10f - EstimateGridDistance(startCell, cell) * 0.85f;
            AddStreetLitterCleanerCandidate(cell, candidateScore);
        }

        for (int i = 0; i < streetLitterCleanerCandidateCells.Count; i++)
        {
            Vector2Int cell = streetLitterCleanerCandidateCells[i];
            if (!streetLitterByCell.TryGetValue(cell, out float litterScore))
            {
                continue;
            }

            List<Vector2Int> path = FindDriverWalkPath(startCell, cell, DriverRescuePhase.CleanerToLitter);
            if (path == null || path.Count == 0 || (path.Count <= 1 && startCell != cell))
            {
                continue;
            }

            float score = litterScore * 10f - path.Count * 1.35f;
            if (score <= bestScore)
            {
                continue;
            }

            bestScore = score;
            targetCell = cell;
            bestPath = path;
        }

        if (bestPath == null)
        {
            return false;
        }

        streetLitterReservationsByCell[targetCell] = worker.DriverId;
        worker.CleanerTargetCell = targetCell;
        worker.HasCleanerTargetCell = true;
        targetWorld = GetStreetLitterCleanupWorld(targetCell);
        return true;
    }

    private void AddStreetLitterCleanerCandidate(Vector2Int cell, float score)
    {
        int insertIndex = streetLitterCleanerCandidateScores.Count;
        while (insertIndex > 0 && score > streetLitterCleanerCandidateScores[insertIndex - 1])
        {
            insertIndex--;
        }

        if (insertIndex >= CleanerTargetMaxPathChecks)
        {
            return;
        }

        streetLitterCleanerCandidateCells.Insert(insertIndex, cell);
        streetLitterCleanerCandidateScores.Insert(insertIndex, score);
        if (streetLitterCleanerCandidateCells.Count > CleanerTargetMaxPathChecks)
        {
            int lastIndex = streetLitterCleanerCandidateCells.Count - 1;
            streetLitterCleanerCandidateCells.RemoveAt(lastIndex);
            streetLitterCleanerCandidateScores.RemoveAt(lastIndex);
        }
    }

    private bool IsStreetLitterCleanupTargetValid(DriverAgent worker)
    {
        if (worker == null || !worker.HasCleanerTargetCell)
        {
            return false;
        }

        Vector2Int cell = worker.CleanerTargetCell;
        if (!streetLitterByCell.TryGetValue(cell, out float score) || GetStreetLitterStage(score) <= 0)
        {
            return false;
        }

        return CanStreetLitterOccupyCell(cell) &&
               (!streetLitterReservationsByCell.TryGetValue(cell, out int reservedBy) || reservedBy == worker.DriverId);
    }

    private void ReleaseStreetLitterReservation(DriverAgent worker)
    {
        if (worker == null || !worker.HasCleanerTargetCell)
        {
            return;
        }

        Vector2Int cell = worker.CleanerTargetCell;
        if (streetLitterReservationsByCell.TryGetValue(cell, out int reservedBy) && reservedBy == worker.DriverId)
        {
            streetLitterReservationsByCell.Remove(cell);
        }

        worker.HasCleanerTargetCell = false;
        worker.CleanerTargetCell = default;
    }

    private bool CleanReservedStreetLitterTarget(DriverAgent worker)
    {
        if (!IsStreetLitterCleanupTargetValid(worker))
        {
            ReleaseStreetLitterReservation(worker);
            return false;
        }

        Vector2Int cell = worker.CleanerTargetCell;
        bool removed = ClearStreetLitterAtCell(cell);
        worker.HasCleanerTargetCell = false;
        worker.CleanerTargetCell = default;
        return removed;
    }

    private Vector3 GetStreetLitterCleanupWorld(Vector2Int cell)
    {
        Vector3 center = GetCellCenter(cell);
        center.y = GetStreetLitterSurfaceY(cell, center.x, center.z) + 0.035f;
        return center;
    }

    private bool DestroyStreetLitterVisual(Vector2Int cell)
    {
        if (!streetLitterVisualsByCell.TryGetValue(cell, out GameObject root))
        {
            return false;
        }

        streetLitterVisualsByCell.Remove(cell);
        if (root != null)
        {
            Destroy(root);
        }

        return true;
    }

    private static int GetStreetLitterStage(float score)
    {
        if (score >= StreetLitterStageThreeScore)
        {
            return 3;
        }

        if (score >= StreetLitterStageTwoScore)
        {
            return 2;
        }

        return score >= StreetLitterVisibleScore ? 1 : 0;
    }

    private static float StreetLitterHash01(int seed)
    {
        unchecked
        {
            uint x = (uint)seed;
            x ^= x >> 16;
            x *= 0x7feb352du;
            x ^= x >> 15;
            x *= 0x846ca68bu;
            x ^= x >> 16;
            return (x & 0x00FFFFFFu) / 16777215f;
        }
    }
}
