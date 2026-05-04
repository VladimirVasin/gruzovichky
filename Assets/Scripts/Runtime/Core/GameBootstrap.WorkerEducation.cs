using UnityEngine;

public partial class GameBootstrap
{
    private void AssignWorkerEducation(DriverAgent driver)
    {
        if (driver == null)
        {
            return;
        }

        float roll = Random.value;
        driver.Education = roll < 0.18f
            ? WorkerEducationLevel.Higher
            : roll < 0.48f
                ? WorkerEducationLevel.Vocational
                : WorkerEducationLevel.Basic;
    }

    private void PromoteWorkerToHigherEducation(DriverAgent driver, string reason)
    {
        if (driver == null || driver.Education == WorkerEducationLevel.Higher)
        {
            return;
        }

        driver.Education = WorkerEducationLevel.Higher;
        SessionDebugLogger.Log("LABOR_EXCHANGE", $"{driver.DriverName} education promoted to Higher ({reason}).");
    }

    private static bool HasHigherEducation(DriverAgent driver)
    {
        return driver != null && driver.Education == WorkerEducationLevel.Higher;
    }

    private static bool DoesBuildingRequireHigherEducation(LocationType buildingType)
    {
        return buildingType == LocationType.LaborExchange;
    }

    private bool CanWorkerMeetBuildingEducationRequirement(DriverAgent driver, LocationType buildingType, out string reason)
    {
        bool ru = IsRussianLanguage();
        reason = string.Empty;
        if (!DoesBuildingRequireHigherEducation(buildingType))
        {
            return true;
        }

        if (HasHigherEducation(driver))
        {
            return true;
        }

        reason = ru
            ? "\u043d\u0443\u0436\u043d\u043e \u0432\u044b\u0441\u0448\u0435\u0435 \u043e\u0431\u0440\u0430\u0437\u043e\u0432\u0430\u043d\u0438\u0435"
            : "higher education required";
        return false;
    }

    private static string GetWorkerEducationDisplayName(WorkerEducationLevel education, bool ru)
    {
        return education switch
        {
            WorkerEducationLevel.Higher => ru ? "\u0412\u044b\u0441\u0448\u0435\u0435" : "Higher",
            WorkerEducationLevel.Vocational => ru ? "\u0421\u0440\u0435\u0434\u043d\u0435\u0435 \u0441\u043f\u0435\u0446." : "Vocational",
            _ => ru ? "\u0411\u0430\u0437\u043e\u0432\u043e\u0435" : "Basic"
        };
    }
}
