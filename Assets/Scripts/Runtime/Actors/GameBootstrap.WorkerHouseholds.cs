using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerFamilyBaseDailyUpkeep = 4;
    private const int WorkerFamilyChildDailyUpkeep = 3;
    private const int KindergartenChildCapacityPerStaff = 5;
    private const int WorkerFamilyLowSavingsThreshold = 25;
    private const int WorkerFamilyComfortSavingsThreshold = 90;

    private void UpdateWorkerHouseholdRuntime()
    {
        for (int i = workerFamilies.Count - 1; i >= 0; i--)
        {
            WorkerFamily family = workerFamilies[i];
            if (family == null || !IsWorkerFamilyLivingInHouse(family))
            {
                continue;
            }

            UpdateWorkerHouseholdSnapshot(family);
            if (currentDay > family.CreatedDay && family.LastDailyUpdateDay != currentDay)
            {
                ApplyWorkerHouseholdDailyUpdate(family);
            }
        }
    }

    private void ApplyWorkerHouseholdDailyUpdate(WorkerFamily family)
    {
        if (family == null)
        {
            return;
        }

        int upkeep = CalculateWorkerFamilyDailyUpkeep(family);
        int paid = CollectWorkerFamilyUpkeep(family, upkeep);
        family.LastDailyUpkeepDay = currentDay;
        family.LastDailyUpkeepAmount = upkeep;
        family.LastDailyUpkeepPaidAmount = paid;
        family.LastDailyUpkeepShortfall = Mathf.Max(0, upkeep - paid);
        UpdateWorkerHouseholdSnapshot(family);
        UpdateWorkerFamilyHappiness(family);
        family.LastDailyUpdateDay = currentDay;

        SessionDebugLogger.Log(
            "FAMILY",
            $"Family #{family.Id} household day update: upkeep=${upkeep}, paid=${paid}, shortfall=${family.LastDailyUpkeepShortfall}, savings=${family.LastAdultMoneyTotal}, happiness={family.Happiness}, delta={family.LastHappinessDelta:+#;-#;0}, reason={family.LastHappinessReason}.");

        isDriversScreenDirty = true;
        isFleetScreenDirty = true;
    }

    private int CalculateWorkerFamilyDailyUpkeep(WorkerFamily family)
    {
        int upkeep = WorkerFamilyBaseDailyUpkeep;
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child != null && child.FamilyId == (family?.Id ?? -1))
            {
                upkeep += GetWorkerChildDailyUpkeep(child);
            }
        }

        return upkeep;
    }

    private int CollectWorkerFamilyUpkeep(WorkerFamily family, int amount)
    {
        if (family == null || amount <= 0)
        {
            return 0;
        }

        List<DriverAgent> adults = GetWorkerFamilyAdultMembers(family);
        int remaining = amount;
        int paid = 0;
        for (int pass = 0; pass < 2 && remaining > 0; pass++)
        {
            int payers = 0;
            for (int i = 0; i < adults.Count; i++)
            {
                if (adults[i].Money > 0)
                {
                    payers++;
                }
            }

            if (payers == 0)
            {
                break;
            }

            int share = Mathf.Max(1, Mathf.CeilToInt(remaining / (float)payers));
            for (int i = 0; i < adults.Count && remaining > 0; i++)
            {
                DriverAgent adult = adults[i];
                if (adult.Money <= 0)
                {
                    continue;
                }

                int contribution = Mathf.Min(adult.Money, share, remaining);
                adult.Money -= contribution;
                remaining -= contribution;
                paid += contribution;
            }
        }

        return paid;
    }

    private void UpdateWorkerHouseholdSnapshot(WorkerFamily family)
    {
        if (family == null)
        {
            return;
        }

        int total = 0;
        List<DriverAgent> adults = GetWorkerFamilyAdultMembers(family);
        for (int i = 0; i < adults.Count; i++)
        {
            total += adults[i].Money;
        }

        family.LastAdultMoneyTotal = total;
    }

    private void UpdateWorkerFamilyHappiness(WorkerFamily family)
    {
        if (family == null)
        {
            return;
        }

        int delta = 0;
        List<string> reasons = new();
        int childCount = CountWorkerFamilyChildren(family.Id);
        int childCareNeedCount = CountWorkerFamilyChildrenNeedingChildCare(family);
        List<DriverAgent> adults = GetWorkerFamilyAdultMembers(family);
        int employedAdults = 0;
        bool hasCriticalNeed = false;
        bool blockedByMoney = false;

        for (int i = 0; i < adults.Count; i++)
        {
            DriverAgent adult = adults[i];
            if (IsWorkerEmployedForMigration(adult))
            {
                employedAdults++;
            }

            if (HasCriticalWorkerNeed(adult))
            {
                hasCriticalNeed = true;
            }

            if (IsWorkerDueButBlockedByMoney(adult))
            {
                blockedByMoney = true;
            }
        }

        if (adults.Count >= 2)
        {
            delta += 2;
            reasons.Add("settled home");
        }

        if (employedAdults == adults.Count && adults.Count > 0)
        {
            delta += 3;
            reasons.Add("stable work");
        }
        else if (employedAdults == 0)
        {
            delta -= 8;
            reasons.Add("no family income");
        }
        else
        {
            delta -= 3;
            reasons.Add("partial family income");
        }

        if (family.LastAdultMoneyTotal >= WorkerFamilyComfortSavingsThreshold)
        {
            delta += 2;
            reasons.Add("comfortable savings");
        }
        else if (family.LastAdultMoneyTotal < WorkerFamilyLowSavingsThreshold)
        {
            delta -= childCount > 0 ? 7 : 4;
            reasons.Add("low household savings");
        }

        if (family.LastDailyUpkeepShortfall > 0)
        {
            delta -= 8;
            reasons.Add("unpaid household upkeep");
        }

        if (childCount > 0)
        {
            int childLoadPenalty = Mathf.Min(8, GetWorkerFamilyChildLoadPressure(family));
            if (childLoadPenalty > 0)
            {
                delta -= childLoadPenalty;
                reasons.Add("child load");
            }
        }

        if (childCareNeedCount > 0)
        {
            int coveredChildren = CountWorkerFamilyChildCareCovered(family);
            if (coveredChildren >= childCareNeedCount)
            {
                delta += 4;
                reasons.Add("child care covered");
            }
            else
            {
                delta -= locations.ContainsKey(LocationType.Kindergarten) ? 4 : 6;
                reasons.Add(GetWorkerFamilyChildCareStressReason(family));
            }
        }

        int schoolPressure = GetWorkerFamilySchoolPressure(family);
        if (schoolPressure > 0)
        {
            delta -= Mathf.Clamp(schoolPressure, 2, 8);
            LocationType? shortage = GetWorkerFamilyMostNeededSchoolLocation(family);
            reasons.Add(shortage.HasValue ? $"missing {shortage.Value}" : "school capacity short");
        }

        if (family.BirthJoyUntilDay >= currentDay)
        {
            delta += 5;
            reasons.Add("new child joy");
        }

        if (hasCriticalNeed)
        {
            delta -= 6;
            reasons.Add("parent critical needs");
        }

        if (blockedByMoney)
        {
            delta -= 4;
            reasons.Add("services unaffordable");
        }

        delta = Mathf.Clamp(delta, -18, 12);
        family.Happiness = Mathf.Clamp(family.Happiness + delta, 0, 100);
        family.LastHappinessDelta = delta;
        family.LastHappinessReason = reasons.Count > 0 ? string.Join(", ", reasons) : "stable household";
    }

    private int GetWorkerFamilySatisfactionDelta(DriverAgent worker, out string reason)
    {
        reason = string.Empty;
        WorkerFamily family = GetWorkerFamilyById(worker?.FamilyId ?? -1);
        if (family == null)
        {
            return 0;
        }

        int delta = 0;
        List<string> reasons = new();
        int childCount = CountWorkerFamilyChildren(family.Id);
        int childCareNeedCount = CountWorkerFamilyChildrenNeedingChildCare(family);

        if (family.Happiness >= 75)
        {
            delta += childCount > 0 ? 4 : 2;
            reasons.Add("happy family");
        }
        else if (family.Happiness <= 35)
        {
            delta -= childCount > 0 ? 7 : 5;
            reasons.Add("family stress");
        }

        if (family.LastDailyUpkeepShortfall > 0)
        {
            delta -= 5;
            reasons.Add("household bills unpaid");
        }

        if (childCareNeedCount > 0 && !IsWorkerFamilyChildCareCovered(family))
        {
            delta -= 3;
            reasons.Add("missing child care");
        }
        else if (GetWorkerFamilySchoolPressure(family) > 0)
        {
            delta -= 3;
            reasons.Add("missing school seats");
        }
        else if (childCount > 0 && family.Happiness >= 55)
        {
            delta += 2;
            reasons.Add("child cared for");
        }

        reason = reasons.Count > 0 ? string.Join(", ", reasons) : "family neutral";
        return Mathf.Clamp(delta, -12, 8);
    }

    private List<DriverAgent> GetWorkerFamilyAdultMembers(WorkerFamily family)
    {
        List<DriverAgent> adults = new();
        if (family == null)
        {
            return adults;
        }

        for (int i = 0; i < family.MemberWorkerIds.Count; i++)
        {
            DriverAgent member = GetDriverAgentById(family.MemberWorkerIds[i]);
            if (member != null &&
                member.FamilyId == family.Id &&
                !member.HasDepartedTown &&
                !member.IsLeavingTown)
            {
                adults.Add(member);
            }
        }

        return adults;
    }

    private WorkerFamily GetWorkerFamilyForHouse(int houseIndex)
    {
        if (!IsValidPersonalHouseIndex(houseIndex))
        {
            return null;
        }

        for (int i = 0; i < workerFamilies.Count; i++)
        {
            WorkerFamily family = workerFamilies[i];
            if (family != null && family.HouseIndex == houseIndex)
            {
                return family;
            }
        }

        return null;
    }

    private int CountWorkerFamilyChildren(int familyId)
    {
        int count = 0;
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child != null && child.FamilyId == familyId)
            {
                count++;
            }
        }

        return count;
    }

    private int CountWorkerFamilyChildrenNeedingChildCare(WorkerFamily family)
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
                IsWorkerChildNeedingChildCare(child))
            {
                count++;
            }
        }

        return count;
    }

    private int CalculateWorkerFamilyNextChildReadiness(WorkerFamily family, out string reason)
    {
        reason = string.Empty;
        if (family == null)
        {
            reason = "family missing";
            return 0;
        }

        List<string> reasons = new();
        int score = 45;
        int childCount = CountWorkerFamilyChildren(family.Id);
        if (childCount >= MaxWorkerFamilyChildren)
        {
            reason = "child slots full";
            return 0;
        }

        List<DriverAgent> adults = GetWorkerFamilyAdultMembers(family);
        int employedAdults = 0;
        int adultMoneyTotal = 0;
        bool hasCriticalNeed = false;
        for (int i = 0; i < adults.Count; i++)
        {
            adultMoneyTotal += adults[i].Money;
            if (IsWorkerEmployedForMigration(adults[i]))
            {
                employedAdults++;
            }

            if (HasCriticalWorkerNeed(adults[i]))
            {
                hasCriticalNeed = true;
            }
        }

        family.LastAdultMoneyTotal = adultMoneyTotal;

        if (adults.Count >= 2)
        {
            score += 8;
            reasons.Add("settled parents");
        }
        else
        {
            score -= 30;
            reasons.Add("missing parent");
        }

        if (adults.Count > 0 && employedAdults == adults.Count)
        {
            score += 18;
            reasons.Add("stable work");
        }
        else if (employedAdults == 0)
        {
            score -= 22;
            reasons.Add("no stable work");
        }
        else
        {
            score += 4;
            reasons.Add("partial work");
        }

        int relationship = GetWorkerFamilyAdultRelationshipScore(family);
        if (relationship >= 70)
        {
            score += 12;
            reasons.Add("strong relationship");
        }
        else if (relationship >= 55)
        {
            score += 6;
            reasons.Add("steady relationship");
        }
        else if (relationship > 0)
        {
            score -= 8;
            reasons.Add("weak relationship");
        }

        if (family.Happiness >= 75)
        {
            score += 16;
            reasons.Add("high family happiness");
        }
        else if (family.Happiness >= 55)
        {
            score += 7;
            reasons.Add("stable family happiness");
        }
        else if (family.Happiness <= 35)
        {
            score -= 24;
            reasons.Add("low family happiness");
        }
        else
        {
            score -= 8;
            reasons.Add("strained family happiness");
        }

        if (adultMoneyTotal >= WorkerFamilyComfortSavingsThreshold)
        {
            score += 14;
            reasons.Add("comfortable savings");
        }
        else if (adultMoneyTotal < WorkerFamilyLowSavingsThreshold)
        {
            score -= 18;
            reasons.Add("low savings");
        }

        int childPenalty = GetWorkerFamilyNextChildLoadPenalty(family);
        if (childPenalty > 0)
        {
            score -= childPenalty;
            reasons.Add($"existing children -{childPenalty}");
        }

        int globalCareNeed = CountWorkerChildrenNeedingChildCare();
        int freeCareCapacity = GetKindergartenChildCapacity() - globalCareNeed;
        if (freeCareCapacity >= 1)
        {
            score += 10;
            reasons.Add("free child-care capacity");
        }
        else if (!locations.ContainsKey(LocationType.Kindergarten))
        {
            score -= childCount > 0 ? 14 : 8;
            reasons.Add("no kindergarten");
        }
        else
        {
            score -= 12;
            reasons.Add("child-care capacity short");
        }

        int schoolPenalty = GetWorkerFamilySchoolReadinessPenalty(family, childCount);
        if (schoolPenalty > 0)
        {
            score -= schoolPenalty;
            reasons.Add($"school readiness -{schoolPenalty}");
        }
        else if (childCount > 0 && HasWorkerFamilySchoolCoverage(family))
        {
            score += 4;
            reasons.Add("school path ready");
        }

        if (family.LastDailyUpkeepShortfall > 0)
        {
            score -= 12;
            reasons.Add("unpaid upkeep");
        }

        if (hasCriticalNeed)
        {
            score -= 14;
            reasons.Add("parent critical needs");
        }

        reason = reasons.Count > 0 ? string.Join(", ", reasons) : "neutral";
        return Mathf.Clamp(score, 0, 100);
    }

    private int GetWorkerFamilyAdultRelationshipScore(WorkerFamily family)
    {
        List<DriverAgent> adults = GetWorkerFamilyAdultMembers(family);
        if (adults.Count < 2)
        {
            return 0;
        }

        WorkerSocialMemory first = FindWorkerSocialMemory(adults[0], adults[1].DriverId);
        WorkerSocialMemory second = FindWorkerSocialMemory(adults[1], adults[0].DriverId);
        int total = 0;
        int count = 0;
        if (first != null)
        {
            total += first.Relationship;
            count++;
        }

        if (second != null)
        {
            total += second.Relationship;
            count++;
        }

        return count > 0 ? Mathf.RoundToInt(total / (float)count) : WorkerFamilyFormationRelationshipFloor;
    }

    private int GetWorkerFamilyNextChildLoadPenalty(WorkerFamily family)
    {
        int penalty = 0;
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child != null && child.FamilyId == (family?.Id ?? -1))
            {
                penalty += child.Stage switch
                {
                    WorkerChildStage.Baby => 18,
                    WorkerChildStage.Toddler => 18,
                    WorkerChildStage.Child => 10,
                    WorkerChildStage.Teen => 4,
                    _ => 0
                };
            }
        }

        return penalty;
    }

    private int GetWorkerFamilyChildLoadPressure(WorkerFamily family)
    {
        int pressure = 0;
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child == null || child.FamilyId != (family?.Id ?? -1))
            {
                continue;
            }

            pressure += child.Stage switch
            {
                WorkerChildStage.Baby => 4,
                WorkerChildStage.Toddler => 4,
                WorkerChildStage.Child => 2,
                WorkerChildStage.Teen => -1,
                _ => 0
            };
        }

        int childCareNeedCount = CountWorkerFamilyChildrenNeedingChildCare(family);
        int childCareShortfall = Mathf.Max(0, childCareNeedCount - CountWorkerFamilyChildCareCovered(family));
        pressure += childCareShortfall * 3;
        if (family != null && family.LastDailyUpkeepShortfall > 0)
        {
            pressure += 2;
        }

        return Mathf.Max(0, pressure);
    }

    private int GetWorkerFamilyChildPressure(WorkerFamily family)
    {
        if (family == null)
        {
            return 0;
        }

        int pressure = GetWorkerFamilyChildLoadPressure(family);
        pressure += GetWorkerFamilySchoolPressure(family);
        if (family.Happiness <= 35)
        {
            pressure += 2;
        }

        return pressure;
    }

    private static int GetWorkerChildDailyUpkeep(WorkerChild child)
    {
        if (child == null)
        {
            return WorkerFamilyChildDailyUpkeep;
        }

        return child.Stage switch
        {
            WorkerChildStage.Baby => 4,
            WorkerChildStage.Toddler => 4,
            WorkerChildStage.Child => WorkerFamilyChildDailyUpkeep,
            WorkerChildStage.Teen => 2,
            _ => WorkerFamilyChildDailyUpkeep
        };
    }

    private int CountBuiltKindergartenLocations()
    {
        int count = 0;
        foreach (LocationData _ in EnumerateAssignableBuildingLocations(LocationType.Kindergarten))
        {
            count++;
        }

        return count;
    }

    private int CountKindergartenAssignedStaff()
    {
        return CountLogisticsWorkers(LocationType.Kindergarten);
    }

    private int GetKindergartenTotalStaffSlots()
    {
        return CountBuiltKindergartenLocations() * GetMaxBuildingWorkerSlots(LocationType.Kindergarten);
    }

    private int GetKindergartenChildCapacity()
    {
        return CountKindergartenAssignedStaff() * KindergartenChildCapacityPerStaff;
    }

    private bool IsWorkerChildNeedingChildCare(WorkerChild child)
    {
        if (child == null || child.FamilyId <= 0 || child.HouseIndex < 0)
        {
            return false;
        }

        WorkerFamily family = GetWorkerFamilyById(child.FamilyId);
        return family != null &&
               IsWorkerFamilyLivingInHouse(family) &&
               child.Stage == WorkerChildStage.Toddler;
    }

    private int CountWorkerChildrenNeedingChildCare()
    {
        int count = 0;
        for (int i = 0; i < workerChildren.Count; i++)
        {
            if (IsWorkerChildNeedingChildCare(workerChildren[i]))
            {
                count++;
            }
        }

        return count;
    }

    private int CountKindergartenCoveredChildren()
    {
        return Mathf.Min(CountWorkerChildrenNeedingChildCare(), GetKindergartenChildCapacity());
    }

    private int CountWorkerFamilyChildCareCovered(WorkerFamily family)
    {
        if (family == null)
        {
            return 0;
        }

        int capacity = GetKindergartenChildCapacity();
        if (capacity <= 0)
        {
            return 0;
        }

        int consumedCapacity = 0;
        int covered = 0;
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (!IsWorkerChildNeedingChildCare(child))
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

    private bool IsWorkerFamilyChildCareCovered(WorkerFamily family)
    {
        int childCareNeedCount = CountWorkerFamilyChildrenNeedingChildCare(family);
        return childCareNeedCount <= 0 || CountWorkerFamilyChildCareCovered(family) >= childCareNeedCount;
    }

    private string GetWorkerFamilyChildCareStressReason(WorkerFamily family)
    {
        if (!locations.ContainsKey(LocationType.Kindergarten))
        {
            return "no kindergarten";
        }

        if (CountKindergartenAssignedStaff() <= 0)
        {
            return "kindergarten unstaffed";
        }

        return "kindergarten capacity short";
    }

    private string FormatWorkerFamilyChildCareLabel(WorkerFamily family, bool ru)
    {
        int childCount = CountWorkerFamilyChildren(family?.Id ?? -1);
        if (childCount <= 0)
        {
            return "\u2014";
        }

        int childCareNeedCount = CountWorkerFamilyChildrenNeedingChildCare(family);
        if (childCareNeedCount <= 0)
        {
            return ru ? "\u043d\u0435 \u0442\u0440\u0435\u0431\u0443\u0435\u0442\u0441\u044f" : "not needed";
        }

        int covered = CountWorkerFamilyChildCareCovered(family);
        if (covered >= childCareNeedCount)
        {
            return ru ? "\u043c\u0435\u0441\u0442\u0430 \u0435\u0441\u0442\u044c" : "covered";
        }

        if (!locations.ContainsKey(LocationType.Kindergarten))
        {
            return ru ? "\u043d\u0435\u0442 \u0434\u0435\u0442\u0441\u0430\u0434\u0430" : "no kindergarten";
        }

        if (CountKindergartenAssignedStaff() <= 0)
        {
            return ru ? "\u043d\u0435\u0442 \u0432\u043e\u0441\u043f\u0438\u0442\u0430\u0442\u0435\u043b\u0435\u0439" : "no caregivers";
        }

        return ru
            ? $"{covered}/{childCareNeedCount}, \u043d\u0435 \u0445\u0432\u0430\u0442\u0430\u0435\u0442 \u043c\u0435\u0441\u0442"
            : $"{covered}/{childCareNeedCount}, needs capacity";
    }

    private string FormatKindergartenCoverageLabel(bool ru)
    {
        return FormatEducationCoverageLabel(LocationType.Kindergarten, ru);
    }

    private string FormatWorkerFamilyHappinessLabel(WorkerFamily family, bool ru)
    {
        if (family == null)
        {
            return "\u2014";
        }

        string mood = family.Happiness >= 75
            ? ru ? "\u0441\u0447\u0430\u0441\u0442\u043b\u0438\u0432\u0430" : "happy"
            : family.Happiness >= 45
                ? ru ? "\u0441\u0442\u0430\u0431\u0438\u043b\u044c\u043d\u0430" : "stable"
                : ru ? "\u043d\u0430\u043f\u0440\u044f\u0436\u0435\u043d\u0430" : "strained";
        return $"{family.Happiness}/100, {mood}";
    }

    private string FormatWorkerFamilyUpkeepLabel(WorkerFamily family, bool ru)
    {
        if (family == null)
        {
            return "\u2014";
        }

        int upkeep = CalculateWorkerFamilyDailyUpkeep(family);
        if (family.LastDailyUpkeepShortfall > 0)
        {
            return ru
                ? $"${upkeep}/\u0434\u0435\u043d\u044c, \u043d\u0435 \u0445\u0432\u0430\u0442\u0438\u043b\u043e ${family.LastDailyUpkeepShortfall}"
                : $"${upkeep}/day, short ${family.LastDailyUpkeepShortfall}";
        }

        return $"${upkeep}/day";
    }
}
