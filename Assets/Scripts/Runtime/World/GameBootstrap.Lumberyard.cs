using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private const int LumberTreeMaxGrowthStage = 5;
    private const int LumberTreeMatureGrowthStage = 3;
    private const float LumberTreeChopInterval = 0.55f;
    private const int LumberTreeChopsRequired = 5;
    private const float LumberTreeFallDuration = 0.8f;
    private const float LumberTreePlantDuration = 1.5f;
    private const float LumberTreeCarryLogLift = 0.18f;

    private enum LumberTreeRuntimeState
    {
        Growing,
        Ready,
        Falling,
        Felled
    }

    private sealed class LumberGroundLogData
    {
        public int Id;
        public GameObject RootObject;
        public Vector3 WorldPosition;
        public bool IsCollected;
    }

    private sealed class LumberTreeRuntimeData
    {
        public Vector2Int Cell;
        public Transform RootTransform;
        public int VariantIndex;
        public float BaseScale;
        public int GrowthStage;
        public int LastGrowthDay;
        public int ChopHitsCompleted;
        public float ChopTimer;
        public float FallTimer;
        public Quaternion UprightRotation;
        public Quaternion FallenRotation;
        public LumberTreeRuntimeState State;
        public bool NeedsReplant;
        public readonly List<LumberGroundLogData> GroundLogs = new();
    }

    private sealed class LumberWorkerTaskData
    {
        public Vector2Int TreeCell;
        public int CarryLogId = -1;
        public bool PendingPlanting;
    }

    private readonly Dictionary<Vector2Int, LumberTreeRuntimeData> lumberTrees = new();
    private readonly Dictionary<int, LumberWorkerTaskData> lumberWorkerTasks = new();
    private readonly Dictionary<int, Transform> lumberCarryVisuals = new();
    private int nextLumberGroundLogId = 1;
    private int lastLumberGrowthDay = -1;

    private void ResetLumberyardWorldState()
    {
        foreach (LumberTreeRuntimeData tree in lumberTrees.Values)
        {
            if (tree == null)
            {
                continue;
            }

            for (int i = 0; i < tree.GroundLogs.Count; i++)
            {
                if (tree.GroundLogs[i]?.RootObject != null)
                {
                    Destroy(tree.GroundLogs[i].RootObject);
                }
            }
        }

        lumberTrees.Clear();
        lumberWorkerTasks.Clear();
        nextLumberGroundLogId = 1;
        lastLumberGrowthDay = currentDay;

        foreach (Transform carryVisual in lumberCarryVisuals.Values)
        {
            if (carryVisual != null)
            {
                Destroy(carryVisual.gameObject);
            }
        }

        lumberCarryVisuals.Clear();
    }

    private bool RegisterLumberTree(Vector2Int cell, Transform rootTransform, int variantIndex)
    {
        if (rootTransform == null || !IsDenseForestCell(cell.x, cell.y))
        {
            return false;
        }

        float baseScale = rootTransform.localScale.x;
        LumberTreeRuntimeData tree = new()
        {
            Cell = cell,
            RootTransform = rootTransform,
            VariantIndex = variantIndex,
            BaseScale = Mathf.Max(0.01f, baseScale),
            GrowthStage = Random.Range(LumberTreeMatureGrowthStage, LumberTreeMaxGrowthStage + 1),
            LastGrowthDay = currentDay,
            UprightRotation = rootTransform.localRotation,
            State = LumberTreeRuntimeState.Ready
        };
        tree.FallenRotation = tree.UprightRotation * Quaternion.Euler(0f, 0f, 92f);
        lumberTrees[cell] = tree;
        ApplyLumberTreeStageVisual(tree);
        return true;
    }

    private void UpdateLumberyardSystem()
    {
        UpdateLumberTreeGrowthCycle();
        UpdateLumberTreeFallAnimations();

        for (int i = 0; i < driverAgents.Count; i++)
        {
            UpdateLumberyardWorker(driverAgents[i]);
        }
    }

    private void UpdateLumberTreeGrowthCycle()
    {
        if (lastLumberGrowthDay == currentDay)
        {
            return;
        }

        lastLumberGrowthDay = currentDay;
        int grownCount = 0;
        foreach (LumberTreeRuntimeData tree in lumberTrees.Values)
        {
            if (tree == null || tree.RootTransform == null || tree.State == LumberTreeRuntimeState.Falling)
            {
                continue;
            }

            if (tree.GrowthStage >= LumberTreeMaxGrowthStage)
            {
                continue;
            }

            tree.GrowthStage = Mathf.Min(LumberTreeMaxGrowthStage, tree.GrowthStage + 1);
            tree.LastGrowthDay = currentDay;
            tree.State = tree.GrowthStage >= LumberTreeMatureGrowthStage
                ? LumberTreeRuntimeState.Ready
                : LumberTreeRuntimeState.Growing;
            ApplyLumberTreeStageVisual(tree);
            grownCount++;
        }

        if (grownCount > 0)
        {
            SessionDebugLogger.Log("LUMBER", $"Daily tree growth advanced on {grownCount} forestry trees for day {currentDay}.");
        }
    }

    private void UpdateLumberTreeFallAnimations()
    {
        foreach (LumberTreeRuntimeData tree in lumberTrees.Values)
        {
            if (tree == null || tree.RootTransform == null || tree.State != LumberTreeRuntimeState.Falling)
            {
                continue;
            }

            tree.FallTimer += Time.deltaTime * gameSpeedMultiplier;
            float t = Mathf.Clamp01(tree.FallTimer / LumberTreeFallDuration);
            tree.RootTransform.localRotation = Quaternion.Slerp(tree.UprightRotation, tree.FallenRotation, Mathf.SmoothStep(0f, 1f, t));
            if (t < 1f)
            {
                continue;
            }

            FinalizeFelledTree(tree);
        }
    }

    private void UpdateLumberyardWorker(DriverAgent driver)
    {
        if (driver == null ||
            driver.DutyMode != DriverDutyMode.Logistics ||
            driver.AssignedBuildingType != LocationType.Forest ||
            !driver.IsOnActiveShift)
        {
            return;
        }

        if (!lumberWorkerTasks.TryGetValue(driver.DriverId, out LumberWorkerTaskData task))
        {
            task = new LumberWorkerTaskData();
            lumberWorkerTasks[driver.DriverId] = task;
        }

        if (driver.WalkPhase == DriverRescuePhase.LumberChopping)
        {
            UpdateActiveLumberChopping(driver, task);
            return;
        }

        if (driver.WalkPhase == DriverRescuePhase.LumberPlanting)
        {
            UpdateActiveLumberPlanting(driver, task);
            return;
        }

        if (driver.WalkPhase != DriverRescuePhase.None)
        {
            return;
        }

        if (driver.IsInsideBuilding)
        {
            TryDispatchForestWorkerFromBuilding(driver, task);
        }
    }

    private void TryDispatchForestWorkerFromBuilding(DriverAgent driver, LumberWorkerTaskData task)
    {
        if (!locations.TryGetValue(LocationType.Forest, out LocationData forestLocation))
        {
            return;
        }

        if (forestLocation.LogsStored >= ForestMaxLogsStorage && !HasPendingGroundLogs())
        {
            return;
        }

        LumberTreeRuntimeData targetTree = null;
        bool shouldPlant = false;
        int carryLogId = -1;

        if (TryFindTreeWithPendingGroundLog(out targetTree, out LumberGroundLogData groundLog))
        {
            carryLogId = groundLog.Id;
        }
        else if (TryFindTreeNeedingPlanting(out targetTree))
        {
            shouldPlant = true;
        }
        else if (forestLocation.LogsStored < ForestMaxLogsStorage && TryFindMatureTree(out targetTree))
        {
        }

        if (targetTree == null)
        {
            return;
        }

        SpawnForestWorkerOutside(driver);
        task.TreeCell = targetTree.Cell;
        task.CarryLogId = carryLogId;
        task.PendingPlanting = shouldPlant;

        Vector3 target = GetLumberTreeWorkPoint(targetTree);
        driver.WalkTargetWorld = target;
        driver.WalkPhase = shouldPlant ? DriverRescuePhase.LumberReturnToTreeForPlanting : DriverRescuePhase.LumberToTree;
        BuildDriverWalkPath(driver, driver.DriverObject.transform.position, target);

        string reason = shouldPlant ? "planting" : carryLogId > 0 ? "log pickup" : "tree cutting";
        SessionDebugLogger.Log("LUMBER", $"{driver.DriverName} left Lumberyard for {reason} at tree ({targetTree.Cell.x},{targetTree.Cell.y}).");
    }

    private void SpawnForestWorkerOutside(DriverAgent driver)
    {
        if (driver?.DriverObject == null)
        {
            return;
        }

        if (!locations.TryGetValue(LocationType.Forest, out LocationData forestLocation))
        {
            return;
        }

        Vector3 spawnPos = GetCellCenter(forestLocation.Anchor);
        spawnPos.y += 0.05f;
        driver.IsInsideBuilding = false;
        driver.DriverObject.SetActive(true);
        driver.DriverObject.transform.position = spawnPos;
        driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        driver.WalkAnimationTime = 0f;
        ApplyDriverPose(driver, 0f, 0f);
    }

    private bool TryFindMatureTree(out LumberTreeRuntimeData result)
    {
        result = null;
        float bestDistance = float.MaxValue;
        Vector3 forestCenter = GetLocationCenter(LocationType.Forest);

        foreach (LumberTreeRuntimeData tree in lumberTrees.Values)
        {
            if (!IsTreeAvailableForCutting(tree))
            {
                continue;
            }

            float distance = Vector3.Distance(forestCenter, GetCellCenter(tree.Cell));
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            result = tree;
        }

        return result != null;
    }

    private bool TryFindTreeWithPendingGroundLog(out LumberTreeRuntimeData resultTree, out LumberGroundLogData resultLog)
    {
        resultTree = null;
        resultLog = null;
        float bestDistance = float.MaxValue;
        Vector3 forestCenter = GetLocationCenter(LocationType.Forest);

        foreach (LumberTreeRuntimeData tree in lumberTrees.Values)
        {
            if (tree == null)
            {
                continue;
            }

            for (int i = 0; i < tree.GroundLogs.Count; i++)
            {
                LumberGroundLogData log = tree.GroundLogs[i];
                if (log == null || log.IsCollected)
                {
                    continue;
                }

                float distance = Vector3.Distance(forestCenter, log.WorldPosition);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                resultTree = tree;
                resultLog = log;
            }
        }

        return resultTree != null && resultLog != null;
    }

    private bool TryFindTreeNeedingPlanting(out LumberTreeRuntimeData result)
    {
        result = null;
        float bestDistance = float.MaxValue;
        Vector3 forestCenter = GetLocationCenter(LocationType.Forest);

        foreach (LumberTreeRuntimeData tree in lumberTrees.Values)
        {
            if (tree == null || !tree.NeedsReplant || HasUncollectedGroundLogs(tree))
            {
                continue;
            }

            float distance = Vector3.Distance(forestCenter, GetCellCenter(tree.Cell));
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            result = tree;
        }

        return result != null;
    }

    private static bool HasUncollectedGroundLogs(LumberTreeRuntimeData tree)
    {
        if (tree == null)
        {
            return false;
        }

        for (int i = 0; i < tree.GroundLogs.Count; i++)
        {
            if (tree.GroundLogs[i] != null && !tree.GroundLogs[i].IsCollected)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasPendingGroundLogs()
    {
        foreach (LumberTreeRuntimeData tree in lumberTrees.Values)
        {
            if (HasUncollectedGroundLogs(tree))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTreeAvailableForCutting(LumberTreeRuntimeData tree)
    {
        return tree != null &&
               tree.RootTransform != null &&
               tree.State == LumberTreeRuntimeState.Ready &&
               tree.GrowthStage >= LumberTreeMatureGrowthStage &&
               !tree.NeedsReplant &&
               !HasUncollectedGroundLogs(tree);
    }

    private void UpdateActiveLumberChopping(DriverAgent driver, LumberWorkerTaskData task)
    {
        if (!TryGetLumberTree(task.TreeCell, out LumberTreeRuntimeData tree))
        {
            ReturnForestWorkerInside(driver, "tree task vanished");
            return;
        }

        driver.WalkAnimationTime += Time.deltaTime * 3.4f;
        ApplyDriverLumberChopPose(driver, Mathf.PingPong(driver.WalkAnimationTime * 0.75f, 1f));

        if (tree.State == LumberTreeRuntimeState.Felled)
        {
            if (TryAssignNearestGroundLog(task, tree, out LumberGroundLogData log))
            {
                StartForestWorkerCarryLog(driver, task, tree, log);
            }
            else if (tree.NeedsReplant)
            {
                task.PendingPlanting = true;
                driver.WalkPhase = DriverRescuePhase.LumberPlanting;
                tree.ChopTimer = 0f;
                SessionDebugLogger.Log("LUMBER", $"{driver.DriverName} started planting at ({tree.Cell.x},{tree.Cell.y}).");
            }
            else
            {
                ReturnForestWorkerInside(driver, "felling cycle complete");
            }
            return;
        }

        tree.ChopTimer += Time.deltaTime * gameSpeedMultiplier;
        if (tree.ChopTimer < LumberTreeChopInterval)
        {
            return;
        }

        tree.ChopTimer = 0f;
        tree.ChopHitsCompleted++;
        SpawnLumberWoodChips(driver.DriverObject.transform.position + driver.DriverObject.transform.forward * 0.24f);
        PlayForestWorkerFx(forestChopClip, driver.DriverObject.transform.position, Random.Range(0.55f, 0.82f));

        if (tree.RootTransform != null)
        {
            tree.RootTransform.localRotation = tree.UprightRotation * Quaternion.Euler(0f, 0f, Random.Range(-5f, 5f));
        }

        SessionDebugLogger.Log("LUMBER", $"{driver.DriverName} chopped tree ({tree.Cell.x},{tree.Cell.y}) hit {tree.ChopHitsCompleted}/{LumberTreeChopsRequired}.");

        if (tree.ChopHitsCompleted < LumberTreeChopsRequired)
        {
            return;
        }

        tree.State = LumberTreeRuntimeState.Falling;
        tree.FallTimer = 0f;
        tree.ChopHitsCompleted = 0;
        tree.ChopTimer = 0f;
        SessionDebugLogger.Log("LUMBER", $"Tree ({tree.Cell.x},{tree.Cell.y}) started falling.");
    }

    private void UpdateActiveLumberPlanting(DriverAgent driver, LumberWorkerTaskData task)
    {
        if (!TryGetLumberTree(task.TreeCell, out LumberTreeRuntimeData tree))
        {
            ReturnForestWorkerInside(driver, "planting target vanished");
            return;
        }

        driver.WalkAnimationTime += Time.deltaTime * 2.8f;
        ApplyDriverLumberPlantPose(driver, Mathf.PingPong(driver.WalkAnimationTime * 0.8f, 1f));
        tree.ChopTimer += Time.deltaTime * gameSpeedMultiplier;
        if (tree.ChopTimer < LumberTreePlantDuration)
        {
            return;
        }

        tree.ChopTimer = 0f;
        tree.GrowthStage = 0;
        tree.LastGrowthDay = currentDay;
        tree.NeedsReplant = false;
        tree.State = LumberTreeRuntimeState.Growing;
        ApplyLumberTreeStageVisual(tree);
        task.PendingPlanting = false;
        SessionDebugLogger.Log("LUMBER", $"{driver.DriverName} planted a new tree at ({tree.Cell.x},{tree.Cell.y}).");
        ReturnForestWorkerInside(driver, "tree planted");
    }

    private void FinalizeFelledTree(LumberTreeRuntimeData tree)
    {
        tree.State = LumberTreeRuntimeState.Felled;
        tree.FallTimer = 0f;
        if (tree.RootTransform != null)
        {
            tree.RootTransform.gameObject.SetActive(false);
        }

        int logCount = GetLumberTreeLogYield(tree);
        SpawnGroundLogsForTree(tree, logCount);
        tree.NeedsReplant = true;
        SessionDebugLogger.Log("LUMBER", $"Tree ({tree.Cell.x},{tree.Cell.y}) fell and created {logCount} world logs.");
    }

    private int GetLumberTreeLogYield(LumberTreeRuntimeData tree)
    {
        if (tree == null)
        {
            return 1;
        }

        return tree.GrowthStage switch
        {
            >= 5 => 3,
            >= 4 => 2,
            _ => 1
        };
    }

    private void SpawnGroundLogsForTree(LumberTreeRuntimeData tree, int count)
    {
        tree.GroundLogs.Clear();
        Vector3 center = GetCellCenter(tree.Cell);
        for (int i = 0; i < count; i++)
        {
            GameObject logRoot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            logRoot.name = $"LumberGroundLog_{nextLumberGroundLogId}";
            logRoot.transform.SetParent(miscRoot != null ? miscRoot : worldRoot, false);
            logRoot.transform.position = center + new Vector3(-0.18f + i * 0.18f, 0.11f, -0.06f + (i % 2) * 0.12f);
            logRoot.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 180f), 90f);
            logRoot.transform.localScale = new Vector3(0.12f, 0.22f, 0.12f);
            ApplyColor(logRoot, new Color(0.58f, 0.4f, 0.22f));
            ConfigureStaticVisual(logRoot);

            tree.GroundLogs.Add(new LumberGroundLogData
            {
                Id = nextLumberGroundLogId++,
                RootObject = logRoot,
                WorldPosition = logRoot.transform.position,
                IsCollected = false
            });
        }
    }

    private bool TryAssignNearestGroundLog(LumberWorkerTaskData task, LumberTreeRuntimeData tree, out LumberGroundLogData result)
    {
        result = null;
        if (tree == null)
        {
            return false;
        }

        for (int i = 0; i < tree.GroundLogs.Count; i++)
        {
            LumberGroundLogData log = tree.GroundLogs[i];
            if (log == null || log.IsCollected)
            {
                continue;
            }

            task.CarryLogId = log.Id;
            result = log;
            return true;
        }

        return false;
    }

    private void StartForestWorkerCarryLog(DriverAgent driver, LumberWorkerTaskData task, LumberTreeRuntimeData tree, LumberGroundLogData log)
    {
        if (driver?.DriverObject == null || log == null)
        {
            ReturnForestWorkerInside(driver, "carry log setup failed");
            return;
        }

        log.IsCollected = true;
        if (log.RootObject != null)
        {
            log.RootObject.SetActive(false);
        }

        EnsureForestCarryVisual(driver);
        Vector3 target = GetCellCenter(locations[LocationType.Forest].Anchor) + new Vector3(0f, 0.05f, 0f);
        driver.WalkTargetWorld = target;
        driver.WalkPhase = DriverRescuePhase.LumberCarryLogToBuilding;
        BuildDriverWalkPath(driver, driver.DriverObject.transform.position, target);
        SessionDebugLogger.Log("LUMBER", $"{driver.DriverName} picked up world log #{log.Id} from tree ({tree.Cell.x},{tree.Cell.y}).");
    }

    private void EnsureForestCarryVisual(DriverAgent driver)
    {
        if (driver?.DriverVisualRoot == null)
        {
            return;
        }

        if (!lumberCarryVisuals.TryGetValue(driver.DriverId, out Transform carryVisual) || carryVisual == null)
        {
            GameObject log = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            log.name = $"DriverCarryLog_{driver.DriverId}";
            log.transform.SetParent(driver.DriverVisualRoot, false);
            log.transform.localScale = new Vector3(0.1f, 0.18f, 0.1f);
            ApplyColor(log, new Color(0.58f, 0.4f, 0.22f));
            ConfigureShadowVisual(log);
            carryVisual = log.transform;
            lumberCarryVisuals[driver.DriverId] = carryVisual;
        }

        carryVisual.gameObject.SetActive(true);
        carryVisual.localPosition = new Vector3(0.16f, 0.46f + LumberTreeCarryLogLift, 0.08f);
        carryVisual.localRotation = Quaternion.Euler(0f, 0f, 90f);
    }

    private void HideForestCarryVisual(DriverAgent driver)
    {
        if (driver != null &&
            lumberCarryVisuals.TryGetValue(driver.DriverId, out Transform carryVisual) &&
            carryVisual != null)
        {
            carryVisual.gameObject.SetActive(false);
        }
    }

    private void ReturnForestWorkerInside(DriverAgent driver, string reason)
    {
        if (driver == null)
        {
            return;
        }

        driver.WalkPhase = DriverRescuePhase.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;
        driver.IsInsideBuilding = true;
        HideForestCarryVisual(driver);
        if (driver.DriverObject != null)
        {
            driver.DriverObject.SetActive(false);
        }

        SessionDebugLogger.Log("LUMBER", $"{driver.DriverName} returned inside Lumberyard: {reason}.");
    }

    private bool TryGetLumberTree(Vector2Int cell, out LumberTreeRuntimeData tree)
    {
        return lumberTrees.TryGetValue(cell, out tree) && tree != null;
    }

    private Vector3 GetLumberTreeWorkPoint(LumberTreeRuntimeData tree)
    {
        Vector3 treeCenter = GetCellCenter(tree.Cell);
        Vector3 forestCenter = GetLocationCenter(LocationType.Forest);
        Vector3 away = (treeCenter - forestCenter);
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
        {
            away = Vector3.forward;
        }

        away.Normalize();
        Vector3 point = treeCenter - away * 0.34f;
        point.y = 0.05f;
        return point;
    }

    private void ApplyLumberTreeStageVisual(LumberTreeRuntimeData tree)
    {
        if (tree?.RootTransform == null)
        {
            return;
        }

        float normalized = tree.GrowthStage / (float)LumberTreeMaxGrowthStage;
        float visualScale = Mathf.Lerp(0.16f, 1f, normalized);
        tree.RootTransform.gameObject.SetActive(true);
        tree.RootTransform.localScale = Vector3.one * (tree.BaseScale * visualScale);
        tree.RootTransform.localRotation = tree.UprightRotation;
    }

    private void ApplyDriverLumberChopPose(DriverAgent driver, float phase)
    {
        if (driver?.DriverVisualRoot == null)
        {
            return;
        }

        float bodyBob = Mathf.Sin(Time.time * 7.5f + driver.DriverId) * 0.02f;
        driver.DriverVisualRoot.localPosition = new Vector3(0f, bodyBob, 0f);

        if (driver.DriverBodyTransform != null)
            driver.DriverBodyTransform.localRotation = Quaternion.Euler(Mathf.Lerp(8f, -10f, phase), 0f, 0f);
        if (driver.DriverHeadTransform != null)
            driver.DriverHeadTransform.localRotation = Quaternion.Euler(Mathf.Lerp(-6f, 4f, phase), 0f, 0f);
        if (driver.DriverLeftArmTransform != null)
            driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(Mathf.Lerp(20f, -26f, phase), 0f, -10f);
        if (driver.DriverRightArmTransform != null)
            driver.DriverRightArmTransform.localRotation = Quaternion.Euler(Mathf.Lerp(-86f, 108f, phase), 0f, 12f);
        if (driver.DriverLeftLegTransform != null)
            driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(-8f, 0f, 0f);
        if (driver.DriverRightLegTransform != null)
            driver.DriverRightLegTransform.localRotation = Quaternion.Euler(10f, 0f, 0f);
    }

    private void ApplyDriverLumberPlantPose(DriverAgent driver, float phase)
    {
        if (driver?.DriverVisualRoot == null)
        {
            return;
        }

        driver.DriverVisualRoot.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 5f + driver.DriverId) * 0.01f, 0f);
        if (driver.DriverBodyTransform != null)
            driver.DriverBodyTransform.localRotation = Quaternion.Euler(Mathf.Lerp(16f, 28f, phase), 0f, 0f);
        if (driver.DriverHeadTransform != null)
            driver.DriverHeadTransform.localRotation = Quaternion.Euler(12f, 0f, 0f);
        if (driver.DriverLeftArmTransform != null)
            driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(Mathf.Lerp(24f, 52f, phase), 0f, 0f);
        if (driver.DriverRightArmTransform != null)
            driver.DriverRightArmTransform.localRotation = Quaternion.Euler(Mathf.Lerp(18f, 48f, phase), 0f, 0f);
        if (driver.DriverLeftLegTransform != null)
            driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(-6f, 0f, 0f);
        if (driver.DriverRightLegTransform != null)
            driver.DriverRightLegTransform.localRotation = Quaternion.Euler(6f, 0f, 0f);
    }

    private void SpawnLumberWoodChips(Vector3 position)
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject chip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chip.transform.SetParent(worldRoot, false);
            chip.transform.position = position + new Vector3(Random.Range(-0.08f, 0.08f), 0.12f, Random.Range(-0.08f, 0.08f));
            chip.transform.localScale = new Vector3(0.03f, 0.015f, 0.02f);
            ApplyColor(chip, new Color(0.8f, 0.68f, 0.34f));
            ConfigureStaticVisual(chip);
            Destroy(chip, 0.55f);
        }
    }

    private void DeliverForestWorkerLog(DriverAgent driver, LumberWorkerTaskData task)
    {
        HideForestCarryVisual(driver);
        if (locations.TryGetValue(LocationType.Forest, out LocationData forestLocation))
        {
            forestLocation.LogsStored = Mathf.Min(ForestMaxLogsStorage, forestLocation.LogsStored + 1);
            RefreshForestStoredLogsVisual();
            SessionDebugLogger.Log("LUMBER", $"{driver.DriverName} delivered 1 log to Lumberyard. Stored={forestLocation.LogsStored}/{ForestMaxLogsStorage}.");
        }

        task.CarryLogId = -1;
        if (!TryGetLumberTree(task.TreeCell, out LumberTreeRuntimeData tree))
        {
            ReturnForestWorkerInside(driver, "source tree no longer exists after delivery");
            return;
        }

        if (HasUncollectedGroundLogs(tree) && locations[LocationType.Forest].LogsStored < ForestMaxLogsStorage)
        {
            Vector3 target = GetLumberTreeWorkPoint(tree);
            driver.WalkTargetWorld = target;
            driver.WalkPhase = DriverRescuePhase.LumberToTree;
            BuildDriverWalkPath(driver, GetCellCenter(locations[LocationType.Forest].Anchor) + new Vector3(0f, 0.05f, 0f), target);
            return;
        }

        if (tree.NeedsReplant)
        {
            task.PendingPlanting = true;
            Vector3 target = GetLumberTreeWorkPoint(tree);
            driver.WalkTargetWorld = target;
            driver.WalkPhase = DriverRescuePhase.LumberReturnToTreeForPlanting;
            BuildDriverWalkPath(driver, GetCellCenter(locations[LocationType.Forest].Anchor) + new Vector3(0f, 0.05f, 0f), target);
            return;
        }

        ReturnForestWorkerInside(driver, "delivered final log");
    }

    private void CancelForestFieldWork(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        HideForestCarryVisual(driver);
        if (lumberWorkerTasks.TryGetValue(driver.DriverId, out LumberWorkerTaskData task))
        {
            task.CarryLogId = -1;
            task.PendingPlanting = false;
        }
    }
}
