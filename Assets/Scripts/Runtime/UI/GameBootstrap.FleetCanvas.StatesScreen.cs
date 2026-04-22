using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private sealed class StatesEntryUi
    {
        public Text NameText;
        public Text ModifiersText;
        public Text DescText;
    }

    private sealed class StatesScreenUiRefs
    {
        public GameObject CanvasRoot;
        public Text TitleText;
        public Text SkillsSectionHeader;
        public Text EffectsSectionHeader;
        public Text ActivitiesSubheader;
        public Text NeedsSubheader;
        public Text PerksSectionHeader;
        public readonly List<StatesEntryUi> SkillEntries  = new();
        public readonly List<StatesEntryUi> EffectEntries = new();
        public readonly List<StatesEntryUi> PerkEntries   = new();
    }

    private StatesScreenUiRefs statesScreenUi;
    private bool isStatesScreenDirty = true;

    private void SetupStatesScreenUi()
    {
        if (statesScreenUi != null) return;

        EnsureFleetEventSystem();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statesScreenUi = new StatesScreenUiRefs();

        GameObject canvasObj = new("StatesScreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        statesScreenUi.CanvasRoot = canvasObj;

        RectTransform windowRoot = CreateStyledPanel("StatesScreenRoot", canvasObj.transform, FleetPanelColor);
        SetCenteredWindow(windowRoot, 800f, 660f, -16f);
        VerticalLayoutGroup rootLayout = windowRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        rootLayout.padding    = new RectOffset(18, 18, 16, 14);
        rootLayout.spacing    = 10;
        rootLayout.childControlWidth      = true;
        rootLayout.childControlHeight     = true;
        rootLayout.childForceExpandWidth  = true;
        rootLayout.childForceExpandHeight = false;

        // ── Title row ──────────────────────────────────────────────────────────
        RectTransform titleRow = CreateLayoutRow("TitleRow", windowRoot, 30f, 8f);
        statesScreenUi.TitleText = CreateHeaderText("Title", titleRow, font, string.Empty, 20, TextAnchor.MiddleLeft, Color.white);
        statesScreenUi.TitleText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        Button closeBtn = CreateButton("CloseBtn", titleRow, font, out _, "X", 13, new Color(0.26f, 0.30f, 0.36f, 1f), Color.white);
        LayoutElement closeBtnLE = closeBtn.gameObject.AddComponent<LayoutElement>();
        closeBtnLE.preferredWidth  = 28f;
        closeBtnLE.preferredHeight = 28f;
        closeBtn.onClick.AddListener(() =>
        {
            isStatesPanelOpen   = false;
            isStatesScreenDirty = true;
            PlayUiSound(uiPanelCloseClip, 0.82f);
        });

        // ── Scroll area ────────────────────────────────────────────────────────
        GameObject scrollObj = CreateUiObject("StatesScroll", windowRoot);
        scrollObj.AddComponent<LayoutElement>().flexibleHeight = 1f;
        scrollObj.AddComponent<RectMask2D>();
        ScrollRect scrollRect = scrollObj.AddComponent<ScrollRect>();

        GameObject contentObj = CreateUiObject("StatesContent", scrollObj.transform);
        RectTransform contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin  = new Vector2(0f, 1f);
        contentRt.anchorMax  = new Vector2(1f, 1f);
        contentRt.pivot      = new Vector2(0.5f, 1f);
        contentRt.offsetMin  = Vector2.zero;
        contentRt.offsetMax  = Vector2.zero;
        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing               = 6;
        contentLayout.padding               = new RectOffset(0, 10, 0, 8);
        contentLayout.childControlWidth     = true;
        contentLayout.childControlHeight    = true;
        contentLayout.childForceExpandWidth  = true;
        contentLayout.childForceExpandHeight = false;
        contentObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content          = contentRt;
        scrollRect.horizontal       = false;
        scrollRect.vertical         = true;
        scrollRect.movementType     = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        // ── Skills section ─────────────────────────────────────────────────────
        statesScreenUi.SkillsSectionHeader = BuildStatesSectionHeader("SkillsHdr", contentRt, font);
        for (int i = 0; i < 4; i++)
            statesScreenUi.SkillEntries.Add(BuildStatesEntry(contentRt, font, false));

        // ── Effects section — activities ───────────────────────────────────────
        statesScreenUi.EffectsSectionHeader = BuildStatesSectionHeader("EffectsHdr", contentRt, font);
        statesScreenUi.ActivitiesSubheader  = BuildStatesSubheader("ActSubhdr", contentRt, font);
        for (int i = 0; i < 12; i++)
            statesScreenUi.EffectEntries.Add(BuildStatesEntry(contentRt, font, true));

        // ── Effects section — needs ────────────────────────────────────────────
        statesScreenUi.NeedsSubheader = BuildStatesSubheader("NeedsSubhdr", contentRt, font);
        for (int i = 0; i < 6; i++)
            statesScreenUi.EffectEntries.Add(BuildStatesEntry(contentRt, font, true));

        // ── Perks section ──────────────────────────────────────────────────────
        statesScreenUi.PerksSectionHeader = BuildStatesSectionHeader("PerksHdr", contentRt, font);
        int totalPerkCount = NegativePerks.Length + PositivePerks.Length;
        for (int i = 0; i < totalPerkCount; i++)
            statesScreenUi.PerkEntries.Add(BuildStatesEntry(contentRt, font, false));

        statesScreenUi.CanvasRoot.SetActive(false);
        UpdateStatesScreenUi();
    }

    private void UpdateStatesScreenUi()
    {
        if (statesScreenUi == null)
        {
            if (isStatesPanelOpen) SetupStatesScreenUi();
            return;
        }

        bool shouldShow = isStatesPanelOpen;
        if (statesScreenUi.CanvasRoot.activeSelf != shouldShow)
            statesScreenUi.CanvasRoot.SetActive(shouldShow);

        if (!shouldShow) return;

        if (!isStatesScreenDirty) return;
        isStatesScreenDirty = false;

        bool ru = IsRussianLanguage();

        statesScreenUi.TitleText.text = ru ? "Состояния" : "States";

        // ── Section headers ────────────────────────────────────────────────────
        statesScreenUi.SkillsSectionHeader.text  = ru ? "Навыки" : "Skills";
        statesScreenUi.EffectsSectionHeader.text = ru ? "Эффекты" : "Effects";
        statesScreenUi.ActivitiesSubheader.text  = ru ? "— Активности —" : "— Activities —";
        statesScreenUi.NeedsSubheader.text       = ru ? "— Потребности —" : "— Needs —";
        statesScreenUi.PerksSectionHeader.text   = ru ? "Перки" : "Perks";

        // ── Skills ─────────────────────────────────────────────────────────────
        FillStatesSkill(statesScreenUi.SkillEntries[0], ru,
            ru ? "Вождение" : "Driving",
            ru ? "Влияет на рейсы грузовика, вождение на маршрутах и будущее управление в гонках."
               : "Affects truck trips, route driving, and future race handling.");

        FillStatesSkill(statesScreenUi.SkillEntries[1], ru,
            ru ? "Выносливость" : "Stamina",
            ru ? "Влияет на устойчивость к усталости, восстановление и будущие дебаффы потребностей."
               : "Affects tiredness tolerance, recovery pacing, and future need debuffs.");

        FillStatesSkill(statesScreenUi.SkillEntries[2], ru,
            ru ? "Производство" : "Production",
            ru ? "Влияет на работу в Лесу, Лесопилке и Мебельной фабрике."
               : "Affects work at Forest, Sawmill, and Furniture Factory.");

        FillStatesSkill(statesScreenUi.SkillEntries[3], ru,
            ru ? "Логистика" : "Logistics",
            ru ? "Влияет на склад, переноску, погрузку, разгрузку и будущие торговые рейсы."
               : "Affects Warehouse work, carrying, loading, unloading, and future trade runs.");

        // ── Activity effects (indices 0–10) ────────────────────────────────────
        int ei = 0;

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Отдохнул" : "Rested",
            BuildStatesMods(ru, driving: +1, stamina: +2),
            ru ? "Сон вернул фокус: больше выносливости и спокойнее рука на руле."
               : "Sleep restored focus: better stamina and a steadier hand on the wheel.",
            ru ? "Источник: отдых в мотеле." : "Source: sleeping in the motel.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Сытость" : "Well Fed",
            BuildStatesMods(ru, stamina: +2, production: +1, logistics: +1),
            ru ? "Нормальная еда повышает выносливость и чуть стабилизирует обычную работу."
               : "A proper meal improves stamina and makes ordinary work a little steadier.",
            ru ? "Источник: посещение столовой." : "Source: visiting the Canteen.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Лесная закалка" : "Forest Air",
            BuildStatesMods(ru, stamina: +1, production: +1),
            ru ? "Лесная работа грубая, зато воздух держит в тонусе."
               : "Forest work is rough, but the air keeps the worker sharp.",
            ru ? "Источник: смена в Лесу." : "Source: completing a Forest shift.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Пыль лесопилки" : "Sawdust",
            BuildStatesMods(ru, stamina: -1, production: +1),
            ru ? "Ритм производства хорош. Пыль — нет."
               : "The production rhythm is good. The dust is not.",
            ru ? "Источник: смена на Лесопилке." : "Source: completing a Sawmill shift.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Складской ритм" : "Warehouse Flow",
            BuildStatesMods(ru, stamina: -1, logistics: +2),
            ru ? "Погрузка идёт ровнее, но ноги устают."
               : "Loading work clicks into place, at the cost of tired legs.",
            ru ? "Источник: смена на Складе." : "Source: completing a Warehouse shift.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Точная сборка" : "Craft Focus",
            BuildStatesMods(ru, production: +2, logistics: -1),
            ru ? "Аккуратная сборка усиливает производство, но мешает быстрой логистике."
               : "Careful assembly improves production but makes quick logistics feel clumsy.",
            ru ? "Источник: смена на Мебельной фабрике." : "Source: completing a Furniture Factory shift.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "После смены" : "Worked Hard",
            BuildStatesMods(ru, stamina: -1),
            ru ? "Отработанный день оставляет немного усталости."
               : "A completed workday leaves a little fatigue behind.",
            ru ? "Источник: завершение любой смены." : "Source: finishing any work shift.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Опьянение" : "Drunk",
            BuildStatesMods(ru, driving: -5, production: +1, logistics: +1),
            ru ? "Вождение сильно снижено, а обычная работа чуть усилена. Алкоголик: Вождение −6, Производство +2, Логистика +2, длится дольше."
               : "Driving is heavily reduced, while ordinary work gets a small boost. Alcoholism: Driving −6, Production +2, Logistics +2, lasts longer.",
            ru ? "Источник: посещение Бара." : "Source: visiting the Bar.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Дорожный фокус" : "Road Focus",
            BuildStatesMods(ru, driving: +1, logistics: +1),
            ru ? "Завершённый рейс обостряет вождение и ритм погрузки."
               : "A completed route sharpens driving and loading rhythm.",
            ru ? "Источник: завершение торгового рейса." : "Source: completing a trade route.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Дорожная усталость" : "Road Fatigue",
            BuildStatesMods(ru, driving: -1, stamina: -2),
            ru ? "Дальний межгород оставляет водителя уставшим и менее точным."
               : "A long intercity run leaves the driver tired and less steady.",
            ru ? "Источник: завершение межгородского рейса." : "Source: completing an intercity run.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Гоночный азарт" : "Race Rush",
            BuildStatesMods(ru, driving: +2, stamina: -1),
            ru ? "Адреналин на время усиливает вождение, но тело платит."
               : "Adrenaline boosts driving for a while, but the body pays for it.",
            ru ? "Источник: участие в гонке." : "Source: participating in a race.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "В ударе" : "Lucky",
            BuildStatesMods(ru, production: +1, logistics: +1),
            ru ? "Удачная партия придаёт работнику энергии."
               : "A winning streak puts a spring in the worker's step.",
            ru ? "Источник: посещение Игровых автоматов." : "Source: visiting the Gambling Hall.");

        // ── Need effects (indices 12–17) ───────────────────────────────────────
        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Голод" : "Hungry",
            BuildStatesMods(ru, stamina: -2, production: -1, logistics: -1),
            ru ? "Пустой желудок бьёт по выносливости и физической работе."
               : "An empty stomach weakens stamina and physical work.",
            ru ? "Причина: без еды ~16 часов." : "Cause: no food for ~16 hours.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Сильный голод" : "Starving",
            BuildStatesMods(ru, driving: -1, stamina: -4, production: -2, logistics: -2),
            ru ? "Еду игнорировали слишком долго. Работа сыплется, вождение тоже просядает."
               : "Food has been ignored too long. Work collapses; driving starts to suffer.",
            ru ? "Причина: без еды ~24 часа." : "Cause: no food for ~24 hours.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Недосып" : "Sleep Deprived",
            BuildStatesMods(ru, driving: -2, stamina: -2, production: -1, logistics: -1),
            ru ? "Плохой сон сначала бьёт по вождению, потом по всему дню."
               : "Bad sleep hits driving first, then the rest of the day.",
            ru ? "Причина: без сна ~16 часов." : "Cause: no sleep for ~16 hours.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Измотан" : "Exhausted",
            BuildStatesMods(ru, driving: -4, stamina: -4, production: -2, logistics: -2),
            ru ? "Сна в костях не осталось. Хуже становится всё."
               : "No sleep left in the bones. Everything gets worse.",
            ru ? "Причина: без сна ~24 часа." : "Cause: no sleep for ~24 hours.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Скука" : "Bored",
            BuildStatesMods(ru, production: -1, logistics: -1),
            ru ? "Рабочий на месте, но искра где-то отстала."
               : "The worker is present, but the spark stayed somewhere else.",
            ru ? "Причина: без отдыха ~16 часов." : "Cause: no leisure for ~16 hours.");

        FillStatesEffect(statesScreenUi.EffectEntries[ei++], ru,
            ru ? "Выгорание" : "Burned Out",
            BuildStatesMods(ru, driving: -1, stamina: -1, production: -2, logistics: -2),
            ru ? "Нет досуга — нет искры. Почти любое дело становится тяжелее."
               : "No leisure, no spark. Almost every task becomes heavier.",
            ru ? "Причина: без отдыха ~24 часа." : "Cause: no leisure for ~24 hours.");

        // ── Perks ──────────────────────────────────────────────────────────────
        int pi = 0;
        foreach (WorkerPerkKind perk in NegativePerks)
        {
            if (pi < statesScreenUi.PerkEntries.Count)
            {
                StatesEntryUi entry = statesScreenUi.PerkEntries[pi++];
                FillStatesSkill(entry, ru, GetWorkerPerkDisplayName(perk, ru), GetWorkerPerkDescription(perk, ru));
                entry.NameText.color = GetWorkerPerkTypeColor(WorkerPerkType.Negative);
            }
        }
        foreach (WorkerPerkKind perk in PositivePerks)
        {
            if (pi < statesScreenUi.PerkEntries.Count)
            {
                StatesEntryUi entry = statesScreenUi.PerkEntries[pi++];
                FillStatesSkill(entry, ru, GetWorkerPerkDisplayName(perk, ru), GetWorkerPerkDescription(perk, ru));
                entry.NameText.color = GetWorkerPerkTypeColor(WorkerPerkType.Positive);
            }
        }
    }

    // ── Entry builders ─────────────────────────────────────────────────────────

    private static Text BuildStatesSectionHeader(string name, Transform parent, Font font)
    {
        RectTransform bg = CreateStyledPanel(name, parent, new Color(0.18f, 0.22f, 0.29f, 1f));
        LayoutElement le = bg.gameObject.AddComponent<LayoutElement>();
        le.preferredHeight = 28f;
        HorizontalLayoutGroup hlg = bg.gameObject.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(14, 14, 6, 6);
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;
        Text t = CreateHeaderText($"{name}Txt", bg, font, string.Empty, 14, TextAnchor.MiddleLeft, FleetAccentColor);
        return t;
    }

    private static Text BuildStatesSubheader(string name, Transform parent, Font font)
    {
        GameObject go = CreateUiObject(name, parent);
        go.AddComponent<LayoutElement>().preferredHeight = 22f;
        HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(14, 14, 4, 2);
        hlg.childControlWidth  = true;
        hlg.childControlHeight = true;
        Text t = CreateBodyText($"{name}Txt", go.GetComponent<RectTransform>(), font, string.Empty, 11, TextAnchor.MiddleCenter, FleetMutedTextColor);
        t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        return t;
    }

    private static StatesEntryUi BuildStatesEntry(Transform parent, Font font, bool hasModifiers)
    {
        RectTransform card = CreateStyledPanel($"StateEntry_{parent.childCount}", parent, FleetInsetColor);
        card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup vlg = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(14, 14, 8, 8);
        vlg.spacing               = 3;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var entry = new StatesEntryUi();
        entry.NameText = CreateHeaderText("EntryName", card, font, string.Empty, 13, TextAnchor.MiddleLeft, Color.white);
        entry.NameText.gameObject.AddComponent<LayoutElement>().preferredHeight = 18f;

        if (hasModifiers)
        {
            entry.ModifiersText = CreateBodyText("EntryMods", card, font, string.Empty, 11, TextAnchor.MiddleLeft, FleetSecondaryTextColor);
            entry.ModifiersText.gameObject.AddComponent<LayoutElement>().preferredHeight = 16f;
            entry.ModifiersText.supportRichText = true;
        }

        entry.DescText = CreateBodyText("EntryDesc", card, font, string.Empty, 11, TextAnchor.UpperLeft, FleetMutedTextColor);
        entry.DescText.horizontalOverflow = HorizontalWrapMode.Wrap;
        entry.DescText.verticalOverflow   = VerticalWrapMode.Overflow;
        LayoutElement descLE = entry.DescText.gameObject.AddComponent<LayoutElement>();
        descLE.minHeight       = 14f;
        descLE.flexibleHeight  = 0f;

        return entry;
    }

    // ── Fill helpers ───────────────────────────────────────────────────────────

    private static void FillStatesSkill(StatesEntryUi entry, bool ru, string name, string desc)
    {
        if (entry == null) return;
        entry.NameText.text = name;
        entry.DescText.text = desc;
    }

    private static void FillStatesEffect(StatesEntryUi entry, bool ru, string name, string mods, string desc, string source)
    {
        if (entry == null) return;
        entry.NameText.text = name;
        if (entry.ModifiersText != null)
            entry.ModifiersText.text = $"{mods}   <color=#{ColorUtility.ToHtmlStringRGB(FleetMutedTextColor)}>{source}</color>";
        entry.DescText.text = desc;
    }

    private static string BuildStatesMods(bool ru,
        int driving = 0, int stamina = 0, int production = 0, int logistics = 0)
    {
        string dLabel = ru ? "Вождение"      : "Driving";
        string sLabel = ru ? "Выносливость"  : "Stamina";
        string pLabel = ru ? "Производство"  : "Production";
        string lLabel = ru ? "Логистика"     : "Logistics";

        string positiveHex = "5cdb5c";
        string negativeHex = "e86060";

        var parts = new StringBuilder();
        AppendMod(parts, dLabel, driving,    positiveHex, negativeHex);
        AppendMod(parts, sLabel, stamina,    positiveHex, negativeHex);
        AppendMod(parts, pLabel, production, positiveHex, negativeHex);
        AppendMod(parts, lLabel, logistics,  positiveHex, negativeHex);
        return parts.ToString().Trim();
    }

    private static void AppendMod(StringBuilder sb, string label, int delta, string posHex, string negHex)
    {
        if (delta == 0) return;
        if (sb.Length > 0) sb.Append("   ");
        string hex = delta > 0 ? posHex : negHex;
        string sign = delta > 0 ? "+" : string.Empty;
        sb.Append($"{label}: <color=#{hex}>{sign}{delta}</color>");
    }
}
