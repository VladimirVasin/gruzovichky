public partial class GameBootstrap
{
    private void RefreshCitizenProfession(DriverAgent worker)
    {
        if (worker == null)
        {
            return;
        }

        if (worker.CitizenId <= 0)
        {
            worker.CitizenId = worker.DriverId;
        }

        worker.CitizenProfession = ResolveCitizenProfession(worker);
    }

    private CitizenProfessionKind ResolveCitizenProfession(DriverAgent worker)
    {
        if (worker == null || worker.ContractVacancyKind == VacancyKind.None)
        {
            return CitizenProfessionKind.Resident;
        }

        return worker.ContractVacancyKind switch
        {
            VacancyKind.TruckDriver => CitizenProfessionKind.TruckDriver,
            VacancyKind.Intercity => CitizenProfessionKind.IntercityDriver,
            VacancyKind.BusDriver => CitizenProfessionKind.BusDriver,
            VacancyKind.Production => ResolveProductionCitizenProfession(worker.ContractBuildingType),
            VacancyKind.Service => ResolveServiceCitizenProfession(worker.ContractBuildingType),
            _ => CitizenProfessionKind.Resident
        };
    }

    private static CitizenProfessionKind ResolveProductionCitizenProfession(LocationType? buildingType)
    {
        return buildingType switch
        {
            LocationType.Forest => CitizenProfessionKind.Lumberjack,
            LocationType.Sawmill => CitizenProfessionKind.SawmillWorker,
            LocationType.FurnitureFactory => CitizenProfessionKind.Carpenter,
            LocationType.Warehouse => CitizenProfessionKind.WarehouseWorker,
            LocationType.Docks => CitizenProfessionKind.DockWorker,
            _ => CitizenProfessionKind.ProductionWorker
        };
    }

    private static CitizenProfessionKind ResolveServiceCitizenProfession(LocationType? buildingType)
    {
        return buildingType switch
        {
            LocationType.CleaningDepot => CitizenProfessionKind.Cleaner,
            LocationType.LaborExchange => CitizenProfessionKind.EmploymentClerk,
            LocationType.Kindergarten => CitizenProfessionKind.ChildcareWorker,
            LocationType.CarMarket => CitizenProfessionKind.CarDealer,
            _ => CitizenProfessionKind.ServiceWorker
        };
    }

    private static string GetCitizenProfessionLabel(DriverAgent worker)
    {
        if (worker == null)
        {
            return "Resident";
        }

        return worker.CitizenProfession switch
        {
            CitizenProfessionKind.TruckDriver => "Truck driver",
            CitizenProfessionKind.IntercityDriver => "Intercity driver",
            CitizenProfessionKind.BusDriver => "Bus driver",
            CitizenProfessionKind.ProductionWorker => "Production worker",
            CitizenProfessionKind.Lumberjack => "Lumberjack",
            CitizenProfessionKind.SawmillWorker => "Sawmill worker",
            CitizenProfessionKind.Carpenter => "Carpenter",
            CitizenProfessionKind.WarehouseWorker => "Warehouse worker",
            CitizenProfessionKind.DockWorker => "Dock worker",
            CitizenProfessionKind.ServiceWorker => "Service worker",
            CitizenProfessionKind.Cleaner => "Cleaner",
            CitizenProfessionKind.EmploymentClerk => "Employment clerk",
            CitizenProfessionKind.ChildcareWorker => "Childcare worker",
            CitizenProfessionKind.CarDealer => "Car dealer",
            _ => "Resident"
        };
    }
}
