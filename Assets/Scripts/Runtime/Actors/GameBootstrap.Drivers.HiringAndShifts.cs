using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private void UpdateHiringDriverArrival()
    {
        if (hiringDriverArrival == null)
        {
            return;
        }

        DriverAgent driver = GetPrimaryHiringArrivalDriver();
        if (driver == null)
        {
            CleanupHiringDriverArrival(false);
            return;
        }

        float dt = Time.deltaTime * gameSpeedMultiplier;
        if (dt <= 0f)
        {
            return;
        }

        switch (hiringDriverArrival.Phase)
        {
            case HiringDriverArrivalPhase.WaitingLaneClear:
                if (hiringDriverArrival.IsTutorialWave || !HasActiveCitySideAmbientBus())
                {
                    CreateHiringArrivalBusVisual();
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.ApproachingStop;
                    SessionDebugLogger.Log("DRIVER", $"{GetHiringArrivalLabel()} arrival bus entered the edge highway.");
                }
                break;

            case HiringDriverArrivalPhase.ApproachingStop:
                if (hiringDriverArrival.BusRootTransform == null)
                {
                    CleanupHiringDriverArrival(false);
                    return;
                }

                hiringDriverArrival.BusWorldX += hiringDriverArrival.BusSpeed * dt;
                float stopX = GetHiringBusStopWorldX();
                if (hiringDriverArrival.BusWorldX >= stopX)
                {
                    hiringDriverArrival.BusWorldX = stopX;
                    int passengerCount = Mathf.Max(1, hiringDriverArrival.Drivers.Count);
                    hiringDriverArrival.StopTimer = HiringBusStopDuration + passengerCount * HiringBusDisembarkInterval;
                    hiringDriverArrival.DisembarkTimer = 0f;
                    hiringDriverArrival.NextDisembarkIndex = 0;
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.StoppedForDropoff;
                    SessionDebugLogger.Log("DRIVER", $"{GetHiringArrivalLabel()} arrival bus stopped at Intercity Stop.");
                }

                UpdateHiringBusTransform();
                break;

            case HiringDriverArrivalPhase.StoppedForDropoff:
                UpdateHiringBusTransform();
                UpdateHiringBusDisembark(dt);
                hiringDriverArrival.StopTimer -= dt;
                if (hiringDriverArrival.StopTimer <= 0f && HasFinishedHiringBusDisembark())
                {
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.Departing;
                    SessionDebugLogger.Log("DRIVER", $"{GetHiringArrivalLabel()} finished disembarking; arrival bus departing immediately.");
                }
                break;

            case HiringDriverArrivalPhase.DriverWalkingToMotel:
                UpdateHiringBusTransform();
                if (!driver.IsArrivingByBus)
                {
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.Departing;
                    SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} reached Motel; arrival bus departing.");
                }
                break;

            case HiringDriverArrivalPhase.Departing:
                if (hiringDriverArrival.BusRootTransform == null)
                {
                    CleanupHiringDriverArrival(false);
                    return;
                }

                hiringDriverArrival.BusWorldX += hiringDriverArrival.BusSpeed * dt;
                UpdateHiringBusTransform();
                if (hiringDriverArrival.BusWorldX >= GridWidth)
                {
                    CleanupHiringDriverArrival(true);
                }
                break;
        }
    }

    private bool HasActiveCitySideAmbientBus()
    {
        for (int i = 0; i < edgeHighwayBuses.Count; i++)
        {
            EdgeHighwayBusData bus = edgeHighwayBuses[i];
            if (bus != null && bus.RootTransform != null && bus.IsCitySideLane)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsDriverMotelArrivalInProgress()
    {
        return hiringDriverArrival != null;
    }

    private float GetHiringBusStopWorldX()
    {
        if (!locations.TryGetValue(LocationType.IntercityStop, out _))
        {
            return GridWidth * 0.5f;
        }

        Vector3 busStopCenter = GetLocationCenter(LocationType.IntercityStop);
        return Mathf.Clamp(busStopCenter.x + 0.4f, 1.2f, GridWidth - 1.2f);
    }

    private Vector3 GetHiringBusDropoffWorld()
    {
        if (!locations.TryGetValue(LocationType.IntercityStop, out _))
        {
            Vector3 fallback = new(GridWidth * 0.5f, 0f, 3.2f);
            fallback.y = SampleTerrainHeight(fallback.x, fallback.z);
            return fallback;
        }

        Vector3 center = GetLocationCenter(LocationType.IntercityStop);
        Vector3 dropoff = new(GetHiringBusStopWorldX() - 0.22f, 0f, center.z + 0.48f);
        dropoff.y = SampleTerrainHeight(dropoff.x, dropoff.z);
        return dropoff;
    }

    private DriverAgent GetPrimaryHiringArrivalDriver()
    {
        if (hiringDriverArrival == null)
        {
            return null;
        }

        if (hiringDriverArrival.Driver != null)
        {
            return hiringDriverArrival.Driver;
        }

        for (int i = 0; i < hiringDriverArrival.Drivers.Count; i++)
        {
            if (hiringDriverArrival.Drivers[i] != null)
            {
                return hiringDriverArrival.Drivers[i];
            }
        }

        return null;
    }

    private string GetHiringArrivalLabel()
    {
        if (hiringDriverArrival == null)
        {
            return "Hiring";
        }

        int count = Mathf.Max(1, hiringDriverArrival.Drivers.Count);
        if (count <= 1)
        {
            return GetPrimaryHiringArrivalDriver()?.DriverName ?? "Hiring";
        }

        return $"{count} workers";
    }

    private void UpdateHiringBusDisembark(float dt)
    {
        if (hiringDriverArrival == null)
        {
            return;
        }

        if (hiringDriverArrival.Drivers.Count == 0 && hiringDriverArrival.Driver != null)
        {
            hiringDriverArrival.Drivers.Add(hiringDriverArrival.Driver);
        }

        hiringDriverArrival.DisembarkTimer -= dt;
        while (hiringDriverArrival.DisembarkTimer <= 0f &&
               hiringDriverArrival.NextDisembarkIndex < hiringDriverArrival.Drivers.Count)
        {
            SpawnDriverFromHiringBus(hiringDriverArrival.NextDisembarkIndex);
            hiringDriverArrival.NextDisembarkIndex++;
            hiringDriverArrival.DisembarkTimer += HiringBusDisembarkInterval;
        }

        if (HasFinishedHiringBusDisembark() && !hiringDriverArrival.HasNotifiedDisembark)
        {
            hiringDriverArrival.HasNotifiedDisembark = true;
            if (hiringDriverArrival.IsTutorialWave)
            {
                NotifyTutorialHiringWaveDisembarked();
            }

            if (hiringDriverArrival.IsMotelBootstrapWave)
            {
                NotifyMotelBootstrapWorkerWaveDisembarked();
            }
        }
    }

    private bool HasFinishedHiringBusDisembark()
    {
        return hiringDriverArrival == null ||
               hiringDriverArrival.NextDisembarkIndex >= Mathf.Max(1, hiringDriverArrival.Drivers.Count);
    }

    private void SpawnDriverFromHiringBus(int index)
    {
        if (hiringDriverArrival == null)
        {
            return;
        }

        if (hiringDriverArrival.Drivers.Count == 0 && hiringDriverArrival.Driver != null)
        {
            hiringDriverArrival.Drivers.Add(hiringDriverArrival.Driver);
        }

        if (index < 0 || index >= hiringDriverArrival.Drivers.Count)
        {
            return;
        }

        DriverAgent driver = hiringDriverArrival.Drivers[index];
        if (driver?.DriverObject == null)
        {
            return;
        }

        Vector3 baseDropoff = GetHiringBusDropoffWorld();
        Vector3 offset = new((index % 4 - 1.5f) * 0.34f, 0f, (index / 4) * 0.36f);
        Vector3 dropoff = baseDropoff + offset;
        dropoff.y = SampleTerrainHeight(dropoff.x, dropoff.z);
        SpawnDriverFromHiringBus(driver, dropoff);
    }

    private void SpawnDriverFromHiringBus(DriverAgent driver, Vector3 dropoff)
    {
        driver.DriverObject.SetActive(true);
        driver.DriverObject.transform.position = dropoff;
        driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        driver.WalkAnimationTime = 0f;
        driver.WalkPhase = DriverRescuePhase.ToMotelFromBusStop;
        driver.WalkTargetWorld = driver.MotelIdlePosition;
        driver.IdleWanderPauseTimer = 0f;
        driver.IdleWanderPointIndex = -1;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        ApplyDriverPose(driver, 0f, 0f);
        BuildDriverWalkPath(driver, dropoff, driver.MotelIdlePosition);
        SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} exited the arrival bus and started walking to Motel.");
    }

    private void CleanupHiringDriverArrival(bool destroyBus)
    {
        if (hiringDriverArrival == null)
        {
            return;
        }

        if (destroyBus && hiringDriverArrival.BusRootTransform != null)
        {
            Destroy(hiringDriverArrival.BusRootTransform.gameObject);
        }

        hiringDriverArrival = null;
    }

    private DriverAgent GetDriverAgentById(int driverId)
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            if (driverAgents[i].DriverId == driverId)
            {
                return driverAgents[i];
            }
        }

        return null;
    }

    private bool CanDriverConverse(DriverAgent driver)
    {
        return driver != null &&
               driver.DriverObject != null &&
               driver.DriverObject.activeSelf &&
               !driver.IsArrivingByBus &&
               !driver.IsOnActiveShift &&
               !driver.NeedsShiftEndReturn &&
               !driver.WaitingForShiftAtParking &&
               driver.RestPhase == DriverRestPhase.None &&
               driver.WalkPhase == DriverRescuePhase.None &&
               GetCurrentTruckForDriver(driver) == null &&
               !IsDriverConversationShiftBlocked(driver);
    }

    private bool IsDriverConversationShiftBlocked(DriverAgent driver)
    {
        if (driver == null)
        {
            return true;
        }

        if (driver.DutyMode == DriverDutyMode.Logistics &&
            (ShouldLogisticsWorkerHeadToBuilding(driver) || IsLogisticsWorkerWorkHour(driver)))
        {
            return true;
        }

        return driver.ShiftStartHour >= 0 &&
               (ShouldDriverHeadToShift(driver) || IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour));
    }

    private bool CanDriverStartIdleConversation(DriverAgent driver)
    {
        return CanDriverConverse(driver) &&
               driver.IdleConversationTimer <= 0f &&
               driver.IdleConversationCooldownTimer <= 0f;
    }

    private bool TryStartIdleConversation(DriverAgent driver)
    {
        if (!CanDriverStartIdleConversation(driver))
        {
            return false;
        }

        DriverAgent bestPartner = null;
        float bestDistanceSqr = DriverIdleConversationDistance * DriverIdleConversationDistance;
        Vector3 driverPosition = driver.DriverObject.transform.position;
        Vector2Int driverCell = WorldToCell(driverPosition);
        float minConversationDistanceSqr = DriverIdlePersonalSpace * DriverIdlePersonalSpace;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent candidate = driverAgents[i];
            if (candidate == driver || !CanDriverStartIdleConversation(candidate))
            {
                continue;
            }

            Vector3 delta = candidate.DriverObject.transform.position - driverPosition;
            delta.y = 0f;
            float sqrDistance = delta.sqrMagnitude;
            if (WorldToCell(candidate.DriverObject.transform.position) == driverCell ||
                sqrDistance < minConversationDistanceSqr ||
                sqrDistance > bestDistanceSqr)
            {
                continue;
            }

            bestDistanceSqr = sqrDistance;
            bestPartner = candidate;
        }

        if (bestPartner == null)
        {
            return false;
        }

        float startChance = GetDriverIdleConversationStartChance(driver, bestPartner);
        if (Random.value > startChance)
        {
            driver.IdleWanderPauseTimer = Random.Range(0.8f, 1.6f);
            return false;
        }

        float duration = StartWorkerIdleDialogue(driver, bestPartner);
        driver.IdleConversationTimer = duration;
        driver.IdleConversationPartnerId = bestPartner.DriverId;
        bestPartner.IdleConversationTimer = duration;
        bestPartner.IdleConversationPartnerId = driver.DriverId;
        driver.IdleWanderPauseTimer = 0f;
        bestPartner.IdleWanderPauseTimer = 0f;
        driver.WalkAnimationTime = 0f;
        bestPartner.WalkAnimationTime = 0f;
        RecordWorkerSocialInteraction(driver, bestPartner, WorkerSocialInteractionKind.IdleConversation);
        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} and {bestPartner.DriverName} started an idle conversation.");
        return true;
    }

    private float GetDriverIdleConversationStartChance(DriverAgent driver, DriverAgent partner)
    {
        float chance = DriverIdleConversationStartChance;
        if (HasWorkerPerk(driver, WorkerPerkKind.Socialite))
        {
            chance += 0.25f;
        }

        if (HasWorkerPerk(partner, WorkerPerkKind.Socialite))
        {
            chance += 0.2f;
        }

        if (HasActiveWorkerConversationTopicKnowledge(driver))
        {
            chance += 0.35f;
        }

        return Mathf.Clamp01(chance);
    }

    private bool CanDriverContinueIdleConversation(DriverAgent driver, DriverAgent partner)
    {
        if (!CanDriverConverse(driver) || partner == null || !CanDriverConverse(partner))
        {
            return false;
        }

        if (partner.IdleConversationPartnerId != driver.DriverId)
        {
            return false;
        }

        Vector3 delta = partner.DriverObject.transform.position - driver.DriverObject.transform.position;
        delta.y = 0f;
        float sqrDistance = delta.sqrMagnitude;
        return WorldToCell(partner.DriverObject.transform.position) != WorldToCell(driver.DriverObject.transform.position) &&
               sqrDistance >= DriverIdlePersonalSpace * DriverIdlePersonalSpace * 0.64f &&
               sqrDistance <= DriverIdleConversationDistance * DriverIdleConversationDistance * 1.2f;
    }

    private void StopDriverIdleConversation(DriverAgent driver, bool addPause)
    {
        if (driver == null)
        {
            return;
        }

        int partnerId = driver.IdleConversationPartnerId;
        StopWorkerIdleDialogue(driver.DriverId, partnerId);
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        driver.IdleConversationCooldownTimer = Random.Range(DriverIdleConversationCooldownMin, DriverIdleConversationCooldownMax);
        driver.IdleWanderPointIndex++;
        if (addPause)
        {
            driver.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
        }

        DriverAgent partner = GetDriverAgentById(partnerId);
        if (partner != null && partner.IdleConversationPartnerId == driver.DriverId)
        {
            partner.IdleConversationTimer = 0f;
            partner.IdleConversationPartnerId = -1;
            partner.IdleConversationCooldownTimer = Random.Range(DriverIdleConversationCooldownMin, DriverIdleConversationCooldownMax);
            partner.IdleWanderPointIndex += 2;
            if (addPause)
            {
                partner.IdleWanderPauseTimer = Random.Range(DriverIdleWanderPauseMin, DriverIdleWanderPauseMax);
            }
        }
    }

    private void UpdateDriverShiftActivation(DriverAgent driver)
    {
        if (driver == null) return;
        if (IsDriverIntercity(driver)) return;
        if (driver.DutyMode == DriverDutyMode.Logistics) return;
        if (driver.IsArrivingByBus) return;
        if (driver.ShiftStartHour < 0) return;
        if (driver.IsOnActiveShift) return;
        if (driver.RestPhase != DriverRestPhase.None) return;
        if (!IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour)) return;

        if (IsDriverBusDriver(driver))
        {
            if (!TryBoardBusDriver(driver) && !IsBusDriverOnActiveRoute(driver))
            {
                return;
            }

            driver.IsOnActiveShift = true;
            driver.IsShiftSalaryPending = false;
            SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} bus shift started ({GetShiftRangeLabel(driver.ShiftStartHour)}).");
            if (localBusRoute != null && localBusRoute.Driver == driver && localBusRoute.Phase == LocalBusPhase.ParkedAwaitingShiftStart)
            {
                BeginLocalBusRouteFromParking();
            }
            return;
        }

        if (!TryReserveAvailableTruckForDriver(driver, out TruckAgent assignedTruck, "freight shift activation"))
        {
            return;
        }

        if (assignedTruck.Driver != driver && !TryBoardDriverToAssignedTruck(driver))
        {
            return;
        }

        driver.IsOnActiveShift = true;
        driver.IsShiftSalaryPending = false;
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} shift started ({GetShiftRangeLabel(driver.ShiftStartHour)}).");
        SetTruckAutoMode(assignedTruck, true);
    }

    private void UpdateDriverShiftEnd(TruckAgent truckAgent, DriverAgent driver)
    {
        if (truckAgent == null || driver == null || IsDriverIntercity(driver) || !driver.IsOnActiveShift || driver.ShiftStartHour < 0)
        {
            return;
        }

        if (IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour))
        {
            return;
        }

        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            driver.IsOnActiveShift = false;
            driver.NeedsShiftEndReturn = false;
            driver.IsShiftSalaryPending = false;
            truckAgent.IsTruckAutoModeEnabled = false;
            SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} shift return cancelled: Parking is not built.");
            return;
        }

        if (driver.NeedsShiftEndReturn)
        {
            if (truckCell == parking.Anchor &&
                !isTruckMoving &&
                !isTruckInteracting &&
                !isDriverRescueActive &&
                currentAssignedTrip == TripType.None &&
                currentRefuelPhase == RefuelPhase.None)
            {
                PayDriverSalary(driver);
                StartDriverMotelRest(truckAgent, driver);
            }
            return;
        }

        driver.IsOnActiveShift = false;
        truckAgent.IsTruckAutoModeEnabled = false;
        driver.NeedsShiftEndReturn = true;
        driver.IsShiftSalaryPending = true;
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} shift ended. {truckAgent.DisplayName} returning to Parking for handoff.");

        if (currentAssignedTrip == TripType.None &&
            currentRefuelPhase == RefuelPhase.None &&
            !isTruckMoving &&
            !isTruckInteracting &&
            !isDriverRescueActive)
        {
            if (truckCell != parking.Anchor)
            {
                StartMoveTo(parking.Anchor);
            }
            else
            {
                PayDriverSalary(driver);
                StartDriverMotelRest(truckAgent, driver);
            }
        }
    }

    private void UpdateBusDriverShiftEnd(DriverAgent driver)
    {
        if (driver == null || !IsDriverBusDriver(driver) || driver.ShiftStartHour < 0)
        {
            return;
        }

        if (driver.IsOnActiveShift && IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour))
        {
            return;
        }

        if (driver.NeedsShiftEndReturn)
        {
            if (localBusRoute != null &&
                localBusRoute.Driver == driver &&
                localBusRoute.Phase == LocalBusPhase.ParkedAwaitingShiftStart)
            {
                CompleteBusDriverShiftReturn(driver);
            }
            return;
        }

        if (!driver.IsOnActiveShift)
        {
            return;
        }

        driver.IsOnActiveShift = false;
        driver.NeedsShiftEndReturn = true;
        driver.IsShiftSalaryPending = true;
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} bus shift ended. Finishing the current stop before returning the local bus to Parking.");
        if (localBusRoute != null && localBusRoute.Driver == driver)
        {
            SessionDebugLogger.Log(
                "BUS_SHIFT",
                $"{driver.DriverName} marked for shift-end return: phase={localBusRoute.Phase}, currentStopIndex={localBusRoute.CurrentStopIndex}, direction={(localBusRoute.TravelDirection > 0 ? "ascending" : "descending")}.");
        }
    }

    private void PayDriverSalary(DriverAgent driver)
    {
        if (driver == null || driver.Salary <= 0 || !driver.IsShiftSalaryPending) return;
        int salaryShiftStart = GetSalaryShiftStartHour(driver);
        int salaryShiftDay = GetSalaryShiftDay(driver, salaryShiftStart);
        if (driver.LastSalaryPaidShiftDay == salaryShiftDay &&
            driver.LastSalaryPaidShiftStartHour == salaryShiftStart)
        {
            driver.IsShiftSalaryPending = false;
            SessionDebugLogger.Log("PAY", $"{driver.DriverName} salary payout skipped: already paid for shiftDay={salaryShiftDay}, shiftStart={salaryShiftStart}.");
            return;
        }

        int treasuryBefore = money;
        driver.Money += driver.Salary;
        money -= driver.Salary;
        int actualTreasuryDelta = money - treasuryBefore;
        driver.IsShiftSalaryPending = false;
        driver.LastSalaryPaidShiftDay = salaryShiftDay;
        driver.LastSalaryPaidShiftStartHour = salaryShiftStart;
        RecordMoneyMovement(
            actualTreasuryDelta,
            "Treasury",
            driver.DriverName,
            driver.DutyMode == DriverDutyMode.Logistics
                ? $"Salary payout ({GetProductionWorkRangeLabel()})"
                : $"Salary payout ({GetShiftRangeLabel(driver.ShiftStartHour)})",
            money,
            driver.Money,
            MoneyAccountKind.CityBudget,
            MoneyAccountKind.ResidentWallet,
            MoneyTransactionReasonKind.Salary,
            toOwnerId: driver.DriverId);
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        SessionDebugLogger.Log("PAY", $"{driver.DriverName} paid ${driver.Salary}. Personal balance: ${driver.Money}. Treasury: ${money}.");
        RecordWorkerThought(
            driver,
            WorkerThoughtKind.Money,
            WorkerThoughtTone.Positive,
            driver.Money < 20 ? 68 : 52,
            "salary_paid",
            new[]
            {
                ThoughtText("amount", $"${driver.Salary}"),
                ThoughtText("balance", $"${driver.Money}")
            },
            WorkerThoughtSubjectType.Text,
            0,
            "salary",
            "salary",
            4,
            $"salary_paid|{salaryShiftDay}|{salaryShiftStart}",
            8f);
        AdvanceWorkerContractAfterPaidShift(driver);
    }

    private int GetSalaryShiftStartHour(DriverAgent driver)
    {
        if (driver == null)
        {
            return -1;
        }

        if (driver.DutyMode == DriverDutyMode.Logistics && driver.AssignedBuildingType.HasValue)
        {
            int shiftStart = GetBuildingWorkerShiftStartHour(driver.AssignedBuildingType.Value, GetLogisticsWorkerSlotIndex(driver));
            return shiftStart >= 0 ? shiftStart : ProductionWorkStartHour;
        }

        return driver.ShiftStartHour >= 0 ? driver.ShiftStartHour : ProductionWorkStartHour;
    }

    private int GetSalaryShiftDay(DriverAgent driver, int shiftStartHour)
    {
        int hour = GetCurrentHour();
        if (shiftStartHour >= 0 && shiftStartHour != ProductionWorkStartHour && hour < shiftStartHour)
        {
            return currentDay - 1;
        }

        if (shiftStartHour == ProductionWorkStartHour && hour < ProductionWorkStartHour)
        {
            return currentDay - 1;
        }

        return currentDay;
    }

    private void EnsurePendingShiftSalaryPaid(DriverAgent driver)
    {
        if (driver == null) return;
        // Driver may leave before shift end time for the life-cycle flow; keep salary payout pending.
        if (driver.IsOnActiveShift && !driver.IsShiftSalaryPending)
        {
            driver.IsShiftSalaryPending = true;
        }
        PayDriverSalary(driver);
    }

    private void RecordMoneyMovement(
        int treasuryDelta,
        string fromLabel,
        string toLabel,
        string reason,
        int? treasuryAfter = null,
        int? recipientBalanceAfter = null,
        MoneyAccountKind? fromAccountKind = null,
        MoneyAccountKind? toAccountKind = null,
        MoneyTransactionReasonKind reasonKind = MoneyTransactionReasonKind.Other,
        int fromOwnerId = 0,
        int toOwnerId = 0)
    {
        MoneyLedgerEntry entry = new()
        {
            TimeLabel = GetDayNightClockLabel(),
            TreasuryDelta = treasuryDelta,
            FromLabel = fromLabel,
            ToLabel = toLabel,
            FromAccountKind = fromAccountKind ?? InferMoneyAccountKind(fromLabel, treasuryDelta < 0),
            ToAccountKind = toAccountKind ?? InferMoneyAccountKind(toLabel, treasuryDelta > 0),
            FromOwnerId = Mathf.Max(0, fromOwnerId),
            ToOwnerId = Mathf.Max(0, toOwnerId),
            ReasonKind = reasonKind,
            Reason = reason,
            TreasuryAfter = treasuryAfter,
            RecipientBalanceAfter = recipientBalanceAfter
        };

        moneyLedgerEntries.Insert(0, entry);
        if (moneyLedgerEntries.Count > MaxMoneyLedgerEntries)
        {
            moneyLedgerEntries.RemoveAt(moneyLedgerEntries.Count - 1);
        }

        LogEconomyMovement(entry);
        isEconomyScreenDirty = true;
        isBuildScreenDirty = true;
    }

    private static MoneyAccountKind InferMoneyAccountKind(string label, bool treasurySide)
    {
        if (string.Equals(label, "Treasury", System.StringComparison.OrdinalIgnoreCase))
        {
            return MoneyAccountKind.CityBudget;
        }

        if (string.Equals(label, "Building Taxes", System.StringComparison.OrdinalIgnoreCase))
        {
            return MoneyAccountKind.BuildingCash;
        }

        if (string.Equals(label, "Debug", System.StringComparison.OrdinalIgnoreCase))
        {
            return MoneyAccountKind.Debug;
        }

        return treasurySide ? MoneyAccountKind.CityBudget : MoneyAccountKind.External;
    }

    private void UpdateIdleRecall(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        if (IsDriverIntercity(driver) || IsDriverOnActiveTradeRun(driver))
        {
            return;
        }

        // Driver has no shift assigned - ensure truck returns to parking
        if (driver.DutyMode != DriverDutyMode.Local) return;
        if (driver.ShiftStartHour >= 0) return;
        if (driver.RestPhase != DriverRestPhase.None) return;
        if (isDriverRescueActive) return;
        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            currentAssignedTrip = TripType.None;
            currentTripPhase = TripPhase.None;
            currentRefuelPhase = RefuelPhase.None;
            isTruckAutoModeEnabled = false;
            currentAssignedTripReward = 0;
            if (GetCurrentTruckForDriver(driver) is TruckAgent currentTruck)
            {
                StartDriverMotelRest(currentTruck, driver);
            }
            return;
        }

        if (truckCell == parking.Anchor)
        {
            if (GetCurrentTruckForDriver(driver) is TruckAgent currentTruck)
            {
                StartDriverMotelRest(currentTruck, driver);
            }
            return;
        }
        if (isTruckMoving) return;

        // Cancel any active orders then head home
        currentAssignedTrip = TripType.None;
        currentTripPhase = TripPhase.None;
        currentRefuelPhase = RefuelPhase.None;
        isTruckAutoModeEnabled = false;
        currentAssignedTripReward = 0;
        StartMoveTo(parking.Anchor);
        SessionDebugLogger.Log("IDLE", $"{GetLoadedTruckDisplayName()} returning to parking - driver is idle.");
}


}
