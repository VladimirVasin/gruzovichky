using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private static string FormatCellList(List<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0)
        {
            return "none";
        }

        System.Text.StringBuilder sb = new();
        for (int i = 0; i < cells.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append('(').Append(cells[i].x).Append(',').Append(cells[i].y).Append(')');
        }

        return sb.ToString();
    }

    private static string FormatStringList(List<string> values)
    {
        return values == null || values.Count == 0 ? "none" : string.Join(", ", values);
    }

    private static void AddUniqueDebugValue(List<string> values, string value)
    {
        if (values == null || string.IsNullOrEmpty(value) || values.Contains(value))
        {
            return;
        }

        values.Add(value);
    }

    private static string FormatRoadLanePairId(Vector2Int anchorCell, Vector2Int widthOffset, Vector2Int direction)
    {
        Vector2Int sideCell = anchorCell + widthOffset;
        int minX = Mathf.Min(anchorCell.x, sideCell.x);
        int maxX = Mathf.Max(anchorCell.x, sideCell.x);
        int minY = Mathf.Min(anchorCell.y, sideCell.y);
        int maxY = Mathf.Max(anchorCell.y, sideCell.y);
        return $"({minX},{minY})-({maxX},{maxY})/dir=({direction.x},{direction.y})";
    }

    private static string FormatCell(Vector2Int cell)
    {
        return $"({cell.x},{cell.y})";
    }
}
