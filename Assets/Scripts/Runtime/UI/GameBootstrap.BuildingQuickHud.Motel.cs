using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class MotelQuickHudRowUi
    {
        public Image IconImage;
        public Text LabelText;
        public Text ValueText;
    }

    private enum MotelQuickHudIconKind
    {
        Bed,
        Bell,
        Moon,
        Dollar,
        People,
        Cash,
        Person
    }

    private static readonly Color MotelQuickHudPanelColor = new(0.025f, 0.055f, 0.075f, 0.96f);
    private static readonly Color MotelQuickHudCardColor = new(0.045f, 0.075f, 0.105f, 0.94f);
    private static readonly Color MotelQuickHudBorderColor = new(0.47f, 0.63f, 0.78f, 0.22f);
    private static readonly Color MotelQuickHudMainTextColor = new(0.94f, 0.96f, 0.98f, 1f);
    private static readonly Color MotelQuickHudSecondaryTextColor = new(0.66f, 0.71f, 0.78f, 1f);
    private static readonly Color MotelQuickHudMutedTextColor = new(0.46f, 0.51f, 0.59f, 1f);
    private static readonly Color MotelQuickHudAmberColor = new(0.95f, 0.65f, 0.10f, 1f);
    private static readonly Color MotelQuickHudBlueColor = new(0.24f, 0.63f, 1f, 1f);
    private static readonly Color MotelQuickHudGreenColor = new(0.31f, 0.79f, 0.42f, 1f);

    private RectTransform motelQuickHudCard;
    private Text motelQuickHudDescriptionText;
    private MotelQuickHudRowUi motelQuickHudServiceRow;
    private MotelQuickHudRowUi motelQuickHudFeeRow;
    private MotelQuickHudRowUi motelQuickHudStaffRow;
    private MotelQuickHudRowUi motelQuickHudCashRow;
    private MotelQuickHudRowUi motelQuickHudGuestsRow;

    private void CreateMotelBuildingQuickHud(RectTransform root, Font uiFont)
    {
        RectTransform card = CreateStyledPanel("MotelQuickHudCard", root, MotelQuickHudCardColor);
        ApplyMotelQuickHudPanelStyle(card, MotelQuickHudCardColor);
        LayoutElement cardLayoutElement = card.gameObject.AddComponent<LayoutElement>();
        cardLayoutElement.preferredHeight = 260f;

        VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(10, 10, 10, 10);
        cardLayout.spacing = 4f;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;
        motelQuickHudCard = card;

        RectTransform introRow = CreateLayoutRow("MotelIntroRow", card, 62f, 10f);
        RectTransform serviceIconRoot = CreateUiObject("MotelServiceIcon", introRow).GetComponent<RectTransform>();
        LayoutElement serviceIconLayout = serviceIconRoot.gameObject.AddComponent<LayoutElement>();
        serviceIconLayout.preferredWidth = 56f;
        serviceIconLayout.preferredHeight = 56f;
        Image serviceIcon = serviceIconRoot.gameObject.AddComponent<Image>();
        serviceIcon.sprite = CreateMotelQuickHudIconSprite(MotelQuickHudIconKind.Bell, Color.white, new Color(0.08f, 0.32f, 0.54f, 1f), 56, true);
        serviceIcon.preserveAspect = true;
        serviceIcon.raycastTarget = false;

        motelQuickHudDescriptionText = CreateBodyText(
            "MotelDescription",
            introRow,
            uiFont,
            string.Empty,
            15,
            TextAnchor.MiddleLeft,
            MotelQuickHudSecondaryTextColor);
        motelQuickHudDescriptionText.lineSpacing = 1.08f;
        motelQuickHudDescriptionText.verticalOverflow = VerticalWrapMode.Truncate;
        motelQuickHudDescriptionText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        CreateMotelQuickHudDivider(card);
        motelQuickHudServiceRow = CreateMotelQuickHudRow(card, uiFont, MotelQuickHudIconKind.Moon, MotelQuickHudBlueColor);
        motelQuickHudFeeRow = CreateMotelQuickHudRow(card, uiFont, MotelQuickHudIconKind.Dollar, MotelQuickHudAmberColor);
        motelQuickHudStaffRow = CreateMotelQuickHudRow(card, uiFont, MotelQuickHudIconKind.People, MotelQuickHudBlueColor);
        motelQuickHudCashRow = CreateMotelQuickHudRow(card, uiFont, MotelQuickHudIconKind.Cash, MotelQuickHudGreenColor);
        CreateMotelQuickHudDivider(card);
        motelQuickHudGuestsRow = CreateMotelQuickHudRow(card, uiFont, MotelQuickHudIconKind.Person, MotelQuickHudMutedTextColor);

        card.gameObject.SetActive(false);
    }

    private void SetMotelBuildingQuickHudVisible(bool visible)
    {
        if (motelQuickHudCard != null)
        {
            motelQuickHudCard.gameObject.SetActive(visible);
        }
    }

    private void UpdateMotelBuildingQuickHud(LocationData selectedMotel)
    {
        if (motelQuickHudCard == null)
        {
            return;
        }

        LocationData motel = selectedMotel;
        if (motel == null)
        {
            locations.TryGetValue(LocationType.Motel, out motel);
        }

        int serviceFee = motel != null ? motel.ServiceFee : 0;
        int staffMax = GetMaxBuildingWorkerSlots(LocationType.Motel);
        int staffCurrent = CountWorkersOnShiftAt(LocationType.Motel);
        int cash = motel != null ? motel.BuildingBank : 0;

        motelQuickHudDescriptionText.text = "\u0416\u0438\u0442\u0435\u043b\u0438 \u043e\u0442\u0434\u044b\u0445\u0430\u044e\u0442 \u0438 \u043d\u043e\u0447\u0443\u044e\u0442 \u0437\u0434\u0435\u0441\u044c,\n\u043a\u043e\u0433\u0434\u0430 \u0438\u043c \u043d\u0443\u0436\u0435\u043d \u0441\u043e\u043d \u0438\u043b\u0438 \u043f\u0430\u0443\u0437\u0430.";
        SetMotelQuickHudRow(motelQuickHudServiceRow, "\u0423\u0441\u043b\u0443\u0433\u0430", "\u0421\u043e\u043d \u0438 \u043e\u0442\u0434\u044b\u0445", MotelQuickHudBlueColor);
        SetMotelQuickHudRow(motelQuickHudFeeRow, "\u0421\u0442\u043e\u0438\u043c\u043e\u0441\u0442\u044c", $"${serviceFee}", MotelQuickHudAmberColor);
        SetMotelQuickHudRow(motelQuickHudStaffRow, "\u0421\u043e\u0442\u0440\u0443\u0434\u043d\u0438\u043a\u0438", $"{staffCurrent} / {staffMax}", MotelQuickHudMainTextColor);
        SetMotelQuickHudRow(motelQuickHudCashRow, "\u041a\u0430\u0441\u0441\u0430", $"${cash}", MotelQuickHudGreenColor);
        SetMotelQuickHudRow(motelQuickHudGuestsRow, "\u041f\u043e\u0441\u0442\u043e\u044f\u043b\u044c\u0446\u044b", GetMotelQuickHudGuestStatus(), MotelQuickHudSecondaryTextColor);
    }

    private void UpdateMotelBuildingQuickHudChrome()
    {
        ApplyMotelQuickHudPanelStyle(buildingQuickHud.Root, MotelQuickHudPanelColor);
        if (buildingQuickHud.HeaderIconImage != null)
        {
            buildingQuickHud.HeaderIconImage.sprite = CreateMotelQuickHudIconSprite(MotelQuickHudIconKind.Bed, Color.white, Color.clear, 52, false);
            buildingQuickHud.HeaderIconImage.preserveAspect = true;
        }

        Color normal = new(0.035f, 0.065f, 0.09f, 0.98f);
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
            closeLayout.preferredWidth = 40f;
            closeLayout.preferredHeight = 40f;
        }

        buildingQuickHud.CloseButtonText.fontSize = 24;
        buildingQuickHud.CloseButtonText.text = "X";
    }

    private MotelQuickHudRowUi CreateMotelQuickHudRow(Transform parent, Font uiFont, MotelQuickHudIconKind icon, Color iconColor)
    {
        RectTransform row = CreateLayoutRow($"Motel{icon}Row", parent, 29f, 8f);
        row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

        RectTransform iconRoot = CreateUiObject("Icon", row).GetComponent<RectTransform>();
        LayoutElement iconLayout = iconRoot.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 23f;
        iconLayout.preferredHeight = 23f;
        Image iconImage = iconRoot.gameObject.AddComponent<Image>();
        iconImage.sprite = CreateMotelQuickHudIconSprite(icon, iconColor, Color.clear, 24, false);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        Text label = CreateBodyText("Label", row, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, MotelQuickHudMainTextColor);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        Text value = CreateBodyText("Value", row, uiFont, string.Empty, 13, TextAnchor.MiddleRight, MotelQuickHudMainTextColor);
        LayoutElement valueLayout = value.gameObject.AddComponent<LayoutElement>();
        valueLayout.preferredWidth = 112f;
        value.horizontalOverflow = HorizontalWrapMode.Wrap;
        value.verticalOverflow = VerticalWrapMode.Truncate;

        return new MotelQuickHudRowUi
        {
            IconImage = iconImage,
            LabelText = label,
            ValueText = value
        };
    }

    private static void SetMotelQuickHudRow(MotelQuickHudRowUi row, string label, string value, Color valueColor)
    {
        if (row == null)
        {
            return;
        }

        row.LabelText.text = label;
        row.ValueText.text = value;
        row.ValueText.color = valueColor;
    }

    private string GetMotelQuickHudGuestStatus()
    {
        int guestCount = GetBuildingQuickHudMotelGuestCount();
        return guestCount > 0 ? guestCount.ToString() : "\u0421\u0435\u0439\u0447\u0430\u0441 \u043d\u0438\u043a\u043e\u0433\u043e \u043d\u0435\u0442";
    }

    private static RectTransform CreateMotelQuickHudDivider(Transform parent)
    {
        RectTransform divider = CreateUiObject("MotelDivider", parent).GetComponent<RectTransform>();
        divider.gameObject.AddComponent<Image>().color = new Color(0.47f, 0.63f, 0.78f, 0.18f);
        divider.gameObject.AddComponent<LayoutElement>().preferredHeight = 1f;
        return divider;
    }

    private static void ApplyMotelQuickHudPanelStyle(RectTransform panel, Color fill)
    {
        if (panel == null)
        {
            return;
        }

        if (panel.TryGetComponent(out Image image))
        {
            image.color = fill;
        }

        if (!panel.TryGetComponent(out Outline outline))
        {
            outline = panel.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = MotelQuickHudBorderColor;
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private static Sprite CreateMotelQuickHudIconSprite(MotelQuickHudIconKind kind, Color primary, Color background, int size, bool circleBackground)
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
            case MotelQuickHudIconKind.Bed:
                PaintMotelBedIcon(pixels, size, primary);
                break;
            case MotelQuickHudIconKind.Bell:
                PaintMotelBellIcon(pixels, size, primary);
                break;
            case MotelQuickHudIconKind.Moon:
                PaintMotelMoonIcon(pixels, size, primary);
                break;
            case MotelQuickHudIconKind.Dollar:
                PaintMotelDollarIcon(pixels, size, primary);
                break;
            case MotelQuickHudIconKind.People:
                PaintCityHallPeopleIcon(pixels, size, primary);
                break;
            case MotelQuickHudIconKind.Cash:
                PaintMotelCashIcon(pixels, size, primary);
                break;
            case MotelQuickHudIconKind.Person:
                PaintMotelPersonIcon(pixels, size, primary);
                break;
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static void PaintMotelBedIcon(Color[] pixels, int size, Color color)
    {
        FillCityHallIconRect(pixels, size, 5, 7, 4, size - 14, color);
        FillCityHallIconRect(pixels, size, 8, 12, size - 12, 7, color);
        FillCityHallIconRect(pixels, size, 12, 19, size - 17, 5, color);
        FillCityHallIconRect(pixels, size, 8, 6, 4, 5, color);
        FillCityHallIconRect(pixels, size, size - 10, 6, 4, 5, color);
        DrawCityHallIconLine(pixels, size, 6, size - 8, size / 2, size - 2, color, 3);
        DrawCityHallIconLine(pixels, size, size / 2, size - 2, size - 7, size - 8, color, 3);
    }

    private static void PaintMotelBellIcon(Color[] pixels, int size, Color color)
    {
        int cx = size / 2;
        int cy = size / 2 - 2;
        int radius = size / 4;
        DrawCityHallIconArc(pixels, size, cx, cy, radius, 0, 180, color, 4);
        FillCityHallIconRect(pixels, size, cx - radius, cy - 3, radius * 2, 8, color);
        FillCityHallIconRect(pixels, size, cx - 2, cy + radius - 3, 4, 5, color);
        FillCityHallIconRect(pixels, size, cx - radius - 4, cy - 8, radius * 2 + 8, 4, color);
        FillCityHallIconCircle(pixels, size, cx, cy - 10, 3, color);
    }

    private static void PaintMotelMoonIcon(Color[] pixels, int size, Color color)
    {
        FillCityHallIconCircle(pixels, size, size / 2 - 1, size / 2 + 1, size / 3, color);
        FillCityHallIconCircle(pixels, size, size / 2 + 7, size / 2 + 6, size / 3, Color.clear);
    }

    private static void PaintMotelDollarIcon(Color[] pixels, int size, Color color)
    {
        FillCityHallIconCircle(pixels, size, size / 2, size / 2, size / 2 - 3, color);
        Color cutout = new(0.025f, 0.055f, 0.075f, 1f);
        FillCityHallIconRect(pixels, size, size / 2 - 1, 7, 3, size - 14, cutout);
        DrawCityHallIconLine(pixels, size, size / 2 + 5, size - 9, size / 2 - 5, size - 9, cutout, 3);
        DrawCityHallIconLine(pixels, size, size / 2 - 5, size - 9, size / 2 - 5, size / 2, cutout, 3);
        DrawCityHallIconLine(pixels, size, size / 2 - 5, size / 2, size / 2 + 5, size / 2, cutout, 3);
        DrawCityHallIconLine(pixels, size, size / 2 + 5, size / 2, size / 2 + 5, 8, cutout, 3);
        DrawCityHallIconLine(pixels, size, size / 2 + 5, 8, size / 2 - 5, 8, cutout, 3);
    }

    private static void PaintMotelCashIcon(Color[] pixels, int size, Color color)
    {
        DrawCityHallIconRect(pixels, size, 4, 8, size - 8, size - 16, color, 3);
        FillCityHallIconCircle(pixels, size, size / 2, size / 2, 5, color);
        FillCityHallIconRect(pixels, size, 7, size / 2 - 2, 4, 4, color);
        FillCityHallIconRect(pixels, size, size - 11, size / 2 - 2, 4, 4, color);
    }

    private static void PaintMotelPersonIcon(Color[] pixels, int size, Color color)
    {
        FillCityHallIconCircle(pixels, size, size / 2, size - 9, 6, color);
        FillCityHallIconRect(pixels, size, size / 2 - 8, 6, 16, 11, color);
        DrawCityHallIconCircle(pixels, size, size / 2, 10, 8, color, 3);
    }
}
