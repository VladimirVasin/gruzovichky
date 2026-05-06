using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class TransportTradeSmokeTests
{
    [Test]
    public void LocalBusRoutePlanner_RequiresAtLeastTwoStops()
    {
        LocalBusNextStopDecision oneStop = LocalBusRoutePlanner.GetNextStop(1, 0, 1);

        Assert.That(oneStop.HasNextStop, Is.False);
        Assert.That(LocalBusRoutePlanner.ShouldReturnToParkingAfterCurrentStop(1, 0, 1), Is.True);
    }

    [Test]
    public void LocalBusRoutePlanner_BouncesAtBothTerminals()
    {
        LocalBusNextStopDecision fromLast = LocalBusRoutePlanner.GetNextStop(4, 3, 1);
        Assert.That(fromLast.HasNextStop, Is.True);
        Assert.That(fromLast.NextStopIndex, Is.EqualTo(2));
        Assert.That(fromLast.TravelDirection, Is.EqualTo(-1));

        LocalBusNextStopDecision fromFirst = LocalBusRoutePlanner.GetNextStop(4, 0, -1);
        Assert.That(fromFirst.NextStopIndex, Is.EqualTo(1));
        Assert.That(fromFirst.TravelDirection, Is.EqualTo(1));
    }

    [Test]
    public void BusStopOrderingService_UsesStableTieBreakers()
    {
        List<BusStopOrderKey> stops = new()
        {
            new BusStopOrderKey(7, 2, new Vector2Int(4, 4)),
            new BusStopOrderKey(2, 1, new Vector2Int(6, 1)),
            new BusStopOrderKey(5, 1, new Vector2Int(6, 1)),
            new BusStopOrderKey(1, 1, new Vector2Int(3, 9))
        };

        Assert.That(BusStopOrderingService.GetOrderedIndices(stops), Is.EqualTo(new[] { 3, 1, 2, 0 }));
    }

    [Test]
    public void LocalBusPassengerService_ChargesFareOnlyWhenBoarding()
    {
        LocalBusPassengerBoardingDecision paid = LocalBusPassengerService.EvaluateBoarding(
            passengerCount: 1,
            passengerCapacity: 5,
            passengerMoney: 3,
            fare: 1,
            fareExempt: false);

        Assert.That(paid.Kind, Is.EqualTo(LocalBusPassengerBoardingDecisionKind.Board));
        Assert.That(paid.FareCharged, Is.EqualTo(1));

        LocalBusPassengerBoardingDecision full = LocalBusPassengerService.EvaluateBoarding(
            passengerCount: 5,
            passengerCapacity: 5,
            passengerMoney: 3,
            fare: 1,
            fareExempt: false);

        Assert.That(full.Kind, Is.EqualTo(LocalBusPassengerBoardingDecisionKind.ContinueWaiting));
        Assert.That(full.FareCharged, Is.EqualTo(0));
    }

    [Test]
    public void TradeDispatchPreconditionService_BlocksActiveRunsAndSellWithoutStock()
    {
        TradeDispatchPreconditionResult activeRun = TradeDispatchPreconditionService.Evaluate(new TradeDispatchPreconditionInput(
            hasActiveRun: true,
            hasDriver: true,
            driverArriving: false,
            driverBusy: false,
            hasTruck: true,
            truckBusy: false,
            truckHasOtherDriver: false,
            highwayConnected: true,
            truckParkedOrDriverOnboard: true,
            isBuyOrder: false,
            treasury: 0,
            price: 50,
            storedResourceAmount: 10,
            quantity: 1,
            truckDisplayName: "Truck #1",
            resourceLabel: "Boards"));

        Assert.That(activeRun.CanDispatch, Is.False);
        Assert.That(activeRun.BlockReason, Is.EqualTo("Another trade run is already active"));

        TradeDispatchPreconditionResult noStock = TradeDispatchPreconditionService.Evaluate(new TradeDispatchPreconditionInput(
            hasActiveRun: false,
            hasDriver: true,
            driverArriving: false,
            driverBusy: false,
            hasTruck: true,
            truckBusy: false,
            truckHasOtherDriver: false,
            highwayConnected: true,
            truckParkedOrDriverOnboard: true,
            isBuyOrder: false,
            treasury: 0,
            price: 50,
            storedResourceAmount: 1,
            quantity: 3,
            truckDisplayName: "Truck #1",
            resourceLabel: "Boards"));

        Assert.That(noStock.CanDispatch, Is.False);
        Assert.That(noStock.BlockReason, Is.EqualTo("Need 3 Boards to sell"));
    }

    [Test]
    public void TradeOrderQueueService_RemovesOnlyRequestedOrder()
    {
        List<GameBootstrap.TradeHudOrder> orders = new()
        {
            TradeOrderQueueService.CreateOrder(10, GameBootstrap.TradeResourceType.Textile, GameBootstrap.TradeOrderType.Buy, 2, targetRegionIndex: 5),
            TradeOrderQueueService.CreateOrder(11, GameBootstrap.TradeResourceType.Boards, GameBootstrap.TradeOrderType.Sell, 4, targetRegionIndex: 6),
            TradeOrderQueueService.CreateOrder(12, GameBootstrap.TradeResourceType.Cotton, GameBootstrap.TradeOrderType.Buy, 1)
        };

        Assert.That(TradeOrderQueueService.RemoveById(orders, 11), Is.EqualTo(1));
        Assert.That(orders, Has.Count.EqualTo(2));
        Assert.That(orders.Exists(order => order.Id == 11), Is.False);
        Assert.That(orders[0].Id, Is.EqualTo(10));
        Assert.That(orders[1].Id, Is.EqualTo(12));
    }

    [Test]
    public void TradeState_StoresPoliciesAndWarehouseProjection()
    {
        TradeState state = new();
        GameBootstrap.TradeResourceType[] resources =
        {
            GameBootstrap.TradeResourceType.Logs,
            GameBootstrap.TradeResourceType.Boards,
            GameBootstrap.TradeResourceType.Cotton,
            GameBootstrap.TradeResourceType.Textile,
            GameBootstrap.TradeResourceType.Furniture
        };

        Assert.That(state.GetStoredResourceAmount(GameBootstrap.TradeResourceType.Logs, warehouseLogs: 7, warehouseBoards: 2), Is.EqualTo(7));
        state.CottonStored = 4;
        Assert.That(state.GetStoredResourceAmount(GameBootstrap.TradeResourceType.Cotton, warehouseLogs: 0, warehouseBoards: 0), Is.EqualTo(4));

        Assert.That(state.TrySetPolicyMode(GameBootstrap.TradeResourceType.Textile, GameBootstrap.TradePolicyMode.BuyUpTo, resources), Is.True);
        Assert.That(state.GetPolicyMode(GameBootstrap.TradeResourceType.Textile, resources), Is.EqualTo(GameBootstrap.TradePolicyMode.BuyUpTo));
        Assert.That(state.AdjustPolicyTarget(GameBootstrap.TradeResourceType.Textile, 3, 0, 99, resources), Is.EqualTo(8));
    }

    [Test]
    public void TradePolicyRuntime_BuildsSellAndBuyDispatchDecisions()
    {
        TradePolicyDispatchDecision sell = TradePolicyRuntime.EvaluateDispatch(
            GameBootstrap.TradeResourceType.Boards,
            GameBootstrap.TradePolicyMode.SellAbove,
            storedAmount: 12,
            targetAmount: 4,
            modeSupported: true,
            hasLandRoute: true,
            hasRiverRoute: false,
            targetRegionIndex: 6,
            cargoCapacity: 5);

        Assert.That(sell.ShouldDispatch, Is.True);
        Assert.That(sell.OrderType, Is.EqualTo(GameBootstrap.TradeOrderType.Sell));
        Assert.That(sell.Amount, Is.EqualTo(5));
        Assert.That(sell.TargetRegionIndex, Is.EqualTo(6));

        TradePolicyDispatchDecision buy = TradePolicyRuntime.EvaluateDispatch(
            GameBootstrap.TradeResourceType.Textile,
            GameBootstrap.TradePolicyMode.BuyUpTo,
            storedAmount: 1,
            targetAmount: 4,
            modeSupported: true,
            hasLandRoute: true,
            hasRiverRoute: false,
            targetRegionIndex: 5,
            cargoCapacity: 5);

        Assert.That(buy.ShouldDispatch, Is.True);
        Assert.That(buy.OrderType, Is.EqualTo(GameBootstrap.TradeOrderType.Buy));
        Assert.That(buy.Amount, Is.EqualTo(3));
        Assert.That(buy.TargetRegionIndex, Is.EqualTo(5));
    }

    [Test]
    public void TradePolicyRuntime_DistinguishesRiverAndMissingLandRoutes()
    {
        TradePolicyDispatchDecision river = TradePolicyRuntime.EvaluateDispatch(
            GameBootstrap.TradeResourceType.Textile,
            GameBootstrap.TradePolicyMode.BuyUpTo,
            storedAmount: 0,
            targetAmount: 5,
            modeSupported: true,
            hasLandRoute: false,
            hasRiverRoute: true,
            targetRegionIndex: -1,
            cargoCapacity: 5);

        Assert.That(river.Kind, Is.EqualTo(TradePolicyDispatchDecisionKind.RiverRoute));
        Assert.That(river.ShouldDispatch, Is.False);

        TradePolicyDispatchDecision missing = TradePolicyRuntime.EvaluateDispatch(
            GameBootstrap.TradeResourceType.Textile,
            GameBootstrap.TradePolicyMode.BuyUpTo,
            storedAmount: 0,
            targetAmount: 5,
            modeSupported: true,
            hasLandRoute: false,
            hasRiverRoute: false,
            targetRegionIndex: -1,
            cargoCapacity: 5);

        Assert.That(missing.Kind, Is.EqualTo(TradePolicyDispatchDecisionKind.MissingRoute));
    }

    [Test]
    public void TradeResourceLedger_AddsConsumesAndCapsStock()
    {
        TradeResourceStock stock = new(logs: 2, boards: 0, cotton: 1, textile: 0, furniture: 0);

        Assert.That(TradeResourceLedger.Add(ref stock, GameBootstrap.TradeResourceType.Logs, 10, capacity: 8), Is.EqualTo(6));
        Assert.That(stock.Logs, Is.EqualTo(8));
        Assert.That(TradeResourceLedger.TryConsume(ref stock, GameBootstrap.TradeResourceType.Logs, 3), Is.True);
        Assert.That(stock.Logs, Is.EqualTo(5));
        Assert.That(TradeResourceLedger.TryConsume(ref stock, GameBootstrap.TradeResourceType.Cotton, 2), Is.False);
        Assert.That(stock.Cotton, Is.EqualTo(1));
    }

    [Test]
    public void DocksTradePolicyRuntime_SelectsRiverBuySellAndReportsSkips()
    {
        DocksTradePolicyResourceState[] buyStates =
        {
            new(
                GameBootstrap.TradeResourceType.Textile,
                GameBootstrap.TradePolicyMode.BuyUpTo,
                targetAmount: 5,
                hasRiverRoute: true,
                missingRiverRouteReason: string.Empty,
                warehouseAmount: 1,
                dockExportAmount: 0,
                dockImportAmount: 1)
        };

        DocksTradePolicyDecision buy = DocksTradePolicyRuntime.FindTrade(
            GameBootstrap.TradeOrderType.Buy,
            buyStates,
            batchSize: 3,
            storageCapacity: 8);

        Assert.That(buy.CanTrade, Is.True);
        Assert.That(buy.ResourceType, Is.EqualTo(GameBootstrap.TradeResourceType.Textile));
        Assert.That(buy.Quantity, Is.EqualTo(3));

        DocksTradePolicyResourceState[] sellStates =
        {
            new(
                GameBootstrap.TradeResourceType.Boards,
                GameBootstrap.TradePolicyMode.SellAbove,
                targetAmount: 4,
                hasRiverRoute: true,
                missingRiverRouteReason: string.Empty,
                warehouseAmount: 3,
                dockExportAmount: 5,
                dockImportAmount: 0)
        };

        DocksTradePolicyDecision sell = DocksTradePolicyRuntime.FindTrade(
            GameBootstrap.TradeOrderType.Sell,
            sellStates,
            batchSize: 3,
            storageCapacity: 8);

        Assert.That(sell.CanTrade, Is.True);
        Assert.That(sell.Quantity, Is.EqualTo(3));

        DocksTradePolicyResourceState[] missingRoute =
        {
            new(
                GameBootstrap.TradeResourceType.Cotton,
                GameBootstrap.TradePolicyMode.BuyUpTo,
                targetAmount: 5,
                hasRiverRoute: false,
                missingRiverRouteReason: "route missing",
                warehouseAmount: 0,
                dockExportAmount: 0,
                dockImportAmount: 0)
        };

        Assert.That(
            DocksTradePolicyRuntime.GetSkipReason(GameBootstrap.TradeOrderType.Buy, missingRoute, storageCapacity: 8),
            Is.EqualTo("Buy Cotton: route missing"));
    }

    [Test]
    public void TradeScreenModel_ProjectsPolicyRowButtonStates()
    {
        TradePolicyRowModel row = TradeScreenModel.CreatePolicyRow(
            GameBootstrap.TradeResourceType.Furniture,
            resourceLabel: "Furniture",
            warehouseAmount: 2,
            selectedMode: GameBootstrap.TradePolicyMode.SellAbove,
            target: 4,
            statusText: "no surplus",
            noTradeLabel: "No trade",
            sellAboveLabel: "Sell surplus",
            buyUpToLabel: "Buy up to",
            noTradeSupported: true,
            sellAboveSupported: true,
            buyUpToSupported: false);

        Assert.That(row.WarehouseAmountText, Is.EqualTo("2"));
        Assert.That(row.TargetText, Is.EqualTo("4"));
        Assert.That(row.SellAboveButton.Selected, Is.True);
        Assert.That(row.BuyUpToButton.Supported, Is.False);
        Assert.That(row.CanDecreaseTarget, Is.True);
        Assert.That(row.CanIncreaseTarget, Is.True);
    }

    [Test]
    public void TradeSimulation_TicksAutoDispatchThroughCoordinator()
    {
        TradeSimulation simulation = new();
        TradeSimulationTickResult waiting = simulation.Tick(new TradeSimulationTickInput(
            isWeekend: false,
            hasActiveTradeRuntime: false,
            dispatchablePolicyCount: 1,
            autoDispatchRetryTimer: 0f,
            deltaTime: 0.5f,
            retryInterval: 2f));

        Assert.That(waiting.ShouldAutoDispatch, Is.False);
        Assert.That(waiting.AutoDispatchRetryTimer, Is.EqualTo(0.5f));

        TradeSimulationTickResult ready = simulation.Tick(new TradeSimulationTickInput(
            isWeekend: false,
            hasActiveTradeRuntime: false,
            dispatchablePolicyCount: 1,
            autoDispatchRetryTimer: 1.8f,
            deltaTime: 0.3f,
            retryInterval: 2f));

        Assert.That(ready.ShouldAutoDispatch, Is.True);
        Assert.That(ready.AutoDispatchRetryTimer, Is.EqualTo(0f));
    }
}
