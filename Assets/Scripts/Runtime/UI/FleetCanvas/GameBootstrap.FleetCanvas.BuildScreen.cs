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
            UnlockDefaultRoadBuildTools();
            return;
        }

        foreach (BuildTool tool in System.Enum.GetValues(typeof(BuildTool)))
        {
            if (tool == BuildTool.None) continue;
            unlockedBuildTools.Add(tool);
        }
    }

    private void UnlockDefaultRoadBuildTools()
    {
        unlockedBuildTools?.Add(BuildTool.SingleRoad);
        unlockedBuildTools?.Add(BuildTool.Road);
    }

    private bool IsBuildToolUnlocked(BuildTool tool)
    {
        return unlockedBuildTools != null
            ? unlockedBuildTools.Contains(tool)
            : tool == BuildTool.SingleRoad || tool == BuildTool.Road;
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

    private void UnlockAllBuildTools()
    {
        if (unlockedBuildTools == null)
        {
            InitUnlockedBuildTools();
        }

        foreach (BuildTool tool in System.Enum.GetValues(typeof(BuildTool)))
        {
            if (tool == BuildTool.None) continue;
            unlockedBuildTools.Add(tool);
        }

        isBuildScreenDirty = true;
        SessionDebugLogger.Log("BUILD", "All build tools unlocked.");
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
                (BuildTool.Parking,          "PK", "Parking",           new Color(0.28f, 0.30f, 0.38f)),
                (BuildTool.Warehouse,        "WH", "Warehouse",         new Color(0.70f, 0.52f, 0.30f)),
                (BuildTool.SingleRoad,       "1R", "One-Lane Road",     new Color(0.22f, 0.34f, 0.46f)),
                (BuildTool.Road,             "2W", "Two-Way Road",      new Color(0.27f, 0.42f, 0.60f)),
                (BuildTool.Stop,             "ST", "Bus Stop",          new Color(0.72f, 0.28f, 0.24f)),
                (BuildTool.Motel,            "MT", "Motel",             new Color(0.24f, 0.48f, 0.36f))),
            CreateBuildCategory(cardList, font, "Production", "Производство", false,
                (BuildTool.Forest,           "LC", "Lumberjack Camp",    new Color(0.28f, 0.45f, 0.18f)),
                (BuildTool.Sawmill,          "SW", "Sawmill",           new Color(0.58f, 0.36f, 0.16f)),
                (BuildTool.FurnitureFactory, "FF", "Furniture Factory", new Color(0.46f, 0.26f, 0.52f))),
            CreateBuildCategory(cardList, font, "Services", "Сервисы", false,
                (BuildTool.Bar,          "BR", "Bar",          new Color(0.52f, 0.20f, 0.20f)),
                (BuildTool.Canteen,      "CT", "Canteen",      new Color(0.20f, 0.42f, 0.50f)),
                (BuildTool.GasStation,   "GS", "Gas Station",  new Color(0.84f, 0.68f, 0.26f)),
                (BuildTool.GamblingHall, "GH", "Gambling Hall",  new Color(0.52f, 0.38f, 0.08f)),
                (BuildTool.CityPark,     "CP", "City Park",      new Color(0.22f, 0.48f, 0.22f)),
                (BuildTool.PersonalHouse,"PH", "Personal House", new Color(0.55f, 0.42f, 0.30f)),
                (BuildTool.CarMarket,    "CM", "Car Market",     new Color(0.64f, 0.52f, 0.38f))),
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

        CreateBuildAccentVisual(accentStrip, font, tool, abbrev);

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

    private void CreateBuildAccentVisual(RectTransform accentStrip, Font font, BuildTool tool, string abbrev)
    {
        // Accent strip: 68px wide, 72px tall.  Local helpers:
        // P(ax,ay,bx,by,col) uses anchor-based rects (0..1 range).
        // R(cx,cy,w,h,col,rot) uses pivot-centered rects with optional rotation.

        RectTransform P(float ax, float ay, float bx, float by, Color col)
        {
            GameObject g = new("Ic");
            g.transform.SetParent(accentStrip, false);
            RectTransform rt = g.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax, ay);
            rt.anchorMax = new Vector2(bx, by);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            g.AddComponent<Image>().color = col;
            return rt;
        }

        void R(float cx, float cy, float w, float h, Color col, float rot = 0f)
        {
            GameObject g = new("IcR");
            g.transform.SetParent(accentStrip, false);
            RectTransform rt = g.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(cx, cy);
            if (rot != 0f) rt.localEulerAngles = new Vector3(0f, 0f, rot);
            g.AddComponent<Image>().color = col;
        }

        switch (tool)
        {
            // ── Parking ───────────────────────────────────────────────────────
            case BuildTool.Parking:
            {
                Color asph = new(0.20f, 0.20f, 0.22f);
                Color roof = new(0.18f, 0.20f, 0.26f);
                Color post = new(0.32f, 0.34f, 0.38f);
                Color line = new(0.88f, 0.88f, 0.82f);
                // Asphalt base
                P(0.06f, 0.10f, 0.94f, 0.52f, asph);
                // 3 parking bays (4 dividing lines)
                P(0.10f, 0.12f, 0.14f, 0.50f, line);
                P(0.38f, 0.12f, 0.42f, 0.50f, line);
                P(0.62f, 0.12f, 0.66f, 0.50f, line);
                P(0.86f, 0.12f, 0.90f, 0.50f, line);
                // Canopy roof
                P(0.06f, 0.54f, 0.94f, 0.66f, roof);
                // Two support posts
                P(0.08f, 0.36f, 0.18f, 0.66f, post);
                P(0.82f, 0.36f, 0.92f, 0.66f, post);
                break;
            }

            // One-lane Road
            case BuildTool.SingleRoad:
            {
                accentStrip.GetComponent<Image>().color = new Color(0.13f, 0.16f, 0.22f, 1f);
                P(0.28f, 0.18f, 0.72f, 0.78f, new Color(0.15f, 0.17f, 0.20f));
                P(0.47f, 0.18f, 0.53f, 0.78f, new Color(0.95f, 0.93f, 0.82f));
                Text lbl = CreateHeaderText("Lbl", accentStrip, font, abbrev, 12, TextAnchor.MiddleCenter, Color.white);
                RectTransform lr = lbl.GetComponent<RectTransform>();
                lr.anchorMin = new Vector2(0f, 0f); lr.anchorMax = new Vector2(1f, 0f);
                lr.pivot = new Vector2(0.5f, 0f); lr.sizeDelta = new Vector2(0f, 18f); lr.anchoredPosition = new Vector2(0f, 4f);
                break;
            }

            // ── Road ─────────────────────────────────────────────────────────
            case BuildTool.Road:
            {
                accentStrip.GetComponent<Image>().color = new Color(0.13f, 0.16f, 0.22f, 1f);
                // Asphalt surface
                P(0.18f, 0.18f, 0.82f, 0.78f, new Color(0.15f, 0.17f, 0.20f));
                // Yellow center dash
                P(0.45f, 0.12f, 0.55f, 0.88f, new Color(0.95f, 0.82f, 0.32f));
                // White edge lines
                P(0.16f, 0.10f, 0.20f, 0.90f, new Color(0.95f, 0.93f, 0.82f));
                P(0.80f, 0.10f, 0.84f, 0.90f, new Color(0.95f, 0.93f, 0.82f));
                // "2W" label at bottom
                Text lbl = CreateHeaderText("Lbl", accentStrip, font, abbrev, 12, TextAnchor.MiddleCenter, Color.white);
                RectTransform lr = lbl.GetComponent<RectTransform>();
                lr.anchorMin = new Vector2(0f, 0f); lr.anchorMax = new Vector2(1f, 0f);
                lr.pivot = new Vector2(0.5f, 0f); lr.sizeDelta = new Vector2(0f, 18f); lr.anchoredPosition = new Vector2(0f, 4f);
                break;
            }

            // ── Bus Stop ─────────────────────────────────────────────────────
            case BuildTool.Stop:
            {
                Color w = Color.white; Color bench = new(0.68f, 0.84f, 0.96f);
                // Left + right posts
                P(0.20f, 0.26f, 0.30f, 0.72f, w);
                P(0.70f, 0.26f, 0.80f, 0.72f, w);
                // Shelter roof
                P(0.08f, 0.70f, 0.92f, 0.80f, w);
                // Bench
                P(0.22f, 0.42f, 0.78f, 0.50f, bench);
                P(0.22f, 0.50f, 0.26f, 0.62f, bench); // bench leg L
                P(0.74f, 0.50f, 0.78f, 0.62f, bench); // bench leg R
                // Ground line
                P(0.08f, 0.24f, 0.92f, 0.28f, new Color(0.9f, 0.9f, 0.9f, 0.6f));
                break;
            }

            // ── Motel ────────────────────────────────────────────────────────
            case BuildTool.Motel:
            {
                Color cream = new(0.91f, 0.87f, 0.74f); Color red = new(0.76f, 0.22f, 0.18f);
                Color yellow = new(0.98f, 0.84f, 0.16f); Color blue = new(0.62f, 0.80f, 0.92f);
                Color dark = new(0.14f, 0.12f, 0.10f);
                // Building body
                P(0.12f, 0.18f, 0.88f, 0.56f, cream);
                // Red roof overhang
                P(0.08f, 0.54f, 0.92f, 0.64f, red);
                // MOTEL neon sign bar
                P(0.16f, 0.64f, 0.84f, 0.74f, yellow);
                // Windows
                P(0.16f, 0.36f, 0.36f, 0.50f, blue);
                P(0.64f, 0.36f, 0.84f, 0.50f, blue);
                // Door
                P(0.42f, 0.18f, 0.58f, 0.44f, dark);
                P(0.48f, 0.30f, 0.52f, 0.36f, new Color(0.8f, 0.7f, 0.4f)); // knob
                break;
            }

            // ── Sawmill ──────────────────────────────────────────────────────
            case BuildTool.Forest:
            {
                Color hut = new(0.46f, 0.30f, 0.15f);
                Color roof = new(0.22f, 0.16f, 0.10f);
                Color trunk = new(0.34f, 0.21f, 0.10f);
                Color leaf = new(0.18f, 0.42f, 0.16f);
                Color log = new(0.55f, 0.34f, 0.16f);
                P(0.12f, 0.18f, 0.58f, 0.50f, hut);
                P(0.08f, 0.50f, 0.62f, 0.64f, roof);
                P(0.28f, 0.18f, 0.42f, 0.38f, new Color(0.18f, 0.12f, 0.08f));
                P(0.70f, 0.12f, 0.80f, 0.42f, trunk);
                P(0.60f, 0.38f, 0.90f, 0.58f, leaf);
                P(0.64f, 0.56f, 0.86f, 0.72f, leaf);
                P(0.12f, 0.08f, 0.50f, 0.16f, log);
                P(0.18f, 0.02f, 0.56f, 0.10f, log);
                break;
            }

            case BuildTool.Sawmill:
            {
                Color log = new(0.34f, 0.21f, 0.10f); Color grain = new(0.44f, 0.30f, 0.16f);
                Color blade = new(0.76f, 0.76f, 0.76f); Color shine = new(0.94f, 0.94f, 0.94f);
                Color hole = new(0.22f, 0.14f, 0.08f);
                // Log horizontal
                P(0.08f, 0.38f, 0.92f, 0.62f, log);
                P(0.10f, 0.43f, 0.90f, 0.57f, grain);
                // Circular saw blade (vertical strip)
                P(0.36f, 0.08f, 0.64f, 0.92f, blade);
                P(0.40f, 0.12f, 0.60f, 0.88f, shine);
                // Center hole of blade
                P(0.44f, 0.42f, 0.56f, 0.58f, hole);
                // Blade teeth suggestions (top + bottom notches)
                P(0.34f, 0.08f, 0.38f, 0.14f, blade); P(0.42f, 0.06f, 0.46f, 0.11f, blade);
                P(0.54f, 0.06f, 0.58f, 0.11f, blade); P(0.62f, 0.08f, 0.66f, 0.14f, blade);
                P(0.34f, 0.86f, 0.38f, 0.92f, blade); P(0.42f, 0.89f, 0.46f, 0.94f, blade);
                P(0.54f, 0.89f, 0.58f, 0.94f, blade); P(0.62f, 0.86f, 0.66f, 0.92f, blade);
                break;
            }

            // ── Furniture Factory ─────────────────────────────────────────────
            case BuildTool.FurnitureFactory:
            {
                Color body = new(0.50f, 0.48f, 0.52f); Color win = new(0.62f, 0.76f, 0.90f);
                Color chim = new(0.34f, 0.32f, 0.36f); Color smoke = new(0.84f, 0.84f, 0.84f, 0.9f);
                Color dark = new(0.12f, 0.11f, 0.12f);
                // Building body
                P(0.10f, 0.16f, 0.90f, 0.58f, body);
                // Windows strip
                P(0.14f, 0.40f, 0.86f, 0.50f, win);
                // Chimneys
                P(0.18f, 0.56f, 0.36f, 0.78f, chim);
                P(0.64f, 0.56f, 0.82f, 0.78f, chim);
                // Smoke puffs
                P(0.20f, 0.76f, 0.34f, 0.86f, smoke);
                P(0.66f, 0.76f, 0.80f, 0.86f, smoke);
                // Door
                P(0.44f, 0.16f, 0.56f, 0.38f, dark);
                break;
            }

            // ── Bar ──────────────────────────────────────────────────────────
            case BuildTool.Bar:
            {
                Color amber = new(0.90f, 0.70f, 0.22f); Color foam = new(0.97f, 0.96f, 0.94f);
                Color ltamber = new(0.96f, 0.84f, 0.50f); Color dark = new(0.18f, 0.12f, 0.08f);
                // Glass body
                P(0.28f, 0.16f, 0.70f, 0.64f, amber);
                // Foam
                P(0.24f, 0.60f, 0.74f, 0.74f, foam);
                // Handle: outer arc (right side)
                P(0.68f, 0.32f, 0.84f, 0.58f, amber); // outer handle
                P(0.72f, 0.38f, 0.80f, 0.52f, dark);  // handle cutout
                // Glass highlight
                P(0.30f, 0.20f, 0.40f, 0.62f, ltamber);
                // Tiny bubbles in glass
                P(0.44f, 0.28f, 0.48f, 0.32f, ltamber);
                P(0.54f, 0.40f, 0.58f, 0.44f, ltamber);
                P(0.46f, 0.50f, 0.50f, 0.54f, ltamber);
                break;
            }

            // ── Canteen ──────────────────────────────────────────────────────
            case BuildTool.Canteen:
            {
                Color plate = new(0.92f, 0.90f, 0.84f); Color rim = new(0.74f, 0.72f, 0.66f);
                Color w = new(0.94f, 0.92f, 0.86f); Color steam = new(0.90f, 0.90f, 0.90f, 0.85f);
                Color dark = new(0.14f, 0.12f, 0.10f);
                // Plate rim (behind)
                P(0.30f, 0.22f, 0.92f, 0.56f, rim);
                // Plate surface
                P(0.34f, 0.26f, 0.88f, 0.52f, plate);
                // Food dot on plate
                P(0.54f, 0.30f, 0.72f, 0.48f, new Color(0.80f, 0.48f, 0.22f, 0.9f));
                // Fork handle
                P(0.08f, 0.16f, 0.18f, 0.66f, w);
                // Fork tines (3)
                P(0.08f, 0.62f, 0.12f, 0.80f, w);
                P(0.12f, 0.62f, 0.16f, 0.80f, dark); // gap between tines
                P(0.16f, 0.62f, 0.20f, 0.80f, w);
                // Steam lines
                P(0.44f, 0.54f, 0.48f, 0.70f, steam);
                P(0.58f, 0.56f, 0.62f, 0.72f, steam);
                break;
            }

            // ── Gambling Hall ─────────────────────────────────────────────────
            case BuildTool.GamblingHall:
            {
                Color face = new(0.96f, 0.96f, 0.96f); Color pip = new(0.14f, 0.12f, 0.10f);
                Color side = new(0.70f, 0.64f, 0.16f); Color top = new(0.82f, 0.76f, 0.22f);
                // Die — right face (perspective)
                P(0.72f, 0.20f, 0.84f, 0.74f, side);
                // Die — top face (perspective)
                P(0.14f, 0.74f, 0.84f, 0.84f, top);
                // Die — front face
                P(0.14f, 0.18f, 0.74f, 0.76f, face);
                // 5-pip pattern on front face (standard "5" die face)
                P(0.20f, 0.64f, 0.32f, 0.72f, pip); // TL
                P(0.56f, 0.64f, 0.68f, 0.72f, pip); // TR
                P(0.38f, 0.44f, 0.50f, 0.52f, pip); // center
                P(0.20f, 0.24f, 0.32f, 0.32f, pip); // BL
                P(0.56f, 0.24f, 0.68f, 0.32f, pip); // BR
                break;
            }

            // ── City Park ─────────────────────────────────────────────────────
            case BuildTool.CityPark:
            {
                Color trunk = new(0.44f, 0.30f, 0.14f);
                Color g1 = new(0.16f, 0.40f, 0.14f); // bottom layer
                Color g2 = new(0.20f, 0.50f, 0.16f); // mid layer
                Color g3 = new(0.24f, 0.60f, 0.20f); // top layer
                // Trunk
                P(0.44f, 0.10f, 0.56f, 0.38f, trunk);
                // Canopy layers (pine tree silhouette)
                P(0.12f, 0.34f, 0.88f, 0.54f, g1);
                P(0.20f, 0.52f, 0.80f, 0.68f, g2);
                P(0.28f, 0.66f, 0.72f, 0.80f, g3);
                break;
            }

            // ── Personal House ────────────────────────────────────────────────
            case BuildTool.PersonalHouse:
            {
                Color wall = new(0.92f, 0.88f, 0.76f); Color gar = new(0.76f, 0.75f, 0.70f);
                Color gd = new(0.55f, 0.54f, 0.50f); Color door = new(0.28f, 0.18f, 0.10f);
                Color win = new(0.60f, 0.80f, 0.90f); Color roof = new(0.46f, 0.22f, 0.14f);
                // House wall
                P(0.10f, 0.12f, 0.90f, 0.54f, wall);
                // Garage area (left third)
                P(0.10f, 0.12f, 0.42f, 0.46f, gar);
                P(0.12f, 0.13f, 0.40f, 0.44f, gd); // garage door panel
                // Front door
                P(0.56f, 0.12f, 0.68f, 0.38f, door);
                // Window (right side of house)
                P(0.70f, 0.32f, 0.87f, 0.50f, win);
                // Pitched roof: two rotated panels forming Λ shape
                // Strip center at (34, 36) px. Roof sits at ~y=52-68 (above wall top).
                R(-14f, 20f, 5f, 42f, roof,  32f); // left slope
                R( 14f, 20f, 5f, 42f, roof, -32f); // right slope
                // Chimney above roof (right side)
                P(0.70f, 0.66f, 0.80f, 0.80f, new Color(0.58f, 0.26f, 0.16f));
                break;
            }

            // ── Car Market ────────────────────────────────────────────────────
            case BuildTool.CarMarket:
            {
                Color car  = new(0.22f, 0.38f, 0.70f);
                Color glas = new(0.70f, 0.88f, 0.96f);
                Color whl  = new(0.14f, 0.14f, 0.14f);
                Color flag = new(0.98f, 0.82f, 0.18f);
                Color pole = new(0.85f, 0.85f, 0.80f);
                // Car body
                P(0.06f, 0.22f, 0.94f, 0.50f, car);
                // Cabin roof
                P(0.20f, 0.48f, 0.80f, 0.68f, car);
                // Windshield + rear window
                P(0.22f, 0.50f, 0.44f, 0.66f, glas);
                P(0.56f, 0.50f, 0.78f, 0.66f, glas);
                // Front wheel + hub
                P(0.08f, 0.06f, 0.30f, 0.26f, whl);
                P(0.13f, 0.10f, 0.25f, 0.22f, new Color(0.38f, 0.38f, 0.42f));
                // Rear wheel + hub
                P(0.68f, 0.06f, 0.90f, 0.26f, whl);
                P(0.73f, 0.10f, 0.85f, 0.22f, new Color(0.38f, 0.38f, 0.42f));
                // Dealership flag pole + yellow flag
                P(0.86f, 0.66f, 0.90f, 0.90f, pole);
                P(0.62f, 0.76f, 0.88f, 0.92f, flag);
                break;
            }

            // ── Warehouse ─────────────────────────────────────────────────────
            case BuildTool.Warehouse:
            {
                Color wall   = new(0.76f, 0.64f, 0.44f);
                Color roof   = new(0.50f, 0.34f, 0.18f);
                Color stripe = new(0.62f, 0.50f, 0.32f);
                Color bay    = new(0.12f, 0.10f, 0.08f);
                Color win    = new(0.62f, 0.80f, 0.92f);
                // Building body
                P(0.08f, 0.14f, 0.92f, 0.58f, wall);
                // Barrel arch roof — three stepped layers faking a curve
                P(0.06f, 0.56f, 0.94f, 0.66f, roof);
                P(0.12f, 0.64f, 0.88f, 0.74f, roof);
                P(0.22f, 0.72f, 0.78f, 0.80f, roof);
                // Corrugated panel dividers
                P(0.18f, 0.16f, 0.22f, 0.56f, stripe);
                P(0.78f, 0.16f, 0.82f, 0.56f, stripe);
                // Large loading bay (double door)
                P(0.30f, 0.14f, 0.70f, 0.50f, bay);
                P(0.48f, 0.14f, 0.52f, 0.50f, stripe); // center divider
                // Small side window
                P(0.74f, 0.40f, 0.88f, 0.54f, win);
                break;
            }

            default:
            {
                Text lbl = CreateHeaderText("AccentLabel", accentStrip, font, abbrev, 17, TextAnchor.MiddleCenter, Color.white);
                RectTransform lr = lbl.GetComponent<RectTransform>();
                lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
                lr.sizeDelta = Vector2.zero; lr.anchoredPosition = Vector2.zero;
                break;
            }
        }
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

        Text arrowText = CreateBodyText("Arrow", headerRoot, font, expanded ? "v" : ">", 13, TextAnchor.MiddleLeft, new Color(0.65f, 0.72f, 0.82f));
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

            foreach (BuildItemUi item in cat.Items)
            {
                item.Root.gameObject.SetActive(false);
            }
            cat.HeaderRoot.gameObject.SetActive(anyUnlocked);
            if (!anyUnlocked) continue;

            cat.HeaderText.text = ru ? cat.LabelRu : cat.LabelEn;
            cat.ArrowText.text  = cat.IsExpanded ? "v" : ">";

            foreach (BuildItemUi item in cat.Items)
            {
                bool unlocked = IsBuildToolUnlocked(item.Tool);
                bool visible  = unlocked && cat.IsExpanded;
                if (visible)
                {
                    item.Root.gameObject.SetActive(true);
                }
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
            BuildTool.Parking          => locations.ContainsKey(LocationType.Parking),
            BuildTool.Warehouse        => locations.ContainsKey(LocationType.Warehouse),
            BuildTool.SingleRoad       => false,
            BuildTool.Road             => false,
            BuildTool.Stop             => false,
            BuildTool.Forest           => locations.ContainsKey(LocationType.Forest),
            BuildTool.Sawmill          => locations.ContainsKey(LocationType.Sawmill),
            BuildTool.Motel            => locations.ContainsKey(LocationType.Motel),
            BuildTool.FurnitureFactory => locations.ContainsKey(LocationType.FurnitureFactory),
            BuildTool.Bar              => locations.ContainsKey(LocationType.Bar),
            BuildTool.Canteen          => locations.ContainsKey(LocationType.Canteen),
            BuildTool.GasStation       => locations.ContainsKey(LocationType.GasStation),
            BuildTool.GamblingHall     => locations.ContainsKey(LocationType.GamblingHall),
            BuildTool.CityPark         => locations.ContainsKey(LocationType.CityPark),
            BuildTool.PersonalHouse    => false,
            BuildTool.CarMarket        => locations.ContainsKey(LocationType.CarMarket),
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
                BuildTool.Parking          => ru ? "Парковка — основной хаб грузовиков. Поставь её вручную, когда будешь готов к автопарку." : "Mode active: place the truck yard manually when you are ready to run a fleet.",
                BuildTool.Warehouse        => ru ? $"Режим активен: поставь склад 2x2 с подъездом. R — поворот ({rot})." : $"Mode active: place one 2x2 warehouse from its driveway cell. R rotates ({rot}).",
                BuildTool.SingleRoad       => ru ? $"Режим активен: левая кнопка строит обычную дорогу 1 клетку шириной. Shift — протянуть путь." : "Mode active: left click builds a 1-cell road. Hold Shift to drag a path.",
                BuildTool.Road             => ru ? $"Режим активен: левая кнопка строит дорогу 2 клетки шириной. R — поворот ({rot}), Shift — протянуть путь." : $"Mode active: left click builds a road 2 cells wide. R rotates ({rot}); hold Shift to drag a path.",
                BuildTool.Stop             => ru ? $"Режим активен: поставь автобусную остановку 2x1 с подъездом. R — поворот ({rot})." : $"Mode active: place one 2x1 bus stop from its driveway cell. R rotates ({rot}).",
                BuildTool.Forest           => ru ? $"Режим активен: поставь лагерь лесорубов 3x3 с подъездом. R — поворот ({rot})." : $"Mode active: place one 3x3 lumberjack camp from its driveway cell. R rotates ({rot}).",
                BuildTool.Sawmill          => ru ? $"Режим активен: поставь лесопилку 2x2 с подъездом. R — поворот ({rot})." : $"Mode active: place one 2x2 sawmill from its driveway cell. R rotates ({rot}).",
                BuildTool.Motel            => ru ? $"Режим активен: поставь мотель 2x2 с подъездом. R — поворот ({rot})." : $"Mode active: place one 2x2 motel from its driveway cell. R rotates ({rot}).",
                BuildTool.FurnitureFactory => ru ? $"Режим активен: поставь фабрику 3x2 с подъездом. R — поворот ({rot})." : $"Mode active: place one 3x2 furniture factory from its driveway cell. R rotates ({rot}).",
                BuildTool.Bar              => ru ? $"Режим активен: поставь бар с подъездом. R — поворот ({rot})." : $"Mode active: place bar from its driveway cell. R rotates ({rot}).",
                BuildTool.Canteen          => ru ? $"Режим активен: поставь столовую 3x2 с подъездом. R — поворот ({rot})." : $"Mode active: place one 3x2 canteen from its driveway cell. R rotates ({rot}).",
                BuildTool.GamblingHall     => ru ? $"Режим активен: поставь игровой зал 3x3 с подъездом. R — поворот ({rot})." : $"Mode active: place one 3x3 gambling hall from its driveway cell. R rotates ({rot}).",
                BuildTool.CityPark         => ru ? $"Режим активен: поставь городской парк 8x8 с входом. R — поворот ({rot})." : $"Mode active: place one 8x8 city park from its entrance cell. R rotates ({rot}).",
                BuildTool.PersonalHouse    => ru ? $"Режим активен: жилой дом 5x6, вход со стороны дороги. R — поворот ({rot})." : $"Mode active: 5x6 personal house, entrance faces the road. R rotates ({rot}).",
                BuildTool.CarMarket        => $"Mode active: place one 5x5 car market from its driveway cell. R rotates ({rot}).",
                BuildTool.GasStation       => ru ? $"\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u0437\u0430\u043f\u0440\u0430\u0432\u043a\u0443 2x2 \u0441 \u043f\u043e\u0434\u044a\u0435\u0437\u0434\u043e\u043c. R - \u043f\u043e\u0432\u043e\u0440\u043e\u0442 ({rot})." : $"Mode active: place one 2x2 gas station from its driveway cell. R rotates ({rot}).",
                _                          => string.Empty
            };
        }

        string alreadyBuilt = ru ? "Уже построено на этой карте." : "Already built on this map.";
        return tool switch
        {
            BuildTool.Parking          => locations.ContainsKey(LocationType.Parking) ? alreadyBuilt : (ru ? "Парковка: база для грузовиков. В User-режиме стартовых грузовиков нет, пока парковка не построена." : "Truck yard: the fleet base. User mode starts with no trucks until this exists."),
            BuildTool.Warehouse        => locations.ContainsKey(LocationType.Warehouse) ? alreadyBuilt : (ru ? "Склад 2x2: центральное хранение ресурсов и точка для производственных цепочек." : "2x2 warehouse: central resource storage for production chains."),
            BuildTool.SingleRoad       => ru ? "Обычная дорога занимает 1 клетку. Удобна для подъездов, узких участков и ручной достройки." : "Build a regular 1-cell road for driveways, narrow links, and manual fixes.",
            BuildTool.Road             => ru ? "Двухполосная дорога занимает 2 клетки шириной, с жёлтой центральной разметкой и автоматическим движением по полосам." : "Build a two-way road that occupies 2 cells of width, with a yellow center divider and automatic lane movement.",
            BuildTool.Stop             => ru ? "Автобусная остановка 2x1: локальная городская остановка для будущего транспорта рабочих." : "Place a 2x1 local bus stop for future worker public transport routes.",
            BuildTool.Forest           => locations.ContainsKey(LocationType.Forest)           ? alreadyBuilt : (ru ? "Лагерь лесорубов 3x3: рабочие выходят рубить деревья, таскают брёвна и высаживают саженцы." : "3x3 lumberjack camp: workers chop trees, carry logs, and replant saplings."),
            BuildTool.Sawmill          => locations.ContainsKey(LocationType.Sawmill)          ? alreadyBuilt : (ru ? "Здание 2x2: превращает брёвна в доски." : "Place a 2x2 production building that turns logs into boards."),
            BuildTool.Motel            => locations.ContainsKey(LocationType.Motel)            ? alreadyBuilt : (ru ? "Мотель 2x2: водители нанимаются и ждут здесь." : "Place a 2x2 driver hub. Drivers can idle and be hired after it exists."),
            BuildTool.FurnitureFactory => locations.ContainsKey(LocationType.FurnitureFactory) ? alreadyBuilt : (ru ? "Фабрика 3x2: 1 Доска + 1 Ткань = 1 Мебель." : "Place a 3x2 factory that turns 1 Board + 1 Textile into 1 Furniture."),
            BuildTool.Bar              => locations.ContainsKey(LocationType.Bar)              ? alreadyBuilt : (ru ? "Соцточка — водители собираются здесь отдыхать." : "Social hub — idle drivers gather here to rest."),
            BuildTool.Canteen          => locations.ContainsKey(LocationType.Canteen)          ? alreadyBuilt : (ru ? "Столовая: водители и рабочие платят $10 за обед." : "Service building: visiting drivers/workers pay $10 for a quick meal."),
            BuildTool.GamblingHall     => locations.ContainsKey(LocationType.GamblingHall)     ? alreadyBuilt : (ru ? "Досуг: бесплатный вход — рабочие расслабляются здесь." : "Leisure: free entry — workers unwind here."),
            BuildTool.CityPark         => locations.ContainsKey(LocationType.CityPark)         ? alreadyBuilt : (ru ? "Парк 8x8: рабочие гуляют и сидят на лавочках. Бесплатно, повышает настроение." : "8x8 park: workers stroll and sit on benches. Free entry, boosts leisure need."),
            BuildTool.PersonalHouse    => ru ? "Жилой дом 5x6 — американский пригородный дом в одной из 5 случайных вариаций." : "5x6 suburban house — one of 5 random American home styles. Decorative for now.",
            BuildTool.CarMarket        => locations.ContainsKey(LocationType.CarMarket) ? alreadyBuilt : "5x5 car market: workers with $100 can buy personal cars here.",
            BuildTool.GasStation       => locations.ContainsKey(LocationType.GasStation) ? alreadyBuilt : (ru ? "\u0417\u0430\u043f\u0440\u0430\u0432\u043a\u0430 2x2: \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438 \u0435\u0434\u0443\u0442 \u0441\u044e\u0434\u0430 \u0437\u0430 \u0442\u043e\u043f\u043b\u0438\u0432\u043e\u043c, \u043a\u043e\u0433\u0434\u0430 \u0440\u0435\u0439\u0441\u044b \u0441\u0442\u0430\u043d\u043e\u0432\u044f\u0442\u0441\u044f \u0441\u043b\u0438\u0448\u043a\u043e\u043c \u0434\u043b\u0438\u043d\u043d\u044b\u043c\u0438." : "2x2 fuel service: trucks refuel here when routes get too long."),
            _                          => string.Empty
        };
    }

}
