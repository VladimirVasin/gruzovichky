using UnityEngine;

public partial class GameBootstrap
{
    private void RecordWorkerBuildingKnowledge(DriverAgent worker, LocationType buildingType, string reasonRu, string reasonEn)
    {
        if (!locations.TryGetValue(buildingType, out LocationData location))
        {
            return;
        }

        RecordWorkerBuildingKnowledge(worker, location, reasonRu, reasonEn);
    }

    private void RecordWorkerBuildingKnowledge(DriverAgent worker, LocationData location, string reasonRu, string reasonEn)
    {
        if (worker == null ||
            location == null ||
            worker.HasDepartedTown ||
            worker.IsLeavingTown ||
            location.InstanceId <= 0)
        {
            return;
        }

        float now = GetCurrentWorldHour();
        PruneExpiredWorkerMemories(worker, now);

        WorkerMemory existing = FindWorkerBuildingKnowledgeMemory(worker, location.Type, location.InstanceId);
        if (existing != null)
        {
            return;
        }

        WorkerMemory memory = new()
        {
            Kind = WorkerMemoryKind.BuildingExistence,
            BuildingType = location.Type,
            BuildingInstanceId = location.InstanceId,
            BuildingLabel = GetWorkerKnowledgeBuildingDisplayName(location.Type, location.InstanceId, IsRussianLanguage()),
            SourceRu = reasonRu ?? string.Empty,
            SourceEn = reasonEn ?? string.Empty,
            Positive = true,
            CreatedDay = currentDay,
            CreatedWorldHour = now,
            ExpiresWorldHour = now + WorkerPersonalMemoryLifetimeHours
        };

        worker.Memories.Insert(0, memory);
        RecordNoosphereKnowledgeReceived(worker, null, memory, now);
        TrimWorkerMemories(worker, now);
        isDriversScreenDirty = true;
    }

    private void RecordCityHallKnowledgeForComplaintSigners(CityComplaint complaint, string reasonRu, string reasonEn)
    {
        if (complaint == null)
        {
            return;
        }

        for (int i = 0; i < complaint.SignerIds.Count; i++)
        {
            RecordWorkerBuildingKnowledge(GetDriverAgentById(complaint.SignerIds[i]), LocationType.CityHall, reasonRu, reasonEn);
        }
    }

    private LocationData GetLocalStopByNumber(int stopNumber)
    {
        if (stopNumber <= 0)
        {
            return null;
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            LocationData stop = localStops[i];
            if (stop != null && stop.StopNumber == stopNumber)
            {
                return stop;
            }
        }

        return null;
    }

    private DriverAgent GetLoadedTruckDriver()
    {
        for (int i = 0; i < truckAgents.Count; i++)
        {
            TruckAgent truckAgent = truckAgents[i];
            if (truckAgent != null && truckAgent.TruckObject == truckObject)
            {
                return truckAgent.Driver;
            }
        }

        return null;
    }

    private WorkerMemory FindWorkerBuildingKnowledgeMemory(DriverAgent worker, LocationType buildingType, int instanceId)
    {
        if (worker == null)
        {
            return null;
        }

        for (int i = 0; i < worker.Memories.Count; i++)
        {
            WorkerMemory memory = worker.Memories[i];
            if (memory != null &&
                memory.Kind == WorkerMemoryKind.BuildingExistence &&
                memory.BuildingType == buildingType &&
                memory.BuildingInstanceId == instanceId)
            {
                return memory;
            }
        }

        return null;
    }

    private static bool IsTimedWorkerMemory(WorkerMemory memory)
    {
        return memory != null &&
               (memory.Kind == WorkerMemoryKind.ConversationTopic ||
                memory.Kind == WorkerMemoryKind.BuildingExistence);
    }

    private static bool IsWorkerMemoryDisplayable(WorkerMemory memory)
    {
        if (memory == null)
        {
            return false;
        }

        return memory.Kind switch
        {
            WorkerMemoryKind.ConversationTopic => !string.IsNullOrWhiteSpace(memory.Topic),
            WorkerMemoryKind.BuildingExistence => memory.BuildingType.HasValue && memory.BuildingInstanceId > 0,
            _ => false
        };
    }

    private string GetWorkerKnowledgeBuildingDisplayName(WorkerMemory memory, bool ru)
    {
        if (memory == null || !memory.BuildingType.HasValue)
        {
            return ru ? "\u043f\u043e\u0441\u0442\u0440\u043e\u0439\u043a\u0430" : "building";
        }

        if (FindLocationByInstanceId(memory.BuildingInstanceId) == null &&
            !string.IsNullOrWhiteSpace(memory.BuildingLabel))
        {
            return memory.BuildingLabel;
        }

        return GetWorkerKnowledgeBuildingDisplayName(memory.BuildingType.Value, memory.BuildingInstanceId, ru);
    }

    private string GetWorkerKnowledgeBuildingDisplayName(LocationType buildingType, int instanceId, bool ru)
    {
        string baseName = GetWorkerKnowledgeBuildingTypeLabel(buildingType, ru);
        int ordinal = GetWorkerKnowledgeLocationOrdinal(buildingType, instanceId);
        if (ordinal <= 1 &&
            !IsMultiInstanceServiceBuildType(buildingType) &&
            buildingType != LocationType.PersonalHouse &&
            buildingType != LocationType.Stop)
        {
            return baseName;
        }

        return ordinal > 0 ? $"{baseName} #{ordinal}" : baseName;
    }

    private int GetWorkerKnowledgeLocationOrdinal(LocationType buildingType, int instanceId)
    {
        if (instanceId <= 0)
        {
            return 0;
        }

        if (buildingType == LocationType.Stop)
        {
            for (int i = 0; i < localStops.Count; i++)
            {
                if (localStops[i]?.InstanceId == instanceId)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        if (buildingType == LocationType.PersonalHouse)
        {
            for (int i = 0; i < personalHouses.Count; i++)
            {
                if (personalHouses[i]?.InstanceId == instanceId)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        return GetLocationInstanceOrdinal(buildingType, ResolveBuildingInstanceId(buildingType, instanceId));
    }

    private static string GetWorkerKnowledgeBuildingTypeLabel(LocationType buildingType, bool ru)
    {
        if (!ru)
        {
            return buildingType switch
            {
                LocationType.Parking => "Parking",
                LocationType.GasStation => "Fuel Stop",
                LocationType.Forest => "Lumberyard",
                LocationType.Warehouse => "Warehouse",
                LocationType.Sawmill => "Sawmill",
                LocationType.FurnitureFactory => "Furniture Factory",
                LocationType.Motel => "Motel",
                LocationType.IntercityStop => "Intercity Stop",
                LocationType.Stop => "Bus Stop",
                LocationType.Bar => "Bar",
                LocationType.Canteen => "Canteen",
                LocationType.Kiosk => "Kiosk",
                LocationType.GamblingHall => "Gambling Hall",
                LocationType.CityPark => "City Park",
                LocationType.PersonalHouse => "Personal House",
                LocationType.Kindergarten => "Kindergarten",
                LocationType.CarMarket => "Car Market",
                LocationType.LaborExchange => "Labor Exchange",
                LocationType.CityHall => "City Hall",
                LocationType.Docks => "Docks",
                _ => "Building"
            };
        }

        return buildingType switch
        {
            LocationType.Parking => "\u041f\u0430\u0440\u043a\u043e\u0432\u043a\u0430",
            LocationType.GasStation => "\u0417\u0430\u043f\u0440\u0430\u0432\u043a\u0430",
            LocationType.Forest => "\u041b\u0435\u0441\u043e\u0437\u0430\u0433\u043e\u0442\u043e\u0432\u043a\u0430",
            LocationType.Warehouse => "\u0421\u043a\u043b\u0430\u0434",
            LocationType.Sawmill => "\u041b\u0435\u0441\u043e\u043f\u0438\u043b\u043a\u0430",
            LocationType.FurnitureFactory => "\u041c\u0435\u0431\u0435\u043b\u044c\u043d\u0430\u044f \u0444\u0430\u0431\u0440\u0438\u043a\u0430",
            LocationType.Motel => "\u041c\u043e\u0442\u0435\u043b\u044c",
            LocationType.IntercityStop => "\u041c\u0435\u0436\u0434\u0443\u0433\u043e\u0440\u043e\u0434\u043d\u044f\u044f \u043e\u0441\u0442\u0430\u043d\u043e\u0432\u043a\u0430",
            LocationType.Stop => "\u0410\u0432\u0442\u043e\u0431\u0443\u0441\u043d\u0430\u044f \u043e\u0441\u0442\u0430\u043d\u043e\u0432\u043a\u0430",
            LocationType.Bar => "\u0411\u0430\u0440",
            LocationType.Canteen => "\u0421\u0442\u043e\u043b\u043e\u0432\u0430\u044f",
            LocationType.Kiosk => "\u041a\u0438\u043e\u0441\u043a",
            LocationType.GamblingHall => "\u0418\u0433\u0440\u043e\u0432\u043e\u0439 \u0437\u0430\u043b",
            LocationType.CityPark => "\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u043e\u0439 \u043f\u0430\u0440\u043a",
            LocationType.PersonalHouse => "\u0416\u0438\u043b\u043e\u0439 \u0434\u043e\u043c",
            LocationType.Kindergarten => "\u0414\u0435\u0442\u0441\u043a\u0438\u0439 \u0441\u0430\u0434",
            LocationType.CarMarket => "\u0410\u0432\u0442\u043e\u0440\u044b\u043d\u043e\u043a",
            LocationType.LaborExchange => "\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430",
            LocationType.CityHall => "\u0420\u0430\u0442\u0443\u0448\u0430",
            LocationType.Docks => "\u0414\u043e\u043a\u0438",
            _ => "\u041f\u043e\u0441\u0442\u0440\u043e\u0439\u043a\u0430"
        };
    }
}
