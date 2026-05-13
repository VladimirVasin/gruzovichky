using UnityEngine;

public partial class GameBootstrap
{
    private static readonly Vector3 WorkerSelectionHighlightSize = new(1.05f, 0.06f, 1.05f);
    private static readonly Vector3 TruckSelectionHighlightSize = new(2.25f, 0.06f, 3.25f);
    private static readonly Vector3 BusSelectionHighlightSize = new(2.35f, 0.06f, 3.65f);
    private static readonly Vector3 PersonalCarSelectionHighlightSize = new(1.65f, 0.06f, 2.45f);

    private void FocusWorkerFromQuickHud(int driverId, string source)
    {
        if (driverId <= 0)
        {
            return;
        }

        DriverAgent driver = driverAgents.Find(d => d.DriverId == driverId);
        if (driver == null)
        {
            return;
        }

        LogUiInput($"Quick HUD: focused {driver.DriverName} from {source}");
        FocusDriver(driverId);
    }

    private bool CanFocusDriver(DriverAgent driver)
    {
        return TryResolveDriverFocusTarget(driver, out _, out _, out _);
    }

    private bool TryFocusCameraOnDriver(DriverAgent driver, out string targetLabel)
    {
        targetLabel = string.Empty;
        if (!TryResolveDriverFocusTarget(driver, out Vector3 targetPosition, out _, out targetLabel))
        {
            return false;
        }

        FocusCameraOnWorldPosition(targetPosition);
        return true;
    }

    private void FocusCameraOnWorldPosition(Vector3 targetPosition)
    {
        DisableTruckCameraFocus();
        cameraFocusPoint = new Vector3(targetPosition.x, 0f, targetPosition.z);
        ClampCameraFocus();
        if (cameraOffset.sqrMagnitude < 0.0001f)
        {
            cameraOffset = DioramaCameraOffset;
        }

        cameraTargetOffset = cameraOffset;
        isCameraReturningToDiorama = true;
        isCameraRotatingToTarget = false;
    }

    private bool TryGetSelectedEntityHighlightTarget(out Vector3 position, out Vector3 size)
    {
        position = Vector3.zero;
        size = Vector3.zero;

        if (isDriverDetailsOpen && selectedDriverId > 0)
        {
            DriverAgent driver = driverAgents.Find(d => d.DriverId == selectedDriverId);
            return TryResolveDriverFocusTarget(driver, out position, out size, out _);
        }

        if (isTruckDetailsOpen && TryResolveTruckFocusTarget(GetTruckAgent(selectedTruckNumber), out position, out size, out _))
        {
            return true;
        }

        if (isLocalBusDetailsOpen && TryResolveLocalBusFocusTarget(out position, out size, out _))
        {
            return true;
        }

        return false;
    }

    private bool TryResolveDriverFocusTarget(DriverAgent driver, out Vector3 position, out Vector3 size, out string label)
    {
        position = Vector3.zero;
        size = Vector3.zero;
        label = string.Empty;

        if (driver == null)
        {
            return false;
        }

        if (TryResolveHiringBusFocusTarget(driver, out position, out size, out label))
        {
            return true;
        }

        if ((driver.WalkPhase == DriverRescuePhase.RidingLocalBus || localBusRoute?.Driver == driver) &&
            TryResolveLocalBusFocusTarget(out position, out size, out label))
        {
            return true;
        }

        if (TryResolveTruckFocusTarget(GetCurrentTruckForDriver(driver), out position, out size, out label))
        {
            return true;
        }

        if (TryResolvePersonalCarFocusTarget(driver, out position, out size, out label))
        {
            return true;
        }

        if (driver.DriverObject != null && driver.DriverObject.activeSelf && !driver.IsInsideBuilding)
        {
            position = driver.DriverObject.transform.position;
            size = WorkerSelectionHighlightSize;
            label = driver.DriverName;
            return true;
        }

        if (TryResolveDriverBuildingFocusTarget(driver, out position, out size, out label))
        {
            return true;
        }

        return TryResolveTruckFocusTarget(GetAssignedTruckForDriver(driver), out position, out size, out label);
    }

    private bool TryResolveHiringBusFocusTarget(DriverAgent driver, out Vector3 position, out Vector3 size, out string label)
    {
        position = Vector3.zero;
        size = Vector3.zero;
        label = string.Empty;

        if (!driver.IsArrivingByBus ||
            hiringDriverArrival?.BusRootTransform == null ||
            (driver.DriverObject != null && driver.DriverObject.activeSelf))
        {
            return false;
        }

        if (hiringDriverArrival.Driver != driver && !hiringDriverArrival.Drivers.Contains(driver))
        {
            return false;
        }

        position = hiringDriverArrival.BusRootTransform.position;
        size = BusSelectionHighlightSize;
        label = "Arrival Bus";
        return true;
    }

    private bool TryResolveLocalBusFocusTarget(out Vector3 position, out Vector3 size, out string label)
    {
        position = Vector3.zero;
        size = Vector3.zero;
        label = string.Empty;

        if (localBusRoute?.RootTransform == null)
        {
            return false;
        }

        position = localBusRoute.RootTransform.position;
        size = BusSelectionHighlightSize;
        label = localBusRoute.Bus?.DisplayName ?? "Local Bus";
        return true;
    }

    private bool TryResolveTruckFocusTarget(TruckAgent truck, out Vector3 position, out Vector3 size, out string label)
    {
        position = Vector3.zero;
        size = Vector3.zero;
        label = string.Empty;

        if (truck?.TruckObject == null || !truck.TruckObject.activeSelf)
        {
            return false;
        }

        position = truck.TruckObject.transform.position;
        size = TruckSelectionHighlightSize;
        label = truck.DisplayName;
        return true;
    }

    private bool TryResolvePersonalCarFocusTarget(DriverAgent driver, out Vector3 position, out Vector3 size, out string label)
    {
        position = Vector3.zero;
        size = Vector3.zero;
        label = string.Empty;

        if (driver?.OwnedCarObject == null || !driver.IsDrivingPersonalCar || !driver.OwnedCarObject.activeSelf)
        {
            return false;
        }

        position = driver.OwnedCarObject.transform.position;
        size = PersonalCarSelectionHighlightSize;
        label = driver.OwnedCarModelIndex >= 0 && driver.OwnedCarModelIndex < CarModelNames.Length
            ? CarModelNames[driver.OwnedCarModelIndex]
            : "Personal Car";
        return true;
    }

    private bool TryResolveDriverBuildingFocusTarget(DriverAgent driver, out Vector3 position, out Vector3 size, out string label)
    {
        position = Vector3.zero;
        size = Vector3.zero;
        label = string.Empty;

        if (driver == null)
        {
            return false;
        }

        if (driver.RestPhase == DriverRestPhase.SleepingAtHome ||
            driver.WalkPhase == DriverRescuePhase.IdleAtPersonalHouseMeal ||
            driver.InsideBuildingType == LocationType.PersonalHouse)
        {
            return TryResolvePersonalHouseFocusTarget(driver, out position, out size, out label);
        }

        if (driver.RestPhase == DriverRestPhase.Sleeping)
        {
            return TryResolveLocationFocusTarget(LocationType.Motel, 0, out position, out size, out label);
        }

        if (driver.IsInsideBuilding)
        {
            if (driver.InsideBuildingType.HasValue &&
                TryResolveLocationFocusTarget(driver.InsideBuildingType.Value, driver.InsideBuildingInstanceId, out position, out size, out label))
            {
                return true;
            }

            if (driver.AssignedBuildingType.HasValue &&
                TryResolveLocationFocusTarget(driver.AssignedBuildingType.Value, driver.AssignedBuildingInstanceId, out position, out size, out label))
            {
                return true;
            }
        }

        LocationType? serviceLocation = GetDriverServiceLocation(driver.WalkPhase);
        if (serviceLocation.HasValue &&
            TryResolveLocationFocusTarget(serviceLocation.Value, driver.InsideBuildingInstanceId, out position, out size, out label))
        {
            return true;
        }

        if (driver.AssignedBuildingType.HasValue && driver.IsOnActiveShift)
        {
            return TryResolveLocationFocusTarget(driver.AssignedBuildingType.Value, driver.AssignedBuildingInstanceId, out position, out size, out label);
        }

        return false;
    }

    private bool TryResolvePersonalHouseFocusTarget(DriverAgent driver, out Vector3 position, out Vector3 size, out string label)
    {
        position = Vector3.zero;
        size = Vector3.zero;
        label = string.Empty;

        if (driver == null ||
            driver.AssignedPersonalHouseIndex < 0 ||
            driver.AssignedPersonalHouseIndex >= personalHouses.Count)
        {
            return false;
        }

        LocationData house = personalHouses[driver.AssignedPersonalHouseIndex];
        position = GetLocationCenter(house);
        size = GetLocationSelectionHighlightSize(house);
        label = house.Label;
        return true;
    }

    private bool TryResolveLocationFocusTarget(LocationType locationType, int instanceId, out Vector3 position, out Vector3 size, out string label)
    {
        position = Vector3.zero;
        size = Vector3.zero;
        label = string.Empty;

        LocationData location = null;
        if (instanceId > 0)
        {
            location = FindLocationByInstanceId(instanceId);
            if (location != null && location.Type != locationType)
            {
                location = null;
            }
        }

        if (location == null && !locations.TryGetValue(locationType, out location))
        {
            return false;
        }

        position = GetLocationCenter(location);
        size = GetLocationSelectionHighlightSize(location);
        label = GetBuildingInstanceDisplayName(location.Type, location.InstanceId);
        return true;
    }

    private string FormatWorkerPerksInline(DriverAgent driver, bool ru, int maxVisible = 3)
    {
        if (driver == null)
        {
            return "-";
        }

        EnsureWorkerPerks(driver);

        string text = string.Empty;
        int count = driver.Traits.Count < maxVisible ? driver.Traits.Count : maxVisible;
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                text += ", ";
            }

            text += GetWorkerTraitDisplayName(driver.Traits[i], ru);
        }

        if (driver.Traits.Count > count)
        {
            text += ru ? $" +{driver.Traits.Count - count}" : $" +{driver.Traits.Count - count}";
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            text = ru ? "\u0431\u0435\u0437 \u0447\u0435\u0440\u0442" : "no traits";
        }

        if (driver.Weakness != WorkerWeaknessKind.None)
        {
            text += $" / {GetWorkerWeaknessDisplayName(driver.Weakness, ru)}";
        }

        return text;
    }
}
