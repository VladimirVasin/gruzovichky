using System.Collections.Generic;

public partial class GameBootstrap
{
    private void CollectDailyBuildingTaxes()
    {
        EnsureDefaultTaxPolicies();
        EnsureTaxDayStatsCurrent();
        if (lastTaxCollectionDay == currentDay)
        {
            return;
        }

        TaxPolicy policy = GetTaxPolicy(TaxSourceKind.BuildingCashReserve);
        if (policy == null || !policy.IsEnabled || policy.RatePercent <= 0)
        {
            lastTaxCollectionDay = currentDay;
            lastTaxCollectedAmount = 0;
            lastTaxedBuildingCount = 0;
            isEconomyScreenDirty = true;
            SessionDebugLogger.Log("TAX", $"Daily building cash reserve tax skipped on day {currentDay}: policy disabled or zero rate.");
            return;
        }

        int treasuryBefore = money;
        int totalCollected = 0;
        int taxedBuildings = 0;
        int taxableBankTotal = 0;
        List<string> taxedBreakdown = new();

        foreach (LocationData location in EnumerateTaxableBuildingBankLocations())
        {
            if (location == null || location.RootObject == null || location.BuildingBank <= 0)
            {
                continue;
            }

            taxableBankTotal += location.BuildingBank;
            int bankBefore = location.BuildingBank;
            int taxAmount = CalculateTaxAmount(policy, location.BuildingBank, 1);
            if (taxAmount <= 0)
            {
                SessionDebugLogger.Log(
                    "TAX_DETAIL",
                    $"{location.Label}: bank=${bankBefore}, rate={policy.RatePercent}%, tax=$0 (below rounding threshold).");
                continue;
            }

            location.BuildingBank -= taxAmount;
            totalCollected += taxAmount;
            taxedBuildings++;
            taxedBreakdown.Add($"{location.Label} ${taxAmount}");
            SessionDebugLogger.Log(
                "TAX_DETAIL",
                $"{location.Label}: bank ${bankBefore}->{location.BuildingBank}, rate={policy.RatePercent}%, collected=${taxAmount}.");
        }

        money += totalCollected;
        lastTaxCollectionDay = currentDay;
        lastTaxCollectedAmount = totalCollected;
        lastTaxedBuildingCount = taxedBuildings;
        if (totalCollected > 0)
        {
            TrackTaxCollected(totalCollected);
            RecordMoneyMovement(
                totalCollected,
                policy.Name,
                "Treasury",
                $"Daily tax collection from {taxedBuildings} building(s)",
                money,
                null,
                MoneyAccountKind.BuildingCash,
                MoneyAccountKind.CityBudget,
                MoneyTransactionReasonKind.BuildingTax);
            PlayUiSound(moneyCollectClip, 0.48f);
        }
        else
        {
            isEconomyScreenDirty = true;
        }

        string breakdown = taxedBreakdown.Count > 0 ? string.Join(", ", taxedBreakdown) : "none";
        SessionDebugLogger.Log(
            "TAX",
            $"Collected ${totalCollected} on day {currentDay} from {taxedBuildings} building(s). Taxable bank total=${taxableBankTotal}. Breakdown: {breakdown}.");

        int actualTreasuryDelta = money - treasuryBefore;
        if (actualTreasuryDelta != totalCollected)
        {
            SessionDebugLogger.Log(
                "TAX",
                $"Treasury delta mismatch after tax collection. Expected ${totalCollected}, actual ${actualTreasuryDelta}.");
        }
    }

    private int GetCurrentTaxableBuildingBankTotal()
    {
        int total = 0;
        foreach (LocationData location in EnumerateTaxableBuildingBankLocations())
        {
            if (location == null || location.RootObject == null || location.BuildingBank <= 0)
            {
                continue;
            }

            total += location.BuildingBank;
        }

        return total;
    }
}
