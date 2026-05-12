using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int PrimarySchoolBaseSeats = 4;
    private const int PrimarySchoolSeatsPerTeacher = 8;
    private const int SecondarySchoolBaseSeats = 3;
    private const int SecondarySchoolSeatsPerTeacher = 6;

    private void UpdateEducationBuildToolUnlocks()
    {
        if (!IsNewGameBuildUnlockProgressionActive() || unlockedBuildTools == null)
        {
            return;
        }

        bool shouldUnlockKindergarten = ShouldUnlockKindergartenForFamilies();
        bool shouldUnlockPrimary = ShouldUnlockPrimarySchoolForFamilies();
        bool shouldUnlockSecondary = ShouldUnlockSecondarySchoolForFamilies();
        if ((!shouldUnlockKindergarten || unlockedBuildTools.Contains(BuildTool.Kindergarten)) &&
            (!shouldUnlockPrimary || unlockedBuildTools.Contains(BuildTool.PrimarySchool)) &&
            (!shouldUnlockSecondary || unlockedBuildTools.Contains(BuildTool.SecondarySchool)))
        {
            return;
        }

        List<BuildTool> newlyUnlocked = new();
        if (shouldUnlockKindergarten)
        {
            UnlockNewGameBuildGroup(newlyUnlocked, BuildTool.Kindergarten);
        }

        if (shouldUnlockPrimary)
        {
            UnlockNewGameBuildGroup(newlyUnlocked, BuildTool.PrimarySchool);
        }

        if (shouldUnlockSecondary)
        {
            UnlockNewGameBuildGroup(newlyUnlocked, BuildTool.SecondarySchool);
        }

        ShowNewGameBuildUnlockFeedback(newlyUnlocked);
    }

    private bool ShouldUnlockKindergartenForFamilies()
    {
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child != null && child.Stage != WorkerChildStage.YoungAdult)
            {
                return true;
            }
        }

        return false;
    }

    private bool ShouldUnlockPrimarySchoolForFamilies()
    {
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child == null)
            {
                continue;
            }

            if (child.Stage == WorkerChildStage.Toddler ||
                child.Stage == WorkerChildStage.Child ||
                child.Stage == WorkerChildStage.Teen)
            {
                return true;
            }
        }

        return false;
    }

    private bool ShouldUnlockSecondarySchoolForFamilies()
    {
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child == null)
            {
                continue;
            }

            if (child.Stage == WorkerChildStage.Child ||
                child.Stage == WorkerChildStage.Teen)
            {
                return true;
            }
        }

        return false;
    }

    private int CountBuiltEducationLocations(LocationType educationType)
    {
        int count = 0;
        foreach (LocationData _ in EnumerateAssignableBuildingLocations(educationType))
        {
            count++;
        }

        return count;
    }

    private int CountEducationAssignedStaff(LocationType educationType)
    {
        return CountLogisticsWorkers(educationType);
    }

    private int GetEducationTotalStaffSlots(LocationType educationType)
    {
        return CountBuiltEducationLocations(educationType) * GetMaxBuildingWorkerSlots(educationType);
    }

    private int GetEducationCapacity(LocationType educationType)
    {
        int built = CountBuiltEducationLocations(educationType);
        int assigned = CountEducationAssignedStaff(educationType);
        return educationType switch
        {
            LocationType.Kindergarten => assigned * KindergartenChildCapacityPerStaff,
            LocationType.PrimarySchool => built * PrimarySchoolBaseSeats + assigned * PrimarySchoolSeatsPerTeacher,
            LocationType.SecondarySchool => built * SecondarySchoolBaseSeats + assigned * SecondarySchoolSeatsPerTeacher,
            _ => 0
        };
    }

    private bool TryGetWorkerChildEducationLocationType(WorkerChild child, out LocationType educationType)
    {
        educationType = default;
        if (child == null)
        {
            return false;
        }

        switch (child.Stage)
        {
            case WorkerChildStage.Toddler:
                educationType = LocationType.Kindergarten;
                return true;
            case WorkerChildStage.Child:
                educationType = LocationType.PrimarySchool;
                return true;
            case WorkerChildStage.Teen:
                educationType = LocationType.SecondarySchool;
                return true;
            default:
                return false;
        }
    }

    private bool IsWorkerChildEligibleForEducation(WorkerChild child)
    {
        if (child == null || child.FamilyId <= 0 || child.HouseIndex < 0)
        {
            return false;
        }

        WorkerFamily family = GetWorkerFamilyById(child.FamilyId);
        return family != null && IsWorkerFamilyLivingInHouse(family);
    }

    private bool IsWorkerChildNeedingEducation(WorkerChild child, LocationType educationType)
    {
        return IsWorkerChildEligibleForEducation(child) &&
               TryGetWorkerChildEducationLocationType(child, out LocationType requiredType) &&
               requiredType == educationType;
    }

    private int CountWorkerChildrenNeedingEducation(LocationType educationType)
    {
        int count = 0;
        for (int i = 0; i < workerChildren.Count; i++)
        {
            if (IsWorkerChildNeedingEducation(workerChildren[i], educationType))
            {
                count++;
            }
        }

        return count;
    }

    private int CountEducationCoveredChildren(LocationType educationType)
    {
        return Mathf.Min(CountWorkerChildrenNeedingEducation(educationType), GetEducationCapacity(educationType));
    }

    private int CountWorkerFamilyEducationNeed(WorkerFamily family, LocationType educationType)
    {
        if (family == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child != null &&
                child.FamilyId == family.Id &&
                IsWorkerChildNeedingEducation(child, educationType))
            {
                count++;
            }
        }

        return count;
    }

    private int CountWorkerFamilyEducationCovered(WorkerFamily family, LocationType educationType)
    {
        if (family == null)
        {
            return 0;
        }

        int capacity = GetEducationCapacity(educationType);
        if (capacity <= 0)
        {
            return 0;
        }

        int consumedCapacity = 0;
        int covered = 0;
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (!IsWorkerChildNeedingEducation(child, educationType))
            {
                continue;
            }

            consumedCapacity++;
            if (consumedCapacity <= capacity && child.FamilyId == family.Id)
            {
                covered++;
            }
        }

        return covered;
    }

    private int CountWorkerFamilyEducationShortfall(WorkerFamily family, LocationType educationType)
    {
        int need = CountWorkerFamilyEducationNeed(family, educationType);
        if (need <= 0)
        {
            return 0;
        }

        return Mathf.Max(0, need - CountWorkerFamilyEducationCovered(family, educationType));
    }

    private bool IsWorkerChildEducationCovered(WorkerChild target)
    {
        if (!TryGetWorkerChildEducationLocationType(target, out LocationType educationType))
        {
            return true;
        }

        int capacity = GetEducationCapacity(educationType);
        if (capacity <= 0)
        {
            return false;
        }

        int consumedCapacity = 0;
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (!IsWorkerChildNeedingEducation(child, educationType))
            {
                continue;
            }

            consumedCapacity++;
            if (child.Id == target.Id)
            {
                return consumedCapacity <= capacity;
            }
        }

        return false;
    }

    private int GetWorkerFamilySchoolPressure(WorkerFamily family)
    {
        if (family == null)
        {
            return 0;
        }

        int primaryShortfall = CountWorkerFamilyEducationShortfall(family, LocationType.PrimarySchool);
        int secondaryShortfall = CountWorkerFamilyEducationShortfall(family, LocationType.SecondarySchool);
        int pressure = primaryShortfall * 4 + secondaryShortfall * 5;
        if (primaryShortfall > 0 && CountBuiltEducationLocations(LocationType.PrimarySchool) <= 0)
        {
            pressure += 2;
        }

        if (secondaryShortfall > 0 && CountBuiltEducationLocations(LocationType.SecondarySchool) <= 0)
        {
            pressure += 2;
        }

        return Mathf.Max(0, pressure);
    }

    private int GetWorkerFamilySchoolReadinessPenalty(WorkerFamily family, int existingChildCount)
    {
        if (family == null || existingChildCount <= 0)
        {
            return 0;
        }

        int penalty = 0;
        int primaryShortfall = CountWorkerFamilyEducationShortfall(family, LocationType.PrimarySchool);
        int secondaryShortfall = CountWorkerFamilyEducationShortfall(family, LocationType.SecondarySchool);
        penalty += primaryShortfall * 10;
        penalty += secondaryShortfall * 12;

        if (CountBuiltEducationLocations(LocationType.PrimarySchool) <= 0)
        {
            penalty += HasWorkerFamilyChildAtStage(family, WorkerChildStage.Toddler, WorkerChildStage.Child, WorkerChildStage.Teen) ? 8 : 4;
        }

        if (CountBuiltEducationLocations(LocationType.SecondarySchool) <= 0 &&
            HasWorkerFamilyChildAtStage(family, WorkerChildStage.Child, WorkerChildStage.Teen))
        {
            penalty += 6;
        }

        return Mathf.Clamp(penalty, 0, 24);
    }

    private bool HasWorkerFamilySchoolCoverage(WorkerFamily family)
    {
        if (family == null)
        {
            return false;
        }

        int schoolNeed =
            CountWorkerFamilyEducationNeed(family, LocationType.PrimarySchool) +
            CountWorkerFamilyEducationNeed(family, LocationType.SecondarySchool);
        if (schoolNeed <= 0)
        {
            return CountBuiltEducationLocations(LocationType.PrimarySchool) > 0;
        }

        int schoolCovered =
            CountWorkerFamilyEducationCovered(family, LocationType.PrimarySchool) +
            CountWorkerFamilyEducationCovered(family, LocationType.SecondarySchool);
        return schoolCovered >= schoolNeed;
    }

    private bool HasWorkerFamilyChildAtStage(WorkerFamily family, params WorkerChildStage[] stages)
    {
        if (family == null || stages == null || stages.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child == null || child.FamilyId != family.Id)
            {
                continue;
            }

            for (int s = 0; s < stages.Length; s++)
            {
                if (child.Stage == stages[s])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private LocationType? GetWorkerFamilyMostNeededEducationLocation(WorkerFamily family)
    {
        if (family == null)
        {
            return null;
        }

        int kindergartenShortfall = CountWorkerFamilyEducationShortfall(family, LocationType.Kindergarten);
        int primaryShortfall = CountWorkerFamilyEducationShortfall(family, LocationType.PrimarySchool);
        int secondaryShortfall = CountWorkerFamilyEducationShortfall(family, LocationType.SecondarySchool);
        if (secondaryShortfall > 0)
        {
            return LocationType.SecondarySchool;
        }

        if (primaryShortfall > 0)
        {
            return LocationType.PrimarySchool;
        }

        if (kindergartenShortfall > 0)
        {
            return LocationType.Kindergarten;
        }

        return null;
    }

    private LocationType? GetWorkerFamilyMostNeededSchoolLocation(WorkerFamily family)
    {
        if (family == null)
        {
            return null;
        }

        if (CountWorkerFamilyEducationShortfall(family, LocationType.SecondarySchool) > 0)
        {
            return LocationType.SecondarySchool;
        }

        if (CountWorkerFamilyEducationShortfall(family, LocationType.PrimarySchool) > 0)
        {
            return LocationType.PrimarySchool;
        }

        return null;
    }

    private string FormatWorkerFamilyEducationLabel(WorkerFamily family, bool ru)
    {
        int schoolNeed =
            CountWorkerFamilyEducationNeed(family, LocationType.PrimarySchool) +
            CountWorkerFamilyEducationNeed(family, LocationType.SecondarySchool);
        if (schoolNeed <= 0)
        {
            return ru ? "\u043f\u043e\u043a\u0430 \u043d\u0435 \u0442\u0440\u0435\u0431\u0443\u0435\u0442\u0441\u044f" : "not needed yet";
        }

        int schoolCovered =
            CountWorkerFamilyEducationCovered(family, LocationType.PrimarySchool) +
            CountWorkerFamilyEducationCovered(family, LocationType.SecondarySchool);
        if (schoolCovered >= schoolNeed)
        {
            return ru ? "\u043c\u0435\u0441\u0442\u0430 \u0435\u0441\u0442\u044c" : "covered";
        }

        LocationType? shortage = GetWorkerFamilyMostNeededSchoolLocation(family);
        string shortageName = shortage.HasValue
            ? FormatEducationLocationName(shortage.Value, ru)
            : (ru ? "\u0448\u043a\u043e\u043b\u044b" : "schools");
        return ru
            ? $"{schoolCovered}/{schoolNeed}, \u043d\u0443\u0436\u043d\u0430 {shortageName}"
            : $"{schoolCovered}/{schoolNeed}, needs {shortageName}";
    }

    private string FormatWorkerChildEducationLabel(WorkerChild child, bool ru)
    {
        if (!TryGetWorkerChildEducationLocationType(child, out LocationType educationType))
        {
            return ru ? "\u0443\u0445\u043e\u0434 \u0434\u043e\u043c\u0430" : "home care";
        }

        string name = FormatEducationLocationName(educationType, ru);
        if (IsWorkerChildEducationCovered(child))
        {
            return ru ? $"{name}: \u043c\u0435\u0441\u0442\u043e \u0435\u0441\u0442\u044c" : $"{name}: covered";
        }

        if (CountBuiltEducationLocations(educationType) <= 0)
        {
            return ru ? $"{name}: \u043d\u0435\u0442 \u0437\u0434\u0430\u043d\u0438\u044f" : $"{name}: no building";
        }

        if (CountEducationAssignedStaff(educationType) <= 0)
        {
            return educationType == LocationType.Kindergarten
                ? ru ? $"{name}: \u043d\u0435\u0442 \u0432\u043e\u0441\u043f\u0438\u0442\u0430\u0442\u0435\u043b\u0435\u0439" : $"{name}: no caregivers"
                : ru ? $"{name}: \u043d\u0435\u0442 \u0443\u0447\u0438\u0442\u0435\u043b\u0435\u0439" : $"{name}: no teachers";
        }

        int covered = CountEducationCoveredChildren(educationType);
        int need = CountWorkerChildrenNeedingEducation(educationType);
        return ru ? $"{name}: {covered}/{need}" : $"{name}: {covered}/{need}";
    }

    private string FormatEducationCoverageLabel(LocationType educationType, bool ru)
    {
        int children = CountWorkerChildrenNeedingEducation(educationType);
        int covered = CountEducationCoveredChildren(educationType);
        return ru
            ? $"{covered}/{children} \u0434\u0435\u0442\u0435\u0439"
            : $"{covered}/{children} children";
    }

    private string FormatEducationLocationName(LocationType educationType, bool ru)
    {
        return educationType switch
        {
            LocationType.Kindergarten => ru ? "\u0434\u0435\u0442\u0441\u0430\u0434" : "kindergarten",
            LocationType.PrimarySchool => ru ? "\u043d\u0430\u0447\u0430\u043b\u044c\u043d\u0430\u044f \u0448\u043a\u043e\u043b\u0430" : "primary school",
            LocationType.SecondarySchool => ru ? "\u0441\u0440\u0435\u0434\u043d\u044f\u044f \u0448\u043a\u043e\u043b\u0430" : "secondary school",
            _ => ru ? "\u043e\u0431\u0440\u0430\u0437\u043e\u0432\u0430\u043d\u0438\u0435" : "education"
        };
    }
}
