using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const float RegionalMerchantTruckSpeed = 3.05f;
    private const float RegionalMerchantTradeDuration = 2.4f;

    private enum RegionalLandTradePhase
    {
        None,
        ArrivingFromEdge,
        DrivingToWarehouse,
        TradingAtWarehouse,
        DrivingToEdge,
        DepartingToEdge
    }

    private sealed class ActiveRegionalLandTrade
    {
        public RegionalCityData City;
        public TradeResourceType ResourceType;
        public TradeOrderType OrderType;
        public int Quantity;
        public int Price;
        public RegionalLandTradePhase Phase;
        public readonly List<Vector2Int> Path = new();
        public int PathIndex;
        public float TradeTimer;
        public Vector3 EdgeSpawnWorld;
        public Vector3 EdgeExitWorld;
        public GameObject TruckObject;
    }

    private ActiveRegionalLandTrade activeRegionalLandTrade;

    private void UpdateRegionalTradeRuntime()
    {
        UpdateRegionalLandTradeRuntime();
    }

    private bool HasActiveRegionalLandTrade()
    {
        return activeRegionalLandTrade != null && activeRegionalLandTrade.Phase != RegionalLandTradePhase.None;
    }

    private bool TryDispatchRegionalLandTrade(TradeHudOrder request)
    {
        if (request == null)
        {
            return false;
        }

        if (HasActiveRegionalLandTrade())
        {
            tradeDispatchStatusText = "Regional land trade already active";
            SessionDebugLogger.Log("TRADE_LAND", $"Dispatch blocked: active land trade with {activeRegionalLandTrade.City?.NameEn}.");
            return false;
        }

        if (!locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse))
        {
            tradeDispatchStatusText = "Build Warehouse for land trade";
            SessionDebugLogger.Log("TRADE_LAND", $"Dispatch blocked: no Warehouse for {request.OrderType} {request.ResourceType}.");
            return false;
        }

        RegionalCityData city = request.TargetRegionIndex >= 0
            ? GetRegionalCity(request.TargetRegionIndex)
            : null;
        if (city == null ||
            !city.TradeRouteBuilt ||
            city.RouteMode != RegionalTradeRouteMode.Land ||
            !TryFindBuiltRegionalTradeRoute(request.ResourceType, request.OrderType, RegionalTradeRouteMode.Land, out city))
        {
            string reason = DescribeRegionalTradeRouteAvailability(request.ResourceType, request.OrderType, RegionalTradeRouteMode.Land);
            tradeDispatchStatusText = reason;
            SessionDebugLogger.Log("TRADE_LAND", $"Dispatch blocked for {request.OrderType} {request.ResourceType}: {reason}.");
            return false;
        }

        Vector2Int roadConnection = GetTradeHighwayRoadConnectionCell();
        List<Vector2Int> path = FindPath(roadConnection, warehouse.Anchor);
        if (path == null || path.Count < 2)
        {
            tradeDispatchStatusText = "Warehouse is not connected to land route";
            SessionDebugLogger.Log("TRADE_LAND", $"Dispatch blocked: no path from highway connection ({roadConnection.x},{roadConnection.y}) to Warehouse ({warehouse.Anchor.x},{warehouse.Anchor.y}).");
            return false;
        }

        int quantity = Mathf.Clamp(request.Amount, 1, TruckCargoCapacity);
        int price = GetTradeResourceUnitPrice(request.ResourceType, request.OrderType) * quantity;
        if (request.OrderType == TradeOrderType.Sell && GetWarehouseTradeResourceAmount(request.ResourceType) < quantity)
        {
            tradeDispatchStatusText = $"Need {quantity} {GetTradeResourceLabel(request.ResourceType)} to sell";
            SessionDebugLogger.Log("TRADE_LAND", $"Dispatch blocked: Warehouse has {GetWarehouseTradeResourceAmount(request.ResourceType)} {request.ResourceType}, need {quantity}.");
            return false;
        }

        ActiveRegionalLandTrade trade = new()
        {
            City = city,
            ResourceType = request.ResourceType,
            OrderType = request.OrderType,
            Quantity = quantity,
            Price = price,
            Phase = RegionalLandTradePhase.ArrivingFromEdge,
            PathIndex = 0,
            EdgeSpawnWorld = GetRoadVehicleWorldPosition(GridWidth + 4.0f, roadConnection.y + 0.5f, TruckSegmentStartLift),
            EdgeExitWorld = GetRoadVehicleWorldPosition(GridWidth + 4.5f, roadConnection.y + 0.5f, TruckSegmentStartLift)
        };
        trade.Path.AddRange(path);
        trade.TruckObject = CreateRegionalMerchantTruck();
        trade.TruckObject.transform.position = trade.EdgeSpawnWorld;
        activeRegionalLandTrade = trade;
        tradeDispatchStatusText = $"Land trade inbound: {GetTradeResourceLabel(request.ResourceType)} x{quantity}";
        SessionDebugLogger.Log("TRADE_LAND", $"Dispatched merchant truck from {city.NameEn}: {request.OrderType} {request.ResourceType} x{quantity}, price=${price}, pathSteps={path.Count}.");
        return true;
    }

    private void UpdateRegionalLandTradeRuntime()
    {
        if (!HasActiveRegionalLandTrade())
        {
            return;
        }

        ActiveRegionalLandTrade trade = activeRegionalLandTrade;
        float step = RegionalMerchantTruckSpeed * Time.deltaTime * gameSpeedMultiplier;
        switch (trade.Phase)
        {
            case RegionalLandTradePhase.ArrivingFromEdge:
                MoveRegionalMerchantTruck(trade, GetTruckWorldPosition(trade.Path[0]), step);
                if (Vector3.Distance(trade.TruckObject.transform.position, GetTruckWorldPosition(trade.Path[0])) < 0.05f)
                {
                    trade.Phase = RegionalLandTradePhase.DrivingToWarehouse;
                    trade.PathIndex = 1;
                    SessionDebugLogger.Log("TRADE_LAND", $"{trade.City.NameEn} merchant truck entered city road network.");
                }
                break;

            case RegionalLandTradePhase.DrivingToWarehouse:
                UpdateRegionalMerchantPathForward(trade, step);
                break;

            case RegionalLandTradePhase.TradingAtWarehouse:
                trade.TradeTimer -= Time.deltaTime * gameSpeedMultiplier;
                if (trade.TradeTimer <= 0f)
                {
                    CompleteRegionalLandWarehouseTrade(trade);
                    trade.Phase = RegionalLandTradePhase.DrivingToEdge;
                    trade.PathIndex = Mathf.Max(0, trade.Path.Count - 2);
                }
                break;

            case RegionalLandTradePhase.DrivingToEdge:
                UpdateRegionalMerchantPathBackward(trade, step);
                break;

            case RegionalLandTradePhase.DepartingToEdge:
                MoveRegionalMerchantTruck(trade, trade.EdgeExitWorld, step);
                if (Vector3.Distance(trade.TruckObject.transform.position, trade.EdgeExitWorld) < 0.05f)
                {
                    SessionDebugLogger.Log("TRADE_LAND", $"{trade.City.NameEn} merchant truck left the map.");
                    if (trade.TruckObject != null)
                    {
                        Destroy(trade.TruckObject);
                    }
                    activeRegionalLandTrade = null;
                    tradeDispatchStatusText = "Regional trade ready";
                    isTradeScreenDirty = true;
                }
                break;
        }
    }

    private void UpdateRegionalMerchantPathForward(ActiveRegionalLandTrade trade, float step)
    {
        if (trade.PathIndex >= trade.Path.Count)
        {
            trade.Phase = RegionalLandTradePhase.TradingAtWarehouse;
            trade.TradeTimer = RegionalMerchantTradeDuration;
            SessionDebugLogger.Log("TRADE_LAND", $"{trade.City.NameEn} merchant truck reached Warehouse; trading {trade.OrderType} {trade.ResourceType} x{trade.Quantity}.");
            return;
        }

        Vector3 target = GetTruckWorldPosition(trade.Path[trade.PathIndex]);
        MoveRegionalMerchantTruck(trade, target, step);
        if (Vector3.Distance(trade.TruckObject.transform.position, target) < 0.05f)
        {
            trade.PathIndex++;
        }
    }

    private void UpdateRegionalMerchantPathBackward(ActiveRegionalLandTrade trade, float step)
    {
        if (trade.PathIndex < 0)
        {
            trade.Phase = RegionalLandTradePhase.DepartingToEdge;
            return;
        }

        Vector3 target = GetTruckWorldPosition(trade.Path[trade.PathIndex]);
        MoveRegionalMerchantTruck(trade, target, step);
        if (Vector3.Distance(trade.TruckObject.transform.position, target) < 0.05f)
        {
            trade.PathIndex--;
        }
    }

    private void CompleteRegionalLandWarehouseTrade(ActiveRegionalLandTrade trade)
    {
        string resourceLabel = GetTradeResourceLabel(trade.ResourceType);
        if (trade.OrderType == TradeOrderType.Buy)
        {
            money -= trade.Price;
            AddStoredTradeResource(trade.ResourceType, trade.Quantity);
            RecordMoneyMovement(
                -trade.Price,
                "Treasury",
                trade.City.NameEn,
                $"Land import: {resourceLabel} x{trade.Quantity}",
                money,
                null,
                MoneyAccountKind.CityBudget,
                MoneyAccountKind.External,
                MoneyTransactionReasonKind.Trade);
            ApplyTradeImportTaxes(trade.Price, trade.Quantity, trade.ResourceType, trade.City.NameEn, $"Land import: {resourceLabel} x{trade.Quantity}");
            SessionDebugLogger.Log("TRADE_LAND", $"Bought {trade.ResourceType} x{trade.Quantity} from {trade.City.NameEn} for ${trade.Price}; Warehouse now has {GetWarehouseTradeResourceAmount(trade.ResourceType)}.");
        }
        else if (TryConsumeStoredTradeResource(trade.ResourceType, trade.Quantity))
        {
            money += trade.Price;
            RecordMoneyMovement(
                trade.Price,
                trade.City.NameEn,
                "Treasury",
                $"Land export: {resourceLabel} x{trade.Quantity}",
                money,
                null,
                MoneyAccountKind.External,
                MoneyAccountKind.CityBudget,
                MoneyTransactionReasonKind.Trade);
            ApplyTradeExportTaxes(trade.Price, trade.Quantity, trade.ResourceType, trade.City.NameEn, $"Land export: {resourceLabel} x{trade.Quantity}");
            SessionDebugLogger.Log("TRADE_LAND", $"Sold {trade.ResourceType} x{trade.Quantity} to {trade.City.NameEn} for ${trade.Price}; Warehouse now has {GetWarehouseTradeResourceAmount(trade.ResourceType)}.");
        }

        PushFeedEvent(
            trade.OrderType == TradeOrderType.Buy
                ? $"Land trade: bought {resourceLabel} x{trade.Quantity} from {trade.City.NameEn}."
                : $"Land trade: sold {resourceLabel} x{trade.Quantity} to {trade.City.NameEn}.",
            trade.OrderType == TradeOrderType.Buy
                ? $"Сухопутная торговля: куплено {L(resourceLabel)} x{trade.Quantity}."
                : $"Сухопутная торговля: продано {L(resourceLabel)} x{trade.Quantity}.",
            FeedEventType.Money);
        isTradeScreenDirty = true;
        isEconomyScreenDirty = true;
        isFleetScreenDirty = true;
    }

    private void MoveRegionalMerchantTruck(ActiveRegionalLandTrade trade, Vector3 target, float step)
    {
        if (trade.TruckObject == null)
        {
            return;
        }

        Vector3 current = trade.TruckObject.transform.position;
        Vector3 next = Vector3.MoveTowards(current, target, step);
        trade.TruckObject.transform.position = next;
        Vector3 direction = next - current;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
        {
            trade.TruckObject.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }

    private GameObject CreateRegionalMerchantTruck()
    {
        GameObject root = new("RegionalMerchantTruck");
        root.transform.SetParent(worldRoot, false);
        Color body = new(0.38f, 0.20f, 0.11f);
        Color cab = new(0.78f, 0.62f, 0.36f);
        Color cargo = new(0.62f, 0.42f, 0.18f);
        Color wheel = new(0.06f, 0.06f, 0.07f);
        CreateBuildingBox(root.transform, "Body", new Vector3(0f, 0.28f, 0f), new Vector3(0.82f, 0.38f, 1.20f), body, VisualSmoothnessVehicleMetal, true, true);
        CreateBuildingBox(root.transform, "Cab", new Vector3(0f, 0.45f, 0.48f), new Vector3(0.74f, 0.48f, 0.46f), cab, VisualSmoothnessVehicleMetal, true, true);
        CreateBuildingBox(root.transform, "Cargo", new Vector3(0f, 0.56f, -0.22f), new Vector3(0.70f, 0.36f, 0.64f), cargo, VisualSmoothnessWood, true, true);
        CreateBuildingBox(root.transform, "WheelFL", new Vector3(-0.45f, 0.14f, 0.38f), new Vector3(0.14f, 0.22f, 0.22f), wheel, VisualSmoothnessVehicleMetal, true);
        CreateBuildingBox(root.transform, "WheelFR", new Vector3(0.45f, 0.14f, 0.38f), new Vector3(0.14f, 0.22f, 0.22f), wheel, VisualSmoothnessVehicleMetal, true);
        CreateBuildingBox(root.transform, "WheelBL", new Vector3(-0.45f, 0.14f, -0.42f), new Vector3(0.14f, 0.22f, 0.22f), wheel, VisualSmoothnessVehicleMetal, true);
        CreateBuildingBox(root.transform, "WheelBR", new Vector3(0.45f, 0.14f, -0.42f), new Vector3(0.14f, 0.22f, 0.22f), wheel, VisualSmoothnessVehicleMetal, true);
        return root;
    }
}
