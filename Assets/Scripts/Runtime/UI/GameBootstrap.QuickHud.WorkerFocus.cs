public partial class GameBootstrap
{
    private void FocusWorkerFromQuickHud(int driverId, string source)
    {
        if (driverId <= 0)
        {
            return;
        }

        DriverAgent driver = driverAgents.Find(d => d.DriverId == driverId);
        if (driver == null)
        {
            return;
        }

        LogUiInput($"Quick HUD: focused {driver.DriverName} from {source}");
        FocusDriver(driverId);
    }

    private string FormatWorkerPerksInline(DriverAgent driver, bool ru, int maxVisible = 3)
    {
        if (driver == null)
        {
            return "-";
        }

        EnsureWorkerPerks(driver);
        if (driver.Perks.Count == 0)
        {
            return ru ? "\u043d\u0435\u0442" : "none";
        }

        string text = string.Empty;
        int count = driver.Perks.Count < maxVisible ? driver.Perks.Count : maxVisible;
        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                text += ", ";
            }

            text += GetWorkerPerkDisplayName(driver.Perks[i], ru);
        }

        if (driver.Perks.Count > count)
        {
            text += ru ? $" +{driver.Perks.Count - count}" : $" +{driver.Perks.Count - count}";
        }

        return text;
    }
}
