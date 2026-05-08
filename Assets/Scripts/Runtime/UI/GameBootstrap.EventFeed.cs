using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const int EventFeedMaxVisibleEntries = 3;
    private const float EventFeedEntryLifetime = 4.8f;
    private const float EventFeedFadeDuration = 0.35f;
    private const float EventFeedRowWidth = 390f;
    private const float EventFeedRowHeight = 28f;

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
        public Image BadgeImage;
        public Text BadgeText;
        public Text TimeText;
        public Text MessageText;
    }

    private RectTransform eventFeedRoot;
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

        GameObject canvasObject = new("EventFeedCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.pivot = new Vector2(0.5f, 0.5f);
        canvasRect.anchoredPosition = Vector2.zero;
        canvasRect.sizeDelta = Vector2.zero;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        eventFeedRoot = CreateUiObject("EventFeedRoot", canvasObject.transform).GetComponent<RectTransform>();
        eventFeedRoot.anchorMin = new Vector2(1f, 1f);
        eventFeedRoot.anchorMax = new Vector2(1f, 1f);
        eventFeedRoot.pivot = new Vector2(1f, 1f);
        eventFeedRoot.sizeDelta = new Vector2(EventFeedRowWidth, EventFeedMaxVisibleEntries * (EventFeedRowHeight + 2f));
        eventFeedRoot.anchoredPosition = new Vector2(-14f, -54f);

        VerticalLayoutGroup layout = eventFeedRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperRight;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 2f;
        layout.padding = new RectOffset(0, 0, 0, 0);

        eventFeedRoot.gameObject.SetActive(false);
    }

    private EventFeedEntryUi CreateEventFeedEntry(string enText, string ruText, FeedEventType type)
    {
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform row = CreateUiObject("FeedEntry", eventFeedRoot).GetComponent<RectTransform>();
        row.gameObject.AddComponent<RectMask2D>();
        LayoutElement layout = row.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = EventFeedRowWidth;
        layout.preferredWidth = EventFeedRowWidth;
        layout.minHeight = EventFeedRowHeight;
        layout.preferredHeight = EventFeedRowHeight;

        Image background = row.gameObject.AddComponent<Image>();
        background.color = new Color(0.045f, 0.060f, 0.080f, 0.58f);
        background.raycastTarget = false;
        CanvasGroup canvasGroup = row.gameObject.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(7, 9, 2, 2);
        rowLayout.spacing = 6f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        RectTransform accent = CreateUiObject("Accent", row).GetComponent<RectTransform>();
        LayoutElement accentLayout = accent.gameObject.AddComponent<LayoutElement>();
        accentLayout.preferredWidth = 2f;
        accentLayout.minWidth = 2f;
        Image accentImage = accent.gameObject.AddComponent<Image>();
        accentImage.color = GetFeedEventAccentColor(type);
        accentImage.raycastTarget = false;

        Text badgeText = CreateHeaderText("TypeText", row, uiFont, string.Empty, 12, TextAnchor.MiddleCenter, GetFeedEventAccentColor(type));
        LayoutElement badgeLayout = badgeText.gameObject.AddComponent<LayoutElement>();
        badgeLayout.preferredWidth = 18f;
        badgeText.horizontalOverflow = HorizontalWrapMode.Overflow;
        badgeText.verticalOverflow = VerticalWrapMode.Truncate;
        badgeText.raycastTarget = false;

        Text messageText = CreateBodyText("Message", row, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, new Color(0.94f, 0.96f, 0.99f, 1f));
        messageText.supportRichText = true;
        messageText.horizontalOverflow = HorizontalWrapMode.Overflow;
        messageText.verticalOverflow = VerticalWrapMode.Truncate;
        messageText.raycastTarget = false;
        LayoutElement messageLayout = messageText.gameObject.AddComponent<LayoutElement>();
        messageLayout.minWidth = 120f;
        messageLayout.flexibleWidth = 1f;

        return new EventFeedEntryUi
        {
            EnText = enText,
            RuText = ruText,
            Type = type,
            RemainingTime = EventFeedEntryLifetime,
            Lifetime = EventFeedEntryLifetime,
            RepeatCount = 1,
            RootObject = row.gameObject,
            CanvasGroup = canvasGroup,
            AccentImage = accentImage,
            BadgeImage = null,
            BadgeText = badgeText,
            TimeText = null,
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

        Color accent = GetFeedEventAccentColor(entry.Type);
        if (entry.TimeText != null)
        {
            entry.TimeText.text = GetDayNightClockLabel();
        }
        entry.BadgeText.text = GetFeedEventTypeLabel(entry.Type);
        entry.BadgeText.color = accent;
        if (entry.BadgeImage != null)
        {
            entry.BadgeImage.color = accent;
        }
        entry.AccentImage.color = accent;
        entry.MessageText.text = body;

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
        return type switch
        {
            FeedEventType.Success => "OK",
            FeedEventType.Warning => "!",
            FeedEventType.Money => "$",
            _ => "i"
        };
    }
}
