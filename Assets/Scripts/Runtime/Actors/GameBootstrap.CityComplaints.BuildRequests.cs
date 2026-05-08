using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private sealed class CityConstructionRequestDefinition
    {
        public readonly BuildTool Tool;
        public readonly LocationType Target;
        public readonly WorkerNeedKind? LinkedNeed;
        public readonly int Severity;
        public readonly int Weight;
        public readonly int RequiredLocationCount;

        public CityConstructionRequestDefinition(
            BuildTool tool,
            LocationType target,
            WorkerNeedKind? linkedNeed,
            int severity,
            int weight,
            int requiredLocationCount = 1)
        {
            Tool = tool;
            Target = target;
            LinkedNeed = linkedNeed;
            Severity = severity;
            Weight = weight;
            RequiredLocationCount = requiredLocationCount;
        }
    }

    private static readonly CityConstructionRequestDefinition[] CityCoreConstructionRequests =
    {
        new(BuildTool.Warehouse, LocationType.Warehouse, null, 4, 10),
        new(BuildTool.Motel, LocationType.Motel, WorkerNeedKind.Sleep, 4, 9),
        new(BuildTool.CityHall, LocationType.CityHall, null, 4, 8)
    };

    private static readonly CityConstructionRequestDefinition[] CitySecondLayerConstructionRequests =
    {
        new(BuildTool.Parking, LocationType.Parking, null, 4, 10),
        new(BuildTool.LaborExchange, LocationType.LaborExchange, null, 4, 9),
        new(BuildTool.Canteen, LocationType.Canteen, WorkerNeedKind.Meal, 3, 8),
        new(BuildTool.Forest, LocationType.Forest, null, 3, 7),
        new(BuildTool.GasStation, LocationType.GasStation, null, 2, 6),
        new(BuildTool.Sawmill, LocationType.Sawmill, null, 3, 5),
        new(BuildTool.Bar, LocationType.Bar, WorkerNeedKind.Leisure, 2, 4)
    };

    private static readonly CityConstructionRequestDefinition[] CityThirdLayerConstructionRequests =
    {
        new(BuildTool.Stop, LocationType.Stop, null, 3, 10, 2),
        new(BuildTool.Docks, LocationType.Docks, null, 3, 9),
        new(BuildTool.FurnitureFactory, LocationType.FurnitureFactory, null, 3, 8),
        new(BuildTool.Kiosk, LocationType.Kiosk, WorkerNeedKind.Meal, 2, 7),
        new(BuildTool.GamblingHall, LocationType.GamblingHall, WorkerNeedKind.Leisure, 2, 6),
        new(BuildTool.CityPark, LocationType.CityPark, WorkerNeedKind.Leisure, 2, 6),
        new(BuildTool.PersonalHouse, LocationType.PersonalHouse, null, 2, 5),
        new(BuildTool.Kindergarten, LocationType.Kindergarten, null, 2, 5),
        new(BuildTool.CarMarket, LocationType.CarMarket, null, 2, 4)
    };

    private List<CityServiceRequestCandidate> BuildCityConstructionRequestCandidates()
    {
        List<CityServiceRequestCandidate> candidates = new();
        AddCityConstructionRequestLayer(candidates, CityCoreConstructionRequests);
        if (HasUnbuiltUnlockedCityConstructionTarget(CityCoreConstructionRequests))
        {
            return candidates;
        }

        AddCityConstructionRequestLayer(candidates, CitySecondLayerConstructionRequests);
        if (HasUnbuiltUnlockedCityConstructionTarget(CitySecondLayerConstructionRequests))
        {
            return candidates;
        }

        AddCityConstructionRequestLayer(candidates, CityThirdLayerConstructionRequests);
        return candidates;
    }

    private void AddCityConstructionRequestLayer(
        List<CityServiceRequestCandidate> candidates,
        CityConstructionRequestDefinition[] definitions)
    {
        if (candidates == null || definitions == null)
        {
            return;
        }

        for (int i = 0; i < definitions.Length; i++)
        {
            AddCityConstructionRequestCandidate(candidates, definitions[i]);
        }
    }

    private void AddCityConstructionRequestCandidate(
        List<CityServiceRequestCandidate> candidates,
        CityConstructionRequestDefinition definition)
    {
        if (candidates == null || definition == null || !IsBuildToolUnlocked(definition.Tool))
        {
            return;
        }

        int requiredLocationCount = Mathf.Max(1, definition.RequiredLocationCount);
        if (GetBuiltLocationCount(definition.Target) >= requiredLocationCount)
        {
            return;
        }

        string groupKey = GetCityComplaintGroupKey(
            CityComplaintCategory.ServiceMissing,
            definition.LinkedNeed,
            definition.Target);
        if (FindActiveCityComplaintByGroupKey(groupKey) != null)
        {
            return;
        }

        float now = GetCurrentWorldHour();
        if (cityComplaintCooldownByKey.TryGetValue(groupKey, out float nextAllowedWorldHour) &&
            now < nextAllowedWorldHour)
        {
            return;
        }

        candidates.Add(new CityServiceRequestCandidate
        {
            Target = definition.Target,
            LinkedNeed = definition.LinkedNeed,
            Severity = Mathf.Clamp(definition.Severity, 1, 4),
            Weight = Mathf.Max(1, definition.Weight),
            RequiredLocationCount = requiredLocationCount
        });
    }

    private bool HasUnbuiltUnlockedCityConstructionTarget(CityConstructionRequestDefinition[] definitions)
    {
        if (definitions == null)
        {
            return false;
        }

        for (int i = 0; i < definitions.Length; i++)
        {
            CityConstructionRequestDefinition definition = definitions[i];
            if (definition == null || !IsBuildToolUnlocked(definition.Tool))
            {
                continue;
            }

            int requiredLocationCount = Mathf.Max(1, definition.RequiredLocationCount);
            if (GetBuiltLocationCount(definition.Target) < requiredLocationCount)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsCityConstructionRequestSatisfied(CityComplaint complaint)
    {
        if (complaint == null || !complaint.LinkedLocationType.HasValue)
        {
            return false;
        }

        int requiredLocationCount = Mathf.Max(1, complaint.RequiredLocationCount);
        return GetBuiltLocationCount(complaint.LinkedLocationType.Value) >= requiredLocationCount;
    }

    private int GetBuiltLocationCount(LocationType target)
    {
        if (target == LocationType.Stop)
        {
            return localStops.Count;
        }

        if (target == LocationType.PersonalHouse)
        {
            return personalHouses.Count;
        }

        int count = locations.ContainsKey(target) ? 1 : 0;
        for (int i = 0; i < extraServiceLocations.Count; i++)
        {
            LocationData location = extraServiceLocations[i];
            if (location != null && location.Type == target)
            {
                count++;
            }
        }

        return count;
    }
}
