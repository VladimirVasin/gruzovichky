using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
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
        if (!locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse))
        {
            AbortActiveTradeRun("Trade run aborted: Warehouse missing", driver, truckAgent);
            return;
        }

        Vector2Int warehouseAnchor = warehouse.Anchor;
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
        if (!locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse))
        {
            AbortActiveTradeRun("Trade run aborted: Warehouse missing", driver, truckAgent);
            return;
        }

        Vector2Int warehouseAnchor = warehouse.Anchor;
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
        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            AbortActiveTradeRun("Trade run aborted: Parking missing", driver, truckAgent);
            return;
        }

        Vector2Int parkingAnchor = parking.Anchor;
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
        tradeDispatchStatusText = "Ready to dispatch via edge highway";
        isEconomyScreenDirty = true;
        isTradeScreenDirty = true;
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
        TryAutoDispatchNextHudOrder();
    }

    private void AbortActiveTradeRun(string statusText, DriverAgent driver = null, TruckAgent truckAgent = null)
    {
        if (!HasActiveTradeRun())
        {
            return;
        }

        bool releasedDriverFromTruck = false;
        if (truckAgent != null)
        {
            LoadTruckState(truckAgent);
            if (truckObject != null && !truckObject.activeSelf)
            {
                Vector2Int returnCell = GetTradeHighwayReturnCell();
                Vector3 returnPosition = GetTruckWorldPosition(returnCell);
                truckObject.SetActive(true);
                truckObject.transform.position = returnPosition;
                truckObject.transform.rotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
                truckCell = returnCell;
                truckTargetWorld = returnPosition;
                truckSegmentStartWorld = returnPosition;
                truckSmoothedForward = Vector3.left;
            }

            CancelLoadedTruckRuntimeOrder(statusText);
            ClearTruckCargo();
            if (driver != null && truckAgent.Driver == driver)
            {
                driver.IsShiftSalaryPending = false;
                driver.NeedsShiftEndReturn = false;
                StartDriverMotelRest(truckAgent, driver);
                releasedDriverFromTruck = true;
            }
            SaveTruckState(truckAgent);
        }

        if (driver != null)
        {
            if (!releasedDriverFromTruck && driver.DriverObject != null)
            {
                if (!driver.DriverObject.activeSelf)
                {
                    Vector3 driverPosition = truckAgent?.TruckObject != null
                        ? truckAgent.TruckObject.transform.position + new Vector3(0.35f, 0f, -0.35f)
                        : driver.MotelIdlePosition;
                    driverPosition.y = SampleTerrainHeight(driverPosition.x, driverPosition.z);
                    driver.DriverObject.SetActive(true);
                    driver.DriverObject.transform.position = driverPosition;
                }

                driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                ApplyDriverPose(driver, 0f, 0f);
            }

            driver.WaitingForShiftAtParking = false;
            driver.IsOnActiveShift = false;
            driver.NeedsShiftEndReturn = false;
            driver.IsShiftSalaryPending = false;
            if (!releasedDriverFromTruck)
            {
                driver.WalkPhase = DriverRescuePhase.None;
                driver.WalkPath.Clear();
                driver.WalkWaypointIndex = 0;
                driver.WalkAnimationTime = 0f;
                driver.LifeGoal = WorkerLifeGoal.Idle;
            }
        }

        tradeDispatchStatusText = statusText;
        SessionDebugLogger.Log("TRADE", statusText);
        activeTradeRun = null;
        isEconomyScreenDirty = true;
        isTradeScreenDirty = true;
        isFleetScreenDirty = true;
        isDriversScreenDirty = true;
        isShiftsScreenDirty = true;
    }

}
