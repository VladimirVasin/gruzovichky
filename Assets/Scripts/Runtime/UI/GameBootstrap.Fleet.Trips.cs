using System.Collections.Generic;

public partial class GameBootstrap
{
    private List<TripOption> GetAvailableTrips()
    {
        List<TripOption> trips = new();

        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            return trips;
        }

        foreach (LocationData forest in EnumerateLocationsOfType(LocationType.Forest))
        {
            if (forest == null || forest.LogsStored <= 0 || !HasPath(parking.Anchor, forest.Anchor))
            {
                continue;
            }

            bool addedSawmillRoute = false;
            foreach (LocationData sawmill in EnumerateLocationsOfType(LocationType.Sawmill))
            {
                if (sawmill == null || !HasPath(forest.Anchor, sawmill.Anchor))
                {
                    continue;
                }

                addedSawmillRoute = true;
                trips.Add(CreateTripOption(
                    TripType.ForestToSawmill,
                    $"Deliver Logs: {GetTripLocationName(forest)} -> {GetTripLocationName(sawmill)}",
                    "Pick up logs and deliver them to a sawmill.",
                    forest,
                    sawmill));
            }

            if (addedSawmillRoute)
            {
                continue;
            }

            foreach (LocationData warehouse in EnumerateLocationsOfType(LocationType.Warehouse))
            {
                if (warehouse == null || !HasPath(forest.Anchor, warehouse.Anchor))
                {
                    continue;
                }

                trips.Add(CreateTripOption(
                    TripType.ForestToWarehouse,
                    $"Deliver Logs: {GetTripLocationName(forest)} -> {GetTripLocationName(warehouse)}",
                    "No reachable sawmill route is available, so store logs at a warehouse.",
                    forest,
                    warehouse));
            }
        }

        foreach (LocationData sawmill in EnumerateLocationsOfType(LocationType.Sawmill))
        {
            if (sawmill == null || sawmill.BoardsStored <= 0 || !HasPath(parking.Anchor, sawmill.Anchor))
            {
                continue;
            }

            foreach (LocationData warehouse in EnumerateLocationsOfType(LocationType.Warehouse))
            {
                if (warehouse == null || !HasPath(sawmill.Anchor, warehouse.Anchor))
                {
                    continue;
                }

                trips.Add(CreateTripOption(
                    TripType.SawmillToWarehouse,
                    $"Deliver Boards: {GetTripLocationName(sawmill)} -> {GetTripLocationName(warehouse)}",
                    "Take processed boards from a sawmill to a warehouse.",
                    sawmill,
                    warehouse));
            }
        }

        foreach (LocationData warehouse in EnumerateLocationsOfType(LocationType.Warehouse))
        {
            if (warehouse == null || !HasPath(parking.Anchor, warehouse.Anchor))
            {
                continue;
            }

            foreach (LocationData furnitureFactory in EnumerateLocationsOfType(LocationType.FurnitureFactory))
            {
                if (furnitureFactory == null)
                {
                    continue;
                }

                bool canReachFactoryFromWarehouse = HasPath(warehouse.Anchor, furnitureFactory.Anchor);
                bool canReachWarehouseFromFactory =
                    HasPath(parking.Anchor, furnitureFactory.Anchor) &&
                    HasPath(furnitureFactory.Anchor, warehouse.Anchor);

                if (GetWarehouseTripResourceAmount(warehouse, TradeResourceType.Boards) > 0 &&
                    furnitureFactory.BoardsStored < FurnitureFactoryMaxBoardsStorage &&
                    canReachFactoryFromWarehouse)
                {
                    trips.Add(CreateTripOption(
                        TripType.WarehouseToFurnitureFactoryBoards,
                        $"Deliver Boards: {GetTripLocationName(warehouse)} -> {GetTripLocationName(furnitureFactory)}",
                        "Take boards from a warehouse to a furniture factory.",
                        warehouse,
                        furnitureFactory));
                }

                if (GetWarehouseTripResourceAmount(warehouse, TradeResourceType.Textile) > 0 &&
                    furnitureFactory.TextileStored < FurnitureFactoryMaxTextileStorage &&
                    canReachFactoryFromWarehouse)
                {
                    trips.Add(CreateTripOption(
                        TripType.WarehouseToFurnitureFactoryTextile,
                        $"Deliver Textile: {GetTripLocationName(warehouse)} -> {GetTripLocationName(furnitureFactory)}",
                        "Take textile stock from a warehouse to a furniture factory.",
                        warehouse,
                        furnitureFactory));
                }

                if (furnitureFactory.FurnitureStored > 0 && canReachWarehouseFromFactory)
                {
                    trips.Add(CreateTripOption(
                        TripType.FurnitureFactoryToWarehouse,
                        $"Deliver Furniture: {GetTripLocationName(furnitureFactory)} -> {GetTripLocationName(warehouse)}",
                        "Pick up finished furniture and return it to warehouse storage.",
                        furnitureFactory,
                        warehouse));
                }
            }

            foreach (LocationData docks in EnumerateLocationsOfType(LocationType.Docks))
            {
                if (docks == null)
                {
                    continue;
                }

                bool canReachDocksFromWarehouse = HasPath(warehouse.Anchor, docks.Anchor);
                bool canReachWarehouseFromDocks =
                    HasPath(parking.Anchor, docks.Anchor) &&
                    HasPath(docks.Anchor, warehouse.Anchor);

                if (canReachDocksFromWarehouse)
                {
                    AddDocksExportTripIfAvailable(trips, warehouse, docks, TradeResourceType.Logs, TripType.WarehouseToDocksLogs, "Logs");
                    AddDocksExportTripIfAvailable(trips, warehouse, docks, TradeResourceType.Boards, TripType.WarehouseToDocksBoards, "Boards");
                    AddDocksExportTripIfAvailable(trips, warehouse, docks, TradeResourceType.Furniture, TripType.WarehouseToDocksFurniture, "Furniture");
                }

                if (canReachWarehouseFromDocks)
                {
                    AddDocksImportTripIfAvailable(trips, docks, warehouse, TradeResourceType.Cotton, TripType.DocksToWarehouseCotton, "Cotton");
                    AddDocksImportTripIfAvailable(trips, docks, warehouse, TradeResourceType.Textile, TripType.DocksToWarehouseTextile, "Textile");
                    AddDocksImportTripIfAvailable(trips, docks, warehouse, TradeResourceType.Furniture, TripType.DocksToWarehouseFurniture, "Furniture");
                }
            }
        }

        trips.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        return trips;
    }

    private TripOption CreateTripOption(TripType tripType, string title, string description, LocationData pickup, LocationData dropoff)
    {
        return new TripOption
        {
            Type = tripType,
            Title = title,
            Description = description,
            Reward = GetTripReward(tripType, pickup, dropoff),
            Priority = GetTruckTripPriority(tripType),
            PickupLocationInstanceId = pickup?.InstanceId ?? 0,
            DropoffLocationInstanceId = dropoff?.InstanceId ?? 0
        };
    }

    private string GetTripLocationName(LocationData location)
    {
        return location == null
            ? "Location"
            : GetBuildingInstanceDisplayName(location.Type, location.InstanceId);
    }

    private int GetWarehouseTripResourceAmount(LocationData warehouse, TradeResourceType resourceType)
    {
        if (warehouse == null)
        {
            return 0;
        }

        int localAmount = resourceType switch
        {
            TradeResourceType.Logs      => warehouse.LogsStored,
            TradeResourceType.Boards    => warehouse.BoardsStored,
            TradeResourceType.Cotton    => warehouse.CottonStored,
            TradeResourceType.Textile   => warehouse.TextileStored,
            TradeResourceType.Furniture => warehouse.FurnitureStored,
            _                           => 0
        };

        bool isPrimaryWarehouse = locations.TryGetValue(LocationType.Warehouse, out LocationData primaryWarehouse) &&
                                  primaryWarehouse == warehouse;
        if (!isPrimaryWarehouse)
        {
            return localAmount;
        }

        return resourceType switch
        {
            TradeResourceType.Cotton    => localAmount + cottonStored,
            TradeResourceType.Textile   => localAmount + textileStored,
            TradeResourceType.Furniture => localAmount + furnitureStored,
            _                           => localAmount
        };
    }

    private void AddDocksExportTripIfAvailable(List<TripOption> trips, LocationData warehouse, LocationData docks, TradeResourceType resource, TripType tripType, string label)
    {
        if (GetTradePolicyMode(resource) != TradePolicyMode.SellAbove ||
            !HasBuiltRegionalTradeRoute(resource, TradeOrderType.Sell, RegionalTradeRouteMode.River) ||
            GetDocksExportTripLoadLimit(docks, resource, warehouse) <= 0 ||
            GetDocksExportStoredResource(docks, resource) >= DocksResourceCapacity)
        {
            return;
        }

        trips.Add(CreateTripOption(
            tripType,
            $"Export {label}: {GetTripLocationName(warehouse)} -> {GetTripLocationName(docks)}",
            $"Move assigned export {label} to the docks before the next ship.",
            warehouse,
            docks));
    }

    private void AddDocksImportTripIfAvailable(List<TripOption> trips, LocationData docks, LocationData warehouse, TradeResourceType resource, TripType tripType, string label)
    {
        if (GetDocksImportStoredResource(docks, resource) <= 0)
        {
            return;
        }

        trips.Add(CreateTripOption(
            tripType,
            $"Import {label}: {GetTripLocationName(docks)} -> {GetTripLocationName(warehouse)}",
            $"Move imported {label} from the docks to town storage.",
            docks,
            warehouse));
    }
}
