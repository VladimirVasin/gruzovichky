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

    private static readonly WorkerThoughtInfluenceRule[] WorkerThoughtInfluenceRules = BuildWorkerThoughtInfluenceRules();

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

    private static WorkerThoughtInfluenceRule[] BuildWorkerThoughtInfluenceRules()
    {
        return new[]
        {
            Rule("low_money", "service_unaffordable", WorkerThoughtInfluenceDirection.Amplify, 10, 1, 0.85f, 48f, -2, "money pressure was confirmed by an unaffordable service", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Frugal, 4), Trait(WorkerTraitKind.Anxious, 3))),
            Rule("low_money", "affect_financial_pressure", WorkerThoughtInfluenceDirection.Amplify, 12, 1, 0.82f, 48f, -2, "old low-money pressure makes the new pressure feel expected", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Frugal, 3), Trait(WorkerTraitKind.Anxious, 5))),
            Rule("low_money", "no_job_warning", WorkerThoughtInfluenceDirection.Amplify, 8, 1, 0.9f, 36f, -1, "without money, missing work feels dangerous faster", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Dutiful, 3))),
            Rule("low_money", "salary_paid", WorkerThoughtInfluenceDirection.Relief, 6, 0, 0.9f, 72f, 2, "payday is a visible breath after money pressure", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Adaptable, 2))),

            Rule("service_unaffordable", "low_money", WorkerThoughtInfluenceDirection.Amplify, 8, 0, 0.9f, 48f, -1, "an unaffordable service makes poverty concrete", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Frugal, 3))),
            Rule("service_unaffordable", "affect_financial_pressure", WorkerThoughtInfluenceDirection.Amplify, 9, 1, 0.86f, 48f, -2, "service cost turns into financial pressure", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Frugal, 4))),

            Rule("salary_paid", "low_money", WorkerThoughtInfluenceDirection.Dampen, -8, 0, 1.05f, 48f, 1, "recent pay gives a little hope before money panic", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Adaptable, -2))),
            Rule("salary_paid", "affect_financial_pressure", WorkerThoughtInfluenceDirection.Relief, -10, -1, 1.08f, 48f, 2, "income relieves part of the accumulated financial pressure", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Frugal, 2))),
            Rule("salary_paid", "service_unaffordable", WorkerThoughtInfluenceDirection.Contradict, 4, 0, 0.95f, 24f, -1, "the service is still expensive even after payday", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Frugal, 3))),

            Rule("affect_financial_pressure", "service_unaffordable", WorkerThoughtInfluenceDirection.Amplify, 11, 1, 0.84f, 48f, -2, "financial pressure is confirmed by another unaffordable service", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Frugal, 4))),
            Rule("affect_financial_pressure", "no_job_warning", WorkerThoughtInfluenceDirection.Amplify, 9, 1, 0.88f, 48f, -1, "money pressure makes the need for work sharper", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Dutiful, 3), Trait(WorkerTraitKind.Anxious, 4))),
            Rule("affect_financial_pressure", "salary_paid", WorkerThoughtInfluenceDirection.Relief, 8, 0, 0.9f, 72f, 2, "pay removes part of the immediate financial pressure", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Adaptable, 2))),
            Rule("affect_financial_pressure", "affect_stable_routine", WorkerThoughtInfluenceDirection.Dampen, -8, 0, 1.08f, 48f, -2, "financial pressure makes routine feel fragile", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Frugal, 3))),

            Rule("no_job_warning", "starter_job_suggestion", WorkerThoughtInfluenceDirection.Amplify, 8, 0, 0.86f, 48f, 1, "being out of work makes a starter job feel more acceptable", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Dutiful, 3), Trait(WorkerTraitKind.Adaptable, 2))),
            Rule("no_job_warning", "job_found", WorkerThoughtInfluenceDirection.Relief, 10, 0, 0.82f, 72f, 2, "finding work closes the active no-job worry", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Dutiful, 2))),
            Rule("no_job_warning", "low_money", WorkerThoughtInfluenceDirection.Amplify, 8, 1, 0.88f, 48f, -1, "without work, every expense feels more threatening", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Frugal, 3))),
            Rule("no_job_warning", "affect_financial_pressure", WorkerThoughtInfluenceDirection.Amplify, 9, 1, 0.86f, 48f, -2, "unemployment explains the financial pressure", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Anxious, 4))),
            Rule("no_job_warning", "salary_paid", WorkerThoughtInfluenceDirection.Relief, 6, 0, 0.9f, 72f, 2, "pay confirms that work is restoring control", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Dutiful, 2))),

            Rule("starter_job_suggestion", "job_found", WorkerThoughtInfluenceDirection.Relief, 7, 0, 0.88f, 48f, 2, "the suggested simple-work path worked", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Adaptable, 3))),
            Rule("starter_job_suggestion", "no_job_warning", WorkerThoughtInfluenceDirection.Amplify, 5, 0, 0.94f, 24f, -1, "the worker knows the answer but has not acted on it yet", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Stubborn, 2))),

            Rule("job_found", "no_job_warning", WorkerThoughtInfluenceDirection.Dampen, -10, -1, 1.1f, 72f, 2, "recently finding work proves that the problem can be solved", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Adaptable, 3))),
            Rule("job_found", "salary_paid", WorkerThoughtInfluenceDirection.Stabilize, 8, 0, 0.88f, 96f, 2, "work has become a source of money", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Dutiful, 2))),
            Rule("job_found", "affect_financial_pressure", WorkerThoughtInfluenceDirection.Relief, -7, 0, 1.05f, 72f, 1, "having work gives a visible path through money pressure", "MoneyWorkPaidServices",
                Traits(Trait(WorkerTraitKind.Dutiful, 2))),

            Rule("need_meal_critical", "meal_service_good", WorkerThoughtInfluenceDirection.Relief, 9, 0, 0.84f, 24f, 2, "critical hunger makes a real meal feel valuable", "Needs",
                Traits(Trait(WorkerTraitKind.Adaptable, 2))),
            Rule("need_meal_critical", "used_snack", WorkerThoughtInfluenceDirection.Relief, 5, 0, 0.9f, 12f, 1, "a quick snack helps the worker hold on", "Needs",
                Traits(Trait(WorkerTraitKind.Impulsive, 2))),
            Rule("need_meal_critical", "service_unaffordable", WorkerThoughtInfluenceDirection.Amplify, 10, 1, 0.84f, 24f, -2, "hunger plus price feels like the city failed a basic need", "Needs",
                Traits(Trait(WorkerTraitKind.Frugal, 3), Trait(WorkerTraitKind.Anxious, 3))),
            Rule("meal_service_good", "need_meal_critical", WorkerThoughtInfluenceDirection.Dampen, -7, 0, 1.05f, 48f, 1, "remembering a place to eat lowers panic", "Needs",
                Traits(Trait(WorkerTraitKind.Adaptable, 2))),
            Rule("meal_service_good", "stable_life", WorkerThoughtInfluenceDirection.Stabilize, 5, 0, 0.94f, 48f, 1, "regular food makes daily life feel organized", "Needs",
                Traits(Trait(WorkerTraitKind.Meticulous, 1))),
            Rule("used_snack", "need_meal_critical", WorkerThoughtInfluenceDirection.Dampen, -4, 0, 1.02f, 12f, 0, "a snack is remembered as a short-term fallback", "Needs",
                Traits(Trait(WorkerTraitKind.Impulsive, 1))),
            Rule("used_snack", "service_unaffordable", WorkerThoughtInfluenceDirection.Contradict, 4, 0, 0.98f, 24f, -1, "a snack helped, but it did not replace proper food", "Needs",
                Traits(Trait(WorkerTraitKind.Frugal, 2))),

            Rule("need_sleep_critical", "sleep_service_good", WorkerThoughtInfluenceDirection.Relief, 9, 0, 0.84f, 24f, 2, "critical fatigue makes recovery feel real", "Needs",
                Traits(Trait(WorkerTraitKind.Adaptable, 2))),
            Rule("need_sleep_critical", "home_sleep_good", WorkerThoughtInfluenceDirection.Relief, 10, 0, 0.82f, 48f, 3, "sleeping at home turns recovery into stability", "Needs",
                Traits(Trait(WorkerTraitKind.Dutiful, 2))),
            Rule("need_sleep_critical", "used_coffee", WorkerThoughtInfluenceDirection.Relief, 5, 0, 0.9f, 12f, 1, "coffee helps briefly but does not solve sleep", "Needs",
                Traits(Trait(WorkerTraitKind.Impulsive, 2))),
            Rule("sleep_service_good", "need_sleep_critical", WorkerThoughtInfluenceDirection.Dampen, -7, 0, 1.05f, 48f, 1, "the worker remembers where recovery is possible", "Needs",
                Traits(Trait(WorkerTraitKind.Adaptable, 2))),
            Rule("sleep_service_good", "stable_life", WorkerThoughtInfluenceDirection.Stabilize, 6, 0, 0.94f, 48f, 1, "proper sleep makes the day feel manageable", "Needs",
                Traits(Trait(WorkerTraitKind.Dutiful, 1))),
            Rule("home_sleep_good", "need_sleep_critical", WorkerThoughtInfluenceDirection.Dampen, -9, 0, 1.08f, 72f, 2, "having a place to sleep softens fatigue panic", "Needs",
                Traits(Trait(WorkerTraitKind.Adaptable, 2))),
            Rule("home_sleep_good", "affect_family_anxiety", WorkerThoughtInfluenceDirection.Dampen, -5, 0, 1.04f, 48f, 1, "home as a base softens family pressure", "Needs",
                Traits(Trait(WorkerTraitKind.Dutiful, 2))),
            Rule("used_coffee", "need_sleep_critical", WorkerThoughtInfluenceDirection.Contradict, 5, 0, 0.96f, 12f, -1, "coffee was only a temporary workaround", "Needs",
                Traits(Trait(WorkerTraitKind.Anxious, 2))),
            Rule("used_coffee", "salary_paid", WorkerThoughtInfluenceDirection.Stabilize, 3, 0, 0.98f, 24f, 1, "the short push helped the worker reach pay", "Needs",
                Traits(Trait(WorkerTraitKind.Dutiful, 1))),

            Rule("need_leisure_critical", "leisure_service_good", WorkerThoughtInfluenceDirection.Relief, 8, 0, 0.86f, 24f, 2, "high tension makes successful rest stand out", "Needs",
                Traits(Trait(WorkerTraitKind.Adaptable, 2))),
            Rule("need_leisure_critical", "affect_relief_after_rest", WorkerThoughtInfluenceDirection.Amplify, 7, 0, 0.9f, 24f, 1, "relief feels stronger after leisure was at the limit", "Needs",
                Traits(Trait(WorkerTraitKind.Impulsive, 2))),
            Rule("need_leisure_critical", "affect_hangover", WorkerThoughtInfluenceDirection.Contradict, 8, 1, 0.88f, 24f, -2, "trying to rest created a cost afterwards", "Needs",
                Traits(Trait(WorkerTraitKind.Anxious, 2)),
                Weaknesses(Weakness(WorkerWeaknessKind.Alcoholism, 4))),
            Rule("leisure_service_good", "need_leisure_critical", WorkerThoughtInfluenceDirection.Dampen, -6, 0, 1.04f, 48f, 1, "the worker remembers a place to unwind", "Needs",
                Traits(Trait(WorkerTraitKind.Adaptable, 2))),
            Rule("leisure_service_good", "affect_relief_after_rest", WorkerThoughtInfluenceDirection.Amplify, 6, 0, 0.92f, 24f, 1, "successful leisure reinforces relief", "Needs",
                weaknesses: Weaknesses(Weakness(WorkerWeaknessKind.Alcoholism, 2), Weakness(WorkerWeaknessKind.Gambling, 2))),
            Rule("leisure_service_good", "affect_hangover", WorkerThoughtInfluenceDirection.Contradict, 6, 0, 0.94f, 24f, -1, "a good leisure place can still leave an unpleasant aftertaste", "Needs",
                weaknesses: Weaknesses(Weakness(WorkerWeaknessKind.Alcoholism, 4))),

            Rule("affect_relief_after_rest", "need_leisure_critical", WorkerThoughtInfluenceDirection.Dampen, -6, 0, 1.04f, 48f, 1, "recent relief proves that rest can help again", "Needs",
                Traits(Trait(WorkerTraitKind.Adaptable, 2))),
            Rule("affect_relief_after_rest", "affect_hangover", WorkerThoughtInfluenceDirection.Contradict, 6, 0, 0.94f, 24f, -1, "relief conflicts with the bad aftertaste of rest", "Needs",
                weaknesses: Weaknesses(Weakness(WorkerWeaknessKind.Alcoholism, 3))),
            Rule("affect_relief_after_rest", "stable_life", WorkerThoughtInfluenceDirection.Stabilize, 5, 0, 0.94f, 48f, 1, "rest restored some stability reserve", "Needs",
                Traits(Trait(WorkerTraitKind.Adaptable, 2))),
            Rule("affect_hangover", "need_sleep_critical", WorkerThoughtInfluenceDirection.Amplify, 10, 1, 0.84f, 24f, -2, "hangover makes fatigue feel physical and immediate", "Needs",
                Traits(Trait(WorkerTraitKind.Anxious, 3)),
                Weaknesses(Weakness(WorkerWeaknessKind.Alcoholism, 4))),
            Rule("affect_hangover", "leisure_service_good", WorkerThoughtInfluenceDirection.Contradict, 5, 0, 0.96f, 48f, -1, "the bar may help, but the morning after is remembered", "Needs",
                weaknesses: Weaknesses(Weakness(WorkerWeaknessKind.Alcoholism, 2))),
            Rule("affect_hangover", "affect_relief_after_rest", WorkerThoughtInfluenceDirection.Contradict, 5, 0, 0.96f, 48f, -1, "relief is colored by the risk of consequences", "Needs",
                weaknesses: Weaknesses(Weakness(WorkerWeaknessKind.Alcoholism, 3))),
            Rule("affect_hangover", "affect_financial_pressure", WorkerThoughtInfluenceDirection.Amplify, 7, 0, 0.9f, 24f, -2, "bad rest plus spending becomes a money sting", "Needs",
                Traits(Trait(WorkerTraitKind.Frugal, 3))),
            Rule("affect_hangover", "service_unaffordable", WorkerThoughtInfluenceDirection.Amplify, 6, 0, 0.92f, 24f, -1, "after spending on rest, unaffordable services feel deserved", "Needs",
                Traits(Trait(WorkerTraitKind.Frugal, 3)))
        };
    }

    private static WorkerThoughtInfluenceRule Rule(
        string source,
        string target,
        WorkerThoughtInfluenceDirection direction,
        int intensityDelta,
        int priorityDelta,
        float formationTimeMultiplier,
        float windowHours,
        int opinionDeltaModifier,
        string reasonEn,
        string group,
        WorkerThoughtInfluenceTraitModifier[] traits = null,
        WorkerThoughtInfluenceWeaknessModifier[] weaknesses = null)
    {
        return new WorkerThoughtInfluenceRule
        {
            SourceThoughtKey = source,
            TargetThoughtKey = target,
            Direction = direction,
            IntensityDelta = intensityDelta,
            PriorityDelta = priorityDelta,
            FormationTimeMultiplier = formationTimeMultiplier,
            WindowHours = windowHours,
            OpinionDeltaModifier = opinionDeltaModifier,
            TraitModifiers = traits,
            WeaknessModifiers = weaknesses,
            HumanReasonEn = reasonEn,
            Group = group
        };
    }

    private static WorkerThoughtInfluenceTraitModifier Trait(
        WorkerTraitKind trait,
        int intensityDelta,
        int opinionDeltaModifier = 0,
        float formationTimeMultiplier = 1f)
    {
        return new WorkerThoughtInfluenceTraitModifier(trait, intensityDelta, opinionDeltaModifier, formationTimeMultiplier);
    }

    private static WorkerThoughtInfluenceTraitModifier[] Traits(params WorkerThoughtInfluenceTraitModifier[] modifiers)
    {
        return modifiers;
    }

    private static WorkerThoughtInfluenceWeaknessModifier Weakness(
        WorkerWeaknessKind weakness,
        int intensityDelta,
        int opinionDeltaModifier = 0,
        float formationTimeMultiplier = 1f)
    {
        return new WorkerThoughtInfluenceWeaknessModifier(weakness, intensityDelta, opinionDeltaModifier, formationTimeMultiplier);
    }

    private static WorkerThoughtInfluenceWeaknessModifier[] Weaknesses(params WorkerThoughtInfluenceWeaknessModifier[] modifiers)
    {
        return modifiers;
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
        if (ShouldShowWorkerThoughtInfluenceSuffix(strongest))
        {
            UpsertWorkerThoughtInfluencePlaceholder(placeholders, strongest.Rule.Direction);
        }

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

    private static bool ShouldShowWorkerThoughtInfluenceSuffix(WorkerThoughtInfluenceCandidate candidate)
    {
        if (candidate.Rule == null)
        {
            return false;
        }

        return Mathf.Abs(candidate.IntensityDelta) >= 7 ||
               candidate.Rule.Direction is WorkerThoughtInfluenceDirection.Contradict or WorkerThoughtInfluenceDirection.Stabilize;
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
