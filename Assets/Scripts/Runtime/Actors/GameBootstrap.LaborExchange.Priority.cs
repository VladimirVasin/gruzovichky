public partial class GameBootstrap
{
    private bool TryMakeRoomForPriorityLaborExchangePosting(LaborExchangeCandidate candidate)
    {
        if (!IsPriorityTransportCandidate(candidate))
        {
            return false;
        }

        int removeIndex = -1;
        int removePriority = candidate.Priority;
        for (int i = 0; i < laborExchangePostings.Count; i++)
        {
            LaborExchangePosting posting = laborExchangePostings[i];
            if (posting == null || posting.ReservedWorkerId > 0)
            {
                continue;
            }

            int priority = GetLaborExchangePostingPriority(posting);
            if (priority > removePriority)
            {
                removePriority = priority;
                removeIndex = i;
            }
        }

        if (removeIndex < 0)
        {
            return false;
        }

        LaborExchangePosting removed = laborExchangePostings[removeIndex];
        laborExchangePostings.RemoveAt(removeIndex);
        SessionDebugLogger.Log(
            "LABOR_EXCHANGE",
            $"Priority vacancy made room: removed #{removed.Id} {GetLaborExchangePostingLabel(removed)} (priority {removePriority}) for {candidate.Kind} shift {candidate.ShiftIndex + 1} (priority {candidate.Priority}); active={laborExchangePostings.Count}/{LaborExchangeMaxActivePostings}.");
        return true;
    }

    private bool IsPriorityTransportCandidate(LaborExchangeCandidate candidate)
    {
        return (candidate.Kind == VacancyKind.TruckDriver || candidate.Kind == VacancyKind.BusDriver) &&
               candidate.Priority <= 14;
    }

    private int GetLaborExchangePostingPriority(LaborExchangePosting posting)
    {
        return posting.Kind switch
        {
            VacancyKind.TruckDriver => GetLaborExchangeTruckDriverPriority(),
            VacancyKind.BusDriver => GetLaborExchangeBusDriverPriority(),
            VacancyKind.Production when posting.BuildingType == LocationType.Warehouse => 15,
            VacancyKind.Production => 20,
            VacancyKind.Service when posting.BuildingType == LocationType.LaborExchange => 12,
            VacancyKind.Service => 40,
            _ => 100
        };
    }

    private int GetLaborExchangeTruckDriverPriority()
    {
        if (HasFreightLogisticsPressure())
        {
            return HasAnyAssignedTruckDriver() ? 18 : 8;
        }

        return 30;
    }

    private int GetLaborExchangeBusDriverPriority()
    {
        if (HasLocalBusLogisticsPressure())
        {
            return HasAnyAssignedBusDriver() ? 35 : 14;
        }

        return 50;
    }

    private bool HasFreightLogisticsPressure()
    {
        if (!locations.ContainsKey(LocationType.Parking) || !HasAvailableTruckInParking())
        {
            return false;
        }

        if (HasPendingDocksCargoForTruck() || HasActiveTradePolicyWithBuiltRoute())
        {
            return true;
        }

        return locations.ContainsKey(LocationType.Warehouse) &&
               (locations.ContainsKey(LocationType.Forest) ||
                locations.ContainsKey(LocationType.Sawmill) ||
                locations.ContainsKey(LocationType.FurnitureFactory) ||
                locations.ContainsKey(LocationType.Docks));
    }

    private bool HasLocalBusLogisticsPressure()
    {
        return HasAvailableBusInParking() &&
               HasWorkingLocalBusStopNetwork();
    }

    private bool HasAnyAssignedTruckDriver()
    {
        for (int i = 0; i < ShiftPresetHours.Length; i++)
        {
            if (IsAnyTruckDriverAssignedToShift(i))
            {
                return true;
            }
        }

        return false;
    }

    private bool HasAnyAssignedBusDriver()
    {
        for (int i = 0; i < ShiftPresetHours.Length; i++)
        {
            if (GetBusAssignedDriver(i) != null)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasPendingDocksCargoForTruck()
    {
        if (!locations.TryGetValue(LocationType.Docks, out LocationData docks))
        {
            return false;
        }

        for (int i = 0; i < TradeHudResources.Length; i++)
        {
            TradeResourceType resourceType = TradeHudResources[i];
            if (GetDocksImportStoredResource(docks, resourceType) > 0 ||
                GetDocksExportStoredResource(docks, resourceType) > 0)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasActiveTradePolicyWithBuiltRoute()
    {
        for (int i = 0; i < TradeHudResources.Length; i++)
        {
            TradeResourceType resourceType = TradeHudResources[i];
            TradePolicyMode mode = GetTradePolicyMode(resourceType);
            if (mode == TradePolicyMode.None)
            {
                continue;
            }

            TradeOrderType orderType = mode == TradePolicyMode.SellAbove ? TradeOrderType.Sell : TradeOrderType.Buy;
            if (HasBuiltRegionalTradeRoute(resourceType, orderType))
            {
                return true;
            }
        }

        return false;
    }
}
