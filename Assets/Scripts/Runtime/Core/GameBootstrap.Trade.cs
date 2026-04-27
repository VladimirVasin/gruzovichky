using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    public enum TradeRunPhase
    {
        None,
        DriverToParking,
        DrivingToWarehouse,
        LoadingAtWarehouse,
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
    private float tradeAutoDispatchRetryTimer;
    private const float TradeAutoDispatchRetryInterval = 2f;
    private static readonly TradeResourceType[] TradeImportCatalog = { TradeResourceType.Cotton, TradeResourceType.Textile, TradeResourceType.Furniture, TradeResourceType.Fuel, TradeResourceType.Alcohol, TradeResourceType.Food };
    private static readonly TradeResourceType[] TradeExportCatalog = { TradeResourceType.Logs, TradeResourceType.Boards, TradeResourceType.Furniture };

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
            TradeRunPhase.DriverToParking    => $"{orderLabel} {resourceLabel}: driver walking to parking",
            TradeRunPhase.DrivingToWarehouse => $"{orderLabel} {resourceLabel}: loading at warehouse",
            TradeRunPhase.LoadingAtWarehouse => $"{orderLabel} {resourceLabel}: loading cargo",
            TradeRunPhase.DrivingToHighway   => $"{orderLabel} {resourceLabel}: driving to highway",
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
            TradeRunPhase.None               => "None",
            TradeRunPhase.DriverToParking    => "DriverToParking",
            TradeRunPhase.DrivingToWarehouse => "DrivingToWarehouse",
            TradeRunPhase.LoadingAtWarehouse => "LoadingAtWarehouse",
            TradeRunPhase.DrivingToHighway   => "DrivingToHighway",
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
        return locations[LocationType.IntercityStop].Anchor;
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
        return orderType == TradeOrderType.Buy ? "Buy" : "Sell";
    }

    private int GetTradeRunQuantity(TradeResourceType resourceType)
    {
        return Mathf.Clamp(selectedTradeOrderAmount, 1, 5);
    }

    private static CargoType TradeResourceTypeToCargoType(TradeResourceType resourceType)
    {
        return resourceType switch
        {
            TradeResourceType.Logs      => CargoType.Logs,
            TradeResourceType.Boards    => CargoType.Boards,
            TradeResourceType.Cotton    => CargoType.Cotton,
            TradeResourceType.Textile   => CargoType.Textile,
            TradeResourceType.Furniture => CargoType.Furniture,
            TradeResourceType.Fuel      => CargoType.Fuel,
            TradeResourceType.Alcohol   => CargoType.Alcohol,
            TradeResourceType.Food      => CargoType.Food,
            _                           => CargoType.None
        };
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
        locations.TryGetValue(LocationType.Warehouse, out LocationData wh);
        return resourceType switch
        {
            TradeResourceType.Logs      => GetTotalLogsResourceAmount(),
            TradeResourceType.Boards    => GetTotalBoardsResourceAmount(),
            TradeResourceType.Cotton    => cottonStored,
            TradeResourceType.Textile   => textileStored,
            TradeResourceType.Furniture => furnitureStored,
            TradeResourceType.Fuel      => wh?.FuelStored ?? 0,
            TradeResourceType.Alcohol   => wh?.AlcoholStored ?? 0,
            TradeResourceType.Food      => wh?.FoodStored ?? 0,
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
            case TradeResourceType.Fuel:
                if (locations.TryGetValue(LocationType.Warehouse, out LocationData whFuel))
                    whFuel.FuelStored = Mathf.Min(whFuel.FuelStored + amount, WarehouseMaxFuelStorage);
                break;
            case TradeResourceType.Alcohol:
                if (locations.TryGetValue(LocationType.Warehouse, out LocationData whAlcohol))
                    whAlcohol.AlcoholStored = Mathf.Min(whAlcohol.AlcoholStored + amount, WarehouseMaxAlcoholStorage);
                break;
            case TradeResourceType.Food:
                if (locations.TryGetValue(LocationType.Warehouse, out LocationData whFood))
                    whFood.FoodStored = Mathf.Min(whFood.FoodStored + amount, WarehouseMaxFoodStorage);
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

        int quantity = GetTradeRunQuantity(selectedTradeResourceType);
        int price = GetTradeRunPrice(selectedTradeResourceType, selectedTradeOrderType);
        string resourceLabel = GetTradeResourceLabel(selectedTradeResourceType);
        bool hasDriver = driver != null;
        bool driverArriving = driver != null && driver.IsArrivingByBus;
        bool driverBusy = driver != null && (driver.RestPhase != DriverRestPhase.None || IsDriverBusyWalkPhase(driver) || IsDriverIdleConversing(driver));
        truckAgent = driver != null ? GetAssignedTruckForDriver(driver) : null;
        bool hasTruck = truckAgent != null;
        bool truckBusy = truckAgent != null &&
                         (truckAgent.CurrentAssignedTrip != TripType.None ||
                          truckAgent.CurrentRefuelPhase != RefuelPhase.None ||
                          truckAgent.IsTruckMoving ||
                          truckAgent.IsTruckInteracting ||
                          truckAgent.IsDriverRescueActive);
        bool truckHasOtherDriver = truckAgent != null && truckAgent.Driver != null && truckAgent.Driver != driver;
        bool highwayConnected =
            locations.TryGetValue(LocationType.Parking, out LocationData parking) &&
            locations.TryGetValue(LocationType.IntercityStop, out LocationData busStop) &&
            HasPath(parking.Anchor, busStop.Anchor);
        bool truckParkedOrDriverOnboard = truckAgent != null && (!locations.TryGetValue(LocationType.Parking, out parking) || truckAgent.TruckCell == parking.Anchor || truckAgent.Driver == driver);

        TradeDispatchPreconditionResult result = TradeDispatchPreconditionService.Evaluate(
            new TradeDispatchPreconditionInput(
                HasActiveTradeRun(),
                hasDriver,
                driverArriving,
                driverBusy,
                hasTruck,
                truckBusy,
                truckHasOtherDriver,
                highwayConnected,
                truckParkedOrDriverOnboard,
                selectedTradeOrderType == TradeOrderType.Buy,
                money,
                price,
                GetStoredTradeResourceAmount(selectedTradeResourceType),
                quantity,
                truckAgent?.DisplayName,
                resourceLabel));

        if (!result.CanDispatch)
        {
            blockReason = result.BlockReason;
            return false;
        }

        blockReason = string.Empty;
        return true;
    }

    private void TryAutoDispatchNextHudOrder()
    {
        if (HasActiveTradeRun())
        {
            SessionDebugLogger.Log("TRADE_AUTO", $"Auto-dispatch skipped: active run phase={GetTradePhaseLabel(activeTradeRun.Phase)}, queuedOrders={activeTradeHudOrders.Count}.");
            return;
        }

        if (!TradeOrderQueueService.TryPeek(activeTradeHudOrders, out TradeHudOrder next))
        {
            return;
        }

        selectedTradeResourceType  = next.ResourceType;
        selectedTradeOrderType     = next.OrderType;
        selectedTradeOrderAmount   = Mathf.Clamp(next.Amount, 1, 5);
        SessionDebugLogger.Log(
            "TRADE_AUTO",
            $"Trying queued order #{next.Id}: {next.OrderType} {next.ResourceType} x{next.Amount}, targetRegion={next.TargetRegionIndex}, queue={activeTradeHudOrders.Count}.");
        bool dispatched = BeginTradeRun();
        isEconomyScreenDirty = true;
        if (dispatched)
        {
            tradeAutoDispatchRetryTimer = 0f;
        }
        else
        {
            SessionDebugLogger.Log("TRADE_AUTO", $"Queued order #{next.Id} dispatch blocked: {tradeDispatchStatusText}.");
        }
    }

    private void UpdateTradeAutoDispatch()
    {
        TradeAutoDispatchTick tick = TradeAutoDispatchService.Tick(
            IsWeekend(),
            HasActiveTradeRun(),
            activeTradeHudOrders.Count,
            tradeAutoDispatchRetryTimer,
            Time.deltaTime,
            TradeAutoDispatchRetryInterval);
        tradeAutoDispatchRetryTimer = tick.RetryTimer;
        if (tick.ShouldDispatch)
        {
            TryAutoDispatchNextHudOrder();
        }
    }

    private bool BeginTradeRun()
    {
        EnsureTradeSelectionMatchesCurrentZone();
        if (!TryGetTradeDispatchContext(out DriverAgent driver, out TruckAgent truckAgent, out string blockReason))
        {
            tradeDispatchStatusText = blockReason;
            SessionDebugLogger.Log(
                "TRADE_BLOCK",
                $"Dispatch blocked for {selectedTradeOrderType} {selectedTradeResourceType} x{selectedTradeOrderAmount}: {blockReason}; treasury=${money}, stored={GetStoredTradeResourceAmount(selectedTradeResourceType)}, queue={activeTradeHudOrders.Count}.");
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
        // For Sell: resources are consumed at warehouse loading, not here

        bool driverNeedsBoarding = truckAgent.Driver != driver;
        if (driverNeedsBoarding)
        {
            truckAgent.IsTruckAutoModeEnabled = false;
            driver.WaitingForShiftAtParking = false;
            driver.IdleWanderPauseTimer = 0f;
            driver.IdleConversationTimer = 0f;
            driver.IdleConversationPartnerId = -1;
        }

        TradeRunPhase initialPhase;
        if (driverNeedsBoarding)
            initialPhase = TradeRunPhase.DriverToParking;
        else if (selectedTradeOrderType == TradeOrderType.Sell)
            initialPhase = TradeRunPhase.DrivingToWarehouse;
        else
            initialPhase = TradeRunPhase.DrivingToHighway;

        activeTradeRun = new ActiveTradeRunData
        {
            DriverId = driver.DriverId,
            TruckNumber = truckAgent.TruckNumber,
            ResourceType = selectedTradeResourceType,
            OrderType = selectedTradeOrderType,
            Phase = initialPhase,
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
            $"{driver.DriverName} dispatched on trade run with {truckAgent.DisplayName}: {(selectedTradeOrderType == TradeOrderType.Buy ? "buy" : "sell")} {resourceLabel} x{quantity}; price=${price}; treasury=${money}; truckCargo={truckAgent.TruckCargoType} x{truckAgent.TruckCargoAmount}.");
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
            case TradeRunPhase.DrivingToWarehouse:
                UpdateTradeRunDrivingToWarehouse(driver, truckAgent);
                break;
            case TradeRunPhase.LoadingAtWarehouse:
                UpdateTradeRunLoadingAtWarehouse(driver, truckAgent);
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
        TradeRunDriverParkingAction action = TradeRunRuntimeService.EvaluateDriverToParking(
            truckAgent.Driver == driver,
            driver.WaitingForShiftAtParking,
            driver.WalkPhase == DriverRescuePhase.None && !driver.WaitingForShiftAtParking,
            activeTradeRun.OrderType == TradeOrderType.Sell);

        switch (action.Kind)
        {
            case TradeRunDriverParkingActionKind.Advance:
                SetTradeRunPhase(action.NextPhase, $"{driver.DriverName} already boarded {truckAgent.DisplayName}.");
                tradeDispatchStatusText = GetTradeRunStatusLabel();
                return;

            case TradeRunDriverParkingActionKind.StartCommute:
                StartDriverTradeCommuteToParking(driver);
                tradeDispatchStatusText = GetTradeRunStatusLabel();
                return;

            case TradeRunDriverParkingActionKind.TryBoard:
                TryBoardDriverToAssignedTruck(driver);
                if (truckAgent.Driver == driver)
                {
                    TradeRunPhase nextPhase = TradeRunRuntimeService.GetLoadedNextPhase(activeTradeRun.OrderType == TradeOrderType.Sell);
                    SetTradeRunPhase(nextPhase, $"{driver.DriverName} boarded {truckAgent.DisplayName} at Parking.");
                    tradeDispatchStatusText = GetTradeRunStatusLabel();
                    SessionDebugLogger.Log("TRADE", $"{driver.DriverName} boarded {truckAgent.DisplayName} for Intercity trade.");
                }
                return;

            case TradeRunDriverParkingActionKind.Wait:
            default:
                tradeDispatchStatusText = GetTradeRunStatusLabel();
                return;
        }
    }

    private void UpdateTradeRunDrivingToWarehouse(DriverAgent driver, TruckAgent truckAgent)
    {
        Vector2Int warehouseAnchor = locations[LocationType.Warehouse].Anchor;
        TradeRunTruckTargetAction action = TradeRunRuntimeService.EvaluateTruckTarget(
            truckAgent.TruckCell,
            warehouseAnchor,
            truckAgent.IsTruckMoving,
            truckAgent.IsTruckInteracting);

        if (action.Kind == TradeRunTruckTargetActionKind.Arrived)
        {
            LoadTruckState(truckAgent);
            if (TryStartTruckInteraction(TruckInteractionType.TradeLoadAtWarehouse, LocationType.Warehouse))
            {
                SetTradeRunPhase(TradeRunPhase.LoadingAtWarehouse, $"{truckAgent.DisplayName} reached Warehouse for sell loading.");
            }
            SaveTruckState(truckAgent);
            tradeDispatchStatusText = GetTradeRunStatusLabel();
            return;
        }

        if (action.Kind == TradeRunTruckTargetActionKind.MoveToTarget)
        {
            LoadTruckState(truckAgent);
            StartMoveTo(action.TargetCell);
            SaveTruckState(truckAgent);
        }

        tradeDispatchStatusText = GetTradeRunStatusLabel();
    }

    private void UpdateTradeRunLoadingAtWarehouse(DriverAgent driver, TruckAgent truckAgent)
    {
        if (truckAgent.IsTruckInteracting)
        {
            tradeDispatchStatusText = GetTradeRunStatusLabel();
            return;
        }

        string resourceLabel = GetTradeResourceLabel(activeTradeRun.ResourceType);
        int logsAfter = GetTotalLogsResourceAmount();
        int boardsAfter = GetTotalBoardsResourceAmount();
        Debug.Log($"[TRADE] {truckAgent.DisplayName} finished loading {resourceLabel} x{activeTradeRun.Quantity}. " +
                  $"Truck cargo: {truckAgent.TruckCargoAmount} {truckAgent.TruckCargoType}. " +
                  $"Resource totals after load - Logs:{logsAfter} Boards:{boardsAfter}");
        SessionDebugLogger.Log("TRADE", $"{truckAgent.DisplayName} loaded {resourceLabel} x{activeTradeRun.Quantity} from Warehouse.");
        SetTradeRunPhase(TradeRunPhase.DrivingToHighway, $"{truckAgent.DisplayName} finished Warehouse loading.");
        tradeDispatchStatusText = GetTradeRunStatusLabel();
        isEconomyScreenDirty = true;
    }

    private void UpdateTradeRunDrivingToHighway(DriverAgent driver, TruckAgent truckAgent)
    {
        Vector2Int highwayDepartureEdgeCell = GetTradeHighwayDepartureEdgeCell();
        TradeRunHighwayDepartureAction action = TradeRunRuntimeService.EvaluateDrivingToHighway(
            truckAgent.Driver == driver,
            !driver.WaitingForShiftAtParking && driver.WalkPhase == DriverRescuePhase.None,
            truckAgent.TruckCell,
            highwayDepartureEdgeCell,
            truckAgent.IsTruckMoving,
            truckAgent.IsTruckInteracting);

        switch (action.Kind)
        {
            case TradeRunHighwayDepartureActionKind.RestartDriverCommute:
                SetTradeRunPhase(TradeRunPhase.DriverToParking, $"{driver.DriverName} was not onboard {truckAgent.DisplayName}; restarting Parking commute.");
                tradeDispatchStatusText = GetTradeRunStatusLabel();
                StartDriverTradeCommuteToParking(driver);
                return;

            case TradeRunHighwayDepartureActionKind.LeaveMap:
                SetTradeRunPhase(TradeRunPhase.OutOfMap, $"{truckAgent.DisplayName} reached edge highway cell ({highwayDepartureEdgeCell.x},{highwayDepartureEdgeCell.y}).");
                tradeDispatchStatusText = GetTradeRunStatusLabel();
                SessionDebugLogger.Log("TRADE", $"{truckAgent.DisplayName} reached the end of the edge highway and is leaving the map.");
                if (truckAgent.TruckObject != null)
                {
                    truckAgent.TruckObject.SetActive(false);
                }
                truckAgent.TruckCargoAmount = 0;
                truckAgent.TruckCargoType   = CargoType.None;
                activeTradeRun.OutOfMapTimer = activeTradeRun.OutOfMapDuration;
                isEconomyScreenDirty = true;
                return;

            case TradeRunHighwayDepartureActionKind.StartHighwayMove:
                LoadTruckState(truckAgent);
                if (!StartTradeMoveToHighwayEdge(truckAgent))
                {
                    tradeDispatchStatusText = "Trade route blocked near Parking/Highway";
                }
                SaveTruckState(truckAgent);
                break;
        }

        tradeDispatchStatusText = GetTradeRunStatusLabel();
    }

    private void UpdateTradeRunDrivingOffMap(DriverAgent driver, TruckAgent truckAgent)
    {
    }

    private void UpdateTradeRunOutOfMap(DriverAgent driver, TruckAgent truckAgent)
    {
        TradeRunOutOfMapTick tick = TradeRunRuntimeService.TickOutOfMap(
            isRacingActive,
            activeTradeRun.OutOfMapTimer,
            Time.deltaTime,
            gameSpeedMultiplier);
        activeTradeRun.OutOfMapTimer = tick.Timer;
        tradeDispatchStatusText = GetTradeRunStatusLabel();

        if (!tick.ShouldReturn)
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
        if (activeTradeRun.OrderType == TradeOrderType.Buy)
        {
            truckAgent.TruckCargoAmount = activeTradeRun.Quantity;
            truckAgent.TruckCargoType = TradeResourceTypeToCargoType(activeTradeRun.ResourceType);
        }
        if (truckAgent.TruckObject != null)
        {
            truckAgent.TruckObject.SetActive(true);
            truckAgent.TruckObject.transform.position = spawnPosition;
            truckAgent.TruckObject.transform.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
        }

        SetTradeRunPhase(TradeRunPhase.ReturningFromOffMap, $"{truckAgent.DisplayName} re-entered map at highway cell ({highwayReturnCell.x},{highwayReturnCell.y}).");
        tradeDispatchStatusText = GetTradeRunStatusLabel();
        SessionDebugLogger.Log(
            "TRADE",
            $"{truckAgent.DisplayName} returned to the map from edge highway with cargo {truckAgent.TruckCargoAmount} {truckAgent.TruckCargoType}.");
        isEconomyScreenDirty = true;
        isFleetScreenDirty = true;
    }

    private void UpdateTradeRunReturningFromOffMap(DriverAgent driver, TruckAgent truckAgent)
    {
        Vector2Int cityRoadCell = GetTradeHighwayRoadConnectionCell();
        TradeRunTruckTargetAction action = TradeRunRuntimeService.EvaluateTruckTarget(
            truckAgent.TruckCell,
            cityRoadCell,
            truckAgent.IsTruckMoving,
            truckAgent.IsTruckInteracting);

        if (action.Kind == TradeRunTruckTargetActionKind.Arrived)
        {
            SetTradeRunPhase(
                TradeRunRuntimeService.GetReturnEntryNextPhase(activeTradeRun.OrderType == TradeOrderType.Buy),
                $"{truckAgent.DisplayName} left highway and reached city road connection ({cityRoadCell.x},{cityRoadCell.y}).");
            tradeDispatchStatusText = GetTradeRunStatusLabel();
            SessionDebugLogger.Log("TRADE", $"{truckAgent.DisplayName} re-entered the playable map from the highway edge.");
            return;
        }

        if (action.Kind == TradeRunTruckTargetActionKind.MoveToTarget)
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
        TradeRunTruckTargetAction action = TradeRunRuntimeService.EvaluateTruckTarget(
            truckAgent.TruckCell,
            warehouseAnchor,
            truckAgent.IsTruckMoving,
            truckAgent.IsTruckInteracting);

        if (action.Kind == TradeRunTruckTargetActionKind.Arrived)
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

        if (action.Kind == TradeRunTruckTargetActionKind.MoveToTarget)
        {
            LoadTruckState(truckAgent);
            StartMoveTo(action.TargetCell);
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
        TradeRunTruckTargetAction action = TradeRunRuntimeService.EvaluateTruckTarget(
            truckAgent.TruckCell,
            parkingAnchor,
            truckAgent.IsTruckMoving,
            truckAgent.IsTruckInteracting);

        if (action.Kind == TradeRunTruckTargetActionKind.Arrived)
        {
            CompleteTradeRun(driver, truckAgent);
            return;
        }

        if (action.Kind == TradeRunTruckTargetActionKind.MoveToTarget)
        {
            LoadTruckState(truckAgent);
            StartMoveTo(action.TargetCell);
            SaveTruckState(truckAgent);
        }

        tradeDispatchStatusText = GetTradeRunStatusLabel();
    }

    private void CompleteTradeRun(DriverAgent driver, TruckAgent truckAgent)
    {
        if (racingBonusEarned > 0)
        {
            money += racingBonusEarned;
            RecordMoneyMovement(racingBonusEarned, "Racing Bonus", "Treasury", "Intercity racing bonus", money);
            ApplyWorkerRaceRushEffect(driver);
            racingBonusEarned = 0;
        }

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
        ApplyWorkerRoadFatigueEffect(driver);
        SessionDebugLogger.Log("TRADE", $"{driver.DriverName} completed trade run with {truckAgent.DisplayName}.");
        PushFeedEvent(
            activeTradeRun.OrderType == TradeOrderType.Buy
                ? $"Trade run complete: bought {resourceLabel} x{activeTradeRun.Quantity}."
                : $"Trade run complete: sold {resourceLabel} x{activeTradeRun.Quantity} for ${activeTradeRun.Price}.",
            activeTradeRun.OrderType == TradeOrderType.Buy
                ? $"Торговый рейс завершён: куплено {L(resourceLabel)} x{activeTradeRun.Quantity}."
                : $"Торговый рейс завершён: продано {L(resourceLabel)} x{activeTradeRun.Quantity} за ${activeTradeRun.Price}.",
            FeedEventType.Money);
        StartDriverMotelRest(truckAgent, driver);
        activeTradeRun = null;
        RemoveCompletedTradeDispatchLedgerEntry(completedOrderType, completedResourceType, completedQuantity);
        TradeOrderQueueService.RemoveFirst(activeTradeHudOrders);
        tradeDispatchStatusText = "Ready to dispatch via edge highway";
        isEconomyScreenDirty = true;
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
        TryAutoDispatchNextHudOrder();
    }

    private string GetTradeOfferLabel(TradeResourceType resourceType, TradeOrderType orderType)
    {
        int quantity = GetTradeRunQuantity(resourceType);
        int price = GetTradeRunPrice(resourceType, orderType);
        string action = orderType == TradeOrderType.Buy ? "Import" : "Export";
        return $"{action} x{quantity}  \u2022  ${price}";
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
                _ => "ETA \u2014"
            };
        }

        return $"ETA {Mathf.CeilToInt(GetTradeRunDuration(selectedTradeResourceType))}s";
    }
}
