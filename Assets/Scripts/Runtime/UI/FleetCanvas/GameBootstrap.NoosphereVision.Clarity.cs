using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private RectTransform noosphereVisionLegendRoot;
    private RectTransform noosphereVisionResidentTooltipRoot;
    private Text noosphereVisionResidentTooltipTitleText;
    private Text noosphereVisionResidentTooltipBodyText;

    private void CreateNoosphereVisionClarityUi(RectTransform field, Font font)
    {
        CreateNoosphereVisionLegend(field, font);
        CreateNoosphereVisionResidentTooltip(field, font);
    }

    private void CreateNoosphereVisionLegend(RectTransform field, Font font)
    {
        bool ru = IsRussianLanguage();
        noosphereVisionLegendRoot = CreateStyledPanel("NoosphereVisionLegend", field, new Color(0.025f, 0.035f, 0.055f, 0.74f));
        noosphereVisionLegendRoot.anchorMin = new Vector2(0f, 0f);
        noosphereVisionLegendRoot.anchorMax = new Vector2(0f, 0f);
        noosphereVisionLegendRoot.pivot = new Vector2(0f, 0f);
        noosphereVisionLegendRoot.anchoredPosition = new Vector2(28f, 76f);
        noosphereVisionLegendRoot.sizeDelta = new Vector2(468f, 46f);

        HorizontalLayoutGroup layout = noosphereVisionLegendRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 8, 8);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CreateNoosphereVisionLegendItem(font, ru ? "\u043d\u0430\u0434\u0435\u0436\u0434\u0430" : "hope", new Color(0.36f, 0.95f, 0.58f, 1f), 100f);
        CreateNoosphereVisionLegendItem(font, ru ? "\u0442\u0440\u0435\u0432\u043e\u0433\u0430" : "worry", new Color(1f, 0.42f, 0.25f, 1f), 100f);
        CreateNoosphereVisionLegendItem(font, ru ? "\u043d\u0435\u0439\u0442\u0440." : "neutral", new Color(0.62f, 0.82f, 1f, 1f), 82f);
        CreateNoosphereVisionLegendItem(font, ru ? "\u0444\u043e\u043a\u0443\u0441" : "focus", new Color(1f, 0.92f, 0.48f, 1f), 82f);
    }

    private void CreateNoosphereVisionLegendItem(Font font, string label, Color color, float width)
    {
        RectTransform item = CreateUiObject($"Legend_{label}", noosphereVisionLegendRoot).GetComponent<RectTransform>();
        item.sizeDelta = new Vector2(width, 24f);
        item.gameObject.AddComponent<LayoutElement>().preferredWidth = width;

        HorizontalLayoutGroup layout = item.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 6;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        RectTransform dot = CreateUiObject("Dot", item).GetComponent<RectTransform>();
        dot.sizeDelta = new Vector2(14f, 14f);
        dot.gameObject.AddComponent<LayoutElement>().preferredWidth = 14f;
        Image image = dot.gameObject.AddComponent<Image>();
        image.sprite = GetNoosphereVisionSoftDotSprite();
        image.color = color;
        image.raycastTarget = false;

        Text text = CreateBodyText("Label", item, font, label, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        text.raycastTarget = false;
        text.gameObject.AddComponent<LayoutElement>().preferredWidth = Mathf.Max(42f, width - 22f);
    }

    private void CreateNoosphereVisionResidentTooltip(RectTransform field, Font font)
    {
        noosphereVisionResidentTooltipRoot = CreateStyledPanel("NoosphereVisionResidentTooltip", field, new Color(0.035f, 0.050f, 0.072f, 0.94f));
        noosphereVisionResidentTooltipRoot.anchorMin = new Vector2(0.5f, 0.5f);
        noosphereVisionResidentTooltipRoot.anchorMax = new Vector2(0.5f, 0.5f);
        noosphereVisionResidentTooltipRoot.pivot = new Vector2(0f, 1f);
        noosphereVisionResidentTooltipRoot.sizeDelta = new Vector2(302f, 116f);
        noosphereVisionResidentTooltipRoot.GetComponent<Image>().raycastTarget = false;

        VerticalLayoutGroup layout = noosphereVisionResidentTooltipRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(13, 13, 10, 10);
        layout.spacing = 5;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        noosphereVisionResidentTooltipTitleText = CreateHeaderText("Title", noosphereVisionResidentTooltipRoot, font, string.Empty, 15, TextAnchor.MiddleLeft, Color.white);
        noosphereVisionResidentTooltipTitleText.raycastTarget = false;
        noosphereVisionResidentTooltipTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        noosphereVisionResidentTooltipBodyText = CreateBodyText("Body", noosphereVisionResidentTooltipRoot, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        noosphereVisionResidentTooltipBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        noosphereVisionResidentTooltipBodyText.verticalOverflow = VerticalWrapMode.Truncate;
        noosphereVisionResidentTooltipBodyText.raycastTarget = false;
        noosphereVisionResidentTooltipBodyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 72f;
        noosphereVisionResidentTooltipRoot.gameObject.SetActive(false);
    }

    private void UpdateNoosphereVisionResidentTooltip(float progress)
    {
        if (noosphereVisionResidentTooltipRoot == null)
        {
            return;
        }

        if (progress < 0.65f || noosphereVisionHoveredResident == null || !noosphereVisionHasMouseLocal)
        {
            noosphereVisionResidentTooltipRoot.gameObject.SetActive(false);
            return;
        }

        noosphereVisionResidentTooltipRoot.gameObject.SetActive(true);
        noosphereVisionResidentTooltipTitleText.text = string.IsNullOrWhiteSpace(noosphereVisionHoveredResident.DriverName)
            ? $"#{noosphereVisionHoveredResident.DriverId}"
            : noosphereVisionHoveredResident.DriverName;
        noosphereVisionResidentTooltipBodyText.text = FormatNoosphereVisionResidentTooltip(noosphereVisionHoveredResident);

        Vector2 panelSize = noosphereVisionResidentTooltipRoot.sizeDelta;
        Rect fieldRect = noosphereVisionUi.FieldRoot.rect;
        Vector2 target = noosphereVisionMouseLocal + new Vector2(18f, -18f);
        float minX = fieldRect.xMin + 16f;
        float maxX = fieldRect.xMax - panelSize.x - 16f;
        float minY = fieldRect.yMin + panelSize.y + 16f;
        float maxY = fieldRect.yMax - 16f;
        target.x = Mathf.Clamp(target.x, minX, maxX);
        target.y = Mathf.Clamp(target.y, minY, maxY);
        noosphereVisionResidentTooltipRoot.anchoredPosition = target;
    }

    private string FormatNoosphereVisionResidentTooltip(DriverAgent worker)
    {
        bool ru = IsRussianLanguage();
        WorkerThought thought = GetMostImportantWorkerThought(worker);
        string tone = FormatNoosphereVisionResidentTone(worker, ru);
        string family = worker.FamilyId > 0
            ? ru ? $"\u0441\u0435\u043c\u044c\u044f #{worker.FamilyId}" : $"family #{worker.FamilyId}"
            : ru ? "\u0431\u0435\u0437 \u0441\u0435\u043c\u044c\u0438" : "no family";

        if (thought != null)
        {
            string thoughtText = RenderWorkerThought(thought, ru);
            return ru
                ? $"{tone}, \u0441\u0438\u043b\u0430 {thought.Intensity}\n{thoughtText}\n\u0441\u0447\u0430\u0441\u0442\u044c\u0435 {worker.Satisfaction}, ${worker.Money}, {family}"
                : $"{tone}, strength {thought.Intensity}\n{thoughtText}\nsatisfaction {worker.Satisfaction}, ${worker.Money}, {family}";
        }

        return ru
            ? $"{tone}\n\u0441\u0447\u0430\u0441\u0442\u044c\u0435 {worker.Satisfaction}, \u0434\u0435\u043d\u044c\u0433\u0438 ${worker.Money}\n{family}"
            : $"{tone}\nsatisfaction {worker.Satisfaction}, money ${worker.Money}\n{family}";
    }

    private string FormatNoosphereVisionResidentTone(DriverAgent worker, bool ru)
    {
        WorkerThought thought = GetMostImportantWorkerThought(worker);
        if (thought != null)
        {
            return thought.Tone switch
            {
                WorkerThoughtTone.Positive => ru ? "\u043d\u0430\u0434\u0435\u0436\u0434\u0430" : "hope",
                WorkerThoughtTone.Negative => ru ? "\u0442\u0440\u0435\u0432\u043e\u0433\u0430" : "worry",
                _ => ru ? "\u043d\u0435\u0439\u0442\u0440\u0430\u043b\u044c\u043d\u043e" : "neutral"
            };
        }

        if (worker.Satisfaction >= 75)
        {
            return ru ? "\u0441\u043f\u043e\u043a\u043e\u0439\u043d\u043e" : "steady";
        }

        return worker.Satisfaction < 45
            ? ru ? "\u043d\u0430\u043f\u0440\u044f\u0436\u0451\u043d\u043d\u043e" : "strained"
            : ru ? "\u043d\u0435\u0439\u0442\u0440\u0430\u043b\u044c\u043d\u043e" : "neutral";
    }
}
