using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int WorkerSkillMin = 1;
    private const int WorkerSkillMax = 10;
    private const float WorkerSkillTooltipWidth = 330f;
    private const float WorkerSkillTooltipHeight = 86f;

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

    private void EnsureWorkerStats(DriverAgent driver)
    {
        if (driver == null || driver.HasWorkerStats) return;
        AssignWorkerStats(driver);
    }

    private void UpdateWorkerStatsUi(DriverAgent driver, bool ru)
    {
        if (driversScreenUi == null) return;
        EnsureWorkerStats(driver);

        if (driversScreenUi.DetailSkillsTitleText != null)
        {
            driversScreenUi.DetailSkillsTitleText.text = ru ? "\u041d\u0430\u0432\u044b\u043a\u0438" : "Skills";
        }

        SetWorkerSkillText(driversScreenUi.DetailDrivingSkillText, GetWorkerSkillLabel(WorkerSkillKind.Driving, ru), driver?.DrivingSkill ?? 0);
        SetWorkerSkillText(driversScreenUi.DetailStaminaSkillText, GetWorkerSkillLabel(WorkerSkillKind.Stamina, ru), driver?.StaminaSkill ?? 0);
        SetWorkerSkillText(driversScreenUi.DetailProductionSkillText, GetWorkerSkillLabel(WorkerSkillKind.Production, ru), driver?.ProductionSkill ?? 0);
        SetWorkerSkillText(driversScreenUi.DetailLogisticsSkillText, GetWorkerSkillLabel(WorkerSkillKind.Logistics, ru), driver?.LogisticsSkill ?? 0);
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
        }

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
                windowRect.width * 0.5f - WorkerSkillTooltipWidth - 12f);
            float y = Mathf.Clamp(
                localPoint.y,
                -windowRect.height * 0.5f + WorkerSkillTooltipHeight + 12f,
                windowRect.height * 0.5f - 12f);
            tooltipRect.anchoredPosition = new Vector2(x, y);
        }

        driversScreenUi.DetailSkillTooltipRoot.SetActive(true);
        driversScreenUi.DetailSkillTooltipRoot.transform.SetAsLastSibling();
    }

    private void HideWorkerSkillTooltip()
    {
        if (driversScreenUi?.DetailSkillTooltipRoot == null) return;
        driversScreenUi.DetailSkillTooltipRoot.SetActive(false);
    }

    private static void SetWorkerSkillText(Text text, string label, int value)
    {
        if (text == null) return;
        int clampedValue = ClampWorkerSkill(value);
        Color valueColor = GetWorkerSkillColor(clampedValue);
        string labelColor = ColorUtility.ToHtmlStringRGB(FleetAccentColor);
        string statColor = ColorUtility.ToHtmlStringRGB(valueColor);
        text.text = $"<color=#{labelColor}>{label}</color>  <color=#{statColor}>{clampedValue}/{WorkerSkillMax}</color>";
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
