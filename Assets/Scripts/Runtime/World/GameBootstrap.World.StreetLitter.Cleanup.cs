using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private string GetStreetLitterTopSourceSummary(bool ru)
    {
        if (streetLitterByCell.Count == 0)
        {
            return ru ? "\u043d\u0435\u0442" : "none";
        }

        Dictionary<StreetLitterSourceKind, int> counts = new();
        foreach (KeyValuePair<Vector2Int, float> pair in streetLitterByCell)
        {
            if (GetStreetLitterStage(pair.Value) <= 0)
            {
                continue;
            }

            StreetLitterSourceKind source = streetLitterSourceByCell.TryGetValue(pair.Key, out StreetLitterSourceKind known)
                ? known
                : StreetLitterSourceKind.PedestrianDensity;
            counts.TryGetValue(source, out int count);
            counts[source] = count + 1;
        }

        StreetLitterSourceKind best = StreetLitterSourceKind.PedestrianDensity;
        int bestCount = 0;
        foreach (KeyValuePair<StreetLitterSourceKind, int> pair in counts)
        {
            if (pair.Value > bestCount)
            {
                best = pair.Key;
                bestCount = pair.Value;
            }
        }

        return bestCount <= 0
            ? (ru ? "\u043d\u0435\u0442" : "none")
            : $"{GetStreetLitterSourceLabel(best, ru)} ({bestCount})";
    }

    private static string GetStreetLitterSourceLabel(StreetLitterSourceKind source, bool ru)
    {
        return source switch
        {
            StreetLitterSourceKind.TransportStop => ru ? "\u043e\u0441\u0442\u0430\u043d\u043e\u0432\u043a\u0438" : "transport stops",
            StreetLitterSourceKind.TrashCanOverflow => ru ? "\u0443\u0440\u043d\u044b" : "trash cans",
            StreetLitterSourceKind.FoodService => ru ? "\u0435\u0434\u0430" : "food service",
            StreetLitterSourceKind.Nightlife => ru ? "\u0431\u0430\u0440\u044b/\u0430\u0437\u0430\u0440\u0442" : "nightlife",
            StreetLitterSourceKind.StreetCommerce => ru ? "\u043a\u0438\u043e\u0441\u043a\u0438" : "street commerce",
            StreetLitterSourceKind.PublicLeisure => ru ? "\u043e\u0442\u0434\u044b\u0445" : "public leisure",
            StreetLitterSourceKind.PoorInfrastructure => ru ? "\u0441\u043b\u0430\u0431\u0430\u044f \u0438\u043d\u0444\u0440\u0430\u0441\u0442\u0440\u0443\u043a\u0442\u0443\u0440\u0430" : "weak infrastructure",
            _ => ru ? "\u0441\u043a\u043e\u043f\u043b\u0435\u043d\u0438\u044f \u043b\u044e\u0434\u0435\u0439" : "crowds"
        };
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
}
