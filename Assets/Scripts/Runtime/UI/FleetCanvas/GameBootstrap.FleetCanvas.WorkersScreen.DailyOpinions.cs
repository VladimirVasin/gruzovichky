using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void SetupWorkerDailyOpinionUi(RectTransform content, Font font)
    {
        driversScreenUi.DetailDailyOpinionTitleText = CreateHeaderText(
            "WorkerDailyOpinionSectionTitle",
            content,
            font,
            "Пережитый опыт",
            18,
            TextAnchor.MiddleLeft,
            FleetAccentColor);
        driversScreenUi.DetailDailyOpinionTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        RectTransform card = CreateResidentHudPanel(
            "WorkerDailyOpinionCard",
            content,
            new Color(0.045f, 0.095f, 0.15f, 0.88f),
            ResidentHudBorderColor);
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = 128f;
        driversScreenUi.DetailDailyOpinionBackground = card.GetComponent<Image>();
        driversScreenUi.DetailDailyOpinionOutline = card.GetComponent<Outline>();

        HorizontalLayoutGroup layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 18, 14, 14);
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        driversScreenUi.DetailDailyOpinionIcon = CreateWorkerThoughtIconImage(
            "DailyOpinionIcon",
            card,
            GetWorkerThoughtSpeechIcon(),
            42f,
            FleetMutedTextColor);

        RectTransform textColumn = CreateVerticalStack(
            "DailyOpinionTextColumn",
            card,
            new RectOffset(),
            4f,
            flexibleWidth: 1f);
        RectTransform headerRow = CreateLayoutRow("DailyOpinionHeaderRow", textColumn, 24f, 10f);
        driversScreenUi.DetailDailyOpinionToneText = CreateHeaderText(
            "DailyOpinionTone",
            headerRow,
            font,
            string.Empty,
            18,
            TextAnchor.MiddleLeft,
            Color.white);
        driversScreenUi.DetailDailyOpinionToneText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        driversScreenUi.DetailDailyOpinionScoreText = CreateBodyText(
            "DailyOpinionScore",
            headerRow,
            font,
            string.Empty,
            13,
            TextAnchor.MiddleRight,
            FleetMutedTextColor);
        driversScreenUi.DetailDailyOpinionScoreText.gameObject.AddComponent<LayoutElement>().preferredWidth = 260f;

        driversScreenUi.DetailDailyOpinionSummaryText = CreateBodyText(
            "DailyOpinionSummary",
            textColumn,
            font,
            string.Empty,
            14,
            TextAnchor.UpperLeft,
            FleetSecondaryTextColor);
        driversScreenUi.DetailDailyOpinionSummaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        driversScreenUi.DetailDailyOpinionSummaryText.verticalOverflow = VerticalWrapMode.Truncate;
        driversScreenUi.DetailDailyOpinionSummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        driversScreenUi.DetailDailyOpinionReasonText = CreateBodyText(
            "DailyOpinionReason",
            textColumn,
            font,
            string.Empty,
            13,
            TextAnchor.UpperLeft,
            FleetSecondaryTextColor);
        driversScreenUi.DetailDailyOpinionReasonText.horizontalOverflow = HorizontalWrapMode.Wrap;
        driversScreenUi.DetailDailyOpinionReasonText.verticalOverflow = VerticalWrapMode.Truncate;
        driversScreenUi.DetailDailyOpinionReasonText.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
    }

    private void UpdateWorkerDailyOpinionUi(DriverAgent worker)
    {
        WorkerDailyOpinion opinion = GetLatestWorkerDailyOpinion(worker);
        bool hasOpinion = opinion != null;
        bool positive = hasOpinion && opinion.FinalTone == WorkerDailyOpinionTone.Positive;
        Color accent = !hasOpinion
            ? FleetMutedTextColor
            : positive
                ? ResidentHudPositiveColor
                : new Color(0.94f, 0.34f, 0.24f, 1f);

        if (driversScreenUi.DetailDailyOpinionBackground != null)
        {
            driversScreenUi.DetailDailyOpinionBackground.color = hasOpinion && !positive
                ? new Color(0.15f, 0.075f, 0.055f, 0.78f)
                : new Color(0.045f, 0.095f, 0.15f, 0.88f);
        }

        if (driversScreenUi.DetailDailyOpinionOutline != null)
        {
            driversScreenUi.DetailDailyOpinionOutline.effectColor = hasOpinion && !positive
                ? new Color(0.94f, 0.34f, 0.24f, 0.46f)
                : ResidentHudBorderColor;
        }

        if (driversScreenUi.DetailDailyOpinionIcon != null)
        {
            driversScreenUi.DetailDailyOpinionIcon.sprite = hasOpinion && !positive
                ? GetWorkerInventoryWarningIcon()
                : GetWorkerThoughtSpeechIcon();
            driversScreenUi.DetailDailyOpinionIcon.color = accent;
        }

        if (driversScreenUi.DetailDailyOpinionToneText != null)
        {
            driversScreenUi.DetailDailyOpinionToneText.text = worker == null
                ? "Житель не выбран"
                : FormatWorkerDailyOpinionToneLabel(opinion, true);
            driversScreenUi.DetailDailyOpinionToneText.color = hasOpinion ? accent : Color.white;
        }

        if (driversScreenUi.DetailDailyOpinionSummaryText != null)
        {
            driversScreenUi.DetailDailyOpinionSummaryText.text = worker == null
                ? "Выберите жителя слева."
                : hasOpinion
                    ? opinion.SummaryRu
                    : "Пережитый опыт появится после первой полуночи.";
        }

        if (driversScreenUi.DetailDailyOpinionReasonText != null)
        {
            driversScreenUi.DetailDailyOpinionReasonText.text = worker == null
                ? string.Empty
                : BuildWorkerDailyOpinionReasonLine(opinion, true);
        }

        if (driversScreenUi.DetailDailyOpinionScoreText != null)
        {
            driversScreenUi.DetailDailyOpinionScoreText.text = FormatWorkerDailyOpinionScoreLine(opinion, true);
            driversScreenUi.DetailDailyOpinionScoreText.color = hasOpinion ? FleetMutedTextColor : new Color(1f, 1f, 1f, 0f);
        }
    }
}
