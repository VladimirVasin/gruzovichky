using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int NoosphereKnowledgeLogCap = 150;
    private const float NoosphereKnowledgeExpiryBarWidth = 104f;
    private const float NoosphereTimerUiRefreshSeconds = 0.25f;

    private bool isNoospherePanelOpen;
    private bool isNoosphereScreenDirty = true;
    private float noosphereTimerUiRefreshAccumulator;
    private readonly List<NoosphereKnowledgeLogEntry> noosphereKnowledgeLog = new();
    private NoosphereScreenUiRefs noosphereScreenUi;

    private sealed class NoosphereScreenUiRefs
    {
        public GameObject CanvasRoot;
        public RectTransform WindowRoot;
        public Text TitleText;
        public Text SummaryText;
        public Text EmptyText;
        public ScrollRect ScrollRect;
        public Button CloseButton;
        public readonly List<NoosphereLogRowUi> Rows = new();
    }

    private sealed class NoosphereLogRowUi
    {
        public RectTransform Root;
        public Image Background;
        public Image Accent;
        public Text TimeText;
        public Text BadgeText;
        public Text TitleText;
        public Text BodyText;
        public Text ReasonText;
        public RectTransform ExpiryRoot;
        public RectTransform ExpiryFillRect;
        public Image ExpiryFillImage;
        public Text ExpiryLabelText;
        public Text ExpiryText;
    }

    private void RecordNoosphereKnowledgeReceived(DriverAgent owner, DriverAgent other, WorkerMemory memory, float now)
    {
        string reasonRu = !string.IsNullOrWhiteSpace(memory?.SourceRu)
            ? memory.SourceRu
            : memory?.Kind == WorkerMemoryKind.BuildingExistence
                ? memory.SourceRu
                : "\u0420\u0430\u0442\u0443\u0448\u0430: \u0442\u0435\u043c\u0430 \u0437\u043d\u0430\u043a\u043e\u043c\u0441\u0442\u0432\u0430 \u043e\u0442 \u0438\u0433\u0440\u043e\u043a\u0430";
        string reasonEn = !string.IsNullOrWhiteSpace(memory?.SourceEn)
            ? memory.SourceEn
            : memory?.Kind == WorkerMemoryKind.BuildingExistence
                ? memory.SourceEn
                : "City Hall: player-provided introduction topic";
        RecordNoosphereKnowledgeEvent(
            NoosphereKnowledgeEventKind.Received,
            owner,
            other,
            memory,
            reasonRu,
            reasonEn,
            now);
    }

    private void RecordNoosphereKnowledgeBurned(DriverAgent owner, WorkerMemory memory, string reasonRu, string reasonEn, float now)
    {
        if (memory == null)
        {
            return;
        }

        RecordNoosphereKnowledgeEvent(
            NoosphereKnowledgeEventKind.Burned,
            owner,
            GetDriverAgentById(memory.OtherWorkerId),
            memory,
            reasonRu,
            reasonEn,
            now);
    }

    private void RecordNoosphereKnowledgeExpired(DriverAgent owner, WorkerMemory memory, float now)
    {
        RecordNoosphereKnowledgeBurned(
            owner,
            memory,
            "\u0418\u0441\u0442\u0451\u043a \u0442\u0430\u0439\u043c\u0435\u0440 48\u0447",
            "48h timer expired",
            now);
    }

    private void RecordNoosphereKnowledgeForgottenByLimit(DriverAgent owner, WorkerMemory memory, float now)
    {
        RecordNoosphereKnowledgeBurned(
            owner,
            memory,
            "\u0412\u044b\u0442\u0435\u0441\u043d\u0435\u043d\u043e \u043b\u0438\u043c\u0438\u0442\u043e\u043c \u043b\u0438\u0447\u043d\u043e\u0439 \u043f\u0430\u043c\u044f\u0442\u0438",
            "Forgotten because the personal memory list is full",
            now);
    }

    private void RecordNoosphereKnowledgeEvent(
        NoosphereKnowledgeEventKind eventKind,
        DriverAgent owner,
        DriverAgent other,
        WorkerMemory memory,
        string reasonRu,
        string reasonEn,
        float now)
    {
        if (!IsWorkerMemoryDisplayable(memory))
        {
            return;
        }

        NoosphereKnowledgeLogEntry entry = new()
        {
            EventKind = eventKind,
            MemoryKind = memory.Kind,
            OwnerWorkerId = owner?.DriverId ?? 0,
            OtherWorkerId = memory.OtherWorkerId,
            OwnerName = GetNoosphereWorkerName(owner, owner?.DriverId ?? 0),
            OtherName = GetNoosphereWorkerName(other, memory.OtherWorkerId),
            Topic = GetWorkerRumorTopic(memory),
            BuildingType = memory.BuildingType,
            BuildingInstanceId = memory.BuildingInstanceId,
            BuildingLabel = memory.Kind == WorkerMemoryKind.BuildingExistence
                ? GetWorkerKnowledgeBuildingDisplayName(memory, IsRussianLanguage())
                : string.Empty,
            Positive = memory.Positive,
            ReasonRu = reasonRu ?? string.Empty,
            ReasonEn = reasonEn ?? string.Empty,
            KnowledgeIteration = GetWorkerKnowledgeIteration(memory),
            SourceAttitude = memory.SourceAttitude,
            OpinionTone = memory.OpinionTone,
            OpinionScore = memory.OpinionScore,
            OpinionConfidence = memory.OpinionConfidence,
            OpinionReasonRu = memory.OpinionReasonRu ?? string.Empty,
            OpinionReasonEn = memory.OpinionReasonEn ?? string.Empty,
            EventDay = currentDay,
            EventWorldHour = now,
            MemoryCreatedWorldHour = memory.CreatedWorldHour,
            MemoryExpiresWorldHour = GetWorkerMemoryExpiresWorldHour(memory)
        };
        CopyWorkerRumorState(entry, memory);

        noosphereKnowledgeLog.Insert(0, entry);
        while (noosphereKnowledgeLog.Count > NoosphereKnowledgeLogCap)
        {
            noosphereKnowledgeLog.RemoveAt(noosphereKnowledgeLog.Count - 1);
        }

        isNoosphereScreenDirty = true;
    }

    private static string GetNoosphereWorkerName(DriverAgent worker, int fallbackId)
    {
        if (worker != null && !string.IsNullOrWhiteSpace(worker.DriverName))
        {
            return worker.DriverName;
        }

        return fallbackId > 0 ? $"#{fallbackId}" : "\u0416\u0438\u0442\u0435\u043b\u044c";
    }

    private void SetupNoosphereScreenUi()
    {
        if (noosphereScreenUi != null)
        {
            return;
        }

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        noosphereScreenUi = new NoosphereScreenUiRefs();

        GameObject canvasObject = new("NoosphereScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        noosphereScreenUi.CanvasRoot = canvasObject;

        RectTransform window = CreateStyledPanel("NoosphereWindowRoot", canvasObject.transform, FleetPanelColor);
        SetCenteredWindow(window, 1120f, 650f, -12f);
        noosphereScreenUi.WindowRoot = window;

        VerticalLayoutGroup windowLayout = window.gameObject.AddComponent<VerticalLayoutGroup>();
        windowLayout.padding = new RectOffset(18, 18, 18, 18);
        windowLayout.spacing = 12f;
        windowLayout.childControlWidth = true;
        windowLayout.childControlHeight = true;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("NoosphereHeaderRow", window, 36f, 10f);
        noosphereScreenUi.TitleText = CreateHeaderText(
            "NoosphereTitle",
            headerRow,
            font,
            string.Empty,
            26,
            TextAnchor.MiddleLeft,
            Color.white);
        noosphereScreenUi.TitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        noosphereScreenUi.SummaryText = CreateBodyText(
            "NoosphereSummary",
            window,
            font,
            string.Empty,
            13,
            TextAnchor.MiddleLeft,
            FleetSecondaryTextColor);
        noosphereScreenUi.SummaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        noosphereScreenUi.SummaryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        RectTransform listPanel = CreateStyledPanel("NoosphereListPanel", window, FleetInsetColor);
        listPanel.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        VerticalLayoutGroup listLayout = listPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.padding = new RectOffset(12, 12, 12, 12);
        listLayout.spacing = 8f;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;

        Text listTitle = CreateHeaderText("NoosphereListTitle", listPanel, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        listTitle.text = IsRussianLanguage() ? "\u0416\u0443\u0440\u043d\u0430\u043b \u0437\u043d\u0430\u043d\u0438\u0439" : "Knowledge log";
        listTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        noosphereScreenUi.EmptyText = CreateBodyText(
            "NoosphereEmpty",
            listPanel,
            font,
            string.Empty,
            13,
            TextAnchor.MiddleLeft,
            FleetMutedTextColor);
        noosphereScreenUi.EmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;

        FleetCanvasUiFactory.ScrollPanelRefs scroll = CreateVerticalScrollList(
            "NoosphereScroll",
            listPanel,
            "NoosphereRows",
            8f,
            34f,
            flexibleHeight: 1f);
        Image scrollRaycast = scroll.Root.gameObject.AddComponent<Image>();
        scrollRaycast.color = new Color(0f, 0f, 0f, 0f);
        scrollRaycast.raycastTarget = true;
        noosphereScreenUi.ScrollRect = scroll.ScrollRect;

        for (int i = 0; i < NoosphereKnowledgeLogCap; i++)
        {
            noosphereScreenUi.Rows.Add(CreateNoosphereLogRow(scroll.Content, font, i));
        }

        AddOverlayCloseButton(window, font);
        noosphereScreenUi.CloseButton = FindNoosphereCloseButton(window);
        ValidateNoosphereScreenClickTargets();
        noosphereScreenUi.CanvasRoot.SetActive(false);
        UpdateNoosphereScreenUi();
    }

    private NoosphereLogRowUi CreateNoosphereLogRow(RectTransform parent, Font font, int index)
    {
        NoosphereLogRowUi row = new();
        row.Root = CreateStyledPanel($"NoosphereLogRow{index + 1}", parent, new Color(0.050f, 0.075f, 0.110f, 0.94f));
        row.Root.gameObject.AddComponent<LayoutElement>().preferredHeight = 86f;
        row.Background = row.Root.GetComponent<Image>();
        if (row.Background != null)
        {
            row.Background.raycastTarget = false;
        }

        HorizontalLayoutGroup rowLayout = row.Root.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(0, 12, 10, 10);
        rowLayout.spacing = 10f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        RectTransform accentRoot = CreateUiObject("Accent", row.Root).GetComponent<RectTransform>();
        accentRoot.gameObject.AddComponent<LayoutElement>().preferredWidth = 4f;
        row.Accent = accentRoot.gameObject.AddComponent<Image>();
        row.Accent.raycastTarget = false;

        row.TimeText = CreateBodyText("Time", row.Root, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        row.TimeText.raycastTarget = false;
        row.TimeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 84f;

        row.BadgeText = CreateHeaderText("Badge", row.Root, font, string.Empty, 12, TextAnchor.MiddleCenter, FleetAccentColor);
        row.BadgeText.raycastTarget = false;
        row.BadgeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 54f;

        RectTransform textStack = CreateVerticalStack("NoosphereLogText", row.Root, new RectOffset(), 3f, flexibleWidth: 1f);
        row.TitleText = CreateHeaderText("Title", textStack, font, string.Empty, 15, TextAnchor.MiddleLeft, Color.white);
        row.TitleText.raycastTarget = false;
        row.TitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.TitleText.verticalOverflow = VerticalWrapMode.Truncate;
        row.TitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        row.BodyText = CreateBodyText("Body", textStack, font, string.Empty, 13, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        row.BodyText.raycastTarget = false;
        row.BodyText.supportRichText = true;
        row.BodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.BodyText.verticalOverflow = VerticalWrapMode.Truncate;
        row.BodyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        row.ReasonText = CreateBodyText("Reason", textStack, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        row.ReasonText.raycastTarget = false;
        row.ReasonText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.ReasonText.verticalOverflow = VerticalWrapMode.Truncate;
        row.ReasonText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        row.ExpiryRoot = CreateVerticalStack(
            "Timer",
            row.Root,
            new RectOffset(0, 0, 1, 1),
            4f,
            preferredWidth: 112f,
            flexibleWidth: 0f);

        row.ExpiryLabelText = CreateBodyText("TimerLabel", row.ExpiryRoot, font, string.Empty, 10, TextAnchor.MiddleRight, FleetMutedTextColor);
        row.ExpiryLabelText.raycastTarget = false;
        row.ExpiryLabelText.gameObject.AddComponent<LayoutElement>().preferredHeight = 13f;

        RectTransform expiryBar = CreateUiObject("TimerBar", row.ExpiryRoot).GetComponent<RectTransform>();
        LayoutElement expiryBarLayout = expiryBar.gameObject.AddComponent<LayoutElement>();
        expiryBarLayout.preferredWidth = NoosphereKnowledgeExpiryBarWidth;
        expiryBarLayout.preferredHeight = 7f;
        expiryBarLayout.minWidth = NoosphereKnowledgeExpiryBarWidth;
        expiryBarLayout.minHeight = 7f;
        Image expiryBarBackground = expiryBar.gameObject.AddComponent<Image>();
        expiryBarBackground.color = new Color(0.08f, 0.10f, 0.14f, 1f);
        expiryBarBackground.raycastTarget = false;

        row.ExpiryFillRect = CreateUiObject("TimerFill", expiryBar).GetComponent<RectTransform>();
        row.ExpiryFillRect.anchorMin = new Vector2(0f, 0f);
        row.ExpiryFillRect.anchorMax = new Vector2(0f, 1f);
        row.ExpiryFillRect.pivot = new Vector2(0f, 0.5f);
        row.ExpiryFillRect.anchoredPosition = Vector2.zero;
        row.ExpiryFillRect.sizeDelta = new Vector2(NoosphereKnowledgeExpiryBarWidth, 0f);
        row.ExpiryFillImage = row.ExpiryFillRect.gameObject.AddComponent<Image>();
        row.ExpiryFillImage.color = FleetAccentColor;
        row.ExpiryFillImage.raycastTarget = false;

        row.ExpiryText = CreateBodyText("TimerText", row.ExpiryRoot, font, string.Empty, 12, TextAnchor.MiddleRight, FleetMutedTextColor);
        row.ExpiryText.raycastTarget = false;
        row.ExpiryText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.ExpiryText.verticalOverflow = VerticalWrapMode.Truncate;
        row.ExpiryText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        row.Root.gameObject.SetActive(false);
        return row;
    }

    private void UpdateNoosphereScreenUi()
    {
        if (noosphereScreenUi == null)
        {
            if (isNoospherePanelOpen)
            {
                SetupNoosphereScreenUi();
            }

            return;
        }

        bool shouldShow = isNoospherePanelOpen;
        if (noosphereScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            noosphereScreenUi.CanvasRoot.SetActive(shouldShow);
            if (shouldShow)
            {
                isNoosphereScreenDirty = true;
                noosphereTimerUiRefreshAccumulator = 0f;
                if (noosphereScreenUi.ScrollRect != null)
                {
                    noosphereScreenUi.ScrollRect.verticalNormalizedPosition = 1f;
                }
            }
            else
            {
                noosphereTimerUiRefreshAccumulator = 0f;
            }
        }

        if (shouldShow && !isNoosphereScreenDirty)
        {
            noosphereTimerUiRefreshAccumulator += Time.unscaledDeltaTime;
            if (noosphereTimerUiRefreshAccumulator >= NoosphereTimerUiRefreshSeconds)
            {
                noosphereTimerUiRefreshAccumulator = 0f;
                isNoosphereScreenDirty = true;
            }
        }

        if (!shouldShow || !isNoosphereScreenDirty)
        {
            return;
        }

        RebuildNoosphereScreen();
        isNoosphereScreenDirty = false;
    }

    private void RebuildNoosphereScreen()
    {
        bool ru = IsRussianLanguage();
        float now = GetCurrentWorldHour();
        int activeKnowledgeCount = CountActiveNoosphereKnowledge(now);
        int burnedCount = CountNoosphereEvents(NoosphereKnowledgeEventKind.Burned);
        int receivedCount = CountNoosphereEvents(NoosphereKnowledgeEventKind.Received);

        noosphereScreenUi.TitleText.text = ru ? "\u041d\u043e\u043e\u0441\u0444\u0435\u0440\u0430" : "Noosphere";
        noosphereScreenUi.SummaryText.text = ru
            ? $"\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u043e\u0439 \u0441\u043b\u0435\u0434 \u0437\u043d\u0430\u043d\u0438\u0439: \u0430\u043a\u0442\u0438\u0432\u043d\u043e {activeKnowledgeCount}, \u043f\u043e\u043b\u0443\u0447\u0435\u043d\u043e {receivedCount}, \u0441\u0433\u043e\u0440\u0435\u043b\u043e {burnedCount}. \u0417\u0434\u0435\u0441\u044c \u0432\u0438\u0434\u043d\u043e, \u043a\u0442\u043e \u0447\u0442\u043e \u0443\u0437\u043d\u0430\u043b, \u043e\u0442 \u043a\u043e\u0433\u043e \u0438 \u043f\u043e\u0447\u0435\u043c\u0443 \u0437\u043d\u0430\u043d\u0438\u0435 \u0438\u0441\u0447\u0435\u0437\u043b\u043e."
            : $"City knowledge trace: active {activeKnowledgeCount}, received {receivedCount}, burned {burnedCount}. This shows who learned what, from whom, and why knowledge disappeared.";

        noosphereScreenUi.EmptyText.gameObject.SetActive(noosphereKnowledgeLog.Count == 0);
        noosphereScreenUi.EmptyText.text = ru
            ? "\u041f\u043e\u043a\u0430 \u043d\u0435\u0442 \u0441\u043e\u0431\u044b\u0442\u0438\u0439. \u041d\u043e\u043e\u0441\u0444\u0435\u0440\u0430 \u043c\u043e\u043b\u0447\u0438\u0442, \u043a\u0430\u043a \u0430\u0440\u0445\u0438\u0432 \u0434\u043e \u043f\u0435\u0440\u0432\u043e\u0439 \u043f\u0430\u043f\u043a\u0438."
            : "No knowledge events yet.";

        for (int i = 0; i < noosphereScreenUi.Rows.Count; i++)
        {
            NoosphereLogRowUi row = noosphereScreenUi.Rows[i];
            bool visible = i < noosphereKnowledgeLog.Count;
            row.Root.gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            ApplyNoosphereLogRow(row, noosphereKnowledgeLog[i], ru);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(noosphereScreenUi.WindowRoot);
    }

    private void ApplyNoosphereLogRow(NoosphereLogRowUi row, NoosphereKnowledgeLogEntry entry, bool ru)
    {
        float now = GetCurrentWorldHour();
        Color accent = GetNoosphereEventColor(entry.EventKind);
        row.Accent.color = accent;
        row.BadgeText.color = accent;
        row.BadgeText.text = GetNoosphereBadgeText(entry.EventKind, ru);
        row.TimeText.text = FormatNoosphereEventTime(entry, ru);
        row.TitleText.text = GetNoosphereEventTitle(entry, ru);
        row.BodyText.text = BuildNoosphereEventBody(entry, ru);
        row.ReasonText.text = BuildNoosphereEventReason(entry, ru);
        ApplyNoosphereExpiryIndicator(row, entry, now, ru);
        row.Background.color = entry.EventKind == NoosphereKnowledgeEventKind.Received
            ? new Color(0.050f, 0.115f, 0.105f, 0.94f)
            : new Color(0.130f, 0.070f, 0.070f, 0.94f);
    }

    private int CountActiveNoosphereKnowledge(float now)
    {
        int count = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (worker == null)
            {
                continue;
            }

            for (int j = 0; j < worker.Memories.Count; j++)
            {
                WorkerMemory memory = worker.Memories[j];
                if (IsWorkerMemoryDisplayable(memory) &&
                    !ShouldExpireWorkerMemory(memory, now))
                {
                    count++;
                }
            }
        }

        return count;
    }

    private int CountNoosphereEvents(NoosphereKnowledgeEventKind kind)
    {
        int count = 0;
        for (int i = 0; i < noosphereKnowledgeLog.Count; i++)
        {
            if (noosphereKnowledgeLog[i]?.EventKind == kind)
            {
                count++;
            }
        }

        return count;
    }

    private static void ApplyNoosphereExpiryIndicator(NoosphereLogRowUi row, NoosphereKnowledgeLogEntry entry, float now, bool ru)
    {
        if (row?.ExpiryRoot == null)
        {
            return;
        }

        bool hasTimer = entry != null &&
                        entry.MemoryCreatedWorldHour >= 0f &&
                        entry.MemoryExpiresWorldHour > entry.MemoryCreatedWorldHour;
        row.ExpiryRoot.gameObject.SetActive(hasTimer);
        if (!hasTimer)
        {
            return;
        }

        bool burned = entry.EventKind == NoosphereKnowledgeEventKind.Burned;
        float lifetimeHours = Mathf.Max(0.01f, entry.MemoryExpiresWorldHour - entry.MemoryCreatedWorldHour);
        float remainingHours = burned ? 0f : Mathf.Max(0f, entry.MemoryExpiresWorldHour - now);
        float freshness = Mathf.Clamp01(remainingHours / lifetimeHours);
        Color expiryColor = Color.Lerp(new Color(0.95f, 0.34f, 0.24f, 1f), new Color(1f, 0.82f, 0.29f, 1f), freshness);

        if (row.ExpiryLabelText != null)
        {
            row.ExpiryLabelText.text = ru ? "\u0421\u0440\u043e\u043a" : "TTL";
            row.ExpiryLabelText.color = burned || remainingHours <= 0f ? expiryColor : FleetMutedTextColor;
        }

        if (row.ExpiryFillRect != null)
        {
            row.ExpiryFillRect.sizeDelta = new Vector2(NoosphereKnowledgeExpiryBarWidth * freshness, 0f);
        }

        if (row.ExpiryFillImage != null)
        {
            row.ExpiryFillImage.color = expiryColor;
        }

        if (row.ExpiryText != null)
        {
            row.ExpiryText.text = burned || remainingHours <= 0f
                ? (ru ? "\u0441\u0433\u043e\u0440\u0435\u043b\u043e" : "burned")
                : FormatNoosphereRemainingTime(remainingHours, ru);
            row.ExpiryText.color = freshness <= 0.25f ? expiryColor : FleetMutedTextColor;
        }
    }

    private static string FormatNoosphereRemainingTime(float remainingHours, bool ru)
    {
        int hours = Mathf.CeilToInt(Mathf.Max(0f, remainingHours));
        if (hours <= 0)
        {
            return ru ? "\u0441\u0433\u043e\u0440\u0430\u0435\u0442" : "fading";
        }

        return ru ? $"{hours}\u0447" : $"{hours}h";
    }

    private static string BuildNoosphereEventBody(NoosphereKnowledgeLogEntry entry, bool ru)
    {
        string owner = $"<b>{SanitizeRichTextLiteral(entry.OwnerName)}</b>";
        if (entry.MemoryKind == WorkerMemoryKind.BuildingExistence)
        {
            string buildingLabel = string.IsNullOrWhiteSpace(entry.BuildingLabel)
                ? (ru ? "\u043f\u043e\u0441\u0442\u0440\u043e\u0439\u043a\u0430" : "building")
                : entry.BuildingLabel;
            string building = $"<color=#74D7FF><b>{SanitizeRichTextLiteral(buildingLabel)}</b></color>";
            if (entry.EventKind == NoosphereKnowledgeEventKind.Burned)
            {
                return ru
                    ? $"{owner} \u0437\u0430\u0431\u044b\u043b, \u0447\u0442\u043e \u0432 \u0433\u043e\u0440\u043e\u0434\u0435 \u0435\u0441\u0442\u044c {building}."
                    : $"{owner} forgot that {building} exists in town.";
            }

            return ru
                ? $"{owner} \u043f\u043e\u043d\u044f\u043b, \u0447\u0442\u043e \u0432 \u0433\u043e\u0440\u043e\u0434\u0435 \u0435\u0441\u0442\u044c {building}."
                : $"{owner} learned that {building} exists in town.";
        }

        string other = $"<b>{SanitizeRichTextLiteral(entry.OtherName)}</b>";
        string topic = FormatCitySocialTopicRichText(GetWorkerRumorTopic(entry));

        if (entry.EventKind == NoosphereKnowledgeEventKind.Burned)
        {
            return ru
                ? $"{owner} \u043f\u043e\u0442\u0435\u0440\u044f\u043b \u0437\u043d\u0430\u043d\u0438\u0435 \u043e \u0442\u0435\u043c\u0435 \u00ab{topic}\u00bb, \u0441\u0432\u044f\u0437\u0430\u043d\u043d\u043e\u0439 \u0441 {other}."
                : $"{owner} lost knowledge of \"{topic}\" tied to {other}.";
        }

        return ru
            ? $"{owner} \u0437\u0430\u043f\u043e\u043c\u043d\u0438\u043b \u0442\u0435\u043c\u0443 \u00ab{topic}\u00bb \u043f\u043e\u0441\u043b\u0435 \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u0430 \u0441 {other}."
            : $"{owner} remembered \"{topic}\" after talking with {other}.";
    }

    private static string BuildNoosphereEventReason(NoosphereKnowledgeLogEntry entry, bool ru)
    {
        string reason = ru ? entry.ReasonRu : entry.ReasonEn;
        if (string.IsNullOrWhiteSpace(reason))
        {
            reason = ru ? "\u043f\u0440\u0438\u0447\u0438\u043d\u0430 \u043d\u0435 \u0443\u043a\u0430\u0437\u0430\u043d\u0430" : "no reason recorded";
        }

        if (entry.MemoryKind == WorkerMemoryKind.BuildingExistence)
        {
            return ru
                ? $"\u0418\u0442\u0435\u0440\u0430\u0446\u0438\u044f {GetNoosphereKnowledgeIteration(entry)}; \u041f\u0440\u0438\u0447\u0438\u043d\u0430: {reason}; {FormatNoosphereKnowledgeOpinion(entry, ru)}"
                : $"Iteration {GetNoosphereKnowledgeIteration(entry)}; Reason: {reason}; {FormatNoosphereKnowledgeOpinion(entry, ru)}";
        }

        string outcome = entry.Positive
            ? (ru ? "\u0438\u0441\u0445\u043e\u0434: \u0442\u0435\u043c\u0430 \u0441\u0440\u0430\u0431\u043e\u0442\u0430\u043b\u0430" : "outcome: topic worked")
            : (ru ? "\u0438\u0441\u0445\u043e\u0434: \u0442\u0435\u043c\u0430 \u0431\u044b\u043b\u0430 \u043d\u0435\u043b\u043e\u0432\u043a\u043e\u0439" : "outcome: topic felt awkward");
        string rumorState = FormatWorkerRumorStateMeta(entry, ru);
        string originalTopic = GetWorkerRumorOriginalTopic(entry);
        string currentTopic = GetWorkerRumorTopic(entry);
        string originalMeta = !string.IsNullOrWhiteSpace(originalTopic) &&
                              NormalizeWorkerKnowledgeTopicKey(originalTopic) != NormalizeWorkerKnowledgeTopicKey(currentTopic)
            ? ru ? $"; \u0418\u0441\u0445\u043e\u0434\u043d\u043e: \u00ab{FormatCitySocialTopicRichText(originalTopic)}\u00bb" : $"; Original: \"{FormatCitySocialTopicRichText(originalTopic)}\""
            : string.Empty;
        return ru
            ? $"\u0418\u0442\u0435\u0440\u0430\u0446\u0438\u044f {GetNoosphereKnowledgeIteration(entry)}; {rumorState}; {FormatWorkerKnowledgeSourceAttitudeMeta(entry.SourceAttitude, ru)}{originalMeta}; \u041f\u0440\u0438\u0447\u0438\u043d\u0430: {reason}; {outcome}; {FormatNoosphereKnowledgeOpinion(entry, ru)}"
            : $"Iteration {GetNoosphereKnowledgeIteration(entry)}; {rumorState}; {FormatWorkerKnowledgeSourceAttitudeMeta(entry.SourceAttitude, ru)}{originalMeta}; Reason: {reason}; {outcome}; {FormatNoosphereKnowledgeOpinion(entry, ru)}";
    }

    private static string FormatNoosphereKnowledgeOpinion(NoosphereKnowledgeLogEntry entry, bool ru)
    {
        string opinion = entry?.OpinionTone switch
        {
            WorkerKnowledgeOpinionTone.Positive => ru ? "\u043c\u043d\u0435\u043d\u0438\u0435: \u043f\u043e\u043b\u0435\u0437\u043d\u043e" : "opinion: useful",
            WorkerKnowledgeOpinionTone.Negative => ru ? "\u043c\u043d\u0435\u043d\u0438\u0435: \u0441\u043e\u043c\u043d\u0438\u0442\u0435\u043b\u044c\u043d\u043e" : "opinion: doubtful",
            _ => ru ? "\u043c\u043d\u0435\u043d\u0438\u0435: \u043d\u0435\u0439\u0442\u0440\u0430\u043b\u044c\u043d\u043e" : "opinion: neutral"
        };
        int confidence = Mathf.Clamp(entry?.OpinionConfidence ?? 0, 0, 100);
        string reason = ru ? entry?.OpinionReasonRu : entry?.OpinionReasonEn;
        if (string.IsNullOrWhiteSpace(reason))
        {
            return confidence > 0
                ? $"{opinion}, {confidence}%"
                : opinion;
        }

        return confidence > 0
            ? $"{opinion}, {confidence}%, {reason}"
            : $"{opinion}, {reason}";
    }

    private static int GetNoosphereKnowledgeIteration(NoosphereKnowledgeLogEntry entry)
    {
        return Mathf.Max(1, entry?.KnowledgeIteration ?? 0);
    }

    private static string FormatNoosphereEventTime(NoosphereKnowledgeLogEntry entry, bool ru)
    {
        if (entry == null)
        {
            return string.Empty;
        }

        int day = entry.EventDay > 0 ? entry.EventDay : Mathf.FloorToInt(entry.EventWorldHour / 24f) + 1;
        int hour = Mathf.FloorToInt(Mathf.Repeat(entry.EventWorldHour, 24f));
        return ru ? $"\u0414{day} {hour:00}:00" : $"D{day} {hour:00}:00";
    }

    private static string GetNoosphereEventTitle(NoosphereKnowledgeLogEntry entry, bool ru)
    {
        if (entry?.MemoryKind == WorkerMemoryKind.BuildingExistence)
        {
            return entry.EventKind == NoosphereKnowledgeEventKind.Burned
                ? (ru ? "\u0417\u043d\u0430\u043d\u0438\u0435 \u043e \u043f\u043e\u0441\u0442\u0440\u043e\u0439\u043a\u0435 \u0441\u0433\u043e\u0440\u0435\u043b\u043e" : "Building knowledge burned")
                : (ru ? "\u041d\u043e\u0432\u0430\u044f \u043f\u043e\u0441\u0442\u0440\u043e\u0439\u043a\u0430 \u0432 \u043f\u0430\u043c\u044f\u0442\u0438" : "Building discovered");
        }

        return entry != null && entry.EventKind == NoosphereKnowledgeEventKind.Burned
            ? (ru ? "\u0417\u043d\u0430\u043d\u0438\u0435 \u0441\u0433\u043e\u0440\u0435\u043b\u043e" : "Knowledge burned")
            : (ru ? "\u041f\u043e\u043b\u0443\u0447\u0435\u043d\u043e \u0437\u043d\u0430\u043d\u0438\u0435" : "Knowledge received");
    }

    private static string GetNoosphereBadgeText(NoosphereKnowledgeEventKind kind, bool ru)
    {
        return kind == NoosphereKnowledgeEventKind.Burned
            ? (ru ? "\u0421\u0413\u041e\u0420" : "OUT")
            : (ru ? "\u041d\u041e\u0412" : "NEW");
    }

    private static Color GetNoosphereEventColor(NoosphereKnowledgeEventKind kind)
    {
        return kind == NoosphereKnowledgeEventKind.Burned
            ? new Color(0.95f, 0.36f, 0.25f, 1f)
            : new Color(0.50f, 0.86f, 0.42f, 1f);
    }

    private static Button FindNoosphereCloseButton(RectTransform root)
    {
        if (root == null)
        {
            return null;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.name == "OverlayCloseButton")
            {
                return buttons[i];
            }
        }

        return null;
    }

    private void ValidateNoosphereScreenClickTargets()
    {
        if (noosphereScreenUi?.CanvasRoot == null)
        {
            return;
        }

        bool ok = noosphereScreenUi.CanvasRoot.GetComponent<GraphicRaycaster>() != null &&
                  IsButtonClickTargetReady(noosphereScreenUi.CloseButton) &&
                  noosphereScreenUi.ScrollRect != null &&
                  noosphereScreenUi.ScrollRect.content != null;

        if (!ok)
        {
            SessionDebugLogger.Log("UI_INPUT", "Noosphere click-target validation failed: check GraphicRaycaster, close button, and scroll content.");
        }
    }
}
