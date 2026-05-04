using UnityEngine;

using System.Collections.Generic;

public partial class GameBootstrap
{
    private void DebugAutoAssignAllAvailableWorkers()
    {
        if (driverAgents == null || driverAgents.Count == 0)
        {
            SessionDebugLogger.Log("DEBUG_ASSIGN", "Auto assign skipped: no workers exist.");
            return;
        }

        List<string> assignments = new();
        int truckAssignments = 0;
        int shiftAssignments = 0;
        int buildingAssignments = 0;
        int busAssignments = 0;

        DebugNormalizeTruckRosterShiftAssignments(assignments, ref shiftAssignments);

        if (truckAgents != null)
        {
            for (int truckIndex = 0; truckIndex < truckAgents.Count; truckIndex++)
            {
                TruckAgent truck = truckAgents[truckIndex];
                if (truck == null)
                {
                    continue;
                }

                while (truck.AssignedDrivers.Count < 2)
                {
                    int shiftIndex = DebugFindAvailableShiftIndexForTruck(truck);
                    if (shiftIndex < 0)
                    {
                        assignments.Add($"{truck.DisplayName}: skipped, all logistics shifts are already used on this truck.");
                        break;
                    }

                    DriverAgent worker = DebugFindNextAutoAssignableWorker(d => CanAssignDriverToTruckRoster(truck, d));
                    if (worker == null)
                    {
                        assignments.Add($"{truck.DisplayName}: no available worker for roster slot {truck.AssignedDrivers.Count + 1}.");
                        break;
                    }

                    if (!AssignDriverToTruck(truck, worker))
                    {
                        assignments.Add($"{truck.DisplayName}: assignment failed for {worker.DriverName}.");
                        break;
                    }

                    truckAssignments++;
                    if (DebugAssignTruckWorkerToShift(worker, shiftIndex, truck, assignments))
                    {
                        shiftAssignments++;
                    }
                }
            }
        }

        buildingAssignments += DebugAutoAssignBuildingSlots(assignments);
        busAssignments += DebugAutoAssignBusDriverSlots(assignments);

        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;

        int freeWorkersLeft = DebugCountAutoAssignableWorkers();
        string summary =
            $"Auto assign finished: trucks={truckAssignments}, truckShifts={shiftAssignments}, buildings={buildingAssignments}, " +
            $"bus={busAssignments}, freeWorkersLeft={freeWorkersLeft}.";
        SessionDebugLogger.Log("DEBUG_ASSIGN", summary);
        for (int i = 0; i < assignments.Count; i++)
        {
            SessionDebugLogger.Log("DEBUG_ASSIGN", $"  - {assignments[i]}");
        }
    }

    private void DebugNormalizeTruckRosterShiftAssignments(List<string> assignments, ref int shiftAssignments)
    {
        if (truckAgents == null)
        {
            return;
        }

        for (int truckIndex = 0; truckIndex < truckAgents.Count; truckIndex++)
        {
            TruckAgent truck = truckAgents[truckIndex];
            if (truck == null || truck.AssignedDrivers.Count == 0)
            {
                continue;
            }

            bool[] usedShiftIndexes = new bool[ShiftPresetHours.Length];
            for (int i = 0; i < truck.AssignedDrivers.Count; i++)
            {
                DriverAgent driver = truck.AssignedDrivers[i];
                if (driver == null)
                {
                    continue;
                }

                int shiftIndex = DebugGetShiftIndex(driver.ShiftStartHour);
                if (shiftIndex >= 0 && !usedShiftIndexes[shiftIndex])
                {
                    usedShiftIndexes[shiftIndex] = true;
                    continue;
                }

                int newShiftIndex = DebugFindFirstUnusedShiftIndex(usedShiftIndexes);
                if (newShiftIndex < 0)
                {
                    assignments.Add($"{driver.DriverName}: no unique truck shift available on {truck.DisplayName}.");
                    continue;
                }

                DebugSetLocalShift(driver, newShiftIndex);
                usedShiftIndexes[newShiftIndex] = true;
                shiftAssignments++;
                assignments.Add($"{driver.DriverName}: normalized to {ShiftNames[newShiftIndex]} for {truck.DisplayName}.");
            }
        }
    }

    private int DebugAutoAssignBuildingSlots(List<string> assignments)
    {
        int count = 0;
        LocationType[] buildings =
        {
            LocationType.Forest,
            LocationType.Sawmill,
            LocationType.FurnitureFactory,
            LocationType.Warehouse,
            LocationType.Motel,
            LocationType.Bar,
            LocationType.Canteen,
            LocationType.GasStation,
            LocationType.GamblingHall,
            LocationType.CarMarket,
            LocationType.LaborExchange
        };

        for (int i = 0; i < buildings.Length; i++)
        {
            int maxSlots = GetMaxBuildingWorkerSlots(buildings[i]);
            for (int slotIndex = 0; slotIndex < maxSlots; slotIndex++)
            {
                if (DebugTryAssignBuildingSlot(buildings[i], slotIndex, assignments))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private bool DebugTryAssignBuildingSlot(LocationType buildingType, int slotIndex, List<string> assignments)
    {
        if (locations == null || !locations.ContainsKey(buildingType))
        {
            return false;
        }

        if (GetNthLogisticsWorker(buildingType, slotIndex) != null)
        {
            return false;
        }

        DriverAgent worker = buildingType == LocationType.LaborExchange
            ? DebugFindNextAutoAssignableWorker(d => HasHigherEducation(d))
            : DebugFindNextAutoAssignableWorker();
        if (worker == null)
        {
            assignments.Add($"{buildingType} slot {slotIndex + 1}: no available worker.");
            return false;
        }

        LogisticsSlotUi slot = new()
        {
            BuildingType = buildingType,
            SlotIndex = slotIndex
        };

        AssignWorkerToBuilding(worker, slot);
        string workKind = IsProductionLocation(buildingType) ? "production" : "service";
        assignments.Add($"{worker.DriverName}: assigned to {buildingType} {workKind} slot {slotIndex + 1}.");
        return true;
    }

    private int DebugAutoAssignBusDriverSlots(List<string> assignments)
    {
        int count = 0;
        for (int slotIndex = 0; slotIndex < ShiftPresetHours.Length; slotIndex++)
        {
            if (GetBusAssignedDriver(slotIndex) != null)
            {
                continue;
            }

            DriverAgent worker = DebugFindNextAutoAssignableWorker(d =>
                d.DutyMode == DriverDutyMode.Local &&
                d.AssignedTruckNumber <= 0 &&
                d.ShiftStartHour < 0 &&
                !IsDriverBusDriver(d));

            if (worker == null)
            {
                assignments.Add($"Bus Driver {ShiftNames[slotIndex]}: no available worker.");
                continue;
            }

            AssignDriverToBusSlot(worker, slotIndex);
            assignments.Add($"{worker.DriverName}: assigned to Bus Driver {ShiftNames[slotIndex]}.");
            count++;
        }

        return count;
    }

    private int DebugAutoAssignIntercitySlot(List<string> assignments)
    {
        if (GetIntercityAssignedDriver() != null)
        {
            return 0;
        }

        if (HasActiveTradeRun())
        {
            assignments.Add("Intercity: skipped, active trade run is in progress.");
            return 0;
        }

        TruckAgent intercityTruck = null;
        if (truckAgents != null)
        {
            for (int i = 0; i < truckAgents.Count; i++)
            {
                TruckAgent candidateTruck = truckAgents[i];
                if (candidateTruck != null && candidateTruck.AssignedDrivers.Count < 2)
                {
                    intercityTruck = candidateTruck;
                    break;
                }
            }
        }

        if (intercityTruck == null)
        {
            assignments.Add("Intercity: no truck with free crew slot.");
            return 0;
        }

        DriverAgent worker = DebugFindNextAutoAssignableWorker(d => CanAssignDriverToTruckRoster(intercityTruck, d));
        if (worker == null)
        {
            assignments.Add("Intercity: no available worker.");
            return 0;
        }

        if (!AssignDriverToTruck(intercityTruck, worker))
        {
            assignments.Add($"Intercity: failed to assign {worker.DriverName} to {intercityTruck.DisplayName}.");
            return 0;
        }

        AssignDriverToIntercitySlot(worker);
        assignments.Add($"{worker.DriverName}: assigned to Intercity with {intercityTruck.DisplayName}.");
        return 1;
    }

    private bool DebugAssignTruckWorkerToShift(DriverAgent worker, int shiftIndex, TruckAgent truck, List<string> assignments)
    {
        if (worker == null || truck == null || shiftIndex < 0 || shiftIndex >= ShiftPresetHours.Length)
        {
            return false;
        }

        DebugSetLocalShift(worker, shiftIndex);
        SessionDebugLogger.Log("SHIFT", $"{worker.DriverName} debug-auto assigned to {ShiftNames[shiftIndex]} ({GetShiftRangeLabel(ShiftPresetHours[shiftIndex])}) for {truck.DisplayName}.");
        LogDriverReaction(worker, $"debug-auto assigned to {ShiftNames[shiftIndex]} on {truck.DisplayName}");
        assignments.Add($"{worker.DriverName}: assigned to {truck.DisplayName} + {ShiftNames[shiftIndex]}.");

        if (IsHourInShiftWindow(GetCurrentHour(), ShiftPresetHours[shiftIndex]) &&
            worker.RestPhase == DriverRestPhase.None &&
            !IsDriverBusyWalkPhase(worker))
        {
            StartDriverShiftCommute(worker);
        }

        return true;
    }

    private void DebugSetLocalShift(DriverAgent worker, int shiftIndex)
    {
        if (worker == null || shiftIndex < 0 || shiftIndex >= ShiftPresetHours.Length)
        {
            return;
        }

        SetDriverDutyMode(worker, DriverDutyMode.Local);
        worker.ShiftStartHour = ShiftPresetHours[shiftIndex];
        worker.IsOnActiveShift = false;
        worker.WaitingForShiftAtParking = false;
        worker.NeedsShiftEndReturn = false;
        worker.IsShiftSalaryPending = false;
    }

    private int DebugFindAvailableShiftIndexForTruck(TruckAgent truck)
    {
        bool[] used = new bool[ShiftPresetHours.Length];
        if (truck != null)
        {
            for (int i = 0; i < truck.AssignedDrivers.Count; i++)
            {
                int shiftIndex = DebugGetShiftIndex(truck.AssignedDrivers[i]?.ShiftStartHour ?? -1);
                if (shiftIndex >= 0)
                {
                    used[shiftIndex] = true;
                }
            }
        }

        return DebugFindFirstUnusedShiftIndex(used);
    }

    private static int DebugFindFirstUnusedShiftIndex(bool[] used)
    {
        if (used == null)
        {
            return -1;
        }

        for (int i = 0; i < used.Length; i++)
        {
            if (!used[i])
            {
                return i;
            }
        }

        return -1;
    }

    private static int DebugGetShiftIndex(int shiftStartHour)
    {
        for (int i = 0; i < ShiftPresetHours.Length; i++)
        {
            if (ShiftPresetHours[i] == shiftStartHour)
            {
                return i;
            }
        }

        return -1;
    }

    private DriverAgent DebugFindNextAutoAssignableWorker(System.Predicate<DriverAgent> extraFilter = null)
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (!DebugIsWorkerAutoAssignable(worker))
            {
                continue;
            }

            if (extraFilter != null && !extraFilter(worker))
            {
                continue;
            }

            return worker;
        }

        return null;
    }

    private int DebugCountAutoAssignableWorkers()
    {
        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            if (DebugIsWorkerAutoAssignable(driverAgents[i]))
            {
                count++;
            }
        }

        return count;
    }

    private bool DebugIsWorkerAutoAssignable(DriverAgent worker)
    {
        if (worker == null ||
            worker.DriverObject == null ||
            worker.IsArrivingByBus ||
            IsDriverOnActiveTradeRun(worker) ||
            IsDriverBusDriver(worker) ||
            worker.RestPhase != DriverRestPhase.None ||
            worker.IsOnActiveShift ||
            worker.WaitingForShiftAtParking ||
            worker.NeedsShiftEndReturn ||
            worker.DutyMode != DriverDutyMode.Local ||
            worker.AssignedTruckNumber > 0 ||
            worker.ShiftStartHour >= 0 ||
            worker.AssignedBuildingType.HasValue ||
            IsDriverBusyWalkPhase(worker))
        {
            return false;
        }

        return !IsBusDriverOnActiveRoute(worker);
    }

    private void DebugSetWeather(WeatherState target)
    {
        if (!isWeatherTransitioning && currentWeatherState == target) return;
        if ( isWeatherTransitioning && nextWeatherState   == target) return;

        nextWeatherState          = target;
        weatherTransitionDuration = 30f;
        weatherTransitionTimer    = 0f;
        isWeatherTransitioning    = true;
        weatherHoldTimer          = GetWeatherHoldDuration(target);
        SessionDebugLogger.Log("DEBUG", $"[DBG] Weather forced → {target}.");
    }

    private void DebugSummonWorkerWave(int count)
    {
        if (hiringDriverArrival != null)
        {
            SessionDebugLogger.Log("DEBUG", "[DBG] Worker wave skipped: another hiring bus is already active.");
            return;
        }

        int spawnCount = Mathf.Max(1, count);
        List<DriverAgent> workers = new(spawnCount);
        for (int i = 0; i < spawnCount; i++)
        {
            DriverAgent worker = CreateAndRegisterDriverAgent(spawnInMotel: false);
            workers.Add(worker);
            LogDriverReaction(worker, "debug-summoned and arriving by bus");
        }

        hiringDriverArrival = new HiringDriverArrivalData
        {
            Driver = workers[0],
            IsTutorialWave = false,
            Phase = HiringDriverArrivalPhase.WaitingLaneClear
        };
        hiringDriverArrival.Drivers.AddRange(workers);

        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
        SessionDebugLogger.Log("DEBUG", $"[DBG] Summoned {spawnCount} workers by arrival bus; stagger={HiringBusDisembarkInterval:F2}s.");
        PushFeedEvent(
            $"Debug summoned {spawnCount} workers. Arrival bus is on the way.",
            $"Debug: вызвано рабочих: {spawnCount}. Автобус уже в пути.",
            FeedEventType.Info);
    }
}
