using UnityEngine;

public partial class GameBootstrap
{
    private const float CleanerSweepDuration = 2.2f;
    private const float CleanerSearchPauseMin = 0.8f;
    private const float CleanerSearchPauseMax = 2.1f;

    private void UpdateStreetCleaningWorker(DriverAgent driver)
    {
        if (driver == null ||
            driver.DutyMode != DriverDutyMode.Logistics ||
            driver.AssignedBuildingType != LocationType.CleaningDepot ||
            !driver.IsOnActiveShift)
        {
            return;
        }

        if (driver.WalkPhase == DriverRescuePhase.CleanerCleaning)
        {
            UpdateActiveStreetCleaning(driver);
            return;
        }

        if (driver.WalkPhase != DriverRescuePhase.None)
        {
            return;
        }

        if (!driver.IsInsideBuilding)
        {
            return;
        }

        float dt = Time.deltaTime * Mathf.Max(0f, gameSpeedMultiplier);
        if (driver.CleanerSearchCooldown > 0f)
        {
            driver.CleanerSearchCooldown = Mathf.Max(0f, driver.CleanerSearchCooldown - dt);
            return;
        }

        TryDispatchCleanerFromDepot(driver);
    }

    private void TryDispatchCleanerFromDepot(DriverAgent driver)
    {
        LocationData depot = GetAssignedBuildingLocation(driver);
        if (depot == null)
        {
            return;
        }

        Vector3 startWorld = GetCleaningDepotExitWorld(depot);
        if (!TryReserveStreetLitterCleanupTarget(driver, startWorld, out _, out Vector3 targetWorld))
        {
            driver.CleanerSearchCooldown = Random.Range(CleanerSearchPauseMin, CleanerSearchPauseMax);
            return;
        }

        SpawnCleanerOutside(driver, depot);
        StartCleanerWalkToReservedTarget(driver, targetWorld);
    }

    private void SpawnCleanerOutside(DriverAgent driver, LocationData depot)
    {
        if (driver?.DriverObject == null || depot == null)
        {
            return;
        }

        Vector3 spawnPos = GetCleaningDepotExitWorld(depot);
        driver.IsInsideBuilding = false;
        driver.InsideBuildingType = null;
        driver.InsideBuildingInstanceId = 0;
        driver.DriverObject.SetActive(true);
        driver.DriverObject.transform.position = spawnPos;
        driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        driver.WalkAnimationTime = 0f;
        ApplyDriverPose(driver, 0f, 0f);
    }

    private void StartCleanerWalkToReservedTarget(DriverAgent driver, Vector3 targetWorld)
    {
        if (driver?.DriverObject == null || !IsStreetLitterCleanupTargetValid(driver))
        {
            ReleaseStreetLitterReservation(driver);
            ReturnCleanerInside(driver, "cleanup target disappeared before dispatch");
            return;
        }

        driver.WalkTargetWorld = targetWorld;
        driver.WalkPhase = DriverRescuePhase.CleanerToLitter;
        driver.WalkAnimationTime = 0f;
        if (!BuildDriverWalkPath(driver, driver.DriverObject.transform.position, targetWorld))
        {
            ReleaseStreetLitterReservation(driver);
            ReturnCleanerInside(driver, "could not reach street litter");
        }
    }

    private void StartCleanerCleaningAtTarget(DriverAgent driver)
    {
        if (driver?.DriverObject == null || !IsStreetLitterCleanupTargetValid(driver))
        {
            ReleaseStreetLitterReservation(driver);
            if (!TryStartCleanerNextTargetFromPosition(driver))
            {
                StartCleanerReturnToDepot(driver, "cleanup target vanished");
            }
            return;
        }

        Vector3 targetWorld = GetStreetLitterCleanupWorld(driver.CleanerTargetCell);
        Vector3 look = targetWorld - driver.DriverObject.transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.0001f)
        {
            driver.DriverObject.transform.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
        }

        driver.WalkPhase = DriverRescuePhase.CleanerCleaning;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.CleanerActionTimer = CleanerSweepDuration;
        driver.WalkTargetWorld = driver.DriverObject.transform.position;
        driver.WalkAnimationTime = 0f;
        SessionDebugLogger.Log("CLEANING", $"{driver.DriverName} started cleaning street litter at ({driver.CleanerTargetCell.x},{driver.CleanerTargetCell.y}).");
    }

    private void UpdateActiveStreetCleaning(DriverAgent driver)
    {
        if (driver?.DriverObject == null)
        {
            ReleaseStreetLitterReservation(driver);
            return;
        }

        if (!IsStreetLitterCleanupTargetValid(driver))
        {
            ReleaseStreetLitterReservation(driver);
            if (!TryStartCleanerNextTargetFromPosition(driver))
            {
                StartCleanerReturnToDepot(driver, "cleanup target already gone");
            }
            return;
        }

        float dt = Time.deltaTime * Mathf.Max(0f, gameSpeedMultiplier);
        driver.CleanerActionTimer = Mathf.Max(0f, driver.CleanerActionTimer - dt);
        driver.WalkAnimationTime += Time.deltaTime * 5.5f;
        ApplyCleanerSweepPose(driver, Mathf.PingPong(driver.WalkAnimationTime * 0.65f, 1f));

        if (driver.CleanerActionTimer > 0f)
        {
            return;
        }

        Vector2Int cleanedCell = driver.CleanerTargetCell;
        if (CleanReservedStreetLitterTarget(driver))
        {
            SessionDebugLogger.Log("CLEANING", $"{driver.DriverName} cleaned street litter at ({cleanedCell.x},{cleanedCell.y}).");
        }

        ApplyDriverPose(driver, 0f, 0f);
        if (!TryStartCleanerNextTargetFromPosition(driver))
        {
            StartCleanerReturnToDepot(driver, "no more visible street litter nearby");
        }
    }

    private bool TryStartCleanerNextTargetFromPosition(DriverAgent driver)
    {
        if (driver?.DriverObject == null)
        {
            return false;
        }

        Vector3 startWorld = driver.DriverObject.transform.position;
        if (!TryReserveStreetLitterCleanupTarget(driver, startWorld, out _, out Vector3 targetWorld))
        {
            return false;
        }

        StartCleanerWalkToReservedTarget(driver, targetWorld);
        return driver.WalkPhase == DriverRescuePhase.CleanerToLitter ||
               driver.WalkPhase == DriverRescuePhase.CleanerCleaning;
    }

    private void StartCleanerReturnToDepot(DriverAgent driver, string reason)
    {
        if (driver?.DriverObject == null)
        {
            ReturnCleanerInside(driver, reason);
            return;
        }

        ReleaseStreetLitterReservation(driver);
        LocationData depot = GetAssignedBuildingLocation(driver);
        if (depot == null)
        {
            driver.WalkPhase = DriverRescuePhase.None;
            driver.WalkPath.Clear();
            driver.WalkWaypointIndex = 0;
            driver.CleanerActionTimer = 0f;
            return;
        }

        Vector3 target = GetCleaningDepotExitWorld(depot);
        driver.WalkTargetWorld = target;
        driver.WalkPhase = DriverRescuePhase.CleanerReturnToDepot;
        driver.WalkAnimationTime = 0f;
        if (!BuildDriverWalkPath(driver, driver.DriverObject.transform.position, target))
        {
            ReturnCleanerInside(driver, reason);
            return;
        }

        SessionDebugLogger.Log("CLEANING", $"{driver.DriverName} returning to Cleaning Depot after {reason}.");
    }

    private void ReturnCleanerInside(DriverAgent driver, string reason)
    {
        if (driver == null)
        {
            return;
        }

        ReleaseStreetLitterReservation(driver);
        driver.WalkPhase = DriverRescuePhase.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;
        driver.CleanerActionTimer = 0f;
        driver.CleanerSearchCooldown = Random.Range(CleanerSearchPauseMin, CleanerSearchPauseMax);
        driver.IsInsideBuilding = true;

        LocationData depot = GetAssignedBuildingLocation(driver);
        if (depot != null)
        {
            driver.InsideBuildingType = depot.Type;
            driver.InsideBuildingInstanceId = depot.InstanceId;
        }

        if (driver.DriverObject != null)
        {
            ApplyDriverPose(driver, 0f, 0f);
            driver.DriverObject.SetActive(false);
        }

        SessionDebugLogger.Log("CLEANING", $"{driver.DriverName} returned inside Cleaning Depot: {reason}.");
    }

    private void CancelStreetCleaningWork(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        ReleaseStreetLitterReservation(driver);
        driver.CleanerActionTimer = 0f;
        driver.CleanerSearchCooldown = 0f;
        if (driver.WalkPhase == DriverRescuePhase.CleanerToLitter ||
            driver.WalkPhase == DriverRescuePhase.CleanerCleaning ||
            driver.WalkPhase == DriverRescuePhase.CleanerReturnToDepot)
        {
            driver.WalkPhase = DriverRescuePhase.None;
            driver.WalkPath.Clear();
            driver.WalkWaypointIndex = 0;
            driver.WalkAnimationTime = 0f;
            if (driver.DriverObject != null)
            {
                ApplyDriverPose(driver, 0f, 0f);
            }
        }
    }

    private bool TryFinishActiveStreetCleaningShift(DriverAgent driver, LocationData building)
    {
        if (driver?.AssignedBuildingType != LocationType.CleaningDepot ||
            driver.DriverObject == null ||
            !driver.DriverObject.activeSelf)
        {
            return false;
        }

        CancelStreetCleaningWork(driver);
        if (building != null)
        {
            building.Workers = Mathf.Max(0, building.Workers - 1);
        }

        driver.IsOnActiveShift = false;
        driver.IsInsideBuilding = false;
        driver.InsideBuildingType = null;
        driver.InsideBuildingInstanceId = 0;
        driver.IsShiftSalaryPending = true;
        PayDriverSalary(driver);
        StartWorkerLifeCycleAfterWork(driver, driver.DriverObject.transform.position, "Street cleaning");
        return true;
    }

    private Vector3 GetCleaningDepotExitWorld(LocationData depot)
    {
        Vector3 position = GetCellCenter(depot.Anchor);
        position.y = SampleTerrainHeight(position.x, position.z) + 0.05f;
        return position;
    }

    private void ApplyCleanerSweepPose(DriverAgent driver, float phase)
    {
        if (driver?.DriverVisualRoot == null)
        {
            return;
        }

        driver.DriverVisualRoot.localPosition = new Vector3(0f, Mathf.Sin(Time.time * 7f + driver.DriverId) * 0.012f, 0f);
        if (driver.DriverBodyTransform != null)
            driver.DriverBodyTransform.localRotation = Quaternion.Euler(Mathf.Lerp(10f, 22f, phase), 0f, 0f);
        if (driver.DriverHeadTransform != null)
            driver.DriverHeadTransform.localRotation = Quaternion.Euler(Mathf.Lerp(2f, 10f, phase), 0f, 0f);
        if (driver.DriverLeftArmTransform != null)
            driver.DriverLeftArmTransform.localRotation = Quaternion.Euler(Mathf.Lerp(18f, 54f, phase), 0f, -10f);
        if (driver.DriverRightArmTransform != null)
            driver.DriverRightArmTransform.localRotation = Quaternion.Euler(Mathf.Lerp(58f, 18f, phase), 0f, 14f);
        if (driver.DriverLeftLegTransform != null)
            driver.DriverLeftLegTransform.localRotation = Quaternion.Euler(-6f, 0f, 0f);
        if (driver.DriverRightLegTransform != null)
            driver.DriverRightLegTransform.localRotation = Quaternion.Euler(7f, 0f, 0f);

        ApplyImportedDriverPoseMotion(driver, ImportedDriverPoseKind.CleanerSweep, phase);
    }
}
