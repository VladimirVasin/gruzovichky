using System.Collections.Generic;
using UnityEngine;

public static class WorldLayoutGenerator
{
    public const int ParkingFootprintWidth = 5;
    public const int ParkingFootprintHeight = 4;

    private const int PlacementPadding = 2;
    private const int FarFromParkingDistance = 8;
    private const int DecorativeBottomRoadOccupiedRows = 2;
    private const int DecorativeBottomRoadClearanceRows = 2;
    private const int LayoutGenerationAttempts = 180;

    public static GeneratedWorldLayout Generate(int gridWidth, int gridHeight, HashSet<Vector2Int> blockedCells = null, System.Func<GeneratedWorldLayout, bool> isValidLayout = null)
    {
        blockedCells ??= new HashSet<Vector2Int>();
        GeneratedWorldLayout bestCandidate = null;
        GeneratedWorldLayout bestValidCandidate = null;
        int bestScore = int.MinValue;
        int bestValidScore = int.MinValue;

        for (int attempt = 0; attempt < LayoutGenerationAttempts; attempt++)
        {
            Dictionary<string, WorldLocationPlacement> candidate = new();
            if (!TryPlaceParking(candidate, gridWidth, gridHeight, blockedCells))
            {
                continue;
            }

            if (!TryPlaceGasStation(candidate, gridWidth, gridHeight, blockedCells))
            {
                continue;
            }

            Vector2Int parkingAnchor = candidate["Parking"].Anchor;
            bool parkingOnLeft = parkingAnchor.x < gridWidth * 0.5f;
            bool parkingOnBottom = parkingAnchor.y < gridHeight * 0.5f;

            int innerMinX = gridWidth / 8;
            int innerMaxX = gridWidth - gridWidth / 8 - 1;
            int innerMinY = gridHeight / 8;
            int innerMaxY = gridHeight - gridHeight / 8 - 1;
            int horizontalMid = gridWidth / 2;
            int verticalMid = gridHeight / 2;

            int warehouseMinX = parkingOnLeft ? horizontalMid + 2 : innerMinX;
            int warehouseMaxX = parkingOnLeft ? innerMaxX : horizontalMid - 2;
            int warehouseMinY = parkingOnBottom ? verticalMid + 1 : innerMinY;
            int warehouseMaxY = parkingOnBottom ? innerMaxY : verticalMid - 1;
            if (!TryPlaceInteriorLocation(candidate, "Warehouse", 2, 2, warehouseMinX, warehouseMaxX, warehouseMinY, warehouseMaxY, true, gridWidth, gridHeight, parkingAnchor, blockedCells))
            {
                continue;
            }

            int forestMinX = parkingOnLeft ? horizontalMid + 1 : innerMinX;
            int forestMaxX = parkingOnLeft ? innerMaxX : horizontalMid - 1;
            int forestMinY = parkingOnBottom ? innerMinY : verticalMid + 1;
            int forestMaxY = parkingOnBottom ? verticalMid - 1 : innerMaxY;
            if (!TryPlaceInteriorLocation(candidate, "Forest", 3, 3, forestMinX, forestMaxX, forestMinY, forestMaxY, true, gridWidth, gridHeight, parkingAnchor, blockedCells))
            {
                continue;
            }

            int sawmillMinX = parkingOnLeft ? horizontalMid + 1 : innerMinX;
            int sawmillMaxX = parkingOnLeft ? innerMaxX : horizontalMid - 1;
            int sawmillMinY = parkingOnBottom ? verticalMid + 1 : innerMinY;
            int sawmillMaxY = parkingOnBottom ? innerMaxY : verticalMid - 1;
            if (!TryPlaceInteriorLocation(candidate, "Sawmill", 2, 2, sawmillMinX, sawmillMaxX, sawmillMinY, sawmillMaxY, true, gridWidth, gridHeight, parkingAnchor, blockedCells))
            {
                continue;
            }

            if (!TryPlaceMotel(candidate, gridWidth, gridHeight, blockedCells))
            {
                continue;
            }

            if (!TryPlaceBusStop(candidate, gridWidth, gridHeight, blockedCells))
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
            if (passesValidation && score > bestValidScore)
            {
                bestValidScore = score;
                bestValidCandidate = layout;
            }

            if (score >= 34 && passesValidation)
            {
                return layout;
            }
        }

        if (bestValidCandidate != null)
        {
            return bestValidCandidate;
        }

        if (bestCandidate != null && (isValidLayout == null || isValidLayout(bestCandidate)))
        {
            return bestCandidate;
        }

        return CreateFallbackLayout(gridWidth, gridHeight);
    }

    private static bool TryPlaceParking(Dictionary<string, WorldLocationPlacement> placements, int gridWidth, int gridHeight, HashSet<Vector2Int> blockedCells)
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

            if (!TryCreatePlacementFromAnchor(anchor, ParkingFootprintWidth, ParkingFootprintHeight, facing, gridWidth, gridHeight, out WorldLocationPlacement parking) ||
                !PlacementFits(parking, placements.Values, PlacementPadding, gridWidth, gridHeight, false, blockedCells))
            {
                continue;
            }

            placements["Parking"] = parking;
            return true;
        }

        return false;
    }

    private static bool TryPlaceGasStation(Dictionary<string, WorldLocationPlacement> placements, int gridWidth, int gridHeight, HashSet<Vector2Int> blockedCells)
    {
        WorldLocationPlacement parking = placements["Parking"];
        WorldPlacementFacing facing = (WorldPlacementFacing)Random.Range(0, 4);

        for (int attempt = 0; attempt < 64; attempt++)
        {
            int dx = Random.Range(-7, 8);
            int dy = Random.Range(-7, 8);
            Vector2Int anchor = parking.Anchor + new Vector2Int(dx, dy);
            if (!TryCreatePlacementFromAnchor(anchor, 2, 2, facing, gridWidth, gridHeight, out WorldLocationPlacement gasStation) ||
                !PlacementFits(gasStation, placements.Values, PlacementPadding, gridWidth, gridHeight, false, blockedCells))
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

    private static bool TryPlaceMotel(Dictionary<string, WorldLocationPlacement> placements, int gridWidth, int gridHeight, HashSet<Vector2Int> blockedCells)
    {
        WorldLocationPlacement parking = placements["Parking"];
        WorldPlacementFacing facing = (WorldPlacementFacing)Random.Range(0, 4);

        for (int attempt = 0; attempt < 80; attempt++)
        {
            int dx = Random.Range(-9, 10);
            int dy = Random.Range(-9, 10);
            Vector2Int anchor = parking.Anchor + new Vector2Int(dx, dy);
            if (!TryCreatePlacementFromAnchor(anchor, 2, 2, facing, gridWidth, gridHeight, out WorldLocationPlacement motel) ||
                !PlacementFits(motel, placements.Values, PlacementPadding, gridWidth, gridHeight, false, blockedCells))
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
        Vector2Int parkingAnchor,
        HashSet<Vector2Int> blockedCells)
    {
        for (int attempt = 0; attempt < 80; attempt++)
        {
            WorldPlacementFacing facing = (WorldPlacementFacing)Random.Range(0, 4);
            Vector2Int anchor = new Vector2Int(Random.Range(minAnchorX, maxAnchorX + 1), Random.Range(minAnchorY, maxAnchorY + 1));
            if (!TryCreatePlacementFromAnchor(anchor, width, height, facing, gridWidth, gridHeight, out WorldLocationPlacement placement) ||
                !PlacementFits(placement, placements.Values, PlacementPadding, gridWidth, gridHeight, false, blockedCells))
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

        placement.RoadAccess = GetRoadAccessCell(placement, facing, gridWidth, gridHeight);
        return IsPlacementInsideGrid(placement, gridWidth, gridHeight) && !placement.Contains(anchor);
    }

    private static Vector2Int GetRoadAccessCell(WorldLocationPlacement placement, WorldPlacementFacing facing, int gridWidth, int gridHeight)
    {
        Vector2Int outward = facing switch
        {
            WorldPlacementFacing.North => Vector2Int.up,
            WorldPlacementFacing.South => Vector2Int.down,
            WorldPlacementFacing.East  => Vector2Int.right,
            _                          => Vector2Int.left
        };

        Vector2Int access = placement.Anchor + outward * 2;
        if (IsInsideGrid(access, gridWidth, gridHeight))
        {
            return access;
        }

        access = placement.Anchor + outward;
        return IsInsideGrid(access, gridWidth, gridHeight) ? access : placement.Anchor;
    }

    private static bool PlacementFits(WorldLocationPlacement placement, IEnumerable<WorldLocationPlacement> existingPlacements, int padding, int gridWidth, int gridHeight)
    {
        return PlacementFits(placement, existingPlacements, padding, gridWidth, gridHeight, false, null);
    }

    private static bool PlacementFits(
        WorldLocationPlacement placement,
        IEnumerable<WorldLocationPlacement> existingPlacements,
        int padding,
        int gridWidth,
        int gridHeight,
        bool allowBottomRoadAdjacency,
        HashSet<Vector2Int> blockedCells)
    {
        if (!IsPlacementInsideGrid(placement, gridWidth, gridHeight) ||
            (!allowBottomRoadAdjacency && IsTooCloseToDecorativeBottomRoad(placement)))
        {
            return false;
        }

        if (blockedCells != null)
        {
            for (int x = placement.Min.x - padding; x <= placement.Max.x + padding; x++)
            {
                for (int y = placement.Min.y - padding; y <= placement.Max.y + padding; y++)
                {
                    if (blockedCells.Contains(new Vector2Int(x, y)))
                    {
                        return false;
                    }
                }
            }

            if (blockedCells.Contains(placement.Anchor))
            {
                return false;
            }

            if (blockedCells.Contains(placement.RoadAccess))
            {
                return false;
            }

        }

        foreach (WorldLocationPlacement existing in existingPlacements)
        {
            if (RectanglesOverlapExpanded(placement, existing, padding) ||
                existing.Contains(placement.Anchor) ||
                existing.Contains(placement.RoadAccess) ||
                placement.Contains(existing.Anchor) ||
                placement.Contains(existing.RoadAccess) ||
                placement.RoadAccess == existing.Anchor ||
                placement.Anchor == existing.Anchor)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryPlaceBusStop(Dictionary<string, WorldLocationPlacement> placements, int gridWidth, int gridHeight, HashSet<Vector2Int> blockedCells)
    {
        int centerX = gridWidth / 2;
        int minAnchorX = Mathf.Clamp(centerX - 4, 2, gridWidth - 3);
        int maxAnchorX = Mathf.Clamp(centerX + 4, 2, gridWidth - 3);

        for (int attempt = 0; attempt < 48; attempt++)
        {
            Vector2Int anchor = new(Random.Range(minAnchorX, maxAnchorX + 1), DecorativeBottomRoadOccupiedRows);
            if (!TryCreateBusStopPlacement(anchor, gridWidth, gridHeight, out WorldLocationPlacement busStop))
            {
                continue;
            }

            busStop.RoadAccess = GetBusStopRoadAccessCell(busStop, gridWidth, gridHeight);

            if (!PlacementFits(busStop, placements.Values, PlacementPadding, gridWidth, gridHeight, true, blockedCells))
            {
                continue;
            }

            placements["BusStop"] = busStop;
            return true;
        }

        return false;
    }

    private static bool TryCreateBusStopPlacement(Vector2Int anchor, int gridWidth, int gridHeight, out WorldLocationPlacement placement)
    {
        placement = new WorldLocationPlacement
        {
            Anchor = anchor,
            Min = new Vector2Int(anchor.x - 1, anchor.y),
            Max = new Vector2Int(anchor.x, anchor.y)
        };

        placement.RoadAccess = GetBusStopRoadAccessCell(placement, gridWidth, gridHeight);
        return IsPlacementInsideGrid(placement, gridWidth, gridHeight) && !placement.Contains(placement.RoadAccess);
    }

    private static Vector2Int GetBusStopRoadAccessCell(WorldLocationPlacement placement, int gridWidth, int gridHeight)
    {
        Vector2Int rightSideAccess = placement.Anchor + Vector2Int.right * 2;
        if (IsInsideGrid(rightSideAccess, gridWidth, gridHeight) && !placement.Contains(rightSideAccess))
        {
            return rightSideAccess;
        }

        Vector2Int leftSideAccess = placement.Anchor + Vector2Int.left * 2;
        if (IsInsideGrid(leftSideAccess, gridWidth, gridHeight) && !placement.Contains(leftSideAccess))
        {
            return leftSideAccess;
        }

        return placement.Anchor;
    }

    private static bool IsPlacementInsideGrid(WorldLocationPlacement placement, int gridWidth, int gridHeight)
    {
        return IsInsideGrid(placement.Min, gridWidth, gridHeight) &&
               IsInsideGrid(placement.Max, gridWidth, gridHeight) &&
               IsInsideGrid(placement.Anchor, gridWidth, gridHeight) &&
               IsInsideGrid(placement.RoadAccess, gridWidth, gridHeight);
    }

    private static bool IsTooCloseToDecorativeBottomRoad(WorldLocationPlacement placement)
    {
        int firstAllowedY = DecorativeBottomRoadOccupiedRows + DecorativeBottomRoadClearanceRows;
        return placement.Min.y < firstAllowedY ||
               placement.Anchor.y < firstAllowedY ||
               placement.RoadAccess.y < firstAllowedY;
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
            layout.Motel.Anchor,
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
            macroCells.Add(new Vector2Int(Mathf.Clamp(anchor.x / 10, 0, 2), Mathf.Clamp(anchor.y / 10, 0, 2)));
        }

        int score = minPairDistance + spanX + spanY + macroCells.Count * 4;
        if (ManhattanDistance(layout.Parking.Anchor, layout.GasStation.Anchor) > 10)
        {
            score -= 6;
        }

        if (spanX < 14 || spanY < 14)
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

    private static GeneratedWorldLayout CreateFallbackLayout(int gridWidth, int gridHeight)
    {
        TryCreateBusStopPlacement(new Vector2Int(31, 2), gridWidth, gridHeight, out WorldLocationPlacement busStop);

        return new GeneratedWorldLayout
        {
            Parking = CreateFallbackPlacement(new Vector2Int(8, 12), ParkingFootprintWidth, ParkingFootprintHeight, WorldPlacementFacing.North, gridWidth, gridHeight),
            GasStation = CreateFallbackPlacement(new Vector2Int(17, 16), 2, 2, WorldPlacementFacing.North, gridWidth, gridHeight),
            Forest = CreateFallbackPlacement(new Vector2Int(48, 36), 3, 3, WorldPlacementFacing.South, gridWidth, gridHeight),
            Warehouse = CreateFallbackPlacement(new Vector2Int(44, 19), 2, 2, WorldPlacementFacing.South, gridWidth, gridHeight),
            Sawmill = CreateFallbackPlacement(new Vector2Int(34, 41), 2, 2, WorldPlacementFacing.South, gridWidth, gridHeight),
            Motel = CreateFallbackPlacement(new Vector2Int(23, 22), 2, 2, WorldPlacementFacing.South, gridWidth, gridHeight),
            BusStop = busStop
        };
    }

    private static WorldLocationPlacement CreateFallbackPlacement(Vector2Int anchor, int width, int height, WorldPlacementFacing facing, int gridWidth, int gridHeight)
    {
        if (TryCreatePlacementFromAnchor(anchor, width, height, facing, gridWidth, gridHeight, out WorldLocationPlacement placement))
        {
            return placement;
        }

        Vector2Int clampedAnchor = new(Mathf.Clamp(anchor.x, 1, gridWidth - 2), Mathf.Clamp(anchor.y, 1, gridHeight - 2));
        TryCreatePlacementFromAnchor(clampedAnchor, width, height, facing, gridWidth, gridHeight, out placement);
        return placement;
    }
}

