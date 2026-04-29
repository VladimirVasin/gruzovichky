using UnityEngine;

public partial class GameBootstrap
{
    private bool TryShowBeeEasterEggForCell(Vector2Int cell)
    {
        if (isTutorialOpen || !IsBeeEasterEggDaytime() || !IsFlowerBeeCell(cell))
        {
            return false;
        }

        ShowBeeEasterEggHud();
        SessionDebugLogger.Log("TUTORIAL", $"Bee easter egg shown for flower cell {cell.x},{cell.y}.");
        return true;
    }

    private bool IsBeeEasterEggDaytime()
    {
        int hour = GetCurrentHour();
        return hour >= 12 && hour < 18 && AreAmbientBeesActive();
    }

    private bool IsFlowerBeeCell(Vector2Int cell)
    {
        for (int i = 0; i < flowerBeePoints.Count; i++)
        {
            if (WorldToCell(flowerBeePoints[i]) == cell)
            {
                return true;
            }
        }

        return false;
    }
}
