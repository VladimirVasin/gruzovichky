using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerOpinionBiasMinConfidence = 12;
    private const int WorkerOpinionBiasMaxIntensityDelta = 15;
    private const int WorkerOpinionBiasNeutralToneMinConfidence = 35;
    private const int WorkerOpinionBiasNeutralToneMinScore = 35;
    private const int WorkerOpinionBiasDecayConfidencePerStep = 4;
    private const float WorkerOpinionBiasMinFormationHours = 0.12f;
    private const float WorkerOpinionBiasSameTickGuardHours = 0.05f;
    private const float WorkerOpinionBiasDecayGraceHours = 24f;
    private const float WorkerOpinionBiasDecayStepHours = 24f;
    private const string WorkerOpinionBiasPlaceholderKey = "opinionBias";

    private enum WorkerOpinionBiasRelation
    {
        None,
        Reinforces,
        Dampens,
        ColorsNeutral
    }

    private void ApplyWorkerOpinionBiasToPendingThought(
        DriverAgent worker,
        string thoughtKey,
        ref WorkerThoughtTone tone,
        ref int intensity,
        ref WorkerThoughtPriority priority,
        ref float formationHours,
        WorkerThoughtSubjectType subjectType,
        int subjectId,
        string subjectKey,
        List<WorkerThoughtPlaceholder> placeholders)
    {
        if (worker == null ||
            string.IsNullOrWhiteSpace(thoughtKey) ||
            string.Equals(thoughtKey, "starter_job_resolved", System.StringComparison.Ordinal))
        {
            return;
        }

        WorkerOpinion opinion = FindRelevantWorkerOpinionForPendingThought(
            worker,
            thoughtKey,
            subjectType,
            subjectId,
            subjectKey,
            out int effectiveConfidence,
            out bool exactMatch);
        if (opinion == null)
        {
            return;
        }

        WorkerThoughtTone oldTone = tone;
        WorkerThoughtPriority oldPriority = priority;
        int oldIntensity = intensity;
        float oldFormationHours = formationHours;
        int intensityDelta = CalculateWorkerOpinionBias(opinion, effectiveConfidence, tone, out WorkerOpinionBiasRelation relation);
        if (relation == WorkerOpinionBiasRelation.None)
        {
            return;
        }

        if (relation == WorkerOpinionBiasRelation.ColorsNeutral)
        {
            tone = opinion.Score >= 0 ? WorkerThoughtTone.Positive : WorkerThoughtTone.Negative;
        }

        if (intensityDelta != 0)
        {
            intensity = Mathf.Clamp(intensity + intensityDelta, 0, 100);
        }

        if (formationHours <= 0f)
        {
            formationHours = WorkerOpinionBiasMinFormationHours;
        }

        if (relation == WorkerOpinionBiasRelation.Reinforces)
        {
            float speedup = Mathf.Clamp(Mathf.Abs(intensityDelta) / 65f, 0.06f, 0.24f);
            formationHours = Mathf.Max(WorkerOpinionBiasMinFormationHours, formationHours * (1f - speedup));
            priority = RaiseWorkerThoughtPriorityByOpinionBias(priority, intensity, intensityDelta);
        }
        else if (relation == WorkerOpinionBiasRelation.Dampens)
        {
            float slowdown = Mathf.Clamp(Mathf.Abs(intensityDelta) * 0.01f, 0.02f, 0.1f);
            formationHours = Mathf.Max(WorkerOpinionBiasMinFormationHours, formationHours + slowdown);
        }
        else if (relation == WorkerOpinionBiasRelation.ColorsNeutral)
        {
            formationHours = Mathf.Max(WorkerOpinionBiasMinFormationHours, formationHours * 0.94f);
        }

        if (ShouldShowWorkerOpinionBiasSuffix(relation, intensityDelta))
        {
            UpsertWorkerOpinionBiasPlaceholder(placeholders, relation);
        }

        SessionDebugLogger.Log(
            "THOUGHT_BIAS",
            $"{worker.DriverName} / {thoughtKey} / {FormatWorkerOpinionBiasSubjectDebug(opinion, exactMatch)} / opinion={opinion.Score} conf={effectiveConfidence} / intensity {intensity - oldIntensity:+#;-#;0} / priority {oldPriority}->{priority} / tone {oldTone}->{tone} / formation {oldFormationHours:0.00}->{formationHours:0.00}h.");
    }

    private static int CalculateWorkerOpinionBias(
        WorkerOpinion opinion,
        int effectiveConfidence,
        WorkerThoughtTone tone,
        out WorkerOpinionBiasRelation relation)
    {
        relation = WorkerOpinionBiasRelation.None;
        if (opinion == null || effectiveConfidence < WorkerOpinionBiasMinConfidence || opinion.Score == 0)
        {
            return 0;
        }

        int magnitude = Mathf.Clamp(
            Mathf.RoundToInt(Mathf.Abs(opinion.Score) * effectiveConfidence / 220f),
            1,
            WorkerOpinionBiasMaxIntensityDelta);

        if (tone == WorkerThoughtTone.Negative)
        {
            if (opinion.Score < 0)
            {
                relation = WorkerOpinionBiasRelation.Reinforces;
                return Mathf.Clamp(Mathf.Max(5, magnitude), 1, WorkerOpinionBiasMaxIntensityDelta);
            }

            relation = WorkerOpinionBiasRelation.Dampens;
            return -Mathf.Clamp(Mathf.RoundToInt(magnitude * 0.55f), 1, 8);
        }

        if (tone == WorkerThoughtTone.Positive)
        {
            if (opinion.Score > 0)
            {
                relation = WorkerOpinionBiasRelation.Reinforces;
                return Mathf.Clamp(Mathf.Max(3, magnitude), 1, 10);
            }

            relation = WorkerOpinionBiasRelation.Dampens;
            return -Mathf.Clamp(Mathf.RoundToInt(magnitude * 0.45f), 1, 6);
        }

        if (Mathf.Abs(opinion.Score) >= WorkerOpinionBiasNeutralToneMinScore &&
            effectiveConfidence >= WorkerOpinionBiasNeutralToneMinConfidence)
        {
            relation = WorkerOpinionBiasRelation.ColorsNeutral;
            return Mathf.Clamp(Mathf.RoundToInt(magnitude * 0.5f), 1, 6);
        }

        return 0;
    }

    private WorkerOpinion FindRelevantWorkerOpinionForPendingThought(
        DriverAgent worker,
        string thoughtKey,
        WorkerThoughtSubjectType subjectType,
        int subjectId,
        string subjectKey,
        out int effectiveConfidence,
        out bool exactMatch)
    {
        float now = GetCurrentWorldHour();
        WorkerOpinion best = null;
        int bestConfidence = 0;
        int bestRank = int.MinValue;
        bool bestExactMatch = false;

        SelectWorkerOpinionBiasCandidate(
            worker,
            subjectType,
            subjectId,
            subjectKey,
            true,
            now,
            ref best,
            ref bestConfidence,
            ref bestRank,
            ref bestExactMatch);
        SelectWorkerOpinionBiasFallbackCandidates(
            worker,
            thoughtKey,
            subjectType,
            subjectKey,
            now,
            ref best,
            ref bestConfidence,
            ref bestRank,
            ref bestExactMatch);

        effectiveConfidence = bestConfidence;
        exactMatch = bestExactMatch;
        return best;
    }

    private void SelectWorkerOpinionBiasFallbackCandidates(
        DriverAgent worker,
        string thoughtKey,
        WorkerThoughtSubjectType subjectType,
        string subjectKey,
        float now,
        ref WorkerOpinion best,
        ref int bestConfidence,
        ref int bestRank,
        ref bool bestExactMatch)
    {
        if (IsWorkerThoughtNeedBranch(thoughtKey, subjectType, subjectKey, WorkerNeedKind.Meal))
        {
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.Need, 0, WorkerNeedKind.Meal.ToString(), false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
        }

        if (IsWorkerThoughtNeedBranch(thoughtKey, subjectType, subjectKey, WorkerNeedKind.Sleep))
        {
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.Need, 0, WorkerNeedKind.Sleep.ToString(), false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
        }

        if (IsWorkerThoughtNeedBranch(thoughtKey, subjectType, subjectKey, WorkerNeedKind.Leisure))
        {
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.Need, 0, WorkerNeedKind.Leisure.ToString(), false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
        }

        if (WorkerThoughtKeyContains(thoughtKey, "money") ||
            WorkerThoughtKeyContains(thoughtKey, "unaffordable") ||
            WorkerThoughtKeyContains(thoughtKey, "salary") ||
            WorkerThoughtKeyContains(thoughtKey, "gambling"))
        {
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.Text, 0, "money", false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
        }

        if (WorkerThoughtKeyContains(thoughtKey, "job") ||
            WorkerThoughtKeyContains(thoughtKey, "work") ||
            WorkerThoughtKeyContains(thoughtKey, "starter_job"))
        {
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.Text, 0, "city_work", false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
        }

        if (WorkerThoughtKeyContains(thoughtKey, "family") ||
            WorkerThoughtKeyContains(thoughtKey, "child"))
        {
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.Text, 0, "family", false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
        }

        if (WorkerThoughtKeyContains(thoughtKey, "litter"))
        {
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.Text, 0, "litter", false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.Text, 0, "street_litter", false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
        }

        if (WorkerThoughtKeyContains(thoughtKey, "bus") ||
            WorkerThoughtKeyContains(thoughtKey, "transport"))
        {
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.Text, 0, "local_bus", false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
        }

        if (WorkerThoughtKeyContains(thoughtKey, "gambling"))
        {
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.Text, 0, "gambling", false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.BuildingType, 0, LocationType.GamblingHall.ToString(), false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
        }

        if (WorkerThoughtKeyContains(thoughtKey, "hangover") ||
            WorkerThoughtKeyContains(thoughtKey, "relief_after_rest") ||
            WorkerThoughtKeyContains(thoughtKey, "leisure"))
        {
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.BuildingType, 0, LocationType.Bar.ToString(), false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.BuildingType, 0, LocationType.CityPark.ToString(), false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
        }

        if (WorkerThoughtKeyContains(thoughtKey, "city") ||
            WorkerThoughtKeyContains(thoughtKey, "stable") ||
            WorkerThoughtKeyContains(thoughtKey, "arrived"))
        {
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.Text, 0, "city", false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
            SelectWorkerOpinionBiasCandidate(worker, WorkerThoughtSubjectType.Text, 0, "town_arrival", false, now, ref best, ref bestConfidence, ref bestRank, ref bestExactMatch);
        }
    }

    private static void SelectWorkerOpinionBiasCandidate(
        DriverAgent worker,
        WorkerThoughtSubjectType subjectType,
        int subjectId,
        string subjectKey,
        bool exactMatch,
        float now,
        ref WorkerOpinion best,
        ref int bestConfidence,
        ref int bestRank,
        ref bool bestExactMatch)
    {
        if (subjectType == WorkerThoughtSubjectType.None)
        {
            return;
        }

        WorkerOpinion opinion = FindWorkerOpinion(worker, subjectType, subjectId, subjectKey);
        if (opinion == null)
        {
            return;
        }

        if (now >= opinion.LastUpdatedWorldHour && now - opinion.LastUpdatedWorldHour < WorkerOpinionBiasSameTickGuardHours)
        {
            return;
        }

        int effectiveConfidence = GetWorkerOpinionEffectiveConfidence(opinion, now);
        if (effectiveConfidence < WorkerOpinionBiasMinConfidence)
        {
            return;
        }

        int rank = Mathf.Abs(opinion.Score) * 2 + effectiveConfidence + (exactMatch ? 120 : 0);
        if (rank <= bestRank)
        {
            return;
        }

        best = opinion;
        bestConfidence = effectiveConfidence;
        bestRank = rank;
        bestExactMatch = exactMatch;
    }

    private static int GetWorkerOpinionEffectiveConfidence(WorkerOpinion opinion, float now)
    {
        if (opinion == null)
        {
            return 0;
        }

        int confidence = Mathf.Clamp(opinion.Confidence, 0, 100);
        float ageHours = Mathf.Max(0f, now - opinion.LastUpdatedWorldHour);
        if (ageHours <= WorkerOpinionBiasDecayGraceHours)
        {
            return confidence;
        }

        int decaySteps = Mathf.FloorToInt((ageHours - WorkerOpinionBiasDecayGraceHours) / WorkerOpinionBiasDecayStepHours) + 1;
        return Mathf.Clamp(confidence - decaySteps * WorkerOpinionBiasDecayConfidencePerStep, 0, 100);
    }

    private static bool IsWorkerThoughtNeedBranch(
        string thoughtKey,
        WorkerThoughtSubjectType subjectType,
        string subjectKey,
        WorkerNeedKind need)
    {
        string needKey = need.ToString();
        if (subjectType == WorkerThoughtSubjectType.Need &&
            string.Equals(subjectKey, needKey, System.StringComparison.Ordinal))
        {
            return true;
        }

        return WorkerThoughtKeyContains(thoughtKey, needKey.ToLowerInvariant());
    }

    private static bool WorkerThoughtKeyContains(string thoughtKey, string fragment)
    {
        return !string.IsNullOrEmpty(thoughtKey) &&
               !string.IsNullOrEmpty(fragment) &&
               thoughtKey.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static WorkerThoughtPriority RaiseWorkerThoughtPriorityByOpinionBias(
        WorkerThoughtPriority priority,
        int intensity,
        int intensityDelta)
    {
        if (intensityDelta < 8 || intensity < 65)
        {
            return priority;
        }

        if (priority == WorkerThoughtPriority.Low)
        {
            return WorkerThoughtPriority.Normal;
        }

        if (priority == WorkerThoughtPriority.Normal)
        {
            return WorkerThoughtPriority.High;
        }

        if (priority == WorkerThoughtPriority.High && intensityDelta >= 12 && intensity >= 86)
        {
            return WorkerThoughtPriority.Critical;
        }

        return priority;
    }

    private static bool ShouldShowWorkerOpinionBiasSuffix(WorkerOpinionBiasRelation relation, int intensityDelta)
    {
        return relation == WorkerOpinionBiasRelation.ColorsNeutral ||
               (relation == WorkerOpinionBiasRelation.Reinforces && Mathf.Abs(intensityDelta) >= 7) ||
               (relation == WorkerOpinionBiasRelation.Dampens && Mathf.Abs(intensityDelta) >= 5);
    }

    private static void UpsertWorkerOpinionBiasPlaceholder(
        List<WorkerThoughtPlaceholder> placeholders,
        WorkerOpinionBiasRelation relation)
    {
        if (placeholders == null)
        {
            return;
        }

        string suffixKey = relation switch
        {
            WorkerOpinionBiasRelation.Dampens => "dampens",
            WorkerOpinionBiasRelation.ColorsNeutral => "colors",
            _ => "reinforces"
        };

        for (int i = 0; i < placeholders.Count; i++)
        {
            WorkerThoughtPlaceholder existing = placeholders[i];
            if (existing != null &&
                string.Equals(existing.Key, WorkerOpinionBiasPlaceholderKey, System.StringComparison.Ordinal))
            {
                existing.SubjectKey = suffixKey;
                return;
            }
        }

        placeholders.Add(new WorkerThoughtPlaceholder
        {
            Key = WorkerOpinionBiasPlaceholderKey,
            SubjectType = WorkerThoughtSubjectType.Text,
            SubjectKey = suffixKey,
            FallbackLabel = string.Empty
        });
    }

    private static string AppendWorkerThoughtOptionalSuffix(
        string text,
        IReadOnlyList<WorkerThoughtPlaceholder> placeholders,
        bool ru,
        bool templateHadOpinionBiasToken)
    {
        if (templateHadOpinionBiasToken || string.IsNullOrWhiteSpace(text) || placeholders == null)
        {
            return text ?? string.Empty;
        }

        for (int i = 0; i < placeholders.Count; i++)
        {
            WorkerThoughtPlaceholder placeholder = placeholders[i];
            if (placeholder == null ||
                !string.Equals(placeholder.Key, WorkerOpinionBiasPlaceholderKey, System.StringComparison.Ordinal))
            {
                continue;
            }

            string suffix = FormatWorkerOpinionBiasPlaceholder(placeholder.SubjectKey, ru);
            return string.IsNullOrWhiteSpace(suffix) ? text : $"{text} {suffix}";
        }

        return text;
    }

    private static string FormatWorkerOpinionBiasPlaceholder(string suffixKey, bool ru)
    {
        return suffixKey switch
        {
            "dampens" => ru
                ? "\u0421\u0442\u0430\u0440\u044b\u0439 \u043e\u043f\u044b\u0442 \u0441\u043c\u044f\u0433\u0447\u0430\u0435\u0442 \u0440\u0435\u0430\u043a\u0446\u0438\u044e."
                : "Past experience softens the reaction.",
            "colors" => ru
                ? "\u0421\u0442\u0430\u0440\u044b\u0439 \u043e\u043f\u044b\u0442 \u043e\u043a\u0440\u0430\u0448\u0438\u0432\u0430\u0435\u0442 \u043c\u044b\u0441\u043b\u044c."
                : "Past experience colors the thought.",
            _ => ru
                ? "\u0421\u0442\u0430\u0440\u044b\u0439 \u043e\u043f\u044b\u0442 \u0434\u0435\u043b\u0430\u0435\u0442 \u044d\u0442\u043e \u0437\u0430\u043c\u0435\u0442\u043d\u0435\u0435."
                : "Past experience makes this stand out."
        };
    }

    private static string FormatWorkerOpinionBiasSubjectDebug(WorkerOpinion opinion, bool exactMatch)
    {
        if (opinion == null)
        {
            return "no opinion";
        }

        string match = exactMatch ? "exact" : "fallback";
        return $"{match}:{opinion.SubjectType}/{opinion.SubjectId}/{opinion.SubjectKey}";
    }
}
