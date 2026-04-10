using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap : MonoBehaviour
{
    private bool IsDriverOnShift(DriverAgent driver)
    {
        if (driver == null) return false;
        if (driver.ShiftStartHour < 0) return false; // Idle — no shift assigned
        return driver.IsOnActiveShift;
    }

    private static bool IsDriverIdleWanderPhase(DriverAgent driver)
    {
        return driver != null && driver.WalkPhase == DriverRescuePhase.IdleWander;
    }

    private static bool IsDriverIdleConversing(DriverAgent driver)
    {
        return driver != null && driver.IdleConversationTimer > 0f && driver.WalkPhase == DriverRescuePhase.None;
    }

    private static bool IsDriverBusyWalkPhase(DriverAgent driver)
    {
        return driver != null &&
               driver.WalkPhase != DriverRescuePhase.None &&
               driver.WalkPhase != DriverRescuePhase.IdleWander;
    }

    private bool ShouldDriverHeadToShift(DriverAgent driver)
    {
        if (driver == null || driver.ShiftStartHour < 0 || driver.AssignedTruckNumber <= 0)
        {
            return false;
        }

        int minutesUntilShiftStart = GetMinutesUntilShiftStart(driver);
        return minutesUntilShiftStart > 0 && minutesUntilShiftStart <= Mathf.RoundToInt(DriverShiftArrivalLeadHours * 60f);
    }

    private bool TryBoardDriverToAssignedTruck(DriverAgent driver)
    {
        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (driver == null || assignedTruck == null)
        {
            return false;
        }

        if (!driver.WaitingForShiftAtParking)
        {
            return false;
        }

        if (assignedTruck.Driver != null && assignedTruck.Driver != driver)
        {
            return false;
        }

        LoadTruckState(assignedTruck);
        bool canBoard =
            !isTruckMoving &&
            !isTruckInteracting &&
            !isDriverRescueActive &&
            currentAssignedTrip == TripType.None &&
            currentRefuelPhase == RefuelPhase.None &&
            IsTruckInsideParking();
        SaveTruckState(assignedTruck);
        if (!canBoard)
        {
            return false;
        }

        assignedTruck.Driver = driver;
        driver.WaitingForShiftAtParking = false;
        driver.DriverObject.SetActive(false);
        driver.WalkPhase = DriverRescuePhase.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;
        SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} boarded {assignedTruck.DisplayName} in Parking.");
        return true;
    }

    private void StartDriverShiftCommute(DriverAgent driver)
    {
        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (driver == null || assignedTruck == null || driver.DriverObject == null)
        {
            return;
        }

        if (driver.DriverObject.activeSelf == false)
        {
            driver.DriverObject.SetActive(true);
            driver.DriverObject.transform.position = driver.MotelIdlePosition;
            driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        }

        driver.WaitingForShiftAtParking = false;
        driver.IdleWanderPauseTimer = 0f;
        driver.IdleWanderPointIndex = -1;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        driver.WalkAnimationTime = 0f;
        ApplyDriverPose(driver, 0f, 0f);
        driver.WalkPhase = DriverRescuePhase.ToParkingForShift;
        driver.WalkTargetWorld = GetDriverParkingWaitPosition(assignedTruck);
        BuildDriverWalkPath(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld);
        SessionDebugLogger.Log("SHIFT", $"{driver.DriverName} started commute to Parking for {assignedTruck.DisplayName}.");
    }

    private void StartDriverMotelRest(TruckAgent truckAgent, DriverAgent driver)
    {
        if (driver == null || truckAgent == null || driver.DriverObject == null)
        {
            return;
        }

        truckAgent.Driver = null;
        driver.IsOnActiveShift = false;
        driver.WaitingForShiftAtParking = false;
        driver.NeedsRestAfterTrip = false;
        driver.NeedsShiftEndReturn = false;
        driver.DriverObject.SetActive(true);
        driver.DriverObject.transform.position = GetDriverStandPointNearTruck();
        driver.DriverObject.transform.rotation = truckAgent.TruckObject != null ? truckAgent.TruckObject.transform.rotation : Quaternion.LookRotation(Vector3.forward, Vector3.up);
        driver.WalkAnimationTime = 0f;
        ApplyDriverPose(driver, 0f, 0f);
        driver.WalkTargetWorld = GetDriverStandPointNearLocation(LocationType.Motel);
        BuildDriverWalkPath(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld);
        driver.WalkPhase = DriverRescuePhase.ToMotelEntrance;
        driver.RestPhase = DriverRestPhase.DriverWalkToMotel;
        SessionDebugLogger.Log("REST", $"{driver.DriverName} left {truckAgent.DisplayName} in Parking and is walking to Motel.");
    }

    private void UpdateDriverShiftPreparation(DriverAgent driver)
    {
        if (driver == null || driver.IsArrivingByBus || driver.ShiftStartHour < 0 || driver.IsOnActiveShift || driver.RestPhase != DriverRestPhase.None || IsDriverBusyWalkPhase(driver) || driver.AssignedTruckNumber <= 0)
        {
            return;
        }

        bool shouldCommuteToShift = ShouldDriverHeadToShift(driver) || IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour);
        if (!shouldCommuteToShift)
        {
            return;
        }

        if (driver.WaitingForShiftAtParking)
        {
            TryBoardDriverToAssignedTruck(driver);
            return;
        }

        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (assignedTruck == null)
        {
            return;
        }

        StartDriverShiftCommute(driver);
    }

    private void UpdateDriverIdleWander(DriverAgent driver)
    {
        if (driver == null || driver.DriverObject == null)
        {
            return;
        }

        if (driver.IsArrivingByBus ||
            driver.RestPhase != DriverRestPhase.None ||
            driver.IsOnActiveShift ||
            driver.WaitingForShiftAtParking ||
            GetCurrentTruckForDriver(driver) != null)
        {
            if (IsDriverIdleWanderPhase(driver))
            {
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
            }

            driver.IdleConversationTimer = 0f;
            driver.IdleConversationPartnerId = -1;

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

        if (driver.ShiftStartHour >= 0 &&
            (ShouldDriverHeadToShift(driver) || IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour)))
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

        driver.WalkPhase = DriverRescuePhase.IdleWander;
        driver.IdleWanderPointIndex++;
        driver.WalkAnimationTime = 0f;
        BuildDriverWalkPath(driver, startPosition, targetPosition);
        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} started motel idle walk.");
    }

    private Vector3 FindDriverIdleWanderTarget(DriverAgent driver, Vector3 startPosition)
    {
        int nextPointIndex = driver.IdleWanderPointIndex + 1;
        float personalSpaceSqr = DriverIdlePersonalSpace * DriverIdlePersonalSpace;
        for (int attempt = 0; attempt < 16; attempt++)
        {
            Vector3 candidate = GetDriverIdleMotelWanderPosition(driver.DriverId - 1, nextPointIndex + attempt);
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

    private void UpdateHiringDriverArrival()
    {
        if (hiringDriverArrival == null)
        {
            return;
        }

        DriverAgent driver = hiringDriverArrival.Driver;
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
                if (!HasActiveCitySideAmbientBus())
                {
                    CreateHiringArrivalBusVisual();
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.ApproachingStop;
                    SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} arrival bus entered the edge highway.");
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
                    hiringDriverArrival.StopTimer = HiringBusStopDuration;
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.StoppedForDropoff;
                    SpawnDriverFromHiringBus();
                    SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} arrival bus stopped at Bus Stop.");
                }

                UpdateHiringBusTransform();
                break;

            case HiringDriverArrivalPhase.StoppedForDropoff:
                UpdateHiringBusTransform();
                hiringDriverArrival.StopTimer -= dt;
                if (hiringDriverArrival.StopTimer <= 0f)
                {
                    hiringDriverArrival.Phase = HiringDriverArrivalPhase.Departing;
                    SessionDebugLogger.Log("DRIVER", $"{driver.DriverName} finished disembarking; arrival bus departing immediately.");
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
        if (!locations.TryGetValue(LocationType.BusStop, out _))
        {
            return GridWidth * 0.5f;
        }

        Vector3 busStopCenter = GetLocationCenter(LocationType.BusStop);
        return Mathf.Clamp(busStopCenter.x + 0.4f, 1.2f, GridWidth - 1.2f);
    }

    private Vector3 GetHiringBusDropoffWorld()
    {
        if (!locations.TryGetValue(LocationType.BusStop, out _))
        {
            Vector3 fallback = new(GridWidth * 0.5f, 0f, 3.2f);
            fallback.y = SampleTerrainHeight(fallback.x, fallback.z);
            return fallback;
        }

        Vector3 center = GetLocationCenter(LocationType.BusStop);
        Vector3 dropoff = new(GetHiringBusStopWorldX() - 0.22f, 0f, center.z + 0.48f);
        dropoff.y = SampleTerrainHeight(dropoff.x, dropoff.z);
        return dropoff;
    }

    private void SpawnDriverFromHiringBus()
    {
        if (hiringDriverArrival == null || hiringDriverArrival.Driver == null)
        {
            return;
        }

        DriverAgent driver = hiringDriverArrival.Driver;
        if (driver.DriverObject == null)
        {
            return;
        }

        Vector3 dropoff = GetHiringBusDropoffWorld();
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

    private bool CanDriverStartIdleConversation(DriverAgent driver)
    {
        return driver != null &&
               driver.DriverObject != null &&
               driver.DriverObject.activeSelf &&
               !driver.IsArrivingByBus &&
               driver.ShiftStartHour < 0 &&
               !driver.IsOnActiveShift &&
               !driver.WaitingForShiftAtParking &&
               driver.RestPhase == DriverRestPhase.None &&
               driver.WalkPhase == DriverRescuePhase.None &&
               driver.IdleConversationTimer <= 0f &&
               driver.IdleConversationCooldownTimer <= 0f &&
               GetCurrentTruckForDriver(driver) == null;
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
            if (sqrDistance < 0.12f || sqrDistance > bestDistanceSqr)
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

        if (Random.value > DriverIdleConversationStartChance)
        {
            driver.IdleWanderPauseTimer = Random.Range(0.8f, 1.6f);
            return false;
        }

        float duration = Random.Range(DriverIdleConversationDurationMin, DriverIdleConversationDurationMax);
        driver.IdleConversationTimer = duration;
        driver.IdleConversationPartnerId = bestPartner.DriverId;
        bestPartner.IdleConversationTimer = duration;
        bestPartner.IdleConversationPartnerId = driver.DriverId;
        driver.IdleWanderPauseTimer = 0f;
        bestPartner.IdleWanderPauseTimer = 0f;
        driver.WalkAnimationTime = 0f;
        bestPartner.WalkAnimationTime = 0f;
        SessionDebugLogger.Log("IDLE", $"{driver.DriverName} and {bestPartner.DriverName} started an idle conversation.");
        return true;
    }

    private bool CanDriverContinueIdleConversation(DriverAgent driver, DriverAgent partner)
    {
        if (!CanDriverStartIdleConversation(driver) || partner == null || !CanDriverStartIdleConversation(partner))
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
        return sqrDistance >= 0.12f &&
               sqrDistance <= DriverIdleConversationDistance * DriverIdleConversationDistance * 1.2f;
    }

    private void StopDriverIdleConversation(DriverAgent driver, bool addPause)
    {
        if (driver == null)
        {
            return;
        }

        int partnerId = driver.IdleConversationPartnerId;
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
        if (driver.IsArrivingByBus) return;
        if (driver.ShiftStartHour < 0) return;
        if (driver.IsOnActiveShift) return;
        if (driver.RestPhase != DriverRestPhase.None) return;
        if (!IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour)) return;

        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (assignedTruck == null)
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
        if (truckAgent == null || driver == null || !driver.IsOnActiveShift || driver.ShiftStartHour < 0)
        {
            return;
        }

        if (IsHourInShiftWindow(GetCurrentHour(), driver.ShiftStartHour))
        {
            return;
        }

        if (driver.NeedsShiftEndReturn)
        {
            if (truckCell == locations[LocationType.Parking].Anchor &&
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
            if (truckCell != locations[LocationType.Parking].Anchor)
            {
                StartMoveTo(locations[LocationType.Parking].Anchor);
            }
            else
            {
                PayDriverSalary(driver);
                StartDriverMotelRest(truckAgent, driver);
            }
        }
    }

    private void PayDriverSalary(DriverAgent driver)
    {
        if (driver == null || driver.Salary <= 0 || !driver.IsShiftSalaryPending) return;
        int treasuryBefore = money;
        driver.Money += driver.Salary;
        money = Mathf.Max(0, money - driver.Salary);
        int actualTreasuryDelta = money - treasuryBefore;
        driver.IsShiftSalaryPending = false;
        RecordMoneyMovement(
            actualTreasuryDelta,
            "Treasury",
            driver.DriverName,
            $"Salary payout ({GetShiftRangeLabel(driver.ShiftStartHour)})",
            money,
            driver.Money);
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        SessionDebugLogger.Log("PAY", $"{driver.DriverName} paid ${driver.Salary}. Personal balance: ${driver.Money}. Treasury: ${money}.");
    }

    private void EnsurePendingShiftSalaryPaid(DriverAgent driver)
    {
        if (driver == null) return;
        // Driver may leave before shift end time (energy rest) — mark salary as pending so PayDriverSalary will execute
        if (driver.IsOnActiveShift && !driver.IsShiftSalaryPending)
        {
            driver.IsShiftSalaryPending = true;
        }
        PayDriverSalary(driver);
    }

    private void RecordMoneyMovement(int treasuryDelta, string fromLabel, string toLabel, string reason, int? treasuryAfter = null, int? recipientBalanceAfter = null)
    {
        MoneyLedgerEntry entry = new()
        {
            TimeLabel = GetDayNightClockLabel(),
            TreasuryDelta = treasuryDelta,
            FromLabel = fromLabel,
            ToLabel = toLabel,
            Reason = reason,
            TreasuryAfter = treasuryAfter,
            RecipientBalanceAfter = recipientBalanceAfter
        };

        moneyLedgerEntries.Insert(0, entry);
        if (moneyLedgerEntries.Count > MaxMoneyLedgerEntries)
        {
            moneyLedgerEntries.RemoveAt(moneyLedgerEntries.Count - 1);
        }

        isEconomyScreenDirty = true;
    }

    private void UpdateIdleRecall(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        // Driver has no shift assigned — ensure truck returns to parking
        if (driver.ShiftStartHour >= 0) return;
        if (driver.RestPhase != DriverRestPhase.None) return;
        if (isDriverRescueActive) return;
        if (truckCell == locations[LocationType.Parking].Anchor)
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
        StartMoveTo(locations[LocationType.Parking].Anchor);
        SessionDebugLogger.Log("IDLE", $"{GetLoadedTruckDisplayName()} returning to parking — driver is idle.");
    }

}
