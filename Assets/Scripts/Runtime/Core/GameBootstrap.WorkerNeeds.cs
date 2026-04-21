using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const float WorkerMealWarningHours = 8f;
    private const float WorkerMealCriticalHours = 16f;
    private const float WorkerSleepWarningHours = 16f;
    private const float WorkerSleepCriticalHours = 24f;
    private const float WorkerLeisureWarningHours = 24f;
    private const float WorkerLeisureCriticalHours = 48f;

    private const float WorkerMealSeekHours = 6f;
    private const float WorkerSleepSeekHours = 15f;
    private const float WorkerLeisureSeekHours = 18f;

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

        LogWorkerNeedStatusChange(driver, WorkerNeedKind.Meal, oldMeal, driver.LastMealNeedStatus, driver.HoursSinceMeal);
        LogWorkerNeedStatusChange(driver, WorkerNeedKind.Sleep, oldSleep, driver.LastSleepNeedStatus, driver.HoursSinceSleep);
        LogWorkerNeedStatusChange(driver, WorkerNeedKind.Leisure, oldLeisure, driver.LastLeisureNeedStatus, driver.HoursSinceLeisure);
        SyncWorkerNeedEffects(driver);

        if (isDriversPanelOpen && selectedWorkerPanelDriverId == driver.DriverId)
        {
            UpdateWorkerNeedsUi(driver, IsRussianLanguage());
        }
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
                driver.LastMealNeedStatus = WorkerNeedStatus.Ok;
                RemoveWorkerEffect(driver, WorkerHungryEffectId);
                RemoveWorkerEffect(driver, WorkerStarvingEffectId);
                break;

            case WorkerNeedKind.Sleep:
                driver.HoursSinceSleep = 0f;
                driver.LastSleepNeedStatus = WorkerNeedStatus.Ok;
                RemoveWorkerEffect(driver, WorkerSleepDeprivedEffectId);
                RemoveWorkerEffect(driver, WorkerExhaustedEffectId);
                break;

            case WorkerNeedKind.Leisure:
                driver.HoursSinceLeisure = 0f;
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
