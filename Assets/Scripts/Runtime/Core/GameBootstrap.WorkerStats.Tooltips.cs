using System;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class GameBootstrap
{
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
