using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void ApplyNoosphereVisionUi(float progress)
    {
        if (noosphereVisionUi == null)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        noosphereVisionUi.CanvasGroup.alpha = progress;
        noosphereVisionUi.TitleText.text = ru ? "\u0412\u0438\u0434\u0435\u043d\u0438\u0435 \u043d\u043e\u043e\u0441\u0444\u0435\u0440\u044b" : "Noosphere Vision";
        NoosphereVisionInsight lead = GetNoosphereVisionLeadInsight();
        noosphereVisionUi.LeadText.text = lead == null
            ? ru ? "\u0413\u043e\u0440\u043e\u0434 \u0435\u0449\u0435 \u043c\u043e\u043b\u0447\u0438\u0442." : "The city is still quiet."
            : ru ? lead.TitleRu + ". " + lead.SummaryRu : lead.TitleEn + ". " + lead.SummaryEn;
        noosphereVisionUi.StatsText.text = ru
            ? $"\u0421\u043d\u0438\u043c\u043e\u043a: \u0434\u0435\u043d\u044c {currentDay}, {GetDayNightClockLabel()}  /  \u0436\u0438\u0442\u0435\u043b\u0435\u0439 {CountNoosphereVisionResidents()}  /  \u043c\u044b\u0441\u043b\u0435\u0439 {noosphereVisionInsights.Count}  /  F9 \u0436\u0443\u0440\u043d\u0430\u043b"
            : $"Snapshot: day {currentDay}, {GetDayNightClockLabel()}  /  residents {CountNoosphereVisionResidents()}  /  thoughts {noosphereVisionInsights.Count}  /  F9 journal";
        noosphereVisionUi.FooterText.text = ru
            ? "\u041a\u043b\u0438\u043a\u043d\u0438 \u043c\u044b\u0441\u043b\u044c, \u0447\u0442\u043e\u0431\u044b \u0443\u0432\u0438\u0434\u0435\u0442\u044c \u0438\u0441\u0442\u043e\u0447\u043d\u0438\u043a, \u044d\u0444\u0444\u0435\u043a\u0442 \u0438 \u0434\u0435\u0439\u0441\u0442\u0432\u0438\u0435. Esc / RMB - \u0432\u044b\u0439\u0442\u0438."
            : "Click a thought to see source, effect, and action. Esc / RMB to exit.";

        ApplyNoosphereVisionInsightCards(progress);
        ApplyNoosphereVisionSourcesAndLines(progress);
        ApplyNoosphereVisionDetailPanel();
    }

    private NoosphereVisionInsight GetNoosphereVisionLeadInsight()
    {
        return noosphereVisionInsights.Count > 0 ? noosphereVisionInsights[0] : null;
    }

    private void ApplyNoosphereVisionInsightCards(float progress)
    {
        RectTransform field = noosphereVisionUi.FieldRoot;
        Vector2 fieldSize = field.rect.size;
        float radius = Mathf.Min(fieldSize.x, fieldSize.y) * 0.31f;
        for (int i = 0; i < noosphereVisionUi.Insights.Count; i++)
        {
            NoosphereVisionInsightUi ui = noosphereVisionUi.Insights[i];
            bool active = i < noosphereVisionInsights.Count;
            ui.Root.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            NoosphereVisionInsight insight = noosphereVisionInsights[i];
            float angle = -90f + i * (360f / Mathf.Max(1, noosphereVisionInsights.Count));
            Vector2 target = new(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sin(angle * Mathf.Deg2Rad) * radius * 0.72f);
            ui.AnchoredPosition = target;
            float pop = Mathf.Clamp01(progress * 1.15f - i * 0.08f);
            ui.Root.anchoredPosition = Vector2.Lerp(Vector2.zero, target, SmoothNoosphereVisionProgress(pop));
            ui.Root.localScale = Vector3.one * Mathf.Lerp(0.72f, i == noosphereVisionSelectedInsightIndex ? 1.08f : 1f, pop);
            Color color = GetNoosphereVisionToneColor(insight.Tone);
            ui.Background.color = Color.Lerp(new Color(0.07f, 0.09f, 0.13f, 0.82f), color, i == noosphereVisionSelectedInsightIndex ? 0.62f : 0.38f);
            ui.TitleText.text = IsRussianLanguage() ? insight.TitleRu : insight.TitleEn;
            string tone = FormatNoosphereVisionTone(insight.Tone, IsRussianLanguage());
            ui.MetaText.text = IsRussianLanguage()
                ? $"{tone} / \u0441\u0438\u043b\u0430 {insight.Strength} / \u0438\u0441\u0442. {insight.SourceCount}"
                : $"{tone} / strength {insight.Strength} / src {insight.SourceCount}";
        }
    }

    private void ApplyNoosphereVisionSourcesAndLines(float progress)
    {
        int lineIndex = 0;
        int dotIndex = ApplyNoosphereVisionResidentDots(0, ref lineIndex, progress);
        for (int i = 0; i < noosphereVisionInsights.Count; i++)
        {
            NoosphereVisionInsight insight = noosphereVisionInsights[i];
            bool selectedOnly = noosphereVisionSelectedInsightIndex >= 0;
            bool visibleInsight = !selectedOnly || noosphereVisionSelectedInsightIndex == i;
            if (!visibleInsight)
            {
                continue;
            }

            Vector2 cardPos = i < noosphereVisionUi.Insights.Count ? noosphereVisionUi.Insights[i].AnchoredPosition : Vector2.zero;
            Color color = GetNoosphereVisionToneColor(insight.Tone);
            bool hasRealSources = insight.SourceCount > 0 || insight.SourceWorldPositions.Count > 0;
            if (hasRealSources && lineIndex < noosphereVisionUi.Lines.Count)
            {
                ApplyNoosphereVisionLine(noosphereVisionUi.Lines[lineIndex++], Vector2.zero, cardPos, new Color(color.r, color.g, color.b, 0.34f * progress), 2.2f);
            }

            int sourceLimit = Mathf.Min(insight.SourceWorldPositions.Count, selectedOnly ? 18 : 7);
            for (int j = 0; j < sourceLimit && dotIndex < noosphereVisionUi.SourceDots.Count; j++)
            {
                Vector2 local = WorldToNoosphereVisionLocal(insight.SourceWorldPositions[j]);
                NoosphereVisionSourceDotUi dot = noosphereVisionUi.SourceDots[dotIndex++];
                dot.Root.gameObject.SetActive(true);
                dot.Root.anchoredPosition = local;
                float pulse = 1f + Mathf.Sin(noosphereVisionAnimationTime * 3.1f + j * 0.7f + i) * 0.22f;
                dot.Root.localScale = Vector3.one * pulse;
                dot.Image.color = new Color(color.r, color.g, color.b, selectedOnly ? 0.96f : 0.72f);
                dot.InsightIndex = i;
                if (lineIndex < noosphereVisionUi.Lines.Count)
                {
                    ApplyNoosphereVisionLine(noosphereVisionUi.Lines[lineIndex++], local, Vector2.zero, new Color(color.r, color.g, color.b, selectedOnly ? 0.28f : 0.12f), selectedOnly ? 2.4f : 1.4f);
                }
            }
        }

        for (int i = dotIndex; i < noosphereVisionUi.SourceDots.Count; i++)
        {
            noosphereVisionUi.SourceDots[i].Root.gameObject.SetActive(false);
        }

        for (int i = lineIndex; i < noosphereVisionUi.Lines.Count; i++)
        {
            noosphereVisionUi.Lines[i].Rect.gameObject.SetActive(false);
        }
    }

    private void ApplyNoosphereVisionDetailPanel()
    {
        bool show = noosphereVisionSelectedInsightIndex >= 0 &&
                    noosphereVisionSelectedInsightIndex < noosphereVisionInsights.Count;
        noosphereVisionUi.DetailPanel.gameObject.SetActive(show);
        if (!show)
        {
            return;
        }

        bool ru = IsRussianLanguage();
        NoosphereVisionInsight insight = noosphereVisionInsights[noosphereVisionSelectedInsightIndex];
        noosphereVisionUi.DetailTitleText.text = ru ? insight.TitleRu : insight.TitleEn;
        string source = ru ? insight.SourceRu : insight.SourceEn;
        string effect = ru ? insight.EffectRu : insight.EffectEn;
        string summary = ru ? insight.SummaryRu : insight.SummaryEn;
        noosphereVisionUi.DetailBodyText.text = ru
            ? $"{summary}\n\n\u0418\u0441\u0442\u043e\u0447\u043d\u0438\u043a: {source}\n\n\u042d\u0444\u0444\u0435\u043a\u0442: {effect}"
            : $"{summary}\n\nSource: {source}\n\nEffect: {effect}";
        noosphereVisionUi.DetailActionText.text = ru
            ? $"\u0414\u0435\u0439\u0441\u0442\u0432\u0438\u0435: {insight.ActionRu}"
            : $"Action: {insight.ActionEn}";
    }

    private Vector2 WorldToNoosphereVisionLocal(Vector3 world)
    {
        if (mainCamera == null || noosphereVisionUi?.FieldRoot == null)
        {
            return Vector2.zero;
        }

        Vector3 screen = mainCamera.WorldToScreenPoint(world);
        if (screen.z < 0f)
        {
            screen.x = Screen.width * 0.5f;
            screen.y = Screen.height * 0.5f;
        }

        screen.x = Mathf.Clamp(screen.x, 80f, Screen.width - 80f);
        screen.y = Mathf.Clamp(screen.y, 70f, Screen.height - 70f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(noosphereVisionUi.FieldRoot, screen, null, out Vector2 local);
        return local;
    }

    private void ApplyNoosphereVisionLine(NoosphereVisionLineUi line, Vector2 from, Vector2 to, Color color, float width)
    {
        Vector2 delta = to - from;
        float length = delta.magnitude;
        if (length <= 0.01f)
        {
            line.Rect.gameObject.SetActive(false);
            return;
        }

        line.Rect.gameObject.SetActive(true);
        line.Rect.anchoredPosition = from;
        line.Rect.sizeDelta = new Vector2(length, width);
        line.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        line.Image.color = color;
    }

    private void AnimateNoosphereVisionUi()
    {
        if (noosphereVisionUi?.CoreRoot == null)
        {
            return;
        }

        float pulse = 1f + Mathf.Sin(noosphereVisionAnimationTime * 2.4f) * 0.035f;
        noosphereVisionUi.CoreRoot.localScale = Vector3.one * pulse;
        NoosphereVisionInsight lead = GetNoosphereVisionLeadInsight();
        Color target = lead != null ? GetNoosphereVisionToneColor(lead.Tone) : new Color(0.16f, 0.42f, 0.64f, 0.9f);
        Color ringColor = Color.Lerp(new Color(0.50f, 0.78f, 1f, 0.46f), target, 0.45f + Mathf.Sin(noosphereVisionAnimationTime * 1.9f) * 0.08f);
        ringColor.a = 0.42f + Mathf.Sin(noosphereVisionAnimationTime * 2.1f) * 0.08f;
        noosphereVisionUi.CoreImage.color = ringColor;
        noosphereVisionUi.CoreImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, noosphereVisionAnimationTime * 9f);

        if (noosphereVisionUi.CorePulseImage != null)
        {
            Color pulseColor = new(target.r, target.g, target.b, 0.055f + Mathf.Sin(noosphereVisionAnimationTime * 1.35f) * 0.025f);
            noosphereVisionUi.CorePulseImage.color = pulseColor;
            noosphereVisionUi.CorePulseImage.rectTransform.localScale = Vector3.one * (0.92f + Mathf.Sin(noosphereVisionAnimationTime * 1.35f) * 0.08f);
        }

        if (noosphereVisionUi.CoreInnerImage != null)
        {
            Color innerColor = Color.Lerp(Color.white, target, 0.42f);
            innerColor.a = 0.62f + Mathf.Sin(noosphereVisionAnimationTime * 3.4f) * 0.12f;
            noosphereVisionUi.CoreInnerImage.color = innerColor;
            noosphereVisionUi.CoreInnerImage.rectTransform.localScale = Vector3.one * (0.82f + Mathf.Sin(noosphereVisionAnimationTime * 2.8f) * 0.10f);
        }
    }

    private static Color GetNoosphereVisionToneColor(NoosphereVisionTone tone)
    {
        return tone switch
        {
            NoosphereVisionTone.Positive => new Color(0.32f, 0.86f, 0.54f, 0.96f),
            NoosphereVisionTone.Negative => new Color(1f, 0.33f, 0.18f, 0.96f),
            NoosphereVisionTone.Split => new Color(1f, 0.70f, 0.22f, 0.96f),
            _ => new Color(0.45f, 0.74f, 1f, 0.92f)
        };
    }

    private static string FormatNoosphereVisionTone(NoosphereVisionTone tone, bool ru)
    {
        return tone switch
        {
            NoosphereVisionTone.Positive => ru ? "\u043d\u0430\u0434\u0435\u0436\u0434\u0430" : "hope",
            NoosphereVisionTone.Negative => ru ? "\u0442\u0440\u0435\u0432\u043e\u0433\u0430" : "worry",
            NoosphereVisionTone.Split => ru ? "\u0440\u0430\u0441\u043a\u043e\u043b" : "split",
            _ => ru ? "\u043d\u0435\u0439\u0442\u0440." : "neutral"
        };
    }
}
