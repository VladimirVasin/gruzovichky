using System;
using UnityEngine;
using UnityEngine.UI;

public static class FleetCanvasUiFactory
{
    public sealed class ScrollPanelRefs
    {
        public RectTransform Root;
        public ScrollRect ScrollRect;
        public RectTransform Viewport;
        public RectTransform Content;
    }

    public static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static RectTransform CreateStyledPanel(string name, Transform parent, Color color)
    {
        GameObject go = CreateUiObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = color;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.18f);
        outline.effectDistance = new Vector2(1f, -1f);
        return go.GetComponent<RectTransform>();
    }

    public static RectTransform CreateLayoutRow(string name, Transform parent, float preferredHeight, float spacing)
    {
        RectTransform row = CreateUiObject(name, parent).GetComponent<RectTransform>();
        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        return row;
    }

    public static RectTransform CreateSpacer(
        string name,
        Transform parent,
        float preferredWidth = 0f,
        float preferredHeight = 0f,
        float flexibleWidth = 0f,
        float flexibleHeight = 0f)
    {
        RectTransform spacer = CreateUiObject(name, parent).GetComponent<RectTransform>();
        LayoutElement element = spacer.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = preferredWidth;
        element.preferredHeight = preferredHeight;
        element.flexibleWidth = flexibleWidth;
        element.flexibleHeight = flexibleHeight;
        return spacer;
    }

    public static RectTransform CreateTabRow(string name, Transform parent, float preferredHeight, float spacing)
    {
        RectTransform row = CreateLayoutRow(name, parent, preferredHeight, spacing);
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement element = row.GetComponent<LayoutElement>();
        element.minHeight = preferredHeight;
        element.flexibleHeight = 0f;
        return row;
    }

    public static ScrollPanelRefs CreateVerticalScrollPanel(
        string name,
        Transform parent,
        Color viewportTint,
        float inset,
        float spacing,
        float scrollSensitivity = 28f)
    {
        GameObject rootObj = CreateUiObject(name, parent);
        RectTransform root = rootObj.GetComponent<RectTransform>();
        StretchRect(root, inset, inset, inset, inset);
        rootObj.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);

        ScrollRect scrollRect = rootObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = scrollSensitivity;

        GameObject viewportObj = CreateUiObject("Viewport", rootObj.transform);
        RectTransform viewport = viewportObj.GetComponent<RectTransform>();
        StretchRect(viewport, 0f, 0f, 0f, 0f);
        Image viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = viewportTint;
        viewportImage.raycastTarget = true;
        viewportObj.AddComponent<Mask>().showMaskGraphic = false;

        GameObject contentObj = CreateUiObject("Content", viewportObj.transform);
        RectTransform content = contentObj.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = spacing;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewport;
        scrollRect.content = content;
        return new ScrollPanelRefs
        {
            Root = root,
            ScrollRect = scrollRect,
            Viewport = viewport,
            Content = content
        };
    }

    public static Text CreateBodyText(
        string name,
        Transform parent,
        Font font,
        string value,
        int fontSize,
        TextAnchor alignment,
        Color color,
        Func<string, string> localize)
    {
        GameObject go = CreateUiObject(name, parent);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.text = localize != null ? localize(value) : value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    public static RectTransform CreateSectionCard(
        string name,
        Transform parent,
        Font font,
        string title,
        Color cardColor,
        Color titleColor,
        Func<string, string> localize,
        out RectTransform body,
        bool addTitle = true)
    {
        RectTransform card = CreateStyledPanel(name, parent, cardColor);
        VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(16, 16, 14, 16);
        cardLayout.spacing = 12;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;
        if (addTitle && !string.IsNullOrEmpty(title))
        {
            Text header = CreateBodyText("Header", card, font, title, 18, TextAnchor.MiddleLeft, titleColor, localize);
            header.fontStyle = FontStyle.Bold;
        }

        body = CreateUiObject("Body", card).GetComponent<RectTransform>();
        VerticalLayoutGroup bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.spacing = 8;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;
        return card;
    }

    public static Button CreateButton(
        string name,
        Transform parent,
        Font font,
        out Text label,
        string buttonText,
        int fontSize,
        Color normalColor,
        Color textColor,
        Func<string, string> localize)
    {
        GameObject go = CreateUiObject(name, parent);
        Image image = go.AddComponent<Image>();
        image.color = normalColor;

        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = Color.Lerp(normalColor, Color.white, 0.12f);
        colors.pressedColor = Color.Lerp(normalColor, Color.black, 0.15f);
        colors.selectedColor = normalColor;
        colors.disabledColor = new Color(normalColor.r * 0.42f, normalColor.g * 0.42f, normalColor.b * 0.42f, 0.75f);
        button.colors = colors;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.20f);
        outline.effectDistance = new Vector2(1f, -1f);

        label = CreateBodyText("Label", go.transform, font, buttonText, fontSize, TextAnchor.MiddleCenter, textColor, localize);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(12f, 0f);
        labelRect.offsetMax = new Vector2(-12f, 0f);
        return button;
    }

    public static Scrollbar CreateVerticalScrollbar(
        string name,
        Transform parent,
        Color trackColor,
        Color handleColor)
    {
        RectTransform scrollbarRoot = CreateStyledPanel(name, parent, trackColor);
        scrollbarRoot.anchorMin = new Vector2(1f, 0f);
        scrollbarRoot.anchorMax = new Vector2(1f, 1f);
        scrollbarRoot.pivot = new Vector2(1f, 0.5f);
        scrollbarRoot.sizeDelta = new Vector2(12f, 0f);
        scrollbarRoot.anchoredPosition = Vector2.zero;

        RectTransform slidingArea = CreateUiObject($"{name}_SlidingArea", scrollbarRoot).GetComponent<RectTransform>();
        StretchRect(slidingArea, 2f, 2f, 2f, 2f);

        RectTransform handleRect = CreateStyledPanel($"{name}_Handle", slidingArea, handleColor);
        StretchRect(handleRect, 0f, 0f, 0f, 0f);

        Scrollbar scrollbar = scrollbarRoot.gameObject.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleRect.GetComponent<Image>();
        scrollbar.value = 1f;
        scrollbar.size = 0.25f;
        return scrollbar;
    }

    public static void StretchRect(RectTransform rect, float left, float top, float right, float bottom)
    {
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
