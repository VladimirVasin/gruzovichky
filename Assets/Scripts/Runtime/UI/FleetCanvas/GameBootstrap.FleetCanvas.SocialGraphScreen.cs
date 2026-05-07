using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int SocialGraphInspectorRows = 6;

    private bool isSocialGraphPanelOpen;
    private bool isSocialGraphScreenDirty = true;
    private int selectedSocialGraphWorkerId;
    private int hoveredSocialGraphWorkerId;
    private long hoveredSocialGraphEdgeKey;
    private SocialGraphFilterMode socialGraphFilterMode = SocialGraphFilterMode.Important;
    private SocialGraphScreenUiRefs socialGraphScreenUi;
    private static Sprite s_socialGraphCircleSprite;

    private sealed class SocialGraphScreenUiRefs
    {
        public GameObject CanvasRoot;
        public CanvasGroup CanvasGroup;
        public RectTransform WindowRoot;
        public Text TitleText;
        public Text SubtitleText;
        public RectTransform GraphCanvas;
        public Text EmptyText;
        public readonly List<Button> FilterButtons = new();
        public readonly List<Text> FilterButtonTexts = new();
        public Text InspectorNameText;
        public Text InspectorStatusText;
        public Text InspectorSummaryText;
        public Text LinksTitleText;
        public Text InspectorHintText;
        public Button OpenWorkerButton;
        public Text OpenWorkerButtonText;
        public readonly List<Button> LinkButtons = new();
        public readonly List<Text> LinkButtonTexts = new();
    }

    private void SetupSocialGraphScreenUi()
    {
        if (socialGraphScreenUi != null)
        {
            return;
        }

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        socialGraphScreenUi = new SocialGraphScreenUiRefs();

        GameObject canvasObject = new("SocialGraphScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        socialGraphScreenUi.CanvasRoot = canvasObject;
        socialGraphScreenUi.CanvasGroup = canvasObject.AddComponent<CanvasGroup>();

        RectTransform window = CreateStyledPanel("SocialGraphWindowRoot", canvasObject.transform, FleetPanelColor);
        SetCenteredWindow(window, 1240f, 720f, -12f);
        socialGraphScreenUi.WindowRoot = window;

        VerticalLayoutGroup windowLayout = window.gameObject.AddComponent<VerticalLayoutGroup>();
        windowLayout.padding = new RectOffset(18, 18, 18, 18);
        windowLayout.spacing = 12f;
        windowLayout.childControlWidth = true;
        windowLayout.childControlHeight = true;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childForceExpandHeight = false;

        socialGraphScreenUi.TitleText = CreateHeaderText("SocialGraphTitle", window, font, string.Empty, 24, TextAnchor.MiddleLeft, Color.white);
        socialGraphScreenUi.TitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;
        socialGraphScreenUi.SubtitleText = CreateBodyText("SocialGraphSubtitle", window, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        socialGraphScreenUi.SubtitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        RectTransform filterRow = CreateTabRow("SocialGraphFilterRow", window, 30f, 8f);
        CreateSocialGraphFilterButton(filterRow, font, SocialGraphFilterMode.Important);
        CreateSocialGraphFilterButton(filterRow, font, SocialGraphFilterMode.Conflict);
        CreateSocialGraphFilterButton(filterRow, font, SocialGraphFilterMode.Work);

        RectTransform bodyRow = CreateUiObject("SocialGraphBody", window).GetComponent<RectTransform>();
        bodyRow.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        HorizontalLayoutGroup bodyLayout = bodyRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        bodyLayout.spacing = 14f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = false;
        bodyLayout.childForceExpandHeight = true;

        RectTransform graphPanel = CreateStyledPanel("SocialGraphPanel", bodyRow, FleetInsetColor);
        LayoutElement graphLayout = graphPanel.gameObject.AddComponent<LayoutElement>();
        graphLayout.flexibleWidth = 1f;
        graphLayout.flexibleHeight = 1f;
        graphLayout.minWidth = 760f;

        socialGraphScreenUi.GraphCanvas = CreateUiObject("SocialGraphCanvas", graphPanel).GetComponent<RectTransform>();
        StretchRect(socialGraphScreenUi.GraphCanvas, 14f, 14f, 14f, 14f);

        socialGraphScreenUi.EmptyText = CreateBodyText("SocialGraphEmpty", graphPanel, font, string.Empty, 15, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
        StretchRect(socialGraphScreenUi.EmptyText.rectTransform, 24f, 24f, 24f, 24f);

        RectTransform inspectorPanel = CreateStyledPanel("SocialGraphInspector", bodyRow, FleetPanelColor);
        LayoutElement inspectorLayout = inspectorPanel.gameObject.AddComponent<LayoutElement>();
        inspectorLayout.preferredWidth = 330f;
        inspectorLayout.minWidth = 330f;
        inspectorLayout.flexibleHeight = 1f;

        VerticalLayoutGroup inspectorGroup = inspectorPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        inspectorGroup.padding = new RectOffset(16, 16, 16, 16);
        inspectorGroup.spacing = 10f;
        inspectorGroup.childControlWidth = true;
        inspectorGroup.childControlHeight = true;
        inspectorGroup.childForceExpandWidth = true;
        inspectorGroup.childForceExpandHeight = false;

        socialGraphScreenUi.InspectorNameText = CreateHeaderText("SocialGraphInspectorName", inspectorPanel, font, string.Empty, 20, TextAnchor.MiddleLeft, Color.white);
        socialGraphScreenUi.InspectorNameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        socialGraphScreenUi.InspectorStatusText = CreateBodyText("SocialGraphInspectorStatus", inspectorPanel, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetAccentColor);
        socialGraphScreenUi.InspectorStatusText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        socialGraphScreenUi.InspectorSummaryText = CreateBodyText("SocialGraphInspectorSummary", inspectorPanel, font, string.Empty, 11, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        socialGraphScreenUi.InspectorSummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 112f;

        socialGraphScreenUi.LinksTitleText = CreateHeaderText("SocialGraphLinksTitle", inspectorPanel, font, string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
        socialGraphScreenUi.LinksTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        for (int i = 0; i < SocialGraphInspectorRows; i++)
        {
            Button linkButton = CreateButton($"SocialGraphLink{i + 1}", inspectorPanel, font, out Text linkText, string.Empty, 12, FleetCardMutedColor, Color.white);
            linkText.alignment = TextAnchor.MiddleLeft;
            linkText.horizontalOverflow = HorizontalWrapMode.Wrap;
            linkButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;
            int rowIndex = i;
            linkButton.onClick.AddListener(() => OnSocialGraphLinkRowPressed(rowIndex));
            socialGraphScreenUi.LinkButtons.Add(linkButton);
            socialGraphScreenUi.LinkButtonTexts.Add(linkText);
        }

        socialGraphScreenUi.InspectorHintText = CreateBodyText("SocialGraphInspectorHint", inspectorPanel, font, string.Empty, 11, TextAnchor.UpperLeft, FleetMutedTextColor);
        socialGraphScreenUi.InspectorHintText.gameObject.AddComponent<LayoutElement>().preferredHeight = 52f;

        socialGraphScreenUi.OpenWorkerButton = CreateButton("SocialGraphOpenWorker", inspectorPanel, font, out socialGraphScreenUi.OpenWorkerButtonText, string.Empty, 13, FleetPrimaryButtonColor, Color.white);
        socialGraphScreenUi.OpenWorkerButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
        socialGraphScreenUi.OpenWorkerButton.onClick.AddListener(OpenSelectedSocialGraphWorkerInWorkers);

        AddOverlayCloseButton(window, font);
        socialGraphScreenUi.CanvasRoot.SetActive(false);
        isSocialGraphScreenDirty = true;
        UpdateSocialGraphScreenUi();
    }

    private void CreateSocialGraphFilterButton(RectTransform parent, Font font, SocialGraphFilterMode mode)
    {
        Button button = CreateButton($"SocialGraphFilter_{mode}", parent, font, out Text label, string.Empty, 12, FleetCardMutedColor, Color.white);
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 126f;
        layout.preferredHeight = 30f;
        layout.minHeight = 30f;
        layout.flexibleHeight = 0f;
        button.onClick.AddListener(() =>
        {
            socialGraphFilterMode = mode;
            hoveredSocialGraphWorkerId = 0;
            hoveredSocialGraphEdgeKey = 0;
            isSocialGraphScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.72f);
        });
        socialGraphScreenUi.FilterButtons.Add(button);
        socialGraphScreenUi.FilterButtonTexts.Add(label);
    }

    private void EnsureSocialGraphScreenUiReady()
    {
        if (socialGraphScreenUi == null)
        {
            SetupSocialGraphScreenUi();
        }
    }

    private void ResetSocialGraphScreenSelection()
    {
        selectedSocialGraphWorkerId = 0;
        hoveredSocialGraphWorkerId = 0;
        hoveredSocialGraphEdgeKey = 0;
        socialGraphFilterMode = SocialGraphFilterMode.Important;
    }

    private void UpdateSocialGraphScreenUi()
    {
        if (socialGraphScreenUi == null)
        {
            if (isSocialGraphPanelOpen)
            {
                SetupSocialGraphScreenUi();
            }

            return;
        }

        bool shouldShow = isSocialGraphPanelOpen;
        if (shouldShow)
        {
            if (!socialGraphScreenUi.CanvasRoot.activeSelf)
            {
                socialGraphScreenUi.CanvasRoot.SetActive(true);
                if (socialGraphScreenUi.CanvasGroup != null)
                {
                    socialGraphScreenUi.CanvasGroup.alpha = 0f;
                }

                BeginSocialGraphPanelVisibilityAnimation(true);
                isSocialGraphScreenDirty = true;
            }
            else if (IsSocialGraphPanelClosingAnimationActive())
            {
                BeginSocialGraphPanelVisibilityAnimation(true);
            }
        }
        else if (socialGraphScreenUi.CanvasRoot.activeSelf && !IsSocialGraphPanelClosingAnimationActive())
        {
            BeginSocialGraphPanelVisibilityAnimation(false);
        }

        if (!shouldShow)
        {
            UpdateSocialGraphAnimations();
            return;
        }

        if (!isSocialGraphScreenDirty)
        {
            UpdateSocialGraphAnimations();
            return;
        }

        RebuildSocialGraphScreen();
        isSocialGraphScreenDirty = false;
        UpdateSocialGraphAnimations();
    }

    private void RebuildSocialGraphScreen()
    {
        bool ru = IsRussianLanguage();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        socialGraphScreenUi.TitleText.text = ru ? "\u0421\u0432\u044f\u0437\u0438 \u0433\u043e\u0440\u043e\u0436\u0430\u043d" : "Citizen social graph";

        PrepareSocialGraphAnimatedRebuild();
        ClearUiChildren(socialGraphScreenUi.GraphCanvas);
        DriverAgent selected = ResolveSelectedSocialGraphWorker();
        bool hasSelectedWorker = selected != null;
        socialGraphScreenUi.SubtitleText.text = hasSelectedWorker
            ? (ru
                ? "\u0424\u043e\u043a\u0443\u0441 \u043d\u0430 \u0432\u044b\u0431\u0440\u0430\u043d\u043d\u043e\u043c \u0436\u0438\u0442\u0435\u043b\u0435: \u0432\u0438\u0434\u043d\u044b \u0435\u0433\u043e \u0432\u0430\u0436\u043d\u044b\u0435 \u0441\u0432\u044f\u0437\u0438."
                : "Focused on the selected citizen: shows their important links.")
            : (ru
                ? "\u041e\u0431\u0437\u043e\u0440 \u0432\u0441\u0435\u0445 \u0438\u0437\u0432\u0435\u0441\u0442\u043d\u044b\u0445 \u0441\u0432\u044f\u0437\u0435\u0439 \u0433\u043e\u0440\u043e\u0434\u0430. \u041a\u043b\u0438\u043a \u043f\u043e \u0443\u0437\u043b\u0443 \u0432\u043a\u043b\u044e\u0447\u0438\u0442 \u0444\u043e\u043a\u0443\u0441."
                : "Overview of all known city links. Click a node to focus on that citizen.");
        UpdateSocialGraphFilterButtons(ru, !hasSelectedWorker);
        socialGraphScreenUi.LinksTitleText.text = hasSelectedWorker
            ? (ru ? "\u0413\u043b\u0430\u0432\u043d\u044b\u0435 \u0441\u0432\u044f\u0437\u0438" : "Main links")
            : (ru ? "\u0412\u0430\u0436\u043d\u044b\u0435 \u0441\u0432\u044f\u0437\u0438 \u0433\u043e\u0440\u043e\u0434\u0430" : "Important city links");

        SocialGraphStats stats;
        List<SocialRelationViewModel> visibleRelations = hasSelectedWorker
            ? BuildSocialGraphVisibleRelations(selected, socialGraphFilterMode, out stats)
            : BuildSocialGraphCityRelations(socialGraphFilterMode, out stats);
        List<DriverAgent> visibleWorkers = hasSelectedWorker
            ? BuildSocialGraphFocusedWorkers(selected, visibleRelations)
            : BuildSocialGraphCityVisibleWorkers(visibleRelations);

        bool hasGraph = visibleWorkers.Count > 0;
        socialGraphScreenUi.EmptyText.gameObject.SetActive(!hasGraph);
        if (!hasGraph)
        {
            socialGraphScreenUi.EmptyText.text = ru
                ? "\u041f\u043e\u043a\u0430 \u043d\u0435\u0442 \u0436\u0438\u0442\u0435\u043b\u0435\u0439 \u0434\u043b\u044f \u0433\u0440\u0430\u0444\u0430."
                : "No citizens available for the graph yet.";
            UpdateSocialGraphInspector(null, visibleRelations, stats, ru);
            RememberSocialGraphCurrentView(null, visibleRelations, stats, ru);
            return;
        }

        Rect canvasRect = socialGraphScreenUi.GraphCanvas.rect;
        Vector2 canvasSize = canvasRect.size;
        if (canvasSize.x < 1f || canvasSize.y < 1f)
        {
            canvasSize = new Vector2(820f, 560f);
        }

        Dictionary<int, Vector2> positions = BuildSocialGraphPositions(selected, visibleWorkers, visibleRelations, canvasSize);
        Dictionary<int, SocialRelationViewModel> relationByWorkerId = BuildSocialGraphRelationByWorkerMap(visibleRelations);

        for (int i = 0; i < visibleRelations.Count; i++)
        {
            SocialRelationViewModel relation = visibleRelations[i];
            if (!positions.TryGetValue(relation.FocusWorkerId, out Vector2 a) || !positions.TryGetValue(relation.OtherWorkerId, out Vector2 b))
            {
                continue;
            }

            CreateSocialGraphEdge(relation, a, b);
        }

        for (int i = 0; i < visibleWorkers.Count; i++)
        {
            DriverAgent worker = visibleWorkers[i];
            if (!positions.TryGetValue(worker.DriverId, out Vector2 position))
            {
                continue;
            }

            relationByWorkerId.TryGetValue(worker.DriverId, out SocialRelationViewModel relation);
            CreateSocialGraphNode(worker, position, relation, font, ru);
        }

        UpdateSocialGraphInspector(selected, visibleRelations, stats, ru);
        RememberSocialGraphCurrentView(selected, visibleRelations, stats, ru);
    }

    private DriverAgent ResolveSelectedSocialGraphWorker()
    {
        if (selectedSocialGraphWorkerId <= 0)
        {
            return null;
        }

        if (!IsSocialGraphWorkerIdVisible(selectedSocialGraphWorkerId))
        {
            selectedSocialGraphWorkerId = 0;
            hoveredSocialGraphWorkerId = 0;
            hoveredSocialGraphEdgeKey = 0;
            return null;
        }

        return GetDriverAgentById(selectedSocialGraphWorkerId);
    }

    private List<DriverAgent> BuildSocialGraphFocusedWorkers(DriverAgent selected, List<SocialRelationViewModel> visibleRelations)
    {
        List<DriverAgent> visibleWorkers = new();
        if (selected != null && IsSocialGraphWorkerVisible(selected))
        {
            visibleWorkers.Add(selected);
        }

        for (int i = 0; i < visibleRelations.Count; i++)
        {
            DriverAgent other = visibleRelations[i].OtherWorker;
            if (other == null || !IsSocialGraphWorkerVisible(other))
            {
                continue;
            }

            visibleWorkers.Add(other);
        }

        return visibleWorkers;
    }

    private void CreateSocialGraphEdge(SocialRelationViewModel relation, Vector2 a, Vector2 b)
    {
        GameObject lineObject = CreateUiObject($"SocialGraphEdge_{relation.FocusWorkerId}_{relation.OtherWorkerId}", socialGraphScreenUi.GraphCanvas);
        RectTransform rect = lineObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        Image image = lineObject.AddComponent<Image>();
        image.raycastTarget = true;

        Vector2 delta = b - a;
        rect.anchoredPosition = (a + b) * 0.5f;
        rect.sizeDelta = new Vector2(delta.magnitude, Mathf.Lerp(1.8f, 5.4f, relation.Strength));
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        bool hovered = hoveredSocialGraphEdgeKey == relation.EdgeKey ||
                       hoveredSocialGraphWorkerId == relation.FocusWorkerId ||
                       hoveredSocialGraphWorkerId == relation.OtherWorkerId;
        Color color = GetWorkerSocialRelationshipColor(relation.Relationship);
        if (relation.Category == RelationCategory.Family)
        {
            color = FleetAccentColor;
        }

        color.a = hovered
            ? 0.95f
            : Mathf.Lerp(0.36f, 0.82f, relation.Importance);
        image.color = color;
        RegisterSocialGraphAnimatedEdge(relation, rect, image);

        AddSocialGraphHoverTrigger(
            lineObject,
            () => SetHoveredSocialGraphEdge(relation.EdgeKey),
            () => ClearHoveredSocialGraphEdge(relation.EdgeKey));
    }

    private void CreateSocialGraphNode(DriverAgent worker, Vector2 position, SocialRelationViewModel relation, Font font, bool ru)
    {
        bool selected = worker.DriverId == selectedSocialGraphWorkerId;
        bool hovered = worker.DriverId == hoveredSocialGraphWorkerId ||
                       (relation != null && hoveredSocialGraphEdgeKey == relation.EdgeKey);
        bool dimmed = (hoveredSocialGraphWorkerId > 0 || hoveredSocialGraphEdgeKey != 0) && !selected && !hovered;
        float size = selected
            ? 104f
            : Mathf.Lerp(58f, 88f, relation != null ? relation.Importance : 0.35f);

        GameObject nodeObject = CreateUiObject($"SocialGraphNode_{worker.DriverId}", socialGraphScreenUi.GraphCanvas);
        RectTransform nodeRect = nodeObject.GetComponent<RectTransform>();
        nodeRect.anchorMin = nodeRect.anchorMax = new Vector2(0.5f, 0.5f);
        nodeRect.pivot = new Vector2(0.5f, 0.5f);
        nodeRect.anchoredPosition = position;
        nodeRect.sizeDelta = new Vector2(size, size);

        Image background = nodeObject.AddComponent<Image>();
        background.sprite = GetSocialGraphCircleSprite();
        background.color = selected
            ? new Color(0.97f, 0.80f, 0.30f, 1f)
            : hovered ? new Color(0.36f, 0.48f, 0.66f, 1f)
                      : dimmed ? new Color(0.12f, 0.14f, 0.18f, 0.48f)
                               : new Color(0.24f, 0.30f, 0.40f, 0.94f);

        Outline outline = nodeObject.AddComponent<Outline>();
        outline.effectColor = selected
            ? FleetAccentColor
            : hovered ? GetWorkerSocialRelationshipColor(relation?.Relationship ?? 0)
                      : new Color(0f, 0f, 0f, 0.45f);
        outline.effectDistance = selected || hovered ? new Vector2(3f, -3f) : new Vector2(1f, -1f);

        Button button = nodeObject.AddComponent<Button>();
        button.targetGraphic = background;
        int workerId = worker.DriverId;
        button.onClick.AddListener(() =>
        {
            if (selectedSocialGraphWorkerId == workerId)
            {
                selectedSocialGraphWorkerId = 0;
                hoveredSocialGraphWorkerId = 0;
                hoveredSocialGraphEdgeKey = 0;
            }
            else
            {
                selectedSocialGraphWorkerId = workerId;
                hoveredSocialGraphWorkerId = 0;
                hoveredSocialGraphEdgeKey = 0;
            }

            isSocialGraphScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.75f);
        });
        if (!selected)
        {
            AddSocialGraphHoverTrigger(
                nodeObject,
                () => SetHoveredSocialGraphWorker(workerId),
                () => ClearHoveredSocialGraphWorker(workerId));
        }

        RectTransform portraitRoot = CreateUiObject("Portrait", nodeRect).GetComponent<RectTransform>();
        portraitRoot.anchorMin = portraitRoot.anchorMax = new Vector2(0.5f, 0.5f);
        portraitRoot.pivot = new Vector2(0.5f, 0.5f);
        portraitRoot.anchoredPosition = new Vector2(0f, size * 0.02f);
        portraitRoot.sizeDelta = new Vector2(size * 0.82f, size * 0.74f);
        portraitRoot.gameObject.AddComponent<RectMask2D>();
        DrawWorkerPortraitScaled(worker, portraitRoot, size / 150f);

        GameObject labelObject = CreateUiObject("Name", nodeRect);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -4f);
        labelRect.sizeDelta = new Vector2(Mathf.Max(96f, size + 26f), 20f);
        Text label = labelObject.AddComponent<Text>();
        label.font = font;
        label.fontSize = selected ? 12 : 10;
        label.fontStyle = selected ? FontStyle.Bold : FontStyle.Normal;
        label.alignment = TextAnchor.UpperCenter;
        label.color = dimmed ? FleetMutedTextColor : Color.white;
        label.raycastTarget = false;
        label.text = GetSocialGraphShortName(worker.DriverName, ru);
        RegisterSocialGraphAnimatedNode(worker, nodeRect, selected, relation);
    }

    private void UpdateSocialGraphInspector(DriverAgent selected, List<SocialRelationViewModel> visibleRelations, SocialGraphStats stats, bool ru)
    {
        if (selected == null)
        {
            socialGraphScreenUi.InspectorNameText.text = ru ? "\u0412\u0435\u0441\u044c \u0433\u043e\u0440\u043e\u0434" : "Whole city";
            socialGraphScreenUi.InspectorStatusText.text = ru ? "\u041e\u0431\u0437\u043e\u0440 \u0441\u0432\u044f\u0437\u0435\u0439" : "Relationship overview";
            socialGraphScreenUi.InspectorSummaryText.text = ru
                ? $"\u0421\u043e\u0446\u0438\u0430\u043b\u044c\u043d\u0430\u044f \u043a\u0430\u0440\u0442\u0430:\n\u0412\u0441\u0435\u0433\u043e \u0441\u0432\u044f\u0437\u0435\u0439: {stats.TotalKnownLinks}   \u041f\u043e\u043a\u0430\u0437\u0430\u043d\u043e: {stats.ShownLinks}\n\u0421\u043a\u0440\u044b\u0442\u043e \u0444\u0438\u043b\u044c\u0442\u0440\u043e\u043c: {stats.FilteredOutLinks}\n\u041f\u043e\u0437\u0438\u0442\u0438\u0432\u043d\u044b\u0445: {stats.PositiveLinks}   \u041d\u0435\u0439\u0442\u0440.: {stats.NeutralLinks}   \u041d\u0430\u043f\u0440.: {stats.TenseLinks}\n\u041a\u043b\u0438\u043a \u043f\u043e \u0436\u0438\u0442\u0435\u043b\u044e \u0432\u043a\u043b\u044e\u0447\u0438\u0442 \u0444\u043e\u043a\u0443\u0441."
                : $"Social map:\nTotal links: {stats.TotalKnownLinks}   Shown: {stats.ShownLinks}\nHidden by filter: {stats.FilteredOutLinks}\nPositive: {stats.PositiveLinks}   Neutral: {stats.NeutralLinks}   Tense: {stats.TenseLinks}\nClick a citizen to focus.";
            SocialRelationViewModel hoveredCityRelation = GetHoveredSocialGraphRelation(visibleRelations);
            socialGraphScreenUi.InspectorHintText.text = hoveredCityRelation != null
                ? FormatSocialGraphRelationDetail(null, hoveredCityRelation, ru)
                : (ru
                    ? "\u041d\u0430\u0432\u0435\u0434\u0438 \u043d\u0430 \u0443\u0437\u0435\u043b \u0438\u043b\u0438 \u043b\u0438\u043d\u0438\u044e, \u0447\u0442\u043e\u0431\u044b \u0443\u0432\u0438\u0434\u0435\u0442\u044c \u043f\u0440\u0438\u0447\u0438\u043d\u0443 \u0441\u0432\u044f\u0437\u0438."
                    : "Hover a node or line to see why this relationship is shown.");
            socialGraphScreenUi.OpenWorkerButton.interactable = false;
            socialGraphScreenUi.OpenWorkerButton.gameObject.SetActive(false);
            UpdateSocialGraphLinkRows(selected, visibleRelations, ru);
            return;
        }

        socialGraphScreenUi.InspectorNameText.text = selected.DriverName;
        socialGraphScreenUi.InspectorStatusText.text = GetWorkerListStatusLabel(selected, ru);

        string familyText = selected.FamilyId > 0
            ? (ru ? "\u0421\u0435\u043c\u044c\u044f: \u0435\u0441\u0442\u044c" : "Family: formed")
            : (ru ? "\u0421\u0435\u043c\u044c\u044f: \u043d\u0435\u0442" : "Family: none");
        string hiddenText = stats.FilteredOutLinks > 0
            ? (ru ? $"\n\u0421\u043a\u0440\u044b\u0442\u043e \u0444\u0438\u043b\u044c\u0442\u0440\u043e\u043c: {stats.FilteredOutLinks}" : $"\nHidden by filter: {stats.FilteredOutLinks}")
            : string.Empty;
        string strongestText = stats.StrongestRelation != null
            ? $"{stats.StrongestRelation.OtherWorkerName} - {GetSocialGraphCategoryLabel(stats.StrongestRelation.Category, ru)}, {GetSocialGraphToneLabel(stats.StrongestRelation.Relationship, ru)}"
            : "\u2014";
        socialGraphScreenUi.InspectorSummaryText.text = ru
            ? $"{familyText}\n\n\u0421\u043e\u0446\u0438\u0430\u043b\u044c\u043d\u0430\u044f \u043a\u0430\u0440\u0442\u0430:\n\u0412\u0441\u0435\u0433\u043e \u0441\u0432\u044f\u0437\u0435\u0439: {stats.TotalKnownLinks}   \u041f\u043e\u043a\u0430\u0437\u0430\u043d\u043e: {stats.ShownLinks}\n\u0421\u043a\u0440\u044b\u0442\u043e \u0441\u043b\u0430\u0431\u044b\u0445: {stats.HiddenWeakLinks}{hiddenText}\n\u041f\u043e\u0437\u0438\u0442\u0438\u0432\u043d\u044b\u0445: {stats.PositiveLinks}   \u041d\u0435\u0439\u0442\u0440.: {stats.NeutralLinks}   \u041d\u0430\u043f\u0440.: {stats.TenseLinks}\n\u0421\u0430\u043c\u0430\u044f \u0441\u0438\u043b\u044c\u043d\u0430\u044f: {strongestText}"
            : $"{familyText}\n\nSocial map:\nTotal links: {stats.TotalKnownLinks}   Shown: {stats.ShownLinks}\nHidden weak: {stats.HiddenWeakLinks}{hiddenText}\nPositive: {stats.PositiveLinks}   Neutral: {stats.NeutralLinks}   Tense: {stats.TenseLinks}\nStrongest: {strongestText}";
        SocialRelationViewModel hoveredRelation = GetHoveredSocialGraphRelation(visibleRelations);
        socialGraphScreenUi.InspectorHintText.text = hoveredRelation != null
            ? FormatSocialGraphRelationDetail(selected, hoveredRelation, ru)
            : (ru
                ? "\u041d\u0430\u0432\u0435\u0434\u0438 \u043d\u0430 \u0443\u0437\u0435\u043b \u0438\u043b\u0438 \u043b\u0438\u043d\u0438\u044e, \u0447\u0442\u043e\u0431\u044b \u0443\u0432\u0438\u0434\u0435\u0442\u044c \u043f\u0440\u0438\u0447\u0438\u043d\u0443 \u0441\u0432\u044f\u0437\u0438."
                : "Hover a node or line to see why this relationship is shown.");
        socialGraphScreenUi.OpenWorkerButton.interactable = true;
        socialGraphScreenUi.OpenWorkerButton.gameObject.SetActive(true);
        socialGraphScreenUi.OpenWorkerButtonText.text = ru ? "\u041e\u0442\u043a\u0440\u044b\u0442\u044c \u0432 \u0420\u0430\u0431\u043e\u0447\u0438\u0435" : "Open in Workers";
        UpdateSocialGraphLinkRows(selected, visibleRelations, ru);
    }

    private void UpdateSocialGraphLinkRows(DriverAgent selected, List<SocialRelationViewModel> visibleRelations, bool ru)
    {
        List<SocialRelationViewModel> relations = visibleRelations ?? BuildSocialGraphVisibleRelations(selected, socialGraphFilterMode, out _);
        for (int i = 0; i < socialGraphScreenUi.LinkButtons.Count; i++)
        {
            Button button = socialGraphScreenUi.LinkButtons[i];
            Text text = socialGraphScreenUi.LinkButtonTexts[i];
            bool active = i < relations.Count;
            button.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            SocialRelationViewModel relation = relations[i];
            string name = selected == null
                ? $"{GetSocialGraphShortName(relation.FocusWorker?.DriverName, ru)} <-> {GetSocialGraphShortName(relation.OtherWorkerName, ru)}"
                : relation.OtherWorkerName ?? (ru ? "\u041d\u0435\u0442 \u0434\u0430\u043d\u043d\u044b\u0445" : "Unknown");
            text.text = $"{name}\n{GetSocialGraphCategoryLabel(relation.Category, ru)} - {GetSocialGraphToneLabel(relation.Relationship, ru)}";
            text.color = GetWorkerSocialRelationshipColor(relation.Relationship);
            button.interactable = relation.FocusWorker != null || relation.OtherWorker != null;
        }
    }

    private void OnSocialGraphLinkRowPressed(int rowIndex)
    {
        DriverAgent selected = GetDriverAgentById(selectedSocialGraphWorkerId);
        List<SocialRelationViewModel> relations = selected != null
            ? BuildSocialGraphVisibleRelations(selected, socialGraphFilterMode, out _)
            : BuildSocialGraphCityRelations(socialGraphFilterMode, out _);
        if (rowIndex < 0 || rowIndex >= relations.Count)
        {
            return;
        }

        int nextWorkerId = selected != null
            ? relations[rowIndex].OtherWorkerId
            : relations[rowIndex].FocusWorkerId;
        if (!IsSocialGraphWorkerIdVisible(nextWorkerId))
        {
            return;
        }

        selectedSocialGraphWorkerId = nextWorkerId;
        hoveredSocialGraphWorkerId = 0;
        hoveredSocialGraphEdgeKey = 0;
        isSocialGraphScreenDirty = true;
        PlayUiSound(uiSelectClip, 0.72f);
    }

    private void OpenSelectedSocialGraphWorkerInWorkers()
    {
        if (!IsSocialGraphWorkerIdVisible(selectedSocialGraphWorkerId))
        {
            return;
        }

        selectedWorkerPanelDriverId = selectedSocialGraphWorkerId;
        shouldScrollWorkersListToSelected = true;
        isSocialGraphPanelOpen = false;
        isFleetPanelOpen = false;
        isDriversPanelOpen = true;
        isShiftsPanelOpen = false;
        isResourcesPanelOpen = false;
        isEconomyPanelOpen = false;
        isTradePanelOpen = false;
        isBuildPanelOpen = false;
        isStatesPanelOpen = false;
        activeWorkerDetailTab = WorkerDetailTab.Social;
        isDriversScreenDirty = true;
        isSocialGraphScreenDirty = true;
        PlayUiSound(uiPanelOpenClip, 0.82f);
    }

    private void AddSocialGraphHoverTrigger(GameObject target, System.Action enterAction, System.Action exitAction)
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>() ?? target.AddComponent<EventTrigger>();
        EventTrigger.Entry enter = new() { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => enterAction?.Invoke());
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new() { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => exitAction?.Invoke());
        trigger.triggers.Add(exit);
    }

    private void SetHoveredSocialGraphWorker(int workerId)
    {
        if (hoveredSocialGraphWorkerId == workerId)
        {
            return;
        }

        hoveredSocialGraphWorkerId = workerId;
        hoveredSocialGraphEdgeKey = 0;
        RefreshSocialGraphHoverState();
    }

    private void ClearHoveredSocialGraphWorker(int workerId)
    {
        if (hoveredSocialGraphWorkerId != workerId)
        {
            return;
        }

        hoveredSocialGraphWorkerId = 0;
        RefreshSocialGraphHoverState();
    }

    private void SetHoveredSocialGraphEdge(long edgeKey)
    {
        if (hoveredSocialGraphEdgeKey == edgeKey)
        {
            return;
        }

        hoveredSocialGraphEdgeKey = edgeKey;
        hoveredSocialGraphWorkerId = 0;
        RefreshSocialGraphHoverState();
    }

    private void ClearHoveredSocialGraphEdge(long edgeKey)
    {
        if (hoveredSocialGraphEdgeKey != edgeKey)
        {
            return;
        }

        hoveredSocialGraphEdgeKey = 0;
        RefreshSocialGraphHoverState();
    }

    private bool IsSocialGraphWorkerVisible(DriverAgent worker)
    {
        return worker != null &&
               worker.DriverId > 0 &&
               !worker.HasDepartedTown &&
               !worker.IsLeavingTown;
    }

    private bool IsSocialGraphWorkerIdVisible(int workerId)
    {
        DriverAgent worker = GetDriverAgentById(workerId);
        return IsSocialGraphWorkerVisible(worker);
    }

    private static string GetSocialGraphShortName(string fullName, bool ru)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            return ru ? "\u0416\u0438\u0442\u0435\u043b\u044c" : "Citizen";
        }

        int space = fullName.IndexOf(' ');
        return space > 1 ? fullName[..space] : fullName;
    }

    private static Sprite GetSocialGraphCircleSprite()
    {
        if (s_socialGraphCircleSprite != null)
        {
            return s_socialGraphCircleSprite;
        }

        const int size = 64;
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        float radius = center - 1.5f;
        float radiusSqr = radius * radius;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float distSqr = dx * dx + dy * dy;
                pixels[y * size + x] = distSqr <= radiusSqr ? Color.white : Color.clear;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        s_socialGraphCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return s_socialGraphCircleSprite;
    }
}
