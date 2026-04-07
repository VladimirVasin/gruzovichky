using System.Collections.Generic;
using UnityEngine;

public static class WorldLayoutGenerator
{
    public static GeneratedWorldLayout Generate(int gridWidth, int gridHeight, System.Func<GeneratedWorldLayout, bool> isValidLayout = null)
    {
        GeneratedWorldLayout bestCandidate = null;
        int bestScore = int.MinValue;

        for (int attempt = 0; attempt < 160; attempt++)
        {
            Dictionary<string, WorldLocationPlacement> candidate = new();
            if (!TryPlaceParking(candidate, gridWidth, gridHeight))
            {
                continue;
            }

            if (!TryPlaceGasStation(candidate, gridWidth, gridHeight))
            {
                continue;
            }

            Vector2Int parkingAnchor = candidate["Parking"].Anchor;
            bool parkingOnLeft = parkingAnchor.x < gridWidth * 0.5f;
            bool parkingOnBottom = parkingAnchor.y < gridHeight * 0.5f;

            if (!TryPlaceInteriorLocation(candidate, "Warehouse", 2, 2, 7, 12, 7, 12, true, gridWidth, gridHeight, parkingAnchor))
            {
                continue;
            }

            int forestMinX = parkingOnLeft ? 9 : 2;
            int forestMaxX = parkingOnLeft ? 17 : 11;
            int forestMinY = parkingOnBottom ? 10 : 8;
            int forestMaxY = 17;
            if (!TryPlaceInteriorLocation(candidate, "Forest", 3, 3, forestMinX, forestMaxX, forestMinY, forestMaxY, true, gridWidth, gridHeight, parkingAnchor))
            {
                continue;
            }

            int townMinX = parkingOnLeft ? 9 : 2;
            int townMaxX = parkingOnLeft ? 17 : 11;
            int townMinY = parkingOnBottom ? 2 : 3;
            int townMaxY = parkingOnBottom ? 10 : 11;
            if (!TryPlaceInteriorLocation(candidate, "Town", 2, 2, townMinX, townMaxX, townMinY, townMaxY, true, gridWidth, gridHeight, parkingAnchor))
            {
                continue;
            }

            int motelMinX = parkingOnLeft ? 9 : 2;
            int motelMaxX = parkingOnLeft ? 16 : 11;
            int motelMinY = parkingOnBottom ? 3 : 4;
            int motelMaxY = parkingOnBottom ? 10 : 13;
            if (!TryPlaceInteriorLocation(candidate, "Motel", 2, 2, motelMinX, motelMaxX, motelMinY, motelMaxY, true, gridWidth, gridHeight, parkingAnchor))
            {
                continue;
            }

            GeneratedWorldLayout layout = ToLayout(candidate);
            int score = ScoreLayoutDistribution(layout);
            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = layout;
            }

            bool passesValidation = isValidLayout == null || isValidLayout(layout);
            if (score >= 34 && passesValidation)
            {
                return layout;
            }
        }

        if (bestCandidate != null && (isValidLayout == null || isValidLayout(bestCandidate)))
        {
            return bestCandidate;
        }

        return CreateFallbackLayout();
    }

    private static bool TryPlaceParking(Dictionary<string, WorldLocationPlacement> placements, int gridWidth, int gridHeight)
    {
        int corner = Random.Range(0, 4);
        WorldPlacementFacing facing = corner <= 1 ? WorldPlacementFacing.North : WorldPlacementFacing.South;

        for (int attempt = 0; attempt < 24; attempt++)
        {
            Vector2Int anchor = corner switch
            {
                0 => new Vector2Int(Random.Range(2, 5), Random.Range(4, 6)),
                1 => new Vector2Int(Random.Range(gridWidth - 5, gridWidth - 2), Random.Range(4, 6)),
                2 => new Vector2Int(Random.Range(2, 5), Random.Range(gridHeight - 6, gridHeight - 3)),
                _ => new Vector2Int(Random.Range(gridWidth - 5, gridWidth - 2), Random.Range(gridHeight - 6, gridHeight - 3))
            };

            if (!TryCreatePlacementFromAnchor(anchor, 3, 2, facing, gridWidth, gridHeight, out WorldLocationPlacement parking) ||
                !PlacementFits(parking, placements.Values, 1, gridWidth, gridHeight))
            {
                continue;
            }

            placements["Parking"] = parking;
            return true;
        }

        return false;
    }

    private static bool TryPlaceGasStation(Dictionary<string, WorldLocationPlacement> placements, int gridWidth, int gridHeight)
    {
        WorldLocationPlacement parking = placements["Parking"];
        bool lowerHalf = parking.Anchor.y < gridHeight * 0.5f;
        int xDirection = parking.Anchor.x < gridWidth * 0.5f ? 1 : -1;
        int yDirection = lowerHalf ? 1 : -1;
        WorldPlacementFacing facing = lowerHalf ? WorldPlacementFacing.North : WorldPlacementFacing.South;

        for (int attempt = 0; attempt < 48; attempt++)
        {
            Vector2Int anchor = parking.Anchor + new Vector2Int(xDirection * Random.Range(1, 5), yDirection * Random.Range(2, 5));
            if (!TryCreatePlacementFromAnchor(anchor, 2, 2, facing, gridWidth, gridHeight, out WorldLocationPlacement gasStation) ||
                !PlacementFits(gasStation, placements.Values, 1, gridWidth, gridHeight))
            {
                continue;
            }

            if (ManhattanDistance(parking.Anchor, gasStation.Anchor) > 7)
            {
                continue;
            }

            placements["GasStation"] = gasStation;
            return true;
        }

        return false;
    }

    private static bool TryPlaceInteriorLocation(
        Dictionary<string, WorldLocationPlacement> placements,
        string key,
        int width,
        int height,
        int minAnchorX,
        int maxAnchorX,
        int minAnchorY,
        int maxAnchorY,
        bool requireFarFromParking,
        int gridWidth,
        int gridHeight,
        Vector2Int parkingAnchor)
    {
        for (int attempt = 0; attempt < 80; attempt++)
        {
            WorldPlacementFacing facing = (WorldPlacementFacing)Random.Range(0, 4);
            Vector2Int anchor = new Vector2Int(Random.Range(minAnchorX, maxAnchorX + 1), Random.Range(minAnchorY, maxAnchorY + 1));
            if (!TryCreatePlacementFromAnchor(anchor, width, height, facing, gridWidth, gridHeight, out WorldLocationPlacement placement) ||
                !PlacementFits(placement, placements.Values, 1, gridWidth, gridHeight))
            {
                continue;
            }

            if (requireFarFromParking && ManhattanDistance(anchor, parkingAnchor) < 6)
            {
                continue;
            }

            placements[key] = placement;
            return true;
        }

        return false;
    }

    private static bool TryCreatePlacementFromAnchor(Vector2Int anchor, int width, int height, WorldPlacementFacing facing, int gridWidth, int gridHeight, out WorldLocationPlacement placement)
    {
        placement = new WorldLocationPlacement { Anchor = anchor };
        switch (facing)
        {
            case WorldPlacementFacing.North:
                placement.Min = new Vector2Int(anchor.x - width / 2, anchor.y - height);
                placement.Max = new Vector2Int(placement.Min.x + width - 1, anchor.y - 1);
                break;
            case WorldPlacementFacing.South:
                placement.Min = new Vector2Int(anchor.x - width / 2, anchor.y + 1);
                placement.Max = new Vector2Int(placement.Min.x + width - 1, placement.Min.y + height - 1);
                break;
            case WorldPlacementFacing.East:
                placement.Min = new Vector2Int(anchor.x - width, anchor.y - height / 2);
                placement.Max = new Vector2Int(anchor.x - 1, placement.Min.y + height - 1);
                break;
            default:
                placement.Min = new Vector2Int(anchor.x + 1, anchor.y - height / 2);
                placement.Max = new Vector2Int(placement.Min.x + width - 1, placement.Min.y + height - 1);
                break;
        }

        return IsPlacementInsideGrid(placement, gridWidth, gridHeight) && !placement.Contains(anchor);
    }

    private static bool PlacementFits(WorldLocationPlacement placement, IEnumerable<WorldLocationPlacement> existingPlacements, int padding, int gridWidth, int gridHeight)
    {
        if (!IsPlacementInsideGrid(placement, gridWidth, gridHeight))
        {
            return false;
        }

        foreach (WorldLocationPlacement existing in existingPlacements)
        {
            if (RectanglesOverlapExpanded(placement, existing, padding) ||
                existing.Contains(placement.Anchor) ||
                placement.Contains(existing.Anchor) ||
                placement.Anchor == existing.Anchor)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsPlacementInsideGrid(WorldLocationPlacement placement, int gridWidth, int gridHeight)
    {
        return IsInsideGrid(placement.Min, gridWidth, gridHeight) &&
               IsInsideGrid(placement.Max, gridWidth, gridHeight) &&
               IsInsideGrid(placement.Anchor, gridWidth, gridHeight);
    }

    private static bool IsInsideGrid(Vector2Int cell, int gridWidth, int gridHeight)
    {
        return cell.x >= 0 && cell.x < gridWidth && cell.y >= 0 && cell.y < gridHeight;
    }

    private static bool RectanglesOverlapExpanded(WorldLocationPlacement a, WorldLocationPlacement b, int padding)
    {
        return a.Min.x - padding <= b.Max.x + padding &&
               a.Max.x + padding >= b.Min.x - padding &&
               a.Min.y - padding <= b.Max.y + padding &&
               a.Max.y + padding >= b.Min.y - padding;
    }

    private static int ScoreLayoutDistribution(GeneratedWorldLayout layout)
    {
        Vector2Int[] anchors =
        {
            layout.Parking.Anchor,
            layout.GasStation.Anchor,
            layout.Warehouse.Anchor,
            layout.Forest.Anchor,
            layout.Town.Anchor
        };

        int minPairDistance = int.MaxValue;
        for (int i = 0; i < anchors.Length; i++)
        {
            for (int j = i + 1; j < anchors.Length; j++)
            {
                minPairDistance = Mathf.Min(minPairDistance, ManhattanDistance(anchors[i], anchors[j]));
            }
        }

        int minX = anchors[0].x;
        int maxX = anchors[0].x;
        int minY = anchors[0].y;
        int maxY = anchors[0].y;
        foreach (Vector2Int anchor in anchors)
        {
            minX = Mathf.Min(minX, anchor.x);
            maxX = Mathf.Max(maxX, anchor.x);
            minY = Mathf.Min(minY, anchor.y);
            maxY = Mathf.Max(maxY, anchor.y);
        }

        int spanX = maxX - minX;
        int spanY = maxY - minY;

        HashSet<Vector2Int> macroCells = new();
        foreach (Vector2Int anchor in anchors)
        {
            macroCells.Add(new Vector2Int(Mathf.Clamp(anchor.x / 7, 0, 2), Mathf.Clamp(anchor.y / 7, 0, 2)));
        }

        int score = minPairDistance + spanX + spanY + macroCells.Count * 4;
        if (ManhattanDistance(layout.Parking.Anchor, layout.GasStation.Anchor) > 8)
        {
            score -= 6;
        }

        if (spanX < 8 || spanY < 8)
        {
            score -= 10;
        }

        if (macroCells.Count < 4)
        {
            score -= 8;
        }

        return score;
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static GeneratedWorldLayout ToLayout(Dictionary<string, WorldLocationPlacement> placements)
    {
        return new GeneratedWorldLayout
        {
            Parking = placements["Parking"],
            GasStation = placements["GasStation"],
            Forest = placements["Forest"],
            Warehouse = placements["Warehouse"],
            Town = placements["Town"],
            Motel = placements["Motel"]
        };
    }

    private static GeneratedWorldLayout CreateFallbackLayout()
    {
        return new GeneratedWorldLayout
        {
            Parking = new WorldLocationPlacement { Min = new Vector2Int(2, 2), Max = new Vector2Int(4, 3), Anchor = new Vector2Int(3, 4) },
            GasStation = new WorldLocationPlacement { Min = new Vector2Int(6, 4), Max = new Vector2Int(7, 5), Anchor = new Vector2Int(6, 6) },
            Forest = new WorldLocationPlacement { Min = new Vector2Int(3, 14), Max = new Vector2Int(5, 16), Anchor = new Vector2Int(4, 13) },
            Warehouse = new WorldLocationPlacement { Min = new Vector2Int(9, 9), Max = new Vector2Int(10, 10), Anchor = new Vector2Int(9, 8) },
            Town = new WorldLocationPlacement { Min = new Vector2Int(14, 2), Max = new Vector2Int(15, 3), Anchor = new Vector2Int(13, 3) },
            Motel = new WorldLocationPlacement { Min = new Vector2Int(13, 8), Max = new Vector2Int(14, 9), Anchor = new Vector2Int(13, 7) }
        };
    }
}
