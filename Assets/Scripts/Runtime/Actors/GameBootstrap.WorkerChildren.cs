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

            if (family.ChildIds.Count > 0)
            {
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

            if (!IsWorkerFamilyLivingInHouse(family))
            {
                family.NextChildBirthWorldHour = now + WorkerChildBirthRetryHours;
                SessionDebugLogger.Log(
                    "FAMILY",
                    $"Family #{family.Id} child birth delayed: family is not settled in house #{family.HouseIndex}; retry in {WorkerChildBirthRetryHours:0}h.");
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
        if (family == null || family.ChildIds.Count > 0 || !IsValidPersonalHouseIndex(family.HouseIndex))
        {
            return;
        }

        WorkerGender gender = Random.value < 0.5f ? WorkerGender.Female : WorkerGender.Male;
        WorkerChild child = new()
        {
            Id = nextWorkerChildId++,
            FamilyId = family.Id,
            HouseIndex = family.HouseIndex,
            Gender = gender,
            Name = GenerateWorkerChildName(family, gender),
            BornDay = currentDay,
            BornWorldHour = now,
            YardLateralOffset = Random.Range(-0.62f, 0.62f),
            YardDepthOffset = Random.Range(-0.16f, 0.34f),
            AnimationPhase = Random.Range(0f, Mathf.PI * 2f)
        };

        workerChildren.Add(child);
        family.ChildIds.Add(child.Id);
        family.BirthJoyUntilDay = currentDay + 2;
        family.Happiness = Mathf.Clamp(family.Happiness + 8, 0, 100);
        family.LastHappinessDelta = 8;
        family.LastHappinessReason = "New child";
        CreateWorkerChildVisual(child);

        isDriversScreenDirty = true;
        isFleetScreenDirty = true;
        SessionDebugLogger.Log(
            "FAMILY",
            $"Family #{family.Id} welcomed child #{child.Id} {child.Name} in house #{family.HouseIndex} on day {currentDay}.");
        PushFeedEvent(
            $"{child.Name} was born in {FormatWorkerFamilyMemberNames(family, false)}'s family.",
            $"\u0412 \u0441\u0435\u043c\u044c\u0435 {FormatWorkerFamilyMemberNames(family, true)} \u043f\u043e\u044f\u0432\u0438\u043b\u0441\u044f \u0440\u0435\u0431\u0435\u043d\u043e\u043a: {child.Name}.",
            FeedEventType.Success);
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
        Vector3 position = center + roadDir * (1.45f + child.YardDepthOffset) + rightDir * child.YardLateralOffset;
        position.y = SampleTerrainHeight(position.x, position.z) + 0.02f;
        return position;
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
}
