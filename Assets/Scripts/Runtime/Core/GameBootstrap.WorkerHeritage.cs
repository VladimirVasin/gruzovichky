using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerHeritageMaxThoughtIntensityDelta = 10;
    private const int WorkerHeritageMaxKnowledgeScoreDelta = 8;
    private const int WorkerHeritageMaxKnowledgeConfidenceDelta = 10;
    private const int WorkerHeritageMaxSocialStrengthDelta = 6;
    private const int WorkerHeritageMaxSocialConfidenceDelta = 6;
    private const float WorkerHeritageMinFormationMultiplier = 0.9f;

    private static readonly WorkerHeritageKind[] WorkerHeritageCatalog =
    {
        WorkerHeritageKind.Rovian,
        WorkerHeritageKind.Zelen,
        WorkerHeritageKind.Iskrian
    };

    private static void AssignWorkerHeritageFromRace(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        driver.Heritage = ConvertWorkerRaceToHeritage(driver.Race);
        driver.HasAssignedHeritage = true;
    }

    private static void EnsureWorkerHeritage(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        EnsureWorkerRace(driver);
        if (driver.HasAssignedHeritage)
        {
            return;
        }

        AssignWorkerHeritageFromRace(driver);
    }

    private static WorkerHeritageKind GetWorkerHeritage(DriverAgent driver)
    {
        EnsureWorkerHeritage(driver);
        return driver?.Heritage ?? WorkerHeritageKind.Rovian;
    }

    private static WorkerHeritageKind ConvertWorkerRaceToHeritage(WorkerRaceKind race)
    {
        return race switch
        {
            WorkerRaceKind.Zelen => WorkerHeritageKind.Zelen,
            WorkerRaceKind.Iskrian => WorkerHeritageKind.Iskrian,
            _ => WorkerHeritageKind.Rovian
        };
    }

    private static string GetWorkerHeritageDisplayName(WorkerHeritageKind heritage, bool ru)
    {
        return heritage switch
        {
            WorkerHeritageKind.Rovian => ru ? "\u0420\u043e\u0432\u044f\u043d\u0438\u043d" : "Rovian",
            WorkerHeritageKind.Zelen => ru ? "\u0417\u0435\u043b\u0435\u043d\u0435\u0446" : "Zelen",
            WorkerHeritageKind.Iskrian => ru ? "\u0418\u0441\u043a\u0440\u044f\u043d\u0438\u043d" : "Iskrian",
            _ => ru ? "\u041d\u0430\u0440\u043e\u0434" : "Heritage"
        };
    }

    private static string GetWorkerHeritagePeopleName(WorkerHeritageKind heritage, bool ru)
    {
        return heritage switch
        {
            WorkerHeritageKind.Rovian => ru ? "\u0420\u043e\u0432\u044f\u043d\u0435" : "Rovians",
            WorkerHeritageKind.Zelen => ru ? "\u0417\u0435\u043b\u0435\u043d\u0446\u044b" : "Zelens",
            WorkerHeritageKind.Iskrian => ru ? "\u0418\u0441\u043a\u0440\u044f\u043d\u0435" : "Iskrians",
            _ => ru ? "\u041d\u0430\u0440\u043e\u0434" : "Heritage"
        };
    }

    private static string GetWorkerHeritageShortDescription(WorkerHeritageKind heritage, bool ru)
    {
        return heritage switch
        {
            WorkerHeritageKind.Rovian => ru
                ? "\u0437\u0430\u043c\u0435\u0447\u0430\u0435\u0442 \u0440\u0430\u0431\u043e\u0442\u0443, \u0434\u043e\u0440\u043e\u0433\u0438, \u0434\u0435\u043d\u044c\u0433\u0438 \u0438 \u043f\u0440\u0435\u0434\u0441\u043a\u0430\u0437\u0443\u0435\u043c\u043e\u0441\u0442\u044c"
                : "notices work, roads, money, and predictability",
            WorkerHeritageKind.Zelen => ru
                ? "\u0441\u0438\u043b\u044c\u043d\u0435\u0435 \u0447\u0443\u0432\u0441\u0442\u0432\u0443\u0435\u0442 \u0434\u043e\u043c, \u0441\u0435\u043c\u044c\u044e, \u0434\u0435\u0442\u0435\u0439, \u0437\u0435\u043b\u0435\u043d\u044c \u0438 \u0447\u0438\u0441\u0442\u043e\u0442\u0443"
                : "feels home, family, children, greenery, and cleanliness more strongly",
            WorkerHeritageKind.Iskrian => ru
                ? "\u0431\u044b\u0441\u0442\u0440\u0435\u0435 \u0446\u0435\u043f\u043b\u044f\u0435\u0442 \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u044b, \u0437\u043d\u0430\u043d\u0438\u044f, \u0441\u043b\u0443\u0445\u0438 \u0438 \u043e\u0431\u0449\u0438\u0439 \u0441\u043c\u044b\u0441\u043b"
                : "catches conversation, knowledge, rumors, and public meaning faster",
            _ => ru ? "\u043a\u0443\u043b\u044c\u0442\u0443\u0440\u043d\u0430\u044f \u043e\u043f\u0442\u0438\u043a\u0430" : "cultural lens"
        };
    }

    private static string GetWorkerHeritageDescription(WorkerHeritageKind heritage, bool ru)
    {
        return heritage switch
        {
            WorkerHeritageKind.Rovian => ru
                ? "\u0417\u0430\u043c\u0435\u0447\u0430\u0435\u0442 \u0440\u0430\u0431\u043e\u0442\u0443, \u0434\u043e\u0440\u043e\u0433\u0438, \u0434\u0435\u043d\u044c\u0433\u0438 \u0438 \u043f\u0440\u0435\u0434\u0441\u043a\u0430\u0437\u0443\u0435\u043c\u043e\u0441\u0442\u044c. \u0413\u043e\u0440\u043e\u0434 \u0434\u043b\u044f \u043d\u0435\u0433\u043e \u043f\u043e\u043d\u044f\u0442\u0435\u043d, \u0435\u0441\u043b\u0438 \u043f\u0443\u0442\u044c, \u0437\u0430\u0440\u0430\u0431\u043e\u0442\u043e\u043a \u0438 \u043f\u0440\u0430\u0432\u0438\u043b\u0430 \u0434\u0435\u0440\u0436\u0430\u0442\u0441\u044f \u0440\u043e\u0432\u043d\u043e."
                : "Notices work, roads, money, and predictability. The town makes sense when paths, pay, and rules hold steady.",
            WorkerHeritageKind.Zelen => ru
                ? "\u0421\u0438\u043b\u044c\u043d\u0435\u0435 \u0447\u0443\u0432\u0441\u0442\u0432\u0443\u0435\u0442 \u0434\u043e\u043c, \u0441\u0435\u043c\u044c\u044e, \u0434\u0435\u0442\u0435\u0439, \u0437\u0435\u043b\u0435\u043d\u044c \u0438 \u0447\u0438\u0441\u0442\u043e\u0442\u0443. \u0413\u043e\u0440\u043e\u0434 \u0434\u043b\u044f \u043d\u0435\u0433\u043e \u0436\u0438\u0432\u043e\u0439, \u0435\u0441\u043b\u0438 \u0432 \u043d\u0451\u043c \u043c\u043e\u0436\u043d\u043e \u0441\u043f\u043e\u043a\u043e\u0439\u043d\u043e \u043e\u0431\u0436\u0438\u0442\u044c\u0441\u044f."
                : "Feels home, family, children, greenery, and cleanliness more strongly. The town feels alive when people can settle calmly.",
            WorkerHeritageKind.Iskrian => ru
                ? "\u0411\u044b\u0441\u0442\u0440\u0435\u0435 \u0446\u0435\u043f\u043b\u044f\u0435\u0442 \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u044b, \u0437\u043d\u0430\u043d\u0438\u044f, \u0441\u043b\u0443\u0445\u0438 \u0438 \u043e\u0431\u0449\u0435\u0441\u0442\u0432\u0435\u043d\u043d\u044b\u0439 \u0441\u043c\u044b\u0441\u043b. \u0413\u043e\u0440\u043e\u0434 \u0434\u043b\u044f \u043d\u0435\u0433\u043e \u0433\u043e\u0432\u043e\u0440\u0438\u0442 \u0447\u0435\u0440\u0435\u0437 \u043b\u044e\u0434\u0435\u0439 \u0438 \u043d\u043e\u043e\u0441\u0444\u0435\u0440\u0443."
                : "Catches conversation, knowledge, rumors, and public meaning faster. The town speaks through people and the Noosphere.",
            _ => ru ? "\u041a\u0443\u043b\u044c\u0442\u0443\u0440\u043d\u0430\u044f \u043e\u043f\u0442\u0438\u043a\u0430, \u0430 \u043d\u0435 \u0440\u0430\u0431\u043e\u0447\u0438\u0439 \u0431\u043e\u043d\u0443\u0441." : "A cultural lens, not a work bonus."
        };
    }

    private static Color GetWorkerHeritageColor(WorkerHeritageKind heritage)
    {
        return heritage switch
        {
            WorkerHeritageKind.Rovian => new Color(0.46f, 0.66f, 0.86f, 1f),
            WorkerHeritageKind.Zelen => new Color(0.46f, 0.78f, 0.46f, 1f),
            WorkerHeritageKind.Iskrian => new Color(1f, 0.72f, 0.26f, 1f),
            _ => FleetSecondaryTextColor
        };
    }

    private static Color GetWorkerHeritageAccentColor(WorkerHeritageKind heritage)
    {
        return heritage switch
        {
            WorkerHeritageKind.Rovian => new Color(1f, 0.82f, 0.28f, 1f),
            WorkerHeritageKind.Zelen => new Color(0.72f, 0.48f, 0.30f, 1f),
            WorkerHeritageKind.Iskrian => new Color(0.42f, 0.82f, 0.86f, 1f),
            _ => FleetAccentColor
        };
    }

    private static string GetWorkerHeritageIconGlyph(WorkerHeritageKind heritage)
    {
        return heritage switch
        {
            WorkerHeritageKind.Rovian => "\u21c4",
            WorkerHeritageKind.Zelen => "\u2302",
            WorkerHeritageKind.Iskrian => "\u2726",
            _ => "\u25cf"
        };
    }

    private static string FormatWorkerHeritageBadgeInline(WorkerHeritageKind heritage, bool ru, bool includeName = true)
    {
        string color = ColorUtility.ToHtmlStringRGB(GetWorkerHeritageColor(heritage));
        string accent = ColorUtility.ToHtmlStringRGB(GetWorkerHeritageAccentColor(heritage));
        string glyph = GetWorkerHeritageIconGlyph(heritage);
        string name = includeName ? $" {GetWorkerHeritageDisplayName(heritage, ru)}" : string.Empty;
        return $"<color=#{accent}>{glyph}</color><color=#{color}>{name}</color>";
    }

    private static string FormatWorkerHeritageListBadge(DriverAgent driver, bool ru)
    {
        return driver == null ? "-" : FormatWorkerHeritageBadgeInline(GetWorkerHeritage(driver), ru);
    }

    private static void ApplyWorkerHeritageThoughtBias(
        DriverAgent worker,
        string thoughtKey,
        WorkerThoughtKind kind,
        WorkerThoughtSubjectType subjectType,
        string subjectKey,
        ref int intensity,
        ref WorkerThoughtPriority priority,
        ref float formationHours,
        ref int opinionDelta)
    {
        if (worker == null)
        {
            return;
        }

        int delta = GetWorkerHeritageThoughtIntensityDelta(worker, thoughtKey, kind, subjectType, subjectKey);
        if (delta <= 0)
        {
            return;
        }

        intensity = Mathf.Clamp(intensity + Mathf.Min(delta, WorkerHeritageMaxThoughtIntensityDelta), 0, 100);
        if (formationHours > 0f)
        {
            formationHours = Mathf.Max(WorkerThoughtInfluenceMinFormationHours, formationHours * WorkerHeritageMinFormationMultiplier);
        }

        if (opinionDelta != 0)
        {
            opinionDelta = Mathf.Clamp(opinionDelta + (opinionDelta > 0 ? 1 : -1), -100, 100);
        }
    }

    private static int GetWorkerHeritageThoughtIntensityDelta(
        DriverAgent worker,
        string thoughtKey,
        WorkerThoughtKind kind,
        WorkerThoughtSubjectType subjectType,
        string subjectKey)
    {
        if (worker == null)
        {
            return 0;
        }

        WorkerHeritageKind heritage = GetWorkerHeritage(worker);
        string key = thoughtKey ?? string.Empty;
        string subject = subjectKey ?? string.Empty;

        return heritage switch
        {
            WorkerHeritageKind.Rovian when IsRovianHeritageThought(key, kind, subjectType, subject) => 7,
            WorkerHeritageKind.Zelen when IsZelenHeritageThought(key, kind, subjectType, subject) => 7,
            WorkerHeritageKind.Iskrian when IsIskrianHeritageThought(key, kind, subjectType, subject) => 8,
            _ => 0
        };
    }

    private static bool IsRovianHeritageThought(string key, WorkerThoughtKind kind, WorkerThoughtSubjectType subjectType, string subject)
    {
        return kind is WorkerThoughtKind.Work or WorkerThoughtKind.Money or WorkerThoughtKind.Transport ||
               key is "no_job_warning" or "job_found" or "salary_paid" or "service_unaffordable" or
                   "bus_chosen" or "bus_unavailable" or "stable_life" or "affect_financial_pressure" ||
               IsWorkerHeritageSubject(subject, "LaborExchange", "Warehouse", "Docks", "GasStation", "Stop", "IntercityStop", "city_work", "money");
    }

    private static bool IsZelenHeritageThought(string key, WorkerThoughtKind kind, WorkerThoughtSubjectType subjectType, string subject)
    {
        return kind is WorkerThoughtKind.Family ||
               key is "house_bought" or "family_formed" or "child_born" or "affect_family_anxiety" or
                   "street_litter_low" or "street_litter_medium" or "street_litter_high" or "affect_litter_irritation" ||
               key == "leisure_service_good" && subject == LocationType.CityPark.ToString() ||
               IsWorkerHeritageSubject(subject, "PersonalHouse", "CityPark", "Kindergarten", "PrimarySchool", "SecondarySchool", "street_litter");
    }

    private static bool IsIskrianHeritageThought(string key, WorkerThoughtKind kind, WorkerThoughtSubjectType subjectType, string subject)
    {
        return kind == WorkerThoughtKind.Social ||
               key is "social_talk_good" or "social_shared_place" or "social_learned_new_topic" or
                   "knowledge_reflection_building" or "worker_arrived" ||
               subjectType == WorkerThoughtSubjectType.Text && IsWorkerHeritageSubject(subject, "topic", "knowledge", "noosphere", "city_meaning");
    }

    private static float ApplyWorkerHeritageKnowledgeFormationBias(
        DriverAgent worker,
        WorkerMemoryKind kind,
        LocationType? buildingType,
        string topicKey,
        float hours)
    {
        if (worker == null)
        {
            return hours;
        }

        if (!DoesWorkerHeritageMatchKnowledge(worker, kind, buildingType, topicKey))
        {
            return hours;
        }

        return Mathf.Max(0.18f, hours * WorkerHeritageMinFormationMultiplier);
    }

    private static void ApplyWorkerHeritageKnowledgeBias(
        DriverAgent worker,
        PendingWorkerKnowledge pending,
        ref int score,
        ref int confidence,
        ref string reasonRu,
        ref string reasonEn)
    {
        if (worker == null)
        {
            return;
        }

        if (!DoesWorkerHeritageMatchKnowledge(worker, pending?.Kind ?? WorkerMemoryKind.ConversationTopic, pending?.BuildingType, GetPendingWorkerHeritageTopicKey(pending)))
        {
            return;
        }

        WorkerHeritageKind heritage = GetWorkerHeritage(worker);
        int scoreDelta = pending?.Kind == WorkerMemoryKind.BuildingExistence ? 3 : heritage == WorkerHeritageKind.Iskrian ? 6 : 4;
        int confidenceDelta = heritage == WorkerHeritageKind.Iskrian ? 8 : 6;
        score += Mathf.Min(scoreDelta, WorkerHeritageMaxKnowledgeScoreDelta);
        confidence += Mathf.Min(confidenceDelta, WorkerHeritageMaxKnowledgeConfidenceDelta);
        reasonRu = AppendWorkerHeritageReason(reasonRu, heritage, true);
        reasonEn = AppendWorkerHeritageReason(reasonEn, heritage, false);
    }

    private static bool DoesWorkerHeritageMatchKnowledge(DriverAgent worker, WorkerMemoryKind kind, LocationType? buildingType, string topicKey)
    {
        WorkerHeritageKind heritage = GetWorkerHeritage(worker);
        string topic = topicKey ?? string.Empty;
        return heritage switch
        {
            WorkerHeritageKind.Rovian =>
                buildingType is LocationType.LaborExchange or LocationType.Warehouse or LocationType.Docks or LocationType.GasStation or LocationType.Stop or LocationType.IntercityStop ||
                IsWorkerHeritageSubject(topic, "work", "money", "transport", "bus", "warehouse", "docks", "labor", "gas"),
            WorkerHeritageKind.Zelen =>
                buildingType is LocationType.PersonalHouse or LocationType.CityPark or LocationType.Kindergarten or LocationType.PrimarySchool or LocationType.SecondarySchool ||
                IsWorkerHeritageSubject(topic, "home", "family", "child", "children", "park", "nature", "litter", "clean"),
            WorkerHeritageKind.Iskrian =>
                kind == WorkerMemoryKind.ConversationTopic ||
                buildingType == LocationType.CityHall ||
                IsWorkerHeritageSubject(topic, "social", "knowledge", "topic", "rumor", "noosphere", "meaning", "cityhall"),
            _ => false
        };
    }

    private static string GetPendingWorkerHeritageTopicKey(PendingWorkerKnowledge pending)
    {
        if (pending == null)
        {
            return string.Empty;
        }

        return $"{pending.ConversationTopicKey} {pending.Topic} {pending.OriginalTopic} {pending.RumorTopic} {pending.SourceEn}";
    }

    private static string GetWorkerMemoryHeritageTopicKey(WorkerMemory memory)
    {
        if (memory == null)
        {
            return string.Empty;
        }

        return $"{memory.ConversationTopicKey} {memory.Topic} {memory.OriginalTopic} {memory.RumorTopic} {memory.SourceEn}";
    }

    private static string AppendWorkerHeritageReason(string reason, WorkerHeritageKind heritage, bool ru)
    {
        string suffix = heritage switch
        {
            WorkerHeritageKind.Rovian => ru ? "\u0420\u043e\u0432\u044f\u043d\u0441\u043a\u0430\u044f \u043e\u043f\u0442\u0438\u043a\u0430 \u0434\u0435\u0440\u0436\u0438\u0442 \u0432 \u0444\u043e\u043a\u0443\u0441\u0435 \u0442\u0440\u0443\u0434, \u043f\u0443\u0442\u044c \u0438 \u043e\u0431\u043c\u0435\u043d." : "A Rovian lens keeps work, paths, and exchange in focus.",
            WorkerHeritageKind.Zelen => ru ? "\u0417\u0435\u043b\u0435\u043d\u0441\u043a\u0430\u044f \u043e\u043f\u0442\u0438\u043a\u0430 \u0434\u0435\u0440\u0436\u0438\u0442 \u0432 \u0444\u043e\u043a\u0443\u0441\u0435 \u0434\u043e\u043c, \u0441\u0435\u043c\u044c\u044e \u0438 \u0443\u0445\u043e\u0434." : "A Zelen lens keeps home, family, and care in focus.",
            WorkerHeritageKind.Iskrian => ru ? "\u0418\u0441\u043a\u0440\u044f\u043d\u0441\u043a\u0430\u044f \u043e\u043f\u0442\u0438\u043a\u0430 \u0434\u0435\u0440\u0436\u0438\u0442 \u0432 \u0444\u043e\u043a\u0443\u0441\u0435 \u0440\u0430\u0437\u0433\u043e\u0432\u043e\u0440\u044b \u0438 \u0441\u043c\u044b\u0441\u043b." : "An Iskrian lens keeps conversation and meaning in focus.",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(suffix))
        {
            return reason ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return suffix;
        }

        return $"{reason} {suffix}";
    }

    private static void ApplyWorkerHeritageSocialSignalBias(
        DriverAgent worker,
        SocialSignalCategory category,
        string topicKey,
        ref int strength,
        ref int confidence)
    {
        if (worker == null)
        {
            return;
        }

        WorkerHeritageKind heritage = GetWorkerHeritage(worker);
        int strengthDelta = GetWorkerHeritageSocialSignalStrengthDelta(heritage, category, topicKey);
        if (strengthDelta == 0)
        {
            return;
        }

        int confidenceDelta = heritage == WorkerHeritageKind.Iskrian ? 6 : 4;
        strength += Mathf.Min(strengthDelta, WorkerHeritageMaxSocialStrengthDelta);
        confidence += Mathf.Min(confidenceDelta, WorkerHeritageMaxSocialConfidenceDelta);
    }

    private static int GetWorkerHeritageSocialSignalStrengthDelta(WorkerHeritageKind heritage, SocialSignalCategory category, string topicKey)
    {
        return heritage switch
        {
            WorkerHeritageKind.Rovian when category is SocialSignalCategory.Work or SocialSignalCategory.Money or SocialSignalCategory.Transport => 5,
            WorkerHeritageKind.Zelen when category is SocialSignalCategory.Family or SocialSignalCategory.Housing or SocialSignalCategory.Litter or SocialSignalCategory.Need => 5,
            WorkerHeritageKind.Iskrian when category is SocialSignalCategory.Social or SocialSignalCategory.Knowledge or SocialSignalCategory.Topic or SocialSignalCategory.City => 6,
            WorkerHeritageKind.Zelen when IsWorkerHeritageSubject(topicKey, "park", "home", "family", "litter", "clean") => 5,
            WorkerHeritageKind.Rovian when IsWorkerHeritageSubject(topicKey, "work", "money", "transport", "bus", "road") => 5,
            WorkerHeritageKind.Iskrian when IsWorkerHeritageSubject(topicKey, "topic", "knowledge", "rumor", "noosphere", "social") => 6,
            _ => 0
        };
    }

    private static bool IsWorkerHeritageSubject(string value, params string[] needles)
    {
        if (string.IsNullOrWhiteSpace(value) || needles == null)
        {
            return false;
        }

        for (int i = 0; i < needles.Length; i++)
        {
            string needle = needles[i];
            if (!string.IsNullOrWhiteSpace(needle) &&
                value.IndexOf(needle, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
