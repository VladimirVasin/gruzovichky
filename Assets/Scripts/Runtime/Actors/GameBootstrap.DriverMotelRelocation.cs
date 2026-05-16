using UnityEngine;

public partial class GameBootstrap
{
    private void CompleteDriverMotelRelocationWithoutWalkPath(
        DriverAgent driver,
        Vector3 target,
        DriverRescuePhase interruptedPhase,
        string reason)
    {
        if (driver?.DriverObject == null || !locations.ContainsKey(LocationType.Motel))
        {
            return;
        }

        Vector2Int targetCell = WorldToCell(target);
        if (!IsDriverIdleStandCell(targetCell))
        {
            target = GetDriverIdleMotelPosition(driver.DriverId - 1, driver);
            targetCell = WorldToCell(target);
        }

        if (!IsDriverIdleStandCell(targetCell) &&
            TryFindNearestDriverSafeWalkCell(targetCell, out Vector2Int safeCell))
        {
            target = GetCellCenter(safeCell);
            targetCell = safeCell;
        }

        ReleaseBench(driver);
        ReleaseCatInteraction(driver);
        if (interruptedPhase == DriverRescuePhase.IdleSmoking ||
            driver.WalkPhase == DriverRescuePhase.IdleSmoking)
        {
            StopDriverSmokingParticles(driver);
        }

        target.y = SampleTerrainHeight(target.x, target.z);
        driver.DriverObject.SetActive(true);
        driver.DriverObject.transform.position = target;
        driver.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
        driver.MotelIdlePosition = target;
        driver.WalkTargetWorld = target;
        driver.WalkPhase = DriverRescuePhase.None;
        driver.WalkPath.Clear();
        driver.WalkWaypointIndex = 0;
        driver.WalkAnimationTime = 0f;
        driver.IdleActivityTimer = 0f;
        driver.IdleWanderPauseTimer = Random.Range(0.8f, 1.8f);
        driver.IdleWanderPointIndex = -1;
        driver.IdleConversationTimer = 0f;
        driver.IdleConversationPartnerId = -1;
        driver.PendingVendorLocationInstanceId = 0;
        driver.PendingVendorItemId = string.Empty;
        driver.IsArrivingByBus = false;
        driver.IsInsideBuilding = false;
        driver.InsideBuildingType = null;
        driver.InsideBuildingInstanceId = 0;
        ApplyDriverPose(driver, 0f, 0f);
        UpdateDriverLastSafeWalkPosition(driver, target);
        SessionDebugLogger.Log(
            "DRIVER",
            $"{driver.DriverName} relocated to Motel idle area without a walk path; cell=({targetCell.x},{targetCell.y}); reason={reason}.");
    }
}
