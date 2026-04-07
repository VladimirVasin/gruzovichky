using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class GameBootstrap
{
    private string GetTruckFleetStatusLabel()
    {
        if (isTruckInteracting)
        {
            return "Busy";
        }

        if (isTruckWaitingForService)
        {
            return "Queue";
        }

        if (currentDriverRestPhase != DriverRestPhase.None)
        {
            return "Resting";
        }

        if (isDriverRescueActive)
        {
            return "Rescue";
        }

        if (isTruckMoving)
        {
            return "Moving";
        }

        if (currentRefuelPhase != RefuelPhase.None)
        {
            return "Refuel";
        }

        if (currentAssignedTrip != TripType.None)
        {
            return "Assigned";
        }

        return IsTruckInsideParking() ? "Parked" : "Idle";
    }

    private List<TripOption> GetAvailableTrips()
    {
        List<TripOption> trips = new();

        bool canReachForestTrip =
            HasPath(locations[LocationType.Parking].Anchor, locations[LocationType.Forest].Anchor) &&
            HasPath(locations[LocationType.Forest].Anchor, locations[LocationType.Warehouse].Anchor);
        if (locations[LocationType.Forest].WoodStored > 0 && canReachForestTrip)
        {
            trips.Add(new TripOption
            {
                Type = TripType.ForestToWarehouse,
                Title = "Deliver Logs: Forest -> Warehouse",
                Description = "Pick up logs in Forest and deliver them to Warehouse.",
                Reward = GetTripReward(TripType.ForestToWarehouse)
            });
        }

        bool canReachTownTrip =
            HasPath(locations[LocationType.Parking].Anchor, locations[LocationType.Warehouse].Anchor) &&
            HasPath(locations[LocationType.Warehouse].Anchor, locations[LocationType.Town].Anchor);
        if (locations[LocationType.Warehouse].WoodStored > 0 && canReachTownTrip)
        {
            trips.Add(new TripOption
            {
                Type = TripType.WarehouseToTown,
                Title = "Deliver Logs: Warehouse -> Town",
                Description = "Take stored logs from Warehouse to Town.",
                Reward = GetTripReward(TripType.WarehouseToTown)
            });
        }

        return trips;
    }

    private void AssignTrip(TripOption trip)
    {
        if (trip == null || trip.Type == TripType.None || currentAssignedTrip != TripType.None || currentRefuelPhase != RefuelPhase.None)
        {
            return;
        }

        currentAssignedTrip = trip.Type;
        currentTripPhase = TripPhase.ToPickup;
        currentAssignedTripReward = trip.Reward;
        PlayAssignedTripCue(trip.Type, 0.92f);
    }

    private void StartRefuelOrder()
    {
        if (currentAssignedTrip != TripType.None || currentRefuelPhase != RefuelPhase.None)
        {
            return;
        }

        currentRefuelPhase = RefuelPhase.ToGasStation;
        PlayUiSound(routeAssignRefuelClip, 0.94f);
    }

    private string GetTripTitle(TripType tripType)
    {
        return tripType switch
        {
            TripType.ForestToWarehouse => "Forest -> Warehouse",
            TripType.WarehouseToTown => "Warehouse -> Town",
            _ => "None"
        };
    }

    private bool IsPointerOverHud(Vector2 screenPosition)
    {
        Vector2 guiPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);

        if (GetMoneyHudRect().Contains(guiPosition) || GetTimeHudRect().Contains(guiPosition) || GetSpeedHudRect().Contains(guiPosition) || GetTruckFleetHudRect().Contains(guiPosition) || GetCameraLegendHudRect().Contains(guiPosition))
        {
            return true;
        }

        if (selectedLocation == LocationType.Parking && GetParkingHudRect().Contains(guiPosition))
        {
            return true;
        }

        if (selectedLocation.HasValue && selectedLocation != LocationType.Parking && GetSelectedBuildingHudRect().Contains(guiPosition))
        {
            return true;
        }

        if (selectedLocation == LocationType.Parking && GetAvailableTripsHudRect().Contains(guiPosition))
        {
            return true;
        }

        return isTruckDetailsOpen && GetTruckDetailsHudRect().Contains(guiPosition);
    }

    private Rect GetParkingHudRect()
    {
        return new Rect(Screen.width - 290, 12, 278, 286);
    }

    private Rect GetTruckDetailsHudRect()
    {
        return new Rect(Screen.width - 290, 488, 278, 388);
    }

    private Rect GetAvailableTripsHudRect()
    {
        return new Rect(Screen.width - 290, 308, 278, 170);
    }

    private Rect GetMoneyHudRect()
    {
        return new Rect(Screen.width * 0.5f - 90f, 12f, 180f, 54f);
    }

    private Rect GetTimeHudRect()
    {
        return new Rect(Screen.width * 0.5f + 100f, 12f, 190f, 54f);
    }

    private Rect GetSpeedHudRect()
    {
        return new Rect(Screen.width * 0.5f + 300f, 12f, 120f, 54f);
    }

    private Rect GetTruckFleetHudRect()
    {
        return new Rect(12f, 12f, 220f, 324f);
    }

    private Rect GetCameraLegendHudRect()
    {
        return new Rect(Screen.width * 0.5f - 160f, Screen.height - 78f, 320f, 72f);
    }

    private Rect GetSelectedBuildingHudRect()
    {
        return new Rect(Screen.width - 290, 12, 278, 96);
    }

    private void UpdateMoneyPopup()
    {
        if (moneyPopupTimer <= 0f)
        {
            return;
        }

        moneyPopupTimer = Mathf.Max(0f, moneyPopupTimer - Time.deltaTime);
        if (moneyPopupTimer <= 0f)
        {
            moneyPopupAmount = 0;
        }
    }

    private void AwardMoney(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        money += amount;
        moneyPopupAmount = amount;
        moneyPopupTimer = MoneyPopupDuration;
        PlayUiSound(moneyRewardClip, 0.95f);
    }

    private void PlayAssignedTripCue(TripType tripType, float volumeScale = 0.94f)
    {
        AudioClip clip = tripType switch
        {
            TripType.ForestToWarehouse => routeAssignForestWarehouseClip,
            TripType.WarehouseToTown => routeAssignWarehouseTownClip,
            _ => null
        };

        PlayUiSound(clip, volumeScale);
    }

}

