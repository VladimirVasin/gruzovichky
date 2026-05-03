using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void SetupFleetScrollView(RectTransform parent)
    {
        FleetCanvasUiFactory.ScrollPanelRefs scroll = CreateVerticalScrollPanel(
            "TruckListScrollView",
            parent,
            new Color(0f, 0f, 0f, 0.04f),
            8f,
            10f);
        fleetScreenUi.TruckListScrollRect = scroll.ScrollRect;
        fleetScreenUi.TruckListContent = scroll.Content;
    }

    private FleetTruckRowUi CreateFleetTruckRow(RectTransform parent, int index)
    {
        Font uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        FleetTruckRowUi row = new();
        GameObject rowObject = CreateUiObject($"TruckRow{index + 1}", parent);
        row.Root = rowObject.GetComponent<RectTransform>();
        row.Root.sizeDelta = new Vector2(0f, 92f);
        rowObject.AddComponent<LayoutElement>().preferredHeight = 92f;
        row.Background = rowObject.AddComponent<Image>();
        row.Background.color = FleetRowColor;
        row.Outline = rowObject.AddComponent<Outline>();
        row.Outline.effectColor = new Color(0f, 0f, 0f, 0.26f);
        row.Outline.effectDistance = new Vector2(1f, -1f);
        row.Button = rowObject.AddComponent<Button>();
        row.Button.targetGraphic = row.Background;
        ColorBlock colors = row.Button.colors;
        colors.normalColor = FleetRowColor;
        colors.highlightedColor = new Color(0.23f, 0.28f, 0.36f, 1f);
        colors.pressedColor = new Color(0.16f, 0.19f, 0.25f, 1f);
        colors.selectedColor = new Color(0.24f, 0.28f, 0.34f, 1f);
        colors.disabledColor = new Color(0.12f, 0.14f, 0.18f, 0.65f);
        row.Button.colors = colors;
        row.Button.onClick.AddListener(() =>
        {
            LogUiInput($"Fleet Canvas: selected {GetTruckDisplayName(row.TruckNumber)} from fleet list");
            FocusTruck(row.TruckNumber);
        });

        GameObject accentObject = CreateUiObject("Accent", row.Root);
        RectTransform accentRect = accentObject.GetComponent<RectTransform>();
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(5f, 0f);
        accentRect.anchoredPosition = Vector2.zero;
        row.Accent = accentObject.AddComponent<Image>();
        row.Accent.color = FleetAccentColor;
        row.Accent.enabled = false;

        VerticalLayoutGroup contentLayout = rowObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(16, 14, 12, 12);
        contentLayout.spacing = 4;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        row.TruckNameText = CreateBodyText("TruckName", row.Root, uiFont, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        row.TruckNameText.fontStyle = FontStyle.Bold;
        row.TruckNameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
        row.DriverText = CreateBodyText("Driver", row.Root, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        row.DriverText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        row.StateText = CreateBodyText("State", row.Root, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, new Color(0.83f, 0.88f, 0.95f));
        row.StateText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        row.ResourceText = CreateBodyText("Resources", row.Root, uiFont, string.Empty, 11, TextAnchor.MiddleLeft, FleetMutedTextColor);
        row.ResourceText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
        return row;
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        return FleetCanvasUiFactory.CreateUiObject(name, parent);
    }

    private static RectTransform CreateStyledPanel(string name, Transform parent, Color color)
    {
        return FleetCanvasUiFactory.CreateStyledPanel(name, parent, color);
    }

    private static RectTransform CreateLayoutRow(string name, Transform parent, float preferredHeight, float spacing)
    {
        return FleetCanvasUiFactory.CreateLayoutRow(name, parent, preferredHeight, spacing);
    }

    private static RectTransform CreateSpacer(
        string name,
        Transform parent,
        float preferredWidth = 0f,
        float preferredHeight = 0f,
        float flexibleWidth = 0f,
        float flexibleHeight = 0f)
    {
        return FleetCanvasUiFactory.CreateSpacer(name, parent, preferredWidth, preferredHeight, flexibleWidth, flexibleHeight);
    }

    private static RectTransform CreateTabRow(string name, Transform parent, float preferredHeight, float spacing)
    {
        return FleetCanvasUiFactory.CreateTabRow(name, parent, preferredHeight, spacing);
    }

    private static FleetCanvasUiFactory.ScrollPanelRefs CreateVerticalScrollPanel(
        string name,
        Transform parent,
        Color viewportTint,
        float inset,
        float spacing,
        float scrollSensitivity = 28f)
    {
        return FleetCanvasUiFactory.CreateVerticalScrollPanel(name, parent, viewportTint, inset, spacing, scrollSensitivity);
    }

    private static FleetCanvasUiFactory.ScrollPanelRefs CreateVerticalScrollList(
        string name,
        Transform parent,
        string contentName,
        float spacing,
        float scrollSensitivity = 30f,
        float preferredHeight = -1f,
        float flexibleHeight = -1f)
    {
        return FleetCanvasUiFactory.CreateVerticalScrollList(name, parent, contentName, spacing, scrollSensitivity, preferredHeight, flexibleHeight);
    }

    private static RectTransform CreateVerticalStack(
        string name,
        Transform parent,
        RectOffset padding,
        float spacing,
        float preferredWidth = -1f,
        float preferredHeight = -1f,
        float flexibleWidth = -1f,
        float flexibleHeight = -1f,
        bool addContentSizeFitter = false)
    {
        return FleetCanvasUiFactory.CreateVerticalStack(name, parent, padding, spacing, preferredWidth, preferredHeight, flexibleWidth, flexibleHeight, addContentSizeFitter);
    }

    private static RectTransform CreateHorizontalLayoutPanel(
        string name,
        Transform parent,
        Color color,
        RectOffset padding,
        float spacing,
        float preferredWidth = -1f,
        float preferredHeight = -1f,
        float flexibleWidth = -1f,
        float flexibleHeight = -1f,
        TextAnchor childAlignment = TextAnchor.UpperLeft,
        bool childForceExpandWidth = false,
        bool childForceExpandHeight = false,
        bool addOutline = true)
    {
        return FleetCanvasUiFactory.CreateHorizontalLayoutPanel(
            name,
            parent,
            color,
            padding,
            spacing,
            preferredWidth,
            preferredHeight,
            flexibleWidth,
            flexibleHeight,
            childAlignment,
            childForceExpandWidth,
            childForceExpandHeight,
            addOutline);
    }

    private static RectTransform CreateSectionCard(Transform parent, Font font, string title, out RectTransform body, bool addTitle = true)
    {
        return FleetCanvasUiFactory.CreateSectionCard($"{title}Card", parent, font, title, FleetInsetColor, Color.white, L, out body, addTitle);
    }

    private static Text CreateHeaderText(string name, Transform parent, Font font, string value, int fontSize, TextAnchor alignment, Color color)
    {
        Text text = CreateBodyText(name, parent, font, value, fontSize, alignment, color);
        text.fontStyle = FontStyle.Bold;
        return text;
    }

    private static Text CreateBodyText(string name, Transform parent, Font font, string value, int fontSize, TextAnchor alignment, Color color)
    {
        return FleetCanvasUiFactory.CreateBodyText(name, parent, font, value, fontSize, alignment, color, L);
    }

    private static Text CreateValueText(string name, Transform parent, Font font)
    {
        Text text = CreateBodyText(name, parent, font, string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, Font font, out Text label, string buttonText, int fontSize, Color normalColor, Color textColor)
    {
        return FleetCanvasUiFactory.CreateButton(name, parent, font, out label, buttonText, fontSize, normalColor, textColor, L);
    }

    private static FleetCanvasUiFactory.BadgeRefs CreateBadge(
        string name,
        Transform parent,
        Font font,
        string value,
        int fontSize,
        Color backgroundColor,
        Color textColor,
        float preferredWidth,
        float preferredHeight)
    {
        return FleetCanvasUiFactory.CreateBadge(name, parent, font, value, fontSize, backgroundColor, textColor, preferredWidth, preferredHeight, L);
    }

    private static string FormatValueLine(string label, string value)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(FleetMutedTextColor)}>{L(label)}:</color>  <color=#FFFFFF>{L(value)}</color>";
    }

    private static string FormatTruckCargoValue(int amount, CargoType cargoType, int capacity = 5)
    {
        if (amount <= 0 || cargoType == CargoType.None)
        {
            return L("Empty");
        }

        return $"{amount}/{capacity} ({GetCargoTypeLabel(cargoType)})";
    }

    private static string GetCargoTypeLabel(CargoType cargoType)
    {
        return cargoType switch
        {
            CargoType.Logs      => L("Logs"),
            CargoType.Boards    => L("Boards"),
            CargoType.Cotton    => L("Cotton"),
            CargoType.Textile   => L("Textile"),
            CargoType.Furniture => L("Furniture"),
            CargoType.Fuel      => L("Fuel"),
            CargoType.Alcohol   => L("Alcohol"),
            CargoType.Food      => L("Food"),
            _                   => L("None")
        };
    }

    private static void StretchRect(RectTransform rect, float left, float top, float right, float bottom)
    {
        FleetCanvasUiFactory.StretchRect(rect, left, top, right, bottom);
    }

    private static void SetCenteredWindow(RectTransform rect, float width, float height, float yOffset)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, yOffset);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void AddOverlayCloseButton(RectTransform parent, Font font)
    {
        GameObject go = new("OverlayCloseButton");
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-10f, -10f);
        rt.sizeDelta = new Vector2(36f, 36f);

        // Exclude from layout so it floats over the window
        go.AddComponent<LayoutElement>().ignoreLayout = true;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.12f, 0.12f, 0.92f);

        Button btn = go.AddComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = new Color(0.18f, 0.12f, 0.12f, 0.92f);
        cb.highlightedColor = new Color(0.74f, 0.18f, 0.14f, 1f);
        cb.pressedColor = new Color(0.5f, 0.10f, 0.08f, 1f);
        cb.selectedColor = new Color(0.18f, 0.12f, 0.12f, 0.92f);
        cb.disabledColor = new Color(0.12f, 0.10f, 0.10f, 0.68f);
        btn.colors = cb;
        btn.targetGraphic = img;
        btn.onClick.AddListener(CloseAllMenus);

        GameObject textGo = new("X");
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRt = textGo.AddComponent<RectTransform>();
        StretchRect(textRt, 0f, 0f, 0f, 0f);
        Text txt = textGo.AddComponent<Text>();
        txt.text = "X";
        txt.font = font;
        txt.fontSize = 18;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.raycastTarget = false;

        // Render on top by moving to the last sibling.
        go.transform.SetAsLastSibling();
    }

    // Drivers Canvas Screen
}
