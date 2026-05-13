using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void SetupWorkerTraitTooltip(RectTransform parent, Font font)
    {
        if (driversScreenUi == null || parent == null || font == null)
        {
            return;
        }

        RectTransform tooltip = CreateStyledPanel("WorkerTraitTooltip", parent, new Color(0.04f, 0.06f, 0.09f, 0.98f));
        tooltip.anchorMin = new Vector2(0.5f, 0.5f);
        tooltip.anchorMax = new Vector2(0.5f, 0.5f);
        tooltip.pivot = new Vector2(0f, 1f);
        tooltip.sizeDelta = new Vector2(WorkerTraitTooltipWidth, WorkerTraitTooltipHeight);
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

        driversScreenUi.DetailTraitTooltipTitleText = CreateHeaderText("WorkerTraitTooltipTitle", tooltip, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailTraitTooltipTitleText.raycastTarget = false;
        driversScreenUi.DetailTraitTooltipTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        driversScreenUi.DetailTraitTooltipBodyText = CreateBodyText("WorkerTraitTooltipBody", tooltip, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        driversScreenUi.DetailTraitTooltipBodyText.raycastTarget = false;
        driversScreenUi.DetailTraitTooltipBodyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 88f;

        driversScreenUi.DetailTraitTooltipRoot = tooltip.gameObject;
        driversScreenUi.DetailTraitTooltipRoot.SetActive(false);
    }

    private void ConfigureWorkerTraitTooltip(Text text, WorkerTraitKind trait)
    {
        if (text == null)
        {
            return;
        }

        ConfigureWorkerPersonalityTooltip(
            text,
            () => ShowWorkerTraitTooltip(trait, text.rectTransform));
    }

    private void ConfigureWorkerWeaknessTooltip(Text text, WorkerWeaknessKind weakness)
    {
        if (text == null)
        {
            return;
        }

        ConfigureWorkerPersonalityTooltip(
            text,
            () => ShowWorkerWeaknessTooltip(weakness, text.rectTransform));
    }

    private void ConfigureWorkerRaceTooltip(Text text, WorkerRaceKind race)
    {
        if (text == null)
        {
            return;
        }

        ConfigureWorkerPersonalityTooltip(
            text,
            () => ShowWorkerRaceTooltip(race, text.rectTransform));
    }

    private void ConfigureWorkerHeritageTooltip(Text text, WorkerHeritageKind heritage)
    {
        if (text == null)
        {
            return;
        }

        ConfigureWorkerPersonalityTooltip(
            text,
            () => ShowWorkerHeritageTooltip(heritage, text.rectTransform));
    }

    private void ConfigureWorkerPersonalityTooltip(Text text, UnityEngine.Events.UnityAction onEnter)
    {
        text.raycastTarget = true;
        EventTrigger trigger = text.TryGetComponent(out EventTrigger existingTrigger)
            ? existingTrigger
            : text.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => onEnter());
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new() { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideWorkerTraitTooltip());
        trigger.triggers.Add(exit);
    }

    private static void ClearWorkerTraitTooltip(Text text)
    {
        if (text == null)
        {
            return;
        }

        text.raycastTarget = false;
        if (text.TryGetComponent(out EventTrigger trigger))
        {
            trigger.triggers.Clear();
        }
    }

    private void ShowWorkerTraitTooltip(WorkerTraitKind trait, RectTransform target)
    {
        ShowWorkerPersonalityTooltip(
            GetWorkerTraitDisplayName(trait, IsRussianLanguage()),
            GetWorkerTraitDescription(trait, IsRussianLanguage()),
            GetWorkerTraitColor(trait),
            target);
    }

    private void ShowWorkerWeaknessTooltip(WorkerWeaknessKind weakness, RectTransform target)
    {
        ShowWorkerPersonalityTooltip(
            GetWorkerWeaknessDisplayName(weakness, IsRussianLanguage()),
            GetWorkerWeaknessDescription(weakness, IsRussianLanguage()),
            GetWorkerWeaknessColor(weakness),
            target);
    }

    private void ShowWorkerRaceTooltip(WorkerRaceKind race, RectTransform target)
    {
        ShowWorkerPersonalityTooltip(
            $"{GetWorkerRaceIconGlyph(race)} {GetWorkerRaceDisplayName(race, IsRussianLanguage())}",
            GetWorkerRaceFullDescription(race, IsRussianLanguage()),
            GetWorkerRaceColor(race),
            target);
    }

    private void ShowWorkerHeritageTooltip(WorkerHeritageKind heritage, RectTransform target)
    {
        ShowWorkerPersonalityTooltip(
            $"{GetWorkerHeritageIconGlyph(heritage)} {GetWorkerHeritageDisplayName(heritage, IsRussianLanguage())}",
            GetWorkerHeritageDescription(heritage, IsRussianLanguage()),
            GetWorkerHeritageColor(heritage),
            target);
    }

    private void ShowWorkerPersonalityTooltip(string title, string body, Color color, RectTransform target)
    {
        if (driversScreenUi?.DetailTraitTooltipRoot == null || target == null || driversScreenUi.WindowRoot == null)
        {
            return;
        }

        if (driversScreenUi.DetailTraitTooltipTitleText != null)
        {
            driversScreenUi.DetailTraitTooltipTitleText.text = title;
            driversScreenUi.DetailTraitTooltipTitleText.color = color;
        }

        if (driversScreenUi.DetailTraitTooltipBodyText != null)
        {
            driversScreenUi.DetailTraitTooltipBodyText.text = body;
            SetWorkerTooltipBodyHeight(88f);
        }

        SetWorkerTooltipSize(WorkerTraitTooltipWidth, WorkerTraitTooltipHeight);
        PositionWorkerTooltip(target, WorkerTraitTooltipWidth, WorkerTraitTooltipHeight);
        driversScreenUi.DetailTraitTooltipRoot.SetActive(true);
        driversScreenUi.DetailTraitTooltipRoot.transform.SetAsLastSibling();
    }

    private void SetWorkerTooltipSize(float width, float height)
    {
        if (driversScreenUi?.DetailTraitTooltipRoot == null)
        {
            return;
        }

        RectTransform tooltipRect = driversScreenUi.DetailTraitTooltipRoot.GetComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(width, height);
    }

    private void SetWorkerTooltipBodyHeight(float height)
    {
        if (driversScreenUi?.DetailTraitTooltipBodyText == null)
        {
            return;
        }

        if (driversScreenUi.DetailTraitTooltipBodyText.TryGetComponent(out LayoutElement layout))
        {
            layout.preferredHeight = height;
        }
    }

    private void PositionWorkerTooltip(RectTransform target, float width, float height)
    {
        RectTransform tooltipRect = driversScreenUi.DetailTraitTooltipRoot.GetComponent<RectTransform>();
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

    private void HideWorkerTraitTooltip()
    {
        if (driversScreenUi?.DetailTraitTooltipRoot == null)
        {
            return;
        }

        driversScreenUi.DetailTraitTooltipRoot.SetActive(false);
    }
}
