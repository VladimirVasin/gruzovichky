using UnityEngine;

public partial class GameBootstrap
{
    private void HandleLocalBusPassengersAtStop(LocationData stop)
    {
        if (localBusRoute == null || stop == null)
        {
            return;
        }

        int stopNumber = stop.StopNumber;
        Vector3 stopWaitPoint = GetLocalStopPassengerWaitPoint(stop);

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent passenger = driverAgents[i];
            if (passenger == null ||
                passenger.WalkPhase != DriverRescuePhase.RidingLocalBus ||
                passenger.BusDestinationStopNumber != stopNumber)
            {
                continue;
            }

            passenger.DriverObject.SetActive(true);
            passenger.DriverObject.transform.position = stopWaitPoint;
            passenger.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            passenger.WalkAnimationTime = 0f;
            ApplyDriverPose(passenger, 0f, 0f);

            DriverRescuePhase finalWalkPhase = passenger.BusFinalWalkPhase;
            Vector3 finalTarget = passenger.BusFinalTargetWorld;
            string travelReason = passenger.BusTravelReason;
            ResetWorkerLocalBusTripState(passenger);

            passenger.WalkPhase = finalWalkPhase;
            passenger.WalkTargetWorld = finalTarget;
            if (!BuildDriverWalkPath(passenger, stopWaitPoint, finalTarget))
            {
                HandleFailedLocalBusFinalWalk(passenger, finalWalkPhase, travelReason);
            }

            localBusRoute.PassengerCount = CountActualLocalBusPassengers();
            SyncLocalBusAgentState();
            SessionDebugLogger.Log(
                "BUS_PASSENGER",
                $"{passenger.DriverName} left the local bus at Stop #{stopNumber} and resumed {travelReason}. Passengers={localBusRoute.PassengerCount}/{localBusRoute.PassengerCapacity}.");
        }

        localBusRoute.PassengerCount = CountActualLocalBusPassengers();
        SyncLocalBusAgentState();
        if (localBusRoute.Driver != null && localBusRoute.Driver.NeedsShiftEndReturn)
        {
            SessionDebugLogger.Log("BUS_SHIFT", $"{localBusRoute.Driver.DriverName} is ending the bus shift; boarding is closed at Stop #{stopNumber}.");
            return;
        }

        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent passenger = driverAgents[i];
            if (passenger == null ||
                passenger.WalkPhase != DriverRescuePhase.WaitingAtLocalBusStop ||
                passenger.BusOriginStopNumber != stopNumber ||
                passenger.BusDestinationStopNumber <= 0)
            {
                continue;
            }

            bool fareExempt = passenger.BusRideFareExempt;
            LocalBusPassengerBoardingDecision boardingDecision = LocalBusPassengerService.EvaluateBoarding(
                localBusRoute.PassengerCount,
                localBusRoute.PassengerCapacity,
                passenger.Money,
                LocalBusFare,
                fareExempt);

            if (boardingDecision.Kind == LocalBusPassengerBoardingDecisionKind.ContinueWaiting)
            {
                SessionDebugLogger.Log(
                    "BUS_PASSENGER",
                    $"{passenger.DriverName} could not board at Stop #{stopNumber}: local bus is full ({localBusRoute.PassengerCount}/{localBusRoute.PassengerCapacity}).");
                continue;
            }

            if (boardingDecision.Kind == LocalBusPassengerBoardingDecisionKind.FallBackToWalking)
            {
                DriverRescuePhase finalWalkPhase = passenger.BusFinalWalkPhase;
                Vector3 finalTarget = passenger.BusFinalTargetWorld;
                string travelReason = passenger.BusTravelReason;
                ResetWorkerLocalBusTripState(passenger);
                passenger.WalkPhase = finalWalkPhase;
                passenger.WalkTargetWorld = finalTarget;
                passenger.DriverObject.SetActive(true);
                passenger.DriverObject.transform.position = stopWaitPoint;
                passenger.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                passenger.WalkAnimationTime = 0f;
                ApplyDriverPose(passenger, 0f, 0f);
                if (!BuildDriverWalkPath(passenger, stopWaitPoint, finalTarget))
                {
                    HandleFailedLocalBusFinalWalk(passenger, finalWalkPhase, travelReason);
                }

                SessionDebugLogger.Log(
                    "BUS_PASSENGER",
                    $"{passenger.DriverName} could not pay the ${LocalBusFare} fare at Stop #{stopNumber}; resumed {travelReason} on foot. Balance=${passenger.Money}.");
                continue;
            }

            if (boardingDecision.FareCharged > 0)
            {
                passenger.Money = Mathf.Max(0, passenger.Money - boardingDecision.FareCharged);
                localBusRoute.Bank += boardingDecision.FareCharged;
                RecordMoneyMovement(
                    0,
                    passenger.DriverName,
                    "Local Bus Bank",
                    $"Local bus fare: Stop #{stopNumber} -> Stop #{passenger.BusDestinationStopNumber}",
                    null,
                    localBusRoute.Bank,
                    MoneyAccountKind.ResidentWallet,
                    MoneyAccountKind.BuildingCash,
                    MoneyTransactionReasonKind.TransportFare,
                    fromOwnerId: passenger.DriverId);
                SyncLocalBusAgentState();
                SpawnMoneySpendPopup(stopWaitPoint, boardingDecision.FareCharged);
            }

            passenger.DriverObject.SetActive(false);
            passenger.WalkPhase = DriverRescuePhase.RidingLocalBus;
            passenger.WalkPath.Clear();
            passenger.WalkWaypointIndex = 0;
            passenger.WalkAnimationTime = 0f;
            localBusRoute.PassengerCount = CountActualLocalBusPassengers();
            SyncLocalBusAgentState();
            SessionDebugLogger.Log(
                "BUS_PASSENGER",
                $"{passenger.DriverName} boarded the local bus at Stop #{stopNumber} for Stop #{passenger.BusDestinationStopNumber}; fare={(fareExempt ? "service pass" : $"${LocalBusFare}")}, passengerBalance=${passenger.Money}, busBank=${localBusRoute.Bank}. Passengers={localBusRoute.PassengerCount}/{localBusRoute.PassengerCapacity}.");
        }
    }

    private void ReleaseLocalBusPassengersAtCurrentStop(LocationData stop, string reason)
    {
        if (localBusRoute == null || stop == null)
        {
            return;
        }

        int released = 0;
        int stopNumber = stop.StopNumber;
        Vector3 stopWaitPoint = GetLocalStopPassengerWaitPoint(stop);
        for (int i = 0; i < driverAgents.Count; i++)
        {
            DriverAgent passenger = driverAgents[i];
            if (passenger == null || passenger.WalkPhase != DriverRescuePhase.RidingLocalBus)
            {
                continue;
            }

            passenger.DriverObject.SetActive(true);
            passenger.DriverObject.transform.position = stopWaitPoint;
            passenger.DriverObject.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            passenger.WalkAnimationTime = 0f;
            ApplyDriverPose(passenger, 0f, 0f);

            DriverRescuePhase finalWalkPhase = passenger.BusFinalWalkPhase;
            Vector3 finalTarget = passenger.BusFinalTargetWorld;
            string travelReason = passenger.BusTravelReason;
            ResetWorkerLocalBusTripState(passenger);

            passenger.WalkPhase = finalWalkPhase;
            passenger.WalkTargetWorld = finalTarget;
            if (!BuildDriverWalkPath(passenger, stopWaitPoint, finalTarget))
            {
                HandleFailedLocalBusFinalWalk(passenger, finalWalkPhase, travelReason);
            }

            released++;
            SessionDebugLogger.Log(
                "BUS_PASSENGER",
                $"{passenger.DriverName} left the local bus early at Stop #{stopNumber}: {reason}; resumed {travelReason} on foot.");
        }

        localBusRoute.PassengerCount = CountActualLocalBusPassengers();
        SyncLocalBusAgentState();
        if (released > 0)
        {
            SessionDebugLogger.Log(
                "BUS_SHIFT",
                $"{localBusRoute.Driver?.DriverName ?? "Bus driver"} released {released} passenger(s) at Stop #{stopNumber} before returning to Parking.");
        }
    }

    private void HandleFailedLocalBusFinalWalk(DriverAgent passenger, DriverRescuePhase finalWalkPhase, string travelReason)
    {
        if (passenger == null)
        {
            return;
        }

        passenger.WalkPhase = DriverRescuePhase.None;
        passenger.WalkTargetWorld = passenger.DriverObject != null
            ? passenger.DriverObject.transform.position
            : Vector3.zero;

        if (finalWalkPhase == DriverRescuePhase.ToLaborExchangeForJob &&
            passenger.ReservedLaborExchangePostingId > 0)
        {
            LaborExchangePosting posting = FindLaborExchangePosting(passenger.ReservedLaborExchangePostingId);
            if (posting != null)
            {
                ReleaseLaborExchangePostingReservation(posting);
            }

            passenger.LifeGoal = WorkerLifeGoal.Idle;
        }
        else if (finalWalkPhase == DriverRescuePhase.ToPersonalHouseMeal)
        {
            passenger.LifeGoal = WorkerLifeGoal.None;
            SetWorkerNeedRetryCooldown(passenger, WorkerNeedKind.Meal, "no safe final walk path after local bus");
        }

        SessionDebugLogger.Log(
            "BUS_PASSENGER",
            $"{passenger.DriverName} could not resume {travelReason} after leaving the local bus: no safe final walk path for {finalWalkPhase}.");
    }
}
