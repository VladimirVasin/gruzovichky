using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private string GetBuildingQuickStatusText(LocationType locationType)
    {
        if (IsProductionLocation(locationType) && !IsLocationOperational(locationType))
        {
            return locationType switch
            {
            LocationType.Forest           => "Offline - no workers assigned",
                LocationType.Sawmill          => "Offline - no workers assigned",
                LocationType.FurnitureFactory => "Offline - no workers assigned",
                LocationType.Warehouse        => "Offline - no workers assigned",
                _                             => "Offline"
            };
        }

        return locationType switch
        {
            LocationType.Parking          => "Logistics hub and truck handoff point",
            LocationType.GasStation       => "Fuel service online",
            LocationType.Forest           => "Lumberyard operations",
            LocationType.Sawmill          => "Processing logs into boards",
            LocationType.FurnitureFactory => "Crafting furniture from boards and textile",
            LocationType.Warehouse        => IsLocationOperational(LocationType.Warehouse)
                ? HasActiveTradeRun() &&
                  activeTradeRun.OrderType == TradeOrderType.Buy &&
                  (activeTradeRun.Phase == TradeRunPhase.ReturningToWarehouse || activeTradeRun.Phase == TradeRunPhase.UnloadingAtWarehouse)
                    ? "Receiving imported trade delivery"
                    : "Warehouse operational - resources available"
                : "Finished goods storage",
            LocationType.Motel   => "Drivers rest and idle here",
            LocationType.IntercityStop => "Intercity worker arrival stop by the highway",
            LocationType.Stop    => GetLocalBusStopNetworkStatusText(),
            LocationType.Canteen      => "Service canteen - visitors pay $10 for meals",
            LocationType.Bar          => "Social hub - idle drivers gather here",
            LocationType.GamblingHall => "Gambling Hall - free leisure for workers.",
            LocationType.CityPark     => IsRussianLanguage() ? "Городской парк — рабочие гуляют и сидят на лавочках." : "City Park — workers stroll and sit on benches.",
            LocationType.PersonalHouse => IsRussianLanguage() ? "Жилой дом — пригородный коттедж." : "Personal House — suburban residential home.",
            LocationType.CarMarket     => IsRussianLanguage() ? "\u0410\u0432\u0442\u043e\u0440\u044b\u043d\u043e\u043a: \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u043f\u043e\u043a\u0443\u043f\u0430\u044e\u0442 \u043b\u0438\u0447\u043d\u044b\u0435 \u0430\u0432\u0442\u043e." : "Car Market - workers buy personal cars here.",
            LocationType.LaborExchange => IsRussianLanguage() ? "\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430: \u043a\u043b\u0435\u0440\u043a \u043f\u0443\u0431\u043b\u0438\u043a\u0443\u0435\u0442 \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u0438, \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u043f\u0440\u0438\u0445\u043e\u0434\u044f\u0442 \u0437\u0430 \u0440\u0430\u0431\u043e\u0442\u043e\u0439." : "Labor Exchange - a clerk posts vacancies and workers apply here.",
            _                         => string.Empty
        };
    }

    private string GetBuildingQuickResourceText(LocationType locationType)
    {
        return locationType switch
        {
            LocationType.Parking => $"{FormatValueLine("Truck Slots", $"{GetOwnedTruckCount()} / {GetTruckParkingCapacity()}")}\n{FormatValueLine("Bus Slots", $"{GetOwnedBusCount()} / {GetBusParkingCapacity()}")}\n{FormatValueLine(IsRussianLanguage() ? "Казна парковки" : "Parking Treasury", $"${locations[LocationType.Parking].BuildingBank}")}",
            LocationType.Forest => $"{FormatValueLine("Worker on shift", $"{CountWorkersOnShiftAt(LocationType.Forest)} / {GetMaxBuildingWorkerSlots(LocationType.Forest)}")}\n{FormatValueLine("Logs", $"{locations[LocationType.Forest].LogsStored} / {ForestMaxLogsStorage}")}",
            LocationType.Sawmill => $"{FormatValueLine("Worker on shift", $"{CountWorkersOnShiftAt(LocationType.Sawmill)} / {GetMaxBuildingWorkerSlots(LocationType.Sawmill)}")}\n{FormatValueLine("Logs", locations[LocationType.Sawmill].LogsStored.ToString())}\n{FormatValueLine("Boards", locations[LocationType.Sawmill].BoardsStored.ToString())}",
            LocationType.FurnitureFactory => $"{FormatValueLine("Worker on shift", $"{CountWorkersOnShiftAt(LocationType.FurnitureFactory)} / {GetMaxBuildingWorkerSlots(LocationType.FurnitureFactory)}")}\n{FormatValueLine("Boards", $"{locations[LocationType.FurnitureFactory].BoardsStored} / {FurnitureFactoryMaxBoardsStorage}")}\n{FormatValueLine("Textile", $"{locations[LocationType.FurnitureFactory].TextileStored} / {FurnitureFactoryMaxTextileStorage}")}\n{FormatValueLine("Furniture", $"{locations[LocationType.FurnitureFactory].FurnitureStored} / {FurnitureFactoryMaxFurnitureStorage}")}",
            LocationType.Warehouse => GetWarehouseQuickResourceText(),
            LocationType.GasStation => GetGasStationQuickResourceText(),
            LocationType.IntercityStop    => IsRussianLanguage()
                ? FormatValueLine("Статус", "Готова к приёму")
                : FormatValueLine("Status", "Intercity arrivals ready"),
            LocationType.Stop       => GetLocalBusStopQuickResourceText(),
            LocationType.Motel      => GetServiceBuildingQuickResourceText(locationType),
            LocationType.Bar          => GetBarQuickResourceText(),
            LocationType.Canteen      => GetCanteenQuickResourceText(),
            LocationType.GamblingHall => GetGamblingHallQuickResourceText(),
            LocationType.CityPark     => GetServiceBuildingQuickResourceText(locationType),
            LocationType.CarMarket    => GetServiceBuildingQuickResourceText(locationType),
            LocationType.LaborExchange => GetLaborExchangeQuickResourceText(),
            _ => string.Empty
        };
    }

    private string GetLocalBusStopNetworkStatusText()
    {
        bool ru = IsRussianLanguage();
        if (localStops.Count < 2)
        {
            return ru
                ? $"Не работает - нужно минимум 2 остановки ({localStops.Count}/2)"
                : $"Offline - needs at least 2 stops ({localStops.Count}/2)";
        }

        return ru
            ? $"Остановка подключена к маршруту ({localStops.Count} остановки)"
            : $"Local route stop online ({localStops.Count} stops)";
    }

    private string GetLocalBusStopQuickResourceText()
    {
        bool ru = IsRussianLanguage();
        int stopCount = localStops.Count;
        string status = stopCount < 2
            ? (ru ? "Не работает" : "Offline")
            : (ru ? "Готова к маршруту" : "Route ready");
        string requirement = stopCount < 2
            ? (ru ? $"{stopCount}/2 - построй ещё одну остановку" : $"{stopCount}/2 - build one more stop")
            : (ru ? $"{stopCount} остановки в сети" : $"{stopCount} stops in network");

        return $"{FormatValueLine(ru ? "Статус" : "Status", status)}\n" +
               $"{FormatValueLine(ru ? "Сеть" : "Network", requirement)}";
    }

    private string GetPersonalHouseQuickResourceText()
    {
        int residentCount = 0;
        foreach (DriverAgent d in driverAgents)
        {
            if (d.AssignedPersonalHouseIndex == selectedPersonalHouseIndex)
                residentCount++;
        }
        bool ru = IsRussianLanguage();
        return FormatValueLine(ru ? "Жильцов" : "Residents", residentCount.ToString());
    }

    private void UpdatePersonalHouseResidentsSection()
    {
        if (buildingQuickHud?.ResidentRows == null) return;

        bool ru = IsRussianLanguage();
        buildingQuickHud.PersonalHouseSectionHeader.text = ru ? "Жильцы" : "Residents";

        List<DriverAgent> assigned  = new();
        foreach (DriverAgent d in driverAgents)
        {
            if (d.AssignedPersonalHouseIndex == selectedPersonalHouseIndex)
            {
                assigned.Add(d);
            }
        }

        for (int i = 0; i < buildingQuickHud.ResidentRows.Length; i++)
        {
            PersonalHouseResidentRowUi row = buildingQuickHud.ResidentRows[i];
            if (i >= assigned.Count)
            {
                row.Root.gameObject.SetActive(false);
                row.CurrentDriverId = -1;
                continue;
            }

            DriverAgent d = assigned[i];
            row.CurrentDriverId = d.DriverId;
            row.Root.gameObject.SetActive(true);
            row.NameText.text = d.DriverName;
            row.NameText.color = Color.white;
            row.ActionButton.gameObject.SetActive(true);
            row.ActionButtonText.text = IsRussianLanguage() ? "\u041e\u0442\u043a\u0440\u044b\u0442\u044c" : "View";
        }
    }

    private void OnResidentRowButtonClick(int rowIndex)
    {
        if (buildingQuickHud?.ResidentRows == null ||
            rowIndex < 0 ||
            rowIndex >= buildingQuickHud.ResidentRows.Length)
        {
            return;
        }

        FocusWorkerFromQuickHud(buildingQuickHud.ResidentRows[rowIndex].CurrentDriverId, "personal house HUD");
    }

    private string GetServiceBuildingQuickResourceText(LocationType locationType)
    {
        if (!locations.TryGetValue(locationType, out LocationData location))
        {
            return string.Empty;
        }

        string text = location.ServiceFee > 0
            ? FormatValueLine("Service Fee", $"${location.ServiceFee}")
            : FormatValueLine("Service Fee", "Free");
        int maxStaff = GetMaxBuildingWorkerSlots(locationType);
        if (maxStaff > 0)
        {
            text += "\n" + FormatValueLine("Staff on shift", $"{CountWorkersOnShiftAt(locationType)} / {maxStaff}");
        }
        text += "\n" + FormatValueLine("Workers inside", location.Workers.ToString());
        text += "\n" + FormatValueLine("Building Bank", $"${location.BuildingBank}");
        return text;
    }

    private string GetGasStationQuickResourceText()
    {
        return $"{FormatValueLine("Truck Fuel Service", "Ready")}\n" +
               GetServiceBuildingQuickResourceText(LocationType.GasStation);
    }

    private string GetBarQuickResourceText()
    {
        return GetServiceBuildingQuickResourceText(LocationType.Bar);
    }

    private string GetCanteenQuickResourceText()
    {
        return GetServiceBuildingQuickResourceText(LocationType.Canteen);
    }

    private string GetGamblingHallQuickResourceText()
    {
        locations.TryGetValue(LocationType.GamblingHall, out LocationData gh);
        string text = FormatValueLine("Entry", "Free");
        text += "\n" + FormatValueLine("Staff on shift", $"{CountWorkersOnShiftAt(LocationType.GamblingHall)} / {GetMaxBuildingWorkerSlots(LocationType.GamblingHall)}");
        text += "\n" + FormatValueLine("Workers inside", gh != null ? gh.Workers.ToString() : "0");
        text += "\n" + FormatValueLine("Building Bank", $"${(gh != null ? gh.BuildingBank : 0)}");
        return text;
    }

    private string GetLaborExchangeQuickResourceText()
    {
        bool ru = IsRussianLanguage();
        string text = FormatValueLine(ru ? "\u041a\u043b\u0435\u0440\u043a" : "Clerk on shift", $"{CountWorkersOnShiftAt(LocationType.LaborExchange)} / {GetMaxBuildingWorkerSlots(LocationType.LaborExchange)}");
        text += "\n" + FormatValueLine(ru ? "\u0422\u0440\u0435\u0431\u043e\u0432\u0430\u043d\u0438\u0435" : "Requirement", ru ? "\u0432\u044b\u0441\u0448\u0435\u0435 \u043e\u0431\u0440\u0430\u0437\u043e\u0432\u0430\u043d\u0438\u0435" : "higher education");
        text += "\n" + FormatValueLine(ru ? "\u0414\u043e\u0441\u0442\u0443\u043f\u043d\u044b\u0435 \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u0438" : "Available vacancies", CountAvailableLaborExchangePostings().ToString());
        text += "\n" + FormatValueLine(ru ? "\u0412 \u043f\u0443\u0442\u0438" : "Reserved", CountReservedLaborExchangePostings().ToString());
        text += "\n" + FormatLaborExchangePostingLines(3, ru);
        text += "\n" + FormatValueLine(ru ? "\u0421\u0435\u0433\u043e\u0434\u043d\u044f \u043f\u0440\u0438\u0448\u043b\u0438" : "Applicants today", laborExchangeApplicantsToday.ToString());
        return text;
    }

    private string GetWarehouseQuickResourceText()
    {
        return FormatValueLine("Worker on shift", $"{CountWorkersOnShiftAt(LocationType.Warehouse)} / {WarehouseMaxWorkers}");
    }

    private int CountWorkersOnShiftAt(LocationType locationType)
    {
        if (!IsProductionWorkHour(GetCurrentHour()))
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (driver == null ||
                driver.DutyMode != DriverDutyMode.Logistics ||
                driver.AssignedBuildingType != locationType ||
                driver.RestPhase != DriverRestPhase.None ||
                driver.IsArrivingByBus)
            {
                continue;
            }

            count++;
        }

        return count;
    }

    private static bool HasBuildingContextAction(LocationType locationType)
    {
        return locationType != LocationType.GamblingHall &&
               locationType != LocationType.CityPark &&
               locationType != LocationType.PersonalHouse &&
               locationType != LocationType.IntercityStop &&
               locationType != LocationType.Stop;
    }

    private string GetBuildingQuickContextButtonText(LocationType locationType)
    {
        return locationType switch
        {
            LocationType.Parking => "Open Fleet",
            LocationType.Motel => "Open Drivers",
            LocationType.LaborExchange => "Open Vacancies",
            _ => "Open Resources"
        };
    }

    private void ShiftSelectedStopNumber(int delta)
    {
        if (selectedLocalStopIndex < 0 || selectedLocalStopIndex >= localStops.Count)
        {
            return;
        }

        NormalizeLocalStopNumbers();
        LocationData location = localStops[selectedLocalStopIndex];
        int stopCount = localStops.Count;
        int currentNumber = Mathf.Clamp(location.StopNumber, 1, Mathf.Max(1, stopCount));
        int targetNumber = Mathf.Clamp(currentNumber + delta, 1, Mathf.Max(1, stopCount));
        if (targetNumber == currentNumber)
        {
            UpdateBuildingQuickHud();
            return;
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            if (i == selectedLocalStopIndex)
            {
                continue;
            }

            if (localStops[i].StopNumber == targetNumber)
            {
                localStops[i].StopNumber = currentNumber;
                break;
            }
        }

        location.StopNumber = targetNumber;
        NormalizeLocalStopNumbers();
        isFleetScreenDirty = true;
        LogUiInput($"Quick HUD: changed stop number for {location.Label} to {location.StopNumber}");
        SessionDebugLogger.Log("BUILD", $"{location.Label} stop number changed to {location.StopNumber}.");
        PlayUiSound(uiSelectClip, 0.72f);
        UpdateBuildingQuickHud();
    }

}
