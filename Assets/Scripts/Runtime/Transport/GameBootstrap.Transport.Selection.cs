using UnityEngine;

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

    private bool TryHandleLocalBusSelection(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, 200f))
        {
            return false;
        }

        if (hit.transform == null || localBusRoute?.RootTransform == null)
        {
            return false;
        }

        if (!hit.transform.IsChildOf(localBusRoute.RootTransform))
        {
            return false;
        }

        FocusLocalBus();
        return true;
    }

    private bool TryHandleDriverSelection(Ray ray, Vector2 mousePosition)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, 200f, ~0, QueryTriggerInteraction.Collide);
        float bestHitDistance = float.PositiveInfinity;
        DriverAgent bestHitDriver = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == null)
            {
                continue;
            }

            foreach (DriverAgent driver in driverAgents)
            {
                if (!IsDriverWorldClickable(driver) ||
                    !hitTransform.IsChildOf(driver.DriverObject.transform) ||
                    hits[i].distance >= bestHitDistance)
                {
                    continue;
                }

                bestHitDistance = hits[i].distance;
                bestHitDriver = driver;
            }
        }

        if (bestHitDriver != null)
        {
            FocusDriver(bestHitDriver.DriverId);
            return true;
        }

        if (TryFindDriverAtScreenPosition(mousePosition, ray, out DriverAgent screenDriver))
        {
            FocusDriver(screenDriver.DriverId);
            return true;
        }

        return false;
    }

    private bool TryFindDriverAtScreenPosition(Vector2 mousePosition, Ray ray, out DriverAgent result)
    {
        result = null;
        if (mainCamera == null)
        {
            return false;
        }

        float zoomT = Mathf.InverseLerp(CameraMinHeight, CameraMaxHeight, cameraOffset.y);
        float pickRadius = Mathf.Lerp(28f, 48f, zoomT);
        float pickRadiusSqr = pickRadius * pickRadius;
        float bestScore = float.PositiveInfinity;
        Vector3 rayDirection = ray.direction.sqrMagnitude > 0.0001f ? ray.direction.normalized : Vector3.forward;

        foreach (DriverAgent driver in driverAgents)
        {
            if (!IsDriverWorldClickable(driver))
            {
                continue;
            }

            Vector3 driverPosition = driver.DriverObject.transform.position + Vector3.up * 0.65f;
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(driverPosition);
            if (screenPosition.z <= 0f)
            {
                continue;
            }

            float screenDistanceSqr = ((Vector2)screenPosition - mousePosition).sqrMagnitude;
            if (screenDistanceSqr > pickRadiusSqr)
            {
                continue;
            }

            float rayDistance = Vector3.Cross(rayDirection, driverPosition - ray.origin).magnitude;
            if (rayDistance > 1.05f)
            {
                continue;
            }

            float score = screenDistanceSqr + screenPosition.z * 0.001f;
            if (score >= bestScore)
            {
                continue;
            }

            bestScore = score;
            result = driver;
        }

        return result != null;
    }

    private static bool IsDriverWorldClickable(DriverAgent driver)
    {
        return driver != null &&
               driver.DriverObject != null &&
               driver.DriverObject.activeSelf &&
               !driver.IsInsideBuilding &&
               !driver.HasDepartedTown;
    }
}
