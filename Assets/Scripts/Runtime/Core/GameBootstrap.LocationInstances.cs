using System.Collections.Generic;

public partial class GameBootstrap
{
    private LocationData FindLocationByInstanceId(int instanceId)
    {
        if (instanceId <= 0)
        {
            return null;
        }

        foreach (LocationData location in locations.Values)
        {
            if (location != null && location.InstanceId == instanceId)
            {
                return location;
            }
        }

        for (int i = 0; i < extraServiceLocations.Count; i++)
        {
            LocationData location = extraServiceLocations[i];
            if (location != null && location.InstanceId == instanceId)
            {
                return location;
            }
        }

        return null;
    }

    private LocationData GetAssignedBuildingLocation(DriverAgent driver)
    {
        if (driver == null || !driver.AssignedBuildingType.HasValue)
        {
            return null;
        }

        LocationData instance = FindLocationByInstanceId(driver.AssignedBuildingInstanceId);
        if (instance != null && instance.Type == driver.AssignedBuildingType.Value)
        {
            return instance;
        }

        return locations.TryGetValue(driver.AssignedBuildingType.Value, out LocationData primary)
            ? primary
            : null;
    }

    private int ResolveBuildingInstanceId(LocationType buildingType, int requestedInstanceId)
    {
        LocationData requested = FindLocationByInstanceId(requestedInstanceId);
        if (requested != null && requested.Type == buildingType)
        {
            return requested.InstanceId;
        }

        return locations.TryGetValue(buildingType, out LocationData primary)
            ? primary.InstanceId
            : 0;
    }

    private bool IsLocationInstanceBuilt(LocationType buildingType, int instanceId)
    {
        int resolved = ResolveBuildingInstanceId(buildingType, instanceId);
        return resolved > 0 && FindLocationByInstanceId(resolved) != null;
    }

    private bool IsDriverAssignedToBuildingSlot(DriverAgent driver, LocationType buildingType, int instanceId)
    {
        if (driver == null ||
            driver.DutyMode != DriverDutyMode.Logistics ||
            driver.AssignedBuildingType != buildingType)
        {
            return false;
        }

        int resolved = ResolveBuildingInstanceId(buildingType, instanceId);
        return resolved <= 0 || driver.AssignedBuildingInstanceId == resolved;
    }

    private IEnumerable<LocationData> EnumerateAssignableBuildingLocations(LocationType buildingType)
    {
        if (locations.TryGetValue(buildingType, out LocationData primary))
        {
            yield return primary;
        }

        for (int i = 0; i < extraServiceLocations.Count; i++)
        {
            LocationData location = extraServiceLocations[i];
            if (location != null && location.Type == buildingType)
            {
                yield return location;
            }
        }
    }

    private int GetLocationInstanceOrdinal(LocationType buildingType, int instanceId)
    {
        int ordinal = 0;
        foreach (LocationData location in EnumerateAssignableBuildingLocations(buildingType))
        {
            ordinal++;
            if (location.InstanceId == instanceId)
            {
                return ordinal;
            }
        }

        return 0;
    }

    private string GetBuildingInstanceDisplayName(LocationType buildingType, int instanceId)
    {
        string baseName = GetSelectedLocationDisplayName(buildingType);
        int ordinal = GetLocationInstanceOrdinal(buildingType, ResolveBuildingInstanceId(buildingType, instanceId));
        if (ordinal <= 1 && !IsMultiInstanceServiceBuildType(buildingType))
        {
            return baseName;
        }

        return ordinal > 0 ? $"{baseName} #{ordinal}" : baseName;
    }
}
