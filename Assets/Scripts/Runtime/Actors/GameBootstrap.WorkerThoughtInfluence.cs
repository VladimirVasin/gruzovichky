using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerThoughtInfluenceMaxPositiveIntensityDelta = 15;
    private const int WorkerThoughtInfluenceMaxNegativeIntensityDelta = -10;
    private const int WorkerThoughtInfluenceMaxOpinionDeltaModifier = 4;
    private const int WorkerThoughtInfluenceMaxAppliedRules = 3;
    private const float WorkerThoughtInfluenceSameTickGuardHours = 0.05f;
    private const float WorkerThoughtInfluenceMinFormationHours = 0.12f;
    private const string WorkerThoughtInfluencePlaceholderKey = "thoughtInfluence";

    private static readonly WorkerThoughtInfluenceRule[] WorkerThoughtInfluenceRules = new WorkerThoughtInfluenceRule[0];

    private enum WorkerThoughtInfluenceDirection
    {
        Amplify,
        Dampen,
        Relief,
        Contradict,
        Stabilize
    }

    private sealed class WorkerThoughtInfluenceRule
    {
        public string SourceThoughtKey { get; set; } = string.Empty;
        public string TargetThoughtKey { get; set; } = string.Empty;
        public WorkerThoughtInfluenceDirection Direction { get; set; }
        public int IntensityDelta { get; set; }
        public int PriorityDelta { get; set; }
        public float FormationTimeMultiplier { get; set; } = 1f;
        public float WindowHours { get; set; } = 48f;
        public int OpinionDeltaModifier { get; set; }
        public WorkerThoughtInfluenceTraitModifier[] TraitModifiers { get; set; }
        public WorkerThoughtInfluenceWeaknessModifier[] WeaknessModifiers { get; set; }
        public string HumanReasonRu { get; set; } = string.Empty;
        public string HumanReasonEn { get; set; } = string.Empty;
        public string ExampleWordingRu { get; set; } = string.Empty;
        public string ExampleWordingEn { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }

    private readonly struct WorkerThoughtInfluenceTraitModifier
    {
        public readonly WorkerTraitKind Trait;
        public readonly int IntensityDelta;
        public readonly int OpinionDeltaModifier;
        public readonly float FormationTimeMultiplier;

        public WorkerThoughtInfluenceTraitModifier(
            WorkerTraitKind trait,
            int intensityDelta,
            int opinionDeltaModifier = 0,
            float formationTimeMultiplier = 1f)
        {
            Trait = trait;
            IntensityDelta = intensityDelta;
            OpinionDeltaModifier = opinionDeltaModifier;
            FormationTimeMultiplier = formationTimeMultiplier;
        }
    }

    private readonly struct WorkerThoughtInfluenceWeaknessModifier
    {
        public readonly WorkerWeaknessKind Weakness;
        public readonly int IntensityDelta;
        public readonly int OpinionDeltaModifier;
        public readonly float FormationTimeMultiplier;

        public WorkerThoughtInfluenceWeaknessModifier(
            WorkerWeaknessKind weakness,
            int intensityDelta,
            int opinionDeltaModifier = 0,
            float formationTimeMultiplier = 1f)
        {
            Weakness = weakness;
            IntensityDelta = intensityDelta;
            OpinionDeltaModifier = opinionDeltaModifier;
            FormationTimeMultiplier = formationTimeMultiplier;
        }
    }

    private readonly struct WorkerThoughtInfluenceCandidate
    {
        public readonly WorkerThoughtInfluenceRule Rule;
        public readonly WorkerThought SourceThought;
        public readonly int IntensityDelta;
        public readonly int PriorityDelta;
        public readonly float FormationTimeMultiplier;
        public readonly int OpinionDeltaModifier;
        public readonly int Strength;

        public WorkerThoughtInfluenceCandidate(
            WorkerThoughtInfluenceRule rule,
            WorkerThought sourceThought,
            int intensityDelta,
            int priorityDelta,
            float formationTimeMultiplier,
            int opinionDeltaModifier,
            int strength)
        {
            Rule = rule;
            SourceThought = sourceThought;
            IntensityDelta = intensityDelta;
            PriorityDelta = priorityDelta;
            FormationTimeMultiplier = formationTimeMultiplier;
            OpinionDeltaModifier = opinionDeltaModifier;
            Strength = strength;
        }
    }

    private void ApplyWorkerThoughtInfluenceRulesToPendingThought(
        DriverAgent worker,
        string targetThoughtKey,
        string formationKey,
        ref WorkerThoughtTone tone,
        ref int intensity,
        ref WorkerThoughtPriority priority,
        ref float formationHours,
        ref int opinionDelta,
        List<WorkerThoughtPlaceholder> placeholders)
    {
        if (worker == null ||
            worker.Thoughts.Count == 0 ||
            WorkerThoughtInfluenceRules.Length == 0 ||
            string.IsNullOrWhiteSpace(targetThoughtKey))
        {
            return;
        }

        string normalizedTargetKey = NormalizeWorkerThoughtInfluenceKey(targetThoughtKey);
        if (string.Equals(normalizedTargetKey, "starter_job_resolved", System.StringComparison.Ordinal))
        {
            return;
        }

        float now = GetCurrentWorldHour();
        List<WorkerThoughtInfluenceCandidate> candidates = new();
        for (int i = 0; i < WorkerThoughtInfluenceRules.Length; i++)
        {
            WorkerThoughtInfluenceRule rule = WorkerThoughtInfluenceRules[i];
            if (rule == null ||
                !rule.Enabled ||
                !string.Equals(NormalizeWorkerThoughtInfluenceKey(rule.TargetThoughtKey), normalizedTargetKey, System.StringComparison.Ordinal))
            {
                continue;
            }

            WorkerThought sourceThought = FindWorkerThoughtInfluenceSource(worker, rule, now);
            if (sourceThought == null)
            {
                continue;
            }

            WorkerThoughtInfluenceCandidate candidate = BuildWorkerThoughtInfluenceCandidate(worker, rule, sourceThought);
            if (candidate.Strength > 0)
            {
                candidates.Add(candidate);
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        candidates.Sort((left, right) => right.Strength.CompareTo(left.Strength));
        int appliedCount = Mathf.Min(WorkerThoughtInfluenceMaxAppliedRules, candidates.Count);
        int totalIntensityDelta = 0;
        int totalOpinionDeltaModifier = 0;
        int priorityDelta = 0;
        float formationMultiplier = 1f;
        WorkerThoughtInfluenceCandidate strongest = candidates[0];

        for (int i = 0; i < appliedCount; i++)
        {
            WorkerThoughtInfluenceCandidate candidate = candidates[i];
            totalIntensityDelta += candidate.IntensityDelta;
            totalOpinionDeltaModifier += candidate.OpinionDeltaModifier;
            priorityDelta = Mathf.Clamp(priorityDelta + candidate.PriorityDelta, -1, 1);
            formationMultiplier *= Mathf.Clamp(candidate.FormationTimeMultiplier, 0.75f, 1.18f);
        }

        totalIntensityDelta = Mathf.Clamp(totalIntensityDelta, WorkerThoughtInfluenceMaxNegativeIntensityDelta, WorkerThoughtInfluenceMaxPositiveIntensityDelta);
        totalOpinionDeltaModifier = Mathf.Clamp(totalOpinionDeltaModifier, -WorkerThoughtInfluenceMaxOpinionDeltaModifier, WorkerThoughtInfluenceMaxOpinionDeltaModifier);

        WorkerThoughtPriority oldPriority = priority;
        int oldIntensity = intensity;
        float oldFormationHours = formationHours;
        int oldOpinionDelta = opinionDelta;
        intensity = Mathf.Clamp(intensity + totalIntensityDelta, 0, 100);
        opinionDelta = Mathf.Clamp(opinionDelta + totalOpinionDeltaModifier, -100, 100);
        priority = ApplyWorkerThoughtInfluencePriorityDelta(priority, priorityDelta);
        formationHours = Mathf.Max(WorkerThoughtInfluenceMinFormationHours, formationHours * Mathf.Clamp(formationMultiplier, 0.72f, 1.22f));
        UpsertWorkerThoughtInfluencePlaceholder(placeholders, strongest.Rule.Direction);

        SessionDebugLogger.Log(
            "THOUGHT_INFLUENCE",
            $"{worker.DriverName} / {NormalizeWorkerThoughtInfluenceKey(strongest.Rule.SourceThoughtKey)} -> {normalizedTargetKey} / formation={formationKey} / rules={appliedCount} / I {intensity - oldIntensity:+#;-#;0} / O {opinionDelta - oldOpinionDelta:+#;-#;0} / P {oldPriority}->{priority} / F {oldFormationHours:0.00}->{formationHours:0.00}h / reason={strongest.Rule.HumanReasonEn}.");
    }

    private WorkerThought FindWorkerThoughtInfluenceSource(DriverAgent worker, WorkerThoughtInfluenceRule rule, float now)
    {
        if (worker == null || rule == null || string.IsNullOrWhiteSpace(rule.SourceThoughtKey))
        {
            return null;
        }

        string normalizedSourceKey = NormalizeWorkerThoughtInfluenceKey(rule.SourceThoughtKey);
        if (string.Equals(normalizedSourceKey, "starter_job_resolved", System.StringComparison.Ordinal))
        {
            return null;
        }

        float windowHours = Mathf.Max(0.1f, rule.WindowHours);
        for (int i = 0; i < worker.Thoughts.Count; i++)
        {
            WorkerThought thought = worker.Thoughts[i];
            if (thought == null)
            {
                continue;
            }

            if (now >= thought.CreatedWorldHour && now - thought.CreatedWorldHour < WorkerThoughtInfluenceSameTickGuardHours)
            {
                continue;
            }

            if (now >= thought.CreatedWorldHour && now - thought.CreatedWorldHour > windowHours)
            {
                continue;
            }

            if (DoesWorkerThoughtInfluenceKeyMatch(thought.Key, normalizedSourceKey) ||
                DoesWorkerThoughtInfluenceKeyMatch(thought.TemplateKey, normalizedSourceKey))
            {
                return thought;
            }
        }

        return null;
    }

    private static WorkerThoughtInfluenceCandidate BuildWorkerThoughtInfluenceCandidate(
        DriverAgent worker,
        WorkerThoughtInfluenceRule rule,
        WorkerThought sourceThought)
    {
        int intensityDelta = rule.IntensityDelta;
        int opinionDeltaModifier = rule.OpinionDeltaModifier;
        float formationMultiplier = rule.FormationTimeMultiplier <= 0f ? 1f : rule.FormationTimeMultiplier;

        if (rule.TraitModifiers != null)
        {
            for (int i = 0; i < rule.TraitModifiers.Length; i++)
            {
                WorkerThoughtInfluenceTraitModifier modifier = rule.TraitModifiers[i];
                if (!HasWorkerTrait(worker, modifier.Trait))
                {
                    continue;
                }

                intensityDelta += modifier.IntensityDelta;
                opinionDeltaModifier += modifier.OpinionDeltaModifier;
                if (modifier.FormationTimeMultiplier > 0f)
                {
                    formationMultiplier *= modifier.FormationTimeMultiplier;
                }
            }
        }

        if (rule.WeaknessModifiers != null)
        {
            for (int i = 0; i < rule.WeaknessModifiers.Length; i++)
            {
                WorkerThoughtInfluenceWeaknessModifier modifier = rule.WeaknessModifiers[i];
                if (!HasWorkerWeakness(worker, modifier.Weakness))
                {
                    continue;
                }

                intensityDelta += modifier.IntensityDelta;
                opinionDeltaModifier += modifier.OpinionDeltaModifier;
                if (modifier.FormationTimeMultiplier > 0f)
                {
                    formationMultiplier *= modifier.FormationTimeMultiplier;
                }
            }
        }

        int strength = Mathf.Abs(intensityDelta) + Mathf.Abs(opinionDeltaModifier) * 2 + Mathf.Abs(rule.PriorityDelta) * 6;
        if (sourceThought != null)
        {
            strength += sourceThought.Priority switch
            {
                WorkerThoughtPriority.Critical => 8,
                WorkerThoughtPriority.High => 5,
                WorkerThoughtPriority.Normal => 2,
                _ => 0
            };
        }

        return new WorkerThoughtInfluenceCandidate(
            rule,
            sourceThought,
            intensityDelta,
            Mathf.Clamp(rule.PriorityDelta, -1, 1),
            formationMultiplier,
            opinionDeltaModifier,
            strength);
    }

    private static WorkerThoughtPriority ApplyWorkerThoughtInfluencePriorityDelta(WorkerThoughtPriority priority, int priorityDelta)
    {
        if (priorityDelta == 0)
        {
            return priority;
        }

        int next = Mathf.Clamp((int)priority + Mathf.Clamp(priorityDelta, -1, 1), (int)WorkerThoughtPriority.Low, (int)WorkerThoughtPriority.Critical);
        return (WorkerThoughtPriority)next;
    }

    private static string NormalizeWorkerThoughtInfluenceKey(string thoughtKey)
    {
        if (string.IsNullOrWhiteSpace(thoughtKey))
        {
            return string.Empty;
        }

        return thoughtKey switch
        {
            "need_meal_critical_known_place" => "need_meal_critical",
            "need_sleep_critical_known_place" => "need_sleep_critical",
            "need_leisure_critical_known_place" => "need_leisure_critical",
            "no_job_warning_known_place" => "no_job_warning",
            _ => thoughtKey
        };
    }

    private static bool DoesWorkerThoughtInfluenceKeyMatch(string actualKey, string normalizedExpectedKey)
    {
        return !string.IsNullOrWhiteSpace(normalizedExpectedKey) &&
               string.Equals(NormalizeWorkerThoughtInfluenceKey(actualKey), normalizedExpectedKey, System.StringComparison.Ordinal);
    }

    private static void UpsertWorkerThoughtInfluencePlaceholder(
        List<WorkerThoughtPlaceholder> placeholders,
        WorkerThoughtInfluenceDirection direction)
    {
        if (placeholders == null)
        {
            return;
        }

        string suffixKey = direction switch
        {
            WorkerThoughtInfluenceDirection.Relief => "relief",
            WorkerThoughtInfluenceDirection.Contradict => "contradict",
            WorkerThoughtInfluenceDirection.Stabilize => "stabilize",
            WorkerThoughtInfluenceDirection.Dampen => "dampen",
            _ => "amplify"
        };

        for (int i = 0; i < placeholders.Count; i++)
        {
            WorkerThoughtPlaceholder existing = placeholders[i];
            if (existing != null &&
                string.Equals(existing.Key, WorkerThoughtInfluencePlaceholderKey, System.StringComparison.Ordinal))
            {
                existing.SubjectKey = suffixKey;
                return;
            }
        }

        placeholders.Add(new WorkerThoughtPlaceholder
        {
            Key = WorkerThoughtInfluencePlaceholderKey,
            SubjectType = WorkerThoughtSubjectType.Text,
            SubjectKey = suffixKey,
            FallbackLabel = string.Empty
        });
    }

    private static string FormatWorkerThoughtInfluencePlaceholder(string suffixKey, bool ru)
    {
        return suffixKey switch
        {
            "relief" => ru
                ? "\u041d\u0435\u0434\u0430\u0432\u043d\u0438\u0439 \u043e\u043f\u044b\u0442 \u043d\u0435\u043c\u043d\u043e\u0433\u043e \u0441\u043c\u044f\u0433\u0447\u0430\u0435\u0442 \u0440\u0435\u0430\u043a\u0446\u0438\u044e."
                : "Recent experience softens the reaction a little.",
            "contradict" => ru
                ? "\u041f\u0440\u043e\u0448\u043b\u044b\u0439 \u043e\u043f\u044b\u0442 \u0434\u0435\u043b\u0430\u0435\u0442 \u0447\u0443\u0432\u0441\u0442\u0432\u043e \u043f\u0440\u043e\u0442\u0438\u0432\u043e\u0440\u0435\u0447\u0438\u0432\u044b\u043c."
                : "Past experience makes this feel conflicted.",
            "stabilize" => ru
                ? "\u0421\u0442\u0430\u0431\u0438\u043b\u044c\u043d\u044b\u0439 \u0440\u0438\u0442\u043c \u043f\u043e\u043c\u043e\u0433\u0430\u0435\u0442 \u043d\u0435 \u0440\u0430\u0437\u0433\u043e\u043d\u044f\u0442\u044c \u0442\u0440\u0435\u0432\u043e\u0433\u0443."
                : "A stable rhythm helps keep the reaction grounded.",
            "dampen" => ru
                ? "\u041f\u0440\u043e\u0448\u043b\u044b\u0439 \u043e\u043f\u044b\u0442 \u0441\u043b\u0435\u0433\u043a\u0430 \u0441\u043c\u044f\u0433\u0447\u0430\u0435\u0442 \u044d\u0442\u0443 \u043c\u044b\u0441\u043b\u044c."
                : "Past experience slightly softens this thought.",
            _ => ru
                ? "\u041f\u0440\u043e\u0448\u043b\u044b\u0439 \u043e\u043f\u044b\u0442 \u0441\u0432\u044f\u0437\u044b\u0432\u0430\u0435\u0442 \u044d\u0442\u043e \u0441 \u043d\u0435\u0434\u0430\u0432\u043d\u0438\u043c\u0438 \u0441\u043e\u0431\u044b\u0442\u0438\u044f\u043c\u0438."
                : "Past experience connects this with recent events."
        };
    }
}
