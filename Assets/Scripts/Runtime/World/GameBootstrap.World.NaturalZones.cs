using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int ForestZoneMinCount = 4;
    private const int ForestZoneMaxCount = 7;
    private const int HillZoneMinCount = 3;
    private const int HillZoneMaxCount = 5;
    private const int LakeZoneMinCount = 3;
    private const int LakeZoneMaxCount = 5;
    private const float LakeWaterHeight = 0.22f;

    private void GenerateNaturalZones()
    {
        forestZones.Clear();
        hillZones.Clear();
        lakeZones.Clear();
        lakeWaterCells.Clear();
        naturalBeachCells.Clear();

        AddRandomNaturalZones(forestZones, Random.Range(ForestZoneMinCount, ForestZoneMaxCount + 1), 8f, 18f, 8f, 18f, 1f);
        AddRandomNaturalZones(hillZones, Random.Range(HillZoneMinCount, HillZoneMaxCount + 1), 7f, 15f, 7f, 15f, 1.8f);
        AddRandomNaturalZones(lakeZones, Random.Range(LakeZoneMinCount, LakeZoneMaxCount + 1), 4.5f, 8.5f, 3.8f, 7.5f, 1f);

        SessionDebugLogger.Log(
            "WORLD",
            $"Natural zones generated: forests={forestZones.Count}, hills={hillZones.Count}, lakes={lakeZones.Count}.");
    }

    private void AddRandomNaturalZones(
        List<NaturalZoneData> target,
        int count,
        float minRadiusX,
        float maxRadiusX,
        float minRadiusY,
        float maxRadiusY,
        float strength)
    {
        const int maxAttemptsPerZone = 32;
        int safeTop = GridHeight - WaterRiverWidth - 8;
        for (int i = 0; i < count; i++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < maxAttemptsPerZone && !placed; attempt++)
            {
                Vector2 radius = new(Random.Range(minRadiusX, maxRadiusX), Random.Range(minRadiusY, maxRadiusY));
                Vector2 center = new(
                    Random.Range(8f + radius.x, GridWidth - 8f - radius.x),
                    Random.Range(8f + radius.y, safeTop - radius.y));

                if (IsTooCloseToExistingZone(center, radius, target))
                {
                    continue;
                }

                target.Add(new NaturalZoneData(center, radius, strength, Random.Range(0f, 1000f)));
                placed = true;
            }
        }
    }

    private static bool IsTooCloseToExistingZone(Vector2 center, Vector2 radius, List<NaturalZoneData> zones)
    {
        float minDistance = Mathf.Max(radius.x, radius.y) * 0.55f;
        for (int i = 0; i < zones.Count; i++)
        {
            float otherRadius = Mathf.Max(zones[i].Radius.x, zones[i].Radius.y);
            if (Vector2.Distance(center, zones[i].Center) < minDistance + otherRadius * 0.45f)
            {
                return true;
            }
        }

        return false;
    }

    private void AddLakeWaterCellsFromNaturalZones()
    {
        lakeWaterCells.Clear();
        foreach (NaturalZoneData zone in lakeZones)
        {
            int minX = Mathf.Max(2, Mathf.FloorToInt(zone.Center.x - zone.Radius.x - 1f));
            int maxX = Mathf.Min(GridWidth - 3, Mathf.CeilToInt(zone.Center.x + zone.Radius.x + 1f));
            int minY = Mathf.Max(4, Mathf.FloorToInt(zone.Center.y - zone.Radius.y - 1f));
            int maxY = Mathf.Min(GridHeight - WaterRiverWidth - 5, Mathf.CeilToInt(zone.Center.y + zone.Radius.y + 1f));

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    if (!zone.ContainsCell(x, y, 0.19f, 0.18f))
                    {
                        continue;
                    }

                    Vector2Int cell = new(x, y);
                    lakeWaterCells.Add(cell);
                    waterCells.Add(cell);
                }
            }
        }

        RebuildNaturalBeachCells();
        SessionDebugLogger.Log("WORLD", $"Lake water generated: lakeCells={lakeWaterCells.Count}, lakeBeachCells={naturalBeachCells.Count}.");
    }

    private void RebuildNaturalBeachCells()
    {
        naturalBeachCells.Clear();
        foreach (Vector2Int waterCell in lakeWaterCells)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    Vector2Int beach = new(waterCell.x + dx, waterCell.y + dy);
                    if (beach.x < 0 || beach.x >= GridWidth || beach.y < 0 || beach.y >= GridHeight || waterCells.Contains(beach))
                    {
                        continue;
                    }

                    naturalBeachCells.Add(beach);
                }
            }
        }
    }

    private float GetForestZoneInfluence(int x, int y)
    {
        float influence = 0f;
        for (int i = 0; i < forestZones.Count; i++)
        {
            influence = Mathf.Max(influence, forestZones[i].GetInfluence(x + 0.5f, y + 0.5f));
        }

        return influence;
    }

    private bool IsNaturalForestZoneCell(int x, int y)
    {
        if (IsWaterOrBeachCell(new Vector2Int(x, y)))
        {
            return false;
        }

        float influence = GetForestZoneInfluence(x, y);
        if (influence <= 0f)
        {
            return false;
        }

        float noise = Mathf.PerlinNoise((x + 31) * 0.22f, (y + 17) * 0.2f);
        return influence > 0.22f || noise + influence > 0.78f;
    }

    private bool IsNaturalBeachCell(Vector2Int cell)
    {
        if (naturalBeachCells.Contains(cell))
        {
            return true;
        }

        int shoreRow = GridHeight - WaterRiverWidth;
        return cell.y == shoreRow - 1 || cell.y == shoreRow - 2;
    }
}
