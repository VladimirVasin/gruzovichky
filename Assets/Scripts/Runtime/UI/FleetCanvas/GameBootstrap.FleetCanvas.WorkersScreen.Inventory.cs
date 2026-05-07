using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static Sprite s_workerInventorySnackIcon;
    private static Sprite s_workerInventoryCoffeeIcon;
    private static Sprite s_workerInventoryWarningIcon;
    private static Sprite s_workerInventoryFoodIcon;
    private static Sprite s_workerInventoryEnergyIcon;
    private static Sprite s_workerInventoryAutoIcon;

    private void SetupWorkerInventoryUi(RectTransform inventoryTabRoot, Font font)
    {
        RectTransform inventoryContent = CreateVerticalStack(
            "WorkerInventoryContent",
            inventoryTabRoot,
            new RectOffset(6, 6, 4, 0),
            36f,
            flexibleHeight: 1f);

        driversScreenUi.DetailInventoryTitleText = CreateHeaderText(
            "WorkerInventoryTitle",
            inventoryContent,
            font,
            string.Empty,
            21,
            TextAnchor.MiddleLeft,
            FleetAccentColor);
        driversScreenUi.DetailInventoryTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        RectTransform cardRow = CreateLayoutRow("WorkerInventoryAutoConsumableRow", inventoryContent, 292f, 38f);
        driversScreenUi.DetailInventoryCardRowRoot = cardRow.gameObject;
        HorizontalLayoutGroup rowLayout = cardRow.GetComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.UpperLeft;
        rowLayout.childForceExpandWidth = false;

        driversScreenUi.DetailSnackCard = CreateWorkerAutoConsumableCard(
            cardRow,
            font,
            "WorkerSnackCard",
            WorkerSnackItemId,
            GetWorkerInventorySnackIcon(),
            GetWorkerInventoryFoodIcon());

        driversScreenUi.DetailCoffeeCard = CreateWorkerAutoConsumableCard(
            cardRow,
            font,
            "WorkerCoffeeCard",
            WorkerCoffeeItemId,
            GetWorkerInventoryCoffeeIcon(),
            GetWorkerInventoryEnergyIcon());

        driversScreenUi.DetailInventoryEmptyText = CreateBodyText(
            "WorkerInventoryEmpty",
            inventoryContent,
            font,
            string.Empty,
            15,
            TextAnchor.UpperLeft,
            FleetMutedTextColor);
        driversScreenUi.DetailInventoryEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        driversScreenUi.DetailInventoryEmptyText.gameObject.SetActive(false);
    }

    private WorkerAutoConsumableCardUi CreateWorkerAutoConsumableCard(
        Transform parent,
        Font font,
        string name,
        string itemId,
        Sprite itemIcon,
        Sprite effectIcon)
    {
        RectTransform card = CreateResidentHudPanel(name, parent, new Color(0.045f, 0.095f, 0.15f, 0.96f), ResidentHudBorderColor);
        LayoutElement cardLayoutElement = card.gameObject.AddComponent<LayoutElement>();
        cardLayoutElement.preferredWidth = 410f;
        cardLayoutElement.preferredHeight = 292f;
        cardLayoutElement.minWidth = 360f;
        Image background = card.GetComponent<Image>();
        if (background != null)
        {
            background.raycastTarget = false;
        }

        VerticalLayoutGroup cardLayout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(22, 22, 22, 20);
        cardLayout.spacing = 16f;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        RectTransform header = CreateLayoutRow($"{name}Header", card, 58f, 14f);
        header.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;

        Image itemImage = CreateWorkerInventoryIconImage("ItemIcon", header, itemIcon, 32f, Color.white);

        RectTransform titleStack = CreateVerticalStack(
            $"{name}TitleStack",
            header,
            new RectOffset(),
            4f,
            preferredHeight: 54f,
            flexibleWidth: 1f);
        Text nameText = CreateHeaderText(
            "Name",
            titleStack,
            font,
            itemId == WorkerCoffeeItemId ? "Coffee" : "Snack",
            20,
            TextAnchor.LowerLeft,
            Color.white);
        nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        Text typeText = CreateBodyText("Type", titleStack, font, string.Empty, 15, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        typeText.horizontalOverflow = HorizontalWrapMode.Overflow;
        typeText.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        RectTransform badge = CreateResidentHudPanel($"{name}QuantityBadge", header, new Color(0.035f, 0.075f, 0.12f, 0.94f), ResidentHudBorderColor);
        LayoutElement badgeLayout = badge.gameObject.AddComponent<LayoutElement>();
        badgeLayout.preferredWidth = 54f;
        badgeLayout.preferredHeight = 42f;
        Image badgeImage = badge.GetComponent<Image>();
        if (badgeImage != null)
        {
            badgeImage.raycastTarget = false;
        }

        Text quantityText = CreateHeaderText("Quantity", badge, font, "x0", 18, TextAnchor.MiddleCenter, FleetAccentColor);
        StretchRect(quantityText.rectTransform, 0f, 0f, 0f, 0f);

        CreateWorkerInventoryDivider($"{name}DividerA", card);
        Text triggerText = CreateWorkerInventoryInfoRow(
            $"{name}Trigger",
            card,
            font,
            GetWorkerInventoryWarningIcon(),
            new Color(0.94f, 0.34f, 0.24f, 1f),
            out Image triggerIcon);
        Text effectText = CreateWorkerInventoryInfoRow(
            $"{name}Effect",
            card,
            font,
            effectIcon,
            ResidentHudPositiveColor,
            out Image effectIconImage);

        CreateWorkerInventoryDivider($"{name}DividerB", card);
        Text autoText = CreateWorkerInventoryInfoRow(
            $"{name}AutoUse",
            card,
            font,
            GetWorkerInventoryAutoIcon(),
            FleetMutedTextColor,
            out Image autoIcon);

        return new WorkerAutoConsumableCardUi
        {
            Root = card,
            Background = background,
            ItemIcon = itemImage,
            TriggerIcon = triggerIcon,
            EffectIcon = effectIconImage,
            AutoIcon = autoIcon,
            NameText = nameText,
            QuantityText = quantityText,
            TypeText = typeText,
            TriggerText = triggerText,
            EffectText = effectText,
            AutoText = autoText
        };
    }

    private Text CreateWorkerInventoryInfoRow(
        string name,
        Transform parent,
        Font font,
        Sprite iconSprite,
        Color textColor,
        out Image iconImage)
    {
        RectTransform row = CreateLayoutRow(name, parent, 30f, 12f);
        row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
        iconImage = CreateWorkerInventoryIconImage("Icon", row, iconSprite, 20f, textColor);
        Text text = CreateBodyText("Label", row, font, string.Empty, 16, TextAnchor.MiddleLeft, textColor);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return text;
    }

    private Image CreateWorkerInventoryIconImage(string name, Transform parent, Sprite sprite, float size, Color color)
    {
        RectTransform icon = CreateUiObject(name, parent).GetComponent<RectTransform>();
        LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = size;
        iconLayout.preferredHeight = size;
        iconLayout.minWidth = size;
        iconLayout.minHeight = size;
        Image image = icon.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return image;
    }

    private void CreateWorkerInventoryDivider(string name, Transform parent)
    {
        RectTransform divider = CreateUiObject(name, parent).GetComponent<RectTransform>();
        divider.gameObject.AddComponent<LayoutElement>().preferredHeight = 1f;
        Image image = divider.gameObject.AddComponent<Image>();
        image.color = new Color(0.47f, 0.63f, 0.78f, 0.16f);
        image.raycastTarget = false;
    }

    private void UpdateWorkerInventoryUi(DriverAgent worker, bool ru)
    {
        if (driversScreenUi == null)
        {
            return;
        }

        int snackQuantity = GetWorkerInventoryItemQuantity(worker, WorkerSnackItemId);
        int coffeeQuantity = GetWorkerInventoryItemQuantity(worker, WorkerCoffeeItemId);
        int visibleQuantity = Mathf.Max(0, snackQuantity) + Mathf.Max(0, coffeeQuantity);
        bool hasSnack = snackQuantity > 0;
        bool hasCoffee = coffeeQuantity > 0;
        bool hasVisibleItems = hasSnack || hasCoffee;

        if (driversScreenUi.DetailInventoryTitleText != null)
        {
            driversScreenUi.DetailInventoryTitleText.text = ru
                ? $"\u0418\u043d\u0432\u0435\u043d\u0442\u0430\u0440\u044c ({visibleQuantity}/{WorkerInventoryMaxStacks})"
                : $"Inventory ({visibleQuantity}/{WorkerInventoryMaxStacks})";
        }

        if (driversScreenUi.DetailInventoryCardRowRoot != null)
        {
            driversScreenUi.DetailInventoryCardRowRoot.SetActive(hasVisibleItems);
        }

        if (driversScreenUi.DetailInventoryEmptyText != null)
        {
            driversScreenUi.DetailInventoryEmptyText.gameObject.SetActive(!hasVisibleItems);
            driversScreenUi.DetailInventoryEmptyText.text = ru
                ? "\u0421\u0435\u0439\u0447\u0430\u0441 \u0432 \u0438\u043d\u0432\u0435\u043d\u0442\u0430\u0440\u0435 \u043d\u0435\u0442 \u0430\u0432\u0442\u043e\u0440\u0430\u0441\u0445\u043e\u0434\u043d\u0438\u043a\u043e\u0432."
                : "There are no auto-consumables in stock right now.";
        }

        if (driversScreenUi.DetailSnackCard?.Root != null)
        {
            driversScreenUi.DetailSnackCard.Root.gameObject.SetActive(hasSnack);
        }

        if (driversScreenUi.DetailCoffeeCard?.Root != null)
        {
            driversScreenUi.DetailCoffeeCard.Root.gameObject.SetActive(hasCoffee);
        }

        UpdateWorkerAutoConsumableCard(
            driversScreenUi.DetailSnackCard,
            "Snack",
            snackQuantity,
            ru ? "\u0410\u0432\u0442\u043e\u0440\u0430\u0441\u0445\u043e\u0434\u043d\u0438\u043a" : "Auto consumable",
            ru ? "\u041f\u0440\u0438 \u043a\u0440\u0438\u0442\u0438\u0447\u0435\u0441\u043a\u043e\u043c \u0433\u043e\u043b\u043e\u0434\u0435" : "At critical hunger",
            ru ? "+\u041d\u0435\u043c\u043d\u043e\u0433\u043e \u0435\u0434\u044b" : "+Some food",
            ru ? "\u0410\u0432\u0442\u043e\u0438\u0441\u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u043d\u0438\u0435" : "Auto-use");

        UpdateWorkerAutoConsumableCard(
            driversScreenUi.DetailCoffeeCard,
            "Coffee",
            coffeeQuantity,
            ru ? "\u0410\u0432\u0442\u043e\u0440\u0430\u0441\u0445\u043e\u0434\u043d\u0438\u043a" : "Auto consumable",
            ru ? "\u041f\u0440\u0438 \u043a\u0440\u0438\u0442\u0438\u0447\u0435\u0441\u043a\u043e\u0439 \u0443\u0441\u0442\u0430\u043b\u043e\u0441\u0442\u0438" : "At critical fatigue",
            ru ? "+\u041d\u0435\u043c\u043d\u043e\u0433\u043e \u0431\u043e\u0434\u0440\u043e\u0441\u0442\u0438" : "+Some energy",
            ru ? "\u0410\u0432\u0442\u043e\u0438\u0441\u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u043d\u0438\u0435" : "Auto-use");
    }

    private void UpdateWorkerAutoConsumableCard(
        WorkerAutoConsumableCardUi card,
        string name,
        int quantity,
        string typeLabel,
        string triggerLabel,
        string effectLabel,
        string autoLabel)
    {
        if (card == null)
        {
            return;
        }

        bool hasItem = quantity > 0;
        Color mainText = hasItem ? Color.white : FleetMutedTextColor;
        Color secondaryText = hasItem ? FleetSecondaryTextColor : new Color(FleetMutedTextColor.r, FleetMutedTextColor.g, FleetMutedTextColor.b, 0.72f);
        Color iconColor = hasItem ? Color.white : new Color(1f, 1f, 1f, 0.38f);
        Color warningColor = hasItem ? new Color(0.94f, 0.34f, 0.24f, 1f) : FleetMutedTextColor;
        Color positiveColor = hasItem ? ResidentHudPositiveColor : FleetMutedTextColor;
        Color mutedColor = hasItem ? FleetMutedTextColor : new Color(FleetMutedTextColor.r, FleetMutedTextColor.g, FleetMutedTextColor.b, 0.58f);

        if (card.Background != null)
        {
            card.Background.color = hasItem
                ? new Color(0.045f, 0.095f, 0.15f, 0.96f)
                : new Color(0.04f, 0.075f, 0.11f, 0.78f);
        }

        if (card.ItemIcon != null) card.ItemIcon.color = iconColor;
        if (card.TriggerIcon != null) card.TriggerIcon.color = warningColor;
        if (card.EffectIcon != null) card.EffectIcon.color = positiveColor;
        if (card.AutoIcon != null) card.AutoIcon.color = mutedColor;
        if (card.NameText != null)
        {
            card.NameText.text = name;
            card.NameText.color = mainText;
        }
        if (card.QuantityText != null)
        {
            card.QuantityText.text = $"x{Mathf.Max(0, quantity)}";
            card.QuantityText.color = hasItem ? FleetAccentColor : FleetMutedTextColor;
        }
        if (card.TypeText != null)
        {
            card.TypeText.text = typeLabel;
            card.TypeText.color = secondaryText;
        }
        if (card.TriggerText != null)
        {
            card.TriggerText.text = triggerLabel;
            card.TriggerText.color = warningColor;
        }
        if (card.EffectText != null)
        {
            card.EffectText.text = effectLabel;
            card.EffectText.color = positiveColor;
        }
        if (card.AutoText != null)
        {
            card.AutoText.text = autoLabel;
            card.AutoText.color = mutedColor;
        }
    }

    private static Sprite GetWorkerInventorySnackIcon() =>
        s_workerInventorySnackIcon ??= BuildWorkerInventorySprite(24, PaintWorkerInventorySnackIcon);

    private static Sprite GetWorkerInventoryCoffeeIcon() =>
        s_workerInventoryCoffeeIcon ??= BuildWorkerInventorySprite(24, PaintWorkerInventoryCoffeeIcon);

    private static Sprite GetWorkerInventoryWarningIcon() =>
        s_workerInventoryWarningIcon ??= BuildWorkerInventorySprite(16, PaintWorkerInventoryWarningIcon);

    private static Sprite GetWorkerInventoryFoodIcon() =>
        s_workerInventoryFoodIcon ??= BuildWorkerInventorySprite(16, PaintWorkerInventoryFoodIcon);

    private static Sprite GetWorkerInventoryEnergyIcon() =>
        s_workerInventoryEnergyIcon ??= BuildWorkerInventorySprite(16, PaintWorkerInventoryEnergyIcon);

    private static Sprite GetWorkerInventoryAutoIcon() =>
        s_workerInventoryAutoIcon ??= BuildWorkerInventorySprite(16, PaintWorkerInventoryAutoIcon);

    private static Sprite BuildWorkerInventorySprite(int size, System.Action<Color[], int> paintFn)
    {
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }

        paintFn(pixels, size);
        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static void WorkerInventoryIconSet(Color[] pixels, int size, int x, int y, Color color)
    {
        if (x >= 0 && x < size && y >= 0 && y < size)
        {
            pixels[y * size + x] = color;
        }
    }

    private static void WorkerInventoryIconRect(Color[] pixels, int size, int x, int y, int width, int height, Color color)
    {
        for (int yy = y; yy < y + height; yy++)
        {
            for (int xx = x; xx < x + width; xx++)
            {
                WorkerInventoryIconSet(pixels, size, xx, yy, color);
            }
        }
    }

    private static void PaintWorkerInventorySnackIcon(Color[] pixels, int size)
    {
        Color shadow = new(0.17f, 0.06f, 0.03f, 0.85f);
        Color wrapper = new(0.90f, 0.16f, 0.08f, 1f);
        Color wrapperDark = new(0.56f, 0.06f, 0.04f, 1f);
        Color label = new(1f, 0.75f, 0.12f, 1f);
        Color shine = new(1f, 0.95f, 0.62f, 1f);

        WorkerInventoryIconRect(pixels, size, 4, 8, 17, 10, shadow);
        WorkerInventoryIconRect(pixels, size, 3, 6, 18, 10, wrapper);
        WorkerInventoryIconRect(pixels, size, 3, 6, 3, 10, wrapperDark);
        WorkerInventoryIconRect(pixels, size, 18, 6, 3, 10, wrapperDark);
        WorkerInventoryIconRect(pixels, size, 8, 8, 8, 6, label);
        WorkerInventoryIconRect(pixels, size, 10, 9, 2, 2, shine);
        WorkerInventoryIconSet(pixels, size, 5, 5, shine);
        WorkerInventoryIconSet(pixels, size, 17, 16, shine);
    }

    private static void PaintWorkerInventoryCoffeeIcon(Color[] pixels, int size)
    {
        Color shadow = new(0.16f, 0.08f, 0.04f, 0.82f);
        Color cup = new(0.95f, 0.78f, 0.55f, 1f);
        Color rim = new(1f, 0.92f, 0.72f, 1f);
        Color coffee = new(0.22f, 0.10f, 0.04f, 1f);
        Color handle = new(0.86f, 0.62f, 0.38f, 1f);
        Color steam = new(0.95f, 0.91f, 0.82f, 0.92f);

        WorkerInventoryIconRect(pixels, size, 6, 14, 12, 3, shadow);
        WorkerInventoryIconRect(pixels, size, 5, 9, 12, 7, cup);
        WorkerInventoryIconRect(pixels, size, 5, 8, 12, 2, rim);
        WorkerInventoryIconRect(pixels, size, 7, 10, 8, 2, coffee);
        WorkerInventoryIconRect(pixels, size, 16, 10, 3, 5, handle);
        WorkerInventoryIconRect(pixels, size, 18, 11, 2, 3, Color.clear);
        WorkerInventoryIconSet(pixels, size, 8, 5, steam);
        WorkerInventoryIconSet(pixels, size, 9, 4, steam);
        WorkerInventoryIconSet(pixels, size, 12, 5, steam);
        WorkerInventoryIconSet(pixels, size, 13, 4, steam);
    }

    private static void PaintWorkerInventoryWarningIcon(Color[] pixels, int size)
    {
        Color fill = new(0.94f, 0.34f, 0.24f, 1f);
        Color mark = new(0.10f, 0.05f, 0.04f, 1f);
        for (int y = 2; y <= 13; y++)
        {
            int halfWidth = Mathf.Max(1, (y - 1) / 2);
            for (int x = 8 - halfWidth; x <= 8 + halfWidth; x++)
            {
                WorkerInventoryIconSet(pixels, size, x, y, fill);
            }
        }

        WorkerInventoryIconRect(pixels, size, 7, 6, 2, 5, mark);
        WorkerInventoryIconRect(pixels, size, 7, 12, 2, 2, mark);
    }

    private static void PaintWorkerInventoryFoodIcon(Color[] pixels, int size)
    {
        Color green = new(0.48f, 0.85f, 0.35f, 1f);
        int[,] points =
        {
            { 4, 4 }, { 5, 3 }, { 6, 3 }, { 7, 4 }, { 8, 5 },
            { 9, 4 }, { 10, 3 }, { 11, 3 }, { 12, 4 },
            { 3, 5 }, { 4, 6 }, { 5, 7 }, { 6, 8 }, { 7, 9 },
            { 8, 10 }, { 9, 9 }, { 10, 8 }, { 11, 7 }, { 12, 6 }, { 13, 5 }
        };

        for (int i = 0; i < points.GetLength(0); i++)
        {
            WorkerInventoryIconSet(pixels, size, points[i, 0], points[i, 1], green);
            WorkerInventoryIconSet(pixels, size, points[i, 0], points[i, 1] + 1, green);
        }
    }

    private static void PaintWorkerInventoryEnergyIcon(Color[] pixels, int size)
    {
        Color green = new(0.48f, 0.85f, 0.35f, 1f);
        WorkerInventoryIconRect(pixels, size, 8, 1, 3, 5, green);
        WorkerInventoryIconRect(pixels, size, 6, 5, 5, 3, green);
        WorkerInventoryIconRect(pixels, size, 5, 8, 4, 3, green);
        WorkerInventoryIconRect(pixels, size, 7, 10, 3, 5, green);
        WorkerInventoryIconRect(pixels, size, 9, 8, 3, 3, green);
    }

    private static void PaintWorkerInventoryAutoIcon(Color[] pixels, int size)
    {
        Color muted = new(0.56f, 0.63f, 0.74f, 1f);
        Color clear = Color.clear;
        WorkerInventoryIconRect(pixels, size, 6, 1, 4, 2, muted);
        WorkerInventoryIconRect(pixels, size, 6, 13, 4, 2, muted);
        WorkerInventoryIconRect(pixels, size, 1, 6, 2, 4, muted);
        WorkerInventoryIconRect(pixels, size, 13, 6, 2, 4, muted);

        for (int y = 3; y <= 12; y++)
        {
            for (int x = 3; x <= 12; x++)
            {
                float dx = x - 7.5f;
                float dy = y - 7.5f;
                float d = dx * dx + dy * dy;
                if (d >= 13f && d <= 24f)
                {
                    WorkerInventoryIconSet(pixels, size, x, y, muted);
                }
            }
        }

        WorkerInventoryIconRect(pixels, size, 7, 7, 2, 2, clear);
    }
}
