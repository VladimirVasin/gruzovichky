using UnityEngine;

public partial class GameBootstrap
{
    private void UpdateDocksRuntime()
    {
        if (!locations.TryGetValue(LocationType.Docks, out LocationData docks))
        {
            return;
        }

        float dt = Time.deltaTime * gameSpeedMultiplier;
        switch (docks.DocksShipPhase)
        {
            case DocksShipPhase.Arriving:
                UpdateDocksShipTravel(docks, dt, GetDocksShipDockX(docks), DocksShipPhase.Docked);
                return;
            case DocksShipPhase.Docked:
                docks.DocksShipDockedTimer -= dt;
                UpdateDocksShipVisual(docks);
                if (docks.DocksShipDockedTimer <= 0f)
                {
                    StartDocksShipDeparture(docks);
                }
                return;
            case DocksShipPhase.Departing:
                UpdateDocksShipTravel(docks, dt, DocksShipExitX, DocksShipPhase.Waiting);
                return;
        }

        docks.DocksShipTimer -= dt;
        if (docks.DocksShipTimer <= 0f)
        {
            StartDocksShipApproach(docks);
        }
    }

    private void StartDocksShipApproach(LocationData docks)
    {
        docks.DocksShipPhase = DocksShipPhase.Arriving;
        docks.DocksShipDocked = false;
        docks.DocksShipWorldX = DocksShipSpawnX;
        EnsureDocksShipObject(docks);
        if (docks.DocksShipObject != null)
        {
            docks.DocksShipObject.SetActive(true);
        }

        UpdateDocksShipVisual(docks);
        SessionDebugLogger.Log("DOCKS", $"Ship spawned at river start x={DocksShipSpawnX:0.0}; sailing to Docks at x={GetDocksShipDockX(docks):0.0}.");
    }

    private void StartDocksShipDocked(LocationData docks)
    {
        docks.DocksShipPhase = DocksShipPhase.Docked;
        docks.DocksShipDocked = true;
        docks.DocksShipDockedTimer = DocksShipDwellDuration;
        docks.DocksShipWorldX = GetDocksShipDockX(docks);
        UpdateDocksShipVisual(docks);
        Vector2Int waterCell = GetDocksWaterCell(docks);
        string tradeSummary = PerformDocksShipTrade(docks);
        SessionDebugLogger.Log("DOCKS", $"Ship docked at ({waterCell.x},{waterCell.y}). {tradeSummary}");
        PushFeedEvent(
            $"Ship docked at Docks. {tradeSummary}",
            $"Корабль пришвартовался в Доках. {tradeSummary}",
            FeedEventType.Info);
    }

    private void StartDocksShipDeparture(LocationData docks)
    {
        docks.DocksShipPhase = DocksShipPhase.Departing;
        docks.DocksShipDocked = false;
        SessionDebugLogger.Log("DOCKS", $"Ship departed Docks and is sailing downstream to x={DocksShipExitX:0.0}.");
    }

    private void FinishDocksShipDeparture(LocationData docks)
    {
        docks.DocksShipPhase = DocksShipPhase.Waiting;
        docks.DocksShipTimer = Random.Range(DocksShipIntervalMin, DocksShipIntervalMax);
        docks.DocksShipWorldX = DocksShipSpawnX;
        if (docks.DocksShipObject != null)
        {
            docks.DocksShipObject.SetActive(false);
        }

        SessionDebugLogger.Log("DOCKS", $"Ship left the opposite river edge. Next ship in {docks.DocksShipTimer:0.0}s.");
    }

    private void UpdateDocksShipTravel(LocationData docks, float dt, float targetX, DocksShipPhase nextPhase)
    {
        EnsureDocksShipObject(docks);
        if (docks.DocksShipObject != null && !docks.DocksShipObject.activeSelf)
        {
            docks.DocksShipObject.SetActive(true);
        }

        docks.DocksShipWorldX = Mathf.MoveTowards(docks.DocksShipWorldX, targetX, DocksShipCruiseSpeed * dt);
        UpdateDocksShipVisual(docks);
        if (!Mathf.Approximately(docks.DocksShipWorldX, targetX))
        {
            return;
        }

        if (nextPhase == DocksShipPhase.Docked)
        {
            StartDocksShipDocked(docks);
        }
        else
        {
            FinishDocksShipDeparture(docks);
        }
    }

    private string PerformDocksShipTrade(LocationData docks)
    {
        bool canSell = TryFindDocksShipTrade(docks, TradeOrderType.Sell, out TradeResourceType sellResource, out int sellQuantity);
        int sold = canSell ? sellQuantity : 0;
        int saleMoney = 0;
        if (sold > 0 && TryConsumeDocksExportStoredResource(docks, sellResource, sold))
        {
            saleMoney = sold * GetDocksSellPrice(sellResource);
            money += saleMoney;
            RecordMoneyMovement(saleMoney, "River Ship", "Treasury", $"Docks sale: {GetTradeResourceLabel(sellResource)} x{sold}", money);
        }

        bool canBuy = TryFindDocksShipTrade(docks, TradeOrderType.Buy, out TradeResourceType buyResource, out int buyQuantity);
        int bought = canBuy ? buyQuantity : 0;
        int buyMoney = 0;
        string buySkipReason = string.Empty;
        if (bought > 0)
        {
            int unitPrice = GetDocksBuyPrice(buyResource);
            int affordable = Mathf.Min(bought, unitPrice > 0 ? money / unitPrice : 0);
            if (affordable > 0)
            {
                bought = affordable;
                buyMoney = bought * unitPrice;
                money -= buyMoney;
                AddDocksImportStoredResource(docks, buyResource, bought);
                RecordMoneyMovement(-buyMoney, "Treasury", "River Ship", $"Docks purchase: {GetTradeResourceLabel(buyResource)} x{bought}", money);
            }
            else
            {
                buySkipReason = $"treasury ${money} cannot afford {GetTradeResourceLabel(buyResource)} at ${unitPrice}/unit";
                bought = 0;
            }
        }

        string soldText = sold > 0
            ? $"sold {GetTradeResourceLabel(sellResource)} x{sold} for ${saleMoney}"
            : canSell
                ? $"no {GetTradeResourceLabel(sellResource)} ready to sell"
                : "no river export policy ready";
        string boughtText = bought > 0
            ? $"bought {GetTradeResourceLabel(buyResource)} x{bought} for ${buyMoney}"
            : canBuy
                ? $"no {GetTradeResourceLabel(buyResource)} bought ({buySkipReason})"
                : $"no river import policy ready ({GetDocksTradeSkipReason(docks, TradeOrderType.Buy)})";
        SessionDebugLogger.Log("TRADE_RIVER", $"Ship trade at Docks: {soldText}; {boughtText}; treasury=${money}.");
        return $"{soldText}; {boughtText}.";
    }

    private bool TryFindDocksShipTrade(LocationData docks, TradeOrderType orderType, out TradeResourceType resourceType, out int quantity)
    {
        for (int i = 0; i < TradeHudResources.Length; i++)
        {
            TradeResourceType candidate = TradeHudResources[i];
            TradePolicyMode mode = GetTradePolicyMode(candidate);
            if (orderType == TradeOrderType.Buy && mode != TradePolicyMode.BuyUpTo ||
                orderType == TradeOrderType.Sell && mode != TradePolicyMode.SellAbove ||
                !HasBuiltRegionalTradeRoute(candidate, orderType, RegionalTradeRouteMode.River))
            {
                continue;
            }

            int target = GetTradePolicyTarget(candidate);
            if (orderType == TradeOrderType.Buy)
            {
                int stock = GetWarehouseTradeResourceAmount(candidate) + GetDocksImportStoredResource(docks, candidate);
                int room = DocksResourceCapacity - GetDocksImportStoredResource(docks, candidate);
                quantity = Mathf.Min(DocksShipTradeBatch, Mathf.Min(Mathf.Max(0, target - stock), Mathf.Max(0, room)));
                if (quantity > 0)
                {
                    resourceType = candidate;
                    return true;
                }
            }
            else
            {
                int dockStock = GetDocksExportStoredResource(docks, candidate);
                int totalStock = GetWarehouseTradeResourceAmount(candidate) + dockStock;
                quantity = Mathf.Min(DocksShipTradeBatch, Mathf.Min(dockStock, Mathf.Max(0, totalStock - target)));
                if (quantity > 0)
                {
                    resourceType = candidate;
                    return true;
                }
            }
        }

        resourceType = TradeResourceType.Logs;
        quantity = 0;
        return false;
    }

    private string GetDocksTradeSkipReason(LocationData docks, TradeOrderType orderType)
    {
        for (int i = 0; i < TradeHudResources.Length; i++)
        {
            TradeResourceType candidate = TradeHudResources[i];
            TradePolicyMode mode = GetTradePolicyMode(candidate);
            TradePolicyMode requiredMode = orderType == TradeOrderType.Buy
                ? TradePolicyMode.BuyUpTo
                : TradePolicyMode.SellAbove;
            if (mode != requiredMode)
            {
                continue;
            }

            if (!HasBuiltRegionalTradeRoute(candidate, orderType, RegionalTradeRouteMode.River))
            {
                return $"{orderType} {candidate}: {DescribeRegionalTradeRouteAvailability(candidate, orderType, RegionalTradeRouteMode.River)}";
            }

            int target = GetTradePolicyTarget(candidate);
            if (orderType == TradeOrderType.Buy)
            {
                int stock = GetWarehouseTradeResourceAmount(candidate) + GetDocksImportStoredResource(docks, candidate);
                int room = DocksResourceCapacity - GetDocksImportStoredResource(docks, candidate);
                if (stock >= target)
                {
                    return $"{candidate} stock {stock} already meets target {target}";
                }

                if (room <= 0)
                {
                    return $"{candidate} import storage full";
                }
            }
            else
            {
                int dockStock = GetDocksExportStoredResource(docks, candidate);
                int totalStock = GetWarehouseTradeResourceAmount(candidate) + dockStock;
                if (dockStock <= 0)
                {
                    return $"{candidate} has no export cargo at Docks";
                }

                if (totalStock <= target)
                {
                    return $"{candidate} total stock {totalStock} is not above target {target}";
                }
            }
        }

        return orderType == TradeOrderType.Buy
            ? "no Buy up to policy with an eligible river route"
            : "no Sell surplus policy with an eligible river route";
    }

    private void EnsureDocksShipObject(LocationData docks)
    {
        if (docks.DocksShipObject != null)
        {
            return;
        }

        GameObject shipRoot = new("DocksTradeShip");
        shipRoot.transform.SetParent(worldRoot, false);
        docks.DocksShipObject = shipRoot;

        Color hull = new(0.16f, 0.22f, 0.28f);
        Color deck = new(0.48f, 0.32f, 0.16f);
        Color cabin = new(0.74f, 0.76f, 0.70f);
        Color trim = new(0.78f, 0.58f, 0.22f);
        CreateBuildingBox(shipRoot.transform, "Hull", Vector3.zero, new Vector3(2.9f, 0.34f, 0.82f), hull, VisualSmoothnessVehicleMetal, true, true);
        CreateBuildingBox(shipRoot.transform, "Deck", new Vector3(0f, 0.24f, 0f), new Vector3(2.55f, 0.10f, 0.72f), deck, VisualSmoothnessWood, true);
        CreateBuildingBox(shipRoot.transform, "Cabin", new Vector3(-0.48f, 0.52f, 0f), new Vector3(0.78f, 0.46f, 0.52f), cabin, VisualSmoothnessBuildingWall, true, true);
        CreateBuildingBox(shipRoot.transform, "CabinWindow", new Vector3(-0.48f, 0.58f, -0.27f), new Vector3(0.52f, 0.18f, 0.035f), new Color(0.56f, 0.82f, 0.94f), VisualSmoothnessGlass, true);
        CreateBuildingBox(shipRoot.transform, "CargoStripe", new Vector3(0.76f, 0.39f, 0f), new Vector3(0.95f, 0.16f, 0.58f), trim, VisualSmoothnessVehicleMetal, true);
        CreateBuildingCylinder(shipRoot.transform, "BollardA", new Vector3(-1.24f, 0.46f, -0.28f), new Vector3(0.08f, 0.16f, 0.08f), trim, VisualSmoothnessVehicleMetal, true);
        CreateBuildingCylinder(shipRoot.transform, "BollardB", new Vector3(1.24f, 0.46f, -0.28f), new Vector3(0.08f, 0.16f, 0.08f), trim, VisualSmoothnessVehicleMetal, true);
        shipRoot.SetActive(false);
    }

    private void UpdateDocksShipVisual(LocationData docks)
    {
        if (docks.DocksShipObject == null)
        {
            return;
        }

        Vector2Int waterCell = GetDocksWaterCell(docks);
        float bob = Mathf.Sin(Time.time * 1.8f) * 0.035f;
        Vector3 waterPos = GetCellCenter(waterCell);
        float laneZ = Mathf.Clamp(waterPos.z + 0.30f, GridHeight - WaterRiverWidth + 0.55f, GridHeight - 0.45f);
        Vector3 pos = new(docks.DocksShipWorldX, GetDocksTradeShipWaterY(docks.DocksShipWorldX, laneZ) + 0.14f + bob, laneZ);
        docks.DocksShipObject.transform.position = pos;
        docks.DocksShipObject.transform.rotation = Quaternion.identity;
    }

    private float GetDocksShipDockX(LocationData docks)
    {
        return GetCellCenter(GetDocksWaterCell(docks)).x;
    }

    private float GetDocksTradeShipWaterY(float worldX, float laneZ)
    {
        Vector2Int cell = new(
            Mathf.Clamp(Mathf.FloorToInt(worldX), 0, GridWidth - 1),
            Mathf.Clamp(Mathf.FloorToInt(laneZ), GridHeight - WaterRiverWidth, GridHeight - 1));
        return waterCells.Contains(cell) ? GetCurrentVisualWaterHeight(cell) : 0.22f;
    }

    private bool CanDocksSellResource(TradeResourceType resourceType)
    {
        return GetTradePolicyMode(resourceType) == TradePolicyMode.SellAbove &&
               HasBuiltTradeRouteForOrder(resourceType, TradeOrderType.Sell);
    }

    private bool CanDocksBuyResource(TradeResourceType resourceType)
    {
        return GetTradePolicyMode(resourceType) == TradePolicyMode.BuyUpTo &&
               HasBuiltTradeRouteForOrder(resourceType, TradeOrderType.Buy);
    }

    private int GetDocksExportStoredResource(LocationData docks, TradeResourceType resourceType)
    {
        if (docks == null)
        {
            return 0;
        }

        return resourceType switch
        {
            TradeResourceType.Logs => docks.LogsStored,
            TradeResourceType.Boards => docks.BoardsStored,
            TradeResourceType.Cotton => docks.CottonStored,
            TradeResourceType.Textile => docks.TextileStored,
            TradeResourceType.Furniture => docks.FurnitureStored,
            _ => 0
        };
    }

    private int GetDocksImportStoredResource(LocationData docks, TradeResourceType resourceType)
    {
        if (docks == null)
        {
            return 0;
        }

        return resourceType switch
        {
            TradeResourceType.Logs => docks.DocksImportLogsStored,
            TradeResourceType.Boards => docks.DocksImportBoardsStored,
            TradeResourceType.Cotton => docks.DocksImportCottonStored,
            TradeResourceType.Textile => docks.DocksImportTextileStored,
            TradeResourceType.Furniture => docks.DocksImportFurnitureStored,
            _ => 0
        };
    }

    private int GetWarehouseExportResourceAmount(TradeResourceType resourceType)
    {
        return resourceType switch
        {
            TradeResourceType.Logs => locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse) ? warehouse.LogsStored : 0,
            TradeResourceType.Boards => locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse) ? warehouse.BoardsStored : 0,
            TradeResourceType.Furniture => furnitureStored,
            _ => 0
        };
    }

    private static TradeResourceType CargoTypeToTradeResourceType(CargoType cargoType)
    {
        return cargoType switch
        {
            CargoType.Logs => TradeResourceType.Logs,
            CargoType.Boards => TradeResourceType.Boards,
            CargoType.Cotton => TradeResourceType.Cotton,
            CargoType.Textile => TradeResourceType.Textile,
            CargoType.Furniture => TradeResourceType.Furniture,
            _ => TradeResourceType.Logs
        };
    }

    private static TradeResourceType GetDocksLoadResourceForTrip(TripType tripType)
    {
        return tripType switch
        {
            TripType.DocksToWarehouseCotton => TradeResourceType.Cotton,
            TripType.DocksToWarehouseTextile => TradeResourceType.Textile,
            TripType.DocksToWarehouseFurniture => TradeResourceType.Furniture,
            _ => TradeResourceType.Logs
        };
    }

    private void AddDocksExportStoredResource(LocationData docks, TradeResourceType resourceType, int amount)
    {
        if (docks == null || amount <= 0)
        {
            return;
        }

        int capped = Mathf.Min(amount, DocksResourceCapacity - GetDocksExportStoredResource(docks, resourceType));
        if (capped <= 0)
        {
            return;
        }

        switch (resourceType)
        {
            case TradeResourceType.Logs: docks.LogsStored += capped; break;
            case TradeResourceType.Boards: docks.BoardsStored += capped; break;
            case TradeResourceType.Cotton: docks.CottonStored += capped; break;
            case TradeResourceType.Textile: docks.TextileStored += capped; break;
            case TradeResourceType.Furniture: docks.FurnitureStored += capped; break;
        }
    }

    private void AddDocksImportStoredResource(LocationData docks, TradeResourceType resourceType, int amount)
    {
        if (docks == null || amount <= 0)
        {
            return;
        }

        int capped = Mathf.Min(amount, DocksResourceCapacity - GetDocksImportStoredResource(docks, resourceType));
        if (capped <= 0)
        {
            return;
        }

        switch (resourceType)
        {
            case TradeResourceType.Logs: docks.DocksImportLogsStored += capped; break;
            case TradeResourceType.Boards: docks.DocksImportBoardsStored += capped; break;
            case TradeResourceType.Cotton: docks.DocksImportCottonStored += capped; break;
            case TradeResourceType.Textile: docks.DocksImportTextileStored += capped; break;
            case TradeResourceType.Furniture: docks.DocksImportFurnitureStored += capped; break;
        }
    }

    private bool TryConsumeDocksExportStoredResource(LocationData docks, TradeResourceType resourceType, int amount)
    {
        if (docks == null || amount <= 0 || GetDocksExportStoredResource(docks, resourceType) < amount)
        {
            return false;
        }

        switch (resourceType)
        {
            case TradeResourceType.Logs: docks.LogsStored -= amount; break;
            case TradeResourceType.Boards: docks.BoardsStored -= amount; break;
            case TradeResourceType.Cotton: docks.CottonStored -= amount; break;
            case TradeResourceType.Textile: docks.TextileStored -= amount; break;
            case TradeResourceType.Furniture: docks.FurnitureStored -= amount; break;
        }

        return true;
    }

    private bool TryConsumeDocksImportStoredResource(LocationData docks, TradeResourceType resourceType, int amount)
    {
        if (docks == null || amount <= 0 || GetDocksImportStoredResource(docks, resourceType) < amount)
        {
            return false;
        }

        switch (resourceType)
        {
            case TradeResourceType.Logs: docks.DocksImportLogsStored -= amount; break;
            case TradeResourceType.Boards: docks.DocksImportBoardsStored -= amount; break;
            case TradeResourceType.Cotton: docks.DocksImportCottonStored -= amount; break;
            case TradeResourceType.Textile: docks.DocksImportTextileStored -= amount; break;
            case TradeResourceType.Furniture: docks.DocksImportFurnitureStored -= amount; break;
        }

        return true;
    }

    private int GetDocksSellPrice(TradeResourceType resourceType) => resourceType switch
    {
        TradeResourceType.Logs => 22,
        TradeResourceType.Boards => 38,
        TradeResourceType.Cotton => 32,
        TradeResourceType.Textile => 50,
        TradeResourceType.Furniture => 78,
        TradeResourceType.Alcohol => 46,
        _ => 40
    };

    private int GetDocksBuyPrice(TradeResourceType resourceType) => Mathf.CeilToInt(GetDocksSellPrice(resourceType) * 1.18f);

    private string GetDocksQuickStatusText()
    {
        if (!locations.TryGetValue(LocationType.Docks, out LocationData docks))
        {
            return string.Empty;
        }

        bool ru = IsRussianLanguage();
        string workerStatus = docks.Workers > 0
            ? (ru ? "Рабочий на смене" : "Worker on shift")
            : (ru ? "Нет рабочего на смене" : "No worker on shift");
        return $"{workerStatus}\n{(ru ? "Корабль" : "Ship")}: {GetDocksShipHudState(docks)}";
    }

    private string GetDocksQuickResourceText()
    {
        if (!locations.TryGetValue(LocationType.Docks, out LocationData docks))
        {
            return string.Empty;
        }

        return GetDocksQuickCargoLines(docks);
    }

    private string GetDocksShipHudState(LocationData docks)
    {
        bool ru = IsRussianLanguage();
        return docks.DocksShipPhase switch
        {
            DocksShipPhase.Arriving => ru ? "подходит" : "Arriving",
            DocksShipPhase.Docked => ru ? $"у причала {Mathf.CeilToInt(docks.DocksShipDockedTimer)}с" : $"Docked {Mathf.CeilToInt(docks.DocksShipDockedTimer)}s",
            DocksShipPhase.Departing => ru ? "отходит" : "Departing",
            _ => ru ? $"ожидание {Mathf.CeilToInt(docks.DocksShipTimer)}с" : $"ETA {Mathf.CeilToInt(docks.DocksShipTimer)}s"
        };
    }

    private string GetDocksQuickCargoSummary(LocationData docks)
    {
        if (docks == null)
        {
            return "-";
        }

        string text = string.Empty;
        AppendDocksCargoSummary(ref text, "Logs", docks.LogsStored + docks.DocksImportLogsStored);
        AppendDocksCargoSummary(ref text, "Boards", docks.BoardsStored + docks.DocksImportBoardsStored);
        AppendDocksCargoSummary(ref text, "Cotton", docks.CottonStored + docks.DocksImportCottonStored);
        AppendDocksCargoSummary(ref text, "Textile", docks.TextileStored + docks.DocksImportTextileStored);
        AppendDocksCargoSummary(ref text, "Furniture", docks.FurnitureStored + docks.DocksImportFurnitureStored);
        return text.Length > 0 ? text : L("Empty");
    }

    private string GetDocksQuickCargoLines(LocationData docks)
    {
        if (docks == null)
        {
            return string.Empty;
        }

        bool ru = IsRussianLanguage();
        string exportText = string.Empty;
        AppendDocksCargoLine(ref exportText, "Logs", docks.LogsStored);
        AppendDocksCargoLine(ref exportText, "Boards", docks.BoardsStored);
        AppendDocksCargoLine(ref exportText, "Cotton", docks.CottonStored);
        AppendDocksCargoLine(ref exportText, "Textile", docks.TextileStored);
        AppendDocksCargoLine(ref exportText, "Furniture", docks.FurnitureStored);

        string importText = string.Empty;
        AppendDocksCargoLine(ref importText, "Logs", docks.DocksImportLogsStored);
        AppendDocksCargoLine(ref importText, "Boards", docks.DocksImportBoardsStored);
        AppendDocksCargoLine(ref importText, "Cotton", docks.DocksImportCottonStored);
        AppendDocksCargoLine(ref importText, "Textile", docks.DocksImportTextileStored);
        AppendDocksCargoLine(ref importText, "Furniture", docks.DocksImportFurnitureStored);

        string text = $"{(ru ? "Товары на продажу" : "Goods for sale")}\n";
        text += exportText.Length > 0 ? exportText : FormatValueLine(ru ? "Груз" : "Cargo", ru ? "пусто" : "empty");
        text += $"\n\n{(ru ? "Купленные товары" : "Purchased goods")}\n";
        text += importText.Length > 0 ? importText : FormatValueLine(ru ? "Груз" : "Cargo", ru ? "пусто" : "empty");

        return text;
    }

    private static void AppendDocksCargoSummary(ref string text, string label, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (text.Length > 0)
        {
            text += ", ";
        }

        text += $"{L(label)} {amount}/{DocksResourceCapacity}";
    }

    private static void AppendDocksCargoLine(ref string text, string label, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (text.Length > 0)
        {
            text += "\n";
        }

        text += FormatValueLine(label, $"{amount} / {DocksResourceCapacity}");
    }
}
