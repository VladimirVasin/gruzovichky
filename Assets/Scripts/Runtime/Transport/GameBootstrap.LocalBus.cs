using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private void SetupLocalBusRuntime()
    {
        CleanupLocalBusRuntime();
        localBusRoute = new LocalBusRouteData
        {
            Speed = EdgeHighwayBusSpeed * LocalBusSpeedMultiplier,
            BobPhase = Random.Range(0f, 10f),
            TravelDirection = 1,
            CurrentStopIndex = -1,
            PassengerCount = 0,
            PassengerCapacity = LocalBusMaxPassengers,
            Bank = 0,
            LastBoardingBlockReason = string.Empty,
            Phase = LocalBusPhase.None
        };
    }

    private int GetLocalBusPassengerCount(DriverAgent driver)
    {
        if (driver == null ||
            localBusRoute == null ||
            localBusRoute.Driver != driver)
        {
            return 0;
        }

        return Mathf.Clamp(localBusRoute.PassengerCount, 0, localBusRoute.PassengerCapacity);
    }

    private int GetLocalBusPassengerCapacity()
    {
        return LocalBusMaxPassengers;
    }

    private int CountActualLocalBusPassengers()
    {
        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            if (driverAgents[i]?.WalkPhase == DriverRescuePhase.RidingLocalBus)
            {
                count++;
            }
        }

        return localBusRoute == null
            ? count
            : Mathf.Clamp(count, 0, localBusRoute.PassengerCapacity);
    }

    private void LogBusBoardingBlockOnce(string reason)
    {
        if (localBusRoute == null)
        {
            return;
        }

        if (localBusRoute.LastBoardingBlockReason == reason)
        {
            return;
        }

        localBusRoute.LastBoardingBlockReason = reason;
        SessionDebugLogger.Log("BUS_SHIFT", reason);
    }

    private void ResetBusBoardingBlockReason()
    {
        if (localBusRoute != null)
        {
            localBusRoute.LastBoardingBlockReason = string.Empty;
        }
    }

    private void CleanupLocalBusRuntime()
    {
        if (localBusRoute?.Driver?.DriverObject != null && !localBusRoute.Driver.DriverObject.activeSelf && locations.ContainsKey(LocationType.Parking))
        {
            localBusRoute.Driver.DriverObject.SetActive(true);
            localBusRoute.Driver.DriverObject.transform.position = GetDriverStandPointNearLocation(LocationType.Parking);
            localBusRoute.Driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            localBusRoute.Driver.WalkPhase = DriverRescuePhase.None;
            localBusRoute.Driver.WaitingForShiftAtParking = false;
        }

        if (localBusRoute?.Bus != null)
        {
            localBusRoute.Bus.Driver = null;
            localBusRoute.Bus.PassengerCount = 0;
            if (localBusRoute.Bus.BusObject != null && locations.ContainsKey(LocationType.Parking))
            {
                localBusRoute.Bus.BusObject.transform.position = GetBusParkingSlotWorldPosition(localBusRoute.Bus.ParkingSlotIndex);
                localBusRoute.Bus.BusObject.transform.rotation = Quaternion.identity;
            }
        }

        localBusRoute = null;
    }

    private bool IsBusDriverOnActiveRoute(DriverAgent driver)
    {
        return driver != null &&
               localBusRoute != null &&
               localBusRoute.Driver == driver &&
               localBusRoute.RootTransform != null &&
               localBusRoute.Phase != LocalBusPhase.None;
    }

    private float GetRealSecondsForGameMinutes(float minutes)
    {
        return DayNightCycleDuration * (minutes / (24f * 60f));
    }

    private Vector3 GetLocalBusParkingWorldPosition()
    {
        return locations.TryGetValue(LocationType.Parking, out LocationData parking)
            ? GetBusRoadWorldPosition(parking.Anchor)
            : Vector3.zero;
    }

    private Vector3 GetBusRoadWorldPosition(Vector2Int cell)
    {
        return GetRoadVehicleWorldPosition(cell.x + 0.5f, cell.y + 0.5f, LocalBusRoadSurfaceLift);
    }

    private Vector3 GetBusRoadWorldPosition(Vector2Int fromCell, Vector2Int toCell)
    {
        Vector3 world = GetBusRoadWorldPosition(toCell);
        if (!IsAnchorCell(fromCell) && !IsAnchorCell(toCell) && roadCells.Contains(toCell) && !IsRoadDeadEnd(toCell))
        {
            world += GetRightHandLaneOffset(toCell - fromCell);
        }

        return world;
    }

    private static Quaternion GetLocalBusFacingRotation(Vector3 flatDirection)
    {
        if (flatDirection.sqrMagnitude <= 0.0001f)
        {
            return Quaternion.identity;
        }

        // Bus mesh in BuildSharedBusVisual is modeled with nose along +X.
        return Quaternion.FromToRotation(Vector3.right, flatDirection.normalized);
    }

    private bool ShouldBusDriverHeadToShift(DriverAgent driver)
    {
        if (driver == null || !IsDriverBusDriver(driver) || driver.IsArrivingByBus || driver.ShiftStartHour < 0)
        {
            return false;
        }

        int minutesUntilShiftStart = GetMinutesUntilShiftStart(driver);
        return minutesUntilShiftStart > 0 && minutesUntilShiftStart <= Mathf.RoundToInt(DriverShiftArrivalLeadHours * 60f);
    }

    private void StartBusDriverShiftCommute(DriverAgent driver)
    {
        if (driver == null || driver.DriverObject == null || !locations.ContainsKey(LocationType.Parking) || localStops.Count == 0)
        {
            return;
        }

        if (IsBusDriverOnActiveRoute(driver))
        {
            SessionDebugLogger.Log("BUS_SHIFT", $"{driver.DriverName} commute to Parking skipped because the worker already controls the active local bus route.");
            return;
        }

        if (!TryConsumeWorkerShiftCommutePathBudget(driver))
        {
            return;
        }

        if (!locations.ContainsKey(LocationType.Parking))
        {
            SessionDebugLogger.Log("BUS_SHIFT", $"{driver.DriverName} cannot start local bus shift: Parking is not built.");
            return;
        }

        InterruptDriverIdleActivityForShift(driver, "Parking");
        if (!driver.DriverObject.activeSelf)
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
        ReleaseBench(driver);
        ApplyDriverPose(driver, 0f, 0f);
        driver.WalkPhase = DriverRescuePhase.ToParkingForShift;
        driver.WalkTargetWorld = GetDriverStandPointNearLocation(LocationType.Parking);
        if (TryStartWorkerPersonalCarTrip(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld, DriverRescuePhase.ToParkingForShift, "local bus shift"))
        {
            SessionDebugLogger.Log("BUS", $"{driver.DriverName} started personal car commute to Parking for bus shift.");
            return;
        }
        if (CanWorkerUsePersonalCar(driver))
        {
            SessionDebugLogger.Log("BUS", $"{driver.DriverName} could not start personal car commute to Parking for bus shift; no car route.");
            return;
        }

        if (!BuildDriverWalkPath(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld))
        {
            driver.WalkPhase = DriverRescuePhase.None;
            driver.WalkTargetWorld = driver.DriverObject.transform.position;
            SessionDebugLogger.Log("BUS", $"{driver.DriverName} could not start commute to Parking for bus shift; no safe walk path.");
            return;
        }
        SessionDebugLogger.Log("BUS", $"{driver.DriverName} started commute to Parking for bus shift.");
    }

    private bool TryBoardBusDriver(DriverAgent driver)
    {
        if (localStops.Count == 0)
        {
            return false;
        }

        if (driver == null || !IsDriverBusDriver(driver) || !driver.WaitingForShiftAtParking || driver.DriverObject == null)
        {
            if (driver != null && IsDriverBusDriver(driver) && driver.WaitingForShiftAtParking)
            {
                LogBusBoardingBlockOnce($"{driver.DriverName} cannot board local bus: driver object missing or state invalid.");
            }
            return false;
        }

        NormalizeLocalStopNumbers();
        if (!locations.ContainsKey(LocationType.Parking))
        {
            LogBusBoardingBlockOnce($"{driver.DriverName} cannot board local bus: Parking is missing.");
            return false;
        }

        if (localBusRoute == null)
        {
            SetupLocalBusRuntime();
        }

        if (localBusRoute.Driver != null && localBusRoute.Driver != driver)
        {
            LogBusBoardingBlockOnce($"{driver.DriverName} cannot board local bus: route is currently controlled by {localBusRoute.Driver.DriverName}.");
            return false;
        }

        if (localBusRoute.Bus == null || localBusRoute.RootTransform == null)
        {
            if (!TryReserveAvailableBusForDriver(driver, out BusAgent busAgent, "local bus shift boarding"))
            {
                LogBusBoardingBlockOnce($"{driver.DriverName} cannot board local bus: no available parked buses.");
                return false;
            }

            localBusRoute.Bus = busAgent;
            localBusRoute.RootTransform = busAgent.BusObject.transform;
            localBusRoute.HeadlightLeftRenderer = busAgent.HeadlightLeftRenderer;
            localBusRoute.HeadlightRightRenderer = busAgent.HeadlightRightRenderer;
            localBusRoute.HeadlightLeftMaterial = busAgent.HeadlightLeftMaterial;
            localBusRoute.HeadlightRightMaterial = busAgent.HeadlightRightMaterial;
            localBusRoute.HeadlightLeft = busAgent.HeadlightLeft;
            localBusRoute.HeadlightRight = busAgent.HeadlightRight;
        }
        else if (localBusRoute.Bus.Driver == null)
        {
            localBusRoute.Bus.Driver = driver;
        }

        localBusRoute.Driver = driver;
        localBusRoute.Speed = EdgeHighwayBusSpeed * LocalBusSpeedMultiplier;
        localBusRoute.BobPhase = Random.Range(0f, 10f);
        localBusRoute.DwellTimer = 0f;
        localBusRoute.Waypoints.Clear();
        localBusRoute.WaypointIndex = 0;
        localBusRoute.CurrentStopIndex = -1;
        localBusRoute.TravelDirection = 1;
        localBusRoute.PassengerCount = 0;
        localBusRoute.PassengerCapacity = LocalBusMaxPassengers;
        localBusRoute.Bank = 0;
        localBusRoute.Phase = LocalBusPhase.ParkedAwaitingShiftStart;
        SyncLocalBusAgentState();
        ResetBusBoardingBlockReason();

        driver.WaitingForShiftAtParking = false;
        driver.DriverObject.SetActive(false);
        driver.WalkPhase = DriverRescuePhase.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;

        Vector3 parkingWorld = GetLocalBusParkingWorldPosition();
        localBusRoute.RootTransform.position = parkingWorld;
        localBusRoute.RootTransform.rotation = Quaternion.identity;
        UpdateLocalBusVisual();

        SessionDebugLogger.Log("BUS", $"{driver.DriverName} boarded {localBusRoute.Bus?.DisplayName ?? "local route bus"} in Parking. Passengers={localBusRoute.PassengerCount}/{localBusRoute.PassengerCapacity}.");
        SessionDebugLogger.Log("BUS_SHIFT", $"{driver.DriverName} local bus boarded and awaiting shift start window.");
        return true;
    }

    private void SyncLocalBusAgentState()
    {
        if (localBusRoute?.Bus == null)
        {
            return;
        }

        localBusRoute.Bus.PassengerCount = localBusRoute.PassengerCount;
        localBusRoute.Bus.PassengerCapacity = localBusRoute.PassengerCapacity;
        localBusRoute.Bus.Bank = localBusRoute.Bank;
    }

    private void UpdateLocalBusRoute()
    {
        if (localBusRoute?.RootTransform == null)
        {
            return;
        }

        DriverAgent driver = localBusRoute.Driver;
        if (driver == null || !IsDriverBusDriver(driver))
        {
            CleanupLocalBusRuntime();
            return;
        }

        float dt = Time.deltaTime * gameSpeedMultiplier;
        switch (localBusRoute.Phase)
        {
            case LocalBusPhase.DrivingRoute:
            case LocalBusPhase.ReturningToParking:
                UpdateLocalBusMovement(dt);
                break;

            case LocalBusPhase.WaitingAtStop:
                LocalBusDwellTick dwellTick = LocalBusRuntimeService.TickDwell(localBusRoute.DwellTimer, dt);
                localBusRoute.DwellTimer = dwellTick.Timer;
                if (dwellTick.IsComplete)
                {
                    if (driver.NeedsShiftEndReturn)
                    {
                        List<LocationData> orderedStops = GetOrderedLocalStops();
                        int currentIndex = Mathf.Clamp(localBusRoute.CurrentStopIndex, 0, Mathf.Max(orderedStops.Count - 1, 0));
                        int stopNumber = orderedStops.Count > 0 ? orderedStops[currentIndex].StopNumber : -1;
                        SessionDebugLogger.Log(
                            "BUS_SHIFT",
                            $"{driver.DriverName} shift-end stop reached at Stop #{stopNumber}: currentIndex={currentIndex}, direction={(localBusRoute.TravelDirection > 0 ? "ascending" : "descending")}, stopCount={orderedStops.Count}, returnToParking=yes.");

                        if (orderedStops.Count > 0)
                        {
                            ReleaseLocalBusPassengersAtCurrentStop(orderedStops[currentIndex], "bus shift ended");
                        }

                        SessionDebugLogger.Log("BUS_SHIFT", $"{driver.DriverName} finished the current stop and is now returning the local bus to Parking.");
                        BeginLocalBusReturnToParking();
                        break;
                    }

                    if (!TryBeginNextLocalBusStopSegment())
                    {
                        SessionDebugLogger.Log("BUS_SHIFT", $"{driver.DriverName} could not continue the local route cycle, forcing return to Parking.");
                        BeginLocalBusReturnToParking();
                    }
                }
                break;

            case LocalBusPhase.ParkedAwaitingShiftStart:
                localBusRoute.RootTransform.position = GetLocalBusParkingWorldPosition();
                break;
        }

        UpdateLocalBusVisual();
    }

    private void UpdateLocalBusMovement(float dt)
    {
        if (localBusRoute.WaypointIndex >= localBusRoute.Waypoints.Count)
        {
            CompleteLocalBusCurrentSegment();
            return;
        }

        Vector3 target = localBusRoute.Waypoints[localBusRoute.WaypointIndex];
        LocalBusMovementStep movement = LocalBusRuntimeService.StepTowardWaypoint(
            localBusRoute.RootTransform.position,
            target,
            localBusRoute.Speed,
            dt,
            SampleRoadSurfaceHeight,
            LocalBusRoadSurfaceLift);
        localBusRoute.RootTransform.position = movement.Position;
        if (movement.HasFacingDirection)
        {
            Quaternion targetRotation = GetLocalBusFacingRotation(movement.FacingDirection);
            float rotationLerp = 1f - Mathf.Exp(-11f * Mathf.Max(0f, dt));
            localBusRoute.RootTransform.rotation = Quaternion.Slerp(localBusRoute.RootTransform.rotation, targetRotation, rotationLerp);
        }

        if (movement.ReachedWaypoint)
        {
            localBusRoute.WaypointIndex++;
            CompleteLocalBusCurrentSegment();
        }
    }

    private void CompleteLocalBusCurrentSegment()
    {
        if (localBusRoute == null)
        {
            return;
        }

        if (localBusRoute.WaypointIndex < localBusRoute.Waypoints.Count)
        {
            return;
        }

        localBusRoute.Waypoints.Clear();
        localBusRoute.WaypointIndex = 0;

        if (localBusRoute.Phase == LocalBusPhase.ReturningToParking)
        {
            DriverAgent driver = localBusRoute.Driver;
            localBusRoute.RootTransform.position = GetLocalBusParkingWorldPosition();
            if (localBusRoute.Bus != null)
            {
                localBusRoute.Bus.BusObject.transform.position = localBusRoute.RootTransform.position;
            }
            if (driver != null && (driver.NeedsShiftEndReturn || !driver.IsOnActiveShift))
            {
                CompleteBusDriverShiftReturn(driver);
            }
            else
            {
                localBusRoute.Phase = LocalBusPhase.ParkedAwaitingShiftStart;
                if (localStops.Count > 0)
                {
                    SessionDebugLogger.Log(
                        "BUS_SHIFT",
                        $"{driver?.DriverName ?? "Bus driver"} returned to Parking and is waiting for the next valid local-bus route start.");
                }
            }

            return;
        }

        List<LocationData> orderedStops = GetOrderedLocalStops();
        if (localBusRoute.CurrentStopIndex < 0 || localBusRoute.CurrentStopIndex >= orderedStops.Count)
        {
            BeginLocalBusReturnToParking();
            return;
        }

        LocationData stop = orderedStops[localBusRoute.CurrentStopIndex];
        localBusRoute.RootTransform.position = GetBusRoadWorldPosition(stop.Anchor);
        localBusRoute.DwellTimer = GetRealSecondsForGameMinutes(LocalBusStopDwellGameMinutes);
        localBusRoute.Phase = LocalBusPhase.WaitingAtStop;
        HandleLocalBusPassengersAtStop(stop);
        SessionDebugLogger.Log("BUS", $"{localBusRoute.Driver?.DriverName ?? "Bus driver"} reached Stop #{stop.StopNumber} and is waiting.");
    }

    private bool ShouldLocalBusReturnToParkingAfterCurrentStop()
    {
        if (localBusRoute == null)
        {
            return true;
        }

        List<LocationData> orderedStops = GetOrderedLocalStops();
        if (orderedStops.Count <= 1)
        {
            return true;
        }

        return LocalBusRoutePlanner.ShouldReturnToParkingAfterCurrentStop(
            orderedStops.Count,
            localBusRoute.CurrentStopIndex,
            localBusRoute.TravelDirection);
    }

    private bool BeginLocalBusRouteFromParking()
    {
        NormalizeLocalStopNumbers();
        List<LocationData> orderedStops = GetOrderedLocalStops();
        if (orderedStops.Count == 0)
        {
            return false;
        }

        if (orderedStops.Count < 2)
        {
            ShowLocalBusStopMinimumHintIfNeeded();
        }

        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            SessionDebugLogger.Log("BUS_SHIFT", $"{localBusRoute?.Driver?.DriverName ?? "Bus driver"} cannot start route from Parking: Parking missing.");
            return false;
        }

        if (orderedStops.Count < 2)
        {
            SessionDebugLogger.Log(
                "BUS_SHIFT",
                $"{localBusRoute?.Driver?.DriverName ?? "Bus driver"} cannot start local bus route: at least 2 local stops are required, currentStops={orderedStops.Count}.");
            return false;
        }

        localBusRoute.TravelDirection = 1;
        return TryBeginFirstReachableLocalBusStop(orderedStops, parking);
    }

    private bool HasWorkingLocalBusStopNetwork()
    {
        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            return false;
        }

        NormalizeLocalStopNumbers();
        List<LocationData> orderedStops = GetOrderedLocalStops();
        if (orderedStops.Count < 2)
        {
            return false;
        }

        for (int i = 0; i < orderedStops.Count; i++)
        {
            LocationData firstStop = orderedStops[i];
            if (!HasPath(parking.Anchor, firstStop.Anchor))
            {
                continue;
            }

            int travelDirection = i >= orderedStops.Count - 1 ? -1 : 1;
            if (HasReachableLocalBusStopAfter(orderedStops, i, travelDirection))
            {
                return true;
            }

            return false;
        }

        return false;
    }

    private bool HasReachableLocalBusStopAfter(List<LocationData> orderedStops, int originIndex, int travelDirection)
    {
        if (orderedStops == null || orderedStops.Count < 2 || originIndex < 0 || originIndex >= orderedStops.Count)
        {
            return false;
        }

        Vector2Int originAnchor = orderedStops[originIndex].Anchor;
        int scanIndex = originIndex;
        int scanDirection = travelDirection;
        int maxAttempts = Mathf.Max(1, orderedStops.Count * 2);

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            LocalBusNextStopDecision decision = LocalBusRoutePlanner.GetNextStop(
                orderedStops.Count,
                scanIndex,
                scanDirection);
            if (!decision.HasNextStop)
            {
                return false;
            }

            scanDirection = decision.TravelDirection;
            scanIndex = decision.NextStopIndex;
            if (scanIndex == originIndex)
            {
                continue;
            }

            if (HasPath(originAnchor, orderedStops[scanIndex].Anchor))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryBeginNextLocalBusStopSegment()
    {
        List<LocationData> orderedStops = GetOrderedLocalStops();
        if (orderedStops.Count == 0)
        {
            BeginLocalBusReturnToParking(logStart: false);
            return true;
        }

        if (orderedStops.Count == 1)
        {
            BeginLocalBusReturnToParking();
            return true;
        }

        return TryBeginNextReachableLocalBusStop(orderedStops);
    }

    private void BeginLocalBusReturnToParking(bool logStart = true)
    {
        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            return;
        }

        Vector2Int startCell = localBusRoute?.RootTransform != null
            ? WorldToCell(localBusRoute.RootTransform.position)
            : parking.Anchor;

        if (logStart)
        {
            SessionDebugLogger.Log("BUS_SHIFT", $"{localBusRoute?.Driver?.DriverName ?? "Bus driver"} starting return-to-parking segment from ({startCell.x},{startCell.y}) to Parking ({parking.Anchor.x},{parking.Anchor.y}).");
        }

        if (!TryBeginLocalBusDriveSegment(startCell, parking.Anchor, LocalBusPhase.ReturningToParking))
        {
            DriverAgent driver = localBusRoute?.Driver;
            if (driver != null)
            {
                CompleteBusDriverShiftReturn(driver);
            }
        }
    }

    private bool TryBeginLocalBusDriveSegment(Vector2Int startCell, Vector2Int endCell, LocalBusPhase phase)
    {
        List<Vector2Int> path = FindPath(startCell, endCell);
        if (path == null || path.Count == 0)
        {
            SessionDebugLogger.Log("BUS", $"Local bus failed to find path from ({startCell.x},{startCell.y}) to ({endCell.x},{endCell.y}).");
            return false;
        }

        localBusRoute.Waypoints.Clear();
        for (int i = 1; i < path.Count; i++)
        {
            localBusRoute.Waypoints.Add(GetBusRoadWorldPosition(path[i - 1], path[i]));
        }

        if (localBusRoute.Waypoints.Count == 0)
        {
            localBusRoute.Waypoints.Add(GetBusRoadWorldPosition(endCell));
        }

        localBusRoute.WaypointIndex = 0;
        localBusRoute.Phase = phase;
        SessionDebugLogger.Log("BUS", $"Local bus started segment {phase} from ({startCell.x},{startCell.y}) to ({endCell.x},{endCell.y}) over {localBusRoute.Waypoints.Count} waypoints.");
        return true;
    }

    private void UpdateLocalBusVisual()
    {
        if (localBusRoute?.RootTransform == null)
        {
            return;
        }

        Vector3 position = localBusRoute.RootTransform.position;
        float bob = Mathf.Sin(Time.time * 3.2f + localBusRoute.BobPhase) * 0.015f;
        position.y = SampleRoadSurfaceHeight(position.x, position.z) + LocalBusRoadSurfaceLift + bob;
        localBusRoute.RootTransform.position = position;

        float darkness = 1f - currentStylizedDaylight;
        bool headlightsOn = darkness > 0.55f;
        float headlightIntensity = headlightsOn ? Mathf.Lerp(0.48f, 1.95f, Mathf.InverseLerp(0.55f, 1f, darkness)) : 0f;
        Color lampColor = Color.Lerp(
            new Color(0.34f, 0.22f, 0.12f),
            new Color(1f, 0.82f, 0.5f),
            Mathf.Clamp01(headlightIntensity / 1.95f));

        if (localBusRoute.HeadlightLeft != null)
        {
            localBusRoute.HeadlightLeft.enabled = headlightsOn;
            localBusRoute.HeadlightLeft.intensity = headlightIntensity;
        }

        if (localBusRoute.HeadlightRight != null)
        {
            localBusRoute.HeadlightRight.enabled = headlightsOn;
            localBusRoute.HeadlightRight.intensity = headlightIntensity;
        }

        if (localBusRoute.HeadlightLeftMaterial != null)
        {
            localBusRoute.HeadlightLeftMaterial.color = lampColor;
        }

        if (localBusRoute.HeadlightRightMaterial != null)
        {
            localBusRoute.HeadlightRightMaterial.color = lampColor;
        }
    }

    private void CompleteBusDriverShiftReturn(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        int transferredBank = 0;
        if (localBusRoute != null &&
            locations.TryGetValue(LocationType.Parking, out LocationData parking) &&
            localBusRoute.Bank > 0)
        {
            transferredBank = localBusRoute.Bank;
            parking.BuildingBank += transferredBank;
            localBusRoute.Bank = 0;
            RecordMoneyMovement(
                0,
                "Local Bus Bank",
                parking.Label,
                "Local bus fares delivered to Parking cash",
                null,
                parking.BuildingBank,
                MoneyAccountKind.BuildingCash,
                MoneyAccountKind.BuildingCash,
                MoneyTransactionReasonKind.TransportFare,
                toOwnerId: parking.InstanceId);
            ApplyTransportFareTaxes(parking, transferredBank, "Local bus fares delivered to Parking cash");
            SpawnMoneyEarnPopup(GetLocationCenter(LocationType.Parking), transferredBank);
            SessionDebugLogger.Log("BUS_ECON", $"{driver.DriverName} delivered ${transferredBank} from the local bus bank to Parking treasury. Parking treasury=${parking.BuildingBank}.");
        }

        bool hasParking = locations.ContainsKey(LocationType.Parking);
        if (localBusRoute?.Bus != null)
        {
            localBusRoute.Bus.Bank = localBusRoute?.Bank ?? 0;
            localBusRoute.Bus.PassengerCount = 0;
            localBusRoute.Bus.Driver = null;
            if (localBusRoute.Bus.BusObject != null && hasParking)
            {
                localBusRoute.Bus.BusObject.transform.position = GetBusParkingSlotWorldPosition(localBusRoute.Bus.ParkingSlotIndex);
                localBusRoute.Bus.BusObject.transform.rotation = Quaternion.identity;
            }
        }

        localBusRoute = null;
        SetupLocalBusRuntime();

        driver.IsOnActiveShift = false;
        driver.WaitingForShiftAtParking = false;
        driver.NeedsShiftEndReturn = false;
        driver.DriverObject.SetActive(true);
        driver.DriverObject.transform.position = hasParking
            ? GetDriverStandPointNearLocation(LocationType.Parking)
            : driver.DriverObject.transform.position;
        driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        driver.WalkAnimationTime = 0f;
        ApplyDriverPose(driver, 0f, 0f);
        PayDriverSalary(driver);
        SessionDebugLogger.Log("BUS", $"{driver.DriverName} parked the local bus in Parking and finished the shift. Bus bank transfer=${transferredBank}.");
        StartWorkerLifeCycleAfterWork(driver, driver.DriverObject.transform.position, "local bus shift");
    }
}
