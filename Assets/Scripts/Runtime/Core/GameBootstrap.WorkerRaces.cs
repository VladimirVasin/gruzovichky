using UnityEngine;

public partial class GameBootstrap
{
    private void AssignWorkerRace(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        driver.Race = PickWorkerRace(driver.DriverId, driver.DriverName);
        driver.HasAssignedRace = true;
    }

    private static void EnsureWorkerRace(DriverAgent driver)
    {
        if (driver == null || driver.HasAssignedRace)
        {
            return;
        }

        driver.Race = PickWorkerRace(driver.DriverId, driver.DriverName);
        driver.HasAssignedRace = true;
    }

    private static WorkerRaceKind PickWorkerRace(int workerId, string workerName)
    {
        unchecked
        {
            int hash = StableWorkerTraitHash(workerName) ^ (workerId * 83492791);
            uint stable = (uint)hash;
            return (stable % 3u) switch
            {
                0u => WorkerRaceKind.Rovian,
                1u => WorkerRaceKind.Zelen,
                _ => WorkerRaceKind.Iskrian
            };
        }
    }

    private static bool HasWorkerRace(DriverAgent driver, WorkerRaceKind race)
    {
        EnsureWorkerRace(driver);
        return driver != null && driver.Race == race;
    }

    private static string GetWorkerRaceDisplayName(WorkerRaceKind race, bool ru)
    {
        return race switch
        {
            WorkerRaceKind.Rovian => ru ? "\u0420\u043e\u0432\u044f\u043d\u0435" : "Rovians",
            WorkerRaceKind.Zelen => ru ? "\u0417\u0435\u043b\u0435\u043d\u0446\u044b" : "Zelens",
            WorkerRaceKind.Iskrian => ru ? "\u0418\u0441\u043a\u0440\u044f\u043d\u0435" : "Iskrians",
            _ => ru ? "\u0420\u0430\u0441\u0430" : "Race"
        };
    }

    private static string GetWorkerRaceShortDescription(WorkerRaceKind race, bool ru)
    {
        return race switch
        {
            WorkerRaceKind.Rovian => ru
                ? "\u041b\u044e\u0434\u0438 \u0434\u043e\u0440\u043e\u0433\u0438, \u0442\u0440\u0443\u0434\u0430 \u0438 \u043e\u0431\u043c\u0435\u043d\u0430. \u041e\u0441\u0442\u0440\u0435\u0435 \u0437\u0430\u043c\u0435\u0447\u0430\u044e\u0442 \u0440\u0430\u0431\u043e\u0442\u0443, \u0434\u0435\u043d\u044c\u0433\u0438 \u0438 \u0442\u0440\u0430\u043d\u0441\u043f\u043e\u0440\u0442."
                : "People of roads, work, and exchange. They notice work, money, and transport problems sooner.",
            WorkerRaceKind.Zelen => ru
                ? "\u041b\u044e\u0434\u0438 \u0434\u043e\u043c\u0430, \u0437\u0435\u043c\u043b\u0438 \u0438 \u0443\u0445\u043e\u0434\u0430. \u041e\u0441\u0442\u0440\u0435\u0435 \u0437\u0430\u043c\u0435\u0447\u0430\u044e\u0442 \u0447\u0438\u0441\u0442\u043e\u0442\u0443, \u0436\u0438\u043b\u044c\u0435, \u0434\u0435\u0442\u0435\u0439 \u0438 \u0440\u0430\u0439\u043e\u043d."
                : "People of home, land, and care. They notice cleanliness, housing, children, and neighborhood comfort sooner.",
            WorkerRaceKind.Iskrian => ru
                ? "\u041b\u044e\u0434\u0438 \u0441\u043c\u044b\u0441\u043b\u0430, \u043f\u0430\u043c\u044f\u0442\u0438 \u0438 \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u043e\u0432. \u041e\u0441\u0442\u0440\u0435\u0435 \u0447\u0443\u0432\u0441\u0442\u0432\u0443\u044e\u0442 \u0441\u0432\u044f\u0437\u0438 \u0438 \u043d\u0430\u0441\u0442\u0440\u043e\u0435\u043d\u0438\u0435 \u0433\u043e\u0440\u043e\u0434\u0430."
                : "People of meaning, memory, and conversation. They sense social ties and the city's mood sooner.",
            _ => ru ? "\u0427\u0430\u0441\u0442\u044c \u043b\u0438\u0447\u043d\u043e\u0441\u0442\u0438 \u0436\u0438\u0442\u0435\u043b\u044f." : "Part of resident identity."
        };
    }

    private static string GetWorkerRaceFullDescription(WorkerRaceKind race, bool ru)
    {
        return race switch
        {
            WorkerRaceKind.Rovian => ru
                ? "\u0420\u043e\u0432\u044f\u043d\u0435 \u0441\u043c\u043e\u0442\u0440\u044f\u0442 \u043d\u0430 \u0433\u043e\u0440\u043e\u0434 \u0447\u0435\u0440\u0435\u0437 \u0434\u043e\u0440\u043e\u0433\u0438, \u0442\u0440\u0443\u0434, \u0440\u0430\u0441\u043f\u0438\u0441\u0430\u043d\u0438\u0435 \u0438 \u043e\u0431\u043c\u0435\u043d. \u0412 \u044d\u0442\u043e\u043c \u0441\u043b\u043e\u0435 \u044d\u0442\u043e \u043d\u0435 \u0431\u043e\u043d\u0443\u0441, \u0430 \u0447\u0430\u0441\u0442\u044c \u0438\u0434\u0435\u043d\u0442\u0438\u0447\u043d\u043e\u0441\u0442\u0438 \u0436\u0438\u0442\u0435\u043b\u044f."
                : "Rovians read the town through roads, work, schedules, and exchange. In this layer race is identity, not a work bonus.",
            WorkerRaceKind.Zelen => ru
                ? "\u0417\u0435\u043b\u0435\u043d\u0446\u044b \u0441\u043c\u043e\u0442\u0440\u044f\u0442 \u043d\u0430 \u0433\u043e\u0440\u043e\u0434 \u0447\u0435\u0440\u0435\u0437 \u0434\u043e\u043c, \u0437\u0435\u043c\u043b\u044e, \u0441\u0435\u043c\u044c\u044e \u0438 \u0443\u0445\u043e\u0434. \u0412 \u044d\u0442\u043e\u043c \u0441\u043b\u043e\u0435 \u044d\u0442\u043e \u043d\u0435 \u0431\u043e\u043d\u0443\u0441, \u0430 \u0447\u0430\u0441\u0442\u044c \u0438\u0434\u0435\u043d\u0442\u0438\u0447\u043d\u043e\u0441\u0442\u0438 \u0436\u0438\u0442\u0435\u043b\u044f."
                : "Zelens read the town through home, land, family, and care. In this layer race is identity, not a work bonus.",
            WorkerRaceKind.Iskrian => ru
                ? "\u0418\u0441\u043a\u0440\u044f\u043d\u0435 \u0441\u043c\u043e\u0442\u0440\u044f\u0442 \u043d\u0430 \u0433\u043e\u0440\u043e\u0434 \u0447\u0435\u0440\u0435\u0437 \u0441\u043c\u044b\u0441\u043b\u044b, \u043f\u0430\u043c\u044f\u0442\u044c, \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u044b \u0438 \u0434\u043e\u0432\u0435\u0440\u0438\u0435. \u0412 \u044d\u0442\u043e\u043c \u0441\u043b\u043e\u0435 \u044d\u0442\u043e \u043d\u0435 \u0431\u043e\u043d\u0443\u0441, \u0430 \u0447\u0430\u0441\u0442\u044c \u0438\u0434\u0435\u043d\u0442\u0438\u0447\u043d\u043e\u0441\u0442\u0438 \u0436\u0438\u0442\u0435\u043b\u044f."
                : "Iskrians read the town through meaning, memory, conversations, and trust. In this layer race is identity, not a work bonus.",
            _ => ru ? "\u041d\u0435\u0439\u0442\u0440\u0430\u043b\u044c\u043d\u0430\u044f \u0447\u0430\u0441\u0442\u044c \u043b\u0438\u0447\u043d\u043e\u0441\u0442\u0438 \u0436\u0438\u0442\u0435\u043b\u044f." : "A neutral part of resident identity."
        };
    }

    private static Color GetWorkerRaceColor(WorkerRaceKind race)
    {
        return race switch
        {
            WorkerRaceKind.Rovian => new Color(0.46f, 0.66f, 0.86f, 1f),
            WorkerRaceKind.Zelen => new Color(0.46f, 0.78f, 0.46f, 1f),
            WorkerRaceKind.Iskrian => new Color(1f, 0.72f, 0.26f, 1f),
            _ => FleetSecondaryTextColor
        };
    }

    private static Color GetWorkerRaceAccentColor(WorkerRaceKind race)
    {
        return race switch
        {
            WorkerRaceKind.Rovian => new Color(1f, 0.82f, 0.28f, 1f),
            WorkerRaceKind.Zelen => new Color(0.72f, 0.48f, 0.30f, 1f),
            WorkerRaceKind.Iskrian => new Color(0.42f, 0.82f, 0.86f, 1f),
            _ => FleetAccentColor
        };
    }

    private static string GetWorkerRaceIconKey(WorkerRaceKind race)
    {
        return race switch
        {
            WorkerRaceKind.Rovian => "race_rovian_route",
            WorkerRaceKind.Zelen => "race_zelen_leaf",
            WorkerRaceKind.Iskrian => "race_iskrian_spark",
            _ => "race_unknown"
        };
    }

    private static string GetWorkerRaceIconGlyph(WorkerRaceKind race)
    {
        return race switch
        {
            WorkerRaceKind.Rovian => "\u21c4",
            WorkerRaceKind.Zelen => "\u2302",
            WorkerRaceKind.Iskrian => "\u2726",
            _ => "\u25cf"
        };
    }

    private static string FormatWorkerRaceBadgeInline(WorkerRaceKind race, bool ru, bool includeName = true)
    {
        string color = ColorUtility.ToHtmlStringRGB(GetWorkerRaceColor(race));
        string accent = ColorUtility.ToHtmlStringRGB(GetWorkerRaceAccentColor(race));
        string glyph = GetWorkerRaceIconGlyph(race);
        string name = includeName ? $" {GetWorkerRaceDisplayName(race, ru)}" : string.Empty;
        return $"<color=#{accent}>{glyph}</color><color=#{color}>{name}</color>";
    }

    private static string FormatWorkerRaceListBadge(DriverAgent driver, bool ru)
    {
        EnsureWorkerRace(driver);
        return driver == null ? "-" : FormatWorkerRaceBadgeInline(driver.Race, ru);
    }

}
