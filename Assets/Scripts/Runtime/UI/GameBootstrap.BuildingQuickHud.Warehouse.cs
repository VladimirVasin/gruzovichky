using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class WarehouseQuickHudRowUi
    {
        public Text LabelText;
        public Text ValueText;
    }

    private sealed class WarehouseQuickHudResourcePairUi
    {
        public Text LeftLabelText;
        public Text LeftValueText;
        public Text RightLabelText;
        public Text RightValueText;
    }

    private enum WarehouseQuickHudIconKind
    {
        Building,
        Crate,
        Status,
        People,
        Truck
    }

    private static readonly Color WarehouseQuickHudBlueColor = new(0.24f, 0.63f, 1f, 1f);
    private static readonly Color WarehouseQuickHudAmberColor = new(0.95f, 0.65f, 0.10f, 1f);
    private static readonly Color WarehouseQuickHudGreenColor = new(0.31f, 0.79f, 0.42f, 1f);
    private static readonly Color WarehouseQuickHudRedColor = new(0.92f, 0.26f, 0.20f, 1f);

    private RectTransform warehouseQuickHudCard;
    private Text warehouseQuickHudDescriptionText;
    private Text warehouseQuickHudResourcesTitleText;
    private WarehouseQuickHudRowUi warehouseQuickHudStatusRow;
    private WarehouseQuickHudRowUi warehouseQuickHudLoadersRow;
    private WarehouseQuickHudRowUi warehouseQuickHudLogisticsRow;
    private WarehouseQuickHudResourcePairUi warehouseQuickHudLogsBoardsRow;
    private WarehouseQuickHudResourcePairUi warehouseQuickHudCottonTextileRow;
    private WarehouseQuickHudResourcePairUi warehouseQuickHudFurnitureRow;

    private void CreateWarehouseBuildingQuickHud(RectTransform root, Font uiFont)
    {
        RectTransform card = CreateStyledPanel("WarehouseQuickHudCard", root, CityHallQuickHudCardColor);
        ApplyCityHallQuickHudPanelStyle(card, CityHallQuickHudCardColor);
        LayoutElement cardLayoutElement = card.gameObject.AddComponent<LayoutElement>();
        cardLayoutElement.preferredHeight = 254f;

        VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(10, 10, 10, 10);
        cardLayout.spacing = 3f;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;
        warehouseQuickHudCard = card;

        RectTransform introRow = CreateLayoutRow("WarehouseIntroRow", card, 48f, 9f);
        RectTransform crateIconRoot = CreateUiObject("WarehouseIntroIcon", introRow).GetComponent<RectTransform>();
        LayoutElement crateIconLayout = crateIconRoot.gameObject.AddComponent<LayoutElement>();
        crateIconLayout.preferredWidth = 48f;
        crateIconLayout.preferredHeight = 48f;
        Image crateIcon = crateIconRoot.gameObject.AddComponent<Image>();
        crateIcon.sprite = CreateWarehouseQuickHudIconSprite(
            WarehouseQuickHudIconKind.Crate,
            Color.white,
            new Color(0.12f, 0.34f, 0.54f, 1f),
            48,
            true);
        crateIcon.preserveAspect = true;
        crateIcon.raycastTarget = false;

        warehouseQuickHudDescriptionText = CreateBodyText(
            "WarehouseDescription",
            introRow,
            uiFont,
            string.Empty,
            13,
            TextAnchor.MiddleLeft,
            CityHallQuickHudSecondaryTextColor);
        warehouseQuickHudDescriptionText.lineSpacing = 1.04f;
        warehouseQuickHudDescriptionText.verticalOverflow = VerticalWrapMode.Truncate;
        warehouseQuickHudDescriptionText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        CreateCityHallQuickHudDivider(card);
        warehouseQuickHudStatusRow = CreateWarehouseQuickHudRow(card, uiFont, WarehouseQuickHudIconKind.Status, WarehouseQuickHudGreenColor);
        warehouseQuickHudLoadersRow = CreateWarehouseQuickHudRow(card, uiFont, WarehouseQuickHudIconKind.People, WarehouseQuickHudBlueColor);
        warehouseQuickHudLogisticsRow = CreateWarehouseQuickHudRow(card, uiFont, WarehouseQuickHudIconKind.Truck, WarehouseQuickHudAmberColor);
        CreateCityHallQuickHudDivider(card);

        warehouseQuickHudResourcesTitleText = CreateBodyText(
            "WarehouseResourcesTitle",
            card,
            uiFont,
            "\u0417\u0430\u043f\u0430\u0441\u044b",
            13,
            TextAnchor.MiddleLeft,
            Color.white);
        warehouseQuickHudResourcesTitleText.fontStyle = FontStyle.Bold;
        warehouseQuickHudResourcesTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        warehouseQuickHudLogsBoardsRow = CreateWarehouseQuickHudResourcePairRow(card, uiFont);
        warehouseQuickHudCottonTextileRow = CreateWarehouseQuickHudResourcePairRow(card, uiFont);
        warehouseQuickHudFurnitureRow = CreateWarehouseQuickHudResourcePairRow(card, uiFont);

        card.gameObject.SetActive(false);
    }

    private void SetWarehouseBuildingQuickHudVisible(bool visible)
    {
        if (warehouseQuickHudCard != null)
        {
            warehouseQuickHudCard.gameObject.SetActive(visible);
        }
    }

    private void UpdateWarehouseBuildingQuickHud(LocationData selectedWarehouse)
    {
        if (warehouseQuickHudCard == null)
        {
            return;
        }

        LocationData warehouse = selectedWarehouse;
        if (warehouse == null)
        {
            locations.TryGetValue(LocationType.Warehouse, out warehouse);
        }

        int loadersOnShift = CountWorkersOnShiftAt(LocationType.Warehouse);
        int assignedLoaders = CountLogisticsWorkers(LocationType.Warehouse);
        if (!TryGetRoadAccessQuickWarning(warehouse, out string statusText, out Color statusColor))
        {
            GetWarehouseQuickHudStatus(loadersOnShift, assignedLoaders, out statusText, out statusColor);
        }
        else
        {
            statusText = IsRussianLanguage() ? "\u041d\u0435\u0442 \u0434\u043e\u0440\u043e\u0433\u0438" : "No road";
        }

        warehouseQuickHudDescriptionText.text =
            "\u0426\u0435\u043d\u0442\u0440\u0430\u043b\u044c\u043d\u043e\u0435 \u0445\u0440\u0430\u043d\u0435\u043d\u0438\u0435 \u0440\u0435\u0441\u0443\u0440\u0441\u043e\u0432.\n" +
            "\u0413\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438 \u043f\u0440\u0438\u0432\u043e\u0437\u044f\u0442 \u0438 \u0437\u0430\u0431\u0438\u0440\u0430\u044e\u0442 \u043c\u0430\u0442\u0435\u0440\u0438\u0430\u043b\u044b.";

        SetWarehouseQuickHudRow(warehouseQuickHudStatusRow, "\u0421\u0442\u0430\u0442\u0443\u0441", statusText, statusColor);
        SetWarehouseQuickHudRow(warehouseQuickHudLoadersRow, "\u0413\u0440\u0443\u0437\u0447\u0438\u043a\u0438", $"{loadersOnShift} / {WarehouseMaxWorkers}", loadersOnShift > 0 ? WarehouseQuickHudGreenColor : CityHallQuickHudSecondaryTextColor);
        SetWarehouseQuickHudRow(warehouseQuickHudLogisticsRow, "\u041b\u043e\u0433\u0438\u0441\u0442\u0438\u043a\u0430", GetWarehouseQuickHudLogisticsText(), WarehouseQuickHudAmberColor);

        SetWarehouseQuickHudResourcePair(
            warehouseQuickHudLogsBoardsRow,
            "\u0411\u0440\u0451\u0432\u043d\u0430",
            GetWarehouseTradeResourceAmount(TradeResourceType.Logs).ToString(),
            "\u0414\u043e\u0441\u043a\u0438",
            GetWarehouseTradeResourceAmount(TradeResourceType.Boards).ToString());
        SetWarehouseQuickHudResourcePair(
            warehouseQuickHudCottonTextileRow,
            "\u0425\u043b\u043e\u043f\u043e\u043a",
            GetWarehouseTradeResourceAmount(TradeResourceType.Cotton).ToString(),
            "\u0422\u043a\u0430\u043d\u044c",
            GetWarehouseTradeResourceAmount(TradeResourceType.Textile).ToString());
        SetWarehouseQuickHudResourcePair(
            warehouseQuickHudFurnitureRow,
            "\u041c\u0435\u0431\u0435\u043b\u044c",
            GetWarehouseTradeResourceAmount(TradeResourceType.Furniture).ToString(),
            string.Empty,
            string.Empty);

        UpdateWarehouseQuickHudRootHeight();
    }

    private void UpdateWarehouseBuildingQuickHudChrome()
    {
        ApplyCityHallQuickHudPanelStyle(buildingQuickHud.Root, CityHallQuickHudPanelColor);
        if (buildingQuickHud.HeaderIconImage != null)
        {
            buildingQuickHud.HeaderIconImage.sprite = CreateWarehouseQuickHudIconSprite(
                WarehouseQuickHudIconKind.Building,
                Color.white,
                Color.clear,
                48,
                false);
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

    private void UpdateWarehouseBuildingWorkerSectionChrome()
    {
        if (buildingQuickHud?.WorkerSlotsSectionHeader != null)
        {
            buildingQuickHud.WorkerSlotsSectionHeader.color = CityHallQuickHudSecondaryTextColor;
            buildingQuickHud.WorkerSlotsSectionHeader.fontStyle = FontStyle.Bold;
        }

        if (buildingQuickHud?.WorkerSlotsEmptyText != null)
        {
            buildingQuickHud.WorkerSlotsEmptyText.color = CityHallQuickHudSecondaryTextColor;
        }
    }

    private void UpdateWarehouseQuickHudRootHeight()
    {
        if (buildingQuickHud?.Root == null)
        {
            return;
        }

        int workerEntries = CollectBuildingQuickHudWorkerEntries(LocationType.Warehouse, true).Count;
        float height = workerEntries <= 0
            ? 430f
            : Mathf.Min(536f, 430f + workerEntries * 54f);
        buildingQuickHud.Root.sizeDelta = new Vector2(330f, height);
    }

    private void GetWarehouseQuickHudStatus(int loadersOnShift, int assignedLoaders, out string status, out Color color)
    {
        if (loadersOnShift > 0)
        {
            status = "\u0420\u0430\u0431\u043e\u0442\u0430\u0435\u0442";
            color = WarehouseQuickHudGreenColor;
            return;
        }

        if (assignedLoaders > 0)
        {
            status = "\u041e\u0436\u0438\u0434\u0430\u0435\u0442 \u0441\u043c\u0435\u043d\u0443";
            color = WarehouseQuickHudAmberColor;
            return;
        }

        status = "\u041d\u0435\u0442 \u0433\u0440\u0443\u0437\u0447\u0438\u043a\u043e\u0432";
        color = WarehouseQuickHudRedColor;
    }

    private string GetWarehouseQuickHudLogisticsText()
    {
        switch (activeTruckInteraction)
        {
            case TruckInteractionType.LoadBoardsAtWarehouse:
            case TruckInteractionType.LoadTextileAtWarehouse:
            case TruckInteractionType.LoadLogsAtWarehouse:
            case TruckInteractionType.LoadFurnitureAtWarehouse:
            case TruckInteractionType.TradeLoadAtWarehouse:
                return "\u041f\u043e\u0433\u0440\u0443\u0437\u043a\u0430";
            case TruckInteractionType.UnloadAtWarehouse:
            case TruckInteractionType.UnloadFurnitureAtWarehouse:
            case TruckInteractionType.TradeUnloadAtWarehouse:
            case TruckInteractionType.UnloadDocksImportAtWarehouse:
                return "\u0420\u0430\u0437\u0433\u0440\u0443\u0437\u043a\u0430";
        }

        if (HasActiveTradeRun() &&
            (activeTradeRun.Phase == TradeRunPhase.DrivingToWarehouse ||
             activeTradeRun.Phase == TradeRunPhase.LoadingAtWarehouse ||
             activeTradeRun.Phase == TradeRunPhase.ReturningToWarehouse ||
             activeTradeRun.Phase == TradeRunPhase.UnloadingAtWarehouse))
        {
            return "\u0422\u043e\u0440\u0433\u043e\u0432\u044b\u0439 \u0440\u0435\u0439\u0441";
        }

        return "\u041d\u0435\u0442 \u0430\u043a\u0442\u0438\u0432\u043d\u044b\u0445";
    }

    private WarehouseQuickHudRowUi CreateWarehouseQuickHudRow(Transform parent, Font uiFont, WarehouseQuickHudIconKind icon, Color iconColor)
    {
        RectTransform row = CreateLayoutRow($"Warehouse{icon}Row", parent, 25f, 8f);
        row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

        RectTransform iconRoot = CreateUiObject("Icon", row).GetComponent<RectTransform>();
        LayoutElement iconLayout = iconRoot.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 22f;
        iconLayout.preferredHeight = 22f;
        Image iconImage = iconRoot.gameObject.AddComponent<Image>();
        iconImage.sprite = CreateWarehouseQuickHudIconSprite(icon, iconColor, Color.clear, 24, false);
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        Text label = CreateBodyText("Label", row, uiFont, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        Text value = CreateBodyText("Value", row, uiFont, string.Empty, 13, TextAnchor.MiddleRight, Color.white);
        value.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;
        value.horizontalOverflow = HorizontalWrapMode.Wrap;
        value.verticalOverflow = VerticalWrapMode.Truncate;

        return new WarehouseQuickHudRowUi
        {
            LabelText = label,
            ValueText = value
        };
    }

    private static void SetWarehouseQuickHudRow(WarehouseQuickHudRowUi row, string label, string value, Color valueColor)
    {
        if (row == null)
        {
            return;
        }

        row.LabelText.text = label;
        row.ValueText.text = value;
        row.ValueText.color = valueColor;
    }

    private WarehouseQuickHudResourcePairUi CreateWarehouseQuickHudResourcePairRow(Transform parent, Font uiFont)
    {
        RectTransform row = CreateLayoutRow("WarehouseResourcePairRow", parent, 21f, 7f);
        row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

        Text leftLabel = CreateBodyText("LeftLabel", row, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, CityHallQuickHudSecondaryTextColor);
        leftLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 55f;
        Text leftValue = CreateBodyText("LeftValue", row, uiFont, string.Empty, 12, TextAnchor.MiddleRight, Color.white);
        leftValue.fontStyle = FontStyle.Bold;
        leftValue.gameObject.AddComponent<LayoutElement>().preferredWidth = 34f;
        Text rightLabel = CreateBodyText("RightLabel", row, uiFont, string.Empty, 12, TextAnchor.MiddleLeft, CityHallQuickHudSecondaryTextColor);
        rightLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 55f;
        Text rightValue = CreateBodyText("RightValue", row, uiFont, string.Empty, 12, TextAnchor.MiddleRight, Color.white);
        rightValue.fontStyle = FontStyle.Bold;
        rightValue.gameObject.AddComponent<LayoutElement>().preferredWidth = 34f;

        return new WarehouseQuickHudResourcePairUi
        {
            LeftLabelText = leftLabel,
            LeftValueText = leftValue,
            RightLabelText = rightLabel,
            RightValueText = rightValue
        };
    }

    private static void SetWarehouseQuickHudResourcePair(
        WarehouseQuickHudResourcePairUi row,
        string leftLabel,
        string leftValue,
        string rightLabel,
        string rightValue)
    {
        if (row == null)
        {
            return;
        }

        row.LeftLabelText.text = leftLabel;
        row.LeftValueText.text = leftValue;
        row.RightLabelText.text = rightLabel;
        row.RightValueText.text = rightValue;
        row.RightLabelText.gameObject.SetActive(!string.IsNullOrEmpty(rightLabel));
        row.RightValueText.gameObject.SetActive(!string.IsNullOrEmpty(rightValue));
    }

    private static Sprite CreateWarehouseQuickHudIconSprite(
        WarehouseQuickHudIconKind kind,
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
            case WarehouseQuickHudIconKind.Building:
                PaintWarehouseBuildingIcon(pixels, size, primary);
                break;
            case WarehouseQuickHudIconKind.Crate:
                PaintWarehouseCrateIcon(pixels, size, primary);
                break;
            case WarehouseQuickHudIconKind.Status:
                PaintWarehouseStatusIcon(pixels, size, primary);
                break;
            case WarehouseQuickHudIconKind.People:
                PaintCityHallPeopleIcon(pixels, size, primary);
                break;
            case WarehouseQuickHudIconKind.Truck:
                PaintWarehouseTruckIcon(pixels, size, primary);
                break;
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static void PaintWarehouseBuildingIcon(Color[] pixels, int size, Color color)
    {
        DrawCityHallIconRect(pixels, size, 6, 7, size - 12, size - 15, color, 3);
        FillCityHallIconRect(pixels, size, 4, size - 10, size - 8, 4, color);
        FillCityHallIconRect(pixels, size, 10, 10, 5, size - 19, color);
        FillCityHallIconRect(pixels, size, 18, 10, 5, size - 19, color);
        FillCityHallIconRect(pixels, size, size - 15, 10, 5, size - 19, color);
        FillCityHallIconRect(pixels, size, size - 12, size - 20, 5, 4, WarehouseQuickHudBlueColor);
    }

    private static void PaintWarehouseCrateIcon(Color[] pixels, int size, Color color)
    {
        int x = size / 4;
        int y = size / 4;
        int w = size / 2;
        int h = size / 2;
        DrawCityHallIconRect(pixels, size, x, y, w, h, color, 3);
        DrawCityHallIconLine(pixels, size, x, y + h, x + w / 2, y + h - 8, color, 2);
        DrawCityHallIconLine(pixels, size, x + w, y + h, x + w / 2, y + h - 8, color, 2);
        FillCityHallIconRect(pixels, size, x + w / 2 - 2, y + 2, 4, h - 4, color);
    }

    private static void PaintWarehouseStatusIcon(Color[] pixels, int size, Color color)
    {
        DrawCityHallIconCircle(pixels, size, size / 2, size / 2, size / 2 - 5, color, 3);
        DrawCityHallIconLine(pixels, size, size / 2 - 7, size / 2, size / 2 - 2, size / 2 - 6, color, 3);
        DrawCityHallIconLine(pixels, size, size / 2 - 2, size / 2 - 6, size / 2 + 8, size / 2 + 7, color, 3);
    }

    private static void PaintWarehouseTruckIcon(Color[] pixels, int size, Color color)
    {
        FillCityHallIconRect(pixels, size, 4, 11, size - 15, 10, color);
        FillCityHallIconRect(pixels, size, size - 14, 14, 9, 7, color);
        FillCityHallIconRect(pixels, size, size - 12, 17, 5, 3, Color.clear);
        FillCityHallIconCircle(pixels, size, 10, 8, 4, color);
        FillCityHallIconCircle(pixels, size, size - 10, 8, 4, color);
    }
}
