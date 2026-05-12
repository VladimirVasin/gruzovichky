using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int DailyExperienceSocialSignalLimit = 4;
    private const int DailyExperienceSocialSignalMinimumMagnitude = 6;

    private int EmitWorkerDailyExperienceSocialSignals(DriverAgent worker, WorkerDailyOpinion opinion)
    {
        if (worker == null || opinion == null || opinion.Day <= 0)
        {
            return 0;
        }

        int emitted = 0;
        WorkerDailyOpinionFactor dominant = FindDailyExperienceSignalFactor(opinion, opinion.DominantKind, null) ??
                                            FindDailyExperienceSignalFactor(opinion, null, null);
        emitted += RecordDailyExperienceSocialSignal(worker, opinion, dominant, summary: true) != null ? 1 : 0;

        HashSet<WorkerDailyOpinionFactor> used = new();
        if (dominant != null)
        {
            used.Add(dominant);
        }

        while (emitted < DailyExperienceSocialSignalLimit)
        {
            WorkerDailyOpinionFactor next = FindDailyExperienceSignalFactor(opinion, null, used);
            if (next == null || Mathf.Abs(next.Score) < DailyExperienceSocialSignalMinimumMagnitude)
            {
                break;
            }

            SocialSignal signal = RecordDailyExperienceSocialSignal(worker, opinion, next, summary: false);
            if (signal == null)
            {
                break;
            }

            used.Add(next);
            emitted++;
        }

        return emitted;
    }

    private SocialSignal RecordDailyExperienceSocialSignal(
        DriverAgent worker,
        WorkerDailyOpinion opinion,
        WorkerDailyOpinionFactor factor,
        bool summary)
    {
        if (worker == null || opinion == null)
        {
            return null;
        }

        WorkerDailyOpinionFactorKind kind = factor?.Kind ?? opinion.DominantKind;
        int score = summary ? opinion.Score : factor?.Score ?? 0;
        if (score == 0 && !summary)
        {
            return null;
        }

        SocialSignalCategory category = MapDailyOpinionFactorKindToSocialSignalCategory(kind);
        SocialSignalTone tone = score > 0
            ? SocialSignalTone.Positive
            : score < 0 ? SocialSignalTone.Negative : SocialSignalTone.Neutral;
        string topicKey = summary
            ? $"daily_experience:{category}"
            : $"daily_experience:{category}:{NormalizeSocialSignalTopicKey(factor?.ReasonEn, factor?.ReasonRu, category)}";
        string labelRu = summary
            ? "\u041f\u0435\u0440\u0435\u0436\u0438\u0442\u044b\u0439 \u043e\u043f\u044b\u0442"
            : factor?.ReasonRu;
        string labelEn = summary ? "lived experience" : factor?.ReasonEn;
        string reasonRu = summary
            ? $"{opinion.SummaryRu}: {opinion.MainReasonRu}"
            : factor?.ReasonRu;
        string reasonEn = summary
            ? $"{opinion.SummaryEn}: {opinion.MainReasonEn}"
            : factor?.ReasonEn;

        return RecordSocialSignal(
            worker,
            category,
            SocialSignalSourceKind.DailyExperience,
            tone,
            Mathf.Clamp(Mathf.Abs(score), 1, 100),
            Mathf.Clamp(opinion.Confidence, 1, 100),
            topicKey,
            labelRu,
            labelEn,
            reasonRu,
            reasonEn,
            sourceKey: summary
                ? $"daily_experience:{opinion.Day}:summary"
                : $"daily_experience:{opinion.Day}:{category}:{Mathf.Abs(factor?.ReasonEn?.GetHashCode() ?? score)}",
            includeInDailyExperience: false,
            publicForNoosphere: true,
            dedupeHours: 24f,
            dayOverride: opinion.Day);
    }

    private static WorkerDailyOpinionFactor FindDailyExperienceSignalFactor(
        WorkerDailyOpinion opinion,
        WorkerDailyOpinionFactorKind? preferredKind,
        HashSet<WorkerDailyOpinionFactor> excluded)
    {
        if (opinion == null)
        {
            return null;
        }

        WorkerDailyOpinionFactor best = null;
        for (int i = 0; i < opinion.Factors.Count; i++)
        {
            WorkerDailyOpinionFactor factor = opinion.Factors[i];
            if (factor == null ||
                excluded?.Contains(factor) == true ||
                preferredKind.HasValue && factor.Kind != preferredKind.Value)
            {
                continue;
            }

            if (best == null || Mathf.Abs(factor.Score) > Mathf.Abs(best.Score))
            {
                best = factor;
            }
        }

        return best;
    }

    private static SocialSignalCategory MapDailyOpinionFactorKindToSocialSignalCategory(WorkerDailyOpinionFactorKind kind)
    {
        return kind switch
        {
            WorkerDailyOpinionFactorKind.Need => SocialSignalCategory.Need,
            WorkerDailyOpinionFactorKind.Work => SocialSignalCategory.Work,
            WorkerDailyOpinionFactorKind.Money => SocialSignalCategory.Money,
            WorkerDailyOpinionFactorKind.Social => SocialSignalCategory.Social,
            WorkerDailyOpinionFactorKind.Family => SocialSignalCategory.Family,
            WorkerDailyOpinionFactorKind.Transport => SocialSignalCategory.Transport,
            WorkerDailyOpinionFactorKind.Housing => SocialSignalCategory.Housing,
            _ => SocialSignalCategory.City
        };
    }
}
