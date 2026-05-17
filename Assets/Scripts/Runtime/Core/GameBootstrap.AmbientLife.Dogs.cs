using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap : MonoBehaviour
{
    private const string AmbientDogModelResourcePath = "Misc/dog";
    private const float AmbientDogImportedTargetWidth = 0.42f;
    private const float AmbientDogImportedTargetHeight = 0.48f;
    private const float AmbientDogImportedTargetLength = 0.78f;
    private const float AmbientDogWalkSpeed = 1.38f;

    private void SetupAmbientDogs()
    {
        ambientDogs.Clear();
        ambientDogRoamPoints.Clear();
        if (ambientDogRoot != null)
        {
            Destroy(ambientDogRoot.gameObject);
        }

        if (worldRoot == null)
        {
            return;
        }

        RegisterAmbientDogRoamPoints();
        if (ambientDogRoamPoints.Count == 0)
        {
            return;
        }

        ambientDogRoot = new GameObject("AmbientDogs").transform;
        ambientDogRoot.SetParent(worldRoot, false);

        int dogCount = Mathf.Min(AmbientDogCount, ambientDogRoamPoints.Count);
        for (int i = 0; i < dogCount; i++)
        {
            CreateAmbientDog(i, dogCount);
        }
    }

    private void RegisterAmbientDogRoamPoints()
    {
        ambientDogRoamPoints.Clear();
        for (int x = 1; x < GridWidth - 1; x++)
        {
            for (int y = 1; y < GridHeight - 1; y++)
            {
                Vector2Int cell = new(x, y);
                if (!IsAmbientDogTargetCell(cell) || !ShouldRegisterAmbientDogRoamCell(cell))
                {
                    continue;
                }

                Vector3 point = GetCellCenter(cell);
                point.y = SampleTerrainHeight(point.x, point.z);
                ambientDogRoamPoints.Add(point);
            }
        }
    }

    private bool ShouldRegisterAmbientDogRoamCell(Vector2Int cell)
    {
        if (roadCells.Contains(cell) ||
            IsVisibleFootpathCell(cell) ||
            IsCityParkFootpathCell(cell) ||
            IsAmbientDogNearLocation(cell, 4))
        {
            return true;
        }

        int hash = Mathf.Abs(cell.x * 73856093 ^ cell.y * 19349663);
        return hash % 11 == 0;
    }

    private bool IsAmbientDogNearLocation(Vector2Int cell, int radius)
    {
        foreach (LocationData location in locations.Values)
        {
            if (IsAmbientDogNearLocation(location, cell, radius))
            {
                return true;
            }
        }

        for (int i = 0; i < extraServiceLocations.Count; i++)
        {
            if (IsAmbientDogNearLocation(extraServiceLocations[i], cell, radius))
            {
                return true;
            }
        }

        for (int i = 0; i < personalHouses.Count; i++)
        {
            if (IsAmbientDogNearLocation(personalHouses[i], cell, radius))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAmbientDogNearLocation(LocationData location, Vector2Int cell, int radius)
    {
        if (location == null)
        {
            return false;
        }

        return cell.x >= location.Min.x - radius &&
               cell.x <= location.Max.x + radius &&
               cell.y >= location.Min.y - radius &&
               cell.y <= location.Max.y + radius;
    }

    private void CreateAmbientDog(int dogIndex, int totalCount)
    {
        if (ambientDogRoot == null || ambientDogRoamPoints.Count == 0)
        {
            return;
        }

        GameObject dogRoot = new($"AmbientDog_{dogIndex + 1}");
        dogRoot.transform.SetParent(ambientDogRoot, false);

        bool usesImportedModel = TryCreateImportedAmbientDogModel(
            dogRoot.transform,
            dogIndex,
            out Transform bodyTransform,
            out Transform headTransform,
            out Transform tailTransform,
            out Vector3 bodyBaseScale,
            out Quaternion headBaseRotation,
            out Quaternion tailBaseRotation,
            out Transform[] legTransforms,
            out Quaternion[] legBaseRotations);

        if (!usesImportedModel)
        {
            CreateProceduralAmbientDogModel(
                dogRoot.transform,
                dogIndex,
                out bodyTransform,
                out headTransform,
                out tailTransform,
                out bodyBaseScale,
                out headBaseRotation,
                out tailBaseRotation,
                out legTransforms,
                out legBaseRotations);
        }

        int step = Mathf.Max(1, ambientDogRoamPoints.Count / Mathf.Max(1, totalCount));
        int pointIndex = dogIndex * step % ambientDogRoamPoints.Count;
        Vector3 position = ambientDogRoamPoints[pointIndex];
        float yaw = Random.Range(0f, 360f);
        dogRoot.transform.position = position;
        dogRoot.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        AmbientDogData dog = new()
        {
            RootTransform = dogRoot.transform,
            BodyTransform = bodyTransform,
            HeadTransform = headTransform,
            TailTransform = tailTransform,
            UsesImportedModel = usesImportedModel,
            BodyBaseScale = bodyBaseScale,
            HeadBaseRotation = headBaseRotation,
            TailBaseRotation = tailBaseRotation,
            EarTransforms = System.Array.Empty<Transform>(),
            EarBaseRotations = System.Array.Empty<Quaternion>(),
            LegTransforms = legTransforms,
            LegBaseRotations = legBaseRotations,
            CurrentPosition = position,
            StateTimer = Random.Range(1.2f, 3.8f),
            AnimationPhase = Random.Range(0f, 10f),
            TailPhase = Random.Range(0f, 10f),
            Yaw = yaw,
            LastTargetPointIndex = pointIndex,
            ScratchLeftSide = dogIndex % 2 == 0,
            State = PickAmbientDogIdleState()
        };
        ambientDogs.Add(dog);
    }

    private void UpdateAmbientDogs()
    {
        if (ambientDogs.Count == 0 || ambientDogRoamPoints.Count == 0)
        {
            return;
        }

        float dt = Time.deltaTime * gameSpeedMultiplier;
        float time = Time.time;
        for (int i = ambientDogs.Count - 1; i >= 0; i--)
        {
            AmbientDogData dog = ambientDogs[i];
            if (dog.RootTransform == null)
            {
                ambientDogs.RemoveAt(i);
                continue;
            }

            if (!IsAmbientDogTargetCell(WorldToCell(dog.CurrentPosition)))
            {
                MoveAmbientDogToNearestSafeCell(dog);
            }

            if (dog.State == AmbientDogState.Wandering)
            {
                UpdateAmbientDogWandering(dog, dt, time);
            }
            else
            {
                UpdateAmbientDogIdle(dog, dt, time);
            }
        }
    }

    private void UpdateAmbientDogWandering(AmbientDogData dog, float dt, float time)
    {
        if (dog.WalkPath.Count == 0 || dog.WalkWaypointIndex >= dog.WalkPath.Count)
        {
            StartAmbientDogIdle(dog, PickAmbientDogIdleState());
            return;
        }

        Vector3 current = dog.CurrentPosition;
        Vector3 target = dog.WalkPath[dog.WalkWaypointIndex];
        target.y = SampleTerrainHeight(target.x, target.z);
        Vector3 toTarget = target - current;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;
        if (distance <= 0.04f)
        {
            dog.CurrentPosition = target;
            dog.WalkWaypointIndex++;
            if (dog.WalkWaypointIndex >= dog.WalkPath.Count)
            {
                dog.Yaw = dog.RootTransform.eulerAngles.y;
                StartAmbientDogIdle(dog, PickAmbientDogIdleState());
            }
            return;
        }

        Vector3 direction = toTarget / Mathf.Max(distance, 0.001f);
        float step = AmbientDogWalkSpeed * dt;
        Vector3 next = current + direction * Mathf.Min(step, distance);
        if (!IsAmbientDogWalkableCell(WorldToCell(next), WorldToCell(current), WorldToCell(target)))
        {
            StartAmbientDogIdle(dog, AmbientDogState.Sniffing);
            return;
        }

        next.y = SampleTerrainHeight(next.x, next.z);
        dog.CurrentPosition = next;
        dog.RootTransform.position = next + new Vector3(0f, Mathf.Abs(Mathf.Sin(time * 9.5f + dog.AnimationPhase)) * 0.035f, 0f);
        dog.RootTransform.rotation = Quaternion.Slerp(
            dog.RootTransform.rotation,
            Quaternion.LookRotation(direction, Vector3.up),
            10f * Time.deltaTime);

        ApplyDogBodyScale(
            dog,
            new Vector3(0.20f, 0.34f, 0.20f),
            new Vector3(
                1.02f + Mathf.Sin(time * 9.5f + dog.AnimationPhase) * 0.018f,
                0.98f + Mathf.Abs(Mathf.Sin(time * 9.5f + dog.AnimationPhase)) * 0.055f,
                1.01f));
        ApplyDogHeadMotion(dog, Quaternion.Euler(Mathf.Sin(time * 10f + dog.AnimationPhase) * 5f, 0f, 0f));
        ApplyDogTailMotion(
            dog,
            Quaternion.Euler(0f, Mathf.Sin(time * 8.5f + dog.TailPhase) * 24f, Mathf.Sin(time * 6f + dog.TailPhase) * 8f),
            Quaternion.Euler(-48f, Mathf.Sin(time * 8.5f + dog.TailPhase) * 18f, 0f));
        AnimateDogLegs(dog, time, 10.5f, 25f, 0f);
    }

    private void UpdateAmbientDogIdle(AmbientDogData dog, float dt, float time)
    {
        dog.StateTimer -= dt;
        Vector3 position = dog.CurrentPosition;
        position.y = SampleTerrainHeight(position.x, position.z);
        float bob = 0f;
        if (dog.State == AmbientDogState.Barking)
        {
            bob = Mathf.Abs(Mathf.Sin(time * 8f + dog.AnimationPhase)) * 0.035f;
        }
        else if (dog.State == AmbientDogState.PlayHop)
        {
            bob = Mathf.Abs(Mathf.Sin(time * 7.5f + dog.AnimationPhase)) * 0.075f;
        }
        else if (dog.State != AmbientDogState.Lying)
        {
            bob = Mathf.Sin(time * 1.8f + dog.AnimationPhase) * 0.008f;
        }

        dog.RootTransform.position = position + new Vector3(0f, bob, 0f);
        dog.RootTransform.rotation = Quaternion.Slerp(
            dog.RootTransform.rotation,
            Quaternion.Euler(0f, dog.Yaw, 0f),
            5f * Time.deltaTime);

        ApplyAmbientDogIdlePose(dog, time);

        if (dog.StateTimer > 0f)
        {
            return;
        }

        if (!StartAmbientDogWander(dog))
        {
            StartAmbientDogIdle(dog, PickAmbientDogIdleState());
        }
    }

    private void ApplyAmbientDogIdlePose(AmbientDogData dog, float time)
    {
        switch (dog.State)
        {
            case AmbientDogState.Sniffing:
                ApplyDogBodyScale(dog, new Vector3(0.21f, 0.31f, 0.22f), new Vector3(1.04f, 0.92f, 1.08f));
                ApplyDogHeadMotion(dog, Quaternion.Euler(26f + Mathf.Sin(time * 5.8f + dog.AnimationPhase) * 10f, 0f, 0f));
                ApplyDogTailMotion(dog, Quaternion.Euler(-5f, Mathf.Sin(time * 2.5f + dog.TailPhase) * 10f, 8f), Quaternion.Euler(-62f, Mathf.Sin(time * 2.5f + dog.TailPhase) * 8f, 0f));
                AnimateDogLegs(dog, time, 4f, 3f, -8f);
                break;

            case AmbientDogState.Sitting:
                ApplyDogBodyScale(dog, new Vector3(0.20f, 0.29f, 0.23f), new Vector3(0.98f, 0.96f, 1.08f));
                ApplyDogHeadMotion(dog, Quaternion.Euler(-4f, Mathf.Sin(time * 1.2f + dog.AnimationPhase) * 13f, 0f));
                ApplyDogTailMotion(dog, Quaternion.Euler(2f, Mathf.Sin(time * 4.2f + dog.TailPhase) * 22f, 8f), Quaternion.Euler(-64f, Mathf.Sin(time * 4.2f + dog.TailPhase) * 16f, 0f));
                AnimateDogLegs(dog, time, 2.2f, 2f, -16f);
                break;

            case AmbientDogState.Lying:
                ApplyDogBodyScale(dog, new Vector3(0.25f, 0.23f, 0.25f), new Vector3(1.12f, 0.82f, 1.12f));
                ApplyDogHeadMotion(dog, Quaternion.Euler(12f, Mathf.Sin(time * 0.8f + dog.AnimationPhase) * 6f, 0f));
                ApplyDogTailMotion(dog, Quaternion.Euler(-8f, Mathf.Sin(time * 1.2f + dog.TailPhase) * 5f, 0f), Quaternion.Euler(-74f, 0f, 0f));
                AnimateDogLegs(dog, time, 1.5f, 1f, -22f);
                break;

            case AmbientDogState.Barking:
                ApplyDogBodyScale(dog, new Vector3(0.20f, 0.35f, 0.20f), new Vector3(1.02f, 1.06f, 0.98f));
                ApplyDogHeadMotion(dog, Quaternion.Euler(-16f + Mathf.Abs(Mathf.Sin(time * 8f + dog.AnimationPhase)) * 18f, 0f, 0f));
                ApplyDogTailMotion(dog, Quaternion.Euler(0f, Mathf.Sin(time * 10f + dog.TailPhase) * 26f, 10f), Quaternion.Euler(-42f, Mathf.Sin(time * 10f + dog.TailPhase) * 18f, 0f));
                AnimateDogLegs(dog, time, 7f, 5f, -4f);
                break;

            case AmbientDogState.Scratching:
                ApplyDogBodyScale(dog, new Vector3(0.20f, 0.31f, 0.22f), new Vector3(1.02f, 0.92f, 1.06f));
                ApplyDogHeadMotion(dog, Quaternion.Euler(4f, dog.ScratchLeftSide ? -18f : 18f, 0f));
                ApplyDogTailMotion(dog, Quaternion.Euler(-4f, Mathf.Sin(time * 5f + dog.TailPhase) * 16f, 6f), Quaternion.Euler(-54f, Mathf.Sin(time * 5f + dog.TailPhase) * 12f, 0f));
                AnimateAmbientDogScratch(dog, time);
                break;

            case AmbientDogState.PlayHop:
                ApplyDogBodyScale(dog, new Vector3(0.20f, 0.34f, 0.20f), new Vector3(1.04f, 1.02f, 0.98f));
                ApplyDogHeadMotion(dog, Quaternion.Euler(Mathf.Sin(time * 7.5f + dog.AnimationPhase) * 8f, Mathf.Sin(time * 2.5f + dog.AnimationPhase) * 12f, 0f));
                ApplyDogTailMotion(dog, Quaternion.Euler(4f, Mathf.Sin(time * 12f + dog.TailPhase) * 32f, 12f), Quaternion.Euler(-36f, Mathf.Sin(time * 12f + dog.TailPhase) * 22f, 0f));
                AnimateDogLegs(dog, time, 12f, 18f, -4f);
                break;
        }
    }

    private static void AnimateAmbientDogScratch(AmbientDogData dog, float time)
    {
        if (dog?.LegTransforms == null || dog.LegBaseRotations == null)
        {
            return;
        }

        int count = Mathf.Min(dog.LegTransforms.Length, dog.LegBaseRotations.Length);
        int scratchIndex = dog.ScratchLeftSide ? 2 : 3;
        for (int i = 0; i < count; i++)
        {
            Transform leg = dog.LegTransforms[i];
            if (leg == null)
            {
                continue;
            }

            if (i == scratchIndex)
            {
                float kick = Mathf.Sin(time * 15f + dog.AnimationPhase) * 28f;
                float side = dog.ScratchLeftSide ? -18f : 18f;
                leg.localRotation = dog.LegBaseRotations[i] * Quaternion.Euler(-8f + kick, side, 0f);
            }
            else
            {
                leg.localRotation = dog.LegBaseRotations[i] * Quaternion.Euler(-10f, 0f, 0f);
            }
        }
    }

    private bool StartAmbientDogWander(AmbientDogData dog)
    {
        if (dog == null)
        {
            return false;
        }

        if (ambientDogRoamPoints.Count < 8)
        {
            RegisterAmbientDogRoamPoints();
        }

        Vector2Int startCell = WorldToCell(dog.CurrentPosition);
        if (!IsAmbientDogTargetCell(startCell) && !TryFindNearestAmbientDogSafeCell(startCell, out startCell))
        {
            return false;
        }

        bool longWalk = Random.value < 0.24f;
        float minDistance = longWalk ? 14f : 4f;
        float maxDistance = longWalk ? 64f : 24f;
        for (int attempt = 0; attempt < 36; attempt++)
        {
            int targetIndex = Random.Range(0, ambientDogRoamPoints.Count);
            if (targetIndex == dog.LastTargetPointIndex)
            {
                continue;
            }

            Vector3 target = ambientDogRoamPoints[targetIndex];
            Vector2Int targetCell = WorldToCell(target);
            if (!IsAmbientDogTargetCell(targetCell) || IsAmbientDogPositionCrowded(dog, target, 0.72f))
            {
                continue;
            }

            float distance = Vector3.Distance(dog.CurrentPosition, target);
            if (distance < minDistance || distance > maxDistance)
            {
                continue;
            }

            List<Vector2Int> path = FindAmbientDogPath(startCell, targetCell);
            if (path == null || path.Count <= 1)
            {
                continue;
            }

            dog.WalkPath.Clear();
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 waypoint = GetCellCenter(path[i]);
                waypoint.y = SampleTerrainHeight(waypoint.x, waypoint.z);
                dog.WalkPath.Add(waypoint);
            }

            dog.WalkWaypointIndex = 0;
            dog.LastTargetPointIndex = targetIndex;
            dog.State = AmbientDogState.Wandering;
            dog.StateTimer = 0f;
            return true;
        }

        return false;
    }

    private void StartAmbientDogIdle(AmbientDogData dog, AmbientDogState state)
    {
        dog.WalkPath.Clear();
        dog.WalkWaypointIndex = 0;
        dog.State = state;
        dog.Yaw = dog.RootTransform != null ? dog.RootTransform.eulerAngles.y : dog.Yaw;
        dog.StateTimer = state switch
        {
            AmbientDogState.Sniffing => Random.Range(2.0f, 4.8f),
            AmbientDogState.Sitting => Random.Range(3.2f, 7.5f),
            AmbientDogState.Lying => Random.Range(5.0f, 10.5f),
            AmbientDogState.Barking => Random.Range(1.0f, 2.2f),
            AmbientDogState.Scratching => Random.Range(1.3f, 2.6f),
            AmbientDogState.PlayHop => Random.Range(1.4f, 2.8f),
            _ => Random.Range(2.0f, 5.0f)
        };
        dog.ScratchLeftSide = Random.value < 0.5f;
    }

    private AmbientDogState PickAmbientDogIdleState()
    {
        int hour = GetCurrentHour();
        if (hour >= 22 || hour < 6)
        {
            return Random.value < 0.72f ? AmbientDogState.Lying : AmbientDogState.Sitting;
        }

        float roll = Random.value;
        if (roll < 0.32f) return AmbientDogState.Sniffing;
        if (roll < 0.52f) return AmbientDogState.Sitting;
        if (roll < 0.67f) return AmbientDogState.Barking;
        if (roll < 0.82f) return AmbientDogState.Scratching;
        if (roll < 0.94f) return AmbientDogState.PlayHop;
        return AmbientDogState.Lying;
    }

    private List<Vector2Int> FindAmbientDogPath(Vector2Int start, Vector2Int goal)
    {
        return GridPathService.FindWeightedPath(
            start,
            goal,
            GridPathService.GetCardinalNeighbors,
            neighbor => IsAmbientDogWalkableCell(neighbor, start, goal),
            neighbor => GetAmbientDogWalkCellCost(neighbor, start, goal));
    }

    private bool IsAmbientDogTargetCell(Vector2Int cell)
    {
        if (!IsInsideGrid(cell) ||
            waterCells.Contains(cell) ||
            edgeHighwayCells.Contains(cell) ||
            IsBuildingWalkBufferCell(cell))
        {
            return false;
        }

        return !IsLocationCell(cell) || IsCityParkFootpathCell(cell);
    }

    private bool IsAmbientDogWalkableCell(Vector2Int cell, Vector2Int start, Vector2Int goal)
    {
        if (!IsInsideGrid(cell) || waterCells.Contains(cell) || edgeHighwayCells.Contains(cell))
        {
            return false;
        }

        if (cell == start)
        {
            return true;
        }

        if (IsBuildingWalkBufferCell(cell))
        {
            return false;
        }

        if (cell == goal)
        {
            return IsAmbientDogTargetCell(cell);
        }

        if (IsCityParkFootpathCell(cell))
        {
            return true;
        }

        return !IsLocationCell(cell);
    }

    private float GetAmbientDogWalkCellCost(Vector2Int cell, Vector2Int start, Vector2Int goal)
    {
        if (cell == start || cell == goal)
        {
            return 1f;
        }

        float cost = 1f;
        if (IsVisibleFootpathCell(cell))
        {
            cost = 0.62f;
        }
        else if (IsCityParkFootpathCell(cell))
        {
            cost = 0.76f;
        }
        else if (roadCells.Contains(cell))
        {
            cost = 1.18f;
        }

        return cost + GetAmbientDogCrowdingCost(cell);
    }

    private float GetAmbientDogCrowdingCost(Vector2Int cell)
    {
        float cost = 0f;
        for (int i = 0; i < ambientDogs.Count; i++)
        {
            AmbientDogData other = ambientDogs[i];
            if (other == null || other.RootTransform == null)
            {
                continue;
            }

            Vector2Int otherCell = WorldToCell(other.CurrentPosition);
            if (otherCell == cell)
            {
                cost += 1.8f;
            }
            else if (Mathf.Abs(otherCell.x - cell.x) + Mathf.Abs(otherCell.y - cell.y) == 1)
            {
                cost += 0.35f;
            }
        }

        return Mathf.Min(cost, 2.4f);
    }

    private bool IsAmbientDogPositionCrowded(AmbientDogData currentDog, Vector3 position, float minDistance)
    {
        for (int i = 0; i < ambientDogs.Count; i++)
        {
            AmbientDogData other = ambientDogs[i];
            if (other == null || other == currentDog || other.RootTransform == null)
            {
                continue;
            }

            Vector3 otherPosition = other.CurrentPosition;
            otherPosition.y = position.y;
            if (Vector3.Distance(position, otherPosition) < minDistance)
            {
                return true;
            }
        }

        return false;
    }

    private void MoveAmbientDogToNearestSafeCell(AmbientDogData dog)
    {
        if (dog == null)
        {
            return;
        }

        if (!TryFindNearestAmbientDogSafeCell(WorldToCell(dog.CurrentPosition), out Vector2Int safeCell))
        {
            return;
        }

        Vector3 position = GetCellCenter(safeCell);
        position.y = SampleTerrainHeight(position.x, position.z);
        dog.CurrentPosition = position;
        dog.RootTransform.position = position;
        dog.WalkPath.Clear();
        dog.WalkWaypointIndex = 0;
        dog.State = AmbientDogState.Sniffing;
        dog.StateTimer = Random.Range(1.0f, 2.5f);
    }

    private bool TryFindNearestAmbientDogSafeCell(Vector2Int origin, out Vector2Int safeCell)
    {
        const int maxRadius = 18;
        if (IsAmbientDogTargetCell(origin))
        {
            safeCell = origin;
            return true;
        }

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) != radius)
                    {
                        continue;
                    }

                    Vector2Int candidate = origin + new Vector2Int(dx, dy);
                    if (IsAmbientDogTargetCell(candidate))
                    {
                        safeCell = candidate;
                        return true;
                    }
                }
            }
        }

        safeCell = default;
        return false;
    }
}
