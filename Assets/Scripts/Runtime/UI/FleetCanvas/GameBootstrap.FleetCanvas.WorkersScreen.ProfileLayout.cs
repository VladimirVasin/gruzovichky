using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static readonly Color ResidentHudCardColor = new(0.055f, 0.115f, 0.185f, 0.98f);
    private static readonly Color ResidentHudTileColor = new(0.075f, 0.145f, 0.225f, 0.98f);
    private static readonly Color ResidentHudBorderColor = new(0.47f, 0.63f, 0.78f, 0.18f);
    private static readonly Color ResidentHudAmberBorderColor = new(0.95f, 0.65f, 0.12f, 0.82f);
    private static readonly Color ResidentHudPositiveColor = new(0.45f, 0.85f, 0.35f, 1f);
    private static Sprite s_workerProfileNeedsHeartIcon;

    private void SetupWorkerProfileTabUi(RectTransform profileTabRoot, Font font)
    {
        RectTransform dossierCard = CreateResidentHudPanel("WorkerDossierCard", profileTabRoot, ResidentHudCardColor, ResidentHudBorderColor);
        dossierCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 216f;
        HorizontalLayoutGroup dossierLayout = dossierCard.gameObject.AddComponent<HorizontalLayoutGroup>();
        dossierLayout.padding = new RectOffset(18, 18, 18, 18);
        dossierLayout.spacing = 16f;
        dossierLayout.childAlignment = TextAnchor.MiddleLeft;
        dossierLayout.childControlWidth = true;
        dossierLayout.childControlHeight = true;
        dossierLayout.childForceExpandWidth = false;
        dossierLayout.childForceExpandHeight = true;

        RectTransform portraitFrame = CreateResidentHudPanel("WorkerPortraitFrame", dossierCard, FleetCardMutedColor, ResidentHudBorderColor);
        LayoutElement portraitLayout = portraitFrame.gameObject.AddComponent<LayoutElement>();
        portraitLayout.preferredWidth = 150f;
        portraitLayout.minWidth = 150f;
        portraitLayout.preferredHeight = 170f;
        portraitLayout.minHeight = 166f;
        Image portraitFrameImage = portraitFrame.GetComponent<Image>();
        if (portraitFrameImage != null) portraitFrameImage.raycastTarget = false;
        driversScreenUi.DetailPortraitRoot = CreateUiObject("WorkerPortraitRoot", portraitFrame).GetComponent<RectTransform>();
        StretchRect(driversScreenUi.DetailPortraitRoot, 0f, 0f, 0f, 0f);

        RectTransform dossierColumn = CreateUiObject("WorkerDossierColumn", dossierCard).GetComponent<RectTransform>();
        LayoutElement dossierColumnLayout = dossierColumn.gameObject.AddComponent<LayoutElement>();
        dossierColumnLayout.flexibleWidth = 1f;
        dossierColumnLayout.minWidth = 500f;
        VerticalLayoutGroup dossierColumnGroup = dossierColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        dossierColumnGroup.spacing = 12f;
        dossierColumnGroup.childControlWidth = true;
        dossierColumnGroup.childControlHeight = true;
        dossierColumnGroup.childForceExpandWidth = true;
        dossierColumnGroup.childForceExpandHeight = false;

        driversScreenUi.DetailProfileTitleText = CreateHeaderText("WorkerProfileTitle", dossierColumn, font, string.Empty, 19, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailProfileTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        RectTransform tileGrid = CreateUiObject("WorkerDossierTileGrid", dossierColumn).GetComponent<RectTransform>();
        tileGrid.gameObject.AddComponent<LayoutElement>().preferredHeight = 144f;
        GridLayoutGroup tileGridGroup = tileGrid.gameObject.AddComponent<GridLayoutGroup>();
        tileGridGroup.cellSize = new Vector2(164f, 66f);
        tileGridGroup.spacing = new Vector2(10f, 10f);
        tileGridGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        tileGridGroup.constraintCount = 3;

        driversScreenUi.DetailGenderTile = CreateResidentInfoTile("WorkerGenderTile", tileGrid, font);
        driversScreenUi.DetailAgeTile = CreateResidentInfoTile("WorkerAgeTile", tileGrid, font);
        driversScreenUi.DetailEducationTile = CreateResidentInfoTile("WorkerEducationTile", tileGrid, font);
        driversScreenUi.DetailMoneyTile = CreateResidentInfoTile("WorkerMoneyTile", tileGrid, font);
        driversScreenUi.DetailHomeTile = CreateResidentInfoTile("WorkerHomeTile", tileGrid, font);
        driversScreenUi.DetailCarTile = CreateResidentInfoTile("WorkerCarTile", tileGrid, font);

        RectTransform skillsPanel = CreateResidentHudPanel("WorkerSkillsPanel", dossierCard, ResidentHudTileColor, ResidentHudBorderColor);
        LayoutElement skillsPanelLayout = skillsPanel.gameObject.AddComponent<LayoutElement>();
        skillsPanelLayout.preferredWidth = 352f;
        skillsPanelLayout.minWidth = 336f;
        VerticalLayoutGroup skillsPanelGroup = skillsPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        skillsPanelGroup.padding = new RectOffset(16, 16, 14, 14);
        skillsPanelGroup.spacing = 12f;
        skillsPanelGroup.childControlWidth = true;
        skillsPanelGroup.childControlHeight = true;
        skillsPanelGroup.childForceExpandWidth = true;
        skillsPanelGroup.childForceExpandHeight = false;

        driversScreenUi.DetailSkillsTitleText = CreateHeaderText("WorkerSkillsTitle", skillsPanel, font, string.Empty, 16, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailSkillsTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        RectTransform skillsRow = CreateLayoutRow("WorkerSkillsRow", skillsPanel, 84f, 10f);
        driversScreenUi.DetailLogisticsSkillTile = CreateWorkerSkillTile("WorkerLogisticsSkill", skillsRow, font);
        driversScreenUi.DetailProductionSkillTile = CreateWorkerSkillTile("WorkerProductionSkill", skillsRow, font);
        driversScreenUi.DetailServiceSkillTile = CreateWorkerSkillTile("WorkerServiceSkill", skillsRow, font);

        RectTransform conditionRow = CreateLayoutRow("WorkerConditionRow", profileTabRoot, 250f, 14f);

        RectTransform needsCard = CreateResidentHudPanel("WorkerNeedsCard", conditionRow, ResidentHudCardColor, ResidentHudBorderColor);
        needsCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup needsCardLayout = needsCard.gameObject.AddComponent<VerticalLayoutGroup>();
        needsCardLayout.padding = new RectOffset(18, 18, 16, 16);
        needsCardLayout.spacing = 8f;
        needsCardLayout.childControlWidth = true;
        needsCardLayout.childControlHeight = true;
        needsCardLayout.childForceExpandWidth = true;
        needsCardLayout.childForceExpandHeight = false;

        RectTransform needsTitleRow = CreateLayoutRow("WorkerNeedsTitleRow", needsCard, 24f, 10f);
        CreateWorkerProfileIconImage("NeedsTitleIcon", needsTitleRow, GetWorkerProfileNeedsHeartIcon(), 22f, FleetAccentColor);
        driversScreenUi.DetailNeedsTitleText = CreateHeaderText("WorkerNeedsTitle", needsTitleRow, font, string.Empty, 16, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailNeedsTitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        driversScreenUi.DetailMealNeedRow = CreateWorkerNeedRow("WorkerMealNeed", needsCard, font, GetNeedsMealIcon());
        driversScreenUi.DetailSleepNeedRow = CreateWorkerNeedRow("WorkerSleepNeed", needsCard, font, GetNeedsSleepIcon());
        driversScreenUi.DetailLeisureNeedRow = CreateWorkerNeedRow("WorkerLeisureNeed", needsCard, font, GetNeedsLeisureIcon());

        RectTransform overallRow = CreateResidentHudPanel("WorkerNeedsOverallRow", needsCard, new Color(0.06f, 0.12f, 0.18f, 0.7f), new Color(0.47f, 0.63f, 0.78f, 0.10f));
        overallRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        HorizontalLayoutGroup overallLayout = overallRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        overallLayout.padding = new RectOffset(10, 10, 3, 3);
        overallLayout.spacing = 10f;
        overallLayout.childControlWidth = true;
        overallLayout.childControlHeight = true;
        overallLayout.childForceExpandWidth = false;
        overallLayout.childForceExpandHeight = true;
        driversScreenUi.DetailOverallNeedsLabelText = CreateBodyText("OverallLabel", overallRow, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        driversScreenUi.DetailOverallNeedsLabelText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        driversScreenUi.DetailOverallNeedsValueText = CreateHeaderText("OverallValue", overallRow, font, string.Empty, 12, TextAnchor.MiddleRight, ResidentHudPositiveColor);
        driversScreenUi.DetailOverallNeedsValueText.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;

        RectTransform perksCard = CreateResidentHudPanel("WorkerPerksCard", conditionRow, ResidentHudCardColor, ResidentHudBorderColor);
        perksCard.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1.28f;
        VerticalLayoutGroup perksCardLayout = perksCard.gameObject.AddComponent<VerticalLayoutGroup>();
        perksCardLayout.padding = new RectOffset(18, 18, 16, 16);
        perksCardLayout.spacing = 7f;
        perksCardLayout.childControlWidth = true;
        perksCardLayout.childControlHeight = true;
        perksCardLayout.childForceExpandWidth = true;
        perksCardLayout.childForceExpandHeight = false;

        driversScreenUi.DetailPerksTitleText = CreateHeaderText("WorkerPerksTitle", perksCard, font, string.Empty, 16, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailPerksTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        driversScreenUi.DetailPerksEmptyText = CreateBodyText("WorkerPerksEmpty", perksCard, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        driversScreenUi.DetailPerksEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        for (int i = 0; i < WorkerPerkHudRowCount; i++)
        {
            Text perkText = CreateBodyText($"WorkerPerkRow{i + 1}", perksCard, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            perkText.lineSpacing = 1.08f;
            perkText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
            perkText.gameObject.SetActive(false);
            driversScreenUi.DetailPerkTexts.Add(perkText);
        }

        RectTransform workCard = CreateResidentHudPanel("WorkerWorkCard", profileTabRoot, ResidentHudCardColor, ResidentHudBorderColor);
        driversScreenUi.DetailWorkCardLayout = workCard.gameObject.AddComponent<LayoutElement>();
        driversScreenUi.DetailWorkCardLayout.preferredHeight = 112f;
        VerticalLayoutGroup workCardLayout = workCard.gameObject.AddComponent<VerticalLayoutGroup>();
        workCardLayout.padding = new RectOffset(20, 20, 14, 14);
        workCardLayout.spacing = 7f;
        workCardLayout.childControlWidth = true;
        workCardLayout.childControlHeight = true;
        workCardLayout.childForceExpandWidth = true;
        workCardLayout.childForceExpandHeight = false;

        driversScreenUi.DetailWorkTitleText = CreateHeaderText("WorkerWorkTitle", workCard, font, string.Empty, 16, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailWorkTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        RectTransform assignRow = CreateProfileValueRow("AssignRow", workCard, font, out driversScreenUi.DetailAssignmentLabel, out driversScreenUi.DetailAssignmentValue);
        driversScreenUi.DetailAssignmentRow = assignRow;
        RectTransform shiftRow = CreateProfileValueRow("ShiftRow", workCard, font, out driversScreenUi.DetailShiftLabel, out driversScreenUi.DetailShiftText);
        driversScreenUi.DetailShiftRow = shiftRow;
        RectTransform dutyRow = CreateProfileValueRow("DutyRow", workCard, font, out driversScreenUi.DetailDutyLabel, out driversScreenUi.DetailDutyStateText);
        driversScreenUi.DetailDutyRow = dutyRow;
    }

    private RectTransform CreateResidentHudPanel(string name, Transform parent, Color fill, Color outlineColor)
    {
        RectTransform panel = CreateStyledPanel(name, parent, fill);
        Outline outline = panel.GetComponent<Outline>();
        if (outline != null)
        {
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1f, -1f);
        }

        return panel;
    }

    private ResidentInfoTileUi CreateResidentInfoTile(string name, Transform parent, Font font)
    {
        RectTransform tile = CreateResidentHudPanel(name, parent, ResidentHudTileColor, ResidentHudBorderColor);
        VerticalLayoutGroup tileLayout = tile.gameObject.AddComponent<VerticalLayoutGroup>();
        tileLayout.padding = new RectOffset(14, 12, 9, 9);
        tileLayout.spacing = 5f;
        tileLayout.childControlWidth = true;
        tileLayout.childControlHeight = true;
        tileLayout.childForceExpandWidth = true;
        tileLayout.childForceExpandHeight = false;

        ResidentInfoTileUi ui = new()
        {
            LabelText = CreateBodyText("Label", tile, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor),
            ValueText = CreateHeaderText("Value", tile, font, string.Empty, 15, TextAnchor.MiddleLeft, Color.white)
        };
        ui.LabelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        ui.ValueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        return ui;
    }

    private WorkerSkillTileUi CreateWorkerSkillTile(string name, Transform parent, Font font)
    {
        RectTransform tile = CreateResidentHudPanel(name, parent, ResidentHudCardColor, ResidentHudBorderColor);
        tile.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup tileLayout = tile.gameObject.AddComponent<VerticalLayoutGroup>();
        tileLayout.padding = new RectOffset(10, 10, 10, 10);
        tileLayout.spacing = 6f;
        tileLayout.childAlignment = TextAnchor.MiddleCenter;
        tileLayout.childControlWidth = true;
        tileLayout.childControlHeight = true;
        tileLayout.childForceExpandWidth = true;
        tileLayout.childForceExpandHeight = false;

        WorkerSkillTileUi ui = new()
        {
            LabelText = CreateBodyText("Label", tile, font, string.Empty, 12, TextAnchor.MiddleCenter, FleetSecondaryTextColor),
            ValueText = CreateHeaderText("Value", tile, font, string.Empty, 23, TextAnchor.MiddleCenter, Color.white)
        };
        ui.LabelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        ui.LabelText.verticalOverflow = VerticalWrapMode.Truncate;
        ui.LabelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        ui.ValueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        return ui;
    }

    private WorkerNeedRowUi CreateWorkerNeedRow(string name, Transform parent, Font font, Sprite iconSprite)
    {
        RectTransform row = CreateResidentHudPanel(name, parent, new Color(0.065f, 0.13f, 0.20f, 0.92f), new Color(0.47f, 0.63f, 0.78f, 0.10f));
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(10, 10, 6, 6);
        rowLayout.spacing = 9f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;
        WorkerNeedRowUi ui = new()
        {
            IconImage = CreateWorkerProfileIconImage("NeedIcon", row, iconSprite, 24f, Color.white),
            LabelText = CreateBodyText("NeedLabel", row, font, string.Empty, 12, TextAnchor.MiddleLeft, Color.white)
        };
        ui.LabelText.gameObject.AddComponent<LayoutElement>().preferredWidth = 110f;

        RectTransform segmentsRoot = CreateLayoutRow("NeedSegments", row, 18f, 4f);
        HorizontalLayoutGroup segmentsGroup = segmentsRoot.GetComponent<HorizontalLayoutGroup>();
        segmentsGroup.childAlignment = TextAnchor.MiddleCenter;
        LayoutElement segmentsLayout = segmentsRoot.gameObject.AddComponent<LayoutElement>();
        segmentsLayout.preferredWidth = 178f;
        segmentsLayout.minWidth = 166f;
        segmentsLayout.preferredHeight = 18f;
        segmentsLayout.minHeight = 14f;
        for (int i = 0; i < 6; i++)
        {
            RectTransform segment = CreateStyledPanel($"Segment{i + 1}", segmentsRoot, new Color(0.19f, 0.23f, 0.28f, 1f));
            LayoutElement segmentLayout = segment.gameObject.AddComponent<LayoutElement>();
            segmentLayout.preferredWidth = 24f;
            segmentLayout.minWidth = 22f;
            segmentLayout.preferredHeight = 10f;
            segmentLayout.minHeight = 10f;
            ui.SegmentImages.Add(segment.GetComponent<Image>());
        }

        CreateUiObject("NeedRowSpacer", row).AddComponent<LayoutElement>().flexibleWidth = 1f;
        ui.StatusText = CreateHeaderText("NeedStatus", row, font, string.Empty, 12, TextAnchor.MiddleRight, ResidentHudPositiveColor);
        ui.StatusText.gameObject.AddComponent<LayoutElement>().preferredWidth = 54f;
        return ui;
    }

    private static Image CreateWorkerProfileIconImage(string name, Transform parent, Sprite sprite, float size, Color color)
    {
        GameObject iconObject = CreateUiObject(name, parent);
        RectTransform rect = iconObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);
        Image image = iconObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        LayoutElement layout = iconObject.AddComponent<LayoutElement>();
        layout.preferredWidth = size;
        layout.preferredHeight = size;
        layout.minWidth = size;
        layout.minHeight = size;
        return image;
    }

    private static Sprite GetWorkerProfileNeedsHeartIcon() =>
        s_workerProfileNeedsHeartIcon ??= BuildWorkerInventorySprite(22, PaintWorkerProfileNeedsHeartIcon);

    private static void PaintWorkerProfileNeedsHeartIcon(Color[] pixels, int size)
    {
        Color amber = new(0.95f, 0.65f, 0.12f, 1f);
        int[,] points =
        {
            { 6, 5 }, { 7, 4 }, { 8, 4 }, { 9, 5 }, { 10, 6 },
            { 11, 5 }, { 12, 4 }, { 13, 4 }, { 14, 5 },
            { 5, 6 }, { 6, 7 }, { 7, 8 }, { 8, 9 }, { 9, 10 },
            { 10, 11 }, { 11, 12 }, { 12, 11 }, { 13, 10 }, { 14, 9 },
            { 15, 8 }, { 16, 7 }, { 17, 6 }
        };

        for (int i = 0; i < points.GetLength(0); i++)
        {
            WorkerInventoryIconSet(pixels, size, points[i, 0], points[i, 1], amber);
            WorkerInventoryIconSet(pixels, size, points[i, 0], points[i, 1] + 1, amber);
        }
    }

    private RectTransform CreateProfileValueRow(string name, Transform parent, Font font, out Text label, out Text value)
    {
        RectTransform row = CreateLayoutRow(name, parent, 26f, 16f);
        label = CreateBodyText("Label", row, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        label.gameObject.AddComponent<LayoutElement>().preferredWidth = 160f;
        value = CreateHeaderText("Value", row, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        value.horizontalOverflow = HorizontalWrapMode.Wrap;
        value.verticalOverflow = VerticalWrapMode.Truncate;
        value.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return row;
    }
}
