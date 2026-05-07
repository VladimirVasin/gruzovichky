using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private readonly List<WorkerSocialGraphAnimatedNodeView> workerSocialGraphAnimatedNodes = new();
    private readonly List<WorkerSocialGraphAnimatedEdgeView> workerSocialGraphAnimatedEdges = new();
    private readonly Dictionary<int, WorkerSocialGraphAnimatedNodeView> workerSocialGraphAnimatedNodeById = new();
    private float workerSocialGraphRebuildAnimationStartedAt;
    private RectTransform workerSocialInspectorPortraitRoot;
    private Text workerSocialInspectorNameText;
    private Text workerSocialInspectorStatusText;
    private WorkerSocialMetricRowUi workerSocialTotalRow;
    private WorkerSocialMetricRowUi workerSocialShownRow;
    private WorkerSocialMetricRowUi workerSocialHiddenRow;
    private WorkerSocialMetricRowUi workerSocialPositiveRow;
    private WorkerSocialMetricRowUi workerSocialNeutralRow;
    private WorkerSocialMetricRowUi workerSocialTenseRow;
    private Text workerSocialStrongestNameText;
    private Text workerSocialStrongestMetaText;
    private Text workerSocialTipText;

    private sealed class WorkerSocialGraphAnimatedNodeView
    {
        public RectTransform Rect;
        public int WorkerId;
        public Vector2 BasePosition;
        public Vector2 AnimatedPosition;
        public bool IsSelected;
        public float Phase;
        public float SpawnDelay;
    }

    private sealed class WorkerSocialGraphAnimatedEdgeView
    {
        public RectTransform Rect;
        public Image Image;
        public long EdgeKey;
        public int FocusWorkerId;
        public int OtherWorkerId;
        public Color BaseColor;
        public float BaseWidth;
        public float SpawnDelay;
    }

    private sealed class WorkerSocialMetricRowUi
    {
        public Text LabelText;
        public Text ValueText;
    }

    private void SetupWorkerSocialTabUi(RectTransform socialTabRoot, Font font)
    {
        RectTransform socialCard = CreateResidentHudPanel("WorkerSocialGraphCard", socialTabRoot, ResidentHudCardColor, ResidentHudBorderColor);
        LayoutElement socialCardLayout = socialCard.gameObject.AddComponent<LayoutElement>();
        socialCardLayout.flexibleHeight = 1f;
        VerticalLayoutGroup socialLayout = socialCard.gameObject.AddComponent<VerticalLayoutGroup>();
        socialLayout.padding = new RectOffset(16, 16, 14, 16);
        socialLayout.spacing = 10f;
        socialLayout.childControlWidth = true;
        socialLayout.childControlHeight = true;
        socialLayout.childForceExpandWidth = true;
        socialLayout.childForceExpandHeight = false;

        driversScreenUi.DetailSocialTitleText = CreateHeaderText("WorkerSocialGraphTitle", socialCard, font, string.Empty, 16, TextAnchor.MiddleLeft, FleetAccentColor);
        driversScreenUi.DetailSocialTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        RectTransform bodyRow = CreateUiObject("WorkerSocialGraphBody", socialCard).GetComponent<RectTransform>();
        bodyRow.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        HorizontalLayoutGroup bodyLayout = bodyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 12f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childForceExpandHeight = true;

        RectTransform graphPanel = CreateResidentHudPanel("WorkerSocialGraphPanel", bodyRow, FleetInsetColor, ResidentHudBorderColor);
        LayoutElement graphLayout = graphPanel.gameObject.AddComponent<LayoutElement>();
        graphLayout.flexibleWidth = 1f;
        graphLayout.flexibleHeight = 1f;
        graphLayout.minWidth = 620f;

        driversScreenUi.DetailSocialGraphCanvas = CreateUiObject("WorkerSocialGraphCanvas", graphPanel).GetComponent<RectTransform>();
        StretchRect(driversScreenUi.DetailSocialGraphCanvas, 12f, 12f, 12f, 12f);

        driversScreenUi.DetailSocialEmptyText = CreateBodyText("WorkerSocialGraphEmpty", graphPanel, font, string.Empty, 14, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
        driversScreenUi.DetailSocialEmptyText.raycastTarget = false;
        StretchRect(driversScreenUi.DetailSocialEmptyText.rectTransform, 24f, 24f, 24f, 24f);

        RectTransform inspectorPanel = CreateResidentHudPanel("WorkerSocialGraphInspector", bodyRow, ResidentHudTileColor, ResidentHudBorderColor);
        LayoutElement inspectorLayout = inspectorPanel.gameObject.AddComponent<LayoutElement>();
        inspectorLayout.preferredWidth = 326f;
        inspectorLayout.minWidth = 300f;
        inspectorLayout.flexibleWidth = 0f;
        inspectorLayout.flexibleHeight = 1f;

        VerticalLayoutGroup inspectorGroup = inspectorPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        inspectorGroup.padding = new RectOffset(14, 14, 14, 14);
        inspectorGroup.spacing = 8f;
        inspectorGroup.childControlWidth = true;
        inspectorGroup.childControlHeight = true;
        inspectorGroup.childForceExpandWidth = true;
        inspectorGroup.childForceExpandHeight = false;

        SetupWorkerSocialInspectorPanel(inspectorPanel, font);
    }

    private void SetupWorkerSocialInspectorPanel(RectTransform parent, Font font)
    {
        RectTransform header = CreateResidentHudPanel("WorkerSocialInspectorHeader", parent, ResidentHudCardColor, ResidentHudBorderColor);
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        HorizontalLayoutGroup headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.padding = new RectOffset(10, 10, 8, 8);
        headerLayout.spacing = 10f;
        headerLayout.childAlignment = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth = true;
        headerLayout.childControlHeight = true;
        headerLayout.childForceExpandWidth = false;
        headerLayout.childForceExpandHeight = true;

        RectTransform portraitFrame = CreateResidentHudPanel("WorkerSocialInspectorPortraitFrame", header, FleetCardMutedColor, ResidentHudBorderColor);
        LayoutElement portraitLayout = portraitFrame.gameObject.AddComponent<LayoutElement>();
        portraitLayout.preferredWidth = 52f;
        portraitLayout.minWidth = 52f;
        portraitLayout.preferredHeight = 52f;
        portraitLayout.minHeight = 52f;
        portraitFrame.gameObject.AddComponent<RectMask2D>();
        Image portraitImage = portraitFrame.GetComponent<Image>();
        if (portraitImage != null)
        {
            portraitImage.raycastTarget = false;
        }

        workerSocialInspectorPortraitRoot = CreateUiObject("Portrait", portraitFrame).GetComponent<RectTransform>();
        StretchRect(workerSocialInspectorPortraitRoot, 0f, 0f, 0f, 0f);

        RectTransform identityColumn = CreateUiObject("Identity", header).GetComponent<RectTransform>();
        identityColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup identityLayout = identityColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        identityLayout.spacing = 2f;
        identityLayout.childControlWidth = true;
        identityLayout.childControlHeight = true;
        identityLayout.childForceExpandWidth = true;
        identityLayout.childForceExpandHeight = false;

        workerSocialInspectorNameText = CreateHeaderText("Name", identityColumn, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        workerSocialInspectorNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
        workerSocialInspectorNameText.verticalOverflow = VerticalWrapMode.Truncate;
        workerSocialInspectorNameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 31f;
        workerSocialInspectorStatusText = CreateBodyText("Status", identityColumn, font, string.Empty, 12, TextAnchor.MiddleLeft, ResidentHudPositiveColor);
        workerSocialInspectorStatusText.horizontalOverflow = HorizontalWrapMode.Wrap;
        workerSocialInspectorStatusText.verticalOverflow = VerticalWrapMode.Truncate;
        workerSocialInspectorStatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        RectTransform statsBody = CreateWorkerSocialInspectorSection(parent, font, "WorkerSocialStatsSection", 118f);
        CreateWorkerSocialInspectorSectionTitle(statsBody, font, "Статистика связей", "Connection stats");
        workerSocialTotalRow = CreateWorkerSocialMetricRow("Total", statsBody, font);
        workerSocialShownRow = CreateWorkerSocialMetricRow("Shown", statsBody, font);
        workerSocialHiddenRow = CreateWorkerSocialMetricRow("Hidden", statsBody, font);

        RectTransform toneBody = CreateWorkerSocialInspectorSection(parent, font, "WorkerSocialToneSection", 118f);
        CreateWorkerSocialInspectorSectionTitle(toneBody, font, "Разбивка по типу связей", "Relationship tone");
        workerSocialPositiveRow = CreateWorkerSocialToneRow("Positive", toneBody, font, GetWorkerSocialRelationshipColor(30));
        workerSocialNeutralRow = CreateWorkerSocialToneRow("Neutral", toneBody, font, GetWorkerSocialRelationshipColor(0));
        workerSocialTenseRow = CreateWorkerSocialToneRow("Tense", toneBody, font, GetWorkerSocialRelationshipColor(-30));

        RectTransform strongestBody = CreateWorkerSocialInspectorSection(parent, font, "WorkerSocialStrongestSection", 86f);
        CreateWorkerSocialInspectorSectionTitle(strongestBody, font, "Самая сильная", "Strongest link");
        workerSocialStrongestNameText = CreateHeaderText("StrongestName", strongestBody, font, string.Empty, 13, TextAnchor.MiddleLeft, new Color(0.58f, 0.78f, 1f, 1f));
        workerSocialStrongestNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
        workerSocialStrongestNameText.verticalOverflow = VerticalWrapMode.Truncate;
        workerSocialStrongestNameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 21f;
        workerSocialStrongestMetaText = CreateBodyText("StrongestMeta", strongestBody, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        workerSocialStrongestMetaText.horizontalOverflow = HorizontalWrapMode.Wrap;
        workerSocialStrongestMetaText.verticalOverflow = VerticalWrapMode.Truncate;
        workerSocialStrongestMetaText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        RectTransform legendBody = CreateWorkerSocialInspectorSection(parent, font, "WorkerSocialLegendSection", 100f);
        CreateWorkerSocialInspectorSectionTitle(legendBody, font, "Легенда", "Legend");
        CreateWorkerSocialLegendRow("LegendPositive", legendBody, font, "Позитивные", "Positive", GetWorkerSocialRelationshipColor(30));
        CreateWorkerSocialLegendRow("LegendNeutral", legendBody, font, "Нейтральные", "Neutral", GetWorkerSocialRelationshipColor(0));
        CreateWorkerSocialLegendRow("LegendTense", legendBody, font, "Напряжённые", "Tense", GetWorkerSocialRelationshipColor(-30));

        RectTransform tip = CreateResidentHudPanel("WorkerSocialTip", parent, new Color(0.045f, 0.095f, 0.15f, 0.94f), new Color(0.47f, 0.63f, 0.78f, 0.14f));
        tip.gameObject.AddComponent<LayoutElement>().preferredHeight = 66f;
        HorizontalLayoutGroup tipLayout = tip.gameObject.AddComponent<HorizontalLayoutGroup>();
        tipLayout.padding = new RectOffset(10, 10, 9, 9);
        tipLayout.spacing = 8f;
        tipLayout.childAlignment = TextAnchor.UpperLeft;
        tipLayout.childControlWidth = true;
        tipLayout.childControlHeight = true;
        tipLayout.childForceExpandWidth = false;
        tipLayout.childForceExpandHeight = true;

        Text icon = CreateHeaderText("InfoIcon", tip, font, "i", 15, TextAnchor.UpperCenter, FleetSecondaryTextColor);
        icon.gameObject.AddComponent<LayoutElement>().preferredWidth = 18f;
        workerSocialTipText = CreateBodyText("TipText", tip, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        workerSocialTipText.lineSpacing = 1.06f;
        workerSocialTipText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private RectTransform CreateWorkerSocialInspectorSection(Transform parent, Font font, string name, float preferredHeight)
    {
        RectTransform section = CreateResidentHudPanel(name, parent, ResidentHudCardColor, ResidentHudBorderColor);
        section.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        VerticalLayoutGroup sectionLayout = section.gameObject.AddComponent<VerticalLayoutGroup>();
        sectionLayout.padding = new RectOffset(12, 12, 9, 10);
        sectionLayout.spacing = 5f;
        sectionLayout.childControlWidth = true;
        sectionLayout.childControlHeight = true;
        sectionLayout.childForceExpandWidth = true;
        sectionLayout.childForceExpandHeight = false;
        return section;
    }

    private Text CreateWorkerSocialInspectorSectionTitle(Transform parent, Font font, string ruText, string enText)
    {
        Text title = CreateHeaderText("Title", parent, font, IsRussianLanguage() ? ruText : enText, 13, TextAnchor.MiddleLeft, FleetAccentColor);
        title.horizontalOverflow = HorizontalWrapMode.Wrap;
        title.verticalOverflow = VerticalWrapMode.Truncate;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 21f;
        return title;
    }

    private WorkerSocialMetricRowUi CreateWorkerSocialMetricRow(string name, Transform parent, Font font)
    {
        RectTransform row = CreateLayoutRow(name, parent, 22f, 8f);
        Text label = CreateBodyText("Label", row, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        Text value = CreateHeaderText("Value", row, font, string.Empty, 12, TextAnchor.MiddleRight, Color.white);
        value.gameObject.AddComponent<LayoutElement>().preferredWidth = 52f;
        return new WorkerSocialMetricRowUi { LabelText = label, ValueText = value };
    }

    private WorkerSocialMetricRowUi CreateWorkerSocialToneRow(string name, Transform parent, Font font, Color dotColor)
    {
        RectTransform row = CreateLayoutRow(name, parent, 22f, 7f);
        CreateWorkerSocialDot($"Dot{name}", row, dotColor);
        Text label = CreateBodyText("Label", row, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        Text value = CreateHeaderText("Value", row, font, string.Empty, 12, TextAnchor.MiddleRight, Color.white);
        value.gameObject.AddComponent<LayoutElement>().preferredWidth = 46f;
        return new WorkerSocialMetricRowUi { LabelText = label, ValueText = value };
    }

    private void CreateWorkerSocialLegendRow(string name, Transform parent, Font font, string ruText, string enText, Color dotColor)
    {
        RectTransform row = CreateLayoutRow(name, parent, 20f, 7f);
        CreateWorkerSocialDot($"Dot{name}", row, dotColor);
        Text label = CreateBodyText("Label", row, font, IsRussianLanguage() ? ruText : enText, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private void CreateWorkerSocialDot(string name, Transform parent, Color color)
    {
        RectTransform dot = CreateUiObject(name, parent).GetComponent<RectTransform>();
        Image dotImage = dot.gameObject.AddComponent<Image>();
        dotImage.sprite = GetSocialGraphCircleSprite();
        dotImage.color = color;
        dotImage.raycastTarget = false;
        LayoutElement dotLayout = dot.gameObject.AddComponent<LayoutElement>();
        dotLayout.preferredWidth = 11f;
        dotLayout.minWidth = 11f;
        dotLayout.preferredHeight = 11f;
        dotLayout.minHeight = 11f;
    }

    private void UpdateWorkerSocialGraphUi(DriverAgent worker, bool ru)
    {
        if (driversScreenUi?.DetailSocialGraphCanvas == null)
        {
            return;
        }

        if (driversScreenUi.DetailSocialTitleText != null)
        {
            driversScreenUi.DetailSocialTitleText.text = ru ? "\u0421\u043e\u0446\u0438\u0430\u043b\u044c\u043d\u044b\u0435 \u0441\u0432\u044f\u0437\u0438" : "Social Links";
        }

        if (driversScreenUi.DetailSocialTabRoot != null && !driversScreenUi.DetailSocialTabRoot.activeSelf)
        {
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        PrepareWorkerSocialGraphAnimatedRebuild();
        ClearUiChildren(driversScreenUi.DetailSocialGraphCanvas);

        if (worker == null || !IsSocialGraphWorkerVisible(worker))
        {
            if (driversScreenUi.DetailSocialEmptyText != null)
            {
                driversScreenUi.DetailSocialEmptyText.gameObject.SetActive(true);
                driversScreenUi.DetailSocialEmptyText.text = ru ? "\u0416\u0438\u0442\u0435\u043b\u044c \u043d\u0435 \u0432\u044b\u0431\u0440\u0430\u043d" : "No resident selected";
            }

            UpdateWorkerSocialGraphInspector(null, new List<SocialRelationViewModel>(), new SocialGraphStats(), ru);
            return;
        }

        SocialGraphStats stats;
        List<SocialRelationViewModel> visibleRelations = BuildSocialGraphVisibleRelations(worker, SocialGraphFilterMode.Important, out stats);
        ValidateWorkerSocialGraphHoverState(worker, visibleRelations);
        List<DriverAgent> visibleWorkers = BuildSocialGraphFocusedWorkers(worker, visibleRelations);

        Rect canvasRect = driversScreenUi.DetailSocialGraphCanvas.rect;
        Vector2 canvasSize = canvasRect.size;
        if (canvasSize.x < 1f || canvasSize.y < 1f)
        {
            canvasSize = new Vector2(720f, 460f);
        }

        Dictionary<int, Vector2> positions = BuildSocialGraphPositions(worker, visibleWorkers, visibleRelations, canvasSize);
        Dictionary<int, SocialRelationViewModel> relationByWorkerId = BuildSocialGraphRelationByWorkerMap(visibleRelations);

        for (int i = 0; i < visibleRelations.Count; i++)
        {
            SocialRelationViewModel relation = visibleRelations[i];
            if (!positions.TryGetValue(relation.FocusWorkerId, out Vector2 a) ||
                !positions.TryGetValue(relation.OtherWorkerId, out Vector2 b))
            {
                continue;
            }

            CreateWorkerSocialGraphEdge(relation, a, b);
        }

        for (int i = 0; i < visibleWorkers.Count; i++)
        {
            DriverAgent visibleWorker = visibleWorkers[i];
            if (!positions.TryGetValue(visibleWorker.DriverId, out Vector2 position))
            {
                continue;
            }

            relationByWorkerId.TryGetValue(visibleWorker.DriverId, out SocialRelationViewModel relation);
            CreateWorkerSocialGraphNode(worker.DriverId, visibleWorker, position, relation, font, ru);
        }

        if (driversScreenUi.DetailSocialEmptyText != null)
        {
            driversScreenUi.DetailSocialEmptyText.gameObject.SetActive(false);
        }

        UpdateWorkerSocialGraphInspector(worker, visibleRelations, stats, ru);
    }

    private void CreateWorkerSocialGraphEdge(SocialRelationViewModel relation, Vector2 a, Vector2 b)
    {
        GameObject lineObject = CreateUiObject($"WorkerSocialGraphEdge_{relation.FocusWorkerId}_{relation.OtherWorkerId}", driversScreenUi.DetailSocialGraphCanvas);
        RectTransform rect = lineObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        Image image = lineObject.AddComponent<Image>();
        image.raycastTarget = true;

        Vector2 delta = b - a;
        rect.anchoredPosition = (a + b) * 0.5f;
        rect.sizeDelta = new Vector2(delta.magnitude, Mathf.Lerp(2f, 5.2f, relation.Strength));
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        bool highlighted = IsWorkerSocialGraphEdgeHighlighted(relation);
        bool dimmed = IsWorkerSocialGraphHoverActive() && !highlighted;
        Color color = relation.Category == RelationCategory.Family
            ? FleetAccentColor
            : GetWorkerSocialRelationshipColor(relation.Relationship);
        color.a = dimmed ? 0.18f : highlighted && IsWorkerSocialGraphHoverActive() ? 0.95f : Mathf.Lerp(0.34f, 0.82f, relation.Importance);
        image.color = color;

        long edgeKey = relation.EdgeKey;
        AddSocialGraphHoverTrigger(
            lineObject,
            () => SetWorkerSocialGraphHoverEdge(edgeKey),
            () => ClearWorkerSocialGraphHoverEdge(edgeKey));
        RegisterWorkerSocialGraphAnimatedEdge(relation, rect, image);
    }

    private void CreateWorkerSocialGraphNode(int focusWorkerId, DriverAgent worker, Vector2 position, SocialRelationViewModel relation, Font font, bool ru)
    {
        bool selected = worker.DriverId == focusWorkerId;
        bool highlighted = IsWorkerSocialGraphNodeHighlighted(worker.DriverId, focusWorkerId);
        bool dimmed = IsWorkerSocialGraphHoverActive() && !highlighted;
        float size = selected ? 104f : Mathf.Lerp(58f, 88f, relation != null ? relation.Importance : 0.35f);

        GameObject nodeObject = CreateUiObject($"WorkerSocialGraphNode_{worker.DriverId}", driversScreenUi.DetailSocialGraphCanvas);
        RectTransform nodeRect = nodeObject.GetComponent<RectTransform>();
        nodeRect.anchorMin = nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
        nodeRect.pivot = new Vector2(0.5f, 0.5f);
        nodeRect.anchoredPosition = position;
        nodeRect.sizeDelta = new Vector2(size, size);

        Image background = nodeObject.AddComponent<Image>();
        background.sprite = GetSocialGraphCircleSprite();
        background.color = selected
            ? new Color(0.97f, 0.80f, 0.30f, 1f)
            : highlighted ? new Color(0.30f, 0.40f, 0.55f, 0.96f)
                          : new Color(0.12f, 0.14f, 0.18f, 0.48f);

        Outline outline = nodeObject.AddComponent<Outline>();
        outline.effectColor = selected
            ? FleetAccentColor
            : highlighted ? GetWorkerSocialRelationshipColor(relation?.Relationship ?? 0)
                          : new Color(0f, 0f, 0f, 0.42f);
        outline.effectDistance = selected || highlighted ? new Vector2(3f, -3f) : new Vector2(1f, -1f);

        Button button = nodeObject.AddComponent<Button>();
        button.targetGraphic = background;
        int workerId = worker.DriverId;
        button.onClick.AddListener(() =>
        {
            if (workerId == selectedWorkerPanelDriverId)
            {
                return;
            }

            selectedWorkerPanelDriverId = workerId;
            hoveredSocialGraphWorkerId = 0;
            hoveredSocialGraphEdgeKey = 0;
            shouldScrollWorkersListToSelected = true;
            isDriversScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.75f);
        });

        AddSocialGraphHoverTrigger(
            nodeObject,
            () => SetWorkerSocialGraphHoverWorker(workerId),
            () => ClearWorkerSocialGraphHoverWorker(workerId));

        RectTransform portraitRoot = CreateUiObject("Portrait", nodeRect).GetComponent<RectTransform>();
        portraitRoot.anchorMin = portraitRoot.anchorMax = new Vector2(0.5f, 0.5f);
        portraitRoot.pivot = new Vector2(0.5f, 0.5f);
        portraitRoot.anchoredPosition = new Vector2(0f, size * 0.02f);
        portraitRoot.sizeDelta = new Vector2(size * 0.82f, size * 0.74f);
        portraitRoot.gameObject.AddComponent<RectMask2D>();
        DrawWorkerPortraitScaled(worker, portraitRoot, size / 150f);

        RectTransform labelRect = CreateUiObject("Name", nodeRect).GetComponent<RectTransform>();
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -4f);
        labelRect.sizeDelta = new Vector2(Mathf.Max(96f, size + 26f), 20f);
        Text label = labelRect.gameObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = selected ? 12 : 10;
        label.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
        label.alignment = TextAnchor.UpperCenter;
        label.color = dimmed ? FleetMutedTextColor : Color.white;
        label.raycastTarget = false;
        label.text = GetSocialGraphShortName(worker.DriverName, ru);
        RegisterWorkerSocialGraphAnimatedNode(worker, nodeRect, selected, relation);
    }

    private void UpdateWorkerSocialGraphInspector(DriverAgent worker, List<SocialRelationViewModel> visibleRelations, SocialGraphStats stats, bool ru)
    {
        stats ??= new SocialGraphStats();
        string fallbackName = ru ? "\u041d\u0435\u0438\u0437\u0432\u0435\u0441\u0442\u043d\u044b\u0439 \u0436\u0438\u0442\u0435\u043b\u044c" : "Unknown resident";
        if (workerSocialInspectorNameText != null)
        {
            workerSocialInspectorNameText.text = worker != null ? worker.DriverName : fallbackName;
        }

        if (workerSocialInspectorStatusText != null)
        {
            workerSocialInspectorStatusText.text = worker != null ? GetWorkerListStatusLabel(worker, ru) : "\u2014";
            workerSocialInspectorStatusText.color = worker != null && worker.IsArrivingByBus
                ? FleetAccentColor
                : ResidentHudPositiveColor;
        }

        if (workerSocialInspectorPortraitRoot != null)
        {
            if (worker != null)
            {
                workerSocialInspectorPortraitRoot.gameObject.SetActive(true);
                DrawWorkerPortraitScaled(worker, workerSocialInspectorPortraitRoot, 0.43f);
            }
            else
            {
                ClearUiChildren(workerSocialInspectorPortraitRoot);
                workerSocialInspectorPortraitRoot.gameObject.SetActive(false);
            }
        }

        SetWorkerSocialMetricRow(workerSocialTotalRow, ru ? "\u0412\u0441\u0435\u0433\u043e \u0441\u0432\u044f\u0437\u0435\u0439:" : "Total links:", stats.TotalKnownLinks.ToString(), Color.white);
        SetWorkerSocialMetricRow(workerSocialShownRow, ru ? "\u041f\u043e\u043a\u0430\u0437\u0430\u043d\u043e:" : "Shown:", stats.ShownLinks.ToString(), Color.white);
        SetWorkerSocialMetricRow(workerSocialHiddenRow, ru ? "\u0421\u043a\u0440\u044b\u0442\u043e \u0441\u043b\u0430\u0431\u044b\u0445:" : "Hidden weak:", stats.HiddenWeakLinks.ToString(), Color.white);

        int total = Mathf.Max(0, stats.TotalKnownLinks);
        int positive = Mathf.Clamp(stats.PositiveLinks, 0, total);
        int tense = Mathf.Clamp(stats.TenseLinks, 0, Mathf.Max(0, total - positive));
        int neutral = Mathf.Max(0, total - positive - tense);
        SetWorkerSocialMetricRow(workerSocialPositiveRow, ru ? "\u041f\u043e\u0437\u0438\u0442\u0438\u0432\u043d\u044b\u0445:" : "Positive:", positive.ToString(), GetWorkerSocialRelationshipColor(30));
        SetWorkerSocialMetricRow(workerSocialNeutralRow, ru ? "\u041d\u0435\u0439\u0442\u0440.:" : "Neutral:", neutral.ToString(), GetWorkerSocialRelationshipColor(0));
        SetWorkerSocialMetricRow(workerSocialTenseRow, ru ? "\u041d\u0430\u043f\u0440.:" : "Tense:", tense.ToString(), GetWorkerSocialRelationshipColor(-30));

        if (stats.StrongestRelation != null)
        {
            if (workerSocialStrongestNameText != null)
            {
                workerSocialStrongestNameText.text = stats.StrongestRelation.OtherWorkerName ?? "\u2014";
            }

            if (workerSocialStrongestMetaText != null)
            {
                workerSocialStrongestMetaText.text = $"{GetSocialGraphCategoryLabel(stats.StrongestRelation.Category, ru)}, {GetSocialGraphToneLabel(stats.StrongestRelation.Relationship, ru)}";
                workerSocialStrongestMetaText.color = FleetSecondaryTextColor;
            }
        }
        else
        {
            if (workerSocialStrongestNameText != null)
            {
                workerSocialStrongestNameText.text = "\u2014";
            }

            if (workerSocialStrongestMetaText != null)
            {
                workerSocialStrongestMetaText.text = ru ? "\u041f\u043e\u043a\u0430 \u043d\u0435\u0434\u043e\u0441\u0442\u0430\u0442\u043e\u0447\u043d\u043e \u0434\u0430\u043d\u043d\u044b\u0445." : "Not enough data yet.";
                workerSocialStrongestMetaText.color = FleetMutedTextColor;
            }
        }

        if (workerSocialTipText != null)
        {
            SocialRelationViewModel hoveredRelation = GetHoveredSocialGraphRelation(visibleRelations);
            workerSocialTipText.text = hoveredRelation != null
                ? FormatWorkerSocialCompactRelationTip(worker, hoveredRelation, ru)
                : total <= 0
                    ? (ru
                        ? "\u0421\u0432\u044f\u0437\u0438 \u043f\u043e\u044f\u0432\u044f\u0442\u0441\u044f \u043f\u043e\u0441\u043b\u0435 \u043e\u0431\u0449\u0435\u043d\u0438\u044f \u0441 \u0434\u0440\u0443\u0433\u0438\u043c\u0438 \u0436\u0438\u0442\u0435\u043b\u044f\u043c\u0438."
                        : "Links appear after this resident meets others.")
                    : (ru
                        ? "\u041a\u043b\u0438\u043a \u043f\u043e \u0443\u0437\u043b\u0443 \u043e\u0442\u043a\u0440\u044b\u0432\u0430\u0435\u0442 \u044d\u0442\u043e\u0433\u043e \u0436\u0438\u0442\u0435\u043b\u044f \u0432 \u0446\u0435\u043d\u0442\u0440\u0435."
                        : "Click a node to open that resident in the center.");
        }
    }

    private void SetWorkerSocialMetricRow(WorkerSocialMetricRowUi row, string label, string value, Color valueColor)
    {
        if (row == null)
        {
            return;
        }

        if (row.LabelText != null)
        {
            row.LabelText.text = label;
        }

        if (row.ValueText != null)
        {
            row.ValueText.text = value;
            row.ValueText.color = valueColor;
        }
    }

    private string FormatWorkerSocialCompactRelationTip(DriverAgent selected, SocialRelationViewModel relation, bool ru)
    {
        string focusName = selected?.DriverName ?? relation.FocusWorker?.DriverName ?? (ru ? "\u0416\u0438\u0442\u0435\u043b\u044c" : "Resident");
        string otherName = relation.OtherWorkerName ?? (ru ? "\u0416\u0438\u0442\u0435\u043b\u044c" : "Resident");
        string category = GetSocialGraphCategoryLabel(relation.Category, ru);
        string tone = GetSocialGraphToneLabel(relation.Relationship, ru);
        int importance = Mathf.RoundToInt(relation.Importance * 100f);
        return ru
            ? $"{GetSocialGraphShortName(focusName, ru)} \u2194 {GetSocialGraphShortName(otherName, ru)}\n{category}, {tone}\n\u0421\u0438\u043b\u0430 {relation.Familiarity}/100, \u0432\u0430\u0436\u043d\u043e\u0441\u0442\u044c {importance}"
            : $"{GetSocialGraphShortName(focusName, ru)} \u2194 {GetSocialGraphShortName(otherName, ru)}\n{category}, {tone}\nStrength {relation.Familiarity}/100, importance {importance}";
    }

    private void ValidateWorkerSocialGraphHoverState(DriverAgent worker, List<SocialRelationViewModel> visibleRelations)
    {
        bool validHoveredWorker = hoveredSocialGraphWorkerId == 0 || hoveredSocialGraphWorkerId == worker.DriverId;
        bool validHoveredEdge = hoveredSocialGraphEdgeKey == 0;
        for (int i = 0; i < visibleRelations.Count; i++)
        {
            SocialRelationViewModel relation = visibleRelations[i];
            validHoveredWorker |= relation.OtherWorkerId == hoveredSocialGraphWorkerId;
            validHoveredEdge |= relation.EdgeKey == hoveredSocialGraphEdgeKey;
        }

        if (!validHoveredWorker)
        {
            hoveredSocialGraphWorkerId = 0;
        }

        if (!validHoveredEdge)
        {
            hoveredSocialGraphEdgeKey = 0;
        }
    }

    private bool IsWorkerSocialGraphHoverActive()
    {
        return hoveredSocialGraphWorkerId > 0 || hoveredSocialGraphEdgeKey != 0;
    }

    private bool IsWorkerSocialGraphNodeHighlighted(int workerId, int focusWorkerId)
    {
        if (!IsWorkerSocialGraphHoverActive())
        {
            return true;
        }

        if (workerId == focusWorkerId || workerId == hoveredSocialGraphWorkerId)
        {
            return true;
        }

        if (hoveredSocialGraphEdgeKey != 0)
        {
            int edgeA = (int)(hoveredSocialGraphEdgeKey >> 32);
            int edgeB = (int)(hoveredSocialGraphEdgeKey & 0xffffffff);
            return workerId == edgeA || workerId == edgeB;
        }

        return false;
    }

    private bool IsWorkerSocialGraphEdgeHighlighted(SocialRelationViewModel relation)
    {
        if (!IsWorkerSocialGraphHoverActive())
        {
            return true;
        }

        if (hoveredSocialGraphEdgeKey != 0)
        {
            return relation.EdgeKey == hoveredSocialGraphEdgeKey;
        }

        return hoveredSocialGraphWorkerId > 0 &&
               (relation.FocusWorkerId == hoveredSocialGraphWorkerId || relation.OtherWorkerId == hoveredSocialGraphWorkerId);
    }

    private void SetWorkerSocialGraphHoverWorker(int workerId)
    {
        if (hoveredSocialGraphWorkerId == workerId && hoveredSocialGraphEdgeKey == 0)
        {
            return;
        }

        hoveredSocialGraphWorkerId = workerId;
        hoveredSocialGraphEdgeKey = 0;
        isDriversScreenDirty = true;
    }

    private void ClearWorkerSocialGraphHoverWorker(int workerId)
    {
        if (hoveredSocialGraphWorkerId != workerId)
        {
            return;
        }

        hoveredSocialGraphWorkerId = 0;
        isDriversScreenDirty = true;
    }

    private void SetWorkerSocialGraphHoverEdge(long edgeKey)
    {
        if (hoveredSocialGraphEdgeKey == edgeKey && hoveredSocialGraphWorkerId == 0)
        {
            return;
        }

        hoveredSocialGraphEdgeKey = edgeKey;
        hoveredSocialGraphWorkerId = 0;
        isDriversScreenDirty = true;
    }

    private void ClearWorkerSocialGraphHoverEdge(long edgeKey)
    {
        if (hoveredSocialGraphEdgeKey != edgeKey)
        {
            return;
        }

        hoveredSocialGraphEdgeKey = 0;
        isDriversScreenDirty = true;
    }

    private void PrepareWorkerSocialGraphAnimatedRebuild()
    {
        workerSocialGraphAnimatedNodes.Clear();
        workerSocialGraphAnimatedEdges.Clear();
        workerSocialGraphAnimatedNodeById.Clear();
        workerSocialGraphRebuildAnimationStartedAt = Time.unscaledTime;
    }

    private void RegisterWorkerSocialGraphAnimatedNode(
        DriverAgent worker,
        RectTransform rect,
        bool selected,
        SocialRelationViewModel relation)
    {
        if (worker == null || rect == null)
        {
            return;
        }

        WorkerSocialGraphAnimatedNodeView view = new()
        {
            Rect = rect,
            WorkerId = worker.DriverId,
            BasePosition = rect.anchoredPosition,
            AnimatedPosition = rect.anchoredPosition,
            IsSelected = selected,
            Phase = worker.DriverId * 0.73f + (relation?.Importance ?? 0.42f) * 2.1f,
            SpawnDelay = Mathf.Min(0.14f, workerSocialGraphAnimatedNodes.Count * 0.016f)
        };
        workerSocialGraphAnimatedNodes.Add(view);
        workerSocialGraphAnimatedNodeById[worker.DriverId] = view;
    }

    private void RegisterWorkerSocialGraphAnimatedEdge(
        SocialRelationViewModel relation,
        RectTransform rect,
        Image image)
    {
        if (relation == null || rect == null || image == null)
        {
            return;
        }

        WorkerSocialGraphAnimatedEdgeView view = new()
        {
            Rect = rect,
            Image = image,
            EdgeKey = relation.EdgeKey,
            FocusWorkerId = relation.FocusWorkerId,
            OtherWorkerId = relation.OtherWorkerId,
            BaseColor = image.color,
            BaseWidth = rect.sizeDelta.y,
            SpawnDelay = Mathf.Min(0.16f, workerSocialGraphAnimatedEdges.Count * 0.012f)
        };
        workerSocialGraphAnimatedEdges.Add(view);
    }

    private void UpdateWorkerSocialGraphAnimations()
    {
        if (driversScreenUi == null ||
            driversScreenUi.CanvasRoot == null ||
            !driversScreenUi.CanvasRoot.activeSelf ||
            driversScreenUi.DetailSocialTabRoot == null ||
            !driversScreenUi.DetailSocialTabRoot.activeSelf)
        {
            return;
        }

        float now = Time.unscaledTime;
        for (int i = 0; i < workerSocialGraphAnimatedNodes.Count; i++)
        {
            WorkerSocialGraphAnimatedNodeView node = workerSocialGraphAnimatedNodes[i];
            if (node.Rect == null)
            {
                continue;
            }

            float spawn = GetSocialGraphSpawnProgress(now, node.SpawnDelay);
            bool hovered = hoveredSocialGraphWorkerId == node.WorkerId;
            node.AnimatedPosition = CalculateWorkerSocialGraphAnimatedNodePosition(node, now);
            node.Rect.anchoredPosition = node.AnimatedPosition;

            float hoverScale = hovered ? 1.075f : 1f;
            float pulseScale = node.IsSelected ? 1f + Mathf.Sin(now * 2.45f + node.Phase) * 0.014f : 1f;
            float wobbleScale = 1f + Mathf.Sin(now * 1.32f + node.Phase) * 0.01f;
            node.Rect.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, spawn) * hoverScale * pulseScale * wobbleScale;
            float rotation = Mathf.Sin(now * 1.02f + node.Phase) * (node.IsSelected ? 0.5f : 1.3f);
            node.Rect.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }

        for (int i = 0; i < workerSocialGraphAnimatedEdges.Count; i++)
        {
            UpdateWorkerSocialGraphAnimatedEdge(workerSocialGraphAnimatedEdges[i], now);
        }
    }

    private Vector2 CalculateWorkerSocialGraphAnimatedNodePosition(WorkerSocialGraphAnimatedNodeView node, float now)
    {
        Vector2 position = node.BasePosition;
        if (selectedWorkerPanelDriverId > 0 && !node.IsSelected && position.sqrMagnitude > 16f)
        {
            float orbitAngle = Mathf.Sin(now * 0.40f + node.Phase) * 3.8f;
            position = RotateSocialGraphVector(position, orbitAngle);
        }

        float wobble = node.IsSelected ? 1.15f : 2.45f;
        Vector2 offset = new(
            Mathf.Sin(now * 0.92f + node.Phase) * wobble,
            Mathf.Cos(now * 1.08f + node.Phase * 0.67f) * wobble * 0.72f);
        return position + offset;
    }

    private void UpdateWorkerSocialGraphAnimatedEdge(WorkerSocialGraphAnimatedEdgeView edge, float now)
    {
        if (edge.Rect == null ||
            edge.Image == null ||
            !workerSocialGraphAnimatedNodeById.TryGetValue(edge.FocusWorkerId, out WorkerSocialGraphAnimatedNodeView a) ||
            !workerSocialGraphAnimatedNodeById.TryGetValue(edge.OtherWorkerId, out WorkerSocialGraphAnimatedNodeView b))
        {
            return;
        }

        Vector2 delta = b.AnimatedPosition - a.AnimatedPosition;
        edge.Rect.anchoredPosition = (a.AnimatedPosition + b.AnimatedPosition) * 0.5f;
        edge.Rect.sizeDelta = new Vector2(delta.magnitude, edge.BaseWidth * (IsWorkerSocialGraphAnimatedEdgeHighlighted(edge) ? 1.08f : 1f));
        edge.Rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        float spawn = GetSocialGraphSpawnProgress(now, edge.SpawnDelay);
        bool highlighted = IsWorkerSocialGraphAnimatedEdgeHighlighted(edge);
        bool dimmed = IsWorkerSocialGraphHoverActive() && !highlighted;
        Color color = edge.BaseColor;
        color.a = Mathf.Clamp01(edge.BaseColor.a * spawn * (dimmed ? 0.30f : highlighted && IsWorkerSocialGraphHoverActive() ? 1.18f : 1f));
        edge.Image.color = color;
    }

    private bool IsWorkerSocialGraphAnimatedEdgeHighlighted(WorkerSocialGraphAnimatedEdgeView edge)
    {
        if (!IsWorkerSocialGraphHoverActive())
        {
            return true;
        }

        if (hoveredSocialGraphEdgeKey != 0)
        {
            return edge.EdgeKey == hoveredSocialGraphEdgeKey;
        }

        return hoveredSocialGraphWorkerId > 0 &&
               (edge.FocusWorkerId == hoveredSocialGraphWorkerId || edge.OtherWorkerId == hoveredSocialGraphWorkerId);
    }
}
