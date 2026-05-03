using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private int lastNeedsEconomyDebugDay = -1;
    private int lastNeedsEconomyDebugHour = -1;

    private const float WorkerMealWarningHours = 8f;
    private const float WorkerMealCriticalHours = 16f;
    private const float WorkerSleepWarningHours = 16f;
    private const float WorkerSleepCriticalHours = 24f;
    private const float WorkerLeisureWarningHours = 24f;
    private const float WorkerLeisureCriticalHours = 48f;

    private const float WorkerMealSeekHours = 6f;
    private const float WorkerSleepSeekHours = 15f;
    private const float WorkerLeisureSeekHours = 10f;
    private const float WorkerNeedRetryCooldownHours = 1.25f;
    private const float WorkerNeedMoneyRetryCooldownHours = 3.5f;
    private const float WorkerNeedNoStockRetryCooldownHours = 0.65f;

    private void UpdateWorkerNeedsClock(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        float hourDelta = Time.deltaTime * gameSpeedMultiplier / (DayNightCycleDuration / 24f);
        if (hourDelta <= 0f)
        {
            return;
        }

        WorkerNeedStatus oldMeal = driver.LastMealNeedStatus;
        WorkerNeedStatus oldSleep = driver.LastSleepNeedStatus;
        WorkerNeedStatus oldLeisure = driver.LastLeisureNeedStatus;

        driver.HoursSinceMeal = Mathf.Min(999f, driver.HoursSinceMeal + hourDelta);
        driver.HoursSinceSleep = Mathf.Min(999f, driver.HoursSinceSleep + hourDelta);
        driver.HoursSinceLeisure = Mathf.Min(999f, driver.HoursSinceLeisure + hourDelta);

        driver.LastMealNeedStatus = GetWorkerNeedStatus(WorkerNeedKind.Meal, driver.HoursSinceMeal);
        driver.LastSleepNeedStatus = GetWorkerNeedStatus(WorkerNeedKind.Sleep, driver.HoursSinceSleep);
        driver.LastLeisureNeedStatus = GetWorkerNeedStatus(WorkerNeedKind.Leisure, driver.HoursSinceLeisure);
        ClearDailyNeedFlagIfCritical(driver, WorkerNeedKind.Meal, driver.LastMealNeedStatus);
        ClearDailyNeedFlagIfCritical(driver, WorkerNeedKind.Sleep, driver.LastSleepNeedStatus);
        ClearDailyNeedFlagIfCritical(driver, WorkerNeedKind.Leisure, driver.LastLeisureNeedStatus);

        LogWorkerNeedStatusChange(driver, WorkerNeedKind.Meal, oldMeal, driver.LastMealNeedStatus, driver.HoursSinceMeal);
        LogWorkerNeedStatusChange(driver, WorkerNeedKind.Sleep, oldSleep, driver.LastSleepNeedStatus, driver.HoursSinceSleep);
        LogWorkerNeedStatusChange(driver, WorkerNeedKind.Leisure, oldLeisure, driver.LastLeisureNeedStatus, driver.HoursSinceLeisure);
        SyncWorkerNeedEffects(driver);

        if (isDriversPanelOpen && selectedWorkerPanelDriverId == driver.DriverId)
        {
            UpdateWorkerNeedsUi(driver, IsRussianLanguage());
        }
    }

    private void UpdateHourlyNeedsEconomyTelemetry()
    {
        int hour = GetCurrentHour();
        if (lastNeedsEconomyDebugDay == currentDay && lastNeedsEconomyDebugHour == hour)
        {
            return;
        }

        lastNeedsEconomyDebugDay = currentDay;
        lastNeedsEconomyDebugHour = hour;

        int hungryDue = 0;
        int sleepyDue = 0;
        int leisureDue = 0;
        int suppressedByDailyFlags = 0;
        int blockedByMoney = 0;
        int idleWithCriticalNeed = 0;
        int inServiceOrRest = 0;
        foreach (DriverAgent driver in driverAgents)
        {
            if (ShouldWorkerSeekMeal(driver)) hungryDue++;
            if (ShouldWorkerSeekSleep(driver)) sleepyDue++;
            if (ShouldWorkerSeekLeisure(driver)) leisureDue++;
            if (IsWorkerDueButSuppressedByDailyFlags(driver)) suppressedByDailyFlags++;
            if (IsWorkerDueButBlockedByMoney(driver)) blockedByMoney++;
            if (IsWorkerIdleWithCriticalNeed(driver)) idleWithCriticalNeed++;
            if (IsWorkerInNeedsServiceOrRest(driver)) inServiceOrRest++;
        }

        int serviceBuildingsMissing = CountMissingNeedsServiceBuildings();
        string stockSnapshot = FormatNeedsEconomyStockSnapshot();
        SessionDebugLogger.Log(
            "NEEDS_ECON",
            $"hourly summary day={currentDay} hour={hour:00}: hungryDue={hungryDue}, sleepyDue={sleepyDue}, leisureDue={leisureDue}, suppressedByDailyFlags={suppressedByDailyFlags}, blockedByMoney={blockedByMoney}, inServiceOrRest={inServiceOrRest}, idleWithCriticalNeed={idleWithCriticalNeed}, serviceBuildingsMissing={serviceBuildingsMissing}, workers={driverAgents.Count}, treasury=${money}, {stockSnapshot}.");
    }

    private float GetCurrentWorldHour()
    {
        return (currentDay - 1) * 24f + dayNightCycleTimer / DayNightCycleDuration * 24f;
    }

    private bool IsWorkerNeedRetryReady(DriverAgent driver, WorkerNeedKind need)
    {
        if (driver == null)
        {
            return false;
        }

        float now = GetCurrentWorldHour();
        return need switch
        {
            WorkerNeedKind.Meal => now >= driver.NextMealRetryAtWorldHour,
            WorkerNeedKind.Sleep => now >= driver.NextSleepRetryAtWorldHour,
            WorkerNeedKind.Leisure => now >= driver.NextLeisureRetryAtWorldHour,
            _ => true
        };
    }

    private void SetWorkerNeedRetryCooldown(DriverAgent driver, WorkerNeedKind need, string reason)
    {
        if (driver == null)
        {
            return;
        }

        float cooldownHours = GetWorkerNeedRetryCooldownHours(reason);
        float retryAt = GetCurrentWorldHour() + cooldownHours;
        switch (need)
        {
            case WorkerNeedKind.Meal:
                driver.NextMealRetryAtWorldHour = retryAt;
                break;
            case WorkerNeedKind.Sleep:
                driver.NextSleepRetryAtWorldHour = retryAt;
                break;
            case WorkerNeedKind.Leisure:
                driver.NextLeisureRetryAtWorldHour = retryAt;
                break;
        }

        LogWorkerDecision(driver, "need-retry-cooldown", $"{need}: {reason}; cooldown={cooldownHours:0.00}h; retryAtWorldHour={retryAt:0.0}", true);
    }

    private static float GetWorkerNeedRetryCooldownHours(string reason)
    {
        if (!string.IsNullOrEmpty(reason) && reason.Contains("not enough money"))
        {
            return WorkerNeedMoneyRetryCooldownHours;
        }

        if (!string.IsNullOrEmpty(reason) &&
            (reason.Contains("has no Food") || reason.Contains("has no Alcohol")))
        {
            return WorkerNeedNoStockRetryCooldownHours;
        }

        return WorkerNeedRetryCooldownHours;
    }

    private bool IsWorkerDueButSuppressedByDailyFlags(DriverAgent driver)
    {
        if (driver == null)
        {
            return false;
        }

        return driver.AteToday && ShouldWorkerSeekMeal(driver) && driver.LastMealNeedStatus != WorkerNeedStatus.Critical ||
               driver.SleptToday && ShouldWorkerSeekSleep(driver) && driver.LastSleepNeedStatus != WorkerNeedStatus.Critical ||
               driver.HadLeisureToday && ShouldWorkerSeekLeisure(driver) && driver.LastLeisureNeedStatus != WorkerNeedStatus.Critical;
    }

    private static void ClearDailyNeedFlagIfCritical(DriverAgent driver, WorkerNeedKind need, WorkerNeedStatus status)
    {
        if (driver == null || status != WorkerNeedStatus.Critical)
        {
            return;
        }

        switch (need)
        {
            case WorkerNeedKind.Meal:
                driver.AteToday = false;
                break;
            case WorkerNeedKind.Sleep:
                driver.SleptToday = false;
                break;
            case WorkerNeedKind.Leisure:
                driver.HadLeisureToday = false;
                break;
        }
    }

    private bool IsWorkerDueButBlockedByMoney(DriverAgent driver)
    {
        if (driver == null)
        {
            return false;
        }

        return ShouldWorkerSeekMeal(driver) && IsWorkerServiceBlockedByMoney(driver, LocationType.Canteen) ||
               ShouldWorkerSeekSleep(driver) && IsWorkerServiceBlockedByMoney(driver, LocationType.Motel) ||
               ShouldWorkerSeekLeisure(driver) &&
               (IsWorkerServiceBlockedByMoney(driver, LocationType.Bar) ||
                IsWorkerServiceBlockedByMoney(driver, LocationType.GamblingHall));
    }

    private bool IsWorkerServiceBlockedByMoney(DriverAgent driver, LocationType type)
    {
        return driver != null &&
               locations.TryGetValue(type, out LocationData service) &&
               service.ServiceFee > 0 &&
               driver.Money < service.ServiceFee;
    }

    private bool IsWorkerIdleWithCriticalNeed(DriverAgent driver)
    {
        return driver != null &&
               (driver.LifeGoal == WorkerLifeGoal.Idle || driver.LifeGoal == WorkerLifeGoal.None) &&
               !IsDriverBusyWalkPhase(driver) &&
               !IsWorkerInNeedsServiceOrRest(driver) &&
               HasCriticalWorkerNeed(driver);
    }

    private bool IsWorkerInNeedsServiceOrRest(DriverAgent driver)
    {
        if (driver == null)
        {
            return false;
        }

        return driver.RestPhase != DriverRestPhase.None ||
               driver.WalkPhase == DriverRescuePhase.IdleAtCanteen ||
               driver.WalkPhase == DriverRescuePhase.IdleAtTrashCan ||
               driver.WalkPhase == DriverRescuePhase.IdleAtBar ||
               driver.WalkPhase == DriverRescuePhase.IdleAtGamblingHall ||
               driver.WalkPhase == DriverRescuePhase.IdleAtCityPark ||
               driver.WalkPhase == DriverRescuePhase.ToMotelEntrance ||
               driver.WalkPhase == DriverRescuePhase.ToPersonalHouseEntrance;
    }

    private int CountMissingNeedsServiceBuildings()
    {
        int missing = 0;
        if (!locations.ContainsKey(LocationType.Canteen)) missing++;
        if (!locations.ContainsKey(LocationType.Motel)) missing++;
        if (!locations.ContainsKey(LocationType.Bar)) missing++;
        if (!locations.ContainsKey(LocationType.GamblingHall)) missing++;
        if (!locations.ContainsKey(LocationType.CityPark)) missing++;
        if (!locations.ContainsKey(LocationType.GasStation)) missing++;
        return missing;
    }

    private string FormatNeedsEconomyStockSnapshot()
    {
        int warehouseFuel = 0;
        int warehouseAlcohol = 0;
        int warehouseFood = 0;
        int gasFuel = 0;
        int barAlcohol = 0;
        int canteenFood = 0;

        if (locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse))
        {
            warehouseFuel = warehouse.FuelStored;
            warehouseAlcohol = warehouse.AlcoholStored;
            warehouseFood = warehouse.FoodStored;
        }

        if (locations.TryGetValue(LocationType.GasStation, out LocationData gasStation))
        {
            gasFuel = gasStation.FuelStored;
        }

        if (locations.TryGetValue(LocationType.Bar, out LocationData bar))
        {
            barAlcohol = bar.AlcoholStored;
        }

        if (locations.TryGetValue(LocationType.Canteen, out LocationData canteen))
        {
            canteenFood = canteen.FoodStored;
        }

        return $"stocks: Warehouse(Fuel={warehouseFuel}/{WarehouseMaxFuelStorage}, Alcohol={warehouseAlcohol}/{WarehouseMaxAlcoholStorage}, Food={warehouseFood}/{WarehouseMaxFoodStorage}), GasStation(Fuel={gasFuel}/{GasStationMaxFuelStorage}), Bar(Alcohol={barAlcohol}/{BarMaxAlcoholStorage}), Canteen(Food={canteenFood}/{CanteenMaxFoodStorage})";
    }

    private void ResetWorkerNeedTimer(DriverAgent driver, WorkerNeedKind need)
    {
        if (driver == null)
        {
            return;
        }

        float oldHours = GetWorkerNeedHours(driver, need);
        WorkerNeedStatus oldStatus = GetWorkerNeedStatus(need, oldHours);
        switch (need)
        {
            case WorkerNeedKind.Meal:
                driver.HoursSinceMeal = 0f;
                driver.NextMealRetryAtWorldHour = 0f;
                driver.LastMealNeedStatus = WorkerNeedStatus.Ok;
                RemoveWorkerEffect(driver, WorkerHungryEffectId);
                RemoveWorkerEffect(driver, WorkerStarvingEffectId);
                break;

            case WorkerNeedKind.Sleep:
                driver.HoursSinceSleep = 0f;
                driver.NextSleepRetryAtWorldHour = 0f;
                driver.LastSleepNeedStatus = WorkerNeedStatus.Ok;
                RemoveWorkerEffect(driver, WorkerSleepDeprivedEffectId);
                RemoveWorkerEffect(driver, WorkerExhaustedEffectId);
                break;

            case WorkerNeedKind.Leisure:
                driver.HoursSinceLeisure = 0f;
                driver.NextLeisureRetryAtWorldHour = 0f;
                driver.LastLeisureNeedStatus = WorkerNeedStatus.Ok;
                RemoveWorkerEffect(driver, WorkerBoredEffectId);
                RemoveWorkerEffect(driver, WorkerBurnedOutEffectId);
                break;
        }

        SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} satisfied {need}; before={oldHours:0.0}h/{oldStatus}; after={FormatWorkerNeedsDebug(driver)}.");
    }

    private bool ShouldWorkerSeekMeal(DriverAgent driver)
    {
        return driver != null && driver.HoursSinceMeal >= WorkerMealSeekHours;
    }

    private bool ShouldWorkerSeekSleep(DriverAgent driver)
    {
        return driver != null && driver.HoursSinceSleep >= WorkerSleepSeekHours;
    }

    private bool ShouldWorkerSeekLeisure(DriverAgent driver)
    {
        return driver != null && driver.HoursSinceLeisure >= WorkerLeisureSeekHours;
    }

    private WorkerNeedStatus GetWorkerNeedStatus(WorkerNeedKind need, float hours)
    {
        return need switch
        {
            WorkerNeedKind.Meal => hours >= WorkerMealCriticalHours ? WorkerNeedStatus.Critical
                : hours >= WorkerMealWarningHours ? WorkerNeedStatus.Warning
                : WorkerNeedStatus.Ok,
            WorkerNeedKind.Sleep => hours >= WorkerSleepCriticalHours ? WorkerNeedStatus.Critical
                : hours >= WorkerSleepWarningHours ? WorkerNeedStatus.Warning
                : WorkerNeedStatus.Ok,
            WorkerNeedKind.Leisure => hours >= WorkerLeisureCriticalHours ? WorkerNeedStatus.Critical
                : hours >= WorkerLeisureWarningHours ? WorkerNeedStatus.Warning
                : WorkerNeedStatus.Ok,
            _ => WorkerNeedStatus.Ok
        };
    }

    private Color GetWorkerNeedStatusColor(WorkerNeedStatus status)
    {
        return status switch
        {
            WorkerNeedStatus.Warning => new Color(0.96f, 0.72f, 0.30f, 1f),
            WorkerNeedStatus.Critical => new Color(0.95f, 0.32f, 0.25f, 1f),
            _ => new Color(0.58f, 0.88f, 0.54f, 1f)
        };
    }

    private string FormatWorkerNeedStatus(WorkerNeedStatus status, bool ru)
    {
        return status switch
        {
            WorkerNeedStatus.Warning => ru ? "\u0422\u0440\u0435\u0431\u0443\u0435\u0442 \u0432\u043d\u0438\u043c\u0430\u043d\u0438\u044f" : "Needs attention",
            WorkerNeedStatus.Critical => ru ? "\u0414\u0435\u0431\u0430\u0444\u0444" : "Debuff",
            _ => ru ? "\u041e\u041a" : "OK"
        };
    }

    private string FormatWorkerNeedLine(string label, float hours, WorkerNeedStatus status, bool ru)
    {
        string hourLabel = ru ? "\u0447" : "h";
        return $"{label}: {Mathf.FloorToInt(hours)}{hourLabel}  {FormatWorkerNeedStatus(status, ru)}";
    }

    private void LogWorkerNeedStatusChange(DriverAgent driver, WorkerNeedKind need, WorkerNeedStatus oldStatus, WorkerNeedStatus newStatus, float hours)
    {
        if (driver == null || oldStatus == newStatus || newStatus == WorkerNeedStatus.Ok)
        {
            return;
        }

        SessionDebugLogger.Log("NEEDS", $"{driver.DriverName} {need} need became {newStatus} after {hours:0.0}h; snapshot={FormatWorkerNeedsDebug(driver)}.");
    }

    private float GetWorkerNeedHours(DriverAgent driver, WorkerNeedKind need)
    {
        if (driver == null)
        {
            return 0f;
        }

        return need switch
        {
            WorkerNeedKind.Meal => driver.HoursSinceMeal,
            WorkerNeedKind.Sleep => driver.HoursSinceSleep,
            WorkerNeedKind.Leisure => driver.HoursSinceLeisure,
            _ => 0f
        };
    }

    private WorkerNeedStatus GetWorkerNeedLastStatus(DriverAgent driver, WorkerNeedKind need)
    {
        if (driver == null)
        {
            return WorkerNeedStatus.Ok;
        }

        return need switch
        {
            WorkerNeedKind.Meal => driver.LastMealNeedStatus,
            WorkerNeedKind.Sleep => driver.LastSleepNeedStatus,
            WorkerNeedKind.Leisure => driver.LastLeisureNeedStatus,
            _ => WorkerNeedStatus.Ok
        };
    }

    private string FormatWorkerNeedDebug(DriverAgent driver, WorkerNeedKind need)
    {
        float hours = GetWorkerNeedHours(driver, need);
        return $"{need}={hours:0.0}h/{GetWorkerNeedLastStatus(driver, need)}";
    }

    private string FormatWorkerNeedsDebug(DriverAgent driver)
    {
        if (driver == null)
        {
            return "none";
        }

        return $"{FormatWorkerNeedDebug(driver, WorkerNeedKind.Meal)}, {FormatWorkerNeedDebug(driver, WorkerNeedKind.Sleep)}, {FormatWorkerNeedDebug(driver, WorkerNeedKind.Leisure)}";
    }

    private string GetWorkerServiceUnavailableReason(DriverAgent driver, LocationType type)
    {
        if (driver == null)
        {
            return "no worker";
        }

        if (!locations.TryGetValue(type, out LocationData service))
        {
            return $"{type} not built";
        }

        if (driver.Money < service.ServiceFee)
        {
            return $"not enough money (${driver.Money}/${service.ServiceFee})";
        }

        return type switch
        {
            LocationType.Canteen when service.FoodStored <= 0 => "Canteen has no Food",
            LocationType.Bar when service.AlcoholStored <= 0 => "Bar has no Alcohol",
            _ => "available"
        };
    }

    private void UpdateWorkerNeedsUi(DriverAgent driver, bool ru)
    {
        if (driver == null || driversScreenUi == null)
        {
            return;
        }

        if (driversScreenUi.DetailNeedsTitleText != null)
        {
            driversScreenUi.DetailNeedsTitleText.text = ru ? "\u041f\u043e\u0442\u0440\u0435\u0431\u043d\u043e\u0441\u0442\u0438" : "Needs";
        }

        SetWorkerNeedText(
            driversScreenUi.DetailMealNeedText,
            FormatWorkerNeedLine(ru ? "\u0415\u0434\u0430" : "Food", driver.HoursSinceMeal, driver.LastMealNeedStatus, ru),
            driver.LastMealNeedStatus);

        SetWorkerNeedText(
            driversScreenUi.DetailSleepNeedText,
            FormatWorkerNeedLine(ru ? "\u0421\u043e\u043d" : "Sleep", driver.HoursSinceSleep, driver.LastSleepNeedStatus, ru),
            driver.LastSleepNeedStatus);

        SetWorkerNeedText(
            driversScreenUi.DetailLeisureNeedText,
            FormatWorkerNeedLine(ru ? "\u0420\u0430\u0437\u0432\u043b\u0435\u0447\u0435\u043d\u0438\u0435" : "Leisure", driver.HoursSinceLeisure, driver.LastLeisureNeedStatus, ru),
            driver.LastLeisureNeedStatus);

        UpdateWorkerPerksUi(driver, ru);
    }

    private void SetWorkerNeedText(Text text, string value, WorkerNeedStatus status)
    {
        if (text == null)
        {
            return;
        }

        text.text = value;
        text.color = GetWorkerNeedStatusColor(status);
    }
}

