using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private static bool TryParseBuildCatalogTool(BuildCatalogItemData item, out BuildTool tool)
    {
        tool = BuildTool.None;
        return item != null
               && !string.IsNullOrWhiteSpace(item.tool)
               && System.Enum.TryParse(item.tool, out tool)
               && tool != BuildTool.None;
    }

    private bool TryGetBuildCatalogItem(BuildTool tool, out BuildCatalogItemData item)
    {
        GetBuildCatalogData();
        item = null;
        return cachedBuildCatalogItems != null && cachedBuildCatalogItems.TryGetValue(tool, out item);
    }

    private static bool IsBuildToolTemporarilyUnavailable(BuildTool tool)
    {
        return tool == BuildTool.Road;
    }

    private string GetBuildToolUnavailableStatus(bool ru)
    {
        return ru ? "\u041d\u0430 \u0440\u0435\u043a\u043e\u043d\u0441\u0442\u0440\u0443\u043a\u0446\u0438\u0438" : "Rework";
    }

    private string GetBuildToolUnavailableDescription(BuildTool tool, bool ru)
    {
        return tool == BuildTool.Road
            ? ru
                ? "\u0412\u0440\u0435\u043c\u0435\u043d\u043d\u043e \u043d\u0430 \u0440\u0435\u043a\u043e\u043d\u0441\u0442\u0440\u0443\u043a\u0446\u0438\u0438. \u041f\u043e\u043a\u0430 \u0441\u0442\u0440\u043e\u0439 \u043e\u0431\u044b\u0447\u043d\u044b\u0435 \u0434\u043e\u0440\u043e\u0433\u0438 1 \u043a\u043b\u0435\u0442\u043a\u0443 \u0448\u0438\u0440\u0438\u043d\u043e\u0439."
                : "Temporarily under rework. Use regular 1-cell roads for now."
            : string.Empty;
    }

    private string GetBuildCatalogTitle(BuildTool tool, bool ru, string fallback)
    {
        return TryGetBuildCatalogItem(tool, out BuildCatalogItemData item)
            ? item.GetTitle(ru)
            : fallback;
    }

    private bool TryGetBuildCatalogDescription(BuildTool tool, bool isActive, out string description)
    {
        description = string.Empty;
        if (!TryGetBuildCatalogItem(tool, out BuildCatalogItemData item))
        {
            return false;
        }

        bool ru = IsRussianLanguage();
        string text = isActive
            ? item.GetActiveDescription(ru)
            : GetBuildToolAlreadyBuilt(tool)
                ? item.GetAlreadyBuiltDescription(ru)
                : item.GetDescription(ru);

        if (string.IsNullOrWhiteSpace(text) && !isActive && GetBuildToolAlreadyBuilt(tool))
        {
            text = ru ? "Уже построено на этой карте." : "Already built on this map.";
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        description = text.Replace("{rot}", GetBuildRotationLabel());
        return true;
    }

    private BuildCategoryUi CreateBuildCategory(RectTransform parent, Font font, string labelEn, string labelRu, bool expanded,
        params (BuildTool tool, string abbrev, string title, Color color)[] toolDefs)
    {
        BuildCategoryUi cat = new BuildCategoryUi { LabelEn = labelEn, LabelRu = labelRu, IsExpanded = expanded };

        RectTransform headerRoot = CreateHorizontalLayoutPanel(
            "CatHeader_" + labelEn,
            parent,
            new Color(0.13f, 0.17f, 0.23f, 1f),
            new RectOffset(10, 10, 0, 0),
            6f,
            preferredHeight: 30f,
            flexibleHeight: 0f,
            childForceExpandHeight: true,
            addOutline: false);
        Image headerBg = headerRoot.GetComponent<Image>();
        cat.HeaderRoot = headerRoot;

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
                bool isUnavailable = IsBuildToolTemporarilyUnavailable(item.Tool);
                if (isUnavailable && isActive)
                {
                    activeBuildTool = BuildTool.None;
                    isActive = false;
                }

                item.Button.interactable = !isUnavailable;
                item.CardBg.color = isUnavailable
                    ? new Color(0.11f, 0.13f, 0.16f, 0.82f)
                    : isActive
                        ? new Color(0.20f, 0.27f, 0.37f, 1f)
                        : new Color(0.16f, 0.21f, 0.28f, 1f);
                item.AccentBg.color = isUnavailable
                    ? new Color(0.20f, 0.22f, 0.25f, 0.88f)
                    : isActive
                        ? FleetAccentColor
                        : item.DefaultAccentColor;
                item.TitleText.color = isUnavailable ? new Color(0.62f, 0.66f, 0.72f, 1f) : Color.white;
                item.TitleText.text = GetBuildCatalogTitle(item.Tool, ru, item.TitleText.text);
                item.DescText.color = isUnavailable ? new Color(0.52f, 0.56f, 0.62f, 1f) : FleetSecondaryTextColor;
                item.DescText.text  = isUnavailable
                    ? GetBuildToolUnavailableDescription(item.Tool, ru)
                    : GetBuildDescription(item.Tool, isActive);

                if (isUnavailable)
                {
                    item.StatusBg.color  = new Color(0.24f, 0.24f, 0.26f, 0.85f);
                    item.StatusText.text = GetBuildToolUnavailableStatus(ru);
                }
                else if (isActive)
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
            BuildTool.Docks            => locations.ContainsKey(LocationType.Docks),
            BuildTool.SingleRoad       => false,
            BuildTool.Road             => false,
            BuildTool.Stop             => false,
            BuildTool.Forest           => locations.ContainsKey(LocationType.Forest),
            BuildTool.Sawmill          => locations.ContainsKey(LocationType.Sawmill),
            BuildTool.Motel            => locations.ContainsKey(LocationType.Motel),
            BuildTool.FurnitureFactory => locations.ContainsKey(LocationType.FurnitureFactory),
            BuildTool.Bar              => false,
            BuildTool.Canteen          => false,
            BuildTool.Kiosk            => false,
            BuildTool.GasStation       => false,
            BuildTool.GamblingHall     => false,
            BuildTool.CityPark         => false,
            BuildTool.PersonalHouse    => false,
            BuildTool.Kindergarten     => false,
            BuildTool.CarMarket        => locations.ContainsKey(LocationType.CarMarket),
            BuildTool.LaborExchange    => locations.ContainsKey(LocationType.LaborExchange),
            BuildTool.CityHall         => locations.ContainsKey(LocationType.CityHall),
            _                          => false
        };
    }

    private string GetBuildDescription(BuildTool tool, bool isActive)
    {
        if (TryGetBuildCatalogDescription(tool, isActive, out string catalogDescription))
        {
            return catalogDescription;
        }

        bool ru = IsRussianLanguage();
        string rot = GetBuildRotationLabel();

        if (isActive)
        {
            return tool switch
            {
                BuildTool.Parking          => ru ? "Парковка — основной хаб грузовиков. Поставь её вручную, когда будешь готов к автопарку." : "Mode active: place the truck yard manually when you are ready to run a fleet.",
                BuildTool.Warehouse        => ru ? $"Режим активен: поставь склад 2x2 с подъездом. R — поворот ({rot})." : $"Mode active: place one 2x2 warehouse from its driveway cell. R rotates ({rot}).",
                BuildTool.SingleRoad       => ru ? $"Режим активен: левая кнопка строит обычную дорогу 1 клетку шириной. Shift — протянуть путь." : "Mode active: left click builds a 1-cell road. Hold Shift to drag a path.",
                BuildTool.Road             => ru ? $"Режим активен: ЛКМ выбирает начало участка, второй ЛКМ строит двухполосную дорогу. Shift — строго по одной оси. R — поворот ({rot})." : $"Mode active: left click selects a segment start, second left click builds the two-way road. Shift constrains to one axis. R rotates ({rot}).",
                BuildTool.Stop             => ru ? $"Режим активен: поставь автобусную остановку 2x1 с подъездом. R — поворот ({rot})." : $"Mode active: place one 2x1 bus stop from its driveway cell. R rotates ({rot}).",
                BuildTool.Forest           => ru ? $"Режим активен: поставь лагерь лесорубов 3x3 с подъездом. R — поворот ({rot})." : $"Mode active: place one 3x3 lumberjack camp from its driveway cell. R rotates ({rot}).",
                BuildTool.Sawmill          => ru ? $"Режим активен: поставь лесопилку 2x2 с подъездом. R — поворот ({rot})." : $"Mode active: place one 2x2 sawmill from its driveway cell. R rotates ({rot}).",
                BuildTool.Motel            => ru ? $"Режим активен: поставь мотель 2x2 с подъездом. R — поворот ({rot})." : $"Mode active: place one 2x2 motel from its driveway cell. R rotates ({rot}).",
                BuildTool.FurnitureFactory => ru ? $"Режим активен: поставь фабрику 3x2 с подъездом. R — поворот ({rot})." : $"Mode active: place one 3x2 furniture factory from its driveway cell. R rotates ({rot}).",
                BuildTool.Bar              => ru ? $"Режим активен: поставь бар с подъездом. Можно строить несколько. R — поворот ({rot})." : $"Mode active: place a bar from its driveway cell. Multiple bars are allowed. R rotates ({rot}).",
                BuildTool.Canteen          => ru ? $"Режим активен: поставь столовую 3x2 с подъездом. Можно строить несколько. R — поворот ({rot})." : $"Mode active: place a 3x2 canteen from its driveway cell. Multiple canteens are allowed. R rotates ({rot}).",
                BuildTool.Kiosk            => ru ? $"Режим активен: поставь киоск 2x1 без подъезда к дороге. Snack и Coffee покупаются здесь за $4. R — поворот ({rot})." : $"Mode active: place a 2x1 walk-up kiosk. Snacks and Coffee cost $4 here. No road driveway required. R rotates ({rot}).",
                BuildTool.GamblingHall     => ru ? $"Режим активен: поставь игровой зал 3x3 с подъездом. Можно строить несколько. R — поворот ({rot})." : $"Mode active: place a 3x3 gambling hall from its driveway cell. Multiple halls are allowed. R rotates ({rot}).",
                BuildTool.CityPark         => ru ? $"Режим активен: поставь городской парк 8x8 без подъезда к дороге. R — поворот ({rot})." : $"Mode active: place an 8x8 city park with no road driveway. R rotates ({rot}).",
                BuildTool.PersonalHouse    => ru ? $"Режим активен: жилой дом 5x6, вход со стороны дороги. R — поворот ({rot})." : $"Mode active: 5x6 personal house, entrance faces the road. R rotates ({rot}).",
                BuildTool.Kindergarten     => ru ? $"\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u0434\u0435\u0442\u0441\u043a\u0438\u0439 \u0441\u0430\u0434 4x3 \u0441 \u043f\u043e\u0434\u044a\u0435\u0437\u0434\u043e\u043c. \u041c\u043e\u0436\u043d\u043e \u0441\u0442\u0440\u043e\u0438\u0442\u044c \u043d\u0435\u0441\u043a\u043e\u043b\u044c\u043a\u043e. R - \u043f\u043e\u0432\u043e\u0440\u043e\u0442 ({rot})." : $"Mode active: place a 4x3 kindergarten from its driveway cell. Multiple kindergartens are allowed. R rotates ({rot}).",
                BuildTool.CarMarket        => $"Mode active: place one 5x5 car market from its driveway cell. R rotates ({rot}).",
                BuildTool.LaborExchange    => ru ? $"\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u0411\u0438\u0440\u0436\u0443 \u0442\u0440\u0443\u0434\u0430 3x2 \u0441 \u043f\u043e\u0434\u044a\u0435\u0437\u0434\u043e\u043c. R - \u043f\u043e\u0432\u043e\u0440\u043e\u0442 ({rot})." : $"Mode active: place one 3x2 labor exchange from its driveway cell. R rotates ({rot}).",
                BuildTool.CityHall         => ru ? $"\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u0440\u0430\u0442\u0443\u0448\u0443 4x3 \u0441 \u043f\u043e\u0434\u044a\u0435\u0437\u0434\u043e\u043c. R - \u043f\u043e\u0432\u043e\u0440\u043e\u0442 ({rot})." : $"Mode active: place one 4x3 city hall from its driveway cell. R rotates ({rot}).",
                BuildTool.GasStation       => ru ? $"\u0420\u0435\u0436\u0438\u043c \u0430\u043a\u0442\u0438\u0432\u0435\u043d: \u043f\u043e\u0441\u0442\u0430\u0432\u044c \u0437\u0430\u043f\u0440\u0430\u0432\u043a\u0443 2x2 \u0441 \u043f\u043e\u0434\u044a\u0435\u0437\u0434\u043e\u043c. R - \u043f\u043e\u0432\u043e\u0440\u043e\u0442 ({rot})." : $"Mode active: place one 2x2 gas station from its driveway cell. R rotates ({rot}).",
                _                          => string.Empty
            };
        }

        string alreadyBuilt = ru ? "Уже построено на этой карте." : "Already built on this map.";
        return tool switch
        {
            BuildTool.Parking          => locations.ContainsKey(LocationType.Parking) ? alreadyBuilt : (ru ? "Парковка: база для грузовиков. В новой игре стартовых грузовиков нет, пока парковка не построена." : "Truck yard: the fleet base. New games start with no trucks until this exists."),
            BuildTool.Warehouse        => locations.ContainsKey(LocationType.Warehouse) ? alreadyBuilt : (ru ? "Склад 2x2: центральное хранение ресурсов и точка для производственных цепочек." : "2x2 warehouse: central resource storage for production chains."),
            BuildTool.SingleRoad       => ru ? "Обычная дорога занимает 1 клетку. Удобна для подъездов, узких участков и ручной достройки." : "Build a regular 1-cell road for driveways, narrow links, and manual fixes.",
            BuildTool.Road             => ru ? "Двухполосная дорога строится участками: выбери начало, затем конец. Она занимает 2 клетки шириной, с центральной разметкой и движением по полосам." : "Build two-way road segments: choose a start, then an end. It occupies 2 cells of width with center markings and lane movement.",
            BuildTool.Stop             => ru ? "Автобусная остановка 2x1: локальная городская остановка для будущего транспорта рабочих." : "Place a 2x1 local bus stop for future worker public transport routes.",
            BuildTool.Forest           => locations.ContainsKey(LocationType.Forest)           ? alreadyBuilt : (ru ? "Лагерь лесорубов 3x3: рабочие выходят рубить деревья, таскают брёвна и высаживают саженцы." : "3x3 lumberjack camp: workers chop trees, carry logs, and replant saplings."),
            BuildTool.Sawmill          => locations.ContainsKey(LocationType.Sawmill)          ? alreadyBuilt : (ru ? "Здание 2x2: превращает брёвна в доски." : "Place a 2x2 production building that turns logs into boards."),
            BuildTool.Motel            => locations.ContainsKey(LocationType.Motel)            ? alreadyBuilt : (ru ? "Мотель 2x2: рабочие заселяются и ждут здесь." : "Place a 2x2 worker hub. New arrivals check in and idle here."),
            BuildTool.FurnitureFactory => locations.ContainsKey(LocationType.FurnitureFactory) ? alreadyBuilt : (ru ? "Фабрика 3x2: 1 Доска + 1 Ткань = 1 Мебель." : "Place a 3x2 factory that turns 1 Board + 1 Textile into 1 Furniture."),
            BuildTool.Bar              => ru ? "Соцточка — водители собираются здесь отдыхать. Можно строить несколько." : "Social hub — idle drivers gather here to rest. Multiple bars are allowed.",
            BuildTool.Canteen          => ru ? "Столовая: водители и рабочие платят $8 за обед. Можно строить несколько." : "Service building: visiting drivers/workers pay $8 for a quick meal. Multiple canteens are allowed.",
            BuildTool.Kiosk            => ru ? "Киоск: рабочие подходят к стойке и покупают Snack или Coffee за $4 в инвентарь. Подъезд к дороге не нужен." : "Walk-up kiosk: workers buy a $4 Snack or Coffee for their inventory. No road driveway required.",
            BuildTool.GamblingHall     => ru ? "Досуг: бесплатный вход — рабочие расслабляются здесь. Можно строить несколько." : "Leisure: free entry — workers unwind here. Multiple halls are allowed.",
            BuildTool.CityPark         => ru ? "Парк 8x8: рабочие гуляют и сидят на лавочках. Подъезд к дороге не нужен." : "8x8 park: workers stroll and sit on benches. No road driveway required.",
            BuildTool.PersonalHouse    => ru ? "Жилой дом 5x6 — американский пригородный дом в одной из 5 случайных вариаций." : "5x6 suburban house — one of 5 random American home styles. Decorative for now.",
            BuildTool.Kindergarten     => ru ? "\u0414\u0435\u0442\u0441\u043a\u0438\u0439 \u0441\u0430\u0434 4x3: \u0441\u0435\u0440\u0432\u0438\u0441\u043d\u044b\u0435 \u0440\u0430\u0431\u043e\u0447\u0438\u0435 \u0441\u043e\u0437\u0434\u0430\u044e\u0442 \u043c\u0435\u0441\u0442\u0430 \u0434\u043b\u044f \u0434\u0435\u0442\u0435\u0439 \u0438 \u0441\u043d\u0438\u0436\u0430\u044e\u0442 \u0441\u0442\u0440\u0435\u0441\u0441 \u0441\u0435\u043c\u0435\u0439." : "4x3 kindergarten: service workers create child-care capacity and lower family stress.",
            BuildTool.CarMarket        => locations.ContainsKey(LocationType.CarMarket) ? alreadyBuilt : "5x5 car market: workers with $100 can buy personal cars here.",
            BuildTool.LaborExchange    => locations.ContainsKey(LocationType.LaborExchange) ? alreadyBuilt : (ru ? "\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430 3x2: \u043a\u043b\u0435\u0440\u043a \u0441 \u0432\u044b\u0441\u0448\u0438\u043c \u043e\u0431\u0440\u0430\u0437\u043e\u0432\u0430\u043d\u0438\u0435\u043c \u043f\u0443\u0431\u043b\u0438\u043a\u0443\u0435\u0442 \u0432\u0430\u043a\u0430\u043d\u0441\u0438\u0438." : "3x2 labor exchange: a higher-educated clerk publishes vacancies for workers."),
            BuildTool.CityHall         => locations.ContainsKey(LocationType.CityHall) ? alreadyBuilt : (ru ? "Ратуша 4x3: горожане подают обращения, а принятые обращения становятся городскими целями на 24 часа." : "4x3 city hall: citizens file requests that can become 24h city goals."),
            BuildTool.GasStation       => ru ? "\u0417\u0430\u043f\u0440\u0430\u0432\u043a\u0430 2x2: \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0438 \u0435\u0434\u0443\u0442 \u0441\u044e\u0434\u0430 \u0437\u0430 \u0442\u043e\u043f\u043b\u0438\u0432\u043e\u043c. \u041c\u043e\u0436\u043d\u043e \u0441\u0442\u0440\u043e\u0438\u0442\u044c \u043d\u0435\u0441\u043a\u043e\u043b\u044c\u043a\u043e." : "2x2 fuel service: trucks refuel here when routes get too long. Multiple stations are allowed.",
            _                          => string.Empty
        };
    }

}
