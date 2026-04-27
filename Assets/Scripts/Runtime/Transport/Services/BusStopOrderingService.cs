using System.Collections.Generic;
using UnityEngine;

public readonly struct BusStopOrderKey
{
    public readonly int OriginalIndex;
    public readonly int StopNumber;
    public readonly Vector2Int Anchor;

    public BusStopOrderKey(int originalIndex, int stopNumber, Vector2Int anchor)
    {
        OriginalIndex = originalIndex;
        StopNumber = stopNumber;
        Anchor = anchor;
    }
}

public static class BusStopOrderingService
{
    public static List<int> GetOrderedIndices(IReadOnlyList<BusStopOrderKey> stops)
    {
        List<int> indices = new();
        if (stops == null)
        {
            return indices;
        }

        for (int i = 0; i < stops.Count; i++)
        {
            indices.Add(i);
        }

        indices.Sort((leftIndex, rightIndex) =>
        {
            BusStopOrderKey left = stops[leftIndex];
            BusStopOrderKey right = stops[rightIndex];
            int numberCompare = left.StopNumber.CompareTo(right.StopNumber);
            if (numberCompare != 0)
            {
                return numberCompare;
            }

            int xCompare = left.Anchor.x.CompareTo(right.Anchor.x);
            if (xCompare != 0)
            {
                return xCompare;
            }

            int yCompare = left.Anchor.y.CompareTo(right.Anchor.y);
            if (yCompare != 0)
            {
                return yCompare;
            }

            return left.OriginalIndex.CompareTo(right.OriginalIndex);
        });

        return indices;
    }
}
