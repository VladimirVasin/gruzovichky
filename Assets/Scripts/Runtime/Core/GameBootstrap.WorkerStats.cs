using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int WorkerSkillMin = 1;
    private const int WorkerSkillMax = 10;
    private const float WorkerSkillTooltipWidth = 330f;
    private const float WorkerSkillTooltipHeight = 86f;
    private const float WorkerEffectTooltipHeight = 220f;
    private const int WorkerEffectHudRowCount = 8;
    private const int WorkerPerkHudRowCount = 5;
    private const int WorkerNegativePerkCount = 1;
    private const int WorkerPositivePerkCount = 4;
    private static readonly WorkerPerkKind[] NegativePerks = { WorkerPerkKind.Alcoholism, WorkerPerkKind.Gambler };
    private static readonly WorkerPerkKind[] PositivePerks =
    {
        WorkerPerkKind.Nightowl, WorkerPerkKind.Ironman, WorkerPerkKind.Motorhead, WorkerPerkKind.Trader,
        WorkerPerkKind.Handyman, WorkerPerkKind.Socialite, WorkerPerkKind.Frugal, WorkerPerkKind.Quicklearner
    };
    private const string WorkerDrunkEffectId = "drunk";
    private const string WorkerHangoverEffectId = "hangover";
    private const float WorkerDrunkEffectDurationHours = 4f;
    private const float WorkerAlcoholicDrunkEffectDurationHours = 6f;
    private const int WorkerDrunkDrivingDelta = -5;
    private const int WorkerDrunkProductionDelta = 1;
    private const int WorkerDrunkLogisticsDelta = 1;
    private const int WorkerHangoverDrivingDelta = -6;
    private const int WorkerHangoverStaminaDelta = -3;
    private const int WorkerHangoverProductionDelta = -2;
    private const int WorkerHangoverLogisticsDelta = -2;
    private const string WorkerFedEffectId = "fed";
    private const float WorkerFedEffectDurationHours = 6f;
    private const int WorkerFedStaminaDelta = 2;
    private const int WorkerFedProductionDelta = 1;
    private const int WorkerFedLogisticsDelta = 1;
    private const float WorkerNeedEffectRefreshHours = 0.5f;
    private const string WorkerRestedEffectId = "rested";
    private const string WorkerHungryEffectId = "hungry";
    private const string WorkerStarvingEffectId = "starving";
    private const string WorkerSleepDeprivedEffectId = "sleep_deprived";
    private const string WorkerExhaustedEffectId = "exhausted";
    private const string WorkerBoredEffectId = "bored";
    private const string WorkerBurnedOutEffectId = "burned_out";
    private const string WorkerWorkedHardEffectId = "worked_hard";
    private const string WorkerForestAirEffectId = "forest_air";
    private const string WorkerSawdustEffectId = "sawdust";
    private const string WorkerWarehouseFlowEffectId = "warehouse_flow";
    private const string WorkerCraftFocusEffectId = "craft_focus";
    private const string WorkerRoadFocusEffectId = "road_focus";
    private const string WorkerRoadFatigueEffectId = "road_fatigue";
    private const string WorkerRaceRushEffectId = "race_rush";
    private const string WorkerLuckyEffectId = "lucky";
    private const float WorkerLuckyEffectDurationHours = 4f;
    private enum WorkerSkillKind
    {
        Driving,
        Stamina,
        Production,
        Logistics
    }

    private void AssignWorkerStats(DriverAgent driver)
    {
        if (driver == null) return;

        int seed = StableWorkerStatsHash(driver.DriverName) ^ (driver.DriverId * 19349663);
        System.Random rng = new(seed);

        driver.DrivingSkill = RollWorkerSkill(rng);
        driver.StaminaSkill = RollWorkerSkill(rng);
        driver.ProductionSkill = RollWorkerSkill(rng);
        driver.LogisticsSkill = RollWorkerSkill(rng);
        AssignWorkerPerks(driver, rng);

        // Give every worker a small standout angle so hires feel less samey.
        int specialty = rng.Next(4);
        int specialtyBonus = rng.Next(1, 3);
        switch (specialty)
        {
            case 0:
                driver.DrivingSkill = ClampWorkerSkill(driver.DrivingSkill + specialtyBonus);
                break;
            case 1:
                driver.StaminaSkill = ClampWorkerSkill(driver.StaminaSkill + specialtyBonus);
                break;
            case 2:
                driver.ProductionSkill = ClampWorkerSkill(driver.ProductionSkill + specialtyBonus);
                break;
            default:
                driver.LogisticsSkill = ClampWorkerSkill(driver.LogisticsSkill + specialtyBonus);
                break;
        }

        driver.HasWorkerStats = true;
    }

    private void AssignWorkerPerks(DriverAgent driver, System.Random rng)
    {
        if (driver == null || rng == null) return;

        driver.Perks.Clear();

        int negativeCount = Mathf.Min(WorkerNegativePerkCount, NegativePerks.Length);
        int positiveCount = Mathf.Min(WorkerPositivePerkCount, PositivePerks.Length);

        WorkerPerkKind[] negativePool = (WorkerPerkKind[])NegativePerks.Clone();
        WorkerPerkKind[] positivePool = (WorkerPerkKind[])PositivePerks.Clone();

        ShuffleWorkerPerkPool(negativePool, rng);
        ShuffleWorkerPerkPool(positivePool, rng);

        for (int i = 0; i < negativeCount; i++)
        {
            driver.Perks.Add(negativePool[i]);
        }

        for (int i = 0; i < positiveCount; i++)
        {
            driver.Perks.Add(positivePool[i]);
        }
    }

    private void EnsureWorkerStats(DriverAgent driver)
    {
        if (driver == null) return;
        if (!driver.HasWorkerStats)
        {
            AssignWorkerStats(driver);
            return;
        }

        int expectedPerkCount = Mathf.Min(WorkerNegativePerkCount, NegativePerks.Length) +
                                Mathf.Min(WorkerPositivePerkCount, PositivePerks.Length);
        if (driver.Perks.Count != expectedPerkCount)
        {
            int seed = StableWorkerStatsHash(driver.DriverName) ^ (driver.DriverId * 19349663);
            AssignWorkerPerks(driver, new System.Random(seed));
        }
    }

    private static void ShuffleWorkerPerkPool(WorkerPerkKind[] perks, System.Random rng)
    {
        if (perks == null || rng == null) return;

        for (int i = perks.Length - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (perks[i], perks[swapIndex]) = (perks[swapIndex], perks[i]);
        }
    }

    private void UpdateWorkerStatsUi(DriverAgent driver, bool ru)
    {
        if (driversScreenUi == null) return;
        EnsureWorkerStats(driver);

        if (driversScreenUi.DetailSkillsTitleText != null)
        {
            driversScreenUi.DetailSkillsTitleText.text = ru ? "\u041d\u0430\u0432\u044b\u043a\u0438" : "Skills";
        }

        SetWorkerSkillText(driversScreenUi.DetailDrivingSkillText, GetWorkerSkillLabel(WorkerSkillKind.Driving, ru), driver?.DrivingSkill ?? 0, GetWorkerSkillDelta(driver, WorkerSkillKind.Driving));
        SetWorkerSkillText(driversScreenUi.DetailStaminaSkillText, GetWorkerSkillLabel(WorkerSkillKind.Stamina, ru), driver?.StaminaSkill ?? 0, GetWorkerSkillDelta(driver, WorkerSkillKind.Stamina));
        SetWorkerSkillText(driversScreenUi.DetailProductionSkillText, GetWorkerSkillLabel(WorkerSkillKind.Production, ru), driver?.ProductionSkill ?? 0, GetWorkerSkillDelta(driver, WorkerSkillKind.Production));
        SetWorkerSkillText(driversScreenUi.DetailLogisticsSkillText, GetWorkerSkillLabel(WorkerSkillKind.Logistics, ru), driver?.LogisticsSkill ?? 0, GetWorkerSkillDelta(driver, WorkerSkillKind.Logistics));
        UpdateWorkerEffectsUi(driver, ru);
    }

    private void ApplyWorkerDrunkEffect(DriverAgent driver)
    {
        bool hasAlcoholism = HasWorkerPerk(driver, WorkerPerkKind.Alcoholism);
        RemoveWorkerEffect(driver, WorkerHangoverEffectId);
        AddOrRefreshWorkerEffect(
            driver,
            WorkerDrunkEffectId,
            "Drunk",
            "\u041e\u043f\u044c\u044f\u043d\u0435\u043d\u0438\u0435",
            hasAlcoholism
                ? "Alcoholism makes this effect last longer: driving suffers harder, but ordinary work gets a stronger short burst."
                : "Driving is heavily reduced, while ordinary work gets a small short-lived boost.",
            hasAlcoholism
                ? "\u0410\u043b\u043a\u043e\u0433\u043e\u043b\u0438\u0437\u043c \u0443\u0441\u0438\u043b\u0438\u0432\u0430\u0435\u0442 \u044d\u0442\u043e\u0442 \u044d\u0444\u0444\u0435\u043a\u0442: \u0432\u043e\u0436\u0434\u0435\u043d\u0438\u0435 \u0441\u0442\u0440\u0430\u0434\u0430\u0435\u0442 \u0441\u0438\u043b\u044c\u043d\u0435\u0435, \u0430 \u043e\u0431\u044b\u0447\u043d\u0430\u044f \u0440\u0430\u0431\u043e\u0442\u0430 \u043d\u0430 \u0432\u0440\u0435\u043c\u044f \u0438\u0434\u0435\u0442 \u0431\u043e\u0434\u0440\u0435\u0435."
                : "\u0412\u043e\u0436\u0434\u0435\u043d\u0438\u0435 \u0441\u0438\u043b\u044c\u043d\u043e \u0441\u043d\u0438\u0436\u0435\u043d\u043e, \u0430 \u043e\u0431\u044b\u0447\u043d\u0430\u044f \u0440\u0430\u0431\u043e\u0442\u0430 \u0447\u0443\u0442\u044c \u0443\u0441\u0438\u043b\u0435\u043d\u0430.",
            hasAlcoholism ? WorkerAlcoholicDrunkEffectDurationHours : WorkerDrunkEffectDurationHours,
            drivingDelta: hasAlcoholism ? WorkerDrunkDrivingDelta - 1 : WorkerDrunkDrivingDelta,
            productionDelta: hasAlcoholism ? WorkerDrunkProductionDelta + 1 : WorkerDrunkProductionDelta,
            logisticsDelta: hasAlcoholism ? WorkerDrunkLogisticsDelta + 1 : WorkerDrunkLogisticsDelta);
    }

    private void ApplyWorkerHangoverEffect(DriverAgent driver)
    {
        AddOrRefreshWorkerEffect(
            driver,
            WorkerHangoverEffectId,
            "Hangover",
            "\u041f\u043e\u0445\u043c\u0435\u043b\u044c\u0435",
            "After Drunk wears off, the worker crashes hard. The debuff stays until they visit the Bar again.",
            "\u041f\u043e\u0441\u043b\u0435 \u043e\u043f\u044c\u044f\u043d\u0435\u043d\u0438\u044f \u0440\u0430\u0431\u043e\u0442\u043d\u0438\u043a\u0430 \u043d\u0430\u043a\u0440\u044b\u0432\u0430\u0435\u0442 \u0436\u0451\u0441\u0442\u043a\u0438\u043c \u043f\u043e\u0445\u043c\u0435\u043b\u044c\u0435\u043c. \u042d\u0444\u0444\u0435\u043a\u0442 \u0434\u0435\u0440\u0436\u0438\u0442\u0441\u044f \u0434\u043e \u0441\u043b\u0435\u0434\u0443\u044e\u0449\u0435\u0433\u043e \u043f\u043e\u0445\u043e\u0434\u0430 \u0432 \u0411\u0430\u0440.",
            float.PositiveInfinity,
            drivingDelta: WorkerHangoverDrivingDelta,
            staminaDelta: WorkerHangoverStaminaDelta,
            productionDelta: WorkerHangoverProductionDelta,
            logisticsDelta: WorkerHangoverLogisticsDelta);
    }

    private void ApplyWorkerFedEffect(DriverAgent driver)
    {
        AddOrRefreshWorkerEffect(
            driver,
            WorkerFedEffectId,
            "Well Fed",
            "\u0421\u044b\u0442\u043e\u0441\u0442\u044c",
            "A proper meal improves stamina and makes ordinary work a little steadier.",
            "\u041d\u043e\u0440\u043c\u0430\u043b\u044c\u043d\u0430\u044f \u0435\u0434\u0430 \u043f\u043e\u0432\u044b\u0448\u0430\u0435\u0442 \u0432\u044b\u043d\u043e\u0441\u043b\u0438\u0432\u043e\u0441\u0442\u044c \u0438 \u0447\u0443\u0442\u044c \u0441\u0442\u0430\u0431\u0438\u043b\u0438\u0437\u0438\u0440\u0443\u0435\u0442 \u043e\u0431\u044b\u0447\u043d\u0443\u044e \u0440\u0430\u0431\u043e\u0442\u0443.",
            WorkerFedEffectDurationHours,
            staminaDelta: WorkerFedStaminaDelta,
            productionDelta: WorkerFedProductionDelta,
            logisticsDelta: WorkerFedLogisticsDelta);
    }

    private void ApplyWorkerRestedEffect(DriverAgent driver)
    {
        AddOrRefreshWorkerEffect(
            driver,
            WorkerRestedEffectId,
            "Rested",
            "\u041e\u0442\u0434\u043e\u0445\u043d\u0443\u043b",
            "Sleep restored focus: better stamina and a steadier hand on the wheel.",
            "\u0421\u043e\u043d \u0432\u0435\u0440\u043d\u0443\u043b \u0444\u043e\u043a\u0443\u0441: \u0431\u043e\u043b\u044c\u0448\u0435 \u0432\u044b\u043d\u043e\u0441\u043b\u0438\u0432\u043e\u0441\u0442\u0438 \u0438 \u0441\u043f\u043e\u043a\u043e\u0439\u043d\u0435\u0435 \u0440\u0443\u043a\u0430 \u043d\u0430 \u0440\u0443\u043b\u0435.",
            8f,
            drivingDelta: 1,
            staminaDelta: 2);
    }

    private void ApplyWorkerAfterWorkEffects(DriverAgent driver, LocationType? buildingType)
    {
        AddOrRefreshWorkerEffect(
            driver,
            WorkerWorkedHardEffectId,
            "Worked Hard",
            "\u041f\u043e\u0441\u043b\u0435 \u0441\u043c\u0435\u043d\u044b",
            "A completed workday leaves a little fatigue behind.",
            "\u041e\u0442\u0440\u0430\u0431\u043e\u0442\u0430\u043d\u043d\u044b\u0439 \u0434\u0435\u043d\u044c \u043e\u0441\u0442\u0430\u0432\u043b\u044f\u0435\u0442 \u043d\u0435\u043c\u043d\u043e\u0433\u043e \u0443\u0441\u0442\u0430\u043b\u043e\u0441\u0442\u0438.",
            6f,
            staminaDelta: -1);

        switch (buildingType)
        {
            case LocationType.Forest:
                AddOrRefreshWorkerEffect(
                    driver,
                    WorkerForestAirEffectId,
                    "Forest Air",
                    "\u041b\u0435\u0441\u043d\u0430\u044f \u0437\u0430\u043a\u0430\u043b\u043a\u0430",
                    "Forest work is rough, but the air keeps the worker sharp.",
                    "\u041b\u0435\u0441\u043d\u0430\u044f \u0440\u0430\u0431\u043e\u0442\u0430 \u0433\u0440\u0443\u0431\u0430\u044f, \u0437\u0430\u0442\u043e \u0432\u043e\u0437\u0434\u0443\u0445 \u0434\u0435\u0440\u0436\u0438\u0442 \u0432 \u0442\u043e\u043d\u0443\u0441\u0435.",
                    4f,
                    staminaDelta: 1,
                    productionDelta: 1);
                break;

            case LocationType.Sawmill:
                AddOrRefreshWorkerEffect(
                    driver,
                    WorkerSawdustEffectId,
                    "Sawdust",
                    "\u041f\u044b\u043b\u044c \u043b\u0435\u0441\u043e\u043f\u0438\u043b\u043a\u0438",
                    "The production rhythm is good. The dust is not.",
                    "\u0420\u0438\u0442\u043c \u043f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0441\u0442\u0432\u0430 \u0445\u043e\u0440\u043e\u0448. \u041f\u044b\u043b\u044c - \u043d\u0435\u0442.",
                    4f,
                    staminaDelta: -1,
                    productionDelta: 1);
                break;

            case LocationType.Warehouse:
                AddOrRefreshWorkerEffect(
                    driver,
                    WorkerWarehouseFlowEffectId,
                    "Warehouse Flow",
                    "\u0421\u043a\u043b\u0430\u0434\u0441\u043a\u043e\u0439 \u0440\u0438\u0442\u043c",
                    "Loading work clicks into place, at the cost of tired legs.",
                    "\u041f\u043e\u0433\u0440\u0443\u0437\u043a\u0430 \u0438\u0434\u0435\u0442 \u0440\u043e\u0432\u043d\u0435\u0435, \u043d\u043e \u043d\u043e\u0433\u0438 \u0443\u0441\u0442\u0430\u044e\u0442.",
                    4f,
                    staminaDelta: -1,
                    logisticsDelta: 2);
                break;

            case LocationType.FurnitureFactory:
                AddOrRefreshWorkerEffect(
                    driver,
                    WorkerCraftFocusEffectId,
                    "Craft Focus",
                    "\u0422\u043e\u0447\u043d\u0430\u044f \u0441\u0431\u043e\u0440\u043a\u0430",
                    "Careful assembly improves production but makes quick logistics feel clumsy.",
                    "\u0410\u043a\u043a\u0443\u0440\u0430\u0442\u043d\u0430\u044f \u0441\u0431\u043e\u0440\u043a\u0430 \u0443\u0441\u0438\u043b\u0438\u0432\u0430\u0435\u0442 \u043f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0441\u0442\u0432\u043e, \u043d\u043e \u043c\u0435\u0448\u0430\u0435\u0442 \u0431\u044b\u0441\u0442\u0440\u043e\u0439 \u043b\u043e\u0433\u0438\u0441\u0442\u0438\u043a\u0435.",
                    4f,
                    productionDelta: 2,
                    logisticsDelta: -1);
                break;
        }
    }

    private void ApplyWorkerRoadFocusEffect(DriverAgent driver)
    {
        AddOrRefreshWorkerEffect(
            driver,
            WorkerRoadFocusEffectId,
            "Road Focus",
            "\u0414\u043e\u0440\u043e\u0436\u043d\u044b\u0439 \u0444\u043e\u043a\u0443\u0441",
            "A completed route sharpens driving and loading rhythm.",
            "\u0417\u0430\u0432\u0435\u0440\u0448\u0435\u043d\u043d\u044b\u0439 \u0440\u0435\u0439\u0441 \u043e\u0431\u043e\u0441\u0442\u0440\u044f\u0435\u0442 \u0432\u043e\u0436\u0434\u0435\u043d\u0438\u0435 \u0438 \u0440\u0438\u0442\u043c \u043f\u043e\u0433\u0440\u0443\u0437\u043a\u0438.",
            4f,
            drivingDelta: 1,
            logisticsDelta: 1);
    }

    private void ApplyWorkerRoadFatigueEffect(DriverAgent driver)
    {
        AddOrRefreshWorkerEffect(
            driver,
            WorkerRoadFatigueEffectId,
            "Road Fatigue",
            "\u0414\u043e\u0440\u043e\u0436\u043d\u0430\u044f \u0443\u0441\u0442\u0430\u043b\u043e\u0441\u0442\u044c",
            "A long intercity run leaves the driver tired and less steady.",
            "\u0414\u0430\u043b\u044c\u043d\u0438\u0439 \u043c\u0435\u0436\u0433\u043e\u0440\u043e\u0434 \u043e\u0441\u0442\u0430\u0432\u043b\u044f\u0435\u0442 \u0432\u043e\u0434\u0438\u0442\u0435\u043b\u044f \u0443\u0441\u0442\u0430\u0432\u0448\u0438\u043c \u0438 \u043c\u0435\u043d\u0435\u0435 \u0442\u043e\u0447\u043d\u044b\u043c.",
            6f,
            drivingDelta: -1,
            staminaDelta: -2);
    }

    private void ApplyWorkerRaceRushEffect(DriverAgent driver)
    {
        AddOrRefreshWorkerEffect(
            driver,
            WorkerRaceRushEffectId,
            "Race Rush",
            "\u0413\u043e\u043d\u043e\u0447\u043d\u044b\u0439 \u0430\u0437\u0430\u0440\u0442",
            "Adrenaline boosts driving for a while, but the body pays for it.",
            "\u0410\u0434\u0440\u0435\u043d\u0430\u043b\u0438\u043d \u043d\u0430 \u0432\u0440\u0435\u043c\u044f \u0443\u0441\u0438\u043b\u0438\u0432\u0430\u0435\u0442 \u0432\u043e\u0436\u0434\u0435\u043d\u0438\u0435, \u043d\u043e \u0442\u0435\u043b\u043e \u043f\u043b\u0430\u0442\u0438\u0442.",
            3f,
            drivingDelta: 2,
            staminaDelta: -1);
    }

    private void ApplyWorkerLuckyEffect(DriverAgent driver)
    {
        AddOrRefreshWorkerEffect(
            driver,
            WorkerLuckyEffectId,
            "Lucky",
            "\u0412 \u0443\u0434\u0430\u0440\u0435",
            "A winning streak puts a spring in the worker's step.",
            "\u0423\u0434\u0430\u0447\u043d\u0430\u044f \u043f\u0430\u0440\u0442\u0438\u044f \u043f\u0440\u0438\u0434\u0430\u0451\u0442 \u0440\u0430\u0431\u043e\u0442\u043d\u0438\u043a\u0443 \u044d\u043d\u0435\u0440\u0433\u0438\u0438.",
            WorkerLuckyEffectDurationHours,
            productionDelta: 1,
            logisticsDelta: 1);
    }

    private void UpdateWorkerEffectsClock(DriverAgent driver)
    {
        if (driver == null || driver.ActiveEffects.Count == 0)
        {
            return;
        }

        float hourDelta = Time.deltaTime * gameSpeedMultiplier / (DayNightCycleDuration / 24f);
        if (hourDelta <= 0f)
        {
            return;
        }

        for (int i = driver.ActiveEffects.Count - 1; i >= 0; i--)
        {
            WorkerEffectState effect = driver.ActiveEffects[i];
            if (effect == null)
            {
                driver.ActiveEffects.RemoveAt(i);
                continue;
            }

            effect.RemainingHours -= hourDelta;
            if (effect.RemainingHours <= 0f)
            {
                SessionDebugLogger.Log("WORKER_EFFECT", $"{driver.DriverName} effect expired: {effect.EffectId}.");
                driver.ActiveEffects.RemoveAt(i);
                if (effect.EffectId == WorkerDrunkEffectId && HasWorkerPerk(driver, WorkerPerkKind.Alcoholism))
                {
                    ApplyWorkerHangoverEffect(driver);
                }
            }
        }

        if (isDriversPanelOpen && selectedWorkerPanelDriverId == driver.DriverId)
        {
            UpdateWorkerEffectsUi(driver, IsRussianLanguage());
        }
    }

    private void AddOrRefreshWorkerEffect(
        DriverAgent driver,
        string effectId,
        string englishName,
        string russianName,
        string englishDescription,
        string russianDescription,
        float durationHours,
        int drivingDelta = 0,
        int staminaDelta = 0,
        int productionDelta = 0,
        int logisticsDelta = 0)
    {
        if (driver == null || string.IsNullOrWhiteSpace(effectId) || durationHours <= 0f)
        {
            return;
        }

        WorkerEffectState effect = null;
        bool isNewEffect = false;
        for (int i = 0; i < driver.ActiveEffects.Count; i++)
        {
            if (driver.ActiveEffects[i]?.EffectId == effectId)
            {
                effect = driver.ActiveEffects[i];
                break;
            }
        }

        if (effect == null)
        {
            effect = new WorkerEffectState { EffectId = effectId };
            driver.ActiveEffects.Add(effect);
            isNewEffect = true;
        }

        bool changed =
            effect.EnglishName != englishName ||
            effect.RussianName != russianName ||
            effect.EnglishDescription != englishDescription ||
            effect.RussianDescription != russianDescription ||
            effect.DrivingDelta != drivingDelta ||
            effect.StaminaDelta != staminaDelta ||
            effect.ProductionDelta != productionDelta ||
            effect.LogisticsDelta != logisticsDelta;

        effect.EnglishName = englishName;
        effect.RussianName = russianName;
        effect.EnglishDescription = englishDescription;
        effect.RussianDescription = russianDescription;
        effect.RemainingHours = durationHours;
        effect.DrivingDelta = drivingDelta;
        effect.StaminaDelta = staminaDelta;
        effect.ProductionDelta = productionDelta;
        effect.LogisticsDelta = logisticsDelta;

        if (isNewEffect || changed)
        {
            SessionDebugLogger.Log("WORKER_EFFECT", $"{driver.DriverName} effect active: {effectId}; duration={durationHours:0.0}h; modifiers={FormatWorkerEffectModifiers(effect, false)}.");
        }

        if (isDriversPanelOpen && selectedWorkerPanelDriverId == driver.DriverId)
        {
            UpdateWorkerEffectsUi(driver, IsRussianLanguage());
        }
    }

    private void RemoveWorkerEffect(DriverAgent driver, string effectId, bool logRemoval = true)
    {
        if (driver == null || string.IsNullOrWhiteSpace(effectId) || driver.ActiveEffects.Count == 0)
        {
            return;
        }

        for (int i = driver.ActiveEffects.Count - 1; i >= 0; i--)
        {
            WorkerEffectState effect = driver.ActiveEffects[i];
            if (effect == null || effect.EffectId != effectId)
            {
                continue;
            }

            driver.ActiveEffects.RemoveAt(i);
            if (logRemoval)
            {
                SessionDebugLogger.Log("WORKER_EFFECT", $"{driver.DriverName} effect removed: {effectId}.");
            }
        }

        if (isDriversPanelOpen && selectedWorkerPanelDriverId == driver.DriverId)
        {
            UpdateWorkerEffectsUi(driver, IsRussianLanguage());
        }
    }

    private void UpdateWorkerEffectsUi(DriverAgent driver, bool ru)
    {
        if (driversScreenUi == null)
        {
            return;
        }

        if (driversScreenUi.DetailEffectsTitleText != null)
        {
            driversScreenUi.DetailEffectsTitleText.text = ru ? "\u042d\u0444\u0444\u0435\u043a\u0442\u044b" : "Effects";
        }

        if (driver == null || driver.ActiveEffects.Count == 0)
        {
            if (driversScreenUi.DetailEffectsEmptyText != null)
            {
                driversScreenUi.DetailEffectsEmptyText.gameObject.SetActive(true);
                driversScreenUi.DetailEffectsEmptyText.text = ru ? "\u041d\u0435\u0442 \u0430\u043a\u0442\u0438\u0432\u043d\u044b\u0445 \u044d\u0444\u0444\u0435\u043a\u0442\u043e\u0432" : "No active effects";
            }

            for (int i = 0; i < driversScreenUi.DetailEffectTexts.Count; i++)
            {
                driversScreenUi.DetailEffectTexts[i].gameObject.SetActive(false);
            }
            return;
        }

        if (driversScreenUi.DetailEffectsEmptyText != null)
        {
            driversScreenUi.DetailEffectsEmptyText.gameObject.SetActive(false);
        }

        int rowCount = driversScreenUi.DetailEffectTexts.Count;
        for (int i = 0; i < rowCount; i++)
        {
            Text rowText = driversScreenUi.DetailEffectTexts[i];
            if (i >= driver.ActiveEffects.Count)
            {
                rowText.gameObject.SetActive(false);
                continue;
            }

            WorkerEffectState effect = driver.ActiveEffects[i];
            rowText.gameObject.SetActive(effect != null);
            if (effect == null)
            {
                continue;
            }
 
            string accent = ColorUtility.ToHtmlStringRGB(FleetAccentColor);
            string name = GetWorkerEffectName(effect, ru);
            string time = FormatWorkerEffectTime(effect.RemainingHours, ru);
            rowText.color = Color.white;
            rowText.text = $"<color=#{accent}>{name}</color>  {time}";
            ConfigureWorkerEffectTooltip(rowText, effect);
        }
    }

    private void UpdateWorkerPerksUi(DriverAgent driver, bool ru)
    {
        if (driversScreenUi == null)
        {
            return;
        }

        if (driversScreenUi.DetailPerksTitleText != null)
        {
            driversScreenUi.DetailPerksTitleText.text = ru ? "\u041f\u0435\u0440\u043a\u0438" : "Perks";
        }

        if (driver == null || driver.Perks.Count == 0)
        {
            if (driversScreenUi.DetailPerksEmptyText != null)
            {
                driversScreenUi.DetailPerksEmptyText.gameObject.SetActive(true);
                driversScreenUi.DetailPerksEmptyText.text = ru ? "\u041d\u0435\u0442 \u043f\u0435\u0440\u043a\u043e\u0432" : "No perks";
            }

            for (int i = 0; i < driversScreenUi.DetailPerkTexts.Count; i++)
            {
                driversScreenUi.DetailPerkTexts[i].gameObject.SetActive(false);
            }
            return;
        }

        if (driversScreenUi.DetailPerksEmptyText != null)
        {
            driversScreenUi.DetailPerksEmptyText.gameObject.SetActive(false);
        }

        int rowCount = driversScreenUi.DetailPerkTexts.Count;
        for (int i = 0; i < rowCount; i++)
        {
            Text rowText = driversScreenUi.DetailPerkTexts[i];
            if (i >= driver.Perks.Count)
            {
                rowText.gameObject.SetActive(false);
                continue;
            }

            WorkerPerkKind perk = driver.Perks[i];
            rowText.gameObject.SetActive(true);
            rowText.color = Color.white;
            rowText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(FleetAccentColor)}>{GetWorkerPerkDisplayName(perk, ru)}</color>";
            ConfigureWorkerPerkTooltip(rowText, perk);
        }
    }

    private static bool HasWorkerPerk(DriverAgent driver, WorkerPerkKind perk)
    {
        return driver != null && driver.Perks.Contains(perk);
    }

    private static bool HasWorkerEffect(DriverAgent driver, string effectId)
    {
        if (driver == null || string.IsNullOrWhiteSpace(effectId) || driver.ActiveEffects.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < driver.ActiveEffects.Count; i++)
        {
            if (driver.ActiveEffects[i]?.EffectId == effectId)
            {
                return true;
            }
        }

        return false;
    }

    private static string GetWorkerPerkDisplayName(WorkerPerkKind perk, bool ru)
    {
        return perk switch
        {
            WorkerPerkKind.Alcoholism   => ru ? "\u0410\u043b\u043a\u043e\u0433\u043e\u043b\u0438\u0437\u043c" : "Alcoholism",
            WorkerPerkKind.Gambler      => ru ? "\u041b\u0443\u0434\u043e\u043c\u0430\u043d" : "Gambler",
            WorkerPerkKind.Nightowl     => ru ? "\u0421\u043e\u0432\u0430" : "Night Owl",
            WorkerPerkKind.Ironman      => ru ? "\u0416\u0435\u043b\u0435\u0437\u043d\u044b\u0439" : "Iron Man",
            WorkerPerkKind.Motorhead    => ru ? "\u0410\u0432\u0442\u043e\u043b\u044e\u0431\u0438\u0442\u0435\u043b\u044c" : "Motorhead",
            WorkerPerkKind.Trader       => ru ? "\u0414\u0435\u043b\u0435\u0446" : "Trader",
            WorkerPerkKind.Handyman     => ru ? "\u041c\u0430\u0441\u0442\u0435\u0440" : "Handyman",
            WorkerPerkKind.Socialite    => ru ? "\u041e\u0431\u0449\u0438\u0442\u0435\u043b\u044c\u043d\u044b\u0439" : "Socialite",
            WorkerPerkKind.Frugal       => ru ? "\u042d\u043a\u043e\u043d\u043e\u043c\u043d\u044b\u0439" : "Frugal",
            WorkerPerkKind.Quicklearner => ru ? "\u0421\u043c\u044b\u0448\u043b\u0451\u043d\u044b\u0439" : "Quick Learner",
            _ => ru ? "\u041f\u0435\u0440\u043a" : "Perk"
        };
    }

    private static string GetWorkerPerkName(WorkerPerkKind perk, bool ru)
    {
        return perk switch
        {
            WorkerPerkKind.Alcoholism   => ru ? "Алкоголизм" : "Alcoholism",
            WorkerPerkKind.Gambler      => ru ? "Лудоман" : "Gambler",
            WorkerPerkKind.Nightowl     => ru ? "Сова" : "Night Owl",
            WorkerPerkKind.Ironman      => ru ? "Железный" : "Iron Man",
            WorkerPerkKind.Motorhead    => ru ? "Автолюбитель" : "Motorhead",
            WorkerPerkKind.Trader       => ru ? "Делец" : "Trader",
            WorkerPerkKind.Handyman     => ru ? "Мастер" : "Handyman",
            WorkerPerkKind.Socialite    => ru ? "Общительный" : "Socialite",
            WorkerPerkKind.Frugal       => ru ? "Экономный" : "Frugal",
            WorkerPerkKind.Quicklearner => ru ? "Смышлёный" : "Quick Learner",
            _ => ru ? "Перк" : "Perk"
        };
    }

    private static WorkerPerkType GetWorkerPerkType(WorkerPerkKind perk)
    {
        return perk switch
        {
            WorkerPerkKind.Alcoholism => WorkerPerkType.Negative,
            WorkerPerkKind.Gambler    => WorkerPerkType.Negative,
            _ => WorkerPerkType.Positive
        };
    }
    // All new perks are Positive by default via the wildcard above.

    private static string GetWorkerPerkTypeLabel(WorkerPerkType type, bool ru)
    {
        return type switch
        {
            WorkerPerkType.Negative => ru ? "\u041d\u0435\u0433\u0430\u0442\u0438\u0432\u043d\u044b\u0439" : "Negative",
            WorkerPerkType.Positive => ru ? "\u041f\u043e\u0437\u0438\u0442\u0438\u0432\u043d\u044b\u0439" : "Positive",
            _ => ru ? "\u0422\u0438\u043f" : "Type"
        };
    }

    private static Color GetWorkerPerkTypeColor(WorkerPerkType type)
    {
        return type switch
        {
            WorkerPerkType.Negative => new Color(0.96f, 0.46f, 0.38f, 1f),
            WorkerPerkType.Positive => new Color(0.55f, 0.88f, 0.44f, 1f),
            _ => FleetSecondaryTextColor
        };
    }

    private static string GetWorkerPerkDescription(WorkerPerkKind perk, bool ru)
    {
        return perk switch
        {
            WorkerPerkKind.Alcoholism => ru
                ? "\u041f\u043e\u0441\u043b\u0435 \u0411\u0430\u0440\u0430 \u043e\u043f\u044c\u044f\u043d\u0435\u043d\u0438\u0435 \u0434\u043b\u0438\u0442\u0441\u044f \u0434\u043e\u043b\u044c\u0448\u0435 \u0438 \u0434\u0430\u0451\u0442 \u0431\u043e\u043b\u044c\u0448\u0435 \u0431\u0430\u0444\u043e\u0432 \u043a \u043e\u0431\u044b\u0447\u043d\u043e\u0439 \u0440\u0430\u0431\u043e\u0442\u0435. \u041f\u043e\u0441\u043b\u0435 \u0441\u043f\u0430\u0434\u0430 \u043d\u0430\u0441\u0442\u0443\u043f\u0430\u0435\u0442 \u0436\u0451\u0441\u0442\u043a\u043e\u0435 \u043f\u043e\u0445\u043c\u0435\u043b\u044c\u0435 \u0434\u043e \u0441\u043b\u0435\u0434\u0443\u044e\u0449\u0435\u0433\u043e \u043f\u043e\u0445\u043e\u0434\u0430 \u0432 \u0411\u0430\u0440. \u0414\u043b\u044f \u0434\u043e\u0441\u0443\u0433\u0430 \u0442\u0430\u043a\u043e\u0439 \u0440\u0430\u0431\u043e\u0442\u043d\u0438\u043a \u0432\u0441\u0435\u0433\u0434\u0430 \u0442\u044f\u043d\u0435\u0442\u0441\u044f \u0438\u043c\u0435\u043d\u043d\u043e \u0432 \u0411\u0430\u0440."
                : "After visiting the Bar, Drunk lasts longer and gives stronger boosts to ordinary work. When it wears off, a harsh Hangover remains until the next Bar visit. For leisure, this worker always prefers the Bar.",
            WorkerPerkKind.Gambler => ru
                ? "Всегда выбирает Игровые автоматы. Ставит весь баланс, делает 2 ставки за визит. Больший выигрыш (x12/x6), меньший проигрыш (20% возвращается). После проигрыша следующая ставка выше."
                : "Always picks Gambling Hall. Bets full balance, makes 2 bets per visit. Higher jackpot (x12/x6), smaller losses (20% returned). Doubles down after a loss.",
            WorkerPerkKind.Nightowl => ru
                ? "[Заглушка] Ночная Сова. Работает эффективнее в ночные смены — бонус к вождению и производству в тёмное время суток."
                : "[Stub] Night Owl. Works better during night shifts — driving and production bonus in the dark hours.",
            WorkerPerkKind.Ironman => ru
                ? "[Заглушка] Железный. Медленнее накапливает усталость и реже нуждается во сне. Нужды снижаются медленнее."
                : "[Stub] Iron Man. Accumulates fatigue slower and needs less sleep. Need timers tick slower.",
            WorkerPerkKind.Motorhead => ru
                ? "[Заглушка] Автолюбитель. Бонус к навыку вождения. В будущем — снижение расхода топлива и ускоренное обслуживание грузовика."
                : "[Stub] Motorhead. Driving skill bonus. Future: reduced fuel consumption and faster truck maintenance.",
            WorkerPerkKind.Trader => ru
                ? "[Заглушка] Делец. Торговые рейсы приносят больше прибыли. В будущем — бонус к логистике и снижение закупочной цены."
                : "[Stub] Trader. Trade runs yield more profit. Future: Logistics bonus and lower purchase prices.",
            WorkerPerkKind.Handyman => ru
                ? "[Заглушка] Мастер. Ускоряет производство на всех зданиях. Бонус к производству и сокращение времени простоя."
                : "[Stub] Handyman. Speeds up production at all buildings. Production bonus and reduced downtime.",
            WorkerPerkKind.Socialite => ru
                ? "[Заглушка] Общительный. Восстанавливает потребность в досуге быстрее. Бар и другие заведения дают больший эффект."
                : "[Stub] Socialite. Leisure need recovers faster. Bar and service buildings give stronger effects.",
            WorkerPerkKind.Frugal => ru
                ? "[Заглушка] Экономный. Меньше тратит на сервисные здания и бытовые нужды. Скидка на все услуги."
                : "[Stub] Frugal. Spends less at service buildings. Discount on all service fees.",
            WorkerPerkKind.Quicklearner => ru
                ? "[Заглушка] Смышлёный. Быстрее прокачивает навыки через опыт. В будущем — ускоренный рост всех характеристик."
                : "[Stub] Quick Learner. Gains skills faster through experience. Future: accelerated stat growth.",
            _ => ru ? "Постоянная черта рабочего." : "A permanent worker trait."
        };
    }

    private static string GetWorkerEffectName(WorkerEffectState effect, bool ru)
    {
        if (effect == null)
        {
            return ru ? "\u042d\u0444\u0444\u0435\u043a\u0442" : "Effect";
        }

        string localized = ru ? effect.RussianName : effect.EnglishName;
        if (!string.IsNullOrWhiteSpace(localized))
        {
            return localized;
        }

        return !string.IsNullOrWhiteSpace(effect.EffectId)
            ? effect.EffectId
            : ru ? "\u042d\u0444\u0444\u0435\u043a\u0442" : "Effect";
    }

    private static string FormatWorkerEffectTime(float remainingHours, bool ru)
    {
        if (float.IsInfinity(remainingHours))
        {
            return "\u221e";
        }

        int minutes = Mathf.Max(1, Mathf.CeilToInt(remainingHours * 60f));
        if (minutes >= 60)
        {
            int hours = Mathf.CeilToInt(minutes / 60f);
            return ru ? $"{hours}\u0447" : $"{hours}h";
        }

        return ru ? $"{minutes}\u043c" : $"{minutes}m";
    }

    private static string FormatWorkerEffectModifiers(WorkerEffectState effect, bool ru)
    {
        if (effect == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new();
        AppendWorkerEffectModifier(builder, ru ? "\u0412\u043e\u0436\u0434." : "Drv", effect.DrivingDelta);
        AppendWorkerEffectModifier(builder, ru ? "\u0412\u044b\u043d." : "Sta", effect.StaminaDelta);
        AppendWorkerEffectModifier(builder, ru ? "\u041f\u0440\u043e\u0438\u0437." : "Prod", effect.ProductionDelta);
        AppendWorkerEffectModifier(builder, ru ? "\u041b\u043e\u0433." : "Log", effect.LogisticsDelta);
        return builder.ToString();
    }

    private static string GetWorkerEffectDescription(WorkerEffectState effect, bool ru)
    {
        if (effect == null)
        {
            return string.Empty;
        }

        string localized = ru ? effect.RussianDescription : effect.EnglishDescription;
        return localized ?? string.Empty;
    }

    private static void AppendWorkerEffectModifier(StringBuilder builder, string label, int delta)
    {
        if (builder == null || delta == 0)
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.Append(", ");
        }

        builder.Append(label).Append(' ');
        if (delta > 0)
        {
            builder.Append('+');
        }

        builder.Append(delta);
    }

    private void SetupWorkerSkillTooltip(RectTransform parent, Font font)
    {
        if (driversScreenUi == null || parent == null || font == null) return;

        RectTransform tooltip = CreateStyledPanel("WorkerSkillTooltip", parent, new Color(0.04f, 0.06f, 0.09f, 0.98f));
        tooltip.anchorMin = new Vector2(0.5f, 0.5f);
        tooltip.anchorMax = new Vector2(0.5f, 0.5f);
        tooltip.pivot = new Vector2(0f, 1f);
        tooltip.sizeDelta = new Vector2(WorkerSkillTooltipWidth, WorkerSkillTooltipHeight);
        tooltip.anchoredPosition = Vector2.zero;
        tooltip.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        if (tooltip.TryGetComponent(out Image tooltipImage))
        {
            tooltipImage.raycastTarget = false;
        }

        VerticalLayoutGroup layout = tooltip.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 10);
        layout.spacing = 5f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        driversScreenUi.DetailSkillTooltipTitleText = CreateHeaderText("WorkerSkillTooltipTitle", tooltip, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailSkillTooltipTitleText.raycastTarget = false;
        driversScreenUi.DetailSkillTooltipTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        driversScreenUi.DetailSkillTooltipBodyText = CreateBodyText("WorkerSkillTooltipBody", tooltip, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        driversScreenUi.DetailSkillTooltipBodyText.raycastTarget = false;
        driversScreenUi.DetailSkillTooltipBodyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;

        driversScreenUi.DetailSkillTooltipRoot = tooltip.gameObject;
        driversScreenUi.DetailSkillTooltipRoot.SetActive(false);
    }

    private void ConfigureWorkerSkillTooltip(Text text, WorkerSkillKind skill)
    {
        if (text == null) return;

        text.raycastTarget = true;
        EventTrigger trigger = text.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowWorkerSkillTooltip(skill, text.rectTransform));
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new() { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideWorkerSkillTooltip());
        trigger.triggers.Add(exit);
    }

    private void ConfigureWorkerEffectTooltip(Text text, WorkerEffectState effect)
    {
        if (text == null || effect == null) return;

        text.raycastTarget = true;
        EventTrigger trigger = text.TryGetComponent(out EventTrigger existingTrigger)
            ? existingTrigger
            : text.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();
        WorkerEffectState capturedEffect = effect;

        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowWorkerEffectTooltip(capturedEffect, text.rectTransform));
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new() { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideWorkerSkillTooltip());
        trigger.triggers.Add(exit);
    }

    private void ConfigureWorkerPerkTooltip(Text text, WorkerPerkKind perk)
    {
        if (text == null) return;

        text.raycastTarget = true;
        EventTrigger trigger = text.TryGetComponent(out EventTrigger existingTrigger)
            ? existingTrigger
            : text.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowWorkerPerkTooltip(perk, text.rectTransform));
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new() { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideWorkerSkillTooltip());
        trigger.triggers.Add(exit);
    }

    private void ShowWorkerSkillTooltip(WorkerSkillKind skill, RectTransform target)
    {
        if (driversScreenUi?.DetailSkillTooltipRoot == null || target == null || driversScreenUi.WindowRoot == null) return;

        bool ru = IsRussianLanguage();
        if (driversScreenUi.DetailSkillTooltipTitleText != null)
        {
            driversScreenUi.DetailSkillTooltipTitleText.text = GetWorkerSkillLabel(skill, ru);
        }

        if (driversScreenUi.DetailSkillTooltipBodyText != null)
        {
            driversScreenUi.DetailSkillTooltipBodyText.text = GetWorkerSkillDescription(skill, ru);
            SetWorkerTooltipBodyHeight(42f);
        }

        SetWorkerTooltipSize(WorkerSkillTooltipWidth, WorkerSkillTooltipHeight);
        PositionWorkerTooltip(target, WorkerSkillTooltipWidth, WorkerSkillTooltipHeight);

        driversScreenUi.DetailSkillTooltipRoot.SetActive(true);
        driversScreenUi.DetailSkillTooltipRoot.transform.SetAsLastSibling();
    }

    private void ShowWorkerEffectTooltip(WorkerEffectState effect, RectTransform target)
    {
        if (driversScreenUi?.DetailSkillTooltipRoot == null || target == null || driversScreenUi.WindowRoot == null) return;

        if (effect == null)
        {
            HideWorkerSkillTooltip();
            return;
        }

        bool ru = IsRussianLanguage();
        if (driversScreenUi.DetailSkillTooltipTitleText != null)
        {
            driversScreenUi.DetailSkillTooltipTitleText.text = GetWorkerEffectName(effect, ru);
        }

        if (driversScreenUi.DetailSkillTooltipBodyText != null)
        {
            driversScreenUi.DetailSkillTooltipBodyText.text = FormatWorkerEffectTooltip(effect, ru);
            SetWorkerTooltipBodyHeight(176f);
        }

        SetWorkerTooltipSize(WorkerSkillTooltipWidth, WorkerEffectTooltipHeight);
        PositionWorkerTooltip(target, WorkerSkillTooltipWidth, WorkerEffectTooltipHeight);
        driversScreenUi.DetailSkillTooltipRoot.SetActive(true);
        driversScreenUi.DetailSkillTooltipRoot.transform.SetAsLastSibling();
    }

    private void ShowWorkerPerkTooltip(WorkerPerkKind perk, RectTransform target)
    {
        if (driversScreenUi?.DetailSkillTooltipRoot == null || target == null || driversScreenUi.WindowRoot == null) return;

        bool ru = IsRussianLanguage();
        if (driversScreenUi.DetailSkillTooltipTitleText != null)
        {
            driversScreenUi.DetailSkillTooltipTitleText.text = GetWorkerPerkDisplayName(perk, ru);
        }

        if (driversScreenUi.DetailSkillTooltipBodyText != null)
        {
            driversScreenUi.DetailSkillTooltipBodyText.text = GetWorkerPerkDescription(perk, ru);
            SetWorkerTooltipBodyHeight(82f);
        }

        SetWorkerTooltipSize(WorkerSkillTooltipWidth, 126f);
        PositionWorkerTooltip(target, WorkerSkillTooltipWidth, 126f);
        driversScreenUi.DetailSkillTooltipRoot.SetActive(true);
        driversScreenUi.DetailSkillTooltipRoot.transform.SetAsLastSibling();
    }

    private void SetWorkerTooltipSize(float width, float height)
    {
        if (driversScreenUi?.DetailSkillTooltipRoot == null) return;
        RectTransform tooltipRect = driversScreenUi.DetailSkillTooltipRoot.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(width, height);
    }

    private void SetWorkerTooltipBodyHeight(float height)
    {
        if (driversScreenUi?.DetailSkillTooltipBodyText == null) return;
        if (driversScreenUi.DetailSkillTooltipBodyText.TryGetComponent(out LayoutElement layout))
        {
            layout.preferredHeight = height;
        }
    }

    private void PositionWorkerTooltip(RectTransform target, float width, float height)
    {
        RectTransform tooltipRect = driversScreenUi.DetailSkillTooltipRoot.GetComponent<RectTransform>();
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(null, corners[2]);
        screenPoint += new Vector2(14f, 8f);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(driversScreenUi.WindowRoot, screenPoint, null, out Vector2 localPoint))
        {
            Rect windowRect = driversScreenUi.WindowRoot.rect;
            float x = Mathf.Clamp(
                localPoint.x,
                -windowRect.width * 0.5f + 12f,
                windowRect.width * 0.5f - width - 12f);
            float y = Mathf.Clamp(
                localPoint.y,
                -windowRect.height * 0.5f + height + 12f,
                windowRect.height * 0.5f - 12f);
            tooltipRect.anchoredPosition = new Vector2(x, y);
        }
    }

    private void HideWorkerSkillTooltip()
    {
        if (driversScreenUi?.DetailSkillTooltipRoot == null) return;
        driversScreenUi.DetailSkillTooltipRoot.SetActive(false);
    }

    private static string FormatWorkerEffectTooltip(WorkerEffectState effect, bool ru)
    {
        if (effect == null)
        {
            return ru ? "\u041d\u0435\u0442 \u0430\u043a\u0442\u0438\u0432\u043d\u044b\u0445 \u044d\u0444\u0444\u0435\u043a\u0442\u043e\u0432." : "No active effects.";
        }

        StringBuilder builder = new();
        builder.Append(ru ? "\u041e\u0441\u0442\u0430\u043b\u043e\u0441\u044c: " : "Remaining: ");
        builder.Append(FormatWorkerEffectTime(effect.RemainingHours, ru));

        string modifiers = FormatWorkerEffectModifiers(effect, ru);
        if (!string.IsNullOrEmpty(modifiers))
        {
            builder.Append('\n').Append(modifiers);
        }

        string description = GetWorkerEffectDescription(effect, ru);
        if (!string.IsNullOrWhiteSpace(description))
        {
            builder.Append('\n').Append(description);
        }

        return builder.ToString();
    }

    private static void SetWorkerSkillText(Text text, string label, int baseValue, int delta)
    {
        if (text == null) return;
        int clampedBaseValue = ClampWorkerSkill(baseValue);
        int clampedValue = ClampWorkerSkill(baseValue + delta);
        Color valueColor = GetWorkerSkillColor(clampedValue);
        string labelColor = ColorUtility.ToHtmlStringRGB(FleetAccentColor);
        string statColor = ColorUtility.ToHtmlStringRGB(valueColor);
        string deltaText = delta == 0 ? string.Empty : $" ({clampedBaseValue}{(delta > 0 ? "+" : string.Empty)}{delta})";
        text.text = $"<color=#{labelColor}>{label}</color>  <color=#{statColor}>{clampedValue}/{WorkerSkillMax}</color>{deltaText}";
    }

    private static int GetWorkerSkillDelta(DriverAgent driver, WorkerSkillKind skill)
    {
        if (driver == null || driver.ActiveEffects.Count == 0)
        {
            return 0;
        }

        int delta = 0;
        for (int i = 0; i < driver.ActiveEffects.Count; i++)
        {
            WorkerEffectState effect = driver.ActiveEffects[i];
            if (effect == null)
            {
                continue;
            }

            delta += skill switch
            {
                WorkerSkillKind.Driving => effect.DrivingDelta,
                WorkerSkillKind.Stamina => effect.StaminaDelta,
                WorkerSkillKind.Production => effect.ProductionDelta,
                WorkerSkillKind.Logistics => effect.LogisticsDelta,
                _ => 0
            };
        }

        return delta;
    }

    private static int GetWorkerEffectiveSkill(DriverAgent driver, WorkerSkillKind skill)
    {
        if (driver == null)
        {
            return WorkerSkillMin;
        }

        int baseValue = skill switch
        {
            WorkerSkillKind.Driving => driver.DrivingSkill,
            WorkerSkillKind.Stamina => driver.StaminaSkill,
            WorkerSkillKind.Production => driver.ProductionSkill,
            WorkerSkillKind.Logistics => driver.LogisticsSkill,
            _ => WorkerSkillMin
        };

        return ClampWorkerSkill(baseValue + GetWorkerSkillDelta(driver, skill));
    }

    private void SyncWorkerNeedEffects(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        SyncWorkerMealNeedEffect(driver);
        SyncWorkerSleepNeedEffect(driver);
        SyncWorkerLeisureNeedEffect(driver);
    }

    private void SyncWorkerMealNeedEffect(DriverAgent driver)
    {
        if (driver.LastMealNeedStatus == WorkerNeedStatus.Critical)
        {
            RemoveWorkerEffect(driver, WorkerHungryEffectId, false);
            AddOrRefreshWorkerEffect(
                driver,
                WorkerStarvingEffectId,
                "Starving",
                "\u0421\u0438\u043b\u044c\u043d\u044b\u0439 \u0433\u043e\u043b\u043e\u0434",
                "Food has been ignored too long. Work collapses; driving starts to suffer.",
                "\u0415\u0434\u0443 \u0438\u0433\u043d\u043e\u0440\u0438\u0440\u043e\u0432\u0430\u043b\u0438 \u0441\u043b\u0438\u0448\u043a\u043e\u043c \u0434\u043e\u043b\u0433\u043e. \u0420\u0430\u0431\u043e\u0442\u0430 \u0441\u044b\u043f\u043b\u0435\u0442\u0441\u044f, \u0432\u043e\u0436\u0434\u0435\u043d\u0438\u0435 \u0442\u043e\u0436\u0435 \u043f\u0440\u043e\u0441\u0435\u0434\u0430\u0435\u0442.",
                WorkerNeedEffectRefreshHours,
                drivingDelta: -1,
                staminaDelta: -4,
                productionDelta: -2,
                logisticsDelta: -2);
            return;
        }

        if (driver.LastMealNeedStatus == WorkerNeedStatus.Warning)
        {
            RemoveWorkerEffect(driver, WorkerStarvingEffectId, false);
            AddOrRefreshWorkerEffect(
                driver,
                WorkerHungryEffectId,
                "Hungry",
                "\u0413\u043e\u043b\u043e\u0434",
                "An empty stomach weakens stamina and physical work.",
                "\u041f\u0443\u0441\u0442\u043e\u0439 \u0436\u0435\u043b\u0443\u0434\u043e\u043a \u0431\u044c\u0435\u0442 \u043f\u043e \u0432\u044b\u043d\u043e\u0441\u043b\u0438\u0432\u043e\u0441\u0442\u0438 \u0438 \u0444\u0438\u0437\u0438\u0447\u0435\u0441\u043a\u043e\u0439 \u0440\u0430\u0431\u043e\u0442\u0435.",
                WorkerNeedEffectRefreshHours,
                staminaDelta: -2,
                productionDelta: -1,
                logisticsDelta: -1);
            return;
        }

        RemoveWorkerEffect(driver, WorkerHungryEffectId, false);
        RemoveWorkerEffect(driver, WorkerStarvingEffectId, false);
    }

    private void SyncWorkerSleepNeedEffect(DriverAgent driver)
    {
        if (driver.LastSleepNeedStatus == WorkerNeedStatus.Critical)
        {
            RemoveWorkerEffect(driver, WorkerSleepDeprivedEffectId, false);
            AddOrRefreshWorkerEffect(
                driver,
                WorkerExhaustedEffectId,
                "Exhausted",
                "\u0418\u0437\u043c\u043e\u0442\u0430\u043d",
                "No sleep left in the bones. Everything gets worse.",
                "\u0421\u043d\u0430 \u0432 \u043a\u043e\u0441\u0442\u044f\u0445 \u043d\u0435 \u043e\u0441\u0442\u0430\u043b\u043e\u0441\u044c. \u0425\u0443\u0436\u0435 \u0441\u0442\u0430\u043d\u043e\u0432\u0438\u0442\u0441\u044f \u0432\u0441\u0435.",
                WorkerNeedEffectRefreshHours,
                drivingDelta: -4,
                staminaDelta: -4,
                productionDelta: -2,
                logisticsDelta: -2);
            return;
        }

        if (driver.LastSleepNeedStatus == WorkerNeedStatus.Warning)
        {
            RemoveWorkerEffect(driver, WorkerExhaustedEffectId, false);
            AddOrRefreshWorkerEffect(
                driver,
                WorkerSleepDeprivedEffectId,
                "Sleep Deprived",
                "\u041d\u0435\u0434\u043e\u0441\u044b\u043f",
                "Bad sleep hits driving first, then the rest of the day.",
                "\u041f\u043b\u043e\u0445\u043e\u0439 \u0441\u043e\u043d \u0441\u043d\u0430\u0447\u0430\u043b\u0430 \u0431\u044c\u0435\u0442 \u043f\u043e \u0432\u043e\u0436\u0434\u0435\u043d\u0438\u044e, \u043f\u043e\u0442\u043e\u043c \u043f\u043e \u0432\u0441\u0435\u043c\u0443 \u0434\u043d\u044e.",
                WorkerNeedEffectRefreshHours,
                drivingDelta: -2,
                staminaDelta: -2,
                productionDelta: -1,
                logisticsDelta: -1);
            return;
        }

        RemoveWorkerEffect(driver, WorkerSleepDeprivedEffectId, false);
        RemoveWorkerEffect(driver, WorkerExhaustedEffectId, false);
    }

    private void SyncWorkerLeisureNeedEffect(DriverAgent driver)
    {
        if (driver.LastLeisureNeedStatus == WorkerNeedStatus.Critical)
        {
            RemoveWorkerEffect(driver, WorkerBoredEffectId, false);
            AddOrRefreshWorkerEffect(
                driver,
                WorkerBurnedOutEffectId,
                "Burned Out",
                "\u0412\u044b\u0433\u043e\u0440\u0430\u043d\u0438\u0435",
                "No leisure, no spark. Almost every task becomes heavier.",
                "\u041d\u0435\u0442 \u0434\u043e\u0441\u0443\u0433\u0430 - \u043d\u0435\u0442 \u0438\u0441\u043a\u0440\u044b. \u041f\u043e\u0447\u0442\u0438 \u043b\u044e\u0431\u043e\u0435 \u0434\u0435\u043b\u043e \u0441\u0442\u0430\u043d\u043e\u0432\u0438\u0442\u0441\u044f \u0442\u044f\u0436\u0435\u043b\u0435\u0435.",
                WorkerNeedEffectRefreshHours,
                drivingDelta: -1,
                staminaDelta: -1,
                productionDelta: -2,
                logisticsDelta: -2);
            return;
        }

        if (driver.LastLeisureNeedStatus == WorkerNeedStatus.Warning)
        {
            RemoveWorkerEffect(driver, WorkerBurnedOutEffectId, false);
            AddOrRefreshWorkerEffect(
                driver,
                WorkerBoredEffectId,
                "Bored",
                "\u0421\u043a\u0443\u043a\u0430",
                "The worker is present, but the spark stayed somewhere else.",
                "\u0420\u0430\u0431\u043e\u0447\u0438\u0439 \u043d\u0430 \u043c\u0435\u0441\u0442\u0435, \u043d\u043e \u0438\u0441\u043a\u0440\u0430 \u0433\u0434\u0435-\u0442\u043e \u043e\u0442\u0441\u0442\u0430\u043b\u0430.",
                WorkerNeedEffectRefreshHours,
                productionDelta: -1,
                logisticsDelta: -1);
            return;
        }

        RemoveWorkerEffect(driver, WorkerBoredEffectId, false);
        RemoveWorkerEffect(driver, WorkerBurnedOutEffectId, false);
    }

    private static string GetWorkerSkillLabel(WorkerSkillKind skill, bool ru)
    {
        return skill switch
        {
            WorkerSkillKind.Driving    => ru ? "\u0412\u043e\u0436\u0434\u0435\u043d\u0438\u0435" : "Driving",
            WorkerSkillKind.Stamina    => ru ? "\u0412\u044b\u043d\u043e\u0441\u043b\u0438\u0432\u043e\u0441\u0442\u044c" : "Stamina",
            WorkerSkillKind.Production => ru ? "\u041f\u0440\u043e\u0438\u0437\u0432\u043e\u0434\u0441\u0442\u0432\u043e" : "Production",
            WorkerSkillKind.Logistics  => ru ? "\u041b\u043e\u0433\u0438\u0441\u0442\u0438\u043a\u0430" : "Logistics",
            _                          => ru ? "\u041d\u0430\u0432\u044b\u043a" : "Skill"
        };
    }

    private static string GetWorkerSkillDescription(WorkerSkillKind skill, bool ru)
    {
        return skill switch
        {
            WorkerSkillKind.Driving => ru
                ? "\u0412\u043b\u0438\u044f\u0435\u0442 \u043d\u0430 \u0440\u0435\u0439\u0441\u044b \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0430, \u0432\u043e\u0436\u0434\u0435\u043d\u0438\u0435 \u043d\u0430 \u043c\u0430\u0440\u0448\u0440\u0443\u0442\u0430\u0445 \u0438 \u0431\u0443\u0434\u0443\u0449\u0435\u0435 \u0443\u043f\u0440\u0430\u0432\u043b\u0435\u043d\u0438\u0435 \u0432 \u0433\u043e\u043d\u043a\u0430\u0445."
                : "Affects truck trips, route driving, and future race handling.",
            WorkerSkillKind.Stamina => ru
                ? "\u0412\u043b\u0438\u044f\u0435\u0442 \u043d\u0430 \u0443\u0441\u0442\u043e\u0439\u0447\u0438\u0432\u043e\u0441\u0442\u044c \u043a \u0443\u0441\u0442\u0430\u043b\u043e\u0441\u0442\u0438, \u0432\u043e\u0441\u0441\u0442\u0430\u043d\u043e\u0432\u043b\u0435\u043d\u0438\u0435 \u0438 \u0431\u0443\u0434\u0443\u0449\u0438\u0435 \u0434\u0435\u0431\u0430\u0444\u0444\u044b \u043f\u043e\u0442\u0440\u0435\u0431\u043d\u043e\u0441\u0442\u0435\u0439."
                : "Affects tiredness tolerance, recovery pacing, and future need debuffs.",
            WorkerSkillKind.Production => ru
                ? "\u0412\u043b\u0438\u044f\u0435\u0442 \u043d\u0430 \u0440\u0430\u0431\u043e\u0442\u0443 \u0432 \u041b\u0435\u0441\u0443, \u041b\u0435\u0441\u043e\u043f\u0438\u043b\u043a\u0435 \u0438 \u041c\u0435\u0431\u0435\u043b\u044c\u043d\u043e\u0439 \u0444\u0430\u0431\u0440\u0438\u043a\u0435."
                : "Affects work at Forest, Sawmill, and Furniture Factory.",
            WorkerSkillKind.Logistics => ru
                ? "\u0412\u043b\u0438\u044f\u0435\u0442 \u043d\u0430 \u0441\u043a\u043b\u0430\u0434, \u043f\u0435\u0440\u0435\u043d\u043e\u0441\u043a\u0443, \u043f\u043e\u0433\u0440\u0443\u0437\u043a\u0443, \u0440\u0430\u0437\u0433\u0440\u0443\u0437\u043a\u0443 \u0438 \u0431\u0443\u0434\u0443\u0449\u0438\u0435 trade-\u0440\u0435\u0439\u0441\u044b."
                : "Affects Warehouse work, carrying, loading, unloading, and future trade runs.",
            _ => string.Empty
        };
    }

    private static int RollWorkerSkill(System.Random rng)
    {
        if (rng == null) return 5;

        // Two rolls make most workers average while still allowing clear outliers.
        int rollA = rng.Next(WorkerSkillMin, WorkerSkillMax + 1);
        int rollB = rng.Next(WorkerSkillMin, WorkerSkillMax + 1);
        return ClampWorkerSkill((rollA + rollB + 1) / 2);
    }

    private static int ClampWorkerSkill(int value)
    {
        return Math.Max(WorkerSkillMin, Math.Min(WorkerSkillMax, value));
    }

    private static Color GetWorkerSkillColor(int value)
    {
        if (value >= 8) return new Color(0.62f, 0.92f, 0.62f, 1f);
        if (value >= 5) return Color.white;
        return new Color(0.96f, 0.72f, 0.42f, 1f);
    }

    private static int StableWorkerStatsHash(string value)
    {
        unchecked
        {
            int hash = 29;
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 37 + value[i];
                }
            }

            return hash;
        }
    }
}

