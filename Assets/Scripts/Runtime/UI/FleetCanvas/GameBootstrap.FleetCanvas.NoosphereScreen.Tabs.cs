using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int NoosphereOverviewRowCount = 4;
    private const int NoosphereKnowledgeStateRowCount = 36;
    private const int NoosphereSignalRowCount = 32;

    private enum NoosphereScreenTab
    {
        Overview,
        Knowledge,
        Signals,
        Journal,
        Visual
    }

    private enum NoosphereKnowledgeStateKind
    {
        Canonized,
        Forming,
        Contested
    }

    private sealed class NoosphereTextRowUi
    {
        public RectTransform Root;
        public Image Background;
        public Image Accent;
        public Text BadgeText;
        public Text TitleText;
        public Text BodyText;
        public Text MetaText;
    }

    private sealed class NoosphereKnowledgeStateRow
    {
        public NoosphereKnowledgeStateKind Kind;
        public WorkerMemory Memory;
        public CityKnowledgeCanonEntry CanonEntry;
        public int Count;
        public int Required;
        public int PositiveCount;
        public int NegativeCount;
        public int ScoreTotal;
        public int ConfidenceTotal;
        public int Score;
        public int Confidence;
        public int LastDay;
        public float LastWorldHour;
    }

    private void SetupNoosphereScreenTabs(RectTransform tabRow, Font font)
    {
        noosphereScreenUi.OverviewTabButton = CreateNoosphereTabButton(
            "NoosphereOverviewTab",
            tabRow,
            font,
            NoosphereScreenTab.Overview,
            out noosphereScreenUi.OverviewTabText);
        noosphereScreenUi.KnowledgeTabButton = CreateNoosphereTabButton(
            "NoosphereKnowledgeTab",
            tabRow,
            font,
            NoosphereScreenTab.Knowledge,
            out noosphereScreenUi.KnowledgeTabText);
        noosphereScreenUi.SignalsTabButton = CreateNoosphereTabButton(
            "NoosphereSignalsTab",
            tabRow,
            font,
            NoosphereScreenTab.Signals,
            out noosphereScreenUi.SignalsTabText);
        noosphereScreenUi.JournalTabButton = CreateNoosphereTabButton(
            "NoosphereJournalTab",
            tabRow,
            font,
            NoosphereScreenTab.Journal,
            out noosphereScreenUi.JournalTabText);
        noosphereScreenUi.VisualTabButton = CreateNoosphereTabButton(
            "NoosphereVisualTab",
            tabRow,
            font,
            NoosphereScreenTab.Visual,
            out noosphereScreenUi.VisualTabText);
    }

    private Button CreateNoosphereTabButton(
        string name,
        RectTransform parent,
        Font font,
        NoosphereScreenTab tab,
        out Text label)
    {
        Button button = CreateButton(
            name,
            parent,
            font,
            out label,
            string.Empty,
            13,
            new Color(0.08f, 0.10f, 0.14f, 1f),
            Color.white);
        ConfigureNoosphereTabButton(button, label);
        button.onClick.AddListener(() => SetNoosphereScreenTab(tab));
        return button;
    }

    private static void ConfigureNoosphereTabButton(Button button, Text label)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.targetGraphic as Image ?? button.GetComponent<Image>();
        if (image != null)
        {
            image.raycastTarget = true;
            button.targetGraphic = image;
        }

        if (label != null)
        {
            label.fontStyle = FontStyle.Bold;
            label.raycastTarget = false;
        }

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = button.gameObject.AddComponent<LayoutElement>();
        }

        layout.minHeight = 38f;
        layout.preferredHeight = 38f;
        layout.flexibleWidth = 1f;
        layout.flexibleHeight = 0f;

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.None;
        button.navigation = navigation;
    }

    private void SetNoosphereScreenTab(NoosphereScreenTab tab)
    {
        if (activeNoosphereScreenTab == tab)
        {
            return;
        }

        activeNoosphereScreenTab = tab;
        isNoosphereScreenDirty = true;
        noosphereVisualDirty = true;
        ResetNoosphereActiveTabScroll();
        PlayUiSound(uiPanelOpenClip, 0.42f);
        UpdateNoosphereScreenUi();
    }

    private void ResetNoosphereActiveTabScroll()
    {
        ScrollRect scroll = activeNoosphereScreenTab switch
        {
            NoosphereScreenTab.Knowledge => noosphereScreenUi?.KnowledgeScrollRect,
            NoosphereScreenTab.Signals => noosphereScreenUi?.SignalsScrollRect,
            NoosphereScreenTab.Journal => noosphereScreenUi?.ScrollRect,
            _ => null
        };

        if (scroll != null)
        {
            scroll.verticalNormalizedPosition = 1f;
        }
    }

    private void ApplyNoosphereScreenTabVisuals(bool ru)
    {
        SetNoosphereTabButtonState(noosphereScreenUi.OverviewTabButton, noosphereScreenUi.OverviewTabText, NoosphereScreenTab.Overview, ru);
        SetNoosphereTabButtonState(noosphereScreenUi.KnowledgeTabButton, noosphereScreenUi.KnowledgeTabText, NoosphereScreenTab.Knowledge, ru);
        SetNoosphereTabButtonState(noosphereScreenUi.SignalsTabButton, noosphereScreenUi.SignalsTabText, NoosphereScreenTab.Signals, ru);
        SetNoosphereTabButtonState(noosphereScreenUi.JournalTabButton, noosphereScreenUi.JournalTabText, NoosphereScreenTab.Journal, ru);
        SetNoosphereTabButtonState(noosphereScreenUi.VisualTabButton, noosphereScreenUi.VisualTabText, NoosphereScreenTab.Visual, ru);
    }

    private void SetNoosphereTabButtonState(Button button, Text label, NoosphereScreenTab tab, bool ru)
    {
        if (label != null)
        {
            label.text = GetNoosphereTabLabel(tab, ru);
        }

        ApplyShiftsTabVisual(button, label, activeNoosphereScreenTab == tab);
    }

    private static string GetNoosphereTabLabel(NoosphereScreenTab tab, bool ru)
    {
        return tab switch
        {
            NoosphereScreenTab.Knowledge => ru ? "Знания" : "Knowledge",
            NoosphereScreenTab.Signals => ru ? "Сигналы" : "Signals",
            NoosphereScreenTab.Journal => ru ? "Журнал" : "Journal",
            NoosphereScreenTab.Visual => ru ? "Схема" : "Map",
            _ => ru ? "Обзор" : "Overview"
        };
    }

    private void ApplyNoosphereScreenTabVisibility()
    {
        SetNoosphereRootVisible(noosphereScreenUi.OverviewRoot, activeNoosphereScreenTab == NoosphereScreenTab.Overview);
        SetNoosphereRootVisible(noosphereScreenUi.KnowledgeRoot, activeNoosphereScreenTab == NoosphereScreenTab.Knowledge);
        SetNoosphereRootVisible(noosphereScreenUi.SignalsRoot, activeNoosphereScreenTab == NoosphereScreenTab.Signals);
        SetNoosphereRootVisible(noosphereScreenUi.JournalRoot, activeNoosphereScreenTab == NoosphereScreenTab.Journal);
        SetNoosphereRootVisible(noosphereScreenUi.VisualPanelRoot, activeNoosphereScreenTab == NoosphereScreenTab.Visual);
    }

    private static void SetNoosphereRootVisible(RectTransform root, bool visible)
    {
        if (root != null && root.gameObject.activeSelf != visible)
        {
            root.gameObject.SetActive(visible);
        }
    }

    private void RebuildNoosphereActiveTab(
        bool ru,
        float now,
        int activeKnowledgeCount,
        int receivedCount,
        int burnedCount,
        int canonizedCount,
        int socialSignalCount)
    {
        switch (activeNoosphereScreenTab)
        {
            case NoosphereScreenTab.Knowledge:
                RebuildNoosphereKnowledgeTab(ru, now);
                break;
            case NoosphereScreenTab.Signals:
                RebuildNoosphereSignalsTab(ru, socialSignalCount);
                break;
            case NoosphereScreenTab.Journal:
                RebuildNoosphereJournalTab(ru, socialSignalCount);
                break;
            case NoosphereScreenTab.Visual:
                noosphereVisualDirty = true;
                break;
            default:
                RebuildNoosphereOverviewTab(ru, now, activeKnowledgeCount, receivedCount, burnedCount, canonizedCount, socialSignalCount);
                break;
        }
    }

    private void SetupNoosphereOverviewTab(RectTransform bodyRoot, Font font)
    {
        RectTransform root = CreateNoosphereTabRoot("NoosphereOverviewRoot", bodyRoot);
        noosphereScreenUi.OverviewRoot = root;

        VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 14, 14);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        noosphereScreenUi.OverviewLeadText = CreateHeaderText("NoosphereOverviewLead", root, font, string.Empty, 18, TextAnchor.MiddleLeft, Color.white);
        noosphereScreenUi.OverviewLeadText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        for (int i = 0; i < NoosphereOverviewRowCount; i++)
        {
            noosphereScreenUi.OverviewRows.Add(CreateNoosphereTextRow(root, font, $"NoosphereOverviewRow{i + 1}", 112f));
        }
    }

    private void SetupNoosphereKnowledgeTab(RectTransform bodyRoot, Font font)
    {
        RectTransform root = CreateNoosphereTabRoot("NoosphereKnowledgeRoot", bodyRoot);
        noosphereScreenUi.KnowledgeRoot = root;
        SetupNoosphereListTabLayout(root, new RectOffset(14, 14, 14, 14));
        noosphereScreenUi.KnowledgeTitleText = CreateHeaderText("NoosphereKnowledgeTitle", root, font, string.Empty, 17, TextAnchor.MiddleLeft, Color.white);
        noosphereScreenUi.KnowledgeTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        noosphereScreenUi.KnowledgeEmptyText = CreateBodyText("NoosphereKnowledgeEmpty", root, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetMutedTextColor);
        noosphereScreenUi.KnowledgeEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;

        FleetCanvasUiFactory.ScrollPanelRefs scroll = CreateVerticalScrollList(
            "NoosphereKnowledgeScroll",
            root,
            "NoosphereKnowledgeRows",
            8f,
            34f,
            flexibleHeight: 1f);
        AddTransparentScrollRaycast(scroll.Root);
        noosphereScreenUi.KnowledgeScrollRect = scroll.ScrollRect;
        for (int i = 0; i < NoosphereKnowledgeStateRowCount; i++)
        {
            noosphereScreenUi.KnowledgeRows.Add(CreateNoosphereTextRow(scroll.Content, font, $"NoosphereKnowledgeStateRow{i + 1}", 92f));
        }
    }

    private void SetupNoosphereSignalsTab(RectTransform bodyRoot, Font font)
    {
        RectTransform root = CreateNoosphereTabRoot("NoosphereSignalsRoot", bodyRoot);
        noosphereScreenUi.SignalsRoot = root;
        SetupNoosphereListTabLayout(root, new RectOffset(14, 14, 14, 14));
        noosphereScreenUi.SignalsTitleText = CreateHeaderText("NoosphereSignalsTitle", root, font, string.Empty, 17, TextAnchor.MiddleLeft, Color.white);
        noosphereScreenUi.SignalsTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;
        noosphereScreenUi.SignalsEmptyText = CreateBodyText("NoosphereSignalsEmpty", root, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetMutedTextColor);
        noosphereScreenUi.SignalsEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;

        FleetCanvasUiFactory.ScrollPanelRefs scroll = CreateVerticalScrollList(
            "NoosphereSignalsScroll",
            root,
            "NoosphereSignalRows",
            8f,
            34f,
            flexibleHeight: 1f);
        AddTransparentScrollRaycast(scroll.Root);
        noosphereScreenUi.SignalsScrollRect = scroll.ScrollRect;
        for (int i = 0; i < NoosphereSignalRowCount; i++)
        {
            noosphereScreenUi.SignalsRows.Add(CreateNoosphereTextRow(scroll.Content, font, $"NoosphereSignalRow{i + 1}", 86f));
        }
    }

    private void SetupNoosphereJournalTab(RectTransform bodyRoot, Font font)
    {
        RectTransform root = CreateNoosphereTabRoot("NoosphereJournalRoot", bodyRoot);
        noosphereScreenUi.JournalRoot = root;
        SetupNoosphereListTabLayout(root, new RectOffset(14, 14, 14, 14));

        noosphereScreenUi.JournalTitleText = CreateHeaderText("NoosphereListTitle", root, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        noosphereScreenUi.JournalTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        noosphereScreenUi.EmptyText = CreateBodyText(
            "NoosphereEmpty",
            root,
            font,
            string.Empty,
            13,
            TextAnchor.MiddleLeft,
            FleetMutedTextColor);
        noosphereScreenUi.EmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 38f;

        FleetCanvasUiFactory.ScrollPanelRefs scroll = CreateVerticalScrollList(
            "NoosphereScroll",
            root,
            "NoosphereRows",
            8f,
            34f,
            flexibleHeight: 1f);
        AddTransparentScrollRaycast(scroll.Root);
        noosphereScreenUi.ScrollRect = scroll.ScrollRect;

        for (int i = 0; i < NoosphereKnowledgeLogCap; i++)
        {
            noosphereScreenUi.Rows.Add(CreateNoosphereLogRow(scroll.Content, font, i));
        }
    }

    private void SetupNoosphereVisualTab(RectTransform bodyRoot, Font font)
    {
        RectTransform visualPanel = CreateNoosphereTabRoot("NoosphereVisualPanel", bodyRoot);
        noosphereScreenUi.VisualPanelRoot = visualPanel;
        SetupNoosphereVisualPanelUi(visualPanel, font);
    }

    private RectTransform CreateNoosphereTabRoot(string name, RectTransform bodyRoot)
    {
        RectTransform root = CreateStyledPanel(name, bodyRoot, FleetInsetColor);
        StretchRect(root, 0f, 0f, 0f, 0f);
        root.gameObject.SetActive(false);
        return root;
    }

    private static void SetupNoosphereListTabLayout(RectTransform root, RectOffset padding)
    {
        VerticalLayoutGroup layout = root.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = padding;
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void AddTransparentScrollRaycast(RectTransform root)
    {
        Image scrollRaycast = root.gameObject.AddComponent<Image>();
        scrollRaycast.color = new Color(0f, 0f, 0f, 0f);
        scrollRaycast.raycastTarget = true;
    }

    private NoosphereTextRowUi CreateNoosphereTextRow(RectTransform parent, Font font, string name, float preferredHeight)
    {
        NoosphereTextRowUi row = new();
        row.Root = CreateStyledPanel(name, parent, new Color(0.050f, 0.075f, 0.110f, 0.94f));
        row.Root.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        row.Background = row.Root.GetComponent<Image>();
        if (row.Background != null)
        {
            row.Background.raycastTarget = false;
        }

        HorizontalLayoutGroup layout = row.Root.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 12, 10, 10);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        RectTransform accentRoot = CreateUiObject("Accent", row.Root).GetComponent<RectTransform>();
        accentRoot.gameObject.AddComponent<LayoutElement>().preferredWidth = 4f;
        row.Accent = accentRoot.gameObject.AddComponent<Image>();
        row.Accent.raycastTarget = false;

        row.BadgeText = CreateHeaderText("Badge", row.Root, font, string.Empty, 12, TextAnchor.MiddleCenter, FleetAccentColor);
        row.BadgeText.raycastTarget = false;
        row.BadgeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 68f;

        RectTransform textStack = CreateVerticalStack("TextStack", row.Root, new RectOffset(), 4f, flexibleWidth: 1f);
        row.TitleText = CreateHeaderText("Title", textStack, font, string.Empty, 15, TextAnchor.MiddleLeft, Color.white);
        row.TitleText.raycastTarget = false;
        row.TitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.TitleText.verticalOverflow = VerticalWrapMode.Truncate;
        row.TitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 21f;

        row.BodyText = CreateBodyText("Body", textStack, font, string.Empty, 13, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        row.BodyText.raycastTarget = false;
        row.BodyText.supportRichText = true;
        row.BodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.BodyText.verticalOverflow = VerticalWrapMode.Truncate;
        row.BodyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        row.MetaText = CreateBodyText("Meta", textStack, font, string.Empty, 12, TextAnchor.MiddleLeft, FleetMutedTextColor);
        row.MetaText.raycastTarget = false;
        row.MetaText.horizontalOverflow = HorizontalWrapMode.Wrap;
        row.MetaText.verticalOverflow = VerticalWrapMode.Truncate;
        row.MetaText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;
        row.Root.gameObject.SetActive(false);
        return row;
    }

    private static void ApplyNoosphereTextRow(
        NoosphereTextRowUi row,
        string badge,
        Color accent,
        string title,
        string body,
        string meta,
        Color background)
    {
        row.Root.gameObject.SetActive(true);
        row.Accent.color = accent;
        row.BadgeText.text = badge;
        row.BadgeText.color = accent;
        row.TitleText.text = title;
        row.BodyText.text = body;
        row.MetaText.text = meta;
        row.Background.color = background;
    }

    private static void HideNoosphereTextRows(List<NoosphereTextRowUi> rows, int startIndex)
    {
        for (int i = startIndex; i < rows.Count; i++)
        {
            rows[i].Root.gameObject.SetActive(false);
        }
    }

    private bool IsNoosphereVisualTabVisible()
    {
        return activeNoosphereScreenTab == NoosphereScreenTab.Visual &&
               noosphereScreenUi?.VisualPanelRoot != null &&
               noosphereScreenUi.VisualPanelRoot.gameObject.activeInHierarchy;
    }
}
