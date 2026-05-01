using UnityEngine;

public partial class GameBootstrap
{
    private int GetOwnedBusCount()
    {
        return busAgents.Count;
    }

    private int GetParkingBusCount()
    {
        int count = 0;
        for (int i = 0; i < busAgents.Count; i++)
        {
            if (IsBusInsideParking(busAgents[i]))
            {
                count++;
            }
        }

        return count;
    }

    private BusAgent GetBusAgent(int busNumber)
    {
        for (int i = 0; i < busAgents.Count; i++)
        {
            if (busAgents[i].BusNumber == busNumber)
            {
                return busAgents[i];
            }
        }

        return null;
    }

    private Vector3 GetBusParkingSlotWorldPosition(int parkingSlotIndex)
    {
        if (!locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            return Vector3.zero;
        }

        Vector3 center = GetLocationCenter(LocationType.Parking);
        float halfWidth = Mathf.Max(0.35f, ((parking.Max.x - parking.Min.x + 1) * 0.5f) - 0.45f);
        float halfDepth = Mathf.Max(0.28f, ((parking.Max.y - parking.Min.y + 1) * 0.5f) - 0.38f);
        Vector3[] parkingSlots =
        {
            center + new Vector3(-halfWidth * 0.70f, 0f, halfDepth * 0.70f),
            center + new Vector3(0f, 0f, halfDepth * 0.70f),
            center + new Vector3(halfWidth * 0.70f, 0f, halfDepth * 0.70f)
        };

        int slotIndex = Mathf.Clamp(parkingSlotIndex, 0, parkingSlots.Length - 1);
        Vector3 position = parkingSlots[slotIndex];
        position.y = SampleTerrainHeight(position.x, position.z) + RoadHeight + EdgeHighwayBusLift;
        return position;
    }

    private bool IsBusInsideParking(BusAgent busAgent)
    {
        if (busAgent == null || busAgent.BusObject == null || !locations.TryGetValue(LocationType.Parking, out LocationData parking))
        {
            return false;
        }

        Vector2Int cell = WorldToCell(busAgent.BusObject.transform.position);
        return cell.x >= parking.Min.x &&
               cell.x <= parking.Max.x &&
               cell.y >= parking.Min.y &&
               cell.y <= parking.Max.y;
    }

    private bool IsBusOperationallyAvailable(BusAgent busAgent)
    {
        if (busAgent == null || busAgent.BusObject == null)
        {
            return false;
        }

        if (busAgent.Driver != null)
        {
            return false;
        }

        if (busAgent.IsPurchaseArrivalActive)
        {
            return false;
        }

        if (localBusRoute != null && localBusRoute.Bus == busAgent && localBusRoute.Phase != LocalBusPhase.None)
        {
            return false;
        }

        return IsBusInsideParking(busAgent);
    }

    private bool HasAvailableBusInParking()
    {
        for (int i = 0; i < busAgents.Count; i++)
        {
            if (IsBusOperationallyAvailable(busAgents[i]))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryReserveAvailableBusForDriver(DriverAgent driver, out BusAgent busAgent, string reason)
    {
        busAgent = null;
        if (driver == null)
        {
            return false;
        }

        for (int i = 0; i < busAgents.Count; i++)
        {
            BusAgent candidate = busAgents[i];
            if (!IsBusOperationallyAvailable(candidate))
            {
                continue;
            }

            candidate.Driver = driver;
            busAgent = candidate;
            SessionDebugLogger.Log("BUS_POOL", $"{driver.DriverName} reserved {candidate.DisplayName} automatically for {reason}.");
            return true;
        }

        SessionDebugLogger.Log("BUS_POOL", $"{driver.DriverName} could not reserve a bus for {reason}: no available parked buses.");
        return false;
    }

    private BusAgent CreateAndRegisterBusAgent(int busNumber, int parkingSlotIndex, bool spawnAtParking = true)
    {
        GameObject busRoot = new($"Bus_{busNumber}");
        busRoot.transform.SetParent(worldRoot, false);

        BuildSharedBusVisual(
            busRoot.transform,
            new Color(0.28f, 0.58f, 0.9f),
            $"Bus_{busNumber}_HeadlightLeft",
            $"Bus_{busNumber}_HeadlightRight",
            out Renderer headlightLeftRenderer,
            out Renderer headlightRightRenderer,
            out Material headlightLeftMaterial,
            out Material headlightRightMaterial,
            out Light headlightLeft,
            out Light headlightRight);

        BusAgent busAgent = new()
        {
            BusNumber = busNumber,
            DisplayName = $"Bus #{busNumber}",
            BusObject = busRoot,
            HeadlightLeftRenderer = headlightLeftRenderer,
            HeadlightRightRenderer = headlightRightRenderer,
            HeadlightLeftMaterial = headlightLeftMaterial,
            HeadlightRightMaterial = headlightRightMaterial,
            HeadlightLeft = headlightLeft,
            HeadlightRight = headlightRight,
            ParkingSlotIndex = parkingSlotIndex,
            PassengerCapacity = LocalBusMaxPassengers
        };

        busRoot.transform.position = spawnAtParking ? GetBusParkingSlotWorldPosition(parkingSlotIndex) : Vector3.zero;
        busRoot.transform.rotation = Quaternion.identity;
        busAgents.Add(busAgent);
        SessionDebugLogger.Log("BUS_POOL", $"Registered {busAgent.DisplayName} for Parking slot {parkingSlotIndex + 1}; spawnAtParking={(spawnAtParking ? "yes" : "no")}.");
        return busAgent;
    }

    private void HireNewBus()
    {
        LogUiInput("Vacancies: clicked Buy New Bus");
        LogCommand($"HireNewBus(cost=${HireBusCost})");
        if (!locations.ContainsKey(LocationType.Parking) || GetOwnedBusCount() >= MaxBusCount || money < HireBusCost)
        {
            SessionDebugLogger.Log("BUS_POOL", $"Hire new bus rejected: {GetBusBuyStatusLabel()}");
            return;
        }

        BusAgent hiredBus = CreateAndRegisterBusAgent(nextHireBusNumber, busAgents.Count, spawnAtParking: false);
        nextHireBusNumber++;
        money -= HireBusCost;
        RecordMoneyMovement(-HireBusCost, "Treasury", "Fleet Expansion", $"Hire {hiredBus.DisplayName}", money);
        SessionDebugLogger.Log("BUS_POOL", $"Hired {hiredBus.DisplayName} for ${HireBusCost}. Money now ${money}.");
        StartPurchasedBusArrival(hiredBus);
        isShiftsScreenDirty = true;
        PlayUiSound(uiSelectClip, 1f);
    }

    private string GetBusBuyStatusLabel()
    {
        bool ru = IsRussianLanguage();
        if (!locations.ContainsKey(LocationType.Parking))
        {
            return ru ? "\u0421\u043d\u0430\u0447\u0430\u043b\u0430 \u043f\u043e\u0441\u0442\u0440\u043e\u0439 Parking." : "Build Parking first.";
        }

        if (GetOwnedBusCount() >= MaxBusCount)
        {
            return ru ? "\u041f\u0430\u0440\u043a \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u043e\u0432 \u0437\u0430\u043f\u043e\u043b\u043d\u0435\u043d." : "Bus parking is full.";
        }

        if (money < HireBusCost)
        {
            return ru ? $"\u041d\u0443\u0436\u043d\u043e ${HireBusCost} \u043d\u0430 \u0430\u0432\u0442\u043e\u0431\u0443\u0441." : $"Need ${HireBusCost} to buy a bus.";
        }

        return ru ? "\u041c\u043e\u0436\u043d\u043e \u043a\u0443\u043f\u0438\u0442\u044c \u0435\u0449\u0451 \u0430\u0432\u0442\u043e\u0431\u0443\u0441." : "Ready to buy another bus.";
    }
}
