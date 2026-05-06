using UnityEngine;

public partial class GameBootstrap
{
    private const int WorkerFamilyStrongRelationshipThreshold = 50;
    private const int WorkerFamilyFormationFamiliarityFloor = 55;
    private const int WorkerFamilyFormationRelationshipFloor = 55;
    private const float WorkerFamilyPendingFormationHours = 72f;
    private const float WorkerFamilyPendingRetryHours = 24f;

    private void OnWorkerMovedIntoPersonalHouse(DriverAgent worker, int houseIndex)
    {
        if (worker == null || !IsValidPersonalHouseIndex(houseIndex) || worker.FamilyId >= 0)
        {
            return;
        }

        CancelPendingWorkerFamilyFormation(worker.DriverId);
        if (TryFindStrongWorkerFamilyPartner(worker, out DriverAgent partner, out int relationship))
        {
            CreateWorkerFamily(worker, partner, houseIndex, $"strong social relationship={relationship}");
            return;
        }

        ScheduleWorkerFamilyFormation(worker, houseIndex);
    }

    private void UpdateWorkerFamilyRuntime()
    {
        float now = GetCurrentWorldHour();
        for (int i = pendingWorkerFamilyFormations.Count - 1; i >= 0; i--)
        {
            WorkerFamilyPendingFormation pending = pendingWorkerFamilyFormations[i];
            DriverAgent worker = pending != null ? GetDriverAgentById(pending.WorkerId) : null;
            if (worker == null || worker.HasDepartedTown || worker.IsLeavingTown || worker.FamilyId >= 0)
            {
                pendingWorkerFamilyFormations.RemoveAt(i);
                continue;
            }

            if (!IsValidPersonalHouseIndex(pending.HouseIndex) ||
                worker.AssignedPersonalHouseIndex != pending.HouseIndex)
            {
                pendingWorkerFamilyFormations.RemoveAt(i);
                continue;
            }

            if (now < pending.DueWorldHour)
            {
                continue;
            }

            if (TryFindPendingWorkerFamilyPartner(worker, out DriverAgent partner, out string reason))
            {
                pendingWorkerFamilyFormations.RemoveAt(i);
                CreateWorkerFamily(worker, partner, pending.HouseIndex, reason);
                continue;
            }

            pending.DueWorldHour = now + WorkerFamilyPendingRetryHours;
            SessionDebugLogger.Log(
                "FAMILY",
                $"{worker.DriverName} family formation delayed: no eligible opposite-gender worker; retry in {WorkerFamilyPendingRetryHours:0}h.");
        }

        UpdateWorkerFamilyChildBirths(now);
        UpdateWorkerHouseholdRuntime();
        UpdateWorkerChildVisuals();
    }

    private void ScheduleWorkerFamilyFormation(DriverAgent worker, int houseIndex)
    {
        if (worker == null || !IsValidPersonalHouseIndex(houseIndex))
        {
            return;
        }

        float now = GetCurrentWorldHour();
        WorkerFamilyPendingFormation pending = FindPendingWorkerFamilyFormation(worker.DriverId);
        if (pending == null)
        {
            pending = new WorkerFamilyPendingFormation { WorkerId = worker.DriverId };
            pendingWorkerFamilyFormations.Add(pending);
        }

        pending.HouseIndex = houseIndex;
        pending.CreatedWorldHour = now;
        pending.DueWorldHour = now + WorkerFamilyPendingFormationHours;
        SessionDebugLogger.Log(
            "FAMILY",
            $"{worker.DriverName} has no strong family partner yet; pending family formation for house #{houseIndex}, due in {WorkerFamilyPendingFormationHours:0}h.");
    }

    private bool TryFindStrongWorkerFamilyPartner(DriverAgent worker, out DriverAgent partner, out int relationship)
    {
        partner = null;
        relationship = int.MinValue;
        if (worker == null)
        {
            return false;
        }

        for (int i = 0; i < worker.SocialMemories.Count; i++)
        {
            WorkerSocialMemory memory = worker.SocialMemories[i];
            if (memory == null || memory.Relationship < WorkerFamilyStrongRelationshipThreshold)
            {
                continue;
            }

            DriverAgent candidate = GetDriverAgentById(memory.OtherWorkerId);
            if (!IsEligibleWorkerFamilyPartner(worker, candidate))
            {
                continue;
            }

            if (memory.Relationship > relationship)
            {
                partner = candidate;
                relationship = memory.Relationship;
            }
        }

        return partner != null;
    }

    private bool TryFindPendingWorkerFamilyPartner(DriverAgent worker, out DriverAgent partner, out string reason)
    {
        partner = null;
        reason = string.Empty;
        if (worker == null)
        {
            return false;
        }

        int bestRelationship = int.MinValue;
        int bestFamiliarity = int.MinValue;
        for (int i = 0; i < worker.SocialMemories.Count; i++)
        {
            WorkerSocialMemory memory = worker.SocialMemories[i];
            DriverAgent candidate = memory != null ? GetDriverAgentById(memory.OtherWorkerId) : null;
            if (memory == null || !IsEligibleWorkerFamilyPartner(worker, candidate))
            {
                continue;
            }

            if (memory.Relationship > bestRelationship ||
                memory.Relationship == bestRelationship && memory.Familiarity > bestFamiliarity)
            {
                partner = candidate;
                bestRelationship = memory.Relationship;
                bestFamiliarity = memory.Familiarity;
            }
        }

        if (partner != null)
        {
            reason = $"best social contact relationship={bestRelationship}, familiarity={bestFamiliarity}";
            return true;
        }

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent candidate = driverAgents[i];
            if (!IsEligibleWorkerFamilyPartner(worker, candidate))
            {
                continue;
            }

            partner = candidate;
            reason = "fallback first opposite-gender worker";
            return true;
        }

        return false;
    }

    private bool IsEligibleWorkerFamilyPartner(DriverAgent worker, DriverAgent candidate)
    {
        return worker != null &&
               candidate != null &&
               worker != candidate &&
               candidate.DriverId > 0 &&
               !candidate.HasDepartedTown &&
               !candidate.IsLeavingTown &&
               !candidate.IsArrivingByBus &&
               candidate.FamilyId < 0 &&
               candidate.Gender != worker.Gender &&
               candidate.LifeGoal != WorkerLifeGoal.BuyHouse &&
               candidate.WalkPhase != DriverRescuePhase.ToPersonalHouseForPurchase;
    }

    private void CreateWorkerFamily(DriverAgent first, DriverAgent second, int houseIndex, string reason)
    {
        if (first == null || second == null || !IsValidPersonalHouseIndex(houseIndex))
        {
            return;
        }

        if (first.FamilyId >= 0 || second.FamilyId >= 0 || first.Gender == second.Gender)
        {
            return;
        }

        if (second.AssignedPersonalHouseIndex != houseIndex &&
            CountPersonalHouseResidents(houseIndex) >= MaxPersonalHouseResidents)
        {
            SessionDebugLogger.Log(
                "FAMILY",
                $"{first.DriverName} family formation skipped: house #{houseIndex} already has {MaxPersonalHouseResidents} residents.");
            return;
        }

        float now = GetCurrentWorldHour();
        WorkerFamily family = new()
        {
            Id = nextWorkerFamilyId++,
            HouseIndex = houseIndex,
            CreatedDay = currentDay,
            CreatedWorldHour = now,
            NextChildBirthWorldHour = now + Random.Range(WorkerChildBirthMinHours, WorkerChildBirthMaxHours),
            LastDailyUpdateDay = currentDay,
            LastDailyUpkeepDay = currentDay,
            LastHappinessReason = "New household"
        };
        family.MemberWorkerIds.Add(first.DriverId);
        family.MemberWorkerIds.Add(second.DriverId);
        workerFamilies.Add(family);

        AssignWorkerToFamilyHouse(first, family);
        AssignWorkerToFamilyHouse(second, family);
        CancelPendingWorkerFamilyFormation(first.DriverId);
        CancelPendingWorkerFamilyFormation(second.DriverId);
        EnsureFamilySocialMemory(first, second);

        isDriversScreenDirty = true;
        isFleetScreenDirty = true;
        SessionDebugLogger.Log(
            "FAMILY",
            $"{first.DriverName} and {second.DriverName} formed family #{family.Id} in house #{houseIndex}; reason={reason}.");
        PushFeedEvent(
            $"{first.DriverName} and {second.DriverName} formed a family.",
            $"{first.DriverName} \u0438 {second.DriverName} \u0441\u0442\u0430\u043b\u0438 \u0441\u0435\u043c\u044c\u0435\u0439.",
            FeedEventType.Success);
    }

    private void AssignWorkerToFamilyHouse(DriverAgent worker, WorkerFamily family)
    {
        if (worker == null || family == null)
        {
            return;
        }

        int oldHouseIndex = worker.AssignedPersonalHouseIndex;
        worker.FamilyId = family.Id;
        worker.AssignedPersonalHouseIndex = family.HouseIndex;
        if (oldHouseIndex != family.HouseIndex)
        {
            ResetWorkerHomeCarParking(worker);
        }
    }

    private void EnsureFamilySocialMemory(DriverAgent first, DriverAgent second)
    {
        float now = GetCurrentWorldHour();
        EnsureFamilySocialMemoryOneWay(first, second, now);
        EnsureFamilySocialMemoryOneWay(second, first, now);
    }

    private void EnsureFamilySocialMemoryOneWay(DriverAgent owner, DriverAgent other, float now)
    {
        if (owner == null || other == null)
        {
            return;
        }

        WorkerSocialMemory memory = FindWorkerSocialMemory(owner, other.DriverId);
        if (memory == null)
        {
            memory = new WorkerSocialMemory { OtherWorkerId = other.DriverId };
            owner.SocialMemories.Add(memory);
        }

        memory.Familiarity = Mathf.Max(memory.Familiarity, WorkerFamilyFormationFamiliarityFloor);
        memory.Relationship = Mathf.Max(memory.Relationship, WorkerFamilyFormationRelationshipFloor);
        memory.InteractionCount++;
        memory.LastInteractionDay = currentDay;
        memory.LastInteractionWorldHour = now;
        memory.NextFamiliarityDecayWorldHour = now + WorkerSocialFamiliarityDecayGraceHours;
        memory.LastKind = WorkerSocialInteractionKind.FamilyFormation;
        memory.LastLocationType = LocationType.PersonalHouse;
        TrimWorkerSocialMemories(owner);
    }

    private void CleanupWorkerFamilyForDeparture(DriverAgent worker)
    {
        if (worker == null)
        {
            return;
        }

        CancelPendingWorkerFamilyFormation(worker.DriverId);
        if (worker.FamilyId < 0)
        {
            return;
        }

        DisbandWorkerFamily(worker.FamilyId, $"{worker.DriverName} left town");
    }

    private void HandleWorkerFamiliesAfterHouseDemolished(int removedHouseIndex)
    {
        for (int i = workerFamilies.Count - 1; i >= 0; i--)
        {
            WorkerFamily family = workerFamilies[i];
            if (family == null)
            {
                workerFamilies.RemoveAt(i);
                continue;
            }

            if (family.HouseIndex == removedHouseIndex)
            {
                DisbandWorkerFamily(family.Id, $"house #{removedHouseIndex} demolished");
            }
            else if (family.HouseIndex > removedHouseIndex)
            {
                family.HouseIndex--;
                UpdateWorkerChildrenHouseIndex(family.Id, family.HouseIndex);
            }
        }

        for (int i = pendingWorkerFamilyFormations.Count - 1; i >= 0; i--)
        {
            WorkerFamilyPendingFormation pending = pendingWorkerFamilyFormations[i];
            if (pending == null || pending.HouseIndex == removedHouseIndex)
            {
                pendingWorkerFamilyFormations.RemoveAt(i);
                continue;
            }

            if (pending.HouseIndex > removedHouseIndex)
            {
                pending.HouseIndex--;
            }
        }
    }

    private void DisbandWorkerFamily(int familyId, string reason)
    {
        WorkerFamily family = GetWorkerFamilyById(familyId);
        if (family == null)
        {
            return;
        }

        RemoveWorkerChildrenForFamily(family.Id, reason);

        for (int i = 0; i < family.MemberWorkerIds.Count; i++)
        {
            DriverAgent member = GetDriverAgentById(family.MemberWorkerIds[i]);
            if (member != null && member.FamilyId == family.Id)
            {
                member.FamilyId = -1;
            }
        }

        workerFamilies.Remove(family);
        isDriversScreenDirty = true;
        SessionDebugLogger.Log("FAMILY", $"Family #{familyId} disbanded: {reason}.");
    }

    private WorkerFamily GetWorkerFamilyById(int familyId)
    {
        if (familyId < 0)
        {
            return null;
        }

        for (int i = 0; i < workerFamilies.Count; i++)
        {
            WorkerFamily family = workerFamilies[i];
            if (family != null && family.Id == familyId)
            {
                return family;
            }
        }

        return null;
    }

    private WorkerFamilyPendingFormation FindPendingWorkerFamilyFormation(int workerId)
    {
        for (int i = 0; i < pendingWorkerFamilyFormations.Count; i++)
        {
            WorkerFamilyPendingFormation pending = pendingWorkerFamilyFormations[i];
            if (pending != null && pending.WorkerId == workerId)
            {
                return pending;
            }
        }

        return null;
    }

    private void CancelPendingWorkerFamilyFormation(int workerId)
    {
        for (int i = pendingWorkerFamilyFormations.Count - 1; i >= 0; i--)
        {
            WorkerFamilyPendingFormation pending = pendingWorkerFamilyFormations[i];
            if (pending != null && pending.WorkerId == workerId)
            {
                pendingWorkerFamilyFormations.RemoveAt(i);
            }
        }
    }

    private bool IsValidPersonalHouseIndex(int houseIndex)
    {
        return houseIndex >= 0 && houseIndex < personalHouses.Count;
    }

    private void ResetWorkerHomeCarParking(DriverAgent worker)
    {
        if (worker == null)
        {
            return;
        }

        worker.HasOwnedCarParking = false;
        if (worker.OwnedCarObject != null)
        {
            Destroy(worker.OwnedCarObject);
            worker.OwnedCarObject = null;
        }
    }

    private string FormatWorkerHomeLabel(DriverAgent worker, bool ru)
    {
        if (worker == null ||
            worker.AssignedPersonalHouseIndex < 0 ||
            worker.AssignedPersonalHouseIndex >= personalHouses.Count)
        {
            return "\u2014";
        }

        string label = personalHouses[worker.AssignedPersonalHouseIndex].Label;
        WorkerFamily family = GetWorkerFamilyById(worker.FamilyId);
        if (family != null)
        {
            DriverAgent partner = null;
            for (int i = 0; i < family.MemberWorkerIds.Count; i++)
            {
                DriverAgent member = GetDriverAgentById(family.MemberWorkerIds[i]);
                if (member != null && member.DriverId != worker.DriverId)
                {
                    partner = member;
                    break;
                }
            }

            if (partner != null)
            {
                string familyLabel = ru
                    ? $"{label}; \u0441\u0435\u043c\u044c\u044f: {partner.DriverName}"
                    : $"{label}; family: {partner.DriverName}";
                WorkerChild child = GetFirstWorkerFamilyChild(family.Id);
                if (child != null)
                {
                    familyLabel += ru
                        ? $"; \u0440\u0435\u0431\u0435\u043d\u043e\u043a: {child.Name}"
                        : $"; child: {child.Name}";
                }

                familyLabel += ru
                    ? $"; \u0441\u0447\u0430\u0441\u0442\u044c\u0435: {family.Happiness}/100"
                    : $"; happiness: {family.Happiness}/100";
                return familyLabel;
            }
        }

        if (FindPendingWorkerFamilyFormation(worker.DriverId) != null)
        {
            return ru ? $"{label}; \u0441\u0435\u043c\u044c\u044f \u0436\u0434\u0435\u0442" : $"{label}; family pending";
        }

        return label;
    }
}
