using System;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const float WorkerTraitTooltipWidth = 330f;
    private const float WorkerTraitTooltipHeight = 142f;
    private const int WorkerPerkHudRowCount = 7;
    private const int WorkerStartTraitCount = 2;

    private static readonly WorkerPerkKind[] WorkerTraitPool =
    {
        WorkerPerkKind.Socialite,
        WorkerPerkKind.Frugal,
        WorkerPerkKind.Quicklearner
    };

    private static readonly WorkerLeisurePreferenceKind[] WorkerLeisurePreferencePool =
    {
        WorkerLeisurePreferenceKind.BarRegular,
        WorkerLeisurePreferenceKind.RiskPlayer,
        WorkerLeisurePreferenceKind.NatureWalker,
        WorkerLeisurePreferenceKind.StreetWanderer
    };

    private void AssignWorkerPerks(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

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
        driver.LeisurePreference = WorkerLeisurePreferenceKind.None;
        NormalizeWorkerPersonality(driver, rng);
    }

    private void EnsureWorkerPerks(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        int seed = StableWorkerTraitHash(driver.DriverName) ^ (driver.DriverId * 19349663);
        NormalizeWorkerPersonality(driver, new System.Random(seed));
    }

    private static bool IsWorkerTraitSetValid(DriverAgent driver)
    {
        if (driver == null || driver.Perks.Count != Mathf.Min(WorkerStartTraitCount, WorkerTraitPool.Length))
        {
            return false;
        }

        for (int i = 0; i < driver.Perks.Count; i++)
        {
            WorkerPerkKind perk = driver.Perks[i];
            if (!IsWorkerTraitInPool(perk))
            {
                return false;
            }

            for (int j = i + 1; j < driver.Perks.Count; j++)
            {
                if (driver.Perks[j] == perk)
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

        if (!IsWorkerLeisurePreferenceInPool(driver.LeisurePreference))
        {
            driver.LeisurePreference = WorkerLeisurePreferenceKind.None;
        }

        bool hadAlcoholism = RemoveWorkerLegacyPerk(driver, WorkerPerkKind.Alcoholism);
        bool hadGambler = RemoveWorkerLegacyPerk(driver, WorkerPerkKind.Gambler);
        if (driver.LeisurePreference == WorkerLeisurePreferenceKind.None)
        {
            if (hadAlcoholism && hadGambler)
            {
                driver.LeisurePreference = rng.Next(2) == 0
                    ? WorkerLeisurePreferenceKind.BarRegular
                    : WorkerLeisurePreferenceKind.RiskPlayer;
            }
            else if (hadAlcoholism)
            {
                driver.LeisurePreference = WorkerLeisurePreferenceKind.BarRegular;
            }
            else if (hadGambler)
            {
                driver.LeisurePreference = WorkerLeisurePreferenceKind.RiskPlayer;
            }
        }

        RemoveInvalidOrDuplicateWorkerTraits(driver);

        WorkerPerkKind[] traitPool = (WorkerPerkKind[])WorkerTraitPool.Clone();
        ShuffleWorkerTraitPool(traitPool, rng);
        int targetCount = Mathf.Min(WorkerStartTraitCount, WorkerTraitPool.Length);
        for (int i = 0; i < traitPool.Length && driver.Perks.Count < targetCount; i++)
        {
            if (!driver.Perks.Contains(traitPool[i]))
            {
                driver.Perks.Add(traitPool[i]);
            }
        }

        while (driver.Perks.Count > targetCount)
        {
            driver.Perks.RemoveAt(driver.Perks.Count - 1);
        }

        if (driver.LeisurePreference == WorkerLeisurePreferenceKind.None && WorkerLeisurePreferencePool.Length > 0)
        {
            driver.LeisurePreference = WorkerLeisurePreferencePool[rng.Next(WorkerLeisurePreferencePool.Length)];
        }
    }

    private static bool RemoveWorkerLegacyPerk(DriverAgent driver, WorkerPerkKind legacyPerk)
    {
        bool removed = false;
        for (int i = driver.Perks.Count - 1; i >= 0; i--)
        {
            if (driver.Perks[i] == legacyPerk)
            {
                driver.Perks.RemoveAt(i);
                removed = true;
            }
        }

        return removed;
    }

    private static void RemoveInvalidOrDuplicateWorkerTraits(DriverAgent driver)
    {
        for (int i = driver.Perks.Count - 1; i >= 0; i--)
        {
            WorkerPerkKind perk = driver.Perks[i];
            if (!IsWorkerTraitInPool(perk))
            {
                driver.Perks.RemoveAt(i);
                continue;
            }

            for (int j = 0; j < i; j++)
            {
                if (driver.Perks[j] == perk)
                {
                    driver.Perks.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private static bool IsWorkerTraitInPool(WorkerPerkKind perk)
    {
        for (int i = 0; i < WorkerTraitPool.Length; i++)
        {
            if (WorkerTraitPool[i] == perk)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWorkerLeisurePreferenceInPool(WorkerLeisurePreferenceKind preference)
    {
        for (int i = 0; i < WorkerLeisurePreferencePool.Length; i++)
        {
            if (WorkerLeisurePreferencePool[i] == preference)
            {
                return true;
            }
        }

        return false;
    }

    private static void ShuffleWorkerTraitPool(WorkerPerkKind[] perks, System.Random rng)
    {
        if (perks == null || rng == null)
        {
            return;
        }

        for (int i = perks.Length - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(i + 1);
            (perks[i], perks[swapIndex]) = (perks[swapIndex], perks[i]);
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
            driversScreenUi.DetailPerksTitleText.text = ru ? "\u041b\u0438\u0447\u043d\u043e\u0441\u0442\u044c" : "Personality";
        }

        if (driver == null)
        {
            if (driversScreenUi.DetailPerksEmptyText != null)
            {
                driversScreenUi.DetailPerksEmptyText.gameObject.SetActive(true);
                driversScreenUi.DetailPerksEmptyText.text = ru ? "\u0416\u0438\u0442\u0435\u043b\u044c \u043d\u0435 \u0432\u044b\u0431\u0440\u0430\u043d" : "No resident selected";
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
        string leisureColor = ColorUtility.ToHtmlStringRGB(GetWorkerLeisurePreferenceColor(driver.LeisurePreference));
        Text leisureText = SetWorkerPersonalityTextRow(
            rowIndex++,
            $"{(ru ? "\u0414\u043e\u0441\u0443\u0433" : "Leisure")}: <color=#{leisureColor}>{GetWorkerLeisurePreferenceDisplayName(driver.LeisurePreference, ru)}</color>",
            FleetSecondaryTextColor);
        ConfigureWorkerLeisurePreferenceTooltip(leisureText, driver.LeisurePreference);

        for (int i = 0; i < driver.Perks.Count && rowIndex < driversScreenUi.DetailPerkTexts.Count; i++)
        {
            WorkerPerkKind perk = driver.Perks[i];
            string perkColor = ColorUtility.ToHtmlStringRGB(GetWorkerPerkColor());
            Text rowText = SetWorkerPersonalityTextRow(
                rowIndex++,
                $"{(ru ? "\u0427\u0435\u0440\u0442\u0430" : "Trait")}: <color=#{perkColor}>{GetWorkerPerkDisplayName(perk, ru)}</color> - {GetWorkerPerkShortDescription(perk, ru)}",
                FleetSecondaryTextColor);
            ConfigureWorkerPerkTooltip(rowText, perk);
        }

        SetWorkerPersonalityTextRow(
            rowIndex++,
            $"{(ru ? "\u0421\u043e\u0441\u0442\u043e\u044f\u043d\u0438\u044f" : "States")}: {FormatWorkerAffectsInline(driver, ru, 3)}",
            FleetSecondaryTextColor);

        SetWorkerPersonalityTextRow(
            rowIndex++,
            $"{(ru ? "\u0413\u043b\u0430\u0432\u043d\u0430\u044f \u043c\u044b\u0441\u043b\u044c" : "Main thought")}: {FormatWorkerMainThoughtInline(driver, ru)}",
            FleetSecondaryTextColor);

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

    private static bool HasWorkerPerk(DriverAgent driver, WorkerPerkKind perk)
    {
        return driver != null && driver.Perks.Contains(perk);
    }

    private static bool HasWorkerLeisurePreference(DriverAgent driver, WorkerLeisurePreferenceKind preference)
    {
        return GetWorkerLeisurePreference(driver) == preference;
    }

    private static string GetWorkerPerkDisplayName(WorkerPerkKind perk, bool ru)
    {
        return perk switch
        {
            WorkerPerkKind.Socialite    => ru ? "\u041e\u0431\u0449\u0438\u0442\u0435\u043b\u044c\u043d\u044b\u0439" : "Socialite",
            WorkerPerkKind.Frugal       => ru ? "\u042d\u043a\u043e\u043d\u043e\u043c\u043d\u044b\u0439" : "Frugal",
            WorkerPerkKind.Quicklearner => ru ? "\u0421\u043c\u044b\u0448\u043b\u0435\u043d\u044b\u0439" : "Quick Learner",
            _ => ru ? "\u0427\u0435\u0440\u0442\u0430" : "Trait"
        };
    }

    private static Color GetWorkerPerkColor()
    {
        return new Color(0.62f, 0.82f, 1f, 1f);
    }

    private static string GetWorkerPerkDescription(WorkerPerkKind perk, bool ru)
    {
        return perk switch
        {
            WorkerPerkKind.Socialite => ru
                ? "\u041e\u0431\u0449\u0438\u0442\u0435\u043b\u0435\u043d: \u0447\u0430\u0449\u0435 \u0437\u0430\u0432\u043e\u0434\u0438\u0442 idle-\u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u044b, \u043b\u0435\u0433\u0447\u0435 \u043f\u0440\u0435\u0432\u0440\u0430\u0449\u0430\u0435\u0442 \u0432\u0441\u0442\u0440\u0435\u0447\u0438 \u0432 \u0437\u043d\u0430\u043a\u043e\u043c\u0441\u0442\u0432\u0430 \u0438 \u0431\u044b\u0441\u0442\u0440\u0435\u0435 \u0443\u043a\u0440\u0435\u043f\u043b\u044f\u0435\u0442 \u0441\u0432\u044f\u0437\u0438."
                : "Social worker; starts idle conversations more often, turns encounters into acquaintances more easily, and strengthens bonds faster.",
            WorkerPerkKind.Frugal => ru
                ? "\u042d\u043a\u043e\u043d\u043e\u043c\u0438\u0442 \u0434\u0435\u043d\u044c\u0433\u0438; \u0431\u043e\u043b\u0435\u0435 \u043f\u043e\u0445\u043e\u0436 \u043d\u0430 \u0436\u0438\u0442\u0435\u043b\u044f, \u043a\u043e\u0442\u043e\u0440\u044b\u0439 \u0441\u043c\u043e\u0436\u0435\u0442 \u043d\u0430\u043a\u043e\u043f\u0438\u0442\u044c \u043d\u0430 \u0434\u043e\u043c."
                : "Saves money; more likely to become the kind of resident who can afford a home.",
            WorkerPerkKind.Quicklearner => ru
                ? "\u0411\u044b\u0441\u0442\u0440\u0435\u0435 \u043d\u0430\u0431\u0438\u0440\u0430\u0435\u0442 \u043f\u0440\u043e\u0444\u043e\u043f\u044b\u0442: \u0443\u0440. 2 \u043f\u043e\u0441\u043b\u0435 2 \u0440\u0430\u0431. \u0434\u043d\u0435\u0439, \u0443\u0440. 3 \u043f\u043e\u0441\u043b\u0435 7."
                : "Gains professional experience faster: level 2 after 2 workdays, level 3 after 7.",
            _ => ru ? "\u041f\u043e\u0441\u0442\u043e\u044f\u043d\u043d\u0430\u044f \u0447\u0435\u0440\u0442\u0430 \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e." : "A permanent worker trait."
        };
    }

    private static string GetWorkerPerkShortDescription(WorkerPerkKind perk, bool ru)
    {
        return perk switch
        {
            WorkerPerkKind.Socialite => ru ? "\u0431\u044b\u0441\u0442\u0440\u0435\u0435 \u0437\u043d\u0430\u043a\u043e\u043c\u0438\u0442\u0441\u044f" : "faster social bonds",
            WorkerPerkKind.Frugal => ru ? "\u043a\u043e\u043f\u0438\u0442 \u0434\u0435\u043d\u044c\u0433\u0438" : "saves money",
            WorkerPerkKind.Quicklearner => ru ? "\u0431\u044b\u0441\u0442\u0440\u0435\u0435 \u0440\u0430\u0441\u0442\u0435\u0442" : "levels faster",
            _ => ru ? "\u0447\u0435\u0440\u0442\u0430" : "trait"
        };
    }

    private static WorkerLeisurePreferenceKind GetWorkerLeisurePreference(DriverAgent driver)
    {
        return driver?.LeisurePreference ?? WorkerLeisurePreferenceKind.None;
    }

    private static string GetWorkerLeisurePreferenceDisplayName(WorkerLeisurePreferenceKind preference, bool ru)
    {
        return preference switch
        {
            WorkerLeisurePreferenceKind.BarRegular => ru ? "\u0411\u0430\u0440\u043d\u044b\u0439 \u043e\u0442\u0434\u044b\u0445" : "Bar regular",
            WorkerLeisurePreferenceKind.RiskPlayer => ru ? "\u0410\u0437\u0430\u0440\u0442\u043d\u044b\u0439 \u0434\u043e\u0441\u0443\u0433" : "Risk player",
            WorkerLeisurePreferenceKind.NatureWalker => ru ? "\u041f\u0440\u043e\u0433\u0443\u043b\u043a\u0438 \u043d\u0430 \u043f\u0440\u0438\u0440\u043e\u0434\u0435" : "Nature walker",
            WorkerLeisurePreferenceKind.StreetWanderer => ru ? "\u0421\u0432\u043e\u0431\u043e\u0434\u043d\u0430\u044f \u043f\u0440\u043e\u0433\u0443\u043b\u043a\u0430" : "Street wanderer",
            _ => ru ? "\u0411\u0435\u0437 \u043f\u0440\u0438\u0432\u044b\u0447\u043a\u0438" : "No preference"
        };
    }

    private static string GetWorkerLeisurePreferenceDescription(WorkerLeisurePreferenceKind preference, bool ru)
    {
        return preference switch
        {
            WorkerLeisurePreferenceKind.BarRegular => ru
                ? "\u0416\u0438\u0442\u0435\u043b\u044c \u0447\u0430\u0449\u0435 \u0432\u044b\u0431\u0438\u0440\u0430\u0435\u0442 \u0431\u0430\u0440, \u043a\u043e\u0433\u0434\u0430 \u0435\u043c\u0443 \u043d\u0443\u0436\u0435\u043d \u0434\u043e\u0441\u0443\u0433. \u0411\u0430\u0440 \u0441\u0438\u043b\u044c\u043d\u0435\u0435 \u0432\u043b\u0438\u044f\u0435\u0442 \u043d\u0430 \u0435\u0433\u043e \u0432\u043f\u0435\u0447\u0430\u0442\u043b\u0435\u043d\u0438\u044f \u043e \u0433\u043e\u0440\u043e\u0434\u0435."
                : "The resident more often chooses the Bar when they need leisure. The Bar has stronger influence on their city impressions.",
            WorkerLeisurePreferenceKind.RiskPlayer => ru
                ? "\u0416\u0438\u0442\u0435\u043b\u044c \u0447\u0430\u0449\u0435 \u0432\u044b\u0431\u0438\u0440\u0430\u0435\u0442 \u0438\u0433\u0440\u043e\u0432\u044b\u0435 \u0430\u0432\u0442\u043e\u043c\u0430\u0442\u044b. \u0412\u044b\u0438\u0433\u0440\u044b\u0448 \u0438\u043b\u0438 \u043f\u0440\u043e\u0438\u0433\u0440\u044b\u0448 \u0441\u0438\u043b\u044c\u043d\u0435\u0435 \u043e\u043a\u0440\u0430\u0448\u0438\u0432\u0430\u0435\u0442 \u0435\u0433\u043e \u043c\u044b\u0441\u043b\u0438."
                : "The resident more often chooses gambling machines. Wins and losses color their thoughts more strongly.",
            WorkerLeisurePreferenceKind.NatureWalker => ru
                ? "\u0416\u0438\u0442\u0435\u043b\u044c \u0447\u0430\u0449\u0435 \u0438\u0434\u0435\u0442 \u0432 \u043f\u0430\u0440\u043a \u0438 \u043b\u0443\u0447\u0448\u0435 \u0437\u0430\u043f\u043e\u043c\u0438\u043d\u0430\u0435\u0442 \u0437\u0435\u043b\u0435\u043d\u044b\u0435 \u043c\u0435\u0441\u0442\u0430."
                : "The resident more often chooses the park and remembers green places more warmly.",
            WorkerLeisurePreferenceKind.StreetWanderer => ru
                ? "\u0416\u0438\u0442\u0435\u043b\u044c \u0442\u0435\u0440\u043f\u0438\u043c\u0435\u0435 \u043a \u043e\u0442\u0434\u044b\u0445\u0443 \u0431\u0435\u0437 \u0437\u0434\u0430\u043d\u0438\u044f \u0438 \u0447\u0430\u0449\u0435 \u0437\u0430\u043c\u0435\u0447\u0430\u0435\u0442 \u0443\u043b\u0438\u0447\u043d\u0443\u044e \u0441\u0440\u0435\u0434\u0443."
                : "The resident tolerates leisure without a building better and notices street conditions more.",
            _ => ru
                ? "\u041d\u0435\u0442 \u0443\u0441\u0442\u043e\u0439\u0447\u0438\u0432\u043e\u0439 \u0434\u043e\u0441\u0443\u0433\u043e\u0432\u043e\u0439 \u043f\u0440\u0438\u0432\u044b\u0447\u043a\u0438."
                : "No steady leisure habit."
        };
    }

    private static Color GetWorkerLeisurePreferenceColor(WorkerLeisurePreferenceKind preference)
    {
        return preference switch
        {
            WorkerLeisurePreferenceKind.BarRegular => new Color(1f, 0.72f, 0.42f, 1f),
            WorkerLeisurePreferenceKind.RiskPlayer => new Color(1f, 0.56f, 0.95f, 1f),
            WorkerLeisurePreferenceKind.NatureWalker => new Color(0.42f, 0.92f, 0.58f, 1f),
            WorkerLeisurePreferenceKind.StreetWanderer => new Color(0.72f, 0.86f, 1f, 1f),
            _ => new Color(0.62f, 0.82f, 1f, 1f)
        };
    }

    private string FormatWorkerMainThoughtInline(DriverAgent worker, bool ru)
    {
        WorkerThought thought = GetMostImportantWorkerThought(worker);
        if (thought == null)
        {
            return ru ? "\u043d\u0435\u0442 \u0441\u0438\u043b\u044c\u043d\u043e\u0439" : "none strong";
        }

        return TrimWorkerPersonalityLine(RenderWorkerThought(thought, ru), 74);
    }

    private static string TrimWorkerPersonalityLine(string text, int maxLength = 96)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= maxLength)
        {
            return text ?? string.Empty;
        }

        return text.Substring(0, Mathf.Max(0, maxLength - 1)).TrimEnd() + "\u2026";
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
