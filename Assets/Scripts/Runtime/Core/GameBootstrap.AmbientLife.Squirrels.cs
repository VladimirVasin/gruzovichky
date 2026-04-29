using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private int FindNextSquirrelRoamPoint(AmbientSquirrelData sq)
    {
        int current = sq?.CurrentPointIndex ?? -1;
        if (ambientSquirrelRoamPoints.Count < 2 || current < 0 || current >= ambientSquirrelRoamPoints.Count)
        {
            return -1;
        }

        List<int> candidates = new();
        Vector3 currentPos = ambientSquirrelRoamPoints[current];
        for (int i = 0; i < ambientSquirrelRoamPoints.Count; i++)
        {
            if (i == current)
            {
                continue;
            }

            float dist = Vector3.Distance(currentPos, ambientSquirrelRoamPoints[i]);
            if (dist >= 1.5f && dist <= 8f)
            {
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0)
        {
            return (current + 1) % ambientSquirrelRoamPoints.Count;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private int FindNearestSquirrelRoamPoint(Vector3 position)
    {
        if (ambientSquirrelRoamPoints.Count == 0)
        {
            return -1;
        }

        int bestIndex = 0;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < ambientSquirrelRoamPoints.Count; i++)
        {
            float distance = (ambientSquirrelRoamPoints[i] - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
}
