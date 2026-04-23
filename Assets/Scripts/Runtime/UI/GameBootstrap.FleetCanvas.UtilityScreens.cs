using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private void InitUnlockedBuildTools()
    {
        unlockedBuildTools = new HashSet<BuildTool>();
        if (selectedGameStartMode == GameStartMode.User)
        {
            unlockedBuildTools.Add(BuildTool.Road);
            unlockedBuildTools.Add(BuildTool.Motel);
            unlockedBuildTools.Add(BuildTool.Stop);
        }
        else
        {
            unlockedBuildTools.Add(BuildTool.Road);
            unlockedBuildTools.Add(BuildTool.Motel);
            unlockedBuildTools.Add(BuildTool.Stop);
            unlockedBuildTools.Add(BuildTool.Sawmill);
            unlockedBuildTools.Add(BuildTool.FurnitureFactory);
            unlockedBuildTools.Add(BuildTool.Bar);
            unlockedBuildTools.Add(BuildTool.Canteen);
            unlockedBuildTools.Add(BuildTool.GamblingHall);
        }
    }

    private bool IsBuildToolUnlocked(BuildTool tool)
    {
        return unlockedBuildTools == null || unlockedBuildTools.Contains(tool);
    }

    private void UnlockBuildTool(BuildTool tool)
    {
        if (unlockedBuildTools == null) return;
        if (unlockedBuildTools.Add(tool))
        {
            isBuildScreenDirty = true;
            SessionDebugLogger.Log("BUILD", $"Build tool unlocked: {tool}.");
        }
    }

    private void SetupBuildScreenUi()
    {
        if (buildScreenUi != null) return;

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        buildScreenUi = new BuildScreenUiRefs();

        GameObject canvasObject = new("BuildScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        buildScreenUi.CanvasRoot = canvasObject;

        GameObject windowRoot = CreateUiObject("BuildWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        SetCenteredWindow(windowRect, 660f, 740f, -16f);
        buildScreenUi.WindowRoot = windowRect;

        Image windowBg = windowRoot.AddComponent<Image>();
        windowBg.color = FleetScreenTint;
        Outline windowOutline = windowRoot.AddComponent<Outline>();
        windowOutline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        windowOutline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup rootLayout = windowRoot.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 14;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("BuildHeaderRow", windowRoot.transform, 36f, 0f);
        Text buildTitle = CreateHeaderText("BuildTitle", headerRow, font, "Build", 22, TextAnchor.MiddleLeft, Color.white);
        buildTitle.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        GameObject buildScrollGo = CreateUiObject("BuildScrollView", windowRoot.transform);
        buildScrollGo.AddComponent<LayoutElement>().flexibleHeight = 1f;
        ScrollRect buildScroll = buildScrollGo.AddComponent<ScrollRect>();
        buildScrollGo.AddComponent<RectMask2D>();
        buildScroll.horizontal = false; buildScroll.vertical = true;
        buildScroll.movementType = ScrollRect.MovementType.Clamped;
        buildScroll.scrollSensitivity = 30f; buildScroll.inertia = false;

        GameObject buildContentGo = CreateUiObject("BuildCardList", buildScrollGo.transform);
        RectTransform cardList = buildContentGo.GetComponent<RectTransform>();
        cardList.anchorMin = new Vector2(0f, 1f); cardList.anchorMax = new Vector2(1f, 1f);
        cardList.pivot = new Vector2(0.5f, 1f);
        cardList.anchoredPosition = Vector2.zero; cardList.sizeDelta = Vector2.zero;
        VerticalLayoutGroup cardListLayout = buildContentGo.AddComponent<VerticalLayoutGroup>();
        cardListLayout.spacing = 10f;
        cardListLayout.childControlWidth = true;
        cardListLayout.childControlHeight = true;
        cardListLayout.childForceExpandWidth = true;
        cardListLayout.childForceExpandHeight = false;
        buildContentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        buildScroll.content = cardList;

        buildScreenUi.Categories = new BuildCategoryUi[]
        {
            CreateBuildCategory(cardList, font, "Infrastructure", "Инфраструктура", false,
                (BuildTool.Road,             "RD", "Road",              new Color(0.27f, 0.42f, 0.60f)),
                (BuildTool.Stop,             "ST", "Bus Stop",          new Color(0.72f, 0.28f, 0.24f)),
                (BuildTool.Motel,            "MT", "Motel",             new Color(0.24f, 0.48f, 0.36f))),
            CreateBuildCategory(cardList, font, "Production", "Производство", false,
                (BuildTool.Sawmill,          "SW", "Sawmill",           new Color(0.58f, 0.36f, 0.16f)),
                (BuildTool.FurnitureFactory, "FF", "Furniture Factory", new Color(0.46f, 0.26f, 0.52f))),
            CreateBuildCategory(cardList, font, "Services", "Сервисы", false,
                (BuildTool.Bar,          "BR", "Bar",          new Color(0.52f, 0.20f, 0.20f)),
                (BuildTool.Canteen,      "CT", "Canteen",      new Color(0.20f, 0.42f, 0.50f)),
                (BuildTool.GamblingHall, "GH", "Gambling Hall",new Color(0.52f, 0.38f, 0.08f))),
        };

        AddOverlayCloseButton(windowRect, font);
        buildScreenUi.CanvasRoot.SetActive(false);
        UpdateBuildScreenUi();
    }

    private BuildItemUi CreateBuildItemCard(RectTransform parent, Font font, BuildTool tool, string abbrev, string title, Color accentColor)
    {
        BuildItemUi item = new BuildItemUi { Tool = tool, DefaultAccentColor = accentColor };

        RectTransform cardRoot = CreateUiObject("BuildCard_" + tool, parent).GetComponent<RectTransform>();
        LayoutElement cardLE = cardRoot.gameObject.AddComponent<LayoutElement>();
        cardLE.preferredHeight = 72f;
        cardLE.flexibleHeight  = 0f;
        Image cardBg = cardRoot.gameObject.AddComponent<Image>();
        cardBg.color = new Color(0.16f, 0.21f, 0.28f, 1f);
        item.Root   = cardRoot;
        item.CardBg = cardBg;

        HorizontalLayoutGroup cardLayout = cardRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        cardLayout.childControlWidth  = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth  = false;
        cardLayout.childForceExpandHeight = true;

        // Colored accent strip (left)
        RectTransform accentStrip = CreateUiObject("Accent", cardRoot).GetComponent<RectTransform>();
        Image accentBg = accentStrip.gameObject.AddComponent<Image>();
        accentBg.color = accentColor;
        accentStrip.gameObject.AddComponent<LayoutElement>().preferredWidth = 68f;
        item.AccentBg = accentBg;

        Text accentLabel = CreateHeaderText("AccentLabel", accentStrip, font, abbrev, 17, TextAnchor.MiddleCenter, Color.white);
        RectTransform accentLabelRect = accentLabel.GetComponent<RectTransform>();
        accentLabelRect.anchorMin = Vector2.zero;
        accentLabelRect.anchorMax = Vector2.one;
        accentLabelRect.sizeDelta  = Vector2.zero;
        accentLabelRect.anchoredPosition = Vector2.zero;

        // Card body (right)
        RectTransform body = CreateUiObject("Body", cardRoot).GetComponent<RectTransform>();
        body.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        VerticalLayoutGroup bodyLayout = body.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.padding = new RectOffset(14, 10, 10, 8);
        bodyLayout.spacing = 4;
        bodyLayout.childControlWidth  = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth  = true;
        bodyLayout.childForceExpandHeight = false;

        // Title row: name + status badge
        RectTransform titleRow = CreateLayoutRow("TitleRow", body, 22f, 0f);

        Text titleText = CreateHeaderText("Title", titleRow, font, title, 15, TextAnchor.MiddleLeft, Color.white);
        titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        item.TitleText = titleText;

        RectTransform statusBadgeRoot = CreateUiObject("StatusBadge", titleRow).GetComponent<RectTransform>();
        Image statusBg = statusBadgeRoot.gameObject.AddComponent<Image>();
        statusBg.color = new Color(0.22f, 0.28f, 0.38f, 0.8f);
        LayoutElement statusLayout = statusBadgeRoot.gameObject.AddComponent<LayoutElement>();
        statusLayout.preferredWidth  = 72f;
        statusLayout.preferredHeight = 20f;
        item.StatusBg = statusBg;

        Text statusText = CreateBodyText("StatusText", statusBadgeRoot, font, "Available", 11, TextAnchor.MiddleCenter, Color.white);
        RectTransform statusTextRect = statusText.GetComponent<RectTransform>();
        statusTextRect.anchorMin = Vector2.zero;
        statusTextRect.anchorMax = Vector2.one;
        statusTextRect.sizeDelta  = Vector2.zero;
        statusTextRect.anchoredPosition = Vector2.zero;
        item.StatusText = statusText;

        // Description
        Text descText = CreateBodyText("Desc", body, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        descText.horizontalOverflow = HorizontalWrapMode.Wrap;
        descText.verticalOverflow   = VerticalWrapMode.Overflow;
        descText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 0f;
        item.DescText = descText;

        // Invisible button over the whole card
        Button btn = cardRoot.gameObject.AddComponent<Button>();
        btn.targetGraphic = cardBg;
        ColorBlock colors = btn.colors;
        colors.normalColor      = Color.white;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
        colors.pressedColor     = new Color(0.88f, 0.88f, 0.88f, 1f);
        colors.disabledColor    = Color.white;
        btn.colors = colors;
        BuildTool capturedTool = tool;
        btn.onClick.AddListener(() =>
        {
            if (!IsBuildToolUnlocked(capturedTool)) return;
            activeBuildTool = activeBuildTool == capturedTool ? BuildTool.None : capturedTool;
            isBuildPanelOpen = false;
            LogUiInput($"Build Canvas: switched tool to {activeBuildTool}");
            PlayUiSound(uiSelectClip, 0.85f);
            SessionDebugLogger.Log("BUILD", $"Build tool switched to {activeBuildTool}.");
            isBuildScreenDirty = true;
        });
        item.Button = btn;
        return item;
    }

    private BuildCategoryUi CreateBuildCategory(RectTransform parent, Font font, string labelEn, string labelRu, bool expanded,
        params (BuildTool tool, string abbrev, string title, Color color)[] toolDefs)
    {
        BuildCategoryUi cat = new BuildCategoryUi { LabelEn = labelEn, LabelRu = labelRu, IsExpanded = expanded };

        RectTransform headerRoot = CreateUiObject("CatHeader_" + labelEn, parent).GetComponent<RectTransform>();
        LayoutElement hLE = headerRoot.gameObject.AddComponent<LayoutElement>();
        hLE.preferredHeight = 30f;
        hLE.flexibleHeight  = 0f;
        Image headerBg = headerRoot.gameObject.AddComponent<Image>();
        headerBg.color = new Color(0.13f, 0.17f, 0.23f, 1f);
        cat.HeaderRoot = headerRoot;

        HorizontalLayoutGroup hLG = headerRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        hLG.padding = new RectOffset(10, 10, 0, 0);
        hLG.spacing = 6f;
        hLG.childControlWidth  = true;
        hLG.childControlHeight = true;
        hLG.childForceExpandWidth  = false;
        hLG.childForceExpandHeight = true;

        Text arrowText = CreateBodyText("Arrow", headerRoot, font, expanded ? "▼" : "▶", 13, TextAnchor.MiddleLeft, new Color(0.65f, 0.72f, 0.82f));
        arrowText.gameObject.AddComponent<LayoutElement>().preferredWidth = 14f;
        cat.ArrowText = arrowText;

        Text headerText = CreateBodyText("CatLabel", headerRoot, font, labelEn, 13, TextAnchor.MiddleLeft, new Color(0.78f, 0.84f, 0.92f));
        headerText.fontStyle = FontStyle.Bold;
        headerText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        cat.HeaderText = headerText;

        Button btn = headerRoot.gameObject.AddComponent<Button>();
        btn.targetGraphic = headerBg;
        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
        cb.pressedColor     = new Color(0.85f, 0.85f, 0.85f, 1f);
        btn.colors = cb;
        BuildCategoryUi capturedCat = cat;
        btn.onClick.AddListener(() =>
        {
            capturedCat.IsExpanded = !capturedCat.IsExpanded;
            isBuildScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.70f);
        });

        // Create items after header so they appear below it in the layout
        cat.Items = new BuildItemUi[toolDefs.Length];
        for (int i = 0; i < toolDefs.Length; i++)
            cat.Items[i] = CreateBuildItemCard(parent, font, toolDefs[i].tool, toolDefs[i].abbrev, toolDefs[i].title, toolDefs[i].color);

        return cat;
    }

    private void UpdateBuildScreenUi()
    {
        if (buildScreenUi == null) return;

        bool shouldShow = isBuildPanelOpen;
        if (buildScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            buildScreenUi.CanvasRoot.SetActive(shouldShow);
            isBuildScreenDirty = true;
        }

        if (!shouldShow) return;
        if (!isBuildScreenDirty) return;

        bool ru = IsRussianLanguage();
        foreach (BuildCategoryUi cat in buildScreenUi.Categories)
        {
            bool anyUnlocked = false;
            foreach (BuildItemUi ci in cat.Items)
                if (IsBuildToolUnlocked(ci.Tool)) { anyUnlocked = true; break; }

            cat.HeaderRoot.gameObject.SetActive(anyUnlocked);
            if (!anyUnlocked) continue;

            cat.HeaderText.text = ru ? cat.LabelRu : cat.LabelEn;
            cat.ArrowText.text  = cat.IsExpanded ? "▼" : "▶";

            foreach (BuildItemUi item in cat.Items)
            {
                bool unlocked = IsBuildToolUnlocked(item.Tool);
                bool visible  = unlocked && cat.IsExpanded;
                item.Root.gameObject.SetActive(visible);
                if (!visible) continue;

                bool isActive = activeBuildTool == item.Tool;
                bool isBuilt  = GetBuildToolAlreadyBuilt(item.Tool);

                item.CardBg.color   = isActive ? new Color(0.20f, 0.27f, 0.37f, 1f) : new Color(0.16f, 0.21f, 0.28f, 1f);
                item.AccentBg.color = isActive ? FleetAccentColor : item.DefaultAccentColor;
                item.DescText.text  = GetBuildDescription(item.Tool, isActive);

                if (isActive)
                {
                    item.StatusBg.color  = new Color(0.60f, 0.36f, 0.10f, 0.85f);
                    item.StatusText.text = "Active";
                }
                else if (isBuilt)
                {
                    item.StatusBg.color  = new Color(0.18f, 0.40f, 0.24f, 0.85f);
                    item.StatusText.text = "Built";
                }
                else
                {
                    item.StatusBg.color  = new Color(0.22f, 0.28f, 0.38f, 0.80f);
                    item.StatusText.text = "Available";
                }
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(buildScreenUi.WindowRoot);
        LocalizeCanvas(buildScreenUi.CanvasRoot);
        isBuildScreenDirty = false;
    }

    private bool GetBuildToolAlreadyBuilt(BuildTool tool)
    {
        return tool switch
        {
            BuildTool.Road             => false,
            BuildTool.Stop             => false,
            BuildTool.Sawmill          => locations.ContainsKey(LocationType.Sawmill),
            BuildTool.Motel            => locations.ContainsKey(LocationType.Motel),
            BuildTool.FurnitureFactory => locations.ContainsKey(LocationType.FurnitureFactory),
            BuildTool.Bar              => locations.ContainsKey(LocationType.Bar),
            BuildTool.Canteen          => locations.ContainsKey(LocationType.Canteen),
            BuildTool.GamblingHall     => locations.ContainsKey(LocationType.GamblingHall),
            _                          => false
        };
    }

    private string GetBuildDescription(BuildTool tool, bool isActive)
    {
        bool ru = IsRussianLanguage();
        string rot = GetBuildRotationLabel();

        if (isActive)
        {
            return tool switch
            {
                BuildTool.Road             => ru ? "Режим активен: левый клик строит, правый удаляет." : "Mode active: left click builds, right click removes.",
                BuildTool.Stop             => ru ? $"Режим активен: поставь автобусную остановку 2x1 с подъездом. R — поворот ({rot})." : $"Mode active: place one 2x1 bus stop from its driveway cell. R rotates ({rot}).",
                BuildTool.Sawmill          => ru ? $"Режим активен: поставь лесопилку 2x2 с подъездом. R — поворот ({rot})." : $"Mode active: place one 2x2 sawmill from its driveway cell. R rotates ({rot}).",
                BuildTool.Motel            => ru ? $"Режим активен: поставь мотель 2x2 с подъездом. R — поворот ({rot})." : $"Mode active: place one 2x2 motel from its driveway cell. R rotates ({rot}).",
                BuildTool.FurnitureFactory => ru ? $"Режим активен: поставь фабрику 3x2 с подъездом. R — поворот ({rot})." : $"Mode active: place one 3x2 furniture factory from its driveway cell. R rotates ({rot}).",
                BuildTool.Bar              => ru ? $"Режим активен: поставь бар с подъездом. R — поворот ({rot})." : $"Mode active: place bar from its driveway cell. R rotates ({rot}).",
                BuildTool.Canteen          => ru ? $"Режим активен: поставь столовую 2x2 с подъездом. R — поворот ({rot})." : $"Mode active: place one 2x2 canteen from its driveway cell. R rotates ({rot}).",
                BuildTool.GamblingHall     => ru ? $"Режим активен: поставь игровые автоматы 2x2 с подъездом. R — поворот ({rot})." : $"Mode active: place gambling hall from its driveway cell. R rotates ({rot}).",
                _                          => string.Empty
            };
        }

        string alreadyBuilt = ru ? "Уже построено на этой карте." : "Already built on this map.";
        return tool switch
        {
            BuildTool.Road             => ru ? "Нажми для входа в режим постройки дорог." : "Click to enter road building mode.",
            BuildTool.Stop             => ru ? "Автобусная остановка 2x1: локальная городская остановка для будущего транспорта рабочих." : "Place a 2x1 local bus stop for future worker public transport routes.",
            BuildTool.Sawmill          => locations.ContainsKey(LocationType.Sawmill)          ? alreadyBuilt : (ru ? "Здание 2x2: превращает брёвна в доски." : "Place a 2x2 production building that turns logs into boards."),
            BuildTool.Motel            => locations.ContainsKey(LocationType.Motel)            ? alreadyBuilt : (ru ? "Мотель 2x2: водители нанимаются и ждут здесь." : "Place a 2x2 driver hub. Drivers can idle and be hired after it exists."),
            BuildTool.FurnitureFactory => locations.ContainsKey(LocationType.FurnitureFactory) ? alreadyBuilt : (ru ? "Фабрика 3x2: 1 Доска + 1 Ткань = 1 Мебель." : "Place a 3x2 factory that turns 1 Board + 1 Textile into 1 Furniture."),
            BuildTool.Bar              => locations.ContainsKey(LocationType.Bar)              ? alreadyBuilt : (ru ? "Соцточка — водители собираются здесь отдыхать." : "Social hub — idle drivers gather here to rest."),
            BuildTool.Canteen          => locations.ContainsKey(LocationType.Canteen)          ? alreadyBuilt : (ru ? "Столовая: водители и рабочие платят $10 за обед." : "Service building: visiting drivers/workers pay $10 for a quick meal."),
            BuildTool.GamblingHall     => locations.ContainsKey(LocationType.GamblingHall)     ? alreadyBuilt : (ru ? "Досуг: бесплатный вход — рабочие расслабляются здесь." : "Leisure: free entry — workers unwind here."),
            _                          => string.Empty
        };
    }

    private void SetupResourcesScreenUi()
    {
        if (resourcesScreenUi != null) return;

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        resourcesScreenUi = new ResourcesScreenUiRefs();

        GameObject canvasObject = new("ResourcesScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        resourcesScreenUi.CanvasRoot = canvasObject;

        GameObject windowRoot = CreateUiObject("ResourcesWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        SetCenteredWindow(windowRect, 560f, 680f, -16f);
        resourcesScreenUi.WindowRoot = windowRect;

        Image windowBg = windowRoot.AddComponent<Image>();
        windowBg.color = DriversScreenTint;
        Outline windowOutline = windowRoot.AddComponent<Outline>();
        windowOutline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        windowOutline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup rootLayout = windowRoot.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(14, 14, 14, 14);
        rootLayout.spacing = 8;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        // Header row: title
        RectTransform headerRow = CreateLayoutRow("ResourcesHeaderRow", windowRoot.transform, 34f, 0f);
        Text resourcesTitleText = CreateHeaderText("ResourcesTitle", headerRow, font, "Resources", 20, TextAnchor.MiddleLeft, Color.white);
        resourcesTitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // Tab row: two toggle buttons (mirrors Shifts tab pattern)
        const float ResTabRowHeight   = 36f;
        const float ResPanelHeight    = 500f;
        RectTransform tabRow = CreateLayoutRow("ResourcesTabRow", windowRoot.transform, ResTabRowHeight, 0f);
        LayoutElement tabRowLE = tabRow.GetComponent<LayoutElement>();
        tabRowLE.minHeight     = ResTabRowHeight;
        tabRowLE.flexibleHeight = 0f;
        HorizontalLayoutGroup tabHlg = tabRow.GetComponent<HorizontalLayoutGroup>();
        tabHlg.childForceExpandWidth  = true;
        tabHlg.childForceExpandHeight = true;

        resourcesScreenUi.WarehouseTabBtn = CreateButton("WarehouseTabBtn", tabRow, font, out resourcesScreenUi.WarehouseTabText, "Warehouse", 13, FleetPrimaryButtonColor, Color.white);
        resourcesScreenUi.WarehouseTabText.fontStyle = FontStyle.Bold;
        resourcesScreenUi.WarehouseTabBtn.transition = Selectable.Transition.None;
        resourcesScreenUi.WarehouseTabBtn.onClick.AddListener(() => { isResourcesWarehouseTab = true;  UpdateResourcesScreenUi(); });

        resourcesScreenUi.ProductionTabBtn = CreateButton("ProductionTabBtn", tabRow, font, out resourcesScreenUi.ProductionTabText, "Production", 13, new Color(0.22f, 0.26f, 0.32f, 1f), Color.white);
        resourcesScreenUi.ProductionTabText.fontStyle = FontStyle.Bold;
        resourcesScreenUi.ProductionTabBtn.transition = Selectable.Transition.None;
        resourcesScreenUi.ProductionTabBtn.onClick.AddListener(() => { isResourcesWarehouseTab = false; UpdateResourcesScreenUi(); });

        // --- WAREHOUSE PANEL ---
        GameObject warehousePanel = CreateUiObject("WarehousePanel", windowRoot.transform);
        resourcesScreenUi.WarehousePanel = warehousePanel;
        LayoutElement warehousePanelLE = warehousePanel.AddComponent<LayoutElement>();
        warehousePanelLE.preferredHeight = ResPanelHeight;
        warehousePanelLE.minHeight       = ResPanelHeight;
        warehousePanelLE.flexibleHeight  = 0f;

        GameObject wScrollGo = CreateUiObject("WResourcesScrollView", warehousePanel.transform);
        RectTransform wScrollRect = wScrollGo.GetComponent<RectTransform>();
        wScrollRect.anchorMin = Vector2.zero;
        wScrollRect.anchorMax = Vector2.one;
        wScrollRect.sizeDelta = Vector2.zero;
        wScrollRect.anchoredPosition = Vector2.zero;
        ScrollRect wScroll = wScrollGo.AddComponent<ScrollRect>();
        wScrollGo.AddComponent<RectMask2D>();
        wScroll.horizontal = false;
        wScroll.vertical = true;
        wScroll.movementType = ScrollRect.MovementType.Clamped;
        wScroll.scrollSensitivity = 30f;
        wScroll.inertia = false;

        GameObject wContentGo = CreateUiObject("WResourcesContentRoot", wScrollGo.transform);
        RectTransform wContentRoot = wContentGo.GetComponent<RectTransform>();
        wContentRoot.anchorMin = new Vector2(0f, 1f);
        wContentRoot.anchorMax = new Vector2(1f, 1f);
        wContentRoot.pivot = new Vector2(0.5f, 1f);
        wContentRoot.anchoredPosition = Vector2.zero;
        wContentRoot.sizeDelta = Vector2.zero;
        VerticalLayoutGroup wContentGroup = wContentGo.AddComponent<VerticalLayoutGroup>();
        wContentGroup.spacing = 6;
        wContentGroup.childControlWidth = true;
        wContentGroup.childControlHeight = true;
        wContentGroup.childForceExpandWidth = true;
        wContentGroup.childForceExpandHeight = false;
        wContentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        wScroll.content = wContentRoot;

        CreateResourceSummaryRow(wContentRoot, font, "Logs",      ResourceVisualKind.Logs,      TradeResourceType.Logs,      resourcesScreenUi.WarehouseRows);
        CreateResourceSummaryRow(wContentRoot, font, "Boards",    ResourceVisualKind.Boards,    TradeResourceType.Boards,    resourcesScreenUi.WarehouseRows);
        CreateResourceSummaryRow(wContentRoot, font, "Cotton",    ResourceVisualKind.Cotton,    TradeResourceType.Cotton,    resourcesScreenUi.WarehouseRows);
        CreateResourceSummaryRow(wContentRoot, font, "Textile",   ResourceVisualKind.Textile,   TradeResourceType.Textile,   resourcesScreenUi.WarehouseRows);
        CreateResourceSummaryRow(wContentRoot, font, "Furniture", ResourceVisualKind.Furniture, TradeResourceType.Furniture, resourcesScreenUi.WarehouseRows);
        CreateResourceSummaryRow(wContentRoot, font, "Fuel",      ResourceVisualKind.Fuel,      resourcesScreenUi.WarehouseRows);
        CreateResourceSummaryRow(wContentRoot, font, "Alcohol",   ResourceVisualKind.Alcohol,   resourcesScreenUi.WarehouseRows);
        CreateResourceSummaryRow(wContentRoot, font, "Food",      ResourceVisualKind.Food,       resourcesScreenUi.WarehouseRows);

        // --- PRODUCTION PANEL ---
        GameObject productionPanel = CreateUiObject("ProductionPanel", windowRoot.transform);
        resourcesScreenUi.ProductionPanel = productionPanel;
        LayoutElement productionPanelLE = productionPanel.AddComponent<LayoutElement>();
        productionPanelLE.preferredHeight = ResPanelHeight;
        productionPanelLE.minHeight       = ResPanelHeight;
        productionPanelLE.flexibleHeight  = 0f;

        GameObject pScrollGo = CreateUiObject("PResourcesScrollView", productionPanel.transform);
        RectTransform pScrollRect = pScrollGo.GetComponent<RectTransform>();
        pScrollRect.anchorMin = Vector2.zero;
        pScrollRect.anchorMax = Vector2.one;
        pScrollRect.sizeDelta = Vector2.zero;
        pScrollRect.anchoredPosition = Vector2.zero;
        ScrollRect pScroll = pScrollGo.AddComponent<ScrollRect>();
        pScrollGo.AddComponent<RectMask2D>();
        pScroll.horizontal = false;
        pScroll.vertical = true;
        pScroll.movementType = ScrollRect.MovementType.Clamped;
        pScroll.scrollSensitivity = 30f;
        pScroll.inertia = false;

        GameObject pContentGo = CreateUiObject("PResourcesContentRoot", pScrollGo.transform);
        RectTransform pContentRoot = pContentGo.GetComponent<RectTransform>();
        pContentRoot.anchorMin = new Vector2(0f, 1f);
        pContentRoot.anchorMax = new Vector2(1f, 1f);
        pContentRoot.pivot = new Vector2(0.5f, 1f);
        pContentRoot.anchoredPosition = Vector2.zero;
        pContentRoot.sizeDelta = Vector2.zero;
        VerticalLayoutGroup pContentGroup = pContentGo.AddComponent<VerticalLayoutGroup>();
        pContentGroup.spacing = 10;
        pContentGroup.childControlWidth = true;
        pContentGroup.childControlHeight = true;
        pContentGroup.childForceExpandWidth = true;
        pContentGroup.childForceExpandHeight = false;
        pContentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        pScroll.content = pContentRoot;

        // Production sections: Forest, Sawmill, FurnitureFactory, GasStation
        resourcesScreenUi.ProductionSections.Add(CreateProductionSection(pContentRoot, font, LocationType.Forest,           "Forest",            new[] { (ResourceVisualKind.Logs,      TradeResourceType.Logs,      "Logs")      }));
        resourcesScreenUi.ProductionSections.Add(CreateProductionSection(pContentRoot, font, LocationType.Sawmill,          "Sawmill",           new[] { (ResourceVisualKind.Logs,      TradeResourceType.Logs,      "Logs"),      (ResourceVisualKind.Boards,   TradeResourceType.Boards,   "Boards")    }));
        resourcesScreenUi.ProductionSections.Add(CreateProductionSection(pContentRoot, font, LocationType.FurnitureFactory,  "Furniture Factory", new[] { (ResourceVisualKind.Boards,    TradeResourceType.Boards,    "Boards"),    (ResourceVisualKind.Textile,  TradeResourceType.Textile,  "Textile"),   (ResourceVisualKind.Furniture, TradeResourceType.Furniture, "Furniture") }));
        resourcesScreenUi.ProductionSections.Add(CreateProductionSection(pContentRoot, font, LocationType.GasStation,        "Gas Station",       new[] { (ResourceVisualKind.Fuel,      TradeResourceType.Logs,      "Fuel")      }));

        // Treasury footer
        RectTransform footerCard = CreateSectionCard(windowRoot.transform, font, "Treasury", out RectTransform footerBody);
        footerCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 60f;
        resourcesScreenUi.TreasuryValueText = CreateHeaderText("TreasuryValue", footerBody, font, string.Empty, 16, TextAnchor.MiddleLeft, FleetAccentColor);

        AddOverlayCloseButton(windowRect, font);
        resourcesScreenUi.CanvasRoot.SetActive(false);
        UpdateResourcesScreenUi();
    }

    private ProductionBuildingSectionUi CreateProductionSection(RectTransform parent, Font font, LocationType buildingType, string buildingName, (ResourceVisualKind kind, TradeResourceType resType, string label)[] resources)
    {
        ProductionBuildingSectionUi section = new ProductionBuildingSectionUi { BuildingType = buildingType };

        GameObject sectionGo = CreateUiObject($"ProdSection_{buildingType}", parent);
        section.Root = sectionGo;
        VerticalLayoutGroup sectionVlg = sectionGo.AddComponent<VerticalLayoutGroup>();
        sectionVlg.spacing = 4f;
        sectionVlg.childControlWidth = true;
        sectionVlg.childControlHeight = true;
        sectionVlg.childForceExpandWidth = true;
        sectionVlg.childForceExpandHeight = false;
        sectionGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Section header
        GameObject headerGo = CreateUiObject($"ProdSectionHeader_{buildingType}", sectionGo.GetComponent<RectTransform>());
        headerGo.AddComponent<Image>().color = new Color(0.20f, 0.26f, 0.34f, 1f);
        headerGo.AddComponent<LayoutElement>().preferredHeight = 26f;
        section.SectionHeaderText = CreateBodyText($"ProdSectionTitle_{buildingType}", headerGo.GetComponent<RectTransform>(), font, buildingName, 12, TextAnchor.MiddleLeft, new Color(0.82f, 0.86f, 0.92f, 1f));
        section.SectionHeaderText.fontStyle = FontStyle.Bold;
        section.SectionHeaderText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

        // Resource rows
        foreach (var (kind, resType, label) in resources)
        {
            CreateResourceSummaryRow(sectionGo.GetComponent<RectTransform>(), font, label, kind, resType, section.Rows);
        }

        return section;
    }

    private enum ResourceVisualKind
    {
        Logs,
        Boards,
        Cotton,
        Textile,
        Furniture,
        Fuel,
        Alcohol,
        Food
    }

    private void CreateResourceSummaryRow(RectTransform parent, Font font, string title, ResourceVisualKind iconKind, List<ResourceSummaryRowUi> rows)
    {
        CreateResourceSummaryRow(parent, font, title, iconKind, TradeResourceType.Logs, rows);
    }

    private void CreateResourceSummaryRow(RectTransform parent, Font font, string title, ResourceVisualKind iconKind, TradeResourceType resourceType, List<ResourceSummaryRowUi> rows)
    {
        RectTransform card = CreateStyledPanel($"{title}RowCard", parent, FleetInsetColor);
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = 42f;

        HorizontalLayoutGroup layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        RectTransform iconRoot = CreateUiObject($"{title}IconRoot", card).GetComponent<RectTransform>();
        LayoutElement iconLayout = iconRoot.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 28f;
        iconLayout.preferredHeight = 28f;
        Image iconBackground = iconRoot.gameObject.AddComponent<Image>();
        iconBackground.color = new Color(1f, 1f, 1f, 0.06f);
        DrawResourceIcon(iconRoot, iconKind);

        RectTransform textRoot = CreateUiObject($"{title}TextRoot", card).GetComponent<RectTransform>();
        LayoutElement textLayout = textRoot.gameObject.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1f;
        VerticalLayoutGroup textGroup = textRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        textGroup.spacing = 1f;
        textGroup.childControlWidth = true;
        textGroup.childControlHeight = true;
        textGroup.childForceExpandWidth = true;
        textGroup.childForceExpandHeight = false;

        Text nameText = CreateBodyText($"{title}Name", textRoot, font, title, 12, TextAnchor.MiddleLeft, Color.white);
        nameText.fontStyle = FontStyle.Bold;
        nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 15f;

        Text valueText = CreateHeaderText($"{title}Value", textRoot, font, string.Empty, 16, TextAnchor.MiddleLeft, FleetAccentColor);
        valueText.gameObject.AddComponent<LayoutElement>().preferredHeight = 19f;

        rows.Add(new ResourceSummaryRowUi
        {
            NameText = nameText,
            ValueText = valueText,
            ResourceType = resourceType,
        });
    }

    private static void DrawResourceIcon(RectTransform parent, ResourceVisualKind iconKind)
    {
        switch (iconKind)
        {
            case ResourceVisualKind.Logs:
                CreateIconBar(parent, new Vector2(18f, 7f), new Vector2(0f, 9f), new Color(0.63f, 0.43f, 0.2f));
                CreateIconBar(parent, new Vector2(20f, 7f), new Vector2(2f, 0f), new Color(0.74f, 0.52f, 0.26f));
                CreateIconBar(parent, new Vector2(18f, 7f), new Vector2(-1f, -9f), new Color(0.56f, 0.36f, 0.16f));
                break;
            case ResourceVisualKind.Boards:
                CreateIconBar(parent, new Vector2(20f, 4f), new Vector2(0f, 8f), new Color(0.86f, 0.72f, 0.45f));
                CreateIconBar(parent, new Vector2(20f, 4f), new Vector2(0f, 1f), new Color(0.81f, 0.67f, 0.4f));
                CreateIconBar(parent, new Vector2(20f, 4f), new Vector2(0f, -6f), new Color(0.76f, 0.61f, 0.35f));
                break;
            case ResourceVisualKind.Cotton:
                CreateIconCircle(parent, 11f, new Vector2(-6f, 2f), new Color(0.97f, 0.97f, 0.95f));
                CreateIconCircle(parent, 10f, new Vector2(4f, 5f), new Color(0.98f, 0.98f, 0.96f));
                CreateIconCircle(parent, 9f, new Vector2(0f, -5f), new Color(0.95f, 0.95f, 0.93f));
                break;
            case ResourceVisualKind.Textile:
                CreateIconBar(parent, new Vector2(22f, 18f), Vector2.zero, new Color(0.72f, 0.84f, 0.95f));
                CreateIconBar(parent, new Vector2(3f, 18f), new Vector2(-6f, 0f), new Color(0.55f, 0.74f, 0.9f));
                CreateIconBar(parent, new Vector2(3f, 18f), new Vector2(2f, 0f), new Color(0.55f, 0.74f, 0.9f));
                CreateIconBar(parent, new Vector2(3f, 18f), new Vector2(10f, 0f), new Color(0.55f, 0.74f, 0.9f));
                break;
            case ResourceVisualKind.Furniture:
                CreateIconBar(parent, new Vector2(18f, 4f), new Vector2(0f, 7f), new Color(0.78f, 0.56f, 0.3f));
                CreateIconBar(parent, new Vector2(14f, 4f), new Vector2(0f, -1f), new Color(0.72f, 0.5f, 0.25f));
                CreateIconBar(parent, new Vector2(3f, 10f), new Vector2(-6f, -8f), new Color(0.58f, 0.39f, 0.18f));
                CreateIconBar(parent, new Vector2(3f, 10f), new Vector2(6f, -8f), new Color(0.58f, 0.39f, 0.18f));
                break;
            case ResourceVisualKind.Fuel:
                // pump body
                CreateIconBar(parent, new Vector2(12f, 16f), new Vector2(-3f, -2f), new Color(0.30f, 0.36f, 0.44f));
                // arm
                CreateIconBar(parent, new Vector2(7f, 3f), new Vector2(4f, 5f), new Color(0.45f, 0.52f, 0.60f));
                // nozzle
                CreateIconBar(parent, new Vector2(3f, 9f), new Vector2(7f, 0f), new Color(0.45f, 0.52f, 0.60f));
                // fuel fill line
                CreateIconBar(parent, new Vector2(8f, 3f), new Vector2(-3f, -4f), new Color(0.98f, 0.80f, 0.20f));
                break;
            case ResourceVisualKind.Alcohol:
                // bottle body
                CreateIconBar(parent, new Vector2(10f, 14f), new Vector2(0f, -4f), new Color(0.28f, 0.58f, 0.36f));
                // bottle neck
                CreateIconBar(parent, new Vector2(5f, 6f), new Vector2(0f, 7f), new Color(0.24f, 0.50f, 0.30f));
                // cap
                CreateIconBar(parent, new Vector2(7f, 3f), new Vector2(0f, 11f), new Color(0.60f, 0.40f, 0.20f));
                break;
            case ResourceVisualKind.Food:
                // plate
                CreateIconCircle(parent, 20f, new Vector2(0f, -2f), new Color(0.90f, 0.86f, 0.78f));
                // food on plate
                CreateIconCircle(parent, 12f, new Vector2(0f, -2f), new Color(0.94f, 0.68f, 0.32f));
                // bread top
                CreateIconCircle(parent, 7f, new Vector2(0f, 2f), new Color(0.82f, 0.54f, 0.20f));
                break;
        }
    }

    private static void CreateIconBar(RectTransform parent, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        RectTransform rect = CreateUiObject("IconBar", parent).GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
    }

    private static void CreateIconCircle(RectTransform parent, float diameter, Vector2 anchoredPosition, Color color)
    {
        RectTransform rect = CreateUiObject("IconCircle", parent).GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(diameter, diameter);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
    }

    private void UpdateResourcesScreenUi()
    {
        if (resourcesScreenUi == null) return;

        bool shouldShow = isResourcesPanelOpen;
        bool forceLayoutRebuild = false;
        if (resourcesScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            resourcesScreenUi.CanvasRoot.SetActive(shouldShow);
            forceLayoutRebuild = shouldShow;
        }

        if (!shouldShow) return;

        bool ru = IsRussianLanguage();

        // Tab panel visibility
        resourcesScreenUi.WarehousePanel.SetActive(isResourcesWarehouseTab);
        resourcesScreenUi.ProductionPanel.SetActive(!isResourcesWarehouseTab);

        // Tab button highlight
        Color activeTabColor   = FleetPrimaryButtonColor;
        Color inactiveTabColor = new Color(0.16f, 0.21f, 0.28f, 1f);
        Color activeTextColor   = Color.white;
        Color inactiveTextColor = FleetSecondaryTextColor;

        resourcesScreenUi.WarehouseTabBtn .GetComponent<Image>().color = isResourcesWarehouseTab  ? activeTabColor : inactiveTabColor;
        resourcesScreenUi.ProductionTabBtn.GetComponent<Image>().color = !isResourcesWarehouseTab ? activeTabColor : inactiveTabColor;
        resourcesScreenUi.WarehouseTabText .color = isResourcesWarehouseTab  ? activeTextColor : inactiveTextColor;
        resourcesScreenUi.ProductionTabText.color = !isResourcesWarehouseTab ? activeTextColor : inactiveTextColor;

        resourcesScreenUi.WarehouseTabText.text  = ru ? "На складе"    : "Warehouse";
        resourcesScreenUi.ProductionTabText.text = ru ? "Производство" : "Production";

        if (isResourcesWarehouseTab)
        {
            locations.TryGetValue(LocationType.Warehouse, out LocationData warehouseData);
            string[] resourceNames =
            {
                ru ? "Брёвна"    : "Logs",
                ru ? "Доски"     : "Boards",
                ru ? "Хлопок"    : "Cotton",
                ru ? "Ткань"     : "Textile",
                ru ? "Мебель"    : "Furniture",
                ru ? "Топливо"   : "Fuel",
                ru ? "Алкоголь"  : "Alcohol",
                ru ? "Еда"       : "Food"
            };
            string[] resourceValues =
            {
                (warehouseData?.LogsStored ?? 0).ToString(),
                (warehouseData?.BoardsStored ?? 0).ToString(),
                cottonStored.ToString(),
                textileStored.ToString(),
                furnitureStored.ToString(),
                $"{warehouseData?.FuelStored ?? 0} / {WarehouseMaxFuelStorage}",
                $"{warehouseData?.AlcoholStored ?? 0} / {WarehouseMaxAlcoholStorage}",
                $"{warehouseData?.FoodStored ?? 0} / {WarehouseMaxFoodStorage}"
            };

            for (int i = 0; i < resourcesScreenUi.WarehouseRows.Count && i < resourceNames.Length; i++)
            {
                ResourceSummaryRowUi row = resourcesScreenUi.WarehouseRows[i];
                if (row.LastName != resourceNames[i])
                {
                    row.NameText.text = resourceNames[i];
                    row.LastName = resourceNames[i];
                    forceLayoutRebuild = true;
                }
                if (row.LastValue != resourceValues[i])
                {
                    row.ValueText.text = resourceValues[i];
                    row.LastValue = resourceValues[i];
                    forceLayoutRebuild = true;
                }
            }
        }
        else
        {
            foreach (ProductionBuildingSectionUi section in resourcesScreenUi.ProductionSections)
            {
                bool exists = locations.TryGetValue(section.BuildingType, out LocationData bData);
                section.Root.SetActive(true);

                string headerName = section.BuildingType switch
                {
                    LocationType.Forest          => ru ? "Лесозаготовка"    : "Lumberyard",
                    LocationType.Sawmill         => ru ? "Лесопилка"        : "Sawmill",
                    LocationType.FurnitureFactory => ru ? "Мебельный завод"  : "Furniture Factory",
                    LocationType.GasStation      => ru ? "Заправка"         : "Gas Station",
                    _ => section.BuildingType.ToString()
                };
                if (!exists) headerName += ru ? " (не построено)" : " (not built)";
                section.SectionHeaderText.text = headerName;

                foreach (ResourceSummaryRowUi row in section.Rows)
                {
                    string rowName = row.ResourceType switch
                    {
                        TradeResourceType.Logs      => ru ? "Брёвна"  : "Logs",
                        TradeResourceType.Boards    => ru ? "Доски"   : "Boards",
                        TradeResourceType.Textile   => ru ? "Ткань"   : "Textile",
                        TradeResourceType.Furniture => ru ? "Мебель"  : "Furniture",
                        _ => row.NameText.text
                    };
                    // Fuel row has resource type Logs as sentinel — detect by name
                    if (section.BuildingType == LocationType.GasStation && row.ResourceType == TradeResourceType.Logs)
                        rowName = ru ? "Топливо" : "Fuel";

                    string rowValue = exists ? GetProductionBuildingResourceValue(bData, section.BuildingType, row.ResourceType) : "—";

                    if (row.LastName != rowName) { row.NameText.text = rowName; row.LastName = rowName; forceLayoutRebuild = true; }
                    if (row.LastValue != rowValue) { row.ValueText.text = rowValue; row.LastValue = rowValue; forceLayoutRebuild = true; }
                }
            }
        }

        string treasuryValue = $"${money}";
        if (resourcesScreenUi.LastTreasuryValue != treasuryValue)
        {
            resourcesScreenUi.TreasuryValueText.text = treasuryValue;
            resourcesScreenUi.LastTreasuryValue = treasuryValue;
            forceLayoutRebuild = true;
        }

        if (forceLayoutRebuild)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(resourcesScreenUi.WindowRoot);
        }
    }

    private string GetProductionBuildingResourceValue(LocationData bData, LocationType buildingType, TradeResourceType resType)
    {
        if (buildingType == LocationType.GasStation && resType == TradeResourceType.Logs)
            return $"{bData.FuelStored} / {WarehouseMaxFuelStorage}";
        return resType switch
        {
            TradeResourceType.Logs      => bData.LogsStored.ToString(),
            TradeResourceType.Boards    => bData.BoardsStored.ToString(),
            TradeResourceType.Textile   => bData.TextileStored.ToString(),
            TradeResourceType.Furniture => bData.FurnitureStored.ToString(),
            _ => "0"
        };
    }

    private int GetTotalLogsResourceAmount()
    {
        int total = 0;
        if (locations.TryGetValue(LocationType.Forest, out LocationData forest))
            total += forest.LogsStored;
        if (locations.TryGetValue(LocationType.Sawmill, out LocationData sawmill))
            total += sawmill.LogsStored;
        if (locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse))
            total += warehouse.LogsStored;
        foreach (TruckAgent truck in truckAgents)
        {
            if (truck.TruckCargoType == CargoType.Logs && !IsTruckOnActiveTradeSellRun(truck))
                total += truck.TruckCargoAmount;
        }
        return total;
    }

    private int GetTotalBoardsResourceAmount()
    {
        int total = 0;
        if (locations.TryGetValue(LocationType.Sawmill, out LocationData sawmill))
            total += sawmill.BoardsStored;
        if (locations.TryGetValue(LocationType.Warehouse, out LocationData warehouse))
            total += warehouse.BoardsStored;
        if (locations.TryGetValue(LocationType.FurnitureFactory, out LocationData furnitureFactory))
            total += furnitureFactory.BoardsStored;
        foreach (TruckAgent truck in truckAgents)
        {
            if (truck.TruckCargoType == CargoType.Boards && !IsTruckOnActiveTradeSellRun(truck))
                total += truck.TruckCargoAmount;
        }
        return total;
    }

    private int GetTotalFuelAmount()
    {
        int total = 0;
        if (locations.TryGetValue(LocationType.Warehouse, out LocationData wh)) total += wh.FuelStored;
        if (locations.TryGetValue(LocationType.GasStation, out LocationData gs)) total += gs.FuelStored;
        return total;
    }

    private int GetTotalAlcoholAmount()
    {
        int total = 0;
        if (locations.TryGetValue(LocationType.Warehouse, out LocationData wh)) total += wh.AlcoholStored;
        if (locations.TryGetValue(LocationType.Bar, out LocationData bar)) total += bar.AlcoholStored;
        return total;
    }

    private int GetTotalFoodAmount()
    {
        int total = 0;
        if (locations.TryGetValue(LocationType.Warehouse, out LocationData wh)) total += wh.FoodStored;
        if (locations.TryGetValue(LocationType.Canteen, out LocationData canteen)) total += canteen.FoodStored;
        return total;
    }

    private bool IsTruckOnActiveTradeSellRun(TruckAgent truck)
    {
        return HasActiveTradeRun() &&
               activeTradeRun.OrderType == TradeOrderType.Sell &&
               activeTradeRun.TruckNumber == truck.TruckNumber;
    }

    private int GetTotalTextileResourceAmount()
    {
        int total = textileStored;
        if (locations.TryGetValue(LocationType.FurnitureFactory, out LocationData furnitureFactory))
            total += furnitureFactory.TextileStored;
        foreach (TruckAgent truck in truckAgents)
        {
            if (truck.TruckCargoType == CargoType.Textile && !IsTruckOnActiveTradeSellRun(truck))
                total += truck.TruckCargoAmount;
        }
        return total;
    }

    private int GetTotalFurnitureResourceAmount()
    {
        int total = furnitureStored;
        if (locations.TryGetValue(LocationType.FurnitureFactory, out LocationData furnitureFactory))
            total += furnitureFactory.FurnitureStored;
        foreach (TruckAgent truck in truckAgents)
        {
            if (truck.TruckCargoType == CargoType.Furniture && !IsTruckOnActiveTradeSellRun(truck))
                total += truck.TruckCargoAmount;
        }
        return total;
    }

    private static readonly TradeResourceType[] TradeHudResources =
    {
        TradeResourceType.Logs,
        TradeResourceType.Boards,
        TradeResourceType.Cotton,
        TradeResourceType.Textile,
        TradeResourceType.Furniture,
        TradeResourceType.Fuel,
        TradeResourceType.Alcohol,
        TradeResourceType.Food,
    };

    private void SetupEconomyScreenUi()
    {
        if (economyScreenUi != null) return;

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        economyScreenUi = new EconomyScreenUiRefs();

        GameObject canvasObject = new("EconomyScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        economyScreenUi.CanvasRoot = canvasObject;

        GameObject windowRoot = CreateUiObject("EconomyWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        SetCenteredWindow(windowRect, 940f, 700f, -16f);
        economyScreenUi.WindowRoot = windowRect;

        Image windowBg = windowRoot.AddComponent<Image>();
        windowBg.color = DriversScreenTint;
        Outline windowOutline = windowRoot.AddComponent<Outline>();
        windowOutline.effectColor = new Color(0f, 0f, 0f, 0.28f);
        windowOutline.effectDistance = new Vector2(2f, -2f);

        VerticalLayoutGroup rootLayout = windowRoot.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(18, 18, 18, 18);
        rootLayout.spacing = 16;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("EconomyHeaderRow", windowRoot.transform, 48f, 0f);
        Text titleText = CreateHeaderText("EconomyTitle", headerRow, font, "Trade", 30, TextAnchor.MiddleLeft, Color.white);
        titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        economyScreenUi.HeaderCountText = CreateHeaderText("EconomyCount", headerRow, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        RectTransform tradeCard = CreateSectionCard(windowRoot.transform, font, "Create Order", out RectTransform tradeBody);
        tradeCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 236f;

        RectTransform resourceRow = CreateLayoutRow("TradeResourceRow", tradeBody, 38f, 8f);
        resourceRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        CreateBodyText("TradeResourceLabel", resourceRow, font, "Resource:", 15, TextAnchor.MiddleLeft, Color.white)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 118f;
        economyScreenUi.TradeResourceDropdownButton = CreateButton("TradeResourceDropdown", resourceRow, font, out Text tradeResourceText, string.Empty, 16, new Color(0.16f, 0.19f, 0.25f, 1f), Color.white);
        economyScreenUi.TradeResourceText = tradeResourceText;
        economyScreenUi.TradeResourceDropdownButton.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        economyScreenUi.TradeResourceDropdownButton.onClick.AddListener(() =>
        {
            isTradeResourceDropdownOpen = !isTradeResourceDropdownOpen;
            isTradeActionDropdownOpen = false;
            isEconomyScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.82f);
        });

        // Resource picker — separate side panel to the right of the trade window
        {
            GameObject pickerObj = CreateUiObject("TradeResourcePickerPanel", canvasObject.transform);
            RectTransform pickerRect = pickerObj.GetComponent<RectTransform>();
            // Anchored to canvas centre, pivot at left-middle — sits just right of the 840px window
            pickerRect.anchorMin = pickerRect.anchorMax = new Vector2(0.5f, 0.5f);
            pickerRect.pivot = new Vector2(0f, 0.5f);
            pickerRect.anchoredPosition = new Vector2(430f, -16f);
            pickerRect.sizeDelta = new Vector2(232f, 0f);   // height driven by ContentSizeFitter
            Image pickerBg = pickerObj.AddComponent<Image>();
            pickerBg.color = DriversScreenTint;
            Outline pickerOutline = pickerObj.AddComponent<Outline>();
            pickerOutline.effectColor = new Color(0f, 0f, 0f, 0.32f);
            pickerOutline.effectDistance = new Vector2(2f, -2f);
            VerticalLayoutGroup pickerLayout = pickerObj.AddComponent<VerticalLayoutGroup>();
            pickerLayout.padding = new RectOffset(0, 0, 0, 8);
            pickerLayout.spacing = 2;
            pickerLayout.childControlWidth = true;
            pickerLayout.childControlHeight = true;
            pickerLayout.childForceExpandWidth = true;
            pickerLayout.childForceExpandHeight = false;
            pickerObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Header bar
            GameObject headerObj = CreateUiObject("PickerHeader", pickerObj.transform);
            headerObj.AddComponent<LayoutElement>().preferredHeight = 36f;
            headerObj.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.14f, 1f);
            Text headerLbl = CreateHeaderText("PickerTitle", headerObj.transform, font, "Resource", 13, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            StretchRect(headerLbl.rectTransform, 14f, 0f, 14f, 0f);

            // One row per resource
            for (int i = 0; i < TradeHudResources.Length; i++)
            {
                TradeResourceType res = TradeHudResources[i];
                ResourceVisualKind iconKind = (ResourceVisualKind)i;

                Button rowBtn = CreateButton($"ResPickerRow{i}", pickerObj.transform, font, out Text rowLabel, string.Empty, 13, new Color(0.11f, 0.14f, 0.19f, 1f), Color.white);
                rowBtn.transition = Selectable.Transition.None;
                rowBtn.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
                rowLabel.text = L(GetTradeResourceShortLabel(res));
                rowLabel.alignment = TextAnchor.MiddleLeft;

                // Icon
                RectTransform iconRoot = CreateUiObject($"ResPickerIcon{i}", rowBtn.transform).GetComponent<RectTransform>();
                iconRoot.anchorMin = new Vector2(0f, 0.5f);
                iconRoot.anchorMax = new Vector2(0f, 0.5f);
                iconRoot.pivot = new Vector2(0f, 0.5f);
                iconRoot.anchoredPosition = new Vector2(10f, 0f);
                iconRoot.sizeDelta = new Vector2(24f, 24f);
                iconRoot.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
                DrawResourceIcon(iconRoot, iconKind);

                // Offset label right of icon, leave room for amount on the right
                rowLabel.rectTransform.offsetMin = new Vector2(42f, 0f);
                rowLabel.rectTransform.offsetMax = new Vector2(-68f, 0f);

                // Amount text — right-aligned inside row
                Text amountTxt = CreateBodyText($"ResPickerAmt{i}", rowBtn.transform, font, string.Empty, 12, TextAnchor.MiddleRight, FleetSecondaryTextColor);
                amountTxt.rectTransform.anchorMin = new Vector2(1f, 0f);
                amountTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
                amountTxt.rectTransform.pivot     = new Vector2(1f, 0.5f);
                amountTxt.rectTransform.anchoredPosition = new Vector2(-10f, 0f);
                amountTxt.rectTransform.sizeDelta  = new Vector2(60f, 0f);

                economyScreenUi.TradeResourceOptionButtons.Add(rowBtn);
                economyScreenUi.TradeResourceOptionTexts.Add(rowLabel);
                economyScreenUi.TradeResourceOptionAmountTexts.Add(amountTxt);

                rowBtn.onClick.AddListener(() =>
                {
                    selectedTradeResourceType = res;
                    isTradeResourceDropdownOpen = false;
                    isEconomyScreenDirty = true;
                    PlayUiSound(uiSelectClip, 0.82f);
                });
            }

            pickerObj.SetActive(false);
            economyScreenUi.TradeResourceOptionsPanel = pickerRect;
        }

        RectTransform actionRow = CreateLayoutRow("TradeActionRow", tradeBody, 38f, 8f);
        actionRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        CreateBodyText("TradeActionLabel", actionRow, font, "Action:", 15, TextAnchor.MiddleLeft, Color.white)
            .gameObject.AddComponent<LayoutElement>().preferredWidth = 118f;

        // [<] [Купить / Продать] [>] cycler — no dropdown needed
        Button prevActionBtn = CreateButton("TradeActionPrev", actionRow, font, out Text prevActionTxt, "<", 16, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        prevActionTxt.fontStyle = FontStyle.Bold;
        prevActionBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 38f;
        prevActionBtn.onClick.AddListener(() => CycleTradeOrderType(-1));

        economyScreenUi.TradeActionDropdownButton = CreateButton("TradeActionDisplay", actionRow, font, out Text tradeActionText, string.Empty, 15, new Color(0.12f, 0.15f, 0.20f, 1f), Color.white);
        economyScreenUi.TradeActionDropdownButton.transition = Selectable.Transition.None;
        economyScreenUi.TradeActionText = tradeActionText;
        economyScreenUi.TradeActionDropdownButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 152f;

        Button nextActionBtn = CreateButton("TradeActionNext", actionRow, font, out Text nextActionTxt, ">", 16, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        nextActionTxt.fontStyle = FontStyle.Bold;
        nextActionBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 38f;
        nextActionBtn.onClick.AddListener(() => CycleTradeOrderType(1));

        economyScreenUi.TradeActionOptionsPanel = null;  // no dropdown panel

        // Spacer pushes amount stepper to right edge
        CreateUiObject("AmountSpacer", actionRow).AddComponent<LayoutElement>().flexibleWidth = 1f;
        economyScreenUi.TradeAmountMinusButton = CreateButton("TradeAmountMinus", actionRow, font, out Text minusText, "-", 18, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        minusText.fontStyle = FontStyle.Bold;
        economyScreenUi.TradeAmountMinusButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;
        economyScreenUi.TradeAmountMinusButton.onClick.AddListener(() =>
        {
            selectedTradeOrderAmount = Mathf.Max(1, selectedTradeOrderAmount - 1);
            isEconomyScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.75f);
        });
        economyScreenUi.TradeAmountText = CreateHeaderText("TradeAmountValue", actionRow, font, string.Empty, 18, TextAnchor.MiddleCenter, Color.white);
        economyScreenUi.TradeAmountText.gameObject.AddComponent<LayoutElement>().preferredWidth = 56f;
        economyScreenUi.TradeAmountPlusButton = CreateButton("TradeAmountPlus", actionRow, font, out Text plusText, "+", 18, new Color(0.18f, 0.21f, 0.27f, 1f), Color.white);
        plusText.fontStyle = FontStyle.Bold;
        economyScreenUi.TradeAmountPlusButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 48f;
        economyScreenUi.TradeAmountPlusButton.onClick.AddListener(() =>
        {
            selectedTradeOrderAmount = Mathf.Min(selectedTradeOrderAmount + 1, 5);
            isEconomyScreenDirty = true;
            PlayUiSound(uiSelectClip, 0.75f);
        });

        RectTransform placeOrderRow = CreateLayoutRow("TradePlaceOrderRow", tradeBody, 46f, 0f);
        placeOrderRow.GetComponent<HorizontalLayoutGroup>().childForceExpandHeight = true;
        economyScreenUi.TradePlaceOrderButton = CreateButton("TradePlaceOrderButton", placeOrderRow, font, out Text placeOrderText, "PLACE ORDER", 20, new Color(0.24f, 0.64f, 0.10f, 1f), Color.white);
        economyScreenUi.TradePlaceOrderButtonText = placeOrderText;
        placeOrderText.fontStyle = FontStyle.Bold;
        economyScreenUi.TradePlaceOrderButton.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        economyScreenUi.TradePlaceOrderButton.onClick.AddListener(CreateTradeHudOrder);

        RectTransform ordersFrame = CreateSectionCard(windowRoot.transform, font, "Active Orders", out RectTransform ordersBody);
        LayoutElement frameLayout = ordersFrame.gameObject.AddComponent<LayoutElement>();
        frameLayout.flexibleHeight = 1f;
        frameLayout.minHeight = 300f;

        GameObject scrollObj = CreateUiObject("TradeOrdersScrollView", ordersBody);
        RectTransform scrollRoot = scrollObj.GetComponent<RectTransform>();
        scrollRoot.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        Image scrollImage = scrollObj.AddComponent<Image>();
        scrollImage.color = new Color(0f, 0f, 0f, 0f);
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.scrollSensitivity = 28f;

        GameObject viewportObj = CreateUiObject("Viewport", scrollObj.transform);
        StretchRect(viewportObj.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        Image viewportImage = viewportObj.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.04f);
        viewportObj.AddComponent<Mask>().showMaskGraphic = false;

        GameObject contentObj = CreateUiObject("TradeOrdersContent", viewportObj.transform);
        RectTransform contentRect = contentObj.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 8f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.viewport = viewportObj.GetComponent<RectTransform>();
        scrollRect.content = contentRect;
        economyScreenUi.ActiveOrdersContent = contentRect;

        economyScreenUi.EmptyOrdersText = CreateBodyText("TradeOrdersEmptyText", contentRect, font, "No active trade orders.", 15, TextAnchor.MiddleCenter, FleetSecondaryTextColor);
        economyScreenUi.EmptyOrdersText.gameObject.AddComponent<LayoutElement>().preferredHeight = 30f;

        for (int i = 0; i < MaxEconomyRowSlots; i++)
        {
            TradeOrderRowUi orderRow = CreateTradeOrderRow(contentRect, font, i);
            orderRow.RemoveButton.onClick.AddListener(() =>
            {
                RemoveTradeHudOrder(orderRow.OrderId);
            });
            economyScreenUi.TradeOrderRows.Add(orderRow);
        }

        AddOverlayCloseButton(windowRect, font);
        economyScreenUi.CanvasRoot.SetActive(false);
        UpdateEconomyScreenUi();
    }

    private static void PlaceDropdownBelow(RectTransform anchor, RectTransform dropdown, RectTransform canvasRect)
    {
        Vector3[] corners = new Vector3[4];
        anchor.GetWorldCorners(corners);
        // corners[0] = bottom-left in screen-pixel space (SSO canvas)
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, new Vector2(corners[0].x, corners[0].y), null, out Vector2 localPos);
        dropdown.anchoredPosition = localPos;
    }

    private RectTransform CreateTradeDropdownOptionsPanel(string name, Transform parent, Font font, int optionCount, List<Button> buttons, List<Text> texts)
    {
        RectTransform panel = CreateStyledPanel(name, parent, new Color(0.08f, 0.10f, 0.14f, 0.98f));
        LayoutElement layout = panel.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = optionCount * 30f + 8f;
        VerticalLayoutGroup group = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        group.padding = new RectOffset(8, 8, 6, 6);
        group.spacing = 4f;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

        for (int i = 0; i < optionCount; i++)
        {
            Button option = CreateButton($"{name}Option{i}", panel, font, out Text optionText, string.Empty, 13, new Color(0.16f, 0.19f, 0.25f, 1f), Color.white);
            option.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;
            buttons.Add(option);
            texts.Add(optionText);
        }

        panel.gameObject.SetActive(false);
        return panel;
    }

    private static TradeOrderRowUi CreateTradeOrderRow(RectTransform parent, Font font, int rowIndex)
    {
        TradeOrderRowUi row = new();
        RectTransform card = CreateStyledPanel($"TradeOrderRow{rowIndex}", parent, FleetInsetColor);
        row.Root = card;
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;

        HorizontalLayoutGroup layout = card.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 6, 6);
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.childAlignment = TextAnchor.MiddleCenter;

        RectTransform tagRoot = CreateUiObject($"TradeOrderTag{rowIndex}", card).GetComponent<RectTransform>();
        tagRoot.gameObject.AddComponent<LayoutElement>().preferredWidth = 86f;
        row.TagBackground = tagRoot.gameObject.AddComponent<Image>();
        row.TagBackground.color = new Color(0.23f, 0.62f, 0.10f, 1f);
        row.TagText = CreateHeaderText("TagText", tagRoot, font, "BUY", 16, TextAnchor.MiddleCenter, Color.white);
        StretchRect(row.TagText.rectTransform, 0f, 0f, 0f, 0f);

        row.OrderText = CreateHeaderText($"TradeOrderText{rowIndex}", card, font, string.Empty, 16, TextAnchor.MiddleLeft, Color.white);
        row.OrderText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        row.RemoveButton = CreateButton($"TradeOrderRemove{rowIndex}", card, font, out Text removeText, "X", 18, new Color(0.74f, 0.55f, 0.08f, 1f), Color.white);
        removeText.fontStyle = FontStyle.Bold;
        row.RemoveButton.gameObject.AddComponent<LayoutElement>().preferredWidth = 46f;
        return row;
    }

    private void UpdateEconomyScreenUi()
    {
        if (economyScreenUi == null) return;

        bool shouldShow = isEconomyPanelOpen;
        if (economyScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            economyScreenUi.CanvasRoot.SetActive(shouldShow);
            isEconomyScreenDirty = true;
        }

        if (!shouldShow) return;
        bool forceLayoutRebuild = isEconomyScreenDirty;

        EnsureTradeHudSelectionValid();
        economyScreenUi.HeaderCountText.text = string.Empty;
        economyScreenUi.TradeResourceText.text = $"{L(GetTradeResourceShortLabel(selectedTradeResourceType))}  ▾";
        economyScreenUi.TradeActionText.text = L(GetTradeModeLabel(selectedTradeOrderType));
        bool isBuyMode = selectedTradeOrderType == TradeOrderType.Buy;
        if (economyScreenUi.TradeActionDropdownButton != null)
            economyScreenUi.TradeActionDropdownButton.image.color = isBuyMode
                ? new Color(0.20f, 0.36f, 0.16f, 1f)
                : new Color(0.40f, 0.16f, 0.14f, 1f);
        economyScreenUi.TradeAmountText.text = selectedTradeOrderAmount.ToString();
        economyScreenUi.TradePlaceOrderButton.interactable = selectedTradeOrderAmount >= 1;
        if (economyScreenUi.TradeResourceOptionsPanel != null)
            economyScreenUi.TradeResourceOptionsPanel.gameObject.SetActive(isTradeResourceDropdownOpen);
        if (economyScreenUi.TradeActionOptionsPanel != null)
        {
            economyScreenUi.TradeActionOptionsPanel.gameObject.SetActive(isTradeActionDropdownOpen);
            if (isTradeActionDropdownOpen)
                PlaceDropdownBelow(economyScreenUi.TradeActionDropdownButton.GetComponent<RectTransform>(), economyScreenUi.TradeActionOptionsPanel, economyScreenUi.CanvasRoot.GetComponent<RectTransform>());
        }
        UpdateTradeDropdownOptions();
        economyScreenUi.EmptyOrdersText.gameObject.SetActive(activeTradeHudOrders.Count == 0);

        for (int i = 0; i < economyScreenUi.TradeOrderRows.Count; i++)
        {
            bool active = i < activeTradeHudOrders.Count;
            TradeOrderRowUi row = economyScreenUi.TradeOrderRows[i];
            row.Root.gameObject.SetActive(active);
            if (!active) continue;

            TradeHudOrder order = activeTradeHudOrders[i];
            row.OrderId = order.Id;
            bool isBuy = order.OrderType == TradeOrderType.Buy;
            row.TagText.text = isBuy ? "BUY" : "SELL";
            row.TagBackground.color = isBuy ? new Color(0.23f, 0.62f, 0.10f, 1f) : new Color(0.72f, 0.12f, 0.10f, 1f);
            row.OrderText.text = $"{row.TagText.text} {order.Amount} {L(GetTradeResourceShortLabel(order.ResourceType))}";
        }

        if (forceLayoutRebuild)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(economyScreenUi.ActiveOrdersContent);
            LayoutRebuilder.ForceRebuildLayoutImmediate(economyScreenUi.WindowRoot);
        }

        LocalizeCanvas(economyScreenUi.CanvasRoot);
        isEconomyScreenDirty = false;
    }

    private void EnsureTradeHudSelectionValid()
    {
        if (System.Array.IndexOf(TradeHudResources, selectedTradeResourceType) < 0)
        {
            selectedTradeResourceType = TradeHudResources[0];
        }

        selectedTradeOrderAmount = Mathf.Max(1, selectedTradeOrderAmount);
    }

    private void UpdateTradeDropdownOptions()
    {
        for (int i = 0; i < economyScreenUi.TradeResourceOptionButtons.Count && i < TradeHudResources.Length; i++)
        {
            TradeResourceType resourceType = TradeHudResources[i];
            bool isSelected = resourceType == selectedTradeResourceType;
            Image image = economyScreenUi.TradeResourceOptionButtons[i].GetComponent<Image>();
            image.color = isSelected ? new Color(0.20f, 0.30f, 0.44f, 1f) : new Color(0.11f, 0.14f, 0.19f, 1f);
            if (i < economyScreenUi.TradeResourceOptionAmountTexts.Count)
                economyScreenUi.TradeResourceOptionAmountTexts[i].text = GetStoredTradeResourceAmount(resourceType).ToString();
        }

        TradeOrderType[] orderTypes = { TradeOrderType.Buy, TradeOrderType.Sell };
        for (int i = 0; i < economyScreenUi.TradeActionOptionTexts.Count && i < orderTypes.Length; i++)
        {
            TradeOrderType orderType = orderTypes[i];
            economyScreenUi.TradeActionOptionTexts[i].text = L(GetTradeModeLabel(orderType));
            Image image = economyScreenUi.TradeActionOptionButtons[i].GetComponent<Image>();
            image.color = orderType == selectedTradeOrderType ? FleetPrimaryButtonColor : new Color(0.16f, 0.19f, 0.25f, 1f);
        }
    }

    private void SelectTradeOrderTypeFromHud(TradeOrderType orderType)
    {
        selectedTradeOrderType = orderType;
        isTradeActionDropdownOpen = false;
        isEconomyScreenDirty = true;
        PlayUiSound(uiSelectClip, 0.82f);
    }

    private void CycleTradeOrderType(int direction)
    {
        TradeOrderType[] types = { TradeOrderType.Buy, TradeOrderType.Sell };
        int idx = System.Array.IndexOf(types, selectedTradeOrderType);
        selectedTradeOrderType = types[(idx + direction + types.Length) % types.Length];
        isEconomyScreenDirty = true;
        PlayUiSound(uiSelectClip, 0.82f);
    }

    private void CreateTradeHudOrder()
    {
        if (selectedTradeOrderAmount < 1)
        {
            return;
        }

        activeTradeHudOrders.Add(new TradeHudOrder
        {
            Id = nextTradeOrderId++,
            ResourceType = selectedTradeResourceType,
            OrderType = selectedTradeOrderType,
            Amount = selectedTradeOrderAmount
        });
        isTradeResourceDropdownOpen = false;
        isTradeActionDropdownOpen = false;
        SessionDebugLogger.Log("TRADE_HUD", $"Created {selectedTradeOrderType} order: {selectedTradeOrderAmount} {GetTradeResourceShortLabel(selectedTradeResourceType)}.");
        PlayUiSound(uiSelectClip, 0.9f);
        TryAutoDispatchNextHudOrder();
    }

    private void RemoveTradeHudOrder(int orderId)
    {
        int removed = activeTradeHudOrders.RemoveAll(order => order.Id == orderId);
        if (removed <= 0)
        {
            return;
        }

        isEconomyScreenDirty = true;
        SessionDebugLogger.Log("TRADE_HUD", $"Removed trade order #{orderId}.");
        PlayUiSound(uiPanelCloseClip, 0.76f);
    }

    private string GetTradeResourceShortLabel(TradeResourceType resourceType)
    {
        return resourceType switch
        {
            TradeResourceType.Logs => "Logs",
            TradeResourceType.Boards => "Boards",
            TradeResourceType.Cotton => "Cotton",
            TradeResourceType.Textile => "Textile",
            TradeResourceType.Furniture => "Furniture",
            _ => resourceType.ToString()
        };
    }

    private TradeResourceType GetAdjacentTradeResource(TradeResourceType current, int direction)
    {
        TradeResourceType[] values = GetTradeCatalogForCurrentZone(selectedTradeOrderType);
        if (values.Length == 0)
        {
            return current;
        }

        int currentIndex = System.Array.IndexOf(values, current);
        if (currentIndex < 0)
        {
            return values[0];
        }

        currentIndex = (currentIndex + direction + values.Length) % values.Length;
        return values[currentIndex];
    }

    private string GetResourcePriority(TradeResourceType resourceType)
    {
        return resourceType switch
        {
            TradeResourceType.Logs => "Low",
            TradeResourceType.Boards => "Medium",
            TradeResourceType.Cotton => "Low",
            TradeResourceType.Textile => "Medium",
            TradeResourceType.Furniture => "High",
            TradeResourceType.Fuel => "Critical",
            TradeResourceType.Alcohol => "Medium",
            TradeResourceType.Food => "High",
            _ => "Unknown"
        };
    }

    private string GetTradeResourceLabel(TradeResourceType resourceType)
    {
        string baseName = resourceType switch
        {
            TradeResourceType.Logs      => "Logs",
            TradeResourceType.Boards    => "Boards",
            TradeResourceType.Cotton    => "Cotton",
            TradeResourceType.Textile   => "Textile",
            TradeResourceType.Furniture => "Furniture",
            TradeResourceType.Fuel      => "Fuel",
            TradeResourceType.Alcohol   => "Alcohol",
            TradeResourceType.Food      => "Food",
            _ => resourceType.ToString()
        };
        string priority = GetResourcePriority(resourceType);
        return $"{baseName} ({priority})";
    }

    private bool CanDispatchTradeRun()
    {
        return TryGetTradeDispatchContext(out _, out _, out _);
    }

    private string BuildTradeDispatchStatusText()
    {
        if (HasActiveTradeRun())
        {
            return GetTradeRunStatusLabel();
        }

        if (!TryGetTradeDispatchContext(out _, out _, out string blockReason))
        {
            return blockReason;
        }

        if (tradeDispatchStatusText == "Assign an Intercity driver to unlock trade dispatch.")
        {
            return "Ready to dispatch via edge highway";
        }

        return tradeDispatchStatusText;
    }

    private void HandleTradeDispatchRequested()
    {
        if (!BeginTradeRun())
        {
            tradeDispatchStatusText = BuildTradeDispatchStatusText();
            isEconomyScreenDirty = true;
            return;
        }

        PlayUiSound(uiSelectClip, 0.88f);
        isEconomyScreenDirty = true;
    }

    private static string BuildEconomyEntryDetail(MoneyLedgerEntry entry)
    {
        string detail = entry.Reason;
        if (entry.RecipientBalanceAfter.HasValue)
        {
            detail += $" вЂў {entry.ToLabel} balance: ${entry.RecipientBalanceAfter.Value}";
        }

        if (entry.TreasuryAfter.HasValue)
        {
            detail += $" вЂў Treasury: ${entry.TreasuryAfter.Value}";
        }

        return detail;
    }

    private void SetupWorldMapScreenUi()
    {
        if (worldMapScreenUi != null) return;

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        worldMapScreenUi = new WorldMapScreenUiRefs();

        GameObject canvasObject = new("WorldMapScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        worldMapScreenUi.CanvasRoot = canvasObject;

        // Fullscreen backdrop вЂ" sits directly on the canvas, covers everything below
        GameObject backdropGo = CreateUiObject("WorldMapBackdrop", canvasObject.transform);
        RectTransform backdropRect = backdropGo.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.sizeDelta = Vector2.zero;
        backdropGo.AddComponent<Image>().color = new Color(0.04f, 0.06f, 0.09f, 0.96f);

        GameObject windowRoot = CreateUiObject("WorldMapWindowRoot", canvasObject.transform);
        RectTransform windowRect = windowRoot.GetComponent<RectTransform>();
        windowRect.anchorMin = Vector2.zero;
        windowRect.anchorMax = Vector2.one;
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.anchoredPosition = Vector2.zero;
        windowRect.sizeDelta = Vector2.zero;
        worldMapScreenUi.WindowRoot = windowRect;

        VerticalLayoutGroup rootLayout = windowRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding = new RectOffset(28, 28, 28, 28);
        rootLayout.spacing = 16;
        rootLayout.childControlWidth = true;
        rootLayout.childControlHeight = true;
        rootLayout.childForceExpandWidth = true;
        rootLayout.childForceExpandHeight = false;

        RectTransform headerRow = CreateLayoutRow("WorldMapHeaderRow", windowRoot.transform, 44f, 0f);
        worldMapScreenUi.TitleText = CreateHeaderText("WorldMapTitle", headerRow, font, "Regional Map", 24, TextAnchor.MiddleLeft, Color.white);
        worldMapScreenUi.TitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        worldMapScreenUi.SubtitleText = CreateBodyText("WorldMapSubtitle", headerRow, font, string.Empty, 13, TextAnchor.MiddleRight, FleetSecondaryTextColor);

        RectTransform contentRow = CreateUiObject("WorldMapContentRow", windowRoot.transform).GetComponent<RectTransform>();
        LayoutElement contentLayout = contentRow.gameObject.AddComponent<LayoutElement>();
        contentLayout.flexibleHeight = 1f;
        HorizontalLayoutGroup contentGroup = contentRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        contentGroup.spacing = 16f;
        contentGroup.childControlWidth = true;
        contentGroup.childControlHeight = true;
        contentGroup.childForceExpandWidth = true;
        contentGroup.childForceExpandHeight = true;

        RectTransform mapCard = CreateSectionCard(contentRow, font, "Region Grid", out RectTransform mapBody);
        LayoutElement mapCardLayout = mapCard.gameObject.AddComponent<LayoutElement>();
        mapCardLayout.preferredWidth = 600f;
        mapCardLayout.flexibleWidth = 1f;
        mapCardLayout.flexibleHeight = 1f;

        worldMapScreenUi.SelectionHintText = CreateBodyText(
            "WorldMapHint",
            mapBody,
            font,
            "Click a region cell to inspect its resource profile.",
            13,
            TextAnchor.MiddleLeft,
            FleetSecondaryTextColor);

        RectTransform mapFrame = CreateStyledPanel("WorldMapFrame", mapBody, FleetCardMutedColor);
        LayoutElement mapFrameLayout = mapFrame.gameObject.AddComponent<LayoutElement>();
        mapFrameLayout.preferredHeight = 480f;
        VerticalLayoutGroup mapFrameLayoutGroup = mapFrame.gameObject.AddComponent<VerticalLayoutGroup>();
        mapFrameLayoutGroup.padding = new RectOffset(16, 16, 16, 16);
        mapFrameLayoutGroup.spacing = 10;
        mapFrameLayoutGroup.childControlWidth = true;
        mapFrameLayoutGroup.childControlHeight = true;
        mapFrameLayoutGroup.childForceExpandWidth = true;
        mapFrameLayoutGroup.childForceExpandHeight = false;

        RectTransform mapSurface = CreateUiObject("WorldMapSurface", mapFrame).GetComponent<RectTransform>();
        LayoutElement mapSurfaceLayout = mapSurface.gameObject.AddComponent<LayoutElement>();
        mapSurfaceLayout.preferredHeight = 420f;
        Image mapSurfaceBackground = mapSurface.gameObject.AddComponent<Image>();
        mapSurfaceBackground.color = new Color(0.11f, 0.14f, 0.19f, 0.72f);

        RectTransform gridRoot = CreateUiObject("WorldMapGridRoot", mapSurface).GetComponent<RectTransform>();
        gridRoot.anchorMin = Vector2.zero;
        gridRoot.anchorMax = Vector2.one;
        gridRoot.offsetMin = Vector2.zero;
        gridRoot.offsetMax = Vector2.zero;
        LayoutElement gridLayoutElement = gridRoot.gameObject.AddComponent<LayoutElement>();
        gridLayoutElement.preferredHeight = 420f;
        GridLayoutGroup gridLayout = gridRoot.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(170f, 120f);
        gridLayout.spacing = new Vector2(10f, 10f);
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 3;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        for (int regionIndex = 0; regionIndex < 9; regionIndex++)
        {
            worldMapScreenUi.Cells.Add(CreateWorldMapCell(gridRoot, font, regionIndex));
        }

        RectTransform detailsCard = CreateSectionCard(contentRow, font, "Region Preview", out RectTransform detailsBody);
        LayoutElement detailsCardLayout = detailsCard.gameObject.AddComponent<LayoutElement>();
        detailsCardLayout.preferredWidth = 448f;
        detailsCardLayout.flexibleWidth = 0f;
        detailsCardLayout.flexibleHeight = 1f;

        RectTransform previewContainer = CreateStyledPanel("WorldMapDetailPreviewContainer", detailsBody, new Color(0.12f, 0.15f, 0.20f, 0.98f));
        LayoutElement previewContainerLayout = previewContainer.gameObject.AddComponent<LayoutElement>();
        previewContainerLayout.flexibleHeight = 1f;
        previewContainerLayout.minHeight = 200f;
        worldMapScreenUi.DetailPreview = CreateWorldMapDetailPreview(previewContainer, font);

        RectTransform infoPanel = CreateStyledPanel("WorldMapDetailInfoPanel", detailsBody, FleetCardMutedColor);
        infoPanel.gameObject.AddComponent<LayoutElement>().preferredHeight = 190f;
        VerticalLayoutGroup infoLayout = infoPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        infoLayout.padding = new RectOffset(14, 14, 12, 12);
        infoLayout.spacing = 6f;
        infoLayout.childControlWidth = true;
        infoLayout.childControlHeight = true;
        infoLayout.childForceExpandWidth = true;
        infoLayout.childForceExpandHeight = false;

        worldMapScreenUi.DetailsNameText = CreateHeaderText("WorldMapDetailsName", infoPanel, font, string.Empty, 22, TextAnchor.MiddleLeft, Color.white);
        worldMapScreenUi.DetailsStatusText = CreateBodyText("WorldMapDetailsStatus", infoPanel, font, string.Empty, 13, TextAnchor.MiddleLeft, FleetMutedTextColor);
        CreateHeaderText("WorldMapResourcesLabel", infoPanel, font, "Produced Resources", 11, TextAnchor.MiddleLeft, FleetMutedTextColor);
        worldMapScreenUi.DetailsResourcesText = CreateHeaderText("WorldMapDetailsResources", infoPanel, font, string.Empty, 17, TextAnchor.MiddleLeft, FleetAccentColor);
        worldMapScreenUi.DetailsDescriptionText = CreateBodyText("WorldMapDetailsDescription", infoPanel, font, string.Empty, 12, TextAnchor.UpperLeft, FleetSecondaryTextColor);
        worldMapScreenUi.DetailsDescriptionText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        RectTransform footerCard = CreateSectionCard(windowRoot.transform, font, "Current Trade Context", out RectTransform footerBody);
        footerCard.gameObject.AddComponent<LayoutElement>().preferredHeight = 74f;
        CreateBodyText(
            "WorldMapFooter",
            footerBody,
            font,
            "Current gameplay rule: textile comes from another region, so the map shows where it conceptually enters your economy.",
            13,
            TextAnchor.MiddleLeft,
            FleetAccentColor);

        AddOverlayCloseButton(windowRect, font);
        worldMapScreenUi.CanvasRoot.SetActive(false);
        UpdateWorldMapScreenUi();
    }

    private WorldMapCellUi CreateWorldMapCell(RectTransform parent, Font font, int regionIndex)
    {
        WorldMapCellUi cell = new();
        GameObject cellObject = CreateUiObject($"WorldMapCell_{regionIndex}", parent);
        RectTransform cellRect = cellObject.GetComponent<RectTransform>();
        LayoutElement layoutElement = cellRect.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 170f;
        layoutElement.preferredHeight = 120f;

        Image background = cellObject.AddComponent<Image>();
        background.color = FleetInsetColor;
        Button button = cellObject.AddComponent<Button>();
        Outline outline = cellObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.22f);
        outline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup layout = cellObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 10);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        RectTransform previewRoot = CreateStyledPanel($"WorldMapCellPreview_{regionIndex}", cellRect, new Color(0.12f, 0.15f, 0.20f, 0.98f));
        LayoutElement previewLayout = previewRoot.gameObject.AddComponent<LayoutElement>();
        previewLayout.preferredHeight = 70f;
        cell.PreviewBackground = previewRoot.GetComponent<Image>();

        cell.PreviewPlaceholderText = CreateBodyText($"WorldMapCellPreviewPlaceholder_{regionIndex}", previewRoot, font, string.Empty, 12, TextAnchor.MiddleCenter, FleetMutedTextColor);
        StretchRect(cell.PreviewPlaceholderText.rectTransform, 10f, 10f, 10f, 10f);

        cell.WaterShape = CreateWorldMapPreviewShape(previewRoot, $"WorldMapWater_{regionIndex}", new Color(0.54f, 0.77f, 0.92f, 0.95f), 0f, 0.76f, 1f, 0.24f);
        cell.HighwayShape = CreateWorldMapPreviewShape(previewRoot, $"WorldMapHighway_{regionIndex}", new Color(0.15f, 0.17f, 0.20f, 1f), 0.04f, 0.08f, 0.92f, 0.16f);
        cell.ForestShape = CreateWorldMapPreviewShape(previewRoot, $"WorldMapForest_{regionIndex}", new Color(0.19f, 0.39f, 0.24f, 0.98f), 0.06f, 0.36f, 0.28f, 0.30f);
        cell.TownBlockA = CreateWorldMapPreviewShape(previewRoot, $"WorldMapTownA_{regionIndex}", new Color(0.83f, 0.72f, 0.46f, 0.96f), 0.40f, 0.30f, 0.16f, 0.16f);
        cell.TownBlockB = CreateWorldMapPreviewShape(previewRoot, $"WorldMapTownB_{regionIndex}", new Color(0.86f, 0.78f, 0.55f, 0.96f), 0.58f, 0.30f, 0.18f, 0.18f);
        cell.TownBlockC = CreateWorldMapPreviewShape(previewRoot, $"WorldMapTownC_{regionIndex}", new Color(0.76f, 0.63f, 0.34f, 0.96f), 0.50f, 0.50f, 0.12f, 0.12f);
        cell.HighwayDashA = CreateWorldMapPreviewShape(previewRoot, $"WorldMapDashA_{regionIndex}", new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.18f, 0.12f, 0.10f, 0.03f);
        cell.HighwayDashB = CreateWorldMapPreviewShape(previewRoot, $"WorldMapDashB_{regionIndex}", new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.45f, 0.12f, 0.10f, 0.03f);
        cell.HighwayDashC = CreateWorldMapPreviewShape(previewRoot, $"WorldMapDashC_{regionIndex}", new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.72f, 0.12f, 0.10f, 0.03f);

        cell.NameText = CreateHeaderText($"WorldMapCellName_{regionIndex}", cellRect, font, string.Empty, 15, TextAnchor.MiddleLeft, Color.white);
        cell.TypeText = CreateBodyText($"WorldMapCellType_{regionIndex}", cellRect, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetMutedTextColor);

        cell.Button = button;
        cell.Background = background;
        cell.Outline = outline;
        cell.RegionIndex = regionIndex;
        cell.Button.onClick.AddListener(() => SelectWorldMapRegion(regionIndex));
        return cell;
    }

    private WorldMapDetailPreviewUi CreateWorldMapDetailPreview(RectTransform parent, Font font)
    {
        WorldMapDetailPreviewUi preview = new();
        preview.PreviewBackground = parent.GetComponent<Image>();

        preview.PlaceholderText = CreateBodyText("WorldMapDetailPlaceholder", parent, font,
            "No regional map yet", 14, TextAnchor.MiddleCenter, FleetMutedTextColor);
        StretchRect(preview.PlaceholderText.rectTransform, 10f, 10f, 10f, 10f);

        preview.WaterShape   = CreateWorldMapPreviewShape(parent, "WorldMapDetailWater",    new Color(0.54f, 0.77f, 0.92f, 0.95f), 0f,    0.76f, 1f,    0.24f);
        preview.HighwayShape = CreateWorldMapPreviewShape(parent, "WorldMapDetailHighway",  new Color(0.15f, 0.17f, 0.20f, 1f),    0.04f, 0.08f, 0.92f, 0.16f);
        preview.ForestShape  = CreateWorldMapPreviewShape(parent, "WorldMapDetailForest",   new Color(0.19f, 0.39f, 0.24f, 0.98f), 0.06f, 0.36f, 0.28f, 0.30f);
        preview.TownBlockA   = CreateWorldMapPreviewShape(parent, "WorldMapDetailTownA",    new Color(0.83f, 0.72f, 0.46f, 0.96f), 0.40f, 0.30f, 0.16f, 0.16f);
        preview.TownBlockB   = CreateWorldMapPreviewShape(parent, "WorldMapDetailTownB",    new Color(0.86f, 0.78f, 0.55f, 0.96f), 0.58f, 0.30f, 0.18f, 0.18f);
        preview.TownBlockC   = CreateWorldMapPreviewShape(parent, "WorldMapDetailTownC",    new Color(0.76f, 0.63f, 0.34f, 0.96f), 0.50f, 0.50f, 0.12f, 0.12f);
        preview.HighwayDashA = CreateWorldMapPreviewShape(parent, "WorldMapDetailDashA",    new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.18f, 0.12f, 0.10f, 0.03f);
        preview.HighwayDashB = CreateWorldMapPreviewShape(parent, "WorldMapDetailDashB",    new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.45f, 0.12f, 0.10f, 0.03f);
        preview.HighwayDashC = CreateWorldMapPreviewShape(parent, "WorldMapDetailDashC",    new Color(0.95f, 0.93f, 0.82f, 0.95f), 0.72f, 0.12f, 0.10f, 0.03f);

        return preview;
    }

    private Image CreateWorldMapPreviewShape(RectTransform parent, string name, Color color, float x, float y, float width, float height)
    {
        GameObject obj = CreateUiObject(name, parent);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(x, y);
        rect.anchorMax = new Vector2(x + width, y + height);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private void SelectWorldMapRegion(int regionIndex)
    {
        selectedWorldMapRegionIndex = Mathf.Clamp(regionIndex, 0, 8);
        isWorldMapScreenDirty = true;
        PlayUiSound(uiSelectClip, 0.82f);
    }

    private static string GetWorldMapRegionName(int regionIndex)
    {
        return regionIndex switch
        {
            0 => "North Ridge",
            1 => "Forest Belt",
            2 => "River Port",
            3 => "Cotton Plains",
            4 => "Your Town",
            5 => "Textile District",
            6 => "Dry South",
            7 => "Freight Steppe",
            8 => "Coastal Gate",
            _ => "Unknown Region"
        };
    }

    private static string GetWorldMapRegionTypeLabel(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "Current region",
            2 or 3 or 5 => "Neighbor region",
            _ => "Empty region slot"
        };
    }

    private static string GetWorldMapRegionProducedResources(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "Logs, Boards, Furniture",
            5 => "Textile",
            3 => "Cotton",
            2 => "Trade logistics",
            _ => "No confirmed survey data"
        };
    }

    private static string GetWorldMapRegionDescription(int regionIndex)
    {
        return regionIndex switch
        {
            4 => "This is your active simulation region. It contains the current town, highways, production buildings, and local roads.",
            5 => "A neighboring industrial district focused on textile output. This is the important current external source for textile supply.",
            3 => "A farming-heavy region that supplies raw cotton into the wider trade network.",
            2 => "A schematic route hub near the river corridor, reserved for future logistics and regional expansion passes.",
            _ => "This region exists on the wider map, but it has not been fully designed or assigned concrete production data yet."
        };
    }

    private static bool IsWorldMapRegionKnown(int regionIndex)
    {
        return regionIndex == 2 || regionIndex == 3 || regionIndex == 4 || regionIndex == 5;
    }

    private void UpdateWorldMapScreenUi()
    {
        if (worldMapScreenUi == null) return;

        bool shouldShow = isWorldMapPanelOpen;
        if (worldMapScreenUi.CanvasRoot.activeSelf != shouldShow)
        {
            worldMapScreenUi.CanvasRoot.SetActive(shouldShow);
            isWorldMapScreenDirty = true;
        }

        if (!shouldShow || !isWorldMapScreenDirty)
        {
            return;
        }

        worldMapScreenUi.TitleText.text = "Regional Map";
        worldMapScreenUi.SubtitleText.text = "Open/Close: M";
        worldMapScreenUi.SelectionHintText.text = "Each cell is a mini-map of a larger world region. Only the current region is drawn for now.";

        for (int i = 0; i < worldMapScreenUi.Cells.Count; i++)
        {
            WorldMapCellUi cell = worldMapScreenUi.Cells[i];
            bool isSelected = i == selectedWorldMapRegionIndex;
            bool isCurrent = i == 4;
            bool isKnown = IsWorldMapRegionKnown(i);
            bool hasRealPreview = isCurrent;

            cell.NameText.text = GetWorldMapRegionName(i);
            cell.TypeText.text = GetWorldMapRegionTypeLabel(i);
            cell.Background.color = isSelected
                ? FleetSelectedRowColor
                : isCurrent
                    ? new Color(0.30f, 0.24f, 0.10f, 0.98f)
                    : isKnown
                        ? FleetInsetColor
                        : FleetCardMutedColor;
            cell.NameText.color = isKnown || isCurrent ? Color.white : FleetSecondaryTextColor;
            cell.TypeText.color = isSelected ? FleetAccentColor : FleetMutedTextColor;
            cell.PreviewBackground.color = hasRealPreview
                ? new Color(0.18f, 0.20f, 0.17f, 1f)
                : new Color(0.15f, 0.17f, 0.21f, 0.98f);
            cell.PreviewPlaceholderText.gameObject.SetActive(!hasRealPreview);
            cell.PreviewPlaceholderText.text = i == 4 ? string.Empty : "No regional map yet";
            cell.WaterShape.gameObject.SetActive(hasRealPreview);
            cell.HighwayShape.gameObject.SetActive(hasRealPreview);
            cell.ForestShape.gameObject.SetActive(hasRealPreview);
            cell.TownBlockA.gameObject.SetActive(hasRealPreview);
            cell.TownBlockB.gameObject.SetActive(hasRealPreview);
            cell.TownBlockC.gameObject.SetActive(hasRealPreview);
            cell.HighwayDashA.gameObject.SetActive(hasRealPreview);
            cell.HighwayDashB.gameObject.SetActive(hasRealPreview);
            cell.HighwayDashC.gameObject.SetActive(hasRealPreview);
            if (cell.Outline != null)
            {
                cell.Outline.effectColor = isSelected
                    ? new Color(FleetAccentColor.r, FleetAccentColor.g, FleetAccentColor.b, 0.72f)
                    : new Color(0f, 0f, 0f, 0.22f);
                cell.Outline.effectDistance = isSelected ? new Vector2(2f, -2f) : new Vector2(1f, -1f);
            }
        }

        int selected = Mathf.Clamp(selectedWorldMapRegionIndex, 0, 8);
        bool detailHasPreview = selected == 4;

        if (worldMapScreenUi.DetailPreview != null)
        {
            worldMapScreenUi.DetailPreview.PlaceholderText.gameObject.SetActive(!detailHasPreview);
            worldMapScreenUi.DetailPreview.PreviewBackground.color = detailHasPreview
                ? new Color(0.18f, 0.20f, 0.17f, 1f)
                : new Color(0.12f, 0.15f, 0.20f, 0.98f);
            worldMapScreenUi.DetailPreview.WaterShape.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.HighwayShape.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.ForestShape.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.TownBlockA.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.TownBlockB.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.TownBlockC.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.HighwayDashA.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.HighwayDashB.gameObject.SetActive(detailHasPreview);
            worldMapScreenUi.DetailPreview.HighwayDashC.gameObject.SetActive(detailHasPreview);
        }

        worldMapScreenUi.DetailsNameText.text = GetWorldMapRegionName(selected);
        worldMapScreenUi.DetailsStatusText.text = GetWorldMapRegionTypeLabel(selected);
        worldMapScreenUi.DetailsResourcesText.text = GetWorldMapRegionProducedResources(selected);
        worldMapScreenUi.DetailsDescriptionText.text = GetWorldMapRegionDescription(selected);

        LayoutRebuilder.ForceRebuildLayoutImmediate(worldMapScreenUi.WindowRoot);
        LocalizeCanvas(worldMapScreenUi.CanvasRoot);
        isWorldMapScreenDirty = false;
    }
}

