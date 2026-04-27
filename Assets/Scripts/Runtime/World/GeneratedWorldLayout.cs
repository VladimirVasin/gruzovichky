using UnityEngine;
using System.Collections.Generic;

public sealed class WorldLocationPlacement
{
    public Vector2Int Min;
    public Vector2Int Max;
    public Vector2Int Anchor;
    public Vector2Int RoadAccess;

    public bool Contains(Vector2Int cell)
    {
        return cell.x >= Min.x && cell.x <= Max.x && cell.y >= Min.y && cell.y <= Max.y;
    }
}

public sealed class GeneratedWorldLayout
{
    public WorldLocationPlacement Parking;
    public WorldLocationPlacement GasStation;
    public WorldLocationPlacement Forest;
    public WorldLocationPlacement Warehouse;
    public WorldLocationPlacement Sawmill;
    public WorldLocationPlacement Motel;
    public WorldLocationPlacement BusStop;

    public IEnumerable<WorldLocationPlacement> GetAllPlacements()
    {
        yield return Parking;
        yield return GasStation;
        yield return Forest;
        yield return Warehouse;
        yield return Sawmill;
        yield return Motel;
        yield return BusStop;
    }
}

public enum WorldPlacementFacing
{
    North,
    South,
    East,
    West
}

