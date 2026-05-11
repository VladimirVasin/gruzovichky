using UnityEngine;

public partial class GameBootstrap
{
    private void ResetFootpathSystem()
    {
        footpathWearByCell.Clear();
        visibleFootpathCells.Clear();

        for (int i = 0; i < driverAgents.Count; i++)
        {
            driverAgents[i].HasLastFootpathWearCell = false;
        }
    }

    private void RecordDriverFootpathWear(DriverAgent driver, Vector3 position)
    {
        if (driver == null ||
            driver.DriverObject == null ||
            driver.IsInsideBuilding ||
            driver.IsDrivingPersonalCar ||
            driver.WalkPhase == DriverRescuePhase.None ||
            driver.WalkPhase == DriverRescuePhase.RidingLocalBus ||
            driver.WalkPhase == DriverRescuePhase.WaitingAtLocalBusStop)
        {
            return;
        }

        Vector2Int cell = WorldToCell(position);
        if (driver.HasLastFootpathWearCell && driver.LastFootpathWearCell == cell)
        {
            return;
        }

        driver.LastFootpathWearCell = cell;
        driver.HasLastFootpathWearCell = true;

        if (!CanFootpathOccupyCell(cell))
        {
            return;
        }

        footpathWearByCell.TryGetValue(cell, out float previousWear);
        float nextWear = Mathf.Min(FootpathMaxWear, previousWear + FootpathWearPerCellVisit);
        if (Mathf.Approximately(previousWear, nextWear))
        {
            return;
        }

        footpathWearByCell[cell] = nextWear;

        int previousStage = GetFootpathVisualStage(previousWear);
        int nextStage = GetFootpathVisualStage(nextWear);
        if (nextStage <= 0)
        {
            return;
        }

        visibleFootpathCells.Add(cell);
        if (nextStage != previousStage)
        {
            RefreshGroundCellSurfaceMaterial(cell);
        }
    }

    private float GetDriverWalkCellCost(Vector2Int cell, Vector2Int start, Vector2Int goal)
    {
        if (cell == start || cell == goal)
        {
            return 1f;
        }

        float baseCost = 1f;
        if (IsVisibleFootpathCell(cell))
        {
            float t = GetFootpathWear01(cell);
            baseCost = Mathf.Lerp(FootpathFreshWalkCost, FootpathStrongWalkCost, t);
        }

        return baseCost + GetDriverWalkCongestionCost(cell);
    }

    private float GetDriverWalkCongestionCost(Vector2Int cell)
    {
        float cost = 0f;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent other = driverAgents[i];
            if (other == null ||
                other.DriverObject == null ||
                !other.DriverObject.activeSelf ||
                other.IsInsideBuilding ||
                other.IsDrivingPersonalCar)
            {
                continue;
            }

            Vector2Int otherCell = WorldToCell(other.DriverObject.transform.position);
            if (otherCell == cell)
            {
                cost += DriverWalkOccupiedCellCost;
            }
            else if (Mathf.Abs(otherCell.x - cell.x) + Mathf.Abs(otherCell.y - cell.y) == 1)
            {
                cost += DriverWalkAdjacentCellCost;
            }

            if (cost >= DriverWalkMaxCongestionCost)
            {
                return DriverWalkMaxCongestionCost;
            }
        }

        return cost;
    }

    private bool ClearFootpathAtCell(Vector2Int cell, bool refreshGround = true)
    {
        bool removedWear = footpathWearByCell.Remove(cell);
        bool removedVisible = visibleFootpathCells.Remove(cell);
        if (!removedWear && !removedVisible)
        {
            return false;
        }

        if (refreshGround)
        {
            RefreshGroundCellSurfaceMaterial(cell);
        }

        return true;
    }

    private int ClearFootpathsInFootprint(Vector2Int min, Vector2Int max)
    {
        int removed = 0;
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                if (ClearFootpathAtCell(new Vector2Int(x, y)))
                {
                    removed++;
                }
            }
        }

        return removed;
    }

    private void ApplyGroundCellSurfaceMaterial(GameObject target, int x, int y)
    {
        Vector2Int cell = new(x, y);
        if (IsVisibleFootpathCell(cell))
        {
            ApplyFootpathGroundMaterial(target, cell);
            return;
        }

        ApplyStylizedGroundMaterial(target, x, y);
    }

    private void RefreshGroundCellSurfaceMaterial(Vector2Int cell)
    {
        if (!IsInsideGrid(cell) || IsWaterOrBeachCell(cell) || groundRoot == null)
        {
            return;
        }

        Transform tile = groundRoot.Find($"Ground_{cell.x}_{cell.y}");
        if (tile == null)
        {
            return;
        }

        ApplyGroundCellSurfaceMaterial(tile.gameObject, cell.x, cell.y);
    }

    private void ApplyFootpathGroundMaterial(GameObject target, Vector2Int cell)
    {
        if (target == null)
        {
            return;
        }

        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        float wear01 = GetFootpathWear01(cell);
        float tintNoise = Mathf.PerlinNoise((cell.x + 1) * 0.33f + 6.8f, (cell.y + 1) * 0.35f + 9.1f);
        Color fresh = Color.Lerp(new Color(0.80f, 0.70f, 0.48f), new Color(0.92f, 0.82f, 0.60f), tintNoise);
        Color packed = new(0.50f, 0.40f, 0.26f);
        Color tint = QuantizeVisualTint(Color.Lerp(fresh, packed, wear01), 14f);
        Texture texture = footpathSurfaceTexture != null ? footpathSurfaceTexture : groundSurfaceTexture;
        renderer.sharedMaterial = GetCachedLitMaterial(texture, tint, 0.07f, new Vector2(0.96f, 0.96f));
    }

    private bool IsVisibleFootpathCell(Vector2Int cell)
    {
        return visibleFootpathCells.Contains(cell) &&
               footpathWearByCell.TryGetValue(cell, out float wear) &&
               wear >= FootpathVisibleWear &&
               CanFootpathOccupyCell(cell);
    }

    private bool CanFootpathOccupyCell(Vector2Int cell)
    {
        if (!IsInsideGrid(cell) ||
            IsWaterOrBeachCell(cell) ||
            roadCells.Contains(cell) ||
            edgeHighwayCells.Contains(cell))
        {
            return false;
        }

        return !IsLocationCell(cell) || IsCityParkFootpathCell(cell);
    }

    private bool IsCityParkFootpathCell(Vector2Int cell)
    {
        return locations.TryGetValue(LocationType.CityPark, out LocationData park) && park.Contains(cell);
    }

    private float GetFootpathWear01(Vector2Int cell)
    {
        if (!footpathWearByCell.TryGetValue(cell, out float wear))
        {
            return 0f;
        }

        return Mathf.InverseLerp(FootpathVisibleWear, FootpathStrongWear, wear);
    }

    private int GetFootpathVisualStage(float wear)
    {
        if (wear >= FootpathStrongWear)
        {
            return 3;
        }

        if (wear >= FootpathEstablishedWear)
        {
            return 2;
        }

        if (wear >= FootpathVisibleWear)
        {
            return 1;
        }

        return 0;
    }
}
