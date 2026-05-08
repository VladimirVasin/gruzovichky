public partial class GameBootstrap
{
    private string GetStaffingScreenTitle(bool ru)
    {
        if (locations.ContainsKey(LocationType.LaborExchange))
        {
            return ru ? "\u0411\u0438\u0440\u0436\u0430 \u0442\u0440\u0443\u0434\u0430" : "Labor Exchange";
        }

        return ru ? "\u0420\u0443\u0447\u043d\u044b\u0435 \u043d\u0430\u0437\u043d\u0430\u0447\u0435\u043d\u0438\u044f" : "Manual Staffing";
    }

    private string GetStaffingScreenCountLabel(int openSlots, int totalSlots, bool ru)
    {
        if (locations.ContainsKey(LocationType.LaborExchange))
        {
            int availablePostings = CountAvailableLaborExchangePostings();
            int reservedPostings = CountReservedLaborExchangePostings();
            return ru
                ? $"{availablePostings} \u043e\u0431\u044a\u044f\u0432\u043b. / {reservedPostings} \u0432 \u043f\u0443\u0442\u0438 / {openSlots} \u0441\u043b\u043e\u0442\u043e\u0432"
                : $"{availablePostings} posted / {reservedPostings} reserved / {openSlots} slots";
        }

        return ru
            ? $"{openSlots} \u0441\u0432\u043e\u0431\u043e\u0434\u043d\u043e / {totalSlots} \u0432\u0441\u0435\u0433\u043e"
            : $"{openSlots} free / {totalSlots} total";
    }

    private string GetStaffingScreenEmptyHint(bool ru)
    {
        if (locations.ContainsKey(LocationType.LaborExchange))
        {
            return ru
                ? "\u0411\u0438\u0440\u0436\u0430 \u0441\u0430\u043c\u0430 \u043f\u0443\u0431\u043b\u0438\u043a\u0443\u0435\u0442 \u043e\u0442\u043a\u0440\u044b\u0442\u044b\u0435 \u0441\u043b\u043e\u0442\u044b. \u0412\u044b\u0431\u0435\u0440\u0438 \u0441\u043b\u0435\u0432\u0430 \u043f\u043e\u0437\u0438\u0446\u0438\u044e, \u0435\u0441\u043b\u0438 \u043d\u0443\u0436\u0435\u043d \u0440\u0443\u0447\u043d\u043e\u0439 override."
                : "The Labor Exchange posts open slots automatically. Pick a position on the left only when you need a manual override.";
        }

        return ru
            ? "\u0414\u043e \u0411\u0438\u0440\u0436\u0438 \u0442\u0440\u0443\u0434\u0430 \u044d\u0442\u043e \u0440\u0443\u0447\u043d\u043e\u0439 tutorial-\u044d\u043a\u0440\u0430\u043d: \u0432\u044b\u0431\u0435\u0440\u0438 \u0441\u043b\u043e\u0442, \u0441\u043c\u0435\u043d\u0443 \u0438 \u0440\u0430\u0431\u043e\u0447\u0435\u0433\u043e."
            : "Before the Labor Exchange, this is a tutorial-only manual screen: pick a slot, shift, and worker.";
    }
}
