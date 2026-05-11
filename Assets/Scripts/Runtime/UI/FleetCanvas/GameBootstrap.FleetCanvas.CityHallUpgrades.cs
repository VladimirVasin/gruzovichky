using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class CityHallUpgradeCardUi
    {
        public CityUpgradeId UpgradeId;
        public RectTransform Root;
        public Image Background;
        public Outline Outline;
        public Text TitleText;
        public Text DescriptionText;
        public Text MetaText;
        public Text StatusText;
        public Button BuyButton;
        public Text BuyButtonText;
        public Image ConnectionLine;
    }

    private sealed partial class CityHallScreenUiRefs
    {
        public RectTransform UpgradesRoot;
        public Text UpgradeHintText;
        public Text[] UpgradeBranchLabels;
        public readonly List<CityHallUpgradeCardUi> UpgradeCards = new();
    }

    private void SetupCityHallUpgradeTreeUi(RectTransform window, Font font)
    {
        RectTransform root = CreateStyledPanel("CityHallUpgradesRoot", window, FleetInsetColor);
        LayoutElement rootLayout = root.gameObject.AddComponent<LayoutElement>();
        rootLayout.minHeight = 390f;
        rootLayout.flexibleHeight = 1f;
        cityHallScreenUi.UpgradesRoot = root;

        cityHallScreenUi.UpgradeHintText = CreateBodyText("CityUpgradeHint", root, font, string.Empty, 12, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
        SetCityUpgradeTopStretchRect(cityHallScreenUi.UpgradeHintText.rectTransform, 16f, -14f, 28f);

        CityUpgradeBranch[] branches =
        {
            CityUpgradeBranch.Cleanliness,
            CityUpgradeBranch.Economy,
            CityUpgradeBranch.Trust
        };
        cityHallScreenUi.UpgradeBranchLabels = new Text[branches.Length];
        for (int i = 0; i < branches.Length; i++)
        {
            Text label = CreateBodyText($"CityUpgradeBranch_{branches[i]}", root, font, string.Empty, 14, TextAnchor.MiddleCenter, Color.white);
            label.fontStyle = FontStyle.Bold;
            SetCityUpgradeRect(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(GetCityUpgradeBranchX(branches[i]), 166f), new Vector2(292f, 24f));
            cityHallScreenUi.UpgradeBranchLabels[i] = label;
        }

        for (int i = 0; i < CityUpgradeDefinitions.Length; i++)
        {
            CityUpgradeDefinition definition = CityUpgradeDefinitions[i];
            CityHallUpgradeCardUi card = CreateCityHallUpgradeCard(root, font, definition);
            cityHallScreenUi.UpgradeCards.Add(card);
        }

        root.gameObject.SetActive(false);
    }

    private CityHallUpgradeCardUi CreateCityHallUpgradeCard(RectTransform parent, Font font, CityUpgradeDefinition definition)
    {
        float x = GetCityUpgradeBranchX(definition.Branch);
        float y = definition.Level == 0 ? 66f : -118f;

        Image connectionLine = null;
        if (definition.Parent.HasValue)
        {
            RectTransform lineRect = CreateUiObject($"CityUpgradeLine_{definition.Id}", parent).GetComponent<RectTransform>();
            SetCityUpgradeRect(lineRect, new Vector2(0.5f, 0.5f), new Vector2(x, -26f), new Vector2(5f, 26f));
            connectionLine = lineRect.gameObject.AddComponent<Image>();
            connectionLine.raycastTarget = false;
        }

        RectTransform card = CreateStyledPanel($"CityUpgradeCard_{definition.Id}", parent, FleetCardMutedColor);
        SetCityUpgradeRect(card, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(300f, 160f));
        VerticalLayoutGroup layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Image background = card.GetComponent<Image>();
        Outline outline = card.GetComponent<Outline>();

        Text title = CreateBodyText("Title", card, font, string.Empty, 14, TextAnchor.MiddleLeft, Color.white);
        title.fontStyle = FontStyle.Bold;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        Text description = CreateBodyText("Description", card, font, string.Empty, 11, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        description.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;

        Text meta = CreateBodyText("Meta", card, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetAccentColor);
        meta.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        Text status = CreateBodyText("Status", card, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
        status.fontStyle = FontStyle.Bold;
        status.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        Button buyButton = CreateButton("BuyButton", card, font, out Text buyText, string.Empty, 12, FleetPrimaryButtonColor, Color.white);
        LayoutElement buyLayout = buyButton.gameObject.AddComponent<LayoutElement>();
        buyLayout.preferredHeight = 28f;
        buyLayout.minHeight = 28f;
        CityUpgradeId capturedId = definition.Id;
        buyButton.onClick.AddListener(() => OnCityUpgradeBuyButtonClicked(capturedId));

        return new CityHallUpgradeCardUi
        {
            UpgradeId = definition.Id,
            Root = card,
            Background = background,
            Outline = outline,
            TitleText = title,
            DescriptionText = description,
            MetaText = meta,
            StatusText = status,
            BuyButton = buyButton,
            BuyButtonText = buyText,
            ConnectionLine = connectionLine
        };
    }

    private void ApplyCityHallTabVisuals(bool ru)
    {
        if (cityHallScreenUi == null)
        {
            return;
        }

        cityHallScreenUi.RequestsTabText.text = ru ? "Обращения" : "Requests";
        cityHallScreenUi.UpgradesTabText.text = ru ? "Апдейты" : "Upgrades";
        ApplyShiftsTabVisual(cityHallScreenUi.RequestsTabButton, cityHallScreenUi.RequestsTabText, !isCityHallUpgradesTabActive);
        ApplyShiftsTabVisual(cityHallScreenUi.UpgradesTabButton, cityHallScreenUi.UpgradesTabText, isCityHallUpgradesTabActive);
    }

    private void RebuildCityHallUpgradeTree(bool ru)
    {
        if (cityHallScreenUi?.UpgradesRoot == null)
        {
            return;
        }

        cityHallScreenUi.UpgradeHintText.text = ru
            ? "Дерево городских апдейтов: покупается за казну, открывается уровнем доверия."
            : "City upgrade tree: paid from treasury, unlocked by trust.";

        if (cityHallScreenUi.UpgradeBranchLabels != null && cityHallScreenUi.UpgradeBranchLabels.Length >= 3)
        {
            cityHallScreenUi.UpgradeBranchLabels[0].text = ru ? "Чистота" : "Cleanliness";
            cityHallScreenUi.UpgradeBranchLabels[1].text = ru ? "Экономика" : "Economy";
            cityHallScreenUi.UpgradeBranchLabels[2].text = ru ? "Доверие" : "Trust";
        }

        for (int i = 0; i < cityHallScreenUi.UpgradeCards.Count; i++)
        {
            CityHallUpgradeCardUi card = cityHallScreenUi.UpgradeCards[i];
            if (card == null || !TryGetCityUpgradeDefinition(card.UpgradeId, out CityUpgradeDefinition definition))
            {
                continue;
            }

            bool purchased = HasCityUpgrade(definition.Id);
            bool parentMissing = definition.Parent.HasValue && !HasCityUpgrade(definition.Parent.Value);
            bool trustLocked = !purchased && !parentMissing && cityTrust < definition.RequiredTrust;
            bool noFunds = !purchased && !parentMissing && !trustLocked && money < definition.Cost;
            bool available = !purchased && !parentMissing && !trustLocked && !noFunds;

            card.TitleText.text = ru ? definition.TitleRu : definition.TitleEn;
            card.DescriptionText.text = ru ? definition.DescriptionRu : definition.DescriptionEn;
            card.MetaText.text = ru
                ? $"${definition.Cost} | доверие {FormatCityUpgradeSignedValue(definition.RequiredTrust)}"
                : $"${definition.Cost} | trust {FormatCityUpgradeSignedValue(definition.RequiredTrust)}";

            card.StatusText.text = GetCityUpgradeStatusText(definition, purchased, parentMissing, trustLocked, noFunds, available, ru);
            card.StatusText.color = purchased
                ? new Color(0.55f, 0.92f, 0.50f, 1f)
                : available
                    ? new Color(0.82f, 0.95f, 1f, 1f)
                    : noFunds
                        ? new Color(1f, 0.72f, 0.38f, 1f)
                        : FleetMutedTextColor;

            Color cardColor = purchased
                ? new Color(0.12f, 0.27f, 0.20f, 1f)
                : available
                    ? new Color(0.14f, 0.22f, 0.32f, 1f)
                    : new Color(0.09f, 0.11f, 0.15f, 1f);
            card.Background.color = cardColor;
            if (card.Outline != null)
            {
                card.Outline.effectColor = purchased
                    ? new Color(0.25f, 0.84f, 0.40f, 0.55f)
                    : available
                        ? new Color(0.26f, 0.58f, 0.95f, 0.48f)
                        : new Color(0f, 0f, 0f, 0.22f);
            }

            card.BuyButton.interactable = available;
            card.BuyButtonText.text = purchased
                ? (ru ? "Куплено" : "Purchased")
                : available
                    ? (ru ? "Купить" : "Buy")
                    : noFunds
                        ? (ru ? "Нет денег" : "No funds")
                        : (ru ? "Закрыто" : "Locked");
            SetCityUpgradeButtonColor(card, purchased, available, noFunds);

            if (card.ConnectionLine != null)
            {
                bool parentPurchased = definition.Parent.HasValue && HasCityUpgrade(definition.Parent.Value);
                card.ConnectionLine.color = purchased
                    ? new Color(0.35f, 0.90f, 0.42f, 0.90f)
                    : parentPurchased
                        ? new Color(0.32f, 0.58f, 0.92f, 0.72f)
                        : new Color(0.34f, 0.40f, 0.48f, 0.46f);
            }
        }
    }

    private string GetCityUpgradeStatusText(
        CityUpgradeDefinition definition,
        bool purchased,
        bool parentMissing,
        bool trustLocked,
        bool noFunds,
        bool available,
        bool ru)
    {
        if (purchased)
        {
            return ru ? "Работает" : "Active";
        }

        if (parentMissing)
        {
            return ru
                ? $"Нужно: {GetCityUpgradeParentTitle(definition, true)}"
                : $"Requires: {GetCityUpgradeParentTitle(definition, false)}";
        }

        if (trustLocked)
        {
            return ru
                ? $"Доверие {FormatCityUpgradeSignedValue(cityTrust)} / {FormatCityUpgradeSignedValue(definition.RequiredTrust)}"
                : $"Trust {FormatCityUpgradeSignedValue(cityTrust)} / {FormatCityUpgradeSignedValue(definition.RequiredTrust)}";
        }

        if (noFunds)
        {
            return ru ? $"В казне ${money} / ${definition.Cost}" : $"Treasury ${money} / ${definition.Cost}";
        }

        if (available)
        {
            return ru ? "Доступно" : "Available";
        }

        return ru ? "Недоступно" : "Unavailable";
    }

    private string GetCityUpgradeParentTitle(CityUpgradeDefinition definition, bool ru)
    {
        if (definition?.Parent == null ||
            !TryGetCityUpgradeDefinition(definition.Parent.Value, out CityUpgradeDefinition parent))
        {
            return ru ? "предыдущий апдейт" : "previous upgrade";
        }

        return ru ? parent.TitleRu : parent.TitleEn;
    }

    private void SetCityUpgradeButtonColor(CityHallUpgradeCardUi card, bool purchased, bool available, bool noFunds)
    {
        Color baseColor = purchased
            ? new Color(0.18f, 0.44f, 0.26f, 1f)
            : available
                ? new Color(0.24f, 0.50f, 0.28f, 1f)
                : noFunds
                    ? new Color(0.46f, 0.30f, 0.16f, 1f)
                    : new Color(0.22f, 0.25f, 0.31f, 1f);

        if (card.BuyButton.targetGraphic is Image image)
        {
            image.color = baseColor;
        }

        ColorBlock colors = card.BuyButton.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = Color.Lerp(baseColor, Color.white, 0.14f);
        colors.pressedColor = Color.Lerp(baseColor, Color.black, 0.18f);
        colors.selectedColor = baseColor;
        colors.disabledColor = new Color(baseColor.r * 0.72f, baseColor.g * 0.72f, baseColor.b * 0.72f, 0.82f);
        card.BuyButton.colors = colors;
    }

    private void OnCityUpgradeBuyButtonClicked(CityUpgradeId upgradeId)
    {
        bool purchased = PurchaseCityUpgrade(upgradeId);
        PlayUiSound(purchased ? uiPanelOpenClip : uiPanelCloseClip, 0.58f);
        isCityHallScreenDirty = true;
    }

    private bool AreCityHallUpgradeClickTargetsReady()
    {
        if (cityHallScreenUi?.UpgradeCards == null)
        {
            return true;
        }

        bool ok = true;
        for (int i = 0; i < cityHallScreenUi.UpgradeCards.Count; i++)
        {
            ok &= IsButtonClickTargetReady(cityHallScreenUi.UpgradeCards[i]?.BuyButton);
        }

        return ok;
    }

    private static float GetCityUpgradeBranchX(CityUpgradeBranch branch)
    {
        return branch switch
        {
            CityUpgradeBranch.Cleanliness => -330f,
            CityUpgradeBranch.Economy => 0f,
            _ => 330f
        };
    }

    private static void SetCityUpgradeRect(RectTransform rect, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void SetCityUpgradeTopStretchRect(RectTransform rect, float horizontalInset, float y, float height)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(-horizontalInset * 2f, height);
    }
}
