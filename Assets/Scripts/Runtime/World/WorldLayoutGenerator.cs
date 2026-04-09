using System.Collections.Generic;
using UnityEngine;

public static class WorldLayoutGenerator
{
    private const int PlacementPadding = 2;
    private const int FarFromParkingDistance = 8;
    private const int DecorativeBottomRoadOccupiedRows = 2;
    private const int DecorativeBottomRoadClearanceRows = 2;

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

            int innerMinX = 4;
            int innerMaxX = gridWidth - 5;
            int innerMinY = 4;
            int innerMaxY = gridHeight - 5;
            int horizontalMid = gridWidth / 2;
            int verticalMid = gridHeight / 2;

            int warehouseMinX = parkingOnLeft ? horizontalMid + 2 : innerMinX;
            int warehouseMaxX = parkingOnLeft ? innerMaxX : horizontalMid - 2;
            int warehouseMinY = parkingOnBottom ? verticalMid + 1 : innerMinY;
            int warehouseMaxY = parkingOnBottom ? innerMaxY : verticalMid - 1;
            if (!TryPlaceInteriorLocation(candidate, "Warehouse", 2, 2, warehouseMinX, warehouseMaxX, warehouseMinY, warehouseMaxY, true, gridWidth, gridHeight, parkingAnchor))
            {
                continue;
            }

            int forestMinX = parkingOnLeft ? horizontalMid + 1 : innerMinX;
            int forestMaxX = parkingOnLeft ? innerMaxX : horizontalMid - 1;
            int forestMinY = parkingOnBottom ? innerMinY : verticalMid + 1;
            int forestMaxY = parkingOnBottom ? verticalMid - 1 : innerMaxY;
            if (!TryPlaceInteriorLocation(candidate, "Forest", 3, 3, forestMinX, forestMaxX, forestMinY, forestMaxY, true, gridWidth, gridHeight, parkingAnchor))
            {
                continue;
            }

            int sawmillMinX = parkingOnLeft ? horizontalMid + 1 : innerMinX;
            int sawmillMaxX = parkingOnLeft ? innerMaxX : horizontalMid - 1;
            int sawmillMinY = parkingOnBottom ? verticalMid + 1 : innerMinY;
            int sawmillMaxY = parkingOnBottom ? innerMaxY : verticalMid - 1;
            if (!TryPlaceInteriorLocation(candidate, "Sawmill", 2, 2, sawmillMinX, sawmillMaxX, sawmillMinY, sawmillMaxY, true, gridWidth, gridHeight, parkingAnchor))
            {
                continue;
            }

            if (!TryPlaceMotel(candidate, gridWidth, gridHeight))
            {
                continue;
            }

            if (!TryPlaceBusStop(candidate, gridWidth, gridHeight))
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
                0 => new Vector2Int(Random.Range(2, 7), Random.Range(3, 8)),
                1 => new Vector2Int(Random.Range(gridWidth - 7, gridWidth - 2), Random.Range(3, 8)),
                2 => new Vector2Int(Random.Range(2, 7), Random.Range(gridHeight - 8, gridHeight - 3)),
                _ => new Vector2Int(Random.Range(gridWidth - 7, gridWidth - 2), Random.Range(gridHeight - 8, gridHeight - 3))
            };

            if (!TryCreatePlacementFromAnchor(anchor, 3, 2, facing, gridWidth, gridHeight, out WorldLocationPlacement parking) ||
                !PlacementFits(parking, placements.Values, PlacementPadding, gridWidth, gridHeight))
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
        // GasStation can go in any direction from parking, not just "inward"
        WorldPlacementFacing facing = (WorldPlacementFacing)Random.Range(0, 4);

        for (int attempt = 0; attempt < 64; attempt++)
        {
            int dx = Random.Range(-7, 8);
            int dy = Random.Range(-7, 8);
            Vector2Int anchor = parking.Anchor + new Vector2Int(dx, dy);
            if (!TryCreatePlacementFromAnchor(anchor, 2, 2, facing, gridWidth, gridHeight, out WorldLocationPlacement gasStation) ||
                !PlacementFits(gasStation, placements.Values, PlacementPadding, gridWidth, gridHeight))
            {
                continue;
            }

            int distanceToParking = ManhattanDistance(parking.Anchor, gasStation.Anchor);
            if (distanceToParking < 4 || distanceToParking > 12)
            {
                continue;
            }

            placements["GasStation"] = gasStation;
            return true;
        }

        return false;
    }

    private static bool TryPlaceMotel(Dictionary<string, WorldLocationPlacement> placements, int gridWidth, int gridHeight)
    {
        WorldLocationPlacement parking = placements["Parking"];
        WorldPlacementFacing facing = (WorldPlacementFacing)Random.Range(0, 4);

        for (int attempt = 0; attempt < 80; attempt++)
        {
            int dx = Random.Range(-9, 10);
            int dy = Random.Range(-9, 10);
            Vector2Int anchor = parking.Anchor + new Vector2Int(dx, dy);
            if (!TryCreatePlacementFromAnchor(anchor, 2, 2, facing, gridWidth, gridHeight, out WorldLocationPlacement motel) ||
                !PlacementFits(motel, placements.Values, PlacementPadding, gridWidth, gridHeight))
            {
                continue;
            }

            int distanceToParking = ManhattanDistance(parking.Anchor, motel.Anchor);
            if (distanceToParking < 6 || distanceToParking > 14)
            {
                continue;
            }

            placements["Motel"] = motel;
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
                !PlacementFits(placement, placements.Values, PlacementPadding, gridWidth, gridHeight))
            {
                continue;
            }

            if (requireFarFromParking && ManhattanDistance(anchor, parkingAnchor) < FarFromParkingDistance)
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
        return PlacementFits(placement, existingPlacements, padding, gridWidth, gridHeight, false);
    }

    private static bool PlacementFits(
        WorldLocationPlacement placement,
        IEnumerable<WorldLocationPlacement> existingPlacements,
        int padding,
        int gridWidth,
        int gridHeight,
        bool allowBottomRoadAdjacency)
    {
        if (!IsPlacementInsideGrid(placement, gridWidth, gridHeight) ||
            (!allowBottomRoadAdjacency && IsTooCloseToDecorativeBottomRoad(placement)))
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

    private static bool TryPlaceBusStop(Dictionary<string, WorldLocationPlacement> placements, int gridWidth, int gridHeight)
    {
        int centerX = gridWidth / 2;
        int minAnchorX = Mathf.Clamp(centerX - 4, 2, gridWidth - 3);
        int maxAnchorX = Mathf.Clamp(centerX + 4, 2, gridWidth - 3);

        for (int attempt = 0; attempt < 48; attempt++)
        {
            Vector2Int anchor = new(Random.Range(minAnchorX, maxAnchorX + 1), DecorativeBottomRoadOccupiedRows - 1);
            if (!TryCreatePlacementFromAnchor(anchor, 2, 1, WorldPlacementFacing.South, gridWidth, gridHeight, out WorldLocationPlacement busStop) ||
                !PlacementFits(busStop, placements.Values, PlacementPadding, gridWidth, gridHeight, true))
            {
                continue;
            }

            placements["BusStop"] = busStop;
            return true;
        }

        return false;
    }

    private static bool IsPlacementInsideGrid(WorldLocationPlacement placement, int gridWidth, int gridHeight)
    {
        return IsInsideGrid(placement.Min, gridWidth, gridHeight) &&
               IsInsideGrid(placement.Max, gridWidth, gridHeight) &&
               IsInsideGrid(placement.Anchor, gridWidth, gridHeight);
    }

    private static bool IsTooCloseToDecorativeBottomRoad(WorldLocationPlacement placement)
    {
        int firstAllowedY = DecorativeBottomRoadOccupiedRows + DecorativeBottomRoadClearanceRows;
        return placement.Min.y < firstAllowedY || placement.Anchor.y < firstAllowedY;
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
            layout.Sawmill.Anchor,
            layout.BusStop.Anchor
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
            macroCells.Add(new Vector2Int(Mathf.Clamp(anchor.x / 8, 0, 2), Mathf.Clamp(anchor.y / 8, 0, 2)));
        }

        int score = minPairDistance + spanX + spanY + macroCells.Count * 4;
        if (ManhattanDistance(layout.Parking.Anchor, layout.GasStation.Anchor) > 10)
        {
            score -= 6;
        }

        if (spanX < 10 || spanY < 10)
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
            Sawmill = placements["Sawmill"],
            Motel = placements["Motel"],
            BusStop = placements["BusStop"]
        };
    }

    private static GeneratedWorldLayout CreateFallbackLayout()
    {
        return new GeneratedWorldLayout
        {
            Parking = new WorldLocationPlacement { Min = new Vector2Int(2, 4), Max = new Vector2Int(4, 5), Anchor = new Vector2Int(3, 6) },
            GasStation = new WorldLocationPlacement { Min = new Vector2Int(8, 8), Max = new Vector2Int(9, 9), Anchor = new Vector2Int(8, 10) },
            Forest = new WorldLocationPlacement { Min = new Vector2Int(20, 19), Max = new Vector2Int(22, 21), Anchor = new Vector2Int(21, 18) },
            Warehouse = new WorldLocationPlacement { Min = new Vector2Int(21, 10), Max = new Vector2Int(22, 11), Anchor = new Vector2Int(21, 9) },
            Sawmill = new WorldLocationPlacement { Min = new Vector2Int(15, 22), Max = new Vector2Int(16, 23), Anchor = new Vector2Int(15, 21) },
            Motel = new WorldLocationPlacement { Min = new Vector2Int(11, 11), Max = new Vector2Int(12, 12), Anchor = new Vector2Int(11, 10) },
            BusStop = new WorldLocationPlacement { Min = new Vector2Int(12, 2), Max = new Vector2Int(13, 2), Anchor = new Vector2Int(13, 1) }
        };
    }
}
