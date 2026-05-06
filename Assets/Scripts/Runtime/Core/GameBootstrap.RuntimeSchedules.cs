using UnityEngine;

public partial class GameBootstrap
{
    private enum BuildingWorkScheduleKind
    {
        None,
        ProductionDay,
        OfficeDay,
        ServiceDayEvening,
        ServiceEveningNight
    }

    private static BuildingWorkScheduleKind GetBuildingWorkScheduleKind(LocationType buildingType)
    {
        if (DoesBuildingRequireHigherEducation(buildingType))
        {
            return BuildingWorkScheduleKind.OfficeDay;
        }

        if (IsProductionLocation(buildingType))
        {
            return BuildingWorkScheduleKind.ProductionDay;
        }

        if (buildingType == LocationType.Bar ||
            buildingType == LocationType.GamblingHall)
        {
            return BuildingWorkScheduleKind.ServiceEveningNight;
        }

        return HasServiceWorkerSlot(buildingType)
            ? BuildingWorkScheduleKind.ServiceDayEvening
            : BuildingWorkScheduleKind.None;
    }

    private static int GetBuildingWorkerScheduleSlotCount(LocationType buildingType)
    {
        return GetBuildingWorkScheduleKind(buildingType) switch
        {
            BuildingWorkScheduleKind.OfficeDay => 1,
            BuildingWorkScheduleKind.ProductionDay => buildingType switch
            {
                LocationType.Warehouse => WarehouseMaxWorkers,
                LocationType.Docks => 1,
                _ => ProductionMaxWorkersPerBuilding
            },
            BuildingWorkScheduleKind.ServiceDayEvening or BuildingWorkScheduleKind.ServiceEveningNight => ServiceMaxWorkersPerBuilding,
            _ => 0
        };
    }

    private static bool DoesBuildingScheduleUseWeekends(BuildingWorkScheduleKind scheduleKind)
    {
        return scheduleKind == BuildingWorkScheduleKind.ServiceDayEvening ||
               scheduleKind == BuildingWorkScheduleKind.ServiceEveningNight;
    }

    private static bool IsDayWorkSchedule(BuildingWorkScheduleKind scheduleKind)
    {
        return scheduleKind == BuildingWorkScheduleKind.ProductionDay ||
               scheduleKind == BuildingWorkScheduleKind.OfficeDay;
    }
}
