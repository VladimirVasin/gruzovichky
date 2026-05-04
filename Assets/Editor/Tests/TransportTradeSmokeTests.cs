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
}
