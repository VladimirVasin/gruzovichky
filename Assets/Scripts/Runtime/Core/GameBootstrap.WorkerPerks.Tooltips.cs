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

    private void ConfigureWorkerPerkTooltip(Text text, WorkerPerkKind perk)
    {
        if (text == null)
        {
            return;
        }

        text.raycastTarget = true;
        EventTrigger trigger = text.TryGetComponent(out EventTrigger existingTrigger)
            ? existingTrigger
            : text.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowWorkerPerkTooltip(perk, text.rectTransform));
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new() { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideWorkerTraitTooltip());
        trigger.triggers.Add(exit);
    }

    private void ConfigureWorkerLeisurePreferenceTooltip(Text text, WorkerLeisurePreferenceKind preference)
    {
        if (text == null)
        {
            return;
        }

        text.raycastTarget = true;
        EventTrigger trigger = text.TryGetComponent(out EventTrigger existingTrigger)
            ? existingTrigger
            : text.gameObject.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowWorkerLeisurePreferenceTooltip(preference, text.rectTransform));
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

    private void ShowWorkerPerkTooltip(WorkerPerkKind perk, RectTransform target)
    {
        if (driversScreenUi?.DetailTraitTooltipRoot == null || target == null || driversScreenUi.WindowRoot == null)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        if (driversScreenUi.DetailTraitTooltipTitleText != null)
        {
            driversScreenUi.DetailTraitTooltipTitleText.text = GetWorkerPerkDisplayName(perk, ru);
            driversScreenUi.DetailTraitTooltipTitleText.color = GetWorkerPerkColor();
        }

        if (driversScreenUi.DetailTraitTooltipBodyText != null)
        {
            driversScreenUi.DetailTraitTooltipBodyText.text = GetWorkerPerkDescription(perk, ru);
            SetWorkerTooltipBodyHeight(88f);
        }

        SetWorkerTooltipSize(WorkerTraitTooltipWidth, WorkerTraitTooltipHeight);
        PositionWorkerTooltip(target, WorkerTraitTooltipWidth, WorkerTraitTooltipHeight);
        driversScreenUi.DetailTraitTooltipRoot.SetActive(true);
        driversScreenUi.DetailTraitTooltipRoot.transform.SetAsLastSibling();
    }

    private void ShowWorkerLeisurePreferenceTooltip(WorkerLeisurePreferenceKind preference, RectTransform target)
    {
        if (driversScreenUi?.DetailTraitTooltipRoot == null || target == null || driversScreenUi.WindowRoot == null)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        if (driversScreenUi.DetailTraitTooltipTitleText != null)
        {
            driversScreenUi.DetailTraitTooltipTitleText.text = GetWorkerLeisurePreferenceDisplayName(preference, ru);
            driversScreenUi.DetailTraitTooltipTitleText.color = GetWorkerLeisurePreferenceColor(preference);
        }

        if (driversScreenUi.DetailTraitTooltipBodyText != null)
        {
            driversScreenUi.DetailTraitTooltipBodyText.text = GetWorkerLeisurePreferenceDescription(preference, ru);
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
