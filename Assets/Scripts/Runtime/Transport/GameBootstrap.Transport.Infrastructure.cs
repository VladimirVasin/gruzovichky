using UnityEngine;

public partial class GameBootstrap
{
    private int GetTruckParkingCapacity()
    {
        return locations.ContainsKey(LocationType.Parking) ? MaxTruckCount : 0;
    }

    private int GetBusParkingCapacity()
    {
        return locations.ContainsKey(LocationType.Parking) ? MaxBusCount : 0;
    }

    private bool CanProvisionTruckFromParkingCapacity()
    {
        return GetOwnedTruckCount() < GetTruckParkingCapacity();
    }

    private bool CanProvisionBusFromParkingCapacity()
    {
        return GetOwnedBusCount() < GetBusParkingCapacity();
    }

    private int GetNextTruckNumber()
    {
        int max = 0;
        for (int i = 0; i < truckAgents.Count; i++)
        {
            if (truckAgents[i] != null)
            {
                max = Mathf.Max(max, truckAgents[i].TruckNumber);
            }
        }

        return max + 1;
    }

    private int GetNextBusNumber()
    {
        int max = 0;
        for (int i = 0; i < busAgents.Count; i++)
        {
            if (busAgents[i] != null)
            {
                max = Mathf.Max(max, busAgents[i].BusNumber);
            }
        }

        return max + 1;
    }

    private int GetFirstFreeTruckParkingSlotIndex()
    {
        int capacity = GetTruckParkingCapacity();
        for (int slot = 0; slot < capacity; slot++)
        {
            bool used = false;
            for (int i = 0; i < truckAgents.Count; i++)
            {
                if (truckAgents[i] != null && truckAgents[i].ParkingSlotIndex == slot)
                {
                    used = true;
                    break;
                }
            }

            if (!used)
            {
                return slot;
            }
        }

        return -1;
    }

    private int GetFirstFreeBusParkingSlotIndex()
    {
        int capacity = GetBusParkingCapacity();
        for (int slot = 0; slot < capacity; slot++)
        {
            bool used = false;
            for (int i = 0; i < busAgents.Count; i++)
            {
                if (busAgents[i] != null && busAgents[i].ParkingSlotIndex == slot)
                {
                    used = true;
                    break;
                }
            }

            if (!used)
            {
                return slot;
            }
        }

        return -1;
    }

    private bool TryProvisionTruckFromParkingCapacity(out TruckAgent truckAgent, string reason)
    {
        truckAgent = null;
        if (!CanProvisionTruckFromParkingCapacity())
        {
            return false;
        }

        int slotIndex = GetFirstFreeTruckParkingSlotIndex();
        if (slotIndex < 0)
        {
            return false;
        }

        truckAgent = CreateAndRegisterTruckAgent(GetNextTruckNumber(), slotIndex);
        SetupTruckAudio(truckAgent);
        SessionDebugLogger.Log(
            "TRUCK_POOL",
            $"Provisioned {truckAgent.DisplayName} from Parking infrastructure slot {slotIndex + 1} for {reason}.");
        return true;
    }

    private bool TryProvisionBusFromParkingCapacity(out BusAgent busAgent, string reason)
    {
        busAgent = null;
        if (!CanProvisionBusFromParkingCapacity())
        {
            return false;
        }

        int slotIndex = GetFirstFreeBusParkingSlotIndex();
        if (slotIndex < 0)
        {
            return false;
        }

        busAgent = CreateAndRegisterBusAgent(GetNextBusNumber(), slotIndex);
        SessionDebugLogger.Log(
            "BUS_POOL",
            $"Provisioned {busAgent.DisplayName} from Parking infrastructure slot {slotIndex + 1} for {reason}.");
        return true;
    }

    private bool EnsureBusProvisionedForAssignment(string reason)
    {
        return GetOwnedBusCount() > 0 || TryProvisionBusFromParkingCapacity(out _, reason);
    }
}
