public enum VacancyFlowKind
{
    TruckDriver,
    Intercity,
    BusDriver,
    Production,
    Service
}

public static class VacancyFlowRulesService
{
    public static bool RequiresShiftStep(VacancyFlowKind kind)
    {
        return true;
    }

    public static bool RequiresTruckStep(VacancyFlowKind kind)
    {
        return false;
    }

    public static int GetCurrentStep(bool isOccupied, bool requiresShift, bool hasShift, bool requiresTruck, bool hasTruck)
    {
        if (isOccupied)
        {
            return 4;
        }

        if (requiresShift && !hasShift)
        {
            return 1;
        }

        if (requiresTruck && !hasTruck)
        {
            return 2;
        }

        return 3;
    }
}
