using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private enum BuildingQuickHudSubmenuKind
    {
        None,
        MotelGuests
    }

    private sealed class BuildingQuickHudSubmenuRowUi
    {
        public RectTransform Root;
        public Image Background;
        public Text NameText;
        public Text MetaText;
        public Button Button;
        public int DriverId = -1;
    }

    private static readonly Color BuildingQuickHudSubmenuPanelColor = new(0.035f, 0.065f, 0.090f, 1f);
    private static readonly Color BuildingQuickHudSubmenuRowColor = new(0.070f, 0.105f, 0.145f, 1f);
    private static readonly Color BuildingQuickHudSubmenuRowHoverColor = new(0.105f, 0.150f, 0.205f, 1f);
    private static readonly Color BuildingQuickHudSubmenuBorderColor = new(0.47f, 0.63f, 0.78f, 0.26f);

    private const float BuildingQuickHudSubmenuAnimationSpeed = 7.5f;
    private const float BuildingQuickHudSubmenuMotelTopInset = 139f;

    private readonly List<DriverAgent> buildingQuickHudSubmenuDriverBuffer = new();
    private readonly List<BuildingQuickHudSubmenuRowUi> buildingQuickHudSubmenuRows = new();

    private RectTransform buildingQuickHudSubmenuRoot;
    private LayoutElement buildingQuickHudSubmenuLayout;
    private LayoutElement buildingQuickHudSubmenuScrollLayout;
    private CanvasGroup buildingQuickHudSubmenuCanvasGroup;
    private ScrollRect buildingQuickHudSubmenuScroll;
    private RectTransform buildingQuickHudSubmenuContent;
    private Text buildingQuickHudSubmenuTitleText;
    private Text buildingQuickHudSubmenuEmptyText;
    private Font buildingQuickHudSubmenuFont;
    private BuildingQuickHudSubmenuKind buildingQuickHudSubmenuKind = BuildingQuickHudSubmenuKind.None;
    private LocationType? buildingQuickHudSubmenuLocationType;
    private int buildingQuickHudSubmenuLocationInstanceId;
    private bool buildingQuickHudSubmenuTargetOpen;
    private float buildingQuickHudSubmenuAnimation;
    private float buildingQuickHudSubmenuTargetHeight;

    private void CreateBuildingQuickHudExpandableSubmenu(RectTransform root, Font uiFont)
    {
        buildingQuickHudSubmenuFont = uiFont;

        RectTransform panel = CreateStyledPanel("BuildingQuickHudSubmenu", root, BuildingQuickHudSubmenuPanelColor);
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(1f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.anchoredPosition = new Vector2(0f, 64f);
        panel.sizeDelta = new Vector2(-36f, 0f);
        panel.gameObject.AddComponent<RectMask2D>();

        buildingQuickHudSubmenuLayout = panel.gameObject.AddComponent<LayoutElement>();
        buildingQuickHudSubmenuLayout.ignoreLayout = true;

        buildingQuickHudSubmenuCanvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
        buildingQuickHudSubmenuCanvasGroup.alpha = 0f;
        buildingQuickHudSubmenuCanvasGroup.interactable = false;
        buildingQuickHudSubmenuCanvasGroup.blocksRaycasts = false;

        Outline outline = panel.gameObject.GetComponent<Outline>() ?? panel.gameObject.AddComponent<Outline>();
        outline.effectColor = BuildingQuickHudSubmenuBorderColor;
        outline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 9, 10);
        layout.spacing = 7f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        buildingQuickHudSubmenuTitleText = CreateBodyText(
            "BuildingQuickHudSubmenuTitle",
            panel,
            uiFont,
            string.Empty,
            14,
            TextAnchor.MiddleLeft,
            MotelQuickHudMainTextColor);
        buildingQuickHudSubmenuTitleText.fontStyle = FontStyle.Bold;
        buildingQuickHudSubmenuTitleText.gameObject.AddComponent<LayoutElement>().preferredHeight = 21f;

        FleetCanvasUiFactory.ScrollPanelRefs scroll = CreateVerticalScrollList(
            "BuildingQuickHudSubmenuScroll",
            panel,
            "BuildingQuickHudSubmenuContent",
            6f,
            preferredHeight: 196f);
        buildingQuickHudSubmenuScroll = scroll.ScrollRect;
        buildingQuickHudSubmenuContent = scroll.Content;
        buildingQuickHudSubmenuScrollLayout = scroll.Root.GetComponent<LayoutElement>();
        Scrollbar scrollbar = CreatePanelScrollbar("BuildingQuickHudSubmenuScrollbar", scroll.Root);
        scrollbar.GetComponent<RectTransform>().sizeDelta = new Vector2(8f, 0f);
        buildingQuickHudSubmenuScroll.verticalScrollbar = scrollbar;
        buildingQuickHudSubmenuScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        buildingQuickHudSubmenuEmptyText = CreateBodyText(
            "BuildingQuickHudSubmenuEmpty",
            buildingQuickHudSubmenuContent,
            uiFont,
            string.Empty,
            13,
            TextAnchor.MiddleCenter,
            MotelQuickHudSecondaryTextColor);
        buildingQuickHudSubmenuEmptyText.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;

        buildingQuickHudSubmenuRoot = panel;
        panel.gameObject.SetActive(false);
    }

    private void OnBuildingQuickHudContextButtonClick()
    {
        if (TryGetSelectedBuilding(out _, out LocationType locationType, out _) &&
            HasBuildingQuickHudExpandableSubmenu(locationType))
        {
            ToggleBuildingQuickHudSubmenu(locationType);
            return;
        }

        OpenContextPanelFromBuildingQuickHud();
    }

    private void UpdateBuildingQuickHudSubmenuForSelection(LocationType locationType, LocationData location, bool contextButtonVisible)
    {
        if (buildingQuickHudSubmenuRoot == null)
        {
            return;
        }

        BuildingQuickHudSubmenuKind nextKind = contextButtonVisible
            ? GetBuildingQuickHudSubmenuKind(locationType)
            : BuildingQuickHudSubmenuKind.None;

        if (nextKind == BuildingQuickHudSubmenuKind.None)
        {
            HideBuildingQuickHudSubmenuImmediate();
            return;
        }

        if (buildingQuickHudSubmenuKind != nextKind ||
            buildingQuickHudSubmenuLocationType != locationType ||
            buildingQuickHudSubmenuLocationInstanceId != (location?.InstanceId ?? 0))
        {
            buildingQuickHudSubmenuKind = nextKind;
            buildingQuickHudSubmenuLocationType = locationType;
            buildingQuickHudSubmenuLocationInstanceId = location?.InstanceId ?? 0;
            buildingQuickHudSubmenuTargetOpen = false;
            buildingQuickHudSubmenuAnimation = 0f;
            if (buildingQuickHudSubmenuScroll != null)
            {
                buildingQuickHudSubmenuScroll.verticalNormalizedPosition = 1f;
            }
        }

        buildingQuickHudSubmenuTargetHeight = GetBuildingQuickHudSubmenuTargetHeight(nextKind);
        RefreshBuildingQuickHudSubmenuContent(nextKind, location);
        UpdateBuildingQuickHudSubmenuButtonText(locationType);
        AnimateBuildingQuickHudSubmenu();
    }

    private void HideBuildingQuickHudSubmenuImmediate()
    {
        buildingQuickHudSubmenuKind = BuildingQuickHudSubmenuKind.None;
        buildingQuickHudSubmenuLocationType = null;
        buildingQuickHudSubmenuLocationInstanceId = 0;
        buildingQuickHudSubmenuTargetOpen = false;
        buildingQuickHudSubmenuAnimation = 0f;
        buildingQuickHudSubmenuTargetHeight = 0f;

        if (buildingQuickHudSubmenuRoot != null)
        {
            Vector2 size = buildingQuickHudSubmenuRoot.sizeDelta;
            buildingQuickHudSubmenuRoot.sizeDelta = new Vector2(size.x, 0f);
            buildingQuickHudSubmenuRoot.gameObject.SetActive(false);
        }

        if (buildingQuickHudSubmenuCanvasGroup != null)
        {
            buildingQuickHudSubmenuCanvasGroup.alpha = 0f;
            buildingQuickHudSubmenuCanvasGroup.interactable = false;
            buildingQuickHudSubmenuCanvasGroup.blocksRaycasts = false;
        }
    }

    private static bool HasBuildingQuickHudExpandableSubmenu(LocationType locationType)
    {
        return GetBuildingQuickHudSubmenuKind(locationType) != BuildingQuickHudSubmenuKind.None;
    }

    private static BuildingQuickHudSubmenuKind GetBuildingQuickHudSubmenuKind(LocationType locationType)
    {
        return locationType == LocationType.Motel
            ? BuildingQuickHudSubmenuKind.MotelGuests
            : BuildingQuickHudSubmenuKind.None;
    }

    private float GetBuildingQuickHudSubmenuTargetHeight(BuildingQuickHudSubmenuKind kind)
    {
        if (kind == BuildingQuickHudSubmenuKind.None ||
            buildingQuickHud?.Root == null)
        {
            return 0f;
        }

        float bottom = GetBuildingQuickHudSubmenuBottomOffset();
        float topInset = kind == BuildingQuickHudSubmenuKind.MotelGuests
            ? BuildingQuickHudSubmenuMotelTopInset
            : BuildingQuickHudSubmenuMotelTopInset;
        return Mathf.Max(0f, buildingQuickHud.Root.rect.height - topInset - bottom);
    }

    private void ToggleBuildingQuickHudSubmenu(LocationType locationType)
    {
        BuildingQuickHudSubmenuKind kind = GetBuildingQuickHudSubmenuKind(locationType);
        if (kind == BuildingQuickHudSubmenuKind.None)
        {
            OpenContextPanelFromBuildingQuickHud();
            return;
        }

        if (buildingQuickHudSubmenuKind != kind ||
            buildingQuickHudSubmenuLocationType != locationType)
        {
            buildingQuickHudSubmenuKind = kind;
            buildingQuickHudSubmenuLocationType = locationType;
            buildingQuickHudSubmenuAnimation = 0f;
        }

        buildingQuickHudSubmenuTargetOpen = !buildingQuickHudSubmenuTargetOpen;
        UpdateBuildingQuickHudSubmenuButtonText(locationType);
        LogUiInput(buildingQuickHudSubmenuTargetOpen
            ? $"Quick HUD: opened submenu for {locationType}"
            : $"Quick HUD: closed submenu for {locationType}");
        PlayUiSound(buildingQuickHudSubmenuTargetOpen ? uiPanelOpenClip : uiPanelCloseClip, 0.78f);
    }

    private void UpdateBuildingQuickHudSubmenuButtonText(LocationType locationType)
    {
        if (buildingQuickHud?.ContextButtonText == null ||
            !HasBuildingQuickHudExpandableSubmenu(locationType))
        {
            return;
        }

        buildingQuickHud.ContextButtonText.text = locationType == LocationType.Motel
            ? buildingQuickHudSubmenuTargetOpen
                ? "\u0417\u0430\u043a\u0440\u044b\u0442\u044c \u0441\u043f\u0438\u0441\u043e\u043a \u043f\u043e\u0441\u0442\u043e\u044f\u043b\u044c\u0446\u0435\u0432"
                : "\u041e\u0442\u043a\u0440\u044b\u0442\u044c \u0441\u043f\u0438\u0441\u043e\u043a \u043f\u043e\u0441\u0442\u043e\u044f\u043b\u044c\u0446\u0435\u0432"
            : GetBuildingQuickContextButtonText(locationType);
    }

    private void AnimateBuildingQuickHudSubmenu()
    {
        if (buildingQuickHudSubmenuRoot == null)
        {
            return;
        }

        UpdateBuildingQuickHudSubmenuPlacement();

        float target = buildingQuickHudSubmenuTargetOpen ? 1f : 0f;
        if (target > 0f && !buildingQuickHudSubmenuRoot.gameObject.activeSelf)
        {
            buildingQuickHudSubmenuRoot.gameObject.SetActive(true);
        }

        buildingQuickHudSubmenuAnimation = Mathf.MoveTowards(
            buildingQuickHudSubmenuAnimation,
            target,
            Time.unscaledDeltaTime * BuildingQuickHudSubmenuAnimationSpeed);
        float eased = buildingQuickHudSubmenuAnimation * buildingQuickHudSubmenuAnimation * (3f - 2f * buildingQuickHudSubmenuAnimation);
        float height = Mathf.Lerp(0f, buildingQuickHudSubmenuTargetHeight, eased);
        Vector2 size = buildingQuickHudSubmenuRoot.sizeDelta;
        buildingQuickHudSubmenuRoot.sizeDelta = new Vector2(size.x, height);
        if (buildingQuickHudSubmenuScrollLayout != null)
        {
            buildingQuickHudSubmenuScrollLayout.preferredHeight = Mathf.Max(0f, height - 49f);
        }

        if (buildingQuickHudSubmenuCanvasGroup != null)
        {
            bool visible = buildingQuickHudSubmenuAnimation > 0.001f || buildingQuickHudSubmenuTargetOpen;
            buildingQuickHudSubmenuCanvasGroup.alpha = visible ? 1f : 0f;
            buildingQuickHudSubmenuCanvasGroup.interactable = buildingQuickHudSubmenuTargetOpen && eased > 0.92f;
            buildingQuickHudSubmenuCanvasGroup.blocksRaycasts = visible;
        }

        if (!buildingQuickHudSubmenuTargetOpen && buildingQuickHudSubmenuAnimation <= 0.001f)
        {
            buildingQuickHudSubmenuRoot.gameObject.SetActive(false);
        }
    }

    private void UpdateBuildingQuickHudSubmenuPlacement()
    {
        if (buildingQuickHud?.Root == null ||
            buildingQuickHudSubmenuRoot == null)
        {
            return;
        }

        float left = 18f;
        float right = 18f;
        float bottom = GetBuildingQuickHudSubmenuBottomOffset();
        if (buildingQuickHud.Root.TryGetComponent(out VerticalLayoutGroup layout))
        {
            left = layout.padding.left;
            right = layout.padding.right;
        }

        buildingQuickHudSubmenuRoot.anchoredPosition = new Vector2(0f, bottom);
        Vector2 size = buildingQuickHudSubmenuRoot.sizeDelta;
        buildingQuickHudSubmenuRoot.sizeDelta = new Vector2(-(left + right), size.y);
    }

    private float GetBuildingQuickHudSubmenuBottomOffset()
    {
        if (buildingQuickHud?.Root != null &&
            buildingQuickHud.Root.TryGetComponent(out VerticalLayoutGroup layout))
        {
            return layout.padding.bottom + layout.spacing + 34f;
        }

        return 64f;
    }

    private void RefreshBuildingQuickHudSubmenuContent(BuildingQuickHudSubmenuKind kind, LocationData location)
    {
        switch (kind)
        {
            case BuildingQuickHudSubmenuKind.MotelGuests:
                RefreshMotelGuestSubmenu(location);
                break;
        }
    }

    private void RefreshMotelGuestSubmenu(LocationData location)
    {
        if (buildingQuickHudSubmenuTitleText != null)
        {
            buildingQuickHudSubmenuTitleText.text = "\u041f\u043e\u0441\u0442\u043e\u044f\u043b\u044c\u0446\u044b \u0441\u0435\u0439\u0447\u0430\u0441";
        }

        BuildMotelGuestBuffer(location);
        EnsureBuildingQuickHudSubmenuRows(buildingQuickHudSubmenuDriverBuffer.Count);

        bool hasGuests = buildingQuickHudSubmenuDriverBuffer.Count > 0;
        if (buildingQuickHudSubmenuEmptyText != null)
        {
            buildingQuickHudSubmenuEmptyText.gameObject.SetActive(!hasGuests);
            buildingQuickHudSubmenuEmptyText.text = "\u0421\u0435\u0439\u0447\u0430\u0441 \u043d\u0438\u043a\u043e\u0433\u043e \u043d\u0435\u0442";
        }

        for (int i = 0; i < buildingQuickHudSubmenuRows.Count; i++)
        {
            BuildingQuickHudSubmenuRowUi row = buildingQuickHudSubmenuRows[i];
            bool active = i < buildingQuickHudSubmenuDriverBuffer.Count;
            row.Root.gameObject.SetActive(active);
            if (!active)
            {
                row.DriverId = -1;
                continue;
            }

            DriverAgent driver = buildingQuickHudSubmenuDriverBuffer[i];
            row.DriverId = driver.DriverId;
            row.NameText.text = driver.DriverName;
            row.MetaText.text = $"\u0421\u043f\u0438\u0442 \u0432 \u043c\u043e\u0442\u0435\u043b\u0435 \u00b7 ${driver.Money}";
            row.Background.color = BuildingQuickHudSubmenuRowColor;
        }

        if (buildingQuickHudSubmenuContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(buildingQuickHudSubmenuContent);
        }
    }

    private void BuildMotelGuestBuffer(LocationData location)
    {
        buildingQuickHudSubmenuDriverBuffer.Clear();
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent driver = driverAgents[i];
            if (IsCurrentMotelGuest(driver, location))
            {
                buildingQuickHudSubmenuDriverBuffer.Add(driver);
            }
        }
    }

    private static bool IsCurrentMotelGuest(DriverAgent driver, LocationData location)
    {
        if (driver == null)
        {
            return false;
        }

        if (driver.RestPhase == DriverRestPhase.Sleeping)
        {
            return true;
        }

        return location != null &&
               driver.IsInsideBuilding &&
               driver.InsideBuildingType == LocationType.Motel &&
               (driver.InsideBuildingInstanceId == 0 || driver.InsideBuildingInstanceId == location.InstanceId);
    }

    private int GetBuildingQuickHudMotelGuestCount()
    {
        LocationData motel = null;
        locations.TryGetValue(LocationType.Motel, out motel);
        BuildMotelGuestBuffer(motel);
        return buildingQuickHudSubmenuDriverBuffer.Count;
    }

    private void EnsureBuildingQuickHudSubmenuRows(int count)
    {
        while (buildingQuickHudSubmenuRows.Count < count)
        {
            buildingQuickHudSubmenuRows.Add(CreateBuildingQuickHudSubmenuRow(buildingQuickHudSubmenuRows.Count));
        }
    }

    private BuildingQuickHudSubmenuRowUi CreateBuildingQuickHudSubmenuRow(int index)
    {
        RectTransform rowRoot = CreateStyledPanel($"BuildingQuickHudSubmenuRow{index}", buildingQuickHudSubmenuContent, BuildingQuickHudSubmenuRowColor);
        LayoutElement layoutElement = rowRoot.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 44f;

        Image background = rowRoot.GetComponent<Image>();
        Button button = rowRoot.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.normalColor = BuildingQuickHudSubmenuRowColor;
        colors.highlightedColor = BuildingQuickHudSubmenuRowHoverColor;
        colors.pressedColor = Color.Lerp(BuildingQuickHudSubmenuRowHoverColor, Color.black, 0.16f);
        colors.selectedColor = BuildingQuickHudSubmenuRowHoverColor;
        button.colors = colors;

        HorizontalLayoutGroup rowLayout = rowRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.padding = new RectOffset(9, 10, 5, 5);
        rowLayout.spacing = 8f;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        RectTransform marker = CreateUiObject("Marker", rowRoot).GetComponent<RectTransform>();
        marker.gameObject.AddComponent<LayoutElement>().preferredWidth = 6f;
        Image markerImage = marker.gameObject.AddComponent<Image>();
        markerImage.color = MotelQuickHudBlueColor;
        markerImage.raycastTarget = false;

        RectTransform textColumn = CreateUiObject("Text", rowRoot).GetComponent<RectTransform>();
        textColumn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup textLayout = textColumn.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = 1f;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;

        Text name = CreateBodyText("Name", textColumn, buildingQuickHudSubmenuFont, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        name.fontStyle = FontStyle.Bold;
        name.gameObject.AddComponent<LayoutElement>().preferredHeight = 19f;

        Text meta = CreateBodyText("Meta", textColumn, buildingQuickHudSubmenuFont, string.Empty, 12, TextAnchor.MiddleLeft, MotelQuickHudSecondaryTextColor);
        meta.gameObject.AddComponent<LayoutElement>().preferredHeight = 17f;

        BuildingQuickHudSubmenuRowUi row = new()
        {
            Root = rowRoot,
            Background = background,
            NameText = name,
            MetaText = meta,
            Button = button
        };
        button.onClick.AddListener(() => OnBuildingQuickHudSubmenuDriverClick(row));
        rowRoot.gameObject.SetActive(false);
        return row;
    }

    private void OnBuildingQuickHudSubmenuDriverClick(BuildingQuickHudSubmenuRowUi row)
    {
        if (row == null || row.DriverId <= 0)
        {
            return;
        }

        DriverAgent driver = driverAgents.Find(d => d.DriverId == row.DriverId);
        if (driver == null)
        {
            return;
        }

        LogUiInput($"Quick HUD submenu: opened resident profile for {driver.DriverName}");
        buildingQuickHudSubmenuTargetOpen = false;
        OpenDriversPanelForDriver(driver.DriverId);
    }
}
