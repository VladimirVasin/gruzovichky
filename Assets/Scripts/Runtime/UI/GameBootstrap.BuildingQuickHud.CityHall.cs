using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class CityHallQuickHudRowUi
    {
        public Image IconImage;
        public Text LabelText;
        public Text ValueText;
    }

    private enum CityHallQuickHudIconKind
    {
        Government,
        Service,
        Envelope,
        Growth,
        Clock,
        People,
        Flag,
        Open
    }

    private static readonly Color CityHallQuickHudPanelColor = new(0.025f, 0.055f, 0.075f, 0.96f);
    private static readonly Color CityHallQuickHudCardColor = new(0.045f, 0.075f, 0.105f, 0.94f);
    private static readonly Color CityHallQuickHudBorderColor = new(0.47f, 0.63f, 0.78f, 0.24f);
    private static readonly Color CityHallQuickHudSecondaryTextColor = new(0.62f, 0.70f, 0.82f, 1f);
    private static readonly Color CityHallQuickHudMutedTextColor = new(0.50f, 0.58f, 0.68f, 1f);
    private static readonly Color CityHallQuickHudAmberColor = new(1f, 0.66f, 0.08f, 1f);
    private static readonly Color CityHallQuickHudGreenColor = new(0.46f, 0.82f, 0.36f, 1f);
    private static readonly Color CityHallQuickHudRedColor = new(1f, 0.27f, 0.24f, 1f);
    private static readonly Color CityHallQuickHudBlueColor = new(0.30f, 0.62f, 0.92f, 1f);

    private void CreateCityHallBuildingQuickHud(RectTransform root, Font uiFont)
    {
        if (buildingQuickHud == null)
        {
            return;
        }

        if (buildingQuickHud.HeaderIconImage != null)
        {
            buildingQuickHud.HeaderIconImage.sprite = CreateCityHallQuickHudIconSprite(
                CityHallQuickHudIconKind.Government,
                Color.white,
                Color.clear,
                48,
                false);
            buildingQuickHud.HeaderIconImage.preserveAspect = true;
        }

        RectTransform card = CreateStyledPanel("CityHallQuickHudCard", root, CityHallQuickHudCardColor);
        ApplyCityHallQuickHudPanelStyle(card, CityHallQuickHudCardColor);
        LayoutElement cardLayoutElement = card.gameObject.AddComponent<LayoutElement>();
        cardLayoutElement.preferredHeight = 267f;

        VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(10, 10, 10, 12);
        cardLayout.spacing = 5f;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;
        buildingQuickHud.CityHallCard = card;

        RectTransform introRow = CreateLayoutRow("CityHallIntroRow", card, 46f, 9f);
        RectTransform serviceIconRoot = CreateUiObject("CityHallServiceIcon", introRow).GetComponent<RectTransform>();
        serviceIconRoot.gameObject.AddComponent<LayoutElement>().preferredWidth = 44f;
        Image serviceIcon = serviceIconRoot.gameObject.AddComponent<Image>();
        serviceIcon.sprite = CreateCityHallQuickHudIconSprite(
            CityHallQuickHudIconKind.Service,
            Color.white,
            new Color(0.12f, 0.34f, 0.54f, 1f),
            44,
            true);
        serviceIcon.preserveAspect = true;
        serviceIcon.raycastTarget = false;

        RectTransform introTextColumn = CreateUiObject("CityHallIntroText", introRow).GetComponent<RectTransform>();
        introTextColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup introTextLayout = introTextColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        introTextLayout.spacing = 2f;
        introTextLayout.childControlWidth = true;
        introTextLayout.childControlHeight = true;
        introTextLayout.childForceExpandWidth = true;
        introTextLayout.childForceExpandHeight = false;

        buildingQuickHud.CityHallTitleText = CreateHeaderText("CityHallCardTitle", introTextColumn, uiFont, string.Empty, 18, TextAnchor.MiddleLeft, Color.white);
        buildingQuickHud.CityHallTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 0f;
        buildingQuickHud.CityHallTitleText.gameObject.SetActive(false);
        buildingQuickHud.CityHallDescriptionText = CreateBodyText("CityHallDescription", introTextColumn, uiFont, string.Empty, 12, TextAnchor.UpperLeft, CityHallQuickHudSecondaryTextColor);
        buildingQuickHud.CityHallDescriptionText.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

        CreateCityHallQuickHudDivider(card);
        buildingQuickHud.CityHallCompletionRow = CreateCityHallQuickHudRow(card, uiFont, CityHallQuickHudIconKind.Growth, CityHallQuickHudGreenColor);
        buildingQuickHud.CityHallPenaltyRow = CreateCityHallQuickHudRow(card, uiFont, CityHallQuickHudIconKind.Clock, CityHallQuickHudRedColor);
        CreateCityHallQuickHudDivider(card);
        buildingQuickHud.CityHallRequestsStatusRow = CreateCityHallQuickHudRow(card, uiFont, CityHallQuickHudIconKind.People, CityHallQuickHudBlueColor);
        buildingQuickHud.CityHallGoalStatusRow = CreateCityHallQuickHudRow(card, uiFont, CityHallQuickHudIconKind.Flag, CityHallQuickHudBlueColor);

        buildingQuickHud.CityHallOpenButton = CreateButton(
            "CityHallOpenButton",
            card,
            uiFont,
            out buildingQuickHud.CityHallOpenButtonText,
            "Открыть Ратушу",
            15,
            new Color(0.82f, 0.34f, 0f, 1f),
            Color.white);
        LayoutElement buttonLayout = buildingQuickHud.CityHallOpenButton.gameObject.AddComponent<LayoutElement>();
        buttonLayout.preferredHeight = 36f;
        buildingQuickHud.CityHallOpenButtonText.fontStyle = FontStyle.Bold;
        buildingQuickHud.CityHallOpenButtonText.rectTransform.offsetMin = new Vector2(42f, 0f);

        RectTransform openIconRoot = CreateUiObject("CityHallOpenButtonIcon", buildingQuickHud.CityHallOpenButton.transform).GetComponent<RectTransform>();
        openIconRoot.anchorMin = new Vector2(0f, 0.5f);
        openIconRoot.anchorMax = new Vector2(0f, 0.5f);
        openIconRoot.pivot = new Vector2(0.5f, 0.5f);
        openIconRoot.anchoredPosition = new Vector2(26f, 0f);
        openIconRoot.sizeDelta = new Vector2(20f, 20f);
        buildingQuickHud.CityHallOpenButtonIcon = openIconRoot.gameObject.AddComponent<Image>();
        buildingQuickHud.CityHallOpenButtonIcon.sprite = CreateCityHallQuickHudIconSprite(
            CityHallQuickHudIconKind.Open,
            Color.white,
            Color.clear,
            24,
            false);
        buildingQuickHud.CityHallOpenButtonIcon.raycastTarget = false;
        buildingQuickHud.CityHallOpenButtonIcon.preserveAspect = true;
        buildingQuickHud.CityHallOpenButton.onClick.AddListener(OpenContextPanelFromBuildingQuickHud);

        card.gameObject.SetActive(false);
    }

    private void ConfigureBuildingQuickHudMode(bool cityHall, bool motel = false)
    {
        if (buildingQuickHud == null)
        {
            return;
        }

        buildingQuickHud.Root.sizeDelta = cityHall
            ? new Vector2(330f, 341f)
            : motel
                ? new Vector2(330f, 374f)
            : new Vector2(360f, 500f);

        if (buildingQuickHud.Root.TryGetComponent(out VerticalLayoutGroup layout))
        {
            layout.padding = cityHall ? new RectOffset(12, 12, 12, 12)
                : motel ? new RectOffset(12, 12, 12, 12)
                : new RectOffset(16, 16, 16, 16);
            layout.spacing = cityHall ? 7f : motel ? 7f : 14f;
        }

        ApplyCityHallQuickHudPanelStyle(
            buildingQuickHud.Root,
            cityHall || motel ? CityHallQuickHudPanelColor : FleetPanelColor,
            cityHall || motel);

        if (buildingQuickHud.HeaderIconRoot != null)
        {
            buildingQuickHud.HeaderIconRoot.gameObject.SetActive(cityHall || motel);
            if (buildingQuickHud.HeaderIconRoot.TryGetComponent(out LayoutElement headerIconLayout))
            {
                headerIconLayout.preferredWidth = cityHall ? 30f : motel ? 44f : 38f;
                headerIconLayout.preferredHeight = cityHall ? 30f : motel ? 36f : 38f;
            }

            if (cityHall && buildingQuickHud.HeaderIconImage != null)
            {
                buildingQuickHud.HeaderIconImage.sprite = CreateCityHallQuickHudIconSprite(
                    CityHallQuickHudIconKind.Government,
                    Color.white,
                    Color.clear,
                    48,
                    false);
                buildingQuickHud.HeaderIconImage.preserveAspect = true;
            }
        }

        if (buildingQuickHud.SummaryCard != null)
        {
            buildingQuickHud.SummaryCard.gameObject.SetActive(!cityHall && !motel);
        }

        if (buildingQuickHud.CityHallCard != null)
        {
            buildingQuickHud.CityHallCard.gameObject.SetActive(cityHall);
        }
        SetMotelBuildingQuickHudVisible(motel);

        if (buildingQuickHud.ContextButtonRow != null)
        {
            buildingQuickHud.ContextButtonRow.gameObject.SetActive(!cityHall);
        }

        UpdateCityHallQuickHudCloseButtonStyle(cityHall);
        if (motel)
        {
            UpdateMotelBuildingQuickHudChrome();
        }
    }

    private void UpdateCityHallBuildingQuickHud()
    {
        if (buildingQuickHud == null)
        {
            return;
        }

        buildingQuickHud.CityHallTitleText.text = string.Empty;
        buildingQuickHud.CityHallDescriptionText.text = "\u041f\u0440\u0438\u043d\u0438\u043c\u0430\u0435\u0442 \u043e\u0431\u0440\u0430\u0449\u0435\u043d\u0438\u044f \u0436\u0438\u0442\u0435\u043b\u0435\u0439\n\u0438 \u0432\u0435\u0434\u0451\u0442 \u0433\u043e\u0440\u043e\u0434\u0441\u043a\u0438\u0435 \u0446\u0435\u043b\u0438.";

        int activeRequests = CountOpenCityComplaints();
        CityComplaint acceptedGoal = GetActiveAcceptedCityServiceRequest();
        SetCityHallQuickHudRow(
            buildingQuickHud.CityHallCompletionRow,
            "\u0412\u044b\u043f\u043e\u043b\u043d\u0435\u043d\u0438\u0435",
            "\u041f\u043e\u0432\u044b\u0448\u0430\u0435\u0442 \u0434\u043e\u0432\u0435\u0440\u0438\u0435",
            CityHallQuickHudGreenColor);
        SetCityHallQuickHudRow(
            buildingQuickHud.CityHallPenaltyRow,
            "\u041e\u0442\u043a\u0430\u0437 / \u041f\u0440\u043e\u0441\u0440\u043e\u0447\u043a\u0430",
            "\u0421\u043d\u0438\u0436\u0430\u044e\u0442 \u0434\u043e\u0432\u0435\u0440\u0438\u0435",
            CityHallQuickHudRedColor);

        SetCityHallQuickHudRow(
            buildingQuickHud.CityHallRequestsStatusRow,
            "\u041e\u0431\u0440\u0430\u0449\u0435\u043d\u0438\u044f",
            activeRequests > 0 ? $"{activeRequests} \u0430\u043a\u0442\u0438\u0432\u043d." : "\u041d\u0435\u0442 \u0430\u043a\u0442\u0438\u0432\u043d\u044b\u0445",
            activeRequests > 0 ? Color.white : CityHallQuickHudSecondaryTextColor);
        SetCityHallQuickHudRow(
            buildingQuickHud.CityHallGoalStatusRow,
            "\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u0430\u044f \u0446\u0435\u043b\u044c",
            acceptedGoal != null ? TruncateCityHallQuickHudValue(FormatCityComplaintTitle(acceptedGoal, true), 24) : "\u041d\u0435\u0442 \u043f\u0440\u0438\u043d\u044f\u0442\u043e\u0439 \u0446\u0435\u043b\u0438",
            acceptedGoal != null ? Color.white : CityHallQuickHudSecondaryTextColor);
    }

    private CityHallQuickHudRowUi CreateCityHallQuickHudRow(
        Transform parent,
        Font uiFont,
        CityHallQuickHudIconKind icon,
        Color iconColor)
    {
        RectTransform row = CreateLayoutRow($"CityHall{icon}Row", parent, 30f, 8f);
        row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

        RectTransform iconRoot = CreateUiObject("Icon", row).GetComponent<RectTransform>();
        LayoutElement iconLayout = iconRoot.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 24f;
        iconLayout.preferredHeight = 24f;
        Image iconImage = iconRoot.gameObject.AddComponent<Image>();
        iconImage.sprite = CreateCityHallQuickHudIconSprite(icon, iconColor, Color.clear, 28, false);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        Text label = CreateBodyText("Label", row, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        Text value = CreateBodyText("Value", row, uiFont, string.Empty, 13, TextAnchor.MiddleRight, Color.white);
        LayoutElement valueLayout = value.gameObject.AddComponent<LayoutElement>();
        valueLayout.preferredWidth = 132f;
        value.horizontalOverflow = HorizontalWrapMode.Wrap;
        value.verticalOverflow = VerticalWrapMode.Truncate;

        return new CityHallQuickHudRowUi
        {
            IconImage = iconImage,
            LabelText = label,
            ValueText = value
        };
    }

    private static void SetCityHallQuickHudRow(CityHallQuickHudRowUi row, string label, string value, Color valueColor)
    {
        if (row == null)
        {
            return;
        }

        row.LabelText.text = label;
        row.ValueText.text = value;
        row.ValueText.color = valueColor;
    }

    private static RectTransform CreateCityHallQuickHudDivider(Transform parent)
    {
        RectTransform divider = CreateUiObject("CityHallDivider", parent).GetComponent<RectTransform>();
        divider.gameObject.AddComponent<Image>().color = new Color(0.47f, 0.63f, 0.78f, 0.18f);
        divider.gameObject.AddComponent<LayoutElement>().preferredHeight = 1f;
        return divider;
    }

    private static void ApplyCityHallQuickHudPanelStyle(RectTransform panel, Color fill, bool cityHall = true)
    {
        if (panel == null)
        {
            return;
        }

        if (panel.TryGetComponent(out Image image))
        {
            image.color = fill;
        }

        if (panel.TryGetComponent(out Outline outline))
        {
            outline.effectColor = cityHall ? CityHallQuickHudBorderColor : new Color(0f, 0f, 0f, 0.18f);
            outline.effectDistance = cityHall ? new Vector2(1f, -1f) : new Vector2(1f, -1f);
        }
    }

    private void UpdateCityHallQuickHudCloseButtonStyle(bool cityHall)
    {
        if (buildingQuickHud?.CloseButton == null)
        {
            return;
        }

        Color normal = cityHall
            ? new Color(0.035f, 0.065f, 0.09f, 0.98f)
            : new Color(0.26f, 0.30f, 0.36f, 1f);
        ColorBlock colors = buildingQuickHud.CloseButton.colors;
        colors.normalColor = normal;
        colors.highlightedColor = Color.Lerp(normal, Color.white, 0.14f);
        colors.pressedColor = Color.Lerp(normal, Color.black, 0.16f);
        colors.selectedColor = normal;
        buildingQuickHud.CloseButton.colors = colors;

        if (buildingQuickHud.CloseButton.targetGraphic is Image image)
        {
            image.color = normal;
        }

        if (buildingQuickHud.CloseButton.TryGetComponent(out LayoutElement closeLayout))
        {
            closeLayout.preferredWidth = cityHall ? 30f : 36f;
            closeLayout.preferredHeight = cityHall ? 30f : 36f;
        }

        if (buildingQuickHud.CloseButtonText != null)
        {
            buildingQuickHud.CloseButtonText.fontSize = cityHall ? 18 : 12;
            buildingQuickHud.CloseButtonText.text = "X";
        }
    }

    private static string TruncateCityHallQuickHudValue(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
        {
            return value;
        }

        return value.Substring(0, Mathf.Max(0, maxLength - 3)) + "...";
    }

    private static Sprite CreateCityHallQuickHudIconSprite(
        CityHallQuickHudIconKind kind,
        Color primary,
        Color background,
        int size,
        bool circleBackground)
    {
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        if (circleBackground)
        {
            FillCityHallIconCircle(pixels, size, size / 2, size / 2, size / 2 - 2, background);
            DrawCityHallIconCircle(pixels, size, size / 2, size / 2, size / 2 - 3, Color.Lerp(background, Color.white, 0.12f), 2);
        }

        switch (kind)
        {
            case CityHallQuickHudIconKind.Government:
                PaintCityHallGovernmentIcon(pixels, size, primary);
                break;
            case CityHallQuickHudIconKind.Service:
                PaintCityHallServiceIcon(pixels, size, primary);
                break;
            case CityHallQuickHudIconKind.Envelope:
                PaintCityHallEnvelopeIcon(pixels, size, primary);
                break;
            case CityHallQuickHudIconKind.Growth:
                PaintCityHallGrowthIcon(pixels, size, primary);
                break;
            case CityHallQuickHudIconKind.Clock:
                PaintCityHallClockIcon(pixels, size, primary);
                break;
            case CityHallQuickHudIconKind.People:
                PaintCityHallPeopleIcon(pixels, size, primary);
                break;
            case CityHallQuickHudIconKind.Flag:
                PaintCityHallFlagIcon(pixels, size, primary);
                break;
            case CityHallQuickHudIconKind.Open:
                PaintCityHallOpenIcon(pixels, size, primary);
                break;
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static void PaintCityHallGovernmentIcon(Color[] pixels, int size, Color color)
    {
        DrawCityHallIconLine(pixels, size, size / 2, size - 7, 5, size - 16, color, 3);
        DrawCityHallIconLine(pixels, size, size / 2, size - 7, size - 6, size - 16, color, 3);
        FillCityHallIconRect(pixels, size, 6, size - 18, size - 12, 3, color);
        int columnTop = size - 20;
        int columnHeight = Mathf.Max(10, size / 3);
        for (int x = 8; x <= size - 10; x += Mathf.Max(6, size / 5))
        {
            FillCityHallIconRect(pixels, size, x, columnTop - columnHeight, 3, columnHeight, color);
        }

        FillCityHallIconRect(pixels, size, 5, 5, size - 10, 4, color);
        FillCityHallIconRect(pixels, size, 3, 2, size - 6, 3, color);
    }

    private static void PaintCityHallServiceIcon(Color[] pixels, int size, Color color)
    {
        int cx = size / 2;
        int cy = size / 2 + 1;
        DrawCityHallIconArc(pixels, size, cx, cy, size / 4, 25, 155, color, 4);
        FillCityHallIconRect(pixels, size, cx - size / 4 - 4, cy - 7, 6, 14, color);
        FillCityHallIconRect(pixels, size, cx + size / 4 - 2, cy - 7, 6, 14, color);
        DrawCityHallIconLine(pixels, size, cx + size / 5, cy - 11, cx + size / 3, cy - 15, color, 3);
        FillCityHallIconRect(pixels, size, cx + size / 3, cy - 16, 8, 3, color);
    }

    private static void PaintCityHallEnvelopeIcon(Color[] pixels, int size, Color color)
    {
        int x = 5;
        int y = 8;
        int w = size - 10;
        int h = size - 16;
        DrawCityHallIconRect(pixels, size, x, y, w, h, color, 3);
        DrawCityHallIconLine(pixels, size, x + 2, y + h - 2, size / 2, y + h / 2, color, 2);
        DrawCityHallIconLine(pixels, size, x + w - 3, y + h - 2, size / 2, y + h / 2, color, 2);
    }

    private static void PaintCityHallGrowthIcon(Color[] pixels, int size, Color color)
    {
        DrawCityHallIconLine(pixels, size, 4, 8, 12, 16, color, 3);
        DrawCityHallIconLine(pixels, size, 12, 16, 18, 13, color, 3);
        DrawCityHallIconLine(pixels, size, 18, 13, size - 7, size - 7, color, 3);
        DrawCityHallIconLine(pixels, size, size - 7, size - 7, size - 7, size - 14, color, 3);
        DrawCityHallIconLine(pixels, size, size - 7, size - 7, size - 14, size - 7, color, 3);
    }

    private static void PaintCityHallClockIcon(Color[] pixels, int size, Color color)
    {
        int cx = size / 2;
        int cy = size / 2;
        DrawCityHallIconCircle(pixels, size, cx, cy, size / 2 - 5, color, 3);
        DrawCityHallIconLine(pixels, size, cx, cy, cx, cy + 8, color, 3);
        DrawCityHallIconLine(pixels, size, cx, cy, cx + 6, cy - 5, color, 3);
    }

    private static void PaintCityHallPeopleIcon(Color[] pixels, int size, Color color)
    {
        FillCityHallIconCircle(pixels, size, 11, 21, 5, color);
        FillCityHallIconCircle(pixels, size, 22, 21, 5, color * 0.92f);
        FillCityHallIconRect(pixels, size, 6, 8, 11, 9, color);
        FillCityHallIconRect(pixels, size, 17, 8, 11, 9, color * 0.92f);
        FillCityHallIconCircle(pixels, size, 16, 22, 6, Color.Lerp(color, Color.white, 0.12f));
        FillCityHallIconRect(pixels, size, 9, 7, 15, 10, Color.Lerp(color, Color.white, 0.12f));
    }

    private static void PaintCityHallFlagIcon(Color[] pixels, int size, Color color)
    {
        FillCityHallIconRect(pixels, size, 7, 5, 3, size - 10, color);
        FillCityHallIconRect(pixels, size, 10, size - 12, size - 14, 10, color);
        DrawCityHallIconLine(pixels, size, size - 4, size - 12, size - 4, size - 19, color, 2);
    }

    private static void PaintCityHallOpenIcon(Color[] pixels, int size, Color color)
    {
        DrawCityHallIconRect(pixels, size, 5, 5, 16, 16, color, 3);
        FillCityHallIconRect(pixels, size, 18, 18, 9, 3, color);
        FillCityHallIconRect(pixels, size, 24, 12, 3, 9, color);
        DrawCityHallIconLine(pixels, size, 16, 16, 27, 27, color, 3);
        DrawCityHallIconLine(pixels, size, 27, 27, 27, 20, color, 3);
        DrawCityHallIconLine(pixels, size, 27, 27, 20, 27, color, 3);
    }

    private static void FillCityHallIconRect(Color[] pixels, int size, int x, int y, int width, int height, Color color)
    {
        for (int yy = y; yy < y + height; yy++)
        {
            for (int xx = x; xx < x + width; xx++)
            {
                SetCityHallIconPixel(pixels, size, xx, yy, color);
            }
        }
    }

    private static void DrawCityHallIconRect(Color[] pixels, int size, int x, int y, int width, int height, Color color, int thickness)
    {
        FillCityHallIconRect(pixels, size, x, y, width, thickness, color);
        FillCityHallIconRect(pixels, size, x, y + height - thickness, width, thickness, color);
        FillCityHallIconRect(pixels, size, x, y, thickness, height, color);
        FillCityHallIconRect(pixels, size, x + width - thickness, y, thickness, height, color);
    }

    private static void FillCityHallIconCircle(Color[] pixels, int size, int cx, int cy, int radius, Color color)
    {
        int radiusSq = radius * radius;
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                if (dx * dx + dy * dy <= radiusSq)
                {
                    SetCityHallIconPixel(pixels, size, x, y, color);
                }
            }
        }
    }

    private static void DrawCityHallIconCircle(Color[] pixels, int size, int cx, int cy, int radius, Color color, int thickness)
    {
        int outerSq = radius * radius;
        int inner = Mathf.Max(0, radius - thickness);
        int innerSq = inner * inner;
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                int d = dx * dx + dy * dy;
                if (d <= outerSq && d >= innerSq)
                {
                    SetCityHallIconPixel(pixels, size, x, y, color);
                }
            }
        }
    }

    private static void DrawCityHallIconArc(
        Color[] pixels,
        int size,
        int cx,
        int cy,
        int radius,
        int startDeg,
        int endDeg,
        Color color,
        int thickness)
    {
        for (int angle = startDeg; angle <= endDeg; angle += 3)
        {
            float rad = angle * Mathf.Deg2Rad;
            int x = Mathf.RoundToInt(cx + Mathf.Cos(rad) * radius);
            int y = Mathf.RoundToInt(cy + Mathf.Sin(rad) * radius);
            FillCityHallIconCircle(pixels, size, x, y, Mathf.Max(1, thickness / 2), color);
        }
    }

    private static void DrawCityHallIconLine(Color[] pixels, int size, int x0, int y0, int x1, int y1, Color color, int thickness)
    {
        int dx = Mathf.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            FillCityHallIconCircle(pixels, size, x0, y0, Mathf.Max(1, thickness / 2), color);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (e2 <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private static void SetCityHallIconPixel(Color[] pixels, int size, int x, int y, Color color)
    {
        if (x < 0 || x >= size || y < 0 || y >= size)
        {
            return;
        }

        pixels[y * size + x] = color;
    }
}
