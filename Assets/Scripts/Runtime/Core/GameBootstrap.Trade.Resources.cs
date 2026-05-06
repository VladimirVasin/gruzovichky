public partial class GameBootstrap
{
    private int GetStoredTradeResourceAmount(TradeResourceType resourceType)
    {
        return GetWarehouseTradeResourceAmount(resourceType);
    }

    private int GetWarehouseTradeResourceAmount(TradeResourceType resourceType)
    {
        return TradeResourceLedger.GetAmount(GetWarehouseTradeStock(), resourceType);
    }

    private int GetWarehouseExportResourceAmount(TradeResourceType resourceType)
    {
        TradeResourceStock stock = GetWarehouseTradeStock();
        return resourceType switch
        {
            TradeResourceType.Logs => stock.Logs,
            TradeResourceType.Boards => stock.Boards,
            TradeResourceType.Furniture => stock.Furniture,
            _ => 0
        };
    }

    private void AddStoredTradeResource(TradeResourceType resourceType, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        TradeResourceStock stock = GetWarehouseTradeStock();
        TradeResourceLedger.Add(ref stock, resourceType, amount);
        ApplyWarehouseTradeStock(stock);
    }

    private bool TryConsumeStoredTradeResource(TradeResourceType resourceType, int amount)
    {
        TradeResourceStock stock = GetWarehouseTradeStock();
        if (!TradeResourceLedger.TryConsume(ref stock, resourceType, amount))
        {
            return false;
        }

        ApplyWarehouseTradeStock(stock);
        return true;
    }

    private int GetDocksExportStoredResource(LocationData docks, TradeResourceType resourceType)
    {
        return TradeResourceLedger.GetAmount(GetDocksExportStock(docks), resourceType);
    }

    private int GetDocksImportStoredResource(LocationData docks, TradeResourceType resourceType)
    {
        return TradeResourceLedger.GetAmount(GetDocksImportStock(docks), resourceType);
    }

    private void AddDocksExportStoredResource(LocationData docks, TradeResourceType resourceType, int amount)
    {
        if (docks == null || amount <= 0)
        {
            return;
        }

        TradeResourceStock stock = GetDocksExportStock(docks);
        if (TradeResourceLedger.Add(ref stock, resourceType, amount, DocksResourceCapacity) <= 0)
        {
            return;
        }

        ApplyDocksExportStock(docks, stock);
    }

    private void AddDocksImportStoredResource(LocationData docks, TradeResourceType resourceType, int amount)
    {
        if (docks == null || amount <= 0)
        {
            return;
        }

        TradeResourceStock stock = GetDocksImportStock(docks);
        if (TradeResourceLedger.Add(ref stock, resourceType, amount, DocksResourceCapacity) <= 0)
        {
            return;
        }

        ApplyDocksImportStock(docks, stock);
    }

    private bool TryConsumeDocksExportStoredResource(LocationData docks, TradeResourceType resourceType, int amount)
    {
        if (docks == null)
        {
            return false;
        }

        TradeResourceStock stock = GetDocksExportStock(docks);
        if (!TradeResourceLedger.TryConsume(ref stock, resourceType, amount))
        {
            return false;
        }

        ApplyDocksExportStock(docks, stock);
        return true;
    }

    private bool TryConsumeDocksImportStoredResource(LocationData docks, TradeResourceType resourceType, int amount)
    {
        if (docks == null)
        {
            return false;
        }

        TradeResourceStock stock = GetDocksImportStock(docks);
        if (!TradeResourceLedger.TryConsume(ref stock, resourceType, amount))
        {
            return false;
        }

        ApplyDocksImportStock(docks, stock);
        return true;
    }

    private TradeResourceStock GetWarehouseTradeStock()
    {
        locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse);
        return tradeState.GetStoredResourceStock(
            warehouse?.LogsStored ?? 0,
            warehouse?.BoardsStored ?? 0);
    }

    private void ApplyWarehouseTradeStock(TradeResourceStock stock)
    {
        if (locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse))
        {
            warehouse.LogsStored = stock.Logs;
            warehouse.BoardsStored = stock.Boards;
        }

        cottonStored = stock.Cotton;
        textileStored = stock.Textile;
        furnitureStored = stock.Furniture;
    }

    private static TradeResourceStock GetDocksExportStock(LocationData docks)
    {
        return docks == null
            ? new TradeResourceStock()
            : new TradeResourceStock(docks.LogsStored, docks.BoardsStored, docks.CottonStored, docks.TextileStored, docks.FurnitureStored);
    }

    private static TradeResourceStock GetDocksImportStock(LocationData docks)
    {
        return docks == null
            ? new TradeResourceStock()
            : new TradeResourceStock(docks.DocksImportLogsStored, docks.DocksImportBoardsStored, docks.DocksImportCottonStored, docks.DocksImportTextileStored, docks.DocksImportFurnitureStored);
    }

    private static void ApplyDocksExportStock(LocationData docks, TradeResourceStock stock)
    {
        if (docks == null)
        {
            return;
        }

        docks.LogsStored = stock.Logs;
        docks.BoardsStored = stock.Boards;
        docks.CottonStored = stock.Cotton;
        docks.TextileStored = stock.Textile;
        docks.FurnitureStored = stock.Furniture;
    }

    private static void ApplyDocksImportStock(LocationData docks, TradeResourceStock stock)
    {
        if (docks == null)
        {
            return;
        }

        docks.DocksImportLogsStored = stock.Logs;
        docks.DocksImportBoardsStored = stock.Boards;
        docks.DocksImportCottonStored = stock.Cotton;
        docks.DocksImportTextileStored = stock.Textile;
        docks.DocksImportFurnitureStored = stock.Furniture;
    }
}
