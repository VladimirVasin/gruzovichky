using System;
using System.Collections.Generic;
using UnityEngine;

public static class RoadMarkingPlanner
{
    public static bool TryGetRoadVisualAxis(Vector2Int cell, Func<Vector2Int, Vector2Int, bool> connectsToRoadOrAnchor, out bool isHorizontal)
    {
        bool east = connectsToRoadOrAnchor != null && connectsToRoadOrAnchor(cell, Vector2Int.right);
        bool west = connectsToRoadOrAnchor != null && connectsToRoadOrAnchor(cell, Vector2Int.left);
        bool north = connectsToRoadOrAnchor != null && connectsToRoadOrAnchor(cell, Vector2Int.up);
        bool south = connectsToRoadOrAnchor != null && connectsToRoadOrAnchor(cell, Vector2Int.down);

        bool fullHorizontal = east && west;
        bool fullVertical = north && south;

        if (fullHorizontal && !fullVertical)
        {
            isHorizontal = true;
            return true;
        }

        if (fullVertical && !fullHorizontal)
        {
            isHorizontal = false;
            return true;
        }

        bool oneSidedHorizontal = (east || west) && !fullVertical;
        bool oneSidedVertical = (north || south) && !fullHorizontal;
        if (oneSidedHorizontal && !oneSidedVertical)
        {
            isHorizontal = true;
            return true;
        }

        if (oneSidedVertical && !oneSidedHorizontal)
        {
            isHorizontal = false;
            return true;
        }

        isHorizontal = true;
        return false;
    }

    public static bool ShouldDrawTwoCellCenterDash(
        Vector2Int cell,
        bool isHorizontal,
        ISet<Vector2Int> roadCells,
        Func<Vector2Int, Vector2Int, bool> connectsToRoadOrAnchor)
    {
        if (roadCells == null)
        {
            return false;
        }

        if (isHorizontal)
        {
            Vector2Int north = cell + Vector2Int.up;
            return roadCells.Contains(north) &&
                   TryGetRoadVisualAxis(cell, connectsToRoadOrAnchor, out bool cellHorizontal) &&
                   cellHorizontal &&
                   TryGetRoadVisualAxis(north, connectsToRoadOrAnchor, out bool northHorizontal) &&
                   northHorizontal;
        }

        Vector2Int east = cell + Vector2Int.right;
        return roadCells.Contains(east) &&
               TryGetRoadVisualAxis(cell, connectsToRoadOrAnchor, out bool cellIsHorizontal) &&
               !cellIsHorizontal &&
               TryGetRoadVisualAxis(east, connectsToRoadOrAnchor, out bool eastIsHorizontal) &&
               !eastIsHorizontal;
    }
}
