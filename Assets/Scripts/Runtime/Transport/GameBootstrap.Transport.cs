using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private bool TryHandleTruckSelection(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            return false;
        }

        if (hit.transform == null)
        {
            return false;
        }

        foreach (TruckAgent truckAgent in truckAgents)
        {
            if (truckAgent.TruckObject != null && hit.transform.IsChildOf(truckAgent.TruckObject.transform))
            {
                FocusTruck(truckAgent.TruckNumber);
                return true;
            }
        }

        return false;
    }

    private void ProduceForestWood()
    {
        productionTimer += Time.deltaTime;
        if (productionTimer < ForestProductionInterval)
        {
            return;
        }

        productionTimer = 0f;
        locations[LocationType.Forest].WoodStored += 1;
    }

    private void UpdateTruckMovement()
    {
        if (isTruckInteracting || isDriverRescueActive)
        {
            UpdateTruckVisuals(0f, false);
            return;
        }

        if (!isTruckMoving || activePath.Count == 0)
        {
            UpdateTruckVisuals(0f, false);
            return;
        }

        if (truckSegmentDuration <= 0.0001f)
        {
            BeginNextTruckSegment(activePath[0]);
        }

        Vector3 segmentDirection = truckTargetWorld - truckSegmentStartWorld;
        float segmentDistance = segmentDirection.magnitude;
        if (segmentDistance <= 0.0001f)
        {
            CompleteTruckSegment();
            return;
        }

        truckSegmentProgress += Time.deltaTime / truckSegmentDuration;
        float easedProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(truckSegmentProgress));
        Vector3 currentPosition = Vector3.Lerp(truckSegmentStartWorld, truckTargetWorld, easedProgress);
        truckObject.transform.position = currentPosition;

        Vector3 desiredForward = segmentDirection.normalized;
        truckSmoothedForward = Vector3.Slerp(truckSmoothedForward, desiredForward, 6f * Time.deltaTime).normalized;
        if (truckSmoothedForward.sqrMagnitude > 0.0001f)
        {
            truckObject.transform.rotation = Quaternion.Slerp(
                truckObject.transform.rotation,
                Quaternion.LookRotation(truckSmoothedForward, Vector3.up),
                9f * Time.deltaTime);
        }

        float segmentSpeed = segmentDistance / Mathf.Max(truckSegmentDuration, 0.001f);
        UpdateTruckVisuals(segmentSpeed, true);

        if (truckSegmentProgress < 1f)
        {
            return;
        }

        CompleteTruckSegment();
    }

    private void UpdateDriverRescue()
    {
        if (!isDriverRescueActive || driverObject == null)
        {
            return;
        }

        Vector3 currentPosition = driverObject.transform.position;
        Vector3 targetPosition = driverRescueTargetWorld;
        if (driverRescuePath.Count > 0)
        {
            targetPosition = driverRescuePath[Mathf.Clamp(driverRescueWaypointIndex, 0, driverRescuePath.Count - 1)];
        }

        Vector3 flatDirection = targetPosition - currentPosition;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 step = flatDirection.normalized * (DriverWalkSpeed * Time.deltaTime);
            if (step.sqrMagnitude >= flatDirection.sqrMagnitude)
            {
                currentPosition = targetPosition;
            }
            else
            {
                currentPosition += step;
            }

            driverObject.transform.position = currentPosition;
            driverObject.transform.rotation = Quaternion.Slerp(
                driverObject.transform.rotation,
                Quaternion.LookRotation(flatDirection.normalized, Vector3.up),
                10f * Time.deltaTime);
        }

        if ((driverObject.transform.position - targetPosition).sqrMagnitude > 0.001f)
        {
            return;
        }

        if (driverRescuePath.Count > 0 && driverRescueWaypointIndex < driverRescuePath.Count - 1)
        {
            driverRescueWaypointIndex++;
            return;
        }

        switch (currentDriverRescuePhase)
        {
            case DriverRescuePhase.ToGasStation:
                currentDriverRescuePhase = DriverRescuePhase.ToTruck;
                if (driverFuelCanTransform != null)
                {
                    driverFuelCanTransform.gameObject.SetActive(true);
                }

                SessionDebugLogger.Log("FUEL", $"{GetLoadedTruckDisplayName()} driver reached Gas Station and is returning with fuel.");
                driverRescueTargetWorld = GetDriverStandPointNearTruck();
                BuildDriverRescuePath(currentPosition, driverRescueTargetWorld);
                return;

            case DriverRescuePhase.ToTruck:
                truckFuel = TruckFuelCapacity;
                isDriverRescueActive = false;
                currentDriverRescuePhase = DriverRescuePhase.None;
                if (driverFuelCanTransform != null)
                {
                    driverFuelCanTransform.gameObject.SetActive(false);
                }

                driverObject.SetActive(false);
                driverRescuePath.Clear();
                driverRescueWaypointIndex = 0;
                driverWalkAnimationTime = 0f;
                SessionDebugLogger.Log("FUEL", $"{GetLoadedTruckDisplayName()} rescue completed. Fuel restored to {Mathf.CeilToInt(truckFuel)}/{Mathf.CeilToInt(TruckFuelCapacity)}.");
                if (activePath.Count > 0)
                {
                    isTruckMoving = true;
                    BeginNextTruckSegment(activePath[0]);
                }

                return;
        }
    }

    private void UpdateDriverVisualAnimation()
    {
        if (driverObject == null || driverVisualRoot == null)
        {
            return;
        }

        if (!driverObject.activeSelf)
        {
            driverWalkAnimationTime = 0f;
            ApplyDriverPose(0f, 0f);
            return;
        }

        Vector3 toTarget = driverRescueTargetWorld - driverObject.transform.position;
        toTarget.y = 0f;
        bool isWalking = isDriverRescueActive && toTarget.sqrMagnitude > 0.012f;
        if (isWalking)
        {
            driverWalkAnimationTime += Time.deltaTime * 8.2f;
        }
        else
        {
            driverWalkAnimationTime = Mathf.MoveTowards(driverWalkAnimationTime, 0f, Time.deltaTime * 6f);
        }

        float swing = isWalking ? Mathf.Sin(driverWalkAnimationTime) : 0f;
        float bob = isWalking ? Mathf.Abs(Mathf.Sin(driverWalkAnimationTime * 2f)) * 0.06f : 0f;
        ApplyDriverPose(swing, bob);
    }

    private void ApplyDriverPose(float swing, float bob)
    {
        driverVisualRoot.localPosition = new Vector3(0f, bob, 0f);
        driverVisualRoot.localRotation = Quaternion.Euler(0f, 0f, swing * 2.5f);

        if (driverBodyTransform != null)
        {
            driverBodyTransform.localRotation = Quaternion.Euler(swing * 4f, 0f, 0f);
        }

        if (driverHeadTransform != null)
        {
            driverHeadTransform.localRotation = Quaternion.Euler(-swing * 2f, 0f, 0f);
        }

        if (driverCapTransform != null)
        {
            driverCapTransform.localRotation = Quaternion.Euler(-swing * 1.5f, 0f, 0f);
        }

        if (driverLeftArmTransform != null)
        {
            driverLeftArmTransform.localRotation = Quaternion.Euler(swing * 28f, 0f, 0f);
        }

        if (driverRightArmTransform != null)
        {
            float carryOffset = driverFuelCanTransform != null && driverFuelCanTransform.gameObject.activeSelf ? 18f : 0f;
            driverRightArmTransform.localRotation = Quaternion.Euler(-swing * 28f - carryOffset, 0f, 0f);
        }

        if (driverLeftLegTransform != null)
        {
            driverLeftLegTransform.localRotation = Quaternion.Euler(-swing * 24f, 0f, 0f);
        }

        if (driverRightLegTransform != null)
        {
            driverRightLegTransform.localRotation = Quaternion.Euler(swing * 24f, 0f, 0f);
        }

        if (driverFuelCanTransform != null && driverFuelCanTransform.gameObject.activeSelf)
        {
            driverFuelCanTransform.localPosition = new Vector3(0.2f, 0.4f - bob * 0.2f, 0.04f);
            driverFuelCanTransform.localRotation = Quaternion.Euler(0f, 0f, -10f + swing * 6f);
        }
        else if (driverFuelCanTransform != null)
        {
            driverFuelCanTransform.localPosition = new Vector3(0.18f, 0.42f, 0f);
            driverFuelCanTransform.localRotation = Quaternion.identity;
        }

        if (driverFlashlightTransform != null)
        {
            driverFlashlightTransform.localPosition = new Vector3(0.24f, 0.57f - bob * 0.12f, 0.1f);
            driverFlashlightTransform.localRotation = Quaternion.Euler(16f + swing * 10f, swing * 5f, 0f);
        }
    }

    private void StartMoveTo(Vector2Int destination)
    {
        List<Vector2Int> path = FindPath(truckCell, destination);
        if (path == null || path.Count < 2)
        {
            SessionDebugLogger.Log("PATH", $"{GetLoadedTruckDisplayName()} failed to find path from ({truckCell.x},{truckCell.y}) to ({destination.x},{destination.y}).");
            return;
        }

        activePath.Clear();
        for (int i = 1; i < path.Count; i++)
        {
            activePath.Add(path[i]);
        }

        isTruckMoving = true;
        BeginNextTruckSegment(activePath[0]);
        SessionDebugLogger.Log("PATH", $"{GetLoadedTruckDisplayName()} started moving from ({truckCell.x},{truckCell.y}) to ({destination.x},{destination.y}) over {activePath.Count} steps.");
    }

    private bool HasPath(Vector2Int start, Vector2Int goal)
    {
        return FindPath(start, goal) != null;
    }

    private List<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        LocationType? startLocation = GetContainingLocation(start);
        LocationType? goalLocation = GetContainingLocation(goal);
        return GridPathService.FindPath(
            start,
            goal,
            GridPathService.GetCardinalNeighbors,
            neighbor => IsDriveableForPath(neighbor, startLocation, goalLocation));
    }

    private List<Vector2Int> FindRoadBuildPath(Vector2Int start, Vector2Int goal, System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        return GridPathService.FindPath(
            start,
            goal,
            GridPathService.GetCardinalNeighbors,
            neighbor => CanBuildRoadThroughCell(neighbor, start, goal, isBlockedLocationCell));
    }

    private bool IsDriveable(Vector2Int cell)
    {
        return IsInsideGrid(cell) && (roadCells.Contains(cell) || IsAnchorCell(cell));
    }

    private bool CanBuildRoadThroughCell(Vector2Int cell, Vector2Int start, Vector2Int goal, System.Func<Vector2Int, bool> isBlockedLocationCell)
    {
        if (!IsInsideGrid(cell))
        {
            return false;
        }

        if (cell == start || cell == goal || roadCells.Contains(cell))
        {
            return true;
        }

        return !isBlockedLocationCell(cell);
    }

    private bool IsDriveableForPath(Vector2Int cell, LocationType? startLocation, LocationType? goalLocation)
    {
        return IsDriveable(cell);
    }

    private bool IsAnchorCell(Vector2Int cell)
    {
        foreach (LocationData location in locations.Values)
        {
            if (location.Anchor == cell)
            {
                return true;
            }
        }

        return false;
    }

    private LocationType? GetContainingLocation(Vector2Int cell)
    {
        foreach (KeyValuePair<LocationType, LocationData> pair in locations)
        {
            if (pair.Value.Contains(cell) || pair.Value.Anchor == cell)
            {
                return pair.Key;
            }
        }

        return null;
    }

    private bool IsLocationCell(Vector2Int cell)
    {
        foreach (LocationData location in locations.Values)
        {
            if (location.Contains(cell) || location.Anchor == cell)
            {
                return true;
            }
        }

        return false;
    }

    private void AddRoad(Vector2Int cell)
    {
        if (roadCells.Contains(cell) || !IsInsideGrid(cell) || IsLocationCell(cell))
        {
            return;
        }

        roadCells.Add(cell);

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = $"Road_{cell.x}_{cell.y}";
        road.transform.SetParent(roadsRoot, false);
        road.transform.position = GetCellCenter(cell) + new Vector3(0f, RoadHeight, 0f);
        road.transform.localScale = new Vector3(1.04f, 0.18f, 1.04f);
        ApplyColor(road, new Color(0.18f, 0.19f, 0.21f));
        ConfigureStaticVisual(road);

        GameObject roadTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roadTop.name = "RoadTop";
        roadTop.transform.SetParent(road.transform, false);
        roadTop.transform.localPosition = new Vector3(0f, 0.28f, 0f);
        roadTop.transform.localScale = new Vector3(0.84f, 0.16f, 0.84f);
        ApplyColor(roadTop, new Color(0.76f, 0.71f, 0.58f));
        ConfigureStaticVisual(roadTop);
        roadVisuals[cell] = road;

        RefreshRoadVisual(cell);
        foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(cell))
        {
            if (roadVisuals.ContainsKey(neighbor))
            {
                RefreshRoadVisual(neighbor);
            }
        }

        RebuildRoadLanterns();
        SessionDebugLogger.Log("ROAD", $"Added road at cell ({cell.x},{cell.y}).");
    }

    private void RemoveRoad(Vector2Int cell)
    {
        if (!roadCells.Remove(cell))
        {
            return;
        }

        if (roadVisuals.TryGetValue(cell, out GameObject road))
        {
            roadVisuals.Remove(cell);
            Destroy(road);
        }

        foreach (Vector2Int neighbor in GridPathService.GetCardinalNeighbors(cell))
        {
            if (roadVisuals.ContainsKey(neighbor))
            {
                RefreshRoadVisual(neighbor);
            }
        }

        RebuildRoadLanterns();
        SessionDebugLogger.Log("ROAD", $"Removed road at cell ({cell.x},{cell.y}).");
    }

    private void GenerateInitialRoadNetwork()
    {
        CreateGuaranteedRoadConnection(locations[LocationType.Parking].Anchor, locations[LocationType.GasStation].Anchor);
        CreateGuaranteedRoadConnection(locations[LocationType.GasStation].Anchor, locations[LocationType.Warehouse].Anchor);
        CreateGuaranteedRoadConnection(locations[LocationType.Warehouse].Anchor, locations[LocationType.Forest].Anchor);
        CreateGuaranteedRoadConnection(locations[LocationType.Warehouse].Anchor, locations[LocationType.Town].Anchor);
        SessionDebugLogger.Log("ROAD", $"Generated starter road network with {roadCells.Count} road cells.");
    }

    private void CreateGuaranteedRoadConnection(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = FindRoadBuildPath(start, end, IsLocationCell);
        if (path == null)
        {
            return;
        }

        foreach (Vector2Int cell in path)
        {
            TryAddStarterRoadCell(cell, start, end);
        }
    }

    private void TryAddStarterRoadCell(Vector2Int cell, Vector2Int start, Vector2Int end)
    {
        if (cell == start || cell == end)
        {
            return;
        }

        AddRoad(cell);
    }

    private void RefreshRoadVisual(Vector2Int cell)
    {
        if (!roadVisuals.TryGetValue(cell, out GameObject road))
        {
            return;
        }

        bool horizontal = ConnectsToRoadOrAnchor(cell, new Vector2Int(1, 0)) || ConnectsToRoadOrAnchor(cell, new Vector2Int(-1, 0));
        bool vertical = ConnectsToRoadOrAnchor(cell, new Vector2Int(0, 1)) || ConnectsToRoadOrAnchor(cell, new Vector2Int(0, -1));

        Vector3 scale = road.transform.localScale;
        scale.x = horizontal ? 1.12f : 0.82f;
        scale.z = vertical ? 1.12f : 0.82f;
        road.transform.localScale = scale;

        if (road.transform.childCount > 0)
        {
            Transform roadTop = road.transform.GetChild(0);
            Vector3 topScale = roadTop.localScale;
            topScale.x = horizontal ? 0.92f : 0.62f;
            topScale.z = vertical ? 0.92f : 0.62f;
            roadTop.localScale = topScale;
        }
    }

    private void RebuildRoadLanterns()
    {
        if (lanternsRoot == null)
        {
            return;
        }

        for (int i = lanternsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(lanternsRoot.GetChild(i).gameObject);
        }

        roadLanterns.Clear();

        foreach (Vector2Int roadCell in roadCells)
        {
            if (!TryGetRoadLanternPlacement(roadCell, out Vector3 worldPosition, out Quaternion worldRotation))
            {
                continue;
            }

            CreateRoadLantern(worldPosition, worldRotation);
        }
    }

    private bool TryGetRoadLanternPlacement(Vector2Int cell, out Vector3 worldPosition, out Quaternion worldRotation)
    {
        return RoadLanternPlanner.TryGetPlacement(
            cell,
            neighbor => roadCells.Contains(neighbor) || IsAnchorCell(neighbor),
            IsLocationCell,
            GetCellCenter,
            out worldPosition,
            out worldRotation);
    }

    private bool ConnectsToRoadOrAnchor(Vector2Int cell, Vector2Int offset)
    {
        Vector2Int neighbor = cell + offset;
        return roadCells.Contains(neighbor) || IsAnchorCell(neighbor);
    }

    private void CreateRoadLantern(Vector3 worldPosition, Quaternion worldRotation)
    {
        GameObject lanternRoot = new("RoadLantern");
        lanternRoot.transform.SetParent(lanternsRoot, false);
        lanternRoot.transform.position = worldPosition;
        lanternRoot.transform.rotation = worldRotation;

        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pole.transform.SetParent(lanternRoot.transform, false);
        pole.transform.localPosition = new Vector3(0f, 0.72f, 0f);
        pole.transform.localScale = new Vector3(0.08f, 1.42f, 0.08f);
        ApplyColor(pole, new Color(0.22f, 0.23f, 0.27f));
        ConfigureShadowVisual(pole);

        GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arm.transform.SetParent(lanternRoot.transform, false);
        arm.transform.localPosition = new Vector3(0.14f, 1.34f, 0f);
        arm.transform.localScale = new Vector3(0.3f, 0.06f, 0.06f);
        ApplyColor(arm, new Color(0.22f, 0.23f, 0.27f));
        ConfigureShadowVisual(arm);

        GameObject lampHead = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lampHead.transform.SetParent(lanternRoot.transform, false);
        lampHead.transform.localPosition = new Vector3(0.26f, 1.16f, 0f);
        lampHead.transform.localScale = new Vector3(0.16f, 0.22f, 0.16f);
        ApplyColor(lampHead, new Color(0.3f, 0.28f, 0.2f));
        ConfigureShadowVisual(lampHead);

        GameObject lampGlow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampGlow.transform.SetParent(lanternRoot.transform, false);
        lampGlow.transform.localPosition = new Vector3(0.26f, 1.05f, 0f);
        lampGlow.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
        ApplyColor(lampGlow, new Color(0.26f, 0.22f, 0.18f));
        ConfigureStaticVisual(lampGlow);
        Renderer lampGlowRenderer = lampGlow.GetComponent<Renderer>();

        GameObject lightObject = new("LanternLight");
        lightObject.transform.SetParent(lanternRoot.transform, false);
        lightObject.transform.localPosition = new Vector3(0.26f, 1.02f, 0f);

        Light lanternLight = lightObject.AddComponent<Light>();
        lanternLight.type = LightType.Point;
        lanternLight.color = new Color(1f, 0.9f, 0.72f);
        lanternLight.range = 4.4f;
        lanternLight.intensity = 0f;
        lanternLight.shadows = LightShadows.None;
        lanternLight.enabled = false;

        roadLanterns.Add(new RoadLanternData
        {
            Light = lanternLight,
            GlowRenderer = lampGlowRenderer,
            ActivationOffset = Random.Range(-0.14f, 0.2f),
            FlickerSeed = Random.Range(0.1f, 100f),
            FlickerSpeed = Random.Range(0.7f, 1.35f),
            FlickerStrength = Random.Range(0.18f, 0.42f),
            FlickerThreshold = Random.Range(0.72f, 0.9f)
        });
    }

    private void CreateLocation(LocationType type, string label, Vector2Int min, Vector2Int max, Vector2Int anchor, Color baseColor)
    {
        LocationData data = new()
        {
            Label = label,
            Min = min,
            Max = max,
            Anchor = anchor
            ,
            BaseColor = baseColor
        };

        GameObject root = new(label);
        root.transform.SetParent(worldRoot, false);
        data.RootObject = root;

        Vector2Int size = new(max.x - min.x + 1, max.y - min.y + 1);
        Vector3 center = new Vector3((min.x + max.x + 1) * 0.5f, 0.35f, (min.y + max.y + 1) * 0.5f);

        GameObject baseBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseBlock.transform.SetParent(root.transform, false);
        baseBlock.transform.position = center;
        baseBlock.transform.localScale = new Vector3(size.x * 0.95f, 0.7f, size.y * 0.95f);
        ApplyColor(baseBlock, baseColor);
        ConfigureShadowVisual(baseBlock);
        data.BaseRenderer = baseBlock.GetComponent<Renderer>();

        if (type == LocationType.Parking)
        {
            CreateParkingDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.GasStation)
        {
            CreateGasStationDecoration(root.transform, center, min, max, anchor);
        }
        else if (type == LocationType.Forest)
        {
            CreateForestDecoration(root.transform, min, max);
        }
        else if (type == LocationType.Warehouse)
        {
            CreateWarehouseDecoration(root.transform, center);
        }
        else
        {
            CreateTownDecoration(root.transform, center);
        }

        CreateLocationNightLights(type, root.transform, center, size);

        GameObject anchorMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        anchorMarker.transform.SetParent(root.transform, false);
        anchorMarker.transform.position = GetCellCenter(anchor) + new Vector3(0f, 0.05f, 0f);
        anchorMarker.transform.localScale = new Vector3(0.22f, 0.02f, 0.22f);
        ApplyColor(anchorMarker, new Color(1f, 0.9f, 0.35f));

        locations[type] = data;
    }

    private void CreateParkingDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
        canopy.transform.SetParent(parent, false);
        canopy.transform.position = center + new Vector3(0f, 0.6f, -0.15f);
        canopy.transform.localScale = new Vector3(2.8f, 0.12f, 1.4f);
        ApplyColor(canopy, new Color(0.18f, 0.2f, 0.24f));

        Vector3[] postOffsets =
        {
            new(-1.15f, 0.28f, -0.55f),
            new(1.15f, 0.28f, -0.55f),
            new(-1.15f, 0.28f, 0.25f),
            new(1.15f, 0.28f, 0.25f)
        };

        foreach (Vector3 offset in postOffsets)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(parent, false);
            post.transform.position = center + offset;
            post.transform.localScale = new Vector3(0.12f, 0.56f, 0.12f);
            ApplyColor(post, new Color(0.3f, 0.32f, 0.36f));
        }

        CreateDrivewayToAnchor(parent, min, max, anchor, 0.62f);
    }

    private void CreateForestDecoration(Transform parent, Vector2Int min, Vector2Int max)
    {
        Vector3[] treePositions =
        {
            GetCellCenter(min),
            GetCellCenter(new Vector2Int(max.x, min.y)),
            GetCellCenter(new Vector2Int(min.x, max.y)),
            GetCellCenter(max)
        };

        foreach (Vector3 position in treePositions)
        {
            GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(parent, false);
            trunk.transform.position = position + new Vector3(0f, 0.45f, 0f);
            trunk.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            ApplyColor(trunk, new Color(0.42f, 0.26f, 0.14f));

            GameObject leaves = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leaves.transform.SetParent(parent, false);
            leaves.transform.position = position + new Vector3(0f, 0.95f, 0f);
            leaves.transform.localScale = new Vector3(0.55f, 0.55f, 0.55f);
            ApplyColor(leaves, new Color(0.14f, 0.5f, 0.2f));
        }
    }

    private void CreateGasStationDecoration(Transform parent, Vector3 center, Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + new Vector3(0f, 0.72f, -0.18f);
        roof.transform.localScale = new Vector3(2.15f, 0.12f, 1.08f);
        ApplyColor(roof, new Color(0.95f, 0.3f, 0.22f));

        Vector3[] postOffsets =
        {
            new(-0.8f, 0.32f, -0.44f),
            new(0.8f, 0.32f, -0.44f),
            new(-0.8f, 0.32f, 0.08f),
            new(0.8f, 0.32f, 0.08f)
        };

        foreach (Vector3 offset in postOffsets)
        {
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
            post.transform.SetParent(parent, false);
            post.transform.position = center + offset;
            post.transform.localScale = new Vector3(0.12f, 0.64f, 0.12f);
            ApplyColor(post, new Color(0.96f, 0.94f, 0.88f));
        }

        GameObject kiosk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        kiosk.transform.SetParent(parent, false);
        kiosk.transform.position = center + new Vector3(0f, 0.36f, 0.38f);
        kiosk.transform.localScale = new Vector3(1.25f, 0.52f, 0.5f);
        ApplyColor(kiosk, new Color(0.98f, 0.92f, 0.78f));

        GameObject pump = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pump.transform.SetParent(parent, false);
        pump.transform.position = center + new Vector3(0f, 0.32f, -0.12f);
        pump.transform.localScale = new Vector3(0.24f, 0.42f, 0.24f);
        ApplyColor(pump, new Color(0.2f, 0.22f, 0.26f));

        CreateDrivewayToAnchor(parent, min, max, anchor, 0.58f);
    }

    private void CreateDrivewayToAnchor(Transform parent, Vector2Int min, Vector2Int max, Vector2Int anchor, float width)
    {
        Vector3 end = GetCellCenter(anchor) + new Vector3(0f, 0.11f, 0f);
        Vector3 start = GetDrivewayStartPoint(min, max, anchor) + new Vector3(0f, 0.11f, 0f);
        CreateDriveway(parent, start, end, width);
    }

    private Vector3 GetDrivewayStartPoint(Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        float centerX = (min.x + max.x + 1) * 0.5f;
        float centerZ = (min.y + max.y + 1) * 0.5f;

        if (anchor.y < min.y)
        {
            return new Vector3(Mathf.Clamp(anchor.x + 0.5f, min.x + 0.25f, max.x + 0.75f), GetLocationPadHeight(min, max, anchor), min.y - 0.02f);
        }

        if (anchor.y > max.y)
        {
            return new Vector3(Mathf.Clamp(anchor.x + 0.5f, min.x + 0.25f, max.x + 0.75f), GetLocationPadHeight(min, max, anchor), max.y + 1.02f);
        }

        if (anchor.x < min.x)
        {
            return new Vector3(min.x - 0.02f, GetLocationPadHeight(min, max, anchor), Mathf.Clamp(anchor.y + 0.5f, min.y + 0.25f, max.y + 0.75f));
        }

        if (anchor.x > max.x)
        {
            return new Vector3(max.x + 1.02f, GetLocationPadHeight(min, max, anchor), Mathf.Clamp(anchor.y + 0.5f, min.y + 0.25f, max.y + 0.75f));
        }

        return new Vector3(centerX, GetLocationPadHeight(min, max, anchor), centerZ);
    }

    private float GetLocationPadHeight(Vector2Int min, Vector2Int max, Vector2Int anchor)
    {
        float total = terrainHeights[anchor.x, anchor.y];
        int count = 1;
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                total += terrainHeights[x, y];
                count++;
            }
        }

        return total / Mathf.Max(1, count);
    }

    private void CreateDriveway(Transform parent, Vector3 worldStart, Vector3 worldEnd, float width)
    {
        GameObject driveway = GameObject.CreatePrimitive(PrimitiveType.Cube);
        driveway.name = "Driveway";
        driveway.transform.SetParent(parent, false);

        Vector3 delta = worldEnd - worldStart;
        float length = delta.magnitude;
        driveway.transform.position = worldStart + delta * 0.5f;
        driveway.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
        driveway.transform.localScale = new Vector3(width, 0.1f, length);

        ApplyColor(driveway, new Color(0.2f, 0.21f, 0.23f));
        ConfigureStaticVisual(driveway);

        GameObject drivewayTop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        drivewayTop.name = "DrivewayTop";
        drivewayTop.transform.SetParent(driveway.transform, false);
        drivewayTop.transform.localPosition = new Vector3(0f, 0.58f, 0f);
        drivewayTop.transform.localScale = new Vector3(0.72f, 0.18f, 0.88f);
        ApplyColor(drivewayTop, new Color(0.76f, 0.71f, 0.58f));
        ConfigureStaticVisual(drivewayTop);
    }

    private void CreateWarehouseDecoration(Transform parent, Vector3 center)
    {
        GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
        roof.transform.SetParent(parent, false);
        roof.transform.position = center + new Vector3(0f, 0.47f, 0f);
        roof.transform.localScale = new Vector3(2.05f, 0.12f, 2.05f);
        ApplyColor(roof, new Color(0.88f, 0.24f, 0.2f));
    }

    private void CreateTownDecoration(Transform parent, Vector3 center)
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
            house.transform.SetParent(parent, false);
            house.transform.position = center + new Vector3(-0.3f + i * 0.6f, 0.4f, 0f);
            house.transform.localScale = new Vector3(0.45f, 0.5f, 0.45f);
            ApplyColor(house, new Color(0.92f, 0.84f, 0.66f));
        }
    }

    private void CreateLocationNightLights(LocationType type, Transform parent, Vector3 center, Vector2Int size)
    {
        if (type == LocationType.Forest)
        {
            CreateLocationNightLight(parent, center + new Vector3(0f, 1.15f, -0.95f));
            return;
        }

        float xOffset = Mathf.Max(0.45f, size.x * 0.28f);
        float zOffset = Mathf.Max(0.38f, size.y * 0.28f);
        CreateLocationNightLight(parent, center + new Vector3(-xOffset, 0.92f, -zOffset));
        CreateLocationNightLight(parent, center + new Vector3(xOffset, 0.92f, -zOffset));

        if (type == LocationType.Town)
        {
            CreateLocationNightLight(parent, center + new Vector3(0f, 0.86f, zOffset));
        }
    }

    private void CreateLocationNightLight(Transform parent, Vector3 localPosition)
    {
        GameObject lampVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lampVisual.transform.SetParent(parent, false);
        lampVisual.transform.localPosition = localPosition;
        lampVisual.transform.localScale = new Vector3(0.14f, 0.14f, 0.14f);
        ApplyColor(lampVisual, new Color(0.28f, 0.24f, 0.18f));
        ConfigureStaticVisual(lampVisual);
        locationNightLightRenderers.Add(lampVisual.GetComponent<Renderer>());

        GameObject lightObject = new("NightLamp");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = localPosition + new Vector3(0f, 0.06f, 0f);

        Light lamp = lightObject.AddComponent<Light>();
        lamp.type = LightType.Point;
        lamp.color = new Color(1f, 0.9f, 0.72f);
        lamp.range = 3.2f;
        lamp.intensity = 0f;
        lamp.shadows = LightShadows.None;
        lamp.enabled = false;
        locationNightLights.Add(lamp);
    }

    private void CreateGridLine(Transform parent, Material lineMaterial, Vector3 start, Vector3 end)
    {
        GameObject lineObject = new($"GridLine_{start.x}_{start.z}_{end.x}_{end.z}");
        lineObject.transform.SetParent(parent, false);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.useWorldSpace = false;
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.widthMultiplier = 0.03f;
        lineRenderer.material = lineMaterial;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }

    private Vector3 GetCellCenter(Vector2Int cell)
    {
        return new Vector3(cell.x + 0.5f, GetTerrainHeight(cell), cell.y + 0.5f);
    }

    private Vector3 GetTruckWorldPosition(Vector2Int cell)
    {
        return GetCellCenter(cell) + new Vector3(0f, TruckSegmentStartLift, 0f);
    }

    private static Vector2Int WorldToCell(Vector3 point)
    {
        return new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.z));
    }

    private float GetTerrainHeight(Vector2Int cell)
    {
        if (!IsInsideGrid(cell))
        {
            return 0f;
        }

        return terrainHeights[cell.x, cell.y];
    }

    private float GetLocationBaseHeight(LocationType locationType)
    {
        if (!locations.TryGetValue(locationType, out LocationData location))
        {
            return 0f;
        }

        float total = 0f;
        int count = 0;
        for (int x = location.Min.x; x <= location.Max.x; x++)
        {
            for (int y = location.Min.y; y <= location.Max.y; y++)
            {
                total += terrainHeights[x, y];
                count++;
            }
        }

        total += terrainHeights[location.Anchor.x, location.Anchor.y];
        count++;
        return total / Mathf.Max(1, count);
    }

    private float SampleTerrainHeight(float worldX, float worldZ)
    {
        float clampedX = Mathf.Clamp(worldX - 0.5f, 0f, GridWidth - 1.001f);
        float clampedZ = Mathf.Clamp(worldZ - 0.5f, 0f, GridHeight - 1.001f);
        int x0 = Mathf.Clamp(Mathf.FloorToInt(clampedX), 0, GridWidth - 1);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(clampedZ), 0, GridHeight - 1);
        int x1 = Mathf.Min(GridWidth - 1, x0 + 1);
        int z1 = Mathf.Min(GridHeight - 1, z0 + 1);
        float tx = clampedX - x0;
        float tz = clampedZ - z0;
        float h00 = terrainHeights[x0, z0];
        float h10 = terrainHeights[x1, z0];
        float h01 = terrainHeights[x0, z1];
        float h11 = terrainHeights[x1, z1];
        float hx0 = Mathf.Lerp(h00, h10, tx);
        float hx1 = Mathf.Lerp(h01, h11, tx);
        return Mathf.Lerp(hx0, hx1, tz);
    }

    private float GetVerticalEdgeHeight(int x, int y)
    {
        if (x <= 0)
        {
            return terrainHeights[0, Mathf.Clamp(y, 0, GridHeight - 1)];
        }

        if (x >= GridWidth)
        {
            return terrainHeights[GridWidth - 1, Mathf.Clamp(y, 0, GridHeight - 1)];
        }

        int clampedY = Mathf.Clamp(y, 0, GridHeight - 1);
        return (terrainHeights[x - 1, clampedY] + terrainHeights[x, clampedY]) * 0.5f;
    }

    private float GetHorizontalEdgeHeight(int x, int y)
    {
        if (y <= 0)
        {
            return terrainHeights[Mathf.Clamp(x, 0, GridWidth - 1), 0];
        }

        if (y >= GridHeight)
        {
            return terrainHeights[Mathf.Clamp(x, 0, GridWidth - 1), GridHeight - 1];
        }

        int clampedX = Mathf.Clamp(x, 0, GridWidth - 1);
        return (terrainHeights[clampedX, y - 1] + terrainHeights[clampedX, y]) * 0.5f;
    }

    private static void ApplyColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = new(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", 0.14f);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0f);
        }

        renderer.material = material;
    }

    private static void ApplyUnlitColor(GameObject target, Color color)
    {
        Renderer renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new(shader)
        {
            color = color
        };

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        renderer.material = material;
    }

    private static void ConfigureStaticVisual(GameObject target)
    {
        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void ConfigureShadowVisual(GameObject target)
    {
        if (!target.TryGetComponent(out Renderer renderer))
        {
            return;
        }

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        renderer.receiveShadows = true;
    }

    private bool IsInsideGrid(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < GridWidth && cell.y >= 0 && cell.y < GridHeight;
    }
}

