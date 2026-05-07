using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private BuildCatalogData cachedBuildCatalogData;
    private Dictionary<BuildTool, BuildCatalogItemData> cachedBuildCatalogItems;

    private void InitUnlockedBuildTools()
    {
        unlockedBuildTools = new HashSet<BuildTool>();
        if (selectedGameStartMode == GameStartMode.Tutorial)
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
        unlockedBuildTools?.Add(BuildTool.Docks);
    }

    private bool IsBuildToolUnlocked(BuildTool tool)
    {
        if (tool == BuildTool.Docks)
        {
            return true;
        }

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

        FleetCanvasUiFactory.ScrollPanelRefs buildScroll = CreateVerticalScrollList(
            "BuildScrollView",
            windowRoot.transform,
            "BuildCardList",
            10f,
            flexibleHeight: 1f);
        RectTransform cardList = buildScroll.Content;

        buildScreenUi.Categories = CreateBuildCategoriesFromCatalog(cardList, font) ?? new BuildCategoryUi[]
        {
            CreateBuildCategory(cardList, font, "Roads & Transport", "Дороги и транспорт", true,
                (BuildTool.SingleRoad,       "1R", "One-Lane Road",     new Color(0.22f, 0.34f, 0.46f)),
                (BuildTool.Road,             "2W", "Two-Way Road",      new Color(0.27f, 0.42f, 0.60f)),
                (BuildTool.Stop,             "ST", "Bus Stop",          new Color(0.72f, 0.28f, 0.24f)),
                (BuildTool.Parking,          "PK", "Parking",           new Color(0.28f, 0.30f, 0.38f)),
                (BuildTool.GasStation,       "GS", "Gas Station",       new Color(0.84f, 0.68f, 0.26f))),
            CreateBuildCategory(cardList, font, "Logistics & Trade", "Логистика и торговля", false,
                (BuildTool.Warehouse,        "WH", "Warehouse",         new Color(0.70f, 0.52f, 0.30f)),
                (BuildTool.Docks,            "DK", "Docks",             new Color(0.24f, 0.44f, 0.54f))),
            CreateBuildCategory(cardList, font, "Production", "Производство", false,
                (BuildTool.Forest,           "LC", "Lumberjack Camp",    new Color(0.28f, 0.45f, 0.18f)),
                (BuildTool.Sawmill,          "SW", "Sawmill",           new Color(0.58f, 0.36f, 0.16f)),
                (BuildTool.FurnitureFactory, "FF", "Furniture Factory", new Color(0.46f, 0.26f, 0.52f))),
            CreateBuildCategory(cardList, font, "Housing & Civic", "Жильё и город", false,
                (BuildTool.Motel,            "MT", "Motel",          new Color(0.24f, 0.48f, 0.36f)),
                (BuildTool.PersonalHouse,    "PH", "Personal House", new Color(0.55f, 0.42f, 0.30f)),
                (BuildTool.Kindergarten,     "KG", "Kindergarten",   new Color(0.46f, 0.62f, 0.36f)),
                (BuildTool.LaborExchange,    "LE", "Labor Exchange", new Color(0.34f, 0.47f, 0.56f))),
            CreateBuildCategory(cardList, font, "Services & Leisure", "Сервисы и досуг", false,
                (BuildTool.Canteen,          "CT", "Canteen",       new Color(0.20f, 0.42f, 0.50f)),
                (BuildTool.Kiosk,            "KS", "Kiosk",         new Color(0.86f, 0.58f, 0.24f)),
                (BuildTool.CoffeeShop,       "CF", "Coffee Shop",   new Color(0.48f, 0.32f, 0.22f)),
                (BuildTool.Bar,              "BR", "Bar",           new Color(0.52f, 0.20f, 0.20f)),
                (BuildTool.GamblingHall,     "GH", "Gambling Hall", new Color(0.52f, 0.38f, 0.08f)),
                (BuildTool.CityPark,         "CP", "City Park",     new Color(0.22f, 0.48f, 0.22f)),
                (BuildTool.CarMarket,        "CM", "Car Market",    new Color(0.64f, 0.52f, 0.38f))),
        };

        AddOverlayCloseButton(windowRect, font);
        buildScreenUi.CanvasRoot.SetActive(false);
        UpdateBuildScreenUi();
    }

    private BuildItemUi CreateBuildItemCard(RectTransform parent, Font font, BuildTool tool, string abbrev, string title, Color accentColor)
    {
        BuildItemUi item = new BuildItemUi { Tool = tool, DefaultAccentColor = accentColor };

        RectTransform cardRoot = CreateHorizontalLayoutPanel(
            "BuildCard_" + tool,
            parent,
            new Color(0.16f, 0.21f, 0.28f, 1f),
            new RectOffset(),
            0f,
            preferredHeight: 72f,
            flexibleHeight: 0f,
            childForceExpandHeight: true,
            addOutline: false);
        Image cardBg = cardRoot.GetComponent<Image>();
        item.Root   = cardRoot;
        item.CardBg = cardBg;

        // Colored accent strip (left)
        RectTransform accentStrip = CreateUiObject("Accent", cardRoot).GetComponent<RectTransform>();
        Image accentBg = accentStrip.gameObject.AddComponent<Image>();
        accentBg.color = accentColor;
        accentStrip.gameObject.AddComponent<LayoutElement>().preferredWidth = 68f;
        item.AccentBg = accentBg;

        CreateBuildAccentVisual(accentStrip, font, tool, abbrev);

        // Card body (right)
        RectTransform body = CreateVerticalStack(
            "Body",
            cardRoot,
            new RectOffset(14, 10, 10, 8),
            4,
            flexibleWidth: 1f);

        // Title row: name + status badge
        RectTransform titleRow = CreateLayoutRow("TitleRow", body, 22f, 0f);

        Text titleText = CreateHeaderText("Title", titleRow, font, title, 15, TextAnchor.MiddleLeft, Color.white);
        titleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        item.TitleText = titleText;

        FleetCanvasUiFactory.BadgeRefs statusBadge = CreateBadge(
            "StatusBadge",
            titleRow,
            font,
            "Available",
            11,
            new Color(0.22f, 0.28f, 0.38f, 0.8f),
            Color.white,
            72f,
            20f);
        item.StatusBg = statusBadge.Background;
        item.StatusText = statusBadge.Label;

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
            if (!IsBuildToolUnlocked(capturedTool) || IsBuildToolTemporarilyUnavailable(capturedTool)) return;
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

            // Kindergarten
            case BuildTool.Kindergarten:
            {
                Color wall = new(0.88f, 0.82f, 0.54f);
                Color roof = new(0.30f, 0.52f, 0.46f);
                Color mat = new(0.34f, 0.62f, 0.36f);
                Color slide = new(0.28f, 0.58f, 0.90f);
                Color trim = new(0.96f, 0.58f, 0.30f);
                P(0.10f, 0.18f, 0.90f, 0.56f, wall);
                P(0.06f, 0.54f, 0.94f, 0.66f, roof);
                P(0.42f, 0.18f, 0.58f, 0.46f, new Color(0.20f, 0.30f, 0.34f));
                P(0.16f, 0.36f, 0.34f, 0.50f, new Color(0.58f, 0.82f, 0.94f));
                P(0.66f, 0.36f, 0.84f, 0.50f, new Color(0.58f, 0.82f, 0.94f));
                P(0.12f, 0.08f, 0.88f, 0.16f, mat);
                P(0.18f, 0.12f, 0.34f, 0.28f, trim);
                R(17f, -16f, 7f, 28f, slide, -28f);
                P(0.62f, 0.10f, 0.76f, 0.24f, new Color(0.88f, 0.32f, 0.32f));
                break;
            }

            // Car Market
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

            // ── Gas Station ───────────────────────────────────────────────────
            case BuildTool.LaborExchange:
            {
                Color wall = new(0.34f, 0.47f, 0.56f);
                Color roof = new(0.18f, 0.24f, 0.28f);
                Color paper = new(0.92f, 0.88f, 0.74f);
                Color dark = new(0.16f, 0.18f, 0.18f);
                P(0.10f, 0.18f, 0.90f, 0.58f, wall);
                P(0.06f, 0.56f, 0.94f, 0.68f, roof);
                P(0.38f, 0.18f, 0.62f, 0.48f, dark);
                P(0.16f, 0.38f, 0.34f, 0.52f, new Color(0.58f, 0.82f, 0.92f));
                P(0.66f, 0.38f, 0.84f, 0.52f, new Color(0.58f, 0.82f, 0.92f));
                P(0.22f, 0.70f, 0.78f, 0.84f, dark);
                P(0.30f, 0.74f, 0.46f, 0.78f, paper);
                P(0.54f, 0.74f, 0.70f, 0.78f, paper);
                P(0.08f, 0.08f, 0.92f, 0.14f, new Color(0.74f, 0.70f, 0.58f));
                break;
            }

            case BuildTool.GasStation:
            {
                Color canopy = new(0.84f, 0.68f, 0.26f); // yellow canopy
                Color pole   = new(0.55f, 0.56f, 0.58f); // steel poles
                Color pump   = new(0.22f, 0.24f, 0.28f); // dark pump body
                Color screen = new(0.50f, 0.82f, 0.96f); // blue display
                Color nozzle = new(0.18f, 0.18f, 0.20f); // nozzle/hose
                Color asph   = new(0.20f, 0.20f, 0.22f); // asphalt ground
                // Ground strip
                P(0.06f, 0.08f, 0.94f, 0.16f, asph);
                // Canopy support poles
                P(0.13f, 0.14f, 0.19f, 0.72f, pole);
                P(0.81f, 0.14f, 0.87f, 0.72f, pole);
                // Canopy roof
                P(0.06f, 0.70f, 0.94f, 0.84f, canopy);
                // Fuel pump body
                P(0.38f, 0.16f, 0.62f, 0.64f, pump);
                // Pump screen (blue display)
                P(0.42f, 0.46f, 0.58f, 0.60f, screen);
                // Pump nozzle hook (horizontal arm + vertical drop)
                P(0.24f, 0.36f, 0.38f, 0.41f, nozzle);
                P(0.24f, 0.22f, 0.30f, 0.38f, nozzle);
                // Red fuel indicator dot on pump
                P(0.42f, 0.30f, 0.52f, 0.40f, new Color(0.90f, 0.22f, 0.18f));
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

            case BuildTool.Docks:
            {
                Color water = new(0.10f, 0.36f, 0.56f);
                Color waterHi = new(0.32f, 0.72f, 0.88f, 0.9f);
                Color pier = new(0.48f, 0.30f, 0.14f);
                Color pierDark = new(0.26f, 0.16f, 0.08f);
                Color warehouse = new(0.60f, 0.38f, 0.18f);
                Color roof = new(0.18f, 0.22f, 0.24f);
                Color crane = new(0.78f, 0.62f, 0.24f);
                P(0.05f, 0.08f, 0.95f, 0.46f, water);
                P(0.10f, 0.34f, 0.42f, 0.39f, waterHi);
                P(0.56f, 0.18f, 0.90f, 0.23f, waterHi);
                P(0.10f, 0.44f, 0.90f, 0.56f, pier);
                P(0.18f, 0.12f, 0.26f, 0.56f, pierDark);
                P(0.44f, 0.12f, 0.52f, 0.56f, pierDark);
                P(0.66f, 0.12f, 0.74f, 0.56f, pierDark);
                P(0.12f, 0.56f, 0.56f, 0.78f, warehouse);
                P(0.08f, 0.76f, 0.60f, 0.88f, roof);
                P(0.66f, 0.52f, 0.72f, 0.86f, crane);
                R(18f, 21f, 6f, 30f, crane, -46f);
                P(0.80f, 0.36f, 0.86f, 0.52f, crane);
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

    private BuildCategoryUi[] CreateBuildCategoriesFromCatalog(RectTransform parent, Font font)
    {
        BuildCatalogData catalog = GetBuildCatalogData();
        if (catalog?.categories == null || catalog.categories.Length == 0)
        {
            return null;
        }

        List<BuildCategoryUi> categories = new();
        foreach (BuildCatalogCategoryData categoryData in catalog.categories)
        {
            if (categoryData?.items == null || categoryData.items.Length == 0)
            {
                continue;
            }

            List<(BuildTool tool, string abbrev, string title, Color color)> toolDefs = new();
            foreach (BuildCatalogItemData itemData in categoryData.items)
            {
                if (!TryParseBuildCatalogTool(itemData, out BuildTool tool))
                {
                    continue;
                }

                toolDefs.Add((
                    tool,
                    itemData.GetAbbrev(),
                    itemData.GetTitle(false),
                    itemData.GetColor(new Color(0.30f, 0.36f, 0.44f))));
            }

            if (toolDefs.Count == 0)
            {
                continue;
            }

            categories.Add(CreateBuildCategory(
                parent,
                font,
                categoryData.GetLabel(false),
                categoryData.GetLabel(true),
                categoryData.expanded,
                toolDefs.ToArray()));
        }

        return categories.Count > 0 ? categories.ToArray() : null;
    }

    private BuildCatalogData GetBuildCatalogData()
    {
        if (cachedBuildCatalogData != null)
        {
            return cachedBuildCatalogData;
        }

        cachedBuildCatalogData = BuildCatalog.Load();
        cachedBuildCatalogItems = new Dictionary<BuildTool, BuildCatalogItemData>();
        if (cachedBuildCatalogData?.categories == null)
        {
            return cachedBuildCatalogData;
        }

        foreach (BuildCatalogCategoryData category in cachedBuildCatalogData.categories)
        {
            if (category?.items == null)
            {
                continue;
            }

            foreach (BuildCatalogItemData item in category.items)
            {
                if (TryParseBuildCatalogTool(item, out BuildTool tool))
                {
                    cachedBuildCatalogItems[tool] = item;
                }
            }
        }

        return cachedBuildCatalogData;
    }


}
