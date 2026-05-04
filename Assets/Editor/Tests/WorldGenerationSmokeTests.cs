using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class WorldGenerationSmokeTests
{
    private const int GridWidth = 128;
    private const int GridHeight = 128;

    [Test]
    public void WorldLayoutGenerator_ProducesValidSeparatedPlacements()
    {
        for (int seed = 1; seed <= 20; seed++)
        {
            Random.InitState(seed);
            GeneratedWorldLayout layout = WorldLayoutGenerator.Generate(GridWidth, GridHeight);
            List<WorldLocationPlacement> placements = new(layout.GetAllPlacements());

            Assert.That(placements, Has.Count.EqualTo(7), $"seed {seed}");
            foreach (WorldLocationPlacement placement in placements)
            {
                AssertPlacementInsideGrid(placement, seed);
                Assert.That(placement.Contains(placement.Anchor), Is.False, $"anchor must remain outside footprint, seed {seed}");
                Assert.That(placement.Contains(placement.RoadAccess), Is.False, $"road access must remain outside footprint, seed {seed}");
            }

            for (int i = 0; i < placements.Count; i++)
            {
                for (int j = i + 1; j < placements.Count; j++)
                {
                    Assert.That(Overlaps(placements[i], placements[j], padding: 0), Is.False, $"placements overlap, seed {seed}, pair {i}/{j}");
                    Assert.That(placements[i].Contains(placements[j].Anchor), Is.False, $"anchor inside another building, seed {seed}, pair {i}/{j}");
                    Assert.That(placements[i].Contains(placements[j].RoadAccess), Is.False, $"road access inside another building, seed {seed}, pair {i}/{j}");
                }
            }
        }
    }

    [Test]
    public void WorldLayoutGenerator_DebugRoadAccessChainIsBuildable()
    {
        for (int seed = 1; seed <= 20; seed++)
        {
            Random.InitState(seed);
            GeneratedWorldLayout layout = WorldLayoutGenerator.Generate(GridWidth, GridHeight, null, layout => HasDebugRoadChain(layout));

            Assert.That(HasDebugRoadChain(layout), Is.True, $"debug road chain must be buildable, seed {seed}");
        }
    }

    [Test]
    public void WorldLayoutGenerator_UserRoadAccessChainIsBuildable()
    {
        for (int seed = 1; seed <= 20; seed++)
        {
            Random.InitState(seed);
            GeneratedWorldLayout layout = WorldLayoutGenerator.Generate(GridWidth, GridHeight, null, layout => HasUserRoadChain(layout));

            Assert.That(HasUserRoadChain(layout), Is.True, $"user road chain must be buildable, seed {seed}");
        }
    }

    [Test]
    public void WorldLayoutGenerator_DebugStarterRoadNetworkConnectsRequiredDestinations()
    {
        for (int seed = 1; seed <= 20; seed++)
        {
            Random.InitState(seed);
            GeneratedWorldLayout layout = WorldLayoutGenerator.Generate(GridWidth, GridHeight, null, layout => HasDebugRoadChain(layout));
            HashSet<Vector2Int> roadCells = BuildDebugStarterRoadCells(layout);

            Assert.That(IsConnectedByRoad(roadCells, layout.Parking.RoadAccess, layout.GasStation.RoadAccess), Is.True, $"parking -> gas station disconnected, seed {seed}");
            Assert.That(IsConnectedByRoad(roadCells, layout.Parking.RoadAccess, layout.Warehouse.RoadAccess), Is.True, $"parking -> warehouse disconnected, seed {seed}");
            Assert.That(IsConnectedByRoad(roadCells, layout.Parking.RoadAccess, layout.Forest.RoadAccess), Is.True, $"parking -> forest disconnected, seed {seed}");
            Assert.That(IsConnectedByRoad(roadCells, layout.Parking.RoadAccess, layout.Motel.RoadAccess), Is.True, $"parking -> motel disconnected, seed {seed}");
            Assert.That(IsConnectedByRoad(roadCells, layout.Parking.RoadAccess, layout.BusStop.RoadAccess), Is.True, $"parking -> bus stop disconnected, seed {seed}");
        }
    }

    [Test]
    public void BuildingPlacementService_RotatesFootprintAroundExternalAnchor()
    {
        Vector2Int anchor = new(10, 10);

        BuildingPlacementService.GetRotatedFootprint(anchor, 3, 2, 0, out Vector2Int min0, out Vector2Int max0);
        Assert.That(min0, Is.EqualTo(new Vector2Int(9, 11)));
        Assert.That(max0, Is.EqualTo(new Vector2Int(11, 12)));

        BuildingPlacementService.GetRotatedFootprint(anchor, 3, 2, 1, out Vector2Int min1, out Vector2Int max1);
        Assert.That(min1, Is.EqualTo(new Vector2Int(11, 9)));
        Assert.That(max1, Is.EqualTo(new Vector2Int(12, 11)));
    }

    [Test]
    public void BuildingPlacementService_BlocksOccupiedFootprintCells()
    {
        Vector2Int anchor = new(10, 10);
        BuildingPlacementService.GetRotatedFootprint(anchor, 2, 2, 0, out Vector2Int min, out Vector2Int max);

        Assert.That(
            BuildingPlacementService.IsFootprintClear(
                anchor,
                min,
                max,
                cell => cell.x >= 0 && cell.x < GridWidth && cell.y >= 0 && cell.y < GridHeight,
                cell => cell == new Vector2Int(10, 11)),
            Is.False);
    }

    [Test]
    public void LocalBusRoutePlanner_BouncesBetweenTerminalStops()
    {
        LocalBusNextStopDecision toSecond = LocalBusRoutePlanner.GetNextStop(3, 0, 1);
        Assert.That(toSecond.HasNextStop, Is.True);
        Assert.That(toSecond.NextStopIndex, Is.EqualTo(1));
        Assert.That(toSecond.TravelDirection, Is.EqualTo(1));

        LocalBusNextStopDecision turnBack = LocalBusRoutePlanner.GetNextStop(3, 2, 1);
        Assert.That(turnBack.NextStopIndex, Is.EqualTo(1));
        Assert.That(turnBack.TravelDirection, Is.EqualTo(-1));

        Assert.That(LocalBusRoutePlanner.ShouldReturnToParkingAfterCurrentStop(3, 0, -1), Is.True);
        Assert.That(LocalBusRoutePlanner.ShouldReturnToParkingAfterCurrentStop(3, 1, -1), Is.False);
    }

    [Test]
    public void BusStopOrderingService_OrdersByNumberThenAnchor()
    {
        List<BusStopOrderKey> stops = new()
        {
            new BusStopOrderKey(0, 2, new Vector2Int(8, 1)),
            new BusStopOrderKey(1, 1, new Vector2Int(7, 1)),
            new BusStopOrderKey(2, 1, new Vector2Int(3, 9))
        };

        List<int> ordered = BusStopOrderingService.GetOrderedIndices(stops);

        Assert.That(ordered, Is.EqualTo(new[] { 2, 1, 0 }));
    }

    [Test]
    public void LocalBusRuntimeService_MovesAndCompletesDwell()
    {
        LocalBusDwellTick dwell = LocalBusRuntimeService.TickDwell(0.5f, 0.6f);
        Assert.That(dwell.IsComplete, Is.True);
        Assert.That(dwell.Timer, Is.LessThanOrEqualTo(0f));

        LocalBusMovementStep step = LocalBusRuntimeService.StepTowardWaypoint(
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            speed: 2f,
            deltaTime: 0.25f,
            sampleHeight: (_, _) => 3f,
            verticalOffset: 0.2f);

        Assert.That(step.ReachedWaypoint, Is.False);
        Assert.That(step.Position.x, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(step.Position.y, Is.EqualTo(3.2f).Within(0.001f));
        Assert.That(step.HasFacingDirection, Is.True);
    }

    [Test]
    public void TruckRuntimeGuardService_BlocksConflictingTruckStates()
    {
        Assert.That(
            TruckRuntimeGuardService.CanUpdateAssignedTrip(
                hasAssignedTrip: true,
                hasActiveRefuelOrder: false,
                driverIsResting: false,
                driverRescueActive: false,
                truckMoving: false,
                truckInteracting: false),
            Is.True);

        Assert.That(
            TruckRuntimeGuardService.CanUpdateAssignedTrip(
                hasAssignedTrip: true,
                hasActiveRefuelOrder: true,
                driverIsResting: false,
                driverRescueActive: false,
                truckMoving: false,
                truckInteracting: false),
            Is.False);
    }

    [Test]
    public void RoadBuildPlacementService_ResolvesExistingTwoLanePairAndBlocksThirdLane()
    {
        HashSet<Vector2Int> roads = new()
        {
            new Vector2Int(10, 10),
            new Vector2Int(10, 9)
        };

        RoadFootprintResolveResult reuse = RoadBuildPlacementService.ResolveFootprintOffset(
            new Vector2Int(10, 10),
            Vector2Int.right,
            requireNewRoadCell: false,
            GridWidth,
            GridHeight,
            roads,
            edgeHighwayCells: null,
            miscOccupiedCells: null,
            isBlockedLocationCell: _ => false);

        Assert.That(reuse.CanPlace, Is.True);
        Assert.That(reuse.WidthOffset, Is.EqualTo(Vector2Int.down));

        Assert.That(
            RoadBuildPlacementService.WouldCreateThirdParallelRoadLane(new Vector2Int(10, 11), Vector2Int.right, roads),
            Is.True);
    }

    [Test]
    public void RoadBuildPlacementService_BlocksBuildingsAndMiscCells()
    {
        HashSet<Vector2Int> misc = new() { new Vector2Int(3, 2) };

        RoadFootprintResolveResult blockedByBuilding = RoadBuildPlacementService.ResolveFootprintOffset(
            new Vector2Int(2, 2),
            Vector2Int.right,
            requireNewRoadCell: true,
            GridWidth,
            GridHeight,
            roadCells: null,
            edgeHighwayCells: null,
            miscOccupiedCells: misc,
            isBlockedLocationCell: cell => cell == new Vector2Int(2, 2));
        Assert.That(blockedByBuilding.CanPlace, Is.False);

        RoadFootprintResolveResult blockedByMiscSideCell = RoadBuildPlacementService.ResolveFootprintOffset(
            new Vector2Int(3, 3),
            Vector2Int.right,
            requireNewRoadCell: true,
            GridWidth,
            GridHeight,
            roadCells: null,
            edgeHighwayCells: null,
            miscOccupiedCells: misc,
            isBlockedLocationCell: _ => false);
        Assert.That(blockedByMiscSideCell.CanPlace, Is.True);
        Assert.That(blockedByMiscSideCell.WidthOffset, Is.EqualTo(Vector2Int.up));
    }

    [Test]
    public void RoadBuildPlacementService_PathPreviewKeepsTwoLaneFootprintContinuous()
    {
        Vector2Int[] path =
        {
            new(10, 10),
            new(11, 10),
            new(12, 10)
        };
        HashSet<Vector2Int> previewCells = new();

        for (int i = 0; i < path.Length; i++)
        {
            Vector2Int dir = i < path.Length - 1 ? path[i + 1] - path[i] : path[i] - path[i - 1];
            RoadFootprintResolveResult result = RoadBuildPlacementService.ResolveFootprintOffset(
                path[i],
                dir,
                requireNewRoadCell: true,
                GridWidth,
                GridHeight,
                previewCells,
                edgeHighwayCells: null,
                miscOccupiedCells: null,
                isBlockedLocationCell: _ => false);

            Assert.That(result.CanPlace, Is.True, $"path cell {i}");
            previewCells.Add(path[i]);
            previewCells.Add(path[i] + result.WidthOffset);
        }

        Assert.That(previewCells.Contains(new Vector2Int(10, 9)), Is.True);
        Assert.That(previewCells.Contains(new Vector2Int(11, 9)), Is.True);
        Assert.That(previewCells.Contains(new Vector2Int(12, 9)), Is.True);
        Assert.That(previewCells, Has.Count.EqualTo(6));
    }

    [Test]
    public void TruckTripRuntimeService_DrivesTripPhaseDecisions()
    {
        Vector2Int parking = new(1, 1);
        Vector2Int pickup = new(2, 1);
        Vector2Int dropoff = new(3, 1);

        TruckTripRuntimeAction moveToPickup = TruckTripRuntimeService.Evaluate(
            GameBootstrap.TripPhase.ToPickup,
            parking,
            pickup,
            dropoff,
            parking,
            truckInteracting: false,
            queuedInteractionResumed: false);
        Assert.That(moveToPickup.Kind, Is.EqualTo(TruckTripRuntimeActionKind.MoveToPickup));
        Assert.That(moveToPickup.TargetCell, Is.EqualTo(pickup));

        TruckTripRuntimeAction startLoading = TruckTripRuntimeService.Evaluate(
            GameBootstrap.TripPhase.ToPickup,
            pickup,
            pickup,
            dropoff,
            parking,
            truckInteracting: false,
            queuedInteractionResumed: false);
        Assert.That(startLoading.Kind, Is.EqualTo(TruckTripRuntimeActionKind.StartLoading));

        TruckTripRuntimeAction advance = TruckTripRuntimeService.Evaluate(
            GameBootstrap.TripPhase.Loading,
            pickup,
            pickup,
            dropoff,
            parking,
            truckInteracting: false,
            queuedInteractionResumed: false);
        Assert.That(advance.Kind, Is.EqualTo(TruckTripRuntimeActionKind.AdvanceToDropoff));
        Assert.That(advance.NextPhase, Is.EqualTo(GameBootstrap.TripPhase.ToDropoff));
    }

    [Test]
    public void TruckRefuelRuntimeService_DrivesRefuelPhaseDecisions()
    {
        Vector2Int parking = new(1, 1);
        Vector2Int gasStation = new(2, 1);

        TruckRefuelRuntimeAction moveToGas = TruckRefuelRuntimeService.Evaluate(
            GameBootstrap.RefuelPhase.ToGasStation,
            parking,
            gasStation,
            parking,
            truckInteracting: false,
            queuedInteractionResumed: false);
        Assert.That(moveToGas.Kind, Is.EqualTo(TruckRefuelRuntimeActionKind.MoveToGasStation));
        Assert.That(moveToGas.TargetCell, Is.EqualTo(gasStation));

        TruckRefuelRuntimeAction finishRefueling = TruckRefuelRuntimeService.Evaluate(
            GameBootstrap.RefuelPhase.Refueling,
            gasStation,
            gasStation,
            parking,
            truckInteracting: false,
            queuedInteractionResumed: false);
        Assert.That(finishRefueling.Kind, Is.EqualTo(TruckRefuelRuntimeActionKind.AdvanceToParking));
        Assert.That(finishRefueling.NextPhase, Is.EqualTo(GameBootstrap.RefuelPhase.ReturnToParking));
    }

    [Test]
    public void TradeAutoDispatchService_WaitsForRetryIntervalAndSkipsWeekends()
    {
        TradeAutoDispatchTick waiting = TradeAutoDispatchService.Tick(
            isWeekend: false,
            hasActiveRun: false,
            activeOrderCount: 1,
            retryTimer: 0.25f,
            deltaTime: 0.25f,
            retryInterval: 1f);
        Assert.That(waiting.ShouldDispatch, Is.False);
        Assert.That(waiting.RetryTimer, Is.EqualTo(0.5f).Within(0.001f));

        TradeAutoDispatchTick dispatch = TradeAutoDispatchService.Tick(
            isWeekend: false,
            hasActiveRun: false,
            activeOrderCount: 1,
            retryTimer: 0.75f,
            deltaTime: 0.25f,
            retryInterval: 1f);
        Assert.That(dispatch.ShouldDispatch, Is.True);
        Assert.That(dispatch.RetryTimer, Is.EqualTo(0f).Within(0.001f));

        TradeAutoDispatchTick weekend = TradeAutoDispatchService.Tick(
            isWeekend: true,
            hasActiveRun: false,
            activeOrderCount: 1,
            retryTimer: 0.75f,
            deltaTime: 0.25f,
            retryInterval: 1f);
        Assert.That(weekend.ShouldDispatch, Is.False);
        Assert.That(weekend.RetryTimer, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void TradeDispatchPreconditionService_ReturnsUsefulBlockReasons()
    {
        TradeDispatchPreconditionResult noDriver = TradeDispatchPreconditionService.Evaluate(
            new TradeDispatchPreconditionInput(
                hasActiveRun: false,
                hasDriver: false,
                driverArriving: false,
                driverBusy: false,
                hasTruck: false,
                truckBusy: false,
                truckHasOtherDriver: false,
                highwayConnected: true,
                truckParkedOrDriverOnboard: true,
                isBuyOrder: true,
                treasury: 100,
                price: 50,
                storedResourceAmount: 0,
                quantity: 1,
                truckDisplayName: "Truck #1",
                resourceLabel: "Cotton"));
        Assert.That(noDriver.CanDispatch, Is.False);
        Assert.That(noDriver.BlockReason, Is.EqualTo("Assign an Intercity driver first"));

        TradeDispatchPreconditionResult noMoney = TradeDispatchPreconditionService.Evaluate(
            new TradeDispatchPreconditionInput(
                hasActiveRun: false,
                hasDriver: true,
                driverArriving: false,
                driverBusy: false,
                hasTruck: true,
                truckBusy: false,
                truckHasOtherDriver: false,
                highwayConnected: true,
                truckParkedOrDriverOnboard: true,
                isBuyOrder: true,
                treasury: 25,
                price: 50,
                storedResourceAmount: 0,
                quantity: 1,
                truckDisplayName: "Truck #1",
                resourceLabel: "Cotton"));
        Assert.That(noMoney.CanDispatch, Is.False);
        Assert.That(noMoney.BlockReason, Is.EqualTo("Need $50 Treasury for this trade buy"));

        TradeDispatchPreconditionResult ok = TradeDispatchPreconditionService.Evaluate(
            new TradeDispatchPreconditionInput(
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
                storedResourceAmount: 3,
                quantity: 3,
                truckDisplayName: "Truck #1",
                resourceLabel: "Boards"));
        Assert.That(ok.CanDispatch, Is.True);
    }

    [Test]
    public void TradeRunRuntimeService_DrivesTradePhaseDecisions()
    {
        TradeRunDriverParkingAction board = TradeRunRuntimeService.EvaluateDriverToParking(
            driverAlreadyBoarded: true,
            driverWaitingAtParking: false,
            driverCanStartCommute: false,
            isSellOrder: true);
        Assert.That(board.Kind, Is.EqualTo(TradeRunDriverParkingActionKind.Advance));
        Assert.That(board.NextPhase, Is.EqualTo(GameBootstrap.TradeRunPhase.DrivingToWarehouse));

        TradeRunTruckTargetAction arrived = TradeRunRuntimeService.EvaluateTruckTarget(
            currentCell: new Vector2Int(5, 5),
            targetCell: new Vector2Int(5, 5),
            truckMoving: false,
            truckInteracting: false);
        Assert.That(arrived.Kind, Is.EqualTo(TradeRunTruckTargetActionKind.Arrived));

        TradeRunHighwayDepartureAction leave = TradeRunRuntimeService.EvaluateDrivingToHighway(
            driverOnTruck: true,
            driverCanStartCommute: false,
            truckCell: new Vector2Int(10, 0),
            edgeCell: new Vector2Int(10, 0),
            truckMoving: false,
            truckInteracting: false);
        Assert.That(leave.Kind, Is.EqualTo(TradeRunHighwayDepartureActionKind.LeaveMap));

        TradeRunOutOfMapTick tick = TradeRunRuntimeService.TickOutOfMap(
            racingActive: false,
            timer: 0.5f,
            deltaTime: 0.5f,
            gameSpeedMultiplier: 1f);
        Assert.That(tick.ShouldReturn, Is.True);
        Assert.That(tick.Timer, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void LocalizationJsonLoader_ReadsFlatTable()
    {
        Dictionary<string, string> table = LocalizationJsonLoader.ParseFlatJsonObject("{\"Fleet\":\"Автопарк\",\"Line\":\"One\\nTwo\"}");

        Assert.That(table["Fleet"], Is.EqualTo("Автопарк"));
        Assert.That(table["Line"], Is.EqualTo("One\nTwo"));
    }

    [Test]
    public void TradeOrderQueueService_HandlesOrderLifecycle()
    {
        List<GameBootstrap.TradeHudOrder> orders = new()
        {
            TradeOrderQueueService.CreateOrder(1, GameBootstrap.TradeResourceType.Logs, GameBootstrap.TradeOrderType.Buy, 5),
            TradeOrderQueueService.CreateOrder(2, GameBootstrap.TradeResourceType.Boards, GameBootstrap.TradeOrderType.Sell, 2, targetRegionIndex: 6)
        };

        Assert.That(TradeOrderQueueService.TryPeek(orders, out GameBootstrap.TradeHudOrder first), Is.True);
        Assert.That(first.Id, Is.EqualTo(1));
        Assert.That(first.TargetRegionIndex, Is.EqualTo(-1));

        Assert.That(TradeOrderQueueService.RemoveById(orders, 2), Is.EqualTo(1));
        Assert.That(orders, Has.Count.EqualTo(1));
        Assert.That(TradeOrderQueueService.RemoveFirst(orders), Is.True);
        Assert.That(orders, Is.Empty);
        Assert.That(TradeOrderQueueService.TryPeek(orders, out _), Is.False);
    }

    [Test]
    public void LocalBusPassengerService_SeparatesBoardingFareAndFallback()
    {
        LocalBusPassengerBoardingDecision full = LocalBusPassengerService.EvaluateBoarding(
            passengerCount: 5,
            passengerCapacity: 5,
            passengerMoney: 10,
            fare: 1,
            fareExempt: false);
        Assert.That(full.Kind, Is.EqualTo(LocalBusPassengerBoardingDecisionKind.ContinueWaiting));

        LocalBusPassengerBoardingDecision broke = LocalBusPassengerService.EvaluateBoarding(
            passengerCount: 2,
            passengerCapacity: 5,
            passengerMoney: 0,
            fare: 1,
            fareExempt: false);
        Assert.That(broke.Kind, Is.EqualTo(LocalBusPassengerBoardingDecisionKind.FallBackToWalking));

        LocalBusPassengerBoardingDecision exempt = LocalBusPassengerService.EvaluateBoarding(
            passengerCount: 2,
            passengerCapacity: 5,
            passengerMoney: 0,
            fare: 1,
            fareExempt: true);
        Assert.That(exempt.Kind, Is.EqualTo(LocalBusPassengerBoardingDecisionKind.Board));
        Assert.That(exempt.FareCharged, Is.EqualTo(0));
    }

    [Test]
    public void ServiceDecorationStyleService_GivesDistinctNightIdentity()
    {
        ServiceDecorationLightStyle bar = ServiceDecorationStyleService.GetLightStyle(ServiceDecorationKind.Bar);
        ServiceDecorationLightStyle canteen = ServiceDecorationStyleService.GetLightStyle(ServiceDecorationKind.Canteen);
        ServiceDecorationLightStyle gambling = ServiceDecorationStyleService.GetLightStyle(ServiceDecorationKind.GamblingHall);

        Assert.That(bar.Color, Is.Not.EqualTo(canteen.Color));
        Assert.That(gambling.Range, Is.GreaterThan(canteen.Range));
        Assert.That(gambling.Intensity, Is.GreaterThan(bar.Intensity));
    }

    [Test]
    public void MiscDecorationSpawnService_UsesStableChanceBuckets()
    {
        Assert.That(MiscDecorationSpawnService.ChooseKind(0.05f, 0.18f, 0.28f), Is.EqualTo(MiscDecorationKind.FlowerPatch));
        Assert.That(MiscDecorationSpawnService.ChooseKind(0.30f, 0.18f, 0.28f), Is.EqualTo(MiscDecorationKind.BerryBush));
        Assert.That(MiscDecorationSpawnService.ChooseKind(0.90f, 0.18f, 0.28f), Is.EqualTo(MiscDecorationKind.Tree));
    }

    [Test]
    public void TwoLaneRoadGeometry_UsesRightHandLaneOffsets()
    {
        Assert.That(TwoLaneRoadGeometry.GetRightLaneOffset(Vector2Int.right), Is.EqualTo(Vector2Int.down));
        Assert.That(TwoLaneRoadGeometry.GetRightLaneOffset(Vector2Int.left), Is.EqualTo(Vector2Int.up));
        Assert.That(TwoLaneRoadGeometry.GetRightLaneOffset(Vector2Int.up), Is.EqualTo(Vector2Int.right));
        Assert.That(TwoLaneRoadGeometry.GetRightLaneOffset(Vector2Int.down), Is.EqualTo(Vector2Int.left));
    }

    [Test]
    public void TwoLaneRoadGeometry_TurnBoundsCoverBothLanes()
    {
        Vector2Int previous = new(14, 12);
        Vector2Int current = new(15, 12);

        TwoLaneRoadGeometry.GetTurnFillBounds(previous, Vector2Int.up, current, Vector2Int.right, out int minX, out int maxX, out int minY, out int maxY);

        Assert.That(minX, Is.EqualTo(14));
        Assert.That(maxX, Is.EqualTo(15));
        Assert.That(minY, Is.EqualTo(11));
        Assert.That(maxY, Is.EqualTo(12));
    }

    [Test]
    public void RoadMarkingPlanner_DrawsCenterDashOnlyOnPairedLanes()
    {
        HashSet<Vector2Int> roads = new()
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1)
        };

        bool Connects(Vector2Int cell, Vector2Int offset) => roads.Contains(cell + offset);

        Assert.That(RoadMarkingPlanner.TryGetRoadVisualAxis(new Vector2Int(0, 0), Connects, out bool isHorizontal), Is.True);
        Assert.That(isHorizontal, Is.True);
        Assert.That(RoadMarkingPlanner.ShouldDrawTwoCellCenterDash(new Vector2Int(0, 0), true, roads, Connects), Is.True);
        Assert.That(RoadMarkingPlanner.ShouldDrawTwoCellCenterDash(new Vector2Int(0, 0), false, roads, Connects), Is.False);
    }

    private static void AssertPlacementInsideGrid(WorldLocationPlacement placement, int seed)
    {
        Assert.That(IsInside(placement.Min), Is.True, $"min outside grid, seed {seed}");
        Assert.That(IsInside(placement.Max), Is.True, $"max outside grid, seed {seed}");
        Assert.That(IsInside(placement.Anchor), Is.True, $"anchor outside grid, seed {seed}");
        Assert.That(IsInside(placement.RoadAccess), Is.True, $"road access outside grid, seed {seed}");
    }

    private static bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < GridWidth && cell.y >= 0 && cell.y < GridHeight;
    }

    private static bool Overlaps(WorldLocationPlacement a, WorldLocationPlacement b, int padding)
    {
        return a.Min.x - padding <= b.Max.x + padding &&
               a.Max.x + padding >= b.Min.x - padding &&
               a.Min.y - padding <= b.Max.y + padding &&
               a.Max.y + padding >= b.Min.y - padding;
    }

    private static bool HasDebugRoadChain(GeneratedWorldLayout layout)
    {
        return HasWideRoad(layout, layout.Parking.RoadAccess, layout.GasStation.RoadAccess) &&
               HasWideRoad(layout, layout.GasStation.RoadAccess, layout.Warehouse.RoadAccess) &&
               HasWideRoad(layout, layout.Warehouse.RoadAccess, layout.Forest.RoadAccess) &&
               HasWideRoad(layout, layout.Forest.RoadAccess, layout.Sawmill.RoadAccess) &&
               HasWideRoad(layout, layout.Sawmill.RoadAccess, layout.Warehouse.RoadAccess) &&
               HasWideRoad(layout, layout.Warehouse.RoadAccess, layout.Motel.RoadAccess) &&
               HasWideRoad(layout, layout.Motel.RoadAccess, layout.BusStop.RoadAccess);
    }

    private static bool HasUserRoadChain(GeneratedWorldLayout layout)
    {
        return HasWideRoad(layout, layout.Parking.RoadAccess, layout.GasStation.RoadAccess) &&
               HasWideRoad(layout, layout.GasStation.RoadAccess, layout.Warehouse.RoadAccess) &&
               HasWideRoad(layout, layout.Warehouse.RoadAccess, layout.Forest.RoadAccess) &&
               HasWideRoad(layout, layout.Warehouse.RoadAccess, layout.BusStop.RoadAccess);
    }

    private static HashSet<Vector2Int> BuildDebugStarterRoadCells(GeneratedWorldLayout layout)
    {
        HashSet<Vector2Int> roadCells = new();
        Assert.That(AppendWideRoad(layout, roadCells, layout.Parking.RoadAccess, layout.GasStation.RoadAccess), Is.True);
        Assert.That(AppendWideRoad(layout, roadCells, layout.GasStation.RoadAccess, layout.Warehouse.RoadAccess), Is.True);
        Assert.That(AppendWideRoad(layout, roadCells, layout.Warehouse.RoadAccess, layout.Forest.RoadAccess), Is.True);
        Assert.That(AppendWideRoad(layout, roadCells, layout.Forest.RoadAccess, layout.Sawmill.RoadAccess), Is.True);
        Assert.That(AppendWideRoad(layout, roadCells, layout.Sawmill.RoadAccess, layout.Warehouse.RoadAccess), Is.True);
        Assert.That(AppendWideRoad(layout, roadCells, layout.Warehouse.RoadAccess, layout.Motel.RoadAccess), Is.True);
        Assert.That(AppendWideRoad(layout, roadCells, layout.Motel.RoadAccess, layout.BusStop.RoadAccess), Is.True);
        return roadCells;
    }

    private static bool AppendWideRoad(GeneratedWorldLayout layout, ISet<Vector2Int> roadCells, Vector2Int start, Vector2Int goal)
    {
        return WorldLayoutRoadValidator.TryAppendWideRoadPathCells(
            start,
            goal,
            GridWidth,
            GridHeight,
            cell => IsPlacementCell(layout, cell),
            cell => IsAnchorOrAccessCell(layout, cell),
            roadCells);
    }

    private static bool IsConnectedByRoad(ISet<Vector2Int> roadCells, Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> path = GridPathService.FindPath(
            start,
            goal,
            GridPathService.GetCardinalNeighbors,
            cell => roadCells.Contains(cell));
        return path != null && path.Count > 0;
    }

    private static bool HasWideRoad(GeneratedWorldLayout layout, Vector2Int start, Vector2Int goal)
    {
        return WorldLayoutRoadValidator.CanBuildWideRoadPath(
            start,
            goal,
            GridWidth,
            GridHeight,
            cell => IsPlacementCell(layout, cell),
            cell => IsAnchorOrAccessCell(layout, cell));
    }

    private static bool IsPlacementCell(GeneratedWorldLayout layout, Vector2Int cell)
    {
        foreach (WorldLocationPlacement placement in layout.GetAllPlacements())
        {
            if (placement.Anchor == cell || placement.Contains(cell))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAnchorOrAccessCell(GeneratedWorldLayout layout, Vector2Int cell)
    {
        foreach (WorldLocationPlacement placement in layout.GetAllPlacements())
        {
            if (placement.Anchor == cell || placement.RoadAccess == cell)
            {
                return true;
            }
        }

        return false;
    }
}
