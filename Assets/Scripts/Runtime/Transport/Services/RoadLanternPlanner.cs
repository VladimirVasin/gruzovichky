using UnityEngine;

public static class RoadLanternPlanner
{
    public static bool TryGetPlacement(
        Vector2Int cell,
        System.Func<Vector2Int, bool> connectsToRoadOrAnchor,
        System.Func<Vector2Int, bool> isLocationCell,
        System.Func<Vector2Int, Vector3> getCellCenter,
        out Vector3 worldPosition,
        out Quaternion worldRotation)
    {
        worldPosition = Vector3.zero;
        worldRotation = Quaternion.identity;

        bool connectLeft = connectsToRoadOrAnchor(cell + new Vector2Int(-1, 0));
        bool connectRight = connectsToRoadOrAnchor(cell + new Vector2Int(1, 0));
        bool connectDown = connectsToRoadOrAnchor(cell + new Vector2Int(0, -1));
        bool connectUp = connectsToRoadOrAnchor(cell + new Vector2Int(0, 1));

        bool horizontalStraight = connectLeft && connectRight && !connectDown && !connectUp;
        bool verticalStraight = connectUp && connectDown && !connectLeft && !connectRight;
        if (!horizontalStraight && !verticalStraight)
        {
            return false;
        }

        if (horizontalStraight && cell.x % 2 != 0)
        {
            return false;
        }

        if (verticalStraight && cell.y % 2 != 0)
        {
            return false;
        }

        Vector3 baseCenter = getCellCenter(cell);
        Vector3 sideOffset = horizontalStraight
            ? new Vector3(0f, 0f, cell.x % 4 == 0 ? 0.44f : -0.44f)
            : new Vector3(cell.y % 4 == 0 ? 0.44f : -0.44f, 0f, 0f);

        Vector2Int sideCell = horizontalStraight
            ? new Vector2Int(cell.x, cell.y + (sideOffset.z > 0f ? 1 : -1))
            : new Vector2Int(cell.x + (sideOffset.x > 0f ? 1 : -1), cell.y);

        if (isLocationCell(sideCell))
        {
            sideOffset *= -1f;
            sideCell = horizontalStraight
                ? new Vector2Int(cell.x, cell.y + (sideOffset.z > 0f ? 1 : -1))
                : new Vector2Int(cell.x + (sideOffset.x > 0f ? 1 : -1), cell.y);

            if (isLocationCell(sideCell))
            {
                return false;
            }
        }

        worldPosition = baseCenter + sideOffset + new Vector3(0f, 0.04f, 0f);
        worldRotation = horizontalStraight ? Quaternion.identity : Quaternion.Euler(0f, 90f, 0f);
        return true;
    }
}
