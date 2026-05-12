public partial class GameBootstrap
{
    private bool TryAcceptCityHallRequestCommand(int complaintId, out bool startsSocialScene)
    {
        startsSocialScene = false;
        CityComplaint complaint = GetCityComplaintById(complaintId);
        if (complaint == null)
        {
            return false;
        }

        startsSocialScene = complaint.Category == CityComplaintCategory.SocialIntroduction;
        return AcceptCityComplaint(complaintId);
    }

    private bool TryRejectCityHallRequestCommand(int complaintId)
    {
        return RejectCityComplaint(complaintId);
    }

    private bool TryPurchaseCityUpgradeCommand(CityUpgradeId upgradeId)
    {
        return PurchaseCityUpgrade(upgradeId);
    }
}
