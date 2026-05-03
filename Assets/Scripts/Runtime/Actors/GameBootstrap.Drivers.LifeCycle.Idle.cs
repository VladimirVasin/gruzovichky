using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private void UpdateDriverIdleWander(DriverAgent driver)
    {
        if (driver == null || driver.DriverObject == null)
        {
            return;
        }

        UpdateWorkerLifeCycleDailyState(driver);

        if (driver.IsArrivingByBus ||
            driver.RestPhase != DriverRestPhase.None ||
            driver.IsOnActiveShift ||
            driver.NeedsShiftEndReturn ||
            driver.WaitingForShiftAtParking ||
            GetCurrentTruckForDriver(driver) != null)
        {
            if (IsDriverIdleWanderPhase(driver) || IsDriverInIdleActivity(driver))
            {
                ReleaseBench(driver);
                ReleaseCatInteraction(driver);
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
            }

            driver.IdleConversationTimer = 0f;
            driver.IdleConversationPartnerId = -1;

            LogWorkerDecision(driver, "idle-blocked", "worker is arriving/resting/on shift/returning/waiting/assigned to truck");
            return;
        }

        if (driver.IdleConversationCooldownTimer > 0f)
        {
            driver.IdleConversationCooldownTimer = Mathf.Max(0f, driver.IdleConversationCooldownTimer - Time.deltaTime * gameSpeedMultiplier);
        }

        if (IsDriverBusyWalkPhase(driver))
        {
            return;
        }

        if (IsDriverIdleConversing(driver))
        {
            DriverAgent partner = GetDriverAgentById(driver.IdleConversationPartnerId);
            if (!CanDriverContinueIdleConversation(driver, partner))
            {
                StopDriverIdleConversation(driver, true);
                return;
            }

            driver.IdleConversationTimer -= Time.deltaTime * gameSpeedMultiplier;
            if (driver.IdleConversationTimer <= 0f)
            {
                StopDriverIdleConversation(driver, true);
            }
            return;
        }

        if (IsDriverIdleWanderPhase(driver))
        {
            return;
        }

        if (HasCriticalWorkerNeed(driver) && IsLowPriorityIdleActivity(driver) && TryInterruptIdleForCriticalNeed(driver))
        {
            return;
        }

        // Handle stationary idle activities
        if (driver.WalkPhase == DriverRescuePhase.IdleSittingOnBench ||
            driver.WalkPhase == DriverRescuePhase.IdleAtBar ||
            driver.WalkPhase == DriverRescuePhase.IdleAtCanteen ||
            driver.WalkPhase == DriverRescuePhase.IdleAtTrashCan ||
            driver.WalkPhase == DriverRescuePhase.IdleAtGamblingHall ||
            driver.WalkPhase == DriverRescuePhase.IdleAtCityPark ||
            driver.WalkPhase == DriverRescuePhase.IdleSmoking ||
            driver.WalkPhase == DriverRescuePhase.IdlePhoneCall ||
            driver.WalkPhase == DriverRescuePhase.IdlePettingCat)
        {
            driver.IdleActivityTimer -= Time.deltaTime * gameSpeedMultiplier;
            if (driver.IdleActivityTimer <= 0f)
            {
                WorkerLifeGoal completedGoal = driver.LifeGoal;
                DriverRescuePhase completedPhase = driver.WalkPhase;
                if (driver.WalkPhase == DriverRescuePhase.IdleSittingOnBench)
                {
                    ReleaseBench(driver);
                }
                else if (driver.WalkPhase == DriverRescuePhase.IdleAtCityPark)
                {
                    if (completedGoal == WorkerLifeGoal.Leisure && TryStartCityParkExit(driver, driver.DriverObject.transform.position))
                    {
                        return;
                    }

                    ReleaseBench(driver);
                }

                if (completedPhase == DriverRescuePhase.IdleSmoking)
                {
                    StopDriverSmokingParticles(driver);
                }

                if (completedPhase == DriverRescuePhase.IdlePettingCat)
                {
                    ReleaseCatInteraction(driver);
                }

                driver.WalkPhase = DriverRescuePhase.None;
                if (completedGoal == WorkerLifeGoal.Eat)
                {
                    ResetWorkerNeedTimer(driver, WorkerNeedKind.Meal);
                    driver.AteToday = true;
                    driver.LifeGoal = WorkerLifeGoal.None;
                    ContinueWorkerLifeCycle(driver, driver.DriverObject.transform.position);
                    return;
                }

                if (completedGoal == WorkerLifeGoal.Leisure)
                {
                    ResetWorkerNeedTimer(driver, WorkerNeedKind.Leisure);
                    driver.HadLeisureToday = true;
                    // Fallback: apply gambling money if animation never completed (HUD was closed)
                    if (driver.GamblingMoneyPending)
                    {
                        driver.GamblingMoneyPending = false;
                        int net = driver.GamblingPayout - driver.GamblingBet;
                        driver.Money = Mathf.Max(0, driver.Money + net);
                        SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} gambling fallback applied: net={net:+#;-#;0}, balance=${driver.Money}.");
                    }
                    driver.GamblingBet = 0;
                    driver.GamblingPayout = 0;
                    driver.GamblingMultiplier = 0;
                    driver.GamblingBetCount = 0;
                    driver.GamblerBroke = false;
                    driver.LifeGoal = WorkerLifeGoal.None;
                    ContinueWorkerLifeCycle(driver, driver.DriverObject.transform.position);
                    return;
                }

                if (completedGoal == WorkerLifeGoal.Sleep)
                {
                    ResetWorkerNeedTimer(driver, WorkerNeedKind.Sleep);
                    driver.SleptToday = true;
                    driver.LifeGoal = WorkerLifeGoal.None;
                    ContinueWorkerLifeCycle(driver, driver.DriverObject.transform.position);
                    return;
                }

                driver.IdleWanderPauseTimer = Random.Range(0.5f, 1.5f);
            }

            return;
        }

        if (driver.DutyMode == DriverDutyMode.Logistics &&
            (ShouldLogisticsWorkerHeadToBuilding(driver) || IsProductionWorkHour(GetCurrentHour())))
        {
            LogWorkerDecision(driver, "idle-blocked", "logistics worker should be handled by production/logistics runtime");
            return;
        }

        if (driver.ShiftStartHour >= 0 &&
            (ShouldDriverHeadToShift(driver) || IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour)))
        {
            LogWorkerDecision(driver, "idle-blocked", "worker should prepare for assigned shift");
            return;
        }

        if (TryStartDueWorkerLifeCycle(driver))
        {
            return;
        }

        if (driver.IdleWanderPauseTimer > 0f)
        {
            driver.IdleWanderPauseTimer -= Time.deltaTime * gameSpeedMultiplier;
            return;
        }

        if (TryStartIdleConversation(driver))
        {
            return;
        }

        Vector3 startPosition = driver.DriverObject.transform.position;
        Vector3 targetPosition = FindDriverIdleWanderTarget(driver, startPosition);
        if ((targetPosition - startPosition).sqrMagnitude < 0.04f)
        {
            driver.IdleWanderPointIndex++;
            driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
            return;
        }

        SelectNextIdleActivity(driver, startPosition, targetPosition);
    }

    private void SelectNextIdleActivity(DriverAgent driver, Vector3 startPosition, Vector3 wanderTarget)
    {
        float roll = Random.value;

        if (roll < 0.45f)
        {
            driver.WalkPhase = DriverRescuePhase.IdleWander;
            driver.IdleWanderPointIndex++;
            driver.WalkAnimationTime = 0f;
            BuildDriverWalkPath(driver, startPosition, wanderTarget);
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} started idle walk.");
            LogWorkerDecision(driver, "idle-activity", $"wander roll={roll:0.00}", true);
        }
        else if (roll < 0.75f && TryGetNearestFreeBench(startPosition, 14f, out int bIdx, out Vector3 bPos))
        {
            MarkRoadsideBenchOccupied(bIdx);
            driver.SittingBenchIndex = bIdx;
            driver.IdleActivityTimer = Random.Range(15f, 45f);
            driver.WalkTargetWorld = bPos;
            BuildDriverWalkPath(driver, startPosition, bPos);
            driver.WalkPhase = DriverRescuePhase.IdleWalkToBench;
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} heading to bench {bIdx}.");
            LogWorkerDecision(driver, "idle-activity", $"bench={bIdx}, roll={roll:0.00}", true);
        }
        else if (roll < 0.88f)
        {
            driver.IdleActivityTimer = Random.Range(20f, 45f);
            driver.WalkPhase = DriverRescuePhase.IdleSmoking;
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} started smoking break.");
            LogWorkerDecision(driver, "idle-activity", $"smoking, roll={roll:0.00}", true);
        }
        else if (roll < 0.97f && TryStartWorkerPetCat(driver, startPosition))
        {
            LogWorkerDecision(driver, "idle-activity", $"walking to pet cat, roll={roll:0.00}", true);
        }
        else
        {
            driver.IdleActivityTimer = Random.Range(8f, 25f);
            driver.WalkPhase = DriverRescuePhase.IdlePhoneCall;
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} making a phone call.");
            LogWorkerDecision(driver, "idle-activity", $"phone call, roll={roll:0.00}", true);
        }
    }

    private bool TryStartWorkerPetCat(DriverAgent driver, Vector3 startPosition)
    {
        if (ambientCats.Count == 0 || AreAmbientCatsSleepingNight()) return false;

        const float searchRadiusSqr = 49f; // 7 units
        int bestIndex = -1;
        float bestDistSqr = searchRadiusSqr;

        for (int i = 0; i < ambientCats.Count; i++)
        {
            AmbientCatData cat = ambientCats[i];
            if (cat == null || cat.RootTransform == null || cat.State == AmbientCatState.BeingPetted) continue;

            Vector3 delta = cat.RootTransform.position - startPosition;
            delta.y = 0f;
            float distSqr = delta.sqrMagnitude;
            if (distSqr < bestDistSqr)
            {
                bestDistSqr = distSqr;
                bestIndex = i;
            }
        }

        if (bestIndex < 0) return false;

        AmbientCatData targetCat = ambientCats[bestIndex];
        Vector3 catPos = targetCat.RootTransform.position;
        catPos.y = SampleTerrainHeight(catPos.x, catPos.z);

        driver.IdleCatPetTargetIndex = bestIndex;
        driver.IdleActivityTimer = Random.Range(4f, 8f);
        driver.WalkAnimationTime = 0f;
        driver.WalkPhase = DriverRescuePhase.IdleWalkToCat;
        BuildDriverWalkPath(driver, startPosition, catPos);
        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} heading toward a cat to pet it.");
        return true;
    }

    private void ReleaseCatInteraction(DriverAgent driver)
    {
        if (driver == null) return;

        int idx = driver.IdleCatPetTargetIndex;
        if (idx >= 0 && idx < ambientCats.Count)
        {
            AmbientCatData cat = ambientCats[idx];
            if (cat != null && cat.PettedByDriverId == driver.DriverId)
            {
                cat.State = AmbientCatState.Lazing;
                cat.StateTimer = Random.Range(4f, 8f);
                cat.PettedByDriverId = -1;
            }
        }

        driver.IdleCatPetTargetIndex = -1;
    }

    private Vector3 FindDriverIdleWanderTarget(DriverAgent driver, Vector3 startPosition)
    {
        int nextPointIndex = driver.IdleWanderPointIndex + 1;
        float personalSpaceSqr = DriverIdlePersonalSpace * DriverIdlePersonalSpace;
        for (int attempt = 0; attempt < 16; attempt++)
        {
            int candidateIndex = nextPointIndex + attempt;
            bool preferCityPoint = (candidateIndex + driver.DriverId) % 3 == 0;
            Vector3 candidate = preferCityPoint && TryGetCityIdleWanderTarget(driver, startPosition, candidateIndex, out Vector3 cityTarget)
                ? cityTarget
                : GetDriverIdleMotelWanderPosition(driver.DriverId - 1, candidateIndex);
            Vector3 flatDelta = candidate - startPosition;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude < 0.04f)
            {
                continue;
            }

            bool blockedByOtherDriver = false;
            for (int i = 0; i < driverAgents.Count; i++)
            {
                DriverAgent other = driverAgents[i];
                if (other == null || other == driver || other.DriverObject == null || !other.DriverObject.activeSelf)
                {
                    continue;
                }

                Vector3 otherDelta = other.DriverObject.transform.position - candidate;
                otherDelta.y = 0f;
                if (otherDelta.sqrMagnitude < personalSpaceSqr)
                {
                    blockedByOtherDriver = true;
                    break;
                }
            }

            if (!blockedByOtherDriver)
            {
                return candidate;
            }
        }

        return startPosition;
    }

    private bool IsLowPriorityIdleActivity(DriverAgent driver)
    {
        if (driver == null)
        {
            return false;
        }

        if (driver.LifeGoal == WorkerLifeGoal.Eat ||
            driver.LifeGoal == WorkerLifeGoal.Sleep ||
            driver.LifeGoal == WorkerLifeGoal.Leisure)
        {
            return false;
        }

        return driver.WalkPhase == DriverRescuePhase.IdleSittingOnBench ||
               driver.WalkPhase == DriverRescuePhase.IdleSmoking ||
               driver.WalkPhase == DriverRescuePhase.IdlePhoneCall ||
               driver.WalkPhase == DriverRescuePhase.IdlePettingCat;
    }

    private bool TryInterruptIdleForCriticalNeed(DriverAgent driver)
    {
        if (driver == null)
        {
            return false;
        }

        DriverRescuePhase interruptedPhase = driver.WalkPhase;
        ReleaseBench(driver);
        ReleaseCatInteraction(driver);
        if (interruptedPhase == DriverRescuePhase.IdleSmoking)
        {
            StopDriverSmokingParticles(driver);
        }

        driver.WalkPhase = DriverRescuePhase.None;
        driver.IdleActivityTimer = 0f;
        driver.LifeGoal = WorkerLifeGoal.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        LogWorkerDecision(driver, "idle-interrupted-critical-need", $"phase={interruptedPhase}; needs={FormatWorkerNeedsDebug(driver)}", true);
        return TryStartDueWorkerLifeCycle(driver);
    }

    private bool TryGetCityIdleWanderTarget(DriverAgent driver, Vector3 startPosition, int pointIndex, out Vector3 target)
    {
        target = Vector3.zero;
        if (driver == null)
        {
            return false;
        }

        LocationType[] interestTypes =
        {
            LocationType.CityPark,
            LocationType.Canteen,
            LocationType.Bar,
            LocationType.GamblingHall,
            LocationType.Stop,
            LocationType.Warehouse,
            LocationType.Parking,
            LocationType.IntercityStop
        };

        int startIndex = Mathf.Abs(driver.DriverId * 11 + pointIndex * 5) % interestTypes.Length;
        for (int i = 0; i < interestTypes.Length; i++)
        {
            LocationType type = interestTypes[(startIndex + i) % interestTypes.Length];
            if (!TryGetIdlePointNearLocation(type, driver.DriverId, pointIndex + i, out Vector3 candidate))
            {
                continue;
            }

            Vector3 flatDelta = candidate - startPosition;
            flatDelta.y = 0f;
            if (flatDelta.sqrMagnitude < 1.44f)
            {
                continue;
            }

            target = candidate;
            return true;
        }

        return false;
    }

    private bool TryGetIdlePointNearLocation(LocationType type, int driverId, int pointIndex, out Vector3 target)
    {
        target = Vector3.zero;
        if (!locations.TryGetValue(type, out LocationData location))
        {
            return false;
        }

        Vector3 center = GetLocationCenter(location);
        Vector3 anchor = GetCellCenter(location.Anchor);
        Vector3 outward = anchor - center;
        outward.y = 0f;
        if (outward.sqrMagnitude < 0.0001f)
        {
            outward = Vector3.forward;
        }
        else
        {
            outward.Normalize();
        }

        Vector3 right = new(outward.z, 0f, -outward.x);
        Vector2[] offsets =
        {
            new Vector2(0.35f, 1.25f),
            new Vector2(-0.55f, 1.55f),
            new Vector2(0.95f, 1.80f),
            new Vector2(-1.15f, 1.10f),
            new Vector2(1.35f, 0.72f),
            new Vector2(0.15f, 2.20f)
        };

        int baseIndex = Mathf.Abs(driverId + pointIndex * 3) % offsets.Length;
        for (int attempt = 0; attempt < offsets.Length; attempt++)
        {
            Vector2 offset = offsets[(baseIndex + attempt) % offsets.Length];
            Vector3 candidate = anchor + right * offset.x + outward * offset.y;
            Vector2Int cell = WorldToCell(candidate);
            if (!IsInsideGrid(cell) ||
                roadCells.Contains(cell) ||
                edgeHighwayCells.Contains(cell) ||
                waterCells.Contains(cell) ||
                IsLocationCell(cell))
            {
                continue;
            }

            candidate.y = SampleTerrainHeight(candidate.x, candidate.z);
            target = candidate;
            return true;
        }

        return false;
    }

}
