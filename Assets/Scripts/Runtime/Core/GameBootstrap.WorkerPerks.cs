using System;
using UnityEngine;
using UnityEngine.UI;

public partial class GameBootstrap
{
    private const float WorkerTraitTooltipWidth = 330f;
    private const float WorkerTraitTooltipHeight = 142f;
    private const int WorkerPerkHudRowCount = 5;
    private const int WorkerNegativePerkCount = 1;
    private const int WorkerPositivePerkCount = 4;

    private static readonly WorkerPerkKind[] NegativePerks = { WorkerPerkKind.Alcoholism, WorkerPerkKind.Gambler };
    private static readonly WorkerPerkKind[] PositivePerks =
    {
        WorkerPerkKind.Nightowl, WorkerPerkKind.Ironman, WorkerPerkKind.Motorhead, WorkerPerkKind.Trader,
        WorkerPerkKind.Handyman, WorkerPerkKind.Socialite, WorkerPerkKind.Frugal, WorkerPerkKind.Quicklearner
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

        int negativeCount = Mathf.Min(WorkerNegativePerkCount, NegativePerks.Length);
        int positiveCount = Mathf.Min(WorkerPositivePerkCount, PositivePerks.Length);

        WorkerPerkKind[] negativePool = (WorkerPerkKind[])NegativePerks.Clone();
        WorkerPerkKind[] positivePool = (WorkerPerkKind[])PositivePerks.Clone();

        ShuffleWorkerPerkPool(negativePool, rng);
        ShuffleWorkerPerkPool(positivePool, rng);

        for (int i = 0; i < negativeCount; i++)
        {
            driver.Perks.Add(negativePool[i]);
        }

        for (int i = 0; i < positiveCount; i++)
        {
            driver.Perks.Add(positivePool[i]);
        }
    }

    private void EnsureWorkerPerks(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        int expectedPerkCount = Mathf.Min(WorkerNegativePerkCount, NegativePerks.Length) +
                                Mathf.Min(WorkerPositivePerkCount, PositivePerks.Length);
        if (driver.Perks.Count == expectedPerkCount)
        {
            return;
        }

        AssignWorkerPerks(driver);
    }

    private static void ShuffleWorkerPerkPool(WorkerPerkKind[] perks, System.Random rng)
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
            driversScreenUi.DetailPerksTitleText.text = ru ? "\u041f\u0435\u0440\u043a\u0438" : "Perks";
        }

        if (driver == null || driver.Perks.Count == 0)
        {
            if (driversScreenUi.DetailPerksEmptyText != null)
            {
                driversScreenUi.DetailPerksEmptyText.gameObject.SetActive(true);
                driversScreenUi.DetailPerksEmptyText.text = ru ? "\u041d\u0435\u0442 \u043f\u0435\u0440\u043a\u043e\u0432" : "No perks";
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

        int rowCount = driversScreenUi.DetailPerkTexts.Count;
        for (int i = 0; i < rowCount; i++)
        {
            Text rowText = driversScreenUi.DetailPerkTexts[i];
            if (i >= driver.Perks.Count)
            {
                rowText.gameObject.SetActive(false);
                continue;
            }

            WorkerPerkKind perk = driver.Perks[i];
            rowText.gameObject.SetActive(true);
            rowText.color = Color.white;
            rowText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(FleetAccentColor)}>{GetWorkerPerkDisplayName(perk, ru)}</color>";
            ConfigureWorkerPerkTooltip(rowText, perk);
        }
    }

    private static bool HasWorkerPerk(DriverAgent driver, WorkerPerkKind perk)
    {
        return driver != null && driver.Perks.Contains(perk);
    }

    private static string GetWorkerPerkDisplayName(WorkerPerkKind perk, bool ru)
    {
        return perk switch
        {
            WorkerPerkKind.Alcoholism   => ru ? "\u0410\u043b\u043a\u043e\u0433\u043e\u043b\u0438\u0437\u043c" : "Alcoholism",
            WorkerPerkKind.Gambler      => ru ? "\u041b\u0443\u0434\u043e\u043c\u0430\u043d" : "Gambler",
            WorkerPerkKind.Nightowl     => ru ? "\u0421\u043e\u0432\u0430" : "Night Owl",
            WorkerPerkKind.Ironman      => ru ? "\u0416\u0435\u043b\u0435\u0437\u043d\u044b\u0439" : "Iron Man",
            WorkerPerkKind.Motorhead    => ru ? "\u0410\u0432\u0442\u043e\u043b\u044e\u0431\u0438\u0442\u0435\u043b\u044c" : "Motorhead",
            WorkerPerkKind.Trader       => ru ? "\u0414\u0435\u043b\u0435\u0446" : "Trader",
            WorkerPerkKind.Handyman     => ru ? "\u041c\u0430\u0441\u0442\u0435\u0440" : "Handyman",
            WorkerPerkKind.Socialite    => ru ? "\u041e\u0431\u0449\u0438\u0442\u0435\u043b\u044c\u043d\u044b\u0439" : "Socialite",
            WorkerPerkKind.Frugal       => ru ? "\u042d\u043a\u043e\u043d\u043e\u043c\u043d\u044b\u0439" : "Frugal",
            WorkerPerkKind.Quicklearner => ru ? "\u0421\u043c\u044b\u0448\u043b\u0435\u043d\u044b\u0439" : "Quick Learner",
            _ => ru ? "\u041f\u0435\u0440\u043a" : "Perk"
        };
    }

    private static WorkerPerkType GetWorkerPerkType(WorkerPerkKind perk)
    {
        return perk switch
        {
            WorkerPerkKind.Alcoholism => WorkerPerkType.Negative,
            WorkerPerkKind.Gambler => WorkerPerkType.Negative,
            _ => WorkerPerkType.Positive
        };
    }

    private static Color GetWorkerPerkTypeColor(WorkerPerkType type)
    {
        return type switch
        {
            WorkerPerkType.Negative => new Color(0.96f, 0.46f, 0.38f, 1f),
            WorkerPerkType.Positive => new Color(0.55f, 0.88f, 0.44f, 1f),
            _ => FleetSecondaryTextColor
        };
    }

    private static string GetWorkerPerkDescription(WorkerPerkKind perk, bool ru)
    {
        return perk switch
        {
            WorkerPerkKind.Alcoholism => ru
                ? "\u0414\u043b\u044f \u0434\u043e\u0441\u0443\u0433\u0430 \u0442\u0430\u043a\u043e\u0439 \u0440\u0430\u0431\u043e\u0447\u0438\u0439 \u0432 \u043f\u0435\u0440\u0432\u0443\u044e \u043e\u0447\u0435\u0440\u0435\u0434\u044c \u0442\u044f\u043d\u0435\u0442\u0441\u044f \u0432 \u0411\u0430\u0440."
                : "For leisure, this worker strongly prefers the Bar.",
            WorkerPerkKind.Gambler => ru
                ? "\u0412\u0441\u0435\u0433\u0434\u0430 \u0432\u044b\u0431\u0438\u0440\u0430\u0435\u0442 \u0418\u0433\u0440\u043e\u0432\u044b\u0435 \u0430\u0432\u0442\u043e\u043c\u0430\u0442\u044b, \u0441\u0442\u0430\u0432\u0438\u0442 \u0431\u0430\u043b\u0430\u043d\u0441 \u0438 \u0438\u0433\u0440\u0430\u0435\u0442 \u0440\u0438\u0441\u043a\u043e\u0432\u0430\u043d\u043d\u0435\u0435 \u043e\u0431\u044b\u0447\u043d\u043e\u0433\u043e \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e."
                : "Always picks the Gambling Hall, bets the current balance, and plays riskier than a regular worker.",
            WorkerPerkKind.Nightowl => ru
                ? "\u0427\u0435\u0440\u0442\u0430 \u0445\u0430\u0440\u0430\u043a\u0442\u0435\u0440\u0430, \u043a\u043e\u0442\u043e\u0440\u0430\u044f \u0432\u0438\u0434\u043d\u0430 \u0432 \u043f\u0440\u043e\u0444\u0438\u043b\u0435 \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e."
                : "A personality trait shown in the worker profile.",
            WorkerPerkKind.Ironman => ru
                ? "\u0427\u0435\u0440\u0442\u0430 \u0445\u0430\u0440\u0430\u043a\u0442\u0435\u0440\u0430, \u043a\u043e\u0442\u043e\u0440\u0430\u044f \u0432\u0438\u0434\u043d\u0430 \u0432 \u043f\u0440\u043e\u0444\u0438\u043b\u0435 \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e."
                : "A personality trait shown in the worker profile.",
            WorkerPerkKind.Motorhead => ru
                ? "\u041b\u044e\u0431\u0438\u0442 \u043c\u0430\u0448\u0438\u043d\u044b."
                : "Likes vehicles.",
            WorkerPerkKind.Trader => ru
                ? "\u0427\u0435\u0440\u0442\u0430 \u0445\u0430\u0440\u0430\u043a\u0442\u0435\u0440\u0430, \u043a\u043e\u0442\u043e\u0440\u0430\u044f \u0432\u0438\u0434\u043d\u0430 \u0432 \u043f\u0440\u043e\u0444\u0438\u043b\u0435 \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e."
                : "A personality trait shown in the worker profile.",
            WorkerPerkKind.Handyman => ru
                ? "\u0427\u0435\u0440\u0442\u0430 \u0445\u0430\u0440\u0430\u043a\u0442\u0435\u0440\u0430, \u043a\u043e\u0442\u043e\u0440\u0430\u044f \u0432\u0438\u0434\u043d\u0430 \u0432 \u043f\u0440\u043e\u0444\u0438\u043b\u0435 \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e."
                : "A personality trait shown in the worker profile.",
            WorkerPerkKind.Socialite => ru
                ? "\u0427\u0435\u0440\u0442\u0430 \u0445\u0430\u0440\u0430\u043a\u0442\u0435\u0440\u0430, \u043a\u043e\u0442\u043e\u0440\u0430\u044f \u0432\u0438\u0434\u043d\u0430 \u0432 \u043f\u0440\u043e\u0444\u0438\u043b\u0435 \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e."
                : "A personality trait shown in the worker profile.",
            WorkerPerkKind.Frugal => ru
                ? "\u0427\u0435\u0440\u0442\u0430 \u0445\u0430\u0440\u0430\u043a\u0442\u0435\u0440\u0430, \u043a\u043e\u0442\u043e\u0440\u0430\u044f \u0432\u0438\u0434\u043d\u0430 \u0432 \u043f\u0440\u043e\u0444\u0438\u043b\u0435 \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e."
                : "A personality trait shown in the worker profile.",
            WorkerPerkKind.Quicklearner => ru
                ? "\u0427\u0435\u0440\u0442\u0430 \u0445\u0430\u0440\u0430\u043a\u0442\u0435\u0440\u0430, \u043a\u043e\u0442\u043e\u0440\u0430\u044f \u0432\u0438\u0434\u043d\u0430 \u0432 \u043f\u0440\u043e\u0444\u0438\u043b\u0435 \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e."
                : "A personality trait shown in the worker profile.",
            _ => ru ? "\u041f\u043e\u0441\u0442\u043e\u044f\u043d\u043d\u0430\u044f \u0447\u0435\u0440\u0442\u0430 \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e." : "A permanent worker trait."
        };
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
