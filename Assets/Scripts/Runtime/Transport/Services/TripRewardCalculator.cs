using UnityEngine;

public static class TripRewardCalculator
{
    public static int Calculate(int totalSteps, int handlingBonus, int locationBonus, int minimumReward = 18)
    {
        return Mathf.Max(minimumReward, totalSteps * 3 + handlingBonus + locationBonus);
    }
}
