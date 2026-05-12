using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private enum CityUpgradeId
    {
        PublicBins,
        CleanerRoutes,
        TransparentEstimates,
        BulkProcurement,
        CitizenReception,
        PublicReports
    }

    private enum CityUpgradeBranch
    {
        Cleanliness,
        Economy,
        Trust
    }

    private sealed class CityUpgradeDefinition
    {
        public readonly CityUpgradeId Id;
        public readonly CityUpgradeBranch Branch;
        public readonly int Level;
        public readonly int Cost;
        public readonly int RequiredTrust;
        public readonly CityUpgradeId? Parent;
        public readonly string TitleRu;
        public readonly string TitleEn;
        public readonly string DescriptionRu;
        public readonly string DescriptionEn;

        public CityUpgradeDefinition(
            CityUpgradeId id,
            CityUpgradeBranch branch,
            int level,
            int cost,
            int requiredTrust,
            CityUpgradeId? parent,
            string titleRu,
            string titleEn,
            string descriptionRu,
            string descriptionEn)
        {
            Id = id;
            Branch = branch;
            Level = level;
            Cost = cost;
            RequiredTrust = requiredTrust;
            Parent = parent;
            TitleRu = titleRu;
            TitleEn = titleEn;
            DescriptionRu = descriptionRu;
            DescriptionEn = descriptionEn;
        }
    }

    private static readonly CityUpgradeDefinition[] CityUpgradeDefinitions =
    {
        new(
            CityUpgradeId.PublicBins,
            CityUpgradeBranch.Cleanliness,
            0,
            300,
            -20,
            null,
            "Городские урны",
            "Public bins",
            "Снижает прирост уличного мусора в местах скопления людей.",
            "Reduces street litter growth around crowded places."),
        new(
            CityUpgradeId.CleanerRoutes,
            CityUpgradeBranch.Cleanliness,
            1,
            550,
            0,
            CityUpgradeId.PublicBins,
            "Маршруты уборки",
            "Cleaner routes",
            "Увеличивает зелёный радиус работы уборщиков и их доступную зону.",
            "Expands cleaner coverage radius and reachable cleanup area."),
        new(
            CityUpgradeId.TransparentEstimates,
            CityUpgradeBranch.Economy,
            0,
            650,
            20,
            null,
            "Прозрачная смета",
            "Transparent estimates",
            "Даёт скидку 5% на строительство зданий и дорог.",
            "Gives a 5% discount on building and road construction."),
        new(
            CityUpgradeId.BulkProcurement,
            CityUpgradeBranch.Economy,
            1,
            900,
            35,
            CityUpgradeId.TransparentEstimates,
            "Оптовые закупки",
            "Bulk procurement",
            "Усиливает строительную скидку до 10%.",
            "Improves the construction discount to 10%."),
        new(
            CityUpgradeId.CitizenReception,
            CityUpgradeBranch.Trust,
            0,
            600,
            10,
            null,
            "Приёмная граждан",
            "Citizen reception",
            "Даёт +6 часов на выполнение принятых обращений.",
            "Adds 6 hours to accepted request deadlines."),
        new(
            CityUpgradeId.PublicReports,
            CityUpgradeBranch.Trust,
            1,
            850,
            35,
            CityUpgradeId.CitizenReception,
            "Публичные отчёты",
            "Public reports",
            "Снижает штраф доверия за отказ или просрочку с -25 до -15.",
            "Reduces rejection and expiry trust penalties from -25 to -15.")
    };

    private readonly HashSet<CityUpgradeId> purchasedCityUpgrades = new();

    private void ResetCityUpgrades()
    {
        purchasedCityUpgrades.Clear();
        isCityHallUpgradesTabActive = false;
        isCityHallScreenDirty = true;
    }

    private bool HasCityUpgrade(CityUpgradeId upgradeId)
    {
        return purchasedCityUpgrades.Contains(upgradeId);
    }

    private int CountPurchasedCityUpgrades()
    {
        return purchasedCityUpgrades.Count;
    }

    private bool TryGetCityUpgradeDefinition(CityUpgradeId upgradeId, out CityUpgradeDefinition definition)
    {
        for (int i = 0; i < CityUpgradeDefinitions.Length; i++)
        {
            if (CityUpgradeDefinitions[i].Id == upgradeId)
            {
                definition = CityUpgradeDefinitions[i];
                return true;
            }
        }

        definition = null;
        return false;
    }

    private bool CanPurchaseCityUpgrade(CityUpgradeId upgradeId, out string reasonRu, out string reasonEn)
    {
        reasonRu = string.Empty;
        reasonEn = string.Empty;
        if (!TryGetCityUpgradeDefinition(upgradeId, out CityUpgradeDefinition definition))
        {
            reasonRu = "Апдейт не найден.";
            reasonEn = "Upgrade not found.";
            return false;
        }

        if (HasCityUpgrade(upgradeId))
        {
            reasonRu = "Уже куплено.";
            reasonEn = "Already purchased.";
            return false;
        }

        if (definition.Parent.HasValue && !HasCityUpgrade(definition.Parent.Value))
        {
            string parentTitle = TryGetCityUpgradeDefinition(definition.Parent.Value, out CityUpgradeDefinition parent)
                ? parent.TitleRu
                : definition.Parent.Value.ToString();
            reasonRu = $"Сначала нужен апдейт: {parentTitle}.";
            reasonEn = "Parent upgrade is required first.";
            return false;
        }

        if (cityTrust < definition.RequiredTrust)
        {
            reasonRu = $"Нужно доверие {FormatCityUpgradeSignedValue(definition.RequiredTrust)}.";
            reasonEn = $"Requires trust {FormatCityUpgradeSignedValue(definition.RequiredTrust)}.";
            return false;
        }

        if (money < definition.Cost)
        {
            reasonRu = $"В казне ${money}, нужно ${definition.Cost}.";
            reasonEn = $"Treasury has ${money}, needs ${definition.Cost}.";
            return false;
        }

        return true;
    }

    private bool PurchaseCityUpgrade(CityUpgradeId upgradeId)
    {
        if (!TryGetCityUpgradeDefinition(upgradeId, out CityUpgradeDefinition definition))
        {
            return false;
        }

        if (!CanPurchaseCityUpgrade(upgradeId, out string reasonRu, out string reasonEn))
        {
            PushFeedEvent(
                $"City upgrade unavailable: {reasonEn}",
                $"Апдейт города недоступен: {reasonRu}",
                FeedEventType.Warning);
            return false;
        }

        money -= definition.Cost;
        purchasedCityUpgrades.Add(upgradeId);
        RecordMoneyMovement(
            -definition.Cost,
            "Treasury",
            "City Hall",
            $"City upgrade: {definition.TitleEn}",
            money,
            null,
            MoneyAccountKind.CityBudget,
            MoneyAccountKind.External,
            MoneyTransactionReasonKind.CityUpgrade);
        moneyPopupAmount = -definition.Cost;
        moneyPopupTimer = MoneyPopupDuration;

        isCityHallScreenDirty = true;
        isEconomyScreenDirty = true;
        isBuildScreenDirty = true;
        isFleetScreenDirty = true;
        RefreshCleaningDepotRadiusVisuals();

        PushFeedEvent(
            $"City upgrade purchased: {definition.TitleEn}. -${definition.Cost}.",
            $"Куплен апдейт города: {definition.TitleRu}. -${definition.Cost}.",
            FeedEventType.Money);
        SessionDebugLogger.Log(
            "CITY_UPGRADE",
            $"Purchased {upgradeId}: cost=${definition.Cost}, trust={cityTrust}, treasury=${money}.");
        return true;
    }

    private float GetStreetLitterCityUpgradeGainMultiplier()
    {
        return HasCityUpgrade(CityUpgradeId.PublicBins) ? 0.78f : 1f;
    }

    private float GetCleanerCoverageRadius()
    {
        return HasCityUpgrade(CityUpgradeId.CleanerRoutes)
            ? 22f
            : CleanerCoverageRadius;
    }

    private int ApplyCityUpgradeBuildCostDiscount(int baseCost)
    {
        if (baseCost <= 0)
        {
            return baseCost;
        }

        float multiplier = HasCityUpgrade(CityUpgradeId.BulkProcurement)
            ? 0.90f
            : HasCityUpgrade(CityUpgradeId.TransparentEstimates)
                ? 0.95f
                : 1f;
        return Mathf.Max(1, Mathf.RoundToInt(baseCost * multiplier));
    }

    private float GetCityComplaintDueWorldHours()
    {
        return CityComplaintDueWorldHours + (HasCityUpgrade(CityUpgradeId.CitizenReception) ? 6f : 0f);
    }

    private int GetCityTrustComplaintExpiredPenalty()
    {
        return ApplyCityUpgradeTrustPenaltyRelief(CityTrustComplaintExpiredPenalty);
    }

    private int GetCityTrustCitizenRequestRejectedPenalty()
    {
        return ApplyCityUpgradeTrustPenaltyRelief(CityTrustCitizenRequestRejectedPenalty);
    }

    private int ApplyCityUpgradeTrustPenaltyRelief(int basePenalty)
    {
        if (basePenalty >= 0 || !HasCityUpgrade(CityUpgradeId.PublicReports))
        {
            return basePenalty;
        }

        return Mathf.Min(-1, Mathf.RoundToInt(basePenalty * 0.60f));
    }

    private bool IsCityUpgradeProblemRelevant(CityUpgradeDefinition definition, out string reasonRu, out string reasonEn)
    {
        reasonRu = string.Empty;
        reasonEn = string.Empty;
        if (definition == null)
        {
            return false;
        }

        switch (definition.Branch)
        {
            case CityUpgradeBranch.Cleanliness:
                if (CountVisibleStreetLitterCells() >= 6 ||
                    HasRecentNegativeSocialSignalCategory(SocialSignalCategory.Litter, 2, 48))
                {
                    reasonRu = "\u041d\u043e\u043e\u0441\u0444\u0435\u0440\u0430 \u0432\u0438\u0434\u0438\u0442 \u0440\u043e\u0441\u0442 \u0442\u0435\u043c\u044b \u0433\u0440\u044f\u0437\u0438.";
                    reasonEn = "Noosphere sees the cleanliness problem growing.";
                    return true;
                }
                break;
            case CityUpgradeBranch.Economy:
                if (money < 900 ||
                    HasRecentNegativeSocialSignalCategory(SocialSignalCategory.Money, 2, 42))
                {
                    reasonRu = "\u041a\u0430\u0437\u043d\u0430 \u0438 \u0434\u0435\u043d\u0435\u0436\u043d\u044b\u0435 \u043c\u044b\u0441\u043b\u0438 \u0434\u0430\u0432\u044f\u0442 \u043d\u0430 \u0433\u043e\u0440\u043e\u0434.";
                    reasonEn = "Treasury pressure and money signals are visible.";
                    return true;
                }
                break;
            case CityUpgradeBranch.Trust:
                if (cityTrust < 10 ||
                    CountOpenCityComplaints() >= 2 ||
                    CountExpiredCityComplaints() > 0 ||
                    HasRecentNegativeSocialSignalCategory(SocialSignalCategory.Governance, 1, 36))
                {
                    reasonRu = "\u0416\u0438\u0442\u0435\u043b\u0438 \u0447\u0430\u0449\u0435 \u0434\u0430\u0432\u044f\u0442 \u043d\u0430 \u0420\u0430\u0442\u0443\u0448\u0443 \u0438 \u0434\u043e\u0432\u0435\u0440\u0438\u0435.";
                    reasonEn = "Citizens are putting pressure on City Hall and trust.";
                    return true;
                }
                break;
        }

        return false;
    }

    private bool HasRecentNegativeSocialSignalCategory(SocialSignalCategory category, int minCount, int minStrength)
    {
        int latestDay = GetLatestSocialSignalDay();
        if (latestDay <= 0)
        {
            return false;
        }

        int count = 0;
        int strength = 0;
        for (int i = 0; i < socialSignals.Count; i++)
        {
            SocialSignal signal = socialSignals[i];
            if (signal == null)
            {
                continue;
            }

            if (signal.Day < latestDay)
            {
                break;
            }

            if (signal.Day != latestDay ||
                signal.Tone != SocialSignalTone.Negative ||
                signal.Category != category)
            {
                continue;
            }

            count++;
            strength += signal.DailyScoreHint != 0 ? Mathf.Abs(signal.DailyScoreHint) : Mathf.Clamp(signal.Strength, 1, 100);
            if (count >= minCount && strength >= minStrength)
            {
                return true;
            }
        }

        return false;
    }

    private static string FormatCityUpgradeSignedValue(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }
}
