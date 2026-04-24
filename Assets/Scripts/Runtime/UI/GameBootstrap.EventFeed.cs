using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int EventFeedMaxVisibleEntries = 5;
    private const float EventFeedEntryLifetime = 6.5f;
    private const float EventFeedFadeDuration = 0.45f;
    private const float EventFeedCardWidth = 240f;
    private const float EventFeedCardMinHeight = 48f;
    private const float EventFeedTopOffset = 138f;

    private enum FeedEventType
    {
        Info,
        Success,
        Warning,
        Money
    }

    private sealed class EventFeedEntryUi
    {
        public string EnText;
        public string RuText;
        public FeedEventType Type;
        public float RemainingTime;
        public float Lifetime;
        public int RepeatCount;
        public GameObject RootObject;
        public CanvasGroup CanvasGroup;
        public Image AccentImage;
        public Image HeaderBadgeImage;
        public Text HeaderBadgeText;
        public Text TimeText;
        public Text MessageText;
    }

    private RectTransform eventFeedRoot;
    private VerticalLayoutGroup eventFeedLayout;
    private readonly List<EventFeedEntryUi> eventFeedEntries = new();

    private void PushFeedEvent(string enText, string ruText, FeedEventType type)
    {
        if (string.IsNullOrWhiteSpace(enText) && string.IsNullOrWhiteSpace(ruText))
        {
            return;
        }

        SetupEventFeedUi();

        if (eventFeedEntries.Count > 0)
        {
            EventFeedEntryUi latest = eventFeedEntries[0];
            if (latest != null &&
                latest.Type == type &&
                latest.EnText == enText &&
                latest.RuText == ruText)
            {
                latest.RepeatCount++;
                latest.RemainingTime = latest.Lifetime;
                RefreshEventFeedEntry(latest);
                return;
            }
        }

        EventFeedEntryUi entry = CreateEventFeedEntry(enText, ruText, type);
        eventFeedEntries.Insert(0, entry);
        entry.RootObject.transform.SetAsFirstSibling();
        RefreshEventFeedEntry(entry);

        while (eventFeedEntries.Count > EventFeedMaxVisibleEntries)
        {
            RemoveEventFeedEntry(eventFeedEntries[eventFeedEntries.Count - 1]);
        }
    }

    private float _feedDebugTimer;

    private void UpdateEventFeedUi()
    {
        if (isMainMenuOpen || isLoadingWorld || isRacingActive)
        {
            SetEventFeedVisible(false);
            ClearEventFeedEntries();
            return;
        }

        SetupEventFeedUi();
        SetEventFeedVisible(true);

        _feedDebugTimer -= Time.unscaledDeltaTime;
        if (_feedDebugTimer <= 0f && eventFeedRoot != null)
        {
            _feedDebugTimer = 2f;
            Vector2 pos  = eventFeedRoot.anchoredPosition;
            Vector2 size = eventFeedRoot.rect.size;
            Vector2 anch = eventFeedRoot.anchorMin;
            Vector3 world = eventFeedRoot.position;
            Debug.Log($"[EventFeed] anchor=({anch.x:F2},{anch.y:F2})  anchoredPos=({pos.x:F0},{pos.y:F0})  rectSize=({size.x:F0}×{size.y:F0})  worldPos=({world.x:F0},{world.y:F0})  entries={eventFeedEntries.Count}");
        }

        float dt = Time.unscaledDeltaTime;
        for (int i = eventFeedEntries.Count - 1; i >= 0; i--)
        {
            EventFeedEntryUi entry = eventFeedEntries[i];
            if (entry == null)
            {
                eventFeedEntries.RemoveAt(i);
                continue;
            }

            entry.RemainingTime -= dt;
            RefreshEventFeedEntry(entry);
            if (entry.RemainingTime <= 0f)
            {
                RemoveEventFeedEntry(entry);
            }
        }
    }

    private void SetupEventFeedUi()
    {
        if (eventFeedRoot != null)
        {
            return;
        }

        EnsureFleetEventSystem();
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new("EventFeedCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.anchoredPosition = Vector2.zero;
        canvasRect.sizeDelta = Vector2.zero;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        eventFeedRoot = CreateUiObject("EventFeedRoot", canvasObject.transform).GetComponent<RectTransform>();
        eventFeedRoot.anchorMin = new Vector2(1f, 0.5f);
        eventFeedRoot.anchorMax = new Vector2(1f, 0.5f);
        eventFeedRoot.pivot = new Vector2(1f, 1f);
        eventFeedRoot.sizeDelta = new Vector2(EventFeedCardWidth, 540f);
        // Right edge; pivot top-right so entries grow downward from 240 px above center.
        eventFeedRoot.anchoredPosition = new Vector2(0f, 340f);

        eventFeedLayout = eventFeedRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        eventFeedLayout.childAlignment = TextAnchor.UpperRight;
        eventFeedLayout.childControlWidth = true;
        eventFeedLayout.childControlHeight = false;
        eventFeedLayout.childForceExpandWidth = true;
        eventFeedLayout.childForceExpandHeight = false;
        eventFeedLayout.spacing = 10f;
        eventFeedLayout.padding = new RectOffset(0, 0, 0, 0);

        ContentSizeFitter fitter = eventFeedRoot.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        eventFeedRoot.gameObject.SetActive(false);
    }

    private EventFeedEntryUi CreateEventFeedEntry(string enText, string ruText, FeedEventType type)
    {
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject cardObject = CreateUiObject("FeedEntry", eventFeedRoot);
        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        LayoutElement layout = cardObject.AddComponent<LayoutElement>();
        layout.minWidth = EventFeedCardWidth;
        layout.preferredWidth = EventFeedCardWidth;
        layout.flexibleWidth = 0f;
        layout.minHeight = EventFeedCardMinHeight;
        Image background = cardObject.AddComponent<Image>();
        background.color = new Color(0.09f, 0.11f, 0.16f, 0.94f);
        Outline outline = cardObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.26f);
        outline.effectDistance = new Vector2(1f, -1f);
        CanvasGroup canvasGroup = cardObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        HorizontalLayoutGroup rowLayout = cardObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(0, 0, 0, 0);
        rowLayout.spacing = 0f;
        rowLayout.childAlignment = TextAnchor.UpperLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        ContentSizeFitter cardFitter = cardObject.AddComponent<ContentSizeFitter>();
        cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        RectTransform accent = CreateUiObject("Accent", cardRect).GetComponent<RectTransform>();
        LayoutElement accentLayout = accent.gameObject.AddComponent<LayoutElement>();
        accentLayout.preferredWidth = 6f;
        accentLayout.minWidth = 6f;
        accentLayout.flexibleWidth = 0f;
        Image accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.color = GetFeedEventAccentColor(type);

        RectTransform content = CreateUiObject("Content", cardRect).GetComponent<RectTransform>();
        LayoutElement contentLayout = content.gameObject.AddComponent<LayoutElement>();
        contentLayout.minWidth = EventFeedCardWidth - 6f;
        contentLayout.preferredWidth = EventFeedCardWidth - 6f;
        contentLayout.flexibleWidth = 1f;
        VerticalLayoutGroup contentGroup = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentGroup.padding = new RectOffset(8, 8, 5, 5);
        contentGroup.spacing = 3f;
        contentGroup.childControlWidth = true;
        contentGroup.childControlHeight = false;
        contentGroup.childForceExpandWidth = true;
        contentGroup.childForceExpandHeight = false;

        RectTransform metaRow = CreateUiObject("MetaRow", content).GetComponent<RectTransform>();
        HorizontalLayoutGroup metaLayout = metaRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        metaLayout.padding = new RectOffset(0, 0, 0, 0);
        metaLayout.spacing = 8f;
        metaLayout.childAlignment = TextAnchor.MiddleLeft;
        metaLayout.childControlWidth = false;
        metaLayout.childControlHeight = true;
        metaLayout.childForceExpandWidth = false;
        metaLayout.childForceExpandHeight = false;
        metaRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;

        RectTransform badgeRoot = CreateUiObject("HeaderBadge", metaRow).GetComponent<RectTransform>();
        LayoutElement badgeLayout = badgeRoot.gameObject.AddComponent<LayoutElement>();
        badgeLayout.minWidth = 72f;
        badgeLayout.preferredWidth = 72f;
        badgeLayout.preferredHeight = 16f;
        Image badgeImage = badgeRoot.gameObject.AddComponent<Image>();
        badgeImage.color = GetFeedEventAccentColor(type);
        Outline badgeOutline = badgeRoot.gameObject.AddComponent<Outline>();
        badgeOutline.effectColor = new Color(0f, 0f, 0f, 0.22f);
        badgeOutline.effectDistance = new Vector2(1f, -1f);
        Text badgeText = CreateHeaderText("HeaderBadgeText", badgeRoot, uiFont, string.Empty, 11, TextAnchor.MiddleCenter, Color.white);

        Text timeText = CreateBodyText("Time", metaRow, uiFont, string.Empty, 11, TextAnchor.MiddleLeft, FleetMutedTextColor);
        LayoutElement timeLayout = timeText.gameObject.AddComponent<LayoutElement>();
        timeLayout.preferredHeight = 14f;
        timeLayout.flexibleWidth = 1f;
        timeText.horizontalOverflow = HorizontalWrapMode.Overflow;
        timeText.verticalOverflow = VerticalWrapMode.Truncate;
        Text messageText = CreateBodyText("Message", content, uiFont, string.Empty, 12, TextAnchor.UpperLeft, Color.white);
        messageText.supportRichText = true;
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow = VerticalWrapMode.Truncate;
        messageText.lineSpacing = 1.05f;
        LayoutElement messageLayout = messageText.gameObject.AddComponent<LayoutElement>();
        messageLayout.preferredHeight = 20f;
        Shadow messageShadow = messageText.gameObject.AddComponent<Shadow>();
        messageShadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        messageShadow.effectDistance = new Vector2(1f, -1f);

        return new EventFeedEntryUi
        {
            EnText = enText,
            RuText = ruText,
            Type = type,
            RemainingTime = EventFeedEntryLifetime,
            Lifetime = EventFeedEntryLifetime,
            RepeatCount = 1,
            RootObject = cardObject,
            CanvasGroup = canvasGroup,
            AccentImage = accentImage,
            HeaderBadgeImage = badgeImage,
            HeaderBadgeText = badgeText,
            TimeText = timeText,
            MessageText = messageText
        };
    }

    private void RefreshEventFeedEntry(EventFeedEntryUi entry)
    {
        if (entry == null || entry.RootObject == null)
        {
            return;
        }

        string body = IsRussianLanguage() && !string.IsNullOrWhiteSpace(entry.RuText) ? entry.RuText : entry.EnText;
        if (entry.RepeatCount > 1)
        {
            body = $"{body} <color=#{ColorUtility.ToHtmlStringRGB(FleetAccentColor)}>x{entry.RepeatCount}</color>";
        }

        entry.TimeText.text = GetDayNightClockLabel();
        if (entry.HeaderBadgeText != null)
        {
            entry.HeaderBadgeText.text = GetFeedEventTypeLabel(entry.Type);
        }
        if (entry.HeaderBadgeImage != null)
        {
            entry.HeaderBadgeImage.color = GetFeedEventAccentColor(entry.Type);
        }
        entry.MessageText.text = body;
        entry.AccentImage.color = GetFeedEventAccentColor(entry.Type);

        float alpha = entry.RemainingTime > EventFeedFadeDuration
            ? 1f
            : Mathf.Clamp01(entry.RemainingTime / EventFeedFadeDuration);
        entry.CanvasGroup.alpha = alpha;
    }

    private void RemoveEventFeedEntry(EventFeedEntryUi entry)
    {
        if (entry == null)
        {
            return;
        }

        eventFeedEntries.Remove(entry);
        if (entry.RootObject != null)
        {
            Destroy(entry.RootObject);
        }
    }

    private void ClearEventFeedEntries()
    {
        if (eventFeedEntries.Count == 0)
        {
            return;
        }

        for (int i = eventFeedEntries.Count - 1; i >= 0; i--)
        {
            EventFeedEntryUi entry = eventFeedEntries[i];
            if (entry?.RootObject != null)
            {
                Destroy(entry.RootObject);
            }
        }

        eventFeedEntries.Clear();
    }

    private void SetEventFeedVisible(bool visible)
    {
        if (eventFeedRoot != null && eventFeedRoot.gameObject.activeSelf != visible)
        {
            eventFeedRoot.gameObject.SetActive(visible);
        }
    }

    private static Color GetFeedEventAccentColor(FeedEventType type)
    {
        return type switch
        {
            FeedEventType.Success => new Color(0.35f, 0.78f, 0.42f, 1f),
            FeedEventType.Warning => new Color(0.93f, 0.65f, 0.22f, 1f),
            FeedEventType.Money => new Color(0.94f, 0.78f, 0.25f, 1f),
            _ => new Color(0.42f, 0.63f, 0.88f, 1f)
        };
    }

    private string GetFeedEventTypeLabel(FeedEventType type)
    {
        bool ru = IsRussianLanguage();
        return type switch
        {
            FeedEventType.Success => ru ? "УСПЕХ" : "SUCCESS",
            FeedEventType.Warning => ru ? "ВНИМАНИЕ" : "WARNING",
            FeedEventType.Money => ru ? "ДЕНЬГИ" : "MONEY",
            _ => ru ? "СОБЫТИЕ" : "EVENT"
        };
    }
}
