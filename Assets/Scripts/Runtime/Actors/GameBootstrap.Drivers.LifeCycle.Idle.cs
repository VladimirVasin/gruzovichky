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
        if (TryRescueDriverFromUnsafeWalkCell(driver, "idle life update"))
        {
            return;
        }

        if (driver.WaitingForShiftAtParking &&
            HasCriticalWorkerNeed(driver) &&
            TryStartDueWorkerLifeCycle(driver))
        {
            return;
        }

        if (driver.IsArrivingByBus ||
            driver.RestPhase != DriverRestPhase.None ||
            driver.IsOnActiveShift ||
            driver.NeedsShiftEndReturn ||
            driver.WaitingForShiftAtParking ||
            GetCurrentTruckForDriver(driver) != null)
        {
            if (IsDriverIdleWanderPhase(driver) || IsDriverInIdleActivity(driver))
            {
                ExitWorkerServiceInterior(driver, driver.WalkPhase);
                ReleaseBench(driver);
                ReleaseCatInteraction(driver);
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.PendingVendorLocationInstanceId = 0;
                driver.PendingVendorItemId = string.Empty;
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
            driver.WalkPhase == DriverRescuePhase.IdleAtKiosk ||
            driver.WalkPhase == DriverRescuePhase.IdleAtPersonalHouseMeal ||
            driver.WalkPhase == DriverRescuePhase.IdleAtTrashCan ||
            driver.WalkPhase == DriverRescuePhase.IdleAtGamblingHall ||
            driver.WalkPhase == DriverRescuePhase.IdleAtCityPark ||
            driver.WalkPhase == DriverRescuePhase.AtLaborExchange ||
            driver.WalkPhase == DriverRescuePhase.IdleSmoking ||
            driver.WalkPhase == DriverRescuePhase.IdlePhoneCall ||
            driver.WalkPhase == DriverRescuePhase.IdlePettingCat)
        {
            driver.IdleActivityTimer -= Time.deltaTime * gameSpeedMultiplier;
            if (driver.IdleActivityTimer <= 0f)
            {
                WorkerLifeGoal completedGoal = driver.LifeGoal;
                DriverRescuePhase completedPhase = driver.WalkPhase;
                Vector3 completionPosition = ExitWorkerServiceInterior(driver, completedPhase);
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

                if (completedPhase == DriverRescuePhase.AtLaborExchange)
                {
                    driver.WalkPhase = DriverRescuePhase.None;
                    CompleteLaborExchangeApplication(driver, completionPosition);
                    return;
                }

                if (completedPhase == DriverRescuePhase.IdleAtKiosk)
                {
                    driver.WalkPhase = DriverRescuePhase.None;
                    CompleteWorkerVendorPurchase(driver, completedPhase, completionPosition);
                    if (completedGoal == WorkerLifeGoal.Eat || completedGoal == WorkerLifeGoal.Sleep)
                    {
                        driver.LifeGoal = WorkerLifeGoal.None;
                        ContinueWorkerLifeCycle(driver, completionPosition);
                        return;
                    }

                    driver.LifeGoal = WorkerLifeGoal.Idle;
                    driver.IdleWanderPauseTimer = Random.Range(0.5f, 1.4f);
                    return;
                }

                driver.WalkPhase = DriverRescuePhase.None;
                if (completedGoal == WorkerLifeGoal.Eat)
                {
                    ResetWorkerNeedTimer(driver, WorkerNeedKind.Meal);
                    driver.AteToday = true;
                    driver.LifeGoal = WorkerLifeGoal.None;
                    ContinueWorkerLifeCycle(driver, completionPosition);
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
                    ContinueWorkerLifeCycle(driver, completionPosition);
                    return;
                }

                if (completedGoal == WorkerLifeGoal.Sleep)
                {
                    ResetWorkerNeedTimer(driver, WorkerNeedKind.Sleep);
                    driver.SleptToday = true;
                    driver.LifeGoal = WorkerLifeGoal.None;
                    ContinueWorkerLifeCycle(driver, completionPosition);
                    return;
                }

                driver.IdleWanderPauseTimer = Random.Range(0.5f, 1.5f);
            }

            return;
        }

        if (driver.DutyMode == DriverDutyMode.Logistics &&
            (ShouldLogisticsWorkerHeadToBuilding(driver) || IsLogisticsWorkerWorkHour(driver)))
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

        if (driver.IdleWanderPauseTimer > 0f)
        {
            driver.IdleWanderPauseTimer -= Time.deltaTime * gameSpeedMultiplier;
            return;
        }

        if (TryStartDueWorkerLifeCycle(driver))
        {
            return;
        }

        if (TryStartLaborExchangeJobSearch(driver))
        {
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

    private void EnterWorkerServiceInterior(DriverAgent driver, LocationType locationType, LocationData locationOverride = null)
    {
        if (driver?.DriverObject == null)
        {
            return;
        }

        driver.IsInsideBuilding = true;
        driver.InsideBuildingType = locationType;
        LocationData location = locationOverride != null && locationOverride.Type == locationType
            ? locationOverride
            : GetDriverPendingServiceLocation(driver, locationType);
        driver.PendingServiceLocationInstanceId = 0;
        driver.InsideBuildingInstanceId = location != null ? location.InstanceId : 0;
        bool isVisibleImportedVisitor =
            (locationType == LocationType.Bar || locationType == LocationType.GamblingHall) &&
            TrySeatWorkerAtImportedService(driver, location);
        if (!isVisibleImportedVisitor)
        {
            if (locationType == LocationType.Bar || locationType == LocationType.GamblingHall)
            {
                RequestImportedBuildingDoorOpen(location);
            }

            driver.DriverObject.SetActive(false);
        }

        if (location != null)
        {
            RecordWorkerBuildingKnowledge(driver, location, "\u041f\u043e\u0441\u0435\u0442\u0438\u043b \u043f\u043e\u0441\u0442\u0440\u043e\u0439\u043a\u0443", "Visited the building");
        }
        RecordWorkerServiceCoPresence(driver, locationType);
        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} entered {locationType} interior.");
    }

    private Vector3 ExitWorkerServiceInterior(DriverAgent driver, DriverRescuePhase completedPhase)
    {
        if (driver?.DriverObject == null)
        {
            return Vector3.zero;
        }

        LocationType? locationType = GetDriverServiceLocation(completedPhase);
        if (completedPhase == DriverRescuePhase.IdleAtPersonalHouseMeal)
        {
            int houseIndex = driver.AssignedPersonalHouseIndex;
            Vector3 homeMealExitPosition = houseIndex >= 0 && houseIndex < personalHouses.Count
                ? GetDriverStandPointNearPersonalHouse(houseIndex)
                : driver.DriverObject.transform.position;
            driver.IsInsideBuilding = false;
            driver.InsideBuildingType = null;
            driver.InsideBuildingInstanceId = 0;
            driver.DriverObject.transform.position = homeMealExitPosition;
            driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            driver.DriverObject.SetActive(true);
            driver.WalkAnimationTime = 0f;
            ApplyDriverPose(driver, 0f, 0f);
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} exited PersonalHouse meal interior.");
            return homeMealExitPosition;
        }

        if (locationType == LocationType.Bar || locationType == LocationType.GamblingHall)
        {
            LocationType serviceType = locationType.Value;
            if (!TryGetImportedServiceExitPosition(driver, serviceType, out Vector3 serviceExitPosition))
            {
                serviceExitPosition = GetDriverStandPointNearLocation(serviceType);
            }

            ReleaseImportedBarSeat(driver);
            driver.IsInsideBuilding = false;
            driver.InsideBuildingType = null;
            driver.InsideBuildingInstanceId = 0;
            driver.DriverObject.transform.position = serviceExitPosition;
            driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            driver.DriverObject.SetActive(true);
            driver.WalkAnimationTime = 0f;
            ApplyDriverPose(driver, 0f, 0f);
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} exited {serviceType} interior.");
            return serviceExitPosition;
        }

        if (locationType != LocationType.Bar &&
            locationType != LocationType.Canteen &&
            locationType != LocationType.GamblingHall &&
            locationType != LocationType.LaborExchange)
        {
            return driver.DriverObject.transform.position;
        }

        Vector3 exitPosition = GetDriverStandPointNearLocation(locationType.Value);
        if (exitPosition == Vector3.zero)
        {
            exitPosition = driver.DriverObject.transform.position;
            exitPosition.y = SampleTerrainHeight(exitPosition.x, exitPosition.z);
        }

        driver.IsInsideBuilding = false;
        driver.InsideBuildingType = null;
        driver.InsideBuildingInstanceId = 0;
        driver.DriverObject.transform.position = exitPosition;
        driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        driver.DriverObject.SetActive(true);
        driver.WalkAnimationTime = 0f;
        ApplyDriverPose(driver, 0f, 0f);
        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} exited {locationType.Value} interior.");
        return exitPosition;
    }

    private void SelectNextIdleActivity(DriverAgent driver, Vector3 startPosition, Vector3 wanderTarget)
    {
        float roll = Random.value;

        if (roll < 0.18f && TryStartWorkerIdleVendorPurchase(driver, startPosition))
        {
            LogWorkerDecision(driver, "idle-activity", $"vendor purchase, roll={roll:0.00}", true);
        }
        else if (roll < 0.45f)
        {
            TryStartIdleWanderActivity(driver, startPosition, wanderTarget, $"wander roll={roll:0.00}", $"wander path unavailable, roll={roll:0.00}");
        }
        else if (roll < 0.75f && TryGetNearestFreeBench(startPosition, 14f, out int bIdx, out Vector3 bPos))
        {
            driver.IdleActivityTimer = Random.Range(15f, 45f);
            driver.WalkTargetWorld = bPos;
            driver.WalkPhase = DriverRescuePhase.IdleWalkToBench;
            if (!BuildDriverWalkPath(driver, startPosition, bPos))
            {
                driver.WalkPhase = DriverRescuePhase.None;
                driver.IdleActivityTimer = 0f;
                driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
                LogWorkerDecision(driver, "idle-activity-blocked", $"bench={bIdx} path unavailable, roll={roll:0.00}", true);
                return;
            }

            MarkRoadsideBenchOccupied(bIdx);
            driver.SittingBenchIndex = bIdx;
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} heading to bench {bIdx}.");
            LogWorkerDecision(driver, "idle-activity", $"bench={bIdx}, roll={roll:0.00}", true);
        }
        else if (roll < 0.88f)
        {
            if (IsIdleStationarySpotReserved(driver, startPosition))
            {
                TryStartIdleWanderActivity(
                    driver,
                    startPosition,
                    wanderTarget,
                    $"wander from occupied idle spot, roll={roll:0.00}",
                    $"stationary idle spot occupied, roll={roll:0.00}");
                return;
            }

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
            if (IsIdleStationarySpotReserved(driver, startPosition))
            {
                TryStartIdleWanderActivity(
                    driver,
                    startPosition,
                    wanderTarget,
                    $"wander from occupied idle spot before phone call, roll={roll:0.00}",
                    $"phone idle spot occupied, roll={roll:0.00}");
                return;
            }

            driver.IdleActivityTimer = Random.Range(8f, 25f);
            driver.WalkPhase = DriverRescuePhase.IdlePhoneCall;
            SessionDebugLogger.Log("IDLE", $"{driver.DriverName} making a phone call.");
            LogWorkerDecision(driver, "idle-activity", $"phone call, roll={roll:0.00}", true);
        }
    }

    private bool TryStartIdleWanderActivity(DriverAgent driver, Vector3 startPosition, Vector3 wanderTarget, string reason, string blockedReason)
    {
        driver.WalkPhase = DriverRescuePhase.IdleWander;
        driver.IdleWanderPointIndex++;
        driver.WalkAnimationTime = 0f;
        if (!BuildDriverWalkPath(driver, startPosition, wanderTarget))
        {
            driver.WalkPhase = DriverRescuePhase.None;
            driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
            LogWorkerDecision(driver, "idle-activity-blocked", blockedReason, true);
            return false;
        }

        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} started idle walk.");
        LogWorkerDecision(driver, "idle-activity", reason, true);
        return true;
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
        if (!BuildDriverWalkPath(driver, startPosition, catPos))
        {
            driver.IdleCatPetTargetIndex = -1;
            driver.IdleActivityTimer = 0f;
            driver.WalkPhase = DriverRescuePhase.None;
            driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
            LogWorkerDecision(driver, "idle-activity-blocked", "cat path unavailable", true);
            return false;
        }
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
        const int maxIdleWanderCellSteps = 18;
        int nextPointIndex = driver.IdleWanderPointIndex + 1;
        float personalSpaceSqr = DriverIdlePersonalSpace * DriverIdlePersonalSpace;
        Vector2Int startCell = WorldToCell(startPosition);
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

            Vector2Int candidateCell = WorldToCell(candidate);
            if (candidateCell == startCell)
            {
                continue;
            }

            List<Vector2Int> path = FindDriverWalkPath(startCell, candidateCell, DriverRescuePhase.IdleWander);
            if (path == null || path.Count == 0 || path.Count - 1 > maxIdleWanderCellSteps)
            {
                continue;
            }

            if (!IsIdleWanderTargetReserved(driver, candidate, candidateCell, personalSpaceSqr))
            {
                return candidate;
            }
        }

        return startPosition;
    }

    private bool IsIdleWanderTargetReserved(DriverAgent driver, Vector3 candidate, Vector2Int candidateCell, float personalSpaceSqr)
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent other = driverAgents[i];
            if (other == null || other == driver || other.DriverObject == null || !other.DriverObject.activeSelf)
            {
                continue;
            }

            Vector3 otherPosition = other.DriverObject.transform.position;
            Vector3 otherDelta = otherPosition - candidate;
            otherDelta.y = 0f;
            if (otherDelta.sqrMagnitude < personalSpaceSqr || WorldToCell(otherPosition) == candidateCell)
            {
                return true;
            }

            if (other.WalkPhase != DriverRescuePhase.IdleWander)
            {
                continue;
            }

            Vector3 otherTarget = other.WalkTargetWorld;
            if (other.WalkPath.Count > 0)
            {
                otherTarget = other.WalkPath[other.WalkPath.Count - 1];
            }

            Vector3 targetDelta = otherTarget - candidate;
            targetDelta.y = 0f;
            if (targetDelta.sqrMagnitude < personalSpaceSqr || WorldToCell(otherTarget) == candidateCell)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsIdleStationarySpotReserved(DriverAgent driver, Vector3 position)
    {
        float personalSpaceSqr = DriverIdlePersonalSpace * DriverIdlePersonalSpace;
        Vector2Int cell = WorldToCell(position);
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent other = driverAgents[i];
            if (other == null || other == driver || other.DriverObject == null || !other.DriverObject.activeSelf)
            {
                continue;
            }

            Vector3 otherPosition = other.DriverObject.transform.position;
            if (WorldToCell(otherPosition) == cell)
            {
                return true;
            }

            Vector3 delta = otherPosition - position;
            delta.y = 0f;
            if (delta.sqrMagnitude < personalSpaceSqr)
            {
                return true;
            }
        }

        return false;
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
               driver.WalkPhase == DriverRescuePhase.IdleAtKiosk ||
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
        driver.PendingVendorLocationInstanceId = 0;
        driver.PendingVendorItemId = string.Empty;
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
            LocationType.Kiosk,
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
                IsBuildingWalkBufferCell(cell) ||
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
