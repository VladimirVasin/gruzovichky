using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private WorkerThoughtRowUi CreateWorkerThoughtRow(RectTransform parent, Font font, int index)
    {
        WorkerThoughtRowUi row = new();
        row.Root = CreateLayoutRow($"WorkerThoughtRow{index + 1}", parent, 32f, 8f);
        row.TimeText = CreateBodyText("Time", row.Root, font, string.Empty, 10, TextAnchor.MiddleLeft, FleetMutedTextColor);
        row.TimeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 58f;
        row.ToneText = CreateHeaderText("Tone", row.Root, font, string.Empty, 12, TextAnchor.MiddleCenter, FleetAccentColor);
        row.ToneText.gameObject.AddComponent<LayoutElement>().preferredWidth = 34f;
        row.BodyText = CreateBodyText("Thought", row.Root, font, string.Empty, 11, TextAnchor.MiddleLeft, Color.white);
        row.BodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.BodyText.verticalOverflow = VerticalWrapMode.Truncate;
        row.BodyText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        row.Root.gameObject.SetActive(false);
        return row;
    }

    private void UpdateWorkerThoughtsUi(DriverAgent worker, bool ru)
    {
        if (driversScreenUi == null)
        {
            return;
        }

        if (driversScreenUi.DetailThoughtsTitleText != null)
        {
            driversScreenUi.DetailThoughtsTitleText.text = ru ? "\u041c\u044b\u0441\u043b\u0438 \u0438 \u043c\u043d\u0435\u043d\u0438\u044f" : "Thoughts and Opinions";
        }

        UpdateWorkerOpinionChips(worker, ru);
        bool hasThoughts = worker != null && worker.Thoughts.Count > 0;
        if (driversScreenUi.DetailThoughtsEmptyText != null)
        {
            driversScreenUi.DetailThoughtsEmptyText.gameObject.SetActive(!hasThoughts);
            driversScreenUi.DetailThoughtsEmptyText.text = ru
                ? "\u041f\u043e\u043a\u0430 \u043d\u0435\u0442 \u044f\u0432\u043d\u044b\u0445 \u043c\u044b\u0441\u043b\u0435\u0439"
                : "No clear thoughts yet";
        }

        for (int i = 0; i < driversScreenUi.DetailThoughtRows.Count; i++)
        {
            WorkerThoughtRowUi row = driversScreenUi.DetailThoughtRows[i];
            bool active = hasThoughts && i < worker.Thoughts.Count;
            row.Root.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            WorkerThought thought = worker.Thoughts[i];
            row.TimeText.text = FormatWorkerThoughtTime(thought, ru);
            row.ToneText.text = FormatWorkerThoughtToneGlyph(thought.Tone);
            row.ToneText.color = GetWorkerThoughtToneColor(thought.Tone);
            row.BodyText.text = RenderWorkerThought(thought, ru);
            row.BodyText.color = thought.Tone == WorkerThoughtTone.Negative
                ? new Color(0.96f, 0.72f, 0.62f, 1f)
                : Color.white;
        }
    }

    private void UpdateWorkerOpinionChips(DriverAgent worker, bool ru)
    {
        List<WorkerOpinion> opinions = GetWorkerOpinionsSorted(worker);
        for (int i = 0; i < driversScreenUi.DetailOpinionChipTexts.Count; i++)
        {
            Text chip = driversScreenUi.DetailOpinionChipTexts[i];
            bool active = i < opinions.Count;
            chip.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            WorkerOpinion opinion = opinions[i];
            string label = ResolveWorkerOpinionSubject(opinion, ru);
            string prefix = opinion.Score >= 20
                ? (ru ? "\u041b\u044e\u0431\u0438\u0442" : "Likes")
                : opinion.Score <= -20
                    ? (ru ? "\u041d\u0435 \u043b\u044e\u0431\u0438\u0442" : "Dislikes")
                    : (ru ? "\u0414\u0443\u043c\u0430\u0435" : "Thinks");
            chip.text = $"{prefix}: {label}";
            chip.color = GetWorkerOpinionColor(opinion.Score);
        }
    }

    private List<WorkerOpinion> GetWorkerOpinionsSorted(DriverAgent worker)
    {
        List<WorkerOpinion> result = new();
        if (worker == null)
        {
            return result;
        }

        for (int i = 0; i < worker.Opinions.Count; i++)
        {
            WorkerOpinion opinion = worker.Opinions[i];
            if (opinion != null && opinion.Confidence > 0)
            {
                result.Add(opinion);
            }
        }

        result.Sort((a, b) =>
        {
            int strength = Mathf.Abs(b.Score).CompareTo(Mathf.Abs(a.Score));
            if (strength != 0) return strength;
            int confidence = b.Confidence.CompareTo(a.Confidence);
            if (confidence != 0) return confidence;
            return b.LastUpdatedWorldHour.CompareTo(a.LastUpdatedWorldHour);
        });
        return result;
    }

    private string ResolveWorkerOpinionSubject(WorkerOpinion opinion, bool ru)
    {
        if (opinion == null)
        {
            return string.Empty;
        }

        WorkerThoughtPlaceholder placeholder = new()
        {
            Key = "opinion",
            SubjectType = opinion.SubjectType,
            SubjectId = opinion.SubjectId,
            SubjectKey = opinion.SubjectKey,
            FallbackLabel = opinion.FallbackLabel
        };
        return ResolveWorkerThoughtPlaceholder(placeholder, ru);
    }

    private static string FormatWorkerThoughtTime(WorkerThought thought, bool ru)
    {
        if (thought == null)
        {
            return string.Empty;
        }

        int hour = Mathf.FloorToInt(Mathf.Repeat(thought.CreatedWorldHour, 24f));
        return ru ? $"\u0414{thought.CreatedDay} {hour:00}:00" : $"D{thought.CreatedDay} {hour:00}:00";
    }

    private static string FormatWorkerThoughtToneGlyph(WorkerThoughtTone tone)
    {
        return tone switch
        {
            WorkerThoughtTone.Positive => "+",
            WorkerThoughtTone.Negative => "!",
            _ => "."
        };
    }

    private static Color GetWorkerThoughtToneColor(WorkerThoughtTone tone)
    {
        return tone switch
        {
            WorkerThoughtTone.Positive => new Color(0.55f, 0.86f, 0.58f, 1f),
            WorkerThoughtTone.Negative => new Color(0.96f, 0.58f, 0.45f, 1f),
            _ => new Color(0.78f, 0.84f, 0.92f, 1f)
        };
    }

    private static Color GetWorkerOpinionColor(int score)
    {
        if (score >= 20) return new Color(0.55f, 0.86f, 0.58f, 1f);
        if (score <= -20) return new Color(0.96f, 0.58f, 0.45f, 1f);
        return new Color(0.78f, 0.84f, 0.92f, 1f);
    }
}
