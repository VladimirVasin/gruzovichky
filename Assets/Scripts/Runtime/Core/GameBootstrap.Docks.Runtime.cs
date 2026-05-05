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
        int sold = Mathf.Min(DocksShipTradeBatch, GetDocksStoredResource(docks, docks.DocksExportResource));
        int saleMoney = 0;
        if (sold > 0 && TryConsumeDocksStoredResource(docks, docks.DocksExportResource, sold))
        {
            saleMoney = sold * GetDocksSellPrice(docks.DocksExportResource);
            money += saleMoney;
            RecordMoneyMovement(saleMoney, "River Ship", "Treasury", $"Docks sale: {GetTradeResourceLabel(docks.DocksExportResource)} x{sold}", money);
        }

        int buyRoom = DocksResourceCapacity - GetDocksStoredResource(docks, docks.DocksImportResource);
        int bought = Mathf.Min(DocksShipTradeBatch, Mathf.Max(0, buyRoom));
        int buyMoney = 0;
        if (bought > 0)
        {
            int unitPrice = GetDocksBuyPrice(docks.DocksImportResource);
            int affordable = Mathf.Min(bought, unitPrice > 0 ? money / unitPrice : 0);
            if (affordable > 0)
            {
                bought = affordable;
                buyMoney = bought * unitPrice;
                money -= buyMoney;
                AddDocksStoredResource(docks, docks.DocksImportResource, bought);
                RecordMoneyMovement(-buyMoney, "Treasury", "River Ship", $"Docks purchase: {GetTradeResourceLabel(docks.DocksImportResource)} x{bought}", money);
            }
            else
            {
                bought = 0;
            }
        }

        string soldText = sold > 0
            ? $"sold {GetTradeResourceLabel(docks.DocksExportResource)} x{sold} for ${saleMoney}"
            : $"no {GetTradeResourceLabel(docks.DocksExportResource)} ready to sell";
        string boughtText = bought > 0
            ? $"bought {GetTradeResourceLabel(docks.DocksImportResource)} x{bought} for ${buyMoney}"
            : $"no {GetTradeResourceLabel(docks.DocksImportResource)} bought";
        return $"{soldText}; {boughtText}.";
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

    private int GetDocksStoredResource(LocationData docks, TradeResourceType resourceType)
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

    private void AddDocksStoredResource(LocationData docks, TradeResourceType resourceType, int amount)
    {
        if (docks == null || amount <= 0)
        {
            return;
        }

        int capped = Mathf.Min(amount, DocksResourceCapacity - GetDocksStoredResource(docks, resourceType));
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

    private bool TryConsumeDocksStoredResource(LocationData docks, TradeResourceType resourceType, int amount)
    {
        if (docks == null || amount <= 0 || GetDocksStoredResource(docks, resourceType) < amount)
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

    private int GetDocksSellPrice(TradeResourceType resourceType) => resourceType switch
    {
        TradeResourceType.Logs => 22,
        TradeResourceType.Boards => 38,
        TradeResourceType.Cotton => 32,
        TradeResourceType.Textile => 50,
        TradeResourceType.Furniture => 78,
        _ => 40
    };

    private int GetDocksBuyPrice(TradeResourceType resourceType) => Mathf.CeilToInt(GetDocksSellPrice(resourceType) * 1.18f);

    private string GetDocksQuickStatusText()
    {
        if (!locations.TryGetValue(LocationType.Docks, out LocationData docks))
        {
            return string.Empty;
        }

        return IsRussianLanguage()
            ? "Содержимое склада доков"
            : "Docks cargo storage";
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
        return docks.DocksShipPhase switch
        {
            DocksShipPhase.Arriving => "Arriving",
            DocksShipPhase.Docked => $"Docked {Mathf.CeilToInt(docks.DocksShipDockedTimer)}s",
            DocksShipPhase.Departing => "Departing",
            _ => $"ETA {Mathf.CeilToInt(docks.DocksShipTimer)}s"
        };
    }

    private string GetDocksQuickCargoSummary(LocationData docks)
    {
        if (docks == null)
        {
            return "-";
        }

        string text = string.Empty;
        AppendDocksCargoSummary(ref text, "Logs", docks.LogsStored);
        AppendDocksCargoSummary(ref text, "Boards", docks.BoardsStored);
        AppendDocksCargoSummary(ref text, "Cotton", docks.CottonStored);
        AppendDocksCargoSummary(ref text, "Textile", docks.TextileStored);
        AppendDocksCargoSummary(ref text, "Furniture", docks.FurnitureStored);
        return text.Length > 0 ? text : L("Empty");
    }

    private string GetDocksQuickCargoLines(LocationData docks)
    {
        if (docks == null)
        {
            return string.Empty;
        }

        bool ru = IsRussianLanguage();
        string text = string.Empty;
        AppendDocksCargoLine(ref text, "Logs", docks.LogsStored);
        AppendDocksCargoLine(ref text, "Boards", docks.BoardsStored);
        AppendDocksCargoLine(ref text, "Cotton", docks.CottonStored);
        AppendDocksCargoLine(ref text, "Textile", docks.TextileStored);
        AppendDocksCargoLine(ref text, "Furniture", docks.FurnitureStored);

        return text.Length > 0
            ? text
            : FormatValueLine(ru ? "Груз" : "Cargo", ru ? "пусто" : "empty");
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
