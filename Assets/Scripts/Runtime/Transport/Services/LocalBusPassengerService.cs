public enum LocalBusPassengerBoardingDecisionKind
{
    Board,
    ContinueWaiting,
    FallBackToWalking
}

public readonly struct LocalBusPassengerBoardingDecision
{
    public LocalBusPassengerBoardingDecision(LocalBusPassengerBoardingDecisionKind kind, int fareCharged)
    {
        Kind = kind;
        FareCharged = fareCharged;
    }

    public LocalBusPassengerBoardingDecisionKind Kind { get; }
    public int FareCharged { get; }
}

public static class LocalBusPassengerService
{
    public static LocalBusPassengerBoardingDecision EvaluateBoarding(
        int passengerCount,
        int passengerCapacity,
        int passengerMoney,
        int fare,
        bool fareExempt)
    {
        if (passengerCount >= passengerCapacity)
        {
            return new LocalBusPassengerBoardingDecision(LocalBusPassengerBoardingDecisionKind.ContinueWaiting, 0);
        }

        if (!fareExempt && passengerMoney < fare)
        {
            return new LocalBusPassengerBoardingDecision(LocalBusPassengerBoardingDecisionKind.FallBackToWalking, 0);
        }

        return new LocalBusPassengerBoardingDecision(
            LocalBusPassengerBoardingDecisionKind.Board,
            fareExempt ? 0 : fare);
    }
}
