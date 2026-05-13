using System;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const float WorkerTraitTooltipWidth = 330f;
    private const float WorkerTraitTooltipHeight = 142f;
    private const int WorkerPerkHudRowCount = 9;
    private const int WorkerStartTraitCount = 3;
    private const int WorkerWeaknessChancePercent = 33;

    private static readonly WorkerTraitKind[] WorkerTraitPool =
    {
        WorkerTraitKind.Sociable,
        WorkerTraitKind.Reserved,
        WorkerTraitKind.Frugal,
        WorkerTraitKind.Curious,
        WorkerTraitKind.Anxious,
        WorkerTraitKind.Impulsive,
        WorkerTraitKind.Cautious,
        WorkerTraitKind.Trusting,
        WorkerTraitKind.Skeptical,
        WorkerTraitKind.Stubborn,
        WorkerTraitKind.Adaptable,
        WorkerTraitKind.Meticulous,
        WorkerTraitKind.Dutiful
    };

    private static readonly WorkerWeaknessKind[] WorkerWeaknessPool =
    {
        WorkerWeaknessKind.Alcoholism,
        WorkerWeaknessKind.Gambling
    };

    private void AssignWorkerPerks(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        EnsureWorkerRace(driver);
        int seed = StableWorkerTraitHash(driver.DriverName) ^ (driver.DriverId * 19349663);
        AssignWorkerPerks(driver, new System.Random(seed));
    }

    private void AssignWorkerPerks(DriverAgent driver, System.Random rng)
    {
        if (driver == null || rng == null)
        {
            return;
        }

        driver.Perks.Clear();
        driver.Traits.Clear();
        driver.Weakness = WorkerWeaknessKind.None;
        NormalizeWorkerPersonality(driver, rng);
    }

    private void EnsureWorkerPerks(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        EnsureWorkerRace(driver);
        int seed = StableWorkerTraitHash(driver.DriverName) ^ (driver.DriverId * 19349663);
        NormalizeWorkerPersonality(driver, new System.Random(seed));
    }

    private static bool IsWorkerTraitSetValid(DriverAgent driver)
    {
        if (driver == null || driver.Traits.Count != Mathf.Min(WorkerStartTraitCount, WorkerTraitPool.Length))
        {
            return false;
        }

        for (int i = 0; i < driver.Traits.Count; i++)
        {
            WorkerTraitKind trait = driver.Traits[i];
            if (!IsWorkerTraitInPool(trait) || HasConflictingWorkerTrait(driver, trait, i))
            {
                return false;
            }

            for (int j = i + 1; j < driver.Traits.Count; j++)
            {
                if (driver.Traits[j] == trait)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void NormalizeWorkerPersonality(DriverAgent driver, System.Random rng)
    {
        if (driver == null || rng == null)
        {
            return;
        }

        MigrateLegacyWorkerPerks(driver, rng);
        RemoveInvalidOrDuplicateWorkerTraits(driver);

        WorkerTraitKind[] traitPool = (WorkerTraitKind[])WorkerTraitPool.Clone();
        ShuffleWorkerTraitPool(traitPool, rng);
        int targetCount = Mathf.Min(WorkerStartTraitCount, WorkerTraitPool.Length);
        for (int i = 0; i < traitPool.Length && driver.Traits.Count < targetCount; i++)
        {
            TryAddWorkerTrait(driver, traitPool[i]);
        }

        while (driver.Traits.Count > targetCount)
        {
            driver.Traits.RemoveAt(driver.Traits.Count - 1);
        }

        if (!IsWorkerWeaknessInPool(driver.Weakness))
        {
            driver.Weakness = WorkerWeaknessKind.None;
        }

        if (driver.Weakness == WorkerWeaknessKind.None &&
            WorkerWeaknessPool.Length > 0 &&
            rng.Next(100) < WorkerWeaknessChancePercent)
        {
            driver.Weakness = WorkerWeaknessPool[rng.Next(WorkerWeaknessPool.Length)];
        }
    }

    private static void MigrateLegacyWorkerPerks(DriverAgent driver, System.Random rng)
    {
        bool hadAlcoholism = false;
        bool hadGambler = false;
        for (int i = driver.Perks.Count - 1; i >= 0; i--)
        {
            switch (driver.Perks[i])
            {
                case WorkerPerkKind.Socialite:
                    TryAddWorkerTrait(driver, WorkerTraitKind.Sociable);
                    break;
                case WorkerPerkKind.Frugal:
                    TryAddWorkerTrait(driver, WorkerTraitKind.Frugal);
                    break;
                case WorkerPerkKind.Quicklearner:
                    TryAddWorkerTrait(driver, WorkerTraitKind.Curious);
                    break;
                case WorkerPerkKind.Alcoholism:
                    hadAlcoholism = true;
                    break;
                case WorkerPerkKind.Gambler:
                    hadGambler = true;
                    break;
            }
        }

        driver.Perks.Clear();
        if (driver.Weakness != WorkerWeaknessKind.None)
        {
            return;
        }

        if (hadAlcoholism && hadGambler)
        {
            driver.Weakness = ShouldPreferLegacyGamblingWeakness(driver, rng)
                ? WorkerWeaknessKind.Gambling
                : WorkerWeaknessKind.Alcoholism;
        }
        else if (hadAlcoholism)
        {
            driver.Weakness = WorkerWeaknessKind.Alcoholism;
        }
        else if (hadGambler)
        {
            driver.Weakness = WorkerWeaknessKind.Gambling;
        }
    }

    private static bool ShouldPreferLegacyGamblingWeakness(DriverAgent driver, System.Random rng)
    {
        if (driver != null && (driver.GamblingBroke || driver.GamblingBetCount > 0))
        {
            return true;
        }

        return rng.Next(2) == 0;
    }

    private static bool TryAddWorkerTrait(DriverAgent driver, WorkerTraitKind trait)
    {
        if (driver == null || !IsWorkerTraitInPool(trait) || driver.Traits.Contains(trait) || HasConflictingWorkerTrait(driver, trait))
        {
            return false;
        }

        driver.Traits.Add(trait);
        return true;
    }

    private static void RemoveInvalidOrDuplicateWorkerTraits(DriverAgent driver)
    {
        for (int i = driver.Traits.Count - 1; i >= 0; i--)
        {
            WorkerTraitKind trait = driver.Traits[i];
            if (!IsWorkerTraitInPool(trait) || HasConflictingWorkerTrait(driver, trait, i))
            {
                driver.Traits.RemoveAt(i);
                continue;
            }

            for (int j = 0; j < i; j++)
            {
                if (driver.Traits[j] == trait)
                {
                    driver.Traits.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private static bool HasConflictingWorkerTrait(DriverAgent driver, WorkerTraitKind trait, int ignoreIndex = -1)
    {
        int group = GetWorkerTraitConflictGroup(trait);
        if (driver == null || group <= 0)
        {
            return false;
        }

        for (int i = 0; i < driver.Traits.Count; i++)
        {
            if (i == ignoreIndex)
            {
                continue;
            }

            if (GetWorkerTraitConflictGroup(driver.Traits[i]) == group)
            {
                return true;
            }
        }

        return false;
    }

    private static int GetWorkerTraitConflictGroup(WorkerTraitKind trait)
    {
        return trait switch
        {
            WorkerTraitKind.Sociable or WorkerTraitKind.Reserved => 1,
            WorkerTraitKind.Impulsive or WorkerTraitKind.Cautious => 2,
            WorkerTraitKind.Trusting or WorkerTraitKind.Skeptical => 3,
            WorkerTraitKind.Stubborn or WorkerTraitKind.Adaptable => 4,
            _ => 0
        };
    }

    private static bool IsWorkerTraitInPool(WorkerTraitKind trait)
    {
        for (int i = 0; i < WorkerTraitPool.Length; i++)
        {
            if (WorkerTraitPool[i] == trait)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWorkerWeaknessInPool(WorkerWeaknessKind weakness)
    {
        if (weakness == WorkerWeaknessKind.None)
        {
            return true;
        }

        for (int i = 0; i < WorkerWeaknessPool.Length; i++)
        {
            if (WorkerWeaknessPool[i] == weakness)
            {
                return true;
            }
        }

        return false;
    }

    private static void ShuffleWorkerTraitPool(WorkerTraitKind[] traits, System.Random rng)
    {
        if (traits == null || rng == null)
        {
            return;
        }

        for (int i = traits.Length - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (traits[i], traits[swapIndex]) = (traits[swapIndex], traits[i]);
        }
    }

    private void UpdateWorkerPerksUi(DriverAgent driver, bool ru)
    {
        if (driversScreenUi == null)
        {
            return;
        }

        EnsureWorkerPerks(driver);
        if (driversScreenUi.DetailPerksTitleText != null)
        {
            driversScreenUi.DetailPerksTitleText.text = ru ? "Личность" : "Personality";
        }

        if (driver == null)
        {
            if (driversScreenUi.DetailPerksEmptyText != null)
            {
                driversScreenUi.DetailPerksEmptyText.gameObject.SetActive(true);
                driversScreenUi.DetailPerksEmptyText.text = ru ? "Житель не выбран" : "No resident selected";
            }

            for (int i = 0; i < driversScreenUi.DetailPerkTexts.Count; i++)
            {
                driversScreenUi.DetailPerkTexts[i].gameObject.SetActive(false);
            }
            return;
        }

        if (driversScreenUi.DetailPerksEmptyText != null)
        {
            driversScreenUi.DetailPerksEmptyText.gameObject.SetActive(false);
        }

        UpdateWorkerAffects(driver);

        int rowIndex = 0;
        SetWorkerPersonalityTextRow(rowIndex++, ru ? "\u0420\u0430\u0441\u0430:" : "Race:", FleetAccentColor);
        Text raceText = SetWorkerPersonalityTextRow(
            rowIndex++,
            $"- {FormatWorkerRaceBadgeInline(driver.Race, ru)} - {GetWorkerRaceShortDescription(driver.Race, ru)}",
            FleetSecondaryTextColor);
        ConfigureWorkerRaceTooltip(raceText, driver.Race);

        SetWorkerPersonalityTextRow(rowIndex++, ru ? "Характер:" : "Character:", FleetAccentColor);
        for (int i = 0; i < driver.Traits.Count && rowIndex < driversScreenUi.DetailPerkTexts.Count; i++)
        {
            WorkerTraitKind trait = driver.Traits[i];
            string traitColor = ColorUtility.ToHtmlStringRGB(GetWorkerTraitColor(trait));
            Text rowText = SetWorkerPersonalityTextRow(
                rowIndex++,
                $"- <color=#{traitColor}>{GetWorkerTraitDisplayName(trait, ru)}</color> - {GetWorkerTraitShortDescription(trait, ru)}",
                FleetSecondaryTextColor);
            ConfigureWorkerTraitTooltip(rowText, trait);
        }

        SetWorkerPersonalityTextRow(rowIndex++, ru ? "Слабости:" : "Weaknesses:", FleetAccentColor);
        Text weaknessText = SetWorkerPersonalityTextRow(
            rowIndex++,
            $"- {FormatWorkerWeaknessInline(driver.Weakness, ru)}",
            FleetSecondaryTextColor);
        ConfigureWorkerWeaknessTooltip(weaknessText, driver.Weakness);

        SetWorkerPersonalityTextRow(
            rowIndex++,
            $"{(ru ? "Состояния" : "States")}: {FormatWorkerAffectsInline(driver, ru, 3)}",
            FleetAccentColor);

        for (int i = rowIndex; i < driversScreenUi.DetailPerkTexts.Count; i++)
        {
            Text rowText = driversScreenUi.DetailPerkTexts[i];
            ClearWorkerTraitTooltip(rowText);
            rowText.gameObject.SetActive(false);
        }
    }

    private Text SetWorkerPersonalityTextRow(int index, string text, Color color)
    {
        if (driversScreenUi == null || index < 0 || index >= driversScreenUi.DetailPerkTexts.Count)
        {
            return null;
        }

        Text rowText = driversScreenUi.DetailPerkTexts[index];
        ClearWorkerTraitTooltip(rowText);
        rowText.gameObject.SetActive(true);
        rowText.color = color;
        rowText.fontSize = 12;
        rowText.text = TrimWorkerPersonalityLine(text);
        return rowText;
    }

    private static bool HasWorkerTrait(DriverAgent driver, WorkerTraitKind trait)
    {
        return driver != null && driver.Traits.Contains(trait);
    }

    private static bool HasWorkerWeakness(DriverAgent driver, WorkerWeaknessKind weakness)
    {
        return driver != null && driver.Weakness == weakness;
    }

    private static string FormatWorkerWeaknessInline(WorkerWeaknessKind weakness, bool ru)
    {
        if (weakness == WorkerWeaknessKind.None)
        {
            return ru ? "нет устойчивой слабости" : "none";
        }

        string color = ColorUtility.ToHtmlStringRGB(GetWorkerWeaknessColor(weakness));
        return $"<color=#{color}>{GetWorkerWeaknessDisplayName(weakness, ru)}</color>";
    }

    private static string GetWorkerTraitDisplayName(WorkerTraitKind trait, bool ru)
    {
        return trait switch
        {
            WorkerTraitKind.Sociable => ru ? "Общительный" : "Sociable",
            WorkerTraitKind.Reserved => ru ? "Замкнутый" : "Reserved",
            WorkerTraitKind.Frugal => ru ? "Экономный" : "Frugal",
            WorkerTraitKind.Curious => ru ? "Любознательный" : "Curious",
            WorkerTraitKind.Anxious => ru ? "Тревожный" : "Anxious",
            WorkerTraitKind.Impulsive => ru ? "Импульсивный" : "Impulsive",
            WorkerTraitKind.Cautious => ru ? "Осторожный" : "Cautious",
            WorkerTraitKind.Trusting => ru ? "Доверчивый" : "Trusting",
            WorkerTraitKind.Skeptical => ru ? "Скептичный" : "Skeptical",
            WorkerTraitKind.Stubborn => ru ? "Упрямый" : "Stubborn",
            WorkerTraitKind.Adaptable => ru ? "Гибкий" : "Adaptable",
            WorkerTraitKind.Meticulous => ru ? "Аккуратный" : "Meticulous",
            WorkerTraitKind.Dutiful => ru ? "Ответственный" : "Dutiful",
            _ => ru ? "Черта" : "Trait"
        };
    }

    private static string GetWorkerTraitShortDescription(WorkerTraitKind trait, bool ru)
    {
        return trait switch
        {
            WorkerTraitKind.Sociable => ru ? "сильнее учится через людей" : "learns through people",
            WorkerTraitKind.Reserved => ru ? "держит дистанцию" : "keeps distance",
            WorkerTraitKind.Frugal => ru ? "замечает цену решений" : "notices costs",
            WorkerTraitKind.Curious => ru ? "быстрее собирает знания" : "forms knowledge faster",
            WorkerTraitKind.Anxious => ru ? "раньше видит угрозы" : "spots threats early",
            WorkerTraitKind.Impulsive => ru ? "ярче реагирует на новое" : "reacts strongly",
            WorkerTraitKind.Cautious => ru ? "сначала видит риск" : "sees risk first",
            WorkerTraitKind.Trusting => ru ? "легче принимает слова" : "accepts framing easier",
            WorkerTraitKind.Skeptical => ru ? "ждет подтверждений" : "waits for proof",
            WorkerTraitKind.Stubborn => ru ? "медленнее меняет мнение" : "changes stance slowly",
            WorkerTraitKind.Adaptable => ru ? "быстрее перестраивается" : "adapts quickly",
            WorkerTraitKind.Meticulous => ru ? "замечает беспорядок" : "notices disorder",
            WorkerTraitKind.Dutiful => ru ? "сильно чувствует долг" : "feels obligations",
            _ => ru ? "черта характера" : "character trait"
        };
    }

    private static string GetWorkerTraitDescription(WorkerTraitKind trait, bool ru)
    {
        return trait switch
        {
            WorkerTraitKind.Sociable => ru ? "Тянется к людям: чаще заводит idle-разговоры, быстрее превращает встречи в знакомства, позитивные разговоры сильнее влияют на знания." : "Drawn to people: starts idle conversations more often, forms acquaintances faster, and positive conversations shape knowledge more strongly.",
            WorkerTraitKind.Reserved => ru ? "Держит дистанцию: реже вступает в случайные разговоры и слабее зависит от социальных сигналов." : "Keeps distance: enters random conversations less often and depends less on social signals.",
            WorkerTraitKind.Frugal => ru ? "Сильно замечает цену решений: деньги, мотель, бар, игровые автоматы и платные сервисы сильнее влияют на мысли и знания." : "Notices the price of decisions: money, Motel, Bar, Gambling Hall, and paid services weigh more in thoughts and knowledge.",
            WorkerTraitKind.Curious => ru ? "Хочет разобраться: быстрее формирует WorkerKnowledge и увереннее обновляет знания о зданиях и темах." : "Wants to understand: forms WorkerKnowledge faster and updates building/topic knowledge with more confidence.",
            WorkerTraitKind.Anxious => ru ? "Рано замечает угрозы: деньги, критические needs, семья, школы и мусор дают более интенсивные мысли." : "Spots threats early: money, critical needs, family, schools, and litter create more intense thoughts.",
            WorkerTraitKind.Impulsive => ru ? "Быстро эмоционально реагирует: свежие события сильнее двигают мнение, но уверенность ниже." : "Reacts quickly: fresh events move opinions more strongly, but confidence is lower.",
            WorkerTraitKind.Cautious => ru ? "Сначала видит риск: дорогие сервисы, малый запас денег, игровой зал и сомнительные слухи получают осторожный минус." : "Sees risk first: expensive services, low money, Gambling Hall, and suspicious rumors receive a cautious penalty.",
            WorkerTraitKind.Trusting => ru ? "Легче принимает чужие слова и framing игрока: слухи и доверенные источники сильнее двигают мнения." : "Accepts other voices and player framing more easily: rumors and trusted speakers move opinions more.",
            WorkerTraitKind.Skeptical => ru ? "Не верит сразу: слухи и framing слабее, личный опыт и повторения важнее." : "Does not believe immediately: rumors and framing are weaker, while personal experience and repetition matter more.",
            WorkerTraitKind.Stubborn => ru ? "Медленно меняет позицию: прежнее мнение имеет больший вес." : "Changes stance slowly: previous opinions have more weight.",
            WorkerTraitKind.Adaptable => ru ? "Быстрее перестраивается под новые обстоятельства: новые факты легче обновляют мнение." : "Adapts to new circumstances faster: new facts update opinions more easily.",
            WorkerTraitKind.Meticulous => ru ? "Замечает беспорядок, детали и нарушения нормы: мусор и хаос улиц сильнее попадают в мысли." : "Notices disorder, details, and broken norms: litter and street chaos affect thoughts more.",
            WorkerTraitKind.Dutiful => ru ? "Сильно реагирует на обязательства: работа, семья, дети, upkeep, школы и незанятость сильнее влияют на мысли." : "Feels obligations strongly: work, family, children, upkeep, schools, and unemployment shape thoughts more.",
            _ => ru ? "Нейтральная черта характера." : "A neutral character trait."
        };
    }

    private static Color GetWorkerTraitColor(WorkerTraitKind trait)
    {
        return trait switch
        {
            WorkerTraitKind.Frugal or WorkerTraitKind.Cautious or WorkerTraitKind.Meticulous => new Color(0.62f, 0.82f, 1f, 1f),
            WorkerTraitKind.Sociable or WorkerTraitKind.Trusting or WorkerTraitKind.Adaptable => new Color(0.58f, 0.92f, 0.66f, 1f),
            WorkerTraitKind.Anxious or WorkerTraitKind.Impulsive or WorkerTraitKind.Stubborn => new Color(1f, 0.78f, 0.42f, 1f),
            _ => new Color(0.76f, 0.76f, 0.94f, 1f)
        };
    }

    private static string GetWorkerWeaknessDisplayName(WorkerWeaknessKind weakness, bool ru)
    {
        return weakness switch
        {
            WorkerWeaknessKind.Alcoholism => ru ? "Алкоголизм" : "Alcoholism",
            WorkerWeaknessKind.Gambling => ru ? "Азартность" : "Gambling",
            _ => ru ? "Нет" : "None"
        };
    }

    private static string GetWorkerWeaknessDescription(WorkerWeaknessKind weakness, bool ru)
    {
        return weakness switch
        {
            WorkerWeaknessKind.Alcoholism => ru ? "Устойчивый риск-паттерн: житель чаще выбирает бар, бар сильнее окрашивает знания, а после отдыха возможны облегчение или похмелье." : "A steady risk pattern: the resident chooses the Bar more often, Bar knowledge becomes more personal, and rest can leave relief or a hangover.",
            WorkerWeaknessKind.Gambling => ru ? "Устойчивый риск-паттерн: житель чаще выбирает игровые автоматы, использует рискованную gambling-логику, а выигрыш или проигрыш создают сильные состояния." : "A steady risk pattern: the resident chooses gambling machines more often, uses risky gambling logic, and wins or losses create stronger states.",
            _ => ru ? "У жителя нет выраженной устойчивой слабости." : "The resident has no steady weakness."
        };
    }

    private static Color GetWorkerWeaknessColor(WorkerWeaknessKind weakness)
    {
        return weakness switch
        {
            WorkerWeaknessKind.Alcoholism => new Color(1f, 0.68f, 0.38f, 1f),
            WorkerWeaknessKind.Gambling => new Color(1f, 0.56f, 0.95f, 1f),
            _ => FleetSecondaryTextColor
        };
    }

    private static string TrimWorkerPersonalityLine(string text, int maxLength = 96)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
        {
            return text ?? string.Empty;
        }

        return text.Substring(0, Mathf.Max(0, maxLength - 1)).TrimEnd() + "...";
    }

    private static int StableWorkerTraitHash(string value)
    {
        unchecked
        {
            int hash = 29;
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 37 + value[i];
                }
            }

            return hash;
        }
    }
}
