using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private enum TradeRunPhase
    {
        None,
        DriverToParking,
        DrivingToHighway,
        DrivingOffMap,
        OutOfMap,
        ReturningFromOffMap,
        ReturningToWarehouse,
        UnloadingAtWarehouse,
        ReturningToParking
    }

    private sealed class ActiveTradeRunData
    {
        public int DriverId;
        public int TruckNumber;
        public TradeResourceType ResourceType;
        public TradeOrderType OrderType;
        public TradeRunPhase Phase;
        public int Quantity;
        public int Price;
        public float OutOfMapTimer;
        public float OutOfMapDuration;
        public float EdgeTravelDirection;
    }

    private ActiveTradeRunData activeTradeRun;
    private static readonly TradeResourceType[] TradeImportCatalog = { TradeResourceType.Cotton, TradeResourceType.Textile, TradeResourceType.Furniture };
    private static readonly TradeResourceType[] TradeExportCatalog = { TradeResourceType.Logs, TradeResourceType.Boards };

    private bool HasActiveTradeRun()
    {
        return activeTradeRun != null && activeTradeRun.Phase != TradeRunPhase.None;
    }

    private bool IsDriverOnActiveTradeRun(DriverAgent driver)
    {
        return driver != null && HasActiveTradeRun() && activeTradeRun.DriverId == driver.DriverId;
    }

    private bool IsTruckOnActiveTradeRun(TruckAgent truckAgent)
    {
        return truckAgent != null && HasActiveTradeRun() && activeTradeRun.TruckNumber == truckAgent.TruckNumber;
    }

    private bool IsTruckOutOfMapForTrade(TruckAgent truckAgent)
    {
        return truckAgent != null &&
               HasActiveTradeRun() &&
               activeTradeRun.TruckNumber == truckAgent.TruckNumber &&
               activeTradeRun.Phase == TradeRunPhase.OutOfMap;
    }

    private void RemoveCompletedTradeDispatchLedgerEntry(TradeOrderType orderType, TradeResourceType resourceType, int quantity)
    {
        if (orderType != TradeOrderType.Buy)
        {
            return;
        }

        string expectedReason = $"Dispatch trade buy: {GetTradeResourceLabel(resourceType)} x{quantity}";
        for (int i = 0; i < moneyLedgerEntries.Count; i++)
        {
            MoneyLedgerEntry entry = moneyLedgerEntries[i];
            if (entry == null)
            {
                continue;
            }

            if (entry.FromLabel == "Treasury" &&
                entry.ToLabel == "Trade Market" &&
                entry.Reason == expectedReason)
            {
                moneyLedgerEntries.RemoveAt(i);
                return;
            }
        }
    }

    private bool IsTruckInManualTradeEdgePhase(TruckAgent truckAgent)
    {
        return truckAgent != null &&
               HasActiveTradeRun() &&
               activeTradeRun.TruckNumber == truckAgent.TruckNumber &&
               activeTradeRun.Phase == TradeRunPhase.DrivingOffMap;
    }

    private bool ShouldSkipTruckRuntimeForTrade(TruckAgent truckAgent)
    {
        return IsTruckOutOfMapForTrade(truckAgent) || IsTruckInManualTradeEdgePhase(truckAgent);
    }

    private string GetTradeRunStatusLabel()
    {
        if (!HasActiveTradeRun())
        {
            return string.Empty;
        }

        string resourceLabel = GetTradeResourceLabel(activeTradeRun.ResourceType);
        string orderLabel = activeTradeRun.OrderType == TradeOrderType.Buy ? "Buying" : "Selling";
        return activeTradeRun.Phase switch
        {
            TradeRunPhase.DriverToParking => $"{orderLabel} {resourceLabel}: driver walking to parking",
            TradeRunPhase.DrivingToHighway => $"{orderLabel} {resourceLabel}: driving to highway",
            TradeRunPhase.DrivingOffMap => $"{orderLabel} {resourceLabel}: leaving the map",
            TradeRunPhase.OutOfMap => $"{orderLabel} {resourceLabel}: away {Mathf.CeilToInt(activeTradeRun.OutOfMapTimer)}s",
            TradeRunPhase.ReturningFromOffMap => $"{orderLabel} {resourceLabel}: entering from highway",
            TradeRunPhase.ReturningToWarehouse => $"{orderLabel} {resourceLabel}: inbound to warehouse",
            TradeRunPhase.UnloadingAtWarehouse => $"{orderLabel} {resourceLabel}: unloading at warehouse",
            TradeRunPhase.ReturningToParking => $"{orderLabel} {resourceLabel}: returning to parking",
            _ => $"{orderLabel} {resourceLabel}"
        };
    }

    private string GetTradePhaseLabel(TradeRunPhase phase)
    {
        return phase switch
        {
            TradeRunPhase.None => "None",
            TradeRunPhase.DriverToParking => "DriverToParking",
            TradeRunPhase.DrivingToHighway => "DrivingToHighway",
            TradeRunPhase.DrivingOffMap => "DrivingOffMap",
            TradeRunPhase.OutOfMap => "OutOfMap",
            TradeRunPhase.ReturningFromOffMap => "ReturningFromOffMap",
            TradeRunPhase.ReturningToWarehouse => "ReturningToWarehouse",
            TradeRunPhase.UnloadingAtWarehouse => "UnloadingAtWarehouse",
            TradeRunPhase.ReturningToParking => "ReturningToParking",
            _ => phase.ToString()
        };
    }

    private void SetTradeRunPhase(TradeRunPhase nextPhase, string reason)
    {
        if (!HasActiveTradeRun())
        {
            return;
        }

        TradeRunPhase previousPhase = activeTradeRun.Phase;
        activeTradeRun.Phase = nextPhase;
        SessionDebugLogger.Log(
            "TRADE_PHASE",
            $"{GetTradePhaseLabel(previousPhase)} -> {GetTradePhaseLabel(nextPhase)} | {reason}");
    }

    private Vector2Int GetTradeHighwayEntryCell()
    {
        return locations[LocationType.BusStop].Anchor;
    }

    private Vector2Int GetTradeHighwayDepartureCell()
    {
        return new Vector2Int(GetTradeHighwayEntryCell().x, 0);
    }

    private Vector2Int GetTradeHighwayDepartureEdgeCell()
    {
        return new Vector2Int(GridWidth - 1, 0);
    }

    private Vector2Int GetTradeHighwayReturnCell()
    {
        return new Vector2Int(GridWidth - 1, 1);
    }

    private Vector2Int GetTradeHighwayRoadConnectionCell()
    {
        return GetTradeHighwayEntryCell();
    }

    private float GetTradeHighwayLaneWorldZ(float travelDirection)
    {
        return GetCellCenter(travelDirection > 0f ? GetTradeHighwayDepartureCell() : GetTradeHighwayReturnCell()).z;
    }

    private bool IsTradeHighwayLaneClear(float travelDirection)
    {
        bool citySideLane = travelDirection < 0f;
        float laneCheckpointX = travelDirection > 0f
            ? GetTradeHighwayEntryCell().x
            : GetTradeHighwayReturnCell().x;
        float protectedMinX = travelDirection > 0f ? laneCheckpointX - 0.75f : laneCheckpointX - 4.5f;
        float protectedMaxX = travelDirection > 0f ? laneCheckpointX + 4.5f : laneCheckpointX + 0.75f;
        for (int i = 0; i < edgeHighwayBuses.Count; i++)
        {
            EdgeHighwayBusData bus = edgeHighwayBuses[i];
            if (bus == null || bus.RootTransform == null || bus.IsCitySideLane != citySideLane)
            {
                continue;
            }

            if (bus.WorldX >= protectedMinX && bus.WorldX <= protectedMaxX)
            {
                return false;
            }
        }

        return true;
    }

    private bool StartTradeMoveToHighwayEdge(TruckAgent truckAgent)
    {
        Vector2Int cityRoadCell = GetTradeHighwayRoadConnectionCell();
        Vector2Int cityEntryCell = GetTradeHighwayEntryCell();
        Vector2Int departureCell = GetTradeHighwayDepartureCell();
        Vector2Int departureEdgeCell = GetTradeHighwayDepartureEdgeCell();
        Vector2Int pathStartCell = truckAgent.TruckCell;
        bool usedParkingAnchorFallback = false;
        List<Vector2Int> pathToLane = FindPath(pathStartCell, cityRoadCell);
        if (pathToLane == null || pathToLane.Count == 0)
        {
            Vector2Int parkingAnchor = locations[LocationType.Parking].Anchor;
            if (parkingAnchor != pathStartCell)
            {
                pathStartCell = parkingAnchor;
                usedParkingAnchorFallback = true;
                pathToLane = FindPath(pathStartCell, cityRoadCell);
            }
        }

        SessionDebugLogger.Log(
            "TRADE_PATH",
            $"{truckAgent.DisplayName} depart plan: start=({truckAgent.TruckCell.x},{truckAgent.TruckCell.y}), normalizedStart=({pathStartCell.x},{pathStartCell.y}), roadConnection=({cityRoadCell.x},{cityRoadCell.y}), cityEntry=({cityEntryCell.x},{cityEntryCell.y}), laneCell=({departureCell.x},{departureCell.y}), edge=({departureEdgeCell.x},{departureEdgeCell.y}), fallbackToParkingAnchor={(usedParkingAnchorFallback ? "yes" : "no")}.");

        if (pathToLane == null || pathToLane.Count == 0)
        {
            SessionDebugLogger.Log(
                "PATH",
                $"{truckAgent.DisplayName} failed to build trade highway path from ({truckAgent.TruckCell.x},{truckAgent.TruckCell.y}) toward road connection ({cityRoadCell.x},{cityRoadCell.y}).");
            return false;
        }

        if (truckAgent.TruckCell != pathStartCell)
        {
            Vector3 normalizedStartWorld = GetTruckWorldPosition(pathStartCell);
            normalizedStartWorld.y = SampleTerrainHeight(normalizedStartWorld.x, normalizedStartWorld.z) + RoadHeight + 0.12f;
            truckCell = pathStartCell;
            truckTargetWorld = normalizedStartWorld;
            truckSegmentStartWorld = normalizedStartWorld;
            truckSmoothedForward = Vector3.forward;
            if (truckObject != null)
            {
                truckObject.transform.position = normalizedStartWorld;
            }

            SessionDebugLogger.Log(
                "TRADE",
                $"{truckAgent.DisplayName} normalized trade path start from ({truckAgent.TruckCell.x},{truckAgent.TruckCell.y}) to road anchor ({pathStartCell.x},{pathStartCell.y}).");
        }

        activePath.Clear();
        for (int i = 1; i < pathToLane.Count; i++)
        {
            Vector2Int step = pathToLane[i];
            if (activePath.Count == 0 || activePath[activePath.Count - 1] != step)
            {
                activePath.Add(step);
            }
        }

        if (activePath.Count == 0 || activePath[activePath.Count - 1] != cityEntryCell)
        {
            activePath.Add(cityEntryCell);
        }

        if (activePath[activePath.Count - 1] != departureCell)
        {
            activePath.Add(departureCell);
        }

        for (int x = departureCell.x + 1; x <= departureEdgeCell.x; x++)
        {
            Vector2Int step = new Vector2Int(x, departureEdgeCell.y);
            if (activePath.Count == 0 || activePath[activePath.Count - 1] != step)
            {
                activePath.Add(step);
            }
        }

        if (activePath.Count == 0)
        {
            SessionDebugLogger.Log(
                "PATH",
                $"{truckAgent.DisplayName} built an empty trade highway path from ({truckCell.x},{truckCell.y}) toward edge cell ({departureEdgeCell.x},{departureEdgeCell.y}).");
            return false;
        }

        isTruckMoving = true;
        BeginNextTruckSegment(activePath[0]);
        SessionDebugLogger.Log("PATH", $"{truckAgent.DisplayName} started trade highway run from ({truckAgent.TruckCell.x},{truckAgent.TruckCell.y}) to edge cell ({departureEdgeCell.x},{departureEdgeCell.y}) over {activePath.Count} steps.");
        SessionDebugLogger.Log(
            "TRADE_PATH",
            $"{truckAgent.DisplayName} depart path ready: firstStep=({activePath[0].x},{activePath[0].y}), steps={activePath.Count}, laneRow={departureCell.y}.");
        return true;
    }

    private bool StartTradeMoveFromHighwayEdge(TruckAgent truckAgent)
    {
        Vector2Int returnCell = GetTradeHighwayReturnCell();
        Vector2Int cityEntryCell = GetTradeHighwayEntryCell();
        Vector2Int cityRoadCell = GetTradeHighwayRoadConnectionCell();

        SessionDebugLogger.Log(
            "TRADE_PATH",
            $"{truckAgent.DisplayName} return plan: start=({truckAgent.TruckCell.x},{truckAgent.TruckCell.y}), returnLaneStart=({returnCell.x},{returnCell.y}), cityEntry=({cityEntryCell.x},{cityEntryCell.y}), roadConnection=({cityRoadCell.x},{cityRoadCell.y}).");

        activePath.Clear();
        for (int x = returnCell.x - 1; x >= cityEntryCell.x; x--)
        {
            Vector2Int step = new Vector2Int(x, returnCell.y);
            if (activePath.Count == 0 || activePath[activePath.Count - 1] != step)
            {
                activePath.Add(step);
            }
        }

        if (activePath.Count == 0 || activePath[activePath.Count - 1] != cityRoadCell)
        {
            activePath.Add(cityRoadCell);
        }

        if (activePath.Count == 0)
        {
            SessionDebugLogger.Log(
                "PATH",
                $"{truckAgent.DisplayName} built an empty trade return path from ({truckAgent.TruckCell.x},{truckAgent.TruckCell.y}) toward road connection ({cityRoadCell.x},{cityRoadCell.y}).");
            return false;
        }

        isTruckMoving = true;
        BeginNextTruckSegment(activePath[0]);
        SessionDebugLogger.Log(
            "TRADE_PATH",
            $"{truckAgent.DisplayName} return path ready: firstStep=({activePath[0].x},{activePath[0].y}), steps={activePath.Count}, laneRow={returnCell.y}.");
        return true;
    }

    private void StartDriverTradeCommuteToParking(DriverAgent driver)
    {
        TruckAgent assignedTruck = GetAssignedTruckForDriver(driver);
        if (driver == null || assignedTruck == null || driver.DriverObject == null)
        {
            return;
        }

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
        ApplyDriverPose(driver, 0f, 0f);
        driver.WalkPhase = DriverRescuePhase.ToParkingForShift;
        driver.WalkTargetWorld = GetDriverParkingWaitPosition(assignedTruck);
        BuildDriverWalkPath(driver, driver.DriverObject.transform.position, driver.WalkTargetWorld);
        SessionDebugLogger.Log("TRADE", $"{driver.DriverName} started walking from Motel to Parking for Intercity trade.");
    }

    private TradeResourceType[] GetTradeCatalogForCurrentZone(TradeOrderType orderType)
    {
        return orderType == TradeOrderType.Buy ? TradeImportCatalog : TradeExportCatalog;
    }

    private void EnsureTradeSelectionMatchesCurrentZone()
    {
        TradeResourceType[] catalog = GetTradeCatalogForCurrentZone(selectedTradeOrderType);
        if (catalog.Length == 0)
        {
            return;
        }

        for (int i = 0; i < catalog.Length; i++)
        {
            if (catalog[i] == selectedTradeResourceType)
            {
                return;
            }
        }

        selectedTradeResourceType = catalog[0];
    }

    private string GetTradeModeLabel(TradeOrderType orderType)
    {
        return orderType == TradeOrderType.Buy ? "Buy / Imports" : "Sell / Exports";
    }

    private int GetTradeRunQuantity(TradeResourceType resourceType)
    {
        return 1;
    }

    private int GetTradeRunPrice(TradeResourceType resourceType, TradeOrderType orderType)
    {
        return 50;
    }

    private float GetTradeRunDuration(TradeResourceType resourceType)
    {
        return resourceType switch
        {
            TradeResourceType.Logs => 18f,
            TradeResourceType.Boards => 20f,
            TradeResourceType.Cotton => 22f,
            TradeResourceType.Textile => 24f,
            TradeResourceType.Furniture => 26f,
            _ => 20f
        };
    }

    private int GetStoredTradeResourceAmount(TradeResourceType resourceType)
    {
        return resourceType switch
        {
            TradeResourceType.Logs => GetTotalLogsResourceAmount(),
            TradeResourceType.Boards => GetTotalBoardsResourceAmount(),
            TradeResourceType.Cotton => cottonStored,
            TradeResourceType.Textile => textileStored,
            TradeResourceType.Furniture => furnitureStored,
            _ => 0
        };
    }

    private void AddStoredTradeResource(TradeResourceType resourceType, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        switch (resourceType)
        {
            case TradeResourceType.Logs:
                if (locations.TryGetValue(LocationType.Warehouse, out LocationData logsWarehouse))
                {
                    logsWarehouse.LogsStored += amount;
                }
                break;
            case TradeResourceType.Boards:
                if (locations.TryGetValue(LocationType.Warehouse, out LocationData boardsWarehouse))
                {
                    boardsWarehouse.BoardsStored += amount;
                }
                break;
            case TradeResourceType.Cotton:
                cottonStored += amount;
                break;
            case TradeResourceType.Textile:
                textileStored += amount;
                break;
            case TradeResourceType.Furniture:
                furnitureStored += amount;
                break;
        }
    }

    private bool TryConsumeStoredTradeResource(TradeResourceType resourceType, int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        switch (resourceType)
        {
            case TradeResourceType.Logs:
                return TryConsumeLogsStored(amount);
            case TradeResourceType.Boards:
                return TryConsumeBoardsStored(amount);
            case TradeResourceType.Cotton:
                if (cottonStored < amount) return false;
                cottonStored -= amount;
                return true;
            case TradeResourceType.Textile:
                if (textileStored < amount) return false;
                textileStored -= amount;
                return true;
            case TradeResourceType.Furniture:
                if (furnitureStored < amount) return false;
                furnitureStored -= amount;
                return true;
            default:
                return false;
        }
    }

    private bool TryConsumeLogsStored(int amount)
    {
        if (GetTotalLogsResourceAmount() < amount)
        {
            return false;
        }

        amount = ConsumeLocationLogs(LocationType.Warehouse, amount);
        amount = ConsumeLocationLogs(LocationType.Sawmill, amount);
        amount = ConsumeLocationLogs(LocationType.Forest, amount);
        return amount <= 0;
    }

    private int ConsumeLocationLogs(LocationType locationType, int remaining)
    {
        if (remaining <= 0 || !locations.TryGetValue(locationType, out LocationData location))
        {
            return remaining;
        }

        int consumed = Mathf.Min(location.LogsStored, remaining);
        location.LogsStored -= consumed;
        return remaining - consumed;
    }

    private bool TryConsumeBoardsStored(int amount)
    {
        if (GetTotalBoardsResourceAmount() < amount)
        {
            return false;
        }

        amount = ConsumeLocationBoards(LocationType.Warehouse, amount);
        amount = ConsumeLocationBoards(LocationType.Sawmill, amount);
        return amount <= 0;
    }

    private int ConsumeLocationBoards(LocationType locationType, int remaining)
    {
        if (remaining <= 0 || !locations.TryGetValue(locationType, out LocationData location))
        {
            return remaining;
        }

        int consumed = Mathf.Min(location.BoardsStored, remaining);
        location.BoardsStored -= consumed;
        return remaining - consumed;
    }

    private bool TryGetTradeDispatchContext(out DriverAgent driver, out TruckAgent truckAgent, out string blockReason)
    {
        EnsureTradeSelectionMatchesCurrentZone();
        driver = GetIntercityAssignedDriver();
        truckAgent = null;

        if (HasActiveTradeRun())
        {
            blockReason = "Another trade run is already active";
            return false;
        }

        if (driver == null)
        {
            blockReason = "Assign an Intercity driver first";
            return false;
        }

        if (driver.IsArrivingByBus)
        {
            blockReason = "Intercity driver is still arriving";
            return false;
        }

        if (driver.RestPhase != DriverRestPhase.None || IsDriverBusyWalkPhase(driver) || IsDriverIdleConversing(driver))
        {
            blockReason = "Intercity driver is busy";
            return false;
        }

        truckAgent = GetAssignedTruckForDriver(driver);
        if (truckAgent == null)
        {
            blockReason = "Intercity driver needs an assigned truck";
            return false;
        }

        if (truckAgent.CurrentAssignedTrip != TripType.None ||
            truckAgent.CurrentRefuelPhase != RefuelPhase.None ||
            truckAgent.IsTruckMoving ||
            truckAgent.IsTruckInteracting ||
            truckAgent.IsDriverRescueActive)
        {
            blockReason = $"{truckAgent.DisplayName} is busy";
            return false;
        }

        if (truckAgent.Driver != null && truckAgent.Driver != driver)
        {
            blockReason = $"{truckAgent.DisplayName} is using another driver";
            return false;
        }

        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking) ||
            !locations.TryGetValue(LocationType.BusStop, out LocationData busStop) ||
            !HasPath(parking.Anchor, busStop.Anchor))
        {
            blockReason = "Highway access is not connected";
            return false;
        }

        if (truckAgent.TruckCell != parking.Anchor && truckAgent.Driver != driver)
        {
            blockReason = $"{truckAgent.DisplayName} must be parked first";
            return false;
        }

        int quantity = GetTradeRunQuantity(selectedTradeResourceType);
        int price = GetTradeRunPrice(selectedTradeResourceType, selectedTradeOrderType);
        if (selectedTradeOrderType == TradeOrderType.Buy && money < price)
        {
            blockReason = $"Need ${price} Treasury for this trade buy";
            return false;
        }

        if (selectedTradeOrderType == TradeOrderType.Sell && GetStoredTradeResourceAmount(selectedTradeResourceType) < quantity)
        {
            blockReason = $"Need {quantity} {GetTradeResourceLabel(selectedTradeResourceType)} to sell";
            return false;
        }

        blockReason = string.Empty;
        return true;
    }

    private bool BeginTradeRun()
    {
        EnsureTradeSelectionMatchesCurrentZone();
        if (!TryGetTradeDispatchContext(out DriverAgent driver, out TruckAgent truckAgent, out string blockReason))
        {
            tradeDispatchStatusText = blockReason;
            return false;
        }

        int quantity = GetTradeRunQuantity(selectedTradeResourceType);
        int price = GetTradeRunPrice(selectedTradeResourceType, selectedTradeOrderType);
        string resourceLabel = GetTradeResourceLabel(selectedTradeResourceType);

        if (selectedTradeOrderType == TradeOrderType.Buy)
        {
            money -= price;
            RecordMoneyMovement(-price, "Treasury", "Trade Market", $"Dispatch trade buy: {resourceLabel} x{quantity}", money);
        }
        else if (!TryConsumeStoredTradeResource(selectedTradeResourceType, quantity))
        {
            tradeDispatchStatusText = $"Need {quantity} {resourceLabel} to sell";
            return false;
        }

        bool driverNeedsBoarding = truckAgent.Driver != driver;
        if (driverNeedsBoarding)
        {
            truckAgent.IsTruckAutoModeEnabled = false;
            driver.WaitingForShiftAtParking = false;
            driver.IdleWanderPauseTimer = 0f;
            driver.IdleConversationTimer = 0f;
            driver.IdleConversationPartnerId = -1;
        }

        activeTradeRun = new ActiveTradeRunData
        {
            DriverId = driver.DriverId,
            TruckNumber = truckAgent.TruckNumber,
            ResourceType = selectedTradeResourceType,
            OrderType = selectedTradeOrderType,
            Phase = driverNeedsBoarding ? TradeRunPhase.DriverToParking : TradeRunPhase.DrivingToHighway,
            Quantity = quantity,
            Price = price,
            OutOfMapDuration = GetTradeRunDuration(selectedTradeResourceType),
            OutOfMapTimer = GetTradeRunDuration(selectedTradeResourceType),
            EdgeTravelDirection = 1f
        };

        SessionDebugLogger.Log(
            "TRADE_PHASE",
            $"None -> {GetTradePhaseLabel(activeTradeRun.Phase)} | dispatch {(selectedTradeOrderType == TradeOrderType.Buy ? "buy" : "sell")} {resourceLabel} x{quantity} with {driver.DriverName} / {truckAgent.DisplayName}.");

        if (driverNeedsBoarding)
        {
            StartDriverTradeCommuteToParking(driver);
        }

        tradeDispatchStatusText = GetTradeRunStatusLabel();
        isEconomyScreenDirty = true;
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;

        SessionDebugLogger.Log(
            "TRADE",
            $"{driver.DriverName} dispatched on trade run with {truckAgent.DisplayName}: {(selectedTradeOrderType == TradeOrderType.Buy ? "buy" : "sell")} {resourceLabel} x{quantity}.");
        return true;
    }

    private void UpdateActiveTradeRun()
    {
        if (!HasActiveTradeRun())
        {
            return;
        }

        DriverAgent driver = GetDriverAgentById(activeTradeRun.DriverId);
        TruckAgent truckAgent = GetTruckAgent(activeTradeRun.TruckNumber);
        if (driver == null || truckAgent == null)
        {
            tradeDispatchStatusText = "Trade run aborted";
            activeTradeRun = null;
            isEconomyScreenDirty = true;
            return;
        }

        switch (activeTradeRun.Phase)
        {
            case TradeRunPhase.DriverToParking:
                UpdateTradeRunDriverToParking(driver, truckAgent);
                break;
            case TradeRunPhase.DrivingToHighway:
                UpdateTradeRunDrivingToHighway(driver, truckAgent);
                break;
            case TradeRunPhase.DrivingOffMap:
                UpdateTradeRunDrivingOffMap(driver, truckAgent);
                break;
            case TradeRunPhase.OutOfMap:
                UpdateTradeRunOutOfMap(driver, truckAgent);
                break;
            case TradeRunPhase.ReturningFromOffMap:
                UpdateTradeRunReturningFromOffMap(driver, truckAgent);
                break;
            case TradeRunPhase.ReturningToWarehouse:
                UpdateTradeRunReturningToWarehouse(driver, truckAgent);
                break;
            case TradeRunPhase.UnloadingAtWarehouse:
                UpdateTradeRunUnloadingAtWarehouse(driver, truckAgent);
                break;
            case TradeRunPhase.ReturningToParking:
                UpdateTradeRunReturning(driver, truckAgent);
                break;
        }
    }

    private void UpdateTradeRunDriverToParking(DriverAgent driver, TruckAgent truckAgent)
    {
        if (truckAgent.Driver == driver)
        {
            SetTradeRunPhase(TradeRunPhase.DrivingToHighway, $"{driver.DriverName} already boarded {truckAgent.DisplayName}.");
            tradeDispatchStatusText = GetTradeRunStatusLabel();
            return;
        }

        if (driver.WalkPhase == DriverRescuePhase.None && !driver.WaitingForShiftAtParking)
        {
            StartDriverTradeCommuteToParking(driver);
            tradeDispatchStatusText = GetTradeRunStatusLabel();
            return;
        }

        if (driver.WaitingForShiftAtParking)
        {
            TryBoardDriverToAssignedTruck(driver);
            if (truckAgent.Driver == driver)
            {
                SetTradeRunPhase(TradeRunPhase.DrivingToHighway, $"{driver.DriverName} boarded {truckAgent.DisplayName} at Parking.");
                tradeDispatchStatusText = GetTradeRunStatusLabel();
                SessionDebugLogger.Log("TRADE", $"{driver.DriverName} boarded {truckAgent.DisplayName} for Intercity trade.");
            }
        }
    }

    private void UpdateTradeRunDrivingToHighway(DriverAgent driver, TruckAgent truckAgent)
    {
        if (truckAgent.Driver != driver)
        {
            SetTradeRunPhase(TradeRunPhase.DriverToParking, $"{driver.DriverName} was not onboard {truckAgent.DisplayName}; restarting Parking commute.");
            tradeDispatchStatusText = GetTradeRunStatusLabel();
            if (!driver.WaitingForShiftAtParking && driver.WalkPhase == DriverRescuePhase.None)
            {
                StartDriverTradeCommuteToParking(driver);
            }
            return;
        }

        Vector2Int highwayDepartureEdgeCell = GetTradeHighwayDepartureEdgeCell();
        if (truckAgent.TruckCell == highwayDepartureEdgeCell && !truckAgent.IsTruckMoving && !truckAgent.IsTruckInteracting)
        {
            SetTradeRunPhase(TradeRunPhase.OutOfMap, $"{truckAgent.DisplayName} reached edge highway cell ({highwayDepartureEdgeCell.x},{highwayDepartureEdgeCell.y}).");
            tradeDispatchStatusText = GetTradeRunStatusLabel();
            SessionDebugLogger.Log("TRADE", $"{truckAgent.DisplayName} reached the end of the edge highway and is leaving the map.");
            if (truckAgent.TruckObject != null)
            {
                truckAgent.TruckObject.SetActive(false);
            }
            activeTradeRun.OutOfMapTimer = activeTradeRun.OutOfMapDuration;
            isEconomyScreenDirty = true;
            return;
        }

        if (!truckAgent.IsTruckMoving && !truckAgent.IsTruckInteracting)
        {
            LoadTruckState(truckAgent);
            if (!StartTradeMoveToHighwayEdge(truckAgent))
            {
                tradeDispatchStatusText = "Trade route blocked near Parking/Highway";
            }
            SaveTruckState(truckAgent);
        }

        tradeDispatchStatusText = GetTradeRunStatusLabel();
    }

    private void UpdateTradeRunDrivingOffMap(DriverAgent driver, TruckAgent truckAgent)
    {
    }

    private void UpdateTradeRunOutOfMap(DriverAgent driver, TruckAgent truckAgent)
    {
        activeTradeRun.OutOfMapTimer = Mathf.Max(0f, activeTradeRun.OutOfMapTimer - Time.deltaTime * gameSpeedMultiplier);
        tradeDispatchStatusText = GetTradeRunStatusLabel();

        if (activeTradeRun.OutOfMapTimer > 0f)
        {
            return;
        }

        Vector2Int highwayReturnCell = GetTradeHighwayReturnCell();
        activeTradeRun.EdgeTravelDirection = -1f;
        Vector3 spawnPosition = GetTruckWorldPosition(highwayReturnCell);
        spawnPosition.y = SampleTerrainHeight(spawnPosition.x, spawnPosition.z) + RoadHeight + 0.12f;
        truckAgent.TruckCell = highwayReturnCell;
        truckAgent.TruckTargetWorld = spawnPosition;
        truckAgent.TruckSegmentStartWorld = spawnPosition;
        truckAgent.TruckSmoothedForward = Vector3.left;
        truckAgent.IsTruckMoving = false;
        truckAgent.IsTruckInteracting = false;
        truckAgent.IsDriverRescueActive = false;
        truckAgent.ActivePath.Clear();
        if (truckAgent.TruckObject != null)
        {
            truckAgent.TruckObject.SetActive(true);
            truckAgent.TruckObject.transform.position = spawnPosition;
            truckAgent.TruckObject.transform.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
        }

        SetTradeRunPhase(TradeRunPhase.ReturningFromOffMap, $"{truckAgent.DisplayName} re-entered map at highway cell ({highwayReturnCell.x},{highwayReturnCell.y}).");
        tradeDispatchStatusText = GetTradeRunStatusLabel();
        SessionDebugLogger.Log("TRADE", $"{truckAgent.DisplayName} returned to the map from edge highway.");
        isEconomyScreenDirty = true;
    }

    private void UpdateTradeRunReturningFromOffMap(DriverAgent driver, TruckAgent truckAgent)
    {
        Vector2Int cityRoadCell = GetTradeHighwayRoadConnectionCell();
        if (truckAgent.TruckCell == cityRoadCell && !truckAgent.IsTruckMoving && !truckAgent.IsTruckInteracting)
        {
            SetTradeRunPhase(
                activeTradeRun.OrderType == TradeOrderType.Buy ? TradeRunPhase.ReturningToWarehouse : TradeRunPhase.ReturningToParking,
                $"{truckAgent.DisplayName} left highway and reached city road connection ({cityRoadCell.x},{cityRoadCell.y}).");
            tradeDispatchStatusText = GetTradeRunStatusLabel();
            SessionDebugLogger.Log("TRADE", $"{truckAgent.DisplayName} re-entered the playable map from the highway edge.");
            return;
        }

        if (!truckAgent.IsTruckMoving && !truckAgent.IsTruckInteracting)
        {
            LoadTruckState(truckAgent);
            if (!StartTradeMoveFromHighwayEdge(truckAgent))
            {
                tradeDispatchStatusText = "Trade return path blocked near Highway";
            }
            SaveTruckState(truckAgent);
        }

        tradeDispatchStatusText = GetTradeRunStatusLabel();
    }

    private void UpdateTradeRunReturningToWarehouse(DriverAgent driver, TruckAgent truckAgent)
    {
        Vector2Int warehouseAnchor = locations[LocationType.Warehouse].Anchor;
        if (truckAgent.TruckCell == warehouseAnchor && !truckAgent.IsTruckMoving && !truckAgent.IsTruckInteracting)
        {
            LoadTruckState(truckAgent);
            if (TryStartTruckInteraction(TruckInteractionType.TradeUnloadAtWarehouse, LocationType.Warehouse))
            {
                SetTradeRunPhase(TradeRunPhase.UnloadingAtWarehouse, $"{truckAgent.DisplayName} reached Warehouse at ({warehouseAnchor.x},{warehouseAnchor.y}).");
                tradeDispatchStatusText = GetTradeRunStatusLabel();
                SessionDebugLogger.Log("TRADE", $"{truckAgent.DisplayName} started unloading imported goods at Warehouse.");
            }
            SaveTruckState(truckAgent);
            return;
        }

        if (!truckAgent.IsTruckMoving && !truckAgent.IsTruckInteracting)
        {
            LoadTruckState(truckAgent);
            StartMoveTo(warehouseAnchor);
            SaveTruckState(truckAgent);
        }

        tradeDispatchStatusText = GetTradeRunStatusLabel();
    }

    private void UpdateTradeRunUnloadingAtWarehouse(DriverAgent driver, TruckAgent truckAgent)
    {
        if (truckAgent.IsTruckInteracting)
        {
            tradeDispatchStatusText = GetTradeRunStatusLabel();
            return;
        }

        string resourceLabel = GetTradeResourceLabel(activeTradeRun.ResourceType);
        AddStoredTradeResource(activeTradeRun.ResourceType, activeTradeRun.Quantity);
        SessionDebugLogger.Log("TRADE", $"{truckAgent.DisplayName} unloaded imported {resourceLabel} x{activeTradeRun.Quantity} at Warehouse.");
        SetTradeRunPhase(TradeRunPhase.ReturningToParking, $"{truckAgent.DisplayName} finished Warehouse unload.");
        tradeDispatchStatusText = GetTradeRunStatusLabel();
    }

    private void UpdateTradeRunReturning(DriverAgent driver, TruckAgent truckAgent)
    {
        Vector2Int parkingAnchor = locations[LocationType.Parking].Anchor;
        if (truckAgent.TruckCell == parkingAnchor && !truckAgent.IsTruckMoving && !truckAgent.IsTruckInteracting)
        {
            CompleteTradeRun(driver, truckAgent);
            return;
        }

        if (!truckAgent.IsTruckMoving && !truckAgent.IsTruckInteracting)
        {
            LoadTruckState(truckAgent);
            StartMoveTo(parkingAnchor);
            SaveTruckState(truckAgent);
        }

        tradeDispatchStatusText = GetTradeRunStatusLabel();
    }

    private void CompleteTradeRun(DriverAgent driver, TruckAgent truckAgent)
    {
        string resourceLabel = GetTradeResourceLabel(activeTradeRun.ResourceType);
        TradeOrderType completedOrderType = activeTradeRun.OrderType;
        TradeResourceType completedResourceType = activeTradeRun.ResourceType;
        int completedQuantity = activeTradeRun.Quantity;
        if (activeTradeRun.OrderType == TradeOrderType.Sell)
        {
            money += activeTradeRun.Price;
            RecordMoneyMovement(activeTradeRun.Price, "Trade Market", "Treasury", $"Trade sale: {resourceLabel} x{activeTradeRun.Quantity}", money);
            SessionDebugLogger.Log("TRADE", $"{truckAgent.DisplayName} completed sale of {resourceLabel} x{activeTradeRun.Quantity} for ${activeTradeRun.Price}.");
        }

        string completionVerb = activeTradeRun.OrderType == TradeOrderType.Buy ? "Bought" : "Sold";
        tradeDispatchStatusText = $"{completionVerb} {resourceLabel} x{activeTradeRun.Quantity}";
        SessionDebugLogger.Log("TRADE", $"{driver.DriverName} completed trade run with {truckAgent.DisplayName}.");
        StartDriverMotelRest(truckAgent, driver);
        activeTradeRun = null;
        RemoveCompletedTradeDispatchLedgerEntry(completedOrderType, completedResourceType, completedQuantity);
        tradeDispatchStatusText = "Ready to dispatch via edge highway";
        isEconomyScreenDirty = true;
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
    }

    private string GetTradeOfferLabel(TradeResourceType resourceType, TradeOrderType orderType)
    {
        int quantity = GetTradeRunQuantity(resourceType);
        int price = GetTradeRunPrice(resourceType, orderType);
        string action = orderType == TradeOrderType.Buy ? "Import" : "Export";
        return $"{action} x{quantity}  •  ${price}";
    }

    private string GetTradeEtaLabel()
    {
        if (HasActiveTradeRun())
        {
            if (activeTradeRun.Phase == TradeRunPhase.OutOfMap)
            {
                return $"ETA {Mathf.CeilToInt(activeTradeRun.OutOfMapTimer)}s";
            }

            return activeTradeRun.Phase switch
            {
                TradeRunPhase.DrivingToHighway => "ETA: highway departure",
                TradeRunPhase.ReturningToWarehouse => "ETA: warehouse unload",
                TradeRunPhase.UnloadingAtWarehouse => "ETA: unloading",
                TradeRunPhase.ReturningToParking => "ETA: parking return",
                _ => "ETA —"
            };
        }

        return $"ETA {Mathf.CeilToInt(GetTradeRunDuration(selectedTradeResourceType))}s";
    }
}
