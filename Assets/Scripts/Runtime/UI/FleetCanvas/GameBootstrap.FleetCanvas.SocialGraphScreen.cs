using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int SocialGraphMaxNodes = 28;
    private const int SocialGraphMaxEdges = 96;
    private const int SocialGraphInspectorRows = 8;

    private bool isSocialGraphPanelOpen;
    private bool isSocialGraphScreenDirty = true;
    private int selectedSocialGraphWorkerId;
    private SocialGraphScreenUiRefs socialGraphScreenUi;
    private static Sprite s_socialGraphCircleSprite;

    private sealed class SocialGraphScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Text TitleText;
        public Text SubtitleText;
        public RectTransform GraphCanvas;
        public Text EmptyText;
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

    private sealed class SocialGraphEdgeModel
    {
        public int AId;
        public int BId;
        public int Familiarity;
        public int Relationship;
        public int InteractionCount;
        public WorkerSocialInteractionKind Kind;
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

        RectTransform window = CreateStyledPanel("SocialGraphWindowRoot", canvasObject.transform, FleetPanelColor);
        SetCenteredWindow(window, 1240f, 660f, -16f);
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
        socialGraphScreenUi.InspectorSummaryText = CreateBodyText("SocialGraphInspectorSummary", inspectorPanel, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        socialGraphScreenUi.InspectorSummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 70f;

        socialGraphScreenUi.LinksTitleText = CreateHeaderText("SocialGraphLinksTitle", inspectorPanel, font, string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
        socialGraphScreenUi.LinksTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        for (int i = 0; i < SocialGraphInspectorRows; i++)
        {
            Button linkButton = CreateButton($"SocialGraphLink{i + 1}", inspectorPanel, font, out Text linkText, string.Empty, 12, FleetCardMutedColor, Color.white);
            linkText.alignment = TextAnchor.MiddleLeft;
            linkText.horizontalOverflow = HorizontalWrapMode.Wrap;
            linkButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;
            int rowIndex = i;
            linkButton.onClick.AddListener(() => OnSocialGraphLinkRowPressed(rowIndex));
            socialGraphScreenUi.LinkButtons.Add(linkButton);
            socialGraphScreenUi.LinkButtonTexts.Add(linkText);
        }

        socialGraphScreenUi.InspectorHintText = CreateBodyText("SocialGraphInspectorHint", inspectorPanel, font, string.Empty, 11, TextAnchor.UpperLeft, FleetMutedTextColor);
        socialGraphScreenUi.InspectorHintText.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;

        socialGraphScreenUi.OpenWorkerButton = CreateButton("SocialGraphOpenWorker", inspectorPanel, font, out socialGraphScreenUi.OpenWorkerButtonText, string.Empty, 13, FleetPrimaryButtonColor, Color.white);
        socialGraphScreenUi.OpenWorkerButton.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;
        socialGraphScreenUi.OpenWorkerButton.onClick.AddListener(OpenSelectedSocialGraphWorkerInWorkers);

        AddOverlayCloseButton(window, font);
        socialGraphScreenUi.CanvasRoot.SetActive(false);
        isSocialGraphScreenDirty = true;
        UpdateSocialGraphScreenUi();
    }

    private void EnsureSocialGraphScreenUiReady()
    {
        if (socialGraphScreenUi == null)
        {
            SetupSocialGraphScreenUi();
        }
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
        if (socialGraphScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            socialGraphScreenUi.CanvasRoot.SetActive(shouldShow);
            isSocialGraphScreenDirty = true;
        }

        if (!shouldShow || !isSocialGraphScreenDirty)
        {
            return;
        }

        RebuildSocialGraphScreen();
        isSocialGraphScreenDirty = false;
    }

    private void RebuildSocialGraphScreen()
    {
        bool ru = IsRussianLanguage();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        socialGraphScreenUi.TitleText.text = ru ? "\u0421\u0432\u044f\u0437\u0438 \u0433\u043e\u0440\u043e\u0436\u0430\u043d" : "Citizen social graph";
        socialGraphScreenUi.SubtitleText.text = ru
            ? "\u0423\u0437\u043b\u044b - \u0440\u0430\u0431\u043e\u0447\u0438\u0435, \u043b\u0438\u043d\u0438\u0438 - \u0438\u0445 \u043f\u0430\u043c\u044f\u0442\u044c \u043e \u0432\u0441\u0442\u0440\u0435\u0447\u0430\u0445. \u041a\u043b\u0438\u043a \u043f\u043e \u0443\u0437\u043b\u0443 \u043f\u043e\u043a\u0430\u0437\u044b\u0432\u0430\u0435\u0442 \u0431\u043b\u0438\u0437\u043a\u0438\u0435 \u0441\u0432\u044f\u0437\u0438."
            : "Nodes are workers, lines are remembered contacts. Click a node to inspect its closest links.";
        socialGraphScreenUi.LinksTitleText.text = ru ? "\u0411\u043b\u0438\u0436\u0430\u0439\u0448\u0438\u0435 \u0441\u0432\u044f\u0437\u0438" : "Strongest links";

        ClearUiChildren(socialGraphScreenUi.GraphCanvas);
        List<DriverAgent> visibleWorkers = BuildSocialGraphVisibleWorkers();
        List<SocialGraphEdgeModel> edges = BuildSocialGraphEdges(visibleWorkers);

        bool hasGraph = visibleWorkers.Count > 0;
        socialGraphScreenUi.EmptyText.gameObject.SetActive(!hasGraph);
        if (!hasGraph)
        {
            socialGraphScreenUi.EmptyText.text = ru
                ? "\u041f\u043e\u043a\u0430 \u043d\u0435\u0442 \u0436\u0438\u0442\u0435\u043b\u0435\u0439 \u0434\u043b\u044f \u0433\u0440\u0430\u0444\u0430."
                : "No citizens available for the graph yet.";
            UpdateSocialGraphInspector(null, edges, ru);
            return;
        }

        Rect canvasRect = socialGraphScreenUi.GraphCanvas.rect;
        Vector2 canvasSize = canvasRect.size;
        if (canvasSize.x < 1f || canvasSize.y < 1f)
        {
            canvasSize = new Vector2(820f, 560f);
        }

        Dictionary<int, Vector2> positions = BuildSocialGraphPositions(visibleWorkers, canvasSize);
        Dictionary<int, int> degreeScores = BuildSocialGraphDegreeScores(visibleWorkers);

        for (int i = 0; i < edges.Count; i++)
        {
            SocialGraphEdgeModel edge = edges[i];
            if (!positions.TryGetValue(edge.AId, out Vector2 a) || !positions.TryGetValue(edge.BId, out Vector2 b))
            {
                continue;
            }

            CreateSocialGraphEdge(edge, a, b);
        }

        for (int i = 0; i < visibleWorkers.Count; i++)
        {
            DriverAgent worker = visibleWorkers[i];
            if (!positions.TryGetValue(worker.DriverId, out Vector2 position))
            {
                continue;
            }

            int degreeScore = degreeScores.TryGetValue(worker.DriverId, out int score) ? score : 0;
            CreateSocialGraphNode(worker, position, degreeScore, font, ru);
        }

        DriverAgent selected = GetDriverAgentById(selectedSocialGraphWorkerId);
        UpdateSocialGraphInspector(selected, edges, ru);
    }

    private List<DriverAgent> BuildSocialGraphVisibleWorkers()
    {
        List<DriverAgent> activeWorkers = new();
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (IsSocialGraphWorkerVisible(worker))
            {
                activeWorkers.Add(worker);
            }
        }

        if (activeWorkers.Count == 0)
        {
            selectedSocialGraphWorkerId = 0;
            return activeWorkers;
        }

        if (!IsSocialGraphWorkerIdVisible(selectedSocialGraphWorkerId))
        {
            selectedSocialGraphWorkerId = IsSocialGraphWorkerIdVisible(selectedWorkerPanelDriverId)
                ? selectedWorkerPanelDriverId
                : activeWorkers[0].DriverId;
        }

        activeWorkers.Sort((a, b) =>
        {
            int bScore = GetSocialGraphWorkerSortScore(b);
            int aScore = GetSocialGraphWorkerSortScore(a);
            int scoreCompare = bScore.CompareTo(aScore);
            if (scoreCompare != 0) return scoreCompare;
            return a.DriverId.CompareTo(b.DriverId);
        });

        List<DriverAgent> visibleWorkers = new();
        DriverAgent selected = GetDriverAgentById(selectedSocialGraphWorkerId);
        if (selected != null && IsSocialGraphWorkerVisible(selected))
        {
            visibleWorkers.Add(selected);
        }

        for (int i = 0; i < activeWorkers.Count && visibleWorkers.Count < SocialGraphMaxNodes; i++)
        {
            DriverAgent worker = activeWorkers[i];
            if (worker.DriverId == selectedSocialGraphWorkerId)
            {
                continue;
            }

            visibleWorkers.Add(worker);
        }

        return visibleWorkers;
    }

    private List<SocialGraphEdgeModel> BuildSocialGraphEdges(List<DriverAgent> workers)
    {
        HashSet<int> visibleIds = new();
        for (int i = 0; i < workers.Count; i++)
        {
            visibleIds.Add(workers[i].DriverId);
        }

        HashSet<long> seenEdges = new();
        List<SocialGraphEdgeModel> edges = new();
        for (int i = 0; i < workers.Count; i++)
        {
            DriverAgent worker = workers[i];
            for (int j = 0; j < worker.SocialMemories.Count; j++)
            {
                WorkerSocialMemory memory = worker.SocialMemories[j];
                if (memory == null || memory.Familiarity <= 0 || !visibleIds.Contains(memory.OtherWorkerId))
                {
                    continue;
                }

                int a = Mathf.Min(worker.DriverId, memory.OtherWorkerId);
                int b = Mathf.Max(worker.DriverId, memory.OtherWorkerId);
                long key = ((long)a << 32) | (uint)b;
                if (!seenEdges.Add(key))
                {
                    continue;
                }

                edges.Add(new SocialGraphEdgeModel
                {
                    AId = a,
                    BId = b,
                    Familiarity = memory.Familiarity,
                    Relationship = memory.Relationship,
                    InteractionCount = memory.InteractionCount,
                    Kind = memory.LastKind
                });
            }
        }

        edges.Sort((a, b) =>
        {
            int selectedCompare = IsSocialGraphEdgeSelected(b).CompareTo(IsSocialGraphEdgeSelected(a));
            if (selectedCompare != 0) return selectedCompare;
            int familiarityCompare = b.Familiarity.CompareTo(a.Familiarity);
            if (familiarityCompare != 0) return familiarityCompare;
            return b.InteractionCount.CompareTo(a.InteractionCount);
        });

        if (edges.Count > SocialGraphMaxEdges)
        {
            edges.RemoveRange(SocialGraphMaxEdges, edges.Count - SocialGraphMaxEdges);
        }

        return edges;
    }

    private Dictionary<int, Vector2> BuildSocialGraphPositions(List<DriverAgent> workers, Vector2 canvasSize)
    {
        Dictionary<int, Vector2> positions = new();
        float radiusX = Mathf.Max(210f, canvasSize.x * 0.40f);
        float radiusY = Mathf.Max(150f, canvasSize.y * 0.34f);
        bool hasSelected = selectedSocialGraphWorkerId > 0 && workers.Exists(w => w.DriverId == selectedSocialGraphWorkerId);
        int ringCount = hasSelected ? workers.Count - 1 : workers.Count;
        int ringIndex = 0;

        for (int i = 0; i < workers.Count; i++)
        {
            DriverAgent worker = workers[i];
            if (hasSelected && worker.DriverId == selectedSocialGraphWorkerId)
            {
                positions[worker.DriverId] = Vector2.zero;
                continue;
            }

            float t = ringCount <= 1 ? 0f : ringIndex / (float)ringCount;
            float angle = t * Mathf.PI * 2f - Mathf.PI * 0.5f;
            float wobble = 1f + 0.08f * Mathf.Sin((worker.DriverId + 3) * 1.91f);
            float x = Mathf.Cos(angle) * radiusX * wobble;
            float y = Mathf.Sin(angle) * radiusY * (1f + 0.05f * Mathf.Cos(worker.DriverId * 2.37f));

            if (ringIndex % 5 == 2)
            {
                x *= 0.72f;
                y *= 0.72f;
            }

            positions[worker.DriverId] = new Vector2(x, y);
            ringIndex++;
        }

        return positions;
    }

    private Dictionary<int, int> BuildSocialGraphDegreeScores(List<DriverAgent> workers)
    {
        Dictionary<int, int> scores = new();
        for (int i = 0; i < workers.Count; i++)
        {
            scores[workers[i].DriverId] = GetSocialGraphWorkerSortScore(workers[i]);
        }

        return scores;
    }

    private void CreateSocialGraphEdge(SocialGraphEdgeModel edge, Vector2 a, Vector2 b)
    {
        GameObject lineObject = CreateUiObject($"SocialGraphEdge_{edge.AId}_{edge.BId}", socialGraphScreenUi.GraphCanvas);
        RectTransform rect = lineObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        Image image = lineObject.AddComponent<Image>();
        image.raycastTarget = false;

        Vector2 delta = b - a;
        rect.anchoredPosition = (a + b) * 0.5f;
        rect.sizeDelta = new Vector2(delta.magnitude, Mathf.Lerp(1.6f, 4.5f, Mathf.Clamp01(edge.Familiarity / 100f)));
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        bool selectedEdge = IsSocialGraphEdgeSelected(edge);
        Color color = GetWorkerSocialRelationshipColor(edge.Relationship);
        if (edge.Kind == WorkerSocialInteractionKind.FamilyFormation)
        {
            color = FleetAccentColor;
        }

        color.a = selectedEdge || selectedSocialGraphWorkerId <= 0 ? 0.78f : 0.22f;
        image.color = color;
    }

    private void CreateSocialGraphNode(DriverAgent worker, Vector2 position, int degreeScore, Font font, bool ru)
    {
        bool selected = worker.DriverId == selectedSocialGraphWorkerId;
        bool neighbor = selected || IsSocialGraphNeighbor(worker.DriverId, selectedSocialGraphWorkerId);
        float size = selected
            ? 104f
            : Mathf.Lerp(58f, 88f, Mathf.Clamp01(degreeScore / 260f));

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
            : neighbor ? new Color(0.30f, 0.40f, 0.54f, 1f)
                       : new Color(0.16f, 0.19f, 0.25f, 0.92f);

        Outline outline = nodeObject.AddComponent<Outline>();
        outline.effectColor = selected ? FleetAccentColor : new Color(0f, 0f, 0f, 0.45f);
        outline.effectDistance = selected ? new Vector2(3f, -3f) : new Vector2(1f, -1f);

        Button button = nodeObject.AddComponent<Button>();
        button.targetGraphic = background;
        int workerId = worker.DriverId;
        button.onClick.AddListener(() =>
        {
            selectedSocialGraphWorkerId = workerId;
            isSocialGraphScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.75f);
        });

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
        label.color = neighbor ? Color.white : FleetMutedTextColor;
        label.raycastTarget = false;
        label.text = GetSocialGraphShortName(worker.DriverName, ru);
    }

    private void UpdateSocialGraphInspector(DriverAgent selected, List<SocialGraphEdgeModel> edges, bool ru)
    {
        if (selected == null)
        {
            socialGraphScreenUi.InspectorNameText.text = ru ? "\u041d\u0435\u0442 \u0432\u044b\u0431\u043e\u0440\u0430" : "No selection";
            socialGraphScreenUi.InspectorStatusText.text = string.Empty;
            socialGraphScreenUi.InspectorSummaryText.text = ru
                ? "\u0412\u044b\u0431\u0435\u0440\u0438 \u0443\u0437\u0435\u043b \u043d\u0430 \u0433\u0440\u0430\u0444\u0435, \u0447\u0442\u043e\u0431\u044b \u0443\u0432\u0438\u0434\u0435\u0442\u044c \u0431\u043b\u0438\u0436\u0430\u0439\u0448\u0438\u0435 \u0441\u0432\u044f\u0437\u0438."
                : "Select a node on the graph to inspect closest links.";
            socialGraphScreenUi.InspectorHintText.text = string.Empty;
            socialGraphScreenUi.OpenWorkerButton.interactable = false;
            socialGraphScreenUi.OpenWorkerButtonText.text = ru ? "\u041e\u0442\u043a\u0440\u044b\u0442\u044c \u0432 \u0420\u0430\u0431\u043e\u0447\u0438\u0435" : "Open in Workers";
            UpdateSocialGraphLinkRows(selected, ru);
            return;
        }

        socialGraphScreenUi.InspectorNameText.text = selected.DriverName;
        socialGraphScreenUi.InspectorStatusText.text = GetWorkerListStatusLabel(selected, ru);

        int linkCount = 0;
        int positive = 0;
        int tense = 0;
        int strongest = 0;
        for (int i = 0; i < selected.SocialMemories.Count; i++)
        {
            WorkerSocialMemory memory = selected.SocialMemories[i];
            if (memory == null || memory.Familiarity <= 0 || !IsSocialGraphWorkerIdVisible(memory.OtherWorkerId))
            {
                continue;
            }

            linkCount++;
            strongest = Mathf.Max(strongest, memory.Familiarity);
            if (memory.Relationship >= 20) positive++;
            if (memory.Relationship <= -20) tense++;
        }

        string familyText = selected.FamilyId > 0
            ? (ru ? "\u0421\u0435\u043c\u044c\u044f: \u0435\u0441\u0442\u044c" : "Family: formed")
            : (ru ? "\u0421\u0435\u043c\u044c\u044f: \u043d\u0435\u0442" : "Family: none");
        socialGraphScreenUi.InspectorSummaryText.text = ru
            ? $"Связей: {linkCount}\nПозитивных: {positive}   Напряжённых: {tense}\nСамая сильная: {strongest}\n{familyText}"
            : $"Links: {linkCount}\nPositive: {positive}   Tense: {tense}\nStrongest: {strongest}\n{familyText}";
        socialGraphScreenUi.InspectorHintText.text = ru
            ? "\u0420\u0430\u0437\u043c\u0435\u0440 \u0443\u0437\u043b\u0430 \u0437\u0430\u0432\u0438\u0441\u0438\u0442 \u043e\u0442 \u0441\u0438\u043b\u044b \u0441\u0432\u044f\u0437\u0435\u0439. \u0426\u0432\u0435\u0442 \u043b\u0438\u043d\u0438\u0438 \u043f\u043e\u043a\u0430\u0437\u044b\u0432\u0430\u0435\u0442 \u043d\u0430\u0441\u0442\u0440\u043e\u0439: \u043f\u043b\u044e\u0441, \u043d\u0435\u0439\u0442\u0440\u0430\u043b, \u043a\u043e\u043d\u0444\u043b\u0438\u043a\u0442."
            : "Node size follows link strength. Line color shows the relationship tone: positive, neutral, or conflict.";
        socialGraphScreenUi.OpenWorkerButton.interactable = true;
        socialGraphScreenUi.OpenWorkerButtonText.text = ru ? "\u041e\u0442\u043a\u0440\u044b\u0442\u044c \u0432 \u0420\u0430\u0431\u043e\u0447\u0438\u0435" : "Open in Workers";
        UpdateSocialGraphLinkRows(selected, ru);
    }

    private void UpdateSocialGraphLinkRows(DriverAgent selected, bool ru)
    {
        List<WorkerSocialMemory> memories = GetWorkerSocialMemoriesSorted(selected);
        for (int i = 0; i < socialGraphScreenUi.LinkButtons.Count; i++)
        {
            Button button = socialGraphScreenUi.LinkButtons[i];
            Text text = socialGraphScreenUi.LinkButtonTexts[i];
            bool active = selected != null && i < memories.Count;
            button.gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            WorkerSocialMemory memory = memories[i];
            DriverAgent other = GetDriverAgentById(memory.OtherWorkerId);
            string name = other != null ? other.DriverName : (ru ? "\u041d\u0435\u0442 \u0434\u0430\u043d\u043d\u044b\u0445" : "Unknown");
            text.text = $"{name}\n{GetWorkerSocialRelationshipLabel(memory.Relationship, ru)}  {memory.Familiarity}";
            text.color = GetWorkerSocialRelationshipColor(memory.Relationship);
            button.interactable = other != null;
        }
    }

    private void OnSocialGraphLinkRowPressed(int rowIndex)
    {
        DriverAgent selected = GetDriverAgentById(selectedSocialGraphWorkerId);
        if (selected == null)
        {
            return;
        }

        List<WorkerSocialMemory> memories = GetWorkerSocialMemoriesSorted(selected);
        if (rowIndex < 0 || rowIndex >= memories.Count)
        {
            return;
        }

        int nextWorkerId = memories[rowIndex].OtherWorkerId;
        if (!IsSocialGraphWorkerIdVisible(nextWorkerId))
        {
            return;
        }

        selectedSocialGraphWorkerId = nextWorkerId;
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

    private int GetSocialGraphWorkerSortScore(DriverAgent worker)
    {
        if (worker == null)
        {
            return 0;
        }

        int score = 0;
        for (int i = 0; i < worker.SocialMemories.Count; i++)
        {
            WorkerSocialMemory memory = worker.SocialMemories[i];
            if (memory == null || memory.Familiarity <= 0 || !IsSocialGraphWorkerIdVisible(memory.OtherWorkerId))
            {
                continue;
            }

            score += memory.Familiarity + Mathf.Abs(memory.Relationship) / 2 + memory.InteractionCount * 4;
        }

        if (worker.DriverId == selectedSocialGraphWorkerId)
        {
            score += 1000;
        }

        return score;
    }

    private bool IsSocialGraphNeighbor(int workerId, int selectedWorkerId)
    {
        if (workerId <= 0 || selectedWorkerId <= 0 || workerId == selectedWorkerId)
        {
            return workerId == selectedWorkerId;
        }

        DriverAgent worker = GetDriverAgentById(workerId);
        return FindWorkerSocialMemory(worker, selectedWorkerId)?.Familiarity > 0;
    }

    private bool IsSocialGraphEdgeSelected(SocialGraphEdgeModel edge)
    {
        return edge != null &&
               selectedSocialGraphWorkerId > 0 &&
               (edge.AId == selectedSocialGraphWorkerId || edge.BId == selectedSocialGraphWorkerId);
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
