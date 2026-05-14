using System.Collections.Generic;
using UnityEngine;

public partial class GameBootstrap
{
    private enum TaxSourceKind
    {
        BuildingCashReserve,
        ServiceSale,
        SalaryIncome,
        TransportFare,
        PropertyPurchase,
        VehiclePurchase,
        GamblingRevenue,
        TradeImport,
        TradeExport,
        ConstructionPermit
    }

    private enum TaxFrequency
    {
        PerTransaction,
        Daily
    }

    private enum TaxIncidence
    {
        TakenFromReceiver,
        AddedToPayerCost
    }

    private sealed class TaxPolicy
    {
        public TaxPolicy()
        {
            FlatAmount = 0;
            PerUnitAmount = 0;
            AppliesToLocationTypes = System.Array.Empty<LocationType>();
            AppliesToResourceTypes = System.Array.Empty<TradeResourceType>();
        }

        public int Id;
        public string Name;
        public TaxSourceKind SourceKind;
        public TaxFrequency Frequency;
        public TaxIncidence Incidence;
        public bool IsEnabled;
        public bool IsPlayerCreated;
        public int RatePercent;
        public int FlatAmount;
        public int PerUnitAmount;
        public int MinTaxableAmount;
        public int MaxTaxAmount;
        public LocationType[] AppliesToLocationTypes;
        public TradeResourceType[] AppliesToResourceTypes;
        public string Description;
    }

    private sealed class TaxableEvent
    {
        public TaxableEvent()
        {
            ResourceType = null;
            PayerLocation = null;
        }

        public TaxSourceKind SourceKind;
        public int Amount;
        public int Quantity = 1;
        public LocationType? LocationType;
        public int LocationInstanceId;
        public TradeResourceType? ResourceType;
        public DriverAgent PayerWorker;
        public DriverAgent ReceiverWorker;
        public LocationData PayerLocation;
        public LocationData ReceiverLocation;
        public string ExternalPayerLabel;
        public string Reason;
    }

    private void EnsureDefaultTaxPolicies()
    {
        if (taxPolicies.Count > 0)
        {
            SyncPrimaryTaxRateField();
            return;
        }

        AddTaxPolicy(
            "Service Sales Tax",
            TaxSourceKind.ServiceSale,
            TaxFrequency.PerTransaction,
            TaxIncidence.TakenFromReceiver,
            dailyBuildingTaxPercent,
            isEnabled: true,
            "Charged from service building revenue when residents pay for food, sleep, leisure, or kiosk goods.");

        AddTaxPolicy(
            "Daily Cash Reserve Tax",
            TaxSourceKind.BuildingCashReserve,
            TaxFrequency.Daily,
            TaxIncidence.TakenFromReceiver,
            0,
            isEnabled: false,
            "Legacy-style daily tax from positive building cash reserves.");

        AddTaxPolicy(
            "Salary Withholding",
            TaxSourceKind.SalaryIncome,
            TaxFrequency.PerTransaction,
            TaxIncidence.TakenFromReceiver,
            0,
            isEnabled: false,
            "Withheld from a resident after a salary payout.");

        AddTaxPolicy(
            "Transport Fare Tax",
            TaxSourceKind.TransportFare,
            TaxFrequency.PerTransaction,
            TaxIncidence.TakenFromReceiver,
            0,
            isEnabled: false,
            "Charged from Parking when local bus fares are delivered.");

        AddTaxPolicy(
            "Property Transfer Tax",
            TaxSourceKind.PropertyPurchase,
            TaxFrequency.PerTransaction,
            TaxIncidence.TakenFromReceiver,
            0,
            isEnabled: false,
            "Charged from a house cash reserve when a resident buys a home.");

        AddTaxPolicy(
            "Vehicle Registration Tax",
            TaxSourceKind.VehiclePurchase,
            TaxFrequency.PerTransaction,
            TaxIncidence.TakenFromReceiver,
            0,
            isEnabled: false,
            "Charged from the car market when a resident buys a personal car.");

        AddTaxPolicy(
            "Gambling Revenue Tax",
            TaxSourceKind.GamblingRevenue,
            TaxFrequency.PerTransaction,
            TaxIncidence.TakenFromReceiver,
            0,
            isEnabled: false,
            "Charged from gambling hall revenue when residents lose money there.");

        AddTaxPolicy(
            "Import Tariff",
            TaxSourceKind.TradeImport,
            TaxFrequency.PerTransaction,
            TaxIncidence.AddedToPayerCost,
            0,
            isEnabled: false,
            "Customs revenue generated when the town imports resources.");

        AddTaxPolicy(
            "Export Duty",
            TaxSourceKind.TradeExport,
            TaxFrequency.PerTransaction,
            TaxIncidence.AddedToPayerCost,
            0,
            isEnabled: false,
            "Customs revenue generated when the town exports resources.");

        AddTaxPolicy(
            "Construction Permit Fee",
            TaxSourceKind.ConstructionPermit,
            TaxFrequency.PerTransaction,
            TaxIncidence.AddedToPayerCost,
            0,
            isEnabled: false,
            "Permit revenue generated when new buildings or roads are constructed.");

        SyncPrimaryTaxRateField();
    }

    private TaxPolicy AddTaxPolicy(
        string name,
        TaxSourceKind sourceKind,
        TaxFrequency frequency,
        TaxIncidence incidence,
        int ratePercent,
        bool isEnabled,
        string description)
    {
        TaxPolicy policy = new()
        {
            Id = nextTaxPolicyId++,
            Name = name,
            SourceKind = sourceKind,
            Frequency = frequency,
            Incidence = incidence,
            RatePercent = Mathf.Clamp(ratePercent, MinDailyBuildingTaxPercent, MaxDailyBuildingTaxPercent),
            IsEnabled = isEnabled,
            IsPlayerCreated = true,
            Description = description,
            MinTaxableAmount = 0,
            MaxTaxAmount = 0
        };

        taxPolicies.Add(policy);
        return policy;
    }

    private TaxPolicy GetTaxPolicyById(int policyId)
    {
        for (int i = 0; i < taxPolicies.Count; i++)
        {
            if (taxPolicies[i].Id == policyId)
            {
                return taxPolicies[i];
            }
        }

        return null;
    }

    private TaxPolicy GetTaxPolicy(TaxSourceKind sourceKind)
    {
        EnsureDefaultTaxPolicies();
        for (int i = 0; i < taxPolicies.Count; i++)
        {
            if (taxPolicies[i].SourceKind == sourceKind)
            {
                return taxPolicies[i];
            }
        }

        return null;
    }

    private TaxPolicy GetPrimaryTaxPolicy()
    {
        EnsureDefaultTaxPolicies();
        for (int i = 0; i < taxPolicies.Count; i++)
        {
            if (taxPolicies[i].SourceKind == TaxSourceKind.ServiceSale)
            {
                return taxPolicies[i];
            }
        }

        return taxPolicies.Count > 0 ? taxPolicies[0] : null;
    }

    private void SyncPrimaryTaxRateField()
    {
        TaxPolicy primary = null;
        for (int i = 0; i < taxPolicies.Count; i++)
        {
            if (taxPolicies[i].SourceKind == TaxSourceKind.ServiceSale)
            {
                primary = taxPolicies[i];
                break;
            }
        }

        primary ??= taxPolicies.Count > 0 ? taxPolicies[0] : null;
        if (primary != null)
        {
            dailyBuildingTaxPercent = primary.RatePercent;
        }
    }

    private void AdjustPrimaryTaxRate(int delta)
    {
        TaxPolicy primary = GetPrimaryTaxPolicy();
        if (primary == null)
        {
            return;
        }

        AdjustTaxPolicyRate(primary.Id, delta, checkTutorialGoal: true);
    }

    private void ToggleTaxPolicy(int policyId)
    {
        TaxPolicy policy = GetTaxPolicyById(policyId);
        if (policy == null)
        {
            return;
        }

        policy.IsEnabled = !policy.IsEnabled;
        SyncPrimaryTaxRateField();
        isEconomyScreenDirty = true;
        PlayUiSound(uiSelectClip, 0.75f);
        SessionDebugLogger.Log("TAX_POLICY", $"{policy.Name} enabled={policy.IsEnabled}, rate={policy.RatePercent}%.");
    }

    private void AdjustTaxPolicyRate(int policyId, int delta, bool checkTutorialGoal)
    {
        TaxPolicy policy = GetTaxPolicyById(policyId);
        if (policy == null)
        {
            return;
        }

        policy.RatePercent = Mathf.Clamp(policy.RatePercent + delta, MinDailyBuildingTaxPercent, MaxDailyBuildingTaxPercent);
        SyncPrimaryTaxRateField();
        isEconomyScreenDirty = true;
        if (checkTutorialGoal)
        {
            CheckTutorialTaxRateGoal();
        }

        PlayUiSound(uiSelectClip, 0.75f);
        SessionDebugLogger.Log("TAX_POLICY", $"{policy.Name} rate changed to {policy.RatePercent}%.");
    }

    private void ApplyServiceSaleTaxes(DriverAgent payer, LocationData receiver, int amount, string reason)
    {
        ApplyTaxableEvent(new TaxableEvent
        {
            SourceKind = TaxSourceKind.ServiceSale,
            Amount = amount,
            Quantity = 1,
            LocationType = receiver?.Type,
            LocationInstanceId = receiver?.InstanceId ?? 0,
            PayerWorker = payer,
            ReceiverLocation = receiver,
            Reason = reason
        });
    }

    private void ApplySalaryIncomeTaxes(DriverAgent receiver, int amount, string reason)
    {
        ApplyTaxableEvent(new TaxableEvent
        {
            SourceKind = TaxSourceKind.SalaryIncome,
            Amount = amount,
            Quantity = 1,
            ReceiverWorker = receiver,
            Reason = reason
        });
    }

    private void ApplyTransportFareTaxes(LocationData receiver, int amount, string reason)
    {
        ApplyTaxableEvent(new TaxableEvent
        {
            SourceKind = TaxSourceKind.TransportFare,
            Amount = amount,
            Quantity = Mathf.Max(1, amount),
            LocationType = receiver?.Type,
            LocationInstanceId = receiver?.InstanceId ?? 0,
            ReceiverLocation = receiver,
            Reason = reason
        });
    }

    private void ApplyPropertyPurchaseTaxes(DriverAgent payer, LocationData receiver, int amount, string reason)
    {
        ApplyTaxableEvent(new TaxableEvent
        {
            SourceKind = TaxSourceKind.PropertyPurchase,
            Amount = amount,
            Quantity = 1,
            LocationType = receiver?.Type,
            LocationInstanceId = receiver?.InstanceId ?? 0,
            PayerWorker = payer,
            ReceiverLocation = receiver,
            Reason = reason
        });
    }

    private void ApplyVehiclePurchaseTaxes(DriverAgent payer, LocationData receiver, int amount, string reason)
    {
        ApplyTaxableEvent(new TaxableEvent
        {
            SourceKind = TaxSourceKind.VehiclePurchase,
            Amount = amount,
            Quantity = 1,
            LocationType = receiver?.Type,
            LocationInstanceId = receiver?.InstanceId ?? 0,
            PayerWorker = payer,
            ReceiverLocation = receiver,
            Reason = reason
        });
    }

    private void ApplyGamblingRevenueTaxes(DriverAgent payer, LocationData receiver, int amount, string reason)
    {
        ApplyTaxableEvent(new TaxableEvent
        {
            SourceKind = TaxSourceKind.GamblingRevenue,
            Amount = amount,
            Quantity = 1,
            LocationType = receiver?.Type,
            LocationInstanceId = receiver?.InstanceId ?? 0,
            PayerWorker = payer,
            ReceiverLocation = receiver,
            Reason = reason
        });
    }

    private void ApplyTradeImportTaxes(int amount, int quantity, TradeResourceType resourceType, string counterpartyLabel, string reason)
    {
        ApplyTaxableEvent(new TaxableEvent
        {
            SourceKind = TaxSourceKind.TradeImport,
            Amount = amount,
            Quantity = Mathf.Max(1, quantity),
            ResourceType = resourceType,
            ExternalPayerLabel = string.IsNullOrWhiteSpace(counterpartyLabel) ? "Trade Counterparty" : counterpartyLabel,
            Reason = reason
        });
    }

    private void ApplyTradeExportTaxes(int amount, int quantity, TradeResourceType resourceType, string counterpartyLabel, string reason)
    {
        ApplyTaxableEvent(new TaxableEvent
        {
            SourceKind = TaxSourceKind.TradeExport,
            Amount = amount,
            Quantity = Mathf.Max(1, quantity),
            ResourceType = resourceType,
            ExternalPayerLabel = string.IsNullOrWhiteSpace(counterpartyLabel) ? "Trade Counterparty" : counterpartyLabel,
            Reason = reason
        });
    }

    private void ApplyConstructionPermitTaxes(int amount, string counterpartyLabel, string reason)
    {
        ApplyTaxableEvent(new TaxableEvent
        {
            SourceKind = TaxSourceKind.ConstructionPermit,
            Amount = amount,
            Quantity = 1,
            ExternalPayerLabel = string.IsNullOrWhiteSpace(counterpartyLabel) ? "Construction Crew" : counterpartyLabel,
            Reason = reason
        });
    }

    private int ApplyTaxableEvent(TaxableEvent taxableEvent)
    {
        if (taxableEvent == null || taxableEvent.Amount <= 0)
        {
            return 0;
        }

        EnsureDefaultTaxPolicies();
        EnsureTaxDayStatsCurrent();

        int totalTax = 0;
        for (int i = 0; i < taxPolicies.Count; i++)
        {
            TaxPolicy policy = taxPolicies[i];
            if (policy == null ||
                !policy.IsEnabled ||
                policy.Frequency != TaxFrequency.PerTransaction ||
                policy.SourceKind != taxableEvent.SourceKind ||
                !DoesTaxPolicyApply(policy, taxableEvent))
            {
                continue;
            }

            int taxAmount = CalculateTaxAmount(policy, taxableEvent.Amount, taxableEvent.Quantity);
            if (taxAmount <= 0)
            {
                continue;
            }

            int collected = CollectTaxFromEvent(policy, taxableEvent, taxAmount);
            if (collected <= 0)
            {
                continue;
            }

            totalTax += collected;
        }

        return totalTax;
    }

    private bool DoesTaxPolicyApply(TaxPolicy policy, TaxableEvent taxableEvent)
    {
        if (policy.AppliesToLocationTypes != null &&
            policy.AppliesToLocationTypes.Length > 0 &&
            taxableEvent.LocationType.HasValue &&
            System.Array.IndexOf(policy.AppliesToLocationTypes, taxableEvent.LocationType.Value) < 0)
        {
            return false;
        }

        if (policy.AppliesToResourceTypes != null &&
            policy.AppliesToResourceTypes.Length > 0 &&
            taxableEvent.ResourceType.HasValue &&
            System.Array.IndexOf(policy.AppliesToResourceTypes, taxableEvent.ResourceType.Value) < 0)
        {
            return false;
        }

        return taxableEvent.Amount >= Mathf.Max(0, policy.MinTaxableAmount);
    }

    private int CalculateTaxAmount(TaxPolicy policy, int amount, int quantity)
    {
        int taxAmount = Mathf.FloorToInt(amount * (policy.RatePercent / 100f));
        if (taxAmount == 0 &&
            policy.RatePercent > 0 &&
            amount > 0 &&
            policy.Frequency == TaxFrequency.PerTransaction)
        {
            taxAmount = 1;
        }

        taxAmount += Mathf.Max(0, policy.FlatAmount);
        taxAmount += Mathf.Max(0, policy.PerUnitAmount) * Mathf.Max(1, quantity);
        if (policy.MaxTaxAmount > 0)
        {
            taxAmount = Mathf.Min(taxAmount, policy.MaxTaxAmount);
        }

        return Mathf.Max(0, taxAmount);
    }

    private int CollectTaxFromEvent(TaxPolicy policy, TaxableEvent taxableEvent, int requestedTaxAmount)
    {
        int collected = 0;
        string fromLabel = "External";
        MoneyAccountKind fromKind = MoneyAccountKind.External;
        int fromOwnerId = 0;

        if (policy.Incidence == TaxIncidence.TakenFromReceiver)
        {
            if (taxableEvent.ReceiverLocation != null && taxableEvent.ReceiverLocation.BuildingBank > 0)
            {
                collected = Mathf.Min(requestedTaxAmount, taxableEvent.ReceiverLocation.BuildingBank);
                taxableEvent.ReceiverLocation.BuildingBank -= collected;
                fromLabel = taxableEvent.ReceiverLocation.Label;
                fromKind = MoneyAccountKind.BuildingCash;
                fromOwnerId = taxableEvent.ReceiverLocation.InstanceId;
            }
            else if (taxableEvent.ReceiverWorker != null && taxableEvent.ReceiverWorker.Money > 0)
            {
                collected = Mathf.Min(requestedTaxAmount, taxableEvent.ReceiverWorker.Money);
                taxableEvent.ReceiverWorker.Money -= collected;
                fromLabel = taxableEvent.ReceiverWorker.DriverName;
                fromKind = MoneyAccountKind.ResidentWallet;
                fromOwnerId = taxableEvent.ReceiverWorker.DriverId;
                isDriversScreenDirty = true;
            }
        }
        else if (policy.Incidence == TaxIncidence.AddedToPayerCost)
        {
            if (taxableEvent.PayerWorker != null && taxableEvent.PayerWorker.Money > 0)
            {
                collected = Mathf.Min(requestedTaxAmount, taxableEvent.PayerWorker.Money);
                taxableEvent.PayerWorker.Money -= collected;
                fromLabel = taxableEvent.PayerWorker.DriverName;
                fromKind = MoneyAccountKind.ResidentWallet;
                fromOwnerId = taxableEvent.PayerWorker.DriverId;
                isDriversScreenDirty = true;
            }
            else if (taxableEvent.PayerLocation != null && taxableEvent.PayerLocation.BuildingBank > 0)
            {
                collected = Mathf.Min(requestedTaxAmount, taxableEvent.PayerLocation.BuildingBank);
                taxableEvent.PayerLocation.BuildingBank -= collected;
                fromLabel = taxableEvent.PayerLocation.Label;
                fromKind = MoneyAccountKind.BuildingCash;
                fromOwnerId = taxableEvent.PayerLocation.InstanceId;
            }
            else if (!string.IsNullOrWhiteSpace(taxableEvent.ExternalPayerLabel))
            {
                collected = requestedTaxAmount;
                fromLabel = taxableEvent.ExternalPayerLabel;
                fromKind = MoneyAccountKind.External;
            }
        }

        if (collected <= 0)
        {
            SessionDebugLogger.Log(
                "TAX_EVENT",
                $"{policy.Name} skipped: unable to collect ${requestedTaxAmount} from {taxableEvent.Reason}.");
            return 0;
        }

        money += collected;
        TrackTaxCollected(collected);
        RecordMoneyMovement(
            collected,
            fromLabel,
            "Treasury",
            $"Tax: {policy.Name} - {taxableEvent.Reason}",
            money,
            null,
            fromKind,
            MoneyAccountKind.CityBudget,
            MoneyTransactionReasonKind.BuildingTax,
            fromOwnerId: fromOwnerId);
        SessionDebugLogger.Log(
            "TAX_EVENT",
            $"{policy.Name}: source={taxableEvent.SourceKind}, base=${taxableEvent.Amount}, collected=${collected}, from={fromLabel}, treasury=${money}.");
        return collected;
    }

    private void EnsureTaxDayStatsCurrent()
    {
        if (taxStatsDay == 0)
        {
            taxStatsDay = currentDay;
            return;
        }

        if (taxStatsDay == currentDay)
        {
            return;
        }

        taxCollectedPreviousDay = taxCollectedToday;
        taxEventsPreviousDay = taxEventsToday;
        taxCollectedToday = 0;
        taxEventsToday = 0;
        taxStatsDay = currentDay;
    }

    private void TrackTaxCollected(int amount)
    {
        EnsureTaxDayStatsCurrent();
        taxCollectedToday += Mathf.Max(0, amount);
        taxEventsToday++;
        isEconomyScreenDirty = true;
    }

    private string GetTaxSourceLabel(TaxSourceKind sourceKind)
    {
        return sourceKind switch
        {
            TaxSourceKind.BuildingCashReserve => L("Building cash reserve"),
            TaxSourceKind.ServiceSale         => L("Service sales"),
            TaxSourceKind.SalaryIncome        => L("Salary income"),
            TaxSourceKind.TransportFare       => L("Transport fares"),
            TaxSourceKind.PropertyPurchase    => L("Property purchases"),
            TaxSourceKind.VehiclePurchase     => L("Vehicle purchases"),
            TaxSourceKind.GamblingRevenue     => L("Gambling revenue"),
            TaxSourceKind.TradeImport         => L("Trade imports"),
            TaxSourceKind.TradeExport         => L("Trade exports"),
            TaxSourceKind.ConstructionPermit  => L("Construction permits"),
            _                                 => sourceKind.ToString()
        };
    }

    private string GetTaxFrequencyLabel(TaxFrequency frequency)
    {
        return frequency == TaxFrequency.Daily ? L("Daily") : L("Per transaction");
    }

    private string GetTaxIncidenceLabel(TaxIncidence incidence)
    {
        return incidence == TaxIncidence.TakenFromReceiver ? L("receiver pays") : L("payer pays");
    }

    private IEnumerable<LocationData> EnumerateTaxableBuildingBankLocations()
    {
        HashSet<int> seen = new();

        foreach (LocationData location in locations.Values)
        {
            if (location != null && seen.Add(location.InstanceId))
            {
                yield return location;
            }
        }

        for (int i = 0; i < extraServiceLocations.Count; i++)
        {
            LocationData location = extraServiceLocations[i];
            if (location != null && seen.Add(location.InstanceId))
            {
                yield return location;
            }
        }

        for (int i = 0; i < personalHouses.Count; i++)
        {
            LocationData location = personalHouses[i];
            if (location != null && seen.Add(location.InstanceId))
            {
                yield return location;
            }
        }

        for (int i = 0; i < localStops.Count; i++)
        {
            LocationData location = localStops[i];
            if (location != null && seen.Add(location.InstanceId))
            {
                yield return location;
            }
        }
    }
}
