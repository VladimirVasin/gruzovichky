using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class VacancyTutorialSmokeTests
{
    [Test]
    public void VacancyFlowRulesService_SingleShiftVacanciesStillRequireShiftStep()
    {
        Assert.That(VacancyFlowRulesService.RequiresShiftStep(VacancyFlowKind.Production), Is.True);
        Assert.That(VacancyFlowRulesService.RequiresShiftStep(VacancyFlowKind.BusDriver), Is.True);
        Assert.That(VacancyFlowRulesService.RequiresShiftStep(VacancyFlowKind.TruckDriver), Is.True);
        Assert.That(VacancyFlowRulesService.RequiresShiftStep(VacancyFlowKind.Intercity), Is.True);
        Assert.That(VacancyFlowRulesService.RequiresShiftStep(VacancyFlowKind.Service), Is.True);
    }

    [Test]
    public void VacancyFlowRulesService_CurrentFlowDoesNotExposeManualTruckStep()
    {
        Assert.That(VacancyFlowRulesService.RequiresTruckStep(VacancyFlowKind.Production), Is.False);
        Assert.That(VacancyFlowRulesService.RequiresTruckStep(VacancyFlowKind.BusDriver), Is.False);
        Assert.That(VacancyFlowRulesService.RequiresTruckStep(VacancyFlowKind.TruckDriver), Is.False);
        Assert.That(VacancyFlowRulesService.RequiresTruckStep(VacancyFlowKind.Intercity), Is.False);
        Assert.That(VacancyFlowRulesService.RequiresTruckStep(VacancyFlowKind.Service), Is.False);
    }

    [Test]
    public void VacancyFlowRulesService_ReportsCurrentStepFromSelectionState()
    {
        Assert.That(VacancyFlowRulesService.GetCurrentStep(false, true, false, false, false), Is.EqualTo(1));
        Assert.That(VacancyFlowRulesService.GetCurrentStep(false, true, true, false, false), Is.EqualTo(3));
        Assert.That(VacancyFlowRulesService.GetCurrentStep(false, true, true, true, false), Is.EqualTo(2));
        Assert.That(VacancyFlowRulesService.GetCurrentStep(true, true, true, true, true), Is.EqualTo(4));
    }

    [Test]
    public void TutorialGoalLabels_HaveEnglishTextForEveryGoal()
    {
        Type bootstrapType = typeof(GameBootstrap);
        Type goalKindType = bootstrapType.GetNestedType("TutorialGoalKind", BindingFlags.NonPublic);
        MethodInfo labelMethod = bootstrapType.GetMethod("GetTutorialGoalLabelSafe", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(goalKindType, Is.Not.Null);
        Assert.That(labelMethod, Is.Not.Null);

        foreach (object value in Enum.GetValues(goalKindType))
        {
            string label = (string)labelMethod.Invoke(null, new[] { value, false });
            Assert.That(label, Is.Not.Null.And.Not.Empty, $"missing tutorial goal label for {value}");
        }
    }

    [Test]
    public void TutorialGoalVisibility_EachModeShowsAtLeastOneGoal()
    {
        Type bootstrapType = typeof(GameBootstrap);
        Type goalsModeType = bootstrapType.GetNestedType("TutorialGoalsMode", BindingFlags.NonPublic);
        Type goalKindType = bootstrapType.GetNestedType("TutorialGoalKind", BindingFlags.NonPublic);
        FieldInfo modeField = bootstrapType.GetField("tutorialGoalsMode", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo visibleMethod = bootstrapType.GetMethod("IsTutorialGoalVisible", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.That(goalsModeType, Is.Not.Null);
        Assert.That(goalKindType, Is.Not.Null);
        Assert.That(modeField, Is.Not.Null);
        Assert.That(visibleMethod, Is.Not.Null);

        GameObject host = new("TutorialGoalVisibilityTestHost");
        try
        {
            GameBootstrap bootstrap = host.AddComponent<GameBootstrap>();
            foreach (object mode in Enum.GetValues(goalsModeType))
            {
                modeField.SetValue(bootstrap, mode);
                bool anyVisible = false;
                foreach (object goal in Enum.GetValues(goalKindType))
                {
                    if ((bool)visibleMethod.Invoke(bootstrap, new[] { goal }))
                    {
                        anyVisible = true;
                        break;
                    }
                }

                Assert.That(anyVisible, Is.True, $"tutorial mode {mode} has no visible goals");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(host);
        }
    }
}
