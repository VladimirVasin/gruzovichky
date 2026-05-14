public partial class GameBootstrap
{
    private string GetLaborExchangePostingLabel(LaborExchangePosting posting)
    {
        if (posting == null)
        {
            return "Unknown vacancy";
        }

        return posting.Kind switch
        {
            VacancyKind.Production or VacancyKind.Service =>
                $"{GetBuildingInstanceDisplayName(posting.BuildingType, posting.BuildingInstanceId)} slot {posting.SlotIndex + 1}",
            VacancyKind.TruckDriver =>
                posting.ShiftIndex >= 0 && posting.ShiftIndex < ShiftNames.Length
                    ? $"Truck Driver {ShiftNames[posting.ShiftIndex]}"
                    : "Truck Driver",
            VacancyKind.BusDriver =>
                posting.ShiftIndex >= 0 && posting.ShiftIndex < ShiftNames.Length
                    ? $"Bus Driver {ShiftNames[posting.ShiftIndex]}"
                    : "Bus Driver",
            _ => "Unknown vacancy"
        };
    }

    private string GetLaborExchangePostingDisplayLabel(LaborExchangePosting posting, bool ru)
    {
        if (posting == null)
        {
            return ru ? "\u043d\u0435\u0438\u0437\u0432\u0435\u0441\u0442\u043d\u043e" : "unknown";
        }

        if (posting.Kind == VacancyKind.Production || posting.Kind == VacancyKind.Service)
        {
            string role = L(GetBuildingWorkerRoleLabel(posting.BuildingType));
            string building = L(GetBuildingInstanceDisplayName(posting.BuildingType, posting.BuildingInstanceId));
            string slot = GetMaxBuildingWorkerSlots(posting.BuildingType) > 1 ? $" #{posting.SlotIndex + 1}" : string.Empty;
            return $"{role}: {building}{slot}";
        }

        if (posting.ShiftIndex >= 0 && posting.ShiftIndex < ShiftPresetHours.Length)
        {
            string title = posting.Kind == VacancyKind.BusDriver
                ? (ru ? "\u0412\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u0430\u0432\u0442\u043e\u0431\u0443\u0441\u0430" : "Bus Driver")
                : (ru ? "\u0412\u043e\u0434\u0438\u0442\u0435\u043b\u044c \u0433\u0440\u0443\u0437\u043e\u0432\u0438\u043a\u0430" : "Truck Driver");
            return $"{title}: {L(ShiftNames[posting.ShiftIndex])}";
        }

        return GetLaborExchangePostingLabel(posting);
    }
}
