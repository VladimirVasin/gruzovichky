using UnityEngine;

public partial class GameBootstrap
{
    private void UpdateDocksRuntime()
    {
        if (!locations.TryGetValue(LocationType.Docks, out LocationData docks))
        {
            return;
        }

        if (docks.DocksShipDocked)
        {
            docks.DocksShipDockedTimer -= Time.deltaTime * gameSpeedMultiplier;
            UpdateDocksShipVisual(docks);
            if (docks.DocksShipDockedTimer <= 0f)
            {
                EndDocksShipVisit(docks);
            }
            return;
        }

        docks.DocksShipTimer -= Time.deltaTime * gameSpeedMultiplier;
        if (docks.DocksShipTimer <= 0f)
        {
            StartDocksShipVisit(docks);
        }
    }

    private void StartDocksShipVisit(LocationData docks)
    {
        docks.DocksShipDocked = true;
        docks.DocksShipDockedTimer = DocksShipDwellDuration;
        EnsureDocksShipObject(docks);
        if (docks.DocksShipObject != null)
        {
            docks.DocksShipObject.SetActive(true);
        }

        Vector2Int waterCell = GetDocksWaterCell(docks);
        string tradeSummary = PerformDocksShipTrade(docks);
        SessionDebugLogger.Log("DOCKS", $"Ship docked at ({waterCell.x},{waterCell.y}). {tradeSummary}");
        PushFeedEvent(
            $"Ship docked at Docks. {tradeSummary}",
            $"Корабль пришвартовался в Доках. {tradeSummary}",
            FeedEventType.Info);
    }

    private void EndDocksShipVisit(LocationData docks)
    {
        docks.DocksShipDocked = false;
        docks.DocksShipTimer = Random.Range(DocksShipIntervalMin, DocksShipIntervalMax);
        if (docks.DocksShipObject != null)
        {
            docks.DocksShipObject.SetActive(false);
        }

        SessionDebugLogger.Log("DOCKS", $"Ship departed. Next ship in {docks.DocksShipTimer:0.0}s.");
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
        Vector3 pos = GetCellCenter(waterCell);
        pos.y = 0.36f + bob;
        pos.z += 0.30f;
        docks.DocksShipObject.transform.position = pos;
        docks.DocksShipObject.transform.rotation = Quaternion.LookRotation(Vector3.right, Vector3.up);
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

        return docks.DocksShipDocked
            ? $"Ship docked - departing in {Mathf.CeilToInt(docks.DocksShipDockedTimer)}s"
            : $"Waiting for ship - ETA {Mathf.CeilToInt(docks.DocksShipTimer)}s";
    }

    private string GetDocksQuickResourceText()
    {
        if (!locations.TryGetValue(LocationType.Docks, out LocationData docks))
        {
            return string.Empty;
        }

        return $"{FormatValueLine("Export order", GetTradeResourceLabel(docks.DocksExportResource))}\n" +
               $"{FormatValueLine("Import order", GetTradeResourceLabel(docks.DocksImportResource))}\n" +
               $"{FormatValueLine("Cargo", GetDocksQuickCargoSummary(docks))}\n" +
               $"{FormatValueLine("Ship", docks.DocksShipDocked ? "Docked" : $"ETA {Mathf.CeilToInt(docks.DocksShipTimer)}s")}";
    }

    private string GetDocksQuickCargoSummary(LocationData docks)
    {
        if (docks == null)
        {
            return "-";
        }

        return $"L {docks.LogsStored}/{DocksResourceCapacity}, B {docks.BoardsStored}/{DocksResourceCapacity}, " +
               $"C {docks.CottonStored}/{DocksResourceCapacity}, T {docks.TextileStored}/{DocksResourceCapacity}, " +
               $"F {docks.FurnitureStored}/{DocksResourceCapacity}";
    }
}
