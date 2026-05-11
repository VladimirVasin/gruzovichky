using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int CityDailyExperienceHistoryCap = 14;

    private readonly List<CityDailyExperience> cityDailyExperiences = new();

    private sealed class CityDailyExperience
    {
        public int Day;
        public WorkerDailyOpinionTone FinalTone;
        public int Score;
        public int Confidence;
        public int Consensus;
        public int Tension;
        public int ResidentCount;
        public int PositiveResidentCount;
        public int NegativeResidentCount;
        public WorkerDailyOpinionFactorKind DominantKind = WorkerDailyOpinionFactorKind.City;
        public string SummaryRu = string.Empty;
        public string SummaryEn = string.Empty;
        public string MainReasonRu = string.Empty;
        public string MainReasonEn = string.Empty;
        public string CounterpointRu = string.Empty;
        public string CounterpointEn = string.Empty;
        public float CreatedWorldHour;
        public readonly List<CityDailyExperienceFactor> Factors = new();
    }

    private sealed class CityDailyExperienceFactor
    {
        public WorkerDailyOpinionFactorKind Kind;
        public int Score;
        public int ResidentCount;
        public int PositiveCount;
        public int NegativeCount;
        public int RepresentativeScore;
        public string RepresentativeReasonRu = string.Empty;
        public string RepresentativeReasonEn = string.Empty;
    }

    private void FinalizeCityDailyExperienceForDay(int endedDay)
    {
        if (endedDay <= 0 || HasCityDailyExperienceForDay(endedDay))
        {
            return;
        }

        CityDailyExperience experience = BuildCityDailyExperience(endedDay);
        if (experience == null)
        {
            return;
        }

        cityDailyExperiences.Insert(0, experience);
        while (cityDailyExperiences.Count > CityDailyExperienceHistoryCap)
        {
            cityDailyExperiences.RemoveAt(cityDailyExperiences.Count - 1);
        }

        isNoosphereScreenDirty = true;
        noosphereVisualDirty = true;
        SessionDebugLogger.Log(
            "CITY_EXPERIENCE",
            $"City day {endedDay}: tone={experience.FinalTone}, score={experience.Score}, confidence={experience.Confidence}, consensus={experience.Consensus}, tension={experience.Tension}, residents={experience.ResidentCount}, main={experience.MainReasonEn}.");
    }

    private CityDailyExperience BuildCityDailyExperience(int day)
    {
        List<CityDailyExperienceFactor> factors = new();
        int residentCount = 0;
        int positiveResidents = 0;
        int negativeResidents = 0;
        int weightedScoreTotal = 0;
        int weightTotal = 0;
        int confidenceTotal = 0;

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (worker == null || worker.HasDepartedTown || worker.IsLeavingTown)
            {
                continue;
            }

            WorkerDailyOpinion opinion = FindWorkerDailyOpinionForDay(worker, day);
            if (opinion == null)
            {
                continue;
            }

            residentCount++;
            if (opinion.FinalTone == WorkerDailyOpinionTone.Positive)
            {
                positiveResidents++;
            }
            else
            {
                negativeResidents++;
            }

            int weight = Mathf.Clamp(opinion.Confidence, 1, 100);
            weightedScoreTotal += opinion.Score * weight;
            weightTotal += weight;
            confidenceTotal += opinion.Confidence;
            AddCityDailyExperienceFactors(factors, opinion);
        }

        if (residentCount <= 0 || weightTotal <= 0)
        {
            return null;
        }

        int score = Mathf.Clamp(Mathf.RoundToInt(weightedScoreTotal / (float)weightTotal), -100, 100);
        WorkerDailyOpinionTone tone = score >= 0 ? WorkerDailyOpinionTone.Positive : WorkerDailyOpinionTone.Negative;
        int majorityCount = Mathf.Max(positiveResidents, negativeResidents);
        int consensus = Mathf.Clamp(Mathf.RoundToInt(majorityCount / (float)residentCount * 100f), 0, 100);
        int tension = Mathf.Clamp(Mathf.RoundToInt((1f - Mathf.Abs(positiveResidents - negativeResidents) / (float)residentCount) * 100f), 0, 100);
        CityDailyExperienceFactor main = FindStrongestCityDailyExperienceFactor(factors, tone == WorkerDailyOpinionTone.Positive, null) ??
                                         FindStrongestCityDailyExperienceFactor(factors, null, null);
        CityDailyExperienceFactor counterpoint = FindStrongestCityDailyExperienceFactor(factors, tone != WorkerDailyOpinionTone.Positive, main);

        CityDailyExperience experience = new()
        {
            Day = day,
            FinalTone = tone,
            Score = score,
            Confidence = CalculateCityDailyExperienceConfidence(confidenceTotal, residentCount, consensus, tension),
            Consensus = consensus,
            Tension = tension,
            ResidentCount = residentCount,
            PositiveResidentCount = positiveResidents,
            NegativeResidentCount = negativeResidents,
            DominantKind = main?.Kind ?? WorkerDailyOpinionFactorKind.City,
            SummaryRu = GetCityDailyExperienceSummary(score, tension, true),
            SummaryEn = GetCityDailyExperienceSummary(score, tension, false),
            MainReasonRu = BuildCityDailyExperienceFactorReason(main, true),
            MainReasonEn = BuildCityDailyExperienceFactorReason(main, false),
            CounterpointRu = BuildCityDailyExperienceFactorReason(counterpoint, true),
            CounterpointEn = BuildCityDailyExperienceFactorReason(counterpoint, false),
            CreatedWorldHour = GetCurrentWorldHour()
        };
        experience.Factors.AddRange(factors);
        return experience;
    }

    private static void AddCityDailyExperienceFactors(List<CityDailyExperienceFactor> factors, WorkerDailyOpinion opinion)
    {
        if (factors == null || opinion == null)
        {
            return;
        }

        for (int i = 0; i < opinion.Factors.Count; i++)
        {
            WorkerDailyOpinionFactor workerFactor = opinion.Factors[i];
            if (workerFactor == null || workerFactor.Score == 0)
            {
                continue;
            }

            CityDailyExperienceFactor factor = GetOrCreateCityDailyExperienceFactor(factors, workerFactor.Kind);
            factor.Score += workerFactor.Score;
            factor.ResidentCount++;
            if (workerFactor.Score >= 0)
            {
                factor.PositiveCount++;
            }
            else
            {
                factor.NegativeCount++;
            }

            if (Mathf.Abs(workerFactor.Score) > Mathf.Abs(factor.RepresentativeScore))
            {
                factor.RepresentativeScore = workerFactor.Score;
                factor.RepresentativeReasonRu = workerFactor.ReasonRu ?? string.Empty;
                factor.RepresentativeReasonEn = workerFactor.ReasonEn ?? string.Empty;
            }
        }
    }

    private static CityDailyExperienceFactor GetOrCreateCityDailyExperienceFactor(List<CityDailyExperienceFactor> factors, WorkerDailyOpinionFactorKind kind)
    {
        for (int i = 0; i < factors.Count; i++)
        {
            if (factors[i]?.Kind == kind)
            {
                return factors[i];
            }
        }

        CityDailyExperienceFactor factor = new() { Kind = kind };
        factors.Add(factor);
        return factor;
    }

    private static CityDailyExperienceFactor FindStrongestCityDailyExperienceFactor(
        List<CityDailyExperienceFactor> factors,
        bool? positive,
        CityDailyExperienceFactor except)
    {
        CityDailyExperienceFactor best = null;
        int bestMagnitude = -1;
        for (int i = 0; i < factors.Count; i++)
        {
            CityDailyExperienceFactor factor = factors[i];
            if (factor == null || factor == except)
            {
                continue;
            }

            if (positive.HasValue && (factor.Score >= 0) != positive.Value)
            {
                continue;
            }

            int magnitude = Mathf.Abs(factor.Score);
            if (magnitude > bestMagnitude)
            {
                best = factor;
                bestMagnitude = magnitude;
            }
        }

        return best;
    }

    private static int CalculateCityDailyExperienceConfidence(int confidenceTotal, int residentCount, int consensus, int tension)
    {
        if (residentCount <= 0)
        {
            return 0;
        }

        int averageConfidence = Mathf.RoundToInt(confidenceTotal / (float)residentCount);
        return Mathf.Clamp(averageConfidence + consensus / 5 - tension / 8, 25, 98);
    }

    private WorkerDailyOpinion FindWorkerDailyOpinionForDay(DriverAgent worker, int day)
    {
        if (worker == null)
        {
            return null;
        }

        for (int i = 0; i < worker.DailyOpinions.Count; i++)
        {
            WorkerDailyOpinion opinion = worker.DailyOpinions[i];
            if (opinion?.Day == day)
            {
                return opinion;
            }
        }

        return null;
    }

    private bool HasCityDailyExperienceForDay(int day)
    {
        for (int i = 0; i < cityDailyExperiences.Count; i++)
        {
            if (cityDailyExperiences[i]?.Day == day)
            {
                return true;
            }
        }

        return false;
    }

    private CityDailyExperience GetLatestCityDailyExperience()
    {
        return cityDailyExperiences.Count > 0 ? cityDailyExperiences[0] : null;
    }

    private string FormatLatestCityDailyExperienceNoosphereSummary(bool ru)
    {
        CityDailyExperience experience = GetLatestCityDailyExperience();
        if (experience == null)
        {
            return ru
                ? "Общий пережитый опыт города появится здесь после первой полуночи."
                : "The city's shared lived experience will appear here after the first midnight.";
        }

        string tone = FormatCityDailyExperienceToneLabel(experience.FinalTone, ru);
        string consensus = FormatCityDailyExperienceConsensusLabel(experience, ru);
        string score = $"{experience.Score:+#;-#;0}";
        string counterpoint = string.IsNullOrWhiteSpace(ru ? experience.CounterpointRu : experience.CounterpointEn)
            ? string.Empty
            : ru ? $" Противовес: {experience.CounterpointRu}." : $" Counterpoint: {experience.CounterpointEn}.";
        return ru
            ? $"Общий пережитый опыт: {tone}, оценка {score}, уверенность {experience.Confidence}%. Город {consensus}. Почему: {experience.MainReasonRu}.{counterpoint}"
            : $"Shared lived experience: {tone}, score {score}, confidence {experience.Confidence}%. The city is {consensus}. Why: {experience.MainReasonEn}.{counterpoint}";
    }

    private string FormatNoosphereVisualCityExperienceStat(bool ru)
    {
        CityDailyExperience experience = GetLatestCityDailyExperience();
        if (experience == null)
        {
            return string.Empty;
        }

        string score = $"{experience.Score:+#;-#;0}";
        return ru ? $" / опыт {score}" : $" / exp {score}";
    }

    private Color GetNoosphereCityExperienceCoreColor()
    {
        CityDailyExperience experience = GetLatestCityDailyExperience();
        Color neutral = new(0.60f, 0.86f, 1f, 1f);
        if (experience == null)
        {
            return neutral;
        }

        Color toneColor = experience.FinalTone == WorkerDailyOpinionTone.Positive
            ? new Color(0.78f, 1f, 0.36f, 1f)
            : new Color(1f, 0.34f, 0.24f, 1f);
        if (experience.Tension >= 65)
        {
            toneColor = Color.Lerp(toneColor, new Color(1f, 0.78f, 0.24f, 1f), 0.35f);
        }

        float strength = Mathf.Clamp01((Mathf.Abs(experience.Score) + experience.Confidence * 0.35f) / 115f);
        return Color.Lerp(neutral, toneColor, strength);
    }

    private void ApplyTopicOpinionCityExperience(
        ref int score,
        ref int confidence,
        ref int strongestReasonMagnitude,
        ref string reasonRu,
        ref string reasonEn)
    {
        CityDailyExperience experience = GetLatestCityDailyExperience();
        if (experience == null)
        {
            return;
        }

        int citySignal = Mathf.RoundToInt(Mathf.Clamp(experience.Score, -100, 100) * 0.08f);
        AddTopicOpinionComponent(
            citySignal,
            Mathf.Clamp(experience.Confidence / 18, 0, 6),
            citySignal >= 0 ? "общий опыт города был спокойным и смягчил тему" : "общий опыт города был тяжелым и сделал тему острее",
            citySignal >= 0 ? "the city's shared experience was steady and softened the topic" : "the city's shared experience was hard and sharpened the topic",
            ref score,
            ref confidence,
            ref strongestReasonMagnitude,
            ref reasonRu,
            ref reasonEn,
            replaceReasonThreshold: 9);
    }

    private static string GetCityDailyExperienceSummary(int score, int tension, bool ru)
    {
        if (tension >= 70)
        {
            return ru ? "Город пережил расколотый день." : "The city lived through a divided day.";
        }

        if (score >= 50) return ru ? "Город пережил очень хороший день." : "The city lived through a very good day.";
        if (score >= 15) return ru ? "Город пережил скорее хороший день." : "The city lived through a mostly good day.";
        if (score >= 0) return ru ? "Город пережил ровный день." : "The city lived through a steady day.";
        if (score > -15) return ru ? "Город пережил тревожный день." : "The city lived through an uneasy day.";
        if (score > -50) return ru ? "Город пережил скорее плохой день." : "The city lived through a mostly bad day.";
        return ru ? "Город пережил тяжелый день." : "The city lived through a hard day.";
    }

    private static string BuildCityDailyExperienceFactorReason(CityDailyExperienceFactor factor, bool ru)
    {
        if (factor == null)
        {
            return ru ? "день не дал общей причины" : "the day had no shared reason";
        }

        string label = GetCityDailyExperienceFactorLabel(factor.Kind, ru);
        string reason = ru ? factor.RepresentativeReasonRu : factor.RepresentativeReasonEn;
        if (string.IsNullOrWhiteSpace(reason))
        {
            reason = factor.Score >= 0
                ? ru ? "сигнал был положительным" : "the signal was positive"
                : ru ? "сигнал был отрицательным" : "the signal was negative";
        }

        string count = ru ? $"{factor.ResidentCount} жителей" : $"{factor.ResidentCount} residents";
        return ru
            ? $"{label}: {reason} ({count})"
            : $"{label}: {reason} ({count})";
    }

    private static string FormatCityDailyExperienceToneLabel(WorkerDailyOpinionTone tone, bool ru)
    {
        return tone == WorkerDailyOpinionTone.Positive
            ? ru ? "позитивный" : "positive"
            : ru ? "негативный" : "negative";
    }

    private static string FormatCityDailyExperienceConsensusLabel(CityDailyExperience experience, bool ru)
    {
        if (experience == null)
        {
            return ru ? "без общего вывода" : "without a shared verdict";
        }

        if (experience.Tension >= 70)
        {
            return ru ? "расколот" : "divided";
        }

        if (experience.Consensus >= 80)
        {
            return ru ? "почти единодушен" : "nearly unanimous";
        }

        if (experience.Consensus >= 62)
        {
            return ru ? "скорее согласен" : "mostly aligned";
        }

        return ru ? "напряжен" : "tense";
    }

    private static string GetCityDailyExperienceFactorLabel(WorkerDailyOpinionFactorKind kind, bool ru)
    {
        return kind switch
        {
            WorkerDailyOpinionFactorKind.Need => ru ? "потребности" : "needs",
            WorkerDailyOpinionFactorKind.Work => ru ? "работа" : "work",
            WorkerDailyOpinionFactorKind.Money => ru ? "деньги" : "money",
            WorkerDailyOpinionFactorKind.Social => ru ? "социалка" : "social life",
            WorkerDailyOpinionFactorKind.Family => ru ? "семья" : "family",
            WorkerDailyOpinionFactorKind.Transport => ru ? "транспорт" : "transport",
            WorkerDailyOpinionFactorKind.Housing => ru ? "жилье" : "housing",
            _ => ru ? "город" : "city"
        };
    }
}
