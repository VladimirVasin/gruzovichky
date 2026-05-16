using UnityEngine;
using UnityEngine.Rendering;

public partial class GameBootstrap
{
    private void UpdateWorkerFamilyChildBirths(float now)
    {
        for (int i = workerFamilies.Count - 1; i >= 0; i--)
        {
            WorkerFamily family = workerFamilies[i];
            if (family == null)
            {
                workerFamilies.RemoveAt(i);
                continue;
            }

            int childCount = CountWorkerFamilyChildren(family.Id);
            if (childCount >= MaxWorkerFamilyChildren)
            {
                family.NextChildBirthWorldHour = 0f;
                continue;
            }

            if (family.NextChildBirthWorldHour <= 0f)
            {
                family.NextChildBirthWorldHour = now + Random.Range(WorkerChildBirthMinHours, WorkerChildBirthMaxHours);
                continue;
            }

            if (now < family.NextChildBirthWorldHour)
            {
                continue;
            }

            if (family.LastChildBornWorldHour > 0f && now - family.LastChildBornWorldHour < WorkerChildBirthMinHours)
            {
                family.NextChildBirthWorldHour = family.LastChildBornWorldHour + WorkerChildBirthMinHours;
                continue;
            }

            if (!IsWorkerFamilyLivingInHouse(family))
            {
                family.NextChildBirthWorldHour = now + WorkerChildBirthRetryHours;
                SessionDebugLogger.Log(
                    "FAMILY",
                    $"Family #{family.Id} child birth delayed: family is not settled in house #{family.HouseIndex}; retry in {WorkerChildBirthRetryHours:0}h.");
                continue;
            }

            int readiness = CalculateWorkerFamilyNextChildReadiness(family, out string readinessReason);
            if (readiness < WorkerFamilyNextChildReadinessThreshold)
            {
                family.NextChildBirthWorldHour = now + WorkerChildBirthRetryHours;
                SessionDebugLogger.Log(
                    "FAMILY",
                    $"Family #{family.Id} next-child readiness delayed: score={readiness}, threshold={WorkerFamilyNextChildReadinessThreshold}, children={childCount}/{MaxWorkerFamilyChildren}, reason={readinessReason}; retry in {WorkerChildBirthRetryHours:0}h.");
                continue;
            }

            CreateWorkerFamilyChild(family, now);
        }
    }

    private bool IsWorkerFamilyLivingInHouse(WorkerFamily family)
    {
        if (family == null || !IsValidPersonalHouseIndex(family.HouseIndex))
        {
            return false;
        }

        int adultResidents = 0;
        for (int i = 0; i < family.MemberWorkerIds.Count; i++)
        {
            DriverAgent member = GetDriverAgentById(family.MemberWorkerIds[i]);
            if (member != null &&
                member.FamilyId == family.Id &&
                !member.HasDepartedTown &&
                !member.IsLeavingTown &&
                member.AssignedPersonalHouseIndex == family.HouseIndex)
            {
                adultResidents++;
            }
        }

        return adultResidents >= 2;
    }

    private void CreateWorkerFamilyChild(WorkerFamily family, float now)
    {
        if (family == null || !IsValidPersonalHouseIndex(family.HouseIndex))
        {
            return;
        }

        int childSlot = CountWorkerFamilyChildren(family.Id);
        if (childSlot >= MaxWorkerFamilyChildren)
        {
            family.NextChildBirthWorldHour = 0f;
            return;
        }

        WorkerGender gender = Random.value < 0.5f ? WorkerGender.Female : WorkerGender.Male;
        float slotT = MaxWorkerFamilyChildren <= 1 ? 0.5f : childSlot / (float)(MaxWorkerFamilyChildren - 1);
        WorkerChild child = new()
        {
            Id = nextWorkerChildId++,
            FamilyId = family.Id,
            HouseIndex = family.HouseIndex,
            Gender = gender,
            Stage = WorkerChildStage.Baby,
            Name = GenerateWorkerChildName(family, gender),
            BornDay = currentDay,
            BornWorldHour = now,
            StageStartedDay = currentDay,
            NextStageDay = currentDay + GetWorkerChildStageDurationDays(WorkerChildStage.Baby),
            YardLateralOffset = Mathf.Lerp(-0.58f, 0.58f, slotT) + Random.Range(-0.10f, 0.10f),
            YardDepthOffset = Random.Range(-0.16f, 0.34f),
            AnimationPhase = Random.Range(0f, Mathf.PI * 2f)
        };

        workerChildren.Add(child);
        family.ChildIds.Add(child.Id);
        family.LastChildBornWorldHour = now;
        family.NextChildBirthWorldHour = CountWorkerFamilyChildren(family.Id) < MaxWorkerFamilyChildren
            ? now + Random.Range(WorkerChildBirthMinHours, WorkerChildBirthMaxHours)
            : 0f;
        family.BirthJoyUntilDay = currentDay + 2;
        family.Happiness = Mathf.Clamp(family.Happiness + 8, 0, 100);
        family.LastHappinessDelta = 8;
        family.LastHappinessReason = "New child";
        CreateWorkerChildVisual(child);
        RecordWorkerChildBirthThoughts(family, child);

        isDriversScreenDirty = true;
        isFleetScreenDirty = true;
        SessionDebugLogger.Log(
            "FAMILY",
            $"Family #{family.Id} welcomed child #{child.Id} {child.Name} in house #{family.HouseIndex} on day {currentDay}; children={CountWorkerFamilyChildren(family.Id)}/{MaxWorkerFamilyChildren}.");
        PushFeedEvent(
            $"{child.Name} was born in {FormatWorkerFamilyMemberNames(family, false)}'s family.",
            $"\u0412 \u0441\u0435\u043c\u044c\u0435 {FormatWorkerFamilyMemberNames(family, true)} \u043f\u043e\u044f\u0432\u0438\u043b\u0441\u044f \u0440\u0435\u0431\u0435\u043d\u043e\u043a: {child.Name}.",
            FeedEventType.Success);
    }

    private void RecordWorkerChildBirthThoughts(WorkerFamily family, WorkerChild child)
    {
        if (family == null || child == null)
        {
            return;
        }

        for (int i = 0; i < family.MemberWorkerIds.Count; i++)
        {
            DriverAgent adult = GetDriverAgentById(family.MemberWorkerIds[i]);
            if (adult == null || adult.HasDepartedTown || adult.IsLeavingTown)
            {
                continue;
            }

            RecordWorkerThought(
                adult,
                WorkerThoughtKind.Family,
                WorkerThoughtTone.Positive,
                95,
                "child_born",
                new[]
                {
                    ThoughtChild("child", child),
                    ThoughtFamily("family", family)
                },
                WorkerThoughtSubjectType.Child,
                child.Id,
                null,
                child.Name,
                18,
                $"child_born|{child.Id}",
                72f);
        }
    }

    private void UpdateWorkerChildStages()
    {
        for (int i = workerChildren.Count - 1; i >= 0; i--)
        {
            WorkerChild child = workerChildren[i];
            if (child == null)
            {
                workerChildren.RemoveAt(i);
                continue;
            }

            EnsureWorkerChildStageSchedule(child);
            while (child.Stage != WorkerChildStage.YoungAdult &&
                   child.NextStageDay > 0 &&
                   currentDay >= child.NextStageDay)
            {
                if (AdvanceWorkerChildStage(child))
                {
                    break;
                }
            }
        }
    }

    private void EnsureWorkerChildStageSchedule(WorkerChild child)
    {
        if (child == null)
        {
            return;
        }

        if (child.StageStartedDay <= 0)
        {
            child.StageStartedDay = Mathf.Max(1, child.BornDay);
        }

        if (child.NextStageDay <= 0 && child.Stage != WorkerChildStage.YoungAdult)
        {
            child.NextStageDay = child.StageStartedDay + GetWorkerChildStageDurationDays(child.Stage);
        }
    }

    private bool AdvanceWorkerChildStage(WorkerChild child)
    {
        WorkerChildStage nextStage = GetNextWorkerChildStage(child.Stage);
        if (nextStage == WorkerChildStage.YoungAdult)
        {
            CompleteWorkerChildYoungAdultTransition(child);
            return true;
        }

        child.Stage = nextStage;
        child.StageStartedDay = currentDay;
        child.NextStageDay = currentDay + GetWorkerChildStageDurationDays(nextStage);
        isDriversScreenDirty = true;
        isFleetScreenDirty = true;
        SessionDebugLogger.Log(
            "FAMILY",
            $"Child #{child.Id} {child.Name} advanced to {nextStage}; next stage day={child.NextStageDay}.");
        return false;
    }

    private void CompleteWorkerChildYoungAdultTransition(WorkerChild child)
    {
        if (child == null)
        {
            return;
        }

        WorkerFamily family = GetWorkerFamilyById(child.FamilyId);
        RemoveWorkerChildIdFromFamily(family, child.Id);
        DestroyWorkerChildVisual(child);
        workerChildren.Remove(child);

        bool staysInTown = locations.ContainsKey(LocationType.Motel) && Random.value <= WorkerChildStayInTownChance;
        if (staysInTown)
        {
            DriverAgent youngAdult = CreateAndRegisterDriverAgent(
                true,
                child.Gender,
                child.Name,
                18,
                "grew up in town");
            youngAdult.Satisfaction = Mathf.Clamp(youngAdult.Satisfaction + 8, 0, 100);
            youngAdult.Money = Mathf.Max(youngAdult.Money, Random.Range(18, 36));
            PushFeedEvent(
                $"{child.Name} grew up and became a town resident.",
                $"{child.Name} \u0432\u044b\u0440\u043e\u0441 \u0438 \u0441\u0442\u0430\u043b \u0436\u0438\u0442\u0435\u043b\u0435\u043c \u0433\u043e\u0440\u043e\u0434\u0430.",
                FeedEventType.Success);
            SessionDebugLogger.Log(
                "FAMILY",
                $"Child #{child.Id} {child.Name} became resident #{youngAdult.DriverId} after growing up.");
        }
        else
        {
            PushFeedEvent(
                $"{child.Name} grew up and left for adult life.",
                $"{child.Name} \u0432\u044b\u0440\u043e\u0441 \u0438 \u0443\u0435\u0445\u0430\u043b \u0432\u043e \u0432\u0437\u0440\u043e\u0441\u043b\u0443\u044e \u0436\u0438\u0437\u043d\u044c.",
                FeedEventType.Info);
            SessionDebugLogger.Log("FAMILY", $"Child #{child.Id} {child.Name} left town after growing up.");
        }

        if (family != null && CountWorkerFamilyChildren(family.Id) < MaxWorkerFamilyChildren)
        {
            float now = GetCurrentWorldHour();
            family.NextChildBirthWorldHour = now + Random.Range(WorkerChildBirthMinHours, WorkerChildBirthMaxHours);
        }

        isDriversScreenDirty = true;
        isFleetScreenDirty = true;
    }

    private static WorkerChildStage GetNextWorkerChildStage(WorkerChildStage stage)
    {
        return stage switch
        {
            WorkerChildStage.Baby => WorkerChildStage.Toddler,
            WorkerChildStage.Toddler => WorkerChildStage.Child,
            WorkerChildStage.Child => WorkerChildStage.Teen,
            WorkerChildStage.Teen => WorkerChildStage.YoungAdult,
            _ => WorkerChildStage.YoungAdult
        };
    }

    private static int GetWorkerChildStageDurationDays(WorkerChildStage stage)
    {
        return stage switch
        {
            WorkerChildStage.Baby => WorkerChildBabyStageDays,
            WorkerChildStage.Toddler => WorkerChildToddlerStageDays,
            WorkerChildStage.Child => WorkerChildChildStageDays,
            WorkerChildStage.Teen => WorkerChildTeenStageDays,
            _ => 0
        };
    }

    private string GenerateWorkerChildName(WorkerFamily family, WorkerGender gender)
    {
        string[] firstPool = gender == WorkerGender.Female ? WorkerFemaleFirstNames : WorkerMaleFirstNames;
        string lastName = GetWorkerFamilyLastName(family);
        if (string.IsNullOrWhiteSpace(lastName))
        {
            lastName = WorkerLastNames[Random.Range(0, WorkerLastNames.Length)];
        }

        int startIndex = Random.Range(0, firstPool.Length);
        for (int offset = 0; offset < firstPool.Length; offset++)
        {
            string candidate = $"{firstPool[(startIndex + offset) % firstPool.Length]} {lastName}";
            if (!IsWorkerOrChildNameUsed(candidate))
            {
                return candidate;
            }
        }

        return $"{firstPool[startIndex]} {lastName}";
    }

    private string GetWorkerFamilyLastName(WorkerFamily family)
    {
        if (family == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < family.MemberWorkerIds.Count; i++)
        {
            DriverAgent member = GetDriverAgentById(family.MemberWorkerIds[i]);
            if (TrySplitWorkerFullName(member?.DriverName, out _, out string lastName))
            {
                return lastName;
            }
        }

        return string.Empty;
    }

    private bool IsWorkerOrChildNameUsed(string fullName)
    {
        for (int i = 0; i < driverAgents.Count; i++)
        {
            if (string.Equals(driverAgents[i]?.DriverName, fullName, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        for (int i = 0; i < workerChildren.Count; i++)
        {
            if (string.Equals(workerChildren[i]?.Name, fullName, System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private string FormatWorkerFamilyMemberNames(WorkerFamily family, bool ru)
    {
        if (family == null)
        {
            return ru ? "\u0441\u0435\u043c\u044c\u044f" : "family";
        }

        DriverAgent first = null;
        DriverAgent second = null;
        for (int i = 0; i < family.MemberWorkerIds.Count; i++)
        {
            DriverAgent member = GetDriverAgentById(family.MemberWorkerIds[i]);
            if (member == null)
            {
                continue;
            }

            if (first == null)
            {
                first = member;
            }
            else
            {
                second = member;
                break;
            }
        }

        if (first != null && second != null)
        {
            string separator = ru ? " \u0438 " : " and ";
            return $"{first.DriverName}{separator}{second.DriverName}";
        }

        return first != null ? first.DriverName : ru ? "\u0441\u0435\u043c\u044c\u044f" : "family";
    }

    private void CreateWorkerChildVisual(WorkerChild child)
    {
        if (child == null)
        {
            return;
        }

        if (child.RootObject != null)
        {
            Destroy(child.RootObject);
            child.RootObject = null;
        }

        GameObject root = new($"Child_{child.Id}_{child.Name}");
        if (worldRoot != null)
        {
            root.transform.SetParent(worldRoot, false);
        }

        root.transform.position = GetWorkerChildWorldPosition(child);
        root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        child.RootObject = root;

        child.VisualRoot = new GameObject("ChildVisualRoot").transform;
        child.VisualRoot.SetParent(root.transform, false);

        GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shadow.name = "ChildShadow";
        shadow.transform.SetParent(child.VisualRoot, false);
        shadow.transform.localPosition = new Vector3(0f, -0.01f, 0f);
        shadow.transform.localScale = new Vector3(0.22f, 0.006f, 0.22f);
        Renderer shadowRenderer = shadow.GetComponent<Renderer>();
        shadowRenderer.material = CreateTransparentOverlayMaterial(new Color(0f, 0f, 0f, 0.13f));
        shadowRenderer.shadowCastingMode = ShadowCastingMode.Off;
        shadowRenderer.receiveShadows = false;
        DisableWorkerChildCollider(shadow);

        Color[] shirtColors =
        {
            new Color(0.96f, 0.62f, 0.22f),
            new Color(0.22f, 0.62f, 0.86f),
            new Color(0.72f, 0.40f, 0.82f),
            new Color(0.38f, 0.70f, 0.34f)
        };
        Color shirtColor = shirtColors[(child.Id - 1) % shirtColors.Length];
        Color trousersColor = Color.Lerp(new Color(0.14f, 0.18f, 0.26f), shirtColor, 0.18f);

        GameObject body = CreateWorkerChildPart(
            child.VisualRoot,
            "ChildBody",
            PrimitiveType.Capsule,
            new Vector3(0f, 0.25f, 0f),
            new Vector3(0.15f, 0.23f, 0.15f),
            shirtColor,
            VisualSmoothnessFabric);
        child.BodyTransform = body.transform;

        GameObject head = CreateWorkerChildPart(
            child.VisualRoot,
            "ChildHead",
            PrimitiveType.Sphere,
            new Vector3(0f, 0.61f, 0f),
            new Vector3(0.18f, 0.18f, 0.18f),
            new Color(0.96f, 0.82f, 0.68f),
            VisualSmoothnessSkin);
        child.HeadTransform = head.transform;

        GameObject cap = CreateWorkerChildPart(
            child.VisualRoot,
            "ChildCap",
            PrimitiveType.Cube,
            new Vector3(0f, 0.73f, 0.01f),
            new Vector3(0.20f, 0.05f, 0.20f),
            Color.Lerp(shirtColor, Color.white, 0.12f),
            VisualSmoothnessFabric);
        cap.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

        child.LeftArmTransform = CreateWorkerChildPart(
            child.VisualRoot,
            "ChildLeftArm",
            PrimitiveType.Cube,
            new Vector3(-0.13f, 0.34f, 0f),
            new Vector3(0.055f, 0.22f, 0.055f),
            shirtColor,
            VisualSmoothnessFabric).transform;
        child.RightArmTransform = CreateWorkerChildPart(
            child.VisualRoot,
            "ChildRightArm",
            PrimitiveType.Cube,
            new Vector3(0.13f, 0.34f, 0f),
            new Vector3(0.055f, 0.22f, 0.055f),
            shirtColor,
            VisualSmoothnessFabric).transform;
        child.LeftLegTransform = CreateWorkerChildPart(
            child.VisualRoot,
            "ChildLeftLeg",
            PrimitiveType.Cube,
            new Vector3(-0.06f, 0.09f, 0f),
            new Vector3(0.06f, 0.22f, 0.06f),
            trousersColor,
            VisualSmoothnessFabric).transform;
        child.RightLegTransform = CreateWorkerChildPart(
            child.VisualRoot,
            "ChildRightLeg",
            PrimitiveType.Cube,
            new Vector3(0.06f, 0.09f, 0f),
            new Vector3(0.06f, 0.22f, 0.06f),
            trousersColor,
            VisualSmoothnessFabric).transform;

        CreateWorkerChildPart(
            child.VisualRoot,
            "ChildToy",
            PrimitiveType.Sphere,
            new Vector3(0.21f, 0.08f, 0.12f),
            new Vector3(0.10f, 0.10f, 0.10f),
            new Color(0.96f, 0.84f, 0.20f),
            VisualSmoothnessRubber);
    }

    private GameObject CreateWorkerChildPart(
        Transform parent,
        string name,
        PrimitiveType primitiveType,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        float smoothness)
    {
        GameObject part = GameObject.CreatePrimitive(primitiveType);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        ApplyColor(part, color, smoothness);
        ConfigureShadowVisual(part, smoothness);
        DisableWorkerChildCollider(part);
        return part;
    }

    private static void DisableWorkerChildCollider(GameObject part)
    {
        if (part != null && part.TryGetComponent(out Collider collider))
        {
            Object.Destroy(collider);
        }
    }

    private void UpdateWorkerChildVisuals()
    {
        for (int i = workerChildren.Count - 1; i >= 0; i--)
        {
            WorkerChild child = workerChildren[i];
            if (child == null)
            {
                workerChildren.RemoveAt(i);
                continue;
            }

            WorkerFamily family = GetWorkerFamilyById(child.FamilyId);
            if (family == null || !IsValidPersonalHouseIndex(family.HouseIndex))
            {
                DestroyWorkerChildVisual(child);
                workerChildren.RemoveAt(i);
                continue;
            }

            child.HouseIndex = family.HouseIndex;
            if (child.RootObject == null)
            {
                CreateWorkerChildVisual(child);
            }

            child.RootObject.transform.position = GetWorkerChildWorldPosition(child);
            if (child.VisualRoot != null)
            {
                child.VisualRoot.localScale = Vector3.one * GetWorkerChildStageVisualScale(child.Stage);
            }

            float t = Time.time * 1.7f + child.AnimationPhase;
            if (child.VisualRoot != null)
            {
                child.VisualRoot.localPosition = new Vector3(0f, Mathf.Sin(t) * 0.018f, 0f);
                child.VisualRoot.localRotation = Quaternion.Euler(0f, Mathf.Sin(t * 0.32f) * 10f, Mathf.Sin(t * 0.7f) * 1.8f);
            }

            float swing = Mathf.Sin(t * 1.3f) * 10f;
            if (child.LeftArmTransform != null) child.LeftArmTransform.localRotation = Quaternion.Euler(swing, 0f, 0f);
            if (child.RightArmTransform != null) child.RightArmTransform.localRotation = Quaternion.Euler(-swing, 0f, 0f);
            if (child.LeftLegTransform != null) child.LeftLegTransform.localRotation = Quaternion.Euler(-swing * 0.55f, 0f, 0f);
            if (child.RightLegTransform != null) child.RightLegTransform.localRotation = Quaternion.Euler(swing * 0.55f, 0f, 0f);
            if (child.HeadTransform != null) child.HeadTransform.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.9f) * 4f, 0f, 0f);
        }
    }

    private Vector3 GetWorkerChildWorldPosition(WorkerChild child)
    {
        if (child == null)
        {
            return Vector3.zero;
        }

        int houseIndex = child.HouseIndex;
        WorkerFamily family = GetWorkerFamilyById(child.FamilyId);
        if (!IsValidPersonalHouseIndex(houseIndex) && family != null)
        {
            houseIndex = family.HouseIndex;
        }

        if (!IsValidPersonalHouseIndex(houseIndex))
        {
            return Vector3.zero;
        }

        LocationData house = personalHouses[houseIndex];
        Vector3 center = new((house.Min.x + house.Max.x + 1) * 0.5f, 0f, (house.Min.y + house.Max.y + 1) * 0.5f);
        Vector3 roadPoint = GetCellCenter(house.RoadAccess == default ? house.Anchor : house.RoadAccess);
        Vector3 roadDir = roadPoint - center;
        roadDir.y = 0f;
        if (roadDir.sqrMagnitude < 0.001f)
        {
            roadDir = Vector3.forward;
        }
        else
        {
            roadDir.Normalize();
        }

        Vector3 rightDir = new(roadDir.z, 0f, -roadDir.x);
        Vector3 position = center + roadDir * (GetWorkerChildStageYardDepth(child.Stage) + child.YardDepthOffset) + rightDir * child.YardLateralOffset;
        position.y = SampleTerrainHeight(position.x, position.z) + 0.02f;
        return position;
    }

    private static float GetWorkerChildStageVisualScale(WorkerChildStage stage)
    {
        float stageScale = stage switch
        {
            WorkerChildStage.Baby => 0.56f,
            WorkerChildStage.Toddler => 0.74f,
            WorkerChildStage.Child => 0.92f,
            WorkerChildStage.Teen => 1.12f,
            _ => 1f
        };
        return stageScale * CharacterWorldVisualScale;
    }

    private static float GetWorkerChildStageYardDepth(WorkerChildStage stage)
    {
        return stage switch
        {
            WorkerChildStage.Baby => 1.15f,
            WorkerChildStage.Toddler => 1.32f,
            WorkerChildStage.Child => 1.48f,
            WorkerChildStage.Teen => 1.62f,
            _ => 1.45f
        };
    }

    private void DestroyWorkerChildVisual(WorkerChild child)
    {
        if (child?.RootObject == null)
        {
            return;
        }

        Destroy(child.RootObject);
        child.RootObject = null;
        child.VisualRoot = null;
        child.HeadTransform = null;
        child.BodyTransform = null;
        child.LeftArmTransform = null;
        child.RightArmTransform = null;
        child.LeftLegTransform = null;
        child.RightLegTransform = null;
    }

    private void RemoveWorkerChildrenForFamily(int familyId, string reason)
    {
        for (int i = workerChildren.Count - 1; i >= 0; i--)
        {
            WorkerChild child = workerChildren[i];
            if (child == null || child.FamilyId != familyId)
            {
                continue;
            }

            DestroyWorkerChildVisual(child);
            workerChildren.RemoveAt(i);
            SessionDebugLogger.Log("FAMILY", $"Child #{child.Id} removed with family #{familyId}: {reason}.");
        }
    }

    private void UpdateWorkerChildrenHouseIndex(int familyId, int houseIndex)
    {
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child != null && child.FamilyId == familyId)
            {
                child.HouseIndex = houseIndex;
            }
        }
    }

    private int CountWorkerChildrenInHouse(int houseIndex)
    {
        int count = 0;
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child != null && child.HouseIndex == houseIndex)
            {
                count++;
            }
        }

        return count;
    }

    private WorkerChild GetFirstWorkerFamilyChild(int familyId)
    {
        for (int i = 0; i < workerChildren.Count; i++)
        {
            WorkerChild child = workerChildren[i];
            if (child != null && child.FamilyId == familyId)
            {
                return child;
            }
        }

        return null;
    }

    private string FormatWorkerChildStageLabel(WorkerChildStage stage, bool ru)
    {
        return stage switch
        {
            WorkerChildStage.Baby => ru ? "\u043c\u043b\u0430\u0434\u0435\u043d\u0435\u0446" : "baby",
            WorkerChildStage.Toddler => ru ? "\u043c\u0430\u043b\u044b\u0448" : "toddler",
            WorkerChildStage.Child => ru ? "\u0440\u0435\u0431\u0435\u043d\u043e\u043a" : "child",
            WorkerChildStage.Teen => ru ? "\u043f\u043e\u0434\u0440\u043e\u0441\u0442\u043e\u043a" : "teen",
            WorkerChildStage.YoungAdult => ru ? "\u0432\u0437\u0440\u043e\u0441\u043b\u044b\u0439" : "young adult",
            _ => ru ? "\u0440\u0435\u0431\u0435\u043d\u043e\u043a" : "child"
        };
    }

    private string FormatWorkerChildAgeAndNextStage(WorkerChild child, bool ru)
    {
        if (child == null)
        {
            return string.Empty;
        }

        int ageDays = Mathf.Max(0, currentDay - child.BornDay);
        if (child.Stage == WorkerChildStage.YoungAdult || child.NextStageDay <= 0)
        {
            return ru ? $"{ageDays} \u0434\u043d." : $"{ageDays}d";
        }

        int daysLeft = Mathf.Max(0, child.NextStageDay - currentDay);
        return ru
            ? $"{ageDays} \u0434\u043d., \u0441\u043b\u0435\u0434. \u044d\u0442\u0430\u043f {daysLeft} \u0434."
            : $"{ageDays}d, next in {daysLeft}d";
    }
}
