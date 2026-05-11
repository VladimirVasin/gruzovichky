using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerDailyOpinionHistoryCap = 7;
    private const int WorkerDailyOpinionReasonMaxChars = 96;

    private void FinalizeWorkerDailyOpinionsForDay(int endedDay)
    {
        if (endedDay <= 0)
        {
            return;
        }

        int positive = 0;
        int negative = 0;
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent worker = driverAgents[i];
            if (worker == null || worker.HasDepartedTown || worker.LastDailyOpinionDay >= endedDay)
            {
                continue;
            }

            WorkerDailyOpinion opinion = BuildWorkerDailyOpinion(worker, endedDay);
            worker.DailyOpinions.Insert(0, opinion);
            while (worker.DailyOpinions.Count > WorkerDailyOpinionHistoryCap)
            {
                worker.DailyOpinions.RemoveAt(worker.DailyOpinions.Count - 1);
            }

            worker.LastDailyOpinionDay = endedDay;
            if (opinion.FinalTone == WorkerDailyOpinionTone.Positive)
            {
                positive++;
            }
            else
            {
                negative++;
            }

            SessionDebugLogger.LogVerbose(
                "DAILY_OPINION",
                $"{worker.DriverName} day {endedDay}: tone={opinion.FinalTone}, score={opinion.Score}, confidence={opinion.Confidence}, main={opinion.MainReasonEn}, secondary={opinion.SecondaryReasonEn}.");
        }

        if (positive > 0 || negative > 0)
        {
            SessionDebugLogger.Log(
                "DAILY_OPINION",
                $"Finalized day {endedDay} resident opinions: positive={positive}, negative={negative}.");
            isDriversScreenDirty = true;
        }

        FinalizeCityDailyExperienceForDay(endedDay);
    }

    private WorkerDailyOpinion BuildWorkerDailyOpinion(DriverAgent worker, int day)
    {
        UpdateWorkerLifeOpinionsSnapshot(worker);

        List<WorkerDailyOpinionFactor> factors = new();
        int positiveThoughts = 0;
        int negativeThoughts = 0;
        int criticalActiveThoughts = 0;

        for (int i = 0; i < worker.Thoughts.Count; i++)
        {
            WorkerThought thought = worker.Thoughts[i];
            if (thought == null)
            {
                continue;
            }

            if (thought.CreatedDay == day)
            {
                int thoughtScore = CalculateWorkerDailyThoughtScore(thought);
                if (thought.Tone == WorkerThoughtTone.Positive)
                {
                    positiveThoughts++;
                }
                else if (thought.Tone == WorkerThoughtTone.Negative)
                {
                    negativeThoughts++;
                }

                if (thoughtScore != 0)
                {
                    AddDailyOpinionFactor(
                        factors,
                        MapWorkerThoughtKindToDailyFactorKind(thought.Kind),
                        thoughtScore,
                        NormalizeDailyOpinionReason(RenderWorkerThought(thought, true), GetDailyThoughtFallbackReason(thought, true)),
                        NormalizeDailyOpinionReason(RenderWorkerThought(thought, false), GetDailyThoughtFallbackReason(thought, false)));
                }
            }

            if (thought.Active && thought.Priority == WorkerThoughtPriority.Critical)
            {
                criticalActiveThoughts++;
            }
            else if (thought.Active && thought.CreatedDay < day && thought.Tone == WorkerThoughtTone.Negative)
            {
                int activeScore = -Mathf.Max(8, Mathf.RoundToInt(thought.Intensity * 0.18f));
                AddDailyOpinionFactor(
                    factors,
                    MapWorkerThoughtKindToDailyFactorKind(thought.Kind),
                    activeScore,
                    NormalizeDailyOpinionReason(RenderWorkerThought(thought, true), "нерешенная тревожная мысль"),
                    NormalizeDailyOpinionReason(RenderWorkerThought(thought, false), "unresolved worrying thought"));
            }
        }

        AddDailyNeedFactors(worker, factors);
        AddDailyWorkFactors(worker, factors);
        AddDailyMoneyFactors(worker, factors);
        AddDailyLifeOpinionFactors(worker, factors);
        AddDailyFamilyFactors(worker, factors);

        if (factors.Count == 0)
        {
            AddDailyOpinionFactor(
                factors,
                WorkerDailyOpinionFactorKind.City,
                4,
                "день прошел ровно",
                "the day was steady");
        }

        int score = 0;
        for (int i = 0; i < factors.Count; i++)
        {
            score += factors[i].Score;
        }

        score = Mathf.Clamp(score, -100, 100);
        WorkerDailyOpinionTone tone = score >= 0 ? WorkerDailyOpinionTone.Positive : WorkerDailyOpinionTone.Negative;
        WorkerDailyOpinionFactor main = FindStrongestDailyOpinionFactor(factors, tone == WorkerDailyOpinionTone.Positive, null) ??
                                        FindStrongestDailyOpinionFactor(factors, null, null);
        WorkerDailyOpinionFactor secondary = FindStrongestDailyOpinionFactor(factors, null, main);
        WorkerDailyOpinionFactor positive = FindStrongestDailyOpinionFactor(factors, true, main);
        WorkerDailyOpinionFactor negative = FindStrongestDailyOpinionFactor(factors, false, main);

        WorkerDailyOpinion opinion = new()
        {
            Day = day,
            FinalTone = tone,
            Score = score,
            Confidence = CalculateDailyOpinionConfidence(factors.Count, criticalActiveThoughts, positiveThoughts, negativeThoughts),
            SummaryRu = GetDailyOpinionSummary(score, true),
            SummaryEn = GetDailyOpinionSummary(score, false),
            MainReasonRu = main?.ReasonRu ?? "день не дал яркой причины",
            MainReasonEn = main?.ReasonEn ?? "the day had no strong reason",
            SecondaryReasonRu = secondary?.ReasonRu ?? string.Empty,
            SecondaryReasonEn = secondary?.ReasonEn ?? string.Empty,
            PositiveReasonRu = positive?.ReasonRu ?? string.Empty,
            PositiveReasonEn = positive?.ReasonEn ?? string.Empty,
            NegativeReasonRu = negative?.ReasonRu ?? string.Empty,
            NegativeReasonEn = negative?.ReasonEn ?? string.Empty,
            PositiveThoughtCount = positiveThoughts,
            NegativeThoughtCount = negativeThoughts,
            CriticalActiveThoughtCount = criticalActiveThoughts,
            SatisfactionDeltaHint = Mathf.Clamp(Mathf.RoundToInt(score * 0.16f), -18, 12),
            DominantKind = main?.Kind ?? WorkerDailyOpinionFactorKind.City
        };
        opinion.Factors.AddRange(factors);
        return opinion;
    }

    private static int CalculateWorkerDailyThoughtScore(WorkerThought thought)
    {
        if (thought == null || thought.Tone == WorkerThoughtTone.Neutral)
        {
            return 0;
        }

        int priorityBonus = thought.Priority switch
        {
            WorkerThoughtPriority.Critical => 16,
            WorkerThoughtPriority.High => 10,
            WorkerThoughtPriority.Normal => 5,
            _ => 2
        };
        int magnitude = Mathf.Clamp(Mathf.RoundToInt(thought.Intensity * 0.32f) + priorityBonus, 2, 44);
        if (thought.Active)
        {
            magnitude += 6;
        }

        return thought.Tone == WorkerThoughtTone.Positive ? magnitude : -magnitude;
    }

    private void AddDailyNeedFactors(DriverAgent worker, List<WorkerDailyOpinionFactor> factors)
    {
        AddDailyNeedStatusFactor(worker.LastMealNeedStatus, WorkerNeedKind.Meal, factors);
        AddDailyNeedStatusFactor(worker.LastSleepNeedStatus, WorkerNeedKind.Sleep, factors);
        AddDailyNeedStatusFactor(worker.LastLeisureNeedStatus, WorkerNeedKind.Leisure, factors);

        if (worker.AteToday)
        {
            AddDailyOpinionFactor(factors, WorkerDailyOpinionFactorKind.Need, 7, "еда была закрыта", "food was handled");
        }

        if (worker.SleptToday)
        {
            AddDailyOpinionFactor(factors, WorkerDailyOpinionFactorKind.Need, 8, "получилось поспать", "sleep was handled");
        }

        if (worker.HadLeisureToday)
        {
            AddDailyOpinionFactor(factors, WorkerDailyOpinionFactorKind.Need, 6, "получилось отдохнуть", "leisure was handled");
        }
    }

    private void AddDailyNeedStatusFactor(WorkerNeedStatus status, WorkerNeedKind need, List<WorkerDailyOpinionFactor> factors)
    {
        if (status == WorkerNeedStatus.Ok)
        {
            return;
        }

        bool critical = status == WorkerNeedStatus.Critical;
        int score = need switch
        {
            WorkerNeedKind.Sleep => critical ? -30 : -10,
            WorkerNeedKind.Meal => critical ? -26 : -9,
            _ => critical ? -20 : -7
        };
        string needRu = FormatWorkerThoughtNeed(need.ToString(), true);
        string needEn = FormatWorkerThoughtNeed(need.ToString(), false);
        AddDailyOpinionFactor(
            factors,
            WorkerDailyOpinionFactorKind.Need,
            score,
            critical ? $"{needRu} осталась критической" : $"{needRu} тревожила к концу дня",
            critical ? $"{needEn} stayed critical" : $"{needEn} was worrying by the end of the day");
    }

    private void AddDailyWorkFactors(DriverAgent worker, List<WorkerDailyOpinionFactor> factors)
    {
        if (worker.IsArrivingByBus)
        {
            AddDailyOpinionFactor(factors, WorkerDailyOpinionFactorKind.City, 2, "житель еще добирался в город", "the resident was still arriving");
            return;
        }

        bool employed = IsWorkerEmployedForMigration(worker);
        AddDailyOpinionFactor(
            factors,
            WorkerDailyOpinionFactorKind.Work,
            employed ? 12 : -24,
            employed ? "работа была закреплена" : "работы пока нет",
            employed ? "work was secured" : "there was no job yet");

        if (worker.WorkedToday)
        {
            AddDailyOpinionFactor(factors, WorkerDailyOpinionFactorKind.Work, 10, "смена действительно состоялась", "the shift actually happened");
        }
    }

    private void AddDailyMoneyFactors(DriverAgent worker, List<WorkerDailyOpinionFactor> factors)
    {
        if (worker.Money < 15)
        {
            AddDailyOpinionFactor(factors, WorkerDailyOpinionFactorKind.Money, -24, "денег почти не осталось", "money was almost gone");
        }
        else if (worker.Money < 50)
        {
            AddDailyOpinionFactor(factors, WorkerDailyOpinionFactorKind.Money, -11, "денег мало", "money was low");
        }
        else if (worker.Money >= 140)
        {
            AddDailyOpinionFactor(factors, WorkerDailyOpinionFactorKind.Money, 14, "денег с запасом", "money felt comfortable");
        }
        else if (worker.Money >= 80)
        {
            AddDailyOpinionFactor(factors, WorkerDailyOpinionFactorKind.Money, 8, "денег хватало", "money was enough");
        }

        if (IsWorkerDueButBlockedByMoney(worker))
        {
            AddDailyOpinionFactor(factors, WorkerDailyOpinionFactorKind.Money, -16, "не хватало денег на сервисы", "services were unaffordable");
        }
    }

    private void AddDailyLifeOpinionFactors(DriverAgent worker, List<WorkerDailyOpinionFactor> factors)
    {
        AddDailyLifeOpinionFactor(FindWorkerLifeOpinion(worker, WorkerLifeOpinionCategory.City), WorkerDailyOpinionFactorKind.City, factors);
        AddDailyLifeOpinionFactor(FindWorkerLifeOpinion(worker, WorkerLifeOpinionCategory.Social), WorkerDailyOpinionFactorKind.Social, factors);
        AddDailyLifeOpinionFactor(FindWorkerLifeOpinion(worker, WorkerLifeOpinionCategory.Housing), WorkerDailyOpinionFactorKind.Housing, factors);
    }

    private static void AddDailyLifeOpinionFactor(WorkerLifeOpinion opinion, WorkerDailyOpinionFactorKind kind, List<WorkerDailyOpinionFactor> factors)
    {
        if (opinion == null || !opinion.HasScore || Mathf.Abs(opinion.Score) < 15)
        {
            return;
        }

        int score = Mathf.Clamp(Mathf.RoundToInt(opinion.Score * Mathf.Lerp(0.08f, 0.18f, opinion.Confidence / 100f)), -18, 18);
        if (score == 0)
        {
            return;
        }

        AddDailyOpinionFactor(factors, kind, score, GetLifeOpinionDailyReason(kind, score, true), GetLifeOpinionDailyReason(kind, score, false));
    }

    private void AddDailyFamilyFactors(DriverAgent worker, List<WorkerDailyOpinionFactor> factors)
    {
        WorkerFamily family = GetWorkerFamilyById(worker.FamilyId);
        if (family == null)
        {
            return;
        }

        if (family.LastDailyUpkeepShortfall > 0)
        {
            AddDailyOpinionFactor(factors, WorkerDailyOpinionFactorKind.Family, -16, "семейному бюджету не хватило денег", "the household budget was short");
        }

        if (family.Happiness >= 75)
        {
            AddDailyOpinionFactor(factors, WorkerDailyOpinionFactorKind.Family, 10, "дома спокойно", "home felt stable");
        }
        else if (family.Happiness <= 35)
        {
            AddDailyOpinionFactor(factors, WorkerDailyOpinionFactorKind.Family, -14, "дома накопилось напряжение", "home felt tense");
        }
    }

    private static void AddDailyOpinionFactor(
        List<WorkerDailyOpinionFactor> factors,
        WorkerDailyOpinionFactorKind kind,
        int score,
        string reasonRu,
        string reasonEn)
    {
        if (factors == null || score == 0)
        {
            return;
        }

        factors.Add(new WorkerDailyOpinionFactor
        {
            Kind = kind,
            Score = score,
            ReasonRu = NormalizeDailyOpinionReason(reasonRu, "день повлиял на настроение"),
            ReasonEn = NormalizeDailyOpinionReason(reasonEn, "the day affected the mood")
        });
    }

    private static WorkerDailyOpinionFactor FindStrongestDailyOpinionFactor(
        List<WorkerDailyOpinionFactor> factors,
        bool? positive,
        WorkerDailyOpinionFactor except)
    {
        WorkerDailyOpinionFactor best = null;
        int bestMagnitude = -1;
        for (int i = 0; i < factors.Count; i++)
        {
            WorkerDailyOpinionFactor factor = factors[i];
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

    private static int CalculateDailyOpinionConfidence(int factorCount, int criticalActiveThoughts, int positiveThoughts, int negativeThoughts)
    {
        return Mathf.Clamp(42 + factorCount * 6 + criticalActiveThoughts * 10 + Mathf.Abs(positiveThoughts - negativeThoughts) * 4, 35, 95);
    }

    private static WorkerDailyOpinionFactorKind MapWorkerThoughtKindToDailyFactorKind(WorkerThoughtKind kind)
    {
        return kind switch
        {
            WorkerThoughtKind.Need => WorkerDailyOpinionFactorKind.Need,
            WorkerThoughtKind.Work => WorkerDailyOpinionFactorKind.Work,
            WorkerThoughtKind.Money => WorkerDailyOpinionFactorKind.Money,
            WorkerThoughtKind.Social => WorkerDailyOpinionFactorKind.Social,
            WorkerThoughtKind.Family => WorkerDailyOpinionFactorKind.Family,
            WorkerThoughtKind.Transport => WorkerDailyOpinionFactorKind.Transport,
            _ => WorkerDailyOpinionFactorKind.City
        };
    }

    private static string GetDailyThoughtFallbackReason(WorkerThought thought, bool ru)
    {
        return thought?.Kind switch
        {
            WorkerThoughtKind.Need => ru ? "потребности давили" : "needs were pressing",
            WorkerThoughtKind.Work => ru ? "работа повлияла на день" : "work affected the day",
            WorkerThoughtKind.Money => ru ? "деньги повлияли на день" : "money affected the day",
            WorkerThoughtKind.Social => ru ? "общение оставило след" : "social life left a mark",
            WorkerThoughtKind.Family => ru ? "семья повлияла на день" : "family affected the day",
            WorkerThoughtKind.Transport => ru ? "дорога повлияла на день" : "transport affected the day",
            _ => ru ? "город оставил впечатление" : "the city left an impression"
        };
    }

    private static string GetLifeOpinionDailyReason(WorkerDailyOpinionFactorKind kind, int score, bool ru)
    {
        bool positive = score >= 0;
        return kind switch
        {
            WorkerDailyOpinionFactorKind.Social => ru
                ? positive ? "общение складывалось хорошо" : "общение складывалось тяжело"
                : positive ? "social life felt good" : "social life felt strained",
            WorkerDailyOpinionFactorKind.Housing => ru
                ? positive ? "с жильем все спокойно" : "жилье не дает опоры"
                : positive ? "housing felt stable" : "housing did not feel secure",
            _ => ru
                ? positive ? "город ощущался надежнее" : "город ощущался тревожно"
                : positive ? "the city felt safer" : "the city felt worrying"
        };
    }

    private static string GetDailyOpinionSummary(int score, bool ru)
    {
        if (score >= 50) return ru ? "День закончился очень хорошо." : "The day ended very well.";
        if (score >= 15) return ru ? "День скорее хороший." : "The day was mostly good.";
        if (score >= 0) return ru ? "День ровный, итог скорее положительный." : "The day was steady, leaning positive.";
        if (score > -15) return ru ? "День тревожный, но без провала." : "The day was uneasy, but not a collapse.";
        if (score > -50) return ru ? "День скорее плохой." : "The day was mostly bad.";
        return ru ? "День закончился тяжело." : "The day ended badly.";
    }

    private static string NormalizeDailyOpinionReason(string text, string fallback)
    {
        string value = string.IsNullOrWhiteSpace(text) ? fallback : text.Trim();
        value = value.Replace('\n', ' ').Replace('\r', ' ');
        while (value.Contains("  "))
        {
            value = value.Replace("  ", " ");
        }

        if (value.Length <= WorkerDailyOpinionReasonMaxChars)
        {
            return value;
        }

        return value.Substring(0, WorkerDailyOpinionReasonMaxChars - 3).TrimEnd() + "...";
    }

    private static WorkerDailyOpinion GetLatestWorkerDailyOpinion(DriverAgent worker)
    {
        return worker != null && worker.DailyOpinions.Count > 0 ? worker.DailyOpinions[0] : null;
    }

    private static string FormatWorkerDailyOpinionToneLabel(WorkerDailyOpinion opinion, bool ru)
    {
        if (opinion == null)
        {
            return ru ? "Итог дня ещё не сформирован" : "Daily result is not formed yet";
        }

        bool positive = opinion.FinalTone == WorkerDailyOpinionTone.Positive;
        return ru
            ? $"Пережитый опыт: {(positive ? "позитивный" : "негативный")}"
            : $"Result: {(positive ? "positive" : "negative")}";
    }

    private static string FormatWorkerDailyOpinionScoreLine(WorkerDailyOpinion opinion, bool ru)
    {
        if (opinion == null)
        {
            return string.Empty;
        }

        string score = $"{opinion.Score:+#;-#;0}";
        return ru
            ? $"День {opinion.Day} | оценка {score} | уверенность {opinion.Confidence}%"
            : $"Day {opinion.Day} | score {score} | confidence {opinion.Confidence}%";
    }

    private static string BuildWorkerDailyOpinionReasonLine(WorkerDailyOpinion opinion, bool ru)
    {
        if (opinion == null)
        {
            return ru ? "Появится после первой полуночи." : "Appears after the first midnight.";
        }

        string main = ru ? opinion.MainReasonRu : opinion.MainReasonEn;
        string secondary = ru ? opinion.SecondaryReasonRu : opinion.SecondaryReasonEn;
        string positive = ru ? opinion.PositiveReasonRu : opinion.PositiveReasonEn;
        string negative = ru ? opinion.NegativeReasonRu : opinion.NegativeReasonEn;
        string result = ru ? $"Почему: {main}." : $"Why: {main}.";

        if (!string.IsNullOrWhiteSpace(secondary))
        {
            result += ru ? $" Также: {secondary}." : $" Also: {secondary}.";
        }

        if (opinion.FinalTone == WorkerDailyOpinionTone.Positive && !string.IsNullOrWhiteSpace(negative))
        {
            result += ru ? $" Риск: {negative}." : $" Risk: {negative}.";
        }
        else if (opinion.FinalTone == WorkerDailyOpinionTone.Negative && !string.IsNullOrWhiteSpace(positive))
        {
            result += ru ? $" Позитивное: {positive}." : $" Positive note: {positive}.";
        }

        return result;
    }
}
