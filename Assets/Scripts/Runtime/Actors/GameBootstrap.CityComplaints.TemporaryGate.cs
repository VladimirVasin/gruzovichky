public partial class GameBootstrap
{
    private const bool CityHallSocialIntroductionOnlyMode = true;

    private static bool IsCityComplaintCategoryTemporarilyEnabled(CityComplaintCategory category)
    {
        return !CityHallSocialIntroductionOnlyMode ||
               category == CityComplaintCategory.SocialIntroduction;
    }

    private void ResolveTemporarilyDisabledCityComplaints()
    {
        if (!CityHallSocialIntroductionOnlyMode || cityComplaints.Count == 0)
        {
            return;
        }

        float now = GetCurrentWorldHour();
        bool changed = false;
        for (int i = 0; i < cityComplaints.Count; i++)
        {
            CityComplaint complaint = cityComplaints[i];
            if (!IsCityComplaintActive(complaint) ||
                IsCityComplaintCategoryTemporarilyEnabled(complaint.Category))
            {
                continue;
            }

            complaint.State = CityComplaintState.Resolved;
            complaint.ResolvedWorldHour = now;
            complaint.ResolvedDay = currentDay;
            complaint.ResolveReason = "temporarily disabled";
            complaint.ManuallyResolved = true;
            complaint.DueWorldHour = 0f;
            complaint.IsUnread = false;
            cityComplaintCooldownByKey[GetCityComplaintCooldownKey(complaint)] =
                now + CityComplaintCooldownWorldHours;
            changed = true;
            SessionDebugLogger.Log(
                "CITY_HALL",
                $"Citizen request #{complaint.Id} resolved by temporary SocialIntroduction-only gate: category={complaint.Category}, key={complaint.GroupKey}.");
        }

        if (cityComplaintPendingGroups.Count > 0)
        {
            cityComplaintPendingGroups.Clear();
            changed = true;
        }

        if (changed)
        {
            isCityHallScreenDirty = true;
        }
    }
}
