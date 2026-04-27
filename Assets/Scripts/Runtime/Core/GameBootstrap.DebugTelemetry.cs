using UnityEngine;

public partial class GameBootstrap
{
    private void LogWorkerDecision(DriverAgent driver, string decision, string reason, bool force = false)
    {
        if (driver == null)
        {
            return;
        }

        string key = $"{decision}|{reason}|{driver.WalkPhase}|{driver.RestPhase}|{driver.LifeGoal}|{driver.DutyMode}|{driver.ShiftStartHour}|{driver.AssignedBuildingType}|{driver.AssignedTruckNumber}|{driver.IsOnActiveShift}|{driver.IsInsideBuilding}";
        if (!force && driver.LastWorkerDecisionDebugKey == key)
        {
            return;
        }

        driver.LastWorkerDecisionDebugKey = key;
        SessionDebugLogger.Log(
            "WORKER_DECISION",
            $"{driver.DriverName}: {decision}; reason={reason}; {FormatWorkerDecisionSnapshot(driver)}.");
    }

    private string FormatWorkerDecisionSnapshot(DriverAgent driver)
    {
        if (driver == null)
        {
            return "worker=null";
        }

        Vector2Int cell = GetWorkerDebugCell(driver);
        string assignment = driver.AssignedBuildingType.HasValue
            ? driver.AssignedBuildingType.Value.ToString()
            : driver.AssignedTruckNumber > 0
                ? $"Truck #{driver.AssignedTruckNumber}"
                : "none";
        string bus = driver.BusDestinationStopNumber > 0
            ? $"bus={driver.WalkPhase} Stop#{driver.BusOriginStopNumber}->Stop#{driver.BusDestinationStopNumber} ({driver.BusTravelReason})"
            : "bus=none";

        return $"cell=({cell.x},{cell.y}), duty={driver.DutyMode}, assignment={assignment}, shift={driver.ShiftStartHour}, onShift={driver.IsOnActiveShift}, inside={driver.IsInsideBuilding}, walk={driver.WalkPhase}, rest={driver.RestPhase}, goal={driver.LifeGoal}, money=${driver.Money}, needs=[{FormatWorkerNeedsDebug(driver)}], {bus}";
    }

    private Vector2Int GetWorkerDebugCell(DriverAgent driver)
    {
        if (driver == null || driver.DriverObject == null)
        {
            return new Vector2Int(-1, -1);
        }

        return WorldToCell(driver.DriverObject.transform.position);
    }

    private void LogBuildingBankTransaction(LocationData location, DriverAgent driver, int amount, string reason, int driverMoneyBefore, int bankBefore)
    {
        if (location == null)
        {
            return;
        }

        string workerLabel = driver != null ? driver.DriverName : "n/a";
        int driverMoneyAfter = driver != null ? driver.Money : driverMoneyBefore;
        SessionDebugLogger.Log(
            "BUILDING_BANK",
            $"{location.Label}: +${amount} from {workerLabel}; reason={reason}; workerMoney ${driverMoneyBefore}->{driverMoneyAfter}; bank ${bankBefore}->{location.BuildingBank}; resources fuel={location.FuelStored}, alcohol={location.AlcoholStored}, food={location.FoodStored}, logs={location.LogsStored}, boards={location.BoardsStored}, textile={location.TextileStored}, furniture={location.FurnitureStored}.");
    }

    private void LogEconomyMovement(MoneyLedgerEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        string treasuryAfter = entry.TreasuryAfter.HasValue ? $" treasuryAfter=${entry.TreasuryAfter.Value};" : string.Empty;
        string recipientAfter = entry.RecipientBalanceAfter.HasValue ? $" recipientBalanceAfter=${entry.RecipientBalanceAfter.Value};" : string.Empty;
        SessionDebugLogger.Log(
            "ECON",
            $"ledger delta={entry.TreasuryDelta:+#;-#;0}; from={entry.FromLabel}; to={entry.ToLabel}; reason={entry.Reason};{treasuryAfter}{recipientAfter} time={entry.TimeLabel}.");
    }
}
